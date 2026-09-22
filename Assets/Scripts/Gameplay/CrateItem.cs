using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 木箱（Crate）：网格内一块**完整矩形**区域（左上 + 右下，长宽均 ≥ 2）。
    ///
    /// 它**盖住**范围内的像素：不可见（关掉这些像素的渲染器）、不可点击（HandleClick 的
    /// <c>IsCovered</c> 守卫直接返回，**没有任何反馈** —— 那里看起来本来就没有像素，点了就该像没点到），
    /// 并且整块矩形（含其中的空格）都算障碍（并入 <see cref="PixelGroup.IsBlocked"/>）。
    /// 于是木箱格同时是同色连通块的边界 —— 贴着木箱的同色像素不会被木箱里的像素连累点亮，
    /// 点击也不会把木箱里的像素一起带走。这一点与 <see cref="IceItem"/> 的冰格是同一个套路，
    /// 区别是冰格不是障碍（只「视为不暴露」），木箱格是障碍。
    ///
    /// 拆箱靠**外部点击**：每成功点击移出一次，与本次移出的像素上下左右（4 邻）相接的木箱各计 1 次；
    /// 同一次点击移出的是同一组，组内有多颗挨着木箱也只算 1 次。计满
    /// <see cref="destroyAfterMoves"/> 次即销毁，底下像素随之恢复可见、按正常规则重新判定暴露、恢复可点。
    ///
    /// 木箱**不产生**像素、不改 TotalPixelCount：被盖住的像素是先算进像素总数、再被盖住的，
    /// 所以必须先拆箱、再点出去，关卡才可能通。
    /// </summary>
    public class CrateItem : MonoBehaviour
    {
        [Header("区域（左上 + 右下）")]
        public int colMin, rowMin, colMax, rowMax;

        [Header("行为")]
        [Tooltip("拆箱所需的「相邻像素移出」次数（同一次点击移出的一组只算 1 次）")]
        [Min(1)]
        public int destroyAfterMoves = 3;

        [Header("视觉（3 个预制体，各占一格；留空则取 PixelGroup 上的木箱预制体）")]
        [Tooltip("四个角格用的木箱预制体")]
        public GameObject cornerPrefab;

        [Tooltip("边缘格（非角）用的木箱预制体")]
        public GameObject edgePrefab;

        [Tooltip("中心格（非边缘）用的木箱预制体")]
        public GameObject centerPrefab;

        /// <summary>消失动画两段的时长（秒），与 BoxItem 保持一致。</summary>
        private const float PopDuration = 0.2f;
        private const float ShrinkDuration = 0.2f;

        /// <summary>所属 PixelGroup（由 PixelGroup.RebuildGrid / SpawnCrate 赋值，不序列化）。</summary>
        [System.NonSerialized] public PixelGroup group;

        /// <summary>已计数的「相邻像素移出」次数。运行时值，不序列化（重进关卡由 SpawnCrate 复位）。</summary>
        [System.NonSerialized] public int movedOutCount;

        /// <summary>是否已拆掉。拆掉后不再占格、不再盖像素、不再计数。</summary>
        [System.NonSerialized] public bool destroyed;

        /// <summary>拼接出的木箱视觉块（消失动画 + 重建显示用）。</summary>
        private readonly List<GameObject> _visualPieces = new List<GameObject>();

        /// <summary>本体格集合（惰性缓存；字段被 Inspector 改动后由 <see cref="RefreshCells"/> 重算）。</summary>
        private HashSet<Vector2Int> _cells;

        /// <summary>本体横向格数。</summary>
        public int ColCount => colMax - colMin + 1;

        /// <summary>本体纵向格数。</summary>
        public int RowCount => rowMax - rowMin + 1;

        /// <summary>长宽是否都 ≥ 2（木箱唯一的形状约束）。</summary>
        public bool IsValidSize => ColCount >= 2 && RowCount >= 2;

        /// <summary>还差几次被拆掉（供 Inspector 显示；表现层以后可以用它做破损程度）。</summary>
        public int RemainingMoves => Mathf.Max(0, Mathf.Max(1, destroyAfterMoves) - movedOutCount);

        /// <summary>本体格集合（网格坐标）。</summary>
        public HashSet<Vector2Int> Cells
        {
            get
            {
                if (_cells == null)
                    RefreshCells();
                return _cells;
            }
        }

        /// <summary>按当前字段重算本体格集合。</summary>
        public void RefreshCells()
        {
            if (_cells == null)
                _cells = new HashSet<Vector2Int>();
            else
                _cells.Clear();

            for (int r = rowMin; r <= rowMax; r++)
                for (int c = colMin; c <= colMax; c++)
                    _cells.Add(new Vector2Int(c, r));
        }

        /// <summary>该格是否在木箱本体矩形内。</summary>
        public bool IsInBody(int c, int r)
        {
            return c >= colMin && c <= colMax && r >= rowMin && r <= rowMax;
        }

        /// <summary>枚举本体矩形内的所有格子。</summary>
        public void EnumerateBody(List<Vector2Int> outList)
        {
            for (int r = rowMin; r <= rowMax; r++)
                for (int c = colMin; c <= colMax; c++)
                    outList.Add(new Vector2Int(c, r));
        }

        /// <summary>
        /// 拼接木箱视觉（角/边/中心 3 类预制体按格子，与 BoxItem 同构）。
        /// 根摆在整块中心、不旋转不缩放；每块摆到自己格子的世界位置（用世界坐标，不假设根的层级深度）。
        ///
        /// 可重复调用：每次都先清掉上一批拼接块，所以编辑器的「重建显示」按钮可以反复点。
        /// </summary>
        public void BuildVisual(PixelGroup pg)
        {
            group = pg;
            if (pg == null)
                return;

            RefreshCells();
            ClearVisual();

            transform.position = (pg.GetWorldPosition(colMin, rowMin) + pg.GetWorldPosition(colMax, rowMax)) * 0.5f;
            transform.localRotation = Quaternion.identity;

            for (int r = rowMin; r <= rowMax; r++)
            {
                for (int c = colMin; c <= colMax; c++)
                {
                    var prefab = PiecePrefab(c, r);
                    if (prefab == null)
                    {
                        Debug.LogWarning("[CrateItem] 木箱视觉预制体缺失，跳过格子 (" + c + "," + r +
                            ")。请在木箱上或 PixelGroup 上补上 crateCorner/Edge/CenterPrefab。");
                        continue;
                    }

                    var piece = PrefabSpawner.Instantiate(prefab, transform);
                    if (piece == null)
                        continue;

                    piece.name = "CratePiece_" + r + "_" + c;
                    piece.transform.position = pg.GetWorldPosition(c, r);
                    piece.transform.localRotation = Quaternion.identity;
                    piece.transform.localScale = Vector3.one * pg.unitSize;   // 与箱子拼块同一口径
                    _visualPieces.Add(piece);
                }
            }
        }

        /// <summary>清掉已拼接的木箱视觉块（编辑器非 Play 模式下用 DestroyImmediate）。</summary>
        public void ClearVisual()
        {
            for (int i = 0; i < _visualPieces.Count; i++)
            {
                var piece = _visualPieces[i];
                if (piece == null)
                    continue;
                if (Application.isPlaying)
                    Destroy(piece);
                else
                    DestroyImmediate(piece);
            }
            _visualPieces.Clear();
        }

        /// <summary>该格用哪个视觉预制体（角/边/中心）；本物体上没配就回退到 PixelGroup 上的木箱预制体。</summary>
        private GameObject PiecePrefab(int c, int r)
        {
            bool isLeft = c == colMin;
            bool isRight = c == colMax;
            bool isTop = r == rowMin;
            bool isBottom = r == rowMax;

            if ((isLeft || isRight) && (isTop || isBottom))
                return cornerPrefab != null ? cornerPrefab : (group != null ? group.crateCornerPrefab : null);
            if (isLeft || isRight || isTop || isBottom)
                return edgePrefab != null ? edgePrefab : (group != null ? group.crateEdgePrefab : null);
            return centerPrefab != null ? centerPrefab : (group != null ? group.crateCenterPrefab : null);
        }

        /// <summary>
        /// 记一次「相邻像素移出」（一次点击调用一次，重复计数由调用方保证 —— 同一组只算 1 次）。
        /// 达到阈值则本箱转为已拆：立刻不再占格 / 不再盖像素（掩码由 PixelGroup.RefreshCrateState 重建），
        /// 并播放「弹一下再缩小」的消失动画。返回**本次是否刚拆掉**。
        /// </summary>
        public bool RegisterAdjacentMoveOut()
        {
            if (destroyed)
                return false;

            movedOutCount++;
            if (movedOutCount < Mathf.Max(1, destroyAfterMoves))
                return false;

            destroyed = true;
            DisappearVisual();
            return true;
        }

        /// <summary>木箱视觉消失：弹一下再缩小（各块独立，回调里销毁自己）。</summary>
        private void DisappearVisual()
        {
            for (int i = 0; i < _visualPieces.Count; i++)
            {
                var piece = _visualPieces[i];
                if (piece == null)
                    continue;

                piece.transform.DisappearWithPop(() =>
                {
                    if (piece != null)
                        Destroy(piece);
                }, PopDuration, ShrinkDuration, restoreScale: false);
            }
            _visualPieces.Clear();
        }

        /// <summary>
        /// 编辑器里销毁木箱（删掉物体 / 清空子物体）时，把被盖像素的渲染还原。
        ///
        /// 需要它的原因：编辑模式下我们**也**会关掉被盖像素的渲染器（与运行时表现一致，
        /// 否则 Scene 视图里像素会从木箱里穿出来），而 <c>Renderer.enabled</c> 是**会序列化的** ——
        /// 木箱一没，就再没有任何东西会去把它们打开，场景里就留下了一片看不见的像素。
        /// 运行时不需要这一步：像素要么随关卡重建（ClearPixels）销毁，要么由
        /// RefreshCrateState 按最新掩码统一处理。
        /// </summary>
        private void OnDestroy()
        {
            if (Application.isPlaying || group == null || group.grid == null)
                return;

            foreach (var cell in Cells)
            {
                if (!group.IsInRange(cell.x, cell.y))
                    continue;
                var item = group.grid[cell.x, cell.y];
                if (item != null)
                    item.SetCovered(false);
            }
        }

        /// <summary>在 Scene 视图绘制木箱区域线框（编辑器预览 + 定位辅助）。</summary>
        private void OnDrawGizmosSelected()
        {
            var pg = group != null ? group : GetComponentInParent<PixelGroup>();
            if (pg == null)
                return;

            Vector3 a = pg.GetLocalPosition(colMin, rowMin);
            Vector3 b = pg.GetLocalPosition(colMax, rowMax);
            Vector3 center = (a + b) * 0.5f;
            Vector3 size = new Vector3(
                Mathf.Abs(b.x - a.x) + pg.CellSizeX,
                0.2f,
                Mathf.Abs(b.z - a.z) + pg.CellSizeZ);

            Color old = Gizmos.color;
            Gizmos.color = IsValidSize ? new Color(0.6f, 0.4f, 0.15f, 0.6f) : new Color(1f, 0.2f, 0.2f, 0.6f);
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = pg.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(center, size);
            Gizmos.matrix = oldMatrix;
            Gizmos.color = old;
        }
    }
}
