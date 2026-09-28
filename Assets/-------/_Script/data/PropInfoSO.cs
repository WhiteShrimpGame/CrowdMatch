using UnityEngine;


[System.Serializable]
public class PropInfo
{
    public ItemType propType;
    public string propId;
    public string propName;
    public string buyLogWay;
    public string adSceneId;
    public Sprite propSpr;
    public Sprite propSmallSpr;
    public int price;
    [TextArea(3, 5)]
    public string propContent;
    public bool isShowTextTip = false;

    public static ItemType PropToType(string prop)
    {
        switch (prop)
        {
            case "AddSlot":
                return ItemType.Add;
            case "RemoveAimTape":
                return ItemType.Remove;
            case "ClearWaitSlot":
                return ItemType.Clear;
            default:
                return ItemType.None;
        }
    }

    public static string TypeToProp(ItemType type)
    {
        switch (type)
        {
            case ItemType.Add:
                return "AddSlot";
            case ItemType.Remove:
                return "RemoveAimTape";
            case ItemType.Clear:
                return "ClearWaitSlot";
            default:
                return "";
        }
    }
}

[System.Serializable]
public class GetPropInfo 
{
    public string propId;
    public int count;
}
