using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MobControl.Crowd;
using MobControl.Gates;
using MobControl.Core;

namespace MobControl.Combat
{
    public class BulletProjectile : MonoBehaviour
    {
        [Header("Bullet Settings")]
        [SerializeField] private float speed = 48.0f;
        [SerializeField] private int damage = 3;
        [SerializeField] private float maxDistance = 75.0f;
        [SerializeField] private float lifeTime = 2.0f;
        [Header("Bullet Visuals")]
        [SerializeField] private bool overrideVisualScale = false;
        [SerializeField] private Vector3 bulletVisualScale = new Vector3(0.14f, 0.38f, 0.14f);

        private const string POOL_TAG = "Bullet";
        private const float BASE_RANGE = 30.0f;
        private const float BASE_LIFETIME = 0.5f;
        private Vector3 spawnPosition;
        private float spawnTime;
        private float distanceTraveled = 0f;
        private HashSet<int> passedGateIds = new HashSet<int>();
        private bool isAlive = false;

        private static Material s_cachedTracerMat;

        private void Awake()
        {
            EnsureTracerMaterial();
            EnsureVisualOrientation();
        }

        private void EnsureVisualOrientation()
        {
            Transform vChild = transform.Find("Visual_9mmBullet");
            if (vChild != null)
            {
                vChild.localRotation = Quaternion.identity;
                vChild.localPosition = new Vector3(0f, 0f, 0.11f);
            }
        }

        private void EnsureTracerMaterial()
        {
            TrailRenderer tr = GetComponent<TrailRenderer>();
            if (tr == null) return;

            if (tr.sharedMaterial == null || tr.sharedMaterial.shader == null || tr.sharedMaterial.shader.name == "Hidden/InternalErrorShader")
            {
                if (s_cachedTracerMat == null)
                {
                    Shader s = Shader.Find("ZombieSquad/BulletTracerTrail")
                        ?? Shader.Find("Universal Render Pipeline/Unlit")
                        ?? Shader.Find("Mobile/Particles/Additive")
                        ?? Shader.Find("Unlit/Color");

                    s_cachedTracerMat = new Material(s);
                    s_cachedTracerMat.name = "Runtime_TracerMat";
                    s_cachedTracerMat.color = Color.white;
                    s_cachedTracerMat.SetColor("_TintColor", new Color(1.0f, 0.88f, 0.38f, 1.0f));
                    s_cachedTracerMat.SetFloat("_Intensity", 2.2f);
#if UNITY_EDITOR
                    Texture2D tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/tex_bullet_tracer_strip.png");
                    if (tex != null) s_cachedTracerMat.mainTexture = tex;
#endif
                }
                tr.sharedMaterial = s_cachedTracerMat;
            }
        }

        // Distinctive Bullet Colors:
        // During Gun Boost: Glowing Cyber Purple (#CD29FF)
        // Default / Post-Boost: Classic Golden Yellow (#FFE060)
        private static readonly Color PurpleBoostColor = new Color(0.80f, 0.16f, 1.0f, 1.0f);
        private static readonly Color DefaultGoldColor = new Color(1.0f, 0.88f, 0.38f, 1.0f);

        private void OnEnable()
        {
            EnsureTracerMaterial();
            EnsureVisualOrientation();
            spawnPosition = transform.position;
            spawnTime = Time.time;
            distanceTraveled = 0f;
            passedGateIds.Clear();
            isAlive = true;

            // Check if active Gun Boost is running
            bool isUpgraded = (PlayerRunnerController.Instance != null && PlayerRunnerController.Instance.HasGunUpgrade);
            Color bulletColor = isUpgraded ? PurpleBoostColor : DefaultGoldColor;
            speed = isUpgraded ? 58.0f : 48.0f; // Rapid laser velocity for upgraded gun

            TrailRenderer tr = GetComponent<TrailRenderer>();
            if (tr != null)
            {
                tr.Clear();
                tr.startColor = bulletColor;
                tr.endColor = new Color(bulletColor.r, bulletColor.g, bulletColor.b, 0.04f);
                tr.time = isUpgraded ? 0.13f : 0.08f;
                tr.startWidth = isUpgraded ? 0.22f : 0.15f;
                tr.endWidth = 0.02f;
            }

            // Tint the 3D bullet mesh itself with glowing neon emission
            Renderer bulletRend = GetComponentInChildren<Renderer>();
            if (bulletRend != null)
            {
                MaterialPropertyBlock mpb = new MaterialPropertyBlock();
                bulletRend.GetPropertyBlock(mpb);
                mpb.SetColor("_Color", bulletColor);
                mpb.SetColor("_BaseColor", bulletColor);
                mpb.SetColor("_EmissionColor", bulletColor * (isUpgraded ? 2.5f : 1.2f));
                bulletRend.SetPropertyBlock(mpb);
            }

            // Base bullet damage = 1 so exact bullet counts match design:
            // Green Zombie = 5 bullets, Red Zombie = 8 bullets, Container = 30 bullets, Boss = 60 bullets
            // + SOLDIER DAMAGE upgrade (rolled per bullet)
            damage = PlayerRunnerController.RollBulletDamage();

            // Standardized reach & speed: guaranteed to reach any visible gate or enemy rapidly
            string weapon = PlayerPrefs.GetString("ZombieSquad_SelectedWeapon", "M41D");
            float rangeBonus = PlayerPrefs.GetInt($"ZombieSquad_Wep_{weapon}_Range", 0) * 4.0f;
            // Short reach on purpose: zombies spawn ~42m+ ahead, so they must walk in close before bullets can kill them.
            // Set fresh every spawn — pooled bullets used to stack the range bonus on each reuse.
            maxDistance = BASE_RANGE + rangeBonus;
            lifeTime = Mathf.Max(BASE_LIFETIME, maxDistance / Mathf.Max(1f, speed) + 0.1f);

            if (overrideVisualScale)
            {
                transform.localScale = bulletVisualScale;
            }
        }

        private void OnDisable()
        {
            isAlive = false;
        }

        private static readonly RaycastHit[] s_sweepHits = new RaycastHit[12];

        private void Update()
        {
            if (!isAlive) return;

            float stepDistance = speed * Time.deltaTime;
            Vector3 currentPos = transform.position;
            Vector3 moveDir = Vector3.forward;

            // 0.22m radius sweep check (accurate bullet trajectory down active lane)
            int hitCount = Physics.SphereCastNonAlloc(currentPos, 0.22f, moveDir, s_sweepHits, stepDistance, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = s_sweepHits[i];
                if (hit.collider == null) continue;
                if (hit.collider.CompareTag("Player") || 
                    hit.collider.GetComponentInParent<PlayerSquadAlly>() != null || 
                    (PlayerRunnerController.Instance != null && hit.collider.transform.IsChildOf(PlayerRunnerController.Instance.transform))) 
                    continue;

                // 1. Hit Enemy Mob
                MobUnit enemyMob = hit.collider.GetComponentInParent<MobUnit>();
                if (enemyMob != null && enemyMob.Team == MobTeam.Enemy && enemyMob.Health > 0)
                {
                    enemyMob.TakeDamage(damage);
                    MobControl.Audio.ZombieSoundManager.Instance?.PlayBulletHit(hit.point != Vector3.zero ? hit.point : enemyMob.transform.position);
                    ReturnToPool();
                    return;
                }

                // 1b. Hit Level 2 Emerging Plant Monster
                var plantMonster = hit.collider.GetComponentInParent<MobControl.Creatures.EmergingPlantMonster>();
                if (plantMonster != null)
                {
                    plantMonster.TakeDamage(damage * 10f);
                    MobControl.Audio.ZombieSoundManager.Instance?.PlayBulletHit(hit.point != Vector3.zero ? hit.point : plantMonster.transform.position);
                    ReturnToPool();
                    return;
                }

                // 1c. Hit Level 2 Flying Wyvern Dragon
                var flyingWyvern = hit.collider.GetComponentInParent<MobControl.Creatures.FlyingWyvernAirThreat>();
                if (flyingWyvern != null)
                {
                    flyingWyvern.TakeDamage(damage * 10f);
                    MobControl.Audio.ZombieSoundManager.Instance?.PlayBulletHit(hit.point != Vector3.zero ? hit.point : flyingWyvern.transform.position);
                    ReturnToPool();
                    return;
                }

                // 2. Hit Destructible Gate
                DestructibleGate dGate = hit.collider.GetComponentInParent<DestructibleGate>();
                if (dGate != null && !dGate.IsBroken)
                {
                    dGate.TakeDamage(damage);
                    ReturnToPool();
                    return;
                }

                // 3. Pass through Multiplier Gate
                MultiplierGate gate = hit.collider.GetComponentInParent<MultiplierGate>();
                if (gate != null)
                {
                    int gateId = gate.GateInstanceId;
                    if (!passedGateIds.Contains(gateId))
                    {
                        passedGateIds.Add(gateId);
                        int count = (gate.Operation == GateOperation.Multiply) ? Mathf.Max(1, gate.Value - 1) : gate.Value;
                        SpawnClonedBullets(gateId, count);
                    }
                    continue;
                }

                // 4. Hit Solid Obstacle (Container, Barricade, or non-trigger obstacle) - Stop and Destroy!
                bool isGroundOrRoad = hit.collider.name.IndexOf("Road", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                      hit.collider.name.IndexOf("Ground", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                      hit.collider.name.IndexOf("Floor", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                      hit.collider.name.IndexOf("Plane", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                      hit.collider.name.IndexOf("Terrain", System.StringComparison.OrdinalIgnoreCase) >= 0;

                if (!isGroundOrRoad && (
                    hit.collider.GetComponentInParent<MobControl.Level.RoadObstacle>() != null ||
                    hit.collider.name.Contains("Barricade") ||
                    hit.collider.name.Contains("Container") ||
                    hit.collider.name.Contains("Obstacle") ||
                    (!hit.collider.isTrigger && hit.collider.GetComponentInParent<MobUnit>() == null &&
                     hit.collider.GetComponentInParent<MobControl.Creatures.EmergingPlantMonster>() == null &&
                     hit.collider.GetComponentInParent<MobControl.Creatures.FlyingWyvernAirThreat>() == null)))
                {
                    ReturnToPool();
                    return;
                }
            }

            transform.position = currentPos + moveDir * stepDistance;
            distanceTraveled += stepDistance;

            // Return to pool when beyond range or lifetime
            if (distanceTraveled >= maxDistance || Time.time >= spawnTime + lifeTime)
            {
                ReturnToPool();
            }
        }

        private void ReturnToPool()
        {
            if (!isAlive) return;
            isAlive = false;
            if (ObjectPooler.Instance != null)
                ObjectPooler.Instance.ReturnToPool(POOL_TAG, gameObject);
            else
                Destroy(gameObject);
        }

        private void OnCollisionEnter(Collision collision)
        {
            HandleTriggerOrCollision(collision.collider);
        }

        private void OnTriggerEnter(Collider other)
        {
            HandleTriggerOrCollision(other);
        }

        private void HandleTriggerOrCollision(Collider other)
        {
            if (!isAlive || other == null) return;
            if (other.CompareTag("Player") || 
                other.GetComponentInParent<PlayerSquadAlly>() != null || 
                (PlayerRunnerController.Instance != null && other.transform.IsChildOf(PlayerRunnerController.Instance.transform))) 
                return;

            // 1. Hit Enemy Zombie
            MobUnit enemyMob = other.GetComponentInParent<MobUnit>();
            if (enemyMob != null && enemyMob.Team == MobTeam.Enemy && enemyMob.Health > 0)
            {
                enemyMob.TakeDamage(damage);
                ReturnToPool();
                return;
            }

            // 1b. Hit Level 2 Emerging Plant Monster
            var plantMonster = other.GetComponentInParent<MobControl.Creatures.EmergingPlantMonster>();
            if (plantMonster != null)
            {
                plantMonster.TakeDamage(damage * 10f);
                ReturnToPool();
                return;
            }

            // 1c. Hit Level 2 Flying Wyvern Dragon
            var flyingWyvern = other.GetComponentInParent<MobControl.Creatures.FlyingWyvernAirThreat>();
            if (flyingWyvern != null)
            {
                flyingWyvern.TakeDamage(damage * 10f);
                ReturnToPool();
                return;
            }

            // 2. Hit Destructible Gate
            DestructibleGate dGate = other.GetComponentInParent<DestructibleGate>();
            if (dGate != null && !dGate.IsBroken)
            {
                dGate.TakeDamage(damage);
                ReturnToPool();
                return;
            }

            // 3. Pass through Multiplier Gate
            MultiplierGate gate = other.GetComponentInParent<MultiplierGate>();
            if (gate != null)
            {
                int gateId = gate.GateInstanceId;
                if (!passedGateIds.Contains(gateId))
                {
                    passedGateIds.Add(gateId);
                    int count = (gate.Operation == GateOperation.Multiply) ? Mathf.Max(1, gate.Value - 1) : gate.Value;
                    SpawnClonedBullets(gateId, count);
                }
                return;
            }

            // 4. Hit Solid Obstacle (Container, Barricade, or non-trigger obstacle) - Stop and Destroy!
            bool isGroundOrRoad = other.name.IndexOf("Road", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                  other.name.IndexOf("Ground", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                  other.name.IndexOf("Floor", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                  other.name.IndexOf("Plane", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                  other.name.IndexOf("Terrain", System.StringComparison.OrdinalIgnoreCase) >= 0;

            if (!isGroundOrRoad && (
                other.GetComponentInParent<MobControl.Level.RoadObstacle>() != null ||
                other.name.Contains("Barricade") ||
                other.name.Contains("Container") ||
                other.name.Contains("Obstacle") ||
                (!other.isTrigger && other.GetComponentInParent<MobUnit>() == null)))
            {
                ReturnToPool();
                return;
            }
        }

        private void SpawnClonedBullets(int gateId, int extraCount = 1)
        {
            // Shift original bullet slightly to left for twin-shot formation
            transform.position += new Vector3(-0.14f, 0f, 0f);

            for (int i = 0; i < extraCount; i++)
            {
                float xOffset = 0.28f * (i + 1);
                Vector3 clonePos = transform.position + new Vector3(xOffset, 0f, 0f);
                GameObject clone = null;
                if (ObjectPooler.Instance != null)
                    clone = ObjectPooler.Instance.SpawnFromPool(POOL_TAG, clonePos, transform.rotation);
                else
                    clone = Instantiate(gameObject, clonePos, transform.rotation);

                if (clone != null && clone.TryGetComponent<BulletProjectile>(out BulletProjectile cloneBullet))
                {
                    cloneBullet.passedGateIds.Add(gateId);
                }
            }
        }
    }
}
