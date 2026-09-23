using CrowdMatch;
using DG.Tweening;

using UnityEngine;
using UnityEngine.UI;

public class FailPanel : MonoBehaviour
{
    [SerializeField] private Button backHomeBtn;
    [SerializeField] private Button restartBtn;
    //[SerializeField] private Button adBtn;
    [SerializeField] private Transform progBg;
    private RectTransform progItem;
    private Text progText;

    private const float startX = -300f;
    private const float endX = 225f;

    private void OnEnable()
    {
        GameState.GameFail();
        if (StaminaSystemData.IsActive())
        {
            if (StaminaSystemData.IsInfiniteStamina)
            {
                //Reporter.StaminaGet(1,"unlimitedStamina");
            }
            StaminaSystemData.CostStamina(1);
            //Reporter.StaminaCost(1,"gameFail");
        }
        //暂时默认没有保存连胜
        GameData.WinStreak = 0;
        UIManager.IsPanelShow = true;

        //AudioManager.Instance.playClip(3);
        
        progItem = (RectTransform)progBg.Find("Item");
        progText = progItem.Find("ProgText").GetComponent<Text>();
        int prog = GameData.ClearedPixelCount * 100 / GameData.TotalPixelCount;
        /*progItem.DOAnchorPosX(startX + (endX - startX) * prog / 100,
            Mathf.Max(0.6f, 1.4f * prog / 100)).SetEase(Ease.OutQuad);*/
        float targetX = startX + (endX - startX) * prog / 100;
        Vector2 pos = progItem.anchoredPosition;
        pos.x = targetX;
        progItem.anchoredPosition = pos;
        progText.text = Mathf.RoundToInt(prog) + "%";
        //progText.text = Mathf.RoundToInt((progItem.anchoredPosition.x - startX) * 100 / (endX - startX)).ToString() + "%";
        
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
        AudioManager.Instance.PlayButtonAudioAndVibrate();
        if (StaminaSystemData.IsActive()&&!StaminaSystemData.HasEnoughStamina(1))
        {
            UIManager.Instance.ShowStaminaPanel(true); // 弹补充体力弹窗
            return;
        }
        /*if (GameController.Instance.isStreakActive && GameData.WinStreak > 0)
        {
            Reporter.WinStreakFail();
        }*/
        GameData.WinStreak = 0;
        UIManager.IsPanelShow = false;
        GameController.Instance.ReloadLevel();
        UIManager.Instance.showFailPanel(false);
    }

    /// <summary>
    /// 返回主界面按钮点击事件
    /// </summary>
    private void OnBackHomeBtnClick()
    {
        AudioManager.Instance.PlayButtonAudioAndVibrate();
        UIManager.Instance.showGamePanel(false);
        UIManager.Instance.ShowMenuPanel(true);
        UIManager.Instance.showFailPanel(false);
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