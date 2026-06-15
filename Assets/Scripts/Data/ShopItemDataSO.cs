using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bathhouse.Data
{
    public enum ShopItemType
    {
        UnlockFacility,
        UpgradeFacility,
        Buff
    }

    [CreateAssetMenu(fileName = "NewShopItem", menuName = "Bathhouse/ShopItemData")]
    public class ShopItemDataSO : ScriptableObject
    {
        public string itemID;
        public string itemName;
        [TextArea]
        public string itemDescription;
        public int cost;
        public Sprite itemIcon;
        public ShopItemType itemType;
    }

    [Serializable]
    public class UpgradeLevelData
    {
        public string itemID;
        public int level;

        public UpgradeLevelData(string id, int lvl)
        {
            itemID = id;
            level = lvl;
        }
    }

    [Serializable]
    public class ShopSaveData
    {
        public List<string> unlockedItemIDs;
        public List<UpgradeLevelData> itemUpgradeLevels;

        public ShopSaveData()
        {
            unlockedItemIDs = new List<string>();
            itemUpgradeLevels = new List<UpgradeLevelData>();
        }

        public int GetUpgradeLevel(string itemID)
        {
            if (itemUpgradeLevels == null) return 0;
            var data = itemUpgradeLevels.Find(x => x.itemID == itemID);
            return data != null ? data.level : 0;
        }

        public void SetUpgradeLevel(string itemID, int level)
        {
            if (itemUpgradeLevels == null)
            {
                itemUpgradeLevels = new List<UpgradeLevelData>();
            }

            var data = itemUpgradeLevels.Find(x => x.itemID == itemID);
            if (data != null)
            {
                data.level = level;
            }
            else
            {
                itemUpgradeLevels.Add(new UpgradeLevelData(itemID, level));
            }
        }
    }
}
