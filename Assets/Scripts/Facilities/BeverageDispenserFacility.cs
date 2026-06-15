using UnityEngine;
using UnityEngine.UI;
using Bathhouse.Managers;
using Bathhouse.Save;
using Bathhouse.Data;

namespace Bathhouse.Facilities
{
    public class BeverageDispenserFacility : FacilityBase, ISavable
    {
        [Header("Interaction UI")]
        [SerializeField] private Button minigameButton; // 월드 캔버스의 버튼 할당
        [SerializeField] private GameObject minigameIcon; // 버튼과 함께 켜질 시각적 오브젝트 (선택사항)

        private bool isMinigameUnlocked = false;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OnItemPurchased += HandleItemPurchased;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OnItemPurchased -= HandleItemPurchased;
            }
        }

        private void Start()
        {
            if (minigameButton != null)
            {
                minigameButton.onClick.AddListener(OnInteractButtonClicked);
            }

            // SaveManager에 등록하여 Json GameData 로드 완료 시점에 LoadData 콜백을 받음
            if (global::GlobalManagers.Save != null)
            {
                global::GlobalManagers.Save.RegisterSavable(this);
            }
        }

        private void OnDestroy()
        {
            if (global::GlobalManagers.Save != null)
            {
                global::GlobalManagers.Save.UnregisterSavable(this);
            }
        }

        public void LoadData(GameData data)
        {
            if (data != null && data.shopData != null)
            {
                isMinigameUnlocked = data.shopData.unlockedItemIDs.Contains("Unlock_BeverageMinigame");
                UpdateButtonState();
            }
        }

        public void SaveData(ref GameData data)
        {
            // 이 컴포넌트에서 직접 저장할 데이터는 없음 (상점이 저장 관리)
        }

        private void HandleItemPurchased(string itemID)
        {
            if (itemID == "Unlock_BeverageMinigame")
            {
                isMinigameUnlocked = true;
                Debug.Log("[BeverageDispenser] 음료 디스펜서 미니게임이 해금되었습니다!");
                UpdateButtonState();
            }
        }

        private void UpdateButtonState()
        {
            if (minigameButton != null)
            {
                minigameButton.gameObject.SetActive(isMinigameUnlocked);
            }
            
            if (minigameIcon != null)
            {
                minigameIcon.SetActive(isMinigameUnlocked);
            }
        }

        private void OnInteractButtonClicked()
        {
            if (!isMinigameUnlocked) return;

            Debug.Log("[BeverageDispenser] 미니게임 진입 상호작용 클릭! 미니게임 시스템 연동 개시.");
            
            if (Bathhouse.Managers.GameManager.MiniGame != null)
            {
                Bathhouse.Managers.GameManager.MiniGame.ShowBeverageMinigame(this);
            }
            else
            {
                Debug.LogError("[BeverageDispenser] GameManager.MiniGame 인스턴스를 찾을 수 없습니다.");
            }
        }

        public override void EnterFacility(NPC.NPC_Base npc, int slotIndex)
        {
            base.EnterFacility(npc, slotIndex);
            Debug.Log($"[BeverageDispenser] NPC가 음료수 자판기 {slotIndex}번 자리를 사용합니다.");
        }

        public override void ExitFacility(NPC.NPC_Base npc, int slotIndex)
        {
            base.ExitFacility(npc, slotIndex);
            Debug.Log($"[BeverageDispenser] NPC가 음료수 자판기 {slotIndex}번 자리 사용을 마쳤습니다.");
        }
    }
}
