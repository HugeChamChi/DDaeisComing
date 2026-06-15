using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace GaeGGUL.Animation
{
    /// <summary>
    /// 크기가 지정된 시작 스케일(기본 0)에서 원래 크기로 팝업(Pop)되는 양방향(In/Out) 스케일 애니메이션입니다.
    /// 말풍선, 팝업창 등에 컴포넌트로 붙여서 바로 사용할 수 있습니다.
    /// </summary>
    public class Anim_UI_Pop : Anim_InOutBase
    {
        [Header("Pop Settings")]
        [Tooltip("PlayIn 시작 시 스케일 (기본: 0)")]
        [SerializeField] private Vector3 startScale = Vector3.zero;

        [Tooltip("PlayOut 종료 시 스케일 (기본: 0)")]
        [SerializeField] private Vector3 endScale = Vector3.zero;

        public override async UniTask PlayIn()
        {
            KillCurrentTween();

            // 1. 시작 스케일로 즉시 설정
            _target.localScale = startScale;

            // 2. 원래 스케일(_originScale)로 커지는 애니메이션
            _currentSeq = DOTween.Sequence()
                .SetUpdate(ignoreTimeScale)
                .Append(GetScaleTween(_originScale, durationIn).SetEase(easeIn))
                .SetDelay(delayIn);

            await _currentSeq.Play().ToUniTask();
        }

        public override async UniTask PlayOut()
        {
            KillCurrentTween();

            // 설정된 목표 스케일(endScale)로 작아지는 애니메이션
            _currentSeq = DOTween.Sequence()
                .SetUpdate(ignoreTimeScale)
                .Append(GetScaleTween(endScale, durationOut).SetEase(easeOut))
                .SetDelay(delayOut);

            await _currentSeq.Play().ToUniTask();
        }
    }
}
