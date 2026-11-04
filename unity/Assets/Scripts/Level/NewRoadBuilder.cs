using UnityEngine;
using System.Collections.Generic;
using System.IO;
using TMPro;
using MobControl.Combat;

namespace MobControl.Level
{
    /// <summary>
    /// Builds the exact Level 1 Stylized Road matching the user's reference image:
    /// - 9.8m wide 2-lane asphalt highway with crisp white dashed divider & solid yellow shoulder lines.
    /// - Concrete curb foundation running outside both yellow lines.
    /// - Dual-rail slate-steel highway guardrails with amber/orange reflector squares on upright posts.
    /// - High-Definition Meshy 3D Stylized Pine Trees with vibrant layered green needles and wood trunk.
    /// - High-Definition Meshy 3D Stylized Cliff Rock monoliths stacked along both banks to form natural canyon walls.
    /// - Young sapling pine trees and smaller boulders nestled along the road verge.
    /// - Soft rolling mountain horizon ridge with atmospheric aerial perspective fog.
    /// - Camera angle and framing matched 1:1 with reference image.
    /// </summary>
    [ExecuteAlways]
    public class NewRoadBuilder : MonoBehaviour
    {
        [Header("Road Dimensions")]
        [SerializeField] private float roadWidth = 9.8f;
        [SerializeField] private float startZ = -15.0f;
        [SerializeField] private float endZ = 240.0f;
        [SerializeField] private float uvRepeatLength = 10.0f;

        [Header("Guardrail Settings")]
        [SerializeField] private float guardrailX = 5.25f;
        [SerializeField] private float postSpacing = 3.6f;

        public const string ROOT_NAME = "Level1_StylizedRoad_Root";

        private static Mesh cachedTreeMesh;
        private static Mesh cachedRockMesh;
        private static Material cachedTreeMat;
        private static Material cachedRockMat;

        private static Mesh cachedPlayerMesh;
        private static Material cachedPlayerMat;
        private static Mesh cachedRifleMesh;
        private static Material cachedRifleMat;

        public static void SafeDestroy(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }

        public static void EnsureNewRoad()
        {
            GameObject existingRoot = GameObject.Find(ROOT_NAME);
            if (existingRoot == null)
            {
                GameObject builderObj = new GameObject("Level1_RoadBuilder_Temp");
                NewRoadBuilder builder = builderObj.AddComponent<NewRoadBuilder>();
                builder.BuildRoadScene();
                SafeDestroy(builderObj);
            }
            else
            {
                GameObject hero = GameObject.Find("Player_Hero");
                if (hero == null)
                {
                    GameObject builderObj = new GameObject("Level1_RoadBuilder_Temp");
                    NewRoadBuilder builder = builderObj.AddComponent<NewRoadBuilder>();
                    builder.BuildPlayerCharacter(existingRoot.transform);
                    SafeDestroy(builderObj);
                }
            }
        }

        private void Start()
        {
            BuildRoadScene();
        }

        [ContextMenu("Build Reference Road")]
        public void BuildRoadScene()
        {
            PurgeSceneClutter();

            GameObject existingRoot = GameObject.Find(ROOT_NAME);
            if (existingRoot != null) SafeDestroy(existingRoot);

            GameObject root = new GameObject(ROOT_NAME);

            // Load Materials
            Material roadMat = Resources.Load<Material>("Materials/M_StylizedRoad");
            Material steelMat = Resources.Load<Material>("Materials/M_GuardrailSteel");
            Material grassMat = Resources.Load<Material>("Materials/M_GrassVerge");
            Material amberMat = Resources.Load<Material>("Materials/M_GunOrange");

            // Load Meshy 3D Assets (Pine Tree & Cliff Rock)
            LoadMeshyAssets();

            // 1. Build & Attach Endless Road Manager (Smoothly rushes road towards player with Barricades and Containers only)
            GameObject endlessObj = new GameObject("EndlessRoadManager");
            endlessObj.transform.SetParent(root.transform, false);
            EndlessRoadManager endlessManager = endlessObj.AddComponent<EndlessRoadManager>();
            endlessManager.BuildInitialEndlessTrack();

            // 2. Build Sweeping Distant Mountain Horizon Ridge
            BuildMountainHorizon(root.transform, grassMat);

            // 3. Setup Exact Camera & Atmospheric Horizon Fog
            SetupCameraAndAtmosphere();

            // 4. Spawn Player Mob & Assault Rifle (Meshy AI 1:1 Reference Match)
            BuildPlayerCharacter(root.transform);

            // 5. Attach ZombieRoadWaveSpawner (Small, Moderate, and Boss Zombies marching down the road)
            BuildZombieRoadSpawner(root.transform);

            Debug.Log("[NewRoadBuilder] Endless Stylized Highway assembled with EndlessRoadManager, Barricades & Containers with gaps, and Left-Right Joystick controls!");
        }

        private void PurgeSceneClutter()
        {
            // Destroy floating in-world TextMeshPro objects
            TextMeshPro[] worldTexts = Object.FindObjectsByType<TextMeshPro>(FindObjectsInactive.Include);
            foreach (var txt in worldTexts)
            {
                if (txt != null && txt.GetComponentInParent<Canvas>() == null)
                {
                    SafeDestroy(txt.gameObject);
                }
            }

            // Destroy legacy gates or unwanted objects
            GameObject[] allGos = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (var go in allGos)
            {
                if (go != null && (go.name.Contains("Gun_M41D") || go.name.Contains("MultiplierGate") || go.name.Contains("DestructibleGate") || go.name.Contains("PlayerMob") || go.name.StartsWith("Preview_Zombie_") || go.name.StartsWith("Zombie_") || go.name.StartsWith("BadgePop_") || go.name.Contains("Celebratory") || (go.name == "Player" && go.GetComponent<MobControl.Combat.PlayerRunnerController>() == null)))
                {
                    SafeDestroy(go);
                }
            }

            // Also purge any stray enemy mob units in the scene
            MobControl.Crowd.MobUnit[] mobs = Object.FindObjectsByType<MobControl.Crowd.MobUnit>(FindObjectsInactive.Include);
            foreach (var mob in mobs)
            {
                if (mob != null && mob.Team == MobControl.Crowd.MobTeam.Enemy)
                {
                    SafeDestroy(mob.gameObject);
                }
            }
        }

        private void LoadMeshyAssets()
        {
            // 1. Load Pine Tree Mesh & Material
            if (cachedTreeMesh == null)
            {
                GameObject treePrefab = Resources.Load<GameObject>("Nature/PineTree_V2");
                if (treePrefab != null) cachedTreeMesh = treePrefab.GetComponentInChildren<MeshFilter>()?.sharedMesh;
#if UNITY_EDITOR && !UNITY_ANDROID
                if (cachedTreeMesh == null)
                {
                    string treeObjPath = Path.Combine(Application.dataPath, "Models/Nature/PineTree_V2.obj");
                    cachedTreeMesh = LoadObjMeshDirect(treeObjPath);
                }
#endif
            }
            if (cachedTreeMat == null)
            {
                cachedTreeMat = Resources.Load<Material>("Materials/M_MeshyPineTree_V2");
                if (cachedTreeMat == null)
                {
                    cachedTreeMat = LoadTextureMaterial("Nature/PineTree_V2_textures/base_color.png", "M_MeshyPineTree_V2");
                }
            }

            // 2. Load Cliff Rock Mesh & Material
            if (cachedRockMesh == null)
            {
                GameObject rockPrefab = Resources.Load<GameObject>("Nature/CliffRock_V2");
                if (rockPrefab != null) cachedRockMesh = rockPrefab.GetComponentInChildren<MeshFilter>()?.sharedMesh;
#if UNITY_EDITOR && !UNITY_ANDROID
                if (cachedRockMesh == null)
                {
                    string rockObjPath = Path.Combine(Application.dataPath, "Models/Nature/CliffRock_V2.obj");
                    cachedRockMesh = LoadObjMeshDirect(rockObjPath);
                }
#endif
            }
            if (cachedRockMat == null)
            {
                cachedRockMat = Resources.Load<Material>("Materials/M_MeshyCliffRock_V2");
                if (cachedRockMat == null)
                {
                    cachedRockMat = LoadTextureMaterial("Nature/CliffRock_V2_textures/base_color.png", "M_MeshyCliffRock_V2");
                }
            }

            // 3. Load Meshy Player Mob Soldier Mesh & Material
            if (cachedPlayerMesh == null)
            {
                GameObject playerPrefab = Resources.Load<GameObject>("Characters/Player_FiringRifle") ?? Resources.Load<GameObject>("Characters/PlayerMob");
                if (playerPrefab != null)
                {
                    cachedPlayerMesh = playerPrefab.GetComponentInChildren<SkinnedMeshRenderer>()?.sharedMesh ?? playerPrefab.GetComponentInChildren<MeshFilter>()?.sharedMesh;
                }
#if UNITY_EDITOR && !UNITY_ANDROID
                if (cachedPlayerMesh == null)
                {
                    string playerObjPath = Path.Combine(Application.dataPath, "Models/Generated/PlayerMob_Rigged/PlayerMob_GameReady.obj");
                    cachedPlayerMesh = LoadObjMeshDirect(playerObjPath);
                }
#endif
            }
            if (cachedPlayerMat == null)
            {
                cachedPlayerMat = Resources.Load<Material>("Materials/PlayerMaterial");
            }

            // 4. Load Meshy Assault Rifle Mesh & Material
            if (cachedRifleMesh == null)
            {
                GameObject riflePrefab = Resources.Load<GameObject>("Models/AssaultRifle_GameReady") ?? Resources.Load<GameObject>("Prefabs/Gun_M41D");
                if (riflePrefab != null) cachedRifleMesh = riflePrefab.GetComponentInChildren<MeshFilter>()?.sharedMesh;
#if UNITY_EDITOR && !UNITY_ANDROID
                if (cachedRifleMesh == null)
                {
                    string rifleObjPath = Path.Combine(Application.dataPath, "Models/Generated/AssaultRifle/AssaultRifle_GameReady.obj");
                    cachedRifleMesh = LoadObjMeshDirect(rifleObjPath);
                }
#endif
            }
            if (cachedRifleMat == null)
            {
                cachedRifleMat = Resources.Load<Material>("Materials/M_MeshyAssaultRifle");
                if (cachedRifleMat == null)
                {
                    cachedRifleMat = LoadTextureMaterial("Textures/Gun/AssaultRifle_base_color.png", "M_MeshyAssaultRifle");
                }
            }
        }

        public static Mesh LoadObjMeshDirect(string filePath)
        {
            try
            {
#if UNITY_EDITOR && !UNITY_ANDROID
                if (!File.Exists(filePath))
                {
                    return null;
                }
                return LoadObjMeshDirectInternal(filePath);
#else
                return null;
#endif
            }
            catch
            {
                return null;
            }
        }

        public static Mesh LoadObjMeshDirectPublic(string filePath) => LoadObjMeshDirect(filePath);
        public static Material LoadTextureMaterialPublic(string texRelativePath, string matName) => LoadTextureMaterial(texRelativePath, matName);

        private static Mesh LoadObjMeshDirectInternal(string filePath)
        {

            List<Vector3> tempPositions = new List<Vector3>();
            List<Vector2> tempUVs = new List<Vector2>();
            List<Vector3> tempNormals = new List<Vector3>();

            List<Vector3> outPositions = new List<Vector3>();
            List<Vector2> outUVs = new List<Vector2>();
            List<Vector3> outNormals = new List<Vector3>();
            List<int> outIndices = new List<int>();

            Dictionary<string, int> vertexCache = new Dictionary<string, int>();

            string[] lines = File.ReadAllLines(filePath);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length < 2) continue;

                if (line.StartsWith("v "))
                {
                    string[] p = line.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                    if (p.Length >= 4)
                    {
                        float x = float.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture);
                        float y = float.Parse(p[2], System.Globalization.CultureInfo.InvariantCulture);
                        float z = float.Parse(p[3], System.Globalization.CultureInfo.InvariantCulture);
                        tempPositions.Add(new Vector3(x, y, z));
                    }
                }
                else if (line.StartsWith("vt "))
                {
                    string[] p = line.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                    if (p.Length >= 3)
                    {
                        float u = float.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture);
                        float v = float.Parse(p[2], System.Globalization.CultureInfo.InvariantCulture);
                        tempUVs.Add(new Vector2(u, v));
                    }
                }
                else if (line.StartsWith("vn "))
                {
                    string[] p = line.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                    if (p.Length >= 4)
                    {
                        float nx = float.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture);
                        float ny = float.Parse(p[2], System.Globalization.CultureInfo.InvariantCulture);
                        float nz = float.Parse(p[3], System.Globalization.CultureInfo.InvariantCulture);
                        tempNormals.Add(new Vector3(nx, ny, nz));
                    }
                }
                else if (line.StartsWith("f "))
                {
                    string[] p = line.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                    if (p.Length >= 4)
                    {
                        int[] faceVerts = new int[3];
                        for (int v = 0; v < 3; v++)
                        {
                            string vertToken = p[v + 1];
                            if (vertexCache.TryGetValue(vertToken, out int existingIdx))
                            {
                                faceVerts[v] = existingIdx;
                            }
                            else
                            {
                                string[] parts = vertToken.Split('/');
                                int posIdx = int.Parse(parts[0]) - 1;
                                int uvIdx = (parts.Length > 1 && !string.IsNullOrEmpty(parts[1])) ? int.Parse(parts[1]) - 1 : -1;
                                int normIdx = (parts.Length > 2 && !string.IsNullOrEmpty(parts[2])) ? int.Parse(parts[2]) - 1 : -1;

                                Vector3 pos = (posIdx >= 0 && posIdx < tempPositions.Count) ? tempPositions[posIdx] : Vector3.zero;
                                Vector2 uv = (uvIdx >= 0 && uvIdx < tempUVs.Count) ? tempUVs[uvIdx] : Vector2.zero;
                                Vector3 norm = (normIdx >= 0 && normIdx < tempNormals.Count) ? tempNormals[normIdx] : Vector3.up;

                                int newIdx = outPositions.Count;
                                outPositions.Add(pos);
                                outUVs.Add(uv);
                                outNormals.Add(norm);
                                vertexCache[vertToken] = newIdx;
                                faceVerts[v] = newIdx;
                            }
                        }
                        outIndices.Add(faceVerts[0]);
                        outIndices.Add(faceVerts[1]);
                        outIndices.Add(faceVerts[2]);
                    }
                }
            }

            Mesh mesh = new Mesh { name = Path.GetFileNameWithoutExtension(filePath) };
            if (outPositions.Count > 65534)
            {
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }
            mesh.SetVertices(outPositions);
            if (outUVs.Count > 0) mesh.SetUVs(0, outUVs);
            if (outNormals.Count > 0) mesh.SetNormals(outNormals);
            mesh.SetTriangles(outIndices, 0);
            mesh.RecalculateBounds();
            if (outNormals.Count == 0) mesh.RecalculateNormals();

            Debug.Log($"[NewRoadBuilder] Successfully loaded {mesh.name}: {outPositions.Count} verts, {outIndices.Count / 3} faces");
            return mesh;
        }

        private static Material LoadTextureMaterial(string texRelativePath, string matName)
        {
            Texture2D tex = null;
            try
            {
                string resPath = texRelativePath;
                if (resPath.StartsWith("Resources/")) resPath = resPath.Substring("Resources/".Length);
                if (resPath.EndsWith(".png") || resPath.EndsWith(".jpg"))
                    resPath = resPath.Substring(0, resPath.LastIndexOf('.'));

                tex = Resources.Load<Texture2D>(resPath);

#if UNITY_EDITOR && !UNITY_ANDROID
                if (tex == null)
                {
                    string fullPath = Path.Combine(Application.dataPath, texRelativePath);
                    if (File.Exists(fullPath))
                    {
                        byte[] bytes = File.ReadAllBytes(fullPath);
                        tex = new Texture2D(2, 2, TextureFormat.RGBA32, true);
                        tex.LoadImage(bytes);
                    }
                }
#endif
            }
            catch { }

            Shader shader = Shader.Find("Mobile/Diffuse") ?? Shader.Find("Standard") ?? Shader.Find("Unlit/Texture");
            Material mat = new Material(shader) { name = matName };
            if (tex != null)
            {
                tex.name = matName + "_Tex";
                tex.wrapMode = TextureWrapMode.Repeat;
                tex.filterMode = FilterMode.Bilinear;
                tex.anisoLevel = 4;
                mat.mainTexture = tex;
            }
            return mat;
        }

        private void BuildRoadSurface(Transform parent, Material roadMat)
        {
            GameObject roadObj = new GameObject("Stylized_Road_Mesh");
            roadObj.transform.SetParent(parent, false);

            MeshFilter mf = roadObj.AddComponent<MeshFilter>();
            MeshRenderer mr = roadObj.AddComponent<MeshRenderer>();
            if (roadMat != null) mr.sharedMaterial = roadMat;

            Mesh mesh = new Mesh { name = "Road_Mesh" };

            float halfW = roadWidth * 0.5f;
            float totalLen = endZ - startZ;
            int segmentsZ = Mathf.CeilToInt(totalLen / 2.0f);

            Vector3[] verts = new Vector3[(segmentsZ + 1) * 2];
            Vector2[] uvs = new Vector2[verts.Length];
            int[] tris = new int[segmentsZ * 6];

            for (int i = 0; i <= segmentsZ; i++)
            {
                float t = (float)i / segmentsZ;
                float z = Mathf.Lerp(startZ, endZ, t);
                float v = (z - startZ) / uvRepeatLength;

                int vi = i * 2;
                verts[vi] = new Vector3(-halfW, 0.002f, z);
                verts[vi + 1] = new Vector3(halfW, 0.002f, z);

                uvs[vi] = new Vector2(0f, v);
                uvs[vi + 1] = new Vector2(1f, v);

                if (i < segmentsZ)
                {
                    int ti = i * 6;
                    tris[ti] = vi;
                    tris[ti + 1] = vi + 2;
                    tris[ti + 2] = vi + 1;
                    tris[ti + 3] = vi + 1;
                    tris[ti + 4] = vi + 2;
                    tris[ti + 5] = vi + 3;
                }
            }

            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mf.sharedMesh = mesh;
        }

        private void BuildGuardrails(Transform parent, Material steelMat, Material amberMat)
        {
            GameObject railRoot = new GameObject("Guardrails_Container");
            railRoot.transform.SetParent(parent, false);

            float totalLen = endZ - startZ;
            float centerZ = (startZ + endZ) * 0.5f;

            // Dual Horizontal Rails: Left Side
            CreateRailBeam(railRoot.transform, "UpperRail_L", -guardrailX, 0.72f, centerZ, totalLen, steelMat);
            CreateRailBeam(railRoot.transform, "LowerRail_L", -guardrailX, 0.40f, centerZ, totalLen, steelMat);

            // Dual Horizontal Rails: Right Side
            CreateRailBeam(railRoot.transform, "UpperRail_R", guardrailX, 0.72f, centerZ, totalLen, steelMat);
            CreateRailBeam(railRoot.transform, "LowerRail_R", guardrailX, 0.40f, centerZ, totalLen, steelMat);

            // Vertical Posts with Amber Reflectors
            for (float z = startZ; z <= endZ; z += postSpacing)
            {
                CreatePostWithReflector(railRoot.transform, -guardrailX, z, steelMat, amberMat, isLeft: true);
                CreatePostWithReflector(railRoot.transform, guardrailX, z, steelMat, amberMat, isLeft: false);
            }
        }

        private void CreateRailBeam(Transform parent, string name, float posX, float posY, float centerZ, float length, Material mat)
        {
            GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beam.name = name;
            beam.transform.SetParent(parent, false);
            beam.transform.position = new Vector3(posX, posY, centerZ);
            beam.transform.localScale = new Vector3(0.10f, 0.22f, length);

            MeshRenderer mr = beam.GetComponent<MeshRenderer>();
            if (mr != null && mat != null) mr.sharedMaterial = mat;

            Collider c = beam.GetComponent<Collider>();
            SafeDestroy(c);
        }

        private void CreatePostWithReflector(Transform parent, float posX, float posZ, Material steelMat, Material amberMat, bool isLeft)
        {
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.name = isLeft ? "Post_L" : "Post_R";
            post.transform.SetParent(parent, false);
            post.transform.position = new Vector3(posX, 0.42f, posZ);
            post.transform.localScale = new Vector3(0.14f, 0.86f, 0.14f);

            MeshRenderer mrPost = post.GetComponent<MeshRenderer>();
            if (mrPost != null && steelMat != null) mrPost.sharedMaterial = steelMat;

            Collider c = post.GetComponent<Collider>();
            SafeDestroy(c);

            // Amber Reflector plate on front-facing side
            GameObject reflector = GameObject.CreatePrimitive(PrimitiveType.Cube);
            reflector.name = "AmberReflector";
            reflector.transform.SetParent(post.transform, false);
            reflector.transform.localPosition = new Vector3(0f, 0.18f, -0.58f);
            reflector.transform.localScale = new Vector3(0.65f, 0.22f, 0.18f);

            MeshRenderer mrReflector = reflector.GetComponent<MeshRenderer>();
            if (mrReflector != null && amberMat != null) mrReflector.sharedMaterial = amberMat;

            Collider rc = reflector.GetComponent<Collider>();
            SafeDestroy(rc);
        }

        private void BuildGrassBanks(Transform parent, Material grassMat)
        {
            GameObject banksRoot = new GameObject("GrassBanks_Landscape");
            banksRoot.transform.SetParent(parent, false);

            float totalLen = (endZ - startZ) + 80f;
            float centerZ = (startZ + endZ) * 0.5f;

            GameObject groundBase = GameObject.CreatePrimitive(PrimitiveType.Plane);
            groundBase.name = "Ground_Landscape_Plane";
            groundBase.transform.SetParent(banksRoot.transform, false);
            groundBase.transform.position = new Vector3(0, -0.05f, centerZ);
            groundBase.transform.localScale = new Vector3(60f, 1f, totalLen * 0.1f);

            MeshRenderer mrBase = groundBase.GetComponent<MeshRenderer>();
            if (mrBase != null && grassMat != null) mrBase.sharedMaterial = grassMat;

            Collider cBase = groundBase.GetComponent<Collider>();
            SafeDestroy(cBase);
        }

        private void BuildHighDetailNatureEnvironment(Transform parent, Material grassMat)
        {
            GameObject natureRoot = new GameObject("Nature_Canyon_And_Pine_Forest");
            natureRoot.transform.SetParent(parent, false);

            // 1. Place towering cliff rock monoliths and layered pine trees along both sides
            for (float z = startZ + 2f; z <= endZ - 4f; z += 6.5f)
            {
                // ================= LEFT BANK =================
                // A. Towering Cliff Rock Wall (forms continuous canyon edge right behind curb)
                if (cachedRockMesh != null)
                {
                    float rockXL = -(guardrailX + 1.8f + Random.Range(0f, 0.6f));
                    float rockScaleL = Random.Range(6.0f, 8.5f);
                    SpawnMeshProp(natureRoot.transform, "CliffRock_L", cachedRockMesh, cachedRockMat,
                        new Vector3(rockXL, 0f, z + Random.Range(-0.6f, 0.6f)),
                        new Vector3(rockScaleL * 0.7f, rockScaleL, rockScaleL * 0.7f),
                        Quaternion.Euler(0f, Random.Range(-25f, 25f), 0f));

                    // Small foreground boulder near the guardrail
                    if (Random.value > 0.35f)
                    {
                        float smallRockXL = -(guardrailX + 1.1f + Random.Range(0f, 0.4f));
                        SpawnMeshProp(natureRoot.transform, "Boulder_L", cachedRockMesh, cachedRockMat,
                            new Vector3(smallRockXL, 0f, z + 3.2f),
                            Vector3.one * Random.Range(2.0f, 3.0f),
                            Quaternion.Euler(Random.Range(-15f, 15f), Random.Range(0f, 360f), 0f));
                    }
                }

                // B. Stylized Pine Trees atop and behind the cliff rocks
                if (cachedTreeMesh != null)
                {
                    // Main towering tree on the cliff crest
                    float treeXL = -(guardrailX + 3.5f + Random.Range(0f, 1.8f));
                    float treeScaleL = Random.Range(6.5f, 9.5f);
                    SpawnMeshProp(natureRoot.transform, "PineTree_L", cachedTreeMesh, cachedTreeMat,
                        new Vector3(treeXL, Random.Range(1.2f, 3.0f), z + Random.Range(-1.0f, 1.0f)),
                        Vector3.one * treeScaleL,
                        Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));

                    // Secondary canopy tree deeper in the background
                    if (Random.value > 0.3f)
                    {
                        float treeXL2 = -(guardrailX + 6.2f + Random.Range(0f, 2.5f));
                        float treeScaleL2 = Random.Range(8.0f, 11.5f);
                        SpawnMeshProp(natureRoot.transform, "PineTree_Back_L", cachedTreeMesh, cachedTreeMat,
                            new Vector3(treeXL2, 2.5f, z + 2.5f),
                            Vector3.one * treeScaleL2,
                            Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                    }

                    // Young sapling tree growing near the road verge
                    if (Random.value > 0.45f)
                    {
                        float saplingXL = -(guardrailX + 1.3f + Random.Range(0f, 0.5f));
                        SpawnMeshProp(natureRoot.transform, "Sapling_L", cachedTreeMesh, cachedTreeMat,
                            new Vector3(saplingXL, 0f, z - 2.8f),
                            Vector3.one * Random.Range(2.5f, 3.6f),
                            Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                    }
                }

                // ================= RIGHT BANK =================
                // A. Towering Cliff Rock Wall
                if (cachedRockMesh != null)
                {
                    float rockXR = (guardrailX + 1.8f + Random.Range(0f, 0.6f));
                    float rockScaleR = Random.Range(6.0f, 8.5f);
                    SpawnMeshProp(natureRoot.transform, "CliffRock_R", cachedRockMesh, cachedRockMat,
                        new Vector3(rockXR, 0f, z + Random.Range(-0.6f, 0.6f)),
                        new Vector3(rockScaleR * 0.7f, rockScaleR, rockScaleR * 0.7f),
                        Quaternion.Euler(0f, Random.Range(-25f, 25f), 0f));

                    if (Random.value > 0.35f)
                    {
                        float smallRockXR = (guardrailX + 1.1f + Random.Range(0f, 0.4f));
                        SpawnMeshProp(natureRoot.transform, "Boulder_R", cachedRockMesh, cachedRockMat,
                            new Vector3(smallRockXR, 0f, z + 3.2f),
                            Vector3.one * Random.Range(2.0f, 3.0f),
                            Quaternion.Euler(Random.Range(-15f, 15f), Random.Range(0f, 360f), 0f));
                    }
                }

                // B. Stylized Pine Trees
                if (cachedTreeMesh != null)
                {
                    float treeXR = (guardrailX + 3.5f + Random.Range(0f, 1.8f));
                    float treeScaleR = Random.Range(6.5f, 9.5f);
                    SpawnMeshProp(natureRoot.transform, "PineTree_R", cachedTreeMesh, cachedTreeMat,
                        new Vector3(treeXR, Random.Range(1.2f, 3.0f), z + Random.Range(-1.0f, 1.0f)),
                        Vector3.one * treeScaleR,
                        Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));

                    if (Random.value > 0.3f)
                    {
                        float treeXR2 = (guardrailX + 6.2f + Random.Range(0f, 2.5f));
                        float treeScaleR2 = Random.Range(8.0f, 11.5f);
                        SpawnMeshProp(natureRoot.transform, "PineTree_Back_R", cachedTreeMesh, cachedTreeMat,
                            new Vector3(treeXR2, 2.5f, z + 2.5f),
                            Vector3.one * treeScaleR2,
                            Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                    }

                    if (Random.value > 0.45f)
                    {
                        float saplingXR = (guardrailX + 1.3f + Random.Range(0f, 0.5f));
                        SpawnMeshProp(natureRoot.transform, "Sapling_R", cachedTreeMesh, cachedTreeMat,
                            new Vector3(saplingXR, 0f, z - 2.8f),
                            Vector3.one * Random.Range(2.5f, 3.6f),
                            Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                    }
                }
            }
        }

        private static GameObject SpawnMeshProp(Transform parent, string name, Mesh mesh, Material mat, Vector3 pos, Vector3 scale, Quaternion rot)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.position = pos;
            obj.transform.localScale = scale;
            obj.transform.rotation = rot;

            MeshFilter mf = obj.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            MeshRenderer mr = obj.AddComponent<MeshRenderer>();
            if (mat != null) mr.sharedMaterial = mat;

            return obj;
        }

        private void BuildMountainHorizon(Transform parent, Material grassMat)
        {
            GameObject horizonRoot = new GameObject("DistantHorizon_Mountains");
            horizonRoot.transform.SetParent(parent, false);

            float ridgeZ = endZ + 45f;
            for (float x = -220f; x <= 220f; x += 35f)
            {
                GameObject peak = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                peak.name = "MountainPeak";
                peak.transform.SetParent(horizonRoot.transform, false);

                float h = Random.Range(22f, 38f);
                float radius = Random.Range(40f, 70f);

                peak.transform.position = new Vector3(x + Random.Range(-8f, 8f), h * 0.5f - 8f, ridgeZ + Random.Range(-10f, 15f));
                peak.transform.localScale = new Vector3(radius, h * 0.5f, radius);
                peak.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                SafeDestroy(peak.GetComponent<Collider>());
                if (grassMat != null) peak.GetComponent<MeshRenderer>().sharedMaterial = grassMat;
            }
        }

        public static void SetupCameraAndAtmosphere()
        {
            // Vibrant bright cartoon sky blue matching reference image
            Color skyBlue = new Color(0.25f, 0.58f, 0.95f, 1f);

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.transform.position = new Vector3(0f, 2.10f, -4.60f);
                mainCam.transform.rotation = Quaternion.Euler(11.0f, 0f, 0f);
                mainCam.fieldOfView = 58.0f;
                mainCam.farClipPlane = 450.0f;
                mainCam.backgroundColor = skyBlue;
                mainCam.clearFlags = CameraClearFlags.SolidColor;
            }

            RenderSettings.skybox = null;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = skyBlue;
            RenderSettings.fogStartDistance = 140.0f;
            RenderSettings.fogEndDistance = 290.0f;
        }

        private void BuildPlayerCharacter(Transform parent)
        {
            LoadMeshyAssets();

            // Destroy any previous player object to avoid duplicates
            GameObject[] oldPlayers = GameObject.FindGameObjectsWithTag("Player");
            foreach (var p in oldPlayers)
            {
                if (p != null) SafeDestroy(p);
            }

            GameObject playerRoot = new GameObject("Player_Hero");
            playerRoot.tag = "Player";
            playerRoot.transform.position = new Vector3(0f, 0f, 0f);
            playerRoot.transform.rotation = Quaternion.identity;

            // 1. Collider
            CapsuleCollider col = playerRoot.AddComponent<CapsuleCollider>();
            col.radius = 0.35f;
            col.height = 1.45f;
            col.center = new Vector3(0f, 0.725f, 0f);

            // 2. Rigidbody
            Rigidbody rb = playerRoot.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            // 3. Rigged Visual Mob Soldier with stationary Firing Rifle animation
            GameObject playerPrefab = null;
#if UNITY_EDITOR
            playerPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/Player_FiringRifle.fbx");
#endif
            if (playerPrefab == null)
            {
                playerPrefab = Resources.Load<GameObject>("Characters/Player_FiringRifle");
            }
            if (playerPrefab == null)
            {
                playerPrefab = Resources.Load<GameObject>("Characters/PlayerMob");
            }

            GameObject visualObj = null;
            if (playerPrefab != null)
            {
                visualObj = Object.Instantiate(playerPrefab, playerRoot.transform);
                visualObj.name = "Visual_Mob";
                visualObj.transform.localPosition = Vector3.zero;
                visualObj.transform.localRotation = Quaternion.identity;
                // Base mesh bounds are ~0.02m tall, scale 68x for clean proportional character height
                visualObj.transform.localScale = Vector3.one * 68.0f;
            }
            else
            {
                visualObj = new GameObject("Visual_Mob");
                visualObj.transform.SetParent(playerRoot.transform, false);
                visualObj.transform.localScale = Vector3.one * 68.0f;
            }

            // 4. Assign crisp HD PlayerMaterial to SkinnedMeshRenderer
            Material playerMat = null;
#if UNITY_EDITOR
            playerMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/Characters/Materials/PlayerMaterial.mat");
#endif
            if (playerMat == null)
            {
                playerMat = Resources.Load<Material>("Materials/PlayerMaterial");
            }
            if (playerMat == null && cachedPlayerMat != null)
            {
                playerMat = cachedPlayerMat;
            }

            var smr = visualObj.GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr != null && playerMat != null)
            {
                smr.sharedMaterial = playerMat;
            }

            // 5. Setup Animator for looping stationary firing rifle animation
            Animator anim = visualObj.GetComponent<Animator>();
            if (anim == null) anim = visualObj.AddComponent<Animator>();
            RuntimeAnimatorController animCtrl = Resources.Load<RuntimeAnimatorController>("Animations/MobAnimator");
#if UNITY_EDITOR
            if (animCtrl == null)
            {
                animCtrl = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/MobAnimator.controller");
            }
#endif
            if (anim != null && animCtrl != null)
            {
                anim.runtimeAnimatorController = animCtrl;
                anim.applyRootMotion = false;
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }

            // 6. Attach Meshy AI Assault Rifle firmly into hands on right side
            GameObject weaponObj = new GameObject("AssaultRifle_Meshy");
            weaponObj.transform.SetParent(playerRoot.transform, false);
            weaponObj.transform.localPosition = new Vector3(0.16f, 0.62f, 0.18f);
            weaponObj.transform.localRotation = Quaternion.identity;
            weaponObj.transform.localScale = Vector3.one * 0.92f;

            if (cachedRifleMesh == null)
            {
                GameObject riflePrefab = Resources.Load<GameObject>("Models/AssaultRifle_GameReady") ?? Resources.Load<GameObject>("Prefabs/Gun_M41D");
                if (riflePrefab != null) cachedRifleMesh = riflePrefab.GetComponentInChildren<MeshFilter>()?.sharedMesh;
#if UNITY_EDITOR && !UNITY_ANDROID
                if (cachedRifleMesh == null)
                {
                    riflePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Generated/AssaultRifle/AssaultRifle_GameReady.obj");
                    if (riflePrefab != null) cachedRifleMesh = riflePrefab.GetComponentInChildren<MeshFilter>()?.sharedMesh;
                }
                if (cachedRifleMesh == null) cachedRifleMesh = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Models/Generated/AssaultRifle/AssaultRifle_GameReady.obj");
                if (cachedRifleMesh == null)
                {
                    string rifleObjPath = Path.Combine(Application.dataPath, "Models/Generated/AssaultRifle/AssaultRifle_GameReady.obj");
                    cachedRifleMesh = LoadObjMeshDirect(rifleObjPath);
                }
#endif
            }
            if (cachedRifleMat == null)
            {
                cachedRifleMat = Resources.Load<Material>("Materials/M_MeshyAssaultRifle");
#if UNITY_EDITOR && !UNITY_ANDROID
                if (cachedRifleMat == null)
                {
                    cachedRifleMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/Generated/AssaultRifle/Materials/M_MeshyAssaultRifle.mat");
                }
#endif
                if (cachedRifleMat == null)
                {
                    cachedRifleMat = LoadTextureMaterial("Textures/Gun/AssaultRifle_base_color.png", "M_MeshyAssaultRifle");
                }
            }

            if (cachedRifleMesh != null)
            {
                MeshFilter wmf = weaponObj.AddComponent<MeshFilter>();
                wmf.sharedMesh = cachedRifleMesh;

                MeshRenderer wmr = weaponObj.AddComponent<MeshRenderer>();
                if (cachedRifleMat != null) wmr.sharedMaterial = cachedRifleMat;
            }

            // 7. Muzzle Fire Point at barrel tip
            GameObject muzzleObj = new GameObject("MuzzleFirePoint");
            muzzleObj.transform.SetParent(weaponObj.transform, false);
            muzzleObj.transform.localPosition = new Vector3(0f, 0.08f, 0.52f);
            muzzleObj.transform.localRotation = Quaternion.identity;

            // 8. Attach & configure PlayerRunnerController
            MobControl.Combat.PlayerRunnerController runner = playerRoot.AddComponent<MobControl.Combat.PlayerRunnerController>();
            runner.EnsurePlayerMaterial();
            runner.EnsureEquippedWeapon();

            // 9. Attach PlayerMobVisualController for weapon recoil & muzzle burst
            MobControl.Combat.PlayerMobVisualController visualCtrl = visualObj.AddComponent<MobControl.Combat.PlayerMobVisualController>();
            visualCtrl.Initialize(runner, weaponObj.transform, muzzleObj.transform);

            // 10. Connect Camera
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                MobControl.Core.SmoothCameraFollow camFollow = mainCam.GetComponent<MobControl.Core.SmoothCameraFollow>();
                if (camFollow == null) camFollow = mainCam.gameObject.AddComponent<MobControl.Core.SmoothCameraFollow>();
                camFollow.SetTarget(playerRoot.transform);
                camFollow.SetCameraParameters(new Vector3(0f, 5.80f, -6.80f), new Vector3(29.0f, 0f, 0f));
            }

            Debug.Log("[NewRoadBuilder] Stationary Player Mob (Firing Rifle animation) & Assault Rifle spawned with X-axis lane control!");
        }

        private void BuildZombieRoadSpawner(Transform parent)
        {
            GameObject spawnerObj = new GameObject("ZombieRoadWaveSpawner");
            spawnerObj.transform.SetParent(parent, false);
            spawnerObj.transform.position = new Vector3(0f, 0f, 0f);

            var spawner = spawnerObj.AddComponent<MobControl.Crowd.ZombieRoadWaveSpawner>();
            if (Application.isPlaying)
            {
                spawner.LoadPrefabsIfMissing();
                spawner.RegisterZombiePools();
            }

            Debug.Log("[NewRoadBuilder] ZombieRoadWaveSpawner attached with 3 Meshy 3D Zombie types (Small, Moderate, Boss)!");
        }

        private void BuildShippingContainersAndObstacles(Transform parent)
        {
            // 1. Primary Destructible Shipping Container Gate (Image 1 & 2 Match: Z = 26m, 30 bullets to destroy, multiplier = x2)
            BuildShippingContainerGate(parent, 26.0f, 2, 30, new Color(0.85f, 0.16f, 0.14f), "Textures/tex_shipping_container_red.png");

            // Secondary container gates omitted for Level 1 matching Image 2 sketch (open road to bridge gate)

            // 3. Road Barricades, barrels and crates (Image 1 match)
            BuildRoadProps(parent);
        }

        private GameObject BuildShippingContainerGate(Transform parent, float zPos, int multiplier, int maxHp, Color bodyColor, string texPath)
        {
            GameObject gateRoot = new GameObject($"DestructibleGate_Container_x{multiplier}");
            gateRoot.transform.SetParent(parent, false);
            gateRoot.transform.position = new Vector3(0f, 1.10f, zPos);

            // 1. BoxCollider for bullet hit detection & zombie blocking
            BoxCollider boxCol = gateRoot.AddComponent<BoxCollider>();
            boxCol.isTrigger = true;
            boxCol.size = new Vector3(5.2f, 2.2f, 1.4f);
            boxCol.center = Vector3.zero;

            // 2. DestructibleGate component
            DestructibleGate dGate = gateRoot.AddComponent<DestructibleGate>();
            dGate.SetGateStats(maxHp, multiplier, MobControl.Gates.GateOperation.Multiply, 5.2f);

            // 3. Container Body Mesh (Corrugated Industrial Red Steel)
            GameObject bodyObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bodyObj.name = "ContainerBody";
            bodyObj.transform.SetParent(gateRoot.transform, false);
            bodyObj.transform.localPosition = Vector3.zero;
            bodyObj.transform.localScale = new Vector3(4.8f, 2.05f, 1.3f);
            SafeDestroy(bodyObj.GetComponent<Collider>());

            Material containerMat = LoadTextureMaterial(texPath, $"M_Container_x{multiplier}");
            if (containerMat != null)
            {
                containerMat.color = bodyColor;
                bodyObj.GetComponent<MeshRenderer>().sharedMaterial = containerMat;
            }

            // 4. Hazard Striped Side Posts (Left & Right)
            Material hazardMat = LoadTextureMaterial("Textures/tex_hazard_stripes.png", "M_HazardStripes");
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                post.name = side < 0 ? "HazardPost_L" : "HazardPost_R";
                post.transform.SetParent(gateRoot.transform, false);
                post.transform.localPosition = new Vector3(side * 2.55f, 0f, 0f);
                post.transform.localScale = new Vector3(0.35f, 2.2f, 1.38f);
                SafeDestroy(post.GetComponent<Collider>());
                if (hazardMat != null) post.GetComponent<MeshRenderer>().sharedMaterial = hazardMat;

                // Hazard Beacon Light on top of each post
                GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                beacon.name = "BeaconLight";
                beacon.transform.SetParent(post.transform, false);
                beacon.transform.localPosition = new Vector3(0f, 0.58f, 0f);
                beacon.transform.localScale = new Vector3(0.55f, 0.16f, 0.55f);
                SafeDestroy(beacon.GetComponent<Collider>());
                Material beaconMat = new Material(Shader.Find("Standard"));
                beaconMat.color = new Color(1f, 0.2f, 0.05f, 1f);
                beaconMat.EnableKeyword("_EMISSION");
                beaconMat.SetColor("_EmissionColor", new Color(2.5f, 0.3f, 0.1f));
                beacon.GetComponent<MeshRenderer>().sharedMaterial = beaconMat;
            }

            // 5. Electric Cyan Multiplier Badge on front face
            GameObject badgeObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            badgeObj.name = "MultiplierBadge";
            badgeObj.transform.SetParent(gateRoot.transform, false);
            badgeObj.transform.localPosition = new Vector3(0f, 0.05f, -0.68f);
            badgeObj.transform.localScale = new Vector3(2.4f, 1.25f, 1f);
            SafeDestroy(badgeObj.GetComponent<Collider>());

            Material badgeMat = LoadTextureMaterial("Textures/tex_blue_multiplier_badge.png", "M_BlueMultiplierBadge");
            if (badgeMat != null)
            {
                badgeObj.GetComponent<MeshRenderer>().sharedMaterial = badgeMat;
            }

            // 3D Text: "x2" inside badge
            GameObject textObj = new GameObject("MultiplierText");
            textObj.transform.SetParent(gateRoot.transform, false);
            textObj.transform.localPosition = new Vector3(0f, 0.05f, -0.72f);
            TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
            tmp.text = $"x{multiplier}";
            tmp.fontSize = 8.5f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;

            // 6. Overhead Hits / Health Text
            GameObject hpObj = new GameObject("HealthText");
            hpObj.transform.SetParent(gateRoot.transform, false);
            hpObj.transform.localPosition = new Vector3(0f, 1.45f, 0f);
            TextMeshPro hpTmp = hpObj.AddComponent<TextMeshPro>();
            hpTmp.text = $"<color=#FFE600><b>{maxHp}</b></color> <size=75%>/ {maxHp}</size>";
            hpTmp.fontSize = 4.8f;
            hpTmp.alignment = TextAlignmentOptions.Center;
            hpTmp.fontStyle = FontStyles.Bold;

            return gateRoot;
        }

        private void BuildRoadProps(Transform parent)
        {
            GameObject propsRoot = new GameObject("RoadProps_Level1");
            propsRoot.transform.SetParent(parent, false);

            Material redBarrelMat = new Material(Shader.Find("Standard"));
            redBarrelMat.color = new Color(0.85f, 0.18f, 0.12f);

            Material woodMat = new Material(Shader.Find("Standard"));
            woodMat.color = new Color(0.65f, 0.45f, 0.28f);

            Material stripeMat = LoadTextureMaterial("Textures/tex_hazard_stripes.png", "M_HazardBarricade");

            // Red Oil/Explosive Barrels
            SpawnBarrel(propsRoot.transform, new Vector3(0.8f, 0.65f, 18.0f), redBarrelMat);
            SpawnBarrel(propsRoot.transform, new Vector3(0.95f, 0.65f, 19.1f), redBarrelMat);
            SpawnBarrel(propsRoot.transform, new Vector3(-0.6f, 0.65f, 35.0f), redBarrelMat);

            // Barricades (Red & White striped wooden barricade boards)
            SpawnBarricade(propsRoot.transform, new Vector3(-1.4f, 0.55f, 20.5f), stripeMat);
            SpawnBarricade(propsRoot.transform, new Vector3(1.4f, 0.55f, 37.0f), stripeMat);

            // Wooden Crates stacked by the guardrails
            SpawnCrate(propsRoot.transform, new Vector3(-2.45f, 0.45f, 11.0f), woodMat);
            SpawnCrate(propsRoot.transform, new Vector3(-2.45f, 1.25f, 11.0f), woodMat);
            SpawnCrate(propsRoot.transform, new Vector3(2.45f, 0.45f, 22.0f), woodMat);
            SpawnCrate(propsRoot.transform, new Vector3(2.45f, 0.45f, 33.0f), woodMat);
        }

        private void SpawnBarrel(Transform parent, Vector3 pos, Material mat)
        {
            GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.name = "Barrel_Red";
            barrel.transform.SetParent(parent, false);
            barrel.transform.position = pos;
            barrel.transform.localScale = new Vector3(0.65f, 0.65f, 0.65f);
            SafeDestroy(barrel.GetComponent<Collider>());
            if (mat != null) barrel.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private void SpawnBarricade(Transform parent, Vector3 pos, Material mat)
        {
            GameObject barricade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barricade.name = "Barricade_Hazard";
            barricade.transform.SetParent(parent, false);
            barricade.transform.position = pos;
            barricade.transform.localScale = new Vector3(1.6f, 0.70f, 0.15f);
            SafeDestroy(barricade.GetComponent<Collider>());
            if (mat != null) barricade.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private void SpawnCrate(Transform parent, Vector3 pos, Material mat)
        {
            GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.name = "SupplyCrate";
            crate.transform.SetParent(parent, false);
            crate.transform.position = pos;
            crate.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
            SafeDestroy(crate.GetComponent<Collider>());
            if (mat != null) crate.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        public static Transform FindBoneRecursive(Transform parent, string boneName)
        {
            if (parent.name.IndexOf(boneName, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindBoneRecursive(parent.GetChild(i), boneName);
                if (found != null) return found;
            }
            return null;
        }

        [ContextMenu("Clear Road")]
        public void ClearRoad()
        {
            GameObject root = GameObject.Find(ROOT_NAME);
            SafeDestroy(root);
        }
    }
}
