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

        private Vector3 _lastVisualPosOffset;
        private Vector3 _lastVisualScaleOffset;
        [HideInInspector] public int lastWidth = -1;
        [HideInInspector] public int lastHeight = -1;
        [HideInInspector] public int lastGridX = -1;
        [HideInInspector] public int lastGridY = -1;

        private void Awake()
        {
            if (Application.isPlaying)
            {
                UpdateSortingOrder();
            }
        }

        public void UpdateSortingOrder()
        {
            float bottomY = transform.position.y;
            float bottomX = transform.position.x;
            SceneFacilityBuilder builder = FindObjectOfType<SceneFacilityBuilder>();
            if (builder != null)
            {
                // 구조물이 차지하는 타일(Node)의 맨 아랫부분(바닥) Y좌표 및 좌측 X좌표를 정확히 계산합니다.
                bottomY = builder.transform.position.y + (gridY * builder.nodeSize);
                bottomX = builder.transform.position.x + (gridX * builder.nodeSize);
            }

            // NPC(DynamicDepthSorter)와 100% 동일한 공식 적용 (동기화)
            // Y 1칸당 -100, X 1칸당 -1 (왼쪽 오브젝트가 더 상위에 표시됨)
            int newOrder = 30000 - Mathf.RoundToInt(bottomY * 100f) - Mathf.RoundToInt(bottomX * 1f);
            
            UnityEngine.Rendering.SortingGroup sortingGroup = GetComponent<UnityEngine.Rendering.SortingGroup>();
            if (sortingGroup != null)
            {
                sortingGroup.sortingOrder = newOrder;
            }
            else
            {
                // SortingGroup이 없다면 하위에 있는 모든 Visual Object(만쥬 등)의 SpriteRenderer를 찾아 전부 적용
                SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
                foreach (var sr in renderers)
                {
                    // 참고: 만약 하위 오브젝트들끼리 미세한 앞뒤 정렬(탁자와 만쥬)이 필요하다면 
                    // 프리팹의 최상위 오브젝트에 'Sorting Group' 컴포넌트를 달아주시는 것이 가장 완벽합니다!
                    sr.sortingOrder = newOrder;
                }
            }
        }

        private void Update()
        {
            if (!Application.isPlaying && facilityData != null)
            {
                bool changed = false;
                bool gridChanged = (lastGridX != gridX || lastGridY != gridY);
                bool sizeChanged = (lastWidth != facilityData.width || lastHeight != facilityData.height);

                SceneFacilityBuilder builder = FindObjectOfType<SceneFacilityBuilder>();

                // 씬 뷰에서 Transform을 직접 이동시켰을 경우 감지
                if (transform.hasChanged && builder != null && !gridChanged && !sizeChanged)
                {
                    Vector3 relPos = transform.position - facilityData.visualPosOffset - builder.transform.position;
                    int newGridX = Mathf.RoundToInt((relPos.x - (facilityData.width * builder.nodeSize) / 2f) / builder.nodeSize);
                    int newGridY = Mathf.RoundToInt((relPos.y - (facilityData.height * builder.nodeSize) / 2f) / builder.nodeSize);
                    
                    if (newGridX != gridX || newGridY != gridY)
                    {
                        gridX = newGridX;
                        gridY = newGridY;
                        gridChanged = true;
                    }
                }

                if (_lastVisualPosOffset != facilityData.visualPosOffset || _lastVisualScaleOffset != facilityData.visualScaleOffset)
                {
                    _lastVisualPosOffset = facilityData.visualPosOffset;
                    _lastVisualScaleOffset = facilityData.visualScaleOffset;
                    changed = true;
                }

                if (sizeChanged || gridChanged)
                {
                    if (builder != null && builder.tiles != null)
                    {
                        if (lastWidth > 0 && lastHeight > 0 && lastGridX >= 0 && lastGridY >= 0)
                        {
                            // 1. 기존 타일 영역 롤백 (이동 가능하게 복구)
                            for (int i = 0; i < lastWidth; i++)
                            {
                                for (int j = 0; j < lastHeight; j++)
                                {
                                    int targetX = lastGridX + i;
                                    int targetY = lastGridY + j;
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
                    lastGridX = gridX;
                    lastGridY = gridY;
                    changed = true;
                }

                if (changed)
                {
                    if (builder != null)
                    {
                        Vector3 center = builder.transform.position + new Vector3(
                            gridX * builder.nodeSize + (facilityData.width * builder.nodeSize) / 2f,
                            gridY * builder.nodeSize + (facilityData.height * builder.nodeSize) / 2f,
                            0f
                        );
                        transform.position = center + facilityData.visualPosOffset;
                        transform.localScale = facilityData.visualScaleOffset;

                        // 배치/이동 시점에 Node 좌표 기반으로 Sorting Order 업데이트
                        UpdateSortingOrder();

#if UNITY_EDITOR
                        UnityEditor.EditorUtility.SetDirty(this);
#endif
                    }
                }

                transform.hasChanged = false;
            }
        }

        private void OnDestroy()
        {
            if (lastWidth > 0 && lastHeight > 0 && lastGridX >= 0 && lastGridY >= 0)
            {
                SceneFacilityBuilder builder = FindObjectOfType<SceneFacilityBuilder>();
                if (builder != null && builder.tiles != null)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                        UnityEditor.Undo.RecordObject(builder, "Restore Tiles");
#endif
                    for (int i = 0; i < lastWidth; i++)
                    {
                        for (int j = 0; j < lastHeight; j++)
                        {
                            int targetX = lastGridX + i;
                            int targetY = lastGridY + j;
                            GridTileData tile = builder.tiles.Find(t => t.x == targetX && t.y == targetY);
                            if (tile != null) tile.isWalkable = true;
                        }
                    }
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                        UnityEditor.EditorUtility.SetDirty(builder);
#endif
                }
            }
        }
    }
}
