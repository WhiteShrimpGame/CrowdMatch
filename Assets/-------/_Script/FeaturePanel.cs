using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;

/// <summary>
/// 新功能解锁弹窗面板
/// </summary>
public class FeaturePanel : MonoBehaviour
{
    [Header("UI绑定")]
    public Text txtTip;
    public Image imgIcon;
    public Button continueBtn;
    //public CanvasGroup canvasGroup;

    /*[Header("动画参数")]
    public float fadeDuration = 0.3f;
    public RectTransform panelRoot;

    private Sequence _showSequence;*/

    private void Awake()
    {
        // 初始隐藏
        //canvasGroup.alpha = 0;
        //canvasGroup.interactable = false;
        //canvasGroup.blocksRaycasts = false;
        continueBtn.onClick.AddListener(ClosePanel);
    }

    /// <summary>
    /// 填充数据并弹出面板
    /// </summary>
    public void ShowPanel(NewFeatureData data)
    {
        if (data == null)
        {
            Debug.LogError("NewFeatureData 为空！");
            return;
        }

        // 赋值文本与图标
        txtTip.text = data.featureTip;
        imgIcon.sprite = data.featureBigIcon;
        // 图片自适应原生尺寸
        imgIcon.SetNativeSize();
    }

    /// <summary>
    /// 关闭面板
    /// </summary>
    public void ClosePanel()
    {
        UIManager.Instance.HidePanel(transform);
    }
    private void OnDisable()
    {
        Destroy(gameObject);
    }

}