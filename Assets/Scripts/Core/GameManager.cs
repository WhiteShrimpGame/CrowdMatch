using System.Collections.Generic;
using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 全局单例，通过 DefaultExecutionOrder 保证最先执行 Awake。
    /// 挂到一个场景物体上即可，引用 ColorConfig 颜色配置与关卡 JSON 列表。
    /// 关卡流程的「裁决层」：持有关卡配置，并提供 GameWin / GameFail 两种过渡入口。
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("配置")]
        [Tooltip("颜色配置 ScriptableObject，提供 24 种基础颜色材质")]
        public ColorConfig colorConfig;

        [Header("关卡")]
        [Tooltip("关卡 JSON 列表（按关卡序号 1..N 依次对应），通过 TextAsset 访问关卡循环")]
        public List<TextAsset> levelJsons = new List<TextAsset>();

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

        // ========== Debug / 调试 ==========

#if UNITY_EDITOR
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.N))
            {
                GameWin();
                ReloadLevel();
            }
            else if (Input.GetKeyDown(KeyCode.B))
            {
                GameData.CurrentLevel--;
                GameData.FailCount = 0;
                ReloadLevel();
            }
        }
#endif

        /// <summary>按关卡序号（1 起）取对应 JSON TextAsset；越界返回 null。</summary>
        public TextAsset GetLevelJson(int level)
        {
            int idx = level - 1;
            if (levelJsons == null || idx < 0 || idx >= levelJsons.Count)
                return null;
            return levelJsons[idx];
        }

        /// <summary>胜利：进入下一关（关卡序号 +1，连败清零，重载关卡）。</summary>
        public void GameWin()
        {
            GameData.CurrentLevel++;
            GameData.FailCount = 0;
            ReloadLevel();
        }

        /// <summary>失败：重置当前关（关卡序号不变，连败 +1，重载关卡）。</summary>
        public void GameFail()
        {
            GameData.FailCount++;
            ReloadLevel();
        }

        /// <summary>重载当前关卡（原地重建，不重载场景）：重置计数后交由 GameController 重新初始化。</summary>
        private void ReloadLevel()
        {
            GameData.Init(true);
            var gc = GameController.Instance;
            if (gc != null)
                gc.ReloadLevel();
        }
    }
}
