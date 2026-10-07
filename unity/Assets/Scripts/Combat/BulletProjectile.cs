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
        [SerializeField] private float speed = 42.0f;
        [SerializeField] private int damage = 1;
        [SerializeField] private float maxDistance = 52.0f;
        [SerializeField] private float lifeTime = 1.8f;
        [Header("Bullet Visuals")]
        [SerializeField] private bool overrideVisualScale = false;
        [SerializeField] private Vector3 bulletVisualScale = new Vector3(0.14f, 0.38f, 0.14f);

        private const string POOL_TAG = "Bullet";
        private Vector3 spawnPosition;
        private float spawnTime;
        private float distanceTraveled = 0f;
        private HashSet<int> passedGateIds = new HashSet<int>();
        private bool isAlive = false;

        private void OnEnable()
        {
            spawnPosition = transform.position;
            spawnTime = Time.time;
            distanceTraveled = 0f;
            passedGateIds.Clear();
            isAlive = true;

            // Standardized reach & speed: guaranteed to reach any visible gate or enemy
            if (maxDistance < 50.0f) maxDistance = 52.0f;
            if (speed < 40.0f) speed = 42.0f;
            if (lifeTime < 1.6f) lifeTime = 1.8f;

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

            // Continuous SphereCast sweep check with generous 0.28m radius
            // Ensures reliable hit registration along bullet's lane without phantom collisions across lanes
            int hitCount = Physics.SphereCastNonAlloc(currentPos, 0.28f, moveDir, s_sweepHits, stepDistance, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = s_sweepHits[i];
                if (hit.collider == null) continue;

                // Hit Enemy Mob
                MobUnit enemyMob = hit.collider.GetComponentInParent<MobUnit>();
                if (enemyMob != null && enemyMob.Team == MobTeam.Enemy && enemyMob.Health > 0)
                {
                    enemyMob.TakeDamage(damage);
                    ReturnToPool();
                    return;
                }

                // Hit Destructible Gate
                DestructibleGate dGate = hit.collider.GetComponentInParent<DestructibleGate>();
                if (dGate != null && !dGate.IsBroken)
                {
                    dGate.TakeDamage(damage);
                    ReturnToPool();
                    return;
                }

                // Pass through Multiplier Gate
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

        private void OnTriggerEnter(Collider other)
        {
            if (!isAlive) return;

            // Hit Enemy Zombie
            MobUnit enemyMob = other.GetComponentInParent<MobUnit>();
            if (enemyMob != null && enemyMob.Team == MobTeam.Enemy && enemyMob.Health > 0)
            {
                enemyMob.TakeDamage(damage);
                ReturnToPool();
                return;
            }

            // Hit Destructible Gate
            DestructibleGate dGate = other.GetComponentInParent<DestructibleGate>();
            if (dGate != null && !dGate.IsBroken)
            {
                dGate.TakeDamage(damage);
                ReturnToPool();
                return;
            }

            // Pass through Multiplier Gate
            MultiplierGate gate;
            if (other.TryGetComponent<MultiplierGate>(out gate))
            {
                int gateId = gate.GateInstanceId;
                if (!passedGateIds.Contains(gateId))
                {
                    passedGateIds.Add(gateId);
                    int count = (gate.Operation == GateOperation.Multiply) ? Mathf.Max(1, gate.Value - 1) : gate.Value;
                    SpawnClonedBullets(gateId, count);
                }
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
