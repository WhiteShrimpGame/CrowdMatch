using System.Collections.Generic;
using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// ScriptableObject 关卡编排：顺序关卡 + 循环关卡。
    /// levels 按序号 1..N 依次播放；用尽后若 loopLevels 非空则循环取用（无尽模式）。
    /// 创建方式：Assets → Create → CrowdMatch → LevelDataConfig。
    /// </summary>
    [CreateAssetMenu(menuName = "CrowdMatch/LevelDataConfig", fileName = "LevelDataConfig")]
    public class LevelDataConfig : ScriptableObject
    {
        [Tooltip("顺序关卡，按序号 1..N 依次对应（index 0 = 第 1 关）")]
        public List<TextAsset> levels = new List<TextAsset>();

        [Tooltip("循环关卡：顺序关用尽后循环取用。留空表示有限关卡集（越界返回 null）")]
        public List<TextAsset> loopLevels = new List<TextAsset>();

        /// <summary>
        /// 解析 1 起始关卡编号对应的 JSON。解析顺序：
        ///   1. 落在 levels 内 → 返回之
        ///   2. loopLevels 非空 → 循环取用
        ///   3. 否则返回 null（集成者处理"没有更多关卡"）
        /// </summary>
        public TextAsset GetLevel(int currentLevel)
        {
            int index = currentLevel - 1;

            if (levels != null && index >= 0 && index < levels.Count)
                return levels[index];

            if (loopLevels != null && loopLevels.Count > 0)
            {
                int sequentialCount = levels != null ? levels.Count : 0;
                int loopIndex = (index - sequentialCount) % loopLevels.Count;
                if (loopIndex < 0) loopIndex += loopLevels.Count;
                return loopLevels[loopIndex];
            }

            return null;
        }
    }
}
