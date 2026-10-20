using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace MobControl.Level
{
    /// <summary>
    /// Builds a fresh, clean, stylized 3D road for Level 1:
    /// - Hides/deactivates legacy road.obj instances.
    /// - Generates seamless asphalt road with yellow curbs and white center stripes.
    /// - Adds 3D highway guardrails (posts & horizontal rails) on both sides.
    /// - Adds roadside grass banks.
    /// - Spawns stylized low-poly Pine Trees and Rocks along both sides.
    /// - Adds Level 1 Start Line with overhead banner at Z = 0.
    /// - Adds Level 1 Red Finish Line & 3D Finish Arch Banner at Z = 120.
    /// </summary>
    [ExecuteAlways]
    public class NewRoadBuilder : MonoBehaviour
    {
        [Header("Road Dimensions")]
        [SerializeField] private float roadWidth = 6.0f;
        [SerializeField] private float startZ = -80.0f;
        [SerializeField] private float endZ = 450.0f;
        [SerializeField] private float roadY = 0.005f;

        [Header("Level Markers")]
        [SerializeField] private float level1StartZ = 0.0f;
        [SerializeField] private float level1FinishZ = 120.0f;

        [Header("Guardrail Settings")]
        [SerializeField] private bool buildGuardrails = true;
        [SerializeField] private float guardrailHeight = 0.65f;
        [SerializeField] private float postSpacing = 4.0f;

        [Header("Nature Props (Pine Trees & Rocks)")]
        [SerializeField] private bool buildTreesAndRocks = true;
        [SerializeField] private float treeSpacingZ = 6.5f;

        [Header("Materials (Auto-loaded from Resources if empty)")]
        [SerializeField] private Material roadMaterial;
        [SerializeField] private Material steelMaterial;
        [SerializeField] private Material grassMaterial;
        [SerializeField] private Material pineFoliageMaterial;
        [SerializeField] private Material pineFoliageLightMaterial;
        [SerializeField] private Material pineBarkMaterial;
        [SerializeField] private Material rockMaterial;
        [SerializeField] private Material startLineMaterial;
        [SerializeField] private Material finishBannerMaterial;
        [SerializeField] private Material finishTrussMaterial;

        private static NewRoadBuilder instance;
        public static NewRoadBuilder Instance => instance;

        private const string NEW_ROAD_ROOT_NAME = "NewRoad_Level1_Root";

        // Shared procedural cone meshes for pine trees
        private static Mesh sharedConeTier1;
        private static Mesh sharedConeTier2;
        private static Mesh sharedConeTier3;

        private void Awake()
        {
            instance = this;
            DeactivateOldRoad();
        }

        private void OnEnable()
        {
            DeactivateOldRoad();
        }

        private void Start()
        {
            DeactivateOldRoad();
            if (Application.isPlaying)
            {
                EnsureNewRoad();
            }
        }

        /// <summary>
        /// Called when Level 1 is started: disables old road and ensures new road is active.
        /// </summary>
        public static void EnsureNewRoad()
        {
            DeactivateOldRoad();

            GameObject existingRoot = GameObject.Find(NEW_ROAD_ROOT_NAME);
            if (existingRoot != null)
            {
                existingRoot.SetActive(true);
                foreach (Transform child in existingRoot.transform)
                {
                    if (child != null) child.gameObject.SetActive(true);
                }
                return;
            }

            GameObject builderObj = GameObject.Find("NewRoadBuilder_Manager");
            if (builderObj == null)
            {
                builderObj = new GameObject("NewRoadBuilder_Manager");
            }
            NewRoadBuilder builder = builderObj.GetComponent<NewRoadBuilder>();
            if (builder == null) builder = builderObj.AddComponent<NewRoadBuilder>();
            builder.BuildRoad();
        }

        /// <summary>
        /// Finds and deactivates any legacy road.obj instances in the scene.
        /// </summary>
        [ContextMenu("Deactivate Old Road")]
        public static void DeactivateOldRoad()
        {
            GameObject[] allObjs = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int count = 0;
            foreach (var go in allObjs)
            {
                if (go == null) continue;

                // Target old road meshes, but NEVER touch our new road or its children
                if (go.name.StartsWith("NewRoad") || go.transform.root.name.StartsWith("NewRoad")) continue;

                string lowerName = go.name.ToLower().Trim();

                // Match any legacy road object (road, road (1), road (2), road_..., road.obj, etc.)
                bool isLegacyRoad = lowerName == "road" ||
                                    lowerName.StartsWith("road ") ||
                                    lowerName.StartsWith("road(") ||
                                    lowerName.StartsWith("road_") ||
                                    lowerName.Contains("road.obj") ||
                                    lowerName.Contains("polygon_1") ||
                                    (lowerName.StartsWith("road") && !lowerName.StartsWith("roadgenerator"));

                if (isLegacyRoad)
                {
                    go.SetActive(false);

                    var renderers = go.GetComponentsInChildren<Renderer>(true);
                    for (int r = 0; r < renderers.Length; r++)
                    {
                        if (renderers[r] != null) renderers[r].enabled = false;
                    }

                    var colliders = go.GetComponentsInChildren<Collider>(true);
                    for (int c = 0; c < colliders.Length; c++)
                    {
                        if (colliders[c] != null) colliders[c].enabled = false;
                    }

                    count++;
                }
            }
            if (count > 0)
            {
                Debug.Log($"[NewRoadBuilder] Deactivated {count} legacy road object(s).");
            }
        }

        public static void SafeDestroy(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }

        [ContextMenu("Build New Road In Scene")]
        public void BuildRoad()
        {
            DeactivateOldRoad();

            // Destroy existing new road root if regenerating
            GameObject oldRoot = GameObject.Find(NEW_ROAD_ROOT_NAME);
            SafeDestroy(oldRoot);

            LoadMaterials();

            GameObject root = new GameObject(NEW_ROAD_ROOT_NAME);
            root.transform.position = Vector3.zero;

            float length = endZ - startZ;
            float centerZ = (startZ + endZ) * 0.5f;

            // 1. Main Asphalt Road Surface
            CreateRoadSurface(root.transform, length, centerZ);

            // 2. Concrete Curbs (Beveled 3D steps along road edges)
            CreateRoadCurbs(root.transform, length, centerZ);

            // 3. 3D Highway Steel Guardrails (Left & Right)
            if (buildGuardrails)
            {
                CreateGuardrails(root.transform, length);
            }

            // 4. Vast Seamless Ground Landscape & Horizon Backdrop Mountains (Eliminates all edge clipping)
            CreateVastGroundLandscape(root.transform, startZ, endZ);

            // 5. Level 1 Start Line & Overhead Banner
            CreateStartLine(root.transform, level1StartZ);

            // 6. Level 1 Red Finish Line & 3D Finish Arch Banner
            CreateFinishBanner(root.transform, level1FinishZ);

            // 7. Stylized Pine Trees & Rocks (Lush road corridor)
            if (buildTreesAndRocks)
            {
                CreateSidePineTreesAndRocks(root.transform);
            }

            Debug.Log($"[NewRoadBuilder] Fresh 3D Road successfully generated! Length={length}m (Z: {startZ} -> {endZ}), Trees & Banners created!");
        }

        private void LoadMaterials()
        {
            if (roadMaterial == null) roadMaterial = Resources.Load<Material>("Materials/M_StylizedRoad");
            if (steelMaterial == null) steelMaterial = Resources.Load<Material>("Materials/M_GuardrailSteel");
            if (grassMaterial == null) grassMaterial = Resources.Load<Material>("Materials/M_GrassVerge");
            if (pineFoliageMaterial == null) pineFoliageMaterial = Resources.Load<Material>("Materials/M_PineFoliage");
            if (pineFoliageLightMaterial == null) pineFoliageLightMaterial = Resources.Load<Material>("Materials/M_PineFoliageLight");
            if (pineBarkMaterial == null) pineBarkMaterial = Resources.Load<Material>("Materials/M_PineBark");
            if (rockMaterial == null) rockMaterial = Resources.Load<Material>("Materials/M_StylizedRock");
            if (startLineMaterial == null) startLineMaterial = Resources.Load<Material>("Materials/M_StartLine");
            if (finishBannerMaterial == null) finishBannerMaterial = Resources.Load<Material>("Materials/M_FinishBanner");
            if (finishTrussMaterial == null) finishTrussMaterial = Resources.Load<Material>("Materials/M_FinishTruss");

#if UNITY_EDITOR
            if (roadMaterial == null) roadMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Road/M_StylizedRoad.mat");
            if (steelMaterial == null) steelMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Road/M_GuardrailSteel.mat");
            if (grassMaterial == null) grassMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Road/M_GrassVerge.mat");
            if (pineFoliageMaterial == null) pineFoliageMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Road/M_PineFoliage.mat");
            if (pineFoliageLightMaterial == null) pineFoliageLightMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Road/M_PineFoliageLight.mat");
            if (pineBarkMaterial == null) pineBarkMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Road/M_PineBark.mat");
            if (rockMaterial == null) rockMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Road/M_StylizedRock.mat");
            if (startLineMaterial == null) startLineMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Road/M_StartLine.mat");
            if (finishBannerMaterial == null) finishBannerMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Road/M_FinishBanner.mat");
            if (finishTrussMaterial == null) finishTrussMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Road/M_FinishTruss.mat");
#endif
        }

        private void CreateRoadSurface(Transform parent, float length, float centerZ)
        {
            GameObject roadObj = new GameObject("NewRoad_AsphaltSurface");
            roadObj.transform.SetParent(parent, false);
            roadObj.transform.position = new Vector3(0, roadY, centerZ);

            MeshFilter mf = roadObj.AddComponent<MeshFilter>();
            MeshRenderer mr = roadObj.AddComponent<MeshRenderer>();
            mr.sharedMaterial = roadMaterial;

            Mesh mesh = new Mesh { name = "Road_Mesh" };

            float halfW = roadWidth * 0.5f;
            float halfL = length * 0.5f;

            Vector3[] vertices = new Vector3[]
            {
                new Vector3(-halfW, 0, -halfL),
                new Vector3( halfW, 0, -halfL),
                new Vector3(-halfW, 0,  halfL),
                new Vector3( halfW, 0,  halfL)
            };

            float vTile = length / roadWidth;
            Vector2[] uvs = new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, vTile),
                new Vector2(1, vTile)
            };

            int[] triangles = new int[] { 0, 2, 1, 2, 3, 1 };
            Vector3[] normals = new Vector3[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.normals = normals;
            mesh.RecalculateBounds();
            mf.sharedMesh = mesh;

            BoxCollider col = roadObj.AddComponent<BoxCollider>();
            col.center = new Vector3(0, -0.1f, 0);
            col.size = new Vector3(roadWidth, 0.2f, length);
        }

        private void CreateRoadCurbs(Transform parent, float length, float centerZ)
        {
            float halfW = roadWidth * 0.5f;
            float curbWidth = 0.25f;
            float curbHeight = 0.12f;

            CreateBoxCurb(parent, "Curb_Left", -halfW - (curbWidth * 0.5f), centerZ, curbWidth, curbHeight, length);
            CreateBoxCurb(parent, "Curb_Right", halfW + (curbWidth * 0.5f), centerZ, curbWidth, curbHeight, length);
        }

        private void CreateBoxCurb(Transform parent, string name, float posX, float centerZ, float width, float height, float length)
        {
            GameObject curbObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            curbObj.name = name;
            curbObj.transform.SetParent(parent, false);
            curbObj.transform.position = new Vector3(posX, roadY + (height * 0.5f), centerZ);
            curbObj.transform.localScale = new Vector3(width, height, length);

            MeshRenderer mr = curbObj.GetComponent<MeshRenderer>();
            if (mr != null && steelMaterial != null) mr.sharedMaterial = steelMaterial;
        }

        private void CreateGuardrails(Transform parent, float length)
        {
            GameObject guardrailsRoot = new GameObject("Guardrails_Container");
            guardrailsRoot.transform.SetParent(parent, false);

            float halfW = roadWidth * 0.5f + 0.35f;

            CreateHorizontalRail(guardrailsRoot.transform, "Rail_Left_Upper",  -halfW, roadY + guardrailHeight, length);
            CreateHorizontalRail(guardrailsRoot.transform, "Rail_Left_Lower",  -halfW, roadY + (guardrailHeight * 0.55f), length);
            CreateHorizontalRail(guardrailsRoot.transform, "Rail_Right_Upper",  halfW, roadY + guardrailHeight, length);
            CreateHorizontalRail(guardrailsRoot.transform, "Rail_Right_Lower",  halfW, roadY + (guardrailHeight * 0.55f), length);

            GameObject postsRoot = new GameObject("Guardrail_Posts");
            postsRoot.transform.SetParent(guardrailsRoot.transform, false);

            float curZ = startZ;
            while (curZ <= endZ)
            {
                CreateGuardrailPost(postsRoot.transform, "Post_L", -halfW, curZ);
                CreateGuardrailPost(postsRoot.transform, "Post_R",  halfW, curZ);
                curZ += postSpacing;
            }
        }

        private void CreateHorizontalRail(Transform parent, string name, float posX, float posY, float length)
        {
            GameObject rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rail.name = name;
            rail.transform.SetParent(parent, false);
            rail.transform.position = new Vector3(posX, posY, (startZ + endZ) * 0.5f);
            rail.transform.localScale = new Vector3(0.08f, 0.18f, length);

            MeshRenderer mr = rail.GetComponent<MeshRenderer>();
            if (mr != null && steelMaterial != null) mr.sharedMaterial = steelMaterial;

            Collider c = rail.GetComponent<Collider>();
            SafeDestroy(c);
        }

        private void CreateGuardrailPost(Transform parent, string name, float posX, float posZ)
        {
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.name = name;
            post.transform.SetParent(parent, false);
            post.transform.position = new Vector3(posX, roadY + (guardrailHeight * 0.5f), posZ);
            post.transform.localScale = new Vector3(0.12f, guardrailHeight, 0.12f);

            MeshRenderer mr = post.GetComponent<MeshRenderer>();
            if (mr != null && steelMaterial != null) mr.sharedMaterial = steelMaterial;

            Collider c = post.GetComponent<Collider>();
            SafeDestroy(c);
        }

        private void CreateVastGroundLandscape(Transform parent, float startZ, float endZ)
        {
            GameObject groundRoot = new GameObject("NewRoad_GroundLandscape");
            groundRoot.transform.SetParent(parent, false);

            float totalLength = (endZ - startZ) + 300f; // ~830m length
            float centerZ = (startZ + endZ) * 0.5f + 50f;
            float totalWidth = 500f; // 250m left, 250m right

            // Single massive seamless landscape plane covering the entire visible horizon
            GameObject groundPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            groundPlane.name = "Ground_Landscape_Plane";
            groundPlane.transform.SetParent(groundRoot.transform, false);
            groundPlane.transform.position = new Vector3(0, roadY - 0.025f, centerZ);
            groundPlane.transform.localScale = new Vector3(totalWidth * 0.1f, 1f, totalLength * 0.1f);

            MeshRenderer mr = groundPlane.GetComponent<MeshRenderer>();
            if (mr != null && grassMaterial != null) mr.sharedMaterial = grassMaterial;

            Collider c = groundPlane.GetComponent<Collider>();
            SafeDestroy(c);

            // Horizon mountain backdrop ridge at far distance (Z = endZ + 40f)
            CreateHorizonBackdrop(groundRoot.transform, endZ + 40f);
        }

        private void CreateHorizonBackdrop(Transform parent, float zPos)
        {
            GameObject hillsRoot = new GameObject("Horizon_DistantHills");
            hillsRoot.transform.SetParent(parent, false);

            for (float x = -240f; x <= 240f; x += 45f)
            {
                GameObject hill = GameObject.CreatePrimitive(PrimitiveType.Cube);
                hill.name = "DistantHill";
                hill.transform.SetParent(hillsRoot.transform, false);
                float h = Random.Range(35f, 65f);
                float w = Random.Range(55f, 85f);
                hill.transform.position = new Vector3(x + Random.Range(-10f, 10f), h * 0.5f - 4f, zPos + Random.Range(-20f, 20f));
                hill.transform.localScale = new Vector3(w, h, 40f);
                hill.transform.localRotation = Quaternion.Euler(0, Random.Range(-25f, 25f), Random.Range(-5f, 5f));

                MeshRenderer mr = hill.GetComponent<MeshRenderer>();
                if (mr != null && pineFoliageMaterial != null) mr.sharedMaterial = pineFoliageMaterial;

                Collider c = hill.GetComponent<Collider>();
                SafeDestroy(c);
            }
        }

        // ==========================================
        // LEVEL 1 START LINE & OVERHEAD ARCH
        // ==========================================
        private void CreateStartLine(Transform parent, float zPos)
        {
            GameObject startRoot = new GameObject("Level1_StartLine_Container");
            startRoot.transform.SetParent(parent, false);
            startRoot.transform.position = new Vector3(0, roadY, zPos);

            // 1. Road Surface Start Stripe (Checkered / White-Green)
            GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.name = "Start_RoadStripe";
            stripe.transform.SetParent(startRoot.transform, false);
            stripe.transform.localPosition = new Vector3(0, 0.02f, 0);
            stripe.transform.localScale = new Vector3(roadWidth - 0.2f, 0.02f, 1.4f);

            MeshRenderer mrStripe = stripe.GetComponent<MeshRenderer>();
            if (mrStripe != null && startLineMaterial != null) mrStripe.sharedMaterial = startLineMaterial;
            Collider sc = stripe.GetComponent<Collider>();
            SafeDestroy(sc);

            // 2. Left & Right Arch Pillars
            float pillarX = roadWidth * 0.5f + 0.4f;
            float archHeight = 3.6f;

            CreateArchPillar(startRoot.transform, "StartPillar_L", -pillarX, archHeight, finishTrussMaterial);
            CreateArchPillar(startRoot.transform, "StartPillar_R",  pillarX, archHeight, finishTrussMaterial);

            // 3. Overhead Horizontal Crossbar
            GameObject crossbar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crossbar.name = "Start_Crossbar";
            crossbar.transform.SetParent(startRoot.transform, false);
            crossbar.transform.localPosition = new Vector3(0, archHeight, 0);
            crossbar.transform.localScale = new Vector3(pillarX * 2.0f + 0.4f, 0.2f, 0.2f);
            MeshRenderer mrCb = crossbar.GetComponent<MeshRenderer>();
            if (mrCb != null && finishTrussMaterial != null) mrCb.sharedMaterial = finishTrussMaterial;
            Collider ccc = crossbar.GetComponent<Collider>();
            SafeDestroy(ccc);

            // 4. Overhead "LEVEL 1 START" Banner Signboard
            GameObject bannerObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bannerObj.name = "Start_Signboard";
            bannerObj.transform.SetParent(startRoot.transform, false);
            bannerObj.transform.localPosition = new Vector3(0, archHeight - 0.45f, 0);
            bannerObj.transform.localScale = new Vector3(4.8f, 0.75f, 0.12f);
            MeshRenderer mrB = bannerObj.GetComponent<MeshRenderer>();
            if (mrB != null && startLineMaterial != null) mrB.sharedMaterial = startLineMaterial;
            Collider bCol = bannerObj.GetComponent<Collider>();
            SafeDestroy(bCol);

            // Text on Banner
            Create3DWorldText(bannerObj.transform, "START_TEXT", "LEVEL 1 - START", new Vector3(0, 0, -0.08f), Color.white, 5.0f);
        }

        // ==========================================
        // LEVEL 1 RED FINISH LINE & END ARCH BANNER
        // ==========================================
        private void CreateFinishBanner(Transform parent, float zPos)
        {
            GameObject finishRoot = new GameObject("Level1_FinishArch_Container");
            finishRoot.transform.SetParent(parent, false);
            finishRoot.transform.position = new Vector3(0, roadY, zPos);

            // 1. Red Finish Road Stripe
            GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.name = "Finish_RoadStripe_Red";
            stripe.transform.SetParent(finishRoot.transform, false);
            stripe.transform.localPosition = new Vector3(0, 0.02f, 0);
            stripe.transform.localScale = new Vector3(roadWidth - 0.1f, 0.02f, 2.0f);

            MeshRenderer mrStripe = stripe.GetComponent<MeshRenderer>();
            if (mrStripe != null && finishBannerMaterial != null) mrStripe.sharedMaterial = finishBannerMaterial;
            Collider sc = stripe.GetComponent<Collider>();
            SafeDestroy(sc);

            // 2. Heavy Arch Truss Pillars
            float pillarX = roadWidth * 0.5f + 0.45f;
            float archHeight = 4.4f;

            CreateArchPillar(finishRoot.transform, "FinishPillar_L", -pillarX, archHeight, finishTrussMaterial);
            CreateArchPillar(finishRoot.transform, "FinishPillar_R",  pillarX, archHeight, finishTrussMaterial);

            // 3. Top Truss Beam
            GameObject crossbar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crossbar.name = "Finish_TrussBeam";
            crossbar.transform.SetParent(finishRoot.transform, false);
            crossbar.transform.localPosition = new Vector3(0, archHeight + 0.1f, 0);
            crossbar.transform.localScale = new Vector3(pillarX * 2.0f + 0.6f, 0.35f, 0.35f);
            MeshRenderer mrCb = crossbar.GetComponent<MeshRenderer>();
            if (mrCb != null && finishTrussMaterial != null) mrCb.sharedMaterial = finishTrussMaterial;
            Collider ccc = crossbar.GetComponent<Collider>();
            SafeDestroy(ccc);

            // 4. Large Red Banner ("LEVEL 1 FINISH")
            GameObject bannerObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bannerObj.name = "Finish_Banner_Red";
            bannerObj.transform.SetParent(finishRoot.transform, false);
            bannerObj.transform.localPosition = new Vector3(0, archHeight - 0.65f, 0);
            bannerObj.transform.localScale = new Vector3(5.2f, 1.15f, 0.16f);

            MeshRenderer mrB = bannerObj.GetComponent<MeshRenderer>();
            if (mrB != null && finishBannerMaterial != null) mrB.sharedMaterial = finishBannerMaterial;
            Collider bCol = bannerObj.GetComponent<Collider>();
            SafeDestroy(bCol);

            // Golden Border Frame on Banner
            CreateBannerFrame(bannerObj.transform, 5.2f, 1.15f);

            // Text on Red Banner
            Create3DWorldText(bannerObj.transform, "FINISH_TEXT", "FINISH", new Vector3(0, 0.12f, -0.10f), new Color(1f, 0.88f, 0.15f, 1f), 7.2f);
            Create3DWorldText(bannerObj.transform, "SUB_TEXT", "LEVEL 1 COMPLETE", new Vector3(0, -0.28f, -0.10f), Color.white, 3.4f);

            // Checkered Flags on Pillars
            CreateCheckeredFlag(finishRoot.transform, "Flag_L", -pillarX, archHeight + 0.7f);
            CreateCheckeredFlag(finishRoot.transform, "Flag_R",  pillarX, archHeight + 0.7f);
        }

        private void CreateArchPillar(Transform parent, string name, float posX, float height, Material mat)
        {
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillar.name = name;
            pillar.transform.SetParent(parent, false);
            pillar.transform.localPosition = new Vector3(posX, height * 0.5f, 0);
            pillar.transform.localScale = new Vector3(0.35f, height, 0.35f);

            MeshRenderer mr = pillar.GetComponent<MeshRenderer>();
            if (mr != null && mat != null) mr.sharedMaterial = mat;

            Collider c = pillar.GetComponent<Collider>();
            SafeDestroy(c);
        }

        private void CreateBannerFrame(Transform bannerTransform, float w, float h)
        {
            GameObject frameObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frameObj.name = "GoldFrame";
            frameObj.transform.SetParent(bannerTransform, false);
            frameObj.transform.localPosition = new Vector3(0, 0, -0.02f);
            frameObj.transform.localScale = new Vector3(1.02f, 1.08f, 0.95f);
            MeshRenderer mr = frameObj.GetComponent<MeshRenderer>();
            if (mr != null && steelMaterial != null) mr.sharedMaterial = steelMaterial;
            Collider c = frameObj.GetComponent<Collider>();
            SafeDestroy(c);
        }

        private void CreateCheckeredFlag(Transform parent, string name, float posX, float posY)
        {
            GameObject flagPole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            flagPole.name = name;
            flagPole.transform.SetParent(parent, false);
            flagPole.transform.localPosition = new Vector3(posX, posY, 0);
            flagPole.transform.localScale = new Vector3(0.08f, 0.5f, 0.08f);
            MeshRenderer mrPole = flagPole.GetComponent<MeshRenderer>();
            if (mrPole != null && steelMaterial != null) mrPole.sharedMaterial = steelMaterial;
            Collider cp = flagPole.GetComponent<Collider>();
            SafeDestroy(cp);

            GameObject flagCloth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flagCloth.name = "Cloth";
            flagCloth.transform.SetParent(flagPole.transform, false);
            flagCloth.transform.localPosition = new Vector3(0.4f, 0.2f, 0);
            flagCloth.transform.localScale = new Vector3(0.7f, 0.4f, 0.05f);
            MeshRenderer mrCloth = flagCloth.GetComponent<MeshRenderer>();
            if (mrCloth != null && finishBannerMaterial != null) mrCloth.sharedMaterial = finishBannerMaterial;
            Collider cc = flagCloth.GetComponent<Collider>();
            SafeDestroy(cc);
        }

        private void Create3DWorldText(Transform parent, string name, string text, Vector3 localPos, Color color, float fontSize)
        {
            GameObject txtObj = new GameObject(name);
            txtObj.transform.SetParent(parent, false);
            txtObj.transform.localPosition = localPos;
            txtObj.transform.localRotation = Quaternion.identity;

            TextMeshPro tmp = txtObj.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;

            RectTransform rt = txtObj.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(10f, 2f);
            }
        }

        // ==========================================
        // STYLIZED PINE TREES & ROCKS GENERATOR
        // ==========================================
        private void CreateSidePineTreesAndRocks(Transform parent)
        {
            GameObject natureRoot = new GameObject("Nature_Trees_And_Rocks");
            natureRoot.transform.SetParent(parent, false);

            EnsureSharedConeMeshes();

            Random.InitState(1337); // Deterministic seed for beautiful organic placement

            float roadHalf = roadWidth * 0.5f + 0.45f;
            float zStep = treeSpacingZ;

            // Generate trees in 2 rows on left and right for depth
            for (float z = startZ; z <= endZ + 10f; z += zStep)
            {
                // Left Side - Row 1 (Near guardrail)
                float xL1 = -(roadHalf + Random.Range(1.8f, 3.8f));
                float zL1 = z + Random.Range(-1.2f, 1.2f);
                SpawnPineTree(natureRoot.transform, new Vector3(xL1, roadY, zL1), Random.Range(0.85f, 1.25f));

                // Left Side - Row 2 (Further on hillside)
                if (Random.value > 0.25f)
                {
                    float xL2 = -(roadHalf + Random.Range(5.0f, 9.5f));
                    float zL2 = z + Random.Range(-2.0f, 2.0f);
                    SpawnPineTree(natureRoot.transform, new Vector3(xL2, roadY, zL2), Random.Range(1.1f, 1.55f));
                }

                // Right Side - Row 1 (Near guardrail)
                float xR1 = roadHalf + Random.Range(1.8f, 3.8f);
                float zR1 = z + Random.Range(-1.2f, 1.2f);
                SpawnPineTree(natureRoot.transform, new Vector3(xR1, roadY, zR1), Random.Range(0.85f, 1.25f));

                // Right Side - Row 2 (Further on hillside)
                if (Random.value > 0.25f)
                {
                    float xR2 = roadHalf + Random.Range(5.0f, 9.5f);
                    float zR2 = z + Random.Range(-2.0f, 2.0f);
                    SpawnPineTree(natureRoot.transform, new Vector3(xR2, roadY, zR2), Random.Range(1.1f, 1.55f));
                }

                // Scatter stylized rocks every ~12 meters
                if (Random.value > 0.45f)
                {
                    float rockSide = (Random.value > 0.5f) ? -1f : 1f;
                    float xRock = rockSide * (roadHalf + Random.Range(1.2f, 4.5f));
                    float zRock = z + Random.Range(-2.5f, 2.5f);
                    SpawnRock(natureRoot.transform, new Vector3(xRock, roadY, zRock));
                }
            }
        }

        private void SpawnPineTree(Transform parent, Vector3 position, float scale)
        {
            GameObject treeObj = new GameObject("PineTree");
            treeObj.transform.SetParent(parent, false);
            treeObj.transform.position = position;
            treeObj.transform.localScale = Vector3.one * scale;
            treeObj.transform.localRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

            // 1. Trunk
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(treeObj.transform, false);
            trunk.transform.localPosition = new Vector3(0, 0.6f, 0);
            trunk.transform.localScale = new Vector3(0.32f, 0.6f, 0.32f);
            MeshRenderer mrTrunk = trunk.GetComponent<MeshRenderer>();
            if (mrTrunk != null && pineBarkMaterial != null) mrTrunk.sharedMaterial = pineBarkMaterial;
            Collider tc = trunk.GetComponent<Collider>();
            SafeDestroy(tc);

            // 2. Foliage Tier 1 (Bottom - Darker Green)
            CreateFoliageTier(treeObj.transform, "Tier1", sharedConeTier1, pineFoliageMaterial, new Vector3(0, 0.9f, 0));

            // 3. Foliage Tier 2 (Middle - Lighter Green)
            CreateFoliageTier(treeObj.transform, "Tier2", sharedConeTier2, pineFoliageLightMaterial, new Vector3(0, 1.8f, 0));

            // 4. Foliage Tier 3 (Top - Darker Green)
            CreateFoliageTier(treeObj.transform, "Tier3", sharedConeTier3, pineFoliageMaterial, new Vector3(0, 2.6f, 0));
        }

        private void CreateFoliageTier(Transform parent, string name, Mesh coneMesh, Material mat, Vector3 localPos)
        {
            GameObject tierObj = new GameObject(name);
            tierObj.transform.SetParent(parent, false);
            tierObj.transform.localPosition = localPos;

            MeshFilter mf = tierObj.AddComponent<MeshFilter>();
            mf.sharedMesh = coneMesh;

            MeshRenderer mr = tierObj.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
        }

        private void SpawnRock(Transform parent, Vector3 position)
        {
            GameObject rockObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rockObj.name = "Rock_Boulder";
            rockObj.transform.SetParent(parent, false);
            rockObj.transform.position = position + new Vector3(0, 0.25f, 0);

            float sx = Random.Range(0.6f, 1.4f);
            float sy = Random.Range(0.4f, 0.9f);
            float sz = Random.Range(0.6f, 1.3f);
            rockObj.transform.localScale = new Vector3(sx, sy, sz);
            rockObj.transform.localRotation = Quaternion.Euler(Random.Range(-15f, 15f), Random.Range(0f, 360f), Random.Range(-15f, 15f));

            MeshRenderer mr = rockObj.GetComponent<MeshRenderer>();
            if (mr != null && rockMaterial != null) mr.sharedMaterial = rockMaterial;

            Collider c = rockObj.GetComponent<Collider>();
            SafeDestroy(c);
        }

        private void EnsureSharedConeMeshes()
        {
            if (sharedConeTier1 == null) sharedConeTier1 = BuildLowPolyConeMesh(1.6f, 1.4f, 8);
            if (sharedConeTier2 == null) sharedConeTier2 = BuildLowPolyConeMesh(1.2f, 1.3f, 8);
            if (sharedConeTier3 == null) sharedConeTier3 = BuildLowPolyConeMesh(0.85f, 1.2f, 8);
        }

        /// <summary>
        /// Generates a fast, low-poly 8-sided stylized cone mesh for pine trees.
        /// </summary>
        private Mesh BuildLowPolyConeMesh(float radius, float height, int segments)
        {
            Mesh mesh = new Mesh { name = $"Cone_R{radius}_H{height}" };

            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();
            List<Vector3> norms = new List<Vector3>();

            Vector3 tip = new Vector3(0, height, 0);

            // Generate side facets
            for (int i = 0; i < segments; i++)
            {
                float a1 = (float)i / segments * Mathf.PI * 2f;
                float a2 = (float)(i + 1) / segments * Mathf.PI * 2f;

                Vector3 p1 = new Vector3(Mathf.Cos(a1) * radius, 0, Mathf.Sin(a1) * radius);
                Vector3 p2 = new Vector3(Mathf.Cos(a2) * radius, 0, Mathf.Sin(a2) * radius);

                Vector3 normal = Vector3.Cross(p2 - p1, tip - p1).normalized;

                int idx = verts.Count;
                verts.Add(p1);
                verts.Add(p2);
                verts.Add(tip);

                norms.Add(normal);
                norms.Add(normal);
                norms.Add(normal);

                tris.Add(idx);
                tris.Add(idx + 1);
                tris.Add(idx + 2);
            }

            // Bottom cap
            Vector3 center = Vector3.zero;
            for (int i = 0; i < segments; i++)
            {
                float a1 = (float)i / segments * Mathf.PI * 2f;
                float a2 = (float)(i + 1) / segments * Mathf.PI * 2f;

                Vector3 p1 = new Vector3(Mathf.Cos(a1) * radius, 0, Mathf.Sin(a1) * radius);
                Vector3 p2 = new Vector3(Mathf.Cos(a2) * radius, 0, Mathf.Sin(a2) * radius);

                int idx = verts.Count;
                verts.Add(center);
                verts.Add(p2);
                verts.Add(p1);

                norms.Add(Vector3.down);
                norms.Add(Vector3.down);
                norms.Add(Vector3.down);

                tris.Add(idx);
                tris.Add(idx + 1);
                tris.Add(idx + 2);
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        [ContextMenu("Clear New Road")]
        public void ClearNewRoad()
        {
            GameObject root = GameObject.Find(NEW_ROAD_ROOT_NAME);
            SafeDestroy(root);
        }
    }
}
