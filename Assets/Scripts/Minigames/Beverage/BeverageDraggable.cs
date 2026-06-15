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

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();

            // Awake 시점에서 저장하여 프리팹 초기 배치 상태 유지
            initialPosition = rectTransform.anchoredPosition;
            initialParent = transform.parent;
            initialSiblingIndex = transform.GetSiblingIndex();
        }

        public void ResetPosition()
        {
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
