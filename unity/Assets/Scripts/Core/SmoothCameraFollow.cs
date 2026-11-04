using UnityEngine;

namespace MobControl.Core
{
    public class SmoothCameraFollow : MonoBehaviour
    {
        [Header("Target & Offset")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 5.80f, -6.80f);
        [SerializeField] private float smoothTime = 0.04f;
        [SerializeField] private Vector3 lookRotation = new Vector3(29.0f, 0f, 0f);

        [Header("Stationary Mode Axes Lock")]
        [SerializeField] private bool followX = false; // Kept at 0 so player sweeps visibly across lanes (-X to +X)
        [SerializeField] private bool followZ = false; // Kept fixed for stationary defense

        [Header("Shooting Vibration / Screen Shake")]
        [SerializeField] private float currentShakeIntensity = 0f;
        [SerializeField] private float shakeDecay = 24.0f;

        private Vector3 currentVelocity;

        private void Awake()
        {
            // Elevated top-view camera framing to see incoming enemies across road lanes clearly
            offset = new Vector3(0f, 5.80f, -6.80f);
            lookRotation = new Vector3(29.0f, 0f, 0f);
            followX = false;
            followZ = false;

            Camera cam = GetComponent<Camera>();
            if (cam != null)
            {
                cam.fieldOfView = 60.0f;
            }
        }

        private void Start()
        {
            if (target == null)
            {
                GameObject hero = GameObject.Find("Player_Hero");
                if (hero != null) target = hero.transform;
            }
            transform.position = offset;
            transform.rotation = Quaternion.Euler(lookRotation);
        }

        private void LateUpdate()
        {
            Vector3 desiredPosition;
            if (target != null)
            {
                float targetX = followX ? (target.position.x + offset.x) : offset.x;
                float targetY = offset.y;
                float targetZ = followZ ? (target.position.z + offset.z) : offset.z;
                desiredPosition = new Vector3(targetX, targetY, targetZ);
            }
            else
            {
                desiredPosition = offset;
            }

            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, smoothTime);
            transform.rotation = Quaternion.Euler(lookRotation);

            // Apply high-frequency firing vibration / micro rumble
            if (currentShakeIntensity > 0.001f)
            {
                Vector3 shakeOffset = new Vector3(
                    Random.Range(-1f, 1f) * currentShakeIntensity,
                    Random.Range(-1f, 1f) * currentShakeIntensity * 0.7f,
                    0f
                );
                transform.position += shakeOffset;
                currentShakeIntensity = Mathf.MoveTowards(currentShakeIntensity, 0f, Time.deltaTime * shakeDecay);
            }
        }

        public void TriggerShake(float intensity = 0.038f)
        {
            currentShakeIntensity = Mathf.Max(currentShakeIntensity, intensity);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public void SetCameraParameters(Vector3 newOffset, Vector3 newLookRotation)
        {
            offset = newOffset;
            lookRotation = newLookRotation;
        }
    }
}
