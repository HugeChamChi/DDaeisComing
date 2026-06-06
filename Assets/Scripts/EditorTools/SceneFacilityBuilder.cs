using UnityEngine;
using System.Collections.Generic;
using Bathhouse.Data;

namespace Bathhouse.Tools
{
    public enum BuilderMode { Placement, Erase, SetWalkable, SetUnwalkable }

    /// <summary>
    /// 씬에서 구조물을 마우스로 클릭해 시각적으로 배치할 수 있게 해주는 빌더 클래스입니다.
    /// 에디터 스크립트(SceneFacilityBuilderEditor)와 연동되어 동작합니다.
    /// </summary>
    [ExecuteInEditMode]
    public class SceneFacilityBuilder : MonoBehaviour
    {
        public BuilderMode currentMode = BuilderMode.Placement;

        [HideInInspector]
        public List<Bathhouse.Data.GridTileData> tiles = new List<Bathhouse.Data.GridTileData>();

        [Header("Grid Config")]
        [Tooltip("공통 맵 설정 (ScriptableObject)")]
        public MapConfig mapConfig;

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

            // 1. 범위를 벗어난 기존 타일 제거
            tiles.RemoveAll(t => t.x >= targetWidth || t.y >= targetHeight);

            // 2. 새로 확장된 영역에 타일 추가 (기존에 없는 것만)
            for (int y = 0; y < targetHeight; y++)
            {
                for (int x = 0; x < targetWidth; x++)
                {
                    if (!tiles.Exists(t => t.x == x && t.y == y))
                    {
                        tiles.Add(new Bathhouse.Data.GridTileData { x = x, y = y, isWalkable = true });
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

        public void InitializeTiles()
        {
            tiles.Clear();
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    tiles.Add(new Bathhouse.Data.GridTileData { x = x, y = y, isWalkable = true });
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
                    Gizmos.color = tile.isWalkable ? new Color(0, 1, 0, 0.3f) : new Color(1, 0, 0, 0.3f);
                    Vector3 center = origin + new Vector3(tile.x * nodeSize + nodeSize * 0.5f, tile.y * nodeSize + nodeSize * 0.5f, 0);
                    Gizmos.DrawCube(center, new Vector3(nodeSize, nodeSize, 0.1f));
                    Gizmos.color = new Color(1, 1, 1, 0.2f);
                    Gizmos.DrawWireCube(center, new Vector3(nodeSize, nodeSize, 0.1f));
                }
            }
        }
    }
}
