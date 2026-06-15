using System.Collections.Generic;
using UnityEngine;
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
            
            // 모바일 및 EventSystem 기반 2D 터치 인터랙션을 위해 메인 카메라에 Physics2DRaycaster를 보장합니다.
            if (Camera.main != null && Camera.main.GetComponent<UnityEngine.EventSystems.Physics2DRaycaster>() == null)
            {
                Camera.main.gameObject.AddComponent<UnityEngine.EventSystems.Physics2DRaycaster>();
            }

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
        /// 씬에 이미 배치된 구조물을 매니저에 등록하고 초기화합니다.
        /// </summary>
        public void RegisterSceneFacility(GameObject go, FacilityData soData, int gridX, int gridY, float nodeSize)
        {
            if (soData == null) return;

            if (!_facilities.ContainsKey(soData.facilityType))
            {
                _facilities[soData.facilityType] = new List<FacilityBase>();
            }

            FacilityBase facInstance = go.GetComponent<FacilityBase>();
            if (facInstance == null)
            {
                facInstance = AddFacilityComponent(go, soData.facilityType);
            }

            facInstance.Initialize(soData, gridX, gridY, nodeSize);

            if (!_facilities[soData.facilityType].Contains(facInstance))
            {
                _facilities[soData.facilityType].Add(facInstance);
                Debug.Log($"[FacilityManager] 씬 배치된 {soData.facilityType} 등록 완료.");
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
                case FacilityType.BeverageDispenser: return go.AddComponent<BeverageDispenserFacility>();
                case FacilityType.DisposableDispenser: return go.AddComponent<DisposableDispenserFacility>();
                case FacilityType.TowelStorage: return go.AddComponent<TowelStorageFacility>();
                case FacilityType.TowelReturn: return go.AddComponent<TowelReturnFacility>();
                case FacilityType.BodyDryer: return go.AddComponent<BodyDryerFacility>();
                case FacilityType.Platform: return go.AddComponent<PlatformFacility>();
                case FacilityType.Shower: return go.AddComponent<ShowerFacility>();
                case FacilityType.Bath: return go.AddComponent<BathFacility>();
                case FacilityType.Sauna: return go.AddComponent<SaunaFacility>();
                case FacilityType.ScrubArea: return go.AddComponent<ScrubAreaFacility>();
                default: return go.AddComponent<BathFacility>();
            }
        }

        /// <summary>
        /// Returns a random AVAILABLE facility instance of a given type.
        /// </summary>
        public FacilityBase GetRandomAvailableFacility(FacilityType type, HashSet<FacilityBase> ignoredFacilities = null)
        {
            if (!_facilities.ContainsKey(type) || _facilities[type].Count == 0)
                return null;

            List<FacilityBase> availableFacilities = new List<FacilityBase>();

            foreach (var fac in _facilities[type])
            {
                // 무시 목록에 있으면 건너뜀
                if (ignoredFacilities != null && ignoredFacilities.Contains(fac)) continue;

                // 꽉 차지 않은 시설만 필터링
                if (!fac.CanEnter()) continue;

                availableFacilities.Add(fac);
            }

            if (availableFacilities.Count == 0) return null;

            int randomIndex = UnityEngine.Random.Range(0, availableFacilities.Count);
            return availableFacilities[randomIndex];
        }
    }
}
