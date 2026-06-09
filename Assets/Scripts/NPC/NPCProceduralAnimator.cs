using UnityEngine;
using Bathhouse.NPC.Animations;

namespace Bathhouse.NPC
{
    public class NPCProceduralAnimator : MonoBehaviour
    {
        private Transform _visualTransform;

        [Header("Abstracted Animations")]
        [Tooltip("대기(Idle) 중일 때 재생할 절차적 애니메이션 SO를 드래그 앤 드롭 하세요.")]
        [SerializeField] private ProceduralAnimationBase _idleAnimation;
        
        [Tooltip("이동(Move) 중일 때 재생할 절차적 애니메이션 SO를 드래그 앤 드롭 하세요.")]
        [SerializeField] private ProceduralAnimationBase _moveAnimation;

        private Vector3 _baseScale;
        private ProceduralAnimationBase _currentAnimation;

        public enum ProceduralState { None, Idle, Move, Action }
        private ProceduralState _currentState = ProceduralState.None;

        public void Initialize(Transform visualTransform)
        {
            _visualTransform = visualTransform;
            _baseScale = _visualTransform.localScale;
        }

        public void PlayState(ProceduralState state)
        {
            if (_visualTransform == null) return;
            if (_currentState == state) return;

            // 1. 기존 애니메이션 정지 및 초기화
            if (_currentAnimation != null)
            {
                _currentAnimation.StopAnimation(_visualTransform, _baseScale);
                _currentAnimation = null;
            }

            _currentState = state;

            // 2. 새로운 애니메이션 실행
            switch (state)
            {
                case ProceduralState.Idle:
                    if (_idleAnimation != null)
                    {
                        _idleAnimation.PlayAnimation(_visualTransform, _baseScale);
                        _currentAnimation = _idleAnimation;
                    }
                    break;

                case ProceduralState.Move:
                    if (_moveAnimation != null)
                    {
                        _moveAnimation.PlayAnimation(_visualTransform, _baseScale);
                        _currentAnimation = _moveAnimation;
                    }
                    break;

                case ProceduralState.Action:
                    // 상호작용 시에는 기본적으로 애니메이션 정지
                    break;
            }
        }

        private void OnDisable()
        {
            if (_currentAnimation != null && _visualTransform != null)
            {
                _currentAnimation.StopAnimation(_visualTransform, _baseScale);
            }
        }
    }
}
