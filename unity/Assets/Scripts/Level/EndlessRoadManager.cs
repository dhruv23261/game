using System.Collections.Generic;
using UnityEngine;
using MobControl.Combat;
using MobControl.Core;
using MobControl.UI;

namespace MobControl.Level
{
    /// <summary>
    /// Manages the endless stylized highway road that continuously rushes towards the player.
    /// Modular road segments are smoothly spawned ahead and recycled behind.
    /// Obstacles consist ONLY of hazard barricades and shipping containers placed with varied gaps
    /// requiring the player to dodge left, right, or center with the joystick.
    /// </summary>
    public class EndlessRoadManager : MonoBehaviour
    {
        public static EndlessRoadManager Instance { get; private set; }

        [Header("Road Speed")]
        [Tooltip("Forward rushing speed of the road towards the player (m/s)")]
        [SerializeField] private float roadSpeed = 4.2f;
        [SerializeField] private bool isRunning = true;

        [Header("Level Configuration")]
        [SerializeField] private int currentLevel = 1;
        public int CurrentLevel
        {
            get => currentLevel;
            set
            {
                if (currentLevel == value) return;
                currentLevel = value;
                // Track may already be built with the previous level's layout (Start() runs before StartMission sets the level)
                if (segments.Count > 0) BuildInitialEndlessTrack();
            }
        }

        public bool IsMoving => isRunning;
        public float RoadSpeed => roadSpeed;

        [Header("Segment Dimensions")]
        public const float SEGMENT_LENGTH = 30.0f;
        public const int NUM_SEGMENTS = 6;
        public const float ROAD_WIDTH = 9.8f;
        public const float GUARDRAIL_X = 5.25f;
        public const float DESPAWN_Z = -35.0f;

        private readonly List<RoadSegment> segments = new List<RoadSegment>();
        private int patternIndex = 0;
        private int totalSegmentsPopulated = 0;

        // Cached Materials
        private Material roadMat;
        private Material steelMat;
        private Material grassMat;
        private Material amberMat;
        private Material hazardMat;
        private Material containerRedMat;
        private Material containerBlueMat;
        private Material containerOrangeMat;
        private Material containerGreenMat;
        private Material containerPurpleMat;
        private Material burnedCarMat;
        private Material rustCarMat;
        private Material tireMat;
        private Material lavaMat;
        private Material lavaCrustMat;

        // Cached Nature Meshes
        private Mesh treeMesh;
        private Material treeMat;
        private Mesh rockMesh;
        private Material rockMat;

        public float CurrentRoadSpeed => isRunning ? roadSpeed : 0.0f;
        public bool IsRunning => isRunning;

        public void SetRunning(bool running)
        {
            isRunning = running;
        }

        public void SetSpeed(float newSpeed)
        {
            roadSpeed = newSpeed;
        }

        private void Awake()
        {
            Instance = this;
            LoadMaterialsAndAssets();
        }

        private void Start()
        {
            BuildInitialEndlessTrack();
        }

        private void LoadMaterialsAndAssets()
        {
            if (roadMat == null) roadMat = Resources.Load<Material>("Materials/M_StylizedRoad");
#if UNITY_EDITOR
            if (roadMat == null) roadMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/M_StylizedRoad.mat");
#endif
            if (roadMat == null)
            {
                roadMat = NewRoadBuilder.LoadTextureMaterialPublic("Textures/Road/Road_Stylized_Reference.png", "M_StylizedRoad_Endless");
            }

            if (steelMat == null) steelMat = Resources.Load<Material>("Materials/M_GuardrailSteel");
            if (grassMat == null) grassMat = Resources.Load<Material>("Materials/M_GrassVerge");
            if (amberMat == null) amberMat = Resources.Load<Material>("Materials/M_GunOrange");

#if UNITY_EDITOR
            if (steelMat == null) steelMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/M_GuardrailSteel.mat");
            if (grassMat == null) grassMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/M_GrassVerge.mat");
            if (amberMat == null) amberMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/M_GunOrange.mat");
#endif

            if (hazardMat == null) hazardMat = NewRoadBuilder.LoadTextureMaterialPublic("Textures/tex_hazard_stripes.png", "M_HazardBarricade_Endless");

            if (containerRedMat == null)
            {
                containerRedMat = NewRoadBuilder.LoadTextureMaterialPublic("Textures/tex_shipping_container_red.png", "M_Container_Red_Endless");
                if (containerRedMat != null) containerRedMat.color = new Color(0.85f, 0.16f, 0.14f);
            }

            if (containerBlueMat == null)
            {
                containerBlueMat = NewRoadBuilder.LoadTextureMaterialPublic("Textures/tex_shipping_container_red.png", "M_Container_Blue_Endless");
                if (containerBlueMat != null) containerBlueMat.color = new Color(0.12f, 0.45f, 0.90f);
            }

            if (containerOrangeMat == null)
            {
                containerOrangeMat = NewRoadBuilder.LoadTextureMaterialPublic("Textures/tex_shipping_container_red.png", "M_Container_Orange_Endless");
                if (containerOrangeMat != null) containerOrangeMat.color = new Color(0.92f, 0.50f, 0.12f);
            }

            if (containerGreenMat == null)
            {
                containerGreenMat = NewRoadBuilder.LoadTextureMaterialPublic("Textures/tex_shipping_container_red.png", "M_Container_Green_Endless");
                if (containerGreenMat != null) containerGreenMat.color = new Color(0.24f, 0.42f, 0.20f);
            }

            if (containerPurpleMat == null)
            {
                containerPurpleMat = NewRoadBuilder.LoadTextureMaterialPublic("Textures/tex_shipping_container_red.png", "M_Container_Purple_Endless");
                if (containerPurpleMat != null) containerPurpleMat.color = new Color(0.72f, 0.16f, 0.95f);
            }

            if (burnedCarMat == null)
            {
                burnedCarMat = NewRoadBuilder.LoadTextureMaterialPublic("Textures/tex_burned_metal.png", "M_BurnedCar_Endless");
                if (burnedCarMat != null) burnedCarMat.color = new Color(0.85f, 0.85f, 0.85f);
            }

            if (rustCarMat == null)
            {
                rustCarMat = new Material(Shader.Find("Standard"));
                rustCarMat.color = new Color(0.42f, 0.20f, 0.12f);
            }

            if (tireMat == null)
            {
                tireMat = new Material(Shader.Find("Standard"));
                tireMat.color = new Color(0.08f, 0.08f, 0.09f);
            }

            if (lavaMat == null)
            {
                Shader unlit = Shader.Find("Unlit/Texture") ?? Shader.Find("Mobile/Diffuse");
                lavaMat = new Material(unlit) { name = "M_MoltenLava_Endless" };
                Texture2D lavaTex = Resources.Load<Texture2D>("Textures/tex_molten_lava");
                if (lavaTex != null)
                {
                    lavaTex.wrapMode = TextureWrapMode.Repeat;
                    lavaMat.mainTexture = lavaTex;
                }
                else
                {
                    lavaMat = NewRoadBuilder.LoadTextureMaterialPublic("Textures/tex_molten_lava.png", "M_MoltenLava_Endless");
                }
            }

            if (lavaCrustMat == null)
            {
                lavaCrustMat = new Material(Shader.Find("Standard"));
                lavaCrustMat.color = new Color(0.12f, 0.09f, 0.08f);
            }

            // Nature Assets
            if (treeMesh == null)
            {
                GameObject treePrefab = Resources.Load<GameObject>("Nature/PineTree_V2");
                if (treePrefab != null) treeMesh = treePrefab.GetComponentInChildren<MeshFilter>()?.sharedMesh;
#if UNITY_EDITOR && !UNITY_ANDROID
                if (treeMesh == null)
                {
                    string treeObjPath = System.IO.Path.Combine(Application.dataPath, "Models/Nature/PineTree_V2.obj");
                    treeMesh = NewRoadBuilder.LoadObjMeshDirectPublic(treeObjPath);
                }
#endif
            }
            if (treeMat == null) treeMat = Resources.Load<Material>("Materials/M_MeshyPineTree_V2") ?? NewRoadBuilder.LoadTextureMaterialPublic("Nature/PineTree_V2_textures/base_color.png", "M_MeshyPineTree_Endless");

            if (rockMesh == null)
            {
                GameObject rockPrefab = Resources.Load<GameObject>("Nature/CliffRock_V2");
                if (rockPrefab != null) rockMesh = rockPrefab.GetComponentInChildren<MeshFilter>()?.sharedMesh;
#if UNITY_EDITOR && !UNITY_ANDROID
                if (rockMesh == null)
                {
                    string rockObjPath = System.IO.Path.Combine(Application.dataPath, "Models/Nature/CliffRock_V2.obj");
                    rockMesh = NewRoadBuilder.LoadObjMeshDirectPublic(rockObjPath);
                }
#endif
            }
            if (rockMat == null) rockMat = Resources.Load<Material>("Materials/M_MeshyCliffRock_V2") ?? NewRoadBuilder.LoadTextureMaterialPublic("Nature/CliffRock_V2_textures/base_color.png", "M_MeshyCliffRock_Endless");
        }

        public static void SafeDestroy(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }

        public void BuildInitialEndlessTrack()
        {
            if (roadMat == null)
            {
                LoadMaterialsAndAssets();
            }

            // Clean up any existing segment children
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                SafeDestroy(transform.GetChild(i).gameObject);
            }
            segments.Clear();
            totalSegmentsPopulated = 0;
            l2EnvironmentCounter = 0;

            // Spawn 6 sequential segments from Z = -30m to Z = +120m
            for (int i = 0; i < NUM_SEGMENTS; i++)
            {
                float zPos = -30.0f + (i * SEGMENT_LENGTH);
                // First 2 segments (under and right in front of player) start empty so player can breathe
                bool spawnObstacles = (i >= 2);
                RoadSegment seg = CreateSegment(i, zPos, spawnObstacles);
                segments.Add(seg);
            }
            isRunning = true;
        }

        private void Update()
        {
            // Only scroll when gameplay is active
            if (ZombieRunnerUIManager.Instance != null &&
                ZombieRunnerUIManager.Instance.CurrentState != ZombieRunnerUIManager.ScreenState.Gameplay)
            {
                return;
            }

            // Stop road immediately if player is dead or blocked
            if (PlayerRunnerController.Instance != null && PlayerRunnerController.Instance.IsDead)
            {
                isRunning = false;
                return;
            }

            if (!isRunning) return;

            float moveDelta = roadSpeed * Time.deltaTime;

            // 1. Move all segments backward towards and past player
            for (int i = 0; i < segments.Count; i++)
            {
                RoadSegment seg = segments[i];
                if (seg != null && seg.rootObj != null)
                {
                    Vector3 pos = seg.rootObj.transform.position;
                    pos.z -= moveDelta;
                    seg.rootObj.transform.position = pos;
                }
            }

            // 2. Check for segments that have completely passed behind player
            for (int i = 0; i < segments.Count; i++)
            {
                RoadSegment seg = segments[i];
                if (seg != null && seg.rootObj != null)
                {
                    if (seg.rootObj.transform.position.z <= DESPAWN_Z)
                    {
                        RecycleSegmentToFront(seg);
                    }
                }
            }
        }

        private void RecycleSegmentToFront(RoadSegment seg)
        {
            // Find current highest Z among all other segments
            float highestZ = float.MinValue;
            for (int i = 0; i < segments.Count; i++)
            {
                if (segments[i] != seg && segments[i].rootObj != null)
                {
                    float z = segments[i].rootObj.transform.position.z;
                    if (z > highestZ) highestZ = z;
                }
            }

            float newZ = highestZ + SEGMENT_LENGTH;
            Vector3 pos = seg.rootObj.transform.position;
            pos.z = newZ;
            seg.rootObj.transform.position = pos;

            // Clear old obstacles and populate with fresh varied pattern
            seg.ClearObstacles();
            List<Rect> pitRects = GetPitRectsForStep(totalSegmentsPopulated);
            if (seg.roadMeshFilter != null)
            {
                seg.roadMeshFilter.sharedMesh = CreateSegmentRoadMesh(pitRects);
            }
            PopulateSegmentObstacles(seg);
        }

        private RoadSegment CreateSegment(int index, float zPos, bool withObstacles)
        {
            GameObject segObj = new GameObject($"RoadSegment_{index}");
            segObj.transform.SetParent(transform, false);
            segObj.transform.position = new Vector3(0f, 0f, zPos);

            RoadSegment seg = new RoadSegment
            {
                index = index,
                rootObj = segObj
            };

            List<Rect> pitRects = null;
            if (withObstacles)
            {
                pitRects = GetPitRectsForStep(totalSegmentsPopulated);
            }

            // 1. Asphalt Road Surface (Length 30m, exact UV repeat of 10m -> 3 seamless repeats!)
            seg.roadMeshFilter = CreateSegmentRoadSurface(segObj.transform, pitRects);

            // 2. Dual Steel Guardrails with Amber Reflectors
            CreateSegmentGuardrails(segObj.transform);

            // 3. Grass Banks & Nature Monoliths (Pine Trees & Cliff Rocks)
            CreateSegmentNatureEnvironment(segObj.transform);

            // 4. Obstacle Holder Root
            GameObject obsRoot = new GameObject("ObstaclesRoot");
            obsRoot.transform.SetParent(segObj.transform, false);
            seg.obstaclesRoot = obsRoot;

            if (withObstacles)
            {
                PopulateSegmentObstacles(seg);
            }

            return seg;
        }

        /// <summary>Road-mesh holes for the lava pits the next populated segment will contain (must match the Populate* layouts).</summary>
        private List<Rect> GetPitRectsForStep(int step)
        {
            if (ArmyRunLevel.HandlesLevel(currentLevel)) return new List<Rect>(); // Army Run: clean road, no lava pits
            if (currentLevel == 2) return GetLevel2PitRectsForPattern(GetLevel2PatternId(step));
            // Matches the pattern formula used by PopulateSegmentObstacles for level 1
            int patternId = (step < 10) ? step : (3 + (step % 7));
            return GetPitRectsForPattern(patternId);
        }

        private static int GetLevel2PatternId(int step) => (step < 10) ? step : (2 + (step % 8));

        private static List<Rect> GetLevel2PitRectsForPattern(int patternId)
        {
            // Level 2 is Overgrown Road — NO lava pits! Clean continuous asphalt for plant monsters & roots.
            return new List<Rect>();
        }

        private List<Rect> GetPitRectsForPattern(int patternId)
        {
            List<Rect> list = new List<Rect>();
            switch (patternId)
            {
                case 2: // Zone 3: Molten Lava Fissure (Left+Center) — right lane safe
                    list.Add(new Rect(-1.5f - 1.9f, 15.0f - 3.25f, 3.8f, 6.5f));
                    break;
                case 5: // Zone 6: Twin Lava Canyon — center bridge
                    list.Add(new Rect(-2.8f - 1.2f, 15.0f - 3.75f, 2.4f, 7.5f));
                    list.Add(new Rect( 2.8f - 1.2f, 15.0f - 3.75f, 2.4f, 7.5f));
                    break;
                case 7: // Zone 8: Burned Car + Lava Trap (right lava pit)
                    list.Add(new Rect(2.0f - 1.3f, 21.0f - 3.0f, 2.6f, 6.0f));
                    break;
            }
            return list;
        }

        private MeshFilter CreateSegmentRoadSurface(Transform parent, List<Rect> pitRects)
        {
            if (roadMat == null) LoadMaterialsAndAssets();

            GameObject roadObj = new GameObject("RoadSurface");
            roadObj.transform.SetParent(parent, false);
            roadObj.transform.localPosition = Vector3.zero;

            MeshFilter mf = roadObj.AddComponent<MeshFilter>();
            MeshRenderer mr = roadObj.AddComponent<MeshRenderer>();
            if (roadMat != null)
            {
                mr.sharedMaterial = roadMat;
            }
            else
            {
                roadMat = NewRoadBuilder.LoadTextureMaterialPublic("Textures/Road/Road_Stylized_Reference.png", "M_StylizedRoad_Endless");
                mr.sharedMaterial = roadMat;
            }

            mf.sharedMesh = CreateSegmentRoadMesh(pitRects);

            // BoxCollider for road bed
            BoxCollider col = roadObj.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, -0.1f, SEGMENT_LENGTH * 0.5f);
            col.size = new Vector3(ROAD_WIDTH, 0.2f, SEGMENT_LENGTH);

            return mf;
        }

        private Mesh CreateSegmentRoadMesh(List<Rect> pitRects)
        {
            Mesh mesh = new Mesh { name = "RoadSegmentMesh" };
            int cols = 12; // 0.5m resolution across 6m road
            int rows = 30; // 1.0m resolution along 30m length
            float stepX = ROAD_WIDTH / cols;
            float stepZ = SEGMENT_LENGTH / rows;
            float halfW = ROAD_WIDTH * 0.5f;

            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            for (int r = 0; r < rows; r++)
            {
                float z0 = r * stepZ;
                float z1 = (r + 1) * stepZ;
                float zMid = (z0 + z1) * 0.5f;

                for (int c = 0; c < cols; c++)
                {
                    float x0 = -halfW + c * stepX;
                    float x1 = -halfW + (c + 1) * stepX;
                    float xMid = (x0 + x1) * 0.5f;

                    // Check if cell is inside any lava pit cutout
                    bool inPit = false;
                    if (pitRects != null && pitRects.Count > 0)
                    {
                        for (int p = 0; p < pitRects.Count; p++)
                        {
                            if (pitRects[p].Contains(new Vector2(xMid, zMid)))
                            {
                                inPit = true;
                                break;
                            }
                        }
                    }

                    if (inPit) continue; // Skip quad, leaves authentic physical hole!

                    int vBase = verts.Count;
                    verts.Add(new Vector3(x0, 0.002f, z0));
                    verts.Add(new Vector3(x1, 0.002f, z0));
                    verts.Add(new Vector3(x0, 0.002f, z1));
                    verts.Add(new Vector3(x1, 0.002f, z1));

                    uvs.Add(new Vector2((x0 + halfW) / ROAD_WIDTH, z0 / 10.0f));
                    uvs.Add(new Vector2((x1 + halfW) / ROAD_WIDTH, z0 / 10.0f));
                    uvs.Add(new Vector2((x0 + halfW) / ROAD_WIDTH, z1 / 10.0f));
                    uvs.Add(new Vector2((x1 + halfW) / ROAD_WIDTH, z1 / 10.0f));

                    tris.Add(vBase);
                    tris.Add(vBase + 2);
                    tris.Add(vBase + 1);

                    tris.Add(vBase + 1);
                    tris.Add(vBase + 2);
                    tris.Add(vBase + 3);
                }
            }

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void CreateSegmentGuardrails(Transform parent)
        {
            GameObject railRoot = new GameObject("Guardrails");
            railRoot.transform.SetParent(parent, false);

            float centerZ = SEGMENT_LENGTH * 0.5f;

            // Left Horizontal Rails
            CreateRailBeam(railRoot.transform, -GUARDRAIL_X, 0.72f, centerZ, SEGMENT_LENGTH);
            CreateRailBeam(railRoot.transform, -GUARDRAIL_X, 0.40f, centerZ, SEGMENT_LENGTH);

            // Right Horizontal Rails
            CreateRailBeam(railRoot.transform, GUARDRAIL_X, 0.72f, centerZ, SEGMENT_LENGTH);
            CreateRailBeam(railRoot.transform, GUARDRAIL_X, 0.40f, centerZ, SEGMENT_LENGTH);

            // Posts spaced every 3.75m (8 posts per 30m segment)
            for (float z = 0f; z <= SEGMENT_LENGTH; z += 3.75f)
            {
                CreatePostWithReflector(railRoot.transform, -GUARDRAIL_X, z, true);
                CreatePostWithReflector(railRoot.transform, GUARDRAIL_X, z, false);
            }
        }

        private void CreateRailBeam(Transform parent, float posX, float posY, float centerZ, float length)
        {
            GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beam.name = "RailBeam";
            beam.transform.SetParent(parent, false);
            beam.transform.localPosition = new Vector3(posX, posY, centerZ);
            beam.transform.localScale = new Vector3(0.10f, 0.22f, length);
            SafeDestroy(beam.GetComponent<Collider>());
            if (steelMat != null) beam.GetComponent<MeshRenderer>().sharedMaterial = steelMat;
        }

        private void CreatePostWithReflector(Transform parent, float posX, float posZ, bool isLeft)
        {
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.name = isLeft ? "Post_L" : "Post_R";
            post.transform.SetParent(parent, false);
            post.transform.localPosition = new Vector3(posX, 0.42f, posZ);
            post.transform.localScale = new Vector3(0.14f, 0.86f, 0.14f);
            SafeDestroy(post.GetComponent<Collider>());
            if (steelMat != null) post.GetComponent<MeshRenderer>().sharedMaterial = steelMat;

            // Amber Reflector plate
            GameObject reflector = GameObject.CreatePrimitive(PrimitiveType.Cube);
            reflector.name = "Reflector";
            reflector.transform.SetParent(post.transform, false);
            reflector.transform.localPosition = new Vector3(0f, 0.18f, -0.58f);
            reflector.transform.localScale = new Vector3(0.65f, 0.22f, 0.18f);
            SafeDestroy(reflector.GetComponent<Collider>());
            if (amberMat != null) reflector.GetComponent<MeshRenderer>().sharedMaterial = amberMat;
        }

        private void CreateSegmentNatureEnvironment(Transform parent)
        {
            GameObject natureRoot = new GameObject("NatureEnvironment");
            natureRoot.transform.SetParent(parent, false);

            if (currentLevel == 2)
            {
                CreateLevel2Environment(natureRoot.transform);
                return;
            }

            // Left & Right sloping green grass verge
            CreateGrassVerge(natureRoot.transform, true);
            CreateGrassVerge(natureRoot.transform, false);

            // High-Definition Meshy Pine Trees & Cliff Rocks alongside guardrails
            for (float z = 3.0f; z < SEGMENT_LENGTH; z += 9.5f)
            {
                // Left Bank Nature
                if (rockMesh != null)
                {
                    float rockXL = -(GUARDRAIL_X + 2.0f + Random.Range(0f, 0.5f));
                    float rScale = Random.Range(6.0f, 8.2f);
                    SpawnMeshProp(natureRoot.transform, "CliffRock_L", rockMesh, rockMat,
                        new Vector3(rockXL, 0f, z + Random.Range(-0.5f, 0.5f)),
                        new Vector3(rScale * 0.7f, rScale, rScale * 0.7f),
                        Quaternion.Euler(0f, Random.Range(-25f, 25f), 0f));
                }

                if (treeMesh != null)
                {
                    float treeXL = -(GUARDRAIL_X + 3.8f + Random.Range(0f, 1.2f));
                    float tScale = Random.Range(6.5f, 9.2f);
                    SpawnMeshProp(natureRoot.transform, "PineTree_L", treeMesh, treeMat,
                        new Vector3(treeXL, Random.Range(1.2f, 2.5f), z + Random.Range(-0.8f, 0.8f)),
                        Vector3.one * tScale,
                        Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                }

                // Right Bank Nature
                if (rockMesh != null)
                {
                    float rockXR = (GUARDRAIL_X + 2.0f + Random.Range(0f, 0.5f));
                    float rScale = Random.Range(6.0f, 8.2f);
                    SpawnMeshProp(natureRoot.transform, "CliffRock_R", rockMesh, rockMat,
                        new Vector3(rockXR, 0f, z + Random.Range(-0.5f, 0.5f)),
                        new Vector3(rScale * 0.7f, rScale, rScale * 0.7f),
                        Quaternion.Euler(0f, Random.Range(-25f, 25f), 0f));
                }

                if (treeMesh != null)
                {
                    float treeXR = (GUARDRAIL_X + 3.8f + Random.Range(0f, 1.2f));
                    float tScale = Random.Range(6.5f, 9.2f);
                    SpawnMeshProp(natureRoot.transform, "PineTree_R", treeMesh, treeMat,
                        new Vector3(treeXR, Random.Range(1.2f, 2.5f), z + Random.Range(-0.8f, 0.8f)),
                        Vector3.one * tScale,
                        Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                }
            }
        }

        private void CreateGrassVerge(Transform parent, bool isLeft)
        {
            GameObject verge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            verge.name = isLeft ? "GrassBank_L" : "GrassBank_R";
            verge.transform.SetParent(parent, false);
            float xCenter = isLeft ? -(GUARDRAIL_X + 18.0f) : (GUARDRAIL_X + 18.0f);
            verge.transform.localPosition = new Vector3(xCenter, -0.4f, SEGMENT_LENGTH * 0.5f);
            verge.transform.localScale = new Vector3(36.0f, 0.8f, SEGMENT_LENGTH);
            SafeDestroy(verge.GetComponent<Collider>());
            if (grassMat != null) verge.GetComponent<MeshRenderer>().sharedMaterial = grassMat;
        }

        private GameObject SpawnMeshProp(Transform parent, string name, Mesh mesh, Material mat, Vector3 pos, Vector3 scale, Quaternion rot)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = pos;
            obj.transform.localScale = scale;
            obj.transform.localRotation = rot;

            MeshFilter mf = obj.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            MeshRenderer mr = obj.AddComponent<MeshRenderer>();
            if (mat != null) mr.sharedMaterial = mat;

            return obj;
        }

        /// <summary>
        /// Structured Level Layout — 3-Phase Road Map:
        ///
        ///  PHASE 1 — Scout Rush (Zones 0-3, ~Easy):
        ///    Zone 0: Empty warm-up road
        ///    Zone 1: Single burned car — first obstacle, open dodge lanes
        ///    Zone 2: Gun Upgrade Container at center — reward early kill
        ///    Zone 3: Molten Lava fissure (left+center) — first hazard, hug right
        ///
        ///  PHASE 2 — Mixed Assault (Zones 4-6, ~Medium):
        ///    Zone 4: Squad Multiplier Gate (orange container, destructible)
        ///    Zone 5: Double slalom burned cars + center Gun Upgrade
        ///    Zone 6: Twin Lava Canyon — left and right pits, precision center bridge
        ///
        ///  PHASE 3 — Heavy Push (Zones 7-9, ~Hard):
        ///    Zone 7: Stacked Industrial Containers on left + Mega Multiplier on right
        ///    Zone 8: Burned car + lava trap — dual hazard gauntlet
        ///    Zone 9: Angled Container Gateway — wide gap, high-drama funnel
        ///
        ///  BOSS THRESHOLD (Zone 10):
        ///    Zone 10: Side containers framing the road, open center for Boss stomp arrival
        ///
        ///  ENDLESS LOOP: Cycles zones 3-9 (progressively harder) after initial 10 zones
        /// </summary>
        // ════════════════════════════════════════════════════════════════
        //  LEVEL 2 — "Overgrown Highway"
        //  Sunset sky, abandoned wrecks (Meshy heli / car / pickup / bus) and
        //  vines creeping over the asphalt. Lane blockers are real wrecks
        //  (solid, indestructible — no bonus rewards) and plant pods that erupt into flytraps.
        //  Every zone keeps at least one lane open.
        // ════════════════════════════════════════════════════════════════

        private GameObject l2HeliPrefab, l2CarPrefab, l2PickupPrefab, l2BusPrefab;
        private Material l2GrassMat, l2VineMat, l2VineDarkMat;
        private bool l2AssetsLoaded;
        private int l2EnvironmentCounter;

        private void LoadLevel2Assets()
        {
            if (l2AssetsLoaded) return;
            l2AssetsLoaded = true;
            l2HeliPrefab   = Resources.Load<GameObject>("Prefabs/Level2/L2_HeliWreck");
            l2CarPrefab    = Resources.Load<GameObject>("Prefabs/Level2/L2_CarWreck");
            l2PickupPrefab = Resources.Load<GameObject>("Prefabs/Level2/L2_PickupWreck");
            l2BusPrefab    = Resources.Load<GameObject>("Prefabs/Level2/L2_BusWreck");
            l2VineMat      = Resources.Load<Material>("Materials/Level2/M_L2_PodLeaf");
            l2VineDarkMat  = Resources.Load<Material>("Materials/Level2/M_L2_PodLeafDark");

            if (grassMat != null)
            {
                // Deeper, lusher overgrowth green (reads warm under the level 2 sunset light)
                l2GrassMat = new Material(grassMat) { name = "M_GrassVerge_L2" };
                l2GrassMat.color = grassMat.color * new Color(0.78f, 0.92f, 0.62f, 1f);
            }
        }

        private void CreateLevel2Environment(Transform natureRoot)
        {
            LoadLevel2Assets();

            Material savedGrass = grassMat;
            if (l2GrassMat != null) grassMat = l2GrassMat;
            CreateGrassVerge(natureRoot, true);
            CreateGrassVerge(natureRoot, false);
            grassMat = savedGrass;

            int envIndex = l2EnvironmentCounter++;
            bool heliHere = l2HeliPrefab != null && (envIndex % 3 == 1);
            int heliSide = ((envIndex / 3) % 2 == 0) ? 1 : -1;

            int slot = 0;
            for (float z = 3.0f; z < SEGMENT_LENGTH; z += 9.5f, slot++)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    float zJit = z + Random.Range(-0.6f, 0.6f);

                    if (heliHere && side == heliSide && slot == 1)
                    {
                        SpawnHeliCrashSetPiece(natureRoot, new Vector3(side * (GUARDRAIL_X + 3.4f), 0f, zJit), side);
                        continue;
                    }

                    float roll = Random.value;
                    if (roll < 0.42f)
                    {
                        // Abandoned vehicle on the verge (no collider — scenery only)
                        GameObject prefab = PickRoadsideWreck();
                        float yaw = Random.Range(-28f, 28f) + (Random.value < 0.5f ? 180f : 0f);
                        float x = side * (GUARDRAIL_X + (prefab == l2BusPrefab ? 2.6f : 2.0f) + Random.Range(0f, 0.4f));
                        SpawnDecorWreck(natureRoot, prefab, new Vector3(x, 0f, zJit), yaw);
                    }
                    else if (rockMesh != null)
                    {
                        float rScale = Random.Range(5.2f, 7.4f);
                        SpawnMeshProp(natureRoot, side < 0 ? "CliffRock_L" : "CliffRock_R", rockMesh, rockMat,
                            new Vector3(side * (GUARDRAIL_X + 2.0f + Random.Range(0f, 0.5f)), 0f, zJit),
                            new Vector3(rScale * 0.7f, rScale, rScale * 0.7f),
                            Quaternion.Euler(0f, Random.Range(-25f, 25f), 0f));
                    }

                    if (treeMesh != null && Random.value < 0.8f)
                    {
                        float tScale = Random.Range(6.5f, 9.2f);
                        SpawnMeshProp(natureRoot, side < 0 ? "PineTree_L" : "PineTree_R", treeMesh, treeMat,
                            new Vector3(side * (GUARDRAIL_X + 4.6f + Random.Range(0f, 1.4f)), Random.Range(1.2f, 2.5f), z + Random.Range(-0.8f, 0.8f)),
                            Vector3.one * tScale,
                            Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                    }
                }
            }

            CreateRoadVines(natureRoot);
        }

        private GameObject PickRoadsideWreck()
        {
            float r = Random.value;
            GameObject p = r < 0.40f ? l2CarPrefab : (r < 0.75f ? l2PickupPrefab : l2BusPrefab);
            return p != null ? p : (l2CarPrefab != null ? l2CarPrefab : l2PickupPrefab);
        }

        private GameObject SpawnDecorWreck(Transform parent, GameObject prefab, Vector3 localPos, float yaw)
        {
            if (prefab == null) return null;
            GameObject go = Instantiate(prefab, parent);
            go.name = "Decor_" + prefab.name;
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) SafeDestroy(c);
            return go;
        }

        /// <summary>Crashed military helicopter just past the guardrail, still burning.</summary>
        private void SpawnHeliCrashSetPiece(Transform parent, Vector3 localPos, int side)
        {
            float yaw = (side > 0 ? -38f : 38f) + Random.Range(-8f, 8f) + (Random.value < 0.5f ? 180f : 0f);
            GameObject heli = SpawnDecorWreck(parent, l2HeliPrefab, localPos, yaw);
            if (heli == null) return;
            heli.name = "Decor_HeliCrash";
            BurnedCarBuilder.AttachWreckFire(heli.transform, new Vector3(0f, 1.6f, -1.2f), 1.25f);
        }

        /// <summary>Vines and moss creeping in from both road edges.</summary>
        private void CreateRoadVines(Transform parent)
        {
            if (l2VineMat == null) return;
            const float edgeX = ROAD_WIDTH * 0.5f;
            for (int i = 0; i < 4; i++)
            {
                int side = (i % 2 == 0) ? -1 : 1;
                float z = Random.Range(2f, SEGMENT_LENGTH - 2f);
                // A creeping runner: a few slim overlapping leaves along the shoulder, angled slightly toward the lane
                float baseYaw = Random.Range(-18f, 18f) - side * 8f;
                int leaves = Random.Range(3, 5);
                for (int k = 0; k < leaves; k++)
                {
                    GameObject vine = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    vine.name = "RoadVine";
                    vine.transform.SetParent(parent, false);
                    vine.transform.localPosition = new Vector3(side * (edgeX - 0.25f - k * 0.12f) , 0.012f, z + k * 0.55f);
                    vine.transform.localRotation = Quaternion.Euler(0f, baseYaw + Random.Range(-25f, 25f), 0f);
                    vine.transform.localScale = new Vector3(Random.Range(0.18f, 0.28f), 0.03f, Random.Range(0.55f, 0.85f));
                    SafeDestroy(vine.GetComponent<Collider>());
                    var r = vine.GetComponent<MeshRenderer>();
                    r.sharedMaterial = (k % 2 == 0 && l2VineDarkMat != null) ? l2VineDarkMat : l2VineMat;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
        }

        private void PopulateLevel2SegmentObstacles(RoadSegment seg)
        {
            if (seg == null || seg.obstaclesRoot == null) return;
            LoadLevel2Assets();
            Transform parent = seg.obstaclesRoot.transform;

            int step = totalSegmentsPopulated;
            totalSegmentsPopulated++;

            switch (GetLevel2PatternId(step))
            {
                case 0:
                    // First encounter: plant pod on the left, wrecked car on the right, centre open
                    SpawnPlantMonster(parent, new Vector3(-2.2f, 0f, 15.0f));
                    SpawnWreckBlocker(parent, l2CarPrefab, new Vector3(2.3f, 0f, 22.0f), 8f, destructible: false);
                    break;

                case 1:
                    // Twin pods flanking a gun upgrade
                    SpawnPlantMonster(parent, new Vector3(-2.4f, 0f, 16.0f));
                    SpawnPlantMonster(parent, new Vector3( 2.4f, 0f, 16.0f));
                    SpawnGunUpgradeContainer(parent, new Vector3(0.0f, 1.10f, 13.0f), 20);
                    break;

                case 2:
                    // Pickup wreck left, pod centre, right lane open
                    SpawnWreckBlocker(parent, l2PickupPrefab, new Vector3(-2.3f, 0f, 12.0f), -10f, destructible: false);
                    SpawnPlantMonster(parent, new Vector3(0.0f, 0f, 20.0f));
                    break;

                case 3:
                    // x2 squad gate with pods either side
                    SpawnDestructibleMultiplierContainer(parent, new Vector3(0.0f, 1.10f, 15.0f), 2, 20);
                    SpawnPlantMonster(parent, new Vector3(-2.4f, 0f,  9.0f));
                    SpawnPlantMonster(parent, new Vector3( 2.4f, 0f, 22.0f));
                    break;

                case 4:
                    // Slalom: three pods across left, right, centre
                    SpawnPlantMonster(parent, new Vector3(-2.2f, 0f,  8.0f));
                    SpawnPlantMonster(parent, new Vector3( 2.2f, 0f, 16.0f));
                    SpawnPlantMonster(parent, new Vector3( 0.0f, 0f, 24.0f));
                    break;

                case 5:
                    // School bus parked down the left lane, gun upgrade beside it, pod ahead on the right
                    SpawnWreckBlocker(parent, l2BusPrefab, new Vector3(-3.0f, 0f, 15.0f), 3f, destructible: false);
                    SpawnGunUpgradeContainer(parent, new Vector3(0.5f, 1.10f, 13.0f), 20);
                    SpawnPlantMonster(parent, new Vector3(2.4f, 0f, 25.0f));
                    break;

                case 6:
                    // x3 squad gate with flanking pods
                    SpawnDestructibleMultiplierContainer(parent, new Vector3(0.0f, 1.10f, 15.0f), 3, 24);
                    SpawnPlantMonster(parent, new Vector3(-2.3f, 0f, 18.0f));
                    SpawnPlantMonster(parent, new Vector3( 2.3f, 0f, 18.0f));
                    break;

                case 7:
                    // Wreck slalom: car left, pickup right, pod in the middle further on
                    SpawnWreckBlocker(parent, l2CarPrefab,    new Vector3(-2.3f, 0f,  9.0f), -15f, destructible: false);
                    SpawnWreckBlocker(parent, l2PickupPrefab, new Vector3( 2.3f, 0f, 19.0f), 168f, destructible: false);
                    SpawnPlantMonster(parent, new Vector3(0.0f, 0f, 27.0f));
                    break;

                case 8:
                    // x2 gate behind a pickup on the right, pod left
                    SpawnWreckBlocker(parent, l2PickupPrefab, new Vector3(2.4f, 0f, 11.0f), 172f, destructible: false);
                    SpawnDestructibleMultiplierContainer(parent, new Vector3(0.0f, 1.10f, 20.0f), 2, 22);
                    SpawnPlantMonster(parent, new Vector3(-2.3f, 0f, 22.0f));
                    break;

                case 9:
                default:
                    // Bus down the right lane, pods left and centre
                    SpawnWreckBlocker(parent, l2BusPrefab, new Vector3(3.0f, 0f, 16.0f), -4f, destructible: false);
                    SpawnPlantMonster(parent, new Vector3(-2.3f, 0f, 10.0f));
                    SpawnPlantMonster(parent, new Vector3( 0.0f, 0f, 23.0f));
                    break;
            }
        }

        /// <summary>
        /// A Meshy wreck parked in a lane. Solid (hurts on contact like level 1's burned cars).
        /// Level 2 wrecks are all solid scenery-blockers (destructible=false): they never explode or give rewards.
        /// </summary>
        private GameObject SpawnWreckBlocker(Transform parent, GameObject prefab, Vector3 localPos, float yaw, bool destructible)
        {
            if (prefab == null) return SpawnBurnedCarObstacle(parent, localPos, yaw);

            GameObject go = Instantiate(prefab, parent);
            go.name = "Obstacle_Wreck_" + prefab.name;
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            BoxCollider col = go.GetComponent<BoxCollider>();
            if (col == null)
            {
                col = go.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, 0.7f, 0f);
                col.size = new Vector3(1.85f, 1.4f, 4.0f);
            }
            col.isTrigger = false;

            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            RoadObstacle obs = go.AddComponent<RoadObstacle>();
            obs.Initialize(isContainer: false, damage: 1);

            if (destructible)
            {
                DestructibleGate dGate = go.AddComponent<DestructibleGate>();
                dGate.SetGateStats(15, 1, MobControl.Gates.GateOperation.Add, 2.0f);
            }
            return go;
        }

        private GameObject SpawnPlantMonster(Transform parent, Vector3 localPos)
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/PlantMonster_Prefab");
            if (prefab == null)
            {
#if UNITY_EDITOR
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/PlantMonster_Prefab.prefab");
#endif
            }
            // Fallback: keep the lane blocked even if the plant asset is missing from the build
            if (prefab == null) return SpawnBurnedCarObstacle(parent, localPos, Random.Range(-20f, 20f));

            GameObject plant = Instantiate(prefab, parent);
            plant.name = $"PlantMonster_{localPos.x:F1}_{localPos.z:F1}";
            plant.transform.localPosition = localPos;
            plant.transform.localRotation = Quaternion.identity;
            return plant;
        }

        private void PopulateSegmentObstacles(RoadSegment seg)
        {
            if (seg == null || seg.obstaclesRoot == null) return;

            // Level 1 is now the Army Run (ArmyRunLevel builds its own gates / barrels / hordes)
            if (ArmyRunLevel.HandlesLevel(currentLevel)) { totalSegmentsPopulated++; return; }

            if (currentLevel == 2)
            {
                PopulateLevel2SegmentObstacles(seg);
                return;
            }

            Transform parent = seg.obstaclesRoot.transform;

            int step = totalSegmentsPopulated;
            totalSegmentsPopulated++;

            // First 10 curated zones → then cycle the hard middle zones (3–9) endlessly
            int patternId = (step < 10) ? step : (3 + (step % 7));

            switch (patternId)
            {
                // ─── PHASE 1: Scout Rush Zones (Light/Open) ─────────────────────
                case 0:
                    // Zone 1: Warm-up — Burned Wreck Debut
                    // Abandoned smoking sedan on Right lane. Center and Left wide open!
                    SpawnBurnedCarObstacle(parent, new Vector3(2.2f, 0f, 16.0f), 14.0f);
                    break;

                case 1:
                    // Zone 2: Gun Upgrade Threshold — Immediate Early Powerup!
                    // Center Destructible Gun Upgrade Container (+RAPID FIRE, 15s) with 30-hit countdown!
                    SpawnGunUpgradeContainer(parent, new Vector3(0.0f, 1.10f, 15.0f), 30);
                    break;

                case 2:
                    // Zone 3: Molten Lava Fissure — First Hazard
                    // Bubbling lava across Left+Center. Right lane (X≈+2.5f) is safe asphalt!
                    SpawnLavaHazard(parent, new Vector3(-1.5f, 0f, 15.0f), new Vector2(3.8f, 6.5f));
                    break;

                // ─── PHASE 2: Mixed Assault Zones (Medium Difficulty) ──────────
                case 3:
                    // Zone 4: Squad Multiplier Gate
                    // Destructible Orange Container (x2 Multiplier). Shoot to double squad!
                    SpawnContainerObstacle(parent, new Vector3(0.0f, 1.10f, 15.0f), containerOrangeMat, isDestructible: true);
                    break;

                case 4:
                    // Zone 5: Double Burned Car Slalom + Center Gun Upgrade
                    // Left car at Z=8, Right car at Z=22 — S-curve challenge!
                    // Center Destructible Gun Upgrade Container at Z=15 as bonus reward (30-hit countdown).
                    SpawnBurnedCarObstacle(parent, new Vector3(-2.2f, 0f,  8.0f), -12.0f);
                    SpawnBurnedCarObstacle(parent, new Vector3( 2.2f, 0f, 22.0f),  15.0f);
                    SpawnGunUpgradeContainer(parent, new Vector3(0.0f, 1.10f, 15.0f), 30);
                    break;

                case 5:
                    // Zone 6: Twin Lava Canyon — Precision Center Bridge
                    // Lava pits on Left AND Right. Only narrow center asphalt bridge survives!
                    SpawnLavaHazard(parent, new Vector3(-2.8f, 0f, 15.0f), new Vector2(2.4f, 7.5f));
                    SpawnLavaHazard(parent, new Vector3( 2.8f, 0f, 15.0f), new Vector2(2.4f, 7.5f));
                    break;

                // ─── PHASE 3: Heavy Push Zones (Hard) ──────────────────────────
                case 6:
                    // Zone 7: Stacked Industrial Fortress + x2 Multiplier
                    // Double-stacked containers on Left (tall!) + x2 Orange Multiplier on Center-Right
                    SpawnStackedContainersObstacle(parent, new Vector3(-2.6f, 1.10f, 15.0f), containerBlueMat, containerRedMat);
                    SpawnDestructibleMultiplierContainer(parent, new Vector3(1.0f, 1.10f, 16.0f), 2, 30);
                    break;

                case 7:
                    // Zone 8: Burned Car + Lava Trap — Dual Hazard Gauntlet
                    // Burned car on Left (Z=9m), lava pool on Right (Z=21m). Weave!
                    SpawnBurnedCarObstacle(parent, new Vector3(-2.2f, 0f,  9.0f),  8.0f);
                    SpawnLavaHazard(parent,        new Vector3( 2.0f, 0f, 21.0f), new Vector2(2.6f, 6.0f));
                    break;

                case 8:
                    // Zone 9: Angled Container Gateway — High-Drama Funnel
                    // Wide comfortable central highway (~3.7m) between two angled containers
                    SpawnAngledContainerObstacle(parent, new Vector3(-3.25f, 1.10f, 15.0f),  8.0f, containerRedMat);
                    SpawnAngledContainerObstacle(parent, new Vector3( 3.25f, 1.10f, 15.0f), -8.0f,
                        containerGreenMat != null ? containerGreenMat : containerBlueMat);
                    break;

                // ─── BOSS THRESHOLD (Zone 10) ───────────────────────────────────
                case 9:
                    // Zone 10: Boss Arena Entrance
                    // Side containers frame the road shoulders — open center highway for Boss stomp!
                    SpawnContainerObstacle(parent, new Vector3(-3.8f, 1.10f, 15.0f), containerRedMat,  isDestructible: false);
                    SpawnContainerObstacle(parent, new Vector3( 3.8f, 1.10f, 15.0f), containerBlueMat, isDestructible: false);
                    break;
            }
        }

        private GameObject SpawnBurnedCarObstacle(Transform parent, Vector3 localPos, float yaw)
        {
            return BurnedCarBuilder.CreateBurnedCar(parent, localPos, Quaternion.Euler(0f, yaw, 0f), burnedCarMat, rustCarMat, tireMat);
        }

        private GameObject SpawnLavaHazard(Transform parent, Vector3 localPos, Vector2 size)
        {
            GameObject lavaRoot = new GameObject("Hazard_MoltenLavaPit");
            lavaRoot.transform.SetParent(parent, false);
            lavaRoot.transform.localPosition = localPos;

            float depth = 1.35f;
            float lavaY = -0.95f;
            float halfW = size.x * 0.5f;
            float halfL = size.y * 0.5f;
            float wallThick = 0.32f;

            // 1. Excavated Basalt Trench Pit Walls (descending to -1.35m)
            CreatePitWall(lavaRoot.transform, new Vector3(-halfW, -depth * 0.5f, 0f), new Vector3(wallThick, depth, size.y + wallThick * 2f), lavaCrustMat);
            CreatePitWall(lavaRoot.transform, new Vector3(halfW, -depth * 0.5f, 0f), new Vector3(wallThick, depth, size.y + wallThick * 2f), lavaCrustMat);
            CreatePitWall(lavaRoot.transform, new Vector3(0f, -depth * 0.5f, halfL), new Vector3(size.x + wallThick * 2f, depth, wallThick), lavaCrustMat);
            CreatePitWall(lavaRoot.transform, new Vector3(0f, -depth * 0.5f, -halfL), new Vector3(size.x + wallThick * 2f, depth, wallThick), lavaCrustMat);

            // 2. Solid Charred Pit Floor at -depth
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "PitFloor";
            floor.transform.SetParent(lavaRoot.transform, false);
            floor.transform.localPosition = new Vector3(0f, -depth, 0f);
            floor.transform.localScale = new Vector3(size.x + wallThick * 2f, 0.2f, size.y + wallThick * 2f);
            SafeDestroy(floor.GetComponent<Collider>());
            if (lavaCrustMat != null) floor.GetComponent<Renderer>().sharedMaterial = lavaCrustMat;

            // 3. Cracked Jagged Road Rim Lip Framing (+0.02f)
            CreatePitRim(lavaRoot.transform, size, lavaCrustMat);

            // 4. Boiling Molten Magma Surface resting deep inside the Pit (Y = -0.95m)
            GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Quad);
            surface.name = "LavaSurface";
            surface.transform.SetParent(lavaRoot.transform, false);
            surface.transform.localPosition = new Vector3(0f, lavaY, 0f);
            surface.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            surface.transform.localScale = new Vector3(size.x, size.y, 1f);
            SafeDestroy(surface.GetComponent<Collider>());
            if (lavaMat != null) surface.GetComponent<Renderer>().sharedMaterial = lavaMat;

            // 5. Trigger Collider spanning cavity from -1.4m to +0.8m
            BoxCollider trigger = lavaRoot.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, -0.2f, 0f);
            trigger.size = new Vector3(size.x, 1.8f, size.y);

            // 6. Attach LavaHazard component
            LavaHazard hazard = lavaRoot.AddComponent<LavaHazard>();
            hazard.Initialize(lavaMat, size);

            // 7. Volcanic Ambient Light (inside pit at Y = -0.50m)
            GameObject glowLight = new GameObject("LavaLight");
            glowLight.transform.SetParent(lavaRoot.transform, false);
            glowLight.transform.localPosition = new Vector3(0f, -0.45f, 0f);
            Light l = glowLight.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.42f, 0.05f);
            l.range = 6.5f;
            l.intensity = 2.4f;

            return lavaRoot;
        }

        private static void CreatePitWall(Transform parent, Vector3 localPos, Vector3 scale, Material mat)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "PitWall";
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = localPos;
            wall.transform.localScale = scale;
            SafeDestroy(wall.GetComponent<Collider>());
            if (mat != null) wall.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static void CreatePitRim(Transform parent, Vector2 size, Material mat)
        {
            float halfW = size.x * 0.5f;
            float halfL = size.y * 0.5f;
            float rimWidth = 0.24f;
            float rimHeight = 0.04f;

            CreatePitWall(parent, new Vector3(-halfW, 0.02f, 0f), new Vector3(rimWidth, rimHeight, size.y), mat);
            CreatePitWall(parent, new Vector3(halfW, 0.02f, 0f), new Vector3(rimWidth, rimHeight, size.y), mat);
            CreatePitWall(parent, new Vector3(0f, 0.02f, halfL), new Vector3(size.x, rimHeight, rimWidth), mat);
            CreatePitWall(parent, new Vector3(0f, 0.02f, -halfL), new Vector3(size.x, rimHeight, rimWidth), mat);
        }

        private GameObject SpawnStackedContainersObstacle(Transform parent, Vector3 localPos, Material bottomMat, Material topMat)
        {
            GameObject stackRoot = new GameObject("Obstacle_StackedContainers");
            stackRoot.transform.SetParent(parent, false);
            stackRoot.transform.localPosition = localPos;

            // Bottom Container
            SpawnContainerObstacle(stackRoot.transform, Vector3.zero, bottomMat, isDestructible: false);

            // Top Container (stacked at Y = 2.10m)
            SpawnContainerObstacle(stackRoot.transform, new Vector3(0f, 2.10f, 0f), topMat, isDestructible: false);

            return stackRoot;
        }

        private GameObject SpawnAngledContainerObstacle(Transform parent, Vector3 localPos, float yaw, Material mat)
        {
            GameObject container = SpawnContainerObstacle(parent, localPos, mat, isDestructible: false);
            container.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return container;
        }

        private GameObject SpawnDestructibleMultiplierContainer(Transform parent, Vector3 localPos, int multiplier, int health)
        {
            GameObject containerRoot = new GameObject($"Obstacle_Container_Multiplier_x{multiplier}");
            containerRoot.transform.SetParent(parent, false);
            containerRoot.transform.localPosition = localPos;

            // Container Body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "ContainerBody";
            body.transform.SetParent(containerRoot.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(2.45f, 2.05f, 3.6f);
            SafeDestroy(body.GetComponent<Collider>());
            if (containerOrangeMat != null) body.GetComponent<MeshRenderer>().sharedMaterial = containerOrangeMat;

            // Corner steel frames
            Material frameMat = new Material(Shader.Find("Standard"));
            frameMat.color = new Color(0.12f, 0.12f, 0.14f);
            for (int xSide = -1; xSide <= 1; xSide += 2)
            {
                for (int zSide = -1; zSide <= 1; zSide += 2)
                {
                    GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    post.name = "CornerPost";
                    post.transform.SetParent(containerRoot.transform, false);
                    post.transform.localPosition = new Vector3(xSide * 1.24f, 0f, zSide * 1.82f);
                    post.transform.localScale = new Vector3(0.12f, 2.10f, 0.12f);
                    SafeDestroy(post.GetComponent<Collider>());
                    post.GetComponent<MeshRenderer>().sharedMaterial = frameMat;
                }
            }

            // Hazard Striped warning bars on front face
            if (hazardMat != null)
            {
                GameObject hazardStrip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                hazardStrip.name = "FrontHazardStrip";
                hazardStrip.transform.SetParent(containerRoot.transform, false);
                hazardStrip.transform.localPosition = new Vector3(0f, -0.65f, -1.82f);
                hazardStrip.transform.localScale = new Vector3(2.40f, 0.35f, 0.08f);
                SafeDestroy(hazardStrip.GetComponent<Collider>());
                hazardStrip.GetComponent<MeshRenderer>().sharedMaterial = hazardMat;
            }

            BoxCollider col = containerRoot.AddComponent<BoxCollider>();
            col.isTrigger = false;
            col.size = new Vector3(2.55f, 2.2f, 3.7f);
            col.center = Vector3.zero;

            Rigidbody rb = containerRoot.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            RoadObstacle obs = containerRoot.AddComponent<RoadObstacle>();
            obs.Initialize(isContainer: true, damage: 1);

            DestructibleGate dGate = containerRoot.AddComponent<DestructibleGate>();
            dGate.SetGateStats(health, multiplier, MobControl.Gates.GateOperation.Multiply, 2.45f);

            // Multiplier Badge
            GameObject badge = GameObject.CreatePrimitive(PrimitiveType.Quad);
            badge.name = "MultiplierBadge";
            badge.transform.SetParent(containerRoot.transform, false);
            badge.transform.localPosition = new Vector3(0f, 0.20f, -1.84f);
            badge.transform.localScale = new Vector3(1.4f, 0.75f, 1f);
            SafeDestroy(badge.GetComponent<Collider>());

            Material badgeMat = NewRoadBuilder.LoadTextureMaterialPublic("Textures/tex_blue_multiplier_badge.png", "M_BlueMultiplierBadge");
            if (badgeMat != null) badge.GetComponent<MeshRenderer>().sharedMaterial = badgeMat;

            GameObject textObj = new GameObject("MultiplierText");
            textObj.transform.SetParent(containerRoot.transform, false);
            textObj.transform.localPosition = new Vector3(0f, 0.15f, -1.85f);
            TMPro.TextMeshPro tmp = textObj.AddComponent<TMPro.TextMeshPro>();
            tmp.text = $"x{multiplier}";
            tmp.fontSize = 6.5f;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.fontStyle = TMPro.FontStyles.Bold;
            tmp.color = Color.white;

            return containerRoot;
        }

        private GameObject SpawnGunUpgradeContainer(Transform parent, Vector3 localPos, int health = 30)
        {
            GameObject containerRoot = new GameObject("Obstacle_Container_GunUpgrade");
            containerRoot.transform.SetParent(parent, false);
            containerRoot.transform.localPosition = localPos;

            // 1. Container Body (Clean shipping container - no glowing borders/posts)
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "ContainerBody";
            body.transform.SetParent(containerRoot.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(2.45f, 2.05f, 3.6f);
            SafeDestroy(body.GetComponent<Collider>());
            if (containerRedMat != null)
                body.GetComponent<MeshRenderer>().sharedMaterial = containerRedMat;
            else if (containerPurpleMat != null)
                body.GetComponent<MeshRenderer>().sharedMaterial = containerPurpleMat;

            BoxCollider col = containerRoot.AddComponent<BoxCollider>();
            col.isTrigger = false;
            col.size = new Vector3(2.55f, 2.2f, 3.7f);
            col.center = Vector3.zero;

            Rigidbody rb = containerRoot.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            RoadObstacle obs = containerRoot.AddComponent<RoadObstacle>();
            obs.Initialize(isContainer: true, damage: 1);

            // 2. Front Health / Hit Counter Badge (Stylized, matches game theme)
            GameObject badgePlate = GameObject.CreatePrimitive(PrimitiveType.Quad);
            badgePlate.name = "ContainerHealthBadge";
            badgePlate.transform.SetParent(containerRoot.transform, false);
            badgePlate.transform.localPosition = new Vector3(0f, 0.20f, -1.84f);
            badgePlate.transform.localScale = new Vector3(1.35f, 0.78f, 1f);
            SafeDestroy(badgePlate.GetComponent<Collider>());

            Material badgeMat = NewRoadBuilder.LoadTextureMaterialPublic("Textures/tex_blue_multiplier_badge.png", "M_ContainerHitBadge");
            if (badgeMat != null) badgePlate.GetComponent<MeshRenderer>().sharedMaterial = badgeMat;

            GameObject textObj = new GameObject("HealthText");
            textObj.transform.SetParent(containerRoot.transform, false);
            textObj.transform.localPosition = new Vector3(0f, 0.17f, -1.85f);
            TMPro.TextMeshPro tmp = textObj.AddComponent<TMPro.TextMeshPro>();
            tmp.text = $"<color=#FFD700><b>{health}</b></color>";
            tmp.fontSize = 7.5f;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.fontStyle = TMPro.FontStyles.Bold;

            DestructibleGate dGate = containerRoot.AddComponent<DestructibleGate>();
            dGate.SetGunUpgradeStats(health, speedMult: 2.2f, width: 2.45f);

            // 3. Top Static Isolated Gun Emblem
            GameObject badgeRoot = new GameObject("TopGunUpgrade_IsolatedGun");
            badgeRoot.transform.SetParent(containerRoot.transform, false);
            badgeRoot.transform.localPosition = new Vector3(0f, 1.75f, 0f);

            Material topBadgeMat = LoadTransparentMaterial("Textures/GunUpgrade_IsolatedGun.png", "M_GunUpgrade_IsolatedGun");

            // Front Quad (Facing towards oncoming player / camera)
            GameObject badgeFront = GameObject.CreatePrimitive(PrimitiveType.Quad);
            badgeFront.name = "BadgeFront";
            badgeFront.transform.SetParent(badgeRoot.transform, false);
            badgeFront.transform.localPosition = Vector3.zero;
            badgeFront.transform.localRotation = Quaternion.identity;
            badgeFront.transform.localScale = new Vector3(1.85f, 1.85f, 1f);
            SafeDestroy(badgeFront.GetComponent<Collider>());
            if (topBadgeMat != null) badgeFront.GetComponent<MeshRenderer>().sharedMaterial = topBadgeMat;

            // Back Quad (Facing opposite direction for 2-sided visibility)
            GameObject badgeBack = GameObject.CreatePrimitive(PrimitiveType.Quad);
            badgeBack.name = "BadgeBack";
            badgeBack.transform.SetParent(badgeRoot.transform, false);
            badgeBack.transform.localPosition = Vector3.zero;
            badgeBack.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            badgeBack.transform.localScale = new Vector3(1.85f, 1.85f, 1f);
            SafeDestroy(badgeBack.GetComponent<Collider>());
            if (topBadgeMat != null) badgeBack.GetComponent<MeshRenderer>().sharedMaterial = topBadgeMat;

            return containerRoot;
        }

        private static Material LoadTransparentMaterial(string texRelativePath, string matName)
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
                    string fullPath = System.IO.Path.Combine(Application.dataPath, texRelativePath);
                    if (System.IO.File.Exists(fullPath))
                    {
                        byte[] bytes = System.IO.File.ReadAllBytes(fullPath);
                        tex = new Texture2D(2, 2, TextureFormat.RGBA32, true);
                        tex.LoadImage(bytes);
                    }
                }
#endif
            }
            catch { }

            Shader s = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Transparent") ?? Shader.Find("Standard");
            Material mat = new Material(s) { name = matName };
            if (tex != null)
            {
                tex.name = matName + "_Tex";
                tex.filterMode = FilterMode.Bilinear;
                mat.mainTexture = tex;
            }
            return mat;
        }

        private GameObject SpawnBarricadeObstacle(Transform parent, Vector3 localPos, float width)
        {
            GameObject barricadeRoot = new GameObject("Obstacle_Barricade");
            barricadeRoot.transform.SetParent(parent, false);
            barricadeRoot.transform.localPosition = localPos;

            // Visual Main Plank (Hazard diagonal yellow-black stripes)
            GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "BarricadeBoard";
            board.transform.SetParent(barricadeRoot.transform, false);
            board.transform.localPosition = Vector3.zero;
            board.transform.localScale = new Vector3(width, 0.70f, 0.16f);
            SafeDestroy(board.GetComponent<Collider>());
            if (hazardMat != null) board.GetComponent<MeshRenderer>().sharedMaterial = hazardMat;

            // Left & Right Support Legs
            Material legMat = new Material(Shader.Find("Standard"));
            legMat.color = new Color(0.18f, 0.18f, 0.20f);
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leg.name = "Leg";
                leg.transform.SetParent(barricadeRoot.transform, false);
                leg.transform.localPosition = new Vector3(side * (width * 0.45f), -0.22f, 0f);
                leg.transform.localScale = new Vector3(0.14f, 0.65f, 0.40f);
                SafeDestroy(leg.GetComponent<Collider>());
                leg.GetComponent<MeshRenderer>().sharedMaterial = legMat;

                // Orange hazard flashing warning light on top of each post
                GameObject light = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                light.name = "WarningLight";
                light.transform.SetParent(barricadeRoot.transform, false);
                light.transform.localPosition = new Vector3(side * (width * 0.45f), 0.48f, 0f);
                light.transform.localScale = new Vector3(0.24f, 0.12f, 0.24f);
                SafeDestroy(light.GetComponent<Collider>());
                Material lightMat = new Material(Shader.Find("Standard"));
                lightMat.color = new Color(1f, 0.55f, 0.05f);
                lightMat.EnableKeyword("_EMISSION");
                lightMat.SetColor("_EmissionColor", new Color(2.5f, 1.2f, 0.1f));
                light.GetComponent<MeshRenderer>().sharedMaterial = lightMat;
            }

            // Solid BoxCollider (NOT A TRIGGER) & Kinematic Rigidbody with Continuous Collision
            BoxCollider col = barricadeRoot.AddComponent<BoxCollider>();
            col.isTrigger = false;
            col.size = new Vector3(width, 1.5f, 0.8f);
            col.center = new Vector3(0f, 0.2f, 0f);

            Rigidbody rb = barricadeRoot.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            RoadObstacle obs = barricadeRoot.AddComponent<RoadObstacle>();
            obs.Initialize(isContainer: false, damage: 1);

            return barricadeRoot;
        }

        private GameObject SpawnContainerObstacle(Transform parent, Vector3 localPos, Material mat, bool isDestructible)
        {
            GameObject containerRoot = new GameObject(isDestructible ? "Obstacle_Container_Destructible" : "Obstacle_Container_Solid");
            containerRoot.transform.SetParent(parent, false);
            containerRoot.transform.localPosition = localPos;

            // Container Body (Industrial corrugated steel box)
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "ContainerBody";
            body.transform.SetParent(containerRoot.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(2.45f, 2.05f, 3.6f);
            SafeDestroy(body.GetComponent<Collider>());
            if (mat != null) body.GetComponent<MeshRenderer>().sharedMaterial = mat;

            // Corner steel frames
            Material frameMat = new Material(Shader.Find("Standard"));
            frameMat.color = new Color(0.12f, 0.12f, 0.14f);
            for (int xSide = -1; xSide <= 1; xSide += 2)
            {
                for (int zSide = -1; zSide <= 1; zSide += 2)
                {
                    GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    post.name = "CornerPost";
                    post.transform.SetParent(containerRoot.transform, false);
                    post.transform.localPosition = new Vector3(xSide * 1.24f, 0f, zSide * 1.82f);
                    post.transform.localScale = new Vector3(0.12f, 2.10f, 0.12f);
                    SafeDestroy(post.GetComponent<Collider>());
                    post.GetComponent<MeshRenderer>().sharedMaterial = frameMat;
                }
            }

            // Hazard Striped warning bars on front face
            if (hazardMat != null)
            {
                GameObject hazardStrip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                hazardStrip.name = "FrontHazardStrip";
                hazardStrip.transform.SetParent(containerRoot.transform, false);
                hazardStrip.transform.localPosition = new Vector3(0f, -0.65f, -1.82f);
                hazardStrip.transform.localScale = new Vector3(2.40f, 0.35f, 0.08f);
                SafeDestroy(hazardStrip.GetComponent<Collider>());
                hazardStrip.GetComponent<MeshRenderer>().sharedMaterial = hazardMat;
            }

            // Solid BoxCollider (NOT A TRIGGER) & Kinematic Rigidbody with Continuous Collision
            BoxCollider col = containerRoot.AddComponent<BoxCollider>();
            col.isTrigger = false;
            col.size = new Vector3(2.55f, 2.2f, 3.7f);
            col.center = Vector3.zero;

            Rigidbody rb = containerRoot.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            RoadObstacle obs = containerRoot.AddComponent<RoadObstacle>();
            obs.Initialize(isContainer: true, damage: 1);

            // If destructible, also attach DestructibleGate to reward squad duplication & gun speed
            if (isDestructible)
            {
                DestructibleGate dGate = containerRoot.AddComponent<DestructibleGate>();
                dGate.SetGateStats(24, 2, MobControl.Gates.GateOperation.Multiply, 2.45f);

                // Cyan Multiplier Badge on front
                GameObject badge = GameObject.CreatePrimitive(PrimitiveType.Quad);
                badge.name = "MultiplierBadge";
                badge.transform.SetParent(containerRoot.transform, false);
                badge.transform.localPosition = new Vector3(0f, 0.20f, -1.84f);
                badge.transform.localScale = new Vector3(1.4f, 0.75f, 1f);
                SafeDestroy(badge.GetComponent<Collider>());

                Material badgeMat = NewRoadBuilder.LoadTextureMaterialPublic("Textures/tex_blue_multiplier_badge.png", "M_BlueMultiplierBadge");
                if (badgeMat != null) badge.GetComponent<MeshRenderer>().sharedMaterial = badgeMat;

                GameObject textObj = new GameObject("MultiplierText");
                textObj.transform.SetParent(containerRoot.transform, false);
                textObj.transform.localPosition = new Vector3(0f, 0.15f, -1.85f);
                TMPro.TextMeshPro tmp = textObj.AddComponent<TMPro.TextMeshPro>();
                tmp.text = "×2";
                tmp.fontSize = 6.5f;
                tmp.alignment = TMPro.TextAlignmentOptions.Center;
                tmp.fontStyle = TMPro.FontStyles.Bold;
                tmp.color = Color.white;
            }

            return containerRoot;
        }

        public class RoadSegment
        {
            public int index;
            public GameObject rootObj;
            public GameObject obstaclesRoot;
            public MeshFilter roadMeshFilter;

            public void ClearObstacles()
            {
                if (obstaclesRoot == null) return;
                for (int i = obstaclesRoot.transform.childCount - 1; i >= 0; i--)
                {
                    SafeDestroy(obstaclesRoot.transform.GetChild(i).gameObject);
                }
            }
        }
    }

    /// <summary>
    /// Smooth floating bob and spin for the 3D holographic weapon badge above the Gun Upgrade container.
    /// Also gives the front shield badge a dynamic energetic breathing glow pulse.
    /// </summary>
    public class GunContainerHoloRotator : MonoBehaviour
    {
        [SerializeField] private float rotateSpeed = 65.0f;
        [SerializeField] private float bobAmplitude = 0.08f;
        [SerializeField] private float bobSpeed = 2.8f;

        private Vector3 initialLocalPos;
        private Transform frontBadgeTransform;
        private Vector3 initialBadgeScale = Vector3.one;

        public void SetFrontBadge(Transform badge)
        {
            frontBadgeTransform = badge;
            if (badge != null) initialBadgeScale = badge.localScale;
        }

        private void Start()
        {
            initialLocalPos = transform.localPosition;
        }

        private void Update()
        {
            // 3D holographic rotation
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);

            // Smooth floating vertical bob
            float bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
            transform.localPosition = initialLocalPos + new Vector3(0f, bobOffset, 0f);

            // Subtle pulsing energy breath on front badge
            if (frontBadgeTransform != null)
            {
                float pulse = 1.0f + Mathf.Sin(Time.time * 3.5f) * 0.04f;
                frontBadgeTransform.localScale = initialBadgeScale * pulse;
            }
        }
    }
}

