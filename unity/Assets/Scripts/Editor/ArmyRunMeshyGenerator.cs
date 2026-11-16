#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace MobControl.EditorTools
{
    /// <summary>
    /// Generates the Level 1 enemy + boss models with Meshy AI, straight from the Unity editor
    /// (the editor runs on your Mac, so it has normal internet).
    ///
    ///   Tools ▸ Army Run ▸ Generate Meshy Models (enemy + boss)
    ///
    /// Per model: text-to-3d preview → refine (texture) → rigging (walk/run animation) → download into
    /// Assets/Resources/ArmyRun/<Name>.fbx (+ _BaseColor.png). ArmyRunModels picks them up automatically.
    /// The player character is NOT generated or changed.
    /// API key: env MESHY_API_KEY, else gameproject1/.agents/mcp_config.json (never logged).
    /// Progress is printed in the Console ("[Meshy] ..."); takes ~5–10 minutes.
    /// </summary>
    public static class ArmyRunMeshyGenerator
    {
        private const string BASE = "https://api.meshy.ai";
        private const string OUT = "Assets/Resources/ArmyRun";

        private class Spec { public string name, prompt, texturePrompt, anim; public int poly; public float height; }

        private static readonly Spec[] Specs =
        {
            new Spec
            {
                name = "EnemySoldier",
                prompt = "cute chubby red blob minion creature, small round body, short stubby arms and legs, two tiny horns, big white eyes, " +
                         "angry little mouth, glossy bright red skin, stylized cartoon mobile crowd game character, full body, A-pose, clean topology",
                texturePrompt = "glossy bright red skin, white eyes with black pupils, darker red feet, clean cartoon colors",
                anim = "running", poly = 3000, height = 1.0f
            },
            new Spec
            {
                name = "BossTyrant",
                prompt = "huge muscular brute giant boss, red and yellow armored body, bald angry head, massive fists, thick arms and legs, " +
                         "stylized cartoon mobile game boss character, chunky proportions, full body, A-pose, clean topology",
                texturePrompt = "bright red skin, yellow armor plates and belt, dark brown pants, angry eyes, bold cartoon colors",
                anim = "walking", poly = 12000, height = 3.5f
            },
        };

        private class Job
        {
            public Spec spec;
            public string stage = "preview", taskId, previewId, refineId;
            public UnityWebRequest req;
            public double nextPoll;
            public string fbxUrl, texUrl;
            public bool done, failed;
            public int lastProgress = -1;
            public bool retriedWithoutOptional;
        }

        private static readonly List<Job> jobs = new List<Job>();
        private static string key;

        [MenuItem("Tools/Army Run/Generate Meshy Models (enemy + boss)")]
        public static void GenerateAll()
        {
            if (jobs.Count > 0 && jobs.Exists(j => !j.done && !j.failed)) { Debug.LogWarning("[Meshy] Already running — watch the Console."); return; }
            key = ReadKey();
            if (string.IsNullOrEmpty(key)) { Debug.LogError("[Meshy] No API key (set MESHY_API_KEY or .agents/mcp_config.json)."); return; }
            jobs.Clear();
            foreach (var s in Specs) jobs.Add(new Job { spec = s });
            foreach (var j in jobs) StartPreview(j);
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Debug.Log("[Meshy] Started: " + string.Join(", ", Array.ConvertAll(Specs, s => s.name)) + " (≈5–10 min). Keep Unity open.");
        }

        // ─────────────────────────────── pipeline ──

        private static void StartPreview(Job j)
        {
            var sb = new StringBuilder("{");
            sb.Append($"\"mode\":\"preview\",\"prompt\":{Q(j.spec.prompt)},\"art_style\":\"realistic\",\"should_remesh\":true,");
            sb.Append($"\"topology\":\"triangle\",\"target_polycount\":{j.spec.poly}");
            if (!j.retriedWithoutOptional) sb.Append(",\"symmetry_mode\":\"on\",\"pose_mode\":\"a-pose\"");
            sb.Append("}");
            j.stage = "preview-create";
            j.req = Post("/openapi/v2/text-to-3d", sb.ToString());
        }

        private static void Tick()
        {
            bool active = false;
            foreach (var j in jobs)
            {
                if (j.done || j.failed) continue;
                active = true;
                try { Step(j); }
                catch (Exception e) { Fail(j, e.Message); }
            }
            if (!active)
            {
                EditorApplication.update -= Tick;
                AssetDatabase.Refresh();
                int ok = jobs.FindAll(x => x.done).Count;
                Debug.Log($"[Meshy] Finished: {ok}/{jobs.Count} models in {OUT}. Press Play — Level 1 uses them automatically.");
            }
        }

        private static void Step(Job j)
        {
            if (j.req != null)
            {
                if (!j.req.isDone) return;
                string body = j.req.downloadHandler != null ? j.req.downloadHandler.text : "";
                long code = j.req.responseCode;
                bool ok = j.req.result == UnityWebRequest.Result.Success;
                j.req.Dispose();
                j.req = null;
                if (!ok)
                {
                    if (code == 400 && j.stage == "preview-create" && !j.retriedWithoutOptional)
                    {
                        j.retriedWithoutOptional = true;
                        Debug.Log($"[Meshy] {j.spec.name}: retrying without pose/symmetry options");
                        StartPreview(j);
                        return;
                    }
                    Fail(j, $"{j.stage} HTTP {code}: {Trim(body)}");
                    return;
                }
                OnResponse(j, body);
                return;
            }
            if (EditorApplication.timeSinceStartup < j.nextPoll) return;

            switch (j.stage)
            {
                case "preview-poll": j.req = Get("/openapi/v2/text-to-3d/" + j.previewId); break;
                case "refine-poll": j.req = Get("/openapi/v2/text-to-3d/" + j.refineId); break;
                case "rig-poll": j.req = Get("/openapi/v1/rigging/" + j.taskId); break;
            }
        }

        private static void OnResponse(Job j, string body)
        {
            switch (j.stage)
            {
                case "preview-create":
                    j.previewId = Field(body, "result");
                    j.stage = "preview-poll"; j.nextPoll = Now() + 5;
                    Debug.Log($"[Meshy] {j.spec.name}: preview task created");
                    break;

                case "preview-poll":
                    if (Poll(j, body, "preview"))
                    {
                        j.stage = "refine-create";
                        string payload = $"{{\"mode\":\"refine\",\"preview_task_id\":{Q(j.previewId)},\"enable_pbr\":false,\"texture_prompt\":{Q(j.spec.texturePrompt)}}}";
                        j.req = Post("/openapi/v2/text-to-3d", payload);
                    }
                    break;

                case "refine-create":
                    j.refineId = Field(body, "result");
                    j.stage = "refine-poll"; j.nextPoll = Now() + 5;
                    Debug.Log($"[Meshy] {j.spec.name}: texturing…");
                    break;

                case "refine-poll":
                    if (Poll(j, body, "texture"))
                    {
                        j.texUrl = Field(body, "base_color");
                        var fbx = Regex.Match(body, "\"fbx\"\\s*:\\s*\"([^\"]+)\"");
                        if (fbx.Success) j.fbxUrl = Unescape(fbx.Groups[1].Value);
                        j.stage = "rig-create";
                        j.req = Post("/openapi/v1/rigging", $"{{\"input_task_id\":{Q(j.refineId)},\"height_meters\":{j.spec.height.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}");
                    }
                    break;

                case "rig-create":
                    j.taskId = Field(body, "result");
                    j.stage = "rig-poll"; j.nextPoll = Now() + 5;
                    Debug.Log($"[Meshy] {j.spec.name}: rigging + animation…");
                    break;

                case "rig-poll":
                    if (Poll(j, body, "rigging"))
                    {
                        // prefer the animated FBX (running / walking), else the rigged character, else the static model
                        string best = null, rigged = null;
                        foreach (Match m in Regex.Matches(body, "\"(\\w+_fbx_url)\"\\s*:\\s*\"([^\"]+)\""))
                        {
                            string k = m.Groups[1].Value, v = Unescape(m.Groups[2].Value);
                            if (k.Contains("armature")) continue;
                            if (k.Contains(j.spec.anim)) best = v;
                            else if (best == null && (k.Contains("running") || k.Contains("walking"))) best = v;
                            if (k.Contains("rigged_character")) rigged = v;
                        }
                        j.fbxUrl = best ?? rigged ?? j.fbxUrl;
                        Download(j);
                    }
                    break;
            }
        }

        /// <summary>true when SUCCEEDED; handles progress, failure and (for rigging) falls back to the static model.</summary>
        private static bool Poll(Job j, string body, string label)
        {
            string status = Field(body, "status");
            int.TryParse(Regex.Match(body, "\"progress\"\\s*:\\s*(\\d+)").Groups[1].Value, out int progress);
            if (progress != j.lastProgress) { Debug.Log($"[Meshy] {j.spec.name}: {label} {status} {progress}%"); j.lastProgress = progress; }
            if (status == "SUCCEEDED") { j.lastProgress = -1; return true; }
            if (status == "FAILED" || status == "EXPIRED" || status == "CANCELED")
            {
                if (label == "rigging" && !string.IsNullOrEmpty(j.fbxUrl))
                {
                    Debug.LogWarning($"[Meshy] {j.spec.name}: rigging failed — using the static model (Unity adds a march bounce).");
                    Download(j);
                    return false;
                }
                Fail(j, $"{label} {status}: {Trim(body)}");
                return false;
            }
            j.nextPoll = Now() + 5;
            return false;
        }

        private static void Download(Job j)
        {
            if (string.IsNullOrEmpty(j.fbxUrl)) { Fail(j, "no FBX url in Meshy response"); return; }
            Directory.CreateDirectory(OUT);
            Save(j.fbxUrl, Path.Combine(OUT, j.spec.name + ".fbx"));
            if (!string.IsNullOrEmpty(j.texUrl)) Save(j.texUrl, Path.Combine(OUT, j.spec.name + "_BaseColor.png"));
            j.done = true;
            j.stage = "done";
            Debug.Log($"[Meshy] {j.spec.name}: ✔ saved to {OUT}/{j.spec.name}.fbx");
        }

        private static void Save(string url, string path)
        {
            using (var r = UnityWebRequest.Get(url))
            {
                var op = r.SendWebRequest();
                double until = Now() + 120;
                while (!op.isDone && Now() < until) System.Threading.Thread.Sleep(50);
                if (r.result != UnityWebRequest.Result.Success) throw new Exception("download failed: " + r.error);
                File.WriteAllBytes(path, r.downloadHandler.data);
            }
        }

        private static void Fail(Job j, string msg)
        {
            j.failed = true;
            Debug.LogError($"[Meshy] {j.spec.name}: {msg}");
        }

        // ─────────────────────────────── http / json helpers ──

        private static UnityWebRequest Post(string path, string json)
        {
            var r = new UnityWebRequest(BASE + path, "POST")
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer()
            };
            r.SetRequestHeader("Content-Type", "application/json");
            r.SetRequestHeader("Authorization", "Bearer " + key);
            r.SendWebRequest();
            return r;
        }

        private static UnityWebRequest Get(string path)
        {
            var r = UnityWebRequest.Get(BASE + path);
            r.SetRequestHeader("Authorization", "Bearer " + key);
            r.SendWebRequest();
            return r;
        }

        private static string Field(string json, string name)
        {
            var m = Regex.Match(json, "\"" + name + "\"\\s*:\\s*\"([^\"]*)\"");
            return m.Success ? Unescape(m.Groups[1].Value) : null;
        }

        private static string Unescape(string s) => s.Replace("\\u0026", "&").Replace("\\/", "/");
        private static string Q(string s) => "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        private static string Trim(string s) => string.IsNullOrEmpty(s) ? "" : (s.Length > 300 ? s.Substring(0, 300) : s);
        private static double Now() => EditorApplication.timeSinceStartup;

        private static string ReadKey()
        {
            string env = Environment.GetEnvironmentVariable("MESHY_API_KEY");
            if (!string.IsNullOrEmpty(env)) return env.Trim();
            string cfg = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".agents", "mcp_config.json"));
            if (!File.Exists(cfg)) return null;
            var m = Regex.Match(File.ReadAllText(cfg), "\"MESHY_API_KEY\"\\s*:\\s*\"([^\"]+)\"");
            return m.Success ? m.Groups[1].Value : null;
        }
    }
}
#endif
