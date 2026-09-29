using System.Collections.Generic;
using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 「快速生成 Record」的离线棋盘：**只吃一份关卡 JSON**（<see cref="LevelData"/>），
    /// 不依赖场景、不进 Play，算出每个**操作组**的**层级**（= 移出它之前至少要移出多少颗像素）。
    ///
    /// 口径（与运行时代码逐条对齐；改动这里之前先回去核对 PixelGroup / GameController）：
    ///
    /// · **操作组**：网格像素按同色 4 邻连通块；木箱下的像素只与**同一木箱**内相邻
    ///   （<c>CrateAdjacencyAllowed</c> 的 Record 口径）；管道**一波一组**；箱子内**每个颜色一组**；
    ///   升降台一组内按同色 4 邻再拆；冰下像素与未揭晓问号**断开 ⇒ 各自成组**。
    ///   组在开始时**定死**，之后不因冰化开 / 木箱拆掉 / 箱子开启而重新合并。
    ///
    /// · **能移出**：寻路口径 —— 组要能经「空 / 组内」格连通到首排（<c>CanReachFront</c>），
    ///   含倍乘门来路规则；另外要过未揭晓问号 / 冰冻 / 木箱盖住三条守卫。
    ///
    /// · **层级**：贪心每轮取走**全部**可走组；层级 = 严格早于它的轮次里移出的**像素总数**
    ///   （按倍率展开），再**加上该组绕管道的代价**（见下）。一轮至少走 1 颗。
    ///
    /// · **管道**（简化口径，不模拟释放过程）：① 管道轨道格**不算障碍** —— 运行时会把「还有待产波次」
    ///   的管道整条轨道当障碍，那是本工具刻意偏离的一处，否则管道会把自己的轨道尾端锁死；
    ///   ② 第 0 波层级 =「朝向前一格」（轨道第一格）变成「空且通首排」的那一轮；
    ///   ③ 第 k 波层级 = 第 0 波层级 + k × 单波容量（单波容量 = 轨道各格倍率之和）；
    ///   ④ 别的组若路线**依赖**某条管道（只把该管道轨道当障碍就到不了首排），
    ///   其层级 += 该管道「单波容量 × 波数」。
    ///
    /// · **箱子**（简化口径）：只要箱体四邻存在「空且通首排」的格即视为可释放，**不看容量 / 空位数**；
    ///   释放后箱体不再算障碍，箱内像素不占格、不挡别人、不与箱外同色连通。
    ///
    /// · **倍乘门**：区域内的格按各门倍数连乘（<see cref="GateMultiplierMap"/>），
    ///   影响元素数量与 Record 的重复条数。
    /// </summary>
    public sealed class LevelGridBoard
    {
        /// <summary>棋盘上的「无像素」标记（与关卡 JSON 的空像素 -1 同值）。</summary>
        public const int Empty = -1;

        public enum GroupKind { GridPixel = 0, PipeWave = 1, Elevator = 2, BoxColor = 3 }

        /// <summary>一个操作组：一次点击能整组移走的最小单位。</summary>
        public sealed class Group
        {
            public GroupKind kind;
            public int color;
            public int boxIndex = -1;
            public int elevatorIndex = -1;
            public int elevatorGroupIndex = -1;

            /// <summary>成员格。箱子颜色组为空（释放落位不可预知，按口径不建模）。</summary>
            public readonly List<Vector2Int> cells = new List<Vector2Int>();

            /// <summary>该组在 Record 里会写出几行（已按倍率展开）。</summary>
            public int elements;

            /// <summary>层级 = 前置像素总数。未走通的组为最后一档。</summary>
            public int tier = -1;

            /// <summary>被取走的轮次（0 起）。仅供报告对照。</summary>
            public int round = -1;

            /// <summary>所属生产者是否已产出（升降台组）。</summary>
            public bool materialized;

            /// <summary>为了绕开管道轨道而加进来的代价（= 各挡路管道的「单波容量 × 波数」之和）；0 = 不经过管道。</summary>
            public int pipePenalty;

            /// <summary>贪心轮里始终走不掉（死局）。</summary>
            public bool stuck;

            public string note = "";
        }

        private sealed class BoxState
        {
            public int c0, r0, c1, r1, capacity;
            public int[] colors;
            public int anchorC, anchorR;
            public bool opened;
            public readonly List<Vector2Int> ring = new List<Vector2Int>();
        }

        private sealed class CrateState
        {
            public int c0, r0, c1, r1, need, moves;
            public bool destroyed;
        }

        private sealed class IceState
        {
            public readonly HashSet<Vector2Int> cells = new HashSet<Vector2Int>();
            public int remaining;
            public bool meltWhenExposed;
            public bool exposedBefore;
        }

        private sealed class PipeState
        {
            public readonly List<Vector2Int> track = new List<Vector2Int>();
            public readonly HashSet<Vector2Int> covered = new HashSet<Vector2Int>();
            public readonly List<Group> waveGroups = new List<Group>();
            public int[] colors;

            /// <summary>单波容量 = 一条轨道各格倍率之和（倍率展开的元素数）。每一波都一样。</summary>
            public int waveElements;

            /// <summary>「管道朝向前一格」= 轨道第一格。它成为「空且通首排」的那一轮定第 0 波层级。</summary>
            public Vector2Int frontCell;

            /// <summary>第 0 波层级；-1 = 还没算出来（前格始终不通 ⇒ 该管道整组卡死）。</summary>
            public int wave0Tier = -1;
        }

        private sealed class ElevatorState
        {
            public int c0, r0, c1, r1;
            public int nextGroup;
            public readonly List<List<Vector3Int>> groups = new List<List<Vector3Int>>();
        }

        private static readonly int[] DC = { 1, -1, 0, 0 };
        private static readonly int[] DR = { 0, 0, 1, -1 };

        private readonly int _columns;
        private readonly int _rows;
        private readonly int[,] _mult;
        private readonly bool[,] _barrier;      // 墙 ∪ 管道自身格：永久障碍
        private readonly int[,] _gateOwner;     // 门格 → 门下标；-1 = 非门格
        private readonly List<bool[,]> _gateRegion = new List<bool[,]>();

        private readonly List<BoxState> _boxes = new List<BoxState>();
        private readonly List<CrateState> _crates = new List<CrateState>();
        private readonly List<IceState> _ices = new List<IceState>();
        private readonly List<PipeState> _pipes = new List<PipeState>();
        private readonly List<ElevatorState> _elevators = new List<ElevatorState>();

        private readonly bool[,] _initFrozen;     // 分组用：开局是否冻住
        private readonly bool[,] _initRevealed;   // 分组用：开局是否已揭晓

        private int[,] _color;
        private bool[,] _question;
        private bool[,] _revealed;
        private bool[,] _frozen;
        private bool[,] _crateMask;
        private bool[,] _boxLive;
        private bool[,] _reachableEmpty;
        private bool[,] _directlyExposed;

        private readonly List<Group> _groups = new List<Group>();
        private readonly HashSet<Group> _removed = new HashSet<Group>();

        public int Columns => _columns;
        public int Rows => _rows;
        public IReadOnlyList<Group> Groups => _groups;

        /// <summary>全部组的元素数之和 = 该关的 Record 应写出的行数。</summary>
        public int TotalLines { get; private set; }

        /// <summary>无法开算时的原因（如 cells 数量不足）；null = 正常。</summary>
        public string Warning { get; private set; }

        /// <summary>
        /// 「箱子体 / 门格上留了非负像素值」的格数。运行时（<c>LevelLoader.ApplyPixel</c> 的 skipCells）
        /// 与本次生成都会跳过这些格，但 <c>ContainerRearranger.Validate</c> 只看「是否障碍格」、不跳箱/门格，
        /// 所以它的期望像素数会比实际多这么多 —— 生成后总数对不上时用这个数解释，不是生成的问题。
        /// </summary>
        public int CellsSkippedButCountedByValidate { get; private set; }

        /// <summary>整张网的倍率（[column, row]；不在任何门区域内为 1）。</summary>
        public int MultiplierAt(int col, int row)
        {
            if (!InRange(col, row)) return 1;
            return Mathf.Max(1, _mult[col, row]);
        }

        public LevelGridBoard(LevelData data)
        {
            _columns = Mathf.Max(1, data.pixel.columns);
            _rows = Mathf.Max(0, data.pixel.rows) + Mathf.Max(0, data.pixel.tailRows);

            int expected = _columns * _rows;
            if (data.pixel.cells == null || data.pixel.cells.Length < expected)
            {
                Warning = "关卡像素 cells 数量不足（需要 " + expected + "，实际 " +
                    (data.pixel.cells != null ? data.pixel.cells.Length : 0) + "）。";
                _rows = 0;
                _color = new int[_columns, 0];
                _question = new bool[_columns, 0];
                _revealed = new bool[_columns, 0];
                _frozen = new bool[_columns, 0];
                _crateMask = new bool[_columns, 0];
                _boxLive = new bool[_columns, 0];
                _barrier = new bool[_columns, 0];
                _gateOwner = new int[_columns, 0];
                _initFrozen = new bool[_columns, 0];
                _initRevealed = new bool[_columns, 0];
                _mult = new int[_columns, 0];
                return;
            }

            _barrier = new bool[_columns, _rows];
            _gateOwner = new int[_columns, _rows];
            for (int c = 0; c < _columns; c++)
                for (int r = 0; r < _rows; r++)
                    _gateOwner[c, r] = -1;

            // 墙 ∪ 管道自身格 = 永久障碍（与 LevelLoader.ApplyPixel 的 skipCells、Validate 的 barrierCells 同口径）
            var barrierCells = new HashSet<Vector2Int>();
            if (data.walls != null)
                foreach (var w in data.walls)
                {
                    if (w == null || w.points == null) continue;
                    WallItem.CollectOccupiedCells(w.points, w.closed, barrierCells);
                }
            if (data.pipes != null)
                foreach (var p in data.pipes)
                {
                    if (p == null || p.points == null || p.points.Length < 1) continue;
                    barrierCells.Add(PipeItem.GetPipeCell(p.points));
                }
            foreach (var cell in barrierCells)
                if (InRange(cell.x, cell.y)) _barrier[cell.x, cell.y] = true;

            // 倍乘门：门格 + 闭合区域（区域只把「墙 ∪ 管道」当永久障碍）
            if (data.gates != null)
                foreach (var gate in data.gates)
                {
                    if (gate == null) continue;
                    var gateCells = new HashSet<Vector2Int>();
                    GateItem.CollectCells(gate.start, gate.end, gateCells);
                    if (gateCells.Count == 0) continue;

                    var region = GateRegion.ComputeRegion(
                        _columns, _rows,
                        (c, r) => barrierCells.Contains(new Vector2Int(c, r)),
                        gateCells);

                    int gi = _gateRegion.Count;
                    var mask = new bool[_columns, _rows];
                    foreach (var cell in region)
                        if (InRange(cell.x, cell.y)) mask[cell.x, cell.y] = true;
                    foreach (var cell in gateCells)
                        if (InRange(cell.x, cell.y) && _gateOwner[cell.x, cell.y] < 0)
                            _gateOwner[cell.x, cell.y] = gi;
                    _gateRegion.Add(mask);
                }

            _mult = GateMultiplierMap.Build(data, _columns, _rows, barrierCells);

            BuildCrates(data);
            BuildBoxes(data);
            BuildIces(data);
            BuildPipes(data);
            BuildElevators(data);

            // 网格像素：跳过障碍格 / 门格 / 箱子体（与运行时 ApplyPixel 的 skipCells 一致），
            // 再跳过 JSON 里的空像素（-1）。
            _color = new int[_columns, _rows];
            _question = new bool[_columns, _rows];
            for (int r = 0; r < _rows; r++)
                for (int c = 0; c < _columns; c++)
                {
                    _color[c, r] = Empty;
                    int index = r * _columns + c;
                    if (_barrier[c, r]) continue;
                    if (_gateOwner[c, r] >= 0 || InAnyBoxBody(c, r))
                    {
                        if (data.pixel.cells[index] >= 0) CellsSkippedButCountedByValidate++;
                        continue;
                    }
                    int color = data.pixel.cells[index];
                    if (color < 0) continue;
                    _color[c, r] = color;
                    _question[c, r] = data.pixel.questionCells != null
                        && data.pixel.questionCells.Length > index
                        && data.pixel.questionCells[index];
                }

            _revealed = new bool[_columns, _rows];
            _frozen = new bool[_columns, _rows];
            _crateMask = new bool[_columns, _rows];
            _boxLive = new bool[_columns, _rows];

            for (int i = 0; i < _boxes.Count; i++) SetBoxBodyLive(_boxes[i], true);

            RefreshFrozen();
            _initFrozen = (bool[,])_frozen.Clone();
            RefreshCrateMask();

            // 开局先算一次暴露 —— 顺带得到「开局已揭晓的问号」（游戏里加载后第一次 RefreshExposed 就揭晓了）
            RefreshExposure();
            _initRevealed = (bool[,])_revealed.Clone();

            BuildGroups();

            TotalLines = 0;
            for (int i = 0; i < _groups.Count; i++) TotalLines += _groups[i].elements;
        }

        // ===== 关卡结构 =====

        private void BuildCrates(LevelData data)
        {
            if (data.crates == null) return;
            foreach (var c in data.crates)
            {
                if (c == null) continue;
                int c0 = Mathf.Clamp(Mathf.Min(c.colMin, c.colMax), 0, _columns - 1);
                int c1 = Mathf.Clamp(Mathf.Max(c.colMin, c.colMax), 0, _columns - 1);
                int r0 = Mathf.Clamp(Mathf.Min(c.rowMin, c.rowMax), 0, _rows - 1);
                int r1 = Mathf.Clamp(Mathf.Max(c.rowMin, c.rowMax), 0, _rows - 1);
                if (c1 < c0 || r1 < r0) continue;
                _crates.Add(new CrateState
                {
                    c0 = c0, r0 = r0, c1 = c1, r1 = r1,
                    need = Mathf.Max(1, c.destroyAfterMoves),
                });
            }
        }

        private void BuildBoxes(LevelData data)
        {
            if (data.boxes == null) return;
            foreach (var b in data.boxes)
            {
                if (b == null) continue;
                int c0 = Mathf.Clamp(Mathf.Min(b.colMin, b.colMax), 0, _columns - 1);
                int c1 = Mathf.Clamp(Mathf.Max(b.colMin, b.colMax), 0, _columns - 1);
                int r0 = Mathf.Clamp(Mathf.Min(b.rowMin, b.rowMax), 0, _rows - 1);
                int r1 = Mathf.Clamp(Mathf.Max(b.rowMin, b.rowMax), 0, _rows - 1);
                if (c1 < c0 || r1 < r0) continue;

                var st = new BoxState
                {
                    c0 = c0, r0 = r0, c1 = c1, r1 = r1,
                    capacity = b.capacity,
                    colors = b.colorIds != null ? b.colorIds : new int[0],
                    anchorC = c0, anchorR = r0,
                };

                // 箱体四邻（矩形外圈）：只要有一格空且通首排即可释放
                for (int c = c0 - 1; c <= c1 + 1; c++)
                {
                    AddRingCell(st, c, r0 - 1);
                    AddRingCell(st, c, r1 + 1);
                }
                for (int r = r0; r <= r1; r++)
                {
                    AddRingCell(st, c0 - 1, r);
                    AddRingCell(st, c1 + 1, r);
                }

                _boxes.Add(st);
            }
        }

        private void AddRingCell(BoxState box, int c, int r)
        {
            if (!InRange(c, r)) return;
            var cell = new Vector2Int(c, r);
            if (box.ring.Contains(cell)) return;
            box.ring.Add(cell);
        }

        private void BuildIces(LevelData data)
        {
            if (data.iceGroups == null) return;
            foreach (var g in data.iceGroups)
            {
                if (g == null || g.cells == null || g.cells.Length == 0) continue;
                var st = new IceState
                {
                    remaining = Mathf.Max(1, g.count),
                    meltWhenExposed = g.meltWhenExposed,
                };
                for (int i = 0; i < g.cells.Length; i++)
                {
                    int c = Mathf.RoundToInt(g.cells[i].x);
                    int r = Mathf.RoundToInt(g.cells[i].y);
                    if (InRange(c, r)) st.cells.Add(new Vector2Int(c, r));
                }
                if (st.cells.Count > 0) _ices.Add(st);
            }
        }

        private void BuildPipes(LevelData data)
        {
            if (data.pipes == null) return;
            foreach (var p in data.pipes)
            {
                if (p == null || p.points == null || p.points.Length < 2
                    || p.colors == null || p.colors.Length < 1) continue;

                var st = new PipeState { colors = p.colors };
                PipeItem.CollectTrackCells(p.points, _columns, _rows, st.track);
                if (st.track.Count == 0) continue;

                var pipeCell = PipeItem.GetPipeCell(p.points);
                if (InRange(pipeCell.x, pipeCell.y)) st.covered.Add(pipeCell);
                for (int i = 0; i < st.track.Count; i++) st.covered.Add(st.track[i]);

                st.frontCell = st.track[0];
                for (int i = 0; i < st.track.Count; i++)
                    st.waveElements += MultiplierAt(st.track[i].x, st.track[i].y);

                _pipes.Add(st);
            }
        }

        private void BuildElevators(LevelData data)
        {
            if (data.elevators == null) return;
            foreach (var e in data.elevators)
            {
                if (e == null) continue;
                var st = new ElevatorState
                {
                    c0 = Mathf.Clamp(Mathf.Min(e.colMin, e.colMax), 0, _columns - 1),
                    c1 = Mathf.Clamp(Mathf.Max(e.colMin, e.colMax), 0, _columns - 1),
                    r0 = Mathf.Clamp(Mathf.Min(e.rowMin, e.rowMax), 0, _rows - 1),
                    r1 = Mathf.Clamp(Mathf.Max(e.rowMin, e.rowMax), 0, _rows - 1),
                };
                if (e.groups != null)
                    foreach (var g in e.groups)
                    {
                        if (g == null || g.cells == null) continue;
                        var list = new List<Vector3Int>();
                        for (int i = 0; i + 2 < g.cells.Length; i += 3)
                            list.Add(new Vector3Int(g.cells[i], g.cells[i + 1], g.cells[i + 2]));
                        if (list.Count > 0) st.groups.Add(list);
                    }
                if (st.groups.Count > 0) _elevators.Add(st);
            }
        }

        // ===== 分组（开始时定死） =====

        private void BuildGroups()
        {
            BuildGridGroups();
            BuildPipeGroups();
            BuildElevatorGroups();
            BuildBoxGroups();
        }

        private void BuildGridGroups()
        {
            var visited = new bool[_columns, _rows];
            for (int r = 0; r < _rows; r++)
                for (int c = 0; c < _columns; c++)
                {
                    if (visited[c, r] || !HasPixel(c, r)) continue;

                    if (IsolatedAtInit(c, r))
                    {
                        visited[c, r] = true;
                        AddGridGroup(new List<Vector2Int> { new Vector2Int(c, r) });
                        continue;
                    }

                    int color = _color[c, r];
                    var comp = new List<Vector2Int>();
                    var queue = new Queue<Vector2Int>();
                    queue.Enqueue(new Vector2Int(c, r));
                    visited[c, r] = true;

                    while (queue.Count > 0)
                    {
                        var cur = queue.Dequeue();
                        comp.Add(cur);
                        for (int d = 0; d < 4; d++)
                        {
                            int nc = cur.x + DC[d], nr = cur.y + DR[d];
                            if (!InRange(nc, nr) || visited[nc, nr]) continue;
                            if (!HasPixel(nc, nr) || _color[nc, nr] != color) continue;
                            if (IsolatedAtInit(nc, nr)) continue;   // 冻住 / 未揭晓问号 自成一格，不并入
                            if (!CrateAdjacentOk(cur, new Vector2Int(nc, nr))) continue;
                            visited[nc, nr] = true;
                            queue.Enqueue(new Vector2Int(nc, nr));
                        }
                    }
                    AddGridGroup(comp);
                }
        }

        private void AddGridGroup(List<Vector2Int> comp)
        {
            var g = new Group
            {
                kind = GroupKind.GridPixel,
                color = _color[comp[0].x, comp[0].y],
                materialized = true,
            };
            for (int i = 0; i < comp.Count; i++)
            {
                g.cells.Add(comp[i]);
                g.elements += MultiplierAt(comp[i].x, comp[i].y);
            }
            _groups.Add(g);
        }

        private void BuildPipeGroups()
        {
            for (int pi = 0; pi < _pipes.Count; pi++)
            {
                var p = _pipes[pi];
                for (int w = 0; w < p.colors.Length; w++)
                {
                    var g = new Group
                    {
                        kind = GroupKind.PipeWave,
                        color = p.colors[w],
                        note = "管道#" + pi + " 第" + w + "波",
                    };
                    for (int i = 0; i < p.track.Count; i++)
                    {
                        g.cells.Add(p.track[i]);
                        g.elements += MultiplierAt(p.track[i].x, p.track[i].y);
                    }
                    p.waveGroups.Add(g);
                    _groups.Add(g);
                }
            }
        }

        private void BuildElevatorGroups()
        {
            for (int ei = 0; ei < _elevators.Count; ei++)
            {
                var e = _elevators[ei];
                for (int gi = 0; gi < e.groups.Count; gi++)
                {
                    var list = e.groups[gi];
                    var indexOf = new Dictionary<Vector2Int, int>();
                    for (int i = 0; i < list.Count; i++)
                        indexOf[new Vector2Int(list[i].x, list[i].y)] = i;

                    var visited = new bool[list.Count];
                    for (int seed = 0; seed < list.Count; seed++)
                    {
                        if (visited[seed]) continue;
                        int compColor = list[seed].z;
                        var comp = new List<Vector2Int>();
                        var queue = new Queue<int>();
                        queue.Enqueue(seed);
                        visited[seed] = true;

                        while (queue.Count > 0)
                        {
                            int k = queue.Dequeue();
                            var cell = new Vector2Int(list[k].x, list[k].y);
                            comp.Add(cell);
                            for (int d = 0; d < 4; d++)
                            {
                                var nb = new Vector2Int(cell.x + DC[d], cell.y + DR[d]);
                                if (!indexOf.TryGetValue(nb, out int j)) continue;
                                if (visited[j] || list[j].z != compColor) continue;
                                visited[j] = true;
                                queue.Enqueue(j);
                            }
                        }

                        var g = new Group
                        {
                            kind = GroupKind.Elevator,
                            color = compColor,
                            elevatorIndex = ei,
                            elevatorGroupIndex = gi,
                            note = "升降台#" + ei + " 第" + gi + "组",
                        };
                        for (int i = 0; i < comp.Count; i++)
                        {
                            g.cells.Add(comp[i]);
                            g.elements += MultiplierAt(comp[i].x, comp[i].y);
                        }
                        _groups.Add(g);
                    }
                }
            }
        }

        private void BuildBoxGroups()
        {
            for (int bi = 0; bi < _boxes.Count; bi++)
            {
                var box = _boxes[bi];
                int count = Mathf.Min(box.capacity, box.colors.Length);
                var perColor = new Dictionary<int, int>();
                var order = new List<int>();
                for (int i = 0; i < count; i++)
                {
                    int color = box.colors[i];
                    if (!perColor.ContainsKey(color)) { perColor[color] = 0; order.Add(color); }
                    perColor[color]++;
                }
                order.Sort();
                int mult = MultiplierAt(box.anchorC, box.anchorR);
                for (int i = 0; i < order.Count; i++)
                {
                    int color = order[i];
                    _groups.Add(new Group
                    {
                        kind = GroupKind.BoxColor,
                        color = color,
                        boxIndex = bi,
                        materialized = true,
                        elements = perColor[color] * mult,
                        note = "箱子#" + bi + " 颜色" + color,
                    });
                }
            }
        }

        // ===== 求解 =====

        /// <summary>
        /// 贪心推进：每轮取走全部可走组，据此给出每组的层级（= 之前移出的像素总数 + 该组绕管道的代价）。
        /// 管道波次**不参与**轮次推进，层级单独按公式顺推（见 <see cref="AssignPipeTiers"/>）。
        /// </summary>
        public void Solve()
        {
            if (Warning != null) return;

            int cumulative = 0;
            int round = 0;

            while (true)
            {
                if (round > 100000) { Warning = "贪心轮次异常多，已中断。"; break; }

                // 冰的「点击前是否暴露」快照：本轮所有点击共用同一份「点击前」状态
                for (int i = 0; i < _ices.Count; i++)
                    _ices[i].exposedBefore = IceHasExposedMember(_ices[i]);

                MaterializeProducers();

                // 管道第 0 波：只要「朝向前一格」变成「空且通首排」就记下这一轮的层级
                for (int i = 0; i < _pipes.Count; i++)
                {
                    var p = _pipes[i];
                    if (p.wave0Tier >= 0) continue;
                    if (InRange(p.frontCell.x, p.frontCell.y) && _reachableEmpty[p.frontCell.x, p.frontCell.y])
                        p.wave0Tier = cumulative;
                }

                var ready = new List<Group>();
                for (int i = 0; i < _groups.Count; i++)
                {
                    var g = _groups[i];
                    if (g.kind == GroupKind.PipeWave) continue;   // 波次单独顺推
                    if (!_removed.Contains(g) && CanRemove(g)) ready.Add(g);
                }
                if (ready.Count == 0) break;

                for (int i = 0; i < ready.Count; i++)
                {
                    var g = ready[i];
                    g.pipePenalty = PipePenalty(g);
                    g.tier = cumulative + g.pipePenalty;
                    g.round = round;
                }
                for (int i = 0; i < ready.Count; i++) Remove(ready[i]);
                for (int i = 0; i < ready.Count; i++) cumulative += ready[i].elements;
                round++;
            }

            for (int i = 0; i < _groups.Count; i++)
            {
                var g = _groups[i];
                if (g.kind == GroupKind.PipeWave || _removed.Contains(g)) continue;
                g.tier = cumulative;
                g.round = round;
                g.stuck = true;
                g.note = (g.note.Length > 0 ? g.note + " " : "") + "卡死：" + BlockReason(g);
            }

            AssignPipeTiers(cumulative, round);
        }

        /// <summary>
        /// 管道各波层级：第 k 波 = 第 0 波层级 + k × 单波容量。
        /// 第 0 波层级来自「朝向前一格变空且通首排」的那一轮；前格始终不通 ⇒ 该管道整组归入最后一档。
        /// </summary>
        private void AssignPipeTiers(int lastCumulative, int lastRound)
        {
            for (int i = 0; i < _pipes.Count; i++)
            {
                var p = _pipes[i];
                for (int k = 0; k < p.waveGroups.Count; k++)
                {
                    var g = p.waveGroups[k];
                    g.materialized = true;
                    g.round = -1;   // 波次不落在任何一轮里（层级是顺推出来的）
                    if (p.wave0Tier < 0)
                    {
                        g.tier = lastCumulative;
                        g.stuck = true;
                        g.note = (g.note.Length > 0 ? g.note + " " : "") + "卡死：管道朝向前一格始终不通首排";
                        continue;
                    }
                    g.tier = p.wave0Tier + k * p.waveElements;
                }
            }
        }

        /// <summary>生产者推进到稳态：开箱 / 升降台升组 / 管道补波（链式，直到没有变化）。</summary>
        private void MaterializeProducers()
        {
            int guard = 0;
            bool changed = true;
            while (changed && guard++ < 512)
            {
                changed = false;
                RefreshExposure();

                for (int i = 0; i < _boxes.Count; i++)
                {
                    var box = _boxes[i];
                    if (box.opened || !BoxReleasable(box)) continue;
                    box.opened = true;
                    SetBoxBodyLive(box, false);
                    changed = true;
                }

                for (int i = 0; i < _elevators.Count; i++)
                {
                    var e = _elevators[i];
                    if (e.nextGroup >= e.groups.Count) continue;
                    if (!ElevatorRegionEmpty(e)) continue;

                    var list = e.groups[e.nextGroup];
                    for (int k = 0; k < list.Count; k++)
                    {
                        if (!InRange(list[k].x, list[k].y)) continue;
                        if (HasPixel(list[k].x, list[k].y)) continue;   // 声明格被占：跳过（与运行时同，不覆盖）
                        _color[list[k].x, list[k].y] = list[k].z;
                    }
                    for (int k = 0; k < _groups.Count; k++)
                    {
                        var g = _groups[k];
                        if (g.kind == GroupKind.Elevator && g.elevatorIndex == i && g.elevatorGroupIndex == e.nextGroup)
                            g.materialized = true;
                    }
                    e.nextGroup++;
                    changed = true;
                }
            }

            RefreshExposure();
        }

        private bool CanRemove(Group g)
        {
            switch (g.kind)
            {
                case GroupKind.BoxColor:
                    return _boxes[g.boxIndex].opened;   // 箱子一暴露，箱内各颜色组即可带走
                case GroupKind.Elevator:
                    if (!g.materialized) return false;
                    break;
            }

            for (int i = 0; i < g.cells.Count; i++)
            {
                var cell = g.cells[i];
                if (!InRange(cell.x, cell.y)) continue;
                if (_frozen[cell.x, cell.y]) return false;                                   // 冰冻守卫
                if (_question[cell.x, cell.y] && !_revealed[cell.x, cell.y]) return false;   // 未揭晓问号守卫
                if (_crateMask[cell.x, cell.y]) return false;                                // 木箱盖住守卫
            }

            return CanReachFront(g);
        }

        private void Remove(Group g)
        {
            _removed.Add(g);

            // 木箱：本组有任意一颗像素与箱 4 邻 → 该箱记 1 次（一箱一次点击最多 1 次），计满即拆
            for (int i = 0; i < _crates.Count; i++)
            {
                var crate = _crates[i];
                if (crate.destroyed || !TouchesCrate(g, crate)) continue;
                crate.moves++;
                if (crate.moves >= crate.need) crate.destroyed = true;
            }

            for (int i = 0; i < g.cells.Count; i++) ClearCell(g.cells[i]);

            // 冰：一次成功点击 → 全局每个冰组各 -1（按点击不按像素）
            for (int i = 0; i < _ices.Count; i++)
            {
                var ice = _ices[i];
                if (ice.remaining <= 0) continue;
                if (ice.meltWhenExposed && !ice.exposedBefore) continue;
                ice.remaining--;
            }

            RefreshFrozen();
            RefreshCrateMask();
            RefreshExposure();
        }

        // ===== 判定 =====

        /// <summary>
        /// 与 <c>GameController.CanReachFront</c> 同口径：把组内格视为即将腾空，检查是否存在一条
        /// 只经过「空 / 组内」格、从组连通到首排（row 0）的路径。障碍 = 墙/管道自身格/关着的箱体/覆盖中的木箱，
        /// 外加「不是本组来路」的门格。
        ///
        /// **管道轨道格不算障碍**（与运行时不同，这是本工具的口径简化）：管道不再模拟释放过程，
        /// 挡路只以一个独立代价（<see cref="PipePenalty"/>）加在层级上，否则管道会把自己的轨道连同尾端一起锁死
        /// （轨道不空 ⇒ 不产波 ⇒ 障碍不解除）。<paramref name="extraBlocked"/> 用来单独把某条管道的轨道
        /// 当障碍试一次，判断该组的路线是否**依赖**这条管道。
        /// </summary>
        private bool CanReachFront(Group g, HashSet<Vector2Int> extraBlocked = null)
        {
            if (g.cells.Count == 0) return false;

            var inGroup = new HashSet<Vector2Int>();
            for (int i = 0; i < g.cells.Count; i++) inGroup.Add(g.cells[i]);

            // 本组能穿哪些门：组内只要有一颗落在该门闭合区域内 → 整组都能过这道门
            var passGates = new HashSet<int>();
            for (int gi = 0; gi < _gateRegion.Count; gi++)
            {
                var mask = _gateRegion[gi];
                for (int i = 0; i < g.cells.Count; i++)
                {
                    var cell = g.cells[i];
                    if (InRange(cell.x, cell.y) && mask[cell.x, cell.y]) { passGates.Add(gi); break; }
                }
            }

            var visited = new bool[_columns, _rows];
            var queue = new Queue<Vector2Int>();
            for (int i = 0; i < g.cells.Count; i++)
            {
                var cell = g.cells[i];
                if (!InRange(cell.x, cell.y)) continue;
                queue.Enqueue(cell);
                visited[cell.x, cell.y] = true;
            }

            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                if (cur.y == 0) return true;

                for (int d = 0; d < 4; d++)
                {
                    int nc = cur.x + DC[d], nr = cur.y + DR[d];
                    if (!InRange(nc, nr) || visited[nc, nr]) continue;
                    if (Blocked(nc, nr)) continue;
                    if (extraBlocked != null && extraBlocked.Contains(new Vector2Int(nc, nr))) continue;

                    int owner = _gateOwner[nc, nr];
                    if (owner >= 0 && !passGates.Contains(owner)) continue;

                    if (HasPixel(nc, nr) && !inGroup.Contains(new Vector2Int(nc, nr))) continue;

                    visited[nc, nr] = true;
                    queue.Enqueue(new Vector2Int(nc, nr));
                }
            }

            return false;
        }

        private bool BoxReleasable(BoxState box)
        {
            for (int i = 0; i < box.ring.Count; i++)
            {
                var cell = box.ring[i];
                if (_reachableEmpty[cell.x, cell.y]) return true;
            }
            return false;
        }

        private bool ElevatorRegionEmpty(ElevatorState e)
        {
            for (int c = e.c0; c <= e.c1; c++)
                for (int r = e.r0; r <= e.r1; r++)
                    if (HasPixel(c, r)) return false;
            return true;
        }

        private bool TouchesCrate(Group g, CrateState crate)
        {
            for (int i = 0; i < g.cells.Count; i++)
            {
                var cell = g.cells[i];
                for (int d = 0; d < 4; d++)
                {
                    int nc = cell.x + DC[d], nr = cell.y + DR[d];
                    if (nc >= crate.c0 && nc <= crate.c1 && nr >= crate.r0 && nr <= crate.r1)
                        return true;
                }
            }
            return false;
        }

        private string BlockReason(Group g)
        {
            if (g.kind == GroupKind.Elevator && !g.materialized) return "所属升降台还没升到它";
            for (int i = 0; i < g.cells.Count; i++)
            {
                var cell = g.cells[i];
                if (!InRange(cell.x, cell.y)) continue;
                if (_frozen[cell.x, cell.y]) return "被冰冻住";
                if (_question[cell.x, cell.y] && !_revealed[cell.x, cell.y]) return "问号未揭晓";
                if (_crateMask[cell.x, cell.y]) return "被木箱盖住";
            }
            if (g.kind == GroupKind.BoxColor && !_boxes[g.boxIndex].opened) return "箱子还没暴露";
            return "无法连通到首排";
        }

        // ===== 暴露判定（与 PixelGroup.RefreshExposed 同口径） =====

        private void RefreshExposure()
        {
            // 1. 「能连通到首排的空格」：从首排空/出口出发 BFS，只经过「空且非障碍且未被活跃管道覆盖」的格
            _reachableEmpty = new bool[_columns, _rows];
            var queue = new Queue<Vector2Int>();
            for (int c = 0; c < _columns; c++)
            {
                if (!EmptyForExposure(c, 0)) continue;
                _reachableEmpty[c, 0] = true;
                queue.Enqueue(new Vector2Int(c, 0));
            }
            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                for (int d = 0; d < 4; d++)
                {
                    int nc = cur.x + DC[d], nr = cur.y + DR[d];
                    if (!InRange(nc, nr) || _reachableEmpty[nc, nr]) continue;
                    if (!EmptyForExposure(nc, nr)) continue;
                    _reachableEmpty[nc, nr] = true;
                    queue.Enqueue(new Vector2Int(nc, nr));
                }
            }

            // 2. 直接暴露：首排，或四邻里有「连通首排的空格」——门格只对**该门闭合区域内**的格作数
            _directlyExposed = new bool[_columns, _rows];
            for (int c = 0; c < _columns; c++)
                for (int r = 0; r < _rows; r++)
                {
                    if (!HasPixel(c, r) || Blocked(c, r)) continue;
                    _directlyExposed[c, r] =
                        r == 0
                        || NeighbourReachableForExposure(c, r, c, r - 1)
                        || NeighbourReachableForExposure(c, r, c, r + 1)
                        || NeighbourReachableForExposure(c, r, c - 1, r)
                        || NeighbourReachableForExposure(c, r, c + 1, r);
                }

            // 3. 同色连通块：**不因未揭晓问号断开**（与 GameController.FloodFill 不同！这里是暴露判定）；
            //    冻住的格既不做种子也不能被穿过。块内有「直接暴露的未揭晓问号」→ 块内问号全部揭晓。
            var visited = new bool[_columns, _rows];
            for (int c = 0; c < _columns; c++)
                for (int r = 0; r < _rows; r++)
                {
                    if (!HasPixel(c, r) || Blocked(c, r) || visited[c, r] || _frozen[c, r]) continue;

                    int color = _color[c, r];
                    bool hasExposedQuestion = false;
                    var comp = new List<Vector2Int>();
                    var queue2 = new Queue<Vector2Int>();
                    queue2.Enqueue(new Vector2Int(c, r));
                    visited[c, r] = true;

                    while (queue2.Count > 0)
                    {
                        var cur = queue2.Dequeue();
                        comp.Add(cur);
                        if (_directlyExposed[cur.x, cur.y] && _question[cur.x, cur.y] && !_revealed[cur.x, cur.y])
                            hasExposedQuestion = true;

                        for (int d = 0; d < 4; d++)
                        {
                            int nc = cur.x + DC[d], nr = cur.y + DR[d];
                            if (!InRange(nc, nr) || visited[nc, nr]) continue;
                            if (!HasPixel(nc, nr) || Blocked(nc, nr) || _color[nc, nr] != color) continue;
                            if (_frozen[nc, nr]) continue;
                            visited[nc, nr] = true;
                            queue2.Enqueue(new Vector2Int(nc, nr));
                        }
                    }

                    if (!hasExposedQuestion) continue;
                    for (int i = 0; i < comp.Count; i++)
                        if (_question[comp[i].x, comp[i].y]) _revealed[comp[i].x, comp[i].y] = true;
                }
        }

        private bool NeighbourReachableForExposure(int c, int r, int nc, int nr)
        {
            if (!InRange(nc, nr) || !_reachableEmpty[nc, nr]) return false;
            int owner = _gateOwner[nc, nr];
            if (owner < 0) return true;
            return _gateRegion[owner][c, r];
        }

        private bool IceHasExposedMember(IceState ice)
        {
            foreach (var cell in ice.cells)
                if (InRange(cell.x, cell.y) && _directlyExposed[cell.x, cell.y]) return true;
            return false;
        }

        // ===== 小工具 =====

        private void RefreshFrozen()
        {
            for (int c = 0; c < _columns; c++)
                for (int r = 0; r < _rows; r++)
                    _frozen[c, r] = false;
            for (int i = 0; i < _ices.Count; i++)
            {
                var ice = _ices[i];
                if (ice.remaining <= 0) continue;
                foreach (var cell in ice.cells)
                    if (InRange(cell.x, cell.y)) _frozen[cell.x, cell.y] = true;
            }
        }

        private void RefreshCrateMask()
        {
            for (int c = 0; c < _columns; c++)
                for (int r = 0; r < _rows; r++)
                    _crateMask[c, r] = false;
            for (int i = 0; i < _crates.Count; i++)
            {
                var crate = _crates[i];
                if (crate.destroyed) continue;
                for (int c = crate.c0; c <= crate.c1; c++)
                    for (int r = crate.r0; r <= crate.r1; r++)
                        _crateMask[c, r] = true;
            }
        }

        private void SetBoxBodyLive(BoxState box, bool live)
        {
            for (int c = box.c0; c <= box.c1; c++)
                for (int r = box.r0; r <= box.r1; r++)
                    _boxLive[c, r] = live;
        }

        private void ClearCell(Vector2Int cell)
        {
            if (!InRange(cell.x, cell.y)) return;
            _color[cell.x, cell.y] = Empty;
            _question[cell.x, cell.y] = false;
            _revealed[cell.x, cell.y] = false;
        }

        private bool IsolatedAtInit(int c, int r)
        {
            return _initFrozen[c, r] || (_question[c, r] && !_initRevealed[c, r]);
        }

        /// <summary>木箱处「这两颗算不算相邻」：两颗都不在木箱里 → 相邻；同属一个木箱 → 相邻；其余不相邻。</summary>
        private bool CrateAdjacentOk(Vector2Int a, Vector2Int b)
        {
            int ca = CrateIndexAt(a);
            int cb = CrateIndexAt(b);
            if (ca < 0 && cb < 0) return true;
            return ca >= 0 && ca == cb;
        }

        private int CrateIndexAt(Vector2Int cell)
        {
            for (int i = 0; i < _crates.Count; i++)
            {
                var crate = _crates[i];
                if (cell.x >= crate.c0 && cell.x <= crate.c1
                    && cell.y >= crate.r0 && cell.y <= crate.r1) return i;
            }
            return -1;
        }

        private bool InAnyBoxBody(int c, int r)
        {
            for (int i = 0; i < _boxes.Count; i++)
            {
                var box = _boxes[i];
                if (c >= box.c0 && c <= box.c1 && r >= box.r0 && r <= box.r1) return true;
            }
            return false;
        }

        private bool InRange(int c, int r)
        {
            return c >= 0 && c < _columns && r >= 0 && r < _rows;
        }

        private bool HasPixel(int c, int r)
        {
            return _color[c, r] != Empty;
        }

        /// <summary>障碍：墙/管道自身格、覆盖中的木箱、关着的箱体（= PixelGroup.IsBlocked）。</summary>
        private bool Blocked(int c, int r)
        {
            return _barrier[c, r] || _crateMask[c, r] || _boxLive[c, r];
        }

        /// <summary>
        /// 绕开管道轨道的代价：对每条管道单独试一次 —— **只**把它的轨道当障碍时该组就到不了首排
        /// （而放开就能到）⇒ 这条路线依赖它，加上「单波容量 × 波数」（= 该管道全部波次加起来的元素数）。
        /// 多条管道各自独立判一次，依赖几条就累加几条。
        /// 只看几何（该组的路线是否穿过管道路径），与实际第几轮无关。
        /// </summary>
        private int PipePenalty(Group g)
        {
            int penalty = 0;
            for (int i = 0; i < _pipes.Count; i++)
            {
                var p = _pipes[i];
                if (CanReachFront(g, p.covered)) continue;   // 只挡它一条也照样能到 ⇒ 不依赖它
                penalty += p.waveElements * p.colors.Length;
            }
            return penalty;
        }

        private bool EmptyForExposure(int c, int r)
        {
            return !HasPixel(c, r) && !Blocked(c, r);
        }
    }
}
