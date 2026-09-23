using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CrowdMatch
{
    /// <summary>
    /// 木箱 Inspector：区域尺寸校验、覆盖像素数、拆箱进度、与其它机制的冲突提示。
    /// 木箱只在编辑器里「选左上、右下」创建，创建时不删除任何像素（与箱子相反）。
    /// </summary>
    [CustomEditor(typeof(CrateItem))]
    public class CrateItemEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var crate = (CrateItem)target;
            var pg = crate.GetComponentInParent<PixelGroup>();

            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            if (pg == null)
            {
                EditorGUILayout.HelpBox("未找到父级 PixelGroup。", MessageType.Warning);
                return;
            }

            // 墙 / 管道 / 冰 / 已有木箱的掩码要先刷新，下面的冲突检查才准
            pg.RebuildGrid();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("区域", crate.ColCount + " × " + crate.RowCount + " 格");

            if (!crate.IsValidSize)
            {
                EditorGUILayout.HelpBox("木箱必须是完整的矩形，且长宽都至少为 2（当前 " +
                    crate.ColCount + "×" + crate.RowCount + "）。", MessageType.Error);
            }

            string conflict = FindConflict(pg, crate.colMin, crate.rowMin, crate.colMax, crate.rowMax, crate);
            if (conflict != null)
            {
                EditorGUILayout.HelpBox("区域内存在冲突：" + conflict +
                    "。木箱要求覆盖一个完整的矩形范围。", MessageType.Warning);
            }

            EditorGUILayout.LabelField("覆盖像素数", pg.CountPixelsUnderCrate(crate).ToString());

            // 死锁提示：网格上的像素**全部**被木箱盖住 → 没有任何能点的像素，
            // 拆箱次数永远不会推进，关卡卡死。只可能来自关卡数据，所以只提示不修正。
            int onGrid = 0;
            int covered = 0;
            for (int c = 0; c < pg.columns; c++)
            {
                for (int r = 0; r < pg.TotalRows; r++)
                {
                    if (pg.grid == null || pg.grid[c, r] == null)
                        continue;
                    onGrid++;
                    if (pg.IsCrateCell(c, r))
                        covered++;
                }
            }
            if (onGrid > 0 && onGrid == covered)
            {
                EditorGUILayout.HelpBox("网格上的 " + onGrid +
                    " 颗像素全部被木箱盖住：没有任何可点的像素，拆箱次数永远不会推进（关卡死锁）。",
                    MessageType.Error);
            }

            EditorGUILayout.LabelField("拆箱进度",
                crate.movedOutCount + " / " + Mathf.Max(1, crate.destroyAfterMoves) +
                "（还需 " + crate.RemainingMoves + " 次相邻移出）");

            // 计数表现：封条条数 = 次数 − 1（上限 2）。次数超出时要说清楚，否则「点了没反应」找不到原因。
            int moves = Mathf.Max(1, crate.destroyAfterMoves);
            int sealCount = crate.SealCount;
            EditorGUILayout.LabelField("封条数", sealCount + "（次数 " + moves + " − 1，上限 2）");

            if (!crate.destroyed && sealCount > 0 &&
                (pg.crateSealPrefab == null || pg.crateNailPrefab == null))
            {
                EditorGUILayout.HelpBox("PixelGroup 上未配 crateSealPrefab / crateNailPrefab，" +
                    "木箱不会拼封条与钉子（拆箱逻辑不受影响）。", MessageType.Warning);
            }
            else if (!crate.destroyed && moves - 1 > sealCount)
            {
                EditorGUILayout.HelpBox("次数 " + moves + " > 3：封条最多 2 条，前 " + (moves - 1 - sealCount) +
                    " 次点击只减次数、不摘封条（点上去视觉上没有反应）。", MessageType.Warning);
            }

            if (crate.destroyed)
                EditorGUILayout.HelpBox("已拆掉：不再占格、不再盖像素。", MessageType.Info);

            EditorGUILayout.Space();
            if (GUILayout.Button("重建显示（重拼木箱 + 刷新遮盖）"))
            {
                Undo.RegisterFullObjectHierarchyUndo(crate.gameObject, "重建木箱显示");
                crate.BuildVisual(pg);
                pg.RebuildGrid();
                EditorUtility.SetDirty(pg);
                EditorUtility.SetDirty(crate);
                SceneView.RepaintAll();
            }
        }

        /// <summary>
        /// 检查矩形区域能否放木箱：返回冲突描述（含冲突格），null = 可以。
        /// 木箱要求覆盖一个**完整**的矩形范围，所以区域里不能有墙 / 管道 / 门格 / 箱子 / 冰组格 / 另一木箱。
        /// <paramref name="ignore"/> 用于检查**已存在**的木箱（忽略它自己）。
        /// </summary>
        public static string FindConflict(PixelGroup pg, int cmin, int rmin, int cmax, int rmax, CrateItem ignore)
        {
            if (pg == null)
                return null;

            for (int r = rmin; r <= rmax; r++)
            {
                for (int c = cmin; c <= cmax; c++)
                {
                    if (!pg.IsInRange(c, r))
                        return "格 (" + c + "," + r + ") 越出网格范围";
                    if (pg.IsWall(c, r))
                        return "格 (" + c + "," + r + ") 是墙体";
                    if (pg.IsPipe(c, r))
                        return "格 (" + c + "," + r + ") 是管道";
                    if (pg.IsBox(c, r))
                        return "格 (" + c + "," + r + ") 是箱子";
                    if (pg.IsGateCell(c, r))
                        return "格 (" + c + "," + r + ") 是倍乘门门格（门格必须能通行，盖住会堵死）";
                    if (pg.IsFrozenCell(c, r))
                        return "格 (" + c + "," + r + ") 落在冰组内";

                    // 另一木箱：走木箱列表而不是 crateMask，这样才能忽略自己
                    if (pg.crates != null)
                    {
                        for (int i = 0; i < pg.crates.Count; i++)
                        {
                            var other = pg.crates[i];
                            if (other != null && other != ignore && other.IsInBody(c, r))
                                return "格 (" + c + "," + r + ") 已被另一个木箱覆盖";
                        }
                    }
                }
            }
            return null;
        }
    }

    /// <summary>
    /// 用选中的两个 PixelItem 作为左上、右下创建木箱（与选中顺序无关，取 min/max 归一化）。
    /// **不删除**区域内的像素 —— 木箱是盖在上面。
    /// </summary>
    public static class CrateCreator
    {
        [MenuItem("CrowdMatch/创建（场景视图 · 选中 Pixel）/木箱（左上、右下）", true, MenuPriority.Create + MenuPriority.Seg2 + 1)]
        private static bool ValidateCreateCrateFromSelection() => CollectSelectedPixels().Count == 2;

        [MenuItem("CrowdMatch/创建（场景视图 · 选中 Pixel）/木箱（左上、右下）", false, MenuPriority.Create + MenuPriority.Seg2 + 1)]
        private static void CreateCrateFromSelection()
        {
            var pixels = CollectSelectedPixels();
            if (pixels.Count != 2)
            {
                EditorUtility.DisplayDialog("创建木箱", "请恰好选中 2 个 PixelItem（左上、右下）。", "确定");
                return;
            }

            var group = pixels[0].GetComponentInParent<PixelGroup>();
            if (group == null || pixels[1].GetComponentInParent<PixelGroup>() != group)
            {
                EditorUtility.DisplayDialog("创建木箱", "选中的 Pixel 必须位于同一个 PixelGroup 下。", "确定");
                return;
            }

            int cmin = Mathf.Min(pixels[0].gridX, pixels[1].gridX);
            int rmin = Mathf.Min(pixels[0].gridZ, pixels[1].gridZ);
            int cmax = Mathf.Max(pixels[0].gridX, pixels[1].gridX);
            int rmax = Mathf.Max(pixels[0].gridZ, pixels[1].gridZ);

            if (cmax - cmin + 1 < 2 || rmax - rmin + 1 < 2)
            {
                EditorUtility.DisplayDialog("创建木箱", "木箱长宽都必须至少为 2。", "确定");
                return;
            }

            // 掩码先刷新，冲突检查才准
            group.RebuildGrid();

            string conflict = CrateItemEditor.FindConflict(group, cmin, rmin, cmax, rmax, null);
            if (conflict != null)
            {
                EditorUtility.DisplayDialog("创建木箱",
                    "木箱要求覆盖一个完整的矩形范围，但 " + conflict + "。", "确定");
                return;
            }

            Undo.SetCurrentGroupName("创建木箱");
            int undoGroup = Undo.GetCurrentGroup();

            var go = new GameObject("Crate_" + rmin + "_" + cmin);
            go.transform.SetParent(group.transform, false);
            go.transform.localPosition = Vector3.zero;

            var crate = go.AddComponent<CrateItem>();
            crate.colMin = cmin;
            crate.rowMin = rmin;
            crate.colMax = cmax;
            crate.rowMax = rmax;

            Undo.RegisterCreatedObjectUndo(go, "创建木箱");
            Undo.CollapseUndoOperations(undoGroup);

            crate.BuildVisual(group);
            group.RebuildGrid();   // 重新登记木箱 + 刷新遮盖（关掉被盖像素的渲染）
            EditorUtility.SetDirty(group);
            Selection.activeGameObject = go;

            Debug.Log("[CrateCreator] 已创建木箱：(" + cmin + "," + rmin + ")~(" + cmax + "," + rmax +
                ")，覆盖 " + group.CountPixelsUnderCrate(crate) + " 颗像素，拆箱需 " +
                crate.destroyAfterMoves + " 次相邻移出。");
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
