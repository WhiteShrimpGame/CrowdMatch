using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 新功能解锁配置 — 控制关卡间新功能介绍弹窗。
/// TODO: 在 Inspector 中配置每个新功能的展示关卡和图标。
/// New feature unlock configuration.
/// </summary>
[CreateAssetMenu(menuName = "BubbleLoop/NewFeatureConfig")]
public class NewFeatureConfig : ScriptableObject
{
    public List<NewFeatureData> NewFeatureDatas = new List<NewFeatureData>();
}

[System.Serializable]
public class NewFeatureData
{
    public int featureShowLevel;
    public string featureTip;
    public Sprite featureMinIcon;
    public Sprite featureBigIcon;
}
