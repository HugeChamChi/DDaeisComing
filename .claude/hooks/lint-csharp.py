"""PreToolUse(Write|Edit) hook: C# 코드 품질 린터 (GaeGGUL lint-csharp.sh를 이 프로젝트에 맞게 옮긴 것).

Assets/Scripts/, Assets/WorkSpace/ 아래 런타임 .cs(Editor 폴더 제외)를 쓰기 직전에 검사한다.
- BLOCK (exit 2, Claude가 스스로 고침): Update 계열 메서드 바디 안의 Find*/GetComponent 호출
- ADVISORY (exit 0, stderr 안내): 그 밖의 Find*, Update 안 Debug.Log, 퍼사드 우회, 하드코딩 수치, private 필드 이름
Edit는 new_string 조각만 보므로, 조각 안에 Update 시그니처가 있을 때만 바디 검사가 된다.
"""
import json
import os
import re
import sys

FIND_RE = re.compile(r"\b(FindObjectOfType|FindFirstObjectByType|FindAnyObjectByType|FindObjectsOfType|FindObjectsByType|GameObject\.Find)\b")
GETCOMP_RE = re.compile(r"\bGetComponent(InChildren|InParent|s)?\s*<")
LOG_RE = re.compile(r"\bDebug\.Log(Warning|Error)?\s*\(")
UPDATE_RE = re.compile(r"\bvoid\s+(Update|FixedUpdate|LateUpdate)\s*\(")
# GameManager가 MiniGame / Interaction / Facility 퍼사드를 제공한다.
FACADE = {"MiniGameManager": "GameManager.MiniGame",
          "InteractionManager": "GameManager.Interaction",
          "FacilityManager": "GameManager.Facility"}
SKIP_FILES = re.compile(r"^(MonoSingleton.*|GlobalManagers|GameManager)\.cs$")


def update_body_lines(content):
    """Lines that sit inside Update/FixedUpdate/LateUpdate bodies (brace-depth tracking)."""
    out, depth, entry, inside, started = [], 0, 0, False, False
    for line in content.split("\n"):
        code = line.split("//", 1)[0]
        if not inside and UPDATE_RE.search(code):
            inside, entry, started = True, depth, False
        if inside:
            out.append(code)
            if "{" in code:
                started = True
        depth += code.count("{") - code.count("}")
        if inside and started and depth <= entry:
            inside = False
    return out


def main():
    try:
        data = json.loads(sys.stdin.buffer.read().decode("utf-8") or "{}")
    except Exception:
        return 0
    tool = data.get("tool_name", "")
    ti = data.get("tool_input") or {}
    path = (ti.get("file_path") or "").replace("\\", "/")
    content = ti.get("content", "") if tool == "Write" else ti.get("new_string", "") if tool == "Edit" else ""
    low = path.lower()
    if not content or not low.endswith(".cs"):
        return 0
    if "/assets/scripts/" not in low and "/assets/workspace/" not in low:
        return 0
    if "/editor/" in low or SKIP_FILES.match(os.path.basename(path)):
        return 0

    blocks, warns = [], []
    upd = "\n".join(update_body_lines(content))

    if FIND_RE.search(upd):
        blocks.append("[PERF-1] Update 계열 메서드 안에서 Find* 를 호출합니다.\n"
                      "         → Awake/Start에서 한 번 찾아 private 필드에 캐싱하세요.")
    elif FIND_RE.search(content):
        warns.append("[PERF-1] Find* 감지 — Awake/Start에서만 쓰고 결과를 필드에 캐싱하세요. "
                     "가능하면 [SerializeField] 참조나 GameManager 퍼사드를 쓰세요.")
    if GETCOMP_RE.search(upd):
        blocks.append("[PERF-2] Update 계열 메서드 안에서 GetComponent 를 호출합니다.\n"
                      "         → Awake에서 캐싱 후 필드로 참조하세요.")
    if LOG_RE.search(upd):
        warns.append("[PERF-3] Update 계열 메서드 안에 Debug.Log 가 있습니다 — 매 프레임 로그는 모바일에서 비쌉니다. "
                     "조건부로 한 번만 찍거나 지우세요.")

    bypass = sorted({m for m in re.findall(r"\b(MiniGameManager|InteractionManager|FacilityManager)\.Instance\b", content)})
    if bypass:
        warns.append("[ARCH-1] 퍼사드 우회: " + ", ".join(f"{m}.Instance → {FACADE[m]}" for m in bypass))

    magic = [l.strip() for l in content.split("\n")
             if re.search(r"\b(private|public|protected)\s+(float|int)\s+\w+\s*=\s*[2-9]\d*(\.\d+)?f?\s*;", l)
             and not re.search(r"\b(const|readonly)\b", l)]
    if magic:
        warns.append(f"[DATA-1] 하드코딩된 게임플레이 수치: {magic[0]}\n"
                     "         → 손님·시설·미니게임 수치는 SO(GameConfig, MiniGameData, FacilityData 등)로 빼거나 const로 이름 붙이세요.")

    naming = [l.strip() for l in content.split("\n")
              if re.search(r"\bprivate\s+(float|int|bool|string|Vector[23]|Transform|GameObject|List<[^>]+>|Dictionary<[^>]+>)\s+[a-z]\w*\s*[;=]", l)
              and not re.search(r"\b(const|readonly|static)\b", l)]
    if naming:
        warns.append(f"[NAMING-1] '_' 접두사 없는 private 필드: {naming[0]}  → private 필드는 _camelCase")

    if not blocks and not warns:
        return 0
    name = os.path.basename(path)
    err = sys.stderr
    print(f"=== C# 코드 품질 검사: {name} ===", file=err)
    if warns:
        print("\n[Advisory — 진행 가능, 커밋 전 확인 권장]\n" + "\n".join(warns), file=err)
    if blocks:
        print("\n[BLOCKING — 아래를 고친 뒤 다시 작성하세요]\n" + "\n".join(blocks), file=err)
        return 2
    return 0


if __name__ == "__main__":
    if hasattr(sys.stderr, "reconfigure"):
        sys.stderr.reconfigure(encoding="utf-8")
    sys.exit(main())
