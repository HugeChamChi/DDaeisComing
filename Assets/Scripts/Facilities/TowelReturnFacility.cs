using UnityEngine;
using UnityEngine.UI;
using Bathhouse.Managers;
using Bathhouse.UI;
using Bathhouse.Data;

namespace Bathhouse.Facilities
{
    public class TowelReturnFacility : FacilityBase
    {
        [Header("Towel Settings")]
        public int currentTowels = 0;
        public int maxTowels = 10;
        [Tooltip("체크 시 가득 찼을 때 바닥에 수건을 스폰합니다.")]
        [SerializeField] private bool spawnDroppedTowel = true;
        [SerializeField] private GameObject droppedTowelPrefab;
        [SerializeField] private Slider gaugeSlider;
        [SerializeField] private Button clickButton;

        protected override void Awake()
        {
            base.Awake();
            if (gaugeSlider != null)
            {
                gaugeSlider.maxValue = maxTowels + 4; // maxCapacity + 4까지 허용
                gaugeSlider.value = currentTowels;
            }

            if (clickButton != null)
            {
                clickButton.onClick.AddListener(OnFacilityClicked);
            }
        }

        private void OnFacilityClicked()
        {
            if (currentTowels >= maxTowels + 4)
            {
                Debug.Log("[TowelReturn] TODO: 미니게임 실행");
                // TODO: 미니게임 호출 로직
            }
            else
            {
                EmptyStorage();
            }
        }

        public override bool CanEnter()
        {
            return _data != null;
        }

        public override int GetAvailableSlotIndex()
        {
            return 0; // 항상 0번 반환
        }

        public override bool ReserveSlot(NPC.NPC_Base npc, out int slotIndex)
        {
            slotIndex = 0;
            return true; // 일단 시설 접근은 허용 (EnterFacility에서 예외 처리)
        }

        public override void EnterFacility(NPC.NPC_Base npc, int slotIndex)
        {
            base.EnterFacility(npc, slotIndex);
            
            currentTowels++;
            UpdateGauge();
            
            if (currentTowels > maxTowels)
            {
                Debug.Log("[TowelReturn] 수거함이 꽉 차서 NPC가 수건을 바닥에 버립니다.");
                
                // NPC 불만족 패널티 부여
                npc.AddSatisfaction(-0.1f);
                if (GameManager.Interaction != null)
                {
                    GameManager.Interaction.AddSatisfaction(-5);
                }

                // DroppedTowel 인스턴스화
                if (spawnDroppedTowel)
                {
                    InstantiateDroppedTowel(npc.transform.position);
                }
            }
            else
            {
                Debug.Log($"[TowelReturn] 수건 정상 반납. 현재 {currentTowels}/{maxTowels}");
            }
        }

        public override void ExitFacility(NPC.NPC_Base npc, int slotIndex)
        {
            base.ExitFacility(npc, slotIndex);
            Debug.Log($"[TowelReturn] NPC가 수건 반납함 사용을 마쳤습니다.");
        }

        private void UpdateGauge()
        {
            if (gaugeSlider != null)
            {
                gaugeSlider.value = currentTowels;
            }
        }

        public void EmptyStorage()
        {
            currentTowels = 0;
            UpdateGauge();
            Debug.Log("[TowelReturn] 수건 수거함을 비웠습니다.");
        }

        protected virtual void Start()
        {
            if (Bathhouse.Managers.GameManager.Instance != null)
            {
                Bathhouse.Managers.GameManager.Instance.OnNextDayStarted += EmptyStorage;
            }
        }

        protected virtual void OnDestroy()
        {
            if (Bathhouse.Managers.GameManager.Instance != null)
            {
                Bathhouse.Managers.GameManager.Instance.OnNextDayStarted -= EmptyStorage;
            }
        }

        private void InstantiateDroppedTowel(Vector3 position)
        {
            if (droppedTowelPrefab == null)
            {
                Debug.LogWarning("[TowelReturn] droppedTowelPrefab is not assigned.");
                return;
            }

            Vector3 spawnPosition = position + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0);
            GameObject towelObj = Instantiate(droppedTowelPrefab, spawnPosition, Quaternion.identity);
        }
    }
}
