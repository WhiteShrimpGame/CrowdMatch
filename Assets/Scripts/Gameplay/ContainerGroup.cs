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
                var front = GetItem(col, 0);
                if (front == null || front.IsEmpty)
                    continue;

                var pixel = FindMatchingPixel(gc.gatheredItems, front.colorId);
                if (pixel == null)
                    continue;

                gc.gatheredItems.Remove(pixel);
                bool isLast = front.Consume(); // 预留一格容量
                StartCoroutine(MovePixelToContainer(pixel, front, col, isLast));
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

            Destroy(pixel.gameObject);

            if (isLast)
                DisappearAndRefill(container, col);
        }

        private void DisappearAndRefill(ContainerItem gone, int col)
        {
            grid[col, 0] = null;
            Destroy(gone.gameObject);

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
        }
    }
}
