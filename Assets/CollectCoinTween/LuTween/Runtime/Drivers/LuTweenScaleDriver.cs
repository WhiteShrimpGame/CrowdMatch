using UnityEngine;

namespace Lu.DoTween
{
    public class LuTweenScaleDriver : LuTweenDriverBase
    {
        private Vector3 _target;
        private Vector3 _speed;

        internal LuTweenDriverBase DoScale(Vector3 target, float duration)
        {
            tweenDuration = duration;
            _target = target;
            _speed = (target - transform.localScale) / duration;
            return this;
        }

        private void Update()
        {
            if (tweenDuration <= 0)
                return;
            transform.localScale += _speed * Time.deltaTime;
            tweenDuration -= Time.deltaTime;
            if (tweenDuration <= 0)
            {
                transform.localScale = _target;
                InvokeCompleteAction();
            }
        }
    }
}