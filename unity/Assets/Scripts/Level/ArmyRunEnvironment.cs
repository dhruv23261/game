using System.Collections.Generic;
using UnityEngine;

namespace MobControl.Level
{
    /// <summary>
    /// LEVEL 1 world — a sea crossing like the Top War reference.
    ///
    ///   x:  -13.7 ... -10.5 ... -7.3        -3.6 ........ 0 ........ +3.6
    ///       | ENEMY BRIDGE (red legion) |water|     MAIN BRIDGE (player)    |   water, islands, ships
    ///
    /// • MAIN BRIDGE: 7.2 m wide like the Mob Control road; full width is playable (commander steers ±3.0).
    /// • ENEMY BRIDGE: the red Dark Legion parade marches on it (pure scenery, can't be shot).
    /// • Bridge = identical 20 m segments that scroll toward the player and are recycled to the front.
    ///   Every 7th segment carries a suspension-bridge tower with cables.
    /// • Scenery layers scroll with parallax: buoys 1×, ships 0.8×, islands 0.3×, clouds 0.05×.
    /// • Sky dome + fog + warm sun + trilight ambient. Old forest highway is hidden while Level 1 runs.
    /// </summary>
    public class ArmyRunEnvironment
    {
        // ─── Layout (metres) ───
        public const float ROAD_LEFT = -3.6f;
        public const float ROAD_RIGHT = 3.6f;
        public const float ENEMY_LANE_X = -10.5f;
        public const float ENEMY_HALF = 3.2f;
        public const float WATER_Y = -3.0f;
        public const float SEG_LEN = 20f;
        public const int SEG_COUNT = 15;
        public const int PYLON_EVERY = 7;
        public static float RoadCenter => 0f;
        /// <summary>
        /// true  = Mob Control "Cave Spike Roller" look: one elevated road over a misty snowy pine forest,
        ///         +1 strip lane on the right (default for Level 1).
        /// false = the old sea-bridge world.
        /// </summary>
        public static bool SnowForest = true;
        public const float STRIP_LEFT = 2.05f;      // +1 strip lane: x 2.05 … 3.6

        // ─── Palette ───
        private static readonly Color C_DECK = new Color(0.78f, 0.79f, 0.82f);
        private static readonly Color C_DECK2 = new Color(0.70f, 0.72f, 0.76f);
        private static readonly Color C_SIDE = new Color(0.52f, 0.56f, 0.63f);
        private static readonly Color C_LINE = new Color(0.98f, 0.98f, 0.98f);
        private static readonly Color C_BARRIER = new Color(0.92f, 0.93f, 0.96f);
        private static readonly Color C_STRIPE = new Color(0.20f, 0.45f, 0.85f);
        private static readonly Color C_POLE = new Color(0.30f, 0.33f, 0.40f);
        private static readonly Color C_PYLON = new Color(0.86f, 0.36f, 0.24f);   // red-orange bridge towers
        private static readonly Color C_CABLE = new Color(0.95f, 0.95f, 0.97f);
        private static readonly Color C_WATER = new Color(0.10f, 0.47f, 0.86f);
        private static readonly Color C_SKY_TOP = new Color(0.30f, 0.58f, 0.95f);
        private static readonly Color C_SKY_HORIZON = new Color(0.78f, 0.90f, 1.00f);
        private static readonly Color C_FOG = new Color(0.72f, 0.86f, 0.98f);

        private Transform root;
        private readonly List<Transform> segments = new List<Transform>();
        private readonly List<GameObject> segmentPylons = new List<GameObject>();
        private int segmentCounter;

        private class Layer { public float factor, wrapLen, minZ; public readonly List<Transform> items = new List<Transform>(); }
        private readonly List<Layer> layers = new List<Layer>();
        private readonly List<Transform> bobbers = new List<Transform>();
        private Transform skyDome;
        private Material waterMat;
        private float waterOffset;
        private const float WATER_TILE_M = 8f;

        // restore info
        private readonly List<GameObject> hiddenObjects = new List<GameObject>();
        private bool saved;
        private bool prevFog;
        private Color prevFogColor, prevBg, prevAmbSky, prevAmbEq, prevAmbGround, prevSunColor;
        private float prevFogStart, prevFogEnd, prevSunIntensity, prevShadowDist;
        private UnityEngine.Rendering.AmbientMode prevAmbMode;
        private Quaternion prevSunRot;
        private CameraClearFlags prevClear;
        private Light sun;

        private static Dictionary<Material, Mesh> s_segMeshes, s_pylonMeshes;
        private static readonly Dictionary<int, Material> s_mats = new Dictionary<int, Material>();

        // ═════════════════════════════════════ Lifecycle ══

        public void Build(Transform parent, Camera cam)
        {
            root = new GameObject("ArmyRun_Environment").transform;
            root.SetParent(parent, false);

            HideOldLevel1Environment();
            if (SnowForest) { BuildSnowForest(cam); return; }
            ApplyAtmosphere(cam);

            if (s_segMeshes == null) s_segMeshes = BuildSegmentMeshes();
            if (s_pylonMeshes == null) s_pylonMeshes = BuildPylonMeshes();

            for (int i = 0; i < SEG_COUNT; i++)
            {
                var seg = new GameObject("BridgeSegment").transform;
                seg.SetParent(root, false);
                seg.localPosition = new Vector3(0f, 0f, -2f * SEG_LEN + i * SEG_LEN);
                AddMeshes(seg, s_segMeshes);
                var pylon = new GameObject("Pylon").transform;
                pylon.SetParent(seg, false);
                AddMeshes(pylon, s_pylonMeshes);
                segments.Add(seg);
                segmentPylons.Add(pylon.gameObject);
                pylon.gameObject.SetActive(segmentCounter % PYLON_EVERY == 3);
                segmentCounter++;
            }

            BuildSky();
            BuildWater();
            BuildBuoys();
            BuildShips();
            BuildIslands();
            BuildClouds();
        }

        /// <summary>Move everything toward the player by dz (world metres the army "ran").</summary>
        public void Scroll(float dz)
        {
            if (root == null) return;
            if (dz != 0f)
            {
                for (int i = 0; i < segments.Count; i++)
                {
                    var s = segments[i];
                    Vector3 p = s.localPosition;
                    p.z -= dz;
                    if (p.z < -2f * SEG_LEN)
                    {
                        p.z += SEG_LEN * SEG_COUNT;
                        segmentPylons[i].SetActive(segmentCounter % PYLON_EVERY == 3);
                        segmentCounter++;
                    }
                    s.localPosition = p;
                }
                foreach (var L in layers)
                    foreach (var t in L.items)
                    {
                        Vector3 p = t.localPosition;
                        p.z -= dz * L.factor;
                        if (p.z < L.minZ) p.z += L.wrapLen;
                        t.localPosition = p;
                    }
                if (waterMat != null)
                {
                    waterOffset = Mathf.Repeat(waterOffset + dz / WATER_TILE_M, 1f);
                    waterMat.mainTextureOffset = new Vector2(0f, waterOffset);
                }
            }
        }

        /// <summary>Per-frame idle animation (buoys bob, ships rock, sky follows camera).</summary>
        public void Tick(Camera cam)
        {
            if (root == null) return;
            float t = Time.time;
            for (int i = 0; i < bobbers.Count; i++)
            {
                var b = bobbers[i];
                if (b == null) continue;
                Vector3 p = b.localPosition;
                p.y = WATER_Y + Mathf.Sin(t * 1.6f + i * 1.7f) * 0.12f;
                b.localPosition = p;
                b.localRotation = Quaternion.Euler(Mathf.Sin(t * 1.3f + i) * 4f, b.localEulerAngles.y, Mathf.Cos(t * 1.1f + i) * 3f);
            }
            if (skyDome != null && cam != null) skyDome.position = cam.transform.position;
        }

        public void Dispose()
        {
            foreach (var go in hiddenObjects) if (go != null) go.SetActive(true);
            hiddenObjects.Clear();
            if (saved)
            {
                RenderSettings.fog = prevFog;
                RenderSettings.fogColor = prevFogColor;
                RenderSettings.fogStartDistance = prevFogStart;
                RenderSettings.fogEndDistance = prevFogEnd;
                RenderSettings.ambientMode = prevAmbMode;
                RenderSettings.ambientSkyColor = prevAmbSky;
                RenderSettings.ambientEquatorColor = prevAmbEq;
                RenderSettings.ambientGroundColor = prevAmbGround;
                QualitySettings.shadowDistance = prevShadowDist;
                if (sun != null) { sun.color = prevSunColor; sun.intensity = prevSunIntensity; sun.transform.rotation = prevSunRot; }
                var cam = Camera.main;
                if (cam != null) { cam.backgroundColor = prevBg; cam.clearFlags = prevClear; }
                saved = false;
            }
            if (root != null) Object.Destroy(root.gameObject);
            root = null;
            segments.Clear(); segmentPylons.Clear(); layers.Clear(); bobbers.Clear();
        }

        // ═════════════════════════════════════ Snow forest (Mob Control reference) ══

        private static Dictionary<Material, Mesh> s_snowSegMeshes;
        private static readonly List<Dictionary<Material, Mesh>> s_pineTiles = new List<Dictionary<Material, Mesh>>();
        private static readonly Color C_SNOW_FOG = new Color(0.80f, 0.82f, 0.86f);
        private const float GROUND_Y = -7f;
        private const float PINE_TILE = 40f;

        private void BuildSnowForest(Camera cam)
        {
            ApplySnowAtmosphere(cam);
            if (s_snowSegMeshes == null) s_snowSegMeshes = BuildSnowSegmentMeshes();
            for (int i = 0; i < SEG_COUNT; i++)
            {
                var seg = new GameObject("RoadSegment").transform;
                seg.SetParent(root, false);
                seg.localPosition = new Vector3(0f, 0f, -2f * SEG_LEN + i * SEG_LEN);
                AddMeshes(seg, s_snowSegMeshes);
                var none = new GameObject("NoPylon");
                none.transform.SetParent(seg, false);
                segments.Add(seg);
                segmentPylons.Add(none);
            }

            // snowy ground far below the road (plain colour: the fog does the rest)
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "SnowGround";
            Object.Destroy(ground.GetComponent<Collider>());
            ground.transform.SetParent(root, false);
            ground.transform.localPosition = new Vector3(0f, GROUND_Y, 200f);
            ground.transform.localScale = new Vector3(100f, 1f, 100f);
            var gr = ground.GetComponent<MeshRenderer>();
            gr.sharedMaterial = Mat(new Color(0.88f, 0.9f, 0.93f), 0.05f);
            gr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // pine forest: 8 tiles of 40 m, 1× scroll, recycled to the front
            if (s_pineTiles.Count == 0)
                for (int k = 0; k < 3; k++) s_pineTiles.Add(BuildPineTile(k));
            var L = NewLayer(1f, -2f * PINE_TILE, PINE_TILE * 9f);
            for (int i = 0; i < 9; i++)
            {
                var tile = new GameObject("PineTile").transform;
                tile.SetParent(root, false);
                tile.localPosition = new Vector3(0f, GROUND_Y, -PINE_TILE + i * PINE_TILE);
                AddMeshes(tile, s_pineTiles[i % s_pineTiles.Count]);
                L.items.Add(tile);
            }
        }

        private void ApplySnowAtmosphere(Camera cam)
        {
            prevFog = RenderSettings.fog; prevFogColor = RenderSettings.fogColor;
            prevFogStart = RenderSettings.fogStartDistance; prevFogEnd = RenderSettings.fogEndDistance;
            prevAmbMode = RenderSettings.ambientMode; prevAmbSky = RenderSettings.ambientSkyColor;
            prevAmbEq = RenderSettings.ambientEquatorColor; prevAmbGround = RenderSettings.ambientGroundColor;
            prevShadowDist = QualitySettings.shadowDistance;
            if (cam != null) { prevBg = cam.backgroundColor; prevClear = cam.clearFlags; }
            sun = FindSun();
            if (sun != null) { prevSunColor = sun.color; prevSunIntensity = sun.intensity; prevSunRot = sun.transform.rotation; }
            saved = true;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = C_SNOW_FOG;
            RenderSettings.fogStartDistance = 24f;       // thick mist: the far end of the swarm fades out
            RenderSettings.fogEndDistance = 105f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.86f, 0.89f, 0.95f);
            RenderSettings.ambientEquatorColor = new Color(0.72f, 0.75f, 0.80f);
            RenderSettings.ambientGroundColor = new Color(0.50f, 0.53f, 0.58f);
            QualitySettings.shadowDistance = 40f;
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(58f, -25f, 0f);
                sun.color = new Color(1f, 0.97f, 0.93f);
                sun.intensity = 1.05f;
            }
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = C_SNOW_FOG;
                cam.farClipPlane = Mathf.Max(cam.farClipPlane, 300f);
            }
        }

        /// <summary>
        /// One 20 m piece of the elevated road: light concrete deck, dark low side walls, the +1 strip lane
        /// (x 2.05 … 3.6, bluish steel with a curb) and a tall dark pier wall down into the mist.
        /// </summary>
        private static Dictionary<Material, Mesh> BuildSnowSegmentMeshes()
        {
            Material deck = Mat(new Color(0.80f, 0.81f, 0.84f), 0.1f), seam = Mat(new Color(0.72f, 0.73f, 0.77f), 0.1f);
            Material wall = Mat(new Color(0.32f, 0.34f, 0.39f), 0.2f), wallTop = Mat(new Color(0.55f, 0.57f, 0.62f), 0.3f);
            Material pier = Mat(new Color(0.40f, 0.42f, 0.47f), 0.05f);
            Material strip = Mat(new Color(0.58f, 0.62f, 0.70f), 0.35f), curb = Mat(new Color(0.30f, 0.33f, 0.40f), 0.3f);

            var temp = new GameObject("SnowSegTemp").transform;
            float len = SEG_LEN, mid = len * 0.5f;
            float W = ROAD_RIGHT - ROAD_LEFT;
            Part(temp, PrimitiveType.Cube, new Vector3((ROAD_LEFT + STRIP_LEFT) * 0.5f, -0.25f, mid), new Vector3(STRIP_LEFT - ROAD_LEFT, 0.5f, len), deck);
            Part(temp, PrimitiveType.Cube, new Vector3((STRIP_LEFT + ROAD_RIGHT) * 0.5f, -0.22f, mid), new Vector3(ROAD_RIGHT - STRIP_LEFT, 0.5f, len), strip);
            Part(temp, PrimitiveType.Cube, new Vector3(STRIP_LEFT, 0.06f, mid), new Vector3(0.12f, 0.14f, len), curb);
            for (float z = 0f; z < len; z += 4f)
                Part(temp, PrimitiveType.Cube, new Vector3((ROAD_LEFT + STRIP_LEFT) * 0.5f, 0.004f, z), new Vector3(STRIP_LEFT - ROAD_LEFT - 0.3f, 0.01f, 0.05f), seam);
            foreach (float sx in new[] { ROAD_LEFT - 0.15f, ROAD_RIGHT + 0.15f })
            {
                Part(temp, PrimitiveType.Cube, new Vector3(sx, 0.2f, mid), new Vector3(0.3f, 0.9f, len), wall);
                Part(temp, PrimitiveType.Cube, new Vector3(sx, 0.66f, mid), new Vector3(0.34f, 0.04f, len), wallTop);
            }
            Part(temp, PrimitiveType.Cube, new Vector3(0f, (GROUND_Y - 0.5f) * 0.5f, mid), new Vector3(W + 0.4f, -GROUND_Y - 0.5f, len), pier);
            return Combine(temp);
        }

        /// <summary>40 m of forest on both sides: ~70 snowy pines (stacked cones + white tips) in 2 meshes.</summary>
        private static Dictionary<Material, Mesh> BuildPineTile(int seed)
        {
            var state = Random.state;
            Random.InitState(9173 + seed * 31);
            // CC0 snowy pine (Quaternius, baked) when available: ~36 per tile, random size / turn, merged
            var pine = ArmyRunBakedModels.Load("SnowPine");
            if (pine != null && pine.materials.Length == 1)
            {
                var t0 = new GameObject("PineTemp").transform;
                Mesh pm = pine.frames[0][0];
                for (int i = 0; i < 36; i++)
                {
                    float side = i % 2 == 0 ? -1f : 1f;
                    float x = side * Random.Range(5.0f, 42f);
                    float z = Random.Range(0f, PINE_TILE);
                    float h = Random.Range(7f, 12.5f) * (Mathf.Abs(x) < 9f ? 0.85f : 1f);
                    var part = MeshPart(t0, pm, new Vector3(x, 0f, z), Vector3.one * h, pine.materials[0]);
                    part.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                }
                Random.state = state;
                return Combine(t0);
            }
            Material leaf = Mat(new Color(0.22f, 0.31f, 0.33f), 0.05f), leaf2 = Mat(new Color(0.28f, 0.38f, 0.40f), 0.05f);
            Material snow = Mat(new Color(0.93f, 0.95f, 0.98f), 0.1f), trunk = Mat(new Color(0.30f, 0.24f, 0.20f), 0.05f);
            Mesh cone = ConeMesh(7);
            var temp = new GameObject("PineTemp").transform;
            for (int i = 0; i < 70; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float x = side * Random.Range(5.2f, 42f);
                float z = Random.Range(0f, PINE_TILE);
                float h = Random.Range(6.5f, 11.5f);
                float r = h * Random.Range(0.2f, 0.26f);
                Part(temp, PrimitiveType.Cylinder, new Vector3(x, h * 0.1f, z), new Vector3(r * 0.25f, h * 0.1f, r * 0.25f), trunk);
                Material lm = Random.value < 0.5f ? leaf : leaf2;
                for (int k = 0; k < 3; k++)
                {
                    float y0 = h * (0.15f + k * 0.24f);
                    float lr = r * (1f - k * 0.26f);
                    MeshPart(temp, cone, new Vector3(x, y0, z), new Vector3(lr * 2f, h * 0.42f, lr * 2f), lm);
                    MeshPart(temp, cone, new Vector3(x, y0 + h * 0.3f, z), new Vector3(lr * 0.9f, h * 0.13f, lr * 0.9f), snow);
                }
            }
            Random.state = state;
            return Combine(temp);
        }

        private static GameObject MeshPart(Transform parent, Mesh mesh, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = new GameObject("MeshPart");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        /// <summary>Unit cone (base radius 0.5 at y=0, apex at y=1), flat-shaded.</summary>
        private static Mesh ConeMesh(int sides)
        {
            var v = new List<Vector3>(); var tri = new List<int>();
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2f / sides, a1 = (i + 1) * Mathf.PI * 2f / sides;
                Vector3 p0 = new Vector3(Mathf.Cos(a0) * 0.5f, 0f, Mathf.Sin(a0) * 0.5f);
                Vector3 p1 = new Vector3(Mathf.Cos(a1) * 0.5f, 0f, Mathf.Sin(a1) * 0.5f);
                int b = v.Count;
                v.Add(p0); v.Add(Vector3.up); v.Add(p1);
                tri.Add(b); tri.Add(b + 1); tri.Add(b + 2);
                b = v.Count;
                v.Add(p0); v.Add(p1); v.Add(Vector3.zero);
                tri.Add(b); tri.Add(b + 1); tri.Add(b + 2);
            }
            var m = new Mesh { name = "PineCone" };
            m.SetVertices(v); m.SetTriangles(tri, 0);
            m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        // ═════════════════════════════════════ Old level / atmosphere ══

        private void HideOldLevel1Environment()
        {
            var oldRoot = GameObject.Find(NewRoadBuilder.ROOT_NAME);
            if (oldRoot != null)
            {
                foreach (Transform child in oldRoot.transform)
                {
                    if (!child.gameObject.activeSelf) continue;
                    if (child.GetComponentInChildren<MobControl.Combat.PlayerRunnerController>(true) != null) continue;
                    if (child.GetComponentInChildren<MobControl.Crowd.ZombieRoadWaveSpawner>(true) != null) continue;
                    if (child.GetComponentInChildren<Camera>(true) != null || child.GetComponentInChildren<Light>(true) != null) continue;
                    child.gameObject.SetActive(false);
                    hiddenObjects.Add(child.gameObject);
                }
            }
            var road = EndlessRoadManager.Instance;
            if (road != null && road.gameObject.activeSelf && road.GetComponentInChildren<MobControl.Combat.PlayerRunnerController>(true) == null)
            {
                road.gameObject.SetActive(false);
                hiddenObjects.Add(road.gameObject);
            }
        }

        private void ApplyAtmosphere(Camera cam)
        {
            prevFog = RenderSettings.fog; prevFogColor = RenderSettings.fogColor;
            prevFogStart = RenderSettings.fogStartDistance; prevFogEnd = RenderSettings.fogEndDistance;
            prevAmbMode = RenderSettings.ambientMode; prevAmbSky = RenderSettings.ambientSkyColor;
            prevAmbEq = RenderSettings.ambientEquatorColor; prevAmbGround = RenderSettings.ambientGroundColor;
            prevShadowDist = QualitySettings.shadowDistance;
            if (cam != null) { prevBg = cam.backgroundColor; prevClear = cam.clearFlags; }
            sun = FindSun();
            if (sun != null) { prevSunColor = sun.color; prevSunIntensity = sun.intensity; prevSunRot = sun.transform.rotation; }
            saved = true;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = C_FOG;
            RenderSettings.fogStartDistance = 90f;
            RenderSettings.fogEndDistance = 320f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.72f, 0.82f, 0.98f);
            RenderSettings.ambientEquatorColor = new Color(0.62f, 0.68f, 0.76f);
            RenderSettings.ambientGroundColor = new Color(0.34f, 0.40f, 0.48f);
            QualitySettings.shadowDistance = 45f;
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(52f, -32f, 0f);
                sun.color = new Color(1f, 0.95f, 0.86f);
                sun.intensity = 1.25f;
            }
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = C_SKY_HORIZON;
                cam.farClipPlane = Mathf.Max(cam.farClipPlane, 600f);
            }
        }

        private static Light FindSun()
        {
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude))
                if (l != null && l.type == LightType.Directional) return l;
            return null;
        }

        // ═════════════════════════════════════ Bridge meshes ══

        private static Dictionary<Material, Mesh> BuildSegmentMeshes()
        {
            Material deck = Mat(C_DECK, 0.1f), deck2 = Mat(C_DECK2, 0.1f), side = Mat(C_SIDE, 0.1f), line = Mat(C_LINE, 0.2f);
            Material barrier = Mat(C_BARRIER, 0.3f), stripe = Mat(C_STRIPE, 0.4f), pole = Mat(C_POLE, 0.5f);
            Material lamp = Flat(new Color(1f, 0.97f, 0.85f));

            var temp = new GameObject("SegTemp").transform;
            float len = SEG_LEN, mid = len * 0.5f;

            // ── Main bridge ──
            float W = ROAD_RIGHT - ROAD_LEFT;
            Part(temp, PrimitiveType.Cube, new Vector3(0f, -0.3f, mid), new Vector3(W, 0.6f, len), deck);
            Part(temp, PrimitiveType.Cube, new Vector3(0f, -1.3f, mid), new Vector3(W - 1.6f, 1.4f, len), side);
            Part(temp, PrimitiveType.Cube, new Vector3(0f, -2.1f, mid), new Vector3(W - 0.6f, 0.3f, 1.8f), side);
            Part(temp, PrimitiveType.Cylinder, new Vector3(-2.3f, -3.2f, mid), new Vector3(1.4f, 1.2f, 1.4f), side);
            Part(temp, PrimitiveType.Cylinder, new Vector3(2.3f, -3.2f, mid), new Vector3(1.4f, 1.2f, 1.4f), side);
            for (float z = 0f; z < len; z += 5f)   // subtle slab seams
                Part(temp, PrimitiveType.Cube, new Vector3(0f, 0.004f, z), new Vector3(W - 0.8f, 0.01f, 0.06f), deck2);
            Barrier(temp, ROAD_LEFT + 0.2f, len, barrier, stripe);
            Barrier(temp, ROAD_RIGHT - 0.2f, len, barrier, stripe);
            Part(temp, PrimitiveType.Cube, new Vector3(ROAD_LEFT + 0.6f, 0.012f, mid), new Vector3(0.14f, 0.02f, len), line);
            Part(temp, PrimitiveType.Cube, new Vector3(ROAD_RIGHT - 0.6f, 0.012f, mid), new Vector3(0.14f, 0.02f, len), line);
            for (float z = 1.5f; z < len; z += 5f)
            {
                Part(temp, PrimitiveType.Cube, new Vector3(-1.2f, 0.012f, z), new Vector3(0.15f, 0.02f, 2.6f), line);
                Part(temp, PrimitiveType.Cube, new Vector3(1.2f, 0.012f, z), new Vector3(0.15f, 0.02f, 2.6f), line);
            }
            Lamp(temp, ROAD_LEFT - 0.05f, 4f, +1, pole, lamp);
            Lamp(temp, ROAD_RIGHT + 0.05f, 14f, -1, pole, lamp);

            // ── Enemy bridge ──
            float ex = ENEMY_LANE_X, ew = ENEMY_HALF * 2f;
            Part(temp, PrimitiveType.Cube, new Vector3(ex, -0.3f, mid), new Vector3(ew, 0.6f, len), deck);
            Part(temp, PrimitiveType.Cube, new Vector3(ex, -1.3f, mid), new Vector3(ew - 1.2f, 1.4f, len), side);
            Part(temp, PrimitiveType.Cylinder, new Vector3(ex, -3.2f, mid), new Vector3(1.6f, 1.2f, 1.6f), side);
            Barrier(temp, ex - ENEMY_HALF + 0.2f, len, barrier, stripe);
            Barrier(temp, ex + ENEMY_HALF - 0.2f, len, barrier, stripe);
            for (float z = 1.5f; z < len; z += 5f)
                Part(temp, PrimitiveType.Cube, new Vector3(ex, 0.012f, z), new Vector3(0.15f, 0.02f, 2.6f), line);
            Lamp(temp, ex + ENEMY_HALF + 0.05f, 9f, -1, pole, lamp);

            return Combine(temp);
        }

        /// <summary>Suspension tower: two red legs, two cross beams, fan of white cables to the deck.</summary>
        private static Dictionary<Material, Mesh> BuildPylonMeshes()
        {
            Material pylon = Mat(C_PYLON, 0.35f), cable = Mat(C_CABLE, 0.6f);
            var temp = new GameObject("PylonTemp").transform;
            float zc = SEG_LEN * 0.5f, H = 20f, X = 4.2f;
            foreach (float sx in new[] { -1f, 1f })
                Part(temp, PrimitiveType.Cube, new Vector3(sx * X, H * 0.5f - 2.5f, zc), new Vector3(0.9f, H + 5f, 1.1f), pylon);
            Part(temp, PrimitiveType.Cube, new Vector3(0f, H, zc), new Vector3(X * 2f + 0.9f, 1.0f, 1.1f), pylon);
            Part(temp, PrimitiveType.Cube, new Vector3(0f, H * 0.55f, zc), new Vector3(X * 2f + 0.9f, 0.7f, 0.9f), pylon);
            foreach (float sx in new[] { -1f, 1f })
                for (int k = 1; k <= 5; k++)
                    foreach (float dir in new[] { -1f, 1f })
                    {
                        Vector3 top = new Vector3(sx * X, H - 0.8f - k * 0.5f, zc);
                        Vector3 bottom = new Vector3(sx * (X - 0.4f), 0.6f, zc + dir * k * 5.5f);
                        Vector3 c = (top + bottom) * 0.5f;
                        var go = Part(temp, PrimitiveType.Cube, c, new Vector3(0.07f, 0.07f, (top - bottom).magnitude), cable);
                        go.transform.rotation = Quaternion.LookRotation(top - bottom);
                    }
            return Combine(temp);
        }

        private static void Barrier(Transform t, float x, float len, Material body, Material stripe)
        {
            Part(t, PrimitiveType.Cube, new Vector3(x, 0.35f, len * 0.5f), new Vector3(0.4f, 0.7f, len), body);
            Part(t, PrimitiveType.Cube, new Vector3(x, 0.52f, len * 0.5f), new Vector3(0.42f, 0.1f, len), stripe);
        }

        private static void Lamp(Transform t, float x, float z, int towardRoad, Material pole, Material lamp)
        {
            Part(t, PrimitiveType.Cylinder, new Vector3(x, 2.6f, z), new Vector3(0.14f, 2.6f, 0.14f), pole);
            Part(t, PrimitiveType.Cube, new Vector3(x + towardRoad * 0.6f, 5.15f, z), new Vector3(1.3f, 0.1f, 0.12f), pole);
            Part(t, PrimitiveType.Cube, new Vector3(x + towardRoad * 1.15f, 5.05f, z), new Vector3(0.45f, 0.12f, 0.22f), lamp);
        }

        private static GameObject Part(Transform parent, PrimitiveType type, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        private static Dictionary<Material, Mesh> Combine(Transform temp)
        {
            var groups = new Dictionary<Material, List<CombineInstance>>();
            foreach (var mf in temp.GetComponentsInChildren<MeshFilter>())
            {
                var mat = mf.GetComponent<MeshRenderer>().sharedMaterial;
                if (!groups.TryGetValue(mat, out var list)) groups[mat] = list = new List<CombineInstance>();
                Mesh src = mf.sharedMesh;
                if (mat.shader != null && mat.shader.name == "Sprites/Default") src = WithWhiteColors(src);
                list.Add(new CombineInstance { mesh = src, transform = temp.worldToLocalMatrix * mf.transform.localToWorldMatrix });
            }
            var result = new Dictionary<Material, Mesh>();
            foreach (var kv in groups)
            {
                var m = new Mesh { name = "Env_" + kv.Key.name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                m.CombineMeshes(kv.Value.ToArray(), true, true);
                m.RecalculateBounds();
                result[kv.Key] = m;
            }
            Object.DestroyImmediate(temp.gameObject);
            return result;
        }

        private static readonly Dictionary<Mesh, Mesh> s_white = new Dictionary<Mesh, Mesh>();
        private static Mesh WithWhiteColors(Mesh src)
        {
            if (s_white.TryGetValue(src, out var w) && w != null) return w;
            w = Object.Instantiate(src);
            var c = new Color32[w.vertexCount];
            for (int i = 0; i < c.Length; i++) c[i] = new Color32(255, 255, 255, 255);
            w.colors32 = c;
            s_white[src] = w;
            return w;
        }

        /// <summary>
        /// Merges every MeshRenderer under t into ONE mesh per material (children of t), so a palm island
        /// with 80 pieces becomes ~4 draw calls. t keeps its own transform, so it can still move / spin.
        /// </summary>
        public static void CombineChildren(Transform t)
        {
            var groups = new Dictionary<Material, List<CombineInstance>>();
            var old = new List<GameObject>();
            foreach (var mf in t.GetComponentsInChildren<MeshFilter>(true))
            {
                var mr = mf.GetComponent<MeshRenderer>();
                if (mr == null || mf.sharedMesh == null || mr.sharedMaterial == null || !mf.sharedMesh.isReadable) continue; // imported models stay separate
                if (!groups.TryGetValue(mr.sharedMaterial, out var list)) groups[mr.sharedMaterial] = list = new List<CombineInstance>();
                list.Add(new CombineInstance { mesh = mf.sharedMesh, transform = t.worldToLocalMatrix * mf.transform.localToWorldMatrix });
                old.Add(mf.gameObject);
            }
            foreach (var go in old) if (go != t.gameObject) { go.transform.SetParent(null, false); Object.DestroyImmediate(go); }
            foreach (var kv in groups)
            {
                var m = new Mesh { name = "Merged_" + kv.Key.name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                m.CombineMeshes(kv.Value.ToArray(), true, true);
                m.RecalculateBounds();
                var part = new GameObject("Merged_" + kv.Key.name);
                part.transform.SetParent(t, false);
                part.AddComponent<MeshFilter>().sharedMesh = m;
                var r = part.AddComponent<MeshRenderer>();
                r.sharedMaterial = kv.Key;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        private static void AddMeshes(Transform parent, Dictionary<Material, Mesh> meshes)
        {
            foreach (var kv in meshes)
            {
                var part = new GameObject(kv.Key.name);
                part.transform.SetParent(parent, false);
                part.AddComponent<MeshFilter>().sharedMesh = kv.Value;
                var mr = part.AddComponent<MeshRenderer>();
                mr.sharedMaterial = kv.Key;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        // ═════════════════════════════════════ Scenery ══

        private Layer NewLayer(float factor, float minZ, float wrapLen)
        {
            var L = new Layer { factor = factor, minZ = minZ, wrapLen = wrapLen };
            layers.Add(L);
            return L;
        }

        /// <summary>Gradient sky sphere (horizon light → zenith blue). Follows the camera.</summary>
        private void BuildSky()
        {
            const int lon = 24, lat = 12;
            var v = new List<Vector3>(); var c = new List<Color32>(); var tri = new List<int>();
            for (int y = 0; y <= lat; y++)
            {
                float phi = Mathf.PI * y / lat;               // 0 = top
                float h = Mathf.Cos(phi);                     // 1 top, -1 bottom
                Color col = h > 0f ? Color.Lerp(C_SKY_HORIZON, C_SKY_TOP, Mathf.Pow(h, 0.6f)) : C_SKY_HORIZON;
                for (int x = 0; x <= lon; x++)
                {
                    float th = 2f * Mathf.PI * x / lon;
                    v.Add(new Vector3(Mathf.Sin(phi) * Mathf.Cos(th), h, Mathf.Sin(phi) * Mathf.Sin(th)) * 500f);
                    c.Add(col);
                }
            }
            for (int y = 0; y < lat; y++)
                for (int x = 0; x < lon; x++)
                {
                    int a = y * (lon + 1) + x, b = a + lon + 1;
                    tri.Add(a); tri.Add(b); tri.Add(a + 1);
                    tri.Add(a + 1); tri.Add(b); tri.Add(b + 1);
                }
            var m = new Mesh { name = "SkyDome" };
            m.SetVertices(v); m.SetColors(c); m.SetTriangles(tri, 0); m.RecalculateBounds();
            var go = new GameObject("SkyDome");
            go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var mat = new Material(Shader.Find("Sprites/Default")) { name = "Sky", renderQueue = 1000 };
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            skyDome = go.transform;
        }

        private void BuildWater()
        {
            var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "Sea";
            Object.Destroy(water.GetComponent<Collider>());
            water.transform.SetParent(root, false);
            water.transform.localPosition = new Vector3(0f, WATER_Y, 250f);
            water.transform.localScale = new Vector3(80f, 1f, 80f);   // 800 m x 800 m
            waterMat = Mat(Color.white, 0.94f);
            waterMat.name = "Sea";
            waterMat.mainTexture = BuildWaveTexture();
            waterMat.mainTextureScale = new Vector2(800f / WATER_TILE_M, 800f / WATER_TILE_M);
            var mr = water.GetComponent<MeshRenderer>();
            mr.sharedMaterial = waterMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private static Texture2D BuildWaveTexture()
        {
            const int N = 128;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, true) { name = "SeaWaves", wrapMode = TextureWrapMode.Repeat };
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = x / (float)N, v = y / (float)N;
                    float w = Mathf.Sin((v * 3f + Mathf.Sin(u * Mathf.PI * 4f) * 0.09f) * Mathf.PI * 2f);
                    float w2 = Mathf.Sin((u * 2f + v) * Mathf.PI * 2f) * 0.5f;
                    float n = Mathf.PerlinNoise(u * 6f, v * 6f) * 0.5f;
                    float crest = Mathf.Clamp01((w + w2 * 0.4f + n - 1.0f) * 2.6f);
                    Color c = Color.Lerp(C_WATER, new Color(0.62f, 0.86f, 1f), crest * 0.7f);
                    c = Color.Lerp(c, new Color(0.06f, 0.36f, 0.75f), (1f - n) * 0.25f);
                    px[y * N + x] = c;
                }
            tex.SetPixels32(px);
            tex.Apply(true);
            return tex;
        }

        /// <summary>Red/white buoys bobbing next to the bridges (1× scroll).</summary>
        private void BuildBuoys()
        {
            var L = NewLayer(1f, -30f, 300f);
            Material red = Mat(new Color(0.92f, 0.2f, 0.18f), 0.5f), white = Mat(Color.white, 0.5f);
            for (int i = 0; i < 10; i++)
            {
                var b = new GameObject("Buoy").transform;
                b.SetParent(root, false);
                float x = (i % 2 == 0) ? Random.Range(5.6f, 7.5f) : Random.Range(-6.0f, -5.0f);
                b.localPosition = new Vector3(x, WATER_Y, i * 30f + Random.Range(0f, 10f));
                b.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                Part(b, PrimitiveType.Cylinder, new Vector3(0f, 0.25f, 0f), new Vector3(0.9f, 0.35f, 0.9f), red);
                Part(b, PrimitiveType.Cylinder, new Vector3(0f, 0.9f, 0f), new Vector3(0.45f, 0.35f, 0.45f), white);
                Part(b, PrimitiveType.Sphere, new Vector3(0f, 1.35f, 0f), new Vector3(0.35f, 0.35f, 0.35f), red);
                CombineChildren(b);
                L.items.Add(b);
                bobbers.Add(b);
            }
        }

        /// <summary>Cargo ships and sailboats in the distance (0.8× scroll).</summary>
        private void BuildShips()
        {
            var L = NewLayer(0.8f, -60f, 520f);
            Color[] cont = { new Color(0.85f, 0.25f, 0.2f), new Color(0.2f, 0.5f, 0.85f), new Color(0.95f, 0.7f, 0.2f), new Color(0.3f, 0.7f, 0.4f) };
            for (int i = 0; i < 5; i++)
            {
                var s = new GameObject(i % 2 == 0 ? "CargoShip" : "Sailboat").transform;
                s.SetParent(root, false);
                bool right = i % 2 == 0;
                s.localPosition = new Vector3(right ? Random.Range(18f, 34f) : Random.Range(-38f, -24f), WATER_Y, 40f + i * 100f);
                s.localRotation = Quaternion.Euler(0f, Random.Range(-25f, 25f), 0f);
                if (i % 2 == 0)
                {
                    Part(s, PrimitiveType.Cube, new Vector3(0f, 0.6f, 0f), new Vector3(4f, 1.8f, 16f), Mat(new Color(0.18f, 0.22f, 0.32f), 0.4f));
                    Part(s, PrimitiveType.Cube, new Vector3(0f, 1.55f, 0f), new Vector3(4.1f, 0.15f, 16.1f), Mat(new Color(0.8f, 0.2f, 0.18f), 0.4f));
                    for (int k = 0; k < 6; k++)
                        Part(s, PrimitiveType.Cube, new Vector3((k % 2 == 0) ? -0.9f : 0.9f, 2.3f + (k / 4) * 1.1f, -4f + (k / 2) * 2.6f),
                            new Vector3(1.7f, 1.1f, 2.4f), Mat(cont[(k + i) % cont.Length], 0.3f));
                    Part(s, PrimitiveType.Cube, new Vector3(0f, 3.2f, 6f), new Vector3(3.2f, 3f, 2.5f), Mat(Color.white, 0.4f));
                }
                else
                {
                    Part(s, PrimitiveType.Cube, new Vector3(0f, 0.3f, 0f), new Vector3(1.4f, 0.7f, 4.5f), Mat(Color.white, 0.5f));
                    Part(s, PrimitiveType.Cylinder, new Vector3(0f, 3f, 0.3f), new Vector3(0.12f, 2.6f, 0.12f), Mat(new Color(0.4f, 0.3f, 0.2f), 0.3f));
                    var sail = Part(s, PrimitiveType.Cube, new Vector3(0f, 3.2f, -0.6f), new Vector3(0.05f, 4.2f, 2.2f), Mat(new Color(1f, 0.97f, 0.9f), 0.2f));
                    sail.transform.localRotation = Quaternion.Euler(0f, 0f, 3f);
                }
                CombineChildren(s);
                L.items.Add(s);
                bobbers.Add(s);
            }
        }

        /// <summary>Tropical islands with rocks, palm trees and the odd lighthouse (0.3× parallax).</summary>
        private void BuildIslands()
        {
            var L = NewLayer(0.3f, -120f, 760f);
            Material sand = Mat(new Color(0.95f, 0.86f, 0.62f), 0.1f), grass = Mat(new Color(0.36f, 0.72f, 0.32f), 0.1f);
            Material trunk = Mat(new Color(0.55f, 0.38f, 0.22f), 0.1f), leaf = Mat(new Color(0.22f, 0.62f, 0.25f), 0.2f);
            Material rockMat = Resources.Load<Material>("Materials/M_StylizedCliffRock") ?? Mat(new Color(0.55f, 0.55f, 0.58f), 0.15f);
            Mesh rockMesh = LoadMesh("Nature/Stylized_Cliff_Rock") ?? LoadMesh("Nature/CliffRock_V2");

            for (int i = 0; i < 10; i++)
            {
                var isl = new GameObject("Island").transform;
                isl.SetParent(root, false);
                bool right = i % 2 == 0;
                float r = Random.Range(10f, 22f);
                isl.localPosition = new Vector3((right ? 1f : -1f) * Random.Range(45f, 110f), WATER_Y, -60f + i * 76f + Random.Range(-15f, 15f));
                isl.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                Part(isl, PrimitiveType.Sphere, new Vector3(0f, -0.4f, 0f), new Vector3(r * 2f, 2.2f, r * 1.6f), sand);
                Part(isl, PrimitiveType.Sphere, new Vector3(0f, 0.6f, 0f), new Vector3(r * 1.5f, 3.5f + r * 0.2f, r * 1.15f), grass);

                int rocks = Random.Range(1, 4);
                for (int k = 0; k < rocks; k++)
                {
                    Vector3 p = new Vector3(Random.Range(-r * 0.6f, r * 0.6f), 0.5f, Random.Range(-r * 0.5f, r * 0.5f));
                    float s = Random.Range(4f, 9f);
                    if (rockMesh != null)
                    {
                        var go = new GameObject("Rock");
                        go.transform.SetParent(isl, false);
                        go.transform.localPosition = p;
                        go.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                        float ms = s / Mathf.Max(0.01f, rockMesh.bounds.size.y);
                        go.transform.localScale = Vector3.one * ms;
                        go.AddComponent<MeshFilter>().sharedMesh = rockMesh;
                        var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = rockMat;
                        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    }
                    else Part(isl, PrimitiveType.Sphere, p, new Vector3(s, s * 1.2f, s), rockMat);
                }

                int palms = Random.Range(3, 7);
                for (int k = 0; k < palms; k++)
                    Palm(isl, new Vector3(Random.Range(-r * 0.55f, r * 0.55f), 1.5f, Random.Range(-r * 0.45f, r * 0.45f)), Random.Range(5f, 8f), trunk, leaf);

                if (i % 4 == 1) Lighthouse(isl, new Vector3(r * 0.4f, 1.5f, 0f));
                CombineChildren(isl);
                L.items.Add(isl);
            }
        }

        private static void Palm(Transform parent, Vector3 basePos, float h, Material trunk, Material leaf)
        {
            var palm = new GameObject("Palm").transform;
            palm.SetParent(parent, false);
            palm.localPosition = basePos;
            palm.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            float lean = Random.Range(6f, 16f);
            Vector3 p = Vector3.zero;
            int segs = 5;
            for (int i = 0; i < segs; i++)
            {
                float segH = h / segs;
                var go = Part(palm, PrimitiveType.Cylinder, p + Vector3.up * segH * 0.5f, new Vector3(0.45f - i * 0.05f, segH * 0.5f, 0.45f - i * 0.05f), trunk);
                go.transform.localRotation = Quaternion.Euler(0f, 0f, lean * i / segs);
                p += Quaternion.Euler(0f, 0f, lean * i / segs) * Vector3.up * segH;
            }
            for (int k = 0; k < 7; k++)
            {
                var l = Part(palm, PrimitiveType.Cube, p, new Vector3(0.9f, 0.08f, 3.2f), leaf);
                l.transform.localRotation = Quaternion.Euler(28f, k * (360f / 7f), 0f);
                l.transform.localPosition = p + l.transform.localRotation * new Vector3(0f, 0f, 1.4f);
            }
        }

        private static void Lighthouse(Transform parent, Vector3 pos)
        {
            Material w = Mat(Color.white, 0.4f), r = Mat(new Color(0.9f, 0.2f, 0.18f), 0.4f);
            for (int i = 0; i < 5; i++)
                Part(parent, PrimitiveType.Cylinder, pos + Vector3.up * (1.5f + i * 3f), new Vector3(3f - i * 0.25f, 1.5f, 3f - i * 0.25f), i % 2 == 0 ? w : r);
            Part(parent, PrimitiveType.Cylinder, pos + Vector3.up * 16.2f, new Vector3(2.2f, 0.8f, 2.2f), Mat(new Color(1f, 0.95f, 0.6f), 0.9f));
            Part(parent, PrimitiveType.Sphere, pos + Vector3.up * 17.4f, new Vector3(2.4f, 1.6f, 2.4f), r);
        }

        /// <summary>Soft cloud billboards high in the sky (0.05× parallax).</summary>
        private void BuildClouds()
        {
            var L = NewLayer(0.05f, -50f, 700f);
            var tex = CloudTexture();
            var mat = new Material(Shader.Find("Sprites/Default")) { name = "Clouds", mainTexture = tex, renderQueue = 1500 };
            Mesh quad = WithWhiteColors(QuadMesh());
            for (int i = 0; i < 14; i++)
            {
                var go = new GameObject("Cloud");
                go.transform.SetParent(root, false);
                float w = Random.Range(60f, 130f);
                go.transform.localPosition = new Vector3(Random.Range(-260f, 260f), Random.Range(70f, 140f), Random.Range(150f, 650f));
                go.transform.localRotation = Quaternion.Euler(-10f, 0f, 0f);
                go.transform.localScale = new Vector3(w, w * 0.42f, 1f);
                go.AddComponent<MeshFilter>().sharedMesh = quad;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                L.items.Add(go.transform);
            }
        }

        private static Texture2D CloudTexture()
        {
            const int W = 128, H = 64;
            var t = new Texture2D(W, H, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, name = "Cloud" };
            var px = new Color32[W * H];
            Vector3[] blobs = { new Vector3(0.3f, 0.45f, 0.22f), new Vector3(0.5f, 0.55f, 0.28f), new Vector3(0.7f, 0.45f, 0.22f), new Vector3(0.42f, 0.38f, 0.2f), new Vector3(0.6f, 0.36f, 0.2f) };
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float u = x / (float)W, v = y / (float)H, a = 0f;
                    foreach (var b in blobs)
                    {
                        float dx = (u - b.x) / b.z, dy = (v - b.y) / (b.z * 1.6f);
                        a = Mathf.Max(a, Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy)));
                    }
                    a = Mathf.SmoothStep(0f, 1f, a * 1.4f) * 0.9f;
                    byte shade = (byte)(235 + 20 * v);
                    px[y * W + x] = new Color32(shade, shade, 255, (byte)(a * 255));
                }
            t.SetPixels32(px);
            t.Apply(true);
            return t;
        }

        private static Mesh QuadMesh()
        {
            var m = new Mesh { name = "Quad" };
            m.vertices = new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(0.5f, 0.5f, 0), new Vector3(-0.5f, 0.5f, 0) };
            m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        private static Mesh LoadMesh(string resPath)
        {
            var go = Resources.Load<GameObject>(resPath);
            var mf = go != null ? go.GetComponentInChildren<MeshFilter>() : null;
            return mf != null ? mf.sharedMesh : null;
        }

        // ═════════════════════════════════════ Materials ══

        public static Material LitMat(string name, Color c, float gloss)
        {
            var m = Mat(c, gloss);
            m.name = name;
            return m;
        }

        private static Material Mat(Color c, float gloss)
        {
            int key = Key(c) ^ (int)(gloss * 1000) * 7919;
            if (s_mats.TryGetValue(key, out var cached) && cached != null) return cached;
            Shader sh = Shader.Find("Standard") ?? Shader.Find("Mobile/Diffuse");
            var m = new Material(sh) { color = c, name = "Env_" + ColorUtility.ToHtmlStringRGB(c) };
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", gloss);
            s_mats[key] = m;
            return m;
        }

        private static Material Flat(Color c)
        {
            int key = Key(c) ^ 0x5f5f5f;
            if (s_mats.TryGetValue(key, out var cached) && cached != null) return cached;
            var m = new Material(Shader.Find("Sprites/Default")) { color = c, name = "EnvFlat" };
            s_mats[key] = m;
            return m;
        }

        private static int Key(Color c) => ((int)(c.r * 255) << 24) ^ ((int)(c.g * 255) << 16) ^ ((int)(c.b * 255) << 8) ^ (int)(c.a * 255);
    }
}
