using UnityEngine;

namespace Bathhouse.Data
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Bathhouse/Data/GameConfig")]
    public class GameConfigSO : ScriptableObject
    {
        [Header("Day Settings")]
        [Tooltip("하루 총 영업 시간 (초)")]
        public float dayDurationSeconds = 180f;

        [Tooltip("영업 종료 몇 초 전부터 손님이 오지 않게 할지 임계값")]
        public float noCustomerTimeThreshold = 30f;
    }
}
