using UnityEngine;
using Bathhouse.NPC;
using Bathhouse.MiniGames;
using System;
using System.Collections.Generic;

namespace Bathhouse.Facilities
{
    public class ScrubAreaFacility : FacilityBase
    {
        [Header("Scrub Settings")]
        [Tooltip("NPC 머리 위에 띄울 '때밀이 요청' UI 프리팹 (WorldSpace Canvas + Button 권장)")]
        public GameObject interactionBubblePrefab; 

        private Dictionary<NPC_Base, Bathhouse.UI.NPCInteractionSpeechBubbleUI> _activeBubbles = new Dictionary<NPC_Base, Bathhouse.UI.NPCInteractionSpeechBubbleUI>();
        private Dictionary<NPC_Base, bool> _scrubCompleted = new Dictionary<NPC_Base, bool>();

        public override void EnterFacility(NPC_Base npc, int slotIndex)
        {
            base.EnterFacility(npc, slotIndex);
            
            _scrubCompleted[npc] = false;

            if (Bathhouse.Managers.InteractionManager.Instance != null)
            {
                var bubbleUI = Bathhouse.Managers.InteractionManager.Instance.GetBubble(
                    npc.transform,
                    () => 
                    {
                        // 유저가 터치하면 버블 매니저 참조 제거 (UI 스스로가 반환될 것이므로)
                        if (_activeBubbles.ContainsKey(npc))
                        {
                            _activeBubbles.Remove(npc);
                        }
                        
                        // 미니게임 플레이 중에는 대기 시간(타임아웃)이 흐르지 않도록 일시정지!
                        npc.SetActionTimerPaused(true);
                        
                        StartScrubMiniGame(npc);
                    },
                    Bathhouse.Data.InteractionBubbleType.Scrub
                );

                if (bubbleUI != null)
                {
                    _activeBubbles[npc] = bubbleUI;
                }
            }
        }

        private void StartScrubMiniGame(NPC_Base npc)
        {
            var ui = FindObjectOfType<ScrubMiniGameUI>(true);
            if (ui != null)
            {
                // TODO: 추후 GameManager나 DayManager에서 실제 현재 날짜(Day)를 가져와야 함. 일단 1일차로 고정.
                int currentDay = 1; 
                ui.StartMiniGame(npc, currentDay, 
                    onSuccess: () => OnMiniGameSuccess(npc),
                    onFail: () => OnMiniGameFail(npc));
            }
            else
            {
                OnMiniGameSuccess(npc);
            }
        }

        private void OnMiniGameFail(NPC_Base npc)
        {
            _scrubCompleted[npc] = false;
            npc.ForceFinishCurrentAction(); // 실패 시 강제 종료
        }

        private void OnMiniGameSuccess(NPC_Base npc)
        {
            _scrubCompleted[npc] = true;
            npc.ForceFinishCurrentAction(); // 성공 시 강제 종료
        }

        public override void ExitFacility(NPC_Base npc, int slotIndex)
        {
            base.ExitFacility(npc, slotIndex);

            // 지정된 시간(baseUseTime)동안 유저가 터치 안해서 그냥 나가는 경우 버블 삭제 처리
            if (_activeBubbles.ContainsKey(npc))
            {
                if (_activeBubbles[npc] != null && Bathhouse.Managers.InteractionManager.Instance != null) 
                {
                    Bathhouse.Managers.InteractionManager.Instance.ReturnBubble(_activeBubbles[npc]);
                }
                _activeBubbles.Remove(npc);
            }

            if (_scrubCompleted.ContainsKey(npc) && _scrubCompleted[npc])
            {
                // npc.Brain.bathCount++; (신규 라우트 시스템에서는 불필요)
                Debug.Log($"[ScrubArea] {npc.Data.name} 때밀이 완료! (요금 지불함)");
                // 추후 코스트 지불 로직 추가
            }
            else
            {
                Debug.Log($"[ScrubArea] {npc.Data.name} 때밀이를 아무도 안 해줘서 화나서 그냥 나감! (요금 지불 안함)");
            }

            _scrubCompleted.Remove(npc);
        }

        // GetUsageTime()은 Base 클래스 것을 그대로 사용하여 _data.baseUseTime 만큼 기다리게 함
    }
}
