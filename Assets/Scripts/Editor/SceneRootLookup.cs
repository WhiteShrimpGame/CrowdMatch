using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrowdMatch
{
    /// <summary>
    /// 「在当前打开的场景里找人」的小工具：**只在根物体上找** —— 不递归子物体、不扫预制体资产。
    ///
    /// 用途：编辑器窗口没绑到目标组件时（没选中任何东西、或选中的不是它）退一步自动绑一个。
    /// 组通常就挂在根节点上，所以这样既便宜（不遍历整棵树）又不会误抓到嵌套实例 / 别的场景里的组。
    /// 找不到就返回 null，调用方保持未绑定即可（**不报错**）。
    /// </summary>
    internal static class SceneRootLookup
    {
        /// <summary>在当前打开场景（active scene）的根物体上找第一个 <typeparamref name="T"/>；没有则 null。</summary>
        public static T FindComponent<T>() where T : Component
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.isLoaded)
                return null;

            var roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                var found = roots[i].GetComponent<T>();
                if (found != null)
                    return found;
            }
            return null;
        }
    }
}
