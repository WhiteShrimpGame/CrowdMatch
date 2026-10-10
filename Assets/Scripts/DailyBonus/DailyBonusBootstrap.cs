using System.Collections.Generic;
using CrowdMatch;
using DG.Tweening;
using UnityEngine;
using WsGame.DailyBouns;

namespace WsGame.DailyBouns.Integration
{
    /// <summary>
    /// 集成引导：放到场景中任意常驻 GameObject 上。
    /// 在 Awake 中注入奖励发放器与图标解析器。持久化默认已是 UnityEngine.PlayerPrefs，无需配置。
    /// </summary>
    public class DailyBonusBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            // 注入奖励发放器（必做：不注入则领取只记录状态、不真正发放）
            DailyBounsData.RegisterRewardHandler(new DailyBonusRewardHandler());

            // 面板每次打开都会重新实例化，所以用静态回调注入，而不是直接持有面板引用
            DailyBonusPanel.OnPanelCreated += SetupCoinTween;

            // 注入图标加载：奖励图标查宿主的 ItemDataConfig
            RegisterIconResolvers();
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
                DOVirtual.DelayedCall(0.6f, () => { RewardTips.CoinSE(); });
                coinTween.ShowCoins(start.position, target, newCount - lastCount, 1, () =>
                {
                    DOVirtual.Int(lastCount, newCount, 0.5f, panel.SetGoldText).SetEase(Ease.Linear);
                }, duration: 1);
            };
        }

        /// <summary>
        /// 奖励图标解析：纯金币奖励用金币图，带道具的用第一件道具的**大图**（签到格子显示尺寸大，
        /// 小图放大会糊）。取到图后套 sprite 原生尺寸，再按类型叠一个缩放系数：金币 0.4、
        /// 道具 3(Remove) 0.33、其余道具 0.35 —— 三种图源原生尺寸不一致，不归一大小会不齐。
        /// </summary>
        private static void RegisterIconResolvers()
        {
            DailyRewardItem.GlobalIconResolver = (img, reward) =>
            {
                if (reward == null || reward.items == null)
                    return;

                // 只有「单件道具」和「纯金币」才走资源表换图。
                // 多件道具（第7天礼包）保持 prefab 原图：格子放不下多件，换图只能显示第一件，会误导。
                Sprite sprite;
                float scale;
                if (reward.items.Count == 1)
                {
                    sprite = GetItemSprite(DailyBonusRewardHandler.ToItemType(reward.items[0].type), true);
                    scale = reward.items[0].type == (int)ItemType.Remove ? 0.33f : 0.35f;
                }
                else if (reward.items.Count == 0)
                {
                    sprite = GetGoldSprite();
                    scale = 0.4f;
                }
                else
                {
                    return;
                }

                // 查不到就不覆盖：prefab 里本来就摆好了金币/礼包图，赋 null 会把它抹成空白，
                // 连带 sizeDelta 也变成 sprite 原生尺寸以外的值（金币图走 ItemDataConfig.goldImg）。
                if (sprite != null)
                {
                    img.sprite = sprite;
                    img.rectTransform.sizeDelta = sprite.rect.size;
                }
                img.rectTransform.localScale = Vector3.one * scale;
            };
            RewardEffect.GlobalIconResolver = (img, rewardType) =>
            {
                var sprite = GetItemSprite(DailyBonusRewardHandler.ToItemType(rewardType));
                if (sprite != null)
                    img.sprite = sprite;
            };
        }

        private static ItemDataConfig ItemConfig
        {
            get { return GameManager.Instance != null ? GameManager.Instance.itemData : null; }
        }

        /// <summary>
        /// 查道具图。不用 ItemDataConfig.GetSmallImg/GetBigImg —— 它们内部是 indexDict[type]，
        /// 配置里缺这件道具时会抛 KeyNotFoundException；直接遍历 data 数组更安全。
        /// </summary>
        private static Sprite GetItemSprite(ItemType type, bool big = false)
        {
            var cfg = ItemConfig;
            if (cfg == null || cfg.data == null || type == ItemType.None)
                return null;

            for (int i = 0; i < cfg.data.Length; i++)
            {
                if (cfg.data[i].type == type)
                    return big ? cfg.data[i].bigImg : cfg.data[i].smallImg;
            }
            return null;
        }

        private static Sprite GetGoldSprite()
        {
            var cfg = ItemConfig;
            return cfg != null ? cfg.goldImg : null;
        }
    }

    /// <summary>
    /// 签到奖励发放器：金币走 GameData.Gold，道具走 GameData.itemPlayerData，
    /// 发放后刷新游戏内的金币与道具数量显示。
    /// </summary>
    public class DailyBonusRewardHandler : IRewardHandler
    {
        public void GrantRewards(IReadOnlyList<RewardData> rewards, int ratio = 1, string way = "")
        {
            bool changed = false;

            foreach (var reward in rewards)
            {
                if (reward.gold > 0)
                {
                    GameData.Gold.Add(reward.gold * ratio, way);
                    changed = true;
                }

                foreach (var item in reward.items)
                {
                    var type = ToItemType(item.type);
                    if (type == ItemType.None)
                    {
                        Debug.LogWarning("[DailyBonus] 道具编号非法，跳过发放 type=" + item.type);
                        continue;
                    }

                    GameData.itemPlayerData.AddCount(type, item.count * ratio, way);
                    changed = true;
                }
            }

            if (changed)
            {
                // 签到发放（金币或道具）都播这个音频。
                // 金币那条路另外还有 CoinSE 的连响，两声叠加是刻意保留的。
                AudioManager.Instance.Play("DailyReward");
                RefreshHostUI();
            }
        }

        /// <summary>金币读接口：返回宿主真实金币数，供飘字/增量显示使用。</summary>
        public int GetGoldCount()
        {
            return GameData.Gold.Count;
        }

        /// <summary>
        /// 配置道具编号 → 宿主枚举。签到配置用的是 1/2/3，与
        /// ItemType(Add=1, Remove=2, Clear=3) 一一对应，**不需要偏移**；
        /// 越界返回 None，由调用方跳过。
        /// </summary>
        public static ItemType ToItemType(int type)
        {
            /*if (type < (int)ItemType.Add || type > (int)ItemType.Clear)
                return ItemType.None;*/
            if (type < 1 || type > 3)
                return ItemType.None;
            return (ItemType)type;
        }

        private static void RefreshHostUI()
        {
            var ui = UIManager.Instance;
            if (ui == null || ui.gameInnerUI == null || !ui.gameInnerUI.gameObject.activeSelf)
                return;

            ui.gameInnerUI.RefreshGoldCount();

            // UpdateCurrentButtonInfo 内部会走 ItemPlayerData.IsUnlock → GameManager.itemData.GetUnlockLvl，
            // itemData 未挂时会 NRE。这里挡一道，免得场景漏挂配置时把领取流程整条打断。
            if (GameManager.Instance != null && GameManager.Instance.itemData != null)
                ui.gameInnerUI.UpdateCurrentButtonInfo();   // 道具数量
        }
    }
}
