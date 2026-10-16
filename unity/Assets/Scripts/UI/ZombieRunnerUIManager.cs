using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using MobControl.Combat;

namespace MobControl.UI
{
    /// <summary>
    /// ZombieRunnerUIManager
    /// Automatically manages:
    /// 1. Splash Screen with "Zombie Runner" title, animated 🧟 emoji, and smooth Loading Bar.
    /// 2. Level Select Screen with Level 1, 2, 3... unlock progression, stars, and coins.
    /// 3. In-Game HUD (Level name, distance progress bar, health).
    /// 4. Level Complete / Victory Screen with star bounce pop animations & coin rewards.
    /// 5. Defeat / Game Over screen with Retry button.
    ///
    /// Auto-initializes at runtime if no UI is present!
    /// </summary>
    public class ZombieRunnerUIManager : MonoBehaviour
    {
        public static ZombieRunnerUIManager Instance { get; private set; }

        public enum ScreenState
        {
            SplashScreen,
            LevelSelect,
            Gameplay,
            Victory,
            Defeat
        }

        [Header("State")]
        [SerializeField] private ScreenState currentState = ScreenState.SplashScreen;

        [Header("Level Track Settings")]
        [SerializeField] private float levelFinishZ = 120.0f; // End of the zombie street
        [SerializeField] private int activeLevel = 1;
        [SerializeField] private int totalLevels = 6;

        [Header("Splash Settings")]
        [SerializeField] private float splashDurationSeconds = 2.8f;

        // UI Canvas and Panels
        private Canvas mainCanvas;
        private GameObject splashPanel;
        private GameObject levelSelectPanel;
        private GameObject gameplayHudPanel;
        private GameObject victoryPanel;
        private GameObject defeatPanel;

        // Splash Elements
        private RectTransform splashBannerRect;
        private Image splashLoadingFillImage;
        private TextMeshProUGUI splashPercentText;
        private TextMeshProUGUI splashTipText;

        // Defeat Elements
        private TextMeshProUGUI defeatTitleText;
        private TextMeshProUGUI defeatSubText;

        public static bool startAtLevelSelect = false;
        public static bool autoStartMissionOnLoad = false;
        public static int levelToAutoStart = 1;

        // Level Select Elements
        private TextMeshProUGUI coinsText;
        private Transform levelGridContainer;
        private List<GameObject> levelCardObjects = new List<GameObject>();

        // Gameplay HUD Elements
        private TextMeshProUGUI hudLevelText;
        private Image hudProgressBarFill;
        private RectTransform hudProgressBarFillRt;
        private float hudProgressTrackWidth = 460f;
        private TextMeshProUGUI hudHealthText;

        // Victory Elements
        private RectTransform victoryCardRect;
        private TextMeshProUGUI victorySubText;
        private TextMeshProUGUI victoryRewardText;
        private Button victoryNextButton;

        private class StarSlotRef
        {
            public RectTransform slot;
            public Image dimImg;
            public Image goldImg;
            public RectTransform goldRt;
        }
        private List<StarSlotRef> victoryStarSlots = new List<StarSlotRef>();

        // 3D Star Rig Elements
        private RenderTexture starRenderTexture;
        private Camera starCamera;
        private GameObject starRigRoot;
        private List<GameObject> star3DInstances = new List<GameObject>();
        private List<Vector3> star3DBaseScales = new List<Vector3>();
        private RawImage victory3DStarRawImage;

        // Player reference
        private PlayerRunnerController playerRunner;
        private float playerStartZ = 0f;
        private bool isGameActive = false;
        private bool isFinishedHandled = false;
        private bool isSceneReloading = false;

        public bool IsGameActive => isGameActive;

        // Button Rect Transforms for reliable screen-click fallbacks
        private RectTransform defeatRetryRect;
        private RectTransform defeatMenuRect;
        private RectTransform victoryNextRect;
        private RectTransform victoryReplayRect;
        private RectTransform victoryMenuRect;

        private const string PREF_UNLOCKED_LEVEL = "ZombieRunner_UnlockedLevel";
        private const string PREF_COINS = "ZombieRunner_Coins";
        private const string PREF_STARS_PREFIX = "ZombieRunner_Stars_Lvl_";

        public static ZombieRunnerUIManager EnsureInstance()
        {
            if (Instance == null)
            {
                ZombieRunnerUIManager existing = FindAnyObjectByType<ZombieRunnerUIManager>();
                if (existing == null)
                {
                    GameObject uiRoot = new GameObject("[ZombieRunner_UI_Manager]");
                    Instance = uiRoot.AddComponent<ZombieRunnerUIManager>();
                    DontDestroyOnLoad(uiRoot);
                }
                else
                {
                    Instance = existing;
                }
            }
            return Instance;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            EnsureInstance();
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            EnsureEventSystem();
            SceneManager.sceneLoaded += OnSceneLoaded;
            LockPlayer(true);
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        public void EnsureEventSystem()
        {
            UnityEngine.EventSystems.EventSystem es = UnityEngine.EventSystems.EventSystem.current;
            if (es == null)
            {
                es = FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            }

            if (es == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.transform.SetParent(transform);
                es = esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
            else if (es.transform.parent != transform)
            {
                // Parent EventSystem to persistent manager so scene reloads don't destroy it!
                es.transform.SetParent(transform);
            }

            if (es != null)
            {
                es.gameObject.SetActive(true);
                es.enabled = true;
                UnityEngine.EventSystems.StandaloneInputModule module = es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                if (module == null)
                {
                    module = es.gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                }
                module.enabled = true;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            isSceneReloading = false;
            EnsureEventSystem();

            // Re-cache player runner in newly loaded scene
            playerRunner = PlayerRunnerController.Instance != null 
                ? PlayerRunnerController.Instance 
                : FindAnyObjectByType<PlayerRunnerController>();

            if (playerRunner != null)
            {
                playerStartZ = playerRunner.transform.position.z;
            }

            isFinishedHandled = false;

            if (startAtLevelSelect)
            {
                startAtLevelSelect = false;
                ShowLevelSelect();
            }
            else if (autoStartMissionOnLoad)
            {
                autoStartMissionOnLoad = false;
                StartMission(levelToAutoStart);
            }
        }

        private void Start()
        {
            // Cache player
            playerRunner = PlayerRunnerController.Instance;
            if (playerRunner == null)
            {
                playerRunner = FindAnyObjectByType<PlayerRunnerController>();
            }

            if (playerRunner != null)
            {
                playerStartZ = playerRunner.transform.position.z;
                playerRunner.SetControllable(false);
            }

            BuildUIHierarchy();

            if (startAtLevelSelect)
            {
                startAtLevelSelect = false;
                ShowLevelSelect();
            }
            else if (autoStartMissionOnLoad)
            {
                autoStartMissionOnLoad = false;
                StartMission(levelToAutoStart);
            }
            else
            {
                ShowSplashScreen();
            }
        }

        private void Update()
        {
            // Find player if not cached yet
            if (playerRunner == null)
            {
                playerRunner = PlayerRunnerController.Instance;
                if (playerRunner != null)
                {
                    playerStartZ = playerRunner.transform.position.z;
                    if (!isGameActive) playerRunner.SetControllable(false);
                }
            }

            // Keep player locked while in menus
            if (!isGameActive && playerRunner != null && playerRunner.isActiveAndEnabled)
            {
                playerRunner.SetControllable(false);
            }

            // High-reliability touch/mouse fallback for Defeat & Victory buttons
            if (currentState == ScreenState.Defeat && defeatPanel != null && defeatPanel.activeInHierarchy && !isSceneReloading)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Vector2 mousePos = Input.mousePosition;
                    if (defeatRetryRect != null && RectTransformUtility.RectangleContainsScreenPoint(defeatRetryRect, mousePos, null))
                    {
                        Debug.Log("[ZombieRunnerUIManager] Retry button clicked via direct screen click fallback!");
                        OnReplayClicked();
                        return;
                    }
                    else if (defeatMenuRect != null && RectTransformUtility.RectangleContainsScreenPoint(defeatMenuRect, mousePos, null))
                    {
                        Debug.Log("[ZombieRunnerUIManager] Menu button clicked via direct screen click fallback!");
                        OnReturnToMenuClicked();
                        return;
                    }
                }
            }
            else if (currentState == ScreenState.Victory && victoryPanel != null && victoryPanel.activeInHierarchy && !isSceneReloading)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Vector2 mousePos = Input.mousePosition;
                    if (victoryNextRect != null && RectTransformUtility.RectangleContainsScreenPoint(victoryNextRect, mousePos, null))
                    {
                        Debug.Log("[ZombieRunnerUIManager] Victory Next clicked via direct screen click fallback!");
                        OnNextLevelClicked();
                        return;
                    }
                    else if (victoryReplayRect != null && RectTransformUtility.RectangleContainsScreenPoint(victoryReplayRect, mousePos, null))
                    {
                        Debug.Log("[ZombieRunnerUIManager] Victory Replay clicked via direct screen click fallback!");
                        OnReplayClicked();
                        return;
                    }
                    else if (victoryMenuRect != null && RectTransformUtility.RectangleContainsScreenPoint(victoryMenuRect, mousePos, null))
                    {
                        Debug.Log("[ZombieRunnerUIManager] Victory Menu clicked via direct screen click fallback!");
                        OnReturnToMenuClicked();
                        return;
                    }
                }
            }

            // 3D Victory Stars Animation & Studio Rig Management
            if (currentState == ScreenState.Victory && victoryPanel != null && victoryPanel.activeInHierarchy)
            {
                if (starRigRoot != null && !starRigRoot.activeSelf)
                {
                    starRigRoot.SetActive(true);
                }
                if (starCamera != null && !starCamera.enabled)
                {
                    starCamera.enabled = true;
                }

                if (star3DInstances != null && star3DInstances.Count > 0)
                {
                    float rotDelta = 35f * Time.deltaTime;
                    for (int i = 0; i < star3DInstances.Count; i++)
                    {
                        if (star3DInstances[i] != null && star3DInstances[i].activeSelf)
                        {
                            star3DInstances[i].transform.Rotate(0f, rotDelta, 0f, Space.Self);
                        }
                    }
                }
            }
            else
            {
                if (starRigRoot != null && starRigRoot.activeSelf)
                {
                    starRigRoot.SetActive(false);
                }
                if (starCamera != null && starCamera.enabled)
                {
                    starCamera.enabled = false;
                }
            }

            // Gameplay tracking
            if (isGameActive && playerRunner != null && !isFinishedHandled)
            {
                UpdateGameplayHud();

                // Check Victory: Reached end of street
                if (playerRunner.transform.position.z >= levelFinishZ)
                {
                    TriggerVictory();
                }
                // Check Defeat: Player died
                else if (playerRunner.IsDead || playerRunner.Health <= 0)
                {
                    TriggerDefeatWithReason("YOU WERE OVERWHELMED!", "The zombie horde infected your squad.");
                }
                else
                {
                    // Check Defeat: If any zombie has breached past the player runner!
                    MobControl.Crowd.MobUnit[] activeMobs = FindObjectsByType<MobControl.Crowd.MobUnit>();
                    for (int i = 0; i < activeMobs.Length; i++)
                    {
                        MobControl.Crowd.MobUnit m = activeMobs[i];
                        if (m != null && m.Team == MobControl.Crowd.MobTeam.Enemy && !m.IsDead && m.gameObject.activeInHierarchy)
                        {
                            if (m.transform.position.z < playerRunner.transform.position.z - 0.6f)
                            {
                                TriggerDefeatWithReason("GAME OVER", "A zombie breached your defense line!");
                                break;
                            }
                        }
                    }
                }
            }
        }

        public void LockPlayer(bool locked)
        {
            if (playerRunner != null)
            {
                playerRunner.SetControllable(!locked);
            }
        }

        #region UI Builder (Clean Code-Driven Canvas)

        private void BuildUIHierarchy()
        {
            // 1. Ensure Persistent EventSystem
            EnsureEventSystem();

            // 2. Main Canvas
            GameObject canvasObj = new GameObject("ZombieRunnerCanvas");
            canvasObj.transform.SetParent(transform);
            mainCanvas = canvasObj.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mainCanvas.sortingOrder = 999;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            if (Screen.width >= Screen.height)
            {
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
            }
            else
            {
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0.5f;
            }
            scaler.referencePixelsPerUnit = 100;

            GraphicRaycaster gr = canvasObj.AddComponent<GraphicRaycaster>();
            gr.enabled = true;

            // 3. Build Sub-Panels
            BuildSplashPanel(canvasObj.transform);
            BuildLevelSelectPanel(canvasObj.transform);
            BuildGameplayHudPanel(canvasObj.transform);
            BuildVictoryPanel(canvasObj.transform);
            BuildDefeatPanel(canvasObj.transform);
        }

        // Helper: Create Full-Screen Panel
        private GameObject CreateFullPanel(string name, Transform parent, Color bgColor)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            RectTransform rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image img = panel.AddComponent<Image>();
            img.color = bgColor;
            img.raycastTarget = true; // Blocks clicks behind to the 3D scene

            return panel;
        }

        // Helper: Create Text (raycastTarget defaults to false so it never blocks button clicks!)
        private TextMeshProUGUI CreateText(string name, Transform parent, string text, float fontSize, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center, bool bold = false)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false; // Never intercept button clicks
            if (bold) tmp.fontStyle = FontStyles.Bold;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(400, 100);

            return tmp;
        }

        // Helper: Create Styled Button
        private Button CreateButton(string name, Transform parent, string label, Color bgCol, Color textCol, Vector2 size, System.Action onClick)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.sizeDelta = size;

            Image img = btnObj.AddComponent<Image>();
            img.color = bgCol;
            img.raycastTarget = true; // Explicitly ensure raycastTarget is true on button

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.interactable = true;

            ColorBlock cb = btn.colors;
            cb.normalColor = bgCol;
            cb.highlightedColor = bgCol * 1.2f;
            cb.pressedColor = bgCol * 0.8f;
            cb.selectedColor = bgCol;
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.08f;
            btn.colors = cb;

            TextMeshProUGUI txt = CreateText("Label", btnObj.transform, label, 28, textCol, TextAlignmentOptions.Center, true);
            txt.raycastTarget = false; // Label must NEVER steal clicks from the button!
            RectTransform txtRt = txt.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            if (onClick != null)
            {
                btn.onClick.AddListener(() =>
                {
                    Debug.Log($"[ZombieRunnerUIManager] Button '{name}' clicked via Button.onClick");
                    onClick.Invoke();
                });
            }

            return btn;
        }

        #region Star Sprite Generator (High-Res Procedural Graphics)

        private static Sprite s_goldStarSprite;
        private static Sprite s_dimStarSprite;

        public static Sprite GetStarSprite(bool filled)
        {
            if (filled && s_goldStarSprite != null) return s_goldStarSprite;
            if (!filled && s_dimStarSprite != null) return s_dimStarSprite;

            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float outerRadius = size * 0.46f;
            float innerRadius = outerRadius * 0.40f;

            // 10 vertices of a classic upright 5-pointed star
            Vector2[] vertices = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float angle = (Mathf.PI * 0.5f) - i * (Mathf.PI / 5f);
                float r = (i % 2 == 0) ? outerRadius : innerRadius;
                vertices[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
            }

            Color transparent = new Color(0, 0, 0, 0);
            Color goldHighlight = new Color(1f, 0.98f, 0.55f, 1f);
            Color goldBody = new Color(1f, 0.82f, 0.08f, 1f);
            Color goldShadow = new Color(0.96f, 0.52f, 0.02f, 1f);

            Color dimBody = new Color(0.24f, 0.28f, 0.38f, 0.85f);
            Color dimEdge = new Color(0.14f, 0.17f, 0.24f, 0.90f);

            Vector2 highlightCenter = center + new Vector2(0f, outerRadius * 0.2f);

            Color[] pixels = new Color[size * size];
            float[] offsets = new float[] { 0.25f, 0.75f };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // 2x2 supersampling for smooth anti-aliased edges
                    float coverage = 0f;
                    for (int sy = 0; sy < 2; sy++)
                    {
                        for (int sx = 0; sx < 2; sx++)
                        {
                            if (IsPointInStar(x + offsets[sx], y + offsets[sy], vertices))
                            {
                                coverage += 0.25f;
                            }
                        }
                    }

                    if (coverage > 0.001f)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), highlightCenter) / outerRadius;
                        float normDist = Mathf.Clamp01(dist);

                        Color baseCol;
                        if (filled)
                        {
                            baseCol = (normDist < 0.5f) 
                                ? Color.Lerp(goldHighlight, goldBody, normDist / 0.5f)
                                : Color.Lerp(goldBody, goldShadow, (normDist - 0.5f) / 0.5f);
                        }
                        else
                        {
                            baseCol = Color.Lerp(dimBody, dimEdge, normDist);
                        }

                        baseCol.a *= coverage;
                        pixels[y * size + x] = baseCol;
                    }
                    else
                    {
                        pixels[y * size + x] = transparent;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            if (filled) s_goldStarSprite = sprite;
            else s_dimStarSprite = sprite;
            return sprite;
        }

        private static bool IsPointInStar(float px, float py, Vector2[] vertices)
        {
            bool inside = false;
            for (int i = 0, j = vertices.Length - 1; i < vertices.Length; j = i++)
            {
                if (((vertices[i].y > py) != (vertices[j].y > py)) &&
                    (px < (vertices[j].x - vertices[i].x) * (py - vertices[i].y) / (vertices[j].y - vertices[i].y) + vertices[i].x))
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        #endregion

        #region 3D Star Rig (Real 3D FBX Model Integration)

        private static Texture2D LoadGoldStarTexture(string textureFileName)
        {
            try
            {
                string resName = "GoldStar/" + System.IO.Path.GetFileNameWithoutExtension(textureFileName);
                Texture2D resTex = Resources.Load<Texture2D>(resName);
                if (resTex != null) return resTex;

                string fullPath = System.IO.Path.Combine(Application.dataPath, "Resources", "GoldStar", textureFileName);
                if (!System.IO.File.Exists(fullPath))
                {
                    fullPath = System.IO.Path.Combine(Application.dataPath, "Models", "GoldStar", textureFileName);
                }

                if (System.IO.File.Exists(fullPath))
                {
                    byte[] bytes = System.IO.File.ReadAllBytes(fullPath);
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (tex.LoadImage(bytes))
                    {
                        tex.wrapMode = TextureWrapMode.Clamp;
                        tex.filterMode = FilterMode.Bilinear;
                        return tex;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[ZombieRunnerUIManager] LoadGoldStarTexture: " + ex.Message);
            }
            return null;
        }

        private static Mesh CreateProceduralStarMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "Procedural3DStar";

            // 10 perimeter vertices
            Vector3[] perimeter = new Vector3[10];
            for (int i = 0; i < 10; i++)
            {
                float angle = (Mathf.PI * 0.5f) - i * (Mathf.PI / 5f);
                float r = (i % 2 == 0) ? 1.0f : 0.44f;
                perimeter[i] = new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0f);
            }

            Vector3 frontApex = new Vector3(0f, 0f, -0.28f);
            Vector3 backApex = new Vector3(0f, 0f, 0.28f);

            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();

            // Front 10 facets
            for (int i = 0; i < 10; i++)
            {
                int next = (i + 1) % 10;
                int idx = verts.Count;
                verts.Add(frontApex);
                verts.Add(perimeter[i]);
                verts.Add(perimeter[next]);
                tris.Add(idx);
                tris.Add(idx + 1);
                tris.Add(idx + 2);
            }

            // Back 10 facets
            for (int i = 0; i < 10; i++)
            {
                int next = (i + 1) % 10;
                int idx = verts.Count;
                verts.Add(backApex);
                verts.Add(perimeter[next]);
                verts.Add(perimeter[i]);
                tris.Add(idx);
                tris.Add(idx + 1);
                tris.Add(idx + 2);
            }

            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void Ensure3DStarRig()
        {
            if (starRigRoot != null && starCamera != null) return;

            GameObject starPrefab = Resources.Load<GameObject>("GoldStar/gold star");
            if (starPrefab == null)
            {
                starPrefab = Resources.Load<GameObject>("GoldStar/gold star.fbx");
            }

#if UNITY_EDITOR
            if (starPrefab == null)
            {
                starPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/GoldStar/gold star.fbx");
                if (starPrefab == null)
                {
                    starPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/GoldStar/gold star.fbx");
                }
            }
#endif

            // Create persistent 3D rig far away from gameplay road
            starRigRoot = new GameObject("[Victory_3D_Star_Rig]");
            starRigRoot.transform.SetParent(transform);
            starRigRoot.transform.position = new Vector3(800f, 800f, 800f);

            // Create transparent RenderTexture
            if (starRenderTexture == null)
            {
                starRenderTexture = new RenderTexture(720, 240, 16, RenderTextureFormat.ARGB32);
                starRenderTexture.antiAliasing = 4;
            }

            // Create dedicated Camera
            GameObject camObj = new GameObject("StarCamera");
            camObj.transform.SetParent(starRigRoot.transform, false);
            starCamera = camObj.AddComponent<Camera>();
            starCamera.clearFlags = CameraClearFlags.SolidColor;
            starCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            starCamera.targetTexture = starRenderTexture;
            starCamera.fieldOfView = 36f;
            starCamera.nearClipPlane = 0.1f;
            starCamera.farClipPlane = 30f;
            starCamera.transform.localPosition = new Vector3(0f, 0f, -5.5f);
            starCamera.transform.localRotation = Quaternion.identity;
            starCamera.enabled = false; // Enabled only during Victory screen

            // Dedicated Local Studio Lights (Point lights confined strictly to 3D rig at (800,800,800), NEVER Directional!)
            GameObject lightObj = new GameObject("StarKeyLight");
            lightObj.transform.SetParent(starRigRoot.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 1.2f, -3.2f);
            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 8f;
            l.color = new Color(1f, 0.96f, 0.88f);
            l.intensity = 3.5f;

            GameObject fillLightObj = new GameObject("StarFillLight");
            fillLightObj.transform.SetParent(starRigRoot.transform, false);
            fillLightObj.transform.localPosition = new Vector3(0f, -1.2f, -3.2f);
            Light fl = fillLightObj.AddComponent<Light>();
            fl.type = LightType.Point;
            fl.range = 8f;
            fl.color = new Color(0.9f, 0.95f, 1f);
            fl.intensity = 1.8f;

            // Materials
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Diffuse");
            Material goldMat = new Material(litShader);
            Texture2D baseTex = LoadGoldStarTexture("gold star_Star_BaseColor.png");
            if (baseTex != null)
            {
                goldMat.SetTexture("_BaseMap", baseTex);
                goldMat.SetTexture("_MainTex", baseTex);
            }
            Texture2D normTex = LoadGoldStarTexture("gold star_Star_Normal.png");
            if (normTex != null)
            {
                goldMat.SetTexture("_BumpMap", normTex);
                goldMat.EnableKeyword("_NORMALMAP");
            }
            goldMat.SetFloat("_Metallic", 0.95f);
            goldMat.SetFloat("_Smoothness", 0.88f);
            goldMat.SetColor("_BaseColor", Color.white);

            star3DInstances.Clear();
            star3DBaseScales.Clear();

            float[] xOffsets = new float[] { -1.7f, 0f, 1.7f };

            if (starPrefab != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    GameObject sObj = Instantiate(starPrefab, starRigRoot.transform);
                    sObj.name = $"Star3D_{i + 1}";
                    sObj.transform.localPosition = new Vector3(xOffsets[i], 0f, 0f);
                    sObj.transform.localRotation = Quaternion.identity;

                    MeshRenderer[] mrs = sObj.GetComponentsInChildren<MeshRenderer>();
                    float baseScale = 1.0f;
                    if (mrs != null && mrs.Length > 0)
                    {
                        Bounds b = mrs[0].bounds;
                        foreach (var mr in mrs)
                        {
                            mr.material = goldMat;
                            b.Encapsulate(mr.bounds);
                        }
                        float maxDim = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                        if (maxDim > 0.001f)
                        {
                            baseScale = 1.25f / maxDim;
                        }
                    }

                    Vector3 targetScale = Vector3.one * baseScale;
                    sObj.transform.localScale = targetScale;
                    star3DInstances.Add(sObj);
                    star3DBaseScales.Add(targetScale);
                }
            }
            else
            {
                // Procedural 3D Star Fallback (Always renders immediate 3D stars)
                Mesh starMesh = CreateProceduralStarMesh();
                for (int i = 0; i < 3; i++)
                {
                    GameObject sObj = new GameObject($"Star3D_{i + 1}");
                    sObj.transform.SetParent(starRigRoot.transform, false);
                    sObj.transform.localPosition = new Vector3(xOffsets[i], 0f, 0f);
                    sObj.transform.localRotation = Quaternion.identity;

                    MeshFilter mf = sObj.AddComponent<MeshFilter>();
                    mf.sharedMesh = starMesh;
                    MeshRenderer mr = sObj.AddComponent<MeshRenderer>();
                    mr.material = goldMat;

                    Vector3 targetScale = Vector3.one * 1.15f;
                    sObj.transform.localScale = targetScale;
                    star3DInstances.Add(sObj);
                    star3DBaseScales.Add(targetScale);
                }
            }

            // Studio rig stays completely inactive until Victory screen appears!
            starRigRoot.SetActive(false);
        }

        #endregion

        private Sprite LoadSplashBannerSprite()
        {
            try
            {
                Sprite s = Resources.Load<Sprite>("ZombieSplashBanner");
                if (s != null) return s;

                Texture2D resTex = Resources.Load<Texture2D>("ZombieSplashBanner");
                if (resTex != null)
                {
                    return Sprite.Create(resTex, new Rect(0, 0, resTex.width, resTex.height), new Vector2(0.5f, 0.5f));
                }

                string localPath = System.IO.Path.Combine(Application.dataPath, "Resources", "ZombieSplashBanner.jpg");
                if (System.IO.File.Exists(localPath))
                {
                    byte[] fileData = System.IO.File.ReadAllBytes(localPath);
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
                    if (tex.LoadImage(fileData))
                    {
                        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[ZombieRunnerUIManager] Splash banner loading: " + ex.Message);
            }
            return null;
        }

        private void BuildSplashPanel(Transform canvasRoot)
        {
            splashPanel = CreateFullPanel("SplashPanel", canvasRoot, new Color(0.04f, 0.05f, 0.08f, 0.99f));

            // Content Container
            GameObject container = new GameObject("Container");
            container.transform.SetParent(splashPanel.transform, false);
            RectTransform cRt = container.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.5f, 0.5f);
            cRt.anchorMax = new Vector2(0.5f, 0.5f);
            cRt.sizeDelta = new Vector2(900, 800);
            cRt.anchoredPosition = Vector2.zero;

            // Game Title
            TextMeshProUGUI titleText = CreateText("GameTitle", container.transform, "ZOMBIE RUNNER", 54, new Color(0.2f, 0.95f, 0.42f, 1f), TextAlignmentOptions.Center, true);
            RectTransform titleRt = titleText.GetComponent<RectTransform>();
            titleRt.sizeDelta = new Vector2(850, 70);
            titleRt.anchoredPosition = new Vector2(0, 300);

            // Subtitle
            TextMeshProUGUI subText = CreateText("Subtitle", container.transform, "— OUTBREAK SURVIVOR —", 22, new Color(1f, 0.28f, 0.28f, 0.95f), TextAlignmentOptions.Center, true);
            RectTransform subRt = subText.GetComponent<RectTransform>();
            subRt.sizeDelta = new Vector2(850, 35);
            subRt.anchoredPosition = new Vector2(0, 255);

            // Splash Banner Art Card
            GameObject bannerFrame = new GameObject("BannerFrame");
            bannerFrame.transform.SetParent(container.transform, false);
            splashBannerRect = bannerFrame.AddComponent<RectTransform>();
            splashBannerRect.sizeDelta = new Vector2(680, 382); // 16:9 cinematic aspect ratio
            splashBannerRect.anchoredPosition = new Vector2(0, 40);

            Image frameImg = bannerFrame.AddComponent<Image>();
            frameImg.color = new Color(0.15f, 0.22f, 0.32f, 1f);

            GameObject bannerObj = new GameObject("BannerImage");
            bannerObj.transform.SetParent(bannerFrame.transform, false);
            RectTransform bannerRt = bannerObj.AddComponent<RectTransform>();
            bannerRt.anchorMin = Vector2.zero;
            bannerRt.anchorMax = Vector2.one;
            bannerRt.offsetMin = new Vector2(3, 3);
            bannerRt.offsetMax = new Vector2(-3, -3);

            Image bannerImg = bannerObj.AddComponent<Image>();
            Sprite bannerSprite = LoadSplashBannerSprite();
            if (bannerSprite != null)
            {
                bannerImg.sprite = bannerSprite;
                bannerImg.preserveAspect = true;
            }
            else
            {
                bannerImg.color = new Color(0.12f, 0.16f, 0.24f, 1f);
            }

            // Loading Bar Background Track
            GameObject barBg = new GameObject("LoadingBarBg");
            barBg.transform.SetParent(container.transform, false);
            RectTransform barBgRt = barBg.AddComponent<RectTransform>();
            barBgRt.sizeDelta = new Vector2(640, 24);
            barBgRt.anchoredPosition = new Vector2(0, -185);
            Image barBgImg = barBg.AddComponent<Image>();
            barBgImg.color = new Color(0.12f, 0.16f, 0.22f, 1f);

            // Loading Bar Fill
            GameObject barFill = new GameObject("LoadingBarFill");
            barFill.transform.SetParent(barBg.transform, false);
            RectTransform fillRt = barFill.AddComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0, 0);
            fillRt.anchorMax = new Vector2(1, 1);
            fillRt.offsetMin = new Vector2(2, 2);
            fillRt.offsetMax = new Vector2(-2, -2);
            splashLoadingFillImage = barFill.AddComponent<Image>();
            splashLoadingFillImage.type = Image.Type.Filled;
            splashLoadingFillImage.fillMethod = Image.FillMethod.Horizontal;
            splashLoadingFillImage.fillAmount = 0f;
            splashLoadingFillImage.color = new Color(0.2f, 0.95f, 0.45f, 1f);

            // Loading Percentage Text
            splashPercentText = CreateText("PercentText", container.transform, "0%", 22, Color.white, TextAlignmentOptions.Center, true);
            RectTransform pRt = splashPercentText.GetComponent<RectTransform>();
            pRt.anchoredPosition = new Vector2(0, -225);

            // Tips Text
            splashTipText = CreateText("TipText", container.transform, "Scanning infected highway sector...", 18, new Color(0.7f, 0.8f, 0.9f, 0.85f));
            RectTransform tipRt = splashTipText.GetComponent<RectTransform>();
            tipRt.sizeDelta = new Vector2(750, 40);
            tipRt.anchoredPosition = new Vector2(0, -260);
        }

        private void BuildLevelSelectPanel(Transform canvasRoot)
        {
            levelSelectPanel = CreateFullPanel("LevelSelectPanel", canvasRoot, new Color(0.08f, 0.10f, 0.15f, 0.96f));

            // Top Bar
            GameObject topBar = new GameObject("TopBar");
            topBar.transform.SetParent(levelSelectPanel.transform, false);
            RectTransform tbRt = topBar.AddComponent<RectTransform>();
            tbRt.anchorMin = new Vector2(0, 1);
            tbRt.anchorMax = new Vector2(1, 1);
            tbRt.pivot = new Vector2(0.5f, 1);
            tbRt.sizeDelta = new Vector2(0, 140);
            tbRt.anchoredPosition = Vector2.zero;
            Image tbImg = topBar.AddComponent<Image>();
            tbImg.color = new Color(0.12f, 0.15f, 0.22f, 1f);

            // Currency Text
            coinsText = CreateText("CoinsText", topBar.transform, "COINS: 250   |   GEMS: 15", 26, new Color(1f, 0.85f, 0.2f, 1f), TextAlignmentOptions.Right, true);
            RectTransform ctRt = coinsText.GetComponent<RectTransform>();
            ctRt.anchorMin = new Vector2(1, 0.5f);
            ctRt.anchorMax = new Vector2(1, 0.5f);
            ctRt.pivot = new Vector2(1, 0.5f);
            ctRt.sizeDelta = new Vector2(400, 60);
            ctRt.anchoredPosition = new Vector2(-40, 0);

            // Title
            TextMeshProUGUI msTitle = CreateText("HeaderTitle", topBar.transform, "SELECT MISSION", 34, Color.white, TextAlignmentOptions.Left, true);
            RectTransform mtRt = msTitle.GetComponent<RectTransform>();
            mtRt.anchorMin = new Vector2(0, 0.5f);
            mtRt.anchorMax = new Vector2(0, 0.5f);
            mtRt.pivot = new Vector2(0, 0.5f);
            mtRt.sizeDelta = new Vector2(500, 60);
            mtRt.anchoredPosition = new Vector2(40, 0);

            // Grid Container
            GameObject grid = new GameObject("LevelGrid");
            grid.transform.SetParent(levelSelectPanel.transform, false);
            RectTransform gRt = grid.AddComponent<RectTransform>();
            gRt.anchorMin = new Vector2(0.5f, 0.5f);
            gRt.anchorMax = new Vector2(0.5f, 0.5f);
            gRt.sizeDelta = new Vector2(800, 950);
            gRt.anchoredPosition = new Vector2(0, -40);

            VerticalLayoutGroup vlg = grid.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 24;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            levelGridContainer = grid.transform;
        }

        private void BuildGameplayHudPanel(Transform canvasRoot)
        {
            gameplayHudPanel = CreateFullPanel("GameplayHud", canvasRoot, Color.clear);
            gameplayHudPanel.GetComponent<Image>().raycastTarget = false;

            // Top HUD Bar (Sleek dark translucent pill)
            GameObject hudBar = new GameObject("HudBar");
            hudBar.transform.SetParent(gameplayHudPanel.transform, false);
            RectTransform hRt = hudBar.AddComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0.5f, 1f);
            hRt.anchorMax = new Vector2(0.5f, 1f);
            hRt.pivot = new Vector2(0.5f, 1f);
            hRt.sizeDelta = new Vector2(560, 68);
            hRt.anchoredPosition = new Vector2(0, -18);
            Image hImg = hudBar.AddComponent<Image>();
            hImg.color = new Color(0.08f, 0.11f, 0.17f, 0.88f);
            hImg.raycastTarget = false;

            hudLevelText = CreateText("LevelName", hudBar.transform, "MISSION 1: OUTBREAK STREET", 22, Color.white, TextAlignmentOptions.Center, true);
            RectTransform lRt = hudLevelText.GetComponent<RectTransform>();
            lRt.anchoredPosition = new Vector2(0, 10);

            // Progress Bar Track (Slim elegant track)
            GameObject pbBg = new GameObject("ProgressTrack");
            pbBg.transform.SetParent(hudBar.transform, false);
            RectTransform pbRt = pbBg.AddComponent<RectTransform>();
            hudProgressTrackWidth = 460f;
            pbRt.sizeDelta = new Vector2(hudProgressTrackWidth, 8);
            pbRt.anchoredPosition = new Vector2(0, -16);
            Image pbImg = pbBg.AddComponent<Image>();
            pbImg.color = new Color(0.18f, 0.22f, 0.32f, 0.9f);
            pbImg.raycastTarget = false;

            // Progress Bar Fill (Starts at 0 width, expands cleanly with player progress)
            GameObject pbFill = new GameObject("ProgressFill");
            pbFill.transform.SetParent(pbBg.transform, false);
            hudProgressBarFillRt = pbFill.AddComponent<RectTransform>();
            hudProgressBarFillRt.anchorMin = new Vector2(0f, 0f);
            hudProgressBarFillRt.anchorMax = new Vector2(0f, 1f);
            hudProgressBarFillRt.pivot = new Vector2(0f, 0.5f);
            hudProgressBarFillRt.offsetMin = Vector2.zero;
            hudProgressBarFillRt.offsetMax = Vector2.zero;
            hudProgressBarFillRt.sizeDelta = new Vector2(0f, 0f); // Starts completely empty (0 width)!
            hudProgressBarFill = pbFill.AddComponent<Image>();
            hudProgressBarFill.color = new Color(0.2f, 0.9f, 0.45f, 1f);
            hudProgressBarFill.raycastTarget = false;

            // Health Badge (Top-Left sleek dark pill badge)
            GameObject hpBadge = new GameObject("HpBadge");
            hpBadge.transform.SetParent(gameplayHudPanel.transform, false);
            RectTransform hpRt = hpBadge.AddComponent<RectTransform>();
            hpRt.anchorMin = new Vector2(0f, 1f);
            hpRt.anchorMax = new Vector2(0f, 1f);
            hpRt.pivot = new Vector2(0f, 1f);
            hpRt.sizeDelta = new Vector2(170, 48);
            hpRt.anchoredPosition = new Vector2(25, -20);
            Image hpImg = hpBadge.AddComponent<Image>();
            hpImg.color = new Color(0.08f, 0.11f, 0.17f, 0.85f);
            hpImg.raycastTarget = false;

            hudHealthText = CreateText("HealthText", hpBadge.transform, "HP: 3 / 3", 20, new Color(1f, 0.35f, 0.35f, 1f), TextAlignmentOptions.Center, true);
            RectTransform htRt = hudHealthText.GetComponent<RectTransform>();
            htRt.anchoredPosition = Vector2.zero;
        }

        private void BuildVictoryPanel(Transform canvasRoot)
        {
            victoryPanel = CreateFullPanel("VictoryPanel", canvasRoot, new Color(0.05f, 0.08f, 0.12f, 0.94f));

            // Card
            GameObject card = new GameObject("VictoryCard");
            card.transform.SetParent(victoryPanel.transform, false);
            victoryCardRect = card.AddComponent<RectTransform>();
            victoryCardRect.sizeDelta = new Vector2(760, 800);
            victoryCardRect.anchoredPosition = Vector2.zero;
            Image cImg = card.AddComponent<Image>();
            cImg.color = new Color(0.12f, 0.16f, 0.24f, 1f);
            cImg.raycastTarget = false; // Don't block buttons

            // Title
            TextMeshProUGUI vTitle = CreateText("VictoryTitle", card.transform, "MISSION CLEARED!", 44, new Color(1f, 0.85f, 0.2f, 1f), TextAlignmentOptions.Center, true);
            RectTransform vtRt = vTitle.GetComponent<RectTransform>();
            vtRt.anchoredPosition = new Vector2(0, 280);

            victorySubText = CreateText("VictorySub", card.transform, "Area Cleared of All Zombies!", 22, new Color(0.75f, 0.85f, 0.95f, 0.8f));
            RectTransform vsRt = victorySubText.GetComponent<RectTransform>();
            vsRt.anchoredPosition = new Vector2(0, 220);

            // Stars Row (High-resolution procedural graphic stars)
            GameObject starsRow = new GameObject("StarsRow");
            starsRow.transform.SetParent(card.transform, false);
            RectTransform srRt = starsRow.AddComponent<RectTransform>();
            srRt.sizeDelta = new Vector2(420, 110);
            srRt.anchoredPosition = new Vector2(0, 120);

            HorizontalLayoutGroup hlg = starsRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 30;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            victoryStarSlots.Clear();
            for (int i = 0; i < 3; i++)
            {
                GameObject slot = new GameObject($"StarSlot_{i + 1}");
                slot.transform.SetParent(starsRow.transform, false);
                RectTransform slotRt = slot.AddComponent<RectTransform>();
                slotRt.sizeDelta = new Vector2(96, 96);

                // 1. Dim background star silhouette (Empty slot)
                GameObject dimObj = new GameObject("DimStar");
                dimObj.transform.SetParent(slot.transform, false);
                RectTransform dimRt = dimObj.AddComponent<RectTransform>();
                dimRt.anchorMin = Vector2.zero;
                dimRt.anchorMax = Vector2.one;
                dimRt.offsetMin = Vector2.zero;
                dimRt.offsetMax = Vector2.zero;
                Image dimImg = dimObj.AddComponent<Image>();
                dimImg.sprite = GetStarSprite(false);
                dimImg.raycastTarget = false;

                // 2. Shiny Gold Star (Pop-in celebration)
                GameObject goldObj = new GameObject("GoldStar");
                goldObj.transform.SetParent(slot.transform, false);
                RectTransform goldRt = goldObj.AddComponent<RectTransform>();
                goldRt.anchorMin = Vector2.zero;
                goldRt.anchorMax = Vector2.one;
                goldRt.offsetMin = Vector2.zero;
                goldRt.offsetMax = Vector2.zero;
                Image goldImg = goldObj.AddComponent<Image>();
                goldImg.sprite = GetStarSprite(true);
                goldImg.raycastTarget = false;

                victoryStarSlots.Add(new StarSlotRef
                {
                    slot = slotRt,
                    dimImg = dimImg,
                    goldImg = goldImg,
                    goldRt = goldRt
                });
            }

            // 3D Star Overlay (Displaying the real-time spinning 3D Gold Star rig)
            Ensure3DStarRig();
            if (starRenderTexture != null)
            {
                GameObject rawImgObj = new GameObject("Victory3DStars");
                rawImgObj.transform.SetParent(card.transform, false);
                RectTransform rt3D = rawImgObj.AddComponent<RectTransform>();
                rt3D.sizeDelta = new Vector2(500, 150);
                rt3D.anchoredPosition = new Vector2(0, 120);

                victory3DStarRawImage = rawImgObj.AddComponent<RawImage>();
                victory3DStarRawImage.texture = starRenderTexture;
                victory3DStarRawImage.color = Color.white;
                victory3DStarRawImage.raycastTarget = false;
            }

            // Reward
            victoryRewardText = CreateText("RewardText", card.transform, "+100 COINS", 32, new Color(0.3f, 0.95f, 0.5f, 1f), TextAlignmentOptions.Center, true);
            RectTransform vrRt = victoryRewardText.GetComponent<RectTransform>();
            vrRt.anchoredPosition = new Vector2(0, -10);

            // Buttons
            victoryNextButton = CreateButton("NextButton", card.transform, "NEXT MISSION >", new Color(0.15f, 0.75f, 0.35f, 1f), Color.white, new Vector2(520, 80), OnNextLevelClicked);
            victoryNextRect = victoryNextButton.GetComponent<RectTransform>();
            victoryNextRect.anchoredPosition = new Vector2(0, -140);

            Button replayBtn = CreateButton("ReplayButton", card.transform, "REPLAY MISSION", new Color(0.25f, 0.3f, 0.42f, 1f), Color.white, new Vector2(520, 70), OnReplayClicked);
            victoryReplayRect = replayBtn.GetComponent<RectTransform>();
            victoryReplayRect.anchoredPosition = new Vector2(0, -230);

            Button menuBtn = CreateButton("MenuButton", card.transform, "MISSION MENU", new Color(0.2f, 0.22f, 0.3f, 1f), Color.white, new Vector2(520, 60), OnReturnToMenuClicked);
            victoryMenuRect = menuBtn.GetComponent<RectTransform>();
            victoryMenuRect.anchoredPosition = new Vector2(0, -310);
        }

        private void BuildDefeatPanel(Transform canvasRoot)
        {
            defeatPanel = CreateFullPanel("DefeatPanel", canvasRoot, new Color(0.12f, 0.05f, 0.06f, 0.94f));

            // Card
            GameObject card = new GameObject("DefeatCard");
            card.transform.SetParent(defeatPanel.transform, false);
            RectTransform dRt = card.AddComponent<RectTransform>();
            dRt.sizeDelta = new Vector2(740, 640);
            dRt.anchoredPosition = Vector2.zero;
            Image cImg = card.AddComponent<Image>();
            cImg.color = new Color(0.2f, 0.1f, 0.12f, 1f);
            cImg.raycastTarget = false; // Don't block button clicks

            // Title
            defeatTitleText = CreateText("DefeatTitle", card.transform, "GAME OVER", 40, new Color(1f, 0.3f, 0.35f, 1f), TextAlignmentOptions.Center, true);
            defeatTitleText.raycastTarget = false;
            RectTransform dtRt = defeatTitleText.GetComponent<RectTransform>();
            dtRt.anchoredPosition = new Vector2(0, 180);

            defeatSubText = CreateText("DefeatSub", card.transform, "A zombie breached your defense line!", 22, new Color(0.85f, 0.75f, 0.75f, 0.8f));
            defeatSubText.raycastTarget = false;
            RectTransform dsRt = defeatSubText.GetComponent<RectTransform>();
            dsRt.anchoredPosition = new Vector2(0, 110);

            // Buttons
            Button retryBtn = CreateButton("RetryButton", card.transform, "TRY AGAIN", new Color(0.85f, 0.25f, 0.3f, 1f), Color.white, new Vector2(500, 80), OnReplayClicked);
            defeatRetryRect = retryBtn.GetComponent<RectTransform>();
            defeatRetryRect.anchoredPosition = new Vector2(0, -40);

            Button menuBtn = CreateButton("MenuButton", card.transform, "MISSION MENU", new Color(0.3f, 0.3f, 0.35f, 1f), Color.white, new Vector2(500, 70), OnReturnToMenuClicked);
            defeatMenuRect = menuBtn.GetComponent<RectTransform>();
            defeatMenuRect.anchoredPosition = new Vector2(0, -140);
        }

        #endregion

        #region Screens Flow & Transitions

        public void ShowSplashScreen()
        {
            currentState = ScreenState.SplashScreen;
            isGameActive = false;
            LockPlayer(true);

            splashPanel.SetActive(true);
            levelSelectPanel.SetActive(false);
            gameplayHudPanel.SetActive(false);
            victoryPanel.SetActive(false);
            defeatPanel.SetActive(false);
            if (starRigRoot != null) starRigRoot.SetActive(false);
            if (starCamera != null) starCamera.enabled = false;

            StartCoroutine(AnimateSplashScreen());
        }

        private IEnumerator AnimateSplashScreen()
        {
            float elapsed = 0f;
            string[] tips = new string[]
            {
                "Scanning infected sector...",
                "Loading rifle ammunition...",
                "Locking multiplier gates...",
                "Ready for battle!"
            };

            while (elapsed < splashDurationSeconds)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / splashDurationSeconds);

                // Update loading bar
                if (splashLoadingFillImage != null)
                {
                    splashLoadingFillImage.fillAmount = progress;
                }

                // Update percentage
                if (splashPercentText != null)
                {
                    splashPercentText.text = $"{(int)(progress * 100)}%";
                }

                // Rotate tip text
                if (splashTipText != null)
                {
                    int tipIndex = Mathf.Clamp((int)(progress * tips.Length), 0, tips.Length - 1);
                    splashTipText.text = tips[tipIndex];
                }

                // Subtle cinematic breathing pulse on banner
                if (splashBannerRect != null)
                {
                    float scale = 1f + 0.02f * Mathf.Sin(elapsed * 4f);
                    splashBannerRect.localScale = new Vector3(scale, scale, 1f);
                }

                yield return null;
            }

            yield return new WaitForSeconds(0.3f);
            ShowLevelSelect();
        }

        public void ShowLevelSelect()
        {
            currentState = ScreenState.LevelSelect;
            isGameActive = false;
            LockPlayer(true);

            splashPanel.SetActive(false);
            levelSelectPanel.SetActive(true);
            gameplayHudPanel.SetActive(false);
            victoryPanel.SetActive(false);
            defeatPanel.SetActive(false);
            if (starRigRoot != null) starRigRoot.SetActive(false);
            if (starCamera != null) starCamera.enabled = false;

            UpdateCurrencyHud();
            PopulateLevelGrid();
        }

        private void PopulateLevelGrid()
        {
            // Clear existing cards
            foreach (var card in levelCardObjects)
            {
                if (card != null) Destroy(card);
            }
            levelCardObjects.Clear();

            int unlockedLevel = PlayerPrefs.GetInt(PREF_UNLOCKED_LEVEL, 1);

            string[] missionNames = new string[]
            {
                "OUTBREAK STREET",
                "IRON GATES RUN",
                "HORDE RUSH",
                "TOXIC INFESTATION",
                "MUTANT CROSSING",
                "THE FINAL ESCAPE"
            };

            for (int i = 1; i <= totalLevels; i++)
            {
                int lvlNum = i;
                bool isUnlocked = (lvlNum <= unlockedLevel);
                string mName = (i - 1 < missionNames.Length) ? missionNames[i - 1] : $"SECTOR {i}";
                int stars = PlayerPrefs.GetInt(PREF_STARS_PREFIX + lvlNum, 0);

                GameObject card = BuildLevelCard(lvlNum, mName, isUnlocked, stars);
                card.transform.SetParent(levelGridContainer, false);
                levelCardObjects.Add(card);
            }
        }

        private GameObject BuildLevelCard(int levelNum, string missionName, bool isUnlocked, int stars)
        {
            GameObject cardObj = new GameObject($"LevelCard_{levelNum}");
            RectTransform rt = cardObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(760, 115);

            Image bg = cardObj.AddComponent<Image>();
            bg.color = isUnlocked 
                ? new Color(0.14f, 0.18f, 0.28f, 1f) 
                : new Color(0.12f, 0.14f, 0.18f, 0.6f);

            // Left Number & Title
            string titleText = isUnlocked 
                ? $"<b>LEVEL {levelNum}</b> — <color=#66CCFF>{missionName}</color>" 
                : $"<b>LEVEL {levelNum}</b> — <color=#777777>[LOCKED]</color>";

            TextMeshProUGUI label = CreateText("Title", cardObj.transform, titleText, 24, Color.white, TextAlignmentOptions.Left, false);
            RectTransform lRt = label.GetComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0, 0.5f);
            lRt.anchorMax = new Vector2(0, 0.5f);
            lRt.pivot = new Vector2(0, 0.5f);
            lRt.sizeDelta = new Vector2(440, 50);
            lRt.anchoredPosition = new Vector2(25, 12);

            // Stars Icons
            GameObject starsRow = new GameObject("Stars");
            starsRow.transform.SetParent(cardObj.transform, false);
            RectTransform sRt = starsRow.AddComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0, 0.5f);
            sRt.anchorMax = new Vector2(0, 0.5f);
            sRt.pivot = new Vector2(0, 0.5f);
            sRt.sizeDelta = new Vector2(100, 24);
            sRt.anchoredPosition = new Vector2(25, -22);

            HorizontalLayoutGroup sHlg = starsRow.AddComponent<HorizontalLayoutGroup>();
            sHlg.spacing = 6;
            sHlg.childAlignment = TextAnchor.MiddleLeft;
            sHlg.childControlWidth = false;
            sHlg.childControlHeight = false;

            if (isUnlocked)
            {
                for (int s = 0; s < 3; s++)
                {
                    GameObject sObj = new GameObject($"Star_{s + 1}");
                    sObj.transform.SetParent(starsRow.transform, false);
                    RectTransform sSlotRt = sObj.AddComponent<RectTransform>();
                    sSlotRt.sizeDelta = new Vector2(22, 22);

                    Image sImg = sObj.AddComponent<Image>();
                    sImg.sprite = GetStarSprite(s < stars);
                    sImg.raycastTarget = false;
                }
            }

            // Right Action Button
            if (isUnlocked)
            {
                Button btn = CreateButton("PlayBtn", cardObj.transform, "PLAY >", new Color(0.15f, 0.75f, 0.35f, 1f), Color.white, new Vector2(170, 65), () =>
                {
                    StartMission(levelNum);
                });
                RectTransform bRt = btn.GetComponent<RectTransform>();
                bRt.anchorMin = new Vector2(1, 0.5f);
                bRt.anchorMax = new Vector2(1, 0.5f);
                bRt.pivot = new Vector2(1, 0.5f);
                bRt.anchoredPosition = new Vector2(-20, 0);
            }
            else
            {
                TextMeshProUGUI lockTxt = CreateText("LockedTxt", cardObj.transform, "LOCKED", 22, new Color(0.5f, 0.55f, 0.65f, 0.8f), TextAlignmentOptions.Center, true);
                RectTransform lkRt = lockTxt.GetComponent<RectTransform>();
                lkRt.anchorMin = new Vector2(1, 0.5f);
                lkRt.anchorMax = new Vector2(1, 0.5f);
                lkRt.pivot = new Vector2(1, 0.5f);
                lkRt.sizeDelta = new Vector2(170, 65);
                lkRt.anchoredPosition = new Vector2(-20, 0);
            }

            return cardObj;
        }

        public void StartMission(int levelNumber)
        {
            activeLevel = levelNumber;
            currentState = ScreenState.Gameplay;
            isGameActive = true;
            isFinishedHandled = false;

            splashPanel.SetActive(false);
            levelSelectPanel.SetActive(false);
            gameplayHudPanel.SetActive(true);
            victoryPanel.SetActive(false);
            defeatPanel.SetActive(false);

            if (hudLevelText != null)
            {
                hudLevelText.text = $"MISSION {levelNumber}: OUTBREAK STREET";
            }

            if (hudProgressBarFillRt != null)
            {
                hudProgressBarFillRt.sizeDelta = new Vector2(0f, 0f);
            }

            if (starRigRoot != null)
            {
                starRigRoot.SetActive(false);
            }

            // Unlock player to start running and shooting!
            LockPlayer(false);
        }

        private void UpdateGameplayHud()
        {
            if (playerRunner == null) return;

            // Update Progress Bar
            float currentZ = playerRunner.transform.position.z;
            float totalDist = Mathf.Max(1f, levelFinishZ - playerStartZ);
            float currentDist = Mathf.Clamp(currentZ - playerStartZ, 0f, totalDist);
            float progress = Mathf.Clamp01(currentDist / totalDist);

            if (hudProgressBarFillRt != null)
            {
                hudProgressBarFillRt.sizeDelta = new Vector2(hudProgressTrackWidth * progress, 0f);
            }

            // Update Health Text
            if (hudHealthText != null)
            {
                int hp = Mathf.Max(0, playerRunner.Health);
                hudHealthText.text = $"HP: {hp} / {playerRunner.MaxHealth}";
            }
        }

        #endregion

        #region Victory & Animations

        public void TriggerVictory()
        {
            if (isFinishedHandled) return;
            isFinishedHandled = true;
            isGameActive = false;
            currentState = ScreenState.Victory;

            Debug.Log("[ZombieRunnerUIManager] Victory triggered!");

            // Stop player from running further
            LockPlayer(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            EnsureEventSystem();

            Ensure3DStarRig();
            if (starRigRoot != null) starRigRoot.SetActive(true);
            if (starCamera != null) starCamera.enabled = true;

            if (gameplayHudPanel != null) gameplayHudPanel.SetActive(false);
            if (defeatPanel != null) defeatPanel.SetActive(false);

            // 1. Calculate Stars based on remaining player HP:
            // 3 HP -> 3 Stars
            // 2 HP -> 2 Stars
            // 1 HP -> 1 Star
            int currentHealth = (playerRunner != null) ? playerRunner.Health : 3;
            int starsEarned = Mathf.Clamp(currentHealth, 1, 3);

            // 2. Calculate Coins reward based on Stars:
            // 1 Star  -> 50 Coins
            // 2 Stars -> 75 Coins
            // 3 Stars -> 100 Coins
            int coinsEarned = 100;
            if (starsEarned == 1) coinsEarned = 50;
            else if (starsEarned == 2) coinsEarned = 75;
            else if (starsEarned == 3) coinsEarned = 100;

            Debug.Log($"[ZombieRunnerUIManager] Victory Stats: HP={currentHealth}, Stars={starsEarned}, Coins={coinsEarned}");

            // Award coins to player wallet
            int currentCoins = PlayerPrefs.GetInt(PREF_COINS, 250);
            currentCoins += coinsEarned;
            PlayerPrefs.SetInt(PREF_COINS, currentCoins);

            // Unlock next level
            int unlockedLevel = PlayerPrefs.GetInt(PREF_UNLOCKED_LEVEL, 1);
            if (activeLevel >= unlockedLevel && activeLevel < totalLevels)
            {
                PlayerPrefs.SetInt(PREF_UNLOCKED_LEVEL, activeLevel + 1);
            }

            // Save highest stars earned for this level
            int prevStars = PlayerPrefs.GetInt(PREF_STARS_PREFIX + activeLevel, 0);
            if (starsEarned > prevStars)
            {
                PlayerPrefs.SetInt(PREF_STARS_PREFIX + activeLevel, starsEarned);
            }
            PlayerPrefs.Save();

            // Set dynamic subtitle text
            if (victorySubText != null)
            {
                if (starsEarned == 3) victorySubText.text = "PERFECT RUN! (3/3 HP)";
                else if (starsEarned == 2) victorySubText.text = "GREAT COMBAT! (2/3 HP)";
                else victorySubText.text = "SURVIVED! (1/3 HP)";
            }

            // Show victory panel with animations
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);
                victoryPanel.transform.SetAsLastSibling();
            }
            StartCoroutine(AnimateVictoryCelebration(starsEarned, coinsEarned));
        }

        private IEnumerator AnimateVictoryCelebration(int starsEarned, int coinsEarned)
        {
            // Reset star visuals before animation begins
            for (int i = 0; i < victoryStarSlots.Count; i++)
            {
                var slot = victoryStarSlots[i];
                if (slot == null) continue;
                if (slot.dimImg != null) slot.dimImg.gameObject.SetActive(true);
                if (slot.goldImg != null)
                {
                    slot.goldImg.gameObject.SetActive(false);
                    if (slot.goldRt != null) slot.goldRt.localScale = Vector3.zero;
                }
            }

            // Also reset 3D star instances
            if (star3DInstances != null)
            {
                for (int i = 0; i < star3DInstances.Count; i++)
                {
                    if (star3DInstances[i] != null)
                    {
                        star3DInstances[i].SetActive(false);
                        star3DInstances[i].transform.localScale = Vector3.zero;
                    }
                }
            }

            if (victoryRewardText != null)
            {
                victoryRewardText.text = "+0 COINS";
            }

            // 1. Bounce-in Victory Card (Scale punch effect)
            if (victoryCardRect != null)
            {
                victoryCardRect.localScale = Vector3.zero;
                float t = 0f;
                while (t < 0.35f)
                {
                    t += Time.deltaTime;
                    float prog = t / 0.35f;
                    float scale = Mathf.Sin(prog * Mathf.PI * 0.5f) * 1.08f;
                    if (prog >= 0.8f) scale = Mathf.Lerp(1.08f, 1.0f, (prog - 0.8f) / 0.2f);
                    victoryCardRect.localScale = new Vector3(scale, scale, 1f);
                    yield return null;
                }
                victoryCardRect.localScale = Vector3.one;
            }

            yield return new WaitForSeconds(0.15f);

            // 2. Animate Star 1, 2, 3 popping in one by one with punch bounce!
            for (int i = 0; i < 3; i++)
            {
                if (i >= starsEarned) break; // Unearned stars remain as dim silhouette

                var slot = (i < victoryStarSlots.Count) ? victoryStarSlots[i] : null;
                if (slot != null && slot.goldImg != null)
                {
                    slot.goldImg.gameObject.SetActive(true);
                }

                GameObject star3D = (i < star3DInstances.Count) ? star3DInstances[i] : null;
                Vector3 baseScale = (i < star3DBaseScales.Count) ? star3DBaseScales[i] : Vector3.one;
                if (star3D != null)
                {
                    star3D.SetActive(true);
                    star3D.transform.localScale = Vector3.zero;
                }

                float starTime = 0f;
                while (starTime < 0.28f)
                {
                    starTime += Time.deltaTime;
                    float p = starTime / 0.28f;
                    float sScale = Mathf.Sin(p * Mathf.PI * 0.7f) * 1.35f;
                    if (p >= 0.7f) sScale = Mathf.Lerp(1.35f, 1f, (p - 0.7f) / 0.3f);

                    if (slot != null && slot.goldRt != null)
                    {
                        slot.goldRt.localScale = new Vector3(sScale, sScale, 1f);
                    }
                    if (star3D != null)
                    {
                        star3D.transform.localScale = baseScale * sScale;
                    }
                    yield return null;
                }

                if (slot != null && slot.goldRt != null) slot.goldRt.localScale = Vector3.one;
                if (star3D != null) star3D.transform.localScale = baseScale;

                yield return new WaitForSeconds(0.12f);
            }

            yield return new WaitForSeconds(0.1f);

            // 3. Count up reward coins to coinsEarned (50, 75, or 100)
            if (victoryRewardText != null)
            {
                int step = (coinsEarned == 75) ? 5 : 10;
                for (int c = 0; c <= coinsEarned; c += step)
                {
                    victoryRewardText.text = $"+{c} COINS";
                    yield return new WaitForSeconds(0.035f);
                }
                victoryRewardText.text = $"+{coinsEarned} COINS";
            }
        }

        public void TriggerDefeatWithReason(string title = "GAME OVER", string subtitle = "Mission Failed")
        {
            if (isFinishedHandled) return;
            isFinishedHandled = true;
            isGameActive = false;
            currentState = ScreenState.Defeat;

            Debug.Log($"[ZombieRunnerUIManager] Defeat triggered: title='{title}', subtitle='{subtitle}'");

            LockPlayer(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            EnsureEventSystem();

            if (defeatTitleText != null) defeatTitleText.text = title;
            if (defeatSubText != null) defeatSubText.text = subtitle;

            if (gameplayHudPanel != null) gameplayHudPanel.SetActive(false);
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (starRigRoot != null) starRigRoot.SetActive(false);
            if (starCamera != null) starCamera.enabled = false;

            if (defeatPanel != null)
            {
                defeatPanel.SetActive(true);
                defeatPanel.transform.SetAsLastSibling();
            }
        }

        public void TriggerDefeat()
        {
            TriggerDefeatWithReason("YOU WERE OVERWHELMED!", "The zombie horde infected your squad.");
        }

        #endregion

        #region Button Callbacks

        private void OnNextLevelClicked()
        {
            Debug.Log("[ZombieRunnerUIManager] OnNextLevelClicked triggered");
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (defeatPanel != null) defeatPanel.SetActive(false);
            if (starRigRoot != null) starRigRoot.SetActive(false);
            if (starCamera != null) starCamera.enabled = false;

            int next = activeLevel + 1;
            if (next <= totalLevels)
            {
                activeLevel = next;
            }
            autoStartMissionOnLoad = true;
            levelToAutoStart = activeLevel;
            ReloadCurrentScene();
        }

        private void OnReplayClicked()
        {
            Debug.Log("[ZombieRunnerUIManager] OnReplayClicked triggered");
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (defeatPanel != null) defeatPanel.SetActive(false);
            if (starRigRoot != null) starRigRoot.SetActive(false);
            if (starCamera != null) starCamera.enabled = false;

            autoStartMissionOnLoad = true;
            levelToAutoStart = activeLevel > 0 ? activeLevel : 1;
            ReloadCurrentScene();
        }

        private void OnReturnToMenuClicked()
        {
            Debug.Log("[ZombieRunnerUIManager] OnReturnToMenuClicked triggered");
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (defeatPanel != null) defeatPanel.SetActive(false);
            if (starRigRoot != null) starRigRoot.SetActive(false);
            if (starCamera != null) starCamera.enabled = false;

            startAtLevelSelect = true;
            autoStartMissionOnLoad = false;
            ReloadCurrentScene();
        }

        private void ReloadCurrentScene()
        {
            if (isSceneReloading) return;
            isSceneReloading = true;
            Time.timeScale = 1f;

            int buildIndex = SceneManager.GetActiveScene().buildIndex;
            string sceneName = SceneManager.GetActiveScene().name;
            Debug.Log($"[ZombieRunnerUIManager] ReloadCurrentScene executing: index={buildIndex}, name={sceneName}");

            if (buildIndex >= 0)
            {
                SceneManager.LoadScene(buildIndex);
            }
            else if (!string.IsNullOrEmpty(sceneName))
            {
                SceneManager.LoadScene(sceneName);
            }
            else
            {
                SceneManager.LoadScene(0);
            }
        }

        private void UpdateCurrencyHud()
        {
            int coins = PlayerPrefs.GetInt(PREF_COINS, 250);
            if (coinsText != null)
            {
                coinsText.text = $"COINS: {coins}   |   GEMS: 15";
            }
        }

        /// <summary>
        /// Resets all player progress to Level 1 (for testing).
        /// </summary>
        public void ResetProgress()
        {
            PlayerPrefs.DeleteKey(PREF_UNLOCKED_LEVEL);
            PlayerPrefs.DeleteKey(PREF_COINS);
            for (int i = 1; i <= totalLevels; i++)
            {
                PlayerPrefs.DeleteKey(PREF_STARS_PREFIX + i);
            }
            PlayerPrefs.Save();
            PopulateLevelGrid();
            UpdateCurrencyHud();
        }

        #endregion
    }
}
