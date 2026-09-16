using System.Collections.Generic;
using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 整体描边（单元块）：以 Pixel 网格的交点（Corner）为每个单元块的中心点，
    /// 依据四周四角（左上/右上/左下/右下）Pixel 的暴露与颜色，从 10 张描边贴图（A–J）
    /// 中选一张并绕 Y 旋转，把整个暴露区域轮廓（含颜色分界）连成平滑描边。
    /// 详见 Docs/PixelOutlineDesign.md。
    ///
    /// 用法：建议作为 PixelGroup 的子物体（local 归零），副本的 localPosition 直接等于
    /// 交点的局部坐标；视觉朝向/尺寸由父物体与原子物体（模板）配置。暴露状态变化后需再次调用 Build() 刷新。
    /// </summary>
    public class FrameItem : MonoBehaviour
    {
        [Header("贴图")]
        [Tooltip("10 张描边贴图（需导入为 Sprite (2D and UI)），顺序 A=0 … J=9。J 为「空」，对应交点不生成副本。")]
        public Sprite[] sprites = new Sprite[10];

        [Header("原子物体")]
        [Tooltip("单元块模板（建议 inactive；每个有效单元块会 Instantiate 一份副本并激活）。")]
        public Transform atomicObject;

        [Tooltip("模板上的 SpriteRenderer（副本据此定位并换 Sprite；可位于模板子级）。")]
        public SpriteRenderer atomicSprite;

        [Header("网格")]
        [Tooltip("所属 PixelGroup（留空则在运行时自动向上查找）。")]
        public PixelGroup group;

        /// <summary>已生成的副本（Build 前先清空，避免重复叠加）。</summary>
        [System.NonSerialized] private readonly List<GameObject> _spawned = new List<GameObject>();

        /// <summary>原子 Sprite 相对 atomicObject 的路径（缓存，供副本定位同位置 Renderer）。</summary>
        [System.NonSerialized] private string _atomicSpritePath;

        [System.NonSerialized] private bool _pathCached;

        /// <summary>场景中处于启用状态的 FrameItem 数量；&gt; 0 时 PixelItem 不再显示自身描边（由 FrameItem 统一绘制）。</summary>
        private static int _activeCount;

        /// <summary>是否有 FrameItem 在使用中（PixelItem 据此关闭自身描边）。</summary>
        public static bool InUse => _activeCount > 0;

        // 角索引（顺时针环序）：0=TL,1=TR,2=BR,3=BL；相邻差 1（模 4），对角差 2。
        private const int TL = 0, TR = 1, BR = 2, BL = 3;

        // 贴图下标：A–J
        private const int SPR_A = 0, SPR_B = 1, SPR_C = 2, SPR_D = 3, SPR_E = 4,
                          SPR_F = 5, SPR_G = 6, SPR_H = 7, SPR_I = 8, SPR_J = 9;

        private void OnEnable()
        {
            _activeCount++;
        }

        private void OnDisable()
        {
            _activeCount--;
        }

        private void Start()
        {
            if (group == null)
                group = GetComponentInParent<PixelGroup>();
            Build();
        }

        /// <summary>清空并重建全部单元块副本（暴露状态变化后调用）。未激活时视为未启用，直接跳过。</summary>
        [ContextMenu("Build Frame")]
        public void Build()
        {
            if (!isActiveAndEnabled)
                return;
            if (group == null)
                group = GetComponentInParent<PixelGroup>();
            if (group == null)
            {
                Debug.LogError("[FrameItem] 未找到 PixelGroup，无法构建描边。");
                return;
            }
            if (group.grid == null)
                group.RebuildGrid();
            if (atomicObject == null)
            {
                Debug.LogError("[FrameItem] atomicObject 为空，无法生成描边副本。");
                return;
            }

            Clear();
            CacheAtomicSpritePath();
            atomicObject.gameObject.SetActive(false); // 隐藏模板

            int cols = group.columns;
            int totalRows = group.TotalRows;
            for (int col = 0; col <= cols; col++)
            {
                for (int row = 0; row <= totalRows; row++)
                {
                    int[] c =
                    {
                        Identity(group.GetItem(col - 1, row - 1)), // TL
                        Identity(group.GetItem(col,     row - 1)), // TR
                        Identity(group.GetItem(col,     row)),     // BR
                        Identity(group.GetItem(col - 1, row)),     // BL
                    };

                    if (Resolve(c, out int sprite, out float yaw))
                        Spawn(col, row, sprite, yaw);
                }
            }
        }

        /// <summary>销毁所有已生成的单元块副本。</summary>
        [ContextMenu("Clear Frame")]
        public void Clear()
        {
            for (int i = _spawned.Count - 1; i >= 0; i--)
            {
                var go = _spawned[i];
                if (go == null)
                    continue;
                if (Application.isPlaying)
                    Destroy(go);
                else
                    DestroyImmediate(go);
            }
            _spawned.Clear();
        }

        /// <summary>角身份：暴露 → colorId，未暴露（无像素/未暴露/越界）→ -1。</summary>
        private static int Identity(PixelItem p)
        {
            return (p != null && p.IsExposed) ? p.colorId : -1;
        }

        private void Spawn(int col, int row, int sprite, float yaw)
        {
            var copy = Instantiate(atomicObject, transform, false);
            copy.name = "Frame_" + row + "_" + col;
            copy.localPosition = CornerLocalPosition(col, row);
            // 绕 Y（网格朝上轴）旋转：模板贴图基准姿态与网格环序相差 180°，故在解算角度上再补 180°。
            // 模板自身朝向（如铺平贴图）由 atomicObject.localRotation 提供。
            copy.localRotation = Quaternion.Euler(0f, yaw + 180f, 0f) * atomicObject.localRotation;

            // 编辑器里构建的副本不随场景保存（避免污染场景，运行期由 Build 重新生成）
            if (!Application.isPlaying)
                copy.gameObject.hideFlags = HideFlags.DontSave;

            var sr = ResolveSpriteRenderer(copy);
            if (sr != null && sprite >= 0 && sprite < sprites.Length && sprites[sprite] != null)
                sr.sprite = sprites[sprite];

            copy.gameObject.SetActive(true);
            _spawned.Add(copy.gameObject);
        }

        /// <summary>交点（单元块中心）局部坐标：x=(col−columns/2)·CellSizeX，z=−(row−0.5)·CellSizeZ。</summary>
        private Vector3 CornerLocalPosition(int col, int row)
        {
            float x = (col - group.columns * 0.5f) * group.CellSizeX;
            float z = -(row - 0.5f) * group.CellSizeZ;
            return new Vector3(x, 0f, z);
        }

        private void CacheAtomicSpritePath()
        {
            if (_pathCached)
                return;
            _pathCached = true;
            _atomicSpritePath = string.Empty;
            if (atomicObject == null || atomicSprite == null)
                return;
            _atomicSpritePath = BuildRelativePath(atomicSprite.transform, atomicObject);
        }

        private static string BuildRelativePath(Transform target, Transform root)
        {
            if (target == root)
                return string.Empty;
            if (target == null || target.parent == null)
                return null;
            string parent = BuildRelativePath(target.parent, root);
            if (parent == null)
                return null;
            return string.IsNullOrEmpty(parent) ? target.name : parent + "/" + target.name;
        }

        private SpriteRenderer ResolveSpriteRenderer(Transform copy)
        {
            if (string.IsNullOrEmpty(_atomicSpritePath))
                return copy.GetComponent<SpriteRenderer>();
            var t = copy.Find(_atomicSpritePath);
            return t != null ? t.GetComponent<SpriteRenderer>() : null;
        }

        // ===== 四角状态 → 贴图 + 旋转（完全枚举，见 Docs/PixelOutlineDesign.md §2） =====

        /// <summary>
        /// 四角状态 → (贴图下标, 绕 Y 旋转角度)。c 按 [TL,TR,BR,BL] 环序，值 = colorId（≥0 暴露）或 −1（未暴露）。
        /// 返回 false 表示 J（空），无需生成副本。
        /// </summary>
        public static bool Resolve(int[] c, out int sprite, out float yaw)
        {
            sprite = SPR_J;
            yaw = 0f;

            switch (CountExposed(c))
            {
                case 0: return false;                              // J1 全空
                case 1: return ResolveOne(c, out sprite, out yaw);  // A1
                case 2: return ResolveTwo(c, out sprite, out yaw);
                case 3: return ResolveThree(c, out sprite, out yaw);
                case 4: return ResolveFour(c, out sprite, out yaw);
                default: return false;
            }
        }

        /// <summary>1 暴露：A1，圆角对准唯一暴露角。</summary>
        private static bool ResolveOne(int[] c, out int sprite, out float yaw)
        {
            sprite = SPR_A;
            yaw = FirstExposed(c) * 90f;
            return true;
        }

        /// <summary>2 暴露：对角 → C；相邻同色 → I（I2）；相邻异色 → B。</summary>
        private static bool ResolveTwo(int[] c, out int sprite, out float yaw)
        {
            int a = -1, b = -1;
            for (int i = 0; i < 4; i++)
            {
                if (c[i] < 0) continue;
                if (a < 0) a = i; else b = i;
            }

            if (Mod4(b - a) == 2)
            {
                // C：对角两格暴露（颜色无关）
                sprite = SPR_C;
                yaw = (a == TL || b == TL) ? 0f : 90f; // 含 TL 的对角（TL,BR）→0°，另一对角（TR,BL）→90°
                return true;
            }

            int e = Mod4(b - a) == 1 ? a : b; // 暴露边（a→b 顺时针）
            if (c[a] == c[b])
            {
                sprite = SPR_I; // I2：相邻同色暴露 + 另两格未暴露
                yaw = (e == 0 || e == 2) ? 0f : 90f;
            }
            else
            {
                sprite = SPR_B; // B：相邻异色暴露
                yaw = e * 90f;  // 暴露边对齐到顶边（edge 0）
            }
            return true;
        }

        /// <summary>3 暴露：全同色 → A2；无相邻同色（肘部异色）→ D；恰一条同色边 → G/H。</summary>
        private static bool ResolveThree(int[] c, out int sprite, out float yaw)
        {
            int empty = -1;
            for (int i = 0; i < 4; i++) if (c[i] < 0) empty = i;

            int elbow = Mod4(empty + 2); // 肘部 = 空角对角（暴露角中唯一有两个暴露邻居者）
            int tip1 = Mod4(empty + 1);
            int tip2 = Mod4(empty + 3);

            if (c[elbow] == c[tip1] && c[elbow] == c[tip2])
            {
                sprite = SPR_A; // A2：三格同色暴露，孤立角 = 空角
                yaw = empty * 90f;
                return true;
            }

            if (c[elbow] != c[tip1] && c[elbow] != c[tip2])
            {
                sprite = SPR_D; // D：三格暴露且无相邻同色（肘部异色）
                yaw = elbow * 90f;
                return true;
            }

            // G/H：肘部与一个 tip 同色（同色对），另一 tip 为异色孤立角
            int lone = c[tip1] == c[elbow] ? tip2 : tip1;
            sprite = Mod4(empty - lone) == 1 ? SPR_G : SPR_H;
            yaw = lone * 90f;
            return true;
        }

        /// <summary>4 暴露：全同色 → J2；否则按相邻同色边数分 E / F / I（I1）/ A3。</summary>
        private static bool ResolveFour(int[] c, out int sprite, out float yaw)
        {
            if (c[0] == c[1] && c[1] == c[2] && c[2] == c[3])
            {
                sprite = SPR_J; // J2：四格同色
                yaw = 0f;
                return false;
            }

            bool[] same = new bool[4];
            int sameCount = 0;
            int e0 = -1, e1 = -1;
            for (int i = 0; i < 4; i++)
            {
                same[i] = c[i] == c[(i + 1) % 4];
                if (!same[i]) continue;
                sameCount++;
                if (e0 < 0) e0 = i; else e1 = i;
            }

            if (sameCount == 0)
            {
                sprite = SPR_E; // E：四格暴露且无相邻同色
                yaw = 0f;
                return true;
            }

            if (sameCount == 1)
            {
                sprite = SPR_F; // F：相邻两格同色，其余两格异色
                yaw = Mod4(e0 - 2) * 90f; // 同色边对齐到底边（edge 2）
                return true;
            }

            if (Mod4(e0 - e1) == 2)
            {
                sprite = SPR_I; // I1：四格两两相邻同色（对边同色）
                yaw = (e0 == 0 || e0 == 2) ? 0f : 90f;
                return true;
            }

            // A3：三条同色 + 一条异色，孤立角 = 两条相邻边均异色的角
            int lone = 0;
            for (int i = 0; i < 4; i++)
            {
                if (!same[i] && !same[Mod4(i - 1)]) { lone = i; break; }
            }
            sprite = SPR_A;
            yaw = lone * 90f;
            return true;
        }

        private static int CountExposed(int[] c)
        {
            int n = 0;
            for (int i = 0; i < 4; i++) if (c[i] >= 0) n++;
            return n;
        }

        private static int FirstExposed(int[] c)
        {
            for (int i = 0; i < 4; i++) if (c[i] >= 0) return i;
            return -1;
        }

        private static int Mod4(int x) => ((x % 4) + 4) % 4;
    }
}
