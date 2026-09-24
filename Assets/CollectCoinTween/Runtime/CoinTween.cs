using System.Collections;
using System.Collections.Generic;
using Lu.DoTween;
using UnityEngine;
using UnityEngine.UI;

public class CoinTween : MonoBehaviour
{
    private float tweenTimer;
    private float tweenDuration;
    private Vector3 startPos;
    private Vector3 midPos;
    private Transform targetTrans;
    private int index;
    public Image image;
    private System.Action onComplete;
    private float accelerate;

    private void Awake()
    {
        image = GetComponent<Image>();
    }

    public void Move(int index,Vector3 startPos, Vector3 midPos, Transform targetTrans, float accelerate,
        System.Action onComplete = null, float tweenDuration=1)
    {
        this.startPos = startPos;
        this.midPos = midPos;
        this.targetTrans = targetTrans;
        this.accelerate = accelerate;
        this.onComplete = onComplete;
        this.index = index;
        this.tweenDuration = tweenDuration;
        

        gameObject.SetActive(true);

        image.color = Color.white;
        transform.localScale = Vector3.one;
        transform.position = startPos;
        tweenTimer = 0;
    }

    private void Update()
    {
        if (tweenTimer != tweenDuration)
        {
            transform.position = GetBezierPos(tweenTimer/tweenDuration, startPos, midPos, targetTrans.position);
            tweenTimer += Time.deltaTime * (accelerate == -1 ? (1 + index * 0.1f) : accelerate);
            tweenTimer = Mathf.Clamp(tweenTimer, 0, tweenDuration);
            if (tweenTimer == tweenDuration)
            {
                onComplete?.Invoke();
                transform.LuTweenDoScale(Vector3.one * 2f, 0.3f);
                image.LuTweenDoFade(0, 0.3f).OnComplete(()=>gameObject.SetActive(false));
            }
        }
    }


    public static Vector3 GetBezierPos(float t, Vector3 start, Vector3 center, Vector3 end)
    {
        return (1 - t) * (1 - t) * start + 2 * t * (1 - t) * center + t * t * end;
    }

}