using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CrowdMatch
{
    /// <summary>
    /// 管道单位：占据一个像素格子（points[0]），沿轨迹折线（points[0]→points[1]→…）在轨道清空后逐波生成同色像素。
    /// 轨迹端点使用网格坐标（Vector2：x = 列 col，y = 行 row），首点为管道自身所在格；
    /// 轨道 = 折线经过的所有格（去首点、含各段中间格），像素布满整条路径而非仅端点。
    /// 开局不立即生成：当轨道格上的像素全部移走后，管道生成下一波（colors[waveIndex]）；
    /// 每个像素从管道格以 scale=0 生成，y 从 Body Mesh 高度 InQuad 降到标准高度，沿轨道蛇形前进并平滑缩放到 unitSize；colors 耗尽后停止。
    /// </summary>
    public class PipeItem : MonoBehaviour
    {
        [Header("轨迹")]
        [Tooltip("轨迹端点（网格坐标）。points[0] = 管道格；points[1..] = 轨道格（每波像素的目标格）")]
        public List<Vector2> points = new List<Vector2>();

        [Tooltip("每波颜色（第 i 波用 colors[i]）。耗尽后不再生成。")]
        public List<int> colors = new List<int>();

        [Header("生成动画")]
        [Tooltip("像素每走一格时长（秒）")]
        public float cellMoveDuration = 0.3f;

        [Tooltip("（已弃用）旧版相邻像素生成间隔；流式补位按 cellMoveDuration 逐格推进，此值不再使用")]
        public float spawnInterval = 0.3f;

        [Header("调试")]
        [Tooltip("开启后输出「蛇头合法前进时」的蛇头上一格/前进格，以及所有寻路块当前格/目标格（定位完可关闭）")]
        public bool debugLog = true;

        [Tooltip("（仅 Editor）开启后，每次打印上述 step 日志时暂停编辑器播放，便于逐帧查看场景/Inspector 状态")]
        public bool pauseOnLog = false;

        [Header("显示")]
        [Tooltip("管道本体网格 Transform（其本地 +Z 将朝向 points[0]→points[1] 方向）；留空自动取子物体首个带 MeshFilter 的物体")]
        public Transform bodyMesh;

        [Tooltip("剩余波次数字（UI Text，留空自动从子物体查找）")]
        public Text waveCountText;

        [Tooltip("剩余波次数字的整体偏移（世界 xz：x = 世界 X，y = 世界 Z）。" +
                 "**只有管道朝向左右两侧**（轨迹沿列方向，即 ±X）时才应用，竖直朝向时归零 —— " +
                 "所以把 points 从横向改成竖向，数字会回到预制体原位，不会粘着旧偏移。\n" +
                 "x 按朝向**镜像**：按「朝右」填正数，朝左时自动取负，数字始终落在外侧同一侧；z 分量不镜像")]
        public Vector2 waveCountTextOffset = Vector2.zero;

        [Tooltip("勾选后，剩余波次数字按 ColorConfig 的字体颜色 / 描边颜色显示（按下一波颜色 ID 索引）")]
        public bool useConfigTextColor;

        [Tooltip("显示下一颜色的 Renderer + 材质槽位列表；空则不显示")]
        public List<NextColorIndicator> nextColorIndicators = new List<NextColorIndicator>();

        [Serializable]
        public class NextColorIndicator
        {
            [Tooltip("需要替换材质的 Renderer")]
            public Renderer renderer;

            [Tooltip("要替换的材质槽位下标（Renderer.materials 数组的 index）")]
            public int materialIndex;

            [Tooltip("颜色耗尽时是否隐藏该 Renderer（不显示最后一波颜色）")]
            public bool hideWhenEmpty;

            [Tooltip("默认材质：颜色耗尽且不隐藏时，若非空则换回该材质（留空则保持最后一波颜色）")]
            public Material defaultMaterial;
        }

        [System.NonSerialized] public PixelGroup group;

        [System.NonSerialized] private int _waveIndex;   // 已生成波数（下一波用 colors[_waveIndex]）
        [System.NonSerialized] private bool _spawning;

        /// <summary>剩余波次数字父物体**相对管道根**的原始位置（局部坐标）：只在第一次应用偏移时记一次，
        /// 之后一切以它为基准重算 —— 反复调朝向 / 反复调偏移量都不会叠加，管道根被拖动时数字也跟着走。
        ///
        /// **必须序列化**：它记的是「原位」，而应用偏移会就地改掉物体的位置。只放 NonSerialized 的话，
        /// 编辑器一次脚本重载就会把「已偏移后的位置」当成原位，再偏一次 → 越改越远
        /// （<see cref="IceItem"/> 的计数文字基准踩过同一个坑，那边也是靠序列化解决的）。</summary>
        [SerializeField, HideInInspector] private Vector3 _textBaseLocal;
        [SerializeField, HideInInspector] private bool _textBaseCaptured;

        /// <summary>蛇形生成中，蛇当前占据的格子（蛇头→蛇尾顺序，含蛇头正在前往的格子）。仅 IsReleasing 期间有效。</summary>
        [System.NonSerialized] public List<Vector2Int> snakeCells = new List<Vector2Int>();

        /// <summary>所属 PixelGroup（惰性：先读运行时赋值，为空则向上查找）。</summary>
        public PixelGroup Group => group != null ? group : (group = GetComponentInParent<PixelGroup>());

        /// <summary>网格坐标（浮点）就近取整为格子坐标。</summary>
        public static Vector2Int ToCell(Vector2 p)
        {
            return new Vector2Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y));
        }

        /// <summary>管道自身所在格（points[0]）。</summary>
        public static Vector2Int GetPipeCell(IReadOnlyList<Vector2> points)
        {
            return points != null && points.Count > 0
                ? ToCell(points[0])
                : new Vector2Int(-1, -1);
        }

        /// <summary>是否还有未释放的波次（colors 未耗尽）。</summary>
        public bool HasRemainingWaves => colors != null && _waveIndex < colors.Count;

        /// <summary>是否正在释放一波（蛇形生成动画进行中，_spawning）。</summary>
        public bool IsReleasing => _spawning;

        /// <summary>该格是否属于管道覆盖范围（管道自身格 + 轨道格）。供暴露判定把管道覆盖格视为阻挡。</summary>
        public bool CoversCell(int col, int row)
        {
            if (points == null || points.Count < 1)
                return false;
            if (GetPipeCell(points) == new Vector2Int(col, row))
                return true;
            if (points.Count < 2)
                return false;

            Vector2Int prev = ToCell(points[0]);
            for (int i = 1; i < points.Count; i++)
            {
                Vector2Int cur = ToCell(points[i]);
                int steps = Mathf.Max(Mathf.Abs(cur.x - prev.x), Mathf.Abs(cur.y - prev.y));
                int dx = System.Math.Sign(cur.x - prev.x);
                int dy = System.Math.Sign(cur.y - prev.y);
                for (int s = 1; s <= steps; s++)
                {
                    if (prev.x + dx * s == col && prev.y + dy * s == row)
                        return true;
                }
                prev = cur;
            }
            return false;
        }

        /// <summary>轨道格数量 = 折线经过的所有格（去首点、去重、仅限 [columns × totalRows] 范围内）。</summary>
        public static int CountTrackCells(IReadOnlyList<Vector2> points, int columns, int totalRows)
        {
            if (points == null || points.Count < 2)
                return 0;
            var cells = new List<Vector2Int>();
            CollectTrackCells(points, columns, totalRows, cells);
            return cells.Count;
        }

        /// <summary>
        /// 轨道格 = 折线经过的所有格（去首点、去重、仅限网格范围内），按路径顺序（近管道 → 远）。
        /// **唯一定义的走法**：实例版 <see cref="TrackCells"/> 与 <see cref="CountTrackCells"/> 都走这里，
        /// 编辑器只拿关卡 JSON（没有 PixelGroup）时也能算，用于查每格的倍乘门倍率。
        /// </summary>
        public static void CollectTrackCells(IReadOnlyList<Vector2> points, int columns, int totalRows, List<Vector2Int> outCells)
        {
            if (outCells == null)
                return;
            outCells.Clear();
            if (points == null || points.Count < 2)
                return;

            var seen = new HashSet<Vector2Int>();
            Vector2Int prev = ToCell(points[0]);
            for (int i = 1; i < points.Count; i++)
            {
                Vector2Int cur = ToCell(points[i]);
                int steps = Mathf.Max(Mathf.Abs(cur.x - prev.x), Mathf.Abs(cur.y - prev.y));
                int dx = System.Math.Sign(cur.x - prev.x);
                int dy = System.Math.Sign(cur.y - prev.y);
                for (int s = 1; s <= steps; s++)   // s=0 即 prev（段起点，已计入或为管道格）
                {
                    var c = new Vector2Int(prev.x + dx * s, prev.y + dy * s);
                    if (c.x < 0 || c.x >= columns || c.y < 0 || c.y >= totalRows)
                        continue;
                    if (seen.Add(c))
                        outCells.Add(c);
                }
                prev = cur;
            }
        }

        /// <summary>轨道格 = 折线经过的所有格（去首点、去重、仅限网格范围内），按路径顺序（近管道 → 远）。</summary>
        public List<Vector2Int> TrackCells()
        {
            var result = new List<Vector2Int>();
            var g = Group;
            if (points == null || points.Count < 2 || g == null)
                return result;
            CollectTrackCells(points, g.columns, g.TotalRows, result);
            return result;
        }

        /// <summary>轨道格数量（每波生成的像素数）。</summary>
        public int TrackCellCount() => TrackCells().Count;

        /// <summary>轨道上是否已无像素（所有轨道格 grid 均为空）。</summary>
        public bool TrackEmpty()
        {
            var g = Group;
            if (g == null || g.grid == null)
                return false;
            foreach (var c in TrackCells())
            {
                if (g.GetItem(c.x, c.y) != null)
                    return false;
            }
            return true;
        }

        private void Awake()
        {
            if (waveCountText == null)
                waveCountText = GetComponentInChildren<Text>(true);
        }

        private void Start()
        {
            if (!Application.isPlaying)
                return;
            OrientBody();
            ApplyNextColorForWave();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 编辑器里改 points 后实时刷新朝向（Play 模式交给 Start，避免运行时误触）
            if (!Application.isPlaying)
                OrientBody();
        }
#endif

        private void Update()
        {
            if (!Application.isPlaying)
                return;
            if (Group == null)
                return;
            if (_spawning)
                return;
            if (_waveIndex >= colors.Count)
                return;
            // 上一波已被完全匹配（轨道格 grid 全空）即触发补位；具体每格能否进入由 SpawnWave 的蛇头逐格等待决定，
            // 不必等整条轨道 / 整组提取全部结束，蛇头前方一格空闲即可前进。
            if (!TrackEmpty())
                return;
            StartCoroutine(SpawnWave(colors[_waveIndex]));
        }

        /// <summary>等待某格不再被提取中的像素占用（停靠/进入视为占用，正在离开不算）。无缓冲区时立即返回。
        /// 判定不合法（前方被占用）首次进入等待时打印一条等待日志；判定通过后（与判定同一帧）打印蛇头前进日志，
        /// 确保快照精确反映「判定那一刻」所有寻路块的状态。</summary>
        private IEnumerator WaitUntilCellFree(Vector2Int fromCell, Vector2Int toCell, int step)
        {
            var gc = GameController.Instance;
            if (gc == null || gc.crowdBuffer == null)
            {
                if (debugLog)
                    LogAdvance(fromCell, toCell, step);
                yield break;
            }
            bool waitLogged = false;
            while (gc.crowdBuffer.IsExtractingOccupied(toCell.x, toCell.y))
            {
                if (debugLog && !waitLogged)
                {
                    waitLogged = true;
                    LogWait(fromCell, toCell, step);
                }
                yield return null;
            }
            if (debugLog)
                LogAdvance(fromCell, toCell, step);
        }

        /// <summary>蛇头判定合法前进时打印：上一格 → 当前前进格，以及所有寻路块当前格/目标格。</summary>
        private void LogAdvance(Vector2Int fromCell, Vector2Int toCell, int step)
        {
            LogStep(fromCell, toCell, step, "合法前进");
        }

        /// <summary>蛇头判定前进不合法、前方被占用开始等待时打印（每次等待只打印一条）。</summary>
        private void LogWait(Vector2Int fromCell, Vector2Int toCell, int step)
        {
            LogStep(fromCell, toCell, step, "等待（前方被占用）");
        }

        /// <summary>统一打印一条蛇头判定日志（前进或等待），附带所有寻路块快照。</summary>
        private void LogStep(Vector2Int fromCell, Vector2Int toCell, int step, string verdict)
        {
            var cb = GameController.Instance != null ? GameController.Instance.crowdBuffer : null;
            string dump = cb != null ? cb.DescribeExtraction() : "无 crowdBuffer";
            Debug.Log($"[Pipe] {name} step{step}: 蛇头 {fromCell}->{toCell} {verdict} | {dump}");
#if UNITY_EDITOR
            if (pauseOnLog)
                UnityEditor.EditorApplication.isPaused = true;
#endif
        }

        /// <summary>
        /// 生成一波同色像素：从管道格 scale=0 蛇形前进填满轨道，移动中平滑缩放到 unitSize。
        /// 流式补位：蛇头逐格推进，每步仅当蛇头目标格不再被提取像素占用（停靠/进入视为占用，正在离开不算）时才前进，
        /// 身体同步跟进一格。这样轨道不必整体清空即可开始补位，显著缩短等待时间。
        /// </summary>
        private IEnumerator SpawnWave(int color)
        {
            _spawning = true;
            _waveIndex++;

            var track = TrackCells();
            int n = track.Count;
            if (n == 0)
            {
                ApplyNextColorForWave();   // 无轨道：立即切换下一颜色材质（或隐藏）
                snakeCells.Clear();
                _spawning = false;
                yield break;
            }

            var g = Group;
            var config = GameManager.Instance != null ? GameManager.Instance.colorConfig : null;
            var pipeCell = GetPipeCell(points);

            // 路径 P[0..n]：P[0]=管道格，P[i]=track[i-1]（i≥1）。
            var path = new List<Vector2Int>(n + 1) { pipeCell };
            for (int i = 0; i < n; i++)
                path.Add(track[i]);

            // 生成 n 个像素，全部在管道格 scale=0 起步。
            // 蛇形补位：头像素（p=0）去最远端 P[n]=track[n-1]，后续像素依次停在 P[n-1]…P[1]（尾像素到 track[0]）。
            // 后一个像素只会进入「已被前一个像素让出」的格子，天然不重合、不互相阻挡。
            // grid 目标格在生成时即标记，避免动画期间误判轨道为空。
            var items = new List<PixelItem>(n);
            for (int p = 0; p < n; p++)
            {
                var item = SpawnPixelAtPipe(color, config, pipeCell);
                if (item == null)
                    continue;
                int targetPos = n - p;
                item.gridX = path[targetPos].x;
                item.gridZ = path[targetPos].y;
                if (g != null && g.grid != null)
                    g.grid[item.gridX, item.gridZ] = item;
                items.Add(item);
            }

            float dur = Mathf.Max(0.0001f, cellMoveDuration);

            // 第 s 步（s=1..n）：蛇头从 P[s-1] 进入 P[s]，身体同步前进一格（像素 p 从 P[s-1-p] 进入 P[s-p]）。
            // 每步先等蛇头目标格 P[s] 空闲，再整队同步前进。
            for (int s = 1; s <= n; s++)
            {
                var headDest = path[s];
                UpdateSnakeCells(s, path);   // 蛇头正在前往 path[s]，蛇体已占据 path[s-1..1]
                yield return WaitUntilCellFree(path[s - 1], headDest, s);

                int launched = Mathf.Min(s, items.Count);
                if (s == items.Count)   // 最后一个 pixel 开始释放：此时切换下一颜色材质（含耗尽隐藏颜色 mesh）
                    ApplyNextColorForWave();
                int active = launched;
                for (int p = 0; p < launched; p++)
                {
                    var item = items[p];
                    if (item == null || item.transform == null)
                    {
                        active--;
                        continue;
                    }
                    Vector2Int fromCell = path[s - 1 - p];
                    Vector2Int toCell = path[s - p];
                    StartCoroutine(MoveCell(item, fromCell, toCell, dur, () => active--));
                }
                while (active > 0)
                    yield return null;
            }

            // 全部就位：统一解锁本批像素（清除放置标记、恢复可点击），并按暴露状态激活 Animator + 平滑 Root 位置。
            foreach (var item in items)
            {
                if (item == null || g == null || g.grid == null)
                    continue;
                if (g.grid[item.gridX, item.gridZ] != item)
                    continue;   // 已被匹配移出，不再处理
                item.MarkPlaced();
                item.SetClickable(true);
            }
            Group?.RefreshExposed();
            snakeCells.Clear();
            _spawning = false;
        }

        /// <summary>更新蛇形生成中的实时蛇格：蛇头正在前往 path[s]，蛇体已占据 path[s-1..1]，按蛇头→蛇尾顺序。</summary>
        private void UpdateSnakeCells(int s, List<Vector2Int> path)
        {
            snakeCells.Clear();
            for (int i = s; i >= 1; i--)
                snakeCells.Add(path[i]);
        }

        private PixelItem SpawnPixelAtPipe(int color, ColorConfig config, Vector2Int pipeCell)
        {
            var g = Group;
            if (g == null)
                return null;
            var item = g.SpawnPixel(pipeCell.x, pipeCell.y, color, config, true);
            if (item != null)
            {
                item.SetClickable(false);
                item.placing = true;                      // 蛇形动画期间不解锁 Animator，就位后由 MarkPlaced + RefreshExposed 统一激活
                item.walkableDuringExtraction = true;     // 本批提取像素可穿过该格（不阻挡），提取结束时由 CrowdBufferZone 清除
            }
            return item;
        }

        /// <summary>
        /// 单个像素做一次格子到格子的平滑移动；首段（自管道格出发）同时 scale 0 → unitSize，
        /// 且 y 从 Body Mesh 高度 InQuad 降到标准 y（0）。
        /// </summary>
        private IEnumerator MoveCell(PixelItem item, Vector2Int fromCell, Vector2Int toCell, float dur, Action onDone)
        {
            var g = Group;
            if (item == null || g == null)
            {
                onDone?.Invoke();
                yield break;
            }

            Vector3 from = g.GetLocalPosition(fromCell.x, fromCell.y);
            Vector3 to = g.GetLocalPosition(toCell.x, toCell.y);
            bool firstSegment = fromCell == GetPipeCell(points);

            // 首段：像素自 Body Mesh 的高度出发（scale=0 时不可见），随前进 InQuad 降到标准 y=0。
            float spawnY = 0f;
            if (firstSegment)
            {
                spawnY = GetBodyMeshLocalY();
                if (item.transform != null)
                    item.transform.localPosition = new Vector3(from.x, spawnY, from.z);
            }

            float t = 0f;
            while (t < dur)
            {
                if (item == null || item.transform == null)
                {
                    onDone?.Invoke();
                    yield break;
                }
                // 已被匹配移出网格：交还控制权，不再动画
                if (g.grid == null || g.grid[item.gridX, item.gridZ] != item)
                {
                    onDone?.Invoke();
                    yield break;
                }
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);

                Vector3 pos = Vector3.Lerp(from, to, k);
                if (firstSegment)
                {
                    pos.y = spawnY * (1f - k * k);              // InQuad：自 spawnY 平滑降到 0
                    item.transform.localScale = Vector3.one * g.unitSize * k;
                }
                item.transform.localPosition = pos;
                yield return null;
            }

            if (item != null && item.transform != null)
            {
                item.transform.localPosition = to;
                item.transform.localScale = Vector3.one * g.unitSize;
            }
            onDone?.Invoke();
        }

        /// <summary>
        /// 把管道本体网格的本地 +Z 转向 points[0]→points[1] 的方向（世界方向换算到其父物体局部空间）。
        /// bodyMesh 留空时自动取子物体首个带 MeshFilter 的物体（排除「下一颜色指示器」的 Renderer）。
        /// 顺带按同一方向摆好剩余波次数字（见 <see cref="ApplyWaveCountTextOffset"/>）。
        /// </summary>
        public void OrientBody()
        {
            var g = Group;
            if (g == null || points == null || points.Count < 2)
                return;

            Vector2Int ca = ToCell(points[0]);
            Vector2Int cb = ToCell(points[1]);

            // 数字位置只取决于 points（跟有没有 Body Mesh 无关），所以放在取 bodyMesh 之前
            ApplyWaveCountTextOffset(ca, cb);

            Transform target = bodyMesh != null ? bodyMesh : FindBodyMesh();
            if (target == null)
                return;

            Vector3 worldDir = g.GetWorldPosition(cb.x, cb.y) - g.GetWorldPosition(ca.x, ca.y);
            if (worldDir.sqrMagnitude < 0.0001f)
                return;

            Vector3 localDir = target.parent != null ? target.parent.InverseTransformDirection(worldDir) : worldDir;
            target.localRotation = Quaternion.LookRotation(localDir.normalized);
        }

        /// <summary>
        /// 按管道朝向摆剩余波次数字：**只有朝向左右两侧**（轨迹沿列方向）时应用 <see cref="waveCountTextOffset"/>，
        /// 竖直朝向（沿行方向）时归零 —— 于是把 points 从横向改成竖向，数字回到预制体原位。
        ///
        /// 偏移在**世界 xz** 上叠加，且 **x 按朝向镜像**（朝右用配置值，朝左取负）：配置里的 x 按「朝右」理解，
        /// 两种左右朝向就能共用同一个「外侧」偏移量。z 分量不镜像。
        ///
        /// 改的是 Text **父物体**的世界坐标（不是 Text 自己的 localPosition）—— 预制体里 Text 嵌在
        /// Canvas / 空物体下面也摆得对（与 <c>IceItem.countOffset</c> 同一套做法）。父物体就是管道根时是例外
        /// （预制体里 Text 直接挂根下）：动它会连管道本体一起挪走，所以退回改 Text 自己的世界坐标。
        /// </summary>
        private void ApplyWaveCountTextOffset(Vector2Int pipeCell, Vector2Int firstTrackCell)
        {
            if (waveCountText == null)
                waveCountText = GetComponentInChildren<Text>(true);
            if (waveCountText == null || waveCountText.transform == null)
                return;

            Transform target = waveCountText.transform.parent != null
                ? waveCountText.transform.parent
                : waveCountText.transform;
            if (target == transform)
                target = waveCountText.transform;   // 父物体就是根：退到改 Text 自己

            if (!_textBaseCaptured)
            {
                // 记「相对管道根」的局部位置（不是世界坐标）：管道根在场景里被拖动时数字跟着走，
                // 也不会因为先前的偏移被当成基准而越推越远
                _textBaseLocal = transform.InverseTransformPoint(target.position);
                _textBaseCaptured = true;
            }

            int dx = firstTrackCell.x - pipeCell.x;
            int dz = firstTrackCell.y - pipeCell.y;

            Vector3 offset = Vector3.zero;
            if (dx != 0 && Mathf.Abs(dx) >= Mathf.Abs(dz))
            {
                // 左右朝向：x 按朝向镜像（朝左取负），z 分量照搬
                float x = dx < 0 ? -waveCountTextOffset.x : waveCountTextOffset.x;
                offset = new Vector3(x, 0f, waveCountTextOffset.y);
            }

            target.position = transform.TransformPoint(_textBaseLocal) + offset;
        }

        /// <summary>
        /// 把「数字父物体的原位」重新记成**当前**位置（Inspector 上有按钮，供 <see cref="OrientBody"/> 之后的摆放重设基准）。
        /// 想把数字整体挪到别处时：先把 <see cref="waveCountTextOffset"/> 设成 0、把 Text 的父物体拖到想要的位置，
        /// 再按这个按钮 —— 之后偏移都从这个新原位算起。
        /// </summary>
        public void RecaptureTextBase()
        {
            _textBaseCaptured = false;
            if (points == null || points.Count < 2)
                return;
            ApplyWaveCountTextOffset(ToCell(points[0]), ToCell(points[1]));
        }

        private Transform FindBodyMesh()
        {
            foreach (var f in GetComponentsInChildren<MeshFilter>(true))
            {
                if (f == null || f.transform == transform)
                    continue;
                bool isIndicator = false;
                if (nextColorIndicators != null)
                {
                    foreach (var ind in nextColorIndicators)
                    {
                        if (ind != null && ind.renderer != null && ind.renderer.transform == f.transform)
                        { isIndicator = true; break; }
                    }
                }
                if (isIndicator)
                    continue;
                return f.transform;
            }
            return null;
        }

        /// <summary>Body Mesh 在 PixelGroup 局部空间下的 Y（像素自管道生成的初始高度）；无 Body Mesh 时返回 0。</summary>
        private float GetBodyMeshLocalY()
        {
            var g = Group;
            if (g == null)
                return 0f;
            var body = bodyMesh != null ? bodyMesh : FindBodyMesh();
            if (body == null)
                return 0f;
            return g.transform.InverseTransformPoint(body.position).y;
        }

        private void UpdateDisplay()
        {
            bool hasNext = _waveIndex < colors.Count;

            if (waveCountText != null)
            {
                if (hasNext)
                {
                    waveCountText.gameObject.SetActive(true);
                    waveCountText.text = (colors.Count - _waveIndex).ToString();
                    ApplyTextColor(colors[_waveIndex]);
                }
                else
                {
                    waveCountText.text = "";
                    waveCountText.gameObject.SetActive(false);   // 库存耗尽：隐藏剩余波次数字
                }
            }

        }

        /// <summary>最后一个 pixel 开始释放时的统一处理：更新剩余波次数字（含字色/描边与隐藏）+ 切换下一颜色材质（耗尽隐藏颜色 mesh）。</summary>
        private void ApplyNextColorForWave()
        {
            UpdateDisplay();
            bool hasNext = _waveIndex < colors.Count;
            int nextColor = hasNext ? colors[_waveIndex] : -1;
            ApplyNextColor(nextColor);
        }

        /// <summary>按颜色 ID 应用剩余波次数字的字体颜色与描边颜色（useConfigTextColor 勾选时）。</summary>
        private void ApplyTextColor(int colorId)
        {
            if (!useConfigTextColor || waveCountText == null)
                return;

            var config = GameManager.Instance != null ? GameManager.Instance.colorConfig : null;
            if (config == null)
                return;

            waveCountText.color = config.GetTextColor(colorId);

            var outline = waveCountText.GetComponent<Outline>();
            if (outline == null)
                outline = waveCountText.gameObject.AddComponent<Outline>();
            outline.effectColor = config.GetTextOutlineColor(colorId);
        }

        private void ApplyNextColor(int colorId)
        {
            if (nextColorIndicators == null || nextColorIndicators.Count == 0)
                return;

            var config = GameManager.Instance != null ? GameManager.Instance.colorConfig : null;
            Material mat = (colorId >= 0 && config != null) ? config.GetMaterial(colorId) : null;

            foreach (var ind in nextColorIndicators)
            {
                if (ind == null || ind.renderer == null)
                    continue;

                var mats = ind.renderer.sharedMaterials;
                if (mats == null || ind.materialIndex < 0 || ind.materialIndex >= mats.Length)
                    continue;

                if (mat == null)
                {
                    // 颜色耗尽：优先隐藏；不隐藏则换回默认材质；都不配置则保持最后一波颜色
                    if (ind.hideWhenEmpty)
                    {
                        ind.renderer.gameObject.SetActive(false);
                    }
                    else if (ind.defaultMaterial != null)
                    {
                        ind.renderer.gameObject.SetActive(true);
                        mats[ind.materialIndex] = ind.defaultMaterial;
                        ind.renderer.sharedMaterials = mats;
                    }
                    continue;
                }

                ind.renderer.gameObject.SetActive(true);
                mats[ind.materialIndex] = mat;
                ind.renderer.sharedMaterials = mats;
            }
        }
    }
}
