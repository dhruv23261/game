using UnityEngine;

namespace MobControl.Core
{
    public class SmoothCameraFollow : MonoBehaviour
    {
        [Header("Target & Offset")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0, 3.5f, -6.0f);
        [SerializeField] private float smoothTime = 0.05f;
        [SerializeField] private Vector3 lookRotation = new Vector3(20f, 0f, 0f);

        private Vector3 currentVelocity;

        private void LateUpdate()
        {
            if (target != null)
            {
                Vector3 desiredPosition = target.position + offset;
                transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, smoothTime);
                transform.rotation = Quaternion.Euler(lookRotation);
            }
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}
