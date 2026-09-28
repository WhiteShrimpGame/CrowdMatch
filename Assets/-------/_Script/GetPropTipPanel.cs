using System;
using CrowdMatch;
using UnityEngine;
using UnityEngine.UI;
using WsGame;
public class GetPropTipPanel : MonoBehaviour
{
    [Header("UIReference")]
    [SerializeField] Button closeBtn;
    [SerializeField] Button getPropBtn;
    [SerializeField] Button getADPropBtn;
    [SerializeField] Text titleNameText;
    [SerializeField] Text propContentText;
    [SerializeField] Text goldText;
    [SerializeField] Image propIcon;
    [SerializeField] Text goldCountText;
    private EnableBtn goldEnableBtn;

    private Action<int, int> GetTipAction;
    private ItemType itemType;
    private int price;
    private int count;
    private string buyLogWay;
    private string adSceneId;

    private void Awake()
    {
        closeBtn.onClick.AddListener(ClosePanelMethod);
        getPropBtn.onClick.AddListener(GetPropPanelMethod);
        getADPropBtn.onClick.AddListener(GetADPropPanelMethod);
    }

    /// <summary>
    /// 获得道具
    /// </summary>
    private void GetPropPanelMethod()
    {
        AudioManager.Instance.PlayButtonAudioAndVibrate();
        if(GetTipAction == null)return;

        if (price > 0 && GameData.Gold.CheckEnough(price * count))
        {
            GameData.Gold.Cost(price * count, buyLogWay);
            GetTipAction?.Invoke(0, count);
            RefreshGoldCount();
            ClosePanelMethod();
        }
        else
        {
            UIManager.Instance.ShowTip("金币不够");
        }
    }

    /// <summary>
    /// 看广告获得道具
    /// </summary>
    private void GetADPropPanelMethod()
    {
        AudioManager.Instance.PlayButtonAudioAndVibrate();
        if(GetTipAction == null)return;
        GetTipAction?.Invoke(1, 1);
        RefreshGoldCount();
        UIManager.Instance.ShowPropGetTip(false);
        /*MiniGameSolution.Ad.ShowRewardAd(() =>
        {
            Debug.Log("激励回调成功, 发放奖励");
            GetTipAction?.Invoke(1, 1);
            GoldConfig.AddItemAdCount(itemType);
            WS_TapAway_Cloud.LevelRecord.SaveLevelRecord();
            ClosePanelMethod();
        }, sceneId: adSceneId);*/
    }

    /// <summary>
    /// 显示道具信息
    /// </summary>
    public void InitPropPanel(PropInfo aimInfo , Action<int, int> triggerMethod) 
    {
        count = GoldConfig.GetGoldGetItemCount();

        if (aimInfo != null) {
            titleNameText.text = aimInfo.propName;
            propContentText.text = aimInfo.propContent;
            propIcon.sprite = aimInfo.propSpr;
            buyLogWay = aimInfo.buyLogWay;
            adSceneId = aimInfo.adSceneId;
            //price = int.Parse(aimInfo.price);
            price = GoldConfig.GetItemPrice(aimInfo.propType);
            itemType = aimInfo.propType;
        }
        RefreshGoldCount();

        if (GoldConfig.IsItemAdOutOfUse(aimInfo.propType))
        {
            getADPropBtn.gameObject.SetActive(false);
        }
        else
        {
            //Reporter.VideoShow(adSceneId);
        }

        if (GoldConfig.IsGoldGetItemActive())
        {
            if (count == 1)
            {
                goldText.text = price.ToString();
            }
            else
            {
                getPropBtn.gameObject.SetActive(false);
            }
        }
        else
        {
            getPropBtn.gameObject.SetActive(false);
        }

        GetTipAction = triggerMethod;
    }

    /// <summary>
    /// 更新金币
    /// </summary>
    public void RefreshGoldCount()
    {
        goldCountText.text = ((float)GameData.Gold.Count).ConvertToKMGString();
    }

    private void ClosePanelMethod()
    {
        AudioManager.Instance.PlayButtonAudioAndVibrate();

        UIManager.Instance.ShowPropGetTip(false);
    }

    private void OnDisable()
    {
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        closeBtn.onClick.RemoveListener(ClosePanelMethod);
        getPropBtn.onClick.RemoveListener(GetPropPanelMethod);
        getADPropBtn.onClick.RemoveListener(GetADPropPanelMethod);
    }

}
