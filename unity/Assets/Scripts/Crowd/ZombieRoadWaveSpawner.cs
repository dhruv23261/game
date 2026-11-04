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
        }

        public void RegisterZombiePools()
        {
            LoadPrefabsIfMissing();
            if (ObjectPooler.Instance != null)
            {
                if (smallZombiePrefab    != null) ObjectPooler.Instance.RegisterPool("Zombie_Small",    smallZombiePrefab,    24);
                if (moderateZombiePrefab != null) ObjectPooler.Instance.RegisterPool("Zombie_Moderate", moderateZombiePrefab, 20);
                if (bossZombiePrefab     != null) ObjectPooler.Instance.RegisterPool("Zombie_Boss",     bossZombiePrefab,      6);
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
                                   go.name.StartsWith("Zombie_Boss")))
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
                default:                return moderateZombiePrefab;
            }
        }
    }
}
