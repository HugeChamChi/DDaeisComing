# 때가왔다(DDaeisComing) — AI 공용 작업 규칙

Claude·Codex 등 이 저장소에서 일하는 모든 에이전트가 따르는 규칙이다. Claude는 [CLAUDE.md](CLAUDE.md)가 이 파일을 불러온다.

## 프로젝트
- 목욕탕 타이쿤(모바일 세로). 손님 입장 → 동선 따라 시설 이용 → 말풍선·미니게임 → 하루 정산(수익−지출−월세) → 상점 해금 → 다음 날. 다음 날을 골드 음수로 맞으면 파산.
- **Unity 2022.3.62f3**, 2D URP 14, UGUI + TextMeshPro, UniTask, DOTween. 실제 게임 씬은 `Assets/Scenes/EditScene.unity` 하나(빌드 씬).
- 코드: `Assets/Scripts/`(네임스페이스 `Bathhouse.*`), 에디터 도구 `Assets/Editor/`, 개인 작업 `Assets/WorkSpace/`. 데이터 SO: `Assets/Data/`, `Assets/Resources/`.

## 하네스 (먼저 읽기)
1. 작업 전에 [하네스 색인](Docs/harness/README.md)에서 시스템 카드를 고른다: `python Tools/harness/harness.py resolve "<요청>"`.
   카드의 진입 파일부터 열고, 카드에 없는 것만 더 탐색한다.
2. 파일을 고치기 전에 작업판에 잡는다: `python Tools/harness/harness.py lane set --task "무엇" --hold "경로,glob"`. 끝나면 `lane done --note "결과"`.
   레인 이름은 `GAE_LANE` 환경변수(세션 실행 별칭이 넣어 줌) 또는 `--lane`.
3. **다른 레인이 잡은 파일은 고치지 않는다.** 필요하면 사용자에게 묻는다.
4. 카드와 코드가 다르면 코드가 정답이다. 진입 파일·규칙을 바꿨으면 카드도 고치고 `verified`·`commit`을 갱신한다.
5. Codex는 훅이 없으므로 위 명령을 직접 실행하고, Unity 명령을 쓴 요청은 끝에 `harness.py describe <경로들>` 결과를 붙인다.

## 여러 폴더에서 동시 작업
- 메인 `E:/DDaeisComing`(main)과 실험실 `E:/DDaeisComing-lab`(work/lab)은 같은 저장소의 git 워크트리다. 다른 에이전트가 동시에 작업할 수 있다.
- 세션 시작 시 보이는 미커밋 변경은 **내가 만든 게 아닐 수 있다**. 내 작업과 관계없는 변경을 되돌리거나, 정리하거나, 같이 커밋하지 않는다.
- `EditScene.unity`, 프리팹 같은 큰 YAML은 충돌이 잦다. 고치기 전에 작업판을 확인하고, 가능하면 에디터에서 고친다.

## 코드 규칙 (현재 코드의 관례)
- 인게임 매니저는 퍼사드로 접근한다: `GameManager.MiniGame`, `GameManager.Interaction`, `GameManager.Facility`. 전역은 `GlobalManagers.Data`, `GlobalManagers.Save`.
- 하루 경계 처리는 `GameManager` 이벤트(OnGameStarted / OnNoMoreCustomers / OnDayEnded / OnNextDayStarted / OnGameOver)를 구독한다. 구독하면 `OnDestroy`에서 해제한다.
- 게임플레이 수치는 SO에 둔다(GameConfig, RentData, FacilityIncomeData, MiniGameData, DailySpawnData 등). 코드 필드 초기값으로 박지 않는다.
- private 필드는 `_camelCase`. 비동기는 UniTask, 취소 토큰(`GetCancellationTokenOnDestroy`)을 넘긴다.
- Update 계열에서 `Find*`/`GetComponent`를 호출하지 않는다(Claude는 훅이 막는다).
- 저장 데이터는 `GameData`(JsonUtility). 새 필드는 직렬화 가능한 필드로, 옛 세이브에서도 안전한 기본값으로.

## Unity 파일 규칙
- `.meta`는 GUID 원본이다. 에셋을 추가·이동·삭제할 때 `.meta`를 같이 다룬다.
- `FacilityType` 같은 직렬화되는 enum의 번호를 바꾸지 않는다(끝에 추가).
- 컴파일 확인 방법은 [Unity 도구 카드](Docs/harness/tools/unity.md). Unity 6용 CLI·Pipeline·mcp-unity는 이 프로젝트(2022.3)에서 쓰지 않는다.

## 커밋
- 메시지는 `[Feat] / [Fix] / [Chore] / [Refactor] / [Docs]` 태그로 시작하고 한국어로 쓴다. 예: `[Fix] ResetButton 참조 오류 수정`.
- 사용자가 요청할 때만 커밋·푸시한다. 내 작업 파일만 골라서 스테이징한다(`git add -A` 금지).
- 빌드 산출물(`*.apk`, `Builds/`)과 키스토어는 커밋하지 않는다.

## 결과 보고
- 확인한 것과 확인하지 않은 것을 구분한다. 정적으로 읽은 것과 에디터 Play로 본 것을 섞어 "동작 확인"이라고 하지 않는다.
