# CrowdBufferZone 实现方案归档（备用）

> 本文归档「挤地铁」缓冲区效果的两种**手写仿真**实现，含完整代码，供后续参考 / 回滚 / 对比。
> 两者经实际验证效果均不够理想，当前转向**真实 2D 物理**实现。
>
> - 版本 1：软力版（separation 互斥 + 侧边向后推力）
> - 版本 2：顺序投影硬约束版（按 z 序顺序投影 + noBacktrack + fixedOrder）
>
> 与正式功能设计文档 `CrowdBufferDesign.md` 对应关系：设计文档第 6 节已改写为版本 2 的算法描述。

---

## 版本概览

| 版本 | 核心机制 | 间距 | 侧边约束 | 遗留问题 |
|---|---|---|---|---|
| v1 软力 | steer 前进 + O(n²) 互斥力 + 侧边软向后推力 | 软力（dist < d 才互斥） | 越界越深向后推力越大 | 间距不足、拥挤、需调强度、易震荡 |
| v2 顺序投影 | 每帧按 z 序逐个求真实位置（硬约束单轮投影） | 硬约束（中心距 ≥ spacing，单轮） | 硬约束（中心距 ≥ spacing/2） | 单轮投影残留重叠、后排除被顶回退、排序突变未处理 |

---

## 版本 1：软力版（separation + boundary push）

### 设计要点

- 每帧先为所有像素算**合成速度**：`朝缺口前进` + `间距 d 互斥力` + `侧边向后推力`，再统一积分位移。
- 间距互斥：`dist < spacing` 时按 `(spacing - dist) / spacing * separationStrength` 施加沿「邻居 → 当前」方向的力。
- 侧边约束：**不横向投影**，而是越出侧边时沿 `-axis`（向后）施加与越界深度成正比的推力，把像素推回更宽的区域。
- 两个强度参数：`separationStrength`（互斥刚度）、`boundaryPushStrength`（边界向后推力刚度）。

### 完整代码

```csharp
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
```

---

## 版本 2：顺序投影硬约束版（noBacktrack + fixedOrder）

### 设计要点

- **不做软力、不做速度积分**，每帧按「越靠近缺口越靠前」的顺序，逐个求解像素真实位置。
- 每帧对每个像素：
  1. 候选位置 = 当前位置 + 朝缺口前进 `crowdSpeed * dt`。
  2. **侧边硬约束**：横向投影回有效区域（中心距侧边 ≥ `spacing/2`）。
  3. **间距硬约束**：对每个已固定（z 更大）邻居，若中心距 < `spacing`，沿「邻居 → 当前」方向推到距离 = `spacing`（各向同性，单轮，容忍残留）。
  4. **最大帧位移 clamp**：位移超过 `maxFrameMove` 时沿原方向截断。
  5. **禁止后退（noBacktrack，可选）**：若本帧轴向分量为负（离出口更远），砍掉轴向分量、只保留横向。
- `fixedOrder`（可选）：不按实时 z 排序，按加入队列先后顺序求解（同次点击前排优先、同排靠中心优先，由 `GameController.ResolveMatch` 预排序保证）。
- `spacing` 重定义为**像素直径**：像素间中心距 ≥ spacing，像素中心与侧边 ≥ spacing/2。

### 完整代码

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 「挤地铁」扇形缓冲区：像素离开 PixelGroup 后进入一段梯形缓冲区，朝缺口前进，
    /// 每帧按「越靠近缺口越靠前」的顺序，逐个求解像素的真实位置（硬约束、顺序投影）；
    /// 抵达缺口附近后按最小时间间隔逐个释放，再匀速移动到集结位置并置 arrivedAtGatherPoint，
    /// 交由 ContainerGroup 消费。
    ///
    /// 几何：XZ 平面上的梯形，入口边（宽 entranceWidth）朝像素群，缺口边（宽 gapWidth）朝集结位置。
    /// 约束：像素中心距两条侧边 ≥ spacing/2（半径），像素之间中心距 ≥ spacing（直径，视为像素直径）。
    /// 入口边与缺口边不做轴向钳制（入口是进入点，缺口是出口）。
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
        [Tooltip("像素直径（= 期望中心间距）：像素间中心距 ≥ spacing，中心距侧边 ≥ spacing/2")]
        public float spacing = 0.7f;

        [Tooltip("缓冲区内朝缺口前进的速度（世界单位/秒）")]
        public float crowdSpeed = 5f;

        [Tooltip("单帧最大位移：约束求解结果与当前位置的位移上限，防抖动/限制回退")]
        public float maxFrameMove = 0.3f;

        [Tooltip("禁止后退：像素已到达的轴向位置不回退，后退位移只保留横向分量")]
        public bool noBacktrack = true;

        [Tooltip("固定遍历顺序：按加入队列的先后顺序求解（同次点击前排优先、靠中心优先），不按实时位置重排")]
        public bool fixedOrder = false;

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

            // 保持当前位置，由 Update 驱动朝缺口前进，全程受约束
            _buffered.Add(item);
        }

        private void Update()
        {
            if (_buffered.Count == 0)
                return;

            RefreshGeometry(out Vector3 entrance, out Vector3 gap, out Vector3 axis, out _, out float length);
            float halfEntrance = entranceWidth * 0.5f;
            float halfGap = gapWidth * 0.5f;
            float halfSpacing = spacing * 0.5f;
            float steerStep = crowdSpeed * Time.deltaTime;

            int n = _buffered.Count;

            // 1) 收集有效像素索引
            var order = new List<int>(n);
            for (int i = 0; i < n; i++)
            {
                if (_buffered[i] != null)
                    order.Add(i);
            }

            // 排序：默认按实时轴向 z（越靠近缺口越大）降序；fixedOrder 时保持加入队列的先后顺序
            if (!fixedOrder)
            {
                order.Sort((a, b) =>
                {
                    float za = Vector3.Dot(_buffered[a].transform.position - entrance, axis);
                    float zb = Vector3.Dot(_buffered[b].transform.position - entrance, axis);
                    return zb.CompareTo(za); // 降序：z 大（靠缺口）在前
                });
            }

            // 2) 顺序投影：z 大的先固定，约束 z 小的
            var resolved = new Vector3[n];
            for (int k = 0; k < order.Count; k++)
            {
                int i = order[k];
                Vector3 currentPos = _buffered[i].transform.position;
                Vector3 pos = currentPos;

                // 候选位置：朝缺口前进一步
                Vector3 toGap = gap - pos;
                toGap.y = 0f;
                if (toGap.sqrMagnitude > 0.0001f)
                    pos += toGap.normalized * steerStep;

                // 侧边硬约束（中心距侧边 ≥ spacing/2）
                pos = ProjectToSides(pos, entrance, axis, length, halfEntrance, halfGap, halfSpacing);

                // 与已固定（z 更大）邻居的间距硬约束（各向同性，单轮，容忍残留）
                for (int kk = 0; kk < k; kk++)
                {
                    int j = order[kk];
                    Vector3 d = pos - resolved[j];
                    d.y = 0f;
                    float dist = d.magnitude;
                    if (dist > 0.0001f && dist < spacing)
                        pos += (d / dist) * (spacing - dist);
                }

                // 最大帧位移 clamp（防抖动 / 限制被顶回退的距离）
                Vector3 delta = pos - currentPos;
                delta.y = 0f;
                float move = delta.magnitude;
                if (move > maxFrameMove)
                    pos = currentPos + (delta / move) * maxFrameMove;

                // 禁止后退（可选）：若本帧位移会让像素离出口更远，则砍掉轴向分量，只保留横向分量
                if (noBacktrack)
                {
                    Vector3 d = pos - currentPos;
                    d.y = 0f;
                    float axialMove = Vector3.Dot(d, axis);
                    if (axialMove < 0f)
                        pos = currentPos + (d - axis * axialMove);
                }

                pos.y = currentPos.y;

                resolved[i] = pos;
            }

            // 3) 应用最终位置
            for (int k = 0; k < order.Count; k++)
            {
                int i = order[k];
                _buffered[i].transform.position = resolved[i];
            }

            // 4) 缺口释放调度
            TryRelease(gap);
        }

        /// <summary>把位置沿横向夹回两条侧边内，并留出 spacing/2 的半径余量（中心距侧边 ≥ 半径）</summary>
        private Vector3 ProjectToSides(Vector3 p, Vector3 entrance, Vector3 axis, float length,
            float halfEntrance, float halfGap, float halfSpacing)
        {
            Vector3 rel = p - entrance;
            rel.y = 0f;

            float axial = Vector3.Dot(rel, axis);
            Vector3 lateral = rel - axis * axial; // 垂直于 axis 的侧向分量

            float t = length > 0.0001f ? axial / length : 0f;
            float halfWidth = Mathf.Lerp(halfEntrance, halfGap, Mathf.Clamp01(t));
            float effectiveHalf = Mathf.Max(0f, halfWidth - halfSpacing); // 中心距侧边 ≥ spacing/2

            float latDist = lateral.magnitude;
            if (latDist > effectiveHalf)
            {
                Vector3 dir = latDist > 0.0001f ? lateral / latDist : Vector3.zero;
                lateral = dir * effectiveHalf;
            }

            Vector3 result = entrance + axis * axial + lateral;
            result.y = p.y;
            return result;
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
```

---

## 配套改动（两版一致）

`GameController.ResolveMatch` 的改动（与归档版本配套，当前仍在用）：

```csharp
private void ResolveMatch(PixelItem start)
{
    List<PixelItem> matched = FloodFill(start);

    // 同一次匹配内排序：前排优先（gridZ 大），同排靠中心优先（供 CrowdBufferZone 固定顺序模式使用）
    matched.Sort((a, b) =>
    {
        int zcmp = b.gridZ.CompareTo(a.gridZ);
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

    // 从网格移除并送去聚集点（有缓冲区则先过闸，否则直接散布聚集）
    foreach (var item in matched)
    {
        pixelGroup.grid[item.gridX, item.gridZ] = null;
        if (crowdBuffer != null)
            crowdBuffer.Enter(item);
        else
            GatherItem(item);
    }

    // 各列后排补位
    CollapseColumns();
}
```

以及 `GameController` 新增字段：

```csharp
[Header("过闸缓冲区（可选）")]
[Tooltip("像素离开网格后进入的扇形缓冲区；留空则回退到旧的直接散布聚集")]
public CrowdBufferZone crowdBuffer;
```

---

## 为什么两个版本都不够好

1. **软力版（v1）**：互斥力是软力，拥挤时受力平衡在低于 `spacing` 的位置，间距普遍偏小；强度调高又易震荡 / 穿透。`separationStrength` 与 `boundaryPushStrength` 需反复调，手感不稳定。
2. **顺序投影版（v2）**：
   - 单轮投影**残留重叠**（isotropic push 只把当前像素推开，不回头校正已固定的邻居）。
   - 后排被前排顶住时会**整体向后回退**，形成抖动（`maxFrameMove` 只能缓解不能消除）。
   - 排序突变（相邻两帧像素 z 序交换）会导致位置跳变，尚未处理。
3. **共同问题**：手写约束本质是在近似刚体碰撞，而真实物理引擎（Box2D）天然、稳定地处理「碰撞挤开 + 边界硬约束」，无需手调强度、无单轮残留。因此转向真实 2D 物理。

---

## 版本追溯

| 版本 | Git 提交 | 说明 |
|---|---|---|
| v1 软力 | `ad3c1a0` | `git show ad3c1a0:Assets/Scripts/Gameplay/CrowdBufferZone.cs` 可回看 |
| v2 顺序投影 | 工作区当前版本 | 见本文「版本 2」章节 |
