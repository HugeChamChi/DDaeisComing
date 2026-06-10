using UnityEngine;
using Bathhouse.Data;

namespace Bathhouse.Managers
{
    /// <summary>
    /// 게임 내 핵심 진행 데이터(GameData)의 상태를 들고 있는 매니저입니다.
    /// 데이터 로드, 저장, 리셋 등의 기능을 수행합니다.
    /// </summary>
    public class DataManager : MonoBehaviour
    {
        public GameData Current { get; private set; }
        public DailyRecordModel DailyRecord { get; private set; }

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

            IncomeDataSO = Resources.Load<FacilityIncomeDataSO>("FacilityIncomeData");
            if (IncomeDataSO != null)
            {
                IncomeDataSO.Initialize();
            }
            if (GameManager.Save != null)
            {
                Current = GameManager.Save.LoadGame();
            }
            else
            {
                Current = new GameData();
            }
        }

        public void SaveData()
        {
            if (GameManager.Save != null && Current != null)
            {
                GameManager.Save.SaveGame(Current);
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
