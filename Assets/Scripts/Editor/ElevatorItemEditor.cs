using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CrowdMatch
{
    /// <summary>ElevatorItem 的 Inspector：显示区域、分组摘要，并提供「从选中 Pixel 追加一组」。</summary>
    [CustomEditor(typeof(ElevatorItem))]
    public class ElevatorItemEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var elev = (ElevatorItem)target;
            var pg = elev.GetComponentInParent<PixelGroup>();

            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();

            int groupCount = elev.groups != null ? elev.groups.Count : 0;
            int totalCells = 0;
            if (elev.groups != null)
            {
                foreach (var g in elev.groups)
                    if (g != null && g.cells != null)
                        totalCells += g.cells.Length / 3;
            }

            EditorGUILayout.HelpBox(
                "区域 (" + elev.colMin + "," + elev.rowMin + ")~(" + elev.colMax + "," + elev.rowMax +
                ")，共 " + groupCount + " 组 / " + totalCells + " 个像素。\n" +
                "所有组都在地下竖井里，第 0 组是第一个升起的组。\n" +
                "每组 cells = [col,row,color, ...] 三元组拍平。", MessageType.Info);

            if (pg != null && GUILayout.Button("从选中 Pixel 追加一组"))
            {
                AppendGroupFromSelection(elev, pg);
            }
        }

        /// <summary>把当前选中的 PixelItem 捕获为一组（记录 col,row,color 并移除这些像素，避免导出重复计数）。</summary>
        private void AppendGroupFromSelection(ElevatorItem elev, PixelGroup pg)
        {
            var pixels = CollectSelectedPixels();
            if (pixels.Count == 0)
            {
                EditorUtility.DisplayDialog("追加组", "请先选中一个或多个 PixelItem。", "确定");
                return;
            }

            var cells = new List<int>();
            foreach (var p in pixels)
            {
                cells.Add(p.gridX);
                cells.Add(p.gridZ);
                cells.Add(p.colorId);
            }

            Undo.SetCurrentGroupName("追加升降台组");
            int undoGroup = Undo.GetCurrentGroup();

            Undo.RecordObject(elev, "追加升降台组");
            elev.groups.Add(new LevelData.ElevatorGroupData { cells = cells.ToArray() });

            // 移除这些像素（升降台区域的像素由分组提供，基础网格里不应再出现）
            foreach (var p in pixels)
                Undo.DestroyObjectImmediate(p.gameObject);

            Undo.CollapseUndoOperations(undoGroup);
            pg.RebuildGrid();
            EditorUtility.SetDirty(elev);
            EditorUtility.SetDirty(pg);

            Debug.Log("[ElevatorItemEditor] 已追加第 " + (elev.groups.Count - 1) + " 组，共 " +
                (cells.Count / 3) + " 个像素。");
        }

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

    /// <summary>
    /// 用选中的两个 PixelItem 作为左上、右下创建升降台（与选中顺序无关，取 min/max 归一化）。
    /// 创建时区域内的现有 Pixel 保留为「上方地上 pixel」（触发升起的地上层），不并入任何组。
    /// 升降台自身的像素全部在地下，用「从选中 Pixel 追加一组」逐个追加地下组。
    /// </summary>
    public static class ElevatorCreator
    {
        [MenuItem("CrowdMatch/用选中 Pixel 创建升降台（左上、右下）", true)]
        private static bool ValidateCreateElevatorFromSelection() => CollectSelectedPixels().Count == 2;

        [MenuItem("CrowdMatch/用选中 Pixel 创建升降台（左上、右下）")]
        private static void CreateElevatorFromSelection()
        {
            var pixels = CollectSelectedPixels();
            if (pixels.Count != 2)
            {
                EditorUtility.DisplayDialog("创建升降台", "请恰好选中 2 个 PixelItem（左上、右下）。", "确定");
                return;
            }

            var group = pixels[0].GetComponentInParent<PixelGroup>();
            if (group == null || pixels[1].GetComponentInParent<PixelGroup>() != group)
            {
                EditorUtility.DisplayDialog("创建升降台", "选中的 Pixel 必须位于同一个 PixelGroup 下。", "确定");
                return;
            }

            int cmin = Mathf.Min(pixels[0].gridX, pixels[1].gridX);
            int rmin = Mathf.Min(pixels[0].gridZ, pixels[1].gridZ);
            int cmax = Mathf.Max(pixels[0].gridX, pixels[1].gridX);
            int rmax = Mathf.Max(pixels[0].gridZ, pixels[1].gridZ);

            Undo.SetCurrentGroupName("创建升降台");
            int undoGroup = Undo.GetCurrentGroup();

            // 区域内的现有像素保留为「上方地上 pixel」（触发升起的地上层），不并入任何组。
            // 升降台自身的像素全部在地下，用「从选中 Pixel 追加一组」逐个追加。

            GameObject go;
            ElevatorItem elev;
            if (group.elevatorPrefab != null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(group.elevatorPrefab, group.transform);
                go.name = "Elevator_" + rmin + "_" + cmin;
                go.transform.localPosition = Vector3.zero;
                elev = go.GetComponent<ElevatorItem>();
                if (elev == null)
                    elev = go.AddComponent<ElevatorItem>();
            }
            else
            {
                go = new GameObject("Elevator_" + rmin + "_" + cmin);
                go.transform.SetParent(group.transform, false);
                go.transform.localPosition = Vector3.zero;
                elev = go.AddComponent<ElevatorItem>();
            }

            elev.colMin = cmin;
            elev.rowMin = rmin;
            elev.colMax = cmax;
            elev.rowMax = rmax;

            Undo.RegisterCreatedObjectUndo(go, "创建升降台");
            elev.ConfigureVisualOnly(group);
            Undo.CollapseUndoOperations(undoGroup);

            group.RebuildGrid();
            EditorUtility.SetDirty(group);
            Selection.activeGameObject = go;

            Debug.Log("[ElevatorCreator] 已创建升降台：(" + cmin + "," + rmin + ")~(" + cmax + "," + rmax +
                ")。区域内现有像素保留为上方触发层，地下组请用「从选中 Pixel 追加一组」添加。");
        }

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
