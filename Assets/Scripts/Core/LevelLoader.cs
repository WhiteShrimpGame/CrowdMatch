using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 关卡加载器（运行时）：把 JSON TextAsset 解析为 LevelData，并应用到场景里的
    /// PixelGroup / ContainerGroup（设置布局字段 + 清空旧子物体 + spawn 新子物体）。
    /// </summary>
    public static class LevelLoader
    {
        /// <summary>解析关卡 JSON TextAsset；失败返回 null。</summary>
        public static LevelData Parse(TextAsset asset)
        {
            if (asset == null || string.IsNullOrEmpty(asset.text))
            {
                Debug.LogError("[LevelLoader] TextAsset 为空，无法解析关卡。");
                return null;
            }
            return ParseJson(asset.text, asset.name);
        }

        /// <summary>解析关卡 JSON 字符串（供运行时 TextAsset 与编辑器文件导入共用）；失败返回 null。</summary>
        public static LevelData ParseJson(string json, string sourceName = "JSON")
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("[LevelLoader] JSON 为空，无法解析关卡。");
                return null;
            }

            try
            {
                var data = JsonUtility.FromJson<LevelData>(json);
                if (data == null)
                {
                    Debug.LogError("[LevelLoader] 关卡 JSON 解析失败：" + sourceName);
                    return null;
                }
                return data;
            }
            catch (System.Exception e)
            {
                Debug.LogError("[LevelLoader] 关卡 JSON 解析异常（" + sourceName + "）：" + e.Message);
                return null;
            }
        }

        /// <summary>把关卡数据应用到两个网格（空的 group 参数会被跳过）。</summary>
        public static void Apply(PixelGroup pixelGroup, ContainerGroup containerGroup, LevelData data, ColorConfig colorConfig)
        {
            if (data == null)
                return;
            if (pixelGroup != null)
                ApplyPixel(pixelGroup, data.pixel, colorConfig);
            if (containerGroup != null)
                ApplyContainer(containerGroup, data.container, colorConfig);
        }

        private static void ApplyPixel(PixelGroup pg, LevelData.PixelData d, ColorConfig config)
        {
            int columns = Mathf.Max(1, d.columns);
            int totalRows = Mathf.Max(0, d.rows) + Mathf.Max(0, d.tailRows);
            int expected = columns * totalRows;
            if (d.cells == null || d.cells.Length < expected)
            {
                Debug.LogError("[LevelLoader] 像素 cells 数量不足（需要 " + expected +
                    "，实际 " + (d.cells != null ? d.cells.Length : 0) + "），跳过 PixelGroup 加载。");
                return;
            }

            pg.columns = columns;
            pg.rows = Mathf.Max(0, d.rows);
            pg.tailRows = Mathf.Max(0, d.tailRows);
            pg.unitSize = d.unitSize > 0.0001f ? d.unitSize : 1f;

            pg.ClearPixels();

            for (int r = 0; r < totalRows; r++)
                for (int c = 0; c < columns; c++)
                {
                    int colorId = d.cells[r * columns + c];
                    pg.SpawnPixel(c, r, colorId, config);
                }

            pg.RebuildGrid();
        }

        private static void ApplyContainer(ContainerGroup cg, LevelData.ContainerData d, ColorConfig config)
        {
            cg.columns = Mathf.Max(1, d.columns);
            cg.rows = Mathf.Max(1, d.rows);

            cg.ClearContainers();

            if (d.items != null)
            {
                foreach (var it in d.items)
                {
                    if (!cg.IsInRange(it.x, it.y))
                    {
                        Debug.LogWarning("[LevelLoader] 容器越界被忽略：x=" + it.x + " y=" + it.y);
                        continue;
                    }
                    cg.SpawnContainer(it.x, it.y, it.colorId, it.capacity, config);
                }
            }

            cg.RebuildGrid();
        }

        /// <summary>
        /// 洗牌容器：随机打乱各容器在网格上的摆放位置（每个容器的 colorId / capacity 保持不变），
        /// 使关卡每次初始化时容器排列不同，同时不影响各颜色总容量与可解性。
        /// </summary>
        public static void ShuffleContainers(LevelData.ContainerData d)
        {
            if (d == null || d.items == null || d.items.Length < 2)
                return;

            var xs = new int[d.items.Length];
            var ys = new int[d.items.Length];
            for (int i = 0; i < d.items.Length; i++)
            {
                xs[i] = d.items[i].x;
                ys[i] = d.items[i].y;
            }

            // Fisher-Yates 洗牌位置
            for (int i = d.items.Length - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                int tx = xs[i]; xs[i] = xs[j]; xs[j] = tx;
                int ty = ys[i]; ys[i] = ys[j]; ys[j] = ty;
            }

            // 洗牌后的位置重新分配回各容器（colorId / capacity 不变）
            for (int i = 0; i < d.items.Length; i++)
            {
                d.items[i].x = xs[i];
                d.items[i].y = ys[i];
            }
        }
    }
}
