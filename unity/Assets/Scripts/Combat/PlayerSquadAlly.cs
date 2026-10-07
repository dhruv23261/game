using UnityEngine;
using MobControl.Crowd;
using MobControl.Core;

namespace MobControl.Combat
{
    public class PlayerSquadAlly : MonoBehaviour
    {
        private PlayerRunnerController leader;
        private Vector3 formationOffset;
        private Animator animator;
        private float fireTimer = 0f;
        private float fireRate = 0.12f;
        private Vector3 muzzleOffset = new Vector3(0.24f, 1.35f, 0.95f);
        private GameObject bulletPrefab;
        private MobUnit mobUnit;
        private bool isInitialized = false;

        public void Initialize(PlayerRunnerController leaderRef, Vector3 offset, GameObject bulletRef, Vector3 muzzleOff, float rate)
        {
            leader = leaderRef;
            formationOffset = offset;
            bulletPrefab = bulletRef;
            muzzleOffset = muzzleOff;
            fireRate = rate;

            animator = GetComponent<Animator>();
            mobUnit = GetComponent<MobUnit>();

            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            if (animator != null)
            {
                animator.SetBool("isMoving", true);
            }

            isInitialized = true;
        }

        private void Update()
        {
            if (!isInitialized || leader == null || leader.IsDead)
            {
                if (animator != null) animator.SetBool("isMoving", false);
                return;
            }

            // 1. Maintain formation position relative to player runner
            Vector3 targetPos = leader.transform.position + formationOffset;
            targetPos.x = Mathf.Clamp(targetPos.x, -2.4f, 2.4f);
            
            // Ultra-responsive, smooth frame-rate independent tracking
            Vector3 curPos = transform.position;
            curPos.z = targetPos.z;
            curPos.y = targetPos.y;
            curPos.x = Mathf.Lerp(curPos.x, targetPos.x, 1f - Mathf.Exp(-24.0f * Time.deltaTime));
            transform.position = curPos;

            // 2. Synchronize running / idle animation with leader
            bool shouldMove = !leader.IsBlocked;
            if (animator != null)
            {
                animator.SetBool("isMoving", shouldMove);
            }

            // 3. Parallel automatic shooting forward along lane
            if (shouldMove)
            {
                fireTimer += Time.deltaTime;
                if (fireTimer >= fireRate)
                {
                    fireTimer = 0f;
                    ShootBullet();
                }
            }
        }

        private void ShootBullet()
        {
            if (bulletPrefab == null) return;

            Vector3 spawnPos = transform.position 
                + (transform.right * muzzleOffset.x) 
                + (transform.up * muzzleOffset.y) 
                + (transform.forward * muzzleOffset.z);

            if (ObjectPooler.Instance != null)
            {
                ObjectPooler.Instance.SpawnFromPool("Bullet", spawnPos, Quaternion.identity);
            }
            else
            {
                Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
            }
        }

        private void OnDestroy()
        {
            if (leader != null)
            {
                leader.RemoveSquadAlly(this);
            }
        }
    }
}
