using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace CrowdMatch
{
    [CustomEditor(typeof(PixelGroup))]
    public class PixelGroupEditor : Editor
    {
        /// <summary>导入/导出 PNG 时每格像素边长（方阵网格，一格一个色块）。</summary>
        private const int CellSize = 32;

        public override void OnInspectorGUI()
        {
            var group = (PixelGroup)target;

            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "「生成网格」会删除现有 PixelItem 子物体，按当前参数重新生成 Sphere，并随机生成局部同色颜色分布。",
                MessageType.Info);

            if (GUILayout.Button("生成网格"))
            {
                GenerateGrid(group);
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "「重新生成颜色分布」不改动几何，仅按 colorIds / 最小连续 / 最大连续 重新随机分布颜色。",
                MessageType.Info);

            if (GUILayout.Button("重新生成颜色分布"))
            {
                AssignClusteredColors(group);
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "「统计颜色总数」输出当前网格每种颜色的总数，并检查是否被 3 整除（不能整除则显示余数）。",
                MessageType.Info);

            if (GUILayout.Button("统计颜色总数 (Debug.Log)"))
            {
                LogColorCounts(group);
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "「导出颜色」把当前网格配色导出为 PNG（每格 " + CellSize + "px 色块）；\n" +
                "「导入颜色」从 PNG 读回配色——每格需为正方形色块，尺寸为 columns×rows 的整数倍。",
                MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("导出颜色 (PNG)"))
            {
                ExportColors(group);
            }
            if (GUILayout.Button("导入颜色 (PNG)"))
            {
                ImportColors(group);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void GenerateGrid(PixelGroup group)
        {
            // 删除现有的 PixelItem 子物体
            for (int i = group.transform.childCount - 1; i >= 0; i--)
            {
                var child = group.transform.GetChild(i);
                if (child.GetComponent<PixelItem>() != null)
                    Undo.DestroyObjectImmediate(child.gameObject);
            }

            // 以自身为中心重新生成（仅几何 + 组件，颜色由 AssignClusteredColors 统一分配）
            for (int col = 0; col < group.columns; col++)
            {
                for (int row = 0; row < group.rows; row++)
                {
                    var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    go.name = "Pixel_" + row + "_" + col; // 命名 row_col：row 0 = 前排（Z 最大），col 0 = 最左（X 最小）
                    go.transform.SetParent(group.transform, false);
                    go.transform.localPosition = group.GetLocalPosition(col, row);
                    go.transform.localScale = Vector3.one * group.unitSize;

                    Undo.RegisterCreatedObjectUndo(go, "生成网格");

                    var item = Undo.AddComponent<PixelItem>(go);
                    item.gridX = col;
                    item.gridZ = row;
                }
            }

            AssignClusteredColors(group);

            EditorUtility.SetDirty(group);
        }

        /// <summary>
        /// 区域生长法生成局部同色分布：随机取种子格子与颜色，向相邻未分配格子扩张，
        /// 形成大小在 [minRunLength, maxRunLength] 之间的同色区域，直到填满网格。
        /// </summary>
        private void AssignClusteredColors(PixelGroup group)
        {
            var colorConfig = ColorConfigLocator.Find();

            // 收集有效颜色 ID（去重，过滤越界）
            var validIds = new List<int>();
            var seen = new HashSet<int>();
            if (group.colorIds != null)
            {
                foreach (var id in group.colorIds)
                {
                    if (id < 0) continue;
                    if (colorConfig != null && id >= colorConfig.Count) continue;
                    if (seen.Add(id)) validIds.Add(id);
                }
            }
            if (validIds.Count == 0)
            {
                int total = colorConfig != null ? colorConfig.Count : 24;
                for (int i = 0; i < total; i++) validIds.Add(i);
            }

            int minRun = Mathf.Max(1, group.minRunLength);
            int maxRun = Mathf.Max(minRun, group.maxRunLength);

            // 构建 item 网格
            var grid = new PixelItem[group.columns, group.rows];
            foreach (var item in group.GetComponentsInChildren<PixelItem>())
            {
                if (group.IsInRange(item.gridX, item.gridZ))
                    grid[item.gridX, item.gridZ] = item;
            }

            var cellColor = new int[group.columns, group.rows];
            var unassigned = new List<Vector2Int>();
            for (int c = 0; c < group.columns; c++)
            {
                for (int r = 0; r < group.rows; r++)
                {
                    cellColor[c, r] = -1;
                    if (grid[c, r] != null)
                        unassigned.Add(new Vector2Int(c, r));
                }
            }

            int[] dx = { 1, -1, 0, 0 };
            int[] dz = { 0, 0, 1, -1 };

            while (unassigned.Count > 0)
            {
                // 随机种子
                int seedIdx = Random.Range(0, unassigned.Count);
                Vector2Int seed = unassigned[seedIdx];
                unassigned.RemoveAt(seedIdx);

                int color = validIds[Random.Range(0, validIds.Count)];
                int targetSize = Random.Range(minRun, maxRun + 1);

                cellColor[seed.x, seed.y] = color;
                var frontier = new List<Vector2Int> { seed };
                int regionSize = 1;

                while (regionSize < targetSize && frontier.Count > 0)
                {
                    int fi = Random.Range(0, frontier.Count);
                    Vector2Int cur = frontier[fi];

                    // 收集该格子尚未分配的邻居
                    var candidates = new List<Vector2Int>();
                    for (int d = 0; d < 4; d++)
                    {
                        int nx = cur.x + dx[d];
                        int nz = cur.y + dz[d];
                        if (group.IsInRange(nx, nz) && grid[nx, nz] != null && cellColor[nx, nz] == -1)
                            candidates.Add(new Vector2Int(nx, nz));
                    }

                    if (candidates.Count == 0)
                    {
                        frontier.RemoveAt(fi);
                        continue;
                    }

                    Vector2Int next = candidates[Random.Range(0, candidates.Count)];
                    cellColor[next.x, next.y] = color;
                    regionSize++;
                    unassigned.Remove(next);
                    frontier.Add(next);
                }
            }

            // 应用到 PixelItem
            for (int c = 0; c < group.columns; c++)
            {
                for (int r = 0; r < group.rows; r++)
                {
                    var item = grid[c, r];
                    if (item == null) continue;

                    Undo.RecordObject(item, "Set Pixel Color");
                    var rend = item.GetComponent<Renderer>();
                    if (rend != null) Undo.RecordObject(rend, "Set Pixel Material");

                    item.colorId = cellColor[c, r];
                    item.ApplyMaterial(colorConfig);
                    EditorUtility.SetDirty(item);
                }
            }
        }

        /// <summary>统计当前网格每种颜色的总数，并输出是否被 3 整除（不能整除则显示余数）。</summary>
        private void LogColorCounts(PixelGroup group)
        {
            var items = group.GetComponentsInChildren<PixelItem>();
            var counts = new Dictionary<int, int>();
            foreach (var it in items)
            {
                if (it == null) continue;
                counts.TryGetValue(it.colorId, out int c);
                counts[it.colorId] = c + 1;
            }

            if (counts.Count == 0)
            {
                Debug.Log("[PixelGroup] 没有找到任何 PixelItem，请先「生成网格」。");
                return;
            }

            var config = ColorConfigLocator.Find();

            var ids = new List<int>(counts.Keys);
            ids.Sort();

            int notDivisible = 0;
            foreach (int id in ids)
            {
                int total = counts[id];
                int rem = total % 3;

                string label = "颜色 " + id;
                if (config != null)
                {
                    var mat = config.GetMaterial(id);
                    if (mat != null && !string.IsNullOrEmpty(mat.name))
                        label += "（" + mat.name + "）";
                }

                string verdict = rem == 0 ? "✓ 被 3 整除" : "✗ 余 " + rem;
                if (rem != 0) notDivisible++;

                Debug.Log("[PixelGroup] " + label + "：总数 " + total + "，" + verdict);
            }

            Debug.Log("[PixelGroup] 统计完成：共 " + ids.Count + " 种颜色，" +
                (notDivisible == 0 ? "全部能被 3 整除。" : notDivisible + " 种不能被 3 整除。"));
        }

        // ===== 颜色导入 / 导出 =====

        /// <summary>从 ColorConfig 构建纯色调色板（与 colorId 对齐）。</summary>
        private static Color[] BuildPalette(ColorConfig config)
        {
            if (config == null || config.materials == null)
                return new Color[0];
            var colors = new Color[config.materials.Length];
            for (int i = 0; i < config.materials.Length; i++)
            {
                var mat = config.materials[i];
                colors[i] = mat != null ? mat.color : Color.magenta;
            }
            return colors;
        }

        /// <summary>像素图导入/导出共用的「上次路径」EditorPrefs 键。</summary>
        private const string ColorPathKey = "CrowdMatch.PixelGroup.LastColorPath";

        private void ExportColors(PixelGroup group)
        {
            var config = ColorConfigLocator.Find();
            if (config == null)
            {
                EditorUtility.DisplayDialog("导出颜色", "未找到 ColorConfig，无法读取颜色。", "确定");
                return;
            }

            string path = EditorUtility.SaveFilePanel("导出颜色 PNG", EditorPathMemory.LoadDir(ColorPathKey), "PixelGroup.png", "png");
            if (string.IsNullOrEmpty(path))
                return;
            EditorPathMemory.SaveDir(ColorPathKey, path);

            group.RebuildGrid();

            // 导出：图片顶行 = 最前排（gridZ 0），底行 = 后排；左 = 最小 X（gridX 0）。
            // 即图片按 row_col 表格从上到下、从左到右读取（顶行 0_0 0_1 …，下行 1_0 …）。
            var tex = SquareGridColorTool.Export(
                group.columns, group.rows, CellSize,
                (col, row) =>
                {
                    var item = group.GetItem(col, group.rows - 1 - row);
                    return item != null ? (Color?)config.GetColor(item.colorId) : null;
                });

            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            Debug.Log("[PixelGroup] 已导出颜色到 " + path);
        }

        private void ImportColors(PixelGroup group)
        {
            var config = ColorConfigLocator.Find();
            if (config == null)
            {
                EditorUtility.DisplayDialog("导入颜色", "未找到 ColorConfig，无法映射颜色 ID。", "确定");
                return;
            }

            string path = EditorUtility.OpenFilePanel("导入颜色 PNG", EditorPathMemory.LoadDir(ColorPathKey), "png");
            if (string.IsNullOrEmpty(path))
                return;
            EditorPathMemory.SaveDir(ColorPathKey, path);

            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(path);
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("导入颜色", "读取文件失败：\n" + e.Message, "确定");
                return;
            }

            var tex = new Texture2D(2, 2);
            if (!tex.LoadImage(bytes))
            {
                Object.DestroyImmediate(tex);
                EditorUtility.DisplayDialog("导入颜色", "无法解码 PNG 图片。", "确定");
                return;
            }

            // 校验尺寸：每格应为正方形色块，且宽/高分别被列/行整除
            if (group.columns <= 0 || group.rows <= 0
                || tex.width % group.columns != 0
                || tex.height % group.rows != 0)
            {
                Object.DestroyImmediate(tex);
                EditorUtility.DisplayDialog("导入颜色",
                    "图片尺寸需为网格（" + group.columns + " 列 × " + group.rows + " 行）的整数倍色块。\n当前 " + tex.width + "×" + tex.height + "。",
                    "确定");
                return;
            }

            int cellSizeX = tex.width / group.columns;
            int cellSizeY = tex.height / group.rows;
            if (cellSizeX != cellSizeY || cellSizeX <= 0)
            {
                Object.DestroyImmediate(tex);
                EditorUtility.DisplayDialog("导入颜色",
                    "每格必须是正方形色块（宽/列 应等于 高/行）。\n当前每格 " + cellSizeX + "×" + cellSizeY + "px。",
                    "确定");
                return;
            }

            var palette = BuildPalette(config);
            var cells = SquareGridColorTool.Import(tex, cellSizeX, palette);

            group.RebuildGrid();

            int applied = 0;
            foreach (var (col, row, colorIndex) in cells)
            {
                if (colorIndex < 0)
                    continue;

                // 图片顶行 = 最前排（gridZ 0）：把工具按「底行 0」采样的 row 反转到 gridZ
                var item = group.GetItem(col, group.rows - 1 - row);
                if (item == null)
                    continue;

                Undo.RecordObject(item, "Import Pixel Color");
                var rend = item.GetComponent<Renderer>();
                if (rend != null)
                    Undo.RecordObject(rend, "Import Pixel Material");

                item.colorId = colorIndex;
                item.ApplyMaterial(config);
                EditorUtility.SetDirty(item);
                applied++;
            }

            Object.DestroyImmediate(tex);
            EditorUtility.SetDirty(group);

            Debug.Log("[PixelGroup] 已从 " + path + " 导入 " + applied + " 个格子颜色。");
        }
    }
}
