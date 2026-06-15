using System.Threading;
using Bathhouse.Data;
using Cysharp.Threading.Tasks;
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

        [Header("One-shot Action Settings")]
        [Tooltip("바디 드라이어 연출 클립 길이(초). 이 시간이 지나면 마지막 프레임에 갇히지 않고 Idle로 복귀합니다.")]
        [SerializeField] private float _bodyDryerClipLength = 0.6f;

        // 한 번만 재생하는 액션이 끝난 뒤 Idle로 복귀시키는 UniTask 취소 토큰
        private CancellationTokenSource _returnToIdleCts;

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

        public void PlayFacilityAction(FacilityType facilityType)
        {
            _isUsingFacility = true;
            
            if (facilityType == FacilityType.Platform)
            {
                string actionName = "Action_Platform";
                if (_animator != null)
                {
                    _animator.Play(actionName);
                }
                _currentStateName = actionName;
            }
            else if (facilityType == FacilityType.Bath)
            {
                string actionName = "Action_Bath";
                if (_animator != null)
                {
                    _animator.Play(actionName);
                }
                _currentStateName = actionName;
            }
            else if (facilityType == FacilityType.ScrubArea)
            {
                string actionName = "Action_Scrub";
                if (_animator != null)
                {
                    _animator.Play(actionName);
                }
                _currentStateName = actionName;
            }
            else if (facilityType == FacilityType.BodyDryer)
            {
                string actionName = "Action_BodyDryer";
                if (_animator != null)
                {
                    _animator.Play(actionName);
                }
                _currentStateName = actionName;

                // 드라이어 스프라이트는 좌우 반대로 그려져 있어, 일반 flip과 반대로 적용 (드라이어 전용 예외)
                ApplyDryerFacingFlip();

                // 연출 클립이 끝나면 마지막 프레임에 갇히지 않도록 Idle로 복귀
                CancelReturnToIdle();
                _returnToIdleCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
                ReturnToIdleAfterOneShot(_bodyDryerClipLength, _returnToIdleCts.Token).Forget();
            }
            else
            {
                // 플랫폼이 아닌 경우 방향에 맞는 Idle 모션 재생
                string actionName = GetStateName(false);
                if (_animator != null && _currentStateName != actionName)
                {
                    _animator.Play(actionName);
                }
                _currentStateName = actionName;
            }

            if (_proceduralAnimator != null)
            {
                // 움직일 때 빼고는 전부 숨쉬기(Idle) 절차적 애니메이션 적용
                _proceduralAnimator.PlayState(NPCProceduralAnimator.ProceduralState.Idle);
            }
        }

        public void StopFacilityAction()
        {
            CancelReturnToIdle();

            _isUsingFacility = false;
            _currentStateName = ""; // Update 루프에서 즉시 Idle/Move 로 복귀됨
        }

        /// <summary>
        /// _lastDirection 기준으로 일반 좌우 반전을 적용합니다. (FaceTarget/Update와 동일한 컨벤션)
        /// </summary>
        private void ApplyNormalFacingFlip()
        {
            if (_visualTransform == null) return;

            Vector3 scale = _visualTransform.localScale;
            if (_lastDirection.x > 0) scale.x = -Mathf.Abs(scale.x); // 오른쪽
            else                      scale.x =  Mathf.Abs(scale.x); // 왼쪽
            _visualTransform.localScale = scale;
        }

        /// <summary>
        /// 드라이어 전용 좌우 반전. 드라이어 스프라이트가 반대로 그려져 있어 일반 flip과 부호를 반대로 적용합니다.
        /// </summary>
        private void ApplyDryerFacingFlip()
        {
            if (_visualTransform == null) return;

            Vector3 scale = _visualTransform.localScale;
            if (_lastDirection.x > 0) scale.x =  Mathf.Abs(scale.x); // 오른쪽(드라이어 예외)
            else                      scale.x = -Mathf.Abs(scale.x); // 왼쪽(드라이어 예외)
            _visualTransform.localScale = scale;
        }

        /// <summary>
        /// Idle 복귀 대기 중인 UniTask를 취소하고 토큰 소스를 정리합니다.
        /// </summary>
        private void CancelReturnToIdle()
        {
            if (_returnToIdleCts != null)
            {
                _returnToIdleCts.Cancel();
                _returnToIdleCts.Dispose();
                _returnToIdleCts = null;
            }
        }

        /// <summary>
        /// 한 번만 재생하는 액션 클립(바디 드라이어 등)이 끝난 뒤,
        /// 마지막 프레임에 멈춰 있지 않도록 방향에 맞는 Idle 포즈로 복귀시킵니다.
        /// 시설 이용 상태(_isUsingFacility)는 유지하여 이동 모션으로 튀지 않게 합니다.
        /// </summary>
        private async UniTaskVoid ReturnToIdleAfterOneShot(float clipLength, CancellationToken token)
        {
            bool canceled = await UniTask.Delay(
                System.TimeSpan.FromSeconds(clipLength),
                cancellationToken: token).SuppressCancellationThrow();

            if (canceled) return;

            // 아직 시설 이용 중일 때만 Idle로 전환 (이미 퇴장했으면 무시)
            if (_isUsingFacility)
            {
                // 드라이어 예외로 뒤집었던 flip을 일반 캐릭터 스프라이트용으로 복원
                ApplyNormalFacingFlip();

                string idleState = GetStateName(false);
                if (_animator != null && _currentStateName != idleState)
                {
                    _animator.Play(idleState);
                    _currentStateName = idleState;
                }

                if (_proceduralAnimator != null)
                {
                    _proceduralAnimator.PlayState(NPCProceduralAnimator.ProceduralState.Idle);
                }
            }
        }

        public void ResetAnimation()
        {
            CancelReturnToIdle();

            _isUsingFacility = false;
            _currentStateName = "";
            _lastDirection = Vector2.down; // 기본 방향 아래로
            
            if (_animator != null && _animator.gameObject.activeInHierarchy)
            {
                _animator.Rebind();
                _animator.Update(0f);
            }
            
            // 시각적 크기(Flip) 원상복구
            if (_visualTransform != null)
            {
                Vector3 scale = _visualTransform.localScale;
                scale.x = Mathf.Abs(scale.x);
                _visualTransform.localScale = scale;
            }
        }
    }
}
