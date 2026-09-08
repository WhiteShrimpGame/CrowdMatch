using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace CrowdMatch
{
    /// <summary>
    /// 按 Record Mode 记录的像素取出顺序重排容器（纯算法，无 UnityEditor 依赖）。
    ///
    /// 模型：Record txt 每行一个 colorId，即像素的取出顺序。每步「取某颜色 3 个像素」（不足 3 取剩余），
    /// 计算其造成的积压 backlog；按「当前进度对应分段」的积压范围选色，逐步生成「容器序列」（颜色 + 容量，容量默认 3、尾档为余数），
    /// 最后把该序列随机分布回关卡容器的各列（每列不超过原始容器数量，列内从前排向后排依次填充）。
    ///
    /// 分段：progress = 已标记像素数 / 像素总数，落在某段（进度区间）内时使用该段的 [backlogMin, backlogMax]；尾段强制到 100%。
    /// </summary>
    public static class ContainerRearranger
    {
        /// <summary>单个生成容器的颜色与容量。</summary>
        public class Entry
        {
            public int color;
            public int capacity;
        }

        /// <summary>积压区间段：进度到 percent%（含）之前生成的容器使用 [backlogMin, backlogMax]；尾段 percent 强制 100。</summary>
        [Serializable]
        public class BacklogSegment
        {
            public int percent;
            public int backlogMin;
            public int backlogMax;
        }

        /// <summary>积压分段参数模板（导出 / 导入 JSON 用）。</summary>
        [Serializable]
        public class BacklogSegmentConfig
        {
            public List<BacklogSegment> segments = new List<BacklogSegment>();
        }

        /// <summary>每次取出的像素数（= 标准容器容量；不足时记剩余数量）。</summary>
        private const int BatchSize = 3;

        /// <summary>解析 record 文本为颜色序列；含非法行时返回 false（seq 置 null）。</summary>
        public static bool TryParseRecord(string text, out List<int> seq)
        {
            seq = new List<int>();
            if (string.IsNullOrEmpty(text))
                return true;

            foreach (var raw in text.Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length == 0)
                    continue;
                if (!int.TryParse(line, out int v))
                {
                    seq = null;
                    return false;
                }
                seq.Add(v);
            }
            return true;
        }

        /// <summary>
        /// 校验 record 与关卡是否一致：像素总数一致，且各颜色计数一致。
        /// 通过返回 null，否则返回错误描述。
        /// </summary>
        public static string Validate(LevelData data, IReadOnlyList<int> seq)
        {
            int columns = Mathf.Max(1, data.pixel.columns);
            int totalRows = Mathf.Max(0, data.pixel.rows) + Mathf.Max(0, data.pixel.tailRows);
            int pixelTotal = columns * totalRows;

            if (seq.Count != pixelTotal)
                return "Record 像素数(" + seq.Count + ")与关卡像素数(" + pixelTotal + ")不一致。";
            if (data.pixel.cells == null || data.pixel.cells.Length < pixelTotal)
                return "关卡像素 cells 数量不足（需要 " + pixelTotal + "）。";

            var recordCounts = new Dictionary<int, int>();
            foreach (var c in seq)
                recordCounts[c] = recordCounts.TryGetValue(c, out int rc) ? rc + 1 : 1;

            var pixelCounts = new Dictionary<int, int>();
            for (int i = 0; i < pixelTotal; i++)
            {
                int c = data.pixel.cells[i];
                pixelCounts[c] = pixelCounts.TryGetValue(c, out int pc) ? pc + 1 : 1;
            }

            if (recordCounts.Count != pixelCounts.Count)
                return "Record 与关卡的颜色种类数不一致（" + recordCounts.Count + " vs " + pixelCounts.Count + "）。";

            foreach (var kv in pixelCounts)
            {
                recordCounts.TryGetValue(kv.Key, out int rc);
                if (rc != kv.Value)
                    return "颜色 " + kv.Key + " 数量不一致：像素 " + kv.Value + "，Record " + rc + "。";
            }
            return null;
        }

        /// <summary>
        /// 执行重排：按进度分段生成容器序列 → 随机分布回各列 → 产出新的 LevelData（pixel 不变，lockContainer=true）。
        /// </summary>
        public static LevelData Rearrange(LevelData data, IReadOnlyList<int> seq, IReadOnlyList<BacklogSegment> segments, int columnOverride, int seed)
        {
            var rng = MakeRng(seed);
            var segs = NormalizeSegments(segments);
            var entries = GenerateSequence(seq, segs, rng);

            int origColumns = Mathf.Max(1, data.container.columns);
            int[] origColCount = new int[origColumns];
            if (data.container.items != null)
            {
                foreach (var it in data.container.items)
                {
                    if (it.x >= 0 && it.x < origColumns)
                        origColCount[it.x]++;
                }
            }

            int origCount = 0;
            foreach (var c in origColCount) origCount += c;
            if (entries.Count != origCount)
                Debug.LogError("[ContainerRearranger] 生成的容器数(" + entries.Count + ")与原容器数(" + origCount +
                    ")不一致；关卡容器容量可能不是 3 的拆分，分布结果可能不完整。");

            // 列数：0 / 负 = 维持原列数；否则按新列数平均每列数量
            int columns;
            int[] colCount;
            if (columnOverride > 0 && columnOverride != origColumns)
            {
                columns = columnOverride;
                colCount = EvenlySplit(entries.Count, columns);
            }
            else
            {
                columns = origColumns;
                colCount = origColCount;
            }

            var result = JsonUtility.FromJson<LevelData>(JsonUtility.ToJson(data));
            result.container.lockContainer = true;
            result.container.columns = columns;
            result.container.rows = Mathf.Max(result.container.rows, CeilDiv(entries.Count, columns));
            result.container.items = Distribute(entries, columns, colCount, rng);

            LogSummary(entries, result.container.items, columns);
            return result;
        }

        /// <summary>
        /// 生成容器序列：每步按当前进度（已标记数/总数）对应分段的积压范围选一个颜色，
        /// 取该颜色 3 个（不足取剩余）像素，记录 (颜色, 容量)。
        /// </summary>
        private static List<Entry> GenerateSequence(IReadOnlyList<int> seq, IReadOnlyList<BacklogSegment> segments, System.Random rng)
        {
            int n = seq.Count;

            // 每个颜色在序列中的下标（升序），配合 colorPos 指针找到「下一个未标记」的该颜色像素
            var colorIndices = new Dictionary<int, List<int>>();
            for (int i = 0; i < n; i++)
            {
                if (!colorIndices.TryGetValue(seq[i], out var list))
                {
                    list = new List<int>();
                    colorIndices[seq[i]] = list;
                }
                list.Add(i);
            }

            var colorPos = new Dictionary<int, int>();
            foreach (var kv in colorIndices)
                colorPos[kv.Key] = 0;

            int markedCount = 0;          // 已标记像素总数
            int globalMaxMarked = -1;     // 全局最后一个被标记点的下标
            var result = new List<Entry>();

            while (markedCount < n)
            {
                // 按当前进度选分段
                ResolveSegment(segments, (double)markedCount / n, out int backlogMin, out int backlogMax);

                var inRange = new List<int>();   // backlog ∈ [min,max]
                var below = new List<int>();     // backlog < min
                var minColors = new List<int>(); // 全局最小 backlog 的颜色（并列时随机）
                int minBacklog = int.MaxValue;

                foreach (var kv in colorIndices)
                {
                    int color = kv.Key;
                    var idxs = kv.Value;
                    int pos = colorPos[color];
                    int remaining = idxs.Count - pos;
                    if (remaining <= 0)
                        continue;

                    int take = Math.Min(BatchSize, remaining);
                    int lastIdx = idxs[pos + take - 1];
                    int newMax = Math.Max(globalMaxMarked, lastIdx);

                    // 积压 = 「全局最后一个被标记点」之前仍未标记的点数
                    //      = (newMax + 1) - (markedCount + take)
                    int backlog = (newMax + 1) - (markedCount + take);

                    if (backlog >= backlogMin && backlog <= backlogMax)
                        inRange.Add(color);
                    else if (backlog < backlogMin)
                        below.Add(color);

                    if (backlog < minBacklog)
                    {
                        minBacklog = backlog;
                        minColors.Clear();
                        minColors.Add(color);
                    }
                    else if (backlog == minBacklog)
                    {
                        minColors.Add(color);
                    }
                }

                int pick;
                if (inRange.Count > 0)
                    pick = inRange[rng.Next(inRange.Count)];
                else if (below.Count > 0)
                    pick = below[rng.Next(below.Count)];
                else
                    pick = minColors[rng.Next(minColors.Count)];

                int pickPos = colorPos[pick];
                int pickTake = Math.Min(BatchSize, colorIndices[pick].Count - pickPos);
                int pickLast = colorIndices[pick][pickPos + pickTake - 1];

                colorPos[pick] += pickTake;
                markedCount += pickTake;
                globalMaxMarked = Math.Max(globalMaxMarked, pickLast);

                result.Add(new Entry { color = pick, capacity = pickTake });
            }

            return result;
        }

        /// <summary>
        /// 把容器序列随机分布到各列：每列不超过其原始容器数量，列内从前排（y=0）向后依次填充。
        /// </summary>
        private static LevelData.ContainerItemData[] Distribute(List<Entry> entries, int columns, int[] colCount, System.Random rng)
        {
            var items = new List<LevelData.ContainerItemData>(entries.Count);
            int[] colNext = new int[columns];      // 每列下一个 y
            int[] colRemain = (int[])colCount.Clone(); // 每列剩余可填数量

            foreach (var e in entries)
            {
                var avail = new List<int>();
                for (int c = 0; c < columns; c++)
                    if (colRemain[c] > 0)
                        avail.Add(c);
                if (avail.Count == 0)
                    break;   // 序列超出总槽位（容量模型不一致，防御性截断）

                int x = avail[rng.Next(avail.Count)];
                int y = colNext[x]++;
                colRemain[x]--;

                items.Add(new LevelData.ContainerItemData
                {
                    x = x,
                    y = y,
                    colorId = e.color,
                    capacity = e.capacity,
                });
            }
            return items.ToArray();
        }

        /// <summary>规范化分段：拷贝一份，按 percent 升序排列，尾段强制 100；空输入回退为单段默认。</summary>
        private static List<BacklogSegment> NormalizeSegments(IReadOnlyList<BacklogSegment> segments)
        {
            var segs = new List<BacklogSegment>(segments != null ? segments.Count : 0);
            if (segments != null)
            {
                foreach (var s in segments)
                    segs.Add(new BacklogSegment { percent = s.percent, backlogMin = s.backlogMin, backlogMax = s.backlogMax });
            }

            segs.Sort((a, b) => a.percent.CompareTo(b.percent));
            if (segs.Count == 0)
                segs.Add(new BacklogSegment { percent = 100, backlogMin = 0, backlogMax = 6 });
            else
                segs[segs.Count - 1].percent = 100;   // 尾段强制 100

            return segs;
        }

        /// <summary>按进度定位分段，返回该段的 [min, max]。progress ∈ [0,1]。</summary>
        private static void ResolveSegment(IReadOnlyList<BacklogSegment> segments, double progress, out int min, out int max)
        {
            for (int i = 0; i < segments.Count; i++)
            {
                double end = (i == segments.Count - 1) ? 1.0 : segments[i].percent / 100.0;
                if (progress <= end)
                {
                    min = segments[i].backlogMin;
                    max = segments[i].backlogMax;
                    return;
                }
            }
            min = segments[segments.Count - 1].backlogMin;
            max = segments[segments.Count - 1].backlogMax;
        }

        private static System.Random MakeRng(int seed)
        {
            return seed != 0 ? new System.Random(seed) : new System.Random(Environment.TickCount);
        }

        /// <summary>把 total 个容器平均分到 columns 列：每列 base 个，多出的 remainder 个分给前 rem 列（每列 +1）。</summary>
        private static int[] EvenlySplit(int total, int columns)
        {
            int[] result = new int[columns];
            int baseCount = total / columns;
            int rem = total % columns;
            for (int i = 0; i < columns; i++)
                result[i] = baseCount + (i < rem ? 1 : 0);
            return result;
        }

        /// <summary>整数向上取整除法。</summary>
        private static int CeilDiv(int a, int b)
        {
            return (a + b - 1) / b;
        }

        private static void LogSummary(List<Entry> entries, LevelData.ContainerItemData[] items, int columns)
        {
            int totalCap = 0;
            foreach (var e in entries) totalCap += e.capacity;

            var sb = new StringBuilder();
            for (int i = 0; i < entries.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(entries[i].color);
            }

            Debug.Log("[ContainerRearranger] 生成容器序列（共 " + entries.Count + " 个，总容量 " + totalCap + "）：\n  " + sb);

            var perCol = new int[columns];
            foreach (var it in items)
                if (it.x >= 0 && it.x < columns)
                    perCol[it.x]++;
            Debug.Log("[ContainerRearranger] 列分布：" + string.Join(", ", perCol) + "（已锁定 lockContainer=true）");
        }
    }

    /// <summary>
    /// 编辑器窗口：选择关卡 JSON + Record txt，设定分段积压范围与随机种子，生成并导出重排后的关卡 JSON。
    /// </summary>
    public class ContainerRearrangerWindow : EditorWindow
    {
        private const string RecordPathKey = "CrowdMatch.ContainerRearranger.LastRecordPath";
        private const string ExportPathKey = "CrowdMatch.ContainerRearranger.LastExportPath";
        private const string TemplatePathKey = "CrowdMatch.ContainerRearranger.LastTemplatePath";

        private TextAsset levelJson;
        private string recordPath = "";
        private List<ContainerRearranger.BacklogSegment> segments = new List<ContainerRearranger.BacklogSegment>
        {
            new ContainerRearranger.BacklogSegment { percent = 100, backlogMin = 0, backlogMax = 6 }
        };
        private int seed = 0;
        private int columnOverride = 0;

        [MenuItem("CrowdMatch/按 Record 重排容器")]
        public static void Open() => GetWindow<ContainerRearrangerWindow>("按 Record 重排容器");

        private void OnGUI()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox("根据 Record Mode 的像素取出顺序重排容器并导出新关卡 JSON（像素保持不变，lockContainer=true）。", MessageType.Info);

            levelJson = (TextAsset)EditorGUILayout.ObjectField("关卡 JSON", levelJson, typeof(TextAsset), false);

            EditorGUILayout.BeginHorizontal();
            recordPath = EditorGUILayout.TextField("Record txt", recordPath);
            if (GUILayout.Button("选择", GUILayout.Width(48)))
                PickRecord();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("积压分段（进度% → 积压范围，尾段强制 100%）", EditorStyles.boldLabel);

            int removeAt = -1;
            for (int i = 0; i < segments.Count; i++)
            {
                var seg = segments[i];
                bool isLast = (i == segments.Count - 1);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("段" + (i + 1), GUILayout.Width(28));
                if (isLast)
                {
                    EditorGUILayout.LabelField("100%", GUILayout.Width(40));
                }
                else
                {
                    seg.percent = EditorGUILayout.IntField(seg.percent, GUILayout.Width(32));
                    EditorGUILayout.LabelField("%", GUILayout.Width(14));
                }
                seg.backlogMin = EditorGUILayout.IntField(seg.backlogMin, GUILayout.Width(32));
                EditorGUILayout.LabelField("~", GUILayout.Width(12));
                seg.backlogMax = EditorGUILayout.IntField(seg.backlogMax, GUILayout.Width(32));
                if (GUILayout.Button("×", GUILayout.Width(22)))
                    removeAt = i;
                EditorGUILayout.EndHorizontal();
            }
            if (removeAt >= 0 && segments.Count > 1)
            {
                segments.RemoveAt(removeAt);
                Repaint();
            }

            if (GUILayout.Button("+ 添加段", GUILayout.Width(76)))
            {
                AddSegment();
                Repaint();
            }

            string segErr = ValidateSegments();
            if (segErr != null)
                EditorGUILayout.HelpBox(segErr, MessageType.Warning);

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("导出分段模板", GUILayout.Width(96)))
                ExportSegmentsTemplate();
            if (GUILayout.Button("导入分段模板", GUILayout.Width(96)))
                ImportSegmentsTemplate();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            columnOverride = EditorGUILayout.IntField("容器列数（0=维持原列数）", columnOverride);
            seed = EditorGUILayout.IntField("随机种子（0=随机）", seed);

            EditorGUILayout.Space(8);
            if (GUILayout.Button("生成并导出", GUILayout.Height(28)))
                GenerateAndExport();
        }

        private void AddSegment()
        {
            var seg = new ContainerRearranger.BacklogSegment { percent = 100, backlogMin = 0, backlogMax = 6 };
            if (segments.Count > 0)
            {
                var prev = segments[segments.Count - 1];
                seg.backlogMin = prev.backlogMin;
                seg.backlogMax = prev.backlogMax;
                if (prev.percent >= 100)
                    prev.percent = 50;   // 原尾段让位，默认改到 50%
            }
            segments.Add(seg);
        }

        private string ValidateSegments()
        {
            if (segments.Count == 0)
                return "至少需要一段积压区间。";
            int prev = 0;
            for (int i = 0; i < segments.Count; i++)
            {
                int p = (i == segments.Count - 1) ? 100 : segments[i].percent;
                if (p <= prev)
                    return "进度百分比必须依次递增（段" + (i + 1) + " 应大于 " + prev + "%）。";
                prev = p;
                if (segments[i].backlogMax < segments[i].backlogMin)
                    return "段" + (i + 1) + " 的积压上限应 ≥ 下限。";
            }
            return null;
        }

        private void PickRecord()
        {
            string dir = EditorPathMemory.LoadDir(RecordPathKey, "Assets");
            string path = EditorUtility.OpenFilePanel("选择 Record txt", dir, "txt");
            if (string.IsNullOrEmpty(path))
                return;
            EditorPathMemory.SaveDir(RecordPathKey, path);
            recordPath = path;
        }

        private void ExportSegmentsTemplate()
        {
            var cfg = new ContainerRearranger.BacklogSegmentConfig();
            foreach (var s in segments)
                cfg.segments.Add(new ContainerRearranger.BacklogSegment
                {
                    percent = s.percent,
                    backlogMin = s.backlogMin,
                    backlogMax = s.backlogMax,
                });

            string json = JsonUtility.ToJson(cfg, true);

            string defaultDir = EditorPathMemory.LoadDir(TemplatePathKey, "Assets");
            string path = EditorUtility.SaveFilePanel("导出积压分段模板", defaultDir, "BacklogSegments.json", "json");
            if (string.IsNullOrEmpty(path))
                return;
            EditorPathMemory.SaveDir(TemplatePathKey, path);

            File.WriteAllText(path, json, new UTF8Encoding(false));
            AssetDatabase.Refresh();
            Debug.Log("[ContainerRearranger] 已导出积压分段模板到 " + path);
        }

        private void ImportSegmentsTemplate()
        {
            string defaultDir = EditorPathMemory.LoadDir(TemplatePathKey, "Assets");
            string path = EditorUtility.OpenFilePanel("导入积压分段模板", defaultDir, "json");
            if (string.IsNullOrEmpty(path))
                return;
            EditorPathMemory.SaveDir(TemplatePathKey, path);

            string json;
            try
            {
                json = File.ReadAllText(path);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("导入积压分段模板", "读取失败：\n" + e.Message, "确定");
                return;
            }

            ContainerRearranger.BacklogSegmentConfig cfg;
            try
            {
                cfg = JsonUtility.FromJson<ContainerRearranger.BacklogSegmentConfig>(json);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("导入积压分段模板", "解析失败：\n" + e.Message, "确定");
                return;
            }

            if (cfg == null || cfg.segments == null || cfg.segments.Count == 0)
            {
                EditorUtility.DisplayDialog("导入积压分段模板", "模板中没有任何分段。", "确定");
                return;
            }

            segments = cfg.segments;
            Repaint();
            Debug.Log("[ContainerRearranger] 已导入积压分段模板（" + segments.Count + " 段）：" + path);
        }

        private void GenerateAndExport()
        {
            if (levelJson == null)
            {
                EditorUtility.DisplayDialog("按 Record 重排容器", "请先选择关卡 JSON。", "确定");
                return;
            }

            var data = LevelLoader.ParseJson(levelJson.text, levelJson.name);
            if (data == null)
            {
                EditorUtility.DisplayDialog("按 Record 重排容器", "关卡 JSON 解析失败，详见 Console。", "确定");
                return;
            }

            if (string.IsNullOrEmpty(recordPath))
            {
                EditorUtility.DisplayDialog("按 Record 重排容器", "请先选择 Record txt 文件。", "确定");
                return;
            }

            string segErr = ValidateSegments();
            if (segErr != null)
            {
                EditorUtility.DisplayDialog("按 Record 重排容器", segErr, "确定");
                return;
            }

            string recordText;
            try
            {
                recordText = File.ReadAllText(recordPath);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("按 Record 重排容器", "读取 Record 失败：\n" + e.Message, "确定");
                return;
            }

            if (!ContainerRearranger.TryParseRecord(recordText, out var seq))
            {
                EditorUtility.DisplayDialog("按 Record 重排容器", "Record 文件包含无法解析的行（应为每行一个整数 colorId）。", "确定");
                return;
            }

            string err = ContainerRearranger.Validate(data, seq);
            if (err != null)
            {
                EditorUtility.DisplayDialog("按 Record 重排容器", "校验失败：\n" + err, "确定");
                return;
            }

            var outData = ContainerRearranger.Rearrange(data, seq, segments, columnOverride, seed);

            string json = JsonUtility.ToJson(outData, true);

            string defaultDir = EditorPathMemory.LoadDir(ExportPathKey, "Assets/LevelData");
            string defaultName = levelJson.name + "_rearranged.json";
            string path = EditorUtility.SaveFilePanel("导出重排关卡 JSON", defaultDir, defaultName, "json");
            if (string.IsNullOrEmpty(path))
                return;
            EditorPathMemory.SaveDir(ExportPathKey, path);

            File.WriteAllText(path, json, new UTF8Encoding(false));
            AssetDatabase.Refresh();

            Debug.Log("[ContainerRearranger] 已导出重排关卡 JSON 到 " + path + "（容器 " +
                outData.container.items.Length + " 个，lockContainer=true）");

            EditorUtility.DisplayDialog("按 Record 重排容器",
                "已导出到：\n" + path + "\n\n请确保该文件位于 Assets 目录下，并在 GameManager.levelJsons 中按关卡序号引用。",
                "确定");
        }
    }
}
