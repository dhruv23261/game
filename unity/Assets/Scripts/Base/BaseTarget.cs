using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MobControl.Core;

namespace MobControl.Base
{
    public class BaseTarget : MonoBehaviour
    {
        [Header("Base Properties")]
        [SerializeField] private bool isEnemyBase = true;
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private Slider healthSlider;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private ParticleSystem explosionVfx;

        private int currentHealth;

        public bool IsEnemyBase => isEnemyBase;
        public int CurrentHealth => currentHealth;

        public void InitializeBase(int health)
        {
            maxHealth = health;
            currentHealth = health;
            UpdateUI();
        }

        public void TakeDamage(int damage)
        {
            currentHealth = Mathf.Max(0, currentHealth - damage);
            UpdateUI();

            if (currentHealth <= 0)
            {
                OnBaseDestroyed();
            }
        }

        private void UpdateUI()
        {
            if (healthSlider != null)
            {
                healthSlider.maxValue = maxHealth;
                healthSlider.value = currentHealth;
            }

            if (healthText != null)
            {
                healthText.text = $"{currentHealth}/{maxHealth}";
            }
        }

        private void OnBaseDestroyed()
        {
            if (explosionVfx != null)
            {
                explosionVfx.Play();
            }

            if (isEnemyBase)
            {
                GameManager.Instance.OnLevelWon();
            }
            else
            {
                GameManager.Instance.OnLevelLost();
            }
        }
    }
}
