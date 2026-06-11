using UnityEngine;

namespace Bathhouse.Facilities
{
    public class DisposableDispenserFacility : FacilityBase
    {
        protected override void Awake()
        {
            base.Awake();
            // 구조물 내부 슬롯으로 텔레포트 하지 않고 제자리에서 상호작용하도록 설정
            teleportToSlotOnUse = false;
        }

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
