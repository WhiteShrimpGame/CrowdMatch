using System.Collections;
using System.Collections.Generic;
using CrowdMatch;
using UnityEngine;
[CreateAssetMenu(fileName = "StaminaConfig")]
public class StaminaConfig : ScriptableObject
{
    #region 体力配置参数

    public int StaminaSwitch;      // 0开关
    public int UnlimitedStaminaLevel ; //1无限体力关数
    public int StaminaInitValue ;    //2初始值
    public int StaminaMaxValue ;     //3上限
    public int AdStaminaValue ;       //4广告体力
    public int RecoverTimeMinutes ;  //5恢复时间
    public int GoldBuyPrice ;         //6金币单价
    #endregion

// 默认固定配置
    private const string DEFAULT_CONFIG = "0_0_5_5_1_30_100";

    public void InitStaminaConfig()
    {
        // 1. 先加载默认完整配置
        string[] defaultArr = DEFAULT_CONFIG.Split('_');
        SetConfigValues(defaultArr);

        /*// 2. 获取远程配置
        string remoteConfig = MiniGameSolution.Utilities.GetAbTestKeyValueSync("jdpx_Life_Config");
        //remoteConfig="1_2_5_5_1_5_100";
        //remoteConfig="1";
        if (GameData.IsAllSystemOpen)
        {
            remoteConfig="1";
        }
        if (string.IsNullOrWhiteSpace(remoteConfig)) return;

        // 3. 远程配置仅覆盖【前N个参数】，后面保持默认
        string[] remoteArr = remoteConfig.Split('_');
        for (int i = 0; i < remoteArr.Length; i++)
        {
            // 只覆盖前7个以内的参数（防止越界）
            if (i >= defaultArr.Length) break;
        
            if (int.TryParse(remoteArr[i], out int value))
            {
                SetConfigValueByIndex(i, value);
            }
        }*/
    }

// 批量赋值配置
    private void SetConfigValues(string[] arr)
    {
        for (int i = 0; i < arr.Length; i++)
        {
            if (int.TryParse(arr[i], out int value))
            {
                SetConfigValueByIndex(i, value);
            }
        }
    }

// 根据索引赋值（核心对应7个参数）
    private void SetConfigValueByIndex(int index, int value)
    {
        switch (index)
        {
            case 0: StaminaSwitch = value; break;
            case 1: UnlimitedStaminaLevel = value; break;
            case 2: StaminaInitValue = value; break;
            case 3:
                if (StaminaSystemData.Data.StaminaMaxValueIsAdd)
                {
                    StaminaMaxValue = 8;
                }
                else
                {
                    StaminaMaxValue = value; 
                }
                break;
            case 4: AdStaminaValue = value; break;
            case 5: RecoverTimeMinutes = value; break;
            case 6: GoldBuyPrice = value; break;
        }
    }
}
