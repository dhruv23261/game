using System.Collections;
using UnityEngine;
using TMPro;
using MobControl.Gates;
using MobControl.UI;

namespace MobControl.Combat
{
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

        [Header("Multiplier Reward Configuration")]
        [Tooltip("The multiplier operation: Multiply (×) or Add (+)")]
        [SerializeField] private GateOperation operation = GateOperation.Multiply;

        [Tooltip("The multiplier or addition value (e.g. 2 for ×2, 3 for ×3, 5 for +5). Customize per gate in Inspector!")]
        [SerializeField] private int multiplierValue = 2;

        [Tooltip("Optional reference to the 3D '×2' text label on this gate. Auto-found if null.")]
        [SerializeField] private TextMeshPro multiplierLabel;

        [SerializeField] private GameObject multiplierTrigger;

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
        public event System.Action OnGateBroken;

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

            float targetWorldWidth = 2.3f; // Covers one lane (2.3m), cleanly separated from other lane
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
                BreakAndOpenGate();
            }
        }

        private void UpdateHealthUI()
        {
            if (healthText == null) return;

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
                string symbol = operation == GateOperation.Multiply ? "×" : "+";
                multiplierLabel.text = $"{symbol}{multiplierValue}";
                multiplierLabel.color = (operation == GateOperation.Multiply)
                    ? new Color(0.1f, 0.95f, 1f, 1f) 
                    : new Color(0.2f, 1f, 0.5f, 1f);
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

        private void BreakAndOpenGate()
        {
            isBroken = true;
            ActiveGates.Remove(this);
            UpdateHealthUI();

            OnGateBroken?.Invoke();

            // Hide 2D Health Bar so it vanishes along with the broken gate
            ThomasDev.HealthSystem.HealthBarUI uiBar = GetComponentInChildren<ThomasDev.HealthSystem.HealthBarUI>();
            if (uiBar != null)
            {
                uiBar.Hide();
            }

            if (breakVfx != null)
            {
                breakVfx.Play();
            }

            if (cachedUiBar != null)
            {
                cachedUiBar.Hide();
            }

            if (boxCollider != null)
            {
                boxCollider.enabled = false;
            }

            if (leftDoor != null && rightDoor != null)
            {
                StartCoroutine(SwingDoorsOpen());
            }
            else
            {
                StartCoroutine(SinkGateMesh());
            }

            if (multiplierTrigger != null)
            {
                multiplierTrigger.SetActive(true);
            }

            // Immediately spawn squad allies based on this gate's configured multiplier value!
            if (PlayerRunnerController.Instance != null && !PlayerRunnerController.Instance.IsDead)
            {
                if (multiplierTrigger != null && multiplierTrigger.TryGetComponent<MultiplierGate>(out var mGate))
                {
                    PlayerRunnerController.Instance.MarkGatePassed(mGate.GateInstanceId);
                }

                int currentSquad = PlayerRunnerController.Instance.TotalSquadSize;
                int targetSquad = (operation == GateOperation.Multiply)
                    ? (currentSquad * multiplierValue)
                    : (currentSquad + multiplierValue);
                int alliesToAdd = Mathf.Max(0, targetSquad - currentSquad);

                PlayerRunnerController.Instance.SpawnSquadAllies(alliesToAdd);

                if (healthText != null)
                {
                    healthText.text = $"<size=120%><color=#00FF66><b>+{alliesToAdd} ALLIES! (SQUAD: {targetSquad})</b></color></size>";
                }
            }

            // Pop and highlight the multiplier label
            if (multiplierLabel != null)
            {
                multiplierLabel.color = new Color(0.2f, 1f, 0.4f, 1f);
                multiplierLabel.transform.localScale = multiplierLabel.transform.localScale * 1.25f;
            }
            else
            {
                TextMeshPro[] texts = GetComponentsInChildren<TextMeshPro>(true);
                foreach (var txt in texts)
                {
                    if (txt != null && txt != healthText)
                    {
                        txt.color = new Color(0.2f, 1f, 0.4f, 1f);
                        txt.transform.localScale = txt.transform.localScale * 1.25f;
                    }
                }
            }
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
