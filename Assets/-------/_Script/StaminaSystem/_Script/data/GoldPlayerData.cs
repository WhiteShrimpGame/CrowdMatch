using UnityEngine;

public class GoldPlayerData
{
    private const string Key = "PlayerGold";
    private int _count = -1;

    public int Count
    {
        get
        {
            if (_count < 0)
            {
                _count = PlayerPrefs.GetInt(Key, -2);
                if (_count == -2)
                {
                    _count = 0;
                    PlayerPrefs.SetInt(Key, 0);
                    var begin = GoldConfig.GetBeginGold();
                    if (begin > 0)
                    {
                        return Add(begin, "begin");
                    }
                }
            }

            return _count;
        }
        private set
        {
            _count = value > 0 ? value : 0;
            PlayerPrefs.SetInt(Key, _count);
        }
    }

    public int Cost(int cost, string way)
    {
        if (Count < cost)
        {
            Debug.LogErrorFormat("Cost gold out of remain, cost {0}, remain {1}", cost, Count);
            Count = 0;
        }

        Count -= cost;
        //TaskSystemData.SetTaskProgressByTaskId(TaskID.ConsumeGold, cost);
        //QFramework.TypeEventSystem.Global.Send(new WsGame.TaskSystem.RefreshTaskProgress("GoldCost", cost));
        //Reporter.GoldCost(cost, way);
        return Count;
    }

    public int Add(int add, string way, string type = "0")
    {
        Count += add;
        //Reporter.GoldGet(add, way, type);
        return Count;
    }

    public void Clear()
    {
        _count = 0;
        var begin = GoldConfig.GetBeginGold();
        if (begin > 0)
        {
            Add(begin, "begin");
        }
    }

    public bool CheckEnough(int cost)
    {
        return Count >= cost;
    }

    /*public void WriteByDataStorage(WS_TapAway_Cloud.DataStorage data)
    {
        Count = data.goldCount;
    }*/
}