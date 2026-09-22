using UnityEngine;
using UnityEngine.UI;

namespace WsGame.DailyBouns
{
    /// <summary>
    /// 第 7 天签到格子（大图标展示，无特殊发放逻辑）
    /// </summary>
    public class DailyBonusItem_Day7 : DailyBonusItem
    {
        private Image glowImg;

        public override void InitData(DailyBonusPanel mDailyBonusPanel)
        {
            glowImg = transform.Find("Bg/DailyRewardItem/ItemIcon").GetComponent<Image>();
            base.InitData(mDailyBonusPanel);
        }

        public override void ChangeState(DailyBonusState dailyBonusState)
        {
            if (this.DailyBonusState == dailyBonusState || dailyBonusState == DailyBonusState.None)
                return;

            this.DailyBonusState = dailyBonusState;

            for (int i = 0; i < rewardDatas.Count; i++)
                dailyRewardItems[i].Init(rewardDatas[i]);

            dayText.text = "第" + dailyBonus.Index.ToString() + "天";

            switch (dailyBonusState)
            {
                case DailyBonusState.Finished:
                    icon_CheckImg.gameObject.SetActive(true);
                    maskImg.gameObject.SetActive(true);
                    m_Btn.interactable = false;
                    break;
                case DailyBonusState.Today:
                    icon_CheckImg.gameObject.SetActive(false);
                    maskImg.gameObject.SetActive(false);
                    m_Btn.interactable = true;
                    break;
                case DailyBonusState.UnFinished:
                    icon_CheckImg.gameObject.SetActive(false);
                    maskImg.gameObject.SetActive(false);
                    m_Btn.interactable = false;
                    break;
            }
        }
    }
}
