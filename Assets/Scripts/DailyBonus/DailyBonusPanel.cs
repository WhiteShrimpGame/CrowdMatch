using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using WsGame.Time;

namespace WsGame.DailyBouns
{
    /// <summary>
    /// 每日签到面板
    /// 提取版：移除了 CoinTweenPanel / WSGameTools 等强耦合，奖励发放改为 IRewardHandler 注入。
    /// </summary>
    public class DailyBonusPanel : MonoBehaviour
    {
        public DailyBonusConfig dailyBonusConfig;

        /// <summary>奖励发放器（由宿主注入；为空时奖励发放静默跳过）</summary>
        public IRewardHandler rewardHandler;

        public RewardEffect rewardEffectPrefab;

        private CanvasGroup mainCanvasGroup;
        private List<DailyBonusItem> dailyBonusItems = new List<DailyBonusItem>();

        private UnityEngine.UI.Button claimBtn;
        private UnityEngine.UI.Button closeBtn;
        private UnityEngine.UI.Text countDownText;
        private UnityEngine.UI.Text goldCountText;

        private bool startTime = false;
        private bool isInit = false;

        private int dailyBonusIndex;
        public DailyBonusItem TodayDailyBonus { get; private set; }

        public delegate void CoinTweenDelegate(Transform start, int lastCount, int newCount);
        /// <summary>金币飞行动画（宿主注入；为空则跳过）</summary>
        public CoinTweenDelegate coinTweenCallback;

        public delegate void RefreshGoldDelegate();
        /// <summary>领取后刷新顶部金币显示（宿主注入；为空则跳过）</summary>
        public RefreshGoldDelegate onRefreshGold;

        /// <summary>
        /// 面板首次初始化完成后回调（宿主在这里注入飞币动画等；入口按钮销毁重建面板时只触发一次）。
        /// </summary>
        public static System.Action<DailyBonusPanel> OnPanelCreated;

        public void Show()
        {
            if (!isInit) Init();
            transform.Find("Bg").DOScale(0.4f, 0.3f).From().SetEase(DG.Tweening.Ease.OutBack);
            UpdateDailyItemState();
            RefreshGoldText();
            if (TodayDailyBonus == null)
            {
                countDownText.transform.parent.gameObject.SetActive(true);
                startTime = true;
            }
            else
            {
                countDownText.transform.parent.gameObject.SetActive(false);
                startTime = false;
            }
        }

        public void Hide()
        {
            transform.Find("Bg").DOScale(0.4f, 0.2f).SetEase(DG.Tweening.Ease.InBack).OnComplete(delegate
            {
                DestroyImmediate(gameObject);
            });
        }

        private void Update()
        {
            if (startTime)
            {
                double lastTime = TimeUtils.CheckRefresh(DailyBounsData.RefreshLastTime,
                    DailyBounsData.DailyTime, (v) => DailyBounsData.RefreshLastTime = v);
                countDownText.text = TimeUtils.FormatTime(lastTime);
                if (lastTime <= 0d)
                {
                    startTime = false;
                    UpdateDailyItemState();
                    countDownText.transform.parent.gameObject.SetActive(false);
                }
            }
        }

        private void Init()
        {
            dailyBonusIndex = DailyBounsData.DailyBonusIndex;
            GetComponentsInChildren(dailyBonusItems);
            claimBtn = transform.Find("Bg/BtnGroup/ClaimBtn").GetComponent<UnityEngine.UI.Button>();
            closeBtn = transform.Find("Bg/BackImg/CloseBtn").GetComponent<UnityEngine.UI.Button>();
            countDownText = transform.Find("Bg/BackImg/CountDown/CountDownText").GetComponent<UnityEngine.UI.Text>();
            var goldTrans = transform.Find("Bg/GoldFrame/GoldCount");
            goldCountText = goldTrans != null ? goldTrans.GetComponent<UnityEngine.UI.Text>() : null;
            // 面板根自持 CanvasGroup（子格子 Bg 也有 CanvasGroup，GetComponentInChildren 会抓错）
            mainCanvasGroup = GetComponent<CanvasGroup>();
            if (mainCanvasGroup == null)
                mainCanvasGroup = gameObject.AddComponent<CanvasGroup>();

            claimBtn.onClick.AddListener(ClaimReward);
            closeBtn.onClick.AddListener(Hide);

            // 初始有 1 个，复制 5 个，第 7 天是单独的（共 7 个）
            for (int i = 0; i < 5; i++)
            {
                DailyBonusItem newDailyBonus = Instantiate(dailyBonusItems[0]);
                newDailyBonus.transform.name = dailyBonusItems[0].transform.name;
                newDailyBonus.transform.SetParent(dailyBonusItems[0].transform.parent, false);
                newDailyBonus.transform.localScale = Vector3.one;
                newDailyBonus.transform.localPosition = Vector3.zero;
                newDailyBonus.transform.localRotation = Quaternion.identity;
                dailyBonusItems.Insert(i + 1, newDailyBonus);
            }

            dailyBonusItems.ForEach(v => v.InitData(this));

            for (int i = 1; i <= dailyBonusItems.Count; i++)
                dailyBonusItems[i - 1].Init(dailyBonusConfig.DailyBonusList[i - 1]);

            UpdateDailyItemState();
            isInit = true;
            OnPanelCreated?.Invoke(this);
        }

        private void ClaimReward()
        {
            if (TodayDailyBonus != null)
                TodayDailyBonus.OnBtnClick();
        }

        public void ChangeIndex()
        {
            if (dailyBonusIndex == 1)
            {
                if (DailyBounsData.FirstDailyBonus)
                    DailyBounsData.FirstDailyBonus = false;
            }

            DailyBounsData.RefreshLastTime = DailyBounsData.GetCurrentTime();
            dailyBonusIndex++;
            DailyBounsData.DailyBonusIndex = dailyBonusIndex;

            UpdateDailyItemState();
            startTime = true;
            countDownText.transform.parent.gameObject.SetActive(true);
        }

        /// <summary>
        /// 把面板内金币数刷成宿主当前金币。只在面板打开时调用。
        /// 领取后的数字变化交给飞币动画逐帧 SetGoldText，否则文字会先跳到新值、动画就没意义了。
        /// 预制体里需要有 Bg/GoldFrame/GoldCount 这个 Text 节点；没挂则静默跳过。
        /// </summary>
        public void RefreshGoldText()
        {
            var handler = rewardHandler != null ? rewardHandler : DailyBounsData.RewardHandler;
            if (handler == null)
                return;

            SetGoldText(handler.GetGoldCount());
        }

        /// <summary>把面板内金币文本设为指定值（飞币动画逐帧回调）。无 GoldCount 节点则跳过。</summary>
        public void SetGoldText(int value)
        {
            if (goldCountText == null)
                return;

            goldCountText.text = RewardEffect.FormatKMG(value);
        }

        public void UpdateDailyItemState()
        {
            if (dailyBonusIndex == 8)
            {
                dailyBonusIndex = 1;
                DailyBounsData.DailyBonusIndex = dailyBonusIndex;
            }

            for (int i = 1; i <= dailyBonusItems.Count; i++)
                dailyBonusItems[i - 1].ChangeState(DailyBounsData.GetDailyBonusState(i));

            TodayDailyBonus = null;
            for (int i = 0; i < dailyBonusItems.Count; i++)
            {
                if (dailyBonusItems[i].DailyBonusState == DailyBonusState.Today)
                {
                    TodayDailyBonus = dailyBonusItems[i];
                    break;
                }
            }

            // 领取按钮：今日可领才显示，领取后隐藏
            claimBtn.gameObject.SetActive(TodayDailyBonus != null);
        }

        public void SetCanvasGroupEnable(bool value)
        {
            if (mainCanvasGroup != null)
                mainCanvasGroup.interactable = value;
        }

        public void ShowCoinTween(Transform start, int lastCount, int newCount)
        {
            if (coinTweenCallback != null)
                coinTweenCallback(start, lastCount, newCount);
            if (onRefreshGold != null)
                onRefreshGold();
        }

        public RewardEffect GetRewardEffect()
        {
            return Instantiate(rewardEffectPrefab, transform);
        }
    }
}
