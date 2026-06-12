using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Bathhouse.Data;
using Bathhouse.Managers;
using Bathhouse.NPC;

namespace Bathhouse.Facilities
{
    public class TrashBinController : FacilityBase, IPointerClickHandler
    {
        [Header("Data")]
        [SerializeField] private TrashBinDataSO trashBinData;

        [Header("State")]
        [SerializeField] private TrashBinState state = new TrashBinState();

        [Header("World UI")]
        [Tooltip("쓰레기통 상단 World Canvas의 게이지 Slider")]
        [SerializeField] private Slider gaugeSlider;

        [Tooltip("쓰레기통 전체를 덮는 투명 버튼 (클릭 영역 RectTransform)")]
        [SerializeField] private Button clickButton;

        private void Start()
        {
            if (trashBinData == null)
            {
                Debug.LogWarning($"[TrashBinController] {gameObject.name} 에 TrashBinDataSO가 할당되지 않았습니다.");
            }
            
            if (gaugeSlider != null)
            {
                gaugeSlider.interactable = false;
                if (trashBinData != null)
                {
                    gaugeSlider.maxValue = trashBinData.maxGauge;
                }
            }
            
            if (clickButton != null)
            {
                clickButton.onClick.AddListener(TriggerMinigame);
            }

            if (GameManager.MiniGame != null && Bathhouse.Managers.GameManager.Instance != null)
            {
                // GameManager.MiniGame.RegisterTrashBin(this);
                Bathhouse.Managers.GameManager.Instance.OnNextDayStarted += ReduceGauge;
            }

            UpdateUI();
        }

        private void OnDestroy()
        {
            if (Bathhouse.Managers.GameManager.Instance != null)
            {
                Bathhouse.Managers.GameManager.Instance.OnNextDayStarted -= ReduceGauge;
            }
        }

        public override void ExitFacility(NPC_Base npc, int slotIndex)
        {
            base.ExitFacility(npc, slotIndex);

            if (trashBinData != null)
            {
                state.currentGauge += trashBinData.increaseAmountPerUse;
                state.currentGauge = Mathf.Clamp(state.currentGauge, 0f, trashBinData.maxGauge);
            }

            UpdateUI();
        }

        private void UpdateUI()
        {
            if (gaugeSlider != null && trashBinData != null)
            {
                gaugeSlider.value = state.currentGauge;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // UI를 클릭했을 때 실행됨
            TriggerMinigame();
        }

        private void TriggerMinigame()
        {
            if (state.isMinigamePlaying) return;
            if (state.currentGauge <= 0) return; // 치울 쓰레기가 없으면 상호작용 안 함

            if (GameManager.MiniGame != null)
            {
                state.isMinigamePlaying = true;
                GameManager.MiniGame.ShowTrashBinMinigame(this);
            }
            else
            {
                Debug.LogError("[TrashBinController] GameManager.MiniGame 가 없습니다.");
            }
        }

        public void ReduceGauge()
        {
            // 미니게임 클리어 시 쓰레기통 비우기
            state.currentGauge = 0f;
            
            UpdateUI();
            Debug.Log($"[TrashBinController] 미니게임 클리어! 쓰레기통을 비웠습니다. 현재 게이지: {state.currentGauge}");
        }

        public void SetMinigamePlaying(bool isPlaying)
        {
            state.isMinigamePlaying = isPlaying;
        }

        public TrashBinDataSO GetTrashBinData()
        {
            return trashBinData;
        }

        public TrashBinState GetState()
        {
            return state;
        }
    }
}
