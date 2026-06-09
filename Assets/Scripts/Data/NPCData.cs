using UnityEngine;
using System.Collections.Generic;

namespace Bathhouse.Data
{
    [CreateAssetMenu(fileName = "New NPC Data", menuName = "Bathhouse/NPC Data")]
    public class NPCData : ScriptableObject
    {
        [Header("Movement")]
        public float moveSpeed = 2f;

        [Header("Action Durations (Seconds)")]
        public float showerTime = 3f;
        public float bathTime = 5f;
        public float scrubTime = 4f;

        [Header("Preference Weights")]
        [Tooltip("Weights for random facility choice after locker room.")]
        public int weightShower = 20;
        public int weightBath = 10;
        public int weightSauna = 5;
        public int weightScrubArea = 5;
        public int weightBeverageDispenser = 5;

        [Header("Satisfaction")]
        [Tooltip("NPC의 기본 만족도 상태치. 확률적 요소 계산 시 가중치로 사용됨.")]
        public float baseSatisfactionLevel = 0f;
        
        [Tooltip("만족도가 100%일 때 추가되는 방문 확률의 최대치 (기본 0.5 = 최대 50% 추가 확률)")]
        public float satisfactionProbabilityWeight = 0.5f;
    }
}
