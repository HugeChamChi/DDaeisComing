# AI 작업 하네스 — 입구

AI(Claude·Codex)가 **코드 전체를 탐색하지 않고** 시스템 카드 한 장으로 필요한 파일에 바로 가게 하고,
여러 작업 폴더(레인)에서 동시에 작업할 때 **지금 누가 무엇을 잡고 있는지**를 공유하기 위한 문서다.
GaeGGUL Defence의 하네스를 가져와 이 프로젝트(Unity 2022.3, 목욕탕 타이쿤) 경로에 맞게 고쳤다.

`harness.py check`는 카드의 경로·클래스·멤버가 실제로 있는지와 기준 커밋 이후 바뀐 파일을 검사한다.
동작 설명이 맞는지나 Play 성공까지 검사하지는 않으므로, 기능을 바꿀 때는 진입 파일을 직접 확인한다.

## AI 작업 규칙
1. **작업 전**: 아래 색인에서 해당 시스템 카드를 읽는다(`python Tools/harness/harness.py resolve "<요청>"`).
   카드의 진입 파일부터 열고, 카드에 없는 것만 추가로 탐색한다. Claude는 훅이 관련 카드를 자동으로 알려 준다.
2. **작업판 갱신**: 착수할 때 `lane set --task "무엇" --hold "수정할 경로,glob"`, 끝날 때 `lane done --note "결과 한 줄"`.
   다른 레인이 잡은 파일은 수정하지 않는다(Claude는 수정 직전에 확인 창이 뜬다).
3. **보고**: Unity 명령이나 `/스킬` 명령을 실행한 요청은 끝에 참조 파일을 보고한다.
   Claude는 훅이 자동으로 보고하고, Codex는 `harness.py describe <경로들>`의 결과를 응답에 붙인다.
4. **카드 유지**: 카드와 코드가 다르면 코드가 정답이다. 진입 파일·규칙이 바뀌었으면 카드를 고치고
   front matter의 `verified`·`commit`을 갱신한다. 카드에 적지 않은 사실을 지어내지 않는다.

## 시스템 색인
| 시스템 | 한 줄 | 카드 |
|---|---|---|
| **하루 사이클·정산** | GameManager 하루 타이머 → 월세 포함 정산 → 결과창 → 다음 날/파산 | [카드](systems/day-cycle.md) |
| **손님 NPC** | 스폰 풀, 동선 프로필 확률, 자리 예약 후 A* 이동, 말풍선·만족도 | [카드](systems/npc.md) |
| **시설** | FacilityBase 상속 구조, FacilityManager 등록, 이용 수익 기록 | [카드](systems/facility.md) |
| **미니게임** | 때밀이·쓰레기·음료·수건, 일차별 난이도, MiniGameManager 진입점 | [카드](systems/minigame.md) |
| **상점** | 아이템 SO, 구매·해금, OnItemPurchased로 시설 해금 | [카드](systems/shop.md) |
| **저장·전역 데이터** | GlobalManagers, GameData, ISavable, 암호화 파일 저장 | [카드](systems/save-data.md) |
| **맵·씬 편집** | SceneFacilityBuilder 타일, 맵 편집 창, SceneTestRunner, 정렬 | [카드](systems/map-editor.md) |

| 도구 | 한 줄 | 카드 |
|---|---|---|
| **Unity 명령·빌드** | 2022.3 기준 컴파일 확인, iOS 빌드, 로그 위치 | [카드](tools/unity.md) |

카드 형식: front matter(`keywords`·`verified`·`commit`) → 한 줄 요약 → 흐름 → 진입 파일 표 → 데이터 표 →
불변 규칙·함정 → 검증 표 → 더 읽을 곳. 표의 첫 열은 반드시 `` `경로` ``로 쓴다(검사·설명 조회에 쓰임).
흐름·규칙에 쓰는 `` `Class.Member` ``는 실제 코드에 있어야 한다(없으면 `check`가 DRIFT로 알린다).
카드는 60~90줄 안팎으로 유지한다.

## 레인 (작업 폴더 × 에이전트)
작업판은 **메인 폴더**의 `production/session-state/active.md` 맨 위에 있다.
하네스가 git 공용 디렉터리로 메인 폴더를 찾으므로, 워크트리(`E:/DDaeisComing-lab`)에서 실행해도 같은 작업판을 쓴다.

| 레인 | 실행 | 폴더 |
|---|---|---|
| rayumti_ddae / rayumti2_ddae / rayumti3_ddae | Claude 계정1~3 | 메인 `E:/DDaeisComing` (main) |
| rayumti_ddaelab / rayumti2_ddaelab / rayumti3_ddaelab | Claude 계정1~3 | 실험실 `E:/DDaeisComing-lab` (work/lab) |
| codex_ddae / codex_ddaelab | Codex | 메인 / 실험실 |

레인 이름은 실행 별칭(`Tools/launchers/DdaeSessions.ps1`, GaeGGUL 쪽에 있음)이 `GAE_LANE` 환경변수로 넘긴다.
별칭 없이 실행했다면 `$env:GAE_LANE = 'rayumti_ddae'`처럼 직접 지정하거나 명령에 `--lane 이름`을 붙인다.

```powershell
python Tools/harness/harness.py lane set --task "수건 미니게임 난이도 조정" --hold "Assets/Scripts/USW/**,Assets/Data/MiniGameData.asset"
python Tools/harness/harness.py lane done --note "난이도 곡선 수정 완료"
python Tools/harness/harness.py lane          # 작업판 보기
```

## Claude 훅이 하는 일
| 시점 | 동작 |
|---|---|
| 세션 시작 | `check --brief`: 카드가 코드와 어긋났을 때만 알리고, 작업 중인 레인과 이 세션의 레인을 표시 |
| 요청 제출 | 요청 문장으로 관련 카드·스킬을 찾아 컨텍스트에 넣음 |
| 파일 수정 직전 | 다른 레인이 잡은 파일이면 확인 창 |
| 도구 사용 후 | 읽은/수정한 파일, Unity 명령과 그 인자를 기록 |
| 응답 끝 | Unity 명령이나 `/스킬`이 있었던 요청만 참조 파일 보고. 기록은 `production/session-logs/harness/` |

Codex에는 훅이 없다. AGENTS.md의 하네스 규칙을 따라 `resolve`·`lane`·`describe`를 직접 실행한다.

## 유지보수
- `python Tools/harness/harness.py check` — 카드 경로·심볼, 확인 커밋 이후 바뀐 진입 파일, 작업판 상태.
- "확인 커밋 이후 변경" → 그 파일을 다시 읽고 카드가 여전히 맞는지 확인한 뒤 `verified`·`commit`을 갱신한다.
- `harness.py unregistered` → 보고에서 "카드 미등록"으로 쌓인 파일. 반복되는 파일은 해당 카드의 표에 추가한다.
- `production/`은 PC별 작업 상태라 git에서 제외한다.
