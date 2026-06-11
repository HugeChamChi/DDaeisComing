using UnityEngine;
using Bathhouse.Data;
using Bathhouse.Save;
using Cysharp.Threading.Tasks;

namespace Bathhouse.Managers
{
    /// <summary>
    /// 게임 내 핵심 진행 데이터(GameData)의 상태를 들고 있는 매니저입니다.
    /// 데이터 로드, 저장, 리셋 등의 기능을 수행합니다.
    /// </summary>
    public class DataManager : MonoBehaviour, ISavable
    {
        public GameData Current { get; private set; }
        public DailyRecordModel DailyRecord { get; private set; }

        [Header("SO References (Optional)")]
        [SerializeField] private FacilityIncomeDataSO _inspectorIncomeDataSO;

        public FacilityIncomeDataSO IncomeDataSO { get; private set; }
        public GameConfigSO Config { get; private set; }

        public void InitializeData()
        {
            DailyRecord = new DailyRecordModel();
            
            Config = Resources.Load<GameConfigSO>("GameConfig");
            if (Config == null)
            {
                Debug.LogWarning("[DataManager] GameConfig를 Resources 폴더에서 찾을 수 없습니다! 기본값을 사용하려면 Resources 폴더에 'GameConfig' SO를 생성하세요.");
                Config = ScriptableObject.CreateInstance<GameConfigSO>();
            }

            if (_inspectorIncomeDataSO != null)
            {
                IncomeDataSO = _inspectorIncomeDataSO;
            }
            else
            {
                IncomeDataSO = Resources.Load<FacilityIncomeDataSO>("FacilityIncomeData");
            }
            if (IncomeDataSO != null)
            {
                IncomeDataSO.Initialize();
            }
            if (Current == null)
            {
                Current = new GameData();
            }
        }

        public void LoadData(GameData data)
        {
            Current = data;
        }

        public void SaveData(ref GameData data)
        {
            if (Current != null)
            {
                data = Current;
            }
        }

        public void SaveData()
        {
            if (global::GlobalManagers.Save != null)
            {
                global::GlobalManagers.Save.SaveGameAsync().Forget();
            }
        }

        public void ResetData()
        {
            Current = new GameData();
            SaveData();
            Debug.Log("[DataManager] 게임 데이터가 초기화되었습니다.");
        }
    }
}
