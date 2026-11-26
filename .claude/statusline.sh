#!/usr/bin/env bash
# Status line: ctx% | 모델 | 브랜치(폴더) | 레인: 지금 작업
# GaeGGUL statusline.sh에서 스튜디오 단계 표시를 빼고, 작업판의 내 레인 작업을 보여 주게 바꿨다.
input=$(cat)

PY=""
for c in python python3 py; do command -v "$c" >/dev/null 2>&1 && { PY="$c"; break; }; done
[ -z "$PY" ] && { printf "%s" "status: python 없음"; exit 0; }

printf '%s' "$input" | GAE_LANE="$GAE_LANE" "$PY" -c '
import json, os, re, subprocess, sys
try:
    d = json.loads(sys.stdin.read() or "{}")
except Exception:
    d = {}
model = (d.get("model") or {}).get("display_name") or "?"
pct = (d.get("context_window") or {}).get("used_percentage")
cwd = ((d.get("workspace") or {}).get("current_dir") or d.get("cwd") or ".").replace("\\", "/")
def git(*a):
    try:
        r = subprocess.run(["git", "-C", cwd, *a], capture_output=True, text=True, encoding="utf-8", timeout=3)
        return r.stdout.strip() if r.returncode == 0 else ""
    except Exception:
        return ""
branch = git("rev-parse", "--abbrev-ref", "HEAD") or "-"
top = git("rev-parse", "--show-toplevel")
common = git("rev-parse", "--path-format=absolute", "--git-common-dir")
main = os.path.dirname(common) if common else top
folder = "lab" if top and main and os.path.normcase(top) != os.path.normcase(main) else "main"
parts = [f"ctx {pct}%" if pct is not None else "ctx --", model, f"{branch} ({folder})"]
lane = os.environ.get("GAE_LANE")
if lane:
    seg = lane
    try:
        text = open(os.path.join(main, "production", "session-state", "active.md"), encoding="utf-8").read()
        for line in text.splitlines():
            cells = [c.strip() for c in line.strip().strip("|").split("|")]
            if len(cells) >= 4 and cells[0] == lane and cells[2] not in ("", "대기", "완료"):
                seg = f"{lane}: {cells[3][:40]}"
                break
    except Exception:
        pass
    parts.append(seg)
sys.stdout.write(" | ".join(parts))
'
