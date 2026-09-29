#if TikTok
using TTSDK;
#elif WeChat
using WeChatWASM;
#endif



using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PufferMiniGame
{
    public interface IKeyboard
    {
        void ShowKeyboard(string confirmType = "done", int maxLength = 20, bool multiple = false);
    }


    public class Keyboard : MonoBehaviour
    {

        [SerializeField] InputField input;

        public System.Action<string> onKeyboardConfirm;

        public System.Action<string> onKeyboardInput;
        public System.Action<string> onKeyboardComplete;


        // Start is called before the first frame update
        void Start()
        {
            input.onEndEdit.AddListener(OnKeyboardConfirm);
            input.onValueChanged.AddListener(OnKeyboardInput);
            input.onEndEdit.AddListener(OnKeyboardComplete);

#if NoAds
            //gameObject.AddComponent<WebGLSupport.WebGLInput>();

#endif
            var comp = input.GetComponent<ClickableInputField>();
            if (comp == null)
            {
                comp = input.gameObject.AddComponent<ClickableInputField>();
            }
            comp.multiple = false;
            comp.confirmType = "done";

        }


        private void OnKeyboardInput(string value)
        {
            Debug.Log($"OnKeyboardInput: {value}");
            if (input.isFocused)
            {
                input.text = value;
            }
            onKeyboardInput?.Invoke(value);
        }

        private void OnKeyboardConfirm(string value)
        {
            Debug.Log($"OnKeyboardConfirm: {value}");
            onKeyboardConfirm?.Invoke(value);
        }

        private void OnKeyboardComplete(string value)
        {
            Debug.Log($"OnKeyboardComplete: {value}");
            onKeyboardComplete?.Invoke(value);
        }
    }


    public class ClickableInputField : EventTrigger
    {
        public string confirmType = "done"; // 可选值有: "done", "next", "search", "go", "send"
        public int maxInputLength = 100; // 最大输入长度
        public bool multiple = false; // 是否多行输入
        private InputField _inputField;

        IKeyboard keyboardHandle;

        private void Start()
        {
            _inputField = GetComponent<InputField>();

#if TikTok
            keyboardHandle = new TikTokKeyboardHandle(_inputField);

#elif WeChat

#if UNITY_EDITOR
            keyboardHandle = new UnityKeyboardHandle(_inputField);
#else
            keyboardHandle = new WeChatKeyboardHandle(_inputField);
#endif
    
#endif
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (_inputField != null)
            {
                if (_inputField.isFocused)
                {
                    if (keyboardHandle != null)
                        keyboardHandle.ShowKeyboard(confirmType, maxInputLength, multiple);
                }
            }
        }
    }

    public class UnityKeyboardHandle : IKeyboard
    {
        private InputField _inputField;
        public UnityKeyboardHandle(InputField inputField)
        {
            this._inputField = inputField;
        }

        public void ShowKeyboard(string confirmType = "done", int maxLength = 20, bool multiple = false)
        {
            Debug.Log("展示键盘");
            _inputField.text = "";
            this._inputField.onSubmit.AddListener(OnSubmit);
        }

        void HideKeyBoard()
        {
            this._inputField.onSubmit.RemoveListener(OnSubmit);
        }

        public void OnSubmit(string txt)
        {
            Debug.Log("提交内容" + txt);
        }
    }



#if TikTok
    public class TikTokKeyboardHandle : IKeyboard
    {
        InputField input;
        bool isShowKeyboard = false;

        public void HideKeyboard()
        {
            _HideKeyboard();//隐藏键盘
        }

        public TikTokKeyboardHandle(InputField input)
        {
            this.input = input;
        }
        /// <summary>
        /// 打开键盘
        /// </summary>
        /// <param name="target"></param>
        /// <param name="confirmType"></param>
        /// <param name="maxLength"></param>
        /// <param name="multiple"></param>
        public void ShowKeyboard(string confirmType = "done", int maxLength = 20, bool multiple = false)
        {
            if (!isShowKeyboard)
            {
                TT.ShowKeyboard(new TTKeyboard.ShowKeyboardOptions()
                {
                    maxLength = maxLength,
                    multiple = multiple,
                    defaultValue = input.text,
                    confirmType = confirmType
                });
                TT.OnKeyboardInput += OnKeyboardInput;
                TT.OnKeyboardConfirm += OnKeyboardConfirm;
                TT.OnKeyboardComplete += OnKeyboardComplete;

                //绑定回调
            }
        }
        private void OnKeyboardInput(string value)
        {
            Debug.Log($"OnKeyboardInput: {value}");
            if (input.isFocused)
            {
                input.text = value;
            }
            input.onValueChanged?.Invoke(input.text);
        }

        private void OnKeyboardConfirm(string value)
        {
            Debug.Log($"OnKeyboardConfirm: {value}");
            input.onSubmit?.Invoke(value);
            HideKeyboard();
        }

        private void OnKeyboardComplete(string value)
        {
            Debug.Log($"OnKeyboardComplete: {value}");
            input.onEndEdit?.Invoke(value);
            HideKeyboard();
        }

        private void _HideKeyboard()
        {
            if (isShowKeyboard)
            {
                TT.OnKeyboardInput -= OnKeyboardInput;
                TT.OnKeyboardConfirm -= OnKeyboardConfirm;
                TT.OnKeyboardComplete -= OnKeyboardComplete;

                isShowKeyboard = false;
            }

        }
    }


#elif WeChat

    public class WeChatKeyboardHandle:IKeyboard
    {
        InputField input;
        bool isShowKeyboard = false;


        public WeChatKeyboardHandle(InputField input)
        {
            this.input = input;
        }

        public void HideKeyboard()
        {
            _HideKeyboard();//隐藏键盘
        }

        /// <summary>
        /// 打开键盘
        /// </summary>
        /// <param name="target"></param>
        /// <param name="confirmType"></param>
        /// <param name="maxLength"></param>
        /// <param name="multiple"></param>
        public void ShowKeyboard(string confirmType = "done",int maxLength = 20,bool multiple = false)
        {
            if(!isShowKeyboard)
            {
                WX.ShowKeyboard(new ShowKeyboardOption()
                {
                    defaultValue = input.text, 
                    maxLength = maxLength,
                    confirmType =  confirmType,
                    multiple = multiple,
                    // 键盘右下角 confirm 按钮的类型，只影响按钮的文本内容 可选值： - 'done': 完成; - 'next': 下一个; - 'search':
                    //     搜索; - 'go': 前往; - 'send': 发送;
                });

                //绑定回调
                WX.OnKeyboardConfirm(OnConfirm);
                WX.OnKeyboardComplete(OnComplete);
                WX.OnKeyboardInput(OnInput);
            }
        }

        private void OnInput(OnKeyboardInputListenerResult result)
        {
             Debug.Log("OnInput");
             if(input.isFocused)
             {
                input.text = result.value;
             }
             input.onValueChanged?.Invoke(input.text);
        }

        private void OnComplete(OnKeyboardInputListenerResult result)
        {
            Debug.Log("OnComplete" + result.value);
            input.onEndEdit?.Invoke(result.value);
            HideKeyboard();
        }

        private void OnConfirm(OnKeyboardInputListenerResult result)
        {
            Debug.Log("OnConfirm" + result.value);
            input.onSubmit?.Invoke(result.value);
            HideKeyboard();
        }

        private void _HideKeyboard()
        {
            if(isShowKeyboard)
            {
                WX.HideKeyboard(new HideKeyboardOption());

                //删除掉相关事件监听
                WX.OffKeyboardInput(OnInput);
                WX.OffKeyboardConfirm(OnConfirm);
                WX.OnKeyboardComplete(OnComplete);

                isShowKeyboard = false;
            }

        }
    }
      
#endif

}

