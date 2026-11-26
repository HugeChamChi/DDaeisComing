---
name: perf-profile
description: "성능 병목 후보를 코드에서 정적으로 찾고, 예산 대비 상태와 우선순위가 붙은 최적화 제안을 낸다. 실제 확인은 Unity Profiler로 하도록 측정 방법도 같이 준다. '성능', '프레임 드랍', '렉', '최적화', 'perf profile' 요청에 사용."
argument-hint: "[시스템 이름 | full]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Bash
---

GaeGGUL Defence의 perf-profile 스킬을 옮긴 것이다. 읽기 전용이며 파일을 고치지 않는다.

## 1. 범위
- 시스템 이름(예: `npc`, `minigame`) → 해당 카드(`Docs/harness/systems/*.md`)의 진입 파일 위주
- `full` → `Assets/Scripts/` 전체

## 2. 예산 (모바일 세로, 따로 정한 값이 없으면 이것을 쓴다)
| 항목 | 예산 |
|---|---|
| 프레임 | 60fps = 16.67ms (최소 30fps = 33.3ms) |
| 프레임당 GC 할당 | 0B 목표, 1KB 미만 |
| 드로우콜/배치 | 100 미만 (2D URP) |
| 동시 손님 수 | `DailySpawnDataSO`의 최대 일차 손님 수 기준으로 계산 |

## 3. 코드 분석
**CPU (프레임마다 도는 곳)**
- 모든 `Update`/`LateUpdate`/`FixedUpdate`와 매 프레임 UniTask 루프(`UniTask.Yield` 루프, 예: `NPC_Base.PerformActionRoutineAsync`, `NPCInteractionController.StartInteractionAsync`) 목록과 추정 비용
- 손님 수에 비례하는 비용: 손님 × 시설 탐색, 경로 탐색(`PathfindingService.FindPath`) 호출 빈도, 실패 시 재탐색 루프
- `DynamicDepthSorter`처럼 오브젝트마다 매 프레임 정렬값을 쓰는 곳
- Update 안의 `Find*`, `GetComponent`, LINQ, 문자열 조합, `Debug.Log`(이 프로젝트는 로그가 많다 — 문자열 보간 비용도 포함)

**메모리·GC**
- 프레임마다 `new List<>` / 클로저 / 박싱
- 오브젝트 풀 사용 여부(손님은 풀 사용, 말풍선 풀, 그 밖에 Instantiate/Destroy 반복하는 곳)
- 이벤트 구독 해제 누락으로 남는 참조

**렌더링 (2D URP)**
- 겹치는 반투명 스프라이트(탕 물, 거품, 말풍선) 오버드로
- 스프라이트 아틀라스 미사용으로 배치가 깨지는 곳
- 월드 공간 UI·캔버스 재빌드(매 프레임 바뀌는 Slider/Text)

**I/O**
- 저장: 1분 자동 저장 + 이벤트 저장의 직렬화·암호화 비용
- `Resources.Load` 동기 로드 위치

## 4. 리포트
```markdown
## 성능 분석: [시스템 | 전체]  (YYYY-MM-DD, 정적 분석)

### 예산 대비
| 항목 | 예산 | 추정 | 상태(OK/주의/초과/측정 필요) |

### 병목 후보
| # | 위치(`경로:줄`) | 문제 | 추정 영향 | 수정 규모(S/M/L) |

### 추천 (우선순위 순)
1. **[제목]** — 위치, 예상 이득, 위험도, 방법

### 바로 할 수 있는 것 (1시간 이내)
-

### Profiler로 확인할 것
- 무엇을: (예: 손님 30명일 때 NPC_Base 루프의 GC Alloc)
- 어떻게: Window > Analysis > Profiler, Deep Profile 끄고 CPU/GC Alloc 열, 기기 연결 시 Development Build + Autoconnect Profiler
```

요약으로 상위 3개 병목, 예산 대비 여유, 다음 행동을 말한다.
수정 규모가 M 이상인 항목은 지금 고칠지, 나중으로 미룰지(→ `/tech-debt add`) 사용자에게 묻는다.

## 원칙
- 측정 없이 단정하지 않는다. 정적 분석은 후보를 찾는 것이고, 확정은 Profiler다.
- "빠르게 하자"가 아니라 예상 이득을 숫자나 근거로 쓴다.
- 에디터가 아니라 실제 모바일 기기 기준으로 판단한다.
