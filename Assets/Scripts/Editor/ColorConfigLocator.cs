using UnityEngine;
using UnityEditor;

namespace CrowdMatch
{
    /// <summary>编辑器辅助：查找场景 / 项目中的 ColorConfig</summary>
    public static class ColorConfigLocator
    {
        public static ColorConfig Find()
        {
            if (Application.isPlaying && GameManager.Instance != null && GameManager.Instance.colorConfig != null)
                return GameManager.Instance.colorConfig;

            var guids = AssetDatabase.FindAssets("t:ColorConfig");
            if (guids != null && guids.Length > 0)
                return AssetDatabase.LoadAssetAtPath<ColorConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));

            return null;
        }
    }
}
