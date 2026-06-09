using UnityEngine;

namespace Bathhouse.Facilities
{
    public class LockerRoomFacility : FacilityBase
    {
        [Header("Sprites")]
        public SpriteRenderer spriteRenderer;
        public Sprite openSprite;
        public Sprite closeSprite;

        public override void EnterFacility(NPC.NPC_Base npc, int slotIndex)
        {
            base.EnterFacility(npc, slotIndex);
            Debug.Log($"[LockerRoom] NPC가 락커룸({slotIndex}번 자리)에서 옷을 갈아입습니다.");
            // TODO: NPC 상태를 '나체(탈의)'로 변경
            UpdateSprite();
        }

        public override void ExitFacility(NPC.NPC_Base npc, int slotIndex)
        {
            base.ExitFacility(npc, slotIndex);
            // npc.Brain.hasVisitedLocker = true; (신규 라우트 시스템에서는 불필요)
            Debug.Log($"[LockerRoom] NPC가 락커룸에서 환복을 마쳤습니다.");
            UpdateSprite();
        }

        private void UpdateSprite()
        {
            if (spriteRenderer != null)
            {
                if (_currentUsers > 0 && openSprite != null)
                    spriteRenderer.sprite = openSprite;
                else if (_currentUsers == 0 && closeSprite != null)
                    spriteRenderer.sprite = closeSprite;
            }
        }
    }
}
