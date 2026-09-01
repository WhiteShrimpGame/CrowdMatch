using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 单个像素单位：持有一个颜色 ID，并根据 ID 应用对应材质。
    /// 由 PixelGroup 生成；gridX / gridZ 记录其在网格中的坐标。
    /// </summary>
    public class PixelItem : MonoBehaviour, IConveyorItem
    {
        [Tooltip("颜色 ID，对应 ColorConfig 中的材质下标")]
        public int colorId;

        [Tooltip("网格列坐标（横，X 方向），0 = 最小 X（最左）")]
        public int gridX;

        [Tooltip("网格行坐标（纵，Z 方向），0 = 最前排（Z 最大），越大越靠后（向 -Z）")]
        public int gridZ;

        /// <summary>所属的 PixelGroup（运行时由 RebuildGrid 赋值，不序列化）</summary>
        [System.NonSerialized] public PixelGroup group;

        /// <summary>是否已到达聚集点（运行时标记，供 ContainerGroup 消费）</summary>
        [System.NonSerialized] public bool arrivedAtGatherPoint;

        /// <summary>IConveyorItem：供传送带定位的 Transform。</summary>
        public Transform Transform => transform;

        private Renderer _renderer;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            ApplyMaterial();
        }

        /// <summary>设置颜色 ID 并立即应用材质</summary>
        public void SetColorId(int id)
        {
            colorId = id;
            ApplyMaterial();
        }

        /// <summary>
        /// 根据 colorId 应用材质；config 为空时自动从 GameManager 获取。
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

            if (_renderer == null)
                _renderer = GetComponent<Renderer>();
            if (_renderer != null)
                _renderer.sharedMaterial = mat;
        }
    }
}
