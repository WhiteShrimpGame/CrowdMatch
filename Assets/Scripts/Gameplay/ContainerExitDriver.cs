using System;
using System.Collections;
using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 小车出库动画：容器耗尽后，用「前轴 / 后轴 + 父物体切换」驱动小车先倒车、再出车转正、最后整车直行开出场景。
    /// 轴引用取自同物体上的 ContainerItem.frontAxle / rearAxle；未配置轴时回退为「直接补位 + 销毁」。
    /// 所有 SetParent 都用 worldPositionStays:true 保持世界位姿，零瞬移；角度一律写世界 rotation（eulerY 世界角）。
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

            // 轴未配置：回退到旧「直接补位 + 销毁」
            if (front == null || rear == null)
            {
                onRefill?.Invoke();
                Destroy(gameObject);
                yield break;
            }

            Transform cartParent = transform.parent;   // 小车原始父物体（ContainerGroup）

            // ===== 倒车：后轴驱动 =====
            rear.SetParent(cartParent, true);   // 后轴脱离小车 → 挂到原始父物体
            transform.SetParent(rear, true);    // 小车挂到后轴

            float t = 0f;
            float prevS = 0f;
            while (t < reverseDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / reverseDuration);
                float s = reverseDistance * p * p;
                float ang = reverseAngle * p * p;

                rear.position += rear.right * (s - prevS);   // 沿自身 right（车尾 +X）位移本帧增量
                prevS = s;
                rear.rotation = Quaternion.Euler(0f, -ang, 0f);   // 直接赋值负角度
                yield return null;
            }

            // ===== 倒车等待 =====
            if (reverseWait > 0f)
                yield return new WaitForSeconds(reverseWait);

            // ===== 出车转正：前轴驱动 =====
            transform.SetParent(cartParent, true);   // 小车脱离后轴 → 回到原始父物体
            rear.SetParent(transform, true);         // 后轴归位

            front.SetParent(cartParent, true);       // 前轴脱离小车 → 挂到原始父物体
            transform.SetParent(front, true);        // 小车挂到前轴

            float v = 0f;
            float angle = -reverseAngle;
            float angularVel = 0f;
            bool swung = exitMaxAngle <= reverseAngle;   // 目标角不超过起点角时，跳过甩头直接归 0
            while (true)
            {
                float dt = Time.deltaTime;
                v = Mathf.Min(v + exitAcceleration * dt, exitMaxSpeed);
                front.position += -front.right * (v * dt);   // 沿自身 left（车头 -X）位移（线性照旧，全程推进）

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

                        // 恢复轴子父级，改由整车驱动
                        transform.SetParent(cartParent, true);
                        front.SetParent(transform, true);

                        onRefill?.Invoke();   // 转正瞬间触发后排补位
                        break;
                    }
                }
                front.rotation = Quaternion.Euler(0f, angle, 0f);
                yield return null;
            }

            // ===== 整车直行：加速到最大后匀速，到点销毁 =====
            float hold = 0f;
            while (hold < exitDriveDuration)
            {
                float dt = Time.deltaTime;
                v = Mathf.Min(v + exitAcceleration * dt, exitMaxSpeed);
                transform.position += -transform.right * (v * dt);
                hold += dt;
                yield return null;
            }

            Destroy(gameObject);
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
        }
    }
}
