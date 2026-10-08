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
    /// </summary>
    public static class SameColorMergeWatcher
    {
        private static readonly int[] Dx = { 1, -1, 0, 0 };
        private static readonly int[] Dz = { 0, 0, 1, -1 };

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

            // 步骤 1：只留「真正落在网格里」的新像素（管道波次可能在动画期间就被匹配移出）
            var newSet = new HashSet<PixelItem>();
            for (int i = 0; i < newPixels.Count; i++)
            {
                var p = newPixels[i];
                if (p == null || !group.IsInRange(p.gridX, p.gridZ))
                    continue;
                if (group.grid[p.gridX, p.gridZ] != p)
                    continue;
                newSet.Add(p);
            }
            if (newSet.Count == 0)
                return;

            // 步骤 2：网格上「颜色可见且已就位」的像素全集
            var visible = new List<PixelItem>();
            var visibleIndex = new Dictionary<PixelItem, int>();
            int cols = group.columns;
            int rows = group.TotalRows;
            for (int c = 0; c < cols; c++)
            {
                for (int r = 0; r < rows; r++)
                {
                    var p = group.grid[c, r];
                    if (p == null || p.placing)
                        continue;   // 未就位：不参与（见类注释）
                    if (p.isQuestion && !p.revealed)
                        continue;   // 问号未揭晓：颜色不可见
                    if (group.IsFrozenCell(c, r) || group.IsCrateCell(c, r))
                        continue;   // 冰 / 木箱盖着：颜色不可见
                    if (group.IsBlocked(c, r))
                        continue;   // 障碍格（兜底，正常不该有像素）
                    if (visibleIndex.ContainsKey(p))
                        continue;   // 同一实例出现在两格（异常兜底）：只算先扫到的那格
                    visibleIndex[p] = visible.Count;
                    visible.Add(p);
                }
            }
            if (visible.Count == 0)
                return;

            // 步骤 3：两套连通块 —— 事件后（把新像素算进来）/ 事件前（把新像素当隔绝物）
            var labelAll = new int[visible.Count];
            int labelCount = LabelComponents(group, visible, visibleIndex, null, labelAll);

            var labelOld = new int[visible.Count];
            int oldLabelCount = LabelComponents(group, visible, visibleIndex, newSet, labelOld);

            // 步骤 4：按块统计（新像素数 / 旧像素数 / 各自的成员）
            var newCountOfLabel = new int[labelCount + 1];
            var oldCountOfLabel = new int[labelCount + 1];
            var newMembersOfLabel = new List<PixelItem>[labelCount + 1];

            for (int i = 0; i < visible.Count; i++)
            {
                int li = labelAll[i];
                if (li == 0)
                    continue;
                if (newSet.Contains(visible[i]))
                {
                    newCountOfLabel[li]++;
                    (newMembersOfLabel[li] ??= new List<PixelItem>()).Add(visible[i]);
                }
                else
                {
                    oldCountOfLabel[li]++;
                }
            }

            var newLabelOfOld = new int[oldLabelCount + 1];
            var oldMembers = new List<PixelItem>[oldLabelCount + 1];

            for (int i = 0; i < visible.Count; i++)
            {
                int oi = labelOld[i];
                if (oi == 0)
                    continue;
                newLabelOfOld[oi] = labelAll[i];
                (oldMembers[oi] ??= new List<PixelItem>()).Add(visible[i]);
            }

            // 4a. 每个「被并进来的原有区域」各随机 1 颗
            //     （原本就一整片、只是被新像素接上时就是 1 块；原本分成好几块被焊成一片时就是好几块）
            for (int o = 1; o <= oldLabelCount; o++)
            {
                int li = newLabelOfOld[o];
                if (li == 0 || newCountOfLabel[li] == 0)
                    continue;   // 这块没和新像素连上：没有「连成更大范围的组」
                if (oldMembers[o] == null)
                    continue;
                emoji.TryPlaySurpriseEmoji(oldMembers[o]);
            }

            // 4b. 每个「合并组」的新像素里随机 1 颗
            //     （两个条件一起 = 这个组确实由「新像素 + 原有区域」组成，也就是「连成更大范围的组」）
            for (int li = 1; li <= labelCount; li++)
            {
                if (newCountOfLabel[li] == 0 || oldCountOfLabel[li] == 0)
                    continue;
                if (newMembersOfLabel[li] == null)
                    continue;
                emoji.TryPlaySurpriseEmoji(newMembersOfLabel[li]);
            }
        }

        /// <summary>
        /// 给 <paramref name="visible"/> 里的像素打同色 4 连通块标号：1..N，0 = 未访问 / 被排除。
        /// <paramref name="excluded"/> 非空时，其中的像素既不做种子、也不能被扩散穿过。
        /// 返回块数。
        /// </summary>
        private static int LabelComponents(PixelGroup group, List<PixelItem> visible,
            Dictionary<PixelItem, int> visibleIndex, HashSet<PixelItem> excluded, int[] label)
        {
            for (int i = 0; i < label.Length; i++)
                label[i] = 0;

            int next = 0;
            var queue = new Queue<int>();

            for (int i = 0; i < visible.Count; i++)
            {
                if (label[i] != 0)
                    continue;
                var seed = visible[i];
                if (excluded != null && excluded.Contains(seed))
                    continue;

                next++;
                label[i] = next;
                queue.Clear();
                queue.Enqueue(i);
                int color = seed.colorId;

                while (queue.Count > 0)
                {
                    var cur = visible[queue.Dequeue()];
                    int cx = cur.gridX;
                    int cz = cur.gridZ;

                    for (int d = 0; d < 4; d++)
                    {
                        int nx = cx + Dx[d];
                        int nz = cz + Dz[d];
                        if (!group.IsInRange(nx, nz))
                            continue;

                        var nb = group.grid[nx, nz];
                        if (nb == null || nb.colorId != color)
                            continue;
                        if (excluded != null && excluded.Contains(nb))
                            continue;
                        if (!visibleIndex.TryGetValue(nb, out int ni))
                            continue;   // 不在可见集合里（未就位 / 颜色不可见）
                        if (label[ni] != 0)
                            continue;

                        label[ni] = next;
                        queue.Enqueue(ni);
                    }
                }
            }

            return next;
        }
    }
}
