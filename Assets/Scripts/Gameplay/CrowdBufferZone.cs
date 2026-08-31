using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 「挤地铁」封闭式缓冲区（3D 物理版）：把游戏区（像素网格）与漏斗缓冲区合并为一个完全封闭的区间。
    /// 漏斗两条斜边（入口 entranceWidth → 缺口 gapWidth）保持不变；从斜边两个端点（入口两端）向 -Z 延伸两条新墙，
    /// 加宽至后墙（宽 backWidth 可配，上底封口），把游戏区框在其中，球不会漏出；缺口边（窄 gapWidth）同样封口，
    /// 像素只能通过"释放"（移除碰撞体后匀速穿过封口墙）离开。
    /// 像素离开 PixelGroup 后先匀速移动到入口边，再附加刚体（SphereCollider + Rigidbody，冻结 Y / 关重力），
    /// 每个物理帧把速度直接设定为朝出口方向，由物理引擎处理像素间碰撞挤开与墙约束；
    /// 抵达缺口附近后按最小时间间隔逐个释放（移除碰撞体），先匀速移动到出口位置，再匀速移动到集结位置，
    /// 置 arrivedAtGatherPoint 并交由 ContainerGroup 消费。
    ///
    /// 几何：XZ 平面上的封闭区域（y=0）。漏斗梯形（入口 → 缺口）+ 游戏区梯形（入口 → 后墙）共享入口边；
    /// 窄缺口边（宽 gapWidth）与后墙（宽 backWidth）都封口，整个区间无物理开口，墙均为 static BoxCollider。
    /// </summary>
    public class CrowdBufferZone : MonoBehaviour
    {
        [Header("几何")]
        [Tooltip("入口边宽度（漏斗宽边，也是游戏区封闭区间与漏斗的共享边；中心为组件自身位置）")]
        public float entranceWidth = 8f;

        [Tooltip("缺口中心（Transform 引用），缺口边（已封口）朝集结位置，像素经释放在此穿过")]
        public Transform gapPoint;

        [Tooltip("缺口宽度（收窄后的排队口，单文件通过）")]
        public float gapWidth = 0.4f;

        [Tooltip("后墙（上底封口）宽度，应能把游戏区框在其中（通常 ≥ entranceWidth）")]
        public float backWidth = 9f;

        [Tooltip("后墙到入口中心的距离（游戏区封闭区间沿 -Z 的深度）")]
        public float backDepth = 9f;

        [Header("像素物理")]
        [Tooltip("碰撞球世界半径（球视觉直径 = 像素直径 0.5，0.25 即刚好接触；调小可穿插表现拥挤）")]
        public float radius = 0.25f;

        [Tooltip("进入缓冲区（匀速阶段）与物理阶段的驱动速度（物理阶段每帧朝缺口方向直接设定速度）")]
        public float crowdSpeed = 5f;

        [Header("墙")]
        [Tooltip("墙厚度")]
        public float wallThickness = 0.1f;

        [Tooltip("墙高度（应 ≥ 像素直径，覆盖像素竖直范围）")]
        public float wallHeight = 2f;

        [Header("释放")]
        [Tooltip("距缺口中心多近触发释放（缺口已封口，需 ≥ radius + wallThickness/2，否则贴墙像素够不到释放范围、卡死）")]
        public float releaseRadius = 0.6f;

        [Tooltip("两个先后释放像素之间的最小时间间隔（秒）")]
        public float minReleaseInterval = 0.15f;

        [Tooltip("释放后匀速移动到集结位置的速度（世界单位/秒）")]
        public float releaseSpeed = 8f;

        [Header("引用")]
        [Tooltip("集结位置（= GameController.gatherPoint），像素解除约束后移动到这里")]
        public Transform collectPoint;

        /// <summary>抵达集结位置 / 落位点的判定阈值（世界单位）</summary>
        private const float ArriveEpsilon = 0.05f;

        /// <summary>匀速进入缓冲区阶段的像素</summary>
        private readonly List<PixelItem> _approaching = new List<PixelItem>();

        /// <summary>进入阶段每个像素的落位点（入口边处）</summary>
        private readonly Dictionary<PixelItem, Vector3> _entryTargets = new Dictionary<PixelItem, Vector3>();

        /// <summary>物理阶段（已附加刚体）的像素</summary>
        private readonly List<PixelItem> _physical = new List<PixelItem>();

        private float _lastReleaseTime = float.NegativeInfinity;

        // 封闭区间的墙（运行时创建，static 碰撞体）：漏斗两条斜边 + 游戏区两条侧边 + 后墙 + 缺口封口墙
        private GameObject _funnelLeftWall;
        private GameObject _funnelRightWall;
        private GameObject _enclosureLeftWall;
        private GameObject _enclosureRightWall;
        private GameObject _backWall;
        private GameObject _gapWall;

        private void Awake()
        {
            BuildWalls();
        }

        /// <summary>像素离开网格时调用：关闭点击碰撞体，计算入口边落位点，进入匀速接近阶段</summary>
        public void Enter(PixelItem item)
        {
            if (item == null)
                return;

            // 像素已离开网格：关闭球碰撞体（停止点击检测，进入阶段不参与物理），进入物理阶段时复用同一碰撞体
            var sphere = item.GetComponent<SphereCollider>();
            if (sphere != null)
                sphere.enabled = false;

            // 落位点：保持横向位置，轴向移动到入口边（不瞬移，横向顺序自然保持）
            RefreshGeometry(out Vector3 entrance, out _, out _, out Vector3 perp, out _);
            Vector3 rel = item.transform.position - entrance;
            rel.y = 0f;
            float lateral = Vector3.Dot(rel, perp);
            float clampHalf = Mathf.Max(0f, entranceWidth * 0.5f - radius);
            lateral = Mathf.Clamp(lateral, -clampHalf, clampHalf);

            Vector3 entry = entrance + perp * lateral;
            entry.y = item.transform.position.y;

            _entryTargets[item] = entry;
            _approaching.Add(item);
        }

        private void Update()
        {
            StepApproaching();
            TryRelease();
        }

        private void FixedUpdate()
        {
            if (_physical.Count == 0)
                return;

            RefreshGeometry(out _, out Vector3 gap, out _, out _, out _);

            // 每个物理帧把速度直接设定为朝出口（gap）方向；碰撞挤开与侧边墙仍由物理引擎处理
            for (int i = _physical.Count - 1; i >= 0; i--)
            {
                var p = _physical[i];
                if (p == null)
                {
                    _physical.RemoveAt(i);
                    continue;
                }

                var rb = p.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    _physical.RemoveAt(i);
                    continue;
                }

                Vector3 dir = gap - p.transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f)
                    rb.velocity = dir.normalized * crowdSpeed;
                else
                    rb.velocity = Vector3.zero;
            }
        }

        /// <summary>匀速移动进入阶段的像素到落位点，到达后切换为物理阶段</summary>
        private void StepApproaching()
        {
            for (int i = _approaching.Count - 1; i >= 0; i--)
            {
                var p = _approaching[i];
                if (p == null)
                {
                    _approaching.RemoveAt(i);
                    continue;
                }

                Vector3 target = _entryTargets[p];
                Vector3 pos = p.transform.position;
                Vector3 to = target - pos;
                to.y = 0f;
                float dist = to.magnitude;

                if (dist <= ArriveEpsilon)
                {
                    _approaching.RemoveAt(i);
                    _entryTargets.Remove(p);
                    EnterPhysical(p);
                    continue;
                }

                Vector3 dir = to / dist;
                pos += dir * Mathf.Min(crowdSpeed * Time.deltaTime, dist);
                pos.y = p.transform.position.y;
                p.transform.position = pos;
            }
        }

        /// <summary>附加 SphereCollider + Rigidbody，进入物理模拟（碰撞挤开 + 侧边墙约束）；速度由 FixedUpdate 每帧设定</summary>
        private void EnterPhysical(PixelItem item)
        {
            // 防御：若仍残留非球碰撞体（如旧 Cube 像素未重新生成），移除之，确保只有球碰撞体参与物理
            var cols = item.GetComponents<Collider>();
            for (int i = 0; i < cols.Length; i++)
            {
                var c = cols[i];
                if (c is SphereCollider)
                    continue;
                c.enabled = false;
                Destroy(c);
            }

            // 球碰撞体：radius 字段为世界半径，SphereCollider.radius 是本地值（乘 lossyScale），需除回
            var sphere = item.GetComponent<SphereCollider>();
            if (sphere == null)
                sphere = item.gameObject.AddComponent<SphereCollider>();
            float s = Mathf.Max(0.0001f, item.transform.lossyScale.x);
            sphere.radius = radius / s;
            sphere.enabled = true;

            // 刚体：冻结 Y 与旋转，关重力，只在 XZ 平面做真实碰撞
            var rb = item.GetComponent<Rigidbody>();
            if (rb == null)
                rb = item.gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.mass = 1f;
            rb.drag = 0f;
            rb.angularDrag = 0.05f;
            rb.constraints = RigidbodyConstraints.FreezePositionY
                           | RigidbodyConstraints.FreezeRotationX
                           | RigidbodyConstraints.FreezeRotationY
                           | RigidbodyConstraints.FreezeRotationZ;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // 进入物理阶段即时给一个朝出口（gap）的初速度，后续由 FixedUpdate 每帧重写
            RefreshGeometry(out _, out Vector3 gap, out _, out _, out _);
            Vector3 dir = gap - item.transform.position;
            dir.y = 0f;
            dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.forward;
            rb.velocity = dir * crowdSpeed;

            _physical.Add(item);
        }

        /// <summary>每个满足间隔的帧，释放距缺口最近的已就位像素（每次最多一个）</summary>
        private void TryRelease()
        {
            if (_physical.Count == 0)
                return;
            if (Time.time - _lastReleaseTime < minReleaseInterval)
                return;

            RefreshGeometry(out _, out Vector3 gap, out _, out _, out _);

            PixelItem front = null;
            float bestDist = float.MaxValue;
            foreach (var p in _physical)
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

            Release(front);
        }

        /// <summary>移除刚体与碰撞体，先匀速移动到出口位置，再匀速移动到集结位置</summary>
        private void Release(PixelItem item)
        {
            _physical.Remove(item);
            _lastReleaseTime = Time.time;

            var rb = item.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;   // 立即停止物理影响 transform
                rb.velocity = Vector3.zero;
                Destroy(rb);
            }

            var sphere = item.GetComponent<SphereCollider>();
            if (sphere != null)
            {
                sphere.enabled = false;  // 立即停止碰撞
                Destroy(sphere);
            }

            StartCoroutine(MoveToCollect(item));
        }

        /// <summary>解除约束后，先匀速移动到出口（gap）位置，再匀速移动到集结位置</summary>
        private IEnumerator MoveToCollect(PixelItem item)
        {
            // 第一步：匀速移动到出口（gap）位置，确保像素正好穿过缺口
            Vector3 gapPos = gapPoint != null ? gapPoint.position : item.transform.position;
            yield return MoveUniform(item, gapPos);

            // 第二步：匀速移动到集结点
            Vector3 target = collectPoint != null ? collectPoint.position : item.transform.position;
            yield return MoveUniform(item, target);

            OnArrived(item);
        }

        /// <summary>从当前位置匀速直线移动到目标点（保持 Y 不变）</summary>
        private IEnumerator MoveUniform(PixelItem item, Vector3 target)
        {
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

        /// <summary>创建封闭区间的六条墙（static BoxCollider）：漏斗两条斜边（入口→缺口）+ 游戏区两条侧边（入口→后墙）+ 后墙（上底封口）+ 缺口封口墙</summary>
        private void BuildWalls()
        {
            RefreshGeometry(out Vector3 entrance, out Vector3 gap, out Vector3 axis, out Vector3 perp, out _);
            gap.y = entrance.y;

            float halfEntrance = entranceWidth * 0.5f;
            float halfGap = gapWidth * 0.5f;
            float halfBack = backWidth * 0.5f;

            Vector3 backCenter = entrance - axis * backDepth;
            backCenter.y = entrance.y;

            Vector3 entranceLeft = entrance - perp * halfEntrance;
            Vector3 entranceRight = entrance + perp * halfEntrance;
            Vector3 gapLeft = gap - perp * halfGap;
            Vector3 gapRight = gap + perp * halfGap;
            Vector3 backLeft = backCenter - perp * halfBack;
            Vector3 backRight = backCenter + perp * halfBack;

            // 漏斗斜边：保持不变
            CreateWall("CrowdBufferFunnelLeft", entranceLeft, gapLeft, ref _funnelLeftWall);
            CreateWall("CrowdBufferFunnelRight", entranceRight, gapRight, ref _funnelRightWall);
            // 游戏区侧边：从斜边两个端点（入口两端）向 -Z 延伸的新碰撞体
            CreateWall("CrowdBufferEnclosureLeft", entranceLeft, backLeft, ref _enclosureLeftWall);
            CreateWall("CrowdBufferEnclosureRight", entranceRight, backRight, ref _enclosureRightWall);
            // 后墙：上底封口
            CreateWall("CrowdBufferBackWall", backLeft, backRight, ref _backWall);
            // 缺口封口墙：出口处也封口，像素只能通过释放（移除碰撞体后匀速穿过）离开
            CreateWall("CrowdBufferGapWall", gapLeft, gapRight, ref _gapWall);
        }

        /// <summary>创建一条墙：BoxCollider 长度沿墙方向，高度沿世界 Y，厚度沿法向</summary>
        private void CreateWall(string name, Vector3 from, Vector3 to, ref GameObject wall)
        {
            Vector3 sideDir = to - from;
            sideDir.y = 0f;
            float length = sideDir.magnitude;
            if (length < 0.0001f)
                return;
            sideDir /= length;

            Vector3 mid = (from + to) * 0.5f;

            if (wall == null)
            {
                wall = new GameObject(name);
                wall.transform.SetParent(transform, false);
            }

            wall.transform.position = mid;
            wall.transform.rotation = Quaternion.LookRotation(sideDir, Vector3.up);

            var box = wall.GetComponent<BoxCollider>();
            if (box == null)
                box = wall.AddComponent<BoxCollider>();
            box.size = new Vector3(wallThickness, wallHeight, length);
            box.center = Vector3.zero;
            box.isTrigger = false;
        }

        /// <summary>在 Scene 视图绘制封闭区域边缘、前进方向与缺口释放范围</summary>
        private void OnDrawGizmos()
        {
            RefreshGeometry(out Vector3 entrance, out Vector3 gap, out Vector3 axis, out Vector3 perp, out _);
            gap.y = entrance.y;

            float halfEntrance = entranceWidth * 0.5f;
            float halfGap = gapWidth * 0.5f;
            float halfBack = backWidth * 0.5f;
            Vector3 backCenter = entrance - axis * backDepth;
            backCenter.y = entrance.y;

            Vector3 entranceLeft = entrance - perp * halfEntrance;
            Vector3 entranceRight = entrance + perp * halfEntrance;
            Vector3 gapLeft = gap - perp * halfGap;
            Vector3 gapRight = gap + perp * halfGap;
            Vector3 backLeft = backCenter - perp * halfBack;
            Vector3 backRight = backCenter + perp * halfBack;

            // 封闭区间边缘
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(gapLeft, gapRight);           // 缺口边（出口，封口）
            Gizmos.DrawLine(entranceLeft, gapLeft);       // 漏斗斜边（左，保持不变）
            Gizmos.DrawLine(entranceRight, gapRight);     // 漏斗斜边（右，保持不变）
            Gizmos.DrawLine(entranceLeft, backLeft);      // 游戏区侧边（左，向 -Z 延伸）
            Gizmos.DrawLine(entranceRight, backRight);    // 游戏区侧边（右，向 -Z 延伸）
            Gizmos.DrawLine(backLeft, backRight);         // 后墙（上底封口）

            // 前进方向
            Gizmos.color = Color.green;
            Gizmos.DrawLine(backCenter, gap);

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

        /// <summary>重算几何：入口中心（自身位置）、缺口中心、轴向、垂直方向、轴向长度</summary>
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
