using System;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelTextConfig", menuName = "Config/LevelTextConfig")]
public class LevelTextConfig : ScriptableObject
{
    [Serializable]
    public struct LevelTextItem
    {
        public int level;
        public string text;
    }

    public LevelTextItem[] items;
}