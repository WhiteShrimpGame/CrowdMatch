using System.Collections;
using System.Collections.Generic;
using CrowdMatch;
using UnityEngine;
using UnityEngine.UI;

public class UnlimitedStaminaPanel : MonoBehaviour
{
    [SerializeField] Button closeBtn;
    [SerializeField] Button closeBtn2;
    //[SerializeField] Text lifeCountText;
    [SerializeField] Text timeText;
    [SerializeField] Text tipText;
    [SerializeField] GameObject wxobj;
    private float staminaRefreshTimer;

    void Start()
    {
        closeBtn.onClick.AddListener(ClosePanelMethod);
        closeBtn2.onClick.AddListener(ClosePanelMethod);
        RefreshStaminaUI();
    }
    private void Update()
    {
        staminaRefreshTimer += Time.deltaTime;
        if (staminaRefreshTimer >= 1f)
        {
            RefreshStaminaUI();
            staminaRefreshTimer = 0;
        }
    }

    void ClosePanelMethod()
    {
        AudioManager.Instance.PlayButtonAudioAndVibrate();
        UIManager.Instance.ShowUnlimitedStaminaPanel(false);
    }
    private void RefreshStaminaUI()
    {
        if (!StaminaSystemData._isInit) return;

        if (StaminaSystemData.IsInfiniteStamina)
        {
            //staminaCountText.text = "";
            //infiniteStaminaIcon.gameObject.SetActive(true);
            int sec = StaminaSystemData.InfiniteRemainTime;
            
            // 关卡无限体力
            if (GameManager.Instance.staminaConfig.UnlimitedStaminaLevel >= GameData.CurrentLevel)
            {
                //timeText.gameObject.SetActive(false);
                //wxobj.SetActive(true);
                timeText.text = "无限";
            }
            else
            {
                // 时长型无限体力
                if (sec > 0)
                {
                    timeText.text = $"{sec / 60:D2}:{sec % 60:D2}";
                }
            }
        }
        else
        {
            UIManager.Instance.ShowStaminaPanel(true);
            UIManager.Instance.ShowUnlimitedStaminaPanel(false);
            
        }
    }
    private void OnDisable()
    {
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        closeBtn.onClick.RemoveListener(ClosePanelMethod);
        closeBtn2.onClick.RemoveListener(ClosePanelMethod);
    }
}
