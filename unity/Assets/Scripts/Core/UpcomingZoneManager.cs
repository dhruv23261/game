using System.Collections.Generic;
using UnityEngine;
using MobControl.Crowd;
using MobControl.Combat;

namespace MobControl.Core
{
    /// <summary>
    /// Progressively streams and activates upcoming track content (enemies, gates, roads)
    /// based on the player's runner position along the Z-axis.
    /// Keeps active polygon counts and draw calls strictly within mobile 60 FPS budgets.
    /// </summary>
    public class UpcomingZoneManager : MonoBehaviour
    {
        public static UpcomingZoneManager Instance { get; private set; }

        [Header("Upcoming Zone Streaming")]
        [Tooltip("How far ahead of the player upcoming enemies and gates activate (meters)")]
        [SerializeField] private float upcomingAheadDistance = 140.0f;

        [Tooltip("How far behind the player passed objects get deactivated permanently (meters)")]
        [SerializeField] private float behindDeactivateDistance = 12.0f;

        [Tooltip("Interval between zone update sweeps in seconds")]
        [SerializeField] private float updateInterval = 0.08f; // ~12 times a second

        [Header("Atmosphere / Horizon Fog")]
        [SerializeField] private bool enableAtmosphericFog = false;
        [SerializeField] private float fogStartDistance = 150.0f;
        [SerializeField] private float fogEndDistance = 250.0f;
        [SerializeField] private Color fogColor = new Color(0.48f, 0.68f, 0.88f, 1f); // Pleasant sky blue

        private class TrackItem
        {
            public GameObject gameObject;
            public float initialZ;
            public bool isPassed;
            public bool isRoad;
        }

        private readonly List<TrackItem> trackedItems = new List<TrackItem>();
        private float lastSweepTime = 0f;
        private Transform playerTransform;

        /// <summary>
        /// Ensures an instance exists in the scene at runtime without requiring manual inspector setup.
        /// </summary>
        public static void EnsureInstance()
        {
            if (Instance == null)
            {
                UpcomingZoneManager existing = FindAnyObjectByType<UpcomingZoneManager>();
                if (existing == null)
                {
                    GameObject go = new GameObject("UpcomingZoneManager");
                    Instance = go.AddComponent<UpcomingZoneManager>();
                }
                else
                {
                    Instance = existing;
                }
            }
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.farClipPlane = 300.0f;
                // Use skybox or pleasant sky blue so horizon never flickers to solid white
                if (RenderSettings.skybox != null)
                {
                    mainCam.clearFlags = CameraClearFlags.Skybox;
                }
                else
                {
                    mainCam.clearFlags = CameraClearFlags.SolidColor;
                    mainCam.backgroundColor = fogColor;
                }
                mainCam.allowHDR = false;
                mainCam.allowMSAA = false;
            }

            // Keep fog disabled or smooth to prevent distance flickering
            RenderSettings.fog = enableAtmosphericFog;
            if (enableAtmosphericFog)
            {
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogStartDistance = fogStartDistance;
                RenderSettings.fogEndDistance = fogEndDistance;
                RenderSettings.fogColor = fogColor;
            }

            // Find player
            if (PlayerRunnerController.Instance != null)
            {
                playerTransform = PlayerRunnerController.Instance.transform;
            }
            else
            {
                GameObject playerMob = GameObject.Find("PlayerMob");
                if (playerMob != null) playerTransform = playerMob.transform;
            }

            float playerZ = playerTransform != null ? playerTransform.position.z : 0f;

            // 1. Register all Enemy MobUnits (Zombies)
            MobUnit[] mobs = FindObjectsByType<MobUnit>(FindObjectsInactive.Include);
            foreach (var mob in mobs)
            {
                if (mob.Team == MobTeam.Player || mob.GetComponent<PlayerRunnerController>() != null)
                    continue;

                float z = mob.transform.position.z;
                TrackItem item = new TrackItem
                {
                    gameObject = mob.gameObject,
                    initialZ = z,
                    isPassed = false,
                    isRoad = false
                };
                trackedItems.Add(item);

                // Initial state: only active if within upcoming distance
                bool shouldBeActive = (z >= playerZ - behindDeactivateDistance && z <= playerZ + upcomingAheadDistance);
                mob.gameObject.SetActive(shouldBeActive);
            }

            // 2. Register all Destructible Gates
            DestructibleGate[] gates = FindObjectsByType<DestructibleGate>(FindObjectsInactive.Include);
            foreach (var gate in gates)
            {
                float z = gate.transform.position.z;
                TrackItem item = new TrackItem
                {
                    gameObject = gate.gameObject,
                    initialZ = z,
                    isPassed = false,
                    isRoad = false
                };
                trackedItems.Add(item);

                bool shouldBeActive = (z >= playerZ - behindDeactivateDistance && z <= playerZ + upcomingAheadDistance);
                gate.gameObject.SetActive(shouldBeActive);
            }

            Debug.Log($"[UpcomingZoneManager] Initialized with {trackedItems.Count} tracked elements. Upcoming window: {upcomingAheadDistance}m.");
        }

        private void Update()
        {
            if (Time.time < lastSweepTime + updateInterval) return;
            lastSweepTime = Time.time;

            if (playerTransform == null)
            {
                if (PlayerRunnerController.Instance != null)
                    playerTransform = PlayerRunnerController.Instance.transform;
                else
                    return;
            }

            float playerZ = playerTransform.position.z;
            float maxUpcomingZ = playerZ + upcomingAheadDistance;
            float minBehindZ = playerZ - behindDeactivateDistance;

            for (int i = 0; i < trackedItems.Count; i++)
            {
                TrackItem item = trackedItems[i];
                if (item.gameObject == null) continue;

                // Once permanently passed behind player, keep deactivated
                if (item.isPassed)
                {
                    if (item.gameObject.activeSelf) item.gameObject.SetActive(false);
                    continue;
                }

                // Check if living enemy or gate has fallen behind player
                float currentZ = item.gameObject.transform.position.z;
                if (currentZ < minBehindZ)
                {
                    item.isPassed = true;
                    item.gameObject.SetActive(false);
                    continue;
                }

                // Activate if inside upcoming window ahead of player
                if (item.initialZ <= maxUpcomingZ)
                {
                    if (!item.gameObject.activeSelf)
                    {
                        item.gameObject.SetActive(true);
                    }
                }
                else
                {
                    // Far ahead in the track: keep completely inactive
                    if (item.gameObject.activeSelf)
                    {
                        item.gameObject.SetActive(false);
                    }
                }
            }
        }
    }
}
