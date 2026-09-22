using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Lu.DoTween
{
    public class LuTweenRotationDriver : LuTweenDriverBase
    {
        private Vector3 _target;
        private Vector3 _speed;
        private bool _isLocal;

        internal LuTweenDriverBase DoEuler(Vector3 target, float duration)
        {
            _isLocal = false;
            tweenDuration = duration;
            _target = target;
            _speed = (target - transform.eulerAngles) / duration;
            return this;
        }
        internal LuTweenDriverBase DoEulerX(float target, float duration)
        {
            _target.Set(target,transform.eulerAngles.y,transform.eulerAngles.z);
            return DoEuler(_target, duration);
        }
        internal LuTweenDriverBase DoEulerY(float target, float duration)
        {
            _target.Set(transform.eulerAngles.x,target,transform.eulerAngles.z);
            return DoEuler(_target, duration);
        }
        internal LuTweenDriverBase DoEulerZ(float target, float duration)
        {
            _target.Set(transform.eulerAngles.x,transform.eulerAngles.y,target);
            return DoEuler(_target, duration);
        }
        
        internal LuTweenDriverBase DoLocalEuler(Vector3 target, float duration)
        {
            _isLocal = true;
            tweenDuration = duration;
            _target = target;
            _speed = (target - transform.localEulerAngles) / duration;
            return this;
        }
        internal LuTweenDriverBase DoLocalEulerX(float target, float duration)
        {
            _target.Set(target,transform.localEulerAngles.y,transform.localEulerAngles.z);
            return  DoEuler(_target, duration);
        }
        internal LuTweenDriverBase DoLocalEulerY(float target, float duration)
        {
            _target.Set(transform.localEulerAngles.x,target,transform.localEulerAngles.z);
            return DoEuler(_target, duration);
        }
        internal LuTweenDriverBase DoLocalEulerZ(float target, float duration)
        {
            _target.Set(transform.localEulerAngles.x,transform.localEulerAngles.y,target);
            return DoEuler(_target, duration);
        }
        private void Update()
        {
            if (tweenDuration <= 0)
                return;
            if(_isLocal)
                transform.localEulerAngles += _speed * Time.deltaTime;
            else
                transform.eulerAngles += _speed * Time.deltaTime;

            tweenDuration -= Time.deltaTime;
            if (tweenDuration <= 0)
            {
                if(_isLocal)
                    transform.localEulerAngles = _target;
                else
                    transform.eulerAngles = _target;

                InvokeCompleteAction();
            }
        }
    }
}
