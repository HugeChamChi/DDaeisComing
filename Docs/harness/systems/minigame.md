---
system: minigame
title: 미니게임
keywords: 미니게임, minigame, 때밀이, scrub, 쓰레기, trash, 음료, beverage, 수건, towel, 스포트라이트, spotlight, 난이도, 제한 시간, 드래그
verified: 2026-11-26
commit: d08c723
---

**한 줄 요약**: 시설이 조건을 만족하면(말풍선 클릭, 게이지 가득 등) `MiniGameManager`를 통해 해당 미니게임 UI를 띄우고, 성공/실패 콜백으로 시설 상태를 바꾼다. 대부분 `MiniGameBase`를 상속하고 일차에 따라 제한 시간이 줄어든다.

## 흐름
1. 진입 경로(시설 → 미니게임):
   - 때밀이: `ScrubAreaFacility.EnterFacility`가 말풍선을 띄우고, 클릭 시 손님 타이머를 멈춘 뒤 `ScrubMiniGameUI.StartMiniGame`.
   - 쓰레기: 말풍선 버튼 `TrashBinButtonController` → `MiniGameManager.ShowTrashBinMinigame`.
   - 음료: `BeverageDispenserFacility` 클릭 → `MiniGameManager.ShowBeverageMinigame`.
   - 수건: `TowelReturnFacility`가 가득 찬 상태에서 클릭 → `MiniGameManager.ShowTowelMinigame`(성공 시 `TowelReturnFacility.EmptyStorage`).
2. `MiniGameBase.StartMiniGame`: 난이도 = 기본 + (일차-1) × 일일 증가량(최대값 제한), 제한 시간 = 기본 시간 ÷ 난이도(최소 3초). 패널을 켜고 `OnGameStarted`.
3. 타이머가 0이 되면 `MiniGameBase.GameFailed`, 조건 달성 시 `MiniGameBase.GameSuccess` → 패널 끄고 콜백.
4. 하루가 끝나면 `MiniGameManager.ForceStopAllGames`가 열린 패널을 모두 닫는다. 다른 시스템은 `MiniGameManager.IsAnyMiniGamePlaying`으로 진행 여부를 본다.

## 진입 파일
| 파일 | 역할 |
|---|---|
| `Assets/Scripts/Managers/MiniGameManager.cs` | 미니게임 참조 보관, Show* 진입점, 하루 종료 시 강제 종료, 테스트 메뉴 |
| `Assets/Scripts/UI/MiniGameBase.cs` | 공통 기반. 난이도·타이머·성공/실패 |
| `Assets/Scripts/UI/ScrubMiniGameUI.cs` | 때밀이(드래그로 때 지우기) |
| `Assets/Scripts/UI/TrashMiniGameUI.cs` | 쓰레기 분류(드래그 앤 드롭) |
| `Assets/Scripts/UI/TrashItemUI.cs` | 쓰레기 아이템 |
| `Assets/Scripts/UI/TrashBinButtonController.cs` | 쓰레기통 말풍선 버튼 |
| `Assets/Scripts/Minigames/Beverage/BeverageMinigameUI.cs` | 음료 미니게임 열기/닫기 |
| `Assets/Scripts/Minigames/Beverage/BeverageMinigameController.cs` | 음료 진행 규칙 |
| `Assets/Scripts/Minigames/Beverage/BeverageDraggable.cs` | 음료 드래그 |
| `Assets/Scripts/Minigames/Beverage/BeverageSlot.cs` | 음료 슬롯 |
| `Assets/Scripts/USW/SpotlightMiniGameUI.cs` | 수건 줍기(스포트라이트) |
| `Assets/Scripts/USW/SpotlightMiniGameTester.cs` | 수건 미니게임 단독 테스트 |

## 데이터
| 파일 | 역할 |
|---|---|
| `Assets/Scripts/UI/MiniGameDataSO.cs` | 기본 난이도·일일 증가량·최대 난이도·기본 시간 |
| `Assets/Data/MiniGameData.asset` | 위 값 |
| `Assets/Resources/ScrubBubble.prefab` | 때밀이 거품 |
| `Assets/Prefabs/Trash_Item.prefab` | 쓰레기 아이템 |

## 불변 규칙·함정
- 때밀이 미니게임은 `ScrubAreaFacility`에서 일차를 **1로 고정**해서 넘긴다(TODO 주석). 난이도 곡선을 볼 때 이 점을 감안한다.
- 음료 미니게임은 MiniGameBase를 상속하지 않는다(별도 Controller/UI 구조). 공통 타이머·난이도 규칙이 적용되지 않는다.
- 때밀이 미니게임 중 손님 이용 타이머는 `NPC_Base.SetActionTimerPaused(true)`로 멈춘다. 끝날 때 풀어 주지 않으면 손님이 영원히 머문다.
- `MiniGameManager.scrubMiniGame`이 프리팹 참조이면 실행 시 Instantiate한다.

## 검증
| 파일 | 역할 |
|---|---|
| `Assets/Scripts/Managers/MiniGameManager.cs` | 인스펙터 버튼(TestStartScrubGame, TestStartTrashGame, TestStartTowelGame)과 `testDay`로 단독 실행 |
| `Assets/Scripts/USW/SpotlightMiniGameTester.cs` | 수건 미니게임 키 입력 테스트 |
