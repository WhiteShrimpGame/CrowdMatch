using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CrowdMatch
{
    [CustomEditor(typeof(PixelItem))]
    [CanEditMultipleObjects]
    public class PixelItemEditor : Editor
    {
        private int batchColorId;
        private ColorConfig colorConfig;

        private void OnEnable()
        {
            colorConfig = ColorConfigLocator.Find();
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("批量设置颜色", EditorStyles.boldLabel);

            colorConfig = (ColorConfig)EditorGUILayout.ObjectField("颜色配置", colorConfig, typeof(ColorConfig), false);
            if (colorConfig == null)
                colorConfig = ColorConfigLocator.Find();

            int maxId = colorConfig != null ? colorConfig.Count - 1 : 0;
            maxId = Mathf.Max(0, maxId);
            batchColorId = EditorGUILayout.IntSlider("颜色 ID", batchColorId, 0, maxId);

            DrawPalette();

            string label = targets.Length > 1
                ? "应用到 " + targets.Length + " 个选中单位"
                : "应用颜色";

            if (GUILayout.Button(label))
            {
                ApplyColorToAll();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("问号 Pixel", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("标记为问号 Pixel"))
                SetQuestionAll(true);
            if (GUILayout.Button("取消问号标记"))
                SetQuestionAll(false);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPalette()
        {
            if (colorConfig == null || colorConfig.Count == 0)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("调色板（点击选择颜色 ID）", EditorStyles.boldLabel);

            const int perRow = 8;
            for (int i = 0; i < colorConfig.Count; i++)
            {
                if (i % perRow == 0)
                    EditorGUILayout.BeginHorizontal();

                Color c = colorConfig.GetColor(i);
                Color prev = GUI.backgroundColor;
                GUI.backgroundColor = c;
                if (GUILayout.Button(i.ToString(), GUILayout.Width(26f), GUILayout.Height(26f)))
                    batchColorId = i;
                GUI.backgroundColor = prev;

                if (i % perRow == perRow - 1 || i == colorConfig.Count - 1)
                    EditorGUILayout.EndHorizontal();
            }
        }

        private void ApplyColorToAll()
        {
            foreach (var t in targets)
            {
                var item = (PixelItem)t;

                Undo.RecordObject(item, "Set Pixel Color");
                foreach (var r in item.renderers)
                {
                    if (r != null)
                        Undo.RecordObject(r, "Set Pixel Material");
                }

                item.colorId = batchColorId;
                item.ApplyMaterial(colorConfig);
                EditorUtility.SetDirty(item);
            }
        }

        private void SetQuestionAll(bool question)
        {
            foreach (var t in targets)
            {
                var item = (PixelItem)t;

                Undo.RecordObject(item, question ? "Mark Question Pixel" : "Unmark Question Pixel");
                foreach (var r in item.renderers)
                {
                    if (r != null)
                        Undo.RecordObject(r, question ? "Mark Question Pixel" : "Unmark Question Pixel");
                }
                if (item.questionObject != null)
                    Undo.RecordObject(item.questionObject, question ? "Mark Question Pixel" : "Unmark Question Pixel");

                item.isQuestion = question;
                item.ApplyMaterial(colorConfig);
                item.RefreshQuestionObject();
                EditorUtility.SetDirty(item);
            }
        }
    }

    /// <summary>用选中的两个 Pixel 作为矩形对角（左上 + 右下），清除矩形范围内所有 PixelItem（支持 Undo）。</summary>
    public static class PixelRectClearer
    {
        private const string Tag = "[PixelRectClearer]";

        [MenuItem("CrowdMatch/清除选中矩形范围 Pixel %#x", true)]
        private static bool ValidateClearRectFromSelection()
        {
            return CollectSelectedPixels().Count == 2;
        }

        [MenuItem("CrowdMatch/清除选中矩形范围 Pixel %#x")]
        private static void ClearRectFromSelection()
        {
            var pixels = CollectSelectedPixels();
            if (pixels.Count != 2)
            {
                EditorUtility.DisplayDialog("清除矩形范围 Pixel", "请精确选中 2 个 PixelItem（矩形对角两个角，如左上 + 右下）。", "确定");
                return;
            }

            var a = pixels[0];
            var b = pixels[1];

            var group = a.GetComponentInParent<PixelGroup>();
            if (group == null || b.GetComponentInParent<PixelGroup>() != group)
            {
                EditorUtility.DisplayDialog("清除矩形范围 Pixel", "选中的两个 Pixel 必须属于同一个 PixelGroup。", "确定");
                return;
            }

            // 矩形对角归一化（不依赖选点先后顺序，任一对角都可）
            int minX = Mathf.Min(a.gridX, b.gridX);
            int maxX = Mathf.Max(a.gridX, b.gridX);
            int minZ = Mathf.Min(a.gridZ, b.gridZ);
            int maxZ = Mathf.Max(a.gridZ, b.gridZ);

            group.RebuildGrid();

            int removed = 0;
            for (int r = minZ; r <= maxZ; r++)
            {
                for (int c = minX; c <= maxX; c++)
                {
                    var item = group.GetItem(c, r);
                    if (item == null)
                        continue;
                    Undo.DestroyObjectImmediate(item.gameObject);
                    removed++;
                }
            }

            group.RebuildGrid();
            if (Application.isPlaying)
                group.RefreshExposed();
            EditorUtility.SetDirty(group);

            Debug.Log(Tag + " 已清除矩形范围 (" + minX + ", " + minZ + ") → (" + maxX + ", " + maxZ + ") 内的 " +
                removed + " 个 Pixel（父物体 " + group.name + "）。");
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
}
