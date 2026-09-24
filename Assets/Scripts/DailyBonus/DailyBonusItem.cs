using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace WsGame.DailyBouns
{
    /// <summary>
    /// 每日奖励状态
    /// </summary>
    public enum DailyBonusState
    {
        None,
        Finished,   // 已领
        Today,      // 今日可领
        UnFinished, // 不可领
    }

    /// <summary>
    /// 单个签到格子
    /// </summary>
    public class DailyBonusItem : MonoBehaviour
    {
        public DailyBonusState DailyBonusState = DailyBonusState.None;

        //protected Image bgImg;
        //protected CanvasGroup bgCanvasGroup;
        //[SerializeField] protected Sprite normalBgSprite;
        //[SerializeField] protected Sprite todayBgSprite;
        [SerializeField] protected Image todayBg;
        protected Image icon_CheckImg;
        protected Image maskImg;
        protected Text dayText;
        protected Vector3 startScale;

        protected DailyBonusPanel dailyBonusPanel;
        protected DailyBonus dailyBonus = null;
        protected List<DailyRewardItem> dailyRewardItems = new List<DailyRewardItem>();
        protected List<RewardData> rewardDatas = new List<RewardData>();
        [SerializeField] protected Text rewardCountText;
        protected Button m_Btn;

        public virtual void InitData(DailyBonusPanel mDailyBonusPanel)
        {
            dailyBonusPanel = mDailyBonusPanel;
            //bgImg = transform.Find("Bg").GetComponent<Image>();
            //bgCanvasGroup = bgImg.GetComponent<CanvasGroup>();
            icon_CheckImg = transform.Find("Icon_Check").GetComponent<Image>();
            dayText = transform.Find("Bg/Text_Day").GetComponent<Text>();
            maskImg = transform.Find("Bg/MaskImg").GetComponent<Image>();
            m_Btn = GetComponent<Button>();
            startScale = transform.localScale;

            m_Btn.onClick.AddListener(() => { OnBtnClick(); });

            GetComponentsInChildren(dailyRewardItems);
            foreach (var item in dailyRewardItems)
                item.InitData(dailyBonusPanel);
        }

        public void Init(DailyBonus mDailyBonus)
        {
            this.dailyBonus = mDailyBonus;
            rewardDatas = new List<RewardData>(mDailyBonus.RewardDatas);
        }

        public virtual void ChangeState(DailyBonusState dailyBonusState)
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
                    //bgImg.sprite = normalBgSprite;
                    todayBg.gameObject.SetActive(false);
                    //dayText.color = new Color(213f / 255f, 97f / 255f, 20f / 255f, 1f);
                    //if (rewardCountText != null)
                        //rewardCountText.color = new Color(216f / 255f, 111f / 255f, 49f / 255f, 1f);
                    maskImg.gameObject.SetActive(true);
                    icon_CheckImg.gameObject.SetActive(true);
                    m_Btn.interactable = false;
                    break;
                case DailyBonusState.Today:
                    //bgImg.sprite = todayBgSprite;
                    todayBg.gameObject.SetActive(true);
                    //dayText.color = new Color(70f / 255f, 123f / 255f, 35f / 255f, 1f);
                    //if (rewardCountText != null)
                        //rewardCountText.color = new Color(67f / 255f, 116f / 255f, 35f / 255f, 1f);
                    maskImg.gameObject.SetActive(false);
                    icon_CheckImg.gameObject.SetActive(false);
                    m_Btn.interactable = true;
                    break;
                case DailyBonusState.UnFinished:
                    //bgImg.sprite = normalBgSprite;
                    todayBg.gameObject.SetActive(false);
                    //dayText.color = new Color(213f / 255f, 97f / 255f, 20f / 255f, 1f);
                    //if (rewardCountText != null)
                        //rewardCountText.color = new Color(216f / 255f, 111f / 255f, 49f / 255f, 1f);
                    maskImg.gameObject.SetActive(false);
                    icon_CheckImg.gameObject.SetActive(false);
                    m_Btn.interactable = false;
                    break;
            }
        }

        public void OnBtnClick(int rate = 1)
        {
            transform.DOKill();
            transform.localScale = Vector3.one;
            transform.DOScale(startScale + Vector3.one * 0.08f, 0.1f).OnComplete(() =>
            {
                transform.DOScale(startScale, 0.1f);
            });

            for (int i = 0; i < dailyRewardItems.Count; i++)
                dailyRewardItems[i].GetReward(rate);

            dailyBonusPanel.ChangeIndex();
            dailyBonusPanel.SetCanvasGroupEnable(false);

            DOVirtual.DelayedCall(1.5f, delegate { dailyBonusPanel.Hide(); });
        }
    }
}
