using UnityEngine;

namespace Bathhouse.Facilities
{
    /// <summary>
    /// 온탕 특화 로직이 들어가는 클래스입니다.
    /// 온탕은 사용 시 NPC의 온도를 올리거나 만족도에 가중치를 줄 수 있습니다.
    /// </summary>
    public class BathFacility : FacilityBase
    {
        [Header("Pollution Settings")]
        [SerializeField] private float pollutionPerUse = 20f;
        public float currentPollution { get; private set; } = 0f;
        public float maxPollution { get; private set; } = 100f;

        public event System.Action<float, float> OnPollutionChanged;
        public event System.Action OnPollutionFull;

        public override void EnterFacility(NPC.NPC_Base npc, int slotIndex)
        {
            base.EnterFacility(npc, slotIndex);
            // TODO: NPC의 State를 '목욕 중'으로 변경, 특정 애니메이션 재생 등
            Debug.Log($"[Bath] NPC가 탕 {slotIndex}번 자리에 들어왔습니다. 현재 인원: {_currentUsers}/{_data.maxCapacity}");
        }

        public override void ExitFacility(NPC.NPC_Base npc, int slotIndex)
        {
            base.ExitFacility(npc, slotIndex);
            // npc.Brain.bathCount++; (신규 라우트 시스템에서는 불필요)
            Debug.Log($"[Bath] NPC가 탕 {slotIndex}번 자리에서 나갔습니다. 청결도 하락: {_currentCleanliness}");
            
            AddPollution(pollutionPerUse);
        }

        public override void ProgressFacility(NPC.NPC_Base npc, int slotIndex, float deltaTime)
        {
            base.ProgressFacility(npc, slotIndex, deltaTime);
            // 매 프레임마다 만족도/온도 게이지 상승 등의 로직을 처리합니다.
            // Debug.Log($"[Bath] {slotIndex}번 자리 NPC 목욕 중... (deltaTime: {deltaTime})");
        }

        public void AddPollution(float amount)
        {
            if (currentPollution >= maxPollution) return;

            currentPollution += amount;
            if (currentPollution >= maxPollution)
            {
                currentPollution = maxPollution;
                OnPollutionFull?.Invoke();
            }

            OnPollutionChanged?.Invoke(currentPollution, maxPollution);
        }

        public void PurifyWater()
        {
            currentPollution = 0f;
            OnPollutionChanged?.Invoke(currentPollution, maxPollution);
        }
    }
}
