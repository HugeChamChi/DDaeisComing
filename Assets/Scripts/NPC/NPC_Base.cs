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

        protected virtual void DecideNextAction()
        {
            _currentDestination = _brain.DetermineNextFacility();
            Debug.Log($"[NPC_Base] {_data.name}의 다음 목표가 결정되었습니다: {_currentDestination}");

            // 가장 가깝고 "자리가 비어있는" 시설 찾기
            Bathhouse.Facilities.FacilityBase targetFacility = FacilityManager.Instance.GetNearestAvailableFacility(_currentDestination, transform.position);

            if (targetFacility == null)
            {
                Debug.Log($"[{Data.name}] {_currentDestination} 시설이 모두 꽉 차서 2초 대기 후 다른 행동 탐색...");
                Invoke(nameof(DecideNextAction), 2f);
                return;
            }

            Vector3 targetPos = targetFacility.transform.position;
            var path = _pathfindingService.FindPath(transform.position, targetPos);
            
            _movement.MoveAlongPath(path, () => StartCoroutine(PerformActionRoutine(targetFacility)));
        }

        private System.Collections.IEnumerator WaitAndRetry()
        {
            yield return new WaitForSeconds(2f);
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
            if (slot == -1) slot = 0; // 꽉 찼을 경우 임시로 0번에 겹쳐서 진행

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
