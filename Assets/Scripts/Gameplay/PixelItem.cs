using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 单个像素单位：持有一个颜色 ID，并根据 ID 应用材质到 renderers 列表中的所有 Renderer。
    /// 由 PixelGroup 生成；gridX / gridZ 记录其在网格中的坐标。
    /// renderers 由 Block 预制体手动指定，不再动态创建 PixelItem 组件。
    /// </summary>
    public class PixelItem : MonoBehaviour, IConveyorItem
    {
        [Tooltip("颜色 ID，对应 ColorConfig 中的材质下标")]
        public int colorId;

        [Tooltip("是否为问号 Pixel（隐藏真实颜色，暴露到外层后才揭晓显示 colorId 对应颜色）")]
        public bool isQuestion;

        /// <summary>是否已揭晓：问号暴露过一次后永久为 true，之后保持原色、等同普通像素，不再变回问号。运行时状态，不序列化。</summary>
        [System.NonSerialized] public bool revealed;

        [Header("问号外观")]
        [Tooltip("问号物体（如头顶问号标志）：问号未揭晓时显示，揭晓后隐藏。留空则无问号视觉。")]
        public GameObject questionObject;

        [Tooltip("网格列坐标（横，X 方向），0 = 最小 X（最左）")]
        public int gridX;

        [Tooltip("网格行坐标（纵，Z 方向），0 = 最前排（Z 最大），越大越靠后（向 -Z）")]
        public int gridZ;

        [Header("渲染")]
        [Tooltip("需要更换材质的所有 Renderer（Block 预制体上手动指定）")]
        public List<Renderer> renderers = new List<Renderer>();

        [Header("点击/暴露")]
        [Tooltip("暴露在外层（可点击）时激活的 Animator")]
        public Animator animator;

        [Tooltip("暴露后要匀速移动到 y=0 的物体（独立于 Animator 引用）")]
        public Transform exposeMoveTarget;

        [Tooltip("暴露后把 exposeMoveTarget 匀速移动到 y=0 的时长（秒）")]
        public float exposeMoveDuration = 0.3f;

        [Tooltip("点击碰撞体组件（挂在 Click 层的子物体上）；为空时在 Awake 中自动查找子物体")]
        public PixelClickListener listener;

        [Header("描边")]
        [Tooltip("可点击时显示的白描边 Renderer（头骨上的 Cull Front 白球）；随暴露状态显隐")]
        public Renderer outlineRenderer;

        /// <summary>是否处于暴露（可点击）状态</summary>
        public bool IsExposed { get; private set; }

        /// <summary>管道放置中标记：期间 SetExposed 只记录状态、不激活 Animator，待放置完成后统一激活。</summary>
        [System.NonSerialized] public bool placing;

        /// <summary>管道蛇形生成期间标记：提取寻路（本批离开的像素）中把该像素视为可通行（不阻挡）。由 CrowdBufferZone 在提取结束时清除。</summary>
        [System.NonSerialized] public bool walkableDuringExtraction;

        /// <summary>Animator 中「Walking」布尔参数名（控制走/停动画）。</summary>
        private const string WalkParam = "Walking";

        /// <summary>从 Walking 回到 Idle 时，Animator 所属 Transform 归零的时长（秒）。</summary>
        private const float IdleResetDuration = 0.1f;

        /// <summary>最新期望的走/停状态（由传送带追赶状态等驱动；平滑期间会暂存而不立即应用）。</summary>
        private bool _wantWalking;

        /// <summary>是否正在做 Idle 平滑归零（期间不切回 Walking）。</summary>
        private bool _smoothing;

        /// <summary>起跳坐回时 exposeMoveTarget 的目标 y（Awake 捕获预制体初始值，兜底 -0.6957998）。</summary>
        private float _restLocalY = -0.6957998f;

        private Coroutine _exposeMove;

        /// <summary>问号物体「延迟一帧显示」的协程句柄（隐藏时立即停止）。</summary>
        private Coroutine _questionShowRoutine;

        /// <summary>缓存的颜色配置（Spawn/首次 ApplyMaterial 时记录，供问号揭晓切材质时复用，避免依赖 GameManager 时序）。</summary>
        private ColorConfig _cachedConfig;

        /// <summary>所属的 PixelGroup（运行时由 RebuildGrid 赋值，不序列化）</summary>
        [System.NonSerialized] public PixelGroup group;

        /// <summary>是否已到达聚集点（运行时标记，供 ContainerGroup 消费）</summary>
        [System.NonSerialized] public bool arrivedAtGatherPoint;

        /// <summary>进入物理缓冲区时随机到的个人前进速度（世界单位/秒，由 CrowdBufferZone 赋值，进入后保持不变）</summary>
        [System.NonSerialized] public float bufferCrowdSpeed;

        /// <summary>进入物理缓冲区时随机到的朝向点横向（x）偏移（世界单位，由 CrowdBufferZone 赋值；距出口较远时朝 gap + perp*该偏移 前进）</summary>
        [System.NonSerialized] public float bufferAimOffset;

        /// <summary>IConveyorItem：供传送带定位的 Transform。</summary>
        public Transform Transform => transform;

        private void Awake()
        {
            ApplyMaterial();
            BindClickListener();
            if (exposeMoveTarget != null)
                _restLocalY = exposeMoveTarget.localPosition.y;
            if (outlineRenderer != null)
                outlineRenderer.enabled = false;   // 初始不可点击，描边关闭
            RefreshQuestionObject();   // 初始化问号物体显隐（未揭晓问号显示、其余隐藏）
        }

        /// <summary>查找并绑定点击碰撞体组件，赋值反向引用供点击判定使用。</summary>
        private void BindClickListener()
        {
            if (listener == null)
                listener = GetComponentInChildren<PixelClickListener>(true);
            if (listener != null)
                listener.pixel = this;
        }

        /// <summary>设置点击碰撞体是否启用（从网格移出时禁用，避免再次被点击）。</summary>
        public void SetClickable(bool clickable)
        {
            if (listener != null)
                listener.SetClickable(clickable);
        }

        /// <summary>设置走/停动画：true = 播放 Walking，false = 回到 Idle（并确保 Animator 启用）。
        /// Walking 时恢复根运动；回到 Idle 时关闭根运动，并用 DOTween 平滑归零。
        /// 若切到 Idle 的平滑尚未完成，期间的 Walking 请求会被延后，待平滑结束且仍处于追赶状态时再切回 Walking。</summary>
        public void SetWalking(bool walking)
        {
            if (animator == null)
                return;

            _wantWalking = walking;

            if (walking)
            {
                // 平滑归零进行中：不切回 Walking，等平滑结束再按最新期望状态决定
                if (_smoothing)
                    return;
                ApplyWalking();
            }
            else
            {
                ApplyIdle();
            }
        }

        /// <summary>立即切到 Walking：先直接归零再恢复根运动。走路是站立姿态，故取消进行中的坐下并把 exposeMoveTarget 恢复到 y=0。</summary>
        private void ApplyWalking()
        {
            animator.enabled = true;
            animator.SetBool(WalkParam, true);

            // 停掉可能仍在进行的归零 tween，并直接归零（正常情况下平滑已结束，这里兜底）
            animator.transform.DOKill();
            animator.transform.localPosition = Vector3.zero;
            animator.transform.localRotation = Quaternion.identity;

            // 恢复根运动：身体随 Walking 的根运动位移/晃动
            animator.applyRootMotion = true;

            // 走路是站立姿态：取消「退出暴露触发的坐下」并把 exposeMoveTarget 恢复到 y=0（被匹配移除的像素要站立走开，不应坐下）
            if (_exposeMove != null)
            {
                StopCoroutine(_exposeMove);
                _exposeMove = null;
            }
            if (exposeMoveTarget != null)
            {
                var lp = exposeMoveTarget.localPosition;
                exposeMoveTarget.localPosition = new Vector3(lp.x, 0f, lp.z);
            }
        }

        /// <summary>切到 Idle：关闭根运动并平滑归零；完成后若仍处于追赶状态则切回 Walking。</summary>
        private void ApplyIdle()
        {
            _smoothing = true;

            animator.enabled = true;
            animator.SetBool(WalkParam, false);

            // 关闭根运动：Animator 不再每帧写 transform，随后平滑归零
            animator.applyRootMotion = false;

            animator.transform.DOKill();
            animator.transform.DOLocalMove(Vector3.zero, IdleResetDuration);
            animator.transform.DOLocalRotate(Vector3.zero, IdleResetDuration)
                .OnComplete(OnIdleSmoothComplete);
        }

        /// <summary>Idle 平滑归零结束：清除标记，若期间又有追赶（Walking）请求则切回 Walking。</summary>
        private void OnIdleSmoothComplete()
        {
            _smoothing = false;
            if (_wantWalking && animator != null)
                ApplyWalking();
        }

        /// <summary>把 exposeMoveTarget 匀速坐回原始 y（上车起跳时调用，默认回 _restLocalY）。</summary>
        public void SitDownExposeTarget()
        {
            if (exposeMoveTarget == null)
                return;
            if (_exposeMove != null)
                StopCoroutine(_exposeMove);
            _exposeMove = StartCoroutine(MoveExposeTargetToY(_restLocalY, exposeMoveDuration));
        }

        /// <summary>设置颜色 ID 并立即应用材质</summary>
        public void SetColorId(int id)
        {
            colorId = id;
            ApplyMaterial();
        }

        /// <summary>
        /// 根据 colorId 应用材质到 renderers 列表中的每个 Renderer；config 为空时自动从 GameManager 获取。
        /// </summary>
        public void ApplyMaterial(ColorConfig config = null)
        {
            if (config != null)
                _cachedConfig = config;
            else if (_cachedConfig == null && GameManager.Instance != null)
                _cachedConfig = GameManager.Instance.colorConfig;

            if (_cachedConfig == null)
                return;

            // 问号 Pixel 未揭晓时用问号材质，揭晓后（或普通 Pixel）用 colorId 对应材质
            Material mat;
            if (isQuestion && !revealed)
                mat = _cachedConfig.questionMaterial;
            else
                mat = _cachedConfig.GetMaterial(colorId);

            if (mat == null)
                return;

            foreach (var r in renderers)
            {
                if (r != null)
                    r.sharedMaterial = mat;
            }
        }

        /// <summary>按「是否未揭晓问号」刷新问号物体的显隐：未揭晓问号显示，其余（非问号/已揭晓）隐藏。
        /// 运行时显示延迟一帧（避免生成瞬间就位前瞬移），隐藏立即；编辑器（非 Play）立即显示以便预览。</summary>
        public void RefreshQuestionObject()
        {
            if (questionObject == null)
                return;

            bool show = isQuestion && !revealed;
            if (!show)
            {
                StopQuestionShowDelay();
                questionObject.SetActive(false);
                return;
            }

            if (!Application.isPlaying)
            {
                questionObject.SetActive(true);   // 编辑器预览：立即显示
                return;
            }

            StopQuestionShowDelay();
            _questionShowRoutine = StartCoroutine(ShowQuestionNextFrame());
        }

        private void StopQuestionShowDelay()
        {
            if (_questionShowRoutine != null)
            {
                StopCoroutine(_questionShowRoutine);
                _questionShowRoutine = null;
            }
        }

        /// <summary>延迟一帧后再显示问号物体（期间若已揭晓/隐藏则取消）。</summary>
        private IEnumerator ShowQuestionNextFrame()
        {
            yield return null;
            _questionShowRoutine = null;
            if (questionObject != null && isQuestion && !revealed)
                questionObject.SetActive(true);
        }

        /// <summary>
        /// 设置暴露（可点击）状态：进入暴露时激活 Animator，并在 exposeMoveDuration 内把 exposeMoveTarget 匀速移动到 y=0（起身上升）。
        /// 退出暴露时关闭 Animator，并把 exposeMoveTarget 坐回 _restLocalY（坐下）。起身与坐下互斥共用 _exposeMove、都从当前位置开始，
        /// 因此「坐下途中又暴露」会从当前位置站起（过程状态与稳态都正确）。
        /// </summary>
        public void SetExposed(bool exposed)
        {
            if (IsExposed == exposed)
                return;
            IsExposed = exposed;
            if (isQuestion)
            {
                if (exposed)
                    revealed = true;   // 问号一旦揭晓，永久保持原色、等同普通像素
                ApplyMaterial();         // 揭晓换回原色（之后不再变回问号材质）
                RefreshQuestionObject(); // 揭晓后隐藏问号物体
            }
            if (placing)
                return;   // 管道放置中：只记录状态，不激活动画，放置完成后由 MarkPlaced / RefreshExposed 统一应用
            ApplyExposedState(exposed);
        }

        /// <summary>按暴露状态应用动画：进入暴露激活 Animator、把 exposeMoveTarget 平滑到 y=0（起身）；
        /// 退出暴露关闭 Animator、把 exposeMoveTarget 坐回 _restLocalY（坐下）。
        /// 起身与坐下互斥共用 _exposeMove、都从当前位置开始，中途切换（如坐下途中又暴露）会从当前位置反向移动。</summary>
        private void ApplyExposedState(bool exposed)
        {
            if (outlineRenderer != null)
                outlineRenderer.enabled = exposed;
            if (exposed)
            {
                if (animator != null)
                    animator.enabled = true;
                if (_exposeMove != null)
                    StopCoroutine(_exposeMove);
                _exposeMove = StartCoroutine(MoveExposeTargetToY(0f, exposeMoveDuration));
            }
            else
            {
                if (animator != null)
                    animator.enabled = false;
                SitDownExposeTarget();   // 坐下：被箱体/管道封路重新堵住时，从站起状态坐回
            }
        }

        /// <summary>管道放置完成：清除放置标记并把暴露状态复位（Animator 关闭、Root 归位），等待后续 RefreshExposed 统一激活。</summary>
        public void MarkPlaced()
        {
            if (!placing)
                return;
            placing = false;
            IsExposed = false;
            ApplyExposedState(false);
        }

        /// <summary>把 exposeMoveTarget（localPosition）在 duration 内匀速移动到指定 y（x/z 保持）。</summary>
        private IEnumerator MoveExposeTargetToY(float targetY, float duration)
        {
            if (exposeMoveTarget == null)
                yield break;

            Transform t = exposeMoveTarget;
            Vector3 start = t.localPosition;
            Vector3 target = new Vector3(start.x, targetY, start.z);

            float dur = Mathf.Max(0f, duration);
            if (dur <= 0.0001f)
            {
                t.localPosition = target;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float k = Mathf.Clamp01(elapsed / dur);
                t.localPosition = Vector3.Lerp(start, target, k);
                yield return null;
            }

            t.localPosition = target;
        }
    }
}
