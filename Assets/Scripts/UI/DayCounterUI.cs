using UnityEngine;
using TMPro;
using Bathhouse.Managers;
using Cysharp.Threading.Tasks;

namespace Bathhouse.UI
{
    public class DayCounterUI : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private TMP_Text txtDay;
        
        [Header("Settings")]
        [SerializeField] private string prefix = "Day ";

        private async void Start()
        {
            Debug.Log("[DayCounterUI] Start called. Subscribing to OnNextDayStarted.");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnNextDayStarted += UpdateDayText;
            }

            // 게임 데이터가 완전히 로드될 때까지 안전하게 대기
            await UniTask.WaitUntil(() => GlobalManagers.Data != null && GlobalManagers.Data.Current != null);
            
            Debug.Log("[DayCounterUI] Data loaded. Initializing text.");
            UpdateDayText();
        }

        private void UpdateDayText()
        {
            if (txtDay == null)
            {
                Debug.LogWarning("[DayCounterUI] txtDay is not assigned in the Inspector!");
                return;
            }

            if (GlobalManagers.Data == null || GlobalManagers.Data.Current == null)
            {
                Debug.LogWarning("[DayCounterUI] Cannot update text: GlobalManagers.Data.Current is null.");
                return;
            }

            Debug.Log($"[DayCounterUI] UpdateDayText called. Day: {GlobalManagers.Data.Current.currentDay}");
            txtDay.text = prefix + GlobalManagers.Data.Current.currentDay;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnNextDayStarted -= UpdateDayText;
            }
        }
    }
}
