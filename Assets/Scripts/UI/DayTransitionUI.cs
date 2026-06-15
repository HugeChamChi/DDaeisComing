using UnityEngine;
using TMPro;
using DG.Tweening;
using System;

namespace Bathhouse.UI
{
    public class DayTransitionUI : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private RectTransform currentDayRect;
        [SerializeField] private TMP_Text currentDayText;
        [Space]
        [SerializeField] private RectTransform nextDayRect;
        [SerializeField] private TMP_Text nextDayText;
        [Space]
        [SerializeField] private CanvasGroup backgroundGroup;

        [Header("Settings")]
        [SerializeField] private float dropDuration = 0.6f;
        [SerializeField] private float rushDuration = 0.4f;
        [SerializeField] private float flyDuration = 0.5f;
        [SerializeField] private float fadeOutDuration = 0.5f;
        [SerializeField, Range(0f, 1f)] private float targetBackgroundAlpha = 0.8f;
        
        /// <summary>
        /// 다음 날로 넘어갈 때의 역동적인 텍스트 연출을 재생합니다.
        /// </summary>
        public void PlayTransition(int currentDay, int nextDay, Action onComplete = null)
        {
            bool isFirstDay = (currentDay <= 0);

            // 텍스트 초기화
            if (currentDayText != null) currentDayText.text = $"Day {currentDay}";
            if (nextDayText != null) nextDayText.text = $"Day {nextDay}";

            // DOTween 초기 상태 세팅 (혹시 실행 중인 트윈이 있다면 킬)
            DOTween.Kill(currentDayRect);
            DOTween.Kill(nextDayRect);
            if (backgroundGroup != null) DOTween.Kill(backgroundGroup);
            if (nextDayText != null) DOTween.Kill(nextDayText);

            // 초기 위치 및 스케일 설정
            currentDayRect.anchoredPosition = new Vector2(0, 1000f); // 위쪽 중앙
            currentDayRect.localScale = Vector3.one * 1.5f;
            currentDayRect.localRotation = Quaternion.identity;

            nextDayRect.anchoredPosition = new Vector2(-3000f, 0); // 더 멀리 왼쪽 바깥
            nextDayRect.localScale = Vector3.one * 1.5f;
            nextDayRect.localRotation = Quaternion.identity;

            if (nextDayText != null) nextDayText.alpha = 1f;

            if (backgroundGroup != null)
            {
                backgroundGroup.alpha = 0f;
                backgroundGroup.gameObject.SetActive(true);
            }

            // 게임 오브젝트 활성화
            gameObject.SetActive(true);
            currentDayRect.gameObject.SetActive(!isFirstDay); // 첫 날엔 기존 Day를 아예 끕니다
            nextDayRect.gameObject.SetActive(true);

            // 시퀀스 생성 (timeScale=0 에서도 동작하도록)
            Sequence seq = DOTween.Sequence().SetUpdate(true);

            // 1. 현재 Day 텍스트가 위에서 중앙으로 떨어짐 (바운스 효과)
            if (!isFirstDay)
            {
                seq.Append(currentDayRect.DOAnchorPos(Vector2.zero, dropDuration).SetEase(Ease.OutBounce));
                seq.Join(currentDayRect.DOScale(Vector3.one, dropDuration).SetEase(Ease.OutBounce));
                
                // 동시에 배경 Alpha 켜짐
                if (backgroundGroup != null)
                {
                    seq.Join(backgroundGroup.DOFade(targetBackgroundAlpha, dropDuration));
                }

                seq.AppendInterval(0.3f); // 잠깐 대기

                // 2. 다음 날짜 텍스트가 왼쪽 끝에서 빠르게 중앙으로 돌진
                seq.Append(nextDayRect.DOAnchorPos(Vector2.zero, rushDuration).SetEase(Ease.InCubic));
                seq.Join(nextDayRect.DOScale(Vector3.one, rushDuration).SetEase(Ease.InCubic));

                // 3. 부딪히는 순간 (콜백 활용)
                seq.AppendCallback(() => 
                {
                    // 충돌 느낌: 다음 날짜 텍스트의 흔들림(Punch) 및 크기 반동 (SetUpdate 추가)
                    nextDayRect.DOPunchPosition(new Vector2(50f, 0), 0.3f, 10, 1f).SetUpdate(true);
                    nextDayRect.DOPunchScale(new Vector3(0.4f, 0.4f, 0), 0.3f, 10, 1f).SetUpdate(true);
                    
                    // 타격받은 원래 Day 텍스트가 오른쪽 끝으로 날아가며 회전하고 작아짐
                    currentDayRect.DOAnchorPos(new Vector2(1500f, 200f), flyDuration).SetEase(Ease.OutCubic).SetUpdate(true);
                    currentDayRect.DORotate(new Vector3(0, 0, -360f), flyDuration, RotateMode.FastBeyond360).SetEase(Ease.OutQuad).SetUpdate(true);
                    currentDayRect.DOScale(Vector3.zero, flyDuration).SetEase(Ease.OutCubic).SetUpdate(true);
                });
            }
            else
            {
                // 첫 날의 경우 부딪힐 대상이 없으므로 배경 켜짐과 함께 NextDay만 중앙으로 돌진 후 살짝 바운스
                if (backgroundGroup != null)
                {
                    seq.Append(backgroundGroup.DOFade(targetBackgroundAlpha, dropDuration));
                }
                
                // 배경 켜지는 것과 동시에 날아오기
                seq.Join(nextDayRect.DOAnchorPos(Vector2.zero, dropDuration).SetEase(Ease.OutBack));
                seq.Join(nextDayRect.DOScale(Vector3.one, dropDuration).SetEase(Ease.OutBack));
                
                seq.AppendCallback(() => 
                {
                    nextDayRect.DOPunchScale(new Vector3(0.2f, 0.2f, 0), 0.3f, 10, 1f).SetUpdate(true);
                });
            }

            // 연출 후 잠시 머무름
            seq.AppendInterval(1.5f);

            // 4. 다음 날짜 텍스트와 배경 서서히 사라짐
            if (nextDayText != null)
            {
                seq.Append(nextDayText.DOFade(0f, fadeOutDuration));
            }
            if (backgroundGroup != null)
            {
                seq.Join(backgroundGroup.DOFade(0f, fadeOutDuration));
            }

            // 완료 처리
            seq.OnComplete(() => {
                onComplete?.Invoke();
                gameObject.SetActive(false); // 연출 끝난 후 비활성화
            });
        }

        [Button]
        public void TestTransition()
        {
            if (Application.isPlaying)
            {
                // 테스트용으로 Day 1에서 Day 2로 넘어가는 연출 실행
                PlayTransition(1, 2, () => Debug.Log("테스트 연출 완료!"));
            }
            else
            {
                Debug.LogWarning("DOTween 애니메이션 테스트는 플레이 모드에서만 가능합니다.");
            }
        }
    }
}
