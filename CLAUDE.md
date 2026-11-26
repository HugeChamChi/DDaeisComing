# 때가왔다(DDaeisComing) — Claude 진입점

## 공용 규칙
@AGENTS.md

## 하네스 색인
@Docs/harness/README.md

## Claude 전용
- 훅이 자동으로 하는 일: 세션 시작 시 카드 점검·작업판 표시, 요청마다 관련 카드 안내, 다른 레인이 잡은 파일 수정 전 확인 창,
  C# 린트(`.claude/hooks/lint-csharp.py`), 커밋 전 검사(`.claude/hooks/validate-commit.py`), 알림, 컨텍스트 요약 전후 인계 메모 출력.
- 프로젝트 스킬: `/bug-report`, `/playtest-report`, `/tech-debt`, `/perf-profile`, `/quick-design`, `/prototype`, `/brainstorm`.
- 역할 에이전트(`.claude/agents/`): unity-specialist, unity-ui-specialist, unity-shader-specialist, unity-addressables-specialist, unity-dots-specialist,
  gameplay-programmer, game-designer, economy-designer, qa-lead, qa-tester, performance-analyst, prototyper. 사용자가 요청할 때 쓴다.
- 긴 작업은 `production/session-state/active.md`(메인 폴더, git 제외)의 날짜 섹션에 진행 상황을 남긴다. 컨텍스트 요약 후 이 파일로 복구한다.
