using Bathhouse.Managers;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Bathhouse.NPC;

namespace Bathhouse.UI
{
    /// <summary>
    /// NPC의 만족도 상호작용 말풍선 UI를 제어하는 클래스.
    /// 플레이어의 클릭 이벤트를 감지하여 컨트롤러에 전달하고 피드백을 보여줍니다.
    /// </summary>
    public class NPCInteractionSpeechBubbleUI : MonoBehaviour, IPointerClickHandler
    {
        [Header("References")]
        [SerializeField] private Image _fillImage; // 남은 시간을 시각적으로 보여줄 이미지 (Image Type: Filled 권장)
        [SerializeField] private CanvasGroup _canvasGroup; // 알파값 페이드 효과용
        [SerializeField] private Image _iconImage; // 말풍선 중앙에 표시될 아이콘
        
        [Header("Animation Settings")]
        [SerializeField] private float _clickAnimationDuration = 0.2f;
        [SerializeField] private Vector3 _clickScale = new Vector3(0.85f, 0.85f, 1f);

        private System.Action _onClickCallback;
        private Transform _targetTransform;
        private Vector3 _offset;
        private Vector3 _originalScale;
        private bool _isInteractable = false;

        private void Awake()
        {
            _originalScale = transform.localScale;
            if (_canvasGroup == null) 
                _canvasGroup = GetComponent<CanvasGroup>();
        }

        /// <summary>
        /// 초기 설정. 콜백과 추적할 대상 연결.
        /// </summary>
        public void Init(Transform targetTransform, Vector3 offset, System.Action onClick, Sprite icon)
        {
            _targetTransform = targetTransform;
            _offset = offset;
            _onClickCallback = onClick;
            
            if (_iconImage != null)
            {
                if (icon != null)
                {
                    _iconImage.sprite = icon;
                    _iconImage.gameObject.SetActive(true);
                }
                else
                {
                    _iconImage.gameObject.SetActive(false);
                }
            }
            
            Hide();
        }

        /// <summary>
        /// 말풍선 활성화 및 초기화
        /// </summary>
        public void Show()
        {
            _isInteractable = true;
            gameObject.SetActive(true);
            transform.localScale = _originalScale;
            
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }
            if (_fillImage != null)
            {
                _fillImage.fillAmount = 1f;
            }
        }

        /// <summary>
        /// 말풍선 비활성화
        /// </summary>
        public void Hide()
        {
            _isInteractable = false;
            gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (_targetTransform != null)
            {
                Canvas canvas = GetComponentInParent<Canvas>();
                if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    if (Camera.main != null)
                    {
                        Vector3 screenPos = Camera.main.WorldToScreenPoint(_targetTransform.position + _offset);
                        transform.position = screenPos;
                    }
                }
                else
                {
                    transform.position = _targetTransform.position + _offset;
                }
            }
        }

        /// <summary>
        /// 남은 시간 비율에 따라 UI 업데이트 (컨트롤러에서 호출)
        /// </summary>
        public void UpdateFill(float ratio)
        {
            if (_fillImage != null)
            {
                _fillImage.fillAmount = ratio;
            }
        }

        /// <summary>
        /// 플레이어 클릭 시 호출 (Unity UI EventSystem)
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_isInteractable) return;
            
            // 중복 클릭 방지
            _isInteractable = false;

            // 연결된 콜백 실행
            _onClickCallback?.Invoke();

            // 클릭 피드백 애니메이션 재생 후 숨기기 및 매니저 반환
            StartCoroutine(PlayClickFeedbackRoutine());
        }

        /// <summary>
        /// 클릭 시 약간 줄어들며 투명해지는 애니메이션 연출
        /// </summary>
        private IEnumerator PlayClickFeedbackRoutine()
        {
            float halfDuration = _clickAnimationDuration / 2f;
            float elapsed = 0f;

            // 1단계: 축소
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                
                // 간단한 Ease-Out 연출 (t * (2 - t))
                float easeOut = t * (2f - t);
                transform.localScale = Vector3.Lerp(_originalScale, _clickScale, easeOut);
                
                yield return null;
            }

            elapsed = 0f;
            
            // 2단계: 원래 크기로 복귀하며 페이드 아웃
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                float easeIn = t * t;

                transform.localScale = Vector3.Lerp(_clickScale, _originalScale, easeIn);
                
                if (_canvasGroup != null)
                {
                    _canvasGroup.alpha = Mathf.Lerp(1f, 0f, easeIn);
                }

                yield return null;
            }

            // 애니메이션 종료 후 풀로 반환
            if (GameManager.Interaction != null)
            {
                GameManager.Interaction.ReturnBubble(this);
            }
            else
            {
                Hide();
            }
        }
    }
}
