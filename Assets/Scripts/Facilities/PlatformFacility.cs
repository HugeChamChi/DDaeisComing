using UnityEngine;
using Bathhouse.NPC;
using System.Collections.Generic;

namespace Bathhouse.Facilities
{
    public class PlatformFacility : FacilityBase
    {
        private Dictionary<NPC_Base, bool> _platformCompleted = new Dictionary<NPC_Base, bool>();

        public override void EnterFacility(NPC.NPC_Base npc, int slotIndex)
        {
            base.EnterFacility(npc, slotIndex);
            _platformCompleted[npc] = false;
            
            var interactor = npc.GetComponent<NPCInteractionController>();
            if (interactor != null)
            {
                interactor.OnInteractionResolved += HandleInteractionResolved;
            }
            Debug.Log($"[Platform] NPC가 단상 {slotIndex}번 자리를 사용합니다.");
        }

        private void HandleInteractionResolved(NPC_Base npc, bool isSuccess)
        {
            if (_platformCompleted.ContainsKey(npc))
            {
                _platformCompleted[npc] = isSuccess;
            }
        }

        protected override void RecordUsageAndIncome(NPC_Base npc = null)
        {
            // ExitFacility에서의 자동 호출 무시. 성공 여부 확인 후 직접 처리.
        }

        public override void ExitFacility(NPC.NPC_Base npc, int slotIndex)
        {
            base.ExitFacility(npc, slotIndex);

            var interactor = npc.GetComponent<NPCInteractionController>();
            if (interactor != null)
            {
                interactor.OnInteractionResolved -= HandleInteractionResolved;
            }

            if (_platformCompleted.ContainsKey(npc) && _platformCompleted[npc])
            {
                Debug.Log($"[Platform] {npc.Data.name} 만쥬(단상) 상호작용 성공! 통계/수익 반영");
                base.RecordUsageAndIncome(npc);
            }
            else
            {
                Debug.Log($"[Platform] {npc.Data.name} 만쥬(단상) 상호작용 실패 또는 타임아웃! (통계 반영 안함)");
            }

            _platformCompleted.Remove(npc);

            Debug.Log($"[Platform] NPC가 단상 {slotIndex}번 자리 사용을 마쳤습니다.");
        }
    }
}
