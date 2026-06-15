using UnityEngine;
using Bathhouse.Managers;

namespace Bathhouse.UI
{
    /// <summary>
    /// 게임 내 이벤트(Next Day 등)와 UI 연출을 연결해주는 컨트롤러 컴포넌트입니다.
    /// </summary>
    public class DayTransitionController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private DayTransitionUI dayTransitionUI;

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                // 다음 날이 시작되었다는 이벤트를 구독합니다.
                GameManager.Instance.OnNextDayStarted += HandleNextDayStarted;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnNextDayStarted -= HandleNextDayStarted;
            }
        }

        private void HandleNextDayStarted()
        {
            if (dayTransitionUI != null && global::GlobalManagers.Data?.Current != null)
            {
                int currentDay = global::GlobalManagers.Data.Current.currentDay;
                
                // GameManager.StartNextDay() 내부에서 이미 currentDay가 1 증가한 상태이므로,
                // 이전 날짜(currentDay - 1)에서 현재 날짜(currentDay)로 넘어가는 연출을 실행합니다.
                // 만약 currentDay가 1이라면 prevDay는 0이 되어, UI에서 첫 날 연출(NextDay만 등장)로 자동 처리됩니다.
                int prevDay = currentDay - 1;
                
                dayTransitionUI.PlayTransition(prevDay, currentDay);
            }
        }
    }
}
