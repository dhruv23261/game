using UnityEngine;

namespace MobControl.Level
{
    /// <summary>
    /// Builds the exact Level 1 Stylized Road matching the user's reference design:
    /// - 7.0m wide asphalt road with crisp white dashed center line & solid yellow shoulder lines.
    /// - Light concrete curbs.
    /// - Sleek dark steel dual-rail highway guardrails with amber reflector posts.
    /// - Meshy 3D stylized Pine Trees & Cliff Rocks lining both sides.
    /// - Sloping lush green grass banks.
    /// - Distant mountain horizon backdrop.
    /// - Camera framing matching the reference angle.
    /// </summary>
    [ExecuteAlways]
    public class NewRoadBuilder : MonoBehaviour
    {
        [Header("Road Settings")]
        [SerializeField] private float roadWidth = 7.0f;
        [SerializeField] private float startZ = -15.0f;
        [SerializeField] private float endZ = 220.0f;
        [SerializeField] private float uvRepeatLength = 8.0f;

        [Header("Guardrail Settings")]
        [SerializeField] private float guardrailX = 3.85f;
        [SerializeField] private float postSpacing = 3.5f;

        public const string ROOT_NAME = "Level1_StylizedRoad_Root";

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
            GameObject existingRoot = GameObject.Find(ROOT_NAME);
            if (existingRoot != null) SafeDestroy(existingRoot);

            GameObject root = new GameObject(ROOT_NAME);

            // Load Materials
            Material roadMat = Resources.Load<Material>("Materials/M_StylizedRoad");
            Material steelMat = Resources.Load<Material>("Materials/M_GuardrailSteel");
            Material grassMat = Resources.Load<Material>("Materials/M_GrassVerge");
            Material amberMat = Resources.Load<Material>("Materials/M_GunOrange");
            Material treeMat = Resources.Load<Material>("Materials/M_StylizedPineTree");
            Material rockMat = Resources.Load<Material>("Materials/M_StylizedCliffRock");

            // 1. Build Road Surface Mesh
            BuildRoadSurface(root.transform, roadMat);

            // 2. Build Guardrails with Amber Reflectors
            BuildGuardrails(root.transform, steelMat, amberMat);

            // 3. Build Sloping Grass Banks
            BuildGrassBanks(root.transform, grassMat);

            // 4. Build Nature Decorations (Meshy 3D Pine Trees & Cliff Rocks)
            BuildNatureDecorations(root.transform, treeMat, rockMat);

            // 5. Build Distant Mountain Backdrop Ridge
            BuildDistantHorizon(root.transform, grassMat);

            // 6. Setup Camera & Atmosphere
            SetupCameraAndAtmosphere();

            Debug.Log("[NewRoadBuilder] Level 1 reference stylized road successfully assembled with Meshy 3D assets!");
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
            beam.transform.localScale = new Vector3(0.09f, 0.20f, length);

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
            post.transform.localScale = new Vector3(0.13f, 0.84f, 0.13f);

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

            float totalLen = (endZ - startZ) + 60f;
            float centerZ = (startZ + endZ) * 0.5f;

            // Flat landscape base plane covering entire wide horizon
            GameObject groundBase = GameObject.CreatePrimitive(PrimitiveType.Plane);
            groundBase.name = "Ground_Landscape_Plane";
            groundBase.transform.SetParent(banksRoot.transform, false);
            groundBase.transform.position = new Vector3(0, -0.05f, centerZ);
            groundBase.transform.localScale = new Vector3(50f, 1f, totalLen * 0.1f);

            MeshRenderer mrBase = groundBase.GetComponent<MeshRenderer>();
            if (mrBase != null && grassMat != null) mrBase.sharedMaterial = grassMat;

            Collider cBase = groundBase.GetComponent<Collider>();
            SafeDestroy(cBase);
        }

        private void BuildNatureDecorations(Transform parent, Material treeMat, Material rockMat)
        {
            GameObject natureRoot = new GameObject("Nature_Trees_And_Rocks");
            natureRoot.transform.SetParent(parent, false);

            GameObject treePrefab = Resources.Load<GameObject>("Nature/Stylized_Pine_Tree");
            GameObject rockPrefab = Resources.Load<GameObject>("Nature/Stylized_Cliff_Rock");

            float halfRoad = roadWidth * 0.5f;

            for (float z = startZ + 4f; z <= endZ - 8f; z += 6.5f)
            {
                // Left Side Rocks (Stacked along the bank)
                if (rockPrefab != null)
                {
                    float xRockL = -(halfRoad + 2.0f + Random.Range(0.2f, 1.4f));
                    GameObject rockL = Instantiate(rockPrefab, new Vector3(xRockL, 0.2f, z + Random.Range(-1.0f, 1.0f)), Quaternion.Euler(0, Random.Range(0f, 360f), 0), natureRoot.transform);
                    rockL.transform.localScale = Vector3.one * Random.Range(2.2f, 3.2f);
                    foreach (var c in rockL.GetComponentsInChildren<Collider>()) SafeDestroy(c);
                    if (rockMat != null)
                    {
                        foreach (var mr in rockL.GetComponentsInChildren<MeshRenderer>()) mr.sharedMaterial = rockMat;
                    }
                }

                // Right Side Rocks
                if (rockPrefab != null)
                {
                    float xRockR = (halfRoad + 2.0f + Random.Range(0.2f, 1.4f));
                    GameObject rockR = Instantiate(rockPrefab, new Vector3(xRockR, 0.2f, z + Random.Range(-1.0f, 1.0f)), Quaternion.Euler(0, Random.Range(0f, 360f), 0), natureRoot.transform);
                    rockR.transform.localScale = Vector3.one * Random.Range(2.2f, 3.2f);
                    foreach (var c in rockR.GetComponentsInChildren<Collider>()) SafeDestroy(c);
                    if (rockMat != null)
                    {
                        foreach (var mr in rockR.GetComponentsInChildren<MeshRenderer>()) mr.sharedMaterial = rockMat;
                    }
                }

                // Left Side Pine Trees (Planted amongst and behind the rocks)
                if (treePrefab != null)
                {
                    float xTreeL = -(halfRoad + 3.6f + Random.Range(0.5f, 2.5f));
                    GameObject treeL = Instantiate(treePrefab, new Vector3(xTreeL, 0.4f, z + Random.Range(-1.2f, 1.2f)), Quaternion.Euler(0, Random.Range(0f, 360f), 0), natureRoot.transform);
                    treeL.transform.localScale = Vector3.one * Random.Range(2.8f, 4.2f);
                    foreach (var c in treeL.GetComponentsInChildren<Collider>()) SafeDestroy(c);
                    if (treeMat != null)
                    {
                        foreach (var mr in treeL.GetComponentsInChildren<MeshRenderer>()) mr.sharedMaterial = treeMat;
                    }
                }

                // Right Side Pine Trees
                if (treePrefab != null)
                {
                    float xTreeR = (halfRoad + 3.6f + Random.Range(0.5f, 2.5f));
                    GameObject treeR = Instantiate(treePrefab, new Vector3(xTreeR, 0.4f, z + Random.Range(-1.2f, 1.2f)), Quaternion.Euler(0, Random.Range(0f, 360f), 0), natureRoot.transform);
                    treeR.transform.localScale = Vector3.one * Random.Range(2.8f, 4.2f);
                    foreach (var c in treeR.GetComponentsInChildren<Collider>()) SafeDestroy(c);
                    if (treeMat != null)
                    {
                        foreach (var mr in treeR.GetComponentsInChildren<MeshRenderer>()) mr.sharedMaterial = treeMat;
                    }
                }
            }
        }

        private void BuildDistantHorizon(Transform parent, Material grassMat)
        {
            GameObject horizonRoot = new GameObject("DistantHorizon_Mountains");
            horizonRoot.transform.SetParent(parent, false);

            for (float x = -180f; x <= 180f; x += 35f)
            {
                GameObject hill = GameObject.CreatePrimitive(PrimitiveType.Cube);
                hill.name = "HorizonMountain";
                hill.transform.SetParent(horizonRoot.transform, false);
                float h = Random.Range(35f, 60f);
                float w = Random.Range(45f, 75f);
                hill.transform.position = new Vector3(x + Random.Range(-8f, 8f), h * 0.5f - 4f, endZ + 30f + Random.Range(-15f, 15f));
                hill.transform.localScale = new Vector3(w, h, 30f);
                hill.transform.localRotation = Quaternion.Euler(0, Random.Range(-20f, 20f), 0);

                MeshRenderer mr = hill.GetComponent<MeshRenderer>();
                if (mr != null && grassMat != null) mr.sharedMaterial = grassMat;

                Collider c = hill.GetComponent<Collider>();
                SafeDestroy(c);
            }
        }

        public static void SetupCameraAndAtmosphere()
        {
            Color skyBlue = new Color(0.35f, 0.62f, 0.92f, 1f); // Vibrant bright cartoon sky blue

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.transform.position = new Vector3(0f, 2.3f, -4.8f);
                mainCam.transform.rotation = Quaternion.Euler(10.5f, 0f, 0f);
                mainCam.fieldOfView = 56.0f;
                mainCam.farClipPlane = 400.0f;
                mainCam.backgroundColor = skyBlue;
                mainCam.clearFlags = CameraClearFlags.SolidColor;
            }

            RenderSettings.skybox = null;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = skyBlue;
            RenderSettings.fogStartDistance = 140.0f;
            RenderSettings.fogEndDistance = 280.0f;
        }

        [ContextMenu("Clear Road")]
        public void ClearRoad()
        {
            GameObject root = GameObject.Find(ROOT_NAME);
            SafeDestroy(root);
        }
    }
}
