using System;
using UnityEngine;
using WsGame.Time;

namespace WsGame.DailyBouns
{
    /// <summary>
    /// 每日签到数据持久化层（独立于具体存档实现）
    /// 所有键值均走 IPlayerPrefsProvider，默认实现为 UnityEngine.PlayerPrefs。
    /// </summary>
    public static class DailyBounsData
    {
        public static readonly string WsGame = "wsgame";
        public static readonly float DailyTime = 24 * 60 * 60;

        public static readonly string Pre_DailyBonusIndex = WsGame + "DailyBonusIndex";
        public static readonly string Pre_DailyBonusTime = WsGame + "DailyBonusTime";
        public static readonly string Pre_FirstDailyBonus = WsGame + "FirstDailyBonus";
        public static readonly string Pre_CanADGetDailyBonus = WsGame + "ADGetDailyBonus";

        // 由宿主项目在启动时注入持久化实现（默认 UnityEngine.PlayerPrefs）
        public static IPlayerPrefsProvider PlayerPrefs { get; set; } = new UnityPlayerPrefsProvider();

        /// <summary>
        /// 当前时间戳（委托 TimeUtils.GetCurrentTime，宿主可覆盖）
        /// </summary>
        public static Func<int> GetCurrentTime { get; set; } = TimeUtils.GetCurrentTime;

        /// <summary>
        /// 奖励发放器（由宿主在启动时注册；未注册时领取奖励会被记录但不发放）
        /// </summary>
        public static IRewardHandler RewardHandler { get; set; }

        public static void RegisterRewardHandler(IRewardHandler handler)
        {
            RewardHandler = handler;
        }

        /// <summary>
        /// 当前签到天数索引（1~7循环）
        /// </summary>
        public static int DailyBonusIndex
        {
            get { return PlayerPrefs.GetInt(Pre_DailyBonusIndex, 1); }
            set { PlayerPrefs.SetInt(Pre_DailyBonusIndex, value); }
        }

        /// <summary>
        /// 是否是首次签到（首次无冷却）
        /// </summary>
        public static bool FirstDailyBonus
        {
            get { return PlayerPrefs.GetInt(Pre_FirstDailyBonus, -1) == -1; }
            set { PlayerPrefs.SetInt(Pre_FirstDailyBonus, value ? -1 : 1); }
        }

        /// <summary>
        /// 最后一次领取时间戳
        /// </summary>
        public static int RefreshLastTime
        {
            get { return PlayerPrefs.GetInt(Pre_DailyBonusTime, GetCurrentTime()); }
            set { PlayerPrefs.SetInt(Pre_DailyBonusTime, value); }
        }

        /// <summary>
        /// 当日奖励是否可以领取（领取冷却：距上次领取 >= 24h）
        /// </summary>
        public static bool TodayRewardCanGet()
        {
            int lastTime = RefreshLastTime;
            if (lastTime > GetCurrentTime())
            {
                RefreshLastTime = GetCurrentTime();
                lastTime = GetCurrentTime();
            }
            return (float)(GetCurrentTime() - lastTime) >= DailyTime;
        }

        public static bool TodayBounsRewardCanGet()
        {
            int dailyBonusIndex = DailyBonusIndex;
            if (dailyBonusIndex == 1 && FirstDailyBonus)
                return true;
            return TodayRewardCanGet();
        }

        public static DailyBonusState GetDailyBonusState(int dayIndex)
        {
            int dailyBonusIndex = DailyBonusIndex;
            if (dayIndex == dailyBonusIndex)
            {
                if (dailyBonusIndex == 1)
                {
                    if (FirstDailyBonus || TodayRewardCanGet())
                        return DailyBonusState.Today;
                    return DailyBonusState.UnFinished;
                }
                else
                {
                    if (TodayRewardCanGet())
                        return DailyBonusState.Today;
                    return DailyBonusState.UnFinished;
                }
            }

            // 已领取（过去的）天显示已领取；未来天不可领取（区别于已领取 UI）。
            // 适配记录 §五：原版把所有非当天都返回 Finished，导致未来天显示成"已领取"。
            if (dayIndex < dailyBonusIndex)
                return DailyBonusState.Finished;
            return DailyBonusState.UnFinished;
        }
    }
}
