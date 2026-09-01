using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CrowdMatch
{
    /// <summary>
    /// 全局单例，负责点击匹配、聚集与补位逻辑。
    /// 点击最前排（Z 最大）的 PixelItem 后，连同相邻同色单位一起移动到聚集点；
    /// 空位由后排单位依次匀速补位到前排。
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

        [Header("速度")]
        [Tooltip("单位向聚集点移动的速度（世界单位/秒）")]
        public float gatherSpeed = 12f;

        [Tooltip("后排补位移动的速度（世界单位/秒）")]
        public float refillSpeed = 10f;

        [Header("聚集表现")]
        [Tooltip("单位到达聚集点后的散布半径，避免完全重叠")]
        public float gatherScatterRadius = 0.35f;

        [Header("过闸缓冲区（可选）")]
        [Tooltip("像素离开网格后进入的扇形缓冲区；留空则回退到旧的直接散布聚集")]
        public CrowdBufferZone crowdBuffer;

        [Header("传送带（可选）")]
        [Tooltip("释放后像素进入的闭环传送带；留空则显示 gatheredItems 计数")]
        public ConveyorBeltZone conveyorZone;

        /// <summary>处于聚集点中的单位</summary>
        public List<PixelItem> gatheredItems = new List<PixelItem>();

        private int _refillMovingCount;

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
            if (crowdBuffer != null)
                crowdBuffer.OnBatchExtracted += HandleBatchExtracted;
        }

        /// <summary>一批像素全部离开网格后补位（由 CrowdBufferZone 在提取完成时回调）</summary>
        private void HandleBatchExtracted()
        {
            CollapseColumns();
        }

        private void Update()
        {
            UpdateCountText();

            if (Input.GetMouseButtonDown(0))
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
            // 补位动画进行中或提取（寻路离开）进行中时暂不响应，保证网格状态一致
            if (_refillMovingCount > 0)
                return;
            if (crowdBuffer != null && crowdBuffer.IsExtracting)
                return;
            if (pixelGroup == null || gatherPoint == null || Camera.main == null)
                return;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 1000f))
                return;

            var item = hit.collider.GetComponentInParent<PixelItem>();
            if (item == null)
                return;

            // 只在仍处于网格中时才触发；能否移出改由 ResolveMatch 判定（同色组需连通到首排）
            if (pixelGroup.GetItem(item.gridX, item.gridZ) != item)
                return;

            ResolveMatch(item);
        }

        /// <summary>同色组是否连通到首排（任意成员 gridZ == 0）。连通到首排才可能被移出网格。</summary>
        private bool ReachesFront(List<PixelItem> matched)
        {
            foreach (var item in matched)
                if (item.gridZ == 0)
                    return true;
            return false;
        }

        private void ResolveMatch(PixelItem start)
        {
            List<PixelItem> matched = FloodFill(start);

            // 只有能连通到首排（gridZ 0）的同色组才可移出；否则点击无效
            if (!ReachesFront(matched))
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

            // 从网格移除（匹配格先置空）
            foreach (var item in matched)
                pixelGroup.grid[item.gridX, item.gridZ] = null;

            // 有缓冲区：进入提取阶段（网格寻路离开），补位推迟到提取完成（OnBatchExtracted 回调）
            // 否则：回退到旧的直接散布聚集 + 立即补位
            if (crowdBuffer != null)
            {
                crowdBuffer.EnterBatch(matched, pixelGroup);
            }
            else
            {
                foreach (var item in matched)
                    GatherItem(item);
                CollapseColumns();
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
        }

        private Vector3 RandomGatherTarget()
        {
            Vector2 circle = Random.insideUnitCircle * gatherScatterRadius;
            return new Vector3(circle.x, 0f, circle.y);
        }

        private void CollapseColumns()
        {
            for (int col = 0; col < pixelGroup.columns; col++)
            {
                var remaining = new List<PixelItem>();
                for (int r = 0; r < pixelGroup.rows; r++)
                {
                    var it = pixelGroup.grid[col, r];
                    if (it != null)
                        remaining.Add(it);
                    pixelGroup.grid[col, r] = null;
                }

                // 依次把剩余单位挤到最前排（从 row 0 往下填）
                int targetRow = 0;
                for (int i = 0; i < remaining.Count; i++)
                {
                    var it = remaining[i];
                    int oldRow = it.gridZ;
                    it.gridZ = targetRow;
                    pixelGroup.grid[col, targetRow] = it;

                    if (oldRow != targetRow)
                        StartCoroutine(MoveToGridCell(it, col, targetRow));

                    targetRow++;
                }
            }
        }

        private IEnumerator MoveToGridCell(PixelItem item, int col, int row)
        {
            _refillMovingCount++;

            Vector3 start = item.transform.localPosition;
            Vector3 target = pixelGroup.GetLocalPosition(col, row);

            float duration = refillSpeed > 0.0001f
                ? Vector3.Distance(start, target) / refillSpeed
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
            _refillMovingCount--;
        }
    }
}
