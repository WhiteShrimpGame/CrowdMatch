using CrowdMatch;
using DG.Tweening;

using UnityEngine;
using UnityEngine.UI;

public class FailPanel : MonoBehaviour
{
    [SerializeField] private Button backHomeBtn;
    [SerializeField] private Button restartBtn;
    //[SerializeField] private Button adBtn;

    private RectTransform progItem;
    private Text progText;

    private const float startX = -284f;
    private const float endX = 194f;

    private void OnEnable()
    {
        /*if (StaminaSystemData.IsActive())
        {
            if (StaminaSystemData.IsInfiniteStamina)
            {
                Reporter.StaminaGet(1,"unlimitedStamina");
            }
            StaminaSystemData.CostStamina(1);
            Reporter.StaminaCost(1,"gameFail");
        }*/
        
        UIManager.IsPanelShow = true;

        //AudioManager.Instance.playClip(3);

        // progItem = (RectTransform)transform.Find("FailProg/Item");
        // progText = progItem.Find("ProgText").GetComponent<Text>();
        //
        // if (GameData.TotalTapeCount > 0)
        // {
        //     int prog = GameData.LevelProgress;
        //     progItem.DOAnchorPosX(startX + (endX - startX) * prog / 100,
        //         Mathf.Max(0.8f, 2f * prog / 100)).SetEase(Ease.InOutQuad);
        // }

        restartBtn.onClick.AddListener(OnRestartBtnClick);
        backHomeBtn.onClick.AddListener(OnBackHomeBtnClick);

        /*if (GameController.Instance.isStreakActive && GameData.WinStreak > 0)
        {
            Reporter.VideoShow("KeepWin");
            adBtn.onClick.AddListener(OnAdBtnClick);
        }
        else
        {
            adBtn.gameObject.SetActive(false);
        }*/

        GameData.FailCount++;

/*#if !UNITY_EDITOR
        if (GameData.LevelProgress >= 30)
#endif
        {
            GameData.RetryCount++;
        }*/

        var bg = transform.Find("BG");
        bg.localScale = Vector3.one;
        bg.DOScale(0.4f, 0.4f).From().SetEase(Ease.OutBack).OnComplete(() => { });
        

        UIManager.Instance.HideBanner();
    }

    void Update()
    {
        // progText.text = Mathf.RoundToInt((progItem.anchoredPosition.x - startX) * 100 / (endX - startX)).ToString() +
        //                 "%";
    }

    /// <summary>
    /// 重新开始按钮点击事件
    /// </summary>
    private void OnRestartBtnClick()
    {
        
        /*if (StaminaSystemData.IsActive()&&!StaminaSystemData.HasEnoughStamina(1))
        {
            UIManager.Instance.ShowStaminaPanel(true); // 弹补充体力弹窗
            return;
        }
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
        }*/

        UIManager.IsPanelShow = false;
        AudioManager.Instance.PlayButtonAudioAndVibrate();
        GameController.Instance.ReloadLevel();
        UIManager.Instance.showFailPanel(false);
    }

    /// <summary>
    /// 返回主界面按钮点击事件
    /// </summary>
    private void OnBackHomeBtnClick()
    {
        /*if (GameController.Instance.isStreakActive && GameData.WinStreak > 0)
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

        AudioManager.Instance.PlayButtonAudioAndVibrate();
        //UIManager.Instance.showFailPanel(false);
        //UIManager.Instance.showMainPanel(true);
        //GameData.IsGaming = false;
        Reporter.TapeUpCount();
        GameManager.Instance.ReloadScene(false);*/
    }

    /*private void OnAdBtnClick()
    {
        AudioManager.Instance.PlayButtonAudioAndVibrate();
        PufferMiniGame.MiniGameSolution.Ad.ShowRewardAd(() =>
        {
            Debug.Log("激励回调成功, 发放奖励");
            Debug.Log("保存连胜");
            UIManager.IsPanelShow = false;
            Reporter.TapeUpCount();
            Reporter.GameRetry();
            GameManager.Instance.ReloadScene();
            UIManager.Instance.showRacePopupPanel(false);
            UIManager.Instance.showRacePopupWinPanel(false);
        }, sceneId: "KeepWin");
        
    }*/

    private void OnDisable()
    {
        Destroy(gameObject);
    }
}