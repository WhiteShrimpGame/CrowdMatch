using System.Collections.Generic;
using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 墙体单位：必须作为 PixelGroup 的子物体。用一组网格坐标端点（Vector2：x = 列 col，y = 行 row）
    /// 定义若干段墙体，相邻两个端点构成一段，每段必须平行于 X 或 Z 轴（端点 x 或 y 相等）。
    /// 墙体占据其经过的所有网格格（含端点与中间格），在暴露与寻路逻辑中作为障碍，
    /// 阻止 Pixel 穿过墙体离开。Gizmos 无论是否选中都会显示。
    /// </summary>
    public class WallItem : MonoBehaviour
    {
        [Header("墙体")]
        [Tooltip("墙体端点（网格坐标：x = 列 col，y = 行 row）。相邻两点构成一段，每段必须平行于 X 或 Z 轴。")]
        public List<Vector2> points = new List<Vector2>();

        [Tooltip("墙高度（世界单位，仅用于 Gizmos 显示）")]
        public float height = 2f;

        [Tooltip("Gizmos 显示颜色")]
        public Color gizmoColor = new Color(1f, 0.35f, 0.35f, 0.65f);

        /// <summary>所属 PixelGroup（由 PixelGroup.RebuildGrid 赋值，不序列化）。</summary>
        [System.NonSerialized] public PixelGroup group;

        [System.NonSerialized] private PixelGroup _gizmoGroup;

        /// <summary>查找所属 PixelGroup（编辑器 Gizmos 用，惰性缓存）。</summary>
        public PixelGroup Group
        {
            get
            {
                if (_gizmoGroup == null)
                    _gizmoGroup = GetComponentInParent<PixelGroup>();
                return _gizmoGroup;
            }
        }

        /// <summary>把网格坐标（浮点）换算为整格格坐标（就近取整）。</summary>
        public static Vector2Int ToCell(Vector2 p)
        {
            return new Vector2Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y));
        }

        /// <summary>校验所有段是否都平行于 X 或 Z 轴；返回错误描述（null = 有效）。</summary>
        public bool IsValid(out string error)
        {
            for (int i = 0; i + 1 < points.Count; i++)
            {
                Vector2 a = points[i];
                Vector2 b = points[i + 1];
                if (!Mathf.Approximately(a.x, b.x) && !Mathf.Approximately(a.y, b.y))
                {
                    error = "第 " + (i + 1) + " 段（" + a + " → " + b + "）不平行于 X/Z 轴，端点需满足 x 或 y 相等。";
                    return false;
                }
            }
            error = null;
            return true;
        }

        /// <summary>枚举一段墙经过的所有网格格（含两端与中间格；对角段按逐格阶梯枚举作为兜底）。</summary>
        private static void EnumerateSegment(Vector2 a, Vector2 b, HashSet<Vector2Int> set)
        {
            Vector2Int ca = ToCell(a);
            Vector2Int cb = ToCell(b);

            int steps = Mathf.Max(Mathf.Abs(cb.x - ca.x), Mathf.Abs(cb.y - ca.y));
            int dx = System.Math.Sign(cb.x - ca.x);
            int dy = System.Math.Sign(cb.y - ca.y);

            set.Add(ca);
            for (int i = 1; i <= steps; i++)
                set.Add(new Vector2Int(ca.x + dx * i, ca.y + dy * i));
        }

        /// <summary>把一组端点（网格坐标）换算为占据的网格格集合（静态，供关卡加载等无实例场景复用）。</summary>
        public static void CollectOccupiedCells(IReadOnlyList<Vector2> points, HashSet<Vector2Int> set)
        {
            if (points == null)
                return;
            for (int i = 0; i + 1 < points.Count; i++)
                EnumerateSegment(points[i], points[i + 1], set);
        }

        /// <summary>枚举墙体占据的所有网格格（去重）。</summary>
        public IEnumerable<Vector2Int> EnumerateOccupiedCells()
        {
            var set = new HashSet<Vector2Int>();
            CollectOccupiedCells(points, set);
            return set;
        }

        /// <summary>墙体占据的网格格数量。</summary>
        public int OccupiedCellCount()
        {
            int n = 0;
            foreach (var _ in EnumerateOccupiedCells())
                n++;
            return n;
        }

        /// <summary>某网格格的世界坐标。</summary>
        private Vector3 CellWorld(Vector2 p)
        {
            var g = Group;
            if (g == null)
                return transform.TransformPoint(new Vector3(p.x, 0f, -p.y));
            Vector2Int c = ToCell(p);
            return g.GetWorldPosition(c.x, c.y);
        }

        /// <summary>Gizmos：无论是否选中都绘制墙体面板与占用格标记。</summary>
        private void OnDrawGizmos()
        {
            var g = Group;
            if (g == null)
                return;

            // 墙体面板（底边 + 顶边 + 两端竖线）
            Gizmos.color = gizmoColor;
            for (int i = 0; i + 1 < points.Count; i++)
            {
                Vector3 a = CellWorld(points[i]);
                Vector3 b = CellWorld(points[i + 1]);
                Vector3 aTop = a + Vector3.up * height;
                Vector3 bTop = b + Vector3.up * height;

                Gizmos.DrawLine(a, b);
                Gizmos.DrawLine(aTop, bTop);
                Gizmos.DrawLine(a, aTop);
                Gizmos.DrawLine(b, bTop);
            }

            // 占用格：底面浅色方块标记（可直观看到墙阻挡了哪些格）
            Color cellColor = gizmoColor;
            cellColor.a = Mathf.Clamp01(gizmoColor.a * 0.4f);
            Gizmos.color = cellColor;
            float halfX = g.CellSizeX * 0.45f;
            float halfZ = g.CellSizeZ * 0.45f;
            foreach (var cell in EnumerateOccupiedCells())
            {
                if (!g.IsInRange(cell.x, cell.y))
                    continue;
                Vector3 center = g.GetWorldPosition(cell.x, cell.y);
                Gizmos.DrawCube(center, new Vector3(halfX * 2f, 0.04f, halfZ * 2f));
            }
        }

        /// <summary>选中时额外绘制白色描边，便于定位端点。</summary>
        private void OnDrawGizmosSelected()
        {
            var g = Group;
            if (g == null)
                return;

            Color prev = Gizmos.color;
            Gizmos.color = Color.white;
            for (int i = 0; i + 1 < points.Count; i++)
            {
                Vector3 a = CellWorld(points[i]);
                Vector3 b = CellWorld(points[i + 1]);
                Vector3 aTop = a + Vector3.up * height;
                Vector3 bTop = b + Vector3.up * height;
                Gizmos.DrawLine(a, b);
                Gizmos.DrawLine(aTop, bTop);
            }
            Gizmos.color = prev;
        }
    }
}
