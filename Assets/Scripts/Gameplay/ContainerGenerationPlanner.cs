using System.Collections.Generic;
using UnityEngine;

namespace CrowdMatch
{
    /// <summary>单个容器的生成计划项</summary>
    public struct ContainerPlan
    {
        public int colorId;
        public int capacity;

        public ContainerPlan(int colorId, int capacity)
        {
            this.colorId = colorId;
            this.capacity = capacity;
        }
    }

    /// <summary>
    /// 容器生成规划器（纯 C#，无 MonoBehaviour）。
    /// 参考分层颜色池：把像素按（层, 颜色）分层统计，从最浅层起逐次抽同色 pack，
    /// 输出容器铺放计划。层 0 = 最前排（PixelGroup 的 gridZ 0 = Z 最大）。
    /// </summary>
    public class ContainerGenerationPlanner
    {
        private List<int[]> _tally;   // [layer][color]，layer 0 = 最浅/最前排
        private int _colorCount;
        private int _layerCount;
        private int _maxSpanLayers;

        public int ColorCount => _colorCount;
        public int LayerCount => _layerCount;

        /// <summary>池中是否还有像素。</summary>
        public bool HasPixels
        {
            get
            {
                if (_tally == null) return false;
                foreach (var layer in _tally)
                    foreach (int count in layer)
                        if (count > 0) return true;
                return false;
            }
        }

        /// <summary>
        /// 重建：扫描调用方提供的 (层, 颜色) 列表，按层和颜色统计。
        /// </summary>
        /// <param name="pixels">所有像素的 (层, 颜色) 列表。层 0 = 最前排。</param>
        /// <param name="layerCount">总层数（PixelGroup.TotalRows，含尾部）。</param>
        /// <param name="colorCount">颜色总数（ColorConfig.Count）。</param>
        /// <param name="maxSpanLayers">抽取 pack 时最多跨多少层（0 = 仅最浅层）。</param>
        public void Rebuild(
            IEnumerable<(int layer, int color)> pixels,
            int layerCount,
            int colorCount,
            int maxSpanLayers)
        {
            _colorCount = Mathf.Max(0, colorCount);
            _layerCount = Mathf.Max(0, layerCount);
            _maxSpanLayers = Mathf.Max(0, maxSpanLayers);
            _tally = new List<int[]>(_layerCount);

            for (int l = 0; l < _layerCount; l++)
                _tally.Add(new int[_colorCount]);

            if (pixels == null) return;

            foreach (var (layer, color) in pixels)
            {
                if (color < 0 || color >= _colorCount) continue;
                int l = Mathf.Clamp(layer, 0, _layerCount - 1);
                _tally[l][color]++;
            }

            RemoveEmptyLayers();
        }

        /// <summary>
        /// 从池中抽一个同色 pack。总是从最浅层（layer 0）选数量最大的颜色（大色块优先），
        /// 跨 [0, maxSpanLayers] 层累加同色总数，容量落在 [minCap, maxCap]（或不可少的小尾块）。
        /// </summary>
        /// <returns>(颜色, 容量)。无像素时返回 (-1, 0)。</returns>
        public (int color, int capacity) PullPack(int minCap, int maxCap)
        {
            if (_tally == null || _tally.Count == 0) return (-1, 0);

            minCap = Mathf.Max(1, minCap);
            maxCap = Mathf.Max(minCap, maxCap);

            // 1) 最浅层（layer 0）内数量最大的颜色（大色块优先）
            int color = PickLargestColor(0);
            if (color < 0) return (-1, 0);

            // 2) 跨 [0, span] 层累加该颜色总数
            int spanEnd = Mathf.Min(_maxSpanLayers, _tally.Count - 1);
            int total = 0;
            for (int l = 0; l <= spanEnd; l++)
                total += _tally[l][color];
            if (total <= 0) return (-1, 0);

            // 3) 计算容量
            int cap = ResolvePack(total, minCap, maxCap);

            // 4) 从浅到深扣减
            int remaining = cap;
            for (int l = 0; l <= spanEnd && remaining > 0; l++)
            {
                int take = Mathf.Min(_tally[l][color], remaining);
                _tally[l][color] -= take;
                remaining -= take;
            }

            // 5) 层塌缩
            RemoveEmptyLayers();

            return (color, cap);
        }

        /// <summary>取指定层中数量最大的颜色索引；无则返回 -1。</summary>
        private int PickLargestColor(int layerIdx)
        {
            if (layerIdx < 0 || layerIdx >= _tally.Count) return -1;
            int[] layer = _tally[layerIdx];
            int best = -1;
            int bestCount = 0;
            for (int c = 0; c < layer.Length; c++)
            {
                if (layer[c] > bestCount)
                {
                    bestCount = layer[c];
                    best = c;
                }
            }
            return best;
        }

        /// <summary>把总数 total 均分成若干 [minCap, maxCap] 块，返回其中一块的容量。</summary>
        private int ResolvePack(int total, int minCap, int maxCap)
        {
            if (total <= maxCap)
                return total; // 全取；若 < minCap 属不可避免的小尾块

            int n = Mathf.CeilToInt((float)total / maxCap);
            int maxN = Mathf.Max(1, total / minCap);

            if (n > maxN)
            {
                // 区间为空：无法把 total 均分成每块都落在 [minCap, maxCap] 的块
                //（典型：minCap == maxCap 且 total 不是 maxCap 的倍数，如 4/3）。
                // 此时宁可填满 maxCap、留一个小尾块，也绝不生成超过 maxCap 的容器。
                return maxCap;
            }

            int baseCap = total / n;
            int rem = total % n;
            return baseCap + (rem > 0 ? 1 : 0); // rem 块取 baseCap+1，其余 baseCap
        }

        /// <summary>移除所有颜色计数均为 0 的层，使 layer 0 恒为最浅层。</summary>
        private void RemoveEmptyLayers()
        {
            for (int i = _tally.Count - 1; i >= 0; i--)
            {
                bool allZero = true;
                foreach (int count in _tally[i])
                {
                    if (count > 0) { allZero = false; break; }
                }
                if (allZero)
                    _tally.RemoveAt(i);
            }
        }
    }
}
