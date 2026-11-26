---
system: save-data
title: 저장·전역 데이터
keywords: 저장, save, 로드, load, 세이브, 세이브 파일, 암호화, crypto, 초기화, reset, globalmanagers, datamanager, gamedata, isavable, 자동 저장
verified: 2026-11-26
commit: d08c723
---

**한 줄 요약**: 씬 로드 전에 `GlobalManagers`가 `@GlobalManagers`(DontDestroyOnLoad)를 만들고 `SaveManager`·`DataManager`를 붙인다. 진행 데이터는 `GameData` 하나이며 JSON → AES 암호화 → 로컬 파일(BathhouseSave.dat)로 저장된다.

## 흐름
1. `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` → `GlobalManagers.Save`, `GlobalManagers.Data` 생성 → `SaveManager.InitAsync`(파일 로드) → `DataManager.InitializeData`(Resources에서 GameConfig·RentData·FacilityIncomeData 로드, 당일 기록 생성).
2. 저장 대상은 `ISavable`(LoadData/SaveData). `SaveManager.RegisterSavable`로 등록하면 즉시 현재 데이터를 주입받는다. 씬의 ISavable은 초기화 때 자동 수집도 한다.
3. `SaveManager.SaveGameAsync`: 각 ISavable의 SaveData로 GameData 수집 → JsonUtility.ToJson → `CryptoUtility.EncryptToBytes` → `FileHandler.WriteSafeAsync`. 1분마다 자동 저장 루프가 있다.
4. 명시적 저장 시점: 다음 날 시작(`GameManager.StartNextDay`), 상점 구매(`ShopManager.TryPurchaseItem`) → 둘 다 `DataManager.SaveData`.
5. 초기화: `ResetButton` → `DataManager.ResetData`(Day 1로 덮어쓰기) → 현재 씬 다시 로드.

## 진입 파일
| 파일 | 역할 |
|---|---|
| `Assets/Scripts/Managers/GlobalManagers.cs` | 전역 매니저 생성(씬 로드 전) |
| `Assets/Scripts/Managers/DataManager.cs` | GameData·DailyRecord·설정 SO 보관, 저장/리셋 |
| `Assets/Scripts/Save/SaveManager.cs` | ISavable 수집, 직렬화·암호화·파일 저장, 자동 저장 |
| `Assets/Scripts/Save/FileHandler.cs` | 안전 쓰기(임시 파일 후 교체), 읽기, 삭제 |
| `Assets/Scripts/Save/CryptoUtility.cs` | AES 암복호화 |
| `Assets/Scripts/Save/ISavable.cs` | 저장 인터페이스 |
| `Assets/Scripts/Data/GameData.cs` | 저장되는 진행 데이터 |
| `Assets/Scripts/UI/ResetButton.cs` | 확인 팝업 후 세이브 초기화 |
| `Assets/Scripts/Utils/MonoSingleton.cs` | 싱글턴 기반(DontDestroyOnLoad / 씬 한정 변형) |

## 불변 규칙·함정
- GameData는 `JsonUtility`로 직렬화된다. Dictionary·프로퍼티는 저장되지 않는다. 새 필드는 public 또는 `[SerializeField]` 필드로, 기존 세이브에서 기본값이 들어와도 안전하게 만든다.
- `DailyRecordModel`은 저장되지 않는다. 하루 도중 종료하면 당일 집계는 사라지고 다음 실행은 저장된 일차부터 시작한다.
- `GlobalManagers`는 씬에 배치하지 않는다. 씬에 `@GlobalManagers` 이름의 오브젝트가 있으면 생성을 건너뛴다.
- 세이브 파일은 Application.persistentDataPath 아래에 있다. 에디터에서 데이터를 처음부터 보려면 ResetButton 또는 FileHandler.DeleteSaveFiles 경로를 쓴다.

## 검증
| 파일 | 역할 |
|---|---|
| `Assets/Scenes/EditScene.unity` | 하루 진행 → 재실행 후 일차·골드 유지 확인, Console `[SaveManager]` 로그 |
