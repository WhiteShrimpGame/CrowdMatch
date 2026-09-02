using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace CrowdMatch
{
    [CustomEditor(typeof(ContainerGroup))]
    public class ContainerGroupEditor : Editor
    {
        private const string Tag = "[ContainerGroup]";

        /// <summary>容器布局导入/导出共用的「上次路径」EditorPrefs 键。</summary>
        private const string LayoutPathKey = "CrowdMatch.ContainerGroup.LastLayoutPath";

        public override void OnInspectorGUI()
        {
            var group = (ContainerGroup)target;

            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "「生成 Containers」删除现有 ContainerItem 子物体，读取 PixelGroup 的颜色分布，\n" +
                "按 minCapacity / maxCapacity 生成颜色与数量完全匹配的容器，并尽量让前后排颜色顺序一致。",
                MessageType.Info);

            if (GUILayout.Button("生成 Containers"))
            {
                GenerateContainers(group);
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "「导出 Containers」把当前容器布局存为文本：每行一列、从最前排到后排，\n" +
                "每个容器记作 颜色ID/容量，同行（同列前后）用空格分隔，不记录空格。\n" +
                "「导入 Containers」从文本重建容器布局（按文本调整 columns / rows 并重建子物体）。",
                MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("导出 Containers (txt)"))
            {
                ExportContainers(group);
            }
            if (GUILayout.Button("导入 Containers (txt)"))
            {
                ImportContainers(group);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void GenerateContainers(ContainerGroup group)
        {
            Debug.Log(Tag + " ========== 生成 Containers 开始 ==========");
            Debug.Log(Tag + " ContainerGroup 配置：columns=" + group.columns + " rows=" + group.rows +
                      " xSpacing=" + group.xSpacing + " zSpacing=" + group.zSpacing +
                      " minCapacity=" + group.minCapacity + " maxCapacity=" + group.maxCapacity);

            try
            {
                var pixelGroup = group.pixelGroup;
                if (pixelGroup == null)
                    pixelGroup = UnityEngine.Object.FindObjectOfType<PixelGroup>();
                if (pixelGroup == null)
                {
                    Debug.LogError(Tag + " 找不到 PixelGroup（字段未指定且场景中无 PixelGroup）。");
                    EditorUtility.DisplayDialog("生成 Containers", "找不到 PixelGroup，请在字段中指定。", "确定");
                    return;
                }
                Debug.Log(Tag + " 使用 PixelGroup：" + pixelGroup.name +
                          "（columns=" + pixelGroup.columns + " rows=" + pixelGroup.rows + "）");

                if (group.containerPrefab == null)
                {
                    Debug.LogError(Tag + " containerPrefab 为空，请指定 ContainerItem 预制体。");
                    EditorUtility.DisplayDialog("生成 Containers", "请先指定 ContainerItem 预制体（containerPrefab）。", "确定");
                    return;
                }
                Debug.Log(Tag + " 使用预制体：" + group.containerPrefab.name);

                var prefabType = PrefabUtility.GetPrefabAssetType(group.containerPrefab);
                Debug.Log(Tag + " 预制体资产类型：" + prefabType +
                          "，路径：" + AssetDatabase.GetAssetPath(group.containerPrefab));
                if (prefabType == PrefabAssetType.NotAPrefab)
                    Debug.LogWarning(Tag + " containerPrefab 不是预制体资产，将改用 Object.Instantiate 克隆；建议改用 Prefabs 文件夹中的预制体。");

                var colorConfig = ColorConfigLocator.Find();
                Debug.Log(Tag + " colorConfig = " + (colorConfig != null ? colorConfig.name : "NULL（找不到 ColorConfig）"));

                // 1) 扫描 PixelItem → (layer, color) 列表，layer 0 = 最前排（gridZ 0 = Z 最大）
                int colorCount = colorConfig != null ? colorConfig.Count : 0;
                var pixels = new List<(int layer, int color)>();
                int maxColorId = -1;
                int totalPixels = 0;

                foreach (var it in pixelGroup.GetComponentsInChildren<PixelItem>())
                {
                    if (!pixelGroup.IsInRange(it.gridX, it.gridZ))
                    {
                        Debug.LogWarning(Tag + " PixelItem 越界被忽略：gridX=" + it.gridX + " gridZ=" + it.gridZ +
                                         " colorId=" + it.colorId);
                        continue;
                    }
                    int layer = it.gridZ; // PixelGroup 前排（gridZ 0 = Z 最大）→ layer 0
                    pixels.Add((layer, it.colorId));
                    if (it.colorId > maxColorId) maxColorId = it.colorId;
                    totalPixels++;
                }

                if (totalPixels == 0)
                {
                    Debug.LogError(Tag + " PixelGroup 中没有有效的 PixelItem，请先在 PixelGroup 上「生成网格」。");
                    EditorUtility.DisplayDialog("生成 Containers", "PixelGroup 中没有 PixelItem，请先生成网格。", "确定");
                    return;
                }

                colorCount = Mathf.Max(colorCount, maxColorId + 1);
                Debug.Log(Tag + " 扫描到 " + totalPixels + " 个像素，颜色上限 " + colorCount +
                          "，span=" + group.maxSpanLayers);

                // 2) 用分层颜色池生成容器计划（大色块优先，逐层抽同色 pack）
                int minCap = Mathf.Max(1, group.minCapacity);
                int maxCap = Mathf.Max(minCap, group.maxCapacity);

                var planner = new ContainerGenerationPlanner();
                planner.Rebuild(pixels, pixelGroup.rows, colorCount, group.maxSpanLayers);

                var containers = new List<ContainerPlan>();
                while (planner.HasPixels)
                {
                    var pack = planner.PullPack(minCap, maxCap);
                    if (pack.capacity <= 0) break;
                    containers.Add(new ContainerPlan(pack.color, pack.capacity));
                }

                int containerCount = group.columns * group.rows;
                Debug.Log(Tag + " 需要容器数：" + containers.Count + "，网格格子数：" + containerCount +
                          "（columns×rows=" + group.columns + "×" + group.rows + "）");

                if (containers.Count > containerCount)
                {
                    Debug.LogError(Tag + " 容器数超过网格格子数，无法完全匹配，生成被阻止。");
                    EditorUtility.DisplayDialog("生成 Containers",
                        "需要 " + containers.Count + " 个容器，但 columns × rows 只有 " + containerCount + " 个格子。\n" +
                        "请增大 columns / rows，或调大 minCapacity / maxCapacity。",
                        "确定");
                    return;
                }
                if (containers.Count < containerCount)
                {
                    int suggestedRows = Mathf.CeilToInt((float)containers.Count / Mathf.Max(1, group.columns));
                    Debug.LogWarning(Tag + " 容器数 " + containers.Count + " 少于网格 " + containerCount +
                                     "，尾部格子将留空（建议 rows = " + suggestedRows + "）。");
                }

                // 3) 删除旧子物体
                int removed = 0;
                for (int i = group.transform.childCount - 1; i >= 0; i--)
                {
                    var child = group.transform.GetChild(i);
                    if (child.GetComponent<ContainerItem>() != null)
                    {
                        Undo.DestroyObjectImmediate(child.gameObject);
                        removed++;
                    }
                }
                Debug.Log(Tag + " 删除旧 ContainerItem 子物体：" + removed + " 个");

                // 4) 前到后布局并实例化
                int idx = 0;
                for (int r = 0; r < group.rows && idx < containers.Count; r++)
                {
                    for (int c = 0; c < group.columns && idx < containers.Count; c++)
                    {
                        var plan = containers[idx];

                        GameObject go;
                        try
                        {
                            go = InstantiateTemplate(group.containerPrefab, group.transform);
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError(Tag + " 实例化预制体失败（idx=" + idx + " col=" + c + " row=" + r +
                                           "）：" + e.GetType().Name + " - " + e.Message);
                            EditorUtility.DisplayDialog("生成 Containers",
                                "实例化预制体失败：\n" + e.Message + "\n\n请确认 containerPrefab 是有效的预制体。", "确定");
                            return;
                        }

                        go.name = "Container_" + c + "_" + r;
                        go.transform.localPosition = group.GetLocalPosition(c, r);

                        var item = go.GetComponent<ContainerItem>();
                        if (item == null)
                        {
                            Debug.LogError(Tag + " 预制体 " + group.containerPrefab.name +
                                           " 缺少 ContainerItem 组件，已销毁该实例。", go);
                            DestroyImmediate(go);
                            continue;
                        }

                        item.gridX = c;
                        item.gridZ = r;
                        item.colorId = plan.colorId;
                        item.SetCapacity(plan.capacity);
                        item.ApplyMaterial(colorConfig);
                        EditorUtility.SetDirty(item);

                        Undo.RegisterCreatedObjectUndo(go, "生成 Containers");

                        Debug.Log(Tag + " 实例化 [" + idx + "]：col=" + c + " row=" + r +
                                  " colorId=" + item.colorId + " capacity=" + item.capacity);
                        idx++;
                    }
                }

                Debug.Log(Tag + " ========== 生成结束：共实例化 " + idx + " 个 ContainerItem ==========");
                EditorUtility.SetDirty(group);
            }
            catch (System.Exception e)
            {
                Debug.LogError(Tag + " 生成 Containers 异常：类型=" + e.GetType().FullName +
                               "\n消息=" + e.Message +
                               "\n堆栈=\n" + e.StackTrace);
                EditorUtility.DisplayDialog("生成 Containers", "生成过程中发生异常：\n" + e.Message, "确定");
            }
        }

        /// <summary>导出容器布局为文本：每行一列（col 0 → columns-1），行内从最前排（row 0）到后排，空格分隔，不记录空格。</summary>
        private void ExportContainers(ContainerGroup group)
        {
            string path = EditorUtility.SaveFilePanel("导出 Containers", EditorPathMemory.LoadDir(LayoutPathKey), "ContainerGroup.txt", "txt");
            if (string.IsNullOrEmpty(path))
                return;
            EditorPathMemory.SaveDir(LayoutPathKey, path);

            group.RebuildGrid();

            var sb = new StringBuilder();
            for (int col = 0; col < group.columns; col++)
            {
                bool first = true;
                for (int row = 0; row < group.rows; row++)
                {
                    var item = group.GetItem(col, row);
                    if (item == null)
                        continue; // 跳过空格
                    if (!first)
                        sb.Append(' ');
                    sb.Append(item.colorId).Append('/').Append(item.capacity);
                    first = false;
                }
                sb.AppendLine();
            }

            File.WriteAllText(path, sb.ToString());
            Debug.Log(Tag + " 已导出容器布局到 " + path + "（" + group.columns + " 列 × " + group.rows + " 行）");
        }

        /// <summary>从文本导入容器布局：每行一列、从最前排到后排，容器记作 颜色ID/容量，空格分隔，不记录空格。</summary>
        private void ImportContainers(ContainerGroup group)
        {
            string path = EditorUtility.OpenFilePanel("导入 Containers", EditorPathMemory.LoadDir(LayoutPathKey), "txt");
            if (string.IsNullOrEmpty(path))
                return;
            EditorPathMemory.SaveDir(LayoutPathKey, path);

            string[] lines;
            try
            {
                lines = File.ReadAllLines(path);
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("导入 Containers", "读取文件失败：\n" + e.Message, "确定");
                return;
            }

            // 解析：columns 为列数（非空行数），每列按前到后记录一列容器，无空格占位
            var columns = new List<List<(int colorId, int capacity)>>();
            int parsedRows = 0;
            foreach (var raw in lines)
            {
                string line = raw.Trim();
                if (line.Length == 0)
                    continue;

                var tokens = line.Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
                var col = new List<(int, int)>(tokens.Length);
                foreach (var tok in tokens)
                {
                    int slash = tok.IndexOf('/');
                    if (slash <= 0 || slash >= tok.Length - 1
                        || !int.TryParse(tok.Substring(0, slash), out int colorId)
                        || !int.TryParse(tok.Substring(slash + 1), out int cap))
                    {
                        EditorUtility.DisplayDialog("导入 Containers",
                            "无法解析条目「" + tok + "」。\n应为 颜色ID/容量（如 3/4）。", "确定");
                        return;
                    }
                    col.Add((colorId, cap));
                }
                columns.Add(col);
                parsedRows = Mathf.Max(parsedRows, col.Count);
            }

            int cols = columns.Count;
            if (cols == 0 || parsedRows == 0)
            {
                EditorUtility.DisplayDialog("导入 Containers", "文件中没有有效的容器数据。", "确定");
                return;
            }

            if (group.containerPrefab == null)
            {
                EditorUtility.DisplayDialog("导入 Containers", "请先指定 ContainerItem 预制体（containerPrefab）。", "确定");
                return;
            }

            var colorConfig = ColorConfigLocator.Find();

            // 按文本调整网格尺寸（columns = 列数，rows = 最深的列）
            Undo.RecordObject(group, "导入 Containers");
            group.columns = cols;
            group.rows = parsedRows;

            // 删除旧子物体
            for (int i = group.transform.childCount - 1; i >= 0; i--)
            {
                var child = group.transform.GetChild(i);
                if (child.GetComponent<ContainerItem>() != null)
                    Undo.DestroyObjectImmediate(child.gameObject);
            }

            // 实例化新容器：每列紧凑地从最前排（row 0）往下铺
            int created = 0;
            for (int c = 0; c < cols; c++)
            {
                var colData = columns[c];
                for (int r = 0; r < colData.Count; r++)
                {
                    var (colorId, capacity) = colData[r];

                    var go = InstantiateTemplate(group.containerPrefab, group.transform);
                    go.name = "Container_" + c + "_" + r;
                    go.transform.localPosition = group.GetLocalPosition(c, r);

                    var item = go.GetComponent<ContainerItem>();
                    if (item == null)
                    {
                        Debug.LogError(Tag + " 预制体 " + group.containerPrefab.name + " 缺少 ContainerItem 组件，已销毁该实例。", go);
                        DestroyImmediate(go);
                        continue;
                    }

                    item.gridX = c;
                    item.gridZ = r;
                    item.colorId = colorId;
                    item.SetCapacity(capacity);
                    item.ApplyMaterial(colorConfig);
                    EditorUtility.SetDirty(item);

                    Undo.RegisterCreatedObjectUndo(go, "导入 Containers");
                    created++;
                }
            }

            group.RebuildGrid();
            EditorUtility.SetDirty(group);

            Debug.Log(Tag + " 已从 " + path + " 导入 " + created + " 个容器（" + cols + " 列 × " + parsedRows + " 行）。");
        }

        /// <summary>实例化模板：预制体资产走 InstantiatePrefab，场景对象走 Object.Instantiate 克隆</summary>
        private static GameObject InstantiateTemplate(ContainerItem template, Transform parent)
        {
            if (PrefabUtility.GetPrefabAssetType(template) == PrefabAssetType.NotAPrefab)
                return (GameObject)Object.Instantiate(template.gameObject, parent);
            return (GameObject)PrefabUtility.InstantiatePrefab(template.gameObject, parent);
        }
    }
}
