using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using TMPro;
using Bathhouse.NPC;

namespace Bathhouse.MiniGames
{
    public enum SwipeDirection { Up, Down, Left, Right }

    public class ScrubMiniGameUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("UI References")]
        public GameObject gamePanel;
        public Slider gaugeFill; // Slider 사용
        
        [Tooltip("방향을 표시할 화살표 UI의 RectTransform")]
        public RectTransform arrowRect;
        
        [Tooltip("화살표가 '쓱' 나타나기 위한 Image (Image Type을 Filled로 설정하세요)")]
        public Image arrowImage;

        [Header("Settings")]
        public float baseDifficultyWeight = 1.0f; 
        [Tooltip("하루(Day)가 지날 때마다 추가되는 난이도 (예: 0.1이면 매일 10%씩 어려워짐)")]
        public float difficultyIncreasePerDay = 0.1f;
        [Tooltip("최대로 도달할 수 있는 난이도 상한선")]
        public float maxDifficultyWeight = 3.0f;
        
        public float swipeSensitivity = 50f; 
        public float gaugeIncreaseAmount = 0.2f; 
        public float arrowAnimSpeed = 1.5f; // 화살표가 차오르는 속도
        public float baseTimeLimit = 10f; // 기본 제한 시간

        [Header("Timer UI (선택사항)")]
        [Tooltip("남은 시간을 표시할 슬라이더 (0~1)")]
        public Slider timerSlider;

        private NPC_Base _currentNPC;
        private float _currentGauge = 0f;
        private SwipeDirection _targetDirection;
        private Vector2 _dragStartPos;
        private float _animTimer = 0f;
        private float _timeRemaining = 0f;
        private float _currentTimeLimit = 10f;

        private Action _onSuccessCallback;
        private Action _onFailCallback;

        private void Start()
        {
            gamePanel.SetActive(false);
        }

        private void Update()
        {
            // 알파(투명도) 대신 Before-After 처럼 한쪽에서부터 쓱(Fill) 나타나는 연출
            if (arrowImage != null && gamePanel.activeSelf)
            {
                _animTimer += Time.deltaTime * arrowAnimSpeed;
                
                // 주기를 1.2로 잡아서 0~1 구간은 차오르고, 1~1.2 구간은 잠깐 사라져서 대기
                float t = Mathf.Repeat(_animTimer, 1.2f);
                
                if (t <= 1f)
                {
                    arrowImage.fillAmount = t;
                }
                else
                {
                    arrowImage.fillAmount = 0f;
                }

                // 타이머 로직
                _timeRemaining -= Time.deltaTime;
                if (timerSlider != null)
                {
                    timerSlider.value = _timeRemaining / _currentTimeLimit;
                }

                if (_timeRemaining <= 0f)
                {
                    GameFailed();
                }
            }
        }

        public void StartMiniGame(NPC_Base targetNPC, int currentDay, Action onSuccess, Action onFail)
        {
            _currentNPC = targetNPC;
            _onSuccessCallback = onSuccess;
            _onFailCallback = onFail;
            _currentGauge = 0f;
            
            // 난이도 계산: 기본값 + ((현재 날짜 - 1) * 하루당 증가폭)
            // 예: 1일차 = 1.0 + (0 * 0.1) = 1.0
            // 예: 11일차 = 1.0 + (10 * 0.1) = 2.0
            float calculatedWeight = baseDifficultyWeight + ((currentDay - 1) * difficultyIncreasePerDay);
            
            // 상한선(Max) 적용
            float finalWeight = Mathf.Clamp(calculatedWeight, baseDifficultyWeight, maxDifficultyWeight);

            // 게이지 상승량 및 제한시간 반영
            gaugeIncreaseAmount = 0.25f / finalWeight; 
            
            _currentTimeLimit = baseTimeLimit / finalWeight;
            _currentTimeLimit = Mathf.Max(3f, _currentTimeLimit); // 최소 3초 보장
            _timeRemaining = _currentTimeLimit;

            UpdateGaugeUI();
            SetNewRandomDirection();
            gamePanel.SetActive(true);
            
            Debug.Log($"[ScrubMiniGame] {currentDay}일차 게임 시작! 적용 난이도: {finalWeight:F2}, 제한 시간: {_currentTimeLimit:F1}초");
        }

        private void SetNewRandomDirection()
        {
            _targetDirection = (SwipeDirection)UnityEngine.Random.Range(0, 4);
            
            // 화살표 회전 처리
            if (arrowRect != null)
            {
                float zAngle = 0f;
                switch (_targetDirection)
                {
                    case SwipeDirection.Up: zAngle = 0f; break;
                    case SwipeDirection.Down: zAngle = 180f; break;
                    case SwipeDirection.Left: zAngle = 90f; break;
                    case SwipeDirection.Right: zAngle = -90f; break;
                }
                arrowRect.localEulerAngles = new Vector3(0, 0, zAngle);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _dragStartPos = eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            // 드래그 중 실시간 처리 가능 (여기서는 종료 시 한 번에 판정)
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            Vector2 dragEndPos = eventData.position;
            Vector2 dragVector = dragEndPos - _dragStartPos;

            if (dragVector.magnitude >= swipeSensitivity)
            {
                SwipeDirection swipeDir = GetSwipeDirection(dragVector);
                if (swipeDir == _targetDirection)
                {
                    // 성공
                    _currentGauge += gaugeIncreaseAmount;
                    UpdateGaugeUI();
                    
                    if (_currentGauge >= 1f)
                    {
                        GameSuccess();
                    }
                    else
                    {
                        SetNewRandomDirection();
                    }
                }
                else
                {
                    // 실패 패널티 (게이지 감소 등) - 일단 패스하거나 살짝 감소
                    Debug.Log("[ScrubMiniGame] 방향이 틀렸습니다!");
                }
            }
        }

        private SwipeDirection GetSwipeDirection(Vector2 dragVector)
        {
            if (Mathf.Abs(dragVector.x) > Mathf.Abs(dragVector.y))
            {
                return dragVector.x > 0 ? SwipeDirection.Right : SwipeDirection.Left;
            }
            else
            {
                return dragVector.y > 0 ? SwipeDirection.Up : SwipeDirection.Down;
            }
        }

        private void UpdateGaugeUI()
        {
            if (gaugeFill != null)
                gaugeFill.value = _currentGauge;
        }

        private void GameSuccess()
        {
            Debug.Log("[ScrubMiniGame] 때밀이 완벽 성공!");
            gamePanel.SetActive(false);
            _onSuccessCallback?.Invoke();
        }

        private void GameFailed()
        {
            Debug.Log("[ScrubMiniGame] 시간 초과! 때밀이 실패!");
            gamePanel.SetActive(false);
            _onFailCallback?.Invoke();
        }
    }
}
