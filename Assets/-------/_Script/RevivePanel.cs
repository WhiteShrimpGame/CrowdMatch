using System.Collections;
using System.Collections.Generic;
using CrowdMatch;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Spine.Unity;
using UnityEngine.EventSystems;


public class RevivePanel : MonoBehaviour,IPointerDownHandler, IPointerUpHandler
{
    /*public GameObject item1, item2;
    public Text reviveInfo;
    public GameObject btnTextUse;
    public GameObject btnVideo;
    public GameObject btnTextFree;

    public RectTransform moveRoot;
    public const float moveDistance = -860;*/

    private const float startX = -300f;
    private const float endX = 225f;
    private CanvasGroup mainCanvasGroup;
    private bool inCheckFailedCauseAni = false;
    /*public Transform rewardRoot;

    public int closeStatus;*/

    public Transform progBg;
    private RectTransform progItem;
    private Text progText;
    private Slider slider;
    public ScrollRect scrollRect;
    public float slideDuration = 0.5f;
    private bool isTip = false;
    public SkeletonGraphic spineGraphic;
    private void OnEnable()
    {
        /*if (GameData.IsFirstReview)
        {
            transform.Find("BG/CloseBtn").gameObject.SetActive(false);
            transform.Find("BG/VideoReviveBtn/AdIcon").gameObject.SetActive(false);
        }
        else*/
        {
            transform.Find("BG/CloseBtn").GetComponent<Button>().onClick.AddListener(_OnCloseBtnClk);
        }
        transform.Find("BG/ReviveBtn").GetComponent<Button>().onClick.AddListener(_OnVideoReviveBtnClk);
        mainCanvasGroup = GetComponent<CanvasGroup>();
        //int prog = GameData.LevelProgress;
        int prog = GameData.ClearedPixelCount * 100 / GameData.TotalPixelCount;

        progItem = (RectTransform)progBg.Find("Item");
        progText = progItem.Find("ProgText").GetComponent<Text>();

        progItem.DOAnchorPosX(startX + (endX - startX) * prog / 100,
            Mathf.Max(0.6f, 1.4f * prog / 100)).SetEase(Ease.OutQuad);


    }

    private void OnDisable()
    {
        Destroy(gameObject);
    }

    private void Update()
    {
        if (progBg != null)
        {
            progText.text =
                Mathf.RoundToInt((progItem.anchoredPosition.x - startX) * 100 / (endX - startX)).ToString() + "%";
            //slider.value = (progItem.anchoredPosition.x - startX) / (endX - startX);
        }
    }
    /// <summary>
    /// 水平滑动到目标位置 (0 ~ 1)
    /// 0 = 最左
    /// 1 = 最右
    /// </summary>
    public void SlideToPosition(float targetPos)
    {
        // 用 DOTween 模拟水平滑动
        DOTween.To(
            () => scrollRect.horizontalNormalizedPosition, // 从当前值
            x => scrollRect.horizontalNormalizedPosition = x, // 赋值
            targetPos, // 目标值
            slideDuration // 时长
        ).OnComplete(() =>
        {
            DOVirtual.DelayedCall(0.5f, () =>
            {
                spineGraphic.AnimationState.SetAnimation(0,"idle",false);
            });
        });
    }

    // 快捷方法
    public void SlideToLeft() => SlideToPosition(0);
    public void SlideToRight() => SlideToPosition(1);
    public void _OnCloseBtnClk()
    {
        if (inCheckFailedCauseAni) return;
        AudioManager.Instance.PlayButtonAudioAndVibrate();
        if (!isTip&&StaminaSystemData.IsActive())
        {
            isTip = true;
            SlideToRight();
            return;
        }
        Close();
    }

    private void Close()
    {
        UIManager.Instance.HidePanel(transform, () =>
        {
            if (GameData.WinStreak > 0)
            {
                UIManager.Instance.ShowRetryPanel(true);
            }
            else
            {
                UIManager.Instance.showFailPanel(true);
            }
            
        });
    }

    public void _OnVideoReviveBtnClk()
    {
        if (inCheckFailedCauseAni) return;
        AudioManager.Instance.PlayButtonAudioAndVibrate();
        /*AudioManager.Instance.playClip(1);
        GameManager.Instance.TriggerVibrate(1);*/

        /*if (GameData.IsFirstReview)
        {
            UIManager.Instance.HidePanel(transform, () =>
            {
                GameController.Instance.ReviveGame("0");
                GameData.IsFirstReview=false;
                UIManager.IsPanelShow = false;
            });
        }
        else*/
        {
            Debug.Log("激励回调成功, 发放奖励");
            //GameData.AdCount++;
            //GameData.ReviveAdCount++;
            UIManager.Instance.HidePanel(transform, () =>
            {
                //GameController.Instance.ReviveGame();
                GameController.Instance.DoRevive();
                UIManager.IsPanelShow = false;
            });
        }
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        if (RectTransformUtility.RectangleContainsScreenPoint(GetComponent<RectTransform>(), eventData.position,
                eventData.enterEventCamera))
        {
            inCheckFailedCauseAni = true;
            mainCanvasGroup.DOKill();
            mainCanvasGroup.DOFade(0f, 0.15f);
            
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (RectTransformUtility.RectangleContainsScreenPoint(GetComponent<RectTransform>(), eventData.position,
                eventData.enterEventCamera))
        {
            mainCanvasGroup.DOKill();
            mainCanvasGroup.DOFade(1f, 0.1f);
            inCheckFailedCauseAni = false;
        }
    }
}