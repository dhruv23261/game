using UnityEngine;
using MobControl.Combat;
using MobControl.Core;

namespace MobControl.Level
{
    /// <summary>
    /// Lethal molten volcanic pit hazard carved into the road.
    /// Deep sunken chasm with basalt rock walls and boiling magma pool at the bottom.
    /// Units stepping over the cavity edge plummet downwards into the pit and incinerate.
    /// </summary>
    public class LavaHazard : MonoBehaviour
    {
        [Header("Visuals")]
        [SerializeField] private float pulseSpeed = 3.2f;
        [SerializeField] private Color baseEmission = new Color(3.5f, 1.2f, 0.1f);
        [SerializeField] private Color peakEmission = new Color(5.2f, 2.5f, 0.5f);

        [Header("Damage")]
        [SerializeField] private float hitCooldown = 0.25f;
        private float lastHitTime = -10f;
        private Renderer surfaceRenderer;
        private Material instanceMat;

        private float nextBubbleTime = 0f;
        private Vector2 lavaSize = new Vector2(3f, 6f);
        public const float LAVA_SURFACE_Y = -0.95f;

        public void Initialize(Material mat, Vector2 size)
        {
            this.lavaSize = size;
            surfaceRenderer = transform.Find("LavaSurface")?.GetComponent<Renderer>() ?? GetComponentInChildren<Renderer>();
            if (surfaceRenderer != null && mat != null)
            {
                instanceMat = new Material(mat);
                surfaceRenderer.material = instanceMat;
            }
        }

        private void Awake()
        {
            if (surfaceRenderer == null)
            {
                surfaceRenderer = transform.Find("LavaSurface")?.GetComponent<Renderer>() ?? GetComponentInChildren<Renderer>();
            }
            if (surfaceRenderer != null && instanceMat == null)
            {
                instanceMat = surfaceRenderer.material;
            }
        }

        private void Update()
        {
            // 1. Pulsate molten heat emission & scroll magma texture
            if (instanceMat != null)
            {
                float t = (Mathf.Sin(Time.time * pulseSpeed) + 1.0f) * 0.5f;
                Color currentEmission = Color.Lerp(baseEmission, peakEmission, t);
                instanceMat.SetColor("_EmissionColor", currentEmission);
                if (instanceMat.HasProperty("_Color"))
                {
                    instanceMat.SetColor("_Color", Color.Lerp(new Color(1f, 0.5f, 0.1f), new Color(1f, 0.85f, 0.4f), t));
                }
                Vector2 offset = new Vector2(0f, (Time.time * 0.09f) % 1.0f);
                instanceMat.SetTextureOffset("_MainTex", offset);
            }

            // 2. Spawn bubbling magma bubbles periodically inside the pit
            if (Time.time >= nextBubbleTime)
            {
                nextBubbleTime = Time.time + Random.Range(0.16f, 0.35f);
                SpawnMagmaBubble();
            }

            // 3. Proximity / Fall check for player and squad members
            if (MobControl.UI.ZombieRunnerUIManager.Instance != null &&
                MobControl.UI.ZombieRunnerUIManager.Instance.CurrentState != MobControl.UI.ZombieRunnerUIManager.ScreenState.Gameplay) return;

            var player = PlayerRunnerController.Instance;
            if (player == null || player.IsDead || player.IsWaitingToStart) return;

            Vector3 pitWorldPos = transform.position;
            // Pit horizontal cavity bounds with small grace margin
            float halfW = (lavaSize.x * 0.5f) - 0.12f;
            float halfL = (lavaSize.y * 0.5f) - 0.20f;

            // 3a. Check active Squad Allies first (wide formation crowd falls into the pit!)
            var allies = player.SquadAllies;
            if (allies != null && allies.Count > 0)
            {
                for (int i = allies.Count - 1; i >= 0; i--)
                {
                    var ally = allies[i];
                    if (ally == null || ally.IsFallingInPit) continue;

                    Vector3 pos = ally.transform.position;
                    if (Mathf.Abs(pos.x - pitWorldPos.x) <= halfW && Mathf.Abs(pos.z - pitWorldPos.z) <= halfL)
                    {
                        TriggerAllyFall(ally);
                    }
                }
            }

            // 3b. Check Player Leader
            if (!player.IsFallingInPit && !player.IsObstacleInvulnerable)
            {
                Vector3 pPos = player.transform.position;
                if (Mathf.Abs(pPos.x - pitWorldPos.x) <= halfW && Mathf.Abs(pPos.z - pitWorldPos.z) <= halfL)
                {
                    TriggerPlayerFall(player);
                }
            }
        }

        private void TriggerAllyFall(PlayerSquadAlly ally)
        {
            if (ally == null || ally.IsFallingInPit) return;
            lastHitTime = Time.time;

            Camera mainCam = Camera.main;
            SmoothCameraFollow camFollow = mainCam != null ? mainCam.GetComponent<SmoothCameraFollow>() : null;
            if (camFollow != null) camFollow.TriggerShake(0.04f);

            SpawnLavaSteamBurst(ally.transform.position);
            ally.TriggerFallIntoPit(transform.position);
        }

        private void TriggerPlayerFall(PlayerRunnerController player)
        {
            if (player == null || player.IsFallingInPit || player.IsDead) return;
            lastHitTime = Time.time;

            Camera mainCam = Camera.main;
            SmoothCameraFollow camFollow = mainCam != null ? mainCam.GetComponent<SmoothCameraFollow>() : null;
            if (camFollow != null) camFollow.TriggerShake(0.08f);

            SpawnLavaSteamBurst(player.transform.position);
            player.TriggerFallIntoPit(transform.position);
        }

        private void SpawnMagmaBubble()
        {
            Camera cam = Camera.main;
            if (cam != null && Vector3.Distance(cam.transform.position, transform.position) > 60f) return;

            GameObject bubble = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bubble.name = "MagmaBubble";
            bubble.transform.SetParent(transform, false);

            float rx = Random.Range(-lavaSize.x * 0.40f, lavaSize.x * 0.40f);
            float rz = Random.Range(-lavaSize.y * 0.40f, lavaSize.y * 0.40f);
            bubble.transform.localPosition = new Vector3(rx, LAVA_SURFACE_Y + 0.04f, rz);
            float startScale = Random.Range(0.18f, 0.35f);
            bubble.transform.localScale = Vector3.one * startScale;
            Destroy(bubble.GetComponent<Collider>());

            Material bm = new Material(Shader.Find("Standard"));
            bm.color = new Color(1f, 0.55f, 0.1f);
            bm.EnableKeyword("_EMISSION");
            bm.SetColor("_EmissionColor", new Color(4.8f, 2.0f, 0.3f));
            bubble.GetComponent<Renderer>().material = bm;

            bubble.AddComponent<MagmaBubbleAnimation>();
        }

        private void OnTriggerEnter(Collider other)
        {
            CheckAndTrigger(other.gameObject);
        }

        private void OnTriggerStay(Collider other)
        {
            CheckAndTrigger(other.gameObject);
        }

        private void CheckAndTrigger(GameObject go)
        {
            PlayerSquadAlly ally = go.GetComponentInParent<PlayerSquadAlly>();
            if (ally != null && !ally.IsFallingInPit)
            {
                TriggerAllyFall(ally);
                return;
            }

            PlayerRunnerController player = go.GetComponentInParent<PlayerRunnerController>();
            if (player != null && !player.IsFallingInPit)
            {
                TriggerPlayerFall(player);
            }
        }

        private void SpawnLavaSteamBurst(Vector3 pos)
        {
            GameObject burst = new GameObject("LavaBurn_FX");
            burst.transform.position = pos + Vector3.up * 0.2f;

            GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flash.transform.SetParent(burst.transform, false);
            flash.transform.localScale = Vector3.one * 1.1f;
            Destroy(flash.GetComponent<Collider>());

            Material m = new Material(Shader.Find("Standard"));
            m.color = new Color(1f, 0.45f, 0.05f, 0.85f);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", new Color(5.5f, 2.4f, 0.4f));
            flash.GetComponent<Renderer>().material = m;

            Destroy(burst, 0.45f);
        }

        private void OnDestroy()
        {
            if (instanceMat != null)
            {
                Destroy(instanceMat);
                instanceMat = null;
            }
        }
    }

    public class MagmaBubbleAnimation : MonoBehaviour
    {
        private float life = 0f;
        private float maxLife = 0.55f;
        private Vector3 initialScale;

        private void Start()
        {
            initialScale = transform.localScale;
            Destroy(gameObject, maxLife);
        }

        private void Update()
        {
            life += Time.deltaTime;
            float t = life / maxLife;
            transform.localPosition += Vector3.up * (0.18f * Time.deltaTime);
            float scaleMultiplier = Mathf.Sin(t * Mathf.PI);
            transform.localScale = initialScale * (1f + scaleMultiplier * 0.75f);
        }
    }
}
