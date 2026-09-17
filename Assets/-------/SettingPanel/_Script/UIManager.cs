using System;
using System.Collections.Generic;
using CrowdMatch;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.Events;
using UnityEngine.Networking;

[DefaultExecutionOrder(-80)] //越小越先执行
[AddComponentMenu("UIManager")]
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    private Canvas mCanvas;

    public Image gameBg;
    [HideInInspector] public Transform mainPanel;
    [HideInInspector] public Transform gamePanel;
    [HideInInspector] public Transform failPanel;
    [HideInInspector] public Transform revivePanel;
    [HideInInspector] public Transform winPanel;
    [HideInInspector] public Transform settingPanel;
    [HideInInspector] public Transform winPartPanel;
    [HideInInspector] public Transform retryPanel;
    [HideInInspector] public Transform shareBoxPanel;
    [HideInInspector] public Transform addBoxPanel;
    //[HideInInspector] public Transform getStaminaPanel;
    //[HideInInspector] public Transform unlimitedStaminaPanel;
    [HideInInspector] public Transform propGetTipPanel;
    
    public GameObject mainPanelPrefab;
    public GameObject settingPanelPrefab;
    public GameObject winPartPanelPrefab;
    public GameObject revivePanelPrefab;
    public GameObject winPanelPrefab;
    public GameObject failPanelPrefab;
    public GameObject getRewardPanelPrefab;
    public GameObject retryPanelPrefab;
    public GameObject collectionPanelPrefab;
    public GameObject propGetTipPanelPrefab;
    public GameInnerUI gameInnerUI;
    public GameObject handPanelTipsPanel;
    public GameObject recordPanelPrefab;
    public GameObject shareBoxPanelPrefab;
    public GameObject addBoxPanelPrefab;
    public GameObject getStaminaPanelPrefab;
    public GameObject unlimitedStaminaPanelPrefab;


    public Transform tipsTransform;

    public static bool IsPanelShow = false;

    public Camera uiCam;

    EventSystem _es;
    GraphicRaycaster _gr;

    public bool isShowingBanner
    {
        get { return _isShowingBanner; }
    }

    bool _isShowingBanner = false;

    public bool isMainPanelActive
    {
        get
        {
            return false;
            /*return mainPanel != null &&
                   mainPanel.GetComponent<MainPanel>().isActiveAndEnabled;*/
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        mCanvas = GetComponent<Canvas>();
        _es = GameObject.Find("EventSystem").GetComponent<EventSystem>();
        _gr = GetComponent<GraphicRaycaster>();

        //gamePanel = transform.Find("GamePanel");
    }

    public void Init()
    {
        _isTipShowing = false;
        //var tip = transform.Find("GameTip");
        var tip = transform.Find("GameTip");
        if (tip != null)
            tip.gameObject.SetActive(false);

        Debug.Log($"当前是否处于运行:{GameData.IsGaming}");
        if (GameData.IsGaming)
        {
            IsPanelShow = false;

            showGamePanel(true);
            showMainPanel(false);

            

            gameInnerUI?.GetComponent<GameInnerUI>().ResetLevel();
            ShowBanner();
        }
        else
        {
            showGamePanel(false);
            HideBanner();
        }

        ShowSettingPanel(false);
        showRevivePanel(false);
        showFailPanel(false);
        showWinPanel(false);
        //ShowShareBoxPanel(false);
    }

    //private void OnEnable()
    //{
    //    Init();
    //}

    public bool isRaycastUI
    {
        get
        {
            PointerEventData eventData = new PointerEventData(_es);
            eventData.pressPosition = Input.mousePosition;
            eventData.position = Input.mousePosition;

            List<RaycastResult> list = new List<RaycastResult>();
            _gr.Raycast(eventData, list);
            return list.Count > 0;
        }
    }

    public void showMainPanel(bool isShow)
    {
        if (mainPanel == null && isShow)
        {
            mainPanel = Instantiate(mainPanelPrefab, transform).transform;
            //mainPanel.SetAsFirstSibling();
            mainPanel.transform.SetSiblingIndex(3);
        }

        if (mainPanel != null)
            mainPanel.gameObject.SetActive(isShow);
    }

    public void showGamePanel(bool isShow)
    {
        if (gameInnerUI == null)
        {
            return;
        }

        gameInnerUI.gameObject.SetActive(isShow);
    }

    public void showFailPanel(bool isShow)
    {
        if (failPanel == null && isShow)
        {
            failPanel = Instantiate(failPanelPrefab, transform).transform;
        }

        if (failPanel != null)
            failPanel.gameObject.SetActive(isShow);
    }

    public void showRevivePanel(bool isShow)
    {
        if (revivePanel == null && isShow)
        {
            revivePanel = Instantiate(revivePanelPrefab, transform).transform;
        }

        if (revivePanel != null)
        {
            //gameInnerUI?.GetComponent<GameInnerUI>().HideFoolProofPropTips();
            revivePanel.gameObject.SetActive(isShow);

            if (isShow)
            {
                ShowPanel(revivePanel);
                //gamePanel?.GetComponent<GamePanel>()?.HideAllItemPanel();
                ShowSettingPanel(false);
                //ShowPropGetTip(false);
            }
        }

    }

    public void showWinPanel(bool isShow)
    {
        if (winPanel == null && isShow)
        {
            winPanel = Instantiate(winPanelPrefab, transform).transform;
        }

        if (winPanel != null)
        {
            //gameInnerUI?.GetComponent<GameInnerUI>().HideFoolProofPropTips();
            if (isShow)
            {
                ShowPanel(winPanel);
            }
            else
            {
                winPanel.gameObject.SetActive(isShow);
            }
        }
    }

    /*public void ShowRetryPanel(bool isRevive)
    {
        if (retryPanel == null)
        {
            retryPanel = Instantiate(retryPanelPrefab, transform).transform;
        }

        if (retryPanel != null)
        {
            retryPanel.GetComponent<RetryPanel>()?.Show(isRevive);
        }
    }*/

    public void ShowSettingPanel(bool isShow)
    {
        if (settingPanel == null && isShow)
        {
            settingPanel = Instantiate(settingPanelPrefab, transform).transform;
        }

        if (settingPanel != null)
        {
            settingPanel.gameObject.SetActive(isShow);
            settingPanel?.GetComponent<SettingPanel>()?.InitSettingPanel();
            if (isShow)
            {
                ShowPanel(settingPanel);
            }
        }
    }

    

    public void ShowRecordPanel()
    {
        Instantiate(recordPanelPrefab, transform);
    }

 

    /*/// <summary>
    /// 显示奖励面板
    /// </summary>
    /// <returns></returns>
    public GetRewardPanel ShowGetRewardPanel()
    {
        if (GetComponentInChildren<GetRewardPanel>() != null)
            return GetComponentInChildren<GetRewardPanel>();
        return Instantiate(getRewardPanelPrefab, transform).GetComponent<GetRewardPanel>();
    }

    public void ShowPropGetTip(bool isShow, PropInfo propInfo = null, Action<int, int> finish = null)
    {
        if (propGetTipPanel == null && isShow)
        {
            propGetTipPanel = Instantiate(propGetTipPanelPrefab, transform).transform;
        }

        if (propGetTipPanel != null)
        {
            if (isShow)
            {
                var getCom = propGetTipPanel.GetComponent<GetPropTipPanel>();

                if (getCom != null)
                {
                    getCom.InitPropPanel(propInfo, finish);
                }

                ShowPanel(propGetTipPanel);
            }
            else
            {
                propGetTipPanel.gameObject.SetActive(isShow);
            }
        }
    }

    public void ShowShareBoxPanel(bool isShow)
    {
        if (shareBoxPanel == null && isShow)
        {
            shareBoxPanel = Instantiate(shareBoxPanelPrefab, transform).transform;
        }

        if (shareBoxPanel != null)
        {
            shareBoxPanel.gameObject.SetActive(isShow);

            if (isShow)
            {
                ShowPanel(shareBoxPanel);
            }
        }
    }

    public void ShowAddBoxPanel(bool isShow)
    {
        if (addBoxPanel == null && isShow)
        {
            addBoxPanel = Instantiate(addBoxPanelPrefab, transform).transform;
        }

        if (addBoxPanel != null)
        {
            addBoxPanel.gameObject.SetActive(isShow);

            if (isShow)
            {
                ShowPanel(addBoxPanel);
            }
        }
    }*/
    /*public void ShowStaminaPanel(bool isShow)
    {
        if (getStaminaPanel == null && isShow)
        {
            getStaminaPanel = Instantiate(getStaminaPanelPrefab, transform).transform;
        }

        if (getStaminaPanel != null)
        {
            getStaminaPanel.gameObject.SetActive(isShow);

            if (isShow)
            {
                ShowPanel(getStaminaPanel);
            }
        }
    }

    public void ShowUnlimitedStaminaPanel(bool isShow)
    {
        if (unlimitedStaminaPanel == null && isShow)
        {
            unlimitedStaminaPanel = Instantiate(unlimitedStaminaPanelPrefab, transform).transform;
        }

        if (unlimitedStaminaPanel != null)
        {
            unlimitedStaminaPanel.gameObject.SetActive(isShow);

            if (isShow)
            {
                ShowPanel(unlimitedStaminaPanel);
            }
        }
    }*/


    public void ShowPanel(Transform panel)
    {
        IsPanelShow = true;
        panel.gameObject.SetActive(true);
        var bg = panel.Find("BG");
        bg.localScale = UnityEngine.Vector3.one;
        bg.DOScale(0.4f, 0.3f).From().SetEase(Ease.OutBack);
    }

    public void HidePanel(Transform panel, UnityAction act = null)
    {
        panel.Find("BG").DOScale(0.4f, 0.2f).SetEase(Ease.InBack).OnComplete(delegate
        {
            panel.gameObject.SetActive(false);
            if (isMainPanelActive)
            {
                IsPanelShow = true;
            }
            else
            {
                IsPanelShow = false;
            }

            act?.Invoke();
        });
    }

    public void ShowWinPart()
    {
        if (winPartPanel == null)
        {
            winPartPanel = Instantiate(winPartPanelPrefab, transform).transform;
        }

        /*var win = winPartPanel.GetComponent<UIRainBowPanel>();
        win.rainBowPlay();*/
    }

    public void ShowBanner()
    {
        /*if (isShowingBanner)
        {
            return;
        }

        if (!PufferMiniGame.MiniGameSolution.Ad.IsShowBanner)
        {
            return;
        }

        _isShowingBanner = true;
        PufferMiniGame.MiniGameSolution.Ad.ShowBanner();*/
    }

    public void HideBanner()
    {
        if (!isShowingBanner)
        {
            return;
        }

        //PufferMiniGame.MiniGameSolution.Ad.HideBanner();
        _isShowingBanner = false;
    }

    bool _isTipShowing = false;

    public void ShowTip(string str, float dur = 1f, float anchorPosY = 0, bool isUp = true)
    {
        if (_isTipShowing) return;
        _isTipShowing = true;
        var tip = transform.Find("GameTip").GetComponent<RectTransform>();
        tip.transform.SetAsLastSibling();
        tip.Find("Text").GetComponent<Text>().text = str;
        tip.gameObject.SetActive(true);
        tip.anchoredPosition = new Vector2(0, anchorPosY);
        tip.transform.DOScaleX(0.4f, 0.3f).From().SetEase(Ease.OutBack).OnComplete(() =>
        {
            if (isUp)
            {
                tip.DOAnchorPosY(anchorPosY + 200, dur).SetEase(Ease.Linear).OnComplete(() =>
                {
                    tip.gameObject.SetActive(false);
                    _isTipShowing = false;
                });
            }
            else
            {
                DOVirtual.DelayedCall(dur, () =>
                {
                    tip.gameObject.SetActive(false);
                    _isTipShowing = false;
                });
            }
        });
    }
    /*public void ShowFixedTip()
    {

        if (_isTipShowing) return;
        _isTipShowing = true;
        Vector2 vector2;
        //vector2=Get3DObjToUIPos(GameObject.Find("WaitPosRoot(Clone)").transform.GetChild(2),transform.GetComponent<Canvas>());
        //Debug.Log(vector2);
        //vector2=GameController.Instance.waitAlert.transform.GetChild(0).GetChild(0).GetComponent<RectTransform>().position;
        vector2=Get3DObjToUIPos(GameController.Instance.waitAlert.transform.GetChild(0).GetChild(0),transform.GetComponent<Canvas>());
        Vector2 showPos=new Vector2(0, vector2.y-100);
        var tip = transform.Find("GameTip").GetComponent<RectTransform>();
        
        tip.transform.SetAsLastSibling();
        tip.Find("Text").GetComponent<Text>().text = "只剩一个空位了！";
        
        //tip.sizeDelta = new Vector2(300, tip.sizeDelta.y);

        //设置指定的初始位置 
        tip.anchoredPosition = showPos;

        tip.gameObject.SetActive(true);
        
        tip.transform.DOScaleX(0.4f, 0.3f).From().SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                //停留1秒 
                DOVirtual.DelayedCall(1f, () =>
                {
                    // 向上移动200像素，动画时长1秒
                    tip.DOAnchorPosY(tip.anchoredPosition.y + 200, 1f).SetEase(Ease.Linear)
                        .OnComplete(() =>
                        {
                            // 动画结束：隐藏提示框 + 重置状态
                            tip.gameObject.SetActive(false);
                            _isTipShowing = false;
                        });
                });
            });
    }*/
    public Vector2 Get3DObjToUIPos(Transform target3D, Canvas uiCanvas)
    {
        // 1. 获取主相机（渲染3D物体的相机）
        Camera mainCamera = Camera.main;
        
        // 2. 3D世界坐标 → 屏幕像素坐标（自动适配分辨率！）
        Vector3 screenPos = mainCamera.WorldToScreenPoint(target3D.position);

        // 安全判断：如果物体在相机背后，返回默认坐标（避免UI乱飞）
        if (screenPos.z < 0)
        {
            return new Vector2(0, 0);
        }

        // 3. 屏幕像素坐标 → UGUI锚点坐标（适配UI画布）
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            uiCanvas.transform as RectTransform,  // UI画布根节点
            screenPos,                             // 屏幕坐标
            uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : uiCanvas.worldCamera, // 相机适配
            out Vector2 uiPos                       // 最终UGUI坐标
        );

        return uiPos;
    }
    public void GenerateTip(string str, float dur = 1f)
    {
        var tip = GameManager.Instance.spawnPool.Spawn("GameTip", tipsTransform).GetComponent<RectTransform>();
        tip.Find("Text").GetComponent<Text>().text = str;
        tip.gameObject.SetActive(true);
        tip.anchoredPosition = new Vector2(0, 0);
        tip.transform.DOScaleX(0.4f, 0.3f).From().SetEase(Ease.OutBack).OnComplete(() =>
        {
            tip.DOAnchorPosY(200, dur).OnComplete(() =>
            {
                GameManager.Instance.spawnPool.Despawn(tip.gameObject);
            });
        });
    }

    public void ShowFixedTip(bool isShow, string str, float anchorPosY = 0)
    {
        var tip = transform.Find("GameTip").GetComponent<RectTransform>();
        tip.gameObject.SetActive(isShow);
        tip.Find("Text").GetComponent<Text>().text = str;
        tip.anchoredPosition = new Vector2(0, anchorPosY);
    }
    #region 获取设置头像

    private bool isDownloadingAvatar;
    private bool isDownloadedAvatar;
    private Texture2D avatarTex;
    private List<Action<Texture2D>> getAvatarTexSuccessActionList;
    private List<Action> getAvatarTexFailActionList;

    public void SetAvatarTex(Image img)
    {
        GetAvatarTex((tex) =>
        {
            Sprite sprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f) // 精灵的轴心点
            );
            img.sprite = sprite;
        });
    }

    public void GetAvatarTex(Action<Texture2D> onSuccess, Action onFailed = null)
    {
        if (isDownloadedAvatar)
        {
            onSuccess?.Invoke(avatarTex);
            return;
        }

        if (isDownloadingAvatar)
        {
            if (onSuccess != null)
            {
                getAvatarTexSuccessActionList.Add(onSuccess);
            }

            if (onFailed != null)
            {
                getAvatarTexFailActionList.Add(onFailed);
            }

            return;
        }

        DownloadAvatarTex(onSuccess, onFailed);
    }

    public void DownloadAvatarTex(Action<Texture2D> onSuccess = null, Action onFailed = null)
    {
        getAvatarTexSuccessActionList = new List<Action<Texture2D>>();
        getAvatarTexFailActionList = new List<Action>();

        if (onSuccess != null)
        {
            getAvatarTexSuccessActionList.Add(onSuccess);
        }

        if (onFailed != null)
        {
            getAvatarTexFailActionList.Add(onFailed);
        }

        isDownloadingAvatar = true;

        /*MiniGameSolution.Utilities.GetAvatarUrl((url) => { GetAvatarTexFromURL(url); },
            () =>
            {
                isDownloadingAvatar = false;
                AvatarTexFailAction();
            });*/
    }

    public void GetAvatarTexFromURL(string url)
    {
        StartCoroutine(DoGetAvatarTexFromURL(url));
    }

    System.Collections.IEnumerator DoGetAvatarTexFromURL(string url)
    {
        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(uwr.error);
                AvatarTexFailAction();
            }
            else
            {
                isDownloadingAvatar = false;
                isDownloadedAvatar = true;
                avatarTex = DownloadHandlerTexture.GetContent(uwr);
                AvatarTexSuccessAction();
            }
        }
    }

    private void AvatarTexSuccessAction()
    {
        for (int i = 0; i < getAvatarTexSuccessActionList.Count; i++)
        {
            getAvatarTexSuccessActionList[i]?.Invoke(avatarTex);
        }
    }

    private void AvatarTexFailAction()
    {
        for (int i = 0; i < getAvatarTexFailActionList.Count; i++)
        {
            getAvatarTexFailActionList[i]?.Invoke();
        }
    }

    public void GetTextureFromURL(Image img, string url)
    {
        StartCoroutine(DoGetTextureFromURL(img, url));
    }

    System.Collections.IEnumerator DoGetTextureFromURL(Image img, string url)
    {
        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(uwr.error);
            }
            else
            {
                if (img != null)
                {
                    // Get downloaded asset bundle
                    var sourceTexture = DownloadHandlerTexture.GetContent(uwr);
                    Sprite sprite = Sprite.Create(
                        sourceTexture,
                        new Rect(0, 0, sourceTexture.width, sourceTexture.height),
                        new Vector2(0.5f, 0.5f) // 精灵的轴心点
                    );
                    img.sprite = sprite;
                }
            }
        }
    }

    #endregion

    #region Tool

    public Vector2 WorldToRect(Vector3 worldPos)
    {
        return ScreenToRectPos(WorldToScreen(worldPos));
    }

    private Vector3 WorldToScreen(Vector3 worldPos)
    {
        if (Camera.main != null) return Camera.main.WorldToScreenPoint(worldPos);
        return Vector3.zero;
    }

    private Vector2 ScreenToRectPos(Vector3 screenPos)
    {
        Vector2 tempVec2;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(mCanvas.GetComponent<RectTransform>(), screenPos,
            mCanvas.worldCamera, out tempVec2);
        return tempVec2;
    }

    #endregion
}