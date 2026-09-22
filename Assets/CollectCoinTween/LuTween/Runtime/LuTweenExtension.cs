using UnityEngine;
using UnityEngine.UI;


namespace Lu.DoTween
{
    public static class LuTweenExtension
    {
        public static LuTweenDriverBase LuTweenDoScale(this Transform originTrans,Vector3 target,float duration)
        {
            var luTween = originTrans.GetComponent<LuTweenScaleDriver>();
            if (!luTween)
                luTween = originTrans.gameObject.AddComponent<LuTweenScaleDriver>();
            return luTween.DoScale(target,duration).ClearCompleteAction(); 
        }
    
        public static LuTweenDriverBase LuTweenDoColor(this Image originTrans,Color target,float duration)
        {
            var luTween = originTrans.GetComponent<LuTweenColorDriver>();
            if (!luTween)
                luTween = originTrans.gameObject.AddComponent<LuTweenColorDriver>();
            return luTween.DoColor(target,duration).ClearCompleteAction(); 
        }
    
        public static LuTweenDriverBase LuTweenDoFade(this Image originTrans,float target,float duration)
        {
            var luTween = originTrans.GetComponent<LuTweenColorDriver>();
            if (!luTween)
                luTween = originTrans.gameObject.AddComponent<LuTweenColorDriver>();
            return luTween.DoFade(target,duration).ClearCompleteAction(); 
        }
        
        public static LuTweenDriverBase LuTweenDoMove(this Transform originTrans,Vector3 target,float duration)
        {
            var luTween = originTrans.GetComponent<LuTweenPositionDriver>();
            if (!luTween)
                luTween = originTrans.gameObject.AddComponent<LuTweenPositionDriver>();
            return luTween.DoMove(target,duration).ClearCompleteAction(); 
        }
        
        public static LuTweenDriverBase LuTweenDoLocalMove(this Transform originTrans,Vector3 target,float duration)
        {
            var luTween = originTrans.GetComponent<LuTweenPositionDriver>();
            if (!luTween)
                luTween = originTrans.gameObject.AddComponent<LuTweenPositionDriver>();
            return luTween.DoLocalMove(target,duration).ClearCompleteAction(); 
        }
        
        public static LuTweenDriverBase LuTweenDoAnchorMove(this RectTransform originTrans,Vector2 target,float duration)
        {
            var luTween = originTrans.GetComponent<LuTweenAnchorPosDriver>();
            if (!luTween)
                luTween = originTrans.gameObject.AddComponent<LuTweenAnchorPosDriver>();
            return luTween.DoAnchorPos(target,duration).ClearCompleteAction(); 
        }
        public static LuTweenDriverBase LuTweenDoAnchorMoveX(this RectTransform originTrans,float target,float duration)
        {
            var luTween = originTrans.GetComponent<LuTweenAnchorPosDriver>();
            if (!luTween)
                luTween = originTrans.gameObject.AddComponent<LuTweenAnchorPosDriver>();
            return luTween.DoAnchorPosX(target,duration).ClearCompleteAction(); 
        }
        public static LuTweenDriverBase LuTweenDoAnchorMoveY(this RectTransform originTrans,float target,float duration)
        {
            var luTween = originTrans.GetComponent<LuTweenAnchorPosDriver>();
            if (!luTween)
                luTween = originTrans.gameObject.AddComponent<LuTweenAnchorPosDriver>();
            return luTween.DoAnchorPosY(target,duration).ClearCompleteAction(); 
        }
        
        public static LuTweenDriverBase LuTweenDoEuler(this Transform originTrans,Vector3 target,float duration)
        {
            var luTween = originTrans.GetComponent<LuTweenRotationDriver>();
            if (!luTween)
                luTween = originTrans.gameObject.AddComponent<LuTweenRotationDriver>();
            return luTween.DoEuler(target,duration).ClearCompleteAction(); 
        }
        public static LuTweenDriverBase LuTweenDoEulerX(this Transform originTrans,float target,float duration)
        {
            var luTween = originTrans.GetComponent<LuTweenRotationDriver>();
            if (!luTween)
                luTween = originTrans.gameObject.AddComponent<LuTweenRotationDriver>();
            return luTween.DoEulerX(target,duration).ClearCompleteAction(); 
        }
        public static LuTweenDriverBase LuTweenDoEulerY(this Transform originTrans,float target,float duration)
        {
            var luTween = originTrans.GetComponent<LuTweenRotationDriver>();
            if (!luTween)
                luTween = originTrans.gameObject.AddComponent<LuTweenRotationDriver>();
            return luTween.DoEulerY(target,duration).ClearCompleteAction(); 
        }
        public static LuTweenDriverBase LuTweenDoEulerZ(this Transform originTrans,float target,float duration)
        {
            var luTween = originTrans.GetComponent<LuTweenRotationDriver>();
            if (!luTween)
                luTween = originTrans.gameObject.AddComponent<LuTweenRotationDriver>();
            return luTween.DoEulerZ(target,duration).ClearCompleteAction(); 
        }
        
        public static LuTweenDriverBase LuTweenDoLocalEuler(this Transform originTrans,Vector3 target,float duration)
        {
            var luTween = originTrans.GetComponent<LuTweenRotationDriver>();
            if (!luTween)
                luTween = originTrans.gameObject.AddComponent<LuTweenRotationDriver>();
            return luTween.DoLocalEuler(target,duration).ClearCompleteAction(); 
        }
        public static LuTweenDriverBase LuTweenDoLocalEulerX(this Transform originTrans,float target,float duration)
        {
            var luTween = originTrans.GetComponent<LuTweenRotationDriver>();
            if (!luTween)
                luTween = originTrans.gameObject.AddComponent<LuTweenRotationDriver>();
            return luTween.DoLocalEulerX(target,duration).ClearCompleteAction(); 
        }
        public static LuTweenDriverBase LuTweenDoLocalEulerY(this Transform originTrans,float target,float duration)
        {
            var luTween = originTrans.GetComponent<LuTweenRotationDriver>();
            if (!luTween)
                luTween = originTrans.gameObject.AddComponent<LuTweenRotationDriver>();
            return luTween.DoLocalEulerY(target,duration).ClearCompleteAction(); 
        }
        public static LuTweenDriverBase LuTweenDoLocalEulerZ(this Transform originTrans,float target,float duration)
        {
            var luTween = originTrans.GetComponent<LuTweenRotationDriver>();
            if (!luTween)
                luTween = originTrans.gameObject.AddComponent<LuTweenRotationDriver>();
            return luTween.DoLocalEulerZ(target,duration).ClearCompleteAction(); 
        }

    }

}

