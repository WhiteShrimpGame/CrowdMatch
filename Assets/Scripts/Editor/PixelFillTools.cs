using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CrowdMatch
{
    /// <summary>
    /// 菜单「CrowdMatch ▸ Pixel 工具」下的补 Pixel 工具（均支持 Undo，仅非运行模式可用，颜色取共享的填充颜色 ID）：
    /// 「填充全部空格」直接生效；「填充矩形范围空格」弹窗输入矩形对角坐标。
    /// 两者都只填空格（不含墙 / 管道 / 未开箱的箱子），范围内已有 Pixel 不受影响。
    /// </summary>
    public static class PixelFillMenu
    {
        [MenuItem("CrowdMatch/Pixel 工具/填充全部空格", false, MenuPriority.Pixel + MenuPriority.Seg1)]
        private static void FillAllEmptyCells()
        {
            var group = ResolveGroup();
            if (group == null)
                return;

            if (group.columns <= 0 || group.TotalRows <= 0)
            {
                EditorUtility.DisplayDialog("用 Pixel 填充全部空格", "该 PixelGroup 的网格尺寸无效。", "确定");
                return;
            }

            group.RebuildGrid();
            GridFillUtility.FillEmptyCells(group, AllCells(group), GridFillUtility.FillColorId, "填充全部空格");
        }

        [MenuItem("CrowdMatch/Pixel 工具/填充矩形范围空格", false, MenuPriority.Pixel + MenuPriority.Seg1 + 1)]
        private static void FillRectEmptyCells()
        {
            var group = ResolveGroup();
            if (group == null)
                return;

            PixelRectFillWindow.Open(group);
        }

        /// <summary>网格内全部格（越界与已占用由填充流程逐格过滤）。</summary>
        private static List<Vector2Int> AllCells(PixelGroup group)
        {
            var cells = new List<Vector2Int>(group.columns * group.TotalRows);
            for (int r = 0; r < group.TotalRows; r++)
            {
                for (int c = 0; c < group.columns; c++)
                    cells.Add(new Vector2Int(c, r));
            }
            return cells;
        }

        /// <summary>取本次操作的 PixelGroup：优先选中物体（或其父级），否则用场景里唯一的那个。</summary>
        private static PixelGroup ResolveGroup()
        {
            if (Selection.activeGameObject != null)
            {
                var fromSelection = Selection.activeGameObject.GetComponentInParent<PixelGroup>();
                if (fromSelection != null)
                    return fromSelection;
            }

            var all = Object.FindObjectsOfType<PixelGroup>();
            if (all.Length == 1)
                return all[0];

            EditorUtility.DisplayDialog("填充 Pixel",
                all.Length == 0
                    ? "场景里没有 PixelGroup。"
                    : "场景里有 " + all.Length + " 个 PixelGroup，请先选中要填充的那个。",
                "确定");
            return null;
        }
    }

    /// <summary>
    /// 「用 Pixel 填充矩形范围空格」参数窗口：输入矩形对角的横 / 纵坐标与填充颜色 ID。
    /// 默认值 = 网格里最左上的空格，及其最大全空矩形的右下角；对角输入顺序任意（内部按 min/max 归一化）。
    /// 用 EditorWindow 而非 ScriptableWizard：字段标签要写清「横坐标 / 纵坐标」给策划看，
    /// 而 ScriptableWizard 只能按字段名自动生成标签，无法自定义。
    /// </summary>
    public class PixelRectFillWindow : EditorWindow
    {
        private PixelGroup _group;
        private int _leftCol;
        private int _topRow;
        private int _rightCol;
        private int _bottomRow;
        private int _colorId;

        /// <summary>打开参数窗口（菜单调用），并按当前网格重算默认范围。</summary>
        public static void Open(PixelGroup group)
        {
            var window = GetWindow<PixelRectFillWindow>(true, "用 Pixel 填充矩形范围空格", true);
            window.minSize = new Vector2(380f, 250f);
            window._group = group;
            window.Initialize();
            window.Show();
        }

        /// <summary>按当前网格重算默认值：最左上的空格 + 其最大全空矩形的右下角。</summary>
        private void Initialize()
        {
            _colorId = GridFillUtility.FillColorId;

            if (_group == null)
                return;

            _group.RebuildGrid();   // IsEmpty 依赖当前占用表

            int lc, tr, rc, br;
            if (TryFindTopLeftEmptyRect(_group, out lc, out tr, out rc, out br))
            {
                _leftCol = lc;
                _topRow = tr;
                _rightCol = rc;
                _bottomRow = br;
                return;
            }

            // 一个空格都没有：退化成整个网格
            _leftCol = 0;
            _topRow = 0;
            _rightCol = Mathf.Max(0, _group.columns - 1);
            _bottomRow = Mathf.Max(0, _group.TotalRows - 1);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("矩形对角（只填范围内的空格）", EditorStyles.boldLabel);

            _leftCol = EditorGUILayout.IntField("左上（横坐标）", _leftCol);
            _topRow = EditorGUILayout.IntField("左上（纵坐标）", _topRow);
            _rightCol = EditorGUILayout.IntField("右下（横坐标）", _rightCol);
            _bottomRow = EditorGUILayout.IntField("右下（纵坐标）", _bottomRow);

            EditorGUILayout.Space();
            _colorId = EditorGUILayout.IntField("填充颜色 ID", _colorId);

            int c0, c1, r0, r1;
            Normalize(out c0, out c1, out r0, out r1);

            bool ready = _group != null && _group.columns > 0 && _group.TotalRows > 0;

            EditorGUILayout.Space();
            if (!ready)
            {
                EditorGUILayout.HelpBox(
                    _group == null ? "未找到 PixelGroup。" : "该 PixelGroup 的网格尺寸无效。",
                    MessageType.Error);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "网格 " + _group.columns + " 列 × " + _group.TotalRows + " 行。\n" +
                    "横坐标 = 列 col（0 = 最左），纵坐标 = 行 row（0 = 最上）；两个角填反了也没关系。\n" +
                    "将填充 (" + c0 + ", " + r0 + ") → (" + c1 + ", " + r1 + ") 范围内的 " +
                    CountFillable(_group, c0, c1, r0, r1) +
                    " 个空格（墙 / 管道 / 未开箱的箱子与已有 Pixel 不动）。",
                    MessageType.Info);
            }

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(!ready))
            {
                if (GUILayout.Button("填充"))
                {
                    Fill(c0, c1, r0, r1);
                    Close();
                }
            }
            if (GUILayout.Button("取消"))
                Close();
        }

        private void Fill(int c0, int c1, int r0, int r1)
        {
            if (_group == null)
                return;

            var cells = new List<Vector2Int>((c1 - c0 + 1) * (r1 - r0 + 1));
            for (int r = r0; r <= r1; r++)
            {
                for (int c = c0; c <= c1; c++)
                    cells.Add(new Vector2Int(c, r));
            }

            GridFillUtility.FillColorId = _colorId;
            GridFillUtility.FillEmptyCells(_group, cells, _colorId, "填充矩形范围空格");
        }

        /// <summary>把两角按 min/max 归一化，并夹到网格范围内（网格无效时保持原值）。</summary>
        private void Normalize(out int c0, out int c1, out int r0, out int r1)
        {
            c0 = Mathf.Min(_leftCol, _rightCol);
            c1 = Mathf.Max(_leftCol, _rightCol);
            r0 = Mathf.Min(_topRow, _bottomRow);
            r1 = Mathf.Max(_topRow, _bottomRow);

            if (_group == null || _group.columns <= 0 || _group.TotalRows <= 0)
                return;

            c0 = Mathf.Clamp(c0, 0, _group.columns - 1);
            c1 = Mathf.Clamp(c1, 0, _group.columns - 1);
            r0 = Mathf.Clamp(r0, 0, _group.TotalRows - 1);
            r1 = Mathf.Clamp(r1, 0, _group.TotalRows - 1);
        }

        /// <summary>范围内可填充的空格数（供窗口内提示实时反馈）。</summary>
        private static int CountFillable(PixelGroup g, int c0, int c1, int r0, int r1)
        {
            int n = 0;
            for (int r = r0; r <= r1; r++)
            {
                for (int c = c0; c <= c1; c++)
                {
                    if (g.IsEmpty(c, r))
                        n++;
                }
            }
            return n;
        }

        /// <summary>
        /// 找最左上的空格（行 0 起、列 0 起逐格扫）及其「最大全空矩形」的右下角：
        /// 自该格向下逐行延伸，宽度取「每行自该格起连续空格数」的累计最小值，取面积最大者。
        /// 网格里没有空格时返回 false。
        /// </summary>
        private static bool TryFindTopLeftEmptyRect(PixelGroup g, out int leftCol, out int topRow, out int rightCol, out int bottomRow)
        {
            leftCol = 0;
            topRow = 0;
            rightCol = 0;
            bottomRow = 0;

            for (int r = 0; r < g.TotalRows; r++)
            {
                for (int c = 0; c < g.columns; c++)
                {
                    if (!g.IsEmpty(c, r))
                        continue;

                    int width = RunWidth(g, c, r);
                    int bestWidth = width;
                    int bestHeight = 1;
                    for (int rr = r + 1; rr < g.TotalRows && g.IsEmpty(c, rr); rr++)
                    {
                        width = Mathf.Min(width, RunWidth(g, c, rr));
                        if ((rr - r + 1) * width > bestWidth * bestHeight)
                        {
                            bestWidth = width;
                            bestHeight = rr - r + 1;
                        }
                    }

                    leftCol = c;
                    topRow = r;
                    rightCol = c + bestWidth - 1;
                    bottomRow = r + bestHeight - 1;
                    return true;
                }
            }
            return false;
        }

        /// <summary>自 (col, row) 起向右的连续空格数。</summary>
        private static int RunWidth(PixelGroup g, int col, int row)
        {
            int w = 0;
            while (col + w < g.columns && g.IsEmpty(col + w, row))
                w++;
            return w;
        }
    }
}
