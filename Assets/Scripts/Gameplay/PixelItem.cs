using System.Collections;
using System.Collections.Generic;
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

        /// <summary>是否处于暴露（可点击）状态</summary>
        public bool IsExposed { get; private set; }

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
                _exposeMove = StartCoroutine(MoveExposeTargetToYZero());
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

        /// <summary>把 exposeMoveTarget（localPosition）在 exposeMoveDuration 内匀速移动到 y=0。</summary>
        private IEnumerator MoveExposeTargetToYZero()
        {
            if (exposeMoveTarget == null)
                yield break;

            Transform t = exposeMoveTarget;
            Vector3 start = t.localPosition;
            Vector3 target = new Vector3(start.x, 0f, start.z);

            float duration = Mathf.Max(0f, exposeMoveDuration);
            if (duration <= 0.0001f)
            {
                t.localPosition = target;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float k = Mathf.Clamp01(elapsed / duration);
                t.localPosition = Vector3.Lerp(start, target, k);
                yield return null;
            }

            t.localPosition = target;
        }
    }
}
