using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MobControl.UI
{
    public class WorldHealthBar : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Slider ghostSlider; // Smooth lagging catch-up bar
        [SerializeField] private Image fillImage;
        [SerializeField] private TextMeshProUGUI hpText;

        [Header("Color Gradient")]
        [SerializeField] private Color fullHealthColor = new Color(0.1f, 0.9f, 0.3f);   // Bright Neon Green
        [SerializeField] private Color midHealthColor = new Color(1.0f, 0.85f, 0.1f);  // Vibrant Gold
        [SerializeField] private Color lowHealthColor = new Color(1.0f, 0.2f, 0.2f);   // Bright Crimson Red

        [Header("Settings")]
        [SerializeField] private bool billboardToCamera = true;
        [SerializeField] private bool hideWhenFull = true;
        [SerializeField] private float ghostCatchupSpeed = 2.5f;

        private Camera mainCam;
        private Canvas canvas;
        private int maxHp;
        private int currentHp;
        private Coroutine ghostRoutine;

        private void Awake()
        {
            mainCam = Camera.main;
            canvas = GetComponent<Canvas>();
        }

        private void LateUpdate()
        {
            // Always face camera
            if (billboardToCamera && mainCam != null)
            {
                transform.forward = mainCam.transform.forward;
            }
        }

        public void Initialize(int maxHealth, int currentHealth = -1)
        {
            maxHp = maxHealth;
            currentHp = currentHealth < 0 ? maxHealth : currentHealth;

            if (healthSlider != null)
            {
                healthSlider.maxValue = maxHp;
                healthSlider.value = currentHp;
            }

            if (ghostSlider != null)
            {
                ghostSlider.maxValue = maxHp;
                ghostSlider.value = currentHp;
            }

            UpdateVisuals();
        }

        public void SetHealth(int newHealth)
        {
            currentHp = Mathf.Clamp(newHealth, 0, maxHp);

            if (healthSlider != null)
            {
                healthSlider.value = currentHp;
            }

            if (ghostRoutine != null)
            {
                StopCoroutine(ghostRoutine);
            }
            ghostRoutine = StartCoroutine(SmoothGhostCatchup());

            UpdateVisuals();
        }

        private IEnumerator SmoothGhostCatchup()
        {
            yield return new WaitForSeconds(0.15f); // Brief delay before catching up

            if (ghostSlider != null)
            {
                while (ghostSlider.value > healthSlider.value)
                {
                    ghostSlider.value = Mathf.MoveTowards(ghostSlider.value, healthSlider.value, maxHp * ghostCatchupSpeed * Time.deltaTime);
                    yield return null;
                }
                ghostSlider.value = healthSlider.value;
            }
        }

        private void UpdateVisuals()
        {
            float percent = maxHp > 0 ? (float)currentHp / maxHp : 0;

            // Color gradient shift
            if (fillImage != null)
            {
                if (percent > 0.5f)
                {
                    fillImage.color = Color.Lerp(midHealthColor, fullHealthColor, (percent - 0.5f) * 2f);
                }
                else
                {
                    fillImage.color = Color.Lerp(lowHealthColor, midHealthColor, percent * 2f);
                }
            }

            // Text update
            if (hpText != null)
            {
                hpText.text = $"{currentHp}/{maxHp}";
            }

            // Visibility toggle
            if (hideWhenFull && canvas != null)
            {
                canvas.enabled = currentHp < maxHp && currentHp > 0;
            }
        }
    }
}
