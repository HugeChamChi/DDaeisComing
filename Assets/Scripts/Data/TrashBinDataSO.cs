using UnityEngine;

namespace Bathhouse.Data
{
    public enum MinigameDifficulty
    {
        Easy,
        Normal,
        Hard
    }

    /// <summary>
    /// 쓰레기통의 기본 스펙 및 미니게임 난이도 정의
    /// </summary>
    [CreateAssetMenu(fileName = "TrashBinData", menuName = "Bathhouse/Facilities/TrashBinData", order = 1)]
    public class TrashBinDataSO : ScriptableObject
    {
        [Tooltip("최대 쓰레기 갯수 (슬라이더 최대값 및 미니게임 최대 쓰레기 갯수)")]
        public float maxGauge = 8f;
        
        [Tooltip("NPC가 이용할 때마다 증가하는 쓰레기 갯수")]
        public float increaseAmountPerUse = 1f;
        
        [Tooltip("미니게임 난이도")]
        public MinigameDifficulty difficulty = MinigameDifficulty.Normal;
    }
}
