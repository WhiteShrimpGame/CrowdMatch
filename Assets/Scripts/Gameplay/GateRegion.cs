using System.Collections.Generic;
using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 倍乘门的「闭合区域」求解（纯算法，无副作用）。
    ///
    /// 区域定义：把**这道门的门格当作障碍**，从最前排（row 0）出发四向 BFS，能走到的算「外部」；
    /// 界内、非障碍、非门格、又走不到的格 = 该门的**闭合区域内部**。
    /// 区域为空 ⇒ 这道门没围出任何东西 ⇒ 不闭合（数量无法确定，编辑器会弹窗）。
    ///
    /// 只以 row 0 为种子（不是网格四边）：像素只能朝 row 0 出去，这才是「必须穿过这道门」的正确语义；
    /// 以四边为种子会把贴着后墙的格误判成外部。
    ///
    /// 障碍只取 **墙 ∪ 管道自身格**（调用方通过 isBarrier 给出，不含门格）：
    /// 箱子会开箱、会消失，不能当永久围栏；不把箱子算障碍，区域内箱子自身的格才落在区域内，
    /// 箱子释放出来的像素才可能被正确计入倍乘。
    /// </summary>
    public static class GateRegion
    {
        /// <summary>
        /// 求单道门的闭合区域内部格集合。isBarrier 只应返回「墙或管道」；
        /// 门格由 gateCells 单独传入（本函数把它们也当障碍）。
        /// </summary>
        public static HashSet<Vector2Int> ComputeRegion(
            int columns,
            int totalRows,
            System.Func<int, int, bool> isBarrier,
            ICollection<Vector2Int> gateCells)
        {
            var region = new HashSet<Vector2Int>();
            if (columns <= 0 || totalRows <= 0 || gateCells == null || gateCells.Count == 0)
                return region;

            int[] dx = { 1, -1, 0, 0 };
            int[] dz = { 0, 0, 1, -1 };

            // 外部标记：从最前排的非障碍、非门格格出发
            var outside = new bool[columns, totalRows];
            var queue = new Queue<Vector2Int>();
            for (int c = 0; c < columns; c++)
            {
                var seed = new Vector2Int(c, 0);
                if (gateCells.Contains(seed) || isBarrier(c, 0))
                    continue;
                outside[c, 0] = true;
                queue.Enqueue(seed);
            }

            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                for (int d = 0; d < 4; d++)
                {
                    int nx = cur.x + dx[d];
                    int nz = cur.y + dz[d];
                    if (nx < 0 || nx >= columns || nz < 0 || nz >= totalRows)
                        continue;
                    if (outside[nx, nz])
                        continue;

                    var next = new Vector2Int(nx, nz);
                    if (gateCells.Contains(next) || isBarrier(nx, nz))
                        continue;

                    outside[nx, nz] = true;
                    queue.Enqueue(next);
                }
            }

            for (int c = 0; c < columns; c++)
                for (int r = 0; r < totalRows; r++)
                {
                    if (outside[c, r])
                        continue;
                    var cell = new Vector2Int(c, r);
                    if (gateCells.Contains(cell) || isBarrier(c, r))
                        continue;
                    region.Add(cell);
                }

            return region;
        }
    }
}
