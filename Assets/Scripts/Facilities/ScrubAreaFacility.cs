using Bathhouse.UI;
using Bathhouse.Data;
using Bathhouse.Managers;
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

        private Dictionary<NPC_Base, NPCInteractionSpeechBubbleUI> _activeBubbles = new Dictionary<NPC_Base, NPCInteractionSpeechBubbleUI>();
        private Dictionary<NPC_Base, bool> _scrubCompleted = new Dictionary<NPC_Base, bool>();

        public override void EnterFacility(NPC_Base npc, int slotIndex)
        {
            base.EnterFacility(npc, slotIndex);
            
            _scrubCompleted[npc] = false;

            if (GameManager.Interaction != null)
            {
                var bubbleUI = GameManager.Interaction.GetBubble(
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
                    InteractionBubbleType.Scrub
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
            
            // 씬에서 못 찾았을 경우 MiniGameManager를 통해 찾기 시도
            if (ui == null)
            {
                var manager = FindObjectOfType<MiniGameManager>(true);
                if (manager != null && manager.scrubMiniGame != null)
                {
                    if (manager.scrubMiniGame.gameObject.scene.rootCount == 0) // 프리팹인 경우
                    {
                        ui = Instantiate(manager.scrubMiniGame);
                    }
                    else
                    {
                        ui = manager.scrubMiniGame;
                    }
                }
            }

            if (ui != null)
            {
                Debug.Log("[ScrubAreaFacility] 때밀이 미니게임 UI를 찾았습니다. 게임을 시작합니다.");
                // TODO: 추후 GameManager나 DayManager에서 실제 현재 날짜(Day)를 가져와야 함. 일단 1일차로 고정.
                int currentDay = 1; 
                ui.StartMiniGame(npc, currentDay, 
                    onSuccess: () => OnMiniGameSuccess(npc),
                    onFail: () => OnMiniGameFail(npc));
            }
            else
            {
                Debug.LogError("[ScrubAreaFacility] ScrubMiniGameUI를 씬에서 찾을 수 없어 미니게임을 스킵하고 자동 성공 처리합니다!");
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
                if (_activeBubbles[npc] != null && GameManager.Interaction != null) 
                {
                    GameManager.Interaction.ReturnBubble(_activeBubbles[npc]);
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
