using UnityEngine;
using Bathhouse.Grid;
using Bathhouse.Data;
using Bathhouse.Pathfinding;
using System.IO;
using System.Collections.Generic;

namespace Bathhouse.Managers
{
    /// <summary>
    /// 타일맵을 쓰지 않고(JSON 파싱 기반의 CustomGridMap 사용) 
    /// 길찾기와 NPC 로직을 테스트하기 위한 런처입니다.
    /// </summary>
    public class CustomGridTestRunner : MonoBehaviour
    {
        [Header("Map Settings")]
        [Tooltip("테스트에 사용할 맵 데이터 JSON 파일명 (Assets/Resources 안의 파일)")]
        public string jsonFileName = "DefaultMap";
        public int fallbackWidth = 15;
        public int fallbackHeight = 15;
        public float nodeSize = 1f;

        [Header("System References")]
        [Tooltip("NPC를 생성하고 관리할 스포너")]
        [SerializeField] private NPCSpawner npcSpawner;

        private CustomGridMap _gridMap;

        private void Start()
        {
            if (npcSpawner == null)
            {
                Debug.LogError("[CustomGridTestRunner] NPCSpawner가 연결되지 않았습니다.");
                return;
            }

            Debug.Log("[CustomGridTestRunner] CustomGrid 기반 테스트 환경을 초기화합니다...");

            // 1. JSON 맵 데이터 로드 시도 (없으면 빈 맵 생성)
            MapData mapData = LoadMapData();

            // 1.5 FacilityManager 동적 초기화 (구조물 좌표 및 상호작용 지점 등록)
            FacilityManager.Instance.InitializeFromMapData(mapData, nodeSize);

            // 2. CustomGridMap 인스턴스 생성 (World 원점을 Vector3.zero로 가정)
            _gridMap = new CustomGridMap(mapData, Vector3.zero);

            // 2.5 모든 해상도에서 맵이 한눈에 보이도록 메인 카메라 자동 조절
            FitCameraToGrid(mapData);

            // 3. 스포너에 그리드 맵 주입 (JSON에 저장된 SpawnPoint X, Y 좌표도 함께 전달)
            npcSpawner.Initialize(_gridMap, mapData.spawnX, mapData.spawnY);

            // 4. 스포너 가동 시작
            npcSpawner.StartSpawning();
            
            Debug.Log("[CustomGridTestRunner] 테스트 시작 완료. NPC가 소환됩니다!");
        }

        private MapData LoadMapData()
        {
            TextAsset jsonText = Resources.Load<TextAsset>(jsonFileName);
            if (jsonText != null)
            {
                Debug.Log($"[CustomGridTestRunner] '{jsonFileName}' 파일을 성공적으로 로드했습니다.");
                return JsonUtility.FromJson<MapData>(jsonText.text);
            }

            Debug.LogWarning($"[CustomGridTestRunner] Resources/{jsonFileName}.json 파일을 찾을 수 없어 {fallbackWidth}x{fallbackHeight} 크기의 기본 빈 맵을 생성합니다.");
            MapData fallbackData = new MapData
            {
                width = fallbackWidth,
                height = fallbackHeight,
                nodeSize = nodeSize,
                tiles = new List<GridTileData>()
            };

            // 기본적으로 모든 맵을 이동 가능(Walkable) 상태로 채움
            for (int x = 0; x < fallbackWidth; x++)
            {
                for (int y = 0; y < fallbackHeight; y++)
                {
                    fallbackData.tiles.Add(new GridTileData { x = x, y = y, isWalkable = true });
                }
            }
            return fallbackData;
        }

        private void FitCameraToGrid(MapData mapData)
        {
            if (Camera.main == null || !Camera.main.orthographic) return;

            // 맵 전체 월드 크기 계산
            float mapWorldWidth = mapData.width * nodeSize;
            float mapWorldHeight = mapData.height * nodeSize;

            // 카메라 중심을 맵의 중앙으로 이동
            Camera.main.transform.position = new Vector3(mapWorldWidth / 2f, mapWorldHeight / 2f, -10f);

            // 현재 게임 화면 비율
            float screenRatio = (float)Screen.width / (float)Screen.height;
            float targetRatio = mapWorldWidth / mapWorldHeight;

            float paddingMultiplier = 1.1f; // 10% 여백

            if (screenRatio >= targetRatio)
            {
                // 가로가 화면보다 길거나 같으면 세로 기준 맞춤
                Camera.main.orthographicSize = (mapWorldHeight / 2f) * paddingMultiplier;
            }
            else
            {
                // 화면이 너무 홀쭉해서 가로가 짤릴 위험이 있으면, orthographicSize를 더 키움
                float sizeDifference = targetRatio / screenRatio;
                Camera.main.orthographicSize = (mapWorldHeight / 2f) * sizeDifference * paddingMultiplier;
            }
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || _gridMap == null) return;

            // 디버깅 용도로 현재 그리드 상태를 시각화합니다.
            for (int x = 0; x < _gridMap.Width; x++) 
            {
                for (int y = 0; y < _gridMap.Height; y++)
                {
                    Node node = _gridMap.GetNode(x, y);
                    if (node == null) break;

                    Gizmos.color = node.IsWalkable ? new Color(0, 1, 0, 0.2f) : new Color(1, 0, 0, 0.5f);
                    // 2D 환경이므로 X, Y 방향으로 면적을 그리고 Z는 조금 뒤로 밀어줍니다.
                    Gizmos.DrawCube(node.WorldPosition + Vector3.forward * 0.1f, new Vector3(nodeSize * 0.9f, nodeSize * 0.9f, 0.1f));
                }
            }
        }
    }
}
