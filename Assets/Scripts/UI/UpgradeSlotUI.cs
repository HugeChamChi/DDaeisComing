using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Bathhouse.Data;
using Bathhouse.Managers;

namespace Bathhouse.UI
{
    public class UpgradeSlotUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image imgIcon;
        [SerializeField] private TMP_Text txtName;
        [SerializeField] private TMP_Text txtDesc;
        [SerializeField] private TMP_Text txtCost;
        [SerializeField] private Button btnPurchase;
        [SerializeField] private GameObject objSoldOut;

        private ShopItemDataSO currentItemData;

        public void Initialize(ShopItemDataSO itemData)
        {
            currentItemData = itemData;

            if (imgIcon != null) imgIcon.sprite = itemData.itemIcon;
            if (txtName != null) txtName.text = itemData.itemName;
            if (txtDesc != null) txtDesc.text = itemData.itemDescription;
            if (txtCost != null) txtCost.text = itemData.cost.ToString("N0");

            if (btnPurchase != null)
            {
                btnPurchase.onClick.RemoveAllListeners();
                btnPurchase.onClick.AddListener(OnPurchaseClicked);
            }

            UpdateState();
        }

        public void UpdateState()
        {
            if (currentItemData == null) return;

            bool isUnlocked = ShopManager.Instance.IsItemUnlocked(currentItemData.itemID);
            
            if (objSoldOut != null) objSoldOut.SetActive(isUnlocked);
            if (btnPurchase != null) btnPurchase.interactable = !isUnlocked;
        }

        private void OnPurchaseClicked()
        {
            if (currentItemData == null) return;

            ShopManager.Instance.TryPurchaseItem(currentItemData.itemID);
        }
    }
}
