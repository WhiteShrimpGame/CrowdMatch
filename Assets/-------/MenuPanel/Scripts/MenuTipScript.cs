using CrowdMatch;
using UnityEngine;
using UnityEngine.UI;

//临时脚本，后续删除
public class MenuTipScript : MonoBehaviour
{
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            AudioManager.Instance.PlayButtonAudioAndVibrate();
            UIManager.Instance.ShowTip("敬请期待");
        });
    }

}
