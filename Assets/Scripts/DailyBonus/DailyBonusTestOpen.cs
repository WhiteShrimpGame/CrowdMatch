using UnityEngine;
using WsGame.DailyBouns;

namespace WsGame.DailyBouns.Integration
{
    /// <summary>
    /// 临时验证脚本：挂到场景里的 DailyBonusPanel 实例上，Play 时自动调用 Show() 打开面板。
    /// 当前项目还没有签到入口按钮，先用它开面板。入口按钮接好后请删除本文件。
    /// </summary>
    public class DailyBonusTestOpen : MonoBehaviour
    {
        [Tooltip("勾上则每次 Play 前清空签到进度，用于反复验证第 1 天 / 跨天循环")]
        [SerializeField] private bool resetProgressOnStart = false;

        private void Start()
        {
            var panel = GetComponent<DailyBonusPanel>();
            if (panel == null)
            {
                Debug.LogError("[DailyBonusTestOpen] 所在物体上没有 DailyBonusPanel 组件");
                return;
            }

            if (resetProgressOnStart)
            {
                PlayerPrefs.DeleteKey(DailyBounsData.Pre_DailyBonusIndex);
                PlayerPrefs.DeleteKey(DailyBounsData.Pre_DailyBonusTime);
                PlayerPrefs.DeleteKey(DailyBounsData.Pre_FirstDailyBonus);
                PlayerPrefs.Save();
                Debug.Log("[DailyBonusTestOpen] 已清空签到进度");
            }

            panel.Show();
        }
    }
}
