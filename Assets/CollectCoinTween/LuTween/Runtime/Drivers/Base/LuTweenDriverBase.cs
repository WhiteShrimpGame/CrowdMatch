using System;
using UnityEngine;

namespace Lu.DoTween
{
    public class LuTweenDriverBase : MonoBehaviour
    {
        protected float tweenDuration;
        private Action _onComplete;


        public LuTweenDriverBase OnComplete(Action completeAction)
        {
            _onComplete = completeAction;
            return this;
        }

        public LuTweenDriverBase ClearCompleteAction()
        {
            _onComplete = null;
            return this;
        }

        protected void InvokeCompleteAction()
        {
            _onComplete?.Invoke();
        }
    }
}