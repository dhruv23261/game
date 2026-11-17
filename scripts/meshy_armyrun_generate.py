#!/usr/bin/env python3
"""
Generate the Level 1 "Army Run" characters with Meshy AI and drop them straight into Unity.

    cd ~/Downloads/gameproject1
    python3 scripts/meshy_armyrun_generate.py            # both characters
    python3 scripts/meshy_armyrun_generate.py enemy      # only the red soldier
    python3 scripts/meshy_armyrun_generate.py boss       # only the Tyrant boss
    python3 scripts/meshy_armyrun_generate.py mob brute giant   # Mob Control horde: red blob, yellow brute, blue giant

Pipeline per character (each step waits for the previous one):
    1. text-to-3d  PREVIEW   (shape)                       ~1 min
    2. text-to-3d  REFINE    (texture)                     ~2 min
    3. rigging               (skeleton + walk/run anims)   ~1 min
    4. download  ->  unity/Assets/Resources/ArmyRun/<Name>.fbx  (+ <Name>_BaseColor.png)
       and a copy of everything in meshy_output/<date>_<name>/

Unity picks the files up automatically (ArmyRunModels.cs): it scales them to the right height,
loops the run/walk animation and uses them instead of the recoloured fallback soldier.

API key: env var MESHY_API_KEY, otherwise read from .agents/mcp_config.json.
Uses only the Python standard library.
"""
import json
import os
import sys
import time
import urllib.error
import urllib.request
from datetime import datetime

BASE = "https://api.meshy.ai"
PROJECT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
UNITY_OUT = os.path.join(PROJECT, "unity", "Assets", "Resources", "ArmyRun")
ARCHIVE = os.path.join(PROJECT, "meshy_output")

CHARACTERS = {
    "enemy": {
        "name": "EnemySoldier",
        "prompt": ("chibi cartoon soldier of an evil army, bright red uniform, red combat helmet, black boots, "
                   "holding an assault rifle, big head and small body, cute stylized mobile strategy game character, "
                   "full body, A-pose, clean topology, game asset"),
        "texture_prompt": "bright red military uniform, red helmet, black boots, dark grey rifle, warm skin, clean cartoon colors",
        "polycount": 6000,
        "height_m": 1.2,
        "anim": "running",
    },
    "boss": {
        "name": "BossTyrant",
        "prompt": ("huge muscular brute tyrant boss, red sleeveless shirt, blond spiky hair, angry face, thick arms, "
                   "black pants and boots, holding a heavy machine gun, stylized cartoon mobile game character, "
                   "chunky proportions, full body, A-pose, clean topology, game asset"),
        "texture_prompt": "red shirt, blond hair, tanned skin, black pants, dark metal gun, bold cartoon colors",
        "polycount": 15000,
        "height_m": 3.5,
        "anim": "walking",
    },
    # ── Mob Control horde pieces (Level 1 reference video) ──
    "mob": {
        "name": "MobRed",
        "prompt": ("tiny cute round red jelly blob creature, mob control style crowd minion, simple chunky body, "
                   "two small stubby legs, two tiny arms, big white eyes with black pupils, angry eyebrows, "
                   "glossy bright red, very simple shape, low poly mobile game asset, full body, standing"),
        "texture_prompt": "glossy bright red body, white eyes with black pupils, simple clean cartoon colors",
        "polycount": 600,
        "height_m": 0.8,
        "anim": None,          # static: Level 1 draws hundreds of them with GPU instancing
    },
    "brute": {
        "name": "BruteYellow",
        "prompt": ("giant muscular yellow brute monster, mob control style boss, huge shoulders and fists, tiny head, "
                   "red war paint stripes, red shorts, angry face, stylized chunky cartoon mobile game character, "
                   "full body, A-pose, clean topology, game asset"),
        "texture_prompt": "bright yellow skin, red stripes and red shorts, bold glossy cartoon colors",
        "polycount": 12000,
        "height_m": 3.0,
        "anim": "walking",
    },
    "giant": {
        "name": "AllyGiant",
        "prompt": ("giant muscular blue hero brute, mob control style ally champion, big arms and fists, "
                   "white spiky hair, grey shorts, determined face, stylized chunky cartoon mobile game character, "
                   "full body, A-pose, clean topology, game asset"),
        "texture_prompt": "bright blue skin, white hair, grey shorts, bold glossy cartoon colors",
        "polycount": 12000,
        "height_m": 3.0,
        "anim": "walking",
    },
}


# ───────────────────────────────────────────── helpers ──
def api_key():
    k = os.environ.get("MESHY_API_KEY")
    if k:
        return k
    cfg = os.path.join(PROJECT, ".agents", "mcp_config.json")
    with open(cfg) as f:
        return json.load(f)["mcpServers"]["meshy"]["env"]["MESHY_API_KEY"]


KEY = None


def call(method, path, body=None):
    req = urllib.request.Request(
        BASE + path,
        data=json.dumps(body).encode() if body is not None else None,
        headers={"Authorization": f"Bearer {KEY}", "Content-Type": "application/json"},
        method=method,
    )
    try:
        with urllib.request.urlopen(req, timeout=60) as r:
            return json.loads(r.read().decode())
    except urllib.error.HTTPError as e:
        msg = e.read().decode(errors="replace")
        raise RuntimeError(f"{method} {path} -> HTTP {e.code}: {msg}") from None


def create(path, body, optional_keys=()):
    """POST; if Meshy rejects an optional field (older API), retry without it."""
    try:
        return call("POST", path, body)["result"]
    except RuntimeError as e:
        if "HTTP 400" in str(e) and optional_keys:
            print(f"    ! {e}\n    retrying without {list(optional_keys)}")
            return call("POST", path, {k: v for k, v in body.items() if k not in optional_keys})["result"]
        raise


def wait(path, task_id, label, timeout=1200):
    t0 = time.time()
    last = None
    while time.time() - t0 < timeout:
        res = call("GET", f"{path}/{task_id}")
        st, pr = res.get("status"), res.get("progress", 0)
        if (st, pr) != last:
            print(f"    {label}: {st} {pr}%")
            last = (st, pr)
        if st == "SUCCEEDED":
            return res
        if st in ("FAILED", "EXPIRED", "CANCELED"):
            raise RuntimeError(f"{label} failed: {res.get('task_error')}")
        time.sleep(6)
    raise TimeoutError(f"{label} timed out")


def download(url, dest):
    os.makedirs(os.path.dirname(dest), exist_ok=True)
    urllib.request.urlretrieve(url, dest)
    print(f"    saved {os.path.relpath(dest, PROJECT)} ({os.path.getsize(dest) // 1024} KB)")


def find_urls(obj, suffix):
    """All string values in a nested dict whose key ends with suffix (e.g. '_fbx_url')."""
    out = {}
    if isinstance(obj, dict):
        for k, v in obj.items():
            if isinstance(v, str) and k.endswith(suffix):
                out[k] = v
            else:
                out.update(find_urls(v, suffix))
    return out


# ───────────────────────────────────────────── pipeline ──
def generate(spec):
    name = spec["name"]
    stamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    arch = os.path.join(ARCHIVE, f"{stamp}_armyrun_{name}")
    os.makedirs(arch, exist_ok=True)
    print(f"\n=== {name} ===")

    # 1. preview
    pid = create("/openapi/v2/text-to-3d", {
        "mode": "preview",
        "prompt": spec["prompt"],
        "art_style": "realistic",
        "should_remesh": True,
        "topology": "triangle",
        "target_polycount": spec["polycount"],
        "symmetry_mode": "on",
        "pose_mode": "a-pose",
    }, optional_keys=("pose_mode", "symmetry_mode"))
    wait("/openapi/v2/text-to-3d", pid, "preview")

    # 2. refine (texture)
    rid = create("/openapi/v2/text-to-3d", {
        "mode": "refine",
        "preview_task_id": pid,
        "enable_pbr": False,
        "texture_prompt": spec["texture_prompt"],
    }, optional_keys=("texture_prompt",))
    refined = wait("/openapi/v2/text-to-3d", rid, "refine")
    json.dump(refined, open(os.path.join(arch, "refine.json"), "w"), indent=2)

    tex_url = None
    for t in refined.get("texture_urls") or []:
        tex_url = t.get("base_color") or tex_url
    if refined.get("thumbnail_url"):
        download(refined["thumbnail_url"], os.path.join(arch, "thumbnail.png"))

    # 3. rigging (+ basic walk / run animations) — skipped for static props
    fbx_url = None
    if spec.get("anim") is None:
        print("    static model: no rigging")
    else:
      try:
        gid = create("/openapi/v1/rigging", {"input_task_id": rid, "height_meters": spec["height_m"]})
        rig = wait("/openapi/v1/rigging", gid, "rigging")
        json.dump(rig, open(os.path.join(arch, "rigging.json"), "w"), indent=2)
        urls = find_urls(rig.get("result", rig), "_fbx_url")
        print("    rig outputs:", ", ".join(sorted(urls)) or "none")
        want = spec["anim"]
        fbx_url = (next((u for k, u in urls.items() if want in k and "armature" not in k), None)
                   or next((u for k, u in urls.items() if ("running" in k or "walking" in k) and "armature" not in k), None)
                   or urls.get("rigged_character_fbx_url"))
      except Exception as e:
        print(f"    ! rigging failed ({e}) — using the static model; Unity will add a march bounce instead")

    if not fbx_url:
        fbx_url = (refined.get("model_urls") or {}).get("fbx")
    if not fbx_url:
        raise RuntimeError("no FBX url returned by Meshy")

    # 4. download into Unity + archive
    download(fbx_url, os.path.join(arch, f"{name}.fbx"))
    download(fbx_url, os.path.join(UNITY_OUT, f"{name}.fbx"))
    if tex_url:
        download(tex_url, os.path.join(UNITY_OUT, f"{name}_BaseColor.png"))

    hist_path = os.path.join(ARCHIVE, "history.json")
    try:
        hist = json.load(open(hist_path))
    except Exception:
        hist = {"version": 1, "projects": []}
    hist.setdefault("projects", []).append({
        "folder": os.path.basename(arch), "prompt": spec["prompt"], "task_type": "text-to-3d+rigging",
        "root_task_id": pid, "created_at": datetime.utcnow().isoformat() + "Z", "task_count": 3,
    })
    json.dump(hist, open(hist_path, "w"), indent=2)


def main():
    global KEY
    KEY = api_key()
    which = sys.argv[1:] or ["enemy", "boss"]
    for w in which:
        if w not in CHARACTERS:
            sys.exit(f"unknown '{w}'. choose from: {', '.join(CHARACTERS)}")
    for w in which:
        generate(CHARACTERS[w])
    print("\nDone. Switch to Unity — it imports Assets/Resources/ArmyRun/ and Level 1 uses the new models.")


if __name__ == "__main__":
    main()
