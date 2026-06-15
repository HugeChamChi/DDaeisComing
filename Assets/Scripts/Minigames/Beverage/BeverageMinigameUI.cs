using UnityEngine;
using UnityEngine.UI;


namespace DDaeisComing.Minigames.Beverage
{
    public class BeverageMinigameUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject minigamePanel;
        [SerializeField] private BeverageMinigameController minigameController;

        private void Awake()
        {
            // If the controller isn't assigned, try to find it on the same GameObject or children
            if (minigameController == null)
            {
                minigameController = GetComponentInChildren<BeverageMinigameController>();
            }
        }

        private void OnEnable()
        {
            if (minigameController != null)
            {
                minigameController.OnMinigameCleared += HandleMinigameCleared;
            }
        }

        private void OnDisable()
        {
            if (minigameController != null)
            {
                minigameController.OnMinigameCleared -= HandleMinigameCleared;
            }
        }

        public void OpenMinigame()
        {
            if (minigamePanel != null)
            {
                minigamePanel.SetActive(true);
            }
            else
            {
                // Fallback if panel is not assigned, activate this game object itself
                gameObject.SetActive(true);
            }

            if (minigameController != null)
            {
                minigameController.InitializeMinigame();
            }
        }

        public void CloseMinigame()
        {
            if (minigamePanel != null)
            {
                minigamePanel.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void HandleMinigameCleared()
        {
            CloseMinigame();
        }
    }
}
