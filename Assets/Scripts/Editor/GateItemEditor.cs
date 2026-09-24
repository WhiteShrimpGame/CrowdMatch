using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CrowdMatch
{
    /// <summary>
    /// GateItem 的 Inspector：显示线段校验、格数、闭合状态与倍乘带来的额外像素数；
    /// 提供「重建显示」（改字段后立刻刷新可见门）与「移除倍乘门并还原 Pixel」。
    /// </summary>
    [CustomEditor(typeof(GateItem))]
    public class GateItemEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var gate = (GateItem)target;
            var group = gate.GetComponentInParent<PixelGroup>();

            // 记住改动前的值：Inspector 里一改就重建显示，可见门的长度与倍数数字立刻跟着变
            Vector2 prevStart = gate.start;
            Vector2 prevEnd = gate.end;
            int prevMultiplier = gate.multiplier;

            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            bool changed = prevStart != gate.start
                        || prevEnd != gate.end
                        || prevMultiplier != gate.multiplier;

            if (changed && Application.isPlaying)
            {
                // 播放中改字段不做场景重建（会动到正在跑的网格状态），只提示
                EditorGUILayout.HelpBox("Play 模式下改了倍乘门字段：需要停止播放后重新进入才会生效。", MessageType.Info);
            }
            else if (changed)
            {
                Rebuild(group, gate);
            }

            EditorGUILayout.Space();

            if (group == null)
            {
                EditorGUILayout.HelpBox("本门不在任何 PixelGroup 下：不会参与寻路与倍乘。", MessageType.Error);
                return;
            }

            if (!gate.IsValid(out string segErr))
                EditorGUILayout.HelpBox(segErr, MessageType.Error);

            DrawGateInfo(group, gate);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("重建显示"))
            {
                Rebuild(group, gate);
            }
            if (GUILayout.Button("移除倍乘门并还原 Pixel"))
            {
                if (EditorUtility.DisplayDialog("移除倍乘门",
                    "将移除这道倍乘门，并按创建时记录的快照把门格上的 Pixel 还原回原位。\n是否继续？",
                    "移除并还原", "取消"))
                {
                    RemoveAndRestore(group, gate);
                }
            }
            EditorGUILayout.EndHorizontal();

            DrawPillarRecapture(group, gate);
        }

        /// <summary>
        /// 左右柱基准位置的重记入口。基准字段是 HideInInspector，没有别的修正途径，
        /// 所以给一个按钮（同 PipeItem 的「重新记录剩余波次数字的基准位置」）。
        /// </summary>
        private static void DrawPillarRecapture(PixelGroup group, GateItem gate)
        {
            if (gate.leftPillar == null && gate.rightPillar == null)
                return;

            EditorGUILayout.Space();

            if (GUILayout.Button("重新记录左右柱基准位置"))
            {
                gate.RecapturePillarBase();
                Rebuild(group, gate);
            }

            EditorGUILayout.HelpBox(
                "把两根柱子**当前**的局部位置记为「基准」（= 偏移量为 0 时该在的位置），" +
                "之后按「基准 + 延展轴偏移」摆放，偏移量 = (格数-1)/2 × 该轴向的实际格距。\n" +
                "柱子看起来偏了想校正时：先把它摆到「只有 1 格门」时的位置，再点这里。",
                MessageType.Info);
        }

        /// <summary>闭合状态 / 区域内像素 / 倍乘额外像素的体检信息。</summary>
        private static void DrawGateInfo(PixelGroup group, GateItem gate)
        {
            // 掩码由 RebuildGrid 填；这里不自动重建，避免 Inspector 每帧都跑一遍区域 BFS
            if (gate.regionMask == null)
            {
                EditorGUILayout.HelpBox("尚无闭合区域数据：点一下「重建显示」刷新。", MessageType.Info);
                return;
            }

            int regionCells = gate.RegionCellCount();
            if (regionCells == 0)
            {
                EditorGUILayout.HelpBox(
                    "未围出闭合区域：区域内没有任何格，这道门不会倍乘任何像素。\n" +
                    "请用墙（或管道）配合这道门把要倍乘的像素围成封闭区间。",
                    MessageType.Warning);
                return;
            }

            int regionPixels = group.CountPixelsInRegion(gate);
            int extra = group.CountGateExtraPixels();
            EditorGUILayout.HelpBox(
                "格数 " + gate.CellCount + "，闭合区域 " + regionCells + " 格（其中静态像素 " + regionPixels + " 个）。\n" +
                "区域内像素离开时必须穿过门格：每颗 × " + Mathf.Max(1, gate.multiplier) +
                "，全关倍乘额外像素共 " + extra + " 个。",
                MessageType.Info);
        }

        /// <summary>把网格与可见表现刷新一遍（改字段 / 手动重建都走这里）。</summary>
        private static void Rebuild(PixelGroup group, GateItem gate)
        {
            if (group == null)
            {
                gate.BuildVisual(null);
                return;
            }

            group.RebuildGrid();                 // 让门格表 / 闭合区域掩码 / 倍率图跟上新字段
            gate.BuildVisual(group);             // 定位 + 本体缩放 + 倍数数字
            EditorUtility.SetDirty(gate);
            EditorUtility.SetDirty(group);
            SceneView.RepaintAll();              // Gizmos 里的区域色块也一起刷新
        }

        /// <summary>
        /// 移除倍乘门并按 <see cref="GateItem.clearedPixels"/> 快照还原 Pixel。
        /// 还原 + 销毁合并成一个 Undo 步骤，整段可一次撤销。
        /// </summary>
        private static void RemoveAndRestore(PixelGroup group, GateItem gate)
        {
            var go = gate.gameObject;

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("移除倍乘门并还原 Pixel");

            if (group != null)
                group.RebuildGrid();   // 取当前占用状态，避免生成的 Pixel 与已有像素重叠

            var config = ColorConfigLocator.Find();
            int restored = 0;
            if (group != null && gate.clearedPixels != null)
            {
                foreach (var snap in gate.clearedPixels)
                {
                    if (!group.IsInRange(snap.col, snap.row))
                        continue;
                    if (group.GetItem(snap.col, snap.row) != null)
                        continue;   // 该格已经有像素（后来手工填过）：不重复生成

                    var item = group.SpawnPixel(snap.col, snap.row, snap.colorId, config, false, snap.isQuestion);
                    if (item == null)
                        continue;
                    Undo.RegisterCreatedObjectUndo(item.gameObject, "还原 Pixel");
                    restored++;
                }
            }

            Undo.DestroyObjectImmediate(go);

            if (group != null)
            {
                group.RebuildGrid();
                EditorUtility.SetDirty(group);
            }

            Undo.CollapseUndoOperations(undoGroup);
            SceneView.RepaintAll();

            Debug.Log("[GateItemEditor] 已移除倍乘门 " + go.name + "，还原 Pixel " + restored + " 个。");
        }
    }

    /// <summary>
    /// 用选中的两个 PixelItem 创建倍乘门：按**点选顺序**，第 1 个 = 起点格，第 2 个 = 终点格。
    /// 创建时清掉门格上的 Pixel 并记录快照（供「移除并还原」回填）。
    /// **创建时只校验线段本身（轴对齐、≥2 格），不校验闭合**——闭合由「校验倍乘门」与「生成 Containers」检查。
    /// </summary>
    public static class GateCreator
    {
        private const string Tag = "[GateCreator]";

        [MenuItem("CrowdMatch/创建（场景视图 · 选中 Pixel）/倍乘门", true, MenuPriority.Create + MenuPriority.Seg1 + 2)]
        private static bool ValidateCreateGateFromSelection()
        {
            return CollectSelectedPixels().Count >= 2;
        }

        [MenuItem("CrowdMatch/创建（场景视图 · 选中 Pixel）/倍乘门", false, MenuPriority.Create + MenuPriority.Seg1 + 2)]
        private static void CreateGateFromSelection()
        {
            var pixels = CollectSelectedPixels();
            SelectionOrderTracker.LogState();
            if (pixels.Count < 2)
            {
                EditorUtility.DisplayDialog("创建倍乘门",
                    "请按顺序选中 2 个 PixelItem（第 1 个 = 起点格，第 2 个 = 终点格）。", "确定");
                return;
            }

            var group = pixels[0].GetComponentInParent<PixelGroup>();
            if (group == null)
            {
                EditorUtility.DisplayDialog("创建倍乘门", "选中的 Pixel 必须位于 PixelGroup 下。", "确定");
                return;
            }

            foreach (var p in pixels)
            {
                if (p.GetComponentInParent<PixelGroup>() != group)
                {
                    EditorUtility.DisplayDialog("创建倍乘门", "选中的 Pixel 必须属于同一个 PixelGroup。", "确定");
                    return;
                }
            }

            var start = new Vector2(pixels[0].gridX, pixels[0].gridZ);
            var end = new Vector2(pixels[1].gridX, pixels[1].gridZ);

            if (!GateItem.IsValidSegment(start, end, out string segErr))
            {
                EditorUtility.DisplayDialog("创建倍乘门", segErr, "确定");
                return;
            }

            if (group.gatePrefab == null)
            {
                EditorUtility.DisplayDialog("创建倍乘门",
                    "PixelGroup 未设置 gatePrefab，请先指定倍乘门预制体（需自带 GateItem 组件）。", "确定");
                return;
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("创建倍乘门");

            // 实例化门预制体（预制体资产走 InstantiatePrefab，场景对象走 Object.Instantiate 克隆）
            GameObject gateGo;
            if (PrefabUtility.GetPrefabAssetType(group.gatePrefab) == PrefabAssetType.NotAPrefab)
                gateGo = (GameObject)Object.Instantiate(group.gatePrefab, group.transform);
            else
                gateGo = (GameObject)PrefabUtility.InstantiatePrefab(group.gatePrefab, group.transform);

            gateGo.name = "Gate_" + (group.transform.childCount + 1);

            var gate = gateGo.GetComponent<GateItem>();
            if (gate == null)
            {
                EditorUtility.DisplayDialog("创建倍乘门", "gatePrefab 缺少 GateItem 组件。", "确定");
                Object.DestroyImmediate(gateGo);
                return;
            }

            gate.start = start;
            gate.end = end;
            gate.multiplier = 2;   // 默认倍数
            gate.group = group;

            Undo.RegisterCreatedObjectUndo(gateGo, "创建倍乘门");

            // 清掉门格上的 Pixel，并把它们记进快照（移除时可原样还原）
            group.RebuildGrid();
            gate.clearedPixels = new List<GateItem.ClearedPixel>();
            foreach (var cell in gate.cells)
            {
                var item = group.GetItem(cell.x, cell.y);
                if (item == null)
                    continue;

                gate.clearedPixels.Add(new GateItem.ClearedPixel
                {
                    col = cell.x,
                    row = cell.y,
                    colorId = item.colorId,
                    isQuestion = item.isQuestion,
                });
                Undo.DestroyObjectImmediate(item.gameObject);
            }

            group.RebuildGrid();       // 清格后重建（占用表 + 门格表 + 闭合区域）
            gate.BuildVisual(group);   // 定位到整段中心、本体按格数缩放、数字显示 x2

            EditorUtility.SetDirty(gate);
            EditorUtility.SetDirty(group);
            Undo.CollapseUndoOperations(undoGroup);

            Selection.activeGameObject = gateGo;
            SceneView.RepaintAll();

            Debug.Log(Tag + " 已创建倍乘门：(" + start.x + "," + start.y + ") → (" + end.x + "," + end.y + ")，" +
                gate.CellCount + " 格，倍率 x" + gate.multiplier + "，清除 Pixel " + gate.clearedPixels.Count + " 个。" +
                "请在 Inspector 里设定倍数，并用「校验倍乘门」确认已围成闭合区域。");
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
