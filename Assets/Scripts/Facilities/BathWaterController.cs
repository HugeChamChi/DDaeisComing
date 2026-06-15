using UnityEngine;
using UnityEngine.UI;
using Bathhouse.Managers;

namespace Bathhouse.Facilities
{
    public class BathWaterController : MonoBehaviour
    {
        [Header("Target Bath")]
        [SerializeField] private BathFacility targetBath;

        [Header("UI References")]
        [SerializeField] private Slider pollutionSlider;
        [SerializeField] private Button interactionBubbleButton; // 말풍선 UI 버튼
        [Tooltip("버튼과 함께 표시/숨김할 Image (버튼 UI가 Image로 되어있는 경우 연결)")]
        [SerializeField] private Image interactionBubbleImage;   // 버튼과 함께 켜지고 꺼질 이미지

        private bool isMinigamePlaying = false;

        private void Start()
        {
            if (targetBath != null)
            {
                targetBath.OnPollutionChanged += HandlePollutionChanged;
                targetBath.OnPollutionFull += HandlePollutionFull;
            }
            
            if (interactionBubbleButton != null)
            {
                interactionBubbleButton.onClick.AddListener(OnBubbleClicked);
            }

            if (pollutionSlider != null && targetBath != null)
            {
                pollutionSlider.maxValue = targetBath.maxPollution;
                pollutionSlider.value = targetBath.currentPollution;
            }

            // 시작 시 현재 오염도에 맞춰 버튼 표시 여부를 결정 (이미 가득이면 바로 표시)
            UpdateButtonVisual();
        }

        private void OnDestroy()
        {
            if (targetBath != null)
            {
                targetBath.OnPollutionChanged -= HandlePollutionChanged;
                targetBath.OnPollutionFull -= HandlePollutionFull;
            }
        }

        private void HandlePollutionChanged(float current, float max)
        {
            if (pollutionSlider != null)
            {
                pollutionSlider.maxValue = max;
                pollutionSlider.value = current;
            }

            // 오염도가 바뀔 때마다 버튼 표시 여부를 상태에 맞게 갱신 (정화되면 자동 숨김)
            UpdateButtonVisual();
        }

        private void HandlePollutionFull()
        {
            UpdateButtonVisual();
        }

        /// <summary>
        /// 현재 오염도 상태에 따라 "채우기(정화)" 버튼의 표시 여부를 갱신합니다.
        /// 수건 보관함의 UpdateVisual과 동일하게, 필요할 때만 버튼이 떠 있도록 합니다.
        /// </summary>
        private void UpdateButtonVisual()
        {
            bool needsPurify = !isMinigamePlaying
                               && targetBath != null
                               && targetBath.currentPollution >= targetBath.maxPollution;

            if (interactionBubbleButton != null)
            {
                interactionBubbleButton.gameObject.SetActive(needsPurify);
            }

            if (interactionBubbleImage != null)
            {
                interactionBubbleImage.gameObject.SetActive(needsPurify);
            }
        }

        private void OnBubbleClicked()
        {
            // 미니게임을 띄우지 않고 바로 정화 처리
            // 정화되면 OnPollutionChanged → UpdateButtonVisual이 버튼을 자동으로 숨깁니다.
            if (targetBath != null)
            {
                targetBath.PurifyWater();
            }

            Debug.Log("[BathWaterController] 클릭 감지 - 수질이 즉시 정화되었습니다.");
        }

        private void OnMinigameSuccess()
        {
            isMinigamePlaying = false;
            
            if (targetBath != null)
            {
                targetBath.PurifyWater();
            }
            
            Debug.Log("[BathWaterController] 수질 관리 미니게임 성공! 수질이 정화되었습니다.");
        }

        private void OnMinigameFail()
        {
            isMinigamePlaying = false;
            
            // 실패 시 다시 말풍선을 띄울지, 아니면 패널티를 줄지 기획에 따라 다름.
            // 여기서는 다시 띄워주도록 처리 (무한 대기).
            UpdateButtonVisual();

            Debug.Log("[BathWaterController] 수질 관리 미니게임 실패. 다시 시도할 수 있습니다.");
        }
    }
}
