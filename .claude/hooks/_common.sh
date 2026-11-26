#!/bin/bash
# 세션 훅 공용: 프로젝트 루트로 이동하고, 작업판이 있는 메인 체크아웃 경로를 MAIN_ROOT에 둔다.
# 워크트리(E:/DDaeisComing-lab)에서 실행해도 작업판·로그는 메인 폴더 하나를 같이 쓴다.
cd "${CLAUDE_PROJECT_DIR:-.}" 2>/dev/null || exit 0
COMMON_DIR=$(git rev-parse --path-format=absolute --git-common-dir 2>/dev/null)
if [ -n "$COMMON_DIR" ]; then
    MAIN_ROOT=$(cd "$COMMON_DIR/.." 2>/dev/null && pwd)
fi
[ -z "$MAIN_ROOT" ] && MAIN_ROOT=$(pwd)
STATE_FILE="$MAIN_ROOT/production/session-state/active.md"
SESSION_LOG_DIR="$MAIN_ROOT/production/session-logs"
