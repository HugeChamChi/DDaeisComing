using UnityEngine;
using Bathhouse.Data;
using System.IO;
using System.Collections.Generic;

namespace Bathhouse.Tools
{
    /// <summary>
    /// 인게임(런타임)에서 마우스 클릭으로 맵을 편집하고 저장할 수 있는 컴포넌트입니다.
    /// 테스트 씬이나 유저 커스텀 맵 빌더에 사용할 수 있습니다.
    /// </summary>
    public class RuntimeMapEditor : MonoBehaviour
    {
        [Header("Grid Config")]
        public int gridWidth = 20;
        public int gridHeight = 20;
        public float nodeSize = 1f;

        [Header("Save/Load")]
        public string fileName = "UserCustomMap.json";

        private MapData _mapData;
        private Camera _mainCam;

        private void Start()
        {
            _mainCam = Camera.main;
            InitializeEmptyMap();
        }

        private void InitializeEmptyMap()
        {
            _mapData = new MapData
            {
                width = gridWidth,
                height = gridHeight,
                nodeSize = nodeSize,
                tiles = new List<GridTileData>(),
                facilities = new List<FacilityPlacementData>()
            };

            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    _mapData.tiles.Add(new GridTileData { x = x, y = y, isWalkable = true });
                }
            }
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0)) // 좌클릭으로 타일 Walkable 토글
            {
                Vector3 mousePos = Input.mousePosition;
                mousePos.z = -_mainCam.transform.position.z; // 2D 카메라 깊이 보정
                Vector3 hitPoint = _mainCam.ScreenToWorldPoint(mousePos);
                ToggleTileAtPosition(hitPoint);
            }

            if (Input.GetKeyDown(KeyCode.S)) SaveMap();
            if (Input.GetKeyDown(KeyCode.L)) LoadMap();
        }

        private void ToggleTileAtPosition(Vector3 pos)
        {
            int x = Mathf.FloorToInt(pos.x / nodeSize);
            int y = Mathf.FloorToInt(pos.y / nodeSize); // 2D 환경이므로 z가 아닌 y사용

            if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
            {
                GridTileData tile = _mapData.tiles.Find(t => t.x == x && t.y == y);
                if (tile != null)
                {
                    tile.isWalkable = !tile.isWalkable;
                    Debug.Log($"Toggled tile ({x}, {y}) to Walkable: {tile.isWalkable}");
                }
            }
        }

        public void SaveMap()
        {
            string path = Path.Combine(Application.persistentDataPath, fileName);
            string json = JsonUtility.ToJson(_mapData, true);
            File.WriteAllText(path, json);
            Debug.Log($"[RuntimeMapEditor] Saved to {path}");
        }

        public void LoadMap()
        {
            string path = Path.Combine(Application.persistentDataPath, fileName);
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                _mapData = JsonUtility.FromJson<MapData>(json);
                gridWidth = _mapData.width;
                gridHeight = _mapData.height;
                nodeSize = _mapData.nodeSize;
                Debug.Log($"[RuntimeMapEditor] Loaded from {path}");
            }
            else
            {
                Debug.LogWarning($"[RuntimeMapEditor] Save file not found: {path}");
            }
        }

        private void OnDrawGizmos()
        {
            if (_mapData == null || _mapData.tiles == null) return;

            foreach (var tile in _mapData.tiles)
            {
                Gizmos.color = tile.isWalkable ? new Color(0, 1, 0, 0.3f) : new Color(1, 0, 0, 0.5f);
                // 2D 평면이므로 Z는 고정하고 X, Y 평면에 그립니다.
                Vector3 center = new Vector3(tile.x * nodeSize + nodeSize / 2f, tile.y * nodeSize + nodeSize / 2f, 0.1f);
                Gizmos.DrawCube(center, new Vector3(nodeSize * 0.9f, nodeSize * 0.9f, 0.1f));
            }
        }
    }
}
