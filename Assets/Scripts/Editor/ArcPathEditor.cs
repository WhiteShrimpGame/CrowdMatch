using UnityEditor;
using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// ArcPathController 的自定义编辑器：Scene 视图绘制预览轨迹 + Inspector 面板测试控制。
    /// Custom editor for ArcPathController: Scene-view preview + Inspector test controls.
    /// 注意：本文件依赖 UnityEditor，必须放在项目的 Editor/ 目录下（或包裹 #if UNITY_EDITOR）。
    /// Note: this file requires UnityEditor and must live in an Editor/ folder (or be wrapped in #if UNITY_EDITOR).
    /// </summary>
    [CustomEditor(typeof(ArcPathController))]
    public class ArcPathEditor : Editor
    {
        private ArcPathController _controller;

        private void OnEnable()
        {
            _controller = (ArcPathController)target;
        }

        /// <summary>
        /// Scene 视图绘制预览轨迹。
        /// Draws the preview path in the Scene view.
        /// </summary>
        private void OnSceneGUI()
        {
            if (_controller.arcPaths.Count == 0) return;
            if (_controller.previewSegments <= 0) return;

            _controller.InitializePaths();
            Handles.color = _controller.previewColor;

            foreach (var path in _controller.arcPaths)
            {
                DrawSingleArcPreview(path);
            }
        }

        /// <summary>
        /// 绘制单段圆弧预览。
        /// Draws a single segment preview.
        /// </summary>
        private void DrawSingleArcPreview(ArcPath path)
        {
            float totalLength = path.GetTotalLength();
            if (totalLength <= 0) return;

            Vector3 prevPoint = path.startPosition;

            // 分段绘制平滑圆弧 / draw smooth arc in segments
            for (int i = 1; i <= _controller.previewSegments; i++)
            {
                float t = (float)i / _controller.previewSegments;
                Vector3 currentPoint = path.GetPositionByLength(t * totalLength);

                Handles.DrawLine(prevPoint, currentPoint);
                prevPoint = currentPoint;
            }
        }

        /// <summary>
        /// 自定义 Inspector 面板。
        /// Custom Inspector panel.
        /// </summary>
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            GUILayout.Space(10);

            // 绘制初始化按钮 / draw the initialize button
            if (GUILayout.Button("🔄 初始化并衔接所有轨迹", GUILayout.Height(30)))
            {
                _controller.InitializePaths();
                EditorUtility.SetDirty(_controller);
            }

            GUILayout.Space(5);
            EditorGUILayout.HelpBox("点击按钮自动将后一段轨迹的起点衔接前一段的终点", MessageType.Info);

            GUILayout.Space(10);
            GUILayout.Label("=== 测试运动控制 ===", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _controller.testObject = (Transform)EditorGUILayout.ObjectField("测试物体", _controller.testObject, typeof(Transform), true);
            _controller.previewPosition = EditorGUILayout.Slider("轨迹位置 (0~1)", _controller.previewPosition, 0, 1);
            if (EditorGUI.EndChangeCheck())
            {
                UpdateTestObject(_controller);
                EditorUtility.SetDirty(_controller);
            }
        }

        private void UpdateTestObject(ArcPathController controller)
        {
            if (controller.testObject == null) return;
            if (controller.arcPaths.Count == 0) return;

            controller.InitializePaths();
            float totalLen = controller.GetTotalPathLength();
            float targetLen = totalLen * controller.previewPosition;

            Vector3 pos = controller.GetGlobalPosition(targetLen);
            Vector3 euler = controller.GetGlobalEulerAngles(targetLen);

            controller.testObject.position = pos;
            controller.testObject.rotation = Quaternion.Euler(euler);
        }
    }
}
