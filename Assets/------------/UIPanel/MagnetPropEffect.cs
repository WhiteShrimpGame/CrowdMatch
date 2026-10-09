using CrowdMatch;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace WsGame.HooksJam
{
    /// <summary>
    /// 磁铁道具效果
    /// </summary>
    public class MagnetPropEffect : MonoBehaviour
    {
        private Image mImg;
        private RectTransform mRect;
        private Sequence aniQuence;
        private Vector3 startPos;
        private bool isInit = false;

        [SerializeField] private float singletime = 0.06f;
        [SerializeField] private float posOffset = 15f;

        ParticleSystem VFX;

        // 原来这里有一句占位的 Start() → Show(new Vector2(0,750), facingAngleOffset)。
        // 它会在生成后的下一帧把外部传进来的 Show(位置, 朝向) 覆盖掉（Start 晚于外部那次调用），
        // 所以删掉：位置与朝向一律由调用方（GameInnerUI.ShowMagnetPropEffect）给。

        private void Init()
        {
            mImg = GetComponent<Image>();
            mRect = GetComponent<RectTransform>();
            startPos = mRect.anchoredPosition;
            isInit = true;
            VFX = GetComponentInChildren<ParticleSystem>(true);
            VFX.Show();
        }

        public void Show(Vector2 target, float faceAngle = 0f)
        {
            if (!isInit) Init();

            mRect.DOKill();
            mImg.DOKill();
            aniQuence.Kill();

            mRect.anchoredPosition = target;
            startPos = mRect.anchoredPosition;
            // 朝向**按调用方给的角度直接用**（不再叠加预制体上的偏移量）：
            // 现在只有磁铁一处调用，角度是固定值，由 GameInnerUI 传进来。
            mRect.localEulerAngles = new Vector3(0f, 0f, faceAngle);

            gameObject.SetActive(true);
            VFX.Play();
            mImg.DOFade(1f, 0.2f).OnComplete(delegate { ShowAni(); });
        }

        public void ShowAni()
        {
            if (!isInit) Init();
            AudioManager.Instance.Play("Magnet");
            startPos = mRect.anchoredPosition;
            mRect.DOKill();
            mImg.DOKill();
            aniQuence.Kill();
            VFX.Play();
            aniQuence = DOTween.Sequence();
            aniQuence.Append(mRect.DOAnchorPos(startPos + mRect.right * posOffset, singletime).SetEase(Ease.Linear));
            aniQuence.Append(
                mRect.DOAnchorPos(startPos - mRect.right * posOffset, singletime * 2f).SetEase(Ease.Linear));
            aniQuence.Append(
                mRect.DOAnchorPos(startPos + mRect.right * posOffset, singletime * 2f).SetEase(Ease.Linear));
            aniQuence.Append(
                mRect.DOAnchorPos(startPos - mRect.right * posOffset, singletime * 2f).SetEase(Ease.Linear));
            aniQuence.Append(mRect.DOAnchorPos(startPos, singletime).SetEase(Ease.Linear));
            aniQuence.SetLoops(-1);
        }

        public void Hide()
        {
            mRect.DOKill();
            mImg.DOKill();
            aniQuence.Kill();
            mImg.DOFade(0f, 0.3f).OnComplete(() =>
            {
                gameObject.SetActive(false);
                VFX.Stop();
            });
            Destroy(gameObject);
        }
    }
}
