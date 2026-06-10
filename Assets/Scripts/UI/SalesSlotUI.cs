using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Bathhouse.Utils;
using Cysharp.Threading.Tasks;

namespace Bathhouse.UI
{
    public class SalesSlotUI : MonoBehaviour
    {
        [SerializeField] private Image imgIcon;
        [SerializeField] private TMP_Text txtName;
        [SerializeField] private TMP_Text txtCalculation; // 이용횟수 x 이용비용
        [SerializeField] private TMP_Text txtTotalCost;   // 전체 비용

        [Header("Animation Settings")]
        [SerializeField] private float animDuration = 0.5f;

        private int _targetCount;
        private int _costPerUse;
        private int _targetTotalEarned;

        public void SetData(Sprite icon, string name, int count, int costPerUse, int totalEarned)
        {
            if (imgIcon != null)
            {
                if (icon != null)
                {
                    imgIcon.sprite = icon;
                    imgIcon.gameObject.SetActive(true);
                }
                else
                {
                    // 아이콘이 없으면 이미지를 끕니다 (혹은 기본 이미지로 유지)
                    imgIcon.gameObject.SetActive(false);
                }
            }
            
            if (txtName != null) txtName.text = name;

            _targetCount = count;
            _costPerUse = costPerUse;
            _targetTotalEarned = totalEarned;

            // 초기값 0으로 세팅
            if (txtCalculation != null) txtCalculation.text = $"0회 x {costPerUse.ToComma("원")}";
            if (txtTotalCost != null) txtTotalCost.text = "+0원";
        }

        public async UniTask PlayAnimInAsync(float delaySeconds)
        {
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = 0f;

            if (delaySeconds > 0)
            {
                await UniTask.Delay(System.TimeSpan.FromSeconds(delaySeconds), ignoreTimeScale: true);
            }

            if (this == null) return;

            float elapsed = 0f;

            RectTransform rect = GetComponent<RectTransform>();
            Vector3 targetPos = rect.localPosition;
            Vector3 startPos = targetPos + new Vector3(0, -50f, 0);

            while (elapsed < animDuration)
            {
                if (this == null) return;

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / animDuration);
                float easeOutT = t * (2f - t); // Ease Out Quad

                canvasGroup.alpha = Mathf.Lerp(0f, 1f, easeOutT);
                
                // 숫자 카운팅 (0 -> 목표값)
                int currentCount = Mathf.RoundToInt(Mathf.Lerp(0f, _targetCount, easeOutT));
                int currentTotal = Mathf.RoundToInt(Mathf.Lerp(0f, _targetTotalEarned, easeOutT));

                if (txtCalculation != null) txtCalculation.text = $"{currentCount.ToComma("회")} x {_costPerUse.ToComma("원")}";
                if (txtTotalCost != null) txtTotalCost.text = $"+{currentTotal.ToComma("원")}";

                // PostLateUpdate에서 덮어씌워야 VerticalLayoutGroup과의 충돌을 피할 수 있습니다.
                rect.localPosition = Vector3.Lerp(startPos, targetPos, easeOutT);

                await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
            }

            if (this != null)
            {
                canvasGroup.alpha = 1f;
                // 애니메이션 완료 후 정확한 최종값 고정
                if (txtCalculation != null) txtCalculation.text = $"{_targetCount.ToComma("회")} x {_costPerUse.ToComma("원")}";
                if (txtTotalCost != null) txtTotalCost.text = $"+{_targetTotalEarned.ToComma("원")}";
            }
        }
    }
}
