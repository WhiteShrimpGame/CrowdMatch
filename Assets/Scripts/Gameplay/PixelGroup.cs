using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 管理一个 columns × rows 的 PixelItem 网格。
    /// 可配置单位大小 unitSize、横向间距 spacingX、纵向间距 spacingZ、横向数量 columns、纵向数量 rows。
    /// 运行时通过扫描子物体重建 grid。
    /// </summary>
    public class PixelGroup : MonoBehaviour
    {
        [Header("单位布局")]
        [Tooltip("单个像素的直径（球 primitive 直径 = 1，scale 用 unitSize 即得世界直径）")]
        public float unitSize = 1f;

        [Tooltip("横向（X 方向）相邻单位表面之间的间距")]
        public float spacingX = 0.1f;

        [Tooltip("纵向（Z 方向）相邻单位表面之间的间距")]
        public float spacingZ = 0.1f;

        [Header("网格数量")]
        [Tooltip("横向（X 方向）数量")]
        public int columns = 5;

        [Tooltip("纵向（Z 方向）数量，row 0 为最前排（Z 最大），向后沿 -Z 延伸")]
        public int rows = 5;

        [Header("颜色分布生成")]
        [Tooltip("用于生成局部同色分布的候选颜色 ID 数组")]
        public int[] colorIds = new int[] { 0, 1, 2, 3, 4, 5 };

        [Tooltip("每个同色区域的最小连续格子数")]
        public int minRunLength = 2;

        [Tooltip("每个同色区域的最大连续格子数")]
        public int maxRunLength = 5;

        /// <summary>运行时网格 [column, row]，row 0 为最前排（+Z），row = rows-1 为后排（-Z）</summary>
        [System.NonSerialized] public PixelItem[,] grid;

        /// <summary>相邻两格中心点的横向（X）距离</summary>
        public float CellSizeX => unitSize + spacingX;

        /// <summary>相邻两格中心点的纵向（Z）距离</summary>
        public float CellSizeZ => unitSize + spacingZ;

        private void Start()
        {
            RebuildGrid();
        }

        /// <summary>扫描子物体，重建 grid 数组</summary>
        public void RebuildGrid()
        {
            grid = new PixelItem[columns, rows];
            foreach (var item in GetComponentsInChildren<PixelItem>())
            {
                if (IsInRange(item.gridX, item.gridZ))
                {
                    grid[item.gridX, item.gridZ] = item;
                    item.group = this;
                }
            }
        }

        /// <summary>取指定格子的单位，越界返回 null</summary>
        public PixelItem GetItem(int col, int row)
        {
            if (grid == null)
                return null;
            if (!IsInRange(col, row))
                return null;
            return grid[col, row];
        }

        /// <summary>判断格子坐标是否在范围内</summary>
        public bool IsInRange(int col, int row)
        {
            return col >= 0 && col < columns && row >= 0 && row < rows;
        }

        /// <summary>
        /// 某格子的本地坐标：X 以自身为中心（col 0 = 最小 X），row 0 落在自身中心点（z=0），
        /// 后续行依次向 -Z 延伸一个 CellSizeZ。
        /// </summary>
        public Vector3 GetLocalPosition(int col, int row)
        {
            float x = (col - (columns - 1) * 0.5f) * CellSizeX;
            float z = -row * CellSizeZ;
            return new Vector3(x, 0f, z);
        }

        /// <summary>某格子的世界坐标</summary>
        public Vector3 GetWorldPosition(int col, int row)
        {
            return transform.TransformPoint(GetLocalPosition(col, row));
        }
    }
}
