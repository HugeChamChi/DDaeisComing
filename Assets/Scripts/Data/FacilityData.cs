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


        [Header("Usage Settings")]
        public int maxCapacity = 1;       // 최대 수용 인원
        public float baseUseTime = 2f;    // 기본 이용 시간 (초)
        public float usageCooldown = 0f;  // 이용 후 재사용 대기 시간 (초)

        [Header("Cleanliness")]
        public float cleanlinessDropPerUse = 5f; // 한 명 사용할 때마다 깎이는 청결도

        [Header("Visual Settings")]
        [Tooltip("실제 씬에 배치될 프리팹 (SpriteRenderer, Animator 등 포함)")]
        public GameObject visualPrefab;

        [Tooltip("배치 시 위치 미세 조정을 위한 오프셋")]
        public Vector3 visualPosOffset = Vector3.zero;

        [Tooltip("배치 시 스케일 조정을 위한 오프셋")]
        public Vector3 visualScaleOffset = Vector3.one;

        public static event System.Action<FacilityData> OnDataChanged;

#if UNITY_EDITOR
        private void OnValidate()
        {
            OnDataChanged?.Invoke(this);
        }
#endif
    }
}
