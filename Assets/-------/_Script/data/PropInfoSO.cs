using UnityEngine;


[System.Serializable]
public class PropInfo
{
    public ItemType propType;
    //public string propId;
    public string propName;
    public string buyLogWay;
    public string adSceneId;
    public Sprite propSpr;
    //public Sprite propSmallSpr;
    //public int price;
    [TextArea(3, 5)]
    public string propContent;
    //public bool isShowTextTip = false;

    /*public static ItemType PropToType(string prop)
    {
        switch (prop)
        {
            case "Refresh":
                return ItemType.Refresh;
            case "Magnet":
                return ItemType.Magnet;
            case "Remove":
                return ItemType.Remove;
            default:
                return ItemType.None;
        }
    }

    public static string TypeToProp(ItemType type)
    {
        switch (type)
        {
            case ItemType.Refresh:
                return "Refresh";
            case ItemType.Magnet:
                return "Magnet";
            case ItemType.Remove:
                return "Remove";
            default:
                return "";
        }
    }*/
}

/*[System.Serializable]
public class GetPropInfo 
{
    public string propId;
    public int count;
}*/
