using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 箱子（Box）：网格内一块矩形区域（左上 + 右下），内含若干隐藏 Pixel 并标注容量。
    /// 开箱前区域是障碍；当相邻空格全部清空（可用格 == 容量）时开箱，把隐藏 Pixel 确定性
    /// 释放到「本体 + 相邻」空格（格子按 row/col 排序、颜色按 colorIds 顺序），
    /// 并分两段动画（本体外依次 Jump → 本体内原地站起 + 箱子放大缩小消失）。
    /// </summary>
    public class BoxItem : MonoBehaviour
    {
        [Header("区域（左上 + 右下）")]
        public int colMin, rowMin, colMax, rowMax;

        [Header("内容")]
        [Tooltip("容量 = 隐藏 Pixel 数量 = 开箱触发阈值。运行时按周围环境自动计算（本体 + 相邻有效格），可在 Inspector 重算或手动覆盖。")]
        public int capacity;

        [Tooltip("每个隐藏 Pixel 的颜色 ID，长度 == capacity")]
        public int[] colorIds;

        [Header("行为")]
        [Tooltip("本体外 Pixel 起跳间隔（秒）")]
        public float jumpStartInterval = 0.1f;

        [Tooltip("Pixel 出现位置：箱子中心 / 本体格下方（-Y）偏移量（外跳与本体上升共用）")]
        public float jumpSpawnYOffset = 0.5f;

        [Header("外跳表现")]
        [Tooltip("外跳高度（DOLocalJump 的 jumpPower）")]
        public float jumpPower = 0.6f;

        [Tooltip("外跳弹跳次数（DOLocalJump 的 numJumps）")]
        public int jumpCount = 1;

        [Tooltip("外跳时长（秒，DOLocalJump 的 duration）")]
        public float jumpDuration = 0.35f;

        [Tooltip("本体内 Pixel 从 y 向下偏移位置平滑升到初始位置的时长（秒）")]
        public float bodyRiseDuration = 0.35f;

        [Header("调试")]
        [Tooltip("开箱条件判定时输出详细日志（本体/相邻/可用/容量/结果）")]
        public bool debugOpenLog = true;

        [Header("视觉（3 个预制体，各占一格）")]
        public GameObject cornerPrefab;
        public GameObject edgePrefab;
        public GameObject centerPrefab;

        /// <summary>是否已开箱（开箱后仅保留引用，不再参与触发判定）。</summary>
        [System.NonSerialized] public bool opened;

        /// <summary>所属的 PixelGroup（运行时由 RebuildGrid 赋值）。</summary>
        [System.NonSerialized] public PixelGroup group;

        /// <summary>尚未释放的隐藏 Pixel（开箱时逐个移出，落到 grid）。</summary>
        [System.NonSerialized] public readonly List<PixelItem> hiddenPixels = new List<PixelItem>();

        /// <summary>箱子视觉拼接出的 3 类预制体实例（开箱消失动画用）。</summary>
        private readonly List<GameObject> _visualPieces = new List<GameObject>();

        public int BodyCount => (colMax - colMin + 1) * (rowMax - rowMin + 1);

        /// <summary>
        /// 计算箱子容量 = 本体格子数 + 相邻有效格数（越界/墙体/管道/其它箱子本体不计数）。
        /// 相邻固定按 8 方向（含四角）计算，与开箱触发、确定性落点口径一致。
        /// </summary>
        public int ComputeCapacity() => ComputeCapacity(group, colMin, rowMin, colMax, rowMax);

        public static int ComputeCapacity(PixelGroup group, int colMin, int rowMin, int colMax, int rowMax)
        {
            int body = (colMax - colMin + 1) * (rowMax - rowMin + 1);
            if (group == null)
                return body;

            int adjacent = 0;
            for (int r = rowMin - 1; r <= rowMax + 1; r++)
            {
                for (int c = colMin - 1; c <= colMax + 1; c++)
                {
                    bool inBody = c >= colMin && c <= colMax && r >= rowMin && r <= rowMax;
                    if (inBody)
                        continue;
                    if (!group.IsInRange(c, r))
                        continue;
                    if (group.IsBlocked(c, r))
                        continue;
                    adjacent++;
                }
            }
            return body + adjacent;
        }

        /// <summary>该格是否在箱子本体矩形内。</summary>
        public bool IsInBody(int c, int r)
        {
            return c >= colMin && c <= colMax && r >= rowMin && r <= rowMax;
        }

        /// <summary>枚举箱子本体矩形内的所有格子。</summary>
        public void EnumerateBody(List<Vector2Int> outList)
        {
            for (int r = rowMin; r <= rowMax; r++)
                for (int c = colMin; c <= colMax; c++)
                    outList.Add(new Vector2Int(c, r));
        }

        /// <summary>在 Scene 视图绘制箱子区域线框（编辑器预览 + 运行时定位辅助）。</summary>
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
            Gizmos.color = new Color(1f, 0.6f, 0f, 0.6f);
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = pg.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(center, size);
            Gizmos.matrix = oldMatrix;
            Gizmos.color = old;
        }

        /// <summary>
        /// 拼接箱子视觉（角/边/中心 3 类预制体按格子）+ 生成隐藏 Pixel。
        /// 隐藏 Pixel 作为 PixelGroup 子物体（与普通 Pixel 同父级），哨兵坐标 gridX=gridZ=-1，初始隐藏。
        /// </summary>
        public void BuildVisual(PixelGroup pg, ColorConfig config)
        {
            group = pg;

            // 隐藏 Pixel 数量：以 min(capacity, colorIds.Length) 为准（§12 校验），并归一化 capacity
            int count = colorIds != null ? Mathf.Min(capacity, colorIds.Length) : 0;
            if (count != capacity)
                Debug.LogWarning("[BoxItem] 箱子容量 " + capacity + " 与 colorIds 数量 " +
                    (colorIds != null ? colorIds.Length : 0) + " 不一致，按较小值处理。");
            capacity = count;

            Vector3 appear = BoxCenterLocal() + new Vector3(0f, -jumpSpawnYOffset, 0f);

            for (int i = 0; i < capacity; i++)
            {
                int colorId = colorIds != null && i < colorIds.Length ? colorIds[i] : 0;
                var go = Instantiate(pg.pixelPrefab, pg.transform);
                go.name = "BoxPixel_" + rowMin + "_" + colMin + "_" + i;
                go.transform.localPosition = appear;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one * pg.unitSize;

                var item = go.GetComponent<PixelItem>();
                if (item == null)
                {
                    Debug.LogError("[BoxItem] pixelPrefab 缺少 PixelItem 组件，无法生成箱内隐藏 Pixel。");
                    Destroy(go);
                    continue;
                }

                item.gridX = -1;
                item.gridZ = -1;
                item.colorId = colorId;
                item.ApplyMaterial(config);
                item.SetClickable(false);
                go.SetActive(false);

                hiddenPixels.Add(item);
            }

            // 箱子视觉：3 类预制体按格子拼接（§6）
            for (int r = rowMin; r <= rowMax; r++)
            {
                for (int c = colMin; c <= colMax; c++)
                {
                    var prefab = ChoosePiecePrefab(c, r);
                    if (prefab == null)
                    {
                        Debug.LogWarning("[BoxItem] 箱子视觉预制体为空，跳过格子 (" + c + "," + r + ")。");
                        continue;
                    }
                    var piece = Instantiate(prefab, transform);
                    piece.name = "BoxPiece_" + r + "_" + c;
                    piece.transform.localPosition = pg.GetLocalPosition(c, r);
                    piece.transform.localRotation = Quaternion.identity;
                    piece.transform.localScale = Vector3.one * pg.unitSize;
                    _visualPieces.Add(piece);
                }
            }
        }

        /// <summary>该格用哪个视觉预制体（角/边/中心，按 §6 规则）。</summary>
        private GameObject ChoosePiecePrefab(int c, int r)
        {
            bool isLeft = c == colMin;
            bool isRight = c == colMax;
            bool isTop = r == rowMin;
            bool isBottom = r == rowMax;

            if ((isLeft || isRight) && (isTop || isBottom))
                return cornerPrefab;
            if (isLeft || isRight || isTop || isBottom)
                return edgePrefab;
            return centerPrefab;
        }

        /// <summary>箱子矩形中心在 PixelGroup 本地空间的位置（外跳出现点基准）。</summary>
        private Vector3 BoxCenterLocal()
        {
            if (group == null)
                return Vector3.zero;
            Vector3 a = group.GetLocalPosition(colMin, rowMin);
            Vector3 b = group.GetLocalPosition(colMax, rowMax);
            return (a + b) * 0.5f;
        }

        /// <summary>
        /// 尝试开箱：计算可用格，满足触发条件（相邻空格全部清空）则按确定性顺序分配位置
        /// （候选格按 row/col 排序、颜色按 colorIds 顺序），立即把释放的 Pixel 落到 grid
        /// （供多箱串行判定与后续逻辑看到），并启动两段开箱动画。返回是否实际开箱。
        /// </summary>
        public bool TryOpen()
        {
            if (opened || group == null)
                return false;

            // 无内容：直接清障碍 + 消失，无需释放动画
            if (hiddenPixels.Count == 0)
            {
                opened = true;
                group.OnBoxOpened(this);
                DisappearVisual();
                group.OnBoxReleaseFinished(this);
                return true;
            }

            // 1. 收集可用格：本体 + 相邻（固定 8 方向，含四角）
            var body = new List<Vector2Int>();
            EnumerateBody(body);
            var adjacent = CollectAdjacentEmpty();

            int available = body.Count + adjacent.Count;
            if (debugOpenLog)
            {
                Debug.Log("[Box] 开箱判定 " + name + "：本体=" + body.Count +
                    " 相邻=" + adjacent.Count +
                    " 可用=" + available + " 容量=" + capacity +
                    " 隐藏=" + hiddenPixels.Count +
                    (available >= capacity ? " → 开箱" : " → 空间不足，继续等待"));
            }
            if (available < capacity)
                return false;   // 空间不足，继续等待

            // 2. 标记已开 + 清障碍（增量计数由 group 处理）
            opened = true;
            group.OnBoxOpened(this);

            // 3. 确定性分布：所有候选格按 (row 升序, col 升序) 排序（同排先左后右），
            //    隐藏 Pixel 保持 colorIds 顺序（不随机），一一对应。
            var candidates = new List<Vector2Int>(body.Count + adjacent.Count);
            candidates.AddRange(body);
            candidates.AddRange(adjacent);
            candidates.Sort(CompareByRowCol);

            var pixels = new List<PixelItem>(hiddenPixels);   // 保持 colorIds 顺序，不随机

            int n = Mathf.Min(capacity, Mathf.Min(hiddenPixels.Count, candidates.Count));
            var assignments = new List<(PixelItem pixel, Vector2Int cell)>();
            for (int i = 0; i < n; i++)
            {
                var pixel = pixels[i];
                var cell = candidates[i];
                pixel.gridX = cell.x;
                pixel.gridZ = cell.y;
                pixel.group = group;
                if (group.grid != null && group.IsInRange(cell.x, cell.y))
                    group.grid[cell.x, cell.y] = pixel;   // 立即占格，供后续箱子/逻辑看到
                pixel.SetClickable(false);                 // 动画期间不可交互（就位后统一恢复可点击）
                pixel.placing = true;                      // 动画期间不站起、保持 root 初始位置（就位后 MarkPlaced + RefreshExposed 统一激活）
                pixel.walkableDuringExtraction = true;      // 本次点击开箱导致的占格，提取寻路时视为可走（结束后由 CrowdBufferZone 清除）
                assignments.Add((pixel, cell));
                hiddenPixels.Remove(pixel);
            }

            // 4. 启动两段开箱动画
            StartCoroutine(OpenRoutine(assignments));
            return true;
        }

        /// <summary>收集直接相邻空格（固定 8 方向，含四角）。</summary>
        private List<Vector2Int> CollectAdjacentEmpty()
        {
            var result = new List<Vector2Int>();
            int[] dx8 = { 1, -1, 0, 0, 1, 1, -1, -1 };
            int[] dz8 = { 0, 0, 1, -1, 1, -1, 1, -1 };

            for (int r = rowMin; r <= rowMax; r++)
            {
                for (int c = colMin; c <= colMax; c++)
                {
                    for (int d = 0; d < 8; d++)
                    {
                        int nx = c + dx8[d];
                        int nz = r + dz8[d];
                        if (!group.IsInRange(nx, nz))
                            continue;
                        if (IsInBody(nx, nz))
                            continue;
                        if (!group.IsEmpty(nx, nz))
                            continue;
                        var cell = new Vector2Int(nx, nz);
                        if (!result.Contains(cell))
                            result.Add(cell);
                    }
                }
            }
            return result;
        }

        /// <summary>确定性排序：先按 row（排）升序，同排再按 col（列）升序（从左到右）。</summary>
        private static int CompareByRowCol(Vector2Int a, Vector2Int b)
        {
            int rc = a.y.CompareTo(b.y);
            if (rc != 0)
                return rc;
            return a.x.CompareTo(b.x);
        }

        /// <summary>
        /// 两段开箱动画（动画期间 Pixel 不可交互、保持 root 初始位置）：
        /// 阶段一：本体外 Pixel 按 (row, col) 顺序逐个在箱子下方出现并 Jump（起跳间隔 jumpStartInterval）；
        /// 阶段二：最后一个外跳起跳后过一个间隔，本体内 Pixel 从 y 向下偏移位置出现、平滑升到初始位置，同时箱子放大缩小消失；
        /// 阶段三：全部动画结束后统一 MarkPlaced + 恢复可点击 + RefreshExposed（判定连通性 + 站起）。
        /// </summary>
        private IEnumerator OpenRoutine(List<(PixelItem pixel, Vector2Int cell)> assignments)
        {
            var external = new List<(PixelItem pixel, Vector2Int cell)>();
            var body = new List<(PixelItem pixel, Vector2Int cell)>();
            for (int i = 0; i < assignments.Count; i++)
            {
                var a = assignments[i];
                if (IsInBody(a.cell.x, a.cell.y))
                    body.Add(a);
                else
                    external.Add(a);
            }
            external.Sort((a, b) => CompareByRowCol(a.cell, b.cell));

            // 阶段一：本体外 Pixel 按 (row, col) 顺序逐个出现并 Jump（箱子保持可见）
            for (int i = 0; i < external.Count; i++)
            {
                SpawnExternal(external[i].pixel, external[i].cell);
                yield return new WaitForSeconds(jumpStartInterval);
            }

            // 阶段二：本体内 Pixel 从 y 向下偏移位置出现并平滑升到初始位置，同时箱子消失
            for (int i = 0; i < body.Count; i++)
                SpawnBody(body[i].pixel, body[i].cell);
            DisappearVisual();

            // 等本体内平滑升位与最后一个外跳落地都结束，再统一判定连通性 + 站起
            float wait = 0f;
            if (body.Count > 0)
                wait = Mathf.Max(wait, bodyRiseDuration);
            if (external.Count > 0)
                wait = Mathf.Max(wait, Mathf.Max(0f, jumpDuration - jumpStartInterval));
            if (wait > 0f)
                yield return new WaitForSeconds(wait);

            FinalizeRelease(assignments);
        }

        /// <summary>全部动画结束：统一 MarkPlaced + 恢复可点击 + RefreshExposed（判定连通性 + 站起），并解除箱体释放计数。</summary>
        private void FinalizeRelease(List<(PixelItem pixel, Vector2Int cell)> assignments)
        {
            for (int i = 0; i < assignments.Count; i++)
            {
                var pixel = assignments[i].pixel;
                if (pixel == null || group == null || group.grid == null)
                    continue;
                // 已被后续匹配移出网格：不再处理（其 placing 标记无副作用，交由匹配流程接管）
                if (!group.IsInRange(pixel.gridX, pixel.gridZ) || group.grid[pixel.gridX, pixel.gridZ] != pixel)
                    continue;
                pixel.MarkPlaced();
                pixel.SetClickable(true);
            }

            if (group != null)
            {
                group.RefreshExposed();
                group.OnBoxReleaseFinished(this);
            }
        }

        private void SpawnExternal(PixelItem pixel, Vector2Int cell)
        {
            if (pixel == null || group == null)
                return;

            pixel.transform.localPosition = BoxCenterLocal() + new Vector3(0f, -jumpSpawnYOffset, 0f);
            pixel.transform.localRotation = Quaternion.identity;
            pixel.gameObject.SetActive(true);
            Vector3 target = group.GetLocalPosition(cell.x, cell.y);
            pixel.transform.DOLocalJump(target, jumpPower, jumpCount, jumpDuration);
        }

        private void SpawnBody(PixelItem pixel, Vector2Int cell)
        {
            if (pixel == null || group == null)
                return;

            Vector3 target = group.GetLocalPosition(cell.x, cell.y);
            pixel.transform.localPosition = target + new Vector3(0f, -jumpSpawnYOffset, 0f);
            pixel.transform.localRotation = Quaternion.identity;
            pixel.gameObject.SetActive(true);
            pixel.transform.DOLocalMove(target, bodyRiseDuration);
        }

        /// <summary>箱子 3 类预制体整体先放大后缩小消失（DisappearWithPop）。</summary>
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
                }, restoreScale: false);
            }
            _visualPieces.Clear();
        }
    }
}
