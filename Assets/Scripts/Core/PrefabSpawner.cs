using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CrowdMatch
{
    /// <summary>
    /// 预制体实例化的统一入口：编辑器非运行状态下用 PrefabUtility.InstantiatePrefab，
    /// 让生成的物体保持与预制体的关联（与手动拖入预制体一致，可 Apply/Revert 覆盖）；
    /// 运行期与构建后用 Object.Instantiate。
    /// 供关卡 JSON 在编辑器下导入时保持预制体形态使用。
    /// </summary>
    public static class PrefabSpawner
    {
        /// <summary>实例化预制体并挂到 parent 下；prefab 为空时返回 null。</summary>
        public static GameObject Instantiate(GameObject prefab, Transform parent)
        {
            if (prefab == null)
                return null;

#if UNITY_EDITOR
            PrefabAssetType type = PrefabUtility.GetPrefabAssetType(prefab);
            if (!Application.isPlaying &&
                type != PrefabAssetType.NotAPrefab && type != PrefabAssetType.MissingAsset)
                return (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
#endif

            return Object.Instantiate(prefab, parent);
        }
    }
}
