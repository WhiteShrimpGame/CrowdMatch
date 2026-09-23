using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 容器拖移画布 —— ContainerGroup 的**新增**编辑方式（Inspector 上的「生成 / 导出 / 导入 Containers」
    /// 与「检查并修复容器颜色」全部保留，这个窗口只是多一条更快的手工法）。
    ///
    /// 把容器布局铺成一张格子画布（**下端 = row 0 = 最前排，上端 = 尾排** —— 与场景一致：
    /// 场景里 row 0 的 z 最小、+Z 朝屏幕上方，所以前排在下。格子是扁条：横向不变，纵向压到三分之一），
    /// 按住有车的格子拖到别处松手，就把这辆车挪过去；支持跨列。
    ///
    /// ## 一次拖放做了什么（口径已逐条对齐）
    /// · **取下**被拖的车：源列压紧 —— 它在**上方**的那些车（行更大的）连着中间的空格一起前移，**列里不留洞**。
    /// · **落到落点格**：插入下标 = 落点行夹进 <c>[0, 该列车数]</c>。落点格上（含）及更靠后的车整体后移一格
    ///   （在画面上就是往上让一格），被拖的车落进这一格。落在空格 / 末尾空行上时夹取会把它贴到该列有车部分的
    ///   末尾（列已压紧，末尾 = 第一格空位）。
    /// · 目标列长度超过 <c>rows</c> 时**自动加行**（rows = 该列车数，其它列尾部留空）。
    /// · 画布只画到「最后一个有车的排 + 1 排」，末尾空排不显示（<see cref="ContainerGroup.rows"/> 不变）。
    /// · 高亮画在**落点格**上（半透明黄底 + 2px 亮黄边框），不画在缝上。
    ///
    /// 两列都压紧意味着**同一操作可能让某辆车移动不止 1 格**（跨过原有的洞），这是「列里不留洞」的直接结果。
    ///
    /// 整次拖放 = 一个 Undo 组。列里只改 gridX / gridZ / localPosition，**不新建也不销毁**车，
    /// 颜色 / 容量 / 问号 / ropeGroupId 原样带着走。原地松手（同列且下标没变）算没动，不记 Undo。
    ///
    /// ## 盖子状态
    /// 车落到 row 0 → <see cref="ContainerItem.HideLid"/>（与 <c>RebuildGrid</c> 同一行为）；
    /// 车从 row 0 被拖到后排 → 把盖子重新打开显示（<c>lidOpened=false</c> + <c>SetActive(true)</c>）。
    /// 后者是运行时不会出现的方向（车只会向前补位），不补这一步的话被拖到后排的头车会在 Play 里一直没盖子。
    ///
    /// 格子里只写颜色 id（容量在本工作流里恒为 3，不占地方；要核对就在状态行里悬停看）。
    ///
    /// ## 最前排不动
    /// 画布内容在滚动视图里是顶对齐的，而 row 0 在最底下 —— 可见排数一变（自动加行、末排前移/后移），
    /// row 0 就会上下跳。对齐基准是**上一帧画出来的位置**（<see cref="_anchorAbove"/>），
    /// 每次布局变化走 <see cref="KeepFrontRow"/> 的三级兜底：
    ///
    /// | 情形 | 做法 |
    /// |---|---|
    /// | 内容变矮 | **上方**补出差额留白，above 回到锚点，滚动条不动 |
    /// | 内容变高、滚动范围够 | 把视图往下推差额（`scroll.y += delta`）|
    /// | 内容变高、滚动范围不够（滚动条贴顶 / 内容本来就比视图矮）| 先在**下方**补留白把内容撑长腾出滚动范围，再执行上一行 |
    ///
    /// 留白不做「填满视图」式填充：那种填法以视图底边为**绝对**基准，开窗 / 刷新之后第一次拖放会把画面
    /// 整体拉回旧的对齐位置。现在基准随绘制就地更新，开窗 / 刷新（<see cref="ResetPads"/> + 滚到最下方
    /// <see cref="ScrollToFront"/>）之后对齐的就是**刷新后的那个画面**。
    ///
    /// ## 手感约定（照搬「像素颜色画布」，都是踩过的坑）
    /// 格子一律 <c>Rect.Contains</c> 命中，不用 <c>GUILayout.Button</c>；画只发生在 Repaint；
    /// **收尾放在滚动视图之外**（拖到画布外松手时格子上收不到 MouseUp）；Play 中整块禁用；
    /// 选中变化 / 进出 Play / 撤销重做都重绑并重画。
    /// </summary>
    public class ContainerDragWindow : EditorWindow
    {
        private const string UndoName = "拖移容器";

        /// <summary>格子 高 : 宽 = 1 : 3 —— 横向保持原样，纵向压到三分之一（扁条，一屏能看下更多排）。</summary>
        private const int HeightRatio = 3;

        /// <summary>
        /// 滚动视图之外那些固定控件的总高，**只在还没量到实测可视高度时兜底估算一帧**（见 <see cref="ViewportHeight"/>）。
        /// </summary>
        private const float ChromeHeight = 150f;

        /// <summary>
        /// 滚动视图矩形之外还要扣掉的余量：元素间距 + 可能出现的**横向滚动条**（它压在滚动视图底部，
        /// 占掉的是纵向可视高度）。宁可多扣（可视高度算小 → 滚动量算大）也不要少扣，原因见 <see cref="ViewportHeight"/>。
        /// </summary>
        private const float ScrollbarAllowance = 24f;

        /// <summary>量到的画布可视高度（上下边界之差，见 <see cref="ViewportHeight"/>）；由画布上下两条边界算出。</summary>
        private float _canvasTop;

        /// <summary>画布下边界（状态行顶部）。见 <see cref="ViewportHeight"/>。</summary>
        private float _canvasBottom;

        /// <summary>自适应宽度时给滚动条与缩进留的余量。</summary>
        private const float CanvasMargin = 90f;

        private ContainerGroup _group;
        private ColorConfig _config;
        private Color[] _palette = new Color[0];

        private bool _fitWidth = true;
        private int _cellPx = 32;
        private Vector2 _scroll;

        /// <summary>
        /// 上一帧画出来的「row 0 上方的内容高度」（上方留白 + 上面那些排）。改动后拿它当对齐锚点，
        /// 算出该补多少留白 / 推多少滚动量 —— 见 <see cref="KeepFrontRow"/>。
        /// </summary>
        private float _aboveHeight;

        /// <summary>
        /// 顶部留白：把最前排按在锚点位置上（内容变矮时才需要，补出差额）。
        /// 与 <see cref="_padBelow"/> 一起决定内容总高，见 <see cref="KeepFrontRow"/>。
        /// </summary>
        private float _padAbove;

        /// <summary>
        /// 底部留白：**滚动范围不够时用来「买」出补偿所需的滚动量**。
        /// 排数变多时最前排要靠「视图往下推」保持不动，可滚动条已经贴顶（<c>scroll.y = 0</c>）或内容本来
        /// 就比视图矮 —— 推不动。这时按差额在下方补留白把内容撑长，滚动范围就有了，再把视图推下去。
        /// 只在这种情况下出现（内容本来就比视图高时，长高的那点自己就把滚动范围撑出来了，用不到它）。
        /// </summary>
        private float _padBelow;

        /// <summary>
        /// 下一帧留白的对齐基准 = 上一帧 row 0 在内容里的 y。**它随每次绘制刷新**，所以基准永远是
        /// 「眼前这个画面」—— 尤其开窗 / 刷新之后基准就地重置成「不留白」的状态，之后拖放不会跳回去。
        /// </summary>
        private float _anchorAbove;

        /// <summary>上一帧的格子高度：格子尺寸变了（窗口缩放 / 自适应开关 / 列数变化）留白与锚点都得清掉重来。</summary>
        private float _lastCellH;

        /// <summary>
        /// 还要连续几帧把滚动条压到最下方（开窗 / 刷新快照时置 3）。
        ///
        /// 为什么要连做几帧而不是一次了事：滚动视图是按**上一帧**的内容矩形来夹取滚动位置的，
        /// 刚好在这一帧换了排数 / 首帧还没量到可视高度时，那一次设置会被夹掉 —— 表现出来就是
        /// 「滚动条没到底、第 0 排看不见」。多做两帧幂等的重设即可纠正。
        /// </summary>
        private int _scrollToFrontFrames;

        /// <summary>本帧鼠标下的格（-1 = 不在画布上）。**每帧重算**，Used 事件里不再读 mousePosition。</summary>
        private Vector2Int _hover = new Vector2Int(-1, -1);

        // ===== 拖放状态 =====

        private bool _dragging;

        /// <summary>被拖的车原来所在的格。</summary>
        private Vector2Int _dragFrom = new Vector2Int(-1, -1);

        private ContainerItem _dragItem;

        /// <summary>拖动中光标所在的格（= 落点格，也用来画高亮）。</summary>
        private Vector2Int _dropCell = new Vector2Int(-1, -1);

        /// <summary>本帧解析出来的插入下标（最终落库用的就是它）= 落点行夹进该列的车数范围。</summary>
        private int _dropIndex = -1;

        /// <summary>格内文字样式（缓存；太大 / 太小就不画字了）。</summary>
        private GUIStyle _numStyle;
        private int _numStyleSize = -1;

        // ============================================================
        // 入口 / 生命周期
        // ============================================================

        [MenuItem("CrowdMatch/容器拖移画布")]
        private static void OpenWindow()
        {
            OpenFor(null);
        }

        /// <summary>打开并绑定到指定 ContainerGroup（传 null = 按当前选中自动绑定）。</summary>
        public static void OpenFor(ContainerGroup group)
        {
            var window = GetWindow<ContainerDragWindow>("容器拖移画布");
            window.minSize = new Vector2(420f, 300f);
            window.ResetPads();             // 打开时不留白
            window._scrollToFrontFrames = 3;   // 打开时滚到最下方，露出发配点最近的最前排

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
            ResetPads();               // 开窗（含编辑器重启后恢复窗口）：不留白
            _scrollToFrontFrames = 3;  // 开窗先把最前排露出来
            BindFromSelection();
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= BindFromSelection;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            CancelDrag();   // 关窗时别把手势留在拖动态
        }

        /// <summary>
        /// 撤销 / 重做之后重建快照并重画：Undo 会把车（乃至整个 ContainerGroup 的 rows）还原成另一套状态，
        /// 而画布是读 <c>group.grid</c> + 场景物体画的 —— 不重建就会停在旧快照上（画出已经不存在 / 换了位置的车）。
        /// 顺手取消进行中的拖动：被撤销掉的那辆车可能已经不在原位了。
        /// </summary>
        private void OnUndoRedoPerformed()
        {
            float aboveBefore = _aboveHeight;   // 上一帧画出来的值 = 撤销前
            CancelDrag();
            RefreshSnapshot();
            KeepFrontRow(aboveBefore);   // 撤销 / 重做也是真改动：按锚点把最前排按回原位
            SceneView.RepaintAll();
            Repaint();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // 进 / 出 Play 都会重排容器：取消手势 + 重绑 + 重画，别让画布停在旧快照上
            CancelDrag();
            BindFromSelection();
            Repaint();
        }

        /// <summary>按当前选中重绑 ContainerGroup（选中它或其子物体都行；没选中、或还是同一个，就不动）。</summary>
        private void BindFromSelection()
        {
            var selected = Selection.activeGameObject;
            if (selected == null)
                return;

            var group = selected.GetComponentInParent<ContainerGroup>();
            if (group == null || group == _group)
                return;

            _group = group;
            RefreshSnapshot();
            Repaint();
        }

        /// <summary>取调色板 + 重建占用表。改过场景物件、或换绑到另一个 ContainerGroup 之后必须走一次。</summary>
        private void RefreshSnapshot()
        {
            _config = ColorConfigLocator.Find();
            _palette = BuildPalette(_config);

            if (_group != null)
                _group.RebuildGrid();
        }

        /// <summary>从 ColorConfig 构建调色板（下标 = colorId，与车身上色同一份）。</summary>
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
                EditorGUILayout.HelpBox("未绑定 ContainerGroup：请选中场景里的 ContainerGroup（或其子物体）。", MessageType.Info);
                return;
            }

            if (_group.columns <= 0 || _group.rows <= 0)
            {
                EditorGUILayout.HelpBox(
                    "ContainerGroup 的网格尺寸无效（" + _group.columns + " 列 × " + _group.rows + " 行）。",
                    MessageType.Error);
                return;
            }

            _hover = new Vector2Int(-1, -1);

            DrawToolbar();
            DrawColumnHint();
            DrawCanvas();
            DrawStatusLine();
            DrawDragGhost();   // 必须在本帧事件被 Use 之前画（下面 HandleDrop 会 Use）
            HandleDrop();      // 松手收尾放在滚动视图之外
        }

        private void DrawToolbar()
        {
            bool playing = Application.isPlaying;

            EditorGUILayout.BeginHorizontal();
            _fitWidth = GUILayout.Toggle(_fitWidth, "自适应宽度", GUILayout.Width(90f));

            EditorGUI.BeginDisabledGroup(_fitWidth);   // 自适应时格子尺寸由窗口宽度算出来，滑块没有意义
            _cellPx = Mathf.RoundToInt(EditorGUILayout.Slider("格子宽度", _cellPx, 16f, 64f));
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("操作", GUILayout.Width(90f));
            using (new EditorGUI.DisabledScope(playing))
            {
                if (GUILayout.Button("刷新快照", GUILayout.Width(76f)))
                {
                    CancelDrag();
                    RefreshSnapshot();
                    ResetPads();              // 刷新时清掉上下留白
                    _scrollToFrontFrames = 3; // 并滚到最下方（最前排）
                    SceneView.RepaintAll();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox(
                "按住有车的格子拖到别处松手 → 车挪过去（可跨列）。松手时落点格及其上方的车各后移 1 格、\n" +
                "原位以上的车各前移 1 格（画面上下端 = 最前排）。高亮跟着光标落在目标格上。\n" +
                "每列都压紧（列里不留洞），所以原有空格可能让某辆车移动不止 1 格；目标列放不下时自动加行（改 rows）。\n" +
                "画布只画到最后一个有车的排 + 1 排。车身颜色 / 容量 / 问号 / ropeGroupId 都跟着车走；挪列会改变绳连（Gizmo 可见）。",
                MessageType.Info);

            if (playing)
                EditorGUILayout.HelpBox(
                    "运行模式下不能改场景（会跟正在跑的逻辑打架），请先停止运行。",
                    MessageType.Info);
        }

        /// <summary>画布朝向提示：固定一条，别让下面控件跳位。顺带记下画布的上边界（画布可视高度要用）。</summary>
        private void DrawColumnHint()
        {
            var rect = GUILayoutUtility.GetRect(0f, 16f, GUILayout.ExpandWidth(true));
            _canvasTop = rect.yMax;

            if (Event.current.type != EventType.Repaint)
                return;

            int visible = VisibleRows();
            EditorGUI.LabelField(rect,
                "↑ 上端 = 尾排（row 越大）　｜　↓ 下端 = 最前排（row 0）——与场景一致" +
                (visible < _group.rows
                    ? "　｜　显示 " + visible + " / " + _group.rows + " 排（末尾空排已隐藏）"
                    : "") +
                "　｜　" + _group.name,
                EditorStyles.miniLabel);
        }

        private void DrawCanvas()
        {
            int columns = _group.columns;
            int rows = VisibleRows();
            int cellW = CellW();
            int cellH = CellH();

            _aboveHeight = _padAbove + (rows - 1) * (float)cellH;
            _anchorAbove = _aboveHeight;   // 下一帧的对齐基准就是「这一帧画出来的位置」

            // 开窗 / 刷新快照后的头几帧：先把滚动条压到最下方（最前排露出来），再铺格子。
            // 放在 BeginScrollView **之前**，这样同一帧就用上。
            if (_scrollToFrontFrames > 0)
            {
                _scrollToFrontFrames--;
                ScrollToFront();
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll, true, true, GUILayout.ExpandHeight(true));

            // 上方留白：把最前排按在对齐位置上（内容比锚点矮时才有）。
            if (_padAbove > 0.5f)
                GUILayout.Space(_padAbove);

            // 行**从后往前**画：先画 row = rows-1（最上），最后画 row 0（最下）——
            // 于是「下端 = 最前排」，与场景里 +Z 朝屏幕上方一致（row 0 的 z 最小）。
            for (int row = rows - 1; row >= 0; row--)
            {
                EditorGUILayout.BeginHorizontal();
                for (int col = 0; col < columns; col++)
                {
                    Rect rect = GUILayoutUtility.GetRect(cellW, cellH, GUILayout.Width(cellW), GUILayout.Height(cellH));
                    DrawCell(rect, col, row, cellW, cellH);
                    HandleCell(rect, col, row);
                }
                EditorGUILayout.EndHorizontal();
            }

            // 下方留白：只用来腾出滚动范围（见 _padBelow）。它落在最前排之下，不影响最前排的位置。
            if (_padBelow > 0.5f)
                GUILayout.Space(_padBelow);

            EditorGUILayout.EndScrollView();

            _lastCellH = cellH;   // 供 KeepFrontRow 判断「格子尺寸是否变了」
        }

        /// <summary>
        /// 画布可视高度 = 画布上边界（<see cref="_canvasTop"/>，朝向提示行底部）到画布下边界
        /// （<see cref="_canvasBottom"/>，状态行顶部）之间的距离，再扣掉 <see cref="ScrollbarAllowance"/>，**差一帧**。
        ///
        /// 为什么不直接取滚动视图的矩形：<c>GUILayoutUtility.GetLastRect()</c> 不能紧跟在 Begin 组之后调用
        /// （会报 "You cannot call GetLast immediately after beginning a group"）。自己量上下边界则完全绕开这个限制。
        ///
        /// **方向必须是「宁可算小」**：可视高算小 → 滚动量算大 → 滚动条被 ScrollView 夹回真实底部，
        /// 最前排一定露得出来；反过来算大就会停在底边之上，正好把最前排藏在下面（这里踩过一次）。
        /// </summary>
        private float ViewportHeight()
        {
            float measured = _canvasBottom - _canvasTop - ScrollbarAllowance;
            if (measured > 100f)
                return measured;
            return Mathf.Max(80f, position.height - ChromeHeight);   // 还没量到（第一帧）：估算兜底
        }

        /// <summary>清掉上下留白（开窗 / 刷新快照 / 格子尺寸变化时用 —— 锚点与留白都无意义了）。</summary>
        private void ResetPads()
        {
            _padAbove = 0f;
            _padBelow = 0f;
        }

        /// <summary>
        /// 保持最前排的视觉位置不动：这次改动（拖放 / 撤销）让「row 0 上方的内容」高矮变了，
        /// 于是内容里的 row 0 会跟着下移 / 上移。三级兜底，按可行性依次尝试：
        ///
        /// 1. **内容变矮** → 顶部补出差额留白，above 回到锚点，滚动条完全不用动；
        /// 2. **内容变高、滚动范围内还有余量** → 把视图往下推差额（`scroll.y += delta`）；
        /// 3. **内容变高、滚动范围不够**（滚动条已贴顶 / 内容本来就比视图矮）→ 先在**下方**补出差额留白
        ///    把内容撑长、腾出滚动范围，再执行第 2 步。
        ///
        /// 视觉尺寸（格子高度）变了的话，上面的高度差没有意义，直接清留白重锚。
        /// </summary>
        private void KeepFrontRow(float aboveBefore)
        {
            if (aboveBefore <= 0f)
                return;   // 还没画过一帧，没有可比对的基准

            float cellH = CellH();
            if (!Mathf.Approximately(cellH, _lastCellH))
            {
                ResetPads();
                return;   // 格子尺寸变了：锚点跟留白都按旧尺寸算的，清掉
            }

            int rows = VisibleRows();
            float natural = (rows - 1) * cellH;
            float delta = natural - aboveBefore;

            if (delta <= 0f)
            {
                // 1) 内容变矮：顶部补出留白把最前排按回原位。
                //    注意**不动 _padBelow** —— 这样内容总高不变，滚动范围也不会缩，scroll.y 依旧有效。
                _padAbove = -delta;
                return;
            }

            // 2) + 3) 内容变高：留白补不出来（above 只能变大），只能把视图往下推。
            _padAbove = 0f;
            float target = Mathf.Max(0f, _scroll.y + delta);

            // 现有滚动范围（用**新**排数、旧留白算）。不够就在下方补留白「买」出差额，
            // 补完之后滚动范围恒等于 target，于是这一步可以无条件执行。
            float maxScroll = _padAbove + rows * cellH + _padBelow - ViewportHeight();
            float need = target - maxScroll;
            if (need > 0f)
                _padBelow += need;

            _scroll.y = target;
        }

        /// <summary>
        /// 把滚动条**直接**设到最下端（露出发配点最近的**最前排**）。
        ///
        /// 用 <c>float.MaxValue</c> 而不是自己算「内容高 − 可视高」：滚动视图会把超出的值夹回真实底部，
        /// 于是完全不需要量可视高度 —— 那套算法对可视高度的估算很敏感，估歪一点就会停在底边之上、
        /// 把最前排藏在折线下面（这里踩过一次）。开窗 / 刷新快照时连设几帧，见 <see cref="_scrollToFrontFrames"/>。
        /// </summary>
        private void ScrollToFront()
        {
            _scroll.y = float.MaxValue;
        }

        /// <summary>
        /// 画到「最后一个有车的排 + 1 排」为止，后面那些空排不画（否则容器少时下方会挂一大片空格）。
        /// 只影响**显示**：<see cref="ContainerGroup.rows"/> 与实际网格不动，落在最后那排空行上的拖放
        /// 仍然解析成「贴到该列末尾」（见 <see cref="ResolveDropIndex"/> 的区间夹取）。
        /// </summary>
        private int VisibleRows()
        {
            int last = -1;
            for (int row = _group.rows - 1; row >= 0 && last < 0; row--)
                for (int col = 0; col < _group.columns; col++)
                    if (_group.GetItem(col, row) != null)
                    {
                        last = row;
                        break;
                    }

            return Mathf.Clamp(last + 2, 1, Mathf.Max(1, _group.rows));
        }

        /// <summary>
        /// 格子宽度（像素）。自适应时按窗口宽度算：<c>列数 × 宽</c> 要能塞进窗口；手动时就是滑块值。
        /// **尺寸只在 <see cref="CellW"/> / <see cref="CellH"/> 两处算**，免得公式漂移。
        /// </summary>
        private int CellW()
        {
            if (!_fitWidth)
                return _cellPx;
            int columns = Mathf.Max(1, _group.columns);
            return Mathf.Clamp(
                Mathf.FloorToInt((position.width - CanvasMargin) / columns), 8, 96);
        }

        /// <summary>格子高度 = 宽度的 1/<see cref="HeightRatio"/>（横向不变，纵向压到三分之一）。</summary>
        private int CellH()
        {
            return Mathf.Max(4, CellW() / HeightRatio);
        }

        private void DrawCell(Rect rect, int col, int row, int cellW, int cellH)
        {
            var item = _group.GetItem(col, row);

            if (Event.current.type == EventType.Repaint)
            {
                var border = new Color(0.1f, 0.1f, 0.1f);

                if (item == null)
                {
                    EditorGUI.DrawRect(rect, new Color(0.14f, 0.14f, 0.16f));
                }
                else
                {
                    Color fill = ColorOf(item.colorId);
                    EditorGUI.DrawRect(rect, fill);

                    // 只写颜色 id（容量在本工作流里恒为 3，不必占地方；要核对容量就在状态行里悬停看）。
                    // 扁条的纵向空间只有宽的 1/3：字高跟着格高走，矮到放不下就不写字（免得糊成一团）。
                    if (cellH >= 9 && cellW >= 12)
                    {
                        var style = NumStyle(Mathf.Clamp(Mathf.Min(cellW / 3, cellH - 2), 7, 14));
                        style.normal.textColor = Luminance(fill) > 0.55f ? Color.black : Color.white;
                        EditorGUI.LabelField(rect, item.colorId.ToString(), style);
                    }
                }

                EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), border);
                EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), border);
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), border);
                EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), border);

                if (_dragging)
                {
                    if (col == _dragFrom.x && row == _dragFrom.y)
                    {
                        EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.55f));   // 原位压暗
                    }
                    else if (col == _dropCell.x && row == _dropCell.y)
                    {
                        // 落点格高亮：半透明黄底 + 2px 亮黄边框。
                        // 含义 = 这一格及其上方的车整体上移一格让位，被拖的车落到这一格。
                        var hi = new Color(1f, 0.85f, 0.2f);
                        EditorGUI.DrawRect(rect, new Color(1f, 0.9f, 0.3f, 0.3f));
                        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 2f), hi);
                        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), hi);
                        EditorGUI.DrawRect(new Rect(rect.x, rect.y, 2f, rect.height), hi);
                        EditorGUI.DrawRect(new Rect(rect.xMax - 2f, rect.y, 2f, rect.height), hi);
                    }
                }
            }

            if (rect.Contains(Event.current.mousePosition))
                _hover = new Vector2Int(col, row);
        }

        private Color ColorOf(int colorId)
        {
            if (_palette.Length == 0)
                return Color.gray;
            return _palette[Mathf.Clamp(colorId, 0, _palette.Length - 1)];
        }

        /// <summary>颜色的相对亮度（0~1），只用来决定格内文字用黑字还是白字。</summary>
        private static float Luminance(Color c)
        {
            return 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
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
            }
            return _numStyle;   // 文字颜色每格单独设，画完即用，不跨格残留
        }

        /// <summary>拖动中跟着鼠标的那块车色小方块（画在所有格子之上，比例与格子一致）。</summary>
        private void DrawDragGhost()
        {
            if (!_dragging || _dragItem == null || Event.current.type != EventType.Repaint)
                return;

            float w = Mathf.Max(12, CellW() * 0.8f);
            float h = Mathf.Max(6, CellH());
            var mouse = Event.current.mousePosition;
            var rect = new Rect(mouse.x - w * 0.5f, mouse.y - h * 0.5f, w, h);

            var fill = ColorOf(_dragItem.colorId);
            fill.a = 0.8f;
            EditorGUI.DrawRect(rect, fill);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 2f), new Color(1f, 0.85f, 0.2f));
        }

        /// <summary>底部状态行：固定只占一条等高矩形，别把上面 / 下面的控件顶走。顺带记下画布的下边界。</summary>
        private void DrawStatusLine()
        {
            var rect = GUILayoutUtility.GetRect(0f, 18f, GUILayout.ExpandWidth(true));
            _canvasBottom = rect.y;   // 画布可视高度 = 这个减 _canvasTop（见 ViewportHeight）
            if (Event.current.type != EventType.Repaint)
                return;

            string text;

            if (_dragging && _dragItem != null)
            {
                // 实时预览这次松手会怎么插：落点下标就是本帧解析出来的 _dropIndex（与 PerformDrop 共用）
                int after = ColumnCount(_dropCell.x);
                if (_dropCell.x == _dragFrom.x)
                    after--;                        // 被拖的车从源列里扣掉
                int index = Mathf.Clamp(_dropIndex, 0, Mathf.Max(0, after));

                text = "拖动 " + _dragItem.name + "（颜色 " + _dragItem.colorId + " / 容量 " + _dragItem.capacity + "）" +
                       "　→　落点 (" + _dropCell.x + ", " + _dropCell.y + ")：" +
                       (index >= after
                           ? "该列末尾（现有 " + after + " 辆之后）"
                           : "该列第 " + index + " 辆之前（该格及其上方的 " + (after - index) + " 辆各后移 1 格）") +
                       "　｜　该列 " + after + " → " + (after + 1) + " 辆" +
                       (after + 1 > _group.rows ? "，超出 rows(" + _group.rows + ") → 自动加 1 行" : "") +
                       "　｜　松手落库，" + (_hover.x < 0 ? "当前光标不在画布上 → 松手会取消" : "拖出画布松手 = 取消");
            }
            else if (_hover.x < 0)
            {
                text = "按住有车的格子拖到别处松手即可移动；松手在画布外 = 取消";
            }
            else
            {
                var item = _group.GetItem(_hover.x, _hover.y);
                text = "格 (" + _hover.x + ", " + _hover.y + ")：" + (item == null
                    ? "空格"
                    : "颜色 " + item.colorId + " / 容量 " + item.capacity +
                      (item.isQuestion ? "（问号车）" : "") +
                      (item.ropeGroupId != 0 ? "　绳组 " + item.ropeGroupId : ""));
            }

            text += "　｜　网格 " + _group.columns + " 列 × " + _group.rows + " 行";

            EditorGUI.LabelField(rect, text, EditorStyles.miniLabel);
        }

        // ============================================================
        // 手势
        // ============================================================

        private void HandleCell(Rect rect, int col, int row)
        {
            if (Application.isPlaying)
                return;

            Event ev = Event.current;

            if (ev.type == EventType.MouseDown && rect.Contains(ev.mousePosition))
            {
                BeginDrag(col, row);
                ev.Use();
                Repaint();
            }
            else if (ev.type == EventType.MouseDrag && _dragging && rect.Contains(ev.mousePosition))
            {
                UpdateDropTarget(col, row);
                ev.Use();
                Repaint();
            }
        }

        /// <summary>落点 = 光标所在的那一格（不细分格的上下半）。高亮与状态行共用这一份。</summary>
        private void UpdateDropTarget(int col, int row)
        {
            _dropCell = new Vector2Int(col, row);
            _dropIndex = ResolveDropIndex(col, row);
        }

        /// <summary>
        /// 插入下标 = 落点行，夹进 <c>[0, 该列车数]</c>。
        ///
        /// 落点格上（及更靠后的排）的车整体后移一格（显示上就是往上让一格），被拖的车落到落点格；
        /// 夹取把「落在空格 / 末尾空行上」统一成贴到该列有车部分的末尾（列本来就压紧，末尾 = 第一格空位）。
        /// </summary>
        private int ResolveDropIndex(int col, int row)
        {
            int count = ColumnCount(col);
            if (col == _dragFrom.x)
                count--;   // 同列：被拖的车先从源列里扣掉
            return Mathf.Clamp(row, 0, Mathf.Max(0, count));
        }

        /// <summary>某列的车数。</summary>
        private int ColumnCount(int col)
        {
            int n = 0;
            for (int row = 0; row < _group.rows; row++)
                if (_group.GetItem(col, row) != null)
                    n++;
            return n;
        }

        /// <summary>按下有车的格子开始拖（空格按下什么都不做）。</summary>
        private void BeginDrag(int col, int row)
        {
            CancelDrag();

            _group.RebuildGrid();   // 先让 grid 与场景一致，免得抓到旧引用
            var item = _group.GetItem(col, row);
            if (item == null)
                return;

            _dragging = true;
            _dragFrom = new Vector2Int(col, row);
            _dragItem = item;
            _dropCell = _dragFrom;
            _dropIndex = row;
        }

        /// <summary>
        /// 松手收尾（含拖到画布外松手，所以必须在滚动视图之外调用）。
        /// **落点以松手那一刻光标所在的格为准**（<see cref="_hover"/>），不沿用拖动途中最后一次记下的格 ——
        /// 拖着鼠标快速掠过、或中途拖出窗口再回来时，两者可能不一致。松手不在任何格子上 → 取消，什么都不改。
        /// </summary>
        private void HandleDrop()
        {
            Event ev = Event.current;
            if (ev.type != EventType.MouseUp || !_dragging)
                return;

            _dragging = false;

            if (!Application.isPlaying && _hover.x >= 0)
            {
                UpdateDropTarget(_hover.x, _hover.y);
                PerformDrop();
            }

            _dragItem = null;
            _dropCell = new Vector2Int(-1, -1);
            _dropIndex = -1;
            Repaint();
            ev.Use();
        }

        private void CancelDrag()
        {
            _dragging = false;
            _dragItem = null;
            _dragFrom = new Vector2Int(-1, -1);
            _dropCell = new Vector2Int(-1, -1);
            _dropIndex = -1;
        }

        // ============================================================
        // 落库
        // ============================================================

        /// <summary>某列的车（按行升序）。列本来就是紧凑的，这里也顺带把「洞」忽略掉。</summary>
        private List<ContainerItem> ColumnItems(int col)
        {
            var list = new List<ContainerItem>();
            for (int row = 0; row < _group.rows; row++)
            {
                var item = _group.GetItem(col, row);
                if (item != null)
                    list.Add(item);
            }
            return list;
        }

        /// <summary>
        /// 执行一次拖放：源列压紧 → 目标列按 <see cref="_dropIndex"/>（落点行夹出来的下标）插入
        /// → 两列重新铺行 → 目标列超长就加 rows → 重建 grid + 同步盖子。整次一个 Undo 组。
        /// </summary>
        private void PerformDrop()
        {
            int toCol = _dropCell.x;
            float aboveBefore = _aboveHeight;   // 改动前「row 0 上方」的高度，末尾用来把最前排钉在原地

            var fromList = ColumnItems(_dragFrom.x);
            if (!fromList.Remove(_dragItem))
            {
                Debug.LogWarning("[容器拖移画布] 被拖的车已不在原位（快照过期），本次拖放作废。");
                return;
            }

            var targetList = toCol == _dragFrom.x ? fromList : ColumnItems(toCol);
            int index = Mathf.Clamp(_dropIndex, 0, targetList.Count);

            // 同列且下标没变 = 原地松手：什么都不改，也不记一条空 Undo
            // （注意：这**不会**顺手压紧该列原有的洞 —— 「没动」就是没动）
            if (toCol == _dragFrom.x && index == _dragFrom.y)
            {
                Debug.Log("[容器拖移画布] " + _dragItem.name + " 原地松手，未改动。");
                return;
            }

            targetList.Insert(index, _dragItem);
            int newCount = targetList.Count;

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);

            bool grewRows = false;
            if (newCount > _group.rows)
            {
                Undo.RecordObject(_group, UndoName);
                _group.rows = newCount;   // 目标列放不下：末尾加行（其它列尾部自然多出空格）
                grewRows = true;
            }

            var moved = new List<ContainerItem>();
            RewriteColumn(_dragFrom.x, fromList, moved);
            if (toCol != _dragFrom.x)
                RewriteColumn(toCol, targetList, moved);

            _group.RebuildGrid();
            for (int i = 0; i < moved.Count; i++)
                SyncLid(moved[i]);

            EditorUtility.SetDirty(_group);
            Undo.CollapseUndoOperations(undoGroup);
            KeepFrontRow(aboveBefore);   // 自动加行 / 末排变化会让最前排上下跳，这里按锚点补回来
            SceneView.RepaintAll();

            Debug.Log("[容器拖移画布] " + _dragItem.name + "：(" + _dragFrom.x + "," + _dragFrom.y + ") → (" +
                      toCol + "," + index + ")，两列共移动 " + moved.Count + " 辆车" +
                      (grewRows ? "，rows 自动加到 " + _group.rows : "") + "。");
        }

        /// <summary>把一列的车按 <paramref name="list"/> 的顺序重铺到 row 0..n-1（压紧，不留洞）。</summary>
        private void RewriteColumn(int col, List<ContainerItem> list, List<ContainerItem> moved)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var car = list[i];
                if (car == null)
                    continue;
                if (car.gridX == col && car.gridZ == i)
                    continue;   // 位置没变，不动它（也就不会记空 Undo）

                Undo.RecordObject(car, UndoName);
                Undo.RecordObject(car.transform, UndoName);
                car.gridX = col;
                car.gridZ = i;
                car.transform.localPosition = _group.GetLocalPosition(col, i);
                EditorUtility.SetDirty(car);
                moved.Add(car);
            }
        }

        /// <summary>
        /// 让盖子状态跟行号一致：row 0 = 盖子隐藏（前排车本来就没有盖子，与 <c>RebuildGrid</c> 同一行为）；
        /// 后排 = 盖子显示。运行时车只会向前补位，所以「从 row 0 挪到后排」只有这个工具会造成，
        /// 不补这一步的话那辆车在 Play 里会一直没盖子。
        /// </summary>
        private static void SyncLid(ContainerItem car)
        {
            if (car == null)
                return;

            if (car.gridZ == 0)
            {
                car.HideLid();   // 幂等；同时让未揭晓的问号车揭晓（同 RebuildGrid）
                return;
            }

            if (!car.lidOpened)
                return;

            if (car.lidTransform != null)
            {
                Undo.RecordObject(car.lidTransform.gameObject, UndoName);
                car.lidTransform.gameObject.SetActive(true);
            }
            car.lidOpened = false;
            EditorUtility.SetDirty(car);
        }
    }
}
