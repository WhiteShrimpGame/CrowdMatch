using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace WsGame.DailyBouns
{
    /// <summary>
    /// 单条奖励项（图标 + 数量）
    /// </summary>
    public class DailyRewardItem : MonoBehaviour
    {
        private RewardData rewardData;
        private Image itemIcon;
        private Text numText;

        private List<Image> imgList = new List<Image>();
        private List<Text> textList = new List<Text>();

        private DailyBonusPanel dailyBounsPanel;

        /// <summary>图标加载回调（宿主注入：根据 rewardData 设置 itemIcon.sprite）</summary>
        public System.Action<Image, RewardData> iconResolver;

        /// <summary>全局图标加载回调（推荐：在 Bootstrap 中注入一次，所有实例共享）</summary>
        public static System.Action<Image, RewardData> GlobalIconResolver;

        public void InitData(DailyBonusPanel panel)
        {
            dailyBounsPanel = panel;

            itemIcon = transform.Find("ItemIcon").GetComponent<Image>();
            numText = transform.Find("Count").GetComponent<Text>();

            GetComponentsInChildren(imgList);
            GetComponentsInChildren(textList);
        }

        public void Init(RewardData itemData)
        {
            rewardData = itemData;
            SetIcon();
            SetCount();
        }

        /// <summary>
        /// 显示奖励数量：道具显示 xN，纯金币显示 x{金币数}。
        /// 适配记录 §五：原版 Init 只设图标不设数量，导致道具数量不显示。
        /// </summary>
        private void SetCount()
        {
            if (numText == null)
                return;
            if (rewardData == null)
            {
                numText.gameObject.SetActive(false);
                return;
            }
            if (rewardData.items.Count > 0)
            {
                numText.gameObject.SetActive(true);
                numText.text = "x" + rewardData.items[0].count;
            }
            else if (rewardData.gold > 0)
            {
                numText.gameObject.SetActive(true);
                numText.text = "x" + rewardData.gold;
            }
            else
            {
                numText.gameObject.SetActive(false);
            }
        }

        private void SetIcon()
        {
            if (GlobalIconResolver != null)
            {
                GlobalIconResolver(itemIcon, rewardData);
                return;
            }
            if (iconResolver != null)
                iconResolver(itemIcon, rewardData);
        }

        public void GetReward(int ratio = 1)
        {
            if (rewardData == null)
                return;

            var handler = dailyBounsPanel != null ? dailyBounsPanel.rewardHandler : DailyBounsData.RewardHandler;
            if (handler == null)
                handler = DailyBounsData.RewardHandler;
            int lastGold = (handler != null) ? handler.GetGoldCount() : 0;
            if (handler != null)
                handler.GrantRewards(new List<RewardData> { rewardData }, ratio, "DailyLogin");
            int curGold = (handler != null) ? handler.GetGoldCount() : 0;

            if (curGold > lastGold && dailyBounsPanel != null)
            {
                dailyBounsPanel.ShowCoinTween(itemIcon.transform, lastGold, curGold);
            }

            Sequence quence = DOTween.Sequence();
            for (int i = 0; i < rewardData.items.Count; i++)
            {
                RewardItem item = rewardData.items[i];
                quence.AppendCallback(delegate
                {
                    if (dailyBounsPanel != null && dailyBounsPanel.rewardEffectPrefab != null)
                        dailyBounsPanel.GetRewardEffect().Show(transform, item.type, null, item.count);
                });
                quence.AppendInterval(0.2f);
            }
        }
    }
}
