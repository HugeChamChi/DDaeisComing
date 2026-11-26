"""PreToolUse(Bash|PowerShell) hook: git commit 직전 검사 (GaeGGUL validate-commit.sh를 Unity 프로젝트용으로 옮긴 것).

BLOCK (exit 2): Assets/ 아래 잘못된 JSON, 100MB 이상 파일
ADVISORY (exit 0): .meta 짝 불일치, 20MB 이상 파일, 키스토어·비밀 파일, 커밋 메시지 태그, 주인 없는 TODO
"""
import json
import os
import re
import subprocess
import sys

COMMIT_RE = re.compile(r"(^|[;&|]\s*)git(\s+-C\s+(\"[^\"]+\"|'[^']+'|\S+))?\s+commit\b")
TAGS = ("Feat", "Fix", "Chore", "Refactor", "Docs", "Test", "Style", "Perf", "Build", "Feature")
ROOT = os.environ.get("CLAUDE_PROJECT_DIR") or os.getcwd()


def git(*args):
    try:
        r = subprocess.run(["git", "-C", ROOT, *args], capture_output=True, text=True, encoding="utf-8", timeout=15)
        return r.stdout if r.returncode == 0 else ""
    except Exception:
        return ""


def tracked(path):
    return bool(git("ls-files", "--", path).strip())


def main():
    try:
        data = json.loads(sys.stdin.buffer.read().decode("utf-8") or "{}")
    except Exception:
        return 0
    cmd = (data.get("tool_input") or {}).get("command") or ""
    if not COMMIT_RE.search(cmd):
        return 0

    staged = []  # (status, path)
    for line in git("diff", "--cached", "--name-status", "--no-renames").splitlines():
        parts = line.split("\t")
        if len(parts) >= 2:
            staged.append((parts[0][0], parts[-1]))
    if not staged:
        return 0
    paths = {p for _, p in staged}
    blocks, warns = [], []

    for st, p in staged:
        full = os.path.join(ROOT, p)
        # JSON validity
        if st != "D" and p.startswith("Assets/") and p.endswith(".json") and os.path.isfile(full):
            try:
                with open(full, encoding="utf-8-sig") as f:
                    json.load(f)
            except Exception as e:
                blocks.append(f"잘못된 JSON: {p} ({e})")
        # size
        if st != "D" and os.path.isfile(full):
            size = os.path.getsize(full)
            if size >= 100 * 1024 * 1024:
                blocks.append(f"100MB 이상 파일: {p} ({size // (1024 * 1024)}MB) — GitHub가 거부합니다")
            elif size >= 20 * 1024 * 1024:
                warns.append(f"큰 파일: {p} ({size // (1024 * 1024)}MB) — 정말 커밋할 파일인지 확인")
        # secrets / keystores
        if re.search(r"(^|/)\.env|\.keystore$|\.jks$|\.p12$|\.mobileprovision$|(^|/)KeyStore/", p):
            warns.append(f"비밀/서명 파일로 보임: {p}")
        # .meta pairing (Unity GUID)
        if p.startswith("Assets/"):
            if p.endswith(".meta"):
                asset = p[:-5]
                if st == "A" and asset not in paths and not tracked(asset) and not os.path.exists(os.path.join(ROOT, asset)):
                    warns.append(f".meta만 추가됨(원본 없음): {p}")
                if st == "D" and asset not in paths and os.path.exists(os.path.join(ROOT, asset)):
                    warns.append(f".meta만 삭제됨(원본은 남음, GUID가 바뀝니다): {p}")
            else:
                meta = p + ".meta"
                if st == "A" and meta not in paths and not tracked(meta):
                    warns.append(f".meta 없이 추가됨: {p} — {meta}도 함께 커밋")
                if st == "D" and meta not in paths and tracked(meta):
                    warns.append(f"원본만 삭제됨: {p} — {meta}도 함께 삭제")
        # TODO without owner in newly added lines
        if st in "AM" and p.endswith(".cs"):
            added = [l[1:] for l in git("diff", "--cached", "-U0", "--", p).splitlines()
                     if l.startswith("+") and not l.startswith("+++")]
            if any(re.search(r"\b(TODO|FIXME|HACK)\b(?!\()", l) for l in added):
                warns.append(f"주인 없는 TODO/FIXME: {p} — TODO(이름) 형식 권장")

    m = re.search(r"-m\s+(\"([^\"]*)\"|'([^']*)')", cmd)
    msg = (m.group(2) or m.group(3) or "") if m else ""
    if m and not re.match(r"^\[(%s)\]" % "|".join(TAGS), msg.strip()) and not msg.startswith("Merge"):
        warns.append("커밋 메시지가 '[Feat] / [Fix] / [Chore] ...' 태그로 시작하지 않습니다 (이 저장소 관례)")

    err = sys.stderr
    if warns:
        print("=== 커밋 검사 경고 ===\n- " + "\n- ".join(warns[:30]), file=err)
    if blocks:
        print("=== 커밋 차단 ===\n- " + "\n- ".join(blocks), file=err)
        return 2
    return 0


if __name__ == "__main__":
    if hasattr(sys.stderr, "reconfigure"):
        sys.stderr.reconfigure(encoding="utf-8")
    sys.exit(main())
