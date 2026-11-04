using UnityEngine;
using UnityEditor;
using MobControl.Combat;

namespace MobControl.EditorTools
{
    public class BulletPrefabBuilder
    {
        [MenuItem("Tools/Build 9mm Bullet Prefab")]
        [MenuItem("Zombie Squad/Build Real 3D 9mm Bullet Prefab (Goli Match)")]
        public static void BuildPrefab()
        {
            // 1. Root projectile object
            GameObject bulletObj = new GameObject("Bullet_9mm");

            // 2. Ensure Bullet Materials with realistic PBR brass and copper look
            Material bulletMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Bullet9mmMaterial.mat");
            if (bulletMat == null)
            {
                bulletMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_BulletShell.mat");
            }
            if (bulletMat == null)
            {
                bulletMat = new Material(Shader.Find("Standard"));
                bulletMat.name = "Bullet9mmMaterial";
                bulletMat.color = new Color(0.95f, 0.78f, 0.35f, 1f); // Golden brass
                bulletMat.SetFloat("_Metallic", 0.90f);
                bulletMat.SetFloat("_Glossiness", 0.80f);
                AssetDatabase.CreateAsset(bulletMat, "Assets/Materials/Bullet9mmMaterial.mat");
            }

            Material shellMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_BulletShell.mat") ?? bulletMat;
            Material tipMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_BulletTip.mat") ?? bulletMat;

            // 3. Load Authentic 3D 9mm Bullet FBX Model
            GameObject bulletFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Bullet/source/9mm_bullet.fbx");
            GameObject visualObj = null;

            if (bulletFbx != null)
            {
                visualObj = (GameObject)PrefabUtility.InstantiatePrefab(bulletFbx);
                PrefabUtility.UnpackPrefabInstance(visualObj, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                visualObj.name = "Visual_9mmBullet";
                visualObj.transform.SetParent(bulletObj.transform, false);
                visualObj.transform.localPosition = new Vector3(0f, 0f, 0.11f);
                // 9mm_bullet.fbx is natively modeled along +Z (bullet tip at +Z, shell base at -Z).
                // Keep Quaternion.identity so tip points straight forward (+Z) along line of fire, NOT 90 degrees down.
                visualObj.transform.localRotation = Quaternion.identity;
                // Proportioned visible bullet scale for crisp 3rd-person camera view
                visualObj.transform.localScale = Vector3.one * 3.8f;

                // Assign authentic textures/materials to all child renderers
                var renderers = visualObj.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var mr in renderers)
                {
                    if (mr.gameObject.name.IndexOf("tip", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        mr.gameObject.name.IndexOf("bullet", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        mr.sharedMaterial = tipMat;
                    }
                    else if (mr.gameObject.name.IndexOf("shell", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        mr.sharedMaterial = shellMat;
                    }
                    else
                    {
                        mr.sharedMaterial = bulletMat;
                    }
                }

                // Remove any extraneous colliders from visual child
                var childCols = visualObj.GetComponentsInChildren<Collider>(true);
                foreach (var c in childCols) Object.DestroyImmediate(c);
            }
            else
            {
                // Fallback: Procedural aerodynamic bullet mesh
                visualObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                visualObj.name = "Visual_Bullet";
                visualObj.transform.SetParent(bulletObj.transform, false);
                visualObj.transform.localPosition = Vector3.zero;
                visualObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                visualObj.transform.localScale = new Vector3(0.12f, 0.32f, 0.12f);

                var mr = visualObj.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = bulletMat;

                var c = visualObj.GetComponent<Collider>();
                if (c != null) Object.DestroyImmediate(c);
            }

            // 4. Dynamic Golden Bullet Light (Illuminates road & targets with warm tracer glow)
            GameObject lightObj = new GameObject("TracerGlowLight");
            lightObj.transform.SetParent(bulletObj.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0f, 0.20f);

            Light glowLight = lightObj.AddComponent<Light>();
            glowLight.type = LightType.Point;
            glowLight.color = new Color(1.0f, 0.82f, 0.35f); // Incandescent golden tracer light
            glowLight.range = 2.2f;
            glowLight.intensity = 1.4f;

            // 5. High-Velocity Incandescent Tracer Trail (Custom ZombieSquad/BulletTracerTrail - 100% Guaranteed Zero Pink Shader!)
            Shader trailShader = Shader.Find("ZombieSquad/BulletTracerTrail")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Mobile/Particles/Additive")
                ?? Shader.Find("Unlit/Color");

            if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Materials")) AssetDatabase.CreateFolder("Assets/Resources", "Materials");

            Material trailMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_BulletTrail.mat");
            if (trailMat == null)
            {
                trailMat = new Material(trailShader);
                trailMat.name = "Mat_BulletTrail";
                AssetDatabase.CreateAsset(trailMat, "Assets/Materials/Mat_BulletTrail.mat");
            }
            else
            {
                trailMat.shader = trailShader;
            }

            trailMat.color = Color.white;
            trailMat.SetColor("_TintColor", new Color(1.0f, 0.88f, 0.38f, 1.0f));
            trailMat.SetFloat("_Intensity", 2.2f);

            Texture2D tracerTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/tex_bullet_tracer_strip.png");
            if (tracerTex != null)
            {
                trailMat.mainTexture = tracerTex;
            }
            EditorUtility.SetDirty(trailMat);

            AssetDatabase.SaveAssets();

            // Reload exact persistent asset from database
            Material persistentTrailMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_BulletTrail.mat");

            TrailRenderer trail = bulletObj.AddComponent<TrailRenderer>();
            trail.sharedMaterial = persistentTrailMat; // Critical: Must be sharedMaterial for prefab serialization!
            trail.time = 0.055f;          // Ultra-snappy hypersonic military tracer streak
            trail.startWidth = 0.055f;    // Sleek authentic bullet width
            trail.endWidth = 0.003f;      // Tapers cleanly into distance
            trail.autodestruct = false;
            trail.minVertexDistance = 0.015f;

            // Warm golden military tracer gradient: Bright White-Gold Tip -> Amber Body -> Fading Smoke
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1.0f, 0.95f, 0.48f), 0f),    // Superheated bullet base
                    new GradientColorKey(new Color(1.0f, 0.65f, 0.12f), 0.45f),  // Rich brass/copper tracer
                    new GradientColorKey(new Color(0.90f, 0.35f, 0.05f), 1f)    // Fading orange heat
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.90f, 0f),
                    new GradientAlphaKey(0.50f, 0.45f),
                    new GradientAlphaKey(0.0f, 1f)
                }
            );
            trail.colorGradient = grad;

            // 6. Accurate Physics Trigger Collider
            CapsuleCollider col = bulletObj.AddComponent<CapsuleCollider>();
            col.isTrigger = true;
            col.radius = 0.22f;
            col.height = 0.85f;
            col.direction = 2; // Z-axis forward

            // 7. BulletProjectile Logic
            bulletObj.AddComponent<BulletProjectile>();

            // 8. Save Prefabs across all game paths
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Prefabs")) AssetDatabase.CreateFolder("Assets/Resources", "Prefabs");

            string prefabPath1 = "Assets/Prefabs/Bullet_9mm.prefab";
            string prefabPath2 = "Assets/Resources/Prefabs/Bullet_9mm.prefab";
            string prefabPath3 = "Assets/Prefabs/bullet.prefab";

            AssetDatabase.DeleteAsset(prefabPath1);
            AssetDatabase.DeleteAsset(prefabPath2);
            AssetDatabase.DeleteAsset(prefabPath3);

            PrefabUtility.SaveAsPrefabAsset(bulletObj, prefabPath1);
            PrefabUtility.SaveAsPrefabAsset(bulletObj, prefabPath2);
            PrefabUtility.SaveAsPrefabAsset(bulletObj, prefabPath3);
            Object.DestroyImmediate(bulletObj);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=gold>[BulletPrefabBuilder] Successfully built Authentic 3D 9mm Bullet Prefab (Golden Real Bullet) at:\n{prefabPath1}\n{prefabPath2}\n{prefabPath3}</color>");
        }
    }
}
