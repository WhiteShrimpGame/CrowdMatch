# LevelSolver 归档

> 本文档归档了「关卡可解性求解器」的完整源码。该工具已从项目代码中移除（`Assets/Scripts/Editor/LevelSolver.cs` 已删除），
> 如需恢复，把下方代码保存为 `Assets/Scripts/Editor/LevelSolver.cs` 即可。

## 用途

编辑器菜单工具（`CrowdMatch/求解关卡可解性`），非运行时。加载一个关卡 JSON，做纯数学模拟——不真实加载关卡逻辑、不洗牌容器。
把像素按 4 连通同色分组，遍历所有取组顺序，统计正确/错误求解数，并打印第一个正确求解的路径。

## 求解模型（与游戏规则一致）

- **分组**：像素 4 连通同色为一组，记录颜色、数量、左上角像素坐标（minCol/minRow）、是否触及首排（gridZ==0）、阻挡列表（四方向相邻的其它组 id）。
- **可移除判定**：组触及首排，或四方向相邻的某个组已被移出（移出的组格子变空 → 相邻组被暴露）。
- **匹配**：每步移出一个可移除组后，其像素立即与容器匹配——第 0 列开始检查前排；前排满则后排前移继续，前排不满且不能匹配则去下一列。
  单个容器从残留序列里「顺序内找第一个同色」像素消耗。
- **残留**：未参与匹配的已取出像素保留为 FIFO 颜色序列，参与后续匹配。
- **剪枝**：某次移出导致残留数 ≥ 30，直接判为错误求解，不再继续。
- **枚举**：DFS 全枚举，找到第一个正确求解即中止；设有 50,000,000 节点安全阀。

## 依赖（恢复时需存在）

- `LevelData`（`Assets/Scripts/Core/LevelData.cs`）
- `LevelLoader.ParseJson(string, string)`（`Assets/Scripts/Core/LevelLoader.cs`）
- `EditorPathMemory`（`Assets/Scripts/Editor/EditorPathMemory.cs`）

## 源码

```csharp
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace CrowdMatch
{
    /// <summary>
    /// 关卡可解性求解器（编辑器，非运行时）。加载一个关卡 JSON，做纯数学模拟——不真实加载关卡逻辑、不洗牌容器。
    /// 把像素按 4 连通同色分组，遍历所有取组顺序，统计正确/错误求解数，并打印第一个正确求解的路径。
    /// 可移除判定与游戏一致：组触及首排，或四方向相邻的某个组已被移出（移出的组格子变空 → 相邻组被暴露）。
    /// </summary>
    public static class LevelSolver
    {
        private const string Tag = "[LevelSolver]";

        /// <summary>残留数 ≥ 该值即判为错误求解（不再继续移出其余组）。</summary>
        private const int ResidueLimit = 30;

        /// <summary>搜索节点数安全阀，防止关卡过大导致编辑器卡死。</summary>
        private const long MaxNodes = 50_000_000;

        private const string JsonPathKey = "CrowdMatch.LevelSolver.LastJsonPath";

        // ===== 数据模型 =====

        private class Group
        {
            public int id;
            public int color;
            public int count;
            public bool touchesFront;                 // 含 gridZ == 0 的格子
            public int minCol = int.MaxValue;
            public int minRow = int.MaxValue;
            public readonly List<int> blockers = new List<int>();   // 四方向相邻的其它组 id
        }

        private class State
        {
            public bool[] removed;      // 每组是否已移除
            public int[] remaining;     // 每个容器的剩余容量
            public int[] colFront;      // 每列当前前排容器在 colLists[col] 中的下标
            public List<int> residue;   // 残留颜色序列（未参与匹配的已取出像素）
            public int removedCount;    // 已移除组数
        }

        private class SolverResult
        {
            public long correct;
            public long wrong;
            public bool nodeLimitHit;
            public List<Group> firstPath;   // 第一个正确求解的取组顺序
        }

        // ===== 入口 =====

        [MenuItem("CrowdMatch/求解关卡可解性")]
        public static void SolveFromMenu()
        {
            string json = PickJson();
            if (string.IsNullOrEmpty(json))
                return;

            LevelData data = LevelLoader.ParseJson(json, "求解关卡");
            if (data == null)
                return;

            var result = Solve(data);
            if (result == null)
                return;

            if (result.firstPath != null)
            {
                Debug.Log(Tag + " ==== 第一个正确求解路径（共 " + result.firstPath.Count + " 步）====");
                for (int i = 0; i < result.firstPath.Count; i++)
                {
                    var g = result.firstPath[i];
                    Debug.Log(Tag + string.Format("  {0,3}. 颜色={1}  左上角(col={2}, row={3})  数量={4}",
                        i + 1, g.color, g.minCol, g.minRow, g.count));
                }
            }
            else
            {
                Debug.Log(Tag + " 未找到正确求解路径。");
            }

            Debug.Log(Tag + " 正确求解数=" + result.correct + "，错误求解数=" + result.wrong +
                (result.nodeLimitHit ? "（搜索节点数超限，结果不完整）" : ""));
        }

        private static SolverResult Solve(LevelData data)
        {
            // 1. 像素颜色网格（全满，无空格；颜色 0 也是有效色）
            int columns = Mathf.Max(1, data.pixel.columns);
            int mainRows = Mathf.Max(0, data.pixel.rows);
            int tailRows = Mathf.Max(0, data.pixel.tailRows);
            int totalRows = mainRows + tailRows;
            int expected = columns * totalRows;
            var cells = data.pixel.cells;
            if (cells == null || cells.Length < expected)
            {
                Debug.LogError(Tag + " 像素 cells 数量不足（需要 " + expected + "，实际 " +
                    (cells != null ? cells.Length : 0) + "）。");
                return null;
            }

            int[,] grid = new int[columns, totalRows];
            for (int r = 0; r < totalRows; r++)
                for (int c = 0; c < columns; c++)
                    grid[c, r] = cells[r * columns + c];

            // 2. 分组（4 连通同色）
            var groupId = new int[columns, totalRows];
            for (int c = 0; c < columns; c++)
                for (int r = 0; r < totalRows; r++)
                    groupId[c, r] = -1;

            var groups = new List<Group>();
            int[] dx = { 1, -1, 0, 0 };
            int[] dy = { 0, 0, 1, -1 };

            for (int c = 0; c < columns; c++)
            {
                for (int r = 0; r < totalRows; r++)
                {
                    if (groupId[c, r] >= 0)
                        continue;

                    int color = grid[c, r];
                    var g = new Group { id = groups.Count, color = color };
                    var queue = new Queue<Vector2Int>();
                    queue.Enqueue(new Vector2Int(c, r));
                    groupId[c, r] = g.id;

                    while (queue.Count > 0)
                    {
                        var cur = queue.Dequeue();
                        g.count++;
                        if (cur.x < g.minCol) g.minCol = cur.x;
                        if (cur.y < g.minRow) g.minRow = cur.y;
                        if (cur.y == 0) g.touchesFront = true;

                        for (int d = 0; d < 4; d++)
                        {
                            int nx = cur.x + dx[d];
                            int ny = cur.y + dy[d];
                            if (nx < 0 || nx >= columns || ny < 0 || ny >= totalRows) continue;
                            if (groupId[nx, ny] >= 0) continue;
                            if (grid[nx, ny] != color) continue;
                            groupId[nx, ny] = g.id;
                            queue.Enqueue(new Vector2Int(nx, ny));
                        }
                    }

                    groups.Add(g);
                }
            }

            // 3. 组间相邻（阻挡列表）：检查每个格子的右、下，覆盖所有相邻对
            for (int c = 0; c < columns; c++)
            {
                for (int r = 0; r < totalRows; r++)
                {
                    int id = groupId[c, r];
                    if (c + 1 < columns)
                    {
                        int nb = groupId[c + 1, r];
                        if (nb != id) { AddBlocker(groups[id], nb); AddBlocker(groups[nb], id); }
                    }
                    if (r + 1 < totalRows)
                    {
                        int nb = groupId[c, r + 1];
                        if (nb != id) { AddBlocker(groups[id], nb); AddBlocker(groups[nb], id); }
                    }
                }
            }

            // 4. 容器：每列由前（y 小）到后（y 大）有序列表
            int cCols = Mathf.Max(1, data.container.columns);
            int cRows = Mathf.Max(1, data.container.rows);
            var colLists = new List<int>[cCols];
            for (int i = 0; i < cCols; i++) colLists[i] = new List<int>();

            var contColor = new List<int>();
            var contCap = new List<int>();
            var contRow = new List<int>();

            if (data.container.items != null)
            {
                var items = new List<LevelData.ContainerItemData>(data.container.items);
                items.Sort((a, b) => { int yc = a.y.CompareTo(b.y); return yc != 0 ? yc : a.x.CompareTo(b.x); });
                foreach (var it in items)
                {
                    if (it.x < 0 || it.x >= cCols || it.y < 0 || it.y >= cRows)
                        continue;   // 越界忽略
                    int idx = contColor.Count;
                    contColor.Add(it.colorId);
                    contCap.Add(Mathf.Max(0, it.capacity));
                    contRow.Add(it.y);
                    colLists[it.x].Add(idx);
                }
                for (int i = 0; i < cCols; i++)
                    colLists[i].Sort((a, b) => contRow[a].CompareTo(contRow[b]));
            }

            // 摘要
            int totalPixels = columns * totalRows;
            int totalCap = 0;
            foreach (var cap in contCap) totalCap += cap;
            Debug.Log(Tag + string.Format(" 像素 {0}×{1}，共 {2} 个组；容器 {3} 个（总容量 {4}，像素总数 {5}）。",
                columns, totalRows, groups.Count, contCap.Count, totalCap, totalPixels));
            if (totalPixels > totalCap)
                Debug.LogWarning(Tag + " 像素总数 > 容器总容量，理论上不可能全部吸收，结果应全为错误。");

            // 5. DFS 全枚举
            var result = new SolverResult();
            var state = new State
            {
                removed = new bool[groups.Count],
                remaining = contCap.ToArray(),
                colFront = new int[cCols],
                residue = new List<int>(),
                removedCount = 0,
            };
            long nodeCount = 0;
            var path = new List<Group>();

            Dfs(groups, colLists, cCols, contColor.ToArray(), state, ref nodeCount, result, path);

            return result;
        }

        // ===== 搜索 =====

        private static void Dfs(List<Group> groups, List<int>[] colLists, int colCount, int[] contColor,
            State state, ref long nodeCount, SolverResult result, List<Group> path)
        {
            // 已找到第一个正确求解 → 中止（统计逻辑保留但不再执行，找到一组即停）
            if (result.firstPath != null)
                return;

            nodeCount++;
            if (nodeCount > MaxNodes)
            {
                result.nodeLimitHit = true;
                return;
            }

            // 某次移出导致残留数 ≥ 阈值 → 错误，剪枝
            if (state.residue.Count >= ResidueLimit)
            {
                result.wrong++;
                return;
            }

            var removable = new List<Group>();
            for (int i = 0; i < groups.Count; i++)
            {
                if (state.removed[i]) continue;
                var g = groups[i];
                if (g.touchesFront || HasRemovedBlocker(g, state.removed))
                    removable.Add(g);
            }

            if (removable.Count == 0)
            {
                if (state.removedCount == groups.Count)
                {
                    if (state.residue.Count == 0)
                    {
                        result.correct++;
                        if (result.firstPath == null)
                            result.firstPath = new List<Group>(path);
                    }
                    else
                    {
                        result.wrong++;   // 全部组已移除但残留未清 0
                    }
                }
                else
                {
                    result.wrong++;   // 死锁：还有组但无可移组
                }
                return;
            }

            foreach (var g in removable)
            {
                var ns = CloneState(state);
                ns.removed[g.id] = true;
                ns.removedCount++;
                for (int k = 0; k < g.count; k++)
                    ns.residue.Add(g.color);
                Match(ns, colLists, colCount, contColor);

                path.Add(g);
                Dfs(groups, colLists, colCount, contColor, ns, ref nodeCount, result, path);
                path.RemoveAt(path.Count - 1);
            }
        }

        /// <summary>
        /// 匹配：第 0 列开始检查前排；前排满则后排前移继续，前排不满且不能匹配则去下一列。
        /// 单个容器从残留里「顺序内找第一个同色」像素消耗。
        /// </summary>
        private static void Match(State s, List<int>[] colLists, int colCount, int[] contColor)
        {
            for (int col = 0; col < colCount; col++)
            {
                var list = colLists[col];
                while (s.colFront[col] < list.Count)
                {
                    int ci = list[s.colFront[col]];
                    int idx = s.residue.IndexOf(contColor[ci]);
                    if (idx < 0)
                        break;   // 前排不满且不能匹配 → 下一列

                    s.residue.RemoveAt(idx);
                    s.remaining[ci]--;
                    if (s.remaining[ci] <= 0)
                        s.colFront[col]++;   // 耗尽 → 后排前移
                }
            }
        }

        private static bool HasRemovedBlocker(Group g, bool[] removed)
        {
            for (int i = 0; i < g.blockers.Count; i++)
                if (removed[g.blockers[i]])
                    return true;
            return false;
        }

        private static void AddBlocker(Group g, int otherId)
        {
            if (!g.blockers.Contains(otherId))
                g.blockers.Add(otherId);
        }

        private static State CloneState(State s)
        {
            return new State
            {
                removed = (bool[])s.removed.Clone(),
                remaining = (int[])s.remaining.Clone(),
                colFront = (int[])s.colFront.Clone(),
                residue = new List<int>(s.residue),
                removedCount = s.removedCount,
            };
        }

        // ===== JSON 选择 =====

        private static string PickJson()
        {
            var ta = Selection.activeObject as TextAsset;
            if (ta != null && !string.IsNullOrEmpty(ta.text))
                return ta.text;

            string defaultDir = EditorPathMemory.LoadDir(JsonPathKey, "Assets/LevelData");
            string path = EditorUtility.OpenFilePanel("选择关卡 JSON", defaultDir, "json");
            if (string.IsNullOrEmpty(path))
                return null;
            EditorPathMemory.SaveDir(JsonPathKey, path);

            try
            {
                return File.ReadAllText(path);
            }
            catch (System.Exception e)
            {
                Debug.LogError(Tag + " 读取 JSON 失败：" + e.Message);
                return null;
            }
        }
    }
}
```
