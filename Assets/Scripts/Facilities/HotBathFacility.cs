using UnityEngine;

namespace Bathhouse.Facilities
{
    /// <summary>
    /// 온탕 특화 로직이 들어가는 클래스입니다.
    /// 온탕은 사용 시 NPC의 온도를 올리거나 만족도에 가중치를 줄 수 있습니다.
    /// </summary>
    public class HotBathFacility : FacilityBase
    {
        public override void EnterFacility(NPC.NPC_Base npc, int slotIndex)
        {
            base.EnterFacility(npc, slotIndex);
            // TODO: NPC의 State를 '목욕 중'으로 변경, 특정 애니메이션 재생 등
            Debug.Log($"[HotBath] NPC가 온탕 {slotIndex}번 자리에 들어왔습니다. 현재 인원: {_currentUsers}/{_data.maxCapacity}");
        }

        public override void ExitFacility(NPC.NPC_Base npc, int slotIndex)
        {
            base.ExitFacility(npc, slotIndex);
            npc.Brain.bathCount++;
            Debug.Log($"[HotBath] NPC가 온탕 {slotIndex}번 자리에서 나갔습니다. 청결도 하락: {_currentCleanliness}");
        }

        public override void ProgressFacility(NPC.NPC_Base npc, int slotIndex, float deltaTime)
        {
            base.ProgressFacility(npc, slotIndex, deltaTime);
            // 매 프레임마다 만족도/온도 게이지 상승 등의 로직을 처리합니다.
            // Debug.Log($"[HotBath] {slotIndex}번 자리 NPC 목욕 중... (deltaTime: {deltaTime})");
        }
    }
}
