using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 编辑器便利：每次停止运行（回到编辑模式）后，把场景里 GameController 的 Record 模式复位为 false 并标记脏，
    /// 避免上一次录制忘了关，下次 Play 又误录一遍。
    /// </summary>
    [InitializeOnLoad]
    public static class RecordModeAutoReset
    {
        static RecordModeAutoReset()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // 必须等回到编辑模式、场景已从运行状态还原之后再改；在 ExitingPlayMode 改会被还原覆盖。
            if (state != PlayModeStateChange.EnteredEditMode)
                return;

            // 用 FindObjectsOfTypeAll 以覆盖被禁用的对象；再排除预制体资产，避免改到工程里的资源。
            foreach (var gc in Resources.FindObjectsOfTypeAll<GameController>())
            {
                if (gc == null || EditorUtility.IsPersistent(gc) || !gc.recordMode)
                    continue;

                gc.recordMode = false;
                EditorUtility.SetDirty(gc);

                var scene = gc.gameObject.scene;
                if (scene.IsValid())
                    EditorSceneManager.MarkSceneDirty(scene);

                Debug.Log("[RecordModeAutoReset] 已把 " + gc.name + " 的 Record 模式置为 false。");
            }
        }
    }
}
