using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 颜色配置：存放 24 种基础颜色材质，通过颜色 ID 取材质。
    /// 用菜单 CrowdMatch → Create Color Config 可一键生成资产与 24 个材质。
    /// </summary>
    [CreateAssetMenu(menuName = "CrowdMatch/Color Config", fileName = "ColorConfig")]
    public class ColorConfig : ScriptableObject
    {
        [Tooltip("按颜色 ID 索引的材质数组，长度应为 24")]
        public Material[] materials = new Material[0];

        /// <summary>材质数量（即颜色总数）</summary>
        public int Count => materials != null ? materials.Length : 0;

        /// <summary>根据颜色 ID 返回材质，越界返回 null</summary>
        public Material GetMaterial(int colorId)
        {
            if (materials == null || colorId < 0 || colorId >= materials.Length)
                return null;
            return materials[colorId];
        }

        /// <summary>根据颜色 ID 返回颜色，越界返回洋红以便提示</summary>
        public Color GetColor(int colorId)
        {
            var mat = GetMaterial(colorId);
            return mat != null ? mat.color : Color.magenta;
        }
    }
}
