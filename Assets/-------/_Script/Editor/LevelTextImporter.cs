using UnityEditor;
using System.IO;
using UnityEngine;

public static class LevelTextImporter
{
    [MenuItem("工具/导入关卡文本TXT")]
    public static void ImportTxtToConfig()
    {
        // 弹出窗口选择txt文件
        string filePath = EditorUtility.OpenFilePanel("选择文本文件", Application.dataPath, "txt");
        if (string.IsNullOrEmpty(filePath))
        {
            EditorUtility.DisplayDialog("提示", "未选择文件", "确定");
            return;
        }

        // 读取所有行
        string[] allLines = File.ReadAllLines(filePath);

        // 新建配置实例
        LevelTextConfig config = ScriptableObject.CreateInstance<LevelTextConfig>();
        var itemList = new System.Collections.Generic.List<LevelTextConfig.LevelTextItem>();

        int levelIndex = 1; // 关卡从1开始

        foreach (string rawLine in allLines)
        {
            string line = rawLine.Trim();
            // 空行跳过，不占用关卡编号
            if (string.IsNullOrEmpty(line))
                continue;

            // 文本超过5个字，打印警告并跳过
            if (line.Length > 5)
            {
                Debug.LogWarning($"文本超长，跳过！内容：{line}");
                continue;
            }

            // 合法数据加入列表，关卡自增
            itemList.Add(new LevelTextConfig.LevelTextItem()
            {
                level = levelIndex,
                text = line
            });
            levelIndex++;
        }

        config.items = itemList.ToArray();

        // 保存ScriptableObject到Assets目录
        string savePath = EditorUtility.SaveFilePanelInProject("保存配置", "LevelTextConfig", "asset", "保存关卡文本配置");
        if (!string.IsNullOrEmpty(savePath))
        {
            AssetDatabase.CreateAsset(config, savePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("完成", $"成功导入，共生成 {itemList.Count} 条关卡数据", "确定");
        }
    }
}