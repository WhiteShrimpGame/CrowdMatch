using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CrowdMatch
{
    /// <summary>
    /// 全局单例，负责点击匹配与聚集逻辑。
    /// 点击一个 PixelItem 后，连同相邻同色单位一起离开：只要该同色组能通过空/组内格连通到首排（row 0）即可点击；
    /// 像素离开后，后方像素不再补位（网格保持空位，空位随匹配逐步累积）。
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public class GameController : MonoBehaviour
    {
        public static GameController Instance { get; private set; }

        [Header("引用")]
        [Tooltip("聚集点，被匹配的单位会移动到这里")]
        public Transform gatherPoint;

        [Tooltip("显示聚集点单位数量的 UI 文本")]
        public Text gatherCountText;

        /*[Tooltip("聚集数量的机械卷轴（十位 + 个位，0~30）；留空则只用上面的文本。" +
                 "挂上后只在数值变化时驱动它，切关时直接落位不滚")]
        public DoubleDigitRoller gatherCountRoller;*/

        [Tooltip("实时显示关卡进度的 UI 文本（形如 45%）；口径与复活 / 失败面板的进度条完全一致，见 GameData.ProgressPercent")]
        public Text progressText;

        [Tooltip("显示当前关卡文本（如「北京」）的 UI 文本；留空则不更新。文本取自 GameManager.levelDataConfig 的 levelTexts / levelTextsLoop")]
        public Text levelNameText;

        [Header("关卡文本入场动画")]
        [Tooltip("入场动画作用的 3D 物体；留空则用 levelNameText 的父物体的父物体（即祖父物体）")]
        public Transform levelNameTextRoot;

        [Tooltip("切关后等多久才开始播入场动画（秒）；0 = 立即开始")]
        public float levelTextIntroDelay = 0.1f;

        [Tooltip("父物体 y 上升 1 单位的时长（秒）")]
        public float levelTextRiseDuration = 0.3f;

        [Tooltip("父物体自转 3 圈的时长（秒）")]
        public float levelTextRotateDuration = 0.6f;

        [Tooltip("父物体自转的缓动；慢快慢要明显就选更陡的 InOutQuad / InOutCubic / InOutQuart（InOutSine 最平）")]
        public Ease levelTextRotateEase = Ease.InOutCubic;

        [Tooltip("父物体 y 落下 1 单位的时长（秒）")]
        public float levelTextFallDuration = 0.3f;

        [Tooltip("管理的 PixelGroup，留空会自动查找")]
        public PixelGroup pixelGroup;

        [Tooltip("管理的 ContainerGroup，留空会自动查找")]
        public ContainerGroup containerGroup;

        [Tooltip("整体描边 FrameItem（可选）；留空则不做描边，暴露状态刷新后会自动重建")]
        public FrameItem frameItem;

        [Header("速度")]
        [Tooltip("单位向聚集点移动的速度（世界单位/秒）")]
        public float gatherSpeed = 12f;

        [Header("聚集表现")]
        [Tooltip("单位到达聚集点后的散布半径，避免完全重叠")]
        public float gatherScatterRadius = 0.35f;

        [Header("点击无效反馈")]
        [Tooltip("点击无法移出的像素时，被点像素与相连同色像素一起向前（本地 +Z）晃出的距离（世界单位）")]
        public float blockedNudgeDistance = 0.25f;

        [Tooltip("晃动单程时长（秒）；去与回同速，故两段时长相同")]
        public float blockedNudgeDuration = 0.08f;

        /// <summary>点击序号发号器：每次成功点击移出递增一次，同一次移出的整组共用同一个序号（供传送带入口的插队判定）。</summary>
        private int _clickSeq;

        [Header("过闸缓冲区（可选）")]
        [Tooltip("像素离开网格后进入的扇形缓冲区；留空则回退到旧的直接散布聚集")]
        public CrowdBufferZone crowdBuffer;

        [Header("传送带（可选）")]
        [Tooltip("释放后像素进入的闭环传送带；留空则显示 gatheredItems 计数")]
        public ConveyorBeltZone conveyorZone;

        [Header("Record 模式")]
        [Tooltip("勾选后运行时新建序列文件；点击后像素原地消失并把颜色写入文件（不进入缓冲区/传送带），且允许点击被阻挡的组。按住 S 点击则只移除被点的那一颗，不整组移出")]
        public bool recordMode = false;

        [Tooltip("序列文件输出目录；留空使用工程目录下的 Record 文件夹（编辑器），构建时回退 Application.persistentDataPath")]
        public string recordOutputDir = "";

        [Header("复活")]
        [Tooltip("复活时在传送带上保留的像素数量（其余溢出像素直接匹配后排车）")]
        public int reviveKeepBeltCount = 10;

        [Tooltip("复活跳跃组每颗之间起播的固定间隔（秒）。0 = 同帧全部起播；消失组不走间隔，在 t=0 一次性播完")]
        public float reviveInterval = 0.06f;

        [Header("复活表现 · 单颗时长（只影响复活，不动正常玩法）")]
        [Tooltip("消失·放大阶段时长（秒）—— 只覆盖复活调用的 DisappearWithPop，不动开盖 / 木箱摘封条")]
        public float reviveDisappearPopDuration = 0.2f;

        [Tooltip("消失·缩小阶段时长（秒）—— 同上")]
        public float reviveDisappearShrinkDuration = 0.2f;

        [Tooltip("跳跃上车时长（秒）—— 只覆盖复活，不动车预制体上的 boardJumpDuration")]
        public float reviveJumpDuration = 0.35f;

        [Tooltip("落地弹性放大时长（秒）—— 只覆盖复活")]
        public float reviveElasticScaleDuration = 0.1f;

        [Tooltip("弹性复原时长（秒）—— 只覆盖复活")]
        public float reviveElasticRecoverDuration = 0.15f;

        [Header("堆积")]
        [Tooltip("堆积进入限制：累计 2 次后，未进传送带球数小于等于此值仍放行点击")]
        public int overflowPendingLimit = 9;

        [Header("调试")]
        [Tooltip("开启后打印每次点击的判定结果（提取中忽略 / 射线未命中 / 无点击体 / 已移出网格 / 无法连通首排 / 命中成功），用于定位「起身时点击不到」")]
        public bool debugClickLog = true;

        [Tooltip("开启后打印每次失败判定**没判失败**的结果（检查点 / 被哪条门禁挡下 / 传送带占用），" +
                 "用于定位「该判失败却不判、关卡卡住」")]
        public bool debugFailLog = true;

        /// <summary>处于聚集点中的单位</summary>
        public List<PixelItem> gatheredItems = new List<PixelItem>();

        private StreamWriter _recordWriter;
        private string _recordFilePath;   // 当前记录文件路径（关闭时会改名，只在写入期间有效）
        private string _recordFileBase;   // 记录文件名前缀（不含 _rec<N> 与扩展名）
        private int _recordedCount;       // 当前记录文件已写入的像素数
        private bool _transitioning;

        /// <summary>本次复活的**跳跃**队列（匹配与座位都已定好，只等按次序起播）；播完或被打断时置空。</summary>
        private List<ContainerGroup.BoardingEntry> _reviveJumpQueue;

        /// <summary>复活跳跃队列正在播放：期间挡住失败判定（见 <see cref="TryCheckFail"/>）。</summary>
        private bool _revivePlaying;

        /// <summary>本关是否已经宣告过胜利。胜利是事件驱动的，用它防重入；进关时复位（见 <see cref="InitLevel"/>）。</summary>
        private bool _winDeclared;

        /// <summary>上一条失败判定诊断行：内容完全相同时不重复打印（复活期间会有几十次上车回调，行内容一模一样）。</summary>
        private string _lastFailCheckLog;

        /// <summary>堆积进入限制：in-flight（带 + 已点未进带）达容量后的累计点击次数；总数低于容量时重置。</summary>
        private int _overflowClickCount;

        /// <summary>点击射线检测使用的层遮罩（「Click」层）。</summary>
        private int _clickMask;

        /// <summary>道具「强制取出」模式：开启后点一组即可无视前排连通限制送出（见 <see cref="EnterPropForceMode"/>）。</summary>
        private bool _propForceMode;

        /// <summary>当前是否处于道具「强制取出」模式。</summary>
        public bool PropForceMode { get { return _propForceMode; } }

        [Header("道具3「UFO」演出")]
        [Tooltip("UFO 预制体（Assets/Prefabs/UFO.prefab）。留空则道具3 退化为「这组原地消失」并报错。")]
        public GameObject prop3UfoPrefab;

        [Tooltip("出场位置：屏幕下方（视口 y，负数 = 屏幕下方之外）")]
        public float prop3EnterViewportY = -0.25f;

        [Tooltip("离场位置：屏幕上方（视口 y，>1 = 屏幕上方之外）")]
        public float prop3ExitViewportY = 1.3f;

        [Tooltip("UFO 飞行时长（出场 / 到车阵 / 离场，秒）")]
        public float prop3FlyDuration = 0.45f;

        [Tooltip("UFO 停在被点组正上方的高度（世界 Y 偏移）")]
        public float prop3HoverAboveGroup = 4f;

        [Tooltip("UFO 停在前4排车上方的高度（世界 Y 偏移）")]
        public float prop3HoverAboveCars = 5f;

        [Tooltip("吸人：从原格移动到 UFO 并缩到 0 的时长（秒）")]
        public float prop3SuckDuration = 0.25f;

        [Tooltip("吸人：逐颗错开的间隔（秒）。只影响「一颗接一颗」的节奏。")]
        public float prop3SuckStagger = 0.05f;

        [Tooltip("UFO 缩放脉冲的间隔（秒）：吸人期间**每这么久缩一次**（基准 → 基准×倍率 → 基准），" +
                 "一直重复到人吸完。")]
        public float prop3PulseInterval = 0.15f;

        [Tooltip("吐出（跳车）：逐颗错开间隔（秒）")]
        public float prop3SpitInterval = 0.06f;

        [Tooltip("UFO 自转一圈的时长（秒）；<=0 关掉自转。全程沿自身 Y 轴转，不重置预制体自己的朝向。")]
        public float prop3SpinSeconds = 1.5f;

        [Tooltip("出场到「人上方」这段里，把 UFO 自身 z 滚转逐渐归 0 的时长（秒）。默认与飞行时长一致。")]
        public float prop3LevelDuration = 0.45f;

        [Tooltip("每吸一个人，UFO 的缩放脉冲倍率（基准 × 该值 → 回基准）。1.05 = 预制体 scale 2 → 2.1 → 2。")]
        public float prop3GulpPulse = 1.05f;

        [Tooltip("吸人的终点：UFO 本地坐标。人挂到 UFO 下后逐渐移到这个点（默认 (0,-0.5,0)）。")]
        public Vector3 prop3SuckLocalTarget = new Vector3(0f, -0.5f, 0f);

        /// <summary>道具3「UFO」演出进行中：期间锁点击、锁再点道具。换关时由 CleanupLevel 复位。</summary>
        private bool _prop3Playing;

        /// <summary>
        /// **仅吸人阶段**为 true：管道（逐帧判定）靠它暂停投波，
        /// 免得"人还没吸完、管道里的蛇就先冒出来"。吸完立刻置 false —— 只卡这一段，不卡整场演出。
        /// 换关时由 CleanupLevel 复位（协程被 StopAllCoroutines 中止时不走收尾，必须兜底）。
        /// </summary>
        private bool _prop3SuckPhase;

        /// <summary>道具3 是否正处于「吸人」阶段（管道据此暂停投波）。</summary>
        public bool Prop3SuckPhase { get { return _prop3SuckPhase; } }

        /// <summary>当前在场的 UFO（演出中）；停协程时靠它兜底销毁，避免残留。</summary>
        private GameObject _prop3Ufo;

        /// <summary>
        /// 挂在 UFO 下的人 + **入舱瞬间的世界朝向**。UFO 全程在自转，靠每帧把世界朝向压回这个冻结值，
        /// 让人只跟着 UFO 平移、不跟着转（见 <see cref="Prop3LevelAndSpin"/>）。
        /// </summary>
        private readonly List<Prop3Rider> _prop3Riders = new List<Prop3Rider>();

        private sealed class Prop3Rider
        {
            public PixelItem pixel;
            public Quaternion worldRotation;
        }

        /// <summary>道具3「UFO」演出是否进行中。</summary>
        public bool Prop3Playing { get { return _prop3Playing; } }

        /// <summary>
        /// 4 邻偏移（+x / −x / +z / −z）。**必须是 `static readonly` 字段**：
        /// 写成方法内的 `int[] dx = {…}` 会**每次调用都 `newarr` 分配**
        /// （实测：Roslyn 的 blob 缓存只对静态字段的常量初始化生效，对局部字面量不生效）。
        /// 顺序不要重排 —— BFS 的访问顺序决定了返回列表的元素顺序，下游的动画次序依赖它。
        /// </summary>
        private static readonly int[] Dx4 = { 1, -1, 0, 0 };
        private static readonly int[] Dz4 = { 0, 0, 1, -1 };

        /// <summary>关卡文本入场动画的父物体（3D 物体）；懒解析一次，见 <see cref="CacheLevelTextRoot"/>。</summary>
        private Transform _levelTextRoot;

        /// <summary>父物体的初始位姿：每次起播前复位到它，避免上一次动画被杀在半空时从错的位置起播。</summary>
        private Vector3 _levelTextRootBasePos;
        private Quaternion _levelTextRootBaseRot;
        private bool _levelTextRootCached;

        /// <summary>正在播放的入场序列；重入时先 Kill，避免切关时两套动画同时改同一个 Transform。</summary>
        private Sequence _levelTextSeq;

        /// <summary>上一次写进聚集数量滚轮的值。滚轮必须只在数值变化时驱动：每帧都调 SetTargetNumber 会每帧
        /// Kill 上一段 tween 再重开，轮子永远滚不到终点，只剩原地抖。</summary>
        private int _lastGatherRollerCount;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // 文本不再在这里隐藏：入场动画一开头就要显示「上一关的文本」（见 PlayLevelTextIntro）。
            // 想恢复「动画跑完才出现」的旧行为，把下面这段注释放开、并在动画末尾再 SetActive(true)。
            /*if (levelNameText != null)
                levelNameText.gameObject.SetActive(false);*/
            CacheLevelTextRoot();
        }

        private void Start()
        {
            if (pixelGroup == null)
                pixelGroup = FindObjectOfType<PixelGroup>();
            if (containerGroup == null)
                containerGroup = FindObjectOfType<ContainerGroup>();
            if (frameItem == null)
                frameItem = FindObjectOfType<FrameItem>();

            _clickMask = LayerMask.GetMask("Click");

            // 网格内寻路全部结束时的补一次失败检测（网格里还有像素时 IsFail 会跳过，见 IsFail 的静止门槛）
            // 用独立的方法而不是 `+= TryCheckFail`：TryCheckFail 带检查点名参数，且 OnDestroy 的 -= 必须是同一个委托
            if (crowdBuffer != null)
                crowdBuffer.OnGridPathfindingFinished += OnGridPathfindingFinished;

            Init();
        }

        // ===== 关卡流程（初始化 / 胜负检测 / 重载） =====

        /// <summary>进入游玩模式并加载当前关卡。</summary>
        private void Init()
        {
            GameState.GameStart();
            InitLevel(GameData.CurrentLevel);
        }

        /// <summary>按关卡序号加载并应用关卡：清理上一关残留 → 解析 JSON → 应用到两个网格 → 统计像素总数。</summary>
        private void InitLevel(int level)
        {
            CloseRecord();   // 切关：先把上一关的记录文件落盘改名，本关的文件在下面另开
            CleanupLevel();
            _winDeclared = false;   // 新一关：复位胜利宣告标志

            var gm = GameManager.Instance;
            TextAsset json = gm != null ? gm.GetLevelJson(level) : null;
            if (json == null)
            {
                Debug.LogError("[GameController] 找不到第 " + level + " 关的关卡 JSON，无法初始化。");
                return;
            }

            LevelData data = LevelLoader.Parse(json);
            if (data == null)
                return;

            Debug.Log("[GameController] 加载关卡 " + level + "（JSON：" + json.name + "）");
            UIManager.Instance.Init();
#if UNITY_EDITOR
            LevelDataCache.LastInitData = null;   // 清空上次缓存，避免加载失败时残留旧数据
#endif

            // 洗牌：随机打乱容器摆放位置，让每次进关的容器排列不同（锁定的关卡跳过）。
            // 运行时以 JSON 的 lockContainer 为准；它与场景里 ContainerGroup.shuffleContainers 由导出/导入互相同步。
            if (!data.container.lockContainer)
                LevelLoader.ShuffleContainers(data.container);

            // Record 模式：木箱不再遮挡像素（照常显示 / 参与暴露 / 可正常点击，求组限制在同一木箱内）。
            // 必须在 LevelLoader.Apply **之前**写入：那一步会走 RebuildGrid → RefreshCrateState → SetCovered，
            // 而 SetCovered 是幂等的（同值直接返回），事后再改这个开关不会重刷。
            pixelGroup.recordRevealCrates = recordMode;

            // 关卡加载期间抑制「同色连成一片」惊讶表情：随后的 RebuildGrid 与首次 RefreshExposed 会把
            // 开局就贴着首排 / 连着出口空格的问号像素当场揭晓 —— 那不是动态事件，不该撒一片表情。
            // 必须在 LevelLoader.Apply **之前**写入（那一步的 RebuildGrid 与随后的 RefreshExposed 都会读它）。
            pixelGroup.suppressMergeSurprise = true;

            LevelLoader.Apply(pixelGroup, containerGroup, data, gm != null ? gm.colorConfig : null);

            // 建绳必须在 Apply 之后（依赖已重建的网格与车的列位置）；洗牌开启时不建绳、绳组不生效。
            // 绳组成员由 BuildRopes 整组实例化出来（懒实例化下不能只实例化落进视窗的那部分）。
            if (containerGroup != null)
                containerGroup.BuildRopes(!data.container.lockContainer);

            pixelGroup.RefreshExposed();

            // 开局的问号揭晓已经过去了：复位抑制开关，之后的动态事件照常判定
            pixelGroup.suppressMergeSurprise = false;
            RefreshFrame();

#if UNITY_EDITOR
            // 缓存初始化（洗牌后）的关卡数据快照，供编辑器在 Play 模式下导出「锁定」初始状态
            LevelDataCache.LastInitData = JsonUtility.FromJson<LevelData>(JsonUtility.ToJson(data));
#endif

            GameData.Init(true);
            // 总数 = 网格像素 + 箱子隐藏 + 升降台地下 + 管道计划 + 倍乘门额外（= PixelGroup 的规划底座口径，
            // 与 Record 模式按倍率补记的条数一致；少了倍乘额外，文件名里的 total 会小于实际记录条数）
            GameData.TotalPixelCount = CountPixels() + CountPipePixels() + CountGateExtraPixels();
            GameData.ClearedPixelCount = 0;
            GameData.RemovePixelCount = 0;
            if (recordMode)
                BeginRecord(json.name, GameData.TotalPixelCount);   // json.name = 关卡 JSON 文件名
            //更新道具状态
            var gp= UIManager.Instance.gameInnerUI;
            gp.UpdateCurrentButtonInfo();

            RefreshLevelText();

            /*// 聚集数量卷轴：切关直接落位（不放进位/借位动画），否则会从上一关的数字一路滚过来
            if (gatherCountRoller != null)
            {
                _lastGatherRollerCount = CurrentGatherCount();
                gatherCountRoller.SetNumberImmediate(_lastGatherRollerCount);
            }*/
        }

        /// <summary>
        /// 按当前关卡刷新关卡文本：动画开头先显示上一关的文本（第一关为空白），转过 180° 后再换成当前关卡的文本。
        /// </summary>
        private void RefreshLevelText()
        {
            if (levelNameText == null)
                return;

            int level = GameData.CurrentLevel;
            PlayLevelTextIntro(GetLevelText(level), level > 1 ? GetLevelText(level - 1) : string.Empty);
        }

        /// <summary>取指定关卡的文本；关卡配置缺失、没有这一关、或该关没配文本时返回空串。下标口径与关卡 JSON 一致（含循环关）。</summary>
        private string GetLevelText(int level)
        {
            var gm = GameManager.Instance;
            LevelDataConfig config = gm != null ? gm.levelDataConfig : null;
            return config != null ? config.GetLevelText(level) : string.Empty;
        }

        /// <summary>写关卡文本；对象若被别处关掉了顺手打开，避免文本整段不显示。</summary>
        private void SetLevelNameText(string text)
        {
            if (levelNameText == null)
                return;

            if (!levelNameText.gameObject.activeSelf)
                levelNameText.gameObject.SetActive(true);
            levelNameText.text = text ?? string.Empty;
        }

        /// <summary>解析入场动画作用的 3D 物体并记下初始位姿；未指定 <see cref="levelNameTextRoot"/> 时用文本的父物体的父物体。</summary>
        private void CacheLevelTextRoot()
        {
            if (_levelTextRootCached || levelNameText == null)
                return;

            Transform parent = levelNameText.transform.parent;
            _levelTextRoot = levelNameTextRoot != null
                ? levelNameTextRoot
                : (parent != null ? parent.parent : null);
            if (_levelTextRoot == null)
                return;   // 层级不足（没有祖父物体）：无动画可播，也不缓存，留待下次刷新再试

            _levelTextRootBasePos = _levelTextRoot.position;
            _levelTextRootBaseRot = _levelTextRoot.rotation;
            _levelTextRootCached = true;
        }

        /// <summary>
        /// 关卡文本入场：开头先把<b>上一关</b>的文本摆上（第一关为空白），3D 物体（文本的父物体的父物体）
        /// 自转一圈（冲过 420° 再回弹到 360°）；转到 180° 那一刻换成<b>当前关卡</b>的文本 ——
        /// 此时物体正好背对镜头，换字过程看不见。层级不足时不做动画，直接显示当前文本。
        /// </summary>
        private void PlayLevelTextIntro(string currentText, string previousText)
        {
            if (levelNameText == null)
                return;

            // 上一套还没跑完（连续切关 / 连点重载）：先杀掉，否则两套动画会同时改同一个 Transform
            if (_levelTextSeq != null)
            {
                _levelTextSeq.Kill();
                _levelTextSeq = null;
            }

            SetLevelNameText(previousText);

            CacheLevelTextRoot();
            if (_levelTextRoot == null)
            {
                SetLevelNameText(currentText);
                return;
            }

            Transform root = _levelTextRoot;

            // 先清掉挂在这个物体上的其它 tween：上一轮残留或别的脚本的动画会每帧回写 position / rotation，
            // 只做复位不 Kill 的话，复位会在同一帧被覆盖 —— 表现就是「第一次对，切关后不从头开始」
            root.DOKill();

            // 复位到初始位姿（被杀在半空时位置也不对）
            root.SetPositionAndRotation(_levelTextRootBasePos, _levelTextRootBaseRot);

            _levelTextSeq = DOTween.Sequence();
            // 切关后先空等一段再起播；这段时间里文本停在上面的「上一关文本」上
            if (levelTextIntroDelay > 0f)
                _levelTextSeq.AppendInterval(levelTextIntroDelay);
            //_levelTextSeq.Append(root.DOMove(_levelTextRootBasePos + Vector3.up, levelTextRiseDuration));
            // 自转 3 整圈（360° × 3）：LocalAxisAdd 是在自身朝向基础上追加角度，终点朝向与起点一致
            // InOutSine 的峰值速度只有平均速度的 1.57 倍，转 3 圈几乎看不出快慢；默认改用峰值 3 倍的 InOutCubic
            /*_levelTextSeq.Append(root
                .DORotate(new Vector3(0f, 360f, 0f), levelTextRotateDuration, RotateMode.LocalAxisAdd)
                //.SetEase(levelTextRotateEase));
                .SetEase(Ease.OutBack,1.8f));*/

            //_levelTextSeq.Append(root.DOMove(_levelTextRootBasePos, levelTextFallDuration));
            /*_levelTextSeq.OnComplete(() =>
            {
                if (levelNameText != null)
                    levelNameText.gameObject.SetActive(true);
            });*/
            float overshootAngle = 420f;
            float finalTotalAngle = 360f;
            float bounceDelta = finalTotalAngle - overshootAngle; // -40

            float timeGo = levelTextRotateDuration * 0.75f;
            float timeBounce = levelTextRotateDuration * 0.25f;

            _levelTextSeq.Append(
                root.DORotate(new Vector3(0f, overshootAngle, 0f), timeGo, RotateMode.LocalAxisAdd)
                    //.SetEase(Ease.InOutSine)
                    .SetEase(Ease.InOutCirc)
            );
            _levelTextSeq.Append(
                root.DORotate(new Vector3(0f, bounceDelta, 0f), timeBounce, RotateMode.LocalAxisAdd)
                    .SetEase(Ease.OutBack, 1.5f)
            );

            // 转过 180° 的时间点：反解 InOutCirc 前半段（p = (1 − √(1 − 4t²)) / 2）得到
            // t = √(1 − (1 − 2p)²) / 2，其中 p = 180 / 420 ≈ 0.43 < 0.5，正好落在前半段。
            // 换了别的缓动（含改 overshootAngle）这个公式就不再成立，换缓动时记得同步改这里。
            float p180 = 180f / overshootAngle;
            float t180 = Mathf.Sqrt(1f - (1f - 2f * p180) * (1f - 2f * p180)) * 0.5f;
            float rotStart = levelTextIntroDelay > 0f ? levelTextIntroDelay : 0f;
            // 物体背对镜头的那一瞬间换字：换的过程看不见
            _levelTextSeq.InsertCallback(rotStart + timeGo * t180, () => SetLevelNameText(currentText));
        }

        /// <summary>重建整体描边；未使用 FrameItem 时为空操作。</summary>
        private void RefreshFrame()
        {
            if (frameItem == null)
                return;
            frameItem.Build();
        }

        /// <summary>原地重载当前关卡（由 GameManager 在胜负过渡后调用）。</summary>
        public void ReloadLevel()
        {
            _transitioning = false;
            GameState.GameStart();
            InitLevel(GameData.CurrentLevel);
        }

        /// <summary>统计当前网格中的像素总数（仅限在网格范围内的 PixelItem，含箱子尚未释放的隐藏 Pixel）。</summary>
        private int CountPixels()
        {
            if (pixelGroup == null)
                return 0;
            int n = 0;
            foreach (var it in pixelGroup.GetComponentsInChildren<PixelItem>())
            {
                if (it != null && pixelGroup.IsInRange(it.gridX, it.gridZ))
                    n++;
            }
            // 箱子隐藏 Pixel 采用 active=false，GetComponentsInChildren 默认扫不到，需显式累加
            foreach (var box in pixelGroup.GetComponentsInChildren<BoxItem>())
            {
                if (box != null)
                    n += box.hiddenPixels.Count;
            }
            // 升降台地下像素：哨兵坐标 -1,-1 不在网格范围内，需显式累加
            foreach (var elev in pixelGroup.GetComponentsInChildren<ElevatorItem>())
            {
                if (elev != null)
                    n += elev.undergroundPixels.Count;
            }
            return n;
        }

        /// <summary>统计管道将要生成的像素总数（每管 = 轨道格数 × 波次颜色数），计入胜利判定。</summary>
        private int CountPipePixels()
        {
            if (pixelGroup == null)
                return 0;
            int n = 0;
            foreach (var pipe in pixelGroup.GetComponentsInChildren<PipeItem>())
            {
                if (pipe == null || pipe.colors == null)
                    continue;
                int track = pipe.TrackCellCount();
                if (track <= 0)
                    continue;
                n += track * pipe.colors.Count;
            }
            return n;
        }

        /// <summary>
        /// 统计倍乘门额外产生的像素总数（= Σ(所在格倍率 − 1)），供 <see cref="InitLevel"/> 把
        /// <c>GameData.TotalPixelCount</c> 算全 —— 倍乘出来的像素是真实像素、会被真实消费，
        /// Record 模式也按倍率补记了同样多份，总数少算文件名里的 total 就会与 rec 对不上。
        /// （判胜不看这个数：胜利是容器侧事件驱动的，见 <see cref="CheckWin"/>。）
        /// 与容器规划同源（PixelGroup 的同一份底座），口径不会发散。
        /// </summary>
        private int CountGateExtraPixels()
        {
            return pixelGroup != null ? pixelGroup.CountGateExtraPixels() : 0;
        }

        /// <summary>
        /// 胜利检测（**事件驱动**）：只在容器侧的两个时点被调用——某辆车「完成匹配」（最后一颗像素开始上车）、
        /// 某辆车离开盘面（开始倒车出库）。调用方是 <see cref="ContainerGroup.TryCheckWin"/>，它带一层 O(1) 过滤
        /// 与一次全盘复核。
        ///
        /// 口径：**板上不存在「未完成匹配」的车**。触发后等待 2.5s 进入下一关。
        /// 与旧口径（像素侧计数 <c>ClearedPixelCount &gt;= TotalPixelCount</c>）的关键差别：载体侧与容器规划同源
        /// （倍乘门按倍率重复计的像素也在容器容量里），所以带倍乘门的关卡不会再提前判胜。
        /// </summary>
        /// <param name="checkpoint">调用方检查点名（见 <see cref="WinCheckpoint"/>），只用于日志。</param>
        public void CheckWin(string checkpoint = WinCheckpoint.Unspecified)
        {
            if (_winDeclared || _transitioning)
                return;
            _winDeclared = true;
            Debug.Log("[胜利判定] 检查点=" + checkpoint + " ｜ 板上所有车均已完成匹配 → 判胜");
            GameState.GameWin();
            //如果胜利时其他面板打开，关闭对应面板，包括后需可能的道具提示框
            UIManager.Instance.ShowSettingPanel(false);
            UIManager.Instance.showFailTipPanel(false);
            UIManager.Instance.ShowPropGetTip(false);
            Invoke(nameof(DoGameWin), 2.5f);
            UIManager.Instance.ShowWinPart();
        }

        /// <summary>调试用：跳过判胜条件直接宣告胜利（SettingPanel 的「WinCurrent」）。</summary>
        public void ForceWin()
        {
            _winDeclared = false;
            CheckWin(WinCheckpoint.Forced);
        }

        /// <summary>
        /// 事件驱动的失败检测入口：仅在关键事件点调用（小人进入传送带 / 完成上车 / 未满小车抵达前排 /
        /// 网格内寻路全部结束）。判定在「静止且死锁」时成立：传送带满、无像素还在网格内寻路、
        /// 无小车正在出库/补位/已开启匹配尚未抵达前排、无像素正在上车，且带上所有像素都没有同色可匹配容器。
        /// 触发后等待 1.5s 复活（保留部分像素在带、其余匹配后排车）。
        ///
        /// 未判失败时按 <see cref="debugFailLog"/> 打印诊断：**检查点 / 被哪条门禁挡下 / 传送带占用**
        /// ——这是定位「该判失败却不判」的主要手段（门禁编号与 <c>Docs/FailDetectionReview.md</c> §3 一致）。
        /// </summary>
        /// <param name="checkpoint">调用方的检查点名（见 <see cref="FailCheckpoint"/> 与各调用点），只用于日志。</param>
        public void TryCheckFail(string checkpoint = FailCheckpoint.Unspecified)
        {
            if (_transitioning)
            {
                LogFailCheck(checkpoint, "已锁定 _transitioning（胜负过渡中）");
                return;
            }
            if (IsReviveSequenceRunning())
            {
                // 复活序列期间：保留像素还占着传送带 / 缓冲区，而它们被排除在匹配之外 ——
                // 门禁 8 会因此找不到任何「可匹配」的带上像素而**误判失败**，所以整段窗口一律不判。
                // （正常情形门禁 7 也挡得住，但「溢出像素全部无同色车」时没有任何 consumingCount。）
                LogFailCheck(checkpoint, "复活表现播放中");
                return;
            }
            if (recordMode)
            {
                LogFailCheck(checkpoint, "Record 模式不判失败（容器不参与吸收）");
                return;
            }
            if (IsFail(out string reason))
            {
                _transitioning = true;
                _lastFailCheckLog = null;   // 真判了失败：清掉去重记忆，复活后的诊断不被旧行压掉
                //GameState.GameFail();
                //Invoke(nameof(DoRevive), 1.5f);
                DOVirtual.DelayedCall(1.5f, () =>
                {
                    UIManager.Instance.showRevivePanel(true);
                });
                return;
            }
            LogFailCheck(checkpoint, reason);
        }

        /// <summary>
        /// 失败判定：传送带满 + 无出库/补位/上车进行中 + 带满且每个槽位像素都没有同色可匹配容器。
        /// <paramref name="reason"/> 为「判定为非失败」的原因（含挡下它的门禁编号），判定为失败时为 null。
        /// </summary>
        private bool IsFail(out string reason)
        {
            // 门禁编号与 `Docs/FailDetectionReview.md` §3 一一对应，便于日志与文档互查
            reason = null;

            if (conveyorZone == null || conveyorZone.belt == null)
            {
                reason = "门禁1 没有传送带（conveyorZone / belt 为空）";
                return false;
            }
            if (conveyorZone.TotalSlots <= 0)
            {
                reason = "门禁2 传送带容量为 0";
                return false;
            }
            if (conveyorZone.OccupiedSlots < conveyorZone.TotalSlots)
            {
                reason = "门禁3 传送带未满（还有空槽可进像素）";
                return false;   // 传送带未满
            }
            if (containerGroup == null)
            {
                reason = "门禁4 没有容器组";
                return false;
            }

            // 静止门槛：有像素还在网格内寻路 → 还有进度，不判失败。
            // 关键原因是倍乘门：网格里没走完的像素可能还没穿过门（分身尚未生成），
            // 此时判失败会让复活把它们直接收走，实际送出的像素数就与 TotalPixelCount 对不上。
            // 最后一颗像素走出网格时 CrowdBufferZone 会回调 OnGridPathfindingFinished 补一次检测。
            if (crowdBuffer != null && crowdBuffer.HasGridPathfindingPixels)
            {
                reason = "门禁5 网格内还有像素在寻路（倍乘门分身可能尚未生成）";
                return false;
            }

            // 静止门槛：有车正在出库/补位/已开启匹配尚未抵达前排 → 还有进度，不判失败
            if (containerGroup.HasPendingFrontTransition())
            {
                reason = "门禁6 有车正在补位 / 后排车已开盖且前方已放行（即将抵达前排）";
                return false;
            }

            // 静止门槛：有像素正在上车（jump 或回退 lerp）→ 还有进度，不判失败
            if (containerGroup.consumingCount > 0)
            {
                reason = "门禁7 有 " + containerGroup.consumingCount + " 个像素正在上车";
                return false;
            }

            var belt = conveyorZone.belt;
            for (int i = 0; i < belt.slotCount; i++)
            {
                var pixel = belt.GetItem(i) as PixelItem;
                if (pixel == null)
                    continue;
                if (containerGroup.HasMatchableContainerOfColor(pixel.colorId))
                {
                    reason = "门禁8 带上槽位 " + i + " 的像素（colorId=" + pixel.colorId + "）有同色可匹配容器";
                    return false;   // 至少一个可匹配 → 未失败
                }
            }

            return true;
        }

        /// <summary>
        /// 失败判定「没判失败」时的诊断日志：一行内给出**检查点 / 被哪条门禁挡下 / 传送带占用**。
        /// 由 <see cref="debugFailLog"/> 开关控制；与上一条完全相同的行不重复打印。
        /// </summary>
        private void LogFailCheck(string checkpoint, string reason)
        {
            if (!debugFailLog)
                return;

            string belt = conveyorZone != null
                ? conveyorZone.OccupiedSlots + "/" + conveyorZone.TotalSlots
                : "无传送带";

            string line = "[失败判定] 检查点=" + checkpoint + " ｜ 传送带=" + belt + " ｜ 未判失败：" + reason;

            if (line == _lastFailCheckLog)
                return;
            _lastFailCheckLog = line;

            Debug.Log(line);
        }

        /// <summary>检查点名常量：只用于失败判定日志，便于与 <c>Docs/FailDetectionReview.md</c> §2 的四条调用点对照。</summary>
        public static class FailCheckpoint
        {
            /// <summary>检查点 1：像素上带（ConveyorBeltZone.OnSlotPassedEntry）。</summary>
            public const string SlotEntered = "像素上带";

            /// <summary>检查点 2：单个像素上车落定（ContainerGroup.OnPixelConsumed）。</summary>
            public const string PixelConsumed = "上车落定";

            /// <summary>检查点 3：补位车抵达前排（ContainerGroup.OnCarArrivedFront）。</summary>
            public const string CarArrivedFront = "补位车抵达前排";

            /// <summary>检查点 4：网格内寻路的最后一颗像素走出网格（CrowdBufferZone 事件）。</summary>
            public const string GridPathfindingFinished = "网格寻路排空";

            /// <summary>调用方未标注检查点名。</summary>
            public const string Unspecified = "未标注";
        }

        /// <summary>胜利判定检查点名常量：只在事件驱动的那两个时点被调用，仅用于日志。</summary>
        public static class WinCheckpoint
        {
            /// <summary>某辆车完成匹配（最后一颗像素开始上车）：<c>ContainerGroup.ConsumeCar</c>。</summary>
            public const string CarMatched = "完成匹配";

            /// <summary>某辆车离开盘面（开始倒车出库）：<c>ContainerGroup.StartContainerExit</c>。复查用。</summary>
            public const string CarLeft = "车离开";

            /// <summary>复活落定：<c>DoRevive</c> 末尾的补查（复活期间判胜会被 <c>_transitioning</c> 挡下）。</summary>
            public const string ReviveSettled = "复活落定";

            /// <summary>调试：SettingPanel 的「WinCurrent」强制胜利。</summary>
            public const string Forced = "强制";

            /// <summary>调用方未标注检查点名。</summary>
            public const string Unspecified = "未标注";
        }

        /// <summary>检查点 4 的处理器：订阅 / 退订用同一个具名方法，保证 <c>-=</c> 能解绑。</summary>
        private void OnGridPathfindingFinished()
        {
            TryCheckFail(FailCheckpoint.GridPathfindingFinished);
        }

        private void DoGameWin()
        {
            Debug.Log("Game Win");
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.GameWin();
                UIManager.Instance.showWinPanel(true);
            }
        }

        /// <summary>失败后的复活：保留固定数量像素在传送带，其余溢出像素直接匹配后排车；复活后回到游玩态继续本关。</summary>
        public void DoRevive()
        {
            DOVirtual.DelayedCall(0.3f, () =>
            {
                Revive();
                GameState.GameStart();   // 复活后回到游玩态，继续本关
                _transitioning = false;
                // 复活期间 _transitioning 为真，判胜会被 CheckWin 挡下；这里解锁后补查一次，
                // 避免「复活过程中最后一辆车完成匹配」被永久吞掉（复活的匹配是同步登记的，此刻计数已是终值）。
                if (containerGroup != null)
                    containerGroup.TryCheckWin(WinCheckpoint.ReviveSettled);
            });
        }

        /// <summary>
        /// 复活：保留 reviveKeepBeltCount 个像素在传送带上，其余像素（传送带溢出 + 缓冲区全部，含未上传送带的）
        /// 直接匹配后排车（优先前排、同排列小）。无同色后排车的像素并入「消失」组、
        /// 同样 pop 一下再销毁并计入已清除，保持胜负计数一致。
        ///
        /// **匹配与座位同步做完，只有跳跃组按 reviveInterval 依次起播**（本方法组队，起播交给
        /// <see cref="PlayReviveJumpQueue"/>）：这样 <see cref="DoRevive"/> 里「此刻计数已是终值」的前提不变，
        /// 失败门禁 7 也会在整段跳跃期间一直挡住重复判失败。
        ///
        /// **溢出像素不提前离场**：整段序列期间它们**仍然留在传送带槽位 / 缓冲区里被正常驱动**
        /// （跟着带移动、被物理推挤），只是被标了 <see cref="PixelItem.reviveReserved"/> 而从
        /// 「匹配 / 进传送带 / 表情候选」三条链路里排除；轮到自己时（跳跃 = 起跳前，消失 = pop 完成）
        /// 才由 <see cref="ReleaseRevivePixel"/> 真正摘下来。代价是这段窗口里带 / 缓冲区被它们占着，
        /// 所以 <see cref="TryCheckFail"/> 在窗口内一律不判失败。
        ///
        /// 两组的时序：
        /// · **消失组**（深排原地消失 + 无同色车的）在 t=0 **一次性全部起播**，不走间隔；
        /// · **跳跃组**与消失组同帧起播第一颗，之后每 <see cref="reviveInterval"/> 一颗，
        ///   次序按「车行 → 车列 → 空位」升序（见 <see cref="CompareJumpOrder"/>）——
        ///   座位在 <c>PrepareBoarding</c> 登记那一刻就已预占，所以这个次序是确定的。
        ///
        /// 单颗时长全部取自本组件「复活表现 · 单颗时长」的五项（消失两段 / 跳跃 / 弹性两段），
        /// 打包成 <see cref="ContainerGroup.BoardingTiming"/> 传给起播端 —— **只影响复活**，
        /// 传送带路径不传（= <c>default</c>），继续用预制体与扩展方法自己的默认值。
        /// </summary>
        private void Revive()
        {
            if (conveyorZone == null || containerGroup == null)
                return;

            _overflowClickCount = 0;   // 复活清空堆积点击计数

            // 复活会重排缓冲区、把溢出像素直接送上车：先把场上的生气表情全收掉。
            // 跟随模式下表情是像素的子物体，不收就会跟着像素一起进车（乘客头上顶着生气脸）；
            // 网格上残留的「点击受阻」生气脸在复活之后也没有意义了。
            var emoji = EmojiManager.Instance;
            if (emoji != null)
                emoji.ClearAngryEmojis();

            // 0. 上一轮序列若还没播完，先把残留清干净（连同它们的保留标志一起解除）
            DestroyPendingRevivePixels();

            // 1. 收集溢出像素：传送带第 reviveKeepBeltCount 个之后的所有占用槽 + 缓冲区。
            //    **只标记 reviveReserved、不从带 / 缓冲区里摘下来** —— 它们在轮到自己之前仍被带 / 物理驱动。
            //    收集顺序与改动前一致（带 → 提取中 → 物理），保证 MatchPixelsToCars 的选车顺位不变。
            var overflow = new List<PixelItem>();
            overflow.AddRange(conveyorZone.ReserveBeltBeyond(reviveKeepBeltCount));

            if (crowdBuffer != null)
            {
                overflow.AddRange(crowdBuffer.DrainExtracting());   // 还在网格里走路的：照旧直接取出，不标保留
                overflow.AddRange(crowdBuffer.ReservePhysical());
            }

            // 2. 按颜色匹配车（同步扣容量 / 开盖 / 预占座位，但不起播动画），并按「前排跳车 / 深排原地消失」分表
            var jumps = new List<ContainerGroup.BoardingEntry>();
            var disappears = new List<ContainerGroup.BoardingEntry>();
            var unmatched = containerGroup.MatchPixelsToCars(overflow, jumps, disappears);

            // 3. 无同色后排车的像素：并入「消失」组（同样 pop 后销毁并计入已清除），不再瞬间凭空消失
            for (int i = 0; i < unmatched.Count; i++)
                if (unmatched[i] != null)
                    disappears.Add(new ContainerGroup.BoardingEntry { pixel = unmatched[i] });

            // 4. 表现时长：复活专用的 5 个参数（消失两段 / 跳跃 / 弹性两段）。
            //    不传时各项 <= 0，退回 DisappearWithPop 的 0.2 与车预制体上的 boardJumpDuration / 弹性时长。
            var timing = new ContainerGroup.BoardingTiming
            {
                popDuration            = reviveDisappearPopDuration,
                shrinkDuration         = reviveDisappearShrinkDuration,
                jumpDuration           = reviveJumpDuration,
                elasticScaleDuration   = reviveElasticScaleDuration,
                elasticRecoverDuration = reviveElasticRecoverDuration,
            };

            // 5. 消失组：t=0 一次性全部起播，不走间隔（上一轮残留已在步骤 0 清掉）
            for (int i = 0; i < disappears.Count; i++)
                containerGroup.PlayBoarding(disappears[i], timing);

            // 6. 跳跃组：按「车行 → 车列 → 空位」升序依次起播，第一颗与消失组同帧
            jumps.Sort(CompareJumpOrder);
            if (jumps.Count > 0)
            {
                _reviveJumpQueue = jumps;
                _revivePlaying = true;
                StartCoroutine(PlayReviveJumpQueue(timing));
            }

            if (debugClickLog)
                Debug.Log("[复活] 溢出=" + overflow.Count +
                    " 已匹配后排=" + (overflow.Count - unmatched.Count) +
                    " 无同色车销毁=" + unmatched.Count +
                    " ｜ 跳车=" + jumps.Count + " 消失=" + disappears.Count +
                    " ｜ 间隔=" + reviveInterval + "s");
        }

        /// <summary>
        /// 复活跳跃组的起播次序：**车行 → 车列 → 空位**，均由小到大
        /// （车行 / 车列 = 登记那一刻的车格坐标；空位 = 登记时预占的落点下标）。
        /// </summary>
        private static int CompareJumpOrder(ContainerGroup.BoardingEntry a, ContainerGroup.BoardingEntry b)
        {
            int c = a.row.CompareTo(b.row);
            if (c != 0)
                return c;
            c = a.col.CompareTo(b.col);
            if (c != 0)
                return c;
            return a.seatIndex.CompareTo(b.seatIndex);
        }

        /// <summary>
        /// 复活**跳跃组**按 <see cref="reviveInterval"/> 依次起播（<see cref="Revive"/> 排好序后启动）。
        /// <c>reviveInterval = 0</c> 时整个循环不 yield、同帧跑完。
        /// 跑在 GameController 上是为了蹭 <see cref="CleanupLevel"/> 的 <c>StopAllCoroutines</c>：
        /// 换关 / 重开时序列自然中止（残留像素由 <see cref="DestroyPendingRevivePixels"/> 兜底）。
        /// </summary>
        private IEnumerator PlayReviveJumpQueue(ContainerGroup.BoardingTiming timing)
        {
            var queue = _reviveJumpQueue;
            for (int i = 0; i < queue.Count; i++)
            {
                if (queue[i].pixel == null)
                    continue;   // 跨帧期间已被销毁
                if (containerGroup == null)
                    break;      // 关卡正在拆除（正常路径由 CleanupLevel 的 StopAllCoroutines 中止）
                ReleaseRevivePixel(queue[i].pixel);   // 起跳前才摘：此前一直跟着带 / 缓冲区动
                containerGroup.PlayBoarding(queue[i], timing);
                if (reviveInterval > 0f && i + 1 < queue.Count)
                    yield return new WaitForSeconds(reviveInterval);
            }

            _reviveJumpQueue = null;
            _revivePlaying = false;
        }

        /// <summary>
        /// 复活序列是否还在跑：要么跳跃队列协程没结束，要么**还有保留像素没被摘下来**
        /// （它们占着传送带 / 缓冲区且被排除在匹配外，见 <see cref="TryCheckFail"/>）。
        /// 故意用扫描而不是计数器：像素一旦销毁就自然不再计入，不会把守卫永久卡死。
        /// </summary>
        private bool IsReviveSequenceRunning()
        {
            if (_revivePlaying)
                return true;
            if (conveyorZone != null && conveyorZone.HasReservedPixel())
                return true;
            if (crowdBuffer != null && crowdBuffer.HasReservedPixel())
                return true;
            return false;
        }

        /// <summary>
        /// 把一颗复活保留的像素从它当前的来源真正摘下来（传送带清槽位 / 缓冲区解除物理并出队），
        /// 并解除 <see cref="PixelItem.reviveReserved"/> 标志。
        /// 消失组在 pop 完成时（<c>ContainerGroup.PlayDisappear</c> 的回调里）、跳跃组在起跳前各调一次。
        /// 已经不在带 / 缓冲区里（已销毁 / 已被取走）时只清标志。
        /// </summary>
        public void ReleaseRevivePixel(PixelItem pixel)
        {
            if (pixel == null)
                return;

            pixel.reviveReserved = false;
            if (conveyorZone != null)
                conveyorZone.ReleaseReserved(pixel);
            if (crowdBuffer != null)
                crowdBuffer.ReleaseReserved(pixel);
        }

        /// <summary>
        /// 清掉复活序列里**还没起播**的残留像素：先 <see cref="ReleaseRevivePixel"/>（清保留标志、把像素
        /// 从带 / 缓冲区摘下来）再销毁。序列被打断（重开关卡 / 返回主界面）时调用 ——
        /// 不然它们会既占着带 / 缓冲区、又永远等不到起播。
        /// </summary>
        private void DestroyPendingRevivePixels()
        {
            var queue = _reviveJumpQueue;
            _reviveJumpQueue = null;
            _revivePlaying = false;
            if (queue == null)
                return;

            for (int i = 0; i < queue.Count; i++)
            {
                var pixel = queue[i].pixel;
                if (pixel == null)
                    continue;
                ReleaseRevivePixel(pixel);
                Destroy(pixel.gameObject);
            }
        }

        // ===== 道具2「磁铁」 =====

        [Header("道具2「磁铁」· 上车表现")]
        [Tooltip("被吸的人·原地消失的「弹出」阶段时长（秒）。原来用预制体默认的 0.2，这里放慢")]
        public float magnetPopDuration = 0.4f;

        [Tooltip("被吸的人·原地消失的「缩小」阶段时长（秒）。原来用预制体默认的 0.2，这里放慢")]
        public float magnetShrinkDuration = 0.4f;

        [Tooltip("被吸的人·「上车坐下」的时长（秒）—— 人出现在车上、身体坐到座位偏移所花的时间。**车会等它坐完才出库**")]
        public float magnetSitDownDuration = 0.4f;

        /// <summary>磁铁上车用的表现时长：把"原地消失"的弹出 / 缩小放慢（其余项留 0 = 用该处自己的默认）。</summary>
        private ContainerGroup.BoardingTiming MagnetBoardingTiming()
        {
            return new ContainerGroup.BoardingTiming
            {
                popDuration = magnetPopDuration,
                shrinkDuration = magnetShrinkDuration,
                sitDownDuration = magnetSitDownDuration,
                deferExitUntilSeated = true,   // 人坐稳之后车才开走
                landingSfxWhenLast = true,     // 最后一颗落座补一声「Geton」（人上车）
            };
        }

        /// <summary>
        /// 道具2「磁铁」：把**最前面一排**车（每列 <c>grid[col, 0]</c>）用同色像素喂满，装满的车随即走正常出库链路。
        ///
        /// **取人优先级（6 层，逐层降级）**
        /// | 层 | 来源 | 区域 | 颜色 | 产出 |
        /// | 1 | 棋盘 | 非倍乘 | 明确 | 1 个 |
        /// | 2 | 棋盘 | 非倍乘 | 问号 | 1 个 |
        /// | 3 | 棋盘 | 倍乘 | 明确 | 该格倍率个（补分身） |
        /// | 4 | 棋盘 | 倍乘 | 问号 | 该格倍率个 |
        /// | 5 | 木箱盖住 / 盒子 / 管道 | 非倍乘 | 任意 | 1 个 |
        /// | 6 | 木箱盖住 | 倍乘 | 任意 | 该格倍率个 |
        ///
        /// **在倍乘区域取人 = 磁铁替玩家把这个人"送过了门"**：按 <see cref="PixelGroup.GateMultiplierAt"/>
        /// 补出分身（与道具3 的 UFO 同一套口径），于是总量不多不少。分身装不下目标车时交正常匹配链路
        /// 送给后方的同色车，见 <see cref="FeedCarWithUnits"/>。
        ///
        /// 其他口径：
        /// · **压在管道轨道上的同色人优先取**：按层序扫到的同色人若正落在某条管道的**轨道格**里，
        ///   就先取那条轨道**最远端**（远离管道那端）的同色人 —— 顺手把管道腾空；
        ///   扫到的这一颗**不会被丢掉**，本车还要同色的人时照旧轮得到它。
        ///   正在投波的管道不参与（轨道上的人还在飞），见 <see cref="PreferPipeTail"/>；
        /// · **不碰传送带 / 缓冲区**：那里的像素本来就会按正常流程匹配到同色车，而把它们抽出来只有破坏性的
        ///   Drain API（复活那套会把没匹配上的像素直接销毁），磁铁不该带这种副作用；
        /// · **冰组冻住的一律不取**（冰组记着自己在哪些格上，取走会让它的账错乱）；
        /// · 盒子 / 管道的人不在网格上、坐标是哨兵 (-1,-1)，**永远不在倍乘区域**，所以第 6 层对它们没有变体；
        /// · 装不满的车**留在前排等待**（能吸多少吸多少，不强求出库）。
        ///
        /// 表现走 <see cref="ContainerGroup.ConsumePixelInstant"/>：**原地消失、直接在车上落点出现**（不播跳车、不走路）。
        /// </summary>
        public void MagnetClearFrontRow()
        {
            if (pixelGroup == null || containerGroup == null)
                return;

            var carGrid = containerGroup.grid;
            if (carGrid == null)
                return;

            // 一次成桶：避免每取一颗都按层重扫一遍全盘（层数 × 每颗一次扫描的常数太大）
            var buckets = BuildMagnetBuckets();
            bool harvestedAny = false;

            // 本次磁铁从棋盘上取走的人（木箱拆箱计数用，见循环之后的 NotifyPixelsMovedOut）
            var boardHarvested = new List<PixelItem>();

            // 诊断用：**取走之前**先记下"轨道上当时还点得动的人"，好把"磁铁弄闷死的"和"本来就闷着的"分开。
            // 不加这一步，日志会把所有点不动的轨道人都算到磁铁头上 —— 那是归因错误。
            var clickableBefore = debugClickLog ? SnapshotClickableTrackPixels() : null;

            for (int col = 0; col < containerGroup.columns; col++)
            {
                var car = carGrid[col, 0];
                if (car == null)
                    continue;

                car.OpenLid();   // 幂等：前排车本来就是开盖的

                while (!car.IsEmpty)
                {
                    int mult;
                    var pixel = HarvestForMagnet(car.colorId, buckets, boardHarvested, out mult);
                    if (pixel == null)
                        break;   // 这个颜色已经没有任何可取来源 → 装不满，留在前排等待

                    harvestedAny = true;
                    FeedCarWithUnits(pixel, mult, car);
                }
            }

            // 刚取完：查管道轨道上剩下的人有没有点不出去的，有就把原因和归因打出来
            if (harvestedAny && debugClickLog)
                LogMagnetTrackStranding(boardHarvested, clickableBefore);

            // 木箱拆箱计数：与正常点击**同口径** —— 一次磁铁算一组，每个相邻的木箱只计 1 次
            // （正常点击是"同一次点击移出的一组只算 1 次"，见 <see cref="PixelGroup.NotifyPixelsMovedOut"/>）。
            // 不通知的话，相邻像素被磁铁吃光的木箱永远等不到计数、拆不掉 → 那些格永远是障碍 → 死锁。
            // 它读的是各自"移出前"的 gridX / gridZ，所以先清格再调也没关系。
            if (boardHarvested.Count > 0)
                pixelGroup.NotifyPixelsMovedOut(boardHarvested);

            if (harvestedAny)
                MagnetRefreshAfterRemoval();
        }

        /// <summary>磁铁的分层数（层序见 <see cref="MagnetClearFrontRow"/>）。</summary>
        private const int MagnetTierCount = 6;

        /// <summary>
        /// 棋盘像素按层分桶（一次成桶，出桶即移除，不会重复取）。
        /// 桶下标 0~5 对应第 1~6 层；层序见 <see cref="MagnetClearFrontRow"/>。
        ///
        /// 盒子 / 管道的像素不在这里 —— 它们必须**按颜色现取**（<see cref="ExtractFromAnyBox"/> /
        /// <see cref="PrepayFromAnyPipe"/>），没法预先枚举。
        /// 冰组冻住的一律不进桶（取走会让冰组的账错乱）。
        /// </summary>
        private List<PixelItem>[] BuildMagnetBuckets()
        {
            var buckets = new List<PixelItem>[MagnetTierCount];
            for (int i = 0; i < MagnetTierCount; i++)
                buckets[i] = new List<PixelItem>();

            var grid = pixelGroup.grid;
            if (grid == null)
                return buckets;

            int cols = pixelGroup.columns;
            int totalRows = pixelGroup.TotalRows;

            // 正在投波的管道：它轨道上的人是"还在飞的"（目标格已在 grid 里登记，位置却还在逐格移动），
            // 这时摘走它，MoveCell 的移动协程还会继续往车上的座位上写 localPosition → 人错位。
            // 整条轨道本次一律不碰；等那波落定，下次磁铁再来。
            var midFlight = CollectReleasingPipeCells();

            for (int row = 0; row < totalRows; row++)
                for (int col = 0; col < cols; col++)
                {
                    var p = grid[col, row];
                    if (p == null)
                        continue;
                    if (p.IsFrozen)
                        continue;                        // 冰组：不取
                    if (midFlight.Count > 0 && midFlight.Contains(new Vector2Int(col, row)))
                        continue;                        // 还在飞的管道人：不取

                    bool inGate = pixelGroup.IsInGateRegion(col, row);
                    bool unknown = p.isQuestion && !p.revealed;

                    int tier;
                    if (p.IsCovered)
                        tier = inGate ? 5 : 4;           // 木箱盖住：最低两档
                    else if (inGate)
                        tier = unknown ? 3 : 2;
                    else
                        tier = unknown ? 1 : 0;

                    buckets[tier].Add(p);
                }

            return buckets;
        }

        /// <summary>正在投波的管道的轨道格（这些格上的人是"还在飞的"，本次磁铁一律不碰）。</summary>
        private HashSet<Vector2Int> CollectReleasingPipeCells()
        {
            var cells = new HashSet<Vector2Int>();
            var pipes = pixelGroup.GetComponentsInChildren<PipeItem>();
            for (int i = 0; i < pipes.Length; i++)
            {
                var pipe = pipes[i];
                if (pipe == null || !pipe.IsReleasing)
                    continue;

                var track = pipe.TrackCells();
                if (track == null)
                    continue;
                for (int k = 0; k < track.Count; k++)
                    cells.Add(track[k]);
            }
            return cells;
        }

        /// <summary>
        /// 扫到的人若正压在一条管道的**轨道格**上，就**先取那条轨道最远端**的同色人（"尾部" = 远离管道那端）；
        /// 扫到的这一颗**不受影响、照旧留在桶里**——本车还要同色的人时自然会轮回到它。
        /// 没压在轨道上就原样返回 <paramref name="found"/>。
        ///
        /// 为什么值得先拿尾部那颗：压在轨道上的同色人正是挡住管道的那个（管道靠"轨道清空"才放下一波），
        /// 磁铁顺水推舟先拿它 —— 既没抢别处的人，又顺手把管道解开。
        /// <see cref="PipeItem.TrackCells"/> 是"近管道 → 远"序，所以从末尾往回找。
        ///
        /// 正在投波的管道不参与：轨道上的人是"还在飞的"（目标格已登记、位置还在逐格移动），
        /// 这时摘走它，`PipeItem.MoveCell` 的移动协程会继续往车上的座位写 localPosition → 人错位。
        /// </summary>
        private PixelItem PreferPipeTail(PixelItem found, List<PixelItem>[] buckets)
        {
            if (found == null)
                return null;

            var pipes = pixelGroup.GetComponentsInChildren<PipeItem>();
            for (int i = 0; i < pipes.Length; i++)
            {
                var pipe = pipes[i];
                if (pipe == null || pipe.IsReleasing)
                    continue;

                var track = pipe.TrackCells();
                if (track == null || track.Count == 0)
                    continue;

                bool onTrack = false;
                for (int k = 0; k < track.Count; k++)
                {
                    if (track[k].x == found.gridX && track[k].y == found.gridZ)
                    {
                        onTrack = true;
                        break;
                    }
                }
                if (!onTrack)
                    continue;

                // 尾部（远离管道那端）优先
                for (int k = track.Count - 1; k >= 0; k--)
                {
                    var cell = track[k];
                    var p = pixelGroup.grid[cell.x, cell.y];
                    if (p == null || p.colorId != found.colorId || p.IsFrozen)
                        continue;
                    if (p == found)
                        return found;      // 尾部就是它自己（或前面几颗已被取走）

                    RemoveFromBuckets(buckets, p);   // 先取的那颗从桶里摘掉，免得之后又被取一次
                    return p;
                }
                return found;   // 轨道上没有别的同色（found 自己就在轨道上）
            }
            return found;
        }

        /// <summary>
        /// 磁铁取完之后，检查**被取走的人所在那条管道轨道**上剩下的同色人还点不点得动；点不动就把原因打出来。
        ///
        /// 起因是"闷死"：以前管道**还有未释放波次**时整条轨道都算障碍，压在轨道上的人只能当起点、
        /// 不能当通道，整组只能借某一颗的横向出口（"门"）出去；磁铁按"优先尾部"把门取走，剩下的就再也
        /// 点不出去。**那条规则已经改掉了** —— 轨道格不再参与障碍判定（见 <c>PixelGroup.IsEmptyForExposure</c>
        /// 与 <c>CanGroupReachFront</c>），所以这一类闷死理论上不该再有。
        ///
        /// 这条日志留在那里**当探针**：它要是再响，就说明是别的原因（墙 / 别的颜色 / 倍乘门），四邻那几行会写明。
        /// <paramref name="clickableBefore"/> = 本次取走**之前**"轨道上还点得动的人"
        /// （见 <see cref="SnapshotClickableTrackPixels"/>），用来把"磁铁造成的"和"本来就点不动的"分开。
        ///
        /// 由 <see cref="debugClickLog"/> 开关控制；没闷死就什么都不打。
        /// </summary>
        private void LogMagnetTrackStranding(List<PixelItem> taken, HashSet<PixelItem> clickableBefore)
        {
            if (taken == null || taken.Count == 0 || pixelGroup == null)
                return;

            var pipes = pixelGroup.GetComponentsInChildren<PipeItem>();
            var reported = new HashSet<PipeItem>();   // 同一条轨道只报一次（一次磁铁可能从它取走好几颗）
            for (int i = 0; i < taken.Count; i++)
            {
                var gone = taken[i];
                if (gone == null)
                    continue;

                for (int p = 0; p < pipes.Length; p++)
                {
                    var pipe = pipes[p];
                    if (pipe == null || pipe.IsReleasing)
                        continue;

                    var track = pipe.TrackCells();
                    if (track == null || track.Count == 0)
                        continue;
                    if (!IsOnTrack(track, gone.gridX, gone.gridZ))
                        continue;
                    if (!reported.Add(pipe))
                        continue;

                    LogTrackStrandingDetail(pipe, track, gone, clickableBefore);
                }
            }
        }

        /// <summary>
        /// 快照：**当前**所有非投波管道轨道上、还能点得动的人（判据与玩家一致：<c>FloodFill</c> + <see cref="CanReachFront"/>）。
        /// 磁铁取走之前调一次，之后拿来区分"磁铁新弄闷死的"与"本来就闷着的"。只读，不改状态。
        /// </summary>
        private HashSet<PixelItem> SnapshotClickableTrackPixels()
        {
            var ok = new HashSet<PixelItem>();
            if (pixelGroup == null)
                return ok;

            var pipes = pixelGroup.GetComponentsInChildren<PipeItem>();
            for (int i = 0; i < pipes.Length; i++)
            {
                var pipe = pipes[i];
                if (pipe == null || pipe.IsReleasing)
                    continue;

                var track = pipe.TrackCells();
                if (track == null)
                    continue;

                for (int k = 0; k < track.Count; k++)
                {
                    var cell = track[k];
                    var q = pixelGroup.grid[cell.x, cell.y];
                    if (q == null || q.IsFrozen)
                        continue;

                    var group = FloodFill(q);
                    if (group.Count > 0 && CanReachFront(group))
                        ok.Add(q);
                }
            }
            return ok;
        }

        private static bool IsOnTrack(List<Vector2Int> track, int col, int row)
        {
            for (int k = 0; k < track.Count; k++)
                if (track[k].x == col && track[k].y == row)
                    return true;
            return false;
        }

        /// <summary>
        /// 某条管道轨道上的详情：谁点不动、四邻为什么通不过、以及**归因**（磁铁造成的 / 本来就闷着的）。
        /// </summary>
        private void LogTrackStrandingDetail(PipeItem pipe, List<Vector2Int> track, PixelItem gone,
            HashSet<PixelItem> clickableBefore)
        {
            // 轨道上剩下的同色人（不含刚被取走的那颗）
            var rest = new List<PixelItem>();
            for (int k = 0; k < track.Count; k++)
            {
                var cell = track[k];
                var q = pixelGroup.grid[cell.x, cell.y];
                if (q == null || q == gone || q.colorId != gone.colorId || q.IsFrozen)
                    continue;
                rest.Add(q);
            }
            if (rest.Count == 0)
                return;   // 轨道上没剩下同色的人，谈不上卡住

            // 逐颗按玩家口径验一遍（FloodFill 取组 + CanReachFront），谁点不动就报谁；同时按"取走之前点不点得动"归因
            var stranded = new List<PixelItem>();
            int causedByMagnet = 0;
            int preExisting = 0;
            for (int i = 0; i < rest.Count; i++)
            {
                var group = FloodFill(rest[i]);
                if (group.Count > 0 && CanReachFront(group))
                    continue;   // 还点得动

                stranded.Add(rest[i]);
                if (clickableBefore != null && clickableBefore.Contains(rest[i]))
                    causedByMagnet++;    // 之前还点得动 → 是磁铁取走那几颗把它弄闷死的
                else
                    preExisting++;       // 之前就点不动 → 既有问题，跟磁铁无关
            }
            if (stranded.Count == 0)
                return;   // 都还点得动 → 不打扰

            var sb = new System.Text.StringBuilder();
            sb.Append("[Magnet][闷死] 第 ").Append(GameData.CurrentLevel)
              .Append(" 关：管道 ").Append(pipe.name).Append(" 的轨道上 ")
              .Append(stranded.Count).Append('/').Append(rest.Count)
              .Append(" 颗颜色 ").Append(gone.colorId).Append(" 的人点不出去")
              .Append("（磁铁本次从该轨道取走了 (").Append(gone.gridX).Append(',').Append(gone.gridZ).Append(")）。")
              .Append("\n  · 归因：磁铁造成 ").Append(causedByMagnet).Append(" 颗；磁铁之前就点不动 ")
              .Append(preExisting).Append(" 颗")
              .Append(preExisting > 0 ? "（← 既有问题，与磁铁无关）" : "")
              .Append("\n  · 该管道剩余波次：")
              .Append(pipe.HasRemainingWaves ? "有" : "无")
              .Append("（轨道格已不再参与障碍判定，所以它不再是原因；请看下面四邻）");

            for (int i = 0; i < stranded.Count; i++)
            {
                var s = stranded[i];
                bool wasOk = clickableBefore != null && clickableBefore.Contains(s);
                sb.Append("\n  · ").Append(wasOk ? "【磁铁造成】" : "【既有】")
                  .Append("点不动的 (").Append(s.gridX).Append(',').Append(s.gridZ).Append(") 四邻：");
                for (int d = 0; d < 4; d++)
                {
                    int nx = s.gridX + Dx4[d];
                    int nz = s.gridZ + Dz4[d];
                    sb.Append("\n      ").Append(DirLabel(d)).Append('(').Append(nx).Append(',').Append(nz).Append(") ")
                      .Append(DescribeMagnetBlock(nx, nz, s.colorId, gone));
                }
            }

            sb.Append("\n  · 判读：四邻若全为「障碍」→ 它根本没有出口；若有「可通行」→ 那条路走不到首排。");
            Debug.Log(sb.ToString());
        }

        private static string DirLabel(int d)
        {
            switch (d)
            {
                case 0: return "X+列";
                case 1: return "X-列";
                case 2: return "Z+行(远离首排)";
                default: return "Z-行(靠首排)";
            }
        }

        /// <summary>
        /// 给"闷死"日志用：某一格为什么通不过 —— 判定顺序与 <see cref="PixelGroup.CanGroupReachFront"/> 对齐
        /// （越界 → 墙体 / 管道本体 / 箱子 / 木箱 → 格子内容）。
        /// **活跃管道轨道不在其中**：轨道格已经不是障碍了（空着就算可通行）。
        /// </summary>
        private string DescribeMagnetBlock(int col, int row, int colorId, PixelItem gone)
        {
            if (!pixelGroup.IsInRange(col, row))
                return "越界 → 不算路";
            if (gone != null && col == gone.gridX && row == gone.gridZ)
                return "刚被磁铁取走（原来的出口位）";
            if (pixelGroup.IsBlocked(col, row))
            {
                string what = pixelGroup.IsWall(col, row) ? "墙体"
                    : pixelGroup.IsPipe(col, row) ? "管道本体"
                    : pixelGroup.IsBox(col, row) ? "箱子" : "木箱格";
                return what + " → 障碍";
            }

            var cell = pixelGroup.grid[col, row];
            if (cell == null)
                return "空格 → 可通行";
            if (cell.colorId == colorId)
                return "同色「" + cell.name + "」→ 组内，可通行";
            return "别色「" + cell.name + "」→ 障碍";
        }

        /// <summary>桶里第一颗该颜色的（**只找不摘**：真正取走哪一颗由调用方决定后再 RemoveFromBuckets）。</summary>
        private static PixelItem PeekFromBucket(List<PixelItem> bucket, int colorId)
        {
            for (int i = 0; i < bucket.Count; i++)
            {
                var p = bucket[i];
                if (p != null && p.colorId == colorId)
                    return p;
            }
            return null;
        }

        private static void RemoveFromBuckets(List<PixelItem>[] buckets, PixelItem p)
        {
            for (int i = 0; i < buckets.Length; i++)
                buckets[i].Remove(p);
        }

        /// <summary>
        /// 按层序取一颗该颜色的源像素。<paramref name="mult"/> 出参 = 这颗人"代表"几个
        /// （倍乘区域的格 = 该格倍率，其余恒为 1）。返回 null = 这个颜色已经没有任何可取来源。
        /// 取出的像素**已经离格 / 离源**（棋盘像素已摘网格，盒子 / 管道来的已交出来）。
        /// <paramref name="boardHarvested"/> 收集本次从**棋盘**取走的人（盒子 / 管道的不进，它们不在木箱邻域里），
        /// 交给调用方在整次磁铁结束时一次性地做木箱拆箱计数。
        /// </summary>
        private PixelItem HarvestForMagnet(int colorId, List<PixelItem>[] buckets,
            List<PixelItem> boardHarvested, out int mult)
        {
            mult = 1;

            // 第 1~4 层：棋盘。前两层倍率恒 1，后两层在倍乘区域、按该格倍率补分身。
            for (int tier = 0; tier <= 3; tier++)
            {
                var p = PeekFromBucket(buckets[tier], colorId);
                if (p == null)
                    continue;

                // 压在管道轨道上 → 先取那条轨道最远端的同色人；扫到的这颗不动，留在桶里等下一轮
                var pick = PreferPipeTail(p, buckets);
                RemoveFromBuckets(buckets, pick);

                mult = tier >= 2 ? Mathf.Max(1, pixelGroup.GateMultiplierAt(pick.gridX, pick.gridZ)) : 1;
                DetachPixelForMagnet(pick);
                boardHarvested.Add(pick);
                return pick;
            }

            // 第 5 层：木箱 → 盒子 → 管道。
            // 顺序按"越靠后越不自然"排：木箱本来就是棋盘上的人（只是被盖住）、盒子的人已经实例化、
            // 管道的人还要凭空造出来并让管道后面少产一颗。
            var crateFound = PeekFromBucket(buckets[4], colorId);
            if (crateFound != null)
            {
                var pick = PreferPipeTail(crateFound, buckets);
                RemoveFromBuckets(buckets, pick);
                DetachPixelForMagnet(pick);
                boardHarvested.Add(pick);
                return pick;
            }

            var boxPixel = ExtractFromAnyBox(colorId);
            if (boxPixel != null)
                return boxPixel;

            var pipePixel = PrepayFromAnyPipe(colorId);
            if (pipePixel != null)
                return pipePixel;

            // 第 6 层：木箱盖住 + 倍乘区域
            var crateGateFound = PeekFromBucket(buckets[5], colorId);
            if (crateGateFound != null)
            {
                var pick = PreferPipeTail(crateGateFound, buckets);
                RemoveFromBuckets(buckets, pick);
                mult = Mathf.Max(1, pixelGroup.GateMultiplierAt(pick.gridX, pick.gridZ));
                DetachPixelForMagnet(pick);
                boardHarvested.Add(pick);
                return pick;
            }

            return null;
        }

        /// <summary>第 5 层 · 盒子：从任意箱子里取一颗该颜色的隐藏 Pixel（这个箱子没有就下一个）。</summary>
        private PixelItem ExtractFromAnyBox(int colorId)
        {
            var boxes = pixelGroup.GetComponentsInChildren<BoxItem>();
            for (int i = 0; i < boxes.Length; i++)
            {
                var box = boxes[i];
                if (box == null)
                    continue;

                var p = box.TryExtractOne(colorId);
                if (p == null)
                    continue;

                PrepareOffGridUnit(p);
                return p;
            }
            return null;
        }

        /// <summary>第 5 层 · 管道：从任意管道预支一颗该颜色（管道后面那一波会少产一颗还账）。</summary>
        private PixelItem PrepayFromAnyPipe(int colorId)
        {
            var pipes = pixelGroup.GetComponentsInChildren<PipeItem>();
            for (int i = 0; i < pipes.Length; i++)
            {
                var pipe = pipes[i];
                if (pipe == null)
                    continue;

                var p = pipe.PrepayOne(colorId);
                if (p == null)
                    continue;

                PrepareOffGridUnit(p);
                return p;
            }
            return null;
        }

        /// <summary>
        /// 盒子 / 管道来的像素是隐藏的、坐标是哨兵 (-1,-1)：唤醒、置回正常大小、清掉"生成中"标记、关掉交互。
        /// **位置留在原处**（箱子中心下方 / 管道口）—— 后面的消失动画就在那里播，表现成"从这里被吸走"。
        ///
        /// 必须清 <c>placing</c> / <c>walkableDuringExtraction</c>：那是管道蛇形生成期间的两个标记
        /// （<see cref="PipeItem"/> 的 SpawnPixelAtPipe 会置位），而磁铁预支的这颗**永远不会走生成流程**，
        /// 留着会让它此后 <c>SetExposed</c> 全被闸门挡掉、并且一直自诩"提取中可穿行"。
        /// </summary>
        private void PrepareOffGridUnit(PixelItem p)
        {
            if (p == null)
                return;

            p.gameObject.SetActive(true);
            float unit = pixelGroup != null ? pixelGroup.unitSize : 1f;
            p.transform.localScale = Vector3.one * unit;
            p.walkableDuringExtraction = false;
            p.MarkPlaced();          // 清 placing 并把暴露状态归位（没置位时内部直接返回）
            p.SetClickable(false);
            p.SetWalking(false);
        }

        /// <summary>
        /// 把一颗源像素"兑现"给车：倍乘区域来的先按 <paramref name="mult"/> 补出分身，
        /// 然后先喂目标车；装不下的**交正常匹配链路送给后方的同色车**（前排优先、含未实例化的深排车）。
        ///
        /// 例：目标车还差 3 个，只找得到倍乘门里的绿人 → 取 2 颗（每颗 ×2 = 4 个），
        /// 3 个喂本车、多出来的 1 个匹配到后面。
        /// </summary>
        private void FeedCarWithUnits(PixelItem src, int mult, ContainerItem car)
        {
            if (src == null || car == null)
                return;

            var units = new List<PixelItem>(Mathf.Max(1, mult)) { src };
            for (int i = 1; i < mult; i++)
            {
                var clone = MagnetSpawnUnit(src);
                if (clone != null)
                    units.Add(clone);
            }

            var timing = MagnetBoardingTiming();

            // 本车这一轮能吃几颗：Remaining 是登记口径（ConsumePixelInstant 内部同步扣），一次算准
            int feed = Mathf.Min(units.Count, car.Remaining);
            for (int i = 0; i < feed; i++)
                containerGroup.ConsumePixelInstant(units[i], car, timing);   // 原地消失 → 直接在车上落点出现

            if (feed >= units.Count)
                return;

            // 溢出的：走正常匹配的分发口径 —— 前排优先、同排列小优先、**含未实例化的深排车**。
            // **forceInstant**：磁铁的人一律"原地消失 → 直接在落点出现"，**连前 4 排也不跳车** ——
            // 倍乘多出来的那些不该突然又跳起来（它们和本体是同一批人）。
            var spill = units.GetRange(feed, units.Count - feed);
            var jumps = new List<ContainerGroup.BoardingEntry>();
            var disappears = new List<ContainerGroup.BoardingEntry>();
            containerGroup.MatchPixelsToCars(spill, jumps, disappears, forceInstant: true);

            for (int i = 0; i < jumps.Count; i++)
                containerGroup.PlayBoarding(jumps[i], timing);
            for (int i = 0; i < disappears.Count; i++)
                containerGroup.PlayBoarding(disappears[i], timing);

            // 兜底：MatchPixelsToCars 内部 PrepareBoarding 落空时**既不登记也不返回**（那里只是一句 continue），
            // 所以不能只看它吐出来的 unmatched —— 按"实际登记了谁"取补集，免得漏下没人管的活像素。
            var handled = new HashSet<PixelItem>();
            for (int i = 0; i < jumps.Count; i++)
                if (jumps[i].pixel != null) handled.Add(jumps[i].pixel);
            for (int i = 0; i < disappears.Count; i++)
                if (disappears[i].pixel != null) handled.Add(disappears[i].pixel);

            var leftover = new List<PixelItem>();
            for (int i = 0; i < spill.Count; i++)
                if (spill[i] != null && !handled.Contains(spill[i]))
                    leftover.Add(spill[i]);

            if (leftover.Count > 0)
                LogMagnetLeftover(leftover);
        }

        /// <summary>
        /// 倍乘分身：造一颗同色（同问号）像素，摆到源像素的位置、同缩放。
        /// 与道具3 的 <see cref="Prop3SpawnClone"/> 同源；区别是不挂 UFO、不进 <c>_prop3Riders</c>
        /// （磁铁的分身不挂在任何会自转的物体下，没有这个问题）。
        /// <see cref="PixelGroup.SpawnPixel"/> 只实例化、**不写 pixelGroup.grid**，所以不会在源格留下幽灵。
        /// </summary>
        private PixelItem MagnetSpawnUnit(PixelItem src)
        {
            if (pixelGroup == null || src == null)
                return null;

            var cfg = GameManager.Instance != null ? GameManager.Instance.colorConfig : null;
            var clone = pixelGroup.SpawnPixel(src.gridX, src.gridZ, src.colorId, cfg,
                scaleZero: false, isQuestion: src.isQuestion);
            if (clone == null)
                return null;

            var t = clone.transform;
            t.position = src.transform.position;
            t.rotation = src.transform.rotation;
            t.localScale = src.transform.localScale;
            clone.SetClickable(false);
            clone.SetWalking(false);
            clone.RevealQuestion();   // 本体若已揭晓，分身也得跟着揭晓（否则一颗问号一颗原色，穿帮）
            return clone;
        }

        /// <summary>
        /// 溢出的分身两边都接不住（后方没有同色车 / 车全满）：销毁并打日志。
        /// 正常关卡不该出现 —— 出现即说明这一关的颜色配比真的喂不下这些分身。
        /// 与道具3 UFO 的 unmatched 口径一致：只销毁，不补 <c>ClearedPixelCount</c>。
        /// </summary>
        private void LogMagnetLeftover(List<PixelItem> leftover)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("[Magnet] 第 ").Append(GameData.CurrentLevel)
              .Append(" 关有 ").Append(leftover.Count)
              .Append(" 颗倍乘分身找不到同色车，已销毁。颜色：");
            for (int i = 0; i < leftover.Count; i++)
                sb.Append(leftover[i] != null ? leftover[i].colorId.ToString() : "?").Append(' ');
            Debug.LogWarning(sb.ToString());

            for (int i = 0; i < leftover.Count; i++)
                if (leftover[i] != null)
                    Destroy(leftover[i].gameObject);
        }

        /// <summary>
        /// 取走像素后的收尾，必须与 <see cref="ResolveMatch"/> 的移出收尾**逐条对齐**（少一条就会留下"看不见的遮挡"）：
        /// <c>TryOpenBoxes</c> / <c>TryAdvanceElevators</c> 释放箱子与升降台的占格（不释放那些格依旧是障碍，
        /// 旁边的人既不会亮、也点不出去）；<c>RefreshExposed</c> 重算暴露；<c>RefreshFrame</c> 重建整体描边
        /// （场景里有 FrameItem 时描边由它统一画，不重建就还是旧轮廓 → 「该发白光的没发」）。
        /// </summary>
        private void MagnetRefreshAfterRemoval()
        {
            pixelGroup.TryOpenBoxes();
            pixelGroup.TryAdvanceElevators();
            pixelGroup.RefreshExposed();
            RefreshFrame();
        }

        /// <summary>
        /// 把像素从像素网格摘掉（磁铁捞走后立刻瞬移上车，不走传送带）。
        /// 与 <see cref="ResolveMatch"/> 的移出口径保持一致：置空格、关暴露 / 点击、清道具高亮、
        /// 撤掉木箱遮盖，并记上「已点出」与进度分子（不记的话进度条永远到不了 100%）。
        /// </summary>
        private void DetachPixelForMagnet(PixelItem item)
        {
            int col = item.gridX;
            int row = item.gridZ;

            pixelGroup.grid[col, row] = null;
            item.SetExposed(false);
            item.RevealQuestion();     // 问号的人要坐进车里了：当场换成真实颜色（不揭晓的话车上顶着问号）
            item.SetPropGlow(false);   // 道具3 的强制取出高亮一并复位：它已离格
            item.SetCovered(false);    // 木箱盖住的：撤掉遮盖，否则渲染器还是关的 → 上了车是个隐形人
            item.SetClickable(false);
            item.SetWalking(false);    // 瞬移上车，不播走路

            GameData.RemovePixelCount++;
            GameData.ProgressPixelCount += Mathf.Max(1, pixelGroup.GateMultiplierAt(col, row));
        }

        // ===== 道具「强制取出」模式 =====

        /// <summary>
        /// 进入道具3模式：所有人发光提示，之后点击任意一组都能无视前排连通限制、交给 UFO 搬走。
        /// 由 GameInnerUI 的道具3按钮调用。道具在**点下这一组、UFO 出发时**才扣（见 <see cref="ExitPropForceMode"/>）。
        /// </summary>
        public void EnterPropForceMode()
        {
            if (_propForceMode)
                return;

            // UFO 预制体没配就别让进模式 —— 否则玩家点下去只会白扣道具还把像素销毁掉。
            // 这是装配疏漏，宁可当场拦下来（并报错），也不要造成"道具吃了、人没了"。
            if (prop3UfoPrefab == null)
            {
                Debug.LogError("[Prop3] GameController.prop3UfoPrefab 未配置（应指向 Assets/Prefabs/UFO.prefab），道具3 已拒绝进入。");
                return;
            }

            _propForceMode = true;
            if (pixelGroup != null)
                pixelGroup.SetPropForceGlow(true);
        }

        /// <summary>退出道具「强制取出」模式并恢复常规描边。</summary>
        /// <param name="consumed">true = 成功送出一组（扣 1 个道具）；false = 玩家取消或换关重置（不扣）。</param>
        public void ExitPropForceMode(bool consumed)
        {
            if (!_propForceMode)
                return;

            _propForceMode = false;
            if (pixelGroup != null)
                pixelGroup.SetPropForceGlow(false);

            if (!consumed)
                return;

            var ui = UIManager.Instance;
            if (ui != null && ui.gameInnerUI != null)
                ui.gameInnerUI.ConsumeRemovePropForce();
        }

        // ===== 道具3「UFO」演出 =====

        /// <summary>
        /// 道具3「UFO」：把整组像素吸上 UFO →（若像素**真的落在倍乘门区域内**，按各自倍率补齐分身）→
        /// 飞到前4排车上方 → 能匹配前4排的当场吐出跳车 → 匹配到更后排的参考复活瞬移上车 →
        /// 匹配不上任何车的随 UFO 一起销毁并打错误日志 → UFO 飞离消失。
        ///
        /// 复用：匹配登记走 <see cref="ContainerGroup.MatchPixelsToCars"/>（它已按「车是否在前 maxOpenRows 排」
        /// 分好 jumps / disappears），播放走 <see cref="ContainerGroup.PlayBoarding"/>。
        /// 账目：<c>ProgressPixelCount</c> 在 <see cref="ResolveMatch"/> 里已按同一倍率口径记过，这里**不再记**；
        /// <c>ClearedPixelCount</c> 仍由每颗真实像素进车时各记一次（与正常流程一致）。
        /// </summary>
        private IEnumerator Prop3UfoRoutine(List<PixelItem> group)
        {
            if (group == null || group.Count == 0)
                yield break;

            if (prop3UfoPrefab == null || pixelGroup == null || containerGroup == null)
            {
                Debug.LogError("[Prop3] 缺少 prop3UfoPrefab / pixelGroup / containerGroup，道具3 无法演出；这组像素将原地销毁。");
                for (int i = 0; i < group.Count; i++)
                    if (group[i] != null)
                        Destroy(group[i].gameObject);
                yield break;
            }

            _prop3Playing = true;
            _prop3Riders.Clear();   // 每次演出重新登记"乘客"与其冻结朝向

            // ★ 吸人闸门必须在这里就置位，**不能等到"吸人"那一段**：
            // ResolveMatch 一清完格子，管道下一帧就会看到 TrackEmpty()==true 而放蛇；
            // 而从那帧到「吸人」之间还隔着出场飞行的 yield（约 0.45s），中间这段就是没有闸门的窗口。
            // 箱子 / 升降台的释放也已在 ResolveMatch 里按 propForce 推迟（见那里）。
            _prop3SuckPhase = true;

            var cam = Camera.main;

            // 被点组的中心：用格心换算，避免把正在走位的像素算进平均值
            int sumCol = 0, sumRow = 0;
            for (int i = 0; i < group.Count; i++)
            {
                sumCol += group[i].gridX;
                sumRow += group[i].gridZ;
            }
            Vector3 groupCenter = pixelGroup.GetWorldPosition(
                Mathf.RoundToInt((float)sumCol / group.Count),
                Mathf.RoundToInt((float)sumRow / group.Count));

            Vector3 hoverGroup = groupCenter + Vector3.up * prop3HoverAboveGroup;
            Vector3 hoverCars = FrontRowsCenter() + Vector3.up * prop3HoverAboveCars;

            var ufo = Instantiate(prop3UfoPrefab);
            ufo.name = "UFO_Prop3";
            // 不重置朝向：沿用预制体自己的朝向（美术或摄像机调过就按调好的来）
            ufo.transform.position = ViewportToWorldAtDepth(cam, prop3EnterViewportY, groupCenter);
            _prop3Ufo = ufo;

            Vector3 ufoBaseScale = ufo.transform.localScale;   // 吸人脉冲的基准（预制体是 2）

            // 姿态：出场这段把**自身 z 滚转逐渐归 0**（到人上方时已经归正），同时全程沿**自身 Y 轴**匀速自转。
            // 用协程而不是 DOTween —— DORotate 每帧整写旋转，没法既把 z 插值归零、又持续追加 y；
            // 位置走 DOMove、缩放走脉冲，rotation 交给这条协程，三者互不干扰。
            StartCoroutine(Prop3LevelAndSpin(ufo.transform, ufo.transform.rotation));

            // 1) 出场：屏幕下方 → 组正上方
            yield return ufo.transform.DOMove(hoverGroup, prop3FlyDuration).SetEase(Ease.OutCubic).WaitForCompletion();

            // 2) 吸人 + 倍乘
            // 整个吸人周期内**每 prop3PulseInterval 缩一次**：脉冲一直重复，人吸完就停（见下面 Kill）。
            // （吸人闸门 Prop3SuckPhase 已在协程开头置位，覆盖出场飞行那一段）
            var pulse = Prop3StartPulse(ufo.transform, ufoBaseScale);

            var carried = new List<PixelItem>();
            for (int i = 0; i < group.Count; i++)
            {
                var p = group[i];
                if (p == null)
                    continue;

                carried.Add(p);
                StartCoroutine(Prop3SuckOne(p, ufo.transform));

                // 倍乘：只有**真的落在门区域内**的像素才按**它自己的倍率**复制，不多不少
                //（嵌套门各门连乘已由 GateMultiplierAt 体现）
                if (pixelGroup.IsInGateRegion(p.gridX, p.gridZ))
                {
                    int mult = Mathf.Max(1, pixelGroup.GateMultiplierAt(p.gridX, p.gridZ));
                    for (int k = 1; k < mult; k++)
                    {
                        var clone = Prop3SpawnClone(p, ufo.transform);
                        if (clone != null)
                            carried.Add(clone);
                    }
                }

                if (prop3SuckStagger > 0f && i + 1 < group.Count)
                    yield return new WaitForSeconds(prop3SuckStagger);
            }

            yield return new WaitForSeconds(prop3SuckDuration);   // 等最后一颗落位并隐藏

            // 吸人结束：停掉重复脉冲并把缩放复位 —— 它可能被杀在半途中，不复位会留下一个鼓着的 UFO
            if (pulse != null && pulse.IsActive())
                pulse.Kill();
            ufo.transform.localScale = ufoBaseScale;

            // 人都吸完了：这时候才让**箱子 / 升降台**把人放出来（它们的释放被 ResolveMatch 推迟到这里）。
            // 管道不用在这里做 —— 它是逐帧判定，把 _prop3SuckPhase 清掉，下一帧自己就会接着投波。
            _prop3SuckPhase = false;
            pixelGroup.TryOpenBoxes();
            pixelGroup.TryAdvanceElevators();
            pixelGroup.RefreshExposed();   // 释放后暴露变了
            RefreshFrame();                // 描边跟着重建

            // 3) UFO → 前4排车上方
            yield return ufo.transform.DOMove(hoverCars, prop3FlyDuration).SetEase(Ease.InOutSine).WaitForCompletion();

            // 4) 登记：能匹配前4排的 → jumps（吐出来跳车）；匹配到更后排的 → disappears（瞬移）
            var jumps = new List<ContainerGroup.BoardingEntry>();
            var disappears = new List<ContainerGroup.BoardingEntry>();
            var unmatched = containerGroup.MatchPixelsToCars(carried, jumps, disappears);

            // 4a) 吐出：逐颗错开起播。像素此刻就挂在 UFO 下，所以跳车起点正好是 UFO 处
            for (int i = 0; i < jumps.Count; i++)
            {
                var entry = jumps[i];
                if (entry.pixel == null)
                    continue;
                entry.pixel.gameObject.SetActive(true);   // 在 UFO 里是隐藏的；唤醒时 scale 已是原大小
                containerGroup.PlayBoarding(entry);       // timing 用预制体自身时长
                if (prop3SpitInterval > 0f && i + 1 < jumps.Count)
                    yield return new WaitForSeconds(prop3SpitInterval);
            }

            // 4b) 剩余的：参考复活，瞬间飞到各自车上（DisappearWithPop 会还原大小 → 落定仍是原始大小 0.5）
            for (int i = 0; i < disappears.Count; i++)
            {
                var entry = disappears[i];
                if (entry.pixel == null)
                    continue;
                entry.pixel.gameObject.SetActive(true);
                containerGroup.PlayBoarding(entry);
            }

            // 5) 匹配不上任何车的：随 UFO 一起销毁 + 打日志（正常关卡不该出现，出现即关卡数据有问题）
            if (unmatched.Count > 0)
            {
                var sb = new System.Text.StringBuilder();
                sb.Append("[Prop3] 第 ").Append(GameData.CurrentLevel)
                  .Append(" 关有 ").Append(unmatched.Count)
                  .Append(" 颗像素找不到同色车，将随 UFO 一起销毁。颜色：");
                for (int i = 0; i < unmatched.Count; i++)
                    sb.Append(unmatched[i] != null ? unmatched[i].colorId.ToString() : "?").Append(' ');
                Debug.LogError(sb.ToString());
            }

            // 6) UFO 飞离（屏幕上方）→ 销毁（未匹配的像素挂在它下面一起走）
            yield return ufo.transform.DOMove(ViewportToWorldAtDepth(cam, prop3ExitViewportY, hoverCars),
                prop3FlyDuration).SetEase(Ease.InCubic).WaitForCompletion();

            for (int i = 0; i < unmatched.Count; i++)
                if (unmatched[i] != null)
                    Destroy(unmatched[i].gameObject);
            if (ufo != null)
            {
                ufo.transform.DOKill();   // 清掉位置 / 脉冲 tween（自转是协程，靠 while 里的 ufo != null 自行退出）
                Destroy(ufo.gameObject);
            }

            _prop3Ufo = null;
            _prop3Playing = false;
            _prop3Riders.Clear();
        }

        /// <summary>
        /// UFO 姿态：在 <see cref="prop3LevelDuration"/> 内把**自身 z 滚转**逐渐归 0（飞到人上方时已经归正），
        /// 同时全程沿**自身 Y 轴**匀速自转（<see cref="prop3SpinSeconds"/> 一圈，&lt;=0 关掉自转）。
        ///
        /// 用协程而不是 DOTween：<c>DORotate</c> 每帧整写旋转，没法既把 z 插值归零、又持续追加 y。
        /// 位置走 DOMove、缩放走脉冲，rotation 交给这条协程，三者互不干扰。
        /// UFO 被销毁或演出结束时本条自然退出。
        /// </summary>
        private IEnumerator Prop3LevelAndSpin(Transform ufo, Quaternion startRot)
        {
            if (ufo == null)
                yield break;

            // 目标姿态：保留 x / y，只把 z 滚转归 0
            Vector3 e = startRot.eulerAngles;
            Quaternion leveled = Quaternion.Euler(e.x, e.y, 0f);

            float levelDur = Mathf.Max(0.01f, prop3LevelDuration);
            float t = 0f;
            float spin = 0f;

            while (ufo != null && _prop3Playing)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / levelDur);
                if (prop3SpinSeconds > 0.01f)
                    spin += 360f * Time.deltaTime / prop3SpinSeconds;

                // 先插值到「归正」姿态，再在它的基础上绕自身 Y 追加自转
                ufo.rotation = Quaternion.Slerp(startRot, leveled, k) * Quaternion.Euler(0f, spin, 0f);

                // 紧接着把挂上来的人的**世界朝向**压回入舱时的值 —— 否则他们会跟着 UFO 一起转。
                // 必须写在 UFO 旋转之后（同一帧、顺序确定），否则会被上面这行覆盖掉。
                for (int i = 0; i < _prop3Riders.Count; i++)
                {
                    var r = _prop3Riders[i];
                    if (r.pixel == null)
                        continue;
                    var pr = r.pixel.transform;
                    if (pr.parent != ufo)
                        continue;   // 已经上车 / 已脱离 UFO：不再管
                    pr.rotation = r.worldRotation;
                }

                yield return null;
            }
        }

        /// <summary>
        /// UFO 缩放脉冲（**重复**）：基准 → 基准×<see cref="prop3GulpPulse"/> → 基准，每
        /// <see cref="prop3PulseInterval"/> 一轮，一直重复到调用方 Kill。
        /// 用于「整个吸人周期内每 0.15s 缩一次」。
        ///
        /// 返回这个 tween：吸人阶段结束后由调用方 Kill 并把缩放复位（它可能被杀在半途中）。
        /// </summary>
        private Tweener Prop3StartPulse(Transform ufo, Vector3 baseScale)
        {
            if (ufo == null || prop3GulpPulse <= 1.0001f)
                return null;

            float half = Mathf.Max(0.01f, prop3PulseInterval * 0.5f);
            ufo.localScale = baseScale;   // 起点硬复位，避免漂移
            return ufo.DOScale(baseScale * prop3GulpPulse, half)
                      .SetLoops(-1, LoopType.Yoyo)
                      .SetEase(Ease.OutQuad);
        }

        /// <summary>
        /// 吸走一颗像素：挂到 UFO 下，直线移到 UFO 的 <see cref="prop3SuckLocalTarget"/> 处（默认 (0,-0.5,0)），
        /// 到位后隐藏 —— 随 UFO 飞行 / 自转时不参与渲染，投喂前再唤醒。
        ///
        /// ⚠️ **按世界坐标逐帧写**，不用 <c>DOLocalMove</c>：
        /// UFO 全程自转，局部位移会被父物体带着走成**圆弧**（人绕着 UFO 转一大圈），朝向也被带转。
        /// 每帧写 <c>tr.position</c> 让路径是世界空间里的**直线**；<c>tr.rotation</c> 压回入舱瞬间的朝向。
        /// （目标点在 UFO 本地 Y 轴上，所以自转不影响它的世界位置，起手算一次即可。）
        ///
        /// **全程不改缩放**：人保持原本大小（本工程 0.5，即 <c>PixelGroup.unitSize</c>），
        /// 所以投喂时无论是 <c>DisappearWithPop</c> 还是跳车，拿到的都是正确大小，落定还是 0.5。
        /// </summary>
        private IEnumerator Prop3SuckOne(PixelItem pixel, Transform ufo)
        {
            if (pixel == null || ufo == null)
                yield break;

            var tr = pixel.transform;
            tr.DOKill();   // 清掉可能还在跑的闲置归位 tween，避免和下面抢同一个 transform

            // 入舱瞬间的世界朝向：整个吸 + 带飞过程都把它压回来（乘客表也让姿态协程持续维持）
            Quaternion frozen = tr.rotation;
            _prop3Riders.Add(new Prop3Rider { pixel = pixel, worldRotation = frozen });

            Vector3 startWorld = tr.position;
            Vector3 endWorld = ufo.TransformPoint(prop3SuckLocalTarget);

            tr.SetParent(ufo, true);   // 先挂上去（之后要随它飞向车阵）；位置与朝向下面每帧按世界坐标压住

            float dur = Mathf.Max(0.01f, prop3SuckDuration);
            float t = 0f;
            while (t < dur)
            {
                if (tr == null)
                    yield break;
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                float e = k * k * k;   // InCubic（与原来的缓动一致）
                tr.position = Vector3.Lerp(startWorld, endWorld, e);   // 世界空间直线：不受 UFO 自转影响
                tr.rotation = frozen;
                yield return null;
            }

            if (tr != null)
            {
                tr.position = endWorld;
                tr.rotation = frozen;

                // 进舱那一刻 = 人上传送带的反馈：音效 + 轻震动
                // （与 ConveyorBeltZone 里「进入传送带」的处理完全一致，含空判断）
                if (AudioManager.Instance != null)
                    AudioManager.Instance.Play("OnBelt");
                if (GameManager.Instance != null)
                    GameManager.Instance.TriggerVibrate(0);

                tr.gameObject.SetActive(false);   // 到位后藏起来，投喂前再唤醒
            }
        }

        /// <summary>
        /// 倍乘分身：用 <see cref="PixelGroup.SpawnPixel"/> 生成同色（同问号）像素，按正常大小挂在 UFO 下并隐藏
        /// —— 与本体一致，全程不改缩放（见 <see cref="Prop3SuckOne"/>）。
        /// </summary>
        private PixelItem Prop3SpawnClone(PixelItem src, Transform ufo)
        {
            var cfg = GameManager.Instance != null ? GameManager.Instance.colorConfig : null;
            var clone = pixelGroup.SpawnPixel(src.gridX, src.gridZ, src.colorId, cfg,
                scaleZero: false, isQuestion: src.isQuestion);
            if (clone == null)
                return null;

            clone.transform.SetParent(ufo, true);
            clone.transform.localPosition = Vector3.zero;
            clone.SetClickable(false);
            clone.SetWalking(false);
            clone.gameObject.SetActive(false);

            // 和本体一样登记：克隆挂在 UFO 下也不该跟着转
            _prop3Riders.Add(new Prop3Rider { pixel = clone, worldRotation = clone.transform.rotation });
            return clone;
        }

        /// <summary>前 <c>maxOpenRows</c> 排车的中心（世界坐标）—— UFO 投喂时的悬停点。没有车时退回容器组自身位置。</summary>
        private Vector3 FrontRowsCenter()
        {
            if (containerGroup == null)
                return Vector3.zero;

            var grid = containerGroup.grid;
            int rows = Mathf.Min(containerGroup.maxOpenRows, containerGroup.rows);
            Vector3 sum = Vector3.zero;
            int n = 0;

            for (int r = 0; r < rows; r++)
                for (int c = 0; c < containerGroup.columns; c++)
                {
                    var car = grid != null ? grid[c, r] : null;
                    if (car == null)
                        continue;
                    sum += car.transform.position;
                    n++;
                }

            return n > 0 ? sum / n : containerGroup.transform.position;
        }

        /// <summary>
        /// **最前排（<c>row 0</c>）车队的中心**（世界坐标）—— 道具2 磁铁特效的定位点。
        /// 与 <see cref="FrontRowsCenter"/> 的区别：那个取的是前 <c>maxOpenRows</c> 排的平均（UFO 悬停用），
        /// 这个只取第 0 排。没有车时退回容器组自身位置。
        /// </summary>
        public Vector3 FirstRowCenter()
        {
            if (containerGroup == null)
                return Vector3.zero;

            var grid = containerGroup.grid;
            Vector3 sum = Vector3.zero;
            int n = 0;

            for (int c = 0; c < containerGroup.columns; c++)
            {
                var car = grid != null ? grid[c, 0] : null;
                if (car == null)
                    continue;
                sum += car.transform.position;
                n++;
            }

            return n > 0 ? sum / n : containerGroup.transform.position;
        }

        /// <summary>
        /// 视口坐标 → 世界坐标，深度取 <paramref name="depthRef"/> 距相机的距离 ——
        /// 这样"屏幕下方飞出的 UFO"落在棋盘所在平面附近，而不是贴在相机近裁剪面上。
        /// 相机缺失时退化为参照点上下偏移，保证不崩。
        /// </summary>
        private static Vector3 ViewportToWorldAtDepth(Camera cam, float viewportY, Vector3 depthRef)
        {
            if (cam == null)
                return depthRef + Vector3.up * (viewportY < 1f ? -6f : 6f);

            float depth = Mathf.Max(1f, Vector3.Dot(depthRef - cam.transform.position, cam.transform.forward));
            return cam.ViewportToWorldPoint(new Vector3(0.5f, viewportY, depth));
        }

        /// <summary>清理上一关残留：停止自身协程，销毁聚集/传送带/缓冲区中的像素，为重建腾出空间。</summary>
        private void CleanupLevel()
        {
            StopAllCoroutines();
            DestroyPendingRevivePixels();   // 复活序列被打断：清掉队列里还没起播的像素

            // 换关作废道具模式：这关没用掉就不扣道具
            ExitPropForceMode(false);

            // 道具3 UFO 演出：上面的 StopAllCoroutines 会中止协程，但**不会执行协程里的收尾**，
            // 所以残留的 UFO（及其下面还挂着的像素）要在这里兜底销毁，并把状态复位。
            if (_prop3Ufo != null)
            {
                Destroy(_prop3Ufo);
                _prop3Ufo = null;
            }
            _prop3Playing = false;
            _prop3Riders.Clear();
            _prop3SuckPhase = false;   // 协程被 StopAllCoroutines 中止时不走收尾 —— 这个标志必须兜底复位，否则管道永远不放波

            foreach (var item in gatheredItems)
            {
                if (item != null)
                    Destroy(item.gameObject);
            }
            gatheredItems.Clear();

            if (conveyorZone != null)
            {
                conveyorZone.ClearBelt();
                conveyorZone.ResetSpeed();   // 进新关：传送带回到常规速度
            }

            if (crowdBuffer != null)
                crowdBuffer.ResetAll();

            _overflowClickCount = 0;
        }

        // ===== Record 模式 =====

        /// <summary>Record 默认输出目录：编辑器下为工程目录（Assets 的上一级）下的 Record 文件夹；构建时回退 persistentDataPath。</summary>
        private static string DefaultRecordDir()
        {
#if UNITY_EDITOR
            string projectDir = Path.GetDirectoryName(Application.dataPath);
            return Path.Combine(projectDir, "Record");
#else
            return Application.persistentDataPath;
#endif
        }

        /// <summary>
        /// 开启记录：在指定目录（默认工程目录下的 Record 文件夹）新建一个以「关卡 JSON 文件名 + 关卡像素总数」
        /// 命名的序列文件。文件名里的「记录像素数」只有关闭时才知道，故由 <see cref="CloseRecord"/> 改名补上。
        /// </summary>
        private void BeginRecord(string levelName, int totalPixels)
        {
            string dir = string.IsNullOrEmpty(recordOutputDir)
                ? DefaultRecordDir()
                : recordOutputDir;

            try
            {
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                _recordedCount = 0;
                _recordFileBase = "Record_" + levelName + "_total" + totalPixels + "_" +
                    DateTime.Now.ToString("yyyyMMdd_HHmmss");
                _recordFilePath = Path.Combine(dir, _recordFileBase + ".txt");
                _recordWriter = new StreamWriter(_recordFilePath, false, System.Text.Encoding.UTF8);
                Debug.Log("[GameController] Record 模式已开启，序列文件：" + _recordFilePath);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[GameController] 创建记录文件失败：" + e.Message);
                _recordWriter = null;
            }
        }

        /// <summary>记录一颗离开的小球颜色（每行一个 colorId）。</summary>
        public void RecordBall(int colorId)
        {
            if (_recordWriter == null)
                return;
            _recordWriter.WriteLine(colorId);
            _recordWriter.Flush();
            _recordedCount++;
        }

        /// <summary>
        /// 关闭记录文件：落盘后改名带上「记录像素数」；若一个像素都没记录到（例如进关就立刻切走），
        /// 直接删掉这个空文件，不留无意义的空 txt。无文件时为空操作。
        /// </summary>
        private void CloseRecord()
        {
            if (_recordWriter == null)
                return;

            _recordWriter.Flush();
            _recordWriter.Close();
            _recordWriter = null;

            try
            {
                if (_recordedCount == 0)
                {
                    File.Delete(_recordFilePath);
                    Debug.Log("[GameController] 未记录到任何像素，已删除空记录文件：" + _recordFilePath);
                }
                else
                {
                    string finalPath = Path.Combine(
                        Path.GetDirectoryName(_recordFilePath),
                        _recordFileBase + "_rec" + _recordedCount + ".txt");
                    File.Move(_recordFilePath, finalPath);
                    Debug.Log("[GameController] 已关闭记录文件：" + finalPath + "（记录 " + _recordedCount + " 个像素）");
                }
            }
            catch (System.Exception e)
            {
                // 改名 / 删除失败不影响已写入的内容，保留原文件即可
                Debug.LogWarning("[GameController] 记录文件收尾失败（内容完整，保留原文件）：" + e.Message);
            }

            _recordFilePath = null;
            _recordFileBase = null;
            _recordedCount = 0;
        }

        private void OnApplicationQuit()
        {
            CloseRecord();
        }

        private void OnDestroy()
        {
            if (crowdBuffer != null)
                crowdBuffer.OnGridPathfindingFinished -= OnGridPathfindingFinished;
            CloseRecord();
        }

        private void Update()
        {
            UpdateCountText();

            // 胜负判定都已改为事件驱动（失败见 TryCheckFail，胜利见 ContainerGroup.TryCheckWin），不再每帧检测。
            if (Input.GetMouseButtonDown(0) && GameState.IsGameStart)
                HandleClick();
        }

        /// <summary>刷新每帧变化的文本：聚集数量 + 关卡进度（进度与复活 / 失败面板同源同口径，封顶 99%）。</summary>
        private void UpdateCountText()
        {
            int count = CurrentGatherCount();

            if (gatherCountText != null)
            {
                if (conveyorZone != null)
                    gatherCountText.text = count + "/" + conveyorZone.TotalSlots;
                else
                    gatherCountText.text = count.ToString();
            }

            /*// 卷轴只在数值变化时驱动一次（理由见 _lastGatherRollerCount）
            if (gatherCountRoller != null && count != _lastGatherRollerCount)
            {
                _lastGatherRollerCount = count;
                gatherCountRoller.SetTargetNumber(count);
            }*/

            if (progressText != null)
                progressText.text = GameData.ProgressPercent + "%";
        }

        /// <summary>聚集数量口径：有传送带取「已占用槽位」，否则退回聚集点里的单位数。文本与卷轴共用，避免两处口径漂移。</summary>
        private int CurrentGatherCount()
        {
            return conveyorZone != null ? conveyorZone.OccupiedSlots : gatheredItems.Count;
        }

        /// <summary>当前「传送带 + 已点未进带」的总占用数。</summary>
        private int CurrentInflight()
        {
            int onBelt = conveyorZone != null ? conveyorZone.OccupiedSlots : 0;
            int pending = crowdBuffer != null ? crowdBuffer.PendingCount : 0;
            return onBelt + pending;
        }

        /// <summary>
        /// 堆积进入限制：当「传送带上的像素 + 已点击但尚未进入传送带的像素」总数达到传送带容量上限后，
        /// 开始计数被放行的点击；累计 2 次后，若未进传送带球数 &lt;= 9 仍放行，否则阻止（被阻止的点击不计入）；
        /// 总数回落到容量以下则重置计数。
        /// </summary>
        private bool PassOverflowClickGate()
        {
            if (conveyorZone == null || conveyorZone.belt == null || conveyorZone.TotalSlots <= 0)
                return true;   // 无传送带，不限制

            int capacity = conveyorZone.TotalSlots;
            int before = CurrentInflight();
            int countBefore = _overflowClickCount;

            bool allow;
            if (before < capacity)
            {
                _overflowClickCount = 0;   // 总数低于容量：重置累计次数
                allow = true;
            }
            else if (_overflowClickCount < 2)
            {
                _overflowClickCount++;   // 放行的堆积点击才计数（最多累计 2 次）
                allow = true;
            }
            else
            {
                // 次数已达 2：未进传送带球数 <= overflowPendingLimit 仍放行，否则阻止
                int pending = crowdBuffer != null ? crowdBuffer.PendingCount : 0;
                allow = pending <= overflowPendingLimit;
            }

            if (debugClickLog)
                Debug.Log("[Click] 堆积门槛 前总占用=" + before + "/" + capacity +
                    " count=" + countBefore + "→" + _overflowClickCount +
                    (allow ? " → 放行" : " → 忽略"));

            return allow;
        }

        private void HandleClick()
        {
            // =====新增：如果鼠标在UGUI上，直接跳过3D点击，射线不穿透UI=====
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                if (debugClickLog)
                    Debug.Log("[Click] 鼠标在UI上，跳过物理射线");
                return;
            }

            // 道具3「UFO」演出期间锁点击：那组像素正被 UFO 搬走 / 投喂，再点会打乱状态
            if (_prop3Playing)
            {
                if (debugClickLog)
                    Debug.Log("[Click] 道具3 UFO 演出中，忽略点击");
                return;
            }
            // 提取进行中仍允许点击：每次匹配作为独立批次，各自独立寻路（组间可穿模），无需等待上一批离场。
            if (pixelGroup == null || gatherPoint == null || Camera.main == null)
            {
                if (debugClickLog)
                    Debug.Log("[Click] 忽略点击：引用缺失 pixelGroup=" + (pixelGroup != null) +
                        " gatherPoint=" + (gatherPoint != null) + " Camera.main=" + (Camera.main != null));
                return;
            }

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 1000f, _clickMask))
            {
                if (debugClickLog)
                    Debug.Log("[Click] 射线未命中 Click 层（鼠标 " + Input.mousePosition + "）");
                return;
            }

            var listener = hit.collider.GetComponentInParent<PixelClickListener>();
            if (listener == null || listener.pixel == null)
            {
                if (debugClickLog)
                    Debug.Log("[Click] 命中 " + hit.collider.name + " 但无 PixelClickListener（或 pixel 为空）");
                return;
            }
            var item = listener.pixel;

            // 只在仍处于网格中时才触发；能否移出改由 ResolveMatch 判定（同色组需能通过空/组内格连通到首排）
            if (pixelGroup.GetItem(item.gridX, item.gridZ) != item)
            {
                if (debugClickLog)
                    Debug.Log("[Click] 命中 " + item.name + " 但已不在网格（grid[" + item.gridX + "," + item.gridZ + "] != item）");
                return;
            }

            // 问号 Pixel 未揭晓时不可点击（揭晓后等同普通同色像素）
            if (item.isQuestion && !item.revealed)
            {
                if (debugClickLog)
                    Debug.Log("[Click] 命中 " + item.name + " 但为未揭晓问号 Pixel，忽略点击");
                return;
            }

            // ==========【堆积限制移到这里：拿到有效像素item之后才判断】==========
            if (!PassOverflowClickGate())
            {
                if (debugClickLog)
                {
                    Debug.Log("[Click] 堆积限制：已达容量且累计两次点击，忽略本次点击");
                }
                // 只有点到有效像素才弹提示
                UIManager.Instance.ShowTip("排队人数过多，请稍后");
                return;
            }

            // 被木箱盖住的 Pixel：**无任何反馈**直接返回 —— 那里看起来本来就没有像素，点了就该像没点到。
            // 碰撞体是保留的（见 PixelItem.SetCovered）：关掉的话射线会穿过去打中木箱更后面的像素，
            // 那才是真的错。整块木箱矩形都算障碍，所以木箱格上的这种像素不会被别组带走。
            //
            // 这条守卫在冰冻之前：木箱是盖在最上面的，一个格子同时被冰和木箱占住时以木箱为准
            // （否则会打出冰块那套「有反馈但不摆表情」的表现，而木箱的要求是完全没有反馈）。
            //
            // **Record 模式例外**：木箱不再遮挡像素（见 PixelGroup.recordRevealCrates），
            // 箱内像素照常可点——录的是理想取出顺序，不能让木箱把像素藏起来点不到。
            // 求组时箱内像素只在同一木箱内相邻，见 CrateAdjacencyAllowed。
            if (item.IsCovered && !recordMode)
            {
                if (debugClickLog)
                    Debug.Log("[Click] 命中 " + item.name + " 但被木箱盖住，忽略点击（无反馈）");
                return;
            }

            // 被冰组冻住的 Pixel 不可点击：冰冻计数归 0 前，组内像素「视为不暴露」。
            // HandleClick 不看 IsExposed（能否移出由 CanReachFront 判定），所以真正的门槛在这里。
            // 反馈走同一个 PlayBlockedFeedback：音效 / 震动 / 整组阻挡位移都有，
            // 但**不播愤怒表情**（冰块下不摆表情）。
            if (item.IsFrozen)
            {
                if (debugClickLog)
                    Debug.Log("[Click] 命中 " + item.name + " 但被冰组冻住（计数未归 0），播放阻挡反馈（不摆表情）");
                // 抖的是「冰块内与它相连的同色像素」整组，不含冰块外那部分（见 FloodFrozenSameColor）
                PlayBlockedFeedback(FloodFrozenSameColor(item), item, playAngryEmoji: false);
                return;
            }

            if (debugClickLog)
                Debug.Log("[Click] 命中 " + item.name + " 颜色 " + item.colorId + " @(" + item.gridX + "," + item.gridZ +
                    ") 已暴露=" + item.IsExposed + "，进入 ResolveMatch");
            ResolveMatch(item);

            if (debugClickLog)
                Debug.Log("[Click] 堆积门槛 后总占用=" + CurrentInflight() + "/" +
                    (conveyorZone != null ? conveyorZone.TotalSlots : 0) +
                    " count=" + _overflowClickCount);
        }

        /// <summary>
        /// 同色组能否离开 —— 点击的门槛。
        ///
        /// **实现已挪到 <see cref="PixelGroup.CanGroupReachFront"/>**：暴露判定（"发不发白光"）也要用同一条
        /// 判据，放在 PixelGroup 里两边才共用一份实现；各写一份就会分叉，表现就是"点得出去却不发白光"。
        /// </summary>
        private bool CanReachFront(List<PixelItem> matched)
        {
            return pixelGroup != null && pixelGroup.CanGroupReachFront(matched);
        }

        /// <summary>
        /// 点击无法移出的同色组时的反馈：组内像素（含被点像素）同时向前（本地 +Z）匀速晃出一小段，
        /// 再以相同速度回到各自网格位；同时播放 TapBlocked 音效与强度 1 震动，
        /// 并在**被点的那一个像素**上播生气表情（点谁谁生气；必出，同一像素上一张还没播完则忽略——判定在表情管理器里）。
        /// 回位锚点取网格坐标而非当前 localPosition，避免晃动途中被重复点击导致逐次向前漂移。
        /// </summary>
        /// <summary>
        /// 阻挡反馈：音效 + 震动 + （可选）愤怒表情 + 整组朝首排方向推一下再回位。
        /// <paramref name="playAngryEmoji"/> = false 时不播愤怒表情 —— 冰块下的点击用这个，
        /// 其余（被别的像素堵住）保持默认。
        /// </summary>
        private void PlayBlockedFeedback(List<PixelItem> blocked, PixelItem clicked, bool playAngryEmoji = true)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.Play("TapBlocked");
            if (GameManager.Instance != null)
                GameManager.Instance.TriggerVibrate(1);

            var emoji = EmojiManager.Instance;
            if (emoji != null && playAngryEmoji)
                emoji.TryPlayAngryEmoji(clicked);   // 点谁谁生气：必出、无全局 CD

            float distance = Mathf.Max(0f, blockedNudgeDistance);
            float duration = Mathf.Max(0.0001f, blockedNudgeDuration);

            foreach (var item in blocked)
            {
                if (item == null)
                    continue;

                var tr = item.transform;
                Vector3 origin = pixelGroup.GetLocalPosition(item.gridX, item.gridZ);   // 网格位 = 回位锚点
                Vector3 forward = origin + Vector3.forward * distance;                  // 本地 +Z = 朝首排方向

                tr.DOKill();
                tr.DOLocalMove(forward, duration)
                    .SetEase(Ease.Linear)
                    .OnComplete(() =>
                    {
                        if (tr != null)
                            tr.DOLocalMove(origin, duration).SetEase(Ease.Linear);
                    });
            }
        }

        private void ResolveMatch(PixelItem start)
        {
            // 记录模式下按住 S 点击：只移除被点的这一颗，不做同色整组展开
            bool singleRemove = recordMode && Input.GetKey(KeyCode.S);
            List<PixelItem> matched = singleRemove ? new List<PixelItem> { start } : FloodFill(start);

            // 道具「强制取出」：本批要无视阻挡直接飞出去。先记下来 —— 后面的 ExitPropForceMode
            // 会清掉 _propForceMode，而 EnterBatch 在那之后才调用，不能那时再读。
            bool propForce = _propForceMode;

            // 只有能通过空/组内格连通到首排（row 0）的同色组才可移出；否则点击无效（组被其他像素完全包围）
            // 记录模式不做此限制：被包围的组也允许点击（记录的是取出顺序，与组能否寻路无关）
            // 道具「强制取出」模式下跳过这道限制：点哪组都能送出（木箱/冰冻/堆积门槛仍在 HandleClick 里挡着）
            if (!recordMode && !propForce && !CanReachFront(matched))
            {
                if (debugClickLog)
                    Debug.Log("[Click] 点击无效：同色组（大小 " + matched.Count + "，颜色 " + start.colorId +
                        "）无法通过空/组内格连通到首排（组被其他像素/墙体/管道包围）");
                PlayBlockedFeedback(matched, start);
                return;
            }

            // 点击确认可移出：播放点击音效 + 震动（每次点击一次，不按像素数）
            if (AudioManager.Instance != null)
                AudioManager.Instance.Play("Tap");
            if (GameManager.Instance != null)
                GameManager.Instance.TriggerVibrate(1);

            // 移出前先收掉组内像素的生气表情（跟随模式下它是像素的子物体，不主动收会跟着一起走）
            var emoji = EmojiManager.Instance;
            if (emoji != null)
            {
                for (int i = 0; i < matched.Count; i++)
                    emoji.RemoveAngryEmoji(matched[i]);
            }

            // 同一次匹配内排序：前排优先（gridZ 小），同排靠中心优先（供 CrowdBufferZone 提取阶段前到后寻路使用）
            matched.Sort((a, b) =>
            {
                int zcmp = a.gridZ.CompareTo(b.gridZ);
                if (zcmp != 0)
                    return zcmp;
                float center = (pixelGroup.columns - 1) * 0.5f;
                float da = Mathf.Abs(a.gridX - center);
                float db = Mathf.Abs(b.gridX - center);
                int dcmp = da.CompareTo(db);
                if (dcmp != 0)
                    return dcmp;
                return a.gridX.CompareTo(b.gridX);
            });

            // 冰冻：先记下「点击前」各冰组是否已暴露。「暴露才开始融化」要拿它判断，
            // 所以必须在这个点击的任何副作用之前（开箱 / 升降台推进也会各自刷新暴露状态）。
            pixelGroup.CaptureIceExposedSnapshot();

            // 从网格移除（匹配格先置空，并关闭其暴露状态与点击碰撞体，开始走动画）
            // 同一次点击移出的整组共享一个点击序号，用于传送带入口的「插队」判定
            int clickSeq = ++_clickSeq;
            foreach (var item in matched)
            {
                item.clickSeq = clickSeq;
                pixelGroup.grid[item.gridX, item.gridZ] = null;
                // 从网格移出即算「已点出」：一次点击计入整组像素数（不是点击数）
                GameData.RemovePixelCount++;
                // 进度分子用**计划口径**累加：乘上像素所在格的倍率，把倍乘门裂变出来的分身一并算上。
                // 分身在缓冲区生成（CrowdBufferZone.SpawnGateClone）、从不在网格上被点击，
                // 不补这一笔分子就永远追不上含 CountGateExtraPixels 的分母。
                // 口径与记录模式按倍率补记 N 份、与 PixelGroup.CollectPlanningSources 同源。
                GameData.ProgressPixelCount += Mathf.Max(1, pixelGroup.GateMultiplierAt(item.gridX, item.gridZ));
                item.SetExposed(false);
                item.SetPropGlow(false);   // 道具高亮一并复位：它已离格，不该顶着描边走上传送带
                item.SetClickable(false);
                if (!recordMode)
                    item.SetWalking(!propForce);   // 道具3：被 UFO 搬走，不播走路（其余照旧走上传送带）
            }

            // 道具「强制取出」：一组成功送出即算用掉 → 退出模式并扣 1 个道具
            if (_propForceMode)
                ExitPropForceMode(true);

            // 匹配移除后，先让箱子/升降台释放像素占格（占格同步、动画异步），
            // 再统一刷新暴露状态：避免「移除后短暂暴露的像素紧接着被释放像素封路」却已经站起。
            //
            // ⚠️ 道具3（propForce）例外：箱子 / 升降台是"人"的来源，它们的释放**推迟到吸人之后**
            // （见 Prop3UfoRoutine）—— 否则 UFO 还在吸这组人，盒子里 / 升降台里的人就先冒出来了。
            if (!propForce)
            {
                pixelGroup.TryOpenBoxes();
                pixelGroup.TryAdvanceElevators();
            }
            pixelGroup.RefreshExposed();
            RefreshFrame();

            // 记录模式：像素原地消失，按取出顺序（前到后、中间优先）写入序列文件，不进入缓冲区 / 传送带
            if (recordMode)
            {
                for (int i = 0; i < matched.Count; i++)
                {
                    var item = matched[i];

                    // 倍乘门：这条路径**不经过缓冲区**，裂变（CrowdBufferZone.SpawnGateClone）永远不触发，
                    // 所以按「所在格倍率」补记 N 份——否则记录条数比 TotalPixelCount 少
                    // （后者含 CountGateExtraPixels），文件名里的 total 与 rec 就对不上了。
                    // 口径与 PixelGroup.CollectPlanningSources 一致：查的是**像素所在格**，
                    // 区域内的格按所属各门连乘（嵌套门），区域外为 1。
                    // 顺序上把 N 份紧挨着写：真实裂变也是本体先出门格、分身随后一个个离开。
                    int mult = pixelGroup.GateMultiplierAt(item.gridX, item.gridZ);
                    for (int k = 0; k < mult; k++)
                        RecordBall(item.colorId);

                    Destroy(item.gameObject);
                }
                return;
            }

            // 一次成功的「点击移出」= 冰的计数消耗一次（按点击算，不按像素数）。
            // 放在记录模式的提前返回之后：记录模式只记取出顺序、不玩冰的消耗。
            pixelGroup.NotifyClickMovedOut();

            // 木箱：与本次移出像素上下左右（4 邻）相接的木箱各计一次（同组同时移出只算一次）；
            // 计满的当场拆掉，底下像素恢复可见。返回 true = 有木箱被拆 → 整体描边要重画
            // （上面的 RefreshFrame 在这之前，那时木箱还盖着）。
            if (pixelGroup.NotifyPixelsMovedOut(matched))
                RefreshFrame();

            // 网格已空**且场上没有待产出的像素**（管道还有波次 / 木箱还有未释放像素 / 升降台还有未升起的组）：
            // 传送带逐渐加速，把带上的存量尽快送进容器。
            // 缺了后半句就会在「刚点掉封路像素、生产者还没补位」的那一帧误开加速（管道等的正是这一下点击，
            // 它要到下一次 Update 才把整波 pixel 写回 grid），而加速是本关内不回退的闩锁。
            // 复位只发生在进下一关 / 重载关卡的 CleanupLevel。
            if (conveyorZone != null && pixelGroup.IsGridEmpty() && !pixelGroup.HasPendingProducers())
                conveyorZone.NotifyGridEmptied();

            // 道具3「UFO」：整组交给 UFO 演出（吸走 → 投喂前4排 → 剩余瞬移上车）。
            // **不走网格寻路、不进传送带**，所以不调 EnterBatch —— 人由 UFO 直接送进车。
            if (propForce)
            {
                StartCoroutine(Prop3UfoRoutine(matched));
            }
            // 有缓冲区：进入提取阶段（网格寻路离开）；像素离开后后方不再补位
            // 否则：回退到旧的直接散布聚集
            else if (crowdBuffer != null)
            {
                crowdBuffer.EnterBatch(matched, pixelGroup);
            }
            else
            {
                foreach (var item in matched)
                    GatherItem(item);
            }
        }

        private List<PixelItem> FloodFill(PixelItem start)
        {
            var result = new List<PixelItem>();
            var visited = new HashSet<PixelItem>();
            var queue = new Queue<PixelItem>();

            queue.Enqueue(start);
            visited.Add(start);
            int color = start.colorId;

            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                result.Add(cur);

                // 4 邻内联（原来是 GetNeighbors 迭代器：每遍历一个像素都分配一个状态机 + 两个 int[4]）
                for (int d = 0; d < 4; d++)
                {
                    var nb = pixelGroup.GetItem(cur.gridX + Dx4[d], cur.gridZ + Dz4[d]);
                    if (nb == null || nb.colorId != color)
                        continue;
                    // 未揭晓问号 Pixel 断开连通：不参与移除、不扩散
                    if (nb.isQuestion && !nb.revealed)
                        continue;
                    // 被冰冻住的 Pixel 同样断开连通：不参与移除、不扩散。
                    // 于是点冰旁边的同色像素时，冰里的像素不会被一起带走——必须等冰化开。
                    if (nb.IsFrozen)
                        continue;
                    // 木箱的连通口径（见 CrateAdjacencyAllowed）：
                    // 非 Record 模式完全断开；Record 模式只在同一木箱内相邻。
                    // 这条守卫是必须的 —— 木箱格虽然算障碍，但 FloodFill 根本不看 IsBlocked
                    // （同色相邻就连上），漏了它就会点木箱旁边的同色像素时把木箱里的像素也一起移出去。
                    if (!CrateAdjacencyAllowed(cur, nb))
                        continue;
                    if (visited.Add(nb))
                        queue.Enqueue(nb);
                }
            }

            return result;
        }

        /// <summary>
        /// 木箱处「这两颗像素算不算相邻」的口径：<paramref name="b"/> 能不能被 <paramref name="a"/> 连通到。
        ///
        /// · **非 Record 模式**：木箱像素完全断开连通 —— 与原来的 `if (nb.IsCovered) continue;` 等价。
        ///   点木箱旁边的同色像素不会把箱里的带走，点箱里的也不会带出箱外的。
        /// · **Record 模式**（木箱不再遮挡像素，见 <see cref="PixelGroup.recordRevealCrates"/>）：
        ///   连通性被限制在**同一个木箱内** —— 两颗都不在木箱里 → 相邻（正常规则）；
        ///   同属一个木箱 → 相邻（箱内自成一个连通岛，点一颗能带走同箱同色的整片）；
        ///   一颗在箱里、一颗在箱外 → 不相邻。
        ///
        /// 分属两个木箱也算不相邻：两个木箱的格集合互不重叠，但边缘可以紧挨着，
        /// 那不属于「箱内相邻」。这也是 Record 模式的取舍——录的是理想取出顺序，
        /// 箱内像素被当成一块独立的同色块来处理。
        /// </summary>
        private bool CrateAdjacencyAllowed(PixelItem a, PixelItem b)
        {
            if (!recordMode)
                return !b.IsCovered;

            var ca = pixelGroup.CrateAt(a.gridX, a.gridZ);
            var cb = pixelGroup.CrateAt(b.gridX, b.gridZ);
            if (ca == null && cb == null)
                return true;
            return ca != null && ca == cb;
        }

        /// <summary>
        /// 冰冻反馈专用的求组：从被点的像素出发，**只在冻住的格里**扩散同色像素 ——
        /// 也就是「冰块内相连的同色像素」整组，**不含**冰块外那部分同色像素。
        ///
        /// 与 <see cref="FloodFill"/> 只差一个条件、方向正好相反：那边在冻住的格处**断开**，
        /// 这边只在冻住的格里**连通**。两个不能混用 —— 用错就会把冰块外的同色一起抖起来。
        /// </summary>
        private List<PixelItem> FloodFrozenSameColor(PixelItem start)
        {
            var result = new List<PixelItem>();
            if (start == null)
                return result;

            var visited = new HashSet<PixelItem>();
            var queue = new Queue<PixelItem>();

            queue.Enqueue(start);
            visited.Add(start);
            int color = start.colorId;

            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                result.Add(cur);

                // 4 邻内联（同 FloodFill，消掉迭代器与 int[4] 分配）
                for (int d = 0; d < 4; d++)
                {
                    var nb = pixelGroup.GetItem(cur.gridX + Dx4[d], cur.gridZ + Dz4[d]);
                    if (nb == null || nb.colorId != color)
                        continue;
                    if (nb.isQuestion && !nb.revealed)
                        continue;   // 与 FloodFill 一致：未揭晓问号断开连通
                    if (!nb.IsFrozen)
                        continue;   // ← 只穿冻住的格（FloodFill 是「不穿」）
                    if (visited.Add(nb))
                        queue.Enqueue(nb);
                }
            }

            return result;
        }

        private void GatherItem(PixelItem item)
        {
            // 关闭碰撞体，避免再次被点击
            var col = item.GetComponent<Collider>();
            if (col != null)
                col.enabled = false;

            item.transform.SetParent(gatherPoint, true);
            gatheredItems.Add(item);

            StartCoroutine(MoveToGatherPoint(item));
        }

        private IEnumerator MoveToGatherPoint(PixelItem item)
        {
            Vector3 start = item.transform.localPosition;
            Vector3 target = RandomGatherTarget();

            float duration = gatherSpeed > 0.0001f
                ? Vector3.Distance(start, target) / gatherSpeed
                : 0f;

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(duration > 0.0001f ? t / duration : 1f);
                item.transform.localPosition = Vector3.Lerp(start, target, k);
                yield return null;
            }

            item.transform.localPosition = target;
            item.arrivedAtGatherPoint = true;
            item.SetWalking(false);   // 抵达聚集点后相对静止 → Idle（回退无传送带路径）
        }

        private Vector3 RandomGatherTarget()
        {
            Vector2 circle = UnityEngine.Random.insideUnitCircle * gatherScatterRadius;
            return new Vector3(circle.x, 0f, circle.y);
        }

    }
}
