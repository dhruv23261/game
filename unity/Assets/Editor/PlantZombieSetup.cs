#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using MobControl.Crowd;
using MobControl.UI;

namespace MobControl.EditorTools
{
    /// <summary>
    /// Builds Resources/Prefabs/Zombie_Plant.prefab (Level 2 walking plant zombie) from the Meshy
    /// rigged + running FBX in Assets/Models/Generated/PlantZombie. Runs once automatically after
    /// the model is imported; re-run any time from the menu.
    /// </summary>
    [InitializeOnLoad]
    public static class PlantZombieSetup
    {
        private const string DIR = "Assets/Models/Generated/PlantZombie";
        private const string PREFAB_PATH = "Assets/Resources/Prefabs/Zombie_Plant.prefab";
        private const string MAT_PATH = "Assets/Resources/Materials/M_PlantZombie.mat";
        private const string CONTROLLER_PATH = DIR + "/PlantZombie.controller";

        /// <summary>Visible height of the zombie in metres (green zombie is ~1.3m).</summary>
        private const float TARGET_HEIGHT = 1.6f;
        /// <summary>Extra yaw if the Meshy model faces the wrong way (zombies must face +Z of the root).</summary>
        private const float MODEL_YAW = 0f;

        static PlantZombieSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH) == null && FindAnimatedFbx() != null)
                    Build();
            };
        }

        private static string FindAnimatedFbx()
        {
            foreach (var name in new[] { "PlantZombie_Running.fbx", "PlantZombie_Walking.fbx", "PlantZombie_Rigged.fbx", "PlantZombie_Model.fbx" })
            {
                string p = DIR + "/" + name;
                if (File.Exists(p)) return p;
            }
            return null;
        }

        /// <summary>
        /// Textures that belong to the rigged running FBX. PlantZombie_textures/ belongs to the separate
        /// un-rigged PlantZombie.fbx export and its UVs do NOT match — using it made the zombies look muddy.
        /// </summary>
        private const string TEX_DIR = DIR + "/textures";

        private static string FindTexture(string key)
        {
            string dir = Directory.Exists(TEX_DIR) ? TEX_DIR : DIR;
            if (!Directory.Exists(dir)) return null;
            return Directory.GetFiles(dir, "*.png", SearchOption.AllDirectories)
                .Select(f => f.Replace('\\', '/'))
                .FirstOrDefault(f => Path.GetFileName(f).ToLowerInvariant().Contains(key));
        }

        [MenuItem("Zombie Squad/Level 2/Build Plant Zombie Prefab")]
        public static void Build()
        {
            string fbxPath = FindAnimatedFbx();
            if (fbxPath == null)
            {
                Debug.LogWarning("[PlantZombieSetup] No PlantZombie FBX found in " + DIR);
                return;
            }

            // 1. Import as Generic rig with a looping clip
            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer != null)
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.importAnimation = true;
                var clips = importer.defaultClipAnimations;
                foreach (var c in clips) { c.loopTime = true; c.loopPose = true; }
                if (clips.Length > 0) importer.clipAnimations = clips;
                importer.SaveAndReimport();
            }

            // 2. Material from Meshy PBR textures
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(MAT_PATH);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Standard")) { name = "M_PlantZombie" };
                AssetDatabase.CreateAsset(mat, MAT_PATH);
            }
            string baseTex = FindTexture("base_color");
            string normTex = FindTexture("normal");
            if (normTex != null && AssetImporter.GetAtPath(normTex) is TextureImporter ni && ni.textureType != TextureImporterType.NormalMap)
            {
                ni.textureType = TextureImporterType.NormalMap;
                ni.SaveAndReimport();
            }
            foreach (var t in new[] { baseTex, normTex, FindTexture("metallic"), FindTexture("roughness") })
            {
                if (t != null && AssetImporter.GetAtPath(t) is TextureImporter ti && ti.maxTextureSize > 1024)
                {
                    ti.maxTextureSize = 1024; // mobile
                    ti.SaveAndReimport();
                }
            }
            if (baseTex != null) mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(baseTex);
            if (normTex != null)
            {
                mat.EnableKeyword("_NORMALMAP");
                mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normTex));
            }
            string metTex = FindTexture("metallic");
            if (metTex != null && AssetImporter.GetAtPath(metTex) is TextureImporter mti && mti.sRGBTexture)
            {
                mti.sRGBTexture = false;
                mti.SaveAndReimport();
            }
            if (metTex != null)
            {
                mat.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(metTex));
                mat.EnableKeyword("_METALLICGLOSSMAP");
                mat.SetFloat("_GlossMapScale", 0.35f);
            }
            mat.SetFloat("_Glossiness", 0.35f);
            // Soft self-illumination from the albedo: keeps the green skin, yellow eyes and pink flower readable at phone size
            if (baseTex != null)
            {
                mat.SetTexture("_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(baseTex));
                mat.SetColor("_EmissionColor", new Color(0.32f, 0.32f, 0.32f, 1f));
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);

            // 3. Animator controller with the run cycle as its only state
            AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
            AnimatorController controller = null;
            if (clip != null)
            {
                AssetDatabase.DeleteAsset(CONTROLLER_PATH);
                controller = AnimatorController.CreateAnimatorControllerAtPathWithClip(CONTROLLER_PATH, clip);
            }

            // 4. Prefab
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            GameObject root = new GameObject("Zombie_Plant");

            GameObject vis = (GameObject)PrefabUtility.InstantiatePrefab(model);
            PrefabUtility.UnpackPrefabInstance(vis, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            vis.name = "Visual_PlantZombie";
            vis.transform.SetParent(root.transform, false);
            vis.transform.localRotation = Quaternion.Euler(0f, MODEL_YAW, 0f);
            foreach (var r in vis.GetComponentsInChildren<Renderer>(true)) r.sharedMaterial = mat;

            // Normalise height and put feet on the ground
            Bounds b = GetBounds(vis);
            float scale = b.size.y > 0.001f ? TARGET_HEIGHT / b.size.y : 1f;
            vis.transform.localScale = Vector3.one * scale;
            b = GetBounds(vis);
            vis.transform.localPosition = new Vector3(-b.center.x, -b.min.y, -b.center.z);

            Animator anim = vis.GetComponent<Animator>();
            if (anim == null) anim = vis.AddComponent<Animator>();
            if (anim != null && controller != null)
            {
                anim.runtimeAnimatorController = controller;
                anim.applyRootMotion = false;
                anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            }

            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            BoxCollider col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, TARGET_HEIGHT * 0.5f, 0f);
            col.size = new Vector3(0.75f, TARGET_HEIGHT, 0.75f);

            MobUnit unit = root.AddComponent<MobUnit>();
            unit.Team = MobTeam.Enemy;
            unit.PoolTag = ZombieRoadWaveSpawner.PLANT_ZOMBIE_TAG;
            unit.SetStats(2.9f, 4, 1);

            GameObject barObj = new GameObject("HealthBar3D");
            barObj.transform.SetParent(root.transform, false);
            barObj.transform.localPosition = new Vector3(0f, TARGET_HEIGHT + 0.25f, 0f);
            barObj.AddComponent<Simple3DHealthBar>();

            if (!AssetDatabase.IsValidFolder("Assets/Resources/Prefabs")) AssetDatabase.CreateFolder("Assets/Resources", "Prefabs");
            PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log($"<color=#66FF66><b>[PlantZombieSetup] Built {PREFAB_PATH} from {fbxPath} (clip: {(clip != null ? clip.name : "none")})</b></color>");
        }

        private static Bounds GetBounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            Bounds b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }
    }
}
#endif
