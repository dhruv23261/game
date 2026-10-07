using UnityEngine;
using MobControl.Crowd;
using MobControl.Combat;

namespace MobControl.UI
{
    public class Simple3DHealthBar : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color fullColor = new Color(0.1f, 0.9f, 0.3f); // Neon Green
        [SerializeField] private Color lowColor = new Color(1.0f, 0.15f, 0.25f); // Neon Red

        private static Shader s_unlitShader;
        private static Material s_sharedBgMat;
        private static Material s_sharedFillMat;
        private static MaterialPropertyBlock s_propBlock;

        private Camera mainCam;
        private Transform fillTransform;
        private MeshRenderer fillRend;
        private float maxHp = 10f;
        private float currentHp = 10f;
        private bool isSetup = false;
        private Transform visualRoot;

        private void Start()
        {
            // CRITICAL: NEVER show health bar on Player!
            if (gameObject.name.Contains("PlayerMob") || (transform.parent != null && transform.parent.name.Contains("PlayerMob")))
            {
                Destroy(this);
                return;
            }

            SetupVisuals();
        }

        private void SetupVisuals()
        {
            if (isSetup) return;
            mainCam = Camera.main;

            if (s_unlitShader == null)
            {
                s_unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
                if (s_unlitShader == null)
                    s_unlitShader = Shader.Find("Unlit/Color");
            }

            if (s_sharedBgMat == null && s_unlitShader != null)
            {
                s_sharedBgMat = new Material(s_unlitShader);
                s_sharedBgMat.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
                if (s_sharedBgMat.HasProperty("_BaseColor"))
                    s_sharedBgMat.SetColor("_BaseColor", new Color(0.1f, 0.1f, 0.1f, 0.9f));
            }

            if (s_sharedFillMat == null && s_unlitShader != null)
            {
                s_sharedFillMat = new Material(s_unlitShader);
            }

            if (s_propBlock == null)
            {
                s_propBlock = new MaterialPropertyBlock();
            }

            float pScale = transform.lossyScale.x;
            if (pScale <= 0.001f) pScale = 1.0f;

            // Strict world dimension factor (1.4m width in world space)
            float s = 1.0f / pScale;
            float heightOffset = transform.parent != null && transform.parent.name.Contains("Gate") ? 3.5f : 2.2f;

            GameObject root = new GameObject("HealthBar_Visual");
            root.transform.SetParent(transform);
            root.transform.localPosition = new Vector3(0, heightOffset * s, 0);
            root.transform.localScale = new Vector3(s, s, s);

            // 1. Background Black Quad
            GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bg.name = "BG";
            bg.transform.SetParent(root.transform);
            bg.transform.localPosition = new Vector3(0, 0, 0.01f);
            bg.transform.localScale = new Vector3(1.4f, 0.18f, 1f);
            Destroy(bg.GetComponent<Collider>());
            MeshRenderer bgRend = bg.GetComponent<MeshRenderer>();
            bgRend.sharedMaterial = s_sharedBgMat;

            // 2. Fill Foreground Quad
            GameObject fillPivot = new GameObject("FillPivot");
            fillPivot.transform.SetParent(root.transform);
            fillPivot.transform.localPosition = new Vector3(-0.7f, 0, 0);

            GameObject fill = GameObject.CreatePrimitive(PrimitiveType.Quad);
            fill.name = "Fill";
            fill.transform.SetParent(fillPivot.transform);
            fill.transform.localPosition = new Vector3(0.7f, 0, 0);
            fill.transform.localScale = new Vector3(1.36f, 0.14f, 1f);
            Destroy(fill.GetComponent<Collider>());

            fillTransform = fillPivot.transform;
            fillRend = fill.GetComponent<MeshRenderer>();
            fillRend.sharedMaterial = s_sharedFillMat;

            // Cache visual root reference
            visualRoot = root.transform;

            // HIDE BY DEFAULT until damaged!
            root.SetActive(false);

            isSetup = true;
        }

        private void LateUpdate()
        {
            // Only billboard when visible (skip if bar is hidden)
            if (mainCam != null && visualRoot != null && visualRoot.gameObject.activeInHierarchy)
            {
                transform.forward = mainCam.transform.forward;
            }
        }

        public void SetHealth(float current, float max)
        {
            maxHp = max > 0 ? max : 1f;
            currentHp = Mathf.Clamp(current, 0, maxHp);

            if (!isSetup) SetupVisuals();

            // Only show health bar when damaged!
            if (currentHp < maxHp && currentHp > 0)
            {
                if (visualRoot != null && !visualRoot.gameObject.activeSelf)
                    visualRoot.gameObject.SetActive(true);
            }
            else
            {
                if (visualRoot != null && visualRoot.gameObject.activeSelf)
                    visualRoot.gameObject.SetActive(false);
            }

            float ratio = Mathf.Clamp01(currentHp / maxHp);

            if (fillTransform != null)
            {
                fillTransform.localScale = new Vector3(ratio, 1f, 1f);
            }

            if (fillRend != null)
            {
                Color barColor = Color.Lerp(lowColor, fullColor, ratio);
                s_propBlock.SetColor("_BaseColor", barColor);
                s_propBlock.SetColor("_Color", barColor);
                fillRend.SetPropertyBlock(s_propBlock);
            }

            if (currentHp <= 0)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
