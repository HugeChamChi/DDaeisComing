#!/bin/bash
# Claude Code PreToolUse hook: git push 직전 경고 (GaeGGUL에서 가져옴)
# main/master/develop에 바로 push하면 알린다. 막지는 않는다(exit 0).
cd "${CLAUDE_PROJECT_DIR:-.}" 2>/dev/null || exit 0

INPUT=$(cat)
PY=""
for c in python python3 py; do command -v "$c" >/dev/null 2>&1 && { PY="$c"; break; }; done
if [ -n "$PY" ]; then
    COMMAND=$(printf '%s' "$INPUT" | "$PY" -c "import sys,json; print((json.load(sys.stdin).get('tool_input') or {}).get('command',''))" 2>/dev/null)
else
    COMMAND=$(printf '%s' "$INPUT" | grep -oE '"command"[[:space:]]*:[[:space:]]*"[^"]*"' | sed 's/"command"[[:space:]]*:[[:space:]]*"//;s/"$//')
fi

echo "$COMMAND" | grep -qE '(^|[;&|][[:space:]]*)git[[:space:]]+push' || exit 0

CURRENT_BRANCH=$(git rev-parse --abbrev-ref HEAD 2>/dev/null)
for branch in main master develop; do
    if [ "$CURRENT_BRANCH" = "$branch" ] || echo "$COMMAND" | grep -qE "[[:space:]]${branch}([[:space:]]|$)"; then
        echo "보호 브랜치 '$branch'에 push합니다. 에디터 컴파일 오류가 없고 EditScene Play 확인을 했는지 점검하세요." >&2
        echo "다른 워크트리(DDaeisComing-lab) 작업과 충돌하지 않는지도 확인하세요." >&2
        break
    fi
done
exit 0
