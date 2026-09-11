using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CrowdMatch
{
    /// <summary>PipeItem 的 Inspector：显示校验结果与轨道格数、剩余波次。</summary>
    [CustomEditor(typeof(PipeItem))]
    public class PipeItemEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var pipe = (PipeItem)target;

            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            if (pipe.points == null || pipe.points.Count < 2)
            {
                EditorGUILayout.HelpBox("至少需要 2 个端点：points[0] = 管道格，其余为轨道格（每波像素的目标格）。", MessageType.Info);
                return;
            }

            var group = pipe.GetComponentInParent<PixelGroup>();
            int track = group != null
                ? PipeItem.CountTrackCells(pipe.points, group.columns, group.TotalRows)
                : pipe.points.Count - 1;

            var pipeCell = PipeItem.GetPipeCell(pipe.points);
            string waves = pipe.colors != null ? pipe.colors.Count.ToString() : "0";

            EditorGUILayout.HelpBox(
                "管道格 (" + pipeCell.x + ", " + pipeCell.y + ")，轨道 " + track +
                " 格，波次颜色 " + waves + " 个。\n" +
                "轨道清空后逐波生成（开局不立即生成，轨道上的初始像素作为阻挡）。",
                MessageType.Info);

            if (pipe.colors == null || pipe.colors.Count == 0)
                EditorGUILayout.HelpBox("colors 为空：该管道不会生成任何像素。", MessageType.Warning);
        }
    }

    /// <summary>用选中的 PixelItem 创建管道：首个像素作为管道格（points[0]），其余为轨道格（points[1..]）。</summary>
    public static class PipeCreator
    {
        private const string Tag = "[PipeCreator]";

        [MenuItem("CrowdMatch/用选中 Pixel 创建管道", true)]
        private static bool ValidateCreatePipeFromSelection()
        {
            return CollectSelectedPixels().Count >= 2;
        }

        [MenuItem("CrowdMatch/用选中 Pixel 创建管道")]
        private static void CreatePipeFromSelection()
        {
            var pixels = CollectSelectedPixels();
            SelectionOrderTracker.LogState();
            if (pixels.Count < 2)
            {
                EditorUtility.DisplayDialog("创建管道", "请至少选中 2 个 PixelItem（首个 = 管道格，其余 = 轨道格）。", "确定");
                return;
            }

            var group = pixels[0].GetComponentInParent<PixelGroup>();
            if (group == null)
            {
                EditorUtility.DisplayDialog("创建管道", "选中的 Pixel 必须位于 PixelGroup 下。", "确定");
                return;
            }

            foreach (var p in pixels)
            {
                if (p.GetComponentInParent<PixelGroup>() != group)
                {
                    EditorUtility.DisplayDialog("创建管道", "选中的 Pixel 必须属于同一个 PixelGroup。", "确定");
                    return;
                }
            }

            if (group.pipePrefab == null)
            {
                EditorUtility.DisplayDialog("创建管道", "PixelGroup 未设置 pipePrefab，请先指定管道预制体。", "确定");
                return;
            }

            // 按选中顺序构建轨迹（首点 = 管道格）
            var points = new List<Vector2>(pixels.Count);
            foreach (var p in pixels)
                points.Add(new Vector2(p.gridX, p.gridZ));

            // 实例化管道预制体（预制体资产走 InstantiatePrefab，场景对象走 Object.Instantiate 克隆）
            GameObject pipeGo;
            if (PrefabUtility.GetPrefabAssetType(group.pipePrefab) == PrefabAssetType.NotAPrefab)
                pipeGo = (GameObject)Object.Instantiate(group.pipePrefab, group.transform);
            else
                pipeGo = (GameObject)PrefabUtility.InstantiatePrefab(group.pipePrefab, group.transform);

            pipeGo.name = "Pipe_" + (group.transform.childCount + 1);

            var pipe = pipeGo.GetComponent<PipeItem>();
            if (pipe == null)
            {
                EditorUtility.DisplayDialog("创建管道", "pipePrefab 缺少 PipeItem 组件。", "确定");
                Object.DestroyImmediate(pipeGo);
                return;
            }

            pipe.points = points;
            pipe.colors = new List<int>();
            var pipeCell = PipeItem.GetPipeCell(points);
            pipeGo.transform.localPosition = group.GetLocalPosition(pipeCell.x, pipeCell.y);
            pipe.OrientBody();

            Undo.RegisterCreatedObjectUndo(pipeGo, "创建管道");

            // 刷新网格后移除管道格上的 Pixel（轨道格像素保留，作为开局阻挡）
            group.RebuildGrid();
            var item = group.GetItem(pipeCell.x, pipeCell.y);
            if (item != null)
                Undo.DestroyObjectImmediate(item.gameObject);

            group.RebuildGrid();
            if (Application.isPlaying)
                group.RefreshExposed();

            EditorUtility.SetDirty(group);
            Selection.activeGameObject = pipeGo;

            Debug.Log(Tag + " 已创建管道：轨迹 " + points.Count + " 点，轨道 " +
                PipeItem.CountTrackCells(points, group.columns, group.TotalRows) +
                " 格（请在 Inspector 设置 colors）。");
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
