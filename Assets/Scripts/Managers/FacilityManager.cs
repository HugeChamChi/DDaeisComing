using UnityEngine;
using System.Collections.Generic;
using Bathhouse.Data;
using Bathhouse.Utils;
using Bathhouse.Facilities;

namespace Bathhouse.Managers
{
    /// <summary>
    /// SRP: Manages and provides access to all facility locations.
    /// Inherits from NotDontDestroyOnLoad so it resets per scene.
    /// </summary>
    public class FacilityManager : MonoSingleton_NotDontDestroyOnLoad<FacilityManager>
    {
        private Dictionary<FacilityType, List<FacilityBase>> _facilities = new Dictionary<FacilityType, List<FacilityBase>>();
        private Dictionary<FacilityType, FacilityData> _facilityDataCache = new Dictionary<FacilityType, FacilityData>();

        protected override void Init()
        {
            base.Init();
            
            // Resources/Facilities 폴더 내의 모든 FacilityData 에셋을 로드합니다.
            var dataList = Resources.LoadAll<FacilityData>("Facilities");
            foreach (var d in dataList)
            {
                _facilityDataCache[d.facilityType] = d;
            }
            Debug.Log($"[FacilityManager] {dataList.Length}개의 FacilityData를 로드했습니다.");
        }

        public void InitializeFromMapData(MapData mapData, float nodeSize)
        {
            _facilities.Clear();
            if (mapData == null || mapData.facilities == null) return;

            foreach (var facility in mapData.facilities)
            {
                if (!_facilities.ContainsKey(facility.facilityType))
                {
                    _facilities[facility.facilityType] = new List<FacilityBase>();
                }

                // 나중에 프리팹을 붙일 수 있도록 임시 빈 오브젝트에 해당 컴포넌트들을 달아줍니다.
                GameObject go = new GameObject($"[Facility] {facility.facilityType}");
                go.transform.SetParent(this.transform);

                // 컴포넌트를 동적으로 붙여줍니다.
                FacilityBase facInstance = null;
                switch (facility.facilityType)
                {
                    case FacilityType.Counter: facInstance = go.AddComponent<CounterFacility>(); break;
                    case FacilityType.LockerRoom: facInstance = go.AddComponent<LockerRoomFacility>(); break;
                    case FacilityType.Shower: facInstance = go.AddComponent<ShowerFacility>(); break;
                    case FacilityType.HotBath: facInstance = go.AddComponent<HotBathFacility>(); break;
                    case FacilityType.Sauna: facInstance = go.AddComponent<SaunaFacility>(); break;
                    case FacilityType.ScrubArea: facInstance = go.AddComponent<ScrubAreaFacility>(); break;
                    default: facInstance = go.AddComponent<HotBathFacility>(); break; // fallback
                }

                // Interaction Point 계산 (구조물 중앙 하단)
                float centerX = facility.originX + (facility.sizeX - 1) / 2f;
                float interactY = facility.originY - 1f;

                Vector3 interactPos = new Vector3(
                    centerX * nodeSize + (nodeSize / 2f),
                    interactY * nodeSize + (nodeSize / 2f),
                    0f
                );
                
                go.transform.position = interactPos;

                // 캐시된 FacilityData가 있으면 넘겨줌
                FacilityData soData = null;
                if (_facilityDataCache.TryGetValue(facility.facilityType, out soData))
                {
                    facInstance.Initialize(soData, facility.originX, facility.originY, nodeSize);
                }
                else
                {
                    Debug.LogWarning($"[FacilityManager] {facility.facilityType}에 해당하는 FacilityData를 Resources/Facilities 폴더에서 찾지 못했습니다.");
                    facInstance.Initialize(null, facility.originX, facility.originY, nodeSize);
                }

                _facilities[facility.facilityType].Add(facInstance);
                Debug.Log($"[FacilityManager] {facility.facilityType} 인스턴스화 완료. 상호작용 좌표: {interactPos}");
            }
        }

        /// <summary>
        /// Returns the nearest AVAILABLE facility instance of a given type.
        /// </summary>
        public FacilityBase GetNearestAvailableFacility(FacilityType type, Vector3 currentPos)
        {
            if (!_facilities.ContainsKey(type) || _facilities[type].Count == 0)
                return null;

            FacilityBase nearest = null;
            float minDistance = float.MaxValue;

            foreach (var fac in _facilities[type])
            {
                // 꽉 차지 않은 시설만 필터링
                if (!fac.CanEnter()) continue;

                float dist = Vector3.Distance(currentPos, fac.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearest = fac;
                }
            }

            return nearest;
        }
    }
}
