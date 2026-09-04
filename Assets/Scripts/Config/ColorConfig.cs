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

        [Tooltip("车体材质，按颜色 ID 索引（用于 ContainerItem 替换项中的「车材质」）")]
        public Material[] carMaterials = new Material[0];

        [Tooltip("车内部材质，按颜色 ID 索引（用于 ContainerItem 替换项中的「车内部材质」）")]
        public Material[] interiorMaterials = new Material[0];

        /// <summary>材质数量（即颜色总数）</summary>
        public int Count => materials != null ? materials.Length : 0;

        /// <summary>根据颜色 ID 返回基础材质，越界返回 null</summary>
        public Material GetMaterial(int colorId) => GetMaterialFrom(materials, colorId);

        /// <summary>根据颜色 ID 返回车体材质，越界返回 null</summary>
        public Material GetCarMaterial(int colorId) => GetMaterialFrom(carMaterials, colorId);

        /// <summary>根据颜色 ID 返回车内部材质，越界返回 null</summary>
        public Material GetInteriorMaterial(int colorId) => GetMaterialFrom(interiorMaterials, colorId);

        /// <summary>根据颜色 ID 返回颜色，越界返回洋红以便提示</summary>
        public Color GetColor(int colorId)
        {
            var mat = GetMaterial(colorId);
            return mat != null ? mat.color : Color.magenta;
        }

        private static Material GetMaterialFrom(Material[] arr, int colorId)
        {
            if (arr == null || colorId < 0 || colorId >= arr.Length)
                return null;
            return arr[colorId];
        }
    }
}
