using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace MetaSystem.MenuPanel
{
    public class MenuBtn_MainPanel : MenuBtn
    {
        private RectTransform mRect;

        public override void Init(MenuPanel _menuPanel, UnityAction<MenuPanelBtnType> callback)
        {
            base.Init(_menuPanel, callback);
            BtnType = MenuPanelBtnType.Home;
            mRect = GetComponent<RectTransform>();
        }

        /// <summary>
        /// 被选中
        /// </summary>
        public override void BeCheck(float time = 0.3f)
        {
            base.BeCheck(time);
            UIManager.Instance.showMainPanel(true);
            bindPanel = UIManager.Instance.GetComponentInChildren<MainPanel>()?.gameObject;
        }

        /// <summary>
        /// 取消选中
        /// </summary>
        public override void UnCheck(float time = 0.3f)
        {
            base.UnCheck(time);
            if (bindPanel != null)
                bindPanel.Hide();
        }
    }
}