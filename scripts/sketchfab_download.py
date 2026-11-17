#!/usr/bin/env python3
"""
Download free (CC) Sketchfab models for Level 1 and drop them into Unity.

    cd ~/Downloads/gameproject1
    python3 scripts/sketchfab_download.py            # every model in MODELS
    python3 scripts/sketchfab_download.py zombies    # only one

Token (your own Sketchfab account → Settings → Password & API → "API Token"), read from, in order:
    1. env var SKETCHFAB_API_TOKEN
    2. file .agents/sketchfab_token.txt   (one line, never committed — add it to .gitignore)

For each model:
    • checks the licence is a free Creative Commons one (CC0 / CC-BY / CC-BY-SA) — anything else is skipped
    • downloads the glTF-binary (.glb) archive, or the glTF zip if no .glb is offered
    • puts it in  unity/Assets/Resources/ArmyRun/Sketchfab/<Name>/   (+ a copy in sketchfab_downloads/)
    • appends author / licence / link to  unity/Assets/Resources/ArmyRun/Sketchfab/CREDITS.md
      (CC-BY requires this credit — show it in the game's credits screen too)

Unity needs the glTFast package (com.unity.cloud.gltfast) to import .glb files.
Standard library only.
"""
import io
import json
import os
import sys
import urllib.error
import urllib.request
import zipfile

API = "https://api.sketchfab.com/v3"
PROJECT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
UNITY_OUT = os.path.join(PROJECT, "unity", "Assets", "Resources", "ArmyRun", "Sketchfab")
ARCHIVE = os.path.join(PROJECT, "sketchfab_downloads")
FREE_LICENCES = ("cc0", "by", "by-sa", "cc attribution", "creative commons", "public domain")

# what Level 1 uses each model for
MODELS = {
    "zombies": {"uid": "6c112bed707b4444864b606299f8b85a", "name": "LowPolyZombies",   "use": "the red horde (instanced) + gunners"},
    "ripper":  {"uid": "c5a77a38545543ce9c64eaa534afc23b", "name": "RipperZombie",     "use": "the Brute mini-boss"},
    "pines":   {"uid": "6c588ea426904f0b91a9f1f3081014bf", "name": "SnowyPines",       "use": "forest beside the road"},
}


def token():
    t = os.environ.get("SKETCHFAB_API_TOKEN")
    if t:
        return t.strip()
    path = os.path.join(PROJECT, ".agents", "sketchfab_token.txt")
    if os.path.exists(path):
        return open(path).read().strip()
    sys.exit("No Sketchfab token: set SKETCHFAB_API_TOKEN or put it in .agents/sketchfab_token.txt")


def get(url, tok=None):
    req = urllib.request.Request(url, headers={"Authorization": f"Token {tok}"} if tok else {})
    try:
        with urllib.request.urlopen(req, timeout=60) as r:
            return json.loads(r.read().decode())
    except urllib.error.HTTPError as e:
        raise RuntimeError(f"GET {url} -> HTTP {e.code}: {e.read().decode(errors='replace')[:300]}") from None


def fetch(url):
    with urllib.request.urlopen(url, timeout=300) as r:     # signed S3 link: no auth header
        return r.read()


def download(key, spec, tok):
    uid, name = spec["uid"], spec["name"]
    info = get(f"{API}/models/{uid}")
    lic = (info.get("license") or {})
    lic_label = lic.get("label") or lic.get("slug") or "unknown"
    if not any(f in lic_label.lower() or f == (lic.get("slug") or "").lower() for f in FREE_LICENCES) or "nc" in (lic.get("slug") or "").lower():
        print(f"  ! {name}: licence '{lic_label}' is not a free commercial-use CC licence — skipped")
        return False

    links = get(f"{API}/models/{uid}/download", tok)
    kind = "glb" if "glb" in links else ("gltf" if "gltf" in links else None)
    if kind is None:
        print(f"  ! {name}: no glb/gltf download offered ({list(links)})")
        return False
    size = links[kind].get("size")
    print(f"  {name}: {kind}, {size // 1024 if size else '?'} KB, {lic_label}, by {info['user']['username']}")
    data = fetch(links[kind]["url"])

    out = os.path.join(UNITY_OUT, name)
    arch = os.path.join(ARCHIVE, name)
    os.makedirs(out, exist_ok=True)
    os.makedirs(arch, exist_ok=True)
    if kind == "glb":
        for d in (out, arch):
            open(os.path.join(d, name + ".glb"), "wb").write(data)
    else:
        with zipfile.ZipFile(io.BytesIO(data)) as z:
            for d in (out, arch):
                for member in z.namelist():                 # refuse path tricks inside the archive
                    target = os.path.realpath(os.path.join(d, member))
                    if not target.startswith(os.path.realpath(d) + os.sep):
                        raise RuntimeError(f"unsafe path in archive: {member}")
                z.extractall(d)

    credits = os.path.join(UNITY_OUT, "CREDITS.md")
    line = (f"- **{info['name']}** by [{info['user']['displayName'] or info['user']['username']}]"
            f"({info['user'].get('profileUrl', '')}) — {lic_label} — {info.get('viewerUrl', '')} — used for {spec['use']}\n")
    existing = open(credits).read() if os.path.exists(credits) else "# Sketchfab models used in Level 1\n\n"
    if info.get("viewerUrl", "") not in existing:
        open(credits, "w").write(existing + line)
    return True


def main():
    tok = token()
    which = sys.argv[1:] or list(MODELS)
    for w in which:
        if w not in MODELS:
            sys.exit(f"unknown '{w}'. choose from: {', '.join(MODELS)}")
    ok = 0
    for w in which:
        try:
            ok += bool(download(w, MODELS[w], tok))
        except Exception as e:
            print(f"  ! {w}: {e}")
    print(f"\n{ok}/{len(which)} downloaded → unity/Assets/Resources/ArmyRun/Sketchfab/  (credits in CREDITS.md)")


if __name__ == "__main__":
    main()
