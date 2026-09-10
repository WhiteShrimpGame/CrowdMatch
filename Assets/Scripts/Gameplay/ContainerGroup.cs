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

        /// <summary>正在上车（jump 或回退 lerp）尚未落定的像素计数。失败判定用它做「静止门槛」。</summary>
        [System.NonSerialized] public int consumingCount;

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
        /// 传送带推送模式：吸收一个像素——扣容量 → 像素上车（有空闲落点则 LocalJump + 弹性缩放并保留为乘客，否则回退 Lerp 后销毁）→ 若耗尽则补位。
        /// 开头用 IsEmpty 兜底（见 review H1/M1），避免同帧竞态下重复消费。
        /// </summary>
        public void ConsumePixel(PixelItem pixel, ContainerItem container)
        {
            if (pixel == null || container == null || container.IsEmpty)
                return;

            bool isLast = container.Consume();
            if (isLast)
                OpenRearLid(container);   // 播放移入动画前，先打开其正后方容器的盖子
            consumingCount++;
            StartCoroutine(MovePixelToContainer(pixel, container, container.gridX, isLast));
        }

        /// <summary>
        /// 复活深排上车（gridZ &gt;= maxOpenRows）：像素原地消失（DisappearWithPop，参考开盖 tween）→ 瞬移到目标车落点出现。
        /// 仍走 Consume 扣容量 → OpenRearLid → consumingCount 计数 → OnPixelConsumed（失败判定 + 出库）完整链路，只是省略 jump。
        /// gridZ &gt;= maxOpenRows + 1（视野外更严格 1 排）的车完成匹配时，直接原地销毁并瞬间补位，避免后期大量已匹配车开走产生垃圾时间。
        /// </summary>
        public void ConsumePixelInstant(PixelItem pixel, ContainerItem container)
        {
            if (pixel == null || container == null || container.IsEmpty)
                return;

            bool isLast = container.Consume();
            bool destroyInPlace = isLast && container.gridZ >= maxOpenRows + 1;   // 视野外更严格 1 排：完成匹配 → 原地销毁
            if (isLast)
                OpenRearLid(container);   // 播放移入动画前，先打开其正后方容器的盖子
            consumingCount++;

            int col = container.gridX;
            System.Action onConsumed = () => OnPixelConsumed(container, col, isLast, destroyInPlace);

            // 原地消失（pop 1.1× → 缩到 0，与开盖同一 tween）后，瞬移到目标车落点出现
            pixel.transform.DisappearWithPop(() =>
            {
                if (pixel == null)
                    return;
                bool placed = container != null && container.PlacePixelInstant(pixel);
                if (!placed)
                    Destroy(pixel.gameObject);   // 无空闲落点（或车已销毁）：销毁
                GameData.ClearedPixelCount++;
                onConsumed();
            });
        }

        private IEnumerator MovePixelToContainer(PixelItem pixel, ContainerItem container, int col, bool isLast)
        {
            // 新上车表现：有空闲落点时由 ContainerItem 接管（挂落点 → DOLocalJump 到 0 → 弹性缩放），
            // 每个上车像素弹回完成后触发 OnPixelConsumed（失败判定 + 出库）；无空闲落点则回退到下面的旧 Lerp。
            System.Action onConsumed = () => OnPixelConsumed(container, col, isLast);
            if (container != null && container.TryBoardPixel(pixel, onConsumed))
                yield break;

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

            onConsumed();
        }

        /// <summary>单个像素上车落定（jump 弹回完成 / 回退 lerp 完成）后的统一回调：递减上车计数 → 事件驱动失败判定 → 耗尽则出库或深排原地销毁。</summary>
        private void OnPixelConsumed(ContainerItem container, int col, bool isLast, bool destroyInPlace = false)
        {
            consumingCount = Mathf.Max(0, consumingCount - 1);
            var gc = GameController.Instance;
            if (gc != null)
                gc.TryCheckFail();
            if (!isLast)
                return;
            if (destroyInPlace)
                DestroyContainerInPlace(container);   // 视野外深排车：原地销毁 + 瞬间补位
            else
                TryExitIfAtFront(container, col);
        }

        /// <summary>某车补位到新位置（roll 或 lerp 完成）后的统一回调：事件驱动失败判定 → 若已在前排且耗尽则尝试出库。</summary>
        private void OnCarArrivedFront(ContainerItem item, int col)
        {
            var gc = GameController.Instance;
            if (gc != null)
                gc.TryCheckFail();
            TryExitIfAtFront(item, col);
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
        /// 复活深排车完成匹配：直接原地销毁（连同已上车的乘客像素，均已计入 ClearedPixelCount），
        /// 后车瞬间补位（teleport，无动画）。用于玩家视野外（gridZ >= maxOpenRows + 1）的车，
        /// 避免后期大量已匹配的车逐个开走出库产生垃圾时间。
        /// </summary>
        private void DestroyContainerInPlace(ContainerItem item)
        {
            if (item == null)
                return;
            int col = item.gridX;
            int row = item.gridZ;
            if (grid == null || !IsInRange(col, row) || grid[col, row] != item)
                return;   // 已被移走 / 已销毁，幂等兜底

            grid[col, row] = null;
            Destroy(item.gameObject);   // 车 + 乘客像素一并销毁（乘客已计入 ClearedPixelCount）

            // 后车瞬间补位（teleport，无动画）：每车向上移一格，与 RefillColumn 同构（保留空格）
            for (int r = row + 1; r < rows; r++)
            {
                var rear = grid[col, r];
                if (rear == null)
                    continue;
                int newRow = r - 1;
                rear.gridZ = newRow;
                grid[col, newRow] = rear;
                grid[col, r] = null;
                rear.transform.localPosition = GetLocalPosition(col, newRow);
            }
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
            Vector3 target = GetLocalPosition(col, row);

            // 有 roll 轴：交给 ContainerItem 驱动「补位移动 + 侧倾 + 上车像素锁定」，完成回调里复位并尝试出库
            if (item.TryStartRefillRoll(target, refillSpeed, () =>
            {
                item.isRefilling = false;
                OnCarArrivedFront(item, col);
            }))
                yield break;

            // 回退：无 roll 轴时旧的纯 Lerp 补位
            Vector3 start = item.transform.localPosition;
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
            OnCarArrivedFront(item, col);
        }

        /// <summary>清空所有 ContainerItem 子物体（先脱离父物体再销毁，避免同帧 GetComponentsInChildren 捡到旧物体）。</summary>
        public void ClearContainers()
        {
            consumingCount = 0;
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

        /// <summary>
        /// 是否存在「已开启匹配且即将抵达前排」的车（含正在补位移动的车）。
        /// 判定 = 存在 isRefilling 的车，或存在「非前排、盖子已打开、且前方所有车都已清空」的车。
        /// 说明：RefillColumn 在补位开始时就把 gridZ 同步置 0，而 lidOpened 在 ConsumePixel（OpenRearLid）时就已锁存，
        /// 因此该条件在整个「上车 → 弹回 → 出库动画 → 补位移动」区间内恒为 true，直到车真正落定前排才释放，杜绝空白时间窗。
        /// 「前方已清空」约束用于排除复活直接给「前方仍有非空车」的后排车开盖的情况——那种车并非即将补位，不算过渡中。
        /// </summary>
        public bool HasPendingFrontTransition()
        {
            for (int col = 0; col < columns; col++)
                for (int row = 0; row < rows; row++)
                {
                    var it = grid[col, row];
                    if (it == null)
                        continue;
                    if (it.isRefilling)
                        return true;
                    if (row >= 1 && it.lidOpened && IsFrontCleared(col, row))
                        return true;
                }
            return false;
        }

        /// <summary>该格前方（row 更小）所有车是否都已为空（含 null），即该格即将被补位到前排。</summary>
        private bool IsFrontCleared(int col, int row)
        {
            for (int r = 0; r < row; r++)
            {
                var f = GetItem(col, r);
                if (f != null && !f.IsEmpty)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 复活：把一批像素按颜色直接匹配到车（非空、非补位中），
        /// 优先前排（gridZ 小，含第 0 排）、同排优先列小。复用 ConsumePixel（扣容量 → 跳车 → 出库链路），
        /// 有车被匹配即播放开盖 tween（OpenLid，幂等）。返回未找到同色车的像素。
        /// 说明：失败仅保证「传送带上的像素不匹配」，缓冲区/带溢出里仍可能有匹配前排车的颜色，
        /// 因此必须优先补第 0 排车，否则会把这类像素误判为无车可匹配而销毁，留下被掏空的前排车堵死整列。
        /// </summary>
        public List<PixelItem> MatchPixelsToCars(List<PixelItem> pixels)
        {
            var unmatched = new List<PixelItem>();
            if (pixels == null)
                return unmatched;

            for (int i = 0; i < pixels.Count; i++)
            {
                var pixel = pixels[i];
                if (pixel == null)
                    continue;

                var car = FindCarForColor(pixel.colorId);
                if (car == null)
                {
                    unmatched.Add(pixel);
                    continue;
                }

                car.OpenLid();              // 有车被匹配 → 播放开盖 tween（幂等）
                if (car.gridZ < maxOpenRows)
                    ConsumePixel(pixel, car);        // 前 maxOpenRows 排：复用正常 jump 上车
                else
                    ConsumePixelInstant(pixel, car); // 更后排：原地消失 → 瞬移到目标车落点出现
            }
            return unmatched;
        }

        /// <summary>从全排（gridZ 0..rows-1）按「前排优先、同排列小优先」找第一个同色、非空、非补位中的车；无则 null。</summary>
        private ContainerItem FindCarForColor(int colorId)
        {
            for (int row = 0; row < rows; row++)
                for (int col = 0; col < columns; col++)
                {
                    var it = GetItem(col, row);
                    if (it == null || it.IsEmpty || it.isRefilling)
                        continue;
                    if (it.colorId != colorId)
                        continue;
                    return it;
                }
            return null;
        }
    }
}
