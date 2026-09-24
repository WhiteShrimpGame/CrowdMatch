using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace CrowdMatch
{
    /// <summary>
    /// 纯数据（无 UnityEditor 依赖）的颜色替换工具：把关卡 LevelData 中某颜色在所有位置
    /// （像素 grid、容器、管道波次、箱子内容、升降台地下像素）统一替换为另一种颜色。
    /// 目标颜色**已经存在**时会两种颜色合并、关卡少一种颜色，所以这一步由调用方（窗口）先弹警告并二次确认；
    /// 本类只负责统计与替换。
    /// </summary>
    public static class LevelColorReplacer
    {
        /// <summary>
        /// 统计某颜色在关卡中出现的次数，覆盖所有承载颜色的字段：
        /// pixel.cells、container.items[].colorId、pipes[].colors、boxes[].colorIds、
        /// elevators[].groups[].cells（三元组 [col,row,color,...]，颜色在 index 2、5、8…）。
        /// </summary>
        public static int CountColor(LevelData data, int color)
        {
            if (data == null)
                return 0;
            int count = 0;

            // 像素网格
            if (data.pixel != null && data.pixel.cells != null)
            {
                foreach (var c in data.pixel.cells)
                    if (c == color) count++;
            }

            // 容器
            if (data.container != null && data.container.items != null)
            {
                foreach (var it in data.container.items)
                    if (it != null && it.colorId == color) count++;
            }

            // 管道每波颜色
            if (data.pipes != null)
            {
                foreach (var p in data.pipes)
                {
                    if (p == null || p.colors == null) continue;
                    foreach (var c in p.colors)
                        if (c == color) count++;
                }
            }

            // 箱子隐藏像素颜色
            if (data.boxes != null)
            {
                foreach (var b in data.boxes)
                {
                    if (b == null || b.colorIds == null) continue;
                    foreach (var c in b.colorIds)
                        if (c == color) count++;
                }
            }

            // 升降台地下像素（三元组拍平）
            if (data.elevators != null)
            {
                foreach (var e in data.elevators)
                {
                    if (e == null || e.groups == null) continue;
                    foreach (var g in e.groups)
                    {
                        if (g == null || g.cells == null) continue;
                        for (int i = 2; i < g.cells.Length; i += 3)
                            if (g.cells[i] == color) count++;
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// 把关卡中所有 oldColor 替换为 newColor，返回实际替换次数。
        /// 注意：newColor 已存在时两者会**合并**（关卡颜色种类少一种），
        /// 这个后果由调用方先弹警告 + 二次确认，本方法不做该检查。
        /// </summary>
        public static int ReplaceColor(LevelData data, int oldColor, int newColor)
        {
            if (data == null || oldColor == newColor)
                return 0;
            int count = 0;

            // 像素网格
            if (data.pixel != null && data.pixel.cells != null)
            {
                for (int i = 0; i < data.pixel.cells.Length; i++)
                {
                    if (data.pixel.cells[i] == oldColor)
                    {
                        data.pixel.cells[i] = newColor;
                        count++;
                    }
                }
            }

            // 容器
            if (data.container != null && data.container.items != null)
            {
                foreach (var it in data.container.items)
                {
                    if (it != null && it.colorId == oldColor)
                    {
                        it.colorId = newColor;
                        count++;
                    }
                }
            }

            // 管道每波颜色
            if (data.pipes != null)
            {
                foreach (var p in data.pipes)
                {
                    if (p == null || p.colors == null) continue;
                    for (int i = 0; i < p.colors.Length; i++)
                    {
                        if (p.colors[i] == oldColor)
                        {
                            p.colors[i] = newColor;
                            count++;
                        }
                    }
                }
            }

            // 箱子隐藏像素颜色
            if (data.boxes != null)
            {
                foreach (var b in data.boxes)
                {
                    if (b == null || b.colorIds == null) continue;
                    for (int i = 0; i < b.colorIds.Length; i++)
                    {
                        if (b.colorIds[i] == oldColor)
                        {
                            b.colorIds[i] = newColor;
                            count++;
                        }
                    }
                }
            }

            // 升降台地下像素（三元组拍平）
            if (data.elevators != null)
            {
                foreach (var e in data.elevators)
                {
                    if (e == null || e.groups == null) continue;
                    foreach (var g in e.groups)
                    {
                        if (g == null || g.cells == null) continue;
                        for (int i = 2; i < g.cells.Length; i += 3)
                        {
                            if (g.cells[i] == oldColor)
                            {
                                g.cells[i] = newColor;
                                count++;
                            }
                        }
                    }
                }
            }

            return count;
        }
    }

    /// <summary>
    /// 编辑器窗口：引用一个关卡 JSON，输入旧/新颜色 index，把旧颜色全部替换为新颜色。
    /// 写回方式二选一：**覆盖原文件**（就地写回）或**另存为新文件**（原文件不动，弹保存对话框选路径）。
    /// 目标颜色原本已存在时**不禁止**，而是先弹「会少一种颜色」的警告、确认后再继续。
    /// 覆盖位置：像素、容器、管道、箱子、升降台中的 Pixel。
    /// </summary>
    public class LevelColorReplacerWindow : EditorWindow
    {
        /// <summary>写回方式：覆盖原文件 / 另存为新文件。</summary>
        private enum WriteMode
        {
            Overwrite,
            SaveAs,
        }

        private TextAsset levelJson;
        private int oldIndex = 1;
        private int newIndex = 2;
        private WriteMode writeMode = WriteMode.Overwrite;

        /// <summary>「另存为」上一次选的路径（EditorPrefs 记忆目录，与其它导入导出工具同一套约定）。</summary>
        private const string SaveAsPathKey = "CrowdMatch.LevelColorReplacer.LastSaveAsPath";

        [MenuItem("CrowdMatch/关卡工具/替换关卡颜色", false, MenuPriority.LevelTools + MenuPriority.Seg1)]
        public static void Open() => GetWindow<LevelColorReplacerWindow>("替换关卡颜色");

        private void OnGUI()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "把关卡 JSON 中某颜色全部替换为另一种颜色（覆盖像素、容器、管道、箱子、升降台）。\n" +
                "写回可选择「覆盖原文件」或「另存为新文件」。\n" +
                "目标颜色原本已存在时不禁止，但会让两种颜色合并、关卡少一种颜色 —— 会先警告，确认后才继续。",
                MessageType.Info);

            levelJson = (TextAsset)EditorGUILayout.ObjectField("关卡 JSON", levelJson, typeof(TextAsset), false);

            oldIndex = EditorGUILayout.IntField("旧颜色 index", oldIndex);
            newIndex = EditorGUILayout.IntField("新颜色 index", newIndex);

            EditorGUILayout.Space(4);
            writeMode = (WriteMode)GUILayout.Toolbar((int)writeMode, new[] { "覆盖原文件", "另存为新文件" });
            if (writeMode == WriteMode.Overwrite)
            {
                EditorGUILayout.LabelField("直接写回选中的那个 JSON（没有 Undo）。", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.LabelField("原文件保持不动；点「替换」时弹保存对话框，默认名为「原名_replaced.json」，" +
                    "目录默认源文件所在目录。", EditorStyles.miniLabel);
            }

            // 实时统计：旧/目标颜色各出现多少处，目标已存在时给出「会合并」的警告
            if (levelJson != null)
            {
                var preview = LevelLoader.ParseJson(levelJson.text, levelJson.name);
                if (preview != null)
                {
                    int oldCount = LevelColorReplacer.CountColor(preview, oldIndex);
                    int newCount = LevelColorReplacer.CountColor(preview, newIndex);
                    string msg = "旧颜色 " + oldIndex + "：出现 " + oldCount + " 处；目标颜色 " + newIndex + "：出现 " + newCount + " 处";
                    if (newCount > 0)
                        msg += "\n目标已存在 —— 替换后颜色 " + oldIndex + " 不再出现，两种颜色合并为一种（关卡颜色种类 −1），" +
                               "点「替换」后会先弹一次确认。";
                    EditorGUILayout.HelpBox(msg, newCount > 0 ? MessageType.Warning : MessageType.None);
                }
            }

            EditorGUILayout.Space(8);
            string buttonLabel = writeMode == WriteMode.Overwrite ? "替换（覆盖原文件）" : "替换（另存为新文件）";
            if (GUILayout.Button(buttonLabel, GUILayout.Height(28)))
                Replace();
        }

        private void Replace()
        {
            if (levelJson == null)
            {
                EditorUtility.DisplayDialog("替换关卡颜色", "请先选择关卡 JSON。", "确定");
                return;
            }
            if (oldIndex < 0)
            {
                EditorUtility.DisplayDialog("替换关卡颜色", "旧颜色 index 必须 ≥ 0（-1 = 空格，不是颜色）。", "确定");
                return;
            }
            if (newIndex < 0)
            {
                EditorUtility.DisplayDialog("替换关卡颜色", "新颜色 index 必须 ≥ 0（-1 = 空格，不是颜色）。", "确定");
                return;
            }
            if (oldIndex == newIndex)
            {
                EditorUtility.DisplayDialog("替换关卡颜色", "旧颜色与新颜色相同，无需替换。", "确定");
                return;
            }

            var data = LevelLoader.ParseJson(levelJson.text, levelJson.name);
            if (data == null)
            {
                EditorUtility.DisplayDialog("替换关卡颜色", "关卡 JSON 解析失败，详见 Console。", "确定");
                return;
            }

            int oldCount = LevelColorReplacer.CountColor(data, oldIndex);
            if (oldCount == 0)
            {
                EditorUtility.DisplayDialog("替换关卡颜色", "关卡中未找到旧颜色 index " + oldIndex + "。", "确定");
                return;
            }

            // 目标颜色已存在：不再硬拦，改为**先警告「会少一种颜色」**，用户确认后才继续（覆盖模式后面还有一道覆盖确认）。
            int newCount = LevelColorReplacer.CountColor(data, newIndex);
            if (newCount > 0)
            {
                bool willOverwrite = writeMode == WriteMode.Overwrite;
                if (!EditorUtility.DisplayDialog("替换关卡颜色 · 会让关卡少一种颜色",
                    "目标颜色 index " + newIndex + " 已经存在于关卡中（" + newCount + " 处）。\n\n" +
                    "继续替换会把旧颜色 " + oldIndex + "（" + oldCount + " 处）并入 " + newIndex + "（" + newCount +
                    " 处）：替换后 " + oldIndex + " 在整个关卡里不再出现，像素 / 容器 / 管道波次 / 箱子内容 / 升降台像素" +
                    "全部合并成同一种颜色 —— 关卡的颜色种类减少一种。" +
                    (willOverwrite ? "且这一步不可撤销（直接覆盖写回 JSON）。" : "（原文件不受影响，结果写进新文件。）") + "\n\n" +
                    "若这关按 3 拆车，请顺手核对合并后的总数（" + oldCount + " + " + newCount + "）能否被 3 整除。\n\n" +
                    "是否仍然替换？",
                    "仍然替换", "取消"))
                {
                    return;
                }
            }

            string sourcePath = AssetDatabase.GetAssetPath(levelJson);
            bool overwrite = writeMode == WriteMode.Overwrite;
            string targetPath;

            if (overwrite)
            {
                if (string.IsNullOrEmpty(sourcePath))
                {
                    EditorUtility.DisplayDialog("替换关卡颜色",
                        "无法取得关卡 JSON 的资产路径，不能覆盖写入。\n请把「写回方式」改成「另存为新文件」。", "确定");
                    return;
                }
                targetPath = sourcePath;

                if (!EditorUtility.DisplayDialog("替换关卡颜色",
                    (newCount > 0 ? "（颜色合并已确认，这是最后一次确认）\n\n" : "") +
                    "将把颜色 index " + oldIndex + " 全部替换为 " + newIndex + "，共 " + oldCount + " 处。\n\n覆盖文件：\n" +
                    targetPath + "\n\n原文件会被就地改写（没有 Undo）。是否继续？",
                    "替换", "取消"))
                {
                    return;
                }
            }
            else
            {
                // 另存：源文件保持不动，先选目标路径（默认 = 源文件同目录 + 原名_replaced.json）。
                // 这一步本身就是用户的明确动作，所以不再叠一道确认 —— 它没有「覆盖」的风险。
                string startDir = EditorPathMemory.LoadDir(SaveAsPathKey,
                    string.IsNullOrEmpty(sourcePath) ? "Assets/Levels" : Path.GetDirectoryName(sourcePath));
                string baseName = string.IsNullOrEmpty(sourcePath) ? "Level" : Path.GetFileNameWithoutExtension(sourcePath);

                string picked = EditorUtility.SaveFilePanel("另存替换后的关卡 JSON", startDir, baseName + "_replaced", "json");
                if (string.IsNullOrEmpty(picked))
                    return;   // 保存面板取消 = 什么都不做（源文件也没动）
                EditorPathMemory.SaveDir(SaveAsPathKey, picked);
                targetPath = picked;
            }

            int replaced = LevelColorReplacer.ReplaceColor(data, oldIndex, newIndex);

            string json = JsonUtility.ToJson(data, true);
            try
            {
                File.WriteAllText(targetPath, json, new UTF8Encoding(false));
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("替换关卡颜色", "写入失败：\n" + e.Message, "确定");
                return;
            }

            bool underAssets = IsUnderAssets(targetPath);
            if (underAssets)
                AssetDatabase.ImportAsset(targetPath);   // 覆盖时路径必然在 Assets 下，行为与以前一致
            else
                AssetDatabase.Refresh();

            Debug.Log("[LevelColorReplacer] 已将颜色 " + oldIndex + " → " + newIndex +
                "，替换 " + replaced + " 处" + (newCount > 0 ? "（与原有 " + newCount + " 处合并，关卡少一种颜色）" : "") +
                "，" + (overwrite ? "已覆盖 " : "已另存为 ") + targetPath);

            string done = "完成：已将颜色 " + oldIndex + " 替换为 " + newIndex + "，共 " + replaced + " 处。" +
                (newCount > 0 ? "\n\n已与原有 " + newCount + " 处合并 —— 颜色 " + oldIndex + " 在关卡中不再出现。" : "") +
                "\n\n" + (overwrite ? "已覆盖：" : "已另存为：") + "\n" + targetPath;
            if (!overwrite && !underAssets)
                done += "\n\n注意：该文件不在 Assets 目录下，Unity 不会把它导入成 TextAsset，无法在 GameManager.levelJsons 里引用。";
            else if (!overwrite)
                done += "\n\n原文件没有改动；要换成这份，请在 GameManager.levelJsons 里把引用指向这个新文件。";
            EditorUtility.DisplayDialog("替换关卡颜色", done, "确定");
        }

        /// <summary>该路径是否落在本工程的 Assets 目录下（决定写完后用 ImportAsset 还是 Refresh）。</summary>
        private static bool IsUnderAssets(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;
            string assets = Application.dataPath.Replace('\\', '/');
            string full = Path.GetFullPath(path).Replace('\\', '/');
            return full.StartsWith(assets + "/", StringComparison.OrdinalIgnoreCase);
        }
    }
}
