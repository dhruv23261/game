using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace MobControl.Level
{
    /// <summary>
    /// Character models for the Level 1 Army Run.
    ///
    /// 1) MESHY MODELS (preferred). Run  scripts/meshy_armyrun_generate.py  once. It drops files into
    ///    Assets/Resources/ArmyRun/ :
    ///        EnemySoldier.fbx  (+ EnemySoldier_BaseColor.png)   — red Dark Legion soldier, rigged + running anim
    ///        BossTyrant.fbx    (+ BossTyrant_BaseColor.png)     — the big Tyrant boss, rigged + walking anim
    ///    They are picked up automatically, scaled to the right height and their animation is looped.
    ///
    /// 2) FALLBACK (no Meshy files yet): enemies = the player's own soldier with the uniform recoloured red,
    ///    boss = that red soldier at 3.2x size.
    ///
    /// 3) PARADE HORDE: the huge red army marching in the left lane is hundreds of soldiers, so it uses a
    ///    tiny procedural low-poly soldier mesh (one mesh per 2 rows, 1 draw call each) instead of skinned models.
    /// </summary>
    public static class ArmyRunModels
    {
        public const string RES_FOLDER = "ArmyRun/";
        public const string ENEMY_NAME = "EnemySoldier";
        public const string BOSS_NAME = "BossTyrant";
        /// <summary>If a generated model walks backwards, change this to 180.</summary>
        public const float GENERATED_MODEL_YAW = 0f;

        // ═════════════════════════════════════ Meshy models ══

        public static bool HasGenerated(string name) => Resources.Load<GameObject>(RES_FOLDER + name) != null;

        /// <summary>
        /// Builds an inactive template from Resources/ArmyRun/&lt;name&gt; with its feet on y = 0, facing +Z,
        /// exactly targetHeight metres tall. Returns null when the model has not been generated yet.
        /// </summary>
        public static GameObject TryBuildGeneratedTemplate(string name, float targetHeight, Transform parent)
        {
            GameObject prefab = Resources.Load<GameObject>(RES_FOLDER + name);
            if (prefab == null) return null;

            var tmpl = new GameObject(name + "_Template");
            tmpl.SetActive(false);
            tmpl.transform.SetParent(parent, false);

            GameObject model = Object.Instantiate(prefab, tmpl.transform);
            model.name = "Model";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.Euler(0f, GENERATED_MODEL_YAW, 0f);

            // Texture fix-up: if the FBX material lost its texture, use the downloaded base colour
            Texture2D baseTex = Resources.Load<Texture2D>(RES_FOLDER + name + "_BaseColor");
            Material texMat = null;
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (baseTex != null && (mats[i] == null || mats[i].mainTexture == null))
                    {
                        if (texMat == null)
                        {
                            texMat = ArmyRunEnvironment.LitMat("M_" + name, Color.white, 0.25f);
                            texMat.mainTexture = baseTex;
                        }
                        mats[i] = texMat;
                        changed = true;
                    }
                }
                if (changed) r.sharedMaterials = mats;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            // Normalise size: measure with the template active for one moment (no scripts on it yet)
            tmpl.SetActive(true);
            Bounds b = MeasureBounds(model);
            tmpl.SetActive(false);
            if (b.size.y > 1e-4f)
            {
                float s = targetHeight / b.size.y;
                model.transform.localScale *= s;
                Vector3 c = tmpl.transform.InverseTransformPoint(b.center);
                Vector3 min = tmpl.transform.InverseTransformPoint(b.min);
                model.transform.localPosition = new Vector3(-c.x * s, -min.y * s, -c.z * s);
            }

            // Animation: loop the first real clip found inside the FBX (Meshy rigging gives walk/run)
            AnimationClip clip = FindClip(name);
            if (clip != null)
            {
                Animator anim = model.GetComponentInChildren<Animator>(true);
                if (anim == null) anim = model.AddComponent<Animator>();
                anim.applyRootMotion = false;
                anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                var looper = model.AddComponent<ArmyClipLooper>();
                looper.clip = clip;
            }
            else
            {
                model.AddComponent<ArmyMarchBob>(); // static mesh: procedural march bounce
            }
            return tmpl;
        }

        private static AnimationClip FindClip(string name)
        {
            AnimationClip best = null;
            foreach (var c in Resources.LoadAll<AnimationClip>(RES_FOLDER + name))
            {
                if (c == null || c.name.StartsWith("__preview__") || c.legacy) continue;
                string n = c.name.ToLowerInvariant();
                if (n.Contains("run") || n.Contains("walk")) return c;
                if (best == null) best = c;
            }
            return best;
        }

        public static Bounds MeasureBounds(GameObject go)
        {
            bool has = false;
            Bounds b = new Bounds(go.transform.position, Vector3.zero);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer) continue;
                if (r.GetComponent<TMPro.TMP_Text>() != null) continue;
                if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds);
            }
            return b;
        }

        // ═════════════════════════════════════ Parade horde (low poly) ══

        private static Material s_hordeMat;
        private static Mesh s_hordeRowMesh;
        private static int s_rowCols;
        private static float s_rowSpacing, s_rowHeight;

        /// <summary>Material for the low-poly soldiers: lit Standard + a 5-colour palette texture.</summary>
        public static Material HordeMaterial()
        {
            if (s_hordeMat != null) return s_hordeMat;
            var tex = new Texture2D(8, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "HordePalette" };
            tex.SetPixels32(new Color32[]
            {
                new Color32(222, 36, 32, 255),   // 0 uniform red
                new Color32(130, 14, 16, 255),   // 1 helmet dark red
                new Color32(250, 200, 155, 255), // 2 skin
                new Color32(45, 45, 52, 255),    // 3 rifle
                new Color32(30, 22, 22, 255),    // 4 boots
                new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255)
            });
            tex.Apply(false);
            s_hordeMat = ArmyRunEnvironment.LitMat("M_HordePalette", Color.white, 0.2f);
            s_hordeMat.mainTexture = tex;
            return s_hordeMat;
        }

        /// <summary>A mesh of 2 rows x cols little red soldiers facing -Z (toward the player). Shared by every parade row.</summary>
        public static Mesh HordeRowMesh(int cols, float spacing, float height)
        {
            if (s_hordeRowMesh != null && s_rowCols == cols && Mathf.Approximately(s_rowSpacing, spacing) && Mathf.Approximately(s_rowHeight, height))
                return s_hordeRowMesh;

            var mb = new MeshBuild();
            uint seed = 7u;
            System.Func<float> rnd01 = () => { seed = seed * 1664525u + 1013904223u; return (seed >> 8) / 16777216f; };
            for (int row = 0; row < 2; row++)
                for (int c = 0; c < cols; c++)
                {
                    float x = (c - (cols - 1) * 0.5f) * spacing + (rnd01() - 0.5f) * 0.12f + (row == 1 ? spacing * 0.5f : 0f);
                    float z = row * spacing + (rnd01() - 0.5f) * 0.12f;
                    AddSoldier(mb, new Vector3(x, 0f, z), height / 0.98f);
                }
            s_hordeRowMesh = mb.ToMesh("HordeRow");
            s_rowCols = cols; s_rowSpacing = spacing; s_rowHeight = height;
            return s_hordeRowMesh;
        }

        /// <summary>Chibi soldier ~0.98 units tall at s = 1, facing -Z.</summary>
        private static void AddSoldier(MeshBuild mb, Vector3 o, float s)
        {
            mb.Box(o + new Vector3(-0.1f, 0.11f, 0f) * s, new Vector3(0.13f, 0.22f, 0.16f) * s, 4);   // legs/boots
            mb.Box(o + new Vector3(0.1f, 0.11f, 0f) * s, new Vector3(0.13f, 0.22f, 0.16f) * s, 4);
            mb.Box(o + new Vector3(0f, 0.38f, 0f) * s, new Vector3(0.42f, 0.34f, 0.28f) * s, 0);       // torso
            mb.Box(o + new Vector3(-0.25f, 0.4f, -0.08f) * s, new Vector3(0.1f, 0.26f, 0.12f) * s, 0); // arms
            mb.Box(o + new Vector3(0.25f, 0.4f, -0.08f) * s, new Vector3(0.1f, 0.26f, 0.12f) * s, 0);
            mb.Box(o + new Vector3(0.12f, 0.42f, -0.28f) * s, new Vector3(0.07f, 0.08f, 0.42f) * s, 3); // rifle
            mb.Sphere(o + new Vector3(0f, 0.72f, 0f) * s, 0.21f * s, 2, false);                       // head
            mb.Sphere(o + new Vector3(0f, 0.77f, 0.01f) * s, 0.235f * s, 1, true);                    // helmet
        }

        private class MeshBuild
        {
            private readonly List<Vector3> v = new List<Vector3>();
            private readonly List<Vector3> n = new List<Vector3>();
            private readonly List<Vector2> uv = new List<Vector2>();
            private readonly List<int> tri = new List<int>();

            private static Vector2 Pal(int i) => new Vector2((i + 0.5f) / 8f, 0.5f);

            public void Box(Vector3 c, Vector3 size, int pal)
            {
                Vector3 h = size * 0.5f;
                Vector3[] dirs = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
                foreach (var d in dirs)
                {
                    Vector3 a = d == Vector3.up || d == Vector3.down ? Vector3.forward : Vector3.up;
                    Vector3 bAx = Vector3.Cross(d, a);
                    Vector3 fc = c + Vector3.Scale(d, h);
                    Vector3 ea = Vector3.Scale(a, h), eb = Vector3.Scale(bAx, h);
                    int s = v.Count;
                    v.Add(fc - ea - eb); v.Add(fc - ea + eb); v.Add(fc + ea + eb); v.Add(fc + ea - eb);
                    for (int i = 0; i < 4; i++) { n.Add(d); uv.Add(Pal(pal)); }
                    tri.Add(s); tri.Add(s + 2); tri.Add(s + 1);
                    tri.Add(s); tri.Add(s + 3); tri.Add(s + 2);
                }
            }

            public void Sphere(Vector3 c, float r, int pal, bool topHalfOnly)
            {
                const int lon = 8, lat = 6;
                int start = v.Count;
                int latMax = topHalfOnly ? lat / 2 : lat;
                for (int y = 0; y <= latMax; y++)
                {
                    float phi = Mathf.PI * y / lat;
                    for (int x = 0; x <= lon; x++)
                    {
                        float th = 2f * Mathf.PI * x / lon;
                        Vector3 d = new Vector3(Mathf.Sin(phi) * Mathf.Cos(th), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(th));
                        v.Add(c + d * r); n.Add(d); uv.Add(Pal(pal));
                    }
                }
                for (int y = 0; y < latMax; y++)
                    for (int x = 0; x < lon; x++)
                    {
                        int a = start + y * (lon + 1) + x, b = a + lon + 1;
                        tri.Add(a); tri.Add(a + 1); tri.Add(b);
                        tri.Add(a + 1); tri.Add(b + 1); tri.Add(b);
                    }
            }

            public Mesh ToMesh(string name)
            {
                var m = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(tri, 0);
                m.RecalculateBounds();
                return m;
            }
        }
    }

    /// <summary>Plays one AnimationClip forever on the model's Animator (no AnimatorController needed).</summary>
    public class ArmyClipLooper : MonoBehaviour
    {
        public AnimationClip clip;
        public float speed = 1f;
        private PlayableGraph graph;
        private AnimationClipPlayable playable;

        private void OnEnable()
        {
            var anim = GetComponentInChildren<Animator>();
            if (anim == null || clip == null) return;
            graph = PlayableGraph.Create("ArmyClipLooper");
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            var output = AnimationPlayableOutput.Create(graph, "Anim", anim);
            playable = AnimationClipPlayable.Create(graph, clip);
            playable.SetSpeed(speed * Random.Range(0.92f, 1.1f));
            playable.SetTime(Random.value * clip.length);
            output.SetSourcePlayable(playable);
            graph.Play();
        }

        private void Update()
        {
            if (!graph.IsValid() || clip == null || clip.length <= 0f) return;
            double t = playable.GetTime();
            if (t >= clip.length) playable.SetTime(t % clip.length);
        }

        private void OnDisable()
        {
            if (graph.IsValid()) graph.Destroy();
        }
    }

    /// <summary>Bouncy march for static (un-rigged) models.</summary>
    public class ArmyMarchBob : MonoBehaviour
    {
        private Vector3 basePos;
        private Quaternion baseRot;
        private float phase;

        private void OnEnable()
        {
            basePos = transform.localPosition;
            baseRot = transform.localRotation;
            phase = Random.value * 10f;
        }

        private void Update()
        {
            float t = Time.time * 10f + phase;
            transform.localPosition = basePos + Vector3.up * (Mathf.Abs(Mathf.Sin(t)) * 0.06f);
            transform.localRotation = baseRot * Quaternion.Euler(0f, 0f, Mathf.Sin(t) * 5f);
        }

        private void OnDisable()
        {
            transform.localPosition = basePos;
            transform.localRotation = baseRot;
        }
    }
}
