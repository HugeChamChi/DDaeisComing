using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;

namespace Bathhouse.MiniGames
{
    public class TrashMiniGameUI : MiniGameBase
    {
        [Header("Trash Game Settings")]
        [Tooltip("쓰레기통의 UI 영역 (이곳으로 드래그 앤 드롭해야 성공)")]
        public RectTransform trashCanRect;
        [Tooltip("쓰레기가 생성될 영역")]
        public RectTransform trashSpawnArea;
        [Tooltip("생성될 쓰레기 프리팹 (TrashItemUI 컴포넌트 포함 필요)")]
        public GameObject trashPrefab;
        [Tooltip("기본 쓰레기 개수")]
        public int baseTrashCount = 5;

        private int _currentTrashCount = 0;
        private int _totalTrashCount = 0;
        private List<GameObject> _activeTrashes = new List<GameObject>();

        private int _overrideTrashCount = -1;

        public void SetCustomTrashCount(int count)
        {
            _overrideTrashCount = count;
        }

        protected override void OnGameStarted()
        {
            if (_overrideTrashCount > 0)
            {
                _totalTrashCount = _overrideTrashCount;
                _overrideTrashCount = -1;
            }
            else
            {
                // 난이도에 따라 쓰레기 개수 증가
                _totalTrashCount = Mathf.RoundToInt(baseTrashCount * _finalDifficultyWeight);
            }
            
            _currentTrashCount = 0;

            SpawnTrashes();
            Debug.Log($"[TrashMiniGame] 게임 시작! 쓰레기 개수: {_totalTrashCount}, 제한 시간: {_currentTimeLimit:F1}초");
        }

        private void SpawnTrashes()
        {
            ClearTrashes();

            for (int i = 0; i < _totalTrashCount; i++)
            {
                GameObject trashObj = Instantiate(trashPrefab, trashSpawnArea);
                
                // 랜덤 위치 설정 (trashSpawnArea 내부)
                RectTransform rt = trashObj.GetComponent<RectTransform>();

                Camera raycastCamera = null; 
                Canvas parentCanvas = GetComponentInParent<Canvas>();
                if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    raycastCamera = parentCanvas.worldCamera;
                }

                Vector2 validPos = Vector2.zero;
                bool positionFound = false;
                int maxAttempts = 50;

                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    float x = UnityEngine.Random.Range(trashSpawnArea.rect.xMin, trashSpawnArea.rect.xMax);
                    float y = UnityEngine.Random.Range(trashSpawnArea.rect.yMin, trashSpawnArea.rect.yMax);
                    Vector2 candidatePos = new Vector2(x, y);

                    // 스크린 좌표로 변환하여 쓰레기통 렉트와 겹치는지 확인
                    Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(raycastCamera, trashSpawnArea.TransformPoint(candidatePos));
                    
                    if (RectTransformUtility.RectangleContainsScreenPoint(trashCanRect, screenPos, raycastCamera))
                    {
                        continue; // 쓰레기통과 겹치면 다시
                    }

                    // 다른 쓰레기와 겹치는지 확인 (기준 거리: 100 픽셀)
                    bool overlapsOtherTrash = false;
                    foreach (var existingTrash in _activeTrashes)
                    {
                        RectTransform existingRt = existingTrash.GetComponent<RectTransform>();
                        if (Vector2.Distance(existingRt.anchoredPosition, candidatePos) < 100f)
                        {
                            overlapsOtherTrash = true;
                            break;
                        }
                    }

                    if (overlapsOtherTrash)
                    {
                        continue;
                    }

                    validPos = candidatePos;
                    positionFound = true;
                    break;
                }

                if (!positionFound)
                {
                    // 최후의 수단으로 대충 겹치더라도 배치
                    validPos = new Vector2(
                        UnityEngine.Random.Range(trashSpawnArea.rect.xMin, trashSpawnArea.rect.xMax),
                        UnityEngine.Random.Range(trashSpawnArea.rect.yMin, trashSpawnArea.rect.yMax)
                    );
                }

                rt.anchoredPosition = validPos;

                TrashItemUI trashUI = trashObj.GetComponent<TrashItemUI>();
                if (trashUI != null)
                {
                    trashUI.Initialize(this);
                }
                else
                {
                    Debug.LogWarning("[TrashMiniGame] 쓰레기 프리팹에 TrashItemUI 컴포넌트가 없습니다.");
                }

                _activeTrashes.Add(trashObj);
            }
        }

        public void OnTrashDropped(TrashItemUI trashItem, Vector2 dropScreenPos)
        {
            // 드롭된 위치가 휴지통 영역 안에 있는지 검사
            // (주의: eventData.position은 스크린 좌표이므로 eventCamera를 적절히 넘겨야 함. Overlay 캔버스면 null 전달)
            Camera raycastCamera = null; 
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                raycastCamera = parentCanvas.worldCamera;
            }

            if (RectTransformUtility.RectangleContainsScreenPoint(trashCanRect, dropScreenPos, raycastCamera))
            {
                // 쓰레기 수거 성공
                trashItem.gameObject.SetActive(false);
                _currentTrashCount++;

                if (_currentTrashCount >= _totalTrashCount)
                {
                    GameSuccess();
                }
            }
            else
            {
                // 실패시 제자리로 돌아가기
                trashItem.ResetPosition();
            }
        }

        private void ClearTrashes()
        {
            foreach (var t in _activeTrashes)
            {
                if (t != null) Destroy(t);
            }
            _activeTrashes.Clear();
        }

        protected override void GameSuccess()
        {
            Debug.Log("[TrashMiniGame] 모든 쓰레기 수거 완료!");
            ClearTrashes();
            base.GameSuccess();
        }

        protected override void GameFailed()
        {
            Debug.Log("[TrashMiniGame] 시간 초과! 청소 실패!");
            ClearTrashes();
            base.GameFailed();
        }
    }
}
