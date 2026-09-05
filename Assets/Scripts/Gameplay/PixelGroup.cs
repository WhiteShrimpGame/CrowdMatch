using System.Collections.Generic;
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

        [Tooltip("纵向（Z 方向）数量（主网格，不含尾部），row 0 为最前排（Z 最大），向后沿 -Z 延伸")]
        public int rows = 5;

        [Tooltip("尾部网格行数：追加在主网格末尾（继续向 -Z）用于补齐颜色倍数的额外行，0 = 无尾部")]
        public int tailRows = 0;

        [Header("颜色分布生成")]
        [Tooltip("用于生成局部同色分布的候选颜色 ID 数组")]
        public int[] colorIds = new int[] { 0, 1, 2, 3, 4, 5 };

        [Tooltip("每个同色区域的最小连续格子数")]
        public int minRunLength = 2;

        [Tooltip("每个同色区域的最大连续格子数")]
        public int maxRunLength = 5;

        [Tooltip("补充生成尾部颜色时是否把每种颜色总数补足到 3 的倍数（默认勾选）")]
        public bool fillToMultipleOf3 = true;

        [Header("运行时生成")]
        [Tooltip("PixelItem 预制体模板（Block），需自带 PixelItem 组件并配置好 renderers 列表")]
        public GameObject pixelPrefab;

        /// <summary>运行时网格 [column, row]，row 0 为最前排（+Z），row = TotalRows-1 为后排（-Z，含尾部）</summary>
        [System.NonSerialized] public PixelItem[,] grid;

        /// <summary>相邻两格中心点的横向（X）距离</summary>
        public float CellSizeX => unitSize + spacingX;

        /// <summary>相邻两格中心点的纵向（Z）距离</summary>
        public float CellSizeZ => unitSize + spacingZ;

        /// <summary>总行数 = 主网格 rows + 尾部 tailRows</summary>
        public int TotalRows => rows + Mathf.Max(0, tailRows);

        private void Start()
        {
            RebuildGrid();
        }

        /// <summary>扫描子物体，重建 grid 数组</summary>
        public void RebuildGrid()
        {
            grid = new PixelItem[columns, TotalRows];
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
            return col >= 0 && col < columns && row >= 0 && row < TotalRows;
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

        /// <summary>
        /// 刷新所有像素的「暴露（可点击）」状态：
        /// 先标记「直接暴露」的格子（第 0 行，或前方紧邻格同列 row-1 为空），
        /// 再把每个同色连通块整体激活——只要该连通块包含至少一个直接暴露格，块内所有像素同时激活。
        /// 已离开网格的像素由调用方显式关闭，不在此处理。
        /// </summary>
        public void RefreshExposed()
        {
            if (grid == null)
                RebuildGrid();

            int cols = columns;
            int totalRows = TotalRows;

            // 1. 标记「直接暴露」格子
            var directlyExposed = new bool[cols, totalRows];
            for (int c = 0; c < cols; c++)
            {
                for (int r = 0; r < totalRows; r++)
                {
                    if (grid[c, r] == null)
                        continue;
                    directlyExposed[c, r] = r == 0 || grid[c, r - 1] == null;
                }
            }

            // 2. BFS 扩散同色连通块：含直接暴露格的连通块整块激活
            var visited = new bool[cols, totalRows];
            var active = new bool[cols, totalRows];
            int[] dx = { 1, -1, 0, 0 };
            int[] dz = { 0, 0, 1, -1 };

            for (int c = 0; c < cols; c++)
            {
                for (int r = 0; r < totalRows; r++)
                {
                    if (grid[c, r] == null || visited[c, r])
                        continue;

                    int color = grid[c, r].colorId;
                    var cells = new List<Vector2Int>();
                    bool hasExposed = false;
                    var queue = new Queue<Vector2Int>();
                    queue.Enqueue(new Vector2Int(c, r));
                    visited[c, r] = true;

                    while (queue.Count > 0)
                    {
                        var cur = queue.Dequeue();
                        cells.Add(cur);
                        if (directlyExposed[cur.x, cur.y])
                            hasExposed = true;

                        for (int d = 0; d < 4; d++)
                        {
                            int nx = cur.x + dx[d];
                            int nz = cur.y + dz[d];
                            if (nx < 0 || nx >= cols || nz < 0 || nz >= totalRows)
                                continue;
                            if (visited[nx, nz])
                                continue;

                            var nb = grid[nx, nz];
                            if (nb == null || nb.colorId != color)
                                continue;

                            visited[nx, nz] = true;
                            queue.Enqueue(new Vector2Int(nx, nz));
                        }
                    }

                    if (!hasExposed)
                        continue;

                    foreach (var cell in cells)
                        active[cell.x, cell.y] = true;
                }
            }

            // 3. 应用到各像素
            for (int c = 0; c < cols; c++)
            {
                for (int r = 0; r < totalRows; r++)
                {
                    var item = grid[c, r];
                    if (item == null)
                        continue;
                    item.SetExposed(active[c, r]);
                }
            }
        }

        /// <summary>清空所有 PixelItem 子物体（先脱离父物体再销毁，避免同帧 GetComponentsInChildren 捡到旧物体）。</summary>
        public void ClearPixels()
        {
            var items = GetComponentsInChildren<PixelItem>();
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

        /// <summary>在指定格子生成一个 PixelItem 并应用颜色材质（供运行时关卡加载使用）。PixelItem 组件来自预制体，不再动态创建。</summary>
        public PixelItem SpawnPixel(int col, int row, int colorId, ColorConfig config)
        {
            if (pixelPrefab == null)
            {
                Debug.LogError("[PixelGroup] pixelPrefab 为空，无法生成像素（请挂 Block 预制体，需自带 PixelItem 组件）。");
                return null;
            }

            GameObject go = Instantiate(pixelPrefab);
            go.name = "Pixel_" + row + "_" + col;
            go.transform.SetParent(transform, false);
            go.transform.localPosition = GetLocalPosition(col, row);
            go.transform.localScale = Vector3.one * unitSize;

            var item = go.GetComponent<PixelItem>();
            if (item == null)
            {
                Debug.LogError("[PixelGroup] 预制体 " + pixelPrefab.name + " 缺少 PixelItem 组件。");
                return null;
            }

            item.gridX = col;
            item.gridZ = row;
            item.colorId = colorId;
            item.ApplyMaterial(config);
            return item;
        }
    }
}
