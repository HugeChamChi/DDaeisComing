using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Bathhouse.Managers;

namespace Bathhouse.UI
{
    /// <summary>
    /// 게임 데이터를 초기 상태(Day 1)로 되돌리는 테스트용 리셋 버튼입니다.
    /// 버튼을 누르면 확인 팝업을 띄우고, 확인 시 GameData를 새로 생성한 뒤 씬을 리로드합니다.
    /// </summary>
    public class ResetButton : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button btnReset;

        [Header("Confirm Popup")]
        [SerializeField] private GameObject confirmPanel;
        [SerializeField] private Button btnConfirm;
        [SerializeField] private Button btnCancel;

        private void Awake()
        {
            if (confirmPanel != null)
            {
                confirmPanel.SetActive(false);
            }

            if (btnReset != null)
            {
                btnReset.onClick.AddListener(ShowConfirm);
            }

            if (btnConfirm != null)
            {
                btnConfirm.onClick.AddListener(OnConfirmClicked);
            }

            if (btnCancel != null)
            {
                btnCancel.onClick.AddListener(HideConfirm);
            }
        }

        private void OnDestroy()
        {
            if (btnReset != null)
            {
                btnReset.onClick.RemoveListener(ShowConfirm);
            }

            if (btnConfirm != null)
            {
                btnConfirm.onClick.RemoveListener(OnConfirmClicked);
            }

            if (btnCancel != null)
            {
                btnCancel.onClick.RemoveListener(HideConfirm);
            }
        }

        private void ShowConfirm()
        {
            if (confirmPanel != null)
            {
                confirmPanel.SetActive(true);
            }
        }

        private void HideConfirm()
        {
            if (confirmPanel != null)
            {
                confirmPanel.SetActive(false);
            }
        }

        private void OnConfirmClicked()
        {
            Time.timeScale = 1f;

            if (global::GlobalManagers.Data != null)
            {
                global::GlobalManagers.Data.ResetData();          // Day 1로 초기화 및 세이브 덮어쓰기
                global::GlobalManagers.Data.DailyRecord?.Reset();  // 당일 기록 초기화
            }

            // 현재 씬을 다시 로드하여 초기 상태로 복귀
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
