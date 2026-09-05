using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 点击判定专用的碰撞体组件：挂在独立子物体上，该子物体位于「Click」层并带一个 BoxCollider。
    /// 与像素物理用的 SphereCollider 分离，供 GameController 通过「Click」层射线检测点击。
    /// 持有反向引用 pixel，点击命中时回给 GameController 定位所属 PixelItem。
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class PixelClickListener : MonoBehaviour
    {
        /// <summary>反向引用：所属 PixelItem（由 PixelItem 初始化时赋值，不序列化）。</summary>
        [System.NonSerialized] public PixelItem pixel;

        /// <summary>点击碰撞体（同一物体上的 BoxCollider）。</summary>
        public BoxCollider BoxCollider { get; private set; }

        private void Awake()
        {
            BoxCollider = GetComponent<BoxCollider>();
        }

        /// <summary>设置点击碰撞体是否启用（从网格移出时禁用，避免再次被点击）。</summary>
        public void SetClickable(bool clickable)
        {
            if (BoxCollider != null)
                BoxCollider.enabled = clickable;
        }
    }
}
