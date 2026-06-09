using UnityEngine;
using DG.Tweening;

namespace Bathhouse.NPC.Animations
{
    [CreateAssetMenu(fileName = "New Breath Animation", menuName = "Bathhouse/Procedural Animations/Breath")]
    public class BreathProceduralAnimation : ProceduralAnimationBase
    {
        [Header("Breath Settings")]
        [Tooltip("숨을 들이마실 때 커지는 X, Y축 스케일 배수")]
        public Vector2 scaleMultiplier = new Vector2(1.05f, 0.95f);

        public override void PlayAnimation(Transform target, Vector3 baseScale)
        {
            if (target == null) return;
            
            // 기존 트윈이 있다면 확실히 제거
            StopAnimation(target, baseScale);

            // 각 대상마다 독립적인 로컬 변수(클로저)를 사용하여 멀티 NPC 지원
            Vector2 currentBreath = Vector2.one;

            Sequence seq = DOTween.Sequence();
            seq.Append(DOTween.To(() => currentBreath, x => currentBreath = x, scaleMultiplier, duration).SetEase(easeType))
               .Append(DOTween.To(() => currentBreath, x => currentBreath = x, Vector2.one, duration).SetEase(easeType))
               .OnUpdate(() =>
               {
                   if (target != null)
                   {
                       // 외부(NPCAnimationController)에서 좌우 반전(Scale X)이 일어나도 부호를 유지합니다.
                       float signX = target.localScale.x < 0 ? -1f : 1f;
                       float signY = target.localScale.y < 0 ? -1f : 1f;
                       float signZ = target.localScale.z < 0 ? -1f : 1f;

                       target.localScale = new Vector3(
                           signX * Mathf.Abs(baseScale.x) * currentBreath.x,
                           signY * Mathf.Abs(baseScale.y) * currentBreath.y,
                           signZ * Mathf.Abs(baseScale.z)
                       );
                   }
               })
               .SetLoops(-1)
               .SetId(target.GetInstanceID() + "_Breath");
        }

        public override void StopAnimation(Transform target, Vector3 baseScale)
        {
            if (target == null) return;

            // 고유 ID로 트윈 종료
            DOTween.Kill(target.GetInstanceID() + "_Breath");
            
            // 부호를 유지하며 스케일 원상복구
            if (target != null)
            {
                float signX = target.localScale.x < 0 ? -1f : 1f;
                float signY = target.localScale.y < 0 ? -1f : 1f;
                float signZ = target.localScale.z < 0 ? -1f : 1f;

                target.localScale = new Vector3(
                    signX * Mathf.Abs(baseScale.x),
                    signY * Mathf.Abs(baseScale.y),
                    signZ * Mathf.Abs(baseScale.z)
                );
            }
        }
    }
}
