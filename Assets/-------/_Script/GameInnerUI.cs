using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using CrowdMatch;
using UnityEngine;
using UnityEngine.UI;
using WsGame.HooksJam;

public class GameInnerUI : MonoBehaviour
{
    [Header("UIReference")] 
    [SerializeField] Button setttingButton;
    [SerializeField] Button homeButton;
    [SerializeField] Text levelText;
    [SerializeField] Text goldCountText;
    [SerializeField] Button refreshBtn;
    [SerializeField] Button removeBtn;
    [SerializeField] Button magnetBtn;
    [SerializeField] Button removeAimTapeCloseBtn;

    [Header("道具特效")]
    [Tooltip("道具2「磁铁」的特效预制体（Assets/_Prefabs/UI/MagnetPropEffect.prefab）。留空则不放特效，直接吸人")]
    [SerializeField] MagnetPropEffect magnetPropEffectPrefab;

    [Tooltip("磁铁特效播放多久之后才真正吸人（秒）")]
    [SerializeField] float magnetEffectDelay = 0.6f;

    [Tooltip("磁铁特效出现多久之后收掉（秒）")]
    [SerializeField] float magnetEffectHideDelay = 1f;

    public GameObject removeTip;



    [Header("GameDataConfig")] [SerializeField]
    float levelMaxScale = 1.5f;

    [SerializeField] float levelMinScale = 0.5f;
    Vector3 levelScale;
    float levelScaleMulti = 1;

    [Header("HardLevel")] 
    [SerializeField] Image hardTipBgImg;
    [SerializeField] Sprite hardTipBgHard;
    [SerializeField] Sprite hardTipBgSuperHard;

    [SerializeField] private Transform levelBgHardImg, levelBgSuperHardImg;
    [SerializeField] private Transform hardTip;
    [SerializeField] private Text hardTipText,hardTipHardText, hardTipSuperHardText;
    [SerializeField] private Transform hardTipHardImg, hardTipSuperHardImg;
    [SerializeField] private Image alertImg;
    [SerializeField] private Image arrowTipsImg;

    private Vector3 hardImgPos, superHardImgPos;
    private Vector3 hardImgScale, superHardImgScale;

    private bool isHardTipInit;
    private Sprite currentLevelModelSpr;
    private Sprite currentLevelModelLockSpr;

    public static GameObject obj;
    public Sprite unLuckSprite;
    public Sprite luckSprite;
    

    private void Awake()
    {
        refreshBtn?.onClick.AddListener(OnRefreshBtnClick);
        removeBtn?.onClick.AddListener(OnRemoveBtnClick);
        magnetBtn?.onClick.AddListener(OnMagnetBtnClick);
        //removeAimTapeCloseBtn?.onClick.AddListener(OnRemoveTapeCloseBtnClick);
        homeButton?.onClick.AddListener(OnBackBtnClk);

        UpdateCurrentButtonInfo();
        RefreshGoldCount();

        InitHardTip();
    }

    public void ResetLevel()
    {
        /*if (GameData.isNoCheckRemoveTape)
        {
            RestoreRemoveAimTapeButtonState();
        }*/

        alertImg.gameObject.SetActive(false);
        
        RefreshGoldCount();
        //UpdateCurrentButtonInfo();

        //StartCoroutine("Start");
        levelText.text = "第 " + GameData.CurrentLevel + " 关";
        ShowHardTip();
        Init();
    }

    /// <summary>
    /// 使用了去除胶带的逻辑，回复按钮状态
    /// </summary>
    public void RestoreRemoveAimTapeButtonState()
    {
        /*magnetBtn.interactable = true;
        removeAimTapeCloseBtn.Hide();

        GameData.isNoCheckRemoveTape = false;
        maskImage.gameObject.SetActive(false);
        removeTip.SetActive(false);

        GameController.Instance.curLevel.ReSortTapes();*/
    }

    /// <summary>
    /// 返回主界面
    /// </summary>
    public void OnBackBtnClk()
    {
        if (GameState.IsGameWin) return;
        AudioManager.Instance.PlayButtonAudioAndVibrate();
        if (GameData.ProgressPercent>30)
        {
            UIManager.Instance.showFailTipPanel(true);
        }
        else
        {
            GameManager.Instance.CleanupSpawnPool();
            //UIManager.Instance.Init();
            UIManager.Instance.showGamePanel(false);
            UIManager.Instance.ShowMenuPanel(true);
        }
        
        //UIManager.Instance.showMainPanel(true);
        /*{

            //WS_TapAway_Cloud.LevelRecord.ClearLevelRecord();
            
            GameManager.Instance.ReloadLevel();
            //SceneManager.LoadScene("GameScene");
        }*/
        
    }

    #region 道具按钮方法
    
    private void OnMagnetBtnClick()
    {
        if (GameState.IsGameWin) return;
        if (obj != null)
        {
            //obj.GetComponent<GuideMaskPanel>().Hide();
            GameState.GameStart();
            //Reporter.GameStart();
        }

        AudioManager.Instance.PlayButtonAudioAndVibrate();
        //HideFoolProofPropTips();
        int UnlockLvl = GameManager.Instance.itemData.GetUnlockLvl(ItemType.Magnet);
        if (UnlockLvl > GameData.CurrentLevel)
        {
            UIManager.Instance.ShowTip("第 "+UnlockLvl + "关解锁" );
            return;
        }
        if (!GameState.IsGameStart)
        {
            return;
        }

        /*if (GameData.isNoCheckRemoveTape)
            return;*/

        //var propId = "Magnet";
        ItemType itemType = ItemType.Magnet;
        if (!CanUseProp(itemType))
        {
            var findPropInfo = GameManager.Instance.GetPropInfo(itemType);
            if (GoldConfig.IsItemOutOfUse(findPropInfo.propType))
            {
                UIManager.Instance.ShowTip("道具已用完");
            }
            else
            {
                UIManager.Instance.ShowPropGetTip(true, findPropInfo, (type, count) =>
                {
                    GetAimProp(itemType, type, count);
                    UpdateCurrentButtonInfo();
                    RefreshGoldCount();
                });
            }
        }
        else
        {
            /*if (!GameController.Instance.CheckCanRemoveTape())
            {
                UIManager.Instance.ShowTip("场景中没有胶带！");
                return;
            }*/
            Debug.Log("使用了道具2磁铁");
            // 表现：先在第一排车中间的正上方生成磁铁 UI 特效，**0.6s 之后**才真正吸人
            //（被吸的人**原地消失、直接在车上落点出现**，见 GameController.MagnetClearFrontRow）。
            ShowMagnetPropEffect();
            var gc = GameController.Instance;
            DOVirtual.DelayedCall(magnetEffectDelay, () =>
            {
                if (gc != null)
                    gc.MagnetClearFrontRow();
            });
            CousmeProp(itemType);
            UpdateCurrentButtonInfo();
            //LogicOnMagnetBtnClick();
            //WS_TapAway_Cloud.LevelRecord.SaveLevelRecord();
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// 道具2「磁铁」的道具特效：在第一排车队的**正中间偏上 100** 处生成磁铁 UI 并播放。
    ///
    /// 位置口径：把"第一排车中间"的**世界坐标**用 <see cref="WSGameTools.WorldPosToUgui"/> 换成 UGUI 锚点坐标
    /// （以屏幕中心为原点），再加 100 —— 与 <c>MagnetPropEffect.Show</c> 收的 target 是同一套坐标。
    ///
    /// **注意 MagnetPropEffect 的 Start() 里原来有一句占位的 Show(...)，会覆盖这里的调用**，
    /// 已删掉（见该脚本）。
    ///
    /// 朝向是**固定值** `(0, 0, -120)`：脚本那边不再叠加预制体上的偏移量，传进去多少就是多少。
    /// </summary>
    private void ShowMagnetPropEffect()
    {
        if (magnetPropEffectPrefab == null)
            return;

        var gc = GameController.Instance;
        var cam = Camera.main;
        if (gc == null || cam == null || UIManager.Instance == null)
            return;

        var effect = Instantiate(magnetPropEffectPrefab, UIManager.Instance.transform);
        if (effect == null)
            return;

        Vector3 center = gc.FirstRowCenter();
        Vector2 uiPos = WSGameTools.WorldPosToUgui(center, cam);
        effect.Show(uiPos + new Vector2(-100f, 100f), -120f);

        // 出现 magnetEffectHideDelay 秒后收掉（此时人早飞完了）
        DOVirtual.DelayedCall(magnetEffectHideDelay, () =>
        {
            if (effect != null)
                effect.Hide();
        });
    }

    /*private void LogicOnMagnetBtnClick()
    {
        GameData.isNoCheckRemoveTape = true;
        maskImage.gameObject.SetActive(true);
        magnetBtn.interactable = false;
        removeTip.SetActive(true);
        magnetBtn.transform.Find("CloseBtn").Show();

        GameController.Instance.curLevel.MakeAllTapeBright();
    }*/

    /// <summary>
    /// 点击移除胶带按钮上的关闭按钮
    /// </summary>
    private void OnRemoveTapeCloseBtnClick()
    {
        RestoreRemoveAimTapeButtonState();
    }

    /// <summary>
    /// 消耗移除胶带道具次数
    /// </summary>
    /*public void ConsumeRemoveTapeProp()
    {
        var propId = "RemoveAimTape";
        CousmeProp(propId);
        UpdateCurrentButtonInfo();
        PlayerPrefs.Save();
    }*/

    //按钮触发方法：清空槽位
    private void OnRemoveBtnClick()
    {
        if (GameState.IsGameWin) return;
        if (obj != null)
        {
            //obj.GetComponent<GuideMaskPanel>().Hide();
            GameState.GameStart();
            //Reporter.GameStart();
        }

        AudioManager.Instance.PlayButtonAudioAndVibrate();
        //HideFoolProofPropTips();
        int UnlockLvl = GameManager.Instance.itemData.GetUnlockLvl(ItemType.Remove);
        if (UnlockLvl > GameData.CurrentLevel)
        {
            UIManager.Instance.ShowTip("第 "+UnlockLvl + "关解锁" );
            return;
        }
        if (!GameState.IsGameStart)
        {
            return;
        }

        //var propId = "Remove";
        ItemType itemType = ItemType.Remove;
        if (!CanUseProp(itemType))
        {
            var findPropInfo = GameManager.Instance.GetPropInfo(itemType);
            if (GoldConfig.IsItemOutOfUse(findPropInfo.propType))
            {
                UIManager.Instance.ShowTip("道具已用完");
            }
            else
            {
                UIManager.Instance.ShowPropGetTip(true, findPropInfo, (type, count) =>
                {
                    GetAimProp(itemType, type, count);
                    UpdateCurrentButtonInfo();
                    RefreshGoldCount();
                });
            }
        }
        else
        {
            /*if (!GameController.Instance.CheckCanClearWaitList())
            {
                UIManager.Instance.ShowTip("等待区无胶带可清除");
                return;
            }*/
            var gc = GameController.Instance;
            if (gc == null)
                return;

            // UFO 演出还没走完：先别进新的一次（否则上一组还在搬运，状态会乱）
            if (gc.Prop3Playing)
                return;

            // 再点一次 = 取消模式（不扣道具）
            if (gc.PropForceMode)
            {
                gc.ExitPropForceMode(false);
                return;
            }

            // 进入「强制取出」模式：所有人发光，点任意一组都能无视前排连通送出。
            // 道具在成功送出一组之后才扣（见 ConsumeRemovePropForce）。
            Debug.Log("使用了道具3移除：进入强制取出模式");
            gc.EnterPropForceMode();
        }
    }

    /*private void LogicClearWaitAllItems()
    {
        GameController.Instance.waitAlert.HideRed();
        GameController.Instance.waitAlert.HideSign();
        GameController.Instance.ClearWaitAllItems();

        DOVirtual.DelayedCall(0.2f, () => { AudioManager.Instance.playClip(11); });
    }*/

    //按钮触发方法：增加槽位
    private void OnRefreshBtnClick()
    {
        if (GameState.IsGameWin) return;
        if (obj != null)
        {
            //obj.GetComponent<GuideMaskPanel>().Hide();
            GameState.GameStart();
            //Reporter.GameStart();
        }

        AudioManager.Instance.PlayButtonAudioAndVibrate();
        //HideFoolProofPropTips();
        int UnlockLvl = GameManager.Instance.itemData.GetUnlockLvl(ItemType.Refresh);
        if (UnlockLvl > GameData.CurrentLevel)
        {
            UIManager.Instance.ShowTip("第 "+UnlockLvl + "关解锁" );
            return;
        }
        if (!GameState.IsGameStart)
        {
            return;
        }
        
        //var propId = "Refresh";
        ItemType itemType = ItemType.Refresh;
        if (!CanUseProp(itemType))
        {
            var findPropInfo = GameManager.Instance.GetPropInfo(itemType);
            /*if (GoldConfig.IsItemOutOfUse(findPropInfo.propType))
            {
                UIManager.Instance.ShowTip("道具已用完");
            }
            else*/
            {
                UIManager.Instance.ShowPropGetTip(true, findPropInfo, (type, count) =>
                {
                    GetAimProp(itemType, type, count);
                    UpdateCurrentButtonInfo();
                    RefreshGoldCount();
                });
            }
        }
        else
        {
            /*if (!GameController.Instance.CheckCanAddWaitList())
            {
                UIManager.Instance.ShowTip("槽位已满，无法添加");
                return;
            }*/
            Debug.Log("使用了道具1刷新");
            CousmeProp(itemType);
            UpdateCurrentButtonInfo();
            //LogicAddSlot();
            //WS_TapAway_Cloud.LevelRecord.SaveLevelRecord();
            PlayerPrefs.Save();
        }
    }

    /*private void LogicAddSlot()
    {
        GameController.Instance.UnlockWait();
    }*/

    /*private bool CanUseProp(string propId)
    {
        return  GameData.itemPlayerData.GetCount(PropInfo.PropToType(propId)) > 0;
    }*/
    private bool CanUseProp(ItemType type)
    {
        return  GameData.itemPlayerData.GetCount(type) > 0;
    }

    /*private void CousmeProp(string propId)
    {
        //GameData.ItemUseCount++;
        GameData.itemPlayerData.CostCount(PropInfo.PropToType(propId));
    }*/
    private void CousmeProp(ItemType type)
    {
        //GameData.ItemUseCount++;
        GameData.itemPlayerData.CostCount(type);
    }

    /// <summary>
    /// 道具3「强制取出」成功送出一组后的结算：扣 1 个道具并刷新显示。
    /// 由 GameController.ExitPropForceMode(consumed: true) 调用 —— 扣费在真正用掉那一刻，
    /// 而不是点道具按钮时（玩家进模式后取消不损失）。
    /// </summary>
    public void ConsumeRemovePropForce()
    {
        CousmeProp(ItemType.Remove);
        UpdateCurrentButtonInfo();
        RefreshGoldCount();
        PlayerPrefs.Save();
    }

    private void GetAimProp(ItemType itemType, int type, int count)
    {
        string way;
        string getType;
        switch (type)
        {
            case 0:
                way = "buy";
                getType = "0";
                break;
            case 1:
                way = "ad";
                getType = "1";
                break;
            default:
                way = "";
                getType = "";
                break;
        }

        GameData.itemPlayerData.AddCount(itemType, add: count, way: way, getType: getType);
    }


    public void ItemUIInit(ItemType type)
    {
        GameObject iconImage = null;
        GameObject plusImage = null;
        GameObject countGroup = null;
        Text unlockLevelText = null;
        GameObject lockImg = null;
        Button button = null;
        switch (type)
        {
            case ItemType.Refresh:
                button = refreshBtn;
                break;
            case ItemType.Magnet:
                button = magnetBtn;
                break;
            case ItemType.Remove:
                button = removeBtn;
                break;
        }
        iconImage = button.transform.LFirstOrDefault<Transform>("Icon",true).gameObject;
        plusImage = button.transform.LFirstOrDefault<Transform>("PlusIcon",true).gameObject;
        countGroup = button.transform.LFirstOrDefault<Transform>("CountGroup",true).gameObject;
        lockImg = button.transform.LFirstOrDefault<Transform>("suo",true).gameObject;
        unlockLevelText = button.transform.LFirstOrDefault<Transform>("Text",true).GetComponent<Text>();
        if (GameManager.Instance.itemData.GetUnlockLvl(type) > GameData.CurrentLevel)
        {
            button.transform.GetComponent<Image>().sprite = GameManager.Instance.itemData.itemLockBg;
            lockImg.SetActive(true);
            unlockLevelText.gameObject.SetActive(true);
            unlockLevelText.text = "第 " + GameManager.Instance.itemData.GetUnlockLvl(type) + " 关";
            iconImage.SetActive(false);
            plusImage.SetActive(false);
            countGroup.SetActive(false);
        }
        else
        {
            button.transform.GetComponent<Image>().sprite = GameManager.Instance.itemData.itemUnlockBg;
            lockImg.SetActive(false);
            unlockLevelText.gameObject.SetActive(false);
            iconImage.SetActive(true);
            plusImage.SetActive(true);
            countGroup.SetActive(true);
            /*if (GameData.itemPlayerData.GetCount(type) > 0)
            {
                Debug.Log(button.name);
                Debug.Log(GameData.itemPlayerData.GetCount(type));
                plusImage.SetActive(false);
                countGroup.SetActive(true);
            }
            else
            {
                plusImage.SetActive(true);
                countGroup.SetActive(false);
            }*/
        }
    }
    public void UpdateCurrentButtonInfo()
    {
        ItemUIInit(ItemType.Refresh);
        ItemUIInit(ItemType.Remove);
        ItemUIInit(ItemType.Magnet);

        /*if (refreshBtn == null)
        {
            return;
        }*/

        /*var propAddSlot = "AddSlot";
        var propClearWaitSlot = "ClearWaitSlot";
        var propRemoveAimTape = "RemoveAimTape";*/
        
        if (GameData.itemPlayerData.IsUnlock(ItemType.Refresh) && CanUseProp(ItemType.Refresh))
        {
            var group = refreshBtn.transform.Find("CountGroup");
            group.gameObject.SetActive(true);
            group.Find("Count").GetComponent<Text>().text = GameData.itemPlayerData.GetCount(ItemType.Refresh).ToString();
        }
        else
        {
            refreshBtn.transform.Find("CountGroup").gameObject.SetActive(false);
        }


        if (GameData.itemPlayerData.IsUnlock(ItemType.Remove) && CanUseProp(ItemType.Remove))
        {
            var group = removeBtn.transform.Find("CountGroup");
            group.gameObject.SetActive(true);
            group.Find("Count").GetComponent<Text>().text = GameData.itemPlayerData.GetCount(ItemType.Remove).ToString();
        }
        else
        {
            removeBtn.transform.Find("CountGroup").gameObject.SetActive(false);
        }

        if (GameData.itemPlayerData.IsUnlock(ItemType.Magnet) && CanUseProp(ItemType.Magnet))
        {
            var group = magnetBtn.transform.Find("CountGroup");
            group.gameObject.SetActive(true);
            group.Find("Count").GetComponent<Text>().text =
                GameData.itemPlayerData.GetCount(ItemType.Magnet).ToString();
        }
        else
        {
            magnetBtn.transform.Find("CountGroup").gameObject.SetActive(false);
        }
    }

    #endregion

    //IEnumerator Start()
    private void Init()
    {
        UIInit();
    }
    

    public void LuckButton(Button but,bool luck=true)
    {
        if (but == null)
        {
            return;
        }

        if (luck)
        {
            but.GetComponent<Image>().sprite = luckSprite;  
        }
        else
        {
            but.GetComponent<Image>().sprite = unLuckSprite;
        }
        but.transform.Find("suo").gameObject.SetActive(luck);
        but.GetComponent<Image>().raycastTarget=!luck;
                
        but.transform.Find("Text").gameObject.SetActive(luck);
        but.transform.Find("Icon").gameObject.SetActive(!luck);
        but.transform.Find("CountGroup").gameObject.SetActive(!luck);
        but.transform.Find("PlusIcon").gameObject.SetActive(!luck);
    }
    
    /// <summary>
    /// 更新金币
    /// </summary>
    public void RefreshGoldCount()
    {
        if (goldCountText != null)
        {
            goldCountText.text = ((float)GameData.Gold.Count).ConvertToKMGString();
        }
    }

    #region 开始动画相关

    //IEnumerator BeginShowSticker() 
    //{

    //    iconImage.transform.parent.gameObject.SetActive(false);
    //    stickerBg.InitState(currentLevelModelLockSpr);
    //    //stickerBg.InitStickerController(currentStickerImage);
    //    yield return stickerBg.BeginAnimation(iconImage.transform, () => {
    //        iconImage.transform.parent.gameObject.SetActive(true);

    //        iconImage.transform.parent.DOScale(new Vector3(1.27f, 1.27f, 0.65f), 0.15f).SetEase(Ease.OutQuad).OnComplete(() =>
    //        {
    //            iconImage.transform.parent.DOScale(Vector3.one, 0.15f).SetEase(Ease.InQuad);
    //        });
    //    });

    //    stickerBg.gameObject.SetActive(false);
    //}

    #endregion

    void OnDestroy()
    {
        setttingButton.onClick.RemoveAllListeners();
        refreshBtn?.onClick.RemoveListener(OnRefreshBtnClick);
        removeBtn?.onClick.RemoveListener(OnRemoveBtnClick);
        magnetBtn?.onClick.RemoveListener(OnMagnetBtnClick);
    }

    private void UIInit()
    {
        if (!GameData.IsGaming)
        {
            return;
        }

        setttingButton.onClick.AddListener(ClickSettingMethod);

        RefreshGoldCount();
        
    }

    private void ClickSettingMethod()
    {
        if (GameState.IsGameStart == false) return;
        if (GameState.IsGameWin) return;

        //if (MiniGameSolution.Utilities.IsInFeedStatus) return;

        GameState.GamePause();
        //Reporter.GamePause();

        AudioManager.Instance.PlayButtonAudioAndVibrate();
        UIManager.Instance.ShowSettingPanel(true);
    }

    /*private int GetCurrentLevelHasTapeCount()
    {
        return GameData.TotalTapeCount - GameData.RemovedTapeCount;
    }*/

    
    public void ShowRemoveTip(bool show)
    {
        removeTip.SetActive(show);
    }

    #region Hard Level

    public void InitHardTip()
    {
        if (isHardTipInit)
        {
            return;
        }

        isHardTipInit = true;

        if (hardTipHardImg == null)
        {
            return;
        }
        hardImgPos = hardTipHardImg.position;
        superHardImgPos = hardTipSuperHardImg.position;
        hardImgScale = hardTipHardImg.localScale;
        superHardImgScale = hardTipSuperHardImg.localScale;
    }

    public void ShowHardTip()
    {
        InitHardTip();
        Transform img = null, endImg = null;

        if (GameData.LevelDiff == 1)
        {
            SetOutlineHex(levelText,"#6406c2");
            hardTipBgImg.sprite = hardTipBgHard;
            //hardTipBgImg.gameObject.SetActive(true);
            endImg = levelBgHardImg;
            img = hardTipHardImg;
            img.position = hardImgPos;
            img.localScale = hardImgScale;
            hardTipHardText.gameObject.SetActive(true);
            hardTipText.color = WSGameTools.HexToColor("#973cd3");
            endImg.parent.gameObject.SetActive(true);

            levelBgSuperHardImg.parent.gameObject.SetActive(false);
            levelBgSuperHardImg.gameObject.SetActive(false);
            hardTipSuperHardText.gameObject.SetActive(false);
            hardTipSuperHardImg.gameObject.SetActive(false);
        }
        else if (GameData.LevelDiff == 2)
        {
            SetOutlineHex(levelText,"#7f0a08");
            hardTipBgImg.sprite = hardTipBgSuperHard;
            //superHardTipBgImg.gameObject.SetActive(true);
            endImg = levelBgSuperHardImg;
            img = hardTipSuperHardImg;
            img.position = superHardImgPos;
            img.localScale = superHardImgScale;
            hardTipSuperHardText.gameObject.SetActive(true);
            hardTipText.color = WSGameTools.HexToColor("#C32A25");
            endImg.parent.gameObject.SetActive(true);

            levelBgHardImg.parent.gameObject.SetActive(false);
            levelBgHardImg.gameObject.SetActive(false);
            hardTipHardText.gameObject.SetActive(false);
            hardTipHardImg.gameObject.SetActive(false);
        }
        else
        {
            SetOutlineHex(levelText,"#000000");
            levelBgSuperHardImg.parent.gameObject.SetActive(false);
            levelBgHardImg.parent.gameObject.SetActive(false);
            levelBgHardImg.gameObject.SetActive(false);
            levelBgSuperHardImg.gameObject.SetActive(false);
            hardTip.gameObject.SetActive(false);
            hardTipHardImg.gameObject.SetActive(false);
            hardTipSuperHardImg.gameObject.SetActive(false);
            return;
        }

        hardTip.gameObject.SetActive(true);
        hardTip.localScale = Vector3.one;
        endImg.gameObject.SetActive(false);
        img.gameObject.SetActive(true);

        var imgsp = img.GetComponent<Image>();
        imgsp.color = Color.white;

        ShowSingleAlert(() =>
        {
            ShowSingleAlert(() =>
            {
                hardTip.DOScale(0.4f, 0.2f).SetEase(Ease.InBack).OnComplete(() =>
                {
                    hardTip.gameObject.SetActive(false);
                });

                img.DOScale(endImg.localScale, 0.5f);
                img.DOMove(endImg.position, 0.5f).OnComplete(() =>
                {
                    endImg.gameObject.SetActive(true);
                    imgsp.DOFade(0, 0.4f);
                    img.DOScale(endImg.localScale * 1.35f, 0.4f).OnComplete(() => { img.gameObject.SetActive(false); });
                });
            });
        });
    }

    public void ShowSingleAlert(System.Action cb = null)
    {
        alertImg.gameObject.SetActive(true);

        alertImg.color = new Color(1, 1, 1, 0);
        alertImg.DOFade(0.7f, 0.3f).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            alertImg.DOFade(0, 0.3f).SetDelay(0.2f).SetEase(Ease.InQuad).OnComplete(() =>
            {
                alertImg.gameObject.SetActive(false);
                cb?.Invoke();
            });
        });
    }

    #endregion
    private static bool _hideUI;
    public static bool hideUI
    {
        get { return _hideUI; }
        set
        {
            /*if (value != _hideUI)
            {
                _hideUI = value;
                var gp = UIManager.Instance.gameInnerUI;
                if (_hideUI)
                {
                    gp.transform.Find("SettingButton").GetComponent<Image>().color = new Color(1, 1, 1, 0);
                    gp.transform.Find("HomeButton").gameObject.SetActive(false);
                    gp.transform.Find("LevelNum").gameObject.SetActive(false);
                    gp.transform.Find("TapeCount").gameObject.SetActive(false);
                    gp.transform.Find("Process").gameObject.SetActive(false);
                    gp.transform.Find("DistanceChange").gameObject.SetActive(false);
                    gp.transform.Find("BtnsGroup").gameObject.SetActive(false);
                    gp.transform.Find("GoldFrame").gameObject.SetActive(false);
                    gp.transform.Find("HardLevel").gameObject.SetActive(false);
                }
                else
                {
                    gp.transform.Find("SettingButton").GetComponent<Image>().color = new Color(1, 1, 1, 1);
                    gp.transform.Find("HomeButton").gameObject.SetActive(true);
                    gp.transform.Find("LevelNum").gameObject.SetActive(true);
                    gp.transform.Find("TapeCount").gameObject.SetActive(true);
                    gp.transform.Find("Process").gameObject.SetActive(true);
                    gp.transform.Find("DistanceChange").gameObject.SetActive(true);
                    gp.transform.Find("BtnsGroup").gameObject.SetActive(true);
                    gp.transform.Find("GoldFrame").gameObject.SetActive(true);
                    gp.transform.Find("HardLevel").gameObject.SetActive(true);
                }
            }*/
        }
    }
    //动态改描边颜色
    public void SetOutlineHex(Text text, string hexColor)
    {
        if (text.GetComponent<Outline>()!=null)
        {
            text.GetComponent<Outline>().effectColor = WSGameTools.HexToColor(hexColor);
        }
    }
}