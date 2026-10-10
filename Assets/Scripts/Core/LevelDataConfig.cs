using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

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

        [Tooltip("顺序关的显示文本，按序号循环取用（index 0 = 第 1 关），超出末尾回到第 1 条")]
        public List<string> levelTexts = new List<string>();

        [Tooltip("城市文本源 txt：每行一个城市。右键本资产 →「加载城市信息」，按行拆进上面的 levelTexts")]
        public TextAsset cityTextAsset;

        /// <summary>
        /// 把 1 起始的关卡编号解析成「在哪张表 + 表内下标」。顺序关用尽后按 loopLevels 循环取用。
        /// 返回 false 表示没有这一关（有限关卡集越界）。GetLevel / GameData.LevelDiff 共用这一份换算，
        /// 避免两处各写一套取模逻辑后互相漂移。关卡文本不走这里，见 GetLevelText。
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

        /// <summary>
        /// 解析 1 起始关卡编号对应的显示文本：在 levelTexts 上循环取用，超出末尾回到第 1 条。
        /// 文本只按「第几关」绕圈，不分顺序关/循环关 —— 关卡 JSON 仍按 ResolveIndex 分两张表。
        /// 没配文本时返回空串。
        /// </summary>
        public string GetLevelText(int currentLevel)
        {
            if (levelTexts == null || levelTexts.Count == 0)
                return string.Empty;

            int index = (currentLevel - 1) % levelTexts.Count;
            if (index < 0)
                index += levelTexts.Count;
            return levelTexts[index] ?? string.Empty;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 从 cityTextAsset 导入关卡显示文本：每行一个文本、空行跳过，按顺序全写进 levelTexts。
        /// 文本按关卡序号循环取用，所以不补默认文本、也不丢弃多余行。
        /// 入口：Project 窗口选中本资产 → Inspector 面板右上角 ⋮ →「加载城市信息」。
        /// </summary>
        [ContextMenu("加载城市信息")]
        private void LoadCityInfo()
        {
            if (cityTextAsset == null)
            {
                EditorUtility.DisplayDialog("提示", "请先把城市文本 txt 拖到「城市文本源」字段上，再执行加载。", "确定");
                return;
            }

            List<string> texts = new List<string>();
            foreach (string rawLine in cityTextAsset.text.Split('\n'))
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line))
                    continue;   // 空行跳过，不占用关卡编号

                // 超长只警告不跳过：跳过会让后面所有文本整体前移一格，和关卡对不上号
                if (line.Length > 5)
                    Debug.LogWarning("[加载城市信息] 文本超长（" + line.Length + " 字），仍按下标导入，请确认能否显示：" + line);

                texts.Add(line);
            }

            if (texts.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "「" + cityTextAsset.name + "」里没有有效文本（空行会被跳过）。", "确定");
                return;
            }

            levelTexts = texts;
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();

            string report = "来源：" + cityTextAsset.name + "\n"
                + "levelTexts：" + levelTexts.Count + " 条（按关卡序号循环取用）";
            Debug.Log("[加载城市信息] " + report);
            EditorUtility.DisplayDialog("完成", report, "确定");
        }
#endif
    }
}
