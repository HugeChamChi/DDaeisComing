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

        private void Start()
        {
            if (trashBinData == null)
            {
                Debug.LogWarning($"[TrashBinController] {gameObject.name} 에 TrashBinDataSO가 할당되지 않았습니다.");
            }
            
            if (gaugeSlider != null)
            {
                // 사용자가 슬라이더를 조작하지 못하도록 비활성화
                gaugeSlider.interactable = false;
                if (trashBinData != null)
                {
                    gaugeSlider.maxValue = trashBinData.maxGauge;
                }
            }
            
            UpdateUI();
        }

        public override void ExitFacility(NPC_Base npc, int slotIndex)
        {
            base.ExitFacility(npc, slotIndex);

            // NPC가 이용을 마칠 때마다 쓰레기(게이지) 증가
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
            if (state.isMinigamePlaying) return;
            if (state.currentGauge <= 0) return; // 치울 쓰레기가 없으면 상호작용 안 함

            if (MiniGameManager.Instance != null)
            {
                state.isMinigamePlaying = true;
                MiniGameManager.Instance.ShowTrashBinMinigame(this);
            }
            else
            {
                Debug.LogError("[TrashBinController] MiniGameManager.Instance 가 없습니다.");
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
