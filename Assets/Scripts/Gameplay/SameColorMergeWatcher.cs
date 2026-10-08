using System.Collections.Generic;
using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 「同色连成一片」判定器。一次**动态事件**把一批像素放进网格（管道推波 / 箱子放货 / 升降台升起）
    /// 或把一批像素的颜色显出来（问号揭晓 / 冰化开 / 木箱被拆）之后：
    /// 若这批新像素与网格上**原本就已显色**的同色像素连成了同一个 4 连通块，
    /// 就在**每个被并进来的原有区域**各随机 1 颗、在**本批新像素**里随机 1 颗，各播一个惊讶表情。
    ///
    /// 口径详见 Docs/EmojiSurpriseMergeDesign.md。三条要点：
    ///   · **原有区域** = 「把本批新像素整体当隔绝物」得到的连通块（等价于事件前的分组）—— 无需事件前快照；
    ///   · **未就位**（<see cref="PixelItem.placing"/>）的像素不参与连通：同一次点击里别的生产者
    ///     还没落地，不能被当成本次的「原有区域」；
    ///   · **颜色可见**才算：未揭晓问号 / 冰冻 / 被木箱盖住 / 不在网格的像素一律排除
    ///     （它们的颜色玩家看不见，「连成一片」也就无从谈起）。
    ///
    /// 判定不掷概率（必出）；抑制是**逐像素**的 —— 同一颗头上惊讶还没播完就跳过（见
    /// <see cref="EmojiManager.TryPlaySurpriseEmoji"/>）。录制模式整体不播。
    ///
    /// **实现约定（改之前务必读）**：
    ///   · 下面的缓冲**全部是静态复用的**，所以 <see cref="Notify"/> **不可重入**。目前成立 ——
    ///     它唯一调出去的是 <c>EmojiManager.TryPlaySurpriseEmoji</c> → <c>PlayEmoji</c>，后者只做
    ///     对象池生成 + 世界缩放/朝向 + 一条延时回收，不回 PixelGroup、不重入本类。**若将来让表情
    ///     播放回调进判定链路，这里必须先改成非静态或加可重入保护。**
    ///   · 连通块标号改为**按格索引**（标签表就是网格形状：0 = 可见未标号，-1 = 不可见，&gt;0 = 标号），
    ///     取代原来「`Dictionary&lt;PixelItem,int&gt;` 把像素映射到 `visible` 下标 + `int[]` 标签」。
    ///     于是内层逐邻格判定全是数组读、没有哈希查找。**「同一实例出现在两格」那条兜底**改用
    ///     一颗复用的 <see cref="SeenOnce"/> 完成，保留「只算先扫到的那格」的原口径。
    ///   · 步骤 2b 是一个**早退**：没有任何「新像素 ↔ 同色可见旧像素」的 4 邻对时直接返回
    ///     （证明见那里的注释）—— 它挡掉的正是「新区块根本没挨着任何原有同色区域」这类白跑。
    /// </summary>
    public static class SameColorMergeWatcher
    {
        private static readonly int[] Dx = { 1, -1, 0, 0 };
        private static readonly int[] Dz = { 0, 0, 1, -1 };

        // ===== 复用缓冲（见类注释的不可重入约定）=====

        /// <summary>本次调用里「颜色可见且已就位」的格，按扫描顺序。</summary>
        private static readonly List<Vector2Int> VisibleCells = new List<Vector2Int>();

        /// <summary>「同一实例出现在两格」的去重（只算先扫到的那格）。</summary>
        private static readonly HashSet<PixelItem> SeenOnce = new HashSet<PixelItem>();

        /// <summary>BFS 队列；两遍连通块共用，每次进块前 Clear。</summary>
        private static readonly Queue<Vector2Int> BfsQueue = new Queue<Vector2Int>();

        /// <summary>本批通过校验的新像素（真正落在网格里的那些）。</summary>
        private static readonly List<PixelItem> ValidNew = new List<PixelItem>();

        /// <summary>事件后的同色 4 连通块标号（含本批新像素）。</summary>
        private static int[,] LabelAllGrid;
        /// <summary>事件前的同色 4 连通块标号（把本批新像素当隔绝物）。</summary>
        private static int[,] LabelOldGrid;
        /// <summary>本批新像素的格掩码（取代原来的 <c>HashSet&lt;PixelItem&gt;</c> 排除集合）。</summary>
        private static bool[,] NewMask;

        private static int[] NewCountOfLabel = System.Array.Empty<int>();
        private static int[] OldCountOfLabel = System.Array.Empty<int>();
        private static int[] NewLabelOfOld = System.Array.Empty<int>();
        private static List<PixelItem>[] NewMembersOfLabel = System.Array.Empty<List<PixelItem>>();
        private static List<PixelItem>[] OldMembers = System.Array.Empty<List<PixelItem>>();

        /// <summary>「每个标签一个成员列表」的池：同一次调用里按需租用，下次进来复用（不缩容）。</summary>
        private static readonly List<List<PixelItem>> MemberListPool = new List<List<PixelItem>>();
        private static int MemberListPoolUsed;

        /// <summary>
        /// 事件入口：把「本批新产生 / 新揭示的像素」交进来判定并播放。
        /// 同一批里可以混多种颜色 —— 连通块本身按颜色分开，各色独立出表情。
        /// </summary>
        public static void Notify(PixelGroup group, IList<PixelItem> newPixels)
        {
            if (group == null || group.grid == null || newPixels == null || newPixels.Count == 0)
                return;
            if (!Application.isPlaying)
                return;   // 编辑模式不播（编辑器工具也会调 RefreshExposed；此时对象池没初始化，表情也无意义）
            if (group.suppressMergeSurprise)
                return;   // 关卡加载期间抑制（见 PixelGroup.suppressMergeSurprise）

            var gc = GameController.Instance;
            if (gc != null && gc.recordMode)
                return;   // 录制模式不播：像素原地销毁，机制链路不完整

            var emoji = EmojiManager.Instance;
            if (emoji == null)
                return;   // 未配表情管理器 / 已销毁：静默跳过

            int cols = group.columns;
            int rows = group.TotalRows;
            MemberListPoolUsed = 0;

            // 步骤 1：只留「真正落在网格里」的新像素（管道波次可能在动画期间就被匹配移出）
            ValidNew.Clear();
            for (int i = 0; i < newPixels.Count; i++)
            {
                var p = newPixels[i];
                if (p == null || !group.IsInRange(p.gridX, p.gridZ))
                    continue;
                if (group.grid[p.gridX, p.gridZ] != p)
                    continue;
                ValidNew.Add(p);
            }
            if (ValidNew.Count == 0)
                return;

            // 步骤 2：一次全网格扫描 —— 重置三张表 / 收集可见格 / 标出本批新像素的格
            var labelAll = EnsureIntGrid(ref LabelAllGrid, cols, rows);
            var labelOld = EnsureIntGrid(ref LabelOldGrid, cols, rows);
            var newMask = EnsureBoolGrid(ref NewMask, cols, rows);
            VisibleCells.Clear();
            SeenOnce.Clear();
            for (int c = 0; c < cols; c++)
            {
                for (int r = 0; r < rows; r++)
                {
                    labelAll[c, r] = -1;   // -1 = 不可见；扫到可见格时改 0
                    labelOld[c, r] = -1;
                    newMask[c, r] = false;

                    var p = group.grid[c, r];
                    if (p == null || p.placing)
                        continue;   // 未就位：不参与（见类注释）
                    if (p.isQuestion && !p.revealed)
                        continue;   // 问号未揭晓：颜色不可见
                    if (group.IsFrozenCell(c, r) || group.IsCrateCell(c, r))
                        continue;   // 冰 / 木箱盖着：颜色不可见
                    if (group.IsBlocked(c, r))
                        continue;   // 障碍格（兜底，正常不该有像素）
                    if (!SeenOnce.Add(p))
                        continue;   // 同一实例出现在两格（异常兜底）：只算先扫到的那格

                    labelAll[c, r] = 0;
                    labelOld[c, r] = 0;
                    VisibleCells.Add(new Vector2Int(c, r));
                }
            }
            for (int i = 0; i < ValidNew.Count; i++)
            {
                var p = ValidNew[i];
                newMask[p.gridX, p.gridZ] = true;
            }
            if (VisibleCells.Count == 0)
                return;

            // 步骤 2b：早退 —— 一个「新像素 ↔ 同色、可见、且非本批新像素」的 4 邻对都没有时，
            //   必然不会出任何表情。理由：此时任一连通块要么全由新像素组成、要么全由旧像素组成
            //   （两种像素之间没有边），于是 4a 要求的「旧块与新像素同块」与 4b 要求的
            //   「块内新、旧像素都有」都凑不齐。反过来，只要有这样一对边，连通性判定的两条路才可能成立。
            if (!AnyMergePair(group, labelAll, newMask))
                return;

            // 步骤 3：两套连通块 —— 事件后（把新像素算进来）/ 事件前（把新像素当隔绝物）
            int labelCount = LabelComponents(group, labelAll, newMask, excludeNew: false);
            int oldLabelCount = LabelComponents(group, labelOld, newMask, excludeNew: true);

            // 步骤 4：按块统计（新像素数 / 旧像素数 / 各自的成员）
            int slots = Mathf.Max(labelCount, oldLabelCount) + 1;
            EnsureLabelBuffers(slots);
            for (int i = 0; i < slots; i++)
            {
                NewCountOfLabel[i] = 0;
                OldCountOfLabel[i] = 0;
                NewLabelOfOld[i] = 0;
                NewMembersOfLabel[i] = null;
                OldMembers[i] = null;
            }

            for (int i = 0; i < VisibleCells.Count; i++)
            {
                var cell = VisibleCells[i];
                int li = labelAll[cell.x, cell.y];
                if (li <= 0)
                    continue;
                if (newMask[cell.x, cell.y])
                {
                    NewCountOfLabel[li]++;
                    (NewMembersOfLabel[li] ??= RentMemberList()).Add(group.grid[cell.x, cell.y]);
                }
                else
                {
                    OldCountOfLabel[li]++;
                }
            }

            for (int i = 0; i < VisibleCells.Count; i++)
            {
                var cell = VisibleCells[i];
                int oi = labelOld[cell.x, cell.y];
                if (oi <= 0)
                    continue;   // 0 = 本批新像素（第二遍被排除）；-1 不会出现（VisibleCells 里都是可见格）
                NewLabelOfOld[oi] = labelAll[cell.x, cell.y];
                (OldMembers[oi] ??= RentMemberList()).Add(group.grid[cell.x, cell.y]);
            }

            // 4a. 每个「被并进来的原有区域」各随机 1 颗
            //     （原本就一整片、只是被新像素接上时就是 1 块；原本分成好几块被焊成一片时就是好几块）
            for (int o = 1; o <= oldLabelCount; o++)
            {
                int li = NewLabelOfOld[o];
                if (li == 0 || NewCountOfLabel[li] == 0)
                    continue;   // 这块没和新像素连上：没有「连成更大范围的组」
                if (OldMembers[o] == null)
                    continue;
                emoji.TryPlaySurpriseEmoji(OldMembers[o]);
            }

            // 4b. 每个「合并组」的新像素里随机 1 颗
            //     （两个条件一起 = 这个组确实由「新像素 + 原有区域」组成，也就是「连成更大范围的组」）
            for (int li = 1; li <= labelCount; li++)
            {
                if (NewCountOfLabel[li] == 0 || OldCountOfLabel[li] == 0)
                    continue;
                if (NewMembersOfLabel[li] == null)
                    continue;
                emoji.TryPlaySurpriseEmoji(NewMembersOfLabel[li]);
            }
        }

        /// <summary>
        /// 本批新像素里，有没有任何一颗与「同色、颜色可见、且**不是**本批新像素」的格 4 相邻？
        /// 没有则必然不出表情（证明见调用处的步骤 2b）。
        /// 调用时标签表还没有标号，只有 0（可见未标号）与 -1（不可见）两种值。
        /// </summary>
        private static bool AnyMergePair(PixelGroup group, int[,] labelAll, bool[,] newMask)
        {
            for (int i = 0; i < ValidNew.Count; i++)
            {
                var p = ValidNew[i];
                if (labelAll[p.gridX, p.gridZ] != 0)
                    continue;   // 这颗新像素自己就不可见（未就位 / 被冰盖 / …）：它不计进任何块的计数
                int color = p.colorId;
                for (int d = 0; d < 4; d++)
                {
                    int nx = p.gridX + Dx[d];
                    int nz = p.gridZ + Dz[d];
                    if (!group.IsInRange(nx, nz))
                        continue;
                    if (labelAll[nx, nz] != 0)
                        continue;   // 不可见
                    if (newMask[nx, nz])
                        continue;   // 也是本批新像素：不算「原有区域」
                    var nb = group.grid[nx, nz];
                    if (nb != null && nb.colorId == color)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 给「可见格」（<see cref="VisibleCells"/>）打同色 4 连通块标号：0 → 1..N，写进
        /// <paramref name="labelGrid"/>；不可见格保持 -1；被排除的格保持 0。
        /// <paramref name="excludeNew"/> 为 true 时，本批新像素既不做种子、也不能被扩散穿过。
        /// 邻格判定全是数组读（标签表 + 新像素掩码），没有哈希查找。
        /// </summary>
        private static int LabelComponents(PixelGroup group, int[,] labelGrid, bool[,] newMask, bool excludeNew)
        {
            int next = 0;
            BfsQueue.Clear();

            for (int i = 0; i < VisibleCells.Count; i++)
            {
                var seed = VisibleCells[i];
                if (labelGrid[seed.x, seed.y] != 0)
                    continue;   // 已标号（>0）；-1 不会出现在 VisibleCells 里
                if (excludeNew && newMask[seed.x, seed.y])
                    continue;

                int color = group.grid[seed.x, seed.y].colorId;
                next++;
                labelGrid[seed.x, seed.y] = next;
                BfsQueue.Clear();
                BfsQueue.Enqueue(seed);

                while (BfsQueue.Count > 0)
                {
                    var cur = BfsQueue.Dequeue();
                    int cx = cur.x;
                    int cz = cur.y;

                    for (int d = 0; d < 4; d++)
                    {
                        int nx = cx + Dx[d];
                        int nz = cz + Dz[d];
                        if (!group.IsInRange(nx, nz))
                            continue;
                        if (labelGrid[nx, nz] != 0)
                            continue;   // 不可见（-1）或已标号（>0）
                        if (excludeNew && newMask[nx, nz])
                            continue;
                        if (group.grid[nx, nz].colorId != color)
                            continue;

                        labelGrid[nx, nz] = next;
                        BfsQueue.Enqueue(new Vector2Int(nx, nz));
                    }
                }
            }

            return next;
        }

        private static int[,] EnsureIntGrid(ref int[,] buf, int cols, int rows)
        {
            if (buf == null || buf.GetLength(0) != cols || buf.GetLength(1) != rows)
                buf = new int[cols, rows];
            return buf;
        }

        private static bool[,] EnsureBoolGrid(ref bool[,] buf, int cols, int rows)
        {
            if (buf == null || buf.GetLength(0) != cols || buf.GetLength(1) != rows)
                buf = new bool[cols, rows];
            return buf;
        }

        /// <summary>把五张「按标签下标」的表扩容到至少 <paramref name="slots"/> 项（只增不减；调用方随后清用到的区间）。</summary>
        private static void EnsureLabelBuffers(int slots)
        {
            if (NewLabelOfOld.Length >= slots)
                return;
            NewCountOfLabel = new int[slots];
            OldCountOfLabel = new int[slots];
            NewLabelOfOld = new int[slots];
            NewMembersOfLabel = new List<PixelItem>[slots];
            OldMembers = new List<PixelItem>[slots];
        }

        /// <summary>租一个空的成员列表（同一次调用里每个标签各一个）。</summary>
        private static List<PixelItem> RentMemberList()
        {
            if (MemberListPoolUsed == MemberListPool.Count)
                MemberListPool.Add(new List<PixelItem>());
            var list = MemberListPool[MemberListPoolUsed++];
            list.Clear();
            return list;
        }
    }
}
