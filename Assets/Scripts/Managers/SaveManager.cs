using UnityEngine;
using System.IO;
using System.Text;
using Bathhouse.Data;

namespace Bathhouse.Managers
{
    /// <summary>
    /// 게임 데이터(GameData)를 JSON으로 직렬화하고 로컬 파일로 저장/불러오기 하는 매니저.
    /// 데이터 변조 방지를 위해 간단한 XOR 암호화를 적용합니다.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        private string _saveFilePath;
        private readonly string _xorKey = "DDAE_BATHHOUSE_SECRET_2026"; 

        public void Init()
        {
            _saveFilePath = Path.Combine(Application.persistentDataPath, "BathhouseSave.dat");
        }

        /// <summary>
        /// 제공된 게임 데이터를 암호화하여 로컬에 저장합니다.
        /// </summary>
        public void SaveGame(GameData data)
        {
            if (data == null) return;

            try
            {
                string json = JsonUtility.ToJson(data, true);
                string encrypted = XOREncryptDecrypt(json, _xorKey);
                File.WriteAllText(_saveFilePath, encrypted);
                
                Debug.Log($"<color=#00FF00>[SaveManager]</color> 게임 데이터 저장 완료! (Day: {data.currentDay}) - 경로: {_saveFilePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveManager] 저장 중 오류 발생: {e.Message}");
            }
        }

        /// <summary>
        /// 로컬에서 암호화된 게임 데이터를 불러옵니다.
        /// </summary>
        public GameData LoadGame()
        {
            if (File.Exists(_saveFilePath))
            {
                try
                {
                    string encrypted = File.ReadAllText(_saveFilePath);
                    string decrypted = XOREncryptDecrypt(encrypted, _xorKey);
                    GameData data = JsonUtility.FromJson<GameData>(decrypted);
                    
                    Debug.Log($"<color=#00FFFF>[SaveManager]</color> 게임 데이터 불러오기 성공! (Day: {data.currentDay}, Gold: {data.currentGold})");
                    return data;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[SaveManager] 세이브 파일 손상 또는 복호화 실패. 새 게임 데이터를 생성합니다. 에러: {e.Message}");
                }
            }
            else
            {
                Debug.Log("[SaveManager] 세이브 파일이 존재하지 않습니다. 새 게임을 시작합니다.");
            }
            return new GameData();
        }

        /// <summary>
        /// XOR 대칭 암호화/복호화 알고리즘
        /// </summary>
        private string XOREncryptDecrypt(string input, string key)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(key)) return input;

            StringBuilder sb = new StringBuilder(input.Length);
            for (int i = 0; i < input.Length; i++)
            {
                // 입력 문자와 키 문자를 XOR 연산
                char c = (char)(input[i] ^ key[i % key.Length]);
                sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
