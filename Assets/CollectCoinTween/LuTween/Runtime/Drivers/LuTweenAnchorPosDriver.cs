using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lu.DoTween
{
    public class LuTweenAnchorPosDriver : LuTweenDriverBase
    {
        private RectTransform _rectTransform;
        private RectTransform rectTransform
        {
            get
            {
                if (!_rectTransform)
                    _rectTransform = GetComponent<RectTransform>();
                return _rectTransform;
            }
        }

        private Vector2 _target;
        private Vector2 _speed;
        
        internal LuTweenDriverBase DoAnchorPos(Vector2 target, float duration)
        {
            tweenDuration = duration;
            _target = target;
            _speed = (target - rectTransform.anchoredPosition) / duration;
            return this;
        }
        
        internal LuTweenDriverBase DoAnchorPosY(float targetY, float duration)
        {
            _target.Set(rectTransform.anchoredPosition.x,targetY);
            return DoAnchorPos(_target,duration);
        }
        internal LuTweenDriverBase DoAnchorPosX(float targetX, float duration)
        {
            _target.Set(targetX,rectTransform.anchoredPosition.y);
            return DoAnchorPos(_target,duration);
        }


        private void Update()
        {
            if (tweenDuration <= 0)
                return;
            rectTransform.anchoredPosition += _speed * Time.deltaTime;
            tweenDuration -= Time.deltaTime;
            if (tweenDuration <= 0)
            {
                rectTransform.anchoredPosition = _target;
                InvokeCompleteAction();
            }
        }
    }
}