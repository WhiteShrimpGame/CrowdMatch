using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 编辑器工具：选一份关卡 JSON，**离线**算出「操作组 + 层级」，直接导出成运行时 Record 模式
    /// 产出的那种 txt（每行一个 colorId，按取出顺序）。不依赖场景、不进 Play。
    ///
    /// 口径与算法都在 <see cref="LevelGridBoard"/>；这里只负责选文件、排序 + 同级洗牌、落盘、自检。
    ///
    /// · 层级升序；**同一层级内的组随机洗牌**（组内顺序不变），所以每次生成结果可能不同。
    /// · 生成后跑 <c>ContainerRearranger.Validate</c> 自检总数与逐色数；对不上会先问再写。
    /// </summary>
    public static class RecordQuickGenerator
    {
        private const string Tag = "[RecordQuickGenerator]";
        private const string JsonPathKey = "CrowdMatch.RecordQuickGenerator.LastJsonPath";
        private const string OutPathKey = "CrowdMatch.RecordQuickGenerator.LastRecordPath";

        [MenuItem("CrowdMatch/关卡工具/快速生成 Record", false, MenuPriority.LevelTools + MenuPriority.Seg2)]
        public static void GenerateFromJson()
        {
            string defaultDir = EditorPathMemory.LoadDir(JsonPathKey, "Assets/LevelData");
            string jsonPath = EditorUtility.OpenFilePanel("选择关卡 JSON", defaultDir, "json");
            if (string.IsNullOrEmpty(jsonPath))
                return;
            EditorPathMemory.SaveDir(JsonPathKey, jsonPath);

            string json;
            try
            {
                json = File.ReadAllText(jsonPath);
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("快速生成 Record", "读取文件失败：\n" + e.Message, "确定");
                return;
            }

            var data = LevelLoader.ParseJson(json, Path.GetFileName(jsonPath));
            if (data == null)
            {
                EditorUtility.DisplayDialog("快速生成 Record", "JSON 解析失败，详见 Console。", "确定");
                return;
            }

            var board = new LevelGridBoard(data);
            if (board.Warning != null)
            {
                EditorUtility.DisplayDialog("快速生成 Record", board.Warning, "确定");
                return;
            }
            board.Solve();
            if (board.Warning != null)
            {
                EditorUtility.DisplayDialog("快速生成 Record", board.Warning, "确定");
                return;
            }
            if (board.TotalLines <= 0)
            {
                EditorUtility.DisplayDialog("快速生成 Record", "这份关卡算出来 0 个像素，没有可导出的内容。", "确定");
                return;
            }

            var seq = BuildSequence(board);
            int stuck = CountStuck(board);

            string validateError = ContainerRearranger.Validate(data, seq);
            if (validateError != null)
            {
                var msg = new StringBuilder(validateError);
                if (board.CellsSkippedButCountedByValidate > 0)
                    msg.Append("\n\n本次生成按运行时口径跳过了「箱子体 / 门格上的非负像素值」共 ")
                       .Append(board.CellsSkippedButCountedByValidate)
                       .Append(" 格 —— 运行时也不会在这些格上生成像素，但 Validate 不跳过它们，")
                       .Append("所以它的期望值会大这么多。这不是本次生成的问题。");
                msg.Append("\n\n仍然写入吗？");
                if (!EditorUtility.DisplayDialog("快速生成 Record", msg.ToString(), "仍然写入", "取消"))
                    return;
            }

            string levelName = Path.GetFileNameWithoutExtension(jsonPath);
            string defaultOutDir = EditorPathMemory.LoadDir(OutPathKey, RecordDir());
            string stamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string defaultName = "Record_" + levelName + "_total" + board.TotalLines + "_" + stamp +
                "_rec" + seq.Count + ".txt";
            string outPath = EditorUtility.SaveFilePanel("保存 Record", defaultOutDir, defaultName, "txt");
            if (string.IsNullOrEmpty(outPath))
                return;
            EditorPathMemory.SaveDir(OutPathKey, outPath);

            var text = new StringBuilder(seq.Count * 3);
            for (int i = 0; i < seq.Count; i++) text.Append(seq[i]).Append('\n');
            File.WriteAllText(outPath, text.ToString(), new UTF8Encoding(false));

            string reportPath = Path.Combine(
                Path.GetDirectoryName(outPath),
                Path.GetFileNameWithoutExtension(outPath) + "_tiers.txt");
            File.WriteAllText(reportPath, BuildTierReport(board, Path.GetFileName(jsonPath)), new UTF8Encoding(false));

            if (outPath.Replace('\\', '/').Contains("/Assets/"))
                AssetDatabase.Refresh();

            Debug.Log(Tag + " 已生成 " + outPath + "（" + seq.Count + " 行 / " + board.Groups.Count +
                " 个操作组 / 卡死 " + stuck + " 组）；层级表：" + reportPath);
            EditorUtility.DisplayDialog("快速生成 Record",
                "已生成：\n" + outPath + "\n\n层级表（同层级在 Record 里的先后是随机洗牌的）：\n" + reportPath +
                (stuck > 0 ? "\n\n注意：有 " + stuck + " 个操作组在所选口径下始终走不掉，已归入最后一档（见层级表）。" : ""),
                "确定");
        }

        /// <summary>Record 默认输出目录：与运行时一致 —— 工程目录（Assets 的上一级）下的 Record 文件夹。</summary>
        private static string RecordDir()
        {
            string projectDir = Path.GetDirectoryName(Application.dataPath);
            return Path.Combine(projectDir, "Record");
        }

        /// <summary>按层级升序展开成 Record 序列；**同一层级内的组随机洗牌**（组内顺序不变）。</summary>
        private static List<int> BuildSequence(LevelGridBoard board)
        {
            var byTier = GroupByTier(board);
            var rng = new System.Random(unchecked((int)System.DateTime.Now.Ticks));
            var seq = new List<int>(board.TotalLines);

            foreach (var kv in byTier)
            {
                var list = kv.Value;
                Shuffle(list, rng);
                for (int i = 0; i < list.Count; i++) AppendGroup(seq, list[i], board);
            }
            return seq;
        }

        /// <summary>
        /// 一个组写进 Record 的顺序：前排优先（gridZ 小）→ 同排靠中心优先 → 列号升序
        /// （与 <c>GameController.ResolveMatch</c> 的组内排序同口径）；倍率的 N 份紧挨着写。
        /// </summary>
        private static void AppendGroup(List<int> seq, LevelGridBoard.Group g, LevelGridBoard board)
        {
            if (g.cells.Count == 0)
            {
                // 箱子颜色组：落位不可预知，按元素数平铺（与运行时「每颗补记 N 份」的总条数一致）
                for (int k = 0; k < g.elements; k++) seq.Add(g.color);
                return;
            }

            var cells = new List<Vector2Int>(g.cells);
            float center = (board.Columns - 1) * 0.5f;
            cells.Sort((a, b) =>
            {
                int z = a.y.CompareTo(b.y);
                if (z != 0) return z;
                int d = Mathf.Abs(a.x - center).CompareTo(Mathf.Abs(b.x - center));
                if (d != 0) return d;
                return a.x.CompareTo(b.x);
            });

            for (int i = 0; i < cells.Count; i++)
            {
                int mult = board.MultiplierAt(cells[i].x, cells[i].y);
                for (int k = 0; k < mult; k++) seq.Add(g.color);
            }
        }

        private static SortedDictionary<int, List<LevelGridBoard.Group>> GroupByTier(LevelGridBoard board)
        {
            var byTier = new SortedDictionary<int, List<LevelGridBoard.Group>>();
            for (int i = 0; i < board.Groups.Count; i++)
            {
                var g = board.Groups[i];
                int tier = g.tier < 0 ? int.MaxValue : g.tier;
                if (!byTier.TryGetValue(tier, out var list))
                {
                    list = new List<LevelGridBoard.Group>();
                    byTier[tier] = list;
                }
                list.Add(g);
            }
            return byTier;
        }

        private static void Shuffle<T>(List<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                var tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }

        private static int CountStuck(LevelGridBoard board)
        {
            int n = 0;
            for (int i = 0; i < board.Groups.Count; i++)
                if (board.Groups[i].stuck) n++;
            return n;
        }

        private static string BuildTierReport(LevelGridBoard board, string levelFileName)
        {
            var sb = new StringBuilder();
            sb.Append("关卡：").Append(levelFileName).Append('\n');
            sb.Append("网格：").Append(board.Columns).Append(" 列 × ").Append(board.Rows).Append(" 排\n");
            sb.Append("Record 行数（= 像素总数，已按倍率展开）：").Append(board.TotalLines).Append('\n');
            sb.Append("操作组数：").Append(board.Groups.Count).Append('\n');
            if (board.CellsSkippedButCountedByValidate > 0)
                sb.Append("（另有箱子体/门格上的非负像素值 ").Append(board.CellsSkippedButCountedByValidate)
                  .Append(" 格：运行时与本次生成都跳过，Validate 不跳过）\n");
            sb.Append("\n层级 = 移出本组之前至少要移出的像素总数；轮次 = 被取走的第几轮。\n");
            sb.Append("管道波次不落在轮次里：第 k 波 = 第 0 波层级 + k × 单波容量（顺推）；\n");
            sb.Append("某组的路线若依赖某条管道，层级另加该管道「单波容量 × 波数」（表中标「管道代价」）。\n");
            sb.Append("箱内每个颜色一组，共享**一个**层级 = min over 直接相邻箱体的组 h（排除路线要经过本箱其他邻组的 h）\n");
            sb.Append("  of (h.层级 + h.像素数) = 某个邻组整组彻底走完的那一刻；\n");
            sb.Append("某组的路线若必须经过「箱子释放后会重新占满的那一带」（箱体格 ∪ 箱子邻组的相邻格），层级另加该箱子总容量（表中标「箱子代价」）。\n");
            sb.Append("同一层级内的组在 Record 里的先后是随机洗牌的。\n\n");

            foreach (var kv in GroupByTier(board))
            {
                var list = kv.Value;
                int r = -1;
                for (int i = 0; i < list.Count; i++)
                    if (list[i].round >= 0) { r = list[i].round; break; }

                int sum = 0;
                for (int i = 0; i < list.Count; i++) sum += list[i].elements;

                sb.Append("── 层级 ").Append(kv.Key)
                  .Append("（").Append(r >= 0 ? "第 " + r + " 轮" : "管道顺推").Append("，")
                  .Append(list.Count).Append(" 组，元素合计 ").Append(sum).Append("）\n");

                for (int i = 0; i < list.Count; i++)
                {
                    var g = list[i];
                    sb.Append("    ").Append(KindName(g.kind))
                      .Append(" 颜色=").Append(g.color)
                      .Append(" 元素=").Append(g.elements);
                    if (g.pipePenalty > 0) sb.Append(" 管道代价=+").Append(g.pipePenalty);
                    if (g.boxPenalty > 0) sb.Append(" 箱子代价=+").Append(g.boxPenalty);
                    sb.Append(" 格=").Append(CellsText(g));
                    if (g.note.Length > 0) sb.Append("  ").Append(g.note);
                    sb.Append('\n');
                }
            }
            return sb.ToString();
        }

        private static string KindName(LevelGridBoard.GroupKind kind)
        {
            switch (kind)
            {
                case LevelGridBoard.GroupKind.PipeWave: return "管道波";
                case LevelGridBoard.GroupKind.Elevator: return "升降台";
                case LevelGridBoard.GroupKind.BoxColor: return "箱子颜色";
                default: return "网格";
            }
        }

        private static string CellsText(LevelGridBoard.Group g)
        {
            if (g.cells.Count == 0) return "（落位不可预知）";
            var sb = new StringBuilder();
            int n = Mathf.Min(g.cells.Count, 16);
            for (int i = 0; i < n; i++)
                sb.Append('(').Append(g.cells[i].x).Append(',').Append(g.cells[i].y).Append(')');
            if (g.cells.Count > n) sb.Append("…共").Append(g.cells.Count).Append("格");
            return sb.ToString();
        }
    }
}
