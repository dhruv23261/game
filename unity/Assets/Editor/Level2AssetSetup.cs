#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using MobControl.Creatures;
using MobControl.UI;

namespace MobControl.EditorTools
{
    /// <summary>
    /// Builds every Level 2 ("Overgrown Highway") asset from the Meshy models:
    ///   • Resources/Prefabs/Level2/L2_*.prefab   — helicopter / car / bus / pickup wrecks (visual + box collider)
    ///   • Resources/Prefabs/PlantMonster_Prefab  — dormant pod that erupts into the Meshy flytrap monster
    ///   • Resources/Prefabs/WyvernDragon_Prefab  — flying dragon with its textured material
    ///   • Resources/Materials/Level2/*           — PBR materials, spore VFX material, sunset skybox
    ///   • Resources/Prefabs/Zombie_Plant         — rebuilt via PlantZombieSetup
    /// Runs automatically once per SETUP_VERSION (tracked in Library/), or from the menu.
    /// </summary>
    [InitializeOnLoad]
    public static class Level2AssetSetup
    {
        private const int SETUP_VERSION = 5;

        private const string PROPS_DIR = "Assets/Models/Generated/Level2Props";
        private const string PROP_PREFAB_DIR = "Assets/Resources/Prefabs/Level2";
        private const string MAT_DIR = "Assets/Resources/Materials/Level2";

        private const string FLYTRAP_DIR = "Assets/Models/Generated/PlantMonster";
        private const string DRAGON_DIR = "Assets/Models/Generated/WyvernDragon";

        private const string PLANT_MONSTER_PREFAB = "Assets/Resources/Prefabs/PlantMonster_Prefab.prefab";
        private const string DRAGON_PREFAB = "Assets/Resources/Prefabs/WyvernDragon_Prefab.prefab";

        /// <summary>Flytrap standing height (m) once fully erupted. Player soldier is ~1.3m.</summary>
        private const float FLYTRAP_HEIGHT = 2.3f;
        /// <summary>Meshy models face +Z; enemies must face the camera (-Z).</summary>
        private const float FACE_PLAYER_YAW = 180f;

        // name, target length along the road (m)
        private static readonly (string name, float length)[] Props =
        {
            ("HeliWreck", 10.5f),
            ("CarWreck", 4.1f),
            ("PickupWreck", 4.6f),
            ("BusWreck", 7.6f),
        };

        private static string VersionFile => Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/Level2AssetsVersion.txt"));

        static Level2AssetSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (IsUpToDate()) return;
                SetupAllLevel2Assets();
            };
        }

        private static bool IsUpToDate()
        {
            try { return File.Exists(VersionFile) && File.ReadAllText(VersionFile).Trim() == SETUP_VERSION.ToString(); }
            catch { return false; }
        }

        [MenuItem("Zombie Squad/Level 2/Setup Materials and Prefabs")]
        public static void SetupAllLevel2Assets()
        {
            if (!Directory.Exists(PROPS_DIR) && !Directory.Exists(FLYTRAP_DIR)) return;

            EnsureFolder(PROP_PREFAB_DIR);
            EnsureFolder(MAT_DIR);

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var p in Props) ConfigureModelAndTextures($"{PROPS_DIR}/{p.name}", $"{p.name}.fbx");
                ConfigureModelAndTextures(FLYTRAP_DIR, "PlantMonster.fbx");
                ConfigureModelAndTextures(DRAGON_DIR, "WyvernDragon.fbx");
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            AssetDatabase.Refresh();

            foreach (var p in Props)
            {
                var mat = MakePbrMaterial($"{MAT_DIR}/M_L2_{p.name}.mat", $"{PROPS_DIR}/{p.name}/textures", 0.12f, 0.25f, Color.white);
                BuildPropPrefab(p.name, p.length, mat);
            }

            Material flytrapMat = MakePbrMaterial("Assets/Resources/Materials/M_PlantMonster.mat", $"{FLYTRAP_DIR}/textures", 0.30f, 0.45f, Color.white);
            Material dragonMat = MakePbrMaterial("Assets/Resources/Materials/M_WyvernDragon.mat", $"{DRAGON_DIR}/textures", 0.22f, 0.4f, Color.white);

            BuildSporeMaterial();
            BuildSunsetSkybox();
            BuildPlantMonsterPrefab(flytrapMat);
            BuildWyvernPrefab(dragonMat);

            AssetDatabase.SaveAssets();

            // Walking plant zombie (Meshy rigged runner) with the upgraded material
            PlantZombieSetup.Build();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            try { File.WriteAllText(VersionFile, SETUP_VERSION.ToString()); } catch { }
            Debug.Log("<color=#00FF66><b>[Level 2 Setup] Wrecks, flytrap monster, wyvern, plant zombie and sunset sky are ready.</b></color>");
        }

        // ─────────────────────────────────────────────────────────── import settings

        private static void ConfigureModelAndTextures(string dir, string fbxName)
        {
            string fbx = $"{dir}/{fbxName}";
            if (AssetImporter.GetAtPath(fbx) is ModelImporter mi)
            {
                bool dirty = false;
                if (mi.importCameras) { mi.importCameras = false; dirty = true; }
                if (mi.importLights) { mi.importLights = false; dirty = true; }
                if (mi.materialImportMode != ModelImporterMaterialImportMode.None) { mi.materialImportMode = ModelImporterMaterialImportMode.None; dirty = true; }
                if (mi.isReadable) { mi.isReadable = false; dirty = true; }
                if (mi.meshCompression != ModelImporterMeshCompression.Medium) { mi.meshCompression = ModelImporterMeshCompression.Medium; dirty = true; }
                if (dirty) mi.SaveAndReimport();
            }

            string texDir = Directory.Exists($"{dir}/textures") ? $"{dir}/textures" : dir;
            if (!Directory.Exists(texDir)) return;
            foreach (string f in Directory.GetFiles(texDir, "*.png"))
            {
                string path = f.Replace('\\', '/');
                if (!(AssetImporter.GetAtPath(path) is TextureImporter ti)) continue;
                string n = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                bool dirty = false;
                if (n.Contains("normal") && ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; dirty = true; }
                bool linear = n.Contains("metallic") || n.Contains("roughness");
                if (linear && ti.sRGBTexture) { ti.sRGBTexture = false; dirty = true; }
                if (ti.maxTextureSize != 1024) { ti.maxTextureSize = 1024; dirty = true; }
                if (dirty) ti.SaveAndReimport();
            }
        }

        // ─────────────────────────────────────────────────────────── materials

        private static Material MakePbrMaterial(string matPath, string texDir, float emission, float smoothness, Color tint)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else if (mat.shader == null || mat.shader.name != "Standard") mat.shader = Shader.Find("Standard");

            Texture2D baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{texDir}/base_color.png");
            Texture2D normTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{texDir}/normal.png");
            Texture2D metTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{texDir}/metallic.png");

            mat.color = tint;
            if (baseTex != null) mat.mainTexture = baseTex;

            if (normTex != null)
            {
                mat.SetTexture("_BumpMap", normTex);
                mat.SetFloat("_BumpScale", 1f);
                mat.EnableKeyword("_NORMALMAP");
            }

            if (metTex != null)
            {
                mat.SetTexture("_MetallicGlossMap", metTex);
                mat.EnableKeyword("_METALLICGLOSSMAP");
                mat.SetFloat("_GlossMapScale", smoothness);
            }
            else
            {
                mat.SetTexture("_MetallicGlossMap", null);
                mat.DisableKeyword("_METALLICGLOSSMAP");
                mat.SetFloat("_Metallic", 0f);
                mat.SetFloat("_Glossiness", smoothness);
            }

            // A little self-illumination from the albedo keeps stylised colours readable in shadow on small phone screens
            if (emission > 0f && baseTex != null)
            {
                mat.SetTexture("_EmissionMap", baseTex);
                mat.SetColor("_EmissionColor", new Color(emission, emission, emission, 1f));
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.black);
            }

            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material SolidMaterial(string name, Color albedo, float smoothness, Color emission)
        {
            string path = $"{MAT_DIR}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.color = albedo;
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Glossiness", smoothness);
            if (emission.maxColorComponent > 0.001f)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.black);
            }
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void BuildSporeMaterial()
        {
            string path = $"{MAT_DIR}/M_L2_Spore.mat";
            Shader sh = Shader.Find("ZombieSquad/VFX_FireAdditive") ?? Shader.Find("Legacy Shaders/Particles/Additive") ?? Shader.Find("Sprites/Default");
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(sh);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = sh;
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Textures/VFX/tex_spark_ember.png");
            if (tex != null) mat.mainTexture = tex;
            if (mat.HasProperty("_GlowIntensity")) mat.SetFloat("_GlowIntensity", 1.8f);
            EditorUtility.SetDirty(mat);
        }

        private static void BuildSunsetSkybox()
        {
            string path = $"{MAT_DIR}/M_L2_SunsetSky.mat";
            Shader sh = Shader.Find("Skybox/Procedural");
            if (sh == null) return;
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(sh);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = sh;
            mat.SetFloat("_SunDisk", 2f);              // high quality sun
            mat.SetFloat("_SunSize", 0.05f);
            mat.SetFloat("_SunSizeConvergence", 4f);
            mat.SetFloat("_AtmosphereThickness", 1.55f);
            mat.SetColor("_SkyTint", new Color(0.62f, 0.52f, 0.62f));
            mat.SetColor("_GroundColor", new Color(0.42f, 0.36f, 0.30f));
            mat.SetFloat("_Exposure", 1.35f);
            EditorUtility.SetDirty(mat);
        }

        // ─────────────────────────────────────────────────────────── wreck props

        private static void BuildPropPrefab(string name, float targetLength, Material mat)
        {
            string fbx = $"{PROPS_DIR}/{name}/{name}.fbx";
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            if (model == null) { Debug.LogWarning("[Level 2 Setup] Missing " + fbx); return; }

            GameObject root = new GameObject("L2_" + name);
            GameObject vis = InstantiateModel(model, root.transform, "Visual", mat);

            // Long axis along the road (+Z)
            Bounds b = GetBounds(vis);
            if (b.size.x > b.size.z) vis.transform.rotation = Quaternion.Euler(0f, 90f, 0f) * vis.transform.rotation;

            b = GetBounds(vis);
            float scale = b.size.z > 0.001f ? targetLength / b.size.z : 1f;
            vis.transform.localScale *= scale;
            b = GetBounds(vis);
            vis.transform.position -= new Vector3(b.center.x, b.min.y, b.center.z);
            b = GetBounds(vis);

            // Solid box hugging the body (slightly inset so near-misses feel fair). Decorative copies strip it at runtime.
            BoxCollider col = root.AddComponent<BoxCollider>();
            col.center = b.center;
            col.size = new Vector3(b.size.x * 0.82f, b.size.y * 0.9f, b.size.z * 0.88f);

            string path = $"{PROP_PREFAB_DIR}/L2_{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        // ─────────────────────────────────────────────────────────── plant monster

        private static void BuildPlantMonsterPrefab(Material flytrapMat)
        {
            Material leafMat = SolidMaterial("M_L2_PodLeaf", new Color(0.30f, 0.70f, 0.16f), 0.55f, new Color(0.03f, 0.10f, 0.02f));
            Material leafDarkMat = SolidMaterial("M_L2_PodLeafDark", new Color(0.13f, 0.42f, 0.12f), 0.45f, Color.black);
            Material budMat = SolidMaterial("M_L2_PodBud", new Color(0.78f, 0.18f, 0.66f), 0.7f, new Color(0.45f, 0.04f, 0.38f));
            Material spikeMat = SolidMaterial("M_L2_PodSpike", new Color(0.98f, 0.86f, 0.55f), 0.3f, Color.black);
            Material soilMat = SolidMaterial("M_L2_CrackedSoil", new Color(0.24f, 0.17f, 0.12f), 0.1f, Color.black);

            GameObject root = new GameObject("PlantMonster_Prefab");
            var col = root.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.center = new Vector3(0f, 0.5f, 0f);
            col.size = new Vector3(1.8f, 1.2f, 1.8f);
            var rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            // ── Dormant pod: burst asphalt, a ring of curled leaves and a glowing magenta bud
            GameObject pod = new GameObject("RoadPlantPod");
            pod.transform.SetParent(root.transform, false);

            Prim(PrimitiveType.Sphere, "SoilMound", pod.transform, new Vector3(0f, 0.02f, 0f), new Vector3(1.7f, 0.22f, 1.7f), Quaternion.identity, soilMat);
            for (int i = 0; i < 4; i++)
            {
                float a = i * 90f + 20f;
                Vector3 dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                Prim(PrimitiveType.Cube, $"AsphaltChunk_{i}", pod.transform, dir * 0.95f + Vector3.up * 0.05f,
                    new Vector3(0.38f, 0.08f, 0.28f), Quaternion.Euler(Random.Range(-18f, 18f), a + 15f, Random.Range(10f, 25f)), soilMat);
            }

            for (int i = 0; i < 7; i++)
            {
                float a = i * (360f / 7f);
                Vector3 dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                // Outer leaf lying on the road, tip curling up
                Prim(PrimitiveType.Sphere, $"Leaf_{i}", pod.transform, dir * 0.62f + Vector3.up * 0.10f,
                    new Vector3(0.36f, 0.07f, 0.95f), Quaternion.Euler(-14f, a, 0f), (i % 2 == 0) ? leafMat : leafDarkMat);
            }
            for (int i = 0; i < 5; i++)
            {
                float a = i * 72f + 36f;
                Vector3 dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                // Inner leaves cupping the bud
                Prim(PrimitiveType.Sphere, $"InnerLeaf_{i}", pod.transform, dir * 0.24f + Vector3.up * 0.38f,
                    new Vector3(0.28f, 0.62f, 0.12f), Quaternion.Euler(18f, a, 0f), leafMat);
            }

            GameObject bud = Prim(PrimitiveType.Sphere, "Bud", pod.transform, new Vector3(0f, 0.48f, 0f), new Vector3(0.55f, 0.72f, 0.55f), Quaternion.identity, budMat);
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f;
                Vector3 dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                Prim(PrimitiveType.Capsule, $"Spike_{i}", bud.transform, dir * 0.45f + Vector3.up * 0.12f,
                    new Vector3(0.08f, 0.16f, 0.08f), Quaternion.LookRotation(dir) * Quaternion.Euler(70f, 0f, 0f), spikeMat);
            }

            // ── Erupted form: Meshy flytrap monster (ZombieRoot is scaled 0→1 by EmergingPlantMonster)
            GameObject zombieRoot = new GameObject("ZombieRoot");
            zombieRoot.transform.SetParent(root.transform, false);
            GameObject flytrapModel = AssetDatabase.LoadAssetAtPath<GameObject>($"{FLYTRAP_DIR}/PlantMonster.fbx");
            if (flytrapModel != null)
            {
                GameObject vis = InstantiateModel(flytrapModel, zombieRoot.transform, "Flytrap", flytrapMat);
                vis.transform.rotation = Quaternion.Euler(0f, FACE_PLAYER_YAW, 0f) * vis.transform.rotation;
                NormalizeHeight(vis, FLYTRAP_HEIGHT);
            }

            // Health bar uses the same billboard bar as every zombie (the old cube bar rendered as a white slab)
            GameObject bar = new GameObject("HealthBar3D");
            bar.transform.SetParent(root.transform, false);
            bar.transform.localPosition = new Vector3(0f, FLYTRAP_HEIGHT - 1.75f, 0f);
            bar.AddComponent<Simple3DHealthBar>();

            root.AddComponent<EmergingPlantMonster>();

            PrefabUtility.SaveAsPrefabAsset(root, PLANT_MONSTER_PREFAB);
            Object.DestroyImmediate(root);
        }

        // ─────────────────────────────────────────────────────────── wyvern

        private static void BuildWyvernPrefab(Material mat)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>($"{DRAGON_DIR}/WyvernDragon.fbx");
            if (model == null) return;

            GameObject root = new GameObject("WyvernDragon_Prefab");
            var col = root.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.center = Vector3.zero;
            col.radius = 1.8f;
            var rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            // VisualRoot gets overwritten every frame (bank/tilt); the model's own import rotation lives on the child
            GameObject visualRoot = new GameObject("VisualRoot");
            visualRoot.transform.SetParent(root.transform, false);
            GameObject vis = InstantiateModel(model, visualRoot.transform, "Model", mat);
            Bounds b = GetBounds(vis);
            float span = Mathf.Max(b.size.x, b.size.z);
            if (span > 0.001f) vis.transform.localScale *= 3.0f / span;
            b = GetBounds(vis);
            vis.transform.position -= b.center;

            root.AddComponent<FlyingWyvernAirThreat>();
            PrefabUtility.SaveAsPrefabAsset(root, DRAGON_PREFAB);
            Object.DestroyImmediate(root);
        }

        // ─────────────────────────────────────────────────────────── helpers

        private static GameObject InstantiateModel(GameObject model, Transform parent, string name, Material mat)
        {
            GameObject vis = (GameObject)PrefabUtility.InstantiatePrefab(model);
            PrefabUtility.UnpackPrefabInstance(vis, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            vis.name = name;
            vis.transform.SetParent(parent, false);
            foreach (var c in vis.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            foreach (var r in vis.GetComponentsInChildren<Renderer>(true))
            {
                r.sharedMaterial = mat;
                r.shadowCastingMode = ShadowCastingMode.On;
                r.receiveShadows = true;
            }
            return vis;
        }

        private static void NormalizeHeight(GameObject vis, float height)
        {
            Bounds b = GetBounds(vis);
            if (b.size.y > 0.001f) vis.transform.localScale *= height / b.size.y;
            b = GetBounds(vis);
            vis.transform.position -= new Vector3(b.center.x - vis.transform.parent.position.x, b.min.y - vis.transform.parent.position.y, b.center.z - vis.transform.parent.position.z);
        }

        private static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Quaternion rot, Material mat)
        {
            GameObject g = GameObject.CreatePrimitive(type);
            g.name = name;
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localRotation = rot;
            g.transform.localScale = scale;
            Object.DestroyImmediate(g.GetComponent<Collider>());
            var r = g.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            return g;
        }

        private static Bounds GetBounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            Bounds b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
#endif
