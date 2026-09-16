using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// SpawnPool 的配置资产：Project 窗口右键 → Create → SpawnPoolConfig。
    /// </summary>
    [CreateAssetMenu(fileName = "SpawnPoolConfig", menuName = "SpawnPoolConfig", order = 1)]
    public class SpawnPoolConfig : ScriptableObject
    {
        public SpawnPoolItem[] items;
    }

    [System.Serializable]
    public class SpawnPoolItem
    {
        public string tag;
        public GameObject item;
        public int preloadCount;
    }
}
