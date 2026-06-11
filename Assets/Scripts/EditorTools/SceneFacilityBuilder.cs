using Bathhouse.Data;
using UnityEngine;
using System.Collections.Generic;

namespace Bathhouse.Tools
{
    public enum BuilderMode { None, Placement, Erase, SetWalkable, SetUnwalkable, SetSpawnPos, InsertGrid }

    /// <summary>
    /// 씬에서 구조물을 마우스로 클릭해 시각적으로 배치할 수 있게 해주는 빌더 클래스입니다.
    /// 에디터 스크립트(SceneFacilityBuilderEditor)와 연동되어 동작합니다.
    /// </summary>
    [ExecuteInEditMode]
    public class SceneFacilityBuilder : MonoBehaviour
    {
        public BuilderMode currentMode = BuilderMode.Placement;

        [HideInInspector]
        public List<GridTileData> tiles = new List<GridTileData>();

        [Header("Grid Config")]
        [Tooltip("공통 맵 설정 (ScriptableObject)")]
        public MapConfig mapConfig;

        [Header("Insert Grid Settings")]
        public int insertRightAmount = 2;
        public int insertTopAmount = 0;

        // 에디터와 다른 스크립트에서 기존과 동일하게 접근할 수 있도록 프로퍼티 제공
        public int gridWidth => mapConfig != null ? mapConfig.gridWidth : 20;
        public int gridHeight => mapConfig != null ? mapConfig.gridHeight : 20;
        public float nodeSize => mapConfig != null ? mapConfig.nodeSize : 1f;
        [Header("Placement")]
        [Tooltip("현재 배치하려고 선택된 구조물 데이터")]
        public FacilityData selectedFacility;

        [Header("References")]
        [Tooltip("생성된 구조물들이 속할 부모 Transform (정리용)")]
        public Transform facilityParent;

        [Header("Debug")]
        public bool showGrid = true;
        public Color gridColor = new Color(0.8f, 0.8f, 0.8f, 0.3f);

        private MapConfig _lastMapConfig;

        private void Update()
        {
            if (_lastMapConfig != mapConfig)
            {
                if (_lastMapConfig != null)
                    _lastMapConfig.OnConfigChanged -= ResizeTilesToMatchConfig;
                
                if (mapConfig != null)
                {
                    mapConfig.OnConfigChanged += ResizeTilesToMatchConfig;
                    ResizeTilesToMatchConfig();
                }
                
                _lastMapConfig = mapConfig;
            }
        }

        private void OnDestroy()
        {
            if (mapConfig != null)
                mapConfig.OnConfigChanged -= ResizeTilesToMatchConfig;
        }

        public void ResizeTilesToMatchConfig()
        {
            if (mapConfig == null) return;

            int targetWidth = mapConfig.gridWidth;
            int targetHeight = mapConfig.gridHeight;

            // 1. 범위를 벗어난 기존 타일 제거 (주석 처리하여 기존 세팅 보존)
            // tiles.RemoveAll(t => t.x >= targetWidth || t.y >= targetHeight);

            // 2. 새로 확장된 영역에 타일 추가 (기존에 없는 것만)
            for (int y = 0; y < targetHeight; y++)
            {
                for (int x = 0; x < targetWidth; x++)
                {
                    if (!tiles.Exists(t => t.x == x && t.y == y))
                    {
                        tiles.Add(new GridTileData { x = x, y = y, isWalkable = true });
                    }
                }
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(this);
                if (UnityEditor.SceneView.lastActiveSceneView != null)
                    UnityEditor.SceneView.lastActiveSceneView.Repaint();
            }
#endif
        }

        [ContextMenu("맵 확장 (중앙 기준 상하좌우 +1칸)")]
        public void ExpandMapCentered()
        {
            ShiftMapCentered(1);
        }

        [ContextMenu("맵 축소 (중앙 기준 상하좌우 -1칸)")]
        public void ShrinkMapCentered()
        {
            ShiftMapCentered(-1);
        }

        public void ShiftMapCentered(int amount)
        {
            if (mapConfig == null) return;

            // 방어 코드 (축소 시 2x2 미만이 되지 않도록)
            if (mapConfig.gridWidth + amount * 2 < 2 || mapConfig.gridHeight + amount * 2 < 2) return;

            // 1. MapConfig 설정 업데이트
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(mapConfig, "Shift Map Centered");
#endif
            mapConfig.gridWidth += amount * 2;
            mapConfig.gridHeight += amount * 2;
            mapConfig.spawnGridPos.x += amount;
            mapConfig.spawnGridPos.y += amount;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(mapConfig);
#endif

            // 2. 타일 이동
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(this, "Shift Map Centered");
#endif
            List<GridTileData> newTiles = new List<GridTileData>();
            foreach (var tile in tiles)
            {
                int newX = tile.x + amount;
                int newY = tile.y + amount;
                
                // 축소 시 영역을 벗어나는 타일은 버림
                if (newX >= 0 && newX < mapConfig.gridWidth && newY >= 0 && newY < mapConfig.gridHeight)
                {
                    newTiles.Add(new GridTileData { x = newX, y = newY, isWalkable = tile.isWalkable });
                }
            }
            tiles = newTiles;

            // 3. 구조물 이동
            SceneFacilityInfo[] facilities = GetComponentsInChildren<SceneFacilityInfo>(true);
            foreach (var fac in facilities)
            {
#if UNITY_EDITOR
                UnityEditor.Undo.RecordObject(fac, "Shift Map Centered");
                UnityEditor.Undo.RecordObject(fac.transform, "Shift Map Centered");
#endif
                fac.gridX += amount;
                fac.gridY += amount;
                
                if (fac.facilityData != null)
                {
                    Vector3 center = transform.position + new Vector3(
                        fac.gridX * nodeSize + (fac.facilityData.width * nodeSize) / 2f,
                        fac.gridY * nodeSize + (fac.facilityData.height * nodeSize) / 2f,
                        0f
                    );
                    fac.transform.position = center + fac.facilityData.visualPosOffset;
                    fac.lastGridX = fac.gridX;
                    fac.lastGridY = fac.gridY;
                }

#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(fac);
#endif
            }

            // 4. 나머지 빈 공간 채우기
            ResizeTilesToMatchConfig();
        }

        public void ExpandMapCustom(int left, int right, int top, int bottom)
        {
            if (mapConfig == null) return;

            // 방어 코드 (축소 시 1x1 미만이 되지 않도록)
            if (mapConfig.gridWidth + left + right < 1 || mapConfig.gridHeight + top + bottom < 1) return;

            // 1. MapConfig 설정 업데이트
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(mapConfig, "Expand Map Custom");
#endif
            mapConfig.gridWidth += (left + right);
            mapConfig.gridHeight += (top + bottom);
            mapConfig.spawnGridPos.x += left;
            mapConfig.spawnGridPos.y += bottom;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(mapConfig);
#endif

            // 2. 타일 이동
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(this, "Expand Map Custom");
#endif
            List<GridTileData> newTiles = new List<GridTileData>();
            foreach (var tile in tiles)
            {
                int newX = tile.x + left;
                int newY = tile.y + bottom;
                
                // 축소 시 영역을 벗어나는 타일은 버림
                if (newX >= 0 && newX < mapConfig.gridWidth && newY >= 0 && newY < mapConfig.gridHeight)
                {
                    newTiles.Add(new GridTileData { x = newX, y = newY, isWalkable = tile.isWalkable });
                }
            }
            tiles = newTiles;

            // 3. 구조물 이동
            SceneFacilityInfo[] facilities = GetComponentsInChildren<SceneFacilityInfo>(true);
            foreach (var fac in facilities)
            {
#if UNITY_EDITOR
                UnityEditor.Undo.RecordObject(fac, "Expand Map Custom");
                UnityEditor.Undo.RecordObject(fac.transform, "Expand Map Custom");
#endif
                fac.gridX += left;
                fac.gridY += bottom;
                
                if (fac.facilityData != null)
                {
                    Vector3 center = transform.position + new Vector3(
                        fac.gridX * nodeSize + (fac.facilityData.width * nodeSize) / 2f,
                        fac.gridY * nodeSize + (fac.facilityData.height * nodeSize) / 2f,
                        0f
                    );
                    fac.transform.position = center + fac.facilityData.visualPosOffset;
                    fac.lastGridX = fac.gridX;
                    fac.lastGridY = fac.gridY;
                }

#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(fac);
#endif
            }

            // 4. 나머지 빈 공간 채우기
            ResizeTilesToMatchConfig();
        }

        public void InsertGrid(int baseX, int baseY, int insertX, int insertY)
        {
            if (mapConfig == null) return;
            if (insertX == 0 && insertY == 0) return;

#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(mapConfig, "Insert Grid");
#endif
            mapConfig.gridWidth += insertX;
            mapConfig.gridHeight += insertY;
            
            if (mapConfig.spawnGridPos.x > baseX) mapConfig.spawnGridPos.x += insertX;
            if (mapConfig.spawnGridPos.y > baseY) mapConfig.spawnGridPos.y += insertY;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(mapConfig);
#endif

#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(this, "Insert Grid");
#endif
            List<GridTileData> newTiles = new List<GridTileData>();
            foreach (var tile in tiles)
            {
                int newX = tile.x;
                int newY = tile.y;

                if (insertX != 0 && newX > baseX) newX += insertX;
                if (insertY != 0 && newY > baseY) newY += insertY;

                // 영역 밖으로 밀려나는(음수 크기 시) 타일은 버리기
                if (newX >= 0 && newX < mapConfig.gridWidth && newY >= 0 && newY < mapConfig.gridHeight)
                {
                    newTiles.Add(new GridTileData { x = newX, y = newY, isWalkable = tile.isWalkable });
                }
            }
            tiles = newTiles;

            SceneFacilityInfo[] facilities = GetComponentsInChildren<SceneFacilityInfo>(true);
            foreach (var fac in facilities)
            {
#if UNITY_EDITOR
                UnityEditor.Undo.RecordObject(fac, "Insert Grid");
                UnityEditor.Undo.RecordObject(fac.transform, "Insert Grid");
#endif
                bool moved = false;
                if (insertX != 0 && fac.gridX > baseX)
                {
                    fac.gridX += insertX;
                    moved = true;
                }
                if (insertY != 0 && fac.gridY > baseY)
                {
                    fac.gridY += insertY;
                    moved = true;
                }
                
                if (moved && fac.facilityData != null)
                {
                    Vector3 center = transform.position + new Vector3(
                        fac.gridX * nodeSize + (fac.facilityData.width * nodeSize) / 2f,
                        fac.gridY * nodeSize + (fac.facilityData.height * nodeSize) / 2f,
                        0f
                    );
                    fac.transform.position = center + fac.facilityData.visualPosOffset;
                    fac.lastGridX = fac.gridX;
                    fac.lastGridY = fac.gridY;
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(fac);
#endif
                }
            }

            ResizeTilesToMatchConfig();
        }

        public void InitializeTiles()
        {
            tiles.Clear();
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    tiles.Add(new GridTileData { x = x, y = y, isWalkable = true });
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!showGrid) return;

            Gizmos.color = gridColor;
            Vector3 origin = transform.position; // 이 오브젝트의 위치가 원점(0,0)

            // 타일 색상 표시
            if (tiles != null && tiles.Count > 0)
            {
                foreach (var tile in tiles)
                {
                    if (tile.x >= gridWidth || tile.y >= gridHeight) continue;

                    Gizmos.color = tile.isWalkable ? new Color(0, 1, 0, 0.3f) : new Color(1, 0, 0, 0.3f);
                    Vector3 center = origin + new Vector3(tile.x * nodeSize + nodeSize * 0.5f, tile.y * nodeSize + nodeSize * 0.5f, 0);
                    Gizmos.DrawCube(center, new Vector3(nodeSize, nodeSize, 0.1f));
                    Gizmos.color = new Color(1, 1, 1, 0.2f);
                    Gizmos.DrawWireCube(center, new Vector3(nodeSize, nodeSize, 0.1f));
                }
            }

            // 스폰 위치 표시
            if (mapConfig != null)
            {
                Gizmos.color = new Color(0, 0, 1, 0.7f); // 파란색 반투명
                Vector3 spawnCenter = origin + new Vector3(mapConfig.spawnGridPos.x * nodeSize + nodeSize * 0.5f, mapConfig.spawnGridPos.y * nodeSize + nodeSize * 0.5f, 0);
                Gizmos.DrawSphere(spawnCenter, nodeSize * 0.4f);
            }
        }
    }
}
