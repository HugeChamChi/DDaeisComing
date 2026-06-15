using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Bathhouse.Managers;
using Bathhouse.Data;

namespace Bathhouse.UI
{
    public class GameOverUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Button btnRestart;

        private void Awake()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }

            if (btnRestart != null)
            {
                btnRestart.onClick.AddListener(OnRestartClicked);
            }
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameOver += HandleGameOver;
            }
            else
            {
                Debug.LogWarning("[GameOverUI] GameManager is not found.");
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameOver -= HandleGameOver;
            }
        }

        private void HandleGameOver()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }
            
            Time.timeScale = 0f;
        }

        private void OnRestartClicked()
        {
            Time.timeScale = 1f;

            if (global::GlobalManagers.Data != null)
            {
                global::GlobalManagers.Data.ResetData();
                global::GlobalManagers.Data.DailyRecord?.Reset();
            }

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
