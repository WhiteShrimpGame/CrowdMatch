using UnityEngine;
using UnityEngine.UI;

namespace CrowdMatch
{
    /// <summary>
    /// Canvas 上的跳关按钮：下一关（NextLvl）/ 上一关（PrevLvl），
    /// 对应 GameManager 中调试键 N（下一关）/ B（上一关）的逻辑。
    /// 未在 Inspector 指定按钮时，按物体名自动查找并绑定。
    /// </summary>
    public class LevelSkipButtons : MonoBehaviour
    {
        [Tooltip("下一关按钮（留空则按名称 NextLvl 自动查找）")]
        public Button nextButton;

        [Tooltip("上一关按钮（留空则按名称 PrevLvl 自动查找）")]
        public Button prevButton;

        private void Awake()
        {
            if (nextButton == null)
                nextButton = FindButton("NextLvl");
            if (prevButton == null)
                prevButton = FindButton("PrevLvl");

            if (nextButton != null)
                nextButton.onClick.AddListener(NextLevel);
            if (prevButton != null)
                prevButton.onClick.AddListener(PrevLevel);
        }

        private static Button FindButton(string name)
        {
            var go = GameObject.Find(name);
            return go != null ? go.GetComponent<Button>() : null;
        }

        private void NextLevel()
        {
            var gm = GameManager.Instance;
            if (gm != null)
                gm.GameWin();
        }

        private void PrevLevel()
        {
            var gm = GameManager.Instance;
            if (gm != null)
                gm.PrevLevel();
        }
    }
}
