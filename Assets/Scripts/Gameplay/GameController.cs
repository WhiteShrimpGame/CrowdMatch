using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace CrowdMatch
{
    /// <summary>
    /// 全局单例，负责点击匹配与聚集逻辑。
    /// 点击一个 PixelItem 后，连同相邻同色单位一起离开：只要该同色组能通过空/组内格连通到首排（row 0）即可点击；
    /// 像素离开后，后方像素不再补位（网格保持空位，空位随匹配逐步累积）。
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public class GameController : MonoBehaviour
    {
        public static GameController Instance { get; private set; }

        [Header("引用")]
        [Tooltip("聚集点，被匹配的单位会移动到这里")]
        public Transform gatherPoint;

        [Tooltip("显示聚集点单位数量的 UI 文本")]
        public Text gatherCountText;

        [Tooltip("管理的 PixelGroup，留空会自动查找")]
        public PixelGroup pixelGroup;

        [Tooltip("管理的 ContainerGroup，留空会自动查找")]
        public ContainerGroup containerGroup;

        [Header("速度")]
        [Tooltip("单位向聚集点移动的速度（世界单位/秒）")]
        public float gatherSpeed = 12f;

        [Header("聚集表现")]
        [Tooltip("单位到达聚集点后的散布半径，避免完全重叠")]
        public float gatherScatterRadius = 0.35f;

        [Header("过闸缓冲区（可选）")]
        [Tooltip("像素离开网格后进入的扇形缓冲区；留空则回退到旧的直接散布聚集")]
        public CrowdBufferZone crowdBuffer;

        [Header("传送带（可选）")]
        [Tooltip("释放后像素进入的闭环传送带；留空则显示 gatheredItems 计数")]
        public ConveyorBeltZone conveyorZone;

        [Header("Record 模式")]
        [Tooltip("勾选后运行时新建序列文件；小球到达传送带远侧时直接消失并把颜色写入文件，不进入容器")]
        public bool recordMode = false;

        [Tooltip("序列文件输出目录；留空使用 Application.persistentDataPath")]
        public string recordOutputDir = "";

        /// <summary>处于聚集点中的单位</summary>
        public List<PixelItem> gatheredItems = new List<PixelItem>();

        private StreamWriter _recordWriter;
        private string _recordFilePath;
        private bool _transitioning;

        /// <summary>点击射线检测使用的层遮罩（「Click」层）。</summary>
        private int _clickMask;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (pixelGroup == null)
                pixelGroup = FindObjectOfType<PixelGroup>();
            if (containerGroup == null)
                containerGroup = FindObjectOfType<ContainerGroup>();

            _clickMask = LayerMask.GetMask("Click");

            if (recordMode)
                BeginRecord();

            Init();
        }

        // ===== 关卡流程（初始化 / 胜负检测 / 重载） =====

        /// <summary>进入游玩模式并加载当前关卡。</summary>
        private void Init()
        {
            GameState.GameStart();
            InitLevel(GameData.CurrentLevel);
        }

        /// <summary>按关卡序号加载并应用关卡：清理上一关残留 → 解析 JSON → 应用到两个网格 → 统计像素总数。</summary>
        private void InitLevel(int level)
        {
            CleanupLevel();

            var gm = GameManager.Instance;
            TextAsset json = gm != null ? gm.GetLevelJson(level) : null;
            if (json == null)
            {
                Debug.LogError("[GameController] 找不到第 " + level + " 关的关卡 JSON，无法初始化。");
                return;
            }

            LevelData data = LevelLoader.Parse(json);
            if (data == null)
                return;

            LevelLoader.Apply(pixelGroup, containerGroup, data, gm != null ? gm.colorConfig : null);
            pixelGroup.RefreshExposed();

            GameData.Init(true);
            GameData.TotalPixelCount = CountPixels();
            GameData.ClearedPixelCount = 0;
        }

        /// <summary>原地重载当前关卡（由 GameManager 在胜负过渡后调用）。</summary>
        public void ReloadLevel()
        {
            _transitioning = false;
            GameState.GameStart();
            InitLevel(GameData.CurrentLevel);
        }

        /// <summary>统计当前网格中的像素总数（仅限在网格范围内的 PixelItem）。</summary>
        private int CountPixels()
        {
            if (pixelGroup == null)
                return 0;
            int n = 0;
            foreach (var it in pixelGroup.GetComponentsInChildren<PixelItem>())
            {
                if (it != null && pixelGroup.IsInRange(it.gridX, it.gridZ))
                    n++;
            }
            return n;
        }

        /// <summary>胜利检测：所有像素都被容器消费。触发后等待 1.5s 进入下一关。</summary>
        private void CheckWin()
        {
            if (_transitioning)
                return;
            if (GameData.TotalPixelCount <= 0)
                return;
            if (GameData.ClearedPixelCount >= GameData.TotalPixelCount)
            {
                _transitioning = true;
                GameState.GameWin();
                Invoke(nameof(DoGameWin), 1.5f);
            }
        }

        /// <summary>失败检测：传送带满，且带上所有像素都无法与前排容器匹配。触发后等待 1.5s 重置当前关。</summary>
        private void CheckFail()
        {
            if (_transitioning)
                return;
            if (IsFail())
            {
                _transitioning = true;
                GameState.GameFail();
                Invoke(nameof(DoGameFail), 1.5f);
            }
        }

        /// <summary>失败判定：传送带占满且每个槽位像素都没有同色非空前排容器。</summary>
        private bool IsFail()
        {
            if (conveyorZone == null || conveyorZone.belt == null)
                return false;
            if (conveyorZone.TotalSlots <= 0)
                return false;
            if (conveyorZone.OccupiedSlots < conveyorZone.TotalSlots)
                return false;
            if (containerGroup == null)
                return false;

            var belt = conveyorZone.belt;
            for (int i = 0; i < belt.slotCount; i++)
            {
                var pixel = belt.GetItem(i) as PixelItem;
                if (pixel == null)
                    continue;
                if (containerGroup.HasMatchableContainerOfColor(pixel.colorId))
                    return false;   // 至少一个可匹配 → 未失败
            }
            return true;
        }

        private void DoGameWin()
        {
            var gm = GameManager.Instance;
            if (gm != null)
                gm.GameWin();
        }

        private void DoGameFail()
        {
            var gm = GameManager.Instance;
            if (gm != null)
                gm.GameFail();
        }

        /// <summary>清理上一关残留：停止自身协程，销毁聚集/传送带/缓冲区中的像素，为重建腾出空间。</summary>
        private void CleanupLevel()
        {
            StopAllCoroutines();

            foreach (var item in gatheredItems)
            {
                if (item != null)
                    Destroy(item.gameObject);
            }
            gatheredItems.Clear();

            if (conveyorZone != null)
                conveyorZone.ClearBelt();

            if (crowdBuffer != null)
                crowdBuffer.ResetAll();
        }

        // ===== Record 模式 =====

        /// <summary>开启记录：在指定目录（默认 persistentDataPath）新建带时间戳的序列文件。</summary>
        private void BeginRecord()
        {
            string dir = string.IsNullOrEmpty(recordOutputDir)
                ? Application.persistentDataPath
                : recordOutputDir;

            try
            {
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string name = "Record_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
                _recordFilePath = Path.Combine(dir, name);
                _recordWriter = new StreamWriter(_recordFilePath, false, System.Text.Encoding.UTF8);
                Debug.Log("[GameController] Record 模式已开启，序列文件：" + _recordFilePath);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[GameController] 创建记录文件失败：" + e.Message);
                _recordWriter = null;
            }
        }

        /// <summary>记录一颗离开的小球颜色（每行一个 colorId）。由 ConveyorBeltZone 在记录模式下调用。</summary>
        public void RecordBall(int colorId)
        {
            if (_recordWriter == null)
                return;
            _recordWriter.WriteLine(colorId);
            _recordWriter.Flush();
        }

        private void CloseRecord()
        {
            if (_recordWriter == null)
                return;
            _recordWriter.Flush();
            _recordWriter.Close();
            _recordWriter = null;
            Debug.Log("[GameController] 已关闭记录文件：" + _recordFilePath);
        }

        private void OnApplicationQuit()
        {
            CloseRecord();
        }

        private void OnDestroy()
        {
            CloseRecord();
        }

        private void Update()
        {
            UpdateCountText();

            if (GameState.IsGameStart)
            {
                CheckWin();
                CheckFail();
            }

            if (Input.GetMouseButtonDown(0) && GameState.IsGameStart)
                HandleClick();
        }

        private void UpdateCountText()
        {
            if (gatherCountText != null)
            {
                if (conveyorZone != null)
                    gatherCountText.text = conveyorZone.OccupiedSlots + " / " + conveyorZone.TotalSlots;
                else
                    gatherCountText.text = gatheredItems.Count.ToString();
            }
        }

        private void HandleClick()
        {
            // 提取（寻路离开）进行中时暂不响应，保证网格状态一致
            if (crowdBuffer != null && crowdBuffer.IsExtracting)
                return;
            if (pixelGroup == null || gatherPoint == null || Camera.main == null)
                return;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 1000f, _clickMask))
                return;

            var listener = hit.collider.GetComponentInParent<PixelClickListener>();
            if (listener == null || listener.pixel == null)
                return;
            var item = listener.pixel;

            // 只在仍处于网格中时才触发；能否移出改由 ResolveMatch 判定（同色组需能通过空/组内格连通到首排）
            if (pixelGroup.GetItem(item.gridX, item.gridZ) != item)
                return;

            ResolveMatch(item);
        }

        /// <summary>
        /// 同色组能否离开：把组内格视为即将腾空，检查是否存在一条只经过「空 / 组内」格、从组连通到首排（row 0）的路径。
        /// 有路径即可点击离开（组能寻路到出口）；否则组被其他像素完全包围、无法离开。
        /// </summary>
        private bool CanReachFront(List<PixelItem> matched)
        {
            int cols = pixelGroup.columns;
            int rows = pixelGroup.TotalRows;

            var inGroup = new HashSet<PixelItem>(matched);
            var visited = new bool[cols, rows];
            var queue = new Queue<Vector2Int>();

            foreach (var it in matched)
            {
                if (!pixelGroup.IsInRange(it.gridX, it.gridZ))
                    continue;
                queue.Enqueue(new Vector2Int(it.gridX, it.gridZ));
                visited[it.gridX, it.gridZ] = true;
            }

            int[] dx = { 1, -1, 0, 0 };
            int[] dz = { 0, 0, 1, -1 };

            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                if (cur.y == 0)
                    return true;   // 到达首排

                for (int d = 0; d < 4; d++)
                {
                    int nx = cur.x + dx[d];
                    int nz = cur.y + dz[d];
                    if (!pixelGroup.IsInRange(nx, nz))
                        continue;
                    if (visited[nx, nz])
                        continue;

                    var cell = pixelGroup.grid[nx, nz];
                    if (cell != null && !inGroup.Contains(cell))
                        continue;   // 非组内像素 = 障碍

                    visited[nx, nz] = true;
                    queue.Enqueue(new Vector2Int(nx, nz));
                }
            }

            return false;
        }

        private void ResolveMatch(PixelItem start)
        {
            List<PixelItem> matched = FloodFill(start);

            // 只有能通过空/组内格连通到首排（row 0）的同色组才可移出；否则点击无效（组被其他像素完全包围）
            if (!CanReachFront(matched))
                return;

            // 同一次匹配内排序：前排优先（gridZ 小），同排靠中心优先（供 CrowdBufferZone 提取阶段前到后寻路使用）
            matched.Sort((a, b) =>
            {
                int zcmp = a.gridZ.CompareTo(b.gridZ);
                if (zcmp != 0)
                    return zcmp;
                float center = (pixelGroup.columns - 1) * 0.5f;
                float da = Mathf.Abs(a.gridX - center);
                float db = Mathf.Abs(b.gridX - center);
                int dcmp = da.CompareTo(db);
                if (dcmp != 0)
                    return dcmp;
                return a.gridX.CompareTo(b.gridX);
            });

            // 从网格移除（匹配格先置空，并关闭其暴露状态与点击碰撞体，开始走动画）
            foreach (var item in matched)
            {
                pixelGroup.grid[item.gridX, item.gridZ] = null;
                item.SetExposed(false);
                item.SetClickable(false);
                item.SetWalking(true);
            }

            // 移除后刷新剩余像素的暴露（可点击）状态
            pixelGroup.RefreshExposed();

            // 有缓冲区：进入提取阶段（网格寻路离开）；像素离开后后方不再补位
            // 否则：回退到旧的直接散布聚集
            if (crowdBuffer != null)
            {
                crowdBuffer.EnterBatch(matched, pixelGroup);
            }
            else
            {
                foreach (var item in matched)
                    GatherItem(item);
            }
        }

        private List<PixelItem> FloodFill(PixelItem start)
        {
            var result = new List<PixelItem>();
            var visited = new HashSet<PixelItem>();
            var queue = new Queue<PixelItem>();

            queue.Enqueue(start);
            visited.Add(start);
            int color = start.colorId;

            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                result.Add(cur);

                foreach (var nb in GetNeighbors(cur))
                {
                    if (nb != null && nb.colorId == color && visited.Add(nb))
                        queue.Enqueue(nb);
                }
            }

            return result;
        }

        private IEnumerable<PixelItem> GetNeighbors(PixelItem item)
        {
            int[] dx = { 1, -1, 0, 0 };
            int[] dz = { 0, 0, 1, -1 };
            for (int i = 0; i < dx.Length; i++)
            {
                var nb = pixelGroup.GetItem(item.gridX + dx[i], item.gridZ + dz[i]);
                if (nb != null)
                    yield return nb;
            }
        }

        private void GatherItem(PixelItem item)
        {
            // 关闭碰撞体，避免再次被点击
            var col = item.GetComponent<Collider>();
            if (col != null)
                col.enabled = false;

            item.transform.SetParent(gatherPoint, true);
            gatheredItems.Add(item);

            StartCoroutine(MoveToGatherPoint(item));
        }

        private IEnumerator MoveToGatherPoint(PixelItem item)
        {
            Vector3 start = item.transform.localPosition;
            Vector3 target = RandomGatherTarget();

            float duration = gatherSpeed > 0.0001f
                ? Vector3.Distance(start, target) / gatherSpeed
                : 0f;

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(duration > 0.0001f ? t / duration : 1f);
                item.transform.localPosition = Vector3.Lerp(start, target, k);
                yield return null;
            }

            item.transform.localPosition = target;
            item.arrivedAtGatherPoint = true;
            item.SetWalking(false);   // 抵达聚集点后相对静止 → Idle（回退无传送带路径）
        }

        private Vector3 RandomGatherTarget()
        {
            Vector2 circle = UnityEngine.Random.insideUnitCircle * gatherScatterRadius;
            return new Vector3(circle.x, 0f, circle.y);
        }

    }
}
