using UnityEngine;

namespace MobControl.Core
{
    public class FreeLookCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

#pragma warning disable 0414
        [Header("Distance & Zoom")]
        [SerializeField] private float defaultDistance = 9.2f;
        [SerializeField] private float minDistance = 4.0f;
        [SerializeField] private float maxDistance = 20.0f;
        [SerializeField] private float zoomSpeed = 4.0f;

        [Header("Rotation & Angle")]
        [SerializeField] private float defaultPitch = 30.0f; // Elevated 30-deg runner angle gives clear road & squad overview
        [SerializeField] private float mouseSensitivity = 3.0f;
        [SerializeField] private float smoothTime = 0.08f;
        [SerializeField] private float minPitch = 10.0f;
        [SerializeField] private float maxPitch = 60.0f;
#pragma warning restore 0414

        [Header("Height Offset")]
        [SerializeField] private float targetHeightOffset = 1.6f;

        private float currentYaw = 0.0f;
        private float currentPitch = 30.0f;
        private float currentDistance;
        private Vector3 currentVelocity;

        private void Start()
        {
            currentDistance = defaultDistance;
            currentYaw = 0.0f;
            currentPitch = defaultPitch;

            if (target == null)
            {
                // Auto-find PlayerMob if not assigned
                GameObject player = GameObject.Find("PlayerMob");
                if (player != null) target = player.transform;
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

#if UNITY_EDITOR
            // 1. Mouse Orbit in Editor (Hold Right Click to orbit camera 360 for debugging)
            if (Input.GetMouseButton(1))
            {
                currentYaw += Input.GetAxis("Mouse X") * mouseSensitivity * 100f * Time.deltaTime;
                currentPitch -= Input.GetAxis("Mouse Y") * mouseSensitivity * 100f * Time.deltaTime;
                currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
            }
            else
            {
                // Smoothly return yaw to straight forward runner angle
                currentYaw = Mathf.Lerp(currentYaw, 0f, 10f * Time.deltaTime);
            }

            // 2. Mouse Scroll Zoom
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                currentDistance = Mathf.Clamp(currentDistance - scroll * zoomSpeed, minDistance, maxDistance);
            }
#else
            currentYaw = 0f;
#endif

            // 3. Compute Stable 3rd Person Runner Position
            // Subtle horizontal tracking (30%) keeps the road firmly centered without camera tilt
            float camX = target.position.x * 0.30f;
            Vector3 focusPoint = new Vector3(camX, target.position.y + targetHeightOffset, target.position.z);
            Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
            Vector3 targetPosition = focusPoint - (rotation * Vector3.forward * currentDistance);

            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);
            
            // Fixed forward orientation eliminates all sideways yawing and motion sickness
            transform.rotation = rotation;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}
