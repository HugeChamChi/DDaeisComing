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
                interactionBubbleButton.gameObject.SetActive(false);
                interactionBubbleButton.onClick.AddListener(OnBubbleClicked);
            }

            if (pollutionSlider != null && targetBath != null)
            {
                pollutionSlider.maxValue = targetBath.maxPollution;
                pollutionSlider.value = targetBath.currentPollution;
            }
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
        }

        private void HandlePollutionFull()
        {
            if (interactionBubbleButton != null && !isMinigamePlaying)
            {
                interactionBubbleButton.gameObject.SetActive(true);
            }
        }

        private void OnBubbleClicked()
        {
            if (interactionBubbleButton != null)
            {
                interactionBubbleButton.gameObject.SetActive(false);
            }

            // 미니게임을 띄우지 않고 바로 정화 처리
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
            if (interactionBubbleButton != null && targetBath != null && targetBath.currentPollution >= targetBath.maxPollution)
            {
                interactionBubbleButton.gameObject.SetActive(true);
            }

            Debug.Log("[BathWaterController] 수질 관리 미니게임 실패. 다시 시도할 수 있습니다.");
        }
    }
}
