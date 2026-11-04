using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MobControl.Core;
using MobControl.Base;
using MobControl.UI;
using MobControl.Combat;
using MobControl.Level;

namespace MobControl.Crowd
{
    public enum MobTeam
    {
        Player,
        Enemy
    }

    public class MobUnit : MonoBehaviour
    {
        [Header("Unit Settings")]
        [SerializeField] private MobTeam team = MobTeam.Player;
        [SerializeField] private string customPoolTag = "";
        [SerializeField] private float moveSpeed = 3.5f;
        [SerializeField] private float activationSightDistance = 48.0f; // Only moves when visible on mobile screen (within 48m)
        [SerializeField] private float homingTriggerDistance = 8.0f; // Starts gradual steering only within close distance (8m)
        [SerializeField] private float steerSpeed = 1.8f; // Smooth, gentle drift rate
        [SerializeField] private float attackRange = 1.35f;
        [SerializeField] private int maxHealth = 3;
        [SerializeField] private int attackDamage = 1;
        [SerializeField] private float attackCooldown = 0.8f;

        [Header("Audio / VFX / UI")]
        [SerializeField] private WorldHealthBar healthBar;

        private int currentHealth;
        private Rigidbody rb;
        private Animator animator;
        private bool isDead = false;
        private bool isAttacking = false;
        private bool hasBeenAlerted = false;
        private MobUnit targetEnemy = null;
        private PlayerRunnerController targetPlayer = null;
        private float lastAttackTime = 0f;
        private HashSet<int> passedGateIds = new HashSet<int>();
        // Cached components — found once in Awake, reused every frame
        private Simple3DHealthBar cached3DBar;
        private ThomasDev.HealthSystem.HealthBarUI cachedUiBar;
        private PlayerRunnerController runnerController;
        private ChibiZombieAnimator chibiAnimator;
        private Vector3 initialBaseScale = Vector3.one;
        private bool hasCachedBaseScale = false;
        private static readonly WaitForSeconds s_hitFlashWait = new WaitForSeconds(0.08f);

        public MobTeam Team { get => team; set => team = value; }
        public void SetTeam(MobTeam newTeam) => team = newTeam;
        public string PoolTag { get => !string.IsNullOrEmpty(customPoolTag) ? customPoolTag : (team == MobTeam.Player ? "PlayerMob" : "EnemyMob"); set => customPoolTag = value; }
        public int Health => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsDead => isDead;

        public void SetUiHealthBar(ThomasDev.HealthSystem.HealthBarUI bar)
        {
            cachedUiBar = bar;
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            }

            animator = GetComponent<Animator>();
            if (animator != null)
            {
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            }
            runnerController = GetComponent<PlayerRunnerController>();

            // Auto-ensure collider bounds cover the zombie's full body from feet to head and is solid non-trigger
            BoxCollider col = GetComponent<BoxCollider>();
            if (col != null)
            {
                col.isTrigger = false;
                if (col.center.y < col.size.y * 0.3f)
                {
                    col.center = new Vector3(0, col.size.y * 0.5f, 0);
                }
                col.size = new Vector3(Mathf.Max(col.size.x, 0.012f), Mathf.Max(col.size.y, 0.022f), Mathf.Max(col.size.z, 0.012f));
            }

            // Cache expensive component lookups
            cached3DBar = GetComponentInChildren<Simple3DHealthBar>();
            cachedUiBar = GetComponentInChildren<ThomasDev.HealthSystem.HealthBarUI>(true);

            if (activationSightDistance < 45.0f) activationSightDistance = 48.0f;
            if (!hasCachedBaseScale)
            {
                initialBaseScale = transform.localScale;
                hasCachedBaseScale = true;
            }
            chibiAnimator = GetComponent<ChibiZombieAnimator>();
        }

        private void OnEnable()
        {
            if (hasCachedBaseScale)
            {
                transform.localScale = initialBaseScale;
            }
            isDead = false;
            isAttacking = false;
            hasBeenAlerted = false;
            targetEnemy = null;
            targetPlayer = null;
            currentHealth = maxHealth;
            passedGateIds.Clear();

            if (healthBar != null)
            {
                healthBar.Initialize(maxHealth, currentHealth);
            }
            if (cached3DBar != null)
            {
                cached3DBar.SetHealth(maxHealth, maxHealth);
            }

            if (animator != null)
            {
                animator.SetBool("isAttacking", false);
                animator.SetBool("isMoving", true);
            }
        }

        private void Update()
        {
            if (isDead) return;

            // If attached to the Player Runner or an Ally, let PlayerRunnerController / PlayerSquadAlly handle movement exclusively
            if (runnerController != null || GetComponent<MobControl.Combat.PlayerSquadAlly>() != null) return;

            // Combat / Biting logic
            if (isAttacking)
            {
                if (targetPlayer != null)
                {
                    if (targetPlayer.IsDead)
                    {
                        // Player defeated, stop attacking
                        isAttacking = false;
                        targetPlayer = null;
                        if (animator != null) animator.SetBool("isAttacking", false);
                        return;
                    }

                    // Face player while biting
                    Vector3 lookDir = targetPlayer.transform.position - transform.position;
                    lookDir.y = 0;
                    if (lookDir != Vector3.zero)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 10f);
                    }

                    // Periodic bite / attack
                    if (Time.time >= lastAttackTime + attackCooldown)
                    {
                        lastAttackTime = Time.time;
                        PerformAttackOnPlayer();
                    }
                    return;
                }
                else if (targetEnemy != null)
                {
                    if (targetEnemy.isDead)
                    {
                        // Target defeated, resume walking forward!
                        isAttacking = false;
                        targetEnemy = null;
                        if (animator != null) animator.SetBool("isAttacking", false);
                        return;
                    }

                    // Periodic attack / bite
                    if (Time.time >= lastAttackTime + attackCooldown)
                    {
                        lastAttackTime = Time.time;
                        PerformAttackOnMob();
                    }
                    return;
                }
                else
                {
                    isAttacking = false;
                    if (animator != null) animator.SetBool("isAttacking", false);
                }
            }

            // Zombie AI (Sight Detection, Gate Barrier & Direct Player Tracking)
            if (team == MobTeam.Enemy)
            {
                if (ZombieRunnerUIManager.Instance != null && !ZombieRunnerUIManager.Instance.IsGameActive)
                {
                    if (animator != null) animator.SetBool("isMoving", false);
                    return;
                }

                if (PlayerRunnerController.Instance != null && !PlayerRunnerController.Instance.IsDead)
                {
                    Vector3 playerPos = PlayerRunnerController.Instance.transform.position;
                    float dz = transform.position.z - playerPos.z;

                    // If the zombie has been left behind the player (player ran past), despawn if too far back.
                    // Otherwise, the zombie should turn around and chase the player forward (+Z).
                    if (dz <= 0.0f)
                    {
                        // Clean up zombies that are far behind the player — they will never catch up
                        if (dz < -18.0f)
                        {
                            if (ObjectPooler.Instance != null && ObjectPooler.Instance.HasPool(PoolTag))
                            {
                                ObjectPooler.Instance.ReturnToPool(PoolTag, gameObject);
                            }
                            else
                            {
                                Destroy(gameObject);
                            }
                            return;
                        }

                        // Zombie is slightly behind — chase the player forward (+Z) instead of going backward
                        float chaseSpeed = moveSpeed;
                        if (PlayerRunnerController.Instance != null)
                        {
                            chaseSpeed *= PlayerRunnerController.Instance.GlobalZombieSpeedMultiplier;
                        }
                        if (EndlessRoadManager.Instance != null && EndlessRoadManager.Instance.IsMoving)
                        {
                            chaseSpeed += EndlessRoadManager.Instance.RoadSpeed;
                        }

                        Vector3 toPlayerDir = (playerPos - transform.position);
                        toPlayerDir.y = 0f;
                        if (toPlayerDir == Vector3.zero) toPlayerDir = Vector3.forward;
                        toPlayerDir = toPlayerDir.normalized;

                        Quaternion chaseRot = Quaternion.LookRotation(toPlayerDir, Vector3.up);
                        transform.rotation = Quaternion.Slerp(transform.rotation, chaseRot, Time.deltaTime * 10.0f);
                        transform.position += toPlayerDir * (chaseSpeed * Time.deltaTime);
                        if (animator != null) animator.SetBool("isMoving", true);
                        return;
                    }

                    // 1. Melee Bite Range
                    float distToPlayer = Vector3.Distance(transform.position, playerPos);
                    if (distToPlayer <= attackRange)
                    {
                        StartAttackingPlayer(PlayerRunnerController.Instance);
                        return;
                    }

                    // 2. Direct Movement Towards Player (Face directly towards approaching player)
                    Vector3 toPlayer = playerPos - transform.position;
                    toPlayer.y = 0f; // Fix Y-axis level so zombies never tilt or flip vertically

                    // Enforce that zombies always advance towards the approaching player along -Z
                    if (toPlayer.z >= -0.1f) toPlayer.z = -0.5f;

                    Vector3 moveDir = toPlayer.normalized;

                    float currentSpeed = moveSpeed;
                    if (PlayerRunnerController.Instance != null)
                    {
                        currentSpeed *= PlayerRunnerController.Instance.GlobalZombieSpeedMultiplier;
                    }

                    // Add road rushing speed so the zombie advances firmly forward on the road surface towards player
                    if (EndlessRoadManager.Instance != null && EndlessRoadManager.Instance.IsMoving)
                    {
                        currentSpeed += EndlessRoadManager.Instance.RoadSpeed;
                    }

                    // Dynamic Obstacle Steer: if an obstacle/container is directly in front, steer around it along X
                    float stepDist = currentSpeed * Time.deltaTime;
                    Vector3 checkOrigin = transform.position + Vector3.up * 0.5f;
                    if (Physics.SphereCast(checkOrigin, 0.28f, moveDir, out RaycastHit obsHit, stepDist + 0.35f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        if (obsHit.collider != null && !obsHit.collider.transform.IsChildOf(transform) && obsHit.collider.gameObject != gameObject)
                        {
                            if (obsHit.collider.GetComponentInParent<MobControl.Level.RoadObstacle>() != null ||
                                obsHit.collider.name.Contains("Obstacle") ||
                                obsHit.collider.name.Contains("Container") ||
                                obsHit.collider.name.Contains("Barricade"))
                            {
                                // Steer left or right towards open road corridor, never freezing
                                float dodgeSign = (transform.position.x >= obsHit.collider.bounds.center.x) ? 1.0f : -1.0f;
                                if (Mathf.Abs(transform.position.x) > 2.5f) dodgeSign = -Mathf.Sign(transform.position.x); // Keep inside road
                                moveDir = new Vector3(dodgeSign * 0.7f, 0f, -0.7f).normalized;
                            }
                        }
                    }

                    // Enforce strictly negative Z movement direction (towards player)
                    if (moveDir.z >= 0f) moveDir.z = -0.1f;
                    moveDir = moveDir.normalized;

                    // Smooth rotation towards movement direction (always facing forward towards player)
                    Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 14.0f);

                    transform.position += moveDir * (currentSpeed * Time.deltaTime);
                    if (animator != null) animator.SetBool("isMoving", true);
                    return;
                }
                else
                {
                    // Fallback when player is dead or not yet instantiated: move along -Z
                    float fallbackSpeed = moveSpeed;
                    if (EndlessRoadManager.Instance != null && EndlessRoadManager.Instance.IsMoving)
                    {
                        fallbackSpeed += EndlessRoadManager.Instance.RoadSpeed;
                    }
                    Quaternion targetRot = Quaternion.LookRotation(Vector3.back, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10.0f);
                    transform.position += Vector3.back * (fallbackSpeed * Time.deltaTime);
                    if (animator != null) animator.SetBool("isMoving", true);
                    return;
                }
            }
            else if (team == MobTeam.Player)
            {
                if (CheckBlockedByGate())
                {
                    if (animator != null) animator.SetBool("isMoving", false);
                    return;
                }

                if (animator != null) animator.SetBool("isMoving", true);
                transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime, Space.World);
            }
        }

        private void StartAttackingPlayer(PlayerRunnerController player)
        {
            if (isDead || player == null || player.IsDead) return;

            isAttacking = true;
            targetPlayer = player;
            lastAttackTime = Time.time - (attackCooldown * 0.5f); // Quick first bite

            if (animator != null)
            {
                animator.SetBool("isAttacking", true);
                animator.SetTrigger("attackTrigger");
            }

            if (team == MobTeam.Enemy && MobControl.Audio.ZombieSoundManager.Instance != null)
            {
                MobControl.Audio.ZombieSoundManager.Instance.PlayZombieGroan();
            }
        }

        private void PerformAttackOnPlayer()
        {
            if (animator != null)
            {
                animator.SetTrigger("attackTrigger");
                animator.SetBool("isAttacking", true);
            }

            if (targetPlayer != null && !targetPlayer.IsDead)
            {
                targetPlayer.TakeDamage(attackDamage);
            }
        }

        private void PerformAttackOnMob()
        {
            if (animator != null)
            {
                animator.SetTrigger("attackTrigger");
                animator.SetBool("isAttacking", true);
            }

            if (targetEnemy != null)
            {
                targetEnemy.TakeDamage(attackDamage);
            }
        }

        private bool CheckBlockedByGate()
        {
            var gates = DestructibleGate.ActiveGates;
            int count = gates.Count;
            if (count == 0) return false;

            Vector3 mobPos = transform.position;

            for (int i = 0; i < count; i++)
            {
                DestructibleGate gate = gates[i];
                if (gate == null || gate.IsBroken) continue;

                Vector3 gatePos = gate.transform.position;
                float dx = Mathf.Abs(mobPos.x - gatePos.x);
                float halfW = (gate.GateWidth > 0f ? gate.GateWidth : 2.3f) * 0.5f + 0.25f;
                if (dx > halfW) continue; // Not blocked if outside the gate width bounds

                if (team == MobTeam.Enemy)
                {
                    // Enemies move towards -Z (from higher Z to lower Z)
                    float dz = mobPos.z - gatePos.z;
                    // Block if zombie is in front of the gate (up to 2.2m ahead) or touching front face
                    if (dz >= -0.2f && dz <= 2.2f)
                    {
                        // Firm stop: do not allow zombie to penetrate through the iron gate mesh
                        if (mobPos.z < gatePos.z + 0.85f)
                        {
                            mobPos.z = gatePos.z + 0.85f;
                            transform.position = mobPos;
                        }
                        return true;
                    }
                }
                else
                {
                    // Player mobs move towards +Z (from lower Z to higher Z)
                    float dz = gatePos.z - mobPos.z;
                    if (dz >= -0.2f && dz <= 2.2f)
                    {
                        if (mobPos.z > gatePos.z - 0.85f)
                        {
                            mobPos.z = gatePos.z - 0.85f;
                            transform.position = mobPos;
                        }
                        return true;
                    }
                }
            }

            return false;
        }

        public void SetStats(float speed, int hp, int baseDamage)
        {
            moveSpeed = speed;
            maxHealth = hp;
            currentHealth = hp;
            attackDamage = baseDamage;

            if (healthBar != null)
            {
                healthBar.Initialize(maxHealth, currentHealth);
            }
        }

        public bool HasPassedGate(int gateInstanceId)
        {
            return passedGateIds.Contains(gateInstanceId);
        }

        public void MarkGatePassed(int gateInstanceId)
        {
            passedGateIds.Add(gateInstanceId);
        }

        public event System.Action<int, int> OnMobHealthChanged;
        public event System.Action<MobUnit> OnMobDied;

        public void TakeDamage(int damage)
        {
            hasBeenAlerted = true;
            currentHealth -= damage;
            OnMobHealthChanged?.Invoke(currentHealth, maxHealth);

            if (healthBar != null)
            {
                healthBar.SetHealth(currentHealth);
            }
            else
            {
                if (cachedUiBar != null)
                {
                    cachedUiBar.UpdateHealth(currentHealth, maxHealth);
                }
                else
                {
                    cachedUiBar = GetComponentInChildren<ThomasDev.HealthSystem.HealthBarUI>(true);
                    if (cachedUiBar != null)
                    {
                        cachedUiBar.UpdateHealth(currentHealth, maxHealth);
                    }
                }

                if (cached3DBar != null)
                {
                    cached3DBar.SetHealth(currentHealth, maxHealth);
                }
            }

            // Lock scale to original base scale so enemies NEVER grow or balloon when shot
            if (hasCachedBaseScale)
            {
                transform.localScale = initialBaseScale;
            }

            // Material color hit flash (no scale changes!)
            if (chibiAnimator != null)
            {
                chibiAnimator.PlayHitFlash();
            }

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;

            OnMobDied?.Invoke(this);

            if (team == MobTeam.Enemy && MobControl.Audio.ZombieSoundManager.Instance != null)
            {
                bool isBoss = maxHealth >= 30 || gameObject.name.Contains("Boss");
                MobControl.Audio.ZombieSoundManager.Instance.PlayZombieDie(isBoss);
            }

            // Hide floating health bar
            if (cachedUiBar != null) cachedUiBar.Hide();
            if (cached3DBar != null) cached3DBar.gameObject.SetActive(false);

            string poolTag = PoolTag;
            if (ObjectPooler.Instance != null && ObjectPooler.Instance.HasPool(poolTag))
            {
                ObjectPooler.Instance.ReturnToPool(poolTag, gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isDead) return;

            // Engaging Player Runner in melee combat / bite
            if (team == MobTeam.Enemy && other.TryGetComponent<PlayerRunnerController>(out PlayerRunnerController runner))
            {
                if (!runner.IsDead)
                {
                    StartAttackingPlayer(runner);
                    return;
                }
            }

            // Engaging enemy mob in melee combat / bite
            if (other.TryGetComponent<MobUnit>(out MobUnit otherMob))
            {
                if (otherMob.Team != this.team && !otherMob.isDead)
                {
                    isAttacking = true;
                    targetEnemy = otherMob;
                    if (animator != null) animator.SetBool("isAttacking", true);
                }
            }

            // Attacking Base
            if (other.TryGetComponent<BaseTarget>(out BaseTarget targetBase))
            {
                if ((team == MobTeam.Player && targetBase.IsEnemyBase) ||
                    (team == MobTeam.Enemy && !targetBase.IsEnemyBase))
                {
                    targetBase.TakeDamage(attackDamage);
                    Die();
                }
            }
        }
    }

    public class MobHealthBillboard : MonoBehaviour
    {
        private Camera cam;

        private void Start()
        {
            cam = Camera.main;
        }

        private void LateUpdate()
        {
            if (cam != null)
            {
                transform.forward = cam.transform.forward;
            }
        }
    }
}
