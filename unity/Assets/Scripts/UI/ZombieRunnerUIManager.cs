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
            HomeScreen,
            LevelMap,
            Characters,
            Upgrade,
            Weapons,
            Shop,
            Missions,
            Daily,
            Gameplay,
            Victory,
            Defeat
        }

        [Header("State")]
        [SerializeField] private ScreenState currentState = ScreenState.HomeScreen;

        [Header("Level Track Settings")]
        [SerializeField] private float levelFinishZ = 120.0f; // End of the zombie street
        [SerializeField] private int activeLevel = 1;
        [SerializeField] private int totalLevels = 6;

        [Header("Splash Settings")]
        [SerializeField] private float splashDurationSeconds = 2.8f;

        // UI Canvas and Panels
        [SerializeField] private Canvas mainCanvas;
        [SerializeField] private GameObject homeScreenPanel;
        [SerializeField] private RectTransform homeScreenPlayBtnRect;
        [SerializeField] private RectTransform homeScreenCharactersBtnRect;
        [SerializeField] private RectTransform homeScreenUpgradeBtnRect;
        [SerializeField] private RectTransform homeScreenWeaponsBtnRect;
        [SerializeField] private RectTransform homeScreenShopBtnRect;
        [SerializeField] private RectTransform homeScreenMissionsBtnRect;
        [SerializeField] private RectTransform homeScreenDailyBtnRect;
        [SerializeField] private GameObject levelMapPanel;
        [SerializeField] private RectTransform levelMapBackBtnRect;
        [SerializeField] private GameObject charactersPanel;
        [SerializeField] private RectTransform charactersBackBtnRect;
        [SerializeField] private GameObject upgradePanel;
        [SerializeField] private RectTransform upgradeBackBtnRect;
        [SerializeField] private GameObject weaponsPanel;
        [SerializeField] private RectTransform weaponsBackBtnRect;
        [SerializeField] private Image weaponsScreenBgImage;
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private RectTransform shopBackBtnRect;
        [SerializeField] private Image shopScreenBgImage;
        [SerializeField] private GameObject missionsPanel;
        [SerializeField] private RectTransform missionsBackBtnRect;
        [SerializeField] private Image missionsScreenBgImage;
        [SerializeField] private GameObject dailyPanel;
        [SerializeField] private RectTransform dailyBackBtnRect;
        [SerializeField] private Image dailyScreenBgImage;
        [SerializeField] private GameObject splashPanel;
        [SerializeField] private GameObject levelSelectPanel;
        [SerializeField] private GameObject gameplayHudPanel;
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private GameObject defeatPanel;

        private class HeroData
        {
            public string key;
            public string name;
            public string role;
            public string desc;
            public float damage;
            public float health;
            public float fireRate;
            public float speed;
            public int damageVal;
            public int healthVal;
            public int fireRateVal;
            public int speedVal;
            public Color accentColor;
        }

        private readonly Dictionary<string, HeroData> heroes = new Dictionary<string, HeroData>
        {
            { "Soldier", new HeroData { key = "Soldier", name = "SOLDIER", role = "Assault Specialist", desc = "Versatile combat trooper with high resilience and steady assault rifle firepower.", damage = 0.65f, health = 0.70f, fireRate = 0.60f, speed = 0.65f, damageVal = 25, healthVal = 100, fireRateVal = 60, speedVal = 65, accentColor = new Color(0.2f, 0.75f, 1f, 1f) } },
            { "Ranger", new HeroData { key = "Ranger", name = "RANGER", role = "Rapid Scout", desc = "High-mobility vanguard equipped with rapid dual SMGs and sprint bonuses.", damage = 0.50f, health = 0.55f, fireRate = 0.90f, speed = 0.85f, damageVal = 18, healthVal = 85, fireRateVal = 90, speedVal = 85, accentColor = new Color(0.95f, 0.30f, 0.30f, 1f) } },
            { "Heavy", new HeroData { key = "Heavy", name = "HEAVY", role = "Tank Juggernaut", desc = "Armor-plated frontliner armed with heavy explosive rounds and high survivability.", damage = 0.85f, health = 0.95f, fireRate = 0.40f, speed = 0.45f, damageVal = 45, healthVal = 160, fireRateVal = 40, speedVal = 45, accentColor = new Color(0.95f, 0.75f, 0.15f, 1f) } },
            { "Sniper", new HeroData { key = "Sniper", name = "SNIPER", role = "Long-Range Marksman", desc = "Deadly sharpshooter delivering lethal critical hits and piercing headshots.", damage = 0.95f, health = 0.45f, fireRate = 0.35f, speed = 0.60f, damageVal = 65, healthVal = 75, fireRateVal = 35, speedVal = 60, accentColor = new Color(0.70f, 0.35f, 0.95f, 1f) } },
            { "Medic", new HeroData { key = "Medic", name = "MEDIC", role = "Field Support", desc = "Combat doctor providing periodic health regeneration and defensive shields.", damage = 0.50f, health = 0.80f, fireRate = 0.55f, speed = 0.75f, damageVal = 20, healthVal = 110, fireRateVal = 55, speedVal = 75, accentColor = new Color(0.95f, 0.40f, 0.70f, 1f) } }
        };

        private string selectedCharacterKey = "Soldier";
        private Image charPreviewHeroImage;
        private TextMeshProUGUI charPreviewNameText;
        private TextMeshProUGUI charPreviewRoleText;
        private Image charPreviewDmgFill;
        private Image charPreviewHpFill;
        private Image charPreviewRateFill;
        private Image charPreviewSpeedFill;
        private TextMeshProUGUI charPreviewDmgText;
        private TextMeshProUGUI charPreviewHpText;
        private TextMeshProUGUI charPreviewRateText;
        private TextMeshProUGUI charPreviewSpeedText;
        private Button charPreviewEquipBtn;
        private TextMeshProUGUI charPreviewEquipBtnText;
        private Image charPreviewEquipBtnBg;

        [System.Serializable]
        private class CharacterCardItem
        {
            public string heroKey;
            public GameObject cardObj;
            public Image selectionBorder;
            public TextMeshProUGUI pillText;
            public Image pillBg;
        }
        [SerializeField] private List<CharacterCardItem> characterCardItems = new List<CharacterCardItem>();

        [System.Serializable]
        private class MapNodeItem
        {
            public int levelNumber;
            public GameObject rootObj;
            public Image outerRing;
            public Image innerDisc;
            public Image iconImage;
            public RectTransform iconRt;
            public TextMeshProUGUI numberText;
            public RectTransform textRt;
            public GameObject starsContainer;
            public Image[] starImages;
            public Button button;
            public bool isBoss;
            [System.NonSerialized] public Coroutine pulseCoroutine;
        }
        [SerializeField] private List<MapNodeItem> mapNodeItems = new List<MapNodeItem>();

        public static bool startAtHomeScreen = true;
        public static bool startAtLevelMap = false;
        public static bool startAtLevelSelect = false;
        public static bool autoStartMissionOnLoad = false;
        public static int levelToAutoStart = 1;

        // Splash Elements
        private RectTransform splashBannerRect;
        private Image splashLoadingFillImage;
        private TextMeshProUGUI splashPercentText;
        private TextMeshProUGUI splashTipText;

        // Defeat Elements
        private TextMeshProUGUI defeatTitleText;
        private TextMeshProUGUI defeatSubText;

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

        // 3D Hero Preview Rig Elements (for Home Screen cleanup)
        private RenderTexture homeHeroRenderTexture;
        private GameObject homeHeroRigRoot;

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
            ZombieRunnerUIManager[] allManagers = FindObjectsByType<ZombieRunnerUIManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (allManagers != null && allManagers.Length > 0)
            {
                Instance = allManagers[0];
                for (int i = 1; i < allManagers.Length; i++)
                {
                    if (allManagers[i] != null && allManagers[i] != Instance)
                    {
                        DestroyImmediate(allManagers[i].gameObject);
                    }
                }
            }

            if (Instance == null)
            {
                GameObject uiRoot = new GameObject("[ZombieRunner_UI_Manager]");
                Instance = uiRoot.AddComponent<ZombieRunnerUIManager>();
                DontDestroyOnLoad(uiRoot);
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
            ZombieRunnerUIManager[] all = FindObjectsByType<ZombieRunnerUIManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var m in all)
            {
                if (m != null && m != this)
                {
                    DestroyImmediate(m.gameObject);
                }
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureEventSystem();
            SceneManager.sceneLoaded -= OnSceneLoaded;
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

            if (startAtLevelMap)
            {
                startAtLevelMap = false;
                startAtHomeScreen = false;
                startAtLevelSelect = false;
                ShowZombieSquadLevelMap();
            }
            else if (autoStartMissionOnLoad)
            {
                autoStartMissionOnLoad = false;
                StartMission(levelToAutoStart);
            }
            else
            {
                startAtHomeScreen = false;
                startAtLevelSelect = false;
                ShowZombieSquadHomeScreen();
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

            EnsureUIBuilt();
            MobControl.Level.NewRoadBuilder.EnsureNewRoad();

            if (autoStartMissionOnLoad)
            {
                autoStartMissionOnLoad = false;
                StartMission(levelToAutoStart);
            }
            else
            {
                ShowZombieSquadHomeScreen();
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

#if UNITY_EDITOR
            // Quick test shortcut: Press 'U' on Level Map to advance unlocked levels & preview earned stars
            if (currentState == ScreenState.LevelMap && Input.GetKeyDown(KeyCode.U))
            {
                int currentUnlocked = PlayerPrefs.GetInt(PREF_UNLOCKED_LEVEL, 1);
                int next = (currentUnlocked >= 8) ? 1 : currentUnlocked + 1;
                PlayerPrefs.SetInt(PREF_UNLOCKED_LEVEL, next);
                for (int i = 1; i < next; i++)
                {
                    int testStars = (i == 2 || i == 5) ? 2 : 3;
                    PlayerPrefs.SetInt(PREF_STARS_PREFIX + i, testStars);
                }
                PlayerPrefs.Save();
                Debug.Log($"[ZombieRunnerUIManager] DEBUG (U pressed): Unlocked level set to {next}");
                UpdateLevelMapVisuals();
            }
#endif

            // High-reliability touch/mouse fallback for HomeScreen, LevelMap, Characters, Defeat & Victory buttons
            if (currentState == ScreenState.HomeScreen && homeScreenPanel != null && homeScreenPanel.activeInHierarchy && !isSceneReloading)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Vector2 mousePos = Input.mousePosition;
                    if (homeScreenCharactersBtnRect != null && RectTransformUtility.RectangleContainsScreenPoint(homeScreenCharactersBtnRect, mousePos, null))
                    {
                        Debug.Log("[ZombieRunnerUIManager] HomeScreen CHARACTERS clicked -> Instant Open!");
                        ShowZombieSquadCharactersScreen();
                        return;
                    }
                    else if (homeScreenUpgradeBtnRect != null && RectTransformUtility.RectangleContainsScreenPoint(homeScreenUpgradeBtnRect, mousePos, null))
                    {
                        Debug.Log("[ZombieRunnerUIManager] HomeScreen UPGRADE clicked -> Instant Open!");
                        ShowZombieSquadUpgradeScreen();
                        return;
                    }
                    else if (homeScreenWeaponsBtnRect != null && RectTransformUtility.RectangleContainsScreenPoint(homeScreenWeaponsBtnRect, mousePos, null))
                    {
                        Debug.Log("[ZombieRunnerUIManager] HomeScreen WEAPONS clicked -> Instant Open!");
                        ShowZombieSquadWeaponsScreen();
                        return;
                    }
                    else if (homeScreenShopBtnRect != null && RectTransformUtility.RectangleContainsScreenPoint(homeScreenShopBtnRect, mousePos, null))
                    {
                        Debug.Log("[ZombieRunnerUIManager] HomeScreen SHOP clicked -> Instant Open!");
                        ShowZombieSquadShopScreen();
                        return;
                    }
                    else if (homeScreenMissionsBtnRect != null && RectTransformUtility.RectangleContainsScreenPoint(homeScreenMissionsBtnRect, mousePos, null))
                    {
                        Debug.Log("[ZombieRunnerUIManager] HomeScreen MISSIONS clicked -> Instant Open!");
                        ShowZombieSquadMissionsScreen();
                        return;
                    }
                    else if (homeScreenDailyBtnRect != null && RectTransformUtility.RectangleContainsScreenPoint(homeScreenDailyBtnRect, mousePos, null))
                    {
                        Debug.Log("[ZombieRunnerUIManager] HomeScreen DAILY clicked -> Instant Open!");
                        ShowZombieSquadDailyScreen();
                        return;
                    }
                    else if (homeScreenPlayBtnRect != null && RectTransformUtility.RectangleContainsScreenPoint(homeScreenPlayBtnRect, mousePos, null))
                    {
                        Debug.Log("[ZombieRunnerUIManager] HomeScreen PLAY clicked -> Opening Level Map Screen!");
                        ShowZombieSquadLevelMap();
                        return;
                    }
                }
            }
            else if (currentState == ScreenState.Characters && charactersPanel != null && charactersPanel.activeInHierarchy && !isSceneReloading)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Vector2 mousePos = Input.mousePosition;
                    if (charactersBackBtnRect != null && RectTransformUtility.RectangleContainsScreenPoint(charactersBackBtnRect, mousePos, null))
                    {
                        Debug.Log("[ZombieRunnerUIManager] Characters Back clicked via direct screen fallback!");
                        ShowZombieSquadHomeScreen();
                        return;
                    }
                }
            }
            else if (currentState == ScreenState.Upgrade && upgradePanel != null && upgradePanel.activeInHierarchy && !isSceneReloading)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Vector2 mousePos = Input.mousePosition;
                    if (upgradeBackBtnRect != null && RectTransformUtility.RectangleContainsScreenPoint(upgradeBackBtnRect, mousePos, null))
                    {
                        Debug.Log("[ZombieRunnerUIManager] Upgrade Back clicked via direct screen fallback!");
                        ShowZombieSquadHomeScreen();
                        return;
                    }
                }
            }
            else if (currentState == ScreenState.Weapons && weaponsPanel != null && weaponsPanel.activeInHierarchy && !isSceneReloading)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Vector2 mousePos = Input.mousePosition;
                    if (weaponsBackBtnRect != null && RectTransformUtility.RectangleContainsScreenPoint(weaponsBackBtnRect, mousePos, null))
                    {
                        Debug.Log("[ZombieRunnerUIManager] Weapons Back clicked via direct screen fallback!");
                        ShowZombieSquadHomeScreen();
                        return;
                    }
                }
            }
            else if (currentState == ScreenState.Shop && shopPanel != null && shopPanel.activeInHierarchy && !isSceneReloading)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Vector2 mousePos = Input.mousePosition;
                    if (shopBackBtnRect != null && RectTransformUtility.RectangleContainsScreenPoint(shopBackBtnRect, mousePos, null))
                    {
                        Debug.Log("[ZombieRunnerUIManager] Shop Back clicked via direct screen fallback!");
                        ShowZombieSquadHomeScreen();
                        return;
                    }
                }
            }
            else if (currentState == ScreenState.Missions && missionsPanel != null && missionsPanel.activeInHierarchy && !isSceneReloading)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Vector2 mousePos = Input.mousePosition;
                    if (missionsBackBtnRect != null && RectTransformUtility.RectangleContainsScreenPoint(missionsBackBtnRect, mousePos, null))
                    {
                        Debug.Log("[ZombieRunnerUIManager] Missions Back clicked via direct screen fallback!");
                        ShowZombieSquadHomeScreen();
                        return;
                    }
                }
            }
            else if (currentState == ScreenState.Daily && dailyPanel != null && dailyPanel.activeInHierarchy && !isSceneReloading)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Vector2 mousePos = Input.mousePosition;
                    if (dailyBackBtnRect != null && RectTransformUtility.RectangleContainsScreenPoint(dailyBackBtnRect, mousePos, null))
                    {
                        Debug.Log("[ZombieRunnerUIManager] Daily Back clicked via direct screen fallback!");
                        ShowZombieSquadHomeScreen();
                        return;
                    }
                }
            }
            else if (currentState == ScreenState.LevelMap && levelMapPanel != null && levelMapPanel.activeInHierarchy && !isSceneReloading)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Vector2 mousePos = Input.mousePosition;
                    if (levelMapBackBtnRect != null && RectTransformUtility.RectangleContainsScreenPoint(levelMapBackBtnRect, mousePos, null))
                    {
                        Debug.Log("[ZombieRunnerUIManager] LevelMap Back clicked via direct screen fallback!");
                        ShowZombieSquadHomeScreen();
                        return;
                    }
                }
            }
            else if (currentState == ScreenState.Defeat && defeatPanel != null && defeatPanel.activeInHierarchy && !isSceneReloading)
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

                // Check Victory: Reached end of street (ensure levelFinishZ is valid)
                if (levelFinishZ > 10f && playerRunner.transform.position.z >= levelFinishZ)
                {
                    TriggerVictory();
                }
                // Check Defeat: Player died
                else if (playerRunner.IsDead || playerRunner.Health <= 0)
                {
                    TriggerDefeatWithReason("YOU WERE OVERWHELMED!", "The zombie horde infected your squad.");
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

            // 2. Destroy any existing duplicate canvases in the scene/DontDestroyOnLoad
            Canvas[] allExistingCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            foreach (var c in allExistingCanvases)
            {
                if (c != null && c.name.Contains("ZombieRunnerCanvas"))
                {
                    DestroyImmediate(c.gameObject);
                }
            }

            // 3. Main Canvas
            GameObject canvasObj = new GameObject("ZombieRunnerCanvas");
            canvasObj.transform.SetParent(transform);
            mainCanvas = canvasObj.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mainCanvas.sortingOrder = 999;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100;

            GraphicRaycaster gr = canvasObj.AddComponent<GraphicRaycaster>();
            gr.enabled = true;

            // 3. Build Sub-Panels
            BuildZombieSquadHomeScreenPanel(canvasObj.transform);
            BuildZombieSquadLevelMapPanel(canvasObj.transform);
            BuildZombieSquadCharactersScreenPanel(canvasObj.transform);
            BuildZombieSquadUpgradeScreenPanel(canvasObj.transform);
            BuildZombieSquadWeaponsScreenPanel(canvasObj.transform);
            BuildZombieSquadShopScreenPanel(canvasObj.transform);
            BuildZombieSquadMissionsScreenPanel(canvasObj.transform);
            BuildZombieSquadDailyScreenPanel(canvasObj.transform);
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

            panel.SetActive(false); // Ensure panel starts hidden so nothing flickers during UI build

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

        #region Star Sprite Generator & Loaders

        private static Sprite s_goldStarSprite;
        private static Sprite s_dimStarSprite;
        private static Sprite s_starBadgeBgSprite;

        public static Sprite LoadStarBadgeBgSprite()
        {
            if (s_starBadgeBgSprite != null) return s_starBadgeBgSprite;
            try
            {
                Sprite loaded = Resources.Load<Sprite>("UI/Star_Badge_Bg");
                if (loaded != null) { s_starBadgeBgSprite = loaded; return loaded; }

                string localPath = System.IO.Path.Combine(Application.dataPath, "Resources", "UI", "Star_Badge_Bg.png");
                if (System.IO.File.Exists(localPath))
                {
                    byte[] fileData = System.IO.File.ReadAllBytes(localPath);
                    Texture2D diskTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (diskTex.LoadImage(fileData))
                    {
                        s_starBadgeBgSprite = Sprite.Create(diskTex, new Rect(0, 0, diskTex.width, diskTex.height), new Vector2(0.5f, 0.5f));
                        return s_starBadgeBgSprite;
                    }
                }
            }
            catch {}
            return null;
        }

        public static Sprite GetStarSprite(bool filled)
        {
            if (filled && s_goldStarSprite != null) return s_goldStarSprite;
            if (!filled && s_dimStarSprite != null) return s_dimStarSprite;

            // 1. Try loading from Resources/UI/
            try
            {
                string resName = filled ? "UI/Star_Gold" : "UI/Star_Empty";
                Sprite loaded = Resources.Load<Sprite>(resName);
                if (loaded != null)
                {
                    if (filled) s_goldStarSprite = loaded;
                    else s_dimStarSprite = loaded;
                    return loaded;
                }

                // 2. Direct disk file fallback
                string fileName = filled ? "Star_Gold.png" : "Star_Empty.png";
                string localPath = System.IO.Path.Combine(Application.dataPath, "Resources", "UI", fileName);
                if (System.IO.File.Exists(localPath))
                {
                    byte[] fileData = System.IO.File.ReadAllBytes(localPath);
                    Texture2D diskTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (diskTex.LoadImage(fileData))
                    {
                        Sprite s = Sprite.Create(diskTex, new Rect(0, 0, diskTex.width, diskTex.height), new Vector2(0.5f, 0.5f));
                        if (filled) s_goldStarSprite = s;
                        else s_dimStarSprite = s;
                        return s;
                    }
                }
            }
            catch {}

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

        #region 3D HomeScreen Hero Rig Cleanup

        private void Cleanup3DHomeScreenCharacterRig()
        {
            if (homeHeroRigRoot != null)
            {
                DestroyImmediate(homeHeroRigRoot);
                homeHeroRigRoot = null;
            }

            GameObject orphan = GameObject.Find("[HomeScreen_3D_Hero_Rig]");
            if (orphan != null) DestroyImmediate(orphan);

            if (homeHeroRenderTexture != null)
            {
                homeHeroRenderTexture.Release();
                DestroyImmediate(homeHeroRenderTexture);
                homeHeroRenderTexture = null;
            }
        }

        private void Ensure3DHomeScreenCharacterRig()
        {
            Cleanup3DHomeScreenCharacterRig();
        }

        private void TriggerHeroFistPunch()
        {
        }

        #endregion

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

                string localPath = System.IO.Path.Combine(Application.dataPath, "Resources", "UI", "ZombieSplashBanner.png");
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

        private Sprite LoadZombieSquadHomeScreenSprite()
        {
            try
            {
                Sprite s = Resources.Load<Sprite>("UI/ZombieSquad_HomeScreen");
                if (s != null) return s;

                Texture2D resTex = Resources.Load<Texture2D>("UI/ZombieSquad_HomeScreen");
                if (resTex != null)
                {
                    return Sprite.Create(resTex, new Rect(0, 0, resTex.width, resTex.height), new Vector2(0.5f, 0.5f));
                }

                string[] localPaths = new string[]
                {
                    System.IO.Path.Combine(Application.dataPath, "Resources", "UI", "ZombieSquad_HomeScreen.png"),
                    System.IO.Path.Combine(Application.dataPath, "Resources", "UI", "ZombieSquad_HomeScreen.jpg")
                };

                foreach (string localPath in localPaths)
                {
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
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[ZombieRunnerUIManager] ZombieSquad HomeScreen loading: " + ex.Message);
            }
            return null;
        }

        private IEnumerator PunchScale(Transform tr, float duration, System.Action onComplete)
        {
            if (tr == null)
            {
                onComplete?.Invoke();
                yield break;
            }
            Vector3 originalScale = tr.localScale;
            Vector3 punchScale = originalScale * 0.92f;
            float half = duration * 0.5f;
            float elapsed = 0f;

            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                if (tr != null) tr.localScale = Vector3.Lerp(originalScale, punchScale, elapsed / half);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                if (tr != null) tr.localScale = Vector3.Lerp(punchScale, originalScale, elapsed / half);
                yield return null;
            }

            if (tr != null) tr.localScale = originalScale;
            onComplete?.Invoke();
        }

        private void BuildZombieSquadHomeScreenPanel(Transform canvasRoot)
        {
            // Full screen panel
            homeScreenPanel = CreateFullPanel("ZombieSquadHomeScreenPanel", canvasRoot, new Color(0.04f, 0.06f, 0.10f, 1f));

            // Container for full screen UI
            GameObject artContainer = new GameObject("ArtContainer");
            artContainer.transform.SetParent(homeScreenPanel.transform, false);
            RectTransform artRt = artContainer.AddComponent<RectTransform>();
            artRt.anchorMin = Vector2.zero;
            artRt.anchorMax = Vector2.one;
            artRt.offsetMin = Vector2.zero;
            artRt.offsetMax = Vector2.zero;

            // Background Image - Stretches across the full screen seamlessly
            Image homeImg = artContainer.AddComponent<Image>();
            Sprite homeSprite = LoadZombieSquadHomeScreenSprite();
            if (homeSprite != null)
            {
                homeImg.sprite = homeSprite;
            }
            homeImg.type = Image.Type.Simple;
            homeImg.preserveAspect = false;
            homeImg.raycastTarget = true;

            // Clean up any stale 3D hero objects or rigs
            Cleanup3DHomeScreenCharacterRig();
            Transform oldHero = artContainer.transform.Find("HomeScreen_3D_Hero");
            if (oldHero != null) DestroyImmediate(oldHero.gameObject);

            // Transparent Button Creator Helper
            Button CreateTransparentButton(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, System.Action onClick)
            {
                GameObject btnObj = new GameObject(name);
                btnObj.transform.SetParent(parent, false);

                RectTransform rt = btnObj.AddComponent<RectTransform>();
                rt.anchorMin = anchorMin;
                rt.anchorMax = anchorMax;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                Image img = btnObj.AddComponent<Image>();
                img.color = new Color(1f, 1f, 1f, 0.001f); // Virtually transparent but captures raycasts
                img.raycastTarget = true;

                Button btn = btnObj.AddComponent<Button>();
                btn.targetGraphic = img;

                ColorBlock cb = btn.colors;
                cb.normalColor = new Color(1f, 1f, 1f, 0.001f);
                cb.highlightedColor = new Color(1f, 1f, 1f, 0.15f);
                cb.pressedColor = new Color(0f, 0f, 0f, 0.25f);
                cb.selectedColor = new Color(1f, 1f, 1f, 0.001f);
                cb.fadeDuration = 0.05f;
                btn.colors = cb;

                if (onClick != null)
                {
                    btn.onClick.AddListener(() =>
                    {
                        onClick(); // Instant execution with ZERO lag!
                        StartCoroutine(PunchScale(btnObj.transform, 0.10f, null));
                    });
                }
                return btn;
            }

            // 1. PLAY Button (Over the big yellow button at the bottom) -> Opens Level Map Screen!
            Button playBtn = CreateTransparentButton("Btn_Play", artContainer.transform, new Vector2(0.210f, 0.055f), new Vector2(0.790f, 0.175f), () =>
            {
                Debug.Log("[ZombieSquad] PLAY Button Pressed -> Opening Level Map Screen!");
                ShowZombieSquadLevelMap();
            });
            homeScreenPlayBtnRect = playBtn.GetComponent<RectTransform>();

            Button shopBtn = CreateTransparentButton("Btn_Shop", artContainer.transform, new Vector2(0.020f, 0.550f), new Vector2(0.240f, 0.680f), () =>
            {
                Debug.Log("[ZombieSquad] Shop Clicked -> Opening Shop Screen!");
                ShowZombieSquadShopScreen();
            });
            homeScreenShopBtnRect = shopBtn.GetComponent<RectTransform>();
            Button charBtn = CreateTransparentButton("Btn_Characters", artContainer.transform, new Vector2(0.020f, 0.410f), new Vector2(0.240f, 0.545f), () =>
            {
                Debug.Log("[ZombieSquad] Characters Clicked -> Opening Characters Screen!");
                ShowZombieSquadCharactersScreen();
            });
            homeScreenCharactersBtnRect = charBtn.GetComponent<RectTransform>();
            Button wepBtn = CreateTransparentButton("Btn_Weapons", artContainer.transform, new Vector2(0.020f, 0.280f), new Vector2(0.240f, 0.410f), () =>
            {
                Debug.Log("[ZombieSquad] Weapons Clicked -> Opening Weapons Screen!");
                ShowZombieSquadWeaponsScreen();
            });
            homeScreenWeaponsBtnRect = wepBtn.GetComponent<RectTransform>();

            // 3. Right Menu Buttons: Missions, Daily, Upgrade (EXACT Pixel Mapping!)
            Button missionsBtn = CreateTransparentButton("Btn_Missions", artContainer.transform, new Vector2(0.760f, 0.550f), new Vector2(0.975f, 0.680f), () =>
            {
                Debug.Log("[ZombieSquad] Missions Clicked -> Opening Missions Screen!");
                ShowZombieSquadMissionsScreen();
            });
            homeScreenMissionsBtnRect = missionsBtn.GetComponent<RectTransform>();

            Button dailyBtn = CreateTransparentButton("Btn_Daily", artContainer.transform, new Vector2(0.760f, 0.410f), new Vector2(0.975f, 0.545f), () =>
            {
                Debug.Log("[ZombieSquad] Daily Rewards Clicked -> Opening Daily Screen!");
                ShowZombieSquadDailyScreen();
            });
            homeScreenDailyBtnRect = dailyBtn.GetComponent<RectTransform>();

            Button upgBtn = CreateTransparentButton("Btn_Upgrade", artContainer.transform, new Vector2(0.760f, 0.280f), new Vector2(0.975f, 0.410f), () =>
            {
                Debug.Log("[ZombieSquad] Upgrade Clicked -> Opening Squad Upgrade Screen!");
                ShowZombieSquadUpgradeScreen();
            });
            homeScreenUpgradeBtnRect = upgBtn.GetComponent<RectTransform>();

            // 4. Settings Gear (Top Right)
            CreateTransparentButton("Btn_Settings", artContainer.transform, new Vector2(0.84f, 0.87f), new Vector2(0.98f, 0.98f), () => Debug.Log("[ZombieSquad] Settings Clicked"));
        }

        private Sprite LoadZombieSquadLevelMapSprite()
        {
            try
            {
                Sprite s = Resources.Load<Sprite>("UI/ZombieSquad_LevelMap");
                if (s != null) return s;

                Texture2D resTex = Resources.Load<Texture2D>("UI/ZombieSquad_LevelMap");
                if (resTex != null)
                {
                    return Sprite.Create(resTex, new Rect(0, 0, resTex.width, resTex.height), new Vector2(0.5f, 0.5f));
                }

                string localPath = System.IO.Path.Combine(Application.dataPath, "Resources", "UI", "ZombieSquad_LevelMap.jpg");
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
                Debug.LogWarning("[ZombieRunnerUIManager] LevelMap sprite loading: " + ex.Message);
            }
            return null;
        }

        private static Sprite s_circleBadgeSprite;
        private static Sprite GetCircleBadgeSprite()
        {
            if (s_circleBadgeSprite != null) return s_circleBadgeSprite;
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.46f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            s_circleBadgeSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return s_circleBadgeSprite;
        }

        private Sprite LoadPadlockSprite()
        {
            try
            {
                Sprite s = Resources.Load<Sprite>("UI/Padlock");
                if (s != null) return s;
                string localPath = System.IO.Path.Combine(Application.dataPath, "Resources", "UI", "Padlock.png");
                if (System.IO.File.Exists(localPath))
                {
                    byte[] fileData = System.IO.File.ReadAllBytes(localPath);
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (tex.LoadImage(fileData))
                    {
                        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    }
                }
            }
            catch {}
            return null;
        }

        private Sprite LoadBossSkullSprite()
        {
            try
            {
                Sprite s = Resources.Load<Sprite>("UI/BossSkull");
                if (s != null) return s;
                string localPath = System.IO.Path.Combine(Application.dataPath, "Resources", "UI", "BossSkull.png");
                if (System.IO.File.Exists(localPath))
                {
                    byte[] fileData = System.IO.File.ReadAllBytes(localPath);
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (tex.LoadImage(fileData))
                    {
                        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    }
                }
            }
            catch {}
            return null;
        }

        private void BuildZombieSquadLevelMapPanel(Transform canvasRoot)
        {
            levelMapPanel = CreateFullPanel("ZombieSquadLevelMapPanel", canvasRoot, new Color(0.04f, 0.06f, 0.10f, 1f));

            // Container for 9:16 aspect ratio
            GameObject artContainer = new GameObject("ArtContainer");
            artContainer.transform.SetParent(levelMapPanel.transform, false);
            RectTransform artRt = artContainer.AddComponent<RectTransform>();
            artRt.anchorMin = Vector2.zero;
            artRt.anchorMax = Vector2.one;
            artRt.offsetMin = Vector2.zero;
            artRt.offsetMax = Vector2.zero;


            // Background Image (The 9:16 Level Map Image)
            Image mapImg = artContainer.AddComponent<Image>();
            Sprite mapSprite = LoadZombieSquadLevelMapSprite();
            if (mapSprite != null)
            {
                mapImg.sprite = mapSprite;
            }
            mapImg.raycastTarget = true;

            // Top-Left Back Button to Home Screen
            GameObject backBtnObj = new GameObject("Btn_Back");
            backBtnObj.transform.SetParent(artContainer.transform, false);
            RectTransform backRt = backBtnObj.AddComponent<RectTransform>();
            backRt.anchorMin = new Vector2(0.04f, 0.89f);
            backRt.anchorMax = new Vector2(0.16f, 0.98f);
            backRt.offsetMin = Vector2.zero;
            backRt.offsetMax = Vector2.zero;

            Image backImg = backBtnObj.AddComponent<Image>();
            backImg.color = new Color(1f, 1f, 1f, 0.001f);
            backImg.raycastTarget = true;

            Button backBtn = backBtnObj.AddComponent<Button>();
            backBtn.targetGraphic = backImg;
            levelMapBackBtnRect = backRt;
            backBtn.onClick.AddListener(() =>
            {
                StartCoroutine(PunchScale(backBtnObj.transform, 0.12f, ShowZombieSquadHomeScreen));
            });

            // Precise Road Coordinates along the clean asphalt track
            Vector2[] nodePositions = new Vector2[]
            {
                new Vector2(0.196f, 0.459f), // Node 1 (Start of road at bottom-left)
                new Vector2(0.207f, 0.524f), // Node 2 (Upward curve)
                new Vector2(0.290f, 0.580f), // Node 3 (Top curve near house)
                new Vector2(0.441f, 0.551f), // Node 4 (Bridge curve)
                new Vector2(0.545f, 0.485f), // Node 5 (S-curve)
                new Vector2(0.675f, 0.423f), // Node 6 (Desert road)
                new Vector2(0.804f, 0.322f), // Node 7 (End of road near torches)
                new Vector2(0.847f, 0.819f)  // Boss (Over the drawn Red Skull)
            };

            // Compact, cute sizes matching the road width
            Vector2[] nodeSizes = new Vector2[]
            {
                new Vector2(88f, 88f), // Node 1
                new Vector2(88f, 88f), // Node 2
                new Vector2(88f, 88f), // Node 3
                new Vector2(88f, 88f), // Node 4
                new Vector2(94f, 94f), // Node 5
                new Vector2(88f, 88f), // Node 6
                new Vector2(88f, 88f), // Node 7
                new Vector2(170f, 170f) // Boss (Transparent clickable button over skull)
            };

            mapNodeItems.Clear();

            for (int i = 0; i < nodePositions.Length; i++)
            {
                int lvlNum = i + 1;
                bool isBoss = (i == 7);
                Vector2 normPos = nodePositions[i];
                Vector2 size = nodeSizes[i];

                GameObject nodeRoot = new GameObject(isBoss ? "Node_Boss" : $"Node_Lvl_{lvlNum}");
                nodeRoot.transform.SetParent(artContainer.transform, false);

                RectTransform nodeRt = nodeRoot.AddComponent<RectTransform>();
                nodeRt.anchorMin = normPos;
                nodeRt.anchorMax = normPos;
                nodeRt.pivot = new Vector2(0.5f, 0.5f);
                nodeRt.anchoredPosition = Vector2.zero;
                nodeRt.sizeDelta = size;

                // Outer border ring
                GameObject outerRingObj = new GameObject("OuterRing");
                outerRingObj.transform.SetParent(nodeRoot.transform, false);
                RectTransform outerRt = outerRingObj.AddComponent<RectTransform>();
                outerRt.anchorMin = Vector2.zero;
                outerRt.anchorMax = Vector2.one;
                outerRt.offsetMin = Vector2.zero;
                outerRt.offsetMax = Vector2.zero;
                Image outerRing = outerRingObj.AddComponent<Image>();
                outerRing.sprite = GetCircleBadgeSprite();
                outerRing.raycastTarget = false;

                // Inner Disc
                GameObject innerDiscObj = new GameObject("InnerDisc");
                innerDiscObj.transform.SetParent(nodeRoot.transform, false);
                RectTransform innerRt = innerDiscObj.AddComponent<RectTransform>();
                innerRt.anchorMin = Vector2.zero;
                innerRt.anchorMax = Vector2.one;
                innerRt.offsetMin = isBoss ? Vector2.zero : new Vector2(5, 5);
                innerRt.offsetMax = isBoss ? Vector2.zero : new Vector2(-5, -5);
                Image innerDisc = innerDiscObj.AddComponent<Image>();
                innerDisc.sprite = isBoss ? null : GetCircleBadgeSprite();
                innerDisc.raycastTarget = true;

                // Icon (Padlock for locked levels)
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(nodeRoot.transform, false);
                RectTransform iconRt = iconObj.AddComponent<RectTransform>();
                iconRt.anchorMin = new Vector2(0.5f, 0.60f);
                iconRt.anchorMax = new Vector2(0.5f, 0.60f);
                iconRt.pivot = new Vector2(0.5f, 0.5f);
                iconRt.sizeDelta = new Vector2(36, 36);
                iconRt.anchoredPosition = Vector2.zero;
                Image iconImg = iconObj.AddComponent<Image>();
                iconImg.raycastTarget = false;
                iconObj.SetActive(false);

                // Text (Clean bold level number)
                TextMeshProUGUI tmp = CreateText("Label", nodeRoot.transform, lvlNum.ToString(), 42, Color.white, TextAlignmentOptions.Center, true);
                RectTransform txtRt = tmp.GetComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero;
                txtRt.anchorMax = Vector2.one;
                txtRt.offsetMin = Vector2.zero;
                txtRt.offsetMax = Vector2.zero;
                tmp.raycastTarget = false;

                Button btn = nodeRoot.AddComponent<Button>();
                btn.targetGraphic = innerDisc;

                int capturedLvl = lvlNum;
                btn.onClick.AddListener(() =>
                {
                    OnMapNodeClicked(capturedLvl, nodeRoot.transform);
                });

                // Stars Container (3 Mini Stars arc right on top of the node)
                GameObject starsObj = null;
                Image[] starImgs = null;

                if (!isBoss)
                {
                    starsObj = new GameObject("StarsContainer");
                    starsObj.transform.SetParent(nodeRoot.transform, false);
                    RectTransform starsRt = starsObj.AddComponent<RectTransform>();
                    starsRt.anchorMin = new Vector2(0.5f, 1f);
                    starsRt.anchorMax = new Vector2(0.5f, 1f);
                    starsRt.pivot = new Vector2(0.5f, 0.5f);
                    starsRt.anchoredPosition = new Vector2(0, 10);
                    starsRt.sizeDelta = new Vector2(76, 28);

                    // Pill Badge background for high contrast against map grass/road
                    Image badgeBg = starsObj.AddComponent<Image>();
                    Sprite bgSp = LoadStarBadgeBgSprite();
                    if (bgSp != null) badgeBg.sprite = bgSp;
                    badgeBg.color = Color.white;
                    badgeBg.raycastTarget = false;

                    starImgs = new Image[3];
                    Vector2[] starOffsets = new Vector2[] { new Vector2(-22, -1), new Vector2(0, 4), new Vector2(22, -1) };
                    Vector2[] starSizes = new Vector2[] { new Vector2(20, 20), new Vector2(26, 26), new Vector2(20, 20) };

                    for (int s = 0; s < 3; s++)
                    {
                        GameObject sObj = new GameObject($"Star_{s}");
                        sObj.transform.SetParent(starsObj.transform, false);
                        RectTransform sRt = sObj.AddComponent<RectTransform>();
                        sRt.anchorMin = new Vector2(0.5f, 0.5f);
                        sRt.anchorMax = new Vector2(0.5f, 0.5f);
                        sRt.pivot = new Vector2(0.5f, 0.5f);
                        sRt.anchoredPosition = starOffsets[s];
                        sRt.sizeDelta = starSizes[s];
                        if (s == 0) sRt.localEulerAngles = new Vector3(0, 0, 10);
                        else if (s == 2) sRt.localEulerAngles = new Vector3(0, 0, -10);

                        Image sImg = sObj.AddComponent<Image>();
                        sImg.sprite = GetStarSprite(true);
                        sImg.raycastTarget = false;
                        starImgs[s] = sImg;
                    }
                    starsObj.SetActive(false);
                }

                MapNodeItem item = new MapNodeItem
                {
                    levelNumber = lvlNum,
                    rootObj = nodeRoot,
                    outerRing = outerRing,
                    innerDisc = innerDisc,
                    iconImage = iconImg,
                    iconRt = iconRt,
                    numberText = tmp,
                    textRt = txtRt,
                    starsContainer = starsObj,
                    starImages = starImgs,
                    button = btn,
                    isBoss = isBoss
                };
                mapNodeItems.Add(item);
            }
        }

        private void UpdateLevelMapVisuals()
        {
            int unlockedLevel = PlayerPrefs.GetInt(PREF_UNLOCKED_LEVEL, 1);
            if (unlockedLevel < 1) unlockedLevel = 1;

            Sprite padlockSprite = LoadPadlockSprite();

            for (int i = 0; i < mapNodeItems.Count; i++)
            {
                MapNodeItem item = mapNodeItems[i];
                if (item == null || item.rootObj == null) continue;

                if (item.pulseCoroutine != null)
                {
                    StopCoroutine(item.pulseCoroutine);
                    item.pulseCoroutine = null;
                    item.rootObj.transform.localScale = Vector3.one;
                }

                if (item.isBoss)
                {
                    // The Red Skull is already drawn on the background image!
                    // Keep transparent so background skull shows through cleanly
                    item.outerRing.color = Color.clear;
                    item.innerDisc.color = new Color(1f, 1f, 1f, 0.001f);
                    item.iconImage.gameObject.SetActive(false);
                    item.numberText.gameObject.SetActive(false);
                    item.button.interactable = true;

                    if (unlockedLevel >= 8)
                    {
                        // Unlocked Boss! Add subtle pulse animation on the skull
                        item.pulseCoroutine = StartCoroutine(PulseActiveNode(item.rootObj.transform));
                    }
                    continue;
                }

                if (item.levelNumber < unlockedLevel)
                {
                    // COMPLETED -> BLUE COLOR + STARS DISPLAY
                    item.outerRing.color = new Color(0.02f, 0.28f, 0.65f, 1f); // Dark blue rim
                    item.innerDisc.color = new Color(0.08f, 0.62f, 0.98f, 1f); // Vibrant cyan blue

                    item.iconImage.gameObject.SetActive(false);
                    item.numberText.gameObject.SetActive(true);
                    item.numberText.text = item.levelNumber.ToString();
                    item.numberText.fontSize = 42;
                    item.numberText.color = Color.white;

                    item.textRt.anchorMin = Vector2.zero;
                    item.textRt.anchorMax = Vector2.one;
                    item.textRt.offsetMin = Vector2.zero;
                    item.textRt.offsetMax = Vector2.zero;

                    // Display earned stars on top of node
                    if (item.starsContainer != null && item.starImages != null)
                    {
                        item.starsContainer.SetActive(true);
                        int stars = PlayerPrefs.GetInt(PREF_STARS_PREFIX + item.levelNumber, 3);
                        if (stars < 1) stars = 3;
                        if (stars > 3) stars = 3;

                        for (int s = 0; s < 3; s++)
                        {
                            if (s < stars)
                            {
                                item.starImages[s].sprite = GetStarSprite(true);
                                item.starImages[s].color = Color.white;
                            }
                            else
                            {
                                item.starImages[s].sprite = GetStarSprite(false);
                                item.starImages[s].color = Color.white;
                            }
                        }
                    }

                    item.button.interactable = true;
                }
                else if (item.levelNumber == unlockedLevel)
                {
                    // CURRENT PLAYING -> GOLDEN COLOR with PULSE
                    item.outerRing.color = new Color(0.88f, 0.44f, 0.05f, 1f); // Amber gold rim
                    item.innerDisc.color = new Color(1.0f, 0.82f, 0.12f, 1f);  // Glowing Golden Yellow

                    item.iconImage.gameObject.SetActive(false);
                    item.numberText.gameObject.SetActive(true);
                    item.numberText.text = item.levelNumber.ToString();
                    item.numberText.fontSize = 44;
                    item.numberText.color = new Color(0.24f, 0.12f, 0.02f, 1f); // Dark amber bold text

                    item.textRt.anchorMin = Vector2.zero;
                    item.textRt.anchorMax = Vector2.one;
                    item.textRt.offsetMin = Vector2.zero;
                    item.textRt.offsetMax = Vector2.zero;

                    // If replaying or previously earned stars exist on this level, show them!
                    int prevStars = PlayerPrefs.GetInt(PREF_STARS_PREFIX + item.levelNumber, 0);
                    if (prevStars > 0 && item.starsContainer != null && item.starImages != null)
                    {
                        item.starsContainer.SetActive(true);
                        for (int s = 0; s < 3; s++)
                        {
                            if (s < prevStars)
                            {
                                item.starImages[s].sprite = GetStarSprite(true);
                                item.starImages[s].color = Color.white;
                            }
                            else
                            {
                                item.starImages[s].sprite = GetStarSprite(false);
                                item.starImages[s].color = Color.white;
                            }
                        }
                    }
                    else if (item.starsContainer != null)
                    {
                        item.starsContainer.SetActive(false);
                    }

                    item.button.interactable = true;
                    item.pulseCoroutine = StartCoroutine(PulseActiveNode(item.rootObj.transform));
                }
                else
                {
                    // LOCKED -> GREY COLOR with METALLIC PADLOCK ICON
                    if (item.starsContainer != null) item.starsContainer.SetActive(false);
                    item.outerRing.color = new Color(0.18f, 0.22f, 0.28f, 1f); // Slate dark rim
                    item.innerDisc.color = new Color(0.36f, 0.42f, 0.50f, 1f); // Slate grey disc

                    item.iconImage.gameObject.SetActive(true);
                    if (padlockSprite != null) item.iconImage.sprite = padlockSprite;
                    item.iconImage.color = new Color(0.95f, 0.95f, 0.95f, 0.95f);
                    item.iconRt.anchoredPosition = new Vector2(0, 10);
                    item.iconRt.sizeDelta = new Vector2(36, 36);

                    item.numberText.gameObject.SetActive(true);
                    item.numberText.text = item.levelNumber.ToString();
                    item.numberText.fontSize = 20;
                    item.numberText.color = new Color(0.85f, 0.88f, 0.92f, 0.9f);
                    item.textRt.anchorMin = new Vector2(0.5f, 0.22f);
                    item.textRt.anchorMax = new Vector2(0.5f, 0.22f);
                    item.textRt.pivot = new Vector2(0.5f, 0.5f);
                    item.textRt.sizeDelta = new Vector2(60, 24);
                    item.textRt.anchoredPosition = Vector2.zero;

                    item.button.interactable = true; // Clickable for wobble feedback!
                }
            }
        }

        private IEnumerator PulseActiveNode(Transform tr)
        {
            while (tr != null && currentState == ScreenState.LevelMap)
            {
                float t = Time.unscaledTime * 3.5f;
                float scale = 1.0f + 0.08f * Mathf.Sin(t);
                tr.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            if (tr != null) tr.localScale = Vector3.one;
        }

        private void OnMapNodeClicked(int levelNumber, Transform nodeTransform)
        {
            int unlockedLevel = PlayerPrefs.GetInt(PREF_UNLOCKED_LEVEL, 1);

            if (levelNumber <= unlockedLevel)
            {
                Debug.Log($"[ZombieSquad] Level {levelNumber} clicked! Launching Level {levelNumber}...");
                StartCoroutine(PunchScale(nodeTransform, 0.15f, () =>
                {
                    StartMission(levelNumber);
                }));
            }
            else
            {
                // Locked!
                Debug.Log($"[ZombieSquad] Level {levelNumber} is LOCKED! Complete Level {unlockedLevel} first.");
                StartCoroutine(WobbleShake(nodeTransform, 0.25f));
            }
        }

        private IEnumerator WobbleShake(Transform tr, float duration)
        {
            if (tr == null) yield break;
            Vector3 originalPos = tr.localPosition;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float offset = Mathf.Sin(elapsed * 45f) * 10f * (1f - (elapsed / duration));
                tr.localPosition = originalPos + new Vector3(offset, 0, 0);
                yield return null;
            }
            tr.localPosition = originalPos;
        }

        #region Zombie Squad Characters Screen

        private Sprite LoadZombieSquadCharactersScreenSprite()
        {
            try
            {
                Sprite s = Resources.Load<Sprite>("UI/ZombieSquad_CharactersScreen");
                if (s != null) return s;

                Texture2D resTex = Resources.Load<Texture2D>("UI/ZombieSquad_CharactersScreen");
                if (resTex != null)
                {
                    return Sprite.Create(resTex, new Rect(0, 0, resTex.width, resTex.height), new Vector2(0.5f, 0.5f));
                }

                string[] localPaths = new string[]
                {
                    System.IO.Path.Combine(Application.dataPath, "Resources", "UI", "ZombieSquad_CharactersScreen.png"),
                    System.IO.Path.Combine(Application.dataPath, "Resources", "UI", "ZombieSquad_CharactersScreen.jpg")
                };

                foreach (string localPath in localPaths)
                {
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
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[ZombieRunnerUIManager] CharactersScreen sprite loading: " + ex.Message);
            }
            return null;
        }

        private Sprite LoadHeroIcon(string heroKey)
        {
            try
            {
                Sprite s = Resources.Load<Sprite>($"UI/Hero_{heroKey}_Icon");
                if (s != null) return s;

                Texture2D resTex = Resources.Load<Texture2D>($"UI/Hero_{heroKey}_Icon");
                if (resTex != null)
                {
                    return Sprite.Create(resTex, new Rect(0, 0, resTex.width, resTex.height), new Vector2(0.5f, 0.5f));
                }

                string localPath = System.IO.Path.Combine(Application.dataPath, "Resources", "UI", $"Hero_{heroKey}_Icon.png");
                if (System.IO.File.Exists(localPath))
                {
                    byte[] fileData = System.IO.File.ReadAllBytes(localPath);
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (tex.LoadImage(fileData))
                    {
                        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ZombieRunnerUIManager] Hero {heroKey} icon loading: " + ex.Message);
            }
            return null;
        }

        private void BuildZombieSquadCharactersScreenPanel(Transform canvasRoot)
        {
            charactersPanel = CreateFullPanel("ZombieSquadCharactersPanel", canvasRoot, new Color(0.04f, 0.06f, 0.10f, 1f));

            // Container for 9:16 aspect ratio
            GameObject artContainer = new GameObject("ArtContainer");
            artContainer.transform.SetParent(charactersPanel.transform, false);
            RectTransform artRt = artContainer.AddComponent<RectTransform>();
            artRt.anchorMin = Vector2.zero;
            artRt.anchorMax = Vector2.one;
            artRt.offsetMin = Vector2.zero;
            artRt.offsetMax = Vector2.zero;


            // Background Image - exactly the uploaded artwork
            Image charImg = artContainer.AddComponent<Image>();
            Sprite charSprite = LoadZombieSquadCharactersScreenSprite();
            if (charSprite != null)
            {
                charImg.sprite = charSprite;
            }
            charImg.type = Image.Type.Simple;
            charImg.preserveAspect = false;
            charImg.raycastTarget = true;

            // Transparent Button Creator Helper (Instant zero-delay response!)
            Button CreateTransparentBtn(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, System.Action onClick)
            {
                GameObject btnObj = new GameObject(name);
                btnObj.transform.SetParent(parent, false);

                RectTransform rt = btnObj.AddComponent<RectTransform>();
                rt.anchorMin = anchorMin;
                rt.anchorMax = anchorMax;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                Image img = btnObj.AddComponent<Image>();
                img.color = new Color(1f, 1f, 1f, 0.001f);
                img.raycastTarget = true;

                Button btn = btnObj.AddComponent<Button>();
                btn.targetGraphic = img;

                ColorBlock cb = btn.colors;
                cb.normalColor = new Color(1f, 1f, 1f, 0.001f);
                cb.highlightedColor = new Color(1f, 1f, 1f, 0.15f);
                cb.pressedColor = new Color(0f, 0f, 0f, 0.25f);
                cb.selectedColor = new Color(1f, 1f, 1f, 0.001f);
                cb.fadeDuration = 0.05f;
                btn.colors = cb;

                if (onClick != null)
                {
                    btn.onClick.AddListener(() =>
                    {
                        onClick(); // Instant execution with ZERO lag!
                        StartCoroutine(PunchScale(btnObj.transform, 0.10f, null));
                    });
                }
                return btn;
            }

            // Top-Left Back Button to Home Screen: (0.025, 0.900) to (0.170, 0.980)
            Button backBtn = CreateTransparentBtn("Btn_Back", artContainer.transform, new Vector2(0.025f, 0.900f), new Vector2(0.170f, 0.980f), () =>
            {
                Debug.Log("[ZombieSquad] Characters Screen -> Back pressed -> Returning to Home Screen");
                ShowZombieSquadHomeScreen();
            });
            charactersBackBtnRect = backBtn.GetComponent<RectTransform>();

            // 5 Character Card Buttons (EXACT mapping over each card from the artwork!):
            // Row 1: Soldier, Ranger, Heavy
            CreateTransparentBtn("Btn_Card_Soldier", artContainer.transform, new Vector2(0.025f, 0.275f), new Vector2(0.335f, 0.495f), () => SelectCharacter("Soldier"));
            CreateTransparentBtn("Btn_Card_Ranger",  artContainer.transform, new Vector2(0.340f, 0.275f), new Vector2(0.655f, 0.495f), () => SelectCharacter("Ranger"));
            CreateTransparentBtn("Btn_Card_Heavy",   artContainer.transform, new Vector2(0.660f, 0.275f), new Vector2(0.975f, 0.495f), () => SelectCharacter("Heavy"));

            // Row 2: Sniper, Medic
            CreateTransparentBtn("Btn_Card_Sniper",  artContainer.transform, new Vector2(0.180f, 0.045f), new Vector2(0.495f, 0.270f), () => SelectCharacter("Sniper"));
            CreateTransparentBtn("Btn_Card_Medic",   artContainer.transform, new Vector2(0.510f, 0.045f), new Vector2(0.830f, 0.270f), () => SelectCharacter("Medic"));
        }

        public void SelectCharacter(string heroKey)
        {
            selectedCharacterKey = heroKey;
            PlayerPrefs.SetString("ZombieSquad_SelectedHero", heroKey);
            PlayerPrefs.Save();
            if (playerRunner != null)
            {
                playerRunner.ApplyEquippedHeroStats();
            }
            Debug.Log($"[ZombieSquad] Selected and Equipped Character: {heroKey}");
        }

        public void EquipSelectedCharacter()
        {
            SelectCharacter(selectedCharacterKey);
        }

        public void UpdateCharactersScreenVisuals()
        {
            // Follows user uploaded artwork exactly with zero added overlays
        }

        #endregion

        #region Zombie Squad Upgrade Screen

        private Sprite LoadZombieSquadUpgradeScreenSprite()
        {
            try
            {
                Sprite s = Resources.Load<Sprite>("UI/ZombieSquad_UpgradeScreen");
                if (s != null) return s;

                Texture2D resTex = Resources.Load<Texture2D>("UI/ZombieSquad_UpgradeScreen");
                if (resTex != null)
                {
                    return Sprite.Create(resTex, new Rect(0, 0, resTex.width, resTex.height), new Vector2(0.5f, 0.5f));
                }

                string[] localPaths = new string[]
                {
                    System.IO.Path.Combine(Application.dataPath, "Resources", "UI", "ZombieSquad_UpgradeScreen.png"),
                    System.IO.Path.Combine(Application.dataPath, "Resources", "UI", "ZombieSquad_UpgradeScreen.jpg")
                };

                foreach (string localPath in localPaths)
                {
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
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[ZombieRunnerUIManager] UpgradeScreen sprite loading: " + ex.Message);
            }
            return null;
        }

        private void BuildZombieSquadUpgradeScreenPanel(Transform canvasRoot)
        {
            upgradePanel = CreateFullPanel("ZombieSquadUpgradePanel", canvasRoot, new Color(0.04f, 0.06f, 0.10f, 1f));

            // Container for 9:16 aspect ratio
            GameObject artContainer = new GameObject("ArtContainer");
            artContainer.transform.SetParent(upgradePanel.transform, false);
            RectTransform artRt = artContainer.AddComponent<RectTransform>();
            artRt.anchorMin = Vector2.zero;
            artRt.anchorMax = Vector2.one;
            artRt.offsetMin = Vector2.zero;
            artRt.offsetMax = Vector2.zero;


            // Background Image - exactly the uploaded artwork
            Image upgImg = artContainer.AddComponent<Image>();
            Sprite upgSprite = LoadZombieSquadUpgradeScreenSprite();
            if (upgSprite != null)
            {
                upgImg.sprite = upgSprite;
            }
            upgImg.type = Image.Type.Simple;
            upgImg.preserveAspect = false;
            upgImg.raycastTarget = true;

            // Transparent Button Creator Helper (Instant zero-delay response!)
            Button CreateTransparentBtn(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, System.Action onClick)
            {
                GameObject btnObj = new GameObject(name);
                btnObj.transform.SetParent(parent, false);

                RectTransform rt = btnObj.AddComponent<RectTransform>();
                rt.anchorMin = anchorMin;
                rt.anchorMax = anchorMax;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                Image img = btnObj.AddComponent<Image>();
                img.color = new Color(1f, 1f, 1f, 0.001f);
                img.raycastTarget = true;

                Button btn = btnObj.AddComponent<Button>();
                btn.targetGraphic = img;

                ColorBlock cb = btn.colors;
                cb.normalColor = new Color(1f, 1f, 1f, 0.001f);
                cb.highlightedColor = new Color(1f, 1f, 1f, 0.15f);
                cb.pressedColor = new Color(0f, 0f, 0f, 0.25f);
                cb.selectedColor = new Color(1f, 1f, 1f, 0.001f);
                cb.fadeDuration = 0.05f;
                btn.colors = cb;

                if (onClick != null)
                {
                    btn.onClick.AddListener(() =>
                    {
                        onClick(); // Instant execution with ZERO lag!
                        StartCoroutine(PunchScale(btnObj.transform, 0.10f, null));
                    });
                }
                return btn;
            }

            // Top-Left Back Button to Home Screen: (0.025, 0.900) to (0.170, 0.980)
            Button backBtn = CreateTransparentBtn("Btn_Back", artContainer.transform, new Vector2(0.025f, 0.900f), new Vector2(0.170f, 0.980f), () =>
            {
                Debug.Log("[ZombieSquad] Upgrade Screen -> Back pressed -> Returning to Home Screen");
                ShowZombieSquadHomeScreen();
            });
            upgradeBackBtnRect = backBtn.GetComponent<RectTransform>();

            // 4 Upgrade Buttons (Green Buttons / Cards EXACT Pixel Mapping from the artwork!):
            // 1. Soldier Count (Top-Left)
            CreateTransparentBtn("Btn_Upgrade_Count", artContainer.transform, new Vector2(0.050f, 0.500f), new Vector2(0.460f, 0.600f), () => UpgradeStat("Count"));

            // 2. Soldier Health (Top-Right)
            CreateTransparentBtn("Btn_Upgrade_Health", artContainer.transform, new Vector2(0.540f, 0.500f), new Vector2(0.950f, 0.600f), () => UpgradeStat("Health"));

            // 3. Soldier Damage (Bottom-Left)
            CreateTransparentBtn("Btn_Upgrade_Damage", artContainer.transform, new Vector2(0.050f, 0.060f), new Vector2(0.460f, 0.160f), () => UpgradeStat("Damage"));

            // 4. Soldier Speed (Bottom-Right)
            CreateTransparentBtn("Btn_Upgrade_Speed", artContainer.transform, new Vector2(0.540f, 0.060f), new Vector2(0.950f, 0.160f), () => UpgradeStat("Speed"));
        }

        public void UpgradeStat(string statName)
        {
            string prefKey = $"ZombieSquad_Upgrade_{statName}";
            int currentLevel = PlayerPrefs.GetInt(prefKey, 0);
            int newLevel = currentLevel + 1;
            PlayerPrefs.SetInt(prefKey, newLevel);
            PlayerPrefs.Save();

            Debug.Log($"[ZombieSquad] Upgraded {statName} to Level {newLevel}!");
            UpdateUpgradeScreenVisuals();
        }

        public void UpdateUpgradeScreenVisuals()
        {
            // Follows user uploaded artwork exactly with zero added overlays
        }

        #endregion

        #region Zombie Squad Weapons Screen

        private Sprite LoadZombieSquadWeaponsScreenSprite()
        {
            try
            {
                Sprite s = Resources.Load<Sprite>("UI/ZombieSquad_WeaponsScreen");
                if (s != null) return s;

                Texture2D resTex = Resources.Load<Texture2D>("UI/ZombieSquad_WeaponsScreen");
                if (resTex != null)
                {
                    return Sprite.Create(resTex, new Rect(0, 0, resTex.width, resTex.height), new Vector2(0.5f, 0.5f));
                }

                string[] localPaths = new string[]
                {
                    System.IO.Path.Combine(Application.dataPath, "Resources", "UI", "ZombieSquad_WeaponsScreen.png"),
                    System.IO.Path.Combine(Application.dataPath, "Resources", "UI", "ZombieSquad_WeaponsScreen.jpg")
                };

                foreach (string localPath in localPaths)
                {
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
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[ZombieRunnerUIManager] WeaponsScreen sprite loading: " + ex.Message);
            }
            return null;
        }

        private static readonly string[] s_weaponKeys = new string[] { "AssaultRifle", "Shotgun", "Sniper", "Minigun" };

        private void BuildZombieSquadWeaponsScreenPanel(Transform canvasRoot)
        {
            weaponsPanel = CreateFullPanel("ZombieSquadWeaponsPanel", canvasRoot, new Color(0.04f, 0.06f, 0.10f, 1f));

            // Container for 9:16 aspect ratio
            GameObject artContainer = new GameObject("ArtContainer");
            artContainer.transform.SetParent(weaponsPanel.transform, false);
            RectTransform artRt = artContainer.AddComponent<RectTransform>();
            artRt.anchorMin = Vector2.zero;
            artRt.anchorMax = Vector2.one;
            artRt.offsetMin = Vector2.zero;
            artRt.offsetMax = Vector2.zero;


            // Background Image - exactly the uploaded artwork
            Image wepImg = artContainer.AddComponent<Image>();
            weaponsScreenBgImage = wepImg;
            Sprite wepSprite = LoadZombieSquadWeaponsScreenSprite();
            if (wepSprite != null)
            {
                wepImg.sprite = wepSprite;
            }
            wepImg.type = Image.Type.Simple;
            wepImg.preserveAspect = false;
            wepImg.raycastTarget = true;

            // Transparent Button Creator Helper (Instant zero-delay response!)
            Button CreateTransparentBtn(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, System.Action onClick)
            {
                GameObject btnObj = new GameObject(name);
                btnObj.transform.SetParent(parent, false);

                RectTransform rt = btnObj.AddComponent<RectTransform>();
                rt.anchorMin = anchorMin;
                rt.anchorMax = anchorMax;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                Image img = btnObj.AddComponent<Image>();
                img.color = new Color(1f, 1f, 1f, 0.001f);
                img.raycastTarget = true;

                Button btn = btnObj.AddComponent<Button>();
                btn.targetGraphic = img;

                ColorBlock cb = btn.colors;
                cb.normalColor = new Color(1f, 1f, 1f, 0.001f);
                cb.highlightedColor = new Color(1f, 1f, 1f, 0.15f);
                cb.pressedColor = new Color(0f, 0f, 0f, 0.25f);
                cb.selectedColor = new Color(1f, 1f, 1f, 0.001f);
                cb.fadeDuration = 0.05f;
                btn.colors = cb;

                if (onClick != null)
                {
                    btn.onClick.AddListener(() =>
                    {
                        onClick(); // Instant execution with ZERO lag!
                        StartCoroutine(PunchScale(btnObj.transform, 0.10f, null));
                    });
                }
                return btn;
            }

            // Top-Left Back Button to Home Screen: (0.025, 0.900) to (0.170, 0.980)
            Button backBtn = CreateTransparentBtn("Btn_Back", artContainer.transform, new Vector2(0.025f, 0.900f), new Vector2(0.170f, 0.980f), () =>
            {
                Debug.Log("[ZombieSquad] Weapons Screen -> Back pressed -> Returning to Home Screen");
                ShowZombieSquadHomeScreen();
            });
            weaponsBackBtnRect = backBtn.GetComponent<RectTransform>();

            // Carousel Arrow Buttons (< and >)
            CreateTransparentBtn("Btn_Prev_Weapon", artContainer.transform, new Vector2(0.010f, 0.700f), new Vector2(0.100f, 0.790f), () => CycleWeapon(-1));
            CreateTransparentBtn("Btn_Next_Weapon", artContainer.transform, new Vector2(0.900f, 0.700f), new Vector2(0.990f, 0.790f), () => CycleWeapon(1));

            // 4 Stat Upgrade Buttons (Green Up-Arrow buttons on the right side):
            // Row 1: Damage
            CreateTransparentBtn("Btn_Upgrade_Damage", artContainer.transform, new Vector2(0.740f, 0.450f), new Vector2(0.960f, 0.540f), () => UpgradeWeaponStat("Damage"));

            // Row 2: Fire Rate
            CreateTransparentBtn("Btn_Upgrade_FireRate", artContainer.transform, new Vector2(0.740f, 0.360f), new Vector2(0.960f, 0.450f), () => UpgradeWeaponStat("FireRate"));

            // Row 3: Range
            CreateTransparentBtn("Btn_Upgrade_Range", artContainer.transform, new Vector2(0.740f, 0.270f), new Vector2(0.960f, 0.360f), () => UpgradeWeaponStat("Range"));

            // Row 4: Magazine
            CreateTransparentBtn("Btn_Upgrade_Magazine", artContainer.transform, new Vector2(0.740f, 0.180f), new Vector2(0.960f, 0.270f), () => UpgradeWeaponStat("Magazine"));

            // 4 Bottom Weapon Cards:
            CreateTransparentBtn("Btn_Weapon_AssaultRifle", artContainer.transform, new Vector2(0.020f, 0.025f), new Vector2(0.260f, 0.160f), () => SelectWeapon("AssaultRifle"));
            CreateTransparentBtn("Btn_Weapon_Shotgun",      artContainer.transform, new Vector2(0.265f, 0.025f), new Vector2(0.505f, 0.160f), () => SelectWeapon("Shotgun"));
            CreateTransparentBtn("Btn_Weapon_Sniper",       artContainer.transform, new Vector2(0.510f, 0.025f), new Vector2(0.750f, 0.160f), () => SelectWeapon("Sniper"));
            CreateTransparentBtn("Btn_Weapon_Minigun",      artContainer.transform, new Vector2(0.755f, 0.025f), new Vector2(0.980f, 0.160f), () => SelectWeapon("Minigun"));
        }

        public void CycleWeapon(int direction)
        {
            string current = PlayerPrefs.GetString("ZombieSquad_SelectedWeapon", "AssaultRifle");
            int idx = System.Array.IndexOf(s_weaponKeys, current);
            if (idx < 0) idx = 0;
            idx = (idx + direction + s_weaponKeys.Length) % s_weaponKeys.Length;
            SelectWeapon(s_weaponKeys[idx]);
        }

        public void SelectWeapon(string weaponKey)
        {
            PlayerPrefs.SetString("ZombieSquad_SelectedWeapon", weaponKey);
            PlayerPrefs.Save();
            Debug.Log($"[ZombieSquad] Selected Weapon: {weaponKey}");
            UpdateWeaponsScreenVisuals();
        }

        public void UpgradeWeaponStat(string statName)
        {
            string currentWeapon = PlayerPrefs.GetString("ZombieSquad_SelectedWeapon", "AssaultRifle");
            string prefKey = $"ZombieSquad_Wep_{currentWeapon}_{statName}";
            int currentLevel = PlayerPrefs.GetInt(prefKey, 0);
            int newLevel = currentLevel + 1;
            PlayerPrefs.SetInt(prefKey, newLevel);
            PlayerPrefs.Save();

            Debug.Log($"[ZombieSquad] Upgraded {currentWeapon} {statName} to Level {newLevel}!");
            UpdateWeaponsScreenVisuals();
        }

        public void UpdateWeaponsScreenVisuals()
        {
            if (weaponsScreenBgImage != null)
            {
                Sprite sp = LoadZombieSquadWeaponsScreenSprite();
                if (sp != null) weaponsScreenBgImage.sprite = sp;
            }
        }

        #endregion

        #region Zombie Squad Shop Screen

        private Sprite LoadZombieSquadShopScreenSprite()
        {
            try
            {
                Sprite s = Resources.Load<Sprite>("UI/ZombieSquad_ShopScreen");
                if (s != null) return s;

                Texture2D resTex = Resources.Load<Texture2D>("UI/ZombieSquad_ShopScreen");
                if (resTex != null)
                {
                    return Sprite.Create(resTex, new Rect(0, 0, resTex.width, resTex.height), new Vector2(0.5f, 0.5f));
                }

                string[] localPaths = new string[]
                {
                    System.IO.Path.Combine(Application.dataPath, "Resources", "UI", "ZombieSquad_ShopScreen.png"),
                    System.IO.Path.Combine(Application.dataPath, "Resources", "UI", "ZombieSquad_ShopScreen.jpg")
                };

                foreach (string localPath in localPaths)
                {
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
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[ZombieRunnerUIManager] ShopScreen sprite loading: " + ex.Message);
            }
            return null;
        }

        private void BuildZombieSquadShopScreenPanel(Transform canvasRoot)
        {
            shopPanel = CreateFullPanel("ZombieSquadShopPanel", canvasRoot, new Color(0.04f, 0.06f, 0.10f, 1f));

            // Container for 9:16 aspect ratio
            GameObject artContainer = new GameObject("ArtContainer");
            artContainer.transform.SetParent(shopPanel.transform, false);
            RectTransform artRt = artContainer.AddComponent<RectTransform>();
            artRt.anchorMin = Vector2.zero;
            artRt.anchorMax = Vector2.one;
            artRt.offsetMin = Vector2.zero;
            artRt.offsetMax = Vector2.zero;


            // Background Image - exactly the uploaded artwork
            Image shopImg = artContainer.AddComponent<Image>();
            shopScreenBgImage = shopImg;
            Sprite shopSprite = LoadZombieSquadShopScreenSprite();
            if (shopSprite != null)
            {
                shopImg.sprite = shopSprite;
            }
            shopImg.type = Image.Type.Simple;
            shopImg.preserveAspect = false;
            shopImg.raycastTarget = true;

            // Transparent Button Creator Helper (Instant zero-delay response!)
            Button CreateTransparentBtn(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, System.Action onClick)
            {
                GameObject btnObj = new GameObject(name);
                btnObj.transform.SetParent(parent, false);

                RectTransform rt = btnObj.AddComponent<RectTransform>();
                rt.anchorMin = anchorMin;
                rt.anchorMax = anchorMax;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                Image img = btnObj.AddComponent<Image>();
                img.color = new Color(1f, 1f, 1f, 0.001f);
                img.raycastTarget = true;

                Button btn = btnObj.AddComponent<Button>();
                btn.targetGraphic = img;

                ColorBlock cb = btn.colors;
                cb.normalColor = new Color(1f, 1f, 1f, 0.001f);
                cb.highlightedColor = new Color(1f, 1f, 1f, 0.15f);
                cb.pressedColor = new Color(0f, 0f, 0f, 0.25f);
                cb.selectedColor = new Color(1f, 1f, 1f, 0.001f);
                cb.fadeDuration = 0.05f;
                btn.colors = cb;

                if (onClick != null)
                {
                    btn.onClick.AddListener(() =>
                    {
                        onClick(); // Instant execution with ZERO lag!
                        StartCoroutine(PunchScale(btnObj.transform, 0.10f, null));
                    });
                }
                return btn;
            }

            // Top-Left Back Button to Home Screen: (0.025, 0.900) to (0.170, 0.980)
            Button backBtn = CreateTransparentBtn("Btn_Back", artContainer.transform, new Vector2(0.025f, 0.900f), new Vector2(0.170f, 0.980f), () =>
            {
                Debug.Log("[ZombieSquad] Shop Screen -> Back pressed -> Returning to Home Screen");
                ShowZombieSquadHomeScreen();
            });
            shopBackBtnRect = backBtn.GetComponent<RectTransform>();

            // Category Tabs: Coins, Diamonds, Special, Skins
            CreateTransparentBtn("Btn_Tab_Coins",    artContainer.transform, new Vector2(0.025f, 0.810f), new Vector2(0.260f, 0.875f), () => SelectShopTab("Coins"));
            CreateTransparentBtn("Btn_Tab_Diamonds", artContainer.transform, new Vector2(0.265f, 0.810f), new Vector2(0.505f, 0.875f), () => SelectShopTab("Diamonds"));
            CreateTransparentBtn("Btn_Tab_Special",  artContainer.transform, new Vector2(0.510f, 0.810f), new Vector2(0.745f, 0.875f), () => SelectShopTab("Special"));
            CreateTransparentBtn("Btn_Tab_Skins",    artContainer.transform, new Vector2(0.750f, 0.810f), new Vector2(0.975f, 0.875f), () => SelectShopTab("Skins"));

            // 4 Coin Pack Green Buttons:
            // 1. 1,000 Coins (Top-Left)
            CreateTransparentBtn("Btn_Buy_1000_Coins",  artContainer.transform, new Vector2(0.055f, 0.480f), new Vector2(0.455f, 0.555f), () => BuyCoins(1000));

            // 2. 5,000 Coins (Top-Right)
            CreateTransparentBtn("Btn_Buy_5000_Coins",  artContainer.transform, new Vector2(0.550f, 0.480f), new Vector2(0.945f, 0.555f), () => BuyCoins(5000));

            // 3. 20,000 Coins (Bottom-Left)
            CreateTransparentBtn("Btn_Buy_20000_Coins", artContainer.transform, new Vector2(0.055f, 0.105f), new Vector2(0.455f, 0.180f), () => BuyCoins(20000));

            // 4. 50,000 Coins (Bottom-Right)
            CreateTransparentBtn("Btn_Buy_50000_Coins", artContainer.transform, new Vector2(0.550f, 0.105f), new Vector2(0.945f, 0.180f), () => BuyCoins(50000));
        }

        public void SelectShopTab(string tabKey)
        {
            Debug.Log($"[ZombieSquad] Selected Shop Tab: {tabKey}");
        }

        public void BuyCoins(int amount)
        {
            int currentCoins = PlayerPrefs.GetInt(PREF_COINS, 250);
            currentCoins += amount;
            PlayerPrefs.SetInt(PREF_COINS, currentCoins);
            PlayerPrefs.Save();

            Debug.Log($"[ZombieSquad] Purchased {amount} Coins! New Balance: {currentCoins}");
            UpdateCurrencyHud();
        }

        public void UpdateShopScreenVisuals()
        {
            if (shopScreenBgImage != null)
            {
                Sprite sp = LoadZombieSquadShopScreenSprite();
                if (sp != null) shopScreenBgImage.sprite = sp;
            }
        }

        #endregion

        #region Zombie Squad Missions Screen

        private Sprite LoadZombieSquadMissionsScreenSprite()
        {
            try
            {
                Sprite s = Resources.Load<Sprite>("UI/ZombieSquad_MissionsScreen");
                if (s != null) return s;

                Texture2D resTex = Resources.Load<Texture2D>("UI/ZombieSquad_MissionsScreen");
                if (resTex != null)
                {
                    return Sprite.Create(resTex, new Rect(0, 0, resTex.width, resTex.height), new Vector2(0.5f, 0.5f));
                }

                string[] localPaths = new string[]
                {
                    System.IO.Path.Combine(Application.dataPath, "Resources", "UI", "ZombieSquad_MissionsScreen.png"),
                    System.IO.Path.Combine(Application.dataPath, "Resources", "UI", "ZombieSquad_MissionsScreen.jpg")
                };

                foreach (string localPath in localPaths)
                {
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
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[ZombieRunnerUIManager] MissionsScreen sprite loading: " + ex.Message);
            }
            return null;
        }

        private void BuildZombieSquadMissionsScreenPanel(Transform canvasRoot)
        {
            missionsPanel = CreateFullPanel("ZombieSquadMissionsPanel", canvasRoot, new Color(0.04f, 0.06f, 0.10f, 1f));

            // Container for 9:16 aspect ratio
            GameObject artContainer = new GameObject("ArtContainer");
            artContainer.transform.SetParent(missionsPanel.transform, false);
            RectTransform artRt = artContainer.AddComponent<RectTransform>();
            artRt.anchorMin = Vector2.zero;
            artRt.anchorMax = Vector2.one;
            artRt.offsetMin = Vector2.zero;
            artRt.offsetMax = Vector2.zero;


            // Background Image - exactly the uploaded artwork
            Image missionsImg = artContainer.AddComponent<Image>();
            missionsScreenBgImage = missionsImg;
            Sprite missionsSprite = LoadZombieSquadMissionsScreenSprite();
            if (missionsSprite != null)
            {
                missionsImg.sprite = missionsSprite;
            }
            missionsImg.type = Image.Type.Simple;
            missionsImg.preserveAspect = false;
            missionsImg.raycastTarget = true;

            // Transparent Button Creator Helper (Instant zero-delay response!)
            Button CreateTransparentBtn(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, System.Action onClick)
            {
                GameObject btnObj = new GameObject(name);
                btnObj.transform.SetParent(parent, false);

                RectTransform rt = btnObj.AddComponent<RectTransform>();
                rt.anchorMin = anchorMin;
                rt.anchorMax = anchorMax;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                Image img = btnObj.AddComponent<Image>();
                img.color = new Color(1f, 1f, 1f, 0.001f);
                img.raycastTarget = true;

                Button btn = btnObj.AddComponent<Button>();
                btn.targetGraphic = img;

                ColorBlock cb = btn.colors;
                cb.normalColor = new Color(1f, 1f, 1f, 0.001f);
                cb.highlightedColor = new Color(1f, 1f, 1f, 0.15f);
                cb.pressedColor = new Color(0f, 0f, 0f, 0.25f);
                cb.selectedColor = new Color(1f, 1f, 1f, 0.001f);
                cb.fadeDuration = 0.05f;
                btn.colors = cb;

                if (onClick != null)
                {
                    btn.onClick.AddListener(() =>
                    {
                        onClick(); // Instant execution with ZERO lag!
                        StartCoroutine(PunchScale(btnObj.transform, 0.10f, null));
                    });
                }
                return btn;
            }

            // Top-Left Back Button to Home Screen: (0.025, 0.900) to (0.170, 0.980)
            Button backBtn = CreateTransparentBtn("Btn_Back", artContainer.transform, new Vector2(0.025f, 0.900f), new Vector2(0.170f, 0.980f), () =>
            {
                Debug.Log("[ZombieSquad] Missions Screen -> Back pressed -> Returning to Home Screen");
                ShowZombieSquadHomeScreen();
            });
            missionsBackBtnRect = backBtn.GetComponent<RectTransform>();

            // 3 Claim Buttons:
            // Mission 1: Play 30 Minutes -> 100 Coins
            CreateTransparentBtn("Btn_Claim_Mission1", artContainer.transform, new Vector2(0.680f, 0.485f), new Vector2(0.940f, 0.550f), () => ClaimMissionReward(1, 100));

            // Mission 2: Play 60 Minutes -> 200 Coins
            CreateTransparentBtn("Btn_Claim_Mission2", artContainer.transform, new Vector2(0.680f, 0.270f), new Vector2(0.940f, 0.335f), () => ClaimMissionReward(2, 200));

            // Mission 3: Play 4 Hours -> 500 Coins
            CreateTransparentBtn("Btn_Claim_Mission3", artContainer.transform, new Vector2(0.680f, 0.055f), new Vector2(0.940f, 0.120f), () => ClaimMissionReward(3, 500));
        }

        public void ClaimMissionReward(int missionId, int coinsReward)
        {
            int currentCoins = PlayerPrefs.GetInt(PREF_COINS, 250);
            currentCoins += coinsReward;
            PlayerPrefs.SetInt(PREF_COINS, currentCoins);
            PlayerPrefs.Save();
            Debug.Log($"[ZombieSquad] Claimed Mission {missionId} Reward: +{coinsReward} Coins! Total: {currentCoins}");
            UpdateCurrencyHud();
        }

        public void UpdateMissionsScreenVisuals()
        {
            if (missionsScreenBgImage != null)
            {
                Sprite sp = LoadZombieSquadMissionsScreenSprite();
                if (sp != null) missionsScreenBgImage.sprite = sp;
            }
        }

        #endregion

        #region Zombie Squad Daily Screen

        private Sprite LoadZombieSquadDailyScreenSprite()
        {
            try
            {
                Sprite s = Resources.Load<Sprite>("UI/ZombieSquad_DailyScreen");
                if (s != null) return s;

                Texture2D resTex = Resources.Load<Texture2D>("UI/ZombieSquad_DailyScreen");
                if (resTex != null)
                {
                    return Sprite.Create(resTex, new Rect(0, 0, resTex.width, resTex.height), new Vector2(0.5f, 0.5f));
                }

                string[] localPaths = new string[]
                {
                    System.IO.Path.Combine(Application.dataPath, "Resources", "UI", "ZombieSquad_DailyScreen.png"),
                    System.IO.Path.Combine(Application.dataPath, "Resources", "UI", "ZombieSquad_DailyScreen.jpg")
                };

                foreach (string localPath in localPaths)
                {
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
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[ZombieRunnerUIManager] DailyScreen sprite loading: " + ex.Message);
            }
            return null;
        }

        private void BuildZombieSquadDailyScreenPanel(Transform canvasRoot)
        {
            dailyPanel = CreateFullPanel("ZombieSquadDailyPanel", canvasRoot, new Color(0.04f, 0.06f, 0.10f, 1f));

            // Container for 9:16 aspect ratio
            GameObject artContainer = new GameObject("ArtContainer");
            artContainer.transform.SetParent(dailyPanel.transform, false);
            RectTransform artRt = artContainer.AddComponent<RectTransform>();
            artRt.anchorMin = Vector2.zero;
            artRt.anchorMax = Vector2.one;
            artRt.offsetMin = Vector2.zero;
            artRt.offsetMax = Vector2.zero;


            // Background Image - exactly the uploaded artwork
            Image dailyImg = artContainer.AddComponent<Image>();
            dailyScreenBgImage = dailyImg;
            Sprite dailySprite = LoadZombieSquadDailyScreenSprite();
            if (dailySprite != null)
            {
                dailyImg.sprite = dailySprite;
            }
            dailyImg.type = Image.Type.Simple;
            dailyImg.preserveAspect = false;
            dailyImg.raycastTarget = true;

            // Transparent Button Creator Helper (Instant zero-delay response!)
            Button CreateTransparentBtn(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, System.Action onClick)
            {
                GameObject btnObj = new GameObject(name);
                btnObj.transform.SetParent(parent, false);

                RectTransform rt = btnObj.AddComponent<RectTransform>();
                rt.anchorMin = anchorMin;
                rt.anchorMax = anchorMax;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                Image img = btnObj.AddComponent<Image>();
                img.color = new Color(1f, 1f, 1f, 0.001f);
                img.raycastTarget = true;

                Button btn = btnObj.AddComponent<Button>();
                btn.targetGraphic = img;

                ColorBlock cb = btn.colors;
                cb.normalColor = new Color(1f, 1f, 1f, 0.001f);
                cb.highlightedColor = new Color(1f, 1f, 1f, 0.15f);
                cb.pressedColor = new Color(0f, 0f, 0f, 0.25f);
                cb.selectedColor = new Color(1f, 1f, 1f, 0.001f);
                cb.fadeDuration = 0.05f;
                btn.colors = cb;

                if (onClick != null)
                {
                    btn.onClick.AddListener(() =>
                    {
                        onClick(); // Instant execution with ZERO lag!
                        StartCoroutine(PunchScale(btnObj.transform, 0.10f, null));
                    });
                }
                return btn;
            }

            // Top-Left Back Button to Home Screen: (0.025, 0.900) to (0.170, 0.980)
            Button backBtn = CreateTransparentBtn("Btn_Back", artContainer.transform, new Vector2(0.025f, 0.900f), new Vector2(0.170f, 0.980f), () =>
            {
                Debug.Log("[ZombieSquad] Daily Screen -> Back pressed -> Returning to Home Screen");
                ShowZombieSquadHomeScreen();
            });
            dailyBackBtnRect = backBtn.GetComponent<RectTransform>();

            // Day 1 to 7 reward buttons:
            int[] dailyAmounts = new int[] { 10, 20, 30, 40, 50, 60, 100 };
            float[] colStarts = new float[] { 0.020f, 0.155f, 0.292f, 0.429f, 0.566f, 0.703f, 0.840f };
            float[] colEnds = new float[] { 0.155f, 0.292f, 0.429f, 0.566f, 0.703f, 0.840f, 0.980f };

            for (int i = 0; i < 7; i++)
            {
                int day = i + 1;
                int rewardCoins = dailyAmounts[i];
                Vector2 min = new Vector2(colStarts[i], 0.300f);
                Vector2 max = new Vector2(colEnds[i], 0.530f);
                CreateTransparentBtn($"Btn_Day_{day}", artContainer.transform, min, max, () => ClaimDailyReward(day, rewardCoins));
            }
        }

        public void ClaimDailyReward(int day, int coinsReward)
        {
            int currentCoins = PlayerPrefs.GetInt(PREF_COINS, 250);
            currentCoins += coinsReward;
            PlayerPrefs.SetInt(PREF_COINS, currentCoins);
            PlayerPrefs.Save();
            Debug.Log($"[ZombieSquad] Claimed Day {day} Daily Reward: +{coinsReward} Coins! Total: {currentCoins}");
            UpdateCurrencyHud();
        }

        public void UpdateDailyScreenVisuals()
        {
            if (dailyScreenBgImage != null)
            {
                Sprite sp = LoadZombieSquadDailyScreenSprite();
                if (sp != null) dailyScreenBgImage.sprite = sp;
            }
        }

        #endregion

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

            // Top Header Bar (Matches reference screenshot exactly)
            GameObject headerBar = new GameObject("TopHeaderBar");
            headerBar.transform.SetParent(gameplayHudPanel.transform, false);
            RectTransform hbRt = headerBar.AddComponent<RectTransform>();
            hbRt.anchorMin = new Vector2(0f, 1f);
            hbRt.anchorMax = new Vector2(1f, 1f);
            hbRt.pivot = new Vector2(0.5f, 1f);
            hbRt.sizeDelta = new Vector2(0f, 130f);
            hbRt.anchoredPosition = new Vector2(0f, 0f);

            // 1. Back Button [←] (Top Left rounded blue button with cyan border)
            GameObject backBtnObj = new GameObject("BackBtn");
            backBtnObj.transform.SetParent(headerBar.transform, false);
            RectTransform bRt = backBtnObj.AddComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0f, 1f);
            bRt.anchorMax = new Vector2(0f, 1f);
            bRt.pivot = new Vector2(0.5f, 0.5f);
            bRt.sizeDelta = new Vector2(76f, 72f);
            bRt.anchoredPosition = new Vector2(65f, -65f);
            Image bImg = backBtnObj.AddComponent<Image>();
            bImg.color = new Color(0.10f, 0.44f, 0.92f, 1f);
            bImg.raycastTarget = true;
            Button bBtn = backBtnObj.AddComponent<Button>();
            bBtn.targetGraphic = bImg;
            bBtn.onClick.AddListener(() =>
            {
                ShowZombieSquadLevelMap();
            });

            TextMeshProUGUI bTxt = CreateText("Arrow", backBtnObj.transform, "←", 38, Color.white, TextAlignmentOptions.Center, true);
            bTxt.rectTransform.anchoredPosition = new Vector2(0, 2);

            // 2. LEVEL 1 Pill Badge (Royal Blue with metallic tone)
            GameObject levelPill = new GameObject("LevelPill");
            levelPill.transform.SetParent(headerBar.transform, false);
            RectTransform lpRt = levelPill.AddComponent<RectTransform>();
            lpRt.anchorMin = new Vector2(0f, 1f);
            lpRt.anchorMax = new Vector2(0f, 1f);
            lpRt.pivot = new Vector2(0f, 0.5f);
            lpRt.sizeDelta = new Vector2(215f, 66f);
            lpRt.anchoredPosition = new Vector2(115f, -65f);
            Image lpImg = levelPill.AddComponent<Image>();
            lpImg.color = new Color(0.06f, 0.35f, 0.82f, 1f);
            lpImg.raycastTarget = false;

            hudLevelText = CreateText("LevelText", levelPill.transform, "LEVEL 1", 28, Color.white, TextAlignmentOptions.Center, true);
            hudLevelText.rectTransform.anchoredPosition = Vector2.zero;

            // 3. Coin Counter Pill [Gold Icon + 12,340 + Green Button]
            GameObject coinPill = new GameObject("CoinPill");
            coinPill.transform.SetParent(headerBar.transform, false);
            RectTransform cpRt = coinPill.AddComponent<RectTransform>();
            cpRt.anchorMin = new Vector2(1f, 1f);
            cpRt.anchorMax = new Vector2(1f, 1f);
            cpRt.pivot = new Vector2(1f, 0.5f);
            cpRt.sizeDelta = new Vector2(210f, 60f);
            cpRt.anchoredPosition = new Vector2(-235f, -65f);
            Image cpImg = coinPill.AddComponent<Image>();
            cpImg.color = new Color(0.04f, 0.16f, 0.40f, 0.95f);
            cpImg.raycastTarget = false;

            // Gold Coin Icon (Clean round coin graphic)
            GameObject coinIcon = new GameObject("CoinIcon");
            coinIcon.transform.SetParent(coinPill.transform, false);
            RectTransform ciRt = coinIcon.AddComponent<RectTransform>();
            ciRt.anchorMin = new Vector2(0f, 0.5f);
            ciRt.anchorMax = new Vector2(0f, 0.5f);
            ciRt.pivot = new Vector2(0f, 0.5f);
            ciRt.sizeDelta = new Vector2(34f, 34f);
            ciRt.anchoredPosition = new Vector2(10f, 0f);
            Image ciImg = coinIcon.AddComponent<Image>();
            ciImg.color = new Color(1f, 0.82f, 0.12f, 1f);

            TextMeshProUGUI cpTxt = CreateText("Coins", coinPill.transform, "12,340", 22, Color.white, TextAlignmentOptions.MidlineLeft, true);
            cpTxt.rectTransform.anchoredPosition = new Vector2(40f, 0f);

            GameObject coinPlus = new GameObject("PlusBtn");
            coinPlus.transform.SetParent(coinPill.transform, false);
            RectTransform cPlusRt = coinPlus.AddComponent<RectTransform>();
            cPlusRt.anchorMin = new Vector2(1f, 0.5f);
            cPlusRt.anchorMax = new Vector2(1f, 0.5f);
            cPlusRt.pivot = new Vector2(1f, 0.5f);
            cPlusRt.sizeDelta = new Vector2(44f, 44f);
            cPlusRt.anchoredPosition = new Vector2(-8f, 0f);
            Image cPlusImg = coinPlus.AddComponent<Image>();
            cPlusImg.color = new Color(0.18f, 0.78f, 0.28f, 1f);
            TextMeshProUGUI cPlusTxt = CreateText("PlusTxt", coinPlus.transform, "+", 26, Color.white, TextAlignmentOptions.Center, true);
            cPlusTxt.rectTransform.anchoredPosition = Vector2.zero;

            // 4. Gem Counter Pill [Cyan Diamond + 520 + Green Button]
            GameObject gemPill = new GameObject("GemPill");
            gemPill.transform.SetParent(headerBar.transform, false);
            RectTransform gpRt = gemPill.AddComponent<RectTransform>();
            gpRt.anchorMin = new Vector2(1f, 1f);
            gpRt.anchorMax = new Vector2(1f, 1f);
            gpRt.pivot = new Vector2(1f, 0.5f);
            gpRt.sizeDelta = new Vector2(185f, 60f);
            gpRt.anchoredPosition = new Vector2(-20f, -65f);
            Image gpImg = gemPill.AddComponent<Image>();
            gpImg.color = new Color(0.04f, 0.16f, 0.40f, 0.95f);
            gpImg.raycastTarget = false;

            // Cyan Diamond Gem Icon (Rotated 45-degree square)
            GameObject gemIcon = new GameObject("GemIcon");
            gemIcon.transform.SetParent(gemPill.transform, false);
            RectTransform giRt = gemIcon.AddComponent<RectTransform>();
            giRt.anchorMin = new Vector2(0f, 0.5f);
            giRt.anchorMax = new Vector2(0f, 0.5f);
            giRt.pivot = new Vector2(0.5f, 0.5f);
            giRt.sizeDelta = new Vector2(24f, 24f);
            giRt.anchoredPosition = new Vector2(26f, 0f);
            giRt.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Image giImg = gemIcon.AddComponent<Image>();
            giImg.color = new Color(0.15f, 0.85f, 1f, 1f);

            TextMeshProUGUI gpTxt = CreateText("Gems", gemPill.transform, "520", 22, Color.white, TextAlignmentOptions.MidlineLeft, true);
            gpTxt.rectTransform.anchoredPosition = new Vector2(36f, 0f);

            GameObject gemPlus = new GameObject("PlusBtn");
            gemPlus.transform.SetParent(gemPill.transform, false);
            RectTransform gPlusRt = gemPlus.AddComponent<RectTransform>();
            gPlusRt.anchorMin = new Vector2(1f, 0.5f);
            gPlusRt.anchorMax = new Vector2(1f, 0.5f);
            gPlusRt.pivot = new Vector2(1f, 0.5f);
            gPlusRt.sizeDelta = new Vector2(44f, 44f);
            gPlusRt.anchoredPosition = new Vector2(-8f, 0f);
            Image gPlusImg = gemPlus.AddComponent<Image>();
            gPlusImg.color = new Color(0.18f, 0.78f, 0.28f, 1f);
            TextMeshProUGUI gPlusTxt = CreateText("PlusTxt", gemPlus.transform, "+", 26, Color.white, TextAlignmentOptions.Center, true);
            gPlusTxt.rectTransform.anchoredPosition = Vector2.zero;
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

        private void EnsureUIBuilt()
        {
            if (mainCanvas == null)
            {
                mainCanvas = GetComponentInChildren<Canvas>(true);
            }

            if (mainCanvas != null)
            {
                if (homeScreenPanel == null) homeScreenPanel = mainCanvas.transform.Find("ZombieSquadHomeScreenPanel")?.gameObject;
                if (levelMapPanel == null) levelMapPanel = mainCanvas.transform.Find("ZombieSquadLevelMapPanel")?.gameObject;
                if (charactersPanel == null) charactersPanel = mainCanvas.transform.Find("ZombieSquadCharactersPanel")?.gameObject;
                if (upgradePanel == null) upgradePanel = mainCanvas.transform.Find("ZombieSquadUpgradePanel")?.gameObject;
                if (weaponsPanel == null) weaponsPanel = mainCanvas.transform.Find("ZombieSquadWeaponsPanel")?.gameObject;
                if (shopPanel == null) shopPanel = mainCanvas.transform.Find("ZombieSquadShopPanel")?.gameObject;
                if (missionsPanel == null) missionsPanel = mainCanvas.transform.Find("ZombieSquadMissionsPanel")?.gameObject;
                if (dailyPanel == null) dailyPanel = mainCanvas.transform.Find("ZombieSquadDailyPanel")?.gameObject;
                if (gameplayHudPanel == null) gameplayHudPanel = mainCanvas.transform.Find("GameplayHud")?.gameObject;
                if (victoryPanel == null) victoryPanel = mainCanvas.transform.Find("VictoryPanel")?.gameObject;
                if (defeatPanel == null) defeatPanel = mainCanvas.transform.Find("DefeatPanel")?.gameObject;
            }

            bool allPanelsExist = mainCanvas != null &&
                homeScreenPanel != null &&
                levelMapPanel != null &&
                charactersPanel != null &&
                upgradePanel != null &&
                weaponsPanel != null &&
                shopPanel != null &&
                missionsPanel != null &&
                dailyPanel != null;

            if (!allPanelsExist)
            {
                if (mainCanvas != null)
                {
                    DestroyImmediate(mainCanvas.gameObject);
                    mainCanvas = null;
                }
                for (int i = transform.childCount - 1; i >= 0; i--)
                {
                    Transform child = transform.GetChild(i);
                    if (child.name.Contains("Canvas") || child.GetComponent<Canvas>() != null)
                    {
                        DestroyImmediate(child.gameObject);
                    }
                }
                BuildUIHierarchy();
            }
        }

        private void SwitchToPanel(GameObject targetPanel)
        {
            EnsureUIBuilt();

            // Re-resolve targetPanel based on currentState
            switch (currentState)
            {
                case ScreenState.HomeScreen: targetPanel = homeScreenPanel; break;
                case ScreenState.LevelMap: targetPanel = levelMapPanel; break;
                case ScreenState.Characters: targetPanel = charactersPanel; break;
                case ScreenState.Upgrade: targetPanel = upgradePanel; break;
                case ScreenState.Weapons: targetPanel = weaponsPanel; break;
                case ScreenState.Shop: targetPanel = shopPanel; break;
                case ScreenState.Missions: targetPanel = missionsPanel; break;
                case ScreenState.Daily: targetPanel = dailyPanel; break;
                case ScreenState.Gameplay: targetPanel = gameplayHudPanel; break;
                case ScreenState.Victory: targetPanel = victoryPanel; break;
                case ScreenState.Defeat: targetPanel = defeatPanel; break;
            }

            Debug.Log($"[ZombieRunnerUIManager] SwitchToPanel -> Target: {(targetPanel != null ? targetPanel.name : "NULL")}, CurrentState: {currentState}");

            // 1. Purge any duplicate canvases in scene/DontDestroyOnLoad
            Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            foreach (var c in allCanvases)
            {
                if (c != null && c != mainCanvas && c.name.Contains("ZombieRunnerCanvas"))
                {
                    Debug.Log($"[ZombieRunnerUIManager] Purging duplicate canvas: {c.gameObject.name}");
                    DestroyImmediate(c.gameObject);
                }
            }

            // 2. Loop through all children of mainCanvas and toggle active state
            if (mainCanvas != null)
            {
                for (int i = 0; i < mainCanvas.transform.childCount; i++)
                {
                    Transform child = mainCanvas.transform.GetChild(i);
                    bool isTarget = (targetPanel != null && child.gameObject == targetPanel);
                    child.gameObject.SetActive(isTarget);
                }
            }

            // 3. Explicitly set every known panel field to inactive if it's not the target
            if (homeScreenPanel != null && homeScreenPanel != targetPanel) homeScreenPanel.SetActive(false);
            if (levelMapPanel != null && levelMapPanel != targetPanel) levelMapPanel.SetActive(false);
            if (charactersPanel != null && charactersPanel != targetPanel) charactersPanel.SetActive(false);
            if (upgradePanel != null && upgradePanel != targetPanel) upgradePanel.SetActive(false);
            if (weaponsPanel != null && weaponsPanel != targetPanel) weaponsPanel.SetActive(false);
            if (shopPanel != null && shopPanel != targetPanel) shopPanel.SetActive(false);
            if (missionsPanel != null && missionsPanel != targetPanel) missionsPanel.SetActive(false);
            if (dailyPanel != null && dailyPanel != targetPanel) dailyPanel.SetActive(false);
            if (splashPanel != null && splashPanel != targetPanel) splashPanel.SetActive(false);
            if (levelSelectPanel != null && levelSelectPanel != targetPanel) levelSelectPanel.SetActive(false);
            if (gameplayHudPanel != null && gameplayHudPanel != targetPanel) gameplayHudPanel.SetActive(false);
            if (victoryPanel != null && victoryPanel != targetPanel) victoryPanel.SetActive(false);
            if (defeatPanel != null && defeatPanel != targetPanel) defeatPanel.SetActive(false);

            if (targetPanel != null)
            {
                targetPanel.SetActive(true);
                targetPanel.transform.SetAsLastSibling();
            }

            // 4. If switching away from HomeScreen, deactivate ANY other HomeScreen object in the scene
            if (currentState != ScreenState.HomeScreen)
            {
                GameObject[] allGos = FindObjectsByType<GameObject>(FindObjectsInactive.Include);
                foreach (var go in allGos)
                {
                    if (go != null && go != targetPanel && (go.name == "ZombieSquadHomeScreenPanel" || go.name == "HomeScreen"))
                    {
                        go.SetActive(false);
                    }
                }
            }

            // Reset finished flag, lock player and ensure normal time when in menu screens
            if (currentState != ScreenState.Gameplay && currentState != ScreenState.Victory && currentState != ScreenState.Defeat)
            {
                isGameActive = false;
                isFinishedHandled = false;
                Time.timeScale = 1f;
                LockPlayer(true);
            }

            if (starRigRoot != null) starRigRoot.SetActive(currentState == ScreenState.Victory);
            if (starCamera != null) starCamera.enabled = (currentState == ScreenState.Victory);
        }

        public void ShowZombieSquadHomeScreen()
        {
            currentState = ScreenState.HomeScreen;
            isGameActive = false;
            LockPlayer(true);
            SwitchToPanel(homeScreenPanel);
        }

        public void ShowZombieSquadLevelMap()
        {
            currentState = ScreenState.LevelMap;
            isGameActive = false;
            LockPlayer(true);
            SwitchToPanel(levelMapPanel);
            UpdateLevelMapVisuals();
        }

        public void ShowZombieSquadCharactersScreen()
        {
            currentState = ScreenState.Characters;
            isGameActive = false;
            LockPlayer(true);
            SwitchToPanel(charactersPanel);
            selectedCharacterKey = PlayerPrefs.GetString("ZombieSquad_SelectedHero", "Soldier");
            UpdateCharactersScreenVisuals();
        }

        public void ShowZombieSquadUpgradeScreen()
        {
            currentState = ScreenState.Upgrade;
            isGameActive = false;
            LockPlayer(true);
            SwitchToPanel(upgradePanel);
            UpdateUpgradeScreenVisuals();
        }

        public void ShowZombieSquadWeaponsScreen()
        {
            currentState = ScreenState.Weapons;
            isGameActive = false;
            LockPlayer(true);
            SwitchToPanel(weaponsPanel);
            UpdateWeaponsScreenVisuals();
        }

        public void ShowZombieSquadShopScreen()
        {
            currentState = ScreenState.Shop;
            isGameActive = false;
            LockPlayer(true);
            SwitchToPanel(shopPanel);
            UpdateShopScreenVisuals();
        }

        public void ShowZombieSquadMissionsScreen()
        {
            currentState = ScreenState.Missions;
            isGameActive = false;
            LockPlayer(true);
            SwitchToPanel(missionsPanel);
            UpdateMissionsScreenVisuals();
        }

        public void ShowZombieSquadDailyScreen()
        {
            currentState = ScreenState.Daily;
            isGameActive = false;
            LockPlayer(true);
            SwitchToPanel(dailyPanel);
            UpdateDailyScreenVisuals();
        }

        public void ShowSplashScreen()
        {
            currentState = ScreenState.SplashScreen;
            isGameActive = false;
            LockPlayer(true);

            splashPanel.SetActive(true);
            if (upgradePanel != null) upgradePanel.SetActive(false);
            if (weaponsPanel != null) weaponsPanel.SetActive(false);
            if (shopPanel != null) shopPanel.SetActive(false);
            if (missionsPanel != null) missionsPanel.SetActive(false);
            if (dailyPanel != null) dailyPanel.SetActive(false);
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

            if (splashPanel != null) splashPanel.SetActive(false);
            if (levelSelectPanel != null) levelSelectPanel.SetActive(true);
            if (charactersPanel != null) charactersPanel.SetActive(false);
            if (upgradePanel != null) upgradePanel.SetActive(false);
            if (weaponsPanel != null) weaponsPanel.SetActive(false);
            if (shopPanel != null) shopPanel.SetActive(false);
            if (missionsPanel != null) missionsPanel.SetActive(false);
            if (dailyPanel != null) dailyPanel.SetActive(false);
            if (homeScreenPanel != null) homeScreenPanel.SetActive(false);
            if (levelMapPanel != null) levelMapPanel.SetActive(false);
            if (gameplayHudPanel != null) gameplayHudPanel.SetActive(false);
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (defeatPanel != null) defeatPanel.SetActive(false);
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
            isFinishedHandled = false;
            isGameActive = true;
            currentState = ScreenState.Gameplay;

            // 1. Ensure Reference Level 1 Road & Environment is built
            MobControl.Level.NewRoadBuilder.EnsureNewRoad();

            // 2. Setup Camera and Atmosphere for Level 1 Road
            MobControl.Level.NewRoadBuilder.SetupCameraAndAtmosphere();

            // 3. Update HUD level title
            if (hudLevelText != null)
            {
                hudLevelText.text = $"LEVEL {levelNumber}";
            }

            // 4. Switch to Gameplay HUD
            SwitchToPanel(gameplayHudPanel);

            Debug.Log($"[ZombieSquad] Level {levelNumber} started! Displaying Level 1 Reference Road.");
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
            SwitchToPanel(victoryPanel);

            Ensure3DStarRig();
            if (starRigRoot != null) starRigRoot.SetActive(true);
            if (starCamera != null) starCamera.enabled = true;

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
            SwitchToPanel(defeatPanel);

            if (defeatTitleText != null) defeatTitleText.text = title;
            if (defeatSubText != null) defeatSubText.text = subtitle;
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

            startAtLevelMap = true; // Open Level Map so user sees their updated progress!
            startAtHomeScreen = false;
            startAtLevelSelect = false;
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
