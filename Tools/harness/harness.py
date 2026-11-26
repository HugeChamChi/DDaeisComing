"""DDaeisComing AI harness — system cards, live lane board, Unity reference reports.
(GaeGGUL Defence 하네스에서 가져와 이 프로젝트 경로에 맞게 고친 것)

Usage (from the project root; Python 3.9+):
  python Tools/harness/harness.py check [--brief]     verify cards, skills, lane board
  python Tools/harness/harness.py resolve "<text>"    which card / skill / tool card to read
  python Tools/harness/harness.py describe <path>...  what a file is
  python Tools/harness/harness.py lane                show the live lane board
  python Tools/harness/harness.py lane set --task "..." [--hold "a,b"] [--state 작업중] [--lane NAME]
  python Tools/harness/harness.py lane done [--note "..."] [--lane NAME]
  python Tools/harness/harness.py install [--project DIR]   wire Claude Code hooks
  python Tools/harness/harness.py hook <prompt|log|guard|report>   (called by Claude hooks)

Lane name comes from --lane or the GAE_LANE environment variable.
Maintained original: Docs/harness/README.md
"""
import datetime
import fnmatch
import glob
import hashlib
import json
import os
import re
import subprocess
import sys
import time

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
# Cards describe the canonical checkout (where Tools/harness really lives); other worktrees
# reach it through a junction, so card checks always run against that code.
CANON = os.path.abspath(os.path.join(os.path.dirname(os.path.realpath(__file__)), "..", ".."))


def _main_worktree(root):
    """Main checkout of this repository (shared by every git worktree, e.g. DDaeisComing-lab)."""
    try:
        out = subprocess.run(["git", "-C", root, "rev-parse", "--path-format=absolute", "--git-common-dir"],
                             capture_output=True, text=True, encoding="utf-8", timeout=10)
        common = out.stdout.strip()
        if out.returncode == 0 and common:
            return os.path.abspath(os.path.dirname(common))
    except Exception:
        pass
    return root


# The lane board and logs live in the MAIN checkout so every worktree shares one board
# (GaeGGUL used a junction for this; git's common dir does the same job without one).
MAIN = _main_worktree(ROOT)
HARNESS_DIR = os.path.join(CANON, "Docs", "harness")
CARD_DIRS = [os.path.join(HARNESS_DIR, "systems"), os.path.join(HARNESS_DIR, "tools")]
ACTIVE = os.path.join(MAIN, "production", "session-state", "active.md")
LOG_DIR = os.path.join(MAIN, "production", "session-logs", "harness")
UNREGISTERED = os.path.join(LOG_DIR, "unregistered.txt")
LANES_BEGIN, LANES_END = "<!-- LANES:BEGIN -->", "<!-- LANES:END -->"
LANE_HEADER = ["레인", "폴더·브랜치", "상태", "지금 작업", "잡은 파일(다른 레인 수정 금지)", "갱신"]
CODE_ROOTS = ["Assets/Scripts", "Assets/Editor", "Assets/WorkSpace"]
FILE_EXTS = {"cs", "asset", "prefab", "unity", "md", "json", "meta", "ps1", "py", "txt", "log", "mat", "anim",
             "bat", "sh", "dat", "png", "xml", "csv", "xlsx", "yml", "yaml", "shader", "uxml", "uss", "controller"}

UNITY_CMD = re.compile(
    r"(\bunity(?:\.exe)?\s+(?:command|status|pipeline|list|run|test|build|logs|open|job|mcp|shell|doctor)\b"
    r"|Unity\.exe|-batchmode|-executeMethod)",
    re.I)
PATH_IN_TEXT = re.compile(
    r"(?:[A-Za-z]:[\\/]|/[a-z]/|\.{0,2}/)?(?:[\w.\-가-힣]+[\\/])+[\w.\-가-힣]+"
    r"\.(?:cs|ps1|py|cjs|mjs|unity|prefab|asset|json|md|log|txt|uxml|uss|shader)\b")


# ------------------------------------------------------------------ paths

def to_rel(path):
    """Normalise absolute / backslash / Git-Bash paths to a project-relative '/' path."""
    if not path:
        return ""
    p = path.strip().strip("\"'").replace("\\", "/")
    m = re.match(r"^/([a-zA-Z])/(.*)$", p)
    if m:
        p = m.group(1).upper() + ":/" + m.group(2)
    root = ROOT.replace("\\", "/")
    if p.lower().startswith(root.lower() + "/"):
        p = p[len(root) + 1:]
    return p[2:] if p.startswith("./") else p


def _abs(p):
    p = os.path.expanduser(os.path.expandvars(p))
    return p if os.path.isabs(p) or re.match(r"^[a-zA-Z]:[\\/]", p) else os.path.join(ROOT, p)


def _canon(p):
    a = _abs(p)
    return os.path.join(CANON, p) if a == os.path.join(ROOT, p) else a


def _norm_key(key):
    """Registry keys may use %LOCALAPPDATA% / ~ ; compare them as normalised paths."""
    if "%" in key or key.startswith("~") or "$" in key:
        return to_rel(os.path.expanduser(os.path.expandvars(key)))
    return key


def _read(path):
    try:
        with open(path, encoding="utf-8", errors="replace") as f:
            return f.read()
    except OSError:
        return ""


def _git(*args, root=None):
    try:
        out = subprocess.run(["git", "-C", root or ROOT, *args], capture_output=True, text=True,
                             encoding="utf-8", timeout=20)
        return out.stdout if out.returncode == 0 else ""
    except Exception:
        return ""


# ------------------------------------------------------------------ cards

def _front_matter(text):
    m = re.match(r"^---\s*\n(.*?)\n---\s*\n", text, re.S)
    meta = {}
    if m:
        for line in m.group(1).splitlines():
            if ":" in line:
                k, v = line.split(":", 1)
                meta[k.strip()] = v.strip()
    return meta


def load_cards():
    cards = []
    for d in CARD_DIRS:
        for path in sorted(glob.glob(os.path.join(d, "*.md"))):
            text = _read(path)
            meta = _front_matter(text)
            meta["path"] = to_rel(path)
            meta["text"] = text
            meta["keywords_list"] = [k.strip().lower() for k in meta.get("keywords", "").split(",") if k.strip()]
            cards.append(meta)
    return cards


_ROW = re.compile(r"^\|\s*`([^`]+)`\s*\|\s*(.+?)\s*\|\s*$")


def card_rows(card):
    """(path_or_glob, description, section) rows from card tables whose first cell is `code`."""
    rows, section = [], ""
    for line in card["text"].splitlines():
        if line.startswith("## "):
            section = line[3:].strip()
        m = _ROW.match(line)
        if m:
            rows.append((m.group(1).replace("\\|", "|"), m.group(2).strip(), section))
    return rows


def file_entries(cards=None):
    """[(pattern, description, card_path)] — every card's file table rows except implied/symbol tables."""
    out = []
    for c in cards or load_cards():
        for key, desc, section in card_rows(c):
            if section.startswith("암묵 참조"):
                continue
            if "/" in key or "." in os.path.basename(key):
                out.append((_norm_key(key), desc, c["path"]))
    return out


def implied_entries(cards=None):
    out = []
    for c in cards or load_cards():
        for key, desc, section in card_rows(c):
            if section.startswith("암묵 참조"):
                f = re.search(r"`([^`]+)`", desc)
                if f:
                    out.append((key, f.group(1)))
    return out


def lookup(rel, entries):
    for key, desc, card in entries:
        if key == rel:
            return desc, card
    best = None
    for key, desc, card in entries:
        if any(ch in key for ch in "*?[") and fnmatch.fnmatch(rel, key):
            if best is None or len(key) > len(best[0]):
                best = (key, desc, card)
    return (best[1], best[2]) if best else (None, None)


def _head(path, n):
    lines = []
    try:
        with open(path, encoding="utf-8", errors="replace") as f:
            for i, line in enumerate(f):
                if i >= n:
                    break
                lines.append(line.rstrip("\n"))
    except OSError:
        pass
    return lines


def auto_describe(rel):
    path = _abs(rel)
    if os.path.isdir(path):
        return "디렉터리"
    if not os.path.exists(path):
        return "(파일 없음 — 삭제됐거나 경로가 다름)"
    ext = os.path.splitext(rel)[1].lower()
    if ext == ".cs":
        text = "\n".join(_head(path, 200))
        cls = re.search(r"\b(?:class|struct|interface|enum)\s+(\w+)", text)
        # Only the summary written directly above the type declaration describes the file.
        before = text[:cls.start()] if cls else text
        summ = None
        for summ in re.finditer(r"///\s*<summary>\s*(?:///\s*)?(.+?)\s*(?:///\s*)?</summary>", before, re.S):
            pass
        if summ and re.search(r"\n\s*(?!///|\[|public|internal|sealed|abstract|static|partial)\S", before[summ.end():]):
            summ = None
        kind = "에디터 스크립트" if "/Editor/" in rel or "using UnityEditor" in text else "C# 스크립트"
        label = kind + (f" `{cls.group(1)}`" if cls else "")
        if summ:
            return label + " — " + re.sub(r"\s*///\s*", " ", summ.group(1)).strip()[:120]
        return label
    if ext == ".md":
        h = next((l.lstrip("# ").strip() for l in _head(path, 40) if l.startswith("#")), None)
        return "문서" + (f" — {h}" if h else "")
    if ext in (".ps1", ".py", ".sh", ".cjs", ".mjs", ".js"):
        c = next((l.strip().lstrip("#/\"' ").strip() for l in _head(path, 40)
                  if re.match(r"^\s*(#(?!!)|//|\"\"\")", l) and len(l.strip()) > 4), None)
        return "스크립트" + (f" — {c[:120]}" if c else "")
    kinds = {".asset": "Unity 에셋(SO/설정)", ".prefab": "프리팹", ".unity": "씬", ".mat": "머티리얼",
             ".meta": "Unity 메타(GUID)", ".json": "JSON", ".log": "로그"}
    if ext in (".asset", ".prefab", ".unity"):
        nm = next((l.split(":", 1)[1].strip() for l in _head(path, 80)
                   if l.strip().startswith("m_Name:") and l.split(":", 1)[1].strip()), None)
        return kinds[ext] + (f" (m_Name: {nm})" if nm else "")
    return kinds.get(ext, f"{ext or os.path.basename(rel)} 파일")


def describe(rel, entries=None):
    desc, card = lookup(rel, entries if entries is not None else file_entries())
    if desc:
        return desc, card
    return auto_describe(rel), None


# ------------------------------------------------------------------ resolve

def resolve(text):
    """Return list of (kind, path, reason) the agent should read for this request."""
    low = text.lower()
    hits = []
    for c in load_cards():
        matched = [k for k in c["keywords_list"] if k and k in low]
        if matched:
            hits.append(("카드", c["path"], ", ".join(matched[:4])))
    for m in re.finditer(r"(?:^|\s)[/$]([a-z][\w-]+)", text):
        name = m.group(1)
        for base, label in ((".claude/skills", "Claude 스킬"), (".agents/skills", "Codex 스킬")):
            p = f"{base}/{name}/SKILL.md"
            if os.path.exists(_abs(p)):
                hits.append((label, p, "/" + name))
    if UNITY_CMD.search(text) and not any(h[1].endswith("tools/unity.md") for h in hits):
        hits.append(("카드", "Docs/harness/tools/unity.md", "Unity 명령"))
    seen, out = set(), []
    for h in hits:
        if h[1] not in seen:
            seen.add(h[1])
            out.append(h)
    return out


# ------------------------------------------------------------------ checks

_CLASS_CACHE = {}


def _class_file(name):
    if name not in _CLASS_CACHE:
        found = None
        pat = re.compile(r"\b(?:class|struct|interface|enum)\s+" + re.escape(name) + r"\b")
        for base in CODE_ROOTS:
            for p in glob.glob(os.path.join(CANON, base, "**", "*.cs"), recursive=True):
                if pat.search(_read(p)):
                    found = p
                    break
            if found:
                break
        _CLASS_CACHE[name] = found
    return _CLASS_CACHE[name]


def check_cards():
    res = []
    for c in load_cards():
        name = c["path"]
        issues, paths = [], []
        for key, _, section in card_rows(c):
            # Runtime files are generated on each PC; naming tables classify reports,
            # rather than declare files that every teammate must have installed.
            if section.startswith(("암묵 참조", "로컬 생성 파일", "run_script 대상 스크립트")):
                continue
            if "/" not in key:
                continue
            k = _norm_key(key)
            if any(ch in k for ch in "*?["):
                if not glob.glob(_canon(k), recursive=True):
                    issues.append(f"패턴 불일치 {key}")
            elif not os.path.exists(_canon(k)):
                issues.append(f"파일 없음 {key}")
            elif not os.path.isabs(k) and not re.match(r"^[a-zA-Z]:", k):
                paths.append(k)
        # `Class.Member` symbols mentioned anywhere in the card must exist in code.
        for cls, member in set(re.findall(r"`([A-Z]\w+)\.([A-Za-z_]\w*)(?:\([^`]*\))?`", c["text"])):
            if member.lower() in FILE_EXTS:
                continue
            f = _class_file(cls)
            if f is None:
                issues.append(f"클래스 없음 {cls}")
            elif not re.search(r"\b" + re.escape(member) + r"\b", _read(f)):
                issues.append(f"멤버 없음 {cls}.{member}")
        commit = c.get("commit")
        if commit and paths:
            changed = set(_git("diff", "--name-only", commit, "--", *paths, root=CANON).split())
            if changed:
                issues.append(f"확인 커밋 {commit} 이후 변경 {len(changed)}개: "
                              + ", ".join(sorted(changed)[:4]) + (" …" if len(changed) > 4 else ""))
        if issues:
            res += [("DRIFT", f"{name}: {i}") for i in issues]
        else:
            res.append(("OK", f"{name}: 경로·심볼 일치, {c.get('verified', '?')} 이후 변경 없음"))
    return res


def _skills_block():
    text = _read(os.path.join(HARNESS_DIR, "tools", "unity.md"))
    m = re.search(r"```json harness-skills\s*\n(.*?)```", text, re.S)
    return json.loads(m.group(1)) if m else []


def _expand_one(p):
    p = _abs(p)
    if any(ch in p for ch in "*?["):
        hits = sorted(glob.glob(p))
        return hits[0] if hits else None
    return p


def _sha(path):
    try:
        with open(path, "rb") as f:
            return hashlib.sha1(f.read().replace(b"\r\n", b"\n")).hexdigest()
    except OSError:
        return None


def _probe(kind, arg, pattern=None):
    try:
        if kind == "cmd":
            out = subprocess.run(arg, shell=True, capture_output=True, text=True, timeout=15)
            t = (out.stdout or out.stderr).strip()
            return t.splitlines()[0] if t else None
        if kind == "manifest":
            data = json.loads(_read(os.path.join(ROOT, "Packages", "manifest.json")) or "{}")
            return data.get("dependencies", {}).get(arg)
        if kind == "regex":
            p = _expand_one(arg)
            m = re.search(pattern, _read(p), re.M) if p else None
            return m.group(1) if m else None
    except Exception as e:
        return f"probe-error: {e}"
    return None


def check_skills():
    res = []
    for s in _skills_block():
        name, start = s["name"], len(res)
        path = _expand_one(s["path"])
        if not path or not os.path.exists(path):
            res.append(("MISSING", f"{name}: 파일 없음 ({s['path']})"))
            continue
        if s.get("upstream"):
            up = _expand_one(s["upstream"])
            if not up or not os.path.exists(up):
                res.append(("MISSING", f"{name}: 원본 없음 ({s['upstream']})"))
            elif _sha(up) != _sha(path):
                res.append(("DRIFT", f"{name}: 원본과 다름 → {s.get('sync_hint', '원본에서 복사')}"))
        inst = _probe(*s["installed"]) if s.get("installed") else None
        alig = _probe(*s["aligned"]) if s.get("aligned") else None
        if inst and alig and inst != alig:
            res.append(("DRIFT", f"{name}: 설치 {inst} ≠ 기준 {alig} → {s.get('sync_hint', '갱신')}"))
        if s.get("recorded") and inst and s["recorded"] != inst:
            res.append(("DRIFT", f"{name}: 기록 {s['recorded']} ≠ 설치 {inst} → tools/unity.md 갱신"))
        if len(res) == start:
            res.append(("OK", f"{name}: 최신 ({inst or s.get('recorded') or '원본 일치'})"))
    return res


def check_mirrors():
    a, b = os.path.join(ROOT, ".claude", "skills"), os.path.join(ROOT, ".agents", "skills")
    if not (os.path.isdir(a) and os.path.isdir(b)):
        return []
    missing = sorted(set(os.listdir(a)) - set(os.listdir(b)))
    if missing:
        return [("DRIFT", f"Codex 미러 없는 Claude 스킬: {', '.join(missing)}")]
    return [("OK", f"스킬 미러: .claude/skills {len(os.listdir(a))}개 모두 .agents/skills에 있음")]


def check_unregistered():
    if not os.path.exists(UNREGISTERED):
        return []
    entries = file_entries()
    pending = [l.strip() for l in _read(UNREGISTERED).splitlines() if l.strip()]
    pending = [p for p in pending if lookup(p, entries)[0] is None]
    return [("DRIFT", f"카드에 없는 참조 파일 {len(pending)}개 (harness.py unregistered)")] if pending else []


def check_lanes():
    if not os.path.exists(ACTIVE):
        return [("MISSING", "active.md 없음")]
    if LANES_BEGIN not in _read(ACTIVE):
        return [("DRIFT", "active.md에 실시간 작업판 없음 (harness.py lane set 으로 생성)")]
    return []


# ------------------------------------------------------------------ lane board

def _lock(path, timeout=8.0):
    lock = path + ".lock"
    deadline = time.time() + timeout
    while True:
        try:
            fd = os.open(lock, os.O_CREAT | os.O_EXCL | os.O_WRONLY)
            os.write(fd, str(os.getpid()).encode())
            os.close(fd)
            return lock
        except FileExistsError:
            try:
                if time.time() - os.path.getmtime(lock) > 30:  # stale lock
                    os.remove(lock)
                    continue
            except OSError:
                pass
            if time.time() > deadline:
                raise RuntimeError("active.md 잠금 대기 시간 초과 — 다른 레인이 쓰는 중")
            time.sleep(0.2)


def _write_shared(path, text):
    """Atomic replace; Windows refuses it while another agent/editor has the file open,
    so retry briefly and then fall back to an in-place write."""
    tmp = path + ".tmp"
    with open(tmp, "w", encoding="utf-8", newline="\n") as f:
        f.write(text)
    for _ in range(10):
        try:
            os.replace(tmp, path)
            return
        except PermissionError:
            time.sleep(0.3)
    try:
        with open(path, "w", encoding="utf-8", newline="\n") as f:
            f.write(text)
    finally:
        try:
            os.remove(tmp)
        except OSError:
            pass


def _parse_board(text):
    if LANES_BEGIN not in text:
        return None, []
    block = text.split(LANES_BEGIN, 1)[1].split(LANES_END, 1)[0]
    rows = []
    for line in block.splitlines():
        if line.startswith("|") and not re.match(r"^\|\s*-", line):
            cells = [c.strip() for c in line.strip().strip("|").split("|")]
            if cells and cells[0] != LANE_HEADER[0]:
                rows.append((cells + [""] * len(LANE_HEADER))[:len(LANE_HEADER)])
    return block, rows


def _render_board(rows):
    out = [LANES_BEGIN,
           "## 실시간 작업판",
           "레인마다 자기 행만 고친다: `python Tools/harness/harness.py lane set --task \"...\" --hold \"경로,glob\"` / `lane done`."
           " 잡은 파일은 다른 레인이 수정하지 않는다. 상세 기록은 아래 날짜별 섹션.",
           "",
           "| " + " | ".join(LANE_HEADER) + " |",
           "|" + "---|" * len(LANE_HEADER)]
    for r in rows:
        out.append("| " + " | ".join(c.replace("|", "/") for c in r) + " |")
    out.append(LANES_END)
    return "\n".join(out)


def read_lanes():
    return _parse_board(_read(ACTIVE))[1]


def _folder_label():
    branch = _git("rev-parse", "--abbrev-ref", "HEAD").strip() or "?"
    name = "루트" if os.path.normcase(ROOT) == os.path.normcase(MAIN) else "실험실"
    return f"{name} {branch}"


def update_lane(lane, **fields):
    os.makedirs(os.path.dirname(ACTIVE), exist_ok=True)
    lock = _lock(ACTIVE)
    try:
        text = _read(ACTIVE)
        block, rows = _parse_board(text)
        now = datetime.datetime.now().strftime("%m-%d %H:%M")
        row = next((r for r in rows if r[0] == lane), None)
        if row is None:
            row = [lane, "", "대기", "", "", ""]
            rows.append(row)
        row[1] = _folder_label()
        for idx, key in ((2, "state"), (3, "task"), (4, "hold")):
            if fields.get(key) is not None:
                row[idx] = fields[key]
        row[5] = now
        board = _render_board(rows)
        if block is None:
            lines = text.split("\n")
            insert = next((i for i, l in enumerate(lines) if l.startswith("## ")), len(lines))
            text = "\n".join(lines[:insert] + [board, ""] + lines[insert:])
        else:
            text = text.replace(LANES_BEGIN + block + LANES_END, board)
        _write_shared(ACTIVE, text)
        return row
    finally:
        try:
            os.remove(lock)
        except OSError:
            pass


def holders(rel, me=None):
    """Lanes (other than me) currently holding a pattern that matches rel."""
    out = []
    for r in read_lanes():
        if r[0] == me or r[2] in ("대기", "완료", ""):
            continue
        for pat in [p.strip().strip("`") for p in re.split(r"[,;]", r[4]) if p.strip()]:
            if rel == pat or fnmatch.fnmatch(rel, pat) or rel.startswith(pat.rstrip("/") + "/"):
                out.append((r[0], pat, r[3]))
                break
    return out


# ------------------------------------------------------------------ Claude hooks

def _turn_file(sid):
    os.makedirs(LOG_DIR, exist_ok=True)
    return os.path.join(LOG_DIR, "turn-" + (re.sub(r"[^\w-]", "", sid or "x")[:64] or "x") + ".jsonl")


def _append(sid, rec):
    with open(_turn_file(sid), "a", encoding="utf-8") as f:
        f.write(json.dumps(rec, ensure_ascii=False) + "\n")


def hook_prompt(data):
    """UserPromptSubmit: reset the turn log and point the agent at the relevant cards."""
    sid, prompt = data.get("session_id"), data.get("prompt") or ""
    open(_turn_file(sid), "w", encoding="utf-8").close()
    if prompt.strip().startswith("/"):
        _append(sid, {"kind": "trigger", "cmd": prompt.strip().split()[0]})
    hits = resolve(prompt)
    if hits:
        lines = ["[harness] 이 요청과 관련된 카드/스킬 — 코드 전체를 탐색하기 전에 이것부터 읽기:"]
        lines += [f"- {k}: {p} ({why})" for k, p, why in hits]
        print("\n".join(lines))


def hook_log(data):
    sid, tool, ti = data.get("session_id"), data.get("tool_name", ""), data.get("tool_input") or {}
    if tool in ("Read", "Edit", "Write", "NotebookEdit", "MultiEdit"):
        p = ti.get("file_path") or ti.get("notebook_path")
        if p:
            _append(sid, {"kind": "읽음" if tool == "Read" else "수정", "path": to_rel(p)})
    elif tool in ("Bash", "PowerShell"):
        cmd = ti.get("command") or ""
        if UNITY_CMD.search(cmd):
            _append(sid, {"kind": "trigger", "cmd": " ".join(cmd.split())[:200]})
            for m in PATH_IN_TEXT.finditer(cmd):
                _append(sid, {"kind": "명령 인자", "path": to_rel(m.group(0))})
    elif "unity" in tool.lower() and tool.startswith("mcp__"):
        _append(sid, {"kind": "trigger", "cmd": tool})
        for v in ti.values():
            if isinstance(v, str) and PATH_IN_TEXT.fullmatch(v.strip()):
                _append(sid, {"kind": "명령 인자", "path": to_rel(v)})


def hook_guard(data):
    """PreToolUse(Edit/Write): ask before touching a file another lane holds."""
    ti = data.get("tool_input") or {}
    p = ti.get("file_path") or ti.get("notebook_path")
    if not p:
        return
    rel = to_rel(p)
    h = holders(rel, os.environ.get("GAE_LANE"))
    if h:
        lane, pat, task = h[0]
        print(json.dumps({"hookSpecificOutput": {
            "hookEventName": "PreToolUse",
            "permissionDecision": "ask",
            "permissionDecisionReason": f"[작업판] {rel} 은(는) 레인 '{lane}'이(가) 잡고 있음 ({pat}; 작업: {task}). 계속 수정할까요?"}}))


def hook_report(data):
    path = _turn_file(data.get("session_id"))
    if not os.path.exists(path):
        return
    recs = [json.loads(l) for l in _read(path).splitlines() if l.strip()]
    cmds = [r["cmd"] for r in recs if r["kind"] == "trigger"]
    if not cmds:
        return
    cards = load_cards()
    entries = file_entries(cards)
    refs = {}
    for r in recs:
        if r.get("path"):
            refs.setdefault(r["path"], set()).add(r["kind"])
    for trig, f in implied_entries(cards):
        if any(re.search(trig, c, re.I) for c in cmds):
            refs.setdefault(f, set()).add("암묵 참조")
    lines = [f"[참조 파일] 이번 요청의 명령 {len(cmds)}개"]
    lines += [f"  $ {c}" for c in dict.fromkeys(cmds)]
    unreg = []
    for i, (p, kinds) in enumerate(refs.items()):
        if i >= 40:
            lines.append(f"  … 외 {len(refs) - 40}개")
            break
        desc, card = describe(p, entries)
        if card is None:
            unreg.append(p)
        tag = f" ← {os.path.basename(card)}" if card else " [카드 미등록]"
        lines.append(f"  • {p} ({'/'.join(sorted(kinds))}){tag}\n      └ {desc}")
    if not refs:
        lines.append("  (직접 읽거나 넘긴 파일 없음)")
    if unreg:
        lines.append(f"  카드 미등록 {len(unreg)}개 — 반복되면 해당 시스템 카드에 추가")
        os.makedirs(LOG_DIR, exist_ok=True)
        known = set(_read(UNREGISTERED).split())
        with open(UNREGISTERED, "a", encoding="utf-8") as f:
            f.writelines(p + "\n" for p in unreg if p not in known)
    msg = "\n".join(lines)
    day = datetime.date.today().isoformat()
    with open(os.path.join(LOG_DIR, f"{day}.md"), "a", encoding="utf-8") as f:
        f.write(f"## {datetime.datetime.now():%H:%M} {os.environ.get('GAE_LANE', '?')}\n```\n{msg}\n```\n\n")
    open(path, "w", encoding="utf-8").close()
    print(json.dumps({"systemMessage": msg}))


HOOKS = {
    "SessionStart": (None, "check --brief", 20),
    "UserPromptSubmit": (None, "hook prompt", 10),
    "PreToolUse": ("Edit|Write|MultiEdit|NotebookEdit", "hook guard", 5),
    "PostToolUse": ("Read|Edit|Write|MultiEdit|NotebookEdit|Bash|PowerShell|mcp__.*unity.*", "hook log", 5),
    "Stop": (None, "hook report", 10),
}


def install(project):
    path = os.path.join(project, ".claude", "settings.json")
    data = json.loads(_read(path) or "{}")
    hooks = data.setdefault("hooks", {})
    base = 'python "$CLAUDE_PROJECT_DIR/Tools/harness/harness.py" '
    for event, (matcher, args, timeout) in HOOKS.items():
        groups = hooks.setdefault(event, [])
        for g in groups:  # drop older copies of this harness (and its predecessor)
            g["hooks"] = [h for h in g.get("hooks", [])
                          if "Tools/harness/harness.py" not in h.get("command", "")
                          and "unity_ref_tracker.py" not in h.get("command", "")]
        groups[:] = [g for g in groups if g.get("hooks")]
        groups.append({"matcher": matcher or "", "hooks": [{"type": "command", "command": base + args, "timeout": timeout}]})
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(json.dumps(data, indent=2, ensure_ascii=False) + "\n")
    active = os.path.join(_main_worktree(project), "production", "session-state", "active.md")
    os.makedirs(os.path.dirname(active), exist_ok=True)
    try:
        # Never distribute another PC's current holds or overwrite an existing handoff.
        with open(active, "x", encoding="utf-8", newline="\n") as f:
            f.write("# Session handoff\n\n" + _render_board([]) + "\n")
    except FileExistsError:
        pass
    return path


# ------------------------------------------------------------------ CLI

def _opt(argv, name, default=None):
    if name in argv:
        i = argv.index(name)
        return argv[i + 1] if i + 1 < len(argv) else default
    return default


def main(argv):
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    cmd = argv[1] if len(argv) > 1 else "check"

    if cmd == "hook":
        try:
            data = json.loads(sys.stdin.buffer.read().decode("utf-8") or "{}")
            {"prompt": hook_prompt, "log": hook_log, "guard": hook_guard,
             "report": hook_report}.get(argv[2] if len(argv) > 2 else "", lambda d: None)(data)
        except Exception as e:  # hooks must never block the session
            print(f"harness hook 실패: {e}", file=sys.stderr)
        return 0

    if cmd == "check":
        res = check_cards() + check_skills() + check_mirrors() + check_unregistered() + check_lanes()
        drift = [r for r in res if r[0] != "OK"]
        if "--brief" in argv:
            if drift:
                print("=== harness: 확인 필요 ===")
                for lvl, msg in drift:
                    print(f"  [{lvl}] {msg}")
                print("  → Docs/harness/README.md '유지보수' 절차")
            lanes = [r for r in read_lanes() if r[2] not in ("대기", "완료", "")]
            if lanes:
                print("=== 실시간 작업판 (작업중 레인) ===")
                for r in lanes:
                    print(f"  {r[0]} [{r[1]}] {r[3]} | 잡은 파일: {r[4] or '-'} | {r[5]}")
            if os.path.normcase(CANON) != os.path.normcase(ROOT):
                print(f"참고: 카드는 {to_rel(CANON)} 기준이다. 이 폴더의 브랜치({_folder_label()})는 코드가 다를 수 있다.")
            me = os.environ.get("GAE_LANE")
            print(f"이 세션 레인: {me}" if me else "이 세션 레인: 미설정 (GAE_LANE) — 작업판 갱신 시 --lane 지정")
            return 0
        for lvl, msg in res:
            print(f"[{lvl}] {msg}")
        return 1 if drift else 0

    if cmd == "resolve":
        hits = resolve(" ".join(argv[2:]))
        for k, p, why in hits:
            print(f"{k}\t{p}\t{why}")
        if not hits:
            print("일치하는 카드 없음 — Docs/harness/README.md 색인에서 시스템을 고른다")
        return 0

    if cmd == "describe":
        entries = file_entries()
        for p in argv[2:]:
            rel = to_rel(p)
            desc, card = describe(rel, entries)
            print(f"{rel}\t{desc}\t{card or '[카드 미등록]'}")
        return 0

    if cmd == "unregistered":
        entries = file_entries()
        for p in [l.strip() for l in _read(UNREGISTERED).splitlines() if l.strip()]:
            if lookup(p, entries)[0] is None:
                print(f"{p}\t{auto_describe(p)}")
        return 0

    if cmd == "lane":
        sub = argv[2] if len(argv) > 2 and not argv[2].startswith("--") else "show"
        if sub == "show":
            rows = read_lanes()
            print(_render_board(rows) if rows else "작업판 비어 있음")
            return 0
        lane = _opt(argv, "--lane") or os.environ.get("GAE_LANE")
        if not lane:
            print("레인 이름 없음: --lane NAME 또는 GAE_LANE 환경변수", file=sys.stderr)
            return 2
        if sub == "set":
            row = update_lane(lane, state=_opt(argv, "--state", "작업중"), task=_opt(argv, "--task"),
                              hold=_opt(argv, "--hold"))
        elif sub == "done":
            row = update_lane(lane, state="대기", task=_opt(argv, "--note", "") , hold="")
        else:
            print(__doc__)
            return 2
        print(" | ".join(row))
        return 0

    if cmd == "install":
        print("훅 연결:", install(os.path.abspath(_opt(argv, "--project", ROOT))))
        return 0

    print(__doc__)
    return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv))
