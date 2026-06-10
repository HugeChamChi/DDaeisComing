using UnityEngine;
using UnityEngine.UI;
using Bathhouse.Managers;
using System.Collections.Generic;
using Bathhouse.Data;
using TMPro;

namespace Bathhouse.UI
{
    public class DailyResultUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TMP_Text txtTotalVisits;
        [SerializeField] private Transform itemContainer;
        [SerializeField] private GameObject resultItemPrefab;
        [SerializeField] private TMP_Text txtTotalIncome;
        [SerializeField] private Button btnNextDay;

        [Header("Animation (임시)")]
        [Tooltip("팝업 창이 열릴 때 재생될 Animator (임시 스프라이트 애니메이션 적용용)")]
        [SerializeField] private Animator popupAnimator;

        private void Start()
        {
            if (resultPanel != null) resultPanel.SetActive(false);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnDayEnded += ShowResult;
            }

            if (btnNextDay != null)
            {
                btnNextDay.onClick.AddListener(OnNextDayClicked);
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnDayEnded -= ShowResult;
            }
        }

        public void ShowResult()
        {
            if (resultPanel != null) resultPanel.SetActive(true);

            if (popupAnimator != null)
            {
                // 스프라이트 애니메이션을 위해 팝업 열림 트리거 실행
                popupAnimator.SetTrigger("Open");
            }

            var dailyRecord = GameManager.Data.DailyRecord;
            var incomeDataSO = GameManager.Data.IncomeDataSO;

            if (dailyRecord == null) return;

            // 1. 총 방문객 표시
            if (txtTotalVisits != null)
            {
                txtTotalVisits.text = $"오늘의 방문 손님: {dailyRecord.totalNPCVisits}명";
            }

            // 2. 기존 목록 초기화
            foreach (Transform child in itemContainer)
            {
                Destroy(child.gameObject);
            }

            // 카운터(입장료) 계산 예시 (만약 카운터 상호작용이 따로 없다면 총 방문객으로 계산)
            // 기본 요금이 GameData.currentGold나 다른 곳에 없다면 일단 방문객 * 기본요금으로 하거나,
            // Counter 시설을 정상 이용했을 때 facilityUsageCounts에 등록된 값을 사용합니다.
            // 여기서는 facilityUsageCounts에 등록된 내용을 순회하여 표시합니다.

            foreach (var kvp in dailyRecord.facilityUsageCounts)
            {
                FacilityType type = kvp.Key;
                int count = kvp.Value;
                
                string resultTextName = type.ToString();
                int totalEarned = 0;

                if (incomeDataSO != null)
                {
                    IncomeData data = incomeDataSO.GetIncomeData(type);
                    resultTextName = string.IsNullOrEmpty(data.resultText) ? type.ToString() : data.resultText;
                    totalEarned = count * data.incomeAmount;
                }

                if (totalEarned > 0 || count > 0)
                {
                    CreateResultItem($"{resultTextName}: {count}회 (+{totalEarned:N0})");
                }
            }

            // 3. 총 수익 표시
            if (txtTotalIncome != null)
            {
                txtTotalIncome.text = $"총 수익: {dailyRecord.totalIncome:N0} 골드";
            }
            
            // 시간 타이머를 멈추기 위해 Time.timeScale 조절(선택 사항)
            // Time.timeScale = 0f;
        }

        private void CreateResultItem(string content)
        {
            if (resultItemPrefab == null || itemContainer == null) return;

            GameObject go = Instantiate(resultItemPrefab, itemContainer);
            TMP_Text txt = go.GetComponentInChildren<TMP_Text>();
            if (txt != null)
            {
                txt.text = content;
            }
        }

        private void OnNextDayClicked()
        {
            // 다음 날 처리 
            // Time.timeScale = 1f;
            if (GameManager.Data != null && GameManager.Data.Current != null)
            {
                GameManager.Data.Current.AdvanceToNextDay();
                GameManager.Data.DailyRecord.Reset();
            }

            if (resultPanel != null) resultPanel.SetActive(false);

            // TODO: 씬 재시작 혹은 다음 날을 위한 변수 초기화 등 필요한 로직 호출
            Debug.Log("[DailyResultUI] 다음 날 영업을 시작합니다!");
        }
    }
}
