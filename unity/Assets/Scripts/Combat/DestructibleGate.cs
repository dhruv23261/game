using System.Collections;
using UnityEngine;
using TMPro;
using MobControl.Gates;
using MobControl.UI;

namespace MobControl.Combat
{
    public enum GateRewardType
    {
        SquadMultiplier,
        GunUpgrade
    }

    public class DestructibleGate : MonoBehaviour
    {
        [Header("Gate Health & Settings")]
        [Tooltip("Hit points to destroy this gate. Customize differently per gate in Inspector!")]
        [SerializeField] private int maxHealth = 25;
        [SerializeField] private TextMeshPro healthText;
        [SerializeField] private GameObject gateModel; // Iron gate visual model
        [SerializeField] private Transform leftDoor;
        [SerializeField] private Transform rightDoor;
        [SerializeField] private ParticleSystem breakVfx;

        [Header("Reward Type")]
        [SerializeField] private GateRewardType rewardType = GateRewardType.SquadMultiplier;
        [SerializeField] private float gunSpeedMultiplier = 2.2f;

        [Header("Multiplier Reward Configuration")]
        [Tooltip("The multiplier operation: Multiply (×) or Add (+)")]
        [SerializeField] private GateOperation operation = GateOperation.Multiply;

        [Tooltip("The multiplier or addition value (e.g. 2 for ×2, 3 for ×3, 5 for +5). Customize per gate in Inspector!")]
        [SerializeField] private int multiplierValue = 2;

        [Tooltip("Optional reference to the 3D '×2' text label on this gate. Auto-found if null.")]
        [SerializeField] private TextMeshPro multiplierLabel;
        [SerializeField] private GameObject multiplierTrigger;

        [Tooltip("Custom gate world width (e.g. 2.3 for 1 lane, 5.2 for full road container)")]
        [SerializeField] private float gateWidth = 2.3f;

        private int currentHealth;
        private bool isBroken = false;
        private Vector3 textOriginalScale = Vector3.one;
        private Vector3 gateModelOrigScale = Vector3.one;
        private BoxCollider boxCollider;
        private Coroutine activeShakeRoutine;
        // Cached component refs
        private Simple3DHealthBar cached3DBar;
        private ThomasDev.HealthSystem.HealthBarUI cachedUiBar;

        public static readonly System.Collections.Generic.List<DestructibleGate> ActiveGates = new System.Collections.Generic.List<DestructibleGate>();

        public bool IsBroken => isBroken;
        public int MaxHealth => maxHealth;
        public int MultiplierValue => multiplierValue;
        public GateOperation Operation => operation;
        public GateRewardType RewardType => rewardType;
        public float GateWidth => gateWidth;
        public event System.Action OnGateBroken;

        public void SetGateStats(int health, int multiplier, GateOperation op = GateOperation.Multiply, float width = 2.3f)
        {
            maxHealth = health;
            currentHealth = health;
            rewardType = GateRewardType.SquadMultiplier;
            multiplierValue = multiplier;
            operation = op;
            gateWidth = width;
            FindAndSyncMultiplierLabel();
            UpdateHealthUI();
        }

        public void SetGunUpgradeStats(int health, float speedMult = 2.2f, float width = 2.45f)
        {
            maxHealth = health;
            currentHealth = health;
            rewardType = GateRewardType.GunUpgrade;
            gunSpeedMultiplier = speedMult;
            gateWidth = width;
            FindAndSyncMultiplierLabel();
            FindAndSyncHealthText();
            UpdateHealthUI();

            if (multiplierLabel != null) multiplierLabel.gameObject.SetActive(false);
            if (healthText != null)
            {
                healthText.gameObject.SetActive(true);
                textOriginalScale = healthText.transform.localScale;
            }
        }

        private void OnValidate()
        {
            currentHealth = maxHealth;
            FindAndSyncMultiplierLabel();
        }

        private void OnEnable()
        {
            if (!isBroken && !ActiveGates.Contains(this))
            {
                ActiveGates.Add(this);
            }
        }

        private void OnDisable()
        {
            ActiveGates.Remove(this);
        }

        private void Awake()
        {
            if (!isBroken && !ActiveGates.Contains(this))
            {
                ActiveGates.Add(this);
            }

            FindAndSyncMultiplierLabel();
            FindAndSyncHealthText();

            if (gateModel == null)
            {
                Transform childDefault = transform.Find("default");
                if (childDefault != null)
                {
                    gateModel = childDefault.gameObject;
                }
                else if (transform.childCount > 0)
                {
                    gateModel = transform.GetChild(0).gameObject;
                }
            }

            if (gateModel != null)
            {
                gateModelOrigScale = gateModel.transform.localScale;
            }

            boxCollider = GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                boxCollider = gameObject.AddComponent<BoxCollider>();
            }
            boxCollider.isTrigger = true;

            // Compute local dimensions matching exactly ONE road lane (2.3m world width)
            // Spans from ground level (Y = 0m) up to the top of the gate (Y = 3.6m)
            float sx = Mathf.Max(0.001f, Mathf.Abs(transform.lossyScale.x));
            float sy = Mathf.Max(0.001f, Mathf.Abs(transform.lossyScale.y));
            float sz = Mathf.Max(0.001f, Mathf.Abs(transform.lossyScale.z));

            float targetWorldWidth = gateWidth > 0f ? gateWidth : 2.3f;
            float targetWorldHeight = 3.6f;
            float targetWorldDepth = 1.2f;

            // Transform is at Y = 1.16m in scene, so relative center Y to reach world Y = 1.8m
            float worldCenterOffsetY = 1.8f - transform.position.y;

            boxCollider.size = new Vector3(targetWorldWidth / sx, targetWorldHeight / sy, targetWorldDepth / sz);
            boxCollider.center = new Vector3(0f, worldCenterOffsetY / sy, 0f);

            // Cache health bar refs
            cached3DBar = GetComponentInChildren<Simple3DHealthBar>();
            cachedUiBar = GetComponentInChildren<ThomasDev.HealthSystem.HealthBarUI>();

            EnsureMultiplierTrigger();
        }

        private void Start()
        {
            currentHealth = maxHealth;
            UpdateHealthUI();

            if (multiplierTrigger != null)
            {
                multiplierTrigger.SetActive(false);
            }
        }

        public void TakeDamage(int damage)
        {
            if (isBroken) return;

            currentHealth = Mathf.Max(0, currentHealth - damage);

            if (cached3DBar != null)
            {
                cached3DBar.SetHealth(currentHealth, maxHealth);
            }

            if (cachedUiBar != null)
            {
                cachedUiBar.UpdateHealth(currentHealth, maxHealth);
            }

            if (activeShakeRoutine != null)
            {
                StopCoroutine(activeShakeRoutine);
            }
            activeShakeRoutine = StartCoroutine(GateHitPunchEffect());

            UpdateHealthUI();

            if (currentHealth <= 0)
            {
                isBroken = true;
                BreakAndOpenGate();
            }
        }

        private void UpdateHealthUI()
        {
            if (healthText == null) return;

            healthText.gameObject.SetActive(true);

            if (rewardType == GateRewardType.GunUpgrade)
            {
                if (isBroken || currentHealth <= 0)
                {
                    healthText.text = "<color=#00FFAA><b>0</b></color>";
                    return;
                }

                float pct = (float)currentHealth / Mathf.Max(1, maxHealth);
                string countCol = pct > 0.55f ? "#FFD700" : (pct > 0.25f ? "#FF8800" : "#FF2244");
                healthText.text = $"<color={countCol}><b>{currentHealth}</b></color>";
                return;
            }

            if (isBroken)
            {
                healthText.text = "<size=110%><color=#00FF66><b>BREACHED!</b></color></size>";
                return;
            }

            float healthPercent = (float)currentHealth / maxHealth;
            string countColor = healthPercent > 0.5f ? "#FFE600" : (healthPercent > 0.25f ? "#FF8800" : "#FF1E40");

            healthText.text = $"<color={countColor}><b>{currentHealth}</b></color> <color=#DDDDDD><size=80%>/ {maxHealth}</size></color>";
        }

        private void FindAndSyncMultiplierLabel()
        {
            if (multiplierLabel == null)
            {
                TextMeshPro[] tmps = GetComponentsInChildren<TextMeshPro>(true);
                foreach (var tmp in tmps)
                {
                    if (tmp != healthText)
                    {
                        multiplierLabel = tmp;
                        break;
                    }
                }
            }

            if (multiplierLabel == null && Application.isPlaying)
            {
                // Auto-create 3D multiplier badge so every gate is guaranteed to show its multiplier
                GameObject badgeObj = new GameObject("MultiplierBadge");
                badgeObj.transform.SetParent(transform, false);
                multiplierLabel = badgeObj.AddComponent<TextMeshPro>();
                multiplierLabel.fontSize = 7f;
                multiplierLabel.alignment = TextAlignmentOptions.Center;
                multiplierLabel.fontStyle = FontStyles.Bold;

                // Match existing TMP font asset in the scene
                TextMeshPro sampleTmp = FindAnyObjectByType<TextMeshPro>();
                if (sampleTmp != null && sampleTmp.font != null)
                {
                    multiplierLabel.font = sampleTmp.font;
                    multiplierLabel.fontSharedMaterial = sampleTmp.fontSharedMaterial;
                }

                float sy = Mathf.Max(0.001f, Mathf.Abs(transform.lossyScale.y));
                badgeObj.transform.localPosition = new Vector3(0f, 2.4f / sy, 0.05f);
                badgeObj.transform.localRotation = Quaternion.identity;
                badgeObj.transform.localScale = Vector3.one * 0.12f;
            }

            if (multiplierLabel != null)
            {
                if (rewardType == GateRewardType.GunUpgrade)
                {
                    multiplierLabel.gameObject.SetActive(false);
                }
                else
                {
                    multiplierLabel.gameObject.SetActive(true);
                    string symbol = operation == GateOperation.Multiply ? "×" : "+";
                    multiplierLabel.text = $"{symbol}{multiplierValue}";
                    multiplierLabel.color = (operation == GateOperation.Multiply)
                        ? new Color(0.1f, 0.95f, 1f, 1f) 
                        : new Color(0.2f, 1f, 0.5f, 1f);
                }
            }
        }

        private void FindAndSyncHealthText()
        {
            if (healthText == null)
            {
                Transform ht = transform.Find("HealthText");
                if (ht != null) healthText = ht.GetComponent<TextMeshPro>();
            }

            if (healthText == null && Application.isPlaying)
            {
                GameObject htObj = new GameObject("HealthText");
                htObj.transform.SetParent(transform, false);
                healthText = htObj.AddComponent<TextMeshPro>();
                healthText.fontSize = 5.2f;
                healthText.alignment = TextAlignmentOptions.Center;
                healthText.fontStyle = FontStyles.Bold;

                if (multiplierLabel != null && multiplierLabel.font != null)
                {
                    healthText.font = multiplierLabel.font;
                    healthText.fontSharedMaterial = multiplierLabel.fontSharedMaterial;
                }
                else
                {
                    TextMeshPro sampleTmp = FindAnyObjectByType<TextMeshPro>();
                    if (sampleTmp != null && sampleTmp.font != null)
                    {
                        healthText.font = sampleTmp.font;
                        healthText.fontSharedMaterial = sampleTmp.fontSharedMaterial;
                    }
                }

                float sy = Mathf.Max(0.001f, Mathf.Abs(transform.lossyScale.y));
                htObj.transform.localPosition = new Vector3(0f, 1.8f / sy, 0.05f);
                htObj.transform.localRotation = Quaternion.identity;
                htObj.transform.localScale = Vector3.one * 0.10f;
            }

            if (healthText != null)
            {
                textOriginalScale = healthText.transform.localScale;
            }
        }

        private IEnumerator GateHitPunchEffect()
        {
            // 1. Instant Text Scale Pop
            if (healthText != null)
            {
                healthText.transform.localScale = textOriginalScale * 1.4f;
            }

            // 2. Heavy Mesh Jitter & Punch Scale
            Vector3 origPos = transform.position;
            float shakeMagnitude = 0.08f;

            if (gateModel != null)
            {
                gateModel.transform.localScale = gateModelOrigScale * 1.06f;
            }

            for (int i = 0; i < 3; i++)
            {
                transform.position = origPos + new Vector3(
                    Random.Range(-shakeMagnitude, shakeMagnitude),
                    Random.Range(-shakeMagnitude * 0.5f, shakeMagnitude * 0.5f),
                    0
                );
                yield return new WaitForSeconds(0.025f);
            }

            transform.position = origPos;

            if (gateModel != null)
            {
                gateModel.transform.localScale = gateModelOrigScale;
            }

            if (healthText != null)
            {
                healthText.transform.localScale = textOriginalScale;
            }
        }

        private bool isTriggered = false;

        private void BreakAndOpenGate()
        {
            if (isTriggered) return;
            isTriggered = true;
            isBroken = true;
            ActiveGates.Remove(this);

            OnGateBroken?.Invoke();
            MobControl.Audio.ZombieSoundManager.Instance?.PlayExplosion(transform.position);

            // Disable all colliders immediately so it never blocks, hits, or triggers again
            foreach (var col in GetComponentsInChildren<Collider>(true))
            {
                col.enabled = false;
            }

            // 1. Immediately spawn bursting explosion particles & sound impact
            SpawnBreakExplosion(transform.position);

            // 2. Trigger punchy haptic feedback on box break!
            TriggerBoxBreakHaptic();

            // 3. Screen shake on box destruction
            if (Camera.main != null)
            {
                Camera.main.GetComponent<MobControl.Core.SmoothCameraFollow>()?.TriggerShake(0.18f);
            }

            // 4. Launch 3D animated flying reward that arcs to the player and triggers arrival VFX + ally spawn
            if (PlayerRunnerController.Instance != null && !PlayerRunnerController.Instance.IsDead)
            {
                FlyingRewardSequence.Launch(transform.position, PlayerRunnerController.Instance, rewardType, multiplierValue, gunSpeedMultiplier);
            }

            // Immediately disable and remove this gate from the scene!
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        private void TriggerBoxBreakHaptic()
        {
            try
            {
                if (ZombieRunnerUIManager.Instance != null && !ZombieRunnerUIManager.Instance.IsVibrationEnabled)
                    return;

#if UNITY_ANDROID || UNITY_IOS
                if (!Application.isEditor) Handheld.Vibrate();
#endif
            }
            catch {}
        }

        private static Material s_sharedExplosionMat;

        private void SpawnBreakExplosion(Vector3 pos)
        {
            GameObject explosionObj = new GameObject("Container_Break_Explosion");
            explosionObj.transform.position = pos + new Vector3(0f, 1.0f, 0f);

            ParticleSystem ps = explosionObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.55f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(5.0f, 11.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.55f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.45f, 0.05f), new Color(1f, 0.95f, 0.25f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = false;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 1.0f;

            var renderer = explosionObj.GetComponent<ParticleSystemRenderer>();
            if (s_sharedExplosionMat == null)
            {
                Shader s = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
                if (s != null) s_sharedExplosionMat = new Material(s);
            }
            if (s_sharedExplosionMat != null) renderer.sharedMaterial = s_sharedExplosionMat;

            ps.Play();
            Destroy(explosionObj, 1.2f);
        }

        private IEnumerator Show05SecPopRoutine()
        {
            GameObject popObj = new GameObject("MultiplierPop_05s");
            Vector3 basePos = transform.position + new Vector3(0f, 2.5f, 0f);
            if (PlayerRunnerController.Instance != null)
            {
                basePos = PlayerRunnerController.Instance.transform.position + new Vector3(0f, 2.6f, 1.0f);
            }
            popObj.transform.position = basePos;
            popObj.transform.rotation = Quaternion.Euler(12f, 0f, 0f);

            TextMeshPro tmp = popObj.AddComponent<TextMeshPro>();
            string symbol = operation == GateOperation.Multiply ? "×" : "+";
            tmp.text = $"<color=#00FFCC><b>{symbol}{multiplierValue}</b></color>";
            tmp.fontSize = 11f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            tmp.outlineWidth = 0.32f;
            tmp.outlineColor = Color.black;
            if (multiplierLabel != null && multiplierLabel.font != null)
            {
                tmp.font = multiplierLabel.font;
            }
            else
            {
                TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/Fredoka-Bold SDF");
                if (font != null) tmp.font = font;
            }

            float elapsed = 0f;
            float duration = 0.50f; // Exactly 0.5 seconds as requested!
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                // Punch scale up to 1.8x then snap down
                float scale = (progress < 0.4f)
                    ? Mathf.Lerp(0.8f, 1.8f, progress / 0.4f)
                    : Mathf.Lerp(1.8f, 1.1f, (progress - 0.4f) / 0.6f);

                popObj.transform.localScale = Vector3.one * (scale * 0.16f);
                popObj.transform.position = basePos + new Vector3(0f, progress * 0.8f, 0f);
                yield return null;
            }

            Destroy(popObj);
        }

        private void EnsureMultiplierTrigger()
        {
            // Destroy any leftover pink quad if it exists
            Transform rogueField = transform.Find("PortalEnergyField");
            if (rogueField != null) Destroy(rogueField.gameObject);

            if (multiplierTrigger == null)
            {
                Transform existing = transform.Find("MultiplierTrigger");
                if (existing != null)
                {
                    Transform oldField = existing.Find("PortalEnergyField");
                    if (oldField != null) Destroy(oldField.gameObject);
                    multiplierTrigger = existing.gameObject;
                }
            }

            if (multiplierTrigger == null)
            {
                GameObject triggerObj = new GameObject("MultiplierTrigger");
                triggerObj.transform.SetParent(transform, false);
                triggerObj.transform.localPosition = Vector3.zero;

                float sx = Mathf.Max(0.001f, Mathf.Abs(transform.lossyScale.x));
                float sy = Mathf.Max(0.001f, Mathf.Abs(transform.lossyScale.y));
                float sz = Mathf.Max(0.001f, Mathf.Abs(transform.lossyScale.z));

                float targetWorldWidth = 2.3f;
                float targetWorldHeight = 3.6f;
                float targetWorldDepth = 1.0f;
                float worldCenterOffsetY = 1.8f - transform.position.y;

                BoxCollider col = triggerObj.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.size = new Vector3(targetWorldWidth / sx, targetWorldHeight / sy, targetWorldDepth / sz);
                col.center = new Vector3(0f, worldCenterOffsetY / sy, 0f);

                multiplierTrigger = triggerObj;
            }

            MultiplierGate mGate = multiplierTrigger.GetComponent<MultiplierGate>();
            if (mGate == null)
            {
                mGate = multiplierTrigger.AddComponent<MultiplierGate>();
            }
            mGate.SetupGate(operation, multiplierValue);

            multiplierTrigger.SetActive(false);
        }

        private IEnumerator SinkGateMesh()
        {
            float elapsed = 0f;
            float duration = 0.5f;
            GameObject targetMesh = gateModel != null ? gateModel : gameObject;
            Vector3 startPos = targetMesh.transform.position;
            Vector3 targetPos = startPos + Vector3.down * 4.5f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                targetMesh.transform.position = Vector3.Lerp(startPos, targetPos, t);
                targetMesh.transform.localScale = Vector3.Lerp(gateModelOrigScale, Vector3.zero, t);
                yield return null;
            }

            if (targetMesh != gameObject)
            {
                targetMesh.SetActive(false);
            }
            else
            {
                Renderer r = GetComponent<Renderer>();
                if (r != null) r.enabled = false;
            }
        }

        private IEnumerator SwingDoorsOpen()
        {
            float elapsed = 0f;
            float duration = 0.5f;

            Quaternion leftStartRot = leftDoor != null ? leftDoor.localRotation : Quaternion.identity;
            Quaternion rightStartRot = rightDoor != null ? rightDoor.localRotation : Quaternion.identity;

            Quaternion leftEndRot = leftStartRot * Quaternion.Euler(0, -90f, 0);
            Quaternion rightEndRot = rightStartRot * Quaternion.Euler(0, 90f, 0);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                if (leftDoor != null) leftDoor.localRotation = Quaternion.Slerp(leftStartRot, leftEndRot, t);
                if (rightDoor != null) rightDoor.localRotation = Quaternion.Slerp(rightStartRot, rightEndRot, t);

                yield return null;
            }
        }
    }
}
