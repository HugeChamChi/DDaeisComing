using UnityEngine;
using UnityEngine.Tilemaps;
using Bathhouse.Grid;
using Bathhouse.Tools;
using Bathhouse.Managers;
using Bathhouse.Pathfinding;

namespace Bathhouse.Test
{
    /// <summary>
    /// 씬(에디터)에서 배치한 구조물과 타일맵을 읽어와 길찾기 및 게임 사이클을 테스트하는 런처
    /// </summary>
    public class SceneTestRunner : MonoBehaviour
    {
        [Header("Scene Builder")]
        public SceneFacilityBuilder facilityBuilder;

        [Header("Unit Spawning")]
        public NPCSpawner npcSpawner;

        private void Start()
        {
            if (facilityBuilder == null || npcSpawner == null)
            {
                Debug.LogError("[SceneTestRunner] 필수 컴포넌트(SceneFacilityBuilder, NPCSpawner)가 설정되지 않았습니다.");
                return;
            }

            Debug.Log("[SceneTestRunner] 씬 기반 테스트 환경 초기화 시작...");

            // 1. SceneFacilityBuilder 데이터를 MapData 형태로 변환하여 GridMap 생성
            Bathhouse.Data.MapData mapData = new Bathhouse.Data.MapData
            {
                width = facilityBuilder.gridWidth,
                height = facilityBuilder.gridHeight,
                nodeSize = facilityBuilder.nodeSize,
                tiles = facilityBuilder.tiles
            };
            
            IGridMap gridMap = new CustomGridMap(mapData, facilityBuilder.transform.position);

            // 2. 씬에 배치된 전체 구조물들(SceneFacilityInfo)을 찾아 FacilityManager에 등록
            var placedFacilities = FindObjectsOfType<SceneFacilityInfo>();
            int facilityCount = 0;
            
            foreach (var info in placedFacilities)
            {
                if (info.facilityData != null)
                {
                    // FacilityManager를 통해 씬에 있는 객체를 등록 (초기화 및 컴포넌트 추가 수행)
                    FacilityManager.Instance.RegisterSceneFacility(
                        info.gameObject, 
                        info.facilityData, 
                        info.gridX, 
                        info.gridY, 
                        facilityBuilder.nodeSize
                    );
                    facilityCount++;
                }
            }
            Debug.Log($"[SceneTestRunner] 총 {facilityCount}개의 구조물을 씬에서 찾아 등록했습니다.");

            // 3. NPC 스포너 시작 (MapConfig에 저장된 spawnGridPos 참조)
            npcSpawner.Initialize(gridMap, facilityBuilder.mapConfig.spawnGridPos.x, facilityBuilder.mapConfig.spawnGridPos.y);
            npcSpawner.StartSpawning();

            Debug.Log("[SceneTestRunner] 테스트 준비 완료. NPC 스폰을 시작합니다.");
        }
    }
}
