#!/bin/bash
# Stop hook: 응답이 끝날 때 새 커밋이 생겼으면 세션 로그에 한 줄씩 남긴다.
# GaeGGUL 원본은 응답마다 active.md 전체를 로그에 복사해 파일이 빨리 커졌다.
# 여기서는 HEAD가 바뀐 경우에만 기록한다(레인·폴더별 마지막 HEAD를 기억).
. "$(dirname "$0")/_common.sh"

mkdir -p "$SESSION_LOG_DIR" 2>/dev/null
HEAD=$(git rev-parse --short HEAD 2>/dev/null) || exit 0
KEY=$(printf '%s' "${GAE_LANE:-nolane}_$(pwd)" | tr -c 'A-Za-z0-9_' '_')
MARK="$SESSION_LOG_DIR/.last-head-$KEY"
LAST=$(cat "$MARK" 2>/dev/null)
[ "$HEAD" = "$LAST" ] && exit 0

{
    echo "## $(date '+%Y-%m-%d %H:%M') ${GAE_LANE:-?} @ $(git rev-parse --abbrev-ref HEAD 2>/dev/null) ($(pwd))"
    if [ -n "$LAST" ]; then
        git log --oneline "$LAST..$HEAD" 2>/dev/null | head -20
    else
        git log --oneline -3 2>/dev/null
    fi
    echo ""
} >> "$SESSION_LOG_DIR/session-log.md" 2>/dev/null
echo "$HEAD" > "$MARK" 2>/dev/null
exit 0
