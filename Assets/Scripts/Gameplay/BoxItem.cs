using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CrowdMatch
{
    /// <summary>
    /// 箱子（Box）：网格内一块矩形区域（左上 + 右下），内含若干隐藏 Pixel 并标注容量。
    /// 开箱前区域是障碍；当可用格（本体 + 相邻 + 连通）≥ 容量时开箱，把隐藏 Pixel 整体规划
    /// 释放到这些候选格（优先级：本体 > 相邻 > 连通距离），确保同色像素各自 4 方向连通。
    ///
    /// 释放表现（两段同一套节奏）：数字**在开箱那一刻**先隐藏；本体外与本体内 Pixel 都按
    /// 「从**箱子中心下方**出现 + Jump 跳到目标格」逐个冒出，**起跳点、Jump 参数、起跳间隔全部共用**
    /// （只有目标格不同）；同时箱子视觉**匀速下沉**，沉完销毁。
    /// 相邻仅按上下左右 4 方向（不含四角）。
    /// </summary>
    public class BoxItem : MonoBehaviour
    {
        [Header("区域（左上 + 右下）")]
        public int colMin, rowMin, colMax, rowMax;

        [Header("内容")]
        [Tooltip("容量 = 隐藏 Pixel 数量 = 开箱触发阈值。以 colorIds（内容数）为准；可占用本体 + 相邻 + 连通格。")]
        public int capacity;

        [Tooltip("每个隐藏 Pixel 的颜色 ID，长度 == capacity")]
        public int[] colorIds;

        [Header("行为")]
        [Tooltip("Pixel 起跳间隔（秒）—— 本体外与本体内**共用同一个节奏**，两段之间不额外等待")]
        public float jumpStartInterval = 0.1f;

        [Tooltip("Pixel 出现位置：从**箱子中心**下方多远处冒出来（-Y 偏移量；本体外与本体共用同一个起跳点）")]
        public float jumpSpawnYOffset = 0.5f;

        [Header("Jump 表现（外跳与本体共用同一套）")]
        [Tooltip("Jump 高度（DOLocalJump 的 jumpPower）")]
        public float jumpPower = 0.6f;

        [Tooltip("Jump 弹跳次数（DOLocalJump 的 numJumps）")]
        public int jumpCount = 1;

        [Tooltip("Jump 时长（秒，DOLocalJump 的 duration）")]
        public float jumpDuration = 0.35f;

        [Header("消失表现")]
        [Tooltip("开箱后箱子视觉**匀速下沉**的距离（本地 Y，正数 = 向下沉）")]
        public float disappearDropDistance = 1.5f;

        [Tooltip("箱子视觉下沉的时长（秒，匀速）；沉完即销毁")]
        public float disappearDropDuration = 0.4f;

        [Tooltip("下沉时**在世界坐标系下保持静止**的子物体（留空 = 无此表现）。" +
                 "下沉开始后的 holdStaticDuration 秒内它不动 —— 为此脚本把它在自身坐标系里反向抬升、" +
                 "正好补掉这一段的沉降量；这段时间过去后就跟着箱子一起沉下去")]
        public Transform holdStaticChild;

        [Tooltip("上面那个子物体保持**世界静止**的时长（秒，从下沉开始算）")]
        public float holdStaticDuration = 0.3f;

        [Header("调试")]
        [Tooltip("开箱条件判定时输出详细日志（本体/相邻/可用/容量/结果）")]
        public bool debugOpenLog = true;

        [Header("视觉（3 个预制体，各占一格）")]
        public GameObject cornerPrefab;
        public GameObject edgePrefab;
        public GameObject centerPrefab;

        [Tooltip("勾选后：视觉来自**本物体（整体预制体）自身**，不再按格拼接角/边/中心。" +
                 "2×2 的整体预制体走这条；由 PixelGroup.SpawnBox / 创建向导自动置位")]
        public bool wholePrefab;

        [Tooltip("显示箱内 Pixel 总数（= 容量）的 UI Text，留空自动从子物体查找")]
        public Text countText;

        /// <summary>是否已开箱（开箱后仅保留引用，不再参与触发判定）。</summary>
        [System.NonSerialized] public bool opened;

        /// <summary>所属的 PixelGroup（运行时由 RebuildGrid 赋值）。</summary>
        [System.NonSerialized] public PixelGroup group;

        /// <summary>尚未释放的隐藏 Pixel（开箱时逐个移出，落到 grid）。</summary>
        [System.NonSerialized] public readonly List<PixelItem> hiddenPixels = new List<PixelItem>();

        /// <summary>箱子视觉拼接出的 3 类预制体实例（开箱消失动画用）。</summary>
        private readonly List<GameObject> _visualPieces = new List<GameObject>();

        /// <summary>回溯搜索预算（访问节点数上限）：防止极端关卡下穷举爆炸；超预算退回贪心兜底。</summary>
        private const int BacktrackBudget = 50000;

        /// <summary>回溯中单个颜色最多尝试的候选连通块数量。</summary>
        private const int MaxBlocksPerColor = 64;

        public int BodyCount => (colMax - colMin + 1) * (rowMax - rowMin + 1);

        /// <summary>本体尺寸是否 2×2。</summary>
        public bool Is2x2 => (colMax - colMin + 1) == 2 && (rowMax - rowMin + 1) == 2;

        /// <summary>
        /// 该矩形是否该用「整体预制体」：**2×2** 且 PixelGroup 配了 <see cref="PixelGroup.boxWholePrefab"/>。
        /// 运行时 <see cref="PixelGroup.SpawnBox"/> 与编辑器创建向导共用这一条规则，避免两边判定分叉；
        /// 没配整体预制体时返回 false → 退回按格拼接，不会让箱子变成看不见。
        /// </summary>
        public static bool ShouldUseWholePrefab(PixelGroup group, int colMin, int rowMin, int colMax, int rowMax)
        {
            return group != null && group.boxWholePrefab != null &&
                   (colMax - colMin + 1) == 2 && (rowMax - rowMin + 1) == 2;
        }

        private void Awake()
        {
            // 场景里已有（非运行时生成）的箱子不经过 BuildVisual，这里也刷一次总数显示
            UpdateCountText();
        }

        /// <summary>
        /// 计算箱子「紧邻基础容量」= 本体格子数 + 相邻有效格数（越界/墙体/管道/其它箱子本体不计数）。
        /// 相邻固定按上下左右 4 方向（不含四角），与开箱触发、落点候选口径一致。
        /// 注：开箱实际可用格还包括「连通空格」，故该值仅作编辑器参考/兜底，不再是运行时容量上限。
        /// </summary>
        public int ComputeCapacity() => ComputeCapacity(group, colMin, rowMin, colMax, rowMax);

        public static int ComputeCapacity(PixelGroup group, int colMin, int rowMin, int colMax, int rowMax)
        {
            int body = (colMax - colMin + 1) * (rowMax - rowMin + 1);
            if (group == null)
                return body;

            int adjacent = 0;
            // 上/下边（不含四角）
            for (int c = colMin; c <= colMax; c++)
            {
                CountIfEmpty(group, c, rowMin - 1, ref adjacent);
                CountIfEmpty(group, c, rowMax + 1, ref adjacent);
            }
            // 左/右列（不含四角）
            for (int r = rowMin; r <= rowMax; r++)
            {
                CountIfEmpty(group, colMin - 1, r, ref adjacent);
                CountIfEmpty(group, colMax + 1, r, ref adjacent);
            }
            return body + adjacent;
        }

        /// <summary>若该格在范围内且非障碍（墙/管/箱），则计入相邻有效格数。</summary>
        private static void CountIfEmpty(PixelGroup group, int c, int r, ref int adjacent)
        {
            if (!group.IsInRange(c, r))
                return;
            if (group.IsBlocked(c, r))
                return;
            adjacent++;
        }

        /// <summary>该格是否在箱子本体矩形内。</summary>
        public bool IsInBody(int c, int r)
        {
            return c >= colMin && c <= colMax && r >= rowMin && r <= rowMax;
        }

        /// <summary>枚举箱子本体矩形内的所有格子。</summary>
        public void EnumerateBody(List<Vector2Int> outList)
        {
            for (int r = rowMin; r <= rowMax; r++)
                for (int c = colMin; c <= colMax; c++)
                    outList.Add(new Vector2Int(c, r));
        }

        /// <summary>在 Scene 视图绘制箱子区域线框（编辑器预览 + 运行时定位辅助）。</summary>
        private void OnDrawGizmosSelected()
        {
            var pg = group != null ? group : GetComponentInParent<PixelGroup>();
            if (pg == null)
                return;

            Vector3 a = pg.GetLocalPosition(colMin, rowMin);
            Vector3 b = pg.GetLocalPosition(colMax, rowMax);
            Vector3 center = (a + b) * 0.5f;
            Vector3 size = new Vector3(
                Mathf.Abs(b.x - a.x) + pg.CellSizeX,
                0.2f,
                Mathf.Abs(b.z - a.z) + pg.CellSizeZ);

            Color old = Gizmos.color;
            Gizmos.color = new Color(1f, 0.6f, 0f, 0.6f);
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = pg.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(center, size);
            Gizmos.matrix = oldMatrix;
            Gizmos.color = old;
        }

        /// <summary>
        /// 拼接箱子视觉（角/边/中心 3 类预制体按格子）+ 生成隐藏 Pixel。
        /// 隐藏 Pixel 作为 PixelGroup 子物体（与普通 Pixel 同父级），哨兵坐标 gridX=gridZ=-1，初始隐藏。
        /// </summary>
        public void BuildVisual(PixelGroup pg, ColorConfig config)
        {
            group = pg;

            // 隐藏 Pixel 数量：以 min(capacity, colorIds.Length) 为准（§12 校验），并归一化 capacity
            int count = colorIds != null ? Mathf.Min(capacity, colorIds.Length) : 0;
            if (count != capacity)
                Debug.LogWarning("[BoxItem] 箱子容量 " + capacity + " 与 colorIds 数量 " +
                    (colorIds != null ? colorIds.Length : 0) + " 不一致，按较小值处理。");
            capacity = count;

            Vector3 appear = BoxCenterLocal() + new Vector3(0f, -jumpSpawnYOffset, 0f);

            for (int i = 0; i < capacity; i++)
            {
                int colorId = colorIds != null && i < colorIds.Length ? colorIds[i] : 0;
                var go = PrefabSpawner.Instantiate(pg.pixelPrefab, pg.transform);
                if (go == null)
                {
                    Debug.LogError("[BoxItem] pixelPrefab 为空，无法生成箱内隐藏 Pixel。");
                    continue;
                }
                go.name = "BoxPixel_" + rowMin + "_" + colMin + "_" + i;
                go.transform.localPosition = appear;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one * pg.unitSize;

                var item = go.GetComponent<PixelItem>();
                if (item == null)
                {
                    Debug.LogError("[BoxItem] pixelPrefab 缺少 PixelItem 组件，无法生成箱内隐藏 Pixel。");
                    Destroy(go);
                    continue;
                }

                item.gridX = -1;
                item.gridZ = -1;
                item.colorId = colorId;
                item.ApplyMaterial(config);
                item.SetClickable(false);
                go.SetActive(false);

                hiddenPixels.Add(item);
            }

            if (wholePrefab)
            {
                // 整体预制体：视觉就是本物体自己 —— 只把它摆到箱子中心，不拼接、**不改缩放**
                // （美术按实际尺寸制作；按格拼接那条路才需要乘 unitSize）。
                // 消失动画会把**所有直接子物体**弹掉；根物体（挂着 BoxItem）保留，
                // 因为 group.boxes 里还有引用。
                transform.localPosition = BoxCenterLocal();
                transform.localRotation = Quaternion.identity;

                for (int i = 0; i < transform.childCount; i++)
                    _visualPieces.Add(transform.GetChild(i).gameObject);
            }
            else
            {
                // 箱子视觉：3 类预制体按格子拼接（§6）
                for (int r = rowMin; r <= rowMax; r++)
                {
                    for (int c = colMin; c <= colMax; c++)
                    {
                        var prefab = ChoosePiecePrefab(c, r);
                        if (prefab == null)
                        {
                            Debug.LogWarning("[BoxItem] 箱子视觉预制体为空，跳过格子 (" + c + "," + r + ")。");
                            continue;
                        }
                        var piece = PrefabSpawner.Instantiate(prefab, transform);
                        if (piece == null)
                            continue;
                        piece.name = "BoxPiece_" + r + "_" + c;
                        piece.transform.localPosition = pg.GetLocalPosition(c, r);
                        piece.transform.localRotation = Quaternion.identity;
                        piece.transform.localScale = Vector3.one * pg.unitSize;
                        _visualPieces.Add(piece);
                    }
                }
            }

            UpdateCountText();
        }

        /// <summary>
        /// 刷新「箱内 Pixel 总数」显示（= <see cref="capacity"/>，也就是开箱阈值）。
        /// 开箱是一次性把内容全部释放，所以这个数在箱子存在期间是常量；**开箱那一刻由
        /// <see cref="HideCountText"/> 隐藏**，之后再没有需要显示它的时候。
        /// </summary>
        public void UpdateCountText()
        {
            if (countText == null)
                countText = GetComponentInChildren<Text>(true);
            if (countText == null)
                return;

            countText.text = capacity.ToString();
        }

        /// <summary>释放开始时让数字消失：盒子已经开了，容量这个数就没有意义了（同 <see cref="IceItem"/> 的融化隐藏口径）。</summary>
        private void HideCountText()
        {
            if (countText == null)
                countText = GetComponentInChildren<Text>(true);
            if (countText != null)
                countText.enabled = false;
        }

        /// <summary>该格用哪个视觉预制体（角/边/中心，按 §6 规则）。</summary>
        private GameObject ChoosePiecePrefab(int c, int r)
        {
            bool isLeft = c == colMin;
            bool isRight = c == colMax;
            bool isTop = r == rowMin;
            bool isBottom = r == rowMax;

            if ((isLeft || isRight) && (isTop || isBottom))
                return cornerPrefab;
            if (isLeft || isRight || isTop || isBottom)
                return edgePrefab;
            return centerPrefab;
        }

        /// <summary>箱子矩形中心在 PixelGroup 本地空间的位置（外跳出现点基准）。</summary>
        private Vector3 BoxCenterLocal()
        {
            if (group == null)
                return Vector3.zero;
            Vector3 a = group.GetLocalPosition(colMin, rowMin);
            Vector3 b = group.GetLocalPosition(colMax, rowMax);
            return (a + b) * 0.5f;
        }

        /// <summary>
        /// 尝试开箱：收集候选格（本体 + 相邻 4 方向 + 连通），满足触发条件（可用格 ≥ 容量）则
        /// 整体规划分配位置（同色像素各自连通），立即把释放的 Pixel 落到 grid
        /// （供多箱串行判定与后续逻辑看到），并启动两段开箱动画。返回是否实际开箱。
        /// </summary>
        public bool TryOpen()
        {
            if (opened || group == null)
                return false;

            // 无内容：直接清障碍 + 消失，无需释放动画
            if (hiddenPixels.Count == 0)
            {
                opened = true;
                HideCountText();             // 释放开始：数字（容量）就没有意义了
                group.OnBoxOpened(this);
                DisappearVisual();
                group.OnBoxReleaseFinished(this);
                return true;
            }

            // 1. 收集候选格：本体 + 相邻（上下左右 4 方向）+ 连通（相邻出发 4 方向 BFS 的空格）
            var body = new List<Vector2Int>();
            EnumerateBody(body);
            var adjacent = CollectAdjacentEmpty();
            var connected = CollectConnectedEmpty(adjacent);

            // 优先级分数：本体 0 < 相邻 1 < 连通 2+距离（越小越优先）
            var score = new Dictionary<Vector2Int, int>();
            foreach (var c in body)
                score[c] = 0;
            foreach (var c in adjacent)
                score[c] = 1;
            foreach (var pair in connected)
                score[pair.cell] = 2 + pair.distance;

            int available = body.Count + adjacent.Count + connected.Count;
            if (debugOpenLog)
            {
                Debug.Log("[Box] 开箱判定 " + name + "：本体=" + body.Count +
                    " 相邻=" + adjacent.Count +
                    " 连通=" + connected.Count +
                    " 可用=" + available + " 容量=" + capacity +
                    " 隐藏=" + hiddenPixels.Count +
                    (available >= capacity ? " → 开箱" : " → 空间不足，继续等待"));
            }
            if (available < capacity)
                return false;   // 空间不足，继续等待

            // 2. 标记已开 + 清障碍（增量计数由 group 处理）
            opened = true;
            HideCountText();     // 释放开始：数字（容量）就没有意义了，先它一步消失
            group.OnBoxOpened(this);

            // 3. 整体规划：把隐藏像素按颜色分组，每种颜色分配到一组 4 方向连通的候选格，
            //    确保释放后同色像素各自连通（优先级：本体 > 相邻 > 连通距离）。
            var allCells = new List<Vector2Int>(available);
            allCells.AddRange(body);
            allCells.AddRange(adjacent);
            foreach (var pair in connected)
                allCells.Add(pair.cell);

            var pixels = new List<PixelItem>(hiddenPixels);
            var assignments = PlanAssignments(pixels, score, allCells);

            foreach (var assignment in assignments)
            {
                var pixel = assignment.pixel;
                var cell = assignment.cell;
                pixel.gridX = cell.x;
                pixel.gridZ = cell.y;
                pixel.group = group;
                if (group.grid != null && group.IsInRange(cell.x, cell.y))
                    group.grid[cell.x, cell.y] = pixel;   // 立即占格，供后续箱子/逻辑看到
                pixel.SetClickable(false);                 // 动画期间不可交互（就位后统一恢复可点击）
                pixel.placing = true;                      // 动画期间不站起、保持 root 初始位置（就位后 MarkPlaced + RefreshExposed 统一激活）
                pixel.walkableDuringExtraction = true;      // 本次点击开箱导致的占格，提取寻路时视为可走（结束后由 CrowdBufferZone 清除）
                hiddenPixels.Remove(pixel);
            }

            // 4. 启动两段开箱动画
            StartCoroutine(OpenRoutine(assignments));
            return true;
        }

        /// <summary>收集直接相邻空格（仅上下左右 4 方向，不含四角）。</summary>
        private List<Vector2Int> CollectAdjacentEmpty()
        {
            var result = new List<Vector2Int>();
            int[] dx4 = { 1, -1, 0, 0 };
            int[] dz4 = { 0, 0, 1, -1 };

            for (int r = rowMin; r <= rowMax; r++)
            {
                for (int c = colMin; c <= colMax; c++)
                {
                    for (int d = 0; d < 4; d++)
                    {
                        int nx = c + dx4[d];
                        int nz = r + dz4[d];
                        if (!group.IsInRange(nx, nz))
                            continue;
                        if (IsInBody(nx, nz))
                            continue;
                        if (!group.IsEmpty(nx, nz))
                            continue;
                        var cell = new Vector2Int(nx, nz);
                        if (!result.Contains(cell))
                            result.Add(cell);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 收集「连通空格」：从相邻格出发、只经上下左右 4 方向的空 BFS 扩散可达的空格
        /// （不含本体与相邻格本身）。返回每个格及其到相邻格的最短距离（0 起跳）。
        /// </summary>
        private List<(Vector2Int cell, int distance)> CollectConnectedEmpty(IReadOnlyList<Vector2Int> adjacent)
        {
            var result = new List<(Vector2Int, int)>();
            if (group == null)
                return result;

            var body = new HashSet<Vector2Int>();
            for (int r = rowMin; r <= rowMax; r++)
                for (int c = colMin; c <= colMax; c++)
                    body.Add(new Vector2Int(c, r));

            var adjSet = new HashSet<Vector2Int>(adjacent);
            var visited = new HashSet<Vector2Int>(adjSet);
            var queue = new Queue<(Vector2Int cell, int dist)>();
            foreach (var a in adjacent)
                queue.Enqueue((a, 0));

            int[] dx = { 1, -1, 0, 0 };
            int[] dz = { 0, 0, 1, -1 };

            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                for (int d = 0; d < 4; d++)
                {
                    int nx = cur.cell.x + dx[d];
                    int nz = cur.cell.y + dz[d];
                    var nb = new Vector2Int(nx, nz);
                    if (!group.IsInRange(nx, nz))
                        continue;
                    if (body.Contains(nb) || adjSet.Contains(nb))
                        continue;
                    if (visited.Contains(nb))
                        continue;
                    if (!group.IsEmpty(nx, nz))
                        continue;
                    visited.Add(nb);
                    result.Add((nb, cur.dist + 1));
                    queue.Enqueue((nb, cur.dist + 1));
                }
            }
            return result;
        }

        /// <summary>
        /// 整体规划释放落点：把隐藏像素按颜色分组，每种颜色分配到一组 4 方向连通的候选格，
        /// 使释放后同色像素各自连通。颜色组按数量降序（大块优先占高优先级格）。
        /// 先回溯搜索「所有颜色各自连通」的完整解（受预算约束），避免贪心先把某块贪掉、
        /// 导致其余候选格被割裂、本该连通的颜色被截断；无解/超预算时退回贪心兜底。
        /// </summary>
        private List<(PixelItem pixel, Vector2Int cell)> PlanAssignments(
            List<PixelItem> pixels,
            Dictionary<Vector2Int, int> score,
            List<Vector2Int> allCells)
        {
            // 颜色分组（保持 colorIds 相对顺序）
            var groups = new Dictionary<int, List<PixelItem>>();
            foreach (var p in pixels)
            {
                if (!groups.TryGetValue(p.colorId, out var list))
                {
                    list = new List<PixelItem>();
                    groups[p.colorId] = list;
                }
                list.Add(p);
            }
            var colorGroups = new List<List<PixelItem>>(groups.Values);
            colorGroups.Sort((a, b) =>
            {
                int c = b.Count.CompareTo(a.Count);   // 数量降序：大块优先占高优先级格
                if (c != 0)
                    return c;
                return a[0].colorId.CompareTo(b[0].colorId);   // 平局按颜色值升序，保证确定性
            });

            // 1. 回溯求「所有颜色各自连通」的完整解（若存在，优先于贪心）
            var remaining = new HashSet<Vector2Int>(allCells);
            var solution = new List<Vector2Int>[colorGroups.Count];
            int budget = BacktrackBudget;
            if (SolveConnected(colorGroups, 0, remaining, score, solution, ref budget))
            {
                var result = new List<(PixelItem, Vector2Int)>();
                for (int gi = 0; gi < colorGroups.Count; gi++)
                {
                    var g = colorGroups[gi];
                    var block = solution[gi];
                    for (int i = 0; i < g.Count && i < block.Count; i++)
                        result.Add((g[i], block[i]));
                }
                return result;
            }

            // 2. 兜底：贪心（可能因障碍割裂而不连通）
            return PlanAssignmentsGreedy(colorGroups, score, allCells);
        }

        /// <summary>贪心兜底：每种颜色从剩余候选格按优先级 BFS 生长连通块；被障碍割裂无法连通容纳时按优先级补齐（可能不连通）。</summary>
        private List<(PixelItem pixel, Vector2Int cell)> PlanAssignmentsGreedy(
            List<List<PixelItem>> colorGroups,
            Dictionary<Vector2Int, int> score,
            List<Vector2Int> allCells)
        {
            var result = new List<(PixelItem, Vector2Int)>();
            var remaining = new HashSet<Vector2Int>(allCells);

            foreach (var groupPixels in colorGroups)
            {
                int n = groupPixels.Count;
                var block = GrowBlock(remaining, score, n);
                if (block.Count < n)
                {
                    var blockSet = new HashSet<Vector2Int>(block);
                    var extra = new List<Vector2Int>();
                    foreach (var c in remaining)
                        if (!blockSet.Contains(c))
                            extra.Add(c);
                    extra.Sort(CompareByScoreThenRowCol(score));
                    for (int i = 0; i < extra.Count && block.Count < n; i++)
                        block.Add(extra[i]);
                    Debug.LogWarning("[Box] 开箱 " + name + "：颜色 " + groupPixels[0].colorId +
                        " 需要 " + n + " 格，剩余候选格被障碍割裂无法连通容纳，已按优先级补齐（可能不连通）。");
                }
                for (int i = 0; i < groupPixels.Count && i < block.Count; i++)
                {
                    result.Add((groupPixels[i], block[i]));
                    remaining.Remove(block[i]);
                }
            }
            return result;
        }

        /// <summary>
        /// 回溯搜索：把每个颜色组分配到一组 4 方向连通、互不重叠的候选格（数量 = 该颜色像素数）。
        /// 颜色按数量降序处理（大块优先占高优先级格）；每种颜色按优先级从高到低枚举候选连通块并逐一尝试。
        /// 找到完整解返回 true（solution[gi] = 该颜色的落点列表）；超预算或无解返回 false（调用方走贪心兜底）。
        /// </summary>
        private bool SolveConnected(
            List<List<PixelItem>> colorGroups,
            int gi,
            HashSet<Vector2Int> remaining,
            Dictionary<Vector2Int, int> score,
            List<Vector2Int>[] solution,
            ref int budget)
        {
            if (gi >= colorGroups.Count)
                return true;
            if (budget <= 0)
                return false;
            budget--;

            int n = colorGroups[gi].Count;
            if (n <= 0)
            {
                solution[gi] = new List<Vector2Int>();
                return SolveConnected(colorGroups, gi + 1, remaining, score, solution, ref budget);
            }

            var blocks = new List<List<Vector2Int>>();
            EnumerateConnectedBlocks(remaining, score, n, ref budget, blocks);

            foreach (var block in blocks)
            {
                foreach (var c in block)
                    remaining.Remove(c);
                solution[gi] = block;
                if (SolveConnected(colorGroups, gi + 1, remaining, score, solution, ref budget))
                    return true;
                foreach (var c in block)
                    remaining.Add(c);
                solution[gi] = null;
                if (budget <= 0)
                    return false;
            }
            return false;
        }

        /// <summary>
        /// 枚举 remaining 中大小为 n 的 4 方向连通块（以块内优先级最高格为种子、只朝优先级更低的格子生长，保证每个块唯一枚举）。
        /// 结果按优先级（分数和升序，平局按规范键字典序）排序，最多 MaxBlocksPerColor 个，受 budget 约束。
        /// </summary>
        private void EnumerateConnectedBlocks(
            HashSet<Vector2Int> remaining,
            Dictionary<Vector2Int, int> score,
            int n,
            ref int budget,
            List<List<Vector2Int>> outBlocks)
        {
            outBlocks.Clear();
            if (n <= 0 || remaining.Count < n)
                return;

            var cmp = CompareByScoreThenRowCol(score);
            var seeds = new List<Vector2Int>(remaining);
            seeds.Sort(cmp);

            var block = new List<Vector2Int>();
            var inBlock = new HashSet<Vector2Int>();
            var seen = new HashSet<string>();   // 去重：同一集合可能被不同生长顺序枚举出来

            foreach (var seed in seeds)
            {
                if (budget <= 0 || outBlocks.Count >= MaxBlocksPerColor)
                    break;
                block.Add(seed);
                inBlock.Add(seed);
                GrowConnectedRec(remaining, score, cmp, n, seed, block, inBlock, seen, ref budget, outBlocks);
                inBlock.Clear();
                block.Clear();
            }

            outBlocks.Sort((a, b) =>
            {
                int sa = 0, sb = 0;
                foreach (var c in a)
                    sa += ScoreOf(score, c);
                foreach (var c in b)
                    sb += ScoreOf(score, c);
                if (sa != sb)
                    return sa.CompareTo(sb);
                return string.CompareOrdinal(CanonicalKey(a), CanonicalKey(b));
            });
        }

        /// <summary>
        /// 递归生长连通块：把当前块的可扩展邻居（在 remaining、不在块内、且优先级低于 seed）按优先级排序逐个尝试。
        /// 达到大小 n 时记录（去重）；受 budget 与 MaxBlocksPerColor 约束。
        /// </summary>
        private void GrowConnectedRec(
            HashSet<Vector2Int> remaining,
            Dictionary<Vector2Int, int> score,
            System.Comparison<Vector2Int> cmp,
            int n,
            Vector2Int seed,
            List<Vector2Int> block,
            HashSet<Vector2Int> inBlock,
            HashSet<string> seen,
            ref int budget,
            List<List<Vector2Int>> outBlocks)
        {
            if (budget <= 0 || outBlocks.Count >= MaxBlocksPerColor)
                return;
            budget--;

            if (block.Count == n)
            {
                var key = CanonicalKey(block);
                if (seen.Add(key))
                    outBlocks.Add(new List<Vector2Int>(block));
                return;
            }

            // 收集可扩展邻居（去重 + 仅保留优先级低于 seed 的格子，保证 seed 是块内最优、避免跨种子重复）
            var candidateSet = new HashSet<Vector2Int>();
            int[] dx = { 1, -1, 0, 0 };
            int[] dz = { 0, 0, 1, -1 };
            foreach (var c in block)
            {
                for (int d = 0; d < 4; d++)
                {
                    var nb = new Vector2Int(c.x + dx[d], c.y + dz[d]);
                    if (!remaining.Contains(nb) || inBlock.Contains(nb))
                        continue;
                    if (cmp(seed, nb) >= 0)
                        continue;
                    candidateSet.Add(nb);
                }
            }

            var candidates = new List<Vector2Int>(candidateSet);
            candidates.Sort(cmp);

            foreach (var cand in candidates)
            {
                block.Add(cand);
                inBlock.Add(cand);
                GrowConnectedRec(remaining, score, cmp, n, seed, block, inBlock, seen, ref budget, outBlocks);
                inBlock.Remove(cand);
                block.RemoveAt(block.Count - 1);
                if (budget <= 0 || outBlocks.Count >= MaxBlocksPerColor)
                    break;
            }
        }

        /// <summary>连通块去重/排序用的规范键：按 (x, y) 升序拼接。</summary>
        private static string CanonicalKey(List<Vector2Int> block)
        {
            var arr = new List<Vector2Int>(block);
            arr.Sort((a, b) =>
            {
                int c = a.x.CompareTo(b.x);
                return c != 0 ? c : a.y.CompareTo(b.y);
            });
            var sb = new System.Text.StringBuilder();
            foreach (var c in arr)
                sb.Append(c.x).Append(',').Append(c.y).Append(';');
            return sb.ToString();
        }

        /// <summary>从剩余候选格中按优先级 BFS 生长出至多 n 个 4 方向连通的格子（BFS 前缀保证连通），优先占用 score 低的格子。</summary>
        private List<Vector2Int> GrowBlock(HashSet<Vector2Int> remaining, Dictionary<Vector2Int, int> score, int n)
        {
            var result = new List<Vector2Int>();
            if (n <= 0 || remaining.Count == 0)
                return result;

            // 种子：remaining 中 score 最小（平局 row/col 小），确定性
            Vector2Int seed = default;
            bool found = false;
            int bestScore = int.MaxValue;
            foreach (var c in remaining)
            {
                int s = ScoreOf(score, c);
                if (!found || s < bestScore || (s == bestScore && (c.y < seed.y || (c.y == seed.y && c.x < seed.x))))
                {
                    found = true;
                    bestScore = s;
                    seed = c;
                }
            }
            if (!found)
                return result;

            var visited = new HashSet<Vector2Int>();
            var frontier = new SortedSet<(int s, int r, int c)>();
            int[] dx = { 1, -1, 0, 0 };
            int[] dz = { 0, 0, 1, -1 };

            void Push(Vector2Int cell)
            {
                if (!remaining.Contains(cell) || visited.Contains(cell))
                    return;
                frontier.Add((ScoreOf(score, cell), cell.y, cell.x));
            }

            Push(seed);
            while (result.Count < n && frontier.Count > 0)
            {
                var top = frontier.Min;
                frontier.Remove(top);
                var cur = new Vector2Int(top.c, top.r);
                if (!remaining.Contains(cur) || !visited.Add(cur))
                    continue;
                result.Add(cur);
                for (int d = 0; d < 4; d++)
                    Push(new Vector2Int(cur.x + dx[d], cur.y + dz[d]));
            }
            return result;
        }

        /// <summary>取候选格的优先级分数；未记录（理论上不应出现）的按最低优先级处理。</summary>
        private static int ScoreOf(Dictionary<Vector2Int, int> score, Vector2Int c)
            => score.TryGetValue(c, out var v) ? v : int.MaxValue;

        /// <summary>按优先级分数升序（平局 row/col 升序）排序的比较器，供退化兜底用。</summary>
        private static System.Comparison<Vector2Int> CompareByScoreThenRowCol(Dictionary<Vector2Int, int> score)
        {
            return (Vector2Int a, Vector2Int b) =>
            {
                int sa = ScoreOf(score, a);
                int sb = ScoreOf(score, b);
                if (sa != sb)
                    return sa.CompareTo(sb);
                int rc = a.y.CompareTo(b.y);
                if (rc != 0)
                    return rc;
                return a.x.CompareTo(b.x);
            };
        }

        /// <summary>确定性排序：先按 row（排）升序，同排再按 col（列）升序（从左到右）。</summary>
        private static int CompareByRowCol(Vector2Int a, Vector2Int b)
        {
            int rc = a.y.CompareTo(b.y);
            if (rc != 0)
                return rc;
            return a.x.CompareTo(b.x);
        }

        /// <summary>
        /// 两段开箱动画（动画期间 Pixel 不可交互、保持 root 初始位置）：
        /// 阶段一：本体外 Pixel 按 (row, col) 顺序逐个在**箱子中心下方**出现并 Jump（间隔 jumpStartInterval）；
        /// 阶段二：箱子开始**匀速下沉**；本体内 Pixel 用**同一套出现方式与节奏**（同一个起跳点、
        ///         同样的下方偏移 + Jump + 同一个间隔）接着冒出来；
        /// 阶段三：最后一个 Jump 落地后统一 MarkPlaced + 恢复可点击 + RefreshExposed（判定连通性 + 站起）。
        ///
        /// **两段共用同一个起跳间隔、中间不额外等待**，所以整体节奏是连续的 —— 本体内的像素不会再
        /// 「一次性全冒出来 + 直线升位」，听起来、看起来都与本体外一致。
        /// </summary>
        private IEnumerator OpenRoutine(List<(PixelItem pixel, Vector2Int cell)> assignments)
        {
            var external = new List<(PixelItem pixel, Vector2Int cell)>();
            var body = new List<(PixelItem pixel, Vector2Int cell)>();
            for (int i = 0; i < assignments.Count; i++)
            {
                var a = assignments[i];
                if (IsInBody(a.cell.x, a.cell.y))
                    body.Add(a);
                else
                    external.Add(a);
            }
            external.Sort((a, b) => CompareByRowCol(a.cell, b.cell));
            body.Sort((a, b) => CompareByRowCol(a.cell, b.cell));

            // 阶段一：本体外 Pixel 按 (row, col) 顺序逐个出现并 Jump（箱子保持可见）
            for (int i = 0; i < external.Count; i++)
            {
                SpawnJump(external[i].pixel, external[i].cell);
                yield return new WaitForSeconds(jumpStartInterval);
            }

            // 阶段二：箱子开始下沉消失；本体内 Pixel 用同样的方式与节奏接着冒出来
            DisappearVisual();
            for (int i = 0; i < body.Count; i++)
            {
                SpawnJump(body[i].pixel, body[i].cell);
                yield return new WaitForSeconds(jumpStartInterval);
            }

            // 等最后一个 Jump 落地（最后一次起跳后还需 jumpDuration 减去已经等过的一个间隔）
            if (external.Count + body.Count > 0)
            {
                float wait = Mathf.Max(0f, jumpDuration - jumpStartInterval);
                if (wait > 0f)
                    yield return new WaitForSeconds(wait);
            }

            FinalizeRelease(assignments);
        }

        /// <summary>全部动画结束：统一 MarkPlaced + 恢复可点击 + RefreshExposed（判定连通性 + 站起），并解除箱体释放计数。</summary>
        private void FinalizeRelease(List<(PixelItem pixel, Vector2Int cell)> assignments)
        {
            for (int i = 0; i < assignments.Count; i++)
            {
                var pixel = assignments[i].pixel;
                if (pixel == null || group == null || group.grid == null)
                    continue;
                // 已被后续匹配移出网格：不再处理（其 placing 标记无副作用，交由匹配流程接管）
                if (!group.IsInRange(pixel.gridX, pixel.gridZ) || group.grid[pixel.gridX, pixel.gridZ] != pixel)
                    continue;
                pixel.MarkPlaced();
                pixel.SetClickable(true);
            }

            if (group != null)
            {
                group.RefreshExposed();
                group.OnBoxReleaseFinished(this);
            }
        }

        /// <summary>
        /// 一个 Pixel 的「出现并跳到位」：从**箱子中心**下方 <see cref="jumpSpawnYOffset"/> 处出现，
        /// 用同一套 Jump 参数（<see cref="jumpPower"/> / <see cref="jumpCount"/> / <see cref="jumpDuration"/>）
        /// 跳到目标格。本体外与本体内**起跳点也完全一致**（都在箱子中心下方），只有目标格不同。
        /// </summary>
        private void SpawnJump(PixelItem pixel, Vector2Int cell)
        {
            if (pixel == null || group == null)
                return;

            Vector3 target = group.GetLocalPosition(cell.x, cell.y);
            Vector3 from = BoxCenterLocal() + new Vector3(0f, -jumpSpawnYOffset, 0f);

            pixel.transform.localPosition = from;
            pixel.transform.localRotation = Quaternion.identity;
            pixel.gameObject.SetActive(true);
            pixel.transform.DOLocalJump(target, jumpPower, jumpCount, jumpDuration);
        }

        /// <summary>
        /// 箱子视觉消失：**匀速下沉** <see cref="disappearDropDistance"/>，沉完销毁。
        /// （原来是「弹一下再缩小 + 同时上升」，已按需求换成下沉。）
        ///
        /// <see cref="holdStaticChild"/> 指定的子物体要在下沉开始后的 <see cref="holdStaticDuration"/> 秒里
        /// **世界坐标不动**，所以脚本在同一时间给它一条**反向抬升**的补间把这一段的沉降量补掉
        /// （下沉是匀速的，补偿量 = 下沉速度 × 静止时长）；这段时间过去后它不再反抗，就跟着箱子一起沉。
        ///
        /// 抬升方向取**它父坐标系里的「世界 up」**（不是它自己的 +Y），所以子物体自身带旋转也照样精确静止。
        /// 万一它正好就是某个拼接块本身，则不能「既下沉又反向补」（同一 transform 上两条补间互相打断），
        /// 改成让那一块**延后 holdStaticDuration 秒再开始沉** —— 世界静止的效果一样。
        /// </summary>
        private void DisappearVisual()
        {
            float dropDuration = Mathf.Max(0.0001f, disappearDropDuration);
            float hold = Mathf.Clamp(holdStaticDuration, 0f, dropDuration);
            Vector3 down = Vector3.up * disappearDropDistance;

            Transform held = holdStaticChild;
            bool heldIsPiece = held != null && _visualPieces.Contains(held.gameObject);

            for (int i = 0; i < _visualPieces.Count; i++)
            {
                var piece = _visualPieces[i];
                if (piece == null)
                    continue;

                var tr = piece.transform;
                float delay = (heldIsPiece && tr == held) ? hold : 0f;   // 被引用的那块延后下沉
                float moveDuration = dropDuration - delay;

                if (moveDuration <= 0f)
                {
                    // 整段下沉都要求静止（静止时长 ≥ 下沉时长）：这一块就不动了，到点直接销毁
                    DOVirtual.DelayedCall(dropDuration, () =>
                    {
                        if (piece != null)
                            Destroy(piece);
                    });
                    continue;
                }

                tr.DOLocalMove(tr.localPosition - down, moveDuration)
                    .SetDelay(delay)
                    .SetEase(Ease.Linear)
                    .OnComplete(() =>
                    {
                        if (piece != null)
                            Destroy(piece);
                    });
            }

            if (held != null && !heldIsPiece && hold > 0f)
            {
                float speed = disappearDropDistance / dropDuration;
                Vector3 upInParent = held.parent != null
                    ? held.parent.InverseTransformDirection(Vector3.up)
                    : Vector3.up;

                held.DOLocalMove(held.localPosition + upInParent * (speed * hold), hold)
                    .SetEase(Ease.Linear);
            }

            _visualPieces.Clear();
        }
    }
}
