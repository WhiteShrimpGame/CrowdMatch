using System.Collections.Generic;
using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 容器预制体的**层级快照**：把每个节点的父链、兄弟序、<c>local</c> 姿态、显隐记下来，
    /// 池化复用（<see cref="ContainerGroup.Materialize"/>）时一次性还原，并清掉运行时新增的节点。
    ///
    /// 为什么需要它：车在运行期会被大量改写，散落在 `ContainerItem` / `ContainerExitDriver` 各处，
    /// 逐个手工回滚既容易漏、也会随动画改动而腐化。这里改为「照预制体原样还原」——
    /// 只要预制体是权威，任何新增的改写都不用再补一份回滚代码。已覆盖的改写：
    ///
    /// | 改写 | 出处 |
    /// |---|---|
    /// | 车身被挂到弹性轴 / 侧倾轴下（轴成了车身的**父物体**） | `ContainerItem.SwapElasticAxle` / `SwapRollAxle` |
    /// | 出库途中前轴 / 后轴 / 缩放轴 / 侧倾轴 / 绳后车转轴被提到容器组下 | `ContainerExitDriver.Run` / `ReverseAndSwitchAxle` |
    /// | 轴与车身的 `localScale`（倒车挤压 0.6、弹性缩放）/ `localRotation`（侧翻、甩头） | `SetScaleX` / `drive.rotation` / `roll.localRotation` |
    /// | 车身的世界位移（倒车 + 开出场景） | 出库位移 |
    /// | 盖子 `DisappearWithPop` 后 `SetActive(false)` | `ContainerItem.OpenLid` |
    /// | 问号物体显隐 | `ContainerItem.RefreshQuestionObject` |
    /// | 运行时新增的子物体（拖尾、绳节骨骼、残留乘客） | `ContainerExitDriver.SpawnTrail` / `ContainerRopeLink` |
    ///
    /// **只扫 GameObject 层级，不扫组件。** 运行期新增的组件只有 `ContainerExitDriver`，
    /// 它不能在这里删——`Destroy` 是延迟的，同帧 `GetComponent` 还会拿到将死组件，
    /// 下一次 `driver.Play` 会把它连同协程一起做掉。那一份由
    /// <see cref="ContainerExitDriver.ResetForReuse"/> 显式复位。
    ///
    /// 节点按**名字**定位（不是路径）：换轴期间车身的父物体已经变了，路径不再成立，
    /// 而名字不会变。因此容器预制体上同一棵树内**节点名必须唯一**——重名会在 Console 报错，
    /// 且重名的节点不会被还原（宁可不还原，也不要张冠李戴）。
    /// </summary>
    public sealed class ContainerViewTemplate
    {
        private struct NodeState
        {
            public string name;
            public string parentName;      // 根节点为 null
            public int siblingIndex;       // 根节点为 -1
            public Vector3 localPosition;
            public Quaternion localRotation;
            public Vector3 localScale;
            public bool activeSelf;
        }

        /// <summary>预制体为空时为 false，此时 <see cref="Apply"/> 是空操作。</summary>
        public bool IsValid { get; private set; }

        private readonly List<NodeState> _nodes = new List<NodeState>();
        private readonly HashSet<string> _names = new HashSet<string>();
        private readonly HashSet<string> _duplicateNames = new HashSet<string>();
        private bool _warnedDuplicates;

        /// <summary>从预制体资产抓一份快照（不需要实例）。</summary>
        public static ContainerViewTemplate Capture(Transform prefabRoot)
        {
            var tpl = new ContainerViewTemplate();
            if (prefabRoot == null)
                return tpl;

            tpl.IsValid = true;

            // 用栈做先序遍历（父先子后），子节点逆序入栈以保证出栈顺序与层级顺序一致。
            var stack = new Stack<(Transform node, string parentName, int siblingIndex)>();
            stack.Push((prefabRoot, null, -1));

            while (stack.Count > 0)
            {
                var (node, parentName, siblingIndex) = stack.Pop();

                if (!tpl._names.Add(node.name))
                    tpl._duplicateNames.Add(node.name);   // 重名：Apply 时跳过，避免张冠李戴

                tpl._nodes.Add(new NodeState
                {
                    name = node.name,
                    parentName = parentName,
                    siblingIndex = siblingIndex,
                    localPosition = node.localPosition,
                    localRotation = node.localRotation,
                    localScale = node.localScale,
                    activeSelf = node.gameObject.activeSelf,
                });

                for (int i = node.childCount - 1; i >= 0; i--)
                    stack.Push((node.GetChild(i), node.name, i));
            }

            return tpl;
        }

        /// <summary>
        /// 把一辆车还原成预制体原样。
        /// </summary>
        /// <param name="carRoot">车身根节点（ContainerItem 所在的那个 GameObject 的 Transform）。</param>
        /// <param name="cartParent">车身的「家」——容器组的 Transform。车身此刻的父物体可能已经被换成某根轴，
        /// 所以这里显式传入，用来界定哪些前代节点属于本次要还原的范围。</param>
        public void Apply(Transform carRoot, Transform cartParent)
        {
            if (!IsValid || carRoot == null)
                return;

            WarnDuplicatesOnce();

            // 1. 建「名字 → 节点」表。
            //    除了车身子树，还要收「车身 → cartParent 之间」的**祖先链**：
            //    上车弹性 / 补位侧倾会把车身挂到轴下，出库途中轴也会被提到 cartParent 下——
            //    这些轴此刻是车身的**祖先**，不在子树里，只收子树就找不回它们，父链也就还原不了。
            var map = new Dictionary<string, Transform>();
            for (var t = carRoot.parent; t != null && t != cartParent; t = t.parent)
                RegisterName(map, t);
            RegisterName(map, carRoot);
            var all = carRoot.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
                RegisterName(map, all[i]);

            // 2. 清掉运行时新增的节点（拖尾、绳节、残留乘客）。只删最上层的那些，子节点随父节点一起走；
            //    车身子树之外的一律不碰——同组其它车也挂在 cartParent 下。
            DestroyExtras(carRoot);

            // 3. 按快照顺序（父先子后）还原父链 / 本地姿态 / 显隐。
            for (int i = 0; i < _nodes.Count; i++)
            {
                var n = _nodes[i];

                // 根节点（快照里 parentName 为空的那个）：实例根的名字会被 Instantiate 改成 "xxx(Clone)"、
                // 又被调用方改成 "Container_col_row"，**按名字找不到**，所以不走名字表，直接认 carRoot。
                if (string.IsNullOrEmpty(n.parentName))
                {
                    carRoot.localPosition = n.localPosition;
                    carRoot.localRotation = n.localRotation;
                    carRoot.localScale = n.localScale;
                    continue;
                }

                if (_duplicateNames.Contains(n.name))
                    continue;   // 重名节点不还原（见类注释）
                if (!map.TryGetValue(n.name, out var t) || t == null)
                    continue;   // 缺节点：多半是预制体改过，跳过即可，不影响其它节点

                // 父物体：按名字找；找不到就退回车身根（宁可挂回车身，也不要留在错误的链上）
                Transform parent = null;
                map.TryGetValue(n.parentName, out parent);
                if (parent == null)
                    parent = carRoot;

                if (t.parent != parent)
                    t.SetParent(parent, false);
                t.localPosition = n.localPosition;
                t.localRotation = n.localRotation;
                t.localScale = n.localScale;
                t.SetSiblingIndex(n.siblingIndex);
                t.gameObject.SetActive(n.activeSelf);
            }
        }

        /// <summary>登记节点名。重名只保留第一个（并在 <see cref="_duplicateNames"/> 里记下，Apply 时整体跳过）。</summary>
        private void RegisterName(Dictionary<string, Transform> map, Transform t)
        {
            if (!map.ContainsKey(t.name))
                map.Add(t.name, t);
        }

        /// <summary>自顶向下清掉不属于快照的子节点：命中即整枝销毁，不再往下递归。</summary>
        private void DestroyExtras(Transform node)
        {
            for (int i = node.childCount - 1; i >= 0; i--)
            {
                var child = node.GetChild(i);
                if (_names.Contains(child.name))
                {
                    DestroyExtras(child);
                    continue;
                }

                // 先关再销毁：Destroy 是延迟的，不关的话这一帧还会被画出来。
                child.gameObject.SetActive(false);
                if (Application.isPlaying)
                    Object.Destroy(child.gameObject);
                else
                    Object.DestroyImmediate(child.gameObject);
            }
        }

        private void WarnDuplicatesOnce()
        {
            if (_warnedDuplicates || _duplicateNames.Count == 0)
                return;

            _warnedDuplicates = true;
            var names = string.Join("、", _duplicateNames);
            Debug.LogError("[ContainerViewTemplate] 容器预制体内有重名节点：" + names +
                "。快照按名字定位节点，重名者无法安全还原（已跳过）。请把它们改成不同的名字，" +
                "否则这些子物体在池化复用时可能保持上一次的位移 / 缩放 / 显隐。");
        }
    }
}
