using UnityEngine;
using UnityEngine.UI;

namespace WsGame.DailyBouns
{
    /// <summary>
    /// 每日签到入口按钮（红点提示 + 打开面板）
    /// </summary>
    public class DailyBonusBtn : MonoBehaviour
    {
        [SerializeField] private GameObject dailyBoundPanelPrefab;
        private Button m_Btn;
        private Transform tipIcon;
        private DailyBonusPanel m_DailyBonusPanel;

        private void Awake()
        {
            m_Btn = GetComponent<Button>();
            tipIcon = transform.Find("TipIcon");

            m_Btn.onClick.AddListener(OnBtnClick);
            ShowTip();
        }

        private void OnBtnClick()
        {
            tipIcon.gameObject.SetActive(false);
            ShowDailyBounsPanel();
        }

        private void ShowDailyBounsPanel()
        {
            GameObject dailyBoundPanelObj = Instantiate(dailyBoundPanelPrefab, transform.root);
            dailyBoundPanelObj.name = dailyBoundPanelPrefab.name;
            m_DailyBonusPanel = dailyBoundPanelObj.GetComponent<DailyBonusPanel>();
            m_DailyBonusPanel.Show();
        }

        /// <summary>
        /// 刷新红点显示（建议在每次打开主界面时调用）
        /// </summary>
        public void ShowTip()
        {
            tipIcon.gameObject.SetActive(DailyBounsData.TodayBounsRewardCanGet());
        }
    }
}
