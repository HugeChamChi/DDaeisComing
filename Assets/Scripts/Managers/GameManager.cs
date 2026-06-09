using UnityEngine;
using Bathhouse.Data;

namespace Bathhouse.Managers
{
    /// <summary>
    /// 게임 전체의 데이터와 각 매니저들을 관리하는 최상위 Facade 매니저입니다.
    /// GameManager.MiniGame 과 같은 형태로 다른 매니저들에 쉽게 접근할 수 있습니다.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // 매니저들에 대한 정적 접근 프로퍼티 (Facade Pattern)
        public static MiniGameManager MiniGame => Instance._miniGameManager;
        public static SaveManager Save => Instance._saveManager;
        public static InteractionManager Interaction => Instance._interactionManager;
        public static FacilityManager Facility => Instance._facilityManager;
        public static DataManager Data => Instance._dataManager;

        [Header("Managers")]
        [SerializeField] private MiniGameManager _miniGameManager;
        [SerializeField] private SaveManager _saveManager;
        [SerializeField] private InteractionManager _interactionManager;
        [SerializeField] private FacilityManager _facilityManager;
        [SerializeField] private DataManager _dataManager;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                
                InitializeManagers();
                
                // 각 하위 매니저들의 내부 초기화 순서 제어
                if (_saveManager != null) _saveManager.Init();
                if (_dataManager != null) _dataManager.InitializeData();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeManagers()
        {
            // 아직 할당되지 않은 매니저들을 씬에서 자동으로 찾아줍니다.
            if (_miniGameManager == null) _miniGameManager = FindObjectOfType<MiniGameManager>();
            if (_interactionManager == null) _interactionManager = FindObjectOfType<InteractionManager>();
            if (_facilityManager == null) _facilityManager = FindObjectOfType<FacilityManager>();

            // SaveManager는 씬에 없다면 동적으로 컴포넌트로 추가해줍니다.
            if (_saveManager == null)
            {
                _saveManager = GetComponentInChildren<SaveManager>();
                if (_saveManager == null) _saveManager = gameObject.AddComponent<SaveManager>();
            }

            if (_dataManager == null)
            {
                _dataManager = GetComponentInChildren<DataManager>();
                if (_dataManager == null) _dataManager = gameObject.AddComponent<DataManager>();
            }
        }
    }
}
