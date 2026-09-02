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
        [Tooltip("关卡 JSON 列表（调试用，优先级高于 levelDataConfig；非空时按序号循环取关）")]
        public List<TextAsset> levelJsons = new List<TextAsset>();

        [Tooltip("关卡编排 ScriptableObject（顺序关 + 循环关）。levelJsons 为空时使用")]
        public LevelDataConfig levelDataConfig;

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
                GameData.CurrentLevel = Mathf.Max(1, GameData.CurrentLevel - 1);
                GameData.FailCount = 0;
                ReloadLevel();
            }
        }
#endif

        /// <summary>
        /// 按关卡序号（1 起）解析对应 JSON TextAsset。解析顺序：
        ///   1. levelJsons（调试列表）非空 → 按序号循环取关
        ///   2. levelDataConfig（顺序关 + 循环关）
        ///   3. 都未配置 → 报错并返回 null
        /// </summary>
        public TextAsset GetLevelJson(int level)
        {
            // 优先级 1：调试列表 levelJsons（非空时循环取关）
            if (levelJsons != null && levelJsons.Count > 0)
                return levelJsons[(level - 1) % levelJsons.Count];

            // 优先级 2：ScriptableObject 关卡编排（含循环关）
            if (levelDataConfig != null)
                return levelDataConfig.GetLevel(level);

            Debug.LogError("[GameManager] 未配置关卡来源：请分配 GameManager.levelJsons 或 GameManager.levelDataConfig。");
            return null;
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
