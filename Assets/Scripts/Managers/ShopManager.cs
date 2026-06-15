using System;
using System.Collections.Generic;
using UnityEngine;
using Bathhouse.Data;

namespace Bathhouse.Managers
{
    public class ShopManager : MonoBehaviour
    {
        private static ShopManager _instance;
        public static ShopManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<ShopManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("ShopManager");
                        _instance = go.AddComponent<ShopManager>();
                    }
                }
                return _instance;
            }
        }

        [SerializeField] private List<ShopItemDataSO> allShopItems = new List<ShopItemDataSO>();
        public List<ShopItemDataSO> AllShopItems => allShopItems;

        public event Action<string> OnItemPurchased;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        public bool IsItemUnlocked(string itemID)
        {
            if (global::GlobalManagers.Data?.Current?.shopData == null) return false;
            return global::GlobalManagers.Data.Current.shopData.unlockedItemIDs.Contains(itemID);
        }

        public bool TryPurchaseItem(string itemID)
        {
            ShopItemDataSO itemData = allShopItems.Find(x => x.itemID == itemID);
            if (itemData == null)
            {
                Debug.LogWarning($"[ShopManager] 상점 아이템을 찾을 수 없습니다: {itemID}");
                return false;
            }

            var gameData = global::GlobalManagers.Data?.Current;
            if (gameData == null || gameData.shopData == null)
            {
                Debug.LogError("[ShopManager] GameData 또는 ShopData가 존재하지 않습니다.");
                return false;
            }

            if (IsItemUnlocked(itemID))
            {
                Debug.Log($"[ShopManager] 이미 구매한 아이템입니다: {itemID}");
                return false;
            }

            if (gameData.currentGold >= itemData.cost)
            {
                // 재화 차감
                gameData.currentGold -= itemData.cost;
                
                // 해금 리스트에 추가
                gameData.shopData.unlockedItemIDs.Add(itemID);

                // 세이브 파일 갱신
                global::GlobalManagers.Data.SaveData();

                Debug.Log($"[ShopManager] 아이템 구매 성공: {itemData.itemName} (소모: {itemData.cost})");

                // 이벤트 브로드캐스트
                OnItemPurchased?.Invoke(itemID);

                return true;
            }
            else
            {
                Debug.Log($"[ShopManager] 재화가 부족합니다. 필요: {itemData.cost}, 보유: {gameData.currentGold}");
                return false;
            }
        }
    }
}
