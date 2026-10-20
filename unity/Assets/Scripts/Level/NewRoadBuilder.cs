using UnityEngine;
using System.Collections.Generic;
using System.IO;
using TMPro;

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

            // 1. Build Grand Asphalt Highway Mesh
            BuildRoadSurface(root.transform, roadMat);

            // 2. Build Dual-Rail Steel Guardrails with Amber Reflectors
            BuildGuardrails(root.transform, steelMat, amberMat);

            // 3. Build Sloping Grass Landscape Verge
            BuildGrassBanks(root.transform, grassMat);

            // 4. Build Towering Cliff Canyon Walls & Pine Forest (1:1 Reference Match)
            BuildHighDetailNatureEnvironment(root.transform, grassMat);

            // 5. Build Sweeping Distant Mountain Horizon Ridge
            BuildMountainHorizon(root.transform, grassMat);

            // 6. Setup Exact Camera & Atmospheric Horizon Fog
            SetupCameraAndAtmosphere();

            Debug.Log("[NewRoadBuilder] Level 1 reference stylized road successfully assembled with High-Definition Meshy 3D nature assets!");
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
                if (go != null && (go.name.Contains("Gun_M41D") || go.name.Contains("MultiplierGate") || go.name.Contains("DestructibleGate")))
                {
                    SafeDestroy(go);
                }
            }
        }

        private void LoadMeshyAssets()
        {
            // 1. Load Pine Tree Mesh & Material
            if (cachedTreeMesh == null)
            {
                string treeObjPath = Path.Combine(Application.dataPath, "Models/Nature/PineTree_V2.obj");
                cachedTreeMesh = LoadObjMeshDirect(treeObjPath);
            }
            if (cachedTreeMat == null)
            {
                cachedTreeMat = LoadTextureMaterial("Models/Nature/PineTree_V2_textures/base_color.png", "M_MeshyPineTree_V2");
            }

            // 2. Load Cliff Rock Mesh & Material
            if (cachedRockMesh == null)
            {
                string rockObjPath = Path.Combine(Application.dataPath, "Models/Nature/CliffRock_V2.obj");
                cachedRockMesh = LoadObjMeshDirect(rockObjPath);
            }
            if (cachedRockMat == null)
            {
                cachedRockMat = LoadTextureMaterial("Models/Nature/CliffRock_V2_textures/base_color.png", "M_MeshyCliffRock_V2");
            }
        }

        private static Mesh LoadObjMeshDirect(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[NewRoadBuilder] OBJ file not found: {filePath}");
                return null;
            }

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
            string fullPath = Path.Combine(Application.dataPath, texRelativePath);
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning($"[NewRoadBuilder] Texture not found at {fullPath}");
                return null;
            }

            byte[] bytes = File.ReadAllBytes(fullPath);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, true);
            if (tex.LoadImage(bytes))
            {
                tex.name = matName + "_Tex";
                tex.wrapMode = TextureWrapMode.Repeat;
                tex.filterMode = FilterMode.Bilinear;
                tex.anisoLevel = 4;

                Shader shader = Shader.Find("Mobile/Diffuse") ?? Shader.Find("Standard") ?? Shader.Find("Unlit/Texture");
                Material mat = new Material(shader) { name = matName };
                mat.mainTexture = tex;
                return mat;
            }
            return null;
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

            // Step along road with dense 4.5m spacing to form a continuous, lush corridor
            // Seed Random so placement is consistent and perfectly reproducible
            Random.InitState(424242);

            for (float z = startZ - 5f; z <= endZ + 15f; z += 4.5f)
            {
                // Alternating cadence: rocks and trees alternate prominently along the road verge
                bool rockPrimary = ((int)(z / 4.5f) % 2 == 0);

                // ================= LEFT BANK (Close to Road & Prominent) =================
                // 1. Towering Cliff Rock Monolith (Placed right behind guardrail)
                if (cachedRockMesh != null)
                {
                    float rockOffset = rockPrimary ? 0.55f : 1.20f;
                    float rockXL = -(guardrailX + rockOffset + Random.Range(0f, 0.35f));
                    float rScaleW = Random.Range(9.5f, 13.5f);
                    float rScaleH = Random.Range(12.0f, 16.5f);
                    float rScaleD = Random.Range(9.5f, 13.5f);

                    SpawnMeshProp(natureRoot.transform, "CliffRock_L", cachedRockMesh, cachedRockMat,
                        new Vector3(rockXL, -0.1f, z + Random.Range(-0.4f, 0.4f)),
                        new Vector3(rScaleW, rScaleH, rScaleD),
                        Quaternion.Euler(0f, Random.Range(-30f, 30f), 0f));

                    // Roadside Boulder nestled right at guardrail verge
                    if (Random.value > 0.40f)
                    {
                        float boulderXL = -(guardrailX + 0.45f + Random.Range(0f, 0.30f));
                        SpawnMeshProp(natureRoot.transform, "Boulder_L", cachedRockMesh, cachedRockMat,
                            new Vector3(boulderXL, 0f, z + 2.2f),
                            Vector3.one * Random.Range(4.0f, 6.2f),
                            Quaternion.Euler(Random.Range(-15f, 15f), Random.Range(0f, 360f), 0f));
                    }
                }

                // 2. Stylized Pine Trees (Framing the road prominently)
                if (cachedTreeMesh != null)
                {
                    // Front-Row Prominent Pine Tree (Right at road verge)
                    float treeOffset = rockPrimary ? 1.10f : 0.65f;
                    float treeXL = -(guardrailX + treeOffset + Random.Range(0f, 0.40f));
                    float treeScaleL = Random.Range(15.0f, 21.0f);
                    SpawnMeshProp(natureRoot.transform, "PineTree_Front_L", cachedTreeMesh, cachedTreeMat,
                        new Vector3(treeXL, Random.Range(0f, 0.4f), z + (rockPrimary ? 2.2f : 0f)),
                        Vector3.one * treeScaleL,
                        Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));

                    // Crest Pine Tree atop cliff rock / second row
                    float crestXL = -(guardrailX + 2.4f + Random.Range(0.2f, 1.2f));
                    float crestScaleL = Random.Range(17.0f, 25.0f);
                    SpawnMeshProp(natureRoot.transform, "PineTree_Crest_L", cachedTreeMesh, cachedTreeMat,
                        new Vector3(crestXL, Random.Range(1.8f, 4.0f), z + 1.5f),
                        Vector3.one * crestScaleL,
                        Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));

                    // Dense Deep Background Forest Canopy Tree
                    if (Random.value > 0.30f)
                    {
                        float backXL = -(guardrailX + 4.5f + Random.Range(0.5f, 2.5f));
                        float backScaleL = Random.Range(22.0f, 32.0f);
                        SpawnMeshProp(natureRoot.transform, "PineTree_Forest_L", cachedTreeMesh, cachedTreeMat,
                            new Vector3(backXL, Random.Range(3.5f, 6.5f), z + 2.5f),
                            Vector3.one * backScaleL,
                            Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                    }

                    // Roadside Young Sapling Fir Tree right outside guardrail post
                    if (Random.value > 0.45f)
                    {
                        float saplingXL = -(guardrailX + 0.50f + Random.Range(0f, 0.30f));
                        SpawnMeshProp(natureRoot.transform, "Sapling_L", cachedTreeMesh, cachedTreeMat,
                            new Vector3(saplingXL, 0f, z - 1.8f),
                            Vector3.one * Random.Range(6.5f, 9.5f),
                            Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                    }
                }

                // ================= RIGHT BANK (Close to Road & Prominent) =================
                // 1. Towering Cliff Rock Monolith (Right bank)
                if (cachedRockMesh != null)
                {
                    float rockOffset = (!rockPrimary) ? 0.55f : 1.20f;
                    float rockXR = (guardrailX + rockOffset + Random.Range(0f, 0.35f));
                    float rScaleW = Random.Range(9.5f, 13.5f);
                    float rScaleH = Random.Range(12.0f, 16.5f);
                    float rScaleD = Random.Range(9.5f, 13.5f);

                    SpawnMeshProp(natureRoot.transform, "CliffRock_R", cachedRockMesh, cachedRockMat,
                        new Vector3(rockXR, -0.1f, z + Random.Range(-0.4f, 0.4f)),
                        new Vector3(rScaleW, rScaleH, rScaleD),
                        Quaternion.Euler(0f, Random.Range(-30f, 30f), 0f));

                    // Roadside Boulder
                    if (Random.value > 0.40f)
                    {
                        float boulderXR = (guardrailX + 0.45f + Random.Range(0f, 0.30f));
                        SpawnMeshProp(natureRoot.transform, "Boulder_R", cachedRockMesh, cachedRockMat,
                            new Vector3(boulderXR, 0f, z + 2.2f),
                            Vector3.one * Random.Range(4.0f, 6.2f),
                            Quaternion.Euler(Random.Range(-15f, 15f), Random.Range(0f, 360f), 0f));
                    }
                }

                // 2. Stylized Pine Trees (Right bank)
                if (cachedTreeMesh != null)
                {
                    float treeOffset = (!rockPrimary) ? 1.10f : 0.65f;
                    float treeXR = (guardrailX + treeOffset + Random.Range(0f, 0.40f));
                    float treeScaleR = Random.Range(15.0f, 21.0f);
                    SpawnMeshProp(natureRoot.transform, "PineTree_Front_R", cachedTreeMesh, cachedTreeMat,
                        new Vector3(treeXR, Random.Range(0f, 0.4f), z + ((!rockPrimary) ? 2.2f : 0f)),
                        Vector3.one * treeScaleR,
                        Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));

                    float crestXR = (guardrailX + 2.4f + Random.Range(0.2f, 1.2f));
                    float crestScaleR = Random.Range(17.0f, 25.0f);
                    SpawnMeshProp(natureRoot.transform, "PineTree_Crest_R", cachedTreeMesh, cachedTreeMat,
                        new Vector3(crestXR, Random.Range(1.8f, 4.0f), z + 1.5f),
                        Vector3.one * crestScaleR,
                        Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));

                    if (Random.value > 0.30f)
                    {
                        float backXR = (guardrailX + 4.5f + Random.Range(0.5f, 2.5f));
                        float backScaleR = Random.Range(22.0f, 32.0f);
                        SpawnMeshProp(natureRoot.transform, "PineTree_Forest_R", cachedTreeMesh, cachedTreeMat,
                            new Vector3(backXR, Random.Range(3.5f, 6.5f), z + 2.5f),
                            Vector3.one * backScaleR,
                            Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                    }

                    if (Random.value > 0.45f)
                    {
                        float saplingXR = (guardrailX + 0.50f + Random.Range(0f, 0.30f));
                        SpawnMeshProp(natureRoot.transform, "Sapling_R", cachedTreeMesh, cachedTreeMat,
                            new Vector3(saplingXR, 0f, z - 1.8f),
                            Vector3.one * Random.Range(6.5f, 9.5f),
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
                mainCam.transform.position = new Vector3(0f, 2.05f, -4.2f);
                mainCam.transform.rotation = Quaternion.Euler(8.6f, 0f, 0f);
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

        [ContextMenu("Clear Road")]
        public void ClearRoad()
        {
            GameObject root = GameObject.Find(ROOT_NAME);
            SafeDestroy(root);
        }
    }
}
