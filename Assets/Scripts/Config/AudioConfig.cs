using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// ScriptableObject config for AudioManager.
    /// Create via: Assets → Create → Audio → AudioConfig
    /// AudioManager 的 ScriptableObject 配置文件。
    /// 创建方式：Assets → Create → Audio → AudioConfig
    /// </summary>
    [CreateAssetMenu(menuName = "Audio/AudioConfig")]
    public class AudioConfig : ScriptableObject
    {
        public AudioItem[] items;
    }

    /// <summary>
    /// A single audio entry — one tag maps to one AudioClip.
    /// 单个音频条目——一个 tag 对应一个 AudioClip。
    /// </summary>
    [System.Serializable]
    public class AudioItem
    {
        /// <summary>Unique identifier used in Play/Stop calls. / 播放/停止时使用的唯一标识。</summary>
        public string tag;

        /// <summary>The AudioClip to play. / 要播放的音频剪辑。</summary>
        public AudioClip clip;

        /// <summary>Default volume for this clip (0–1). Can be overridden at runtime. / 默认音量（0–1），运行时可覆盖。</summary>
        [Range(0f, 1f)]
        public float volume = 1f;

        /// <summary>
        /// If true, this clip is treated as BGM (controlled by MusicEnabled).
        /// If false, treated as SFX (controlled by SoundEnabled).
        /// true = 背景音乐（受 MusicEnabled 控制），false = 音效（受 SoundEnabled 控制）。
        /// </summary>
        public bool isBGM;
    }
}
