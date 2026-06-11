using Bathhouse.Managers;
using System.Collections.Generic;
using UnityEngine;
using Bathhouse.Data;
using Bathhouse.Pathfinding;
using System;
using Cysharp.Threading.Tasks;
using System.Threading;

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
        protected NPCInteractionController _interactionController;
        protected NPCAnimationController _animController;
        
        protected Bathhouse.Facilities.FacilityBase _targetFacility;
        private bool _isActionForcedToFinish = false;
        private bool _isActionTimerPaused = false;
        
        protected HashSet<Bathhouse.Facilities.FacilityBase> _unreachableFacilities = new HashSet<Bathhouse.Facilities.FacilityBase>();

        public Data.NPCData Data => _data;
        public NPCRouteController RouteController => _routeController;
        public NPCMovement Movement => _movement;
        
        // NPC 개별 런타임 만족도 (ScriptableObject 데이터와 분리)
        public float CurrentSatisfaction { get; private set; }

        protected virtual void Awake()
        {
            _movement = GetComponent<NPCMovement>();
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            _interactionController = GetComponent<NPCInteractionController>();
            _animController = GetComponent<NPCAnimationController>();

            if (_spriteRenderer == null)
            {
                Debug.LogWarning($"[{gameObject.name}] NPC_Base: SpriteRenderer가 없습니다. 동적 정렬이 작동하지 않습니다.");
            }
        }


        protected Vector3 _spawnPosition;

        public virtual void Initialize(NPCData data, NPCRouteProfileSO routeProfile, PathfindingService pathfinder, Action<NPC_Base> onExitCallback, Vector3 spawnPos = default)
        {
            _data = data;
            _routeController = new NPCRouteController(routeProfile, this);
            _pathfindingService = pathfinder;
            _onExit = onExitCallback;
            _spawnPosition = spawnPos;

            if (_movement == null)
                _movement = GetComponent<NPCMovement>();
            
            // 데이터의 기본 만족도로 런타임 만족도 초기화
            CurrentSatisfaction = _data != null ? _data.baseSatisfactionLevel : 0f;
            
            if (_routeController == null)
                _routeController = new NPCRouteController(routeProfile, this);

            _movement.Initialize(_data.moveSpeed);
            _routeController.ResetRoute();

            if (_animController != null)
            {
                _animController.ResetAnimation();
            }

            DecideNextAction();
        }

        protected virtual void Update()
        {
            // [수정됨] 예약 시스템(Reservation)이 도입되었으므로, 이동 중에 자리가 뺏겼는지 매 프레임 검사할 필요가 없습니다.
            // 오히려 자신이 마지막 남은 자리를 예약해버리면 CanEnter()가 false가 되어 
            // 자기 스스로 이동을 취소해버리는 버그가 발생하므로 기존 검사 로직을 제거했습니다.
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

        protected int _reservedSlot = -1;

        /// <summary>
        /// 특정 타입의 시설 중 이용 가능한 곳을 찾아 이동합니다.
        /// </summary>
        protected virtual void MoveToTargetFacility(FacilityType type)
        {
            // 랜덤으로 "자리가 비어있는" 시설 찾기
            _targetFacility = GameManager.Facility.GetRandomAvailableFacility(type, _unreachableFacilities);

            if (_targetFacility == null)
            {
                Debug.Log($"[{Data.name}] {type} 시설이 모두 꽉 찼거나 도달 불가능합니다. 즉시 다음 행동 탐색...");
                _unreachableFacilities.Clear();
                WaitAndDecideAsync(0.1f, this.GetCancellationTokenOnDestroy()).Forget(); // 무한 루프 방지를 위해 아주 짧은 찰나(0.1초) 대기 후 재결정
                return;
            }

            // [중요: 동선 선점] 출발하기 전에 미리 자리를 예약(찜)합니다.
            if (!_targetFacility.ReserveSlot(this, out _reservedSlot))
            {
                // 그 찰나에 다른 NPC가 찜했다면 다시 탐색
                _targetFacility = null;
                MoveToTargetFacility(type);
                return;
            }

            var targetPositions = _targetFacility.GetAllInteractionWorldPositions();
            List<Vector3> validPath = null;

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
                Debug.Log($"[{Data.name}] {_targetFacility.Data.name}의 모든 상호작용 위치가 막혀있습니다. 예약을 취소하고 다른 시설을 찾습니다.");
                _targetFacility.CancelReservation(this); // 예약 취소
                _unreachableFacilities.Add(_targetFacility);
                _targetFacility = null;
                MoveToTargetFacility(type);
                return;
            }

            _unreachableFacilities.Clear();
            
            _movement.MoveAlongPath(validPath, () => 
            {
                // [도착 후 최종 확인]
                // 이미 예약을 걸어두었으므로, 도중에 다른 NPC에게 뺏기지 않음
                if (_targetFacility != null)
                {
                    var facilityToEnter = _targetFacility;
                    _targetFacility = null; 
                    PerformActionRoutineAsync(facilityToEnter, _reservedSlot, this.GetCancellationTokenOnDestroy()).Forget();
                }
            });
        }

        private async UniTaskVoid WaitAndDecideAsync(float delay, CancellationToken token)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: token);
            DecideNextAction();
        }

        public void ForceFinishCurrentAction()
        {
            _isActionForcedToFinish = true;
        }

        public void AddSatisfaction(float amount)
        {
            CurrentSatisfaction = Mathf.Clamp01(CurrentSatisfaction + amount);
        }

        public void SetActionTimerPaused(bool isPaused)
        {
            _isActionTimerPaused = isPaused;
        }

        public void SetSortingOrder(int order)
        {
            var depthSorter = GetComponentInChildren<Bathhouse.Tools.DynamicDepthSorter>();
            if (depthSorter != null)
            {
                depthSorter.ForceSetOrder(order);
            }
            else if (_spriteRenderer != null)
            {
                _spriteRenderer.sortingOrder = order;
            }
        }

        protected virtual async UniTaskVoid PerformActionRoutineAsync(Bathhouse.Facilities.IFacility facility, int slot, CancellationToken token)
        {
            if (slot == -1) 
            {
                // 혹시나 찰나의 순간에 꽉 찼다면 다시 탐색
                MoveToTargetFacility(_currentDestination);
                return;
            }

            facility.EnterFacility(this, slot);
            float waitTime = facility.GetUsageTime();
            Data.FacilityType type = facility.GetFacilityType();

            // 상호작용 UI(말풍선) 시작 (비동기 병렬 실행)
            if (_interactionController != null)
            {
                _interactionController.StartInteractionAsync(type, waitTime).Forget();
            }

            // 상호작용 애니메이션 실행
            if (_animController != null)
            {
                _animController.PlayFacilityAction(type);
            }

            // 상호작용 지점(O 마커) 저장
            Vector3 interactionPos = transform.position;
            Quaternion originalRotation = transform.rotation;
            var depthSorter = GetComponentInChildren<Bathhouse.Tools.DynamicDepthSorter>();

            // 구조물 이용 시 무조건 구조물보다 높은 정렬(Order) 값을 갖도록 강제
            if (depthSorter != null) depthSorter.IsPaused = true;
            SetSortingOrder(facility.GetSortingOrder() + 1);

            if (facility.TeleportToSlotOnUse)
            {
                // 머무는 자리(U 마커)로 이동 (구조물 내부)
                Vector3 usagePos = facility.GetUsageWorldPosition(slot);
                Quaternion usageRot = facility.GetUsageRotation(slot);
                transform.position = usagePos;
                transform.rotation = usageRot;
            }

            // Progress 루프 (매 프레임 호출)
            float elapsed = 0f;
            _isActionForcedToFinish = false;
            _isActionTimerPaused = false; // 초기화
            
            while (elapsed < waitTime && !_isActionForcedToFinish)
            {
                token.ThrowIfCancellationRequested();

                if (!_isActionTimerPaused)
                {
                    elapsed += Time.deltaTime;
                }
                
                if (type != FacilityType.Counter)
                {
                    facility.ProgressFacility(this, slot, Time.deltaTime);
                }
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            facility.ExitFacility(this, slot);

            if (facility.TeleportToSlotOnUse)
            {
                // 사용이 끝나면 구조물 내부(Slot)에서 다시 상호작용 노드 위치(밖)로 텔레포트 및 회전 복귀
                transform.position = interactionPos;
                transform.rotation = originalRotation;
            }

            // 동적 정렬 다시 켜기
            if (depthSorter != null) depthSorter.IsPaused = false;

            // 상호작용 애니메이션 종료
            if (_animController != null)
            {
                _animController.StopFacilityAction();
            }

            // 다음 행동 결정
            DecideNextAction();
        }

        protected virtual void ExitBathhouse()
        {
            if (_movement != null && _pathfindingService != null)
            {
                var validPath = _pathfindingService.FindPath(transform.position, _spawnPosition);
                if (validPath != null && validPath.Count > 0)
                {
                    _movement.MoveAlongPath(validPath, () => 
                    {
                        _onExit?.Invoke(this);
                    });
                    return;
                }
            }
            // 길을 못 찾거나 Movement가 없으면 즉시 퇴장
            _onExit?.Invoke(this);
        }

        protected virtual void OnDestroy()
        {
            if (_targetFacility != null)
            {
                _targetFacility.CancelReservation(this);
            }
        }
    }
}
