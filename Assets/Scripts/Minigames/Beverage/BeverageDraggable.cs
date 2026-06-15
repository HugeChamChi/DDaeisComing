using UnityEngine;
using UnityEngine.EventSystems;

namespace DDaeisComing.Minigames.Beverage
{
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public class BeverageDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Vector3 originalPosition;
        private Transform originalParent;

        private Vector3 initialPosition;
        private Transform initialParent;
        private int initialSiblingIndex;
        
        public BeverageMinigameController minigameController;

        private bool _initialized;

        private void Awake()
        {
            EnsureInitialized();
        }

        /// <summary>
        /// 컴포넌트 참조와 초기 배치 정보를 확보합니다.
        /// Awake가 아직 돌지 않은 상태(비활성 상태에서 ResetPosition 호출 등)에서도 안전하도록 방어합니다.
        /// </summary>
        private void EnsureInitialized()
        {
            if (_initialized) return;

            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();

            // 초기 배치 상태 저장 (프리팹 초기 위치 유지)
            initialPosition = rectTransform.anchoredPosition;
            initialParent = transform.parent;
            initialSiblingIndex = transform.GetSiblingIndex();

            _initialized = true;
        }

        public void ResetPosition()
        {
            EnsureInitialized();

            if (initialParent != null)
            {
                transform.SetParent(initialParent);
                transform.SetSiblingIndex(initialSiblingIndex);
            }
            rectTransform.anchoredPosition = initialPosition;
            if (canvasGroup != null) canvasGroup.blocksRaycasts = true;
            
            // LayoutGroup 등을 사용할 경우 강제 갱신
            if (initialParent != null)
            {
                UnityEngine.UI.LayoutRebuilder.MarkLayoutForRebuild(initialParent as RectTransform);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            originalPosition = rectTransform.position;
            originalParent = transform.parent;
            
            // Bring UI to front so it renders above other elements
            transform.SetAsLastSibling();
            // Disable raycast blocking so the drop event can pass through (though we handle drop manually, this is good practice)
            canvasGroup.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Follow pointer
            rectTransform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            canvasGroup.blocksRaycasts = true;

            if (minigameController != null)
            {
                bool dropped = minigameController.TryDropBeverage(eventData);
                if (dropped)
                {
                    // Snap to slot logic is handled by the controller (slot turns filled).
                    // We can just disable this draggable item since it's "consumed".
                    gameObject.SetActive(false);
                    return;
                }
            }
            
            // Invalid drop, snap back to original position and order
            rectTransform.position = originalPosition;
            transform.SetParent(originalParent);
            transform.SetSiblingIndex(initialSiblingIndex);
        }
    }
}
