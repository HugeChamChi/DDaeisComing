using System.Collections.Generic;
using UnityEngine;

namespace Bathhouse.Data
{
    public enum InteractionBubbleType
    {
        Counter = 0,
        Scrub = 1,
        Towel = 2,
        Platform = 3,
        TowelReturn = 4,
        ShowerTidyUp = 5,
        // 필요에 따라 추가
    }

    [System.Serializable]
    public struct InteractionIconMapping
    {
        public InteractionBubbleType type;
        public Sprite icon;
    }

    [CreateAssetMenu(fileName = "New Interaction Reward", menuName = "Bathhouse/NPC/Interaction Reward")]
    public class InteractionRewardSO : ScriptableObject
    {
        [Tooltip("타이밍 4분할에 따른 보상 배열 (인덱스 0이 가장 빠름, 3이 가장 늦음)")]
        public int[] rewardTiers = new int[4] { 10, 5, -5, -10 };

        [Header("Icons")]
        public List<InteractionIconMapping> iconMappings;

        public Sprite GetIcon(InteractionBubbleType type)
        {
            if (iconMappings != null)
            {
                foreach (var mapping in iconMappings)
                {
                    if (mapping.type == type) return mapping.icon;
                }
            }
            return null;
        }
    }
}
