using System.Collections.Generic;
using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 冰组的「区域」求解（纯算法，无副作用）。
    ///
    /// 两种创建方式共用这里的工具：
    ///   · 包围线：把点选的多个 Pixel 依次连成一条**首尾相连的折线**（自动补一段闭合），
    ///     再求折线**内部**（含包围线自身占的格）—— 见 <see cref="CollectLoopCells"/> +
    ///     <see cref="ComputeInteriorCells"/>。
    ///   · 多选：点选的格本身就是冰组成员，只需校验**四向连通** —— 见 <see cref="IsConnected"/>。
    ///
    /// 内部求解与 <c>WallItemEditor.ComputeInteriorCells</c> 完全一致：从网格四边（非包围线格）
    /// 四向 BFS 漫过非包围线格，能到达的都算「外部」；包围盒内「非外部、非包围线格」= 内部。
    /// </summary>
    public static class IceRegion
    {
        /// <summary>把网格坐标（浮点）换算为整格格坐标（就近取整）。</summary>
        public static Vector2Int ToCell(Vector2 p)
        {
            return new Vector2Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y));
        }

        /// <summary>
        /// 枚举一段经过的所有网格格（含两端与中间格）。
        /// 起步 / 步长写法与 <c>WallItem.EnumerateSegment</c>、<c>GateItem.CollectCells</c> 一致，
        /// 非轴对齐的斜段也能走出阶梯形状。
        /// </summary>
        public static void CollectSegment(Vector2 a, Vector2 b, HashSet<Vector2Int> set)
        {
            if (set == null)
                return;

            Vector2Int ca = ToCell(a);
            Vector2Int cb = ToCell(b);

            int steps = Mathf.Max(Mathf.Abs(cb.x - ca.x), Mathf.Abs(cb.y - ca.y));
            int dx = System.Math.Sign(cb.x - ca.x);
            int dy = System.Math.Sign(cb.y - ca.y);

            set.Add(ca);
            for (int i = 1; i <= steps; i++)
                set.Add(new Vector2Int(ca.x + dx * i, ca.y + dy * i));
        }

        /// <summary>
        /// 把一串端点依次连成折线并枚举占格；**首尾之间自动补一段闭合**（同
        /// <c>WallItemEditor.CollectLoopCells</c>），这样「包围线」天然是闭环，不需要用户手动补最后一点。
        /// </summary>
        public static void CollectLoopCells(IList<Vector2> points, HashSet<Vector2Int> set)
        {
            if (points == null || points.Count == 0 || set == null)
                return;

            var closed = new List<Vector2>(points);
            if (closed.Count >= 2)
                closed.Add(closed[0]);

            for (int i = 0; i + 1 < closed.Count; i++)
                CollectSegment(closed[i], closed[i + 1], set);
        }

        /// <summary>
        /// 求闭环**内部**的网格格集合（不含包围线自身格）。算法与
        /// <c>WallItemEditor.ComputeInteriorCells</c> 逐字一致：从网格四边非包围线格 BFS 标出「外部」，
        /// 包围盒内「非外部、非包围线格」= 内部。
        /// </summary>
        public static HashSet<Vector2Int> ComputeInteriorCells(PixelGroup group, HashSet<Vector2Int> loopCells)
        {
            var interior = new HashSet<Vector2Int>();
            if (group == null || loopCells == null || loopCells.Count == 0)
                return interior;

            int cols = group.columns;
            int rows = group.TotalRows;
            if (cols <= 0 || rows <= 0)
                return interior;

            int minX = int.MaxValue, maxX = int.MinValue, minZ = int.MaxValue, maxZ = int.MinValue;
            foreach (var c in loopCells)
            {
                if (c.x < minX) minX = c.x;
                if (c.x > maxX) maxX = c.x;
                if (c.y < minZ) minZ = c.y;
                if (c.y > maxZ) maxZ = c.y;
            }

            int[] dx = { 1, -1, 0, 0 };
            int[] dz = { 0, 0, 1, -1 };

            // 从网格边界（非包围线格）BFS，能到达的都视为「外部」
            var outside = new bool[cols, rows];
            var q = new Queue<Vector2Int>();
            for (int c = 0; c < cols; c++)
                for (int r = 0; r < rows; r++)
                {
                    if (c != 0 && c != cols - 1 && r != 0 && r != rows - 1)
                        continue;
                    var cell = new Vector2Int(c, r);
                    if (loopCells.Contains(cell) || outside[c, r])
                        continue;
                    outside[c, r] = true;
                    q.Enqueue(cell);
                }

            while (q.Count > 0)
            {
                var cur = q.Dequeue();
                for (int d = 0; d < 4; d++)
                {
                    int nx = cur.x + dx[d];
                    int nz = cur.y + dz[d];
                    if (nx < 0 || nx >= cols || nz < 0 || nz >= rows)
                        continue;
                    if (outside[nx, nz])
                        continue;
                    var ncell = new Vector2Int(nx, nz);
                    if (loopCells.Contains(ncell))
                        continue;
                    outside[nx, nz] = true;
                    q.Enqueue(ncell);
                }
            }

            for (int x = minX; x <= maxX; x++)
                for (int z = minZ; z <= maxZ; z++)
                {
                    if (x < 0 || x >= cols || z < 0 || z >= rows)
                        continue;
                    var cell = new Vector2Int(x, z);
                    if (loopCells.Contains(cell))
                        continue;
                    if (!outside[x, z])
                        interior.Add(cell);
                }

            return interior;
        }

        /// <summary>
        /// 校验一组格是否**四向连通**（同游戏里暴露 / 同色连通 / 可寻路的连通口径）。
        /// 返回 false 时 <paramref name="error"/> 给出不相连的格数与一个例子，供编辑器弹窗。
        /// </summary>
        public static bool IsConnected(IList<Vector2Int> cells, out string error)
        {
            error = null;
            if (cells == null || cells.Count == 0)
            {
                error = "冰组没有任何格。";
                return false;
            }

            var set = new HashSet<Vector2Int>(cells);
            if (set.Count == 1)
                return true;

            int[] dx = { 1, -1, 0, 0 };
            int[] dz = { 0, 0, 1, -1 };

            var visited = new HashSet<Vector2Int>();
            var q = new Queue<Vector2Int>();
            var first = cells[0];
            visited.Add(first);
            q.Enqueue(first);

            while (q.Count > 0)
            {
                var cur = q.Dequeue();
                for (int d = 0; d < 4; d++)
                {
                    var n = new Vector2Int(cur.x + dx[d], cur.y + dz[d]);
                    if (!set.Contains(n) || visited.Contains(n))
                        continue;
                    visited.Add(n);
                    q.Enqueue(n);
                }
            }

            if (visited.Count == set.Count)
                return true;

            var bad = new List<Vector2Int>();
            foreach (var c in set)
                if (!visited.Contains(c))
                    bad.Add(c);

            error = "选中的 " + set.Count + " 格不是四向连通的：有 " + bad.Count +
                    " 格与主连通块不相连（例如 (" + bad[0].x + "," + bad[0].y + ")）。";
            return false;
        }
    }
}
