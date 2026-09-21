using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using CrowdMatch;
using WsGame.DailyBouns;

/// <summary>
/// 胜利界面
/// </summary>
public class WinPanel : MonoBehaviour
{
    [SerializeField] private Button getBtn_Normal;
    [SerializeField] private Button getBtn_AD;
    [SerializeField] private Text goldCountText;
    public Text goldText;
    [SerializeField] private CoinTweenPanel coinTween;

    [SerializeField] private Transform streakRoot;
    [SerializeField] private Transform starRoot;
    [SerializeField] private Transform extra;

    [SerializeField] private RectTransform coinAniStartPosTran;
    [SerializeField] private RewardEffect rewardEffect;
    [SerializeField] private Text jdText;
    private Transform coinTweenStartRoot;
    private RewardData rewardData;

    private int multiCount;



    private void OnEnable()
    {
        GameState.GameWin();
        UIManager.IsPanelShow = true;

        //AudioManager.Instance.playClip(2);
        AudioManager.Instance.Play("Win");
        getBtn_Normal.onClick.AddListener(OnGetNormalBtnClick);
        getBtn_AD.onClick.AddListener(OnGetADBtnClick);

        rewardData = new RewardData
        {
            gold = GoldConfig.GetWinGold()
        };
        multiCount = GoldConfig.GetWinGoldMultiCount();

        goldText.text = ((float)GameData.Gold.Count - rewardData.gold).ConvertToKMGString();

        goldCountText.text = rewardData.gold.ToString();

        
        var bg = transform.Find("BG");
        bg.localScale = Vector3.one;
        bg.DOScale(0.4f, 0.4f).From().SetEase(Ease.OutBack).OnComplete(() => { });
        
        //CheckShowRecommend();

        ShowWinStreak();
        //UIManager.Instance.HideBanner();
    }

    /// <summary>
    /// 普通奖励按钮点击事件
    /// </summary>
    private void OnGetNormalBtnClick()
    {
        AudioManager.Instance.PlayButtonAudioAndVibrate();
        OnAddReward();
    }

    /// <summary>
    /// AD奖励按钮点击事件
    /// </summary>
    private void OnGetADBtnClick()
    {
        AudioManager.Instance.PlayButtonAudioAndVibrate();
        OnAddReward(multiCount, "1");
        /*MiniGameSolution.Ad.ShowRewardAd(() =>
        {
            //GameData.AdCount++;
            Debug.Log("激励回调成功, 发放奖励");

            OnAddReward(multiCount, "1");
        }, sceneId: "PassRewards");*/
    }

    private void Continue()
    {
        GameManager.Instance.ReloadLevel();
        UIManager.Instance.HidePanel(transform);
        //下一关
        /*{

            if (GameData.IsFlowLevelWin)
            {
                UIManager.IsPanelShow = false;
                GameManager.Instance.ReloadScene();
            }
            else
            {
                GameManager.Instance.ReloadScene(false);
            }
        }*/
    }

    public void OnAddReward(int multiple = 1, string type = "0")
    {
        //Continue();
        getBtn_AD.interactable = false;
        getBtn_Normal.interactable = false;

        int lastGold = GameData.Gold.Count - rewardData.gold;
        if (multiCount > 1)
        {
            rewardData.AddReward(multiple - 1, way: "Level", type: type);
        }
        UIManager.Instance.gameInnerUI.RefreshGoldCount();
        int curGold = GameData.Gold.Count;
        if (curGold > lastGold)
        {
            //DOVirtual.DelayedCall(0.6f, () => { RewardTips.CoinSE(); });
            ShowCoinTween(lastGold, curGold, () =>
            {
                /*if (MiniGameSolution.Ad.IsShowInterstitial && GameData.IsInterstitialShowAfterClose &&
                    !MiniGameSolution.Ad.SkipShowInterstitial(GameData.CurrentLevel - 1, 1))
                {
                    MiniGameSolution.Instance.ShowMask();
                    bool isShow = false;
                    DOVirtual.DelayedCall(3, () =>
                    {
                        if (!isShow)
                        {
                            isShow = true;
                            MiniGameSolution.Instance.HideMask();
                            DOVirtual.DelayedCall(0.2f, Continue);
                        }
                    });
                    MiniGameSolution.Ad.ShowInterstitial((res) =>
                    {
                        if (!isShow)
                        {
                            isShow = true;
                            MiniGameSolution.Instance.HideMask();
                            DOVirtual.DelayedCall(0.2f, Continue);
                        }
                    });
                }
                else
                {
                    DOVirtual.DelayedCall(0.2f, Continue);
                }*/
                DOVirtual.DelayedCall(0.2f, Continue);
            });
        }

        PlayerPrefs.Save();

        /*if (GameData.CurrentLevel == 4 && MiniGameSolution.Utilities.HasReview())
        {
            MiniGameSolution.Utilities.OpenReview();
        }*/
    }

    private void ShowCoinTween(int lastCount, int newCount, System.Action onComplete = null)
    {
        var start = coinTween.transform.Find("Image");
        var end = coinTween.transform.Find("CoinImg");

        if (coinTweenStartRoot != null)
        {
            start.parent = coinTweenStartRoot;
            start.localPosition = Vector3.zero;
        }

        coinTween.ShowCoins(coinAniStartPosTran.position, end, 30, 1,
            () =>
            {
                DOVirtual.Int(lastCount, newCount, 0.5f,
                        (val) =>
                        {
                            goldText.text = ((float)val).ConvertToKMGString();
                        })
                    .SetEase(Ease.Linear).OnComplete(() => { onComplete?.Invoke(); });
            }, duration: 1);
    }

    

    private void ShowWinStreak()
    {
        var curCount = GameData.WinStreak<5?GameData.WinStreak:5;
        jdText.text = curCount + "/5";
        for (int i = 0; i < curCount; i++)
        {
            starRoot.GetChild(i).gameObject.SetActive(true);
        }
    }

    /*/// <summary>
    /// 检测是否显示推荐弹窗
    /// </summary>
    private void CheckShowRecommend()
    {
        if (MiniGameSolution.Utilities.IsSupportRecommend())
        {
            Reporter.ShowRecommendPanel();
            MiniGameSolution.Utilities.ShowRecommend((v) =>
            {
                GameData.ShowRecommendCount++;
                GameData.RecommendCompleted = v ? 1 : 0;
                if (v) Reporter.RecommendCompleted();

                Debug.LogError($"当前完成关卡：{GameData.CurrentLevel}");
                Debug.LogError($"显示弹窗次数：{GameData.ShowRecommendCount}");
                Debug.LogError($"弹窗完成状态：{GameData.RecommendCompleted}");

                MiniGameSolution.Utilities.PlayerPrefs.Save();
            });
        }
    }*/



    private void OnDisable()
    {
        UIManager.Instance.gameInnerUI.RefreshGoldCount();
        Destroy(gameObject);
    }
}