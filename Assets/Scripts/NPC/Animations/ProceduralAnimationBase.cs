using UnityEngine;
using DG.Tweening;

namespace Bathhouse.NPC.Animations
{
    public abstract class ProceduralAnimationBase : ScriptableObject
    {
        [Header("Common Settings")]
        [Tooltip("애니메이션 1회 소요 시간")]
        public float duration = 1f;
        
        [Tooltip("DoTween Ease 방식 (가속/감속 곡선)")]
        public Ease easeType = Ease.InOutSine;

        /// <summary>
        /// 트윈 애니메이션을 실행합니다.
        /// </summary>
        public abstract void PlayAnimation(Transform target, Vector3 baseScale);

        /// <summary>
        /// 트윈 애니메이션을 중지하고 상태를 원상복구합니다.
        /// </summary>
        public abstract void StopAnimation(Transform target, Vector3 baseScale);
    }
}
