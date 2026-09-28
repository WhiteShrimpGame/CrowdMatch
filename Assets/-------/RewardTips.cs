using System.Collections.Generic;
using CrowdMatch;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ItemAddTips
{
    public ItemType type;
    public int count;
    public Transform parent;
}

public static class RewardTips
{
    /*public static bool ShowSingleReward(Transform root, RewardData curReward, bool xCount = false,
        bool isSpinWheel = false)
    {
        var img = root.GetComponent<Image>();
        var reward = curReward;
        int count;
        bool isGift = false;
        if (reward.items.Count == 0)
        {
            if (isSpinWheel)
            {
                img.sprite = GameManager.Instance.itemData.goldImg;
                img.rectTransform.sizeDelta = GameManager.Instance.itemData.goldImg.rect.size;
                count = reward.gold;
            }
            else
            {
                img.sprite = GameManager.Instance.itemData.gold3Img;
                img.rectTransform.sizeDelta = GameManager.Instance.itemData.gold3Img.rect.size;
                count = reward.gold;
            }
        }
        else if (reward.items.Count == 1 && reward.gold == 0)
        {
            PropInfo prop = GameManager.Instance.GetPropInfo(reward.items[0].type);
            //img.sprite = GameManager.Instance.itemData.GetSmallImg(reward.items[0].type);
            img.sprite = prop.propSmallSpr;
            img.rectTransform.sizeDelta = prop.propSmallSpr.rect.size * 0.8f;
            count = reward.items[0].count;
        }
        else
        {
            img.sprite = GameManager.Instance.itemData.giftImg;
            img.rectTransform.sizeDelta = GameManager.Instance.itemData.giftImg.rect.size;
            count = -1;
            isGift = true;
        }

        var countText = img.transform.parent.Find("Count").GetComponent<Text>();
        if (isGift)
        {
            countText.gameObject.SetActive(false);
        }
        else if (xCount)
        {
            countText.text = "x" + count.ToString();
        }
        else
        {
            countText.text = count.ToString();
        }

        return isGift;
    }*/

    public static void CoinSE(int loop = 6)
    {
        AudioManager.Instance.Play("Coins");
        loop--;
        if (loop <= 0)
        {
            return;
        }

        DOVirtual.DelayedCall(0.1f, () => { CoinSE(loop); });
    }
    public static void AddItemAddTips(List<ItemAddTips> list, Transform parent, ItemType type, int count = 1)
    {
        var addTips = new ItemAddTips();
        addTips.type = type;
        addTips.count = count;
        addTips.parent = parent;

        list.Add(addTips);
    }
    public static Transform ShowReward(Transform root, RewardData curReward,
        System.Action<Transform> coinTweenCB = null, System.Action<ItemType, int, Transform> itemTweenCB = null,
        bool tween = false, bool hasHammer = false)
    {
        Transform curRewardRoot;
        bool hasGold = curReward.gold > 0;
        int count = hasGold ? 1 : 0;
        count += curReward.items != null ? curReward.items.Count : 0;
        count += hasHammer ? 1 : 0;
        count += curReward.AvatarNum > 0 ? 1 : 0;
        count += curReward.FrameNum > 0 ? 1 : 0;
        if (count <= 1)
        {
            curRewardRoot = root.Find("RewardRoot");
        }
        else if (count == 2)
        {
            curRewardRoot = root.Find("RewardRoot2");
        }
        else if (count == 3)
        {
            curRewardRoot = root.Find("RewardRoot4");
        }
        else
        {
            curRewardRoot = root.Find("RewardRoot5");
            if (curRewardRoot == null)
            {
                curRewardRoot = root.Find("RewardRoot4");
            }
        }

        //if (curReward.items == null || curReward.items.Count == 0)
        //{
        //    if (!hasHammer)
        //        if (curReward.AvatarNum > 0 || curReward.FrameNum > 0)
        //        {
        //            curRewardRoot = root.Find("RewardRoot2");
        //        }
        //        else
        //        {
        //            curRewardRoot = root.Find("RewardRoot");
        //        }
        //    else
        //    {
        //        if (curReward.AvatarNum > 0 || curReward.FrameNum > 0)
        //        {
        //            curRewardRoot = root.Find("RewardRoot4");
        //        }
        //        else
        //        {
        //            curRewardRoot = root.Find("RewardRoot2");
        //        }
        //    }
        //    hasGold = true;
        //}
        //else if (curReward.items.Count == 1 && curReward.gold == 0)
        //{
        //    if (!hasHammer)
        //        if (curReward.AvatarNum > 0 || curReward.FrameNum > 0)
        //        {
        //            curRewardRoot = root.Find("RewardRoot2");
        //        }
        //        else
        //        {
        //            curRewardRoot = root.Find("RewardRoot");
        //        }
        //    else
        //    {
        //        if (curReward.AvatarNum > 0 || curReward.FrameNum > 0)
        //        {
        //            curRewardRoot = root.Find("RewardRoot4");
        //        }
        //        else
        //        {
        //            curRewardRoot = root.Find("RewardRoot2");
        //        }
        //    }
        //    hasGold = false;
        //}
        //else if (curReward.gold == 0)
        //{
        //    if (!hasHammer)
        //        if (curReward.AvatarNum > 0 || curReward.FrameNum > 0)
        //        {
        //            curRewardRoot = root.Find("RewardRoot4");
        //        }
        //        else
        //        {
        //            curRewardRoot = root.Find("RewardRoot2");
        //        }
        //    else
        //    {
        //        if (curReward.AvatarNum > 0 || curReward.FrameNum > 0)
        //        {
        //            curRewardRoot = root.Find("RewardRoot4");
        //        }
        //        else
        //        {
        //            curRewardRoot = root.Find("RewardRoot4");
        //        }
        //    }
        //    hasGold = false;
        //}
        //else if (curReward.items.Count == 1)
        //{
        //    if (!hasHammer)
        //        if (curReward.AvatarNum > 0 || curReward.FrameNum > 0)
        //        {
        //            curRewardRoot = root.Find("RewardRoot4");
        //        }
        //        else
        //        {
        //            curRewardRoot = root.Find("RewardRoot2");
        //        }
        //    else
        //    {
        //        if (curReward.AvatarNum > 0 || curReward.FrameNum > 0)
        //        {
        //            curRewardRoot = root.Find("RewardRoot4");
        //        }
        //        else
        //        {
        //            curRewardRoot = root.Find("RewardRoot4");
        //        }
        //    }
        //    hasGold = true;
        //}
        //else
        //{
        //    curRewardRoot = root.Find("RewardRoot4");
        //    hasGold = true;
        //}
        curRewardRoot.gameObject.SetActive(true);

        int startIndex = 0;
        if (hasGold)
        {
            var img = curRewardRoot.GetChild(0).Find("RewardImg").GetComponent<Image>();
            img.sprite = GameManager.Instance.itemData.goldImg;
            var countText = curRewardRoot.GetChild(0).Find("RewardCount").GetComponent<Text>();
            if (tween)
            {
                countText.text = "0";
            }
            else
            {
                countText.text = curReward.gold.ToString();
            }

            startIndex = 1;

            coinTweenCB?.Invoke(curRewardRoot.GetChild(0));
        }

        if (curReward.items != null)
        {
            for (int i = 0; i < curReward.items.Count; i++)
            {
                if (curRewardRoot.childCount <= startIndex + i)
                {
                    break;
                }

                var frame = curRewardRoot.GetChild(startIndex + i);
                var img = frame.Find("RewardImg").GetComponent<Image>();
                var countText = frame.Find("RewardCount").GetComponent<Text>();
                var item = curReward.items[i];
                img.sprite = GameManager.Instance.itemData.GetSmallImg(item.type);
                if (tween)
                {
                    countText.text = "0";
                }
                else
                {
                    countText.text = item.count.ToString();
                }

                itemTweenCB?.Invoke(item.type, item.count, frame);
            }
        }

        /*if (hasHammer)
        {
            var frame = curRewardRoot.GetChild(curRewardRoot.childCount - 1);
            var img = frame.Find("RewardImg").GetComponent<Image>();
            var countText = frame.Find("RewardCount").GetComponent<Text>();
            img.sprite = GameManager.Instance.itemData.hammerImg;
            countText.text = GameData.TotalTapeCount.ToString();
        }
        if (curReward.AvatarNum > 0)//添加头像
        {
            var frame = curRewardRoot.GetChild(curRewardRoot.childCount - 1);
            var img = frame.Find("RewardImg").GetComponent<Image>();
            img.sprite = GameManager.Instance.AvatarPanelConfig.AvatarFrameMsgList[curReward.AvatarNum].Icon;
            var countText = frame.Find("RewardCount").GetComponent<Text>();
            countText.text = "";
        }*/

        return curRewardRoot;
    }
}
