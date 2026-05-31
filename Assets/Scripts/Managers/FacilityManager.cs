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

                // 1. FacilityData 로드
                FacilityData soData = null;
                _facilityDataCache.TryGetValue(facility.facilityType, out soData);

                GameObject go = null;
                
                // 2. 프리팹이 있으면 프리팹 생성, 없으면 기존처럼 빈 오브젝트 생성
                if (soData != null && soData.visualPrefab != null)
                {
                    go = Instantiate(soData.visualPrefab, this.transform);
                    go.name = $"[Facility] {facility.facilityType}";
                }
                else
                {
                    go = new GameObject($"[Facility] {facility.facilityType}");
                    go.transform.SetParent(this.transform);
                }

                // 3. 컴포넌트 확인 및 동적 추가 (프리팹에 이미 붙어있을 수도 있음)
                FacilityBase facInstance = go.GetComponent<FacilityBase>();
                if (facInstance == null)
                {
                    facInstance = AddFacilityComponent(go, facility.facilityType);
                }

                // 4. 초기화 및 위치 설정
                facInstance.Initialize(soData, facility.originX, facility.originY, nodeSize);
                // facInstance.Initialize 내부에서 RefreshPosition()이 호출됩니다.

                _facilities[facility.facilityType].Add(facInstance);
                Debug.Log($"[FacilityManager] {facility.facilityType} 생성 완료.");
            }
        }

        /// <summary>
        /// 타입에 맞는 Facility 컴포넌트를 동적으로 추가합니다.
        /// </summary>
        private FacilityBase AddFacilityComponent(GameObject go, FacilityType type)
        {
            switch (type)
            {
                case FacilityType.Counter: return go.AddComponent<CounterFacility>();
                case FacilityType.LockerRoom: return go.AddComponent<LockerRoomFacility>();
                case FacilityType.Shower: return go.AddComponent<ShowerFacility>();
                case FacilityType.HotBath: return go.AddComponent<HotBathFacility>();
                case FacilityType.Sauna: return go.AddComponent<SaunaFacility>();
                case FacilityType.ScrubArea: return go.AddComponent<ScrubAreaFacility>();
                default: return go.AddComponent<HotBathFacility>();
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
