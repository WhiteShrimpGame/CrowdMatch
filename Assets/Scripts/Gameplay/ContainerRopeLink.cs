using System.Collections.Generic;
using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 相邻两车之间的一条绳：左车的 <see cref="ContainerItem.ropeAnchorRight"/> ↔ 右车的 <see cref="ContainerItem.ropeAnchorLeft"/>。
    /// 绳长在任意时刻都**恰好等于**两端点的实际距离。
    ///
    /// UltimateRope 只当网格/骨骼生成器用：建绳后立刻把绳节降级为纯 Transform（刚体转运动学 + 销毁关节），
    /// 之后每帧按 UltimateRope 自己的排布公式把骨骼重铺一遍。走这条自驱路径的原因（详见 Docs/ContainerRopeDesign.md §5.2）：
    ///   · <c>Rope.SetLength()</c> 会全量重建骨骼与蒙皮网格，补位期间距离逐帧变化时不可用；
    ///   · <c>Rope.ExtendBy()</c> 是「展开预折叠绳节」的动画式机制，有预算上限，语义不是「恒等于距离」；
    ///   · 交给物理关节会抖动/下垂，且 UltimateRope 自身要求 <c>fLength ≥ 端点距离</c>，否则打印物理异常警告。
    /// </summary>
    public class ContainerRopeLink : MonoBehaviour
    {
        [Tooltip("左侧车：取它的 ropeAnchorRight 作为绳子起点")]
        public ContainerItem leftCar;

        [Tooltip("右侧车：取它的 ropeAnchorLeft 作为绳子终点")]
        public ContainerItem rightCar;

        [Header("外观")]
        [Tooltip("绳子材质；留空则由 Rope 生成兜底材质")]
        public Material material;

        [Tooltip("绳节数量：越大越顺滑，代价是蒙皮开销")]
        public int linkCount = 8;

        [Tooltip("绳子直径（世界单位）")]
        public float diameter = 0.15f;

        [Tooltip("绳节所在层名。该层不存在时 Rope 会回退到 Default 并打 warning")]
        public string ropeLayerName = "Rope";

        private Rope _rope;
        private UltimateRope.RopeNode _node;
        private bool _ready;

        /// <summary>需要随绳长补偿 tiling 的贴图槽位。TCP2 的 Hybrid 着色器只采样 _BaseMap；_MainTex 留给兜底材质。</summary>
        private static readonly string[] TilingProperties = { "_BaseMap", "_MainTex", "_BumpMap" };

        private SkinnedMeshRenderer _skin;
        private Material[] _materials;                          // 实例材质（改它不会污染共享的 Rope.mat）
        private readonly Dictionary<string, Vector2> _baseTiling = new Dictionary<string, Vector2>();
        private float _baseDistance;                            // 建绳时的端点距离 = 网格 UV / 骨节长度的基准
        private Vector3 _baseLinkScale = Vector3.one;           // 建绳时骨骼的原始缩放

        private Transform StartAnchor => leftCar != null ? leftCar.ropeAnchorRight : null;
        private Transform EndAnchor => rightCar != null ? rightCar.ropeAnchorLeft : null;

        /// <summary>生成绳子并转入自驱模式。两端点齐备且生成成功时返回 true。</summary>
        public bool Build()
        {
            if (StartAnchor == null || EndAnchor == null)
            {
                Debug.LogWarning("[ContainerRopeLink] 端点缺失（左车 " + NameOf(leftCar) + " 的 ropeAnchorRight / 右车 " +
                    NameOf(rightCar) + " 的 ropeAnchorLeft），该组不建绳。", this);
                return false;
            }

            _rope = GetComponent<Rope>();
            if (_rope == null)
                _rope = gameObject.AddComponent<Rope>();

            _rope.startAnchor = StartAnchor;
            _rope.endAnchor = EndAnchor;
            _rope.linkCount = Mathf.Max(2, linkCount);
            _rope.diameter = diameter;
            _rope.length = 0f;                 // <= 0 → 取两端点当前距离
            _rope.material = material;
            _rope.ropeLayerName = ropeLayerName;
            _rope.buildOnStart = false;        // 由这里显式 Build，避免时序不确定

            if (!_rope.Build())
                return false;

            _node = ResolveNode(_rope);
            if (_node == null)
            {
                Debug.LogWarning("[ContainerRopeLink] UltimateRope 未产生可用的绳节数组，该组不建绳。", this);
                return false;
            }

            MakePhysicsFree();
            CacheStretchBaseline();
            ApplyTaut();
            _ready = true;
            return true;
        }

        private static UltimateRope.RopeNode ResolveNode(Rope rope)
        {
            var component = rope.RopeComponent;
            if (component == null || component.RopeNodes == null || component.RopeNodes.Count == 0)
                return null;

            var node = component.RopeNodes[0];
            if (node == null || node.segmentLinks == null || node.segmentLinks.Length == 0)
                return null;
            return node;
        }

        private static string NameOf(ContainerItem car)
        {
            return car != null ? car.name : "<null>";
        }

        /// <summary>
        /// 转成纯 Transform 驱动：绳节刚体置运动学并销毁其关节（使其不受关节与重力影响），
        /// 同时删掉 UltimateRope 的 CreateRopeJoints 加在**两个锚点**上的运动学刚体——
        /// 那是它加在别人物体上的副作用（端点是车预制体的一部分，不该被挂上刚体）。
        /// </summary>
        private void MakePhysicsFree()
        {
            for (int i = 0; i < _node.segmentLinks.Length; i++)
            {
                var link = _node.segmentLinks[i];
                if (link == null)
                    continue;

                var body = link.GetComponent<Rigidbody>();
                if (body != null)
                    body.isKinematic = true;

                var joints = link.GetComponents<Joint>();
                for (int j = 0; j < joints.Length; j++)
                {
                    if (joints[j] != null)
                        Destroy(joints[j]);
                }
            }

            RemoveAnchorBody(StartAnchor);
            RemoveAnchorBody(EndAnchor);
        }

        private static void RemoveAnchorBody(Transform anchor)
        {
            if (anchor == null)
                return;
            var body = anchor.GetComponent<Rigidbody>();
            if (body != null)
                Destroy(body);
        }

        private void LateUpdate()
        {
            // 车（连同端点）已销毁 → 绳根再无意义，自行销毁。出库结束时即走这条。
            if (leftCar == null || rightCar == null)
            {
                Destroy(gameObject);
                return;
            }

            if (_ready)
                ApplyTaut();
        }

        /// <summary>
        /// 把骨骼沿两端点连线重铺一遍。排布公式与 UltimateRope 生成时的一致（CreateRopeJoints 的 Reposition 段），
        /// 只是把当时的固定距离换成当帧的实际距离，因此绳长恒等于端点距离、且与生成瞬间的形态同构。
        /// </summary>
        private void ApplyTaut()
        {
            Transform a = StartAnchor;
            Transform b = EndAnchor;
            if (a == null || b == null)
                return;

            Vector3 p0 = a.position;
            Vector3 p1 = b.position;
            Vector3 delta = p1 - p0;

            int count = _node.segmentLinks.Length;
            int links = _node.nNumLinks > 0 ? _node.nNumLinks : count;

            if (delta.sqrMagnitude < 1e-8f)
            {
                // 两端点重合：全部堆在起点，朝向沿用起点，避免 LookRotation 报错
                for (int i = 0; i < count; i++)
                {
                    var link = _node.segmentLinks[i];
                    if (link == null)
                        continue;
                    link.transform.position = p0;
                    link.transform.rotation = a.rotation;
                }
                _node.fLength = 0f;
                return;
            }

            float distance = delta.magnitude;
            float remaining = Mathf.Max(0f, (distance - distance / links) / distance);   // = (dist - fLinkLength) / dist
            Quaternion facing = Quaternion.LookRotation(delta);

            for (int i = 0; i < count; i++)
            {
                var link = _node.segmentLinks[i];
                if (link == null)
                    continue;

                float t = (count == 1 ? 0f : (float)i / (count - 1)) * remaining;
                link.transform.position = Vector3.Lerp(p0, p1, t);
                link.transform.rotation = facing;
            }

            _node.fLength = distance;   // 保持内部状态与实际一致
            ApplyStretch(distance);
        }

        /// <summary>
        /// 记录「拉伸基准」：建绳时的端点距离、骨骼原始缩放、各贴图槽位的原始 tiling。
        /// 同时取 <c>renderer.materials</c>（返回的是实例）——否则后面写 tiling 会改到共享的 Rope.mat。
        /// </summary>
        private void CacheStretchBaseline()
        {
            _baseDistance = Vector3.Distance(StartAnchor.position, EndAnchor.position);
            if (_baseDistance < 1e-4f)
                return;   // 两端点几乎重合：不做任何拉伸补偿（几何本身已退化）

            if (_node.segmentLinks[0] != null)
                _baseLinkScale = _node.segmentLinks[0].transform.localScale;

            _skin = GetComponent<SkinnedMeshRenderer>();
            if (_skin == null)
                return;

            _materials = _skin.materials;
            if (_materials == null || _materials.Length == 0 || _materials[0] == null)
            {
                _materials = null;
                return;
            }

            for (int i = 0; i < TilingProperties.Length; i++)
            {
                string prop = TilingProperties[i];
                if (_materials[0].HasProperty(prop))
                    _baseTiling[prop] = _materials[0].GetTextureScale(prop);
            }
        }

        /// <summary>
        /// 端点距离相对建绳距离的拉伸倍数 k。骨节长度与纹理密度都用它补偿：k = 1 就是建绳那一刻的形态。
        /// </summary>
        private void ApplyStretch(float distance)
        {
            if (_baseDistance < 1e-4f)
                return;

            float k = distance / _baseDistance;
            ApplyLinkLengthScale(k);
            ApplyTextureDensity(k);
        }

        /// <summary>
        /// 补齐**末端**的一节骨长。
        ///
        /// 网格是非 breakable 的连续管体，环的 z 偏移全是 0（环就贴在骨骼上），**只有最后一个环**带有
        /// 固定的偏移量 <c>LinkLengths[n-1] = fLength / nNumLinks</c>——这个值是建绳时按当时的 <c>fLength</c>
        /// 烘焙进顶点的（<c>UltimateRope.cs:1468</c>、`:1591`），此后不再变。
        ///
        /// 于是管体末端 = <c>bone[n-1] + 烘焙骨长</c>。而我把 <c>bone[n-1]</c> 摆在 <c>(n-1)/n × 距离</c> 处，
        /// 要正好落在终点锚点就需要最后这一节等于 <c>距离/n</c>；但烘焙值是 <c>建绳距离/n</c>，
        /// 两者只在「当前距离 == 建绳距离」时相等。**差多少就漏多少**：`(建绳距离 − 当前距离) / n` ——
        /// 这就是「绳子末端和 anchor 差一段」的来源。
        ///
        /// 修法：把骨骼的 localScale.z 乘 k（骨骼 +Z 就是绳长方向，见下面 ApplyTaut 的 rotation），
        /// 使顶点里那个 z 偏移按 k 缩放 → 末端精确落回锚点。径向用 x/y 缩放，保持 1 不动，管子不会变粗变细。
        /// 所有骨骼都乘 k 是无害的（除末环外其余环的 z 偏移为 0，缩放不移动它们），
        /// 且对 breakable 网格同样正确——那种网格每节都是「骨长 = 节距」，乘 k 后依然首尾相接。
        /// </summary>
        private void ApplyLinkLengthScale(float k)
        {
            for (int i = 0; i < _node.segmentLinks.Length; i++)
            {
                var link = _node.segmentLinks[i];
                if (link == null)
                    continue;
                link.transform.localScale = new Vector3(_baseLinkScale.x, _baseLinkScale.y, _baseLinkScale.z * k);
            }
        }

        /// <summary>
        /// 纹理密度补偿：让贴图沿绳长保持**固定的世界密度**，观感是「绳子从一端被不断放出」而不是「被拉稀」。
        ///
        /// UltimateRope 生成的 UV 是 <c>fRopeT × TotalRopeLength × RopeTextureTileMeters</c>，
        /// 即 u 本身已与世界距离成正比（默认 1 米一次平铺），且 <c>u = 0</c> 落在骨骼链起点——
        /// 也就是左车的右端点（<c>Rope.RopeStart</c>）。几何被拉长 k 倍后，单位世界长度摊到的 u 掉到 1/k、
        /// 贴图因此被拉稀；把 tiling.x 乘 k 即精确还原。而 tiling 是绕 u = 0 缩放 UV 的，
        /// 所以贴图钉在起点端、向另一端生长——正是「从出口放出」的观感。
        ///
        /// 与骨骼数量无关：蒙皮 UV 是顶点属性，不随几何变形改变，所以这条对任意拉伸倍数都成立、也没有长度上限。
        /// </summary>
        private void ApplyTextureDensity(float k)
        {
            if (_materials == null || _baseTiling.Count == 0)
                return;

            foreach (var pair in _baseTiling)
            {
                var scale = new Vector2(pair.Value.x * k, pair.Value.y);
                for (int i = 0; i < _materials.Length; i++)
                {
                    if (_materials[i] != null && _materials[i].HasProperty(pair.Key))
                        _materials[i].SetTextureScale(pair.Key, scale);
                }
            }
        }
    }
}
