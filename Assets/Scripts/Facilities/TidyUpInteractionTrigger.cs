using UnityEngine;
using Bathhouse.Managers;
using Bathhouse.Data;
using Bathhouse.UI;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

namespace Bathhouse.Facilities
{
    /// <summary>
    /// 좌식 샤워기를 NPC가 N번 이용할 때마다 '정리정돈' 말풍선 상호작용을 띄우는 컴포넌트
    /// </summary>
    public class TidyUpInteractionTrigger : MonoBehaviour
    {
        [Header("Tidy Up Settings")]
        public int triggerCountThreshold = 3;
        
        [Header("State (ReadOnly)")]
        [SerializeField] private int currentUseCount = 0;
        [SerializeField] private bool isInteractionActive = false;

        private ShowerFacility showerFacility;
        private NPCInteractionSpeechBubbleUI activeBubble;

        private void Awake()
        {
            showerFacility = GetComponent<ShowerFacility>();
            if (showerFacility == null)
            {
                Debug.LogWarning("[TidyUpInteractionTrigger] 동일한 GameObject 내에 ShowerFacility가 없습니다.");
            }
        }

        private void Start()
        {
            if (showerFacility != null)
            {
                showerFacility.OnShowerUsed += HandleShowerUsed;
            }
        }

        private void OnDestroy()
        {
            if (showerFacility != null)
            {
                showerFacility.OnShowerUsed -= HandleShowerUsed;
            }
        }

        private void HandleShowerUsed()
        {
            currentUseCount++;
            if (currentUseCount >= triggerCountThreshold && !isInteractionActive)
            {
                TriggerTidyUpInteraction();
            }
        }

        private void TriggerTidyUpInteraction()
        {
            isInteractionActive = true;
            
            if (GameManager.Interaction != null)
            {
                activeBubble = GameManager.Interaction.GetBubble(
                    transform, 
                    OnTidyUpClicked, 
                    InteractionBubbleType.ShowerTidyUp
                );

                if (activeBubble != null)
                {
                    // 타이머가 없으므로 게이지를 꽉 찬 상태로 유지 (원치 않으면 제거)
                    activeBubble.UpdateFill(1f);
                }
            }
        }

        private void OnTidyUpClicked()
        {
            if (!isInteractionActive) return;

            isInteractionActive = false;

            Debug.Log("[TidyUpInteractionTrigger] 샤워기 정리정돈 완료 (성공)");
            
            var rewardData = GameManager.Interaction?.GlobalRewardData;
            if (rewardData != null)
            {
                int reward = rewardData.rewardTiers[0]; 
                GameManager.Interaction.AddSatisfaction(reward);
            }

            ResetInteractionState();
        }

        private void ResetInteractionState()
        {
            currentUseCount = 0;
            isInteractionActive = false;
            activeBubble = null;
        }
    }
}
