using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Bathhouse.Data;

namespace Bathhouse.Facilities
{
    public class TowelStorageFacility : FacilityBase, IPointerClickHandler
    {
        [Header("Towel Settings")]
        [SerializeField] private TowelStorageDataSO towelData;
        [SerializeField] private SpriteRenderer visualRenderer;
        
        [Header("Interaction (Minigame Placeholder)")]
        [Tooltip("0개가 되었을 때 표시할 터치 유도용 아이콘/버튼 등")]
        [SerializeField] private GameObject interactionIcon;

        [Tooltip("수건 전체 화면 클릭 인식을 위한 버튼 (없으면 Collider2D 사용)")]
        [SerializeField] private Button clickButton;

        private int _currentTowelCount = 9;

        private void Start()
        {
            if (towelData != null)
                _currentTowelCount = towelData.maxTowelCount;
            
            if (visualRenderer == null)
            {
                visualRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (clickButton != null)
            {
                clickButton.onClick.AddListener(OnTowelTouched);
            }
            else if (GetComponent<Collider2D>() == null)
            {
                var col = gameObject.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                if (visualRenderer != null && visualRenderer.sprite != null)
                {
                    col.size = visualRenderer.sprite.bounds.size;
                    col.offset = visualRenderer.transform.localPosition + visualRenderer.sprite.bounds.center;
                }
                else
                {
                    col.size = new Vector2(1f, 1f);
                }
            }

            UpdateVisual();

            if (Bathhouse.Managers.GameManager.Instance != null)
            {
                Bathhouse.Managers.GameManager.Instance.OnNextDayStarted += Refill;
            }
        }

        private void OnDestroy()
        {
            if (Bathhouse.Managers.GameManager.Instance != null)
            {
                Bathhouse.Managers.GameManager.Instance.OnNextDayStarted -= Refill;
            }
        }

        // 자리가 찼다는 개념 없이 무제한 사용 가능하지만 수건이 0개면 대기
        public override bool CanEnter()
        {
            if (_currentTowelCount <= 0) return false;
            return _data != null;
        }

        public override int GetAvailableSlotIndex()
        {
            return 0; // 항상 0번 반환
        }

        public override bool ReserveSlot(NPC.NPC_Base npc, out int slotIndex)
        {
            slotIndex = 0;
            return true; // 항상 성공
        }

        public override void EnterFacility(NPC.NPC_Base npc, int slotIndex)
        {
            base.EnterFacility(npc, slotIndex);
            Debug.Log($"[TowelStorage] NPC가 수건 보관함을 사용합니다. 남은 수건: {_currentTowelCount}");
        }

        public override void ExitFacility(NPC.NPC_Base npc, int slotIndex)
        {
            base.ExitFacility(npc, slotIndex);

            if (_currentTowelCount > 0)
            {
                _currentTowelCount--;
                UpdateVisual();
            }

            Debug.Log($"[TowelStorage] NPC가 수건 보관함 사용을 마쳤습니다. 남은 수건: {_currentTowelCount}");
        }

        private void UpdateVisual()
        {
            if (towelData != null && visualRenderer != null && towelData.towelCountSprites != null)
            {
                int index = Mathf.Clamp(_currentTowelCount, 0, towelData.towelCountSprites.Length - 1);
                if (index < towelData.towelCountSprites.Length && towelData.towelCountSprites[index] != null)
                {
                    visualRenderer.sprite = towelData.towelCountSprites[index];
                }
            }

            if (interactionIcon != null)
            {
                interactionIcon.SetActive(_currentTowelCount <= 0);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnTowelTouched();
        }

        private void OnMouseDown()
        {
            if (IsPointerOverUI()) return;
            OnTowelTouched();
        }

        private void OnTowelTouched()
        {
            // 0개일 때 터치하면 9개로 리필. 차후 미니게임 추가 예정
            if (_currentTowelCount <= 0)
            {
                Debug.Log("[TowelStorage] 보관함을 리필합니다!");
                Refill();
            }
        }

        public void Refill()
        {
            if (towelData != null)
            {
                _currentTowelCount = towelData.maxTowelCount;
            }
            else
            {
                _currentTowelCount = 9;
            }
            UpdateVisual();
        }

        private bool IsPointerOverUI()
        {
            if (EventSystem.current == null) return false;
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
            if (Input.touchCount > 0)
            {
                return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
            }
#endif
            return EventSystem.current.IsPointerOverGameObject();
        }
    }
}
