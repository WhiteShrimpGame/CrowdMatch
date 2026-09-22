using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CrowdMatch
{
    /// <summary>冰的显示方式。</summary>
    public enum IceDisplayMode
    {
        /// <summary>四角拼接 Sprite：每个网格交点一张填充图（现状，圆角由贴图承担）。</summary>
        CornerSprite,

        /// <summary>实时生成 Mesh：底部多边形（逐角切 45° 斜边）→ 侧面棱柱 → 斜面 → 顶面。</summary>
        GeneratedMesh,
    }

    /// <summary>
    /// 冰冻组：在像素网格上占一片**任意形状的连通格**（以逐格列表记录，不是矩形也不是线段）。
    /// 冰组带一个**冰冻计数**：只要计数还没到 0，组内像素就**视为不暴露**
    /// （不可点击、也不参与同色连通块）；**每成功点击移出一次**（一次点击，不按它移出几颗像素），
    /// 全局计数 -1；归 0 后冰化开、组内像素恢复正常。
    ///
    /// 冰的可见效果有两种，由 <see cref="displayMode"/> 选：
    ///   · **四角拼接**（**填充 · 单色** · Sprite 模式）：把冰组当成「一片格子组成的区域」，
    ///     在每个网格交点放一张图，得到圆角与拐角连贯的整块冰。
    ///   · **实时 Mesh**：逐格并集的轮廓切 45° 斜边 → 侧面棱柱 → 斜面 → 顶面，几何见 <see cref="IceMeshBuilder"/>。
    ///
    /// **每个冰组独立考虑**：只把**本组**的成员格当成激活，别的冰组与本组无关 —— 交界处各自收边、各自倒圆角。
    /// 因此既不需要全局变体图，也不需要「哪个交点归哪个组」的仲裁，最多只用到 5 张图（单元号 0/1/3/4/8）。
    ///
    /// 摆放与生成方式参照 <see cref="FrameItem"/>：单元模板 <see cref="atomicObject"/> 逐个 Instantiate、
    /// 按缓存路径找到 SpriteRenderer 换上 <see cref="sprites"/> 里对应的一张，并绕 Y 转
    /// <c>rotation × 90°</c>。
    ///
    /// **基准姿态由模板自己承担**：实际旋转 = <c>Euler(0, rotation×90, 0) × atomicObject.localRotation</c>。
    /// 也就是说「rotation = 0」渲染出来必须正好是该图形类的基准姿态（逐类的基准姿态见
    /// <see cref="CornerTileTable.FillClassNames"/>）。
    ///
    /// ⚠ 这个角度**只能在一处**补：要么写在模板的 localRotation 上，要么写进代码里的基准角常量。
    ///   两边各补一个 180° 会互相抵消（净 0），看着是对的；删掉其中一个就整体偏 180° —— 这是踩过的坑，
    ///   所以这里**不再提供基准角字段**，统一由模板承担：贴图基准姿态不对就转 Atomic 子物体。
    ///
    /// 根**不旋转**，只有单元各自转，所以计数数字始终保持预制体朝向。
    /// </summary>
    public class IceItem : MonoBehaviour
    {
        [Header("冰组")]
        [Tooltip("冰组成员格（网格坐标：x = 列 col，y = 行 row）。相邻格不要求轴对齐，可以是任意连通形状")]
        public List<Vector2> cells = new List<Vector2>();

        [Tooltip("冰冻计数初值：每成功**点击移出一次**就 -1（按点击算，不按像素数），归 0 时冰化开。编辑器配的是这个值")]
        [Min(1)]
        public int freezeCount = 5;

        [Tooltip("勾选后：冰组未暴露时不显示计数、也不递减计数；暴露后（组内至少 1 颗像素按正常规则暴露）才开始显示并递减。" +
                 "判定用的是**点击前**的暴露状态 —— 所以「使之暴露的那一次点击」本身不计数")]
        public bool meltOnlyWhenExposed;

        [Header("显示模式")]
        [Tooltip("四角拼接 Sprite（现状，用下面那组 sprites）/ 实时生成 Mesh（用下面那组 Mesh 参数）")]
        public IceDisplayMode displayMode = IceDisplayMode.CornerSprite;

        [Header("显示（参考 FrameItem）")]
        [Tooltip("填充贴图，下标 = 单元号 0..12（含义见 CornerTileTable.FillClassNames）。" +
                 "单色填充只用 0/1/3/4/8 这 5 张，其余留空即可 —— 保留 13 格是为了以后想切多色时不用重配")]
        public Sprite[] sprites = new Sprite[13];

        [Tooltip("单元块模板（建议 inactive；每个有效交点会 Instantiate 一份副本并激活）")]
        public Transform atomicObject;

        [Tooltip("模板上的 SpriteRenderer（副本据此定位并换 Sprite；可位于模板子级）")]
        public SpriteRenderer atomicSprite;

        [Header("实时 Mesh（displayMode = GeneratedMesh 时生效；距离都是 **Pixel 单位** = unitSize 的倍数）")]
        [Tooltip("**整体外扩**：方阵轮廓先向外扩这么多，再往下做切斜边与顶面内缩 —— 想给冰留一圈富余就靠它（0 = 不扩）")]
        public float meshExpand = 0f;

        [Tooltip("整块 Mesh 在 **y 方向**上的偏移（底面默认在 y=0，正数抬高）")]
        public float meshBaseY = 0f;

        [Tooltip("底部多边形：**90° 凸角**（2×2 里占 1 格）的 45° 内切距离 —— 沿两条边各取这么多，把角切掉")]
        public float meshCorner90Inset = 0.3f;

        [Tooltip("底部多边形：**270° 凹角**（2×2 里占 3 格）的 45° 外切距离 —— 沿两条边各取这么多，把缺口补上")]
        public float meshCorner270Outset = 0.3f;

        [Tooltip("侧面棱柱高度：底部多边形垂直向上延展这么多")]
        public float meshHeight = 1f;

        [Tooltip("顶面相对「方阵轮廓」的内缩距离（先内缩、再切下面两个斜边）")]
        public float meshTopInset = 0.15f;

        [Tooltip("顶面：90° 凸角内切距离")]
        public float meshTopCorner90Inset = 0.3f;

        [Tooltip("顶面：270° 凹角外切距离")]
        public float meshTopCorner270Outset = 0.3f;

        [Tooltip("顶面相对侧面顶部的**上方 y 偏移**：连接顶面与侧面的斜面高度（0 = 一圈平沿）")]
        public float meshTopYOffset = 0.2f;

        [Tooltip("实时 Mesh 的材质（不生成 UV，用纯色 / 无贴图材质）。两个现成的可选：" +
                 "Assets/Shaders/UnlitColorTransparent.shader（纯色半透）、" +
                 "UnlitColorRimTransparent.shader（同上 + 边缘高光）。留空则用渲染器默认材质（白模）")]
        public Material meshMaterial;

        [Tooltip("冰冻计数数字（UI Text，留空自动从子物体查找）。它的**父物体**会被当作锚点，见 countOffset")]
        public Text countText;

        [Tooltip("计数数字的偏移：锚点世界坐标 = 冰组**包围矩形的中心**（两端格中心的中点）+ 本偏移。\n" +
                 "改的是 Text **父物体**的世界坐标；在编辑器里拖这个值可实时预览（Unity 改字段会调 OnValidate 刷新）")]
        public Vector3 countOffset = DefaultCountOffset;

        /// <summary>新建冰组时计数数字的默认偏移（锚点 = 包围矩形中心）。在 <see cref="PixelGroup.SpawnIce"/> 里写入，
        /// 所以编辑器菜单创建与关卡 JSON 导入两条路径拿到的默认值一致，且不受预制体上旧序列化值影响。</summary>
        public static readonly Vector3 DefaultCountOffset = new Vector3(0f, 2f, -0.55f);

        [Tooltip("计数数字的放大倍数（默认 1）：按初始化时记录的**字号**与**长宽**等比放大。" +
                 "在编辑器里改这个值实时预览；改了预制体里 Text 的原始字号 / 尺寸后，用 Inspector 的「重新记录计数文字基准」按钮重录")]
        [Min(0.01f)]
        public float countFontScale = 1f;

        // ===== 计数文字的基准（初始化时记录一次，之后按 countFontScale 缩放）=====
        // 必须**序列化**：它记录的是「原始值」，而应用缩放会就地改掉 Text 上的字号 / 尺寸。
        // 若只放在 NonSerialized 字段里，编辑器一次脚本重载就会把「已放大后的值」当成原始值，
        // 再乘一次 → 越拖越大。
        [SerializeField, HideInInspector] private bool _textMetricsRecorded;
        [SerializeField, HideInInspector] private int _baseFontSize;
        [SerializeField, HideInInspector] private Vector2 _baseSize;

        [Header("Gizmos")]
        [Tooltip("冰组格的颜色")]
        public Color gizmoColor = new Color(0.55f, 0.9f, 1f, 0.55f);

        /// <summary>所属 PixelGroup（由 PixelGroup.RebuildGrid 赋值，不序列化）。</summary>
        [System.NonSerialized] public PixelGroup group;

        /// <summary>运行期剩余计数（不序列化，所以融化不会污染关卡 JSON）；-1 = 尚未初始化。</summary>
        [System.NonSerialized] public int remaining = -1;

        /// <summary>组内是否有「按正常规则暴露」的像素（由 PixelGroup.RefreshExposed 填，忽略冰冻覆盖）。</summary>
        [System.NonSerialized] public bool hasExposedMember;

        /// <summary>
        /// 「这次点击发生**之前**」本组是否已暴露。由 <see cref="PixelGroup.CaptureIceExposedSnapshot"/> 记录，
        /// 供「暴露才开始融化」判断 —— 用点击前的状态，就顺带实现了「使之暴露的那次点击不计数」。
        /// </summary>
        [System.NonSerialized] public bool exposedAtCapture;

        /// <summary>已生成的单元块副本。</summary>
        [System.NonSerialized] private readonly List<GameObject> _spawned = new List<GameObject>();

        /// <summary>实时 Mesh 模式的子物体（带 MeshFilter / MeshRenderer；Sprite 模式下为 null）。</summary>
        [System.NonSerialized] private GameObject _meshObject;

        /// <summary>实时 Mesh 模式生成的 Mesh（Clear 时一并销毁，避免泄漏）。</summary>
        [System.NonSerialized] private Mesh _mesh;

        /// <summary>最近一次生成 Mesh 时的提示（几何被夹紧 / 孔洞被填实等），Inspector 显示用。</summary>
        [System.NonSerialized] private string _meshWarning;

        /// <summary>去重后的成员格集合（由 <see cref="RefreshCells"/> 从 <see cref="cells"/> 算出）。</summary>
        [System.NonSerialized] private readonly HashSet<Vector2Int> _cellSet = new HashSet<Vector2Int>();

        [System.NonSerialized] private string _atomicSpritePath;
        [System.NonSerialized] private bool _pathCached;

        [System.NonSerialized] private int _colMin, _rowMin, _colMax, _rowMax;

        [System.NonSerialized] private PixelGroup _gizmoGroup;

        /// <summary>计数是否已归 0（冰已化开）。未初始化（-1）时视为「未融化」，即保守地保持冰冻。</summary>
        public bool Melted => remaining == 0;

        /// <summary>成员格数。</summary>
        public int CellCount => _cellSet.Count;

        /// <summary>成员格集合（只读）。</summary>
        public IReadOnlyCollection<Vector2Int> CellSet => _cellSet;

        /// <summary>所属 PixelGroup（编辑器 Gizmos / Inspector 用，惰性缓存）。</summary>
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
            if (countText == null)
                countText = GetComponentInChildren<Text>(true);
            RefreshCells();
        }

        /// <summary>按当前 <see cref="cells"/> 重算成员格集合与包围盒（字段被 Inspector 改动后由 RebuildGrid / 编辑器调用）。</summary>
        public void RefreshCells()
        {
            _cellSet.Clear();
            for (int i = 0; i < cells.Count; i++)
                _cellSet.Add(IceRegion.ToCell(cells[i]));
            RecomputeBounds();
        }

        private void RecomputeBounds()
        {
            _colMin = int.MaxValue;
            _rowMin = int.MaxValue;
            _colMax = int.MinValue;
            _rowMax = int.MinValue;
            foreach (var c in _cellSet)
            {
                if (c.x < _colMin) _colMin = c.x;
                if (c.x > _colMax) _colMax = c.x;
                if (c.y < _rowMin) _rowMin = c.y;
                if (c.y > _rowMax) _rowMax = c.y;
            }
        }

        /// <summary>该格是否是本冰组的成员。</summary>
        public bool IsCell(int col, int row)
        {
            return _cellSet.Contains(new Vector2Int(col, row));
        }

        /// <summary>把运行期计数复位到 <see cref="freezeCount"/>（进关卡 / 重建网格时调用）。</summary>
        public void ResetCount()
        {
            remaining = Mathf.Max(1, freezeCount);
        }

        /// <summary>
        /// 计数 -1。返回**是否正好在本帧融化归 0**（只有真融化才需要重建冰面与暴露）。
        /// 「暴露才开始融化」的门槛**不在这里** —— 那要用「点击前」的暴露状态判断，
        /// 由 <see cref="PixelGroup.NotifyClickMovedOut"/> 统一把关（见那里的注释）。
        /// </summary>
        public bool ConsumeOne()
        {
            if (remaining < 0)
                ResetCount();
            if (remaining <= 0)
                return false;

            remaining--;
            return remaining == 0;
        }

        /// <summary>
        /// 摆放可见表现：按 <see cref="displayMode"/> 走四角拼接 Sprite 或实时 Mesh，并把计数数字放到
        /// 冰组包围矩形中心 + 偏移。已融化时只清空、不生成。编辑器改字段后与运行时重建走的是同一个方法。
        /// </summary>
        public void BuildVisual(PixelGroup pg)
        {
            if (pg != null)
                group = pg;
            if (group == null)
                group = GetComponentInParent<PixelGroup>();

            RefreshCells();
            Clear();

            if (countText == null)
                countText = GetComponentInChildren<Text>(true);

            if (group == null || _cellSet.Count == 0)
            {
                UpdateDisplay();
                return;
            }

            if (Melted)
            {
                UpdateDisplay();
                return;
            }

            if (displayMode == IceDisplayMode.GeneratedMesh)
            {
                if (atomicObject != null)
                    atomicObject.gameObject.SetActive(false);   // 模板别露出来（与 Sprite 模式一致）
                BuildMeshVisual();
                UpdateDisplay();
                return;
            }

            if (atomicObject == null)
            {
                UpdateDisplay();
                return;
            }

            CacheAtomicSpritePath();
            atomicObject.gameObject.SetActive(false);   // 隐藏模板

            // 单色填充 + **每个冰组独立考虑**：只把**本组**的成员格当成激活，别的冰组与本组无关
            // （交界处本组自己收边、倒圆角），所以不需要任何全局变体图，也不需要「哪个组负责这个交点」的仲裁。
            // 四角按顺时针环序 TL, TR, BR, BL。只遍历本组包围盒 ±1 的交点范围。
            for (int col = _colMin; col <= _colMax + 1; col++)
            {
                for (int row = _rowMin; row <= _rowMax + 1; row++)
                {
                    int key = CornerTileKey.Make(
                        Variant(col - 1, row - 1),   // TL
                        Variant(col, row - 1),       // TR
                        Variant(col, row),           // BR
                        Variant(col - 1, row),       // BL
                        CornerTileStates.Single);

                    var tile = CornerTileTable.Resolve(key, CornerTileStyle.Fill);
                    if (tile.spawned)
                        Spawn(col, row, tile.tileId, tile.rotation);
                }
            }

            UpdateDisplay();
        }

        /// <summary>
        /// 实时 Mesh 表现：按 <see cref="IceMeshBuilder"/> 生成一块 Mesh，挂在子物体上。
        /// 子物体摆在**与 PixelGroup 同一个局部空间**（冰组预制体一般就在组原点，这里兜住不在原点的情况），
        /// 所以 Mesh 与四角拼接 Sprite 落在同一个位置。
        /// 编辑器下生成物打 <see cref="HideFlags.DontSave"/>，不随场景保存（运行时由 BuildVisual 重新生成）。
        /// </summary>
        public void BuildMeshVisual()
        {
            if (group == null)
                return;

            float unit = group.unitSize;
            var settings = new IceMeshBuilder.Settings
            {
                expand = Mathf.Max(0f, meshExpand) * unit,
                baseY = meshBaseY * unit,
                corner90Inset = Mathf.Max(0f, meshCorner90Inset) * unit,
                corner270Outset = Mathf.Max(0f, meshCorner270Outset) * unit,
                height = Mathf.Max(0f, meshHeight) * unit,
                topInset = Mathf.Max(0f, meshTopInset) * unit,
                topCorner90Inset = Mathf.Max(0f, meshTopCorner90Inset) * unit,
                topCorner270Outset = Mathf.Max(0f, meshTopCorner270Outset) * unit,
                topYOffset = meshTopYOffset * unit,
            };

            Mesh mesh = IceMeshBuilder.Build(_cellSet, group.columns, group.TotalRows,
                group.CellSizeX, group.CellSizeZ, settings, out string warn);
            _meshWarning = warn;
            if (mesh == null)
            {
                if (!string.IsNullOrEmpty(warn))
                    Debug.LogWarning("[IceItem] 实时 Mesh 未生成：" + warn, this);
                return;
            }

            var go = new GameObject("IceMesh");
            go.transform.SetParent(transform, false);
            // 摆到 PixelGroup 的原点：Mesh 顶点按组局部坐标算，这样与 Sprite 模式的单元块重合
            go.transform.position = group.transform.position;
            go.transform.rotation = group.transform.rotation;
            go.transform.localScale = Vector3.one;

            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var meshRenderer = go.AddComponent<MeshRenderer>();
            if (meshMaterial != null)
                meshRenderer.sharedMaterial = meshMaterial;

            if (!Application.isPlaying)
            {
                go.hideFlags = HideFlags.DontSave;
                mesh.hideFlags = HideFlags.DontSave;
            }

            _meshObject = go;
            _mesh = mesh;
        }

        /// <summary>最近一次生成 Mesh 时的提示（几何被夹紧 / 孔洞被填实等）；null = 无。</summary>
        public string MeshWarning => _meshWarning;

        /// <summary>实时 Mesh 的顶点数（没生成时为 0）。</summary>
        public int MeshVertexCount => _mesh != null ? _mesh.vertexCount : 0;

        /// <summary>实时 Mesh 的三角数（没生成时为 0）。</summary>
        public int MeshTriangleCount => _mesh != null ? _mesh.triangles.Length / 3 : 0;

        /// <summary>
        /// 本组的角身份：是本组格 → 0（单色下具体取值无意义），否则 -1（不激活）。
        /// 越界格一律算不激活 —— 冰组里若不小心留了网格外的格，不至于把图铺到网格外面去。
        /// </summary>
        private int Variant(int col, int row)
        {
            if (group == null || !group.IsInRange(col, row))
                return CornerTileKey.None;
            return _cellSet.Contains(new Vector2Int(col, row)) ? 0 : CornerTileKey.None;
        }

        /// <summary>销毁所有已生成的单元块副本与实时 Mesh（子物体 + Mesh 本身，避免泄漏）。</summary>
        public void Clear()
        {
            for (int i = _spawned.Count - 1; i >= 0; i--)
            {
                var go = _spawned[i];
                if (go == null)
                    continue;
                if (Application.isPlaying)
                    Destroy(go);
                else
                    DestroyImmediate(go);
            }
            _spawned.Clear();

            if (_meshObject != null)
            {
                if (Application.isPlaying)
                    Destroy(_meshObject);
                else
                    DestroyImmediate(_meshObject);
                _meshObject = null;
            }
            if (_mesh != null)
            {
                if (Application.isPlaying)
                    Destroy(_mesh);
                else
                    DestroyImmediate(_mesh);
                _mesh = null;
            }
        }

        /// <summary>
        /// 刷新计数数字：位置（冰组**包围矩形中心** + <see cref="countOffset"/>）、字号缩放、文本、以及该不该显示。
        ///
        /// 位置改的是 **Text 父物体**的**世界坐标**（不是 Text 自己的 localPosition）——
        /// 这样预制体里 Text 嵌在 Canvas / 空物体下面也能定位对。父物体就是冰组根时是例外
        /// （预制体里 Text 直接挂根下）：动它会连冰的单元块一起挪走，所以退回改 Text 自己的世界坐标。
        /// </summary>
        public void UpdateDisplay()
        {
            if (countText == null)
                countText = GetComponentInChildren<Text>(true);
            if (countText == null)
                return;

            if (group != null && _cellSet.Count > 0)
            {
                // 锚点 = 包围矩形中心（两端格中心的中点）+ countOffset。
                // 用「中心」而不是「左上角」：冰组形状是任意的，中心更好调；而且这样 countOffset 是与
                // 冰组形状无关的常量，关卡 JSON 导入回来的冰组也能落在同样的相对位置。
                Vector3 centre = (group.GetWorldPosition(_colMin, _rowMin) +
                                  group.GetWorldPosition(_colMax, _rowMax)) * 0.5f;
                Vector3 world = centre + countOffset;

                Transform anchor = countText.transform.parent;
                if (anchor != null && anchor != transform)
                    anchor.position = world;
                else
                    countText.transform.position = world;
            }

            ApplyTextScale();   // 字号与长宽按 countFontScale 等比缩放

            // 编辑期一律显示 —— 调 countOffset 时直接看得到效果；此时显示的是待机值（freezeCount）
            // 而不是运行期计数，免得看到个没意义的 0。
            bool show = !Application.isPlaying
                || (!Melted && (!meltOnlyWhenExposed || hasExposedMember));
            countText.enabled = show;
            if (show)
                countText.text = (remaining < 0 ? Mathf.Max(1, freezeCount) : remaining).ToString();
        }

        /// <summary>
        /// 记录计数数字的**原始**字号与长宽，只记一次。之后所有缩放都以这份值为基准 ——
        /// 所以重复调用是幂等的，不会越缩越大。
        /// </summary>
        private void CacheTextMetrics()
        {
            if (_textMetricsRecorded || countText == null)
                return;

            _baseFontSize = countText.fontSize;
            RectTransform rt = countText.rectTransform;
            _baseSize = rt != null ? new Vector2(rt.rect.width, rt.rect.height) : Vector2.zero;
            _textMetricsRecorded = true;
        }

        /// <summary>按 <see cref="countFontScale"/> 等比放大字号与长宽（倍数 = 1 时就是记录的原值）。</summary>
        private void ApplyTextScale()
        {
            if (countText == null)
                return;

            CacheTextMetrics();

            float k = Mathf.Max(0.01f, countFontScale);
            countText.fontSize = Mathf.Max(1, Mathf.RoundToInt(_baseFontSize * k));

            RectTransform rt = countText.rectTransform;
            if (rt == null)
                return;

            // 用 SetSizeWithCurrentAnchors 而不是直接乘 sizeDelta：锚点被拉伸时 sizeDelta 表示的是
            // 「相对锚点框的增量」，乘它会算错；按当前锚点设**绝对**尺寸对两种锚点都成立。
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _baseSize.x * k);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _baseSize.y * k);
        }

        /// <summary>
        /// 重新记录基准（Inspector 上有按钮）。注意它记录的是 Text **当前**的字号 / 长宽，
        /// 而这些值平时是被本组件按倍数覆盖着的 —— 所以想换基准时：先把 <see cref="countFontScale"/>
        /// 设回 1、再改 Text 上的原始值、然后按这个按钮。
        /// </summary>
        public void RecacheTextMetrics()
        {
            _textMetricsRecorded = false;
            if (countText == null)
                countText = GetComponentInChildren<Text>(true);
            CacheTextMetrics();
            ApplyTextScale();
        }

        /// <summary>已记录的字号 / 长宽基准；<paramref name="recorded"/> = false 表示还没记录过。</summary>
        public void GetTextMetrics(out bool recorded, out int fontSize, out Vector2 size)
        {
            recorded = _textMetricsRecorded;
            fontSize = _baseFontSize;
            size = _baseSize;
        }

        /// <summary>
        /// 编辑器里改 <see cref="cells"/> / <see cref="countOffset"/> / <see cref="freezeCount"/> 等字段时
        /// Unity 会调这里 → 立刻刷新，于是拖 <see cref="countOffset"/> 能实时看到数字位置（实时预览）。
        /// </summary>
        private void OnValidate()
        {
            RefreshCells();
            if (countText == null)
                countText = GetComponentInChildren<Text>(true);
            UpdateDisplay();
        }

        private void Spawn(int col, int row, int tileId, int rotation)
        {
            var copy = Instantiate(atomicObject, transform, false);
            copy.name = "Ice_" + row + "_" + col;
            copy.localPosition = CornerLocalPosition(col, row);
            // 环位由表给出（rotation × 90°），基准姿态由模板自己的 localRotation 承担 —— 两者相乘即可，
            // 这里**不加任何基准角常量**（见类注释里的警告）。
            copy.localRotation = Quaternion.Euler(0f, rotation * 90f, 0f) * atomicObject.localRotation;

            // 编辑器里构建的副本不随场景保存（运行期由 BuildVisual 重新生成）
            if (!Application.isPlaying)
                copy.gameObject.hideFlags = HideFlags.DontSave;

            var sr = ResolveSpriteRenderer(copy);
            if (sr != null && tileId >= 0 && tileId < sprites.Length && sprites[tileId] != null)
                sr.sprite = sprites[tileId];

            copy.gameObject.SetActive(true);
            _spawned.Add(copy.gameObject);
        }

        /// <summary>
        /// 交点（单元块中心）局部坐标：x=(col−columns/2)·CellSizeX，z=−(row−0.5)·CellSizeZ。
        /// 与 <see cref="IceMeshBuilder.LatticeXZ"/> 共用同一个算法 —— 两种显示模式必须落在同一位置。
        /// </summary>
        private Vector3 CornerLocalPosition(int col, int row)
        {
            Vector2 xz = IceMeshBuilder.LatticeXZ(col, row, group.columns, group.CellSizeX, group.CellSizeZ);
            return new Vector3(xz.x, 0f, xz.y);
        }

        private void CacheAtomicSpritePath()
        {
            if (_pathCached)
                return;
            _pathCached = true;
            _atomicSpritePath = string.Empty;
            if (atomicObject == null || atomicSprite == null)
                return;
            _atomicSpritePath = BuildRelativePath(atomicSprite.transform, atomicObject);
        }

        private static string BuildRelativePath(Transform target, Transform root)
        {
            if (target == root)
                return string.Empty;
            if (target == null || target.parent == null)
                return null;
            string parent = BuildRelativePath(target.parent, root);
            if (parent == null)
                return null;
            return string.IsNullOrEmpty(parent) ? target.name : parent + "/" + target.name;
        }

        private SpriteRenderer ResolveSpriteRenderer(Transform copy)
        {
            if (string.IsNullOrEmpty(_atomicSpritePath))
                return copy.GetComponent<SpriteRenderer>();
            var t = copy.Find(_atomicSpritePath);
            return t != null ? t.GetComponent<SpriteRenderer>() : null;
        }

        /// <summary>编辑器 / 运行时的可视化：冰组格（扁平立方体）+ 计数锚点，一眼看出范围与锚点。</summary>
        private void OnDrawGizmos()
        {
            // 编辑器下 Awake 不跑、Inspector 改格列表也不会通知这里，所以每次重画都重算一遍
            // （同 WallItem.OnDrawGizmos 的做法；格数很小，开销可忽略）
            RefreshCells();

            var pg = Group;
            if (pg == null || _cellSet.Count == 0)
                return;

            Color prev = Gizmos.color;

            Gizmos.color = gizmoColor;
            var cellSize = new Vector3(pg.CellSizeX * 0.9f, 0.04f, pg.CellSizeZ * 0.9f);
            foreach (var cell in _cellSet)
            {
                if (!pg.IsInRange(cell.x, cell.y))
                    continue;
                Gizmos.DrawCube(pg.GetWorldPosition(cell.x, cell.y), cellSize);
            }

            // 计数数字的锚点（包围矩形中心 + 偏移）
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
            Vector3 centre = (pg.GetWorldPosition(_colMin, _rowMin) + pg.GetWorldPosition(_colMax, _rowMax)) * 0.5f;
            Gizmos.DrawWireSphere(centre + countOffset, pg.unitSize * 0.12f);

            Gizmos.color = prev;
        }
    }
}
