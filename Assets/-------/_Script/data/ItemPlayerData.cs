using System.Collections.Generic;
using UnityEngine;

public class ItemPlayerData
{
    public string GetPlayerPrefStr(ItemType type)
    {
        switch (type)
        {
            case ItemType.Add:
                return "PlayerPickCount";
            case ItemType.Remove:
                return "PlayerRemoveCount";
            case ItemType.Clear:
                return "PlayerVIPCount";
            default:
                return "PlayerPickCount";
        }
    }

    public Dictionary<ItemType, int> dataDict = new Dictionary<ItemType, int>();

    public int GetCount(ItemType type)
    {
        if (!dataDict.ContainsKey(type))
        {
            dataDict.Add(type,
                PlayerPrefs.GetInt(GetPlayerPrefStr(type), 0));
        }

        return dataDict[type];
    }

    public string GetCountString()
    {
        return GetCount(ItemType.Add) + "_" + GetCount(ItemType.Remove) + "_" + GetCount(ItemType.Clear);
    }

    public void SetCount(ItemType type, int value)
    {
        if (dataDict.ContainsKey(type))
            dataDict[type] = value;
        else
            dataDict.Add(type, value);

        PlayerPrefs.SetInt(GetPlayerPrefStr(type), value);
    }

    public int CostCount(ItemType type)
    {
        //Reporter.ItemCost(type);

        int count = GetCount(type);
        if (count == 0)
        {
            return 0;
        }

        count--;
        SetCount(type, count);

        /*switch (type)
        {
            case ItemType.Add:
                TaskSystemData.SetTaskProgressByTaskId(TaskID.UseAddProp, 1);
                break;
            case ItemType.Remove:
                TaskSystemData.SetTaskProgressByTaskId(TaskID.UseRemoveProp, 1);
                break;
            case ItemType.Clear:
                TaskSystemData.SetTaskProgressByTaskId(TaskID.UseClearProp, 1);
                break;
        }*/

        return count;
    }

    public int AddCount(ItemType type, int add = 1, string way = "", string getType = "0", bool needReport = true)
    {
        if (needReport)
        {
            for (int i = 0; i < add; i++)
            {
                //Reporter.ItemGet(type, way, getType);
            }
        }

        int count = GetCount(type);

        count += add;
        SetCount(type, count);
        return count;
    }

    public void Clear()
    {
        dataDict.Clear();
    }

    /*public void StorageData(WS_TapAway_Cloud.DataStorage data)
    {
        var list = new List<ItemCount>();
        list.Add(new ItemCount(ItemType.Add, GetCount(ItemType.Add)));
        list.Add(new ItemCount(ItemType.Remove, GetCount(ItemType.Remove)));
        list.Add(new ItemCount(ItemType.Clear, GetCount(ItemType.Clear)));

        data.items = list;
    }

    public void WriteByDataStorage(WS_TapAway_Cloud.DataStorage data)
    {
        for (int i = 0; i < data.items.Count; i++)
        {
            SetCount(data.items[i].type, data.items[i].count);
        }
    }*/
}

public enum ItemType
{
    None = 0,
    Add = 1,
    Remove = 2,
    Clear = 3,
}
[System.Serializable]
public class ItemCount
{
    public ItemType type;
    public int count;

    public ItemCount(ItemType type, int count)
    {
        this.type = type;
        this.count = count;
    }
}
public enum StaminaType
{
    None = 0,
    Normal = 1,
    Maximum = 2,
    Unlimited = 3,
}
[System.Serializable]
public class StaminaCount
{
    public StaminaType type;
    public int count;

    public StaminaCount(StaminaType type, int count)
    {
        this.type = type;
        this.count = count;
    }
}