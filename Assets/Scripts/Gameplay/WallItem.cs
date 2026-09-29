using System.Collections.Generic;
using UnityEngine;

namespace CrowdMatch
{
    /// <summary>墙体视觉部件类型：独立 1×1、端点、边（直段中间）、角（转角）、T 字交叉、十字交叉。</summary>
    public enum WallPieceType
    {
        Single,
        End,
        Edge,
        Corner,
        Tee,
        Cross,
    }

    /// <summary>
    /// 墙体单位：必须作为 PixelGroup 的子物体。用一组网格坐标端点（Vector2：x = 列 col，y = 行 row）
    /// 定义若干段墙体，相邻两个端点构成一段，每段必须平行于 X 或 Z 轴（端点 x 或 y 相等）。
    /// 墙体占据其经过的所有网格格（含端点与中间格），在暴露与寻路逻辑中作为障碍，
    /// 阻止 Pixel 穿过墙体离开。Gizmos 无论是否选中都会显示。
    ///
    /// 墙块（实体预制体）**不由本组件自己拼**：交叉格要判成 T / 十字，掩码必须包含**别的墙**的占格，
    /// 所以统一由 <see cref="PixelGroup.RebuildWallVisuals"/> 按全组并集分类、再回调
    /// <see cref="SpawnPiece"/> 挂到**拥有该格的那面墙**下。
    /// </summary>
    public class WallItem : MonoBehaviour
    {
        [Header("墙体")]
        [Tooltip("墙体端点（网格坐标：x = 列 col，y = 行 row）。相邻两点构成一段，每段必须平行于 X 或 Z 轴。")]
        public List<Vector2> points = new List<Vector2>();

        [Tooltip("闭环墙体：true 时首尾之间自动补一条闭合段（占用格、视觉、Gizmos 均包含该段），并作为封闭障碍包围内部 Pixel。")]
        [HideInInspector]
        public bool closed;

        [Tooltip("墙高度（世界单位，仅用于 Gizmos 显示）")]
        public float height = 2f;

        [Tooltip("Gizmos 显示颜色")]
        public Color gizmoColor = new Color(1f, 0.35f, 0.35f, 0.65f);

        /// <summary>所属 PixelGroup（由 PixelGroup.RebuildGrid 赋值，不序列化）。</summary>
        [System.NonSerialized] public PixelGroup group;

        [System.NonSerialized] private PixelGroup _gizmoGroup;

        /// <summary>查找所属 PixelGroup（编辑器 Gizmos 用，惰性缓存）。</summary>
        public PixelGroup Group
        {
            get
            {
                if (_gizmoGroup == null)
                    _gizmoGroup = GetComponentInParent<PixelGroup>();
                return _gizmoGroup;
            }
        }

        /// <summary>把网格坐标（浮点）换算为整格格坐标（就近取整）。</summary>
        public static Vector2Int ToCell(Vector2 p)
        {
            return new Vector2Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y));
        }

        /// <summary>校验所有段是否都平行于 X 或 Z 轴；返回错误描述（null = 有效）。</summary>
        public bool IsValid(out string error)
        {
            for (int i = 0; i + 1 < points.Count; i++)
            {
                Vector2 a = points[i];
                Vector2 b = points[i + 1];
                if (!Mathf.Approximately(a.x, b.x) && !Mathf.Approximately(a.y, b.y))
                {
                    error = "第 " + (i + 1) + " 段（" + a + " → " + b + "）不平行于 X/Z 轴，端点需满足 x 或 y 相等。";
                    return false;
                }
            }
            error = null;
            return true;
        }

        /// <summary>
        /// 校验能否闭环：返回 null = 可闭环，否则返回失败原因。
        /// 闭环要求（首尾自动补一条闭合段）：
        /// 1. 首点→下一点的延伸方向，与尾点→上一点的延伸方向，必须同为横向（x 变）或同为纵向（y 变）；
        /// 2. 同为横向时，首尾点需同列（纵向闭合）；同为纵向时，首尾点需同行（横向闭合）。
        /// </summary>
        public string CheckClosable()
        {
            if (points == null || points.Count < 3)
                return "至少需要 3 个端点才能闭环。";

            string segErr;
            if (!IsValid(out segErr))
                return "墙体存在非轴对齐段，无法闭环：\n" + segErr;

            Vector2 first = points[0];
            Vector2 second = points[1];
            Vector2 last = points[points.Count - 1];
            Vector2 prev = points[points.Count - 2];

            bool firstHorizontal = !Mathf.Approximately(first.x, second.x);
            bool lastHorizontal = !Mathf.Approximately(prev.x, last.x);

            if (firstHorizontal != lastHorizontal)
                return "首尾两端的延伸方向一个横向、一个纵向，无法形成矩形闭环。";

            if (firstHorizontal)
            {
                if (!Mathf.Approximately(first.x, last.x))
                    return "两端均为横向时，首尾点需在同一列（纵向闭合）：当前首点列 " + first.x + "、尾点列 " + last.x + " 不一致。";
            }
            else
            {
                if (!Mathf.Approximately(first.y, last.y))
                    return "两端均为纵向时，首尾点需在同一行（横向闭合）：当前首点行 " + first.y + "、尾点行 " + last.y + " 不一致。";
            }
            return null;
        }

        /// <summary>枚举一段墙经过的所有网格格（含两端与中间格；对角段按逐格阶梯枚举作为兜底）。</summary>
        private static void EnumerateSegment(Vector2 a, Vector2 b, HashSet<Vector2Int> set)
        {
            Vector2Int ca = ToCell(a);
            Vector2Int cb = ToCell(b);

            int steps = Mathf.Max(Mathf.Abs(cb.x - ca.x), Mathf.Abs(cb.y - ca.y));
            int dx = System.Math.Sign(cb.x - ca.x);
            int dy = System.Math.Sign(cb.y - ca.y);

            set.Add(ca);
            for (int i = 1; i <= steps; i++)
                set.Add(new Vector2Int(ca.x + dx * i, ca.y + dy * i));
        }

        /// <summary>
        /// 把一组端点（网格坐标）换算为占据的网格格集合（静态，供关卡加载等无实例场景复用）。
        /// **1 个端点 = 1×1 墙**：没有线段可枚举，那个端点自己就是它占据的唯一一格。
        /// </summary>
        public static void CollectOccupiedCells(IReadOnlyList<Vector2> points, HashSet<Vector2Int> set)
        {
            if (points == null || points.Count == 0)
                return;

            if (points.Count == 1)
            {
                set.Add(ToCell(points[0]));   // 1×1 墙
                return;
            }

            for (int i = 0; i + 1 < points.Count; i++)
                EnumerateSegment(points[i], points[i + 1], set);
        }

        /// <summary>把一组端点换算为占据的网格格集合；closed = true 时额外补首尾闭合段（供无实例场景复用）。</summary>
        public static void CollectOccupiedCells(IReadOnlyList<Vector2> points, bool closed, HashSet<Vector2Int> set)
        {
            CollectOccupiedCells(points, set);
            if (closed && points != null && points.Count >= 2)
                EnumerateSegment(points[points.Count - 1], points[0], set);
        }

        /// <summary>枚举墙体占据的所有网格格（去重；闭环时包含首尾闭合段）。</summary>
        public IEnumerable<Vector2Int> EnumerateOccupiedCells()
        {
            var set = new HashSet<Vector2Int>();
            CollectOccupiedCells(points, closed, set);
            return set;
        }

        /// <summary>墙体占据的网格格数量。</summary>
        public int OccupiedCellCount()
        {
            int n = 0;
            foreach (var _ in EnumerateOccupiedCells())
                n++;
            return n;
        }

        /// <summary>清掉本墙身上的所有墙块（整组重建前由 <see cref="PixelGroup.RebuildWallVisuals"/> 调用）。</summary>
        public void ClearPieces()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }

        /// <summary>
        /// 在本墙身上为指定格生成一个墙块。**分类不在这里算** —— 交叉格（T / 十字）的掩码要把
        /// **别的墙**的占格也算进去，只有知道「全组并集」的调用方（<see cref="PixelGroup.RebuildWallVisuals"/>）
        /// 才能判对，所以类型与朝向由调用方传入。
        /// 定位到格中心、scale = unitSize；缺少对应预制体时告警并返回 false。
        /// </summary>
        public bool SpawnPiece(PixelGroup pg, Vector2Int cell, WallPieceType type, float yaw)
        {
            var prefab = ChoosePrefab(pg, type);
            if (prefab == null)
            {
                Debug.LogWarning("[WallItem] 缺少" + type + "预制体，跳过墙体格 (" + cell.x + "," + cell.y + ")。");
                return false;
            }

            var piece = PrefabSpawner.Instantiate(prefab, transform);
            if (piece == null)
                return false;

            piece.name = "WallPiece_" + type + "_" + cell.y + "_" + cell.x;
            piece.transform.localPosition = pg.GetLocalPosition(cell.x, cell.y);
            piece.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            piece.transform.localScale = Vector3.one * pg.unitSize;
            return true;
        }

        private GameObject ChoosePrefab(PixelGroup pg, WallPieceType type)
        {
            switch (type)
            {
                case WallPieceType.Single: return pg.wallSinglePrefab;
                case WallPieceType.End:    return pg.wallEndPrefab;
                case WallPieceType.Edge:   return pg.wallEdgePrefab;
                case WallPieceType.Tee:    return pg.wallTeePrefab;
                case WallPieceType.Cross:  return pg.wallCrossPrefab;
                default:                   return pg.wallCornerPrefab;
            }
        }

        /// <summary>
        /// 把一个墙体格按**四向邻接掩码**分类并给出绕 Y 轴的对齐角度。
        /// 掩码由调用方给出（<see cref="PixelGroup.RebuildWallVisuals"/> 传全组墙格并集算出的四向），
        /// 所以跨墙的交叉格也能判对 —— 分类本身与「这个格属于哪面墙」无关。
        /// 0 邻 = 独立 1×1（0°）；1 邻 = 端点（朝相邻方向）；2 邻共线 = 边（水平 90°、竖直 0°）；
        /// 2 邻垂直 = 角；3 邻 = T 字；4 邻 = 十字。
        /// </summary>
        public static WallPieceType Classify(bool right, bool left, bool front, bool back, out float yaw)
        {
            int n = (right ? 1 : 0) + (left ? 1 : 0) + (front ? 1 : 0) + (back ? 1 : 0);

            if (n == 0)
            {
                yaw = 0f;
                return WallPieceType.Single;
            }

            if (n == 1)
            {
                int dc = right ? 1 : (left ? -1 : 0);
                int dr = back ? 1 : (front ? -1 : 0);
                yaw = DirYaw(dc, dr);
                return WallPieceType.End;
            }

            if (n == 2 && ((right && left) || (front && back)))
            {
                yaw = (right && left) ? 90f : 0f;
                return WallPieceType.Edge;
            }

            if (n == 2)
            {
                yaw = CornerYaw(right, left, front, back);
                return WallPieceType.Corner;
            }

            if (n == 3)
            {
                yaw = TeeYaw(right, left, front, back);
                return WallPieceType.Tee;
            }

            yaw = 0f;   // 十字四向对称
            return WallPieceType.Cross;
        }

        /// <summary>
        /// 按「**覆盖了这一格的那些线段**」分类：掩码 = 这些线段在本格贡献的臂的并集。
        ///
        /// 为什么不是「谁的格子挨着」：相邻不等于连着。两种都会判错的场景 ——
        /// ① **同一面墙**折返跑回来贴着自己（比如 ∏ 形），两条臂贴着但不是连着的，按相邻会误判成 T；
        /// ② **两面墙**首尾相接，各是各的墙，端点必须各自保留。
        /// 只有「线段真的穿过/停在这一格」才该贡献臂 —— 于是「同一格被两条线段覆盖」自然叠成 T / 十字，
        /// 跨墙与墙内**同一条口径**（见 Docs/WallJunctionDesign.md §5.2）。
        /// </summary>
        public const int ArmRight = 1;   // +col（右 / +X）
        public const int ArmLeft  = 2;   // -col（左 / -X）
        public const int ArmBack  = 4;   // +row（后 / -Z）
        public const int ArmFront = 8;   // -row（前 / +Z）

        /// <summary>
        /// 按**线段的连接关系**把这面墙的臂累加进 <paramref name="arms"/>（格 → 臂掩码）。
        /// 每条线段逐格登记：**内部**的格贡献前后两条臂，**端点**格只贡献朝内那一条。
        /// 可以对多面墙反复调用同一个字典 —— 同一格被各条覆盖它的线段依次 OR，天然得到合并（不需要别的簿记）。
        ///
        /// 1 个端点（1×1 墙）没有任何线段 ⇒ 不贡献臂 ⇒ 掩码 0 ⇒ `Single`。
        /// 退化段（两端同格）与斜段不贡献臂（斜段本就非法，`IsValid` 会拒）。
        /// 注意步进写法必须与 <see cref="EnumerateSegment"/> 一致（同一套格枚举）。
        /// </summary>
        public static void AccumulateArms(IReadOnlyList<Vector2> points, bool closed,
                                          Dictionary<Vector2Int, int> arms,
                                          System.Func<int, int, bool> inRange = null)
        {
            if (points == null || arms == null || points.Count < 2)
                return;

            for (int i = 0; i + 1 < points.Count; i++)
                AddSegmentArms(points[i], points[i + 1], arms, inRange);

            if (closed && points.Count >= 2)
                AddSegmentArms(points[points.Count - 1], points[0], arms, inRange);
        }

        /// <summary>把一条线段的臂逐格登记进 arms。</summary>
        private static void AddSegmentArms(Vector2 a, Vector2 b, Dictionary<Vector2Int, int> arms,
                                           System.Func<int, int, bool> inRange)
        {
            Vector2Int ca = ToCell(a);
            Vector2Int cb = ToCell(b);

            int steps = Mathf.Max(Mathf.Abs(cb.x - ca.x), Mathf.Abs(cb.y - ca.y));
            if (steps <= 0)
                return;   // 退化段：没有方向，不贡献臂

            int dc = System.Math.Sign(cb.x - ca.x);
            int dr = System.Math.Sign(cb.y - ca.y);
            if (dc != 0 && dr != 0)
                return;   // 斜段（非法数据）：不贡献臂，占格仍由 CollectOccupiedCells 兜底

            for (int i = 0; i <= steps; i++)
            {
                var cell = new Vector2Int(ca.x + dc * i, ca.y + dr * i);

                if (i > 0)
                    AddArm(arms, cell, -dc, -dr, inRange);   // 朝线段上的前一个格
                if (i < steps)
                    AddArm(arms, cell, dc, dr, inRange);     // 朝线段上的后一个格
            }
        }

        /// <summary>给某格或上一条朝 (dc,dr) 的臂。目标格越界时不加（与「越界格不参与」同口径）。</summary>
        private static void AddArm(Dictionary<Vector2Int, int> arms, Vector2Int cell, int dc, int dr,
                                   System.Func<int, int, bool> inRange)
        {
            if (inRange != null && !inRange(cell.x + dc, cell.y + dr))
                return;

            arms.TryGetValue(cell, out int mask);
            arms[cell] = mask | ArmBit(dc, dr);
        }

        private static int ArmBit(int dc, int dr)
        {
            if (dc > 0) return ArmRight;
            if (dc < 0) return ArmLeft;
            if (dr > 0) return ArmBack;
            return ArmFront;
        }

        /// <summary>按臂掩码分类（掩码由 <see cref="AccumulateArms"/> 累加得到）。</summary>
        public static WallPieceType ClassifyMask(int arms, out float yaw)
        {
            return Classify((arms & ArmRight) != 0, (arms & ArmLeft) != 0,
                            (arms & ArmFront) != 0, (arms & ArmBack) != 0, out yaw);
        }

        /// <summary>
        /// T 字格：n == 3 时**恰好只有一个方向是空的**，那个方向就是缺口。
        /// 当前约定 = 「主干（缺口对面那条臂）朝本地 +Z」⇒ yaw = DirYaw(缺口方向) + 180°。
        /// 若做出来的 T 块整体朝向差 180°（主干与缺口反了），把下面那行的取负去掉即可切成
        /// 「缺口朝本地 +Z」。预制体的 0° 摆法见 Docs/WallJunctionDesign.md §10-Q1。
        /// </summary>
        private static float TeeYaw(bool right, bool left, bool front, bool back)
        {
            int gapC = !right ? 1 : (!left ? -1 : 0);   // 唯一空缺的列方向
            int gapR = !back ? 1 : (!front ? -1 : 0);   // 唯一空缺的行方向
            return DirYaw(-gapC, -gapR);                // 本地 +Z 指向缺口对面（= 主干）
        }

        /// <summary>网格方向 → 绕 Y 轴角度（度）。世界方向 = (dc, -dr)：+col 右(+X)、-col 左(-X)、-row 前(+Z)、+row 后(-Z)。组件本地 +Z 对齐该方向。</summary>
        private static float DirYaw(int dc, int dr)
        {
            return Mathf.Atan2(dc, -dr) * Mathf.Rad2Deg;
        }

        /// <summary>角格：取两个相邻墙格方向，令组件本地 +X 与 +Z 两臂分别对齐。绕 Y +90° 把 (x,z) 映射为 (z,-x)。</summary>
        private static float CornerYaw(bool right, bool left, bool front, bool back)
        {
            var dirs = new List<Vector2>(2);
            if (right) dirs.Add(new Vector2(1f, 0f));    // +X
            if (left)  dirs.Add(new Vector2(-1f, 0f));   // -X
            if (front) dirs.Add(new Vector2(0f, 1f));    // +Z
            if (back)  dirs.Add(new Vector2(0f, -1f));   // -Z

            Vector2 a = dirs[0];
            Vector2 b = dirs.Count > 1 ? dirs[1] : Vector2.zero;

            // 若 b == +90°(a)，则 a 为基准臂（本地 +Z 对齐 a）；否则 b 为基准臂。
            if (Mathf.Approximately(b.x, a.y) && Mathf.Approximately(b.y, -a.x))
                return Mathf.Atan2(a.x, a.y) * Mathf.Rad2Deg;
            return Mathf.Atan2(b.x, b.y) * Mathf.Rad2Deg;
        }

        /// <summary>某网格格的世界坐标。</summary>
        private Vector3 CellWorld(Vector2 p)
        {
            var g = Group;
            if (g == null)
                return transform.TransformPoint(new Vector3(p.x, 0f, -p.y));
            Vector2Int c = ToCell(p);
            return g.GetWorldPosition(c.x, c.y);
        }

        /// <summary>
        /// 1×1 墙（只有一个端点、没有线段可画）的 Gizmos：画成一个立柱方框。
        /// 没有它的话这种墙在 Scene 视图里只剩一个扁平的占格标记，看不出是墙。
        /// </summary>
        private void DrawSingleCellPanel()
        {
            var g = Group;
            if (g == null || points == null || points.Count == 0)
                return;

            Vector2Int cell = ToCell(points[0]);
            Vector3 c = g.GetWorldPosition(cell.x, cell.y);
            float hx = g.CellSizeX * 0.5f;
            float hz = g.CellSizeZ * 0.5f;

            Vector3[] corners =
            {
                c + new Vector3(-hx, 0f, -hz),
                c + new Vector3( hx, 0f, -hz),
                c + new Vector3( hx, 0f,  hz),
                c + new Vector3(-hx, 0f,  hz),
            };

            for (int i = 0; i < 4; i++)
            {
                Vector3 a = corners[i];
                Vector3 b = corners[(i + 1) % 4];
                Gizmos.DrawLine(a, b);                                              // 底边
                Gizmos.DrawLine(a + Vector3.up * height, b + Vector3.up * height);   // 顶边
                Gizmos.DrawLine(a, a + Vector3.up * height);                        // 竖棱
            }
        }

        /// <summary>Gizmos：无论是否选中都绘制墙体面板与占用格标记。</summary>
        private void OnDrawGizmos()
        {
            var g = Group;
            if (g == null || points == null)
                return;

            Gizmos.color = gizmoColor;

            // 1×1 墙：没有线段可画，单独画一个立柱
            if (points.Count == 1)
                DrawSingleCellPanel();

            // 墙体面板（底边 + 顶边 + 两端竖线）
            for (int i = 0; i + 1 < points.Count; i++)
            {
                Vector3 a = CellWorld(points[i]);
                Vector3 b = CellWorld(points[i + 1]);
                Vector3 aTop = a + Vector3.up * height;
                Vector3 bTop = b + Vector3.up * height;

                Gizmos.DrawLine(a, b);
                Gizmos.DrawLine(aTop, bTop);
                Gizmos.DrawLine(a, aTop);
                Gizmos.DrawLine(b, bTop);
            }

            // 闭环：补画首尾闭合段面板
            if (closed && points.Count >= 2)
            {
                Vector3 a = CellWorld(points[points.Count - 1]);
                Vector3 b = CellWorld(points[0]);
                Vector3 aTop = a + Vector3.up * height;
                Vector3 bTop = b + Vector3.up * height;

                Gizmos.DrawLine(a, b);
                Gizmos.DrawLine(aTop, bTop);
                Gizmos.DrawLine(a, aTop);
                Gizmos.DrawLine(b, bTop);
            }

            // 占用格：底面浅色方块标记（可直观看到墙阻挡了哪些格）
            Color cellColor = gizmoColor;
            cellColor.a = Mathf.Clamp01(gizmoColor.a * 0.4f);
            Gizmos.color = cellColor;
            float halfX = g.CellSizeX * 0.45f;
            float halfZ = g.CellSizeZ * 0.45f;
            foreach (var cell in EnumerateOccupiedCells())
            {
                if (!g.IsInRange(cell.x, cell.y))
                    continue;
                Vector3 center = g.GetWorldPosition(cell.x, cell.y);
                Gizmos.DrawCube(center, new Vector3(halfX * 2f, 0.04f, halfZ * 2f));
            }
        }

        /// <summary>选中时额外绘制白色描边，便于定位端点。</summary>
        private void OnDrawGizmosSelected()
        {
            var g = Group;
            if (g == null || points == null)
                return;

            Color prev = Gizmos.color;
            Gizmos.color = Color.white;

            // 1×1 墙：没有线段可描边，画立柱
            if (points.Count == 1)
                DrawSingleCellPanel();

            for (int i = 0; i + 1 < points.Count; i++)
            {
                Vector3 a = CellWorld(points[i]);
                Vector3 b = CellWorld(points[i + 1]);
                Vector3 aTop = a + Vector3.up * height;
                Vector3 bTop = b + Vector3.up * height;
                Gizmos.DrawLine(a, b);
                Gizmos.DrawLine(aTop, bTop);
            }

            // 闭环：补画首尾闭合段描边
            if (closed && points.Count >= 2)
            {
                Vector3 a = CellWorld(points[points.Count - 1]);
                Vector3 b = CellWorld(points[0]);
                Vector3 aTop = a + Vector3.up * height;
                Vector3 bTop = b + Vector3.up * height;
                Gizmos.DrawLine(a, b);
                Gizmos.DrawLine(aTop, bTop);
            }
            Gizmos.color = prev;
        }
    }
}
