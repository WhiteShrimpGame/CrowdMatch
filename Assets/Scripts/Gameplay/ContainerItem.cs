using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CrowdMatch
{
    /// <summary>
    /// 容器单位：配置颜色 ID 与容量，挂载一个 UI Text 显示剩余容量。
    /// 由 ContainerGroup 生成；gridX / gridZ 记录网格坐标。
    /// </summary>
    public class ContainerItem : MonoBehaviour
    {
        [Tooltip("容器接受的颜色 ID")]
        public int colorId;

        [Tooltip("总容量（可容纳的 PixelItem 数量）")]
        public int capacity = 1;

        [Tooltip("显示容量的 UI Text，留空自动从子物体查找")]
        public Text capacityText;

        [Tooltip("网格列坐标（横，X 方向）")]
        public int gridX;

        [Tooltip("网格行坐标（纵，Z 方向），0 为最前排，越大越靠后")]
        public int gridZ;

        [Header("小车出库轴（可选）")]
        [Tooltip("前轴（空子物体，出车时的驱动轴）")]
        public Transform frontAxle;

        [Tooltip("后轴（空子物体，倒车时的驱动轴）")]
        public Transform rearAxle;

        [Tooltip("倒车缩放轴（空子物体，倒车/出车时置于轴与车体之间，X 缩放用于惯性夸张）")]
        public Transform reverseScaleAxle;

        [Tooltip("侧翻自转轴（空子物体，最深层节点，位于缩放轴与车体之间，绕前进轴旋转做惯性侧翻）")]
        public Transform rollAxle;

        [Tooltip("弹性缩放轴（空子物体，上车弹性缩放与出车侧翻归 0 后弹性缩放共用的 pivot；不配置则直接缩放车身）")]
        public Transform elasticScaleAxle;

        [Tooltip("盖子（可选，前排打开时直接隐藏；后排在前方全部找全匹配对象时播放 DisappearWithPop 消失动画）")]
        public Transform lidTransform;

        /// <summary>盖子是否已打开（隐藏）。运行时赋值，不序列化。</summary>
        [System.NonSerialized] public bool lidOpened;

        /// <summary>是否正在补位移动（Row 间 lerp）中。移动中禁止匹配与出库，避免与补位动画冲突。</summary>
        [System.NonSerialized] public bool isRefilling;

        /// <summary>所属 ContainerGroup（运行时赋值，不序列化）</summary>
        [System.NonSerialized] public ContainerGroup group;

        [Header("上车表现 / Boarding")]
        [Tooltip("上车落点列表（空子物体）。像素上车时选中一个空闲落点作为父物体，DOLocalJump 到 0 点")]
        public List<Transform> posList = new List<Transform>();

        [Tooltip("上车 LocalJump 高度（米）")]
        public float boardJumpPower = 0.6f;

        [Tooltip("上车 LocalJump 弹跳次数")]
        public int boardJumpCount = 1;

        [Tooltip("上车 LocalJump 时长（秒）")]
        public float boardJumpDuration = 0.35f;

        [Tooltip("上车后弹性缩放最终值（Vector3，从 (1,1,1) 匀减速缩放到该值；1=不变）")]
        public Vector3 boardElasticTargetScale = new Vector3(1.15f, 0.85f, 1.15f);

        [Tooltip("上车弹性缩放到位时长（秒，匀减速）")]
        public float boardElasticScaleDuration = 0.1f;

        [Tooltip("上车弹性缩放复原时长（秒，匀加速回到 1）")]
        public float boardElasticRecoverDuration = 0.15f;

        [Header("补位侧倾 / Refill Roll")]
        [Tooltip("补位侧倾最大角度（度，绕 roll 轴前进轴侧倾的固定最大角）")]
        public float refillRollMaxAngle = 10f;

        [Tooltip("补位侧倾到位时长（秒，匀减速 0→最大角）")]
        public float refillRollOutDuration = 0.3f;

        [Tooltip("补位侧倾回正时长（秒，匀加速 最大角→0）")]
        public float refillRollRecoverDuration = 0.4f;

        [Header("材质替换")]
        [Tooltip("按配置替换容器上指定 Renderer 的材质；留空则回退到现有逻辑（用 colorId 给首个 Renderer 上色）")]
        public List<MaterialReplacement> materialReplacements = new List<MaterialReplacement>();

        /// <summary>车材质类型：标识替换时使用车体材质还是车内部材质。</summary>
        public enum ContainerMaterialType
        {
            Car,      // 车材质
            Interior  // 车内部材质
        }

        [System.Serializable]
        public class MaterialReplacement
        {
            [Tooltip("需要替换材质的 Renderer（指向本预制体上的 Renderer，实例化后自动重映射到实例）")]
            public Renderer renderer;

            [Tooltip("要替换的材质槽位下标（Renderer.materials 数组的 index），颜色仍按 colorId 取")]
            public int materialSlotIndex;

            [Tooltip("使用哪组材质替换：车材质 或 车内部材质")]
            public ContainerMaterialType materialType = ContainerMaterialType.Car;
        }

        private Renderer _renderer;
        private int _remaining;

        private readonly HashSet<Transform> _occupiedPos = new HashSet<Transform>();
        private int _elasticPhase;                 // 0=空闲，1=放大中（未到最大值），2=复原中（最大值→1）
        private Coroutine _elasticRoutine;
        private Transform _elasticAxleParent;      // 换轴时记录车身原始父物体
        private bool _elasticAxleSwapped;          // 车身是否已挂到弹性轴下

        private readonly HashSet<PixelItem> _boardingPixels = new HashSet<PixelItem>();   // 上车跳跃中（未落定）的像素

        private int _rollPhase;                   // 0=空闲，1=侧倾中（0→最大角），2=回正中（最大角→0）
        private float _rollAngle;                 // 当前侧倾角（度）
        private float _rollOutDur;                // 当前侧倾到位时长（重扩时缩短）
        private float _rollTimer;                 // 当前阶段已过时间
        private bool _rollMoveDone;               // 当前补位移动是否已完成
        private bool _rollMoving;                 // 补位移动进行中
        private bool _rollMovePending;            // 起点/时长待换轴后按实际位置计算
        private float _rollMoveSpeed;             // 补位移动速度（ContainerGroup 传入）
        private Vector3 _rollMoveStart;           // roll 轴在 ContainerGroup 空间的移动起点
        private Vector3 _rollMoveTarget;          // roll 轴在 ContainerGroup 空间的移动终点
        private float _rollMoveDuration;
        private float _rollMoveTimer;
        private Coroutine _rollRoutine;
        private Action _rollOnComplete;
        private Transform _rollAxleParent;        // 换轴时记录车身原始父物体
        private Vector3 _rollAxleLocalOffset;     // roll 轴在车身局部空间的偏移（换轴前记录）
        private bool _rollAxleSwapped;            // 车身是否已挂到 roll 轴下

        /// <summary>剩余容量</summary>
        public int Remaining => _remaining;

        public bool IsEmpty => _remaining <= 0;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _remaining = capacity;
            if (capacityText == null)
                capacityText = GetComponentInChildren<Text>();
            ApplyMaterial();
            UpdateText();
        }

        /// <summary>设置容量（编辑器与运行时都可用），并刷新显示</summary>
        public void SetCapacity(int cap)
        {
            capacity = cap;
            _remaining = cap;
            UpdateText();
        }

        /// <summary>消耗 1 点容量，返回是否耗尽</summary>
        public bool Consume()
        {
            _remaining--;
            UpdateText();
            return _remaining <= 0;
        }

        public void UpdateText()
        {
            if (capacityText == null)
                capacityText = GetComponentInChildren<Text>();
            if (capacityText != null)
                capacityText.text = _remaining.ToString();
        }

        /// <summary>直接隐藏盖子（初始就在第一排的小车使用）。</summary>
        public void HideLid()
        {
            lidOpened = true;
            if (lidTransform != null)
                lidTransform.gameObject.SetActive(false);
        }

        /// <summary>播放开盖动画（后排小车满足「前方全部找全匹配对象」时使用），幂等：只播放一次。</summary>
        public void OpenLid()
        {
            if (lidOpened)
                return;
            lidOpened = true;
            if (lidTransform == null || !lidTransform.gameObject.activeSelf)
                return;
            lidTransform.DisappearWithPop(() =>
            {
                if (lidTransform != null)
                    lidTransform.gameObject.SetActive(false);
            });
        }

        /// <summary>
        /// 按 colorId 应用材质，config 为空时从 GameManager 获取。
        /// 正常路径：按 materialReplacements 逐项替换（materialType 决定取车材质还是车内部材质）；
        /// 无任何替换项时回退旧逻辑：用基础材质给首个 Renderer 整车上色。
        /// </summary>
        public void ApplyMaterial(ColorConfig config = null)
        {
            if (config == null)
            {
                if (GameManager.Instance != null)
                    config = GameManager.Instance.colorConfig;
            }
            if (config == null)
                return;

            // 回退：未配置任何替换项时，用基础材质给首个 Renderer 整车上色（旧逻辑）
            if (materialReplacements == null || materialReplacements.Count == 0)
            {
                var mat = config.GetMaterial(colorId);
                if (_renderer == null)
                    _renderer = GetComponent<Renderer>();
                if (_renderer != null && mat != null)
                    _renderer.sharedMaterial = mat;
                return;
            }

            // 正常路径：按配置替换指定 Renderer 的指定材质槽位（颜色仍用 colorId；materialType 决定取车材质还是车内部材质）
            foreach (var rep in materialReplacements)
            {
                if (rep == null || rep.renderer == null)
                    continue;
                var repMat = rep.materialType == ContainerMaterialType.Interior
                    ? config.GetInteriorMaterial(colorId)
                    : config.GetCarMaterial(colorId);
                if (repMat == null)
                    continue;   // 该组未配置此颜色，跳过
                ApplyToMaterialSlot(rep.renderer, rep.materialSlotIndex, repMat);
            }
        }

        /// <summary>把 renderer 的 materials 数组里 index 下标处替换为 mat（越界则忽略）。</summary>
        private static void ApplyToMaterialSlot(Renderer renderer, int index, Material mat)
        {
            var mats = renderer.sharedMaterials;
            if (mats == null || index < 0 || index >= mats.Length)
                return;
            mats[index] = mat;
            renderer.sharedMaterials = mats;
        }

        // ===== 上车表现 / Boarding =====

        /// <summary>
        /// 尝试上车：选一个空闲落点、DOLocalJump 到 0 点、随后触发弹性缩放。
        /// 无空闲落点返回 false（调用方回退旧 Lerp）。onLastComplete 在「最后一个上车像素的弹性归位」后回调（供出库用）。
        /// </summary>
        public bool TryBoardPixel(PixelItem pixel, Action onLastComplete)
        {
            if (pixel == null)
                return false;
            var pos = AcquireFreePos();
            if (pos == null)
                return false;   // 无空闲落点，回退旧处理
            _boardingPixels.Add(pixel);   // 上车中：侧倾时锁定其世界角度
            StartCoroutine(BoardRoutine(pixel, pos, onLastComplete));
            return true;
        }

        private Transform AcquireFreePos()
        {
            for (int i = 0; i < posList.Count; i++)
            {
                var p = posList[i];
                if (p == null || _occupiedPos.Contains(p))
                    continue;
                _occupiedPos.Add(p);
                return p;
            }
            return null;
        }

        private IEnumerator BoardRoutine(PixelItem pixel, Transform pos, Action onLastComplete)
        {
            pixel.transform.SetParent(pos, true);   // 挂到落点下，保持世界位姿（无瞬移）

            // 跳跃与转向并行：DOLocalJump 落到 0 点，同时 localRotation 平滑归 0（各自独立 tween，同时长）
            var jumpTween = pixel.transform.DOLocalJump(Vector3.zero, boardJumpPower, boardJumpCount, boardJumpDuration);
            var rotateTween = pixel.transform.DOLocalRotate(Vector3.zero, boardJumpDuration);
            yield return jumpTween.WaitForCompletion();
            yield return rotateTween.WaitForCompletion();

            if (pixel != null)
            {
                GameData.ClearedPixelCount++;
                pixel.transform.localPosition = Vector3.zero;   // 落定在落点上：不销毁，保留为乘客
                _boardingPixels.Remove(pixel);   // 已落定，成为乘客，随车侧倾
            }
            // 落点不释放：该座位被该像素永久占用，直到整辆车出库销毁时一并带走

            PlayBoardElastic();                 // 触发弹性（叠加规则见 PlayBoardElastic）
            yield return WaitForElasticIdle();  // 等弹性归位

            onLastComplete?.Invoke();
        }

        /// <summary>弹性缩放实际作用的 scale（有弹性轴读/写弹性轴，否则退回车身 localScale）。</summary>
        private Vector3 ElasticScale
        {
            get => elasticScaleAxle != null ? elasticScaleAxle.localScale : transform.localScale;
            set
            {
                if (elasticScaleAxle != null)
                    elasticScaleAxle.localScale = value;
                else
                    transform.localScale = value;
            }
        }

        /// <summary>换轴（幂等）：弹性轴脱离车身挂到原始父物体并重置 scale，车身挂到弹性轴下（世界位姿保持，无瞬移）。</summary>
        private void SwapElasticAxle()
        {
            if (_elasticAxleSwapped)
                return;
            var elastic = elasticScaleAxle;
            if (elastic == null)
                return;

            _elasticAxleParent = transform.parent;
            elastic.SetParent(_elasticAxleParent, true);   // 弹性轴脱离车身 → 原始父物体
            elastic.localScale = Vector3.one;              // 纯 pivot，重置 scale
            transform.SetParent(elastic, true);            // 车身挂到弹性轴下
            _elasticAxleSwapped = true;
        }

        /// <summary>收起轴（幂等）：车身脱离弹性轴回原始父物体，弹性轴还给车身并重置 scale。</summary>
        private void RestoreElasticAxle()
        {
            if (!_elasticAxleSwapped)
                return;
            var elastic = elasticScaleAxle;
            if (elastic == null)
            {
                _elasticAxleSwapped = false;
                return;
            }

            elastic.localScale = Vector3.one;              // 复原
            transform.SetParent(_elasticAxleParent, true); // 车身脱离弹性轴回原始父物体
            elastic.SetParent(transform, true);            // 弹性轴还给车身
            elastic.localScale = Vector3.one;
            _elasticAxleSwapped = false;
        }

        /// <summary>
        /// 触发上车弹性缩放（叠加规则）：放大中（未到最大值）忽略新动画；
        /// 复原中（最大值→1）则中断上一动画，自当前 scale 再扩大至最大值再弹回。
        /// 复原中重扩时，按 √(剩余距离比) 缩短重扩时长，使重扩加速度与初始态直接 ease-out 一致（即从中间逐帧还原直接态动画）。
        /// </summary>
        private void PlayBoardElastic()
        {
            if (_elasticPhase == 1)
                return;   // 尚未到最大值：忽略新动画
            if (_elasticPhase == 2)
            {
                if (_elasticRoutine != null)
                    StopCoroutine(_elasticRoutine);
                _elasticRoutine = StartCoroutine(ElasticRoutine(ElasticScale, ReexpandDuration()));
                return;
            }
            SwapElasticAxle();   // 先换轴再缩放
            _elasticRoutine = StartCoroutine(ElasticRoutine(Vector3.one, boardElasticScaleDuration));
        }

        /// <summary>
        /// 复原中重扩的时长：使重扩加速度与初始态直接 ease-out 一致（即从中间逐帧还原直接态动画）。
        /// 复原进度 p_r ∈ [0,1]（0=在最大值 M，1=已归 1），剩余距离比 |M-S|/|M-1| = p_r²；
        /// 直接态动画在 scale = S 处进度 p_e = 1 - p_r，剩余时长为 T·p_r。故取 T' = T·√(p_r²) = T·p_r。
        /// </summary>
        private float ReexpandDuration()
        {
            Vector3 target = boardElasticTargetScale;
            Vector3 cur = ElasticScale;
            float remainRatio = 0f;   // p_r² = |M-S|/|M-1|（剩余距离比）
            for (int i = 0; i < 3; i++)
            {
                float den = Mathf.Abs(target[i] - 1f);
                if (den < 1e-4f)
                    continue;
                remainRatio = Mathf.Max(remainRatio, Mathf.Abs(target[i] - cur[i]) / den);
            }
            remainRatio = Mathf.Clamp01(remainRatio);
            return Mathf.Sqrt(remainRatio) * boardElasticScaleDuration;
        }

        private IEnumerator ElasticRoutine(Vector3 fromScale, float expandDuration)
        {
            _elasticPhase = 1;
            float t = 0f;
            float dur = Mathf.Max(expandDuration, 0.0001f);   // 防止除零（重扩时剩余距离为 0）
            while (t < dur)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / dur);
                float e = 1f - (1f - p) * (1f - p);   // 匀减速 ease-out
                ElasticScale = Vector3.Lerp(fromScale, boardElasticTargetScale, e);
                yield return null;
            }
            ElasticScale = boardElasticTargetScale;

            _elasticPhase = 2;
            t = 0f;
            while (t < boardElasticRecoverDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / boardElasticRecoverDuration);
                float e = p * p;   // 匀加速 ease-in
                ElasticScale = Vector3.Lerp(boardElasticTargetScale, Vector3.one, e);
                yield return null;
            }
            ElasticScale = Vector3.one;

            RestoreElasticAxle();   // 完成缩放后收起轴

            _elasticPhase = 0;
            _elasticRoutine = null;
        }

        private IEnumerator WaitForElasticIdle()
        {
            while (_elasticPhase != 0)
                yield return null;
        }

        // ===== 补位侧倾 / Refill Roll =====

        /// <summary>
        /// 尝试启动补位侧倾：复用 roll 轴，先换轴，再把「移动 + 侧倾 + 上车像素锁定」交给内部协程。
        /// 侧倾 0→最大角匀减速；侧倾到最大角且完成补位移动后才匀加速归 0；
        /// 移动先完成则等侧倾到最大角再回正，侧倾先到最大角则保持最大角到移动完成再回正。
        /// 叠加规则（同上车弹性）：未到最大角忽略新移动；已过最大角（回正中）则自当前角重扩，保持与原加速度/最大幅度一致。
        /// 无 roll 轴时返回 false，调用方回退旧 Lerp。
        /// </summary>
        public bool TryStartRefillRoll(Vector3 targetLocalPos, float refillSpeed, Action onComplete)
        {
            if (rollAxle == null)
                return false;

            _rollOnComplete = onComplete;
            bool active = _rollRoutine != null && _rollPhase != 0;

            if (!active)
            {
                _rollAxleLocalOffset = rollAxle.localPosition;   // 换轴前记录偏移（车身旋转恒为 identity，与 ContainerGroup 空间同向）
                _rollPhase = 1;
                _rollAngle = 0f;
                _rollOutDur = Mathf.Max(refillRollOutDuration, 0.0001f);
                _rollTimer = 0f;
                _rollMoveDone = false;
                _rollRoutine = StartCoroutine(RefillRollRoutine());
            }
            else if (_rollPhase == 2)
            {
                // 回正中又发生新移动：自当前角重扩，时长按剩余距离比缩短，保持加速度与最大幅度一致
                _rollOutDur = Mathf.Max(ReexpandRollOutDuration(), 0.0001f);
                _rollPhase = 1;
                _rollTimer = 0f;
                _rollMoveDone = false;
            }
            else
            {
                // 侧倾中（未到最大角）或已在最大角保持：忽略新侧倾，只标记移动未完成
                _rollMoveDone = false;
            }

            // 移动目标在 ContainerGroup 空间（车身目标 + 轴偏移）；起点/时长延迟到换轴后按实际位置计算
            _rollMoveSpeed = refillSpeed;
            _rollMoveTarget = targetLocalPos + _rollAxleLocalOffset;
            _rollMoveTimer = 0f;
            _rollMovePending = true;
            _rollMoving = true;
            return true;
        }

        private IEnumerator RefillRollRoutine()
        {
            // 等上车弹性完成并收起轴，避免弹性轴与侧倾轴同时换轴冲突
            while (_elasticPhase != 0 || _elasticAxleSwapped)
                yield return null;

            SwapRollAxle();

            while (true)
            {
                float dt = Time.deltaTime;

                // 1. 补位移动：roll 轴在 ContainerGroup 空间匀速 Lerp
                if (_rollMoving)
                {
                    if (_rollMovePending)
                    {
                        _rollMoveStart = rollAxle.localPosition;   // 此刻已换轴，localPosition 即 ContainerGroup 空间
                        float dist = Vector3.Distance(_rollMoveStart, _rollMoveTarget);
                        _rollMoveDuration = _rollMoveSpeed > 0.0001f ? dist / _rollMoveSpeed : 0f;
                        _rollMovePending = false;
                    }
                    _rollMoveTimer += dt;
                    float k = Mathf.Clamp01(_rollMoveDuration > 0.0001f ? _rollMoveTimer / _rollMoveDuration : 1f);
                    rollAxle.localPosition = Vector3.Lerp(_rollMoveStart, _rollMoveTarget, k);
                    if (k >= 1f)
                    {
                        _rollMoving = false;
                        _rollMoveDone = true;
                    }
                }

                // 2. 侧倾角度推进
                if (_rollPhase == 1)
                {
                    _rollTimer += dt;
                    float p = Mathf.Clamp01(_rollTimer / _rollOutDur);
                    _rollAngle = refillRollMaxAngle * (1f - (1f - p) * (1f - p));   // 匀减速 ease-out
                    if (p >= 1f)
                    {
                        _rollAngle = refillRollMaxAngle;
                        if (_rollMoveDone)
                        {
                            _rollPhase = 2;
                            _rollTimer = 0f;
                        }
                        // 移动未完成：保持最大角（p 恒 1，角度恒最大）
                    }
                }
                else if (_rollPhase == 2)
                {
                    _rollTimer += dt;
                    float p = Mathf.Clamp01(_rollTimer / refillRollRecoverDuration);
                    _rollAngle = refillRollMaxAngle * (1f - p * p);   // 匀加速 ease-in
                    if (p >= 1f)
                    {
                        _rollAngle = 0f;
                        rollAxle.localRotation = Quaternion.identity;
                        RestoreRollAxle();
                        _rollPhase = 0;
                        var cb = _rollOnComplete;
                        _rollOnComplete = null;
                        _rollRoutine = null;
                        cb?.Invoke();
                        yield break;
                    }
                }

                // 3. 应用侧倾角度 + 锁定上车中像素的世界角度
                if (_rollPhase != 0 && rollAxle != null)
                    rollAxle.localRotation = Quaternion.Euler(_rollAngle, 0f, 0f);
                LockBoardingPixelsWorldRotation();

                yield return null;
            }
        }

        /// <summary>换轴（幂等）：roll 轴脱离车身挂到原始父物体并重置 scale，车身挂到 roll 轴下（世界位姿保持，无瞬移）。</summary>
        private void SwapRollAxle()
        {
            if (_rollAxleSwapped)
                return;
            if (rollAxle == null)
                return;

            _rollAxleParent = transform.parent;
            rollAxle.SetParent(_rollAxleParent, true);   // roll 轴脱离车身 → 原始父物体
            rollAxle.localScale = Vector3.one;           // 纯 pivot，重置 scale
            transform.SetParent(rollAxle, true);         // 车身挂到 roll 轴下
            _rollAxleSwapped = true;
        }

        /// <summary>收起轴（幂等）：车身脱离 roll 轴回原始父物体，roll 轴还给车身并重置 scale / rotation。</summary>
        private void RestoreRollAxle()
        {
            if (!_rollAxleSwapped)
                return;
            if (rollAxle == null)
            {
                _rollAxleSwapped = false;
                return;
            }

            rollAxle.localRotation = Quaternion.identity;
            transform.SetParent(_rollAxleParent, true);   // 车身脱离 roll 轴回原始父物体
            rollAxle.SetParent(transform, true);          // roll 轴还给车身
            rollAxle.localScale = Vector3.one;
            _rollAxleSwapped = false;
        }

        /// <summary>
        /// 回正中重扩的到位时长：使重扩加速度与初始态直接 ease-out 一致（自中间逐帧还原直接态动画）。
        /// 剩余距离比 p_r² = |M-θ| / |M-0|，直接态动画在角度 θ 处剩余时长为 T·p_r，故 T' = T·√(p_r²) = T·p_r。
        /// </summary>
        private float ReexpandRollOutDuration()
        {
            float max = Mathf.Abs(refillRollMaxAngle);
            if (max < 1e-4f)
                return refillRollOutDuration;
            float remain = Mathf.Abs(refillRollMaxAngle - _rollAngle);
            float remainRatio = Mathf.Clamp01(remain / max);
            return Mathf.Sqrt(remainRatio) * refillRollOutDuration;
        }

        /// <summary>侧倾时锁定上车中像素（父物体为 Pos，父物体旋转会偏移其位置）的世界角度为 0，保证跳跃上车表现不受侧倾影响。</summary>
        private void LockBoardingPixelsWorldRotation()
        {
            foreach (var pixel in _boardingPixels)
            {
                if (pixel != null)
                    pixel.transform.rotation = Quaternion.identity;
            }
        }
    }
}
