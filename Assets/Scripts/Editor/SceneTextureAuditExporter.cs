using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrowdMatch
{
    /// <summary>
    /// 场景贴图审计：扫描**当前活动场景**引用的全部 Texture2D，按「WebGL 目标下的显存估算」降序导出一份 CSV。
    ///
    /// 用途：做 WebGL 包体 / 显存优化时，先看清这个场景到底用了哪些贴图、各自按什么格式和尺寸导入、大概多大，
    /// 再挑最大的几张去调 Max Size / 换压缩格式。Project 窗口只能一张张点 Inspector，没有全局视图。
    ///
    /// 【收集口径】场景全部根物体的依赖闭包（<see cref="EditorUtility.CollectDependencies"/>，即 Unity 算构建依赖那套机制）
    /// + 场景级根（天空盒材质、烘焙贴图）。只保留落在 <c>Assets/</c> 下的真实工程资产；内建资源、场景内嵌烘焙贴图、
    /// 运行时生成的贴图会被排除，并在摘要里**如实计数**，不静默吞掉。
    ///
    /// 【WebGL 有效设置】<c>GetPlatformTextureSettings("WebGL")</c> 勾了平台覆盖就用它，没勾就用
    /// <c>GetDefaultPlatformTextureSettings()</c>（Default 那一栏）。报告里给出「平台覆盖」列 —— 缺了它，
    /// 读的人无法判断那一格是 WebGL 专属配置还是全局默认。
    ///
    /// 【显存估算】**是估算，不是 Unity 实测**：Unity 不提供任何 API 给出某张贴图在 WebGL 构建里的实际字节数。
    /// 口径 = <c>导入后宽 × 导入后高 × 每像素位数 ÷ 8 × (mipmap ? 4/3 : 1)</c>。
    /// 「导入后宽高」直接取 <c>Texture2D.width/height</c>（Unity 早已按 Max Size 缩放完毕），
    /// 每像素位数由压缩格式查表（<see cref="BitsPerPixel"/>，未收录的格式会把估算留空并汇总 warning）。
    /// 报告里同时给出 bpp 列，让这个数字可被复核。
    /// </summary>
    public static class SceneTextureAuditExporter
    {
        private const string Tag = "[SceneTextureAudit]";

        /// <summary>「上次路径」EditorPrefs 键。</summary>
        private const string PathKey = "CrowdMatch.SceneTextureAuditExporter.LastExportPath";

        /// <summary>平台设置里 WebGL 那一栏的名字。</summary>
        private const string WebGlPlatform = "WebGL";

        /// <summary>导入器没给出有效 Max Size 时的兜底值（Unity 的默认值）。</summary>
        private const int DefaultMaxSize = 2048;

        /// <summary>CSV 列头（顺序与 <see cref="WriteCsv"/> 里逐列填的一致）。</summary>
        private static readonly string[] Header =
        {
            "名称", "路径", "纹理类型", "原始宽", "原始高", "导入后宽", "导入后高",
            "WebGL压缩格式", "平台覆盖", "最大尺寸", "Mipmap", "压缩质量", "Crunched", "可读写",
            "每像素位数(bpp)", "显存估算(B)", "显存估算(KB)", "源文件大小(KB)", "备注",
        };

        [MenuItem("CrowdMatch/更多工具/导出场景贴图报告 (CSV)", false, MenuPriority.More + MenuPriority.Seg3)]
        public static void ExportSceneTextureReport()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(sceneName))
                sceneName = "Untitled";

            var rows = BuildRows(out int skippedNonAsset, out var unknownFormats);

            string defaultDir = ReportsDir();
            Directory.CreateDirectory(defaultDir);
            string defaultName = "SceneTextures_" + sceneName + "_" +
                                 System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";

            string path = EditorUtility.SaveFilePanel("导出场景贴图报告",
                EditorPathMemory.LoadDir(PathKey, defaultDir), defaultName, "csv");
            if (string.IsNullOrEmpty(path))
                return;
            EditorPathMemory.SaveDir(PathKey, path);

            WriteCsv(path, rows);
            if (path.Replace('\\', '/').Contains("/Assets/"))
                AssetDatabase.Refresh();

            double vramBytes = 0;
            long sourceBytes = 0;
            var estimated = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                vramBytes += rows[i].estimateBytes;
                sourceBytes += rows[i].sourceBytes;
                if (rows[i].estimateBytes > 0)
                    estimated++;
            }

            var sb = new StringBuilder();
            sb.Append("共 ").Append(rows.Count).Append(" 张贴图（其中 ").Append(estimated)
              .Append(" 张可估算显存）。\n\n");
            sb.Append("显存估算合计：").Append(Kb(vramBytes)).Append(" KB\n");
            sb.Append("源文件合计：").Append(Kb(sourceBytes)).Append(" KB\n");
            if (skippedNonAsset > 0)
                sb.Append("\n已排除非工程资产（内建资源 / 场景内嵌烘焙贴图 / 运行时生成）：")
                  .Append(skippedNonAsset).Append(" 张\n");
            if (unknownFormats.Count > 0)
                sb.Append("\n有 ").Append(unknownFormats.Count).Append(" 种压缩格式未收录，其显存估算留空（格式名见 Console）\n");

            Debug.Log(Tag + " 已导出 " + rows.Count + " 张贴图 → " + path);
            if (unknownFormats.Count > 0)
                Debug.LogWarning(Tag + " 未收录的压缩格式：" + string.Join("、", unknownFormats) +
                                 "（可在 BitsPerPixelTable 里补表）");

            EditorUtility.DisplayDialog("导出场景贴图报告",
                "已导出到：\n" + path + "\n\n" + sb, "确定");
        }

        /// <summary>Play 模式下场景是临时态，审计出来没意义，菜单置灰。</summary>
        [MenuItem("CrowdMatch/更多工具/导出场景贴图报告 (CSV)", true, MenuPriority.More + MenuPriority.Seg3)]
        private static bool ValidateExportSceneTextureReport() => !EditorApplication.isPlaying;

        // ===================== 收集 =====================

        /// <summary>报告的一行（一张贴图）。显存估算字段留 0 表示无法估算（无导入器 / 格式未收录）。</summary>
        private sealed class Row
        {
            public string name;
            public string path;
            public string textureType;
            public int sourceW, sourceH;        // 源图像素（未缩放）
            public int importedW, importedH;    // Unity 导入后（已按 Max Size 缩放）
            public string format;               // WebGL 有效压缩格式（Automatic 时是推定结果）
            public bool overridden;             // WebGL 是否勾了平台覆盖
            public int maxSize;
            public bool mipmap;
            public int quality;                 // 仅 crunched 有意义，-1 = 不适用
            public bool crunched;
            public bool readable;
            public double bpp;                  // 0 = 格式未收录
            public double estimateBytes;
            public long sourceBytes;
            public string note;
        }

        private static List<Row> BuildRows(out int skippedNonAsset, out HashSet<string> unknownFormats)
        {
            skippedNonAsset = 0;
            unknownFormats = new HashSet<string>();

            var candidates = new HashSet<Texture2D>();

            // 场景全部根物体（依赖闭包会把 Renderer → Material → 贴图、预制体引用的资源一并展开）
            var roots = new List<Object>(SceneManager.GetActiveScene().GetRootGameObjects());
            // 天空盒材质不在任何 GameObject 上，得单独当根（Panoramic 天空盒用的就是 Texture2D）
            if (RenderSettings.skybox != null)
                roots.Add(RenderSettings.skybox);
            AddFromDependencies(candidates, roots.ToArray());

            // 场景级烘焙贴图：CollectDependencies 未必带得出来，直接补
            var lightmaps = LightmapSettings.lightmaps;
            if (lightmaps != null)
            {
                foreach (var lm in lightmaps)
                {
                    if (lm == null)
                        continue;
                    if (lm.lightmapColor != null)
                        candidates.Add(lm.lightmapColor);
                    if (lm.lightmapDir != null)
                        candidates.Add(lm.lightmapDir);
                }
            }

            var rows = new List<Row>(candidates.Count);
            var seen = new HashSet<string>();
            foreach (var tex in candidates)
            {
                if (tex == null)
                    continue;

                string path = (AssetDatabase.GetAssetPath(tex) ?? "").Replace('\\', '/');
                // 只收真正的工程资产：内建资源（Library/…、Resources/unity_builtin_extra）、
                // 场景内嵌烘焙贴图（路径 = .unity）、运行时生成的贴图（无路径）一律排除
                if (path.Length == 0
                    || !path.StartsWith("Assets/", System.StringComparison.Ordinal)
                    || path.EndsWith(".unity", System.StringComparison.OrdinalIgnoreCase))
                {
                    skippedNonAsset++;
                    continue;
                }

                if (!seen.Add(path + "|" + tex.name))
                    continue;

                rows.Add(BuildRow(tex, path, unknownFormats));
            }

            // 报告的主要用途就是从大到小找优化目标
            rows.Sort((a, b) => b.estimateBytes.CompareTo(a.estimateBytes));
            return rows;
        }

        /// <summary>把依赖闭包里的贴图收进 <paramref name="into"/>；Sprite 额外解析成它的 Texture2D（图集）。</summary>
        private static void AddFromDependencies(HashSet<Texture2D> into, Object[] roots)
        {
            if (roots == null || roots.Length == 0)
                return;

            var deps = EditorUtility.CollectDependencies(roots);
            if (deps == null)
                return;

            foreach (var o in deps)
            {
                if (o is Texture2D tex)
                    into.Add(tex);
                else if (o is Sprite sprite && sprite.texture != null)
                    into.Add(sprite.texture);
            }
        }

        // ===================== 单行 =====================

        private static Row BuildRow(Texture2D tex, string path, HashSet<string> unknownFormats)
        {
            var row = new Row
            {
                name = tex.name,
                path = path,
                textureType = "(无 TextureImporter)",
                format = "(无 TextureImporter)",
                note = "无 TextureImporter",
                sourceW = tex.width,
                sourceH = tex.height,
                importedW = tex.width,
                importedH = tex.height,
                quality = -1,
                sourceBytes = SourceFileBytes(path),
            };

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return row;

            row.textureType = importer.textureType.ToString();
            row.mipmap = importer.mipmapEnabled;
            row.readable = importer.isReadable;

            // 源图尺寸要问导入器：texture.width/height 已经是缩放后的了
            importer.GetSourceTextureWidthAndHeight(out int srcW, out int srcH);
            if (srcW > 0 && srcH > 0)
            {
                row.sourceW = srcW;
                row.sourceH = srcH;
            }

            var platform = importer.GetPlatformTextureSettings(WebGlPlatform);
            row.overridden = platform.overridden;
            var eff = platform.overridden ? platform : importer.GetDefaultPlatformTextureSettings();

            row.maxSize = eff.maxTextureSize > 0 ? eff.maxTextureSize : DefaultMaxSize;
            row.crunched = eff.crunchedCompression;
            row.quality = eff.crunchedCompression ? eff.compressionQuality : -1;
            row.format = eff.format.ToString();

            bool hasAlpha = importer.DoesSourceTextureHaveAlpha();
            bool compressed = eff.textureCompression != TextureImporterCompression.Uncompressed;

            if (row.format == "Automatic")
            {
                // Automatic 由 Unity 按平台 + 压缩开关自行挑格式，无法精确预知 → 推定并标明
                if (compressed)
                    row.bpp = hasAlpha || importer.textureType == TextureImporterType.NormalMap ? 8 : 4;
                else
                    row.bpp = hasAlpha ? 32 : 24;

                string guess = row.bpp == 8 ? "DXT5/BC3" : row.bpp == 4 ? "DXT1/BC1" : row.bpp == 32 ? "RGBA32" : "RGB24";
                row.format = "Automatic → 推定 " + guess;
                row.note = "格式 Automatic，按「" + (compressed ? "压缩" : "未压缩") + " + " +
                           (hasAlpha ? "含 Alpha" : "不含 Alpha") + "」推定";
            }
            else
            {
                row.bpp = BitsPerPixel(row.format);
                if (row.bpp <= 0)
                {
                    row.note = "压缩格式未收录，显存估算留空";
                    unknownFormats.Add(row.format);
                    return row;
                }
            }

            double mipFactor = row.mipmap ? 4.0 / 3.0 : 1.0;
            row.estimateBytes = row.importedW * (double)row.importedH * row.bpp / 8.0 * mipFactor;
            return row;
        }

        /// <summary>源文件在磁盘上的字节数（按 Assets 相对路径换算成绝对路径）。取不到记 0。</summary>
        private static long SourceFileBytes(string assetPath)
        {
            var file = new FileInfo(Path.Combine(ProjectRoot(), assetPath));
            return file.Exists ? file.Length : 0L;
        }

        // ===================== 压缩格式 → 每像素位数 =====================

        /// <summary>
        /// 压缩格式 → 每像素位数（bpp），显存估算的分母。
        ///
        /// 用**格式名字符串**而不是枚举值查表：枚举成员在各 Unity 版本间增删过
        /// （ASTC 的 `ASTC_4x4` 与已废弃的 `ASTC_RGB_4x4` 并存），按名字查不会因为版本差异编译不过；
        /// 查不到返回 0，调用方把估算留空并把格式名汇总成 warning，不会静默算错。
        /// Crunched 变体沿用基格式位数：crunched 只压磁盘体积，GPU 端解压后占用不变。
        /// </summary>
        private static readonly Dictionary<string, double> BitsPerPixelTable = new Dictionary<string, double>
        {
            // BC / DXT
            { "DXT1", 4 }, { "DXT1Crunched", 4 }, { "BC1", 4 },
            { "DXT5", 8 }, { "DXT5Crunched", 8 }, { "BC2", 8 }, { "BC3", 8 },
            { "BC4", 4 }, { "BC5", 8 }, { "BC6H", 8 }, { "BC7", 8 },
            // ETC
            { "ETC_RGB4", 4 }, { "ETC_RGB4Crunched", 4 }, { "ETC_RGB4_3DS", 4 }, { "ETC_RGBA8_3DS", 8 },
            { "ETC2_RGB", 4 }, { "ETC2_RGBA1", 4 }, { "ETC2_RGBA8", 8 }, { "ETC2_RGBA8Crunched", 8 },
            // ASTC（4x4=8bpp，块越大越省）
            { "ASTC_4x4", 8 }, { "ASTC_5x5", 5.12 }, { "ASTC_6x6", 3.56 },
            { "ASTC_8x8", 2 }, { "ASTC_10x10", 1.28 }, { "ASTC_12x12", 0.89 },
            { "ASTC_RGB_4x4", 8 }, { "ASTC_RGB_5x5", 5.12 }, { "ASTC_RGB_6x6", 3.56 },
            { "ASTC_RGB_8x8", 2 }, { "ASTC_RGB_10x10", 1.28 }, { "ASTC_RGB_12x12", 0.89 },
            { "ASTC_RGBA_4x4", 8 }, { "ASTC_RGBA_5x5", 5.12 }, { "ASTC_RGBA_6x6", 3.56 },
            { "ASTC_RGBA_8x8", 2 }, { "ASTC_RGBA_10x10", 1.28 }, { "ASTC_RGBA_12x12", 0.89 },
            { "ASTC_HDR_4x4", 8 }, { "ASTC_HDR_5x5", 5.12 }, { "ASTC_HDR_6x6", 3.56 },
            { "ASTC_HDR_8x8", 2 }, { "ASTC_HDR_10x10", 1.28 }, { "ASTC_HDR_12x12", 0.89 },
            { "ASTC_RGB_HDR_4x4", 8 }, { "ASTC_RGB_HDR_5x5", 5.12 }, { "ASTC_RGB_HDR_6x6", 3.56 },
            { "ASTC_RGB_HDR_8x8", 2 }, { "ASTC_RGB_HDR_10x10", 1.28 }, { "ASTC_RGB_HDR_12x12", 0.89 },
            // PVRTC
            { "PVRTC_RGB2", 2 }, { "PVRTC_RGBA2", 2 }, { "PVRTC_RGB4", 4 }, { "PVRTC_RGBA4", 4 },
            { "PVRTC_2BPP_RGB", 2 }, { "PVRTC_2BPP_RGBA", 2 }, { "PVRTC_4BPP_RGB", 4 }, { "PVRTC_4BPP_RGBA", 4 },
            // 未压缩 / 高位深
            { "Alpha8", 8 }, { "Alpha16", 16 },
            { "R8", 8 }, { "R16", 16 }, { "RHalf", 16 }, { "RFloat", 32 },
            { "RG16", 16 }, { "RG32", 32 }, { "RGHalf", 32 }, { "RGFloat", 64 },
            { "RGB24", 24 }, { "RGB48", 48 }, { "RGB565", 16 }, { "RGB9e5Float", 32 },
            { "RGBA32", 32 }, { "ARGB32", 32 }, { "BGRA32", 32 },
            { "RGBA4444", 16 }, { "ARGB4444", 16 },
            { "RGBAHalf", 64 }, { "RGBAFloat", 128 },
            { "RGBM", 32 }, { "RGBMHalf", 64 },
        };

        private static double BitsPerPixel(string formatName)
        {
            if (string.IsNullOrEmpty(formatName))
                return 0;
            return BitsPerPixelTable.TryGetValue(formatName, out double bpp) ? bpp : 0;
        }

        // ===================== CSV =====================

        private static void WriteCsv(string path, List<Row> rows)
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", Header));

            var f = new string[Header.Length];
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                f[0] = r.name;
                f[1] = r.path;
                f[2] = r.textureType;
                f[3] = r.sourceW.ToString(CultureInfo.InvariantCulture);
                f[4] = r.sourceH.ToString(CultureInfo.InvariantCulture);
                f[5] = r.importedW.ToString(CultureInfo.InvariantCulture);
                f[6] = r.importedH.ToString(CultureInfo.InvariantCulture);
                f[7] = r.format;
                f[8] = r.overridden ? "是" : "否";
                f[9] = r.maxSize.ToString(CultureInfo.InvariantCulture);
                f[10] = r.mipmap ? "是" : "否";
                f[11] = r.quality >= 0 ? r.quality.ToString(CultureInfo.InvariantCulture) : "";
                f[12] = r.crunched ? "是" : "否";
                f[13] = r.readable ? "是" : "否";
                f[14] = r.bpp > 0 ? r.bpp.ToString("0.##", CultureInfo.InvariantCulture) : "";
                f[15] = r.estimateBytes > 0 ? System.Math.Round(r.estimateBytes).ToString("0", CultureInfo.InvariantCulture) : "";
                f[16] = r.estimateBytes > 0 ? Kb(r.estimateBytes) : "";
                f[17] = r.sourceBytes > 0 ? (r.sourceBytes / 1024.0).ToString("0.0", CultureInfo.InvariantCulture) : "";
                f[18] = r.note;

                for (int c = 0; c < f.Length; c++)
                    f[c] = Escape(f[c]);
                sb.AppendLine(string.Join(",", f));
            }

            // 带 BOM：没有 BOM 时 Excel 会把中文列头按本地代码页解成乱码（与 JSON 导出的 UTF8Encoding(false) 是**故意**不同）
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        }

        /// <summary>CSV 字段转义：含逗号 / 引号 / 换行时用双引号包裹，内部的引号翻倍。不假设路径里没有逗号。</summary>
        private static string Escape(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "";
            if (field.IndexOf(',') < 0 && field.IndexOf('"') < 0
                && field.IndexOf('\n') < 0 && field.IndexOf('\r') < 0)
                return field;
            return "\"" + field.Replace("\"", "\"\"") + "\"";
        }

        private static string Kb(double bytes) =>
            (bytes / 1024.0).ToString("0.0", CultureInfo.InvariantCulture);

        // ===================== 路径 =====================

        /// <summary>工程根目录（Assets 的上一级）。</summary>
        private static string ProjectRoot() => Path.GetDirectoryName(Application.dataPath);

        /// <summary>默认输出目录：工程根下的 Reports（放在 Assets 外，避免 CSV 被 Unity 当 TextAsset 导入）。</summary>
        private static string ReportsDir() => Path.Combine(ProjectRoot(), "Reports");
    }
}
