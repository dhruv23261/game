using System.Collections;
using UnityEngine;
using MobControl.Combat;
using MobControl.UI;

namespace MobControl.Creatures
{
    /// <summary>
    /// Level 2 Airborne Threat:
    /// High-altitude flying Wyvern dragon that glides above the road,
    /// banks between lanes, and dive-bombs / swoops down towards the player mob.
    /// Can be shot down for massive reward!
    /// </summary>
    public class FlyingWyvernAirThreat : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] private float maxHealth = 300f;
        [SerializeField] private float currentHealth;
        [SerializeField] private float flySpeed = 5.5f;
        [SerializeField] private float hoverAltitude = 6.2f;
        [SerializeField] private float swoopAltitude = 3.4f;
        [SerializeField] private int coinReward = 150;

        [Header("Movement")]
        [SerializeField] private float horizontalSway = 2.6f;
        [SerializeField] private float swayFrequency = 1.2f;

        private Transform playerTransform;
        private Transform visualRoot;
        private Collider col;
        private bool isSwooping = false;
        private bool isDead = false;
        private float initialZ;
        private float timeAlive = 0f;

        private void Awake()
        {
            currentHealth = maxHealth;
            col = GetComponent<Collider>();
            visualRoot = transform.Find("VisualRoot");
            if (visualRoot == null) visualRoot = transform;
            initialZ = transform.position.z;
        }

        private void Start()
        {
            FindPlayer();
        }

        private void FindPlayer()
        {
            var hero = GameObject.Find("Player_Hero");
            if (hero != null) playerTransform = hero.transform;
            else
            {
                var playerRunner = Object.FindAnyObjectByType<PlayerRunnerController>();
                if (playerRunner != null) playerTransform = playerRunner.transform;
            }
        }

        private void Update()
        {
            if (isDead) return;

            timeAlive += Time.deltaTime;

            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform == null) return;
            }

            // Flap & wing wobble animation
            float wingFlap = Mathf.Sin(timeAlive * 8f) * 12f;
            float rollBank = Mathf.Cos(timeAlive * swayFrequency) * 18f;

            // Horizontal swaying across lanes
            float targetX = Mathf.Sin(timeAlive * swayFrequency) * horizontalSway;

            // Distance to player
            float distZ = transform.position.z - playerTransform.position.z;

            // Flew past the squad — despawn instead of flying forever behind the camera
            if (distZ < -3f)
            {
                Destroy(gameObject);
                return;
            }

            // Swoop down when player gets close (35m to 15m away)
            float targetY = hoverAltitude;
            if (distZ <= 14f)
            {
                // Pull up and over the squad so it never fills the screen / hides the HUD
                targetY = hoverAltitude + 5f;
            }
            else if (distZ < 40f)
            {
                float t = Mathf.InverseLerp(40f, 22f, distZ);
                targetY = Mathf.Lerp(hoverAltitude, swoopAltitude, Mathf.Sin(t * Mathf.PI));
                if (!isSwooping && t > 0.4f)
                {
                    isSwooping = true;
                    AudioSource.PlayClipAtPoint(Resources.Load<AudioClip>("Audio/AAA/boss_roar"), transform.position, 0.5f);
                }
            }

            // Smooth movement
            transform.position = new Vector3(
                Mathf.Lerp(transform.position.x, targetX, Time.deltaTime * 3f),
                Mathf.Lerp(transform.position.y, targetY, Time.deltaTime * 4f),
                transform.position.z - (flySpeed * Time.deltaTime)
            );

            // Orient model: tilted slightly down and banking
            visualRoot.localRotation = Quaternion.Euler(15f + wingFlap * 0.2f, 180f, rollBank);
        }

        public void TakeDamage(float damage)
        {
            if (isDead) return;

            currentHealth -= damage;
            StartCoroutine(DamageBlink());

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock mpb;
        private Renderer[] cachedRenderers;

        private IEnumerator DamageBlink()
        {
            // Property block flash: no material instances leaked per hit
            if (cachedRenderers == null) cachedRenderers = GetComponentsInChildren<Renderer>();
            if (mpb == null) mpb = new MaterialPropertyBlock();
            foreach (var r in cachedRenderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(mpb);
                mpb.SetColor(ColorId, new Color(1f, 0.5f, 0.5f));
                mpb.SetColor(EmissionId, new Color(0.9f, 0.2f, 0.15f));
                r.SetPropertyBlock(mpb);
            }
            yield return new WaitForSeconds(0.08f);
            foreach (var r in cachedRenderers) if (r != null) r.SetPropertyBlock(null);
        }

        private void Die()
        {
            isDead = true;
            if (col != null) col.enabled = false;

            AudioSource.PlayClipAtPoint(Resources.Load<AudioClip>("Audio/AAA/explosion_02"), transform.position, 0.8f);

            if (ZombieRunnerUIManager.Instance != null)
            {
                ZombieRunnerUIManager.Instance.AddPlayerCoins(coinReward);
            }

            StartCoroutine(CrashSpinRoutine());
        }

        private IEnumerator CrashSpinRoutine()
        {
            float elapsed = 0f;
            float duration = 0.8f;
            Vector3 vel = new Vector3(Random.Range(-2f, 2f), -5f, 2f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.position += vel * Time.deltaTime;
                visualRoot.Rotate(Vector3.forward, 720f * Time.deltaTime);
                visualRoot.localScale = Vector3.Lerp(Vector3.one * 1.5f, Vector3.zero, elapsed / duration);
                yield return null;
            }

            Destroy(gameObject);
        }

        // Bullet hits are routed through BulletProjectile → TakeDamage (handling them here too doubled the damage)
    }
}
