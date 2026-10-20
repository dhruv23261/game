using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MobControl.Core;
using MobControl.UI;
using MobControl.Crowd;
using MobControl.Combat;

namespace MobControl.Combat
{
    public class PlayerRunnerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float forwardSpeed = 4.0f;
        [SerializeField] private float horizontalLimit = 2.5f; // Clamped within road yellow curbs
        [SerializeField] private float mobileSlideMultiplier = 2.4f; // Swipe sensitivity across road
        [SerializeField] private float horizontalLerpSpeed = 28.0f; // Instant zero-latency glide
        [SerializeField] private float horizontalSensitivity = 14.0f;

        [Header("Stopping & Combat Barrier")]
        [SerializeField] private float stoppingDistance = 1.35f;
        [SerializeField] private LayerMask obstacleMask = ~0;

        [Header("Shooting Settings")]
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private Vector3 muzzleOffset = new Vector3(0.24f, 1.35f, 0.95f); // Rifle muzzle tip (Right: +0.24m, Up: +1.35m, Forward: +0.95m)
        [SerializeField] private float fireRate = 0.10f; // 10 bullets per second
        [SerializeField] private bool autoShoot = true;

        [Header("Equipped Weapon Visuals")]
        [SerializeField] private GameObject equippedWeaponInstance;
        [SerializeField] private Transform muzzleFirePoint;

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

        public static PlayerRunnerController Instance { get; private set; }
        public bool IsDead => isDead;
        public bool IsBlocked => isBlocked;
        public int Health => currentHealth;
        public int MaxHealth => maxHealth;
        public float GlobalZombieSpeedMultiplier => globalZombieSpeedMultiplier;
        public GameObject BulletPrefab => bulletPrefab;
        public Vector3 MuzzleOffset => muzzleOffset;
        public float FireRate => fireRate;
        public int SquadAllyCount => squadAllies.Count;
        public int TotalSquadSize => 1 + squadAllies.Count;

        private float touchStartX;
        private float playerStartX;
        private bool isDraggingInput = false;
        private readonly HashSet<int> passedGateIds = new HashSet<int>();
        private readonly System.Collections.Generic.List<PlayerSquadAlly> squadAllies = new System.Collections.Generic.List<PlayerSquadAlly>();
        private static readonly Vector3[] formationOffsets = new Vector3[]
        {
            new Vector3(-0.65f, 0f, -0.25f), // Wingman 1 (Left)
            new Vector3( 0.65f, 0f, -0.25f), // Wingman 2 (Right)
            new Vector3(-1.20f, 0f, -0.50f), // Outer 1 (Left)
            new Vector3( 1.20f, 0f, -0.50f), // Outer 2 (Right)
            new Vector3(-0.40f, 0f, -0.70f), // Rank 2 (Left)
            new Vector3( 0.40f, 0f, -0.70f), // Rank 2 (Right)
            new Vector3(-0.95f, 0f, -0.90f), // Rank 2 Outer (Left)
            new Vector3( 0.95f, 0f, -0.90f), // Rank 2 Outer (Right)
            new Vector3(-0.60f, 0f, -1.15f), // Rank 3 (Left)
            new Vector3( 0.60f, 0f, -1.15f), // Rank 3 (Right)
            new Vector3(-1.15f, 0f, -1.35f), // Rank 3 Outer (Left)
            new Vector3( 1.15f, 0f, -1.35f)  // Rank 3 Outer (Right)
        };

        private Vector3 GetFormationOffset(int index)
        {
            if (index < formationOffsets.Length) return formationOffsets[index];
            int row = index / 4;
            int col = index % 4;
            float x = (col - 1.5f) * 0.60f;
            float z = -0.3f - (row * 0.45f);
            return new Vector3(x, 0f, z);
        }

        public bool HasPassedGate(int gateInstanceId) => passedGateIds.Contains(gateInstanceId);
        public void MarkGatePassed(int gateInstanceId) => passedGateIds.Add(gateInstanceId);

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

            // Attach Squad Ally AI
            PlayerSquadAlly ally = allyObj.AddComponent<PlayerSquadAlly>();
            ally.Initialize(this, offset, bulletPrefab, muzzleOffset, fireRate);

            squadAllies.Add(ally);
            Debug.Log($"[PlayerRunner] Cloned Squad Ally #{squadAllies.Count} joined the squad!");
        }

        public void RemoveSquadAlly(PlayerSquadAlly ally)
        {
            if (squadAllies.Contains(ally))
            {
                squadAllies.Remove(ally);
            }
        }

        [Header("Character Visuals")]
        [SerializeField] private Material playerMaterial;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;

            EnsurePlayerMaterial();
            EnsureEquippedWeapon();

            // Automatically ensure UI and Streaming managers exist
            ZombieRunnerUIManager.EnsureInstance();
            UpcomingZoneManager.EnsureInstance();
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
                    if (r.GetComponent<LineRenderer>() == null && !r.name.Contains("Gun") && !r.name.Contains("M4") && !r.name.Contains("Receiver") && !r.name.Contains("Barrel") && !r.name.Contains("Magazine") && !r.name.Contains("Stock") && !r.name.Contains("Sight"))
                    {
                        r.sharedMaterial = mat;
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
            Application.targetFrameRate = 60;

            EnsureEquippedWeapon();
            ApplyEquippedHeroStats();

            // Spawn any upgraded squad allies if unlocked
            int upgradedAllies = PlayerPrefs.GetInt("ZombieSquad_Upgrade_Count", 0);
            if (upgradedAllies > 0)
            {
                SpawnSquadAllies(upgradedAllies);
            }

            // Pre-warm the bullet pool immediately so first shots have zero lag
            if (bulletPrefab != null && ObjectPooler.Instance != null)
            {
                ObjectPooler.Instance.RegisterPool("Bullet", bulletPrefab, 60);
            }
        }

        public void ResetForNewRun(float startZ = 0f)
        {
            isDead = false;
            isBlocked = false;
            currentHealth = maxHealth;
            targetX = 0f;
            Vector3 pos = transform.position;
            pos.x = 0f;
            pos.z = startZ;
            transform.position = pos;
            Animator anim = GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.Rebind();
                anim.Update(0f);
            }
            Debug.Log($"[PlayerRunnerController] ResetForNewRun: Position reset to (0, {pos.y}, {startZ}), HP reset to {currentHealth}/{maxHealth}");
        }

        public void ApplyEquippedHeroStats()
        {
            string hero = PlayerPrefs.GetString("ZombieSquad_SelectedHero", "Soldier");
            switch (hero)
            {
                case "Ranger":
                    forwardSpeed = 10.0f;
                    fireRate = 12.0f; // 12 bullets/sec
                    maxHealth = 3;
                    break;
                case "Heavy":
                    forwardSpeed = 7.0f;
                    fireRate = 6.5f; // 6.5 bullets/sec
                    maxHealth = 5;
                    break;
                case "Sniper":
                    forwardSpeed = 8.5f;
                    fireRate = 4.0f; // 4 heavy bullets/sec
                    maxHealth = 2;
                    break;
                case "Medic":
                    forwardSpeed = 9.0f;
                    fireRate = 8.0f; // 8 bullets/sec
                    maxHealth = 4;
                    break;
                default: // Soldier
                    forwardSpeed = 8.5f;
                    fireRate = 9.0f; // 9 bullets/sec
                    maxHealth = 3;
                    break;
            }

            // Apply Squad Upgrades
            int healthBonus = PlayerPrefs.GetInt("ZombieSquad_Upgrade_Health", 0);
            maxHealth += healthBonus;
            currentHealth = maxHealth;
            isDead = false;

            float speedBonus = PlayerPrefs.GetInt("ZombieSquad_Upgrade_Speed", 0) * 0.4f;
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

            EnsureEquippedWeapon();

            Debug.Log($"[PlayerRunnerController] Applied stats for {hero} with {weapon}: Speed={forwardSpeed}, FireRate={fireRate}, MaxHP={maxHealth}");
        }

        private void Update()
        {
            if (!isControllable || isDead) return;

            HandleHorizontalInput();
            ApplyMovement();

            if (autoShoot || Input.GetMouseButton(0) || Input.touchCount > 0)
            {
                HandleShooting();
            }
        }

        private void HandleHorizontalInput()
        {
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
                    // Full road sweep: dragging across ~35% of screen width covers full road width
                    float worldDelta = (screenDeltaX / (float)Screen.width) * (horizontalLimit * 2.4f);
                    targetX = Mathf.Clamp(playerStartX + worldDelta, -horizontalLimit, horizontalLimit);
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
            else if (Input.GetMouseButton(0) && isDraggingInput)
            {
                float screenDeltaX = Input.mousePosition.x - touchStartX;
                float worldDelta = (screenDeltaX / (float)Screen.width) * (horizontalLimit * mobileSlideMultiplier);
                targetX = Mathf.Clamp(playerStartX + worldDelta, -horizontalLimit, horizontalLimit);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                isDraggingInput = false;
            }

            // 3. Keyboard A/D or Left/Right Arrow Keys
            float horizontalInput = Input.GetAxisRaw("Horizontal");
            if (Mathf.Abs(horizontalInput) > 0.01f)
            {
                targetX += horizontalInput * horizontalSensitivity * Time.deltaTime;
                targetX = Mathf.Clamp(targetX, -horizontalLimit, horizontalLimit);
            }
        }

        private void ApplyMovement()
        {
            Vector3 pos = transform.position;

            // Throttle the expensive OverlapBox check to ~15x/sec
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

            // High responsiveness, smooth frame-rate independent interpolation
            pos.x = Mathf.Lerp(pos.x, targetX, 1f - Mathf.Exp(-horizontalLerpSpeed * Time.deltaTime));
            transform.position = pos;
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

                // 1. Blocked by living enemy zombie directly in front on the same lane
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

                // 2. Blocked by unbroken destructible iron gate
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
            // Consistent forward aiming down the player's track at torso/chest height
            return new Vector3(transform.position.x + 0.18f, transform.position.y + 0.95f, transform.position.z + 0.70f);
        }

        private void ShootBullet()
        {
            if (bulletPrefab == null) return;

            Vector3 spawnPos = GetMuzzlePosition();
            GameObject bullet = null;

            // Use object pool for bullets to eliminate GC spikes
            if (ObjectPooler.Instance != null)
            {
                bullet = ObjectPooler.Instance.SpawnFromPool("Bullet", spawnPos, Quaternion.identity);
            }

            if (bullet == null)
            {
                bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
            }

            if (equippedWeaponInstance != null)
            {
                StartCoroutine(WeaponRecoilPunch(equippedWeaponInstance.transform));
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
            if (isDead) return;

            currentHealth -= damage;
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

        private void Die()
        {
            if (isDead) return;
            isDead = true;
            isControllable = false;

            Debug.Log("[PlayerRunner] Player defeated by zombies! Triggering Game Over.");

            ZombieRunnerUIManager ui = ZombieRunnerUIManager.Instance != null 
                ? ZombieRunnerUIManager.Instance 
                : ZombieRunnerUIManager.EnsureInstance();

            if (ui != null)
            {
                ui.TriggerDefeatWithReason("YOU WERE OVERWHELMED!", "The zombie horde infected your squad.");
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
            if (equippedWeaponInstance != null && equippedWeaponInstance.activeInHierarchy)
            {
                if (muzzleFirePoint == null)
                {
                    Transform mp = equippedWeaponInstance.transform.Find("MuzzleFirePoint");
                    if (mp != null) muzzleFirePoint = mp;
                }
                return;
            }

            // 1. Look for existing Gun_M41D under this character
            Transform existingGun = null;
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name.Contains("Gun_M41D") || (t.name == "Gun" && t.parent != null && t.parent.name.Contains("Gun")))
                {
                    existingGun = t;
                    break;
                }
            }

            if (existingGun != null)
            {
                existingGun.gameObject.SetActive(true);

                // Enable renderers and ensure GunMaterial is applied
                Renderer[] gunRends = existingGun.GetComponentsInChildren<Renderer>(true);
                Material gunMat = Resources.Load<Material>("Materials/GunMaterial");
#if UNITY_EDITOR
                if (gunMat == null)
                {
                    gunMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/GunMaterial.mat");
                }
#endif
                foreach (var r in gunRends)
                {
                    r.enabled = true;
                    if (gunMat != null) r.sharedMaterial = gunMat;
                }

                float rootScale = (existingGun.transform.root != null && existingGun.transform.root.lossyScale.x > 0.001f) 
                    ? existingGun.transform.root.lossyScale.x 
                    : 100f;
                float gunScale = (rootScale > 10f) ? 0.0004f : 0.04f;
                Vector3 gunPos = (rootScale > 10f) 
                    ? new Vector3(-0.00127f, 0.00099f, 0.00052f) 
                    : new Vector3(-0.0127f, 0.0099f, 0.0052f);

                existingGun.localScale = new Vector3(gunScale, gunScale, gunScale);
                existingGun.localPosition = gunPos;
                existingGun.localRotation = Quaternion.Euler(183.274f, 90f, 93.656f);

                Transform mp = existingGun.Find("MuzzleFirePoint");
                if (mp == null)
                {
                    GameObject mpObj = new GameObject("MuzzleFirePoint");
                    mpObj.transform.SetParent(existingGun, false);
                    mpObj.transform.localPosition = new Vector3(6.7f, 0.1f, 0.05f); // Front tip of barrel
                    mpObj.transform.localRotation = Quaternion.identity;
                    mp = mpObj.transform;
                }

                muzzleFirePoint = mp;
                firePoint = mp;
                equippedWeaponInstance = existingGun.gameObject;
                return;
            }

            // 2. Fallback: Instantiate Gun_M41D or build stylized assault rifle
            Transform rightHand = FindRightHandBone(transform);
            Transform mount = rightHand != null ? rightHand : transform;

            GameObject gunPrefab = Resources.Load<GameObject>("Prefabs/Gun_M41D");
#if UNITY_EDITOR
            if (gunPrefab == null)
            {
                gunPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gun_M41D.prefab");
            }
#endif
            if (gunPrefab != null && mount != null)
            {
                GameObject newGun = Instantiate(gunPrefab, mount);
                newGun.name = "Gun_M41D";
                newGun.SetActive(true);

                float rootScale = (mount.transform.root != null && mount.transform.root.lossyScale.x > 0.001f) 
                    ? mount.transform.root.lossyScale.x 
                    : 100f;
                float gunScale = (rootScale > 10f) ? 0.0004f : 0.04f;
                Vector3 gunPos = (rootScale > 10f) 
                    ? new Vector3(-0.00127f, 0.00099f, 0.00052f) 
                    : new Vector3(-0.0127f, 0.0099f, 0.0052f);

                newGun.transform.localScale = new Vector3(gunScale, gunScale, gunScale);
                newGun.transform.localPosition = gunPos;
                newGun.transform.localRotation = Quaternion.Euler(183.274f, 90f, 93.656f);

                Transform mp = newGun.transform.Find("MuzzleFirePoint");
                if (mp == null)
                {
                    GameObject mpObj = new GameObject("MuzzleFirePoint");
                    mpObj.transform.SetParent(newGun.transform, false);
                    mpObj.transform.localPosition = new Vector3(6.7f, 0.1f, 0.05f);
                    mp = mpObj.transform;
                }
                muzzleFirePoint = mp;
                firePoint = mp;
                equippedWeaponInstance = newGun;
                return;
            }

            equippedWeaponInstance = BuildStylizedAssaultRifle(mount, out muzzleFirePoint, rightHand != null);
            if (firePoint == null && muzzleFirePoint != null)
            {
                firePoint = muzzleFirePoint;
            }
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
