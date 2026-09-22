using System.Collections.Generic;
using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 冰面的**实时 Mesh** 生成（纯几何：不碰任何组件、不产生副作用，只吐一个 <see cref="Mesh"/>）。
    ///
    /// 形状分四步（距离参数由调用方按 Pixel 单位换算成世界单位后传进来）：
    ///   0. **整体外扩**：方阵轮廓先向外扩 <c>expand</c> —— 在切斜边与顶面内缩**之前**做，
    ///      所以后面那些距离都是在这个更大的轮廓上量的。
    ///   1. **底部多边形**：先求冰组的**外轮廓**（各格并集的边界，环行走法），丢掉孔洞环
    ///      （口径：孔洞**填实**），再把每个交点按类型切 45° 斜边 ——
    ///      **90° 凸角**（2×2 里占 1 格）沿两条边各取 <c>corner90Inset</c> **向内切**；
    ///      **270° 凹角**（2×2 里占 3 格）沿两条边各取 <c>corner270Outset</c> **向外补**。
    ///   2. **侧面棱柱**：底部多边形垂直向上延展 <c>height</c>（整块再抬 <c>baseY</c>）。
    ///   3. **顶面多边形**：轮廓先内缩 <c>topInset</c>，再按顶面自己的两个距离切斜边。
    ///   4. **斜面**：连接「侧面顶部（= 底部多边形轮廓）」与「顶面多边形（再抬高 <c>topYOffset</c>）」。
    ///      底面**不封盖**（朝地看不见，省一半三角）。
    ///
    /// 【几条不能再踩的约定，改之前先看这里】
    ///   · **一切几何都在世界 (x,z) 平面里算**，格坐标 (col,row) 只在「找边界边 + 串环」时用，
    ///     而且换算方向时立刻换到世界方向。原因：格坐标的 row 增大对应世界 **-z**，
    ///     整个平面是**镜像**的 —— 鞋带面积、左右转、凸凹全都会反号，混着两套坐标必然出错（已踩过）。
    ///   · 环行走时**内部恒在左手侧**（左手 = 世界里把方向逆时针转 90°）。
    ///     于是外环的鞋带面积为**正**、孔洞为负 —— 孔洞就是据此丢掉的。
    ///   · 每个角**固定发 2 个点**（切距为 0 时两点重合），所以两层多边形点数一致，
    ///     斜面能逐段一一对应，不会错位。
    ///   · Unity 里 <c>Vector3.Cross(v1-v0, v2-v0)</c> 指向**可见的那一侧**（与 RecalculateNormals 一致）。
    ///     实测：顶面朝上要按「(x,z) 鞋带为负」的顺序缠绕，而我们的外环是正的 ——
    ///     所以**顶面盖面必须把耳切结果反向**缠绕，侧面 / 斜面用 (下i, 上i, 上j) 则天然朝外。
    ///
    /// 斜边距离逐角夹紧到「相邻两边里较短那条的一半」，内缩量夹紧到「最短边的一半以内」；
    /// 越界时自动收缩并写进 <c>warning</c>（不夹紧会切穿到隔壁角或自交）。
    /// </summary>
    public static class IceMeshBuilder
    {
        /// <summary>全部字段都是**世界单位**（调用方已把 Pixel 单位的配置乘过 unitSize）。</summary>
        public struct Settings
        {
            /// <summary>整体外扩：方阵轮廓先向外扩这么多，再做切斜边 / 顶面内缩。0 = 不扩。</summary>
            public float expand;

            /// <summary>整块 Mesh 在 y 方向上的偏移（底面本来在 y=0）。</summary>
            public float baseY;

            /// <summary>底部：90° 凸角（占 1 格）的 45° 内切距离。</summary>
            public float corner90Inset;

            /// <summary>底部：270° 凹角（占 3 格）的 45° 外切距离。</summary>
            public float corner270Outset;

            /// <summary>侧面棱柱高度。</summary>
            public float height;

            /// <summary>顶面相对方阵轮廓的内缩距离。</summary>
            public float topInset;

            /// <summary>顶面：90° 凸角内切距离。</summary>
            public float topCorner90Inset;

            /// <summary>顶面：270° 凹角外切距离。</summary>
            public float topCorner270Outset;

            /// <summary>顶面相对侧面顶部的上方 y 偏移（连接顶面与侧面的斜面高度）。</summary>
            public float topYOffset;
        }

        /// <summary>一条有向格边（a → b）。方向在格坐标里给出，用法前要换成世界方向。</summary>
        private struct Edge
        {
            public Vector2Int a, b;

            public Edge(Vector2Int a, Vector2Int b)
            {
                this.a = a;
                this.b = b;
            }
        }

        /// <summary>
        /// 生成冰面 Mesh。没有任何可画的外环（或全退化）时返回 <c>null</c>，并把原因写进 <paramref name="warning"/>。
        /// </summary>
        /// <param name="cells">冰组成员格（网格坐标 x=col y=row）。越界格会被忽略，与 Sprite 模式一致。</param>
        /// <param name="columns">PixelGroup 的横向格数（交点局部 x 依赖它，必须与 Sprite 模式的算法一致）。</param>
        /// <param name="totalRows">PixelGroup 的总行数（含尾部，用于越界过滤）。</param>
        public static Mesh Build(IEnumerable<Vector2Int> cells, int columns, int totalRows,
            float cellSizeX, float cellSizeZ, Settings s, out string warning)
        {
            warning = null;

            // 越界格一律不算 —— Sprite 模式也是这么过滤的，两边口径要一致
            var set = new HashSet<Vector2Int>();
            if (cells != null)
            {
                foreach (var c in cells)
                {
                    if (c.x < 0 || c.x >= columns || c.y < 0 || c.y >= totalRows)
                        continue;
                    set.Add(c);
                }
            }
            if (set.Count == 0)
            {
                warning = "冰组没有任何落在网格内的格，不生成 Mesh。";
                return null;
            }

            var edges = new List<Edge>();
            CollectBoundaryEdges(set, edges);

            var loops = new List<List<Vector2Int>>();
            WalkLoops(edges, columns, cellSizeX, cellSizeZ, loops);

            // 换成世界 xz 之后再判朝向：外环（鞋带面积为正）留下，孔洞环（负）按「孔洞填实」丢掉
            var outers = new List<List<Vector2>>();
            int holes = 0;
            foreach (var loop in loops)
            {
                var simplified = Simplify(loop);
                if (simplified.Count < 4)
                    continue;

                var world = ToWorld(simplified, columns, cellSizeX, cellSizeZ);
                if (SignedArea(world) > 0f)
                    outers.Add(world);
                else
                    holes++;
            }

            if (outers.Count == 0)
            {
                warning = "没有求出任何外轮廓，不生成 Mesh。";
                return null;
            }

            var verts = new List<Vector3>();
            var tris = new List<int>();
            var notes = new List<string>();
            if (holes > 0)
                notes.Add("丢掉了 " + holes + " 个孔洞环（口径：孔洞填实）");

            foreach (var loop in outers)
            {
                var convex = ClassifyCorners(loop);
                var loopNotes = new List<string>();

                // 0. 整体外扩：必须在切斜边与顶面内缩**之前**做，后面那些距离都在扩完的轮廓上量。
                //    向外扩等价于「与边长 2·expand 的正方形做闵可夫斯基和」（形态学膨胀），
                //    简单多边形膨胀后**必然仍是简单多边形**，所以这里不需要自交保护；
                //    两块只在一点相接时已在上面的环行走里拆成两个独立环，各自膨胀后可能重叠但不会出现乱面。
                var outline = loop;
                if (s.expand > 0f)
                    outline = OffsetLoop(loop, -s.expand);

                var bottomPts = BuildLevel(outline, convex, 0f, s.corner90Inset, s.corner270Outset, loopNotes, "底部");
                var topPts = BuildLevel(outline, convex, s.topInset, s.topCorner90Inset, s.topCorner270Outset, loopNotes, "顶面");

                foreach (var note in loopNotes)
                    if (!notes.Contains(note))
                        notes.Add(note);

                if (bottomPts.Count != topPts.Count || bottomPts.Count < 4)
                {
                    notes.Add("有一圈轮廓算出的两层点数不一致，已跳过这一圈");
                    continue;
                }

                AppendPrism(verts, tris, bottomPts, topPts, s.baseY, s.height, s.topYOffset);
            }

            if (verts.Count == 0)
            {
                warning = "Mesh 没有任何顶点（几何退化）。";
                return null;
            }

            var mesh = new Mesh { name = "IceMesh" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            if (notes.Count > 0)
                warning = string.Join("；", notes.ToArray()) + "。";
            return mesh;
        }

        // ===== 1. 边界与环行走（这一节允许用格坐标，但方向一律先换到世界）=====

        /// <summary>
        /// 各格并集的有向边界边：每格 4 条边里，只保留「邻居不在组里」的那些。
        /// 每条边界边只属于一个格（另一个格不在组里），所以不会重复。
        /// </summary>
        private static void CollectBoundaryEdges(HashSet<Vector2Int> cells, List<Edge> edges)
        {
            foreach (var c in cells)
            {
                if (!cells.Contains(new Vector2Int(c.x - 1, c.y)))
                    edges.Add(new Edge(new Vector2Int(c.x, c.y), new Vector2Int(c.x, c.y + 1)));
                if (!cells.Contains(new Vector2Int(c.x, c.y + 1)))
                    edges.Add(new Edge(new Vector2Int(c.x, c.y + 1), new Vector2Int(c.x + 1, c.y + 1)));
                if (!cells.Contains(new Vector2Int(c.x + 1, c.y)))
                    edges.Add(new Edge(new Vector2Int(c.x + 1, c.y + 1), new Vector2Int(c.x + 1, c.y)));
                if (!cells.Contains(new Vector2Int(c.x, c.y - 1)))
                    edges.Add(new Edge(new Vector2Int(c.x + 1, c.y), new Vector2Int(c.x, c.y)));
            }
        }

        /// <summary>把边界边串成闭环（每个点进出度相等，正常点度为 2）。</summary>
        private static void WalkLoops(List<Edge> edges, int columns, float cellSizeX, float cellSizeZ,
            List<List<Vector2Int>> loops)
        {
            var outgoing = new Dictionary<Vector2Int, List<int>>();
            for (int i = 0; i < edges.Count; i++)
            {
                if (!outgoing.TryGetValue(edges[i].a, out var list))
                {
                    list = new List<int>();
                    outgoing.Add(edges[i].a, list);
                }
                list.Add(i);
            }

            var used = new bool[edges.Count];
            for (int start = 0; start < edges.Count; start++)
            {
                if (used[start])
                    continue;

                var loop = new List<Vector2Int>();
                Vector2Int startPt = edges[start].a;
                int cur = start;
                used[cur] = true;
                loop.Add(edges[cur].a);

                for (int guard = 0; guard <= edges.Count; guard++)
                {
                    Edge e = edges[cur];
                    loop.Add(e.b);
                    if (e.b == startPt)
                        break;

                    int next = PickNext(edges, outgoing, used, cur, cellSizeX, cellSizeZ);
                    if (next < 0)
                        break;
                    used[next] = true;
                    cur = next;
                }

                if (loop.Count > 1 && loop[loop.Count - 1] == loop[0])
                    loop.RemoveAt(loop.Count - 1);   // 去掉闭合时重复的那个点

                if (loop.Count >= 4)
                    loops.Add(loop);
            }
        }

        /// <summary>
        /// 在当前点选下一条尚未用过的出边。正常点只有一条；**对角相接**（2×2 里占对角 2 格）时
        /// 有两条 —— 此时取**最左**的那条转向。这条规则让两块只在一点相接的区域拆成两个独立环，
        /// 而不是自交的「8 字形」，也是 Sprite 模式把两瓣分开的同一个口径。
        /// 「左」在这里是世界 (x,z) 意义下的左，所以方向必须先用 <see cref="WorldDir"/> 换算。
        /// </summary>
        private static int PickNext(List<Edge> edges, Dictionary<Vector2Int, List<int>> outgoing, bool[] used,
            int cur, float cellSizeX, float cellSizeZ)
        {
            Edge e = edges[cur];
            Vector2 dir = WorldDir(e, cellSizeX, cellSizeZ);
            if (!outgoing.TryGetValue(e.b, out var cands))
                return -1;

            Vector2 left = new Vector2(-dir.y, dir.x);   // 世界里的左手 = 逆时针 90°
            int best = -1;
            int bestRank = int.MaxValue;
            for (int i = 0; i < cands.Count; i++)
            {
                int idx = cands[i];
                if (used[idx])
                    continue;

                Vector2 d = WorldDir(edges[idx], cellSizeX, cellSizeZ);
                int rank = d == left ? 0 : (d == dir ? 1 : (d == -left ? 2 : 3));
                if (rank < bestRank)
                {
                    bestRank = rank;
                    best = idx;
                }
            }
            return best;
        }

        /// <summary>
        /// 格边的**世界**方向（单位向量；只有 x/z 两个分量，返回 Vector2 的 x=世界 x、y=世界 z）。
        /// 格坐标 row 增大 = 世界 -z，这一步换算是为了把「镜像」一次性消掉，后面全在世界空间里算。
        /// </summary>
        private static Vector2 WorldDir(Edge e, float cellSizeX, float cellSizeZ)
        {
            Vector2Int d = e.b - e.a;
            float sx = Mathf.Sign(d.x) * cellSizeX;
            float sz = -Mathf.Sign(d.y) * cellSizeZ;
            float len = (float)System.Math.Sqrt(sx * sx + sz * sz);
            return len > 1e-9f ? new Vector2(sx / len, sz / len) : Vector2.zero;
        }

        /// <summary>去掉共线的中间点（它们只是直边上的采样点，不是角）。仿射换算保持共线，所以在格坐标里判即可。</summary>
        private static List<Vector2Int> Simplify(List<Vector2Int> loop)
        {
            var result = new List<Vector2Int>(loop.Count);
            int n = loop.Count;
            for (int i = 0; i < n; i++)
            {
                Vector2Int prev = loop[(i + n - 1) % n];
                Vector2Int cur = loop[i];
                Vector2Int next = loop[(i + 1) % n];
                Vector2Int dIn = cur - prev;
                Vector2Int dOut = next - cur;
                if (System.Math.Sign(dIn.x) == System.Math.Sign(dOut.x) &&
                    System.Math.Sign(dIn.y) == System.Math.Sign(dOut.y))
                    continue;   // 同向 → 直边，不是角
                result.Add(cur);
            }
            return result.Count >= 3 ? result : loop;
        }

        /// <summary>把格坐标的环换算成世界 (x,z) 多边形（顺序不变）。</summary>
        private static List<Vector2> ToWorld(List<Vector2Int> loop, int columns, float cellSizeX, float cellSizeZ)
        {
            var result = new List<Vector2>(loop.Count);
            for (int i = 0; i < loop.Count; i++)
                result.Add(LatticeXZ(loop[i].x, loop[i].y, columns, cellSizeX, cellSizeZ));
            return result;
        }

        // ===== 2. 一层多边形：内缩 + 逐角切 45° 斜边（全部在世界 xz）=====

        /// <summary>逐角判定「凸（90°）/ 凹（270°）」：左转 = 凸角，右转 = 凹角（内部在左手侧的前提下）。</summary>
        private static bool[] ClassifyCorners(List<Vector2> loop)
        {
            int n = loop.Count;
            var convex = new bool[n];
            for (int i = 0; i < n; i++)
            {
                Vector2 eIn = Unit(loop[i] - loop[(i + n - 1) % n]);
                Vector2 eOut = Unit(loop[(i + 1) % n] - loop[i]);
                Vector2 left = new Vector2(-eIn.y, eIn.x);
                convex[i] = eOut == left;
            }
            return convex;
        }

        /// <summary>鞋带面积（世界 xz；外环为正、孔洞为负）。</summary>
        private static float SignedArea(List<Vector2> loop)
        {
            float sum = 0f;
            int n = loop.Count;
            for (int i = 0; i < n; i++)
            {
                Vector2 a = loop[i];
                Vector2 b = loop[(i + 1) % n];
                sum += a.x * b.y - b.x * a.y;
            }
            return sum * 0.5f;
        }

        /// <summary>
        /// 把一环多边形整体内缩（<paramref name="amount"/> &gt; 0）或外扩（&lt; 0）：
        /// 每个交点取相邻两条偏移边线的交点 —— 直角情形即 <c>v + amount·(nIn + nOut)</c>，
        /// nIn / nOut 是两条边的左手法线（指向内部）。凸角向内、凹角向外都不用特判，公式自己成立。
        /// </summary>
        private static List<Vector2> OffsetLoop(List<Vector2> loop, float amount)
        {
            int n = loop.Count;
            var result = new List<Vector2>(n);
            for (int i = 0; i < n; i++)
            {
                Vector2 prev = loop[(i + n - 1) % n];
                Vector2 cur = loop[i];
                Vector2 next = loop[(i + 1) % n];
                Vector2 nIn = Left(Unit(cur - prev));
                Vector2 nOut = Left(Unit(next - cur));
                result.Add(cur + amount * (nIn + nOut));
            }
            return result;
        }

        /// <summary>
        /// 把一环加工成一层多边形（世界 xz）：
        ///   ① 每个交点按内缩量做一次 <see cref="OffsetLoop"/>（直角处即沿两个方向各缩 inset）；
        ///   ② 再按**内缩后**的边长夹紧斜边距离，逐角发 2 个点：
        ///      入边回退 d 的点、出边前进 d 的点 —— 等距取点在直角处必然是 45° 斜边。
        /// 内缩若把某个角的凸凹翻转了（说明这一环被内缩到自交），本层退回「不内缩」。
        /// </summary>
        private static List<Vector2> BuildLevel(List<Vector2> loop, bool[] convex, float inset,
            float d90, float d270, List<string> notes, string label)
        {
            int n = loop.Count;

            // ① 内缩
            float maxInset = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                float len = (loop[(i + 1) % n] - loop[i]).magnitude;
                if (len * 0.5f < maxInset)
                    maxInset = len * 0.5f;
            }
            float useInset = Mathf.Max(0f, inset);
            if (useInset >= maxInset && maxInset > 0f)
            {
                useInset = maxInset * 0.9f;
                notes.Add(label + "内缩距离过大，已收窄到最短边的一半以内");
            }

            var pts = OffsetLoop(loop, useInset);
            if (useInset > 0f && !SameTurns(pts, convex))
            {
                pts = new List<Vector2>(loop);   // 缩到自交了，索性不缩
                notes.Add(label + "内缩会让轮廓自交，本层已按不内缩处理");
            }

            // ② 切斜边（按内缩后的边长夹紧）
            var result = new List<Vector2>(n * 2);
            for (int i = 0; i < n; i++)
            {
                Vector2 prev = pts[(i + n - 1) % n];
                Vector2 cur = pts[i];
                Vector2 next = pts[(i + 1) % n];
                Vector2 eIn = Unit(cur - prev);
                Vector2 eOut = Unit(next - cur);

                float d = convex[i] ? d90 : d270;
                float maxD = 0.5f * Mathf.Min((cur - prev).magnitude, (next - cur).magnitude);
                if (d > maxD)
                {
                    d = maxD;
                    notes.Add(label + "斜边距离超过相邻边的一半，已夹紧");
                }
                if (d < 0f)
                    d = 0f;

                result.Add(cur - eIn * d);
                result.Add(cur + eOut * d);
            }
            return result;
        }

        /// <summary>内缩 / 外扩后的逐角凸凹是否与原来一致（不一致说明缩过头或扩过头、轮廓自交了）。</summary>
        private static bool SameTurns(List<Vector2> after, bool[] convex)
        {
            int n = after.Count;
            for (int i = 0; i < n; i++)
            {
                Vector2 eIn = Unit(after[i] - after[(i + n - 1) % n]);
                Vector2 eOut = Unit(after[(i + 1) % n] - after[i]);
                float cross = eIn.x * eOut.y - eIn.y * eOut.x;   // >0 左转 = 凸
                if ((cross > 0f) != convex[i])
                    return false;
            }
            return true;
        }

        // ===== 3. 侧面 / 斜面 / 顶面 =====

        /// <summary>
        /// 拼一圈的几何：侧面（底部多边形从 baseY 拉到 baseY+height）、
        /// 斜面（再拉到顶面多边形）、顶面盖面（耳切三角化）。缠绕方向见类注释。
        /// </summary>
        private static void AppendPrism(List<Vector3> verts, List<int> tris,
            List<Vector2> bottomPts, List<Vector2> topPts, float baseY, float height, float topYOffset)
        {
            int n = bottomPts.Count;
            float yMid = baseY + height;
            float yTop = yMid + topYOffset;

            for (int i = 0; i < n; i++)
            {
                Vector2 b0 = bottomPts[i];
                Vector2 b1 = bottomPts[(i + 1) % n];
                Vector2 t0 = topPts[i];
                Vector2 t1 = topPts[(i + 1) % n];

                Vector3 lo0 = new Vector3(b0.x, baseY, b0.y);
                Vector3 lo1 = new Vector3(b1.x, baseY, b1.y);
                Vector3 mid0 = new Vector3(b0.x, yMid, b0.y);
                Vector3 mid1 = new Vector3(b1.x, yMid, b1.y);
                Vector3 hi0 = new Vector3(t0.x, yTop, t0.y);
                Vector3 hi1 = new Vector3(t1.x, yTop, t1.y);

                AddQuad(verts, tris, lo0, mid0, mid1, lo1);   // 侧面：朝外
                AddQuad(verts, tris, mid0, hi0, hi1, mid1);   // 斜面：朝外偏上
            }

            // 顶面盖面：耳切结果**反向**缠绕（见类注释：我们的外环顺序给的是朝下的法线）。
            // 顶点基址必须在**所有四边形都加完之后**取 —— 它是盖面顶点自己的起点。
            int baseIndex = verts.Count;
            var poly = new List<Vector2>(n);
            for (int i = 0; i < n; i++)
            {
                poly.Add(topPts[i]);
                verts.Add(new Vector3(topPts[i].x, yTop, topPts[i].y));
            }

            var earTris = new List<int>();
            Triangulate(poly, earTris);
            for (int i = 0; i + 2 < earTris.Count; i += 3)
            {
                tris.Add(baseIndex + earTris[i]);
                tris.Add(baseIndex + earTris[i + 2]);
                tris.Add(baseIndex + earTris[i + 1]);
            }
        }

        /// <summary>加一个四边形（a → b → c → d；按原顺序拆成两个三角，缠绕由调用方保证）。</summary>
        private static void AddQuad(List<Vector3> verts, List<int> tris, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int i = verts.Count;
            verts.Add(a);
            verts.Add(b);
            verts.Add(c);
            verts.Add(d);
            tris.Add(i);
            tris.Add(i + 1);
            tris.Add(i + 2);
            tris.Add(i);
            tris.Add(i + 2);
            tris.Add(i + 3);
        }

        /// <summary>
        /// 简单多边形的耳切三角化（顶点按 <paramref name="poly"/> 顺序，输出的是**局部下标**三元组）。
        /// 自交 / 退化到切不动时提前收敛，剩下的部分丢弃。
        /// </summary>
        private static void Triangulate(List<Vector2> poly, List<int> outTris)
        {
            int n = poly.Count;
            if (n < 3)
                return;

            float orient = PolySignedArea(poly) >= 0f ? 1f : -1f;

            var idx = new List<int>(n);
            for (int i = 0; i < n; i++)
                idx.Add(i);

            int guard = n * n + 16;
            while (idx.Count > 3 && guard-- > 0)
            {
                bool clipped = false;
                for (int k = 0; k < idx.Count; k++)
                {
                    int i0 = idx[(k + idx.Count - 1) % idx.Count];
                    int i1 = idx[k];
                    int i2 = idx[(k + 1) % idx.Count];

                    Vector2 a = poly[i0];
                    Vector2 b = poly[i1];
                    Vector2 c = poly[i2];
                    float cross = (b.x - a.x) * (c.y - b.y) - (b.y - a.y) * (c.x - b.x);
                    if (cross * orient <= 0f)
                        continue;   // 凹角不是耳朵

                    bool contains = false;
                    for (int m = 0; m < idx.Count && !contains; m++)
                    {
                        int im = idx[m];
                        if (im == i0 || im == i1 || im == i2)
                            continue;
                        contains = InTriangle(poly[im], a, b, c);
                    }
                    if (contains)
                        continue;

                    outTris.Add(i0);
                    outTris.Add(i1);
                    outTris.Add(i2);
                    idx.RemoveAt(k);
                    clipped = true;
                    break;
                }
                if (!clipped)
                    break;
            }

            if (idx.Count == 3)
            {
                outTris.Add(idx[0]);
                outTris.Add(idx[1]);
                outTris.Add(idx[2]);
            }
        }

        private static bool InTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Cross2(b - a, p - a);
            float d2 = Cross2(c - b, p - b);
            float d3 = Cross2(a - c, p - c);
            bool neg = (d1 < 0f) || (d2 < 0f) || (d3 < 0f);
            bool pos = (d1 > 0f) || (d2 > 0f) || (d3 > 0f);
            return !(neg && pos);   // 三个符号一致（含落在边上）→ 在内部
        }

        private static float PolySignedArea(List<Vector2> poly)
        {
            float sum = 0f;
            int n = poly.Count;
            for (int i = 0; i < n; i++)
            {
                Vector2 a = poly[i];
                Vector2 b = poly[(i + 1) % n];
                sum += a.x * b.y - b.x * a.y;
            }
            return sum * 0.5f;
        }

        // ===== 小工具 =====

        /// <summary>
        /// 交点（格角）的局部 xz —— 与 <c>IceItem.CornerLocalPosition</c> 逐字一致，
        /// 保证 Mesh 与四角拼接 Sprite 两种模式落在同一个位置。
        /// </summary>
        public static Vector2 LatticeXZ(int col, int row, int columns, float cellSizeX, float cellSizeZ)
        {
            float x = (col - columns * 0.5f) * cellSizeX;
            float z = -(row - 0.5f) * cellSizeZ;
            return new Vector2(x, z);
        }

        /// <summary>左手方向：在世界 (x,z) 平面里把方向逆时针转 90°（= 该边指向内部的那一侧）。</summary>
        private static Vector2 Left(Vector2 d)
        {
            return new Vector2(-d.y, d.x);
        }

        private static float Cross2(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }

        private static Vector2 Unit(Vector2 v)
        {
            float len = v.magnitude;
            return len > 1e-6f ? v / len : Vector2.zero;
        }
    }
}
