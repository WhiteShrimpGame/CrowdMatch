using CrowdMatch;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 重试界面
/// </summary>
public class FailTipPanel : MonoBehaviour
{
    [SerializeField] private Button backBtn;
    [SerializeField] private Button closeBtn;
    [SerializeField] private Transform starRoot;
    [SerializeField] private Text jdText;
    private int maxStarCount;
    private int curStarCount;

    public void Awake()
    {
        UIManager.IsPanelShow = true;
        backBtn.onClick.AddListener(OnBackBtnClick);
        closeBtn.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayButtonAudioAndVibrate();
            UIManager.Instance.showFailTipPanel(false);
        });
        //ShowWinStreak();
    }


    private void OnBackBtnClick()
    {
        AudioManager.Instance.PlayButtonAudioAndVibrate();
        
        gameObject.SetActive(false);
        GameData.FailCount++;
        GameData.WinStreak = 0;
        GameState.GameFail();
        if (StaminaSystemData.IsActive())
        {
            if (StaminaSystemData.IsInfiniteStamina)
            {
                StaminaSystemData.AddStamina(1,"unlimitedStamina");
            }
            StaminaSystemData.CostStamina(1);
        }
        GameManager.Instance.CleanupSpawnPool();
        UIManager.Instance.showGamePanel(false);
        UIManager.Instance.ShowMenuPanel(true);
        UIManager.Instance.showFailTipPanel(false);
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

    private void OnDisable()
    {
        Destroy(gameObject);
    }
}