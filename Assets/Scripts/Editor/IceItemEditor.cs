using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CrowdMatch
{
    /// <summary>
    /// IceItem 的 Inspector：显示格数 / 连通性 / 组内像素数 / 计数 / 缺素材 / 死锁提示，
    /// 并提供「重建显示」（改字段后立刻刷新冰面与计数位置）。
    /// </summary>
    [CustomEditor(typeof(IceItem))]
    public class IceItemEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var ice = (IceItem)target;
            var group = ice.GetComponentInParent<PixelGroup>();

            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();

            if (group == null)
            {
                EditorGUILayout.HelpBox("本冰组不在任何 PixelGroup 下：不会参与冰冻。", MessageType.Error);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("重建显示"))
            {
                group.RebuildGrid();     // 刷新冻结掩码等派生状态
                ice.BuildVisual(group);  // 重画冰面 + 重新定位/缩放计数
                EditorUtility.SetDirty(ice);
                EditorUtility.SetDirty(group);
                SceneView.RepaintAll();
            }
            if (GUILayout.Button("重新记录计数文字基准"))
            {
                Undo.RecordObject(ice, "重新记录计数文字基准");
                ice.RecacheTextMetrics();
                EditorUtility.SetDirty(ice);
                SceneView.RepaintAll();
            }
            EditorGUILayout.EndHorizontal();

            DrawIceInfo(group, ice);
            DrawFillClassReference();
        }

        /// <summary>体检信息：格数 / 连通性 / 组内像素 / 计数 / 缺素材 / 死锁。</summary>
        private static void DrawIceInfo(PixelGroup group, IceItem ice)
        {
            ice.RefreshCells();
            if (ice.CellCount == 0)
            {
                EditorGUILayout.HelpBox("冰组没有任何格。请用菜单「用选中 Pixel 通过包围线 / 通过多选 / " +
                    "通过矩形创建冰组」创建。", MessageType.Warning);
                return;
            }

            var list = new List<Vector2Int>(ice.CellSet);
            IceRegion.IsConnected(list, out string connErr);

            // 本模式（填充 · 单色）只需要 5 个单元号（0/1/3/4/8），逐个查素材配没配
            var required = CornerTileTable.RequiredTiles(CornerTileStyle.Fill, CornerTileStates.Single);
            var missing = new List<int>();
            foreach (var id in required)
                if (ice.sprites == null || id >= ice.sprites.Length || ice.sprites[id] == null)
                    missing.Add(id);

            int pixels = group.CountPixelsInIce(ice);
            int unfrozen = group.CountUnfrozenPixels();

            var sb = new System.Text.StringBuilder();
            sb.Append("格数 ").Append(ice.CellCount).Append("，组内像素 ").Append(pixels).Append(" 个。\n");
            sb.Append("冰冻计数 ").Append(ice.remaining < 0 ? ice.freezeCount : ice.remaining)
              .Append(" / ").Append(ice.freezeCount).Append("（归 0 化开）。");
            ice.GetTextMetrics(out bool recorded, out int baseFont, out Vector2 baseSize);
            sb.Append("\n计数文字基准：").Append(recorded
                ? "字号 " + baseFont + "，长宽 " + baseSize.x + "×" + baseSize.y
                : "尚未记录（下次刷新时自动记录）");
            sb.Append("；当前放大 ×").Append(ice.countFontScale).Append("。");
            if (ice.meltOnlyWhenExposed)
                sb.Append("\n已勾选「暴露才开始融化」：冰组暴露前不显示计数、也不递减。");

            var type = MessageType.Info;

            if (connErr != null)
            {
                sb.Append("\n").Append(connErr);
                type = MessageType.Warning;
            }

            if (missing.Count > 0)
            {
                sb.Append("\n缺 ").Append(missing.Count).Append(" 个单元素材（单元号 ")
                  .Append(string.Join(", ", missing.ConvertAll(x => x.ToString()).ToArray()))
                  .Append("）。本模式需要 ").Append(string.Join(", ", required.ConvertAll(x => x.ToString()).ToArray()))
                  .Append("。");
                type = MessageType.Warning;
            }

            if (Application.isPlaying && ice.remaining == 0)
            {
                sb.Append("\n已融化。");
            }
            else if (Application.isPlaying && pixels > 0 && unfrozen == 0)
            {
                sb.Append("\n全网格已经没有可点的像素来推进计数 → 卡死（计数永远不会归 0）。");
                type = MessageType.Warning;
            }

            EditorGUILayout.HelpBox(sb.ToString(), type);
        }

        /// <summary>本模式（填充 · 单色）用到的单元号含义，供美术按序配 sprites。</summary>
        private static bool _showClasses;

        private static void DrawFillClassReference()
        {
            var required = CornerTileTable.RequiredTiles(CornerTileStyle.Fill, CornerTileStates.Single);
            _showClasses = EditorGUILayout.Foldout(_showClasses,
                "本模式（填充 · 单色）用到的 " + required.Count + " 个单元号", true);
            if (!_showClasses)
                return;

            var names = CornerTileTable.FillClassNames;
            foreach (var id in required)
            {
                string desc = (id >= 0 && id < names.Length) ? names[id] : "";
                EditorGUILayout.LabelField(id.ToString(), desc);
            }
        }
    }

    /// <summary>
    /// 用选中的 PixelItem 创建冰组，两种方式：
    ///   · **通过包围线**（≥3 个）：按点选顺序连成一条首尾自动闭合的折线，冰组 = **内部 ∪ 包围线自身格**。
    ///   · **通过多选**（≥1 个）：选中的格本身就是冰组成员，创建时校验**四向连通**。
    ///
    /// 创建时**不清除**任何像素——冰下面本来就要有像素——所以没有快照 / 还原一说（与 GateCreator 相反）。
    /// </summary>
    public static class IceCreator
    {
        private const string Tag = "[IceCreator]";

        /// <summary>新建冰组的默认冰冻计数。</summary>
        private const int DefaultFreezeCount = 5;

        // ===== 方式一：包围线 =====

        [MenuItem("CrowdMatch/用选中 Pixel 通过包围线创建冰组", true)]
        private static bool ValidateCreateIceByLoop()
        {
            return CollectSelectedPixels().Count >= 3;
        }

        [MenuItem("CrowdMatch/用选中 Pixel 通过包围线创建冰组")]
        private static void CreateIceByLoop()
        {
            var pixels = CollectSelectedPixels();
            SelectionOrderTracker.LogState();

            const string title = "创建冰组（包围线）";
            if (pixels.Count < 3)
            {
                EditorUtility.DisplayDialog(title,
                    "请按顺序选中至少 3 个 PixelItem（依次点选连成包围线，最后一点会自动连回第一点闭合）。", "确定");
                return;
            }
            if (!ResolveGroup(pixels, title, out var group))
                return;

            var points = new List<Vector2>();
            foreach (var p in pixels)
                points.Add(new Vector2(p.gridX, p.gridZ));

            var loopCells = new HashSet<Vector2Int>();
            IceRegion.CollectLoopCells(points, loopCells);
            var interior = IceRegion.ComputeInteriorCells(group, loopCells);

            if (interior.Count == 0 &&
                !EditorUtility.DisplayDialog(title,
                    "这条包围线没有围出任何内部格（冰组将只包含包围线自身 " + loopCells.Count + " 格）。\n" +
                    "常见原因：折线贴着网格边界，或没有真正围成闭环。\n\n是否继续？",
                    "继续", "取消"))
                return;

            var all = new HashSet<Vector2Int>(loopCells);
            all.UnionWith(interior);

            Create(group, new List<Vector2Int>(all), "包围线", loopCells.Count, interior.Count);
        }

        // ===== 方式二：多选 =====

        [MenuItem("CrowdMatch/用选中 Pixel 通过多选创建冰组", true)]
        private static bool ValidateCreateIceBySelection()
        {
            return CollectSelectedPixels().Count >= 1;
        }

        [MenuItem("CrowdMatch/用选中 Pixel 通过多选创建冰组")]
        private static void CreateIceBySelection()
        {
            var pixels = CollectSelectedPixels();
            SelectionOrderTracker.LogState();

            const string title = "创建冰组（多选）";
            if (pixels.Count < 1)
            {
                EditorUtility.DisplayDialog(title, "请先选中 1 个或多个 PixelItem（它们就是冰组的成员格）。", "确定");
                return;
            }
            if (!ResolveGroup(pixels, title, out var group))
                return;

            var cells = new List<Vector2Int>();
            foreach (var p in pixels)
                cells.Add(new Vector2Int(p.gridX, p.gridZ));

            if (!IceRegion.IsConnected(cells, out string connErr))
            {
                EditorUtility.DisplayDialog(title, connErr, "确定");
                return;
            }

            var inRange = new List<Vector2Int>();
            foreach (var c in cells)
                if (group.IsInRange(c.x, c.y))
                    inRange.Add(c);

            Create(group, inRange, "多选", 0, 0);
        }

        // ===== 方式三：矩形（左上、右下）=====

        [MenuItem("CrowdMatch/用选中 Pixel 通过矩形创建冰组（左上、右下）", true)]
        private static bool ValidateCreateIceByRect()
        {
            return CollectSelectedPixels().Count == 2;
        }

        [MenuItem("CrowdMatch/用选中 Pixel 通过矩形创建冰组（左上、右下）")]
        private static void CreateIceByRect()
        {
            var pixels = CollectSelectedPixels();

            const string title = "创建矩形冰组";
            if (pixels.Count != 2)
            {
                EditorUtility.DisplayDialog(title, "请恰好选中 2 个 PixelItem（矩形的一对对角格）。", "确定");
                return;
            }
            if (!ResolveGroup(pixels, title, out var group))
                return;

            // 与 BoxCreator / ElevatorCreator 同写法：取 min/max 就是包围矩形，两点谁先点都可以。
            int colMin = Mathf.Min(pixels[0].gridX, pixels[1].gridX);
            int colMax = Mathf.Max(pixels[0].gridX, pixels[1].gridX);
            int rowMin = Mathf.Min(pixels[0].gridZ, pixels[1].gridZ);
            int rowMax = Mathf.Max(pixels[0].gridZ, pixels[1].gridZ);

            var cells = new List<Vector2Int>();
            for (int r = rowMin; r <= rowMax; r++)
                for (int c = colMin; c <= colMax; c++)
                    if (group.IsInRange(c, r))
                        cells.Add(new Vector2Int(c, r));

            Create(group, cells, "矩形", 0, 0);
        }

        // ===== 共用 =====

        /// <summary>校验选中的 Pixel 是否同属一个 PixelGroup、且该组配了 icePrefab。</summary>
        private static bool ResolveGroup(List<PixelItem> pixels, string title, out PixelGroup group)
        {
            group = null;
            if (pixels.Count == 0)
            {
                EditorUtility.DisplayDialog(title, "请先选中一个或多个 PixelItem。", "确定");
                return false;
            }

            group = pixels[0].GetComponentInParent<PixelGroup>();
            if (group == null)
            {
                EditorUtility.DisplayDialog(title, "选中的 Pixel 必须位于 PixelGroup 下。", "确定");
                return false;
            }

            foreach (var p in pixels)
            {
                if (p.GetComponentInParent<PixelGroup>() != group)
                {
                    EditorUtility.DisplayDialog(title, "选中的 Pixel 必须属于同一个 PixelGroup。", "确定");
                    return false;
                }
            }

            if (group.icePrefab == null)
            {
                EditorUtility.DisplayDialog(title,
                    "PixelGroup 未设置 icePrefab，请先指定冰组预制体（需自带 IceItem 组件）。", "确定");
                return false;
            }

            return true;
        }

        /// <summary>实例化冰组预制体、写入成员格，整段合并成一个 Undo 步骤。**不清除任何像素。**</summary>
        private static void Create(PixelGroup group, List<Vector2Int> cells, string how, int loopCount, int interiorCount)
        {
            string title = "创建冰组（" + how + "）";
            if (cells.Count == 0)
            {
                EditorUtility.DisplayDialog(title, "没有算出任何成员格。", "确定");
                return;
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("创建冰组");

            // 与关卡 JSON 导入共用同一条创建路径（PixelGroup.SpawnIce(LevelData.IceGroupData)），
            // 字段只在这里和 LevelLoader 各装一次，不会两头漏配。
            var cellVecs = new Vector2[cells.Count];
            for (int i = 0; i < cells.Count; i++)
                cellVecs[i] = new Vector2(cells[i].x, cells[i].y);

            var data = new LevelData.IceGroupData
            {
                cells = cellVecs,
                count = DefaultFreezeCount,
                meltWhenExposed = false,
                countOffset = IceItem.DefaultCountOffset,
                fontScale = 1f,
            };

            var ice = group.SpawnIce(data);
            if (ice == null)
            {
                Undo.CollapseUndoOperations(undoGroup);
                EditorUtility.DisplayDialog(title, "生成冰组失败（见 Console）。", "确定");
                return;
            }

            Undo.RegisterCreatedObjectUndo(ice.gameObject, "创建冰组");

            group.RebuildGrid();     // 登记冰组、刷新冻结掩码
            ice.BuildVisual(group);  // 生成冰面 + 定位/缩放计数数字

            EditorUtility.SetDirty(ice);
            EditorUtility.SetDirty(group);
            Undo.CollapseUndoOperations(undoGroup);

            Selection.activeGameObject = ice.gameObject;
            SceneView.RepaintAll();

            string detail = how == "包围线"
                ? "（包围线 " + loopCount + " 格 + 内部 " + interiorCount + " 格）"
                : "";
            Debug.Log(Tag + " 已创建冰组 " + ice.name + "：共 " + cells.Count + " 格" + detail +
                "，冰冻计数 " + ice.freezeCount + "。请在 Inspector 里调计数与「暴露才开始融化」。");
        }

        /// <summary>收集按点选顺序缓存的 GameObject 中的 PixelItem（去重）。支持选中 PixelItem 的子物体（向上查找父级组件）。</summary>
        private static List<PixelItem> CollectSelectedPixels()
        {
            var result = new List<PixelItem>();
            var seen = new HashSet<PixelItem>();
            foreach (var go in SelectionOrderTracker.Ordered)
            {
                if (go == null)
                    continue;
                var p = go.GetComponentInParent<PixelItem>();
                if (p != null && seen.Add(p))
                    result.Add(p);
            }
            return result;
        }
    }
}
