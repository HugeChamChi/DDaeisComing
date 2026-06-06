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

        private Vector3 _lastVisualOffset;

        private void Update()
        {
            if (!Application.isPlaying && facilityData != null)
            {
                if (_lastVisualOffset != facilityData.visualOffset)
                {
                    _lastVisualOffset = facilityData.visualOffset;
                    SceneFacilityBuilder builder = FindObjectOfType<SceneFacilityBuilder>();
                    if (builder != null)
                    {
                        Vector3 center = builder.transform.position + new Vector3(
                            gridX * builder.nodeSize + (facilityData.width * builder.nodeSize) / 2f,
                            gridY * builder.nodeSize + (facilityData.height * builder.nodeSize) / 2f,
                            0f
                        );
                        transform.position = center + facilityData.visualOffset;
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
