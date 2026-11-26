---
system: npc
title: 손님 NPC·동선·길찾기
keywords: npc, 손님, 고객, 스폰, spawn, 동선, route, 경로, 길찾기, pathfinding, a*, 만족도, satisfaction, 말풍선, speech bubble, 이동, 애니메이션, 퇴장, 예약
verified: 2026-11-26
commit: d08c723
---

**한 줄 요약**: `NPCSpawner`가 풀에서 손님을 꺼내고, 각 손님은 동선 프로필(`NPCRouteProfileSO`)의 단계를 따라 시설 자리를 **먼저 예약한 뒤** A* 경로로 이동·이용하고, 단계가 끝나면 퇴장한다.

## 흐름
1. 씬 시작 시 `SceneTestRunner`(EditScene)가 그리드를 만들고 `NPCSpawner.Initialize`(그리드, 스폰 좌표 `MapConfig.spawnGridPos`) → `GameManager.OnGameStarted`에 `NPCSpawner.StartSpawning` 연결.
2. 스폰 간격 = 하루 길이 ÷ `DailySpawnDataSO.GetCustomerCount`(일차별 손님 수). 꺼낼 때 `NPC_Base.Initialize` + `DailyRecordModel.AddVisit`.
3. `NPC_Base.DecideNextAction` → `NPCRouteController.DetermineNextFacility`. 필수 단계는 무조건, 선택 단계는 `baseProbability + 만족도 × satisfactionProbabilityWeight` 확률. 탕(Bath)을 안 거쳤으면 때밀이(ScrubArea)는 건너뛴다. 더 없으면 `FacilityType.None` → 퇴장.
4. `NPC_Base.MoveToTargetFacility`: `FacilityManager.GetRandomAvailableFacility` → `FacilityBase.ReserveSlot`(출발 전 자리 예약) → 상호작용 위치마다 `PathfindingService.FindPath` 시도 → `NPCMovement.MoveAlongPath`. 모든 위치가 막혔으면 예약 취소(`FacilityBase.CancelReservation`) 후 그 시설을 제외하고 재탐색.
5. 도착 → `NPC_Base.PerformActionRoutineAsync`: `FacilityBase.EnterFacility` → 말풍선(`NPCInteractionController.StartInteractionAsync`) → 이용 시간 동안 매 프레임 `FacilityBase.ProgressFacility`(카운터 제외) → `FacilityBase.ExitFacility` → 다음 행동.
6. 미니게임 중에는 `NPC_Base.SetActionTimerPaused`로 이용 타이머를 멈춘다. 말풍선 클릭/타임아웃 결과는 `NPC_Base.AddSatisfaction`과 `InteractionManager.AddSatisfaction`으로 반영.
7. 퇴장 `NPC_Base.ExitBathhouse` → 스포너 콜백으로 풀에 반환.

## 진입 파일
| 파일 | 역할 |
|---|---|
| `Assets/Scripts/Managers/NPCSpawner.cs` | 오브젝트 풀, 스폰 루프(UniTask), PathfindingService 생성·주입 |
| `Assets/Scripts/NPC/NPC_Base.cs` | 손님 본체. 행동 결정·예약·이동·이용 루틴·퇴장 |
| `Assets/Scripts/NPC/NPCRouteController.cs` | 동선 단계 진행과 확률 판정 |
| `Assets/Scripts/NPC/NPCMovement.cs` | 경로 따라 이동 |
| `Assets/Scripts/NPC/NPCInteractionController.cs` | 카운터·수건 보관함·단상 말풍선 타이머와 클릭 처리 |
| `Assets/Scripts/NPC/NPCAnimationController.cs` | 방향·시설별 애니메이션 |
| `Assets/Scripts/NPC/NPCProceduralAnimator.cs` | 절차적 애니메이션(숨쉬기·뒤뚱) 조합 |
| `Assets/Scripts/NPC/Animations/*.cs` | 절차적 애니메이션 단위 |
| `Assets/Scripts/NPC/NPCBrain.cs` | 옛 동선 결정기. 현재 NPC_Base는 NPCRouteController만 쓴다(미사용) |
| `Assets/Scripts/Pathfinding/PathfindingService.cs` | A* 경로 탐색 |
| `Assets/Scripts/Pathfinding/IGridMap.cs` | 그리드 인터페이스 |
| `Assets/Scripts/Grid/CustomGridMap.cs` | MapData 기반 그리드(EditScene에서 사용) |
| `Assets/Scripts/Managers/InteractionManager.cs` | 말풍선 풀, 전역 만족도 |
| `Assets/Scripts/UI/NPCInteractionSpeechBubbleUI.cs` | 말풍선 UI(남은 시간 채움) |
| `Assets/Scripts/Debug/NPC_DebugSatisfactionDisplay.cs` | 만족도 디버그 표시 |

## 데이터
| 파일 | 역할 |
|---|---|
| `Assets/Scripts/Data/NPCData.cs` | 손님 속도, 만족도 확률 가중치 등 |
| `Assets/Data/NPCData.asset` | 기본 손님 데이터 |
| `Assets/Scripts/Data/NPCRouteProfileSO.cs` | 동선 단계(시설 종류·필수 여부·기본 확률), 만족도 상호작용 |
| `Assets/Data/Route Profile.asset` | 기본 동선 |
| `Assets/Data/Route Profile_BodyDryerOnly.asset` | 바디 드라이어 테스트용 동선 |
| `Assets/Scripts/Data/DailySpawnDataSO.cs` | 일차별 손님 수 |
| `Assets/Data/DailySpawnData.asset` | 위 값 |
| `Assets/Scripts/Data/InteractionRewardSO.cs` | 말풍선 결과별 만족도 보상 |
| `Assets/Data/Interaction Reward.asset` | 위 값 |
| `Assets/Prefabs/NPC.prefab` | 손님 프리팹 |

## 불변 규칙·함정
- **예약 먼저, 이동 나중**. 이동 중 자리 검사를 매 프레임 하면 자기 예약 때문에 `CanEnter`가 false가 되어 스스로 취소하는 버그가 있었다(NPC_Base.Update 주석). 이 검사를 되살리지 않는다.
- 시설이 모두 꽉 차면 0.1초 뒤 재결정한다. 동선 단계는 이미 넘어갔으므로 그 단계는 건너뛴 셈이 된다.
- 때밀이 말풍선은 `ScrubAreaFacility`가 직접 띄운다. NPCInteractionController 대상 목록에 ScrubArea를 넣으면 말풍선이 두 개 뜬다.
- 정렬: 시설 이용 중에는 `DynamicDepthSorter`를 멈추고 시설 정렬값 +1로 고정, 끝나면 다시 켠다.

## 검증
| 파일 | 역할 |
|---|---|
| `Assets/Scenes/EditScene.unity` | Play 후 Console의 `[동선 확률]`, `[동선 규칙]` 로그로 판정 확인 |
| `Assets/Scripts/Managers/CustomGridTestRunner.cs` | InGame 씬용 그리드+스폰 테스트 런처 |
