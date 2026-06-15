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
                    if (_instance != null)
                    {
                        _instance.InitializeManagers();
                    }
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
        public event Action OnGameOver;
        public event Action OnGameStarted;

        private float currentDayTime = 0f;
        private bool isNoMoreCustomersTriggered = false;
        private bool isDayEnded = false;
        private bool isGameStarted = false;

        public float CurrentDayTime => currentDayTime;
        public float DayDuration => global::GlobalManagers.Data?.Config?.dayDurationSeconds ?? 60f;

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
            if (!isGameStarted || isDayEnded || global::GlobalManagers.Data?.Current == null) return;

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

                // 하루 종료 시 정산: 유지비 및 월세를 당일 지출에 합산
                if (global::GlobalManagers.Data != null)
                {
                    var gameData = global::GlobalManagers.Data.Current;
                    var rentData = global::GlobalManagers.Data.RentData;
                    var dailyRecord = global::GlobalManagers.Data.DailyRecord;

                    if (gameData != null)
                    {
                        if (dailyRecord != null)
                        {
                            gameData.todayIncome = dailyRecord.totalIncome;
                        }

                        int additionalExpense = gameData.dailyMaintenanceCost;
                        
                        // 월세 납부일인지 확인
                        if (rentData != null && rentData.IsRentDay(gameData.currentDay))
                        {
                            additionalExpense += rentData.GetRentAmount(gameData.currentDay);
                        }

                        // 당일 지출 및 레코드에 반영 (UI에서 표시하기 위함)
                        gameData.todayExpense += additionalExpense;
                        dailyRecord?.AddExpense(additionalExpense);

                    }
                }

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
                var gameData = global::GlobalManagers.Data.Current;

                if (gameData != null)
                {
                    // 순수익(Net Profit)이 마이너스라도 최종 보유금액으로 메꿀 수 있다면 파산하지 않음
                    if (gameData.currentGold + gameData.GetTodayNetProfit() < 0)
                    {
                        Debug.Log($"[GameManager] 월세 및 지출을 감당할 자금이 부족하여 파산했습니다! (최종 예상 자금: {gameData.currentGold + gameData.GetTodayNetProfit()})");
                        OnGameOver?.Invoke();
                        return; // 다음 날로 넘어가지 않고 즉시 종료
                    }
                }

                global::GlobalManagers.Data.Current?.AdvanceToNextDay();
                global::GlobalManagers.Data.DailyRecord?.Reset();
                global::GlobalManagers.Data.SaveData();
            }

            Time.timeScale = 1f;
            OnNextDayStarted?.Invoke();
            
            Debug.Log("[GameManager] 다음 날 사이클이 시작되었습니다.");
        }

        public void StartGame()
        {
            if (isGameStarted) return;
            
            isGameStarted = true;
            isDayEnded = false;
            currentDayTime = 0f;
            isNoMoreCustomersTriggered = false;

            OnGameStarted?.Invoke();
            
            // 첫 날(Day 1) 연출 및 시작을 위해 이벤트를 강제로 한 번 발생시킵니다.
            OnNextDayStarted?.Invoke();
            
            Debug.Log("[GameManager] 게임이 최초 시작되었습니다.");
        }
    }
}
