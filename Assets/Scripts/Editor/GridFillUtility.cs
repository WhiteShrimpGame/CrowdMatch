using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CrowdMatch
{
    /// <summary>
    /// 网格「补 Pixel」工具集合：Inspector 上的「移除障碍物并用它覆盖的范围填满 Pixel」，
    /// 以及菜单栏的「填充全部空格 / 填充矩形范围空格」。
    /// 填充颜色 ID 用 EditorPrefs 记忆（各处共用同一个值）；所有改动合并为一步撤销。
    /// </summary>
    public static class GridFillUtility
    {
        /// <summary>填充颜色 ID 的 EditorPrefs 键（各处共用，记忆上次输入，避免每次重填）。</summary>
        private const string FillColorKey = "CrowdMatch.GridFillUtility.FillColorId";

        /// <summary>共享的填充颜色 ID（Inspector 的「转为 Pixel」与菜单的填充工具共用同一个值）。</summary>
        public static int FillColorId
        {
            get { return EditorPrefs.GetInt(FillColorKey, 0); }
            set { EditorPrefs.SetInt(FillColorKey, value); }
        }

        /// <summary>
        /// 画「转为 Pixel（仅非运行模式）」区：填充颜色 ID + 按钮。点击时把颜色 ID 经 EditorApplication.delayCall
        /// 延后到本帧 GUI 绘制结束后再交给 onExecute —— 障碍物会被销毁，本帧后续的 Layout/Repaint 不该再访问它。
        /// </summary>
        public static void DrawFillSection(string buttonLabel, Action<int> onExecute)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("转为 Pixel（仅非运行模式）", EditorStyles.boldLabel);

            int fillColor = FillColorId;
            int newFillColor = EditorGUILayout.IntField("填充颜色 ID", fillColor);
            if (newFillColor != fillColor)
                FillColorId = newFillColor;   // 仅在真的改了才写 EditorPrefs（每次重绘都写会拖慢 Inspector）

            bool clicked;
            using (new EditorGUI.DisabledScope(Application.isPlaying))
                clicked = GUILayout.Button(buttonLabel);

            if (Application.isPlaying)
                EditorGUILayout.HelpBox("运行模式下不可用，请先停止运行。", MessageType.Info);

            if (clicked && onExecute != null)
                EditorApplication.delayCall += () => onExecute(newFillColor);
        }

        /// <summary>
        /// 把 cells 里的空格（既无 Pixel 也无墙/管/箱）填成 colorId 的 Pixel。
        /// 越界格与已被占用的格一律跳过；合并为一步撤销。返回新建的 Pixel 数。
        /// </summary>
        public static int FillEmptyCells(PixelGroup group, ICollection<Vector2Int> cells, int colorId, string undoName)
        {
            if (!ValidateTarget(group))
                return 0;

            // 先刷新网格，保证下面的 IsEmpty / GetItem 拿到的是当前占用表
            group.RebuildGrid();

            Undo.SetCurrentGroupName(undoName);
            int undoGroup = Undo.GetCurrentGroup();

            int considered;
            int created = SpawnPixels(group, cells, colorId, group.IsEmpty, out considered);

            group.RebuildGrid();
            EditorUtility.SetDirty(group);
            Undo.CollapseUndoOperations(undoGroup);

            if (created == 0)
            {
                EditorUtility.DisplayDialog("填充 Pixel", "范围内没有可填充的空格（越界格、已有 Pixel，或墙 / 管道 / 未开箱的箱子）。", "确定");
                return 0;
            }

            Debug.Log("[GridFill] " + undoName + "：在 " + considered + " 格范围内新建 " + created +
                " 个 Pixel（颜色 " + colorId + "）。");
            return created;
        }

        /// <summary>
        /// 销毁 obstacle（连同 extraDestroy 里的物体），并在 cells 上补 Pixel（颜色统一 colorId）。
        /// cells 是障碍物自身占的格，移除后即空，故只要求「当前没有 Pixel」（该格是否被墙/管/箱标记不作要求）。
        /// 仅非运行模式可用；合并为一步撤销。
        /// </summary>
        public static void RemoveAndFillPixels(GameObject obstacle, PixelGroup group, ICollection<Vector2Int> cells,
            int colorId, string undoName, IEnumerable<GameObject> extraDestroy = null)
        {
            if (obstacle == null)   // 延后执行期间可能已被销毁
                return;

            if (!ValidateTarget(group))
                return;

            string obstacleName = obstacle.name;   // 下面会销毁它，名字先取出来

            group.RebuildGrid();

            Undo.SetCurrentGroupName(undoName);
            int undoGroup = Undo.GetCurrentGroup();

            int considered;
            int created = SpawnPixels(group, cells, colorId, (c, r) => group.GetItem(c, r) == null, out considered);

            if (considered == 0)
            {
                // 没有落在网格范围内的格子：不创建、不销毁，直接放弃
                EditorUtility.DisplayDialog("填充 Pixel", "该物体没有落在网格范围内的格子，无法填充。", "确定");
                return;
            }

            if (extraDestroy != null)
            {
                foreach (var go in extraDestroy)
                {
                    if (go != null)
                        Undo.DestroyObjectImmediate(go);
                }
            }

            Undo.DestroyObjectImmediate(obstacle);

            group.RebuildGrid();
            EditorUtility.SetDirty(group);
            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log("[GridFill] 已移除 " + obstacleName + "，在 " + considered + " 格范围上新建 " + created +
                " 个 Pixel（颜色 " + colorId + "）。");
        }

        /// <summary>公共前置校验：非运行模式、有 PixelGroup、有 pixelPrefab。不通过时弹窗并返回 false。</summary>
        private static bool ValidateTarget(PixelGroup group)
        {
            if (Application.isPlaying)
            {
                EditorUtility.DisplayDialog("填充 Pixel", "请在非运行模式下使用。", "确定");
                return false;
            }

            if (group == null)
            {
                EditorUtility.DisplayDialog("填充 Pixel", "未找到 PixelGroup：请选中 PixelGroup 或其子物体。", "确定");
                return false;
            }

            if (group.pixelPrefab == null)
            {
                EditorUtility.DisplayDialog("填充 Pixel", "PixelGroup.pixelPrefab 为空，无法生成 Pixel。", "确定");
                return false;
            }

            return true;
        }

        /// <summary>逐个把可填的格 SpawnPixel 并登记 Undo。considered = 落在网格范围内的格数（含已被占用者）。</summary>
        private static int SpawnPixels(PixelGroup group, ICollection<Vector2Int> cells, int colorId,
            Func<int, int, bool> canFill, out int considered)
        {
            considered = 0;
            if (cells == null)
                return 0;

            var config = ColorConfigLocator.Find();
            if (config == null)
                Debug.LogWarning("[GridFill] 未找到 ColorConfig，填充的 Pixel 将沿用预制体默认材质。");

            int created = 0;
            foreach (var cell in cells)
            {
                if (!group.IsInRange(cell.x, cell.y))
                    continue;   // 越界格忽略
                considered++;

                if (!canFill(cell.x, cell.y))
                    continue;   // 该格已有 Pixel / 不是空格，不覆盖

                var item = group.SpawnPixel(cell.x, cell.y, colorId, config);
                if (item == null)
                    continue;

                Undo.RegisterCreatedObjectUndo(item.gameObject, "填充 Pixel");
                created++;
            }
            return created;
        }
    }
}
