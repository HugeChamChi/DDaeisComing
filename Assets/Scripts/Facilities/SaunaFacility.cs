using UnityEngine;

namespace Bathhouse.Facilities
{
    public class SaunaFacility : FacilityBase
    {
        public override void EnterFacility(NPC.NPC_Base npc, int slotIndex)
        {
            base.EnterFacility(npc, slotIndex);
            Debug.Log($"[Sauna] NPC가 사우나({slotIndex}번 자리)에서 땀을 뺍니다.");
            // TODO: 땀 흘리는 이펙트, 스트레스 대폭 감소 등
        }

        public override void ExitFacility(NPC.NPC_Base npc, int slotIndex)
        {
            base.ExitFacility(npc, slotIndex);
            npc.Brain.bathCount++;
            Debug.Log($"[Sauna] NPC가 사우나 이용을 마쳤습니다.");
        }
    }
}
