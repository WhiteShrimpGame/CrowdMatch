using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "TestBtnConfig", menuName = "Config/TestBtnConfig")]
public class TestBtnConfigSO : ScriptableObject
{
    public List<TestBtnItem> items;
}

[System.Serializable]
public class TestBtnItem
{
    [Tooltip("按钮显示名称")]
    public string btnName;
    [Tooltip("指令key，必须和代码_cmdDict里key保持一致")]
    public string cmdKey;
}