using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Bathhouse.Managers;
using Bathhouse.Data;
using TMPro;

namespace Bathhouse.UI
{
    public class ShopUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform slotContainer;
        [SerializeField] private GameObject upgradeSlotPrefab;
        [SerializeField] private Button closeButton;

        private Dictionary<string, UpgradeSlotUI> slotDictionary = new Dictionary<string, UpgradeSlotUI>();

        private void OnEnable()
        {
            // 골드 업데이트는 이제 CurrentGoldDisplay 컴포넌트가 담당합니다.
        }

        private void Start()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseShop);
            }

            InitializeShop();

            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OnItemPurchased += HandleItemPurchased;
            }
        }

        private void OnDestroy()
        {
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OnItemPurchased -= HandleItemPurchased;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseShop();
            }
        }

        private void InitializeShop()
        {
            if (ShopManager.Instance == null) return;
            if (slotContainer == null || upgradeSlotPrefab == null) return;

            // 기존 슬롯 초기화
            foreach (Transform child in slotContainer)
            {
                Destroy(child.gameObject);
            }
            slotDictionary.Clear();

            var items = ShopManager.Instance.AllShopItems;
            if (items == null) return;

            foreach (var item in items)
            {
                GameObject go = Instantiate(upgradeSlotPrefab, slotContainer);
                UpgradeSlotUI slotUI = go.GetComponent<UpgradeSlotUI>();

                if (slotUI != null)
                {
                    slotUI.Initialize(item);
                    slotDictionary[item.itemID] = slotUI;
                }
            }
        }

        private void HandleItemPurchased(string itemID)
        {
            if (slotDictionary.TryGetValue(itemID, out UpgradeSlotUI slotUI))
            {
                slotUI.UpdateState();
            }
        }
        
        public void RefreshAllSlots()
        {
            foreach (var kvp in slotDictionary)
            {
                kvp.Value.UpdateState();
            }
        }

        public void CloseShop()
        {
            gameObject.SetActive(false);
        }
    }
}
