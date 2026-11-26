---
system: facility
title: 시설(구조물)
keywords: 시설, facility, 구조물, 탕, bath, 샤워, shower, 사우나, sauna, 때밀이, scrub, 카운터, counter, 탈의실, locker, 수건, towel, 쓰레기통, trash, 음료, beverage, 자판기, 단상, platform, 드라이어, 슬롯, slot, 수익, income, 이용 횟수
verified: 2026-11-26
commit: d08c723
---

**한 줄 요약**: 모든 시설은 `FacilityBase`를 상속하고, `FacilityManager`가 타입별 목록으로 관리한다. 손님이 이용을 마치면 시설 종류별 수익(`FacilityIncomeDataSO`)이 당일 기록에 쌓인다.

## 흐름
1. 등록: EditScene은 씬에 배치된 `SceneFacilityInfo`를 `FacilityManager.RegisterSceneFacility`로, InGame 씬은 맵 JSON을 `FacilityManager.InitializeFromMapData`로 등록한다. 컴포넌트가 없으면 FacilityType에 맞는 클래스를 자동으로 붙인다.
2. `FacilityBase.Initialize`(SO, 그리드 좌표, 노드 크기) → 위치 갱신, 상호작용 콜라이더 보장.
3. 손님 쪽: `FacilityManager.GetRandomAvailableFacility` → `FacilityBase.ReserveSlot` → `FacilityBase.EnterFacility` → `FacilityBase.ProgressFacility`(매 프레임) → `FacilityBase.ExitFacility`.
4. 이용 완료 시 `FacilityBase.RecordUsageAndIncome` → `DailyRecordModel.AddFacilityUsage` + `FacilityIncomeDataSO.GetIncomeData`의 금액으로 `DailyRecordModel.AddIncome`.
5. `TeleportToSlotOnUse`가 켜진 시설은 이용 중 손님을 슬롯(U 마커) 위치로 옮기고, 끝나면 상호작용 위치(O 마커)로 되돌린다.

## 진입 파일
| 파일 | 역할 |
|---|---|
| `Assets/Scripts/Facilities/FacilityBase.cs` | 공통 기반. 슬롯·예약·입퇴장·수익 기록·정렬·상호작용 위치 |
| `Assets/Scripts/Facilities/IFacility.cs` | NPC가 보는 시설 인터페이스 |
| `Assets/Scripts/Managers/FacilityManager.cs` | 타입별 시설 목록, 등록, 빈 시설 무작위 선택 |
| `Assets/Scripts/Data/FacilityType.cs` | 시설 종류 enum(번호 고정) |
| `Assets/Scripts/Facilities/BathFacility.cs` | 탕. 하루 시작 시 물 정화 |
| `Assets/Scripts/Facilities/BathWaterController.cs` | 탕 물 오염도 표현 |
| `Assets/Scripts/Facilities/ScrubAreaFacility.cs` | 때밀이. 말풍선 클릭 → 때밀이 미니게임 |
| `Assets/Scripts/Facilities/TowelStorageFacility.cs` | 수건 보관함. 클릭 보충, 하루 시작 시 Refill |
| `Assets/Scripts/Facilities/TowelReturnFacility.cs` | 수건 반납함. 가득 차면 클릭 → 수건 미니게임 |
| `Assets/Scripts/Facilities/DroppedTowel.cs` | 떨어진 수건 |
| `Assets/Scripts/Facilities/TrashBinController.cs` | 쓰레기통. 이용마다 게이지 증가 |
| `Assets/Scripts/Facilities/BeverageDispenserFacility.cs` | 음료 자판기. 상점 구매로 해금, 저장 대상(ISavable) |
| `Assets/Scripts/Facilities/TidyUpInteractionTrigger.cs` | 정리 상호작용 트리거 |
| `Assets/Scripts/Facilities/CounterFacility.cs` | 카운터 |
| `Assets/Scripts/Facilities/LockerRoomFacility.cs` | 탈의실 |
| `Assets/Scripts/Facilities/PlatformFacility.cs` | 단상 |
| `Assets/Scripts/Facilities/ShowerFacility.cs` | 샤워 |
| `Assets/Scripts/Facilities/SaunaFacility.cs` | 사우나 |
| `Assets/Scripts/Facilities/BodyDryerFacility.cs` | 바디 드라이어 |
| `Assets/Scripts/Facilities/DisposableDispenserFacility.cs` | 일회용품 디스펜서 |

## 데이터
| 파일 | 역할 |
|---|---|
| `Assets/Scripts/Data/FacilityData.cs` | 시설 SO(타입, 크기, 이용 시간, 프리팹 등) |
| `Assets/Resources/Facilities/**/*.asset` | 시설별 FacilityData(InBath/OutBath 폴더) |
| `Assets/Scripts/Data/FacilityIncomeDataSO.cs` | 시설 종류별 수익 |
| `Assets/Resources/FacilityIncomeData.asset` | 위 값(Resources 이름 고정 `FacilityIncomeData`) |
| `Assets/Scripts/Data/TrashBinDataSO.cs` | 쓰레기통 최대 게이지, 이용당 증가량 |
| `Assets/Data/TrashBinData.asset` | 위 값 |
| `Assets/Scripts/Data/TowelStorageDataSO.cs` | 수건 보관함 값 |
| `Assets/Data/TowelStorageData.asset` | 위 값 |
| `Assets/Prefabs/Facility/**/*.prefab` | 시설 프리팹 |
| `Assets/Editor/FacilityDataEditor.cs` | FacilityData 인스펙터 |

## 불변 규칙·함정
- `FacilityType` 번호는 씬·SO·저장 데이터에 숫자로 직렬화된다. 중간에 끼워 넣거나 번호를 바꾸지 말고 끝에 추가한다.
- 새 시설 종류를 추가하면 `FacilityManager`의 타입→컴포넌트 매핑(AddFacilityComponent)과 `FacilityIncomeDataSO` 항목도 같이 추가한다.
- 슬롯 수를 넘는 예약을 막는 곳은 `ReserveSlot`이다. 하위 클래스가 `CanEnter`/`ReserveSlot`을 재정의하면(수건 시설) 예약 규칙도 같이 지켜야 한다.
- 카운터는 `ProgressFacility`가 호출되지 않는다(NPC_Base에서 제외).

## 검증
| 파일 | 역할 |
|---|---|
| `Assets/Scenes/EditScene.unity` | Play 후 하루를 돌려 결과창 시설별 이용 횟수·수익 확인 |
