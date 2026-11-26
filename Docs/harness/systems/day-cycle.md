---
system: day-cycle
title: 하루 사이클·정산
keywords: 하루, day, 날짜, 일차, 타이머, 정산, 결과창, 월세, rent, 지출, 수익, 파산, 게임오버, game over, 다음 날, 시작 버튼, 일일 결과, dailyresult, daytransition
verified: 2026-11-26
commit: d08c723
---

**한 줄 요약**: `GameManager`가 하루 타이머를 돌리고, 끝나면 월세를 더해 정산한 뒤 결과창 → 다음 날(또는 파산) 순서로 넘긴다. 다른 시스템은 GameManager 이벤트를 구독해서 하루 경계에 반응한다.

## 흐름
1. 시작 버튼 `GameStartUI` → `GameManager.StartGame` → `GameManager.OnGameStarted`, 이어서 첫날 연출을 위해 `GameManager.OnNextDayStarted`를 한 번 더 발생시킨다.
2. 매 프레임 `GameManager` 내부 Update가 `currentDayTime`을 올린다. 하루 길이는 `GameConfigSO.dayDurationSeconds`(기본 180초).
3. 남은 시간이 `GameConfigSO.noCustomerTimeThreshold` 이하가 되면 `GameManager.OnNoMoreCustomers` → `NPCSpawner.StopSpawning`.
4. 시간이 다 되면 정산: 오늘 수익 = `DailyRecordModel.totalIncome`. 월세일이면(`RentDataSO.IsRentDay`) `RentDataSO.GetRentAmount`를 지출에 더하고 `DailyRecordModel.AddExpense`로 기록한 뒤 `GameData.ApplyTodayProfit`로 골드에 반영. 그다음 `GameManager.OnDayEnded`.
5. `OnDayEnded` 구독자: `DailyResultUI.ShowResult`(결과창), `MiniGameManager.ForceStopAllGames`(진행 중 미니게임 강제 종료).
6. 결과창 확인 → `GameManager.StartNextDay`. 골드가 음수면 `GameManager.OnGameOver` → `GameOverUI`. 아니면 `GameData.AdvanceToNextDay` → `DailyRecordModel.Reset` → `DataManager.SaveData` → `OnNextDayStarted`.
7. `OnNextDayStarted` 구독자: `DayTransitionController`(날짜 넘김 연출 `DayTransitionUI.PlayTransition`), `DayCounterUI`, 스포너 재시작, 시설 리셋(`BathFacility.PurifyWater`, `TowelStorageFacility.Refill`, `TowelReturnFacility.EmptyStorage`, `TrashBinController.ReduceGauge`), `InteractionManager` 말풍선 정리.

## 진입 파일
| 파일 | 역할 |
|---|---|
| `Assets/Scripts/Managers/GameManager.cs` | 하루 타이머, 정산, 이벤트 5종, 인게임 매니저 퍼사드(`GameManager.MiniGame` 등) |
| `Assets/Scripts/UI/GameStartUI.cs` | 시작 버튼 → StartGame |
| `Assets/Scripts/UI/DayTimerUI.cs` | 남은 시간 표시 |
| `Assets/Scripts/UI/DayCounterUI.cs` | 일차 표시 |
| `Assets/Scripts/UI/DayTransitionController.cs` | OnNextDayStarted → 넘김 연출 호출 |
| `Assets/Scripts/UI/DayTransitionUI.cs` | 날짜 넘김 연출(DOTween) |
| `Assets/Scripts/UI/DailyResultUI.cs` | 일일 결과창(시설별 판매 슬롯), 확인 시 StartNextDay |
| `Assets/Scripts/UI/SalesSlotUI.cs` | 결과창 시설별 한 줄 |
| `Assets/Scripts/UI/GameOverUI.cs` | 파산 화면 |
| `Assets/Scripts/UI/CurrentGoldDisplay.cs` | 보유 골드 표시 |

## 데이터
| 파일 | 역할 |
|---|---|
| `Assets/Scripts/Data/GameConfigSO.cs` | 하루 길이, 손님 마감 시간 |
| `Assets/Resources/GameConfig.asset` | 위 SO 실제 값(Resources 이름 고정 `GameConfig`) |
| `Assets/Scripts/Data/RentDataSO.cs` | 월세 주기·금액 |
| `Assets/Resources/RentData.asset` | 월세 실제 값(Resources 이름 고정 `RentData`) |
| `Assets/Scripts/Data/DailyRecordModel.cs` | 당일 방문·시설 이용·수익·지출 집계(저장 안 됨, 메모리) |
| `Assets/Scripts/Data/GameData.cs` | 저장되는 진행 데이터(일차, 골드, 누적 통계, 상점) |

## 불변 규칙·함정
- 정산은 결과창이 뜨기 **전에** 이미 골드에 반영된다. 결과창에서 다시 더하지 않는다.
- 파산 판정은 `StartNextDay` 시점(결과창 확인 후)이다. 하루 도중 골드가 음수가 돼도 게임오버가 아니다.
- 이벤트 구독은 대부분 `Start`/`OnEnable`에서 `GameManager.Instance`가 있을 때만 한다. GameManager보다 먼저 활성화되는 오브젝트는 구독이 빠질 수 있다.
- GameManager는 EditScene에만 있다(빌드 씬도 EditScene 하나). InGame 씬에는 GameManager가 없다.
- `StartNextDay`는 `Time.timeScale = 1`로 되돌린다. 결과창 등에서 timeScale을 바꿨다면 여기서 풀린다.

## 검증
| 파일 | 역할 |
|---|---|
| `Assets/Scenes/EditScene.unity` | 실제 플레이 씬. Play → 시작 버튼 → 하루 종료까지 확인 |
| `Assets/Scripts/UI/DayTransitionUI.cs` | `TestTransition` 컨텍스트 메뉴로 연출만 단독 확인 |

## 더 읽을 곳
| 파일 | 역할 |
|---|---|
| `Docs/harness/systems/save-data.md` | 저장 시점과 GameData |
| `Docs/harness/systems/facility.md` | 시설 이용 수익이 DailyRecord에 쌓이는 경로 |
