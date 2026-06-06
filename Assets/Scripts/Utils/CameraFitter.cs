using UnityEngine;

namespace Bathhouse.Utils
{
    /// <summary>
    /// 맵의 크기(Grid Width/Height)를 기반으로 카메라의 위치와 Orthographic Size를 자동으로 조절하여
    /// 맵 전체가 한 화면에 보이도록 설정해주는 컴포넌트입니다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [ExecuteInEditMode] // 에디터에서도 실시간으로 카메라 조절 가능
    public class CameraFitter : MonoBehaviour
    {
        private Camera _cam;

        [Header("Map Settings (ScriptableObject)")]
        [Tooltip("공통 맵 설정을 할당하면, 이 값을 추적하여 자동으로 크기가 조절됩니다.")]
        public Data.MapConfig mapConfig;

        [Tooltip("맵이 시작되는 기준점 (SceneFacilityBuilder 오브젝트 등). 비워두면 (0,0,0)을 기준으로 잡습니다.")]
        public Transform mapOrigin;

        [Header("Fallback Settings")]
        [Tooltip("MapConfig가 할당되지 않았을 때 사용하는 기본 가로/세로/사이즈입니다.")]
        public int fallbackGridWidth = 20;
        public int fallbackGridHeight = 20;
        public float fallbackNodeSize = 1f;

        private int CurrentGridWidth => mapConfig != null ? mapConfig.gridWidth : fallbackGridWidth;
        private int CurrentGridHeight => mapConfig != null ? mapConfig.gridHeight : fallbackGridHeight;
        private float CurrentNodeSize => mapConfig != null ? mapConfig.nodeSize : fallbackNodeSize;

        [Header("Camera Settings")]
        [Tooltip("화면 가장자리 여백 (1.1 = 10% 여백)")]
        public float paddingMultiplier = 1.1f;
        
        [Tooltip("체크하면 Update 주기마다 계속 화면에 맞춥니다. 해상도 변경 대응용입니다.")]
        public bool fitEveryFrame = false;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
        }

        private void Start()
        {
            FitToMap();
        }

        private void Update()
        {
            if (fitEveryFrame || !Application.isPlaying)
            {
                FitToMap();
            }
        }

        /// <summary>
        /// 설정된 gridWidth, gridHeight 값을 바탕으로 카메라를 조절합니다.
        /// </summary>
        public void FitToMap()
        {
            if (_cam == null) _cam = GetComponent<Camera>();
            if (!_cam.orthographic)
            {
                Debug.LogWarning("[CameraFitter] 이 스크립트는 2D Orthographic 카메라에서만 정상 작동합니다.");
                return;
            }

            // 1. 맵 전체 월드 크기 계산
            float mapWorldWidth = CurrentGridWidth * CurrentNodeSize;
            float mapWorldHeight = CurrentGridHeight * CurrentNodeSize;

            // 2. 카메라 위치를 맵 중앙으로 이동 (Z축은 그대로 유지)
            Vector3 originPos = mapOrigin != null ? mapOrigin.position : Vector3.zero;
            transform.position = new Vector3(originPos.x + mapWorldWidth / 2f, originPos.y + mapWorldHeight / 2f, transform.position.z);

            // 3. 현재 게임 화면 비율 (스크린 너비 / 높이)
            // Camera.aspect를 사용하면 에디터/빌드, Game/Scene 뷰 환경에 상관없이 가장 정확한 비율을 가져옵니다.
            float screenRatio = _cam.aspect;
            
            // 만약 0이거나 비정상적일 경우 방어코드
            if (screenRatio <= 0) screenRatio = 16f / 9f;

            float targetRatio = mapWorldWidth / mapWorldHeight;

            // 4. 비율에 따라 Orthographic Size 조절
            if (screenRatio >= targetRatio)
            {
                // 화면 가로가 충분히 넓을 경우, 세로(Height)를 기준으로 맞춤
                _cam.orthographicSize = (mapWorldHeight / 2f) * paddingMultiplier;
            }
            else
            {
                // 화면 가로가 좁을 경우(세로로 긴 화면 등), 가로(Width)가 짤리지 않게 Size를 더 키움
                float sizeDifference = targetRatio / screenRatio;
                _cam.orthographicSize = (mapWorldHeight / 2f) * sizeDifference * paddingMultiplier;
            }
        }

        /// <summary>
        /// 외부(다른 스크립트)에서 맵 데이터를 로드한 직후에 호출하여 맞출 때 사용합니다.
        /// (단, MapConfig SO가 비어있을 때 fallback 설정으로 작동합니다)
        /// </summary>
        public void SetupCustomMap(int width, int height, float size)
        {
            this.fallbackGridWidth = width;
            this.fallbackGridHeight = height;
            this.fallbackNodeSize = size;
            FitToMap();
        }
    }
}
