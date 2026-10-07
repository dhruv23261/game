using System;
using UnityEngine;
using UnityEngine.UI;
using ThomasDev.HealthDamageSystem;
using MobControl.Crowd;
using MobControl.Combat;

namespace ThomasDev.HealthSystem
{
    [DisallowMultipleComponent]
    public class HealthBarUI : MonoBehaviour
    {
        [SerializeField] private Image image;
        [SerializeField] private GameObject gameobject;
        [SerializeField] private float heightOffset = 2.2f;

        private Health health;
        private MobUnit mobUnit;
        private DestructibleGate destructibleGate;
        private Camera mainCamera;
        private Transform followTarget;
        private Canvas canvas;

        private void Awake()
        {
            // NEVER show health bar on Player!
            if (gameObject.name.Contains("PlayerMob") || (transform.parent != null && transform.parent.name.Contains("PlayerMob")))
            {
                Destroy(gameObject);
                return;
            }

            mainCamera = Camera.main;

            if (gameobject == null && transform.parent != null)
            {
                gameobject = transform.parent.gameObject;
            }

            if (gameobject != null)
            {
                followTarget = gameobject.transform;
                gameobject.TryGetComponent<Health>(out health);
                gameobject.TryGetComponent<MobUnit>(out mobUnit);
                gameobject.TryGetComponent<DestructibleGate>(out destructibleGate);

                if (destructibleGate != null)
                {
                    heightOffset = 3.6f;
                }
            }

            // Ensure World Space Canvas exists
            canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;
            canvas.enabled = false; // HIDDEN BY DEFAULT (Only shows when shot!)

            if (image == null)
            {
                Image[] images = GetComponentsInChildren<Image>();
                foreach (var img in images)
                {
                    if (img.gameObject.name.ToLower().Contains("fill") || img.type == Image.Type.Filled)
                    {
                        image = img;
                        break;
                    }
                }
                if (image == null && images.Length > 0) image = images[images.Length - 1];
            }

            if (image != null)
            {
                image.type = Image.Type.Filled;
                image.fillMethod = Image.FillMethod.Horizontal;
                image.fillOrigin = (int)Image.OriginHorizontal.Left;
                image.color = Color.green;
            }
        }

        private bool isDamaged = false;

        private void Start()
        {
            // Detach from 100x scaled parent to be immune to scale bugs
            if (followTarget != null)
            {
                transform.SetParent(null);
            }

            RectTransform rootRect = GetComponent<RectTransform>();
            if (rootRect != null)
            {
                rootRect.sizeDelta = new Vector2(160f, 22f);
            }
            transform.localScale = new Vector3(0.008f, 0.008f, 0.008f);

            // Hide initially until damaged!
            if (canvas != null) canvas.enabled = false;

            if (mobUnit != null)
            {
                mobUnit.SetUiHealthBar(this);
                mobUnit.OnMobHealthChanged += OnMobHealthChanged;
                UpdateHealth(mobUnit.Health, mobUnit.MaxHealth);
            }

            if (health != null)
            {
                health.OnDamaged.AddListener(OnHealthChanged);
                health.OnHealed.AddListener(OnHealthChanged);
                UpdateHealth(health.CurrentHealth, health.MaxHealth);
            }

            // Listen to Gate Broken event for instant destruction
            if (destructibleGate != null)
            {
                destructibleGate.OnGateBroken += Hide;
            }
        }

        private void LateUpdate()
        {
            // Immediate destruction check if Gate broke or Zombie died
            if (destructibleGate != null && destructibleGate.IsBroken)
            {
                Destroy(gameObject);
                return;
            }

            if (mobUnit != null && mobUnit.Health <= 0)
            {
                Destroy(gameObject);
                return;
            }

            // Follow target in world space
            if (followTarget != null && followTarget.gameObject.activeInHierarchy)
            {
                // Only show canvas if this enemy has actually been damaged!
                bool shouldShow = isDamaged && (mobUnit == null || mobUnit.Health > 0);
                if (canvas != null && canvas.enabled != shouldShow)
                {
                    canvas.enabled = shouldShow;
                }

                if (shouldShow)
                {
                    transform.position = followTarget.position + Vector3.up * heightOffset;

                    if (mainCamera != null)
                    {
                        transform.forward = mainCamera.transform.forward;
                    }
                }
            }
            else
            {
                // Target temporarily inactive in upcoming zone: hide canvas, DO NOT DESTROY!
                if (canvas != null && canvas.enabled)
                {
                    canvas.enabled = false;
                }

                // Only destroy if target was actually destroyed in memory
                if (followTarget == null)
                {
                    Destroy(gameObject);
                }
            }
        }

        private void OnMobHealthChanged(int curr, int max)
        {
            UpdateHealth((float)curr, (float)max);
        }

        private void OnHealthChanged(float healthCurr, float healthMax)
        {
            UpdateHealth(healthCurr, healthMax);
        }

        public void UpdateHealth(float current, float max)
        {
            if (max <= 0) return;

            // ONLY mark as damaged and visible if current health is strictly less than max!
            if (current < max && current > 0)
            {
                isDamaged = true;
                if (canvas != null) canvas.enabled = true;
            }
            else if (current >= max)
            {
                isDamaged = false;
                if (canvas != null) canvas.enabled = false;
            }

            float ratio = Mathf.Clamp01(current / max);

            if (image != null)
            {
                image.fillAmount = ratio;
                image.color = Color.Lerp(Color.red, Color.green, ratio);
            }

            if (current <= 0)
            {
                if (canvas != null) canvas.enabled = false;
                Destroy(gameObject);
            }
        }

        public void Hide()
        {
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (mobUnit != null)
            {
                mobUnit.OnMobHealthChanged -= OnMobHealthChanged;
            }

            if (health != null)
            {
                health.OnDamaged.RemoveListener(OnHealthChanged);
                health.OnHealed.RemoveListener(OnHealthChanged);
            }

            if (destructibleGate != null)
            {
                destructibleGate.OnGateBroken -= Hide;
            }
        }
    }
}