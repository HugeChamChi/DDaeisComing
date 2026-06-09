using UnityEngine;
using DG.Tweening;

namespace Bathhouse.NPC.Animations
{
    [CreateAssetMenu(fileName = "New Waddle Animation", menuName = "Bathhouse/Procedural Animations/Waddle")]
    public class WaddleProceduralAnimation : ProceduralAnimationBase
    {
        [Header("Waddle Settings")]
        [Tooltip("뒤뚱거릴 때 회전하는 Z축 각도")]
        public float waddleAngle = 8f;

        public override void PlayAnimation(Transform target, Vector3 baseScale)
        {
            if (target == null) return;

            StopAnimation(target, baseScale);

            // 초기 각도 설정
            target.localRotation = Quaternion.Euler(0, 0, -waddleAngle);
            
            // 양옆으로 흔들기
            Sequence seq = DOTween.Sequence();
            seq.Append(target.DORotate(new Vector3(0, 0, waddleAngle), duration).SetEase(easeType))
               .Append(target.DORotate(new Vector3(0, 0, -waddleAngle), duration).SetEase(easeType))
               .SetLoops(-1)
               .SetId(target.GetInstanceID() + "_Waddle");
        }

        public override void StopAnimation(Transform target, Vector3 baseScale)
        {
            if (target == null) return;

            DOTween.Kill(target.GetInstanceID() + "_Waddle");
            
            // 회전 초기화
            target.localRotation = Quaternion.identity;
        }
    }
}
