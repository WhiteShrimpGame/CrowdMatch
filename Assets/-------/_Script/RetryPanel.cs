using System;
using CrowdMatch;
using DG.Tweening;

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 重试界面
/// </summary>
public class RetryPanel : MonoBehaviour
{
    //[SerializeField] private Button continueBtn;
    [SerializeField] private Button reviveBtn;
    [SerializeField] private Button backBtn;
    [SerializeField] private Transform starRoot;
    [SerializeField] private Text jdText;
    //[SerializeField] private GameObject[] jdxList;
    private bool isRevive;

    //private Transform starPosRoot;
    private int maxStarCount;
    private int curStarCount;

    public void Show(bool revive)
    {
        UIManager.IsPanelShow = true;

        isRevive = revive;

        if (isRevive)
        {
            reviveBtn.onClick.AddListener(OnReviveBtnClick);
            //continueBtn.gameObject.SetActive(false);
        }
        else
        {
            //continueBtn.onClick.AddListener(OnContinueBtnClick);
            reviveBtn.gameObject.SetActive(false);
        }

        backBtn.onClick.AddListener(OnBackBtnClick);

        UIManager.Instance.ShowPanel(transform);

        ShowWinStreak();
        //UIManager.Instance.HideBanner();
    }

    /*private void OnContinueBtnClick()
    {
        AudioManager.Instance.PlayButtonAudioAndVibrate();
        UIManager.Instance.HidePanel(transform, () =>
        {
            GameState.GameStart();
            UIManager.Instance.ShowBanner();
        });
    }*/

    private void OnReviveBtnClick()
    {
        AudioManager.Instance.PlayButtonAudioAndVibrate();
        GameController.Instance.DoRevive();
        Debug.Log("复活");
        UIManager.Instance.HidePanel(transform);
        /*MiniGameSolution.Ad.ShowRewardAd(() =>
        {
            Debug.Log("激励回调成功, 发放奖励");
            GameData.AdCount++;
            GameController.Instance.DoRevive();
            Debug.Log("复活");
            UIManager.Instance.HidePanel(transform);
        }, sceneId: "Revive");*/
        
    }

    private void OnBackBtnClick()
    {
        AudioManager.Instance.PlayButtonAudioAndVibrate();
        if (isRevive)
        {
            gameObject.SetActive(false);
            UIManager.Instance.showFailPanel(true);
        }
        else
        {
            gameObject.SetActive(false);
            
            GameData.FailCount++;
            GameData.WinStreak = 0;
            /*GameData.StratifPlayerData.CheckJudge(false);
            if (GameController.Instance.isStreakActive && GameData.WinStreak > 0)
            {
                Reporter.WinStreakFail();
            }

            GameData.WinStreak = 0;

            if (AvatarData.IsUnlock())
            {
                AvatarData.Data.CurrentWinStreak = 0;
                AvatarData.Data.TotalPlayCount++;
                AvatarData.Save();
            }

#if !UNITY_EDITOR
        if (GameData.LevelProgress >= 30)
#endif
            {
                GameData.RetryCount++;
            }

            WS_TapAway_Cloud.LevelRecord.ClearLevelRecord();
            GameManager.Instance.ReloadScene(false);*/
        }
    }

    private void ShowWinStreak()
    {
        var curCount = GameData.WinStreak<5?GameData.WinStreak:5;
        jdText.text = curCount + "/5";
        for (int i = 0; i < curCount; i++)
        {
            starRoot.GetChild(i).gameObject.SetActive(true);
        }
        /*var activeCount = GameData.WinStreakRewardCount;
        switch (activeCount)
        {
            case 3:
                starPosRoot = starRoot.GetChild(0);
                break;
            case 5:
                starPosRoot = starRoot.GetChild(1);
                break;
            case 7:
                starPosRoot = starRoot.GetChild(2);
                break;
            default:
                starPosRoot = starRoot.GetChild(0);
                break;
        }

        starPosRoot.gameObject.SetActive(true);*/

        /*if (curCount > 5)
        {
            maxStarCount = 5 - 1;
        }
        else
        {
            maxStarCount = curCount - 1;
        }

        for (int i = maxStarCount + 1; i < 5; i++)
        {
            starPosRoot.GetChild(i).GetChild(0).gameObject.SetActive(false);
        }

        curStarCount = maxStarCount;
        DOVirtual.DelayedCall(0.4f, WinStreakLoop);*/
    }

    /*private void WinStreakLoop()
    {
        if (curStarCount < 0)
        {
            curStarCount = maxStarCount;
            DOVirtual.DelayedCall(0.5f, () =>
            {
                for (int i = 0; i <= maxStarCount; i++)
                {
                    var curStar = starPosRoot.GetChild(i).GetChild(0);
                    curStar.gameObject.SetActive(true);
                    curStar.localScale = Vector3.one;
                }

                DOVirtual.DelayedCall(0.2f, () => { WinStreakLoop(); });
            });
            return;
        }

        var curStar = starPosRoot.GetChild(curStarCount).GetChild(0);

        curStar.DOScale(Vector3.one * 1.1f, 0.1f).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            curStar.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InQuad).OnComplete(() =>
            {
                curStarCount--;
                WinStreakLoop();
            });
        });
    }*/

    private void OnDisable()
    {
        Destroy(gameObject);
    }
}