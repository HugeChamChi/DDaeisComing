using UnityEngine;

namespace Bathhouse.Facilities
{
    public class LockerRoomFacility : FacilityBase
    {
        public override void EnterFacility(NPC.NPC_Base npc, int slotIndex)
        {
            base.EnterFacility(npc, slotIndex);
            Debug.Log($"[LockerRoom] NPC가 락커룸({slotIndex}번 자리)에서 옷을 갈아입습니다.");
            // TODO: NPC 상태를 '나체(탈의)'로 변경
        }

        public override void ExitFacility(NPC.NPC_Base npc, int slotIndex)
        {
            base.ExitFacility(npc, slotIndex);
            npc.Brain.hasVisitedLocker = true;
            Debug.Log($"[LockerRoom] NPC가 락커룸에서 환복을 마쳤습니다.");
        }
    }
}
