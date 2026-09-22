using System.Collections.Generic;
using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// Lightweight audio manager — singleton MonoBehaviour with tag-based lookup.
    /// AudioSource GameObjects are created lazily on first Play() call.
    /// Config is a ScriptableObject (AudioConfig) with tag → clip → volume mappings.
    ///
    /// 轻量级音频管理器——基于 tag 索引的单例 MonoBehaviour。
    /// AudioSource 子对象在首次 Play() 时懒创建。
    /// 配置使用 ScriptableObject（AudioConfig），包含 tag → clip → volume 映射。
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        // ============================================================
        // Singleton / 单例
        // ============================================================

        public static AudioManager Instance { get; private set; }

        // ============================================================
        // Public settings / 公开设置
        // ============================================================

        /// <summary>Master music switch. Set from your settings UI. / 音乐总开关，由设置界面写入。</summary>
        public bool MusicEnabled { get; set; } = true;

        /// <summary>Master sound-effect switch. Set from your settings UI. / 音效总开关，由设置界面写入。</summary>
        public bool SoundEnabled { get; set; } = true;

        /// <summary>The config asset. Assign via Init() or directly in code. / 配置资产，通过 Init() 或直接赋值。</summary>
        public AudioConfig Config { get; set; }

        // ============================================================
        // Private state / 内部状态
        // ============================================================

        /// <summary>tag → AudioSource (null until first Play). / tag → AudioSource（首次 Play 前为 null）。</summary>
        private Dictionary<string, AudioSource> _sourceDict = new Dictionary<string, AudioSource>();

        /// <summary>tag → cooldown end timestamp (for PlayNoInterrupt). / tag → 冷却结束时间戳（用于 PlayNoInterrupt）。</summary>
        private Dictionary<string, float> _endTimeDict = new Dictionary<string, float>();

        /// <summary>tag → reference count (for PlayMulti/StopMulti). / tag → 引用计数（用于 PlayMulti/StopMulti）。</summary>
        private Dictionary<string, int> _multiCountDict = new Dictionary<string, int>();

        // ============================================================
        // Unity Lifecycle / Unity 生命周期
        // ============================================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // ============================================================
        // Init / 初始化
        // ============================================================

        /// <summary>
        /// Set the config. Call once at startup (e.g. in GameManager.Awake).
        /// 设置配置。在启动时调用一次（如在 GameManager.Awake 中）。
        /// </summary>
        /// <param name="config">The AudioConfig asset. / AudioConfig 资产。</param>
        public void Init(AudioConfig config)
        {
            Config = config;

            // Pre-validate tags — register all known tags, Source created lazily.
            // 预校验所有 tag——注册已知 tag，AudioSource 懒创建。
            if (config == null || config.items == null) return;

            foreach (var item in config.items)
            {
                if (string.IsNullOrEmpty(item.tag))
                {
                    Debug.LogError("[AudioManager] Config item has empty tag. Skipped.");
                    continue;
                }
                if (_sourceDict.ContainsKey(item.tag))
                {
                    Debug.LogError($"[AudioManager] Duplicate tag '{item.tag}' in config. First occurrence kept.");
                    continue;
                }
                _sourceDict[item.tag] = null; // created on first Play / 首次 Play 时创建
            }
        }

        // ============================================================
        // Play / 播放
        // ============================================================

        /// <summary>
        /// Play a clip by tag. Source is created lazily on first call.
        /// 按 tag 播放音频。AudioSource 在首次调用时懒创建。
        /// </summary>
        /// <param name="tag">Tag defined in AudioConfig. / AudioConfig 中定义的 tag。</param>
        /// <param name="volMultiplier">Multiplier on top of the config volume. / 在配置音量基础上的倍率。</param>
        /// <param name="loop">Whether to loop. / 是否循环播放。</param>
        public void Play(string tag, float volMultiplier = 1f, bool loop = false)
        {
            if (Config == null)
            {
                Debug.LogError("[AudioManager] Config is null. Call Init() first.");
                return;
            }

            var item = GetConfigItem(tag);
            if (item == null)
            {
                Debug.LogError($"[AudioManager] Tag '{tag}' not found in AudioConfig.");
                return;
            }

            // Check master switches / 检查总开关
            if (item.isBGM && SettingData.MusicSet == 0) return;
            if (!item.isBGM && SettingData.SoundSet == 0) return;

            var source = GetOrCreateSource(tag, item);
            if (source == null) return;

            source.volume = item.volume * volMultiplier;
            source.loop = loop;
            source.Play();
        }

        // ============================================================
        // Stop / 停止
        // ============================================================

        /// <summary>
        /// Stop a clip by tag.
        /// 按 tag 停止音频。
        /// </summary>
        public void Stop(string tag)
        {
            if (!_sourceDict.TryGetValue(tag, out var source))
            {
                Debug.LogError($"[AudioManager] Tag '{tag}' not found. Cannot stop.");
                return;
            }

            if (source != null)
            {
                source.Stop();
            }
        }

        // ============================================================
        // PlayNoInterrupt / 防打断播放
        // ============================================================

        /// <summary>
        /// Play with a cooldown — subsequent calls within holdTime seconds are ignored.
        /// Useful for preventing sound spam (e.g. rapid collision sounds).
        /// 带冷却时间的播放——holdTime 秒内的重复调用会被忽略。
        /// 用于防止声音刷屏（如快速碰撞音效）。
        /// </summary>
        /// <param name="tag">Tag defined in AudioConfig.</param>
        /// <param name="holdTime">Cooldown in seconds. / 冷却时间（秒）。</param>
        /// <param name="volMultiplier">Volume multiplier. / 音量倍率。</param>
        /// <param name="loop">Whether to loop. / 是否循环。</param>
        public void PlayNoInterrupt(string tag, float holdTime, float volMultiplier = 1f, bool loop = false)
        {
            if (Config == null)
            {
                Debug.LogError("[AudioManager] Config is null. Call Init() first.");
                return;
            }

            if (GetConfigItem(tag) == null)
            {
                Debug.LogError($"[AudioManager] Tag '{tag}' not found in AudioConfig.");
                return;
            }

            if (!_endTimeDict.ContainsKey(tag))
            {
                _endTimeDict.Add(tag, Time.time + holdTime);
                Play(tag, volMultiplier, loop);
            }
            else if (_endTimeDict[tag] < Time.time)
            {
                _endTimeDict[tag] = Time.time + holdTime;
                Play(tag, volMultiplier, loop);
            }
            // else: still in cooldown, skip / 冷却中，跳过
        }

        // ============================================================
        // PlayMulti / StopMulti — reference-counted playback
        // 引用计数播放 / 停止
        // ============================================================

        /// <summary>
        /// Play with reference counting. Each call increments the counter.
        /// The clip loops until StopMulti() brings the count to zero.
        /// Useful when multiple systems want the same looping sound (e.g. tape rolling).
        /// 引用计数播放。每次调用计数器 +1。
        /// 音频循环播放，直到 StopMulti() 将计数归零后才停止。
        /// 适用于多个系统需要同一个循环音效的场景（如胶带滚动声）。
        /// </summary>
        public void PlayMulti(string tag, float volMultiplier = 1f)
        {
            if (Config == null)
            {
                Debug.LogError("[AudioManager] Config is null. Call Init() first.");
                return;
            }

            if (GetConfigItem(tag) == null)
            {
                Debug.LogError($"[AudioManager] Tag '{tag}' not found in AudioConfig.");
                return;
            }

            if (!_multiCountDict.ContainsKey(tag))
            {
                _multiCountDict.Add(tag, 1);
                Play(tag, volMultiplier, loop: true);
            }
            else
            {
                _multiCountDict[tag]++;
            }
        }

        /// <summary>
        /// Decrement the reference count. When it reaches zero, the clip stops.
        /// 引用计数 -1。归零时停止音频。
        /// </summary>
        public void StopMulti(string tag)
        {
            if (!_multiCountDict.ContainsKey(tag))
            {
                Stop(tag);
                return;
            }

            _multiCountDict[tag]--;
            if (_multiCountDict[tag] <= 0)
            {
                _multiCountDict.Remove(tag);
                Stop(tag);
            }
        }

        // ============================================================
        // StopAll / StopAllMulti — bulk cleanup / 批量清理
        // ============================================================

        /// <summary>
        /// Stop all playing clips and clear all internal dictionaries.
        /// Call this before scene transitions.
        /// 停止所有正在播放的音频，清空所有内部字典。
        /// 在场景切换前调用。
        /// </summary>
        public void StopAll()
        {
            foreach (var kvp in _sourceDict)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.Stop();
                }
            }

            _endTimeDict.Clear();
            _multiCountDict.Clear();
        }

        /// <summary>
        /// Stop all reference-counted clips and clear the multi-count dictionary.
        /// 停止所有引用计数的音频并清空引用计数字典。
        /// </summary>
        public void StopAllMulti()
        {
            foreach (var kvp in _multiCountDict)
            {
                if (_sourceDict.TryGetValue(kvp.Key, out var source) && source != null)
                {
                    source.Stop();
                }
            }

            _multiCountDict.Clear();
        }

        // ============================================================
        // Legacy compatibility / 业务兼容方法
        // ============================================================

        /// <summary>
        /// [Business-layer stub] Play button click sound + trigger vibration.
        /// Leave this empty in the base skill — implement vibration logic
        /// in your project's business layer.
        /// 【业务层桩方法】播放按钮点击音效 + 触发振动。
        /// skill 中留空——在具体项目的业务层中按需实现振动逻辑。
        /// </summary>
        public void PlayButtonAudioAndVibrate()
        {
            Play("Button");

            // TODO: Add your project's vibration call here.
            // 在此处添加你的项目的振动调用。
        }

        // ============================================================
        // Private helpers / 内部辅助
        // ============================================================

        /// <summary>
        /// Get or lazily-create the AudioSource child GameObject for a tag.
        /// 获取或懒创建一个 tag 对应的 AudioSource 子对象。
        /// </summary>
        private AudioSource GetOrCreateSource(string tag, AudioItem item)
        {
            if (_sourceDict.TryGetValue(tag, out var existing) && existing != null)
            {
                return existing;
            }

            // Create child GameObject / 创建子对象
            var go = new GameObject($"Audio_{tag}");
            go.transform.SetParent(transform);

            var source = go.AddComponent<AudioSource>();
            source.clip = item.clip;
            source.playOnAwake = false;

            _sourceDict[tag] = source;
            return source;
        }

        /// <summary>
        /// Look up an AudioItem by tag. Returns null if not found.
        /// 按 tag 查找 AudioItem。未找到返回 null。
        /// </summary>
        private AudioItem GetConfigItem(string tag)
        {
            if (Config == null || Config.items == null) return null;

            foreach (var item in Config.items)
            {
                if (item.tag == tag) return item;
            }
            return null;
        }
    }
}
