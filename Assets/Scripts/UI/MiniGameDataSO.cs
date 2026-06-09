using UnityEngine;

namespace Bathhouse.MiniGames
{
    [CreateAssetMenu(fileName = "MiniGameData", menuName = "Bathhouse/MiniGame Data")]
    public class MiniGameDataSO : ScriptableObject
    {
        [Header("Base Settings")]
        public float baseDifficultyWeight = 1.0f;
        [Tooltip("하루(Day)가 지날 때마다 추가되는 난이도 (예: 0.1이면 매일 10%씩 어려워짐)")]
        public float difficultyIncreasePerDay = 0.1f;
        [Tooltip("최대로 도달할 수 있는 난이도 상한선")]
        public float maxDifficultyWeight = 3.0f;
        [Tooltip("기본 제한 시간")]
        public float baseTimeLimit = 10f;
    }
}
