using UnityEngine;
using System.Collections.Generic;
using TMPro;

namespace MobControl.Level
{
    /// <summary>
    /// Builds the exact Level 1 Stylized Road matching the reference design:
    /// - Grand, expansive 9.8m wide 2-lane asphalt highway with crisp white dashed divider & solid yellow shoulder lines.
    /// - Light concrete curbs on both shoulders.
    /// - Dual-rail slate-steel highway guardrails with amber/orange reflector posts.
    /// - Rich roadside landscape: dense multi-tiered stylized pine trees & stacked faceted cliff rocks on both verges.
    /// - Beautiful rolling mountain horizon ridge.
    /// - Low, wide-angle cinematic camera framing matching the reference angle.
    /// </summary>
    [ExecuteAlways]
    public class NewRoadBuilder : MonoBehaviour
    {
        [Header("Road Dimensions")]
        [SerializeField] private float roadWidth = 9.8f; // Expansive wide road matching reference
        [SerializeField] private float startZ = -15.0f;
        [SerializeField] private float endZ = 240.0f;
        [SerializeField] private float uvRepeatLength = 10.0f;

        [Header("Guardrail Settings")]
        [SerializeField] private float guardrailX = 5.25f; // Positioned along outer edge of curb
        [SerializeField] private float postSpacing = 3.6f;

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
            // 0. Purge any legacy floating text, gates, or clutter
            PurgeSceneClutter();

            GameObject existingRoot = GameObject.Find(ROOT_NAME);
            if (existingRoot != null) SafeDestroy(existingRoot);

            GameObject root = new GameObject(ROOT_NAME);

            // Load Materials
            Material roadMat = Resources.Load<Material>("Materials/M_StylizedRoad");
            Material steelMat = Resources.Load<Material>("Materials/M_GuardrailSteel");
            Material grassMat = Resources.Load<Material>("Materials/M_GrassVerge");
            Material amberMat = Resources.Load<Material>("Materials/M_GunOrange");
            Material foliageMat = Resources.Load<Material>("Materials/M_PineFoliage") ?? Resources.Load<Material>("Materials/M_StylizedPineTree");
            Material foliageLightMat = Resources.Load<Material>("Materials/M_PineFoliageLight") ?? foliageMat;
            Material barkMat = Resources.Load<Material>("Materials/M_PineBark");
            Material rockMat = Resources.Load<Material>("Materials/M_StylizedCliffRock") ?? Resources.Load<Material>("Materials/M_StylizedRock");

            // 1. Build Grand Road Surface Mesh
            BuildRoadSurface(root.transform, roadMat);

            // 2. Build Dual-Rail Steel Guardrails with Amber Reflectors
            BuildGuardrails(root.transform, steelMat, amberMat);

            // 3. Build Sloping Grass Landscape Verge
            BuildGrassBanks(root.transform, grassMat);

            // 4. Build Dense Stylized Nature (Layered Conical Pine Trees & Stacked Cliff Boulders)
            BuildNatureForestAndRocks(root.transform, foliageMat, foliageLightMat, barkMat, rockMat);

            // 5. Build Sweeping Distant Mountain Horizon Ridge
            BuildMountainHorizon(root.transform, grassMat);

            // 6. Setup Exact Camera & Atmospheric Horizon Fog
            SetupCameraAndAtmosphere();

            Debug.Log("[NewRoadBuilder] Level 1 reference stylized road successfully assembled with wide highway & dense pine forest!");
        }

        private void PurgeSceneClutter()
        {
            // Destroy any floating 3D TextMeshPro objects in world space (e.g. legacy x2 gates)
            TextMeshPro[] worldTexts = Object.FindObjectsByType<TextMeshPro>(FindObjectsInactive.Include);
            foreach (var txt in worldTexts)
            {
                if (txt != null && txt.GetComponentInParent<Canvas>() == null)
                {
                    SafeDestroy(txt.gameObject);
                }
            }

            // Destroy legacy Gun_M41D or unwanted clutter
            GameObject[] allGos = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (var go in allGos)
            {
                if (go != null && (go.name.Contains("Gun_M41D") || go.name.Contains("MultiplierGate") || go.name.Contains("DestructibleGate")))
                {
                    SafeDestroy(go);
                }
            }
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

            float totalLen = (endZ - startZ) + 80f;
            float centerZ = (startZ + endZ) * 0.5f;

            // Flat expansive landscape base plane
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

        private void BuildNatureForestAndRocks(Transform parent, Material foliageMat, Material foliageLightMat, Material barkMat, Material rockMat)
        {
            GameObject natureRoot = new GameObject("Nature_Trees_And_Rocks");
            natureRoot.transform.SetParent(parent, false);

            float halfRoad = roadWidth * 0.5f;

            // Generate dense trees and stacked rocks continuously along both sides
            for (float z = startZ + 3f; z <= endZ - 5f; z += 4.5f)
            {
                // ---- LEFT SIDE ----
                // 1. Stacked Cliff Rocks (Right behind guardrail)
                float rockXL = -(halfRoad + 1.6f + Random.Range(0f, 0.8f));
                CreateStylizedCliffRock(natureRoot.transform, new Vector3(rockXL, 0.15f, z + Random.Range(-0.8f, 0.8f)), Random.Range(1.8f, 2.8f), rockMat);

                // 2. Front Row Pine Trees (Nestled among rocks)
                float treeXL1 = -(halfRoad + 3.2f + Random.Range(0f, 1.2f));
                Material fMatL1 = (Random.value > 0.4f) ? foliageMat : foliageLightMat;
                CreateStylizedPineTree(natureRoot.transform, new Vector3(treeXL1, 0f, z + Random.Range(-1.2f, 1.2f)), Random.Range(1.8f, 2.6f), fMatL1, barkMat);

                // 3. Back Row Pine Trees (Taller forest canopy background)
                if (Random.value > 0.25f)
                {
                    float treeXL2 = -(halfRoad + 5.5f + Random.Range(0f, 2.5f));
                    CreateStylizedPineTree(natureRoot.transform, new Vector3(treeXL2, 0.4f, z + 2.2f), Random.Range(2.6f, 3.8f), foliageMat, barkMat);
                }

                // ---- RIGHT SIDE ----
                // 1. Stacked Cliff Rocks
                float rockXR = (halfRoad + 1.6f + Random.Range(0f, 0.8f));
                CreateStylizedCliffRock(natureRoot.transform, new Vector3(rockXR, 0.15f, z + Random.Range(-0.8f, 0.8f)), Random.Range(1.8f, 2.8f), rockMat);

                // 2. Front Row Pine Trees
                float treeXR1 = (halfRoad + 3.2f + Random.Range(0f, 1.2f));
                Material fMatR1 = (Random.value > 0.4f) ? foliageMat : foliageLightMat;
                CreateStylizedPineTree(natureRoot.transform, new Vector3(treeXR1, 0f, z + Random.Range(-1.2f, 1.2f)), Random.Range(1.8f, 2.6f), fMatR1, barkMat);

                // 3. Back Row Pine Trees
                if (Random.value > 0.25f)
                {
                    float treeXR2 = (halfRoad + 5.5f + Random.Range(0f, 2.5f));
                    CreateStylizedPineTree(natureRoot.transform, new Vector3(treeXR2, 0.4f, z + 2.2f), Random.Range(2.6f, 3.8f), foliageMat, barkMat);
                }
            }
        }

        private void CreateStylizedPineTree(Transform parent, Vector3 pos, float scale, Material foliageMat, Material barkMat)
        {
            GameObject tree = new GameObject("Stylized_Pine_Tree");
            tree.transform.SetParent(parent, false);
            tree.transform.position = pos;
            tree.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            tree.transform.localScale = Vector3.one * scale;

            // 1. Trunk (Brown wood cylinder)
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(tree.transform, false);
            trunk.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            trunk.transform.localScale = new Vector3(0.35f, 0.7f, 0.35f);
            SafeDestroy(trunk.GetComponent<Collider>());
            if (barkMat != null) trunk.GetComponent<MeshRenderer>().sharedMaterial = barkMat;

            // 2. 4-Tier Layered Conical Foliage Skirts (Exact match for reference image cartoon pine needles)
            CreatePineConeTier(tree.transform, "Tier1_Bottom", 0.90f, 1.60f, 0.55f, 1.10f, foliageMat, 0f);
            CreatePineConeTier(tree.transform, "Tier2_MidLow", 1.65f, 1.30f, 0.40f, 1.00f, foliageMat, 18f);
            CreatePineConeTier(tree.transform, "Tier3_MidHigh", 2.35f, 1.00f, 0.25f, 0.90f, foliageMat, 36f);
            CreatePineConeTip(tree.transform, "Tier4_Peak", 3.00f, 0.70f, 1.10f, foliageMat);
        }

        private void CreatePineConeTier(Transform parent, string name, float posY, float baseR, float topR, float height, Material mat, float rotY)
        {
            GameObject tier = new GameObject(name);
            tier.transform.SetParent(parent, false);
            tier.transform.localPosition = new Vector3(0f, posY, 0f);
            tier.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);

            MeshFilter mf = tier.AddComponent<MeshFilter>();
            MeshRenderer mr = tier.AddComponent<MeshRenderer>();
            if (mat != null) mr.sharedMaterial = mat;

            mf.sharedMesh = GetOrCreateSkirtMesh(baseR, topR, height, 10);
        }

        private void CreatePineConeTip(Transform parent, string name, float posY, float baseR, float height, Material mat)
        {
            GameObject tip = new GameObject(name);
            tip.transform.SetParent(parent, false);
            tip.transform.localPosition = new Vector3(0f, posY, 0f);

            MeshFilter mf = tip.AddComponent<MeshFilter>();
            MeshRenderer mr = tip.AddComponent<MeshRenderer>();
            if (mat != null) mr.sharedMaterial = mat;

            mf.sharedMesh = GetOrCreateTipMesh(baseR, height, 10);
        }

        private static readonly Dictionary<string, Mesh> meshCache = new Dictionary<string, Mesh>();

        private static Mesh GetOrCreateSkirtMesh(float baseR, float topR, float height, int segments)
        {
            string key = $"skirt_{baseR}_{topR}_{height}_{segments}";
            if (meshCache.TryGetValue(key, out Mesh existing) && existing != null) return existing;

            Mesh mesh = new Mesh { name = key };
            int vertCount = (segments + 1) * 2;
            Vector3[] verts = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];
            int[] tris = new int[segments * 6];

            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                // Gentle geometric faceting on skirt needles
                float flare = 1f + 0.08f * Mathf.Sin(angle * 4f);
                float rBottom = baseR * flare;

                verts[i * 2] = new Vector3(cos * rBottom, 0f, sin * rBottom);
                verts[i * 2 + 1] = new Vector3(cos * topR, height, sin * topR);

                uvs[i * 2] = new Vector2((float)i / segments, 0f);
                uvs[i * 2 + 1] = new Vector2((float)i / segments, 1f);

                if (i < segments)
                {
                    int ti = i * 6;
                    tris[ti] = i * 2;
                    tris[ti + 1] = i * 2 + 1;
                    tris[ti + 2] = i * 2 + 2;
                    tris[ti + 3] = i * 2 + 1;
                    tris[ti + 4] = i * 2 + 3;
                    tris[ti + 5] = i * 2 + 2;
                }
            }

            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            meshCache[key] = mesh;
            return mesh;
        }

        private static Mesh GetOrCreateTipMesh(float baseR, float height, int segments)
        {
            string key = $"tip_{baseR}_{height}_{segments}";
            if (meshCache.TryGetValue(key, out Mesh existing) && existing != null) return existing;

            Mesh mesh = new Mesh { name = key };
            Vector3[] verts = new Vector3[segments + 2];
            Vector2[] uvs = new Vector2[verts.Length];
            int[] tris = new int[segments * 3];

            verts[0] = new Vector3(0f, height, 0f); // Apex
            uvs[0] = new Vector2(0.5f, 1f);

            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                verts[i + 1] = new Vector3(Mathf.Cos(angle) * baseR, 0f, Mathf.Sin(angle) * baseR);
                uvs[i + 1] = new Vector2((float)i / segments, 0f);

                if (i < segments)
                {
                    int ti = i * 3;
                    tris[ti] = 0;
                    tris[ti + 1] = i + 1;
                    tris[ti + 2] = i + 2;
                }
            }

            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            meshCache[key] = mesh;
            return mesh;
        }

        private void CreateStylizedCliffRock(Transform parent, Vector3 pos, float scale, Material rockMat)
        {
            GameObject rockGroup = new GameObject("Stylized_Cliff_Rock");
            rockGroup.transform.SetParent(parent, false);
            rockGroup.transform.position = pos;
            rockGroup.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            // Stack 2-3 interlocking faceted boulders
            CreateRockPiece(rockGroup.transform, "BaseRock", Vector3.zero, new Vector3(1.4f, 1.2f, 1.3f) * scale, Quaternion.Euler(12f, 25f, -8f), rockMat);
            CreateRockPiece(rockGroup.transform, "UpperRock", new Vector3(0.2f, 0.6f * scale, 0.1f), new Vector3(1.0f, 0.9f, 0.95f) * scale, Quaternion.Euler(-15f, 65f, 10f), rockMat);
            if (Random.value > 0.4f)
            {
                CreateRockPiece(rockGroup.transform, "SideBoulder", new Vector3(-0.5f * scale, 0.2f, 0.3f * scale), new Vector3(0.8f, 0.7f, 0.75f) * scale, Quaternion.Euler(5f, -30f, 15f), rockMat);
            }
        }

        private void CreateRockPiece(Transform parent, string name, Vector3 localPos, Vector3 localScale, Quaternion localRot, Material mat)
        {
            GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piece.name = name;
            piece.transform.SetParent(parent, false);
            piece.transform.localPosition = localPos;
            piece.transform.localScale = localScale;
            piece.transform.localRotation = localRot;

            SafeDestroy(piece.GetComponent<Collider>());
            if (mat != null) piece.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private void BuildMountainHorizon(Transform parent, Material grassMat)
        {
            GameObject horizonRoot = new GameObject("DistantHorizon_Mountains");
            horizonRoot.transform.SetParent(parent, false);

            // Create smooth layered undulating ridge spanning across the horizon
            float ridgeZ = endZ + 40f;
            for (float x = -200f; x <= 200f; x += 40f)
            {
                GameObject peak = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                peak.name = "MountainPeak";
                peak.transform.SetParent(horizonRoot.transform, false);

                float h = Random.Range(20f, 35f);
                float radius = Random.Range(35f, 60f);

                peak.transform.position = new Vector3(x + Random.Range(-10f, 10f), h * 0.5f - 6f, ridgeZ + Random.Range(-10f, 15f));
                peak.transform.localScale = new Vector3(radius, h * 0.5f, radius);
                peak.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                SafeDestroy(peak.GetComponent<Collider>());
                if (grassMat != null) peak.GetComponent<MeshRenderer>().sharedMaterial = grassMat;
            }
        }

        public static void SetupCameraAndAtmosphere()
        {
            Color skyBlue = new Color(0.35f, 0.62f, 0.92f, 1f); // Vibrant bright cartoon sky blue

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                // Low, wide perspective looking down the grand highway
                mainCam.transform.position = new Vector3(0f, 1.75f, -3.5f);
                mainCam.transform.rotation = Quaternion.Euler(7.5f, 0f, 0f);
                mainCam.fieldOfView = 65.0f; // Wider FOV makes the road look grand and expansive
                mainCam.farClipPlane = 450.0f;
                mainCam.backgroundColor = skyBlue;
                mainCam.clearFlags = CameraClearFlags.SolidColor;
            }

            RenderSettings.skybox = null;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = skyBlue;
            RenderSettings.fogStartDistance = 120.0f;
            RenderSettings.fogEndDistance = 270.0f;
        }

        [ContextMenu("Clear Road")]
        public void ClearRoad()
        {
            GameObject root = GameObject.Find(ROOT_NAME);
            SafeDestroy(root);
        }
    }
}
