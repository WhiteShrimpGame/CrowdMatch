using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 游戏流程数据中心：存储流程编排所需的最少数据（当前关卡、连败次数、是否游玩、
    /// 像素计数）。持久化（存档/读档）暂用内存，接入自己的存储后端即可。
    /// </summary>
    public static class GameData
    {
        public const string MusicState = "MusicState";
        public const string SoundState = "SoundState";
        public const string VibrateState = "VibrateState";
        
        public static GoldPlayerData Gold
        {
            get
            {
                if (_gold == null)
                {
                    _gold = new GoldPlayerData();
                }

                return _gold;
            }
        }
        private static GoldPlayerData _gold;
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
        private static int _winStreak = -1;
        public static int WinStreak
        {
            get
            {
                if (_winStreak < 0)
                {
                    _winStreak = PlayerPrefs.GetInt("WinStreak", 0);
                }

                return _winStreak;
            }
            set
            {
                _winStreak = value;
                PlayerPrefs.SetInt("WinStreak", value);
            }
        }
        public static int AddDay
        {
            get
            {
                if (_addDay < 0)
                {
                    _addDay = PlayerPrefs.GetInt("AddDay", 0);
                }

                return _addDay;
            }
            set
            {
                _addDay = value;
                PlayerPrefs.SetInt("AddDay", _addDay);
            }
        }

        public static int _addDay = -1;
        public static int AddHour
        {
            get
            {
                if (_addHour < 0)
                {
                    _addHour = PlayerPrefs.GetInt("AddHourKey", 0);
                }

                return _addHour;
            }
            set
            {
                _addHour = value;
                PlayerPrefs.SetInt("AddHourKey", _addHour);
            }
        }

        public static int _addHour = -1;
        /// <summary>当前关卡的连续失败次数。胜利时清零。</summary>
        public static int FailCount { get; set; } = 0;

        /// <summary>是否处于游玩模式（当前项目恒为 true，无主菜单）。</summary>
        public static bool IsGaming { get; set; } = true;

        /// <summary>本关像素总数（加载关卡时统计）。</summary>
        public static int TotalPixelCount { get; set; } = 0;

        /// <summary>本关已被消费（移除）的像素数。</summary>
        public static int ClearedPixelCount { get; set; } = 0;
        /// <summary>本关已被点出的像素数。</summary>
        public static int RemovePixelCount { get; set; } = 0;

        /// <summary>重置单局计数。在每次重载关卡时调用。</summary>
        /// <param name="gaming">是否进入游玩模式</param>
        public static void Init(bool gaming = true)
        {
            IsGaming = gaming;
            TotalPixelCount = 0;
            ClearedPixelCount = 0;
            RemovePixelCount = 0;
        }
        // GameData.cs 里新增
        public static void ResetAll()
        {
            _currentLevel = -1;
            _winStreak = -1;
            _addDay = -1;
            _addHour = -1;
            _gold = null;          // 下次访问重新从 PlayerPrefs 读
            FailCount = 0;
            TotalPixelCount = 0;
            ClearedPixelCount = 0;
            RemovePixelCount = 0;
        }
        /// <summary>关卡难度 (0=普通, 1=困难, 2=超难) / Level difficulty</summary>
        //public static int LevelDiff = 0;
        public static int LevelDiff
        {
            get
            {
                //var levelIndex = CurrentLevelIndex;
                int count = GameManager.Instance.levelDataConfig.levels.Count;
                int countLoop = GameManager.Instance.levelDataConfig.loopLevels.Count;
                /*Debug.Log(count);
                Debug.Log(countLoop);*/
                if (CurrentLevel <= count)
                {
                    //Debug.Log(CurrentLevel-1);
                    return GameManager.Instance.levelDataConfig.levelDiff[CurrentLevel-1];
                }
                else if (countLoop > 0)
                {
                    int level = (CurrentLevel - count) % (countLoop)-1;
                    if (level == -1)
                    {
                        level = countLoop - 1;
                    }
                    if (level < countLoop)
                    {
                        return GameManager.Instance.levelDataConfig.levelDiffLoop[level];
                    }
                }
                return 0;
            }
        }
    }
}
