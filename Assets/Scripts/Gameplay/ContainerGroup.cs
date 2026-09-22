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
                if (!IsRowReleased(GetItem(col, r)))
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
        private bool IsRowReleased(ContainerItem item)
        {
            if (item == null)
                return true;                    // 空格子：没有阻挡
            if (!item.IsEmpty)
                return false;                   // 还没找全匹配对象
            return !IsWaitingRopeCar(item);     // 装满但在等同组的绳车：仍未放行
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

            bool isLast = container.Consume();
            if (isLast)
            {
                OpenRearLidAfterMatch(container);   // 播放移入动画前，先开后盖（绳组在整组装满这一刻整组一起开）
                OnLastBoarding(container, pixel);   // 最后一个像素准备上车
            }
            consumingCount++;
            StartCoroutine(MovePixelToContainer(pixel, container, container.gridX, isLast));
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

            bool isLast = container.Consume();
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

            var driver = gone.GetComponent<ContainerExitDriver>();
            if (driver == null)
                driver = gone.gameObject.AddComponent<ContainerExitDriver>();
            driver.Play(() => RefillColumn(col), ropeRearExit);
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
            DetachFromRopeChain(item);   // 绳组：本车出局，先从链里摘掉，否则剩下的成员永远凑不齐
            Destroy(item.gameObject);   // 车 + 乘客像素一并销毁（乘客已计入 ClearedPixelCount）

            // 后车瞬间补位（teleport，无动画）：每车向上移一格，与 RefillColumn 同构（保留空格）
            for (int r = row + 1; r < rows; r++)
            {
                var rear = grid[col, r];
                if (rear == null)
                    continue;
                int newRow = r - 1;
                rear.gridZ = newRow;
                grid[col, newRow] = rear;
                grid[col, r] = null;
                rear.transform.localPosition = GetLocalPosition(col, newRow);
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

            var rear = GetItem(container.gridX, container.gridZ + 1);
            if (rear != null)
                rear.OpenLid();
        }

        /// <summary>某列后排容器依次前移一格（补位）。</summary>
        private void RefillColumn(int col)
        {
            for (int row = 1; row < rows; row++)
            {
                var it = grid[col, row];
                if (it == null)
                    continue;

                int newRow = row - 1;
                it.gridZ = newRow;
                grid[col, newRow] = it;
                grid[col, row] = null;
                StartCoroutine(MoveContainer(it, col, newRow));
            }
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

        /// <summary>清空所有 ContainerItem 子物体（先脱离父物体再销毁，避免同帧 GetComponentsInChildren 捡到旧物体）。</summary>
        public void ClearContainers()
        {
            consumingCount = 0;
            ClearRopes();   // 绳根引用着车，必须随车一起清掉
            var items = GetComponentsInChildren<ContainerItem>();
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

        /// <summary>在指定格子生成一个 ContainerItem 并应用颜色/容量（供运行时关卡加载使用）。</summary>
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
                    var it = grid[col, row];
                    if (it == null)
                        continue;
                    if (it.isRefilling)
                        return true;
                    if (row >= 1 && it.lidOpened && IsFrontCleared(col, row))
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
                if (!IsRowReleased(GetItem(col, r)))
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

                var car = FindCarForColor(pixel.colorId);
                if (car == null)
                {
                    unmatched.Add(pixel);
                    continue;
                }

                car.OpenLid();              // 有车被匹配 → 播放开盖 tween（幂等）
                if (car.gridZ < maxOpenRows)
                    ConsumePixel(pixel, car);        // 前 maxOpenRows 排：复用正常 jump 上车
                else
                    ConsumePixelInstant(pixel, car); // 更后排：原地消失 → 瞬移到目标车落点出现
            }
            return unmatched;
        }

        /// <summary>从全排（gridZ 0..rows-1）按「前排优先、同排列小优先」找第一个同色、非空、非补位中的车；无则 null。</summary>
        private ContainerItem FindCarForColor(int colorId)
        {
            for (int row = 0; row < rows; row++)
                for (int col = 0; col < columns; col++)
                {
                    var it = GetItem(col, row);
                    if (it == null || it.IsEmpty || it.isRefilling)
                        continue;
                    if (it.colorId != colorId)
                        continue;
                    return it;
                }
            return null;
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
