---
system: shop
title: 상점·업그레이드
keywords: 상점, shop, 구매, purchase, 업그레이드, upgrade, 해금, unlock, 아이템, item, 골드, gold, 재화
verified: 2026-11-26
commit: d08c723
---

**한 줄 요약**: `ShopManager`가 상점 아이템 SO 목록을 들고, 구매하면 골드를 차감하고 `GameData.shopData`의 해금 목록에 ID를 넣은 뒤 저장하고 `ShopManager.OnItemPurchased`를 알린다. 시설(예: 음료 자판기)이 이 이벤트로 스스로 해금된다.

## 흐름
1. `ShopButton` → 상점 열기 → `ShopUI`가 `ShopManager.AllShopItems`로 `UpgradeSlotUI`를 만든다.
2. 슬롯 구매 버튼 → `ShopManager.TryPurchaseItem`: 아이템 찾기 → 이미 해금이면 거절 → 골드 확인 → 차감 → `ShopSaveData.unlockedItemIDs`에 추가 → `DataManager.SaveData` → `OnItemPurchased`.
3. 구독자: `ShopUI`(슬롯 갱신 `ShopUI.RefreshAllSlots`), `BeverageDispenserFacility`(해금 처리).
4. 해금 여부 조회는 `ShopManager.IsItemUnlocked`. 레벨형 업그레이드는 `ShopSaveData.GetUpgradeLevel`/`SetUpgradeLevel`.

## 진입 파일
| 파일 | 역할 |
|---|---|
| `Assets/Scripts/Managers/ShopManager.cs` | 아이템 목록, 구매, 해금 조회, 구매 이벤트 |
| `Assets/Scripts/UI/ShopUI.cs` | 상점 창 |
| `Assets/Scripts/UI/UpgradeSlotUI.cs` | 아이템 한 칸(가격, 구매/해금 상태) |
| `Assets/Scripts/UI/ShopButton.cs` | 상점 열기 버튼 |
| `Assets/Prefabs/UI/UpgradeSlot.prefab` | 슬롯 프리팹 |

## 데이터
| 파일 | 역할 |
|---|---|
| `Assets/Scripts/Data/ShopItemDataSO.cs` | 아이템 SO, 아이템 종류 enum, 저장용 ShopSaveData/UpgradeLevelData |
| `Assets/Data/Shop/*.asset` | 상점 아이템(현재 음료 자판기 1개) |

## 불변 규칙·함정
- 아이템은 `itemID` 문자열로 저장된다. 이미 배포·저장된 ID를 바꾸면 기존 세이브의 해금이 사라진다.
- 새 아이템은 SO를 만들고 EditScene의 `ShopManager.allShopItems` 목록에 넣어야 상점에 보인다.
- ShopManager는 EditScene에만 있다. `ShopManager.Instance`가 null인 씬(InGame)에서 구독 코드가 null 체크 없이 돌면 예외가 난다.

## 검증
| 파일 | 역할 |
|---|---|
| `Assets/Scenes/EditScene.unity` | 골드를 충분히 둔 세이브로 구매 → 자판기 해금 확인 |
| `Assets/Scripts/UI/ResetButton.cs` | 세이브 초기화 후 재확인 |
