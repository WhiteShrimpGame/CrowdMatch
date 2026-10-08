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
        public List<int> levelDiff;
        public List<int> levelDiffLoop;

        [Tooltip("顺序关的显示文本，与 levels 一一对应（index 0 = 第 1 关）。缺项按空串处理")]
        public List<string> levelTexts = new List<string>();

        [Tooltip("循环关的显示文本，与 loopLevels 一一对应")]
        public List<string> levelTextsLoop = new List<string>();

        /// <summary>
        /// 把 1 起始的关卡编号解析成「在哪张表 + 表内下标」。顺序关用尽后按 loopLevels 循环取用。
        /// 返回 false 表示没有这一关（有限关卡集越界）。GetLevel / GetLevelText / GameData.LevelDiff 共用这一份换算，
        /// 避免三处各写一套取模逻辑后互相漂移。
        /// </summary>
        public bool ResolveIndex(int currentLevel, out bool isLoop, out int index)
        {
            isLoop = false;
            index = currentLevel - 1;

            int sequentialCount = levels != null ? levels.Count : 0;
            if (index >= 0 && index < sequentialCount)
                return true;

            int loopCount = loopLevels != null ? loopLevels.Count : 0;
            if (loopCount > 0)
            {
                isLoop = true;
                index = (index - sequentialCount) % loopCount;
                if (index < 0)
                    index += loopCount;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 解析 1 起始关卡编号对应的 JSON。解析顺序：
        ///   1. 落在 levels 内 → 返回之
        ///   2. loopLevels 非空 → 循环取用
        ///   3. 否则返回 null（集成者处理"没有更多关卡"）
        /// </summary>
        public TextAsset GetLevel(int currentLevel)
        {
            if (!ResolveIndex(currentLevel, out bool isLoop, out int index))
                return null;

            List<TextAsset> table = isLoop ? loopLevels : levels;
            return index < table.Count ? table[index] : null;
        }

        /// <summary>解析 1 起始关卡编号对应的显示文本；没有这一关或该关没配文本时返回空串。下标口径与 GetLevel 完全一致。</summary>
        public string GetLevelText(int currentLevel)
        {
            if (!ResolveIndex(currentLevel, out bool isLoop, out int index))
                return string.Empty;

            List<string> table = isLoop ? levelTextsLoop : levelTexts;
            if (table == null || index >= table.Count)
                return string.Empty;
            return table[index] ?? string.Empty;
        }
    }
}
