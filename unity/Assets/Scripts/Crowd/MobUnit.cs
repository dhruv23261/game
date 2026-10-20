using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MobControl.Core;
using MobControl.Base;
using MobControl.UI;
using MobControl.Combat;

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
        private static readonly WaitForSeconds s_hitFlashWait = new WaitForSeconds(0.08f);

        public MobTeam Team => team;
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
            animator = GetComponent<Animator>();
            if (animator != null)
            {
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            }
            runnerController = GetComponent<PlayerRunnerController>();

            // Auto-ensure collider bounds cover the zombie's full body from feet to head
            BoxCollider col = GetComponent<BoxCollider>();
            if (col != null)
            {
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
        }

        private void OnEnable()
        {
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

            // Zombie AI (Sight Detection, Gate Barrier & Lane Homing)
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

                // If the player runner has safely run past this zombie, stop the zombie
                if (dz < -2.0f)
                {
                    if (animator != null) animator.SetBool("isMoving", false);
                    return;
                }

                // 1. Sight Activation: Don't move until player comes within visible mobile screen range (or alerted by bullet)
                if (!hasBeenAlerted && dz > activationSightDistance)
                {
                    if (animator != null) animator.SetBool("isMoving", false);
                    return;
                }

                // 2. Gate Barrier: Stop behind unbroken Destructible Gates
                if (CheckBlockedByGate())
                {
                    if (animator != null) animator.SetBool("isMoving", false);
                    return;
                }

                if (animator != null) animator.SetBool("isMoving", true);

                // 3. Close Range Homing: Smoothly steer X towards player's lane when within 8m
                if (dz > -0.5f && dz <= homingTriggerDistance)
                {
                    float proximityWeight = Mathf.Clamp01(1.0f - (dz / homingTriggerDistance));
                    float currentSteerRate = Mathf.Lerp(0.8f, steerSpeed, proximityWeight);

                    Vector3 pos = transform.position;
                    pos.x = Mathf.MoveTowards(pos.x, playerPos.x, currentSteerRate * Time.deltaTime);
                    transform.position = pos;
                }

                // 4. Melee Bite Range
                float distToPlayer = Vector3.Distance(transform.position, playerPos);
                if (distToPlayer <= attackRange)
                {
                    StartAttackingPlayer(PlayerRunnerController.Instance);
                    return;
                }
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
            }

            // Normal forward movement along track
            Vector3 direction = team == MobTeam.Player ? Vector3.forward : Vector3.back;
            float currentSpeed = moveSpeed;
            if (team == MobTeam.Enemy && PlayerRunnerController.Instance != null)
            {
                currentSpeed *= PlayerRunnerController.Instance.GlobalZombieSpeedMultiplier;
            }
            transform.Translate(direction * currentSpeed * Time.deltaTime, Space.World);
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
                if (dx > 1.35f) continue; // In another lane, not blocked by this gate

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

            // Small hit punch scale effect
            StartCoroutine(HitFlashEffect());

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private IEnumerator HitFlashEffect()
        {
            Vector3 origScale = transform.localScale;
            transform.localScale = origScale * 1.15f;
            yield return s_hitFlashWait;
            transform.localScale = origScale;
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;

            // Hide floating health bar
            if (cachedUiBar != null) cachedUiBar.Hide();
            if (cached3DBar != null) cached3DBar.gameObject.SetActive(false);

            string poolTag = team == MobTeam.Player ? "PlayerMob" : "EnemyMob";
            if (ObjectPooler.Instance != null)
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
