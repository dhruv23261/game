#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using MobControl.Level;

namespace MobControl.EditorTools
{
    public static class WreckedCarAssetProcessor
    {
        [MenuItem("Zombie Squad/Process AAA Wrecked Car Asset")]
        public static void ProcessWreckedCar()
        {
            AssetDatabase.Refresh();

            // 1. Configure texture import settings
            string baseColPath = "Assets/Resources/Textures/Car/WreckedCar_base_color.png";
            string normPath = "Assets/Resources/Textures/Car/WreckedCar_normal.png";

            TextureImporter normalImporter = AssetImporter.GetAtPath(normPath) as TextureImporter;
            if (normalImporter != null)
            {
                normalImporter.textureType = TextureImporterType.NormalMap;
                normalImporter.SaveAndReimport();
            }

            // 2. Ensure Material
            string matPath = "Assets/Resources/Materials/M_WreckedCar_AAA.mat";
            if (!Directory.Exists("Assets/Resources/Materials")) Directory.CreateDirectory("Assets/Resources/Materials");

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Standard"));
                mat.name = "M_WreckedCar_AAA";
                AssetDatabase.CreateAsset(mat, matPath);
            }

            Texture2D baseCol = AssetDatabase.LoadAssetAtPath<Texture2D>(baseColPath);
            Texture2D normMap = AssetDatabase.LoadAssetAtPath<Texture2D>(normPath);
            Texture2D metMap = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Textures/Car/WreckedCar_metallic.png");
            Texture2D roughMap = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Textures/Car/WreckedCar_roughness.png");

            if (baseCol != null) mat.mainTexture = baseCol;
            if (normMap != null)
            {
                mat.EnableKeyword("_NORMALMAP");
                mat.SetTexture("_BumpMap", normMap);
            }
            if (metMap != null)
            {
                mat.EnableKeyword("_METALLICGLOSSMAP");
                mat.SetTexture("_MetallicGlossMap", metMap);
            }
            mat.SetFloat("_Glossiness", 0.25f);
            mat.SetFloat("_Metallic", 0.65f);
            EditorUtility.SetDirty(mat);

            // 3. Load Car Mesh
            Mesh carMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Models/Generated/WreckedCar/WreckedCar.obj");
            if (carMesh == null)
            {
                // Try from FBX
                GameObject fbxObj = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Generated/WreckedCar/WreckedCar.fbx");
                if (fbxObj != null)
                {
                    MeshFilter mf = fbxObj.GetComponentInChildren<MeshFilter>();
                    if (mf != null) carMesh = mf.sharedMesh;
                }
            }

            if (carMesh == null)
            {
                string objPath = Path.Combine(Application.dataPath, "Models/Generated/WreckedCar/WreckedCar.obj");
                carMesh = NewRoadBuilder.LoadObjMeshDirectPublic(objPath);
            }

            // 4. Create AAA Wrecked Car Prefab
            string prefabDir = "Assets/Resources/Prefabs";
            if (!Directory.Exists(prefabDir)) Directory.CreateDirectory(prefabDir);
            string prefabPath = prefabDir + "/WreckedCar_AAA.prefab";

            GameObject root = new GameObject("Obstacle_BurnedCar_AAA");

            // Visual Child
            GameObject vis = new GameObject("Visual_Mesh");
            vis.transform.SetParent(root.transform, false);
            vis.transform.localPosition = new Vector3(0f, 0.67f, 0f);
            vis.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            vis.transform.localScale = Vector3.one * 2.1f;

            MeshFilter vmf = vis.AddComponent<MeshFilter>();
            vmf.sharedMesh = carMesh;
            MeshRenderer vmr = vis.AddComponent<MeshRenderer>();
            vmr.sharedMaterial = mat;


            // Solid Collider & Rigidbody
            BoxCollider col = root.AddComponent<BoxCollider>();
            col.isTrigger = false;
            col.center = new Vector3(0f, 0.70f, 0f);
            col.size = new Vector3(1.85f, 1.40f, 4.0f);

            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            RoadObstacle obs = root.AddComponent<RoadObstacle>();
            obs.Initialize(isContainer: false, damage: 1);

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green><b>[AAA Wrecked Car] Successfully created Prefab at " + prefabPath + "!</b></color>");
        }
    }
}
#endif
