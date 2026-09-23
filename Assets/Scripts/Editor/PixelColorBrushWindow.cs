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
    /// ## 三条口径（都与既有工具对齐）
    /// · **画布顶行 = gridZ 0 = 最前排**，与「导出颜色 (PNG)」一致（那边图片顶行就是 gridZ 0）。
    /// · **障碍格不可涂**，只以底色 + 单字标记显示并写明类别：墙 / 管 / 箱 / 木 / 门 / 冰 / 升。
    ///   像素不存在于障碍格上，唯一例外是冰 —— 冰不是障碍、冰底下的像素仍在，所以冰格照常画出颜色、
    ///   悬停时另报底下像素的 colorId。
    /// · **colorId 与 ColorConfig.materials 下标一一对应**，调色板直接取自 ColorConfig，不另立一份选项表。
    ///
    /// ## 笔刷值的表示
    /// <c>_brush = -1</c> = 橡皮、<c>&gt;= 0</c> = colorId。**不能**照搬「0 = 橡皮」那种表示：
    /// 这里 colorId 0 是一个正当颜色，拿 0 当橡皮会把色 0 的格子误判成擦除。
    ///
    /// ## 手势与撤销
    /// 按下记一个 Undo 组 → 拖动期间只改对象 + <c>Repaint</c>（不记 Undo、不 SetDirty）→
    /// 松手才 <c>RebuildGrid</c> + <c>SetDirty</c> + 收拢 Undo 组，整笔一步撤销。
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

        /// <summary>窗口的三种模式：涂颜色 / 添加墙体 / 删除墙体。三种互斥，切换时清掉各自的待定状态。</summary>
        private enum Mode
        {
            Color,
            AddWall,
            DeleteWall,
        }

        private const float SwatchSize = 24f;
        private const float SwatchPad = 2f;

        /// <summary>自适应宽度时给滚动条与缩进留的余量，不留就会横向滚出画布。</summary>
        private const float CanvasMargin = 90f;

        private PixelGroup _group;
        private ColorConfig _config;
        private Color[] _palette = new Color[0];

        private bool _fitWidth = true;
        private int _cellPx = 24;
        private Tool _tool = Tool.Free;
        private Mode _mode = Mode.Color;

        /// <summary>笔刷值：-1 = 橡皮（删除该格像素），&gt;= 0 = 要涂的 colorId。</summary>
        private int _brush = -1;

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

        private Vector2 _scroll;

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
            BindFromSelection();
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= BindFromSelection;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            CancelStroke();   // 关窗时别把手势留在 dragging / rectActive
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // 进 / 出 Play 都会动到像素对象：重新绑一次并重画，别让画布停在旧快照上
            CancelStroke();
            BindFromSelection();
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

            DrawViewToolbar();
            DrawPalette();
            DrawCanvas();
            DrawStatusLine();

            // 手势收尾放在滚动视图之外：拖到画布外松手时格子上的 HandleCell 收不到 MouseUp
            HandleStrokeEnd();
        }

        /// <summary>
        /// 工具栏三段：模式（三选一）/ 视图（自适应 + 格子像素 + 颜色笔刷的手势）/ 操作。
        /// **每一段都常驻**，不按模式隐藏 —— 隐藏会让下面控件的命中矩形当场换人（见 skill 的说明）。
        /// 不适用的控件只禁用，不改布局高度。
        /// </summary>
        private void DrawViewToolbar()
        {
            bool colorMode = _mode == Mode.Color;
            bool playing = Application.isPlaying;

            // 待创建墙线的占格：本帧只算一次，下面按钮 / 画布 / 状态行都读这一份
            _wallStrokeCells = _mode == Mode.AddWall ? CollectStrokeCells() : null;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("模式", GUILayout.Width(90f));
            DrawModeButton(Mode.Color, "涂颜色");
            DrawModeButton(Mode.AddWall, "添加墙体");
            DrawModeButton(Mode.DeleteWall, "删除墙体");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _fitWidth = GUILayout.Toggle(_fitWidth, "自适应宽度", GUILayout.Width(90f));

            EditorGUI.BeginDisabledGroup(_fitWidth);   // 自适应时格子像素由宽度算出来，滑块没有意义
            _cellPx = Mathf.RoundToInt(EditorGUILayout.Slider("格子像素", _cellPx, 12f, 64f));
            EditorGUI.EndDisabledGroup();

            using (new EditorGUI.DisabledScope(!colorMode))   // 工具只影响颜色笔刷的手势
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

            // 「创建墙体」只在添加墙体模式下出现（其它模式连按钮都不画）——
            // 判据见 TryValidateWallStroke，不通过时置灰而不是隐藏，免得按钮忽隐忽现。
            // 不设「取消待创建」：按下左键开新一笔、切走模式都会自动清掉待创建墙线（见 HandleCell / DrawModeButton）。
            if (_mode == Mode.AddWall)
            {
                bool wallReady = !playing && TryValidateWallStroke(_wallStrokeCells, out _, out _);
                using (new EditorGUI.DisabledScope(!wallReady))
                {
                    if (GUILayout.Button("创建墙体", GUILayout.Width(76f)))
                        CreateWallFromStroke();
                }
            }

            EditorGUILayout.EndHorizontal();

            if (playing)
                EditorGUILayout.HelpBox(
                    "运行模式下不能改场景（会跟正在跑的逻辑打架），请先停止运行。",
                    MessageType.Info);
        }

        /// <summary>模式按钮：用 Toggle 的选中态做互斥选择。切换时把两种待定状态都清掉，避免跨模式残留。</summary>
        private void DrawModeButton(Mode mode, string label)
        {
            bool on = GUILayout.Toggle(_mode == mode, label, EditorStyles.miniButton, GUILayout.Width(80f));
            if (!on || _mode == mode)
                return;

            _mode = mode;
            _wallStroke.Clear();
            _deletePendingWall = null;
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

            // 墙体模式下保留这一块（高度稳定，不让下面控件跳位），但压暗且不响应点击
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
                    ev.Use();
                    Repaint();
                }
            }
        }

        private void DrawSwatch(Rect rect, int value)
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

            if (_brush == value)
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
                    EditorGUI.DrawRect(rect, _palette.Length > 0
                        ? _palette[Mathf.Clamp(colorId, 0, _palette.Length - 1)]
                        : Color.gray);
                }
                else if (kind == CellKind.Ice && item != null && item.colorId >= 0 && item.colorId < _palette.Length)
                {
                    // 冰格：底下像素的颜色照常画出来，再叠冰的标记（冰只是「视为不暴露」，不是障碍）
                    EditorGUI.DrawRect(rect, _palette[item.colorId]);
                }
                else
                {
                    EditorGUI.DrawRect(rect, BackgroundOf(kind));
                }

                // 内容：障碍画单字标记，颜色格画 colorId 数字（太小就不画了，看不清反而乱）
                if (!IsPaintable(kind))
                {
                    EditorGUI.LabelField(rect, MarkerOf(kind),
                        MarkStyle(Mathf.Clamp(px / 5, 7, 11)));
                }
                else if (kind == CellKind.Color && px >= 14)
                {
                    EditorGUI.LabelField(rect, colorId.ToString(),
                        NumStyle(Mathf.Clamp(px / 3, 9, 24)));
                }

                // 矩形拖动中的实时预览：范围内的格叠一层半透明黄
                if (_rectActive && InDraggedRect(col, gridZ))
                    EditorGUI.DrawRect(rect, new Color(1f, 0.9f, 0.3f, 0.35f));

                // 墙体模式的高亮：待创建墙线（划过的实色橙、闭环补段淡橙）/ 待删除的那面墙（红）
                if (_mode == Mode.AddWall && _wallStrokeCells != null)
                {
                    var cell = new Vector2Int(col, gridZ);
                    if (_wallStroke.Contains(cell))
                        EditorGUI.DrawRect(rect, new Color(1f, 0.45f, 0.15f, 0.5f));
                    else if (_wallStrokeCells.Contains(cell))
                        EditorGUI.DrawRect(rect, new Color(1f, 0.45f, 0.15f, 0.22f));
                }
                else if (_mode == Mode.DeleteWall && IsCellOfPendingWall(col, gridZ))
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
                        ? "　｜　可生成 ✓ 点「创建墙体」（化简后 " + PendingWallPoints().Count + " 个端点）"
                        : "　｜　✗ " + reason;
                }
                else
                {
                    text += "　｜　按住左键沿行 / 列拖出墙线（松手不落库；再拖一笔或切走模式都会重新开始）";
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
                    text += "，底下像素颜色 " + item.colorId;
            }

            text += "　｜　网格 " + _group.columns + " 列 × " + _group.TotalRows + " 行";

            if (_mode == Mode.Color)
                text += "　｜　笔刷 " + (_brush < 0 ? "橡皮" : "颜色 " + _brush) +
                        "　｜　工具 " + (_tool == Tool.Free ? "自由涂" : "矩形");

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
                    StartStroke();

                    if (_tool == Tool.Free)
                    {
                        PaintCell(col, gridZ);
                    }
                    else
                    {
                        _rectActive = true;
                        _rectAnchor = new Vector2Int(col, gridZ);
                        _rectCurrent = _rectAnchor;
                    }
                }
                else if (_mode == Mode.AddWall)
                {
                    // 一次拖动 = 一条新墙线：按下时清掉上一条待创建的
                    _dragging = true;
                    _rectActive = false;
                    _wallStroke.Clear();
                    AppendWallCell(col, gridZ);
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
                else if (_mode == Mode.AddWall)
                {
                    AppendWallCell(col, gridZ);
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
        /// 涂颜色：矩形到这一刻才真正落库，随后统一重建网格 + 落盘 + 收拢 Undo。
        /// 添加墙体：松手只是结束这一笔选择，**待创建的墙线留着**，等点「创建墙体」才落库。
        /// </summary>
        private void HandleStrokeEnd()
        {
            Event ev = Event.current;
            if (ev.type != EventType.MouseUp || !_dragging)
                return;

            _dragging = false;

            if (_mode == Mode.Color)
            {
                if (_tool == Tool.Rect && _rectActive && !Application.isPlaying)
                    PaintRect();

                _rectActive = false;
                _strokePainted.Clear();

                if (_dirty)
                {
                    _dirty = false;
                    _group.RebuildGrid();          // 占用表 / 冰 / 木箱掩码跟着新像素一起刷新
                    EditorUtility.SetDirty(_group);
                    SceneView.RepaintAll();
                }

                Undo.CollapseUndoOperations(_undoGroup);   // 整笔一步撤销
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
            if (!IsPaintable(kind))
                return;   // 障碍格不可涂

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
            item.colorId = _brush;
            item.ApplyMaterial(_config);
            EditorUtility.SetDirty(item);
            _dirty = true;
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

        /// <summary>待创建墙线的端点（WallItem 的口径：Vector2 的 x = 列 col、y = 行 row）。</summary>
        private List<Vector2> StrokePoints()
        {
            var points = new List<Vector2>(_wallStroke.Count);
            for (int i = 0; i < _wallStroke.Count; i++)
                points.Add(new Vector2(_wallStroke[i].x, _wallStroke[i].y));
            return points;
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
