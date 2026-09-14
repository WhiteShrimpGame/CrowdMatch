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
}
