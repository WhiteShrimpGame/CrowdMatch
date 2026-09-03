using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 「挤地铁」封闭式缓冲区（3D 物理版）：把游戏区（像素网格）与漏斗缓冲区合并为一个完全封闭的区间。
    /// 漏斗两条斜边（入口 entranceWidth → 缺口 gapWidth）保持不变；从斜边两个端点（入口两端）向 -Z 延伸两条新墙，
    /// 加宽至后墙（宽 backWidth 可配，上底封口），把游戏区框在其中，球不会漏出；缺口边（窄 gapWidth）同样封口，
    /// 像素只能通过"释放"（移除碰撞体后匀速穿过封口墙）离开。
    /// 像素离开 PixelGroup 后先匀速移动到入口边，再附加刚体（SphereCollider + Rigidbody，冻结 Y / 关重力），
    /// 每个物理帧把速度直接设定为朝出口方向，由物理引擎处理像素间碰撞挤开与墙约束；
    /// 抵达缺口附近后按最小时间间隔逐个释放（移除碰撞体），先匀速移动到出口位置，再匀速移动到集结位置，
    /// 置 arrivedAtGatherPoint 并交由 ContainerGroup 消费。
    ///
    /// 几何：XZ 平面上的封闭区域（y=0）。漏斗梯形（入口 → 缺口）+ 游戏区梯形（入口 → 后墙）共享入口边；
    /// 窄缺口边（宽 gapWidth）与后墙（宽 backWidth）都封口，整个区间无物理开口，墙均为 static BoxCollider。
    /// </summary>
    public class CrowdBufferZone : MonoBehaviour
    {
        [Header("几何")]
        [Tooltip("入口边宽度（漏斗宽边，也是游戏区封闭区间与漏斗的共享边；中心为组件自身位置）")]
        public float entranceWidth = 8f;

        [Tooltip("缺口中心（Transform 引用），缺口边（已封口）朝集结位置，像素经释放在此穿过")]
        public Transform gapPoint;

        [Tooltip("缺口宽度（收窄后的排队口，单文件通过）")]
        public float gapWidth = 0.4f;

        [Tooltip("后墙（上底封口）宽度，应能把游戏区框在其中（通常 ≥ entranceWidth）")]
        public float backWidth = 9f;

        [Tooltip("后墙到入口中心的距离（游戏区封闭区间沿 -Z 的深度）")]
        public float backDepth = 9f;

        [Tooltip("物理阶段起始范围：从入口边沿 -Z（朝像素群）方向延伸的距离。提取中的像素向正前方移动、一旦进入该范围（离入口边还有这段距离）就提前赋予刚体、朝缺口方向移动。设为 0 表示到入口边才进入物理阶段")]
        public float physicalEntryDepth = 0f;

        [Header("像素物理")]
        [Tooltip("碰撞球世界半径（球视觉直径 = 像素直径 0.5，0.25 即刚好接触；调小可穿插表现拥挤）")]
        public float radius = 0.25f;

        [Tooltip("进入缓冲区（匀速阶段）与物理阶段的驱动速度（物理阶段每帧朝缺口方向直接设定速度）")]
        public float crowdSpeed = 5f;

        [Header("墙")]
        [Tooltip("墙厚度")]
        public float wallThickness = 0.1f;

        [Tooltip("墙高度（应 ≥ 像素直径，覆盖像素竖直范围）")]
        public float wallHeight = 2f;

        [Header("提取（网格寻路）")]
        [Tooltip("匹配像素在网格内寻路离开（并行 sweep）时的移动速度（世界单位/秒），同时决定 sweep 时间片 = CellSizeZ / 该值")]
        public float extractSpeed = 5f;

        [Tooltip("提取阶段（网格寻路 + 移向入口边）像素 z 正方向朝向移动方向时的旋转角速度（度/秒）")]
        public float extractRotateSpeed = 360f;

        [Tooltip("移向入口边阶段，同一列（同入口点）像素排队的前后间距（世界单位），前方未进物理前后方不追尾")]
        public float entryQueueSpacing = 0.5f;

        [Header("释放")]
        [Tooltip("距缺口中心多近触发释放（缺口已封口，需 ≥ radius + wallThickness/2，否则贴墙像素够不到释放范围、卡死）")]
        public float releaseRadius = 0.6f;

        [Tooltip("两个先后释放像素之间的最小时间间隔（秒）")]
        public float minReleaseInterval = 0.15f;

        [Tooltip("释放后匀速移动到集结位置的速度（世界单位/秒）")]
        public float releaseSpeed = 8f;

        [Header("引用")]
        [Tooltip("集结位置（= GameController.gatherPoint），像素解除约束后移动到这里")]
        public Transform collectPoint;

        [Tooltip("释放后像素进入的闭环传送带；留空则回退到旧的集结位置 + gatheredItems 路径。指定后关闭按帧释放，改由 ConveyorBeltZone 槽位过关口时调 CollectNearest 收集")]
        public ConveyorBeltZone conveyorZone;

        /// <summary>抵达集结位置 / 落位点的判定阈值（世界单位）</summary>
        private const float ArriveEpsilon = 0.05f;

        /// <summary>提取阶段（在网格内寻路离开）单个像素的状态</summary>
        private class ExtractState
        {
            public PixelItem item;
            public int col, row;        // 当前逻辑格（本 tick 结束后所在格）
            public int waitCount;       // 等待计数：被挡住的次数（公平性，等待越多下次越优先）

            public bool moving;         // 是否在网格内做格子到格子的平滑动画
            public Vector3 animFrom;    // 动画起点（世界坐标）
            public Vector3 animTo;      // 动画终点（世界坐标）
            public float animT;         // 动画进度 0..1

            public bool exiting;        // 已离开网格、正在移向入口边

            // 本 tick 的决策（瞬态，每次 sweep 前重置）
            public bool pendingExit;            // 本 tick 决定退出网格
            public Vector2Int pendingNext;      // 本 tick 决定移入的格（-1,-1 = 不动）
            public bool resolved;               // 本 tick 是否已确定（移动或退出）
        }

        /// <summary>提取阶段的像素（保持前到后顺序）</summary>
        private readonly List<ExtractState> _extracting = new List<ExtractState>();

        /// <summary>提取期间引用的 PixelGroup 及其网格占用表</summary>
        private PixelGroup _extractGroup;
        private bool[,] _matchedOccupied;   // 尚未离开的匹配像素占用的格（每次 sweep 原子更新）

        /// <summary>并行 sweep 的时间片（一个 tick 移动一格，时长 = 格距 / 速度，动画与逻辑同步）</summary>
        private float _extractTickInterval;
        private float _extractTickTimer;

        /// <summary>物理阶段（已附加刚体）的像素</summary>
        private readonly List<PixelItem> _physical = new List<PixelItem>();

        private float _lastReleaseTime = float.NegativeInfinity;

        /// <summary>是否正在提取（还有匹配像素在网格内寻路离开）</summary>
        public bool IsExtracting => _extracting.Count > 0;

        /// <summary>一批像素全部离开网格、进入缓冲区后触发（供 GameController 补位）</summary>
        public event System.Action OnBatchExtracted;

        // 封闭区间的墙（运行时创建，static 碰撞体）：漏斗两条斜边 + 游戏区两条侧边 + 后墙 + 缺口封口墙
        private GameObject _funnelLeftWall;
        private GameObject _funnelRightWall;
        private GameObject _enclosureLeftWall;
        private GameObject _enclosureRightWall;
        private GameObject _backWall;
        private GameObject _gapWall;

        private void Awake()
        {
            BuildWalls();
        }

        /// <summary>
        /// 一批匹配像素离开网格时调用：按前到后顺序在网格内寻路（BFS）离开，
        /// 只走已腾出或"本 tick 即将腾出"的格子，抵达入口边后进入物理阶段。补位由 OnBatchExtracted 回调触发。
        /// </summary>
        public void EnterBatch(List<PixelItem> matched, PixelGroup group)
        {
            if (matched == null || matched.Count == 0 || group == null)
                return;

            _extractGroup = group;
            _matchedOccupied = new bool[group.columns, group.TotalRows];
            _extractTickInterval = group.CellSizeZ / Mathf.Max(0.0001f, extractSpeed);
            _extractTickTimer = 0f;

            foreach (var item in matched)
            {
                if (item == null)
                    continue;

                // 像素已离开网格：关闭球碰撞体（停止点击检测，进入阶段不参与物理），进入物理阶段时复用同一碰撞体
                var sphere = item.GetComponent<SphereCollider>();
                if (sphere != null)
                    sphere.enabled = false;

                if (!group.IsInRange(item.gridX, item.gridZ))
                    continue;

                _matchedOccupied[item.gridX, item.gridZ] = true;
                _extracting.Add(new ExtractState
                {
                    item = item,
                    col = item.gridX,
                    row = item.gridZ,
                });
            }

            // 空批（全部越界 / 为 null）：直接通知补位
            if (_extracting.Count == 0)
                OnBatchExtracted?.Invoke();
        }

        /// <summary>清空缓冲区状态并销毁提取中 / 物理阶段的像素（供重载关卡时清理，不触发 OnBatchExtracted）。</summary>
        public void ResetAll()
        {
            foreach (var st in _extracting)
            {
                if (st != null && st.item != null)
                    Destroy(st.item.gameObject);
            }
            _extracting.Clear();

            foreach (var p in _physical)
            {
                if (p != null)
                    Destroy(p.gameObject);
            }
            _physical.Clear();

            _extractGroup = null;
            _matchedOccupied = null;
            _lastReleaseTime = float.NegativeInfinity;
        }

        private void Update()
        {
            StepExtracting();
            TryRelease();
        }

        private void FixedUpdate()
        {
            if (_physical.Count == 0)
                return;

            RefreshGeometry(out _, out Vector3 gap, out _, out _, out _);

            // 每个物理帧把速度直接设定为朝出口（gap）方向；碰撞挤开与侧边墙仍由物理引擎处理
            for (int i = _physical.Count - 1; i >= 0; i--)
            {
                var p = _physical[i];
                if (p == null)
                {
                    _physical.RemoveAt(i);
                    continue;
                }

                var rb = p.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    _physical.RemoveAt(i);
                    continue;
                }

                Vector3 dir = gap - p.transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    rb.velocity = dir.normalized * crowdSpeed;
                    // 物理移动阶段：z 正方向始终朝向出口（gap）
                    p.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
                }
                else
                {
                    rb.velocity = Vector3.zero;
                }
            }
        }

        /// <summary>提取阶段：推进退出动画、网格内平滑动画，并按 tick 触发并行 sweep。</summary>
        private void StepExtracting()
        {
            if (_extracting.Count == 0)
                return;

            float dt = Time.deltaTime;

            RefreshGeometry(out Vector3 entrance, out _, out Vector3 axis, out Vector3 perp, out _);

            // 1. 已离开网格的像素：连续匀速移向入口边落位点；同一列排队（前不追尾），
            //    一旦进入物理起始范围（离入口边还有 physicalEntryDepth）即提前赋予刚体朝缺口
            var exiting = new List<ExtractState>(_extracting.Count);
            for (int i = 0; i < _extracting.Count; i++)
            {
                var st = _extracting[i];
                if (!st.exiting)
                    continue;

                if (st.item == null)
                {
                    _extracting.RemoveAt(i);
                    i--;
                    continue;
                }
                exiting.Add(st);
            }

            foreach (var st in exiting)
            {
                Vector3 target = ComputeEntryTarget(st.item.transform.position, entrance, perp);
                Vector3 moveTarget = ApplyEntryQueue(st, target, entrance, axis, perp, exiting);
                MoveToward(st, moveTarget);

                bool reachedTarget = XZDistance(st.item.transform.position, target) <= ArriveEpsilon;
                bool enteredRange = physicalEntryDepth > 0f
                    && Vector3.Dot(st.item.transform.position - entrance, axis) >= -physicalEntryDepth;
                if (reachedTarget || enteredRange)
                {
                    _extracting.Remove(st);
                    EnterPhysical(st.item);
                }
            }

            // 2. 网格内移动动画推进（格子到格子平滑插值，时长 = tick 间隔）
            for (int i = 0; i < _extracting.Count; i++)
            {
                var st = _extracting[i];
                if (st.exiting || !st.moving)
                    continue;
                if (st.item == null)
                {
                    _extracting.RemoveAt(i);
                    i--;
                    continue;
                }

                st.animT += dt / _extractTickInterval;
                if (st.animT >= 1f)
                {
                    st.animT = 1f;
                    st.moving = false;
                }
                st.item.transform.position = Vector3.Lerp(st.animFrom, st.animTo, st.animT);

                // 网格内移动：z 正方向匀速朝向移动方向（animFrom → animTo）
                RotateToward(st.item, st.animTo - st.animFrom);
            }

            // 3. tick 累积 + 并行 sweep（每次 sweep 让所有可移动像素同时推进一格）
            _extractTickTimer += dt;
            if (_extractTickTimer >= _extractTickInterval)
            {
                _extractTickTimer -= _extractTickInterval;
                SweepOnce();
            }

            // 4. 全部离开 → 清理并通知补位
            if (_extracting.Count == 0)
            {
                _extractGroup = null;
                _matchedOccupied = null;
                OnBatchExtracted?.Invoke();
            }
        }

        /// <summary>
        /// 一次并行 sweep：基于本 tick 开始前的占用快照，算出所有可移动像素的下一格。
        /// 关键语义——"本 tick 即将腾出的格"可被同时移入（vacated），从而整列/整批能一波一波连续推进；
        /// 唯一会等待的是多个球争用同一个单格（窄缝），此时按"等待次数多者优先"依次通过。
        /// </summary>
        private void SweepOnce()
        {
            int cols = _extractGroup.columns;
            int rows = _extractGroup.TotalRows;

            // 重置本 tick 决策
            foreach (var st in _extracting)
            {
                st.pendingExit = false;
                st.pendingNext = new Vector2Int(-1, -1);
                st.resolved = false;
            }

            // 参与决策的球（在网格内、未退出）
            var order = new List<ExtractState>();
            foreach (var st in _extracting)
                if (st.item != null && !st.exiting)
                    order.Add(st);

            // 公平性排序：等待次数多者优先；同等待按"前到后、同排靠中心"
            order.Sort((a, b) =>
            {
                int w = b.waitCount.CompareTo(a.waitCount);
                if (w != 0)
                    return w;
                int z = a.row.CompareTo(b.row);
                if (z != 0)
                    return z;
                float center = (cols - 1) * 0.5f;
                float da = Mathf.Abs(a.col - center);
                float db = Mathf.Abs(b.col - center);
                int dc = da.CompareTo(db);
                if (dc != 0)
                    return dc;
                return a.col.CompareTo(b.col);
            });

            // vacated：本 tick 被腾出的格（"即将腾出"）；claimed：本 tick 被移入的格（防两球同格）
            var vacated = new bool[cols, rows];
            var claimed = new bool[cols, rows];

            var exits = new List<ExtractState>();
            var movers = new List<ExtractState>();

            // 迭代到不动点：链式"即将腾出"需要多轮（后排依赖前排腾出后才看得到）
            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (var st in order)
                {
                    if (st.item == null || st.exiting || st.resolved)
                        continue;

                    // 能直接沿 +Z 退出吗（前方无障碍或前方"即将腾出"）
                    if (CanExit(st.col, st.row, vacated))
                    {
                        exits.Add(st);
                        st.resolved = true;
                        st.pendingExit = true;
                        vacated[st.col, st.row] = true;
                        changed = true;
                        continue;
                    }

                    Vector2Int next = FindNextCell(st.col, st.row, vacated);
                    if (next.x < 0)
                        continue;          // 无路：等下一轮（可能因 vacated 扩展而出现新路）
                    if (claimed[next.x, next.y])
                        continue;          // 目标格已被本 tick 的其他球抢占

                    movers.Add(st);
                    st.resolved = true;
                    st.pendingNext = next;
                    claimed[next.x, next.y] = true;
                    vacated[st.col, st.row] = true;
                    changed = true;
                }
            }

            // 未解决的球：等待计数 +1（公平性：被挡得越久，下次越优先）
            foreach (var st in _extracting)
            {
                if (st.item == null || st.exiting || st.resolved)
                    continue;
                st.waitCount++;
            }

            // 原子更新占用表 + 触发动画
            foreach (var st in exits)
            {
                _matchedOccupied[st.col, st.row] = false;
                st.waitCount = 0;
                st.moving = false;
                st.exiting = true;
            }
            foreach (var st in movers)
            {
                _matchedOccupied[st.col, st.row] = false;
                _matchedOccupied[st.pendingNext.x, st.pendingNext.y] = true;
                StartCellMove(st, st.pendingNext);
                st.col = st.pendingNext.x;
                st.row = st.pendingNext.y;
                st.waitCount = 0;
            }
        }

        /// <summary>开始一次格子到格子的平滑动画</summary>
        private void StartCellMove(ExtractState st, Vector2Int to)
        {
            st.animFrom = st.item.transform.position;
            st.animTo = _extractGroup.GetWorldPosition(to.x, to.y);
            st.animT = 0f;
            st.moving = true;
        }

        /// <summary>某格能否直接沿 +Z 退出网格（前方 = 更小的 row，无障碍或前方"即将腾出"）</summary>
        private bool CanExit(int col, int row, bool[,] vacated)
        {
            for (int r = 0; r < row; r++)
            {
                if (IsObstacle(col, r, vacated))
                    return false;
            }
            return true;
        }

        /// <summary>某格是否为障碍：未匹配球、尚未离开且本 tick 未腾出的匹配球</summary>
        private bool IsObstacle(int col, int row, bool[,] vacated)
        {
            if (_extractGroup.grid[col, row] != null)
                return true;
            if (_matchedOccupied[col, row] && !vacated[col, row])
                return true;
            return false;
        }

        /// <summary>BFS 从 (startCol,startRow) 找到可达的出口格，返回"第一步"的格子坐标；无路返回 (-1,-1)</summary>
        private Vector2Int FindNextCell(int startCol, int startRow, bool[,] vacated)
        {
            int cols = _extractGroup.columns;
            int rows = _extractGroup.TotalRows;

            var prev = new Vector2Int[cols, rows];
            for (int c = 0; c < cols; c++)
                for (int r = 0; r < rows; r++)
                    prev[c, r] = new Vector2Int(-1, -1);

            var start = new Vector2Int(startCol, startRow);
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            prev[startCol, startRow] = start;

            int[] dx = { 0, 0, 1, -1 };
            int[] dz = { 1, -1, 0, 0 };

            Vector2Int goal = new Vector2Int(-1, -1);
            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                for (int d = 0; d < 4; d++)
                {
                    int nx = cur.x + dx[d];
                    int nz = cur.y + dz[d];
                    if (!_extractGroup.IsInRange(nx, nz))
                        continue;
                    if (prev[nx, nz].x >= 0)
                        continue;
                    if (IsObstacle(nx, nz, vacated))
                        continue;

                    prev[nx, nz] = cur;
                    if (CanExit(nx, nz, vacated))
                    {
                        goal = new Vector2Int(nx, nz);
                        queue.Clear();
                        break;
                    }
                    queue.Enqueue(new Vector2Int(nx, nz));
                }
            }

            if (goal.x < 0)
                return new Vector2Int(-1, -1);

            // 回溯到起点，取第一步
            var path = new List<Vector2Int>();
            var node = goal;
            while (node != start)
            {
                path.Add(node);
                node = prev[node.x, node.y];
                if (node.x < 0)
                    return new Vector2Int(-1, -1);
            }
            if (path.Count == 0)
                return new Vector2Int(-1, -1);

            return path[path.Count - 1];
        }

        /// <summary>计算入口边落位点：保持横向位置、按入口宽度 clamp（不瞬移）</summary>
        private Vector3 ComputeEntryTarget(Vector3 pos, Vector3 entrance, Vector3 perp)
        {
            Vector3 rel = pos - entrance;
            rel.y = 0f;
            float lateral = Vector3.Dot(rel, perp);
            float clampHalf = Mathf.Max(0f, entranceWidth * 0.5f - radius);
            lateral = Mathf.Clamp(lateral, -clampHalf, clampHalf);

            Vector3 entry = entrance + perp * lateral;
            entry.y = pos.y;
            return entry;
        }

        /// <summary>
        /// 入口排队：若像素前方（更接近入口边）存在同列（横向接近）的退出像素且前后间距不足，
        /// 则把移动目标退回到前方像素后 entryQueueSpacing 处（横向保持自身当前值），实现同列前不追尾。
        /// 无阻挡时返回原目标。横向按 radius 判定同列，避免不同列的像素被误排。
        /// </summary>
        private Vector3 ApplyEntryQueue(ExtractState st, Vector3 target, Vector3 entrance, Vector3 axis, Vector3 perp, List<ExtractState> exiting)
        {
            Vector3 pos = st.item.transform.position;
            float myProg = Vector3.Dot(pos - entrance, axis);
            float latMe = Vector3.Dot(pos - entrance, perp);

            foreach (var other in exiting)
            {
                if (other == st || other.item == null)
                    continue;

                Vector3 op = other.item.transform.position;
                float oProg = Vector3.Dot(op - entrance, axis);
                if (oProg <= myProg)
                    continue;   // 不在前方

                float latOther = Vector3.Dot(op - entrance, perp);
                if (Mathf.Abs(latOther - latMe) > radius)
                    continue;   // 不同列（横向不接近），互不阻塞

                float gap = oProg - myProg;
                if (gap < entryQueueSpacing)
                {
                    float stopProg = oProg - entryQueueSpacing;
                    if (stopProg < myProg)
                        stopProg = myProg;   // 不后退，保持原位等待
                    Vector3 stop = entrance + perp * latMe + axis * stopProg;
                    stop.y = pos.y;
                    return stop;
                }
            }
            return target;
        }

        /// <summary>匀速移动像素到目标点（保持 Y 不变）</summary>
        private void MoveToward(ExtractState st, Vector3 target)
        {
            Vector3 pos = st.item.transform.position;
            Vector3 to = target - pos;
            to.y = 0f;
            float dist = to.magnitude;
            if (dist <= ArriveEpsilon)
                return;

            Vector3 dir = to / dist;
            pos += dir * Mathf.Min(extractSpeed * Time.deltaTime, dist);
            pos.y = st.item.transform.position.y;
            st.item.transform.position = pos;

            // 移向入口边：z 正方向匀速朝向移动方向
            RotateToward(st.item, dir);
        }

        /// <summary>匀速旋转像素使 z 正方向朝向指定世界方向（XZ 平面，角速度由 extractRotateSpeed 决定）。</summary>
        private void RotateToward(PixelItem item, Vector3 dir)
        {
            if (item == null)
                return;
            dir.y = 0f;
            if (dir.sqrMagnitude <= 0.0001f)
                return;

            Quaternion target = Quaternion.LookRotation(dir.normalized, Vector3.up);
            item.transform.rotation = Quaternion.RotateTowards(
                item.transform.rotation, target, extractRotateSpeed * Time.deltaTime);
        }

        /// <summary>XZ 平面距离（忽略 Y）</summary>
        private static float XZDistance(Vector3 a, Vector3 b)
        {
            Vector3 d = a - b;
            d.y = 0f;
            return d.magnitude;
        }

        /// <summary>附加 SphereCollider + Rigidbody，进入物理模拟（碰撞挤开 + 侧边墙约束）；速度由 FixedUpdate 每帧设定</summary>
        private void EnterPhysical(PixelItem item)
        {
            // 防御：若仍残留非球碰撞体（如旧 Cube 像素未重新生成），移除之，确保只有球碰撞体参与物理
            var cols = item.GetComponents<Collider>();
            for (int i = 0; i < cols.Length; i++)
            {
                var c = cols[i];
                if (c is SphereCollider)
                    continue;
                c.enabled = false;
                Destroy(c);
            }

            // 球碰撞体：radius 字段为世界半径，SphereCollider.radius 是本地值（乘 lossyScale），需除回
            var sphere = item.GetComponent<SphereCollider>();
            if (sphere == null)
                sphere = item.gameObject.AddComponent<SphereCollider>();
            float s = Mathf.Max(0.0001f, item.transform.lossyScale.x);
            sphere.radius = radius / s;
            sphere.enabled = true;

            // 刚体：冻结 Y 与旋转，关重力，只在 XZ 平面做真实碰撞
            var rb = item.GetComponent<Rigidbody>();
            if (rb == null)
                rb = item.gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.mass = 1f;
            rb.drag = 0f;
            rb.angularDrag = 0.05f;
            rb.constraints = RigidbodyConstraints.FreezePositionY
                           | RigidbodyConstraints.FreezeRotationX
                           | RigidbodyConstraints.FreezeRotationY
                           | RigidbodyConstraints.FreezeRotationZ;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // 进入物理阶段即时给一个朝出口（gap）的初速度，后续由 FixedUpdate 每帧重写
            RefreshGeometry(out _, out Vector3 gap, out _, out _, out _);
            Vector3 dir = gap - item.transform.position;
            dir.y = 0f;
            dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.forward;
            rb.velocity = dir * crowdSpeed;

            _physical.Add(item);
        }

        /// <summary>每个满足间隔的帧，释放距缺口最近的已就位像素（每次最多一个）</summary>
        private void TryRelease()
        {
            // 传送带模式：释放由「槽位过关口」驱动（ConveyorBeltZone 调 CollectNearest），这里不再按帧释放
            if (conveyorZone != null)
                return;

            if (_physical.Count == 0)
                return;
            if (Time.time - _lastReleaseTime < minReleaseInterval)
                return;

            RefreshGeometry(out _, out Vector3 gap, out _, out _, out _);

            PixelItem front = null;
            float bestDist = float.MaxValue;
            foreach (var p in _physical)
            {
                if (p == null)
                    continue;
                Vector3 toGap = gap - p.transform.position;
                toGap.y = 0f;
                float d = toGap.magnitude;
                if (d <= releaseRadius && d < bestDist)
                {
                    bestDist = d;
                    front = p;
                }
            }

            if (front == null)
                return;

            Release(front);
        }

        /// <summary>解除小球物理约束并匀速移动到集结位置（仅 fallback 无传送带路径）。</summary>
        private void Release(PixelItem item)
        {
            _physical.Remove(item);
            _lastReleaseTime = Time.time;
            DetachPhysics(item);
            StartCoroutine(MoveToCollect(item));
        }

        /// <summary>从物理队列取出距缺口最近（且在 releaseRadius 内）的小球并解除物理约束；无则 null。供传送带「槽位过关口」直接收集。</summary>
        public PixelItem CollectNearest()
        {
            if (_physical.Count == 0)
                return null;

            RefreshGeometry(out _, out Vector3 gap, out _, out _, out _);

            PixelItem best = null;
            float bestDist = float.MaxValue;
            foreach (var p in _physical)
            {
                if (p == null)
                    continue;
                Vector3 toGap = gap - p.transform.position;
                toGap.y = 0f;
                float d = toGap.magnitude;
                if (d <= releaseRadius && d < bestDist)
                {
                    bestDist = d;
                    best = p;
                }
            }

            if (best == null)
                return null;

            _physical.Remove(best);
            _lastReleaseTime = Time.time;
            DetachPhysics(best);
            return best;
        }

        /// <summary>移除刚体与碰撞体（停止物理影响与碰撞）。</summary>
        private void DetachPhysics(PixelItem item)
        {
            var rb = item.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;   // 立即停止物理影响 transform
                rb.velocity = Vector3.zero;
                Destroy(rb);
            }

            var sphere = item.GetComponent<SphereCollider>();
            if (sphere != null)
            {
                sphere.enabled = false;  // 立即停止碰撞
                Destroy(sphere);
            }
        }

        /// <summary>解除约束后，先匀速移动到出口（gap）位置，再匀速移动到集结位置</summary>
        private IEnumerator MoveToCollect(PixelItem item)
        {
            // 第一步：匀速移动到出口（gap）位置，确保像素正好穿过缺口
            Vector3 gapPos = gapPoint != null ? gapPoint.position : item.transform.position;
            yield return MoveUniform(item, gapPos);

            // 第二步：匀速移动到集结点
            Vector3 target = collectPoint != null ? collectPoint.position : item.transform.position;
            yield return MoveUniform(item, target);

            OnArrived(item);
        }

        /// <summary>从当前位置匀速直线移动到目标点（保持 Y 不变）</summary>
        private IEnumerator MoveUniform(PixelItem item, Vector3 target)
        {
            float y = item.transform.position.y;

            while (true)
            {
                Vector3 pos = item.transform.position;
                Vector3 toTarget = target - pos;
                toTarget.y = 0f;
                float dist = toTarget.magnitude;

                if (dist <= ArriveEpsilon)
                    break;

                Vector3 dir = toTarget / dist;
                pos += dir * Mathf.Min(releaseSpeed * Time.deltaTime, dist);
                pos.y = y;
                item.transform.position = pos;

                yield return null;
            }

            Vector3 finalPos = target;
            finalPos.y = y;
            item.transform.position = finalPos;
        }

        /// <summary>抵达集结位置：parent 到 collectPoint，置标记并加入 gatheredItems（沿用现有契约）</summary>
        private void OnArrived(PixelItem item)
        {
            if (collectPoint != null)
                item.transform.SetParent(collectPoint, true);

            item.arrivedAtGatherPoint = true;

            var gc = GameController.Instance;
            if (gc != null)
                gc.gatheredItems.Add(item);
        }

        /// <summary>创建封闭区间的六条墙（static BoxCollider）：漏斗两条斜边（入口→缺口）+ 游戏区两条侧边（入口→后墙）+ 后墙（上底封口）+ 缺口封口墙</summary>
        private void BuildWalls()
        {
            RefreshGeometry(out Vector3 entrance, out Vector3 gap, out Vector3 axis, out Vector3 perp, out _);
            gap.y = entrance.y;

            float halfEntrance = entranceWidth * 0.5f;
            float halfGap = gapWidth * 0.5f;
            float halfBack = backWidth * 0.5f;

            Vector3 backCenter = entrance - axis * backDepth;
            backCenter.y = entrance.y;

            Vector3 entranceLeft = entrance - perp * halfEntrance;
            Vector3 entranceRight = entrance + perp * halfEntrance;
            Vector3 gapLeft = gap - perp * halfGap;
            Vector3 gapRight = gap + perp * halfGap;
            Vector3 backLeft = backCenter - perp * halfBack;
            Vector3 backRight = backCenter + perp * halfBack;

            // 漏斗斜边：保持不变
            CreateWall("CrowdBufferFunnelLeft", entranceLeft, gapLeft, ref _funnelLeftWall);
            CreateWall("CrowdBufferFunnelRight", entranceRight, gapRight, ref _funnelRightWall);
            // 游戏区侧边：从斜边两个端点（入口两端）向 -Z 延伸的新碰撞体
            CreateWall("CrowdBufferEnclosureLeft", entranceLeft, backLeft, ref _enclosureLeftWall);
            CreateWall("CrowdBufferEnclosureRight", entranceRight, backRight, ref _enclosureRightWall);
            // 后墙：上底封口
            CreateWall("CrowdBufferBackWall", backLeft, backRight, ref _backWall);
            // 缺口封口墙：出口处也封口，像素只能通过释放（移除碰撞体后匀速穿过）离开
            CreateWall("CrowdBufferGapWall", gapLeft, gapRight, ref _gapWall);
        }

        /// <summary>创建一条墙：BoxCollider 长度沿墙方向，高度沿世界 Y，厚度沿法向</summary>
        private void CreateWall(string name, Vector3 from, Vector3 to, ref GameObject wall)
        {
            Vector3 sideDir = to - from;
            sideDir.y = 0f;
            float length = sideDir.magnitude;
            if (length < 0.0001f)
                return;
            sideDir /= length;

            Vector3 mid = (from + to) * 0.5f;

            if (wall == null)
            {
                wall = new GameObject(name);
                wall.transform.SetParent(transform, false);
            }

            wall.transform.position = mid;
            wall.transform.rotation = Quaternion.LookRotation(sideDir, Vector3.up);

            var box = wall.GetComponent<BoxCollider>();
            if (box == null)
                box = wall.AddComponent<BoxCollider>();
            box.size = new Vector3(wallThickness, wallHeight, length);
            box.center = Vector3.zero;
            box.isTrigger = false;
        }

        /// <summary>在 Scene 视图绘制封闭区域边缘、前进方向与缺口释放范围</summary>
        private void OnDrawGizmos()
        {
            RefreshGeometry(out Vector3 entrance, out Vector3 gap, out Vector3 axis, out Vector3 perp, out _);
            gap.y = entrance.y;

            float halfEntrance = entranceWidth * 0.5f;
            float halfGap = gapWidth * 0.5f;
            float halfBack = backWidth * 0.5f;
            Vector3 backCenter = entrance - axis * backDepth;
            backCenter.y = entrance.y;

            Vector3 entranceLeft = entrance - perp * halfEntrance;
            Vector3 entranceRight = entrance + perp * halfEntrance;
            Vector3 gapLeft = gap - perp * halfGap;
            Vector3 gapRight = gap + perp * halfGap;
            Vector3 backLeft = backCenter - perp * halfBack;
            Vector3 backRight = backCenter + perp * halfBack;

            // 封闭区间边缘
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(gapLeft, gapRight);           // 缺口边（出口，封口）
            Gizmos.DrawLine(entranceLeft, gapLeft);       // 漏斗斜边（左，保持不变）
            Gizmos.DrawLine(entranceRight, gapRight);     // 漏斗斜边（右，保持不变）
            Gizmos.DrawLine(entranceLeft, backLeft);      // 游戏区侧边（左，向 -Z 延伸）
            Gizmos.DrawLine(entranceRight, backRight);    // 游戏区侧边（右，向 -Z 延伸）
            Gizmos.DrawLine(backLeft, backRight);         // 后墙（上底封口）

            // 前进方向
            Gizmos.color = Color.green;
            Gizmos.DrawLine(backCenter, gap);

            // 物理阶段起始线（离入口边 -Z 方向 physicalEntryDepth 处，越过即赋予刚体朝缺口）
            if (physicalEntryDepth > 0f)
            {
                Gizmos.color = new Color(1f, 0.55f, 0f);
                Vector3 entryLineCenter = entrance - axis * physicalEntryDepth;
                entryLineCenter.y = entrance.y;
                Vector3 entryLineLeft = entryLineCenter - perp * halfBack;
                Vector3 entryLineRight = entryLineCenter + perp * halfBack;
                Gizmos.DrawLine(entryLineLeft, entryLineRight);
            }

            // 缺口可释放范围（releaseRadius）
            Gizmos.color = Color.yellow;
            DrawXZCircle(gap, releaseRadius);
        }

        private static void DrawXZCircle(Vector3 center, float radius, int segments = 32)
        {
            if (segments < 3)
                segments = 3;

            Vector3 prev = center + new Vector3(radius, 0f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                Vector3 p = center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
                Gizmos.DrawLine(prev, p);
                prev = p;
            }
        }

        /// <summary>重算几何：入口中心（自身位置）、缺口中心、轴向、垂直方向、轴向长度</summary>
        private void RefreshGeometry(out Vector3 entrance, out Vector3 gap, out Vector3 axis, out Vector3 perp, out float length)
        {
            entrance = transform.position;
            gap = gapPoint != null ? gapPoint.position : entrance + Vector3.forward * 4f;

            Vector3 delta = gap - entrance;
            delta.y = 0f;
            length = delta.magnitude;

            if (length < 0.0001f)
            {
                axis = Vector3.forward;
                perp = Vector3.right;
                length = 1f;
            }
            else
            {
                axis = delta / length;
                perp = new Vector3(-axis.z, 0f, axis.x); // XZ 平面旋转 90°
            }
        }
    }
}
