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
        
        protected NPCBrain _brain;
        protected NPCMovement _movement;
        protected PathfindingService _pathfindingService;
        
        protected FacilityType _currentDestination;
        protected Action<NPC_Base> _onExit;
        
        protected Bathhouse.Facilities.FacilityBase _targetFacility;
        private bool _isActionForcedToFinish = false;
        private bool _isActionTimerPaused = false;

        public Data.NPCData Data => _data;
        public NPCBrain Brain => _brain;

        public virtual void Initialize(NPCData data, PathfindingService pathfinder, Action<NPC_Base> onExitCallback)
        {
            _data = data;
            _pathfindingService = pathfinder;
            _onExit = onExitCallback;

            if (_movement == null)
                _movement = GetComponent<NPCMovement>();
            
            if (_brain == null)
                _brain = new NPCBrain(_data);

            _movement.Initialize(_data.moveSpeed);
            _brain.ResetMemory();

            DecideNextAction();
        }

        protected virtual void Update()
        {
            // 이동 중에 목표 시설이 꽉 찼는지 실시간 감시
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
            _currentDestination = _brain.DetermineNextFacility();
            Debug.Log($"[NPC_Base] {_data.name}의 다음 목표가 결정되었습니다: {_currentDestination}");

            MoveToTargetFacility(_currentDestination);
        }

        /// <summary>
        /// 특정 타입의 시설 중 이용 가능한 곳을 찾아 이동합니다.
        /// </summary>
        protected virtual void MoveToTargetFacility(FacilityType type)
        {
            // 가장 가깝고 "자리가 비어있는" 시설 찾기
            _targetFacility = FacilityManager.Instance.GetNearestAvailableFacility(type, transform.position);

            if (_targetFacility == null)
            {
                Debug.Log($"[{Data.name}] {type} 시설이 모두 꽉 찼습니다. 즉시 다음 행동 탐색...");
                StartCoroutine(WaitAndDecide(0.1f)); // 무한 루프 방지를 위해 아주 짧은 찰나(0.1초) 대기 후 재결정
                return;
            }

            // 시설 자체가 아닌, 시설의 '상호작용 위치(O 마커)'로 길찾기 수행
            Vector3 targetPos = _targetFacility.GetMainInteractionWorldPosition();
            var path = _pathfindingService.FindPath(transform.position, targetPos);
            
            _movement.MoveAlongPath(path, () => 
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
                // 퇴장 시 정산 대기
                yield return new WaitForSeconds(waitTime);
                facility.ExitFacility(this, slot);
                ExitBathhouse();
                yield break;
            }

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

            facility.ExitFacility(this, slot);

            // 볼일을 마치면 다시 상호작용 지점(밖)으로 나옴
            transform.position = interactionPos;

            DecideNextAction();
        }

        protected virtual void ExitBathhouse()
        {
            _onExit?.Invoke(this);
        }
    }
}
