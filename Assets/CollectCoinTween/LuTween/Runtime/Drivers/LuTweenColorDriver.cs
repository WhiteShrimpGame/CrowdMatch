using UnityEngine;
using UnityEngine.UI;

namespace Lu.DoTween
{
    public class LuTweenColorDriver : LuTweenDriverBase
    {
        private Color _target;
        private Vector4 _speed;
        private Image _image;

        private Vector4 _nowColor = Vector4.zero;
        private Vector4 _targetColor = Vector4.zero;

        internal LuTweenDriverBase DoColor(Color target, float duration)
        {
            _image = _image ? _image : GetComponent<Image>();
            tweenDuration = duration;
            _target = target;
            var imageColor = _image.color;
            _nowColor.Set(imageColor.r, imageColor.g, imageColor.b, imageColor.a);
            _targetColor.Set(_target.r, _target.g, _target.b, _target.a);

            _speed = (_targetColor - _nowColor) / duration;
            return this;
        }

        internal LuTweenDriverBase DoFade(float targetFade, float duration)
        {
            _image = _image ? _image : GetComponent<Image>();
            var targetColor = _image.color;
            targetColor.a = targetFade;
            return DoColor(targetColor,duration);
        }
        private void Update()
        {
            if (tweenDuration <= 0)
                return;
            _nowColor += _speed * Time.deltaTime;
            _image.color = _nowColor;
            tweenDuration -= Time.deltaTime;
            if (tweenDuration <= 0)
            {
                _image.color = _targetColor;
                InvokeCompleteAction();
            }
        }
    }
}