using System.Collections.Generic;
using UnityEngine;

namespace WsGame.DailyBouns
{
    /// <summary>
    /// 每日签到奖励配置
    /// </summary>
    [CreateAssetMenu(fileName = "DailyBonusData", menuName = "WSGame/DailyBonusData", order = 0)]
    public class DailyBonusConfig : ScriptableObject
    {
        [SerializeField] private List<DailyBonus> dailyBonusList = new List<DailyBonus>();

        public List<DailyBonus> DailyBonusList => dailyBonusList;
    }

    /// <summary>
    /// 单日签到奖励
    /// </summary>
    [System.Serializable]
    public class DailyBonus
    {
        public int Index;
        public List<RewardData> RewardDatas = new List<RewardData>();
    }
}
