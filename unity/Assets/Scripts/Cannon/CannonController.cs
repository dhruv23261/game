using UnityEngine;
using MobControl.Core;

namespace MobControl.Cannon
{
    public class CannonController : MonoBehaviour
    {
        [Header("Aiming & Movement")]
        [SerializeField] private float horizontalLimit = 4.0f;
        [SerializeField] private float smoothSpeed = 15.0f;
        [SerializeField] private Transform firePoint;

        [Header("Shooting Settings")]
        [SerializeField] private float baseFireRate = 0.15f; // Seconds between shots
        [SerializeField] private bool autoShoot = true;

        private float fireTimer = 0f;
        private Camera mainCam;
        private float targetX = 0f;
        private bool isControllable = true;
        private float currentFireRate;

        private void Start()
        {
            mainCam = Camera.main;
            currentFireRate = baseFireRate;
        }

        public void SetFireRate(float rate)
        {
            currentFireRate = Mathf.Max(0.04f, rate);
        }

        public void SetControllable(bool state)
        {
            isControllable = state;
        }

        private void Update()
        {
            if (!isControllable) return;

            HandleInput();
            SmoothMovement();

            if (autoShoot || Input.GetMouseButton(0))
            {
                HandleShooting();
            }
        }

        private void HandleInput()
        {
            if (Input.GetMouseButton(0))
            {
                Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
                Plane groundPlane = new Plane(Vector3.up, transform.position);

                if (groundPlane.Raycast(ray, out float enter))
                {
                    Vector3 hitPoint = ray.GetPoint(enter);
                    targetX = Mathf.Clamp(hitPoint.x, -horizontalLimit, horizontalLimit);
                }
            }
        }

        private void SmoothMovement()
        {
            Vector3 pos = transform.position;
            pos.x = Mathf.Lerp(pos.x, targetX, Time.deltaTime * smoothSpeed);
            transform.position = pos;
        }

        private void HandleShooting()
        {
            fireTimer += Time.deltaTime;
            if (fireTimer >= currentFireRate)
            {
                fireTimer = 0f;
                Shoot();
            }
        }

        private void Shoot()
        {
            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
            Quaternion spawnRot = firePoint != null ? firePoint.rotation : Quaternion.identity;

            ObjectPooler.Instance.SpawnFromPool("PlayerMob", spawnPos, spawnRot);
        }
    }
}
