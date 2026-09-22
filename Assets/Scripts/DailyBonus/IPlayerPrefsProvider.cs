using UnityEngine;

namespace WsGame.DailyBouns
{
    /// <summary>
    /// 键值持久化接口（默认实现为 UnityEngine.PlayerPrefs，
    /// 云存档项目可注入自己的实现）。
    /// </summary>
    public interface IPlayerPrefsProvider
    {
        int GetInt(string key, int defaultValue);
        void SetInt(string key, int value);
        void Save();
    }

    /// <summary>
    /// 默认持久化实现：UnityEngine.PlayerPrefs
    /// </summary>
    public sealed class UnityPlayerPrefsProvider : IPlayerPrefsProvider
    {
        public int GetInt(string key, int defaultValue)
        {
            return PlayerPrefs.GetInt(key, defaultValue);
        }

        public void SetInt(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
        }

        public void Save()
        {
            PlayerPrefs.Save();
        }
    }
}
