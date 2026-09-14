using UnityEngine;
using UnityEditor;

namespace CrowdMatch
{
    [CustomEditor(typeof(ContainerItem))]
    [CanEditMultipleObjects]
    public class ContainerItemEditor : Editor
    {
        private ColorConfig colorConfig;

        private void OnEnable()
        {
            colorConfig = ColorConfigLocator.Find();
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("问号车", EditorStyles.boldLabel);

            colorConfig = (ColorConfig)EditorGUILayout.ObjectField("颜色配置", colorConfig, typeof(ColorConfig), false);
            if (colorConfig == null)
                colorConfig = ColorConfigLocator.Find();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("标记为问号车"))
                SetQuestionAll(true);
            if (GUILayout.Button("取消问号标记"))
                SetQuestionAll(false);
            EditorGUILayout.EndHorizontal();
        }

        private void SetQuestionAll(bool question)
        {
            foreach (var t in targets)
            {
                var item = (ContainerItem)t;

                Undo.RecordObject(item, question ? "Mark Question Container" : "Unmark Question Container");
                if (item.materialReplacements != null)
                {
                    foreach (var rep in item.materialReplacements)
                    {
                        if (rep != null && rep.renderer != null)
                            Undo.RecordObject(rep.renderer, question ? "Mark Question Container" : "Unmark Question Container");
                    }
                }
                if (item.questionObject != null)
                    Undo.RecordObject(item.questionObject, question ? "Mark Question Container" : "Unmark Question Container");

                item.isQuestion = question;
                item.ApplyMaterial(colorConfig);
                item.RefreshQuestionObject();
                EditorUtility.SetDirty(item);
            }
        }
    }
}
