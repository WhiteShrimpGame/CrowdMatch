using CrowdMatch;

public static class GoldConfig
{
    /*public static void InitLevelData()
    {
        GameData.MaxItemAdGetCount = GetAdGetItemTime();
        GameData.MaxReviveAdCount = GetAdReviveTime();
        GameData.MaxReviveGoldCount = GetGoldReviveTime();
    }

    public static bool IsItemOutOfUse(ItemType type)
    {
        if (IsGoldGetItemActive())
        {
            return false;
        }

        return IsItemAdOutOfUse(type);
    }

    public static bool IsItemAdOutOfUse(ItemType type)
    {
        if (GameData.MaxItemAdGetCount < 0)
        {
            return false;
        }

        if (GameData.MaxItemAdGetCount == 0)
        {
            return true;
        }

        if (!GameData.ItemAdGetCountDict.ContainsKey(type))
        {
            return false;
        }

        return GameData.ItemAdGetCountDict[type] >= GameData.MaxItemAdGetCount;
    }

    public static bool IsReviveOutOfUse()
    {
        return IsReviveAdOutOfUse() && IsReviveGoldOutOfUse();
    }

    public static bool IsReviveAdOutOfUse()
    {
        if (!IsAdReviveActive())
        {
            return true;
        }

        if (GameData.MaxReviveAdCount <= 0)
        {
            return false;
        }

        return GameData.MaxReviveAdCount <= GameData.ReviveAdCount;
    }

    public static bool IsReviveGoldOutOfUse()
    {
        if (!IsGoldReviveActive())
        {
            return true;
        }

        if (GameData.MaxReviveGoldCount <= 0)
        {
            return false;
        }

        return GameData.MaxReviveGoldCount <= GameData.ReviveGoldCount;
    }

    public static void AddItemAdCount(ItemType type)
    {
        if (GameData.MaxItemAdGetCount <= 0)
        {
            return;
        }

        if (!GameData.ItemAdGetCountDict.ContainsKey(type))
        {
            GameData.ItemAdGetCountDict.Add(type, 1);
        }
        else
        {
            GameData.ItemAdGetCountDict[type]++;
        }
    }

    public static bool IsGoldGetItemActive()
    {
        if (!TryGetItemPriceParams(0, out var str))
        {
            return true;
        }
        if (!int.TryParse(str, out var val))
        {
            return true;
        }

        if (val == 0)
        {
            return false;
        }

        return GameData.CurrentLevel >= val;
    }

    public static bool IsGoldGetBoxActive()
    {
        if (!TryGetItemPriceParams(1, out var str))
        {
            return false;
        }
        if (!int.TryParse(str, out var val))
        {
            return false;
        }

        if (val == 0)
        {
            return false;
        }

        return GameData.CurrentLevel >= val;
    }

    public static int GetAdGetItemTime()
    {
        if (!TryGetItemPriceParams(2, out var str))
        {
            return -1;
        }
        if (!int.TryParse(str, out var val))
        {
            return -1;
        }

        if (val < 0)
        {
            return -1;
        }

        return val;
    }

    public static int GetGoldGetItemCount()
    {
        if (!TryGetItemPriceParams(3, out var str))
        {
            return 1;
        }
        if (!int.TryParse(str, out var val))
        {
            return 1;
        }

        if (val <= 0)
        {
            return 1;
        }

        return val;
    }*/

    public static int GetItemPrice(ItemType type)
    {
        int index;
        int defaultVal;
        switch (type)
        {
            case ItemType.Add:
                index = 4;
                defaultVal = 100;
                break;
            case ItemType.Remove:
                index = 5;
                defaultVal = 150;
                break;
            case ItemType.Clear:
                index = 6;
                defaultVal = 200;
                break;
            default:
                index = 4;
                defaultVal = 100;
                break;
        }

        if (!TryGetItemPriceParams(index, out var str))
        {
            return defaultVal;
        }
        if (!int.TryParse(str, out var val))
        {
            return defaultVal;
        }

        if (val == 0)
        {
            return defaultVal;
        }

        return val;
    }

    public static int GetBoxPrice()
    {
        if (!TryGetItemPriceParams(7, out var str))
        {
            return 300;
        }
        if (!int.TryParse(str, out var val))
        {
            return 300;
        }

        if (val == 0)
        {
            return 300;
        }

        return val;
    }

    public static bool IsAdReviveActive()
    {
        if (!TryGetRevivePriceParams(0, out var str))
        {
            return true;
        }
        if (!int.TryParse(str, out var val))
        {
            return true;
        }

        if (val == 0)
        {
            return false;
        }

        return GameData.CurrentLevel >= val;
    }

    public static bool IsGoldReviveActive()
    {
        if (!TryGetRevivePriceParams(2, out var str))
        {
            return false;
        }
        if (!int.TryParse(str, out var val))
        {
            return false;
        }

        if (val == 0)
        {
            return false;
        }

        return GameData.CurrentLevel >= val;
    }

    public static int GetAdReviveTime()
    {
        if (!TryGetRevivePriceParams(1, out var str))
        {
            return -1;
        }
        if (!int.TryParse(str, out var val))
        {
            return -1;
        }

        if (val <= 0)
        {
            return -1;
        }

        return val;
    }

    public static int GetGoldReviveTime()
    {
        if (!TryGetRevivePriceParams(3, out var str))
        {
            return -1;
        }
        if (!int.TryParse(str, out var val))
        {
            return -1;
        }

        if (val <= 0)
        {
            return -1;
        }

        return val;
    }

    public static int GetGoldRevivePrice()
    {
        /*if (!TryGetRevivePriceParams(4, out var str))
        {
            return 150;
        }

        var priceStr = str.Split('|');
        if (priceStr.Length == 0)
        {
            return 150;
        }

        var time = GameData.ReviveGoldCount;
        if (time >= priceStr.Length)
        {
            time = priceStr.Length - 1;
        }

        if (!int.TryParse(priceStr[time], out var val))
        {
            return 150;
        }

        return val;*/
        return 123;
    }

    public static int GetBeginGold()
    {
        if (!TryGetGoldParams(0, out var str))
        {
            return 0;
        }
        if (!int.TryParse(str, out var val))
        {
            return 0;
        }

        if (val <= 0)
        {
            return 0;
        }

        return val;
    }

    public static int GetWinGold()
    {
        if (!TryGetGoldParams(1, out var str))
        {
            return 10;
        }
        if (!int.TryParse(str, out var val))
        {
            return 10;
        }

        if (val <= 0)
        {
            return 10;
        }

        return val;
    }

    public static int GetWinGoldMultiCount()
    {
        if (!TryGetGoldParams(2, out var str))
        {
            return 3;
        }
        if (!int.TryParse(str, out var val))
        {
            return 3;
        }

        if (val <= 1)
        {
            return 3;
        }

        return val;
    }

    private static bool TryGetItemPriceParams(int index, out string val)
    {
        //var str = "10_15_2_7_70_90_110_140";
        //var str = MiniGameSolution.Utilities.GetAbTestKeyValueSync("jdpx_item_price");
        var str = "0";
        if (str == "" || str == "0")
        {
            val = "";
            return false;
        }
        var strs = str.Split('_');
        if (strs.Length <= index)
        {
            val = "";
            return false;
        }
        val = strs[index];
        return true;
    }

    private static bool TryGetRevivePriceParams(int index, out string val)
    {
        //var str = "10_1_15_0_25|50|75";
        //var str = MiniGameSolution.Utilities.GetAbTestKeyValueSync("jdpx_gold_revive");
        var str = "0";
        if (str == "" || str == "0")
        {
            val = "";
            return false;
        }
        var strs = str.Split('_');
        if (strs.Length <= index)
        {
            val = "";
            return false;
        }
        val = strs[index];
        return true;
    }

    private static bool TryGetGoldParams(int index, out string val)
    {
        //var str = "450_15_9";
        //var str = MiniGameSolution.Utilities.GetAbTestKeyValueSync("jdpx_gold_num");
        var str = "0";
        if (str == "" || str == "0")
        {
            val = "";
            return false;
        }
        var strs = str.Split('_');
        if (strs.Length <= index)
        {
            val = "";
            return false;
        }
        val = strs[index];
        return true;
    }
}