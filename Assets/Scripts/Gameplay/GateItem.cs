using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CrowdMatch
{
    /// <summary>
    /// 倍乘门：在像素网格上占一条**笔直的线段**（起点/终点像素格标记，必须轴对齐）。
    /// 像素寻路离开 PixelGroup 时，凡是「正要离开本门格子」的，就在门格里裂变出分身，
    /// 最终 1 颗进去、N 颗（<see cref="multiplier"/>）出来。
    ///
    /// **门格不阻挡任何东西**：不进墙/管道/箱子占用表，也不改 IsBlocked。
    /// 门是区域的唯一出口，寻路 BFS、暴露 BFS、CanReachFront 都必须能从门前流进区域；
    /// 若把门格当墙，区域内像素永远不可点击、也永远寻不出路，关卡直接死局。
    /// 门格的「围栏」作用只体现在闭合区域求解里（见 <see cref="GateRegion"/>）。
    ///
    /// 可见表现走预制体（同 PipeItem 的路子）：本体网格的本地 +X 沿门格延展方向（竖门绕 Y 转 90°）、
    /// 延展轴按门占据的格数缩放，另有一个 UI Text 显示倍数（如 "x2"）。
    /// </summary>
    public class GateItem : MonoBehaviour
    {
        /// <summary>倍乘门创建时被清掉的像素，供「移除并还原 Pixel」回填。</summary>
        [System.Serializable]
        public struct ClearedPixel
        {
            public int col;
            public int row;
            public int colorId;
            public bool isQuestion;
        }

        [Header("门")]
        [Tooltip("起点格（网格坐标：x = 列 col，y = 行 row）")]
        public Vector2 start;

        [Tooltip("终点格（网格坐标）。与起点必须同行或同列（轴对齐线段）")]
        public Vector2 end;

        [Tooltip("倍数 N：1 颗像素从这里出去会变成 N 颗。1 = 只当通道、不倍乘")]
        [Min(1)]
        public int multiplier = 2;

        [Tooltip("创建本门时清掉的像素快照（供移除时还原，勿手改）")]
        [HideInInspector]
        public List<ClearedPixel> clearedPixels = new List<ClearedPixel>();

        [Header("显示")]
        [Tooltip("门本体网格 Transform（其本地 +X 沿门格延展方向；竖门由脚本绕 Y 转 90°）；留空自动取子物体首个带 MeshFilter 的物体")]
        public Transform bodyMesh;

        [Tooltip("本体网格在「1 格」时的延展轴（本地 X）缩放。脚本按 门占据的格数 乘上去（格数 = 首尾格距 + 1）；" +
                 "网格按单位长度制作时把这里填成格距（unitSize + spacing），不必重做网格。只改 X，Y/Z 保持预制体原值")]
        public float bodyCellScale = 1f;

        [Tooltip("倍数数字（UI Text，留空自动从子物体查找），显示为 x{倍数}")]
        public Text multiplierText;

        [Tooltip("左柱（可选，留空则不动）：摆到门线段「格序小」的一端（colMin / rowMin 侧），" +
                 "在基准位置基础上向外偏移 (格数-1)/2 × 该轴向的网格实际格距")]
        public Transform leftPillar;

        [Tooltip("右柱（可选，留空则不动）：摆到门线段「格序大」的一端（colMax / rowMax 侧），偏移同上")]
        public Transform rightPillar;

        [Tooltip("左右柱的**基准**局部位置（= 偏移量为 0 时柱子该在的位置），首次摆放时从柱子当前局部位置自动记录，" +
                 "勿手改；实际位置 = 基准 + 延展轴偏移，所以预制体上美术手调的 X 不会被覆盖")]
        [SerializeField, HideInInspector] private Vector3 _leftPillarBase;
        [SerializeField, HideInInspector] private Vector3 _rightPillarBase;
        [SerializeField, HideInInspector] private bool _leftPillarBaseRecorded;
        [SerializeField, HideInInspector] private bool _rightPillarBaseRecorded;

        [Header("Gizmos")]
        [Tooltip("门格的颜色")]
        public Color gizmoColor = new Color(0.45f, 0.85f, 1f, 0.75f);

        [Tooltip("闭合区域内部的颜色（只在网格重建后可见）")]
        public Color regionGizmoColor = new Color(0.2f, 0.95f, 0.55f, 0.18f);

        /// <summary>所属 PixelGroup（由 PixelGroup.RebuildGrid 赋值，不序列化）。</summary>
        [System.NonSerialized] public PixelGroup group;

        /// <summary>本门占据的格集合（由 <see cref="RefreshCells"/> 从 start/end 算出）。</summary>
        [System.NonSerialized] public readonly HashSet<Vector2Int> cells = new HashSet<Vector2Int>();

        /// <summary>本门格子掩码 [column, row]（由 PixelGroup.RebuildGrid 填）。</summary>
        [System.NonSerialized] public bool[,] cellMask;

        /// <summary>本门闭合区域内部的格掩码（由 PixelGroup.RebuildGrid 填）。</summary>
        [System.NonSerialized] public bool[,] regionMask;

        [System.NonSerialized] private PixelGroup _gizmoGroup;

        /// <summary>本门占据的格数。</summary>
        public int CellCount => cells.Count;

        /// <summary>所属 PixelGroup（编辑器 Gizmos 用，惰性缓存）。</summary>
        public PixelGroup Group
        {
            get
            {
                if (_gizmoGroup == null)
                    _gizmoGroup = GetComponentInParent<PixelGroup>();
                return _gizmoGroup;
            }
        }

        private void Awake()
        {
            if (multiplierText == null)
                multiplierText = GetComponentInChildren<Text>(true);
        }

        /// <summary>把网格坐标（浮点）换算为整格格坐标（就近取整）。</summary>
        public static Vector2Int ToCell(Vector2 p)
        {
            return new Vector2Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y));
        }

        /// <summary>
        /// 枚举一段轴对齐线段经过的所有网格格（含两端与中间格）。
        /// 起步/步长写法与 WallItem.EnumerateSegment 一致。
        /// </summary>
        public static void CollectCells(Vector2 a, Vector2 b, HashSet<Vector2Int> set)
        {
            if (set == null)
                return;

            Vector2Int ca = ToCell(a);
            Vector2Int cb = ToCell(b);

            int steps = Mathf.Max(Mathf.Abs(cb.x - ca.x), Mathf.Abs(cb.y - ca.y));
            int dx = System.Math.Sign(cb.x - ca.x);
            int dy = System.Math.Sign(cb.y - ca.y);

            set.Add(ca);
            for (int i = 1; i <= steps; i++)
                set.Add(new Vector2Int(ca.x + dx * i, ca.y + dy * i));
        }

        /// <summary>按当前 start/end 重算 <see cref="cells"/>（字段被 Inspector 改动后由 RebuildGrid / 编辑器调用）。</summary>
        public void RefreshCells()
        {
            cells.Clear();
            CollectCells(start, end, cells);
        }

        /// <summary>校验线段是否轴对齐（起点与终点必须同行或同列、且不是同一格）。返回错误描述，null = 有效。</summary>
        public static bool IsValidSegment(Vector2 start, Vector2 end, out string error)
        {
            if (Mathf.Approximately(start.x, end.x) && Mathf.Approximately(start.y, end.y))
            {
                error = "起点与终点是同一格，至少需要 2 格。";
                return false;
            }
            if (!Mathf.Approximately(start.x, end.x) && !Mathf.Approximately(start.y, end.y))
            {
                error = "起点(" + start + ")与终点(" + end + ")既不共列也不共行，门必须是轴对齐的笔直线段。";
                return false;
            }
            error = null;
            return true;
        }

        /// <summary>校验本门线段是否轴对齐。返回错误描述，null = 有效。</summary>
        public bool IsValid(out string error)
        {
            return IsValidSegment(start, end, out error);
        }

        /// <summary>分配本门的掩码数组（由 PixelGroup.RebuildGrid 调用）。</summary>
        public void ResetMasks(int columns, int totalRows)
        {
            cellMask = new bool[columns, totalRows];
            regionMask = new bool[columns, totalRows];

            foreach (var cell in cells)
            {
                if (cell.x < 0 || cell.x >= columns || cell.y < 0 || cell.y >= totalRows)
                    continue;
                cellMask[cell.x, cell.y] = true;
            }
        }

        /// <summary>该格是否是本门的门格。</summary>
        public bool IsCell(int col, int row)
        {
            if (cellMask == null)
                return false;
            if (col < 0 || row < 0 || col >= cellMask.GetLength(0) || row >= cellMask.GetLength(1))
                return false;
            return cellMask[col, row];
        }

        /// <summary>该格是否落在本门的闭合区域内。</summary>
        public bool IsInRegion(int col, int row)
        {
            if (regionMask == null)
                return false;
            if (col < 0 || row < 0 || col >= regionMask.GetLength(0) || row >= regionMask.GetLength(1))
                return false;
            return regionMask[col, row];
        }

        /// <summary>本门闭合区域的格数（未围出任何区域时为 0 = 不闭合）。</summary>
        public int RegionCellCount()
        {
            if (regionMask == null)
                return 0;
            int n = 0;
            for (int c = 0; c < regionMask.GetLength(0); c++)
                for (int r = 0; r < regionMask.GetLength(1); r++)
                    if (regionMask[c, r])
                        n++;
            return n;
        }

        /// <summary>
        /// 摆放可见表现：根定位到整段中心且**根不旋转**（只转本体网格子物体），
        /// 本体网格的本地 +X 沿门格延展方向（竖门绕 Y 转 90°）、延展轴按**门占据的格数**缩放，数字显示 x{倍数}。
        /// 根不旋转是关键——数字是根的子物体，于是横门、竖门的数字都保持预制体朝向，不会躺倒或镜像。
        /// 编辑器改字段后与运行时生成走的是同一个方法，不存在两套摆放逻辑。
        /// </summary>
        public void BuildVisual(PixelGroup pg)
        {
            if (pg == null)
                return;

            RefreshCells();
            if (cells.Count == 0)
                return;

            // 端点格（轴对齐线段的首尾；min/max 对横竖两种走向都成立）
            Vector2Int ca = ToCell(start);
            Vector2Int cb = ToCell(end);
            int colMin = Mathf.Min(ca.x, cb.x);
            int colMax = Mathf.Max(ca.x, cb.x);
            int rowMin = Mathf.Min(ca.y, cb.y);
            int rowMax = Mathf.Max(ca.y, cb.y);

            // 根：整段中心（首尾格中心的中点即整段几何中心），不旋转
            Vector3 a = pg.GetLocalPosition(colMin, rowMin);
            Vector3 b = pg.GetLocalPosition(colMax, rowMax);
            transform.localPosition = (a + b) * 0.5f;
            transform.localRotation = Quaternion.identity;

            // 门线段的**世界**方向（格序小端 → 格序大端）；本体网格与左右柱都用它换算各自的轴向。
            // 取 min → max 而不是 start → end，是为了让左右柱的「左 / 右」与点选顺序无关；
            // 本体只取绝对值判轴向，所以这个改动对本体无影响。
            Vector3 worldDir = pg.GetWorldPosition(colMax, rowMax) - pg.GetWorldPosition(colMin, rowMin);

            Transform body = bodyMesh != null ? bodyMesh : FindBodyMesh();
            if (body != null)
            {
                // 延展方向：只分轴向、不区分正负——轴对齐线段只有两种走向。
                // 横门（沿列）× 本体本地 +X 本来就对上网格 X，保持预制体原朝向；
                // 竖门（沿行）绕 Y 转 90°，让本地 +X 对上网格 Z。
                // 方向先换算到 PixelGroup 局部空间，这样外层被整体旋转过也成立。
                Vector3 dir = body.parent != null
                    ? body.parent.InverseTransformDirection(worldDir)
                    : worldDir;
                Vector3 axis = Mathf.Abs(dir.x) >= Mathf.Abs(dir.z) ? Vector3.right : Vector3.forward;
                body.localRotation = Quaternion.FromToRotation(Vector3.right, axis);

                // 长度：延展轴（本地 X）缩放 = 「1 格」的缩放 × **门占据的格数**。
                // 格数 = 首尾格距 + 1——起点格与终点格各自算一整格，两者都在门的范围内。
                // 注意**不是**倍数 N：倍数是给人看的数字（x2），与门的长度无关。
                // 只改延展轴，Y/Z 保持预制体原值，避免把美术手调的缩放覆盖掉。
                Vector3 scale = body.localScale;
                scale.x = bodyCellScale * cells.Count;
                body.localScale = scale;
            }

            ApplyPillars(pg, worldDir, cells.Count);

            UpdateDisplay();
        }

        /// <summary>
        /// 摆放左右柱：<c>实际位置 = 基准位置 ± 延展轴偏移</c>，偏移量 = <c>(格数-1)/2 × 该轴向的网格实际格距</c>
        /// —— 即从整段中心移到首格 / 尾格**格心**的距离。
        ///
        /// 口径（已与用户核对）：
        /// · **单位是网格实际格距**（横门取 <see cref="PixelGroup.CellSizeX"/>、竖门取
        ///   <see cref="PixelGroup.CellSizeZ"/>），不是 <see cref="bodyCellScale"/>、也不是 unitSize；
        ///   于是 spacingX ≠ spacingZ 时横门与竖门的偏移量不同。
        /// · **在基准位置基础上偏移，不覆盖基准**：柱子预制体上美术手调的 X 得以保留。
        ///   基准只记录一次（见 <see cref="CapturePillarBase"/>），之后每次重建都是「基准 + 偏移」。
        /// · **生效时机与本体一致**：在每次 <see cref="BuildVisual"/> 里重算（创建菜单 /「重建显示」/
        ///   关卡初始化三条路都会走到）。
        /// · 只写延展轴的那**一个**分量，另两个分量保留预制体原值（同木箱钉子的做法）。
        /// · 「左柱」= 格序小的一端（colMin / rowMin），「右柱」= 格序大的一端；横竖两种走向都成立，
        ///   与点选顺序无关。偏移本身对称，只有左右柱模型不对称时这个区分才有意义。
        /// · **不改柱子旋转**：若柱子模型有朝向需求（例如柱头要朝门内），需要另行补一条旋转规则。
        /// </summary>
        private void ApplyPillars(PixelGroup pg, Vector3 worldDir, int cellCount)
        {
            if (leftPillar == null && rightPillar == null)
                return;

            // 横 / 竖以 **PixelGroup 局部空间**判定（worldDir 在世界空间，外层整体旋转过就不准了）
            Vector3 dirInGroup = pg.transform.InverseTransformDirection(worldDir);
            float pitch = Mathf.Abs(dirInGroup.x) >= Mathf.Abs(dirInGroup.z)
                ? pg.CellSizeX
                : pg.CellSizeZ;

            float offset = (Mathf.Max(1, cellCount) - 1) * 0.5f * pitch;

            if (leftPillar != null)
            {
                CapturePillarBase(leftPillar, ref _leftPillarBase, ref _leftPillarBaseRecorded);
                PlacePillar(leftPillar, _leftPillarBase, worldDir, -offset);
            }

            if (rightPillar != null)
            {
                CapturePillarBase(rightPillar, ref _rightPillarBase, ref _rightPillarBaseRecorded);
                PlacePillar(rightPillar, _rightPillarBase, worldDir, offset);
            }
        }

        /// <summary>
        /// 记录柱子的基准局部位置（= 偏移量为 0 时该在的位置），**只记一次**。
        ///
        /// 必须只记一次、且必须序列化：若每次都从「当前局部位置」重记，记到的会是上一次已经偏移过的位置，
        /// 反复重建就会累积漂移（同 IceItem 的基准值被域重载反复捕获那类问题）。
        /// 持久化由调用方负责 —— 编辑器两条路径（创建菜单、Inspector 的「重建显示」）之后都会
        /// <c>SetDirty(gate)</c>；Play 模式下不写盘也无妨，下次实例化从预制体重新捕获即可。
        /// </summary>
        private static void CapturePillarBase(Transform pillar, ref Vector3 basePos, ref bool recorded)
        {
            if (recorded)
                return;

            basePos = pillar.localPosition;
            recorded = true;
        }

        /// <summary>
        /// 重新记录左右柱的基准位置：把两根柱子**当前**的局部位置当作基准（即「偏移量为 0 时该在的位置」）。
        /// 所以用之前要先把柱子摆到「只有 1 格门」时的位置；下一次 <see cref="BuildVisual"/> 会按新基准重新偏移。
        /// 供 Inspector 的「重新记录基准位置」按钮使用（基准字段是 HideInInspector，没有别的修正途径）。
        /// </summary>
        public void RecapturePillarBase()
        {
            _leftPillarBaseRecorded = false;
            _rightPillarBaseRecorded = false;
        }

        /// <summary>
        /// 把一根柱子放到「基准位置 ± offset」（沿延展轴）。延展轴在**柱子自己的父空间**里换算并取主轴，
        /// 与本体网格换算轴向的写法一致；这样柱子挂在与本体不同的父物体下（甚至父级带旋转）也成立。
        /// </summary>
        private static void PlacePillar(Transform pillar, Vector3 basePos, Vector3 worldDir, float offset)
        {
            Vector3 dir = pillar.parent != null
                ? pillar.parent.InverseTransformDirection(worldDir)
                : worldDir;
            bool horizontal = Mathf.Abs(dir.x) >= Mathf.Abs(dir.z);

            // 基准只在延展轴那一个分量上参与计算；另两个分量取预制体当前值，保证美术后调的 y / z 也生效
            Vector3 p = pillar.localPosition;
            pillar.localPosition = horizontal
                ? new Vector3(basePos.x + offset, p.y, p.z)
                : new Vector3(p.x, p.y, basePos.z + offset);
        }

        private Transform FindBodyMesh()
        {
            foreach (var f in GetComponentsInChildren<MeshFilter>(true))
            {
                if (f == null || f.transform == transform)
                    continue;
                // 左右柱一般也带 MeshFilter：本体自动查找必须跳过它们（含它们的子物体），
                // 否则「bodyMesh 留空」时会把柱子认成门本体并给它套上门的长度缩放。
                if (leftPillar != null && f.transform.IsChildOf(leftPillar))
                    continue;
                if (rightPillar != null && f.transform.IsChildOf(rightPillar))
                    continue;
                return f.transform;
            }
            return null;
        }

        /// <summary>刷新倍数数字。</summary>
        public void UpdateDisplay()
        {
            if (multiplierText == null)
                multiplierText = GetComponentInChildren<Text>(true);
            if (multiplierText != null)
                multiplierText.text = "x" + Mathf.Max(1, multiplier);
        }

        /// <summary>编辑器 / 运行时的可视化：门格 + 线段，另用第二种颜色标出闭合区域内部（网格重建后才有）。</summary>
        private void OnDrawGizmos()
        {
            var pg = Group;
            if (pg == null)
                return;

            var drawn = new HashSet<Vector2Int>();
            CollectCells(start, end, drawn);

            Color prev = Gizmos.color;

            // 线段
            Gizmos.color = gizmoColor;
            Vector3 wa = pg.GetWorldPosition(ToCell(start).x, ToCell(start).y);
            Vector3 wb = pg.GetWorldPosition(ToCell(end).x, ToCell(end).y);
            Gizmos.DrawLine(wa, wb);

            // 门格
            var cellSize = new Vector3(pg.CellSizeX * 0.9f, 0.04f, pg.CellSizeZ * 0.9f);
            foreach (var cell in drawn)
            {
                if (!pg.IsInRange(cell.x, cell.y))
                    continue;
                Gizmos.DrawCube(pg.GetWorldPosition(cell.x, cell.y), cellSize);
            }

            // 闭合区域内部（regionMask 只在网格重建后才有；编辑器改字段后会重建）
            if (regionMask != null)
            {
                Gizmos.color = regionGizmoColor;
                var regionCellSize = new Vector3(pg.CellSizeX, 0.02f, pg.CellSizeZ);
                for (int c = 0; c < regionMask.GetLength(0); c++)
                    for (int r = 0; r < regionMask.GetLength(1); r++)
                    {
                        if (!regionMask[c, r] || !pg.IsInRange(c, r))
                            continue;
                        Gizmos.DrawCube(pg.GetWorldPosition(c, r), regionCellSize);
                    }
            }

            Gizmos.color = prev;
        }
    }
}
