using UnityEngine;

namespace Bathhouse.Data
{
    [CreateAssetMenu(fileName = "New Interaction Reward", menuName = "Bathhouse/NPC/Interaction Reward")]
    public class InteractionRewardSO : ScriptableObject
    {
        [Tooltip("말풍선 유지 시간 (초)")]
        public float timeLimit = 4.0f;

        [Tooltip("타이밍 4분할에 따른 보상 배열 (인덱스 0이 가장 빠름, 3이 가장 늦음)")]
        public int[] rewardTiers = new int[4] { 10, 5, -5, -10 };
    }
}
