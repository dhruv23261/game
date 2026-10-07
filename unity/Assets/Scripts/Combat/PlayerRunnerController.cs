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

        private void Awake()
        {
            if (Instance == null) Instance = this;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;

            // Automatically ensure the progressive upcoming zone streaming is active
            UpcomingZoneManager.EnsureInstance();
        }

        private void Start()
        {
            mainCamera = Camera.main;
            targetX = transform.position.x;
            currentHealth = maxHealth;
            isDead = false;
            isBlocked = false;
            Application.targetFrameRate = 60;

            // Pre-warm the bullet pool immediately so first shots have zero lag
            if (bulletPrefab != null && ObjectPooler.Instance != null)
            {
                ObjectPooler.Instance.RegisterPool("Bullet", bulletPrefab, 60);
            }
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
                if (mob != null && mob.Team == MobTeam.Enemy && mob.Health > 0)
                {
                    float dz = mob.transform.position.z - playerPos.z;
                    float dx = Mathf.Abs(mob.transform.position.x - playerPos.x);
                    if (dz > -0.2f && dz <= stoppingDistance && dx < 0.65f)
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
            return transform.position 
                + (transform.right * muzzleOffset.x) 
                + (transform.up * muzzleOffset.y) 
                + (transform.forward * muzzleOffset.z);
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

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnLevelLost();
            }
            else if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowDefeat();
            }
        }

        public void SetControllable(bool state)
        {
            isControllable = state;
        }
    }
}
