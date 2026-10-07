using UnityEngine;
using UnityEditor;
using MobControl.Combat;

namespace MobControl.EditorTools
{
    public class BulletPrefabBuilder
    {
        [MenuItem("Tools/Build 9mm Bullet Prefab")]
        public static void BuildPrefab()
        {
            // 1. Load FBX
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Bullet/source/9mm_bullet.fbx");
            if (fbx == null)
            {
                Debug.LogError("[BulletPrefabBuilder] Could not find 9mm_bullet.fbx!");
                return;
            }

            // 2. Instantiate temporary object
            GameObject bulletObj = new GameObject("bullet_9mm");
            
            // Child visual model from FBX
            GameObject visual = Object.Instantiate(fbx, bulletObj.transform);
            visual.name = "Visual";
            
            // Adjust local rotation so bullet tip faces straight forward (+Z)
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            visual.transform.localScale = new Vector3(3.2f, 3.2f, 3.2f);

            // Assign textures & materials to all renderers
            Texture2D bulletTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/Bullet/textures/bullet_BaseColor.jpg");
            Texture2D bulletNorm = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/Bullet/textures/bullet_Normal.jpg");
            Texture2D bulletMetal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/Bullet/textures/bullet_Metallic.jpg");

            Texture2D shellTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/Bullet/textures/Shell_BaseColor.png");
            Texture2D shellNorm = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/Bullet/textures/Shelll_Normal.png");
            Texture2D shellMetal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/Bullet/textures/Shell_Metallic.png");

            // Create Bullet Tip Material (with vibrant arcade yellow emission)
            Material bulletMat = new Material(Shader.Find("Standard"));
            bulletMat.name = "Mat_BulletTip";
            if (bulletTex != null) bulletMat.mainTexture = bulletTex;
            if (bulletNorm != null) { bulletMat.EnableKeyword("_NORMALMAP"); bulletMat.SetTexture("_BumpMap", bulletNorm); }
            if (bulletMetal != null) { bulletMat.EnableKeyword("_METALLICGLOSSMAP"); bulletMat.SetTexture("_MetallicGlossMap", bulletMetal); bulletMat.SetFloat("_Metallic", 0.9f); }
            bulletMat.color = new Color(1f, 0.88f, 0.1f);
            bulletMat.EnableKeyword("_EMISSION");
            bulletMat.SetColor("_EmissionColor", new Color(1f, 0.85f, 0.1f));
            AssetDatabase.CreateAsset(bulletMat, "Assets/Materials/Mat_BulletTip.mat");

            // Create Shell Casing Material
            Material shellMat = new Material(Shader.Find("Standard"));
            shellMat.name = "Mat_BulletShell";
            if (shellTex != null) shellMat.mainTexture = shellTex;
            if (shellNorm != null) { shellMat.EnableKeyword("_NORMALMAP"); shellMat.SetTexture("_BumpMap", shellNorm); }
            if (shellMetal != null) { shellMat.EnableKeyword("_METALLICGLOSSMAP"); shellMat.SetTexture("_MetallicGlossMap", shellMetal); shellMat.SetFloat("_Metallic", 0.95f); }
            shellMat.color = new Color(1f, 0.82f, 0.15f);
            shellMat.EnableKeyword("_EMISSION");
            shellMat.SetColor("_EmissionColor", new Color(0.95f, 0.8f, 0.15f));
            AssetDatabase.CreateAsset(shellMat, "Assets/Materials/Mat_BulletShell.mat");

            // Create Bullet Trail Material (Electric Neon Blue)
            Shader trailShader = Shader.Find("Mobile/Particles/Additive") ?? Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Sprites/Default");
            Material trailMat = new Material(trailShader);
            trailMat.name = "Mat_BulletTrail";
            trailMat.SetColor("_TintColor", new Color(0.15f, 0.8f, 1f, 0.95f));
            trailMat.color = new Color(0.15f, 0.8f, 1f, 0.95f);
            AssetDatabase.CreateAsset(trailMat, "Assets/Materials/Mat_BulletTrail.mat");

            // Apply to visual child renderers
            MeshRenderer[] renderers = visual.GetComponentsInChildren<MeshRenderer>();
            foreach (var rend in renderers)
            {
                if (rend.gameObject.name.ToLower().Contains("shell"))
                {
                    rend.material = shellMat;
                }
                else
                {
                    rend.material = bulletMat;
                }
            }

            // 3. Add Glowing Tracer Trail (Electric Blue)
            TrailRenderer trail = bulletObj.AddComponent<TrailRenderer>();
            trail.material = trailMat;
            trail.time = 0.12f;
            trail.startWidth = 0.22f;
            trail.endWidth = 0.01f;
            trail.autodestruct = false;
            trail.minVertexDistance = 0.05f;
            
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(0.2f, 0.85f, 1f), 0f), new GradientColorKey(new Color(0.05f, 0.45f, 1f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            trail.colorGradient = grad;

            // 4. Add Collider & Script to Root
            CapsuleCollider col = bulletObj.AddComponent<CapsuleCollider>();
            col.isTrigger = true;
            col.radius = 0.2f;
            col.height = 0.8f;
            col.direction = 2; // Z-axis

            BulletProjectile proj = bulletObj.AddComponent<BulletProjectile>();

            // 4. Save as Prefab
            string prefabPath = "Assets/Prefabs/Bullet_9mm.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(bulletObj, prefabPath);
            Object.DestroyImmediate(bulletObj);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[BulletPrefabBuilder] Successfully created 3D 9mm Bullet Prefab at: {prefabPath}");
        }
    }
}
