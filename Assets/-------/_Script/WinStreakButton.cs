using System.Collections;
using System.Collections.Generic;
using CrowdMatch;
using UnityEngine;
using UnityEngine.UI;
using WsGame.DailyBouns;

public class WinStreakButton : MonoBehaviour
{

    [SerializeField] private GameObject winStreakPanelPrefab;
    private Button m_Btn;
    //private Transform tipIcon;
    private WinStreakPanel m_WinStreakPanel;

    private void Awake()
    {
        m_Btn = GetComponent<Button>();
        //tipIcon = transform.LFirstOrDefault<Transform>("TipIcon");
        m_Btn.onClick.AddListener(OnBtnClick);
        //ShowTip();
    }

    /// <summary>
    /// 点击按钮
    /// </summary>
    private void OnBtnClick()
    {
        AudioManager.Instance.PlayButtonAudioAndVibrate();
        //tipIcon.Hide();
        ShowWinStreakPanel();
    }

    private void ShowWinStreakPanel()
    {
        GameObject dailyBoundPanelObj = Instantiate(winStreakPanelPrefab, UIManager.Instance.transform);
        dailyBoundPanelObj.name = winStreakPanelPrefab.name;
        m_WinStreakPanel = dailyBoundPanelObj.GetComponent<WinStreakPanel>();
        m_WinStreakPanel.Show();
    }

    public void ShowTip()
    {
        
    }
}
