using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 「挤地铁」扇形缓冲区：像素离开 PixelGroup 后进入一段梯形缓冲区，朝缺口前进，
    /// 缓冲区内受边界约束 + 间距 d 互斥分离；抵达缺口附近后按最小时间间隔逐个释放，
    /// 再匀速移动到集结位置并置 arrivedAtGatherPoint，交由 ContainerGroup 消费。
    ///
    /// 几何：XZ 平面上的梯形，入口边（宽 entranceWidth）朝像素群，缺口边（宽 gapWidth）朝集结位置。
    /// 侧边为硬约束；入口边不钳制（像素落位后朝缺口前进）；缺口边为出口而非墙。
    /// </summary>
    public class CrowdBufferZone : MonoBehaviour
    {
        [Header("几何")]
        [Tooltip("入口边宽度（宽口，朝像素群）")]
        public float entranceWidth = 8f;

        [Tooltip("缺口中心（Transform 引用），缺口边朝集结位置")]
        public Transform gapPoint;

        [Tooltip("缺口宽度（0 = 单点，≈ 像素直径 = 单文件通过）")]
        public float gapWidth = 0.2f;

        [Header("仿真")]
        [Tooltip("期望中心间距 d：距离 > d 互不影响，< d 互斥挤开")]
        public float spacing = 0.7f;

        [Tooltip("缓冲区内朝缺口前进的速度（世界单位/秒）")]
        public float crowdSpeed = 5f;

        [Tooltip("间距 d 互斥的推挤强度")]
        public float separationStrength = 3f;

        [Tooltip("侧边向后推力的刚度：越界越深推力越大（代替横向投影）")]
        public float boundaryPushStrength = 15f;

        [Header("释放")]
        [Tooltip("距缺口多近可解除约束")]
        public float releaseRadius = 0.4f;

        [Tooltip("两个先后释放像素之间的最小时间间隔（秒）")]
        public float minReleaseInterval = 0.15f;

        [Tooltip("释放后匀速移动到集结位置的速度（世界单位/秒）")]
        public float releaseSpeed = 8f;

        [Header("引用")]
        [Tooltip("集结位置（= GameController.gatherPoint），像素解除约束后移动到这里")]
        public Transform collectPoint;

        /// <summary>抵达集结位置的判定阈值（世界单位）</summary>
        private const float ArriveEpsilon = 0.05f;

        /// <summary>缓冲区内尚未释放的像素</summary>
        private readonly List<PixelItem> _buffered = new List<PixelItem>();

        private float _lastReleaseTime = float.NegativeInfinity;

        /// <summary>像素离开网格时调用：关闭碰撞体，从当前位置出发，直接进入缓冲区约束（不瞬移）</summary>
        public void Enter(PixelItem item)
        {
            if (item == null)
                return;

            // 关闭碰撞体，避免再次被点击（与 GameController.GatherItem 共享逻辑）
            var col = item.GetComponent<Collider>();
            if (col != null)
                col.enabled = false;

            // 保持当前位置，由 Update 的 steer 驱动朝缺口前进，全程受约束
            _buffered.Add(item);
        }

        private void Update()
        {
            if (_buffered.Count == 0)
                return;

            RefreshGeometry(out Vector3 entrance, out Vector3 gap, out Vector3 axis, out _, out float length);
            float halfEntrance = entranceWidth * 0.5f;
            float halfGap = gapWidth * 0.5f;
            float dt = Time.deltaTime;

            // 1) 先基于当前帧位置统一计算每个像素的合成位移（steer + separation）
            var delta = new Vector3[_buffered.Count];
            for (int i = 0; i < _buffered.Count; i++)
            {
                var p = _buffered[i];
                if (p == null)
                    continue;

                Vector3 pos = p.transform.position;

                // 6.1 朝缺口前进
                Vector3 toGap = gap - pos;
                toGap.y = 0f;
                Vector3 v = toGap.sqrMagnitude > 0.0001f ? toGap.normalized * crowdSpeed : Vector3.zero;

                // 6.2 间距 d 互斥分离（dist < d 才互斥，> d 互不影响）
                for (int j = 0; j < _buffered.Count; j++)
                {
                    if (i == j)
                        continue;
                    var q = _buffered[j];
                    if (q == null)
                        continue;

                    Vector3 d = pos - q.transform.position;
                    d.y = 0f;
                    float dist = d.magnitude;
                    if (dist > 0.0001f && dist < spacing)
                    {
                        float mag = (spacing - dist) / spacing * separationStrength;
                        v += (d / dist) * mag;
                    }
                }

                // 6.3 侧边软向后推力（越界越深推力越大，代替横向投影）
                v += ComputeBoundaryPush(pos, entrance, axis, length, halfEntrance, halfGap);

                delta[i] = v * dt;
            }

            // 2) 统一积分（边界已作为软推力计入位移，不再做位置投影）
            for (int i = 0; i < _buffered.Count; i++)
            {
                var p = _buffered[i];
                if (p == null)
                    continue;
                p.transform.position += delta[i];
            }

            // 3) 缺口释放调度
            TryRelease(gap);
        }

        /// <summary>
        /// 侧边软约束：当像素越出梯形侧边时，沿 -axis（向后）给一个与越界深度成正比的推力，
        /// 把像素推回更宽的区域，而不是横向投影。横向自由度完全交给间距互斥，维持横向 spacing。
        /// </summary>
        private Vector3 ComputeBoundaryPush(Vector3 p, Vector3 entrance, Vector3 axis,
            float length, float halfEntrance, float halfGap)
        {
            Vector3 rel = p - entrance;
            rel.y = 0f;

            float axial = Vector3.Dot(rel, axis);
            Vector3 lateral = rel - axis * axial; // 垂直于 axis 的侧向分量

            float t = length > 0.0001f ? axial / length : 0f;
            float halfWidth = Mathf.Lerp(halfEntrance, halfGap, Mathf.Clamp01(t));

            float over = lateral.magnitude - halfWidth;
            if (over <= 0f)
                return Vector3.zero;

            return -axis * (over * boundaryPushStrength);
        }

        /// <summary>每个满足间隔的帧，释放距缺口最近的已就位像素（每次最多一个）</summary>
        private void TryRelease(Vector3 gap)
        {
            if (_buffered.Count == 0)
                return;
            if (Time.time - _lastReleaseTime < minReleaseInterval)
                return;

            PixelItem front = null;
            float bestDist = float.MaxValue;
            foreach (var p in _buffered)
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

            _buffered.Remove(front);
            _lastReleaseTime = Time.time;
            StartCoroutine(MoveToCollect(front));
        }

        /// <summary>解除约束后，从当前位置匀速直线移动到集结位置</summary>
        private IEnumerator MoveToCollect(PixelItem item)
        {
            Vector3 target = collectPoint != null ? collectPoint.position : item.transform.position;
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

            OnArrived(item);
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

        /// <summary>在 Scene 视图绘制缓冲区梯形边缘、前进方向与缺口释放范围</summary>
        private void OnDrawGizmos()
        {
            RefreshGeometry(out Vector3 entrance, out Vector3 gap, out Vector3 axis, out Vector3 perp, out _);

            // 统一到入口中心所在水平面，避免 entrance / gapPoint 高度不一致导致梯形倾斜
            gap.y = entrance.y;

            Vector3 entranceLeft = entrance - perp * (entranceWidth * 0.5f);
            Vector3 entranceRight = entrance + perp * (entranceWidth * 0.5f);
            Vector3 gapLeft = gap - perp * (gapWidth * 0.5f);
            Vector3 gapRight = gap + perp * (gapWidth * 0.5f);

            // 梯形边缘
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(entranceLeft, entranceRight); // 入口边
            Gizmos.DrawLine(gapLeft, gapRight);           // 缺口边
            Gizmos.DrawLine(entranceLeft, gapLeft);       // 侧边
            Gizmos.DrawLine(entranceRight, gapRight);     // 侧边

            // 前进方向
            Gizmos.color = Color.green;
            Gizmos.DrawLine(entrance, gap);

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

        /// <summary>重算梯形几何：入口中心、缺口中心、轴向、垂直方向、轴向长度</summary>
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
