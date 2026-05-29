using UnityEngine;

namespace Bathhouse.Data
{
    [CreateAssetMenu(fileName = "New Facility Data", menuName = "Bathhouse/Facility Data")]
    public class FacilityData : ScriptableObject
    {
        public FacilityType facilityType;
        public string facilityName = "New Facility";
        
        [Header("Grid Size (타일 칸 수)")]
        [Min(1)] public int width = 1;
        [Min(1)] public int height = 1;

        [Header("Interaction Settings")]
        [Tooltip("NPC가 이 시설을 이용하기 위해 실제로 서야 하는 타일의 상대 좌표 (구조물의 좌하단 기준)")]
        public Vector2Int[] interactionOffsets = new Vector2Int[] { new Vector2Int(0, -1) };

        [Tooltip("NPC가 시설을 '사용 중'일 때 내부에서 실제로 머무는 자리(Slot) 상대 좌표")]
        public Vector2Int[] usageOffsets = new Vector2Int[0];

        [Header("Usage Settings")]
        public int maxCapacity = 1;       // 최대 수용 인원
        public float baseUseTime = 2f;    // 기본 이용 시간 (초)
        public int cost = 100;            // 이용 요금 / 수익

        [Header("Cleanliness")]
        public float cleanlinessDropPerUse = 5f; // 한 명 사용할 때마다 깎이는 청결도
    }
}
