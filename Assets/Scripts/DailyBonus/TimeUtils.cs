using System;
using CrowdMatch;

namespace WsGame.Time
{
    /// <summary>
    /// 通用时间工具（从每日签到模块提取，可独立复用）
    /// 基于 Unix 时间戳（UTC 秒数）。零点计算默认用本地时区（ToLocalTime），
    /// 与原版 UTC 零点相比：本地零点才是玩家感知的"每天重置"时刻。
    /// </summary>
    public static class TimeUtils
    {
        public static Func<int> GetCurrentTime { get; set; } = DefaultGetCurrentTime;

        private static int DefaultGetCurrentTime()
        {
            return DateTimeToUnix(DateTime.UtcNow) + GameData.AddDay * 86400 + GameData.AddHour * 3600;;
        }
        /// <summary>
        /// 跨天刷新判断：计算距下次刷新的剩余秒数（用于倒计时显示）。
        /// lastTime 越界（大于当前时间）时回调修正。
        /// </summary>
        /// <param name="lastTime">上次刷新基准时间戳</param>
        /// <param name="recoverTime">刷新周期秒数（如 24h = 86400）</param>
        /// <param name="callBack">lastTime 越界时回调修正值</param>
        public static double CheckRefresh(int lastTime, double recoverTime, Action<int> callBack = null)
        {
            int curTime = GetCurrentTime();
            if (lastTime > curTime)
            {
                lastTime = curTime;
                callBack?.Invoke(lastTime);
            }
            return GetZeroTime(lastTime) + recoverTime - curTime;
        }

        /// <summary>
        /// 刷新后是否已到新一天。
        /// </summary>
        public static bool IsRefreshDay(double remainSeconds)
        {
            return remainSeconds <= 0d;
        }

        /// <summary>
        /// 生成下一次刷新的零点时间戳（当天零点 + recoverTime）。
        /// </summary>
        public static int GetNextRefreshTime(int lastTime, double recoverTime)
        {
            return GetZeroTime(lastTime) + (int)recoverTime;
        }

        /// <summary>
        /// 取时间戳所属日期的本地零点（本地时区）。
        /// </summary>
        public static int GetZeroTime(int time)
        {
            DateTime dt = UnixTimeToDateTime(time);
            return DateTimeToUnix(new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0, DateTimeKind.Local));
        }

        /// <summary>
        /// 时间戳 → 本地 DateTime
        /// </summary>
        public static DateTime UnixTimeToDateTime(int time)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(time).ToLocalTime();
        }

        /// <summary>
        /// DateTime → Unix 时间戳（UTC 秒数）
        /// </summary>
        public static int DateTimeToUnix(DateTime time)
        {
            return (int)time.ToUniversalTime().Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        }

        /// <summary>
        /// 格式化秒数为 "HH:mm:ss"。
        /// </summary>
        public static string FormatTime(double totalSeconds)
        {
            if (totalSeconds < 0) totalSeconds = 0;
            int total = (int)totalSeconds;
            int hh = total / 3600;
            int mm = (total % 3600) / 60;
            int ss = total % 60;
            return string.Format("{0:D2}:{1:D2}:{2:D2}", hh, mm, ss);
        }
    }
}
