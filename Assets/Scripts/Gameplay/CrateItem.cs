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
    /// 计数表现：箱体上钉两条**交叉封条**，每条封条两端各一颗钉子，钉子位置 = 木箱矩形的四角
    /// 各向箱内偏移 <c>crateSealInset</c>。每减一次数摘掉一条封条，**最后一次减次数连箱体一起拆掉** ——
    /// 所以次数 3 = 两条封条 + 本体，正好是这套表现的标准形（见 <see cref="SealCount"/>）。
    /// 两个预制体与偏移 / 延长 / 抬高等参数都配在 <see cref="PixelGroup"/> 上（与冰组、升降台预制体同一处）。
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

        [Header("音效")]
        [Tooltip("拆箱计数**扣减但还没拆掉**时播放的音效 tag（须在 AudioConfig 里配好；留空则不播）")]
        public string hitSoundTag = "BoxHit";

        [Tooltip("拆箱计数扣减到 0、木箱**被拆掉**时播放的音效 tag（须在 AudioConfig 里配好；留空则不播）")]
        public string breakSoundTag = "BoxBreak";

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

        /// <summary>
        /// 计数表现的封条组（每组 = 一条封条 + 两端各一颗钉子，挂在木箱根下）。
        /// 下标 0 是**先摘掉**的那条（见 <see cref="BuildSeals"/>）；<see cref="RemoveOneSeal"/> 从下标小的开始摘，
        /// 所以列表顺序就是摘除顺序。每摘一条就从列表里移除，剩下的就是场上还看得见的封条。
        /// </summary>
        private readonly List<GameObject> _sealGroups = new List<GameObject>();

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

        /// <summary>
        /// 封条条数 = 次数 − 1，上限 2。
        ///
        /// 口径：每次减一次数摘一条封条，**最后一次减次数摘本体**。所以次数 3 = 两条交叉封条、
        /// 两次点击各摘一条、第三次拆本体 —— 这是设计的标准形。
        /// 次数 &gt; 3 时封条仍是 2 条（表现就固定是「两条交叉」），多出来的那几次点击只减数不减封条；
        /// 次数 1 时没有封条，点一下直接拆。编辑器对这个落差有提示。
        /// </summary>
        public int SealCount => Mathf.Clamp(Mathf.Max(1, destroyAfterMoves) - 1, 0, 2);

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
        /// 拼接木箱视觉（角/边/中心 3 类预制体按格子，与 BoxItem 同构），最后再拼**计数表现**的封条与钉子。
        /// 根摆在整块中心、不旋转；每块摆到自己格子的世界位置（用世界坐标，不假设根的层级深度）。
        ///
        /// **尺寸与朝向（已与用户核对）**：
        /// · 三个预制体**已按实际尺寸（一格）制作**，所以**不改缩放** —— 与 BoxItem 整体预制体同一口径，
        ///   改 unitSize 的关卡需要美术重做素材，而不是代码乘倍率。
        /// · 素材基准姿态：**角块 = 右下角、边块 = 下边缘**，其余方向靠 <see cref="PieceYaw"/> 绕网格朝上轴旋转。
        ///   旋转是「yaw × 预制体自身朝向」（与 <see cref="FrameItem"/> 同写法），
        ///   这样预制体上让贴图铺平的原始旋转不会被覆盖。
        ///
        /// 可重复调用：每次都先清掉上一批拼接块与封条组，所以编辑器的「重建显示」按钮可以反复点。
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

                    // yaw 乘在预制体自身朝向**外侧**（绕网格朝上轴），预制体让贴图铺平的原始旋转得以保留；
                    // 缩放一个字节都不碰 —— 预制体已按实际尺寸制作。
                    piece.transform.localRotation =
                        Quaternion.Euler(0f, PieceYaw(c, r), 0f) * piece.transform.localRotation;

                    _visualPieces.Add(piece);
                }
            }

            BuildSeals(pg);
        }

        /// <summary>
        /// 该格拼接块绕网格朝上轴的 yaw。**素材基准姿态：角块 = 右下角、边块 = 下边缘**，
        /// 于是每个方向都是一次 90° 的位移，公式与推导（已与用户核对）：
        /// · 「下」= row 增大方向 = world −z（本工程约定 **row 0 = 最上**，见 PixelFillTools 的选格提示；
        ///   <see cref="PiecePrefab"/> 里也是 <c>isBottom = r == rowMax</c>）；
        /// · 「右」= col 增大方向 = world +x；
        /// · Unity 的 <c>Quaternion.Euler(0, +90, 0)</c> 把 +z 转向 +x（即把 −z 转向 −x），据此推得
        ///   **下 0° / 左 90° / 上 180° / 右 270°**；角块基准是「右 + 下」，四个角按
        ///   右下 0° → 左下 90° → 左上 180° → 右上 270° 逐个转 90°。
        ///
        /// 角块的两个方向必须**成对**取下 / 右这条对角线：换另一条对角线等价于整体加 180°（仍是纯旋转），
        /// 但若一角取下、另一个取右，就变成镜像 —— 旋转补不回来，素材会看着歪。
        /// </summary>
        private float PieceYaw(int c, int r)
        {
            bool isLeft = c == colMin;
            bool isRight = c == colMax;
            bool isTop = r == rowMin;
            bool isBottom = r == rowMax;

            // 角块（同时压着一条行边界与一条列边界）：基准姿态是右下角
            if ((isLeft || isRight) && (isTop || isBottom))
            {
                if (isRight)
                    return isBottom ? 0f : 270f;   // 右下（基准）/ 右上
                return isBottom ? 90f : 180f;      // 左下 / 左上
            }

            // 边块：yaw 由「特征朝哪一侧」决定，与角块同一张表
            if (isBottom)
                return 0f;                         // 下边（基准）
            if (isLeft)
                return 90f;                        // 左边
            if (isTop)
                return 180f;                       // 上边
            if (isRight)
                return 270f;                       // 右边

            return 0f;                             // 中心块：无方向
        }

        /// <summary>清掉已拼接的木箱视觉块与封条组（编辑器非 Play 模式下用 DestroyImmediate）。</summary>
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

            ClearSeals();
        }

        // ===== 计数表现：封条 + 钉子 =====

        /// <summary>
        /// 拼计数表现的封条组。两条封条沿木箱的两条**对角线**交叉：
        /// 钉子位置 = 木箱矩形的四角各向箱内偏移 <c>crateSealInset</c>（Pixel 单位），
        /// 封条就架在对角的那两颗钉子上，长度 = 钉子间距 + <c>crateSealExtend</c>。
        ///
        /// 偏移 / 延长 / 高度 / 抬高都按 Pixel 单位（<c>unitSize</c> 的倍数）换算 —— 与角/边/中心拼块同一口径，
        /// 换 unitSize 的关卡不用重调。两个预制体本身不按 unitSize 缩放（钉子完全不动，封条只动 x）。
        /// 预制体缺失只警告、不报错（不影响拆箱逻辑本身）。
        /// </summary>
        private void BuildSeals(PixelGroup pg)
        {
            ClearSeals();

            int count = SealCount;
            if (count <= 0)
                return;

            if (pg.crateSealPrefab == null || pg.crateNailPrefab == null)
            {
                Debug.LogWarning("[CrateItem] 缺少封条 / 钉子预制体，木箱不拼封条表现。" +
                    "请在 PixelGroup 上补 crateSealPrefab / crateNailPrefab。", this);
                return;
            }

            float unit = pg.unitSize;
            float insetX = Mathf.Max(0f, pg.crateSealInset.x) * unit;
            float insetZ = Mathf.Max(0f, pg.crateSealInset.y) * unit;
            float extend = Mathf.Max(0f, pg.crateSealExtend) * unit;
            float height = pg.crateSealHeight * unit;
            float lift = pg.crateSealFirstLift * unit;

            // 木箱矩形（含半格）：左前格中心 z 最大、右后格中心 z 最小
            Vector3 front = pg.GetLocalPosition(colMin, rowMin);
            Vector3 back = pg.GetLocalPosition(colMax, rowMax);
            float halfX = pg.CellSizeX * 0.5f;
            float halfZ = pg.CellSizeZ * 0.5f;

            float xMin = front.x - halfX;
            float xMax = back.x + halfX;
            float zFront = front.z + halfZ;
            float zBack = back.z - halfZ;

            // 四个钉子（局部坐标），各自从所在角向箱内偏移
            var frontLeft = new Vector3(xMin + insetX, height, zFront - insetZ);
            var frontRight = new Vector3(xMax - insetX, height, zFront - insetZ);
            var backLeft = new Vector3(xMin + insetX, height, zBack + insetZ);
            var backRight = new Vector3(xMax - insetX, height, zBack + insetZ);

            // 封条 0（左前 ↔ 右后）先摘，抬 lift 避免两条在交叉点重叠打架；封条 1（右前 ↔ 左后）后摘
            AddSeal(pg, 0, frontLeft, backRight, extend, lift);
            if (count >= 2)
                AddSeal(pg, 1, frontRight, backLeft, extend, 0f);
        }

        /// <summary>
        /// 拼一组「封条 + 两端各一颗钉子」。钉子挂在封条组下，所以摘封条时一起消失。
        ///
        /// 封条朝向：预制体长度轴（默认局部 +X，用 <c>crateSealYawOffset</c> 改）对齐钉子连线，只绕 Y 转；
        /// **只动 x 缩放** —— 在预制体原始 x 缩放上乘「钉子间距 + 延长值」，y / z 原样保留。
        /// <paramref name="extraY"/> 把整组（封条与两颗钉子）抬高一点 —— 给两条封条分个上下，
        /// 免得交叉点重合打架。
        /// </summary>
        private void AddSeal(PixelGroup pg, int index, Vector3 localA, Vector3 localB, float extend, float extraY)
        {
            Vector3 a = pg.transform.TransformPoint(localA);
            Vector3 b = pg.transform.TransformPoint(localB);

            // 抬高一整组而不是只抬封条：钉子跟着封条走，看起来才是「这条封条压在另一条上面」
            if (extraY != 0f)
            {
                a.y += extraY;
                b.y += extraY;
            }

            Vector3 dir = b - a;
            dir.y = 0f;   // 封条只绕 Y 转：两端本就等高，这里也挡掉浮点误差
            if (dir.sqrMagnitude < 1e-8f)
                return;

            var root = new GameObject("Seal_" + index);
            root.transform.SetParent(transform, false);
            root.transform.position = (a + b) * 0.5f;
            root.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up) *
                Quaternion.Euler(0f, -90f + pg.crateSealYawOffset, 0f);

            var strip = PrefabSpawner.Instantiate(pg.crateSealPrefab, root.transform);
            if (strip != null)
            {
                strip.name = "SealStrip_" + index;
                strip.transform.localRotation = Quaternion.identity;

                // x / z 归零（封条以钉子中点为原点、长度沿自身 x），**y 保留预制体的原值**
                Vector3 pos = strip.transform.localPosition;
                strip.transform.localPosition = new Vector3(0f, pos.y, 0f);

                // **只动 x**：在预制体原始缩放上乘「钉子间距 + 延长值」，y / z 原样保留
                // （预制体按 1 世界单位长制作 —— 原始 x 缩放为 1 时长度就是 1 世界单位）
                Vector3 scale = strip.transform.localScale;
                strip.transform.localScale = new Vector3(scale.x * (dir.magnitude + extend), scale.y, scale.z);
            }

            AddNail(pg, root.transform, "NailA_" + index, a);
            AddNail(pg, root.transform, "NailB_" + index, b);

            _sealGroups.Add(root);
        }

        /// <summary>
        /// 钉一颗钉子：x / z 落在算出来的钉子位置（木箱四角向内偏移处），**局部 y 保留预制体的原值**。
        /// 缩放与旋转都不动 —— 世界缩放、朝向与预制体一致。
        /// </summary>
        private void AddNail(PixelGroup pg, Transform parent, string name, Vector3 worldPos)
        {
            var nail = PrefabSpawner.Instantiate(pg.crateNailPrefab, parent);
            if (nail == null)
                return;

            nail.name = name;
            Vector3 local = parent.InverseTransformPoint(worldPos);
            nail.transform.localPosition = new Vector3(local.x, nail.transform.localPosition.y, local.z);
        }

        /// <summary>清掉已拼接的封条组（编辑器非 Play 模式下用 DestroyImmediate）。</summary>
        public void ClearSeals()
        {
            for (int i = 0; i < _sealGroups.Count; i++)
            {
                var seal = _sealGroups[i];
                if (seal == null)
                    continue;
                if (Application.isPlaying)
                    Destroy(seal);
                else
                    DestroyImmediate(seal);
            }
            _sealGroups.Clear();
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
        /// 没计满就按计数表现摘掉一条封条（从先摘的那条开始）；
        /// 计满则本箱转为已拆：立刻不再占格 / 不再盖像素（掩码由 PixelGroup.RefreshCrateState 重建），
        /// 并播放「弹一下再缩小」的消失动画（还剩着的封条跟着本体一起消失）。返回**本次是否刚拆掉**。
        ///
        /// 音效：**扣减后不为 0 → <see cref="hitSoundTag"/>；扣减后为 0（拆掉）→ <see cref="breakSoundTag"/>**。
        /// 两者互斥 —— 拆掉那一次只播破碎音，不会再叠一声命中音。
        /// 封条已经摘光但次数还没减完的情形（见 <see cref="SealCount"/>）照旧走「命中」这一支：
        /// 计数的确被扣减了，与「摘了几条封条」无关。
        /// </summary>
        public bool RegisterAdjacentMoveOut()
        {
            if (destroyed)
                return false;

            movedOutCount++;
            if (movedOutCount < Mathf.Max(1, destroyAfterMoves))
            {
                PlayTag(hitSoundTag);   // 只减数、未拆掉
                RemoveOneSeal();
                return false;
            }

            PlayTag(breakSoundTag);     // 扣减到 0：拆掉本体
            destroyed = true;
            DisappearVisual();
            return true;
        }

        /// <summary>
        /// 按 AudioConfig 的 tag 播一次音效（tag 留空则静默跳过）。写法与 <see cref="IceItem"/> 的融化音效一致。
        /// 同一 tag 只有一个 AudioSource，所以同一次点击碰到多个木箱时会互相打断（听起来仍是一次音）——
        /// 要「每次都完整播」得换 <c>AudioManager.PlayNoInterrupt</c>。
        /// </summary>
        private static void PlayTag(string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return;
            if (AudioManager.Instance != null)
                AudioManager.Instance.Play(tag);
        }

        /// <summary>
        /// 摘掉一条封条：从下标最小的、还活着的那条开始（与 <see cref="BuildSeals"/> 里「封条 0 先摘」一致），
        /// 整个 Seal_i 弹缩消失，钉子随之走。
        /// 封条已摘光但次数还没减完时什么也不做 —— 次数 &gt; 3 的木箱会走到这里（见 <see cref="SealCount"/>）。
        /// </summary>
        private void RemoveOneSeal()
        {
            for (int i = 0; i < _sealGroups.Count; i++)
            {
                var seal = _sealGroups[i];
                if (seal == null)
                    continue;

                _sealGroups.RemoveAt(i);
                RemoveSealVisual(seal);
                return;
            }
        }

        /// <summary>一条封条的消失表现：弹一下再缩小，回调里销毁整个 Seal_i（含两颗钉子）。</summary>
        private void RemoveSealVisual(GameObject seal)
        {
            if (!Application.isPlaying)
            {
                DestroyImmediate(seal);
                return;
            }

            seal.transform.DisappearWithPop(() =>
            {
                if (seal != null)
                    Destroy(seal);
            }, PopDuration, ShrinkDuration, restoreScale: false);
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

            // 还剩着的封条（次数 < 3 的木箱：封条没摘完就要拆本体）跟本体一起消失
            for (int i = 0; i < _sealGroups.Count; i++)
            {
                var seal = _sealGroups[i];
                if (seal != null)
                    RemoveSealVisual(seal);
            }
            _sealGroups.Clear();
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
