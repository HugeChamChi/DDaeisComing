using UnityEngine;

namespace Bathhouse.Facilities
{
    public class DisposableDispenserFacility : FacilityBase
    {
        public override void EnterFacility(NPC.NPC_Base npc, int slotIndex)
        {
            base.EnterFacility(npc, slotIndex);
            Debug.Log($"[DisposableDispenser] NPC가 일회용품 디스펜서 {slotIndex}번 자리를 사용합니다.");
        }

        public override void ExitFacility(NPC.NPC_Base npc, int slotIndex)
        {
            base.ExitFacility(npc, slotIndex);
            Debug.Log($"[DisposableDispenser] NPC가 일회용품 디스펜서 {slotIndex}번 자리 사용을 마쳤습니다.");
        }
    }
}
