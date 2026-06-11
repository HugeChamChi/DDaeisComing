using UnityEngine;
using UnityEngine.UI;
using Bathhouse.Managers;
using System.Collections.Generic;
using Bathhouse.Data;
using TMPro;
using GaeGGUL.Animation;
using Cysharp.Threading.Tasks;
using Bathhouse.Utils;

namespace Bathhouse.UI
{
    public class DailyResultUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private Transform itemContainer;
        [SerializeField] private GameObject resultItemPrefab;
        [SerializeField] private Button btnNextDay;

        [Header("Info Panel")]
        [SerializeField] private TMP_Text txtDay;
        [SerializeField] private TMP_Text txtTotalVisits;
        [SerializeField] private TMP_Text txtTotalIncome;
        [SerializeField] private TMP_Text txtTotalExpense;

        [Header("Net Profit Slot")]
        [SerializeField] private TMP_Text txtNetProfit;

        [Header("Animation")]
        [Tooltip("팝업 창이 열릴 때 재생될 커스텀 애니메이션 (Scale/Slide 등)")]
        [SerializeField] private Anim_InOutBase popupAnim;

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

            if (popupAnim != null)
            {
                // Slide나 Scale 등 설정된 커스텀 애니메이션의 In 효과 재생
                popupAnim.PlayIn().Forget();
            }

            var dailyRecord = global::GlobalManagers.Data.DailyRecord;
            var incomeDataSO = global::GlobalManagers.Data.IncomeDataSO;

            if (dailyRecord == null) return;

            if (txtDay != null)
            {
                int currentDay = global::GlobalManagers.Data.Current != null ? global::GlobalManagers.Data.Current.currentDay : 1;
                txtDay.text = $"{currentDay}일째";
            }

            // Info Panel (숫자 카운팅 애니메이션 실행)
            AnimateInfoPanel(dailyRecord).Forget();

            // 2. 기존 목록 초기화
            foreach (Transform child in itemContainer)
            {
                Destroy(child.gameObject);
            }

            if (incomeDataSO != null)
            {
                float slotAnimDelay = 0.3f; // 팝업 애니메이션 끝날 즈음 시작

                foreach (var entry in incomeDataSO.Entries)
                {
                    FacilityType type = entry.facilityType;
                    IncomeData data = entry.incomeData;

                    int count = 0;
                    if (dailyRecord.facilityUsageCounts.TryGetValue(type, out int c))
                    {
                        count = c;
                    }
                    
                    string resultTextName = string.IsNullOrEmpty(data.resultText) ? type.ToString() : data.resultText;
                    int costPerUse = data.incomeAmount;
                    int totalEarned = count * costPerUse;
                    Sprite icon = data.icon;

                    // 0회 이용이어도 무조건 표시합니다.
                    SalesSlotUI slot = CreateResultItem(icon, resultTextName, count, costPerUse, totalEarned);
                    if (slot != null)
                    {
                        slot.PlayAnimInAsync(slotAnimDelay).Forget();
                        slotAnimDelay += 0.15f; // 다음 슬롯은 0.15초 뒤에 올라옴
                    }
                }
            }

            // 시간 타이머를 멈추기 위해 Time.timeScale 조절
            Time.timeScale = 0f;
        }

        private async UniTaskVoid AnimateInfoPanel(DailyRecordModel record)
        {
            float duration = 0.5f;
            float elapsed = 0f;

            int targetVisits = record.totalNPCVisits;
            int targetIncome = record.totalIncome;
            int targetExpense = record.totalExpense;
            int targetNetProfit = targetIncome - targetExpense;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easeOutT = t * (2f - t);

                if (txtTotalVisits != null) txtTotalVisits.text = Mathf.RoundToInt(Mathf.Lerp(0, targetVisits, easeOutT)).ToComma("명");
                if (txtTotalIncome != null) txtTotalIncome.text = Mathf.RoundToInt(Mathf.Lerp(0, targetIncome, easeOutT)).ToComma("원");
                if (txtTotalExpense != null) txtTotalExpense.text = Mathf.RoundToInt(Mathf.Lerp(0, targetExpense, easeOutT)).ToComma("원");
                if (txtNetProfit != null) txtNetProfit.text = Mathf.RoundToInt(Mathf.Lerp(0, targetNetProfit, easeOutT)).ToComma("원");

                await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
            }

            if (txtTotalVisits != null) txtTotalVisits.text = targetVisits.ToComma("명");
            if (txtTotalIncome != null) txtTotalIncome.text = targetIncome.ToComma("원");
            if (txtTotalExpense != null) txtTotalExpense.text = targetExpense.ToComma("원");
            if (txtNetProfit != null) txtNetProfit.text = targetNetProfit.ToComma("원");
        }

        private SalesSlotUI CreateResultItem(Sprite icon, string name, int count, int costPerUse, int totalEarned)
        {
            if (resultItemPrefab == null || itemContainer == null) return null;

            GameObject go = Instantiate(resultItemPrefab, itemContainer);
            SalesSlotUI slotUI = go.GetComponent<SalesSlotUI>();
            if (slotUI != null)
            {
                slotUI.SetData(icon, name, count, costPerUse, totalEarned);
            }
            else
            {
                // 이전 구조의 텍스트 프리팹이 들어왔을 때를 대비한 예외처리
                TMP_Text txt = go.GetComponentInChildren<TMP_Text>();
                if (txt != null)
                {
                    txt.text = $"{name}: {count}회 x {costPerUse} (+{totalEarned:N0})";
                }
            }
            return slotUI;
        }

        private void OnNextDayClicked()
        {
            if (resultPanel != null) resultPanel.SetActive(false);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartNextDay();
            }
        }
    }
}
