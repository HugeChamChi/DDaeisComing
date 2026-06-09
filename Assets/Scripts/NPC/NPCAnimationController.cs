using Bathhouse.Data;
using UnityEngine;

namespace Bathhouse.NPC
{
    [RequireComponent(typeof(NPCMovement))]
    public class NPCAnimationController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("애니메이터 컨트롤러 (비워두면 자식 객체에서 자동 탐색)")]
        [SerializeField] private Animator _animator;
        [Tooltip("반전(Flip)시킬 비주얼 Transform (비워두면 SpriteRenderer가 있는 자식 객체 자동 탐색)")]
        [SerializeField] private Transform _visualTransform;
        
        // 절차적 애니메이션 (DoTween) 컨트롤러
        private NPCProceduralAnimator _proceduralAnimator;
        private NPCMovement _movement;
        
        // 멈췄을 때도 마지막으로 바라보던 방향을 기억
        private Vector2 _lastDirection = Vector2.down;
        
        private bool _isUsingFacility = false;
        private string _currentStateName = "";

        private void Awake()
        {
            _movement = GetComponent<NPCMovement>();
            
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();
                
            if (_visualTransform == null)
            {
                var sr = GetComponentInChildren<SpriteRenderer>();
                if (sr != null) _visualTransform = sr.transform;
                else _visualTransform = transform;
            }

            // DoTween 제어 컴포넌트 추가 및 초기화
            _proceduralAnimator = GetComponent<NPCProceduralAnimator>();
            if (_proceduralAnimator == null)
            {
                _proceduralAnimator = gameObject.AddComponent<NPCProceduralAnimator>();
            }
            _proceduralAnimator.Initialize(_visualTransform);
        }

        private void Update()
        {
            if (_movement == null || _animator == null || _visualTransform == null) return;

            // 시설 이용 중일 때는 이동/대기 로직 스킵 (상호작용 애니메이션 유지)
            if (_isUsingFacility) return;

            bool isMoving = _movement.IsMoving;
            Vector2 dir = _movement.CurrentDirection;

            if (isMoving && dir.sqrMagnitude > 0.01f)
            {
                _lastDirection = dir;
            }

            // 1. 좌우 반전 (Scale X 조절)
            // 좌우 이동이 상하 이동보다 강하거나, 최근 방향이 좌우일 때만 뒤집기 판단
            if (Mathf.Abs(_lastDirection.x) > Mathf.Abs(_lastDirection.y) || Mathf.Abs(_lastDirection.x) > 0.01f)
            {
                Vector3 scale = _visualTransform.localScale;
                if (_lastDirection.x > 0) // 오른쪽
                {
                    scale.x = -Mathf.Abs(scale.x);
                }
                else // 왼쪽
                {
                    scale.x = Mathf.Abs(scale.x);
                }
                _visualTransform.localScale = scale;
            }

            // 2. Animator Play (상태 전이 없이 문자열로 바로 실행)
            string nextState = GetStateName(isMoving);
            if (_currentStateName != nextState)
            {
                _animator.Play(nextState);
                _currentStateName = nextState;
            }

            // 3. DoTween 절차적 애니메이션 (뒤뚱뒤뚱 / 숨쉬기)
            if (isMoving)
            {
                _proceduralAnimator.PlayState(NPCProceduralAnimator.ProceduralState.Move);
            }
            else
            {
                _proceduralAnimator.PlayState(NPCProceduralAnimator.ProceduralState.Idle);
            }
        }

        private string GetStateName(bool isMoving)
        {
            string state = isMoving ? "Move_" : "Idle_";

            // 좌우 이동이 상하 이동보다 클 때 좌우 애니메이션 재생
            if (Mathf.Abs(_lastDirection.x) > Mathf.Abs(_lastDirection.y))
            {
                state += "Left_Right";
            }
            else if (_lastDirection.y > 0) // 위쪽
            {
                state += "Up";
            }
            else // 아래쪽
            {
                state += "Down";
            }
            return state;
        }

        // 특정 위치를 바라보며 Idle 상태로 전환
        public void FaceTarget(Vector3 targetPosition)
        {
            Vector2 dir = (targetPosition - transform.position).normalized;
            if (dir.sqrMagnitude > 0.01f)
            {
                _lastDirection = dir;
            }

            // 좌우 반전 업데이트
            if (Mathf.Abs(_lastDirection.x) > Mathf.Abs(_lastDirection.y) || Mathf.Abs(_lastDirection.x) > 0.01f)
            {
                Vector3 scale = _visualTransform.localScale;
                if (_lastDirection.x > 0) scale.x = -Mathf.Abs(scale.x);
                else scale.x = Mathf.Abs(scale.x);
                _visualTransform.localScale = scale;
            }

            // 바라보는 방향의 Idle 애니메이션 재생
            _isUsingFacility = true; // 시설 이용 중 상태로 고정 (Update에서 덮어쓰기 방지)
            string nextState = GetStateName(false); // isMoving = false
            if (_animator != null && _currentStateName != nextState)
            {
                _animator.Play(nextState);
                _currentStateName = nextState;
            }

            if (_proceduralAnimator != null)
            {
                _proceduralAnimator.PlayState(NPCProceduralAnimator.ProceduralState.Idle);
            }
        }

        // 4. 상호작용 애니메이션 (시설 이용 시)
        public void PlayFacilityAction(FacilityType facilityType)
        {
            _isUsingFacility = true;
            string actionName = "Action_" + facilityType.ToString();
            
            if (_animator != null)
            {
                _animator.Play(actionName);
            }

            if (_proceduralAnimator != null)
            {
                _proceduralAnimator.PlayState(NPCProceduralAnimator.ProceduralState.Action);
            }
            
            _currentStateName = actionName;
        }

        public void StopFacilityAction()
        {
            _isUsingFacility = false;
            _currentStateName = ""; // Update 루프에서 즉시 Idle/Move 로 복귀됨
        }
    }
}
