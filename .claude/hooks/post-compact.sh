#!/bin/bash
# PostCompact hook: 요약 직후 인계 메모를 다시 읽으라고 알린다.
. "$(dirname "$0")/_common.sh"

echo "=== Context Restored After Compaction ==="
if [ -f "$STATE_FILE" ]; then
    echo "인계 메모: $STATE_FILE ($(wc -l < "$STATE_FILE" 2>/dev/null)줄) — 지금 읽어서 작업 맥락을 복구할 것."
else
    echo "인계 메모 없음. 진행 중이던 작업이 있으면 $SESSION_LOG_DIR/session-log.md 를 확인."
fi
echo "========================================="
exit 0
