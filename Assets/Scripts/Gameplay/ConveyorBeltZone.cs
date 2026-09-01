using System.Collections;
using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 传送带宿主：负责把「缓冲区释放出的像素」送入闭环传送带，并在远侧正前方同色 Container 处触发匹配吸收。
    /// 注入 ConveyorBelt 的 ShouldLeave / OnLeave 钩子，管理「移向入口锚点 → reparent 到 carrier → localPosition→0」的无瞬移进入，
    /// 以及入口槽位空闲门控（背压）。
    /// </summary>
    public class ConveyorBeltZone : MonoBehaviour
    {
        [Header("引用")]
        [Tooltip("闭环传送带")]
        public ConveyorBelt belt;

        [Tooltip("容器组（匹配 / 吸收目标）")]
        public ContainerGroup containerGroup;

        [Tooltip("近侧入口锚点（应摆在轨迹近侧直线上）")]
        public Transform entryAnchor;

        [Header("进入")]
        [Tooltip("释放后移向入口锚点的速度（世界单位/秒）")]
        public float releaseSpeed = 8f;

        [Header("匹配")]
        [Tooltip("正前方横向判定范围（约半列间距）")]
        public float matchRangeX = 0.6f;

        [Tooltip("正前方纵向判定范围（远侧到容器前排的间隙）")]
        public float matchRangeZ = 0.8f;

        private const float ArriveEpsilon = 0.05f;
        private const float BoardSmoothRate = 10f;   // localPosition 收敛速率（指数平滑）

        /// <summary>单飞标记：一次只允许一个像素在「出口 → 锚点 → 上车」途中</summary>
        private PixelItem _boarding;

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
            }
        }

        /// <summary>能否接受一个像素：无在途像素，且入口锚点附近存在空槽。</summary>
        public bool CanAccept()
        {
            return _boarding == null && FindNearestFreeSlot() != -1;
        }

        /// <summary>接受一个释放出的像素，起「移向锚点 → 上车 → localPosition→0」协程。</summary>
        public void AcceptPixel(PixelItem pixel)
        {
            if (pixel == null)
                return;

            _boarding = pixel;
            StartCoroutine(BoardRoutine(pixel));
        }

        private IEnumerator BoardRoutine(PixelItem pixel)
        {
            // 1. 匀速移到入口锚点
            if (entryAnchor != null)
                yield return MoveUniform(pixel, entryAnchor.position);

            // 2. 找最近空槽并上车（reparent 到 carrier，保持世界位置 → 不瞬移）
            int slot = FindNearestFreeSlot();
            if (pixel == null || slot < 0 || belt == null || !belt.TryEnter(pixel, slot))
            {
                // 防御兜底：理论上 CanAccept 已挡住（无空槽 / 无 belt）；失败则释放单飞标记
                _boarding = null;
                yield break;
            }

            // 3. localPosition 平滑到 0：像素从当前偏移收敛到 carrier 上，随后被 carrier 带着走
            while (pixel != null && pixel.transform.localPosition.sqrMagnitude > ArriveEpsilon * ArriveEpsilon)
            {
                float k = 1f - Mathf.Exp(-BoardSmoothRate * Time.deltaTime);
                pixel.transform.localPosition = Vector3.Lerp(pixel.transform.localPosition, Vector3.zero, k);
                yield return null;
            }
            if (pixel != null)
                pixel.transform.localPosition = Vector3.zero;

            _boarding = null;
        }

        /// <summary>找距入口锚点最近的空槽；无则 -1。</summary>
        private int FindNearestFreeSlot()
        {
            if (belt == null)
                return -1;

            Vector3 anchor = entryAnchor != null ? entryAnchor.position : belt.transform.position;
            int best = -1;
            float bestDist = float.MaxValue;
            for (int i = 0; i < belt.slotCount; i++)
            {
                if (belt.GetItem(i) != null)
                    continue;

                Vector3 slotPos = belt.GetSlotWorldPosition(i);
                Vector3 d = slotPos - anchor;
                d.y = 0f;
                float dist = d.sqrMagnitude;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = i;
                }
            }
            return best;
        }

        /// <summary>离开判定：像素到达远侧且正前方有同色非空前排 Container。</summary>
        private bool ShouldLeave(IConveyorItem item)
        {
            var pixel = item as PixelItem;
            if (pixel == null || containerGroup == null)
                return false;
            return containerGroup.FindFrontContainerInFrontOf(pixel, matchRangeX, matchRangeZ) != null;
        }

        /// <summary>离开回调：把像素交给同色前排 Container 吸收。</summary>
        private void OnLeave(IConveyorItem item)
        {
            var pixel = item as PixelItem;
            if (pixel == null || containerGroup == null)
                return;

            var container = containerGroup.FindFrontContainerInFrontOf(pixel, matchRangeX, matchRangeZ);
            if (container != null)
                containerGroup.ConsumePixel(pixel, container);
        }

        /// <summary>从当前位置匀速直线移动到目标点（保持 Y 不变）。</summary>
        private IEnumerator MoveUniform(PixelItem item, Vector3 target)
        {
            float y = item.transform.position.y;
            while (item != null)
            {
                Vector3 pos = item.transform.position;
                Vector3 to = target - pos;
                to.y = 0f;
                float dist = to.magnitude;
                if (dist <= ArriveEpsilon)
                    break;

                Vector3 dir = to / dist;
                pos += dir * Mathf.Min(releaseSpeed * Time.deltaTime, dist);
                pos.y = y;
                item.transform.position = pos;
                yield return null;
            }

            if (item != null)
            {
                Vector3 final = target;
                final.y = y;
                item.transform.position = final;
            }
        }
    }
}
