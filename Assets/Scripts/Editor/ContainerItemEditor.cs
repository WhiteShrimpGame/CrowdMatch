using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace CrowdMatch
{
    [CustomEditor(typeof(ContainerItem))]
    [CanEditMultipleObjects]
    public class ContainerItemEditor : Editor
    {
        private ColorConfig colorConfig;

        private void OnEnable()
        {
            colorConfig = ColorConfigLocator.Find();
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("问号车", EditorStyles.boldLabel);

            colorConfig = (ColorConfig)EditorGUILayout.ObjectField("颜色配置", colorConfig, typeof(ColorConfig), false);
            if (colorConfig == null)
                colorConfig = ColorConfigLocator.Find();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("标记为问号车"))
                SetQuestionAll(true);
            if (GUILayout.Button("取消问号标记"))
                SetQuestionAll(false);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("交换颜色（仅非运行模式）", EditorStyles.boldLabel);

            var cars = SelectedCars();
            int colorA, colorB, countEach;
            string selectionError = ValidateSelection(cars, out colorA, out colorB, out countEach);

            using (new EditorGUI.DisabledScope(selectionError != null || Application.isPlaying))
            {
                if (GUILayout.Button(new GUIContent("交换选中车的颜色",
                    "选中的车需恰好两种颜色、且两色车数一致；同色车各自按「排 → 列」位置排序后与另一色一一配对互换")))
                {
                    SwapColors(cars, colorA, colorB, countEach);
                }
            }

            if (Application.isPlaying)
                EditorGUILayout.HelpBox("运行模式下不可用，请先停止运行。", MessageType.Info);
            else if (selectionError != null)
                EditorGUILayout.HelpBox(selectionError, MessageType.Info);
            else
                EditorGUILayout.HelpBox("将按「排 → 列」位置把 颜色 " + colorA + " 与 颜色 " + colorB +
                    " 各 " + countEach + " 辆一一配对互换；交换后所属 ContainerGroup 会标记为不洗牌。", MessageType.Info);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("绳子连接（仅非运行模式）", EditorStyles.boldLabel);

            string ropeError = ValidateRopeSelection(cars);
            string unropeError = ValidateUnropeSelection(cars);

            using (new EditorGUI.DisabledScope(ropeError != null || Application.isPlaying))
            {
                if (GUILayout.Button(new GUIContent("标记选中车为连接",
                    "选中的车需分处相邻的 N 列、每列恰好 1 个；标记后按列序两两成绳（N 辆车 = N-1 条绳）")))
                {
                    MarkRope(cars);
                }
            }

            using (new EditorGUI.DisabledScope(unropeError != null || Application.isPlaying))
            {
                if (GUILayout.Button(new GUIContent("取消选中车的连接",
                    "选中任意一个被连接的车即可取消它所在的整个绳组")))
                {
                    UnmarkRope(cars);
                }
            }

            if (Application.isPlaying)
                EditorGUILayout.HelpBox("运行模式下不可用，请先停止运行。", MessageType.Info);
            else
            {
                string text = ropeError == null
                    ? "标记：将把 " + cars.Count + " 辆车按列序连成一条绳链（共 " + (cars.Count - 1) +
                        " 条绳），并关闭所属 ContainerGroup 的洗牌。"
                    : "标记：" + ropeError;
                if (unropeError != null)
                    text += "\n取消：" + unropeError;
                EditorGUILayout.HelpBox(text, MessageType.Info);
            }
        }

        /// <summary>选中集合里的车（跳过被销毁的引用）。</summary>
        private List<ContainerItem> SelectedCars()
        {
            var cars = new List<ContainerItem>(targets.Length);
            foreach (var t in targets)
            {
                if (t != null)
                    cars.Add((ContainerItem)t);
            }
            return cars;
        }

        /// <summary>
        /// 校验选中集合：恰好两种颜色、且两色车数一致。通过时输出两种颜色（升序）与每色车数；返回错误描述（null = 通过）。
        /// </summary>
        private static string ValidateSelection(List<ContainerItem> cars, out int colorA, out int colorB, out int countEach)
        {
            colorA = 0;
            colorB = 0;
            countEach = 0;

            if (cars.Count < 2)
                return "请选中至少 2 辆车（两种颜色各若干）。";

            var counts = new Dictionary<int, int>();
            foreach (var car in cars)
            {
                int n;
                counts.TryGetValue(car.colorId, out n);
                counts[car.colorId] = n + 1;
            }

            if (counts.Count != 2)
                return "选中的车必须恰好两种颜色，当前是 " + counts.Count + " 种。";

            var keys = new List<int>(counts.Keys);
            keys.Sort();   // 固定顺序，避免提示文案每次重绘都换序
            colorA = keys[0];
            colorB = keys[1];

            if (counts[colorA] != counts[colorB])
                return "两种颜色的车数需一致，当前：颜色 " + colorA + " " + counts[colorA] +
                    " 辆，颜色 " + colorB + " " + counts[colorB] + " 辆。";

            countEach = counts[colorA];
            return null;
        }

        /// <summary>
        /// 把选中车按颜色分成两组、各组按「排 → 列」位置排序后一一配对，互换颜色 ID（容量随车一起互换），
        /// 并把涉及到的 ContainerGroup 全部强制标记为「不洗牌」——交换后的位置是手工指定的，不能再被洗牌打乱。
        /// 只有两种颜色时，颜色本身的结果与配对方式无关（A 组全变 B、B 组全变 A）；配对只影响容量的归属。
        /// </summary>
        private void SwapColors(List<ContainerItem> cars, int colorA, int colorB, int countEach)
        {
            var groupA = new List<ContainerItem>();
            var groupB = new List<ContainerItem>();
            foreach (var car in cars)
                (car.colorId == colorA ? groupA : groupB).Add(car);
            SortByGridPosition(groupA);
            SortByGridPosition(groupB);

            const string undoName = "交换车的颜色";
            foreach (var car in cars)
                RecordItemState(car, undoName);

            for (int i = 0; i < groupA.Count; i++)
            {
                var a = groupA[i];
                var b = groupB[i];

                // 先存下 a 的容量，再依次改写（改写 a 不会影响 b）
                int capacityA = a.capacity;
                ApplyColorAndCapacity(a, b.colorId, b.capacity);
                ApplyColorAndCapacity(b, colorA, capacityA);
            }

            bool marked = false;
            foreach (var car in cars)
            {
                EditorUtility.SetDirty(car);
                if (MarkShuffleOff(car))
                    marked = true;
            }

            Debug.Log("[ContainerItemEditor] 已按 颜色 " + colorA + " ↔ 颜色 " + colorB + " 配对交换 " +
                countEach + " 组共 " + cars.Count + " 辆车的颜色；" +
                (marked ? "并把所属 ContainerGroup 标记为不洗牌。" : "但未找到所属 ContainerGroup，洗牌开关未改动。"));
        }

        /// <summary>按「排（本地 Z）、列（本地 X）」升序排序；位置相同时退回层级顺序，保证配对结果确定、可复现。</summary>
        private static void SortByGridPosition(List<ContainerItem> cars)
        {
            cars.Sort((x, y) =>
            {
                Vector3 px = x.transform.localPosition;
                Vector3 py = y.transform.localPosition;
                if (!Mathf.Approximately(px.z, py.z))
                    return px.z < py.z ? -1 : 1;
                if (!Mathf.Approximately(px.x, py.x))
                    return px.x < py.x ? -1 : 1;
                return x.transform.GetSiblingIndex().CompareTo(y.transform.GetSiblingIndex());
            });
        }

        /// <summary>把该车的颜色 ID 与容量设为给定值（容量走 SetCapacity，同步剩余容量与容量文字），并刷新材质。</summary>
        private void ApplyColorAndCapacity(ContainerItem item, int colorId, int capacity)
        {
            item.colorId = colorId;
            item.SetCapacity(capacity);
            item.ApplyMaterial(colorConfig);
        }

        /// <summary>记录该车交换前需要撤销的状态：车本身、容量文字、以及会被换材质的 Renderer。</summary>
        private static void RecordItemState(ContainerItem item, string undoName)
        {
            Undo.RecordObject(item, undoName);

            // 编辑模式下 Awake 没跑过，capacityText 可能还没解析出来；先解析再登记，让文字改动也能撤销
            if (item.capacityText == null)
                item.capacityText = item.GetComponentInChildren<Text>(true);
            if (item.capacityText != null)
                Undo.RecordObject(item.capacityText, undoName);

            if (item.materialReplacements != null)
            {
                foreach (var rep in item.materialReplacements)
                {
                    if (rep != null && rep.renderer != null)
                        Undo.RecordObject(rep.renderer, undoName);
                }
            }
        }

        /// <summary>把该车所属 ContainerGroup 的洗牌开关关掉并标脏；返回是否找到并处理了 group。
        /// `internal` 是给容器拖移画布的「连绳」复用（洗牌会打乱列位置，绳组关系随即失效）。</summary>
        internal static bool MarkShuffleOff(ContainerItem item)
        {
            var group = item.GetComponentInParent<ContainerGroup>();
            if (group == null)
                return false;

            if (group.shuffleContainers)
            {
                Undo.RecordObject(group, "关闭 ContainerGroup 洗牌");
                group.shuffleContainers = false;
                EditorUtility.SetDirty(group);
            }
            return true;
        }

        private void SetQuestionAll(bool question)
        {
            foreach (var t in targets)
            {
                var item = (ContainerItem)t;

                Undo.RecordObject(item, question ? "Mark Question Container" : "Unmark Question Container");
                if (item.materialReplacements != null)
                {
                    foreach (var rep in item.materialReplacements)
                    {
                        if (rep != null && rep.renderer != null)
                            Undo.RecordObject(rep.renderer, question ? "Mark Question Container" : "Unmark Question Container");
                    }
                }
                if (item.questionObject != null)
                    Undo.RecordObject(item.questionObject, question ? "Mark Question Container" : "Unmark Question Container");

                item.isQuestion = question;
                item.ApplyMaterial(colorConfig);
                item.RefreshQuestionObject();
                EditorUtility.SetDirty(item);
            }
        }

        // ===== 绳子连接 =====

        /// <summary>
        /// 校验「标记为连接」的选中集合；返回错误描述（null = 通过）。
        /// 规则：至少 2 辆车、同列唯一、列号连续、同一 ContainerGroup、均未连接、两端点已配置，
        /// 且与已有绳组不交叉（交叉会导致两组互相等待 → 死锁）。
        /// </summary>
        private static string ValidateRopeSelection(List<ContainerItem> cars)
        {
            if (cars.Count < 2)
                return "请选中至少 2 辆车。";

            var byCol = new Dictionary<int, ContainerItem>();
            foreach (var car in cars)
            {
                if (byCol.ContainsKey(car.gridX))
                    return "同一列只能选 1 个车：第 " + car.gridX + " 列选了多个。";
                byCol[car.gridX] = car;
            }

            int minCol = int.MaxValue;
            int maxCol = int.MinValue;
            foreach (var col in byCol.Keys)
            {
                if (col < minCol) minCol = col;
                if (col > maxCol) maxCol = col;
            }
            if (maxCol - minCol != byCol.Count - 1)
            {
                for (int col = minCol; col <= maxCol; col++)
                {
                    if (!byCol.ContainsKey(col))
                        return "选中的车必须处在相邻的 " + byCol.Count + " 列：缺少第 " + col + " 列。";
                }
                return "选中的车必须处在相邻的 " + byCol.Count + " 列。";
            }

            var group = cars[0].GetComponentInParent<ContainerGroup>();
            if (group == null)
                return "选中的车不在任何 ContainerGroup 下。";

            foreach (var car in cars)
            {
                if (car.GetComponentInParent<ContainerGroup>() != group)
                    return "选中的车不属于同一个 ContainerGroup。";
                if (car.ropeGroupId != 0)
                    return car.name + " 已属于绳组 " + car.ropeGroupId + "，请先取消它的连接。";
                if (car.ropeAnchorLeft == null || car.ropeAnchorRight == null)
                    return car.name + " 未配置 ropeAnchorLeft / ropeAnchorRight（车预制体上需有两个端点空物体），无法建绳。";
            }

            var newRows = new Dictionary<int, int>();   // 列 → 行
            foreach (var car in cars)
                newRows[car.gridX] = car.gridZ;
            return ValidateNoRopeCrossing(newRows, group);
        }

        /// <summary>
        /// 交叉校验：两个绳组若在同一对相邻列上「行序相反」（一条从前往后、另一条从后往前），
        /// 两组的出库条件会互相等待——A 的车要等 B 的车离开某列，B 的车又要等 A 的车离开另一列 → 死锁。
        /// 逐对已有绳组检查。
        ///
        /// <paramref name="newRows"/> = 待建绳组的「列 → 行」；调用方保证入选的车此时
        /// <c>ropeGroupId == 0</c>，故不会混进已有组里。`internal` 是给容器拖移画布的「连绳」复用。
        /// </summary>
        internal static string ValidateNoRopeCrossing(Dictionary<int, int> newRows, ContainerGroup group)
        {
            // 已有绳组：组 id → (列 → 行)。选中的车此时必然 ropeGroupId == 0（上面已拦），故不会混进来。
            var existing = new Dictionary<int, Dictionary<int, int>>();
            foreach (var car in group.GetComponentsInChildren<ContainerItem>())
            {
                if (car == null || car.ropeGroupId == 0)
                    continue;

                Dictionary<int, int> map;
                if (!existing.TryGetValue(car.ropeGroupId, out map))
                {
                    map = new Dictionary<int, int>();
                    existing[car.ropeGroupId] = map;
                }
                map[car.gridX] = car.gridZ;
            }

            foreach (var pair in existing)
            {
                var other = pair.Value;
                for (int col = 0; col + 1 < group.columns; col++)
                {
                    int newFront, newRear, otherFront, otherRear;
                    if (!newRows.TryGetValue(col, out newFront) || !newRows.TryGetValue(col + 1, out newRear))
                        continue;   // 新组没跨这一对列
                    if (!other.TryGetValue(col, out otherFront) || !other.TryGetValue(col + 1, out otherRear))
                        continue;   // 该组没跨这一对列

                    // 同一格不可能有两辆车，故 newFront≠otherFront、newRear≠otherRear，符号判断成立
                    if ((newFront - otherFront > 0) != (newRear - otherRear > 0))
                        return "与已有绳组 " + pair.Key + " 在第 " + col + "–" + (col + 1) +
                            " 列之间交叉（一个从前往后、一个从后往前），两组会互相等待造成死锁。";
                }
            }
            return null;
        }

        /// <summary>校验「取消连接」的选中集合；返回错误描述（null = 通过）。</summary>
        private static string ValidateUnropeSelection(List<ContainerItem> cars)
        {
            if (cars.Count == 0)
                return "请先选中至少 1 辆车。";

            int id = 0;
            foreach (var car in cars)
            {
                if (car.ropeGroupId == 0)
                    return car.name + " 没有连接，无需取消。";
                if (id == 0)
                    id = car.ropeGroupId;
                else if (car.ropeGroupId != id)
                    return "选中的车分属多个绳组，请一次只取消一组。";
            }
            return null;
        }

        /// <summary>标记选中车为同一个新绳组，并强制关闭所属 ContainerGroup 的洗牌（洗牌会打乱列位置，绳组关系随即失效）。</summary>
        private static void MarkRope(List<ContainerItem> cars)
        {
            var group = cars[0].GetComponentInParent<ContainerGroup>();
            if (group == null)
                return;

            int id = NextRopeGroupId(group);

            const string undoName = "标记绳子连接";
            Undo.SetCurrentGroupName(undoName);
            int undoGroup = Undo.GetCurrentGroup();

            foreach (var car in cars)
            {
                Undo.RecordObject(car, undoName);
                car.ropeGroupId = id;
                EditorUtility.SetDirty(car);
            }

            bool marked = MarkShuffleOff(cars[0]);

            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log("[ContainerItemEditor] 已把 " + cars.Count + " 辆车标记为绳组 " + id + "（相邻 " + cars.Count +
                " 列，共 " + (cars.Count - 1) + " 条绳）；" +
                (marked ? "并把所属 ContainerGroup 标记为不洗牌。" : "但未找到所属 ContainerGroup，洗牌开关未改动。"));
        }

        /// <summary>取消选中车所属的**整个**绳组（遍历同组下所有车清零）。</summary>
        private static void UnmarkRope(List<ContainerItem> cars)
        {
            var group = cars[0].GetComponentInParent<ContainerGroup>();
            if (group == null)
                return;

            int id = cars[0].ropeGroupId;

            const string undoName = "取消绳子连接";
            Undo.SetCurrentGroupName(undoName);
            int undoGroup = Undo.GetCurrentGroup();

            int cleared = 0;
            foreach (var car in group.GetComponentsInChildren<ContainerItem>())
            {
                if (car == null || car.ropeGroupId != id)
                    continue;
                Undo.RecordObject(car, undoName);
                car.ropeGroupId = 0;
                EditorUtility.SetDirty(car);
                cleared++;
            }

            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log("[ContainerItemEditor] 已取消绳组 " + id + "，共清除 " + cleared + " 辆车的连接。");
        }

        /// <summary>取该 ContainerGroup 下现有绳组的最大 id + 1（id 只需在单关内唯一）。
        /// `internal` 是给容器拖移画布的「连绳」复用。</summary>
        internal static int NextRopeGroupId(ContainerGroup group)
        {
            int max = 0;
            foreach (var car in group.GetComponentsInChildren<ContainerItem>())
            {
                if (car != null && car.ropeGroupId > max)
                    max = car.ropeGroupId;
            }
            return max + 1;
        }
    }
}

