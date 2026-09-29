using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 管理一个 columns × rows 的 ContainerItem 组。
    /// 位于游戏画面上方（Z 较大一方），以 Z 最小为最前排；前排 Z = 父物体 0 点，后排向 +Z 延展。
    /// 运行时监控聚集 List，把匹配颜色的 PixelItem 移动到最前排 ContainerItem 并消耗容量；
    /// 最后一个 PixelItem 到达时容器消失，后排依次向前补位。
    /// </summary>
    public class ContainerGroup : MonoBehaviour
    {
        [Header("布局")]
        [Tooltip("ContainerItem 预制体模板")]
        public ContainerItem containerPrefab;

        [Tooltip("横向（X 方向）数量")]
        public int columns = 5;

        [Tooltip("纵向（Z 方向）数量，0 为最前排")]
        public int rows = 3;

        [Tooltip("横向间距（X）")]
        public float xSpacing = 1.2f;

        [Tooltip("纵向间距（Z）")]
        public float zSpacing = 1.2f;

        [Header("洗牌")]
        [Tooltip("是否洗牌容器排列（与 JSON 里的 lockContainer 是同一个开关）：导出时取反写入 lockContainer，导入时从 lockContainer 反向读回。运行时实际是否洗牌以关卡 JSON 为准。")]
        public bool shuffleContainers = true;

        [Header("生成参数（编辑器用）")]
        [Tooltip("读取颜色分布的 PixelGroup，留空自动查找")]
        public PixelGroup pixelGroup;

        [Tooltip("单个容器最小容量")]
        public int minCapacity = 2;

        [Tooltip("单个容器最大容量")]
        public int maxCapacity = 5;

        [Tooltip("生成时每个容器最多跨多少像素深度层抽取同色（0 = 仅最前排像素层）")]
        public int maxSpanLayers = 4;

        [Tooltip("最多开启匹配的前排数（前 N 排可同时匹配，默认 4 = 最前排 + 后三排）")]
        public int maxOpenRows = 4;

        [Header("懒实例化（运行模式）")]
        [Tooltip("运行模式开局实例化的排数。实际生效 = max(该值, maxOpenRows + 1)：可开盖匹配的窗口必须全部已实例化，否则失败判定看不到车、关卡会卡住。")]
        public int instantiateRows = 5;

        [Tooltip("容器对象池 tag：需在 SpawnPoolConfig 里注册同名条目（预制体 = containerPrefab）。未注册时自动回退成直接 Instantiate，只报一条 warning。")]
        public string containerPoolTag = "Bus";

        [Header("速度")]
        [Tooltip("PixelItem 移向容器的速度")]
        public float consumeSpeed = 10f;

        [Tooltip("容器补位速度")]
        public float refillSpeed = 10f;

        [Header("绳子连接")]
        [Tooltip("绳子材质（Assets/CrowdMatch/Materials/Rope.mat）")]
        public Material ropeMaterial;

        [Tooltip("每条绳的绳节数量：越大越顺滑，代价是蒙皮开销")]
        public int ropeLinkCount = 8;

        [Tooltip("绳子直径（世界单位）")]
        public float ropeDiameter = 0.15f;

        [Tooltip("绳中段最大偏移（世界单位）：绷直驱动之上叠加的程序化微晃。0 = 完全绷直不动")]
        public float ropeSwayAmplitude = 0.05f;

        [Tooltip("微晃频率（Hz）：每秒往复多少次")]
        public float ropeSwayFrequency = 1.2f;

        [Tooltip("微晃沿绳长的波数：1 = 一个弓形（钟摆感），>1 = 多段起伏")]
        public float ropeSwayWaves = 1f;

        [Tooltip("微晃的纵向分量占比：0 = 只左右摆，1 = 只上下摆")]
        public float ropeSwayVerticalRatio = 0.45f;

        [Tooltip("绳节所在层名。该层需先在 Tags and Layers 中创建，否则绳节会回退到 Default 并与像素互撞")]
        public string ropeLayerName = "Rope";

        [Tooltip("绳子总开关（调试用：关掉可先单独验证出库逻辑）")]
        public bool ropeEnabled = true;

        [Tooltip("绳组出库间隔（秒）：头车 → 第一个后车的间隔（与「后车之间」的间隔独立配置）。0 = 头车与第一个后车同一帧出")]
        public float ropeExitHeadGap = 0.2f;

        [Tooltip("绳组出库间隔（秒）：后车之间（第 2 辆起）每辆比前一辆晚这么多。0 = 全组后续同一帧出")]
        public float ropeExitStagger = 0.2f;

        /// <summary>
        /// 运行时绳组：ropeGroupId → 组内车（按列 gridX 升序，便于相邻两两成链）。
        /// 洗牌开启 / 未启用绳子时保持为空——此时绳组不参与任何判定，车各自独立出库。
        /// </summary>
        [System.NonSerialized] private readonly Dictionary<int, List<ContainerItem>> _ropeGroups =
            new Dictionary<int, List<ContainerItem>>();

        /// <summary>运行时生成的绳根，关卡重建时一并清掉。</summary>
        [System.NonSerialized] private readonly List<GameObject> _ropeRoots = new List<GameObject>();

        /// <summary>运行时网格 [column, row]，row 0 为最前排</summary>
        [System.NonSerialized] public ContainerItem[,] grid;

        /// <summary>正在上车（jump 或回退 lerp）尚未落定的像素计数。失败判定用它做「静止门槛」。</summary>
        [System.NonSerialized] public int consumingCount;

        /// <summary>
        /// 板上「未完成匹配」的车数（= 车上还有容量没被填满的车数）。胜利判定用它做 O(1) 过滤：
        /// 归零才值得做一次全盘复核。<see cref="RebuildGrid"/> 按盘面重算，其余时候由 <see cref="ConsumeCar"/> 递减。
        /// </summary>
        [System.NonSerialized] private int _unfinishedCars;

        /// <summary>
        /// 数据层：[row * columns + col]。**只有运行模式的关卡加载（<see cref="ApplyContainerData"/>）
        /// 之后才是权威**；编辑模式 / 场景里手摆的车走 <see cref="BuildCellsFromView"/>，只是视图的镜像。
        /// 读一律走下面的取值器（有实例读实例、无实例读它），不要在别处直接摸这个数组。
        /// </summary>
        [System.NonSerialized] private ContainerCell[] _cells;

        /// <summary>数据层是否权威（true = 懒实例化生效：只有视窗内的格子有实例）。</summary>
        [System.NonSerialized] private bool _dataAuthoritative;

        /// <summary>关卡加载时传入的颜色配置（懒实例化补造车时要按它上色）。</summary>
        [System.NonSerialized] private ColorConfig _containerConfig;

        /// <summary>预制体层级快照（池化复用复位用；首次需要时从 containerPrefab 抓取）。</summary>
        [System.NonSerialized] private ContainerViewTemplate _viewTemplate;

        private bool _warnedNoPoolTag;
        private bool _warnedNoTemplate;

        /// <summary>视窗深度（排）：实际生效 = max(instantiateRows, maxOpenRows + 1)。</summary>
        private int WindowRows { get { return Mathf.Max(instantiateRows, maxOpenRows + 1); } }

        // ===== 数据层取值器：有实例读实例，无实例读数据层 =====
        //
        // 「有实例的一律以实例为准」是这套设计的关键：实例在场时数据层**完全不参与**读写，
        // 于是不存在两份状态互相漂移的可能；数据层只在「这格还没实例化」时说话。

        private bool HasData { get { return _dataAuthoritative && _cells != null; } }

        private int CellIndex(int col, int row) { return row * columns + col; }

        private bool CellInRange(int col, int row)
        {
            return HasData && col >= 0 && col < columns && row >= 0 && row < rows;
        }

        /// <summary>该格是否有车——**未实例化的深排车也算**。</summary>
        private bool CarAt(int col, int row)
        {
            if (GetItem(col, row) != null)
                return true;
            return CellInRange(col, row) && _cells[CellIndex(col, row)].occupied;
        }

        /// <summary>该格的车是否已装满（已无剩余容量）。无车也返回 true（视作「不阻塞」）。</summary>
        private bool EmptyAt(int col, int row)
        {
            var it = GetItem(col, row);
            if (it != null)
                return it.IsEmpty;
            if (!CellInRange(col, row))
                return true;
            return _cells[CellIndex(col, row)].remaining <= 0;
        }

        /// <summary>该格的车接受的颜色 ID；无车返回 -1。</summary>
        private int ColorAt(int col, int row)
        {
            var it = GetItem(col, row);
            if (it != null)
                return it.colorId;
            return CellInRange(col, row) ? _cells[CellIndex(col, row)].colorId : -1;
        }

        /// <summary>该格的盖子是否已打开（深排车的开盖状态记在数据层）。</summary>
        private bool LidOpenedAt(int col, int row)
        {
            var it = GetItem(col, row);
            if (it != null)
                return it.lidOpened;
            return CellInRange(col, row) && _cells[CellIndex(col, row)].lidOpened;
        }

        private void Start()
        {
            RebuildGrid();
        }

        public void RebuildGrid()
        {
            grid = new ContainerItem[columns, rows];
            foreach (var item in GetComponentsInChildren<ContainerItem>())
            {
                if (IsInRange(item.gridX, item.gridZ))
                {
                    grid[item.gridX, item.gridZ] = item;
                    item.group = this;
                    if (item.gridZ == 0)
                        item.HideLid();   // 初始就在第一排：盖子直接隐藏
                }
            }
            // 数据层维护：
            // · 非懒实例化（编辑模式 / 场景里手摆的车）：重建为视图的镜像，取值器结果与从前逐字一致；
            // · 懒实例化：数据层由 ApplyContainerData 建好，这里只补齐视窗（幂等，
            //   同时兜住 ContainerGroup.Start 与关卡加载的先后顺序）。
            if (_dataAuthoritative)
                EnsureWindow();
            else
                BuildCellsFromView();

            _unfinishedCars = CountUnfinishedCars();   // 换盘后按盘面重算（生成 / 导入 / 洗牌 / 重载都走这里）
        }

        /// <summary>
        /// 把数据层建成**视图的镜像**（编辑模式 / 场景里手摆的车用）。
        /// 这些路径下每格都有实例，于是取值器的「有实例读实例」分支恒成立，行为与改动前逐字一致。
        /// </summary>
        private void BuildCellsFromView()
        {
            _cells = new ContainerCell[Mathf.Max(1, columns * rows)];

            for (int col = 0; col < columns; col++)
                for (int row = 0; row < rows; row++)
                {
                    var it = grid != null ? grid[col, row] : null;
                    if (it == null)
                        continue;

                    _cells[CellIndex(col, row)] = new ContainerCell
                    {
                        col = col,
                        row = row,
                        originCol = col,   // 非懒实例化：格子不动，出生格就是当前格
                        originRow = row,
                        occupied = true,
                        colorId = it.colorId,
                        capacity = it.capacity,
                        remaining = it.Remaining,
                        ropeGroupId = it.ropeGroupId,
                        isQuestion = it.isQuestion,
                        lidOpened = it.lidOpened,
                        revealed = it.revealed,
                    };
                }
        }

        /// <summary>
        /// 列内留洞检查：每列的车必须从 row 0 起**压紧连续**（生成 / 导入 / 拖移窗口都保证这一点）。
        ///
        /// 为什么必须压紧：胜利判定的兜底依赖「深排车原地消失 ⇒ 同列还有浅排车 ⇒ 那些车迟早走前排出库（倒车）」，
        /// 列里夹空格会让这个前提不成立，兜底就可能漏掉一次判胜。
        /// 判定 = 某列存在「空格的下方还有车」（占用行不是 0..k-1 的前缀）。
        /// 返回描述（无洞返回 null），调用方自行弹窗 / 报错。
        /// </summary>
        public string DescribeColumnHoles(int maxColumns = 4)
        {
            if (grid == null && !HasData)
                return null;

            string msg = null;
            int found = 0;
            for (int c = 0; c < columns && found < maxColumns; c++)
            {
                int firstEmpty = -1;
                for (int r = 0; r < rows; r++)
                {
                    if (!CarAt(c, r))
                    {
                        if (firstEmpty < 0)
                            firstEmpty = r;
                        continue;
                    }
                    if (firstEmpty < 0)
                        continue;   // 车还是连续的
                    // 洞之后又有车：列内有洞
                    string one = "列 " + c + "（row " + firstEmpty + " 空，row " + r + " 有车）";
                    msg = msg == null ? one : msg + "；" + one;
                    found++;
                    break;
                }
            }
            return msg;
        }

        public bool IsInRange(int col, int row)
        {
            return col >= 0 && col < columns && row >= 0 && row < rows;
        }

        public ContainerItem GetItem(int col, int row)
        {
            if (grid == null)
                return null;
            if (!IsInRange(col, row))
                return null;
            return grid[col, row];
        }

        /// <summary>
        /// 某格小车是否已「开启匹配」：处于前 maxOpenRows 排，且前方（row 更小）的车全部已**放行**。
        ///
        /// 「放行」的判据统一在 <see cref="IsRowReleased"/> —— 失败判定里的 <see cref="IsFrontCleared"/>
        /// 用的是同一个谓词，两处**必须一致**：前者决定「能不能开盖收像素」，后者决定「算不算还有进度、先别判失败」。
        /// </summary>
        public bool IsOpen(int col, int row)
        {
            if (row >= maxOpenRows)
                return false;
            for (int r = 0; r < row; r++)
            {
                if (!IsRowReleased(col, r))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 前方这一格是否已**放行**——即它后面的车可以接着它开放（收像素）、接着它补位到前排。
        ///
        /// 「空」不等于「放行」：绳车装满之后如果同组还没齐，它是不会走的
        /// （<see cref="TryExitIfAtFront"/> 直接返回，留在前排占位），这段等待期里它必须继续当阻塞物。
        /// 漏掉这一条会同时坏掉两件事：
        ///   · **暴露**：后排开盖把像素吸走，本该留给同组其它车的颜色被吞掉，绳组永远凑不齐（<see cref="IsWaitingRopeCar"/>）；
        ///   · **失败判定**：后排被误判成「即将补位到前排」，于是 <see cref="HasPendingFrontTransition"/>
        ///     在被绳车堵死的列上恒为真 → <c>IsFail</c> 提前返回 false → **该判失败时不判、关卡卡住**。
        /// </summary>
        private bool IsRowReleased(int col, int row)
        {
            if (!CarAt(col, row))
                return true;                    // 空格子：没有阻挡
            if (!EmptyAt(col, row))
                return false;                   // 还没找全匹配对象
            // 装满但在等同组的绳车：仍未放行。
            // 未实例化的深排车 GetItem 为 null，IsWaitingRopeCar 恒 false——
            // 那是安全的：生效中的绳组只可能由已实例化的车构成（建绳只认视窗内的车）。
            return !IsWaitingRopeCar(GetItem(col, row));
        }

        /// <summary>某格子的本地坐标：X 居中，前排（row 0）Z = 0，后排向 +Z 延展</summary>
        public Vector3 GetLocalPosition(int col, int row)
        {
            float x = (col - (columns - 1) * 0.5f) * xSpacing;
            float z = row * zSpacing;
            return new Vector3(x, 0f, z);
        }

        private void Update()
        {
            ProcessConsumption();
        }

        private void ProcessConsumption()
        {
            var gc = GameController.Instance;
            if (gc == null || gc.gatheredItems == null)
                return;

            for (int col = 0; col < columns; col++)
            {
                for (int row = 0; row < rows; row++)
                {
                    var item = GetItem(col, row);
                    if (item == null || item.IsEmpty || item.isRefilling)
                        continue;

                    // 只有满足「处于前 maxOpenRows 排」且「前方没有车 / 前方全部找全匹配对象」才开启匹配
                    if (!IsOpen(col, row))
                        continue;

                    // 首次满足条件时播放开盖动画（幂等）
                    item.OpenLid();

                    var pixel = FindMatchingPixel(gc.gatheredItems, item.colorId);
                    if (pixel == null)
                        continue;

                    gc.gatheredItems.Remove(pixel);
                    bool isLast = item.Consume();
                    if (isLast)
                        OnLastBoarding(item, pixel);   // 最后一个像素准备上车
                    StartCoroutine(MovePixelToContainer(pixel, item, col, isLast));
                }
            }
        }

        private PixelItem FindMatchingPixel(List<PixelItem> list, int colorId)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var p = list[i];
                if (p != null && p.arrivedAtGatherPoint && p.colorId == colorId)
                    return p;
            }
            return null;
        }

        /// <summary>
        /// 传送带推送模式：找某列正前方（远侧）同色的「可匹配」容器；无则 null。
        /// 从最前排（row 0）向后逐排找第一个「可匹配（IsOpen）且非空且同色」的容器（最多 maxOpenRows 排）。
        /// 由 ConveyorBeltZone 在像素越过该列匹配闸口的瞬间按列调用（闸口法，不再做逐帧横向距离判定）；
        /// 判定以前排（row 0）槽位为准——像素始终被送到前排，后排只是接力匹配。
        /// 补位移动中的车（isRefilling）也算可匹配：它的格子在前移开始时就已改写为前排，座位挂在车身下，
        /// 像素上车后会随车继续前移（jump 落点跟随座位，无需额外处理）；代价是这期间上车的像素不播落地弹性
        /// （PlayBoardElastic 被 _rollPhase 挡住），且出库仍要等这辆车补位结束（TryExitIfAtFront 的 isRefilling 门控）。
        /// </summary>
        public ContainerItem FindMatchableInColumn(int col, int colorId)
        {
            int limit = Mathf.Min(rows, maxOpenRows);
            for (int row = 0; row < limit; row++)
            {
                var item = GetItem(col, row);
                if (item == null || item.IsEmpty || item.colorId != colorId)
                    continue;
                if (!IsOpen(col, row))
                    continue;
                return item;
            }
            return null;
        }

        /// <summary>
        /// 传送带推送模式：吸收一个像素——扣容量 → 像素上车（有空闲落点则 LocalJump + 弹性缩放并保留为乘客，否则回退 Lerp 后销毁）→ 若耗尽则补位。
        /// 开头用 IsEmpty 兜底（见 review H1/M1），避免同帧竞态下重复消费。
        /// </summary>
        public void ConsumePixel(PixelItem pixel, ContainerItem container)
        {
            if (pixel == null || container == null || container.IsEmpty)
                return;

            bool isLast = ConsumeCar(container);
            if (isLast)
            {
                OpenRearLidAfterMatch(container);   // 播放移入动画前，先开后盖（绳组在整组装满这一刻整组一起开）
                OnLastBoarding(container, pixel);   // 最后一个像素准备上车
            }
            consumingCount++;
            StartCoroutine(MovePixelToContainer(pixel, container, container.gridX, isLast));
        }

        /// <summary>
        /// 扣容量并登记「完成匹配」：<c>Consume()</c> 返回 true（本像素是这辆车的最后一颗）即视为该车完成匹配。
        /// **全工程只有这一个「完成匹配」登记点**——传送带路径与复活路径都从这里过。
        /// </summary>
        private bool ConsumeCar(ContainerItem container)
        {
            bool isLast = container.Consume();
            if (isLast)
            {
                _unfinishedCars--;
                TryCheckWin(GameController.WinCheckpoint.CarMatched);
            }
            return isLast;
        }

        /// <summary>
        /// 胜利检查（**事件驱动**）：口径 = 板上不存在「未完成匹配」的车。
        ///
        /// 先用 <see cref="_unfinishedCars"/> 做 O(1) 过滤——绝大多数事件发生时它都 &gt; 0，一步返回、不做遍历；
        /// 只有过滤通过（= 计数已归零）才做一次全盘复核，并把计数**重算回真值**：**偏小**时这一步会把它纠回来
        /// （否则会误判胜）；**偏大**由 <see cref="RebuildGrid"/> 每关重算兜底——「完成匹配」只有
        /// <see cref="ConsumeCar"/> 一个登记点，正常运行不会偏大。判定本身交给 <see cref="GameController.CheckWin"/>。
        /// </summary>
        public void TryCheckWin(string checkpoint)
        {
            if (grid == null)
                return;                       // 盘面还没建起来，不判胜
            if (_unfinishedCars > 0)
                return;                       // 过滤：还有车没匹配完，不用遍历

            int actual = CountUnfinishedCars();
            if (actual != _unfinishedCars)
                _unfinishedCars = actual;      // 复核并纠正漂移
            if (actual > 0)
                return;

            var gc = GameController.Instance;
            if (gc != null)
                gc.CheckWin(checkpoint);
        }

        /// <summary>
        /// 扫描盘面统计「未完成匹配」的车数（未出库、还有容量没填满的车）。
        /// 走数据层取值器，**含还没实例化的深排车**——按视图统计会漏掉它们，
        /// 计数偏小就会让 <see cref="TryCheckWin"/> 的 O(1) 过滤直接放行、误判通关。
        /// </summary>
        private int CountUnfinishedCars()
        {
            if (grid == null && !HasData)
                return 0;

            int n = 0;
            for (int c = 0; c < columns; c++)
                for (int r = 0; r < rows; r++)
                {
                    if (CarAt(c, r) && !EmptyAt(c, r))
                        n++;
                }
            return n;
        }

        /// <summary>
        /// 车上最后一个像素匹配到、准备上车时：先收掉车上其它乘客的犯困表情（乘客即将出发，不再犯困），
        /// 再让表情管理器尝试播开心表情（是否真的播由管理器判断——只有前排车会播）。
        /// </summary>
        private static void OnLastBoarding(ContainerItem container, PixelItem boardingPixel)
        {
            var emoji = EmojiManager.Instance;
            if (emoji == null)
                return;

            emoji.ClearSleepEmojis(container);
            emoji.TryPlayHappyEmoji(container, boardingPixel);
        }

        /// <summary>
        /// 复活深排上车（gridZ &gt;= maxOpenRows）：像素原地消失（DisappearWithPop，参考开盖 tween）→ 瞬移到目标车落点出现。
        /// 仍走 Consume 扣容量 → OpenRearLid → consumingCount 计数 → OnPixelConsumed（失败判定 + 出库）完整链路，只是省略 jump。
        /// gridZ &gt;= maxOpenRows + 1（视野外更严格 1 排）的车完成匹配时，直接原地销毁并瞬间补位，避免后期大量已匹配车开走产生垃圾时间。
        /// 绳组车另有一条门槛：**整组**都装满、且都在可消失范围内，才整组一起消失（见 <see cref="RopeCarBlocksInstantDestroy"/>）。
        /// </summary>
        public void ConsumePixelInstant(PixelItem pixel, ContainerItem container)
        {
            if (pixel == null || container == null || container.IsEmpty)
                return;

            bool isLast = ConsumeCar(container);
            // 视野外更严格 1 排：完成匹配 → 原地销毁。绳组车要再收紧一层，见 RopeCarBlocksInstantDestroy。
            bool destroyInPlace = isLast && container.gridZ >= maxOpenRows + 1 && !RopeCarBlocksInstantDestroy(container);
            if (isLast)
                OpenRearLidAfterMatch(container);   // 播放移入动画前，先开后盖（绳组在整组装满这一刻整组一起开）
            consumingCount++;

            int col = container.gridX;
            System.Action onConsumed = () => OnPixelConsumed(container, col, isLast, destroyInPlace);

            // 原地消失（pop 1.1× → 缩到 0，与开盖同一 tween）后，瞬移到目标车落点出现
            pixel.transform.DisappearWithPop(() =>
            {
                if (pixel == null)
                    return;
                bool placed = container != null && container.PlacePixelInstant(pixel);
                if (!placed)
                    Destroy(pixel.gameObject);   // 无空闲落点（或车已销毁）：销毁
                GameData.ClearedPixelCount++;
                onConsumed();
            });
        }

        private IEnumerator MovePixelToContainer(PixelItem pixel, ContainerItem container, int col, bool isLast)
        {
            // 新上车表现：有空闲落点时由 ContainerItem 接管（挂落点 → DOLocalJump 到 0 → 弹性缩放），
            // 每个上车像素弹回完成后触发 OnPixelConsumed（失败判定 + 出库）；无空闲落点则回退到下面的旧 Lerp。
            System.Action onConsumed = () => OnPixelConsumed(container, col, isLast);
            if (container != null && container.TryBoardPixel(pixel, onConsumed))
                yield break;

            Vector3 start = pixel.transform.position;
            Vector3 target = container.transform.position;

            float dist = Vector3.Distance(start, target);
            float duration = consumeSpeed > 0.0001f ? dist / consumeSpeed : 0f;

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(duration > 0.0001f ? t / duration : 1f);
                pixel.transform.position = Vector3.Lerp(start, target, k);
                yield return null;
            }
            pixel.transform.position = target;

            GameData.ClearedPixelCount++;
            Destroy(pixel.gameObject);

            onConsumed();
        }

        /// <summary>单个像素上车落定（jump 弹回完成 / 回退 lerp 完成）后的统一回调：递减上车计数 → 事件驱动失败判定 → 耗尽则出库或深排原地销毁。</summary>
        private void OnPixelConsumed(ContainerItem container, int col, bool isLast, bool destroyInPlace = false)
        {
            consumingCount = Mathf.Max(0, consumingCount - 1);
            var gc = GameController.Instance;
            if (gc != null)
                gc.TryCheckFail(GameController.FailCheckpoint.PixelConsumed);
            if (!isLast)
                return;
            if (destroyInPlace)
                DestroyRopeGroupInPlace(container);   // 视野外深排车：原地销毁 + 瞬间补位（绳组整组一起消失）
            else
                TryExitIfAtFront(container, col);
        }

        /// <summary>某车补位到新位置（roll 或 lerp 完成）后的统一回调：事件驱动失败判定 → 若已在前排且耗尽则尝试出库。</summary>
        private void OnCarArrivedFront(ContainerItem item, int col)
        {
            var gc = GameController.Instance;
            if (gc != null)
                gc.TryCheckFail(GameController.FailCheckpoint.CarArrivedFront);
            TryExitIfAtFront(item, col);
        }

        /// <summary>
        /// 前排容器耗尽：立即清空该格，启动小车出库动画；转正瞬间触发补位。
        /// 轴未配置时（ContainerExitDriver.Play 回退）等价旧的「直接销毁 + 补位」。
        /// </summary>
        /// <param name="ropeRearExit">
        /// true = 绳组的非头车：出库跳过倒车、直接切前轴（运动参数走 ContainerExitDriver 里独立的那一组）。
        /// </param>
        private void StartContainerExit(ContainerItem gone, int col, bool ropeRearExit = false)
        {
            grid[col, 0] = null;
            if (HasData)
                _cells[CellIndex(col, 0)].occupied = false;   // 数据层同步：这一格的车已经走了

            // 第二次检查点（复查）：兜住「最后一辆车在完成匹配那一刻没被判到」的情况
            // （例如它是由复活路径直接匹配完成的，那个时点的判胜会被 _transitioning 挡下）。
            // 原地销毁（DestroyContainerInPlace）不在这里补——盘面清空的最后一步必然是某辆车走前排出库，
            // 因为深排车消失蕴含同列还有浅排车（列内压紧，见 DescribeColumnHoles）。
            TryCheckWin(GameController.WinCheckpoint.CarLeft);

            var driver = gone.GetComponent<ContainerExitDriver>();
            if (driver == null)
                driver = gone.gameObject.AddComponent<ContainerExitDriver>();
            // onFinished：动画播完后把车交回对象池（从前这里是 driver 内部 Destroy(gameObject)）
            driver.Play(() => RefillColumn(col), ropeRearExit, () => DespawnCar(gone));
        }

        /// <summary>
        /// 小车在前排且容量耗尽时启动出库。幂等：grid[col,0] 已非本车（或已开始出库）时跳过，
        /// 避免「后排满但未补位」或「补位完成 / 像素到达」同帧竞态下重复触发。
        ///
        /// 绳组车额外一条门槛：**必须等同组全部装满且都停在前排**才一起出库。
        /// 未就绪时直接返回——本车留在前排占住格子，从而该列不补位（这正是「留在前排等待」的实现）。
        ///
        /// 另有一条对**所有车**都成立的准备门槛：**上车动画必须已经全部结束**。
        /// 车身变空是**瞬间**的（Consume 一执行 IsEmpty 就为 true），但像素还要跳一段才落定、之后才开始播弹性；
        /// 所以这个窗口里「装满 + 在前排 + 不在补位」全部成立，出车会被提前放行——
        /// 那样 ContainerExitDriver 取到的 cartParent 会是弹性轴，车斜着开远且转正拉不回来
        /// （见 <see cref="ContainerItem.IsBoarding"/>）。等上车动画播完会自动重新触发，无需额外轮询。
        /// </summary>
        private void TryExitIfAtFront(ContainerItem item, int col)
        {
            if (item == null || !item.IsEmpty)
                return;
            if (item.isRefilling)
                return;   // 补位移动中，等 MoveContainer 完成后由它触发
            if (item.IsBoarding)
                return;   // 上车动画（跳车 / 弹性换轴）还没结束：等它播完，onBoarded → OnPixelConsumed 会重新触发

            List<ContainerItem> chain;
            if (item.ropeGroupId != 0 && _ropeGroups.TryGetValue(item.ropeGroupId, out chain))
            {
                if (!IsRopeGroupReady(chain))
                    return;   // 组未齐：留在此处等待，不做任何补位
                ExitRopeGroup(chain);
                return;
            }

            if (grid == null || grid[col, 0] != item)
                return;   // 不在前排（或已开始出库）
            StartContainerExit(item, col);
        }

        /// <summary>
        /// 绳组是否全部就绪：组内每辆车都已装满、都在前排、都不在补位移动中、且形变都已播完。
        ///
        /// 「上车动画已结束」这一条必须逐个成员查：车身变空是**瞬间**的（Consume 一执行 IsEmpty 就为 true），
        /// 但像素还要跳一段才落定、之后才开始播弹性。所以一个成员哪怕还在跳车或还在播弹性，
        /// 也已经满足「装满 + 在前排」。整组出库是由**某个成员**的事件触发的，若不查这条，
        /// 那个车身正挂在弹性轴下的成员就会带着整组提前出车——整条出车链条挂到弹性轴下，
        /// 车斜着开远且转正拉不回来（见 <see cref="ContainerItem.IsBoarding"/>）。
        /// 等它播完那次 OnPixelConsumed 会重新走到这里，届时自会就绪。
        /// </summary>
        private bool IsRopeGroupReady(List<ContainerItem> chain)
        {
            if (grid == null)
                return false;

            for (int i = 0; i < chain.Count; i++)
            {
                var car = chain[i];
                if (car == null || !car.IsEmpty || car.isRefilling)
                    return false;
                if (car.IsBoarding)
                    return false;   // 该成员的上车动画还没结束（跳车中 / 车身挂在弹性轴下）
                if (!IsInRange(car.gridX, 0) || grid[car.gridX, 0] != car)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 组内是否**每一辆都已装满**——即「这个绳组还需要像素吗」，只此一项。
        ///
        /// 刻意**不**包含「都在前排」（<see cref="IsRopeGroupReady"/> 管）与「形变/上车动画已播完」（那两个都管）：
        /// 那些是"能不能出库"，而这里问的是"还需不需要像素"，用途完全不同。两处判据共用本方法，
        /// 避免又出现「用 IsRopeGroupReady 当像素需求判据」那类错配（见 <see cref="IsWaitingRopeCar"/>）。
        /// </summary>
        private bool IsRopeGroupMatched(List<ContainerItem> chain)
        {
            if (chain == null)
                return true;

            for (int i = 0; i < chain.Count; i++)
            {
                var car = chain[i];
                if (car != null && !car.IsEmpty)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 全组出库：头车（列最小 = 最左边）立即出发；第一个后车在 <see cref="ropeExitHeadGap"/> 之后，
        /// 其余后车之间再各自间隔 <see cref="ropeExitStagger"/>——两段间隔独立配置。
        /// 出车方式也分两种：头车走正常出车（含倒车），被绳子连接的后车跳过倒车、直接切前轴（见 ContainerExitDriver）。
        /// 期间绳长仍在每帧同步，所以延迟窗口内绳子会被拉长后逐段消失。
        /// </summary>
        private void ExitRopeGroup(List<ContainerItem> chain)
        {
            StartCoroutine(ExitRopeGroupRoutine(chain));
        }

        private IEnumerator ExitRopeGroupRoutine(List<ContainerItem> chain)
        {
            float headGap = Mathf.Max(0f, ropeExitHeadGap);   // 头车 → 第一个后车
            float rearGap = Mathf.Max(0f, ropeExitStagger);   // 后车 → 后车

            for (int i = 0; i < chain.Count; i++)
            {
                if (i > 0)
                {
                    float gap = i == 1 ? headGap : rearGap;
                    if (gap > 0f)
                        yield return new WaitForSeconds(gap);
                }

                var car = chain[i];
                if (car == null || grid == null)
                    yield break;   // 关卡已重建 / 车已被销毁：剩下的交给重建流程

                if (!IsInRange(car.gridX, 0) || grid[car.gridX, 0] != car)
                    continue;      // 已被移走 / 已开始出库：幂等兜底

                // i == 0 是头车：正常出车；i > 0 是「被绳子连接的后车」：跳过倒车、直接切前轴
                StartContainerExit(car, car.gridX, i > 0);
            }
        }

        /// <summary>该车是否属于一个**生效中**的绳组。绳组车不能走「深排原地销毁」路径，否则绳子锚点会当场消失。</summary>
        private bool IsRopeCar(ContainerItem item)
        {
            return item != null && item.ropeGroupId != 0 && _ropeGroups.ContainsKey(item.ropeGroupId);
        }

        /// <summary>
        /// 该车是否「已装满、但同组还有车没装满」——即一辆**仍被同组的像素需求压着、必须继续堵住整列**的绳车。
        ///
        /// 先装满的那几辆会一直占着各自前排的格子（要等全组都到前排才出库），这段时间里它们必须**继续当阻塞物**：
        /// 否则它们后面的车会照常开盖、把像素吸走，而这些像素本该留给同组还没装满的车——
        /// 被吞掉之后该颜色可能再也不出现，绳组就永远凑不齐，整列跟着一起死锁。
        ///
        /// **判据只看「组是否装满」，不看「组是否就绪」**：<see cref="IsRopeGroupReady"/> 还额外要求「都在前排」，
        /// 那是**出库**条件，与「还需不需要像素」无关。用错判据的后果是——整组装满之后、真正移出之前
        /// （各车还在往各自前排挪），后排仍被堵着，要等到车移出才开盖；而单列车的规则是
        /// 「前车最后一个像素开始上车动画时就开它的后盖」。改用 <see cref="IsRopeGroupMatched"/> 后两者一致：
        /// **整组装满那一刻，整组的后排一起开盖**（由 <see cref="OpenRearLidAfterMatch"/> 补上）。
        /// </summary>
        private bool IsWaitingRopeCar(ContainerItem item)
        {
            if (item == null || !item.IsEmpty)
                return false;
            if (!IsRopeCar(item))
                return false;

            List<ContainerItem> chain;
            return _ropeGroups.TryGetValue(item.ropeGroupId, out chain) && !IsRopeGroupMatched(chain);
        }

        /// <summary>
        /// 复活深排车完成匹配：直接原地销毁（连同已上车的乘客像素，均已计入 ClearedPixelCount），
        /// 后车瞬间补位（teleport，无动画）。用于玩家视野外（gridZ >= maxOpenRows + 1）的车，
        /// 避免后期大量已匹配的车逐个开走出库产生垃圾时间。
        /// </summary>
        private void DestroyContainerInPlace(ContainerItem item)
        {
            if (item == null)
                return;
            int col = item.gridX;
            int row = item.gridZ;
            if (grid == null || !IsInRange(col, row) || grid[col, row] != item)
                return;   // 已被移走 / 已销毁，幂等兜底

            grid[col, row] = null;
            if (HasData)
                _cells[CellIndex(col, row)].occupied = false;   // 数据层同步：该格已无车
            DetachFromRopeChain(item);   // 绳组：本车出局，先从链里摘掉，否则剩下的成员永远凑不齐
            DespawnCar(item);            // 车 + 乘客像素一并交回对象池（乘客已计入 ClearedPixelCount）

            // 后车瞬间补位（teleport，无动画）：每车向上移一格，与 RefillColumn 同构（保留空格）
            for (int r = row + 1; r < rows; r++)
            {
                if (!CarAt(col, r))
                    continue;

                int newRow = r - 1;
                var rear = GetItem(col, r);

                // 懒实例化：这一格原本在视窗外（无实例），前移后落进视窗 → 按**最终位置**直接实例化
                // （与 teleport 无动画的补位一致，不需要旧位置）。
                if (rear == null && newRow < WindowRows)
                    rear = Materialize(col, r);

                if (rear != null)
                {
                    rear.gridZ = newRow;
                    grid[col, newRow] = rear;
                    grid[col, r] = null;
                    rear.transform.localPosition = GetLocalPosition(col, newRow);
                }

                MoveCell(col, r, newRow);
            }
        }

        /// <summary>
        /// 绳车是否**不允许**走「深排原地销毁」路径。
        ///
        /// 绳组是一个整体：要消失就**整组一起消失**，所以门槛对整组统一——只要组内有一辆车不满足
        /// 消失条件，全组都不消失。不满足的情形有两类：
        ///
        /// | 不满足 | 为什么 |
        /// |---|---|
        /// | 还有车**没装满** | 这辆车是那一组凑齐的必要拼图；销毁它 = 剩下的成员永远等不到伙伴 |
        /// | 有车**不在可消失范围**（`gridZ < maxOpenRows + 1`，即还在玩家视野内） | 那辆车看得见，凭空消失会穿帮；而且它马上要上前排，正组出库才是它该走的路径 |
        ///
        /// 不满足时车留在原地。它所在列靠前方**非绳车**正常出库把自己逐步带到前排（`RefillColumn`
        /// 是逐列的，所以同组其它列的车也会被各自的前车推着走），最终全组都在前排时按 §5.4 的出库规则
        /// **一起出库**——走正规出库而不是凭空消失，这正是期望的兜底。
        /// 非绳车恒为 false，行为与改动前完全一致。
        /// </summary>
        private bool RopeCarBlocksInstantDestroy(ContainerItem item)
        {
            if (!IsRopeCar(item))
                return false;

            List<ContainerItem> chain;
            if (!_ropeGroups.TryGetValue(item.ropeGroupId, out chain))
                return false;

            if (!IsRopeGroupMatched(chain))
                return true;                          // 同组还有车没装满

            for (int i = 0; i < chain.Count; i++)
            {
                var car = chain[i];
                if (car != null && car.gridZ < maxOpenRows + 1)
                    return true;                      // 同组有车还在视野内，不能消失
            }
            return false;
        }

        /// <summary>
        /// 绳组**整组**原地销毁：组内所有成员在同一帧一起消失。
        ///
        /// 必须整组一起做，不能逐个车各自判断：先消失的那辆会经由 <see cref="DetachFromRopeChain"/>
        /// 把自己从链里摘掉，同组的判定随即失真（`IsRopeGroupReady` 对空链恒为 true），
        /// 剩下那些车的门槛就再也不是原本那条了。
        /// 调用前提：<see cref="RopeCarBlocksInstantDestroy"/> 已确认整组都符合消失条件。
        /// </summary>
        private void DestroyRopeGroupInPlace(ContainerItem item)
        {
            if (item == null)
                return;

            List<ContainerItem> chain;
            if (item.ropeGroupId == 0 || !_ropeGroups.TryGetValue(item.ropeGroupId, out chain))
            {
                DestroyContainerInPlace(item);   // 非绳车（或已不在链里）：退化成单车处理
                return;
            }

            // 复制一份再遍历：DestroyContainerInPlace 会从链里摘人，直接遍历原链会边遍历边改
            var members = new List<ContainerItem>(chain);
            for (int i = 0; i < members.Count; i++)
                DestroyContainerInPlace(members[i]);
        }

        /// <summary>
        /// 把一辆车从它所属的绳链里摘掉。只用于「深排原地销毁」——那辆车就此出局，
        /// 留着一个已销毁的成员会让 <see cref="IsRopeGroupReady"/> 永远返回 false，
        /// 剩下的成员就再也出不了库了（摘掉之后它们照常凑齐、照常一起出库）。
        /// 关卡重建不在这里处理——那条路径由 <see cref="ClearRopes"/> 整体清空。
        /// </summary>
        private void DetachFromRopeChain(ContainerItem item)
        {
            if (item == null || item.ropeGroupId == 0)
                return;

            List<ContainerItem> chain;
            if (_ropeGroups.TryGetValue(item.ropeGroupId, out chain))
                chain.Remove(item);
        }

        /// <summary>
        /// 某容器**耗尽**（最后一个像素开始上车动画）时的开盖入口，单列车与绳组在此统一。
        ///
        /// 单列车：只开它自己正后方那辆——与原来一致。
        /// 绳组：**整组装满的那一刻，把整组每个成员的正后方盖子都打开**。
        ///
        /// 为什么要在这里补一次整组开盖：各成员自己耗尽时会各调一次 <see cref="OpenRearLid"/>，
        /// 但那时组往往还没满 → 被 <see cref="IsWaitingRopeCar"/> 挡下（那些像素要留给同组其它车）。
        /// 等最后一个成员装满，先前那些成员那一次已经错过、不会再补，于是后排要等到车真的移出才开盖；
        /// 而单列车的规则是「前车最后一个像素开始上车动画时就开后盖」。在这里补一次，两者就一致了。
        /// </summary>
        private void OpenRearLidAfterMatch(ContainerItem container)
        {
            if (container == null)
                return;

            List<ContainerItem> chain;
            if (container.ropeGroupId != 0 && _ropeGroups.TryGetValue(container.ropeGroupId, out chain)
                && IsRopeGroupMatched(chain))
            {
                for (int i = 0; i < chain.Count; i++)
                    OpenRearLid(chain[i]);
                return;
            }

            OpenRearLid(container);
        }

        /// <summary>
        /// 打开某容器正后方（gridZ + 1）容器的盖子，让它随后可接收像素。
        /// 前排 / 已开放的后排容器共用此逻辑——耗尽谁的容量就开谁后面的盖子。
        ///
        /// 例外：该车「装满但同组还有车没装满」时**不开**——它仍压着同组的像素需求
        /// （<see cref="IsWaitingRopeCar"/>），打开后盖就等于绕过 <see cref="IsOpen"/> 对后排的堵截。
        /// </summary>
        private void OpenRearLid(ContainerItem container)
        {
            if (container == null)
                return;
            if (IsWaitingRopeCar(container))
                return;

            int col = container.gridX;
            int row = container.gridZ + 1;
            var rear = GetItem(col, row);
            if (rear != null)
            {
                rear.OpenLid();
                return;
            }

            // 懒实例化：正后方那辆车还在视窗外（没有实例）——把开盖状态记到数据层，
            // 等它补位滚进视窗被实例化时由 ContainerItem.ApplyCell 水合出来，与当场开盖表现一致。
            if (!CellInRange(col, row))
                return;
            int i = CellIndex(col, row);
            if (!_cells[i].occupied || _cells[i].lidOpened)
                return;
            _cells[i].lidOpened = true;
            if (_cells[i].isQuestion)
                _cells[i].revealed = true;   // 与 ContainerItem.OpenLid → RevealQuestion 同义
        }

        /// <summary>
        /// 某列后排容器依次前移一格（补位）。
        ///
        /// 懒实例化：原本在视窗外的车补位后会落进视窗——必须**先在旧排位置实例化**，
        /// 再和全列一起被 <see cref="MoveContainer"/> 前移。若直接按终点位置现造，
        /// 它会凭空出现在终点格、与还没挪走的车重叠。
        /// </summary>
        private void RefillColumn(int col)
        {
            for (int row = 1; row < rows; row++)
            {
                if (!CarAt(col, row))
                    continue;

                int newRow = row - 1;
                var it = GetItem(col, row);

                if (it == null && newRow < WindowRows)
                    it = Materialize(col, row);         // 先在旧排位置出现，随后与全列一起前移

                if (it != null)
                {
                    it.gridZ = newRow;
                    grid[col, newRow] = it;
                    grid[col, row] = null;
                }

                MoveCell(col, row, newRow);

                if (it != null)
                    StartCoroutine(MoveContainer(it, col, newRow));
            }
        }

        /// <summary>
        /// 数据层同步：把某格的数据搬到另一格（补位 / 原地销毁后的前移）。
        /// 只有未实例化的格子有实际内容，但整体搬移能让数据层始终与列布局对齐。
        /// 只改 <c>col</c> / <c>row</c>（当前格）；<c>originCol</c> / <c>originRow</c>（出生格）随结构体原样带走
        /// ——那是这辆车的身份，前移多少次都不该变（见 <see cref="ContainerCell.originCol"/>）。
        /// </summary>
        private void MoveCell(int col, int fromRow, int toRow)
        {
            if (!HasData || fromRow == toRow)
                return;
            if (!IsInRange(col, fromRow) || !IsInRange(col, toRow))
                return;

            int from = CellIndex(col, fromRow);
            int to = CellIndex(col, toRow);
            _cells[to] = _cells[from];
            _cells[to].col = col;
            _cells[to].row = toRow;
            _cells[from] = default(ContainerCell);
        }

        private IEnumerator MoveContainer(ContainerItem item, int col, int row)
        {
            item.isRefilling = true;
            Vector3 target = GetLocalPosition(col, row);

            // 有 roll 轴：交给 ContainerItem 驱动「补位移动 + 侧倾 + 上车像素锁定」，完成回调里复位并尝试出库
            if (item.TryStartRefillRoll(target, refillSpeed, () =>
            {
                item.isRefilling = false;
                OnCarArrivedFront(item, col);
            }))
                yield break;

            // 回退：无 roll 轴时旧的纯 Lerp 补位
            Vector3 start = item.transform.localPosition;
            float dist = Vector3.Distance(start, target);
            float duration = refillSpeed > 0.0001f ? dist / refillSpeed : 0f;

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(duration > 0.0001f ? t / duration : 1f);
                item.transform.localPosition = Vector3.Lerp(start, target, k);
                yield return null;
            }
            item.transform.localPosition = target;
            item.isRefilling = false;

            // 补位到前排后，若该车已在后方等满（容量耗尽），立即启动出库
            OnCarArrivedFront(item, col);
        }

        /// <summary>清空所有 ContainerItem 子物体（播放中交回对象池；编辑器下先脱离父物体再销毁，避免同帧 GetComponentsInChildren 捡到旧物体）。</summary>
        public void ClearContainers()
        {
            consumingCount = 0;
            _unfinishedCars = 0;
            ClearRopes();   // 绳根引用着车，必须随车一起清掉
            var items = GetComponentsInChildren<ContainerItem>();
            for (int i = items.Length - 1; i >= 0; i--)
            {
                var it = items[i];
                if (it == null)
                    continue;

                if (Application.isPlaying)
                {
                    DespawnCar(it);   // 池化：交回对象池（未注册池时 DespawnCar 自己销毁）
                }
                else
                {
                    it.transform.SetParent(null, true);
                    DestroyImmediate(it.gameObject);
                }
            }

            // 数据层整体作废，下一关由 ApplyContainerData 重建。
            // （grid 这里不清，与改动前一致——调用方随后都会 RebuildGrid。）
            _cells = null;
            _dataAuthoritative = false;
        }

        /// <summary>
        /// 在指定格子生成一个 ContainerItem 并应用颜色/容量。
        /// 供**编辑器工具**（生成 / 拖移 / 修复）使用；运行模式的关卡加载走
        /// <see cref="ApplyContainerData"/>（只实例化前几排，其余留在数据层）。
        /// </summary>
        public ContainerItem SpawnContainer(int col, int row, int colorId, int capacity, ColorConfig config, bool isQuestion = false, int ropeGroupId = 0)
        {
            GameObject go = containerPrefab != null
                ? PrefabSpawner.Instantiate(containerPrefab.gameObject, transform)
                : null;
            if (go == null)
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);

            go.name = "Container_" + col + "_" + row;
            go.transform.SetParent(transform, false);
            go.transform.localPosition = GetLocalPosition(col, row);

            var item = go.GetComponent<ContainerItem>();
            if (item == null)
                item = go.AddComponent<ContainerItem>();

            item.gridX = col;
            item.gridZ = row;
            item.colorId = colorId;
            item.isQuestion = isQuestion;
            item.ropeGroupId = ropeGroupId;
            item.RefreshQuestionObject();   // Awake 时 isQuestion 尚未赋值，补刷新问号物体显隐
            item.SetCapacity(capacity);
            item.ApplyMaterial(config);
            if (row == 0)
                item.HideLid();   // 初始就在第一排：盖子直接隐藏（问号车此时也揭晓、隐藏问号物体）
            return item;
        }

        // ===== 懒实例化（运行模式）=====

        /// <summary>
        /// 运行模式的关卡加载入口：建立**数据层**并只实例化前 <see cref="WindowRows"/> 排。
        ///
        /// 与 <see cref="SpawnContainer"/> 的分工：后者逐格实例化，供编辑器工具（生成 / 拖移 / 修复）使用；
        /// 这里把没进视窗的车留在 <see cref="ContainerCell"/> 里，等它们补位滚进视窗时再现造。
        /// </summary>
        public void ApplyContainerData(IList<ContainerCell> cells, ColorConfig config)
        {
            _containerConfig = config;
            _cells = new ContainerCell[Mathf.Max(1, columns * rows)];
            grid = new ContainerItem[columns, rows];

            if (cells != null)
                for (int i = 0; i < cells.Count; i++)
                {
                    var c = cells[i];
                    if (!IsInRange(c.col, c.row))
                        continue;

                    // 原始格坐标 = 关卡数据里的出生格。补位前移只改 col/row，这两个始终是身份的锚点
                    // （车实例化时按它命名，见 Materialize）。
                    c.originCol = c.col;
                    c.originRow = c.row;

                    // 第一排的车开局就该是「已开盖」：与 SpawnContainer 里 `row == 0 → HideLid()` 同一口径
                    // （HideLid 会连带揭晓问号车）。
                    if (c.row == 0)
                    {
                        c.lidOpened = true;
                        if (c.isQuestion)
                            c.revealed = true;
                    }

                    _cells[CellIndex(c.col, c.row)] = c;
                }

            EnsureWindowOrSpawnAll();
            _unfinishedCars = CountUnfinishedCars();
        }

        /// <summary>
        /// 关卡数据落地后的实例化分流。
        ///
        /// · **运行模式**：只实例化前 <see cref="WindowRows"/> 排（数据层权威），其余车留给补位时现造；
        /// · **编辑模式**（编辑器里导入 / 应用 JSON）：**全部实例化**。策划要在 Scene 视图里直接看到、
        ///   选中、手工调整每一辆车，懒实例化在编辑器里既没有对象池也没有「帧」去驱动补位；
        ///   此时数据层退化为视图的镜像（每格都有实例 ⇒ 取值器的「有实例读实例」恒成立），
        ///   于是所有编辑器工具（拖移 / 生成 / 修复）照旧靠 RebuildGrid 重建，不存在陈旧数据。
        /// </summary>
        private void EnsureWindowOrSpawnAll()
        {
            if (Application.isPlaying)
            {
                _dataAuthoritative = true;
                EnsureWindow();
                return;
            }

            for (int col = 0; col < columns; col++)
                for (int row = 0; row < rows; row++)
                {
                    int i = CellIndex(col, row);
                    if (_cells[i].occupied)
                        SpawnFromCell(col, row, _cells[i], row);
                }

            _dataAuthoritative = false;   // 交给随后的 RebuildGrid → BuildCellsFromView 重建镜像
        }

        /// <summary>
        /// 补齐视窗：每列 <c>row &lt; WindowRows</c> 的有车之格，缺实例就补上（幂等）。
        /// </summary>
        /// <param name="onlyCol">≥ 0 时只补该列。</param>
        private void EnsureWindow(int onlyCol = -1)
        {
            if (!_dataAuthoritative || _cells == null)
                return;

            int depth = Mathf.Min(WindowRows, rows);
            for (int col = 0; col < columns; col++)
            {
                if (onlyCol >= 0 && col != onlyCol)
                    continue;

                for (int row = 0; row < depth; row++)
                {
                    if (grid != null && grid[col, row] != null)
                        continue;
                    if (!CarAt(col, row))
                        continue;
                    Materialize(col, row);
                }
            }
        }

        /// <summary>
        /// 把某格的车实例化出来并水合到数据层状态——懒实例化的**唯一入口**（该格已有实例时直接返回它）。
        /// 新实例先摆在 <paramref name="row"/> 那一排的位置上：补位前移时调用方传的是**旧排**，
        /// 于是它会跟着全列一起滑进视窗，而不是凭空出现在终点。
        /// </summary>
        private ContainerItem Materialize(int col, int row)
        {
            if (!IsInRange(col, row))
                return null;
            if (grid == null)
                grid = new ContainerItem[columns, rows];
            if (grid[col, row] != null)
                return grid[col, row];
            if (!CellInRange(col, row))
                return null;

            var cell = _cells[CellIndex(col, row)];
            if (!cell.occupied)
                return null;

            return SpawnFromCell(col, row, cell, row);
        }

        /// <summary>
        /// 实例化一辆车到指定格并水合数据层状态。
        /// 运行模式从对象池取、编辑模式走 <see cref="PrefabSpawner"/>（保住与预制体的关联）。
        /// </summary>
        /// <param name="placeRow">新实例先摆放的排。补位前移时传**旧排**（见 <see cref="RefillColumn"/>），
        /// 好让它跟着全列一起滑进视窗，而不是凭空出现在终点。</param>
        private ContainerItem SpawnFromCell(int col, int row, ContainerCell cell, int placeRow)
        {
            GameObject go = null;
            if (PoolReady)
                go = Pool.Spawn(containerPoolTag, transform);
            bool fromPool = go != null;

            if (go == null)
            {
                if (Application.isPlaying)
                    WarnNoPoolOnce();   // 编辑模式本来就没有池，不刷这条 warning
                go = containerPrefab != null
                    ? PrefabSpawner.Instantiate(containerPrefab.gameObject, transform)
                    : null;
                if (go == null)
                    go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.SetParent(transform, false);
            }

            // 按**原始格坐标**命名，而不是实例化那一刻的排号：补位前移时新进窗的车是按旧排实例化的
            // （为了带出滑动入场，见 RefillColumn），拿旧排命名会得到「名字写 row 5、车在 row 4」的误导。
            // 用出生格则名字就是这辆车的稳定身份，查 Hierarchy 时能直接对回关卡数据。
            go.name = "Container_" + cell.originCol + "_" + cell.originRow;

            var item = go.GetComponent<ContainerItem>();
            if (item == null)
                item = go.AddComponent<ContainerItem>();

            // 复用复位：**只有从池里取出来的才需要**（新实例本来就是干净的）。
            // 这同时避开了两个编辑期隐患：对 PrefabSpawner 生成的实例做复位会把预制体实例标脏，
            // 而编辑模式下既没有协程 / tween 也没有对象池可清。
            //
            // 层级还原必须在这里做一次：SpawnPool.GC 与返回主界面的绕过路径会把「换轴换到一半」的车
            // 直接丢进池里，那些车没经过 DespawnCar 的还原，只有取出时补一次才能保证干净。
            if (fromPool)
            {
                item.ResetForReuse();

                // 出库驱动的 _playing 也必须是 false，否则这辆车再也出不了库（Play 被它拦掉）。
                var driver = go.GetComponent<ContainerExitDriver>();
                if (driver != null)
                    driver.ResetForReuse();

                RestoreViewToPrefab(item);
            }

            go.transform.SetParent(transform, false);
            go.transform.localPosition = GetLocalPosition(col, placeRow);

            item.ApplyCell(col, row, cell.colorId, cell.capacity, cell.remaining,
                           cell.isQuestion, cell.revealed, cell.lidOpened, cell.ropeGroupId,
                           _containerConfig);
            item.group = this;

            grid[col, row] = item;
            return item;
        }

        /// <summary>
        /// 把一辆车交回对象池（未注册池 / 池已 GC 过 → 直接销毁，避免泄漏在场景里）。
        ///
        /// 归还前先还原层级：换轴期间**车身是挂在某根轴下面的**，直接改车身父物体到池根
        /// 会把那根轴遗留在容器组里（永生残留）。还原后整棵树才完整地进池。
        /// </summary>
        private void DespawnCar(ContainerItem item)
        {
            if (item == null)
                return;   // Unity 伪空：已销毁的车在这里就被挡掉

            var go = item.gameObject;

            var driver = go.GetComponent<ContainerExitDriver>();
            if (driver != null)
                driver.PrepareForPool();

            RestoreViewToPrefab(item);
            item.ResetForReuse();

            item.transform.SetParent(null, true);   // 先脱离容器组，免得同帧 GetComponentsInChildren 又捡到它

            if (PoolReady && Pool.TryDespawn(go))
                return;

            Destroy(go);
        }

        /// <summary>把车身的层级还原成预制体原样（池化复用）。快照首次需要时从 containerPrefab 抓取。</summary>
        private void RestoreViewToPrefab(ContainerItem item)
        {
            var tpl = GetViewTemplate();
            if (tpl == null || item == null)
                return;
            tpl.Apply(item.transform, transform);
        }

        /// <summary>取（并首次抓取）预制体层级快照；预制体为空时返回 null 并只警告一次。</summary>
        private ContainerViewTemplate GetViewTemplate()
        {
            if (_viewTemplate == null)
            {
                if (containerPrefab == null)
                {
                    if (!_warnedNoTemplate)
                    {
                        _warnedNoTemplate = true;
                        Debug.LogWarning("[ContainerGroup] containerPrefab 为空：池化复用无法还原车体层级，" +
                            "换轴 / 盖子 / 缩放可能残留在下一趟车上。", this);
                    }
                    return null;
                }
                _viewTemplate = ContainerViewTemplate.Capture(containerPrefab.transform);
            }
            return _viewTemplate.IsValid ? _viewTemplate : null;
        }

        private SpawnPool Pool
        {
            get { return GameManager.Instance != null ? GameManager.Instance.spawnPool : null; }
        }

        /// <summary>对象池可用（已初始化且注册了 <see cref="containerPoolTag"/>）。</summary>
        private bool PoolReady
        {
            get { return Pool != null && Pool.HasTag(containerPoolTag); }
        }

        private void WarnNoPoolOnce()
        {
            if (_warnedNoPoolTag)
                return;
            _warnedNoPoolTag = true;
            Debug.LogWarning("[ContainerGroup] SpawnPool 里没有 tag \"" + containerPoolTag +
                "\"（或 spawnPool 尚未初始化）：容器回退为直接 Instantiate + Destroy，不复用。" +
                "请把车预制体加进 Assets/Configs/SpawnPoolConfig 并把 tag 改成该值。", this);
        }

        /// <summary>是否存在同色且可匹配的容器（前排或已开放的后排，最多 maxOpenRows 排）。用于失败判定。</summary>
        public bool HasMatchableContainerOfColor(int colorId)
        {
            for (int col = 0; col < columns; col++)
            {
                if (FindMatchableInColumn(col, colorId) != null)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 是否存在「已开启匹配且即将抵达前排」的车（含正在补位移动的车）。
        /// 判定 = 存在 isRefilling 的车，或存在「非前排、盖子已打开、且前方所有车都已放行」的车。
        /// 说明：RefillColumn 在补位开始时就把 gridZ 同步置 0，而 lidOpened 在 ConsumePixel（OpenRearLid）时就已锁存，
        /// 因此该条件在整个「上车 → 弹回 → 出库动画 → 补位移动」区间内恒为 true，直到车真正落定前排才释放，杜绝空白时间窗。
        /// 「前方已放行」约束用于排除复活直接给「前方仍有非空车」的后排车开盖的情况——那种车并非即将补位，不算过渡中。
        ///
        /// **绳车**沿用的是暴露/开盖那一套判据（<see cref="IsRowReleased"/>）：被等待中的绳车堵住的列
        /// 后面那些车**不会**被补位到前排，所以不能算「过渡中」。否则被绳车堵死的列会让本方法恒为真，
        /// <c>IsFail</c> 提前返回 false → 该判失败时不判、关卡卡住。
        /// </summary>
        public bool HasPendingFrontTransition()
        {
            for (int col = 0; col < columns; col++)
                for (int row = 0; row < rows; row++)
                {
                    var it = GetItem(col, row);
                    if (it != null && it.isRefilling)
                        return true;                                  // 补位移动中的车必然有实例
                    // 盖子已打开这一条要走数据层：复活路径可能给视窗外的深排车开过盖，
                    // 状态记在 ContainerCell 上，只读实例会漏。
                    if (row >= 1 && LidOpenedAt(col, row) && IsFrontCleared(col, row))
                        return true;
                }
            return false;
        }

        /// <summary>
        /// 该格前方（row 更小）所有车是否都已**放行**，即该格即将被补位到前排。
        /// 判据与 <see cref="IsOpen"/> 共用 <see cref="IsRowReleased"/>——两者必须一致，否则会出现
        /// 「不开盖」（前排不放行）却「算过渡中」（后方即将补位）的自相矛盾状态。
        /// </summary>
        private bool IsFrontCleared(int col, int row)
        {
            for (int r = 0; r < row; r++)
            {
                if (!IsRowReleased(col, r))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 复活：把一批像素按颜色直接匹配到车（非空、非补位中），
        /// 优先前排（gridZ 小，含第 0 排）、同排优先列小。复用 ConsumePixel（扣容量 → 跳车 → 出库链路），
        /// 有车被匹配即播放开盖 tween（OpenLid，幂等）。返回未找到同色车的像素。
        /// 说明：失败仅保证「传送带上的像素不匹配」，缓冲区/带溢出里仍可能有匹配前排车的颜色，
        /// 因此必须优先补第 0 排车，否则会把这类像素误判为无车可匹配而销毁，留下被掏空的前排车堵死整列。
        /// </summary>
        public List<PixelItem> MatchPixelsToCars(List<PixelItem> pixels)
        {
            var unmatched = new List<PixelItem>();
            if (pixels == null)
                return unmatched;

            for (int i = 0; i < pixels.Count; i++)
            {
                var pixel = pixels[i];
                if (pixel == null)
                    continue;

                if (!FindCarForColor(pixel.colorId, out int carCol, out int carRow))
                {
                    unmatched.Add(pixel);
                    continue;
                }

                var car = GetItem(carCol, carRow);
                if (car == null)
                {
                    // 懒实例化：命中了一个**还没实例化**的深排车格。
                    // 数据层早已记着这格（颜色 / 容量 / 已扣过的账），这里把它现造出来并水合到数据状态，
                    // 之后走与从前完全相同的上车 / 出库链路——表现与「它早就在那儿」逐字一致；
                    // 若这辆车的容量就此填满且深到视窗外，销毁与瞬间补位也照旧走 DestroyContainerInPlace。
                    car = Materialize(carCol, carRow);
                    if (car == null)
                    {
                        unmatched.Add(pixel);
                        continue;
                    }
                }

                car.OpenLid();              // 有车被匹配 → 播放开盖 tween（幂等）
                if (car.gridZ < maxOpenRows)
                    ConsumePixel(pixel, car);        // 前 maxOpenRows 排：复用正常 jump 上车
                else
                    ConsumePixelInstant(pixel, car); // 更后排：原地消失 → 瞬移到目标车落点出现
            }
            return unmatched;
        }

        /// <summary>
        /// 从全排（0..rows-1）按「前排优先、同排列小优先」找第一个同色、非空、非补位中的车。
        ///
        /// **走数据层、覆盖全部排**——未实例化的深排车也在候选之列（这正是复活要填的「后排车」）：
        /// 只扫实例会漏掉它们，那些像素会被误判成「无车可匹配」而销毁，还会留下被掏空的前排车堵死整列。
        /// </summary>
        private bool FindCarForColor(int colorId, out int carCol, out int carRow)
        {
            for (int row = 0; row < rows; row++)
                for (int col = 0; col < columns; col++)
                {
                    if (!CarAt(col, row) || EmptyAt(col, row) || ColorAt(col, row) != colorId)
                        continue;

                    var it = GetItem(col, row);
                    if (it != null && it.isRefilling)
                        continue;   // 补位移动中的车不接客（只有实例才可能处于补位中）

                    carCol = col;
                    carRow = row;
                    return true;
                }

            carCol = -1;
            carRow = -1;
            return false;
        }

        // ===== 绳子连接 =====

        /// <summary>
        /// 建立绳子：把 ropeGroupId 相同（且非 0）的车按列升序成链，相邻两车之间生成一条绳。
        /// 由 GameController 在关卡应用完成后调用。
        /// </summary>
        /// <param name="shuffleEnabled">
        /// 本次关卡是否启用了洗牌。开启时**完全不建绳**、绳组也不参与任何判定——
        /// 对应「运行模式下洗牌激活时，所有绳子失效」（洗牌会打乱车的列位置，绳组关系已无意义）。
        /// </param>
        public void BuildRopes(bool shuffleEnabled)
        {
            ClearRopes();

            if (!ropeEnabled || shuffleEnabled)
                return;

            CollectRopeGroups();

            foreach (var list in _ropeGroups.Values)
            {
                for (int i = 0; i + 1 < list.Count; i++)
                    CreateRope(list[i], list[i + 1]);
            }
        }

        /// <summary>销毁所有绳根并清空绳组（关卡重建 / 清空容器时调用）。</summary>
        public void ClearRopes()
        {
            for (int i = 0; i < _ropeRoots.Count; i++)
            {
                var root = _ropeRoots[i];
                if (root == null)
                    continue;
                if (Application.isPlaying)
                    Destroy(root);
                else
                    DestroyImmediate(root);
            }
            _ropeRoots.Clear();
            _ropeGroups.Clear();
        }

        /// <summary>按 ropeGroupId 归组：只收网格内确有位置的当前车，组内按列（gridX）升序。</summary>
        private void CollectRopeGroups()
        {
            var all = GetComponentsInChildren<ContainerItem>();
            for (int i = 0; i < all.Length; i++)
            {
                var item = all[i];
                if (item == null || item.ropeGroupId == 0)
                    continue;
                if (grid == null || !IsInRange(item.gridX, item.gridZ) || grid[item.gridX, item.gridZ] != item)
                    continue;   // 跳过预制体模板 / 已脱离网格的残留对象

                List<ContainerItem> list;
                if (!_ropeGroups.TryGetValue(item.ropeGroupId, out list))
                {
                    list = new List<ContainerItem>();
                    _ropeGroups[item.ropeGroupId] = list;
                }
                list.Add(item);
            }

            foreach (var list in _ropeGroups.Values)
                list.Sort((a, b) => a.gridX.CompareTo(b.gridX));
        }

        private void CreateRope(ContainerItem left, ContainerItem right)
        {
            var go = new GameObject("Rope_" + left.gridX + "_" + right.gridX);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;   // 骨骼按世界坐标铺，绳根必须无缩放

            var link = go.AddComponent<ContainerRopeLink>();
            link.leftCar = left;
            link.rightCar = right;
            link.material = ropeMaterial;
            link.linkCount = ropeLinkCount;
            link.diameter = ropeDiameter;
            link.ropeLayerName = ropeLayerName;
            link.swayAmplitude = ropeSwayAmplitude;
            link.swayFrequency = ropeSwayFrequency;
            link.swayWaves = ropeSwayWaves;
            link.swayVerticalRatio = ropeSwayVerticalRatio;

            if (link.Build())
                _ropeRoots.Add(go);
            else
                Destroy(go);   // 端点缺失 / 生成失败：不留空壳
        }

        // ===== 绳连的编辑器可视化 =====

        [Header("绳连 Gizmos")]
        [Tooltip("整条绳连预览（线 + 端点小球）的 Y 偏移（米）：抬到车体上方，避免被车挡住")]
        public float ropeGizmoYOffset = 1f;

        [Tooltip("绳连预览的颜色（洗牌开着导致运行时不会建绳时，自动按该色的低透明度画）")]
        public Color ropeGizmoColor = new Color(0.1f, 1f, 1f);

        [Tooltip("绳连预览端点小球的半径（米）")]
        public float ropeGizmoAnchorRadius = 0.1f;

        /// <summary>端点没配时的醒目色（固定亮红，不跟随 Rope Gizmo Color）。</summary>
        private static readonly Color RopeGizmoMissingAnchorColor = new Color(1f, 0.15f, 0.15f);

        /// <summary>
        /// 非运行模式下把绳连画出来：同一个 ropeGroupId 的车按列升序串成链，在相邻两车之间画一条线——
        /// 端点取车上的 <see cref="ContainerItem.ropeAnchorRight"/> / <see cref="ContainerItem.ropeAnchorLeft"/>，
        /// 也就是运行时真正建绳的那两点。所以看到的就是运行时绳子的位置与走向。
        ///
        /// 两个刻意的处理：
        /// · **整体抬到车体上方**（<see cref="ropeGizmoYOffset"/>）——端点就在车体侧面，不抬会被车完全挡住；
        /// · 洗牌开着或绳子总开关关掉时（运行时不会建绳，见 §5.5）用**同色低透明度**画，一眼能分辨。
        ///
        /// 线宽用 `Gizmos.DrawLine` 的默认值（恒 1px，该 API 没有宽度参数）；曾用 `Handles.DrawAAPolyLine` 加粗过，
        /// 但那样要把整段代码塞进 `#if UNITY_EDITOR` 并引 `UnityEditor`，收益不值当，已放弃。
        ///
        /// 运行时不画：那时绳子是真渲染出来的，再叠一层 Gizmos 只会糊。
        /// </summary>
        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
                return;

            var items = GetComponentsInChildren<ContainerItem>();
            if (items == null || items.Length == 0)
                return;

            var groups = new Dictionary<int, List<ContainerItem>>();
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (item == null || item.ropeGroupId == 0)
                    continue;

                List<ContainerItem> list;
                if (!groups.TryGetValue(item.ropeGroupId, out list))
                {
                    list = new List<ContainerItem>();
                    groups[item.ropeGroupId] = list;
                }
                list.Add(item);
            }

            if (groups.Count == 0)
                return;

            bool live = ropeEnabled && !shuffleContainers;
            Color lineColor = live ? ropeGizmoColor : Dimmed(ropeGizmoColor);
            Vector3 lift = Vector3.up * ropeGizmoYOffset;

            foreach (var pair in groups)
            {
                var chain = pair.Value;
                chain.Sort((a, b) => a.gridX.CompareTo(b.gridX));

                for (int i = 0; i + 1 < chain.Count; i++)
                {
                    Transform leftAnchor = chain[i].ropeAnchorRight;
                    Transform rightAnchor = chain[i + 1].ropeAnchorLeft;

                    Vector3 a = (leftAnchor != null ? leftAnchor.position : chain[i].transform.position) + lift;
                    Vector3 b = (rightAnchor != null ? rightAnchor.position : chain[i + 1].transform.position) + lift;

                    Gizmos.color = lineColor;
                    Gizmos.DrawLine(a, b);

                    DrawRopeGizmoAnchor(leftAnchor, a, live);
                    DrawRopeGizmoAnchor(rightAnchor, b, live);
                }
            }
        }

        /// <summary>画一个端点小球：锚点存在时用链条色，缺失时用醒目红（提示这辆车没配端点）。</summary>
        private void DrawRopeGizmoAnchor(Transform anchor, Vector3 pos, bool live)
        {
            if (anchor == null)
                Gizmos.color = RopeGizmoMissingAnchorColor;
            else
                Gizmos.color = live ? ropeGizmoColor : Dimmed(ropeGizmoColor);

            Gizmos.DrawSphere(pos, ropeGizmoAnchorRadius);
        }

        /// <summary>把颜色压暗成半透明（表示「这些连了也不会建绳」）。</summary>
        private static Color Dimmed(Color c)
        {
            c.a *= 0.35f;
            return c;
        }
    }
}
