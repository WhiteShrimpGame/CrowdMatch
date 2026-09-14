using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 地面升降台（Elevator）：网格内一块矩形区域，被一个紧贴地面的「推拉门」覆盖，门下是竖井（pit）。
    /// 区域内放的是普通地上像素（不属于升降台），全部被匹配移出后，门向两侧滑开（滑到区域外的部分被地面遮挡，看起来钻到地面下），
    /// 露出竖井里的下一组地下像素；门完全打开后，地下像素升起补位。
    /// 最后一组升起后恢复普通地面、外框消失；非最后一组升起后门再次闭合（成为新一组的「未打开的门」）。
    /// 渲染：通过 HoleMask（写 stencil=1）在真实地面（GroundHole 材质，Comp NotEqual 1）上挖洞。
    /// </summary>
    public class ElevatorItem : MonoBehaviour
    {
        [Header("区域（左上 + 右下）")]
        public int colMin, rowMin, colMax, rowMax;

        [Header("分组（稀疏 / 任意形状）")]
        [Tooltip("每组 = 三元组拍平 [col,row,color, col,row,color, ...]；所有组都在地下竖井里，第 0 组是第一个升起的组。")]
        public List<LevelData.ElevatorGroupData> groups = new List<LevelData.ElevatorGroupData>();

        [Header("地面")]
        [Tooltip("地面高度（PixelGroup 本地 Y）。留 -999 时默认取像素底部 -unitSize/2（门贴地面）。")]
        public float groundY = -999f;

        [Tooltip("真实地面 Renderer（用于交换挖洞材质 + 读取高度）。留空自动按名字找 Block_BG / BG。")]
        public Renderer groundRenderer;

        [Tooltip("挖洞地面材质球（CrowdMatch/GroundHole shader，已配好纹理/颜色/透明裁剪）。留空则不交换，挖洞不生效。")]
        public Material groundHoleMaterial;

        [Header("竖井与动画")]
        [Tooltip("竖井深度（世界单位）；<=0 时自动取 unitSize * 1.5")]
        public float pitDepth = 0f;

        [Tooltip("推拉门滑开时长（秒）")]
        public float doorOpenDuration = 0.3f;

        [Tooltip("门滑回闭合时长（秒）")]
        public float doorCloseDuration = 0.3f;

        [Tooltip("地下像素升起时长（秒）")]
        public float riseDuration = 0.35f;

        [Tooltip("门滑出区域外的额外距离（世界单位，保证门完全让开并钻进地面下）")]
        public float doorSlideOutMargin = 0.4f;

        [Header("视觉颜色（占位）")]
        public Color frameColor = new Color(0.32f, 0.36f, 0.45f, 1f);
        public Color doorColor = new Color(0.42f, 0.46f, 0.55f, 1f);
        public Color pitColor = new Color(0.12f, 0.13f, 0.18f, 1f);

        [Header("调试")]
        public bool debugLog = true;

        // ---- runtime ----
        [System.NonSerialized] public PixelGroup group;

        [System.NonSerialized] private readonly List<List<PixelItem>> _groupPixels = new List<List<PixelItem>>();
        [System.NonSerialized] private readonly List<List<Vector2Int>> _groupCells = new List<List<Vector2Int>>();

        [System.NonSerialized] public readonly List<PixelItem> undergroundPixels = new List<PixelItem>();

        [System.NonSerialized] private int _nextGroupIndex;
        [System.NonSerialized] private bool _done;
        [System.NonSerialized] private bool _animating;

        [Header("视觉引用（prefab 自带结构；动态创建时由 BuildVisual 填充）")]
        [Tooltip("外框容器（4 个子节点：FrameFront/FrameBack/FrameLeft/FrameRight）")]
        public Transform frameRoot;

        [Tooltip("左门板（Plane，横向滑动）")]
        public Transform doorLeft;

        [Tooltip("右门板（Plane，横向滑动）")]
        public Transform doorRight;

        [Tooltip("挖洞蒙版 Renderer（HoleMask 材质，写 stencil）")]
        public Renderer holeMask;

        [Tooltip("竖井容器（5 个子节点：PitFloor/PitFront/PitBack/PitLeft/PitRight）")]
        public GameObject pitRoot;

        private Vector3 _doorLeftClosedPos, _doorRightClosedPos;
        private Vector3 _doorLeftOpenPos, _doorRightOpenPos;

        private readonly Dictionary<Color, Material> _colorMatCache = new Dictionary<Color, Material>();

        public int UndergroundPixelCount => undergroundPixels.Count;
        public bool IsDone => _done;

        public float RegionWidth => (colMax - colMin + 1) * (group != null ? group.CellSizeX : 1f);
        public float RegionDepth => (rowMax - rowMin + 1) * (group != null ? group.CellSizeZ : 1f);

        public Vector3 RegionCenterLocal()
        {
            if (group == null) return Vector3.zero;
            Vector3 a = group.GetLocalPosition(colMin, rowMin);
            Vector3 b = group.GetLocalPosition(colMax, rowMax);
            return (a + b) * 0.5f;
        }

        private float EffectivePitDepth()
        {
            if (pitDepth > 0.0001f) return pitDepth;
            float u = group != null ? group.unitSize : 1f;
            return u * 1.5f;
        }

        private Renderer GroundRenderer
        {
            get
            {
                if (groundRenderer != null) return groundRenderer;
                groundRenderer = FindGroundRenderer();
                return groundRenderer;
            }
        }

        public static Renderer FindGroundRenderer()
        {
            // 优先 Block_BG（真正的脚下地面），找不到再退 BG（背景平面）
            Renderer fallback = null;
            foreach (var r in Object.FindObjectsOfType<Renderer>())
            {
                if (r == null)
                    continue;
                string n = r.name;
                if (n == "Block_BG")
                    return r;
                if (n == "BG" && fallback == null)
                    fallback = r;
            }
            return fallback;
        }

        private float ResolveGroundY()
        {
            if (groundY > -500f) return groundY;
            // 默认把门/框贴到像素底部（像素根部 local y=0，底部 -unitSize/2）。
            // 不再依赖地面 Renderer 的世界高度：场景里的 BG/Block_BG 高度可能与像素不在同一层，导致门框被埋。
            float u = group != null ? group.unitSize : 1f;
            return -u * 0.5f;
        }

        public void BuildVisual(PixelGroup pg, ColorConfig config)
        {
            group = pg;

            // prefab 模式：视觉子节点来自 prefab（字段已配置），不清空，只调整尺寸；
            // 动态模式：字段为空，清空旧视觉后由 BuildXxx 重新创建。
            bool prefabVisual = frameRoot != null || doorLeft != null || doorRight != null || pitRoot != null;
            if (!prefabVisual)
            {
                for (int i = transform.childCount - 1; i >= 0; i--)
                {
                    var child = transform.GetChild(i);
                    if (Application.isPlaying) Destroy(child.gameObject);
                    else DestroyImmediate(child.gameObject);
                }
            }

            _groupPixels.Clear();
            _groupCells.Clear();
            undergroundPixels.Clear();
            _nextGroupIndex = 0;
            _done = false;
            _animating = false;

            float ground = ResolveGroundY();
            float depth = EffectivePitDepth();
            float half = pg.unitSize * 0.5f;

            // 所有组都生成在竖井底部（哨兵坐标，待升起）；区域内的地上像素是普通网格像素，不在这里生成。
            for (int gi = 0; gi < groups.Count; gi++)
            {
                var cells = ParseGroupCells(groups[gi]);
                var pixels = new List<PixelItem>();
                var positions = new List<Vector2Int>();

                for (int i = 0; i < cells.Count; i++)
                {
                    int col = cells[i].col, row = cells[i].row, color = cells[i].color;
                    if (!pg.IsInRange(col, row)) continue;
                    var item = pg.SpawnPixel(col, row, color, config);
                    if (item == null) continue;

                    item.name = "ElevatorPixel_" + row + "_" + col + "_g" + gi;

                    item.gridX = -1;
                    item.gridZ = -1;
                    item.SetClickable(false);
                    Vector3 p = pg.GetLocalPosition(col, row);
                    p.y = ground - depth + half;
                    item.transform.localPosition = p;
                    item.transform.localRotation = Quaternion.identity;
                    undergroundPixels.Add(item);

                    pixels.Add(item);
                    positions.Add(new Vector2Int(col, row));
                }

                _groupPixels.Add(pixels);
                _groupCells.Add(positions);
            }

            _nextGroupIndex = 0;

            // 交换真实地面材质为挖洞材质（仅运行时；幂等）
            EnsureGroundHoleMaterial();

            if (prefabVisual)
            {
                ConfigureFrame();
                ConfigureDoor();
                ConfigureHoleMask();
                ConfigurePit();
            }
            else
            {
                BuildFrame(ground);
                BuildDoor(ground);
                BuildHoleMask(ground);
                BuildPit(ground, depth);
            }
        }

        /// <summary>
        /// 仅配置视觉尺寸（不生成地下像素），供编辑器在创建升降台后预览 prefab 视觉。
        /// 仅对 prefab 模式生效；动态模式（无视觉字段）保持 gizmo 预览。
        /// </summary>
        public void ConfigureVisualOnly(PixelGroup pg)
        {
            group = pg;
            bool prefabVisual = frameRoot != null || doorLeft != null || doorRight != null || pitRoot != null;
            if (!prefabVisual) return;

            ConfigureFrame();
            ConfigureDoor();
            ConfigureHoleMask();
            ConfigurePit();
        }

        private List<(int col, int row, int color)> ParseGroupCells(LevelData.ElevatorGroupData g)
        {
            var list = new List<(int, int, int)>();
            if (g == null || g.cells == null) return list;
            for (int i = 0; i + 2 < g.cells.Length; i += 3)
                list.Add((g.cells[i], g.cells[i + 1], g.cells[i + 2]));
            return list;
        }

        private void EnsureGroundHoleMaterial()
        {
            if (!Application.isPlaying) return;   // 仅运行时交换，避免污染 prefab / 材质资产
            var r = GroundRenderer;
            if (r == null)
            {
                if (debugLog) Debug.LogWarning("[Elevator] 未找到真实地面 Renderer，无法挖洞。请设置 groundRenderer 或 groundY。", this);
                return;
            }
            if (groundHoleMaterial == null)
            {
                if (debugLog) Debug.LogWarning("[Elevator] 未设置 groundHoleMaterial（GroundHole 材质球），地面挖洞不生效。", this);
                return;
            }
            if (r.sharedMaterial == groundHoleMaterial)
                return;   // 已交换
            r.sharedMaterial = groundHoleMaterial;
        }

        private void BuildFrame(float ground)
        {
            float w = RegionWidth;
            float d = RegionDepth;
            Vector3 c = RegionCenterLocal();
            float t = 0.08f;
            float h = 0.12f;

            var root = new GameObject("Frame").transform;
            root.SetParent(transform, false);
            root.localPosition = Vector3.zero;
            frameRoot = root;

            // 外框四边（内边对齐区域边界，向区域外偏移 t/2）
            AddBox(root, "FrameFront", new Vector3(c.x, ground + h * 0.5f, c.z + d * 0.5f + t * 0.5f), new Vector3(w + t * 2f, h, t), frameColor);
            AddBox(root, "FrameBack",  new Vector3(c.x, ground + h * 0.5f, c.z - d * 0.5f - t * 0.5f), new Vector3(w + t * 2f, h, t), frameColor);
            AddBox(root, "FrameLeft",  new Vector3(c.x - w * 0.5f - t * 0.5f, ground + h * 0.5f, c.z), new Vector3(t, h, d + t * 2f), frameColor);
            AddBox(root, "FrameRight", new Vector3(c.x + w * 0.5f + t * 0.5f, ground + h * 0.5f, c.z), new Vector3(t, h, d + t * 2f), frameColor);
        }

        private void BuildDoor(float ground)
        {
            float w = RegionWidth;
            float d = RegionDepth;
            Vector3 c = RegionCenterLocal();
            float halfW = w * 0.5f + 0.02f;   // 中间略重叠，避免缝隙
            float y = ground - 0.01f;

            var left = AddQuad("DoorLeft", new Vector3(c.x - halfW * 0.5f, y, c.z), halfW, d, doorColor);
            var right = AddQuad("DoorRight", new Vector3(c.x + halfW * 0.5f, y, c.z), halfW, d, doorColor);

            doorLeft = left.transform;
            doorRight = right.transform;
            _doorLeftClosedPos = doorLeft.localPosition;
            _doorRightClosedPos = doorRight.localPosition;
            float slide = halfW + doorSlideOutMargin;
            _doorLeftOpenPos = _doorLeftClosedPos + new Vector3(-slide, 0f, 0f);
            _doorRightOpenPos = _doorRightClosedPos + new Vector3(slide, 0f, 0f);
        }

        private void BuildHoleMask(float ground)
        {
            float w = RegionWidth;
            float d = RegionDepth;
            Vector3 c = RegionCenterLocal();
            var shader = Shader.Find("CrowdMatch/HoleMask");
            if (shader == null)
            {
                if (debugLog) Debug.LogWarning("[Elevator] 未找到 HoleMask shader，跳过挖洞蒙版。", this);
                return;
            }
            var mat = new Material(shader);
            var go = AddQuad("HoleMask", new Vector3(c.x, ground + 0.02f, c.z), w, d, Color.white, mat);
            holeMask = go.GetComponent<Renderer>();
        }

        private void BuildPit(float ground, float depth)
        {
            float w = RegionWidth;
            float d = RegionDepth;
            Vector3 c = RegionCenterLocal();
            float t = 0.06f;

            var root = new GameObject("Pit").transform;
            root.SetParent(transform, false);
            root.localPosition = Vector3.zero;
            pitRoot = root.gameObject;

            AddBox(root, "PitFloor", new Vector3(c.x, ground - depth, c.z), new Vector3(w, t, d), pitColor);
            AddBox(root, "PitFront", new Vector3(c.x, ground - depth * 0.5f, c.z + d * 0.5f), new Vector3(w, depth, t), pitColor);
            AddBox(root, "PitBack",  new Vector3(c.x, ground - depth * 0.5f, c.z - d * 0.5f), new Vector3(w, depth, t), pitColor);
            AddBox(root, "PitLeft",  new Vector3(c.x - w * 0.5f, ground - depth * 0.5f, c.z), new Vector3(t, depth, d), pitColor);
            AddBox(root, "PitRight", new Vector3(c.x + w * 0.5f, ground - depth * 0.5f, c.z), new Vector3(t, depth, d), pitColor);
        }

        private void ConfigureFrame()
        {
            float w = RegionWidth;
            float d = RegionDepth;
            Vector3 c = RegionCenterLocal();
            float t = 0.08f;

            // 只覆盖水平（x/z）；垂直（y 位置与厚度）沿用 prefab 摆好的值
            SetBoxHorizontal(frameRoot != null ? frameRoot.Find("FrameFront") : null,
                new Vector3(c.x, 0f, c.z + d * 0.5f + t * 0.5f), w + t * 2f, t);
            SetBoxHorizontal(frameRoot != null ? frameRoot.Find("FrameBack") : null,
                new Vector3(c.x, 0f, c.z - d * 0.5f - t * 0.5f), w + t * 2f, t);
            SetBoxHorizontal(frameRoot != null ? frameRoot.Find("FrameLeft") : null,
                new Vector3(c.x - w * 0.5f - t * 0.5f, 0f, c.z), t, d + t * 2f);
            SetBoxHorizontal(frameRoot != null ? frameRoot.Find("FrameRight") : null,
                new Vector3(c.x + w * 0.5f + t * 0.5f, 0f, c.z), t, d + t * 2f);
        }

        private void ConfigureDoor()
        {
            float w = RegionWidth;
            float d = RegionDepth;
            Vector3 c = RegionCenterLocal();
            float halfW = w * 0.5f + 0.02f;   // 中间略重叠，避免缝隙

            SetQuadHorizontal(doorLeft, new Vector3(c.x - halfW * 0.5f, 0f, c.z), halfW, d);
            SetQuadHorizontal(doorRight, new Vector3(c.x + halfW * 0.5f, 0f, c.z), halfW, d);

            _doorLeftClosedPos = doorLeft != null ? doorLeft.localPosition : Vector3.zero;
            _doorRightClosedPos = doorRight != null ? doorRight.localPosition : Vector3.zero;
            float slide = halfW + doorSlideOutMargin;
            _doorLeftOpenPos = _doorLeftClosedPos + new Vector3(-slide, 0f, 0f);
            _doorRightOpenPos = _doorRightClosedPos + new Vector3(slide, 0f, 0f);
        }

        private void ConfigureHoleMask()
        {
            if (holeMask == null)
            {
                BuildHoleMask(ResolveGroundY());
                return;
            }
            float w = RegionWidth;
            float d = RegionDepth;
            Vector3 c = RegionCenterLocal();
            SetQuadHorizontal(holeMask.transform, new Vector3(c.x, 0f, c.z), w, d);
        }

        private void ConfigurePit()
        {
            float w = RegionWidth;
            float d = RegionDepth;
            Vector3 c = RegionCenterLocal();
            float t = 0.06f;

            Transform pit = pitRoot != null ? pitRoot.transform : null;
            SetBoxHorizontal(pit != null ? pit.Find("PitFloor") : null, new Vector3(c.x, 0f, c.z), w, d);
            SetBoxHorizontal(pit != null ? pit.Find("PitFront") : null, new Vector3(c.x, 0f, c.z + d * 0.5f), w, t);
            SetBoxHorizontal(pit != null ? pit.Find("PitBack") : null, new Vector3(c.x, 0f, c.z - d * 0.5f), w, t);
            SetBoxHorizontal(pit != null ? pit.Find("PitLeft") : null, new Vector3(c.x - w * 0.5f, 0f, c.z), t, d);
            SetBoxHorizontal(pit != null ? pit.Find("PitRight") : null, new Vector3(c.x + w * 0.5f, 0f, c.z), t, d);
        }

        private void SetBoxHorizontal(Transform t, Vector3 localPos, float scaleX, float scaleZ)
        {
            if (t == null) return;
            Vector3 p = t.localPosition;
            p.x = localPos.x;
            p.z = localPos.z;
            t.localPosition = p;
            Vector3 s = t.localScale;
            s.x = scaleX;
            s.z = scaleZ;
            t.localScale = s;
        }

        private void SetQuadHorizontal(Transform t, Vector3 localPos, float width, float depth)
        {
            if (t == null) return;
            Vector3 p = t.localPosition;
            p.x = localPos.x;
            p.z = localPos.z;
            t.localPosition = p;
            Vector3 s = t.localScale;
            s.x = width / 10f;
            s.z = depth / 10f;
            t.localScale = s;
        }

        private GameObject AddBox(Transform parent, string name, Vector3 localPos, Vector3 localScale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            var col = go.GetComponent<Collider>();
            if (col != null) DestroyComponent(col);
            go.GetComponent<Renderer>().sharedMaterial = GetColorMaterial(color, "Standard");
            return go;
        }

        private GameObject AddQuad(string name, Vector3 localPos, float width, float depth, Color color, Material mat = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.name = name;
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = new Vector3(width / 10f, 1f, depth / 10f);
            var col = go.GetComponent<Collider>();
            if (col != null) DestroyComponent(col);
            go.GetComponent<Renderer>().sharedMaterial = mat != null ? mat : GetColorMaterial(color, "Standard");
            return go;
        }

        private Material GetColorMaterial(Color c, string shader)
        {
            if (_colorMatCache.TryGetValue(c, out var m) && m != null) return m;
            m = new Material(Shader.Find(shader));
            m.color = c;
            _colorMatCache[c] = m;
            return m;
        }

        private void DestroyComponent(Object obj)
        {
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }

        /// <summary>区域内的地上像素全部清空时开门并升起下一组；返回是否触发了推进。</summary>
        public bool TryAdvance()
        {
            if (_done || _animating || group == null) return false;
            if (group.grid == null) return false;

            // 触发条件：整个区域（colMin..colMax × rowMin..rowMax）内的地上像素全被匹配移出。
            // 初始时这些是「不属于升降台」的普通地上像素；每组升起后它们替换为该组像素，继续等待被清空。
            for (int r = rowMin; r <= rowMax; r++)
            {
                for (int c = colMin; c <= colMax; c++)
                {
                    if (!group.IsInRange(c, r)) continue;
                    if (group.grid[c, r] != null)
                        return false;   // 区域仍有地上像素，继续等待
                }
            }

            if (_nextGroupIndex >= groups.Count)
            {
                FinalizeLast();
                return true;
            }

            if (debugLog)
                Debug.Log("[Elevator] " + name + " 区域清空，开门并升起第 " + _nextGroupIndex + " 组。", this);

            _animating = true;
            group.OnElevatorAdvanceStarted(this);
            StartCoroutine(OpenAndRiseRoutine());
            return true;
        }

        private IEnumerator OpenAndRiseRoutine()
        {
            OpenDoor();
            if (doorOpenDuration > 0f)
                yield return new WaitForSeconds(doorOpenDuration);

            yield return RiseGroupRoutine();

            if (_nextGroupIndex >= groups.Count)
            {
                yield return new WaitForSeconds(0.05f);
                FinalizeLast();
            }
            else
            {
                CloseDoor();
                if (doorCloseDuration > 0f)
                    yield return new WaitForSeconds(doorCloseDuration);
            }

            _animating = false;
            if (group != null)
                group.OnElevatorAdvanceFinished(this);
        }

        private IEnumerator RiseGroupRoutine()
        {
            var pixels = _groupPixels[_nextGroupIndex];
            var cells = _groupCells[_nextGroupIndex];

            for (int i = 0; i < pixels.Count; i++)
            {
                var pixel = pixels[i];
                var cell = cells[i];
                if (pixel == null) continue;

                pixel.gridX = cell.x;
                pixel.gridZ = cell.y;
                pixel.group = group;
                if (group.grid != null && group.IsInRange(cell.x, cell.y))
                    group.grid[cell.x, cell.y] = pixel;
                pixel.SetClickable(false);
                pixel.placing = true;

                Vector3 target = group.GetLocalPosition(cell.x, cell.y);
                pixel.transform.DOLocalMoveY(target.y, riseDuration).SetEase(Ease.OutQuad);
            }

            if (riseDuration > 0f)
                yield return new WaitForSeconds(riseDuration);

            for (int i = 0; i < pixels.Count; i++)
            {
                var pixel = pixels[i];
                if (pixel == null || group == null || group.grid == null) continue;
                if (!group.IsInRange(pixel.gridX, pixel.gridZ) || group.grid[pixel.gridX, pixel.gridZ] != pixel)
                    continue;
                pixel.MarkPlaced();
                pixel.SetClickable(true);
            }

            _nextGroupIndex++;

            if (group != null)
                group.RefreshExposed();
        }

        private void OpenDoor()
        {
            if (doorLeft != null) doorLeft.DOLocalMove(_doorLeftOpenPos, doorOpenDuration).SetEase(Ease.InOutQuad);
            if (doorRight != null) doorRight.DOLocalMove(_doorRightOpenPos, doorOpenDuration).SetEase(Ease.InOutQuad);
        }

        private void CloseDoor()
        {
            if (doorLeft != null) doorLeft.DOLocalMove(_doorLeftClosedPos, doorCloseDuration).SetEase(Ease.InOutQuad);
            if (doorRight != null) doorRight.DOLocalMove(_doorRightClosedPos, doorCloseDuration).SetEase(Ease.InOutQuad);
        }

        private void FinalizeLast()
        {
            if (_done) return;
            _done = true;

            if (debugLog)
                Debug.Log("[Elevator] " + name + " 最后一组已升起，恢复地面、外框消失。", this);

            if (holeMask != null)
                holeMask.enabled = false;

            // prefab 化后不再销毁视觉子节点，仅隐藏，便于复用与还原。
            if (doorLeft != null) { doorLeft.DOKill(); doorLeft.gameObject.SetActive(false); }
            if (doorRight != null) { doorRight.DOKill(); doorRight.gameObject.SetActive(false); }
            if (frameRoot != null) frameRoot.gameObject.SetActive(false);
            if (pitRoot != null) pitRoot.SetActive(false);
        }

        private void OnDrawGizmosSelected()
        {
            var pg = group != null ? group : GetComponentInParent<PixelGroup>();
            if (pg == null) return;

            Vector3 a = pg.GetLocalPosition(colMin, rowMin);
            Vector3 b = pg.GetLocalPosition(colMax, rowMax);
            Vector3 center = (a + b) * 0.5f;
            Vector3 size = new Vector3(
                Mathf.Abs(b.x - a.x) + pg.CellSizeX,
                0.2f,
                Mathf.Abs(b.z - a.z) + pg.CellSizeZ);

            Color old = Gizmos.color;
            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.6f);
            Matrix4x4 m = Gizmos.matrix;
            Gizmos.matrix = pg.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(center, size);
            Gizmos.matrix = m;
            Gizmos.color = old;
        }
    }
}
