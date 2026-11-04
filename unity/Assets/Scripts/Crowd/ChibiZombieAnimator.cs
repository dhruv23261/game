using System.Collections;
using UnityEngine;

namespace MobControl.Crowd
{
    public class ChibiZombieAnimator : MonoBehaviour
    {
        [Header("Animation Bones")]
        public Transform bodyRoot;
        public Transform head;
        public Transform leftArm;
        public Transform rightArm;
        public Transform leftLeg;
        public Transform rightLeg;
        public Transform leftFoot;
        public Transform rightFoot;
        public Transform leftEye;
        public Transform rightEye;

        [Header("Animation Speeds & Angles")]
        public float animSpeed = 5.2f;
        public float legSwingAngle = 36f;
        public float armSwayAngle = 14f;
        public float headBobAmount = 0.035f;
        public float bodyWobbleAngle = 4.5f;

        private float walkPhase = 0f;
        private Vector3 initialBodyPos;
        private Quaternion initialLeftArmRot;
        private Quaternion initialRightArmRot;
        private Renderer[] allRenderers;
        private MaterialPropertyBlock propBlock;
        private MobUnit mobUnit;
        private bool isDying = false;

        private void Awake()
        {
            if (bodyRoot != null) initialBodyPos = bodyRoot.localPosition;
            if (leftArm != null) initialLeftArmRot = leftArm.localRotation;
            if (rightArm != null) initialRightArmRot = rightArm.localRotation;

            allRenderers = GetComponentsInChildren<Renderer>(true);
            propBlock = new MaterialPropertyBlock();
            mobUnit = GetComponent<MobUnit>();

            // Randomize phase so zombies in a wave don't march in robotic unison
            walkPhase = Random.Range(0f, 10f);
        }

        private void Update()
        {
            if (isDying) return;
            if (mobUnit != null && mobUnit.IsDead) return;

            walkPhase += Time.deltaTime * animSpeed;

            // 1. Leg strides (alternating forward reach and backward push)
            float sinP = Mathf.Sin(walkPhase);
            float cosP = Mathf.Cos(walkPhase);

            // In Unity, negative local X rotation reaches leg forward (+Z), positive pushes backward (-Z)
            if (leftLeg != null)
                leftLeg.localRotation = Quaternion.Euler(-sinP * legSwingAngle, 0f, 0f);

            if (rightLeg != null)
                rightLeg.localRotation = Quaternion.Euler(sinP * legSwingAngle, 0f, 0f);

            // 2. Body vertical bounce & forward charging lean towards approaching player
            if (bodyRoot != null)
            {
                float bounce = Mathf.Abs(sinP) * headBobAmount;
                bodyRoot.localPosition = initialBodyPos + new Vector3(0f, bounce, 0.02f);
                bodyRoot.localRotation = Quaternion.Euler(5.5f, sinP * 2.5f, sinP * bodyWobbleAngle);
            }

            // 3. Head cartoon bobbing forward into each step
            if (head != null)
            {
                head.localRotation = Quaternion.Euler(
                    Mathf.Abs(cosP) * 5f - 2f,
                    sinP * 4f,
                    -sinP * 2.5f
                );
            }

            // 4. Arms reaching forward in aggressive cartoon zombie charge
            // Opposite arm and leg swing together for natural walking rhythm:
            // When Left Leg swings forward (-sinP), Right Arm sways forward (-sinP)
            // When Right Leg swings forward (+sinP), Left Arm sways forward (+sinP)
            if (leftArm != null)
            {
                leftArm.localRotation = initialLeftArmRot * Quaternion.Euler(
                    sinP * armSwayAngle,
                    0f,
                    cosP * 3f
                );
            }

            if (rightArm != null)
            {
                rightArm.localRotation = initialRightArmRot * Quaternion.Euler(
                    -sinP * armSwayAngle,
                    0f,
                    -cosP * 3f
                );
            }
        }

        public void PlayHitFlash()
        {
            if (!gameObject.activeInHierarchy) return;
            StartCoroutine(HitFlashRoutine());
        }

        private IEnumerator HitFlashRoutine()
        {
            if (allRenderers == null || allRenderers.Length == 0) yield break;

            // Flash bright red-white
            for (int i = 0; i < allRenderers.Length; i++)
            {
                if (allRenderers[i] == null) continue;
                allRenderers[i].GetPropertyBlock(propBlock);
                propBlock.SetColor("_Color", new Color(1f, 0.35f, 0.35f, 1f));
                allRenderers[i].SetPropertyBlock(propBlock);
            }

            yield return new WaitForSeconds(0.08f);

            // Reset
            for (int i = 0; i < allRenderers.Length; i++)
            {
                if (allRenderers[i] == null) continue;
                allRenderers[i].SetPropertyBlock(null);
            }
        }
    }
}
