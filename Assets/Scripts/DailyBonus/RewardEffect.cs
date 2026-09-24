using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace WsGame.DailyBouns
{
    /// <summary>
    /// 获得奖励飘字效果（图标上浮 + 渐隐）
    /// </summary>
    public class RewardEffect : MonoBehaviour
    {
        private Image icon;
        private Text countText;
        private CanvasGroup m_CanvasGroup;
        private RectTransform rectTrans;

        public float offestY = 220f;

        /// <summary>图标加载回调（宿主注入：根据 rewardType 设置 icon.sprite）</summary>
        public Action<Image, int> iconResolver;

        /// <summary>全局图标加载回调（推荐：在 Bootstrap 中注入一次，所有实例共享）</summary>
        public static Action<Image, int> GlobalIconResolver;

        public void Show(Transform trans, int rewardType, Sprite sprite, float count)
        {
            if (icon == null)
                icon = transform.Find("Icon").GetComponent<Image>();
            if (countText == null)
                countText = transform.Find("CountText").GetComponent<Text>();
            if (m_CanvasGroup == null)
                m_CanvasGroup = GetComponent<CanvasGroup>();
            if (rectTrans == null)
                rectTrans = GetComponent<RectTransform>();

            SetIcon(sprite, rewardType);

            string value = FormatKMG(count);
            countText.text = "+" + value;
            m_CanvasGroup.alpha = 1.0f;

            rectTrans.DOAnchorPosY(rectTrans.anchoredPosition.y + offestY, 1.5f).SetEase(Ease.OutCubic)
                .OnComplete(() => gameObject.SetActive(false));
            DOVirtual.Float(1f, 0f, 1.5f, (v) => { m_CanvasGroup.alpha = v; }).SetEase(Ease.InCubic);
        }

        public void SetIcon(Sprite sprite, int rewardType)
        {
            if (GlobalIconResolver != null)
            {
                GlobalIconResolver(icon, rewardType);
                return;
            }
            if (iconResolver != null)
            {
                iconResolver(icon, rewardType);
                return;
            }
            if (sprite != null)
                icon.sprite = sprite;
        }

        /// <summary>
        /// 数字格式化为 K/M/G 缩写（提取自 ConvertToKMGString，仅保留本模块所需）
        /// </summary>
        internal static string FormatKMG(float value)
        {
            if (value >= 1000000000f) return (value / 1000000000f).ToString("0.#") + "G";
            if (value >= 1000000f) return (value / 1000000f).ToString("0.#") + "M";
            if (value >= 1000f) return (value / 1000f).ToString("0.#") + "K";
            return value.ToString("0");
        }
    }
}
