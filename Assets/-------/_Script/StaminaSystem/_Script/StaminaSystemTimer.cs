using UnityEngine;
using WsGame;
using WsGame.Time;

public class StaminaSystemTimer : MonoBehaviour
    {
        private static StaminaSystemTimer _instance;
        public static StaminaSystemTimer Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<StaminaSystemTimer>();
                    if (_instance == null)
                    {
                        var obj = new GameObject("StaminaSystemTimer");
                        _instance = obj.AddComponent<StaminaSystemTimer>();
                        DontDestroyOnLoad(obj);
                    }
                }
                return _instance;
            }
        }

        private const float RefreshInterval = 2f;
        private float _timer;

        public void InitData() => CheckRecover();

        private void Update()
        {
            if (!StaminaSystemData._isInit) return;
            _timer += Time.deltaTime;
            if (_timer >= RefreshInterval)
            {
                CheckRecover();
                _timer = 0;
            }
        }

        // 自动恢复
        /*private void CheckRecover()
        {
            if (StaminaSystemData.IsInfiniteStamina) return;
            if (StaminaSystemData.Data.CurrentStamina >= StaminaSystemData.MaxStamina) return;

            long now = TimelimitedRefresh.Instance.GetCurrentTime();
            double delta = now - StaminaSystemData.Data.LastRecoverTime;
            int recoverCount = Mathf.FloorToInt((float)(delta / StaminaSystemData.StaminaRecoverInterval));

            // 没有可恢复的体力，直接退出
            if (recoverCount <= 0) return;

            // 一次性恢复所有体力
            StaminaSystemData.AddStamina(recoverCount, false);

            // 更新最后恢复时间
            StaminaSystemData.Data.LastRecoverTime = now;
            StaminaSystemData.Save();
        }*/
        private void CheckRecover()
        {
            if (StaminaSystemData.IsInfiniteStamina) return;
            if (StaminaSystemData.Data.CurrentStamina >= StaminaSystemData.MaxStamina) return;

            int now = TimeUtils.GetCurrentTime();
            int deltaSeconds = now - StaminaSystemData.Data.LastRecoverTime;
            int interval = StaminaSystemData.StaminaRecoverInterval;

            // 计算能恢复多少点
            int recoverCount = Mathf.FloorToInt((float)(deltaSeconds / interval));
            if (recoverCount <= 0) return;

            // 计算剩余时间
            int remainSeconds = deltaSeconds % interval;

            // 增加体力
            int newStamina = Mathf.Min(StaminaSystemData.Data.CurrentStamina + recoverCount, StaminaSystemData.MaxStamina);
            StaminaSystemData.AddStamina(newStamina - StaminaSystemData.Data.CurrentStamina, "time", false);
            StaminaSystemData.Data.CurrentStamina = newStamina;
            StaminaSystemData.Data.LastRecoverTime = now - remainSeconds;
            StaminaSystemData.Save();
        }

        // 剩余时间
        public double GetRemainRecoverTime()
        {
            if (StaminaSystemData.IsInfiniteStamina || StaminaSystemData.Data.CurrentStamina >= StaminaSystemData.MaxStamina)
                return 0;
            
            int now = TimeUtils.GetCurrentTime();
            int delta = now - StaminaSystemData.Data.LastRecoverTime;
            return Mathf.Max(0, (float)(StaminaSystemData.StaminaRecoverInterval - delta));
        }
    }