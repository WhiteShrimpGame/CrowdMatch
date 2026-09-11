using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 头部描边 Billboard：每帧把挂载物体的指定局部轴（默认 -Y）对齐到「物体指向摄像机」的方向，
    /// 使该 Plane 始终背朝摄像机。配合 WhiteOutline（Cull Front，只画背面）材质渲染——
    /// Unity Plane primitive 是单面（三角形在 +Y 侧），让 -Y 朝向摄像机即让正面背离摄像机，
    /// 恰好由 Cull Front 渲染出来。
    ///
    /// 圆形 Plane 绕法线自转（roll）无视觉差异，故用 FromToRotation 只约束一个轴，不额外锁 up。
    /// 用法：挂在头骨下的 Plane 上（替代原球体），Plane 的 MeshRenderer 拖到 PixelItem.outlineRenderer。
    /// </summary>
    [DisallowMultipleComponent]
    public class BillboardOutline : MonoBehaviour
    {
        [Tooltip("对齐到摄像机的局部轴（应为单位向量，-Y 即该轴指向摄像机）")]
        public Vector3 cameraFacingAxis = Vector3.down;

        // 每帧只查一次 Camera.main，供所有实例共享（避免每个 Plane 各 FindWithTag 一次）
        private static Camera _cachedMain;
        private static int _cachedFrame = -1;

        private static Camera Main
        {
            get
            {
                if (_cachedFrame != Time.frameCount)
                {
                    _cachedFrame = Time.frameCount;
                    _cachedMain = Camera.main;
                }
                return _cachedMain;
            }
        }

        private void LateUpdate()
        {
            var cam = Main;
            if (cam == null)
                return;

            Vector3 dir = cam.transform.position - transform.position;
            if (dir.sqrMagnitude < 1e-8f)
                return;

            Vector3 axis = cameraFacingAxis;
            if (axis.sqrMagnitude < 1e-8f)
                return;                       // 零向量保护
            axis.Normalize();

            // 把局部 cameraFacingAxis（如 -Y）转到「物体→摄像机」方向（世界空间）
            transform.rotation = Quaternion.FromToRotation(axis, dir);
        }
    }
}
