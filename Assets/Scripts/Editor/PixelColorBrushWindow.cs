using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 像素颜色画布 —— PixelGroup 的**新增**编辑方式。原来那套（PNG 导出/导入、「重新生成颜色分布」、
    /// 菜单里的矩形填充/清除、选中 Pixel 改 colorId）**全部保留**，这个窗口只是多一条更快的手工回。
    ///
    /// 把网格铺成一张格子画布：每格显示它当前的颜色（色块 + 数字）或「空格」，
    /// 用笔刷涂**颜色**或**橡皮**，支持连续拖涂与矩形填/抹。整览（自适应宽度）与细编（手动格子像素）
    /// 两档共用同一个双向滚动视图。
    ///
    /// ## 十条口径（都与既有工具对齐）
    /// · **画布顶行 = gridZ 0 = 最前排**，与「导出颜色 (PNG)」一致（那边图片顶行就是 gridZ 0）。
    /// · **障碍格不可涂**，只以底色 + 单字标记显示并写明类别：墙 / 管 / 箱 / 木 / 门 / 冰 / 升。
    ///   像素不存在于障碍格上，唯一例外是冰 —— 冰不是障碍、冰底下的像素仍在，所以冰格照常画出颜色、
    ///   悬停时另报底下像素的 colorId。
    /// · **colorId 与 ColorConfig.materials 下标一一对应**，调色板直接取自 ColorConfig，不另立一份选项表。
    /// · **问号不是颜色**：它是 <c>PixelItem.isQuestion</c> 上的 flag，colorId 照旧保留（所以问号像素仍画得出本色）。
    ///   所以「问号标注」模式**不看调色板**（切模式时笔刷原位不动）：按下那格原本不是问号 → **标记**、
    ///   原本是问号 → **取消**。整笔只作用于「按下位置**同色四向连通**的那一组」，划出组外一律不动（防越界误标）。
    /// · **管道覆盖区域画粗描边（常显，与当前模式无关）**：覆盖范围 = 管道格 + 轨道格（与运行时
    ///   <c>PipeItem.CoversCell</c> 同口径），只沿**本根**的外缘画线、区域内部不画；两根管覆盖区相邻时
    ///   交界两侧各画各的（各自完整描框，忽略叠加）。轨道格本身仍是普通颜色格 —— 那里确实有像素（开局阻挡）。
    /// · **倍乘门可加可删**：添加门 = 按下起点、拖到终点（必须**轴对齐**、≥2 格），松手即创建，**倍率固定 x2**
    ///   （画布不给改倍率的入口；要改去 GateItem 的 Inspector，那边改完会重建显示）。
    ///   门格上的 Pixel 会被**清掉并记进 <c>GateItem.clearedPixels</c> 快照**（与「用选中 Pixel 创建倍乘门」同口径），
    ///   删除门时按快照**还原 Pixel** —— 这点与墙体 / 管道相反（那两者删除不回填）。
    /// · **箱子可加可删**：添加箱子 = 拖出 **2×2** 的矩形区域（画布只放开这一种；必须完全在网格内、且不含别的障碍），
    ///   松手即创建；创建时**吃掉区域内的 Pixel**（箱内像素改由 <c>colorIds</c> 提供，与 BoxCreator 同口径），
    ///   容量 = 本体 + 相邻 4 方向格数，colorIds 默认全填当前笔刷色。点箱子区域开**内部颜色二级面板**
    ///   （与管道那个同形：点色块替换、− / + 增删末端、清空全部；色块每行最多 20 个，超过换行）。
    ///   删除箱子会连隐藏 Pixel 一起销毁，**不回填**区域 Pixel。
    /// · **木箱可加可删，且不挡编辑**：木箱**盖住**底下的像素而不清除，所以画布上木箱格照常显示原颜色与编号、
    ///   照常能涂色 / 标问号 —— 辨认木箱靠**双层粗描边（外圈棕、内圈黄）**（不再画「木」字标记）。添加木箱 = 拖出矩形
    ///   （长宽至少 2 格、区域内不能有别的障碍，但**允许有像素**），松手即创建，拆箱次数固定 3（要改去 Inspector）；
    ///   删除木箱只销毁木箱本体，底下的像素原样保留。
    /// · **冰可加可删，还能就地编辑**：添加冰 = 拖出矩形（画布上按矩形建，矩形天然连通；区域内允许有像素与空格，
    ///   只挡别的障碍），松手即创建，次数初值 5；「操作」栏勾上**冰·自由涂抹**后改成**逐格划过**（非矩阵填充，
    ///   形状任意、不强求连通）。冰格照常显示底下像素的颜色与编号，辨认冰靠**双层粗描边（外深蓝内浅蓝）**。
    ///   删除冰只销毁冰组本体，格上的像素原样保留。「**编辑冰**」模式点冰格选中一组，改
    ///   次数 / 仅暴露后扣减 / 计数数字偏移（**本窗口只给 X / Z，Y 保持原值**）/ 计数数字缩放 —— **改动即时预览，
    ///   点「保存编辑」才落库**；保存会落成**一个 Undo 步骤**（Ctrl+Z 一键回到编辑前）。切模式 / 换冰组 / 取消选中时
    ///   若有未保存改动会弹窗二选一（保存并离开 / 放弃并离开，后者回到编辑前状态）；关窗与进出 Play 静默回退。
    /// · **管道波次颜色走二级面板**：涂颜色模式下点**管道格**（不是轨道格）打开 —— 选色复用上面那个调色板
    ///   （面板不再自带一份），面板里顺次列出该管道当前的波次颜色（点一下 = 用当前选中色替换），
    ///   最右 − / + 各删 / 追加末端一个。打开时没有可用颜色就自动落到 0 号色；选橡皮、
    ///   或点管道格以外的地方自动退出。**正在编辑的那根管道换成黄色描边 + 淡黄打底**（其余管是青色）。
    ///
    /// ## 笔刷值的表示
    /// <c>_brush = -1</c> = 橡皮、<c>&gt;= 0</c> = colorId。**不能**照搬「0 = 橡皮」那种表示：
    /// 这里 colorId 0 是一个正当颜色，拿 0 当橡皮会把色 0 的格子误判成擦除。
    ///
    /// ## 手势与撤销
    /// 按下记一个 Undo 组 → 拖动期间只改对象 + <c>Repaint</c>（不记 Undo、不 SetDirty）→
    /// 松手才 <c>RebuildGrid</c> + <c>SetDirty</c> + 收拢 Undo 组，整笔一步撤销。
    /// 添加墙体 / 添加管道更直接：**松手那一刻**体检通过就立即创建（没有「创建」按钮），整笔同样是一步撤销。
    /// 撤销 / 重做回来的改动由 <c>Undo.undoRedoPerformed</c> 重绑快照接住（见 <see cref="OnUndoRedoPerformed"/>），
    /// 窗口与场景同步。
    ///
    /// **收尾必须放在滚动视图之外**：拖到画布外松手时格子上收不到 MouseUp，手势会永久卡在 dragging。
    /// 同理，格子一律用 <c>Rect.Contains</c> 命中而不是 <c>GUILayout.Button</c> —— 按钮的按下态会和拖拽、
    /// 滚动打架。
    /// </summary>
    public class PixelColorBrushWindow : EditorWindow
    {
        /// <summary>笔刷手势模式：自由涂（按下即涂、拖过即涂）/ 矩形（按下定锚点、拖动预览、松手才落库）。</summary>
        private enum Tool
        {
            Free,
            Rect,
        }

        /// <summary>窗口的十五种模式：涂颜色 / 添加墙体 / 删除墙体 / 问号标注 / 添加管道 / 删除管道 /
        /// 添加倍乘门 / 删除倍乘门 / 添加箱子 / 删除箱子 / 添加木箱 / 删除木箱 / 添加冰 / 删除冰 / 编辑冰。
        /// 十五种互斥，切换时清掉各自的待定状态（编辑冰还会问一句未保存的改动）。</summary>
        private enum Mode
        {
            Color,
            AddWall,
            DeleteWall,
            Question,
            AddPipe,
            DeletePipe,
            AddGate,
            DeleteGate,
            AddBox,
            DeleteBox,
            AddCrate,
            DeleteCrate,
            AddIce,
            DeleteIce,
            EditIce,
        }

        private const float SwatchSize = 24f;
        private const float SwatchPad = 2f;

        /// <summary>自适应宽度时给滚动条与缩进留的余量，不留就会横向滚出画布。</summary>
        private const float CanvasMargin = 90f;

        /// <summary>「内部颜色」面板里色块每行的上限：超过就换行（窗口更窄时按实际宽度算，会更早换）。</summary>
        private const int MaxColorsPerRow = 20;

        private PixelGroup _group;
        private ColorConfig _config;
        private Color[] _palette = new Color[0];

        private bool _fitWidth = true;
        private int _cellPx = 24;
        private Tool _tool = Tool.Free;
        private Mode _mode = Mode.Color;

        /// <summary>笔刷值：-1 = 橡皮（删除该格像素），&gt;= 0 = 要涂的 colorId。
        /// 问号模式**不看笔刷**：标 / 取消由「按下的那一格当前是不是问号」决定，切模式时笔刷原位不动。</summary>
        private int _brush = -1;

        /// <summary>问号模式一笔的过滤集 = 按下那一格所属的「同色四向连通组」。拖动只在这一组内生效，
        /// 划到组外一律不动（避免越界误标）。笔画结束 / 取消时置空。</summary>
        private HashSet<Vector2Int> _questionGroup;

        /// <summary>问号模式这一笔的目标状态：按下那格原本不是问号 → true（标记）；原本是问号 → false（取消）。</summary>
        private bool _questionTarget;

        // ===== 墙体状态 =====

        /// <summary>
        /// 「添加墙体」拖动经过的格子，**按经过顺序**记 —— 这个顺序就是 <see cref="WallItem.points"/> 的端点顺序。
        /// 松手不落库，留到点「创建墙体」时才真正建墙（见 <see cref="CreateWallFromStroke"/>）。
        /// </summary>
        private readonly List<Vector2Int> _wallStroke = new List<Vector2Int>();

        /// <summary>「删除墙体」已高亮的那面墙：点一下只高亮，再点一下才真删。</summary>
        private WallItem _deletePendingWall;

        /// <summary>
        /// 待创建墙线占据的格（含闭环补段），每次 OnGUI 在工具栏里算一次：
        /// 工具栏的按钮可用性、画布高亮、状态行共用这一份，不在每格里重复算。
        /// 非「添加墙体」模式为 null。
        /// </summary>
        private HashSet<Vector2Int> _wallStrokeCells;

        /// <summary>
        /// 格 → 占据它的墙（每次 <see cref="RefreshSnapshot"/> 重建）。
        /// 两处用途：删除模式反查「点到的是哪面墙」；添加模式判「新墙是否压到已有墙」。
        /// </summary>
        private Dictionary<Vector2Int, WallItem> _wallCells = new Dictionary<Vector2Int, WallItem>();

        /// <summary>待创建管道的拖动轨迹（格），与 <see cref="_wallStroke"/> 同构；非「添加管道」模式为空。</summary>
        private readonly List<Vector2Int> _pipeStroke = new List<Vector2Int>();

        /// <summary>
        /// 待创建管道覆盖的格（管道格 + 轨道格），每次 OnGUI 在工具栏里算一次：
        /// 按钮可用性、画布高亮、状态行共用这一份。非「添加管道」模式为 null。
        /// </summary>
        private HashSet<Vector2Int> _pipeStrokeCells;

        /// <summary>「删除管道」已高亮待删的那根管（再点一下才真删）。</summary>
        private PipeItem _deletePendingPipe;

        /// <summary>上一笔「添加墙体 / 添加管道」没建成的理由（松手那一刻才判定）。状态行显示它；开新一笔 / 切模式时清掉。</summary>
        private string _lastStrokeError;

        /// <summary>「波次颜色」二级界面正在编辑的管道（涂颜色模式下点管道格打开）。null = 界面关闭。</summary>
        private PipeItem _pipeColorTarget;

        /// <summary>
        /// 格 → 覆盖它的管道（管道格 + 轨道格；每次 <see cref="RefreshSnapshot"/> 重建）。
        /// 三处用途：删除模式反查「点到的是哪根管」、添加模式判重叠、画布给覆盖区画**粗描边**。
        /// </summary>
        private readonly Dictionary<Vector2Int, PipeItem> _pipeCells = new Dictionary<Vector2Int, PipeItem>();

        /// <summary>「添加倍乘门」的两个端点（按下格 = 起点，拖动格 = 终点；门必须是轴对齐笔直线段）。</summary>
        private Vector2Int _gateAnchor;
        private Vector2Int _gateCurrent;

        /// <summary>待创建门线占据的格（由两个端点换算而来），每帧在工具栏里算一次。非「添加倍乘门」模式为 null。</summary>
        private HashSet<Vector2Int> _gateStrokeCells;

        /// <summary>「添加倍乘门」固定的倍率：画布不提供修改入口（要改去 GateItem 的 Inspector，那边改完会重建显示）。</summary>
        private const int GateMultiplier = 2;

        /// <summary>「删除倍乘门」已高亮待删的那道门（再点一下才真删）。</summary>
        private GateItem _deletePendingGate;

        /// <summary>
        /// 格 → 占据它的倍乘门（每次 <see cref="RefreshSnapshot"/> 重建）。
        /// 两处用途：删除模式反查「点到的是哪道门」、添加模式判重叠。
        /// </summary>
        private readonly Dictionary<Vector2Int, GateItem> _gateCells = new Dictionary<Vector2Int, GateItem>();

        /// <summary>「添加箱子」的两个对角（按下格 + 拖动格）—— 箱子是**矩形区域**。</summary>
        private Vector2Int _boxAnchor;
        private Vector2Int _boxCurrent;

        /// <summary>待创建箱子区域占据的格（由两个对角换算），每帧在工具栏里算一次。非「添加箱子」模式为 null。</summary>
        private HashSet<Vector2Int> _boxStrokeCells;

        /// <summary>「删除箱子」已高亮待删的那个箱子（再点一下才真删）。</summary>
        private BoxItem _deletePendingBox;

        /// <summary>格 → 占据它的箱子（只登记未开箱的，与 boxGrid 同口径；每次 RefreshSnapshot 重建）。</summary>
        private readonly Dictionary<Vector2Int, BoxItem> _boxCells = new Dictionary<Vector2Int, BoxItem>();

        /// <summary>「箱内颜色」二级面板正在编辑的箱子（涂颜色模式下点箱子区域打开）。null = 面板关闭。</summary>
        private BoxItem _boxColorTarget;

        /// <summary>「添加木箱」的两个对角（按下格 + 拖动格）—— 木箱也是矩形区域（长宽至少 2 格）。</summary>
        private Vector2Int _crateAnchor;
        private Vector2Int _crateCurrent;

        /// <summary>待创建木箱区域占据的格，每帧在工具栏里算一次。非「添加木箱」模式为 null。</summary>
        private HashSet<Vector2Int> _crateStrokeCells;

        /// <summary>「删除木箱」已高亮待删的那个木箱（再点一下才真删）。</summary>
        private CrateItem _deletePendingCrate;

        /// <summary>格 → 盖住它的木箱（未拆掉的；每次 RefreshSnapshot 重建）。
        /// 用途：棕色描边、删除模式反查、添加模式判重叠。</summary>
        private readonly Dictionary<Vector2Int, CrateItem> _crateCells = new Dictionary<Vector2Int, CrateItem>();

        /// <summary>画布建的木箱固定的「拆箱所需移出次数」（要改去木箱的 Inspector）。</summary>
        private const int CrateDestroyAfterMoves = 3;

        /// <summary>「添加冰」的两个对角（按下格 + 拖动格）—— 不勾「自由涂抹」时按矩形建（矩形天然连通）。</summary>
        private Vector2Int _iceAnchor;
        private Vector2Int _iceCurrent;

        /// <summary>「添加冰」勾了自由涂抹时的划过的格（逐格计入，与墙 / 管的笔画同构）。</summary>
        private readonly List<Vector2Int> _iceStroke = new List<Vector2Int>();

        /// <summary>「添加冰」是否自由涂抹（非矩阵填充）：勾选后不再是拖矩形，而是像画笔一样逐格划过。</summary>
        private bool _iceFreeform;

        /// <summary>待创建冰组占据的格，每帧在工具栏里算一次。非「添加冰」模式为 null。</summary>
        private HashSet<Vector2Int> _iceStrokeCells;

        /// <summary>「删除冰」已高亮待删的那个冰组（再点一下才真删）。</summary>
        private IceItem _deletePendingIce;

        /// <summary>格 → 属于它的冰组（每次 RefreshSnapshot 重建）。用途：双层描边、删除 / 编辑模式反查、添加模式判重叠。</summary>
        private readonly Dictionary<Vector2Int, IceItem> _iceCells = new Dictionary<Vector2Int, IceItem>();

        /// <summary>画布建的冰组固定的次数初值（要改去冰组的 Inspector，或用「编辑冰」模式）。</summary>
        private const int DefaultIceFreezeCount = 5;

        /// <summary>「编辑冰」模式正在编辑的冰组（点冰格选中）。null = 没选。</summary>
        private IceItem _iceEditTarget;

        /// <summary>这一轮编辑**开始前**的那份值：不保存就按它回到编辑前状态。</summary>
        private IceEditValues _iceBefore;

        /// <summary>当前编辑中的值（实时预览用的就是它）。</summary>
        private IceEditValues _iceEditing;

        /// <summary>本轮编辑有没有**未保存**的改动（切模式 / 换冰组 / 关窗时据此提示）。</summary>
        private bool _iceDirty;

        /// <summary>本帧需要重建一次冰的可见表现（改字段后统一在面板末尾重建，别每个控件都重建一次 Mesh）。</summary>
        private bool _iceVisualDirty;

        /// <summary>
        /// 「编辑冰」能改的那几个字段切片（次数 / 是否暴露扣减 / 计数数字偏移 / 计数数字缩放）。
        /// 做成结构体是为了能同时留「编辑前」和「编辑中」两份，随时互相写回。
        /// </summary>
        private struct IceEditValues
        {
            public int freezeCount;
            public bool meltOnlyWhenExposed;
            public Vector3 countOffset;
            public float countFontScale;

            public static IceEditValues Capture(IceItem ice)
            {
                var v = new IceEditValues();
                v.freezeCount = ice.freezeCount;
                v.meltOnlyWhenExposed = ice.meltOnlyWhenExposed;
                v.countOffset = ice.countOffset;
                v.countFontScale = ice.countFontScale;
                return v;
            }

            public void ApplyTo(IceItem ice)
            {
                ice.freezeCount = freezeCount;
                ice.meltOnlyWhenExposed = meltOnlyWhenExposed;
                ice.countOffset = countOffset;
                ice.countFontScale = countFontScale;
            }

            public bool SameAs(IceEditValues other)
            {
                return freezeCount == other.freezeCount
                    && meltOnlyWhenExposed == other.meltOnlyWhenExposed
                    && countOffset == other.countOffset
                    && Mathf.Approximately(countFontScale, other.countFontScale);
            }
        }

        private Vector2 _scroll;

        /// <summary>场景里的关卡内容被别的工具改过（层级变了）→ 需要重建快照。合并到每帧一次，见 <see cref="SyncWithSceneIfDirty"/>。</summary>
        private bool _sceneDirty;

        /// <summary>上次看到的 PixelGroup 子物体数：「内容变了」的兜底判据（层级事件漏发时也能发现）。</summary>
        private int _lastChildCount = -1;

        // ===== 手势状态 =====

        private bool _dragging;
        private bool _dirty;
        private int _undoGroup;

        /// <summary>本笔已经处理过的格。拖动会反复划过同一格，靠它挡掉重复处理
        /// （也免得「pixelPrefab 为空」这类警告被同一格刷很多遍）。</summary>
        private readonly HashSet<Vector2Int> _strokePainted = new HashSet<Vector2Int>();

        private Vector2Int _rectAnchor;
        private Vector2Int _rectCurrent;
        private bool _rectActive;

        /// <summary>本帧悬停的格（每帧重算，供底部状态行）。</summary>
        private Vector2Int _hover = new Vector2Int(-1, -1);

        // ===== 缓存的文字样式（每格新建 GUIStyle 会很浪费）=====

        private GUIStyle _numStyle;
        private int _numStyleSize = -1;
        private GUIStyle _markStyle;
        private int _markStyleSize = -1;

        /// <summary>色块上的编号样式（缓存）。文字颜色每个色块单独设，所以这里只缓存字号与对齐。</summary>
        private GUIStyle _swatchNumStyle;

        // ============================================================
        // 入口 / 生命周期
        // ============================================================

        [MenuItem("CrowdMatch/像素颜色画布")]
        private static void OpenWindow()
        {
            OpenFor(null);
        }

        /// <summary>
        /// 打开窗口并绑定到指定 PixelGroup。供菜单（传 null = 按当前选中自动绑定）与
        /// PixelGroup Inspector 上的「打开像素颜色画布」按钮共用。
        /// </summary>
        public static void OpenFor(PixelGroup group)
        {
            var window = GetWindow<PixelColorBrushWindow>("像素颜色画布");
            window.minSize = new Vector2(460f, 340f);

            if (group != null)
            {
                window._group = group;
                window.RefreshSnapshot();
            }
            else
            {
                window.BindFromSelection();
            }

            window.Show();
            window.Repaint();
        }

        private void OnEnable()
        {
            Selection.selectionChanged += BindFromSelection;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            BindFromSelection();

            // 域重载（改脚本 / 重开窗口）后 _wallCells / _pipeCells 这些 Dictionary 是**空的**
            // （Unity 不序列化 Dictionary），而 BindFromSelection 在「还是同一个 group」时会早退 →
            // 不重建就会「打开窗口什么都没有」：管道没有粗描边、删墙模式反查不到墙。这里强制重建一次。
            if (_group != null)
                RefreshSnapshot();
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= BindFromSelection;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            CancelStroke();   // 关窗时别把手势留在 dragging / rectActive
            RevertIceEditSilently();   // 关窗不适合弹窗：未保存的冰组编辑直接回退
            _iceEditTarget = null;
            _iceDirty = false;
        }

        /// <summary>
        /// 场景层级变了（**导入关卡 JSON / 清空 Group / 生成 Pixel / 加删墙体管道**… 都会新建或销毁对象）：
        /// 这里只置脏，真正的重建放到下一次 OnGUI 里做 —— 导入那种批量操作会连续触发成百上千次事件，
        /// 每次都在回调里重建网格会卡到不可用；置脏 + 每帧最多重建一次就够，且不会漏。
        /// </summary>
        private void OnHierarchyChanged()
        {
            _sceneDirty = true;
            Repaint();
        }

        /// <summary>
        /// 别的工具动过场景就重建一次快照（每帧最多一次），让画布跟上外部改动。
        /// 除了层级事件，另外拿「子物体数」兜一次底（层级事件漏发 / 同一层级内换了对象时也能发现）。
        /// 运行中画布是只读的，不跟场景同步。
        /// </summary>
        private void SyncWithSceneIfDirty()
        {
            if (Application.isPlaying)
                return;

            bool childrenChanged = _group == null || _group.transform.childCount != _lastChildCount;
            if (!_sceneDirty && !childrenChanged)
                return;

            RefreshSnapshot();   // 内部会把 _sceneDirty / _lastChildCount 归位
        }

        /// <summary>
        /// 撤销 / 重做之后重建快照并重画，让窗口跟着场景一起回到撤销后的状态。
        ///
        /// 必须重建的原因：Undo 会**新建 / 销毁** PixelItem 与 WallItem，而画布读的是缓存的占用表
        /// （<c>_group.grid</c>，只由 <c>RebuildGrid</c> 重算）与「格 → 墙」表 —— 不重建就会停在撤销前那一刻，
        /// 画出已经不存在的像素 / 墙。顺手取消进行中的手势：被撤销掉的那一格可能已经不在原位了。
        /// </summary>
        private void OnUndoRedoPerformed()
        {
            if (_group == null)
                return;

            CancelStroke();

            // 编辑冰时按了 Ctrl+Z：撤销可能动的正是冰组本身 ——
            // 目标没了（被撤销掉）就清掉编辑状态；目标还在就重新对齐：有未保存改动时下一帧把预览值写回去，
            // 没改动时重新记一次基线（撤销可能正好撤掉了上一次「保存编辑」）。
            if (_iceEditTarget != null)
            {
                if (_iceDirty)
                    _iceVisualDirty = true;
                else
                {
                    _iceBefore = IceEditValues.Capture(_iceEditTarget);
                    _iceEditing = _iceBefore;
                }
            }
            else
            {
                _iceDirty = false;
            }

            RefreshSnapshot();
            SceneView.RepaintAll();
            Repaint();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // 进 / 出 Play 都会动到像素对象：重新绑一次并重画，别让画布停在旧快照上。
            // 波次颜色二级界面也关掉：运行时不该改管道的波次配置。
            CancelStroke();
            _pipeColorTarget = null;
            _boxColorTarget = null;
            RevertIceEditSilently();   // 进出 Play 前先把未保存的冰组预览收掉（运行时不该改冰组配置）
            _iceEditTarget = null;
            BindFromSelection();

            // 进出 Play 都会动到像素 / 障碍对象：重建一次快照（BindFromSelection 对同一个 group 会早退，不能指望它）
            if (_group != null)
                RefreshSnapshot();

            Repaint();
        }

        /// <summary>按当前选中重绑 PixelGroup（选中它或其子物体都行；没选中、或还是同一个，就不动）。</summary>
        private void BindFromSelection()
        {
            var selected = Selection.activeGameObject;
            if (selected == null)
                return;

            var group = selected.GetComponentInParent<PixelGroup>();
            if (group == null || group == _group)
                return;

            _group = group;
            RefreshSnapshot();
            Repaint();
        }

        /// <summary>重建占用表 + 取调色板。改过场景物件、或换绑到另一个 PixelGroup 之后必须走一次，否则画的是旧快照。</summary>
        private void RefreshSnapshot()
        {
            _config = ColorConfigLocator.Find();
            _palette = BuildPalette(_config);

            // 调色板可能变短（换了 PixelGroup，或 ColorConfig 被改过）：把越界的笔刷收回有效范围，
            // 否则会涂出一个 ColorConfig 映射不到的颜色 ID（材质取空，像素看起来没颜色）。
            if (_brush >= _palette.Length)
                _brush = _palette.Length > 0 ? _palette.Length - 1 : -1;

            if (_group != null)
                _group.RebuildGrid();   // 占用表 / 冰 / 木箱掩码都靠它刷新

            RebuildWallCellMap();
            RebuildPipeCellMap();
            RebuildGateCellMap();
            RebuildBoxCellMap();
            RebuildCrateCellMap();
            RebuildIceCellMap();

            // 记住这份快照对应的场景状态，免得下一次 OnGUI 又白重建一遍（重建路径分散在多处，放在这里最省心）
            _sceneDirty = false;
            _lastChildCount = _group != null ? _group.transform.childCount : -1;
        }

        /// <summary>
        /// 重建「格 → 冰组」表。冰**不是障碍**（不写 wallGrid/pipeGrid/boxGrid），它只是让组内像素
        /// 「视为不暴露」；冰下面的像素仍在 <c>grid</c> 里，所以画布上冰格照常显示原颜色。
        /// </summary>
        private void RebuildIceCellMap()
        {
            _iceCells.Clear();

            if (_group == null)
            {
                _deletePendingIce = null;
                return;
            }

            foreach (var ice in _group.GetComponentsInChildren<IceItem>())
            {
                if (ice == null)
                    continue;

                foreach (var cell in ice.CellSet)
                {
                    if (_group.IsInRange(cell.x, cell.y) && !_iceCells.ContainsKey(cell))
                        _iceCells[cell] = ice;
                }
            }
        }

        /// <summary>
        /// 重建「格 → 木箱」表。木箱**不是障碍**（不写 wallGrid/pipeGrid/boxGrid），它只是**盖住**底下的像素
        /// （关渲染器 + 不可点），像素本身还在 <c>grid</c> 里 —— 所以画布上它照常显示原颜色、也照常能涂。
        /// 只登记未拆掉的木箱（<c>crate.destroyed</c>）。
        /// </summary>
        private void RebuildCrateCellMap()
        {
            _crateCells.Clear();

            if (_group == null)
            {
                _deletePendingCrate = null;
                return;
            }

            foreach (var crate in _group.GetComponentsInChildren<CrateItem>())
            {
                if (crate == null || crate.destroyed)
                    continue;

                for (int r = crate.rowMin; r <= crate.rowMax; r++)
                    for (int c = crate.colMin; c <= crate.colMax; c++)
                    {
                        var cell = new Vector2Int(c, r);
                        if (_group.IsInRange(c, r) && !_crateCells.ContainsKey(cell))
                            _crateCells[cell] = crate;
                    }
            }
        }

        /// <summary>
        /// 重建「格 → 箱子」表。只登记**未开箱**的箱子（开箱后它不再占格，格子归像素用），与
        /// <c>PixelGroup.boxGrid</c> 同口径。用途：删除模式反查「点到的是哪个箱子」、涂颜色模式判断
        /// 「点到的格子属于哪个箱子（好开二级面板）」。
        /// </summary>
        private void RebuildBoxCellMap()
        {
            _boxCells.Clear();

            if (_group == null)
            {
                _deletePendingBox = null;
                return;
            }

            foreach (var box in _group.GetComponentsInChildren<BoxItem>())
            {
                if (box == null || box.opened)
                    continue;

                for (int r = box.rowMin; r <= box.rowMax; r++)
                    for (int c = box.colMin; c <= box.colMax; c++)
                    {
                        var cell = new Vector2Int(c, r);
                        if (_group.IsInRange(c, r) && !_boxCells.ContainsKey(cell))
                            _boxCells[cell] = box;
                    }
            }
        }

        /// <summary>
        /// 重建「格 → 倍乘门」表。门格由 <see cref="GateItem.cells"/> 给出（= start～end 那段轴对齐线段经过的格，
        /// <see cref="PixelGroup.RebuildGrid"/> 已经 RefreshCells 过）。门**不是障碍**（不写 wallGrid/pipeGrid），
        /// 但它的门格是闭合区域计算里的屏障，所以这里照样登记。
        /// </summary>
        private void RebuildGateCellMap()
        {
            _gateCells.Clear();

            if (_group == null)
            {
                _deletePendingGate = null;
                return;
            }

            foreach (var gate in _group.GetComponentsInChildren<GateItem>())
            {
                if (gate == null)
                    continue;

                foreach (var cell in gate.cells)
                {
                    if (_group.IsInRange(cell.x, cell.y) && !_gateCells.ContainsKey(cell))
                        _gateCells[cell] = gate;
                }
            }
        }

        /// <summary>
        /// 重建「格 → 管道」表。覆盖范围与运行时的 <see cref="PipeItem.CoversCell"/> 同口径：
        /// **管道格（points[0]）+ 轨道格**。轨道格虽然在画布上显示为普通颜色格（那里确实有像素），
        /// 但运行时把它当管道覆盖格阻挡暴露，所以删管 / 判重叠 / 画描边都得算上它。
        /// </summary>
        private void RebuildPipeCellMap()
        {
            _pipeCells.Clear();

            if (_group == null)
            {
                _deletePendingPipe = null;
                return;
            }

            foreach (var pipe in _group.GetComponentsInChildren<PipeItem>())
            {
                if (pipe == null)
                    continue;

                var pipeCell = PipeItem.GetPipeCell(pipe.points);
                if (_group.IsInRange(pipeCell.x, pipeCell.y) && !_pipeCells.ContainsKey(pipeCell))
                    _pipeCells[pipeCell] = pipe;

                foreach (var track in pipe.TrackCells())
                {
                    if (_group.IsInRange(track.x, track.y) && !_pipeCells.ContainsKey(track))
                        _pipeCells[track] = pipe;
                }
            }
        }

        /// <summary>重建「格 → 墙」表。墙体没有占用表可用（wallGrid 只是 bool），只能逐面墙枚举它占的格。</summary>
        private void RebuildWallCellMap()
        {
            _wallCells.Clear();

            if (_group == null)
            {
                _deletePendingWall = null;
                return;
            }

            foreach (var wall in _group.GetComponentsInChildren<WallItem>())
            {
                if (wall == null)
                    continue;

                foreach (var cell in wall.EnumerateOccupiedCells())
                {
                    if (_group.IsInRange(cell.x, cell.y) && !_wallCells.ContainsKey(cell))
                        _wallCells[cell] = wall;
                }
            }

            // 高亮着的那面墙可能刚被删掉 / 撤销掉 —— 不用特意清：Unity 的 == null 对已销毁对象同样成立，
            // 后面所有用到 _deletePendingWall 的地方都带着 != null 守卫。
        }

        /// <summary>从 ColorConfig 构建调色板（下标 = colorId）。</summary>
        private static Color[] BuildPalette(ColorConfig config)
        {
            if (config == null || config.materials == null)
                return new Color[0];

            var colors = new Color[config.materials.Length];
            for (int i = 0; i < config.materials.Length; i++)
            {
                var mat = config.materials[i];
                colors[i] = mat != null ? mat.color : Color.magenta;
            }
            return colors;
        }

        // ============================================================
        // GUI
        // ============================================================

        private void OnGUI()
        {
            if (_group == null)
            {
                EditorGUILayout.HelpBox("未绑定 PixelGroup：请选中场景里的 PixelGroup（或其子物体）。", MessageType.Info);
                return;
            }

            if (_group.columns <= 0 || _group.TotalRows <= 0)
            {
                EditorGUILayout.HelpBox(
                    "PixelGroup 的网格尺寸无效（" + _group.columns + " 列 × " + _group.TotalRows + " 行）。",
                    MessageType.Error);
                return;
            }

            _hover = new Vector2Int(-1, -1);

            // 别的工具动过场景（导入关卡 JSON / 清空 Group / 生成 Pixel…）：先重建快照，这一帧就画新的
            SyncWithSceneIfDirty();

            DrawViewToolbar();
            DrawPalette();
            DrawPipeColorPanel();   // 二级面板：涂颜色模式下点了管道格才出现（选色复用上面的调色板）
            DrawBoxColorPanel();    // 二级面板：涂颜色模式下点了箱子区域才出现（与上者互斥）
            DrawIceEditPanel();     // 编辑冰面板：编辑冰模式下点了冰格才出现
            DrawCanvas();
            DrawStatusLine();

            // 手势收尾放在滚动视图之外：拖到画布外松手时格子上的 HandleCell 收不到 MouseUp
            HandleStrokeEnd();
        }

        /// <summary>
        /// 工具栏三段：模式（十五选一，分四行）/ 视图（自适应 + 格子像素 + 颜色笔刷的手势）/ 操作（刷新快照）。
        /// **每一段都常驻**，不按模式隐藏 —— 隐藏会让下面控件的命中矩形当场换人（见 skill 的说明）。
        /// 不适用的控件只禁用，不改布局高度。
        /// </summary>
        private void DrawViewToolbar()
        {
            bool colorMode = _mode == Mode.Color;
            bool playing = Application.isPlaying;

            // 待创建墙线 / 管道 / 门线的占格：本帧只算一次，下面按钮 / 画布 / 状态行都读这一份
            _wallStrokeCells = _mode == Mode.AddWall ? CollectStrokeCells() : null;
            _pipeStrokeCells = _mode == Mode.AddPipe ? CollectPipeStrokeCells() : null;
            _gateStrokeCells = _mode == Mode.AddGate ? CollectGateStrokeCells() : null;
            _boxStrokeCells = _mode == Mode.AddBox ? CollectBoxStrokeCells() : null;
            _crateStrokeCells = _mode == Mode.AddCrate ? CollectCrateStrokeCells() : null;
            _iceStrokeCells = _mode == Mode.AddIce ? CollectIceStrokeCells() : null;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("模式", GUILayout.Width(90f));
            DrawModeButton(Mode.Color, "涂颜色");
            DrawModeButton(Mode.AddWall, "添加墙体");
            DrawModeButton(Mode.DeleteWall, "删除墙体");
            DrawModeButton(Mode.Question, "问号标注");
            EditorGUILayout.EndHorizontal();

            // 第二行：用等宽空标签对齐到第一行的按钮起点（十个模式，一行放不下，分三行每行四个）
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("", GUILayout.Width(90f));
            DrawModeButton(Mode.AddPipe, "添加管道");
            DrawModeButton(Mode.DeletePipe, "删除管道");
            DrawModeButton(Mode.AddGate, "加倍乘门");
            DrawModeButton(Mode.DeleteGate, "删倍乘门");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("", GUILayout.Width(90f));
            DrawModeButton(Mode.AddBox, "添加箱子");
            DrawModeButton(Mode.DeleteBox, "删除箱子");
            DrawModeButton(Mode.AddCrate, "添加木箱");
            DrawModeButton(Mode.DeleteCrate, "删除木箱");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("", GUILayout.Width(90f));
            DrawModeButton(Mode.AddIce, "添加冰");
            DrawModeButton(Mode.DeleteIce, "删除冰");
            DrawModeButton(Mode.EditIce, "编辑冰");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _fitWidth = GUILayout.Toggle(_fitWidth, "自适应宽度", GUILayout.Width(90f));

            EditorGUI.BeginDisabledGroup(_fitWidth);   // 自适应时格子像素由宽度算出来，滑块没有意义
            _cellPx = Mathf.RoundToInt(EditorGUILayout.Slider("格子像素", _cellPx, 12f, 64f));
            EditorGUI.EndDisabledGroup();

            using (new EditorGUI.DisabledScope(!colorMode))   // 工具只影响「涂颜色」模式的手势（问号模式固定自由拖动）
                _tool = (Tool)EditorGUILayout.EnumPopup("工具", _tool, GUILayout.Width(130f));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("操作", GUILayout.Width(90f));

            using (new EditorGUI.DisabledScope(playing))
            {
                if (GUILayout.Button("刷新快照", GUILayout.Width(76f)))
                {
                    RefreshSnapshot();
                    SceneView.RepaintAll();
                }
            }

            // 「添加冰」的自由涂抹（非矩阵填充）：勾选后不再是拖矩形，而是像画笔一样逐格划过。
            // **只在「添加冰」模式下画出来**（其它模式直接不显示，不做灰化）——它只在这一行里，
            // 不画也不改变行高，下面的控件不会跳位。
            if (_mode == Mode.AddIce)
            {
                using (new EditorGUI.DisabledScope(playing))
                {
                    _iceFreeform = GUILayout.Toggle(_iceFreeform, "冰·自由涂抹", GUILayout.Width(110f));
                }
            }

            // 「创建墙体 / 创建管道」已取消按钮：添加模式下**松手即创建（合法时）**，见 FinishWallStroke / FinishPipeStroke。
            // 拖动过程中的实时判据仍由状态行给出（可生成 ✓ / ✗ 原因），所以不必再放一个按钮。

            EditorGUILayout.EndHorizontal();

            if (playing)
                EditorGUILayout.HelpBox(
                    "运行模式下不能改场景（会跟正在跑的逻辑打架），请先停止运行。",
                    MessageType.Info);
        }

        /// <summary>模式按钮：用 Toggle 的选中态做互斥选择。切换时把待定状态都清掉，避免跨模式残留。</summary>
        private void DrawModeButton(Mode mode, string label)
        {
            bool on = GUILayout.Toggle(_mode == mode, label, EditorStyles.miniButton, GUILayout.Width(80f));
            if (!on || _mode == mode)
                return;

            // 离开「编辑冰」：有未保存的改动先问一句（保存 / 放弃，两者都会离开）
            if (_mode == Mode.EditIce)
                ConfirmLeaveIceEdit();

            _mode = mode;
            _wallStroke.Clear();
            _deletePendingWall = null;
            _pipeStroke.Clear();
            _deletePendingPipe = null;
            _deletePendingGate = null;
            _deletePendingBox = null;
            _deletePendingCrate = null;
            _deletePendingIce = null;
            _iceStroke.Clear();
            _iceEditTarget = null;
            _iceDirty = false;
            _lastStrokeError = null;
            // 笔刷不动：问号模式不看笔刷，切回来时原来选的是哪个色块 / 橡皮都还在原位
            Repaint();
        }

        /// <summary>
        /// 调色板：一行等宽色块（最左是橡皮），每块标出它的 colorId，选中项加黄框。
        /// 色块数量随窗口宽度换行，所以**先按宽度算出要几行、再一次性占位** ——
        /// 边画边撑高度会让后面的控件命中矩形当场换人。
        /// </summary>
        private void DrawPalette()
        {
            int count = _palette.Length + 1;   // +1 = 最左的橡皮
            if (count <= 1)
            {
                EditorGUILayout.HelpBox("ColorConfig 里没有颜色（materials 为空）。", MessageType.Warning);
                return;
            }

            float usable = Mathf.Max(SwatchSize, position.width - 16f);
            int perRow = Mathf.Max(1, Mathf.FloorToInt(usable / (SwatchSize + SwatchPad)));
            int rows = Mathf.CeilToInt(count / (float)perRow);

            Rect area = GUILayoutUtility.GetRect(0f, rows * (SwatchSize + SwatchPad), GUILayout.ExpandWidth(true));

            bool playing = Application.isPlaying;
            bool colorMode = _mode == Mode.Color;

            // 非涂色模式（添加/删除墙体、问号标注）下保留这一块（高度稳定，不让下面控件跳位），
            // 但压暗且不响应点击 —— 问号模式不看笔刷，标 / 取消由「按下的那一格是不是问号」决定。
            if (!colorMode && Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(area, new Color(0.1f, 0.1f, 0.12f, 0.55f));

            for (int i = 0; i < count; i++)
            {
                var rect = new Rect(
                    area.x + (i % perRow) * (SwatchSize + SwatchPad),
                    area.y + (i / perRow) * (SwatchSize + SwatchPad),
                    SwatchSize, SwatchSize);

                int value = i == 0 ? -1 : i - 1;   // i == 0 → 橡皮
                DrawSwatch(rect, value);

                if (playing || !colorMode)
                    continue;

                Event ev = Event.current;
                if (ev.type == EventType.MouseDown && rect.Contains(ev.mousePosition))
                {
                    _brush = value;
                    if (value < 0)
                        _pipeColorTarget = null;   // 选橡皮 = 离开管道编辑（橡皮没有「要填的颜色」）
                    ev.Use();
                    Repaint();
                }
            }
        }

        private void DrawSwatch(Rect rect, int value, bool selection = true)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            if (value < 0)
            {
                // 橡皮：深底 + 一道红斜杠，跟颜色块一眼可分（它不是颜色，所以不标编号）
                EditorGUI.DrawRect(rect, new Color(0.22f, 0.22f, 0.24f));
                EditorGUI.DrawRect(
                    new Rect(rect.x + 4f, rect.y + rect.height * 0.5f - 1f, rect.width - 8f, 2f),
                    new Color(0.85f, 0.45f, 0.45f));
            }
            else
            {
                EditorGUI.DrawRect(rect, _palette[value]);

                // 编号 = colorId（与 ColorConfig.materials 下标、关卡 JSON 里的 colorId 同一个数）。
                // 字色按色块亮度反着来：浅底配黑字、深底配白字，否则白字压在浅黄上根本看不见。
                var style = SwatchNumStyle();
                style.normal.textColor = Luminance(_palette[value]) > 0.55f ? Color.black : Color.white;
                EditorGUI.LabelField(rect, value.ToString(), style);
            }

            // 边框与选中框只能在 Repaint 阶段画
            var border = new Color(0.1f, 0.1f, 0.1f);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), border);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), border);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), border);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), border);

            if (selection && _brush == value)
            {
                var yellow = new Color(1f, 0.85f, 0.2f);
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 2f), yellow);
                EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), yellow);
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, 2f, rect.height), yellow);
                EditorGUI.DrawRect(new Rect(rect.xMax - 2f, rect.y, 2f, rect.height), yellow);
            }
        }

        /// <summary>色块编号的样式（缓存）：字号跟着色块大小走，并裁到色块内免得两位数溢出去。</summary>
        private GUIStyle SwatchNumStyle()
        {
            if (_swatchNumStyle == null)
            {
                _swatchNumStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontSize = Mathf.Clamp(Mathf.RoundToInt(SwatchSize * 0.45f), 9, 24),
                    alignment = TextAnchor.MiddleCenter,
                    clipping = TextClipping.Clip,
                };
            }
            return _swatchNumStyle;
        }

        /// <summary>色块的相对亮度（0~1），只用来决定编号用黑字还是白字。</summary>
        private static float Luminance(Color c)
        {
            return 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
        }

        /// <summary>铺画布：行 = TotalRows（顶行 = gridZ 0 = 最前排），每格一个 GetRect，整张包在双向 ScrollView 里。</summary>
        private void DrawCanvas()
        {
            int columns = _group.columns;
            int rows = _group.TotalRows;

            int px = _fitWidth
                ? Mathf.Clamp(Mathf.FloorToInt((position.width - CanvasMargin) / Mathf.Max(1, columns)), 6, 64)
                : _cellPx;

            _scroll = EditorGUILayout.BeginScrollView(_scroll, true, true, GUILayout.ExpandHeight(true));

            for (int gridZ = 0; gridZ < rows; gridZ++)
            {
                EditorGUILayout.BeginHorizontal();
                for (int col = 0; col < columns; col++)
                {
                    Rect rect = GUILayoutUtility.GetRect(px, px, GUILayout.Width(px), GUILayout.Height(px));
                    DrawCell(rect, col, gridZ, px);
                    HandleCell(rect, col, gridZ);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawCell(Rect rect, int col, int gridZ, int px)
        {
            int colorId;
            CellKind kind = Classify(col, gridZ, out colorId);

            if (Event.current.type == EventType.Repaint)
            {
                var item = _group.GetItem(col, gridZ);

                // 底色
                if (kind == CellKind.Color)
                {
                    var baseColor = _palette.Length > 0
                        ? _palette[Mathf.Clamp(colorId, 0, _palette.Length - 1)]
                        : Color.gray;

                    if (item != null && item.isQuestion)
                    {
                        // 问号像素：左半本色、右半黑 —— 既一眼看出被标了问号，又保住它的颜色信息
                        float half = Mathf.Floor(rect.width * 0.5f);
                        EditorGUI.DrawRect(new Rect(rect.x, rect.y, half, rect.height), baseColor);
                        EditorGUI.DrawRect(new Rect(rect.x + half, rect.y, rect.width - half, rect.height), Color.black);
                    }
                    else
                    {
                        EditorGUI.DrawRect(rect, baseColor);
                    }
                }
                else if ((kind == CellKind.Ice || kind == CellKind.Crate)
                         && item != null && item.colorId >= 0 && item.colorId < _palette.Length)
                {
                    // 冰格 / 木箱格：底下像素的颜色照常画出来（冰只是「视为不暴露」，木箱只是「盖住」，
                    // 两者都不是别的像素意义上的障碍：像素还在 grid 里）
                    EditorGUI.DrawRect(rect, _palette[item.colorId]);
                }
                else
                {
                    EditorGUI.DrawRect(rect, BackgroundOf(kind));
                }

                // 内容：障碍画单字标记，颜色格画 colorId 数字（太小就不画了，看不清反而乱）。
                // 木箱 / 冰格例外：不画「木」「冰」字 —— 它们靠各自的**双层描边**辨认；
                // 底下还有像素时（两者都不清除像素）连颜色 + 编号一起画出来。
                if (kind == CellKind.Crate || kind == CellKind.Ice)
                {
                    if (item != null && px >= 14)
                        EditorGUI.LabelField(rect, item.colorId.ToString(), NumStyle(Mathf.Clamp(px / 3, 9, 24)));
                }
                else if (!IsPaintable(kind))
                {
                    EditorGUI.LabelField(rect, MarkerOf(kind),
                        MarkStyle(Mathf.Clamp(px / 5, 7, 11)));
                }
                else if (kind == CellKind.Color && px >= 14)
                {
                    // 问号像素的数字带「?」后缀（如 14?），与右半黑搭配一眼可辨
                    bool question = item != null && item.isQuestion;
                    EditorGUI.LabelField(rect, question ? colorId + "?" : colorId.ToString(),
                        NumStyle(Mathf.Clamp(px / 3, 9, 24)));
                }

                // 管道覆盖区域：沿**本根管道**覆盖区的外缘画粗描边（只看这根的归属，不看别的管）。
                // 两根管挨着时，交界那一格两侧各画各的线（两道 3px 叠在一起）—— 取「各自完整描框」的口径。
                // **正在二级面板里编辑的那根**换成黄色描边 + 一层淡黄打底，一眼看出在改哪根。
                if (_pipeCells.TryGetValue(new Vector2Int(col, gridZ), out var coveringPipe) && coveringPipe != null)
                {
                    bool selected = coveringPipe == _pipeColorTarget;
                    if (selected)
                        EditorGUI.DrawRect(rect, new Color(1f, 0.85f, 0.2f, 0.28f));

                    DrawPipeOutline(rect, col, gridZ, coveringPipe, selected);
                }

                // 正在二级面板里编辑的那个箱子：淡黄打底 + 黄色外框（与选中管道同一套「选中」口径）
                if (_boxCells.TryGetValue(new Vector2Int(col, gridZ), out var coveringBox) && coveringBox == _boxColorTarget)
                    DrawSelectedBoxHighlight(rect, col, gridZ, coveringBox);

                // 木箱覆盖区域：**双层粗描边（外棕内黄）**，常显；只看这格属于哪只木箱，几只挨着时各自完整描框
                if (_crateCells.TryGetValue(new Vector2Int(col, gridZ), out var coveringCrate) && coveringCrate != null)
                    DrawCrateOutline(rect, col, gridZ, coveringCrate);

                // 冰组覆盖区域：**双层粗描边（外深蓝内浅蓝）**，常显；
                // 正在「编辑冰」里选中的那一组另加一层淡黄打底（蓝描边保留，免得看不出是冰）
                if (_iceCells.TryGetValue(new Vector2Int(col, gridZ), out var coveringIce) && coveringIce != null)
                {
                    if (_mode == Mode.EditIce && coveringIce == _iceEditTarget)
                        EditorGUI.DrawRect(rect, new Color(1f, 0.85f, 0.2f, 0.22f));

                    DrawIceOutline(rect, col, gridZ, coveringIce);
                }

                // 矩形拖动中的实时预览：范围内的格叠一层半透明黄
                if (_rectActive && InDraggedRect(col, gridZ))
                    EditorGUI.DrawRect(rect, new Color(1f, 0.9f, 0.3f, 0.35f));

                // 墙体 / 管道模式的高亮：待创建（划过的实色、补段 / 轨道格的淡色）/ 待删除的（红）
                if (_mode == Mode.AddWall && _wallStrokeCells != null)
                {
                    var cell = new Vector2Int(col, gridZ);
                    if (_wallStroke.Contains(cell))
                        EditorGUI.DrawRect(rect, new Color(1f, 0.45f, 0.15f, 0.5f));
                    else if (_wallStrokeCells.Contains(cell))
                        EditorGUI.DrawRect(rect, new Color(1f, 0.45f, 0.15f, 0.22f));
                }
                else if (_mode == Mode.AddPipe && _pipeStrokeCells != null)
                {
                    var cell = new Vector2Int(col, gridZ);
                    if (_pipeStroke.Contains(cell))
                        EditorGUI.DrawRect(rect, new Color(0.15f, 0.75f, 0.95f, 0.5f));
                    else if (_pipeStrokeCells.Contains(cell))
                        EditorGUI.DrawRect(rect, new Color(0.15f, 0.75f, 0.95f, 0.22f));
                }
                else if (_mode == Mode.AddGate && _gateStrokeCells != null)
                {
                    // 待创建的门线：按下格 = 起点、当前格 = 终点，预览直接画「最终会占的那几格」
                    if (_gateStrokeCells.Contains(new Vector2Int(col, gridZ)))
                        EditorGUI.DrawRect(rect, new Color(1f, 0.45f, 0.15f, 0.4f));
                }
                else if (_mode == Mode.AddBox && _boxStrokeCells != null)
                {
                    // 待创建的箱子区域（矩形）：紫色，与墙 / 管 / 门的橙青区分开
                    if (_boxStrokeCells.Contains(new Vector2Int(col, gridZ)))
                        EditorGUI.DrawRect(rect, new Color(0.85f, 0.45f, 0.95f, 0.4f));
                }
                else if (_mode == Mode.AddCrate && _crateStrokeCells != null)
                {
                    // 待创建的木箱区域（矩形）：棕色
                    if (_crateStrokeCells.Contains(new Vector2Int(col, gridZ)))
                        EditorGUI.DrawRect(rect, new Color(0.55f, 0.35f, 0.15f, 0.45f));
                }
                else if (_mode == Mode.AddIce && _iceStrokeCells != null)
                {
                    // 待创建的冰组区域（画布上按矩形建，矩形天然连通）：浅蓝
                    if (_iceStrokeCells.Contains(new Vector2Int(col, gridZ)))
                        EditorGUI.DrawRect(rect, new Color(0.35f, 0.7f, 0.95f, 0.4f));
                }
                else if (_mode == Mode.DeleteWall && IsCellOfPendingWall(col, gridZ))
                {
                    EditorGUI.DrawRect(rect, new Color(1f, 0.15f, 0.15f, 0.55f));
                }
                else if (_mode == Mode.DeletePipe && IsCellOfPendingPipe(col, gridZ))
                {
                    EditorGUI.DrawRect(rect, new Color(1f, 0.15f, 0.15f, 0.55f));
                }
                else if (_mode == Mode.DeleteGate && IsCellOfPendingGate(col, gridZ))
                {
                    EditorGUI.DrawRect(rect, new Color(1f, 0.15f, 0.15f, 0.55f));
                }
                else if (_mode == Mode.DeleteBox && IsCellOfPendingBox(col, gridZ))
                {
                    EditorGUI.DrawRect(rect, new Color(1f, 0.15f, 0.15f, 0.55f));
                }
                else if (_mode == Mode.DeleteCrate && IsCellOfPendingCrate(col, gridZ))
                {
                    EditorGUI.DrawRect(rect, new Color(1f, 0.15f, 0.15f, 0.55f));
                }
                else if (_mode == Mode.DeleteIce && IsCellOfPendingIce(col, gridZ))
                {
                    EditorGUI.DrawRect(rect, new Color(1f, 0.15f, 0.15f, 0.55f));
                }
            }

            if (rect.Contains(Event.current.mousePosition))
                _hover = new Vector2Int(col, gridZ);
        }

        private static Color BackgroundOf(CellKind kind)
        {
            switch (kind)
            {
                case CellKind.Empty: return new Color(0.14f, 0.14f, 0.16f);
                case CellKind.Wall: return new Color(0.36f, 0.36f, 0.40f);
                case CellKind.Pipe: return new Color(0.22f, 0.32f, 0.44f);
                case CellKind.Box: return new Color(0.46f, 0.34f, 0.18f);
                case CellKind.Crate: return new Color(0.40f, 0.28f, 0.14f);
                case CellKind.Gate: return new Color(0.18f, 0.42f, 0.42f);
                case CellKind.Ice: return new Color(0.28f, 0.48f, 0.62f);
                case CellKind.Elevator: return new Color(0.38f, 0.24f, 0.44f);
                default: return new Color(0.14f, 0.14f, 0.16f);
            }
        }

        private static string MarkerOf(CellKind kind)
        {
            switch (kind)
            {
                case CellKind.Wall: return "墙";
                case CellKind.Pipe: return "管";
                case CellKind.Box: return "箱";
                case CellKind.Crate: return "木";
                case CellKind.Gate: return "门";
                case CellKind.Ice: return "冰";
                case CellKind.Elevator: return "升";
                default: return "";
            }
        }

        private GUIStyle NumStyle(int size)
        {
            if (_numStyle == null || _numStyleSize != size)
            {
                _numStyleSize = size;
                _numStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontSize = size,
                    alignment = TextAnchor.MiddleCenter,
                };
                _numStyle.normal.textColor = Color.white;
            }
            return _numStyle;
        }

        private GUIStyle MarkStyle(int size)
        {
            if (_markStyle == null || _markStyleSize != size)
            {
                _markStyleSize = size;
                _markStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontSize = size,
                    alignment = TextAnchor.MiddleCenter,
                };
                _markStyle.normal.textColor = Color.white;
            }
            return _markStyle;
        }

        /// <summary>底部状态行：固定只占一条等高矩形，别让它把上面 / 下面的控件顶走。</summary>
        private void DrawStatusLine()
        {
            var rect = GUILayoutUtility.GetRect(0f, 18f, GUILayout.ExpandWidth(true));
            if (Event.current.type != EventType.Repaint)
                return;

            string text;

            if (_mode == Mode.AddWall)
            {
                text = "待创建墙线：" + _wallStroke.Count + " 格";
                if (ShouldCloseStroke())
                    text += "　｜　首尾相邻 → 创建为**闭环**墙体（补段已用淡色预览）";
                if (_wallStroke.Count > 0)
                {
                    text += TryValidateWallStroke(_wallStrokeCells, out string reason, out _)
                        ? "　｜　可生成 ✓ 松手即创建（化简后 " + PendingWallPoints().Count + " 个端点）"
                        : "　｜　✗ " + reason;
                }
                else
                {
                    text += "　｜　按住左键沿行 / 列拖出墙线，**松手即创建**（合法才建）";
                    if (_lastStrokeError != null)
                        text += "　｜　上一笔未创建 ✗ " + _lastStrokeError;
                }
            }
            else if (_mode == Mode.DeleteWall)
            {
                if (_deletePendingWall != null)
                    text = "已高亮 " + _deletePendingWall.name + "（" + _deletePendingWall.OccupiedCellCount() +
                           " 格）—— 再点一下删除（不回填 pixel）";
                else
                    text = "点一下墙体高亮，再点一次删除；点空格取消高亮";
            }
            else if (_mode == Mode.AddPipe)
            {
                text = "待创建管道：" + _pipeStroke.Count + " 格（首格 = 管道格，其余 = 轨道格）";
                if (_pipeStroke.Count > 0)
                {
                    text += TryValidatePipeStroke(_pipeStrokeCells, out string pipeReason, out _)
                        ? "　｜　可生成 ✓ 松手即创建（端点 " + PipeStrokePoints().Count + " 个，覆盖 " +
                          (_pipeStrokeCells != null ? _pipeStrokeCells.Count : 0) + " 格）"
                        : "　｜　✗ " + pipeReason;
                }
                else
                {
                    text += "　｜　按住左键沿行 / 列拖出管道轨迹，**松手即创建**（合法才建）";
                    if (_lastStrokeError != null)
                        text += "　｜　上一笔未创建 ✗ " + _lastStrokeError;
                }
            }
            else if (_mode == Mode.DeletePipe)
            {
                if (_deletePendingPipe != null)
                    text = "已高亮 " + _deletePendingPipe.name + " —— 再点一下删除（不回填 pixel）";
                else
                    text = "点一下管道覆盖区高亮，再点一次删除；点空格取消高亮";
            }
            else if (_mode == Mode.AddGate)
            {
                text = "待创建倍乘门：(" + _gateAnchor.x + ", " + _gateAnchor.y + ") → (" +
                       _gateCurrent.x + ", " + _gateCurrent.y + ")　｜　倍率 x" + GateMultiplier + "（固定）";
                if (_gateAnchor != _gateCurrent)
                {
                    text += TryValidateGateStroke(_gateStrokeCells, out string gateReason, out _)
                        ? "　｜　可生成 ✓ 松手即创建（" + (_gateStrokeCells != null ? _gateStrokeCells.Count : 0) +
                          " 格，门格上的 Pixel 会被清掉并存快照）"
                        : "　｜　✗ " + gateReason;
                }
                else
                {
                    text += "　｜　按住左键拖到另一格，**松手即创建**（门必须是轴对齐笔直线段，≥2 格）";
                    if (_lastStrokeError != null)
                        text += "　｜　上一笔未创建 ✗ " + _lastStrokeError;
                }
            }
            else if (_mode == Mode.DeleteGate)
            {
                if (_deletePendingGate != null)
                    text = "已高亮 " + _deletePendingGate.name + "（" + _deletePendingGate.CellCount +
                           " 格）—— 再点一下删除（按快照还原 Pixel）";
                else
                    text = "点一下门格高亮，再点一次删除；点空格取消高亮";
            }
            else if (_mode == Mode.AddBox)
            {
                NormalizeBoxRect(out int b0, out int br0, out int b1, out int br1);
                text = "待创建箱子：(" + b0 + ", " + br0 + ") ~ (" + b1 + ", " + br1 + ")" +
                       "　｜　" + (b1 - b0 + 1) + "×" + (br1 - br0 + 1) + " 格";
                if (_boxAnchor != _boxCurrent)
                {
                    text += TryValidateBoxStroke(_boxStrokeCells, out string boxReason, out _)
                        ? "　｜　可生成 ✓ 松手即创建（容量 " +
                          Mathf.Max(1, BoxItem.ComputeCapacity(_group, b0, br0, b1, br1)) +
                          "，区域内 Pixel 会被吃掉）"
                        : "　｜　✗ " + boxReason;
                }
                else
                {
                    text += "　｜　按住左键拖出 **2×2** 的矩形，**松手即创建**（箱内颜色建完可点它改）";
                    if (_lastStrokeError != null)
                        text += "　｜　上一笔未创建 ✗ " + _lastStrokeError;
                }
            }
            else if (_mode == Mode.DeleteBox)
            {
                if (_deletePendingBox != null)
                    text = "已高亮 " + _deletePendingBox.name + "（" + _deletePendingBox.BodyCount +
                           " 格，容量 " + _deletePendingBox.capacity + "）—— 再点一下删除（不回填区域 Pixel）";
                else
                    text = "点一下箱子区域高亮，再点一次删除；点空格取消高亮";
            }
            else if (_mode == Mode.AddCrate)
            {
                NormalizeCrateRect(out int k0, out int kr0, out int k1, out int kr1);
                text = "待创建木箱：(" + k0 + ", " + kr0 + ") ~ (" + k1 + ", " + kr1 + ")　｜　" +
                       (k1 - k0 + 1) + "×" + (kr1 - kr0 + 1) + " 格　｜　拆箱需移出 " + CrateDestroyAfterMoves + " 次";
                if (_crateAnchor != _crateCurrent)
                {
                    text += TryValidateCrateStroke(_crateStrokeCells, out string crateReason, out _)
                        ? "　｜　可生成 ✓ 松手即创建（区域内像素原样保留）"
                        : "　｜　✗ " + crateReason;
                }
                else
                {
                    text += "　｜　按住左键拖出矩形（长宽至少 2 格），**松手即创建**";
                    if (_lastStrokeError != null)
                        text += "　｜　上一笔未创建 ✗ " + _lastStrokeError;
                }
            }
            else if (_mode == Mode.DeleteCrate)
            {
                if (_deletePendingCrate != null)
                    text = "已高亮 " + _deletePendingCrate.name + "（" + _deletePendingCrate.ColCount + "×" +
                           _deletePendingCrate.RowCount + " 格）—— 再点一下删除（底下 Pixel 保留）";
                else
                    text = "点一下木箱区域高亮，再点一次删除；点空格取消高亮";
            }
            else if (_mode == Mode.AddIce)
            {
                if (_iceFreeform)
                {
                    // 自由涂抹：按「已划多少格」报，不报矩形尺寸
                    text = "待创建冰组：自由涂抹（逐格划过）　｜　已划 " + _iceStroke.Count +
                           " 格　｜　次数 " + DefaultIceFreezeCount;
                    if (_iceStroke.Count >= 2)
                    {
                        text += TryValidateIceStroke(_iceStrokeCells, out string freeReason, out _)
                            ? "　｜　可生成 ✓ 松手即创建（格上像素原样保留）"
                            : "　｜　✗ " + freeReason;
                    }
                    else
                    {
                        text += "　｜　按住左键逐格划过（划到哪格算哪格），**松手即创建**";
                        if (_lastStrokeError != null)
                            text += "　｜　上一笔未创建 ✗ " + _lastStrokeError;
                    }
                }
                else
                {
                    NormalizeIceRect(out int i0, out int ir0, out int i1, out int ir1);
                    text = "待创建冰组：(" + i0 + ", " + ir0 + ") ~ (" + i1 + ", " + ir1 + ")　｜　" +
                           (i1 - i0 + 1) + "×" + (ir1 - ir0 + 1) + " 格　｜　次数 " + DefaultIceFreezeCount;
                    if (_iceAnchor != _iceCurrent)
                    {
                        text += TryValidateIceStroke(_iceStrokeCells, out string iceReason, out _)
                            ? "　｜　可生成 ✓ 松手即创建（格上像素原样保留）"
                            : "　｜　✗ " + iceReason;
                    }
                    else
                    {
                        text += "　｜　按住左键拖出矩形，**松手即创建**（勾「冰·自由涂抹」改成逐格划过）";
                        if (_lastStrokeError != null)
                            text += "　｜　上一笔未创建 ✗ " + _lastStrokeError;
                    }
                }
            }
            else if (_mode == Mode.DeleteIce)
            {
                if (_deletePendingIce != null)
                    text = "已高亮 " + _deletePendingIce.name + "（" + _deletePendingIce.CellCount +
                           " 格，次数 " + _deletePendingIce.freezeCount + "）—— 再点一下删除（格上 Pixel 保留）";
                else
                    text = "点一下冰格高亮，再点一次删除；点空格取消高亮";
            }
            else if (_mode == Mode.EditIce)
            {
                if (_iceEditTarget == null)
                    text = "点一组冰（冰格）选中来编辑；四个字段即时预览，「保存编辑」才落库";
                else
                    text = "编辑中：" + _iceEditTarget.name + "　｜　次数 " + _iceEditing.freezeCount +
                           (_iceEditing.meltOnlyWhenExposed ? "（仅暴露后扣减）" : "（一直扣减）") +
                           "　｜　" + (_iceDirty ? "**有未保存的改动**" : "已保存");
            }
            else if (_rectActive)
            {
                int c0, c1, r0, r1;
                NormalizeRect(out c0, out c1, out r0, out r1);
                text = "矩形 (" + c0 + ", " + r0 + ") → (" + c1 + ", " + r1 + ")　｜　松手落库";
            }
            else if (_hover.x < 0)
            {
                text = "悬停到格子上看它是什么";
            }
            else
            {
                int colorId;
                CellKind kind = Classify(_hover.x, _hover.y, out colorId);
                var item = _group.GetItem(_hover.x, _hover.y);

                text = "格 (" + _hover.x + ", " + _hover.y + ")：" + DescribeOf(kind);
                if (_wallCells.TryGetValue(_hover, out var hoveredWall) && hoveredWall != null)
                    text += "【" + hoveredWall.name + "】";
                if (item != null)
                    text += "，底下像素颜色 " + item.colorId + (item.isQuestion ? "（问号）" : "");
            }

            text += "　｜　网格 " + _group.columns + " 列 × " + _group.TotalRows + " 行";

            if (_mode == Mode.Color)
                text += "　｜　笔刷 " + (_brush < 0 ? "橡皮" : "颜色 " + _brush) +
                        "　｜　工具 " + (_tool == Tool.Free ? "自由涂" : "矩形");
            else if (_mode == Mode.Question)
                text += "　｜　" + (_questionGroup == null
                    ? "按下有像素的格子：不是问号 → 标记，已是问号 → 取消（只作用于按下位置同色相连的那一组）"
                    : "本笔：" + (_questionTarget ? "标记问号" : "取消问号") +
                      "（过滤组 " + _questionGroup.Count + " 格，仅按下位置同色相连）");

            if (_pipeColorTarget != null)
                text += "　｜　二级面板：管道 " + _pipeColorTarget.name + " 波次 " +
                        (_pipeColorTarget.colors != null ? _pipeColorTarget.colors.Count : 0) + " 个" +
                        (_brush < 0 ? "（ColorConfig 里没有颜色）" : "（当前选中色 " + _brush + "，选色用上面的调色板）");

            if (_boxColorTarget != null)
                text += "　｜　二级面板：箱子 " + _boxColorTarget.name + " 内容 " +
                        (_boxColorTarget.colorIds != null ? _boxColorTarget.colorIds.Length : 0) + " 个" +
                        (_brush < 0 ? "（ColorConfig 里没有颜色）" : "（当前选中色 " + _brush + "，选色用上面的调色板）");

            EditorGUI.LabelField(rect, text, EditorStyles.miniLabel);
        }

        private static string DescribeOf(CellKind kind)
        {
            switch (kind)
            {
                case CellKind.Empty: return "空格（可涂）";
                case CellKind.Color: return "颜色格（可涂）";
                case CellKind.Wall: return "墙体（不可涂）";
                case CellKind.Pipe: return "管道（不可涂）";
                case CellKind.Box: return "未开箱的箱子（不可涂）";
                case CellKind.Crate: return "木箱本体（不可涂）";
                case CellKind.Gate: return "倍乘门门格（不可涂）";
                case CellKind.Ice: return "冰组冻结中（不可涂）";
                case CellKind.Elevator: return "升降台（不可涂）";
                default: return "未知";
            }
        }

        // ============================================================
        // 手势
        // ============================================================

        private void HandleCell(Rect rect, int col, int gridZ)
        {
            if (Application.isPlaying)
                return;

            Event ev = Event.current;

            if (ev.type == EventType.MouseDown && rect.Contains(ev.mousePosition))
            {
                if (_mode == Mode.Color)
                {
                    // 点到管道格 / 箱子区域 → 打开对应的「内部颜色」二级面板（两者互斥，只留一个）
                    bool openedPanel = TryOpenPipeColorPanel(col, gridZ);
                    if (openedPanel)
                    {
                        _boxColorTarget = null;
                    }
                    else if (TryOpenBoxColorPanel(col, gridZ))
                    {
                        _pipeColorTarget = null;
                        openedPanel = true;
                    }
                    else
                    {
                        // 点到两个面板绑定的物件以外 → 退出面板，照常涂色
                        _pipeColorTarget = null;
                        _boxColorTarget = null;
                    }

                    if (openedPanel)
                    {
                        // 这一下不涂色、也不开笔（末尾统一 ev.Use() + Repaint()）
                    }
                    else if (_tool == Tool.Free)
                    {
                        StartStroke();
                        PaintCell(col, gridZ);
                    }
                    else
                    {
                        StartStroke();
                        _rectActive = true;
                        _rectAnchor = new Vector2Int(col, gridZ);
                        _rectCurrent = _rectAnchor;
                    }
                }
                else if (_mode == Mode.Question)
                {
                    BeginQuestionStroke(col, gridZ);   // 先定这一笔的方向 + 过滤集，再处理按下这一格
                }
                else if (_mode == Mode.AddWall)
                {
                    // 一次拖动 = 一条新墙线：按下时清掉上一条待创建的
                    _dragging = true;
                    _rectActive = false;
                    _wallStroke.Clear();
                    _lastStrokeError = null;
                    AppendWallCell(col, gridZ);
                }
                else if (_mode == Mode.AddPipe)
                {
                    // 一次拖动 = 一条新管道轨迹（与墙体同构：松手即创建）
                    _dragging = true;
                    _rectActive = false;
                    _pipeStroke.Clear();
                    _lastStrokeError = null;
                    AppendPipeCell(col, gridZ);
                }
                else if (_mode == Mode.AddGate)
                {
                    // 门的两个端点：按下格 = 起点、当前格 = 终点（门必须是轴对齐笔直线段，松手即创建）
                    _dragging = true;
                    _rectActive = false;
                    _gateAnchor = new Vector2Int(col, gridZ);
                    _gateCurrent = _gateAnchor;
                    _lastStrokeError = null;
                }
                else if (_mode == Mode.AddBox)
                {
                    // 箱子的两个对角：按下格 + 当前格（矩形区域，松手即创建）
                    _dragging = true;
                    _rectActive = false;
                    _boxAnchor = new Vector2Int(col, gridZ);
                    _boxCurrent = _boxAnchor;
                    _lastStrokeError = null;
                }
                else if (_mode == Mode.AddCrate)
                {
                    // 木箱的两个对角：按下格 + 当前格（矩形区域，松手即创建）
                    _dragging = true;
                    _rectActive = false;
                    _crateAnchor = new Vector2Int(col, gridZ);
                    _crateCurrent = _crateAnchor;
                    _lastStrokeError = null;
                }
                else if (_mode == Mode.AddIce)
                {
                    _dragging = true;
                    _rectActive = false;
                    _lastStrokeError = null;

                    if (_iceFreeform)
                    {
                        // 自由涂抹：这一笔的格 = 划过的格（逐格计入）
                        _iceStroke.Clear();
                        AppendIceCell(col, gridZ);
                    }
                    else
                    {
                        // 矩形填充：按下格 + 当前格两个对角
                        _iceAnchor = new Vector2Int(col, gridZ);
                        _iceCurrent = _iceAnchor;
                    }
                }
                else if (_mode == Mode.DeletePipe)
                {
                    HandlePipeDeleteClick(col, gridZ);   // 单击，不进入拖动手势
                }
                else if (_mode == Mode.DeleteGate)
                {
                    HandleGateDeleteClick(col, gridZ);   // 单击，不进入拖动手势
                }
                else if (_mode == Mode.DeleteBox)
                {
                    HandleBoxDeleteClick(col, gridZ);    // 单击，不进入拖动手势
                }
                else if (_mode == Mode.DeleteCrate)
                {
                    HandleCrateDeleteClick(col, gridZ);  // 单击，不进入拖动手势
                }
                else if (_mode == Mode.DeleteIce)
                {
                    HandleIceDeleteClick(col, gridZ);    // 单击，不进入拖动手势
                }
                else if (_mode == Mode.EditIce)
                {
                    HandleIceEditClick(col, gridZ);      // 单击选中一个冰组来编辑
                }
                else
                {
                    HandleWallDeleteClick(col, gridZ);   // 单击，不进入拖动手势
                }

                ev.Use();
                Repaint();
            }
            else if (ev.type == EventType.MouseDrag && _dragging && rect.Contains(ev.mousePosition))
            {
                if (_mode == Mode.Color)
                {
                    if (_tool == Tool.Free)
                        PaintCell(col, gridZ);
                    else if (_rectActive)
                        _rectCurrent = new Vector2Int(col, gridZ);
                }
                else if (_mode == Mode.Question)
                {
                    PaintQuestionCell(col, gridZ);
                }
                else if (_mode == Mode.AddWall)
                {
                    AppendWallCell(col, gridZ);
                }
                else if (_mode == Mode.AddPipe)
                {
                    AppendPipeCell(col, gridZ);
                }
                else if (_mode == Mode.AddGate)
                {
                    _gateCurrent = new Vector2Int(col, gridZ);   // 只记终点：预览/校验按「起点→终点」现算
                }
                else if (_mode == Mode.AddBox)
                {
                    _boxCurrent = new Vector2Int(col, gridZ);    // 只记对角：矩形由两个角现算
                }
                else if (_mode == Mode.AddCrate)
                {
                    _crateCurrent = new Vector2Int(col, gridZ);
                }
                else if (_mode == Mode.AddIce)
                {
                    if (_iceFreeform)
                        AppendIceCell(col, gridZ);
                    else
                        _iceCurrent = new Vector2Int(col, gridZ);
                }

                ev.Use();
                Repaint();
            }
        }

        /// <summary>开一笔：整笔一个 Undo 组，拖动期间不清 <c>dirty</c>。</summary>
        private void StartStroke()
        {
            Undo.IncrementCurrentGroup();
            _undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(_tool == Tool.Free ? "画布涂色" : "画布矩形涂色");

            _dragging = true;
            _dirty = false;
            _rectActive = false;
            _strokePainted.Clear();
        }

        /// <summary>
        /// 松手收尾（含拖到画布外松手）。
        /// 涂颜色 / 问号标注：矩形到这一刻才真正落库，随后统一重建网格 + 落盘 + 收拢 Undo。
        /// 添加墙体 / 添加管道：**松手即创建**（体检通过才建，不通过由状态行给出原因）。
        /// </summary>
        private void HandleStrokeEnd()
        {
            Event ev = Event.current;
            if (ev.type != EventType.MouseUp || !_dragging)
                return;

            _dragging = false;

            if (_mode == Mode.Color || _mode == Mode.Question)
            {
                if (_tool == Tool.Rect && _rectActive && !Application.isPlaying)
                    PaintRect();

                _rectActive = false;
                _strokePainted.Clear();
                _questionGroup = null;   // 问号模式的过滤集只在一笔内有效

                if (_dirty)
                {
                    _dirty = false;
                    _group.RebuildGrid();          // 占用表 / 冰 / 木箱掩码跟着新像素一起刷新
                    EditorUtility.SetDirty(_group);
                    SceneView.RepaintAll();
                }

                Undo.CollapseUndoOperations(_undoGroup);   // 整笔一步撤销
            }
            else if (_mode == Mode.AddWall)
            {
                FinishWallStroke();
            }
            else if (_mode == Mode.AddPipe)
            {
                FinishPipeStroke();
            }
            else if (_mode == Mode.AddGate)
            {
                FinishGateStroke();
            }
            else if (_mode == Mode.AddBox)
            {
                FinishBoxStroke();
            }
            else if (_mode == Mode.AddCrate)
            {
                FinishCrateStroke();
            }
            else if (_mode == Mode.AddIce)
            {
                FinishIceStroke();
            }

            Repaint();
            ev.Use();
        }

        /// <summary>中途放弃这一笔（关窗 / 进出 Play）：只清手势状态，不落库。</summary>
        private void CancelStroke()
        {
            _dragging = false;
            _rectActive = false;
            _strokePainted.Clear();
            _questionGroup = null;
        }

        /// <summary>把矩形两角归一化并夹进网格范围。</summary>
        private void NormalizeRect(out int c0, out int c1, out int r0, out int r1)
        {
            c0 = Mathf.Clamp(Mathf.Min(_rectAnchor.x, _rectCurrent.x), 0, Mathf.Max(0, _group.columns - 1));
            c1 = Mathf.Clamp(Mathf.Max(_rectAnchor.x, _rectCurrent.x), 0, Mathf.Max(0, _group.columns - 1));
            r0 = Mathf.Clamp(Mathf.Min(_rectAnchor.y, _rectCurrent.y), 0, Mathf.Max(0, _group.TotalRows - 1));
            r1 = Mathf.Clamp(Mathf.Max(_rectAnchor.y, _rectCurrent.y), 0, Mathf.Max(0, _group.TotalRows - 1));
        }

        private bool InDraggedRect(int col, int gridZ)
        {
            int c0, c1, r0, r1;
            NormalizeRect(out c0, out c1, out r0, out r1);
            return col >= c0 && col <= c1 && gridZ >= r0 && gridZ <= r1;
        }

        /// <summary>矩形模式松手落库：逐格走同一条 <see cref="PaintCell"/>（橡皮笔刷就是「矩形抹」）。</summary>
        private void PaintRect()
        {
            int c0, c1, r0, r1;
            NormalizeRect(out c0, out c1, out r0, out r1);

            for (int r = r0; r <= r1; r++)
                for (int c = c0; c <= c1; c++)
                    PaintCell(c, r);
        }

        /// <summary>
        /// 涂 / 擦一格。障碍格直接跳过；橡皮只在有像素时销毁；颜色笔在有像素时改写 colorId、在空格上新建像素。
        ///
        /// **新建 / 销毁之后立刻同步 <c>group.grid</c>**：画布是读 <c>group.grid</c> 画的，而
        /// <see cref="PixelGroup.SpawnPixel"/> 只创建对象、**不写这张表** —— 不同步的话，拖动中新建的空格
        /// 要等松手 <c>RebuildGrid</c> 才显示（场景里其实早就有了），编辑窗看着就像延迟了一拍。
        /// `grid` 是运行时表、每次 RebuildGrid 都按子物体重建，所以这里的写入不会留下痕迹。
        ///
        /// 同一笔内重复划过同一格由 <see cref="_strokePainted"/> 挡掉。
        /// </summary>
        private void PaintCell(int col, int row)
        {
            if (Application.isPlaying || !_group.IsInRange(col, row))
                return;

            if (!_strokePainted.Add(new Vector2Int(col, row)))
                return;   // 本笔已经涂过这一格

            int colorId;
            CellKind kind = Classify(col, row, out colorId);
            if (!IsBrushEditable(kind))
                return;   // 障碍格不可涂（木箱盖住的像素格除外）

            var item = _group.GetItem(col, row);

            if (_brush < 0)
            {
                if (item == null)
                    return;

                Undo.DestroyObjectImmediate(item.gameObject);
                _group.grid[col, row] = null;   // 见下面 PaintCell 开头关于「即时同步 grid」的说明
                _dirty = true;
                return;
            }

            if (item == null)
            {
                if (_group.pixelPrefab == null)
                {
                    Debug.LogWarning("[像素颜色画布] PixelGroup.pixelPrefab 为空，无法新建像素。");
                    return;
                }

                var spawned = _group.SpawnPixel(col, row, _brush, _config);
                if (spawned == null)
                    return;

                Undo.RegisterCreatedObjectUndo(spawned.gameObject, "画布涂色");
                _group.grid[col, row] = spawned;   // 同上：立刻可见，不必等松手
                _dirty = true;
                return;
            }

            if (item.colorId == _brush)
                return;   // 已经是这个颜色：不必记一条空 Undo

            Undo.RecordObject(item, "画布涂色");
            // 颜色的**视觉**来自 Renderer.sharedMaterial（ApplyMaterial 改的是 renderer 组件，不是 PixelItem 上的字段）：
            // 只记 PixelItem 会出现「colorId 撤销回来了、场景里的颜色没变」——窗口读 colorId 先变回旧色、
            // 场景还顶着新材质，两边对不上。所以 renderer 也要逐个记（与 PixelItemEditor / PixelGroupEditor
            // 改色时的既有口径一致）。
            for (int i = 0; i < item.renderers.Count; i++)
            {
                var renderer = item.renderers[i];
                if (renderer != null)
                    Undo.RecordObject(renderer, "画布涂色");
            }

            item.colorId = _brush;
            item.ApplyMaterial(_config);
            EditorUtility.SetDirty(item);
            _dirty = true;
        }

        /// <summary>
        /// 问号模式：按下时定下这一笔的**方向**与**过滤集**，然后处理按下这一格。
        ///
        /// · 方向：按下那格**原本是问号 → 取消**、**原本不是 → 标记**。所以同一笔不会又标又取，
        ///   也不需要调色板里再放一个「问号色」（那样会跟「问号是一个 flag 而不是颜色」相矛盾）。
        /// · 过滤集：按下那格所属的**同色四向连通组**（只看颜色与连通，与调色板无关）。
        ///   拖动只在这一组内生效，划出组外一律不动 —— 避免扫过边界时误标到别的颜色 / 别的组。
        ///
        /// 按下处没得可标（空格 / 障碍格 / 没有像素）时**不开笔**，连空 Undo 组都不记。
        /// </summary>
        private void BeginQuestionStroke(int col, int row)
        {
            _questionGroup = null;
            if (Application.isPlaying || !_group.IsInRange(col, row))
                return;

            var item = _group.GetItem(col, row);
            if (item == null)
                return;                                 // 空格：没有像素可标
            if (!IsPaintable(Classify(col, row, out _)))
                return;                                 // 障碍格（含冰）不动

            _questionGroup = CollectSameColorGroup(col, row, item.colorId);
            _questionTarget = !item.isQuestion;

            StartStroke();                              // 与涂色同一套手势：整笔一个 Undo 组
            PaintQuestionCell(col, row);
        }

        /// <summary>
        /// 问号模式：涂过一格。只处理落在 <see cref="_questionGroup"/> 里的格（= 按下位置同色相连的那一组），
        /// 组外一律不动；已经是目标状态的格不重复写，也不记空 Undo。
        ///
        /// 开关问号要连 Renderer 与 questionObject 一起记 Undo：问号的视觉来自 questionMaterial
        /// （<c>renderer.sharedMaterial</c>）与 questionObject 的显隐，只记 PixelItem 会变成
        /// 「flag 撤销回来了、场景样子没变」（与 <c>PixelItemEditor.SetQuestionAll</c> 同口径）。
        /// </summary>
        private void PaintQuestionCell(int col, int row)
        {
            if (Application.isPlaying || _questionGroup == null)
                return;
            if (!_questionGroup.Contains(new Vector2Int(col, row)))
                return;   // 组外：越界不处理

            if (!_strokePainted.Add(new Vector2Int(col, row)))
                return;   // 本笔已经处理过这一格

            if (!IsBrushEditable(Classify(col, row, out _)))
                return;   // 障碍格（含冰）不动；木箱盖住的像素格照常可标

            var item = _group.GetItem(col, row);
            if (item == null || item.isQuestion == _questionTarget)
                return;   // 空格 / 已是目标状态：不记一条空 Undo

            string op = _questionTarget ? "标记问号" : "取消问号";
            Undo.RecordObject(item, op);
            for (int i = 0; i < item.renderers.Count; i++)
            {
                var renderer = item.renderers[i];
                if (renderer != null)
                    Undo.RecordObject(renderer, op);
            }
            if (item.questionObject != null)
                Undo.RecordObject(item.questionObject, op);

            item.isQuestion = _questionTarget;
            item.ApplyMaterial(_config);
            item.RefreshQuestionObject();
            EditorUtility.SetDirty(item);
            _dirty = true;
        }

        /// <summary>
        /// 取「同色四向连通组」：从 (col, row) 出发，只往**有像素且 colorId 相同**的四向邻居扩散。
        /// 只看颜色与连通、不看覆盖物 —— 冰 / 木箱盖住的像素仍是网格像素，组不该被它们截断；
        /// 而墙 / 管 / 门格上根本没有像素（<c>GetItem</c> 为 null），天然就是断点。
        /// </summary>
        private HashSet<Vector2Int> CollectSameColorGroup(int col, int row, int colorId)
        {
            var group = new HashSet<Vector2Int>();
            var stack = new Stack<Vector2Int>();

            var start = new Vector2Int(col, row);
            group.Add(start);
            stack.Push(start);

            while (stack.Count > 0)
            {
                var cell = stack.Pop();
                Push(cell.x + 1, cell.y);
                Push(cell.x - 1, cell.y);
                Push(cell.x, cell.y + 1);
                Push(cell.x, cell.y - 1);
            }

            return group;

            void Push(int c, int r)
            {
                var cell = new Vector2Int(c, r);
                if (!_group.IsInRange(c, r) || group.Contains(cell))
                    return;

                var it = _group.GetItem(c, r);
                if (it == null || it.colorId != colorId)
                    return;

                group.Add(cell);
                stack.Push(cell);
            }
        }

        // ============================================================
        // 墙体：添加 / 删除
        // ============================================================

        /// <summary>把拖动经过的一格记进待创建墙线（连续重复的同一格只记一次 —— 手在同一个格子里会抖）。</summary>
        private void AppendWallCell(int col, int gridZ)
        {
            var cell = new Vector2Int(col, gridZ);
            if (_wallStroke.Count > 0 && _wallStroke[_wallStroke.Count - 1] == cell)
                return;
            _wallStroke.Add(cell);
        }

        /// <summary>
        /// 是否创建为闭环墙体：**只要求拖过的首尾格相邻**（曼哈顿距离 ≤ 1）。
        /// 距离 1 = 首尾相邻，闭合段正好补上那一格；距离 0 = 又拖回了起点格（线本身已经绕回来了）。
        /// 其它情况一律建开放墙体 —— 与「用选中 Pixel 创建墙体」同口径，之后可在 Inspector 里手动闭环。
        /// </summary>
        private bool ShouldCloseStroke()
        {
            if (_wallStroke.Count < 2)
                return false;

            var first = _wallStroke[0];
            var last = _wallStroke[_wallStroke.Count - 1];
            return Mathf.Abs(first.x - last.x) + Mathf.Abs(first.y - last.y) <= 1;
        }

        /// <summary>格列表 → 端点列表（WallItem / PipeItem 共用口径：Vector2 的 x = 列 col、y = 行 row）。</summary>
        private static List<Vector2> PointsOf(IList<Vector2Int> cells)
        {
            var points = new List<Vector2>(cells.Count);
            for (int i = 0; i < cells.Count; i++)
                points.Add(new Vector2(cells[i].x, cells[i].y));
            return points;
        }

        /// <summary>待创建墙线的端点。</summary>
        private List<Vector2> StrokePoints()
        {
            return PointsOf(_wallStroke);
        }

        /// <summary>相邻两格的方向（单位步长，取值只可能是 (1,0)/(-1,0)/(0,1)/(0,-1)）。</summary>
        private static Vector2Int DirectionOf(Vector2 from, Vector2 to)
        {
            // 必须用 System.Math.Sign：Mathf.Sign(0) 返回 1，会把纵向段算成斜向（(1,±1)），
            // 于是「横着走」和「竖着走」被判成同一方向，直线中段点会被误删。
            return new Vector2Int(
                System.Math.Sign(to.x - from.x),
                System.Math.Sign(to.y - from.y));
        }

        /// <summary>
        /// 把拖动轨迹化简为**必要的端点**：只保留真正的拐点，丢掉直线中段那些多余的点。
        ///
        /// 「多余」= 前后两段方向**完全相同**（同轴同向）。注意**反向折回不算多余**：
        /// A→B→A 里的 B 是墙的末端，删掉整段墙就没了，所以只比较方向是否相同、不看是否共线。
        /// 化简不改变墙体占据的格（同一条直线段覆盖的格完全一样），只是让 Inspector 里的端点表干净、
        /// Gizmos 与 BuildVisual 少绕几道；端点数从「拖过的格数」降到「拐点数」。
        /// </summary>
        private static List<Vector2> SimplifyPoints(List<Vector2> points)
        {
            if (points == null || points.Count <= 2)
                return points;

            var result = new List<Vector2>(points.Count) { points[0] };

            for (int i = 1; i < points.Count - 1; i++)
            {
                Vector2 prev = result[result.Count - 1];   // 上一个**保留下来的**点，不是 points[i-1]
                if (DirectionOf(prev, points[i]) == DirectionOf(points[i], points[i + 1]))
                    continue;                              // 直线中段：丢掉

                result.Add(points[i]);
            }

            result.Add(points[points.Count - 1]);           // 尾点必须留（它是墙的另一端）
            return result;
        }

        /// <summary>
        /// 待创建墙线最终会写进 <see cref="WallItem.points"/> 的端点：
        /// 拖动轨迹 → 化简为拐点 → 若拖回了起点格则去掉重复的尾点（交给 closed 补段）。
        /// 状态行与 <see cref="CreateWallFromStroke"/> **共用这一份**，免得「提示的端点数」和「真建出来的」不一致。
        /// </summary>
        private List<Vector2> PendingWallPoints()
        {
            var points = SimplifyPoints(StrokePoints());

            if (ShouldCloseStroke() && points.Count >= 2 && points[0] == points[points.Count - 1])
                points.RemoveAt(points.Count - 1);

            return points;
        }

        /// <summary>待创建墙线占据的格（含闭环段）—— 借 WallItem 的静态换算，与真正建出来的墙同一口径。</summary>
        private HashSet<Vector2Int> CollectStrokeCells()
        {
            var cells = new HashSet<Vector2Int>();
            WallItem.CollectOccupiedCells(StrokePoints(), ShouldCloseStroke(), cells);
            return cells;
        }

        /// <summary>
        /// 待创建墙线的体检：能否生成 + 不能生成的原因。<paramref name="occupied"/> 是它的占格集合
        /// （由调用方传入，好让一帧内的按钮判据 / 画布高亮 / 状态行共用同一份，不重复算）。
        ///
        /// 判据三条：≥ 2 格；相邻两格必须同行或同列（拖太快跳成对角就不行）；
        /// **不与已有墙体重叠** —— 重叠等于让新墙的墙块压在旧墙上（z-fighting）且同一条线变成两面墙，
        /// 而「已有墙体只能由『删除墙体』移除」是这套编辑的既定口径。
        /// </summary>
        private bool TryValidateWallStroke(HashSet<Vector2Int> occupied, out string error, out int overlapCells)
        {
            error = null;
            overlapCells = 0;

            if (_wallStroke.Count < 2)
            {
                error = "至少需要 2 格才能构成一段墙体。";
                return false;
            }

            for (int i = 0; i + 1 < _wallStroke.Count; i++)
            {
                var a = _wallStroke[i];
                var b = _wallStroke[i + 1];
                if (a.x != b.x && a.y != b.y)
                {
                    error = "第 " + (i + 1) + " 段（" + a.x + "," + a.y + " → " + b.x + "," + b.y +
                            "）既不同行也不同列（拖太快会跳成对角），请沿行 / 列拖。";
                    return false;
                }
            }

            if (occupied != null)
            {
                foreach (var cell in occupied)
                    if (_wallCells.ContainsKey(cell))
                        overlapCells++;
            }

            if (overlapCells > 0)
            {
                error = "与已有墙体重叠 " + overlapCells + " 格（已有墙体只能由「删除墙体」移除）。";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 「创建墙体」：按待创建墙线建一面 WallItem，并把**墙覆盖到的 pixel 直接移除**（不在别处回填）。
        /// 建墙复用 <see cref="PixelGroup.SpawnWall"/>（它负责 new GameObject + 四类墙块 BuildVisual），
        /// 这里只补它不管的三件事：端点化简（见 <see cref="SimplifyPoints"/>）、撤销登记、清掉覆盖格上的 pixel。
        /// </summary>
        private void CreateWallFromStroke()
        {
            if (_group == null)
                return;

            var occupied = CollectStrokeCells();
            if (!TryValidateWallStroke(occupied, out string reason, out _))
            {
                Debug.LogWarning("[像素颜色画布] 无法创建墙体：" + reason);
                return;
            }

            bool close = ShouldCloseStroke();
            var points = PendingWallPoints();   // 化简为拐点 + 去掉重复尾点

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("创建墙体");

            var wall = _group.SpawnWall(points, close);
            if (wall == null)
                return;

            Undo.RegisterCreatedObjectUndo(wall.gameObject, "创建墙体");

            _group.RebuildGrid();
            int removed = 0;
            foreach (var cell in occupied)
            {
                var item = _group.GetItem(cell.x, cell.y);
                if (item == null)
                    continue;
                Undo.DestroyObjectImmediate(item.gameObject);
                removed++;
            }

            _group.RebuildGrid();
            EditorUtility.SetDirty(_group);
            Undo.CollapseUndoOperations(undoGroup);

            _wallStroke.Clear();
            RefreshSnapshot();          // 重建「格 → 墙」表 + 占用表
            SceneView.RepaintAll();
            Repaint();

            Debug.Log("[像素颜色画布] 已创建墙体：端点 " + points.Count + " 个" +
                (close ? "（闭环）" : "（开放）") + "，占据 " + occupied.Count + " 格，移除 " + removed + " 个 Pixel。");
        }

        /// <summary>某格是否属于「已高亮待删除」的那面墙。</summary>
        private bool IsCellOfPendingWall(int col, int gridZ)
        {
            if (_deletePendingWall == null)
                return false;
            return _wallCells.TryGetValue(new Vector2Int(col, gridZ), out var wall) && wall == _deletePendingWall;
        }

        /// <summary>
        /// 「删除墙体」的单击：点到的墙若与已高亮的是同一面 → 真删；否则只把高亮切过去；
        /// 点到没有墙的格子 → 取消高亮。删除**不回填 pixel**（与 Inspector 的「移除墙体并填充 Pixel」相反）。
        /// </summary>
        private void HandleWallDeleteClick(int col, int gridZ)
        {
            if (!_wallCells.TryGetValue(new Vector2Int(col, gridZ), out var wall) || wall == null)
            {
                _deletePendingWall = null;
                return;
            }

            if (wall != _deletePendingWall)
            {
                _deletePendingWall = wall;   // 第一次点：只高亮
                return;
            }

            DeleteWall(wall);
        }

        /// <summary>删掉一面墙：整面墙一起销毁，它占的格留空（不填 pixel）。</summary>
        private void DeleteWall(WallItem wall)
        {
            if (wall == null || _group == null)
                return;

            string wallName = wall.name;

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("删除墙体");

            Undo.DestroyObjectImmediate(wall.gameObject);

            _group.RebuildGrid();
            EditorUtility.SetDirty(_group);
            Undo.CollapseUndoOperations(undoGroup);

            _deletePendingWall = null;
            RefreshSnapshot();
            SceneView.RepaintAll();
            Repaint();

            Debug.Log("[像素颜色画布] 已删除墙体 " + wallName + "（未回填 Pixel）。");
        }

        /// <summary>
        /// 「添加墙体」松手：体检通过就**直接建**（已取消「创建墙体」按钮），不通过就把原因写进
        /// <see cref="_lastStrokeError"/> 交给状态行显示。轨迹无论成败都清掉 —— 一笔就是一笔，想改只能删了重拖。
        /// 单击（1 格）不算「想建墙」，不报错。
        /// </summary>
        private void FinishWallStroke()
        {
            var occupied = CollectStrokeCells();
            bool ok = TryValidateWallStroke(occupied, out string reason, out _);

            _lastStrokeError = ok || _wallStroke.Count < 2 ? null : reason;

            if (ok)
                CreateWallFromStroke();   // 内部会清轨迹 + 重建快照
            else
                _wallStroke.Clear();

            Repaint();
        }

        // ============================================================
        // 管道：添加 / 删除（与墙体同构，区别见各自注释）
        // ============================================================

        /// <summary>把拖动经过的一格记进待创建管道轨迹（连续重复的同一格只记一次 —— 手在同一个格子里会抖）。</summary>
        private void AppendPipeCell(int col, int gridZ)
        {
            var cell = new Vector2Int(col, gridZ);
            if (_pipeStroke.Count > 0 && _pipeStroke[_pipeStroke.Count - 1] == cell)
                return;
            _pipeStroke.Add(cell);
        }

        /// <summary>待创建管道的端点：拖动轨迹 → 化简为拐点。管道**不闭环**（首尾相邻只是路径绕回来了，不是闭合语义）。</summary>
        private List<Vector2> PipeStrokePoints()
        {
            return SimplifyPoints(PointsOf(_pipeStroke));
        }

        /// <summary>
        /// 待创建管道覆盖的格（管道格 + 轨道格）—— 借 <see cref="PipeItem"/> 的静态换算，与真正建出来的同一口径。
        /// 轨道格在画布上显示为普通颜色格（那里确实有像素），但运行时算管道覆盖，所以删管 / 判重叠 / 画描边都得算进来。
        /// </summary>
        private HashSet<Vector2Int> CollectPipeStrokeCells()
        {
            var cells = new HashSet<Vector2Int>();
            var points = PipeStrokePoints();
            if (points.Count < 2 || _group == null)
                return cells;

            var pipeCell = PipeItem.GetPipeCell(points);
            if (_group.IsInRange(pipeCell.x, pipeCell.y))
                cells.Add(pipeCell);

            var track = new List<Vector2Int>();
            PipeItem.CollectTrackCells(points, _group.columns, _group.TotalRows, track);
            foreach (var c in track)
                cells.Add(c);

            return cells;
        }

        /// <summary>
        /// 待创建管道的体检：能否生成 + 不能生成的原因。<paramref name="covered"/> 是它的覆盖格集合
        /// （由调用方传入，好让一帧内的按钮判据 / 画布高亮 / 状态行共用同一份）。
        ///
        /// 判据三条：≥ 2 格（首格 = 管道格，其余 = 轨道格）；相邻两格必须同行或同列；
        /// **不与已有墙体 / 管道重叠** —— 重叠等于两个障碍抢同一格（z-fighting），
        /// 而「已有障碍只能用各自的删除模式移除」是这套编辑的既定口径。
        /// 注意：轨道格本来就该有像素（开局阻挡），所以这里**不**给它做像素校验。
        /// </summary>
        private bool TryValidatePipeStroke(HashSet<Vector2Int> covered, out string error, out int overlapCells)
        {
            error = null;
            overlapCells = 0;

            if (_pipeStroke.Count < 2)
            {
                error = "至少需要 2 格才能构成一根管道（首格 = 管道格，其余 = 轨道格）。";
                return false;
            }

            for (int i = 0; i + 1 < _pipeStroke.Count; i++)
            {
                var a = _pipeStroke[i];
                var b = _pipeStroke[i + 1];
                if (a.x != b.x && a.y != b.y)
                {
                    error = "第 " + (i + 1) + " 段（" + a.x + "," + a.y + " → " + b.x + "," + b.y +
                            "）既不同行也不同列（拖太快会跳成对角），请沿行 / 列拖。";
                    return false;
                }
            }

            if (covered != null)
            {
                foreach (var cell in covered)
                    if (_pipeCells.ContainsKey(cell) || _wallCells.ContainsKey(cell))
                        overlapCells++;
            }

            if (overlapCells > 0)
            {
                error = "与已有墙体 / 管道重叠 " + overlapCells + " 格（已有障碍只能用各自的删除模式移除）。";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 「创建管道」：按待创建轨迹建一根 PipeItem。复用 <see cref="PixelGroup.SpawnPipe"/>（它负责实例化
        /// pipePrefab、写 points/colors、定位到管道格），这里只补它不管的三件事：端点化简、撤销登记、
        /// **清掉管道格上的 pixel**（轨道格像素保留 —— 那是开局的阻挡，运行时要等它们被点走才轮到管道补位）。
        ///
        /// colors 默认取当前笔刷色（一个波次）：管道的波次颜色在 Inspector 里改，这里给一个能直接跑起来的初值；
        /// 笔刷是橡皮（或没选色）时留空，Inspector 会提示「colors 为空：该管道不会生成任何像素」。
        /// </summary>
        private void CreatePipeFromStroke()
        {
            if (_group == null)
                return;

            var covered = CollectPipeStrokeCells();
            if (!TryValidatePipeStroke(covered, out string reason, out _))
            {
                Debug.LogWarning("[像素颜色画布] 无法创建管道：" + reason);
                return;
            }

            var points = PipeStrokePoints();
            var colors = new List<int>();
            if (_brush >= 0)
                colors.Add(_brush);

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("创建管道");

            var pipe = _group.SpawnPipe(points, colors);
            if (pipe == null)
                return;   // 失败原因 SpawnPipe 已经打过日志（pipePrefab 为空 / 预制体缺 PipeItem）

            pipe.OrientBody();
            Undo.RegisterCreatedObjectUndo(pipe.gameObject, "创建管道");

            _group.RebuildGrid();
            var pipeCell = PipeItem.GetPipeCell(points);
            var coveredPixel = _group.GetItem(pipeCell.x, pipeCell.y);
            bool removed = false;
            if (coveredPixel != null)
            {
                Undo.DestroyObjectImmediate(coveredPixel.gameObject);
                removed = true;
            }

            _group.RebuildGrid();
            EditorUtility.SetDirty(_group);
            Undo.CollapseUndoOperations(undoGroup);

            _pipeStroke.Clear();
            RefreshSnapshot();          // 重建「格 → 管道」表 + 占用表
            SceneView.RepaintAll();
            Repaint();

            Debug.Log("[像素颜色画布] 已创建管道：端点 " + points.Count + " 个，覆盖 " + covered.Count +
                " 格（管道格 1 + 轨道 " + Mathf.Max(0, covered.Count - 1) + "），" +
                (removed ? "已移除管道格上的 1 个 Pixel" : "管道格上本来就没有 Pixel") +
                "，colors = " + (colors.Count > 0 ? colors[0].ToString() : "空（请在 Inspector 设置）") + "。");
        }

        /// <summary>某格是否属于「已高亮待删除」的那根管（含它的轨道格）。</summary>
        private bool IsCellOfPendingPipe(int col, int gridZ)
        {
            if (_deletePendingPipe == null)
                return false;
            return _pipeCells.TryGetValue(new Vector2Int(col, gridZ), out var pipe) && pipe == _deletePendingPipe;
        }

        /// <summary>
        /// 「删除管道」的单击：点到的管与已高亮的是同一根 → 真删；否则只把高亮切过去；
        /// 点到没有管的格子 → 取消高亮。删除**不回填 pixel**（与删除墙体同口径）。
        /// </summary>
        private void HandlePipeDeleteClick(int col, int gridZ)
        {
            if (!_pipeCells.TryGetValue(new Vector2Int(col, gridZ), out var pipe) || pipe == null)
            {
                _deletePendingPipe = null;
                return;
            }

            if (pipe != _deletePendingPipe)
            {
                _deletePendingPipe = pipe;   // 第一次点：只高亮
                return;
            }

            DeletePipe(pipe);
        }

        /// <summary>删掉一根管道：整根一起销毁，它覆盖的格留空（不填 pixel）。</summary>
        private void DeletePipe(PipeItem pipe)
        {
            if (pipe == null || _group == null)
                return;

            string pipeName = pipe.name;

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("删除管道");

            Undo.DestroyObjectImmediate(pipe.gameObject);

            _group.RebuildGrid();
            EditorUtility.SetDirty(_group);
            Undo.CollapseUndoOperations(undoGroup);

            _deletePendingPipe = null;
            RefreshSnapshot();
            SceneView.RepaintAll();
            Repaint();

            Debug.Log("[像素颜色画布] 已删除管道 " + pipeName + "（未回填 Pixel）。");
        }

        /// <summary>
        /// 「添加管道」松手：体检通过就直接建（已取消「创建管道」按钮），不通过就把原因写进
        /// <see cref="_lastStrokeError"/>。轨迹无论成败都清掉。单击（1 格）不算「想建管」，不报错。
        /// </summary>
        private void FinishPipeStroke()
        {
            var covered = CollectPipeStrokeCells();
            bool ok = TryValidatePipeStroke(covered, out string reason, out _);

            _lastStrokeError = ok || _pipeStroke.Count < 2 ? null : reason;

            if (ok)
                CreatePipeFromStroke();   // 内部会清轨迹 + 重建快照
            else
                _pipeStroke.Clear();

            Repaint();
        }

        /// <summary>该格是否被**指定的这一根**管道覆盖（管道格或它的轨道格）。越界一律算「未覆盖」→ 会被当成区域外缘。</summary>
        private bool IsCoveredBy(int col, int gridZ, PipeItem pipe)
        {
            return _pipeCells.TryGetValue(new Vector2Int(col, gridZ), out var other) && other == pipe;
        }

        /// <summary>
        /// 管道覆盖区的**粗描边**：四邻里凡是「不被**这一根**管道覆盖」的那一侧各画一条粗线，区域内部不画。
        /// 只看这根自己的归属 —— 两根管覆盖区相邻时，交界两侧各画各的线（各自都是完整描框，忽略叠加）。
        /// 画布上方 = row-1（更靠后），与格子渲染顺序一致。
        /// <paramref name="selected"/> = 它就是二级面板正在编辑的那根 → 用黄色描边（与「选中」的其它黄框同色）。
        /// </summary>
        private void DrawPipeOutline(Rect rect, int col, int gridZ, PipeItem pipe, bool selected)
        {
            const float thickness = 3f;
            var line = selected
                ? new Color(1f, 0.85f, 0.2f)
                : new Color(0.25f, 0.85f, 0.95f);

            if (!IsCoveredBy(col, gridZ - 1, pipe))
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), line);
            if (!IsCoveredBy(col, gridZ + 1, pipe))
                EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), line);
            if (!IsCoveredBy(col - 1, gridZ, pipe))
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), line);
            if (!IsCoveredBy(col + 1, gridZ, pipe))
                EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), line);
        }

        // ============================================================
        // 倍乘门：添加 / 删除（门格的像素会被清掉并存快照，删除时按快照还原）
        // ============================================================

        /// <summary>待创建门线的起点（门 / 墙 / 管共用的口径：Vector2 的 x = 列 col、y = 行 row）。</summary>
        private Vector2 GateStart => new Vector2(_gateAnchor.x, _gateAnchor.y);

        /// <summary>待创建门线的终点。</summary>
        private Vector2 GateEnd => new Vector2(_gateCurrent.x, _gateCurrent.y);

        /// <summary>待创建门线占据的格 —— 借 <see cref="GateItem.CollectCells"/> 的静态换算，与真正建出来的门同一口径。
        /// 还没拖出线段（起点 == 终点）时返回空集，免得预览停在上一笔上。</summary>
        private HashSet<Vector2Int> CollectGateStrokeCells()
        {
            var cells = new HashSet<Vector2Int>();
            if (_gateAnchor == _gateCurrent)
                return cells;

            GateItem.CollectCells(GateStart, GateEnd, cells);
            return cells;
        }

        /// <summary>
        /// 待创建门的体检：① 线段合法（轴对齐、≥2 格，交给 <see cref="GateItem.IsValidSegment"/>）；
        /// ② 不与已有门 / 墙体 / 管道重叠（已有障碍只能用各自的删除模式移除）。
        /// **不校验门格上有没有像素** —— 创建时会把它们清掉并记进快照，这正是门的正常用法。
        /// </summary>
        private bool TryValidateGateStroke(HashSet<Vector2Int> occupied, out string error, out int overlapCells)
        {
            overlapCells = 0;

            if (!GateItem.IsValidSegment(GateStart, GateEnd, out error))
                return false;

            if (occupied != null)
            {
                foreach (var cell in occupied)
                    if (_gateCells.ContainsKey(cell) || _wallCells.ContainsKey(cell) || _pipeCells.ContainsKey(cell))
                        overlapCells++;
            }

            if (overlapCells > 0)
            {
                error = "与已有门 / 墙体 / 管道重叠 " + overlapCells + " 格（已有障碍只能用各自的删除模式移除）。";
                return false;
            }

            return true;
        }

        /// <summary>「添加倍乘门」松手：体检通过就直接建；不通过只记原因。单击一格不算「想建门」，不报错。</summary>
        private void FinishGateStroke()
        {
            var occupied = CollectGateStrokeCells();
            bool ok = TryValidateGateStroke(occupied, out string reason, out _);

            bool isClick = _gateAnchor == _gateCurrent;
            _lastStrokeError = ok || isClick ? null : reason;

            if (ok)
                CreateGateFromStroke();   // 内部会重建快照

            Repaint();
        }

        /// <summary>
        /// 「创建倍乘门」：口径与 <c>GateCreator</c> 一致 —— 门格上的 Pixel 会被清掉并记进
        /// <see cref="GateItem.clearedPixels"/> 快照（供删除时还原），倍率固定 <see cref="GateMultiplier"/>。
        /// </summary>
        private void CreateGateFromStroke()
        {
            if (_group == null)
                return;

            var start = GateStart;
            var end = GateEnd;
            if (!GateItem.IsValidSegment(start, end, out string segErr))
            {
                Debug.LogWarning("[像素颜色画布] 无法创建倍乘门：" + segErr);
                return;
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("创建倍乘门");

            var gate = _group.SpawnGate(start, end, GateMultiplier);
            if (gate == null)
                return;   // SpawnGate 已经打过日志（gatePrefab 为空 / 预制体缺 GateItem）

            Undo.RegisterCreatedObjectUndo(gate.gameObject, "创建倍乘门");

            // 清掉门格上的 Pixel，并把它们记进快照（删除这道门时按快照还原）
            _group.RebuildGrid();   // 先让 gate.cells / 门格表就位
            gate.clearedPixels = new List<GateItem.ClearedPixel>();
            foreach (var cell in gate.cells)
            {
                var item = _group.GetItem(cell.x, cell.y);
                if (item == null)
                    continue;

                gate.clearedPixels.Add(new GateItem.ClearedPixel
                {
                    col = cell.x,
                    row = cell.y,
                    colorId = item.colorId,
                    isQuestion = item.isQuestion,
                });
                Undo.DestroyObjectImmediate(item.gameObject);
            }

            _group.RebuildGrid();       // 清格后重建：占用表 + 门格表 + 闭合区域 + 倍率图
            gate.BuildVisual(_group);
            EditorUtility.SetDirty(gate);
            EditorUtility.SetDirty(_group);
            Undo.CollapseUndoOperations(undoGroup);

            _gateCurrent = _gateAnchor;   // 清掉门线预览
            RefreshSnapshot();
            SceneView.RepaintAll();
            Repaint();

            Debug.Log("[像素颜色画布] 已创建倍乘门：(" + start.x + "," + start.y + ") → (" + end.x + "," + end.y +
                ")，" + gate.CellCount + " 格，倍率 x" + gate.multiplier + "，清除 Pixel " + gate.clearedPixels.Count +
                " 个。闭合区域请用「校验倍乘门」确认。");
        }

        /// <summary>某格是否属于「已高亮待删除」的那道门。</summary>
        private bool IsCellOfPendingGate(int col, int gridZ)
        {
            if (_deletePendingGate == null)
                return false;
            return _gateCells.TryGetValue(new Vector2Int(col, gridZ), out var gate) && gate == _deletePendingGate;
        }

        /// <summary>
        /// 「删除倍乘门」的单击：点到的门与已高亮的是同一道 → 真删；否则只把高亮切过去；
        /// 点到没有门的格子 → 取消高亮。
        /// </summary>
        private void HandleGateDeleteClick(int col, int gridZ)
        {
            if (!_gateCells.TryGetValue(new Vector2Int(col, gridZ), out var gate) || gate == null)
            {
                _deletePendingGate = null;
                return;
            }

            if (gate != _deletePendingGate)
            {
                _deletePendingGate = gate;   // 第一次点：只高亮
                return;
            }

            DeleteGate(gate);
        }

        /// <summary>
        /// 删掉一道门：**按 <see cref="GateItem.clearedPixels"/> 快照把门格上的 Pixel 还原回去**
        /// （与 Inspector 的「移除倍乘门并还原 Pixel」同口径；该格已有像素就不重复生成），再销毁门本体。
        /// 还原 + 销毁合成一个 Undo 步骤。
        /// </summary>
        private void DeleteGate(GateItem gate)
        {
            if (gate == null || _group == null)
                return;

            string gateName = gate.name;

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("删除倍乘门");

            _group.RebuildGrid();   // 取当前占用状态，避免还原出来的 Pixel 与已有像素重叠
            var config = ColorConfigLocator.Find();
            int restored = 0;
            if (gate.clearedPixels != null)
            {
                foreach (var snap in gate.clearedPixels)
                {
                    if (!_group.IsInRange(snap.col, snap.row))
                        continue;
                    if (_group.GetItem(snap.col, snap.row) != null)
                        continue;   // 该格已经有像素（后来手工填过）：不重复生成

                    var item = _group.SpawnPixel(snap.col, snap.row, snap.colorId, config, false, snap.isQuestion);
                    if (item == null)
                        continue;
                    Undo.RegisterCreatedObjectUndo(item.gameObject, "还原 Pixel");
                    restored++;
                }
            }

            Undo.DestroyObjectImmediate(gate.gameObject);

            _group.RebuildGrid();
            EditorUtility.SetDirty(_group);
            Undo.CollapseUndoOperations(undoGroup);

            _deletePendingGate = null;
            RefreshSnapshot();
            SceneView.RepaintAll();
            Repaint();

            Debug.Log("[像素颜色画布] 已删除倍乘门 " + gateName + "，还原 Pixel " + restored + " 个。");
        }

        // ============================================================
        // 箱子：添加 / 删除（矩形区域；箱内颜色走二级面板）
        // ============================================================

        /// <summary>待创建箱子区域的两个角归一化后的范围（列 / 行各从小到大）。</summary>
        private void NormalizeBoxRect(out int c0, out int r0, out int c1, out int r1)
        {
            c0 = Mathf.Min(_boxAnchor.x, _boxCurrent.x);
            c1 = Mathf.Max(_boxAnchor.x, _boxCurrent.x);
            r0 = Mathf.Min(_boxAnchor.y, _boxCurrent.y);
            r1 = Mathf.Max(_boxAnchor.y, _boxCurrent.y);
        }

        /// <summary>待创建箱子区域占据的格。还没拖出区域（两个角相同）时返回空集，免得预览停在上一笔上。</summary>
        private HashSet<Vector2Int> CollectBoxStrokeCells()
        {
            var cells = new HashSet<Vector2Int>();
            if (_boxAnchor == _boxCurrent)
                return cells;

            NormalizeBoxRect(out int c0, out int r0, out int c1, out int r1);
            for (int r = r0; r <= r1; r++)
                for (int c = c0; c <= c1; c++)
                    cells.Add(new Vector2Int(c, r));
            return cells;
        }

        /// <summary>
        /// 待创建箱子的体检：① 区域完全在网格内；② **必须是 2×2**（画布只放开这一种，
        /// 2×2 也正是 <see cref="BoxItem.ShouldUseWholePrefab"/> 有意义的形状）；
        /// ③ 区域内不能有别的障碍（墙 / 管 / 箱 / 门 / 冰 / 木箱 / 升降台）——空与像素格都可以，
        /// **像素会被箱子吃掉**（箱内颜色由 colorIds 提供，与 BoxCreator 的口径一致）。
        /// </summary>
        private bool TryValidateBoxStroke(HashSet<Vector2Int> region, out string error, out int blockedCells)
        {
            error = null;
            blockedCells = 0;

            NormalizeBoxRect(out int c0, out int r0, out int c1, out int r1);
            for (int r = r0; r <= r1; r++)
                for (int c = c0; c <= c1; c++)
                    if (!_group.IsInRange(c, r))
                    {
                        error = "区域越界：格 (" + c + ", " + r + ") 不在网格内。";
                        return false;
                    }

            int w = c1 - c0 + 1;
            int h = r1 - r0 + 1;
            if (w != 2 || h != 2)
            {
                error = "画布只允许创建 2×2 箱子（当前 " + w + "×" + h + "）。";
                return false;
            }

            if (region != null)
            {
                foreach (var cell in region)
                    if (!IsPaintable(Classify(cell.x, cell.y, out _)))
                        blockedCells++;
            }

            if (blockedCells > 0)
            {
                error = "区域内有 " + blockedCells + " 格已被其它障碍占用（墙 / 管 / 箱 / 门 / 冰 / 木箱 / 升降台）。";
                return false;
            }

            return true;
        }

        /// <summary>「添加箱子」松手：体检通过就直接建；不通过只记原因。单击一格不算「想建箱子」，不报错。</summary>
        private void FinishBoxStroke()
        {
            var region = CollectBoxStrokeCells();
            bool ok = TryValidateBoxStroke(region, out string reason, out _);

            bool isClick = _boxAnchor == _boxCurrent;
            _lastStrokeError = ok || isClick ? null : reason;

            if (ok)
                CreateBoxFromStroke();   // 内部会重建快照

            Repaint();
        }

        /// <summary>
        /// 「创建箱子」：口径与 <c>BoxCreator</c> 一致 —— 先清掉区域内的 Pixel（箱子区域应为空，
        /// 开箱后的 Pixel 由 <see cref="BoxItem.colorIds"/> 提供），再按「本体 + 相邻 4 方向」算出容量、
        /// 用当前笔刷色填满 colorIds 建箱。
        /// </summary>
        private void CreateBoxFromStroke()
        {
            if (_group == null)
                return;

            NormalizeBoxRect(out int c0, out int r0, out int c1, out int r1);
            var region = CollectBoxStrokeCells();

            int capacity = Mathf.Max(1, BoxItem.ComputeCapacity(_group, c0, r0, c1, r1));
            int fill = _brush >= 0 ? _brush : 0;
            var colorIds = new int[capacity];
            for (int i = 0; i < capacity; i++)
                colorIds[i] = fill;

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("创建箱子");

            // 箱子区域应为空：开箱后的像素由 colorIds 提供（与 BoxCreator.RemovePixelsInRegion 同口径）
            int removed = 0;
            foreach (var cell in region)
            {
                var item = _group.GetItem(cell.x, cell.y);
                if (item == null)
                    continue;
                Undo.DestroyObjectImmediate(item.gameObject);
                removed++;
            }

            var data = new LevelData.BoxData
            {
                colMin = c0,
                rowMin = r0,
                colMax = c1,
                rowMax = r1,
                capacity = capacity,
                colorIds = colorIds,
            };

            var box = _group.SpawnBox(data, _config);
            if (box == null)
            {
                Undo.CollapseUndoOperations(undoGroup);
                return;   // SpawnBox 已经打过日志（pixelPrefab 为空等）
            }

            Undo.RegisterCreatedObjectUndo(box.gameObject, "创建箱子");
            // 隐藏 Pixel 挂在 PixelGroup 下（不是箱子的子物体），撤销时得单独登记，否则会留一堆孤儿
            if (box.hiddenPixels != null)
            {
                foreach (var p in box.hiddenPixels)
                    if (p != null)
                        Undo.RegisterCreatedObjectUndo(p.gameObject, "创建箱子");
            }

            _group.RebuildGrid();
            EditorUtility.SetDirty(box);
            EditorUtility.SetDirty(_group);
            Undo.CollapseUndoOperations(undoGroup);

            _boxCurrent = _boxAnchor;   // 清掉区域预览
            RefreshSnapshot();
            SceneView.RepaintAll();
            Repaint();

            Debug.Log("[像素颜色画布] 已创建箱子：(" + c0 + ", " + r0 + ") ~ (" + c1 + ", " + r1 +
                ")，容量 " + capacity + "（colorIds 全填 " + fill + "），吃掉区域 Pixel " + removed +
                " 个。箱内颜色可点它开二级面板改。");
        }

        /// <summary>
        /// 选中的箱子（正在改箱内颜色的那个）在画布上的高亮：淡黄打底 + 沿**箱子矩形外框**画 3px 黄线
        /// （只画落在矩形边界上的那几条边）。用的是与选中管道同一套「选中」颜色。
        /// </summary>
        private static void DrawSelectedBoxHighlight(Rect rect, int col, int gridZ, BoxItem box)
        {
            const float thickness = 3f;
            var line = new Color(1f, 0.85f, 0.2f);

            EditorGUI.DrawRect(rect, new Color(1f, 0.85f, 0.2f, 0.28f));

            if (col == box.colMin)
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), line);
            if (col == box.colMax)
                EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), line);
            if (gridZ == box.rowMin)
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), line);
            if (gridZ == box.rowMax)
                EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), line);
        }

        /// <summary>某格是否属于「已高亮待删除」的那个箱子。</summary>
        private bool IsCellOfPendingBox(int col, int gridZ)
        {
            if (_deletePendingBox == null)
                return false;
            return _boxCells.TryGetValue(new Vector2Int(col, gridZ), out var box) && box == _deletePendingBox;
        }

        /// <summary>
        /// 「删除箱子」的单击：点到的箱子与已高亮的是同一个 → 真删；否则只把高亮切过去；
        /// 点到没有箱子的格子 → 取消高亮。
        /// </summary>
        private void HandleBoxDeleteClick(int col, int gridZ)
        {
            if (!_boxCells.TryGetValue(new Vector2Int(col, gridZ), out var box) || box == null)
            {
                _deletePendingBox = null;
                return;
            }

            if (box != _deletePendingBox)
            {
                _deletePendingBox = box;   // 第一次点：只高亮
                return;
            }

            DeleteBox(box);
        }

        /// <summary>
        /// 删掉一个箱子：连同它的**隐藏 Pixel** 一起销毁（隐藏 Pixel 挂在 PixelGroup 下、不是箱子的子物体，
        /// 不显式删就会留下孤儿），并收起它的颜色面板。**不回填**创建时吃掉的区域 Pixel（与删除墙体 / 管道同口径）。
        /// </summary>
        private void DeleteBox(BoxItem box)
        {
            if (box == null || _group == null)
                return;

            string boxName = box.name;

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("删除箱子");

            int hidden = 0;
            if (box.hiddenPixels != null)
            {
                foreach (var p in box.hiddenPixels)
                {
                    if (p == null)
                        continue;
                    Undo.DestroyObjectImmediate(p.gameObject);
                    hidden++;
                }
                box.hiddenPixels.Clear();
            }

            Undo.DestroyObjectImmediate(box.gameObject);

            _group.RebuildGrid();
            EditorUtility.SetDirty(_group);
            Undo.CollapseUndoOperations(undoGroup);

            _deletePendingBox = null;
            if (_boxColorTarget == box)
                _boxColorTarget = null;
            RefreshSnapshot();
            SceneView.RepaintAll();
            Repaint();

            Debug.Log("[像素颜色画布] 已删除箱子 " + boxName + "（连同 " + hidden +
                " 个隐藏 Pixel；不回填区域 Pixel）。");
        }

        // ============================================================
        // 木箱：添加 / 删除（矩形区域；它**盖住**像素，所以画布上照样显示原颜色、也照样能涂）
        // ============================================================

        /// <summary>待创建木箱区域的两个角归一化后的范围（列 / 行各从小到大）。</summary>
        private void NormalizeCrateRect(out int c0, out int r0, out int c1, out int r1)
        {
            c0 = Mathf.Min(_crateAnchor.x, _crateCurrent.x);
            c1 = Mathf.Max(_crateAnchor.x, _crateCurrent.x);
            r0 = Mathf.Min(_crateAnchor.y, _crateCurrent.y);
            r1 = Mathf.Max(_crateAnchor.y, _crateCurrent.y);
        }

        /// <summary>待创建木箱区域占据的格。还没拖出区域时返回空集，免得预览停在上一笔上。</summary>
        private HashSet<Vector2Int> CollectCrateStrokeCells()
        {
            var cells = new HashSet<Vector2Int>();
            if (_crateAnchor == _crateCurrent)
                return cells;

            NormalizeCrateRect(out int c0, out int r0, out int c1, out int r1);
            for (int r = r0; r <= r1; r++)
                for (int c = c0; c <= c1; c++)
                    cells.Add(new Vector2Int(c, r));
            return cells;
        }

        /// <summary>
        /// 待创建木箱的体检：① 区域完全在网格内；② **长宽至少 2 格**（<see cref="CrateItem.IsValidSize"/> 的口径）；
        /// ③ 区域内不能有别的障碍（墙 / 管 / 箱 / 门 / 冰 / 升降台 / 另一只木箱）。
        /// **像素格是允许的** —— 木箱本来就是用来盖像素的（这点与箱子相反：箱子会把像素吃掉）。
        /// </summary>
        private bool TryValidateCrateStroke(HashSet<Vector2Int> region, out string error, out int blockedCells)
        {
            error = null;
            blockedCells = 0;

            NormalizeCrateRect(out int c0, out int r0, out int c1, out int r1);
            for (int r = r0; r <= r1; r++)
                for (int c = c0; c <= c1; c++)
                    if (!_group.IsInRange(c, r))
                    {
                        error = "区域越界：格 (" + c + ", " + r + ") 不在网格内。";
                        return false;
                    }

            int w = c1 - c0 + 1;
            int h = r1 - r0 + 1;
            if (w < 2 || h < 2)
            {
                error = "木箱长宽至少 2 格（当前 " + w + "×" + h + "）。";
                return false;
            }

            if (region != null)
            {
                foreach (var cell in region)
                    if (!IsPaintable(Classify(cell.x, cell.y, out _)))
                        blockedCells++;
            }

            if (blockedCells > 0)
            {
                error = "区域内有 " + blockedCells + " 格已被其它障碍占用（墙 / 管 / 箱 / 门 / 冰 / 升降台 / 木箱）。";
                return false;
            }

            return true;
        }

        /// <summary>「添加木箱」松手：体检通过就直接建；不通过只记原因。单击一格不算「想建木箱」，不报错。</summary>
        private void FinishCrateStroke()
        {
            var region = CollectCrateStrokeCells();
            bool ok = TryValidateCrateStroke(region, out string reason, out _);

            bool isClick = _crateAnchor == _crateCurrent;
            _lastStrokeError = ok || isClick ? null : reason;

            if (ok)
                CreateCrateFromStroke();

            Repaint();
        }

        /// <summary>
        /// 「创建木箱」：走 <see cref="PixelGroup.SpawnCrate"/>（与关卡 JSON 导入同一条路径）。
        /// **不动区域内的像素** —— 木箱只是盖住它们（关渲染器 + 不可点），
        /// 所以画布上照常显示原颜色、也照常能以原颜色编辑，辨认木箱全靠那道棕色描边。
        /// 「拆箱所需移出次数」固定 <see cref="CrateDestroyAfterMoves"/>（要改去木箱的 Inspector）。
        /// </summary>
        private void CreateCrateFromStroke()
        {
            if (_group == null)
                return;

            NormalizeCrateRect(out int c0, out int r0, out int c1, out int r1);

            var data = new LevelData.CrateData
            {
                colMin = c0,
                rowMin = r0,
                colMax = c1,
                rowMax = r1,
                destroyAfterMoves = CrateDestroyAfterMoves,
            };

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("创建木箱");

            var crate = _group.SpawnCrate(data);
            if (crate == null)
            {
                Undo.CollapseUndoOperations(undoGroup);
                return;
            }

            // 视觉与封条都是它的子物体，登记根物体即可整只撤销
            Undo.RegisterCreatedObjectUndo(crate.gameObject, "创建木箱");

            _group.RebuildGrid();
            EditorUtility.SetDirty(crate);
            EditorUtility.SetDirty(_group);
            Undo.CollapseUndoOperations(undoGroup);

            _crateCurrent = _crateAnchor;   // 清掉区域预览
            RefreshSnapshot();
            SceneView.RepaintAll();
            Repaint();

            Debug.Log("[像素颜色画布] 已创建木箱：(" + c0 + ", " + r0 + ") ~ (" + c1 + ", " + r1 + ")，" +
                (c1 - c0 + 1) + "×" + (r1 - r0 + 1) + " 格，拆箱需移出 " + CrateDestroyAfterMoves +
                " 次（区域内像素原样保留，只是被盖住）。");
        }

        /// <summary>某格是否属于「已高亮待删除」的那只木箱。</summary>
        private bool IsCellOfPendingCrate(int col, int gridZ)
        {
            if (_deletePendingCrate == null)
                return false;
            return _crateCells.TryGetValue(new Vector2Int(col, gridZ), out var crate) && crate == _deletePendingCrate;
        }

        /// <summary>「删除木箱」的单击：点到的与已高亮的是同一只 → 真删；否则只切高亮；点空格取消高亮。</summary>
        private void HandleCrateDeleteClick(int col, int gridZ)
        {
            if (!_crateCells.TryGetValue(new Vector2Int(col, gridZ), out var crate) || crate == null)
            {
                _deletePendingCrate = null;
                return;
            }

            if (crate != _deletePendingCrate)
            {
                _deletePendingCrate = crate;   // 第一次点：只高亮
                return;
            }

            DeleteCrate(crate);
        }

        /// <summary>删掉一只木箱：销毁木箱本体（视觉 / 封条都是子物体）—— 底下的像素**原样保留**，不需要回填。</summary>
        private void DeleteCrate(CrateItem crate)
        {
            if (crate == null || _group == null)
                return;

            string crateName = crate.name;

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("删除木箱");

            Undo.DestroyObjectImmediate(crate.gameObject);

            _group.RebuildGrid();
            EditorUtility.SetDirty(_group);
            Undo.CollapseUndoOperations(undoGroup);

            _deletePendingCrate = null;
            RefreshSnapshot();
            SceneView.RepaintAll();
            Repaint();

            Debug.Log("[像素颜色画布] 已删除木箱 " + crateName + "（底下的 Pixel 原样保留）。");
        }

        /// <summary>该格是否被**指定的这一只**木箱盖住（越界一律算「没盖住」→ 会被当成区域外缘画线）。</summary>
        private bool IsCoveredByCrate(int col, int gridZ, CrateItem crate)
        {
            return _crateCells.TryGetValue(new Vector2Int(col, gridZ), out var other) && other == crate;
        }

        /// <summary>
        /// 木箱覆盖区的**双层粗描边**：四邻里凡是不被**这一只**木箱盖住的那一侧各画两条 3px 线 ——
        /// 外面一条棕色、紧挨着里面一条黄色（「再套一层」）。只看这只自己的归属 ——
        /// 两只木箱挨着时，交界两侧各画各的（各自完整描框）。
        /// 木箱格照常显示底下像素的颜色与编号，**辨认木箱全靠这道描边**（不再画「木」字标记）。
        /// </summary>
        private void DrawCrateOutline(Rect rect, int col, int gridZ, CrateItem crate)
        {
            const float t = 3f;
            var outer = new Color(0.55f, 0.33f, 0.15f);   // 外圈：棕
            var inner = new Color(1f, 0.85f, 0.2f);       // 内圈：黄（与「选中」同一套黄）

            // 上
            if (!IsCoveredByCrate(col, gridZ - 1, crate))
            {
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, t), outer);
                EditorGUI.DrawRect(new Rect(rect.x, rect.y + t, rect.width, t), inner);
            }
            // 下
            if (!IsCoveredByCrate(col, gridZ + 1, crate))
            {
                EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - t, rect.width, t), outer);
                EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 2f * t, rect.width, t), inner);
            }
            // 左
            if (!IsCoveredByCrate(col - 1, gridZ, crate))
            {
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, t, rect.height), outer);
                EditorGUI.DrawRect(new Rect(rect.x + t, rect.y, t, rect.height), inner);
            }
            // 右
            if (!IsCoveredByCrate(col + 1, gridZ, crate))
            {
                EditorGUI.DrawRect(new Rect(rect.xMax - t, rect.y, t, rect.height), outer);
                EditorGUI.DrawRect(new Rect(rect.xMax - 2f * t, rect.y, t, rect.height), inner);
            }
        }

        // ============================================================
        // 冰：添加 / 删除（矩形区域）+ 编辑冰模式（次数 / 暴露扣减 / 数字偏移与缩放）
        // ============================================================

        /// <summary>待创建冰组区域的两个角归一化后的范围。</summary>
        private void NormalizeIceRect(out int c0, out int r0, out int c1, out int r1)
        {
            c0 = Mathf.Min(_iceAnchor.x, _iceCurrent.x);
            c1 = Mathf.Max(_iceAnchor.x, _iceCurrent.x);
            r0 = Mathf.Min(_iceAnchor.y, _iceCurrent.y);
            r1 = Mathf.Max(_iceAnchor.y, _iceCurrent.y);
        }

        /// <summary>「添加冰」自由涂抹：把划过的一格记进笔画（连续重复的同一格只记一次 —— 手在同一个格子里会抖）。</summary>
        private void AppendIceCell(int col, int gridZ)
        {
            var cell = new Vector2Int(col, gridZ);
            if (_iceStroke.Count > 0 && _iceStroke[_iceStroke.Count - 1] == cell)
                return;
            _iceStroke.Add(cell);
        }

        /// <summary>
        /// 待创建冰组占据的格：勾了**自由涂抹**就是划过的那些格（形状任意、不强求连通）；
        /// 否则是矩形区域的所有格。还没划/拖出东西时返回空集，免得预览停在上一笔上。
        /// </summary>
        private HashSet<Vector2Int> CollectIceStrokeCells()
        {
            var cells = new HashSet<Vector2Int>();

            if (_iceFreeform)
            {
                foreach (var cell in _iceStroke)
                    cells.Add(cell);
                return cells;
            }

            if (_iceAnchor == _iceCurrent)
                return cells;

            NormalizeIceRect(out int c0, out int r0, out int c1, out int r1);
            for (int r = r0; r <= r1; r++)
                for (int c = c0; c <= c1; c++)
                    cells.Add(new Vector2Int(c, r));
            return cells;
        }

        /// <summary>
        /// 待创建冰组的体检：① 区域完全在网格内；② 区域内不能有别的障碍
        /// （墙 / 管 / 箱 / 门 / 木箱 / 升降台 / 另一组冰）。
        /// **像素格与空格都允许** —— 冰下面本来就可以有像素（那些像素只是「视为不暴露」）。
        /// 冰的形状本身没有最小尺寸限制（1 格也算一组，与 IceItem 的口径一致）。
        /// </summary>
        private bool TryValidateIceStroke(HashSet<Vector2Int> region, out string error, out int blockedCells)
        {
            error = null;
            blockedCells = 0;

            NormalizeIceRect(out int c0, out int r0, out int c1, out int r1);
            for (int r = r0; r <= r1; r++)
                for (int c = c0; c <= c1; c++)
                    if (!_group.IsInRange(c, r))
                    {
                        error = "区域越界：格 (" + c + ", " + r + ") 不在网格内。";
                        return false;
                    }

            if (region != null)
            {
                foreach (var cell in region)
                    if (!IsPaintable(Classify(cell.x, cell.y, out _)))
                        blockedCells++;
            }

            if (blockedCells > 0)
            {
                error = "区域内有 " + blockedCells + " 格已被其它障碍占用（墙 / 管 / 箱 / 门 / 木箱 / 升降台 / 冰）。";
                return false;
            }

            return true;
        }

        /// <summary>「添加冰」松手：体检通过就直接建；不通过只记原因。单击（没有真正划/拖出范围）不算「想建冰」，不报错也不建。</summary>
        private void FinishIceStroke()
        {
            var region = CollectIceStrokeCells();
            bool ok = TryValidateIceStroke(region, out string reason, out _);

            bool isClick = _iceFreeform ? _iceStroke.Count < 2 : _iceAnchor == _iceCurrent;
            _lastStrokeError = ok || isClick ? null : reason;

            if (ok && !isClick)
                CreateIceFromStroke();

            Repaint();
        }

        /// <summary>
        /// 「创建冰组」：走 <see cref="PixelGroup.SpawnIce"/>（与关卡 JSON 导入同一条路径）。
        /// 格集合取自 <see cref="CollectIceStrokeCells"/>（自由涂抹 = 划过的格；否则 = 矩形区域），
        /// 按「行升序、同行列升序」排一下再写，导出的 JSON 才稳定、不会因 HashSet 顺序每次不同。
        /// 次数初值固定 <see cref="DefaultIceFreezeCount"/>；要改次数 / 暴露扣减 / 数字偏移与缩放，用「编辑冰」模式。
        /// **不动格上的像素** —— 冰只是让它们「视为不暴露」。
        /// </summary>
        private void CreateIceFromStroke()
        {
            if (_group == null)
                return;

            var region = CollectIceStrokeCells();
            if (region.Count == 0)
                return;

            var ordered = new List<Vector2Int>(region);
            ordered.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));

            var cells = new List<Vector2>(ordered.Count);
            foreach (var c in ordered)
                cells.Add(new Vector2(c.x, c.y));

            var data = new LevelData.IceGroupData
            {
                cells = cells.ToArray(),
                count = DefaultIceFreezeCount,
                meltWhenExposed = false,
                countOffset = IceItem.DefaultCountOffset,
                fontScale = 1f,
            };

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("创建冰组");

            var ice = _group.SpawnIce(data);
            if (ice == null)
            {
                Undo.CollapseUndoOperations(undoGroup);
                return;
            }

            // 冰面 / 计数数字都是它的子物体，登记根物体即可整组撤销
            Undo.RegisterCreatedObjectUndo(ice.gameObject, "创建冰组");

            _group.RebuildGrid();
            EditorUtility.SetDirty(ice);
            EditorUtility.SetDirty(_group);
            Undo.CollapseUndoOperations(undoGroup);

            _iceCurrent = _iceAnchor;   // 清掉预览
            _iceStroke.Clear();
            RefreshSnapshot();
            SceneView.RepaintAll();
            Repaint();

            Debug.Log("[像素颜色画布] 已创建冰组：" + ordered.Count + " 格（" +
                (_iceFreeform ? "自由涂抹，形状任意" : "矩形填充") + "），次数 " + DefaultIceFreezeCount +
                "（要改次数 / 偏移 / 缩放，切「编辑冰」模式点它）。");
        }

        /// <summary>某格是否属于「已高亮待删除」的那组冰。</summary>
        private bool IsCellOfPendingIce(int col, int gridZ)
        {
            if (_deletePendingIce == null)
                return false;
            return _iceCells.TryGetValue(new Vector2Int(col, gridZ), out var ice) && ice == _deletePendingIce;
        }

        /// <summary>「删除冰」的单击：点到的与已高亮的是同一组 → 真删；否则只切高亮；点空格取消高亮。</summary>
        private void HandleIceDeleteClick(int col, int gridZ)
        {
            if (!_iceCells.TryGetValue(new Vector2Int(col, gridZ), out var ice) || ice == null)
            {
                _deletePendingIce = null;
                return;
            }

            if (ice != _deletePendingIce)
            {
                _deletePendingIce = ice;   // 第一次点：只高亮
                return;
            }

            DeleteIce(ice);
        }

        /// <summary>删掉一组冰：销毁冰组本体（冰面 / 计数数字都是子物体）—— 格上的像素**原样保留**（冰不清除像素）。</summary>
        private void DeleteIce(IceItem ice)
        {
            if (ice == null || _group == null)
                return;

            string iceName = ice.name;

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("删除冰组");

            Undo.DestroyObjectImmediate(ice.gameObject);

            _group.RebuildGrid();
            EditorUtility.SetDirty(_group);
            Undo.CollapseUndoOperations(undoGroup);

            _deletePendingIce = null;
            if (_iceEditTarget == ice)
            {
                _iceEditTarget = null;
                _iceDirty = false;
            }
            RefreshSnapshot();
            SceneView.RepaintAll();
            Repaint();

            Debug.Log("[像素颜色画布] 已删除冰组 " + iceName + "（格上的 Pixel 原样保留）。");
        }

        /// <summary>该格是否属于**指定的这一组**冰（越界一律算「不属于」→ 会被当成区域外缘画线）。</summary>
        private bool IsCoveredByIce(int col, int gridZ, IceItem ice)
        {
            return _iceCells.TryGetValue(new Vector2Int(col, gridZ), out var other) && other == ice;
        }

        /// <summary>
        /// 冰组覆盖区的**双层粗描边**：四邻里凡是不被**这一组**冰覆盖的那一侧各画两条 3px 线 ——
        /// 外面一条深蓝、紧挨着里面一条浅蓝（与木箱那套双层描边同形，颜色换成冰的深浅蓝）。
        /// 冰格照常显示底下像素的颜色与编号，**辨认冰全靠这道描边**（不再画「冰」字标记）。
        /// </summary>
        private void DrawIceOutline(Rect rect, int col, int gridZ, IceItem ice)
        {
            const float t = 3f;
            var outer = new Color(0.1f, 0.25f, 0.6f);    // 外圈：深蓝
            var inner = new Color(0.55f, 0.8f, 1f);      // 内圈：浅蓝

            // 上
            if (!IsCoveredByIce(col, gridZ - 1, ice))
            {
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, t), outer);
                EditorGUI.DrawRect(new Rect(rect.x, rect.y + t, rect.width, t), inner);
            }
            // 下
            if (!IsCoveredByIce(col, gridZ + 1, ice))
            {
                EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - t, rect.width, t), outer);
                EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 2f * t, rect.width, t), inner);
            }
            // 左
            if (!IsCoveredByIce(col - 1, gridZ, ice))
            {
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, t, rect.height), outer);
                EditorGUI.DrawRect(new Rect(rect.x + t, rect.y, t, rect.height), inner);
            }
            // 右
            if (!IsCoveredByIce(col + 1, gridZ, ice))
            {
                EditorGUI.DrawRect(new Rect(rect.xMax - t, rect.y, t, rect.height), outer);
                EditorGUI.DrawRect(new Rect(rect.xMax - 2f * t, rect.y, t, rect.height), inner);
            }
        }

        // ============================================================
        // 编辑冰：选中一组 → 改四个字段（实时预览）→「保存编辑」才落库
        // ============================================================

        /// <summary>
        /// 编辑冰模式单击：点冰格选中那组来编辑；点空格取消选中。
        /// 换组 / 取消前都会先问一句未保存的改动（见 <see cref="ConfirmLeaveIceEdit"/>）。
        /// </summary>
        private void HandleIceEditClick(int col, int gridZ)
        {
            if (!_iceCells.TryGetValue(new Vector2Int(col, gridZ), out var ice) || ice == null)
            {
                if (_iceEditTarget != null)
                {
                    ConfirmLeaveIceEdit();
                    _iceEditTarget = null;
                    _iceDirty = false;
                    Repaint();
                }
                return;
            }

            if (ice == _iceEditTarget)
                return;   // 点的还是同一组：什么都不做（免得每次点都弹窗）

            if (_iceEditTarget != null)
                ConfirmLeaveIceEdit();   // 换组前先处理上一组的未保存改动

            SelectIceForEdit(ice);
        }

        /// <summary>选中一组冰进入编辑：把**编辑前**的值记成基线（此时不记 Undo —— 保存时才落成一步）。</summary>
        private void SelectIceForEdit(IceItem ice)
        {
            _iceEditTarget = ice;
            _iceBefore = IceEditValues.Capture(ice);
            _iceEditing = _iceBefore;
            _iceDirty = false;
            _iceVisualDirty = false;

            Repaint();
            SceneView.RepaintAll();
        }

        /// <summary>编辑冰面板：四个字段 + 保存 / 还原 / 取消选中。改动实时预览，落库要等「保存编辑」。</summary>
        private void DrawIceEditPanel()
        {
            if (_iceEditTarget == null)
                return;

            var ice = _iceEditTarget;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("编辑冰组 " + ice.name + "（" + ice.CellCount + " 格）" +
                (_iceDirty ? "　·　**有未保存的改动**" : ""), EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(!_iceDirty))
            {
                if (GUILayout.Button("保存编辑", GUILayout.Width(70f)))
                    SaveIceEdit();
            }
            using (new EditorGUI.DisabledScope(!_iceDirty))
            {
                if (GUILayout.Button("还原", GUILayout.Width(44f)))
                    RevertIceEdit();
            }
            if (GUILayout.Button("取消选中", GUILayout.Width(64f)))
            {
                ConfirmLeaveIceEdit();
                _iceEditTarget = null;
                _iceDirty = false;
            }
            EditorGUILayout.EndHorizontal();

            var v = _iceEditing;
            EditorGUI.BeginChangeCheck();

            v.freezeCount = Mathf.Max(1, EditorGUILayout.IntField("冰冻次数", v.freezeCount));
            v.meltOnlyWhenExposed = EditorGUILayout.Toggle("仅暴露后才扣减", v.meltOnlyWhenExposed);

            // 偏移只暴露 X / Z（Y 保持原值不动）：这类俯视摆位基本只调水平两轴
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("计数数字偏移", GUILayout.Width(EditorGUIUtility.labelWidth));
            v.countOffset.x = EditorGUILayout.FloatField(v.countOffset.x, GUILayout.Width(60f));
            v.countOffset.z = EditorGUILayout.FloatField(v.countOffset.z, GUILayout.Width(60f));
            GUILayout.Label("(X / Z；Y 保持 " + _iceBefore.countOffset.y.ToString("0.##") + " 不变)", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            v.countFontScale = EditorGUILayout.FloatField("计数数字缩放", v.countFontScale);

            if (EditorGUI.EndChangeCheck())
            {
                _iceEditing = v;
                _iceDirty = !_iceEditing.SameAs(_iceBefore);
                _iceVisualDirty = true;   // 本帧末尾统一重建一次可见表现（别每个控件都重建一次 Mesh）
            }

            EditorGUILayout.LabelField(
                _iceDirty
                    ? "改动已在场景里实时预览；点「保存编辑」才落库（保存后 Ctrl+Z 可一键回到编辑前）。"
                    : "改任意字段即时预览；点「保存编辑」落库。切模式 / 换冰组会问你要不要保存。",
                EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();

            if (_iceVisualDirty)
                PreviewIceEdit();
        }

        /// <summary>
        /// 实时预览：把编辑值写到冰组上并重建可见表现（**不记 Undo** —— 保存时才落库）。
        /// 重建序列与 <c>IceItemEditor</c> 的「重建显示」一致：先 RebuildGrid（冻结掩码 / 计数显示跟着走），
        /// 再 BuildVisual（冰面 + 数字的位置与缩放）。
        /// </summary>
        private void PreviewIceEdit()
        {
            _iceVisualDirty = false;

            var ice = _iceEditTarget;
            if (ice == null || _group == null)
                return;

            _iceEditing.ApplyTo(ice);
            ice.RefreshCells();
            _group.RebuildGrid();
            ice.BuildVisual(_group);
            SceneView.RepaintAll();
            Repaint();
        }

        /// <summary>
        /// 「保存编辑」：把这一轮编辑落成**一个 Undo 步骤**（撤销即回到编辑前）。
        ///
        /// 关键顺序：① 先把对象写回**编辑前**的值 → ② `Undo.RecordObject`（此刻记下的就是编辑前）
        /// → ③ 再写编辑值。直接在「已经预览成编辑值」的状态上 RecordObject，撤销会变成空操作
        /// （画布上那几个颜色面板踩过同一个坑）。
        /// </summary>
        private void SaveIceEdit()
        {
            var ice = _iceEditTarget;
            if (ice == null || _group == null || !_iceDirty)
                return;

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("编辑冰组");

            _iceBefore.ApplyTo(ice);             // ① 回到编辑前
            Undo.RecordObject(ice, "编辑冰组");    // ② 记下编辑前的状态
            _iceEditing.ApplyTo(ice);            // ③ 写入编辑值

            ice.RefreshCells();
            _group.RebuildGrid();
            ice.BuildVisual(_group);
            EditorUtility.SetDirty(ice);
            EditorUtility.SetDirty(_group);
            Undo.CollapseUndoOperations(undoGroup);

            _iceBefore = _iceEditing;   // 新基线
            _iceDirty = false;
            SceneView.RepaintAll();
            Repaint();

            Debug.Log("[像素颜色画布] 已保存冰组 " + ice.name + " 的编辑（Ctrl+Z 可一键回到编辑前）。");
        }

        /// <summary>「还原」：放弃未保存的改动，回到编辑前状态（此时还没记 Undo，直接写回即可）。</summary>
        private void RevertIceEdit()
        {
            var ice = _iceEditTarget;
            if (ice == null)
                return;

            _iceBefore.ApplyTo(ice);
            _iceEditing = _iceBefore;
            _iceDirty = false;

            if (_group != null)
            {
                ice.RefreshCells();
                _group.RebuildGrid();
                ice.BuildVisual(_group);
            }
            SceneView.RepaintAll();
            Repaint();

            Debug.Log("[像素颜色画布] 已放弃未保存的冰组编辑，回到编辑前状态。");
        }

        /// <summary>
        /// 要离开当前编辑（切模式 / 换冰组 / 取消选中）时问一句：有未保存改动就二选一 ——
        /// 「保存并离开」= 落库（之后 Ctrl+Z 可回到编辑前）；「放弃并离开」= 回到编辑前。
        /// 两种都是离开，不提供「留下」（与口径一致：不保存就回到编辑前状态）。
        /// </summary>
        private void ConfirmLeaveIceEdit()
        {
            if (_iceEditTarget == null || !_iceDirty)
                return;

            bool save = EditorUtility.DisplayDialog("冰组编辑未保存",
                "冰组 " + _iceEditTarget.name + " 有未保存的编辑。\n\n" +
                "「保存并离开」= 落库（之后 Ctrl+Z 可一键回到编辑前）\n" +
                "「放弃并离开」= 回到编辑前的状态",
                "保存并离开", "放弃并离开");

            if (save)
                SaveIceEdit();
            else
                RevertIceEdit();
        }

        /// <summary>关窗 / 进出 Play 时静默回退未保存的冰组编辑（这些时机不适合弹窗）。</summary>
        private void RevertIceEditSilently()
        {
            if (_iceEditTarget == null)
                return;

            _iceBefore.ApplyTo(_iceEditTarget);
            _iceEditing = _iceBefore;
            _iceDirty = false;

            if (_group != null)
            {
                _iceEditTarget.RefreshCells();
                _group.RebuildGrid();
                _iceEditTarget.BuildVisual(_group);
            }
            SceneView.RepaintAll();
        }

        // ============================================================
        // 箱子「内部颜色」二级面板（涂颜色模式下点箱子区域打开）
        // ============================================================

        /// <summary>
        /// 涂颜色模式下点到**箱子区域内的格**：打开（或切换到）它的「箱内颜色」二级面板，而不是涂色
        /// （箱子区域本来也是不可涂的障碍格）。返回 true = 已打开 / 已切换。
        /// 打开时若没有可用颜色（选了橡皮 / 从没选过），自动落到 0 号色，面板一打开就能按 +。
        /// </summary>
        private bool TryOpenBoxColorPanel(int col, int gridZ)
        {
            if (!_boxCells.TryGetValue(new Vector2Int(col, gridZ), out var box) || box == null)
                return false;

            _boxColorTarget = box;
            if (_brush < 0 && _palette.Length > 0)
                _brush = 0;

            Repaint();
            return true;
        }

        /// <summary>箱子「内部颜色」二级面板：只列出箱内像素的颜色（点一下 = 用当前选中色替换），最右 − / +。</summary>
        private void DrawBoxColorPanel()
        {
            if (_boxColorTarget == null)
                return;

            bool close = false;
            var box = _boxColorTarget;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("箱子 " + box.name + " 的内部颜色（" + (box.colorIds != null ? box.colorIds.Length : 0) +
                " 个 = 容量；选色用上面的调色板）", EditorStyles.boldLabel);
            if (GUILayout.Button("关闭", GUILayout.Width(52f)))
                close = true;
            EditorGUILayout.EndHorizontal();

            DrawBoxColorRow(box);

            EditorGUILayout.EndVertical();

            if (close)
            {
                _boxColorTarget = null;
                Repaint();
            }
        }

        /// <summary>
        /// 箱子面板的颜色行：箱内颜色顺次排列（点一下 = 用当前选中色替换），下面 − / + / 清空。
        /// **清空** = colorIds 变空（容量 0）→ 箱子成为「无内容」：开箱时直接清障碍消失（运行时支持这种箱子）。
        /// </summary>
        private void DrawBoxColorRow(BoxItem box)
        {
            var working = new List<int>(box.colorIds ?? new int[0]);   // 工作副本
            DrawColorListBlock(working, () => WriteBoxColors(box, working.ToArray(), "箱内颜色"));
        }

        /// <summary>
        /// 把箱内颜色写回：<c>colorIds</c> 是权威数据（容量 = 它的数量），导出 JSON、容器规划、校验读的都是它。
        ///
        /// **不主动同步隐藏 Pixel**：那是 <see cref="BoxItem.BuildVisual"/> 在运行时 / 导入关卡时按 colorIds
        /// 生成的（编辑器里手工摆的箱子本来就没有隐藏 Pixel）。改了颜色后场景里旧的那批会过时，但没有任何
        /// 编辑器工具读它们（导出读 colorIds），下次进游戏也会由 SpawnBox 重新生成，所以无害。
        /// </summary>
        private void WriteBoxColors(BoxItem box, int[] colorIds, string op)
        {
            Undo.RecordObject(box, op);
            box.colorIds = colorIds;
            box.capacity = colorIds.Length;
            box.UpdateCountText();   // 箱子头顶那个数字就是容量，改了数量要当场刷新
            EditorUtility.SetDirty(box);

            Repaint();
            SceneView.RepaintAll();
        }

        // ============================================================
        // 管道「波次颜色」二级界面（涂颜色模式下点管道格打开）
        // ============================================================

        /// <summary>
        /// 涂颜色模式下点到**管道格**（那格没有像素、本来也涂不了色）：打开（或切换到）它的波次颜色二级面板，
        /// 而不是开笔涂色。返回 true = 已打开 / 已切换（调用方这次不要开笔）。
        /// 只认管道格本身、不认轨道格 —— 轨道格上有正常像素，点了应该照常涂色。
        ///
        /// 打开时若没有可用颜色（选了橡皮 / 从没选过），自动落到 0 号色：面板一打开就能直接按 +，
        /// 不必先去上面点一下色块。
        /// </summary>
        private bool TryOpenPipeColorPanel(int col, int gridZ)
        {
            var cell = new Vector2Int(col, gridZ);
            if (!_pipeCells.TryGetValue(cell, out var pipe) || pipe == null)
                return false;
            if (PipeItem.GetPipeCell(pipe.points) != cell)
                return false;

            _pipeColorTarget = pipe;
            if (_brush < 0 && _palette.Length > 0)
                _brush = 0;

            Repaint();
            return true;
        }

        /// <summary>
        /// 二级面板：**不再自带调色板**（选色直接用画布上方那个），只顺次列出该管道当前的波次颜色，
        /// 最右是 − / +。点已有颜色 = 用当前选中色替换；+ 在末端追加一个；− 删掉末端一个。
        ///
        /// 编辑直接写 <c>PipeItem.colors</c>（序列化字段）：每次改动记一条 Undo + 标脏 + 重画场景。
        /// </summary>
        private void DrawPipeColorPanel()
        {
            if (_pipeColorTarget == null)
                return;

            bool close = false;
            var pipe = _pipeColorTarget;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("管道 " + pipe.name + " 的波次颜色（" +
                (pipe.colors != null ? pipe.colors.Count : 0) + " 个，从左到右 = 释放顺序；选色用上面的调色板）",
                EditorStyles.boldLabel);
            if (GUILayout.Button("关闭", GUILayout.Width(52f)))
                close = true;
            EditorGUILayout.EndHorizontal();

            DrawPipeColorRow(pipe);

            EditorGUILayout.EndVertical();

            if (close)
            {
                _pipeColorTarget = null;
                Repaint();
            }
        }

        /// <summary>
        /// 「内部颜色」面板共用的颜色块（管道波次 / 箱内内容都用它）：
        /// 色块逐行排，**第一排与右侧的 − / + / 清空同排**（第一排因此少排几个，给按钮留位置），
        /// 之后的排每行最多 <see cref="MaxColorsPerRow"/> 个（窗口更窄时按实际宽度更早换行）。
        /// 行数先算好、一次性占位再画 —— 边画边撑高会让下面的控件命中矩形当场换人。
        ///
        /// <paramref name="colors"/> 是**工作副本**：点色块 = 就地替换成当前选中色，− / + / 清空改的也是它；
        /// 任何改动之后调 <paramref name="apply"/> 落库 —— 落库时才 <c>Undo.RecordObject</c>，
        /// 所以撤销拿到的是**改之前**的状态（就地改真身会让撤销变成空操作）。
        /// </summary>
        private void DrawColorListBlock(List<int> colors, System.Action apply)
        {
            if (_palette.Length == 0)
            {
                EditorGUILayout.LabelField("ColorConfig 里没有颜色，无法编辑。");
                return;
            }

            bool playing = Application.isPlaying;
            bool hasBrush = _brush >= 0;   // 橡皮选中时没有「要填的颜色」

            float stride = SwatchSize + SwatchPad;
            float usable = Mathf.Max(SwatchSize, position.width - 16f);

            // 第一排要给右侧那三个按钮留位置（它们**永远和第一排同排**），所以排得比后面几排少
            const float ButtonsWidth = 108f;
            int firstRow = Mathf.Clamp(Mathf.FloorToInt((usable - ButtonsWidth) / stride), 1, MaxColorsPerRow);
            int restPerRow = Mathf.Clamp(Mathf.FloorToInt(usable / stride), 1, MaxColorsPerRow);

            int firstCount = Mathf.Min(colors.Count, firstRow);
            int rest = colors.Count - firstCount;
            int extraRows = rest > 0 ? Mathf.CeilToInt(rest / (float)restPerRow) : 0;

            EditorGUILayout.BeginHorizontal();

            if (colors.Count == 0)
            {
                GUILayout.Label("（空：点 + 追加第一个）", EditorStyles.miniLabel);
            }
            else
            {
                float w = firstCount * stride;
                Rect row0 = GUILayoutUtility.GetRect(w, stride, GUILayout.Width(w), GUILayout.Height(stride));
                for (int i = 0; i < firstCount; i++)
                    HandleColorSwatch(new Rect(row0.x + i * stride, row0.y, SwatchSize, SwatchSize),
                        colors, i, playing, hasBrush, apply);
            }

            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(playing || colors.Count == 0))
            {
                if (GUILayout.Button("−", GUILayout.Width(24f)))
                {
                    colors.RemoveAt(colors.Count - 1);
                    apply();
                }
            }

            using (new EditorGUI.DisabledScope(playing || !hasBrush))
            {
                if (GUILayout.Button("+", GUILayout.Width(24f)))
                {
                    colors.Add(_brush);
                    apply();
                }
            }

            using (new EditorGUI.DisabledScope(playing || colors.Count == 0))
            {
                if (GUILayout.Button("清空", GUILayout.Width(44f)))
                {
                    colors.Clear();
                    apply();
                }
            }

            EditorGUILayout.EndHorizontal();

            // 第二排起：整块一次性占位（换行的行不占固定宽度，所以用 ExpandWidth）
            if (extraRows > 0)
            {
                Rect restArea = GUILayoutUtility.GetRect(0f, extraRows * stride, GUILayout.ExpandWidth(true));
                for (int j = 0; j < rest; j++)
                {
                    var rect = new Rect(
                        restArea.x + (j % restPerRow) * stride,
                        restArea.y + (j / restPerRow) * stride,
                        SwatchSize, SwatchSize);
                    HandleColorSwatch(rect, colors, firstCount + j, playing, hasBrush, apply);
                }
            }
        }

        /// <summary>颜色块里一格色块：画出来 + 点中就用当前选中色替换（就地改工作副本，再落库）。</summary>
        private void HandleColorSwatch(Rect rect, List<int> colors, int index, bool playing, bool hasBrush, System.Action apply)
        {
            DrawSwatch(rect, Mathf.Clamp(colors[index], 0, _palette.Length - 1), false);
            // 不画选中黄框：这里的色块是「内容」，不是笔刷

            if (playing || !hasBrush)
                return;

            Event ev = Event.current;
            if (ev.type != EventType.MouseDown || !rect.Contains(ev.mousePosition))
                return;

            if (colors[index] != _brush)
            {
                colors[index] = _brush;   // 替换为当前选中色
                apply();
            }
            ev.Use();
            Repaint();
        }

        /// <summary>管道面板的颜色行：波次颜色顺次排列（点一下 = 用当前选中色替换），下面 − / + / 清空。</summary>
        private void DrawPipeColorRow(PipeItem pipe)
        {
            if (pipe.colors == null)
                pipe.colors = new List<int>();

            var working = new List<int>(pipe.colors);   // 工作副本：改动只落在它身上
            DrawColorListBlock(working, () =>
            {
                Undo.RecordObject(pipe, "管道波次颜色");   // 此刻 pipe.colors 还是旧值 → 撤销能回去
                pipe.colors = working;                    // 换新实例：就地改真身会让撤销变空操作
                EditorUtility.SetDirty(pipe);
                Repaint();
                SceneView.RepaintAll();
            });
        }

        // ============================================================
        // 格子分类
        // ============================================================

        /// <summary>一格的类别。障碍类互斥（按固定顺序判定）；只有 <see cref="Empty"/> 与 <see cref="Color"/> 可涂。</summary>
        private enum CellKind
        {
            Empty,
            Color,
            Wall,
            Pipe,
            Box,
            Crate,
            Gate,
            Ice,
            Elevator,
        }

        private static bool IsPaintable(CellKind kind)
        {
            return kind == CellKind.Empty || kind == CellKind.Color;
        }

        /// <summary>
        /// 画笔能改的格：空、颜色格，以及**木箱盖住的像素格** —— 木箱只是盖住，底下的像素仍是像素，
        /// 画布上照常显示它的颜色、也照常能涂色 / 标问号（木箱只在外围加棕色描边）。
        ///
        /// 与 <see cref="IsPaintable"/> 的区别：后者表示「这格没有被别的障碍占用」，建箱子 / 建木箱、
        /// 判重叠用的都是那个口径 —— 木箱格在那套口径里**不算可用**，两套别混用。
        /// </summary>
        private static bool IsBrushEditable(CellKind kind)
        {
            return kind == CellKind.Empty || kind == CellKind.Color || kind == CellKind.Crate;
        }

        /// <summary>
        /// 判一格属于哪一类。**障碍优先于像素**：障碍格上不存在像素，唯一例外是冰 ——
        /// 冰不是障碍、冰底下的像素还在，所以冰单独一类（悬停时另报底下像素的颜色）。
        /// 判定顺序与 <see cref="PixelGroup.IsBlocked"/> 一致，另加门 / 冰 / 升降台三类。
        /// </summary>
        private CellKind Classify(int col, int row, out int colorId)
        {
            colorId = -1;

            if (_group.IsWall(col, row))
                return CellKind.Wall;
            if (_group.IsPipe(col, row))
                return CellKind.Pipe;
            if (_group.IsBox(col, row))
                return CellKind.Box;
            if (_group.IsCrateCell(col, row))
                return CellKind.Crate;
            if (_group.IsGateCell(col, row))
                return CellKind.Gate;

            var item = _group.GetItem(col, row);
            if (item != null)
                colorId = item.colorId;

            if (_group.IsFrozenCell(col, row))
                return CellKind.Ice;
            if (IsElevatorCell(col, row))
                return CellKind.Elevator;

            return item != null ? CellKind.Color : CellKind.Empty;
        }

        /// <summary>该格是否落在某个升降台的矩形范围内（升降台没有占用表，只能按矩形查）。</summary>
        private bool IsElevatorCell(int col, int row)
        {
            if (_group.elevators == null)
                return false;

            for (int i = 0; i < _group.elevators.Count; i++)
            {
                var elev = _group.elevators[i];
                if (elev == null)
                    continue;
                if (col >= elev.colMin && col <= elev.colMax && row >= elev.rowMin && row <= elev.rowMax)
                    return true;
            }
            return false;
        }
    }
}
