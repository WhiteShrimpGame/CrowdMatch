using CrowdMatch;
using UnityEngine;
using WsGame;
using WsGame.Time;

public static class StaminaSystemData
{
    private static StaminaConfig _config;
    public static StaminaConfig Config
    {
        get
        {
            if (_config == null)
            {
                _config = GameManager.Instance.staminaConfig;
                if (_config == null)
                {
                    Debug.LogError("未找到StaminaConfig配置文件！");
                }
            }
            return _config;
        }
    }

    public static bool IsActive()
    {
        return true;
        /*if (GameData.IsAllSystemOpen)
        {
            return true;
        }
        return Config.StaminaSwitch == 1;*/
    }
    // 数据存储
    public static StaminaDataToSave Data;
    public static bool _isInit;

    // 无限体力 是否暂停中
    //public static bool IsInfinitePaused;

    // 无限体力判断（配置关卡 + 配置时长）
    public static bool IsInfiniteStamina
    {
        get
        {
            // 1. 体力开关关闭 = 无限体力
            if (!IsActive()) return false;
            // 2. 当前关卡 <= 配置无限关数 = 无限体力
            if (GameData.CurrentLevel <= Config.UnlimitedStaminaLevel) return true;
            // 3. 时长型无限体力（暂停时永远生效）
            if (Data.IsInfinitePaused) return Data.InfiniteEndTime > 0;
            // 正常计时
            return Data.InfiniteEndTime > GetCurrentTime();
        }
    }

    // 配置映射（自动从ScriptableObject读取）
    public static int MaxStamina => Config.StaminaMaxValue;
    public static int StaminaRecoverInterval => Config.RecoverTimeMinutes * 60; // 转秒

    // 无限体力剩余时间（支持暂停）
    public static int InfiniteRemainTime
    {
        get
        {
            if (Data.IsInfinitePaused)
            {
                // 暂停：返回当前剩余时间，不减少
                return Mathf.Max(0, Data.InfiniteRemainWhenPaused);
            }
            // 正常计时
            long now = GetCurrentTime();
            return Data.InfiniteEndTime > now ? (int)(Data.InfiniteEndTime - now) : 0;
        }
    }

    #region 初始化
    public static void InitData()
    {
        if (_isInit) return;
        Load();
        // 初始化远程配置
        Config.InitStaminaConfig();
        DataCheck();//先获取当前时间
        CalculateOfflineStamina();//再计算离线体力
        if (GameData.CurrentLevel <= Config.UnlimitedStaminaLevel)//是不是处于无限体力关
        {
            PauseInfiniteTime();
        }
        else
        {
            ResumeInfiniteTime();
        }
        _isInit = true;
    }

    private static void DataCheck()
    {
        Data ??= new StaminaDataToSave();
        Data.CurrentStamina = Mathf.Max(0, Data.CurrentStamina);
        // 初始体力（配置值）
        if (Data.LastRecoverTime <= 0)
        {
            Data.CurrentStamina = Config.StaminaInitValue;
            Data.LastRecoverTime = GetCurrentTime();
        }
        Save();
    }
    #endregion

    #region 离线体力结算
    private static void CalculateOfflineStamina()
    {
        /*if (IsInfiniteStamina || Data.CurrentStamina >= MaxStamina) return;

        int now = GetCurrentTime();
        long offlineTime = now - Data.LastRecoverTime;
        if (offlineTime <= 0) return;

        int recoverCount = Mathf.FloorToInt((float)(offlineTime / StaminaRecoverInterval));
        if (recoverCount <= 0) return;

        int newStamina = Data.CurrentStamina + recoverCount;
        Data.CurrentStamina = Mathf.Min(newStamina, MaxStamina);
        Data.LastRecoverTime += Data.LastRecoverTime + (MaxStamina - newStamina) * StaminaRecoverInterval;*/
    }
    #endregion

    #region 体力核心接口
    public static int GetCurrentStamina()
    {
        if (!_isInit) InitData();
        return IsInfiniteStamina ? 999 : Data.CurrentStamina;
    }

    public static bool HasEnoughStamina(int cost = 1) => IsInfiniteStamina || Data.CurrentStamina >= cost;

    public static bool CostStamina(int cost = 1)
    {
        if (IsInfiniteStamina) return true;
        if (!HasEnoughStamina(cost)) return false;
        
        int count = Data.CurrentStamina;
        Data.CurrentStamina -= cost;
        if (Data.CurrentStamina<0)
        {
            Data.CurrentStamina = 0;
            Debug.LogError("数据异常，体力小于0，已置为0");
        }
        //原本满，扣完不满
        if (count>=MaxStamina&&Data.CurrentStamina<MaxStamina)
        {
            Data.LastRecoverTime = GetCurrentTime();
        }
        Save();
        return true;
    }

    public static void AddStamina(int value, string way, bool canOverLimit = false)
    {
        if (!_isInit) InitData();
        if (IsInfiniteStamina) return;

        if (!canOverLimit)
            Data.CurrentStamina = Mathf.Min(Data.CurrentStamina + value, MaxStamina);
        else
            Data.CurrentStamina += value;
        if (Data.CurrentStamina >= MaxStamina)
        {
            Data.LastRecoverTime = GetCurrentTime();
        }
        //Reporter.StaminaGet(value, way);
        Save();
    }

    /*public static void SetStaminaMax(int value, string way)
    {
        if (!_isInit) InitData();
        GameManager.Instance.staminaConfig.StaminaMaxValue = 8;
        Data.StaminaMaxValueIsAdd = true;
        Save();
    }*/

    // 增加无限时长
    public static void AddInfiniteStamina(int seconds, string way)
    {
        if (!_isInit) InitData();
        int now = GetCurrentTime();
        if (GameData.CurrentLevel <= Config.UnlimitedStaminaLevel)//是不是处于无限体力关
        {
            Data.IsInfinitePaused = true;
            PauseInfiniteTime();
        }
        else
        {
            Data.IsInfinitePaused = false;
            ResumeInfiniteTime();
        }
        if (Data.IsInfinitePaused)
        {
            // 暂停中：直接加剩余时间
            Data.InfiniteRemainWhenPaused += seconds;
        }
        else
        {
            // 正常计时
            if (Data.InfiniteEndTime <= now) Data.InfiniteEndTime = now;
            Data.InfiniteEndTime += seconds;
        }

        //Reporter.UnlimitedStaminaGet(seconds / 60, way);
        Save();
    }
    #endregion

    #region 无限体力 暂停 / 恢复 接口
    /// <summary>
    /// 暂停无限体力计时
    /// </summary>
    public static void PauseInfiniteTime()
    {
        if (Data.IsInfinitePaused) return;
        if (Data.InfiniteEndTime <= GetCurrentTime()) return;

        // 记录剩余秒数
        Data.InfiniteRemainWhenPaused = InfiniteRemainTime;
        Data.IsInfinitePaused = true;
        Save();
        Debug.Log("无限体力计时已暂停");
    }

    /// <summary>
    /// 恢复无限体力计时
    /// </summary>
    public static void ResumeInfiniteTime()
    {
        if (!Data.IsInfinitePaused) return;

        // 把剩余时间写回结束时间
        int now = GetCurrentTime();
        Data.InfiniteEndTime = now + Data.InfiniteRemainWhenPaused;
        Data.InfiniteRemainWhenPaused = 0;
        Data.IsInfinitePaused = false;
        Save();
        Debug.Log("无限体力已恢复计时");
    }
    #endregion

    #region 存储
    public static int GetCurrentTime() => TimeUtils.GetCurrentTime();
    public static readonly string StaminaDataKey = "staminasystemdata";
    public static bool IsFirstNoStamina
    {
        get => Data.IsFirstNoStamina;
        set
        {
            Data.IsFirstNoStamina = value;
            Save();
        }
    }
    public static void Load()
    {
        string json = PlayerPrefs.GetString(StaminaDataKey, "");
        Data = string.IsNullOrEmpty(json) ? new StaminaDataToSave() : JsonUtility.FromJson<StaminaDataToSave>(json);
    }

    public static void Save()
    {
        if (Data == null) return;
        string json = JsonUtility.ToJson(Data);
        PlayerPrefs.SetString(StaminaDataKey, json);
    }

    public static void ResetData()
    {
        Data = new StaminaDataToSave
        {
            CurrentStamina = Config.StaminaInitValue,
            LastRecoverTime = GetCurrentTime(),
            InfiniteEndTime = 0,
            IsFirstNoStamina = false,
            StaminaMaxValueIsAdd = false,
            InfiniteRemainWhenPaused = 0,
            IsInfinitePaused = false

        };
        _isInit=false;
        InitData();
        Save();
    }
    #endregion

    #region 云存档
    /*public static void StorageData(WS_TapAway_Cloud.DataStorage data)
    {
        data.staminaSystemDataJson =
            MiniGameSolution.Instance.MiniGameUtilities.PlayerPrefs.GetString(StaminaDataKey, "");
    }
    public static void WriteByDataStorage(WS_TapAway_Cloud.DataStorage data)
    {
        if (!string.IsNullOrEmpty(data.staminaSystemDataJson))
        {
            Data = JsonUtility.FromJson<StaminaDataToSave>(data.staminaSystemDataJson);
            Save();
        }
        else Data = new StaminaDataToSave();
    }*/
    #endregion
}

[System.Serializable]
public class StaminaDataToSave
{
    public int CurrentStamina;
    //体力开始倒计时时间
    public int LastRecoverTime;
    public int InfiniteEndTime;
    public bool IsFirstNoStamina;
    public bool StaminaMaxValueIsAdd = false;

    // 暂停时保存剩余时间
    public int InfiniteRemainWhenPaused;
    // 无限体力 是否暂停中
    public bool IsInfinitePaused;
}