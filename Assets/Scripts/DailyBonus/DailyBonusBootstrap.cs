using System.Collections.Generic;
using CrowdMatch;
using DG.Tweening;
using UnityEngine;
using WsGame.DailyBouns;

namespace WsGame.DailyBouns.Integration
{
    /// <summary>
    /// 集成引导：放到场景中任意常驻 GameObject 上。
    /// 在 Awake 中注入奖励发放器。持久化默认已是 UnityEngine.PlayerPrefs，无需配置。
    /// </summary>
    public class DailyBonusBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            // 注入奖励发放器（必做：不注入则领取只记录状态、不真正发放）
            DailyBounsData.RegisterRewardHandler(new DailyBonusRewardHandler());

            // 面板每次打开都会重新实例化，所以用静态回调注入，而不是直接持有面板引用
            DailyBonusPanel.OnPanelCreated += SetupCoinTween;

            // 注入图标加载（把 RewardData / type 映射为你的 Sprite）
            // 本项目尚无道具资源表：解析器目前只会把 sprite 置空，反而抹掉 prefab 自带的奖励图标。
            // 接入真实图标表后再改为调用 RegisterIconResolvers()。
            //RegisterIconResolvers();
        }

        private void OnDestroy()
        {
            DailyBonusPanel.OnPanelCreated -= SetupCoinTween;
        }

        /// <summary>
        /// 签到领取金币时的飞币动画（参考 WinPanel.ShowCoinTween）：
        /// 金币从奖励图标飞向面板金币栏的 CoinImg，落地后金币数字滚动上涨。
        /// 起点由模块传入（被点击的奖励图标），终点取面板 Bg/GoldFrame/CoinImg。
        /// </summary>
        private static void SetupCoinTween(DailyBonusPanel panel)
        {
            panel.coinTweenCallback = (start, lastCount, newCount) =>
            {
                var target = panel.transform.Find("Bg/GoldFrame/CoinImg");
                var coinTween = panel.GetComponentInChildren<CoinTweenPanel>(true);

                if (coinTween == null || target == null)
                {
                    // 面板里没有金币飞行容器或终点时，退化为直接显示最终数字
                    panel.SetGoldText(newCount);
                    return;
                }

                coinTween.ShowCoins(start.position, target, newCount - lastCount, 1, () =>
                {
                    DOVirtual.Int(lastCount, newCount, 0.5f, panel.SetGoldText).SetEase(Ease.Linear);
                }, duration: 1);
            };
        }

        private void RegisterIconResolvers()
        {
            // 图标加载两条通用规则（宿主项目的图标解析器必须遵守）：
            // 1. 图片设置原生大小：img.rectTransform.sizeDelta = sprite.rect.width/height（参考
            //    Fruit Loop Stack 版 SetIconNativeSize）。
            // 2. 同一道具若有多个图标（如 PropClickIcon / PropIcon），选较小（面积小）的那个
            //    （参考 Fruit Loop Stack 版 PickSmallerIcon）。
            DailyRewardItem.GlobalIconResolver = (img, data) =>
            {
                // TODO: 换成你的资源表查询，例如：
                // img.sprite = YourGameConfig.GetRewardIcon(data);
                img.sprite = null;
            };
            RewardEffect.GlobalIconResolver = (img, rewardType) =>
            {
                // TODO: 换成你的资源表查询，例如：
                // img.sprite = YourGameConfig.GetItemSprite(rewardType);
                img.sprite = null;
            };
        }
    }

    /// <summary>
    /// 签到奖励发放器：金币读写走宿主 GameData.Gold，领取后刷新游戏内金币显示。
    /// </summary>
    public class DailyBonusRewardHandler : IRewardHandler
    {
        public void GrantRewards(IReadOnlyList<RewardData> rewards, int ratio = 1, string way = "")
        {
            bool goldChanged = false;

            foreach (var reward in rewards)
            {
                if (reward.gold > 0)
                {
                    GameData.Gold.Add(reward.gold * ratio, way);
                    goldChanged = true;
                }

                foreach (var item in reward.items)
                {
                    // 宿主还没有道具存储（GameData.itemPlayerData 尚无定义），道具只记录不发放。
                    Debug.Log("[DailyBonus] 道具未接入，跳过发放 type=" + item.type + " count=" + item.count * ratio);
                }
            }

            if (goldChanged)
                RefreshHostGoldCount();
        }

        /// <summary>金币读接口：返回宿主真实金币数，供飘字/增量显示使用。</summary>
        public int GetGoldCount()
        {
            return GameData.Gold.Count;
        }

        private static void RefreshHostGoldCount()
        {
            var ui = UIManager.Instance;
            if (ui != null && ui.gameInnerUI != null && ui.gameInnerUI.gameObject.activeSelf)
                ui.gameInnerUI.RefreshGoldCount();
        }
    }
}
