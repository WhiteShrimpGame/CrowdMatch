using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CoinTweenPanel : MonoBehaviour
{
    public GameObject coinTweenPrefab;
    private Transform tweenParentTrans;
    private List<CoinTween> coinTweens;
    private RectTransform tempRect;

    /// <summary>
    /// 画布模式
    /// </summary>
    private RenderMode canvasRenderMode;

    /// <summary>
    /// 贝塞尔曲线的中间点
    /// </summary>
    private Transform bezierControlPoint;


    /// <summary>
    /// 在其他脚本中注册，当金币飞到目标位置时获得金币
    /// 例如：
    /// coinTweenPanel.onReceivedCoin+=(coin)=>CoinManager.coin+=coin;
    /// </summary>
    public System.Action<int> onReceivedCoin;

    private void Awake()
    {
        coinTweens = new List<CoinTween>();
        GetComponentsInChildren(true, coinTweens);
        tweenParentTrans = transform.Find("Coins");
        bezierControlPoint = transform.Find("BezierControlPoint");
        canvasRenderMode = GetComponentInParent<Canvas>().renderMode;
        tempRect = transform.Find("TempRect").GetComponent<RectTransform>();
    }


    private void IncreaseCoin(int coin)
    {
        onReceivedCoin?.Invoke(coin);
    }

    /// <summary>
    /// 显示一群金币收集的动画,以UI坐标为起点
    /// </summary>
    /// <param name="anchorPos">金币的开始的位置</param>
    /// <param name="targetTrans">金币飞到的目标Transform</param>
    /// <param name="coinAmount">金币收集的数量（指多少钱，不是动画飞上去的个数）</param>
    /// <param name="type">金币动效的样式（0~5）</param>
    /// <param name="onComplete">金币收集之后的回调</param>
    public void ShowCoins(Vector2 anchorPos, Transform targetTrans, int coinAmount, int type,
        System.Action onComplete = null, float duration = 1)
    {
        tempRect.anchoredPosition = anchorPos;
        ShowCoins(tempRect.transform.position, targetTrans, coinAmount, type, onComplete, duration);
    }

    /// <summary>
    /// 显示一群金币收集的动画,以UI的世界坐标为起点
    /// </summary>
    /// <param name="startPos">金币的开始的位置</param>
    /// <param name="targetTrans">金币飞到的目标Transform</param>
    /// <param name="coinAmount">金币收集的数量（指多少钱，不是动画飞上去的个数）</param>
    /// <param name="type">金币动效的样式（0~5）</param>
    /// <param name="onComplete">金币收集之后的回调</param>
    public void ShowCoins(Vector3 startPos, Transform targetTrans, int coinAmount, int type,
        System.Action onComplete = null, float duration = 1, int showCoinAmount = 20)
    {
        List<CoinTween> tweens = GetTweenList(showCoinAmount); //拿出20个金币

        int timeCoin = coinAmount / tweens.Count;
        int finalCoin = timeCoin + coinAmount % tweens.Count;
        for (int i = 0; i < tweens.Count; i++)
        {
            int index = i; //不要将for循环中的i作为参数传入回调中
            tweens[index].Move(
                index, startPos,
                GetBezierPosByType(type),
                targetTrans, -1,
                () => OnTweenCompleted(index, finalCoin, timeCoin, onComplete),
                duration);
        }
    }


    /// <summary>
    /// 显示单个金币收集动画,以UI坐标为起点
    /// </summary>
    /// <param name="startPos">起始点</param>
    /// <param name="targetTrans">终点Transform</param>
    /// <param name="coinAmount">金币数量</param>
    /// <param name="type">动效样式</param>
    /// <param name="acce">金币加速度，一般设置为2</param>
    /// <param name="onComplete">金币收集之后的回调</param>
    public CoinTween ShowSingleCoin(Vector2 anchorPos, Transform targetTrans, int coinAmount, int type, float acce,
        System.Action onComplete = null, float duration = 1)
    {
        tempRect.anchoredPosition = anchorPos;
        return ShowSingleCoin(tempRect.position, targetTrans, coinAmount, type, acce, onComplete, duration);
    }

    /// <summary>
    /// 显示单个金币收集动画,以UI的世界坐标为起点
    /// </summary>
    /// <param name="startPos">起始点</param>
    /// <param name="targetTrans">终点Transform</param>
    /// <param name="coinAmount">金币数量</param>
    /// <param name="type">动效样式</param>
    /// <param name="acce">金币加速度，一般设置为2</param>
    /// <param name="onComplete">金币收集之后的回调</param>
    public CoinTween ShowSingleCoin(Vector3 startPos, Transform targetTrans, int coinAmount, int type, float acce,
        System.Action onComplete = null, float duration = 1)
    {
        CoinTween tween = GetTweenList(1)[0];
        tween.Move(0, startPos, GetBezierPosByType(type), targetTrans, acce,
            () =>
            {
                IncreaseCoin(coinAmount);
                onComplete?.Invoke();
            },
            duration);
        return tween;
    }

    List<CoinTween> GetTweenList(int count)
    {
        List<CoinTween> returnCoinTweens = new List<CoinTween>();
        for (int i = 0; i < coinTweens.Count; i++)
        {
            if (count <= 0)
                break;
            if (!coinTweens[i].gameObject.activeInHierarchy)
            {
                returnCoinTweens.Add(coinTweens[i]);
                count--;
            }
        }

        if (count > 0)
        {
            for (int i = 0; i < count; i++)
            {
                CoinTween tween = Instantiate(coinTweenPrefab, tweenParentTrans).GetComponent<CoinTween>();
                coinTweens.Add(tween);
                returnCoinTweens.Add(tween);
                count--;
                if (count <= 0)
                    break;
            }
        }

        return returnCoinTweens;
    }

    private void OnTweenCompleted(int index, int finalCoin, int timeCoin, System.Action returnAction)
    {
        if (index == 0)
        {
            IncreaseCoin(finalCoin);
            returnAction?.Invoke();
        }
        else
            IncreaseCoin(timeCoin);
    }

    private Vector3 GetBezierPosByType(int typeIndex)
    {
        var midPos = bezierControlPoint.position;
        float offset = canvasRenderMode == RenderMode.ScreenSpaceCamera ? 1 : 100;
        switch (typeIndex)
        {
            case 1:
                midPos += new Vector3(Random.Range(1.5f, -1.5f), Random.Range(-2, 0), 0) * offset;
                break;
            case 2:
                midPos += new Vector3(Random.Range(3, -3), Random.Range(-3, 0), 0) * offset;
                break;
            case 3:
                midPos += new Vector3(Random.Range(6, -6), Random.Range(-3, 3), 0) * offset;
                break;
            case 4:
                midPos += new Vector3(Random.Range(6, -6), Random.Range(-3, 0), 0) * offset;
                break;
            case 5:
                midPos += new Vector3(Random.Range(7, -6), Random.Range(-8, 0), 0) * offset;
                break;
        }

        return midPos;
    }
}