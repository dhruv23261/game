using System.IO;
using UnityEngine;

namespace MobControl.Level
{
    /// <summary>
    /// Constructs authentic AAA-quality wrecked, charred, and realistically burning automobiles
    /// with multi-layered fire, billowing volumetric smoke, spark embers, dynamic lighting,
    /// and scorched metal materials.
    /// </summary>
    public static class BurnedCarBuilder
    {
        private static GameObject cachedPrefab = null;
        private static Mesh cachedCarMesh = null;
        private static Material cachedCarMat = null;

        private static Material smokeParticleMat = null;
        private static Material fireParticleMat = null;
        private static Material sparkParticleMat = null;
        private static Material sootDecalMat = null;

        public static GameObject CreateBurnedCar(Transform parent, Vector3 localPos, Quaternion localRot, Material fallbackMat, Material rustMat, Material tireMat)
        {
            // 1. Try loading pre-built AAA Wrecked Car Prefab
            if (cachedPrefab == null)
            {
                cachedPrefab = Resources.Load<GameObject>("Prefabs/WreckedCar_AAA");
            }

            if (cachedPrefab != null)
            {
                GameObject instance = Object.Instantiate(cachedPrefab, parent);
                instance.name = "Obstacle_BurnedCar_AAA";
                instance.transform.localPosition = localPos;
                instance.transform.localRotation = localRot;
                return instance;
            }

            // 2. Build dynamically with AAA Wrecked Car Mesh & Scorched Material
            GameObject carRoot = new GameObject("Obstacle_BurnedCar_AAA");
            carRoot.transform.SetParent(parent, false);
            carRoot.transform.localPosition = localPos;
            carRoot.transform.localRotation = localRot;

            EnsureMeshAndMaterial();

            if (cachedCarMesh != null)
            {
                GameObject vis = new GameObject("Visual_Mesh");
                vis.transform.SetParent(carRoot.transform, false);
                vis.transform.localPosition = new Vector3(0f, 0.67f, 0f);
                vis.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                vis.transform.localScale = Vector3.one * 2.1f;

                MeshFilter mf = vis.AddComponent<MeshFilter>();
                mf.sharedMesh = cachedCarMesh;
                MeshRenderer mr = vis.AddComponent<MeshRenderer>();
                mr.sharedMaterial = cachedCarMat != null ? cachedCarMat : fallbackMat;
            }
            else
            {
                // Procedural fallback if mesh missing
                GameObject chassis = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chassis.name = "Chassis";
                chassis.transform.SetParent(carRoot.transform, false);
                chassis.transform.localPosition = new Vector3(0f, 0.48f, 0f);
                chassis.transform.localScale = new Vector3(2.05f, 0.52f, 3.75f);
                SafeDestroy(chassis.GetComponent<Collider>());
                if (fallbackMat != null) chassis.GetComponent<Renderer>().sharedMaterial = fallbackMat;
            }

            // 3. Realistic Scorched Engine Bay (replaces the cartoon yellow box)
            AttachScorchedEngineBay(carRoot.transform);

            // 4. Multi-Layered Realistic Fire, Volumetric Billowing Smoke & Dynamic Lighting
            AttachFireAndSmokeVFX(carRoot.transform);

            // 5. Solid Physics & Collision
            BoxCollider col = carRoot.AddComponent<BoxCollider>();
            col.isTrigger = false;
            col.center = new Vector3(0f, 0.70f, 0f);
            col.size = new Vector3(1.85f, 1.40f, 4.0f);

            Rigidbody rb = carRoot.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            RoadObstacle obs = carRoot.AddComponent<RoadObstacle>();
            obs.Initialize(isContainer: false, damage: 1);

            return carRoot;
        }

        private static void AttachScorchedEngineBay(Transform carRoot)
        {
            GameObject engineBay = new GameObject("EngineBay");
            engineBay.transform.SetParent(carRoot, false);
            engineBay.transform.localPosition = new Vector3(0.0f, 0.74f, -1.25f);

            // Blackened charred engine block
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = "BurnedEngineBlock";
            block.transform.SetParent(engineBay.transform, false);
            block.transform.localPosition = new Vector3(0f, -0.04f, 0f);
            block.transform.localScale = new Vector3(0.68f, 0.22f, 0.52f);
            SafeDestroy(block.GetComponent<Collider>());
            Material blockMat = new Material(Shader.Find("Standard") ?? Shader.Find("Mobile/Diffuse"));
            blockMat.color = new Color(0.09f, 0.08f, 0.08f);
            blockMat.SetFloat("_Glossiness", 0.05f);
            block.GetComponent<Renderer>().sharedMaterial = blockMat;

            // Deep internal molten ember bed (recessed inside the engine crevices)
            GameObject emberBed = GameObject.CreatePrimitive(PrimitiveType.Cube);
            emberBed.name = "MoltenEmberBed";
            emberBed.transform.SetParent(engineBay.transform, false);
            emberBed.transform.localPosition = new Vector3(0f, -0.09f, 0f);
            emberBed.transform.localScale = new Vector3(0.48f, 0.06f, 0.36f);
            SafeDestroy(emberBed.GetComponent<Collider>());
            Material emberMat = new Material(Shader.Find("Standard") ?? Shader.Find("Mobile/Diffuse"));
            emberMat.color = new Color(0.85f, 0.22f, 0.02f);
            emberMat.EnableKeyword("_EMISSION");
            emberMat.SetColor("_EmissionColor", new Color(2.4f, 0.45f, 0.05f));
            emberBed.GetComponent<Renderer>().sharedMaterial = emberMat;

            // Charred soot burn decal overlay across the open hood
            GameObject soot = GameObject.CreatePrimitive(PrimitiveType.Quad);
            soot.name = "BurntHoodSoot";
            soot.transform.SetParent(engineBay.transform, false);
            soot.transform.localPosition = new Vector3(0f, 0.11f, 0f);
            soot.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            soot.transform.localScale = new Vector3(1.40f, 1.15f, 1f);
            SafeDestroy(soot.GetComponent<Collider>());
            Material sMat = GetSootDecalMaterial();
            if (sMat != null) soot.GetComponent<Renderer>().sharedMaterial = sMat;
        }

        /// <summary>Fire, smoke, sparks and flickering light on any wreck (used by level 2's helicopter crash).</summary>
        public static void AttachWreckFire(Transform root, Vector3 localPos, float scale)
        {
            AttachFireAndSmokeVFX(root);
            Transform vfx = root.Find("VFX_FireAndSmoke");
            if (vfx == null) return;
            vfx.localPosition = localPos;
            // Root may be scaled; keep the effect at a consistent world size
            float s = Mathf.Max(0.001f, root.lossyScale.x);
            vfx.localScale = Vector3.one * (scale / s);
        }

        private static void AttachFireAndSmokeVFX(Transform carRoot)
        {
            GameObject vfxRoot = new GameObject("VFX_FireAndSmoke");
            vfxRoot.transform.SetParent(carRoot, false);
            vfxRoot.transform.localPosition = new Vector3(0.0f, 0.85f, -1.25f);

            // 1. REALISTIC FIRE FLAMES (Additive ParticleSystem)
            CreateFireFlameSystem(vfxRoot.transform);

            // 2. REALISTIC VOLUMETRIC BILLOWING SMOKE (Alpha Blended ParticleSystem)
            CreateBillowingSmokeSystem(vfxRoot.transform);

            // 3. POPPING SPARKS & FLYING EMBERS
            CreateSparksSystem(vfxRoot.transform);

            // 4. REAL-TIME DYNAMIC FIRE LIGHT FLICKER
            GameObject lightObj = new GameObject("FireLight");
            lightObj.transform.SetParent(vfxRoot.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            Light lgt = lightObj.AddComponent<Light>();
            lgt.type = LightType.Point;
            lgt.color = new Color(1.0f, 0.46f, 0.10f);
            lgt.range = 4.8f;
            lgt.intensity = 2.4f;
            lightObj.AddComponent<BurnedCarFireLight>();
        }

        private static void CreateFireFlameSystem(Transform parent)
        {
            GameObject fireObj = new GameObject("FireFlames_PS");
            fireObj.transform.SetParent(parent, false);
            fireObj.transform.localPosition = Vector3.zero;

            ParticleSystem ps = fireObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.38f, 0.65f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.8f, 3.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.40f, 0.70f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = true;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.rateOverTime = 32f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(0.55f, 0.10f, 0.38f);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 0.85f);
            curve.AddKey(0.35f, 1.0f);
            curve.AddKey(1f, 0.15f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            var rotOverLifetime = ps.rotationOverLifetime;
            rotOverLifetime.enabled = true;
            rotOverLifetime.z = new ParticleSystem.MinMaxCurve(-1.2f, 1.2f);

            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f, 0.95f, 0.65f), 0f),
                    new GradientColorKey(new Color(1f, 0.48f, 0.05f), 0.35f),
                    new GradientColorKey(new Color(0.85f, 0.14f, 0.02f), 0.75f),
                    new GradientColorKey(new Color(0.20f, 0.04f, 0.01f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.85f, 0f),
                    new GradientAlphaKey(1.0f, 0.20f),
                    new GradientAlphaKey(0.80f, 0.70f),
                    new GradientAlphaKey(0.0f, 1f)
                }
            );
            col.color = grad;

            var renderer = fireObj.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = GetFireMaterial();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 5;

            ps.Play();
        }

        private static void CreateBillowingSmokeSystem(Transform parent)
        {
            GameObject smokeObj = new GameObject("SmokePlume_PS");
            smokeObj.transform.SetParent(parent, false);
            smokeObj.transform.localPosition = new Vector3(0f, 0.15f, 0f);

            ParticleSystem ps = smokeObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.2f, 3.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.1f, 1.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.45f, 0.70f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = true;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.rateOverTime = 14f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 14f;
            shape.radius = 0.22f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve smokeCurve = new AnimationCurve();
            smokeCurve.AddKey(0f, 0.45f);
            smokeCurve.AddKey(0.40f, 1.25f);
            smokeCurve.AddKey(1f, 2.50f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, smokeCurve);

            var rotOverLifetime = ps.rotationOverLifetime;
            rotOverLifetime.enabled = true;
            rotOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);

            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient smokeGrad = new Gradient();
            smokeGrad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(0.11f, 0.10f, 0.10f), 0f),
                    new GradientColorKey(new Color(0.18f, 0.17f, 0.17f), 0.45f),
                    new GradientColorKey(new Color(0.28f, 0.26f, 0.26f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.72f, 0f),
                    new GradientAlphaKey(0.58f, 0.25f),
                    new GradientAlphaKey(0.24f, 0.75f),
                    new GradientAlphaKey(0.0f, 1f)
                }
            );
            col.color = smokeGrad;

            var renderer = smokeObj.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = GetSmokeMaterial();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 4;

            ps.Play();
        }

        private static void CreateSparksSystem(Transform parent)
        {
            GameObject sparkObj = new GameObject("Sparks_PS");
            sparkObj.transform.SetParent(parent, false);
            sparkObj.transform.localPosition = new Vector3(0f, 0.05f, 0f);

            ParticleSystem ps = sparkObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.75f, 1.35f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.4f, 4.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = true;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.rateOverTime = 10f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 18f;
            shape.radius = 0.20f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient sparkGrad = new Gradient();
            sparkGrad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f, 0.90f, 0.4f), 0f),
                    new GradientColorKey(new Color(1f, 0.55f, 0.1f), 0.7f),
                    new GradientColorKey(new Color(0.8f, 0.15f, 0.02f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1.0f, 0f),
                    new GradientAlphaKey(0.9f, 0.7f),
                    new GradientAlphaKey(0.0f, 1f)
                }
            );
            col.color = sparkGrad;

            var renderer = sparkObj.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = GetSparkMaterial();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 6;

            ps.Play();
        }

        private static Material GetSmokeMaterial()
        {
            if (smokeParticleMat == null)
            {
                Shader shader = Shader.Find("ZombieSquad/VFX_SmokeAlpha")
                             ?? Shader.Find("Sprites/Default")
                             ?? Shader.Find("Mobile/Particles/Alpha Blended");
                smokeParticleMat = new Material(shader);
                smokeParticleMat.name = "M_BurnedCar_SmokeVFX";
                smokeParticleMat.color = new Color(0.85f, 0.85f, 0.85f, 1f);
                Texture2D tex = LoadVfxTexture("Textures/VFX/tex_smoke_soft");
                if (tex != null) smokeParticleMat.mainTexture = tex;
            }
            return smokeParticleMat;
        }

        private static Material GetFireMaterial()
        {
            if (fireParticleMat == null)
            {
                Shader shader = Shader.Find("ZombieSquad/VFX_FireAdditive")
                             ?? Shader.Find("Sprites/Default")
                             ?? Shader.Find("Mobile/Particles/Additive");
                fireParticleMat = new Material(shader);
                fireParticleMat.name = "M_BurnedCar_FireVFX";
                Texture2D tex = LoadVfxTexture("Textures/VFX/tex_fire_flame");
                if (tex != null) fireParticleMat.mainTexture = tex;
                if (fireParticleMat.HasProperty("_GlowIntensity")) fireParticleMat.SetFloat("_GlowIntensity", 2.2f);
            }
            return fireParticleMat;
        }

        private static Material GetSparkMaterial()
        {
            if (sparkParticleMat == null)
            {
                Shader shader = Shader.Find("ZombieSquad/VFX_FireAdditive")
                             ?? Shader.Find("Sprites/Default")
                             ?? Shader.Find("Mobile/Particles/Additive");
                sparkParticleMat = new Material(shader);
                sparkParticleMat.name = "M_BurnedCar_SparksVFX";
                Texture2D tex = LoadVfxTexture("Textures/VFX/tex_spark_ember");
                if (tex != null) sparkParticleMat.mainTexture = tex;
                if (sparkParticleMat.HasProperty("_GlowIntensity")) sparkParticleMat.SetFloat("_GlowIntensity", 2.8f);
            }
            return sparkParticleMat;
        }

        private static Material GetSootDecalMaterial()
        {
            if (sootDecalMat == null)
            {
                Shader shader = Shader.Find("ZombieSquad/VFX_SmokeAlpha")
                             ?? Shader.Find("Sprites/Default")
                             ?? Shader.Find("Mobile/Particles/Alpha Blended");
                sootDecalMat = new Material(shader);
                sootDecalMat.name = "M_BurntHoodSoot";
                Texture2D tex = LoadVfxTexture("Textures/VFX/tex_burnt_hood_soot");
                if (tex != null) sootDecalMat.mainTexture = tex;
            }
            return sootDecalMat;
        }

        private static Texture2D LoadVfxTexture(string relPath)
        {
            Texture2D tex = Resources.Load<Texture2D>(relPath);
#if UNITY_EDITOR && !UNITY_ANDROID
            if (tex == null)
            {
                string fullPath = Path.Combine(Application.dataPath, "Resources", relPath + ".png");
                if (File.Exists(fullPath))
                {
                    byte[] bytes = File.ReadAllBytes(fullPath);
                    tex = new Texture2D(2, 2, TextureFormat.RGBA32, true);
                    tex.LoadImage(bytes);
                }
            }
#endif
            return tex;
        }

        private static void EnsureMeshAndMaterial()
        {
            if (cachedCarMesh == null)
            {
                GameObject objModel = Resources.Load<GameObject>("Models/WreckedCar");
                if (objModel != null) cachedCarMesh = objModel.GetComponentInChildren<MeshFilter>()?.sharedMesh;

                if (cachedCarMesh == null)
                {
                    cachedCarMesh = Resources.Load<Mesh>("Models/WreckedCar");
                }

#if UNITY_EDITOR
                if (cachedCarMesh == null)
                {
                    string objPath = Path.Combine(Application.dataPath, "Resources/Models/WreckedCar.obj");
                    if (File.Exists(objPath)) cachedCarMesh = NewRoadBuilder.LoadObjMeshDirectPublic(objPath);
                }
#endif
            }

            if (cachedCarMat == null)
            {
                Material baseMat = Resources.Load<Material>("Materials/M_WreckedCar_AAA");
                if (baseMat != null)
                {
                    // Create an authentic charred, smoke-scorched material instance
                    cachedCarMat = new Material(baseMat);
                    cachedCarMat.name = "M_WreckedCar_Charred";
                    // Heavy soot and fire damage tint
                    cachedCarMat.color = new Color(0.38f, 0.34f, 0.32f, 1f);
                    if (cachedCarMat.HasProperty("_Glossiness")) cachedCarMat.SetFloat("_Glossiness", 0.12f);
                }
                else
                {
                    cachedCarMat = NewRoadBuilder.LoadTextureMaterialPublic("Textures/Car/WreckedCar_base_color.png", "M_WreckedCar_AAA");
                    if (cachedCarMat != null) cachedCarMat.color = new Color(0.38f, 0.34f, 0.32f, 1f);
                }
            }
        }

        private static void SafeDestroy(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Object.Destroy(obj);
            else Object.DestroyImmediate(obj);
        }
    }

    /// <summary>
    /// Realistic fire light flicker using smoothed multi-frequency noise.
    /// </summary>
    public class BurnedCarFireLight : MonoBehaviour
    {
        private Light fireLight;
        private float baseIntensity = 2.4f;
        private float seed;

        private void Awake()
        {
            fireLight = GetComponent<Light>();
            seed = Random.Range(0f, 1000f);
        }

        private void Update()
        {
            if (fireLight == null) return;
            float time = Time.time;
            float n1 = Mathf.PerlinNoise(seed, time * 8.5f);
            float n2 = Mathf.PerlinNoise(seed + 42f, time * 18.0f);
            float flicker = (n1 * 0.7f + n2 * 0.3f);
            fireLight.intensity = baseIntensity * (0.65f + flicker * 0.70f);
        }
    }

    [System.Obsolete("BurnedCarSmoke has been upgraded to ParticleSystem VFX")]
    public class BurnedCarSmoke : MonoBehaviour {}
}
