using System.Collections.Generic;
using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 倍乘门倍率图：**只吃关卡数据**（<see cref="LevelData"/>），不依赖 PixelGroup / 场景 / 编辑器。
    ///
    /// 每道门把**门格也当障碍**、从最前排（row 0）四向 BFS 求出闭合区域，区域内的每格乘上该门倍数
    /// （多道门嵌套即连乘），区域外恒为 1。障碍只取「墙 ∪ 管道自身格」—— 箱子会开、木箱会拆，
    /// 不能当永久围栏。
    ///
    /// 归一到这一处，是为了让「运行时统计 / 按 Record 重排容器（<c>ContainerRearranger</c>）/
    /// 快速生成 Record（<c>LevelGridBoard</c>）」三处的倍率口径**永不发散**。
    /// 原来这段在 <c>ContainerRearranger</c> 里（编辑器程序集），运行时程序集引用不到，故移到此处。
    /// </summary>
    public static class GateMultiplierMap
    {
        /// <summary>构建 [column, row] 倍率图；无门时全为 1。</summary>
        public static int[,] Build(LevelData data, int columns, int totalRows,
                                   HashSet<Vector2Int> barrierCells)
        {
            var multiplier = new int[columns, totalRows];
            for (int c = 0; c < columns; c++)
                for (int r = 0; r < totalRows; r++)
                    multiplier[c, r] = 1;

            if (data == null || data.gates == null)
                return multiplier;

            foreach (var gate in data.gates)
            {
                if (gate == null)
                    continue;

                var gateCells = new HashSet<Vector2Int>();
                GateItem.CollectCells(gate.start, gate.end, gateCells);
                if (gateCells.Count == 0)
                    continue;

                var region = GateRegion.ComputeRegion(
                    columns, totalRows,
                    (c, r) => barrierCells != null && barrierCells.Contains(new Vector2Int(c, r)),
                    gateCells);

                int mult = Mathf.Max(1, gate.multiplier);
                foreach (var cell in region)
                {
                    if (cell.x < 0 || cell.x >= columns || cell.y < 0 || cell.y >= totalRows)
                        continue;
                    multiplier[cell.x, cell.y] *= mult;
                }
            }

            return multiplier;
        }
    }
}
