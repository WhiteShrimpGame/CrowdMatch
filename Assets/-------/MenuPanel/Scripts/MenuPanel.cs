using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

namespace MetaSystem.MenuPanel
{
    /// <summary>
    /// 主场景 菜单面板
    /// </summary>
    public class MenuPanel : MonoBehaviour
    {
        private List<MenuBtn> menuBtns = new List<MenuBtn>();
        private MenuBtn currentMenuBtn = null;
        public MenuBtn CurrentMenuBtn => currentMenuBtn;

        private static bool isFirstLoading = true;

        public void Init()
        {
            GetComponentsInChildren(menuBtns);
            foreach (var btn in menuBtns)
            {
                btn.Init(this, (v) => ChangeMenuBtn(v));
                btn.UnCheck(0f);
            }
            
            ChangeMenuBtn(MenuPanelBtnType.Home, 0f);

            isFirstLoading = false;

            CheckPanelOrder();
        }

        public void ChangeMenuBtn(MenuPanelBtnType btnType, float time = 0.25f)
        {
            currentMenuBtn = GetMenuBtn(btnType);
            menuBtns.ForEach(v =>
            {
                if (v != currentMenuBtn)
                    v.UnCheck(time);
            });
            currentMenuBtn.BeCheck(time);
        }

        public MenuBtn GetMenuBtn(MenuPanelBtnType btnType)
        {
            return menuBtns.FirstOrDefault(v => v.BtnType == btnType);
        }

        /// <summary>
        /// 检测面板显示顺序
        /// </summary>
        private void CheckPanelOrder()
        {
            /*var leaderBoardCompletedPanel = UIManager.Instance.GetComponentInChildren<LeaderBoardCompletedPanel>();
            if (leaderBoardCompletedPanel != null)
            {
                int selfIndex = transform.GetSiblingIndex();
                leaderBoardCompletedPanel.transform.SetSiblingIndex(selfIndex + 1);
            }*/
        }

        private void OnDisable()
        {
            Destroy(gameObject);
        }
    }
}