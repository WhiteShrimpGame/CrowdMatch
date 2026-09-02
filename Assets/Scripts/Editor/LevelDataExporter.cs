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
        public static void ExportCurrentLevel()
        {
            var pixelGroup = Object.FindObjectOfType<PixelGroup>();
            var containerGroup = Object.FindObjectOfType<ContainerGroup>();

            if (pixelGroup == null)
            {
                EditorUtility.DisplayDialog("导出关卡 JSON", "场景中找不到 PixelGroup。", "确定");
                return;
            }
            if (containerGroup == null)
            {
                EditorUtility.DisplayDialog("导出关卡 JSON", "场景中找不到 ContainerGroup。", "确定");
                return;
            }

            var data = BuildLevelData(pixelGroup, containerGroup);
            string json = JsonUtility.ToJson(data, true);

            string defaultDir = EditorPathMemory.LoadDir(ExportPathKey, "Assets/Levels");
            string path = EditorUtility.SaveFilePanel("导出关卡 JSON", defaultDir, "Level.json", "json");
            if (string.IsNullOrEmpty(path))
                return;
            EditorPathMemory.SaveDir(ExportPathKey, path);

            File.WriteAllText(path, json, new UTF8Encoding(false));
            AssetDatabase.Refresh();

            Debug.Log(Tag + " 已导出关卡 JSON 到 " + path + "（像素 " + data.pixel.columns + "×" +
                (data.pixel.rows + data.pixel.tailRows) + "，容器 " + data.container.items.Length + " 个）");

            EditorUtility.DisplayDialog("导出关卡 JSON",
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

        private static LevelData BuildLevelData(PixelGroup pg, ContainerGroup cg)
        {
            pg.RebuildGrid();
            cg.RebuildGrid();

            var data = new LevelData();

            // 像素：cells 按 row-major 拍平，index = row * columns + col，row 0 = 最前排
            data.pixel.columns = pg.columns;
            data.pixel.rows = pg.rows;
            data.pixel.tailRows = pg.tailRows;
            data.pixel.unitSize = pg.unitSize;
            data.pixel.spacingX = pg.spacingX;
            data.pixel.spacingZ = pg.spacingZ;

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
            data.container.xSpacing = cg.xSpacing;
            data.container.zSpacing = cg.zSpacing;

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

            return data;
        }
    }
}
