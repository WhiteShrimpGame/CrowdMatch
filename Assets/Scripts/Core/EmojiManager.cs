using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 表情包管理器：在场景里建好并引用到 <see cref="GameManager.emojiManager"/>（或用便捷入口 <see cref="Instance"/>）。
    /// 表情经 SpawnPool 生成、到时自动回池；follow 模式挂成传入 Transform 的子物体并跟随，
    /// 非 follow 模式挂在池根下、只取它的世界位置。
    /// 每次播放都会登记（表情 / 锚点 / tag），便于按锚点定向回收（例如像素被匹配上车时收掉犯困表情）。
    /// </summary>
    public class EmojiManager : MonoBehaviour
    {
        /// <summary>按 tag 配置的播放加速。</summary>
        [System.Serializable]
        public class EmojiSpeed
        {
            [Tooltip("表情在 SpawnPool 配置里的 tag")]
            public string tag;

            [Tooltip("播放加速倍率（1 = 原速，>1 更快，<1 更慢）")]
            public float speed = 1f;
        }

        /// <summary>按 tag 配置的播放时长。</summary>
        [System.Serializable]
        public class EmojiDuration
        {
            [Tooltip("表情在 SpawnPool 配置里的 tag")]
            public string tag;

            [Tooltip("该表情从生成到自动回池的时长（秒）")]
            public float duration = 1.5f;
        }

        [Tooltip("单个表情从生成到自动回池的时长（秒）；在 durations 里配置过的 tag 用各自的时长")]
        public float emojiDuration = 1.5f;

        [Tooltip("表情的世界缩放（预制体建议按 scale=1 制作，尺寸统一由这里控制；父节点有缩放时按世界缩放换算）")]
        public float emojiScale = 0.5f;

        [Header("每 tag 配置")]
        [Tooltip("按 tag 配置播放加速（粒子的 simulationSpeed / Animator.speed）；未配置的 tag 保持预制体原速")]
        public List<EmojiSpeed> speeds = new List<EmojiSpeed>();

        [Tooltip("按 tag 配置播放时长；未出现在此列表里的 tag 用 Emoji Duration")]
        public List<EmojiDuration> durations = new List<EmojiDuration>();

        [Header("上车开心表情")]
        [Tooltip("车上最后一个像素准备上车时触发开心表情的概率（0 = 不触发，1 = 必触发）")]
        [Range(0f, 1f)] public float happyChance = 0.8f;

        [Tooltip("开心表情在 SpawnPool 配置里的 tag")]
        public string happyTag = "EmojiHappy";

        [Header("犯困表情")]
        [Tooltip("犯困表情在 SpawnPool 配置里的 tag（由传送带宿主按间隔检测播放）")]
        public string sleepTag = "EmojiSleep";

        [Header("点击受阻生气表情")]
        [Tooltip("点击无法移出的像素时触发生气表情的概率（0 = 不触发，1 = 必触发）")]
        [Range(0f, 1f)] public float angryChance = 0.5f;

        [Tooltip("生气表情的全局冷却（秒）：冷却期内不再检查、也不再播放")]
        public float angryCooldown = 3f;

        [Tooltip("生气表情在 SpawnPool 配置里的 tag")]
        public string angryTag = "EmojiAngry";

        [Tooltip("被后人插队时触发生气表情的概率系数：概率 = 该系数 × 被插队的像素数（≥ 100% 必然触发）")]
        [Range(0f, 1f)] public float angryJumpChancePerPixel = 0.1f;

        [Tooltip("插队生气表情的全局冷却（秒）：冷却内不会再次触发（与点击阻挡的 CD 相互独立）")]
        public float angryJumpCooldown = 3f;

        [Tooltip("排队太久触发生气表情的概率系数：概率 = 该系数 × 排队超时人数（≥ 100% 必然触发）")]
        [Range(0f, 1f)] public float queueAngryChancePerPixel = 0.1f;

        [Tooltip("在缓冲区排队超过该秒数就算「排队太久」（由传送带宿主按间隔检测，无 CD）")]
        public float queueAngryWaitSeconds = 5f;

        /// <summary>便捷入口：从 GameManager 取管理器；未初始化或已销毁时返回 null。</summary>
        public static EmojiManager Instance
        {
            get
            {
                var gm = GameManager.Instance;
                if (gm == null)
                    return null;
                var manager = gm.emojiManager;
                return manager != null ? manager : null;   // Unity 的 == 会把已销毁的一并当 null
            }
        }

        /// <summary>一次表情播放的登记，供按锚点 / tag 定向回收。</summary>
        private struct Booking
        {
            public int id;
            public GameObject emoji;
            public Transform anchor;
            public string tag;
        }

        private readonly List<Booking> _bookings = new List<Booking>();
        private int _nextBookingId;

        /// <summary>生气表情下一次可播放的时刻（全局 CD）。</summary>
        private float _angryReadyTime;

        /// <summary>插队生气表情下一次可触发的时刻（全局 CD）。</summary>
        private float _angryJumpReadyTime;

        /// <summary>非 World Space 的 Canvas 只警告一次，避免每次播放都刷屏。</summary>
        private bool _warnedNonWorldCanvas;

        /// <summary>
        /// 在 anchor 处生成一个 tag 表情，该 tag 的时长（默认 <see cref="emojiDuration"/>）后自动回池；未配置对象池 / tag 不存在时返回 null。
        /// follow = true：挂成 anchor 的子物体并归位到它自身（localPosition 归零），随 anchor 移动；
        ///   此时 anchor 被销毁会把表情一起带走（池里只留下一条待 GC 清理的登记，不会报错）。
        /// follow = false：挂在池根下、只取 anchor 当前的世界位置，不跟随。
        /// 两种模式都把表情的**世界缩放**设成 emojiScale（父节点有缩放时不能直接写 localScale），朝向仍由预制体决定。
        /// 预制体里若带 World Space 的 Canvas，会自动挂上 <see cref="EmojiBillboard"/> 让它始终正对镜头。
        /// 不复用 SpawnPool.SpawnDuration：它内部的延时回调不带空守卫，对象若已销毁会在 Despawn 里解引用抛异常。
        /// </summary>
        public GameObject PlayEmoji(Transform anchor, string tag, bool follow = false)
        {
            if (anchor == null || string.IsNullOrEmpty(tag))
                return null;

            var pool = GetPool();
            if (pool == null)
                return null;

            var emoji = pool.Spawn(tag, follow ? anchor : null);   // 不跟随 → 挂到池根下
            if (emoji == null)
                return null;

            if (follow)
                emoji.transform.localPosition = Vector3.zero;   // 归位到节点自身（也让换模式复用时不会带着上次的局部偏移）
            else
                emoji.transform.position = anchor.position;

            ApplyWorldScale(emoji.transform);
            ApplyPlaySpeed(emoji, tag);
            SetupBillboard(emoji);

            int bookingId = _nextBookingId++;
            _bookings.Add(new Booking { id = bookingId, emoji = emoji, anchor = anchor, tag = tag });

            DOVirtual.DelayedCall(Mathf.Max(0f, ResolveDuration(tag)), () =>
            {
                if (this == null)
                    return;   // 管理器已销毁（场景卸载）：池也随之没了，既不用回收也不用清理记录
                RemoveBooking(bookingId);
                if (emoji != null)
                    pool.Despawn(emoji, true);   // 已被提前回收 / 随锚点销毁时跳过
            });
            return emoji;
        }

        /// <summary>在像素的表情节点上播犯困表情（跟随模式）。</summary>
        public void PlaySleepEmoji(PixelItem pixel)
        {
            if (pixel == null || pixel.emojiNode == null)
                return;
            PlayEmoji(pixel.emojiNode, sleepTag, follow: true);
        }

        /// <summary>
        /// 点击无法移出的像素时调用：按 angryChance 概率在**被点的那一个像素**上播生气表情（跟随模式）——点谁谁生气。
        /// 全局 CD：距上次播放不足 angryCooldown 秒时直接返回，连概率都不掷。
        /// </summary>
        public void TryPlayAngryEmoji(PixelItem clicked)
        {
            if (angryChance <= 0f)
                return;
            if (Time.time < _angryReadyTime)
                return;   // CD 中：不检查也不播
            if (clicked == null || clicked.emojiNode == null)
                return;   // 被点的像素没配表情节点：没法显示

            if (Random.value > angryChance)
                return;

            _angryReadyTime = Time.time + Mathf.Max(0f, angryCooldown);
            PlayEmoji(clicked.emojiNode, angryTag, follow: true);
        }

        /// <summary>
        /// 插队生气表情：某像素上带时，若缓冲区里还有比它更早被点击、且颜色不同的像素在排队（后点的先上了带），
        /// 则从这些「被插队者」中随机一个播放生气表情（与点击阻挡复用同一个 emoji）。
        /// 概率 = angryJumpChancePerPixel × 被插队人数（≥ 100% 必然触发）；有独立的全局 CD，CD 内直接返回。
        /// </summary>
        public void TryPlayAngryEmojiForJumped(List<PixelItem> waiting, PixelItem boarding)
        {
            if (boarding == null || angryJumpChancePerPixel <= 0f || boarding.clickSeq <= 0)
                return;
            if (Time.time < _angryJumpReadyTime)
                return;   // CD 中：不检查也不播

            var candidates = new List<PixelItem>();
            if (waiting != null)
            {
                for (int i = 0; i < waiting.Count; i++)
                {
                    var pixel = waiting[i];
                    if (pixel == null || pixel.emojiNode == null)
                        continue;   // 没表情节点就没法显示
                    if (pixel.clickSeq <= 0 || pixel.clickSeq >= boarding.clickSeq)
                        continue;   // 不比它更早被点击
                    if (pixel.colorId == boarding.colorId)
                        continue;   // 同色不算被插队
                    candidates.Add(pixel);
                }
            }

            if (candidates.Count == 0)
                return;

            float chance = angryJumpChancePerPixel * candidates.Count;
            if (chance < 1f && Random.value > chance)
                return;

            _angryJumpReadyTime = Time.time + Mathf.Max(0f, angryJumpCooldown);
            PlayEmoji(candidates[Random.Range(0, candidates.Count)].emojiNode, angryTag, follow: true);
        }

        /// <summary>
        /// 排队生气：缓冲区里排队超过 queueAngryWaitSeconds 的像素都算候选，按 概率系数 × 人数 决定是否触发，
        /// 命中则在这些候选里随机一个上播生气表情（与插队 / 点击阻挡复用同一个 emoji）。
        /// 判定上独立于插队：没有 CD，节流完全由调用方的检测间隔负责。
        /// </summary>
        public void TryPlayAngryEmojiForQueue(List<PixelItem> waiting)
        {
            if (waiting == null || queueAngryChancePerPixel <= 0f)
                return;

            var candidates = new List<PixelItem>();
            for (int i = 0; i < waiting.Count; i++)
            {
                var pixel = waiting[i];
                if (pixel == null || pixel.emojiNode == null)
                    continue;   // 没表情节点就没法显示
                if (float.IsNaN(pixel.bufferedAt))
                    continue;   // 没有入队时刻（异常兜底）
                if (Time.time - pixel.bufferedAt < queueAngryWaitSeconds)
                    continue;   // 还没排够久
                candidates.Add(pixel);
            }

            if (candidates.Count == 0)
                return;

            float chance = queueAngryChancePerPixel * candidates.Count;
            if (chance < 1f && Random.value > chance)
                return;

            PlayEmoji(candidates[Random.Range(0, candidates.Count)].emojiNode, angryTag, follow: true);
        }

        /// <summary>收掉该像素的犯困表情（像素被匹配上车 / 离开传送带时调用）。</summary>
        public void RemoveSleepEmoji(PixelItem pixel)
        {
            if (pixel == null || pixel.emojiNode == null)
                return;
            RemoveEmojis(pixel.emojiNode, sleepTag);
        }

        /// <summary>收掉该像素的生气表情（像素被点击移出网格时调用）。</summary>
        public void RemoveAngryEmoji(PixelItem pixel)
        {
            if (pixel == null || pixel.emojiNode == null)
                return;
            RemoveEmojis(pixel.emojiNode, angryTag);
        }

        /// <summary>收掉该车所有乘客的犯困表情（车已匹配到最后一个像素、乘客即将出发）。返回回收数量。</summary>
        public int ClearSleepEmojis(ContainerItem car)
        {
            if (car == null)
                return 0;

            var passengers = new List<PixelItem>();
            car.CollectPassengers(passengers);

            int removed = 0;
            for (int i = 0; i < passengers.Count; i++)
            {
                var node = passengers[i].emojiNode;
                if (node != null)
                    removed += RemoveEmojis(node, sleepTag);
            }
            return removed;
        }

        /// <summary>
        /// 按锚点定向回收表情（跟随时表情就是锚点的子物体，非跟随也按登记能找到）；tag 留空则不限 tag。
        /// 返回回收数量。
        /// </summary>
        public int RemoveEmojis(Transform anchor, string tag)
        {
            if (anchor == null)
                return 0;
            return RemoveBookings(anchor, tag);
        }

        /// <summary>
        /// 收掉场上**所有**生气表情（复活时调用）。返回回收数量。
        ///
        /// 为什么复活要清：复活会把缓冲区里的像素**直接匹配上车**（`GameController.Revive` → `MatchPixelsToCars`），
        /// 而跟随模式下表情是像素的子物体——不收就会跟着像素一起进车，变成乘客头上顶着生气脸。
        /// 同时网格上残留的「点击受阻」生气脸在复活之后也没有意义了。
        /// </summary>
        public int ClearAngryEmojis()
        {
            return RemoveBookings(null, angryTag);
        }

        /// <summary>回收登记中匹配的表情：anchor 为空 = 不限锚点，tag 为空 = 不限 tag（两个都空即清空全部）。</summary>
        private int RemoveBookings(Transform anchor, string tag)
        {
            var pool = GetPool();

            int removed = 0;
            for (int i = _bookings.Count - 1; i >= 0; i--)
            {
                var booking = _bookings[i];

                // 先判 emoji：已随锚点销毁的登记直接丢掉（也避免两个已销毁对象被 Unity 的 == 判为相等而误配锚点）
                if (booking.emoji == null)
                {
                    _bookings.RemoveAt(i);
                    continue;
                }
                if (anchor != null && booking.anchor != anchor)
                    continue;
                if (!string.IsNullOrEmpty(tag) && booking.tag != tag)
                    continue;

                _bookings.RemoveAt(i);
                if (pool != null)
                    pool.Despawn(booking.emoji, true);
                removed++;
            }
            return removed;
        }

        /// <summary>
        /// 车上最后一个像素准备上车时调用：按 happyChance 概率触发，命中则在车上随机一名乘客的表情节点上播放。
        /// 只在**前排**（gridZ == 0，也就是即将出车的那辆）触发——后排匹配完只是等补位，不播。
        /// 无视上车状态——正在跳跃上车的、以及当前这个尚未挂上座位的 pixel 都算候选。
        /// </summary>
        public void TryPlayHappyEmoji(ContainerItem car, PixelItem boardingPixel)
        {
            if (car == null || happyChance <= 0f)
                return;
            if (car.gridZ != 0)
                return;   // 不在前排

            var pixel = PickRandomPassenger(car, boardingPixel);
            if (pixel == null)
                return;   // 车上没有任何配了表情节点的像素：不播

            if (Random.value > happyChance)
                return;

            PlayEmoji(pixel.emojiNode, happyTag, follow: true);
        }

        private static SpawnPool GetPool()
        {
            var gm = GameManager.Instance;
            return gm != null ? gm.spawnPool : null;
        }

        /// <summary>把表情的世界缩放设为 emojiScale：按父节点 lossyScale 逐轴换算成局部缩放（父缩放为 0 的轴直接取设定值）。</summary>
        private void ApplyWorldScale(Transform emoji)
        {
            Transform parent = emoji.parent;
            Vector3 parentScale = parent != null ? parent.lossyScale : Vector3.one;
            emoji.localScale = new Vector3(
                Divide(emojiScale, parentScale.x),
                Divide(emojiScale, parentScale.y),
                Divide(emojiScale, parentScale.z));
        }

        private static float Divide(float value, float divisor)
        {
            return Mathf.Abs(divisor) > 0.0001f ? value / divisor : value;
        }

        /// <summary>按 tag 应用播放加速：粒子 simulationSpeed + Animator.speed。未配置该 tag 时保持预制体原速。</summary>
        private void ApplyPlaySpeed(GameObject emoji, string tag)
        {
            float speed;
            if (!TryGetPlaySpeed(tag, out speed))
                return;

            var particles = emoji.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particles.Length; i++)
            {
                var main = particles[i].main;
                main.simulationSpeed = speed;
            }

            var animators = emoji.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
                animators[i].speed = speed;
        }

        /// <summary>查该 tag 配置的播放加速；未配置返回 false（调用方保持原速）。</summary>
        private bool TryGetPlaySpeed(string tag, out float speed)
        {
            speed = 1f;
            if (speeds == null || string.IsNullOrEmpty(tag))
                return false;

            for (int i = 0; i < speeds.Count; i++)
            {
                var entry = speeds[i];
                if (entry != null && entry.tag == tag)
                {
                    speed = entry.speed;
                    return true;
                }
            }
            return false;
        }

        /// <summary>查该 tag 配置的播放时长；未配置则用全局 <see cref="emojiDuration"/>。</summary>
        private float ResolveDuration(string tag)
        {
            if (durations != null && !string.IsNullOrEmpty(tag))
            {
                for (int i = 0; i < durations.Count; i++)
                {
                    var entry = durations[i];
                    if (entry != null && entry.tag == tag)
                        return entry.duration;
                }
            }
            return emojiDuration;
        }

        /// <summary>
        /// 支持 Canvas 方式的表情：把表情内部的 **World Space** Canvas 挂上 <see cref="EmojiBillboard"/>，让 UI 始终正对镜头。
        /// 已经在池里复用的对象不会再重复挂（挂了就跳过）。
        ///
        /// 非 World Space 的 Canvas（Screen Space Overlay / Camera）在世界层级里不会跟随锚点、也做不了 billboard，
        /// 这属于预制体配置问题，打一条 warning 指出改法（只打一次，避免刷屏）。
        /// </summary>
        private void SetupBillboard(GameObject emoji)
        {
            var canvases = emoji.GetComponentsInChildren<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                var canvas = canvases[i];
                if (canvas == null)
                    continue;

                if (canvas.renderMode != RenderMode.WorldSpace)
                {
                    if (!_warnedNonWorldCanvas)
                    {
                        _warnedNonWorldCanvas = true;
                        Debug.LogWarning("[EmojiManager] 表情 " + emoji.name + " 里的 Canvas 渲染模式是 " + canvas.renderMode +
                            "，不是 World Space：它不会跟随锚点、也无法做 billboard。请在预制体上把 Canvas 的 Render Mode 改成 World Space。", canvas);
                    }
                    continue;
                }

                if (canvas.GetComponent<EmojiBillboard>() == null)
                    canvas.gameObject.AddComponent<EmojiBillboard>();
            }
        }

        /// <summary>按登记号移除一条播放记录（用登记号而非对象引用：两个已销毁对象会被 Unity 的 == 判为相等）。</summary>
        private void RemoveBooking(int bookingId)
        {
            for (int i = _bookings.Count - 1; i >= 0; i--)
            {
                if (_bookings[i].id == bookingId)
                {
                    _bookings.RemoveAt(i);
                    return;
                }
            }
        }

        /// <summary>
        /// 在车上随机挑一名配了表情节点的乘客：座位上的全部乘客（含正在跳跃上车的），
        /// 外加当前这个可能还没挂上座位（或走无座位回退路径）的 boardingPixel。
        /// </summary>
        private static PixelItem PickRandomPassenger(ContainerItem car, PixelItem boardingPixel)
        {
            var passengers = new List<PixelItem>();
            car.CollectPassengers(passengers);

            if (boardingPixel != null && !passengers.Contains(boardingPixel))
                passengers.Add(boardingPixel);

            return PickRandomWithEmojiNode(passengers);
        }

        /// <summary>从一组像素里随机挑一个配了表情节点的；没有则返回 null。</summary>
        private static PixelItem PickRandomWithEmojiNode(List<PixelItem> pixels)
        {
            if (pixels == null)
                return null;

            var candidates = new List<PixelItem>(pixels.Count);
            for (int i = 0; i < pixels.Count; i++)
            {
                if (pixels[i] != null && pixels[i].emojiNode != null)
                    candidates.Add(pixels[i]);
            }

            return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;
        }
    }
}
