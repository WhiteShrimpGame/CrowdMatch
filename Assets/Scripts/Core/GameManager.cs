using System.Collections.Generic;
using UnityEngine;
#if WeChat
using WeChatWASM;
#endif

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

        [Tooltip("音频配置 ScriptableObject（tag → clip → volume）；留空则不做任何音频初始化")]
        public AudioConfig audioConfig;

        [Header("关卡")]
        [Tooltip("关卡 JSON 列表（调试用，优先级高于 levelDataConfig；非空时按序号循环取关）")]
        public List<TextAsset> levelJsons = new List<TextAsset>();

        [Tooltip("关卡编排 ScriptableObject（顺序关 + 循环关）。levelJsons 为空时使用")]
        public LevelDataConfig levelDataConfig;

        [Header("对象池")]
        [Tooltip("对象池配置资产（tag → prefab → preloadCount）；留空则跳过对象池初始化")]
        public SpawnPoolConfig spawnPoolConfig;

        [Tooltip("对象池预加载与回收对象的父物体")]
        public Transform spawnPoolRoot;

        public SpawnPool spawnPool;

        [Header("表情")]
        [Tooltip("表情包管理器（场景里单独建一个物体挂上，再拖到这里）；留空则所有表情播放自动跳过")]
        public EmojiManager emojiManager;

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

            // 音频：挂上 AudioManager 并注入配置（AudioSource 首次 Play 时才懒创建）
            var audioManager = gameObject.AddComponent<AudioManager>();
            audioManager.Init(audioConfig);
            audioManager.MusicEnabled = PlayerPrefs.GetInt("Music", 1) == 1;
            audioManager.SoundEnabled = PlayerPrefs.GetInt("Sound", 1) == 1;
            if (audioConfig != null)
                audioManager.Play("BGM", loop: true);   // 背景音乐循环播放（跨关卡不重播）

            // 对象池：配置为空时跳过（未使用池化也能正常跑）
            if (spawnPoolConfig != null)
            {
                spawnPool = new SpawnPool();
                spawnPool.Init(spawnPoolConfig, spawnPoolRoot);
            }
        }

        // ========== Debug / 调试 ==========

#if UNITY_EDITOR
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.N))
            {
                GameWin();
            }
            else if (Input.GetKeyDown(KeyCode.B))
            {
                PrevLevel();
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

        /// <summary>上一关：关卡序号 -1（不低于 1），连败清零，重载关卡。</summary>
        public void PrevLevel()
        {
            GameData.CurrentLevel = Mathf.Max(1, GameData.CurrentLevel - 1);
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
            CleanupSpawnPool();   // 关卡重建前回收对象池：在用对象全部归还并裁回 preloadCount
            GameData.Init(true);
            var gc = GameController.Instance;
            if (gc != null)
                gc.ReloadLevel();
        }

        /// <summary>对象池清理：归还所有在用对象并把池裁回 preloadCount。场景切换 / 关卡重建前调用。</summary>
        public void CleanupSpawnPool()
        {
            if (spawnPool != null)
            {
                spawnPool.GC(true);
            }
        }

        // ========== 震动 / Vibration ==========

        /// <summary>
        /// 触发震动。level：0 = 轻，1 = 中，2 = 重，其他 = 长震。
        /// 目前只实现微信小游戏平台（需定义 WeChat 宏并引入 WX SDK），其余平台为空实现。
        /// </summary>
        public void TriggerVibrate(int level)
        {
#if WeChat
            switch (level)
            {
                case 0:
                    WX.VibrateShort(new VibrateShortOption()
                    {
                        type = "light",
                        success = null,
                        fail = null,
                        complete = null,
                    });
                    break;
                case 1:
                    WX.VibrateShort(new VibrateShortOption()
                    {
                        type = "medium",
                        success = null,
                        fail = null,
                        complete = null,
                    });
                    break;
                case 2:
                    WX.VibrateShort(new VibrateShortOption()
                    {
                        type = "heavy",
                        success = null,
                        fail = null,
                        complete = null,
                    });
                    break;
                default:
                    WX.VibrateLong(new VibrateLongOption() { fail = null, complete = null, success = null });
                    break;
            }
#endif
        }
    }
}
