using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 全局单例，通过 DefaultExecutionOrder 保证最先执行 Awake。
    /// 挂到一个场景物体上即可，引用 ColorConfig 颜色配置。
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("配置")]
        [Tooltip("颜色配置 ScriptableObject，提供 24 种基础颜色材质")]
        public ColorConfig colorConfig;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
        }
    }
}
