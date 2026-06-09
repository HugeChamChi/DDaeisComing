using UnityEngine;
using Bathhouse.Data;
using Bathhouse.Pathfinding;
using Bathhouse.Managers;
using System.Collections;
using System;

namespace Bathhouse.NPC
{
    [RequireComponent(typeof(NPCMovement))]
    public class NPC_Base : MonoBehaviour
    {
        [SerializeField] protected NPCData _data;
        
        protected NPCRouteController _routeController;
        protected NPCMovement _movement;
        protected PathfindingService _pathfindingService;
        
        protected SpriteRenderer _spriteRenderer;

        protected FacilityType _currentDestination;
        protected Action<NPC_Base> _onExit;
        
        protected Bathhouse.Facilities.FacilityBase _targetFacility;
        private bool _isActionForcedToFinish = false;
        private bool _isActionTimerPaused = false;
        
        protected System.Collections.Generic.HashSet<Bathhouse.Facilities.FacilityBase> _unreachableFacilities = new System.Collections.Generic.HashSet<Bathhouse.Facilities.FacilityBase>();

        public Data.NPCData Data => _data;
        public NPCRouteController RouteController => _routeController;
        public NPCMovement Movement => _movement;

        protected virtual void Awake()
        {
            _movement = GetComponent<NPCMovement>();
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (_spriteRenderer == null)
            {
                Debug.LogWarning($"[{gameObject.name}] NPC_Base: SpriteRenderer가 없습니다. 동적 정렬이 작동하지 않습니다.");
            }
        }


        public virtual void Initialize(NPCData data, NPCRouteProfileSO routeProfile, PathfindingService pathfinder, Action<NPC_Base> onExitCallback)
        {
            _data = data;
            _pathfindingService = pathfinder;
            _onExit = onExitCallback;

            if (_movement == null)
                _movement = GetComponent<NPCMovement>();
            
            if (_routeController == null)
                _routeController = new NPCRouteController(routeProfile, _data);

            _movement.Initialize(_data.moveSpeed);
            _routeController.ResetRoute();

            DecideNextAction();
        }

        protected virtual void Update()
        {
            if (_movement != null && _movement.IsMoving && _targetFacility != null)
            {
                if (!_targetFacility.CanEnter())
                {
                    Debug.Log($"[{Data.name}] 이동 중에 {_currentDestination} 자리가 찼음! 다른 곳으로 재탐색...");
                    _movement.StopMoving();
                    _targetFacility = null;
                    MoveToTargetFacility(_currentDestination);
                }
            }
        }

        protected virtual void DecideNextAction()
        {
            _currentDestination = _routeController.DetermineNextFacility();
            
            if (_currentDestination == FacilityType.None)
            {
                Debug.Log($"[{Data.name}] 모든 동선을 완료했습니다. 퇴장합니다.");
                ExitBathhouse();
                return;
            }

            Debug.Log($"[NPC_Base] {_data.name}의 다음 목표가 결정되었습니다: {_currentDestination}");

            MoveToTargetFacility(_currentDestination);
        }

        /// <summary>
        /// 특정 타입의 시설 중 이용 가능한 곳을 찾아 이동합니다.
        /// </summary>
        protected virtual void MoveToTargetFacility(FacilityType type)
        {
            // 가장 가깝고 "자리가 비어있는" 시설 찾기
            _targetFacility = FacilityManager.Instance.GetNearestAvailableFacility(type, transform.position, _unreachableFacilities);

            if (_targetFacility == null)
            {
                Debug.Log($"[{Data.name}] {type} 시설이 모두 꽉 찼거나 도달 불가능합니다. 즉시 다음 행동 탐색...");
                _unreachableFacilities.Clear();
                StartCoroutine(WaitAndDecide(0.1f)); // 무한 루프 방지를 위해 아주 짧은 찰나(0.1초) 대기 후 재결정
                return;
            }

            var targetPositions = _targetFacility.GetAllInteractionWorldPositions();
            System.Collections.Generic.List<Vector3> validPath = null;

            foreach (var pos in targetPositions)
            {
                var path = _pathfindingService.FindPath(transform.position, pos);
                if (path != null && path.Count > 0)
                {
                    validPath = path;
                    break;
                }
            }

            if (validPath == null)
            {
                Debug.Log($"[{Data.name}] {_targetFacility.Data.name}의 모든 상호작용 위치가 막혀있습니다. 다른 시설을 찾습니다.");
                _unreachableFacilities.Add(_targetFacility);
                _targetFacility = null;
                MoveToTargetFacility(type);
                return;
            }

            _unreachableFacilities.Clear();
            
            _movement.MoveAlongPath(validPath, () => 
            {
                // [도착 후 최종 확인]
                if (_targetFacility != null && _targetFacility.CanEnter())
                {
                    var facilityToEnter = _targetFacility;
                    _targetFacility = null; 
                    StartCoroutine(PerformActionRoutine(facilityToEnter));
                }
                else
                {
                    Debug.Log($"[{Data.name}] 도착했으나 그 사이 {type} 자리가 찼음! 다시 재탐색...");
                    _targetFacility = null;
                    MoveToTargetFacility(type);
                }
            });
        }

        private System.Collections.IEnumerator WaitAndDecide(float delay)
        {
            yield return new WaitForSeconds(delay);
            DecideNextAction();
        }

        public void ForceFinishCurrentAction()
        {
            _isActionForcedToFinish = true;
        }

        public void SetActionTimerPaused(bool isPaused)
        {
            _isActionTimerPaused = isPaused;
        }

        public void SetSortingOrder(int order)
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.sortingOrder = order;
            }
        }

        protected virtual System.Collections.IEnumerator PerformActionRoutine(Bathhouse.Facilities.IFacility facility)
        {
            int slot = facility.GetAvailableSlotIndex();
            if (slot == -1) 
            {
                // 혹시나 찰나의 순간에 꽉 찼다면 다시 탐색
                MoveToTargetFacility(_currentDestination);
                yield break;
            }

            facility.EnterFacility(this, slot);
            float waitTime = facility.GetUsageTime();
            Data.FacilityType type = facility.GetFacilityType();

            // 상호작용 지점(O 마커) 저장
            Vector3 interactionPos = transform.position;

            // 머무는 자리(U 마커)로 이동 (구조물 내부)
            Vector3 usagePos = facility.GetUsageWorldPosition(slot);
            transform.position = usagePos;

            if (type == FacilityType.Counter)
            {
                // 카운터의 경우 대기 시간만큼 머물렀다가 진행
                yield return new WaitForSeconds(waitTime);
            }
            else
            {
                // Progress 루프 (매 프레임 호출)
                float elapsed = 0f;
                _isActionForcedToFinish = false;
                _isActionTimerPaused = false; // 초기화
                
                while (elapsed < waitTime && !_isActionForcedToFinish)
                {
                    if (!_isActionTimerPaused)
                    {
                        elapsed += Time.deltaTime;
                    }
                    
                    facility.ProgressFacility(this, slot, Time.deltaTime);
                    yield return null;
                }
            }

            facility.ExitFacility(this, slot);

            // 다음 행동 결정
            DecideNextAction();
        }

        protected virtual void ExitBathhouse()
        {
            _onExit?.Invoke(this);
        }
    }
}
