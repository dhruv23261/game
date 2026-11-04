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
        [SerializeField] private float runBobFrequency = 11.0f;
        [SerializeField] private float runBobHeight = 0.048f;
        [SerializeField] private float runSwayAngle = 2.0f;
        [SerializeField] private float bankSpeed = 12.0f;

        [Header("Weapon Recoil")]
        [SerializeField] private float recoilDistance = 0.045f;
        [SerializeField] private float recoilRecoverySpeed = 22.0f;

        [Header("Procedural Running & Leg Walk Animation")]
        [SerializeField] private float walkSpeed = 7.8f;
        [SerializeField] private float maxThighAngle = 25.0f;
        [SerializeField] private float maxKneeBend = 32.0f;
        [SerializeField] private float maxFootPitch = 12.0f;

        private Transform leftUpLeg;
        private Transform rightUpLeg;
        private Transform leftLeg;
        private Transform rightLeg;
        private Transform leftFoot;
        private Transform rightFoot;
        private Animator anim;

        private float walkPhase = 0f;
        private float walkWeight = 0f;

        private Vector3 initialVisualPos;
        private Quaternion initialVisualRot;
        private Vector3 initialWeaponPos;
        private Quaternion initialWeaponRot;
        private float currentBank = 0f;
        private float lastX = 0f;
        private float weaponRecoilZ = 0f;
        private Light muzzleFlashLight;
        private GameObject muzzleFlashVisual;

        public bool IsMoving
        {
            get
            {
                if (runner == null)
                {
                    runner = GetComponentInParent<PlayerRunnerController>();
                    if (runner == null) runner = PlayerRunnerController.Instance;
                }
                if (runner == null || runner.IsDead) return false;
                if (runner.IsBlocked) return false;

                // 1. In stationary runner mode, the character runs forward in place as the road moves
                if (MobControl.Level.EndlessRoadManager.Instance != null && MobControl.Level.EndlessRoadManager.Instance.IsMoving)
                {
                    return true;
                }

                // 2. Active gameplay while not waiting for the initial start swipe
                if (runner.IsControllable && !runner.IsWaitingToStart)
                {
                    return true;
                }

                // 3. Forward motion in normal runner mode
                if (!runner.IsStationary && Mathf.Abs(runner.ForwardSpeed) > 0.1f)
                {
                    return true;
                }

                return false;
            }
        }

        public void Initialize(PlayerRunnerController runnerRef, Transform weaponRef, Transform muzzleRef)
        {
            runner = runnerRef;
            weaponTransform = weaponRef;
            muzzleTransform = muzzleRef;

            initialVisualPos = transform.localPosition;
            initialVisualRot = transform.localRotation;

            if (weaponTransform != null)
            {
                initialWeaponPos = weaponTransform.localPosition != Vector3.zero ? weaponTransform.localPosition : new Vector3(0.20f, 0.78f, 0.22f);
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
                muzzleFlashLight.color = new Color(1.0f, 0.85f, 0.25f); // Golden amber muzzle burst
                muzzleFlashLight.range = 3.8f;
                muzzleFlashLight.intensity = 0f;
                muzzleFlashLight.enabled = false;

                // Subtle realistic golden muzzle burst spark
                muzzleFlashVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                muzzleFlashVisual.name = "FlashBurst_Visual";
                muzzleFlashVisual.transform.SetParent(flashObj.transform, false);
                muzzleFlashVisual.transform.localScale = Vector3.one * 0.06f;
                Collider fc = muzzleFlashVisual.GetComponent<Collider>();
                if (fc != null) Destroy(fc);

                Shader flashShader = Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
                Material flashMat = new Material(flashShader);
                flashMat.color = new Color(1.0f, 0.92f, 0.40f, 0.95f);
                var fmr = muzzleFlashVisual.GetComponent<MeshRenderer>();
                if (fmr != null) fmr.sharedMaterial = flashMat;
                muzzleFlashVisual.SetActive(false);
            }
        }

        private void Start()
        {
            if (runner == null)
            {
                runner = GetComponentInParent<PlayerRunnerController>();
            }
            if (anim == null)
            {
                anim = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
            }
            if (weaponTransform == null && runner != null)
            {
                weaponTransform = runner.transform.Find("AssaultRifle_Meshy");
            }
            if (weaponTransform != null)
            {
                if (initialWeaponPos == Vector3.zero || initialWeaponPos.sqrMagnitude < 0.01f)
                {
                    initialWeaponPos = weaponTransform.localPosition != Vector3.zero ? weaponTransform.localPosition : new Vector3(0.20f, 0.78f, 0.22f);
                }
                initialWeaponRot = weaponTransform.localRotation;
                if (muzzleTransform == null)
                {
                    muzzleTransform = weaponTransform.Find("MuzzleFirePoint");
                }
            }
            if (initialVisualPos == Vector3.zero && transform.localPosition != Vector3.zero)
            {
                initialVisualPos = transform.localPosition;
                initialVisualRot = transform.localRotation;
            }
            lastX = runner != null ? runner.transform.position.x : transform.position.x;
            FindLegBones();
        }

        private void FindLegBones()
        {
            var all = GetComponentsInChildren<Transform>(true);
            foreach (var t in all)
            {
                string n = t.name;
                if (leftUpLeg == null && n.Contains("LeftUpLeg")) leftUpLeg = t;
                else if (rightUpLeg == null && n.Contains("RightUpLeg")) rightUpLeg = t;
                else if (leftLeg == null && (n.Contains("LeftLeg") && !n.Contains("UpLeg"))) leftLeg = t;
                else if (rightLeg == null && (n.Contains("RightLeg") && !n.Contains("UpLeg"))) rightLeg = t;
                else if (leftFoot == null && n.Contains("LeftFoot")) leftFoot = t;
                else if (rightFoot == null && n.Contains("RightFoot")) rightFoot = t;
            }
        }

        private void LateUpdate()
        {
            if (runner == null || runner.IsDead) return;

            if (leftUpLeg == null)
            {
                FindLegBones();
            }

            bool isMoving = IsMoving;
            walkWeight = Mathf.MoveTowards(walkWeight, isMoving ? 1.0f : 0.0f, Time.deltaTime * 6.0f);

            if (walkWeight > 0.001f)
            {
                walkPhase += Time.deltaTime * walkSpeed;

                Vector3 pitchAxis = runner != null ? runner.transform.right : transform.right;

                // Alternate left and right steps
                float leftCycle = Mathf.Sin(walkPhase);
                float rightCycle = -leftCycle;

                // Stride: Swing thigh forward (+) and backward (-)
                float leftThigh = leftCycle * maxThighAngle * walkWeight;
                float rightThigh = rightCycle * maxThighAngle * walkWeight;

                // Knee bend when the leg trails backward to take a step
                float leftKnee = Mathf.Clamp01(-leftCycle) * maxKneeBend * walkWeight;
                float rightKnee = Mathf.Clamp01(-rightCycle) * maxKneeBend * walkWeight;

                // Foot pitch so shoes stay aligned with the road surface
                float leftFootAngle = leftCycle * maxFootPitch * walkWeight;
                float rightFootAngle = rightCycle * maxFootPitch * walkWeight;

                if (leftUpLeg != null)
                {
                    leftUpLeg.rotation = Quaternion.AngleAxis(leftThigh, pitchAxis) * leftUpLeg.rotation;
                }
                if (rightUpLeg != null)
                {
                    rightUpLeg.rotation = Quaternion.AngleAxis(rightThigh, pitchAxis) * rightUpLeg.rotation;
                }
                if (leftLeg != null)
                {
                    leftLeg.rotation = Quaternion.AngleAxis(-leftKnee, pitchAxis) * leftLeg.rotation;
                }
                if (rightLeg != null)
                {
                    rightLeg.rotation = Quaternion.AngleAxis(-rightKnee, pitchAxis) * rightLeg.rotation;
                }
                if (leftFoot != null)
                {
                    leftFoot.rotation = Quaternion.AngleAxis(leftFootAngle, pitchAxis) * leftFoot.rotation;
                }
                if (rightFoot != null)
                {
                    rightFoot.rotation = Quaternion.AngleAxis(rightFootAngle, pitchAxis) * rightFoot.rotation;
                }
            }
        }

        private void Update()
        {
            if (runner == null) return;

            if (runner.IsDead)
            {
                HandleDeathPose();
                return;
            }

            bool isMoving = IsMoving;

            if (anim == null) anim = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
            if (anim != null) anim.SetBool("isMoving", isMoving);

            // 1. Procedural Running Stride Bobbing & Natural Body Sway
            float bobY = 0f;
            float sway = 0f;

            if (walkWeight > 0.001f)
            {
                // Smooth sinusoidal footstep bobbing (2 bob peaks per full 2-step stride)
                bobY = Mathf.Abs(Mathf.Sin(walkPhase)) * runBobHeight * walkWeight;
                sway = Mathf.Sin(walkPhase) * runSwayAngle * walkWeight;
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
                if (initialWeaponPos == Vector3.zero || initialWeaponPos.sqrMagnitude < 0.01f)
                {
                    initialWeaponPos = new Vector3(0.20f, 0.78f, 0.22f);
                }
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
            if (muzzleFlashLight != null)
            {
                muzzleFlashLight.enabled = true;
                muzzleFlashLight.intensity = 3.6f;
            }
            if (muzzleFlashVisual != null) muzzleFlashVisual.SetActive(true);

            yield return new WaitForSeconds(0.045f);

            if (muzzleFlashLight != null)
            {
                muzzleFlashLight.intensity = 0f;
                muzzleFlashLight.enabled = false;
            }
            if (muzzleFlashVisual != null) muzzleFlashVisual.SetActive(false);
        }

        private void HandleDeathPose()
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, initialVisualPos + new Vector3(0f, -0.4f, -0.3f), Time.deltaTime * 6f);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(-65f, 10f, 25f), Time.deltaTime * 6f);
        }
    }
}
