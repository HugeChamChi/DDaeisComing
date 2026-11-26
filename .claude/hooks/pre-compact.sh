#!/bin/bash
# PreCompact hook: 컨텍스트 요약 직전에 인계 메모와 변경 파일 목록을 출력해 요약에 살아남게 한다.
. "$(dirname "$0")/_common.sh"

echo "=== SESSION STATE BEFORE COMPACTION ($(date '+%Y-%m-%d %H:%M')) ==="

if [ -f "$STATE_FILE" ]; then
    echo ""
    echo "## 인계 메모 ($STATE_FILE)"
    LINES=$(wc -l < "$STATE_FILE" 2>/dev/null | tr -d ' ')
    head -n 100 "$STATE_FILE"
    [ "$LINES" -gt 100 ] 2>/dev/null && echo "... (총 ${LINES}줄 중 100줄)"
else
    echo ""
    echo "## 인계 메모 없음 — 긴 작업이면 $STATE_FILE 에 진행 상황을 남겨 두면 복구가 쉽다"
fi

echo ""
echo "## 변경 파일 (git working tree, $(pwd))"
CHANGED=$(git diff --name-only 2>/dev/null)
STAGED=$(git diff --staged --name-only 2>/dev/null)
UNTRACKED=$(git ls-files --others --exclude-standard 2>/dev/null | head -30)
[ -n "$CHANGED" ] && { echo "Unstaged:"; echo "$CHANGED" | sed 's/^/  - /'; }
[ -n "$STAGED" ] && { echo "Staged:"; echo "$STAGED" | sed 's/^/  - /'; }
[ -n "$UNTRACKED" ] && { echo "Untracked:"; echo "$UNTRACKED" | sed 's/^/  - /'; }
[ -z "$CHANGED$STAGED$UNTRACKED" ] && echo "  (미커밋 변경 없음)"

mkdir -p "$SESSION_LOG_DIR" 2>/dev/null
echo "Context compaction at $(date) in $(pwd) lane=${GAE_LANE:-?}" >> "$SESSION_LOG_DIR/compaction-log.txt" 2>/dev/null

echo ""
echo "## 복구 방법"
echo "요약 후 인계 메모를 읽고, 위 목록 중 작업하던 파일을 다시 연다."
echo "=== END SESSION STATE ==="
exit 0
