using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 游戏流程数据中心：存储流程编排所需的最少数据（当前关卡、连败次数、是否游玩、
    /// 像素计数）。持久化（存档/读档）暂用内存，接入自己的存储后端即可。
    /// </summary>
    public static class GameData
    {
        private static int _currentLevel = -1;

        /// <summary>当前关卡编号（1 起）。读取时保护：&lt;= 0 一律按第 1 关处理。胜利时 +1，失败重置当前关时不变。</summary>
        public static int CurrentLevel
        {
            get 
            {
                if (_currentLevel <= 0)
                {
                    _currentLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
                }
                return _currentLevel <= 0 ? 1 : _currentLevel; }

            set 
            { 
                _currentLevel = value <= 0 ? 1 : value;
                PlayerPrefs.SetInt("CurrentLevel", value);
            }
        }

        /// <summary>当前关卡的连续失败次数。胜利时清零。</summary>
        public static int FailCount { get; set; } = 0;

        /// <summary>是否处于游玩模式（当前项目恒为 true，无主菜单）。</summary>
        public static bool IsGaming { get; set; } = true;

        /// <summary>本关像素总数（加载关卡时统计）。</summary>
        public static int TotalPixelCount { get; set; } = 0;

        /// <summary>本关已被消费（移除）的像素数。</summary>
        public static int ClearedPixelCount { get; set; } = 0;

        /// <summary>重置单局计数。在每次重载关卡时调用。</summary>
        /// <param name="gaming">是否进入游玩模式</param>
        public static void Init(bool gaming = true)
        {
            IsGaming = gaming;
            TotalPixelCount = 0;
            ClearedPixelCount = 0;
        }
    }
}
