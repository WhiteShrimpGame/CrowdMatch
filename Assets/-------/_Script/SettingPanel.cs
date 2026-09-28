using DG.Tweening;
using DG.Tweening.Core.Easing;
//using PufferMiniGame;
using System.Collections;
using System.Collections.Generic;
using CrowdMatch;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingData
{
    public static int MusicSet
    {
        get { return PlayerPrefs.GetInt(GameData.MusicState, 1); }
        set { PlayerPrefs.SetInt(GameData.MusicState, value != 0 ? 1 : 0); }
    }

    public static int SoundSet
    {
        get { return PlayerPrefs.GetInt(GameData.SoundState, 1); }
        set { PlayerPrefs.SetInt(GameData.SoundState, value != 0 ? 1 : 0); }
    }

    public static int VibrateSet
    {
        get { return PlayerPrefs.GetInt(GameData.VibrateState, 1); }
        set
        {
            PlayerPrefs.SetInt(GameData.VibrateState, value != 0 ? 1 : 0);
        }
    }
}

public class SettingPanel : MonoBehaviour
{
#if UNITY_EDITOR || NoAds
    public static bool isShowTestPanel = true;
#else
    public static bool isShowTestPanel = false;
#endif

    public static bool isForbidTestPanel = false;
    Transform _musicBtn, _soundBtn, _vibrateBtn, _resumeBtn, _backBtn, _retryBtn;

    public InputField testInput, levelInput;
    public GameObject testPanel;
    public Text noAdTip;
    public Text hideUITip;
    public Text levelGroupText;
    public Text levelDynText;
    public Text levelDynIdText;
    public Text maxColorText;
    public Text levelColorText;
    public Text testTaskSystemText;
    public Text addHourText;
    public Text autoTapeText;
    public Text debugTapeText;
    public Text autoGameText;

    //public GameObject vibrateLevelBtn;
    //public GameObject vibrateTickBtn;
    public Text vibrateLevelText;
    public Text vibrateTickText;

    public Text addDay, addDayAfterClose;
    public Text onlineParams;
    public GameObject onlineParamsPanel;
    public int onlineParamsPageIndex;

    public Text versionText;
    public Text levelShowText;

    private bool _isDown = false;
    public Button paramsNext;
    public Button paramsLast;
    public Button homeBtn;
    public Button retryBtn;
    public Button GMBtn;

    // ========== SO配置 & 动态实例化相关 ==========
    [Header("GM按钮配置")]
    public TestBtnConfigSO testBtnConfigSO;
    [Tooltip("GM按钮预制体，放在TestPanel下，默认关闭")]
    public Button gmBtnPrefab;
    [Tooltip("GM按钮挂载父物体")]
    public Transform gmBtnRoot;

    private Dictionary<string, System.Action> _cmdDict;
    // 保存动态生成的按钮列表，用于销毁
    private List<Button> _spawnedGmBtns = new List<Button>();

    public void InitSettingPanel()
    {
        // 初始化指令字典，所有GM命令在这里注册
        InitCommandDict();

        /*paramsNext.onClick.AddListener(ShowOnlineParamsNext);
        paramsLast.onClick.AddListener(ShowOnlineParamsLast);*/
        _musicBtn = transform.Find("BG/MusicPart/MusicBtn");
        _soundBtn = transform.Find("BG/SoundPart/SoundBtn");
        _vibrateBtn = transform.Find("BG/VibratePart/VibrateBtn");

        _retryBtn = transform.Find("BG/BtnGroup/RetryBtn");
        _resumeBtn = transform.Find("BG/BtnGroup/ResumeBtn");
        _backBtn = transform.Find("BG/BtnGroup/BackBtn");

        transform.Find("BG/CloseBtn").GetComponent<Button>().onClick.AddListener(OnCloseBtnClk);

        var testBtn = transform.Find("BG/Title/Image").GetComponent<EventTrigger>();
        var testDownTrigger = new EventTrigger.Entry();
        testDownTrigger.eventID = EventTriggerType.PointerDown;
        testDownTrigger.callback.AddListener(_OnTestBtnDown);
        testBtn.triggers.Add(testDownTrigger);
        var testUpTrigger = new EventTrigger.Entry();
        testUpTrigger.eventID = EventTriggerType.PointerUp;
        testUpTrigger.callback.AddListener(_OnTestBtnUp);
        testBtn.triggers.Add(testUpTrigger);

        transform.Find("BG/InputField/OkBtn").GetComponent<Button>().onClick.AddListener(_OnCheckBtnClk);
        transform.Find("BG/TestPanel/JumpToLevel/GoBtn").GetComponent<Button>().onClick.AddListener(_OnGoBtnClk);

        // ===================== 【新版】销毁旧GM按钮 + 根据SO动态生成 =====================
        //DestroyAllSpawnedGmButtons();
        SpawnGmButtonsFromSO();
        // ================================================================================

        _musicBtn.GetComponent<Button>().onClick.AddListener(OnMusicBtnClk);
        _soundBtn.GetComponent<Button>().onClick.AddListener(OnSoundBtnClk);
        _vibrateBtn.GetComponent<Button>().onClick.AddListener(OnVibrateBtnClk);

        _retryBtn.GetComponent<Button>().onClick.AddListener(OnRetryBtnClk);
        retryBtn.onClick.AddListener(OnRetryBtnClk);
        _backBtn.GetComponent<Button>().onClick.AddListener(OnBackBtnClk);
        homeBtn.onClick.AddListener(OnBackBtnClk);
        _resumeBtn.GetComponent<Button>().onClick.AddListener(OnCloseBtnClk);

        ShowSettingState();

        if (isShowTestPanel)
        {
            /*GMBtn.gameObject.SetActive(true);
            GMBtn.onClick.AddListener(() =>
            {
                Debug.Log("GMBtn 被点击");
                testPanel.SetActive(!isForbidTestPanel);
                isForbidTestPanel=!isForbidTestPanel;
                GMBtn.transform.GetChild(0).GetComponent<Text>().text = "GM：" + (isForbidTestPanel ? "开" : "关");
            });*/
            testInput.gameObject.SetActive(false);
            testPanel.SetActive(!isForbidTestPanel);
            //levelShowText.gameObject.SetActive(true);
            //ShowLevelText();
        }
        else
        {
            testInput.gameObject.SetActive(false);
            testPanel.SetActive(false);
            //levelShowText.gameObject.SetActive(false);
        }

        /*vibrateLevelBtn.SetActive(true);
        vibrateTickBtn.SetActive(true);
#if ZMY
        if (versionText != null)
        {
            versionText.text = ZMYSDK.ZMYSDKManager.I.Sdk.GetVersionName();
        }
#endif
#if !ZMY || (!UNITY_IOS && !UNITY_ANDROID)
        vibrateLevelBtn.SetActive(false);
        vibrateTickBtn.SetActive(false);
#endif*/
    }

    /// <summary>
    /// 从SO配置生成GM按钮
    /// </summary>
    private void SpawnGmButtonsFromSO()
    {
        if (testBtnConfigSO == null || testBtnConfigSO.items == null || gmBtnPrefab == null || gmBtnRoot == null)
        {
            Debug.LogWarning("GM按钮配置缺失，无法生成GM按钮！检查testBtnConfigSO、gmBtnPrefab、gmBtnRoot");
            return;
        }

        var configList = testBtnConfigSO.items;
        int max = configList.Count;
        for (int i = 0, j = 0; i < max; i++)
        {
            if (i > 7 && i< 24)
            {
                // 创建空物体，父物体设置为 gmBtnRoot
                GameObject spaceObj = new GameObject("SpaceItem");
                spaceObj.transform.SetParent(gmBtnRoot, false); 
                // false = worldPositionStays:false，UI常用，保持本地坐标，避免RectTransform位置错乱

                // 必须添加RectTransform！UGUI子物体必须有这个组件
                spaceObj.AddComponent<RectTransform>();
                max++;
                continue;
            }
            var btnItem = configList[j];
            // 实例化按钮
            Button btn = Instantiate(gmBtnPrefab, gmBtnRoot);
            _spawnedGmBtns.Add(btn);
            btn.gameObject.SetActive(true);

            string cmd = btnItem.cmdKey;
            if (_cmdDict.TryGetValue(cmd, out var action))
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    GameManager.Instance.TriggerVibrate(1);
                    action?.Invoke();
                });
                var uiText = btn.GetComponentInChildren<Text>();
                if (uiText != null) 
                { 
                    if (btnItem.cmdKey=="AddDay")
                    {
                        addDay = uiText;
                        //uiText.text = btnItem.btnName + "_"+GameData.AddDay;
                    }
                    else if (btnItem.cmdKey == "AddHour")
                    {
                        addHourText= uiText;
                        //uiText.text = btnItem.btnName + "_"+GameData.AddHour;
                    }
                    uiText.text = btnItem.btnName;
                    
                }
            }
            else
            {
                Debug.LogError($"找不到测试指令 cmdKey={cmd}");
            }

            j++;
        }
    }

    /// <summary>
    /// 销毁之前动态生成的所有GM按钮
    /// </summary>
    private void DestroyAllSpawnedGmButtons()
    {
        foreach (var btn in _spawnedGmBtns)
        {
            if (btn != null)
            {
                Destroy(btn.gameObject);
            }
        }
        _spawnedGmBtns.Clear();
    }

    // 初始化GM指令映射字典，所有GM功能注册在这里
    private void InitCommandDict()
    {
        _cmdDict = new Dictionary<string, System.Action>()
        {
            {
                "ClearAllData", ()=>
                {
                    PlayerPrefs.DeleteAll();
                    GameData.ResetAll();
                    StaminaSystemData.ResetData();
                    GameManager.Instance.ReloadLevel();
                }
            },
            {
                "WinCurrent", ()=>
                {
                    GameData.ClearedPixelCount = GameData.TotalPixelCount;
                    gameObject.SetActive(false);
                    GameController.Instance.ForceWin();
                }
            },
            {
                "GameWinReload", ()=>
                {
                    GameManager.Instance.GameWin();
                    GameManager.Instance.ReloadLevel();
                }
            },
            {
                "AddGold100", ()=>
                {
                    GameData.Gold.Add(100, "GM");
                    var gp = UIManager.Instance.gameInnerUI;
                    if (gp != null && gp.gameObject.activeSelf)
                    {
                        gp.RefreshGoldCount();
                    }
                }
            },
            {
                "AddDay", ()=>
                {
                    GameData.AddDay++;
                    addDay.text =  "+1天:"+ GameData.AddDay;
                }
            },
            {
                "ShowFailPanel", ()=>
                {
                    gameObject.SetActive(false);
                    UIManager.Instance.showFailPanel(true);
                }
            },
            {
                "PreLevel", ()=>
                {
                    GameData.FailCount = 0;
                    if (GameData.CurrentLevel > 1)
                    {
                        GameData.CurrentLevel--;
                    }
                    GameManager.Instance.ReloadLevel();
                }
            },
            {
                "AddHour", ()=>
                {
                    GameData.AddHour++;
                    addHourText.text = "+1H:" + GameData.AddHour;
                }
            },
            {
                "InfiniteStamina", ()=>
                {
                    StaminaSystemData.AddInfiniteStamina(600,"GM");
                }
            }
        };
    }

    private void OnEnable()
    {
        //ShowSettingState();
        //UIManager.Instance.HideBanner();

        // if (isShowTestPanel)
        // {
        //     testInput.gameObject.SetActive(false);
        //     testPanel.SetActive(!isForbidTestPanel);
        // }
        // else
        // {
        //     testInput.gameObject.SetActive(false);
        //     testPanel.SetActive(false);
        // }
    }

    private void OnDisable()
    {
        // 销毁动态生成按钮，防止残留
        //DestroyAllSpawnedGmButtons();

        if (UIManager.Instance.isMainPanelActive)
        {
            UIManager.Instance.settingPanel = null;
        }

        //isForbidTestPanel = false;
        Destroy(gameObject);
    }

    public void ShowSettingState()
    {
        if (!_musicBtn)
            _musicBtn = transform.Find("BG/MusicPart/MusicBtn");

        if (!_soundBtn)
            _soundBtn = transform.Find("BG/SoundPart/SoundBtn");

        if (!_vibrateBtn)
            _vibrateBtn = transform.Find("BG/VibratePart/VibrateBtn");

        if (!_retryBtn)
            _retryBtn = transform.Find("BG/BtnGroup/RetryBtn");

        if (!_resumeBtn)
            _resumeBtn = transform.Find("BG/BtnGroup/ResumeBtn");

        if (!_backBtn)
            _backBtn = transform.Find("BG/BtnGroup/BackBtn");

        if (SettingData.MusicSet == 1)
        {
            _musicBtn.Find("Open").gameObject.SetActive(true);
            _musicBtn.Find("Close").gameObject.SetActive(false);

            _musicBtn.GetComponent<Button>().targetGraphic = _musicBtn.Find("Open").GetComponent<Image>();
        }
        else
        {
            _musicBtn.Find("Open").gameObject.SetActive(false);
            _musicBtn.Find("Close").gameObject.SetActive(true);

            _musicBtn.GetComponent<Button>().targetGraphic = _musicBtn.Find("Close").GetComponent<Image>();
        }

        if (SettingData.SoundSet == 1)
        {
            _soundBtn.Find("Open").gameObject.SetActive(true);
            _soundBtn.Find("Close").gameObject.SetActive(false);

            _soundBtn.GetComponent<Button>().targetGraphic = _soundBtn.Find("Open").GetComponent<Image>();
        }
        else
        {
            _soundBtn.Find("Open").gameObject.SetActive(false);
            _soundBtn.Find("Close").gameObject.SetActive(true);

            _soundBtn.GetComponent<Button>().targetGraphic = _soundBtn.Find("Close").GetComponent<Image>();
        }

        if (SettingData.VibrateSet == 1)
        {
            _vibrateBtn.Find("Open").gameObject.SetActive(true);
            _vibrateBtn.Find("Close").gameObject.SetActive(false);

            _vibrateBtn.GetComponent<Button>().targetGraphic = _vibrateBtn.Find("Open").GetComponent<Image>();
        }
        else
        {
            _vibrateBtn.Find("Open").gameObject.SetActive(false);
            _vibrateBtn.Find("Close").gameObject.SetActive(true);

            _vibrateBtn.GetComponent<Button>().targetGraphic = _vibrateBtn.Find("Close").GetComponent<Image>();
        }
        if (GameData.AddHour == 0)
        {
            addHourText.text = "+1H";
        }
        else
        {
            addHourText.text = "+1H:" + GameData.AddHour;
        }
        if (GameData.AddDay == 0)
        {
            addDay.text = "+1天";
        }
        else
        {
            addDay.text = "+1天:" + GameData.AddDay;
        }
        // if (UIManager.Instance.isMainPanelActive)
        // {
        //     _resumeBtn.gameObject.SetActive(false);
        //     _backBtn.gameObject.SetActive(false);
        //     _retryBtn.gameObject.SetActive(false);
        // }
        // else
        // {
        //     _resumeBtn.gameObject.SetActive(true);
        //     _backBtn.gameObject.SetActive(true);
        //     _retryBtn.gameObject.SetActive(true);
        // }

        /*_resumeBtn.gameObject.SetActive(false);
        _backBtn.gameObject.SetActive(false);
        _retryBtn.gameObject.SetActive(false);

        if (GameData.IsNoAd)
        {
            noAdTip.text = "开启广告";
        }
        else
        {
            noAdTip.text = "关闭广告";
        }

        if (GameInnerUI.hideUI)
        {
            hideUITip.text = "显示UI";
        }
        else
        {
            hideUITip.text = "隐藏UI";
        }

        addDayAfterClose.text = GameData.AddDayAfterClose.ToString();

        levelGroupText.text = "关卡组 " + GameData.LevelGroup;
        levelDynText.text = "难度模型 " + GameData.LevelDyn;
        if (GameData.LevelDynId == -1)
        {
            levelDynIdText.text = "难度ID";
        }
        else
        {
            levelDynIdText.text = "难度ID " + GameData.LevelDynId;
        }

        if (GameData.MaxColorLow == 0)
        {
            maxColorText.text = "减颜色";
        }
        else
        {
            maxColorText.text = "减颜色 " + GameData.MaxColorLow;
        }

        if (GameData.LevelColorId == -1)
        {
            levelColorText.text = "颜色模型";
        }
        else
        {
            levelColorText.text = "颜色模型 " + GameData.LevelColorId;
        }

        if (GameManager.Instance.isAutoPlay)
        {
            autoTapeText.text = "取消自动";
        }
        else
        {
            autoTapeText.text = "自动撕";
        }

        if (GameManager.Instance.isDebugLevel)
        {
            debugTapeText.text = "取消随意";
        }
        else
        {
            debugTapeText.text = "随意撕";
        }

        if (GameManager.Instance.isAutoGame)
        {
            autoGameText.text = "取消自动";
        }
        else
        {
            autoGameText.text = "自动过关";
        }

        vibrateLevelText.text = "振动等级 " + GameData.VibrateLevel;
        vibrateTickText.text = "振动间隔 " + GameData.VibrateTick;*/
    }

    public void OnCloseBtnClk()
    {
        if (!UIManager.Instance.isMainPanelActive)
        {
            if (GameState.IsGamePause)
            {
                GameState.GameStart();
                UIManager.Instance.ShowBanner();
            }
        }

        ////AudioManager.Instance.playClip(1);
        GameManager.Instance.TriggerVibrate(1);
        //UIManager.Instance.HidePanel(transform);
        UIManager.Instance.ShowSettingPanel(false);
    }

    public void OnBackBtnClk()
    {
        AudioManager.Instance.PlayButtonAudioAndVibrate();
        GameManager.Instance.CleanupSpawnPool();
        //UIManager.Instance.Init();
        UIManager.Instance.showGamePanel(false);
        UIManager.Instance.ShowMenuPanel(true);
        UIManager.Instance.ShowSettingPanel(false);
    }

    public void OnRetryBtnClk()
    {
        ////AudioManager.Instance.playClip(1);
        GameManager.Instance.TriggerVibrate(1);

        GameManager.Instance.ReloadLevel();
        //SceneManager.LoadScene("GameScene");
    }

    public void OnMusicBtnClk()
    {
        ////AudioManager.Instance.playClip(1);
        GameManager.Instance.TriggerVibrate(1);
        if (SettingData.MusicSet == 1)
        {
            //AudioManager.Instance.stopClip(0);
            AudioManager.Instance.Stop("BGM");
            SettingData.MusicSet = 0;
            _musicBtn.Find("Open").gameObject.SetActive(false);
            _musicBtn.Find("Close").gameObject.SetActive(true);

            _musicBtn.GetComponent<Button>().targetGraphic = _musicBtn.Find("Close").GetComponent<Image>();
        }
        else
        {
            SettingData.MusicSet = 1;
            //AudioManager.Instance.playClip(0, 1, true);
            AudioManager.Instance.Play("BGM", 1, true);
            _musicBtn.Find("Open").gameObject.SetActive(true);
            _musicBtn.Find("Close").gameObject.SetActive(false);

            _musicBtn.GetComponent<Button>().targetGraphic = _musicBtn.Find("Open").GetComponent<Image>();
        }
    }

    public void OnSoundBtnClk()
    {
        GameManager.Instance.TriggerVibrate(1);
        if (SettingData.SoundSet == 1)
        {
            SettingData.SoundSet = 0;
            _soundBtn.Find("Open").gameObject.SetActive(false);
            _soundBtn.Find("Close").gameObject.SetActive(true);

            _soundBtn.GetComponent<Button>().targetGraphic = _soundBtn.Find("Close").GetComponent<Image>();
        }
        else
        {
            SettingData.SoundSet = 1;
            ////AudioManager.Instance.playClip(1);
            _soundBtn.Find("Open").gameObject.SetActive(true);
            _soundBtn.Find("Close").gameObject.SetActive(false);

            _soundBtn.GetComponent<Button>().targetGraphic = _soundBtn.Find("Open").GetComponent<Image>();
        }
    }

    public void OnVibrateBtnClk()
    {
        ////AudioManager.Instance.playClip(1);
        if (SettingData.VibrateSet == 1)
        {
            SettingData.VibrateSet = 0;
            _vibrateBtn.Find("Open").gameObject.SetActive(false);
            _vibrateBtn.Find("Close").gameObject.SetActive(true);

            _vibrateBtn.GetComponent<Button>().targetGraphic = _vibrateBtn.Find("Close").GetComponent<Image>();
        }
        else
        {
            SettingData.VibrateSet = 1;
            GameManager.Instance.TriggerVibrate(1);
            _vibrateBtn.Find("Open").gameObject.SetActive(true);
            _vibrateBtn.Find("Close").gameObject.SetActive(false);

            _vibrateBtn.GetComponent<Button>().targetGraphic = _vibrateBtn.Find("Open").GetComponent<Image>();
        }
    }

    public void _OnTestBtnDown(BaseEventData eventData)
    {
        Debug.Log("按下");
        _isDown = true;
    }

    public void _OnTestBtnUp(BaseEventData eventData)
    {
        Debug.Log("抬起");
        _isDown = false;
    }

    float _timer = 0;
    private void Update()
    {
        if (_isDown)
        {
            _timer += Time.deltaTime;
            if (_timer >= 5f)
            {
                _timer = 0;
                _isDown = false;

                testInput.gameObject.SetActive(true);
            }
        }
    }

    public void _OnCheckBtnClk()
    {
        if (testInput.text == "0922519")
        {
            testInput.gameObject.SetActive(false);
            isShowTestPanel = true;
            /*GMBtn.gameObject.SetActive(true);
            GMBtn.onClick.AddListener(() =>
            {
                Debug.Log("GMBtn 被点击");
                testPanel.SetActive(!isForbidTestPanel);
                isForbidTestPanel=!isForbidTestPanel;
                GMBtn.transform.GetChild(0).GetComponent<Text>().text = "GM：" + (isForbidTestPanel ? "开" : "关");
            });*/
            testPanel.SetActive(!isForbidTestPanel);
            //levelShowText.gameObject.SetActive(true);
            //ShowLevelText();
        }
    }

    public void _OnGoBtnClk()
    {
        int level = int.Parse(levelInput.text);
        if (level > 0)
        {
            /*GameData.FailCount = 0;
            GameData.RetryCount = 0;*/
            GameData.CurrentLevel = level;
            //GameManager.Instance.ReloadScene();
            GameManager.Instance.ReloadLevel();
            //SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    /* ======== 已移除旧的 _OnTestBtnClk 方法 ======== */

    /*private void ShowOnlineParamsLast()
    {
        onlineParamsPageIndex--;
        if (onlineParamsPageIndex < 0)
        {
            onlineParamsPageIndex = 4;
        }

        ShowParams();
    }
    private void ShowOnlineParamsNext()
    {
        onlineParamsPageIndex++;
        if (onlineParamsPageIndex > 4)
        {
            onlineParamsPageIndex = 0;
        }

        ShowParams();
    }
    private void ShowParams()
    {
        /*string[] ops;
        switch (onlineParamsPageIndex)
        {
            case 0: ops = OnlineParams; break;
            case 1: ops = OnlineParamsP2; break;
            case 2: ops = OnlineParamsP3; break;
            case 3: ops = OnlineParamsP4; break;
            case 4: ops = OnlineParamsP5; break;
            default: ops = OnlineParams; break;
        }
        //ops = OnlineParams;
        string res = "";
        for (int i = 0; i < ops.Length; i++)
        {
            res += ops[i];
            res += "    ";
            res += MiniGameSolution.Utilities.GetAbTestKeyValueSync(ops[i]);
            res += "\n";
        }
        onlineParams.text = res;#1#
        string[] ops = onlineParamsConfig.GetParamKeysByPage(onlineParamsPageIndex);
        string res = "";

        for (int i = 0; i < ops.Length; i++)
        {
            string key = ops[i];
            string value = MiniGameSolution.Utilities.GetAbTestKeyValueSync(key);
            res += $"{key}    {value}\n";
        }

        onlineParams.text = res;
    }
    private void ShowOnlineParams()
    {
        onlineParamsPageIndex = 0;
        //onlineParams.gameObject.SetActive(true);
        onlineParamsPanel.SetActive(true);
        ShowParams();
        /*string[] ops;
        if (onlineParamsPageIndex == 0 || onlineParamsPageIndex == 5)
        {
            onlineParamsPageIndex = 1;
            ops = OnlineParams;
        }
        else if (onlineParamsPageIndex == 1)
        {
            onlineParamsPageIndex = 2;
            ops = OnlineParamsP2;
        }
        else if (onlineParamsPageIndex == 2)
        {
            onlineParamsPageIndex = 3;
            ops = OnlineParamsP3;
        }
        else if (onlineParamsPageIndex == 3)
        {
            onlineParamsPageIndex = 4;
            ops = OnlineParamsP4;
        }
        else
        {
            onlineParamsPageIndex = 5;
            ops = OnlineParamsP5;
        }

        string res = "";

        for (int i = 0; i < ops.Length; i++)
        {
            res += ops[i];
            res += "    ";
            res += MiniGameSolution.Utilities.GetAbTestKeyValueSync(ops[i]);
            res += "\n";
        }

        onlineParams.text = res;#1#
    }*/

    /*private void ShowLevelText()
    {
        var curLevel = GameController.Instance.curLevel;
        if (curLevel == null)
        {
            levelShowText.text = "";
        }
        else
        {
            levelShowText.text = curLevel.name + " DynId: " +
                GameManager.Instance.dynamicConfig.ConfigToId(curLevel.dynamicConfig, curLevel.dynamicUltraConfig) +
                " Judge: " + StratifPlayerData.JudgeLevel +
                " Win: " + StratifPlayerData.WinCount + " Fail: " + StratifPlayerData.FailCount;
        }
    }*/
}
