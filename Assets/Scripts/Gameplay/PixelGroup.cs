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

        [Tooltip("倍乘门预制体模板（需自带 GateItem 组件，并含本体 Mesh 子物体与倍数 Text 子物体）")]
        public GameObject gatePrefab;

        [Tooltip("箱子角格预制体（占一格，可视觉溢出边界）")]
        public GameObject boxCornerPrefab;

        [Tooltip("箱子边格预制体（占一格，可视觉溢出边界）")]
        public GameObject boxEdgePrefab;

        [Tooltip("箱子中心格预制体（占一格，可视觉溢出边界）")]
        public GameObject boxCenterPrefab;

        [Tooltip("2×2 箱子的**整体**预制体（根物体需自带 BoxItem 组件）。按**实际尺寸**制作（以自身原点居中），" +
                 "视觉放在子物体上；脚本只把它摆到箱子中心，**不缩放**。设了它之后 2×2 的箱子直接用它、不再按格拼接角/边/中心")]
        public GameObject boxWholePrefab;

        [Tooltip("地面升降台预制体模板（需自带 ElevatorItem 组件，并配置好 Frame/Door/HoleMask/Pit 视觉子节点）")]
        public GameObject elevatorPrefab;

        [Tooltip("冰冻组预制体模板（需自带 IceItem 组件，并含单元模板子物体与计数 Text 子物体）。每个冰组实例化一份")]
        public GameObject icePrefab;

        [Tooltip("木箱预制体模板（需自带 CrateItem 组件）。木箱的**全部视觉与表现参数**（角 / 边 / 中心格块、" +
                 "封条与钉子预制体及偏移、消失动画、被盖像素的恢复动画）都配在这个预制体的 CrateItem 上")]
        public GameObject cratePrefab;

        [Tooltip("默认地面材质（原始 Block_BG 材质；无升降台的关卡用它恢复地面，清除挖洞材质污染）")]
        public Material defaultGroundMaterial;

        /// <summary>运行时网格 [column, row]，row 0 为最前排（+Z），row = TotalRows-1 为后排（-Z，含尾部）</summary>
        [System.NonSerialized] public PixelItem[,] grid;

        /// <summary>墙体占用表 [column, row]：true = 该格被 WallItem 占据（作为障碍参与暴露与寻路）。</summary>
        [System.NonSerialized] public bool[,] wallGrid;

        /// <summary>管道占用表 [column, row]：true = 该格被 PipeItem 占据（作为障碍参与暴露与寻路）。</summary>
        [System.NonSerialized] public bool[,] pipeGrid;

        /// <summary>倍乘门门格表 [column, row]：该格属于哪道门（不在任何门上为 null）。
        /// **门格对闭合区域外的像素是障碍、对区域内的像素是唯一出口**（见 <see cref="IsGateBlockedFor"/>）：
        /// 区域内的像素必须穿门格才出得去，所以暴露 BFS 与寻路都必须为它们放行。</summary>
        [System.NonSerialized] public GateItem[,] gateGrid;

        /// <summary>该格是否落在某道门的闭合区域内（供「区域内像素必须走到门格才允许离场」的守卫用）。</summary>
        [System.NonSerialized] public bool[,] gateRegionMask;

        /// <summary>倍率图 [column, row]：该格所属各门倍数之积（不在任何区域内为 1）。嵌套即连乘。</summary>
        [System.NonSerialized] public int[,] gateMultiplier;

        /// <summary>冻结掩码 [column, row]：该格所在冰组尚未融化（计数 &gt; 0）。
        /// 冰格**不是**障碍——它只把组内像素「视为不暴露」，寻路与暴露 BFS 照常穿过。</summary>
        [System.NonSerialized] public bool[,] iceFrozenMask;

        /// <summary>箱子占用表 [column, row]：true = 该格被未开箱的 BoxItem 占据（作为障碍参与暴露与寻路）。</summary>
        [System.NonSerialized] public bool[,] boxGrid;

        /// <summary>木箱占用/遮盖表 [column, row]：true = 该格被未拆掉的木箱盖住。
        /// 与 <see cref="boxGrid"/> 同类 —— **是障碍**（整块矩形，含其中的空格），
        /// 于是木箱格天然被排除在同色连通块之外、也不会出描边。</summary>
        [System.NonSerialized] public bool[,] crateMask;

        /// <summary>运行时收集到的所有管道（重建 grid 时刷新）。</summary>
        [System.NonSerialized] public List<PipeItem> pipes = new List<PipeItem>();

        /// <summary>运行时收集到的所有倍乘门（重建 grid 时刷新）。</summary>
        [System.NonSerialized] public List<GateItem> gates = new List<GateItem>();

        /// <summary>运行时收集到的所有冰组（重建 grid 时刷新）。</summary>
        [System.NonSerialized] public List<IceItem> iceGroups = new List<IceItem>();

        /// <summary>运行时收集到的所有箱子（重建 grid 时刷新；含已开箱的，用 opened 区分）。</summary>
        [System.NonSerialized] public List<BoxItem> boxes = new List<BoxItem>();

        /// <summary>运行时收集到的所有升降台（重建 grid 时刷新）。</summary>
        [System.NonSerialized] public List<ElevatorItem> elevators = new List<ElevatorItem>();

        /// <summary>运行时收集到的所有木箱（重建 grid 时刷新；含已拆掉的，用 destroyed 区分）。</summary>
        [System.NonSerialized] public List<CrateItem> crates = new List<CrateItem>();

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
            crateMask = new bool[columns, TotalRows];
            gateGrid = new GateItem[columns, TotalRows];
            gateRegionMask = new bool[columns, TotalRows];
            gateMultiplier = new int[columns, TotalRows];
            iceFrozenMask = new bool[columns, TotalRows];
            for (int c = 0; c < columns; c++)
                for (int r = 0; r < TotalRows; r++)
                    gateMultiplier[c, r] = 1;
            pipes = new List<PipeItem>();
            boxes = new List<BoxItem>();
            elevators = new List<ElevatorItem>();
            gates = new List<GateItem>();
            iceGroups = new List<IceItem>();
            crates = new List<CrateItem>();

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

            // 升降台：区域不占格（其像素是普通网格像素，由分组提供），仅登记引用
            foreach (var elev in GetComponentsInChildren<ElevatorItem>())
            {
                if (elev == null)
                    continue;
                elev.group = this;
                elevators.Add(elev);
            }

            // 无升降台的关卡：恢复默认地面材质（清除之前升降台留下的挖洞材质污染）
            if (Application.isPlaying && elevators.Count == 0)
                RestoreDefaultGroundMaterial();

            // 倍乘门：登记引用 + 门格表。门格**不写 wallGrid / pipeGrid / boxGrid**（门不是障碍）
            foreach (var gate in GetComponentsInChildren<GateItem>())
            {
                if (gate == null)
                    continue;
                gate.group = this;
                gate.RefreshCells();   // 字段可能在 Inspector 里被改过，用最新的起终点
                gate.ResetMasks(columns, TotalRows);
                gates.Add(gate);

                foreach (var cell in gate.cells)
                {
                    if (IsInRange(cell.x, cell.y))
                        gateGrid[cell.x, cell.y] = gate;
                }
            }

            // 闭合区域 + 连乘倍率图
            for (int i = 0; i < gates.Count; i++)
            {
                var gate = gates[i];
                var region = GateRegion.ComputeRegion(columns, TotalRows, IsPermanentGateBarrier, gate.cells);
                int mult = Mathf.Max(1, gate.multiplier);

                foreach (var cell in region)
                {
                    if (!IsInRange(cell.x, cell.y))
                        continue;
                    gate.regionMask[cell.x, cell.y] = true;
                    gateRegionMask[cell.x, cell.y] = true;
                    gateMultiplier[cell.x, cell.y] *= mult;   // 嵌套 = 各门倍数连乘
                }
            }

            // 冰组：只登记引用。冰格**不写 wallGrid / pipeGrid / boxGrid**（冰不是障碍），
            // 它只通过 iceFrozenMask 把组内像素「视为不暴露」。
            // 冰面由各 IceItem 按**自己**的成员格独立生成（单色填充），所以这里既不需要全局归属表，
            // 也不需要变体图，枚举顺序无关紧要。
            foreach (var ice in GetComponentsInChildren<IceItem>())
            {
                if (ice == null)
                    continue;
                ice.group = this;
                ice.RefreshCells();   // 字段可能在 Inspector 里被改过，用最新的格列表
                iceGroups.Add(ice);
            }

            // 木箱：登记引用 + 本体格掩码（crateMask 由 RefreshCrateState 填）。
            // 与冰**相反**：木箱格是障碍（并入 IsBlocked），所以整块矩形连同其中的空格都占格。
            foreach (var crate in GetComponentsInChildren<CrateItem>())
            {
                if (crate == null)
                    continue;
                crate.group = this;
                crate.RefreshCells();   // 字段可能在 Inspector 里被改过，用最新的区域
                crates.Add(crate);
            }

            RefreshIceState();
            RefreshCrateState();
        }

        /// <summary>
        /// 倍乘门闭合区域求解用的「永久障碍」：墙 ∪ 管道自身格。
        /// **不含箱子**：箱子会开箱、会消失，不能当永久围栏；不把它算障碍，
        /// 区域内箱子自身的格才落在区域内、箱子释放的像素才可能被正确计入倍乘（见 <see cref="GateRegion"/>）。
        /// </summary>
        private bool IsPermanentGateBarrier(int col, int row)
        {
            return IsWall(col, row) || IsPipe(col, row);
        }

        /// <summary>找到地面 Renderer（优先 Block_BG，退 BG），若非默认 BG 材质则换回，用于无升降台关卡恢复地面外观。</summary>
        public void RestoreDefaultGroundMaterial()
        {
            if (defaultGroundMaterial == null)
                return;
            var r = ElevatorItem.FindGroundRenderer();
            if (r != null && r.sharedMaterial != defaultGroundMaterial)
                r.sharedMaterial = defaultGroundMaterial;
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

        /// <summary>该格是否被未拆掉的木箱盖住（木箱本体整块矩形，含其中的空格）。</summary>
        public bool IsCrateCell(int col, int row)
        {
            if (crateMask == null)
                return false;
            if (!IsInRange(col, row))
                return false;
            return crateMask[col, row];
        }

        /// <summary>该格是否为障碍（墙体、管道、未开箱的箱子或未拆掉的木箱）。</summary>
        public bool IsBlocked(int col, int row) => IsWall(col, row) || IsPipe(col, row) || IsBox(col, row) || IsCrateCell(col, row);

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
        /// 暴露判定里，相邻格 (nc,nr) 能否算作「连通首排的空格」供 (c,r) 借光。
        ///
        /// 与旧逻辑（直接读 reachableEmpty）的唯一区别：**门格只对「该门闭合区域内」的格子作数**。
        /// 门对区域外的像素等同墙，所以门框外侧紧邻的像素不再因为贴着门格而点亮；
        /// 区域内的像素照旧靠门格连到首排（否则整个闭环区域都不可点）。
        /// 口径与 <see cref="IsGateBlockedFor"/> 一致。
        ///
        /// 注：`reachableEmpty` 本身仍允许流经门格（区域内部的格必须靠它才连得上首排），
        /// 而「区域外的格能不能借门格的光」由这里挡住 —— 两件事分开判，互不干扰。
        /// </summary>
        private bool NeighbourReachableForExposure(int c, int r, int nc, int nr, bool[,] reachableEmpty)
        {
            if (!reachableEmpty[nc, nr])
                return false;

            var gate = GateAt(nc, nr);
            if (gate == null)
                return true;                                    // 不是门格：与旧逻辑一致
            return gate.regionMask != null && gate.regionMask[c, r];
        }

        /// <summary>该格是否是某道倍乘门的门格。</summary>
        public bool IsGateCell(int col, int row)
        {
            return GateAt(col, row) != null;
        }

        /// <summary>该格所属的倍乘门（不是门格时返回 null）。</summary>
        public GateItem GateAt(int col, int row)
        {
            if (gateGrid == null || !IsInRange(col, row))
                return null;
            return gateGrid[col, row];
        }

        /// <summary>该格是否落在某道门的闭合区域内。</summary>
        public bool IsInGateRegion(int col, int row)
        {
            if (gateRegionMask == null || !IsInRange(col, row))
                return false;
            return gateRegionMask[col, row];
        }

        /// <summary>该格的倍率（所属各门倍数之积；不在任何区域内为 1）。嵌套门即连乘。</summary>
        public int GateMultiplierAt(int col, int row)
        {
            if (gateMultiplier == null || !IsInRange(col, row))
                return 1;
            return gateMultiplier[col, row];
        }

        /// <summary>
        /// 该格是否「在某道门的区域内、却不在那道门的门格上」——这种格不允许像素直接离场，
        /// 必须继续走到门格才允许（见 CrowdBufferZone.CanExit 的守卫；否则区域深处的像素会原地飞出去、
        /// 越过门格却不占用它，裂变不触发、嵌套的外门也会被整层跳过）。
        /// 返回 true 时 out gate 给出对应的门。
        /// </summary>
        public bool MustWalkToGate(int col, int row, out GateItem gate)
        {
            gate = null;
            if (gates == null || !IsInRange(col, row))
                return false;

            for (int i = 0; i < gates.Count; i++)
            {
                var g = gates[i];
                if (g == null || g.regionMask == null)
                    continue;
                if (!g.regionMask[col, row])
                    continue;                                       // 不在这道门的区域内
                if (g.cellMask != null && g.cellMask[col, row])
                    continue;                                       // 已在门格上：允许离场

                gate = g;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 这批像素「允许穿过」的门集合 = 像素所在格落在该门**闭合区域内**的那些门。
        ///
        /// 口径（已与用户核对）：**倍乘门对闭合区域外的像素等同墙** —— 区域外的像素不许走门格、
        /// 不许靠门格连到首排，也因此不会从门格上蹭到倍乘（`TakeGateBudget` 只看门格、不校验来路，
        /// 所以「不许进门格」才是拦点）。区域内的像素必须穿过门格才能出去，所以必须拿得到这份通行证。
        ///
        /// 为什么按**整组**判定而不是逐颗：一次点击移出的是同一组同色连通像素、一个提取批次也是整组一起走，
        /// 而门两侧不可能同色连通（门格上没有像素），所以整组必然同侧。组内只要有一颗在区域内，
        /// 整组就都拿这道门的通行证 —— 否则组里先走出去的那几颗会让剩下的卡在门里。
        /// </summary>
        public HashSet<GateItem> CollectPassGates(IEnumerable<PixelItem> pixels)
        {
            var pass = new HashSet<GateItem>();
            if (pixels == null || gates == null || gates.Count == 0)
                return pass;

            foreach (var p in pixels)
            {
                if (p == null || !IsInRange(p.gridX, p.gridZ))
                    continue;

                for (int i = 0; i < gates.Count; i++)
                {
                    var g = gates[i];
                    if (g == null || g.regionMask == null)
                        continue;
                    if (g.regionMask[p.gridX, p.gridZ])
                        pass.Add(g);
                }
            }
            return pass;
        }

        /// <summary>
        /// 该格对「持有 <paramref name="passGates"/> 通行证的像素」是否被倍乘门挡住。
        /// 不是门格 → 不挡；是门格且该门在通行证里 → 不挡；其余（含区域外像素遇到门格）→ 挡。
        /// <paramref name="passGates"/> 传 null 表示「调用方不区分来路」，一律不挡（= 旧行为）。
        /// </summary>
        public bool IsGateBlockedFor(int col, int row, HashSet<GateItem> passGates)
        {
            var gate = GateAt(col, row);
            if (gate == null)
                return false;                                   // 不是门格
            if (passGates == null)
                return false;                                   // 无门上下文：保持旧行为
            return !passGates.Contains(gate);
        }

        /// <summary>落在该门闭合区域内的静态网格像素数（供 Inspector 显示与校验提示）。</summary>
        public int CountPixelsInRegion(GateItem gate)
        {
            if (gate == null || gate.regionMask == null || grid == null)
                return 0;

            int n = 0;
            for (int c = 0; c < columns; c++)
                for (int r = 0; r < TotalRows; r++)
                    if (gate.regionMask[c, r] && grid[c, r] != null)
                        n++;
            return n;
        }

        /// <summary>网格上是否已经没有任何像素（箱子隐藏像素 / 升降台地下像素不算：它们还没落到格子上）。
        /// 想判断「场上是否还有像素**要来了**」请配 <see cref="HasPendingProducers"/> —— 传送带的空场加速就是两者一起判。</summary>
        public bool IsGridEmpty()
        {
            if (grid == null)
                return true;

            for (int c = 0; c < columns; c++)
                for (int r = 0; r < TotalRows; r++)
                    if (grid[c, r] != null)
                        return false;
            return true;
        }

        /// <summary>
        /// 场上是否还有**待产出**的像素：管道还有波次、木箱还有未释放的隐藏像素、升降台还有未升起的组。
        ///
        /// 与 <see cref="IsGridEmpty"/> 的区别：这三类像素在产出之前都不在 <c>grid</c> 里（管道在生成时才写 grid，
        /// 木箱隐藏像素 active=false，升降台地下像素是哨兵坐标 (-1,-1)），所以「grid 空了」并不等于「场上没有像素要来了」。
        /// 传送带空场加速用它兜住「刚点掉封路像素、生产者还没补位」的那一帧——否则会在关卡中段提前进入加速，
        /// 而加速是本关内不回退的闩锁。倍乘门不算：分身不写 grid，且其本体离开网格后玩家已无后续操作。
        /// </summary>
        public bool HasPendingProducers()
        {
            if (pipes != null)
                for (int i = 0; i < pipes.Count; i++)
                    if (pipes[i] != null && pipes[i].HasRemainingWaves)
                        return true;

            if (boxes != null)
                for (int i = 0; i < boxes.Count; i++)
                    if (boxes[i] != null && boxes[i].hiddenPixels != null && boxes[i].hiddenPixels.Count > 0)
                        return true;

            if (elevators != null)
                for (int i = 0; i < elevators.Count; i++)
                    if (elevators[i] != null && !elevators[i].IsDone)
                        return true;

            return false;
        }

        // ===== 冰冻组 =====

        /// <summary>该格是否被冻住（所在冰组尚未融化）。</summary>
        public bool IsFrozenCell(int col, int row)
        {
            if (iceFrozenMask == null || !IsInRange(col, row))
                return false;
            return iceFrozenMask[col, row];
        }

        /// <summary>落在该冰组成员格上的静态网格像素数（供 Inspector 显示）。</summary>
        public int CountPixelsInIce(IceItem ice)
        {
            if (ice == null || grid == null)
                return 0;

            int n = 0;
            foreach (var cell in ice.CellSet)
                if (IsInRange(cell.x, cell.y) && grid[cell.x, cell.y] != null)
                    n++;
            return n;
        }

        /// <summary>不在任何**冻结中**冰组内的像素数。为 0 且还有冰没融化 = 没有可点的像素来推进计数（死锁提示用）。</summary>
        public int CountUnfrozenPixels()
        {
            if (grid == null)
                return 0;

            int n = 0;
            for (int c = 0; c < columns; c++)
                for (int r = 0; r < TotalRows; r++)
                    if (grid[c, r] != null && !IsFrozenCell(c, r))
                        n++;
            return n;
        }

        /// <summary>
        /// 刷新冰冻状态：初始化尚未初始化的计数、按融化情况重填冻结掩码、把冻结标志写到各像素上。
        ///
        /// **只初始化、不重置**：RebuildGrid 在开箱 / 升降台推进时也会被调用（见 OnBoxOpened 等），
        /// 若在这里 ResetCount 会凭空解冻。复位只发生在 <see cref="SpawnIce"/>（新关卡导入）。
        /// </summary>
        public void RefreshIceState()
        {
            if (iceFrozenMask == null || iceGroups == null || grid == null)
                return;

            for (int c = 0; c < columns; c++)
                for (int r = 0; r < TotalRows; r++)
                    iceFrozenMask[c, r] = false;

            for (int i = 0; i < iceGroups.Count; i++)
            {
                var ice = iceGroups[i];
                if (ice == null)
                    continue;
                if (ice.remaining < 0)
                    ice.ResetCount();
                if (ice.Melted)
                    continue;

                foreach (var cell in ice.CellSet)
                    if (IsInRange(cell.x, cell.y))
                        iceFrozenMask[cell.x, cell.y] = true;
            }

            for (int c = 0; c < columns; c++)
                for (int r = 0; r < TotalRows; r++)
                {
                    var item = grid[c, r];
                    if (item != null)
                        item.SetFrozen(iceFrozenMask[c, r]);
                }
        }

        /// <summary>
        /// 记下「这次点击发生**之前**」每个冰组是否已暴露。必须在本次点击引起的
        /// <see cref="RefreshExposed"/> **之前**调用 —— <see cref="NotifyClickMovedOut"/> 要用它判断
        /// 「暴露才开始融化」。
        /// </summary>
        public void CaptureIceExposedSnapshot()
        {
            if (iceGroups == null)
                return;

            for (int i = 0; i < iceGroups.Count; i++)
            {
                var ice = iceGroups[i];
                if (ice != null)
                    ice.exposedAtCapture = ice.hasExposedMember;
            }
        }

        /// <summary>
        /// 一次成功的「点击移出」→ 每个冰组的计数各 -1。
        /// 计数的粒度是**点击**而不是像素数：一次点击不管移出几颗，都只消耗一次；
        /// 范围是**全局**的：任意一次有效点击都推进所有冰组（不限于被点的那组里的像素）。
        /// 点击无效（没通过 CanReachFront 校验）时不会调到这里。
        ///
        /// 「暴露才开始融化」用**点击前**的暴露状态判断，于是同时满足两条：
        ///   · 还没暴露时点击不消耗；
        ///   · 「使之暴露的那一次点击」也不消耗 —— 那一刻按点击前的状态它仍未暴露。
        ///
        /// 只有真有冰组融化到 0 时才重建（冰面 / 冻结掩码 / 暴露），避免每次点击都跑全网格刷新。
        /// </summary>
        public void NotifyClickMovedOut()
        {
            if (iceGroups == null || iceGroups.Count == 0)
                return;

            bool anyMelted = false;
            for (int i = 0; i < iceGroups.Count; i++)
            {
                var ice = iceGroups[i];
                if (ice == null)
                    continue;

                if (ice.meltOnlyWhenExposed && !ice.exposedAtCapture)
                    continue;                    // 未暴露（或本次点击才让它暴露）：这次不消耗

                if (ice.ConsumeOne())
                {
                    anyMelted = true;
                    ice.PlayMeltEffect();        // 刚化开：生成融化特效 + 播音效（冰上自己配 tag）
                }
                else
                    ice.UpdateDisplay();         // 计数变了（或已归 0）：刷新数字显示
            }

            if (!anyMelted)
                return;

            RefreshIceState();
            for (int i = 0; i < iceGroups.Count; i++)
                if (iceGroups[i] != null)
                    iceGroups[i].BuildVisual(this);
            RefreshExposed();   // 冰化开后组内像素要立刻恢复可点
        }

        // ===== 木箱 =====

        /// <summary>
        /// 刷新木箱状态：按未拆掉的木箱重填 <see cref="crateMask"/>，并把「被盖住」标志写回各像素
        /// （关渲染器 / 恢复显示）。
        ///
        /// **只按 destroyed 计算，不重置计数**：计数只由 <see cref="CrateItem.RegisterAdjacentMoveOut"/>
        /// 推进；复位只发生在 <see cref="SpawnCrate"/>（新关卡导入）与 RebuildGrid 重新登记之后
        /// （新建的 CrateItem 计数天然是 0）。
        /// destroyed 的木箱在消失动画的**放大阶段**内仍计入掩码（见 <see cref="CrateItem.IsHidingForVanish"/>）。
        /// </summary>
        public void RefreshCrateState()
        {
            if (crateMask == null || grid == null)
                return;

            for (int c = 0; c < columns; c++)
                for (int r = 0; r < TotalRows; r++)
                    crateMask[c, r] = false;

            if (crates != null)
            {
                for (int i = 0; i < crates.Count; i++)
                {
                    var crate = crates[i];
                    // destroyed 的木箱在**消失动画的「放大」阶段**内仍算盖住自己的格子（见 CrateItem.IsHidingForVanish）：
                    // 于是那段时间像素不露头、也照旧点不到，缩小一开始才由 CrateItem 撤销这个窗口并重算。
                    if (crate == null || (crate.destroyed && !crate.IsHidingForVanish))
                        continue;

                    foreach (var cell in crate.Cells)
                        if (IsInRange(cell.x, cell.y))
                            crateMask[cell.x, cell.y] = true;
                }
            }

            for (int c = 0; c < columns; c++)
                for (int r = 0; r < TotalRows; r++)
                {
                    var item = grid[c, r];
                    if (item != null)
                        item.SetCovered(crateMask[c, r]);
                }
        }

        /// <summary>落在该木箱本体格上的像素数（供 Inspector 显示）。</summary>
        public int CountPixelsUnderCrate(CrateItem crate)
        {
            if (crate == null || grid == null)
                return 0;

            int n = 0;
            foreach (var cell in crate.Cells)
                if (IsInRange(cell.x, cell.y) && grid[cell.x, cell.y] != null)
                    n++;
            return n;
        }

        /// <summary>
        /// 一次成功的「点击移出」之后：与本次移出的像素上下左右（4 邻）相接的木箱各计 1 次。
        ///
        /// **同组同时移出算一次**：同一次点击移出的是一组同色像素，无论组内有多少颗挨着同一个木箱，
        /// 该木箱都只计 1 次（这里对每个木箱逐次判断「有没有挨着」而不是累计格数）。
        /// 计满的木箱当场拆掉 —— 占格与遮盖立刻撤销（<see cref="RefreshCrateState"/>），
        /// 底下像素恢复可见并按正常规则重新判定暴露。
        ///
        /// 返回**是否有木箱被拆掉**（调用方据此补一次整体描边刷新）。
        /// </summary>
        public bool NotifyPixelsMovedOut(List<PixelItem> moved)
        {
            if (crates == null || crates.Count == 0 || moved == null || moved.Count == 0)
                return false;

            // 本次移出像素的 4 邻格。用它们**移出前**的网格坐标：grid 里的引用已被清空，
            // 但 gridX / gridZ 字段没变，就是它们刚离开的位置。
            var touched = new HashSet<Vector2Int>();
            for (int i = 0; i < moved.Count; i++)
            {
                var it = moved[i];
                if (it == null)
                    continue;
                touched.Add(new Vector2Int(it.gridX - 1, it.gridZ));
                touched.Add(new Vector2Int(it.gridX + 1, it.gridZ));
                touched.Add(new Vector2Int(it.gridX, it.gridZ - 1));
                touched.Add(new Vector2Int(it.gridX, it.gridZ + 1));
            }

            bool anyDestroyed = false;
            for (int i = 0; i < crates.Count; i++)
            {
                var crate = crates[i];
                if (crate == null || crate.destroyed)
                    continue;

                bool adjacent = false;
                foreach (var cell in crate.Cells)
                {
                    if (touched.Contains(cell))
                    {
                        adjacent = true;
                        break;
                    }
                }
                if (!adjacent)
                    continue;

                if (crate.RegisterAdjacentMoveOut())
                    anyDestroyed = true;
            }

            if (!anyDestroyed)
                return false;

            RefreshExposed();   // 内部先 RefreshCrateState（撤占格 + 恢复渲染），再重算暴露
            return true;
        }

        /// <summary>
        /// 校验所有倍乘门（调用前应先 <see cref="RebuildGrid"/> 让掩码与区域刷新）。通过返回 null，否则返回错误描述。
        /// 查三件事：① 线段轴对齐；② 每道门都围出了非空闭合区域；③ 门格互不重叠
        /// （重叠时分身该算哪道门无定义）。**创建门时不做这个检查**，只有数量检查与生成 Containers 才查。
        /// </summary>
        public string ValidateGates()
        {
            if (gates == null || gates.Count == 0)
                return null;

            var seen = new HashSet<Vector2Int>();
            for (int i = 0; i < gates.Count; i++)
            {
                var gate = gates[i];
                if (gate == null)
                    continue;

                if (!gate.IsValid(out string segErr))
                    return "倍乘门 " + gate.name + "：" + segErr;

                gate.RefreshCells();
                foreach (var cell in gate.cells)
                {
                    if (!seen.Add(cell))
                        return "倍乘门门格重叠：格 (" + cell.x + "," + cell.y + ") 被多道门同时占用，" +
                               "重叠时分身属于哪道门没有定义，请错开各门的范围。";
                }

                if (gate.RegionCellCount() == 0)
                    return "倍乘门 " + gate.name + " 没有围出闭合区域。\n" +
                           "请用墙（或管道）配合这道门把要倍乘的像素围成一个封闭区间——" +
                           "门本身算围栏的一段，区域内不能有别的出口，否则无法确定有多少像素会经过这道门。";
            }
            return null;
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

            // 冰冻掩码先刷新到最新：下面同色连通块的扩散要「碰到冰冻中的冰格就停」，
            // 靠的就是 iceFrozenMask。放在这里是为了不依赖调用顺序（调用方可能刚改过冰组、刚融化）。
            RefreshIceState();

            // 木箱掩码同理先刷新：crateMask 是障碍（并入 IsBlocked），下面所有
            // 「跳过 IsBlocked」的分支都依赖它是最新的，否则刚被拆掉的木箱会继续挡住它的像素。
            RefreshCrateState();

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

            // 1. 标记「直接暴露」格子：首排，或四周任一紧邻格为「连通首排的空格」（墙体/管道/活跃管道覆盖视为占用）。
            //    门格另有一条：只对**该门闭合区域内**的格子作数 —— 区域外的像素把门格当墙，
            //    于是「门框外侧紧邻的像素」不再因为贴着门格而点亮（口径见 IsGateBlockedFor）。
            var directlyExposed = new bool[cols, totalRows];
            for (int c = 0; c < cols; c++)
            {
                for (int r = 0; r < totalRows; r++)
                {
                    if (grid[c, r] == null || IsBlocked(c, r))
                        continue;
                    directlyExposed[c, r] =
                        r == 0 ||                                            // 前方：出口（第一排）
                        (r - 1 >= 0 && NeighbourReachableForExposure(c, r, c, r - 1, reachableEmpty)) ||
                        (r + 1 < totalRows && NeighbourReachableForExposure(c, r, c, r + 1, reachableEmpty)) ||
                        (c - 1 >= 0 && NeighbourReachableForExposure(c, r, c - 1, r, reachableEmpty)) ||
                        (c + 1 < cols && NeighbourReachableForExposure(c, r, c + 1, r, reachableEmpty));
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
                    // 冰冻中的冰格**不参与**同色连通块：它既不做块的种子、也不能被扩散穿过
                    // （两条守卫缺一不可）。于是「只有隔着冰才连到外面的同色像素」不会被整块点亮。
                    if (grid[c, r] == null || IsBlocked(c, r) || visited[c, r] || IsFrozenCell(c, r))
                        continue;

                    int color = grid[c, r].colorId;
                    var cells = new List<Vector2Int>();
                    bool hasExposed = false;
                    bool hasExposedQuestion = false;
                    var queue = new Queue<Vector2Int>();
                    queue.Enqueue(new Vector2Int(c, r));
                    visited[c, r] = true;

                    while (queue.Count > 0)
                    {
                        var cur = queue.Dequeue();
                        cells.Add(cur);
                        if (directlyExposed[cur.x, cur.y])
                        {
                            hasExposed = true;
                            var curItem = grid[cur.x, cur.y];
                            if (curItem != null && curItem.isQuestion && !curItem.revealed)
                                hasExposedQuestion = true;
                        }

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
                            if (IsFrozenCell(nx, nz))
                                continue;   // 冰冻中的冰格 = 块边界，不扩散进去（见上面的说明）

                            visited[nx, nz] = true;
                            queue.Enqueue(new Vector2Int(nx, nz));
                        }
                    }

                    // 逐格判定激活：
                    // 未揭晓问号格：仅当块内存在「直接暴露的未揭晓问号格」才激活（问号不因相邻非问号暴露而揭晓）
                    // 已揭晓问号格 / 非问号格：块内任一格直接暴露即激活（原逻辑，揭晓后等同普通像素）
                    foreach (var cell in cells)
                    {
                        var it = grid[cell.x, cell.y];
                        if (it == null)
                            continue;
                        bool isStillQuestion = it.isQuestion && !it.revealed;
                        bool act = isStillQuestion ? hasExposedQuestion : hasExposed;
                        if (act)
                            active[cell.x, cell.y] = true;
                    }
                }
            }

            // 3. 冰组是否已暴露（供「暴露才开始消耗」门槛与计数数字的显隐用）。
            //    冰格已被排除在同色连通块之外（见上面两处守卫），所以它在 active 里天然为 false、
            //    不会出描边，不需要额外遮盖。这里改用 directlyExposed 判断 ——
            //    「冰的位置暴露」= 冰组里至少有一格紧邻通向出口的空格（或就在首排）。
            if (iceGroups != null && iceGroups.Count > 0)
            {
                for (int i = 0; i < iceGroups.Count; i++)
                {
                    var ice = iceGroups[i];
                    if (ice == null)
                        continue;

                    bool hasExposed = false;
                    foreach (var cell in ice.CellSet)
                    {
                        if (!IsInRange(cell.x, cell.y))
                            continue;
                        if (directlyExposed[cell.x, cell.y])
                        {
                            hasExposed = true;
                            break;
                        }
                    }
                    ice.hasExposedMember = hasExposed;
                }

                // 暴露状态刚算完 → 顺带刷新计数数字的显隐。
                // 必须在这里刷：显隐读的就是 hasExposedMember，而勾了「暴露才开始消耗」时，
                // **暴露的那一刻**就要把数字显示出来（而不是等到第一次消耗）——
                // 让它暴露的那次点击既不消耗、过去也不刷新，于是数字一直不出现。
                for (int i = 0; i < iceGroups.Count; i++)
                {
                    var ice = iceGroups[i];
                    if (ice != null)
                        ice.UpdateDisplay();
                }
            }

            // 4. 应用到各像素
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

        /// <summary>清空所有 GateItem 子物体（供关卡重载时重建倍乘门）。</summary>
        public void ClearGates()
        {
            var items = GetComponentsInChildren<GateItem>();
            for (int i = items.Length - 1; i >= 0; i--)
            {
                var g = items[i];
                if (g == null)
                    continue;
                g.transform.SetParent(null, true);
                if (Application.isPlaying)
                    Destroy(g.gameObject);
                else
                    DestroyImmediate(g.gameObject);
            }

            // 置空而不是清零：倍率「不在区域内 = 1」，用 0 填充会在重建前被读成倍率 0。
            // 三个访问器（GateAt / IsInGateRegion / GateMultiplierAt）都已对 null 做了兜底。
            gateGrid = null;
            gateRegionMask = null;
            gateMultiplier = null;
            gates = new List<GateItem>();
        }

        /// <summary>清空所有 IceItem 子物体（供关卡重载时重建冰组）。</summary>
        public void ClearIces()
        {
            var items = GetComponentsInChildren<IceItem>();
            for (int i = items.Length - 1; i >= 0; i--)
            {
                var ice = items[i];
                if (ice == null)
                    continue;
                ice.Clear();
                ice.transform.SetParent(null, true);
                if (Application.isPlaying)
                    Destroy(ice.gameObject);
                else
                    DestroyImmediate(ice.gameObject);
            }

            iceFrozenMask = null;
            iceGroups = new List<IceItem>();
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
        }

        /// <summary>
        /// 清空所有 ElevatorItem 及其视觉部件（供关卡重载时重建升降台）。
        /// 升降台的地下像素（active、哨兵坐标 -1,-1）已由 ClearPixels 销毁，这里只销毁升降台本体。
        /// </summary>
        public void ClearElevators()
        {
            var items = GetComponentsInChildren<ElevatorItem>();
            for (int i = items.Length - 1; i >= 0; i--)
            {
                var e = items[i];
                if (e == null)
                    continue;
                e.transform.SetParent(null, true);
                if (Application.isPlaying)
                    Destroy(e.gameObject);
                else
                    DestroyImmediate(e.gameObject);
            }
            elevators = new List<ElevatorItem>();
        }

        /// <summary>
        /// 清空所有 CrateItem 及其拼接视觉（供关卡重载时重建木箱）。
        /// **不销毁任何像素**：木箱盖住的像素是普通网格像素（不像箱子的隐藏像素），
        /// 它们由 ClearPixels 统一处理 —— 与 ClearElevators 同类。
        /// </summary>
        public void ClearCrates()
        {
            var items = GetComponentsInChildren<CrateItem>();
            for (int i = items.Length - 1; i >= 0; i--)
            {
                var crate = items[i];
                if (crate == null)
                    continue;
                crate.transform.SetParent(null, true);
                if (Application.isPlaying)
                    Destroy(crate.gameObject);
                else
                    DestroyImmediate(crate.gameObject);
            }
            crateMask = new bool[columns, TotalRows];
            crates = new List<CrateItem>();
        }

        /// <summary>
        /// 在 PixelGroup 下动态创建一个 WallItem（不依赖预制体，用 new GameObject + AddComponent），
        /// 并调用其 BuildVisual 用角/边/端点/独立 1×1 四类预制体拼接墙体实体（运行时可视化）。
        /// closed = true 时额外补首尾闭合段。
        /// </summary>
        public WallItem SpawnWall(IList<Vector2> points, bool closed = false)
        {
            var go = new GameObject("Wall_" + (transform.childCount + 1));
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;

            var wall = go.AddComponent<WallItem>();
            wall.points = new List<Vector2>(points);
            wall.closed = closed;
            wall.BuildVisual(this);
            return wall;
        }

        /// <summary>在指定格子生成一个 PixelItem 并应用颜色材质（供运行时关卡加载使用）。PixelItem 组件来自预制体，不再动态创建。</summary>
        public PixelItem SpawnPixel(int col, int row, int colorId, ColorConfig config, bool scaleZero = false, bool isQuestion = false)
        {
            if (pixelPrefab == null)
            {
                Debug.LogError("[PixelGroup] pixelPrefab 为空，无法生成像素（请挂 Block 预制体，需自带 PixelItem 组件）。");
                return null;
            }

            GameObject go = PrefabSpawner.Instantiate(pixelPrefab, transform);
            if (go == null)
                return null;
            go.name = "Pixel_" + row + "_" + col;
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
            item.isQuestion = isQuestion;
            item.RefreshQuestionObject();   // Awake 时 isQuestion 尚未赋值（仍是预制体默认值），此处补刷新问号物体显隐
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

            string pipeName = "Pipe_" + (transform.childCount + 1);   // 先取名，避免实例化后再数子物体多算一个
            var go = PrefabSpawner.Instantiate(pipePrefab, transform);
            if (go == null)
                return null;
            go.name = pipeName;

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
        /// 在 PixelGroup 下动态创建一道倍乘门（用 gatePrefab 实例化），并摆放可见表现
        /// （根定位到整段中心、本体网格按格数缩放、数字显示 x{倍数}）。
        /// 起终点为网格坐标（x = 列 col，y = 行 row）。
        /// </summary>
        public GateItem SpawnGate(Vector2 start, Vector2 end, int multiplier)
        {
            if (gatePrefab == null)
            {
                Debug.LogError("[PixelGroup] gatePrefab 为空，无法生成倍乘门（请指定自带 GateItem 组件的预制体）。");
                return null;
            }

            string gateName = "Gate_" + (transform.childCount + 1);   // 先取名，避免实例化后再数子物体多算一个
            var go = PrefabSpawner.Instantiate(gatePrefab, transform);
            if (go == null)
                return null;
            go.name = gateName;

            var gate = go.GetComponent<GateItem>();
            if (gate == null)
            {
                Debug.LogError("[PixelGroup] 预制体 " + gatePrefab.name + " 缺少 GateItem 组件。");
                if (Application.isPlaying)
                    Destroy(go);
                else
                    DestroyImmediate(go);
                return null;
            }

            gate.start = start;
            gate.end = end;
            gate.multiplier = Mathf.Max(1, multiplier);
            gate.group = this;
            gate.BuildVisual(this);
            return gate;
        }

        /// <summary>
        /// 在 PixelGroup 下实例化一个冰组（每个冰组一份 icePrefab）。参数取 <see cref="LevelData.IceGroupData"/>，
        /// 与 <see cref="SpawnBox"/> / <see cref="SpawnElevator"/> 一致 —— 编辑器创建与关卡 JSON 导入共用同一条路径，
        /// 字段（含计数数字的偏移与放大倍数）不会两头漏配。
        /// 冰组**不清除**格上的像素——冰下面本来就要有像素——所以没有快照 / 还原一说。
        /// </summary>
        public IceItem SpawnIce(LevelData.IceGroupData data)
        {
            if (data == null)
                return null;

            if (icePrefab == null)
            {
                Debug.LogError("[PixelGroup] icePrefab 为空，无法生成冰组（请指定自带 IceItem 组件的预制体）。");
                return null;
            }

            string iceName = "Ice_" + (transform.childCount + 1);   // 先取名，避免实例化后再数子物体多算一个
            var go = PrefabSpawner.Instantiate(icePrefab, transform);
            if (go == null)
                return null;
            go.name = iceName;

            var ice = go.GetComponent<IceItem>();
            if (ice == null)
            {
                Debug.LogError("[PixelGroup] 预制体 " + icePrefab.name + " 缺少 IceItem 组件。");
                if (Application.isPlaying)
                    Destroy(go);
                else
                    DestroyImmediate(go);
                return null;
            }

            // 显式写一遍所有来自数据的字段：预制体上可能留着旧的序列化值，不写就会被它盖掉。
            // JSON 是外部输入，这两个值在这里兜底（旧 JSON 没有字段时取默认；fontScale 为 0 会让字看不见）。
            ice.cells = (data.cells != null) ? new List<Vector2>(data.cells) : new List<Vector2>();
            ice.freezeCount = Mathf.Max(1, data.count);
            ice.meltOnlyWhenExposed = data.meltWhenExposed;
            ice.countOffset = data.countOffset;
            ice.countFontScale = data.fontScale > 0f ? data.fontScale : 1f;

            ice.group = this;
            ice.RefreshCells();
            ice.ResetCount();          // 新冰组的计数从 freezeCount 起算
            ice.BuildVisual(this);
            return ice;
        }

        /// <summary>
        /// 在 PixelGroup 下动态创建一个 CrateItem（new GameObject + AddComponent），把区域裁剪到网格内、拼接视觉。
        ///
        /// **不像箱子那样生成、也不像箱子那样跳过像素**：木箱盖住的像素本来就是 pixel.cells 里的普通像素
        /// （所以 LevelLoader 不把木箱格加进 skipCells），木箱只是盖在上面 —— 渲染由
        /// <see cref="RefreshCrateState"/> 关掉。计数从 0 起算，所以重进关卡天然复位。
        /// </summary>
        public CrateItem SpawnCrate(LevelData.CrateData data)
        {
            if (data == null)
                return null;

            int cmin = Mathf.Max(0, Mathf.Min(data.colMin, data.colMax));
            int cmax = Mathf.Min(columns - 1, Mathf.Max(data.colMin, data.colMax));
            int rmin = Mathf.Max(0, Mathf.Min(data.rowMin, data.rowMax));
            int rmax = Mathf.Min(TotalRows - 1, Mathf.Max(data.rowMin, data.rowMax));

            if (cmin > cmax || rmin > rmax)
            {
                Debug.LogWarning("[PixelGroup] 木箱区域完全越界，已忽略。");
                return null;
            }

            if (cmax - cmin + 1 < 2 || rmax - rmin + 1 < 2)
            {
                Debug.LogWarning("[PixelGroup] 木箱区域裁剪后长宽不足 2（" + (cmax - cmin + 1) + "×" +
                    (rmax - rmin + 1) + "），仍然创建，但请检查关卡数据。");
            }

            if (cratePrefab == null)
            {
                Debug.LogError("[PixelGroup] cratePrefab 为空，无法生成木箱（请指定自带 CrateItem 组件的预制体）。");
                return null;
            }

            var go = PrefabSpawner.Instantiate(cratePrefab, transform);
            if (go == null)
                return null;
            go.name = "Crate_" + rmin + "_" + cmin;

            var crate = go.GetComponent<CrateItem>();
            if (crate == null)
            {
                Debug.LogError("[PixelGroup] 预制体 " + cratePrefab.name + " 缺少 CrateItem 组件。");
                if (Application.isPlaying)
                    Destroy(go);
                else
                    DestroyImmediate(go);
                return null;
            }

            crate.colMin = cmin;
            crate.rowMin = rmin;
            crate.colMax = cmax;
            crate.rowMax = rmax;
            crate.destroyAfterMoves = Mathf.Max(1, data.destroyAfterMoves);

            crate.BuildVisual(this);
            return crate;
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

            // 2×2 且配了整体预制体 → 直接实例化它（BoxItem 来自预制体本身），不再按格拼接
            bool useWhole = BoxItem.ShouldUseWholePrefab(this, cmin, rmin, cmax, rmax);

            GameObject go;
            BoxItem box;
            if (useWhole)
            {
                go = PrefabSpawner.Instantiate(boxWholePrefab, transform);
                if (go == null)
                    return null;
                go.name = "Box_" + rmin + "_" + cmin;

                box = go.GetComponent<BoxItem>();
                if (box == null)
                {
                    Debug.LogError("[PixelGroup] boxWholePrefab " + boxWholePrefab.name + " 缺少 BoxItem 组件。");
                    if (Application.isPlaying)
                        Destroy(go);
                    else
                        DestroyImmediate(go);
                    return null;
                }
                box.wholePrefab = true;
            }
            else
            {
                go = new GameObject("Box_" + rmin + "_" + cmin);
                go.transform.SetParent(transform, false);
                go.transform.localPosition = Vector3.zero;

                box = go.AddComponent<BoxItem>();
                box.cornerPrefab = boxCornerPrefab;
                box.edgePrefab = boxEdgePrefab;
                box.centerPrefab = boxCenterPrefab;
            }

            box.colMin = cmin;
            box.rowMin = rmin;
            box.colMax = cmax;
            box.rowMax = rmax;
            box.colorIds = data.colorIds != null ? (int[])data.colorIds.Clone() : new int[0];
            box.jumpStartInterval = data.jumpStartInterval;
            box.jumpSpawnYOffset = data.jumpSpawnYOffset;

            // 容量以 colorIds（内容数）为准；colorIds 为空时按周围环境（本体 + 相邻 4 方向）兜底。
            // 开箱实际可用格还包括「连通空格」，故不再用周围环境覆盖容量。
            box.capacity = box.colorIds.Length > 0
                ? box.colorIds.Length
                : BoxItem.ComputeCapacity(this, box.colMin, box.rowMin, box.colMax, box.rowMax);
            if (box.colorIds.Length != box.capacity)
                Debug.LogWarning("[PixelGroup] 箱子 " + go.name + " 的 colorIds 数量(" + box.colorIds.Length +
                    ") 与容量(" + box.capacity + ") 不一致，运行时按较小值处理。");

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

        /// <summary>
        /// 在 PixelGroup 下动态创建一个 ElevatorItem（new GameObject + AddComponent），
        /// 并把区域裁剪到网格内、生成地面组与地下组像素、拼接外框/门/竖井视觉。
        /// </summary>
        public ElevatorItem SpawnElevator(LevelData.ElevatorData data, ColorConfig config)
        {
            if (data == null)
                return null;
            if (pixelPrefab == null)
            {
                Debug.LogError("[PixelGroup] pixelPrefab 为空，无法生成升降台像素。");
                return null;
            }

            int cmin = Mathf.Max(0, Mathf.Min(data.colMin, data.colMax));
            int cmax = Mathf.Min(columns - 1, Mathf.Max(data.colMin, data.colMax));
            int rmin = Mathf.Max(0, Mathf.Min(data.rowMin, data.rowMax));
            int rmax = Mathf.Min(TotalRows - 1, Mathf.Max(data.rowMin, data.rowMax));

            if (cmin > cmax || rmin > rmax)
            {
                Debug.LogWarning("[PixelGroup] 升降台区域完全越界，已忽略。");
                return null;
            }

            GameObject go;
            ElevatorItem elev;
            if (elevatorPrefab != null)
            {
                go = PrefabSpawner.Instantiate(elevatorPrefab, transform);
                go.name = "Elevator_" + rmin + "_" + cmin;
                go.transform.localPosition = Vector3.zero;
                elev = go.GetComponent<ElevatorItem>();
                if (elev == null)
                {
                    Debug.LogWarning("[PixelGroup] elevatorPrefab 缺少 ElevatorItem 组件，已回退为动态创建。");
                    elev = go.AddComponent<ElevatorItem>();
                }
            }
            else
            {
                go = new GameObject("Elevator_" + rmin + "_" + cmin);
                go.transform.SetParent(transform, false);
                go.transform.localPosition = Vector3.zero;
                elev = go.AddComponent<ElevatorItem>();
            }

            elev.colMin = cmin;
            elev.rowMin = rmin;
            elev.colMax = cmax;
            elev.rowMax = rmax;
            elev.groundY = data.groundY;
            elev.pitDepth = data.pitDepth;
            elev.groups = new List<LevelData.ElevatorGroupData>();
            if (data.groups != null)
            {
                foreach (var g in data.groups)
                {
                    if (g == null)
                        continue;
                    elev.groups.Add(new LevelData.ElevatorGroupData
                    {
                        cells = g.cells != null ? (int[])g.cells.Clone() : new int[0],
                    });
                }
            }

            elev.BuildVisual(this, config);
            return elev;
        }

        /// <summary>箱子开箱：清除其本体格占用（由 BoxItem.TryOpen 调用）。</summary>
        public void OnBoxOpened(BoxItem box)
        {
            if (box == null)
                return;
            for (int r = box.rowMin; r <= box.rowMax; r++)
                for (int c = box.colMin; c <= box.colMax; c++)
                    if (IsInRange(c, r))
                        boxGrid[c, r] = false;
        }

        /// <summary>
        /// 检查所有未开箱箱子并逐个尝试开箱（§7.3）：按 (rowMin 升序, colMin 升序) 串行判定，
        /// 前箱占格影响后箱，不满足则跳过；动画并行。占格在 TryOpen 内同步完成，
        /// 暴露刷新由调用方在箱子/升降台释放占格全部完成后统一执行（避免释放封路导致旧像素误站起）。
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

            foreach (var box in sorted)
            {
                if (box == null || box.opened)
                    continue;
                box.TryOpen();
            }
        }

        /// <summary>
        /// 检查所有升降台并逐个尝试推进（区域清空 → 开门 + 升起下一组）：
        /// 按 (rowMin 升序, colMin 升序) 串行判定，动画并行。暴露刷新由调用方统一执行。
        /// </summary>
        public void TryAdvanceElevators()
        {
            if (elevators == null || elevators.Count == 0)
                return;
            if (grid == null)
                RebuildGrid();

            var sorted = new List<ElevatorItem>(elevators);
            sorted.Sort((a, b) =>
            {
                if (a == null || b == null)
                    return 0;
                int rc = a.rowMin.CompareTo(b.rowMin);
                if (rc != 0)
                    return rc;
                return a.colMin.CompareTo(b.colMin);
            });

            foreach (var elev in sorted)
            {
                if (elev == null || elev.IsDone)
                    continue;
                elev.TryAdvance();
            }
        }

        /// <summary>
        /// 收集用于容器规划的 (层, 颜色) 列表：静态像素（含轨道上的初始像素）+ 管道将生成的像素 + 箱子隐藏像素 + 升降台分组像素。
        /// 编辑器与运行时均可调用（不依赖 grid 重建）。**已含倍乘门带来的额外像素**：每颗按其所在格的倍率重复发出。
        /// </summary>
        public List<(int layer, int color)> CollectPlanningPixels()
        {
            var sources = new List<(int layer, int color, Vector2Int cell, PlanningSourceKind kind)>();
            CollectPlanningSources(sources);

            var pixels = new List<(int, int)>(sources.Count);
            for (int i = 0; i < sources.Count; i++)
            {
                var s = sources[i];
                int mult = Mathf.Max(1, GateMultiplierAt(s.cell.x, s.cell.y));
                for (int k = 0; k < mult; k++)
                    pixels.Add((s.layer, s.color));
            }
            return pixels;
        }

        /// <summary>
        /// 规划源的**明细**（带机制标签），与 <see cref="CollectPlanningPixels"/> 是**同一次枚举**，
        /// 供「统计颜色总数」按机制分列显示 —— 于是「统计 / 生成 Containers」两处永不发散。
        /// 倍乘门倍率**不在**这里乘：调用方按 <c>cell</c> 查 <see cref="GateMultiplierAt"/> 自行展开（与规划同一算法）。
        /// </summary>
        public List<(int layer, int color, Vector2Int cell, PlanningSourceKind kind)> CollectPlanningDetail()
        {
            var sources = new List<(int layer, int color, Vector2Int cell, PlanningSourceKind kind)>();
            CollectPlanningSources(sources);
            return sources;
        }

        /// <summary>规划源的类别（只用于把统计结果按机制分列）。</summary>
        public enum PlanningSourceKind
        {
            /// <summary>网格上的像素（含管道轨道格上的开局阻挡像素）。</summary>
            Grid,

            /// <summary>管道将生成的像素（轨道格数 × 波次数）。</summary>
            Pipe,

            /// <summary>箱子隐藏像素。</summary>
            Box,

            /// <summary>升降台分组像素。</summary>
            Elevator,
        }

        /// <summary>
        /// 倍乘门额外产生的像素总数 = Σ (所在格倍率 − 1)，供运行时把像素总数算全
        /// （倍乘出来的像素是真实像素、会被真实消费，总数少算就与实际交付量对不上）。
        /// 与 <see cref="CollectPlanningPixels"/> 同源，口径不会发散。
        /// </summary>
        public int CountGateExtraPixels()
        {
            if (gates == null || gates.Count == 0)
                return 0;

            var sources = new List<(int layer, int color, Vector2Int cell, PlanningSourceKind kind)>();
            CollectPlanningSources(sources);

            int extra = 0;
            for (int i = 0; i < sources.Count; i++)
            {
                var s = sources[i];
                int mult = Mathf.Max(1, GateMultiplierAt(s.cell.x, s.cell.y));
                if (mult > 1)
                    extra += mult - 1;
            }
            return extra;
        }

        /// <summary>
        /// **只枚举一次**的规划源列表，是「统计颜色总数 / 生成 Containers / 检查并修复容器颜色」三处共同的底座，
        /// 保证这些口径永不发散。所在格 = 该像素最终落位的格，用来查倍乘门倍率（不在任何门区域内时倍率为 1）。
        ///
        /// 运行时的 <c>GameData.TotalPixelCount</c> 另按**实际生成的物体**统计（CountPixels + CountPipePixels +
        /// CountGateExtraPixels），正常情形与本底座逐项相等；不等只可能来自「这里声明了、运行时没生成」的源
        /// （如越界的升降台分组格、缺 prefab 的 SpawnPixel）。
        /// </summary>
        private void CollectPlanningSources(List<(int layer, int color, Vector2Int cell, PlanningSourceKind kind)> outList)
        {
            outList.Clear();

            // 区域内的地上像素是普通网格像素，正常计入；升降台自身的地下像素由分组单独计入（下方）。
            var elevators = GetComponentsInChildren<ElevatorItem>();

            foreach (var it in GetComponentsInChildren<PixelItem>())
            {
                if (it == null || !IsInRange(it.gridX, it.gridZ))
                    continue;
                outList.Add((it.gridZ, it.colorId, new Vector2Int(it.gridX, it.gridZ), PlanningSourceKind.Grid));
            }

            foreach (var pipe in GetComponentsInChildren<PipeItem>())
            {
                if (pipe == null || pipe.points == null || pipe.points.Count < 2 || pipe.colors == null)
                    continue;
                // 管道每波像素逐个停在轨道格上（PipeItem 会把 gridX/gridZ 设成对应 track[i]），故按轨道格查倍率
                var trackCells = pipe.TrackCells();
                if (trackCells.Count == 0)
                    continue;
                var pipeCell = PipeItem.GetPipeCell(pipe.points);
                int layer = Mathf.Clamp(pipeCell.y, 0, TotalRows - 1);
                foreach (int c in pipe.colors)
                    for (int k = 0; k < trackCells.Count; k++)
                        outList.Add((layer, c, trackCells[k], PlanningSourceKind.Pipe));
            }

            // 箱子隐藏 Pixel：layer 取箱子 rowMin（最前排），颜色按 colorIds 逐个计入。
            // 释放到哪些格是运行时动态选的（BoxItem.PlanAssignments 按当时空位分配），无法预知，
            // 故倍率一律钉在箱子锚点格 (colMin,rowMin) 上——整箱跨门时计数不可靠，校验里会提示。
            foreach (var box in GetComponentsInChildren<BoxItem>())
            {
                if (box == null || box.opened || box.colorIds == null)
                    continue;
                int layer = Mathf.Clamp(box.rowMin, 0, TotalRows - 1);
                int count = Mathf.Min(box.capacity, box.colorIds.Length);
                var anchor = new Vector2Int(box.colMin, box.rowMin);
                for (int i = 0; i < count; i++)
                    outList.Add((layer, box.colorIds[i], anchor, PlanningSourceKind.Box));
            }

            // 升降台分组像素：layer 取升降台 rowMin，颜色与所在格按每组 cells 的三元组取（编辑器与运行时通用）
            foreach (var elev in elevators)
            {
                if (elev == null || elev.groups == null)
                    continue;
                int layer = Mathf.Clamp(elev.rowMin, 0, TotalRows - 1);
                foreach (var g in elev.groups)
                {
                    if (g == null || g.cells == null)
                        continue;
                    for (int i = 0; i + 2 < g.cells.Length; i += 3)
                        outList.Add((layer, g.cells[i + 2], new Vector2Int(g.cells[i], g.cells[i + 1]), PlanningSourceKind.Elevator));
                }
            }
        }
    }
}
