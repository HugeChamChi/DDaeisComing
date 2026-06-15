using UnityEngine;
using UnityEngine.UI;
using Bathhouse.Facilities;
using Bathhouse.Managers;

namespace Bathhouse.UI
{
    /// <summary>
    /// 쓰레기통 게이지가 가득 찼을 때 "청소" 말풍선 버튼/이미지를 표시하고,
    /// 클릭하면 미니게임을 띄우는 UI 컨트롤러입니다.
    /// BathWaterController와 동일한 "필요할 때 버튼 뜨고 → 클릭하면 처리" 패턴이며,
    /// TrashBinController는 수정하지 않고 public 접근자를 통해 상태를 폴링합니다.
    /// </summary>
    public class TrashBinButtonController : MonoBehaviour
    {
        [Header("Target TrashBin")]
        [SerializeField] private TrashBinController targetTrashBin;

        [Header("UI References")]
        [SerializeField] private Button interactionBubbleButton; // 말풍선 UI 버튼
        [Tooltip("버튼과 함께 표시/숨김할 Image (버튼 UI가 Image로 되어있는 경우 연결)")]
        [SerializeField] private Image interactionBubbleImage;   // 버튼과 함께 켜지고 꺼질 이미지

        private void Start()
        {
            if (interactionBubbleButton != null)
            {
                interactionBubbleButton.onClick.AddListener(OnBubbleClicked);
            }

            UpdateButtonVisual();
        }

        private void Update()
        {
            // TrashBinController에 이벤트가 없으므로, 게이지 상태를 매 프레임 확인하여
            // 말풍선 표시 여부를 갱신합니다. (욕탕과 동일한 표시/숨김 동작)
            UpdateButtonVisual();
        }

        /// <summary>
        /// 현재 게이지 상태에 따라 "청소" 말풍선 버튼/이미지의 표시 여부를 갱신합니다.
        /// 게이지가 가득 찼고 미니게임 중이 아닐 때만 표시합니다.
        /// </summary>
        private void UpdateButtonVisual()
        {
            bool needsCleanup = false;

            if (targetTrashBin != null)
            {
                var data = targetTrashBin.GetTrashBinData();
                var state = targetTrashBin.GetState();

                if (data != null && state != null && data.maxGauge > 0f)
                {
                    needsCleanup = !state.isMinigamePlaying
                                   && state.currentGauge >= data.maxGauge;
                }
            }

            if (interactionBubbleButton != null)
            {
                interactionBubbleButton.gameObject.SetActive(needsCleanup);
            }

            if (interactionBubbleImage != null)
            {
                interactionBubbleImage.gameObject.SetActive(needsCleanup);
            }
        }

        private void OnBubbleClicked()
        {
            if (targetTrashBin == null) return;

            var state = targetTrashBin.GetState();
            if (state == null || state.isMinigamePlaying || state.currentGauge <= 0f) return;

            // 미니게임 100% 실행 (욕탕의 즉시 정화 자리에 해당)
            if (GameManager.MiniGame != null)
            {
                targetTrashBin.SetMinigamePlaying(true);
                GameManager.MiniGame.ShowTrashBinMinigame(targetTrashBin);
                Debug.Log("[TrashBinButtonController] 말풍선 클릭 - 쓰레기통 미니게임을 실행합니다.");
            }
            else
            {
                Debug.LogWarning("[TrashBinButtonController] GameManager.MiniGame 가 없습니다.");
            }
        }
    }
}
