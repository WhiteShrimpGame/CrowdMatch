using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace CrowdMatch
{
    /// <summary>
    /// 批量图片转关卡：把选中（或某文件夹内）的图片依次转为关卡 JSON。
    /// 每张图按以下步骤处理：
    ///   1. 清空场景中的 PixelGroup 与 ContainerGroup；
    ///   2. 按图片像素设置 PixelGroup 网格数（columns=宽、rows=高、tailRows=0）并生成网格；
    ///   3. 导入图片颜色（顶行 = 最前排 gridZ 0）；
    ///   4. 按像素颜色分布规划并生成 ContainerGroup；
    ///   5. 导出 JSON 到 Assets/LevelData。
    /// 命名：LevelItem + 三位递增标号（起点 = 现有 Level*.json 数量 + 1）。
    /// </summary>
    public static class ImageBatchLevelExporter
    {
        private const string Tag = "[ImageBatchLevelExporter]";

        /// <summary>图片每格对应的像素边长。1 = 每张图片像素对应一个网格格（像素图）。</summary>
        private const int CellSize = 32;

        /// <summary>图片来源「上次路径」EditorPrefs 键。</summary>
        private const string SourceDirKey = "CrowdMatch.ImageBatchLevelExporter.LastSourceDir";

        private const string OutDir = "Assets/LevelData";

        [MenuItem("CrowdMatch/批量图片转关卡")]
        public static void BatchConvert()
        {
            // 1. 收集图片：优先 Project 窗口多选，否则选择文件夹
            var paths = CollectImagePaths();
            if (paths.Count == 0)
            {
                EditorUtility.DisplayDialog("批量图片转关卡",
                    "请先在 Project 窗口选中多张图片（Texture），再运行本菜单；\n或运行后选择一个图片文件夹。",
                    "确定");
                return;
            }

            var pixelGroup = Object.FindObjectOfType<PixelGroup>();
            var containerGroup = Object.FindObjectOfType<ContainerGroup>();
            if (pixelGroup == null || containerGroup == null)
            {
                EditorUtility.DisplayDialog("批量图片转关卡", "场景中找不到 PixelGroup 或 ContainerGroup。", "确定");
                return;
            }

            var config = ColorConfigLocator.Find();
            if (config == null)
            {
                EditorUtility.DisplayDialog("批量图片转关卡", "未找到 ColorConfig。", "确定");
                return;
            }

            if (containerGroup.containerPrefab == null)
            {
                EditorUtility.DisplayDialog("批量图片转关卡", "ContainerGroup.containerPrefab 为空，请先指定 ContainerItem 预制体。", "确定");
                return;
            }

            int startIndex = CountExistingLevels(OutDir) + 1;

            if (!EditorUtility.DisplayDialog("批量图片转关卡",
                "将把 " + paths.Count + " 张图片依次转为关卡 JSON。\n\n" +
                "输出目录：Assets/LevelData\n" +
                "命名起点：LevelItem" + startIndex.ToString("D3") + ".json\n\n是否继续？",
                "继续", "取消"))
            {
                return;
            }

            int ok = 0;
            for (int i = 0; i < paths.Count; i++)
            {
                int index = startIndex + i;
                string fileName = "LevelItem" + index.ToString("D3") + ".json";
                string srcName = Path.GetFileName(paths[i]);

                if (ProcessImage(paths[i], pixelGroup, containerGroup, config, OutDir, fileName))
                    ok++;
                else
                    Debug.LogError(Tag + " [" + (i + 1) + "/" + paths.Count + "] " + srcName + " 处理失败，已跳过。");
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("批量图片转关卡", "完成：成功 " + ok + " / " + paths.Count + " 张。", "确定");
        }

        // ===== 收集图片 =====

        private static List<string> CollectImagePaths()
        {
            var paths = new List<string>();

            // 优先：Project 窗口选中的 Texture
            foreach (var obj in Selection.objects)
            {
                if (obj is Texture2D)
                {
                    string p = AssetDatabase.GetAssetPath(obj);
                    if (!string.IsNullOrEmpty(p))
                        paths.Add(p);
                }
            }

            // 回退：选择文件夹，处理其中所有图片
            if (paths.Count == 0)
            {
                string folder = EditorUtility.OpenFolderPanel("选择图片文件夹", EditorPathMemory.LoadDir(SourceDirKey), "");
                if (string.IsNullOrEmpty(folder))
                    return paths;
                EditorPathMemory.SaveDir(SourceDirKey, folder);

                var list = new List<string>(Directory.GetFiles(folder));
                list.Sort(System.StringComparer.OrdinalIgnoreCase);
                foreach (var f in list)
                {
                    string ext = Path.GetExtension(f).ToLowerInvariant();
                    if (ext == ".png" || ext == ".jpg" || ext == ".jpeg")
                        paths.Add(f);
                }
            }

            return paths;
        }

        // ===== 单张图片处理 =====

        private static bool ProcessImage(string path, PixelGroup pixelGroup, ContainerGroup containerGroup,
            ColorConfig config, string outDir, string fileName)
        {
            var tex = LoadTexture(path);
            if (tex == null)
                return false;

            int columns = tex.width / CellSize;
            int rows = tex.height / CellSize;

            bool ok = false;
            if (columns <= 0 || rows <= 0)
            {
                Debug.LogError(Tag + " 图片尺寸过小：" + path + "（" + tex.width + "×" + tex.height + "，每格 " + CellSize + "px）。");
            }
            else
            {
                try
                {
                    ok = ProcessImageInner(pixelGroup, containerGroup, config, outDir, fileName, tex, columns, rows);
                }
                catch (System.Exception e)
                {
                    Debug.LogError(Tag + " 处理 " + path + " 异常：" + e.GetType().Name + " - " + e.Message + "\n" + e.StackTrace);
                    ok = false;
                }
            }

            Object.DestroyImmediate(tex);
            return ok;
        }

        private static bool ProcessImageInner(PixelGroup pixelGroup, ContainerGroup containerGroup,
            ColorConfig config, string outDir, string fileName, Texture2D tex, int columns, int rows)
        {
            Undo.RegisterFullObjectHierarchyUndo(pixelGroup.gameObject, "批量图片转关卡");
            Undo.RegisterFullObjectHierarchyUndo(containerGroup.gameObject, "批量图片转关卡");

            // 1. 清空两个 Group
            pixelGroup.ClearPixels();
            containerGroup.ClearContainers();

            // 2. 按图片像素设置 PixelGroup 网格数并生成网格
            pixelGroup.columns = columns;
            pixelGroup.rows = rows;
            pixelGroup.tailRows = 0;
            GeneratePixelGrid(pixelGroup);

            // 3. 导入图片颜色
            var palette = BuildPalette(config);
            ImportPixelColors(pixelGroup, tex, palette, config);

            // 4. 生成 ContainerGroup
            if (!GenerateContainers(containerGroup, pixelGroup, config))
                return false;

            // 5. 导出 JSON
            var data = LevelDataExporter.BuildLevelData(pixelGroup, containerGroup);
            string json = JsonUtility.ToJson(data, true);
            WriteJson(outDir, fileName, json);

            EditorUtility.SetDirty(pixelGroup);
            EditorUtility.SetDirty(containerGroup);

            Debug.Log(Tag + " 已生成：" + fileName + "（像素 " + columns + "×" + rows +
                      "，容器 " + data.container.items.Length + " 个）");
            return true;
        }

        // ===== 步骤实现 =====

        private static void GeneratePixelGrid(PixelGroup pg)
        {
            for (int col = 0; col < pg.columns; col++)
            {
                for (int row = 0; row < pg.TotalRows; row++)
                {
                    var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    go.name = "Pixel_" + row + "_" + col;
                    go.transform.SetParent(pg.transform, false);
                    go.transform.localPosition = pg.GetLocalPosition(col, row);
                    go.transform.localScale = Vector3.one * pg.unitSize;

                    Undo.RegisterCreatedObjectUndo(go, "生成像素网格");

                    var item = Undo.AddComponent<PixelItem>(go);
                    item.gridX = col;
                    item.gridZ = row;
                }
            }
            pg.RebuildGrid();
        }

        private static void ImportPixelColors(PixelGroup pg, Texture2D tex, Color[] palette, ColorConfig config)
        {
            var cells = SquareGridColorTool.Import(tex, CellSize, palette);
            pg.RebuildGrid();

            foreach (var (col, row, colorIndex) in cells)
            {
                if (colorIndex < 0)
                    continue;

                // 图片顶行 = 最前排（gridZ 0）：把工具按「底行 0」采样的 row 反转到 gridZ
                int gridZ = pg.TotalRows - 1 - row;
                var item = pg.GetItem(col, gridZ);
                if (item == null)
                    continue;

                item.colorId = colorIndex;
                item.ApplyMaterial(config);
                EditorUtility.SetDirty(item);
            }
        }

        private static bool GenerateContainers(ContainerGroup cg, PixelGroup pg, ColorConfig config)
        {
            // 1. 扫描像素 → (layer, color)，layer 0 = 最前排（gridZ 0）
            int colorCount = config != null ? config.Count : 0;
            var pixels = new List<(int layer, int color)>();
            int maxColorId = -1;
            foreach (var it in pg.GetComponentsInChildren<PixelItem>())
            {
                if (it == null || !pg.IsInRange(it.gridX, it.gridZ))
                    continue;
                pixels.Add((it.gridZ, it.colorId));
                if (it.colorId > maxColorId)
                    maxColorId = it.colorId;
            }

            if (pixels.Count == 0)
            {
                Debug.LogError(Tag + " PixelGroup 中没有像素，无法生成容器。");
                return false;
            }

            colorCount = Mathf.Max(colorCount, maxColorId + 1);

            // 2. 规划容器
            int minCap = Mathf.Max(1, cg.minCapacity);
            int maxCap = Mathf.Max(minCap, cg.maxCapacity);
            var planner = new ContainerGenerationPlanner();
            planner.Rebuild(pixels, pg.TotalRows, colorCount, cg.maxSpanLayers);

            var plans = new List<ContainerPlan>();
            while (planner.HasPixels)
            {
                var pack = planner.PullPack(minCap, maxCap);
                if (pack.capacity <= 0)
                    break;
                plans.Add(new ContainerPlan(pack.color, pack.capacity));
            }

            // 3. 自动计算 rows（保留 columns），保证格子数 >= 容器数
            int columns = Mathf.Max(1, cg.columns);
            int rows = Mathf.Max(1, Mathf.CeilToInt((float)plans.Count / columns));
            cg.columns = columns;
            cg.rows = rows;

            // 4. 按行优先生成（步骤 1 已清空旧容器）
            int idx = 0;
            for (int r = 0; r < rows && idx < plans.Count; r++)
            {
                for (int c = 0; c < columns && idx < plans.Count; c++)
                {
                    var plan = plans[idx];
                    var go = InstantiateTemplate(cg.containerPrefab, cg.transform);
                    go.name = "Container_" + c + "_" + r;
                    go.transform.localPosition = cg.GetLocalPosition(c, r);

                    var item = go.GetComponent<ContainerItem>();
                    if (item == null)
                    {
                        Debug.LogError(Tag + " 预制体 " + cg.containerPrefab.name + " 缺少 ContainerItem 组件，已销毁实例。", go);
                        Object.DestroyImmediate(go);
                        continue;
                    }

                    item.gridX = c;
                    item.gridZ = r;
                    item.colorId = plan.colorId;
                    item.SetCapacity(plan.capacity);
                    item.ApplyMaterial(config);
                    EditorUtility.SetDirty(item);

                    Undo.RegisterCreatedObjectUndo(go, "生成 Containers");
                    idx++;
                }
            }

            cg.RebuildGrid();
            return true;
        }

        // ===== 辅助 =====

        private static Texture2D LoadTexture(string path)
        {
            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(Path.GetFullPath(path));
            }
            catch (System.Exception e)
            {
                Debug.LogError(Tag + " 读取图片失败 " + path + "：" + e.Message);
                return null;
            }

            var tex = new Texture2D(2, 2);
            if (!tex.LoadImage(bytes))
            {
                Object.DestroyImmediate(tex);
                Debug.LogError(Tag + " 无法解码图片 " + path);
                return null;
            }
            return tex;
        }

        private static Color[] BuildPalette(ColorConfig config)
        {
            if (config == null || config.materials == null)
                return new Color[0];
            var colors = new Color[config.materials.Length];
            for (int i = 0; i < config.materials.Length; i++)
            {
                var mat = config.materials[i];
                colors[i] = mat != null ? mat.color : Color.magenta;
            }
            return colors;
        }

        /// <summary>统计 Assets/LevelData 中现有 Level*.json 数量（不含 .meta）。</summary>
        private static int CountExistingLevels(string outDir)
        {
            string full = Path.GetFullPath(outDir);
            if (!Directory.Exists(full))
                return 0;
            return Directory.GetFiles(full, "Level*.json", SearchOption.TopDirectoryOnly).Length;
        }

        private static void WriteJson(string outDir, string fileName, string json)
        {
            string fullDir = Path.GetFullPath(outDir);
            if (!Directory.Exists(fullDir))
                Directory.CreateDirectory(fullDir);
            File.WriteAllText(Path.Combine(fullDir, fileName), json, new UTF8Encoding(false));
        }

        /// <summary>实例化模板：预制体资产走 InstantiatePrefab，场景对象走 Object.Instantiate 克隆。</summary>
        private static GameObject InstantiateTemplate(ContainerItem template, Transform parent)
        {
            if (PrefabUtility.GetPrefabAssetType(template) == PrefabAssetType.NotAPrefab)
                return (GameObject)Object.Instantiate(template.gameObject, parent);
            return (GameObject)PrefabUtility.InstantiatePrefab(template.gameObject, parent);
        }
    }
}
