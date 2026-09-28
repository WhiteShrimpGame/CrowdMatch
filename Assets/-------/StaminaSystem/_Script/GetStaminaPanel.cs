using System;
using System.Collections;
using System.Collections.Generic;
using CrowdMatch;
using UnityEngine;
using UnityEngine.UI;
using WsGame;

public class GetStaminaPanel : MonoBehaviour
{
    [Header("UIReference")]
    [SerializeField] Button closeBtn;
    [SerializeField] Button getCoinBtn;
    [SerializeField] Button getADBtn;
    [SerializeField] Button fullBtn;
    [SerializeField] Button freeBtn;
    [SerializeField] Text goldCountText;
    [SerializeField] Text allPrice;
    [SerializeField] Text lifeCountText;
    [SerializeField] Text timeText;
    [SerializeField] Text lifeAD;
    [SerializeField] Text lifeCoin;
    [SerializeField] GameObject midObj;
    [SerializeField] GameObject moveObj;
    [SerializeField] Text tipText;
    private int price;
    private string buyLogWay;
    private string adSceneId;
    private StaminaConfig staminaConfig;
    private void Awake()
    {
        staminaConfig = GameManager.Instance.staminaConfig;
        closeBtn.onClick.AddListener(ClosePanelMethod);
        getCoinBtn.onClick.AddListener(GetPropPanelMethod);
        getADBtn.onClick.AddListener(GetADPropPanelMethod);
        fullBtn.onClick.AddListener(OnFullButtonClick);
        freeBtn.onClick.AddListener(OnFreeButtOnClick);
        buyLogWay = "GetStaminaPanel";
        RefreshUI();
        RefreshStaminaUI();
    }
    /// <summary>
    /// 获得体力
    /// </summary>
    private void GetPropPanelMethod()
    {
        //StaminaSystemData.AddStamina(1,"coin",false);
        AudioManager.Instance.PlayButtonAudioAndVibrate();
        int count = StaminaSystemData.MaxStamina - StaminaSystemData.GetCurrentStamina();
        if (price > 0 && GameData.Gold.CheckEnough(price*count))
        {
            GameData.Gold.Cost(price*count, buyLogWay);
            //StaminaSystemData.AddStamina(999,false);
            StaminaSystemData.AddStamina(count,"coin",false);
            //Reporter.StaminaGet(count,"coin");
            RefreshUI();
            //UIManager.Instance.mainPanel.GetComponent<MainPanel>().RefreshGoldCount();
            ClosePanelMethod();
        }
        else
        {
            UIManager.Instance.ShowTip("金币不够");
        }
    }
    void OnFreeButtOnClick()
    {
        AudioManager.Instance.PlayButtonAudioAndVibrate();
        StaminaSystemData.AddStamina(StaminaSystemData.MaxStamina, "free",false);
        StaminaSystemData.IsFirstNoStamina = true;
        //Reporter.StaminaGet(StaminaSystemData.MaxStamina, "free");
        RefreshUI();
        ClosePanelMethod();
    }
    void OnFullButtonClick()
    {
        AudioManager.Instance.PlayButtonAudioAndVibrate();
        UIManager.Instance.ShowTip("当前体力已满");
    }
    /// <summary>
    /// 看广告获得体力
    /// </summary>
    private void GetADPropPanelMethod()
    {
        StaminaSystemData.AddStamina(staminaConfig.AdStaminaValue,"ad",true);
        /*AudioManager.Instance.PlayButtonAudioAndVibrate();

        MiniGameSolution.Ad.ShowRewardAd(() =>
        {
            Debug.Log("激励回调成功, 发放奖励");
            StaminaSystemData.AddStamina(staminaConfig.AdStaminaValue,"ad",true);
            //Reporter.StaminaGet(staminaConfig.AdStaminaValue,"ad");
            ClosePanelMethod();
        }, sceneId: adSceneId);*/
    }
    /// <summary>
    /// 更新ui
    /// </summary>
    public void RefreshUI()
    {
        if (StaminaSystemData.GetCurrentStamina() >= StaminaSystemData.MaxStamina)
        {
            getCoinBtn?.gameObject.SetActive(false);
            getADBtn?.gameObject.SetActive(false);
            fullBtn?.gameObject.SetActive(true);
            freeBtn?.gameObject.SetActive(false);
            midObj?.SetActive(false);
            tipText.text = "当前体力已满";
            //moveObj.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -65);
        }
        else if (!StaminaSystemData.IsFirstNoStamina &&StaminaSystemData.GetCurrentStamina()==0 && GameData.CurrentLevel<=5)
        {
            //StaminaSystemData.IsFirstNoStamina = true;//免费领取后执行
            getCoinBtn?.gameObject.SetActive(false);
            getADBtn?.gameObject.SetActive(false);
            freeBtn?.gameObject.SetActive(true);
        }
        else
        {
            getCoinBtn?.gameObject.SetActive(true);
            getADBtn?.gameObject.SetActive(true);
            fullBtn?.gameObject.SetActive(false);
            freeBtn?.gameObject.SetActive(false);
        }
        lifeCountText.text=StaminaSystemData.GetCurrentStamina().ToString();
        lifeAD.text=staminaConfig.AdStaminaValue.ToString();
        lifeCoin.text= (StaminaSystemData.MaxStamina- StaminaSystemData.GetCurrentStamina()).ToString();
        price = staminaConfig.GoldBuyPrice;
        allPrice.text = (price*(StaminaSystemData.MaxStamina-StaminaSystemData.GetCurrentStamina())).ToString();
        goldCountText.text = ((float)GameData.Gold.Count).ConvertToKMGString();
        UIManager.Instance.gameInnerUI.RefreshGoldCount();
    }

    private void ClosePanelMethod()
    {
        AudioManager.Instance.PlayButtonAudioAndVibrate();

        UIManager.Instance.ShowStaminaPanel(false);
    }

    private void OnDisable()
    {
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        closeBtn.onClick.RemoveListener(ClosePanelMethod);
        getCoinBtn.onClick.RemoveListener(GetPropPanelMethod);
        getADBtn.onClick.RemoveListener(GetADPropPanelMethod);
        fullBtn.onClick.RemoveListener(OnFreeButtOnClick);
    }
    private void RefreshStaminaUI()
    {
        if (!StaminaSystemData._isInit) return;
        // 正常体力显示
        int cur = StaminaSystemData.GetCurrentStamina();
        int max = StaminaSystemData.MaxStamina;
        //staminaCountText.text = $"{cur}/{max}";
        timeText.text = $"{cur}";
        //infiniteStaminaIcon.gameObject.SetActive(false);

        // 满体力 → 显示已满
        if (cur >= max)
        {
            timeText.text = "已满";
            return;
        }

        // 倒计时显示
        double time = StaminaSystemTimer.Instance.GetRemainRecoverTime();
        int m = Mathf.FloorToInt((float)time / 60);
        int s = Mathf.FloorToInt((float)time % 60);
        timeText.text = $"{m:D2}分{s:D2}秒";
    }
    private float staminaRefreshTimer;     
    private void Update()
    {
        staminaRefreshTimer += Time.deltaTime;
        if (staminaRefreshTimer >= 1f)
        {
            RefreshStaminaUI();
            RefreshUI();
            staminaRefreshTimer = 0;
        }
    }
}
