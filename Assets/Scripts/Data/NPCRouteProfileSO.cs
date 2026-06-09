using UnityEngine;
using System.Collections.Generic;

namespace Bathhouse.Data
{
    [System.Serializable]
    public struct RouteStep
    {
        public FacilityType structureType;
        [Tooltip("필수 동선 여부. true이면 반드시 방문.")]
        public bool isMandatory;
        [Tooltip("선택 동선일 경우 기본 방문 확률 (0.0 ~ 1.0)")]
        [Range(0f, 1f)]
        public float baseProbability;
    }

    public enum InteractionType
    {
        Counter,
        Towel,
        Manju,
        Scrub
    }

    [System.Serializable]
    public struct SatisfactionInteraction
    {
        public InteractionType interactionType;
        [Tooltip("제한 시간")]
        public float totalTimeLimit;
        [Tooltip("만족도 점수 배열 (4칸)")]
        public int[] satisfactionScores;
    }

    [CreateAssetMenu(fileName = "New Route Profile", menuName = "Bathhouse/NPC/Route Profile")]
    public class NPCRouteProfileSO : ScriptableObject
    {
        [Tooltip("NPC가 순차적으로 방문할 구조물의 목록")]
        public List<RouteStep> routeSteps = new List<RouteStep>();
        
        [Tooltip("만족도 상호작용 목록")]
        public List<SatisfactionInteraction> satisfactionInteractions = new List<SatisfactionInteraction>();
    }
}
