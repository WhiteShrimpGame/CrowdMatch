using CrowdMatch;
using UnityEngine;
using UnityEngine.UI;

public class MainPanel : MonoBehaviour
{

    private Button startBtn;
    private Text goldCountText;
    private bool showHandBookTipsPanel;

    // ===================== 体力系统UI新增 =====================
    [Header("体力系统UI")]
    [SerializeField] private GameObject staminaPanel;          // 体力总面板
    [SerializeField] private Text staminaCountText;             // 体力数量文本 (当前/上限)
    [SerializeField] private Text staminaCountdownText;         // 体力恢复倒计时文本
    [SerializeField] private GameObject staminaAddBtn;              // 体力+号按钮
    [SerializeField] private Image infiniteStaminaIcon;        // 无限体力图标
    [SerializeField] private Text infiniteCountdownText;       // 无限体力倒计时文本

    // 体力系统私有变量
    private float staminaRefreshTimer;                          // 体力刷新计时器(每秒刷新)
    private bool isInfiniteStamina;                             // 是否激活无限体力
    private int infiniteStaminaEndTime;                        // 无限体力结束时间戳
    private void OnEnable()
    {
        UIManager.IsPanelShow = true;

        if (GameData.LevelDiff == 1)
        {
            startBtn = transform.Find("StartBtnHard").GetComponent<Button>();
        }
        else if (GameData.LevelDiff == 2)
        {
            startBtn = transform.Find("StartBtnSuperHard").GetComponent<Button>();
        }
        else
        {
            startBtn = transform.Find("StartBtn").GetComponent<Button>();
        }

        startBtn.gameObject.SetActive(true);
        startBtn.onClick.AddListener(_OnStartBtnClk);
        var levelNum = startBtn.transform.Find("LevelText").GetComponent<Text>();
        levelNum.text = "第 " + GameData.CurrentLevel + " 关";

        //if (GameData.IsWinStreakActive && GameData.WinStreak > 0)
        if (GameData.WinStreak > 0)
        {
            var streak = transform.Find("WinStreak");
            streak.gameObject.SetActive(true);
            streak.Find("StreakCount").GetComponent<Text>().text =
                Mathf.Min(GameData.WinStreak, 5).ToString();
        }
        goldCountText = transform.Find("GoldFrame/GoldCount").GetComponent<Text>();
        transform.Find("SettingButton").GetComponent<Button>().onClick.AddListener(_OnSettingBtnClk);
        RefreshGoldCount();
        if (StaminaSystemData.IsActive())
        {
            InitStaminaUI();
        }
    }
    
    private void Update()
    {
        if (StaminaSystemData.IsActive())
        {
            staminaRefreshTimer += Time.deltaTime;
            if (staminaRefreshTimer >= 1f)
            {
                RefreshStaminaUI();
                staminaRefreshTimer = 0;
            }
        }
    }
    

    private void OnDisable()
    {
        transform.Find("SettingButton").GetComponent<Button>().onClick.RemoveAllListeners();
        startBtn.onClick.RemoveAllListeners();
        Destroy(gameObject);
    }

    public void _OnSettingBtnClk()
    {
        AudioManager.Instance.PlayButtonAudioAndVibrate();
        UIManager.Instance.ShowSettingPanel(true);
    }

    public void _OnStartBtnClk()
    {
        UIManager.IsPanelShow = false;
        AudioManager.Instance.PlayButtonAudioAndVibrate();
        //if (StaminaSystemData.Config.StaminaSwitch==1&&!StaminaSystemData.HasEnoughStamina(1))
        if (StaminaSystemData.IsActive()&&!StaminaSystemData.HasEnoughStamina(1))
        {
            UIManager.Instance.ShowStaminaPanel(true); // 弹补充体力弹窗
            return;
        }

        GameManager.Instance.ReloadLevel();
        UIManager.Instance.showMainPanel(false);
        
    }
    
    public void RefreshGoldCount()
    {
        goldCountText.text = ((float)GameData.Gold.Count).ConvertToKMGString();
    }

#region 体力
    /// <summary>
    /// 初始化体力UI
    /// </summary>
    private void InitStaminaUI()
    {
        // 体力功能开启后始终显示
        staminaPanel.SetActive(true);

        // 绑定点击事件：体力框整体点击
        staminaPanel.GetComponent<Button>().onClick.AddListener(OnStaminaPanelClick);

        // 首次刷新体力UI
        RefreshStaminaUI();
    }

    /// <summary>
    /// 刷新体力UI（每秒刷新）
    /// </summary>
    private void RefreshStaminaUI()
    {
        if (staminaPanel == null || !StaminaSystemData._isInit) return;

        // 无限体力显示
        if (StaminaSystemData.IsInfiniteStamina)
        {
            staminaCountText.text = "";
            infiniteStaminaIcon.gameObject.SetActive(true);
            int sec = StaminaSystemData.InfiniteRemainTime;

            // 关卡无限体力
            if (GameManager.Instance.staminaConfig.UnlimitedStaminaLevel >= GameData.CurrentLevel)
            {
                infiniteCountdownText.gameObject.SetActive(true);
                infiniteCountdownText.text = "无限";
            }
            else
            {
                // 时长型无限体力，时间到了自动关闭显示
                if (sec > 0)
                {
                    infiniteCountdownText.gameObject.SetActive(true);
                    infiniteCountdownText.text = $"{sec / 60:D2}:{sec % 60:D2}";
                }
                else
                {
                    infiniteCountdownText.gameObject.SetActive(false);
                }
            }
            staminaCountdownText.text = "";
            return;
        }

        // 正常体力显示
        int cur = StaminaSystemData.GetCurrentStamina();
        int max = StaminaSystemData.MaxStamina;
        staminaCountText.text = $"{cur}";
        infiniteStaminaIcon.gameObject.SetActive(false);
        infiniteCountdownText.gameObject.SetActive(false);

        // 满体力 → 显示已满
        if (cur >= max)
        {
            staminaCountdownText.text = "已满";
            return;
        }

        // 倒计时显示
        double time = StaminaSystemTimer.Instance.GetRemainRecoverTime();
        int m = Mathf.FloorToInt((float)time / 60);
        int s = Mathf.FloorToInt((float)time % 60);
        staminaCountdownText.text = $"{m:D2}:{s:D2}";
    }

    /// <summary>
    /// 体力面板点击事件（弹出补充体力弹窗）
    /// </summary>
    private void OnStaminaPanelClick()
    {
        AudioManager.Instance.PlayButtonAudioAndVibrate();
        ShowStaminaSupplyPopup();
    }

    /// <summary>
    /// 显示补充体力弹窗
    /// </summary>
    private void ShowStaminaSupplyPopup()
    {
        Debug.Log("体力弹窗");
        if (StaminaSystemData.IsInfiniteStamina)
        {
            UIManager.Instance.ShowUnlimitedStaminaPanel(true);
        }
        else
        {
            UIManager.Instance.ShowStaminaPanel(true);
        }
    }

    /// <summary>
    /// 激活无限体力（外部调用，如道具生效）
    /// </summary>
    /// <param name="durationSeconds">持续时间(秒)</param>
    public void ActiveInfiniteStamina(int durationSeconds)
    {
        // 真实生效：调用数据层无限体力
        StaminaSystemData.AddInfiniteStamina(durationSeconds, "GM");
        // 立即刷新UI显示
        RefreshStaminaUI();
    }
    #endregion
}