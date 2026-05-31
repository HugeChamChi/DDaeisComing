using UnityEngine;
using UnityEngine.Tilemaps;
using Bathhouse.Grid;
using Bathhouse.Pathfinding;

namespace Bathhouse.Managers
{
    /// <summary>
    /// 유니티 Tilemap을 기반으로 NPC 길찾기를 테스트하기 위한 런처 스크립트입니다.
    /// 이 스크립트는 씬 시작 시 Tilemap을 스캔하여 A* 그리드를 구성하고, 스포너를 가동시킵니다.
    /// </summary>
    public class TilemapTestRunner : MonoBehaviour
    {
        [Header("Tilemap References")]
        [Tooltip("이동 가능한 바닥 타일이 그려진 타일맵 (Walkable)")]
        [SerializeField] private Tilemap floorTilemap;
        
        [Tooltip("벽, 가구 등 지나갈 수 없는 타일이 그려진 타일맵 (Obstacle)")]
        [SerializeField] private Tilemap obstacleTilemap;

        [Header("System References")]
        [Tooltip("NPC를 생성하고 관리할 스포너")]
        [SerializeField] private NPCSpawner npcSpawner;

        [Header("Debug")]
        public bool showDebugGrid = true;

        private void Start()
        {
            // 1. 타일맵 예외 처리: 타일맵이 할당되지 않았다면 에러를 띄웁니다.
            if (floorTilemap == null || npcSpawner == null)
            {
                Debug.LogError("[TilemapTestRunner] 필수 컴포넌트(FloorTilemap, NPCSpawner)가 할당되지 않았습니다! 인스펙터를 확인하세요.");
                return;
            }

            Debug.Log("[TilemapTestRunner] Tilemap 기반 길찾기 테스트 환경을 초기화합니다...");

            // 2. TilemapGridMap 생성: 두 타일맵을 넘겨주면, 내부적으로 Bounds를 계산하고 노드 배열을 만듭니다.
            IGridMap tilemapGrid = new TilemapGridMap(floorTilemap, obstacleTilemap);

            // 3. NPC 스포너 초기화: 생성한 타일맵 그리드와 임시 스폰 좌표(0,0)를 주입합니다.
            npcSpawner.Initialize(tilemapGrid, 0, 0);

            // 4. 스폰 시작: NPC가 정해진 간격에 따라 생성되고 시설물로 이동을 시작합니다.
            npcSpawner.StartSpawning();
            
            Debug.Log("[TilemapTestRunner] 테스트 시작 완료. NPC 스폰을 개시합니다.");
        }
        
        /// <summary>
        /// (테스트용) 에디터 상에서 현재 구성된 맵의 갈 수 있는 영역과 갈 수 없는 영역을 시각적으로 보여줍니다.
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!showDebugGrid || !Application.isPlaying || floorTilemap == null) return;
            
            // 런타임 중에는 IGridMap을 직접 시각화해볼 수 있습니다.
            // 여기서는 성능을 위해 간단히 Bounds 기반으로 체크합니다.
            BoundsInt bounds = floorTilemap.cellBounds;
            
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    Vector3Int localPlace = new Vector3Int(x, y, 0);
                    
                    bool hasFloor = floorTilemap.HasTile(localPlace);
                    bool hasObstacle = obstacleTilemap != null && obstacleTilemap.HasTile(localPlace);
                    
                    if (hasFloor)
                    {
                        Vector3 worldPos = floorTilemap.CellToWorld(localPlace) + floorTilemap.cellSize / 2f;
                        Gizmos.color = hasObstacle ? new Color(1, 0, 0, 0.5f) : new Color(0, 1, 0, 0.3f);
                        Gizmos.DrawCube(worldPos, floorTilemap.cellSize * 0.9f);
                    }
                }
            }
        }
    }
}
