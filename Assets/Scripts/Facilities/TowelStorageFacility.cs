using UnityEngine;

namespace Bathhouse.Facilities
{
    public class TowelStorageFacility : FacilityBase
    {
        // 자리가 찼다는 개념 없이 무제한으로 사용 가능하게 오버라이드
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
            return true; // 항상 성공
        }

        public override void EnterFacility(NPC.NPC_Base npc, int slotIndex)
        {
            base.EnterFacility(npc, slotIndex);
            Debug.Log($"[TowelStorage] NPC가 수건 보관함을 사용합니다.");
        }

        public override void ExitFacility(NPC.NPC_Base npc, int slotIndex)
        {
            base.ExitFacility(npc, slotIndex);
            Debug.Log($"[TowelStorage] NPC가 수건 보관함 사용을 마쳤습니다.");
        }
    }
}
