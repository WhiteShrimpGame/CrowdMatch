using DG.Tweening;
using UnityEngine;

public class ScaleLoopController : MonoBehaviour
{
    [SerializeField] Vector3 startLocalScale;
    [SerializeField] Vector3 endLocalScale;
    [SerializeField] float duration;
    [SerializeField] Transform aimTarget;

    private void OnEnable()
    {
        aimTarget.transform.localScale = startLocalScale;
        aimTarget.DOScale(endLocalScale, duration).SetLoops(-1, LoopType.Yoyo);
    }

    private void OnDisable()
    {
        aimTarget.DOKill();
    }
}