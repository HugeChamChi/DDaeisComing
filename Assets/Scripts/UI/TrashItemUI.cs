using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bathhouse.MiniGames
{
    public class TrashItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private TrashMiniGameUI _gameController;
        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private Vector2 _originalPosition;
        private Canvas _parentCanvas;
        private Image _image;
        [SerializeField] private Sprite[] trashSprites; // 쓰레기 아이템별 스프라이트 배열

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _image = GetComponent<Image>();
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void Start()
        {
            _parentCanvas = GetComponentInParent<Canvas>();
        }

        public void Initialize(TrashMiniGameUI gameController)
        {
            _gameController = gameController;
            // 캔버스 강제 업데이트 이후 포지션을 가져오는 것이 안전하지만 Awake/Spawn 직후이므로 anchoredPosition을 캐싱
            _originalPosition = _rectTransform.anchoredPosition;
            RandomSprites();
        }

        private void RandomSprites()
        {
            if (trashSprites != null && trashSprites.Length > 0 && _image != null)
            {
                int randomIndex = Random.Range(0, trashSprites.Length);
                _image.sprite = trashSprites[randomIndex];
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _originalPosition = _rectTransform.anchoredPosition;
            _canvasGroup.alpha = 0.6f;
            _canvasGroup.blocksRaycasts = false; // 드래그 중 다른 UI 이벤트를 막지 않도록
            
            // 드래그 될 때 최상단으로 렌더링되게 하기 위해 부모의 마지막 자식으로 보냄
            transform.SetAsLastSibling();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_parentCanvas != null)
            {
                _rectTransform.anchoredPosition += eventData.delta / _parentCanvas.scaleFactor;
            }
            else
            {
                _rectTransform.anchoredPosition += eventData.delta;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _canvasGroup.alpha = 1.0f;
            _canvasGroup.blocksRaycasts = true;

            if (_gameController != null)
            {
                _gameController.OnTrashDropped(this, eventData.position);
            }
        }

        public void ResetPosition()
        {
            _rectTransform.anchoredPosition = _originalPosition;
        }
    }
}
