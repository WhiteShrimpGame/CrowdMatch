using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// GridColorMatcher — 最近色匹配工具 / nearest-color matching utility.
    ///
    /// Pure static helper used by SquareGridColorTool. Maps a sampled pixel color to
    /// the closest entry in a target palette using a weighted RGB distance that
    /// approximates human perceptual sensitivity (green weighted highest, blue lowest).
    ///
    /// 纯静态工具，供方阵导入工具使用。用加权 RGB 距离把采样像素颜色匹配到目标
    /// 调色板中最接近的颜色（绿色权重最高、蓝色最低，近似人眼敏感度）。
    ///
    /// Zero dependencies beyond UnityEngine.Color — runtime-safe and unit-testable.
    /// 仅依赖 UnityEngine.Color——运行时安全、可单测。
    /// </summary>
    public static class GridColorMatcher
    {
        /// <summary>
        /// 在调色板中查找与目标颜色最接近的索引。
        /// 权重 R:G:B = 2:4:3，模拟人眼对绿色更敏感、对蓝色较不敏感的感知特性。
        ///
        /// Find the index of the palette color closest to <paramref name="target"/>.
        /// Weights R:G:B = 2:4:3, approximating human perceptual sensitivity.
        /// </summary>
        /// <param name="target">采样得到的像素颜色 / sampled pixel color</param>
        /// <param name="palette">目标调色板（纯色数组）/ target palette (pure color array)</param>
        /// <returns>最接近的调色板索引；调色板为空时返回 -1。/ closest palette index, or -1 if palette empty.</returns>
        public static int FindClosestColorIndex(Color target, Color[] palette)
        {
            if (palette == null || palette.Length == 0) return -1;

            int bestIdx = 0;
            float bestDist = float.MaxValue;

            for (int i = 0; i < palette.Length; i++)
            {
                float dr = target.r - palette[i].r;
                float dg = target.g - palette[i].g;
                float db = target.b - palette[i].b;
                // 加权欧氏距离（平方，省去开根）/ weighted squared distance
                float dist = dr * dr * 2f + dg * dg * 4f + db * db * 3f;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestIdx = i;
                }
            }

            return bestIdx;
        }

        /// <summary>
        /// 判断像素是否为纯白（#FFFFFF）。
        /// 纯白是导入流程的哨兵色：美术在框架图中未填色的格保持纯白，导入时跳过，
        /// 保留该格当前颜色不变。
        ///
        /// Whether the pixel is pure white (#FFFFFF). Pure white is the import sentinel:
        /// cells the artist left unpainted are skipped, preserving their current color.
        /// </summary>
        public static bool IsPureWhite(Color c)
        {
            return Mathf.Approximately(c.r, 1f)
                && Mathf.Approximately(c.g, 1f)
                && Mathf.Approximately(c.b, 1f);
        }
    }
}
