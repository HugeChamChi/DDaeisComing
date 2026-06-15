using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DDaeisComing.Minigames.Beverage
{
    public class BeverageMinigameController : MonoBehaviour
    {
        [Header("Slots Configuration")]
        [SerializeField] private BeverageSlot[] slots = new BeverageSlot[6];
        [SerializeField] private float dropHitboxScale = 1.5f;

        [Header("Draggable Items")]
        [SerializeField] private List<BeverageDraggable> draggableItems;

        public event Action OnMinigameCleared;

        private void OnEnable()
        {
            InitializeMinigame();
        }

        public void InitializeMinigame()
        {
            if (slots.Length != 6)
            {
                Debug.LogWarning("BeverageMinigameController expects exactly 6 slots.");
            }

            // Reset all slots to filled first
            foreach (var slot in slots)
            {
                slot.SetFilled();
            }

            // Randomly select 3 slots to be empty
            var emptyIndices = GetRandomIndices(slots.Length, 3);
            foreach (int index in emptyIndices)
            {
                slots[index].SetEmpty();
            }

            // Enable 3 draggable items and reset their positions
            for (int i = 0; i < draggableItems.Count; i++)
            {
                if (i < 3)
                {
                    draggableItems[i].gameObject.SetActive(true);
                    draggableItems[i].ResetPosition();
                }
                else
                {
                    draggableItems[i].gameObject.SetActive(false);
                }
                draggableItems[i].minigameController = this;
            }
        }

        public bool TryDropBeverage(PointerEventData eventData)
        {
            BeverageSlot closestValidSlot = null;
            float closestDistanceSqr = float.MaxValue;

            foreach (var slot in slots)
            {
                if (slot.isFilled) continue;

                RectTransform slotRect = slot.rectTransform;
                
                // ScreenPointToLocalPointInRectangle converts screen point to local coordinates of the rect
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(slotRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
                {
                    Rect rect = slotRect.rect;
                    Vector2 centerLocal = rect.center;
                    
                    // Multiply size by 1.5f to increase the drop hitbox
                    float halfWidth = (rect.width * dropHitboxScale) / 2f;
                    float halfHeight = (rect.height * dropHitboxScale) / 2f;

                    if (Mathf.Abs(localPoint.x - centerLocal.x) <= halfWidth &&
                        Mathf.Abs(localPoint.y - centerLocal.y) <= halfHeight)
                    {
                        // Valid drop zone (within 1.5x bounding box)
                        float distSqr = (localPoint - centerLocal).sqrMagnitude;
                        if (distSqr < closestDistanceSqr)
                        {
                            closestDistanceSqr = distSqr;
                            closestValidSlot = slot;
                        }
                    }
                }
            }

            if (closestValidSlot != null)
            {
                closestValidSlot.SetFilled();
                CheckGameClear();
                return true;
            }

            return false;
        }

        private void CheckGameClear()
        {
            bool allFilled = true;
            foreach (var slot in slots)
            {
                if (!slot.isFilled)
                {
                    allFilled = false;
                    break;
                }
            }

            if (allFilled)
            {
                Debug.Log("Beverage Minigame Clear!");
                
                // Reward 500 Gold and Save Data
                global::GlobalManagers.Data.Current.currentGold += 500;
                global::GlobalManagers.Data.SaveData();
                Debug.Log("500 Gold rewarded and data saved.");

                OnMinigameCleared?.Invoke();
                // Optionally call MinigameManager.Instance.OnMinigameClear(); here based on architecture
            }
        }

        private List<int> GetRandomIndices(int count, int select)
        {
            List<int> indices = new List<int>();
            for (int i = 0; i < count; i++) indices.Add(i);

            // Fisher-Yates shuffle
            for (int i = 0; i < count; i++)
            {
                int r = UnityEngine.Random.Range(i, count);
                int temp = indices[i];
                indices[i] = indices[r];
                indices[r] = temp;
            }

            return indices.Take(select).ToList();
        }
    }
}
