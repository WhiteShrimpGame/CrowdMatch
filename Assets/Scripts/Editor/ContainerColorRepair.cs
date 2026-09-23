using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace CrowdMatch
{
    /// <summary>
    /// 「检查并修复容器颜色对齐」：把 ContainerGroup 里**容量 = 3 的车**的颜色，对齐到 PixelGroup 的像素分布。
    ///
    /// 像素侧口径**完全复用** <see cref="PixelGroup.CollectPlanningPixels"/>（网格像素 + 管道计划像素 +
    /// 箱子隐藏像素 + 升降台分组像素，每颗按其**所在格**的倍乘门倍率重复发出）——与「生成 Containers /
    /// 统计颜色总数」是同一份底座，所以倍乘门 / 管道 / 箱子 / 升降台都不会被漏算，三处口径也不会发散。
    ///
    /// 检查与修复分五步（口径已逐条对齐）：
    ///   1. 每种颜色的像素数必须能被 3 整除（**逐色**判，不以总数代替）；不满足则点名并**中止**，后面都不跑。
    ///   2. 记下 <c>capacity != 3</c> 的车。它们**视为已配好**：容量从该色的像素数里扣掉，既不参与对比、
    ///      也不参与修复（见第 3 步的负数与余数检查）。
    ///   3. 扣完之后若某色剩余像素 &lt; 0（这类车吃掉的容量超过该色像素数）或不再是 3 的倍数，**中止**点名——
    ///      这两种情形用容量 3 的车无法精确对齐，硬修只会再造出容量≠3 的车。
    ///   4. 逐色比较「容量 = 3 的车数」与「剩余像素数 / 3」，记下未对齐的颜色与差额（以车数计，多 = 正）。
    ///   5. 修复：先按总数把车数对齐（多则从尾部删「多出的颜色」的车，少则从尾部向后补「缺少的颜色」的车），
    ///      再从尾部把「多出的颜色」的车改色成「缺少的颜色」。
    ///
    /// 「尾部」= **行大优先、同行列大优先**（后排 → 前排，同一排从右到左）。补车的落位是**先按尾部顺序填空格，
    /// 空格用尽再在末尾加行**（新行同样从大列往小列填）。
    ///
    /// 修复只动容量 = 3 的车：不改像素、不碰容量≠3 的车、不改 columns；只有补车不够放时才加 rows。全程走 Undo。
    /// </summary>
    public static class ContainerColorRepair
    {
        private const string Tag = "[ContainerColorRepair]";

        /// <summary>本工具认定的「标准容量」。所有对齐比较与修复都以它为单位。</summary>
        private const int NormalCapacity = 3;

        /// <summary>弹窗里每个列表最多列几条，其余写进 Console（避免弹窗被几十行撑爆）。</summary>
        private const int MaxDialogLines = 12;

        /// <summary>一辆车的最小记录。修复期间车可能被销毁，故留 <see cref="item"/> 引用以便操作。</summary>
        public class CarRef
        {
            public int col;
            public int row;
            public int colorId;
            public int capacity;
            public ContainerItem item;

            /// <summary>给人看的坐标（1 起算）。</summary>
            public string Where()
            {
                return "第" + (col + 1) + "列第" + (row + 1) + "排";
            }
        }

        /// <summary>一个颜色的未对齐记录。<see cref="remainPixels"/> 已扣掉容量≠3 的车占的容量。</summary>
        public class Mismatch
        {
            public int colorId;
            public int remainPixels;
            public int carCount;
            public int neededCars;

            /// <summary>车数差：多 = 正、少 = 负。</summary>
            public int CarDiff => carCount - neededCars;
        }

        public class Report
        {
            public bool aborted;
            public string abortReason;

            /// <summary>第 1 步：像素数不能被 3 整除的颜色（颜色, 像素数）。</summary>
            public readonly List<(int color, int count)> badPixels = new List<(int, int)>();

            /// <summary>第 3 步：容量≠3 的车吃掉的容量超过该色像素数（颜色, 那些车的容量和, 该色像素数）。</summary>
            public readonly List<(int color, int oddCapacity, int pixels)> overConsumed = new List<(int, int, int)>();

            /// <summary>第 3 步：扣完之后剩余像素不是 3 的倍数（颜色, 剩余数）。</summary>
            public readonly List<(int color, int remain)> notDivisible = new List<(int, int)>();

            /// <summary>全部车（尾部顺序）。</summary>
            public readonly List<CarRef> cars = new List<CarRef>();

            /// <summary>capacity != 3 的车（尾部顺序），不参与修复。</summary>
            public readonly List<CarRef> oddCars = new List<CarRef>();

            /// <summary>capacity == 3 的车（尾部顺序），修复只动它们。</summary>
            public readonly List<CarRef> normalCars = new List<CarRef>();

            /// <summary>第 4 步：未对齐的颜色。</summary>
            public readonly List<Mismatch> mismatches = new List<Mismatch>();

            /// <summary>各色「待配像素数」（= 像素数 − 容量≠3 的车的容量），修复的目标值就在这份表里。</summary>
            public readonly Dictionary<int, int> remainByColor = new Dictionary<int, int>();

            public int totalPixels;
            public int normalCarCount;
            public int neededCars;
        }

        /// <summary>工具入口：检查 → 弹窗 → （确认后）修复。全程只在编辑模式下可用（修复走 Undo）。</summary>
        public static void Run(ContainerGroup group)
        {
            if (group == null)
                return;

            if (Application.isPlaying)
            {
                EditorUtility.DisplayDialog("检查并修复容器颜色",
                    "请在编辑模式（非 Play）下使用：修复依赖 Undo 记录。", "确定");
                return;
            }

            var pixelGroup = group.pixelGroup != null
                ? group.pixelGroup
                : Object.FindObjectOfType<PixelGroup>();
            if (pixelGroup == null)
            {
                Debug.LogError(Tag + " 找不到 PixelGroup（字段未指定且场景中无 PixelGroup）。");
                EditorUtility.DisplayDialog("检查并修复容器颜色",
                    "找不到 PixelGroup，请在 ContainerGroup 的 pixelGroup 字段中指定。", "确定");
                return;
            }

            pixelGroup.RebuildGrid();   // 倍乘门倍率与掩码必须先是最新的，统计才含机制影响
            group.RebuildGrid();

            var config = ColorConfigLocator.Find();
            var report = Inspect(pixelGroup, group);

            if (report.aborted)
            {
                Debug.LogWarning(Tag + " 检查未通过，" + report.abortReason + "：\n" + BuildReportLog(report, config));
                EditorUtility.DisplayDialog("检查并修复容器颜色",
                    BuildAbortText(report, config), "确定");
                return;
            }

            if (report.mismatches.Count == 0)
            {
                Debug.Log(Tag + " 颜色已对齐，无需修复。\n" + BuildReportLog(report, config));
                EditorUtility.DisplayDialog("检查并修复容器颜色",
                    BuildNoFixText(report, config), "确定");
                return;
            }

            bool go = EditorUtility.DisplayDialog("检查并修复容器颜色",
                BuildConfirmText(report, config), "修复", "取消");
            if (!go)
                return;

            if (report.normalCarCount < report.neededCars && group.containerPrefab == null)
            {
                EditorUtility.DisplayDialog("检查并修复容器颜色",
                    "本次修复需要新增 " + (report.neededCars - report.normalCarCount) +
                    " 辆车，但 containerPrefab 为空，无法实例化。", "确定");
                return;
            }

            string summary = Apply(group, report, config);

            group.RebuildGrid();
            EditorUtility.SetDirty(group);

            Debug.Log(Tag + " 修复完成。以下是**修复前**的检查结果，随后是本次改动：\n" +
                      BuildReportLog(report, config) + "\n" + summary);

            EditorUtility.DisplayDialog("检查并修复容器颜色", summary, "确定");
        }

        // ===== 检查 =====

        /// <summary>
        /// 跑完第 1~4 步。<paramref name="pixelGroup"/> 需已 RebuildGrid（倍率图才是最新的），
        /// <paramref name="group"/> 的 grid 为 null 时本方法内部补一次 RebuildGrid。
        /// </summary>
        public static Report Inspect(PixelGroup pixelGroup, ContainerGroup group)
        {
            var r = new Report();

            // ---- 第 1 步：逐色像素数必须能被 3 整除 ----
            var pixels = pixelGroup.CollectPlanningPixels();
            r.totalPixels = pixels.Count;

            var pixelCounts = new Dictionary<int, int>();
            for (int i = 0; i < pixels.Count; i++)
            {
                int color = pixels[i].color;
                pixelCounts.TryGetValue(color, out int n);
                pixelCounts[color] = n + 1;
            }

            foreach (var kv in pixelCounts)
                if (kv.Value % NormalCapacity != 0)
                    r.badPixels.Add((kv.Key, kv.Value));
            r.badPixels.Sort((a, b) => a.color.CompareTo(b.color));

            if (r.badPixels.Count > 0)
            {
                r.aborted = true;
                r.abortReason = "有颜色的像素数不能被 " + NormalCapacity + " 整除";
                return r;
            }

            // ---- 第 2 步：登记全部车，分出 capacity != 3 的那些 ----
            if (group.grid == null)
                group.RebuildGrid();

            for (int row = 0; row < group.rows; row++)
                for (int col = 0; col < group.columns; col++)
                {
                    var item = group.GetItem(col, row);
                    if (item == null)
                        continue;
                    r.cars.Add(new CarRef
                    {
                        col = col,
                        row = row,
                        colorId = item.colorId,
                        capacity = item.capacity,
                        item = item,
                    });
                }

            r.cars.Sort(TailCompare);
            foreach (var car in r.cars)
            {
                if (car.capacity == NormalCapacity)
                    r.normalCars.Add(car);
                else
                    r.oddCars.Add(car);
            }
            r.normalCarCount = r.normalCars.Count;

            // ---- 第 3 步：把这些车占的容量从各自颜色的像素数里扣掉 ----
            var oddCapacityByColor = new Dictionary<int, int>();
            foreach (var car in r.oddCars)
            {
                int cap = Mathf.Max(0, car.capacity);
                oddCapacityByColor.TryGetValue(car.colorId, out int cur);
                oddCapacityByColor[car.colorId] = cur + cap;
            }

            foreach (var kv in pixelCounts)
                r.remainByColor[kv.Key] = kv.Value;
            foreach (var kv in oddCapacityByColor)
            {
                r.remainByColor.TryGetValue(kv.Key, out int cur);
                r.remainByColor[kv.Key] = cur - kv.Value;
            }

            foreach (var kv in r.remainByColor)
            {
                if (kv.Value < 0)
                {
                    oddCapacityByColor.TryGetValue(kv.Key, out int oddCap);
                    pixelCounts.TryGetValue(kv.Key, out int pc);
                    r.overConsumed.Add((kv.Key, oddCap, pc));
                }
                else if (kv.Value % NormalCapacity != 0)
                {
                    r.notDivisible.Add((kv.Key, kv.Value));
                }
            }
            r.overConsumed.Sort((a, b) => a.color.CompareTo(b.color));
            r.notDivisible.Sort((a, b) => a.color.CompareTo(b.color));

            if (r.overConsumed.Count > 0 || r.notDivisible.Count > 0)
            {
                r.aborted = true;
                r.abortReason = "扣掉容量≠" + NormalCapacity + " 的车之后，剩余像素无法用车对齐";
                return r;
            }

            // ---- 第 4 步：逐色对比「容量=3 的车数」与「剩余像素 / 3」 ----
            var carCountByColor = new Dictionary<int, int>();
            foreach (var car in r.normalCars)
            {
                carCountByColor.TryGetValue(car.colorId, out int n);
                carCountByColor[car.colorId] = n + 1;
            }

            int neededSum = 0;
            foreach (var kv in r.remainByColor)
                neededSum += kv.Value / NormalCapacity;
            r.neededCars = neededSum;

            var colors = new SortedSet<int>(r.remainByColor.Keys);
            colors.UnionWith(carCountByColor.Keys);
            foreach (int color in colors)
            {
                r.remainByColor.TryGetValue(color, out int remain);
                carCountByColor.TryGetValue(color, out int carCount);
                if (remain == 0 && carCount == 0)
                    continue;
                if (carCount * NormalCapacity == remain)
                    continue;
                r.mismatches.Add(new Mismatch
                {
                    colorId = color,
                    remainPixels = remain,
                    carCount = carCount,
                    neededCars = remain / NormalCapacity,
                });
            }

            return r;
        }

        /// <summary>尾部顺序：行大优先，同行列大优先（后排 → 前排，同一排从右到左）。</summary>
        private static int TailCompare(CarRef a, CarRef b)
        {
            if (a.row != b.row)
                return b.row.CompareTo(a.row);
            return b.col.CompareTo(a.col);
        }

        // ===== 修复 =====

        /// <summary>执行修复，返回给弹窗的摘要文本。前提：<see cref="Inspect"/> 未中止。</summary>
        public static string Apply(ContainerGroup group, Report r, ColorConfig config)
        {
            var counts = new Dictionary<int, int>();
            foreach (var car in r.normalCars)
            {
                counts.TryGetValue(car.colorId, out int n);
                counts[car.colorId] = n + 1;
            }

            var needed = new Dictionary<int, int>();
            foreach (var kv in r.remainByColor)
                needed[kv.Key] = kv.Value / NormalCapacity;

            int removedCount = 0;
            int addedCount = 0;
            int recoloredCount = 0;

            // 5a-1) 车多了：从尾部依次找「多出的颜色」的车删掉，直到数量一致
            int extra = r.normalCarCount - r.neededCars;
            if (extra > 0)
            {
                for (int i = 0; i < r.normalCars.Count && removedCount < extra; i++)
                {
                    var car = r.normalCars[i];
                    if (car.item == null)
                        continue;
                    if (CountOf(counts, car.colorId) <= NeedOf(needed, car.colorId))
                        continue;
                    Undo.DestroyObjectImmediate(car.item.gameObject);
                    counts[car.colorId] = CountOf(counts, car.colorId) - 1;
                    removedCount++;
                }
            }
            // 5a-2) 车少了：从尾部向后用「缺少的颜色」补到数量一致
            else if (extra < 0)
            {
                addedCount = AddCars(group, r, needed, counts, config, -extra);
            }

            // 5b) 颜色对齐：从尾部抓「多出的颜色」的车，改成「缺少的颜色」。
            //     5a 之后 Σ(车数) == Σ(需要的车数)，所以只要还有缺色就一定还有多色，循环必然收敛。
            var recolored = new StringBuilder();
            while (true)
            {
                int deficit = PickDeficitColor(needed, counts);
                if (deficit < 0)
                    break;   // 没有缺色了 = 全部对齐

                CarRef pick = null;
                for (int i = 0; i < r.normalCars.Count; i++)
                {
                    var car = r.normalCars[i];
                    if (car.item == null)
                        continue;   // 已被 5a 删掉
                    if (CountOf(counts, car.item.colorId) > NeedOf(needed, car.item.colorId))
                    {
                        pick = car;
                        break;      // 尾部顺序遍历 → 取的就是最靠尾的那辆
                    }
                }
                if (pick == null)
                    break;          // 理论不可达：有缺色却无多色

                int from = pick.item.colorId;
                Undo.RecordObject(pick.item, "修复容器颜色");
                pick.item.colorId = deficit;
                pick.item.ApplyMaterial(config);
                EditorUtility.SetDirty(pick.item);

                counts[from] = CountOf(counts, from) - 1;
                counts[deficit] = CountOf(counts, deficit) + 1;
                pick.colorId = deficit;
                recoloredCount++;

                if (recoloredCount <= MaxDialogLines)
                    recolored.AppendLine("  · " + pick.Where() + "：" + from + " → " + deficit);
            }

            var sb = new StringBuilder();
            sb.AppendLine("修复完成：删车 " + removedCount + " 辆，增车 " + addedCount + " 辆，改色 " + recoloredCount + " 辆。");
            if (recoloredCount > 0)
            {
                sb.AppendLine();
                sb.AppendLine("改色明细（尾部 → 前排）：");
                sb.Append(recolored);
                if (recoloredCount > MaxDialogLines)
                    sb.AppendLine("  …（还有 " + (recoloredCount - MaxDialogLines) + " 辆，详见 Console）");
            }

            int ropeTouched = 0;
            foreach (var car in r.normalCars)
                if (car.item != null && car.item.ropeGroupId != 0)
                    ropeTouched++;
            if (ropeTouched > 0)
            {
                sb.AppendLine();
                sb.AppendLine("注意：修复后仍有 " + ropeTouched + " 辆车带 ropeGroupId（绳组关系按颜色/列位置定义，");
                sb.AppendLine("本次改色与删车可能已让绳组不再对应原来的颜色搭配，建议重新过一遍绳组）。");
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// 补 <paramref name="count"/> 辆车：颜色取「缺少」中 colorId 最小者；位置按尾部顺序**先填空格**，
        /// 空格用尽再在末尾加行（新行同样从大列往小列填）。返回实际创建数。
        ///
        /// 注意：加行只改 <see cref="ContainerGroup.rows"/> 字段，grid 数组要等 Apply 结束后
        /// <see cref="ContainerGroup.RebuildGrid"/> 才重建 —— 所以这里算完空格后**不再调用 GetItem**，
        /// 否则会在未扩容的数组上越界。
        /// </summary>
        private static int AddCars(
            ContainerGroup group, Report r,
            Dictionary<int, int> needed, Dictionary<int, int> counts,
            ColorConfig config, int count)
        {
            var free = new List<Vector2Int>();
            for (int row = group.rows - 1; row >= 0; row--)
                for (int col = group.columns - 1; col >= 0; col--)
                    if (group.GetItem(col, row) == null)
                        free.Add(new Vector2Int(col, row));

            int freeIdx = 0;
            int newRow = group.rows;            // 待新增的行号
            int newCol = group.columns - 1;     // 新行内下一个待填的列
            int created = 0;

            for (int i = 0; i < count; i++)
            {
                int color = PickDeficitColor(needed, counts);
                if (color < 0)
                    break;

                Vector2Int cell;
                if (freeIdx < free.Count)
                {
                    cell = free[freeIdx++];
                }
                else
                {
                    if (newCol < 0)
                    {
                        newRow++;
                        newCol = group.columns - 1;
                    }
                    if (newRow >= group.rows)
                    {
                        Undo.RecordObject(group, "修复容器颜色");
                        group.rows = newRow + 1;   // 行数不够放：末尾加行（列数不动）
                    }
                    cell = new Vector2Int(newCol, newRow);
                    newCol--;
                }

                var go = ContainerGroupEditor.InstantiateTemplate(group.containerPrefab, group.transform);
                if (go == null)
                {
                    Debug.LogError(Tag + " 实例化 containerPrefab 失败，补车中止（已创建 " + created + " 辆）。");
                    break;
                }

                go.name = "Container_" + cell.x + "_" + cell.y;
                go.transform.localPosition = group.GetLocalPosition(cell.x, cell.y);

                var item = go.GetComponent<ContainerItem>();
                if (item == null)
                    item = go.AddComponent<ContainerItem>();
                item.gridX = cell.x;
                item.gridZ = cell.y;
                item.colorId = color;
                item.isQuestion = false;
                item.ropeGroupId = 0;
                item.SetCapacity(NormalCapacity);
                item.ApplyMaterial(config);
                item.RefreshQuestionObject();
                if (cell.y == 0)
                    item.HideLid();
                EditorUtility.SetDirty(item);
                Undo.RegisterCreatedObjectUndo(go, "修复容器颜色");

                counts[color] = CountOf(counts, color) + 1;
                created++;
            }

            return created;
        }

        /// <summary>挑一个「还缺车」的颜色：colorId 升序第一个缺的；都不缺返回 -1。</summary>
        private static int PickDeficitColor(Dictionary<int, int> needed, Dictionary<int, int> counts)
        {
            int best = -1;
            foreach (var kv in needed)
            {
                if (kv.Value <= 0 || CountOf(counts, kv.Key) >= kv.Value)
                    continue;
                if (best < 0 || kv.Key < best)
                    best = kv.Key;
            }
            return best;
        }

        private static int CountOf(Dictionary<int, int> map, int key)
        {
            return map.TryGetValue(key, out int v) ? v : 0;
        }

        private static int NeedOf(Dictionary<int, int> map, int key)
        {
            return map.TryGetValue(key, out int v) ? v : 0;
        }

        // ===== 文本 =====

        private static string BuildAbortText(Report r, ColorConfig config)
        {
            var sb = new StringBuilder();
            sb.AppendLine("检查未通过，已中止（未做任何修改）：");
            sb.AppendLine();

            if (r.badPixels.Count > 0)
            {
                sb.AppendLine("【1】像素数不能被 " + NormalCapacity + " 整除的颜色 " + r.badPixels.Count + " 种：");
                AppendLimited(sb, r.badPixels, (item, s) =>
                    s.AppendLine("  · 颜色 " + item.color + Label(item.color, config) + "：" +
                                 item.count + " 颗，余 " + (item.count % NormalCapacity)));
                sb.AppendLine();
            }

            if (r.overConsumed.Count > 0)
            {
                sb.AppendLine("【3】容量≠" + NormalCapacity + " 的车吃掉的容量超过了该色像素数 " + r.overConsumed.Count + " 种：");
                AppendLimited(sb, r.overConsumed, (item, s) =>
                    s.AppendLine("  · 颜色 " + item.color + Label(item.color, config) + "：这类车共占 " +
                                 item.oddCapacity + " 容量，但该色只有 " + item.pixels + " 颗像素（差 " +
                                 (item.pixels - item.oddCapacity) + "）"));
                sb.AppendLine();
            }

            if (r.notDivisible.Count > 0)
            {
                sb.AppendLine("【3】扣掉这类车后，剩余像素不是 " + NormalCapacity + " 的倍数 " + r.notDivisible.Count + " 种：");
                AppendLimited(sb, r.notDivisible, (item, s) =>
                    s.AppendLine("  · 颜色 " + item.color + Label(item.color, config) + "：剩余 " +
                                 item.remain + " 颗，余 " + (item.remain % NormalCapacity)));
                sb.AppendLine();
            }

            sb.AppendLine("这两种情况都无法用容量 " + NormalCapacity + " 的车精确对齐，硬修只会再造出容量≠" +
                          NormalCapacity + " 的车。");
            sb.AppendLine("请先调整这些颜色的像素分布（含管道计划 / 箱子隐藏 / 升降台分组 / 倍乘门倍率）或这些车的容量，再回来修复。");

            return sb.ToString().TrimEnd();
        }

        private static string BuildNoFixText(Report r, ColorConfig config)
        {
            var sb = new StringBuilder();
            sb.AppendLine("检查通过：容量 = " + NormalCapacity + " 的车与像素分布已对齐，无需修复。");
            sb.AppendLine("（像素 " + r.totalPixels + " 颗 → 需要 " + r.neededCars + " 辆车，现有 " + r.normalCarCount + " 辆）");

            if (r.oddCars.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("另有 " + r.oddCars.Count + " 辆容量≠" + NormalCapacity + " 的车（已视为配好，未参与检查）：");
                AppendLimited(sb, r.oddCars, (car, s) =>
                    s.AppendLine("  · " + car.Where() + "：颜色 " + car.colorId + Label(car.colorId, config) +
                                 " / 容量 " + car.capacity));
            }

            return sb.ToString().TrimEnd();
        }

        private static string BuildConfirmText(Report r, ColorConfig config)
        {
            var sb = new StringBuilder();

            if (r.oddCars.Count > 0)
            {
                sb.AppendLine("【2】容量≠" + NormalCapacity + " 的车 " + r.oddCars.Count + " 辆" +
                              "（视为已配好：容量已从像素数里扣掉，本次不改色也不删）：");
                AppendLimited(sb, r.oddCars, (car, s) =>
                    s.AppendLine("  · " + car.Where() + "：颜色 " + car.colorId + Label(car.colorId, config) +
                                 " / 容量 " + car.capacity));
            }
            else
            {
                sb.AppendLine("【2】容量≠" + NormalCapacity + " 的车：0 辆。");
            }

            sb.AppendLine();
            sb.AppendLine("【3】未对齐的颜色 " + r.mismatches.Count + " 种" +
                          "（容量 = " + NormalCapacity + " 的车共 " + r.normalCarCount +
                          " 辆，需要 " + r.neededCars + " 辆）：");
            AppendLimited(sb, r.mismatches, (m, s) => s.AppendLine(
                "  · 颜色 " + m.colorId + Label(m.colorId, config) +
                "：剩余像素 " + m.remainPixels + " 颗 → 需 " + m.neededCars + " 辆，现有 " + m.carCount +
                " 辆（" + (m.CarDiff > 0 ? "多 " : "少 ") + Mathf.Abs(m.CarDiff) + " 辆）"));

            sb.AppendLine();
            sb.AppendLine("是否修复？");
            sb.AppendLine("修复只动容量 = " + NormalCapacity + " 的车：从尾部（后排 → 前排、同行从右到左）");
            sb.AppendLine("先删/补使车数一致，再把「多出的颜色」改色成「缺少的颜色」。");
            sb.AppendLine("像素与容量≠" + NormalCapacity + " 的车都不动；补车不够放时会在末尾加行。全程可 Undo。");

            return sb.ToString().TrimEnd();
        }

        /// <summary>把整份检查结果写进 Console（不截断）。</summary>
        private static string BuildReportLog(Report r, ColorConfig config)
        {
            var sb = new StringBuilder();
            sb.AppendLine("  像素总数 " + r.totalPixels + " → 需要 " + r.neededCars +
                          " 辆容量 " + NormalCapacity + " 的车；现有这类车 " + r.normalCarCount +
                          " 辆，容量≠" + NormalCapacity + " 的车 " + r.oddCars.Count + " 辆。");

            foreach (var b in r.badPixels)
                sb.AppendLine("  [1] 颜色 " + b.color + Label(b.color, config) + "：" + b.count +
                              " 颗，余 " + (b.count % NormalCapacity));
            foreach (var o in r.overConsumed)
                sb.AppendLine("  [3] 颜色 " + o.color + Label(o.color, config) + "：这类车占 " + o.oddCapacity +
                              " 容量 > 该色 " + o.pixels + " 颗像素");
            foreach (var n in r.notDivisible)
                sb.AppendLine("  [3] 颜色 " + n.color + Label(n.color, config) + "：扣完剩余 " + n.remain +
                              " 颗，余 " + (n.remain % NormalCapacity));
            foreach (var car in r.oddCars)
                sb.AppendLine("  [2] " + car.Where() + "：颜色 " + car.colorId + Label(car.colorId, config) +
                              " / 容量 " + car.capacity);
            foreach (var m in r.mismatches)
                sb.AppendLine("  [4] 颜色 " + m.colorId + Label(m.colorId, config) + "：剩余 " + m.remainPixels +
                              " 颗 → 需 " + m.neededCars + " 辆，现有 " + m.carCount + " 辆（差 " + m.CarDiff + "）");

            return sb.ToString().TrimEnd();
        }

        /// <summary>列表按条数上限打印，超出部分提示去 Console 看。</summary>
        private static void AppendLimited<T>(StringBuilder sb, List<T> list, System.Action<T, StringBuilder> appendLine)
        {
            int n = Mathf.Min(list.Count, MaxDialogLines);
            for (int i = 0; i < n; i++)
                appendLine(list[i], sb);
            if (list.Count > n)
                sb.AppendLine("  …（还有 " + (list.Count - n) + " 条，详见 Console）");
        }

        private static string Label(int colorId, ColorConfig config)
        {
            if (config == null)
                return "";
            var mat = config.GetMaterial(colorId);
            if (mat == null || string.IsNullOrEmpty(mat.name))
                return "";
            return "（" + mat.name + "）";
        }
    }
}
