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

        [Tooltip("墙体角格预制体（占一格，转角处，可视觉溢出边界）")]
        public GameObject wallCornerPrefab;

        [Tooltip("墙体边格预制体（占一格，直段中间，可视觉溢出边界）")]
        public GameObject wallEdgePrefab;

        [Tooltip("墙体端点预制体（占一格，墙的端点，可视觉溢出边界）")]
        public GameObject wallEndPrefab;

        [Tooltip("墙体独立 1×1 预制体（占一格，无相邻墙格，可视觉溢出边界）")]
        public GameObject wallSinglePrefab;

        [Tooltip("管道预制体模板（需自带 PipeItem 组件，并含波次数字 Text 与下一颜色指示 Renderer）")]
        public GameObject pipePrefab;

        [Tooltip("箱子角格预制体（占一格，可视觉溢出边界）")]
        public GameObject boxCornerPrefab;

        [Tooltip("箱子边格预制体（占一格，可视觉溢出边界）")]
        public GameObject boxEdgePrefab;

        [Tooltip("箱子中心格预制体（占一格，可视觉溢出边界）")]
        public GameObject boxCenterPrefab;

        /// <summary>运行时网格 [column, row]，row 0 为最前排（+Z），row = TotalRows-1 为后排（-Z，含尾部）</summary>
        [System.NonSerialized] public PixelItem[,] grid;

        /// <summary>墙体占用表 [column, row]：true = 该格被 WallItem 占据（作为障碍参与暴露与寻路）。</summary>
        [System.NonSerialized] public bool[,] wallGrid;

        /// <summary>管道占用表 [column, row]：true = 该格被 PipeItem 占据（作为障碍参与暴露与寻路）。</summary>
        [System.NonSerialized] public bool[,] pipeGrid;

        /// <summary>箱子占用表 [column, row]：true = 该格被未开箱的 BoxItem 占据（作为障碍参与暴露与寻路）。</summary>
        [System.NonSerialized] public bool[,] boxGrid;

        /// <summary>运行时收集到的所有管道（重建 grid 时刷新）。</summary>
        [System.NonSerialized] public List<PipeItem> pipes = new List<PipeItem>();

        /// <summary>运行时收集到的所有箱子（重建 grid 时刷新；含已开箱的，用 opened 区分）。</summary>
        [System.NonSerialized] public List<BoxItem> boxes = new List<BoxItem>();

        /// <summary>正在释放中的箱子数量（开箱动画期间 > 0，供失败判定阻塞）。</summary>
        [System.NonSerialized] public int releasingBoxesCount;

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

        /// <summary>扫描子物体，重建 grid 数组、墙体占用表与管道占用表</summary>
        public void RebuildGrid()
        {
            grid = new PixelItem[columns, TotalRows];
            wallGrid = new bool[columns, TotalRows];
            pipeGrid = new bool[columns, TotalRows];
            boxGrid = new bool[columns, TotalRows];
            pipes = new List<PipeItem>();
            boxes = new List<BoxItem>();

            foreach (var item in GetComponentsInChildren<PixelItem>())
            {
                if (IsInRange(item.gridX, item.gridZ))
                {
                    grid[item.gridX, item.gridZ] = item;
                    item.group = this;
                }
            }

            foreach (var wall in GetComponentsInChildren<WallItem>())
            {
                if (wall == null)
                    continue;
                wall.group = this;
                foreach (var cell in wall.EnumerateOccupiedCells())
                {
                    if (IsInRange(cell.x, cell.y))
                        wallGrid[cell.x, cell.y] = true;
                }
            }

            foreach (var pipe in GetComponentsInChildren<PipeItem>())
            {
                if (pipe == null)
                    continue;
                pipe.group = this;
                pipes.Add(pipe);
                var cell = PipeItem.GetPipeCell(pipe.points);
                if (IsInRange(cell.x, cell.y))
                    pipeGrid[cell.x, cell.y] = true;
            }

            foreach (var box in GetComponentsInChildren<BoxItem>())
            {
                if (box == null)
                    continue;
                box.group = this;
                boxes.Add(box);
                if (box.opened)
                    continue;   // 已开箱不再占格
                for (int r = box.rowMin; r <= box.rowMax; r++)
                    for (int c = box.colMin; c <= box.colMax; c++)
                        if (IsInRange(c, r))
                            boxGrid[c, r] = true;
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

        /// <summary>该格是否被墙体占据。</summary>
        public bool IsWall(int col, int row)
        {
            if (wallGrid == null)
                return false;
            if (!IsInRange(col, row))
                return false;
            return wallGrid[col, row];
        }

        /// <summary>该格是否被管道占据。</summary>
        public bool IsPipe(int col, int row)
        {
            if (pipeGrid == null)
                return false;
            if (!IsInRange(col, row))
                return false;
            return pipeGrid[col, row];
        }

        /// <summary>该格是否被未开箱的箱子占据。</summary>
        public bool IsBox(int col, int row)
        {
            if (boxGrid == null)
                return false;
            if (!IsInRange(col, row))
                return false;
            return boxGrid[col, row];
        }

        /// <summary>该格是否为障碍（墙体、管道或未开箱的箱子）。</summary>
        public bool IsBlocked(int col, int row) => IsWall(col, row) || IsPipe(col, row) || IsBox(col, row);

        /// <summary>该格是否为空（既无像素也无墙体/管道，可作为可通行 / 暴露判定依据）。grid 未重建时视为非空。</summary>
        public bool IsEmpty(int col, int row)
        {
            if (!IsInRange(col, row))
                return false;
            if (grid == null)
                return false;
            return grid[col, row] == null && !IsBlocked(col, row);
        }

        /// <summary>该格是否被「仍有未释放波次的管道」覆盖（管道自身格 + 轨道格）。暴露判定时视为阻挡。</summary>
        public bool IsActivePipeBlocked(int col, int row)
        {
            if (pipes == null)
                return false;
            for (int i = 0; i < pipes.Count; i++)
            {
                var pipe = pipes[i];
                if (pipe == null || !pipe.HasRemainingWaves)
                    continue;
                if (pipe.CoversCell(col, row))
                    return true;
            }
            return false;
        }

        /// <summary>所有「正在释放中」管道的轨迹（管道自身格 + 轨道格）占据的 row 最小值。
        /// 无正在释放的管道时返回 int.MaxValue（表示不限制离场）。</summary>
        public int MinActivePipeTrackRow()
        {
            if (pipes == null)
                return int.MaxValue;
            int min = int.MaxValue;
            for (int i = 0; i < pipes.Count; i++)
            {
                var pipe = pipes[i];
                if (pipe == null || !pipe.IsReleasing || pipe.points == null)
                    continue;
                for (int j = 0; j < pipe.points.Count; j++)
                {
                    int r = Mathf.RoundToInt(pipe.points[j].y);
                    if (r < min)
                        min = r;
                }
            }
            return min;
        }

        /// <summary>暴露判定用的「空」：无像素、非墙体/管道障碍、且未被活跃管道覆盖。</summary>
        public bool IsEmptyForExposure(int col, int row)
        {
            if (!IsInRange(col, row))
                return false;
            if (grid == null)
                return false;
            if (grid[col, row] != null)
                return false;
            if (IsBlocked(col, row))
                return false;
            return !IsActivePipeBlocked(col, row);
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
        /// 先标记「直接暴露」的格子（第 0 行，或四周前/后/左/右任一紧邻格为「连通首排的空格」），
        /// 再把每个同色连通块整体激活——只要该连通块包含至少一个直接暴露格，块内所有像素同时激活。
        /// 「空」必须是真正通向出口的空：被活跃管道（新蛇即将填充）隔开的空格不算，避免蛇被移出后误暴露。
        /// 已离开网格的像素由调用方显式关闭，不在此处理。
        /// </summary>
        public void RefreshExposed()
        {
            if (grid == null)
                RebuildGrid();

            int cols = columns;
            int totalRows = TotalRows;

            // 0. 计算「能连通到首排的空格」：从首排空/出口出发 BFS，只通过 IsEmptyForExposure 的空格扩散。
            //    「空」必须是真正通向出口的空——被活跃管道（新蛇即将填充）隔开的空格不算，
            //    避免蛇被移出后，紧邻非蛇同色 Pixel 的其他颜色块因「局部空」被误激活 Animator。
            var reachableEmpty = new bool[cols, totalRows];
            {
                int[] edx = { 1, -1, 0, 0 };
                int[] edz = { 0, 0, 1, -1 };
                var q = new Queue<Vector2Int>();
                for (int c = 0; c < cols; c++)
                {
                    if (IsEmptyForExposure(c, 0))
                    {
                        reachableEmpty[c, 0] = true;
                        q.Enqueue(new Vector2Int(c, 0));
                    }
                }
                while (q.Count > 0)
                {
                    var cur = q.Dequeue();
                    for (int d = 0; d < 4; d++)
                    {
                        int nx = cur.x + edx[d];
                        int nz = cur.y + edz[d];
                        if (nx < 0 || nx >= cols || nz < 0 || nz >= totalRows)
                            continue;
                        if (reachableEmpty[nx, nz])
                            continue;
                        if (!IsEmptyForExposure(nx, nz))
                            continue;
                        reachableEmpty[nx, nz] = true;
                        q.Enqueue(new Vector2Int(nx, nz));
                    }
                }
            }

            // 1. 标记「直接暴露」格子：首排，或四周任一紧邻格为「连通首排的空格」（墙体/管道/活跃管道覆盖视为占用）
            var directlyExposed = new bool[cols, totalRows];
            for (int c = 0; c < cols; c++)
            {
                for (int r = 0; r < totalRows; r++)
                {
                    if (grid[c, r] == null || IsBlocked(c, r))
                        continue;
                    directlyExposed[c, r] =
                        r == 0 ||                                            // 前方：出口（第一排）
                        (r - 1 >= 0 && reachableEmpty[c, r - 1]) ||          // 前方空（连通首排）
                        (r + 1 < totalRows && reachableEmpty[c, r + 1]) ||   // 后方空（连通首排）
                        (c - 1 >= 0 && reachableEmpty[c - 1, r]) ||          // 左方空（连通首排）
                        (c + 1 < cols && reachableEmpty[c + 1, r]);          // 右方空（连通首排）
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
                    if (grid[c, r] == null || IsBlocked(c, r) || visited[c, r])
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
                            if (nb == null || IsBlocked(nx, nz) || nb.colorId != color)
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

        /// <summary>清空所有 WallItem 子物体（供关卡重载时重建墙体）。</summary>
        public void ClearWalls()
        {
            var walls = GetComponentsInChildren<WallItem>();
            for (int i = walls.Length - 1; i >= 0; i--)
            {
                var w = walls[i];
                if (w == null)
                    continue;
                w.transform.SetParent(null, true);
                if (Application.isPlaying)
                    Destroy(w.gameObject);
                else
                    DestroyImmediate(w.gameObject);
            }
            wallGrid = new bool[columns, TotalRows];
        }

        /// <summary>清空所有 PipeItem 子物体（供关卡重载时重建管道）。</summary>
        public void ClearPipes()
        {
            var items = GetComponentsInChildren<PipeItem>();
            for (int i = items.Length - 1; i >= 0; i--)
            {
                var p = items[i];
                if (p == null)
                    continue;
                p.transform.SetParent(null, true);
                if (Application.isPlaying)
                    Destroy(p.gameObject);
                else
                    DestroyImmediate(p.gameObject);
            }
            pipeGrid = new bool[columns, TotalRows];
            pipes = new List<PipeItem>();
        }

        /// <summary>
        /// 清空所有 BoxItem 及其隐藏 Pixel（供关卡重载时重建箱子）。
        /// 隐藏 Pixel 是 PixelGroup 的子物体（gridX=gridZ=-1 且 inactive），hiddenPixels 列表在域重载后会清空，
        /// 因此不依赖 b.hiddenPixels，而是按哨兵坐标扫描销毁所有隐藏 Pixel。
        /// </summary>
        public void ClearBoxes()
        {
            // 1. 先销毁所有隐藏 Pixel（哨兵坐标 gridX==-1 && gridZ==-1，inactive）。用 includeInactive 才能捡到。
            var pixels = GetComponentsInChildren<PixelItem>(true);
            for (int i = pixels.Length - 1; i >= 0; i--)
            {
                var p = pixels[i];
                if (p == null)
                    continue;
                if (p.gridX != -1 || p.gridZ != -1)
                    continue;
                p.transform.SetParent(null, true);
                if (Application.isPlaying)
                    Destroy(p.gameObject);
                else
                    DestroyImmediate(p.gameObject);
            }

            // 2. 再销毁所有箱子（视觉部件是箱子的子物体，随箱子一并销毁）。
            var items = GetComponentsInChildren<BoxItem>();
            for (int i = items.Length - 1; i >= 0; i--)
            {
                var b = items[i];
                if (b == null)
                    continue;
                b.transform.SetParent(null, true);
                if (Application.isPlaying)
                    Destroy(b.gameObject);
                else
                    DestroyImmediate(b.gameObject);
            }
            boxGrid = new bool[columns, TotalRows];
            boxes = new List<BoxItem>();
            releasingBoxesCount = 0;
        }

        /// <summary>
        /// 在 PixelGroup 下动态创建一个 WallItem（不依赖预制体，用 new GameObject + AddComponent），
        /// 并调用其 BuildVisual 用角/边/端点/独立 1×1 四类预制体拼接墙体实体（运行时可视化）。
        /// </summary>
        public WallItem SpawnWall(IList<Vector2> points)
        {
            var go = new GameObject("Wall_" + (transform.childCount + 1));
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;

            var wall = go.AddComponent<WallItem>();
            wall.points = new List<Vector2>(points);
            wall.BuildVisual(this);
            return wall;
        }

        /// <summary>在指定格子生成一个 PixelItem 并应用颜色材质（供运行时关卡加载使用）。PixelItem 组件来自预制体，不再动态创建。</summary>
        public PixelItem SpawnPixel(int col, int row, int colorId, ColorConfig config, bool scaleZero = false)
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
            go.transform.localScale = Vector3.one * (scaleZero ? 0f : unitSize);

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

        /// <summary>
        /// 在 PixelGroup 下动态创建一个 PipeItem（用 pipePrefab 实例化），
        /// 定位到 points[0] 所在格；points/colors 交由调用方传入。
        /// </summary>
        public PipeItem SpawnPipe(IList<Vector2> points, IList<int> colors)
        {
            if (pipePrefab == null)
            {
                Debug.LogError("[PixelGroup] pipePrefab 为空，无法生成管道（请指定自带 PipeItem 组件的预制体）。");
                return null;
            }

            var go = Instantiate(pipePrefab);
            go.name = "Pipe_" + (transform.childCount + 1);
            go.transform.SetParent(transform, false);

            var pipe = go.GetComponent<PipeItem>();
            if (pipe == null)
            {
                Debug.LogError("[PixelGroup] 预制体 " + pipePrefab.name + " 缺少 PipeItem 组件。");
                if (Application.isPlaying)
                    Destroy(go);
                else
                    DestroyImmediate(go);
                return null;
            }

            pipe.points = new List<Vector2>(points);
            pipe.colors = new List<int>(colors);
            var cell = PipeItem.GetPipeCell(pipe.points);
            go.transform.localPosition = GetLocalPosition(cell.x, cell.y);
            return pipe;
        }

        /// <summary>
        /// 在 PixelGroup 下动态创建一个 BoxItem（new GameObject + AddComponent），
        /// 并把区域裁剪到网格内、拼接箱子视觉、生成隐藏 Pixel。视觉预制体取自 PixelGroup 字段。
        /// </summary>
        public BoxItem SpawnBox(LevelData.BoxData data, ColorConfig config)
        {
            if (data == null)
                return null;
            if (pixelPrefab == null)
            {
                Debug.LogError("[PixelGroup] pixelPrefab 为空，无法生成箱子隐藏 Pixel。");
                return null;
            }

            int cmin = Mathf.Max(0, Mathf.Min(data.colMin, data.colMax));
            int cmax = Mathf.Min(columns - 1, Mathf.Max(data.colMin, data.colMax));
            int rmin = Mathf.Max(0, Mathf.Min(data.rowMin, data.rowMax));
            int rmax = Mathf.Min(TotalRows - 1, Mathf.Max(data.rowMin, data.rowMax));

            if (cmin > cmax || rmin > rmax)
            {
                Debug.LogWarning("[PixelGroup] 箱子区域完全越界，已忽略。");
                return null;
            }

            var go = new GameObject("Box_" + rmin + "_" + cmin);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;

            var box = go.AddComponent<BoxItem>();
            box.colMin = cmin;
            box.rowMin = rmin;
            box.colMax = cmax;
            box.rowMax = rmax;
            box.colorIds = data.colorIds != null ? (int[])data.colorIds.Clone() : new int[0];
            box.jumpStartInterval = data.jumpStartInterval;
            box.jumpSpawnYOffset = data.jumpSpawnYOffset;
            box.cornerPrefab = boxCornerPrefab;
            box.edgePrefab = boxEdgePrefab;
            box.centerPrefab = boxCenterPrefab;

            // 容量按周围环境自动计算（本体 + 相邻有效格，固定 8 方向），覆盖 JSON 里记录的 capacity。
            box.capacity = BoxItem.ComputeCapacity(this, box.colMin, box.rowMin, box.colMax, box.rowMax);
            if (box.colorIds.Length != box.capacity)
                Debug.LogWarning("[PixelGroup] 箱子 " + go.name + " 的 colorIds 数量(" + box.colorIds.Length +
                    ") 与自动计算的容量(" + box.capacity + ") 不一致，运行时按较小值处理。");

            box.BuildVisual(this, config);

            // 立即占用 boxGrid（供后续箱子的容量计算看到本箱本体）；ApplyBoxes 末尾的 RebuildGrid 会重建权威表。
            if (boxGrid != null)
            {
                for (int r = box.rowMin; r <= box.rowMax; r++)
                    for (int c = box.colMin; c <= box.colMax; c++)
                        if (IsInRange(c, r))
                            boxGrid[c, r] = true;
            }

            return box;
        }

        /// <summary>箱子开箱：清除其本体格占用，并登记「释放中」计数（由 BoxItem.TryOpen 调用）。</summary>
        public void OnBoxOpened(BoxItem box)
        {
            if (box == null)
                return;
            for (int r = box.rowMin; r <= box.rowMax; r++)
                for (int c = box.colMin; c <= box.colMax; c++)
                    if (IsInRange(c, r))
                        boxGrid[c, r] = false;
            releasingBoxesCount++;
        }

        /// <summary>箱子释放完成（动画结束）：解除「释放中」计数（由 BoxItem 开箱动画收尾调用）。</summary>
        public void OnBoxReleaseFinished(BoxItem box)
        {
            releasingBoxesCount = Mathf.Max(0, releasingBoxesCount - 1);
        }

        /// <summary>
        /// 检查所有未开箱箱子并逐个尝试开箱（§7.3）：按 (rowMin 升序, colMin 升序) 串行判定，
        /// 前箱占格影响后箱，不满足则跳过；动画并行。开箱产生的格变化统一刷新一次暴露。
        /// </summary>
        public void TryOpenBoxes()
        {
            if (boxes == null || boxes.Count == 0)
                return;
            if (grid == null)
                RebuildGrid();

            var sorted = new List<BoxItem>(boxes);
            sorted.Sort((a, b) =>
            {
                if (a == null || b == null)
                    return 0;
                int rc = a.rowMin.CompareTo(b.rowMin);
                if (rc != 0)
                    return rc;
                return a.colMin.CompareTo(b.colMin);
            });

            bool anyOpened = false;
            foreach (var box in sorted)
            {
                if (box == null || box.opened)
                    continue;
                if (box.TryOpen())
                    anyOpened = true;
            }

            if (anyOpened)
                RefreshExposed();
        }

        /// <summary>
        /// 收集用于容器规划的 (层, 颜色) 列表：静态像素（含轨道上的初始像素）+ 管道将生成的像素。
        /// 编辑器与运行时均可调用（不依赖 grid 重建）。
        /// </summary>
        public List<(int layer, int color)> CollectPlanningPixels()
        {
            var pixels = new List<(int, int)>();
            foreach (var it in GetComponentsInChildren<PixelItem>())
            {
                if (it != null && IsInRange(it.gridX, it.gridZ))
                    pixels.Add((it.gridZ, it.colorId));
            }

            foreach (var pipe in GetComponentsInChildren<PipeItem>())
            {
                if (pipe == null || pipe.points == null || pipe.points.Count < 2 || pipe.colors == null)
                    continue;
                int track = PipeItem.CountTrackCells(pipe.points, columns, TotalRows);
                if (track <= 0)
                    continue;
                var pipeCell = PipeItem.GetPipeCell(pipe.points);
                int layer = Mathf.Clamp(pipeCell.y, 0, TotalRows - 1);
                foreach (int c in pipe.colors)
                    for (int k = 0; k < track; k++)
                        pixels.Add((layer, c));
            }

            // 箱子隐藏 Pixel：layer 取箱子 rowMin（最前排），颜色按 colorIds 逐个计入
            foreach (var box in GetComponentsInChildren<BoxItem>())
            {
                if (box == null || box.opened || box.colorIds == null)
                    continue;
                int layer = Mathf.Clamp(box.rowMin, 0, TotalRows - 1);
                int count = Mathf.Min(box.capacity, box.colorIds.Length);
                for (int i = 0; i < count; i++)
                    pixels.Add((layer, box.colorIds[i]));
            }

            return pixels;
        }
    }
}
