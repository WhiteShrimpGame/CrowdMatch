using System.Collections.Generic;
using System.IO;
using System.Linq;
using CrowdMatch;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 关卡文本导入：读一个 txt（每行一个关卡的显示文本），按顺序写进 LevelDataConfig.levelTexts / levelTextsLoop。
/// 顺序关先填满，再填循环关：文本不够时缺的补默认文本，超出部分丢弃。
/// 用法：在 Project 窗口选中 LevelDataConfig 资产，再执行菜单。
/// </summary>
public static class LevelTextImporter
{
    /// <summary>文本不够时给空位补的文本。</summary>
    private const string DefaultText = "北京";

    [MenuItem("工具/导入关卡文本")]
    public static void Import()
    {
        LevelDataConfig dataConfig = Selection.activeObject as LevelDataConfig;
        if (dataConfig == null)
        {
            EditorUtility.DisplayDialog("提示", "请先在 Project 窗口选中一个 LevelDataConfig 资产，再执行本菜单。", "确定");
            return;
        }

        string filePath = EditorUtility.OpenFilePanel("选择文本文件", Application.dataPath, "txt");
        if (string.IsNullOrEmpty(filePath))
            return;

        List<string> texts = new List<string>();
        foreach (string rawLine in File.ReadAllLines(filePath))
        {
            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line))
                continue;   // 空行跳过，不占用关卡编号

            // 超长只警告不跳过：跳过会让后面所有文本整体前移一格，和关卡对不上号
            if (line.Length > 5)
                Debug.LogWarning("[关卡文本导入] 文本超长（" + line.Length + " 字），仍按下标导入，请确认能否显示：" + line);

            texts.Add(line);
        }

        int sequentialCount = dataConfig.levels != null ? dataConfig.levels.Count : 0;
        int loopCount = dataConfig.loopLevels != null ? dataConfig.loopLevels.Count : 0;
        int total = sequentialCount + loopCount;
        if (total <= 0)
        {
            EditorUtility.DisplayDialog("提示", "该 LevelDataConfig 的 levels / loopLevels 都是空的，没有可填充的关卡。", "确定");
            return;
        }

        int lineCount = texts.Count;
        int fromFile = Mathf.Min(lineCount, total);
        int dropped = lineCount - fromFile;
        texts.RemoveRange(fromFile, dropped);           // 超出部分不管
        while (texts.Count < total)
            texts.Add(DefaultText);                     // 不够的补默认文本

        // 与 LevelDataConfig.GetLevel 同一套下标：前 sequentialCount 条归顺序关，其余归循环关
        dataConfig.levelTexts = texts.Take(sequentialCount).ToList();
        dataConfig.levelTextsLoop = texts.Skip(sequentialCount).ToList();

        EditorUtility.SetDirty(dataConfig);
        AssetDatabase.SaveAssets();

        string report = "来源：" + Path.GetFileName(filePath) + "（有效文本 " + lineCount + " 条）\n"
            + "顺序关 levelTexts：" + dataConfig.levelTexts.Count + " 条 / levels " + sequentialCount + "\n"
            + "循环关 levelTextsLoop：" + dataConfig.levelTextsLoop.Count + " 条 / loopLevels " + loopCount + "\n"
            + "来自文本 " + fromFile + " 条，补「" + DefaultText + "」" + (total - fromFile) + " 条，丢弃 " + dropped + " 条";

        Debug.Log("[关卡文本导入] " + report);
        EditorUtility.DisplayDialog("完成", report, "确定");
    }
}
