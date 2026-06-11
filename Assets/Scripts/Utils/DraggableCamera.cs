using UnityEngine;

namespace Bathhouse.Utils
{
    /// <summary>
    /// MapConfig의 높이(GridHeight)에 맞춰 카메라 사이즈를 고정하고,
    /// 마우스 드래그를 통해 카메라를 이동할 수 있게 합니다.
    /// 이동 시 맵 바깥 영역이 보이지 않도록 카메라 위치를 제한합니다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [ExecuteInEditMode]
    public class DraggableCamera : MonoBehaviour
    {
        private Camera _cam;

        [Header("Map Settings")]
        [Tooltip("공통 맵 설정을 할당하면, 이 값을 추적하여 맵 경계를 설정합니다.")]
        public Data.MapConfig mapConfig;

        [Tooltip("맵이 시작되는 기준점. 비워두면 (0,0,0)을 기준으로 잡습니다.")]
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
        [Tooltip("맵 높이(GridHeight) 외에 세로로 추가 확보할 여백 (단위: 칸 수)")]
        public float heightPadding = 0f;

        [Tooltip("카메라 Y축 위치 오프셋 (UI가 화면 하단을 가릴 때 카메라를 올리는 용도 등)")]
        public float yPositionOffset = 0f;

        [Header("Drag Settings")]
        [Tooltip("마우스 버튼 (0: 좌클릭, 1: 우클릭, 2: 휠클릭)")]
        public int dragMouseButton = 0;

        private Vector3 _dragOriginScreenPos;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            if (!_cam.orthographic)
            {
                Debug.LogWarning("[DraggableCamera] 이 스크립트는 2D Orthographic 카메라에서만 정상 작동합니다.");
            }
        }

        private void Start()
        {
            UpdateCameraSize();
            
            if (Application.isPlaying)
            {
                MoveToLeftEdge();
            }
            else
            {
                ClampCamera();
            }
        }

        [Button]
        public void MoveToLeftEdge()
        {
            if (_cam == null) _cam = GetComponent<Camera>();
            UpdateCameraSize();

            Vector3 originPos = mapOrigin != null ? mapOrigin.position : Vector3.zero;
            Vector3 pos = transform.position;
            pos.x = originPos.x; // 왼쪽 끝을 기준으로 이동
            transform.position = pos;

            ClampCamera(); // 맵 바깥으로 나가지 않도록 즉시 보정
        }

        private void LateUpdate()
        {
            if (!_cam.orthographic) return;

            UpdateCameraSize(); // 에디터 실시간 반영 및 해상도 변경 대응

            if (Application.isPlaying)
            {
                // 미니게임 진행 중에는 카메라 조작 블록
                if (Bathhouse.Managers.MiniGameManager.Instance != null && Bathhouse.Managers.MiniGameManager.Instance.IsAnyMiniGamePlaying)
                {
                    return;
                }

                HandleDrag();
            }

            ClampCamera(); // 드래그 후 맵 밖으로 벗어나지 못하도록 제한
        }

        private void UpdateCameraSize()
        {
            // MapConfig의 높이에 패딩을 더한 만큼 세로 길이를 맞춤
            float requiredHeight = (CurrentGridHeight + heightPadding) * CurrentNodeSize;
            _cam.orthographicSize = requiredHeight / 2f;
        }

        private void HandleDrag()
        {
            // 모바일 터치 입력 처리 (터치가 1개일 때만 드래그 처리, 핀치 줌 등 방지)
            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);

                if (touch.phase == TouchPhase.Began)
                {
                    _dragOriginScreenPos = touch.position;
                }
                else if (touch.phase == TouchPhase.Moved)
                {
                    MoveCamera(touch.position);
                }
            }
            // 에디터 테스트를 위한 마우스 입력 처리 (터치가 없을 때 작동)
            else if (Input.touchCount == 0)
            {
                if (Input.GetMouseButtonDown(dragMouseButton))
                {
                    _dragOriginScreenPos = Input.mousePosition;
                }
                else if (Input.GetMouseButton(dragMouseButton))
                {
                    MoveCamera(Input.mousePosition);
                }
            }
        }

        private void MoveCamera(Vector3 currentScreenPos)
        {
            // 스크린 좌표의 변화량을 월드 좌표의 변화량으로 변환하여 카메라 이동
            Vector3 prevWorldPos = _cam.ScreenToWorldPoint(_dragOriginScreenPos);
            Vector3 currentWorldPos = _cam.ScreenToWorldPoint(currentScreenPos);
            
            Vector3 diff = prevWorldPos - currentWorldPos;
            transform.position += diff;

            _dragOriginScreenPos = currentScreenPos;
        }

        private void ClampCamera()
        {
            Vector3 originPos = mapOrigin != null ? mapOrigin.position : Vector3.zero;
            float mapWorldWidth = CurrentGridWidth * CurrentNodeSize;
            float mapWorldHeight = CurrentGridHeight * CurrentNodeSize;

            float minX = originPos.x;
            float maxX = originPos.x + mapWorldWidth;
            float minY = originPos.y;
            float maxY = originPos.y + mapWorldHeight;

            float camHeight = _cam.orthographicSize * 2f;
            float camWidth = camHeight * _cam.aspect;

            float camExtentsX = camWidth / 2f;
            float camExtentsY = camHeight / 2f;

            Vector3 pos = transform.position;

            // 1. 가로축 제한
            if (camWidth >= mapWorldWidth)
            {
                // 카메라 너비가 맵보다 크면 맵의 왼쪽 끝을 화면 왼쪽 끝에 고정
                pos.x = minX + camExtentsX;
            }
            else
            {
                // 맵 내에서만 움직이도록 제한
                pos.x = Mathf.Clamp(pos.x, minX + camExtentsX, maxX - camExtentsX);
            }

            // 2. 세로축 제한
            if (camHeight >= mapWorldHeight)
            {
                // 카메라 높이가 맵보다 크면 맵 중앙에 고정
                pos.y = originPos.y + mapWorldHeight / 2f;
            }
            else
            {
                // 맵 내에서만 움직이도록 제한
                pos.y = Mathf.Clamp(pos.y, minY + camExtentsY, maxY - camExtentsY);
            }

            // 추가 Y축 오프셋 반영 (UI 등으로 인해 화면 중심을 옮겨야 할 때 사용)
            pos.y += yPositionOffset;

            transform.position = pos;
        }
    }
}
