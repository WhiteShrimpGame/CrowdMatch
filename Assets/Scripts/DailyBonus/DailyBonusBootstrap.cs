using UnityEngine;
using WsGame.DailyBouns;

namespace WsGame.DailyBouns.Integration
{
    /// <summary>
    /// 集成引导：放到场景中任意常驻 GameObject 上。
    /// 在 Awake 中注入奖励发放器、图标加载。持久化默认已是 UnityEngine.PlayerPrefs，无需配置。
    ///
    /// 注意：SampleRewardHandler 与 RegisterIconResolvers 只是"可编译的示例"，
    /// 具体发放/图标逻辑必须在你的项目里替换成真实的 GameData / 资源表代码。
    /// </summary>
    public class DailyBonusBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            // 注入奖励发放器（必做：不注入则领取只记录状态、不真正发放）
            DailyBounsData.RegisterRewardHandler(new SampleRewardHandler());

            // 注入图标加载（把 RewardData / type 映射为你的 Sprite）
            // 本项目尚无道具资源表：解析器目前只会把 sprite 置空，反而抹掉 prefab 自带的奖励图标。
            // 接入真实图标表后再改为调用 RegisterIconResolvers()。
            //RegisterIconResolvers();
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
    /// 示例奖励发放器：请替换为你项目的真实发放逻辑（金币/道具来源）
    /// </summary>
    public class SampleRewardHandler : IRewardHandler
    {
        public void GrantRewards(System.Collections.Generic.IReadOnlyList<RewardData> rewards, int ratio = 1, string way = "")
        {
            foreach (var reward in rewards)
            {
                if (reward.gold > 0)
                    Debug.Log("[DailyBonus] grant gold: " + reward.gold * ratio);
                foreach (var item in reward.items)
                    Debug.Log("[DailyBonus] grant item type=" + item.type + " count=" + item.count * ratio);
            }
        }

        public int GetGoldCount()
        {
            // TODO: 返回你项目的真实金币数量，用于飘字增量显示
            return 0;
        }
    }
}
