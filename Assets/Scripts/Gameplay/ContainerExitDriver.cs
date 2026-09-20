using System;
using System.Collections;
using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 小车出库动画，两种走法，**换轴逻辑相同**、只是根节点与运动参数不同：
    /// <list type="bullet">
    /// <item><b>正常出车</b>：用「前轴 / 后轴 + 父物体切换」驱动小车先倒车、再出车转正、最后整车直行开出场景。</item>
    /// <item><b>绳连后车</b>（<see cref="ropeRearExitEnabled"/>）：跳过倒车，切到**单独配置的转轴**
    /// （<see cref="ContainerItem.ropeExitAxle"/>，留空退回前轴），从正姿（0°）先甩到
    /// <see cref="ropeExitMaxAngle"/> 再加速归 0 出车；运动参数与正常出车**完全独立**。</item>
    /// </list>
    /// 因为换轴逻辑一致，**侧翻与弹性缩放对两种走法都生效**。
    /// 轴引用取自同物体上的 ContainerItem.frontAxle / rearAxle / reverseScaleAxle / rollAxle；未配置轴时回退为「直接补位 + 销毁」。
    /// 倒车缩放轴（reverseScaleAxle）夹在驱动轴与车体之间做惯性夸张；侧翻自转轴（rollAxle）是最深层节点，出车转正时侧翻、转正后归 0。
    /// 所有 SetParent 都用 worldPositionStays:true 保持世界位姿，零瞬移；偏航（eulerY）写世界 rotation，侧翻（eulerX）写自转轴 localRotation。
    /// </summary>
    public class ContainerExitDriver : MonoBehaviour
    {
        [Header("倒车 / Reverse")]
        [Tooltip("倒车总时长（秒）")]
        public float reverseDuration = 0.4f;

        [Tooltip("倒车总位移（米，沿车尾 +X）")]
        public float reverseDistance = 0.6f;

        [Tooltip("倒车总角度（度，正值为车头甩向负 Y）")]
        public float reverseAngle = 35f;

        [Tooltip("倒车后等待时长（秒）")]
        public float reverseWait = 0.15f;

        [Header("倒车缩放 / Squash")]
        [Tooltip("倒车缩放目标值（倒车+等待结束时 X 缩放到该值，1=不变）")]
        public float reverseSquashScale = 0.6f;

        [Tooltip("倒车缩放起始延迟（从倒车开始计时，多久后开始匀减速缩放；缩放覆盖剩余的倒车 + 等待时间）")]
        public float reverseSquashDelay = 0.2f;

        [Tooltip("出车开始时缩放匀加速回到 1 的时长（秒）")]
        public float exitScaleRecoverDuration = 0.3f;

        [Header("出车 / Exit")]
        [Tooltip("出车转正段（角度回正前）线性加速度（米/秒²）")]
        public float exitAcceleration = 8f;

        [Tooltip("出车角度加速度（度/秒²，先甩头到 -exitMaxAngle 再加速归 0）")]
        public float exitAngularAcceleration = 300f;

        [Tooltip("出车最大角度（度，正值为车头甩向负 Y 的最大值；出车转正前先甩到此角再归 0）")]
        public float exitMaxAngle = 55f;

        [Tooltip("出车最大速度（米/秒）")]
        public float exitMaxSpeed = 6f;

        [Tooltip("转正后整车直行时长（秒），到点销毁")]
        public float exitDriveDuration = 0.8f;

        [Tooltip("转正后整车直行段（角度回正后）线性加速度（米/秒²），可与转正前加速度分别配置")]
        public float exitDriveAcceleration = 8f;

        [Header("侧翻 / Roll")]
        [Tooltip("侧翻最大角度（度，正值为绕前进轴的一侧；出车开始即先匀加速后匀减速侧翻到该角度）")]
        public float rollMaxAngle = 12f;

        [Tooltip("侧翻到位时长（秒，自出车开始计时，先匀加速后匀减速侧翻到 rollMaxAngle 的时长）")]
        public float rollOutDuration = 0.4f;

        [Tooltip("侧翻归零时长（秒，位移角度归 0 后匀加速回到 0）")]
        public float rollRecoverDuration = 0.4f;

        [Header("弹性缩放 / Elastic Scale")]
        [Tooltip("弹性缩放最终缩放值（Vector3，侧翻归 0 后从 (1,1,1) 匀减速缩放到该值；1=不变）")]
        public Vector3 elasticTargetScale = new Vector3(1.2f, 0.83f, 1.2f);

        [Tooltip("弹性缩放到位时长（秒，匀减速缩放到 elasticTargetScale 的时长）")]
        public float elasticScaleDuration = 0.15f;

        [Tooltip("弹性缩放复原时长（秒，到达最大值后立即匀加速回到 1 的时长）")]
        public float elasticRecoverDuration = 0.2f;

        [Header("绳连后车出车 / Rope Rear Exit")]
        [Tooltip("绳组里**非头车**的出车方式：跳过倒车、直接切前轴，从正姿（0°）先甩到 Rope Exit Max Angle 再加速归 0。下方参数与上面的正常出车完全独立")]
        public bool ropeRearExitEnabled = true;

        [Tooltip("甩头最大角度（度，正值为车头甩向负 Y；从 0° 甩到此角再归 0）")]
        public float ropeExitMaxAngle = 55f;

        [Tooltip("出车角度加速度（度/秒²）")]
        public float ropeExitAngularAcceleration = 300f;

        [Tooltip("出车转正段线性加速度（米/秒²）")]
        public float ropeExitAcceleration = 8f;

        [Tooltip("出车最大速度（米/秒）")]
        public float ropeExitMaxSpeed = 6f;

        [Tooltip("转正后整车直行时长（秒），到点销毁")]
        public float ropeExitDriveDuration = 0.8f;

        [Tooltip("转正后整车直行段线性加速度（米/秒²），可与转正前分别配置")]
        public float ropeExitDriveAcceleration = 8f;

        private bool _playing;

        /// <summary>出车时从 SpawnPool 生成的拖尾物体（挂在 ContainerItem.trailParent 下）；车销毁前回收。</summary>
        private GameObject _trail;

        /// <summary>出车起步音效序列：连续出车时依次轮换。</summary>
        private static readonly string[] CarLeaveTags = { "CarLeave", "CarLeave2", "CarLeave3" };

        /// <summary>出车起步音效的轮换窗口（秒）：上次播放距今不超过该值则换下一段，超过则回到第一段。</summary>
        private const float CarLeaveLoopWindow = 2f;

        /// <summary>下一段出车音效的下标（静态：跨所有小车共享，同一时间可能有多辆车出库）。</summary>
        private static int _carLeaveIndex;

        /// <summary>上一次播放出车音效的时间（Time.time）。</summary>
        private static float _carLeaveLastTime = float.NegativeInfinity;

        /// <summary>「绳连后车甩头参数为 0」这条警告只打一次（每辆车都有各自的 driver，避免成组出车时刷屏）。</summary>
        private static bool _warnedRopeExitNoSwing;

        /// <summary>启动出库动画；转正瞬间调用 onRefill（补位回调）。</summary>
        /// <param name="ropeRearExit">
        /// true = 绳组的**非头车**：跳过倒车、直接切前轴，从正姿（0°）起步甩头（运动参数走 Rope Rear Exit 那一组）。
        /// 会被 <see cref="ropeRearExitEnabled"/> 总开关拦一道。
        /// </param>
        public void Play(Action onRefill, bool ropeRearExit = false)
        {
            if (_playing)
                return;
            _playing = true;
            StartCoroutine(Run(onRefill, ropeRearExit && ropeRearExitEnabled));
        }

        private IEnumerator Run(Action onRefill, bool ropeRearExit)
        {
            var container = GetComponent<ContainerItem>();
            Transform front = container != null ? container.frontAxle : null;
            Transform rear = container != null ? container.rearAxle : null;
            Transform scale = container != null ? container.reverseScaleAxle : null;
            Transform roll = container != null ? container.rollAxle : null;
            Transform elastic = container != null ? container.elasticScaleAxle : null;

            // 轴未配置：回退到旧「直接补位 + 销毁」
            if (front == null || rear == null)
            {
                onRefill?.Invoke();
                Destroy(gameObject);
                yield break;
            }

            Transform cartParent = transform.parent;   // 小车原始父物体（ContainerGroup）

            // 绳连后车出车时的转轴：单独配置（ContainerItem.ropeExitAxle）；没配则退回用前轴（= 原版行为）
            Transform ropeAxle = container != null && container.ropeExitAxle != null ? container.ropeExitAxle : front;

            // 出车段的运动参数：绳连后车走独立的一组，与正常出车互不影响
            float maxAngle   = ropeRearExit ? ropeExitMaxAngle            : exitMaxAngle;
            float angAccel   = ropeRearExit ? ropeExitAngularAcceleration : exitAngularAcceleration;
            float linAccel   = ropeRearExit ? ropeExitAcceleration        : exitAcceleration;
            float maxSpeed   = ropeRearExit ? ropeExitMaxSpeed            : exitMaxSpeed;
            float driveDur   = ropeRearExit ? ropeExitDriveDuration       : exitDriveDuration;
            float driveAccel = ropeRearExit ? ropeExitDriveAcceleration   : exitDriveAcceleration;

            if (ropeRearExit)
            {
                // ===== 绳连后车：跳过倒车，切到绳后车转轴（ropeAxle，可单独配置）=====
                // 换轴逻辑与正常出车的 ReverseAndSwitchAxle 完全同构，只是根节点换成 ropeAxle：
                // 必须**逐层**把 缩放轴 → 自转轴 → 车体 挂到链上，最后车体落在最深层节点下——
                // 只挂其中一层的话车体不会随轴走（既不换轴、也不转，只是原地不动）。
                // 因为换轴照旧，侧翻与弹性缩放对这条路径同样生效（它们依赖车体挂在那两个轴下）。
                ropeAxle.SetParent(cartParent, true);   // 转轴脱离小车 → 挂到与车体同父级
                ropeAxle.localScale = Vector3.one;      // 纯 pivot，重置 scale
                Transform chainHead = ropeAxle;
                if (scale != null) { scale.SetParent(chainHead, true); chainHead = scale; }
                if (roll != null)  { roll.SetParent(chainHead, true);  chainHead = roll; }
                transform.SetParent(chainHead, true);   // 车体挂到最深层节点下

                WarnIfRopeExitHasNoSwing();

                PlayCarLeaveSfx();   // 没有倒车段，起步音效与震动挪到出车开始的这一刻
                if (GameManager.Instance != null)
                    GameManager.Instance.TriggerVibrate(1);
            }
            else
            {
                yield return ReverseAndSwitchAxle(cartParent, front, rear, scale, roll);
            }

            // 出车段的驱动对象：正常出车是前轴，绳连后车是它自己的转轴——两者都靠链条带着车体走
            Transform drive = ropeRearExit ? ropeAxle : front;

            // ===== 出车转正：由 drive 驱动（正常出车 = 前轴，绳连后车 = ropeExitAxle） =====
            float v = 0f;
            // 绳连后车没有倒车，从正姿起步；正常出车从倒车留下的 -reverseAngle 起步
            float angle = ropeRearExit ? 0f : -reverseAngle;
            float startAngleAbs = ropeRearExit ? 0f : reverseAngle;
            float angularVel = 0f;
            bool swung = maxAngle <= startAngleAbs;   // 目标角不超过起点角时，跳过甩头直接归 0
            float recoverT = 0f;
            float rollT = 0f;   // 侧翻出车时钟（自出车开始计时）
            while (true)
            {
                float dt = Time.deltaTime;
                v = Mathf.Min(v + linAccel * dt, maxSpeed);
                drive.position += -drive.right * (v * dt);   // 沿自身 left（车头 -X）位移（线性照旧，全程推进）

                // 出车开始：缩放匀加速回到 1（只有正常出车会先倒车挤压，绳连后车没有这一步）
                if (!ropeRearExit && scale != null && recoverT < exitScaleRecoverDuration)
                {
                    recoverT += dt;
                    float rp = Mathf.Clamp01(recoverT / exitScaleRecoverDuration);
                    SetScaleX(scale, Mathf.Lerp(reverseSquashScale, 1f, rp * rp));
                }

                // 出车开始：侧翻先匀加速后匀减速到 rollMaxAngle（时间驱动，覆盖整个出车段）
                if (roll != null)
                {
                    rollT += dt;
                    roll.localRotation = Quaternion.Euler(rollMaxAngle * EaseInOutQuad(Mathf.Clamp01(rollT / rollOutDuration)), 0f, 0f);
                }

                if (!swung)
                {
                    // 第一阶段：加速变大（甩头）到 -maxAngle
                    angularVel -= angAccel * dt;
                    angle += angularVel * dt;
                    if (angle <= -maxAngle)
                    {
                        angle = -maxAngle;
                        angularVel = -angularVel;   // 立即反向角速度
                        swung = true;               // 固定进入归 0 阶段，不再回甩
                    }
                }
                else
                {
                    // 第二阶段：反向后加速归 0
                    angularVel += angAccel * dt;
                    angle += angularVel * dt;
                    if (angle >= 0f)
                    {
                        drive.rotation = Quaternion.Euler(0f, 0f, 0f);   // 转正

                        // 转正后才挂拖尾：生成后随车移动，车销毁前回收
                        SpawnTrail(container);

                        // 恢复位移轴 + 缩放轴，但保留自转轴作为小车父物体（侧翻持续到归 0）
                        if (scale != null)
                            SetScaleX(scale, 1f);           // 先让缩放轴归 1（小车仍在其下，围绕正确 pivot 解除挤压）
                        if (roll != null)
                        {
                            roll.localRotation = Quaternion.Euler(rollMaxAngle, 0f, 0f);   // 侧翻到位
                            roll.SetParent(cartParent, true);   // 自转轴（带小车）提到原始父物体
                        }
                        else
                        {
                            transform.SetParent(cartParent, true);   // 无自转轴：小车回原始父物体
                        }
                        drive.SetParent(transform, true);        // 驱动轴（前轴 / 绳后车转轴）归位为小车子物体
                        if (scale != null)
                        {
                            scale.SetParent(transform, true);    // 缩放轴归位
                            SetScaleX(scale, 1f);
                        }

                        onRefill?.Invoke();   // 转正瞬间触发后排补位
                        break;
                    }
                }
                drive.rotation = Quaternion.Euler(0f, angle, 0f);
                yield return null;
            }

            // ===== 整车直行：加速到最大后匀速，侧翻匀加速归 0，归 0 后换弹性缩放轴做 XZ 放大/Y 缩小的弹性，到点销毁 =====
            float hold = 0f;
            float rollRecoverT = 0f;
            bool rollRestored = roll == null;
            float elasticOutT = 0f;
            float elasticRecoverT = 0f;
            bool elasticStarted = false;
            bool elasticRestored = elastic == null;   // 弹性轴未配置则跳过
            while (hold < driveDur)
            {
                float dt = Time.deltaTime;
                v = Mathf.Min(v + driveAccel * dt, maxSpeed);
                transform.position += -transform.right * (v * dt);
                hold += dt;

                // 侧翻匀加速归 0（ease-in quad），归 0 后自转轴还给小车
                if (!rollRestored)
                {
                    rollRecoverT += dt;
                    if (rollRecoverT >= rollRecoverDuration)
                    {
                        roll.localRotation = Quaternion.identity;   // 侧翻归 0
                        transform.SetParent(cartParent, true);      // 小车先脱离自转轴（自转轴已归 0，不烘）
                        roll.SetParent(transform, true);            // 自转轴还给小车
                        rollRestored = true;
                    }
                    else
                    {
                        float rp = Mathf.Clamp01(rollRecoverT / rollRecoverDuration);
                        roll.localRotation = Quaternion.Euler(rollMaxAngle * (1f - rp * rp), 0f, 0f);
                    }
                }

                // 侧翻归 0 后，单独应用弹性缩放轴（与其他轴不共存）：XZ 匀减速放大、Y 匀减速缩小，到位后立即匀加速复原
                if (rollRestored && !elasticRestored)
                {
                    if (!elasticStarted)
                    {
                        elastic.SetParent(cartParent, true);   // 弹性轴脱离小车 → 原始父物体
                        elastic.localScale = Vector3.one;      // 纯 pivot，重置 scale
                        transform.SetParent(elastic, true);    // 小车挂到弹性轴下
                        elasticStarted = true;
                    }
                    if (elasticOutT < elasticScaleDuration)
                    {
                        elasticOutT += dt;
                        float p = Mathf.Clamp01(elasticOutT / elasticScaleDuration);
                        float e = EaseOutQuad(p);   // 匀减速
                        elastic.localScale = Vector3.Lerp(Vector3.one, elasticTargetScale, e);
                    }
                    else
                    {
                        elasticRecoverT += dt;
                        float p = Mathf.Clamp01(elasticRecoverT / elasticRecoverDuration);
                        float e = p * p;   // 匀加速 ease-in
                        elastic.localScale = Vector3.Lerp(elasticTargetScale, Vector3.one, e);
                        if (elasticRecoverT >= elasticRecoverDuration)
                        {
                            elastic.localScale = Vector3.one;      // 复原
                            transform.SetParent(cartParent, true); // 小车脱离弹性轴
                            elastic.SetParent(transform, true);    // 弹性轴还给小车
                            elastic.localScale = Vector3.one;
                            elasticRestored = true;
                        }
                    }
                }

                yield return null;
            }

            DespawnTrail();   // 拖尾挂在车节点下，必须先回收再销毁车，否则会连带销毁、池里留下空引用
            Destroy(gameObject);
        }

        /// <summary>
        /// 正常出车的准备段：后轴驱动倒车 → 等待 → 位移级换轴到前轴。
        /// 结束后车体挂在链条（前轴 → 缩放轴 → 自转轴 → 车体）上、朝向为 <c>-reverseAngle</c>，由调用方接着跑出车段。
        /// 绳连后车跳过这一整段（见 <see cref="ropeRearExitEnabled"/>），所以这里只服务正常出车。
        /// </summary>
        private IEnumerator ReverseAndSwitchAxle(Transform cartParent, Transform front, Transform rear, Transform scale, Transform roll)
        {
            // ===== 倒车：后轴驱动（缩放轴若存在则夹在后轴与车体之间） =====
            rear.SetParent(cartParent, true);   // 后轴脱离小车 → 挂到原始父物体
            rear.localScale = Vector3.one;      // 轴始终是纯 pivot，重置 scale，避免继承小车的缩放
            Transform chainRoot = rear;
            if (scale != null) { scale.SetParent(chainRoot, true); chainRoot = scale; }   // 缩放轴 → 后轴下
            if (roll != null)  { roll.SetParent(chainRoot, true);  chainRoot = roll; }    // 自转轴 → 缩放轴下（最深层）
            transform.SetParent(chainRoot, true);   // 小车挂到自转轴（或缩放轴、后轴）

            // 开始倒车
            PlayCarLeaveSfx();

            //SpawnConfetti();

            float t = 0f;
            float prevS = 0f;
            float total = reverseDuration + reverseWait;
            float squashTotal = Mathf.Max(0.0001f, total - reverseSquashDelay);
            while (t < reverseDuration)
            {
                float dt = Time.deltaTime;
                t += dt;
                float p = Mathf.Clamp01(t / reverseDuration);
                float s = reverseDistance * p * p;
                float ang = reverseAngle * p * p;

                rear.position += rear.right * (s - prevS);   // 沿自身 right（车尾 +X）位移本帧增量
                prevS = s;
                rear.rotation = Quaternion.Euler(0f, -ang, 0f);   // 直接赋值负角度

                // 从 reverseSquashDelay 起匀减速缩放（覆盖剩余倒车 + 等待）
                if (scale != null)
                    SetScaleX(scale, 1f - (1f - reverseSquashScale) * EaseOutQuad((t - reverseSquashDelay) / squashTotal));
                yield return null;
            }

            // ===== 倒车等待：匀减速缩放继续，到等待结束缩至 reverseSquashScale =====
            while (t < total)
            {
                float dt = Time.deltaTime;
                t += dt;
                if (scale != null)
                    SetScaleX(scale, 1f - (1f - reverseSquashScale) * EaseOutQuad((t - reverseSquashDelay) / squashTotal));
                yield return null;
            }

            // 倒车等待结束，开始出车
            //if (AudioManager.Instance != null)
            //    AudioManager.Instance.Play("CarOut");
            if (GameManager.Instance != null)
                GameManager.Instance.TriggerVibrate(1);

            // ===== 出车转正：前轴驱动（缩放轴 → 自转轴 → 小车 链条整体移到前轴下） =====
            // 位移级换轴：先把新轴（前轴）提到与旧位移轴（后轴）同父级并重置 scale，再把直接挂在后轴下的链条节点
            // （缩放轴，或无缩放轴时的自转轴，或都无时的小车）整体移到新轴下，最后把旧轴还给小车。小车全程不脱离
            // 缩放轴/自转轴，自身缩放不被烘、也不重置（避免缩放 pivot 与车体 pivot 不一致导致瞬移）。
            front.SetParent(cartParent, true);   // 前轴脱离小车 → 挂到与旧位移轴（后轴）同父级
            front.localScale = Vector3.one;      // 此刻前轴与系统无牵连，重置 scale 干净
            Transform chainChild = scale != null ? scale : (roll != null ? roll : transform);
            chainChild.SetParent(front, true);   // 把链条最上层节点（缩放轴/自转轴/小车）移到前轴下
            rear.SetParent(transform, true);     // 旧轴（后轴）还给小车
            rear.localScale = Vector3.one;       // 后轴归位后重置 scale（空轴，重置不引起瞬移）
        }

        /// <summary>从 SpawnPool 生成 Trail 并挂到车上的拖尾父节点下；未配置池 / 节点 / tag 时静默跳过。</summary>
        private void SpawnTrail(ContainerItem container)
        {
            if (_trail != null || container == null || container.trailParent == null)
                return;

            var pool = GameManager.Instance != null ? GameManager.Instance.spawnPool : null;
            if (pool == null)
                return;

            _trail = pool.Spawn("Trail", container.trailParent);
            if (_trail == null)
                return;

            // Spawn 用 parent 赋值（保持世界位姿），此处对齐到拖尾节点自身
            _trail.transform.localPosition = Vector3.zero;
            _trail.transform.localRotation = Quaternion.identity;
            _trail.transform.localScale = Vector3.one;
        }

        /// <summary>
        /// 播放出车起步音效：上次播放距今不超过 CarLeaveLoopWindow 秒时，按 CarLeave → CarLeave2 → CarLeave3 依次轮换
        /// （到末尾回到开头）；超过该窗口则从 CarLeave 重新开始。状态静态，跨所有小车共享。
        /// </summary>
        private static void PlayCarLeaveSfx()
        {
            var audio = AudioManager.Instance;
            if (audio == null)
                return;

            float now = Time.time;
            _carLeaveIndex = now - _carLeaveLastTime > CarLeaveLoopWindow
                ? 0
                : (_carLeaveIndex + 1) % CarLeaveTags.Length;
            _carLeaveLastTime = now;

            audio.Play(CarLeaveTags[_carLeaveIndex]);
        }

        /// <summary>倒车起点就地生成 Confetti，3 秒后由 SpawnPool 自动回收。
        /// 不挂到小车下：避免继承倒车挤压缩放，也让彩带留在原地作为爆发点。</summary>
        //private void SpawnConfetti()
        //{
        //    var pool = GameManager.Instance != null ? GameManager.Instance.spawnPool : null;
        //    if (pool == null)
        //        return;

        //    var fx = pool.SpawnDuration("Confetti", 3f);
        //    if (fx != null)
        //        fx.transform.position = transform.position;
        //}

        /// <summary>
        /// 绳连后车的甩头参数若为 0（`ropeExitMaxAngle` / `ropeExitAngularAcceleration`），出车会**没有任何角度变化**、
        /// 直接水平移出——这是配置漏了，静默下去很难查。这里打一条只出现一次的警告点名。
        /// </summary>
        private void WarnIfRopeExitHasNoSwing()
        {
            if (_warnedRopeExitNoSwing)
                return;
            if (ropeExitMaxAngle > 0f && ropeExitAngularAcceleration > 0f)
                return;

            _warnedRopeExitNoSwing = true;
            Debug.LogWarning("[ContainerExitDriver] 绳连后车的甩头参数为 0（Rope Exit Max Angle = " + ropeExitMaxAngle +
                "，Rope Exit Angular Acceleration = " + ropeExitAngularAcceleration +
                "），本次出车不会有角度变化、只会水平移出。请在该车预制体的 ContainerExitDriver 上配置 Rope Rear Exit 那一组参数。", this);
        }

        /// <summary>把拖尾归还对象池。可重复调用：已回收或未生成时为空操作。</summary>
        private void DespawnTrail()
        {
            if (_trail == null)
                return;

            var pool = GameManager.Instance != null ? GameManager.Instance.spawnPool : null;
            if (pool != null)
                pool.Despawn(_trail, true);

            _trail = null;
        }

        /// <summary>匀减速（ease-out quad，p∈[0,1] → [0,1]，起始最快、末速归零）。</summary>
        private static float EaseOutQuad(float p)
        {
            p = Mathf.Clamp01(p);
            return 1f - (1f - p) * (1f - p);
        }

        /// <summary>先匀加速后匀减速（ease-in-out quad，p∈[0,1] → [0,1]，中点为 0.5，两端速度归零）。</summary>
        private static float EaseInOutQuad(float p)
        {
            p = Mathf.Clamp01(p);
            return p < 0.5f ? 2f * p * p : 1f - 2f * (1f - p) * (1f - p);
        }

        /// <summary>只改 Transform 的 localScale.x（保留 y/z）。</summary>
        private static void SetScaleX(Transform t, float x)
        {
            Vector3 ls = t.localScale;
            ls.x = x;
            t.localScale = ls;
        }

        private void OnDestroy()
        {
            // 中途销毁兜底：游离的轴（父物体不是本物体）一并销毁，避免残留空物体
            var container = GetComponent<ContainerItem>();
            if (container == null)
                return;
            if (container.frontAxle != null && container.frontAxle.parent != transform)
                Destroy(container.frontAxle.gameObject);
            if (container.rearAxle != null && container.rearAxle.parent != transform)
                Destroy(container.rearAxle.gameObject);
            if (container.ropeExitAxle != null && container.ropeExitAxle != container.frontAxle
                && container.ropeExitAxle.parent != transform)
                Destroy(container.ropeExitAxle.gameObject);   // 绳连后车变体中途销毁时可能正挂着这根轴
            if (container.reverseScaleAxle != null && container.reverseScaleAxle.parent != transform)
                Destroy(container.reverseScaleAxle.gameObject);
            if (container.rollAxle != null && container.rollAxle.parent != transform)
                Destroy(container.rollAxle.gameObject);
            if (container.elasticScaleAxle != null && container.elasticScaleAxle.parent != transform)
                Destroy(container.elasticScaleAxle.gameObject);
        }
    }
}
