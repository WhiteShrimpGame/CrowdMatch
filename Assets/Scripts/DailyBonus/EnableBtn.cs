using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WsGame
{
    /// <summary>
    /// 按钮灰度辅助：把子物体所有 Image 的材质切到 HsvSaturation=0（置灰）或 1（恢复）。
    /// 领取按钮 / 双倍按钮上挂这个组件。
    /// 注意：Init 里按需给子 Image 实例化材质（UIBtnMaterial 由宿主项目注入，示例见
    /// Fruit Loop Stack 的 Tools/EnableBtn.cs，它用的是 GlobalConfigMgr.Instance.GlobalConfig.UIBtnMaterial）。
    /// </summary>
    public class EnableBtn : MonoBehaviour
    {
        private List<Image> imgs = new List<Image>();
        private bool isInit = false;

        private static readonly int HsvSaturation = Shader.PropertyToID("_HsvSaturation");

        private void Init()
        {
            GetComponentsInChildren(imgs);
            //imgs.ForEach(v => v.material = Instantiate(GlobalModelMgr.Instance.ButtonMat));
            isInit = true;
        }

        public void EnableButton(bool enable)
        {
            if (!isInit) Init();

            foreach (Image img in imgs)
            {
                if (img.material.HasInt(HsvSaturation))
                    img.material.SetInt(HsvSaturation, enable ? 1 : 0);
            }
        }
    }
}
