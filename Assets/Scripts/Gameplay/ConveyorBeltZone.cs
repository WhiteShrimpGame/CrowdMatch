using System.Collections;
using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 传送带宿主：负责把「缓冲区释放出的像素」送入闭环传送带，并在远侧正前方同色 Container 处触发匹配吸收。
    /// 注入 ConveyorBelt 的 ShouldLeave / OnLeave 钩子；入口采用「槽位直接收集」——订阅 ConveyorBelt.SlotPassedEntry，
    /// 每个槽位过关口时独立地到缓冲区出口取最近的小球上车（reparent + localPosition→0 无瞬移），无全局单飞门控。
    /// </summary>
    public class ConveyorBeltZone : MonoBehaviour
    {
        [Header("引用")]
        [Tooltip("闭环传送带")]
        public ConveyorBelt belt;

        [Tooltip("容器组（匹配 / 吸收目标）")]
        public ContainerGroup containerGroup;

        [Tooltip("缓冲区（出口小球来源）。槽位过关口时从这里取最近的小球")]
        public CrowdBufferZone crowdBuffer;

        [Header("匹配")]
        [Tooltip("正前方横向判定范围（约半列间距）")]
        public float matchRangeX = 0.6f;

        [Tooltip("正前方纵向判定范围（远侧到容器前排的间隙）")]
        public float matchRangeZ = 0.8f;

        [Tooltip("上车收敛的旋转角速度（度/秒）：平滑到槽位途中前半段归 0、后半段转至 localEulerY = -90")]
        public float settleRotateSpeed = 360f;

        private const float ArriveEpsilon = 0.05f;
        private const float BoardSmoothRate = 10f;   // localPosition 收敛速率（指数平滑）

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
                belt.SlotPassedEntry += OnSlotPassedEntry;
                belt.SlotCatchUpChanged += OnSlotCatchUpChanged;
            }
        }

        /// <summary>槽位追赶状态变化：驱动该槽位乘员的走/停动画（追赶 = Walking，否则 = Idle）。</summary>
        private void OnSlotCatchUpChanged(int slotIndex, bool catchingUp)
        {
            if (belt == null)
                return;
            var pixel = belt.GetItem(slotIndex) as PixelItem;
            if (pixel != null)
                pixel.SetWalking(catchingUp);
        }

        /// <summary>某槽位过关口：若该槽仍空且出口有球，取最近小球直接上车。每个槽位独立，互不阻塞。</summary>
        private void OnSlotPassedEntry(int slotIndex)
        {
            if (belt == null || crowdBuffer == null)
                return;
            if (belt.GetItem(slotIndex) != null)
                return;   // 该槽仍被占（异常），跳过本次收集

            var pixel = crowdBuffer.CollectNearest();
            if (pixel == null)
                return;

            if (!belt.TryEnter(pixel, slotIndex))
            {
                // 防御：此处槽位刚确认仍空，单线程下不会失败；真失败则销毁避免泄漏
                Destroy(pixel.gameObject);
                return;
            }

            StartCoroutine(SettleRoutine(pixel, slotIndex));
        }

        /// <summary>上车收敛：localPosition 平滑到槽位 0 点的途中，前半段 localRotation 归 0、后半段 localEulerY 匀速转至 -90。每个小球一条协程，互不阻塞。</summary>
        private IEnumerator SettleRoutine(PixelItem pixel, int slotIndex)
        {
            float startDist = pixel.transform.localPosition.magnitude;
            bool secondHalf = false;

            while (pixel != null)
            {
                float k = 1f - Mathf.Exp(-BoardSmoothRate * Time.deltaTime);
                pixel.transform.localPosition = Vector3.Lerp(pixel.transform.localPosition, Vector3.zero, k);

                // 位置收敛进度：0 = 起点，1 = 到达槽位，按已走距离占初始距离的比例划分前后半段
                float progress = startDist > ArriveEpsilon
                    ? 1f - pixel.transform.localPosition.magnitude / startDist
                    : 1f;
                progress = Mathf.Clamp01(progress);

                if (progress < 0.5f)
                {
                    // 前半段：localRotation 归 0
                    pixel.transform.localRotation = Quaternion.RotateTowards(
                        pixel.transform.localRotation, Quaternion.identity, settleRotateSpeed * Time.deltaTime);
                }
                else
                {
                    // 后半段：localEulerY 匀速转至 -90（X/Z 归 0）
                    secondHalf = true;
                    Vector3 euler = pixel.transform.localEulerAngles;
                    float newY = Mathf.MoveTowardsAngle(euler.y, -90f, settleRotateSpeed * Time.deltaTime);
                    pixel.transform.localRotation = Quaternion.Euler(0f, newY, 0f);
                }

                bool posDone = pixel.transform.localPosition.sqrMagnitude <= ArriveEpsilon * ArriveEpsilon;
                bool rotDone = secondHalf
                    ? Mathf.Abs(Mathf.DeltaAngle(pixel.transform.localEulerAngles.y, -90f)) <= 0.5f
                    : Quaternion.Angle(pixel.transform.localRotation, Quaternion.identity) <= 0.5f;
                if (posDone && rotDone)
                    break;

                yield return null;
            }
            if (pixel != null)
            {
                pixel.transform.localPosition = Vector3.zero;
                pixel.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
                // 落定后按当前槽位追赶状态决定走/停：追赶保持 Walking，否则回到 Idle（相对静止）
                if (belt != null)
                    pixel.SetWalking(belt.IsSlotCatchingUp(slotIndex));
            }
        }

        /// <summary>离开判定：像素到达远侧且正前方有同色非空前排 Container；记录模式下到达远侧即离开。</summary>
        private bool ShouldLeave(IConveyorItem item)
        {
            var pixel = item as PixelItem;
            if (pixel == null)
                return false;

            var gc = GameController.Instance;
            if (gc != null && gc.recordMode)
                return IsAtFarSide(pixel);

            if (containerGroup == null)
                return false;
            return containerGroup.FindMatchableContainer(pixel, matchRangeX, matchRangeZ) != null;
        }

        /// <summary>离开回调：正常模式交给同色前排 Container 吸收；记录模式下直接消失并写入序列文件。</summary>
        private void OnLeave(IConveyorItem item)
        {
            var pixel = item as PixelItem;
            if (pixel == null)
                return;

            var gc = GameController.Instance;
            if (gc != null && gc.recordMode)
            {
                gc.RecordBall(pixel.colorId);
                Destroy(pixel.gameObject);
                return;
            }

            if (containerGroup == null)
                return;

            var container = containerGroup.FindMatchableContainer(pixel, matchRangeX, matchRangeZ);
            if (container != null)
                containerGroup.ConsumePixel(pixel, container);
        }

        /// <summary>像素是否到达传送带远侧（以 ContainerGroup 前排 Z 为基准，纵向落入 matchRangeZ）。</summary>
        private bool IsAtFarSide(PixelItem pixel)
        {
            if (containerGroup == null)
                return false;
            // 前排 row 0 的本地 Z = 0，故世界 Z 即 containerGroup 原点 Z
            float frontZ = containerGroup.transform.position.z;
            return Mathf.Abs(pixel.transform.position.z - frontZ) <= matchRangeZ;
        }

        /// <summary>清空传送带上所有像素（供重载关卡时清理）。</summary>
        public void ClearBelt()
        {
            if (belt == null)
                return;
            for (int i = 0; i < belt.slotCount; i++)
            {
                var pixel = belt.GetItem(i) as PixelItem;
                if (pixel == null)
                    continue;
                belt.ClearSlot(i);
                Destroy(pixel.gameObject);
            }
        }
    }
}
