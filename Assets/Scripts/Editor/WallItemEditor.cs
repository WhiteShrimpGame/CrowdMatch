using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CrowdMatch
{
    /// <summary>WallItem 的 Inspector：显示校验结果与占用格数，并提供闭环/取消闭环操作。</summary>
    [CustomEditor(typeof(WallItem))]
    public class WallItemEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var wall = (WallItem)target;

            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("闭环", EditorStyles.boldLabel);

            if (wall.closed)
                EditorGUILayout.HelpBox("当前为闭环墙体：首尾之间自动补一条闭合段，并作为封闭障碍包围内部 Pixel。", MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("闭环"))
                CloseLoop(wall);
            if (GUILayout.Button("取消闭环"))
                CancelLoop(wall);
            EditorGUILayout.EndHorizontal();

            if (wall.points == null || wall.points.Count < 2)
            {
                EditorGUILayout.HelpBox("至少需要 2 个端点才能构成一段墙体。", MessageType.Info);
                return;
            }

            string error;
            if (!wall.IsValid(out error))
                EditorGUILayout.HelpBox(error, MessageType.Warning);
            else
                EditorGUILayout.HelpBox("有效：共 " + wall.points.Count + " 个端点，占据 " +
                    wall.OccupiedCellCount() + " 个网格格。", MessageType.Info);
        }

        /// <summary>执行闭环：校验闭环条件，弹窗询问是否移除包围 Pixel，通过后标记 closed 并刷新。</summary>
        private void CloseLoop(WallItem wall)
        {
            if (wall.closed)
            {
                EditorUtility.DisplayDialog("闭环", "该墙体已经是闭环，无需重复操作。", "确定");
                return;
            }

            string err = wall.CheckClosable();
            if (err != null)
            {
                EditorUtility.DisplayDialog("闭环", "无法闭环：\n" + err, "确定");
                return;
            }

            var group = wall.Group;
            if (group == null)
            {
                EditorUtility.DisplayDialog("闭环", "墙体必须位于 PixelGroup 下才能闭环。", "确定");
                return;
            }

            group.RebuildGrid();

            var wallCells = CollectLoopCells(wall);
            var interior = ComputeInteriorCells(group, wallCells);

            int pixelCount = 0;
            foreach (var c in wallCells)
                if (group.GetItem(c.x, c.y) != null)
                    pixelCount++;
            foreach (var c in interior)
                if (group.GetItem(c.x, c.y) != null)
                    pixelCount++;

            int choice = EditorUtility.DisplayDialogComplex("闭环",
                "闭环条件满足：包围区域（含闭合段）共 " + (interior.Count + wallCells.Count) + " 格，其中含 Pixel " + pixelCount + " 个。\n\n是否移除包围的 Pixel？",
                "移除 Pixel 并闭环", "只闭环", "取消");

            if (choice == 2)
                return;

            Undo.RecordObject(wall, "闭环墙体");

            if (choice == 0)
            {
                var toRemove = new HashSet<Vector2Int>(wallCells);
                toRemove.UnionWith(interior);
                foreach (var c in toRemove)
                {
                    var item = group.GetItem(c.x, c.y);
                    if (item != null)
                        Undo.DestroyObjectImmediate(item.gameObject);
                }
            }

            wall.closed = true;
            group.RebuildGrid();
            if (Application.isPlaying)
                group.RefreshExposed();
            EditorUtility.SetDirty(wall);
            EditorUtility.SetDirty(group);

            Debug.Log("[WallItem] 已闭环墙体 " + wall.name + "（包围区域 " + (interior.Count + wallCells.Count) + " 格，移除 Pixel " +
                (choice == 0 ? pixelCount : 0) + " 个）。");
        }

        /// <summary>取消闭环：去掉首尾闭合段并刷新（不移除/恢复任何 Pixel）。</summary>
        private void CancelLoop(WallItem wall)
        {
            if (!wall.closed)
            {
                EditorUtility.DisplayDialog("取消闭环", "该墙体当前不是闭环。", "确定");
                return;
            }

            Undo.RecordObject(wall, "取消闭环墙体");
            wall.closed = false;

            var group = wall.Group;
            if (group != null)
            {
                group.RebuildGrid();
                if (Application.isPlaying)
                    group.RefreshExposed();
                EditorUtility.SetDirty(group);
            }
            EditorUtility.SetDirty(wall);

            Debug.Log("[WallItem] 已取消闭环墙体 " + wall.name + "。");
        }

        /// <summary>构建闭环后的完整占格集合（含首尾闭合段）。</summary>
        private static HashSet<Vector2Int> CollectLoopCells(WallItem wall)
        {
            var closedPoints = new List<Vector2>(wall.points);
            if (closedPoints.Count >= 2)
                closedPoints.Add(closedPoints[0]);
            var wallCells = new HashSet<Vector2Int>();
            WallItem.CollectOccupiedCells(closedPoints, wallCells);
            return wallCells;
        }

        /// <summary>用「从网格边界四向 BFS 漫过非墙格」求闭环内部的网格格集合（不含墙自身格）。</summary>
        private static HashSet<Vector2Int> ComputeInteriorCells(PixelGroup group, HashSet<Vector2Int> wallCells)
        {
            int cols = group.columns;
            int rows = group.TotalRows;

            int minX = int.MaxValue, maxX = int.MinValue, minZ = int.MaxValue, maxZ = int.MinValue;
            foreach (var c in wallCells)
            {
                if (c.x < minX) minX = c.x;
                if (c.x > maxX) maxX = c.x;
                if (c.y < minZ) minZ = c.y;
                if (c.y > maxZ) maxZ = c.y;
            }

            // 从网格边界（非墙格）BFS，能到达的都视为「外部」
            var outside = new bool[cols, rows];
            var q = new Queue<Vector2Int>();
            int[] dx = { 1, -1, 0, 0 };
            int[] dz = { 0, 0, 1, -1 };
            for (int c = 0; c < cols; c++)
                for (int r = 0; r < rows; r++)
                {
                    if (c != 0 && c != cols - 1 && r != 0 && r != rows - 1)
                        continue;
                    var cell = new Vector2Int(c, r);
                    if (wallCells.Contains(cell) || outside[c, r])
                        continue;
                    outside[c, r] = true;
                    q.Enqueue(cell);
                }

            while (q.Count > 0)
            {
                var cur = q.Dequeue();
                for (int d = 0; d < 4; d++)
                {
                    int nx = cur.x + dx[d];
                    int nz = cur.y + dz[d];
                    if (nx < 0 || nx >= cols || nz < 0 || nz >= rows)
                        continue;
                    if (outside[nx, nz])
                        continue;
                    var ncell = new Vector2Int(nx, nz);
                    if (wallCells.Contains(ncell))
                        continue;
                    outside[nx, nz] = true;
                    q.Enqueue(ncell);
                }
            }

            var interior = new HashSet<Vector2Int>();
            for (int x = minX; x <= maxX; x++)
                for (int z = minZ; z <= maxZ; z++)
                {
                    if (x < 0 || x >= cols || z < 0 || z >= rows)
                        continue;
                    var cell = new Vector2Int(x, z);
                    if (wallCells.Contains(cell))
                        continue;
                    if (!outside[x, z])
                        interior.Add(cell);
                }
            return interior;
        }
    }

    /// <summary>用选中的 PixelItem 作为端点创建墙体，并移除路径上占用的 Pixel（支持 Undo）。</summary>
    public static class WallCreator
    {
        private const string Tag = "[WallCreator]";

        [MenuItem("CrowdMatch/用选中 Pixel 创建墙体 %#w", true)]
        private static bool ValidateCreateWallFromSelection()
        {
            return CollectSelectedPixels().Count >= 2;
        }

        [MenuItem("CrowdMatch/用选中 Pixel 创建墙体 %#w")]
        private static void CreateWallFromSelection()
        {
            var pixels = CollectSelectedPixels();
            SelectionOrderTracker.LogState();
            if (pixels.Count < 2)
            {
                EditorUtility.DisplayDialog("创建墙体", "请至少选中 2 个 PixelItem。", "确定");
                return;
            }

            var group = pixels[0].GetComponentInParent<PixelGroup>();
            if (group == null)
            {
                EditorUtility.DisplayDialog("创建墙体", "选中的 Pixel 必须位于 PixelGroup 下。", "确定");
                return;
            }

            foreach (var p in pixels)
            {
                if (p.GetComponentInParent<PixelGroup>() != group)
                {
                    EditorUtility.DisplayDialog("创建墙体", "选中的 Pixel 必须属于同一个 PixelGroup。", "确定");
                    return;
                }
            }

            // 严格按照选中顺序构建端点：不排序、不插入拐角（未选中的点不作为端点）
            var points = new List<Vector2>(pixels.Count);
            foreach (var p in pixels)
                points.Add(new Vector2(p.gridX, p.gridZ));

            // 校验相邻端点轴对齐；不合法时提示哪一段
            for (int i = 0; i + 1 < points.Count; i++)
            {
                var a = points[i];
                var b = points[i + 1];
                if (Mathf.Approximately(a.x, b.x) || Mathf.Approximately(a.y, b.y))
                    continue;

                EditorUtility.DisplayDialog("创建墙体",
                    "第 " + (i + 1) + " 段不合法：\n" +
                    pixels[i].name + "（" + pixels[i].gridX + ", " + pixels[i].gridZ + "）→ " +
                    pixels[i + 1].name + "（" + pixels[i + 1].gridX + ", " + pixels[i + 1].gridZ + "）\n" +
                    "两端点需在同一行（gridZ 相等）或同一列（gridX 相等）。",
                    "确定");
                return;
            }

            // 创建墙体（先建物体、再 AddComponent，最后统一登记 Undo，便于整体撤销）
            var wallGo = new GameObject("Wall_" + (group.transform.childCount + 1));
            wallGo.transform.SetParent(group.transform, false);
            wallGo.transform.localPosition = Vector3.zero;

            var wall = wallGo.AddComponent<WallItem>();
            wall.points = points;

            Undo.RegisterCreatedObjectUndo(wallGo, "创建墙体");

            // 收集墙体占据的网格格（含端点与中间格）
            var occupied = new HashSet<Vector2Int>();
            foreach (var cell in wall.EnumerateOccupiedCells())
                occupied.Add(cell);

            // 刷新网格后移除路径上占用的 Pixel
            group.RebuildGrid();
            int removed = 0;
            foreach (var cell in occupied)
            {
                var item = group.GetItem(cell.x, cell.y);
                if (item != null)
                {
                    Undo.DestroyObjectImmediate(item.gameObject);
                    removed++;
                }
            }

            group.RebuildGrid();
            if (Application.isPlaying)
                group.RefreshExposed();

            EditorUtility.SetDirty(group);
            Selection.activeGameObject = wallGo;

            Debug.Log(Tag + " 已创建墙体：端点 " + points.Count + " 个，占据 " + occupied.Count +
                " 格，移除 " + removed + " 个 Pixel（父物体 " + group.name + "）。");
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
