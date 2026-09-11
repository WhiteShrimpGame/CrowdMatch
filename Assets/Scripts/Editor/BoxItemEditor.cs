using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CrowdMatch
{
    /// <summary>BoxItem 的 Inspector：显示区域、容量校验结果。</summary>
    [CustomEditor(typeof(BoxItem))]
    public class BoxItemEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var box = (BoxItem)target;
            var pg = box.GetComponentInParent<PixelGroup>();

            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            int bodyCount = box.BodyCount;
            int colorCount = box.colorIds != null ? box.colorIds.Length : 0;

            if (pg != null)
            {
                // 确保墙/管/箱占用表最新，自动容量推算准确
                pg.RebuildGrid();
                int computed = BoxItem.ComputeCapacity(pg, box.colMin, box.rowMin, box.colMax, box.rowMax);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("自动容量（本体 + 相邻有效格）", computed.ToString());
                if (box.capacity != computed)
                {
                    EditorGUILayout.HelpBox("当前 capacity(" + box.capacity + ") 与周围环境推算的自动容量(" + computed +
                        ") 不一致。可点击下方按钮重算。", MessageType.Warning);
                    if (GUILayout.Button("按周围环境重算 capacity"))
                    {
                        Undo.RecordObject(box, "重算箱子容量");
                        box.capacity = computed;
                        EditorUtility.SetDirty(box);
                    }
                }
            }

            if (bodyCount <= 0 || box.capacity <= 0)
                EditorGUILayout.HelpBox("箱子区域或容量无效。", MessageType.Warning);
            else if (box.capacity != colorCount)
                EditorGUILayout.HelpBox("capacity(" + box.capacity + ") 与 colorIds 数量(" + colorCount +
                    ") 不一致，运行时按较小值处理。", MessageType.Warning);
            else
                EditorGUILayout.HelpBox("箱子占据 " + bodyCount + " 格，容量 " + box.capacity + "。", MessageType.Info);
        }
    }

    /// <summary>用选中的两个 PixelItem 作为左上、右下创建箱子（与选中顺序无关，取 min/max 归一化）。</summary>
    public static class BoxCreator
    {
        [MenuItem("CrowdMatch/用选中 Pixel 创建箱子（左上、右下）", true)]
        private static bool ValidateCreateBoxFromSelection() => CollectSelectedPixels().Count == 2;

        [MenuItem("CrowdMatch/用选中 Pixel 创建箱子（左上、右下）")]
        private static void CreateBoxFromSelection()
        {
            var pixels = CollectSelectedPixels();
            if (pixels.Count != 2)
            {
                EditorUtility.DisplayDialog("创建箱子", "请恰好选中 2 个 PixelItem（左上、右下）。", "确定");
                return;
            }

            var group = pixels[0].GetComponentInParent<PixelGroup>();
            if (group == null || pixels[1].GetComponentInParent<PixelGroup>() != group)
            {
                EditorUtility.DisplayDialog("创建箱子", "选中的 Pixel 必须位于同一个 PixelGroup 下。", "确定");
                return;
            }

            int cmin = Mathf.Min(pixels[0].gridX, pixels[1].gridX);
            int rmin = Mathf.Min(pixels[0].gridZ, pixels[1].gridZ);
            int cmax = Mathf.Max(pixels[0].gridX, pixels[1].gridX);
            int rmax = Mathf.Max(pixels[0].gridZ, pixels[1].gridZ);

            if (cmax - cmin + 1 < 2 || rmax - rmin + 1 < 2)
            {
                EditorUtility.DisplayDialog("创建箱子", "箱子长宽需至少为 2（编辑器限制）。", "确定");
                return;
            }

            BoxCreateWizard.Group = group;
            BoxCreateWizard.ColMin = cmin;
            BoxCreateWizard.RowMin = rmin;
            BoxCreateWizard.ColMax = cmax;
            BoxCreateWizard.RowMax = rmax;

            ScriptableWizard.DisplayWizard<BoxCreateWizard>("创建箱子", "创建", "取消");
        }

        /// <summary>收集按点选顺序缓存的 GameObject 中的 PixelItem（去重）。支持选中 PixelItem 的子物体（向上查找父级组件）。</summary>
        private static List<PixelItem> CollectSelectedPixels()
        {
            var result = new List<PixelItem>();
            var seen = new HashSet<PixelItem>();
            foreach (var go in SelectionOrderTracker.Ordered)
            {
                if (go == null)
                    continue;
                var p = go.GetComponentInParent<PixelItem>();
                if (p != null && seen.Add(p))
                    result.Add(p);
            }
            return result;
        }
    }

    /// <summary>箱子创建参数向导：容量、颜色、动画参数。</summary>
    public class BoxCreateWizard : ScriptableWizard
    {
        public static PixelGroup Group;
        public static int ColMin, RowMin, ColMax, RowMax;

        [Tooltip("容量 = 隐藏 Pixel 数量 = 开箱触发阈值")]
        public int capacity;

        [Tooltip("每个隐藏 Pixel 的颜色 ID，逗号分隔；留空则全部填 0")]
        public string colorIdsText = "";

        public float jumpStartInterval = 0.1f;
        public float jumpSpawnYOffset = 0.5f;

        private void OnEnable()
        {
            int computed;
            if (Group != null)
            {
                Group.RebuildGrid();
                computed = BoxItem.ComputeCapacity(Group, ColMin, RowMin, ColMax, RowMax);
            }
            else
            {
                computed = (ColMax - ColMin + 1) * (RowMax - RowMin + 1);
            }

            if (capacity <= 0)
                capacity = computed;
            if (string.IsNullOrWhiteSpace(colorIdsText))
                colorIdsText = string.Join(",", new int[Mathf.Max(1, capacity)]);
        }

        private void OnWizardCreate()
        {
            if (Group == null)
            {
                Debug.LogError("[BoxCreator] 未找到 PixelGroup。");
                return;
            }

            // 把「移除区域 Pixel + 创建箱子」合并为一步撤销
            Undo.SetCurrentGroupName("创建箱子");
            int undoGroup = Undo.GetCurrentGroup();

            // 移除箱子区域内的 Pixel（箱子区域应为空，开箱后的 Pixel 由 colorIds 提供）
            RemovePixelsInRegion();

            int[] colorIds;
            if (string.IsNullOrWhiteSpace(colorIdsText))
            {
                colorIds = new int[Mathf.Max(1, capacity)];
            }
            else
            {
                var parts = colorIdsText.Split(',');
                var list = new List<int>();
                foreach (var s in parts)
                {
                    if (int.TryParse(s.Trim(), out int v))
                        list.Add(v);
                }
                colorIds = list.ToArray();
            }

            if (colorIds.Length != capacity)
                Debug.LogWarning("[BoxCreator] colorIds 数量(" + colorIds.Length + ") 与 capacity(" + capacity +
                    ") 不一致，运行时按较小值处理。");

            var go = new GameObject("Box_" + RowMin + "_" + ColMin);
            go.transform.SetParent(Group.transform, false);
            go.transform.localPosition = Vector3.zero;

            var box = go.AddComponent<BoxItem>();
            box.colMin = ColMin;
            box.rowMin = RowMin;
            box.colMax = ColMax;
            box.rowMax = RowMax;
            box.capacity = capacity;
            box.colorIds = colorIds;
            box.jumpStartInterval = jumpStartInterval;
            box.jumpSpawnYOffset = jumpSpawnYOffset;
            box.cornerPrefab = Group.boxCornerPrefab;
            box.edgePrefab = Group.boxEdgePrefab;
            box.centerPrefab = Group.boxCenterPrefab;

            Undo.RegisterCreatedObjectUndo(go, "创建箱子");
            Undo.CollapseUndoOperations(undoGroup);

            Group.RebuildGrid();
            EditorUtility.SetDirty(Group);
            Selection.activeGameObject = go;

            Debug.Log("[BoxCreator] 已创建箱子：(" + ColMin + "," + RowMin + ")~(" + ColMax + "," + RowMax +
                ")，容量 " + capacity + "，颜色 [" + string.Join(",", colorIds) + "]。");
        }

        /// <summary>销毁箱子矩形区域内所有 PixelItem（含两角之间的全部格子，不止选中的两个）。</summary>
        private void RemovePixelsInRegion()
        {
            var pixels = Group.GetComponentsInChildren<PixelItem>();
            int removed = 0;
            for (int i = pixels.Length - 1; i >= 0; i--)
            {
                var p = pixels[i];
                if (p == null)
                    continue;
                if (p.gridX < ColMin || p.gridX > ColMax || p.gridZ < RowMin || p.gridZ > RowMax)
                    continue;
                Undo.DestroyObjectImmediate(p.gameObject);
                removed++;
            }
            if (removed > 0)
                Debug.Log("[BoxCreator] 已移除箱子区域内的 " + removed + " 个 Pixel。");
        }
    }
}
