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

        /// <summary>把该车所属 ContainerGroup 的洗牌开关关掉并标脏；返回是否找到并处理了 group。</summary>
        private static bool MarkShuffleOff(ContainerItem item)
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
    }
}

