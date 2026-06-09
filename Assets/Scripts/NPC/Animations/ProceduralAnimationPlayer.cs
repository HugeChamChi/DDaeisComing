using UnityEngine;

namespace Bathhouse.NPC.Animations
{
    /// <summary>
    /// ScriptableObject로 만들어진 절차적 애니메이션(ProceduralAnimationBase)을 
    /// 오브젝트가 활성화(OnEnable)될 때 자동으로 실행해주는 MonoBehaviour 컴포넌트입니다.
    /// </summary>
    public class ProceduralAnimationPlayer : MonoBehaviour
    {
        [Tooltip("실행할 애니메이션 SO 데이터 (Breath, Waddle 등)")]
        public ProceduralAnimationBase animationData;

        [Tooltip("크기가 변경될 타겟 트랜스폼 (비워두면 자기 자신의 Transform 사용)")]
        public Transform targetTransform;

        private Vector3 _baseScale;
        private bool _isInitialized = false;

        private void Awake()
        {
            if (targetTransform == null)
                targetTransform = transform;

            // 원래의 로컬 스케일을 기억해 둡니다 (애니메이션 기준점)
            _baseScale = targetTransform.localScale;
            _isInitialized = true;
        }

        private void OnEnable()
        {
            if (_isInitialized && animationData != null)
            {
                // 오브젝트가 켜질 때 즉시 애니메이션 시작
                animationData.PlayAnimation(targetTransform, _baseScale);
            }
        }

        private void OnDisable()
        {
            if (_isInitialized && animationData != null)
            {
                // 오브젝트가 꺼질 때 애니메이션을 멈추고 크기를 원래대로 복구
                animationData.StopAnimation(targetTransform, _baseScale);
            }
        }

        private void OnDestroy()
        {
            if (_isInitialized && animationData != null)
            {
                animationData.StopAnimation(targetTransform, _baseScale);
            }
        }
    }
}
