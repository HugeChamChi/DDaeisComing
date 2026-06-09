using UnityEngine;
using Bathhouse.Data;

namespace Bathhouse.Tools
{
    /// <summary>
    /// 씬에서 시각적으로 배치된 구조물의 데이터를 저장하는 컴포넌트
    /// </summary>
    [ExecuteInEditMode]
    public class SceneFacilityInfo : MonoBehaviour
    {
        public FacilityData facilityData;
        public int gridX;
        public int gridY;

        [Header("Debug/Preview")]
        public bool showBoundsGizmo = true;

        private Vector3 _lastVisualPosOffset;
        private Vector3 _lastVisualScaleOffset;
        [HideInInspector] public int lastWidth = -1;
        [HideInInspector] public int lastHeight = -1;

        private void Update()
        {
            if (!Application.isPlaying && facilityData != null)
            {
                bool changed = false;

                if (_lastVisualPosOffset != facilityData.visualPosOffset || _lastVisualScaleOffset != facilityData.visualScaleOffset)
                {
                    _lastVisualPosOffset = facilityData.visualPosOffset;
                    _lastVisualScaleOffset = facilityData.visualScaleOffset;
                    changed = true;
                }

                if (lastWidth != facilityData.width || lastHeight != facilityData.height)
                {
                    SceneFacilityBuilder builder = FindObjectOfType<SceneFacilityBuilder>();
                    if (builder != null && builder.tiles != null)
                    {
                        if (lastWidth > 0 && lastHeight > 0)
                        {
                            // 1. 기존 타일 영역 롤백 (이동 가능하게 복구)
                            for (int i = 0; i < lastWidth; i++)
                            {
                                for (int j = 0; j < lastHeight; j++)
                                {
                                    int targetX = gridX + i;
                                    int targetY = gridY + j;
                                    GridTileData tile = builder.tiles.Find(t => t.x == targetX && t.y == targetY);
                                    if (tile != null) tile.isWalkable = true;
                                }
                            }
                        }

                        // 2. 새로운 크기의 타일 영역 차단
                        for (int i = 0; i < facilityData.width; i++)
                        {
                            for (int j = 0; j < facilityData.height; j++)
                            {
                                int targetX = gridX + i;
                                int targetY = gridY + j;
                                GridTileData tile = builder.tiles.Find(t => t.x == targetX && t.y == targetY);
                                if (tile != null) tile.isWalkable = false;
                                else builder.tiles.Add(new GridTileData { x = targetX, y = targetY, isWalkable = false });
                            }
                        }
#if UNITY_EDITOR
                        UnityEditor.EditorUtility.SetDirty(builder);
#endif
                    }

                    lastWidth = facilityData.width;
                    lastHeight = facilityData.height;
                    changed = true;
                }

                if (changed)
                {
                    SceneFacilityBuilder builder = FindObjectOfType<SceneFacilityBuilder>();
                    if (builder != null)
                    {
                        Vector3 center = builder.transform.position + new Vector3(
                            gridX * builder.nodeSize + (facilityData.width * builder.nodeSize) / 2f,
                            gridY * builder.nodeSize + (facilityData.height * builder.nodeSize) / 2f,
                            0f
                        );
                        transform.position = center + facilityData.visualPosOffset;
                        transform.localScale = facilityData.visualScaleOffset;
#if UNITY_EDITOR
                        UnityEditor.EditorUtility.SetDirty(this);
#endif
                    }
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!showBoundsGizmo || facilityData == null) return;
            
            // 노드 사이즈는 SceneFacilityBuilder에서 가져오거나 기본값 1로 가정
            float nodeSize = 1f; 
            
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            
            float widthWorld = facilityData.width * nodeSize;
            float heightWorld = facilityData.height * nodeSize;

            Vector3 center = new Vector3(
                transform.position.x, // transform.position은 이미 중앙에 배치됨
                transform.position.y,
                0f
            );

            Gizmos.DrawCube(center, new Vector3(widthWorld, heightWorld, 0.1f));
        }
    }
}
