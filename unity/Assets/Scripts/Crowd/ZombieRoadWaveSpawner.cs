using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MobControl.Core;
using MobControl.UI;
using MobControl.Combat;

namespace MobControl.Crowd
{
    /// <summary>
    /// Structured 3-Phase Zombie Wave System:
    ///
    ///  PHASE 1 (0–10s)  → Scout Rush:    Only small fast Red zombies (2-3 at a time)
    ///  PHASE 2 (10–22s) → Mixed Assault: Small scouts + Moderate green zombies alternating
    ///  PHASE 3 (22–33s) → Heavy Push:    Large moderate packs (3-4), occasional scouts mixed in
    ///  BOSS   (33s+)    → Boss Monster emerges. Progress bar switches to Boss HP.
    /// </summary>
    public class ZombieRoadWaveSpawner : MonoBehaviour
    {
        public static ZombieRoadWaveSpawner Instance { get; private set; }

        [Header("Zombie Prefabs (Meshy AI 3D Assets)")]
        [SerializeField] private GameObject smallZombiePrefab;
        [SerializeField] private GameObject moderateZombiePrefab;
        [SerializeField] private GameObject bossZombiePrefab;
        [Tooltip("Level 2 Meshy plant zombie (Resources/Prefabs/Zombie_Plant). Falls back to Zombie_Moderate if missing.")]
        [SerializeField] private GameObject plantZombiePrefab;

        [Header("Road Spawning Settings")]
        [SerializeField] private float spawnAheadDistanceZ = 42.0f;
        [SerializeField] private float laneMinX = -2.0f;
        [SerializeField] private float laneMaxX = 2.0f;

        [Header("Phase Durations (seconds)")]
        [SerializeField] private float phase1Duration = 10.0f;  // Scout Rush
        [SerializeField] private float phase2Duration = 12.0f;  // Mixed Assault
        [SerializeField] private float phase3Duration = 11.0f;  // Heavy Push

        [Header("Stats Tuning")]
        [Tooltip("Small Red Zombie: high speed, 8 bullets to kill")]
        [SerializeField] private float smallZombieSpeed = 5.2f;
        [SerializeField] private int smallZombieHealth = 8;

        [Header("Level")]
        [SerializeField] private int currentLevel = 1;
        public int CurrentLevel
        {
            get => currentLevel;
            set => currentLevel = value;
        }

        [Tooltip("Moderate Green Zombie: normal walk speed, 5 bullets to kill")]
        [SerializeField] private float moderateZombieSpeed = 3.4f;
        [SerializeField] private int moderateZombieHealth = 5;

        [Tooltip("Boss Monster: slow stomp, 60 bullets to kill")]
        [SerializeField] private float bossZombieSpeed = 1.4f;
        [SerializeField] private int bossZombieHealth = 60;

        private bool isSpawningActive = false;
        private Coroutine waveCoroutine;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            LoadPrefabsIfMissing();
        }

        private void Start()
        {
            RegisterZombiePools();
            ClearStrayZombies();

            if (ZombieRunnerUIManager.Instance != null &&
                ZombieRunnerUIManager.Instance.CurrentState == ZombieRunnerUIManager.ScreenState.Gameplay &&
                !ZombieRunnerUIManager.Instance.IsWaitingForFirstSwipe)
            {
                StartSpawning();
            }
        }

        // ────────────────────────────────────────────
        //  Public API
        // ────────────────────────────────────────────

        public void LoadPrefabsIfMissing()
        {
            if (smallZombiePrefab == null)
            {
                smallZombiePrefab = Resources.Load<GameObject>("Prefabs/Zombie_Small");
#if UNITY_EDITOR
                if (smallZombiePrefab == null)
                    smallZombiePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Zombie_Small.prefab");
#endif
            }
            if (moderateZombiePrefab == null)
            {
                moderateZombiePrefab = Resources.Load<GameObject>("Prefabs/Zombie_Moderate");
#if UNITY_EDITOR
                if (moderateZombiePrefab == null)
                    moderateZombiePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Zombie_Moderate.prefab");
#endif
            }
            if (bossZombiePrefab == null)
            {
                bossZombiePrefab = Resources.Load<GameObject>("Prefabs/Zombie_Boss");
#if UNITY_EDITOR
                if (bossZombiePrefab == null)
                    bossZombiePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Zombie_Boss.prefab");
#endif
            }
            if (plantZombiePrefab == null)
                plantZombiePrefab = Resources.Load<GameObject>("Prefabs/" + PLANT_ZOMBIE_TAG);
        }

        public void RegisterZombiePools()
        {
            LoadPrefabsIfMissing();
            if (ObjectPooler.Instance != null)
            {
                if (smallZombiePrefab    != null) ObjectPooler.Instance.RegisterPool("Zombie_Small",    smallZombiePrefab,    24);
                if (moderateZombiePrefab != null) ObjectPooler.Instance.RegisterPool("Zombie_Moderate", moderateZombiePrefab, 20);
                if (bossZombiePrefab     != null) ObjectPooler.Instance.RegisterPool("Zombie_Boss",     bossZombiePrefab,      6);
                if (plantZombiePrefab    != null) ObjectPooler.Instance.RegisterPool(PLANT_ZOMBIE_TAG,  plantZombiePrefab,    16);
            }
        }

        public void PrepareForLevelStart()
        {
            StopSpawning();
            ClearStrayZombies();
        }

        public void ResetAndStartSpawning()
        {
            PrepareForLevelStart();
            if (ZombieRunnerUIManager.Instance != null && ZombieRunnerUIManager.Instance.IsWaitingForFirstSwipe)
                return;
            StartSpawning();
        }

        public void ClearStrayZombies()
        {
            MobUnit[] mobs = Object.FindObjectsByType<MobUnit>(FindObjectsInactive.Include);
            foreach (var mob in mobs)
            {
                if (mob != null && mob.Team == MobTeam.Enemy)
                {
                    if (ObjectPooler.Instance != null && ObjectPooler.Instance.HasPool(mob.PoolTag))
                        ObjectPooler.Instance.ReturnToPool(mob.PoolTag, mob.gameObject);
                    else
                        Destroy(mob.gameObject);
                }
            }

            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            {
                if (go != null && (go.name.StartsWith("Preview_Zombie_") ||
                                   go.name.StartsWith("Zombie_Small")    ||
                                   go.name.StartsWith("Zombie_Moderate") ||
                                   go.name.StartsWith("Zombie_Boss")     ||
                                   go.name.StartsWith("Zombie_Plant")     ||
                                   go.name.StartsWith("Level2_FlyingWyvernDragon")))
                    Destroy(go);
            }
        }

        public void StartSpawning()
        {
            if (waveCoroutine != null) StopCoroutine(waveCoroutine);
            isSpawningActive = true;
            waveCoroutine = StartCoroutine(StructuredWaveRoutine());
        }

        public void StopSpawning()
        {
            isSpawningActive = false;
            if (waveCoroutine != null)
            {
                StopCoroutine(waveCoroutine);
                waveCoroutine = null;
            }
        }

        // ════════════════════════════════════════════════
        //  MAIN WAVE ROUTINE  (3 phases → Boss)
        // ════════════════════════════════════════════════

        private IEnumerator StructuredWaveRoutine()
        {
            float totalDuration = phase1Duration + phase2Duration + phase3Duration;

            if (currentLevel == 2)
            {
                yield return Level2WaveRoutine(totalDuration);
                yield break;
            }

            // 1-second breathing space before first wave
            yield return new WaitForSeconds(1.0f);

            // Spawn 3 initial scouts right ahead so player immediately sees enemies
            SpawnSingleZombie("Zombie_Small", smallZombieSpeed, smallZombieHealth,  0.0f, 18.0f);
            SpawnSingleZombie("Zombie_Small", smallZombieSpeed, smallZombieHealth, -1.2f, 22.0f);
            SpawnSingleZombie("Zombie_Small", smallZombieSpeed, smallZombieHealth,  1.2f, 26.0f);

            // ── PHASE 1: Scout Rush (only small red zombies) ──────────────────────
            yield return RunPhase(
                phaseName:        "Phase1_ScoutRush",
                phaseDuration:    phase1Duration,
                totalDuration:    totalDuration,
                phaseStartOffset: 0f,
                spawnInterval:    new Vector2(2.2f, 3.2f),
                spawnAction:      SpawnPhase1Wave
            );

            // ── PHASE 2: Mixed Assault (scouts + moderate alternating) ────────────
            yield return RunPhase(
                phaseName:        "Phase2_MixedAssault",
                phaseDuration:    phase2Duration,
                totalDuration:    totalDuration,
                phaseStartOffset: phase1Duration,
                spawnInterval:    new Vector2(1.8f, 2.6f),
                spawnAction:      SpawnPhase2Wave
            );

            // ── PHASE 3: Heavy Push (large moderate packs) ───────────────────────
            yield return RunPhase(
                phaseName:        "Phase3_HeavyPush",
                phaseDuration:    phase3Duration,
                totalDuration:    totalDuration,
                phaseStartOffset: phase1Duration + phase2Duration,
                spawnInterval:    new Vector2(1.4f, 2.0f),
                spawnAction:      SpawnPhase3Wave
            );

            // ── ALL PHASES DONE: Release the Boss Monster ─────────────────────────
            Debug.Log("[ZombieRoadWaveSpawner] All 3 phases complete! Releasing Boss Monster!");

            if (ZombieRunnerUIManager.Instance != null)
            {
                ZombieRunnerUIManager.Instance.UpdateWaveProgress(1.0f);
                ZombieRunnerUIManager.Instance.TriggerBossAlert();
            }

            yield return new WaitForSeconds(1.2f);

            // Boss spawns 75m ahead, slow stomp down the road
            GameObject bossObj = SpawnSingleZombie("Zombie_Boss", bossZombieSpeed, bossZombieHealth, 0f, 75.0f);
            if (bossObj != null)
            {
                MobUnit bossUnit = bossObj.GetComponent<MobUnit>();
                if (bossUnit != null && ZombieRunnerUIManager.Instance != null)
                    ZombieRunnerUIManager.Instance.OnBossSpawned(bossUnit, bossZombieHealth);
            }
        }

        // ════════════════════════════════════════════════
        //  LEVEL 2 — "Overgrown Highway"
        //  NOT harder than level 1 — just NEW things to see:
        //   • Plant Zombies (Meshy vine walkers) are the main enemy — slow, few hits
        //   • zombies arrive in shapes (side-by-side rows, flank pairs, single-file lines)
        //   • a Wyvern dragon flies over in phase 2, and a pair of them in phase 3 (shoot for bonus coins)
        //  Spawn gaps are longer and groups smaller than level 1.
        // ════════════════════════════════════════════════
        public const string PLANT_ZOMBIE_TAG = "Zombie_Plant";
        private float L2PlantSpeed    => moderateZombieSpeed * 0.85f;
        private const int L2PlantHealth = 4;
        private float L2SmallSpeed    => smallZombieSpeed * 0.9f;
        private int   L2SmallHealth   => Mathf.Max(4, smallZombieHealth - 2);

        private string PlantTag => (plantZombiePrefab != null) ? PLANT_ZOMBIE_TAG : "Zombie_Moderate";

        private IEnumerator Level2WaveRoutine(float totalDuration)
        {
            yield return new WaitForSeconds(1.0f);

            // Opening: a row of 3 plant zombies shuffling out of the overgrowth
            SpawnLevel2Row(3, 22.0f);

            // Phase 1: plant zombies in rows and loose pairs
            yield return RunPhase(
                phaseName: "L2_Phase1_Sprouting",
                phaseDuration: phase1Duration,
                totalDuration: totalDuration,
                phaseStartOffset: 0f,
                spawnInterval: new Vector2(2.6f, 3.4f),
                spawnAction: SpawnLevel2Phase1Wave
            );

            // Phase 2: mixed plant / scout groups

            yield return RunPhase(
                phaseName: "L2_Phase2_WyvernSkies",
                phaseDuration: phase2Duration,
                totalDuration: totalDuration,
                phaseStartOffset: phase1Duration,
                spawnInterval: new Vector2(2.4f, 3.2f),
                spawnAction: SpawnLevel2Phase2Wave
            );

            // Phase 3: plant rows advance

            yield return RunPhase(
                phaseName: "L2_Phase3_Overgrowth",
                phaseDuration: phase3Duration,
                totalDuration: totalDuration,
                phaseStartOffset: phase1Duration + phase2Duration,
                spawnInterval: new Vector2(2.2f, 3.0f),
                spawnAction: SpawnLevel2Phase3Wave
            );

            // Boss arrives flanked by two plant zombies (same boss HP as level 1)
            if (ZombieRunnerUIManager.Instance != null)
            {
                ZombieRunnerUIManager.Instance.UpdateWaveProgress(1.0f);
                ZombieRunnerUIManager.Instance.TriggerBossAlert();
            }

            yield return new WaitForSeconds(1.2f);

            GameObject bossObj = SpawnSingleZombie("Zombie_Boss", bossZombieSpeed, bossZombieHealth, 0f, 75.0f);
            SpawnSingleZombie(PlantTag, L2PlantSpeed * 0.6f, L2PlantHealth, -2.0f, 72.0f);
            SpawnSingleZombie(PlantTag, L2PlantSpeed * 0.6f, L2PlantHealth,  2.0f, 72.0f);
            if (bossObj != null)
            {
                MobUnit bossUnit = bossObj.GetComponent<MobUnit>();
                if (bossUnit != null && ZombieRunnerUIManager.Instance != null)
                    ZombieRunnerUIManager.Instance.OnBossSpawned(bossUnit, bossZombieHealth);
            }
        }

        private void SpawnLevel2Phase1Wave()
        {
            if (Random.value < 0.5f) SpawnLevel2Row(Random.Range(2, 4));
            else SpawnLevel2FlankPair(PlantTag);
        }

        private void SpawnLevel2Phase2Wave()
        {
            float r = Random.value;
            if (r < 0.4f)       SpawnLevel2Row(3);
            else if (r < 0.7f)  SpawnLevel2FlankPair(PlantTag);
            else                SpawnLevel2SingleFile(3, Random.Range(laneMinX, laneMaxX));
        }

        private void SpawnLevel2Phase3Wave()
        {
            if (Random.value < 0.6f) SpawnLevel2Row(Random.Range(3, 5));
            else
            {
                SpawnLevel2FlankPair(PlantTag);
                SpawnSingleZombie("Zombie_Small", L2SmallSpeed, L2SmallHealth, 0f, spawnAheadDistanceZ + 4.0f);
            }
        }

        /// <summary>Plant zombies side by side across the road (same Z).</summary>
        private void SpawnLevel2Row(int count, float? atZ = null)
        {
            float z = atZ ?? (spawnAheadDistanceZ + Random.Range(0f, 4.0f));
            for (int i = 0; i < count; i++)
            {
                float t = (count == 1) ? 0.5f : i / (float)(count - 1);
                float x = Mathf.Lerp(laneMinX, laneMaxX, t);
                SpawnSingleZombie(PlantTag, L2PlantSpeed, L2PlantHealth, x, z + Random.Range(-0.4f, 0.4f));
            }
        }

        /// <summary>One zombie on each road edge at once.</summary>
        private void SpawnLevel2FlankPair(string tag)
        {
            float z = spawnAheadDistanceZ;
            SpawnSingleZombie(tag, L2PlantSpeed, L2PlantHealth, laneMinX - 0.4f, z);
            SpawnSingleZombie(tag, L2PlantSpeed, L2PlantHealth, laneMaxX + 0.4f, z);
        }

        /// <summary>Small zombies in single file down one lane.</summary>
        private void SpawnLevel2SingleFile(int count, float x)
        {
            float z = spawnAheadDistanceZ;
            for (int i = 0; i < count; i++)
                SpawnSingleZombie("Zombie_Small", L2SmallSpeed, L2SmallHealth, x, z + i * 2.4f);
        }

        public GameObject SpawnFlyingDragonThreat() =>
            SpawnFlyingDragonThreat(new Vector3(0f, 6.5f, 65.0f), "Level2_FlyingWyvernDragon");

        public GameObject SpawnFlyingDragonThreat(Vector3 position, string objName)
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/WyvernDragon_Prefab");
            if (prefab == null)
            {
#if UNITY_EDITOR
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/WyvernDragon_Prefab.prefab");
#endif
            }
            if (prefab == null) return null;

            GameObject dragon = Instantiate(prefab, position, Quaternion.identity);
            dragon.name = objName;
            Debug.Log("<color=#FF9900><b>[Level 2] Flying Wyvern Dragon Air Threat Unleashed!</b></color>");
            return dragon;
        }

        // ────────────────────────────────────────────────────────────
        //  Generic phase runner — handles timing, progress bar, pausing
        // ────────────────────────────────────────────────────────────
        private IEnumerator RunPhase(string phaseName, float phaseDuration, float totalDuration,
                                     float phaseStartOffset, Vector2 spawnInterval,
                                     System.Action spawnAction)
        {
            Debug.Log($"[ZombieRoadWaveSpawner] Starting {phaseName}");
            float elapsed   = 0f;
            float nextSpawn = 0f;

            while (elapsed < phaseDuration && isSpawningActive)
            {
                // Pause on menu / modal
                if (ZombieRunnerUIManager.Instance != null &&
                    (ZombieRunnerUIManager.Instance.CurrentState != ZombieRunnerUIManager.ScreenState.Gameplay ||
                     ZombieRunnerUIManager.Instance.IsModalOpen))
                {
                    yield return new WaitForSeconds(0.25f);
                    continue;
                }

                elapsed += Time.deltaTime;

                // Global 0→1 progress across all 3 phases
                float globalProgress = Mathf.Clamp01((phaseStartOffset + elapsed) / totalDuration);
                if (ZombieRunnerUIManager.Instance != null)
                    ZombieRunnerUIManager.Instance.UpdateWaveProgress(globalProgress);

                if (elapsed >= nextSpawn)
                {
                    spawnAction();
                    nextSpawn = elapsed + Random.Range(spawnInterval.x, spawnInterval.y);
                }

                yield return null;
            }
        }

        // ────────────────────────────────────────────────────────────
        //  Phase 1: Scout Rush — ONLY small fast red zombies, 2–3 at a time
        // ────────────────────────────────────────────────────────────
        private void SpawnPhase1Wave()
        {
            int count = Random.Range(2, 4);  // 2 or 3
            for (int i = 0; i < count; i++)
                SpawnSingleZombie("Zombie_Small", smallZombieSpeed, smallZombieHealth);
        }

        // ────────────────────────────────────────────────────────────
        //  Phase 2: Mixed Assault — scouts OR moderate packs, alternating
        // ────────────────────────────────────────────────────────────
        private void SpawnPhase2Wave()
        {
            bool sendScouts = (Random.value < 0.50f);
            if (sendScouts)
            {
                int count = Random.Range(1, 3);  // 1–2 scouts
                for (int i = 0; i < count; i++)
                    SpawnSingleZombie("Zombie_Small", smallZombieSpeed, smallZombieHealth);
            }
            else
            {
                int count = Random.Range(2, 4);  // 2–3 moderates
                for (int i = 0; i < count; i++)
                    SpawnSingleZombie("Zombie_Moderate", moderateZombieSpeed, moderateZombieHealth);
            }
        }

        // ────────────────────────────────────────────────────────────
        //  Phase 3: Heavy Push — mostly large moderate packs
        // ────────────────────────────────────────────────────────────
        private void SpawnPhase3Wave()
        {
            bool mixedAssault = (Random.value < 0.30f);  // 30% chance: 1 scout + 2 moderates together
            if (mixedAssault)
            {
                SpawnSingleZombie("Zombie_Small",    smallZombieSpeed,    smallZombieHealth);
                SpawnSingleZombie("Zombie_Moderate", moderateZombieSpeed, moderateZombieHealth);
                SpawnSingleZombie("Zombie_Moderate", moderateZombieSpeed, moderateZombieHealth);
            }
            else
            {
                int count = Random.Range(3, 5);  // 3–4 moderate zombies charging
                for (int i = 0; i < count; i++)
                    SpawnSingleZombie("Zombie_Moderate", moderateZombieSpeed, moderateZombieHealth);
            }
        }

        // ════════════════════════════════════════════════
        //  Core Spawn Helper
        // ════════════════════════════════════════════════

        private GameObject SpawnSingleZombie(string poolTag, float speed, int hp,
                                              float? forcedX = null, float? forcedZ = null)
        {
            float playerZ = 0f;
            if (PlayerRunnerController.Instance != null)
                playerZ = PlayerRunnerController.Instance.transform.position.z;

            float spawnZ = playerZ + (forcedZ.HasValue
                ? Mathf.Max(15.0f, forcedZ.Value)
                : (Mathf.Max(spawnAheadDistanceZ, 38.0f) + Random.Range(0f, 6.0f)));

            float spawnX = forcedX.HasValue ? forcedX.Value : Random.Range(laneMinX, laneMaxX);
            Vector3 spawnPos = new Vector3(spawnX, 0f, spawnZ);

            // Spawn facing toward the player (-Z direction)
            Quaternion rot = Quaternion.LookRotation(Vector3.back, Vector3.up);

            GameObject zombie = null;
            if (ObjectPooler.Instance != null && ObjectPooler.Instance.HasPool(poolTag))
                zombie = ObjectPooler.Instance.SpawnFromPool(poolTag, spawnPos, rot);

            if (zombie == null)
            {
                GameObject prefab = GetPrefabForTag(poolTag);
                if (prefab != null)
                    zombie = Instantiate(prefab, spawnPos, rot);
            }

            if (zombie != null)
            {
                zombie.transform.position = spawnPos;
                zombie.transform.rotation = rot;

                GameObject prefabRef = GetPrefabForTag(poolTag);
                if (prefabRef != null)
                {
                    zombie.transform.localScale = prefabRef.transform.localScale;
                }

                MobUnit unit = zombie.GetComponent<MobUnit>();
                if (unit != null)
                {
                    unit.Team    = MobTeam.Enemy;
                    unit.PoolTag = poolTag;
                    unit.SetStats(speed, hp, 1);
                }
            }

            return zombie;
        }

        private GameObject GetPrefabForTag(string tag)
        {
            switch (tag)
            {
                case "Zombie_Small":    return smallZombiePrefab;
                case "Zombie_Moderate": return moderateZombiePrefab;
                case "Zombie_Boss":     return bossZombiePrefab;
                case PLANT_ZOMBIE_TAG:  return plantZombiePrefab != null ? plantZombiePrefab : moderateZombiePrefab;
                default:                return moderateZombiePrefab;
            }
        }
    }
}
