using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 管理一个 columns × rows 的 ContainerItem 组。
    /// 位于游戏画面上方（Z 较大一方），以 Z 最小为最前排；前排 Z = 父物体 0 点，后排向 +Z 延展。
    /// 运行时监控聚集 List，把匹配颜色的 PixelItem 移动到最前排 ContainerItem 并消耗容量；
    /// 最后一个 PixelItem 到达时容器消失，后排依次向前补位。
    /// </summary>
    public class ContainerGroup : MonoBehaviour
    {
        [Header("布局")]
        [Tooltip("ContainerItem 预制体模板")]
        public ContainerItem containerPrefab;

        [Tooltip("横向（X 方向）数量")]
        public int columns = 5;

        [Tooltip("纵向（Z 方向）数量，0 为最前排")]
        public int rows = 3;

        [Tooltip("横向间距（X）")]
        public float xSpacing = 1.2f;

        [Tooltip("纵向间距（Z）")]
        public float zSpacing = 1.2f;

        [Header("生成参数（编辑器用）")]
        [Tooltip("读取颜色分布的 PixelGroup，留空自动查找")]
        public PixelGroup pixelGroup;

        [Tooltip("单个容器最小容量")]
        public int minCapacity = 2;

        [Tooltip("单个容器最大容量")]
        public int maxCapacity = 5;

        [Tooltip("生成时每个容器最多跨多少像素深度层抽取同色（0 = 仅最前排像素层）")]
        public int maxSpanLayers = 4;

        [Tooltip("最多开启匹配的前排数（前 N 排可同时匹配，默认 4 = 最前排 + 后三排）")]
        public int maxOpenRows = 4;

        [Header("速度")]
        [Tooltip("PixelItem 移向容器的速度")]
        public float consumeSpeed = 10f;

        [Tooltip("容器补位速度")]
        public float refillSpeed = 10f;

        /// <summary>运行时网格 [column, row]，row 0 为最前排</summary>
        [System.NonSerialized] public ContainerItem[,] grid;

        private void Start()
        {
            RebuildGrid();
        }

        public void RebuildGrid()
        {
            grid = new ContainerItem[columns, rows];
            foreach (var item in GetComponentsInChildren<ContainerItem>())
            {
                if (IsInRange(item.gridX, item.gridZ))
                {
                    grid[item.gridX, item.gridZ] = item;
                    item.group = this;
                    if (item.gridZ == 0)
                        item.HideLid();   // 初始就在第一排：盖子直接隐藏
                }
            }
        }

        public bool IsInRange(int col, int row)
        {
            return col >= 0 && col < columns && row >= 0 && row < rows;
        }

        public ContainerItem GetItem(int col, int row)
        {
            if (grid == null)
                return null;
            if (!IsInRange(col, row))
                return null;
            return grid[col, row];
        }

        /// <summary>
        /// 某格小车是否已「开启匹配」：处于前 maxOpenRows 排，且前方（row 更小）没有车、或前方所有车都已找全匹配对象（容量耗尽）。
        /// </summary>
        public bool IsOpen(int col, int row)
        {
            if (row >= maxOpenRows)
                return false;
            for (int r = 0; r < row; r++)
            {
                var f = GetItem(col, r);
                if (f != null && !f.IsEmpty)
                    return false;   // 前方还有未找全匹配对象的车
            }
            return true;
        }

        /// <summary>某格子的本地坐标：X 居中，前排（row 0）Z = 0，后排向 +Z 延展</summary>
        public Vector3 GetLocalPosition(int col, int row)
        {
            float x = (col - (columns - 1) * 0.5f) * xSpacing;
            float z = row * zSpacing;
            return new Vector3(x, 0f, z);
        }

        private void Update()
        {
            ProcessConsumption();
        }

        private void ProcessConsumption()
        {
            var gc = GameController.Instance;
            if (gc == null || gc.gatheredItems == null)
                return;

            for (int col = 0; col < columns; col++)
            {
                for (int row = 0; row < rows; row++)
                {
                    var item = GetItem(col, row);
                    if (item == null || item.IsEmpty || item.isRefilling)
                        continue;

                    // 只有满足「处于前 maxOpenRows 排」且「前方没有车 / 前方全部找全匹配对象」才开启匹配
                    if (!IsOpen(col, row))
                        continue;

                    // 首次满足条件时播放开盖动画（幂等）
                    item.OpenLid();

                    var pixel = FindMatchingPixel(gc.gatheredItems, item.colorId);
                    if (pixel == null)
                        continue;

                    gc.gatheredItems.Remove(pixel);
                    bool isLast = item.Consume();
                    StartCoroutine(MovePixelToContainer(pixel, item, col, isLast));
                }
            }
        }

        private PixelItem FindMatchingPixel(List<PixelItem> list, int colorId)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var p = list[i];
                if (p != null && p.arrivedAtGatherPoint && p.colorId == colorId)
                    return p;
            }
            return null;
        }

        /// <summary>
        /// 传送带推送模式：找某像素正前方（远侧）的同色「可匹配」容器；无则 null。
        /// 每列从最前排（row 0）向后逐排找第一个「可匹配（IsOpen）且非空且同色」的容器（最多 maxOpenRows 排）；
        /// 横向 / 纵向距离统一以前排（row 0）槽位位置判定——像素始终被送到前排，后排只是接力匹配，
        /// 用后排自身位置会因 Z 距离太远匹配不上。
        /// </summary>
        public ContainerItem FindMatchableContainer(PixelItem pixel, float matchRangeX, float matchRangeZ)
        {
            if (pixel == null || grid == null)
                return null;

            ContainerItem best = null;
            float bestDx = float.MaxValue;
            for (int col = 0; col < columns; col++)
            {
                var item = FindMatchableInColumn(col, pixel.colorId);
                if (item == null)
                    continue;

                Vector3 frontWorld = transform.TransformPoint(GetLocalPosition(col, 0));
                float dx = Mathf.Abs(frontWorld.x - pixel.transform.position.x);
                float dz = Mathf.Abs(frontWorld.z - pixel.transform.position.z);
                if (dx <= matchRangeX && dz <= matchRangeZ && dx < bestDx)
                {
                    bestDx = dx;
                    best = item;
                }
            }
            return best;
        }

        /// <summary>某列从最前排向后，找第一个「可匹配（IsOpen）且非空且同色且非补位中」的容器（最多 maxOpenRows 排）；无则 null。</summary>
        private ContainerItem FindMatchableInColumn(int col, int colorId)
        {
            int limit = Mathf.Min(rows, maxOpenRows);
            for (int row = 0; row < limit; row++)
            {
                var item = GetItem(col, row);
                if (item == null || item.IsEmpty || item.isRefilling || item.colorId != colorId)
                    continue;
                if (!IsOpen(col, row))
                    continue;
                return item;
            }
            return null;
        }

        /// <summary>
        /// 传送带推送模式：吸收一个像素——扣容量 → 像素 Lerp 进容器 → 销毁 → 若耗尽则补位。
        /// 开头用 IsEmpty 兜底（见 review H1/M1），避免同帧竞态下重复消费。
        /// </summary>
        public void ConsumePixel(PixelItem pixel, ContainerItem container)
        {
            if (pixel == null || container == null || container.IsEmpty)
                return;

            bool isLast = container.Consume();
            if (isLast)
                OpenRearLid(container);   // 播放移入动画前，先打开其正后方容器的盖子
            StartCoroutine(MovePixelToContainer(pixel, container, container.gridX, isLast));
        }

        private IEnumerator MovePixelToContainer(PixelItem pixel, ContainerItem container, int col, bool isLast)
        {
            Vector3 start = pixel.transform.position;
            Vector3 target = container.transform.position;

            float dist = Vector3.Distance(start, target);
            float duration = consumeSpeed > 0.0001f ? dist / consumeSpeed : 0f;

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(duration > 0.0001f ? t / duration : 1f);
                pixel.transform.position = Vector3.Lerp(start, target, k);
                yield return null;
            }
            pixel.transform.position = target;

            GameData.ClearedPixelCount++;
            Destroy(pixel.gameObject);

            if (isLast)
                TryExitIfAtFront(container, col);   // 前排且耗尽才出库；后排先等补位到前排
        }

        /// <summary>
        /// 前排容器耗尽：立即清空该格，启动小车出库动画；转正瞬间触发补位。
        /// 轴未配置时（ContainerExitDriver.Play 回退）等价旧的「直接销毁 + 补位」。
        /// </summary>
        private void StartContainerExit(ContainerItem gone, int col)
        {
            grid[col, 0] = null;

            var driver = gone.GetComponent<ContainerExitDriver>();
            if (driver == null)
                driver = gone.gameObject.AddComponent<ContainerExitDriver>();
            driver.Play(() => RefillColumn(col));
        }

        /// <summary>
        /// 小车在前排且容量耗尽时启动出库。幂等：grid[col,0] 已非本车（或已开始出库）时跳过，
        /// 避免「后排满但未补位」或「补位完成 / 像素到达」同帧竞态下重复触发。
        /// </summary>
        private void TryExitIfAtFront(ContainerItem item, int col)
        {
            if (item == null || !item.IsEmpty)
                return;
            if (item.isRefilling)
                return;   // 补位移动中，等 MoveContainer 完成后由它触发
            if (grid == null || grid[col, 0] != item)
                return;   // 不在前排（或已开始出库）
            StartContainerExit(item, col);
        }

        /// <summary>
        /// 传送带吸收模式：某容器耗尽时打开其正后方（gridZ + 1）容器的盖子，让它随后可接收像素。
        /// 前排 / 已开放的后排容器共用此逻辑——耗尽谁的容量就开谁后面的盖子。
        /// </summary>
        private void OpenRearLid(ContainerItem container)
        {
            if (container == null)
                return;
            var rear = GetItem(container.gridX, container.gridZ + 1);
            if (rear != null)
                rear.OpenLid();
        }

        /// <summary>某列后排容器依次前移一格（补位）。</summary>
        private void RefillColumn(int col)
        {
            for (int row = 1; row < rows; row++)
            {
                var it = grid[col, row];
                if (it == null)
                    continue;

                int newRow = row - 1;
                it.gridZ = newRow;
                grid[col, newRow] = it;
                grid[col, row] = null;
                StartCoroutine(MoveContainer(it, col, newRow));
            }
        }

        private IEnumerator MoveContainer(ContainerItem item, int col, int row)
        {
            item.isRefilling = true;
            Vector3 start = item.transform.localPosition;
            Vector3 target = GetLocalPosition(col, row);

            float dist = Vector3.Distance(start, target);
            float duration = refillSpeed > 0.0001f ? dist / refillSpeed : 0f;

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(duration > 0.0001f ? t / duration : 1f);
                item.transform.localPosition = Vector3.Lerp(start, target, k);
                yield return null;
            }
            item.transform.localPosition = target;
            item.isRefilling = false;

            // 补位到前排后，若该车已在后方等满（容量耗尽），立即启动出库
            TryExitIfAtFront(item, col);
        }

        /// <summary>清空所有 ContainerItem 子物体（先脱离父物体再销毁，避免同帧 GetComponentsInChildren 捡到旧物体）。</summary>
        public void ClearContainers()
        {
            var items = GetComponentsInChildren<ContainerItem>();
            for (int i = items.Length - 1; i >= 0; i--)
            {
                var it = items[i];
                if (it == null)
                    continue;
                it.transform.SetParent(null, true);
                if (Application.isPlaying)
                    Destroy(it.gameObject);
                else
                    DestroyImmediate(it.gameObject);
            }
        }

        /// <summary>在指定格子生成一个 ContainerItem 并应用颜色/容量（供运行时关卡加载使用）。</summary>
        public ContainerItem SpawnContainer(int col, int row, int colorId, int capacity, ColorConfig config)
        {
            GameObject go = containerPrefab != null
                ? Instantiate(containerPrefab).gameObject
                : GameObject.CreatePrimitive(PrimitiveType.Cube);

            go.name = "Container_" + col + "_" + row;
            go.transform.SetParent(transform, false);
            go.transform.localPosition = GetLocalPosition(col, row);

            var item = go.GetComponent<ContainerItem>();
            if (item == null)
                item = go.AddComponent<ContainerItem>();

            item.gridX = col;
            item.gridZ = row;
            item.colorId = colorId;
            item.SetCapacity(capacity);
            item.ApplyMaterial(config);
            if (row == 0)
                item.HideLid();   // 初始就在第一排：盖子直接隐藏
            return item;
        }

        /// <summary>是否存在同色且可匹配的容器（前排或已开放的后排，最多 maxOpenRows 排）。用于失败判定。</summary>
        public bool HasMatchableContainerOfColor(int colorId)
        {
            for (int col = 0; col < columns; col++)
            {
                if (FindMatchableInColumn(col, colorId) != null)
                    return true;
            }
            return false;
        }
    }
}
