using UnityEngine;

namespace Bathhouse.Facilities
{
    public class CounterFacility : FacilityBase
    {
        public override void EnterFacility(NPC.NPC_Base npc, int slotIndex)
        {
            base.EnterFacility(npc, slotIndex);
            // NPC의 진행 상태에 따라 '입장'인지 '퇴장 정산'인지 구분 가능
            Debug.Log($"[Counter] NPC가 카운터({slotIndex}번 자리)에서 용무(입장 또는 퇴장 정산)를 시작합니다.");
        }

        public override void ExitFacility(NPC.NPC_Base npc, int slotIndex)
        {
            base.ExitFacility(npc, slotIndex);
            Debug.Log($"[Counter] NPC 용무 완료. (퇴장 정산이었다면 밖으로 나갑니다)");
            // TODO: 만약 최종 퇴장 정산이었다면 NPC를 삭제(Destroy)하거나 스폰 포인트 밖으로 걸어 나가게 처리
        }
    }
}
