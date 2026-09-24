using CrowdMatch;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 重试界面
/// </summary>
public class WinStreakPanel : MonoBehaviour
{
    [SerializeField] private Button backBtn;
    [SerializeField] private Button backBtn2;
    [SerializeField] private Transform starRoot;
    [SerializeField] private Text jdText;
    private bool inCheckFailedCauseAni = false;
    private int maxStarCount;
    private int curStarCount;

    public void Show()
    {
        UIManager.IsPanelShow = true;
        backBtn.onClick.AddListener(OnBackBtnClick);
        backBtn2.onClick.AddListener(OnBackBtnClick);
        UIManager.Instance.ShowPanel(transform);
        ShowWinStreak();
    }
    
    private void OnBackBtnClick()
    {
        AudioManager.Instance.PlayButtonAudioAndVibrate();
        UIManager.Instance.HidePanel(transform);
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