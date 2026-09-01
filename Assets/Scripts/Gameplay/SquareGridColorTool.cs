using UnityEngine;
using System.Collections.Generic;

namespace CrowdMatch
{
    /// <summary>
    /// SquareGridColorTool — 方阵（直角）网格颜色导入导出工具 / square-grid color import/export tool.
    ///
    /// Treats an image as a regular grid of equal-size cells. Each cell is a
    /// <c>cellSize</c>×<c>cellSize</c> pixel block:
    ///   - Import: sample the center pixel of each block, match it to the nearest
    ///     palette color, and return the result as a flat list of (col, row, colorIndex).
    ///   - Export: paint each block with the color supplied by a caller callback.
    ///
    /// 将图片视为等尺寸方格组成的直角网格，每格为 cellSize×cellSize 像素块：
    ///   - 导入：采样每块中心像素，匹配最近调色板色，返回 (col, row, colorIndex) 扁平列表。
    ///   - 导出：用调用方回调提供的颜色填充每块。
    ///
    /// Pure data interface — it never touches the scene hierarchy. The caller decides
    /// how (col, row) maps to its own game objects. 纯数据接口——不触碰场景层级。
    /// 调用方自行决定 (col, row) 与自身游戏对象的映射关系。
    /// </summary>
    public static class SquareGridColorTool
    {
        /// <summary>
        /// 从图片导入颜色 / import colors from an image.
        /// </summary>
        /// <param name="tex">源图片 / source texture</param>
        /// <param name="cellSize">每格像素宽度（色块边长）/ cell size in pixels</param>
        /// <param name="palette">目标调色板（纯色数组）/ target palette (pure color array)</param>
        /// <param name="isEnabled">
        /// 可选过滤器：返回 false 的格子跳过（对应"镂空/禁用位"）。
        /// 参数为 (col, row)。为 null 时不过滤。
        /// Optional filter: cells where it returns false are skipped. Args are (col, row). null = no filter.
        /// </param>
        /// <returns>(col, row, colorIndex) 列表 / list of (col, row, colorIndex)</returns>
        public static List<(int col, int row, int colorIndex)> Import(
            Texture2D tex,
            int cellSize,
            Color[] palette,
            System.Func<int, int, bool> isEnabled = null)
        {
            var result = new List<(int, int, int)>();
            if (tex == null || cellSize <= 0) return result;

            int cols = tex.width / cellSize;
            int rows = tex.height / cellSize;
            if (cols == 0 || rows == 0) return result;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    if (isEnabled != null && !isEnabled(col, row)) continue;

                    // 采样色块中心 / sample the block center
                    int px = col * cellSize + cellSize / 2;
                    int py = row * cellSize + cellSize / 2;
                    px = Mathf.Clamp(px, 0, tex.width - 1);
                    py = Mathf.Clamp(py, 0, tex.height - 1);

                    int colorIndex = GridColorMatcher.FindClosestColorIndex(tex.GetPixel(px, py), palette);
                    result.Add((col, row, colorIndex));
                }
            }

            return result;
        }

        /// <summary>
        /// 导出为图片 / export to a texture.
        /// </summary>
        /// <param name="cols">列数 / column count</param>
        /// <param name="rows">行数 / row count</param>
        /// <param name="cellSize">每格像素宽度 / cell size in pixels</param>
        /// <param name="getColor">
        /// 返回每格颜色的回调。返回 null 表示该格留空（不覆盖背景色）。
        /// 参数为 (col, row)。
        /// Callback returning each cell's color; null leaves the cell unpainted. Args are (col, row).
        /// </param>
        /// <param name="clearColor">背景色，默认白色 / background color, default white</param>
        /// <returns>导出的纹理 / exported texture</returns>
        public static Texture2D Export(
            int cols,
            int rows,
            int cellSize,
            System.Func<int, int, Color?> getColor,
            Color? clearColor = null)
        {
            int w = Mathf.Max(1, cols * cellSize);
            int h = Mathf.Max(1, rows * cellSize);
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            Color background = clearColor ?? Color.white;
            Color[] pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = background;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    Color? c = getColor?.Invoke(col, row);
                    if (!c.HasValue) continue;

                    for (int dy = 0; dy < cellSize; dy++)
                    {
                        for (int dx = 0; dx < cellSize; dx++)
                        {
                            int px = col * cellSize + dx;
                            int py = row * cellSize + dy;
                            int idx = py * w + px;
                            if (idx >= 0 && idx < pixels.Length)
                                pixels[idx] = c.Value;
                        }
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}
