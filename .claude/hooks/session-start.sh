#!/bin/bash
# SessionStart hook: 세션 시작 시 브랜치·최근 커밋·워크트리·미커밋 변경·인계 메모를 컨텍스트에 넣는다.
# (GaeGGUL session-start.sh에서 스프린트/마일스톤/버그 집계 등 스튜디오 절차 부분을 뺀 것)
. "$(dirname "$0")/_common.sh"

echo "=== 때가왔다(DDaeisComing) 세션 컨텍스트 ==="

BRANCH=$(git rev-parse --abbrev-ref HEAD 2>/dev/null)
if [ -n "$BRANCH" ]; then
    echo "Branch: $BRANCH  (폴더: $(pwd))"
    echo ""
    echo "Recent commits:"
    git log --oneline -5 2>/dev/null | sed 's/^/  /'
fi

WT=$(git worktree list 2>/dev/null)
if [ "$(echo "$WT" | grep -c .)" -gt 1 ]; then
    echo ""
    echo "Worktrees (다른 폴더에서 다른 에이전트가 작업 중일 수 있음):"
    echo "$WT" | sed 's/^/  /'
fi

DIRTY=$(git status --porcelain 2>/dev/null | grep -c .)
if [ "$DIRTY" -gt 0 ]; then
    echo ""
    echo "Uncommitted: ${DIRTY}개 (내가 만든 변경이 아닐 수 있음 — 건드리기 전에 확인)"
    git status --porcelain 2>/dev/null | head -10 | sed 's/^/  /'
fi

if [ -f "$STATE_FILE" ]; then
    echo ""
    echo "=== 인계 메모: $STATE_FILE ==="
    # 작업판(LANES 블록)은 harness check --brief가 따로 보여 준다.
    sed '/<!-- LANES:BEGIN -->/,/<!-- LANES:END -->/d' "$STATE_FILE" | grep -v '^\s*$' | head -20
    TOTAL=$(wc -l < "$STATE_FILE" 2>/dev/null)
    [ "$TOTAL" -gt 40 ] && echo "  ... (전체 ${TOTAL}줄 — 이어서 작업할 때 파일 전체를 읽기)"
fi

echo "==================================="
exit 0
