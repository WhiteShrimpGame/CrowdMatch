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
    private void Start()
    {
    }

    public void InitSettingPanel()
    {
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

        var testRoot = transform.Find("BG/TestPanel");

        for (int i = 0; i < testPanel.transform.childCount - 1; i++)
        {
            var btn = testPanel.transform.GetChild(i + 1).GetComponent<Button>();
            var index = i;

            if (btn != null)
                btn.onClick.AddListener(() => { _OnTestBtnClk(index); });
        }

        _musicBtn.GetComponent<Button>().onClick.AddListener(OnMusicBtnClk);
        _soundBtn.GetComponent<Button>().onClick.AddListener(OnSoundBtnClk);
        _vibrateBtn.GetComponent<Button>().onClick.AddListener(OnVibrateBtnClk);

        _retryBtn.GetComponent<Button>().onClick.AddListener(OnRetryBtnClk);
        _backBtn.GetComponent<Button>().onClick.AddListener(OnBackBtnClk);
        _resumeBtn.GetComponent<Button>().onClick.AddListener(OnCloseBtnClk);
        
        ShowSettingState(); 

        if (isShowTestPanel)
        {
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
        if (UIManager.Instance.isMainPanelActive)
        {
            UIManager.Instance.settingPanel = null;
        }
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
            addHourText.text = "+1H " + GameData.AddHour;
        }
        addDay.text = GameData.AddDay.ToString();
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
        ////AudioManager.Instance.playClip(1);
        GameManager.Instance.TriggerVibrate(1);
        gameObject.SetActive(false);

        //GameManager.Instance.ReloadScene(false);
        //SceneManager.LoadScene("GameScene");
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
            testPanel.SetActive(!isForbidTestPanel);
            levelShowText.gameObject.SetActive(true);
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

    public void _OnTestBtnClk(int index)
    {
        ////AudioManager.Instance.playClip(1);
        GameManager.Instance.TriggerVibrate(1);

        switch (index)
        {
            case 0:
                PlayerPrefs.DeleteAll();
                GameData.ResetAll();
                /*HandBookData.ClearData();
                CollectionPanelData.ResetData();
                RaceManager.ClearData();*/
                StaminaSystemData.ResetData();
                GameManager.Instance.ReloadLevel();
                /*SkinData.ClearData();
                BeadPixelData.ClearData();*/
                break;
            /*case 1:
                if (GameData.IsNoAd == false)
                {
                    GameData.IsNoAd = true;
                    noAdTip.text = "开启广告";
                }
                else
                {
                    GameData.IsNoAd = false;
                    noAdTip.text = "关闭广告";
                }
                break;*/
            case 2:
                //Reporter.GameContinue();
                //GameData.RemovedBoxCount = GameData.TotalBoxCount;
                GameData.ClearedPixelCount = GameData.TotalPixelCount;
                gameObject.SetActive(false);
                GameController.Instance.CheckWin();
                //GameController.Instance.curLevel.HideExceptGift();
                /*GameManager.Instance.GameWin();
                UIManager.Instance.showWinPanel(true);*/
                break;
            case 3:
                //Reporter.GameContinue();
                GameManager.Instance.GameWin();
                GameManager.Instance.ReloadLevel();

                //SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                break;
            case 4:
                GameData.Gold.Add(100, "GM");
                var gp = UIManager.Instance.gameInnerUI;
                if (gp != null && gp.gameObject.activeSelf)
                {
                    gp.RefreshGoldCount();
                }

                /*var mp = UIManager.Instance.mainPanel;
                if (mp != null && mp.gameObject.activeSelf)
                {
                    mp.GetComponent<MainPanel>().RefreshGoldCount();
                }*/

                break;
            case 5:
                GameData.AddDay++;
                //OnlineRwardTimer.Instance.ReStartTimer();
                addDay.text = GameData.AddDay.ToString();
                break;
            case 6:
                gameObject.SetActive(false);
                UIManager.Instance.showFailPanel(true);
                break;
            /*case 7:
                ShowOnlineParams();
                break;
            case 8:
                GameData.LevelGroup++;
                if (GameData.LevelGroup >= GameManager.Instance.levelDatas.Length)
                {
                    GameData.LevelGroup = 0;
                }

                levelGroupText.text = "关卡组 " + GameData.LevelGroup;
                break;
            case 9:
                GameData.MaterialAB++;
                if (GameData.MaterialAB >= 2)
                {
                    GameData.MaterialAB = 0;
                }

                break;
            case 10:
                GameData.LevelDyn = 1 - GameData.LevelDyn;
                levelDynText.text = "难度模型 " + GameData.LevelDyn;
                break;
            case 11:
                if (GameInnerUI.hideUI)
                {
                    GameInnerUI.hideUI = false;
                    hideUITip.text = "隐藏UI";
                }
                else
                {
                    GameInnerUI.hideUI = true;
                    hideUITip.text = "显示UI";
                }

                break;
            case 12:
                if (GameData.TestTaskSystem)
                {
                    GameData.TestTaskSystem = false;
                    testTaskSystemText.text = "开启任务测试";
                }
                else
                {
                    GameData.TestTaskSystem = true;
                    testTaskSystemText.text = "关闭任务测试";
                }

                break;*/
            case 13:
                GameData.FailCount = 0;
                //GameData.RetryCount = 0;
                if (GameData.CurrentLevel > 1)
                {
                    GameData.CurrentLevel--;
                }

                GameManager.Instance.ReloadLevel();
                break;
            /*case 14:
                GameData.IsAllSystemOpen = true;
                break;
            case 15:
                if (GameData.LevelDynId == 3)
                {
                    GameData.LevelDynId = 9;
                }
                else if (GameData.LevelDynId == 13)
                {
                    GameData.LevelDynId = 20;
                }
                else if (GameData.LevelDynId == 23)
                {
                    GameData.LevelDynId = 30;
                }
                else if (GameData.LevelDynId == 32)
                {
                    GameData.LevelDynId = -1;
                }
                else
                {
                    GameData.LevelDynId++;
                }

                if (GameData.LevelDynId == -1)
                {
                    levelDynIdText.text = "难度ID";
                }
                else
                {
                    levelDynIdText.text = "难度ID " + GameData.LevelDynId;
                }

                break;
            case 16:
                if (GameData.MaxColorLow >= 4)
                {
                    GameData.MaxColorLow = 0;
                }
                else
                {
                    GameData.MaxColorLow++;
                }

                if (GameData.MaxColorLow == 0)
                {
                    maxColorText.text = "减颜色";
                }
                else
                {
                    maxColorText.text = "减颜色 " + GameData.MaxColorLow;
                }

                break;*/
            case 17:
                GameData.AddHour++;
                //OnlineRwardTimer.Instance.ReStartTimer();
                addHourText.text = "+1H " + GameData.AddHour;
                break;
            /*case 18:
                GameManager.Instance.isAutoPlay = !GameManager.Instance.isAutoPlay;
                if (GameManager.Instance.isAutoPlay)
                {
                    OnCloseBtnClk();
                }
                else
                {
                    autoTapeText.text = "自动撕";
                }
                break;
            case 19:
                GameManager.Instance.isDebugLevel = !GameManager.Instance.isDebugLevel;
                if (GameManager.Instance.isDebugLevel)
                {
                    debugTapeText.text = "取消随意";
                }
                else
                {
                    debugTapeText.text = "随意撕";
                }
                break;*/
            /*case 20:
                GameData.itemPlayerData.AddCount(ItemType.Add, 100, way: "GM", needReport: false);
                GameData.itemPlayerData.AddCount(ItemType.Remove, 100, way: "GM", needReport: false);
                GameData.itemPlayerData.AddCount(ItemType.Clear, 100, way: "GM", needReport: false);
                break;
            case 21:
                GameManager.Instance.isAutoGame = !GameManager.Instance.isAutoGame;
                if (GameManager.Instance.isAutoGame)
                {
                    OnCloseBtnClk();
                }
                else
                {
                    autoGameText.text = "自动过关";
                }

                break;
            case 22:
                HandBookData.Test();
                break;
            case 23:
                GameData.VibrateLevel++;
                if (GameData.VibrateLevel >= 3)
                {
                    GameData.VibrateLevel = 0;
                }

                vibrateLevelText.text = "振动等级 " + GameData.VibrateLevel;
                break;
            case 24:
                GameData.VibrateTick++;
                if (GameData.VibrateTick >= 6)
                {
                    GameData.VibrateTick = 0;
                }

                vibrateTickText.text = "振动间隔 " + GameData.VibrateTick;
                break;
            case 25:
                var item = BeadPixelData.IsBeadPixelTexAllCompleted();
                if (item is { Item1: false, Item2: false })
                {
                    int addCount = BeadPixelData.AddBeadCount();
                    Debug.LogError($"添加{addCount}个拼豆");
                }
                else
                {
                    Debug.LogError("所有拼豆图已完成");
                }

                break;*/
            case 26:
                StaminaSystemData.AddInfiniteStamina(600,"GM");
                break;
            /*case 27:
                if (GameData.LevelColorId >= 4)
                {
                    GameData.LevelColorId = -1;
                }
                else
                {
                    GameData.LevelColorId++;
                }

                if (GameData.LevelColorId == -1)
                {
                    levelColorText.text = "颜色模型";
                }
                else
                {
                    levelColorText.text = "颜色模型 " + GameData.LevelColorId;
                }

                break;*/
            default:
                break;
        }
    }

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