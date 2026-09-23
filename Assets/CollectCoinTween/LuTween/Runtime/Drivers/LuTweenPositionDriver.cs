using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Lu.DoTween
{
    public class LuTweenPositionDriver : LuTweenDriverBase
    {
        private Vector3 _target;
        private Vector3 _speed;
        private bool _isLocal;

        internal LuTweenDriverBase DoMove(Vector3 target, float duration)
        {
            _isLocal = false;
            tweenDuration = duration;
            _target = target;
            _speed = (target - transform.position) / duration;
            return this;
        }
        
        internal LuTweenDriverBase DoLocalMove(Vector3 target, float duration)
        {
            _isLocal = true;
            tweenDuration = duration;
            _target = target;
            _speed = (target - transform.localPosition) / duration;
            return this;
        }

        private void Update()
        {
            if (tweenDuration <= 0)
                return;
            if(_isLocal)
                transform.localPosition += _speed * Time.deltaTime;
            else
                transform.position += _speed * Time.deltaTime;

            tweenDuration -= Time.deltaTime;
            if (tweenDuration <= 0)
            {
                if(_isLocal)
                    transform.localPosition = _target;
                else
                    transform.position = _target;

                InvokeCompleteAction();
            }
        }
    }
}
