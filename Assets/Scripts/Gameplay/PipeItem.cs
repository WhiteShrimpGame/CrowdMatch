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
    /// 每个像素从管道格以 scale=0 生成，沿轨道蛇形前进并平滑缩放到 unitSize；colors 耗尽后停止。
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

        [Header("显示")]
        [Tooltip("剩余波次数字（UI Text，留空自动从子物体查找）")]
        public Text waveCountText;

        [Tooltip("显示下一颜色的 Renderer + 材质槽位列表；空则不显示")]
        public List<NextColorIndicator> nextColorIndicators = new List<NextColorIndicator>();

        [Serializable]
        public class NextColorIndicator
        {
            [Tooltip("需要替换材质的 Renderer")]
            public Renderer renderer;

            [Tooltip("要替换的材质槽位下标（Renderer.materials 数组的 index）")]
            public int materialIndex;
        }

        [System.NonSerialized] public PixelGroup group;

        [System.NonSerialized] private int _waveIndex;   // 已生成波数（下一波用 colors[_waveIndex]）
        [System.NonSerialized] private bool _spawning;

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
            var seen = new HashSet<Vector2Int>();
            int count = 0;

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
                        count++;
                }
                prev = cur;
            }
            return count;
        }

        /// <summary>轨道格 = 折线经过的所有格（去首点、去重、仅限网格范围内），按路径顺序（近管道 → 远）。</summary>
        public List<Vector2Int> TrackCells()
        {
            var result = new List<Vector2Int>();
            var g = Group;
            if (points == null || points.Count < 2 || g == null)
                return result;
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
                    if (!g.IsInRange(c.x, c.y))
                        continue;
                    if (seen.Add(c))
                        result.Add(c);
                }
                prev = cur;
            }
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
            UpdateDisplay();
        }

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

        /// <summary>等待某格不再被提取中的像素占用（停靠/进入视为占用，正在离开不算）。无缓冲区时立即返回。</summary>
        private IEnumerator WaitUntilCellFree(int col, int row)
        {
            var gc = GameController.Instance;
            if (gc == null || gc.crowdBuffer == null)
                yield break;
            while (gc.crowdBuffer.IsExtractingOccupied(col, row))
                yield return null;
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
            UpdateDisplay();

            var track = TrackCells();
            int n = track.Count;
            if (n == 0)
            {
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
                yield return WaitUntilCellFree(headDest.x, headDest.y);

                int launched = Mathf.Min(s, items.Count);
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
            _spawning = false;
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

        /// <summary>单个像素做一次格子到格子的平滑移动；首段（自管道格出发）同时 scale 0 → unitSize。</summary>
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
                item.transform.localPosition = Vector3.Lerp(from, to, k);
                if (firstSegment)
                    item.transform.localScale = Vector3.one * g.unitSize * k;
                yield return null;
            }

            if (item != null && item.transform != null)
            {
                item.transform.localPosition = to;
                item.transform.localScale = Vector3.one * g.unitSize;
            }
            onDone?.Invoke();
        }

        private void UpdateDisplay()
        {
            if (waveCountText != null)
                waveCountText.text = Mathf.Max(0, colors.Count - _waveIndex).ToString();

            bool hasNext = _waveIndex < colors.Count;
            int nextColor = hasNext ? colors[_waveIndex] : -1;
            ApplyNextColor(nextColor);
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
                if (mat == null)
                    continue;   // 颜色耗尽：不隐藏管道，材质保留最后一波颜色（管道本体保持可见）
                ind.renderer.gameObject.SetActive(true);
                var mats = ind.renderer.sharedMaterials;
                if (mats == null || ind.materialIndex < 0 || ind.materialIndex >= mats.Length)
                    continue;
                mats[ind.materialIndex] = mat;
                ind.renderer.sharedMaterials = mats;
            }
        }
    }
}
