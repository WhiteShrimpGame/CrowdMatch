using CrowdMatch;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MetaSystem.MenuPanel
{
    /// <summary>
    /// 主场景菜单面板按钮类型
    /// </summary>
    public enum MenuPanelBtnType
    {
        LeaderBoard,
        Home,
        Shop,
        Collect,
        HandBook,
        BeadPixelSystem,
    }

    /// <summary>
    /// 菜单面板按钮
    /// </summary>
    public class MenuBtn : MonoBehaviour
    {
        public MenuPanelBtnType BtnType;

        protected static readonly int HsvSaturation = Shader.PropertyToID("_HsvSaturation");

        [SerializeField] protected Image bgImg;
        [SerializeField] protected Image iconImg;
        [SerializeField] protected Text titleText;

        protected MenuPanel menuPanel;
        protected Button m_Btn;
        protected GameObject bindPanel;

        /// <summary>
        /// 按钮点击回调
        /// </summary>
        [HideInInspector] public UnityEvent<MenuPanelBtnType> OnClickCallBack = new UnityEvent<MenuPanelBtnType>();

        public virtual void Init(MenuPanel _menuPanel, UnityAction<MenuPanelBtnType> callback)
        {
            menuPanel = _menuPanel;
            m_Btn = GetComponent<Button>();
            OnClickCallBack.AddListener(callback);
            m_Btn.onClick.AddListener(OnBtnClick);
        }

        public virtual void OnBtnClick()
        {
            if (menuPanel.CurrentMenuBtn == this) return;
            AudioManager.Instance.PlayButtonAudioAndVibrate();
            OnClickCallBack?.Invoke(BtnType);
        }

        /// <summary>
        /// 被选中
        /// </summary>
        public virtual void BeCheck(float time = 0.3f)
        {
            m_Btn.interactable = false;
            iconImg.rectTransform.DOKill();
            bgImg.DOKill();
            bgImg.rectTransform.DOScale(Vector3.one, time);
            bgImg.DOFade(1.0f, time);
            iconImg.rectTransform.DOAnchorPosY(82f, time);
            iconImg.rectTransform.DOScale(Vector3.one * 0.9f, time);
            titleText.DOKill();
            transform.DOKill();
            titleText.DOFade(1.0f, time);
        }

        /// <summary>
        /// 取消选中
        /// </summary>
        public virtual void UnCheck(float time = 0.3f)
        {
            m_Btn.interactable = true;
            iconImg.rectTransform.DOKill();
            iconImg.rectTransform.DOAnchorPosY(-30f, time);
            iconImg.rectTransform.DOScale(Vector3.one * 0.7f, time);
            bgImg.DOKill();
            bgImg.rectTransform.DOScale(Vector3.zero, time);
            bgImg.DOFade(0f, time);
            titleText.DOKill();
            transform.DOKill();
            titleText.DOFade(0.0f, time / 2f).SetEase(Ease.OutQuad);
        }

        protected virtual void OnDestroy()
        {
            OnClickCallBack.RemoveAllListeners();
            if (bindPanel != null)
                bindPanel.Hide();
        }
    }
}