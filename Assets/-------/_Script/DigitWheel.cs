using UnityEngine;
using DG.Tweening;

public class DigitWheel : MonoBehaviour
{
    [Header("滚轮配置")]
    public RectTransform content;
    public float cellHeight;
    public float rollDuration = 0.3f;
    public Ease ease = Ease.OutCubic;

    private Tween _tween;
    private int _currentDigit;

    /// <summary>
    /// 滚动到目标数字
    /// isLoop=true：循环特殊跳转（9→0 或者 0→9）
    /// isUpScroll：true向上滚；false向下滚
    /// </summary>
    public void RollTo(int targetDigit, bool isLoop, bool isUpScroll)
    {
        if (_tween != null)
        {
            _tween.Kill();
        }

        float startY = content.anchoredPosition.y;
        float targetY;

        if (isLoop)
        {
            if (isUpScroll)
            {
                //【正向进位：9 → 0】向上滚，从9(y=9H) → 末尾0(y=10H)
                targetY = 10 * cellHeight;
                _tween = content.DOAnchorPosY(targetY, rollDuration)
                    .SetEase(ease)
                    .OnComplete(() =>
                    {
                        //动画结束，切回头部0 y=0
                        content.anchoredPosition = new Vector2(content.anchoredPosition.x, 0 * cellHeight);
                        _currentDigit = targetDigit;
                    });
            }
            else
            {
                //【反向借位：0 → 9】
                //第一步：瞬间把Content从头部0(y=0) → 末尾0(y=10H)，文字相同看不见跳
                content.anchoredPosition = new Vector2(content.anchoredPosition.x, 10 * cellHeight);
                //第二步：向下动画，从10H滑到9H，慢慢看到9
                targetY = 9 * cellHeight;
                _tween = content.DOAnchorPosY(targetY, rollDuration)
                    .SetEase(ease)
                    .OnComplete(() =>
                    {
                        _currentDigit = targetDigit;
                    });
            }
        }
        else
        {
            //普通数字滚动，非循环
            targetY = targetDigit * cellHeight;
            _tween = content.DOAnchorPosY(targetY, rollDuration)
                .SetEase(ease)
                .OnComplete(() =>
                {
                    _currentDigit = targetDigit;
                });
        }
    }

    public void SetDigitImmediate(int digit)
    {
        if (_tween != null)
        {
            _tween.Kill();
        }
        _currentDigit = digit;
        content.anchoredPosition = new Vector2(content.anchoredPosition.x, digit * cellHeight);
    }

    private void OnDestroy()
    {
        if (_tween != null)
        {
            _tween.Kill();
        }
    }
}
