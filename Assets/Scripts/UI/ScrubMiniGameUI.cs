using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using Bathhouse.NPC;

namespace Bathhouse.MiniGames
{
    public enum SwipeDirection { Up, Down, Left, Right }

    public class ScrubMiniGameUI : MiniGameBase, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Scrub UI References")]
        public Slider gaugeFill; 
        public RectTransform arrowRect;
        public Image arrowImage;

        [Header("Scrub Settings")]
        public float swipeSensitivity = 50f; 
        public float gaugeIncreaseAmount = 0.2f; 
        public float arrowAnimSpeed = 1.5f;

        private NPC_Base _currentNPC;
        private float _currentGauge = 0f;
        private SwipeDirection _targetDirection;
        private Vector2 _dragStartPos;
        private float _animTimer = 0f;

        protected override void Update()
        {
            base.Update();

            if (arrowImage != null && gamePanel != null && gamePanel.activeSelf)
            {
                _animTimer += Time.deltaTime * arrowAnimSpeed;
                
                float t = Mathf.Repeat(_animTimer, 1.2f);
                
                if (t <= 1f)
                {
                    arrowImage.fillAmount = t;
                }
                else
                {
                    arrowImage.fillAmount = 0f;
                }
            }
        }

        public void StartMiniGame(NPC_Base targetNPC, int currentDay, Action onSuccess, Action onFail)
        {
            _currentNPC = targetNPC;
            base.StartMiniGame(currentDay, onSuccess, onFail);
        }

        protected override void OnGameStarted()
        {
            _currentGauge = 0f;
            
            // 게이지 상승량 반영
            gaugeIncreaseAmount = 0.25f / _finalDifficultyWeight; 
            
            UpdateGaugeUI();
            SetNewRandomDirection();
            
            Debug.Log($"[ScrubMiniGame] 게임 시작! 적용 난이도: {_finalDifficultyWeight:F2}, 제한 시간: {_currentTimeLimit:F1}초");
        }

        private void SetNewRandomDirection()
        {
            _targetDirection = (SwipeDirection)UnityEngine.Random.Range(0, 4);
            
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

        protected override void GameSuccess()
        {
            Debug.Log("[ScrubMiniGame] 때밀이 완벽 성공!");
            base.GameSuccess();
        }

        protected override void GameFailed()
        {
            Debug.Log("[ScrubMiniGame] 시간 초과! 때밀이 실패!");
            base.GameFailed();
        }
    }
}
