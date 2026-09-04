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

        [Tooltip("弹性缩放轴（空子物体，侧翻归 0 后单独应用的 XZ 放大/Y 缩小弹性缩放 pivot，可选）")]
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
            }
            // 落点不释放：该座位被该像素永久占用，直到整辆车出库销毁时一并带走

            PlayBoardElastic();                 // 触发弹性（叠加规则见 PlayBoardElastic）
            yield return WaitForElasticIdle();  // 等弹性归位

            onLastComplete?.Invoke();
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
                _elasticRoutine = StartCoroutine(ElasticRoutine(transform.localScale, ReexpandDuration()));
                return;
            }
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
            Vector3 cur = transform.localScale;
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
                transform.localScale = Vector3.Lerp(fromScale, boardElasticTargetScale, e);
                yield return null;
            }
            transform.localScale = boardElasticTargetScale;

            _elasticPhase = 2;
            t = 0f;
            while (t < boardElasticRecoverDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / boardElasticRecoverDuration);
                float e = p * p;   // 匀加速 ease-in
                transform.localScale = Vector3.Lerp(boardElasticTargetScale, Vector3.one, e);
                yield return null;
            }
            transform.localScale = Vector3.one;

            _elasticPhase = 0;
            _elasticRoutine = null;
        }

        private IEnumerator WaitForElasticIdle()
        {
            while (_elasticPhase != 0)
                yield return null;
        }
    }
}
