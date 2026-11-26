---
name: bug-report
description: "버그 설명을 재현 단계·심각도·관련 파일이 들어간 구조화된 버그 리포트로 만들거나(기본), 코드를 읽고 잠재 버그를 찾거나(analyze), 수정이 실제로 됐는지 확인(verify)·종결(close)한다. '버그 리포트', '버그 정리', '이거 버그 같아', 'bug report' 요청에 사용."
argument-hint: "[버그 설명] | analyze [파일 경로] | verify [BUG-ID] | close [BUG-ID]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Bash
---

GaeGGUL Defence의 bug-report 스킬을 이 프로젝트(Unity 2022.3 목욕탕 타이쿤)에 맞게 옮긴 것이다.
버그 파일은 `Docs/qa/bugs/BUG-NNNN.md`에 둔다.

## 1. 모드 판별
- 키워드 없음 → **작성 모드**: 설명으로 리포트 작성
- `analyze [경로]` → **분석 모드**: 파일을 읽고 잠재 버그 찾기
- `verify [BUG-ID]` → **확인 모드**: 수정이 실제로 됐는지 확인
- `close [BUG-ID]` → **종결 모드**: 확인된 버그에 종결 기록
- 인자가 없으면 버그 설명을 먼저 묻는다.

## 2A. 작성 모드
1. 설명에서 무엇이 깨졌는지, 언제, 어떻게 재현하는지, 기대 동작이 무엇인지 뽑는다.
2. `python Tools/harness/harness.py resolve "<설명>"`으로 관련 시스템 카드를 찾고, 카드의 진입 파일부터 읽어 원인 후보를 좁힌다.
3. 아래 양식으로 초안을 쓴다. 번호는 `Docs/qa/bugs/`의 기존 최대 번호 + 1.

```markdown
# BUG-NNNN: [짧고 구체적인 제목]

- **심각도**: S1 치명(크래시·세이브 손상·진행 불가) / S2 큼(핵심 루프 오동작) / S3 작음(우회 가능) / S4 사소(외형)
- **상태**: Open
- **보고일**: YYYY-MM-DD
- **시스템**: 하루 사이클 / 손님 NPC / 시설 / 미니게임 / 상점 / 저장 / 맵·씬 / UI
- **빈도**: 항상 / 자주(50%+) / 가끔(10~50%) / 드묾
- **회귀**: 예 / 아니오 / 모름 (전에는 됐는지)

## 환경
- 커밋: `git rev-parse --short HEAD` 결과
- 실행: 에디터 Play / Android / iOS
- 씬·상황: (예: EditScene, 3일차, 골드 1200, 음료 자판기 해금 후)

## 재현 단계
전제: [세이브 상태 등. 필요하면 ResetButton으로 초기화 후 시작]
1.
2.
3.

**기대**: 
**실제**: 

## 기술 메모
- 관련 파일: (카드 진입 파일 기준, `경로:줄`)
- 원인 후보: 
- Console 로그: (에러·스택 그대로)
```

## 2B. 분석 모드
대상 파일을 읽고 이 프로젝트에서 자주 터지는 유형부터 본다:
- 이벤트 구독/해제 짝(`GameManager.Instance`가 null일 때 구독 누락, OnDestroy 해제 누락)
- 시설 슬롯 예약·취소 짝(ReserveSlot / CancelReservation / ExitFacility)
- UniTask 취소 토큰 누락, 파괴된 오브젝트 접근
- `NPC_Base.SetActionTimerPaused(true)` 후 해제 누락
- JsonUtility로 저장 안 되는 필드(Dictionary, 프로퍼티)
- 하루 경계(OnDayEnded / OnNextDayStarted)에서 상태 초기화 누락

찾은 것마다 위 양식으로 쓰되, 실제 코드 줄을 근거로 적고 추측은 추측이라고 표시한다.

## 2C. 확인 모드
`Docs/qa/bugs/[BUG-ID].md`의 재현 단계와 원인 코드를 읽는다.
1. 원인 코드 경로가 아직 그대로인지 Grep으로 확인한다.
2. 같은 패턴이 다른 곳에 새로 생겼는지 찾는다.
3. 판정: **수정 확인됨** / **여전히 있음** / **확인 불가**(에디터 Play로 직접 봐야 함 — 무엇을 보면 되는지 단계로 적는다).
정적 확인만으로 "수정 확인됨"이라고 하지 않는다. Play 확인이 필요하면 그렇게 말한다.
상태를 바꾸기 전에 사용자에게 묻는다.

## 2D. 종결 모드
상태가 "수정 확인됨"이 아니면 멈추고 먼저 verify를 하라고 안내한다.
맞으면 파일 끝에 종결 기록(종결일, 무엇을 고쳤는지 한 줄, 커밋, 확인 방법)을 붙이고 상태를 Closed로 바꾼다. 쓰기 전에 묻는다.

## 3. 저장
초안을 보여 주고 "`Docs/qa/bugs/BUG-NNNN.md`에 저장할까요?"라고 묻는다. 승인하면 폴더가 없을 때 만들고 저장한다.
S1·S2면 바로 고칠지도 묻는다.
