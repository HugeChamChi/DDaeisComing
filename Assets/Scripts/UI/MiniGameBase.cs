using UnityEngine;
using UnityEngine.UI;
using System;

namespace Bathhouse.MiniGames
{
    public abstract class MiniGameBase : MonoBehaviour
    {
        [Header("Base Settings")]
        public GameObject gamePanel;
        public MiniGameDataSO miniGameData;

        [Header("Timer UI (Optional)")]
        [Tooltip("남은 시간을 표시할 슬라이더 (0~1)")]
        public Slider timerSlider;

        protected float _timeRemaining = 0f;
        protected float _currentTimeLimit = 10f;
        protected float _finalDifficultyWeight = 1.0f;

        protected Action _onSuccessCallback;
        protected Action _onFailCallback;

        protected virtual void Start()
        {
            if (gamePanel != null)
                gamePanel.SetActive(false);
        }

        protected virtual void Update()
        {
            if (gamePanel == null || !gamePanel.activeSelf) return;

            UpdateTimer();
        }

        protected virtual void UpdateTimer()
        {
            _timeRemaining -= Time.deltaTime;
            if (timerSlider != null)
            {
                timerSlider.value = _timeRemaining / _currentTimeLimit;
            }

            if (_timeRemaining <= 0f)
            {
                GameFailed();
            }
        }

        public virtual void StartMiniGame(int currentDay, Action onSuccess, Action onFail)
        {
            _onSuccessCallback = onSuccess;
            _onFailCallback = onFail;
            
            float baseDifficulty = miniGameData != null ? miniGameData.baseDifficultyWeight : 1.0f;
            float increasePerDay = miniGameData != null ? miniGameData.difficultyIncreasePerDay : 0.1f;
            float maxDifficulty = miniGameData != null ? miniGameData.maxDifficultyWeight : 3.0f;
            float baseTimeLimit = miniGameData != null ? miniGameData.baseTimeLimit : 10f;

            float calculatedWeight = baseDifficulty + ((currentDay - 1) * increasePerDay);
            _finalDifficultyWeight = Mathf.Clamp(calculatedWeight, baseDifficulty, maxDifficulty);

            _currentTimeLimit = baseTimeLimit / _finalDifficultyWeight;
            _currentTimeLimit = Mathf.Max(3f, _currentTimeLimit);
            _timeRemaining = _currentTimeLimit;

            if (gamePanel != null)
                gamePanel.SetActive(true);
                
            OnGameStarted();
        }

        protected abstract void OnGameStarted();

        protected virtual void GameSuccess()
        {
            if (gamePanel != null)
                gamePanel.SetActive(false);
            _onSuccessCallback?.Invoke();
        }

        protected virtual void GameFailed()
        {
            if (gamePanel != null)
                gamePanel.SetActive(false);
            _onFailCallback?.Invoke();
        }
    }
}
