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

        /// <summary>是否处于暴露（可点击）状态</summary>
        public bool IsExposed { get; private set; }

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

        /// <summary>所属的 PixelGroup（运行时由 RebuildGrid 赋值，不序列化）</summary>
        [System.NonSerialized] public PixelGroup group;

        /// <summary>是否已到达聚集点（运行时标记，供 ContainerGroup 消费）</summary>
        [System.NonSerialized] public bool arrivedAtGatherPoint;

        /// <summary>IConveyorItem：供传送带定位的 Transform。</summary>
        public Transform Transform => transform;

        private void Awake()
        {
            ApplyMaterial();
            BindClickListener();
            if (exposeMoveTarget != null)
                _restLocalY = exposeMoveTarget.localPosition.y;
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

        /// <summary>立即切到 Walking：先直接归零再恢复根运动。</summary>
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
            if (config == null)
            {
                if (GameManager.Instance != null)
                    config = GameManager.Instance.colorConfig;
            }
            if (config == null)
                return;

            var mat = config.GetMaterial(colorId);
            if (mat == null)
                return;

            foreach (var r in renderers)
            {
                if (r != null)
                    r.sharedMaterial = mat;
            }
        }

        /// <summary>
        /// 设置暴露（可点击）状态：进入暴露时激活 Animator，并在 exposeMoveDuration 内把 Animator 物体匀速移动到 y=0；
        /// 退出暴露时关闭 Animator 并停止移动。
        /// </summary>
        public void SetExposed(bool exposed)
        {
            if (IsExposed == exposed)
                return;
            IsExposed = exposed;

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
                if (_exposeMove != null)
                {
                    StopCoroutine(_exposeMove);
                    _exposeMove = null;
                }
                if (animator != null)
                    animator.enabled = false;
            }
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
