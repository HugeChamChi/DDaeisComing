using UnityEngine;
using System;

namespace Bathhouse.Data
{
    /// <summary>
    /// 여러 스크립트(빌더, 카메라 등)에서 공통으로 사용할 맵 기본 설정 데이터를 담는 ScriptableObject입니다.
    /// 하나의 SO 에셋을 여러 곳에서 참조하면 값이 동기화되어 관리가 매우 편해집니다.
    /// </summary>
    [CreateAssetMenu(fileName = "New MapConfig", menuName = "Bathhouse/Map Config", order = 0)]
    public class MapConfig : ScriptableObject
    {
        [Header("Map Dimensions")]
        [Tooltip("맵의 가로 타일 개수")]
        public int gridWidth = 20;

        [Tooltip("맵의 세로 타일 개수")]
        public int gridHeight = 20;

        [Tooltip("한 타일의 실제 월드 크기")]
        public float nodeSize = 1f;

        public event Action OnConfigChanged;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 에디터에서 값이 변경될 때마다 크기가 1 이하로 내려가지 않게 방어
            if (gridWidth < 1) gridWidth = 1;
            if (gridHeight < 1) gridHeight = 1;
            if (nodeSize <= 0.1f) nodeSize = 0.1f;

            // 지연 호출을 통해 씬에 있는 오브젝트들을 안전하게 갱신
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                OnConfigChanged?.Invoke();

                // ScriptableObject의 C# 이벤트는 에디터 직렬화 과정에서 리스너가 유실되기 쉬우므로,
                // 확실한 동작을 위해 씬 내의 빌더와 카메라 피터를 직접 찾아 강제 갱신합니다.
                var builders = FindObjectsOfType<Tools.SceneFacilityBuilder>();
                foreach (var builder in builders)
                {
                    if (builder.mapConfig == this)
                    {
                        builder.ResizeTilesToMatchConfig();
                    }
                }

                var fitters = FindObjectsOfType<Utils.CameraFitter>();
                foreach (var fitter in fitters)
                {
                    if (fitter.mapConfig == this)
                    {
                        fitter.FitToMap();
                    }
                }
            };
        }
#endif
    }
}
