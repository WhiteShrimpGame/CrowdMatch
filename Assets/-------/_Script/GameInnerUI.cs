using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using CrowdMatch;
using UnityEngine;
using UnityEngine.UI;

public class GameInnerUI : MonoBehaviour
{
    [Header("UIReference")] 
    [SerializeField] Button setttingButton;
    [SerializeField] Button homeButton;
    [SerializeField] Text levelText;
    //[SerializeField] Text tapeCountText;
    //[SerializeField] Text levelProcessText;
    [SerializeField] Text goldCountText;
    //[SerializeField] Image iconImage;

    //[SerializeField] Image maskImage;

    [SerializeField] Button addSlotBtn;
    [SerializeField] Button clearWaitSlotBtn;
    [SerializeField] Button removeAimTapBtn;
    [SerializeField] Button removeAimTapeCloseBtn;

    public GameObject removeTip;
    public GameObject GuideTip;



    [Header("GameDataConfig")] [SerializeField]
    float levelMaxScale = 1.5f;

    [SerializeField] float levelMinScale = 0.5f;
    Vector3 levelScale;
    float levelScaleMulti = 1;

    [Header("HardLevel")] [SerializeField] Image hardTipBgImg;
    [SerializeField] Sprite hardTipBgHard;
    [SerializeField] Sprite hardTipBgSuperHard;

    [SerializeField] private Transform levelBgHardImg, levelBgSuperHardImg;
    [SerializeField] private Transform hardTip;
    [SerializeField] private Transform hardTipHardText, hardTipSuperHardText;
    [SerializeField] private Transform hardTipHardImg, hardTipSuperHardImg;
    [SerializeField] private Image alertImg;
    [SerializeField] private Image arrowTipsImg;

    private Vector3 hardImgPos, superHardImgPos;
    private Vector3 hardImgScale, superHardImgScale;

    private bool isHardTipInit;

    //private int currentLevelBlocksIndex;

    //public LevelBlockController CurrentLevelBlock { get { return levelBlocksGroup[currentLevelBlocksIndex]; } }

    private Sprite currentLevelModelSpr;
    private Sprite currentLevelModelLockSpr;

    private float startNormalize = 0.5f;
    public static GameObject obj;
    public Sprite unLuckSprite;
    public Sprite luckSprite;
    public bool isGuide;
    

    private void Awake()
    {
        /*addSlotBtn?.onClick.AddListener(AddSlotMethod);
        clearWaitSlotBtn?.onClick.AddListener(ClearWaitSlotMethod);
        removeAimTapBtn?.onClick.AddListener(RemoveAimTapeMethod);
        removeAimTapeCloseBtn?.onClick.AddListener(OnRemoveTapeCloseBtnClick);*/
        homeButton?.onClick.AddListener(OnBackBtnClk);

        //UpdateCurrentButtonInfo();
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
        Init();
    }

    /// <summary>
    /// 使用了去除胶带的逻辑，回复按钮状态
    /// </summary>
    public void RestoreRemoveAimTapeButtonState()
    {
        /*removeAimTapBtn.interactable = true;
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

        
    }

    /*#region 道具按钮方法

    //按钮触发方法：直接移除Tape
    private void RemoveAimTapeMethod()
    {
        if (obj != null)
        {
            obj.GetComponent<GuideMaskPanel>().Hide();
            GameState.GameStart();
            Reporter.GameStart();
        }

        AudioManager.Instance.PlayButtonAudioAndVibrate();
        HideFoolProofPropTips();

        if (!GameState.IsGameStart)
        {
            return;
        }

        if (GameData.isNoCheckRemoveTape)
            return;

        var propId = "RemoveAimTape";

        if (!CanUseProp(propId))
        {
            var findPropInfo = GameManager.Instance.GetAimPropInfo(propId);
            if (GoldConfig.IsItemOutOfUse(findPropInfo.propType))
            {
                UIManager.Instance.ShowTip("道具已用完");
            }
            else
            {
                UIManager.Instance.ShowPropGetTip(true, findPropInfo, (type, count) =>
                {
                    GetAimProp(propId, type, count);
                    UpdateCurrentButtonInfo();
                    RefreshGoldCount();
                });
            }
        }
        else
        {
            if (!GameController.Instance.CheckCanRemoveTape())
            {
                UIManager.Instance.ShowTip("场景中没有胶带！");
                return;
            }

            // CousmeProp(propId);
            // UpdateCurrentButtonInfo();
            LogicRemoveAimTapeMethod();
            // WS_TapAway_Cloud.LevelRecord.SaveLevelRecord();
            // MiniGameSolution.Utilities.PlayerPrefs.Save();
        }
    }

    private void LogicRemoveAimTapeMethod()
    {
        GameData.isNoCheckRemoveTape = true;
        maskImage.gameObject.SetActive(true);
        removeAimTapBtn.interactable = false;
        removeTip.SetActive(true);
        removeAimTapBtn.transform.Find("CloseBtn").Show();

        GameController.Instance.curLevel.MakeAllTapeBright();
    }

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
    public void ConsumeRemoveTapeProp()
    {
        var propId = "RemoveAimTape";
        CousmeProp(propId);
        UpdateCurrentButtonInfo();
        MiniGameSolution.Utilities.PlayerPrefs.Save();
    }

    //按钮触发方法：清空槽位
    private void ClearWaitSlotMethod()
    {
        if (obj != null)
        {
            obj.GetComponent<GuideMaskPanel>().Hide();
            GameState.GameStart();
            Reporter.GameStart();
        }

        AudioManager.Instance.PlayButtonAudioAndVibrate();
        HideFoolProofPropTips();

        if (!GameState.IsGameStart)
        {
            return;
        }

        var propId = "ClearWaitSlot";

        if (!CanUseProp(propId))
        {
            var findPropInfo = GameManager.Instance.GetAimPropInfo(propId);
            if (GoldConfig.IsItemOutOfUse(findPropInfo.propType))
            {
                UIManager.Instance.ShowTip("道具已用完");
            }
            else
            {
                UIManager.Instance.ShowPropGetTip(true, findPropInfo, (type, count) =>
                {
                    GetAimProp(propId, type, count);
                    UpdateCurrentButtonInfo();
                    RefreshGoldCount();
                });
            }
        }
        else
        {
            if (!GameController.Instance.CheckCanClearWaitList())
            {
                UIManager.Instance.ShowTip("等待区无胶带可清除");
                return;
            }

            CousmeProp(propId);
            UpdateCurrentButtonInfo();
            LogicClearWaitAllItems();
            WS_TapAway_Cloud.LevelRecord.SaveLevelRecord();
            MiniGameSolution.Utilities.PlayerPrefs.Save();
        }
    }

    private void LogicClearWaitAllItems()
    {
        GameController.Instance.waitAlert.HideRed();
        GameController.Instance.waitAlert.HideSign();
        GameController.Instance.ClearWaitAllItems();

        DOVirtual.DelayedCall(0.2f, () => { AudioManager.Instance.playClip(11); });
    }

    //按钮触发方法：增加槽位
    private void AddSlotMethod()
    {
        if (obj != null)
        {
            obj.GetComponent<GuideMaskPanel>().Hide();
            GameState.GameStart();
            Reporter.GameStart();
        }

        AudioManager.Instance.PlayButtonAudioAndVibrate();
        HideFoolProofPropTips();

        if (!GameState.IsGameStart)
        {
            return;
        }

        var propId = "AddSlot";

        if (!CanUseProp(propId))
        {
            var findPropInfo = GameManager.Instance.GetAimPropInfo(propId);
            if (GoldConfig.IsItemOutOfUse(findPropInfo.propType))
            {
                UIManager.Instance.ShowTip("道具已用完");
            }
            else
            {
                UIManager.Instance.ShowPropGetTip(true, findPropInfo, (type, count) =>
                {
                    GetAimProp(propId, type, count);
                    UpdateCurrentButtonInfo();
                    RefreshGoldCount();
                });
            }
        }
        else
        {
            if (!GameController.Instance.CheckCanAddWaitList())
            {
                UIManager.Instance.ShowTip("槽位已满，无法添加");
                return;
            }

            CousmeProp(propId);
            UpdateCurrentButtonInfo();
            LogicAddSlot();
            WS_TapAway_Cloud.LevelRecord.SaveLevelRecord();
            MiniGameSolution.Utilities.PlayerPrefs.Save();
        }
    }

    private void LogicAddSlot()
    {
        GameController.Instance.UnlockWait();
    }

    private bool CanUseProp(string propId)
    {
        return GameData.itemPlayerData.GetCount(PropInfo.PropToType(propId)) > 0;
    }

    private void CousmeProp(string propId)
    {
        GameData.ItemUseCount++;
        GameData.itemPlayerData.CostCount(PropInfo.PropToType(propId));
    }

    private void GetAimProp(string propId, int type, int count)
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

        GameData.itemPlayerData.AddCount(PropInfo.PropToType(propId), add: count, way: way, getType: getType);
    }


    private void UpdateCurrentButtonInfo()
    {
        if (addSlotBtn == null)
        {
            return;
        }

        var propAddSlot = "AddSlot";
        var propClearWaitSlot = "ClearWaitSlot";
        var propRemoveAimTape = "RemoveAimTape";

        if (CanUseProp(propAddSlot))
        {
            var group = addSlotBtn.transform.Find("CountGroup");
            group.gameObject.SetActive(true);
            group.Find("Count").GetComponent<Text>().text = GameData.itemPlayerData.GetCount(ItemType.Add).ToString();
        }
        else
        {
            addSlotBtn.transform.Find("CountGroup").gameObject.SetActive(false);
        }


        if (CanUseProp(propClearWaitSlot))
        {
            var group = clearWaitSlotBtn.transform.Find("CountGroup");
            group.gameObject.SetActive(true);
            group.Find("Count").GetComponent<Text>().text = GameData.itemPlayerData.GetCount(ItemType.Clear).ToString();
        }
        else
        {
            clearWaitSlotBtn.transform.Find("CountGroup").gameObject.SetActive(false);
        }

        if (CanUseProp(propRemoveAimTape))
        {
            var group = removeAimTapBtn.transform.Find("CountGroup");
            group.gameObject.SetActive(true);
            group.Find("Count").GetComponent<Text>().text =
                GameData.itemPlayerData.GetCount(ItemType.Remove).ToString();
        }
        else
        {
            removeAimTapBtn.transform.Find("CountGroup").gameObject.SetActive(false);
        }
    }

    #endregion*/

    //IEnumerator Start()
    private void Init()
    {
        //levelGroup.SetActive(false);
        //yield return new WaitForEndOfFrame();
        UIInit();

        //关掉开始动画
        /*if (GameData.IsGaming)
        {
            GameManager.Instance.itemData.InitFreeItemConfig();
            var itemFreeConfig = GameManager.Instance.itemData.freeItemConfig;
            var addCfg = itemFreeConfig.GetValueOrDefault(4);
            var removeCfg = itemFreeConfig.GetValueOrDefault(5);
            var clearCfg = itemFreeConfig.GetValueOrDefault(6);

            if (addSlotBtn != null)
            {
                if (addCfg != null && GameData.CurrentLevel < addCfg.level)
                {
                    LuckButton(addSlotBtn);
                    addSlotBtn.transform.Find("Text").GetComponent<Text>().text = "第 " + addCfg.level + " 关";
                }
                else
                {
                    LuckButton(addSlotBtn, false);
                    addSlotBtn.transform.Find("Text").gameObject.SetActive(false);
                    //UpdateCurrentButtonInfo();
                }
            }
            if (removeAimTapBtn != null)
            {
                if (removeCfg != null && GameData.CurrentLevel < removeCfg.level)
                {
                    LuckButton(removeAimTapBtn);
                    removeAimTapBtn.transform.Find("Text").GetComponent<Text>().text = "第 " + removeCfg.level + " 关";
                }

                else
                {
                    LuckButton(removeAimTapBtn, false);
                    removeAimTapBtn.transform.Find("Text").gameObject.SetActive(false);
                    UpdateCurrentButtonInfo();
                }
            }
            if (clearWaitSlotBtn != null)
            {
                if (clearCfg != null && GameData.CurrentLevel < clearCfg.level)
                {
                    LuckButton(clearWaitSlotBtn);
                    clearWaitSlotBtn.transform.Find("Text").GetComponent<Text>().text = "第 " + clearCfg.level + " 关";
                }
                else
                {
                    LuckButton(clearWaitSlotBtn, false);
                    clearWaitSlotBtn.transform.Find("Text").gameObject.SetActive(false);
                    UpdateCurrentButtonInfo();
                }
            }
            bool needShowAddGuide = gameObject.activeSelf && !GameData.IsShowAddGuide && addCfg != null &&
                                    GameData.CurrentLevel == addCfg.level;
            bool needShowRemoveGuide = gameObject.activeSelf && !GameData.IsShowRemoveGuide && removeCfg != null &&
                                       GameData.CurrentLevel == removeCfg.level;
            bool needShowClearGuide = gameObject.activeSelf && !GameData.IsShowClearGuide && clearCfg != null &&
                                      GameData.CurrentLevel == clearCfg.level;
            //同关只触发 P1 更小的（4 优先于 5）
            if (needShowAddGuide)
            {
                GameData.itemPlayerData.AddCount(ItemType.Add, addCfg.count, "Guide");
                ShowGuide(addSlotBtn);
                //addSlotBtn.gameObject.SetActive(true);
                /*addSlotBtn.GetComponent<Image>().sprite = unLuckSprite;
                addSlotBtn.transform.Find("suo").gameObject.SetActive(false);//未加
                addSlotBtn.GetComponent<Image>().raycastTarget=true;
                GameState.GamePause();
                Reporter.GamePause();
                GameData.IsShowAddGuide = true;#1#
                
                /*UpdateCurrentButtonInfo();
                obj = Instantiate(UIManager.Instance.maskUIPrefab, UIManager.Instance.transform);
                obj.name = "GuideMaskPanel";
                obj.GetComponent<GuideMaskPanel>().Show();
                obj.GetComponent<GuideMaskPanel>().Guide(addSlotBtn.transform,
                    addSlotBtn.GetComponent<RectTransform>().sizeDelta * 1.1f, 0.6f);#1#
            }
            // 只有不触发4的情况下，才判断是否触发5
            else if (needShowRemoveGuide)
            {
                GameData.itemPlayerData.AddCount(ItemType.Remove, removeCfg.count, "Guide");
                ShowGuide(removeAimTapBtn);
                //removeAimTapBtn.gameObject.SetActive(true);
                /*removeAimTapBtn.GetComponent<Image>().sprite = unLuckSprite;
                removeAimTapBtn.transform.Find("suo").gameObject.SetActive(false);//未加
                removeAimTapBtn.GetComponent<Image>().raycastTarget=true;
                GameState.GamePause();
                Reporter.GamePause();
                GameData.IsShowRemoveGuide = true;#1#
                
                /*UpdateCurrentButtonInfo();
                obj = Instantiate(UIManager.Instance.maskUIPrefab, UIManager.Instance.transform);
                obj.name = "GuideMaskPanel";
                obj.GetComponent<GuideMaskPanel>().Show();
                obj.GetComponent<GuideMaskPanel>().Guide(removeAimTapBtn.transform,
                    removeAimTapBtn.GetComponent<RectTransform>().sizeDelta * 1.1f, 0.6f);#1#
            }
            else if (needShowClearGuide)
            {
                GameData.itemPlayerData.AddCount(ItemType.Clear, clearCfg.count, "Guide");
                ShowGuide(clearWaitSlotBtn);
                
                /*clearWaitSlotBtn.gameObject.SetActive(true);
                GameState.GamePause();
                Reporter.GamePause();
                GameData.IsShowClearGuide = true;
                GameData.itemPlayerData.AddCount(ItemType.Clear, clearCfg.count, "Guide");
                UpdateCurrentButtonInfo();
                obj = Instantiate(UIManager.Instance.maskUIPrefab, UIManager.Instance.transform);
                obj.name = "GuideMaskPanel";
                obj.GetComponent<GuideMaskPanel>().Show();
                obj.GetComponent<GuideMaskPanel>().Guide(clearWaitSlotBtn.transform,
                    clearWaitSlotBtn.GetComponent<RectTransform>().sizeDelta * 1.1f, 0.6f);#1#
                GameController.Instance.AddFakeTape();
            }*/

            //StartCoroutine(BeginShowSticker());

            //iconImage.transform.parent.DOScale(new Vector3(1.27f, 1.27f, 0.65f), 0.15f).SetEase(Ease.OutQuad).OnComplete(() =>
            //{
            //    iconImage.transform.parent.DOScale(Vector3.one, 0.15f).SetEase(Ease.InQuad);
            //});

            //levelScale = GameController.Instance.curLevel.transform.localScale;
            levelScaleMulti = 1;
            startNormalize = (1 - levelMinScale) / (levelMaxScale - levelMinScale);
        
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
    public void ShowGuide(Button but)
    {
        /*but.GetComponent<Image>().sprite = unLuckSprite;
        but.transform.Find("Text").gameObject.SetActive(false);
        but.transform.Find("suo").gameObject.SetActive(false);
        but.transform.Find("Icon").gameObject.SetActive(true);
        but.transform.Find("CountGroup").gameObject.SetActive(true);
        
        but.GetComponent<Image>().raycastTarget=true;
        GameState.GamePause();

        if (but==addSlotBtn)
        {
            GameData.IsShowAddGuide = true;
        }

        if (but==removeAimTapBtn)
        {
            GameData.IsShowRemoveGuide = true;
        }
        if (but==clearWaitSlotBtn)
        {
            GameData.IsShowClearGuide = true;
        }
        
        //GameData.itemPlayerData.AddCount(ItemType.Add, addCfg.count, "Guide");
        UpdateCurrentButtonInfo();
        obj = Instantiate(UIManager.Instance.maskUIPrefab, UIManager.Instance.transform);
        obj.name = "GuideMaskPanel";
        obj.GetComponent<GuideMaskPanel>().Show();
        obj.GetComponent<GuideMaskPanel>().Guide(but.transform,
            but.GetComponent<RectTransform>().sizeDelta * 1.1f, 0.6f);*/
    }

    public static float InverseLerp(Vector3 min, Vector3 max, Vector3 current)
    {
        Vector3 ab = max - min; // 整段向量
        Vector3 av = current - min; // 当前点到起点的向量

        float abLenSq = ab.sqrMagnitude; // |AB|²   （省一次开方）
        if (abLenSq < Mathf.Epsilon) // 起点 == 终点的防错
            return 0f;

        float dot = Vector3.Dot(av, ab); // 投影长度 * |AB|
        float t = dot / abLenSq; // 投影长度 / |AB|

        return Mathf.Clamp01(t); // 保证在 0‑1
    }

    /// <summary>
    /// 更新金币
    /// </summary>
    public void RefreshGoldCount()
    {
        if (goldCountText != null)
        {
            //goldCountText.text = ((float)GameData.Gold.Count).ConvertToKMGString();
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
        /*addSlotBtn?.onClick.RemoveListener(AddSlotMethod);
        clearWaitSlotBtn?.onClick.RemoveListener(ClearWaitSlotMethod);
        removeAimTapBtn?.onClick.RemoveListener(RemoveAimTapeMethod);*/
    }

    private void UIInit()
    {
        if (!GameData.IsGaming)
        {
            return;
        }
        /*if (!isGuide&&GameData.CurrentLevel==1)
        {
            addSlotBtn.transform.parent.gameObject.SetActive(!(!isGuide&&GameData.CurrentLevel==1));
        }
        else
        {
            addSlotBtn.transform.parent.gameObject.SetActive(true);
        }*/
        addSlotBtn?.transform.parent.gameObject.SetActive(!(isGuide&&GameData.CurrentLevel==1));
        //GuideTip?.SetActive(isGuide&&GameData.CurrentLevel==1);
        setttingButton.onClick.AddListener(ClickSettingMethod);

        //currentLevelModelSpr = GameController.Instance.curLevel.GetComponent<LevelStickerController>().currentModelSprite;
        //currentLevelModelLockSpr = GameController.Instance.curLevel.GetComponent<LevelStickerController>().currentModelLockSprite;
        //levelProcessText.transform.parent.parent.Find("icon").GetComponent<Image>().sprite = currentLevelModelSpr;
        //levelProcessText.transform.parent.parent.Find("iconGray").GetComponent<Image>().sprite = currentLevelModelLockSpr;

        RefreshGoldCount();
        
    }

    private void ClickSettingMethod()
    {
        if (GameState.IsGameStart == false) return;
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

        /*if (GameData.LevelDiff == 1)
        {
            hardTipBgImg.sprite = hardTipBgHard;
            endImg = levelBgHardImg;
            img = hardTipHardImg;
            img.position = hardImgPos;
            img.localScale = hardImgScale;
            hardTipHardText.gameObject.SetActive(true);

            levelBgSuperHardImg.gameObject.SetActive(false);
            hardTipSuperHardText.gameObject.SetActive(false);
            hardTipSuperHardImg.gameObject.SetActive(false);
        }
        else if (GameData.LevelDiff == 2)
        {
            hardTipBgImg.sprite = hardTipBgSuperHard;
            endImg = levelBgSuperHardImg;
            img = hardTipSuperHardImg;
            img.position = superHardImgPos;
            img.localScale = superHardImgScale;
            hardTipSuperHardText.gameObject.SetActive(true);

            levelBgHardImg.gameObject.SetActive(false);
            hardTipHardText.gameObject.SetActive(false);
            hardTipHardImg.gameObject.SetActive(false);
        }
        else*/
        {
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
}