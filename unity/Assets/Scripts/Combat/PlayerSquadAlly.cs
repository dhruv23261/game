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
        private GameObject equippedWeaponInstance;
        private Transform muzzleFirePoint;

        public void Initialize(PlayerRunnerController leaderRef, Vector3 offset, GameObject bulletRef, Vector3 muzzleOff, float rate)
        {
            leader = leaderRef;
            formationOffset = offset;
            bulletPrefab = bulletRef;
            muzzleOffset = muzzleOff;
            fireRate = rate;

            animator = GetComponent<Animator>();
            mobUnit = GetComponent<MobUnit>();

            Material mat = Resources.Load<Material>("Materials/PlayerMaterial");
#if UNITY_EDITOR
            if (mat == null) mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/PlayerMaterial.mat");
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

            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            // Equip authentic weapon on ally
            Transform rightHand = PlayerRunnerController.FindRightHandBone(transform);
            Transform mount = rightHand != null ? rightHand : transform;

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
                // Reset root transform to identity so the prefab's internal calibrated grip is cleanly held in hand
                existingGun.localPosition = Vector3.zero;
                existingGun.localRotation = Quaternion.identity;
                existingGun.localScale = Vector3.one;
                equippedWeaponInstance = existingGun.gameObject;
                muzzleFirePoint = existingGun.Find("MuzzleFirePoint");
            }
            else
            {
                equippedWeaponInstance = PlayerRunnerController.BuildStylizedAssaultRifle(mount, out muzzleFirePoint, rightHand != null);
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

            Vector3 spawnPos = muzzleFirePoint != null 
                ? muzzleFirePoint.position 
                : (transform.position + (transform.right * muzzleOffset.x) + (transform.up * muzzleOffset.y) + (transform.forward * muzzleOffset.z));

            if (ObjectPooler.Instance != null)
            {
                ObjectPooler.Instance.SpawnFromPool("Bullet", spawnPos, Quaternion.identity);
            }
            else
            {
                Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
            }

            if (equippedWeaponInstance != null)
            {
                StartCoroutine(WeaponRecoilPunch(equippedWeaponInstance.transform));
            }
        }

        private System.Collections.IEnumerator WeaponRecoilPunch(Transform gunTr)
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

        private void OnDestroy()
        {
            if (leader != null)
            {
                leader.RemoveSquadAlly(this);
            }
        }
    }
}
