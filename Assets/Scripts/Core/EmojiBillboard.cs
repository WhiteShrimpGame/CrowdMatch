using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 让所在的 **World Space** Canvas 始终正对镜头（billboard）。
    ///
    /// 由 <see cref="EmojiManager"/> 在生成带 Canvas 的表情时自动挂上（池里复用过的对象不会重复挂），
    /// 随表情一起回池 / 销毁，不需要手工配置。
    ///
    /// 做法是把 Canvas 的**世界朝向**对齐相机的世界朝向：这样 Canvas 的 +Z 与相机 forward 同向，
    /// 而 UI 的正面朝 -Z，于是正好朝向相机，且 UI 平面始终平行于屏幕平面。
    ///
    /// 放在 LateUpdate：保证在锚点移动（Update 里的 tween / 协程）之后才拨正，不会晚一帧看到歪的。
    /// 表情回池时对象被 SetActive(false)，LateUpdate 自然停止，不需要额外清理。
    /// </summary>
    [DisallowMultipleComponent]
    public class EmojiBillboard : MonoBehaviour
    {
        private Camera _camera;

        private void OnEnable()
        {
            _camera = Camera.main;
        }

        private void LateUpdate()
        {
            if (_camera == null)
            {
                // 相机被销毁 / 重建时 Unity 的 == 会把它当 null，这里重新取一次
                _camera = Camera.main;
                if (_camera == null)
                    return;   // 场景里暂时没有主相机：这一帧不动，等下一帧
            }

            transform.rotation = _camera.transform.rotation;
        }
    }
}
