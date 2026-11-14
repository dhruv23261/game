using System.Collections;
using UnityEngine;
using MobControl.Combat;
using MobControl.UI;

namespace MobControl.Creatures
{
    /// <summary>
    /// Level 2 road creature. Sits on the road as a glowing magenta pod; when the player gets close
    /// (or shoots it) the asphalt bursts and a Meshy carnivorous flytrap erupts, sways, snaps at the
    /// squad and slowly advances. Shoot it down for coins.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class EmergingPlantMonster : MonoBehaviour
    {
        public enum PlantState { DormantBud, Rising, Awakened, Dead }

        [Header("State")]
        [SerializeField] private PlantState currentState = PlantState.DormantBud;

        [Header("Stats")]
        [SerializeField] private float maxHealth = 180f;
        [SerializeField] private float currentHealth;
        [SerializeField] private float activationDistance = 22f;
        [SerializeField] private float advanceSpeed = 1.6f;
        [SerializeField] private int coinReward = 60;

        [Header("Visual Elements")]
        [SerializeField] private Transform roadPlantPod;
        [SerializeField] private Transform zombieRoot;

        private Transform playerTransform;
        private BoxCollider boxCol;
        private Simple3DHealthBar healthBar;
        private Renderer[] monsterRenderers;
        private MaterialPropertyBlock mpb;
        private Transform bud;
        private ParticleSystem spores;
        private float flashUntil;
        private float chompTimer;
        private float seed;

        private static Material s_sporeMat;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private void Awake()
        {
            boxCol = GetComponent<BoxCollider>();
            currentHealth = maxHealth;
            seed = Random.value * 10f;
            mpb = new MaterialPropertyBlock();

            if (roadPlantPod == null) roadPlantPod = transform.Find("RoadPlantPod");
            if (zombieRoot == null) zombieRoot = transform.Find("ZombieRoot");
            if (roadPlantPod != null) bud = roadPlantPod.Find("Bud");
            healthBar = GetComponentInChildren<Simple3DHealthBar>(true);
            monsterRenderers = zombieRoot != null ? zombieRoot.GetComponentsInChildren<Renderer>(true) : new Renderer[0];

            // Randomise the pod a little so a row of them doesn't look copy-pasted
            if (roadPlantPod != null) roadPlantPod.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            CreateSporeAura();
            SetDormant();
        }

        private void CreateSporeAura()
        {
            if (s_sporeMat == null) s_sporeMat = Resources.Load<Material>("Materials/Level2/M_L2_Spore");
            if (s_sporeMat == null) return;

            GameObject go = new GameObject("SporeAura");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            spores = go.AddComponent<ParticleSystem>();
            spores.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = spores.main;
            main.loop = true;
            main.duration = 2f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.16f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.85f, 0.35f, 1f, 0.9f), new Color(0.55f, 1f, 0.35f, 0.9f));
            main.gravityModifier = -0.04f;
            main.maxParticles = 30;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = spores.emission;
            emission.rateOverTime = 7f;

            var shape = spores.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.8f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var col = spores.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0f, 1f) });
            col.color = g;

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = s_sporeMat;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;

            spores.Play();
        }

        private void SetDormant()
        {
            currentState = PlantState.DormantBud;
            if (roadPlantPod != null)
            {
                roadPlantPod.gameObject.SetActive(true);
                roadPlantPod.localScale = Vector3.one;
            }
            if (zombieRoot != null)
            {
                zombieRoot.gameObject.SetActive(false);
                zombieRoot.localScale = Vector3.zero;
            }
            if (boxCol != null)
            {
                boxCol.center = new Vector3(0, 0.5f, 0);
                boxCol.size = new Vector3(1.8f, 1.2f, 1.8f);
            }
        }

        private void Start()
        {
            FindPlayer();
        }

        private void FindPlayer()
        {
            if (PlayerRunnerController.Instance != null) { playerTransform = PlayerRunnerController.Instance.transform; return; }
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
            if (currentState == PlantState.Dead) return;
            float t = Time.time + seed;

            if (currentState == PlantState.DormantBud)
            {
                // Breathing pod + pulsing glow
                if (roadPlantPod != null)
                {
                    float pulse = 1f + 0.05f * Mathf.Sin(t * 3.2f);
                    roadPlantPod.localScale = new Vector3(pulse, 1f + 0.08f * Mathf.Sin(t * 3.2f), pulse);
                }
                if (bud != null) bud.localScale = new Vector3(0.55f, 0.72f, 0.55f) * (1f + 0.08f * Mathf.Sin(t * 6f));
            }
            else if (currentState == PlantState.Awakened && zombieRoot != null)
            {
                // Idle sway + periodic snapping lunge toward the squad
                chompTimer -= Time.deltaTime;
                float lunge = 0f;
                if (chompTimer <= 0f) chompTimer = Random.Range(1.1f, 1.8f);
                if (chompTimer < 0.3f) lunge = Mathf.Sin((0.3f - chompTimer) / 0.3f * Mathf.PI);

                float sway = Mathf.Sin(t * 2.1f) * 7f;
                zombieRoot.localRotation = Quaternion.Euler(-lunge * 16f, sway, Mathf.Sin(t * 1.6f) * 4f);
                float squash = 1f + 0.04f * Mathf.Sin(t * 4.2f) + lunge * 0.10f;
                zombieRoot.localScale = new Vector3(2f - squash, squash, 2f - squash);
            }

            if (Time.time > flashUntil && flashUntil > 0f)
            {
                flashUntil = 0f;
                ApplyTint(Color.white, 0f);
            }

            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform == null) return;
            }

            float dist = Vector3.Distance(transform.position, playerTransform.position);

            if (currentState == PlantState.DormantBud && dist <= activationDistance)
                StartCoroutine(AwakenRoutine());

            if (currentState == PlantState.Awakened && transform.position.z > playerTransform.position.z + 0.5f)
                transform.position += Vector3.back * (advanceSpeed * Time.deltaTime);
        }

        public void TriggerAwaken()
        {
            if (currentState == PlantState.DormantBud) StartCoroutine(AwakenRoutine());
        }

        private IEnumerator AwakenRoutine()
        {
            currentState = PlantState.Rising;

            var roar = Resources.Load<AudioClip>("Audio/AAA/boss_roar");
            if (roar != null) AudioSource.PlayClipAtPoint(roar, transform.position, 0.6f);

            // 1. Pod shudders
            float shake = 0f;
            Vector3 podBasePos = roadPlantPod != null ? roadPlantPod.localPosition : Vector3.zero;
            while (shake < 0.25f)
            {
                shake += Time.deltaTime;
                if (roadPlantPod != null)
                {
                    roadPlantPod.localPosition = podBasePos + new Vector3(Mathf.Sin(shake * 45f) * 0.10f, 0f, 0f);
                    roadPlantPod.localScale = Vector3.one * (1f + 0.15f * Mathf.Sin(shake * 35f));
                }
                yield return null;
            }
            if (roadPlantPod != null) roadPlantPod.localPosition = podBasePos;
            if (spores != null)
            {
                var em = spores.emission;
                em.rateOverTime = 14f;
                spores.Emit(25);
            }

            // 2. Flytrap bursts up out of the bud; leaves peel flat onto the asphalt
            if (zombieRoot != null)
            {
                zombieRoot.gameObject.SetActive(true);
                zombieRoot.localPosition = new Vector3(0f, -1.2f, 0f);
                zombieRoot.localScale = Vector3.zero;
            }
            if (bud != null) bud.gameObject.SetActive(false);

            float dur = 0.45f, e = 0f;
            while (e < dur)
            {
                e += Time.deltaTime;
                float k = Mathf.Clamp01(e / dur);
                float ease = Mathf.Sin(k * Mathf.PI * 0.5f);
                float overshoot = 1f + 0.22f * Mathf.Sin(k * Mathf.PI);
                if (zombieRoot != null)
                {
                    zombieRoot.localScale = Vector3.one * (ease * overshoot);
                    zombieRoot.localPosition = Vector3.Lerp(new Vector3(0f, -1.2f, 0f), Vector3.zero, ease);
                }
                if (roadPlantPod != null) roadPlantPod.localScale = Vector3.Lerp(Vector3.one, new Vector3(1.25f, 0.35f, 1.25f), k);
                yield return null;
            }

            if (zombieRoot != null)
            {
                zombieRoot.localScale = Vector3.one;
                zombieRoot.localPosition = Vector3.zero;
            }

            if (boxCol != null)
            {
                boxCol.center = new Vector3(0f, 1.1f, 0f);
                boxCol.size = new Vector3(1.5f, 2.2f, 1.5f);
            }

            chompTimer = 0.8f;
            currentState = PlantState.Awakened;
        }

        public void TakeDamage(float damage)
        {
            if (currentState == PlantState.Dead) return;

            if (currentState == PlantState.DormantBud) StartCoroutine(AwakenRoutine());

            currentHealth -= damage;
            if (healthBar != null) healthBar.SetHealth(Mathf.Max(0f, currentHealth), maxHealth);

            ApplyTint(new Color(1f, 0.55f, 0.55f), 0.9f);
            flashUntil = Time.time + 0.08f;

            if (currentHealth <= 0f) Die();
        }

        /// <summary>Hit flash via property block (no per-hit material instances).</summary>
        private void ApplyTint(Color c, float glow)
        {
            if (monsterRenderers == null) return;
            foreach (var r in monsterRenderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(mpb);
                mpb.SetColor(ColorId, c);
                if (glow > 0f) mpb.SetColor(EmissionColorId, new Color(glow, 0.25f * glow, 0.2f * glow));
                else mpb.Clear();
                r.SetPropertyBlock(mpb);
            }
        }

        private void Die()
        {
            currentState = PlantState.Dead;
            if (boxCol != null) boxCol.enabled = false;
            if (healthBar != null) healthBar.gameObject.SetActive(false);

            var boom = Resources.Load<AudioClip>("Audio/AAA/explosion_01");
            if (boom != null) AudioSource.PlayClipAtPoint(boom, transform.position, 0.6f);

            if (spores != null)
            {
                spores.Emit(40);
                var em = spores.emission;
                em.rateOverTime = 0f;
            }

            if (ZombieRunnerUIManager.Instance != null) ZombieRunnerUIManager.Instance.AddPlayerCoins(coinReward);

            StartCoroutine(DeathShrinkRoutine());
        }

        private IEnumerator DeathShrinkRoutine()
        {
            Transform target = zombieRoot != null && zombieRoot.gameObject.activeSelf ? zombieRoot : roadPlantPod;
            float duration = 0.4f, elapsed = 0f;
            Vector3 initialScale = target != null ? target.localScale : Vector3.one;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float k = elapsed / duration;
                if (target != null)
                {
                    target.localScale = Vector3.Lerp(initialScale, Vector3.zero, k * k);
                    target.Rotate(Vector3.up, 540f * Time.deltaTime);
                }
                if (roadPlantPod != null && target != roadPlantPod)
                    roadPlantPod.localScale = Vector3.Lerp(roadPlantPod.localScale, new Vector3(1.3f, 0.05f, 1.3f), k);
                yield return null;
            }

            Destroy(gameObject, 1.2f); // leave a moment for the spore burst to fade
            if (zombieRoot != null) zombieRoot.gameObject.SetActive(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            // Bullets are handled by BulletProjectile (it calls TakeDamage) — only contact damage here
            if (currentState != PlantState.Awakened || other == null) return;

            var hero = other.GetComponent<PlayerRunnerController>() ?? other.GetComponentInParent<PlayerRunnerController>();
            if (hero != null)
            {
                hero.TakeDamage(1);
                return;
            }

            var mob = other.GetComponent<MobControl.Crowd.MobUnit>() ?? other.GetComponentInParent<MobControl.Crowd.MobUnit>();
            if (mob != null && mob.Team == MobControl.Crowd.MobTeam.Player)
                mob.TakeDamage(100);
        }
    }
}
