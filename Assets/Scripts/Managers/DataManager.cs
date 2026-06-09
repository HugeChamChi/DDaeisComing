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

        public void InitializeData()
        {
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
