using System.Collections;
using UnityEngine;
using MobControl.Combat;

namespace MobControl.Combat
{
    /// <summary>
    /// Handles procedural running gait, weapon recoil, muzzle flash, and visual dynamics
    /// for the Meshy AI Player Mob Soldier and Assault Rifle.
    /// </summary>
    public class PlayerMobVisualController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerRunnerController runner;
        [SerializeField] private Transform weaponTransform;
        [SerializeField] private Transform muzzleTransform;

        [Header("Gait Tuning")]
        [SerializeField] private float runBobFrequency = 14.0f;
        [SerializeField] private float runBobHeight = 0.038f;
        [SerializeField] private float runSwayAngle = 2.2f;
        [SerializeField] private float bankSpeed = 12.0f;

        [Header("Weapon Recoil")]
        [SerializeField] private float recoilDistance = 0.045f;
        [SerializeField] private float recoilRecoverySpeed = 22.0f;

        private Vector3 initialVisualPos;
        private Quaternion initialVisualRot;
        private Vector3 initialWeaponPos;
        private Quaternion initialWeaponRot;
        private float currentBank = 0f;
        private float lastX = 0f;
        private float weaponRecoilZ = 0f;
        private Light muzzleFlashLight;

        public void Initialize(PlayerRunnerController runnerRef, Transform weaponRef, Transform muzzleRef)
        {
            runner = runnerRef;
            weaponTransform = weaponRef;
            muzzleTransform = muzzleRef;

            initialVisualPos = transform.localPosition;
            initialVisualRot = transform.localRotation;

            if (weaponTransform != null)
            {
                initialWeaponPos = weaponTransform.localPosition;
                initialWeaponRot = weaponTransform.localRotation;
            }

            // Create a small dynamic muzzle flash light at barrel tip
            if (muzzleTransform != null)
            {
                GameObject flashObj = new GameObject("MuzzleFlashLight");
                flashObj.transform.SetParent(muzzleTransform, false);
                flashObj.transform.localPosition = Vector3.zero;

                muzzleFlashLight = flashObj.AddComponent<Light>();
                muzzleFlashLight.type = LightType.Point;
                muzzleFlashLight.color = new Color(1.0f, 0.75f, 0.2f); // Golden amber muzzle burst
                muzzleFlashLight.range = 3.5f;
                muzzleFlashLight.intensity = 0f;
                muzzleFlashLight.enabled = false;
            }
        }

        private void Start()
        {
            if (runner == null)
            {
                runner = GetComponentInParent<PlayerRunnerController>();
            }
            if (initialVisualPos == Vector3.zero && transform.localPosition != Vector3.zero)
            {
                initialVisualPos = transform.localPosition;
                initialVisualRot = transform.localRotation;
            }
            lastX = runner != null ? runner.transform.position.x : transform.position.x;
        }

        private void Update()
        {
            if (runner == null) return;

            if (runner.IsDead)
            {
                HandleDeathPose();
                return;
            }

            // 1. Procedural Running Stride Bobbing & Sway
            bool isMoving = !runner.IsBlocked;
            float bobY = 0f;
            float sway = 0f;

            if (isMoving)
            {
                float t = Time.time * runBobFrequency;
                bobY = Mathf.Abs(Mathf.Sin(t)) * runBobHeight;
                sway = Mathf.Sin(t * 0.5f) * runSwayAngle;
            }

            // 2. Lateral Lane-Change Banking
            float currentX = runner.transform.position.x;
            float deltaX = (currentX - lastX) / Mathf.Max(Time.deltaTime, 0.001f);
            lastX = currentX;

            float targetBank = Mathf.Clamp(-deltaX * 3.5f, -12f, 12f);
            currentBank = Mathf.Lerp(currentBank, targetBank, Time.deltaTime * bankSpeed);

            transform.localPosition = initialVisualPos + new Vector3(0f, bobY, 0f);
            transform.localRotation = initialVisualRot * Quaternion.Euler(isMoving ? 4f : 0f, sway, currentBank);

            // 3. Smooth Weapon Recoil Recovery
            if (weaponTransform != null)
            {
                if (weaponRecoilZ < 0f)
                {
                    weaponRecoilZ = Mathf.MoveTowards(weaponRecoilZ, 0f, Time.deltaTime * recoilRecoverySpeed);
                }
                weaponTransform.localPosition = initialWeaponPos + new Vector3(0f, 0f, weaponRecoilZ);
            }
        }

        /// <summary>
        /// Triggered whenever the assault rifle discharges a bullet.
        /// </summary>
        public void TriggerWeaponFire()
        {
            weaponRecoilZ = -recoilDistance;

            if (muzzleFlashLight != null)
            {
                StopAllCoroutines();
                StartCoroutine(MuzzleFlashRoutine());
            }
        }

        private IEnumerator MuzzleFlashRoutine()
        {
            muzzleFlashLight.enabled = true;
            muzzleFlashLight.intensity = 2.8f;
            yield return new WaitForSeconds(0.045f);
            if (muzzleFlashLight != null)
            {
                muzzleFlashLight.intensity = 0f;
                muzzleFlashLight.enabled = false;
            }
        }

        private void HandleDeathPose()
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, initialVisualPos + new Vector3(0f, -0.4f, -0.3f), Time.deltaTime * 6f);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(-65f, 10f, 25f), Time.deltaTime * 6f);
        }
    }
}
