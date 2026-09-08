using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CrowdMatch
{
    /// <summary>WallItem 的 Inspector：显示校验结果与占用格数。</summary>
    [CustomEditor(typeof(WallItem))]
    public class WallItemEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var wall = (WallItem)target;

            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

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
    }

    /// <summary>用选中的 PixelItem 作为端点创建墙体，并移除路径上占用的 Pixel（支持 Undo）。</summary>
    public static class WallCreator
    {
        private const string Tag = "[WallCreator]";

        [MenuItem("CrowdMatch/用选中 Pixel 创建墙体", true)]
        private static bool ValidateCreateWallFromSelection()
        {
            return CollectSelectedPixels().Count >= 2;
        }

        [MenuItem("CrowdMatch/用选中 Pixel 创建墙体")]
        private static void CreateWallFromSelection()
        {
            var pixels = CollectSelectedPixels();
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

        /// <summary>收集当前选中物体中的 PixelItem（去重）。支持选中 PixelItem 的子物体（向上查找父级组件）。</summary>
        private static List<PixelItem> CollectSelectedPixels()
        {
            var result = new List<PixelItem>();
            var seen = new HashSet<PixelItem>();
            foreach (var go in Selection.gameObjects)
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
