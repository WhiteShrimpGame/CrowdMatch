using UnityEngine;
using DG.Tweening;
using System.Collections;

public class DoubleDigitRoller : MonoBehaviour
{
    [Header("滚轮引用")]
    public DigitWheel tenWheel;    //十位滚轮
    public DigitWheel unitWheel;   //个位滚轮

    private int _currentNum = 0;
    private Sequence _rollSequence;

    /// <summary>
    /// 设置目标数字，范围0~30
    /// 【更新】进位/借位：十位个位动画同时开始，一起结束
    /// </summary>
    public void SetTargetNumber(int target)
    {
        target = Mathf.Clamp(target, 0, 30);
        int oldTen = _currentNum / 10;
        int oldUnit = _currentNum % 10;
        int targetTen = target / 10;
        int targetUnit = target % 10;

        // 杀死上一轮动画，防止叠加
        if (_rollSequence != null)
        {
            _rollSequence.Kill();
            _rollSequence = null;
        }

        bool isCarry = oldUnit == 9 && targetUnit == 0;    //正向进位：09→10
        bool isBorrow = oldUnit == 0 && targetUnit == 9;   //反向借位：10→09

        _rollSequence = DOTween.Sequence();

        if (isCarry)
        {
            // 09→10：个位进位滚动 + 十位滚动【同时启动】
            unitWheel.RollTo(0, true, true);
            tenWheel.RollTo(targetTen, false, true);
        }
        else if (isBorrow)
        {
            //10→09：个位借位滚动 + 十位滚动【同时启动】
            unitWheel.RollTo(9, true, false);
            tenWheel.RollTo(targetTen, false, false);
        }
        else
        {
            //普通情况，十位个位同步滚动
            unitWheel.RollTo(targetUnit, false, true);
            tenWheel.RollTo(targetTen, false, true);
        }

        _currentNum = target;
    }

    /// <summary>
    /// 直接设置数字，无动画
    /// </summary>
    public void SetNumberImmediate(int num)
    {
        if (_rollSequence != null)
        {
            _rollSequence.Kill();
            _rollSequence = null;
        }
        num = Mathf.Clamp(num, 0, 30);
        int ten = num / 10;
        int unit = num % 10;
        tenWheel.SetDigitImmediate(ten);
        unitWheel.SetDigitImmediate(unit);
        _currentNum = num;
    }

    [ContextMenu("测试正向 09→10")]
    void TestCarry()
    {
        SetNumberImmediate(9);
        SetTargetNumber(10);
    }

    [ContextMenu("测试反向 10→09")]
    void TestBorrow()
    {
        SetNumberImmediate(10);
        SetTargetNumber(9);
    }

    [ContextMenu("测试30→29")]
    void Test30To29()
    {
        SetNumberImmediate(30);
        SetTargetNumber(29);
    }

    [ContextMenu("【开始测试】从0 依次滚到30")]
    void StartTest0To30()
    {
        StopAllCoroutines();
        StartCoroutine(TestLoop0To30());
    }

    [ContextMenu("【开始测试】从30 依次滚回0")]
    void StartTest30To0()
    {
        StopAllCoroutines();
        StartCoroutine(TestLoop30To0());
    }

    IEnumerator TestLoop0To30()
    {
        SetNumberImmediate(0);
        yield return new WaitForSeconds(0.8f);
        for (int i = 1; i <= 30; i++)
        {
            SetTargetNumber(i);
            yield return new WaitForSeconds(unitWheel.rollDuration + 0.6f);
        }
    }

    IEnumerator TestLoop30To0()
    {
        SetNumberImmediate(30);
        yield return new WaitForSeconds(0.8f);
        for (int i = 29; i >= 0; i--)
        {
            SetTargetNumber(i);
            yield return new WaitForSeconds(unitWheel.rollDuration + 0.6f);
        }
    }

    private void OnDestroy()
    {
        if (_rollSequence != null)
        {
            _rollSequence.Kill();
        }
        DOTween.Kill(this);
    }
}
