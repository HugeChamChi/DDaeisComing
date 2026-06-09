using UnityEngine;

namespace Bathhouse.Facilities
{
    public class BeverageDispenserFacility : FacilityBase
    {
        public override void EnterFacility(NPC.NPC_Base npc, int slotIndex)
        {
            base.EnterFacility(npc, slotIndex);
            Debug.Log($"[BeverageDispenser] NPC가 음료수 자판기 {slotIndex}번 자리를 사용합니다.");
        }

        public override void ExitFacility(NPC.NPC_Base npc, int slotIndex)
        {
            base.ExitFacility(npc, slotIndex);
            Debug.Log($"[BeverageDispenser] NPC가 음료수 자판기 {slotIndex}번 자리 사용을 마쳤습니다.");
        }
    }
}
