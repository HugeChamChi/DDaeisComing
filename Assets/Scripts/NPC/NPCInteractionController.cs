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
        [SerializeField] private InteractionRewardSO _rewardData;
        [SerializeField] private NPC_Base _npcBase;
        
        [Header("UI Reference")]
        [SerializeField] private Bathhouse.UI.NPCInteractionSpeechBubbleUI _speechBubbleUI;
        
        private CancellationTokenSource _interactionCts;
        private float _currentTimer;
        private bool _isInteracting;

        // DI를 통해 주입받을 수 있는 Global Satisfaction System 이벤트나 참조가 있다고 가정
        // [Inject] private IGlobalSatisfactionSystem _satisfactionSystem;

        private void Awake()
        {
            if (_npcBase == null)
                _npcBase = GetComponent<NPC_Base>();
                
            if (_speechBubbleUI != null)
                _speechBubbleUI.Init(this);
                
            SetBubbleActive(false);
        }

        public void SetRewardData(InteractionRewardSO rewardData)
        {
            _rewardData = rewardData;
        }

        /// <summary>
        /// 특정 시설 이용 시 상호작용 시작
        /// </summary>
        public async System.Threading.Tasks.Task StartInteractionAsync(FacilityType currentFacility)
        {
            // 대상 구조물 확인: 카운터, 수건 보관함, 단상, 때밀이 등
            if (!IsInteractionTarget(currentFacility) || _rewardData == null)
            {
                return;
            }

            _interactionCts?.Cancel();
            _interactionCts = new CancellationTokenSource();

            _isInteracting = true;
            _currentTimer = _rewardData.timeLimit;
            
            SetBubbleActive(true);
            
            Debug.Log($"[{_npcBase.Data.name}] 말풍선 상호작용 시작! ({_rewardData.timeLimit}초)");

            try
            {
                // 제한 시간 동안 카운트다운 (UniTask 기반 비동기 대기)
                while (_currentTimer > 0 && _isInteracting)
                {
                    _currentTimer -= Time.deltaTime;
                    
                    // UI 진행률 업데이트 로직
                    if (_speechBubbleUI != null)
                    {
                        _speechBubbleUI.UpdateFill(_currentTimer / _rewardData.timeLimit);
                    }
                    
                    await System.Threading.Tasks.Task.Yield();
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
                   facility == FacilityType.ScrubArea ||
                   facility == FacilityType.Platform; // 만쥬 등
        }

        /// <summary>
        /// 플레이어가 말풍선을 클릭했을 때 호출 (외부 UI 버튼 등에서 연결)
        /// </summary>
        public void OnSpeechBubbleClicked()
        {
            if (!_isInteracting) return;
            
            _isInteracting = false;
            
            // Phase 판별
            float timeRatio = _currentTimer / _rewardData.timeLimit;
            int phase = GetPhaseFromRatio(timeRatio);
            int reward = _rewardData.rewardTiers[phase];

            ApplySatisfactionReward(reward, phase);
            
            _interactionCts?.Cancel();
        }

        private void ProcessTimeout()
        {
            _isInteracting = false;
            int timeoutPhase = 3; // 가장 늦은 Phase
            int penalty = _rewardData.rewardTiers[timeoutPhase];
            
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
            Debug.Log($"[{_npcBase.Data.name}] 상호작용 완료! Phase: {phase}, 보상: {amount}");
            
            // TODO: Global 만족도 시스템에 보상/패널티 전달
            // if (_satisfactionSystem != null) _satisfactionSystem.AddSatisfaction(amount);
            
            // NPC 자체 만족도 갱신
            if (_npcBase != null && _npcBase.Data != null)
            {
                // 기존 베이스에 반영 (실제 런타임용 인스턴스 데이터 구조가 있다면 거기에 반영)
                float convertedAmount = amount / 100f; // 임의 스케일링
                _npcBase.Data.baseSatisfactionLevel = Mathf.Clamp01(_npcBase.Data.baseSatisfactionLevel + convertedAmount);
            }
        }

        private void EndInteraction()
        {
            _isInteracting = false;
            SetBubbleActive(false);
        }

        private void SetBubbleActive(bool isActive)
        {
            if (_speechBubbleUI != null)
            {
                if (isActive)
                    _speechBubbleUI.Show();
                else
                    _speechBubbleUI.Hide();
            }
        }

        private void OnDestroy()
        {
            _interactionCts?.Cancel();
            _interactionCts?.Dispose();
        }
    }
}
