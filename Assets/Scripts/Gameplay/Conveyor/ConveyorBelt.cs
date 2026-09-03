using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 抽象传送带：让物体沿 ArcPathController 定义的轨迹等距循环运动。
    /// 进入/离开均为接口钩子，业务端决定何时进入、何时离开以及离开后的处理。
    ///
    /// 本工程版改造为「槽位承载物（carrier）」模型：传送带每帧移动每个槽位对应的 carrier（belt 子物体，scale=1），
    /// 乘员（IConveyorItem）reparent 到 carrier 上（localPosition 收敛到 0），由 carrier 带着循环，从而实现无瞬移进入。
    /// 入口收集：每个槽位世界 X 越过入口关口（entryGate）时触发 SlotPassedEntry(i)，由宿主在该槽位直接收一个乘员。
    ///
    /// Abstract conveyor belt: moves items in an evenly-spaced loop along an
    /// ArcPathController-defined path. Enter/leave are interface hooks — the host
    /// decides when an item enters, when it should leave, and what happens after.
    ///
    /// This project's adaptation uses a per-slot "carrier" model: the belt moves each
    /// slot's carrier (child, scale=1) every frame; items reparent onto a carrier
    /// (localPosition converges to 0) and are carried around — enabling teleport-free entry.
    /// </summary>
    public class ConveyorBelt : MonoBehaviour
    {
        [Header("Path / 轨迹")]
        [Tooltip("多段圆弧轨迹控制器 / Multi-segment arc path controller")]
        public ArcPathController path;

        [Header("Motion / 运动")]
        [Tooltip("完整循环一周所需秒数 / Seconds per full loop")]
        public float cycleTime = 6f;
        [Tooltip("运动速度倍率 / Speed multiplier")]
        public float speed = 1f;

        [Header("Slots / 槽位")]
        [Tooltip("等距槽位数量（= 传送带总容量）/ Number of evenly-spaced slots (= total capacity)")]
        public int slotCount = 12;

        /// <summary>槽位数组，null 表示空槽。/ Slot array, null = empty.</summary>
        private IConveyorItem[] slots;

        /// <summary>每个槽位的承载物（belt 子物体，scale=1）。传送带每帧写其世界位置，乘员作为其子物体被带着走。</summary>
        private Transform[] carriers;

        /// <summary>归一化循环偏移 [0,1)。内部驱动整个传送带循环。/ Normalized loop offset [0,1).</summary>
        private float offset;

        [Header("Entry collection / 入口收集")]
        [Tooltip("收集关口（只用其世界 X 坐标）。近侧槽位世界 X 从大于该值跨到小于等于该值时，触发一次收集。")]
        public Transform entryGate;

        [Tooltip("某槽位过关口时触发的收集回调（参数 = 槽位索引），由宿主注入，负责取球并上车。")]
        public Action<int> SlotPassedEntry;

        /// <summary>每个槽位上一帧的世界 X，用于检测「过关口」跨越。/ Per-slot previous-frame world X for entry-gate crossing.</summary>
        private float[] _prevSlotX;

        /// <summary>
        /// 离开判定钩子：返回 true 表示该物体应当离开传送带。
        /// 由业务端注入（例如「与目标位置满足某种关系」）。
        /// Leave-check hook: return true if the item should leave the belt.
        /// </summary>
        public Func<IConveyorItem, bool> ShouldLeave;

        /// <summary>
        /// 离开回调钩子：物体离开传送带时触发，由业务端注入处理逻辑。
        /// Leave callback: invoked when an item leaves the belt. Injected by the host.
        /// </summary>
        public Action<IConveyorItem> OnLeave;

        [Header("Catch-up / 追赶")]
        [Tooltip("是否启用追赶：每周期把「队首直接相连占用块」之外的所有槽位整体向前平移一格，把空隙挤到队尾。/ Enable catch-up: each cycle shifts every slot outside the leader's contiguous occupied block one step forward, pushing gaps to the tail.")]
        public bool catchUpEnabled = true;

        [Tooltip("追赶周期秒数（= 平移一格的时长；默认对齐槽位节拍 cycleTime/slotCount）。/ Catch-up period in seconds (also the duration of one shift step; defaults to the slot beat cycleTime/slotCount).")]
        public float catchUpInterval = 0.5f;

        /// <summary>队首所在槽位（-1 = 无队首 / 空传送带）。队首不参与追赶，其余元素向其靠拢。</summary>
        private int _leaderSlot = -1;

        /// <summary>追赶周期计时器。</summary>
        private float _catchUpTimer = 0f;

        /// <summary>每个槽位的相位偏移（相对 offset，默认 i/N）。追赶时平滑平移相位，乘员始终 localPosition=0 就位。</summary>
        private float[] _slotPhase;

        /// <summary>追赶周期内被「瞬移」到队首、暂时不可用的空槽位（-1 = 无）。经过入口时跳过收集。</summary>
        private int _unavailableSlot = -1;

        /// <summary>本周期内正在平滑平移相位的槽位（相位从 startPhase 匀速到 targetPhase）。</summary>
        private readonly List<PhaseShift> _phaseShifts = new List<PhaseShift>();

        private class PhaseShift
        {
            public int slot;
            public float startPhase;
            public float targetPhase;
            public float t;
        }

        private bool _initialized;

        /// <summary>
        /// 初始化槽位数组与承载物。在 Start 前调用（或由 Start 自动调用）。
        /// Initializes the slot array and carriers. Called automatically in Start.
        /// </summary>
        public void Initialize()
        {
            slots = new IConveyorItem[slotCount];
            CreateCarriers();
            if (path != null)
            {
                path.InitializePaths();
            }
            _slotPhase = new float[slotCount];
            for (int i = 0; i < slotCount; i++)
            {
                _slotPhase[i] = (float)i / slotCount;
            }
            _leaderSlot = -1;
            _catchUpTimer = 0f;
            _unavailableSlot = -1;
            _phaseShifts.Clear();
            _initialized = true;
        }

        /// <summary>创建每个槽位的承载物（清理旧的，避免重复 Initialize 时堆积）。</summary>
        private void CreateCarriers()
        {
            if (carriers != null)
            {
                for (int i = 0; i < carriers.Length; i++)
                {
                    if (carriers[i] != null)
                        Destroy(carriers[i].gameObject);
                }
            }

            carriers = new Transform[slotCount];
            for (int i = 0; i < slotCount; i++)
            {
                var go = new GameObject("Carrier_" + i);
                go.transform.SetParent(transform, false);
                go.transform.localScale = Vector3.one;
                carriers[i] = go.transform;
            }
        }

        private void Start()
        {
            if (!_initialized)
            {
                Initialize();
            }
        }

        private void Update()
        {
            if (!_initialized || path == null)
            {
                return;
            }

            Advance();
            ApplyPositions();
            AdvanceCatchUp();
            CheckLeave();
        }

        /// <summary>推进循环偏移。/ Advances the loop offset.</summary>
        private void Advance()
        {
            offset += Time.deltaTime * speed / cycleTime;
            offset %= 1f;
        }

        /// <summary>
        /// 把每个槽位的 carrier 定位到轨迹对应位置（等距循环）。
        /// 空槽的 carrier 也一并定位，随时可让新乘员上车。
        /// Positions each slot's carrier on the path (evenly-spaced loop);
        /// empty carriers are positioned too, ready for new occupants.
        /// </summary>
        private void ApplyPositions()
        {
            float totalLength = path.GetTotalPathLength();

            // 关口 X（未指定则用传送带自身 X）
            float gateX = entryGate != null ? entryGate.position.x : transform.position.x;
            bool firstTrack = _prevSlotX == null || _prevSlotX.Length != slots.Length;
            if (firstTrack)
            {
                _prevSlotX = new float[slots.Length];
            }

            for (int i = 0; i < slots.Length; i++)
            {
                if (carriers[i] == null)
                {
                    continue;
                }

                float samplePhase = (offset + _slotPhase[i]) % 1f;
                carriers[i].position = path.GetGlobalPosition(samplePhase * totalLength);
                carriers[i].rotation = Quaternion.Euler(path.GetGlobalEulerAngles(samplePhase * totalLength));

                // 过关口检测：世界 X 从 > gateX 跨到 <= gateX（即近侧沿 -X 方向运动）时触发一次收集
                float currX = carriers[i].position.x;
                if (!firstTrack && _prevSlotX[i] > gateX && currX <= gateX)
                {
                    // 追赶周期内被瞬移到队首的空槽位不可用，跳过收集
                    if (i != _unavailableSlot)
                    {
                        SlotPassedEntry?.Invoke(i);
                    }
                }
                _prevSlotX[i] = currX;
            }
        }

        /// <summary>
        /// 每帧检查每个槽位是否满足离开条件，满足则解绑（从 carrier 下取出，保持世界位置）并回调。
        /// Checks every slot against ShouldLeave each frame; on true, unparents the item and invokes OnLeave.
        /// </summary>
        private void CheckLeave()
        {
            if (ShouldLeave == null)
            {
                return;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                var item = slots[i];
                if (item == null)
                {
                    continue;
                }

                // 防御：乘员被异常销毁则清槽
                if (item.Transform == null)
                {
                    VacateSlot(i);
                    continue;
                }

                if (ShouldLeave(item))
                {
                    item.Transform.SetParent(null, true);   // 解绑 carrier（保持世界位置），交给宿主吸收
                    VacateSlot(i);                          // 清槽 + 队首更替
                    OnLeave?.Invoke(item);
                }
            }
        }

        /// <summary>
        /// 尝试让物体进入指定槽位（reparent 到 carrier，保持世界位置 → 不瞬移）。槽位已占用则返回 false。
        /// Tries to place an item into a slot (reparents onto the carrier, keeping world position). Returns false if occupied.
        /// </summary>
        public bool TryEnter(IConveyorItem item, int slotIndex)
        {
            if (!_initialized || item == null || item.Transform == null)
            {
                return false;
            }
            if (slotIndex < 0 || slotIndex >= slots.Length)
            {
                return false;
            }
            if (slots[slotIndex] != null)
            {
                return false;
            }

            slots[slotIndex] = item;
            item.Transform.SetParent(carriers[slotIndex], true);

            // 空传送带时第一个进入的元素成为队首
            if (_leaderSlot < 0)
            {
                _leaderSlot = slotIndex;
            }
            return true;
        }

        /// <summary>获取指定槽位的物体（可能为 null）。/ Returns the item at a slot (may be null).</summary>
        public IConveyorItem GetItem(int slotIndex)
        {
            if (!_initialized || slotIndex < 0 || slotIndex >= slots.Length)
            {
                return null;
            }
            return slots[slotIndex];
        }

        /// <summary>清空指定槽位（不触发 OnLeave）。/ Clears a slot without invoking OnLeave.</summary>
        public void ClearSlot(int slotIndex)
        {
            if (!_initialized || slotIndex < 0 || slotIndex >= slots.Length)
            {
                return;
            }

            var item = slots[slotIndex];
            if (item != null && item.Transform != null)
            {
                item.Transform.SetParent(null, true);
            }
            VacateSlot(slotIndex);
        }

        /// <summary>已占用槽位数（供 UI）。/ Number of occupied slots.</summary>
        public int OccupiedCount
        {
            get
            {
                if (slots == null)
                {
                    return 0;
                }
                int n = 0;
                for (int i = 0; i < slots.Length; i++)
                {
                    if (slots[i] != null)
                    {
                        n++;
                    }
                }
                return n;
            }
        }

        /// <summary>某槽位当前的世界坐标（按当前 offset 采样，供背压判定/调试）。</summary>
        public Vector3 GetSlotWorldPosition(int slotIndex)
        {
            if (!_initialized || path == null)
            {
                return transform.position;
            }
            if (slotIndex < 0 || slotIndex >= slots.Length)
            {
                return transform.position;
            }

            float samplePhase = (offset + _slotPhase[slotIndex]) % 1f;
            return path.GetGlobalPosition(samplePhase * path.GetTotalPathLength());
        }

        /// <summary>
        /// 追赶调度：推进进行中的相位平移（匀速），并按周期触发一次追赶 sweep。
        /// Drives in-progress phase shifts (uniform) and triggers one catch-up sweep per period.
        /// </summary>
        private void AdvanceCatchUp()
        {
            if (!catchUpEnabled)
            {
                return;
            }

            float dt = Time.deltaTime;
            float interval = Mathf.Max(0.0001f, catchUpInterval);

            // (1) 推进相位平移：把每个待平移槽位的相位从 startPhase 匀速拉到 targetPhase
            for (int i = _phaseShifts.Count - 1; i >= 0; i--)
            {
                var s = _phaseShifts[i];
                s.t += dt / interval;
                if (s.t >= 1f)
                {
                    _slotPhase[s.slot] = s.targetPhase % 1f;
                    _phaseShifts.RemoveAt(i);
                }
                else
                {
                    _slotPhase[s.slot] = Mathf.Lerp(s.startPhase, s.targetPhase, s.t);
                }
            }

            // 本周期平移完成 → 恢复被瞬移的空槽位为可用
            if (_phaseShifts.Count == 0 && _unavailableSlot >= 0)
            {
                _unavailableSlot = -1;
            }

            // (2) 周期开始 → 执行一次追赶 sweep
            _catchUpTimer += dt;
            if (_catchUpTimer >= interval && _phaseShifts.Count == 0)
            {
                _catchUpTimer = 0f;
                DoCatchUpSweep();
            }
        }

        /// <summary>
        /// 追赶 sweep：从队首向旋转反向找「直接相连占用块」，把其后的第一个空槽位瞬移到队首（标记不可用），
        /// 其余所有槽位本周期内整体向前平移一格（相位 +1/slotCount），把空隙挤到队尾。
        /// Catch-up sweep: from the leader, find the contiguous occupied block (walking opposite rotation),
        /// teleport the first empty slot after it onto the leader (marking it unavailable), and shift every
        /// other slot one step forward this period, pushing the gap to the tail.
        /// </summary>
        private void DoCatchUpSweep()
        {
            if (_leaderSlot < 0)
            {
                return;   // 空传送带，无队首
            }

            int n = slots.Length;

            // 相位会被追赶重排，索引顺序不再等于传送带上的顺序；以队首相位为锚，
            // 用「落后于队首的距离」（wrap 感知）对槽位排序，得到真实的前→后顺序。
            float leaderPhase = _slotPhase[_leaderSlot];
            var order = new int[n];
            var dist = new float[n];
            for (int i = 0; i < n; i++)
            {
                order[i] = i;
                dist[i] = (leaderPhase - _slotPhase[i] + 1f) % 1f;
            }
            Array.Sort(order, (a, b) => dist[a].CompareTo(dist[b]));

            // 从队首向队尾（落后距离递增）找「直接相连」的已占用块，直到第一个空槽位
            var connected = new HashSet<int>();
            int gap = -1;
            for (int k = 0; k < n; k++)
            {
                int idx = order[k];
                if (slots[idx] == null)
                {
                    gap = idx;
                    break;
                }
                connected.Add(idx);
            }

            // 占用块本身已连续（含全占满）→ 无内部空隙，无需追赶
            int occupiedCount = 0;
            for (int i = 0; i < n; i++)
            {
                if (slots[i] != null)
                {
                    occupiedCount++;
                }
            }
            if (connected.Count == occupiedCount)
            {
                return;
            }

            // 1. 把该空槽位瞬移到队首位置，并标记不可用（经过入口时不被选中）
            _slotPhase[gap] = leaderPhase;
            _unavailableSlot = gap;

            // 2. 除队首与直接相连占用块外，其余槽位本周期内整体向前平移一格（相位 +1/n）
            float step = 1f / n;
            for (int i = 0; i < n; i++)
            {
                if (connected.Contains(i))
                {
                    continue;
                }
                _phaseShifts.Add(new PhaseShift
                {
                    slot = i,
                    startPhase = _slotPhase[i],
                    targetPhase = _slotPhase[i] + step,
                    t = 0f
                });
            }
        }

        /// <summary>清空某槽位并做队首更替。不解除 parent、不触发 OnLeave。</summary>
        private void VacateSlot(int slotIndex)
        {
            slots[slotIndex] = null;

            if (slotIndex == _leaderSlot)
            {
                _leaderSlot = FindNextLeader(slotIndex);
            }
        }

        /// <summary>以刚离开的队首相位为锚，找「落后距离」最小的占用槽作为新队首；全空则返回 -1。</summary>
        private int FindNextLeader(int fromSlot)
        {
            int n = slots.Length;
            float anchor = _slotPhase[fromSlot];
            int best = -1;
            float bestDist = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                if (slots[i] == null)
                {
                    continue;
                }
                float d = (anchor - _slotPhase[i] + 1f) % 1f;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = i;
                }
            }
            return best;
        }
    }
}
