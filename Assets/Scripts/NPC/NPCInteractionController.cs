using System;
using System.Threading;
using UnityEngine;
using Bathhouse.Data;
// using VContainer; // DI

namespace Bathhouse.NPC
{
    /// <summary>
    /// NPC의 만족도 상호작용 시스템을 제어하는 컨트롤러.
    /// 대상 구조물에서 말풍선 활성화 및 타이밍 클릭 결과를 처리합니다.
    /// </summary>
    public class NPCInteractionController : MonoBehaviour
    {
        [SerializeField] private NPC_Base _npcBase;
        
        private Bathhouse.UI.NPCInteractionSpeechBubbleUI _speechBubbleUI;
        
        private CancellationTokenSource _interactionCts;
        private float _currentTimer;
        private float _maxTimer;
        private bool _isInteracting;

        private void Awake()
        {
            if (_npcBase == null)
                _npcBase = GetComponent<NPC_Base>();
        }

        /// <summary>
        /// 특정 시설 이용 시 상호작용 시작
        /// </summary>
        public async Cysharp.Threading.Tasks.UniTaskVoid StartInteractionAsync(FacilityType currentFacility, float usageTime)
        {
            var rewardData = Bathhouse.Managers.InteractionManager.Instance?.GlobalRewardData;

            // 대상 구조물 확인: 카운터, 수건 보관함, 단상, 때밀이 등
            if (!IsInteractionTarget(currentFacility) || rewardData == null)
            {
                return;
            }

            // 이전 상호작용이 아직 덜 끝났다면 강제 종료하여 말풍선을 먼저 반환
            if (_isInteracting)
            {
                EndInteraction();
            }

            _interactionCts?.Cancel();
            _interactionCts = new CancellationTokenSource();

            _isInteracting = true;
            _maxTimer = usageTime;
            _currentTimer = usageTime;
            
            if (Bathhouse.Managers.InteractionManager.Instance != null)
            {
                Bathhouse.Data.InteractionBubbleType bubbleType = GetBubbleTypeFromFacility(currentFacility);
                _speechBubbleUI = Bathhouse.Managers.InteractionManager.Instance.GetBubble(
                    transform, 
                    OnSpeechBubbleClicked, 
                    bubbleType
                );
            }
            
            Debug.Log($"[{_npcBase.Data.name}] 말풍선 상호작용 시작! ({usageTime}초)");

            try
            {
                // 제한 시간 동안 카운트다운 (UniTask 기반 비동기 대기)
                while (_currentTimer > 0 && _isInteracting)
                {
                    _currentTimer -= Time.deltaTime;
                    
                    // UI 진행률 업데이트 로직
                    if (_speechBubbleUI != null)
                    {
                        _speechBubbleUI.UpdateFill(_currentTimer / _maxTimer);
                    }
                    await Cysharp.Threading.Tasks.UniTask.Yield();
                    _interactionCts.Token.ThrowIfCancellationRequested();
                }

                if (_isInteracting)
                {
                    // 타임아웃
                    ProcessTimeout();
                }
            }
            catch (OperationCanceledException)
            {
                // 취소됨
            }
            finally
            {
                EndInteraction();
            }
        }

        private bool IsInteractionTarget(FacilityType facility)
        {
            return facility == FacilityType.Counter ||
                   facility == FacilityType.TowelStorage ||
                   facility == FacilityType.TowelReturn ||
                   // ScrubArea는 ScrubAreaFacility에서 직접 말풍선을 띄우므로 중복을 피하기 위해 제외합니다.
                   facility == FacilityType.Platform ||
                   facility == FacilityType.DisposableDispenser;
        }

        private Bathhouse.Data.InteractionBubbleType GetBubbleTypeFromFacility(FacilityType facility)
        {
            switch (facility)
            {
                case FacilityType.Counter: return Bathhouse.Data.InteractionBubbleType.Counter;
                case FacilityType.TowelStorage:
                case FacilityType.TowelReturn: return Bathhouse.Data.InteractionBubbleType.Towel;
                case FacilityType.Platform: return Bathhouse.Data.InteractionBubbleType.Platform;
                default: return Bathhouse.Data.InteractionBubbleType.Counter; // 기본값
            }
        }

        /// <summary>
        /// 플레이어가 말풍선을 클릭했을 때 호출 (외부 UI 버튼 등에서 연결)
        /// </summary>
        public void OnSpeechBubbleClicked()
        {
            if (!_isInteracting) return;
            
            var rewardData = Bathhouse.Managers.InteractionManager.Instance?.GlobalRewardData;
            if (rewardData == null) return;
            
            _isInteracting = false;
            _speechBubbleUI = null; // UI가 애니메이션 후 스스로 풀로 반환되므로 참조 해제
            
            // Phase 판별
            float timeRatio = _currentTimer / _maxTimer;
            int phase = GetPhaseFromRatio(timeRatio);
            int reward = rewardData.rewardTiers[phase];

            ApplySatisfactionReward(reward, phase);
            
            // 말풍선을 클릭하면 대기 시간을 즉시 건너뛰고 다음 행동으로 넘어감
            if (_npcBase != null)
            {
                _npcBase.ForceFinishCurrentAction();
            }
            
            _interactionCts?.Cancel();
        }

        private void ProcessTimeout()
        {
            var rewardData = Bathhouse.Managers.InteractionManager.Instance?.GlobalRewardData;
            if (rewardData == null) return;

            _isInteracting = false;
            int timeoutPhase = 3; // 가장 늦은 Phase
            int penalty = rewardData.rewardTiers[timeoutPhase];
            
            Debug.Log($"[{_npcBase.Data.name}] 상호작용 타임아웃!");
            ApplySatisfactionReward(penalty, timeoutPhase);
        }

        private int GetPhaseFromRatio(float ratio)
        {
            if (ratio >= 0.75f) return 0;
            if (ratio >= 0.50f) return 1;
            if (ratio >= 0.25f) return 2;
            return 3;
        }

        private void ApplySatisfactionReward(int amount, int phase)
        {
            string colorTag = amount > 0 ? "<color=#00FF00>" : (amount < 0 ? "<color=#FF0000>" : "<color=#FFFFFF>");
            Debug.Log($"{colorTag}[{_npcBase.Data.name}] 상호작용 완료! Phase: {phase}, 보상: {amount}</color>");
            
            // 글로벌 만족도 시스템에 보상/패널티 전달
            if (Bathhouse.Managers.InteractionManager.Instance != null)
            {
                Bathhouse.Managers.InteractionManager.Instance.AddSatisfaction(amount);
            }
            
            // NPC 자체 런타임 만족도 갱신
            if (_npcBase != null)
            {
                float convertedAmount = amount / 100f; // 임의 스케일링
                _npcBase.AddSatisfaction(convertedAmount);
            }
        }

        private void EndInteraction()
        {
            _isInteracting = false;
            if (_speechBubbleUI != null && Bathhouse.Managers.InteractionManager.Instance != null)
            {
                Bathhouse.Managers.InteractionManager.Instance.ReturnBubble(_speechBubbleUI);
                _speechBubbleUI = null;
            }
        }

        private void OnDestroy()
        {
            _interactionCts?.Cancel();
            _interactionCts?.Dispose();
        }
    }
}
