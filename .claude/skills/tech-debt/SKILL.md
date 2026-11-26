---
name: tech-debt
description: "기술 부채를 찾고(scan), 직접 추가하고(add), 우선순위를 다시 매기고(prioritize), 현황을 요약(report)한다. 부채 목록은 Docs/tech-debt-register.md. '기술 부채', '리팩터링 후보', '코드 정리할 곳', 'tech debt' 요청에 사용."
argument-hint: "scan | add | prioritize | report"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write
---

GaeGGUL Defence의 tech-debt 스킬을 옮긴 것이다(스프린트 관련 부분은 뺐다).
부채 목록 파일: `Docs/tech-debt-register.md`.

인자가 없으면 사용법(scan/add/prioritize/report)을 보여 주고 멈춘다.

## scan
`Assets/Scripts/`, `Assets/Editor/`, `Assets/WorkSpace/`를 대상으로 찾는다(서드파티 `Assets/Plugins/`, `Assets/SpriteFusion/`, `Assets/TextMesh Pro/`, `Assets/HierarchyDecorator/`는 제외).

일반 지표:
- `TODO` / `FIXME` / `HACK` 주석 (개수와 내용별 분류)
- 500줄 넘는 파일, 50줄 넘는 메서드
- 비슷한 코드가 여러 파일에 반복되는 곳

이 프로젝트에서 특히 볼 것:
- **실험/테스트 런처 잔재**: `TilemapTestRunner`, `RuntimeMapEditor`, `TilemapGridMap`, `NPCBrain`처럼 씬에서 안 쓰는 코드(씬 YAML에서 스크립트 GUID 검색으로 사용 여부 확인)
- **하드코딩된 임시값**: 예) `ScrubAreaFacility`의 일차 1 고정
- **퍼사드 우회**: `GameManager.MiniGame` 등이 있는데 `FindObjectOfType`/`.Instance`로 직접 찾는 곳
- **런타임 Find 호출**: `FindObjectOfType`, `FindFirstObjectByType`이 Awake/Start 밖에 있는 곳
- **이벤트 구독 해제 누락**: `+=`는 있는데 대응하는 `-=`가 없는 곳
- **다른 프로젝트에서 넘어온 이름**: 메뉴 경로·네임스페이스에 GaeGGUL 등 남은 것
- **SO로 빠져야 할 수치**: 게임플레이 수치가 코드 필드 초기값으로 박힌 곳

분류: 구조 / 코드 품질 / 테스트 / 문서 / 의존성 / 성능.
결과를 보여 주고 "`Docs/tech-debt-register.md`에 추가할까요?"라고 묻는다. 기존 항목은 덮어쓰지 않고 새 항목만 덧붙인다.

## add
설명, 분류, 관련 파일, 예상 수정 규모(S/M/L/XL), 그대로 두면 생기는 문제, **왜 지금 안 고치는지**를 물어 항목을 만든다. 보여 주고 추가할지 묻는다.

## prioritize
목록을 읽고 `(방치 시 영향 × 마주치는 빈도) / 수정 규모`로 점수를 매겨 다시 정렬한다. 다음에 손댈 3개를 추천한다. 다시 쓸지 묻는다.

## report
읽기 전용. 분류별 개수, 총 규모, 지난번 대비 추가·해결 수, 30일 넘게 방치된 항목을 출력한다.

## 목록 형식
```markdown
## 기술 부채 목록
갱신: YYYY-MM-DD | 항목 N개

| ID | 분류 | 내용 | 파일 | 규모 | 영향 | 점수 | 추가일 | 왜 미뤘나 |
|----|------|------|------|------|------|------|--------|-----------|
| TD-001 | 코드 품질 | ... | `Assets/Scripts/...` | S | 중 | 6 | 2026-11-26 | 미니게임 연동 우선 |
```

## 원칙
- 부채는 의식적인 선택이다. 각 항목에 왜 받아들였는지 적는다.
- 오래된 항목은 고치거나, 그대로 두기로 한 이유를 적는다.
- 스캔 결과는 후보일 뿐이다. 실제로 쓰이는지(씬·프리팹 참조) 확인하기 전에 "삭제 대상"이라고 단정하지 않는다.
