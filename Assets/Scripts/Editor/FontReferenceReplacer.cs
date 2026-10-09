using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrowdMatch
{
    /// <summary>
    /// 场景字体引用替换：把**当前活动场景**里所有指向字体 A 的引用改成字体 B。
    ///
    /// 用途：工程里 `Assets/Fonts/Alibaba.ttf` 与 `Assets/_Fonts/Alibaba.ttf` 是**同一字体的两份副本**
    /// （字节数、md5 全同，只有路径与 GUID 不同），引用被劈成两半、白占 2 MB 包体。
    /// 本工具把引用收敛到其中一份，之后另一份就能删掉。
    ///
    /// 【扫两层】
    /// · **L1 场景对象**：递归遍历场景全部根物体（含未激活），对每个组件的序列化字段做**通用遍历**
    ///   （<see cref="SerializedObject.GetIterator"/> + <c>NextVisible(true)</c>），凡取值等于 A 的
    ///   <see cref="SerializedPropertyType.ObjectReference"/> 都算命中。不按字段名硬编码，
    ///   所以 `List&lt;Text&gt;`、自定义 struct 内部、私有 `[SerializeField]` 字段都能覆盖。
    /// · **L3 被引用资产**：对 <see cref="EditorUtility.CollectDependencies"/> 返回的**传递闭包**逐个做同样的遍历，
    ///   于是「场景 → 材质 → 材质里挂的字体」这类两级链条自动覆盖。只报告、只写 `Assets/` 下的资产；
    ///   `Packages/` 下的（如第三方插件自带字体）只报告不写。
    ///   ⚠️ 这里**按「是不是落盘资产」判，绝不能按「是不是 GameObject / Component」判** ——
    ///   预制体资产本身就是 GameObject、它上面的组件就是 Component，按类型过滤会让整个预制体被静默跳过
    ///   （`ContainerGroup.containerPrefab` 是 `ContainerItem` 组件，正落在这个坑里）。预制体 / 模型一律
    ///   按资产路径去重后整棵遍历，不依赖 CollectDependencies 究竟返回根物体还是某一个组件。
    ///
    /// 【预制体：这是本工具唯一的陷阱】
    /// 对场景里的预制体实例做序列化遍历时，`objectReferenceValue` 返回的是**穿透后的有效值**（来自预制体资产）。
    /// 若不管三七二十一就写，`ApplyModifiedProperties()` 会**在实例上造出一条 override** —— 而调用方要的是
    /// 「改资产本身」。所以用 <see cref="SerializedProperty.prefabOverride"/> 分三种情形：
    /// ① 普通场景对象 → 写它自己；② 实例上未被覆盖 → 写**预制体资产**；③ 实例上已被覆盖 → 写实例
    /// （它本来就是一条 override，只改资产救不了本场景）。重定向失败时退回写实例并在报告里标明，**不静默**。
    ///
    /// 【不做】只匹配「字体的引用值恰好等于 A」，不含 A 的派生资产（字体材质 / 图集贴图）——
    /// legacy `Font` 的材质与图集由 Unity 运行时生成、`HideFlags.HideAndDontSave`，根本不是工程资产，
    /// 去匹配「资产路径 == A 的路径」永远命中 0 处。也不处理运行时才赋值的字体（序列化引用的固有限制）。
    /// </summary>
    public static class FontReferenceReplacer
    {
        public const string Tag = "[FontRefReplace]";

        private const string UndoLabel = "替换场景字体引用";

        /// <summary>一次扫描的一条命中。跳过项（<see cref="skip"/>）也放在同一个列表里，靠标记区分。</summary>
        public sealed class Hit
        {
            /// <summary>字段的物理持有者（场景对象 / 资产）。用于报告与定位。</summary>
            public Object owner;
            /// <summary>场景层级路径，或资产路径。</summary>
            public string ownerPath;
            public string componentType;
            public string propertyPath;
            public string displayName;
            /// <summary>true = L3 命中（资产内部）。</summary>
            public bool isAsset;
            /// <summary>实际要写的对象（可能是重定向后的预制体资产）。</summary>
            public Object writeTarget;
            /// <summary>true = 不写，只报告。</summary>
            public bool skip;
            public string note;
        }

        public sealed class ScanResult
        {
            public readonly List<Hit> hits = new List<Hit>();
            /// <summary>会被写盘的去重资产路径（爆炸半径，确认框里列的就是它）。</summary>
            public readonly List<string> writeAssetPaths = new List<string>();
            public int SceneCount;
            public int AssetCount;
            public int SkipCount;

            public bool HasWritable()
            {
                for (int i = 0; i < hits.Count; i++)
                    if (!hits[i].skip)
                        return true;
                return false;
            }
        }

        // ===================== 扫描 =====================

        public static ScanResult Scan(Font a, Font b)
        {
            var r = new ScanResult();
            if (a == null || b == null || a == b)
                return r;

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
                return r;

            int aId = a.GetInstanceID();
            var seen = new HashSet<string>();               // (持有者 instanceID | 字段路径)，同一处只记一次
            var assetPaths = new HashSet<string>();         // 会被写盘的去重资产路径
            var scannedObjects = new HashSet<int>();        // 已扫过的非层级资产（材质 / SO …）
            var scannedAssetPaths = new HashSet<string>();  // 已整棵走过的预制体 / 模型资产

            var roots = scene.GetRootGameObjects();

            // ---- L1：场景对象（含预制体实例上的组件） ----
            for (int i = 0; i < roots.Length; i++)
                WalkTransform(roots[i].transform, null, "", aId, a, false, true, r, seen, assetPaths);

            // ---- L3：场景引用到的资产 ----
            var rootObjs = new List<Object>(roots.Length + 1);
            for (int i = 0; i < roots.Length; i++)
                rootObjs.Add(roots[i]);
            // 天空盒材质不在任何 GameObject 上，得像贴图审计那样单独当根
            if (RenderSettings.skybox != null)
                rootObjs.Add(RenderSettings.skybox);

            var deps = EditorUtility.CollectDependencies(rootObjs.ToArray());
            if (deps != null)
            {
                for (int i = 0; i < deps.Length; i++)
                {
                    var d = deps[i];
                    if (d == null || d == a || d == b)
                        continue;                                  // 不动 A 自己、不动 B

                    // 关键：按「是不是落盘的工程资产」判，**不能**按「是不是 GameObject / Component」判 ——
                    // 预制体资产本身就是 GameObject、它上面的组件就是 Component。按类型过滤会把整个预制体
                    // 静默跳掉，表现就是「预制体里用到的字体扫不到」。ContainerGroup.containerPrefab 的类型是
                    // ContainerItem（组件），CollectDependencies 返回它、或返回预制体根物体，两种形态都命中那个坑。
                    if (!EditorUtility.IsPersistent(d))
                        continue;                                  // 场景对象 / 运行时对象，L1 已覆盖

                    string path = Normalize(AssetDatabase.GetAssetPath(d));
                    if (path.Length == 0)
                        continue;                                  // 无路径的内置对象
                    if (path.EndsWith(".unity", System.StringComparison.OrdinalIgnoreCase))
                        continue;                                  // 场景自身

                    bool underAssets = path.StartsWith("Assets/", System.StringComparison.Ordinal);
                    bool underPackages = path.StartsWith("Packages/", System.StringComparison.Ordinal);
                    if (!underAssets && !underPackages)
                        continue;                                  // Library/… 等内建资源，忽略

                    if (d is GameObject || d is Component)
                    {
                        // 预制体 / 模型：按**资产路径**去重后整棵走一遍。不要依赖 CollectDependencies
                        // 究竟返回根物体还是某一个组件 —— 按路径走一遍两种情况都覆盖。
                        if (!scannedAssetPaths.Add(path))
                            continue;
                        var main = AssetDatabase.LoadMainAssetAtPath(path);
                        if (main is GameObject rootGo)
                            WalkTransform(rootGo.transform, null, path + " ▸ ", aId, a, true, underAssets,
                                          r, seen, assetPaths);
                        else
                            ScanOwner(d, path, aId, a, true, underAssets, r, seen, assetPaths);
                    }
                    else
                    {
                        // 材质 / ScriptableObject 等：没有层级，逐个扫
                        if (!scannedObjects.Add(d.GetInstanceID()))
                            continue;
                        ScanOwner(d, path, aId, a, true, underAssets, r, seen, assetPaths);
                    }
                }
            }

            r.writeAssetPaths.AddRange(assetPaths);
            r.writeAssetPaths.Sort(System.StringComparer.Ordinal);
            return r;
        }

        /// <summary>
        /// 递归遍历一个 Transform 及其全部子物体（含未激活），逐个组件做序列化遍历。
        /// 场景根与预制体资产根共用这一套：<paramref name="asset"/> 决定命中算「场景对象」还是「资产内部」，
        /// <paramref name="prefix"/> 让预制体资产的命中显示成「Assets/…/Bus.prefab ▸ Body/Text」。
        /// </summary>
        private static void WalkTransform(Transform t, string parentPath, string prefix, int aId, Font a,
                                          bool asset, bool writable, ScanResult r,
                                          HashSet<string> seen, HashSet<string> assetPaths)
        {
            string hierarchy = parentPath == null ? t.name : parentPath + "/" + t.name;
            string ownerPath = prefix + hierarchy;

            var comps = t.GetComponents<Component>();
            for (int i = 0; i < comps.Length; i++)
            {
                var c = comps[i];
                if (c == null)
                {
                    // 丢脚本（MissingComponent）：对象已不可用，没法逐字段扫，如实记一条跳过
                    AddSkip(r, null, ownerPath, "(缺失脚本)", "(整个组件)", "组件为 null（脚本丢失）");
                    continue;
                }
                ScanOwner(c, ownerPath, aId, a, asset, writable, r, seen, assetPaths);
            }

            for (int i = 0; i < t.childCount; i++)
                WalkTransform(t.GetChild(i), hierarchy, prefix, aId, a, asset, writable, r, seen, assetPaths);
        }

        /// <summary>
        /// 遍历一个对象的全部可见序列化字段，把取值等于 A 的引用记成命中，并判定该写到哪里。
        /// <paramref name="isAsset"/> = true 表示这是 L3 的资产内部命中；<paramref name="writable"/>
        /// 表示该资产是否落在可写的 <c>Assets/</c> 下（false → 只报告不写，如插件包内资产）。
        /// </summary>
        private static void ScanOwner(Object obj, string ownerPath, int aId, Font a, bool isAsset, bool writable,
                                      ScanResult r, HashSet<string> seen, HashSet<string> assetPaths)
        {
            SerializedObject so;
            try
            {
                so = new SerializedObject(obj);
            }
            catch (System.Exception)
            {
                return;
            }

            var it = so.GetIterator();
            while (it.NextVisible(true))
            {
                if (it.propertyType != SerializedPropertyType.ObjectReference)
                    continue;
                if (!ReferencesA(it, aId, a))
                    continue;

                string key = obj.GetInstanceID() + "|" + it.propertyPath;
                if (!seen.Add(key))
                    continue;

                var hit = new Hit
                {
                    owner = obj,
                    ownerPath = ownerPath,
                    componentType = obj.GetType().Name,
                    propertyPath = it.propertyPath,
                    displayName = it.displayName,
                    isAsset = isAsset,
                };

                if (it.prefabOverride)
                {
                    hit.writeTarget = obj;                          // 情形 ③：本层已是一条 override，改它自己
                    hit.note = "已覆盖，改本层";
                }
                else
                {
                    // 情形 ②：值来自预制体 —— 场景实例的来自预制体资产，预制体资产内嵌实例的来自上层预制体。
                    // 必须改那个源资产；写当前层只会白白造一条 override。这一条对场景对象和资产内部同样成立，
                    // 所以不按 isAsset 分叉。
                    // 注意先判「属不属于预制体实例」：普通场景对象、以及预制体资产自身的组件，
                    // prefabOverride 同样是 false，不判就会拿到 null 源、给每一处都打一条误导性的告警。
                    bool inPrefab = PrefabUtility.IsPartOfPrefabInstance(obj);
                    var src = inPrefab ? PrefabUtility.GetCorrespondingObjectFromOriginalSource(obj) : null;
                    if (src != null && AssetDatabase.Contains(src))
                    {
                        hit.writeTarget = src;
                        hit.note = "→ 改预制体资产";
                    }
                    else
                    {
                        hit.writeTarget = obj;
                        if (inPrefab)
                            hit.note = "找不到预制体源，改本层";   // 兜底：来自包 / 源已丢失
                    }
                }

                // 判定写入目标是否落在可写的 Assets/ 下
                bool targetIsAsset = isAsset || !ReferenceEquals(hit.writeTarget, obj);
                if (targetIsAsset)
                {
                    if (!writable)
                    {
                        hit.skip = true;
                        hit.note = Join(hit.note, "跳过：只读资产（不在 Assets/ 下）");
                    }
                    else
                    {
                        string wtPath = Normalize(AssetDatabase.GetAssetPath(hit.writeTarget));
                        if (wtPath.Length == 0 || !wtPath.StartsWith("Assets/", System.StringComparison.Ordinal))
                        {
                            hit.skip = true;
                            hit.note = Join(hit.note, "跳过：写入目标不是 Assets/ 下的资产");
                        }
                        else
                        {
                            assetPaths.Add(wtPath);
                        }
                    }
                }

                AddHit(r, hit);
            }
        }

        /// <summary>
        /// 该字段是否引用 A。先比 instanceID —— 它不会像 <c>objectReferenceValue</c> 那样把被引用对象
        /// 强制加载进来（L3 会扫成百个资产，这一步省下大量无谓加载）。仅当 instanceID 取不到（0）
        /// 时才退回按引用值比较，覆盖极少数未加载资产的取值差异。
        /// </summary>
        private static bool ReferencesA(SerializedProperty sp, int aId, Font a)
        {
            int id = sp.objectReferenceInstanceIDValue;
            if (id == aId)
                return true;
            if (id != 0)
                return false;
            return sp.objectReferenceValue == a;
        }

        private static void AddHit(ScanResult r, Hit h)
        {
            r.hits.Add(h);
            if (h.skip)
                r.SkipCount++;
            else if (h.isAsset)
                r.AssetCount++;
            else
                r.SceneCount++;
        }

        private static void AddSkip(ScanResult r, Object owner, string ownerPath, string componentType,
                                    string displayName, string note)
        {
            r.hits.Add(new Hit
            {
                owner = owner,
                ownerPath = ownerPath,
                componentType = componentType,
                propertyPath = "",
                displayName = displayName,
                isAsset = false,
                skip = true,
                note = note,
            });
            r.SkipCount++;
        }

        // ===================== 执行替换 =====================

        /// <summary>
        /// 执行替换。同一处（写入目标 + 字段路径）只写一次 —— 预制体实例与它的资产会各报一条命中，
        /// 但实际是同一个字段。全部改动归并成一个撤销组。
        /// </summary>
        public static void Apply(ScanResult r, Font b, out int wroteScene, out int wroteAsset, out int failed)
        {
            wroteScene = 0;
            wroteAsset = 0;
            failed = 0;
            if (r == null || b == null)
                return;

            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoLabel);

            var done = new HashSet<string>();
            for (int i = 0; i < r.hits.Count; i++)
            {
                var h = r.hits[i];
                if (h.skip || h.writeTarget == null || h.owner == null || h.propertyPath.Length == 0)
                    continue;

                string key = h.writeTarget.GetInstanceID() + "|" + h.propertyPath;
                if (!done.Add(key))
                    continue;

                var so = new SerializedObject(h.writeTarget);
                var sp = so.FindProperty(h.propertyPath);
                if (sp == null)
                {
                    failed++;
                    Debug.LogWarning(Tag + " 跳过（写入目标上找不到字段 " + h.propertyPath + "）：" + h.ownerPath);
                    continue;
                }

                Undo.RecordObject(h.writeTarget, UndoLabel);
                sp.objectReferenceValue = b;
                so.ApplyModifiedProperties();     // 场景对象上会自动登记预制体修改
                EditorUtility.SetDirty(h.writeTarget);

                // 按**实际写入目标**分类，而不是按命中位置 —— 场景实例上未被覆盖的字段
                // 会被重定向去写预制体资产，那属于资产改动。
                if (EditorUtility.IsPersistent(h.writeTarget))
                    wroteAsset++;
                else
                    wroteScene++;
            }

            AssetDatabase.SaveAssets();           // 资产改动不落盘的话预制体/材质等于没改
            Undo.CollapseUndoOperations(group);   // 整次替换 = 一步撤销
        }

        // ===================== 小工具 =====================

        private static string Normalize(string path) => (path ?? "").Replace('\\', '/');

        private static string Join(string a, string b) =>
            string.IsNullOrEmpty(a) ? b : a + "；" + b;
    }

    /// <summary>
    /// 编辑器窗口：选字体 A / B → 「扫描」看清命中与爆炸半径 → 「替换」（两道确认后才写）。
    /// 窗口只负责展示与确认，扫描与写入都在 <see cref="FontReferenceReplacer"/>。
    /// </summary>
    public class FontReferenceReplacerWindow : EditorWindow
    {
        private const string MenuPath = "CrowdMatch/更多工具/替换场景字体引用";

        /// <summary>951 —— 紧挨「导出场景贴图报告 (950)」，两者同属「场景资源批处理」。</summary>
        private const int MenuOrder = MenuPriority.More + MenuPriority.Seg3 + 1;

        private Font _a;
        private Font _b;

        /// <summary>上次扫描结果。A / B 一变就作废 —— 绝不能拿旧结果去执行新计划。</summary>
        private FontReferenceReplacer.ScanResult _result;

        private Vector2 _scroll;
        private bool _showScene = true;
        private bool _showAsset = true;
        private bool _showSkip;

        [MenuItem(MenuPath, false, MenuOrder)]
        public static void Open() => GetWindow<FontReferenceReplacerWindow>("替换场景字体引用");

        /// <summary>Play 模式下场景是临时态，改了没意义，菜单置灰。</summary>
        [MenuItem(MenuPath, true, MenuOrder)]
        private static bool ValidateOpen() => !EditorApplication.isPlaying;

        private void OnEnable() => minSize = new Vector2(620f, 460f);

        private void OnGUI()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "把当前活动场景里所有指向「字体 A」的引用改成「字体 B」，包含两层：\n" +
                "· 场景内对象（含预制体实例上的组件）；\n" +
                "· 场景引用到的资产（预制体资产 / 材质 / ScriptableObject）内部。\n\n" +
                "预制体实例上未被覆盖的字段会改为预制体资产本身，不产生 override。\n" +
                "只支持 legacy Font；运行时才赋值的字体扫不到；多场景加载与预制体编辑模式不支持。",
                MessageType.Info);

            EditorGUILayout.Space(2);
            EditorGUI.BeginChangeCheck();
            _a = (Font)EditorGUILayout.ObjectField("字体 A（被替换掉）", _a, typeof(Font), false);
            _b = (Font)EditorGUILayout.ObjectField("字体 B（替换为）", _b, typeof(Font), false);
            if (EditorGUI.EndChangeCheck())
                _result = null;

            EditorGUILayout.Space(2);

            if (_a != null && _b != null && _a == _b)
                EditorGUILayout.HelpBox("字体 A 与字体 B 是同一个资产，无需替换。", MessageType.Warning);

            using (new EditorGUI.DisabledScope(_a == null || _b == null || _a == _b))
            {
                if (GUILayout.Button("扫描", GUILayout.Height(24)))
                {
                    _result = FontReferenceReplacer.Scan(_a, _b);
                    _showScene = true;
                    _showAsset = true;
                    _showSkip = _result.SkipCount > 0;
                }
            }

            DrawStatusLine();

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
            if (_result != null)
            {
                DrawGroup("场景对象", ref _showScene, false);
                DrawGroup("被引用资产", ref _showAsset, true);
                DrawSkips();
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(2);
            bool canReplace = _a != null && _b != null && _a != _b
                              && _result != null && _result.HasWritable();
            using (new EditorGUI.DisabledScope(!canReplace))
            {
                if (GUILayout.Button("替换…", GUILayout.Height(28)))
                    DoReplace();
            }
        }

        /// <summary>定高状态行（miniLabel 恒为一行）—— 避免提示文字增减时把下面的滚动区顶来顶去。</summary>
        private void DrawStatusLine()
        {
            string text;
            var r = _result;
            if (r == null)
            {
                text = "尚未扫描。";
            }
            else
            {
                int total = r.SceneCount + r.AssetCount;
                text = total == 0
                    ? "命中 0 处 —— 当前场景没有引用字体 A。"
                    : "命中 " + total + " 处（场景 " + r.SceneCount + " + 资产 " + r.AssetCount + "）"
                      + " · 跳过 " + r.SkipCount + " · 待写资产 " + r.writeAssetPaths.Count + " 个";
            }
            EditorGUILayout.LabelField(text, EditorStyles.miniLabel);
        }

        private void DrawGroup(string title, ref bool fold, bool assetGroup)
        {
            var r = _result;
            if (r == null)
                return;

            int n = 0;
            for (int i = 0; i < r.hits.Count; i++)
            {
                var h = r.hits[i];
                if (!h.skip && h.isAsset == assetGroup)
                    n++;
            }

            fold = EditorGUILayout.Foldout(fold, title + "（" + n + " 处）", true);
            if (!fold)
                return;

            if (n == 0)
            {
                EditorGUILayout.LabelField("    （无）", EditorStyles.miniLabel);
                return;
            }

            for (int i = 0; i < r.hits.Count; i++)
            {
                var h = r.hits[i];
                if (h.skip || h.isAsset != assetGroup)
                    continue;

                string text = "    " + h.ownerPath + "  ▸  " + h.componentType + "  ▸  " + h.displayName;
                if (!string.IsNullOrEmpty(h.note))
                    text += "   [" + h.note + "]";

                if (GUILayout.Button(new GUIContent(text, "点击定位"), EditorStyles.miniLabel))
                    Ping(h.owner);
            }
        }

        private void DrawSkips()
        {
            var r = _result;
            if (r == null || r.SkipCount == 0)
                return;

            _showSkip = EditorGUILayout.Foldout(_showSkip, "跳过（" + r.SkipCount + " 处，不会被改写）", true);
            if (!_showSkip)
                return;

            for (int i = 0; i < r.hits.Count; i++)
            {
                var h = r.hits[i];
                if (!h.skip)
                    continue;
                EditorGUILayout.LabelField("    ⊗ " + (h.ownerPath ?? "(未知)") + "  ▸  " + h.componentType
                                           + "  ▸  " + (h.displayName ?? "") + "   [" + h.note + "]",
                                           EditorStyles.miniLabel);
            }
        }

        private static void Ping(Object o)
        {
            if (o == null)
                return;
            Selection.activeObject = o;
            EditorGUIUtility.PingObject(o);
        }

        private void DoReplace()
        {
            var r = _result;
            if (r == null || _a == null || _b == null || _a == _b)
                return;

            int writable = 0;
            for (int i = 0; i < r.hits.Count; i++)
                if (!r.hits[i].skip)
                    writable++;
            if (writable == 0)
            {
                EditorUtility.DisplayDialog("替换场景字体引用", "没有可改写的命中。", "确定");
                return;
            }

            bool touchesAssets = r.writeAssetPaths.Count > 0;

            // 跨场景的资产改动需要一条干净的回退线，先劝一次保存
            if (touchesAssets && SceneManager.GetActiveScene().isDirty)
            {
                if (!EditorUtility.DisplayDialog("替换场景字体引用",
                    "当前场景有未保存的改动。\n\n" +
                    "这次替换会改写资产文件（影响其它场景与协作者），建议先把场景保存下来，" +
                    "万一要回退时有一条干净的分界线。\n\n仍要继续吗？",
                    "继续", "取消"))
                    return;
            }

            var sb = new StringBuilder();
            sb.Append("将把 ").Append(writable).Append(" 处引用从「").Append(_a.name)
              .Append("」改为「").Append(_b.name).Append("」。\n\n");

            if (touchesAssets)
            {
                sb.Append("会写盘改写的资产（").Append(r.writeAssetPaths.Count)
                  .Append(" 个）—— 这些改动会波及其它场景与协作者：\n");
                for (int i = 0; i < r.writeAssetPaths.Count; i++)
                    sb.Append("  · ").Append(r.writeAssetPaths[i]).Append('\n');
            }
            else
            {
                sb.Append("全部命中都在场景对象上，不涉及任何资产文件。\n");
            }

            if (r.SkipCount > 0)
                sb.Append("\n另有 ").Append(r.SkipCount).Append(" 处会被跳过（见窗口里的「跳过」列表）。\n");

            if (!EditorUtility.DisplayDialog("替换场景字体引用", sb.ToString(), "替换", "取消"))
                return;

            if (touchesAssets)
            {
                if (!EditorUtility.DisplayDialog("替换场景字体引用 · 会改写资产文件",
                    "最后一次确认：这会直接改写上面列出的资产文件，改动会落盘。\n\n" +
                    "Ctrl+Z 只能退回内存里的值；撤销后还需要再保存一次，资产才会真正回到原状。\n\n" +
                    "是否仍然替换？",
                    "仍然替换", "取消"))
                    return;
            }

            FontReferenceReplacer.Apply(r, _b, out int ws, out int wa, out int failed);
            _result = null;

            string summary = "完成：已把引用从「" + _a.name + "」改为「" + _b.name + "」。\n\n" +
                             "场景对象 " + ws + " 处 · 资产 " + wa + " 处";
            if (failed > 0)
                summary += "\n失败 " + failed + " 处（写入目标上找不到对应字段，详见 Console）";
            if (r.SkipCount > 0)
                summary += "\n跳过 " + r.SkipCount + " 处";

            Debug.Log(FontReferenceReplacer.Tag + " 已替换 " + (ws + wa) + " 处引用：" +
                      _a.name + " → " + _b.name + "（场景 " + ws + " + 资产 " + wa +
                      (failed > 0 ? "，失败 " + failed : "") + "）");

            EditorUtility.DisplayDialog("替换场景字体引用", summary, "确定");
            Repaint();
        }
    }
}
