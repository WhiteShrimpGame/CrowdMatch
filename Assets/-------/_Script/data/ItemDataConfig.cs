using System;
using System.Collections;
using System.Collections.Generic;
using CrowdMatch;
using UnityEngine;

/// <summary>
/// 配置物品与对应的参数
/// </summary>
[CreateAssetMenu(fileName = "ItemDataConfig", menuName = "ItemDataConfig", order = 0)]
public class ItemDataConfig : ScriptableObject
{
    public ItemData[] data;
    public Sprite goldImg;
    public Sprite gold3Img;
    public Sprite giftImg;
    public Sprite hammerImg;

    private Dictionary<ItemType, int> indexDict;

    public void InitIndexDict()
    {
        indexDict = new Dictionary<ItemType, int>();
        for (int i = 0; i < data.Length; i++)
        {
            indexDict.Add(data[i].type, i);
        }
    }

    /// <summary>
    /// 获取道具价格
    /// </summary>
    public int GetPrice(ItemType type)
    {
        if (indexDict == null)
        {
            InitIndexDict();
        }

        return data[indexDict[type]].price;
    }

    /// <summary>
    /// 获取道具小图
    /// </summary>
    public Sprite GetSmallImg(ItemType type)
    {
        if (indexDict == null)
        {
            InitIndexDict();
        }

        return data[indexDict[type]].smallImg;
    }

    /// <summary>
    /// 获取道具大图
    /// </summary>
    public Sprite GetBigImg(ItemType type)
    {
        if (indexDict == null)
        {
            InitIndexDict();
        }

        return data[indexDict[type]].bigImg;
    }

    /// <summary>
    /// 获取道具的解锁关卡
    /// </summary>
    public int GetUnlockLvl(ItemType type)
    {
        if (indexDict == null)
        {
            InitIndexDict();
        }

        return data[indexDict[type]].unlockLvl;
    }


    public Dictionary<int, FreeItemConfig> freeItemConfig = new Dictionary<int, FreeItemConfig>();

    /// <summary>
    /// 解析 AB 配置：jdpx_item_free
    /// 格式：P1_M1_M2|P1_M1_M2
    /// 例如：4_2_1|5_3_2|6_5_1
    /// </summary>
    public void InitFreeItemConfig()
    {
        //string configStr = MiniGameSolution.Utilities.GetAbTestKeyValueSync("jdpx_item_free") ?? string.Empty;
        //string configStr = "4_2_2|5_3_2|6_4_2";
        string configStr = "0";
        // 无配置则清空
        if (string.IsNullOrWhiteSpace(configStr) || configStr == "0")
        {
            freeItemConfig.Clear();
            return;
        }

        freeItemConfig.Clear();
        string[] groups = configStr.Split('|');

        foreach (string g in groups)
        {
            string[] parts = g.Split('_');
            if (parts.Length < 3) continue;

            int p1 = int.Parse(parts[0]); // 道具类型
            int m1 = int.Parse(parts[1]); // 关卡
            int m2 = int.Parse(parts[2]); // 数量

            // 存入字典：用 P1 当 KEY
            freeItemConfig[p1] = new FreeItemConfig
            {
                itemType = p1,
                level = m1,
                count = m2
            };
        }
    }
}

// 免费道具配置（对应 P1_M1_M2）
[System.Serializable]
public class FreeItemConfig
{
    public int itemType; // P1：道具类型 4/5/6
    public int level; // M1：在第几关发放
    public int count; // M2：发放数量
}

[System.Serializable]
public class ItemData
{
    public ItemType type;
    public int price;
    public Sprite smallImg;
    public Sprite bigImg;
    public int unlockLvl;
}

/// <summary>
/// 奖励类型
/// </summary>
[System.Serializable]
public class RewardData
{
    public int gold;
    public List<ItemCount> items;
    public int AvatarNum;
    public List<StaminaCount> staminaItems;
    public int FrameNum;
    public void AddReward(int multiple = 1, string way = "", string type = "0")
    {
        var propAddSlot = "AddSlot";
        var propRemoveAimTape = "RemoveAimTape";
        var propClearWaitSlot = "ClearWaitSlot";
        if (gold > 0)
        {
            GameData.Gold.Add(gold * multiple, way, type);
        }

        if (items != null && items.Count > 0)
        {
            for (int i = 0; i < items.Count; i++)
            {
                string propId = propAddSlot;
                switch (items[i].type)
                {
                    case ItemType.Add:
                        propId = propAddSlot;
                        break;
                    case ItemType.Remove:
                        propId = propRemoveAimTape;
                        break;
                    case ItemType.Clear:
                        propId = propClearWaitSlot;
                        break;
                }

                //GameData.itemPlayerData.AddCount(items[i].type, items[i].count * multiple, way, type);
                

                //var findAimProp = GameData.getProps.Find(a => a.propId == propId);

                //if (findAimProp != null) {
                //    findAimProp.count += items[i].count * multiple;
                //}
                //else
                //{
                //    GameData.getProps.Add(new GetPropInfo() { propId = propId , count = items[i].count * multiple});
                //}
            }
        }
        if (staminaItems != null && staminaItems.Count > 0)
        {
            for (int i = 0; i < staminaItems.Count; i++)
            {
                string propId = propAddSlot;
                switch (staminaItems[i].type)
                {
                    case StaminaType.None:
                        break;
                    case StaminaType.Normal:
                        StaminaSystemData.AddStamina(staminaItems[i].count * multiple, "buy",true);//除金币购买一般可以超上限,可能不用buy
                        break;
                    case StaminaType.Maximum:
                        break;
                    case StaminaType.Unlimited:
                        break;
                    default:
                        break;
                }
            }
        }
    }
}