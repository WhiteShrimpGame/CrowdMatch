using System;
using System.Collections;
using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 小车出库动画：容器耗尽后，用「前轴 / 后轴 + 父物体切换」驱动小车先倒车、再出车转正、最后整车直行开出场景。
    /// 轴引用取自同物体上的 ContainerItem.frontAxle / rearAxle / reverseScaleAxle / rollAxle；未配置轴时回退为「直接补位 + 销毁」。
    /// 倒车缩放轴（reverseScaleAxle）夹在驱动轴与车体之间做惯性夸张；侧翻自转轴（rollAxle）是最深层节点，出车转正时侧翻、转正后归 0。
    /// 所有 SetParent 都用 worldPositionStays:true 保持世界位姿，零瞬移；偏航（eulerY）写世界 rotation，侧翻（eulerX）写自转轴 localRotation。
    /// </summary>
    public class ContainerExitDriver : MonoBehaviour
    {
        [Header("倒车 / Reverse")]
        [Tooltip("倒车总时长（秒）")]
        public float reverseDuration = 0.4f;

        [Tooltip("倒车总位移（米，沿车尾 +X）")]
        public float reverseDistance = 0.6f;

        [Tooltip("倒车总角度（度，正值为车头甩向负 Y）")]
        public float reverseAngle = 35f;

        [Tooltip("倒车后等待时长（秒）")]
        public float reverseWait = 0.15f;

        [Header("倒车缩放 / Squash")]
        [Tooltip("倒车缩放目标值（倒车+等待结束时 X 缩放到该值，1=不变）")]
        public float reverseSquashScale = 0.6f;

        [Tooltip("倒车缩放起始延迟（从倒车开始计时，多久后开始匀减速缩放；缩放覆盖剩余的倒车 + 等待时间）")]
        public float reverseSquashDelay = 0.2f;

        [Tooltip("出车开始时缩放匀加速回到 1 的时长（秒）")]
        public float exitScaleRecoverDuration = 0.3f;

        [Header("出车 / Exit")]
        [Tooltip("出车线性加速度（米/秒²）")]
        public float exitAcceleration = 8f;

        [Tooltip("出车角度加速度（度/秒²，先甩头到 -exitMaxAngle 再加速归 0）")]
        public float exitAngularAcceleration = 300f;

        [Tooltip("出车最大角度（度，正值为车头甩向负 Y 的最大值；出车转正前先甩到此角再归 0）")]
        public float exitMaxAngle = 55f;

        [Tooltip("出车最大速度（米/秒）")]
        public float exitMaxSpeed = 6f;

        [Tooltip("转正后整车直行时长（秒），到点销毁")]
        public float exitDriveDuration = 0.8f;

        [Header("侧翻 / Roll")]
        [Tooltip("侧翻最大角度（度，正值为绕前进轴的一侧；出车开始即先匀加速后匀减速侧翻到该角度）")]
        public float rollMaxAngle = 12f;

        [Tooltip("侧翻到位时长（秒，自出车开始计时，先匀加速后匀减速侧翻到 rollMaxAngle 的时长）")]
        public float rollOutDuration = 0.4f;

        [Tooltip("侧翻归零时长（秒，位移角度归 0 后匀加速回到 0）")]
        public float rollRecoverDuration = 0.4f;

        private bool _playing;

        /// <summary>启动出库动画；转正瞬间调用 onRefill（补位回调）。</summary>
        public void Play(Action onRefill)
        {
            if (_playing)
                return;
            _playing = true;
            StartCoroutine(Run(onRefill));
        }

        private IEnumerator Run(Action onRefill)
        {
            var container = GetComponent<ContainerItem>();
            Transform front = container != null ? container.frontAxle : null;
            Transform rear = container != null ? container.rearAxle : null;
            Transform scale = container != null ? container.reverseScaleAxle : null;
            Transform roll = container != null ? container.rollAxle : null;

            // 轴未配置：回退到旧「直接补位 + 销毁」
            if (front == null || rear == null)
            {
                onRefill?.Invoke();
                Destroy(gameObject);
                yield break;
            }

            Transform cartParent = transform.parent;   // 小车原始父物体（ContainerGroup）

            // ===== 倒车：后轴驱动（缩放轴若存在则夹在后轴与车体之间） =====
            rear.SetParent(cartParent, true);   // 后轴脱离小车 → 挂到原始父物体
            rear.localScale = Vector3.one;      // 轴始终是纯 pivot，重置 scale，避免继承小车的缩放
            Transform chainRoot = rear;
            if (scale != null) { scale.SetParent(chainRoot, true); chainRoot = scale; }   // 缩放轴 → 后轴下
            if (roll != null)  { roll.SetParent(chainRoot, true);  chainRoot = roll; }    // 自转轴 → 缩放轴下（最深层）
            transform.SetParent(chainRoot, true);   // 小车挂到自转轴（或缩放轴、后轴）

            float t = 0f;
            float prevS = 0f;
            float total = reverseDuration + reverseWait;
            float squashTotal = Mathf.Max(0.0001f, total - reverseSquashDelay);
            while (t < reverseDuration)
            {
                float dt = Time.deltaTime;
                t += dt;
                float p = Mathf.Clamp01(t / reverseDuration);
                float s = reverseDistance * p * p;
                float ang = reverseAngle * p * p;

                rear.position += rear.right * (s - prevS);   // 沿自身 right（车尾 +X）位移本帧增量
                prevS = s;
                rear.rotation = Quaternion.Euler(0f, -ang, 0f);   // 直接赋值负角度

                // 从 reverseSquashDelay 起匀减速缩放（覆盖剩余倒车 + 等待）
                if (scale != null)
                    SetScaleX(scale, 1f - (1f - reverseSquashScale) * EaseOutQuad((t - reverseSquashDelay) / squashTotal));
                yield return null;
            }

            // ===== 倒车等待：匀减速缩放继续，到等待结束缩至 reverseSquashScale =====
            while (t < total)
            {
                float dt = Time.deltaTime;
                t += dt;
                if (scale != null)
                    SetScaleX(scale, 1f - (1f - reverseSquashScale) * EaseOutQuad((t - reverseSquashDelay) / squashTotal));
                yield return null;
            }

            // ===== 出车转正：前轴驱动（缩放轴 → 自转轴 → 小车 链条整体移到前轴下） =====
            // 位移级换轴：先把新轴（前轴）提到与旧位移轴（后轴）同父级并重置 scale，再把直接挂在后轴下的链条节点
            // （缩放轴，或无缩放轴时的自转轴，或都无时的小车）整体移到新轴下，最后把旧轴还给小车。小车全程不脱离
            // 缩放轴/自转轴，自身缩放不被烘、也不重置（避免缩放 pivot 与车体 pivot 不一致导致瞬移）。
            front.SetParent(cartParent, true);   // 前轴脱离小车 → 挂到与旧位移轴（后轴）同父级
            front.localScale = Vector3.one;      // 此刻前轴与系统无牵连，重置 scale 干净
            Transform chainChild = scale != null ? scale : (roll != null ? roll : transform);
            chainChild.SetParent(front, true);   // 把链条最上层节点（缩放轴/自转轴/小车）移到前轴下
            rear.SetParent(transform, true);     // 旧轴（后轴）还给小车
            rear.localScale = Vector3.one;       // 后轴归位后重置 scale（空轴，重置不引起瞬移）

            float v = 0f;
            float angle = -reverseAngle;
            float angularVel = 0f;
            bool swung = exitMaxAngle <= reverseAngle;   // 目标角不超过起点角时，跳过甩头直接归 0
            float recoverT = 0f;
            float rollT = 0f;   // 侧翻出车时钟（自出车开始计时）
            while (true)
            {
                float dt = Time.deltaTime;
                v = Mathf.Min(v + exitAcceleration * dt, exitMaxSpeed);
                front.position += -front.right * (v * dt);   // 沿自身 left（车头 -X）位移（线性照旧，全程推进）

                // 出车开始：缩放匀加速回到 1
                if (scale != null && recoverT < exitScaleRecoverDuration)
                {
                    recoverT += dt;
                    float rp = Mathf.Clamp01(recoverT / exitScaleRecoverDuration);
                    SetScaleX(scale, Mathf.Lerp(reverseSquashScale, 1f, rp * rp));
                }

                // 出车开始：侧翻先匀加速后匀减速到 rollMaxAngle（时间驱动，覆盖整个出车段）
                if (roll != null)
                {
                    rollT += dt;
                    roll.localRotation = Quaternion.Euler(rollMaxAngle * EaseInOutQuad(Mathf.Clamp01(rollT / rollOutDuration)), 0f, 0f);
                }

                if (!swung)
                {
                    // 第一阶段：加速变大（甩头）到 -exitMaxAngle
                    angularVel -= exitAngularAcceleration * dt;
                    angle += angularVel * dt;
                    if (angle <= -exitMaxAngle)
                    {
                        angle = -exitMaxAngle;
                        angularVel = -angularVel;   // 立即反向角速度
                        swung = true;               // 固定进入归 0 阶段，不再回甩
                    }
                }
                else
                {
                    // 第二阶段：反向后加速归 0
                    angularVel += exitAngularAcceleration * dt;
                    angle += angularVel * dt;
                    if (angle >= 0f)
                    {
                        front.rotation = Quaternion.Euler(0f, 0f, 0f);   // 转正

                        // 恢复位移轴 + 缩放轴，但保留自转轴作为小车父物体（侧翻持续到归 0）
                        if (scale != null)
                            SetScaleX(scale, 1f);           // 先让缩放轴归 1（小车仍在其下，围绕正确 pivot 解除挤压）
                        if (roll != null)
                        {
                            roll.localRotation = Quaternion.Euler(rollMaxAngle, 0f, 0f);   // 侧翻到位
                            roll.SetParent(cartParent, true);   // 自转轴（带小车）提到原始父物体
                        }
                        else
                        {
                            transform.SetParent(cartParent, true);   // 无自转轴：小车回原始父物体
                        }
                        front.SetParent(transform, true);        // 前轴归位为小车子物体
                        if (scale != null)
                        {
                            scale.SetParent(transform, true);    // 缩放轴归位
                            SetScaleX(scale, 1f);
                        }

                        onRefill?.Invoke();   // 转正瞬间触发后排补位
                        break;
                    }
                }
                front.rotation = Quaternion.Euler(0f, angle, 0f);
                yield return null;
            }

            // ===== 整车直行：加速到最大后匀速，同时侧翻匀加速归 0，到点销毁 =====
            float hold = 0f;
            float rollRecoverT = 0f;
            bool rollRestored = roll == null;
            while (hold < exitDriveDuration)
            {
                float dt = Time.deltaTime;
                v = Mathf.Min(v + exitAcceleration * dt, exitMaxSpeed);
                transform.position += -transform.right * (v * dt);
                hold += dt;

                // 侧翻匀加速归 0（ease-in quad），归 0 后自转轴还给小车
                if (!rollRestored)
                {
                    rollRecoverT += dt;
                    if (rollRecoverT >= rollRecoverDuration)
                    {
                        roll.localRotation = Quaternion.identity;   // 侧翻归 0
                        transform.SetParent(cartParent, true);      // 小车先脱离自转轴（自转轴已归 0，不烘）
                        roll.SetParent(transform, true);            // 自转轴还给小车
                        rollRestored = true;
                    }
                    else
                    {
                        float rp = Mathf.Clamp01(rollRecoverT / rollRecoverDuration);
                        roll.localRotation = Quaternion.Euler(rollMaxAngle * (1f - rp * rp), 0f, 0f);
                    }
                }

                yield return null;
            }

            Destroy(gameObject);
        }

        /// <summary>匀减速（ease-out quad，p∈[0,1] → [0,1]，起始最快、末速归零）。</summary>
        private static float EaseOutQuad(float p)
        {
            p = Mathf.Clamp01(p);
            return 1f - (1f - p) * (1f - p);
        }

        /// <summary>先匀加速后匀减速（ease-in-out quad，p∈[0,1] → [0,1]，中点为 0.5，两端速度归零）。</summary>
        private static float EaseInOutQuad(float p)
        {
            p = Mathf.Clamp01(p);
            return p < 0.5f ? 2f * p * p : 1f - 2f * (1f - p) * (1f - p);
        }

        /// <summary>只改 Transform 的 localScale.x（保留 y/z）。</summary>
        private static void SetScaleX(Transform t, float x)
        {
            Vector3 ls = t.localScale;
            ls.x = x;
            t.localScale = ls;
        }

        private void OnDestroy()
        {
            // 中途销毁兜底：游离的轴（父物体不是本物体）一并销毁，避免残留空物体
            var container = GetComponent<ContainerItem>();
            if (container == null)
                return;
            if (container.frontAxle != null && container.frontAxle.parent != transform)
                Destroy(container.frontAxle.gameObject);
            if (container.rearAxle != null && container.rearAxle.parent != transform)
                Destroy(container.rearAxle.gameObject);
            if (container.reverseScaleAxle != null && container.reverseScaleAxle.parent != transform)
                Destroy(container.reverseScaleAxle.gameObject);
            if (container.rollAxle != null && container.rollAxle.parent != transform)
                Destroy(container.rollAxle.gameObject);
        }
    }
}
