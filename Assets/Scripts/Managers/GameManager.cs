using System;
using UnityEngine;
using Bathhouse.Data;
using Bathhouse.Save;
using Cysharp.Threading.Tasks;

namespace Bathhouse.Managers
{
    /// <summary>
    /// 게임 전체의 데이터와 각 매니저들을 관리하는 최상위 Facade 매니저입니다.
    /// GameManager.MiniGame 과 같은 형태로 다른 매니저들에 쉽게 접근할 수 있습니다.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<GameManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("@GameManager");
                        _instance = go.AddComponent<GameManager>();
                    }
                    _instance.InitializeManagers();
                }
                return _instance;
            }
        }

        // 인게임 씬 매니저들에 대한 정적 접근 프로퍼티 (Facade Pattern)
        public static MiniGameManager MiniGame => Instance._miniGameManager;
        public static InteractionManager Interaction => Instance._interactionManager;
        public static FacilityManager Facility => Instance._facilityManager;

        [Header("InGame Managers")]
        [SerializeField] private MiniGameManager _miniGameManager;
        [SerializeField] private InteractionManager _interactionManager;
        [SerializeField] private FacilityManager _facilityManager;

        public event Action OnNoMoreCustomers;
        public event Action OnDayEnded;
        public event Action OnNextDayStarted;

        private float currentDayTime = 0f;
        private bool isNoMoreCustomersTriggered = false;
        private bool isDayEnded = false;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                InitializeManagers();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void InitializeManagers()
        {
            // 아직 할당되지 않은 인게임 매니저들을 씬에서 자동으로 찾아줍니다.
            if (_miniGameManager == null) _miniGameManager = FindFirstObjectByType<MiniGameManager>();
            if (_interactionManager == null) _interactionManager = FindFirstObjectByType<InteractionManager>();
            if (_facilityManager == null) _facilityManager = FindFirstObjectByType<FacilityManager>();
        }

        private void Update()
        {
            if (isDayEnded || global::GlobalManagers.Data?.Current == null) return;

            currentDayTime += Time.deltaTime;

            float dayDuration = global::GlobalManagers.Data.Config.dayDurationSeconds;
            float threshold = global::GlobalManagers.Data.Config.noCustomerTimeThreshold;

            if (!isNoMoreCustomersTriggered && (dayDuration - currentDayTime <= threshold))
            {
                isNoMoreCustomersTriggered = true;
                OnNoMoreCustomers?.Invoke();
            }

            if (currentDayTime >= dayDuration)
            {
                isDayEnded = true;
                OnDayEnded?.Invoke();
            }
        }

        public void StartNextDay()
        {
            currentDayTime = 0f;
            isDayEnded = false;
            isNoMoreCustomersTriggered = false;

            if (global::GlobalManagers.Data != null)
            {
                global::GlobalManagers.Data.Current?.AdvanceToNextDay();
                global::GlobalManagers.Data.DailyRecord?.Reset();
                global::GlobalManagers.Data.SaveData();
            }

            Time.timeScale = 1f;
            OnNextDayStarted?.Invoke();
            
            Debug.Log("[GameManager] 다음 날 사이클이 시작되었습니다.");
        }
    }
}
