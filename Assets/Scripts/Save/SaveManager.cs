using UnityEngine;
using Cysharp.Threading.Tasks;
using Bathhouse.Data;
using System.Collections.Generic;
using System.Linq;

namespace Bathhouse.Save
{
    public class SaveManager : MonoBehaviour
    {
        private FileHandler _fileHandler;
        private GameData _gameData;
        private List<ISavable> _savables = new List<ISavable>();
        private bool _isSaving;

        private void Awake()
        {
            _fileHandler = new FileHandler("BathhouseSave.dat");
        }

        public async UniTask InitAsync()
        {
            _savables = FindSavables();
            await LoadGameAsync();
            
            // Auto Save Loop
            AutoSaveLoopAsync().Forget();
        }

        public async UniTask SaveGameAsync()
        {
            if (_isSaving) return;
            _isSaving = true;

            try
            {
                if (_gameData == null) _gameData = new GameData();

                // 1. ISavable 구현체로부터 데이터 수집
                // Clean up nulls
                _savables.RemoveAll(s => s == null || s.Equals(null));

                foreach (var savable in _savables)
                {
                    savable.SaveData(ref _gameData);
                }

                // 2. 직렬화
                string json = JsonUtility.ToJson(_gameData, true);

                // 3. 암호화
                byte[] encryptedData = CryptoUtility.EncryptToBytes(json);

                // 4. 로컬 파일 저장
                await _fileHandler.WriteSafeAsync(encryptedData);
                
                Debug.Log($"<color=#00FF00>[SaveManager]</color> 로컬 저장 성공! (Day: {_gameData.currentDay})");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveManager] 저장 중 오류 발생: {e.Message}");
            }
            finally
            {
                _isSaving = false;
            }
        }

        public async UniTask LoadGameAsync()
        {
            try
            {
                byte[] encryptedData = await _fileHandler.ReadAsync();

                if (encryptedData != null)
                {
                    string json = CryptoUtility.DecryptFromBytes(encryptedData);
                    _gameData = JsonUtility.FromJson<GameData>(json);
                    Debug.Log($"<color=#00FFFF>[SaveManager]</color> 로컬 데이터 로드 성공! (Day: {_gameData.currentDay})");
                }
                else
                {
                    Debug.Log("[SaveManager] 세이브 파일이 존재하지 않아 새 게임 데이터를 생성합니다.");
                    _gameData = new GameData();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveManager] 세이브 파일 로드/복호화 실패. 새 게임 데이터 생성. 에러: {e.Message}");
                _gameData = new GameData();
            }

            _savables.RemoveAll(s => s == null || s.Equals(null));
            foreach (var savable in _savables)
            {
                savable.LoadData(_gameData);
            }
        }

        public GameData GetCurrentData() => _gameData;
        public void SetCurrentData(GameData data) => _gameData = data;

        private List<ISavable> FindSavables()
        {
            return FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .OfType<ISavable>()
                .ToList();
        }

        public void RegisterSavable(ISavable savable)
        {
            if (savable != null && !_savables.Contains(savable))
            {
                _savables.Add(savable);
                // 즉시 현재 데이터 주입
                if (_gameData != null)
                {
                    savable.LoadData(_gameData);
                }
            }
        }

        public void UnregisterSavable(ISavable savable)
        {
            if (savable != null && _savables.Contains(savable))
            {
                _savables.Remove(savable);
            }
        }

        private async UniTaskVoid AutoSaveLoopAsync()
        {
            while (true)
            {
                await UniTask.Delay(System.TimeSpan.FromMinutes(1), ignoreTimeScale: true);
                if (!Application.isPlaying) break;
                
                await SaveGameAsync();
            }
        }

        private void OnApplicationQuit()
        {
            SaveGameSync();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveGameSync();
            }
        }

        // 동기 저장: 앱 종료 및 백그라운드 진입 시 즉시 저장
        private void SaveGameSync()
        {
            if (_gameData == null || _isSaving) return;

            _savables.RemoveAll(s => s == null || s.Equals(null));
            foreach (var savable in _savables)
            {
                savable.SaveData(ref _gameData);
            }

            string json = JsonUtility.ToJson(_gameData, true);
            byte[] encryptedData = CryptoUtility.EncryptToBytes(json);

            string fullPath = System.IO.Path.Combine(Application.persistentDataPath, "BathhouseSave.dat");
            System.IO.File.WriteAllBytes(fullPath, encryptedData);
            
            Debug.Log("[SaveManager] 동기 방식 긴급 저장 완료.");
        }
    }
}
