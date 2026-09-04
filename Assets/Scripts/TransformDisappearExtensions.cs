using UnityEngine;
using DG.Tweening;
using System;

/// <summary>
/// DOTween-based disappear animation extension for Transform.
/// DOTween 消失动画 Transform 扩展方法。
///
/// Provides a pop-then-shrink sequence commonly used for removing UI elements,
/// collectibles, and game objects: scale up 1.1× (OutQuad) → shrink to zero (InQuad).
/// 提供"弹大后缩小"消失动画序列，常用于 UI 元素、收集品、游戏对象的移除效果。
/// </summary>
public static class TransformDisappearExtensions
{
    /// <summary>
    /// Disappear with a pop-then-shrink animation.
    /// 弹出后缩小的消失动画。
    ///
    /// Animation sequence / 动画序列:
    ///   1. Scale up to currentScale × 1.1 (popDuration, Ease.OutQuad)
    ///   2. Scale down to Vector3.zero (shrinkDuration, Ease.InQuad)
    ///   3. If restoreScale=true, reset scale to original, then invoke onComplete
    ///
    /// Call <c>transform.DOKill()</c> at entry to prevent overlapping tweens.
    /// 入口处调用 DOKill() 防止重叠 Tween。
    /// </summary>
    /// <param name="transform">
    /// The Transform to animate. Not null. / 要动画的 Transform，不可为空。
    /// </param>
    /// <param name="onComplete">
    /// Callback after shrink completes and scale is reset. Use for Destroy,
    /// Despawn, or SetActive(false). / 缩小完成后、scale 重置后的回调。用于 Destroy、Despawn 或 SetActive(false)。
    /// </param>
    /// <param name="popDuration">
    /// Duration of the pop (scale up) phase in seconds. Default 0.2s. / 弹出阶段时长（秒）。默认 0.2s。
    /// </param>
    /// <param name="shrinkDuration">
    /// Duration of the shrink (scale to zero) phase in seconds. Default 0.2s. / 缩小阶段时长（秒）。默认 0.2s。
    /// </param>
    /// <param name="restoreScale">
    /// Whether to restore original scale before invoking onComplete. Default true.
    /// Set to false when the callback will Destroy/Despawn the object (avoids a one-frame flash at full size).
    /// 是否在调用 onComplete 前恢复原始 scale。默认 true。
    /// 当回调将 Destroy/Despawn 对象时设为 false，避免满尺寸闪烁一帧。
    /// </param>
    public static void DisappearWithPop(
        this Transform transform,
        Action onComplete = null,
        float popDuration = 0.2f,
        float shrinkDuration = 0.2f,
        bool restoreScale = true)
    {
        if (transform == null)
        {
            Debug.LogWarning("[DisappearWithPop] Transform is null. Animation skipped.");
            return;
        }

        // Kill existing tweens on this transform to prevent overlap
        // 清除该 Transform 上的已有 Tween，防止重叠
        transform.DOKill();

        Vector3 originalScale = transform.localScale;
        Vector3 popScale = originalScale * 1.1f;

        transform.DOScale(popScale, popDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                // Guard: transform may have been destroyed during pop
                // 守卫：弹出阶段中 transform 可能已被销毁
                if (transform == null) return;

                transform.DOScale(Vector3.zero, shrinkDuration)
                    .SetEase(Ease.InQuad)
                    .OnComplete(() =>
                    {
                        // Guard: transform may have been destroyed during shrink
                        // 守卫：缩小阶段中 transform 可能已被销毁
                        if (transform != null && restoreScale)
                        {
                            transform.localScale = originalScale;
                        }

                        onComplete?.Invoke();
                    });
            });
    }
}
