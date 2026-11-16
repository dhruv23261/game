using UnityEngine;

namespace MobControl.Level
{
    /// <summary>
    /// All particle effects for the Army Run level.
    ///
    /// One ParticleSystem per effect TYPE lives in world space; effects are fired with Emit() at any
    /// position, so 200 explosions cost the same as one system — no prefabs, no GameObject spawning.
    ///
    ///   Spark   – additive embers (bullet hits, gate sparkle)
    ///   Flash   – additive quick glow (muzzle flash, impact flash)
    ///   Fire    – additive flames (explosions)
    ///   Smoke   – soft alpha puffs (explosions, deaths, dust)
    ///   Chunk   – tumbling little cubes (wood, ice, debris)
    ///   Ring    – flat expanding shockwave on the ground
    ///   Star    – gold sparkles (rewards, coins)
    /// </summary>
    public class ArmyRunFX
    {
        private ParticleSystem spark, flash, fire, smoke, chunk, ring, star;
        private Transform root;

        private static Texture2D s_soft, s_ring;
        private static Material s_add, s_addFire, s_alpha, s_alphaSoft, s_chunkMat, s_ringMat, s_shadowMat;
        private static Mesh s_shadowQuad;

        public void Build(Transform parent)
        {
            root = new GameObject("ArmyRun_FX").transform;
            root.SetParent(parent, false);
            EnsureMaterials();

            spark = Make("Spark", s_add, 400, 0f, ParticleSystemRenderMode.Stretch, true);
            flash = Make("Flash", s_add, 150, 0f, ParticleSystemRenderMode.Billboard, false);
            fire = Make("Fire", s_addFire, 300, -0.4f, ParticleSystemRenderMode.Billboard, false);
            smoke = Make("Smoke", s_alpha, 300, -0.15f, ParticleSystemRenderMode.Billboard, false);
            chunk = Make("Chunk", s_chunkMat, 300, 1.6f, ParticleSystemRenderMode.Mesh, false);
            ring = Make("Ring", s_ringMat, 30, 0f, ParticleSystemRenderMode.HorizontalBillboard, false);
            star = Make("Star", s_add, 200, 0.6f, ParticleSystemRenderMode.Billboard, false);

            // per-type looks
            var cubeMesh = BuiltinCube();
            var chunkR = chunk.GetComponent<ParticleSystemRenderer>();
            chunkR.mesh = cubeMesh;
            var rot = chunk.rotationOverLifetime; rot.enabled = true; rot.separateAxes = true;
            rot.x = new ParticleSystem.MinMaxCurve(-8f, 8f); rot.y = new ParticleSystem.MinMaxCurve(-8f, 8f); rot.z = new ParticleSystem.MinMaxCurve(-8f, 8f);
            chunk.GetComponent<ParticleSystemRenderer>().alignment = ParticleSystemRenderSpace.World;

            SizeCurve(smoke, 0.5f, 1.6f);
            SizeCurve(fire, 1f, 0.3f);
            SizeCurve(flash, 1f, 1.6f);
            SizeCurve(ring, 0.1f, 1f);
            SizeCurve(spark, 1f, 0.2f);
            SizeCurve(star, 1f, 0f);
            SizeCurve(chunk, 1f, 0.4f);
            var sr = spark.GetComponent<ParticleSystemRenderer>();
            sr.velocityScale = 0.04f; sr.lengthScale = 1.2f;
        }

        public void Dispose()
        {
            if (root != null) Object.Destroy(root.gameObject);
            root = null;
        }

        // ═════════════════════════════════════ Public effects ══

        public void MuzzleFlash(Vector3 p)
        {
            Emit(flash, p, Vector3.zero, Random.Range(0.35f, 0.55f), 0.06f, new Color(1f, 0.85f, 0.4f, 1f));
        }

        public void BulletHit(Vector3 p, Color tint)
        {
            Emit(flash, p, Vector3.zero, 0.7f, 0.08f, new Color(1f, 0.95f, 0.7f, 0.9f));
            for (int i = 0; i < 4; i++)
                Emit(spark, p, RandomCone(Vector3.back, 0.9f) * Random.Range(4f, 8f), Random.Range(0.08f, 0.14f), Random.Range(0.15f, 0.3f), Color.Lerp(tint, Color.white, 0.4f));
        }

        /// <summary>Red soldier dies: red puff + sparks + small smoke.</summary>
        public void EnemyDeath(Vector3 p)
        {
            for (int i = 0; i < 6; i++)
                Emit(smoke, p + Random.insideUnitSphere * 0.2f, Random.insideUnitSphere * 1.2f + Vector3.up, Random.Range(0.5f, 0.8f), Random.Range(0.35f, 0.6f), new Color(1f, 0.35f, 0.3f, 0.75f));
            for (int i = 0; i < 6; i++)
                Emit(spark, p, Random.onUnitSphere * Random.Range(3f, 6f) + Vector3.up * 2f, 0.1f, 0.3f, new Color(1f, 0.6f, 0.3f));
            Emit(flash, p, Vector3.zero, 1.1f, 0.1f, new Color(1f, 0.5f, 0.4f, 0.8f));
        }

        /// <summary>One of our soldiers dies: blue puff.</summary>
        public void AllyDeath(Vector3 p)
        {
            for (int i = 0; i < 5; i++)
                Emit(smoke, p + Random.insideUnitSphere * 0.2f, Random.insideUnitSphere + Vector3.up, Random.Range(0.45f, 0.7f), 0.45f, new Color(0.45f, 0.7f, 1f, 0.75f));
            Emit(flash, p, Vector3.zero, 0.9f, 0.1f, new Color(0.5f, 0.75f, 1f, 0.8f));
        }

        public void Chunks(Vector3 p, Color c, int count, float force)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 v = Random.insideUnitSphere * force; v.y = Mathf.Abs(v.y) + force * 0.7f;
                Emit(chunk, p + Random.insideUnitSphere * 0.4f, v, Random.Range(0.12f, 0.3f), Random.Range(0.7f, 1.1f), c);
            }
        }

        /// <summary>Fireball + smoke + shockwave ring + flash.</summary>
        public void Explosion(Vector3 p, float scale)
        {
            Emit(flash, p + Vector3.up * 0.5f, Vector3.zero, 4f * scale, 0.18f, new Color(1f, 0.8f, 0.5f, 1f));
            for (int i = 0; i < (int)(14 * scale); i++)
                Emit(fire, p + Random.insideUnitSphere * 0.6f * scale + Vector3.up * 0.4f, Random.insideUnitSphere * 3f * scale + Vector3.up * 2.5f,
                    Random.Range(1.0f, 1.8f) * scale, Random.Range(0.35f, 0.6f), new Color(1f, Random.Range(0.45f, 0.75f), 0.2f, 1f));
            for (int i = 0; i < (int)(10 * scale); i++)
                Emit(smoke, p + Random.insideUnitSphere * 0.8f * scale + Vector3.up, Random.insideUnitSphere * 1.5f + Vector3.up * 2f,
                    Random.Range(1.4f, 2.4f) * scale, Random.Range(0.9f, 1.5f), new Color(0.25f, 0.24f, 0.24f, 0.7f));
            for (int i = 0; i < (int)(12 * scale); i++)
                Emit(spark, p + Vector3.up * 0.5f, Random.onUnitSphere * Random.Range(6f, 12f) * scale + Vector3.up * 3f, 0.15f, Random.Range(0.3f, 0.6f), new Color(1f, 0.75f, 0.3f));
            Shockwave(p, 7f * scale, new Color(1f, 0.85f, 0.6f, 0.9f));
        }

        public void Shockwave(Vector3 p, float size, Color c)
        {
            Emit(ring, new Vector3(p.x, 0.08f, p.z), Vector3.zero, size, 0.45f, c);
        }

        public void Dust(Vector3 p, float size)
        {
            for (int i = 0; i < 14; i++)
            {
                Vector3 d = Random.insideUnitCircle.normalized;
                Vector3 v = new Vector3(d.x, 0.15f, d.y) * Random.Range(3f, 7f) * size;
                Emit(smoke, new Vector3(p.x, 0.3f, p.z) + v * 0.1f, v, Random.Range(1.2f, 2f) * size, Random.Range(0.6f, 1f), new Color(0.82f, 0.78f, 0.7f, 0.65f));
            }
        }

        public void Sparkle(Vector3 p, Color c, int count, float spread)
        {
            for (int i = 0; i < count; i++)
                Emit(star, p + Random.insideUnitSphere * spread, Random.insideUnitSphere * 2f + Vector3.up * 2.5f, Random.Range(0.18f, 0.35f), Random.Range(0.4f, 0.8f), c);
        }

        // ═════════════════════════════════════ Blob shadows ══

        /// <summary>Soft round shadow on the road under a character / prop (much cheaper than real shadows).</summary>
        public static GameObject AddBlobShadow(Transform parent, float diameter)
        {
            EnsureMaterials();
            var go = new GameObject("BlobShadow");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Vector3 ps = parent.lossyScale;
            float inv = 1f / Mathf.Max(0.001f, ps.x);
            go.transform.localScale = new Vector3(diameter * inv, diameter * inv, 1f);
            go.AddComponent<MeshFilter>().sharedMesh = s_shadowQuad;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = s_shadowMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go;
        }

        // ═════════════════════════════════════ internals ══

        private static void Emit(ParticleSystem ps, Vector3 pos, Vector3 vel, float size, float life, Color c)
        {
            if (ps == null) return;
            var e = new ParticleSystem.EmitParams
            {
                position = pos,
                velocity = vel,
                startSize = size,
                startLifetime = life,
                startColor = c,
                rotation = Random.Range(0f, 360f),
                applyShapeToPosition = false
            };
            ps.Emit(e, 1);
        }

        private static Vector3 RandomCone(Vector3 dir, float spread)
        {
            return (dir + Random.insideUnitSphere * spread).normalized;
        }

        private ParticleSystem Make(string name, Material mat, int max, float gravity, ParticleSystemRenderMode mode, bool stretch)
        {
            var go = new GameObject("FX_" + name);
            go.transform.SetParent(root, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;            // keeps the system "playing" so Emit()ed particles always simulate
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = max;
            main.gravityModifier = gravity;
            main.startSpeed = 0f;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            var em = ps.emission; em.enabled = false;
            var sh = ps.shape; sh.enabled = false;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.85f, 0.5f), new GradientAlphaKey(0f, 1f) });
            col.color = g;

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.renderMode = mode;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.sortMode = ParticleSystemSortMode.None;
            ps.Play();
            return ps;
        }

        private static void SizeCurve(ParticleSystem ps, float from, float to)
        {
            var s = ps.sizeOverLifetime;
            s.enabled = true;
            s.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, from, 1f, to));
        }

        private static Mesh BuiltinCube()
        {
            var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Mesh m = tmp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(tmp);
            return m;
        }

        private static void EnsureMaterials()
        {
            if (s_add != null) return;
            s_soft = RadialTexture(64, 2.2f, false);
            s_ring = RadialTexture(128, 1f, true);

            Shader additive = Shader.Find("ZombieSquad/VFX_FireAdditive") ?? Shader.Find("Mobile/Particles/Additive") ?? Shader.Find("Sprites/Default");
            Shader alpha = Shader.Find("Sprites/Default");

            Texture spark = Resources.Load<Texture2D>("Textures/VFX/tex_spark_ember");
            Texture flame = Resources.Load<Texture2D>("Textures/VFX/tex_fire_flame");
            Texture smokeTex = Resources.Load<Texture2D>("Textures/VFX/tex_smoke_soft");

            s_add = AddMat(additive, spark != null ? spark : s_soft, 1.4f);
            s_addFire = AddMat(additive, flame != null ? flame : s_soft, 1.5f);
            s_alpha = new Material(alpha) { name = "FX_Smoke", mainTexture = smokeTex != null ? smokeTex : s_soft };
            s_alphaSoft = new Material(alpha) { name = "FX_Soft", mainTexture = s_soft };
            s_chunkMat = new Material(alpha) { name = "FX_Chunk" };
            s_ringMat = AddMat(additive, s_ring, 1.2f);

            s_shadowMat = new Material(alpha) { name = "BlobShadow", mainTexture = s_soft, color = new Color(0f, 0f, 0f, 0.38f) };
            s_shadowMat.renderQueue = 2900;
            s_shadowQuad = Quad();
        }

        private static Material AddMat(Shader sh, Texture tex, float glow)
        {
            var m = new Material(sh) { name = "FX_Add", mainTexture = tex };
            if (m.HasProperty("_TintColor")) m.SetColor("_TintColor", Color.white);
            if (m.HasProperty("_GlowIntensity")) m.SetFloat("_GlowIntensity", glow);
            return m;
        }

        /// <summary>Soft white disc (or a ring) with alpha falloff.</summary>
        private static Texture2D RadialTexture(int n, float power, bool ringShape)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, name = ringShape ? "FX_Ring" : "FX_Soft" };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = ringShape
                        ? Mathf.Clamp01(1f - Mathf.Abs(d - 0.82f) / 0.14f) * (d < 1f ? 1f : 0f)
                        : Mathf.Pow(Mathf.Clamp01(1f - d), power);
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            t.SetPixels32(px);
            t.Apply(true);
            return t;
        }

        private static Mesh Quad()
        {
            var m = new Mesh { name = "FX_Quad" };
            m.vertices = new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(0.5f, 0.5f, 0), new Vector3(-0.5f, 0.5f, 0) };
            m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            m.colors32 = new[] { new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255) };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }
    }
}
