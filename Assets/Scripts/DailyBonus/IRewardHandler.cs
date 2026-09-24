using System.Collections.Generic;

namespace WsGame.DailyBouns
{
    /// <summary>
    /// 奖励数据接口（跨项目复用：宿主实现具体发放逻辑）
    /// </summary>
    public interface IRewardHandler
    {
        /// <summary>
        /// 发放一组奖励。
        /// </summary>
        /// <param name="rewards">要发放的奖励列表</param>
        /// <param name="ratio">倍率（当前版本恒为 1，预留）</param>
        /// <param name="way">发放来源标识（如 "DailyLogin"）</param>
        void GrantRewards(IReadOnlyList<RewardData> rewards, int ratio = 1, string way = "");

        /// <summary>
        /// 获取当前金币数量（用于飘字显示增量；无金币概念的项目返回 0）。
        /// </summary>
        int GetGoldCount();
    }

    /// <summary>
    /// 奖励数据（配置与运行时统一使用）
    /// </summary>
    [System.Serializable]
    public class RewardData
    {
        public int gold;
        public List<RewardItem> items = new List<RewardItem>();
    }

    [System.Serializable]
    public class RewardItem
    {
        public int type;
        public int count;
    }
}
