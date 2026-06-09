using UnityEngine;

namespace Bathhouse.Facilities
{
    public class TowelStorageFacility : FacilityBase
    {
        public override void EnterFacility(NPC.NPC_Base npc, int slotIndex)
        {
            base.EnterFacility(npc, slotIndex);
            Debug.Log($"[TowelStorage] NPC가 수건 보관함 {slotIndex}번 자리를 사용합니다.");
        }

        public override void ExitFacility(NPC.NPC_Base npc, int slotIndex)
        {
            base.ExitFacility(npc, slotIndex);
            Debug.Log($"[TowelStorage] NPC가 수건 보관함 {slotIndex}번 자리 사용을 마쳤습니다.");
        }
    }
}
