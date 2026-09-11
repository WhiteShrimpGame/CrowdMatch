using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace CrowdMatch
{
    /// <summary>
    /// 编辑器导出工具：把场景中当前 PixelGroup + ContainerGroup 的布局封装为 LevelData，
    /// 序列化为 JSON 存到 Assets 下（Unity 会导入为 TextAsset，供 GameManager.levelJsons 引用）。
    /// </summary>
    public static class LevelDataExporter
    {
        private const string Tag = "[LevelDataExporter]";

        /// <summary>导出关卡 JSON 共用的「上次路径」EditorPrefs 键。</summary>
        private const string ExportPathKey = "CrowdMatch.LevelDataExporter.LastExportPath";

        /// <summary>导入关卡 JSON 共用的「上次路径」EditorPrefs 键。</summary>
        private const string ImportPathKey = "CrowdMatch.LevelDataExporter.LastImportPath";

        [MenuItem("CrowdMatch/导出关卡 JSON")]
        public static void ExportCurrentLevel() => ExportCurrentLevel(locked: false);

        [MenuItem("CrowdMatch/导出关卡 JSON", true)]
        private static bool ValidateExportCurrentLevel() => !EditorApplication.isPlaying;

        /// <summary>Play 模式下的锁定导出：导出关卡初始化时的状态，并把 lockContainer 置为 true。</summary>
        [MenuItem("CrowdMatch/导出关卡 JSON（锁定）")]
        public static void ExportCurrentLevelLocked() => ExportCurrentLevel(locked: true);

        [MenuItem("CrowdMatch/导出关卡 JSON（锁定）", true)]
        private static bool ValidateExportCurrentLevelLocked() => EditorApplication.isPlaying;

        private static void ExportCurrentLevel(bool locked)
        {
            LevelData data;
            string dialogTitle = locked ? "导出关卡 JSON（锁定）" : "导出关卡 JSON";

            if (locked)
            {
                // Play 模式：改用关卡初始化时缓存的初始状态（复制一份，避免改动缓存），并锁定容器
                if (LevelDataCache.LastInitData == null)
                {
                    EditorUtility.DisplayDialog(dialogTitle,
                        "没有可用的初始化关卡数据。\n请先进入 Play 模式并加载关卡（触发初始化）后再导出。",
                        "确定");
                    return;
                }
                data = JsonUtility.FromJson<LevelData>(JsonUtility.ToJson(LevelDataCache.LastInitData));
                data.container.lockContainer = true;
            }
            else
            {
                var pixelGroup = Object.FindObjectOfType<PixelGroup>();
                var containerGroup = Object.FindObjectOfType<ContainerGroup>();

                if (pixelGroup == null)
                {
                    EditorUtility.DisplayDialog(dialogTitle, "场景中找不到 PixelGroup。", "确定");
                    return;
                }
                if (containerGroup == null)
                {
                    EditorUtility.DisplayDialog(dialogTitle, "场景中找不到 ContainerGroup。", "确定");
                    return;
                }

                data = BuildLevelData(pixelGroup, containerGroup);
            }

            string json = JsonUtility.ToJson(data, true);

            string defaultDir = EditorPathMemory.LoadDir(ExportPathKey, "Assets/Levels");
            string path = EditorUtility.SaveFilePanel(dialogTitle, defaultDir, "Level.json", "json");
            if (string.IsNullOrEmpty(path))
                return;
            EditorPathMemory.SaveDir(ExportPathKey, path);

            File.WriteAllText(path, json, new UTF8Encoding(false));
            AssetDatabase.Refresh();

            Debug.Log(Tag + " 已导出关卡 JSON 到 " + path + "（像素 " + data.pixel.columns + "×" +
                (data.pixel.rows + data.pixel.tailRows) + "，容器 " + data.container.items.Length + " 个，墙体 " +
                data.walls.Length + " 段，管道 " + data.pipes.Length + " 个，箱子 " + data.boxes.Length + " 个" +
                (locked ? "，已锁定" : "") + "）");

            EditorUtility.DisplayDialog(dialogTitle,
                "已导出到：\n" + path +
                "\n\n请确保该文件位于 Assets 目录下，并在 GameManager.levelJsons 中按关卡序号依次引用。",
                "确定");
        }

        /// <summary>从 JSON 文件导入关卡配置到当前场景的 PixelGroup + ContainerGroup。</summary>
        [MenuItem("CrowdMatch/从 JSON 导入配置到当前场景")]
        public static void ImportLevelFromJson()
        {
            var pixelGroup = Object.FindObjectOfType<PixelGroup>();
            var containerGroup = Object.FindObjectOfType<ContainerGroup>();

            if (pixelGroup == null && containerGroup == null)
            {
                EditorUtility.DisplayDialog("导入关卡 JSON", "场景中找不到 PixelGroup 或 ContainerGroup。", "确定");
                return;
            }

            string defaultDir = EditorPathMemory.LoadDir(ImportPathKey, "Assets/Levels");
            string path = EditorUtility.OpenFilePanel("导入关卡 JSON", defaultDir, "json");
            if (string.IsNullOrEmpty(path))
                return;
            EditorPathMemory.SaveDir(ImportPathKey, path);

            string json;
            try
            {
                json = File.ReadAllText(path);
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("导入关卡 JSON", "读取文件失败：\n" + e.Message, "确定");
                return;
            }

            var data = LevelLoader.ParseJson(json, Path.GetFileName(path));
            if (data == null)
            {
                EditorUtility.DisplayDialog("导入关卡 JSON", "JSON 解析失败，详见 Console。", "确定");
                return;
            }

            var config = ColorConfigLocator.Find();
            if (config == null)
                Debug.LogWarning(Tag + " 未找到 ColorConfig，导入的物体将使用默认材质。");

            // 注册整棵层级用于撤销，随后清空旧子物体并 spawn 新子物体
            if (pixelGroup != null)
                Undo.RegisterFullObjectHierarchyUndo(pixelGroup.gameObject, "导入关卡 JSON");
            if (containerGroup != null)
                Undo.RegisterFullObjectHierarchyUndo(containerGroup.gameObject, "导入关卡 JSON");

            LevelLoader.Apply(pixelGroup, containerGroup, data, config);

            if (pixelGroup != null)
                EditorUtility.SetDirty(pixelGroup);
            if (containerGroup != null)
                EditorUtility.SetDirty(containerGroup);

            Debug.Log(Tag + " 已从 " + path + " 导入关卡（像素 " + data.pixel.columns + "×" +
                (data.pixel.rows + data.pixel.tailRows) + "，容器 " + data.container.items.Length + " 个）");

            EditorUtility.DisplayDialog("导入关卡 JSON",
                "已从：\n" + path + "\n导入到当前场景。", "确定");
        }

        /// <summary>清空当前场景中 PixelGroup 与 ContainerGroup 的全部子物体（不改变布局字段）。</summary>
        [MenuItem("CrowdMatch/清空当前场景两个 Group 的子物体")]
        public static void ClearBothGroups()
        {
            var pixelGroup = Object.FindObjectOfType<PixelGroup>();
            var containerGroup = Object.FindObjectOfType<ContainerGroup>();

            if (pixelGroup == null && containerGroup == null)
            {
                EditorUtility.DisplayDialog("清空 Group 子物体", "场景中找不到 PixelGroup 或 ContainerGroup。", "确定");
                return;
            }

            if (!EditorUtility.DisplayDialog("清空 Group 子物体",
                "将清空当前场景中 PixelGroup 与 ContainerGroup 的全部子物体。是否继续？",
                "清空", "取消"))
            {
                return;
            }

            if (pixelGroup != null)
            {
                Undo.RegisterFullObjectHierarchyUndo(pixelGroup.gameObject, "清空 Group 子物体");
                pixelGroup.ClearPixels();
                pixelGroup.ClearWalls();
                pixelGroup.ClearPipes();
                pixelGroup.ClearBoxes();
                pixelGroup.RebuildGrid();
                EditorUtility.SetDirty(pixelGroup);
            }
            if (containerGroup != null)
            {
                Undo.RegisterFullObjectHierarchyUndo(containerGroup.gameObject, "清空 Group 子物体");
                containerGroup.ClearContainers();
                containerGroup.RebuildGrid();
                EditorUtility.SetDirty(containerGroup);
            }

            Debug.Log(Tag + " 已清空当前场景两个 Group 的子物体。");
        }

        public static LevelData BuildLevelData(PixelGroup pg, ContainerGroup cg)
        {
            pg.RebuildGrid();
            cg.RebuildGrid();

            var data = new LevelData();

            // 像素：cells 按 row-major 拍平，index = row * columns + col，row 0 = 最前排
            data.pixel.columns = pg.columns;
            data.pixel.rows = pg.rows;
            data.pixel.tailRows = pg.tailRows;
            data.pixel.unitSize = pg.unitSize;

            int totalRows = pg.TotalRows;
            data.pixel.cells = new int[pg.columns * totalRows];
            int emptyCells = 0;
            for (int r = 0; r < totalRows; r++)
            {
                for (int c = 0; c < pg.columns; c++)
                {
                    var item = pg.GetItem(c, r);
                    if (item != null)
                        data.pixel.cells[r * pg.columns + c] = item.colorId;
                    else
                    {
                        data.pixel.cells[r * pg.columns + c] = 0;
                        emptyCells++;
                    }
                }
            }
            if (emptyCells > 0)
                Debug.LogWarning(Tag + " 像素网格中有 " + emptyCells + " 个空格，导出时将按颜色 0 处理。");

            // 容器：稀疏列表，只存非空格
            data.container.columns = cg.columns;
            data.container.rows = cg.rows;

            var items = new List<LevelData.ContainerItemData>();
            for (int c = 0; c < cg.columns; c++)
            {
                for (int r = 0; r < cg.rows; r++)
                {
                    var item = cg.GetItem(c, r);
                    if (item == null)
                        continue;
                    items.Add(new LevelData.ContainerItemData
                    {
                        x = item.gridX,
                        y = item.gridZ,
                        colorId = item.colorId,
                        capacity = item.capacity,
                    });
                }
            }
            data.container.items = items.ToArray();

            // 墙体：扫描 PixelGroup 下的 WallItem，每个墙存一组端点
            var walls = new List<LevelData.WallData>();
            foreach (var wall in pg.GetComponentsInChildren<WallItem>())
            {
                if (wall == null || wall.points == null || wall.points.Count < 2)
                    continue;
                walls.Add(new LevelData.WallData { points = wall.points.ToArray() });
            }
            data.walls = walls.ToArray();

            // 管道：扫描 PixelGroup 下的 PipeItem，每个管道存轨迹端点 + 每波颜色
            var pipes = new List<LevelData.PipeData>();
            foreach (var pipe in pg.GetComponentsInChildren<PipeItem>())
            {
                if (pipe == null || pipe.points == null || pipe.points.Count < 2 || pipe.colors == null || pipe.colors.Count < 1)
                    continue;
                pipes.Add(new LevelData.PipeData { points = pipe.points.ToArray(), colors = pipe.colors.ToArray() });
            }
            data.pipes = pipes.ToArray();

            // 箱子：扫描 PixelGroup 下的 BoxItem，每个箱子存矩形区域 + 容量 + 隐藏 Pixel 颜色 + 行为开关
            var boxes = new List<LevelData.BoxData>();
            foreach (var box in pg.GetComponentsInChildren<BoxItem>())
            {
                if (box == null)
                    continue;
                boxes.Add(new LevelData.BoxData
                {
                    colMin = box.colMin,
                    rowMin = box.rowMin,
                    colMax = box.colMax,
                    rowMax = box.rowMax,
                    capacity = box.capacity,
                    colorIds = box.colorIds != null ? (int[])box.colorIds.Clone() : new int[0],
                    jumpStartInterval = box.jumpStartInterval,
                    jumpSpawnYOffset = box.jumpSpawnYOffset,
                });
            }
            data.boxes = boxes.ToArray();

            return data;
        }
    }
}
