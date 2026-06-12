using UnityEngine;

namespace Bathhouse.Facilities
{
    public class ShowerFacility : FacilityBase
    {
        public event System.Action OnShowerUsed;

        protected override void Awake()
        {
            base.Awake();
            if (gameObject.GetComponent<TidyUpInteractionTrigger>() == null)
            {
                gameObject.AddComponent<TidyUpInteractionTrigger>();
            }
        }

        public override void EnterFacility(NPC.NPC_Base npc, int slotIndex)
        {
            base.EnterFacility(npc, slotIndex);
            Debug.Log($"[Shower] NPC가 샤워기({slotIndex}번 자리)에서 몸을 씻습니다.");
            // TODO: 샤워 이펙트, 청결도 상승 로직
        }

        public override void ExitFacility(NPC.NPC_Base npc, int slotIndex)
        {
            base.ExitFacility(npc, slotIndex);
            // npc.Brain.hasShowered = true; (신규 라우트 시스템에서는 불필요)
            // npc.Brain.bathCount++;
            Debug.Log($"[Shower] NPC 샤워 완료.");
            OnShowerUsed?.Invoke();
        }

        public override void ProgressFacility(NPC.NPC_Base npc, int slotIndex, float deltaTime)
        {
            base.ProgressFacility(npc, slotIndex, deltaTime);
            // 매 프레임마다 청결도 증가
        }
    }
}
