using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MobControl.Level
{
    /// <summary>
    /// Free CC0 characters (Quaternius via Poly Pizza) baked by scripts/bake_polypizza.py into
    /// Resources/ArmyRun/Baked/&lt;Name&gt;.bytes — no glTF package needed.
    ///
    ///   Zombie     the horde (walk + run cycles, ~1.7k tris, drawn with GPU instancing)
    ///   Orc        the Brute (green) and the Master Tyrant (red tint): Idle / Walk / Punch / Weapon / Death
    ///   BlueDemon  the giant ally: Idle / Walk / Punch / Weapon / Death / Wave (HELP!)
    ///   SnowPine   forest beside the road
    ///
    /// Every animation frame is a pre-skinned mesh; playing a clip = swapping meshes (no skinning cost).
    /// Colours are vertex colours drawn by ArmyRun/VertexColorLit (one draw call per model); if that shader
    /// is unavailable the model falls back to one Standard material per colour.
    /// </summary>
    public static class ArmyRunBakedModels
    {
        public const string FOLDER = "ArmyRun/Baked/";
        public const string SHADER = "ArmyRun/Shaders/ArmyVertexColorLit";

        public sealed class Model
        {
            public string name;
            public string[] clipNames;
            public float[] durations;
            public bool[] loops;
            public Mesh[][] frames;          // [clip][frame]
            public Material[] materials;     // 1 (vertex colour) or one per colour (fallback)
            public int Clip(string n)
            {
                for (int i = 0; i < clipNames.Length; i++) if (clipNames[i] == n) return i;
                return -1;
            }
            /// <summary>Mesh of clip c at time t (seconds); loops or holds the last frame.</summary>
            public Mesh Sample(int c, float t)
            {
                var fr = frames[c];
                if (fr.Length == 1 || durations[c] <= 0f) return fr[0];
                float k = t / durations[c];
                int i = loops[c] ? (int)(Mathf.Repeat(k, 1f) * fr.Length) % fr.Length
                                 : Mathf.Clamp(Mathf.FloorToInt(k * (fr.Length - 1) + 0.5f), 0, fr.Length - 1);
                return fr[i];
            }
        }

        private static readonly Dictionary<string, Model> s_cache = new Dictionary<string, Model>();
        private static Shader s_shader;
        private static bool s_shaderChecked;

        public static bool Has(string name) => Load(name) != null;

        public static Model Load(string name)
        {
            if (s_cache.TryGetValue(name, out var cached)) return cached;
            Model m = null;
            var ta = Resources.Load<TextAsset>(FOLDER + name);
            if (ta != null)
            {
                try { m = Parse(name, ta.bytes); }
                catch (System.Exception e) { Debug.LogWarning($"[ArmyRun] baked model {name} failed to load: {e.Message}"); m = null; }
            }
            s_cache[name] = m;
            return m;
        }

        private static Shader VertexColorShader()
        {
            if (!s_shaderChecked)
            {
                s_shaderChecked = true;
                s_shader = Resources.Load<Shader>(SHADER);
                if (s_shader != null && !s_shader.isSupported) s_shader = null;
            }
            return s_shader;
        }

        private static Model Parse(string name, byte[] bytes)
        {
            using (var br = new BinaryReader(new MemoryStream(bytes)))
            {
                if (new string(br.ReadChars(4)) != "ARB1") throw new System.Exception("bad header");
                int matCount = br.ReadInt32();
                var cols = new Color[matCount];
                bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
                for (int i = 0; i < matCount; i++)
                {
                    var c = new Color(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    cols[i] = linear ? c.linear : c;
                }
                int vc = br.ReadInt32();
                var subIdx = new int[matCount][];
                var vertCol = new Color[vc];
                int total = 0;
                for (int s = 0; s < matCount; s++)
                {
                    int n = br.ReadInt32();
                    var idx = new int[n];
                    for (int k = 0; k < n; k++) { idx[k] = br.ReadInt32(); vertCol[idx[k]] = cols[s]; }
                    subIdx[s] = idx;
                    total += n;
                }

                Shader vcs = VertexColorShader();
                bool useVc = vcs != null;
                int[] allIdx = null;
                if (useVc)
                {
                    allIdx = new int[total];
                    int o = 0;
                    foreach (var a in subIdx) { System.Array.Copy(a, 0, allIdx, o, a.Length); o += a.Length; }
                }

                int clipCount = br.ReadInt32();
                var m = new Model
                {
                    name = name, clipNames = new string[clipCount], durations = new float[clipCount],
                    loops = new bool[clipCount], frames = new Mesh[clipCount][]
                };
                var pos = new Vector3[vc];
                var nor = new Vector3[vc];
                for (int c = 0; c < clipCount; c++)
                {
                    int ln = br.ReadInt32();
                    m.clipNames[c] = System.Text.Encoding.UTF8.GetString(br.ReadBytes(ln));
                    m.durations[c] = br.ReadSingle();
                    m.loops[c] = br.ReadByte() != 0;
                    int fc = br.ReadInt32();
                    m.frames[c] = new Mesh[fc];
                    for (int f = 0; f < fc; f++)
                    {
                        for (int v = 0; v < vc; v++) pos[v] = new Vector3(br.ReadInt16(), br.ReadInt16(), br.ReadInt16()) / 16000f;
                        for (int v = 0; v < vc; v++) nor[v] = new Vector3(br.ReadSByte(), br.ReadSByte(), br.ReadSByte()) / 127f;
                        var mesh = new Mesh { name = $"{name}_{m.clipNames[c]}_{f}" };
                        if (vc > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                        mesh.SetVertices(pos);
                        mesh.SetNormals(nor);
                        if (useVc)
                        {
                            mesh.SetColors(vertCol);
                            mesh.SetTriangles(allIdx, 0, false);
                        }
                        else
                        {
                            mesh.subMeshCount = matCount;
                            for (int s = 0; s < matCount; s++) mesh.SetTriangles(subIdx[s], s, false);
                        }
                        mesh.RecalculateBounds();
                        mesh.bounds = new Bounds(mesh.bounds.center, mesh.bounds.size * 1.3f);   // animated: generous bounds
                        mesh.UploadMeshData(!(clipCount == 1 && fc == 1));   // static props stay readable (combined into tiles)
                        m.frames[c][f] = mesh;
                    }
                }

                if (useVc)
                {
                    var mat = new Material(vcs) { name = "Baked_" + name, enableInstancing = true, color = Color.white };
                    m.materials = new[] { mat };
                }
                else
                {
                    m.materials = new Material[matCount];
                    Shader std = Shader.Find("Standard");
                    for (int s = 0; s < matCount; s++)
                    {
                        var mat = new Material(std) { name = $"Baked_{name}_{s}", color = cols[s], enableInstancing = true };
                        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.25f);
                        m.materials[s] = mat;
                    }
                }
                return m;
            }
        }

        /// <summary>
        /// Inactive character template: root (ArmyBakedRig) → "Body" (MeshFilter + MeshRenderer, scaled to 'height').
        /// Faces +Z, feet on 0. tint multiplies all colours (Master Tyrant = red Orc). Null if not baked.
        /// </summary>
        public static GameObject BuildCharacterTemplate(string name, float height, Transform parent, Color tint)
        {
            var model = Load(name);
            if (model == null) return null;
            var tmpl = new GameObject(name + "_BakedTemplate");
            tmpl.SetActive(false);
            tmpl.transform.SetParent(parent, false);
            var body = new GameObject("Body");
            body.transform.SetParent(tmpl.transform, false);
            body.transform.localScale = Vector3.one * height;
            var mf = body.AddComponent<MeshFilter>();
            int idle = model.Clip("Idle");
            mf.sharedMesh = model.frames[idle >= 0 ? idle : 0][0];
            var mr = body.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var mats = new Material[model.materials.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                if (tint == Color.white) { mats[i] = model.materials[i]; continue; }
                mats[i] = new Material(model.materials[i]) { name = model.materials[i].name + "_Tint" };
                mats[i].color = model.materials[i].color * tint;
            }
            mr.sharedMaterials = mats;
            var rig = tmpl.AddComponent<ArmyBakedRig>();
            rig.modelName = name;
            rig.body = mf;
            return tmpl;
        }
    }

    /// <summary>
    /// Plays baked clips on a character by swapping meshes. Same API as the hand-built rig:
    /// walks when it (or its parent) moves or forceWalk is set, idles otherwise, Punch() / Slam(windup)
    /// (club swing: wind-up lasts the slam telegraph, hits as the circle lands), Die(), helpPose = Wave.
    /// </summary>
    public class ArmyBakedRig : ArmyRigBase
    {
        public string modelName;
        public MeshFilter body;
        public float walkSpeedScale = 1f;

        private ArmyRunBakedModels.Model model;
        private int cIdle, cWalk, cPunch, cWeapon, cDeath, cWave;
        private float t, actionT = -1f, slamDur = 1f, dieT = -1f;
        private int action = -1;                 // clip index of a one-shot (punch / weapon)
        private Vector3 lastOwn, lastParent;
        private bool hasLast;
        private float walkBlend;

        private const float SLAM_STRIKE = 0.3f;

        private void Awake()
        {
            model = ArmyRunBakedModels.Load(modelName);
            if (body == null) body = GetComponentInChildren<MeshFilter>();
            if (model == null) { enabled = false; return; }
            cIdle = model.Clip("Idle"); cWalk = model.Clip("Walk"); cPunch = model.Clip("Punch");
            cWeapon = model.Clip("Weapon"); cDeath = model.Clip("Death"); cWave = model.Clip("Wave");
            if (cIdle < 0) cIdle = 0;
            if (cWalk < 0) cWalk = cIdle;
            t = Random.value * 2f;
        }

        public override void Punch()
        {
            if (dieT >= 0f || cPunch < 0 || action >= 0) return;
            action = cPunch; actionT = 0f;
        }

        public override void Slam(float windup)
        {
            if (dieT >= 0f) return;
            action = cWeapon >= 0 ? cWeapon : cPunch;
            actionT = 0f;
            slamDur = Mathf.Max(0.3f, windup);
        }

        public override void Die() { dieT = 0f; action = -1; }

        private void OnEnable() { hasLast = false; }

        private void Update()
        {
            if (model == null || body == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            t += dt;

            Vector3 own = transform.localPosition, par = transform.parent != null ? transform.parent.localPosition : Vector3.zero;
            float speed = hasLast ? ((own - lastOwn).magnitude + (par - lastParent).magnitude) / dt : 0f;
            lastOwn = own; lastParent = par; hasLast = true;
            walkBlend = Mathf.MoveTowards(walkBlend, (forceWalk || speed > 0.15f) ? 1f : 0f, dt * 4f);

            Mesh mesh;
            if (dieT >= 0f && cDeath >= 0)
            {
                dieT += dt;
                mesh = model.Sample(cDeath, dieT);
            }
            else if (action >= 0)
            {
                actionT += dt;
                float dur = model.durations[action];
                float clipTime;
                if (action == cWeapon && slamDur > 0f && dur > 0f)
                {
                    // wind-up stretched over the telegraph (first 55 % of the swing), strike in SLAM_STRIKE seconds
                    if (actionT < slamDur) clipTime = dur * 0.55f * (actionT / slamDur);
                    else clipTime = dur * (0.55f + 0.45f * Mathf.Clamp01((actionT - slamDur) / SLAM_STRIKE));
                    if (actionT > slamDur + SLAM_STRIKE + 0.15f) action = -1;
                }
                else
                {
                    clipTime = actionT;
                    if (actionT >= dur) action = -1;
                }
                mesh = action >= 0 ? model.Sample(action, clipTime) : model.Sample(cIdle, t);
            }
            else if (helpPose && cWave >= 0) mesh = model.Sample(cWave, t);
            else if (walkBlend > 0.5f) mesh = model.Sample(cWalk, t * walkSpeedScale);
            else mesh = model.Sample(cIdle, t);

            if (body.sharedMesh != mesh) body.sharedMesh = mesh;
        }
    }
}
