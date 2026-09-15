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
    /// 目标颜色是否已存在由调用方（窗口）先拦截，本类只负责统计与替换。
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
        /// 注意：目标颜色已存在时应由调用方先拦截（否则会与已有颜色合并），本方法不做该检查。
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
    /// 编辑器窗口：引用一个关卡 JSON，输入旧/新颜色 index，把旧颜色全部替换为新颜色，
    /// 写回原文件。目标颜色原本已存在时禁止替换（避免两种颜色意外合并）。
    /// 覆盖位置：像素、容器、管道、箱子、升降台中的 Pixel。
    /// </summary>
    public class LevelColorReplacerWindow : EditorWindow
    {
        private TextAsset levelJson;
        private int oldIndex = 1;
        private int newIndex = 2;

        [MenuItem("CrowdMatch/替换关卡颜色")]
        public static void Open() => GetWindow<LevelColorReplacerWindow>("替换关卡颜色");

        private void OnGUI()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "把关卡 JSON 中某颜色全部替换为另一种颜色（覆盖像素、容器、管道、箱子、升降台）。\n目标颜色原本已存在时禁止替换。",
                MessageType.Info);

            levelJson = (TextAsset)EditorGUILayout.ObjectField("关卡 JSON", levelJson, typeof(TextAsset), false);

            oldIndex = EditorGUILayout.IntField("旧颜色 index", oldIndex);
            newIndex = EditorGUILayout.IntField("新颜色 index", newIndex);

            // 实时统计：旧/目标颜色各出现多少处，目标已存在时给出警告
            if (levelJson != null)
            {
                var preview = LevelLoader.ParseJson(levelJson.text, levelJson.name);
                if (preview != null)
                {
                    int oldCount = LevelColorReplacer.CountColor(preview, oldIndex);
                    int newCount = LevelColorReplacer.CountColor(preview, newIndex);
                    string msg = "旧颜色 " + oldIndex + "：出现 " + oldCount + " 处；目标颜色 " + newIndex + "：出现 " + newCount + " 处";
                    if (newCount > 0)
                        msg += "（目标已存在，不能替换）";
                    EditorGUILayout.HelpBox(msg, newCount > 0 ? MessageType.Warning : MessageType.None);
                }
            }

            EditorGUILayout.Space(8);
            if (GUILayout.Button("替换", GUILayout.Height(28)))
                Replace();
        }

        private void Replace()
        {
            if (levelJson == null)
            {
                EditorUtility.DisplayDialog("替换关卡颜色", "请先选择关卡 JSON。", "确定");
                return;
            }
            if (oldIndex < 1)
            {
                EditorUtility.DisplayDialog("替换关卡颜色", "旧颜色 index 必须 ≥ 1（0 = 空格，不是颜色）。", "确定");
                return;
            }
            if (newIndex < 1)
            {
                EditorUtility.DisplayDialog("替换关卡颜色", "新颜色 index 必须 ≥ 1（0 = 空格，不是颜色）。", "确定");
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

            int newCount = LevelColorReplacer.CountColor(data, newIndex);
            if (newCount > 0)
            {
                EditorUtility.DisplayDialog("替换关卡颜色",
                    "目标颜色 index " + newIndex + " 已存在于关卡中（" + newCount + " 处），不能替换。",
                    "确定");
                return;
            }

            string path = AssetDatabase.GetAssetPath(levelJson);
            if (string.IsNullOrEmpty(path))
            {
                EditorUtility.DisplayDialog("替换关卡颜色", "无法取得关卡 JSON 的资产路径，不能覆盖写入。", "确定");
                return;
            }

            if (!EditorUtility.DisplayDialog("替换关卡颜色",
                "将把颜色 index " + oldIndex + " 全部替换为 " + newIndex + "，共 " + oldCount + " 处。\n\n覆盖文件：\n" + path + "\n\n是否继续？",
                "替换", "取消"))
            {
                return;
            }

            int replaced = LevelColorReplacer.ReplaceColor(data, oldIndex, newIndex);

            string json = JsonUtility.ToJson(data, true);
            try
            {
                File.WriteAllText(path, json, new UTF8Encoding(false));
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("替换关卡颜色", "写入失败：\n" + e.Message, "确定");
                return;
            }
            AssetDatabase.ImportAsset(path);

            Debug.Log("[LevelColorReplacer] 已将颜色 " + oldIndex + " → " + newIndex +
                "，替换 " + replaced + " 处，已写回 " + path);

            EditorUtility.DisplayDialog("替换关卡颜色",
                "完成：已将颜色 " + oldIndex + " 替换为 " + newIndex + "，共 " + replaced + " 处。",
                "确定");
        }
    }
}
