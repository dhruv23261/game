#!/usr/bin/env python3
import os
import sys
import time
import json
import urllib.request
import urllib.error

MESHY_API_KEY = os.environ.get("MESHY_API_KEY", "msy_XINtti4yLBnGQA95HrOYxFBoseFoPjrrdIvk")
BASE_URL = "https://api.meshy.ai"

def request_json(url, data=None, method="GET"):
    headers = {
        "Authorization": f"Bearer {MESHY_API_KEY}",
        "Content-Type": "application/json"
    }
    req = urllib.request.Request(
        url,
        data=json.dumps(data).encode("utf-8") if data else None,
        headers=headers,
        method=method
    )
    with urllib.request.urlopen(req) as resp:
        return json.loads(resp.read().decode("utf-8"))

def check_balance():
    print(f"[*] Checking Meshy API key status...")
    tasks = request_json(f"{BASE_URL}/openapi/v2/text-to-3d")
    print(f"[+] API Key authenticated successfully! Tasks count: {len(tasks)}")
    return True

def create_image_to_3d(image_url, enable_pbr=True, surface_mode="hard"):
    payload = {
        "image_url": image_url,
        "enable_pbr": enable_pbr,
        "surface_mode": surface_mode
    }
    res = request_json(f"{BASE_URL}/openapi/v1/image-to-3d", data=payload, method="POST")
    task_id = res.get("result")
    print(f"[+] Image-to-3D Task Created: {task_id}")
    return task_id

def create_text_to_3d(prompt, art_style="realistic", negative_prompt="low quality, low poly, blurry"):
    payload = {
        "mode": "preview",
        "prompt": prompt,
        "art_style": art_style,
        "negative_prompt": negative_prompt
    }
    res = request_json(f"{BASE_URL}/openapi/v2/text-to-3d", data=payload, method="POST")
    task_id = res.get("result")
    print(f"[+] Text-to-3D Preview Task Created: {task_id}")
    return task_id

def poll_task(endpoint_prefix, task_id, timeout_sec=600):
    start = time.time()
    url = f"{BASE_URL}/{endpoint_prefix}/{task_id}"
    while time.time() - start < timeout_sec:
        res = request_json(url)
        status = res.get("status")
        progress = res.get("progress", 0)
        print(f"[*] Task {task_id}: {status} ({progress}%)")
        if status == "SUCCEEDED":
            return res
        elif status in ["FAILED", "EXPIRED"]:
            raise Exception(f"Task {task_id} failed: {res.get('task_error')}")
        time.sleep(5)
    raise TimeoutError(f"Task {task_id} timed out after {timeout_sec}s")

def download_model(model_url, output_path):
    print(f"[*] Downloading {model_url} -> {output_path}...")
    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    urllib.request.urlretrieve(model_url, output_path)
    print(f"[+] Saved model to {output_path} ({os.path.getsize(output_path)} bytes)")

if __name__ == "__main__":
    if len(sys.argv) < 2:
        check_balance()
    elif sys.argv[1] == "check":
        check_balance()
    else:
        print("Usage: meshy_tool.py [check]")
