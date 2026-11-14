using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using MobControl.Core;
using MobControl.UI;
using MobControl.Crowd;
using MobControl.Combat;
using MobControl.Level;

namespace MobControl.Combat
{
    public class PlayerRunnerController : MonoBehaviour
    {
        [Header("Movement Settings (X-Axis Scroll / Lane Control)")]
        [SerializeField] private float forwardSpeed = 0.0f;
        [SerializeField] private bool stationaryMode = true; // Still khada rahega, forward auto-run disabled
        [SerializeField] private float fixedZ = 0.0f;
        [SerializeField] private float horizontalLimit = 2.5f; // Clamped within road yellow curbs
        [SerializeField] private float mobileSlideMultiplier = 2.2f; // Smooth, balanced swipe sensitivity
        [SerializeField] private float scrollWheelSensitivity = 0.55f; // Mouse scroll wheel sensitivity
        [SerializeField] private float horizontalLerpSpeed = 16.0f; // Calmer, smooth responsive glide
        [SerializeField] private float horizontalSensitivity = 10.0f;

        [Header("Stopping & Combat Barrier")]
        [SerializeField] private float stoppingDistance = 1.35f;
        [SerializeField] private LayerMask obstacleMask = ~0;

        [Header("Shooting Settings")]
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private Vector3 muzzleOffset = new Vector3(0.18f, 0.70f, 0.65f); // Rifle muzzle tip (Right: +0.18m, Up: +0.70m, Forward: +0.65m)
        [SerializeField] private float fireRate = 0.16f; // Initial base fire rate (~6.25 bullets/sec - balanced base ability)
        [SerializeField] private bool autoShoot = true;

        [Header("Gun Upgrade & Colorful Bullets (15s Temporary Validity)")]
        [SerializeField] private bool hasGunUpgrade = false;
        [SerializeField] private int gunUpgradeLevel = 0;
        [SerializeField] private float gunUpgradeDuration = 15.0f; // Exactly 15 seconds temporary upgrade
        [SerializeField] private float gunUpgradeTimer = 0f;
        private float baseFireRateBeforeUpgrade = 0.16f;

        public bool HasGunUpgrade => hasGunUpgrade;
        public int GunUpgradeLevel => gunUpgradeLevel;
        public float GunUpgradeRemainingTime => Mathf.Max(0f, gunUpgradeTimer);
        public float GunUpgradeDuration => gunUpgradeDuration;

        [Header("Equipped Weapon Visuals")]
        [SerializeField] private GameObject equippedWeaponInstance;
        [SerializeField] private Transform muzzleFirePoint;
        [Range(0.2f, 2.0f)] [SerializeField] private float weaponScaleMultiplier = 1.0f;
        public float WeaponScaleMultiplier => weaponScaleMultiplier;

        [Header("Global Zombie Tuning")]
        [Tooltip("Easily increase or decrease all zombie walking speeds together for Level 1")]
        [Range(0.1f, 3.0f)] [SerializeField] private float globalZombieSpeedMultiplier = 1.0f;

        [Header("Health Settings")]
        [SerializeField] private int maxHealth = 3;
        private int currentHealth;
        private bool isDead = false;
        private bool isBlocked = false;

        private Camera mainCamera;
        private float targetX = 0f;
        private float fireTimer = 0f;
        private bool isControllable = true;
        private Vector2 lastTouchPos;
        private bool isBlocked_cached = false;
        private float blockedCheckTimer = 0f;
        private const float BLOCKED_CHECK_INTERVAL = 0.066f; // ~15 times/sec instead of 60
        private static readonly Collider[] s_overlapHits = new Collider[16];

        private static PlayerRunnerController s_instance;
        public static PlayerRunnerController Instance
        {
            get
            {
                if (s_instance == null)
                {
                    foreach (var p in FindObjectsByType<PlayerRunnerController>(FindObjectsSortMode.None))
                    {
                        if (p != null && !p.name.Contains("Ally") && !p.name.Contains("Clone"))
                        {
                            s_instance = p;
                            break;
                        }
                    }
                }
                return s_instance;
            }
            private set => s_instance = value;
        }
        public bool IsDead => isDead;
        public bool IsBlocked => isBlocked;
        public bool IsStationary => stationaryMode;
        public int Health => currentHealth;
        public int MaxHealth => maxHealth;
        public float ForwardSpeed => forwardSpeed;
        public float GlobalZombieSpeedMultiplier => globalZombieSpeedMultiplier;
        public GameObject BulletPrefab => bulletPrefab;
        public Vector3 MuzzleOffset => muzzleOffset;
        public float FireRate => fireRate;
        public int SquadAllyCount => squadAllies.Count;
        public int TotalSquadSize => 1 + squadAllies.Count;
        public System.Collections.Generic.IReadOnlyList<PlayerSquadAlly> SquadAllies => squadAllies;

        private float obstacleInvulnerableUntil = 0f;
        public bool IsObstacleInvulnerable => Time.time < obstacleInvulnerableUntil;
        public void SetObstacleInvulnerable(float duration) => obstacleInvulnerableUntil = Time.time + duration;

        public float TargetX { get => targetX; set => targetX = Mathf.Clamp(value, -horizontalLimit, horizontalLimit); }
        public void SetTargetX(float x) => TargetX = x;
        private string deathTitle = "";
        private string deathReason = "";
        public string DeathTitle => deathTitle;
        public string DeathReason => deathReason;

        private bool isWaitingToStart = false;
        public bool IsWaitingToStart => isWaitingToStart;
        public void SetWaitingToStart(bool waiting) => isWaitingToStart = waiting;
        public bool IsControllable => isControllable;

        private float touchStartX;
        private float playerStartX;
        private bool isDraggingInput = false;
        private readonly HashSet<int> passedGateIds = new HashSet<int>();
        private readonly System.Collections.Generic.List<PlayerSquadAlly> squadAllies = new System.Collections.Generic.List<PlayerSquadAlly>();
        private static readonly Vector3[] formationOffsets = new Vector3[]
        {
            new Vector3(-0.65f, 0f, 0.05f), // Beside Leader (Left)
            new Vector3( 0.65f, 0f, 0.05f), // Beside Leader (Right)
            new Vector3(-1.30f, 0f, 0.10f), // Outer 1 (Left)
            new Vector3( 1.30f, 0f, 0.10f), // Outer 2 (Right)
            new Vector3(-0.40f, 0f, 0.35f), // Front Row Left
            new Vector3( 0.40f, 0f, 0.35f), // Front Row Right
            new Vector3(-0.95f, 0f, 0.40f), // Front Row Outer Left
            new Vector3( 0.95f, 0f, 0.40f)  // Front Row Outer Right
        };

        private Vector3 GetFormationOffset(int index)
        {
            if (index < formationOffsets.Length) return formationOffsets[index];
            int side = (index % 2 == 0) ? -1 : 1;
            int col = (index / 2) + 1;
            return new Vector3(side * col * 0.65f, 0f, 0.05f);
        }

        public bool HasPassedGate(int gateInstanceId) => passedGateIds.Contains(gateInstanceId);
        public void MarkGatePassed(int gateInstanceId) => passedGateIds.Add(gateInstanceId);

        // ---------------- Upgrade helpers ----------------
        // Upgrade levels start at 1 (= not upgraded). Each purchased level adds 1 bonus level.
        public static int UpgradeBonusLevels(string key)
        {
            return Mathf.Max(0, PlayerPrefs.GetInt($"ZombieSquad_Upgrade_{key}", 1) - 1);
        }

        /// <summary>
        /// Bullet damage from the SOLDIER DAMAGE upgrade: +15% average damage per purchased level.
        /// Damage is an int, so the fractional part is rolled per bullet (e.g. 1.3 -> 30% of bullets deal 2).
        /// </summary>
        public static int RollBulletDamage()
        {
            float dmg = 1f + 0.15f * UpgradeBonusLevels("Damage");
            int whole = Mathf.FloorToInt(dmg);
            return whole + (Random.value < dmg - whole ? 1 : 0);
        }

        /// <summary>Makes the squad exactly main soldier + purchased allies (never more, so restarts can't stack allies).</summary>
        private void ApplyUpgradedSquad()
        {
            squadAllies.RemoveAll(a => a == null);
            int target = UpgradeBonusLevels("Count");
            while (squadAllies.Count < target)
            {
                int before = squadAllies.Count;
                SpawnSquadAlly();
                if (squadAllies.Count == before) break; // spawn failed, avoid infinite loop
            }
            UpdateSquadCountBadge();
        }

        public void SpawnSquadAllies(int count = 2)
        {
            for (int i = 0; i < count; i++)
            {
                SpawnSquadAlly();
            }
        }

        public void SpawnSquadAlly()
        {
            int index = squadAllies.Count;
            Vector3 offset = GetFormationOffset(index);
            Vector3 spawnPos = transform.position + offset;

            GameObject allyObj = Instantiate(gameObject, spawnPos, transform.rotation);
            allyObj.name = $"PlayerAlly_{index + 1}";

            // Remove runner controller from clone
            PlayerRunnerController cloneRunner = allyObj.GetComponent<PlayerRunnerController>();
            if (cloneRunner != null)
            {
                Destroy(cloneRunner);
            }

            // Remove MobUnit from clone so it doesn't fight PlayerSquadAlly!
            MobControl.Crowd.MobUnit cloneMob = allyObj.GetComponent<MobControl.Crowd.MobUnit>();
            if (cloneMob != null)
            {
                Destroy(cloneMob);
            }

            // Clean up any rogue health bars from clone
            foreach (var hb in allyObj.GetComponentsInChildren<ThomasDev.HealthSystem.HealthBarUI>(true))
            {
                Destroy(hb.gameObject);
            }
            foreach (var h in allyObj.GetComponentsInChildren<ThomasDev.HealthDamageSystem.Health>(true))
            {
                Destroy(h);
            }

            if (bulletPrefab == null)
            {
                bulletPrefab = Resources.Load<GameObject>("Prefabs/Bullet_9mm") ?? Resources.Load<GameObject>("Prefabs/bullet");
#if UNITY_EDITOR
                if (bulletPrefab == null)
                {
                    bulletPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Bullet_9mm.prefab");
                }
#endif
            }

            // Attach Squad Ally AI
            PlayerSquadAlly ally = allyObj.AddComponent<PlayerSquadAlly>();
            ally.Initialize(this, offset, bulletPrefab, muzzleOffset, fireRate);

            squadAllies.Add(ally);
            UpdateSquadCountBadge();
            Debug.Log($"[PlayerRunner] Cloned Squad Ally #{squadAllies.Count} joined the squad! Total squad: {TotalSquadSize}");
        }

        public void RemoveSquadAlly(PlayerSquadAlly ally)
        {
            if (squadAllies.Contains(ally))
            {
                squadAllies.Remove(ally);
                RealignSquadFormation();
                UpdateSquadCountBadge();
            }
        }

        /// <summary>
        /// Deducts 1 squad member when colliding with a barricade.
        /// Returns true if an ally was removed (squad continues), false if no allies left (death triggered).
        /// </summary>
        public bool DeductSquadMember(PlayerSquadAlly specificAlly = null)
        {
            if (squadAllies.Count > 0)
            {
                PlayerSquadAlly allyToRemove = specificAlly;
                if (allyToRemove == null || !squadAllies.Contains(allyToRemove))
                {
                    allyToRemove = squadAllies[squadAllies.Count - 1];
                }

                squadAllies.Remove(allyToRemove);

                if (allyToRemove != null)
                {
                    Vector3 deathPos = allyToRemove.transform.position;
                    Destroy(allyToRemove.gameObject);
                    SpawnMemberLostEffect(deathPos);
                }

                RealignSquadFormation();
                UpdateSquadCountBadge();

                // Camera shake feedback
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    var camFollow = mainCam.GetComponent<MobControl.Core.SmoothCameraFollow>();
                    if (camFollow != null) camFollow.TriggerShake(0.045f);
                }

                SetObstacleInvulnerable(0.40f);

                Debug.Log($"[PlayerRunner] Deducted 1 squad member! Remaining squad: {TotalSquadSize}");
                return true;
            }

            return false;
        }

        public void RealignSquadFormation()
        {
            for (int i = 0; i < squadAllies.Count; i++)
            {
                if (squadAllies[i] != null)
                {
                    squadAllies[i].SetFormationOffset(GetFormationOffset(i));
                }
            }
        }

        #region Overhead Squad Count Badge & Visuals

        private GameObject squadBadgeObj;
        private TextMeshPro squadBadgeText;
        private readonly Vector3 badgeBaseScale = new Vector3(0.55f, 0.55f, 0.55f);

        public void UpdateSquadCountBadge()
        {
            RemoveSquadCountBadge();
        }

        public void RemoveSquadCountBadge()
        {
            if (squadBadgeObj != null)
            {
                if (Application.isPlaying) Destroy(squadBadgeObj);
                else DestroyImmediate(squadBadgeObj);
                squadBadgeObj = null;
            }
            Transform existing = transform.Find("SquadCountBadge");
            if (existing != null)
            {
                if (Application.isPlaying) Destroy(existing.gameObject);
                else DestroyImmediate(existing.gameObject);
            }
        }

        private IEnumerator PunchBadgeScale()
        {
            if (squadBadgeObj == null) yield break;
            squadBadgeObj.transform.localScale = badgeBaseScale * 1.35f;
            yield return new WaitForSeconds(0.08f);
            if (squadBadgeObj != null)
            {
                squadBadgeObj.transform.localScale = badgeBaseScale;
            }
        }

        public void SpawnMemberLostEffect(Vector3 pos)
        {
            GameObject fx = new GameObject("MemberLost_FX");
            fx.transform.position = pos + Vector3.up * 0.8f;

            GameObject txtObj = new GameObject("MinusOne_Text");
            txtObj.transform.SetParent(fx.transform, false);
            txtObj.transform.localPosition = Vector3.zero;

            TextMeshPro tm = txtObj.AddComponent<TextMeshPro>();
            tm.text = "-1";
            tm.fontSize = 6.5f;
            tm.fontStyle = FontStyles.Bold;
            tm.color = new Color(1f, 0.25f, 0.20f, 1f);
            tm.alignment = TextAlignmentOptions.Center;

            StartCoroutine(AnimateFloatingText(fx, tm));
        }

        private IEnumerator AnimateFloatingText(GameObject root, TextMeshPro tm)
        {
            if (root == null || tm == null) yield break;
            float elapsed = 0f;
            float duration = 0.85f;
            Vector3 startPos = root.transform.position;
            Color baseColor = tm.color;

            while (elapsed < duration && root != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                root.transform.position = startPos + Vector3.up * (t * 1.4f);
                if (Camera.main != null)
                {
                    root.transform.rotation = Camera.main.transform.rotation;
                }
                tm.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f - t);
                yield return null;
            }

            if (root != null) Destroy(root);
        }

        private void LateUpdate()
        {
            if (squadBadgeObj != null && Camera.main != null)
            {
                squadBadgeObj.transform.rotation = Camera.main.transform.rotation;
            }
        }

        #endregion

        [Header("Character Visuals")]
        [SerializeField] private Material playerMaterial;

        private void Awake()
        {
            if (gameObject.name.Contains("Clone") || gameObject.name.StartsWith("PlayerAlly"))
            {
                return;
            }
            Instance = this;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;

            if (bulletPrefab == null)
            {
                bulletPrefab = Resources.Load<GameObject>("Prefabs/Bullet_9mm") ?? Resources.Load<GameObject>("Prefabs/bullet");
#if UNITY_EDITOR
                if (bulletPrefab == null)
                {
                    bulletPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Bullet_9mm.prefab");
                }
#endif
            }

            EnsurePlayerMaterial();
            EnsureEquippedWeapon();
            EnsureVisualController();

            // Ensure solid player collider and ContinuousSpeculative Rigidbody
            CapsuleCollider col = GetComponent<CapsuleCollider>();
            if (col != null)
            {
                col.isTrigger = false;
            }
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            }

            // Automatically ensure UI and Streaming managers exist
            ZombieRunnerUIManager.EnsureInstance();
            UpcomingZoneManager.EnsureInstance();

            // Safety Purge: Remove any stray editor-saved badge pop objects from the world
            GameObject[] strayBadges = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (var go in strayBadges)
            {
                if (go != null && (go.name.StartsWith("BadgePop_") || go.name.Contains("Celebratory3DBanner")))
                {
                    Destroy(go);
                }
            }
        }

        public void EnsurePlayerMaterial()
        {
            Material mat = playerMaterial != null ? playerMaterial : Resources.Load<Material>("Materials/PlayerMaterial");
#if UNITY_EDITOR
            if (mat == null)
            {
                mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/PlayerMaterial.mat");
            }
#endif
            if (mat != null)
            {
                foreach (var r in GetComponentsInChildren<Renderer>(true))
                {
                    if (r.GetComponent<LineRenderer>() == null && 
                        !r.name.Contains("Gun") && 
                        !r.name.Contains("M4") && 
                        !r.name.Contains("Receiver") && 
                        !r.name.Contains("Barrel") && 
                        !r.name.Contains("Magazine") && 
                        !r.name.Contains("Stock") && 
                        !r.name.Contains("Sight") && 
                        !r.name.Contains("AssaultRifle") &&
                        !r.name.Contains("Rifle"))
                    {
                        if (r.sharedMaterial == null)
                        {
                            r.sharedMaterial = mat;
                        }
                    }
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsurePlayerMaterial();
            EnsureEquippedWeapon();
        }
#endif

        private void Start()
        {
            mainCamera = Camera.main;
            targetX = transform.position.x;
            isDead = false;
            isBlocked = false;
            isControllable = true;
            Application.targetFrameRate = 60;
            Application.runInBackground = true;

            if (bulletPrefab == null)
            {
                bulletPrefab = Resources.Load<GameObject>("Prefabs/Bullet_9mm") ?? Resources.Load<GameObject>("Prefabs/bullet");
#if UNITY_EDITOR
                if (bulletPrefab == null)
                {
                    bulletPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Bullet_9mm.prefab");
                }
#endif
            }

            EnsureEquippedWeapon();
            EnsureVisualController();
            ApplyEquippedHeroStats();
            baseFireRateBeforeUpgrade = fireRate;

            // Spawn squad allies bought with the SOLDIER COUNT upgrade (idempotent: safe if ResetForNewRun ran first)
            ApplyUpgradedSquad();

            // Pre-warm the bullet pool immediately so first shots have zero lag
            if (bulletPrefab != null && ObjectPooler.Instance != null)
            {
                ObjectPooler.Instance.RegisterPool("Bullet", bulletPrefab, 80);
            }

            UpdateSquadCountBadge();
            SetObstacleInvulnerable(3.0f);
        }

        private float invulnerableUntil = 0f;

        public void SetInvulnerable(float duration)
        {
            invulnerableUntil = Time.time + duration;
        }

        public void ResetForNewRun(float startZ = 0f)
        {
            isDead = false;
            isBlocked = false;
            isWaitingToStart = true;
            ApplyEquippedHeroStats();
            hasGunUpgrade = false;
            gunUpgradeLevel = 0;
            gunUpgradeTimer = 0f;
            baseFireRateBeforeUpgrade = fireRate;
            if (ZombieRunnerUIManager.Instance != null)
            {
                ZombieRunnerUIManager.Instance.HideGunUpgradeTimer();
            }
            targetX = 0f;
            isFallingInPit = false;
            pitVerticalVelocity = 0f;
            transform.rotation = Quaternion.identity;
            Vector3 pos = transform.position;
            pos.x = 0f;
            pos.y = 0f;
            pos.z = startZ;
            transform.position = pos;

            // Rebuild endless road, but keep stationary until first swipe to start
            if (EndlessRoadManager.Instance != null)
            {
                EndlessRoadManager.Instance.BuildInitialEndlessTrack();
                EndlessRoadManager.Instance.SetRunning(false);
            }

            // Clear existing squad allies cleanly so every run starts with 1 main soldier
            for (int i = squadAllies.Count - 1; i >= 0; i--)
            {
                if (squadAllies[i] != null)
                {
                    Destroy(squadAllies[i].gameObject);
                }
            }
            squadAllies.Clear();
            passedGateIds.Clear();

            // Also clean up any orphan PlayerAlly objects in scene
            foreach (var go in FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go != null && go.name.StartsWith("PlayerAlly_"))
                {
                    Destroy(go);
                }
            }
            Animator anim = GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.Rebind();
                anim.Update(0f);
            }

            // Every run starts with the main soldier + only the allies the player has bought
            ApplyUpgradedSquad();

            UpdateSquadCountBadge();
            Debug.Log($"[PlayerRunnerController] ResetForNewRun: Position reset to (0, {pos.y}, {startZ}), HP reset to {currentHealth}/{maxHealth}, Squad: {TotalSquadSize}");
        }

        public void ApplyEquippedHeroStats()
        {
            string hero = PlayerPrefs.GetString("ZombieSquad_SelectedHero", "Soldier");
            switch (hero)
            {
                case "Ranger":
                    forwardSpeed = 5.8f;
                    fireRate = 12.0f; // 12 bullets/sec
                    maxHealth = 15;
                    break;
                case "Heavy":
                    forwardSpeed = 4.5f;
                    fireRate = 6.5f; // 6.5 bullets/sec
                    maxHealth = 30;
                    break;
                case "Sniper":
                    forwardSpeed = 5.0f;
                    fireRate = 4.0f; // 4 heavy bullets/sec
                    maxHealth = 12;
                    break;
                case "Medic":
                    forwardSpeed = 5.2f;
                    fireRate = 8.0f; // 8 bullets/sec
                    maxHealth = 20;
                    break;
                default: // Soldier
                    forwardSpeed = 5.2f;
                    fireRate = 9.0f; // 9 bullets/sec
                    maxHealth = 18;
                    break;
            }

            // Apply Squad Upgrades
            int healthBonus = UpgradeBonusLevels("Health") * 2; // +2 HP per purchased level
            maxHealth += healthBonus;
            currentHealth = maxHealth;
            isDead = false;

            float speedBonus = UpgradeBonusLevels("Speed") * 0.15f;
            forwardSpeed += speedBonus;

            // Apply Equipped Weapon Modifiers
            string weapon = PlayerPrefs.GetString("ZombieSquad_SelectedWeapon", "AssaultRifle");
            switch (weapon)
            {
                case "Shotgun":
                    fireRate *= 0.8f;
                    break;
                case "Sniper":
                    fireRate *= 0.65f;
                    break;
                case "Minigun":
                    fireRate *= 1.75f;
                    break;
                default:
                    break;
            }
            int wepFireRateBonus = PlayerPrefs.GetInt($"ZombieSquad_Wep_{weapon}_FireRate", 0);
            fireRate += wepFireRateBonus * 0.4f;

            if (fireRate >= 1.0f)
            {
                fireRate = 1.0f / fireRate;
            }
            if (fireRate <= 0.02f || fireRate > 0.5f)
            {
                fireRate = 0.11f;
            }

            if (stationaryMode)
            {
                forwardSpeed = 0.0f;
            }

            EnsureEquippedWeapon();

            Debug.Log($"[PlayerRunnerController] Applied stats for {hero} with {weapon}: Speed={forwardSpeed}, FireRate={fireRate}, MaxHP={maxHealth}, Stationary={stationaryMode}");
        }

        private readonly HashSet<Renderer> homeHiddenRenderers = new HashSet<Renderer>();
        private bool isHiddenForHomeScreen;

        private void Update()
        {
            // Completely hide gameplay runner model while on HomeScreen
            if (ZombieRunnerUIManager.Instance != null && ZombieRunnerUIManager.Instance.CurrentState == ZombieRunnerUIManager.ScreenState.HomeScreen)
            {
                // Remember exactly which renderers we hide so they can be restored when leaving the Home Screen
                foreach (var r in GetComponentsInChildren<Renderer>(true))
                {
                    if (r != null && r.enabled)
                    {
                        r.enabled = false;
                        homeHiddenRenderers.Add(r);
                    }
                }
                isHiddenForHomeScreen = true;
                return;
            }

            // Left the Home Screen: bring the player model back (previously it stayed invisible in gameplay)
            if (isHiddenForHomeScreen)
            {
                foreach (var r in homeHiddenRenderers)
                {
                    if (r != null) r.enabled = true;
                }
                homeHiddenRenderers.Clear();
                isHiddenForHomeScreen = false;
            }

            // Pause movement only when modal menu is open
            if (ZombieRunnerUIManager.Instance != null && ZombieRunnerUIManager.Instance.IsModalOpen)
            {
                return;
            }

            // Gun Upgrade 15-Second Temporary Validity Countdown
            if (hasGunUpgrade)
            {
                gunUpgradeTimer -= Time.deltaTime;
                if (ZombieRunnerUIManager.Instance != null)
                {
                    ZombieRunnerUIManager.Instance.UpdateGunUpgradeTimer(gunUpgradeTimer);
                }
                if (gunUpgradeTimer <= 0f)
                {
                    RevertGunUpgrade();
                }
            }

            // Waiting for player's initial swipe to start the level
            if (isWaitingToStart)
            {
                if (HasSwipeStartInput())
                {
                    if (ZombieRunnerUIManager.Instance != null)
                    {
                        ZombieRunnerUIManager.Instance.StartGameplayFromWaiting();
                    }
                    else
                    {
                        isWaitingToStart = false;
                    }
                }
                else
                {
                    return; // Hold still until player swipes/drags!
                }
            }

            isControllable = true;

            HandleHorizontalInput();
            ApplyMovement();

            if (autoShoot || Input.GetMouseButton(0) || Input.touchCount > 0)
            {
                HandleShooting();
            }

#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                SpawnSquadAlly();
            }
#endif
        }

        public bool HasSwipeStartInput()
        {
            // 1. On-Screen Joystick steering input
            if (OnScreenJoystick.Instance != null && OnScreenJoystick.Instance.IsActive)
            {
                if (Mathf.Abs(OnScreenJoystick.Instance.HorizontalAxis) > 0.04f || OnScreenJoystick.Instance.IsDragging)
                {
                    return true;
                }
            }

            // 2. Mobile Touch Screen Swipe
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved || Mathf.Abs(touch.deltaPosition.x) > 2f)
                {
                    return true;
                }
            }

            // 3. Mouse Drag (Unity Editor / PC Play Mode)
            if (Input.GetMouseButtonDown(0) || (Input.GetMouseButton(0) && Mathf.Abs(Input.GetAxis("Mouse X")) > 0.04f))
            {
                return true;
            }

            // 4. Keyboard Controls (A / D, Left / Right Arrow, Space, Return)
            if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.04f ||
                Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D) ||
                Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow) ||
                Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                return true;
            }

            return false;
        }

        public void ForceLaneDeflection(float offset)
        {
            targetX = Mathf.Clamp(transform.position.x + offset, -horizontalLimit, horizontalLimit);
        }

        private void HandleHorizontalInput()
        {
            // 0. On-Screen Left-Right Joystick (Steering Dodge Control)
            if (OnScreenJoystick.Instance != null && OnScreenJoystick.Instance.IsActive)
            {
                float joy = OnScreenJoystick.Instance.HorizontalAxis;
                if (Mathf.Abs(joy) > 0.02f)
                {
                    targetX = Mathf.Clamp(targetX + joy * 20.0f * Time.deltaTime, -horizontalLimit, horizontalLimit);
                    return;
                }
            }

            // 1. Mobile Touch Screen Swipe (Direct relative drag from touch-down)
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    touchStartX = touch.position.x;
                    playerStartX = targetX;
                    isDraggingInput = true;
                }
                else if (isDraggingInput && (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary))
                {
                    float screenDeltaX = touch.position.x - touchStartX;
                    // Balanced road sweep: smooth comfortable swipe
                    float worldDeltaX = (screenDeltaX / (float)Screen.width) * (horizontalLimit * 2.6f);
                    targetX = Mathf.Clamp(playerStartX + worldDeltaX, -horizontalLimit, horizontalLimit);
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    isDraggingInput = false;
                }
            }
            // 2. Mouse Drag (Unity Editor / PC Play Mode)
            else if (Input.GetMouseButtonDown(0))
            {
                touchStartX = Input.mousePosition.x;
                playerStartX = targetX;
                isDraggingInput = true;
            }
            else if (Input.GetMouseButton(0))
            {
                if (!isDraggingInput)
                {
                    touchStartX = Input.mousePosition.x;
                    playerStartX = targetX;
                    isDraggingInput = true;
                }
                float screenDeltaX = Input.mousePosition.x - touchStartX;
                float worldDeltaX = (screenDeltaX / (float)Screen.width) * (horizontalLimit * 2.6f);
                targetX = Mathf.Clamp(playerStartX + worldDeltaX, -horizontalLimit, horizontalLimit);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                isDraggingInput = false;
            }

            // 3. Mouse Scroll Wheel / Trackpad Scroll (Directly scroll along X-axis between -X and +X)
            float mouseScroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(mouseScroll) > 0.001f)
            {
                targetX += mouseScroll * scrollWheelSensitivity;
                targetX = Mathf.Clamp(targetX, -horizontalLimit, horizontalLimit);
            }
            float horizontalScroll = Input.mouseScrollDelta.x;
            if (Mathf.Abs(horizontalScroll) > 0.001f)
            {
                targetX += horizontalScroll * scrollWheelSensitivity;
                targetX = Mathf.Clamp(targetX, -horizontalLimit, horizontalLimit);
            }

            // 4. Keyboard A/D or Left/Right Arrow Keys
            float horizontalInput = Input.GetAxisRaw("Horizontal");
            if (Mathf.Abs(horizontalInput) > 0.01f)
            {
                targetX += horizontalInput * 10.0f * Time.deltaTime;
                targetX = Mathf.Clamp(targetX, -horizontalLimit, horizontalLimit);
            }
        }

        private void ApplyMovement()
        {
            Vector3 pos = transform.position;

            if (stationaryMode)
            {
                // Pure stationary mode: fixed at Z=0, sliding smoothly along X-axis
                pos.z = fixedZ;
            }
            else
            {
                // Runner mode: advance forward down track
                blockedCheckTimer += Time.deltaTime;
                if (blockedCheckTimer >= BLOCKED_CHECK_INTERVAL)
                {
                    blockedCheckTimer = 0f;
                    isBlocked_cached = CheckBlockedAhead();
                    isBlocked = isBlocked_cached;
                }

                if (!isBlocked)
                {
                    pos.z += forwardSpeed * Time.deltaTime;
                }
            }

            float prevX = pos.x;
            // High responsiveness, smooth frame-rate independent interpolation across lanes
            float desiredX = Mathf.Lerp(pos.x, targetX, 1f - Mathf.Exp(-horizontalLerpSpeed * Time.deltaTime));

            // Solid Obstacle Lateral Collision Prevention:
            // Check if moving laterally towards desiredX collides with any solid obstacle (Barricades or Containers)
            float lateralDelta = desiredX - pos.x;
            if (Mathf.Abs(lateralDelta) > 0.0005f)
            {
                float moveDist = Mathf.Abs(lateralDelta);
                Vector3 moveDir = new Vector3(Mathf.Sign(lateralDelta), 0f, 0f);
                Vector3 p1 = pos + Vector3.up * 0.35f;
                Vector3 p2 = pos + Vector3.up * 1.10f;
                float playerRadius = 0.30f;

                if (Physics.CapsuleCast(p1, p2, playerRadius, moveDir, out RaycastHit hit, moveDist + 0.05f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider != null && !hit.collider.transform.IsChildOf(transform) && hit.collider.gameObject != gameObject)
                    {
                        if (hit.collider.GetComponentInParent<MobControl.Level.RoadObstacle>() != null ||
                            hit.collider.name.Contains("Obstacle") ||
                            hit.collider.name.Contains("Container") ||
                            hit.collider.name.Contains("Barricade"))
                        {
                            // Block player cleanly at the solid obstacle boundary:
                            // Allowed distance is up to the hit distance, never pushing the player backward
                            float allowedDist = Mathf.Max(0f, hit.distance - 0.02f);
                            desiredX = pos.x + moveDir.x * allowedDist;
                            targetX = desiredX;
                        }
                    }
                }
            }

            if (isFallingInPit)
            {
                pitVerticalVelocity -= 22f * Time.deltaTime;
                pos.y += pitVerticalVelocity * Time.deltaTime;
                pos.x = Mathf.MoveTowards(pos.x, pitTargetX, 1.8f * Time.deltaTime);
                transform.position = pos;
                transform.Rotate(Vector3.forward, 240f * Time.deltaTime);

                if (pos.y <= -0.95f)
                {
                    if (EndlessRoadManager.Instance != null)
                    {
                        EndlessRoadManager.Instance.SetRunning(false);
                    }
                    Die("INCINERATED!", "You fell into a boiling molten lava pit!");
                }
                return;
            }

            pos.x = desiredX;

            // Check if player fell behind active gameplay area:
            if (stationaryMode && pos.z < fixedZ - 1.2f)
            {
                if (EndlessRoadManager.Instance != null)
                {
                    EndlessRoadManager.Instance.SetRunning(false);
                }
                Die("OVERRUN!", "You fell behind the active gameplay area.");
                return;
            }

            transform.position = pos;

            // Smooth dynamic arcade bank/lean when dodging left/right
            float deltaX = pos.x - prevX;
            float targetTilt = -deltaX * 320.0f;
            targetTilt = Mathf.Clamp(targetTilt, -12.0f, 12.0f);
            Transform vis = transform.Find("Visual_Mob");
            if (vis != null)
            {
                vis.localRotation = Quaternion.Euler(0f, 0f, targetTilt);
            }
        }

        #region Lava Pit Mechanics
        private bool isFallingInPit = false;
        private float pitVerticalVelocity = 0f;
        private float pitTargetX = 0f;

        public bool IsFallingInPit => isFallingInPit;

        public void TriggerFallIntoPit(Vector3 pitCenter)
        {
            if (isFallingInPit || isDead) return;

            // If we have squad allies, the outermost ally falls into the pit
            if (squadAllies != null && squadAllies.Count > 0)
            {
                var sacrificialAlly = squadAllies[squadAllies.Count - 1];
                if (sacrificialAlly != null)
                {
                    sacrificialAlly.TriggerFallIntoPit(pitCenter);
                    return;
                }
            }

            isFallingInPit = true;
            pitVerticalVelocity = -2.5f;
            pitTargetX = pitCenter.x;
        }
        #endregion

        /// <summary>
        /// Doubles gun speed (cuts shot delay in half) and applies to all active squad allies.
        /// </summary>
        public void ApplyGunSpeedMultiplier(float multiplier)
        {
            if (multiplier <= 0.01f) return;
            fireRate = Mathf.Max(0.045f, fireRate / multiplier);
            Debug.Log($"[PlayerRunnerController] ×{multiplier} Gun Speed Upgrade applied! New fireRate delay: {fireRate:F3}s");
            foreach (var ally in squadAllies)
            {
                if (ally != null) ally.SetFireRate(fireRate);
            }
        }

        /// <summary>
        /// Dedicated Gun Upgrade Container Reward:
        /// Significantly boosts firing rate, activates high-velocity COLOURFUL GOLI (multi-color laser bullets),
        /// and applies the rapid-fire upgrade to all current and future squad allies!
        /// </summary>
        public void UpgradeGunAbility(float speedMultiplier = 2.2f, bool enableColorfulBullets = true)
        {
            if (!hasGunUpgrade || gunUpgradeTimer <= 0f)
            {
                baseFireRateBeforeUpgrade = fireRate;
            }

            hasGunUpgrade = true;
            gunUpgradeLevel++;
            MobControl.Audio.ZombieSoundManager.Instance?.PlayGunUpgrade();
            gunUpgradeTimer = gunUpgradeDuration; // Exactly 15.0s temporary validity

            float oldRate = fireRate;
            fireRate = Mathf.Max(0.045f, baseFireRateBeforeUpgrade / speedMultiplier);
            Debug.Log($"[PlayerRunnerController] ⚡ GUN UPGRADE #{gunUpgradeLevel} APPLIED FOR {gunUpgradeDuration:F0}s! FireRate: {oldRate:F3}s -> {fireRate:F3}s (~{1f / fireRate:F1} bullets/sec), Colorful Bullets={enableColorfulBullets}");

            foreach (var ally in squadAllies)
            {
                if (ally != null) ally.SetFireRate(fireRate);
            }

            // Screen punch shake on powerup
            if (Camera.main != null)
            {
                var camFollow = Camera.main.GetComponent<MobControl.Core.SmoothCameraFollow>();
                if (camFollow != null) camFollow.TriggerShake(0.08f);
            }

            if (ZombieRunnerUIManager.Instance != null)
            {
                ZombieRunnerUIManager.Instance.ShowGunUpgradeTimer(gunUpgradeDuration);
            }
        }

        /// <summary>
        /// Automatically downgrades/reverts the gun ability back to baseline after the 15-second duration expires.
        /// </summary>
        public void RevertGunUpgrade()
        {
            if (!hasGunUpgrade && gunUpgradeTimer <= 0f) return;

            hasGunUpgrade = false;
            gunUpgradeLevel = 0;
            gunUpgradeTimer = 0f;
            fireRate = (baseFireRateBeforeUpgrade > 0.02f) ? baseFireRateBeforeUpgrade : 0.16f;

            foreach (var ally in squadAllies)
            {
                if (ally != null) ally.SetFireRate(fireRate);
            }

            Debug.Log($"[PlayerRunnerController] ⏳ Gun upgrade expired (15s duration completed)! FireRate reverted to base: {fireRate:F3}s");

            if (ZombieRunnerUIManager.Instance != null)
            {
                ZombieRunnerUIManager.Instance.HideGunUpgradeTimer();
                ZombieRunnerUIManager.Instance.ShowMultiplierPop("<color=#FFCC00>⚡ BOOST EXPIRED</color>");
            }
        }

        private bool CheckBlockedAhead()
        {
            Vector3 playerPos = transform.position;

            // Box check directly in front of player's current lane
            Vector3 boxCenter = playerPos + Vector3.forward * (stoppingDistance * 0.5f) + Vector3.up * 0.6f;
            Vector3 boxHalfExtents = new Vector3(0.45f, 0.6f, stoppingDistance * 0.5f);

            int hitCount = Physics.OverlapBoxNonAlloc(boxCenter, boxHalfExtents, s_overlapHits, Quaternion.identity, obstacleMask, QueryTriggerInteraction.Collide);

            for (int i = 0; i < hitCount; i++)
            {
                Collider col = s_overlapHits[i];
                if (col == null) continue;

                // Ignore player's own colliders
                if (col.transform.IsChildOf(transform) || col.gameObject == gameObject) continue;

                // 1. Blocked by solid road obstacle (Barricades & Containers)
                if (col.GetComponentInParent<MobControl.Level.RoadObstacle>() != null ||
                    col.name.Contains("Obstacle") ||
                    col.name.Contains("Container") ||
                    col.name.Contains("Barricade"))
                {
                    float dz = col.transform.position.z - playerPos.z;
                    if (dz > -0.5f && dz <= stoppingDistance + 0.8f)
                    {
                        return true;
                    }
                }

                // 2. Blocked by living enemy zombie directly in front on the same lane
                MobUnit mob = col.GetComponentInParent<MobUnit>();
                if (mob != null && mob.Team == MobTeam.Enemy && mob.Health > 0 && !mob.IsDead)
                {
                    float dz = mob.transform.position.z - playerPos.z;
                    float dx = Mathf.Abs(mob.transform.position.x - playerPos.x);
                    if (dz > 0.05f && dz <= stoppingDistance && dx < 0.65f)
                    {
                        return true;
                    }
                }

                // 3. Blocked by unbroken destructible iron gate
                DestructibleGate gate = col.GetComponentInParent<DestructibleGate>();
                if (gate != null && !gate.IsBroken)
                {
                    float dz = gate.transform.position.z - playerPos.z;
                    if (dz > -0.5f && dz <= stoppingDistance + 0.5f)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void HandleShooting()
        {
            if (isDead) return;

            fireTimer += Time.deltaTime;
            if (fireTimer >= fireRate)
            {
                fireTimer = 0f;
                ShootBullet();
            }
        }

        public Vector3 GetMuzzlePosition()
        {
            if (muzzleFirePoint != null)
            {
                return muzzleFirePoint.position;
            }
            if (firePoint != null)
            {
                return firePoint.position;
            }
            if (equippedWeaponInstance != null)
            {
                return equippedWeaponInstance.transform.position + equippedWeaponInstance.transform.forward * 0.52f + equippedWeaponInstance.transform.up * 0.08f;
            }
            // Consistent forward aiming down the player's track at torso/chest height
            return new Vector3(transform.position.x + 0.20f, transform.position.y + 0.95f, transform.position.z + 0.90f);
        }

        private void ShootBullet()
        {
            if (bulletPrefab == null)
            {
                bulletPrefab = Resources.Load<GameObject>("Prefabs/Bullet_9mm") ?? Resources.Load<GameObject>("Prefabs/bullet");
#if UNITY_EDITOR
                if (bulletPrefab == null)
                {
                    bulletPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Bullet_9mm.prefab");
                }
#endif
            }
            if (bulletPrefab == null) return;

            Vector3 spawnPos = GetMuzzlePosition();
            GameObject bullet = null;

            // Use object pool for bullets to eliminate GC spikes
            if (ObjectPooler.Instance != null)
            {
                bullet = ObjectPooler.Instance.SpawnFromPool("Bullet", spawnPos, Quaternion.LookRotation(Vector3.forward, Vector3.up));
            }

            if (bullet == null)
            {
                bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.LookRotation(Vector3.forward, Vector3.up));
            }

            // 1. Trigger visual controller recoil and muzzle flash
            PlayerMobVisualController visualCtrl = GetComponentInChildren<PlayerMobVisualController>();
            if (visualCtrl != null)
            {
                visualCtrl.TriggerWeaponFire();
            }
            else if (equippedWeaponInstance != null)
            {
                StartCoroutine(WeaponRecoilPunch(equippedWeaponInstance.transform));
            }

            // Play gunshot sound effect
            if (MobControl.Audio.ZombieSoundManager.Instance != null)
            {
                MobControl.Audio.ZombieSoundManager.Instance.PlayGunfire(hasGunUpgrade);
            }

            // 2. Fire bullets for all active squad allies in parallel (2 players = 2 bullets, etc.)
            for (int i = squadAllies.Count - 1; i >= 0; i--)
            {
                if (squadAllies[i] == null)
                {
                    squadAllies.RemoveAt(i);
                    continue;
                }
                squadAllies[i].ShootBullet();
            }

        }

        private IEnumerator WeaponRecoilPunch(Transform gunTr)
        {
            if (gunTr == null) yield break;
            Vector3 originalPos = gunTr.localPosition;
            gunTr.localPosition = originalPos - new Vector3(0f, 0f, 0.025f);
            yield return new WaitForSeconds(0.04f);
            if (gunTr != null)
            {
                gunTr.localPosition = originalPos;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 muzzlePos = GetMuzzlePosition();
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(muzzlePos, 0.05f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(muzzlePos, transform.forward * 1.5f);
        }

        public void TakeDamage(int damage)
        {
            if (isDead || Time.time < invulnerableUntil) return;

            currentHealth -= damage;
            MobControl.Audio.ZombieSoundManager.Instance?.PlayPlayerHurt();
            Debug.Log($"[PlayerRunner] Player took {damage} damage! HP: {currentHealth}/{maxHealth}");

            StartCoroutine(HitFeedbackEffect());

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private IEnumerator HitFeedbackEffect()
        {
            Vector3 origScale = transform.localScale;
            transform.localScale = origScale * 1.1f;
            yield return new WaitForSeconds(0.08f);
            transform.localScale = origScale;
        }

        public void Die(string title = "MISSION FAILED!", string reason = "The zombie horde infected your squad.")
        {
            if (isDead) return;
            isDead = true;
            isControllable = false;
            deathTitle = title;
            deathReason = reason;
            hasGunUpgrade = false;
            gunUpgradeTimer = 0f;
            if (ZombieRunnerUIManager.Instance != null)
            {
                ZombieRunnerUIManager.Instance.HideGunUpgradeTimer();
            }

            Debug.Log($"[PlayerRunner] Player died! {title} - {reason}");

            // Haptic vibration and heavy camera shake on player death
            try
            {
                if (ZombieRunnerUIManager.Instance == null || ZombieRunnerUIManager.Instance.IsVibrationEnabled)
                {
#if UNITY_ANDROID || UNITY_IOS
                    if (!Application.isEditor) Handheld.Vibrate();
#endif
                }
            }
            catch {}

            if (Camera.main != null)
            {
                Camera.main.GetComponent<MobControl.Core.SmoothCameraFollow>()?.TriggerShake(0.35f);
            }

            // Immediately stop endless road scrolling
            if (EndlessRoadManager.Instance != null)
            {
                EndlessRoadManager.Instance.SetRunning(false);
            }

            ZombieRunnerUIManager ui = ZombieRunnerUIManager.Instance != null 
                ? ZombieRunnerUIManager.Instance 
                : ZombieRunnerUIManager.EnsureInstance();

            if (ui != null)
            {
                ui.TriggerDefeatWithReason(title, reason);
            }
            else if (GameManager.Instance != null)
            {
                GameManager.Instance.OnLevelLost();
            }
            else if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowDefeat();
            }
        }

        public void EnsureEquippedWeapon()
        {
            // Purge and destroy any legacy Gun_M41D or primitive placeholder guns
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t != null && (t.name.Contains("Gun_M41D") || t.name == "AssaultRifle_Stylized"))
                {
                    if (Application.isPlaying) Destroy(t.gameObject);
                    else DestroyImmediate(t.gameObject);
                }
            }

            // Check if Meshy Assault Rifle already exists
            Transform meshyGun = null;
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t != null && t.name == "AssaultRifle_Meshy")
                {
                    meshyGun = t;
                    break;
                }
            }

            if (meshyGun == null)
            {
                GameObject gunObj = new GameObject("AssaultRifle_Meshy");
                gunObj.transform.SetParent(transform, false);
                gunObj.transform.localPosition = new Vector3(0.16f, 0.62f, 0.18f);
                gunObj.transform.localRotation = Quaternion.identity;
                gunObj.transform.localScale = Vector3.one * 0.92f;

                Mesh rifleMesh = null;
                GameObject riflePrefab = Resources.Load<GameObject>("Models/AssaultRifle_GameReady") ?? Resources.Load<GameObject>("Prefabs/Gun_M41D");
                if (riflePrefab != null) rifleMesh = riflePrefab.GetComponentInChildren<MeshFilter>()?.sharedMesh;
#if UNITY_EDITOR && !UNITY_ANDROID
                if (rifleMesh == null)
                {
                    riflePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Generated/AssaultRifle/AssaultRifle_GameReady.obj");
                    if (riflePrefab != null) rifleMesh = riflePrefab.GetComponentInChildren<MeshFilter>()?.sharedMesh;
                }
                if (rifleMesh == null) rifleMesh = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Models/Generated/AssaultRifle/AssaultRifle_GameReady.obj");
                if (rifleMesh == null)
                {
                    rifleMesh = MobControl.Level.NewRoadBuilder.LoadObjMeshDirectPublic(System.IO.Path.Combine(Application.dataPath, "Models/Generated/AssaultRifle/AssaultRifle_GameReady.obj"));
                }
#endif

                if (rifleMesh != null)
                {
                    MeshFilter mf = gunObj.AddComponent<MeshFilter>();
                    mf.sharedMesh = rifleMesh;

                    MeshRenderer mr = gunObj.AddComponent<MeshRenderer>();
                    Material rifleMat = Resources.Load<Material>("Materials/M_MeshyAssaultRifle");
#if UNITY_EDITOR && !UNITY_ANDROID
                    if (rifleMat == null)
                    {
                        rifleMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/Generated/AssaultRifle/Materials/M_MeshyAssaultRifle.mat");
                    }
#endif
                    if (rifleMat == null)
                    {
                        rifleMat = MobControl.Level.NewRoadBuilder.LoadTextureMaterialPublic("Textures/Gun/AssaultRifle_base_color.png", "M_MeshyAssaultRifle");
                    }
                    if (rifleMat != null) mr.sharedMaterial = rifleMat;
                }

                GameObject mpObj = new GameObject("MuzzleFirePoint");
                mpObj.transform.SetParent(gunObj.transform, false);
                mpObj.transform.localPosition = new Vector3(0f, 0.08f, 0.52f);
                mpObj.transform.localRotation = Quaternion.identity;

                meshyGun = gunObj.transform;
            }

            if (meshyGun != null)
            {
                // Ensure gun is attached directly to hero root, perfectly positioned and forward-facing
                if (meshyGun.parent != transform)
                {
                    meshyGun.SetParent(transform, false);
                }
                meshyGun.localPosition = new Vector3(0.16f, 0.62f, 0.18f);
                meshyGun.localRotation = Quaternion.identity;
                meshyGun.localScale = Vector3.one * 0.92f;

                MeshFilter mf = meshyGun.GetComponent<MeshFilter>();
                if (mf == null) mf = meshyGun.gameObject.AddComponent<MeshFilter>();
                if (mf.sharedMesh == null)
                {
                    Mesh rm = null;
                    GameObject rp = Resources.Load<GameObject>("Models/AssaultRifle_GameReady") ?? Resources.Load<GameObject>("Prefabs/Gun_M41D");
                    if (rp != null) rm = rp.GetComponentInChildren<MeshFilter>()?.sharedMesh;
#if UNITY_EDITOR && !UNITY_ANDROID
                    if (rm == null)
                    {
                        rp = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Generated/AssaultRifle/AssaultRifle_GameReady.obj");
                        if (rp != null) rm = rp.GetComponentInChildren<MeshFilter>()?.sharedMesh;
                    }
                    if (rm == null) rm = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Models/Generated/AssaultRifle/AssaultRifle_GameReady.obj");
                    if (rm == null) rm = MobControl.Level.NewRoadBuilder.LoadObjMeshDirectPublic(System.IO.Path.Combine(Application.dataPath, "Models/Generated/AssaultRifle/AssaultRifle_GameReady.obj"));
#endif
                    mf.sharedMesh = rm;
                }

                MeshRenderer mr = meshyGun.GetComponent<MeshRenderer>();
                if (mr == null) mr = meshyGun.gameObject.AddComponent<MeshRenderer>();
                if (mr.sharedMaterial == null)
                {
                    Material rmat = Resources.Load<Material>("Materials/M_MeshyAssaultRifle");
#if UNITY_EDITOR && !UNITY_ANDROID
                    if (rmat == null)
                    {
                        rmat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/Generated/AssaultRifle/Materials/M_MeshyAssaultRifle.mat");
                    }
#endif
                    if (rmat == null) rmat = MobControl.Level.NewRoadBuilder.LoadTextureMaterialPublic("Textures/Gun/AssaultRifle_base_color.png", "M_MeshyAssaultRifle");
                    mr.sharedMaterial = rmat;
                }

                equippedWeaponInstance = meshyGun.gameObject;
                Transform mp = meshyGun.Find("MuzzleFirePoint");
                if (mp == null)
                {
                    GameObject mpObj = new GameObject("MuzzleFirePoint");
                    mpObj.transform.SetParent(meshyGun, false);
                    mpObj.transform.localPosition = new Vector3(0f, 0.08f, 0.52f);
                    mpObj.transform.localRotation = Quaternion.identity;
                    mp = mpObj.transform;
                }
                muzzleFirePoint = mp;
                firePoint = mp;
            }
        }

        public void EnsureVisualController()
        {
            PlayerMobVisualController visualCtrl = GetComponentInChildren<PlayerMobVisualController>();
            if (visualCtrl == null)
            {
                // Find character model child (e.g. Visual_Player, or object with Animator or SkinnedMeshRenderer)
                Transform visualTarget = null;
                var anim = GetComponentInChildren<Animator>();
                if (anim != null) visualTarget = anim.transform;
                else
                {
                    var smr = GetComponentInChildren<SkinnedMeshRenderer>();
                    if (smr != null) visualTarget = smr.transform;
                }

                if (visualTarget == null) visualTarget = transform;

                visualCtrl = visualTarget.gameObject.AddComponent<PlayerMobVisualController>();
            }

            Transform visMob = transform.Find("Visual_Mob");
            if (visMob != null)
            {
                visMob.localScale = Vector3.one * 68.0f;
            }

            Transform muzzle = muzzleFirePoint != null ? muzzleFirePoint : (equippedWeaponInstance != null ? equippedWeaponInstance.transform.Find("MuzzleFirePoint") : null);
            visualCtrl.Initialize(this, equippedWeaponInstance != null ? equippedWeaponInstance.transform : null, muzzle);
        }

        public static GameObject BuildStylizedAssaultRifle(Transform attachTarget, out Transform muzzlePoint, bool isAttachedToBone = true)
        {
            GameObject gunRoot = new GameObject("AssaultRifle_Stylized");
            gunRoot.transform.SetParent(attachTarget, false);

            if (isAttachedToBone)
            {
                float s = (attachTarget != null && attachTarget.lossyScale.x > 10f) ? 0.01f : 1f;
                gunRoot.transform.localScale = new Vector3(s, s, s);
                gunRoot.transform.localPosition = s < 0.1f ? new Vector3(-0.0004f, 0.0004f, 0.0002f) : new Vector3(-0.04f, 0.04f, 0.02f);
                gunRoot.transform.localRotation = Quaternion.Euler(180f, 90f, 90f);
            }
            else
            {
                gunRoot.transform.localScale = Vector3.one;
                gunRoot.transform.localPosition = new Vector3(0.24f, 1.20f, 0.40f);
                gunRoot.transform.localRotation = Quaternion.identity;
            }

            Material orangeMat = GetGunMaterial("M_GunOrange", new Color(1.0f, 0.43f, 0.05f));
            Material slateMat = GetGunMaterial("M_GunDarkSlate", new Color(0.14f, 0.16f, 0.18f));

            // 1. Receiver Upper & Lower (Vibrant Orange Body)
            CreateWeaponBox(gunRoot.transform, "Receiver_Main", new Vector3(0f, 0.02f, 0.08f), new Vector3(0.065f, 0.09f, 0.32f), Quaternion.identity, orangeMat);
            // 2. Handguard (Vibrant Orange)
            CreateWeaponBox(gunRoot.transform, "Handguard", new Vector3(0f, 0.02f, 0.34f), new Vector3(0.058f, 0.08f, 0.22f), Quaternion.identity, orangeMat);
            // 3. Top Picatinny Rail (Dark Slate)
            CreateWeaponBox(gunRoot.transform, "Receiver_TopRail", new Vector3(0f, 0.075f, 0.15f), new Vector3(0.032f, 0.02f, 0.38f), Quaternion.identity, slateMat);
            // 4. Barrel (Dark Slate)
            CreateWeaponBox(gunRoot.transform, "Barrel", new Vector3(0f, 0.025f, 0.53f), new Vector3(0.026f, 0.026f, 0.26f), Quaternion.identity, slateMat);
            // 5. Tactical Muzzle Brake / Flash Hider (Dark Slate)
            CreateWeaponBox(gunRoot.transform, "MuzzleBrake", new Vector3(0f, 0.025f, 0.68f), new Vector3(0.038f, 0.038f, 0.07f), Quaternion.identity, slateMat);
            // 6. Tactical Magazine (Dark Slate)
            CreateWeaponBox(gunRoot.transform, "TacticalMagazine", new Vector3(0f, -0.095f, 0.14f), new Vector3(0.044f, 0.16f, 0.085f), Quaternion.Euler(14f, 0f, 0f), slateMat);
            // 7. Magazine Base Plate (Orange Accent)
            CreateWeaponBox(gunRoot.transform, "Magazine_BasePlate", new Vector3(0f, -0.18f, 0.16f), new Vector3(0.048f, 0.03f, 0.09f), Quaternion.Euler(14f, 0f, 0f), orangeMat);
            // 8. Ergonomic Pistol Grip (Dark Slate)
            CreateWeaponBox(gunRoot.transform, "PistolGrip", new Vector3(0f, -0.08f, -0.04f), new Vector3(0.045f, 0.13f, 0.06f), Quaternion.Euler(-20f, 0f, 0f), slateMat);
            // 9. Stock Buffer Tube (Dark Slate)
            CreateWeaponBox(gunRoot.transform, "Stock_BufferTube", new Vector3(0f, 0.025f, -0.15f), new Vector3(0.03f, 0.03f, 0.16f), Quaternion.identity, slateMat);
            // 10. Shoulder Buttstock (Dark Slate)
            CreateWeaponBox(gunRoot.transform, "Stock_ShoulderRest", new Vector3(0f, 0.0f, -0.23f), new Vector3(0.048f, 0.13f, 0.09f), Quaternion.identity, slateMat);
            // 11. Holographic Sight Base (Dark Slate)
            CreateWeaponBox(gunRoot.transform, "Sight_Base", new Vector3(0f, 0.095f, 0.09f), new Vector3(0.038f, 0.025f, 0.08f), Quaternion.identity, slateMat);
            // 12. Holographic Sight Lens Frame (Dark Slate)
            CreateWeaponBox(gunRoot.transform, "Sight_Lens", new Vector3(0f, 0.125f, 0.09f), new Vector3(0.042f, 0.045f, 0.02f), Quaternion.identity, slateMat);

            // Muzzle Fire Point at the exact front tip of barrel
            GameObject muzzleObj = new GameObject("MuzzleFirePoint");
            muzzleObj.transform.SetParent(gunRoot.transform, false);
            muzzleObj.transform.localPosition = new Vector3(0f, 0.025f, 0.72f);
            muzzleObj.transform.localRotation = Quaternion.identity;
            muzzlePoint = muzzleObj.transform;

            return gunRoot;
        }

        private static GameObject CreateWeaponBox(Transform parent, string name, Vector3 localPos, Vector3 size, Quaternion localRot, Material mat)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPos;
            cube.transform.localRotation = localRot;
            cube.transform.localScale = size;

            Collider c = cube.GetComponent<Collider>();
            if (c != null)
            {
                if (Application.isPlaying) Destroy(c);
                else DestroyImmediate(c);
            }

            Renderer r = cube.GetComponent<Renderer>();
            if (r != null && mat != null)
            {
                r.sharedMaterial = mat;
            }
            return cube;
        }

        private static Material GetGunMaterial(string matName, Color fallbackColor)
        {
            Material mat = Resources.Load<Material>($"Materials/{matName}");
#if UNITY_EDITOR
            if (mat == null)
            {
                mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/{matName}.mat");
            }
#endif
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                mat = new Material(shader);
                mat.color = fallbackColor;
            }
            return mat;
        }

        public static Transform FindRightHandBone(Transform root)
        {
            if (root == null) return null;
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            foreach (var t in children)
            {
                string n = t.name.ToLowerInvariant();
                if (n.Contains("righthand") || n.EndsWith(":righthand") || n.Contains("right_hand"))
                {
                    return t;
                }
            }
            foreach (var t in children)
            {
                string n = t.name.ToLowerInvariant();
                if (n.Contains("hand") && (n.Contains("right") || n.Contains(".r") || n.Contains("_r")) && !n.Contains("left"))
                {
                    return t;
                }
            }
            return null;
        }

        public void SetControllable(bool state)
        {
            isControllable = state;
        }
    }
}
