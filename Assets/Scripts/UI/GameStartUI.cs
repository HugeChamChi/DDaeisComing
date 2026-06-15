using UnityEngine;
using UnityEngine.UI;
using Bathhouse.Managers;

namespace Bathhouse.UI
{
    public class GameStartUI : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Button btnStart;

        private void Start()
        {
            if (btnStart != null)
            {
                btnStart.onClick.AddListener(OnStartClicked);
            }
        }

        private void OnStartClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartGame();
            }

            // 시작 화면 비활성화
            gameObject.SetActive(false);
        }
    }
}
