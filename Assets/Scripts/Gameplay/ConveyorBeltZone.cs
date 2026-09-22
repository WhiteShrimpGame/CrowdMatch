using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 传送带宿主：负责把「缓冲区释放出的像素」送入闭环传送带，并在远侧越过匹配闸口、正前方有同色 Container 时触发匹配吸收。
    /// 注入 ConveyorBelt 的 ShouldLeave / OnLeave 钩子；入口采用「槽位直接收集」——订阅 ConveyorBelt.SlotPassedEntry，
    /// 每个槽位过关口时独立地到缓冲区出口取最近的小球上车（reparent + localPosition→0 无瞬移），无全局单飞门控。
    /// 匹配采用「闸口法」：远侧沿 +X 运动，像素世界 X 从某列闸口之前跨到其后时，只在跨越的瞬间检测该列是否可匹配。
    /// </summary>
    public class ConveyorBeltZone : MonoBehaviour
    {
        [Header("引用")]
        [Tooltip("闭环传送带")]
        public ConveyorBelt belt;

        [Tooltip("容器组（匹配 / 吸收目标）")]
        public ContainerGroup containerGroup;

        [Tooltip("缓冲区（出口小球来源）。槽位过关口时从这里取最近的小球")]
        public CrowdBufferZone crowdBuffer;

        [Header("匹配")]
        [Tooltip("匹配闸口 X 偏移：闸口 X = 该列前排容器 X + 该值。远侧沿 +X 运动，像素越过闸口的瞬间才检测该列是否匹配")]
        public float matchGateOffsetX = 0f;

        [Tooltip("正前方纵向判定范围（远侧到容器前排的间隙）")]
        public float matchRangeZ = 0.8f;

        [Tooltip("上车收敛的旋转角速度（度/秒）：平滑到槽位途中前半段归 0、后半段转至 localEulerY = -90")]
        public float settleRotateSpeed = 360f;

        [Header("犯困检测")]
        [Tooltip("每次检测的触发概率系数：触发概率 = 该系数 × 犯困候选数（≥ 100% 必然触发）")]
        [Range(0f, 1f)] public float sleepChancePerPixel = 0.1f;

        [Tooltip("已在车上等待超过该秒数的乘客，也计入犯困候选")]
        public float sleepCarWaitSeconds = 3f;

        [Tooltip("只检查前几排的车上乘客（0 = 完全不检查车上乘客）")]
        public int sleepCarCheckRows = 3;

        [Header("排队生气检测")]
        [Tooltip("两次排队生气检测的间隔范围（秒）：每次检测后在该范围内随机取下次间隔（与犯困检测各自独立计时）")]
        public float queueAngryCheckIntervalMin = 3f;

        [Tooltip("两次排队生气检测的间隔上限（秒）")]
        public float queueAngryCheckIntervalMax = 6f;

        [Tooltip("两次检测的间隔范围（秒）：每次检测后在该范围与上限之间随机取下次间隔")]
        public float sleepCheckIntervalMin = 3f;

        [Tooltip("两次检测的间隔上限（秒）")]
        public float sleepCheckIntervalMax = 6f;

        private const float ArriveEpsilon = 0.05f;
        private const float BoardSmoothRate = 10f;   // localPosition 收敛速率（指数平滑）

        /// <summary>每槽乘员绕过的整圈数（相位回绕计数）；该槽换人 / 空置时归零。</summary>
        private int[] _laps;

        /// <summary>每槽上一帧的归一化相位（NaN = 尚未锚定，本帧只记录不计数）。</summary>
        private float[] _prevPhase;

        /// <summary>距下次犯困检测的剩余秒数。</summary>
        private float _sleepTimer;

        /// <summary>距下次排队生气检测的剩余秒数。</summary>
        private float _queueAngryTimer;

        /// <summary>犯困检测的候选像素（复用，避免每次检测都新建列表）。</summary>
        private readonly List<PixelItem> _boredBuffer = new List<PixelItem>();

        /// <summary>收集车上乘客时的复用缓冲。</summary>
        private readonly List<PixelItem> _passengerBuffer = new List<PixelItem>();

        /// <summary>收集缓冲区等待像素时的复用缓冲。</summary>
        private readonly List<PixelItem> _waitingBuffer = new List<PixelItem>();

        /// <summary>每列闸口的世界 X（容器组静止，惰性构建；容器组位置 / 列数 / 偏移变化时重建）。</summary>
        private float[] _gateX;

        /// <summary>构建 _gateX 时的容器组位置基准，用于侦测其移动。</summary>
        private Vector3 _gateOriginCached;

        /// <summary>构建 _gateX 时使用的偏移，用于侦测运行期调参。</summary>
        private float _gateOffsetCached = float.NaN;

        /// <summary>每槽乘员上一帧的世界 X（闸口跨越检测）。NaN = 该槽尚未锚定，本帧只记录不判定。</summary>
        private float[] _prevMatchX;

        /// <summary>ShouldLeave 命中时暂存的吸收目标容器，供随后的 OnLeave 直接使用（避免重复查找）。</summary>
        private ContainerItem _pendingContainer;

        /// <summary>占用槽位数（供 UI）。</summary>
        public int OccupiedSlots => belt != null ? belt.OccupiedCount : 0;

        /// <summary>传送带总容量（供 UI）。</summary>
        public int TotalSlots => belt != null ? belt.slotCount : 0;

        private void Start()
        {
            if (belt != null)
            {
                belt.ShouldLeave = ShouldLeave;
                belt.OnLeave = OnLeave;
                belt.SlotPassedEntry += OnSlotPassedEntry;
                belt.SlotCatchUpChanged += OnSlotCatchUpChanged;
            }

            _sleepTimer = NextInterval(sleepCheckIntervalMin, sleepCheckIntervalMax);
            _queueAngryTimer = NextInterval(queueAngryCheckIntervalMin, queueAngryCheckIntervalMax);
        }

        private void Update()
        {
            UpdateLaps();
            UpdateSleepCheck();
            UpdateQueueAngryCheck();
        }

        /// <summary>
        /// 逐槽统计「绕过的整圈数」：相位每回绕一次 +1（追赶平移也真的把槽位沿轨迹往前推，同样计入）。
        /// 空槽归零、换人时由 ResetLapTracking 归零。
        /// </summary>
        private void UpdateLaps()
        {
            int slotCount = belt != null ? belt.slotCount : 0;
            if (slotCount <= 0)
                return;

            if (_laps == null || _laps.Length != slotCount)
            {
                _laps = new int[slotCount];
                _prevPhase = new float[slotCount];
                for (int i = 0; i < slotCount; i++)
                    _prevPhase[i] = float.NaN;
            }

            for (int i = 0; i < slotCount; i++)
            {
                if (belt.GetItem(i) == null)
                {
                    _laps[i] = 0;                  // 空槽不计数
                    _prevPhase[i] = float.NaN;
                    continue;
                }

                float phase = belt.GetSlotPhase(i);
                if (!float.IsNaN(_prevPhase[i]) && phase < _prevPhase[i])
                    _laps[i]++;                    // 相位回绕 = 走完一圈
                _prevPhase[i] = phase;
            }
        }

        /// <summary>
        /// 犯困表情：每隔一段随机间隔检测一次，统计犯困候选数 N（传送带上绕圈一圈以上的 + 已在车上等太久的乘客），
        /// 以 概率系数 × N 触发一次（≥ 100% 必然触发）；命中则在这些候选里随机一个，用跟随模式播犯困表情。
        /// </summary>
        private void UpdateSleepCheck()
        {
            if (belt == null)
                return;

            _sleepTimer -= Time.deltaTime;
            if (_sleepTimer > 0f)
                return;

            _sleepTimer = NextInterval(sleepCheckIntervalMin, sleepCheckIntervalMax);

            CollectBoredPixels(_boredBuffer);
            if (_boredBuffer.Count == 0)
                return;

            float chance = sleepChancePerPixel * _boredBuffer.Count;
            if (chance < 1f && Random.value > chance)
                return;

            var pixel = _boredBuffer[Random.Range(0, _boredBuffer.Count)];
            var emoji = EmojiManager.Instance;
            if (emoji != null)
                emoji.PlaySleepEmoji(pixel);
        }

        /// <summary>
        /// 收集犯困候选：传送带上已绕圈一圈以上的像素，以及已在车上等待超过 sleepCarWaitSeconds 的乘客。
        /// 只收配了表情节点的（没节点就没法显示）。
        /// </summary>
        private void CollectBoredPixels(List<PixelItem> outList)
        {
            outList.Clear();

            // 传送带上绕圈超过一圈的
            if (belt != null && _laps != null)
            {
                int slotCount = Mathf.Min(_laps.Length, belt.slotCount);
                for (int i = 0; i < slotCount; i++)
                {
                    if (_laps[i] < 1)
                        continue;

                    var pixel = belt.GetItem(i) as PixelItem;
                    if (pixel != null && pixel.emojiNode != null)
                        outList.Add(pixel);
                }
            }

            // 已在车上等了太久的乘客（只看前 sleepCarCheckRows 排）
            if (containerGroup == null)
                return;

            int checkRows = Mathf.Min(containerGroup.rows, Mathf.Max(0, sleepCarCheckRows));
            for (int col = 0; col < containerGroup.columns; col++)
            {
                for (int row = 0; row < checkRows; row++)
                {
                    var car = containerGroup.GetItem(col, row);
                    if (car == null)
                        continue;

                    _passengerBuffer.Clear();
                    car.CollectPassengers(_passengerBuffer);

                    for (int i = 0; i < _passengerBuffer.Count; i++)
                    {
                        var pixel = _passengerBuffer[i];
                        if (pixel.emojiNode == null || float.IsNaN(pixel.boardedAt))
                            continue;   // 没表情节点 / 还在上车途中
                        if (Time.time - pixel.boardedAt < sleepCarWaitSeconds)
                            continue;
                        outList.Add(pixel);
                    }
                }
            }
        }

        /// <summary>检测间隔：在给定的两个值之间随机（顺序填反也能用，下限兜到 0.01 秒）。</summary>
        private static float NextInterval(float a, float b)
        {
            float min = Mathf.Max(0.01f, Mathf.Min(a, b));
            float max = Mathf.Max(0.01f, Mathf.Max(a, b));
            return Random.Range(min, max);
        }

        /// <summary>把某槽位的圈数统计复位（该槽换人 / 被清空）。</summary>
        private void ResetLapTracking(int slotIndex)
        {
            if (_laps == null || slotIndex < 0 || slotIndex >= _laps.Length)
                return;

            _laps[slotIndex] = 0;
            _prevPhase[slotIndex] = float.NaN;
        }

        /// <summary>槽位追赶状态变化：驱动该槽位乘员的走/停动画（追赶 = Walking，否则 = Idle）。</summary>
        private void OnSlotCatchUpChanged(int slotIndex, bool catchingUp)
        {
            if (belt == null)
                return;
            var pixel = belt.GetItem(slotIndex) as PixelItem;
            if (pixel != null)
                pixel.SetWalking(catchingUp);
        }

        /// <summary>某槽位过关口：若该槽仍空且出口有球，取最近小球直接上车。每个槽位独立，互不阻塞。</summary>
        private void OnSlotPassedEntry(int slotIndex)
        {
            if (belt == null || crowdBuffer == null)
                return;
            if (belt.GetItem(slotIndex) != null)
                return;   // 该槽仍被占（异常），跳过本次收集

            var pixel = crowdBuffer.CollectNearest();
            if (pixel == null)
                return;

            if (!belt.TryEnter(pixel, slotIndex))
            {
                // 防御：此处槽位刚确认仍空，单线程下不会失败；真失败则销毁避免泄漏
                Destroy(pixel.gameObject);
                return;
            }

            // 新乘员上车：闸口跨越检测与圈数统计重新锚定，避免沿用上一乘员留下的旧值
            ResetSlotTracking(slotIndex);
            ResetLapTracking(slotIndex);

            // 进入传送带：播放音效 + 轻震动
            if (AudioManager.Instance != null)
                AudioManager.Instance.Play("OnBelt");
            if (GameManager.Instance != null)
                GameManager.Instance.TriggerVibrate(0);

            // 生气表情**保留**：缓冲区 → 传送带不收，直到「上车」才移除（见 OnLeave）。
            // 插队判定：本像素上车时，缓冲区里是否还有比它更早点击、且颜色不同的像素在等
            CheckQueueJump(pixel);

            StartCoroutine(SettleRoutine(pixel, slotIndex));

            // 关键事件点：有小人进入传送带 → 尝试失败判定
            var gc = GameController.Instance;
            if (gc != null)
                gc.TryCheckFail(GameController.FailCheckpoint.SlotEntered);
        }

        /// <summary>
        /// 排队生气：每隔一段随机间隔检查一次缓冲区里「排队超过阈值」的像素，交给表情管理器按 概率系数 × 人数
        /// 决定是否在其中随机一个上播生气表情（判定上与插队相互独立，复用同一个 emoji；无 CD，节流靠这个间隔）。
        /// </summary>
        private void UpdateQueueAngryCheck()
        {
            if (crowdBuffer == null)
                return;

            _queueAngryTimer -= Time.deltaTime;
            if (_queueAngryTimer > 0f)
                return;

            _queueAngryTimer = NextInterval(queueAngryCheckIntervalMin, queueAngryCheckIntervalMax);

            _waitingBuffer.Clear();
            crowdBuffer.CollectWaiting(_waitingBuffer);
            if (_waitingBuffer.Count == 0)
                return;

            var emoji = EmojiManager.Instance;
            if (emoji != null)
                emoji.TryPlayAngryEmojiForQueue(_waitingBuffer);
        }

        /// <summary>
        /// 插队判定：本像素刚上带，若缓冲区里还有「比它更早被点击、且颜色不同」的像素仍在排队（后点的先上了带），
        /// 就把等待队列与这个上带像素交给表情管理器，由它按各自的概率 / 门槛 / CD 决定是否触发：
        /// 生气表情落在**被插队者**中随机一个头上、开心表情落在**插队者（本像素）**头上，两者相互独立（可能同帧一起出现）。
        /// </summary>
        private void CheckQueueJump(PixelItem boardingPixel)
        {
            if (boardingPixel == null || crowdBuffer == null)
                return;

            _waitingBuffer.Clear();
            crowdBuffer.CollectWaiting(_waitingBuffer);
            if (_waitingBuffer.Count == 0)
                return;

            var emoji = EmojiManager.Instance;
            if (emoji == null)
                return;

            emoji.TryPlayAngryEmojiForJumped(_waitingBuffer, boardingPixel);
            emoji.TryPlayHappyEmojiForJumped(_waitingBuffer, boardingPixel);
        }

        /// <summary>上车收敛：localPosition 平滑到槽位 0 点的途中，前半段 localRotation 归 0、后半段 localEulerY 匀速转至 -90。每个小球一条协程，互不阻塞。</summary>
        private IEnumerator SettleRoutine(PixelItem pixel, int slotIndex)
        {
            float startDist = pixel.transform.localPosition.magnitude;
            bool secondHalf = false;

            while (pixel != null)
            {
                float k = 1f - Mathf.Exp(-BoardSmoothRate * Time.deltaTime);
                pixel.transform.localPosition = Vector3.Lerp(pixel.transform.localPosition, Vector3.zero, k);

                // 位置收敛进度：0 = 起点，1 = 到达槽位，按已走距离占初始距离的比例划分前后半段
                float progress = startDist > ArriveEpsilon
                    ? 1f - pixel.transform.localPosition.magnitude / startDist
                    : 1f;
                progress = Mathf.Clamp01(progress);

                if (progress < 0.5f)
                {
                    // 前半段：localRotation 归 0
                    pixel.transform.localRotation = Quaternion.RotateTowards(
                        pixel.transform.localRotation, Quaternion.identity, settleRotateSpeed * Time.deltaTime);
                }
                else
                {
                    // 后半段：localEulerY 匀速转至 -90（X/Z 归 0）
                    secondHalf = true;
                    Vector3 euler = pixel.transform.localEulerAngles;
                    float newY = Mathf.MoveTowardsAngle(euler.y, -90f, settleRotateSpeed * Time.deltaTime);
                    pixel.transform.localRotation = Quaternion.Euler(0f, newY, 0f);
                }

                bool posDone = pixel.transform.localPosition.sqrMagnitude <= ArriveEpsilon * ArriveEpsilon;
                bool rotDone = secondHalf
                    ? Mathf.Abs(Mathf.DeltaAngle(pixel.transform.localEulerAngles.y, -90f)) <= 0.5f
                    : Quaternion.Angle(pixel.transform.localRotation, Quaternion.identity) <= 0.5f;
                if (posDone && rotDone)
                    break;

                yield return null;
            }
            if (pixel != null)
            {
                pixel.transform.localPosition = Vector3.zero;
                pixel.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
                // 落定后按当前槽位追赶状态决定走/停：追赶保持 Walking，否则回到 Idle（相对静止）
                if (belt != null)
                    pixel.SetWalking(belt.IsSlotCatchingUp(slotIndex));
            }
        }

        /// <summary>
        /// 离开判定（闸口法）：像素上一帧还在某列闸口（该列前排容器 X + matchGateOffsetX）之前、本帧越到其后时，
        /// 只在越过的这一瞬间检测该列是否有同色可匹配容器。远侧沿 +X 运动，故跨越判据固定为「上一帧小于闸口、本帧不小于闸口」。
        /// 命中则暂存目标容器，交给紧随其后的 OnLeave 吸收。记录模式下仍为「到达远侧即离开」。
        /// </summary>
        private bool ShouldLeave(int slotIndex, IConveyorItem item)
        {
            var pixel = item as PixelItem;
            if (pixel == null || item.Transform == null)
                return false;

            var gc = GameController.Instance;
            if (gc != null && gc.recordMode)
                return IsAtFarSide(pixel);

            if (containerGroup == null)
                return false;

            EnsureGates();
            if (!EnsureTracking(slotIndex))
                return false;

            // 位置每帧都记录（含绕圈途中），保证跨回远侧时上一帧 X 是连续的，不会用到上一圈的旧值
            float currX = pixel.transform.position.x;
            float prevX = _prevMatchX[slotIndex];
            _prevMatchX[slotIndex] = currX;
            if (float.IsNaN(prevX))
                return false;   // 本槽首次记录：本帧只锚定起点，不判定

            // 闸口只在远侧直线上，纵向离开远侧范围就不必判定（保留原 matchRangeZ 的语义）
            if (!IsAtFarSide(pixel))
                return false;

            _pendingContainer = null;

            // 本帧跨过的闸口（卡顿时可能不止一个），按列序 = 远侧行进顺序取第一个可匹配的列
            for (int col = 0; col < _gateX.Length; col++)
            {
                float gateX = _gateX[col];
                if (prevX >= gateX || currX < gateX)
                    continue;

                var container = containerGroup.FindMatchableInColumn(col, pixel.colorId);
                if (container != null)
                {
                    _pendingContainer = container;
                    return true;
                }
            }
            return false;
        }

        /// <summary>离开回调：正常模式吸收 ShouldLeave 暂存的同色前排 Container；记录模式下直接消失并写入序列文件。</summary>
        private void OnLeave(int slotIndex, IConveyorItem item)
        {
            ResetSlotTracking(slotIndex);
            ResetLapTracking(slotIndex);

            var pixel = item as PixelItem;
            if (pixel == null)
                return;

            // 上车：收掉犯困表情与生气表情——跟随模式下它们是像素的子物体，不主动收会跟着像素一起进车。
            // 生气表情在缓冲区 → 传送带这一段是**保留**的（只在这里、也就是真正上车时才移除）。
            var emoji = EmojiManager.Instance;
            if (emoji != null)
            {
                emoji.RemoveSleepEmoji(pixel);
                emoji.RemoveAngryEmoji(pixel);
            }

            var gc = GameController.Instance;
            if (gc != null && gc.recordMode)
            {
                gc.RecordBall(pixel.colorId);
                Destroy(pixel.gameObject);
                return;
            }

            var container = _pendingContainer;
            _pendingContainer = null;
            if (container != null && containerGroup != null)
                containerGroup.ConsumePixel(pixel, container);
        }

        /// <summary>惰性构建每列闸口世界 X（容器组静止；位置 / 列数 / matchGateOffsetX 变化时重建，便于运行时调参）。</summary>
        private void EnsureGates()
        {
            int columns = containerGroup.columns;
            Vector3 origin = containerGroup.transform.position;
            if (_gateX != null && _gateX.Length == columns
                && _gateOriginCached == origin
                && Mathf.Approximately(_gateOffsetCached, matchGateOffsetX))
                return;

            _gateOriginCached = origin;
            _gateOffsetCached = matchGateOffsetX;
            _gateX = new float[columns];
            for (int col = 0; col < columns; col++)
            {
                Vector3 front = containerGroup.transform.TransformPoint(containerGroup.GetLocalPosition(col, 0));
                _gateX[col] = front.x + matchGateOffsetX;
            }
        }

        /// <summary>确保该槽位已有上一帧 X 记录；无记录（数组未建 / 槽位越界）时返回 false。</summary>
        private bool EnsureTracking(int slotIndex)
        {
            int slotCount = belt != null ? belt.slotCount : 0;
            if (slotCount <= 0)
                return false;

            if (_prevMatchX == null || _prevMatchX.Length != slotCount)
            {
                _prevMatchX = new float[slotCount];
                for (int i = 0; i < slotCount; i++)
                    _prevMatchX[i] = float.NaN;
            }

            return slotIndex >= 0 && slotIndex < slotCount;
        }

        /// <summary>把某槽位的闸口跟踪复位（该槽换人 / 被清空）。</summary>
        private void ResetSlotTracking(int slotIndex)
        {
            if (_prevMatchX != null && slotIndex >= 0 && slotIndex < _prevMatchX.Length)
                _prevMatchX[slotIndex] = float.NaN;
        }

        /// <summary>把所有槽位的闸口跟踪与圈数统计复位（整条传送带被清空）。</summary>
        private void ResetAllTracking()
        {
            if (_prevMatchX != null)
            {
                for (int i = 0; i < _prevMatchX.Length; i++)
                    _prevMatchX[i] = float.NaN;
            }

            if (_laps != null)
            {
                for (int i = 0; i < _laps.Length; i++)
                {
                    _laps[i] = 0;
                    _prevPhase[i] = float.NaN;
                }
            }

            _pendingContainer = null;
        }

        /// <summary>像素是否到达传送带远侧（以 ContainerGroup 前排 Z 为基准，纵向落入 matchRangeZ）。</summary>
        private bool IsAtFarSide(PixelItem pixel)
        {
            if (containerGroup == null)
                return false;
            // 前排 row 0 的本地 Z = 0，故世界 Z 即 containerGroup 原点 Z
            float frontZ = containerGroup.transform.position.z;
            return Mathf.Abs(pixel.transform.position.z - frontZ) <= matchRangeZ;
        }

        /// <summary>清空传送带上所有像素（供重载关卡时清理）。</summary>
        public void ClearBelt()
        {
            ResetAllTracking();
            if (belt == null)
                return;
            for (int i = 0; i < belt.slotCount; i++)
            {
                var pixel = belt.GetItem(i) as PixelItem;
                if (pixel == null)
                    continue;
                belt.ClearSlot(i);
                Destroy(pixel.gameObject);
            }
        }

        /// <summary>
        /// 网格内像素已全部被点走：传送带开始逐渐加速到自己的上限（幂等）。
        /// 触发点在「点击移出」之后，此时带上的存量还在绕圈等匹配，加速纯粹是为了收尾快一些。
        /// </summary>
        public void NotifyGridEmptied()
        {
            if (belt != null)
                belt.BeginClearedSpeedUp();
        }

        /// <summary>回到常规速度（进入下一关 / 重载关卡时调用）。</summary>
        public void ResetSpeed()
        {
            if (belt != null)
                belt.ResetSpeed();
        }

        /// <summary>
        /// 复活用：保留前 keepCount 个占用槽位的像素，其余槽位取下（解绑 carrier、保持世界位置）并返回。
        /// 返回的像素已无父物体，供调用方直接匹配到后排车。
        /// </summary>
        public List<PixelItem> DrainBeltKeep(int keepCount)
        {
            var removed = new List<PixelItem>();
            if (belt == null)
                return removed;

            var occupied = new List<int>();
            for (int i = 0; i < belt.slotCount; i++)
                if (belt.GetItem(i) != null)
                    occupied.Add(i);

            int keep = Mathf.Clamp(keepCount, 0, occupied.Count);
            for (int k = keep; k < occupied.Count; k++)
            {
                int slot = occupied[k];
                var pixel = belt.GetItem(slot) as PixelItem;
                if (pixel != null)
                    removed.Add(pixel);
                belt.ClearSlot(slot);
                ResetSlotTracking(slot);
                ResetLapTracking(slot);
            }
            return removed;
        }
    }
}
