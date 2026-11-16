using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using MobControl.Combat;
using MobControl.Audio;

namespace MobControl.UI
{
    /// <summary>
    /// ZombieRunnerUIManager
    /// Automatically manages:
    /// 1. Splash Screen with "Zombie Rage" title, animated 🧟 emoji, and smooth Loading Bar.
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
        public bool IsModalOpen => currentState != ScreenState.Gameplay || isGamePaused;
        public ScreenState CurrentState => currentState;

        [Header("Level Track Settings")]
        [SerializeField] private float levelFinishZ = 120.0f; // End of the zombie street
        [SerializeField] private int activeLevel = 1;
        [SerializeField] private int totalLevels = 6;
        public int ActiveLevel => activeLevel;

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

        [Header("Home Screen (Responsive)")]
        [SerializeField] private GameObject homeScreenResponsiveContainer;
        private RectTransform modularPlayBtnRect;
        private RectTransform modularCharactersBtnRect;
        private RectTransform modularUpgradeBtnRect;
        private RectTransform modularWeaponsBtnRect;
        private RectTransform modularShopBtnRect;
        private RectTransform modularMissionsBtnRect;
        private RectTransform modularDailyBtnRect;
        [SerializeField] private GameObject levelMapPanel;
        [SerializeField] private RectTransform levelMapArtContainerRect;
        [SerializeField] private RectTransform levelMapBackBtnRect;
        [SerializeField] private RectTransform[] levelMapNodeRects;
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

        [Header("Swipe to Start Prompt")]
        private GameObject swipeToStartPromptObj;
        private CanvasGroup swipeToStartCanvasGroup;
        private RectTransform swipePromptRect;
        private TextMeshProUGUI swipePromptTitleText;
        private TextMeshProUGUI swipePromptSubText;
        private TextMeshProUGUI swipeLeftChevrons;
        private TextMeshProUGUI swipeRightChevrons;
        private RectTransform swipeIndicatorDotRect;
        private bool isWaitingForFirstSwipe = false;
        private Coroutine swipePromptAnimCoroutine;
        private Coroutine swipeFadeOutCoroutine;

        public bool IsWaitingForFirstSwipe => isWaitingForFirstSwipe;

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
            public int priceCoins;
            public int priceDiamonds;
            public bool isDefaultUnlocked;
        }

        private readonly Dictionary<string, HeroData> heroes = new Dictionary<string, HeroData>
        {
            { "Soldier", new HeroData { key = "Soldier", name = "SOLDIER", role = "Assault Specialist", desc = "Versatile combat trooper with high resilience and steady assault rifle firepower.", damage = 0.65f, health = 0.70f, fireRate = 0.60f, speed = 0.65f, damageVal = 25, healthVal = 100, fireRateVal = 60, speedVal = 65, accentColor = new Color(0.2f, 0.75f, 1f, 1f), priceCoins = 0, priceDiamonds = 0, isDefaultUnlocked = true } },
            { "Ranger", new HeroData { key = "Ranger", name = "RANGER", role = "Rapid Scout", desc = "High-mobility vanguard equipped with rapid dual SMGs and sprint bonuses.", damage = 0.50f, health = 0.55f, fireRate = 0.90f, speed = 0.85f, damageVal = 18, healthVal = 85, fireRateVal = 90, speedVal = 85, accentColor = new Color(0.95f, 0.30f, 0.30f, 1f), priceCoins = 2500, priceDiamonds = 0, isDefaultUnlocked = false } },
            { "Heavy", new HeroData { key = "Heavy", name = "HEAVY", role = "Tank Juggernaut", desc = "Armor-plated frontliner armed with heavy explosive rounds and high survivability.", damage = 0.85f, health = 0.95f, fireRate = 0.40f, speed = 0.45f, damageVal = 45, healthVal = 160, fireRateVal = 40, speedVal = 45, accentColor = new Color(0.95f, 0.75f, 0.15f, 1f), priceCoins = 5000, priceDiamonds = 0, isDefaultUnlocked = false } },
            { "Sniper", new HeroData { key = "Sniper", name = "SNIPER", role = "Long-Range Marksman", desc = "Deadly sharpshooter delivering lethal critical hits and piercing headshots.", damage = 0.95f, health = 0.45f, fireRate = 0.35f, speed = 0.60f, damageVal = 65, healthVal = 75, fireRateVal = 35, speedVal = 60, accentColor = new Color(0.70f, 0.35f, 0.95f, 1f), priceCoins = 0, priceDiamonds = 50, isDefaultUnlocked = false } },
            { "Medic", new HeroData { key = "Medic", name = "MEDIC", role = "Field Support", desc = "Combat doctor providing periodic health regeneration and defensive shields.", damage = 0.50f, health = 0.80f, fireRate = 0.55f, speed = 0.75f, damageVal = 20, healthVal = 110, fireRateVal = 55, speedVal = 75, accentColor = new Color(0.95f, 0.40f, 0.70f, 1f), priceCoins = 0, priceDiamonds = 100, isDefaultUnlocked = false } }
        };

        private string selectedCharacterKey = "Soldier";
        private Dictionary<string, TextMeshProUGUI> charCardPriceTexts = new Dictionary<string, TextMeshProUGUI>();
        private Dictionary<string, Image> charCardPriceBgs = new Dictionary<string, Image>();
        private Dictionary<string, GameObject> charCardSelectionBorders = new Dictionary<string, GameObject>();
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
        public static bool startAtShop = false;
        public static bool startAtDefeat = false;
        public static bool startAtVictory = false;
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
        private GameObject defeatSubContainer;

        // Level Select Elements
        private TextMeshProUGUI coinsText;
        private Transform levelGridContainer;
        private List<GameObject> levelCardObjects = new List<GameObject>();

        // Sub-Screens UI References
        private Image[] upgradeStatFills = new Image[4];
        private TextMeshProUGUI[] upgradeStatLevelTexts = new TextMeshProUGUI[4];
        private TextMeshProUGUI[] upgradeStatCostTexts = new TextMeshProUGUI[4];
        private TextMeshProUGUI weaponTitleText;
        private TextMeshProUGUI weaponLevelText;
        private Image weaponChamberImg;
        private RectTransform weaponCardSelectionHighlightRt;
        private Image[] weaponStatFills = new Image[4];
        private TextMeshProUGUI[] weaponStatValTexts = new TextMeshProUGUI[4];
        private TextMeshProUGUI[] weaponStatCostTexts = new TextMeshProUGUI[4];
        private string activeShopTab = "Coins";
        private GameObject shopCoinsContainer;
        private GameObject shopDiamondsContainer;
        private GameObject shopSpecialContainer;
        private GameObject shopSkinsContainer;
        private GameObject shopTabHighlightObj;
        private RectTransform shopTabHighlightRt;
        private Image shopTabHighlightImg;
        private readonly List<Button> shopTabButtonsList = new List<Button>();
        private readonly List<Image> shopTabImagesList = new List<Image>();
        private readonly List<TextMeshProUGUI> shopTabTextList = new List<TextMeshProUGUI>();
        private readonly List<Outline> shopTabOutlinesList = new List<Outline>();
        private readonly List<string> shopTabKeysList = new List<string>();
        private GameObject shopCheckoutModalObj;
        private RectTransform shopCheckoutDialogRt;
        private TextMeshProUGUI shopCheckoutItemText;
        private TextMeshProUGUI shopCheckoutPriceText;
        private TextMeshProUGUI shopCheckoutConfirmBtnText;
        private System.Action onShopCheckoutConfirmAction;
        private Image[] missionProgressFills = new Image[3];
        private TextMeshProUGUI[] missionProgressTexts = new TextMeshProUGUI[3];
        private TextMeshProUGUI[] missionClaimBtnTexts = new TextMeshProUGUI[3];
        private TextMeshProUGUI[] dailyDayStatusTexts = new TextMeshProUGUI[7];
        private Image dailyClaimBtnBg;
        private Button dailyClaimBtn;
        private TextMeshProUGUI dailyClaimBtnText;

        // Gameplay HUD Elements (Image 1 & 2 Match)
        private TextMeshProUGUI hudLevelText;
        private Image hudProgressBarFill;
        private RectTransform hudProgressBarFillRt;
        private float hudProgressTrackWidth = 460f;
        private TextMeshProUGUI hudProgressBarLabel;
        private RectTransform hudBossBadgeRect;
        private Image hudBossBadgeImage;
        private GameObject multiplierPopObj;
        private RectTransform multiplierPopRect;
        private TextMeshProUGUI multiplierPopText;
        private bool isBossPhase = false;
        private Coroutine bossPulseCoroutine;
        private TextMeshProUGUI hudHealthText;

        // Gun Upgrade 15-Second Boost HUD Timer Badge
        private GameObject hudGunUpgradeBadgeObj;
        private RectTransform hudGunUpgradeBadgeRt;
        private TextMeshProUGUI hudGunUpgradeText;

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
        private RectTransform defeatStarsRect;
        private RectTransform victoryNextRect;
        private RectTransform victoryReplayRect;
        private RectTransform victoryMenuRect;

        private const string PREF_UNLOCKED_LEVEL = "ZombieRunner_UnlockedLevel";
        private const string PREF_COINS = "ZombieRunner_Coins";
        /// <summary>Starting wallet on a fresh install.</summary>
        public const int DEFAULT_COINS = 100;
        public const int DEFAULT_DIAMONDS = 10;
        private const string PREF_STARS_PREFIX = "ZombieRunner_Stars_Lvl_";

        public static void SafeDestroy(Object obj)
        {
            if (obj == null) return;
            if (Application.isEditor) Object.DestroyImmediate(obj);
            else Object.Destroy(obj);
        }

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
                        SafeDestroy(allManagers[i].gameObject);
                    }
                }
            }

            if (Instance == null)
            {
                GameObject uiRoot = new GameObject("[ZombieRage_UI_Manager]");
                Instance = uiRoot.AddComponent<ZombieRunnerUIManager>();
                if (Application.isPlaying) DontDestroyOnLoad(uiRoot);
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
                    SafeDestroy(m.gameObject);
                }
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Application.runInBackground = true;

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

            if (startAtDefeat)
            {
                startAtDefeat = false;
                startAtHomeScreen = false;
                startAtLevelMap = false;
                startAtLevelSelect = false;
                TriggerDefeatWithReason("CRASHED!", "You crashed into a wrecked burned car.");
            }
            else if (startAtVictory)
            {
                startAtVictory = false;
                startAtHomeScreen = false;
                startAtLevelMap = false;
                startAtLevelSelect = false;
                TriggerVictory(3, 100);
            }
            else if (startAtLevelMap)
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
            else if (startAtShop)
            {
                startAtShop = false;
                startAtHomeScreen = false;
                startAtLevelSelect = false;
                ShowZombieSquadShopScreen();
            }
            else
            {
                startAtHomeScreen = false;
                startAtLevelSelect = false;
                LaunchFallbackHome();
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
            if (mainCanvas != null)
            {
                HookAllButtonsWithSound(mainCanvas.gameObject);
            }
            if (!MobControl.Level.ArmyRunLevel.Enabled)
            {
                MobControl.Level.NewRoadBuilder.EnsureNewRoad();
            }

            if (currentState == ScreenState.Gameplay)
            {
                // Already started mission (e.g. from Awake or QuickStart), do not override with HomeScreen!
            }
            else if (currentState == ScreenState.Defeat)
            {
                SwitchToPanel(defeatPanel);
            }
            else if (currentState == ScreenState.Victory)
            {
                SwitchToPanel(victoryPanel);
            }
            else if (currentState == ScreenState.LevelMap)
            {
                ShowZombieSquadLevelMap();
            }
            else if (currentState == ScreenState.Shop)
            {
                ShowZombieSquadShopScreen();
            }
            else if (currentState == ScreenState.Characters)
            {
                ShowZombieSquadCharactersScreen();
            }
            else if (currentState == ScreenState.Weapons)
            {
                ShowZombieSquadWeaponsScreen();
            }
            else if (currentState == ScreenState.Upgrade)
            {
                ShowZombieSquadUpgradeScreen();
            }
            else if (autoStartMissionOnLoad)
            {
                autoStartMissionOnLoad = false;
                StartMission(levelToAutoStart);
            }
            else
            {
                LaunchFallbackHome();
            }
        }

        private void Update()
        {
            TickDailyCountdown();
            HandlePauseInput();
            if (mainCanvas != null)
            {
                CanvasScaler dynScaler = mainCanvas.GetComponent<CanvasScaler>();
                if (dynScaler != null)
                {
                    float curAsp = (float)Screen.width / Mathf.Max(1, Screen.height);
                    float targetMatch = (curAsp > (1080f / 1920f)) ? 1f : 0f;
                    if (!Mathf.Approximately(dynScaler.matchWidthOrHeight, targetMatch))
                    {
                        dynScaler.matchWidthOrHeight = targetMatch;
                    }
                }
            }
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

            // Manage player control: unlock in gameplay, lock in menus
            if (playerRunner != null && playerRunner.isActiveAndEnabled)
            {
                if (isGameActive && currentState == ScreenState.Gameplay)
                {
                    playerRunner.SetControllable(true);
                }
                else if (!isGameActive)
                {
                    playerRunner.SetControllable(false);
                }
            }

            // Waiting for initial swipe to begin the level
            if (currentState == ScreenState.Gameplay && isWaitingForFirstSwipe)
            {
                if (HasAnySwipeStartInput())
                {
                    StartGameplayFromWaiting();
                }
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
            // Seamless Keyboard Navigation between screens for testing & play
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                if (currentState == ScreenState.HomeScreen)
                {
                    Debug.Log("[ZombieRunnerUIManager] Space/Return on HomeScreen -> Opening Level Map Screen!");
                    ShowZombieSquadLevelMap();
                    return;
                }
                else if (currentState == ScreenState.LevelMap)
                {
                    Debug.Log("[ZombieRunnerUIManager] Space/Return on LevelMap -> Launching Level 1 Road Battle!");
                    StartMission(1);
                    return;
                }
            }
            for (int lvl = 1; lvl <= 8; lvl++)
            {
                if ((Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha0 + lvl)) || Input.GetKeyDown((KeyCode)((int)KeyCode.Keypad0 + lvl))) && currentState == ScreenState.LevelMap)
                {
                    Debug.Log($"[ZombieRunnerUIManager] Key '{lvl}' pressed on LevelMap -> Launching Level {lvl} Road Battle!");
                    StartMission(lvl);
                    return;
                }
            }
            if (Input.GetKeyDown(KeyCode.M) && currentState != ScreenState.Gameplay)
            {
                ShowZombieSquadLevelMap();
                return;
            }
            if (Input.GetKeyDown(KeyCode.H))
            {
                LaunchFallbackHome();
                return;
            }
            if (Input.GetKeyDown(KeyCode.B))
            {
                ShowZombieSquadShopScreen();
                return;
            }
            if ((Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace)) && currentState == ScreenState.LevelMap)
            {
                ShowZombieSquadHomeScreen();
                return;
            }

            if (currentState == ScreenState.Shop && shopPanel != null && shopPanel.activeInHierarchy)
            {
                if (shopCoinsContainer == null || (shopCoinsContainer.transform.Find("Btn_Buy_1000_Coins") == null && shopCoinsContainer.transform.Find("Card_1000") == null))
                {
                    if (mainCanvas != null)
                    {
                        BuildZombieSquadShopScreenPanel(mainCanvas.transform);
                        SwitchToPanel(shopPanel);
                        UpdateShopScreenVisuals();
                    }
                }
            }

            // High-reliability touch/mouse fallback for HomeScreen, LevelMap, Characters, Defeat & Victory buttons
            if (currentState == ScreenState.HomeScreen && homeScreenPanel != null && homeScreenPanel.activeInHierarchy && !isSceneReloading)
            {
                bool hasPointerInput = Input.GetMouseButtonDown(0);
                Vector2 pointerPos = Input.mousePosition;
                if (!hasPointerInput && Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
                {
                    hasPointerInput = true;
                    pointerPos = Input.GetTouch(0).position;
                }

                if (hasPointerInput)
                {
                    // Check PLAY button FIRST as top priority primary CTA!
                    if (homeScreenPlayBtnRect != null && RectTransformUtility.RectangleContainsScreenPoint(homeScreenPlayBtnRect, pointerPos, null))
                    {
                        Debug.Log("[ZombieRunnerUIManager] HomeScreen PLAY clicked -> Opening Level Map");
                        ShowZombieSquadLevelMap();
                        return;
                    }
                    else if (homeScreenCharactersBtnRect != null && RectTransformUtility.RectangleContainsScreenPoint(homeScreenCharactersBtnRect, pointerPos, null))
                    {
                        Debug.Log("[ZombieRunnerUIManager] HomeScreen CHARACTERS clicked -> Instant Open!");
                        ShowZombieSquadCharactersScreen();
                        return;
                    }
                    else if (homeScreenUpgradeBtnRect != null && RectTransformUtility.RectangleContainsScreenPoint(homeScreenUpgradeBtnRect, pointerPos, null))
                    {
                        Debug.Log("[ZombieRunnerUIManager] HomeScreen UPGRADE clicked -> Instant Open!");
                        ShowZombieSquadUpgradeScreen();
                        return;
                    }
                    else if (homeScreenWeaponsBtnRect != null && RectTransformUtility.RectangleContainsScreenPoint(homeScreenWeaponsBtnRect, pointerPos, null))
                    {
                        Debug.Log("[ZombieRunnerUIManager] HomeScreen WEAPONS clicked -> Instant Open!");
                        ShowZombieSquadWeaponsScreen();
                        return;
                    }
                    else if (homeScreenShopBtnRect != null && RectTransformUtility.RectangleContainsScreenPoint(homeScreenShopBtnRect, pointerPos, null))
                    {
                        Debug.Log("[ZombieRunnerUIManager] HomeScreen SHOP clicked -> Instant Open!");
                        ShowZombieSquadShopScreen();
                        return;
                    }
                    else if (homeScreenMissionsBtnRect != null && RectTransformUtility.RectangleContainsScreenPoint(homeScreenMissionsBtnRect, pointerPos, null))
                    {
                        Debug.Log("[ZombieRunnerUIManager] HomeScreen MISSIONS clicked -> Opening Level Map Screen!");
                        ShowZombieSquadLevelMap();
                        return;
                    }
                    else if (homeScreenDailyBtnRect != null && RectTransformUtility.RectangleContainsScreenPoint(homeScreenDailyBtnRect, pointerPos, null))
                    {
                        Debug.Log("[ZombieRunnerUIManager] HomeScreen DAILY clicked -> Instant Open!");
                        ShowZombieSquadDailyScreen();
                        return;
                    }
                }
            }
            else if (currentState == ScreenState.Characters && charactersPanel != null && charactersPanel.activeInHierarchy && !isSceneReloading)
            {
                bool hasPointerInput = Input.GetMouseButtonDown(0);
                Vector2 pointerPos = Input.mousePosition;
                if (!hasPointerInput && Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
                {
                    hasPointerInput = true;
                    pointerPos = Input.GetTouch(0).position;
                }

                if (hasPointerInput)
                {
                    if (charactersBackBtnRect != null && RectTransformUtility.RectangleContainsScreenPoint(charactersBackBtnRect, pointerPos, null))
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
                bool hasPointerInput = Input.GetMouseButtonDown(0);
                Vector2 pointerPos = Input.mousePosition;
                if (!hasPointerInput && Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
                {
                    hasPointerInput = true;
                    pointerPos = Input.GetTouch(0).position;
                }

                if (hasPointerInput)
                {
                    if (shopCheckoutModalObj == null || !shopCheckoutModalObj.activeInHierarchy)
                    {
                        if (shopBackBtnRect != null && RectTransformUtility.RectangleContainsScreenPoint(shopBackBtnRect, pointerPos, null))
                        {
                            Debug.Log("[ZombieRunnerUIManager] Shop Back clicked via direct screen fallback!");
                            ShowZombieSquadHomeScreen();
                            return;
                        }
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

                    // 1. Back button direct screen rect check
                    if (levelMapBackBtnRect != null && RectTransformUtility.RectangleContainsScreenPoint(levelMapBackBtnRect, mousePos, null))
                    {
                        Debug.Log("[ZombieRunnerUIManager] LevelMap Back clicked via direct screen fallback!");
                        ShowZombieSquadHomeScreen();
                        return;
                    }

                    // 2. Direct screen click fallback for mapNodeItems (Levels 1 to 8)
                    for (int i = 0; i < mapNodeItems.Count; i++)
                    {
                        var item = mapNodeItems[i];
                        if (item != null && item.rootObj != null)
                        {
                            RectTransform rt = item.rootObj.GetComponent<RectTransform>();
                            if (rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, mousePos, null))
                            {
                                Debug.Log($"[ZombieRunnerUIManager] LevelMap Node {item.levelNumber} clicked via mapNodeItems fallback!");
                                OnMapNodeClicked(item.levelNumber, item.rootObj.transform);
                                return;
                            }
                        }
                    }

                    // 3. Fallback: Serialized levelMapNodeRects
                    if (levelMapNodeRects != null)
                    {
                        for (int i = 0; i < levelMapNodeRects.Length; i++)
                        {
                            RectTransform rt = levelMapNodeRects[i];
                            if (rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, mousePos, null))
                            {
                                Debug.Log($"[ZombieRunnerUIManager] LevelMap Node {i + 1} clicked via levelMapNodeRects fallback!");
                                OnMapNodeClicked(i + 1, rt.transform);
                                return;
                            }
                        }
                    }

                    // 4. Ultimate geometric fallback: Normalized screen coordinates on ArtContainer!
                    // Works 100% reliably even if Domain Reload cleared objects or RectTransform caches.
                    RectTransform mapRt = (levelMapArtContainerRect != null) ? levelMapArtContainerRect : levelMapPanel.GetComponent<RectTransform>();
                    if (mapRt != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(mapRt, mousePos, null, out Vector2 localPt))
                    {
                        float normX = (localPt.x - mapRt.rect.xMin) / Mathf.Max(1f, mapRt.rect.width);
                        float normY = (localPt.y - mapRt.rect.yMin) / Mathf.Max(1f, mapRt.rect.height);
                        Vector2 clickNorm = new Vector2(normX, normY);

                        // Back Button area (top left corner)
                        if (normX >= 0.02f && normX <= 0.22f && normY >= 0.86f && normY <= 0.99f)
                        {
                            Debug.Log("[ZombieRunnerUIManager] LevelMap Back clicked via geometric fallback!");
                            ShowZombieSquadHomeScreen();
                            return;
                        }

                        // Check each Level Node position
                        Vector2[] positions = GetMapNodePositions();
                        for (int i = 0; i < positions.Length; i++)
                        {
                            float dist = Vector2.Distance(clickNorm, positions[i]);
                            float hitRadius = (i == 7) ? 0.12f : 0.085f;
                            if (dist <= hitRadius)
                            {
                                Debug.Log($"[ZombieRunnerUIManager] LevelMap Node {i + 1} clicked via geometric fallback (dist={dist:F3})!");
                                Transform tr = (levelMapNodeRects != null && i < levelMapNodeRects.Length && levelMapNodeRects[i] != null) ? levelMapNodeRects[i].transform : null;
                                OnMapNodeClicked(i + 1, tr);
                                return;
                            }
                        }
                    }
                }
            }
            else if (currentState == ScreenState.Defeat && defeatPanel != null && defeatPanel.activeInHierarchy && !isSceneReloading)
            {
                if (defeatStarsRect != null)
                {
                    float wobble = Mathf.Sin(Time.unscaledTime * 4.5f) * 12f;
                    defeatStarsRect.localRotation = Quaternion.Euler(0, 0, wobble);
                    float pulse = 1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.08f;
                    defeatStarsRect.localScale = new Vector3(pulse, pulse, 1f);
                }

                if (Input.GetMouseButtonDown(0))
                {
                    Vector2 mousePos = Input.mousePosition;
                    if (defeatRetryRect != null && RectTransformUtility.RectangleContainsScreenPoint(defeatRetryRect, mousePos, null))
                    {
                        Debug.Log("[ZombieRunnerUIManager] Retry button clicked via direct screen click fallback!");
                        ZombieSoundManager.Instance?.PlayUIClick();
                        OnReplayClicked();
                        return;
                    }
                    else if (defeatMenuRect != null && RectTransformUtility.RectangleContainsScreenPoint(defeatMenuRect, mousePos, null))
                    {
                        Debug.Log("[ZombieRunnerUIManager] Menu button clicked via direct screen click fallback!");
                        ZombieSoundManager.Instance?.PlayUIClick();
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
                    SafeDestroy(c.gameObject);
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
            float screenAspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            scaler.matchWidthOrHeight = (screenAspect > (1080f / 1920f)) ? 1f : 0f;
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
            BuildSettingsPopupPanel(canvasObj.transform);
            BuildSplashPanel(canvasObj.transform);
            if (splashPanel != null) splashPanel.SetActive(false); // splash disabled — game opens straight to Home
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

        private static TMP_FontAsset s_globalChildishFont;

        // Helper: Create Text (raycastTarget defaults to false so it never blocks button clicks!)
        private TextMeshProUGUI CreateText(string name, Transform parent, string text, float fontSize, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center, bool bold = false)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.enabled = false; // Prevent GenerateTextMesh before material is ready
            if (s_globalChildishFont == null)
            {
                s_globalChildishFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Fredoka-Bold SDF")
                                     ?? Resources.Load<TMP_FontAsset>("Fredoka-Bold SDF");
#if UNITY_EDITOR
                if (s_globalChildishFont == null)
                {
                    s_globalChildishFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Fredoka-Bold SDF.asset");
                }
#endif
            }
            if (s_globalChildishFont != null) tmp.font = s_globalChildishFont;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = align;
            tmp.verticalAlignment = VerticalAlignmentOptions.Middle;
            tmp.raycastTarget = false; // Never intercept button clicks
            if (bold) tmp.fontStyle = FontStyles.Bold;
            tmp.enabled = true; // Now safe to generate mesh

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

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

                Texture2D resTex = Resources.Load<Texture2D>("UI/Star_Badge_Bg");
                if (resTex != null)
                {
                    s_starBadgeBgSprite = Sprite.Create(resTex, new Rect(0, 0, resTex.width, resTex.height), new Vector2(0.5f, 0.5f));
                    return s_starBadgeBgSprite;
                }

#if UNITY_EDITOR && !UNITY_ANDROID
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
#endif
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

                Texture2D resTex = Resources.Load<Texture2D>(resName);
                if (resTex != null)
                {
                    Sprite s = Sprite.Create(resTex, new Rect(0, 0, resTex.width, resTex.height), new Vector2(0.5f, 0.5f));
                    if (filled) s_goldStarSprite = s;
                    else s_dimStarSprite = s;
                    return s;
                }

#if UNITY_EDITOR && !UNITY_ANDROID
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
#endif
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
                SafeDestroy(homeHeroRigRoot);
                homeHeroRigRoot = null;
            }

            GameObject orphan = GameObject.Find("[HomeScreen_3D_Hero_Rig]");
            if (orphan != null) SafeDestroy(orphan);

            if (homeHeroRenderTexture != null)
            {
                homeHeroRenderTexture.Release();
                SafeDestroy(homeHeroRenderTexture);
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
                Sprite sHd = Resources.Load<Sprite>("UI/ZombieSquad_HomeScreen_HD");
                if (sHd != null) return sHd;

                Sprite s = Resources.Load<Sprite>("UI/ZombieSquad_HomeScreen");
                if (s != null) return s;

                Texture2D resTexHd = Resources.Load<Texture2D>("UI/ZombieSquad_HomeScreen_HD");
                if (resTexHd != null)
                {
                    return Sprite.Create(resTexHd, new Rect(0, 0, resTexHd.width, resTexHd.height), new Vector2(0.5f, 0.5f));
                }

                Texture2D resTex = Resources.Load<Texture2D>("UI/ZombieSquad_HomeScreen");
                if (resTex != null)
                {
                    return Sprite.Create(resTex, new Rect(0, 0, resTex.width, resTex.height), new Vector2(0.5f, 0.5f));
                }

                string[] localPaths = new string[]
                {
                    System.IO.Path.Combine(Application.dataPath, "Resources", "UI", "ZombieSquad_HomeScreen_HD.png"),
                    System.IO.Path.Combine(Application.dataPath, "Resources", "UI", "ZombieSquad_HomeScreen.png"),
                    System.IO.Path.Combine(Application.dataPath, "Resources", "UI", "ZombieSquad_HomeScreen.jpg")
                };

                foreach (string localPath in localPaths)
                {
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

        private Button CreateSpriteButton(string name, Transform parent, Sprite sprite, Vector2 anchor, Vector2 anchoredPos, Vector2 size, System.Action onClick)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Image img = btnObj.AddComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.raycastTarget = true;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
            cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            cb.selectedColor = Color.white;
            cb.fadeDuration = 0.05f;
            btn.colors = cb;

            if (onClick != null)
            {
                btn.onClick.AddListener(() =>
                {
                    StartCoroutine(PunchScale(btnObj.transform, 0.12f, onClick));
                });
            }
            return btn;
        }

        private Button Create3DCardMenuButton(string name, Transform parent, string iconResPath, string labelText, Vector2 anchor, Vector2 anchoredPos, Vector2 size, System.Action onClick, int badgeCount = 0)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            // 1. 3D Beveled Card Background (512x512 9-Sliced for ultra-sharp borders on any tablet or 4K screen)
            Image bgImg = btnObj.AddComponent<Image>();
            Sprite cardSprite = Resources.Load<Sprite>("UI/HomeScreen/UI_Card_3D_Bevel");
            if (cardSprite != null)
            {
                bgImg.sprite = cardSprite;
                bgImg.type = Image.Type.Sliced;
            }
            else
            {
                bgImg.color = new Color(0.06f, 0.14f, 0.26f, 1f);
            }
            bgImg.raycastTarget = true;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bgImg;

            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            cb.selectedColor = Color.white;
            cb.fadeDuration = 0.05f;
            btn.colors = cb;

            // 2. High-DPI 3D Icon (Centered in top 65% of the card)
            GameObject iconObj = new GameObject("Icon_3D");
            iconObj.transform.SetParent(btnObj.transform, false);
            RectTransform iconRt = iconObj.AddComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = new Vector2(0f, 16f);
            iconRt.sizeDelta = new Vector2(size.x * 0.65f, size.x * 0.65f);

            Image iconImg = iconObj.AddComponent<Image>();
            Sprite iconSp = Resources.Load<Sprite>(iconResPath);
            if (iconSp != null)
            {
                iconImg.sprite = iconSp;
                iconImg.preserveAspect = true;
            }
            iconImg.raycastTarget = false;

            // 3. TextMeshProUGUI SDF Label (Mathematically crisp, never pixelates)
            GameObject textObj = new GameObject("Label_TMP");
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform textRt = textObj.AddComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0f, 0f);
            textRt.anchorMax = new Vector2(1f, 0f);
            textRt.pivot = new Vector2(0.5f, 0f);
            textRt.anchoredPosition = new Vector2(0f, 10f);
            textRt.sizeDelta = new Vector2(0f, 36f);

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.enabled = false; // Prevent GenerateTextMesh before material is ready
            tmp.text = labelText;
            tmp.fontStyle = FontStyles.Bold;
            tmp.fontSize = 22f;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 14f;
            tmp.fontSizeMax = 24f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            tmp.enabled = true; // Now safe to generate mesh

            var outline = textObj.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = new Color32(6, 16, 36, 255);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            // 4. Optional Notification Badge (e.g. for Missions)
            if (badgeCount > 0)
            {
                GameObject badgeObj = new GameObject("Badge_Notif");
                badgeObj.transform.SetParent(btnObj.transform, false);
                RectTransform bRt = badgeObj.AddComponent<RectTransform>();
                bRt.anchorMin = new Vector2(1f, 1f);
                bRt.anchorMax = new Vector2(1f, 1f);
                bRt.pivot = new Vector2(0.5f, 0.5f);
                bRt.anchoredPosition = new Vector2(-12f, -12f);
                bRt.sizeDelta = new Vector2(36f, 36f);

                Image bImg = badgeObj.AddComponent<Image>();
                bImg.sprite = GetCircleBadgeSprite();
                bImg.color = new Color(0.95f, 0.15f, 0.15f, 1f);
                bImg.raycastTarget = false;

                GameObject bTxtObj = new GameObject("Badge_Text");
                bTxtObj.transform.SetParent(badgeObj.transform, false);
                RectTransform btRt = bTxtObj.AddComponent<RectTransform>();
                btRt.anchorMin = Vector2.zero;
                btRt.anchorMax = Vector2.one;
                btRt.offsetMin = Vector2.zero;
                btRt.offsetMax = Vector2.zero;

                TextMeshProUGUI bTmp = bTxtObj.AddComponent<TextMeshProUGUI>();
                bTmp.enabled = false; // Prevent GenerateTextMesh before material is ready
                bTmp.text = "!";
                bTmp.fontStyle = FontStyles.Bold;
                bTmp.fontSize = 22f;
                bTmp.alignment = TextAlignmentOptions.Center;
                bTmp.color = Color.white;
                bTmp.raycastTarget = false;
                bTmp.enabled = true; // Now safe to generate mesh
            }

            if (onClick != null)
            {
                btn.onClick.AddListener(() =>
                {
                    if (MobControl.Audio.ZombieSoundManager.Instance != null)
                    {
                        MobControl.Audio.ZombieSoundManager.Instance.PlayUIClick();
                    }
                    StartCoroutine(PunchScale(btnObj.transform, 0.12f, onClick));
                });
            }

            return btn;
        }

        private Button CreateTransparentButton(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, System.Action onClick)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);

            Image img = btnObj.AddComponent<Image>();
            img.color = Color.clear;
            img.raycastTarget = true;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            if (onClick != null)
            {
                btn.onClick.AddListener(() =>
                {
                    if (MobControl.Audio.ZombieSoundManager.Instance != null)
                    {
                        MobControl.Audio.ZombieSoundManager.Instance.PlayUIClick();
                    }
                    StartCoroutine(PunchScale(btnObj.transform, 0.12f, onClick));
                });
            }

            return btn;
        }

        private Button CreateImageButton(string name, Transform parent, Sprite sprite, Vector2 size, System.Action onClick)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.pivot = new Vector2(0.5f, 0.5f);

            Image img = btnObj.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = true;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            LayoutElement le = btnObj.AddComponent<LayoutElement>();
            le.preferredWidth = size.x;
            le.preferredHeight = size.y;
            le.minWidth = size.x * 0.7f;
            le.minHeight = size.y * 0.7f;

            if (onClick != null)
            {
                btn.onClick.AddListener(() =>
                {
                    if (MobControl.Audio.ZombieSoundManager.Instance != null)
                    {
                        MobControl.Audio.ZombieSoundManager.Instance.PlayUIClick();
                    }
                    StartCoroutine(PunchScale(btnObj.transform, 0.12f, onClick));
                });
            }

            return btn;
        }

        private readonly System.Collections.Generic.HashSet<Button> hookedButtons = new System.Collections.Generic.HashSet<Button>();

        public void HookAllButtonsWithSound(GameObject root)
        {
            if (root == null) return;
            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button btn = buttons[i];
                if (btn == null) continue;
                if (!hookedButtons.Contains(btn))
                {
                    hookedButtons.Add(btn);
                    btn.onClick.AddListener(OnGlobalButtonClicked);
                }
            }
        }

        private void OnGlobalButtonClicked()
        {
            if (MobControl.Audio.ZombieSoundManager.Instance != null)
            {
                MobControl.Audio.ZombieSoundManager.Instance.PlayUIClick();
            }
        }

        private Sprite LoadSpriteFromUI(params string[] candidateRelativePaths)
        {
            foreach (string relPath in candidateRelativePaths)
            {
                try
                {
                    Sprite s = Resources.Load<Sprite>(relPath);
                    if (s != null) return s;

                    Texture2D tex = Resources.Load<Texture2D>(relPath);
                    if (tex != null)
                    {
                        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    }

                    string[] extensions = new string[] { ".png", ".jpg", "" };
                    foreach (string ext in extensions)
                    {
                        string localPath = System.IO.Path.Combine(Application.dataPath, "Resources", relPath + ext);
                        if (System.IO.File.Exists(localPath))
                        {
                            byte[] data = System.IO.File.ReadAllBytes(localPath);
                            Texture2D fileTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                            if (fileTex.LoadImage(data))
                            {
                                return Sprite.Create(fileTex, new Rect(0, 0, fileTex.width, fileTex.height), new Vector2(0.5f, 0.5f));
                            }
                        }
                    }
                }
                catch { }
            }
            return null;
        }

        /// <summary>Home screen is responsive-only now (classic fallback image removed).</summary>
        public void UpdateHomeScreenContainers()
        {
            if (homeScreenResponsiveContainer != null) homeScreenResponsiveContainer.SetActive(true);
            if (modularPlayBtnRect != null) homeScreenPlayBtnRect = modularPlayBtnRect;
            if (modularShopBtnRect != null) homeScreenShopBtnRect = modularShopBtnRect;
            if (modularCharactersBtnRect != null) homeScreenCharactersBtnRect = modularCharactersBtnRect;
            if (modularWeaponsBtnRect != null) homeScreenWeaponsBtnRect = modularWeaponsBtnRect;
            if (modularMissionsBtnRect != null) homeScreenMissionsBtnRect = modularMissionsBtnRect;
            if (modularDailyBtnRect != null) homeScreenDailyBtnRect = modularDailyBtnRect;
            if (modularUpgradeBtnRect != null) homeScreenUpgradeBtnRect = modularUpgradeBtnRect;
        }

        private void BuildZombieSquadHomeScreenPanel(Transform canvasRoot)
        {
            // Fullscreen solid background matching artwork color palette so widescreen/tablet letterboxing blends seamlessly
            homeScreenPanel = CreateFullPanel("ZombieSquadHomeScreenPanel", canvasRoot, new Color(0.06f, 0.08f, 0.12f, 1f));

            // ==========================================
            // 2. RESPONSIVE MODULAR CONTAINER (HTML STYLE)
            // ==========================================
            homeScreenResponsiveContainer = new GameObject("Responsive_Modular_Container");
            homeScreenResponsiveContainer.transform.SetParent(homeScreenPanel.transform, false);
            RectTransform respRootRt = homeScreenResponsiveContainer.AddComponent<RectTransform>();
            respRootRt.anchorMin = Vector2.zero;
            respRootRt.anchorMax = Vector2.one;
            respRootRt.offsetMin = Vector2.zero;
            respRootRt.offsetMax = Vector2.zero;

            // Ambient background filler for ultra-wide aspect ratios
            GameObject respAmbient = new GameObject("Modular_Ambient_Bg");
            respAmbient.transform.SetParent(homeScreenResponsiveContainer.transform, false);
            RectTransform respAmbRt = respAmbient.AddComponent<RectTransform>();
            respAmbRt.anchorMin = Vector2.zero;
            respAmbRt.anchorMax = Vector2.one;
            respAmbRt.offsetMin = Vector2.zero;
            respAmbRt.offsetMax = Vector2.zero;

            Image respAmbImg = respAmbient.AddComponent<Image>();
            respAmbImg.color = new Color(0.12f, 0.16f, 0.24f, 1f);
            respAmbImg.raycastTarget = false;

            // Clean Scenery Background (No baked buttons or text); baked home art as last-resort backdrop
            Sprite masterArtSprite = LoadZombieSquadHomeScreenSprite();
            Sprite cleanScenery = LoadSpriteFromUI(
                "UI/HomeScreen/HomeScreen_Scenery_Clean",
                "UI/HomeScreen_Scenery_Clean"
            ) ?? masterArtSprite;

            GameObject respScenery = new GameObject("Modular_Scenery_Bg");
            respScenery.transform.SetParent(homeScreenResponsiveContainer.transform, false);
            RectTransform sceneryRt = respScenery.AddComponent<RectTransform>();
            sceneryRt.anchorMin = Vector2.zero;
            sceneryRt.anchorMax = Vector2.one;
            sceneryRt.offsetMin = Vector2.zero;
            sceneryRt.offsetMax = Vector2.zero;

            Image sceneryImg = respScenery.AddComponent<Image>();
            sceneryImg.sprite = cleanScenery;
            sceneryImg.preserveAspect = true;
            sceneryImg.raycastTarget = false;

            AspectRatioFitter sceneryFitter = respScenery.AddComponent<AspectRatioFitter>();
            sceneryFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            sceneryFitter.aspectRatio = (cleanScenery != null && cleanScenery.rect.height > 0)
                ? (cleanScenery.rect.width / cleanScenery.rect.height)
                : (1080f / 1920f);

            // Title Logo (Zombie Rage)
            Sprite titleSprite = LoadSpriteFromUI(
                "UI/HomeScreen/Home_Title_ZombieSquad",
                "UI/HomeScreen/crop_title_logo"
            );
            if (titleSprite != null)
            {
                GameObject titleObj = new GameObject("Modular_Title_ZombieSquad");
                titleObj.transform.SetParent(homeScreenResponsiveContainer.transform, false);
                RectTransform titleRt = titleObj.AddComponent<RectTransform>();
                titleRt.anchorMin = new Vector2(0.5f, 1f);
                titleRt.anchorMax = new Vector2(0.5f, 1f);
                titleRt.pivot = new Vector2(0.5f, 1f);
                titleRt.anchoredPosition = new Vector2(0f, -95f);
                titleRt.sizeDelta = new Vector2(540f, 280f);

                Image titleImg = titleObj.AddComponent<Image>();
                titleImg.sprite = titleSprite;
                titleImg.preserveAspect = true;
                titleImg.raycastTarget = false;
            }

            // Settings Button (Top-Right)
            Sprite settingsSprite = LoadSpriteFromUI(
                "UI/HomeScreen/Home_Btn_Settings",
                "UI/HomeScreen/crop_btn_settings",
                "UI/HomeScreen/Icon_3D_Settings_HD"
            );
            Button respSettingsBtn = CreateImageButton("Btn_Settings_Modular", homeScreenResponsiveContainer.transform, settingsSprite, new Vector2(95f, 95f), () =>
            {
                Debug.Log("[ZombieSquad] Settings Clicked -> Opening Settings Popup!");
                ShowZombieSquadSettings();
            });
            RectTransform settingsRt = respSettingsBtn.GetComponent<RectTransform>();
            settingsRt.anchorMin = new Vector2(1f, 1f);
            settingsRt.anchorMax = new Vector2(1f, 1f);
            settingsRt.pivot = new Vector2(1f, 1f);
            settingsRt.anchoredPosition = new Vector2(-45f, -50f);

            // Left Column (Shop, Characters, Weapons)
            GameObject leftCol = new GameObject("Modular_LeftColumn");
            leftCol.transform.SetParent(homeScreenResponsiveContainer.transform, false);
            RectTransform leftColRt = leftCol.AddComponent<RectTransform>();
            leftColRt.anchorMin = new Vector2(0f, 0.5f);
            leftColRt.anchorMax = new Vector2(0f, 0.5f);
            leftColRt.pivot = new Vector2(0f, 0.5f);
            leftColRt.anchoredPosition = new Vector2(40f, -30f);
            leftColRt.sizeDelta = new Vector2(180f, 520f);

            VerticalLayoutGroup leftVlg = leftCol.AddComponent<VerticalLayoutGroup>();
            leftVlg.spacing = 22f;
            leftVlg.childAlignment = TextAnchor.MiddleCenter;
            leftVlg.childControlWidth = true;
            leftVlg.childControlHeight = true;
            leftVlg.childForceExpandWidth = false;
            leftVlg.childForceExpandHeight = false;

            Sprite shopSprite = LoadSpriteFromUI("UI/HomeScreen/Home_Btn_Shop", "UI/HomeScreen/crop_btn_shop");
            Button respShopBtn = CreateImageButton("Btn_Shop_Modular", leftCol.transform, shopSprite, new Vector2(175f, 155f), () =>
            {
                Debug.Log("[ZombieSquad] Shop Clicked -> Opening Shop Screen!");
                ShowZombieSquadShopScreen();
            });
            modularShopBtnRect = respShopBtn.GetComponent<RectTransform>();

            Sprite charSprite = LoadSpriteFromUI("UI/HomeScreen/Home_Btn_Characters", "UI/HomeScreen/crop_btn_characters");
            Button respCharBtn = CreateImageButton("Btn_Characters_Modular", leftCol.transform, charSprite, new Vector2(175f, 155f), () =>
            {
                Debug.Log("[ZombieSquad] Characters Clicked -> Opening Characters Screen!");
                ShowZombieSquadCharactersScreen();
            });
            modularCharactersBtnRect = respCharBtn.GetComponent<RectTransform>();

            Sprite wepSprite = LoadSpriteFromUI("UI/HomeScreen/Home_Btn_Weapons", "UI/HomeScreen/crop_btn_weapons");
            Button respWepBtn = CreateImageButton("Btn_Weapons_Modular", leftCol.transform, wepSprite, new Vector2(175f, 155f), () =>
            {
                Debug.Log("[ZombieSquad] Weapons Clicked -> Opening Weapons Screen!");
                ShowZombieSquadWeaponsScreen();
            });
            modularWeaponsBtnRect = respWepBtn.GetComponent<RectTransform>();

            // Right Column (Missions, Daily, Upgrade)
            GameObject rightCol = new GameObject("Modular_RightColumn");
            rightCol.transform.SetParent(homeScreenResponsiveContainer.transform, false);
            RectTransform rightColRt = rightCol.AddComponent<RectTransform>();
            rightColRt.anchorMin = new Vector2(1f, 0.5f);
            rightColRt.anchorMax = new Vector2(1f, 0.5f);
            rightColRt.pivot = new Vector2(1f, 0.5f);
            rightColRt.anchoredPosition = new Vector2(-40f, -30f);
            rightColRt.sizeDelta = new Vector2(180f, 520f);

            VerticalLayoutGroup rightVlg = rightCol.AddComponent<VerticalLayoutGroup>();
            rightVlg.spacing = 22f;
            rightVlg.childAlignment = TextAnchor.MiddleCenter;
            rightVlg.childControlWidth = true;
            rightVlg.childControlHeight = true;
            rightVlg.childForceExpandWidth = false;
            rightVlg.childForceExpandHeight = false;

            Sprite missionsSprite = LoadSpriteFromUI("UI/HomeScreen/Home_Btn_Missions", "UI/HomeScreen/crop_btn_missions");
            Button respMissionsBtn = CreateImageButton("Btn_Missions_Modular", rightCol.transform, missionsSprite, new Vector2(175f, 155f), () =>
            {
                Debug.Log("[ZombieSquad] Missions Clicked -> Opening Level Map Screen!");
                ShowZombieSquadLevelMap();
            });
            modularMissionsBtnRect = respMissionsBtn.GetComponent<RectTransform>();

            Sprite dailySprite = LoadSpriteFromUI("UI/HomeScreen/Home_Btn_Daily", "UI/HomeScreen/crop_btn_daily");
            Button respDailyBtn = CreateImageButton("Btn_Daily_Modular", rightCol.transform, dailySprite, new Vector2(175f, 155f), () =>
            {
                Debug.Log("[ZombieSquad] Daily Rewards Clicked -> Opening Daily Screen!");
                ShowZombieSquadDailyScreen();
            });
            modularDailyBtnRect = respDailyBtn.GetComponent<RectTransform>();

            Sprite upgSprite = LoadSpriteFromUI("UI/HomeScreen/Home_Btn_Upgrade", "UI/HomeScreen/crop_btn_upgrade");
            Button respUpgBtn = CreateImageButton("Btn_Upgrade_Modular", rightCol.transform, upgSprite, new Vector2(175f, 155f), () =>
            {
                Debug.Log("[ZombieSquad] Upgrade Clicked -> Opening Squad Upgrade Screen!");
                ShowZombieSquadUpgradeScreen();
            });
            modularUpgradeBtnRect = respUpgBtn.GetComponent<RectTransform>();

            // Center Hero Character Interactive Area (The authentic hero and squad are seamlessly rendered in the HD background)
            GameObject heroObj = new GameObject("Modular_Hero_HitArea");
            heroObj.transform.SetParent(homeScreenResponsiveContainer.transform, false);
            RectTransform heroRt = heroObj.AddComponent<RectTransform>();
            heroRt.anchorMin = new Vector2(0.5f, 0f);
            heroRt.anchorMax = new Vector2(0.5f, 0f);
            heroRt.pivot = new Vector2(0.5f, 0f);
            heroRt.anchoredPosition = new Vector2(0f, 250f);
            heroRt.sizeDelta = new Vector2(300f, 400f);

            Image heroHitImg = heroObj.AddComponent<Image>();
            heroHitImg.color = new Color(0f, 0f, 0f, 0f); // Completely transparent hit target over the hero
            heroHitImg.raycastTarget = true;

            // Tapping hero opens Characters screen
            Button heroBtn = heroObj.AddComponent<Button>();
            heroBtn.targetGraphic = heroHitImg;
            heroBtn.onClick.AddListener(() =>
            {
                Debug.Log("[ZombieSquad] Hero tapped -> Opening Characters Screen!");
                ShowZombieSquadCharactersScreen();
            });

            // Bottom PLAY Button
            Sprite playSprite = LoadSpriteFromUI(
                "UI/HomeScreen/Home_Btn_Play",
                "UI/HomeScreen/crop_btn_play",
                "UI/HomeScreen/Btn_Play_3D_HD"
            );
            Button respPlayBtn = CreateImageButton("Btn_Play_Modular", homeScreenResponsiveContainer.transform, playSprite, new Vector2(540f, 180f), () =>
            {
                // PLAY always opens the Level Map first; the player picks a level there
                Debug.Log("[ZombieSquad] PLAY Button Pressed -> Opening Level Map");
                ShowZombieSquadLevelMap();
            });
            modularPlayBtnRect = respPlayBtn.GetComponent<RectTransform>();
            modularPlayBtnRect.anchorMin = new Vector2(0.5f, 0f);
            modularPlayBtnRect.anchorMax = new Vector2(0.5f, 0f);
            modularPlayBtnRect.pivot = new Vector2(0.5f, 0f);
            modularPlayBtnRect.anchoredPosition = new Vector2(0f, 110f);

            UpdateHomeScreenContainers();
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

                Texture2D resTex = Resources.Load<Texture2D>("UI/Padlock");
                if (resTex != null)
                {
                    return Sprite.Create(resTex, new Rect(0, 0, resTex.width, resTex.height), new Vector2(0.5f, 0.5f));
                }

#if UNITY_EDITOR && !UNITY_ANDROID
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
#endif
            }
            catch {}
            return null;
        }

        private Sprite LoadBossSkullSprite()
        {
            try
            {
                Sprite s = Resources.Load<Sprite>("UI/BossSkull") ?? Resources.Load<Sprite>("UI/Ref_BossSkull_HD");
                if (s != null) return s;

                Texture2D resTex = Resources.Load<Texture2D>("UI/BossSkull") ?? Resources.Load<Texture2D>("UI/Ref_BossSkull_HD");
                if (resTex != null)
                {
                    return Sprite.Create(resTex, new Rect(0, 0, resTex.width, resTex.height), new Vector2(0.5f, 0.5f));
                }

#if UNITY_EDITOR && !UNITY_ANDROID
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
#endif
            }
            catch {}
            return null;
        }

        private void AddLevelMapExtension(Transform artContainer, string name, string sprite, Vector2 aMin, Vector2 aMax, Vector2 pivot, bool horizontal)
        {
            Sprite sp = LoadSpriteFromUI(MenuHDPath + sprite);
            if (sp == null) return;
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(artContainer, false);
            go.transform.SetAsFirstSibling();
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.pivot = pivot;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            Image img = go.AddComponent<Image>();
            img.sprite = sp;
            img.raycastTarget = false;
            AspectRatioFitter f = go.AddComponent<AspectRatioFitter>();
            f.aspectMode = horizontal ? AspectRatioFitter.AspectMode.WidthControlsHeight : AspectRatioFitter.AspectMode.HeightControlsWidth;
            f.aspectRatio = sp.rect.width / sp.rect.height;
        }

        private void BuildZombieSquadLevelMapPanel(Transform canvasRoot)
        {
            levelMapPanel = CreateFullPanel("ZombieSquadLevelMapPanel", canvasRoot, new Color(0.04f, 0.06f, 0.10f, 1f));

            // HD (2x upscaled + sharpened) map art; falls back to the original
            Sprite mapSprite = LoadSpriteFromUI(MenuHDPath + "Menu_LevelMap") ?? LoadZombieSquadLevelMapSprite();

            // Blurred, darkened copy fills the extra space on very tall / wide screens (no duplicated-looking map)
            SetupAmbientSceneryCover(levelMapPanel, LoadSpriteFromUI(MenuHDPath + "Menu_LevelMap_Backdrop") ?? mapSprite, 9f / 16f);

            // Container for 9:16 aspect ratio
            GameObject artContainer = new GameObject("ArtContainer");
            artContainer.transform.SetParent(levelMapPanel.transform, false);
            RectTransform artRt = artContainer.AddComponent<RectTransform>();
            artRt.anchorMin = Vector2.zero;
            artRt.anchorMax = Vector2.one;
            artRt.offsetMin = Vector2.zero;
            artRt.offsetMax = Vector2.zero;
            levelMapArtContainerRect = artRt;

            AspectRatioFitter mapArf = artContainer.AddComponent<AspectRatioFitter>();
            mapArf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            mapArf.aspectRatio = 9f / 16f;

            // Background Image (The 9:16 Level Map Image)
            Image mapImg = artContainer.AddComponent<Image>();
            if (mapSprite != null)
            {
                mapImg.sprite = mapSprite;
                if (mapSprite.rect.height > 0) mapArf.aspectRatio = mapSprite.rect.width / mapSprite.rect.height;
            }
            mapImg.raycastTarget = true;

            // Seamless art extensions so very tall phones / tablets see more map instead of bars
            AddLevelMapExtension(artContainer.transform, "ExtTop", "Menu_LevelMap_ExtTop", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0f), true);
            AddLevelMapExtension(artContainer.transform, "ExtBottom", "Menu_LevelMap_ExtBottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 1f), true);
            AddLevelMapExtension(artContainer.transform, "ExtLeft", "Menu_LevelMap_ExtLeft", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 0.5f), false);
            AddLevelMapExtension(artContainer.transform, "ExtRight", "Menu_LevelMap_ExtRight", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), false);

            // Top-Left Back Button to Home Screen
            GameObject backBtnObj = new GameObject("Btn_Back");
            backBtnObj.transform.SetParent(artContainer.transform, false);
            RectTransform backRt = backBtnObj.AddComponent<RectTransform>();
            // Crisp HD back button drawn exactly over the (soft) baked one
            backRt.anchorMin = new Vector2(0.035f, 0.905f);
            backRt.anchorMax = new Vector2(0.175f, 0.985f);
            backRt.offsetMin = Vector2.zero;
            backRt.offsetMax = Vector2.zero;

            Image backImg = backBtnObj.AddComponent<Image>();
            Sprite hdBack = LoadSpriteFromUI(ShopHDPath + "Shop_BackButton");
            if (hdBack != null) { backImg.sprite = hdBack; backImg.preserveAspect = true; backImg.color = Color.white; }
            else backImg.color = new Color(1f, 1f, 1f, 0.001f);
            backImg.raycastTarget = true;

            Button backBtn = backBtnObj.AddComponent<Button>();
            backBtn.targetGraphic = backImg;
            levelMapBackBtnRect = backRt;
            backBtn.onClick.AddListener(() =>
            {
                StartCoroutine(PunchScale(backBtnObj.transform, 0.12f, ShowZombieSquadHomeScreen));
            });

            BuildLevelMapNodes(artContainer.transform);
        }

        private void BuildLevelMapNodes(Transform containerTr)
        {
            if (containerTr == null) return;

            // Clear any lingering nodes
            for (int i = containerTr.childCount - 1; i >= 0; i--)
            {
                Transform child = containerTr.GetChild(i);
                if (child.name.StartsWith("Node_Lvl_") || child.name == "Node_Boss")
                {
                    SafeDestroy(child.gameObject);
                }
            }

            // Precise Road Coordinates along the clean asphalt track
            Vector2[] nodePositions = GetMapNodePositions();

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
            levelMapNodeRects = new RectTransform[nodePositions.Length];

            for (int i = 0; i < nodePositions.Length; i++)
            {
                int lvlNum = i + 1;
                bool isBoss = (i == 7);
                Vector2 normPos = nodePositions[i];
                Vector2 size = nodeSizes[i];

                GameObject nodeRoot = new GameObject(isBoss ? "Node_Boss" : $"Node_Lvl_{lvlNum}");
                nodeRoot.transform.SetParent(containerTr, false);

                // Node box is defined relative to the map art (reference 1080x1920), so it scales with the map
                RectTransform nodeRt = nodeRoot.AddComponent<RectTransform>();
                float nodeScale = isBoss ? 1f : 1.15f;
                Vector2 half = new Vector2(size.x * nodeScale / 1080f * 0.5f, size.y * nodeScale / 1920f * 0.5f);
                nodeRt.anchorMin = normPos - half;
                nodeRt.anchorMax = normPos + half;
                nodeRt.pivot = new Vector2(0.5f, 0.5f);
                nodeRt.anchoredPosition = Vector2.zero;
                nodeRt.offsetMin = Vector2.zero;
                nodeRt.offsetMax = Vector2.zero;
                levelMapNodeRects[i] = nodeRt;

                // All visuals live in a fixed design-size child that is uniformly scaled to the node box
                GameObject visualObj = new GameObject("Visual", typeof(RectTransform));
                visualObj.transform.SetParent(nodeRoot.transform, false);
                ShopResponsiveFitter visFit = visualObj.AddComponent<ShopResponsiveFitter>();
                visFit.designSize = size;
                visFit.minScale = 0.2f;
                visFit.maxScale = 4f;
                Transform visualTr = visualObj.transform;

                // Outer border ring
                GameObject outerRingObj = new GameObject("OuterRing");
                outerRingObj.transform.SetParent(visualTr, false);
                RectTransform outerRt = outerRingObj.AddComponent<RectTransform>();
                outerRt.anchorMin = Vector2.zero;
                outerRt.anchorMax = Vector2.one;
                outerRt.offsetMin = Vector2.zero;
                outerRt.offsetMax = Vector2.zero;
                Image outerRing = outerRingObj.AddComponent<Image>();
                outerRing.sprite = LoadSpriteFromUI(MenuHDPath + "Menu_Node_Ring") ?? GetCircleBadgeSprite();
                outerRing.raycastTarget = false;

                // Inner Disc
                GameObject innerDiscObj = new GameObject("InnerDisc");
                innerDiscObj.transform.SetParent(visualTr, false);
                RectTransform innerRt = innerDiscObj.AddComponent<RectTransform>();
                innerRt.anchorMin = Vector2.zero;
                innerRt.anchorMax = Vector2.one;
                innerRt.offsetMin = isBoss ? Vector2.zero : new Vector2(7, 9);
                innerRt.offsetMax = isBoss ? Vector2.zero : new Vector2(-7, -5);
                Image innerDisc = innerDiscObj.AddComponent<Image>();
                innerDisc.sprite = isBoss ? null : (LoadSpriteFromUI(MenuHDPath + "Menu_Node_Disc") ?? GetCircleBadgeSprite());
                innerDisc.raycastTarget = false;

                // Icon (Padlock for locked levels)
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(visualTr, false);
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
                TextMeshProUGUI tmp = CreateText("Label", visualTr, lvlNum.ToString(), 42, Color.white, TextAlignmentOptions.Center, true);
                tmp.raycastTarget = false;
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                StyleShopTmp(tmp, new Color(0.02f, 0.12f, 0.38f, 1f), 0.22f, true);
                RectTransform txtRt = tmp.GetComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero;
                txtRt.anchorMax = Vector2.one;
                txtRt.offsetMin = Vector2.zero;
                txtRt.offsetMax = Vector2.zero;

                // Root transparent image on nodeRoot for 100% reliable click interception
                Image rootImg = nodeRoot.AddComponent<Image>();
                rootImg.color = Color.clear;
                rootImg.raycastTarget = true;

                Button btn = nodeRoot.AddComponent<Button>();
                btn.targetGraphic = rootImg;

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
                    starsObj.transform.SetParent(visualTr, false);
                    RectTransform starsRt = starsObj.AddComponent<RectTransform>();
                    starsRt.anchorMin = new Vector2(0.5f, 1f);
                    starsRt.anchorMax = new Vector2(0.5f, 1f);
                    starsRt.pivot = new Vector2(0.5f, 0.5f);
                    starsRt.anchoredPosition = new Vector2(0, 12);
                    starsRt.sizeDelta = new Vector2(84, 32);

                    // Pill Badge background for high contrast against map grass/road
                    Image badgeBg = starsObj.AddComponent<Image>();
                    Sprite bgSp = LoadSpriteFromUI(MenuHDPath + "Menu_Star_Badge") ?? LoadStarBadgeBgSprite();
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
                    item.numberText.fontSize = 46;
                    item.numberText.color = Color.white;
                    item.numberText.outlineColor = new Color(0.02f, 0.14f, 0.40f, 1f);

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
                    item.numberText.fontSize = 48;
                    item.numberText.color = Color.white;
                    item.numberText.outlineColor = new Color(0.45f, 0.20f, 0.0f, 1f);

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
                    item.numberText.fontSize = 22;
                    item.numberText.color = new Color(0.92f, 0.94f, 0.97f, 1f);
                    item.numberText.outlineColor = new Color(0.10f, 0.12f, 0.16f, 1f);
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

        public static Vector2[] GetMapNodePositions()
        {
            return new Vector2[]
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
        }

        public void EnsureLevelMapBound()
        {
            if (levelMapPanel == null) return;
            Transform artContainerTr = levelMapPanel.transform.Find("ArtContainer");
            if (artContainerTr == null) return;
            levelMapArtContainerRect = artContainerTr.GetComponent<RectTransform>();

            Transform backTr = artContainerTr.Find("Btn_Back");
            if (backTr != null)
            {
                levelMapBackBtnRect = backTr.GetComponent<RectTransform>();
                Button b = backTr.GetComponent<Button>();
                if (b != null)
                {
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(() =>
                    {
                        StartCoroutine(PunchScale(backTr, 0.12f, ShowZombieSquadHomeScreen));
                    });
                }
            }

            if (levelMapNodeRects == null || levelMapNodeRects.Length < 8)
            {
                levelMapNodeRects = new RectTransform[8];
            }

            Transform testNode = artContainerTr.Find("Node_Lvl_1");
            if (testNode == null || mapNodeItems == null || mapNodeItems.Count < 8)
            {
                BuildLevelMapNodes(artContainerTr);
            }
            else
            {
                for (int i = 0; i < 8; i++)
                {
                    int lvlNum = i + 1;
                    string nodeName = (i == 7) ? "Node_Boss" : $"Node_Lvl_{lvlNum}";
                    Transform nodeTr = artContainerTr.Find(nodeName);
                    if (nodeTr != null)
                    {
                        levelMapNodeRects[i] = nodeTr.GetComponent<RectTransform>();
                        Button btn = nodeTr.GetComponent<Button>();
                        if (btn != null)
                        {
                            btn.onClick.RemoveAllListeners();
                            int capturedLvl = lvlNum;
                            btn.onClick.AddListener(() =>
                            {
                                OnMapNodeClicked(capturedLvl, nodeTr);
                            });
                        }
                    }
                }
            }
        }

        private void OnMapNodeClicked(int levelNumber, Transform nodeTransform)
        {
            int unlockedLevel = PlayerPrefs.GetInt(PREF_UNLOCKED_LEVEL, 1);
            if (unlockedLevel < 1) unlockedLevel = 1;

            if (levelNumber <= unlockedLevel || levelNumber == 1)
            {
                Debug.Log($"[ZombieSquad] Level {levelNumber} clicked! Launching Level {levelNumber}...");
                if (nodeTransform != null)
                {
                    StartCoroutine(PunchScale(nodeTransform, 0.12f, null));
                }
                StartMission(levelNumber);
            }
            else
            {
                // Locked!
                Debug.Log($"[ZombieSquad] Level {levelNumber} is LOCKED! Complete Level {unlockedLevel} first.");
                if (nodeTransform != null)
                {
                    StartCoroutine(WobbleShake(nodeTransform, 0.25f));
                }
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
                Sprite ext = Resources.Load<Sprite>("UI/ZombieSquad_CharactersScreen_Extended");
                if (ext != null) return ext;

                Texture2D extTex = Resources.Load<Texture2D>("UI/ZombieSquad_CharactersScreen_Extended");
                if (extTex != null)
                {
                    extTex.filterMode = FilterMode.Bilinear;
                    return Sprite.Create(extTex, new Rect(0, 0, extTex.width, extTex.height), new Vector2(0.5f, 0.5f));
                }

                string extPath = System.IO.Path.Combine(Application.dataPath, "Resources", "UI", "ZombieSquad_CharactersScreen_Extended.png");
                if (System.IO.File.Exists(extPath))
                {
                    byte[] fileData = System.IO.File.ReadAllBytes(extPath);
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
                    if (tex.LoadImage(fileData))
                    {
                        tex.filterMode = FilterMode.Bilinear;
                        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    }
                }

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
        #endregion

        #region Dynamic Currency & Settings System
        private GameObject settingsPanel;
        private RectTransform settingsDialogRt;
        private CanvasGroup settingsCanvasGroup;
        private Coroutine settingsPopRoutine;
        private SettingsToggleRefs settingsMusicToggle, settingsSfxToggle, settingsVibToggle;
        private Image settingsMusicIcon, settingsSfxIcon, settingsVibIcon;

        private const string PREF_VIBRATION = "ZombieSquad_Vibration";
        private const string PREF_VOLUME = "ZombieSquad_Volume";
        private const string PREF_DIAMONDS = "ZombieRunner_Diamonds";

        public bool IsVibrationEnabled => PlayerPrefs.GetInt(PREF_VIBRATION, 1) == 1;

        private List<TextMeshProUGUI> allCoinsDisplays = new List<TextMeshProUGUI>();
        private List<TextMeshProUGUI> allDiamondsDisplays = new List<TextMeshProUGUI>();

        public int GetPlayerCoins() => PlayerPrefs.GetInt(PREF_COINS, DEFAULT_COINS);
        public int GetPlayerDiamonds() => PlayerPrefs.GetInt(PREF_DIAMONDS, DEFAULT_DIAMONDS);

        public void AddPlayerCoins(int amount)
        {
            int c = GetPlayerCoins() + amount;
            PlayerPrefs.SetInt(PREF_COINS, c);
            PlayerPrefs.Save();
            UpdateAllCurrencyDisplays();
            UpdateUpgradeScreenVisuals();
            UpdateWeaponsScreenVisuals();
        }

        public bool SpendPlayerCoins(int amount)
        {
            int c = GetPlayerCoins();
            if (c < amount) return false;
            c -= amount;
            PlayerPrefs.SetInt(PREF_COINS, c);
            PlayerPrefs.Save();
            UpdateAllCurrencyDisplays();
            UpdateUpgradeScreenVisuals();
            UpdateWeaponsScreenVisuals();
            return true;
        }

        public void AddPlayerDiamonds(int amount)
        {
            int d = GetPlayerDiamonds() + amount;
            PlayerPrefs.SetInt(PREF_DIAMONDS, d);
            PlayerPrefs.Save();
            UpdateAllCurrencyDisplays();
            UpdateUpgradeScreenVisuals();
            UpdateWeaponsScreenVisuals();
        }

        public bool SpendPlayerDiamonds(int amount)
        {
            int d = GetPlayerDiamonds();
            if (d < amount) return false;
            d -= amount;
            PlayerPrefs.SetInt(PREF_DIAMONDS, d);
            PlayerPrefs.Save();
            UpdateAllCurrencyDisplays();
            UpdateUpgradeScreenVisuals();
            UpdateWeaponsScreenVisuals();
            return true;
        }

        public void UpdateAllCurrencyDisplays()
        {
            int coins = GetPlayerCoins();
            int diamonds = GetPlayerDiamonds();

            for (int i = allCoinsDisplays.Count - 1; i >= 0; i--)
            {
                if (allCoinsDisplays[i] == null) allCoinsDisplays.RemoveAt(i);
                else allCoinsDisplays[i].text = coins.ToString("N0");
            }
            for (int i = allDiamondsDisplays.Count - 1; i >= 0; i--)
            {
                if (allDiamondsDisplays[i] == null) allDiamondsDisplays.RemoveAt(i);
                else allDiamondsDisplays[i].text = diamonds.ToString("N0");
            }
        }

        public void ShowZombieSquadSettings()
        {
            if (settingsPanel == null && mainCanvas != null) BuildSettingsPopupPanel(mainCanvas.transform);
            if (settingsPanel == null) return;
            settingsPanel.transform.SetAsLastSibling();
            settingsPanel.SetActive(true);
            UpdateSettingsVisuals();
            if (settingsPopRoutine != null) StopCoroutine(settingsPopRoutine);
            settingsPopRoutine = StartCoroutine(SettingsPopIn());
        }

        public void HideZombieSquadSettings()
        {
            if (settingsPanel == null || !settingsPanel.activeSelf) return;
            if (settingsPopRoutine != null) StopCoroutine(settingsPopRoutine);
            settingsPopRoutine = StartCoroutine(SettingsPopOut());
        }

        private IEnumerator SettingsPopIn()
        {
            float t = 0f;
            const float d = 0.22f;
            while (t < d)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / d);
                // ease-out-back for a springy pop
                float s = 1f + 2.2f * Mathf.Pow(k - 1f, 3f) + 1.2f * Mathf.Pow(k - 1f, 2f);
                if (settingsDialogRt != null) settingsDialogRt.localScale = Vector3.one * Mathf.LerpUnclamped(0.82f, 1f, s);
                if (settingsCanvasGroup != null) settingsCanvasGroup.alpha = Mathf.Clamp01(k * 2f);
                yield return null;
            }
            if (settingsDialogRt != null) settingsDialogRt.localScale = Vector3.one;
            if (settingsCanvasGroup != null) settingsCanvasGroup.alpha = 1f;
            settingsPopRoutine = null;
        }

        private IEnumerator SettingsPopOut()
        {
            float t = 0f;
            const float d = 0.14f;
            while (t < d)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / d);
                if (settingsDialogRt != null) settingsDialogRt.localScale = Vector3.one * Mathf.Lerp(1f, 0.88f, k);
                if (settingsCanvasGroup != null) settingsCanvasGroup.alpha = 1f - k;
                yield return null;
            }
            if (settingsPanel != null) settingsPanel.SetActive(false);
            settingsPopRoutine = null;
        }

        public void ToggleVibration()
        {
            bool next = !IsVibrationEnabled;
            PlayerPrefs.SetInt(PREF_VIBRATION, next ? 1 : 0);
            PlayerPrefs.Save();
            if (next)
            {
                #if UNITY_ANDROID || UNITY_IOS
                Handheld.Vibrate();
                #endif
            }
            UpdateSettingsVisuals();
        }

        private void UpdateSettingsVisuals()
        {
            var snd = MobControl.Audio.ZombieSoundManager.Instance;
            bool music = snd == null || snd.IsMusicEnabled;
            bool sfx = snd == null || snd.IsSfxEnabled;
            bool vib = IsVibrationEnabled;

            SetSettingsToggle(settingsMusicToggle, music);
            SetSettingsToggle(settingsSfxToggle, sfx);
            SetSettingsToggle(settingsVibToggle, vib);
            SetMenuSprite(settingsMusicIcon, music ? "Menu_Btn_Music_On" : "Menu_Btn_Music_Off");
            SetMenuSprite(settingsSfxIcon, sfx ? "Menu_Btn_Sound_On" : "Menu_Btn_Sound_Off");
            if (settingsVibIcon != null) settingsVibIcon.color = vib ? Color.white : new Color(0.55f, 0.6f, 0.7f, 1f);

            // keep the in-game pause toggles in sync
            if (pauseMusicBtnImg != null) RefreshPauseToggles();
        }

        private void SetSettingsToggle(SettingsToggleRefs r, bool on)
        {
            if (r == null) return;
            SetMenuSprite(r.bg, on ? "Menu_Btn_Green" : "Menu_Btn_Grey");
            r.label.text = on ? "ON" : "OFF";
            StyleShopTmp(r.label, on ? new Color(0.03f, 0.25f, 0.07f, 1f) : new Color(0.15f, 0.17f, 0.22f, 1f), 0.26f, false);
        }

        private class SettingsToggleRefs
        {
            public Image bg;
            public TextMeshProUGUI label;
        }

        private void BuildSettingsPopupPanel(Transform canvasRoot)
        {
            Transform old = canvasRoot.Find("ZombieSquadSettingsPanel");
            if (old != null) SafeDestroy(old.gameObject);

            // Migrate the old master "volume" switch (muted the whole AudioListener) into the new music / sfx switches
            if (PlayerPrefs.GetInt(PREF_VOLUME, 1) == 0)
            {
                PlayerPrefs.SetInt(PREF_VOLUME, 1);
                var s0 = MobControl.Audio.ZombieSoundManager.Instance;
                if (s0 != null)
                {
                    if (s0.IsMusicEnabled) s0.ToggleMusic();
                    if (s0.IsSfxEnabled) s0.ToggleSFX();
                }
            }
            AudioListener.volume = 1f;

            settingsPanel = new GameObject("ZombieSquadSettingsPanel", typeof(RectTransform));
            settingsPanel.transform.SetParent(canvasRoot, false);
            RectTransform pr = settingsPanel.GetComponent<RectTransform>();
            ShopStretch(pr);
            settingsCanvasGroup = settingsPanel.AddComponent<CanvasGroup>();

            // Dimmed backdrop: tap outside the dialog to close
            Image dim = settingsPanel.AddComponent<Image>();
            dim.color = new Color(0.01f, 0.02f, 0.07f, 0.75f);
            dim.raycastTarget = true;
            Button dimBtn = settingsPanel.AddComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.onClick.AddListener(() =>
            {
                MobControl.Audio.ZombieSoundManager.Instance?.PlayUIBack();
                HideZombieSquadSettings();
            });

            RectTransform safe = NewShopRect("SafeArea", pr);
            ShopStretch(safe);
            safe.gameObject.AddComponent<ShopSafeArea>();
            RectTransform body = NewShopRect("Body", safe);
            ShopStretch(body, 30f, 30f, 30f, 30f);

            RectTransform root = NewShopRect("Dialog", body);
            ShopResponsiveFitter fit = root.gameObject.AddComponent<ShopResponsiveFitter>();
            fit.designSize = new Vector2(820f, 960f);
            fit.maxScale = 1.2f;
            fit.Apply(true);
            settingsDialogRt = root;

            Image frame = NewShopImage("Frame", root, "Shop_Panel_Frame", false, true); // raycast: taps on the card don't close it
            frame.type = Image.Type.Sliced;
            ShopPlaceTopLeft(frame.rectTransform, 0f, 60f, 820f, 900f);

            Image banner = NewMenuImage("Banner", root, "Menu_Banner", false);
            banner.type = Image.Type.Sliced;
            ShopPlaceTopLeft(banner.rectTransform, 140f, 0f, 540f, 120f);
            TextMeshProUGUI title = CreateText("Title", banner.rectTransform, "SETTINGS", 60f, Color.white, TextAlignmentOptions.Center, true);
            title.rectTransform.offsetMin = new Vector2(10f, 8f);
            title.rectTransform.offsetMax = new Vector2(-10f, -8f);
            title.textWrappingMode = TextWrappingModes.NoWrap;
            StyleShopTmp(title, new Color(0.02f, 0.08f, 0.32f, 1f), 0.24f, true);

            // Section caption
            TextMeshProUGUI cap = CreateText("Caption_Audio", root, "AUDIO & FEEL", 30f, new Color(1f, 0.92f, 0.38f, 1f), TextAlignmentOptions.Left, true);
            ShopPlaceTopLeft(cap.rectTransform, 80f, 150f, 660f, 44f);
            cap.characterSpacing = 6f;
            StyleShopTmp(cap, new Color(0.30f, 0.12f, 0.0f, 1f), 0.24f, false);

            float y = 205f;
            settingsMusicToggle = CreateSettingsRow(root, "Row_Music", y, "MUSIC", "Menu theme & battle tracks", out settingsMusicIcon, "Menu_Btn_Music_On", () =>
            {
                MobControl.Audio.ZombieSoundManager.Instance?.ToggleMusic();
                MobControl.Audio.ZombieSoundManager.Instance?.PlayUIClick();
                UpdateSettingsVisuals();
            });
            y += 150f;
            settingsSfxToggle = CreateSettingsRow(root, "Row_Sound", y, "SOUND FX", "Guns, zombies & buttons", out settingsSfxIcon, "Menu_Btn_Sound_On", () =>
            {
                MobControl.Audio.ZombieSoundManager.Instance?.ToggleSFX();
                MobControl.Audio.ZombieSoundManager.Instance?.PlayUIClick();
                UpdateSettingsVisuals();
            });
            y += 150f;
            settingsVibToggle = CreateSettingsRow(root, "Row_Vibration", y, "VIBRATION", "Haptics on hits & deaths", out settingsVibIcon, null, () =>
            {
                ToggleVibration();
                MobControl.Audio.ZombieSoundManager.Instance?.PlayUIClick();
            });
            y += 175f;

            // DONE button
            Image done = NewMenuImage("Btn_Done", root, "Menu_Btn_Green", false, true);
            RectTransform dRt = done.rectTransform;
            ShopPlaceTopLeft(dRt, 170f, y, 480f, 128f);
            Button dBtn = done.gameObject.AddComponent<Button>();
            dBtn.targetGraphic = done;
            dBtn.transition = Selectable.Transition.None;
            dBtn.onClick.AddListener(() =>
            {
                MobControl.Audio.ZombieSoundManager.Instance?.PlayUIClick();
                StartCoroutine(ShopPress(dRt, HideZombieSquadSettings));
            });
            TextMeshProUGUI dTxt = CreateText("Label", dRt, "DONE", 52f, Color.white, TextAlignmentOptions.Center, true);
            dTxt.rectTransform.offsetMin = new Vector2(12f, 22f);
            dTxt.rectTransform.offsetMax = new Vector2(-12f, -2f);
            StyleShopTmp(dTxt, new Color(0.03f, 0.25f, 0.07f, 1f), 0.26f, false);

            // Footer
            TextMeshProUGUI ver = CreateText("Version", root, $"ZOMBIE RAGE  •  v{Application.version}", 24f, new Color(0.75f, 0.85f, 1f, 0.75f), TextAlignmentOptions.Center, true);
            ShopPlaceTopLeft(ver.rectTransform, 60f, y + 150f, 700f, 36f);
            ver.characterSpacing = 4f;

            settingsPanel.SetActive(false);
        }

        private SettingsToggleRefs CreateSettingsRow(RectTransform root, string name, float y, string label, string sub,
                                                     out Image icon, string iconSprite, System.Action onToggle)
        {
            // Row card
            RectTransform row = NewShopRect(name, root);
            ShopPlaceTopLeft(row, 60f, y, 700f, 130f);
            Image rowBg = row.gameObject.AddComponent<Image>();
            rowBg.color = new Color(0.04f, 0.10f, 0.26f, 0.55f);
            rowBg.raycastTarget = true;
            Outline ol = row.gameObject.AddComponent<Outline>();
            ol.effectColor = new Color(0.45f, 0.75f, 1f, 0.35f);
            ol.effectDistance = new Vector2(2f, -2f);

            // Icon
            if (!string.IsNullOrEmpty(iconSprite))
            {
                icon = NewMenuImage("Icon", row, iconSprite, true, false);
                ShopPlaceTopLeft(icon.rectTransform, 18f, 15f, 100f, 100f);
            }
            else
            {
                icon = BuildVibrationIcon(row);
            }

            // Labels
            TextMeshProUGUI t = CreateText("Label", row, label, 40f, Color.white, TextAlignmentOptions.Left, true);
            ShopPlaceTopLeft(t.rectTransform, 140f, 18f, 300f, 54f);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            StyleShopTmp(t, new Color(0.02f, 0.08f, 0.32f, 1f), 0.24f, false);
            TextMeshProUGUI s = CreateText("Sub", row, sub, 22f, new Color(0.78f, 0.88f, 1f, 0.85f), TextAlignmentOptions.Left, false);
            ShopPlaceTopLeft(s.rectTransform, 142f, 74f, 320f, 34f);
            s.textWrappingMode = TextWrappingModes.NoWrap;

            // ON/OFF pill (whole row is tappable too)
            Image pill = NewMenuImage("Toggle", row, "Menu_Btn_Green", false, true);
            RectTransform pRt = pill.rectTransform;
            ShopPlaceTopLeft(pRt, 470f, 24f, 210f, 82f);
            TextMeshProUGUI pTxt = CreateText("State", pRt, "ON", 38f, Color.white, TextAlignmentOptions.Center, true);
            pTxt.rectTransform.offsetMin = new Vector2(8f, 14f);
            pTxt.rectTransform.offsetMax = new Vector2(-8f, -2f);

            System.Action press = () => StartCoroutine(ShopPress(pRt, onToggle));
            Button pBtn = pill.gameObject.AddComponent<Button>();
            pBtn.targetGraphic = pill;
            pBtn.transition = Selectable.Transition.None;
            pBtn.onClick.AddListener(() => press());
            Button rowBtn = row.gameObject.AddComponent<Button>();
            rowBtn.targetGraphic = rowBg;
            rowBtn.transition = Selectable.Transition.None;
            rowBtn.onClick.AddListener(() => press());

            return new SettingsToggleRefs { bg = pill, label = pTxt };
        }

        /// <summary>Simple phone-with-buzz-lines icon built from UI rects (no sprite needed).</summary>
        private Image BuildVibrationIcon(RectTransform row)
        {
            RectTransform holder = NewShopRect("Icon", row);
            ShopPlaceTopLeft(holder, 18f, 15f, 100f, 100f);
            Image disc = holder.gameObject.AddComponent<Image>();
            Sprite discSprite = LoadSpriteFromUI(MenuHDPath + "Menu_Node_Disc");
            if (discSprite != null) { disc.sprite = discSprite; disc.preserveAspect = true; }
            disc.color = new Color(1f, 0.62f, 0.12f, 1f);
            disc.raycastTarget = false;

            RectTransform phone = NewShopRect("Phone", holder);
            ShopPlaceTopLeft(phone, 36f, 22f, 28f, 54f);
            Image ph = phone.gameObject.AddComponent<Image>();
            ph.color = Color.white; ph.raycastTarget = false;
            RectTransform screen = NewShopRect("Screen", phone);
            ShopPlaceTopLeft(screen, 4f, 6f, 20f, 36f);
            Image sc = screen.gameObject.AddComponent<Image>();
            sc.color = new Color(0.10f, 0.16f, 0.35f, 1f); sc.raycastTarget = false;
            for (int i = 0; i < 2; i++)
            {
                foreach (float side in new[] { -1f, 1f })
                {
                    RectTransform line = NewShopRect("Buzz", holder);
                    float x = 50f + side * (24f + i * 9f);
                    ShopPlaceTopLeft(line, x - 2.5f, 34f - i * 5f, 5f, 30f + i * 10f);
                    Image li = line.gameObject.AddComponent<Image>();
                    li.color = Color.white; li.raycastTarget = false;
                }
            }
            return disc;
        }

        private void SetupAmbientSceneryCover(GameObject panel, Sprite scenerySprite, float fallbackAspect)
        {
            GameObject ambObj = new GameObject("AmbientSceneryCover");
            ambObj.transform.SetParent(panel.transform, false);
            ambObj.transform.SetAsFirstSibling();

            RectTransform ambRt = ambObj.AddComponent<RectTransform>();
            ambRt.anchorMin = Vector2.zero;
            ambRt.anchorMax = Vector2.one;
            ambRt.offsetMin = Vector2.zero;
            ambRt.offsetMax = Vector2.zero;

            // Ambient background must NEVER contain baked logos, text, or modal frames (which cause ghosting/double logos!)
            // Use the specific scenerySprite if provided (e.g. Scenery_NightBattlefield_Clean), else fallback to HomeScreen_Scenery_Clean
            Sprite coverSprite = scenerySprite ?? LoadSpriteFromUI("UI/HomeScreen_Scenery_Clean", "UI/HomeScreen/HomeScreen_Scenery_Clean");

            float aspect = (coverSprite != null && coverSprite.rect.height > 0)
                ? (coverSprite.rect.width / coverSprite.rect.height)
                : (1080f / 1920f);

            AspectRatioFitter arf = ambObj.AddComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            arf.aspectRatio = aspect;

            Image img = ambObj.AddComponent<Image>();
            if (coverSprite != null) img.sprite = coverSprite;
            img.color = Color.white;
            img.preserveAspect = false; // EnvelopeParent guarantees full screen cover with zero distortion!
            img.raycastTarget = false;
        }

        private GameObject CreateModularTopHeader(Transform parent, string titleStr, System.Action onBack)
        {
            GameObject headerObj = new GameObject("TopHUD_ModularHeader");
            headerObj.transform.SetParent(parent, false);
            RectTransform hRt = headerObj.AddComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0f, 1f);
            hRt.anchorMax = new Vector2(1f, 1f);
            hRt.pivot = new Vector2(0.5f, 1f);
            hRt.anchoredPosition = new Vector2(0f, -40f);
            hRt.sizeDelta = new Vector2(0f, 95f);

            // Back button (Beveled 3D blue back button)
            Sprite backSp = LoadSpriteFromUI(
                "UI/CharactersDynamic/05_back_button",
                "UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/08_back_button_icon",
                "UI/Btn_3D_Blue"
            );
            GameObject backObj = new GameObject("Btn_Back");
            backObj.transform.SetParent(headerObj.transform, false);
            RectTransform bRt = backObj.AddComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0f, 0.5f);
            bRt.anchorMax = new Vector2(0f, 0.5f);
            bRt.pivot = new Vector2(0f, 0.5f);
            bRt.anchoredPosition = new Vector2(35f, 0f);
            bRt.sizeDelta = new Vector2(90f, 75f);

            Image bImg = backObj.AddComponent<Image>();
            if (backSp != null) bImg.sprite = backSp;
            bImg.preserveAspect = true;
            bImg.raycastTarget = true;
            Button bBtn = backObj.AddComponent<Button>();
            bBtn.targetGraphic = bImg;
            bBtn.onClick.AddListener(() =>
            {
                StartCoroutine(PunchScale(backObj.transform, 0.10f, () => onBack?.Invoke()));
            });

            if (titleStr == "CHARACTERS") charactersBackBtnRect = bRt;
            if (titleStr == "SHOP") shopBackBtnRect = bRt;

            // Title Text TMP (SHOP / CHARACTERS)
            if (!string.IsNullOrEmpty(titleStr))
            {
                GameObject titleObj = new GameObject("Title_TMP");
                titleObj.transform.SetParent(headerObj.transform, false);
                RectTransform tRt = titleObj.AddComponent<RectTransform>();
                tRt.anchorMin = new Vector2(0f, 0.5f);
                tRt.anchorMax = new Vector2(0f, 0.5f);
                tRt.pivot = new Vector2(0f, 0.5f);
                tRt.anchoredPosition = new Vector2(140f, 0f);
                tRt.sizeDelta = new Vector2(360f, 85f);

                TextMeshProUGUI tTmp = titleObj.AddComponent<TextMeshProUGUI>();
                tTmp.enabled = false;
                if (s_globalChildishFont != null) tTmp.font = s_globalChildishFont;
                tTmp.text = titleStr;
                tTmp.fontSize = 38f;
                tTmp.fontStyle = FontStyles.Bold;
                tTmp.alignment = TextAlignmentOptions.MidlineLeft;
                tTmp.color = Color.white;
                tTmp.raycastTarget = false;
                tTmp.enabled = true;

                Outline tOutline = titleObj.AddComponent<Outline>();
                tOutline.effectColor = new Color(0.01f, 0.08f, 0.28f, 1f);
                tOutline.effectDistance = new Vector2(2.5f, -2.5f);

                Shadow tShadow = titleObj.AddComponent<Shadow>();
                tShadow.effectColor = new Color(0.005f, 0.02f, 0.08f, 0.9f);
                tShadow.effectDistance = new Vector2(3f, -3f);
            }

            // Currency Pills Container with HorizontalLayoutGroup
            GameObject curGroup = new GameObject("CurrencyGroup");
            curGroup.transform.SetParent(headerObj.transform, false);
            RectTransform cgRt = curGroup.AddComponent<RectTransform>();
            cgRt.anchorMin = new Vector2(1f, 0.5f);
            cgRt.anchorMax = new Vector2(1f, 0.5f);
            cgRt.pivot = new Vector2(1f, 0.5f);
            cgRt.anchoredPosition = new Vector2(-25f, 0f);
            cgRt.sizeDelta = new Vector2(510f, 75f);

            HorizontalLayoutGroup hlg = curGroup.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 15f;
            hlg.childAlignment = TextAnchor.MiddleRight;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // 1. Coin Pill (Uses authentic 10_coin_bar with built-in coin star and plus button!)
            CreateCurrencyPill(
                curGroup.transform,
                "CoinPill",
                "UI/CharactersDynamic/10_coin_bar",
                GetPlayerCoins().ToString("N0"),
                new Color(1f, 0.96f, 0.65f, 1f),
                allCoinsDisplays,
                () =>
                {
                    ShowZombieSquadShopScreen();
                    SelectShopTab("Coins");
                }
            );

            // 2. Diamond Pill (Uses authentic 11_diamond_bar with built-in diamond gem and plus button!)
            CreateCurrencyPill(
                curGroup.transform,
                "DiamondPill",
                "UI/CharactersDynamic/11_diamond_bar",
                GetPlayerDiamonds().ToString("N0"),
                new Color(0.55f, 0.90f, 1f, 1f),
                allDiamondsDisplays,
                () =>
                {
                    ShowZombieSquadShopScreen();
                    SelectShopTab("Diamonds");
                }
            );

            return headerObj;
        }

        private GameObject CreateCurrencyPill(
            Transform parent,
            string name,
            string barSpritePath,
            string initialValue,
            Color textColor,
            List<TextMeshProUGUI> registerList,
            System.Action onPlusClick)
        {
            GameObject pillObj = new GameObject(name);
            pillObj.transform.SetParent(parent, false);
            RectTransform pRt = pillObj.AddComponent<RectTransform>();
            pRt.sizeDelta = new Vector2(240f, 72f);

            // Pill background (Authentic 10_coin_bar or 11_diamond_bar)
            Image bgImg = pillObj.AddComponent<Image>();
            Sprite barSp = LoadSpriteFromUI(barSpritePath, "UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/04_empty_currency_bar");
            if (barSp != null) bgImg.sprite = barSp;
            bgImg.preserveAspect = true;
            bgImg.raycastTarget = false;

            // Plus Button clickable overlay on right
            GameObject plusObj = new GameObject("Btn_Plus");
            plusObj.transform.SetParent(pillObj.transform, false);
            RectTransform plusRt = plusObj.AddComponent<RectTransform>();
            plusRt.anchorMin = new Vector2(0.78f, 0.08f);
            plusRt.anchorMax = new Vector2(0.97f, 0.92f);
            plusRt.offsetMin = Vector2.zero;
            plusRt.offsetMax = Vector2.zero;

            Image plusImg = plusObj.AddComponent<Image>();
            plusImg.color = new Color(1f, 1f, 1f, 0.001f); // Transparent click target over the plus icon
            plusImg.raycastTarget = true;
            Button plusBtn = plusObj.AddComponent<Button>();
            plusBtn.targetGraphic = plusImg;
            if (onPlusClick != null)
            {
                plusBtn.onClick.AddListener(() =>
                {
                    StartCoroutine(PunchScale(pillObj.transform, 0.10f, () => onPlusClick()));
                });
            }

            // Dynamic TMP Text precisely centered in middle capsule slot with plenty of breathing room
            GameObject txtObj = new GameObject("ValueText");
            txtObj.transform.SetParent(pillObj.transform, false);
            RectTransform tRt = txtObj.AddComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.32f, 0f);
            tRt.anchorMax = new Vector2(0.72f, 1f);
            tRt.offsetMin = Vector2.zero;
            tRt.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.enabled = false;
            if (s_globalChildishFont != null) tmp.font = s_globalChildishFont;
            tmp.text = initialValue;
            tmp.fontSize = 24f;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 15f;
            tmp.fontSizeMax = 26f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = textColor;
            tmp.raycastTarget = false;
            tmp.enabled = true;

            Outline outline = txtObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.01f, 0.05f, 0.16f, 1f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            if (registerList != null) registerList.Add(tmp);

            return pillObj;
        }

        private void CreateTopCurrencyHeader(Transform parent, string titleStr, System.Action onBack, float yMin = 0.765f, float yMax = 0.812f)
        {
            CreateModularTopHeader(parent, titleStr, onBack);
        }
        #endregion

        #region Zombie Squad Characters Screen
        // ===================== RESPONSIVE HD CHARACTERS SCREEN =====================
        private readonly Dictionary<string, Image> charCardPriceIcons = new Dictionary<string, Image>();
        private Image charPreviewEquipBtnIcon;

        private void BuildZombieSquadCharactersScreenPanel(Transform canvasRoot)
        {
            if (charactersPanel != null) SafeDestroy(charactersPanel);
            Transform oldChars = canvasRoot.Find("ZombieSquadCharactersPanel");
            if (oldChars != null) SafeDestroy(oldChars.gameObject);

            charactersPanel = CreateFullPanel("ZombieSquadCharactersPanel", canvasRoot, new Color(0.01f, 0.03f, 0.08f, 1f));
            RectTransform root = BuildHDScreenScaffold(charactersPanel, "CHARACTERS", () => ShowZombieSquadHomeScreen(), out charactersBackBtnRect);

            // Mascot + logo (soldier sits behind the showcase so its cut base is hidden)
            Image soldierImg = NewShopImage("HeaderSoldier", root, "Shop_Soldier");
            ShopPlaceTopLeft(soldierImg.rectTransform, 160f, 2f, 230f, 157f);

            // Showcase chamber
            const float scX = 40f, scY = 140f, scW = 920f, scH = 540f;
            Image scImg = NewMenuImage("ShowcaseChamber", root, "Menu_Showcase_Frame", false);
            ShopPlaceTopLeft(scImg.rectTransform, scX, scY, scW, scH);
            RectTransform scRt = scImg.rectTransform;

            Image logoImg = NewShopImage("ZombieSquadLogo", root, "Shop_Logo");
            ShopPlaceTopLeft(logoImg.rectTransform, 350f, 0f, 360f, 190f);

            charPreviewNameText = CreateText("HeroName", scRt, "SOLDIER", 52f, Color.white, TextAlignmentOptions.Center, true);
            ShopPlaceTopLeft(charPreviewNameText.rectTransform, 120f, 64f, scW - 240f, 66f);
            charPreviewNameText.textWrappingMode = TextWrappingModes.NoWrap;
            StyleShopTmp(charPreviewNameText, new Color(0.02f, 0.08f, 0.32f, 1f), 0.24f, true);

            charPreviewRoleText = CreateText("HeroRole", scRt, "ASSAULT SPECIALIST", 28f, new Color(0.45f, 0.93f, 1f, 1f), TextAlignmentOptions.Center, true);
            ShopPlaceTopLeft(charPreviewRoleText.rectTransform, 120f, 128f, scW - 240f, 40f);
            charPreviewRoleText.textWrappingMode = TextWrappingModes.NoWrap;
            StyleShopTmp(charPreviewRoleText, new Color(0.01f, 0.06f, 0.25f, 1f), 0.25f, false);

            Image pedImg = NewShopRect("GlowPedestal", scRt).gameObject.AddComponent<Image>();
            Sprite pedSp = LoadSpriteFromUI("UI/CharactersDynamic/14_glow_platform_clean", "UI/CharactersDynamic/14_glow_platform");
            if (pedSp != null) pedImg.sprite = pedSp;
            pedImg.preserveAspect = true;
            pedImg.raycastTarget = false;
            ShopPlaceTopLeft(pedImg.rectTransform, (scW - 470f) * 0.5f, scH - 160f, 470f, 138f);

            // Hero stands on the pedestal, always below the name/role text (no overlap)
            RectTransform avRt = NewShopRect("HeroAvatar", scRt);
            // Tight-cropped hero sprite, height-fitted and horizontally centred; feet land on the pedestal centre
            ShopPlaceBottomCenter(avRt, (scW - 460f) * 0.5f, 168f, 460f, scH - 168f - 84f);
            charPreviewHeroImage = avRt.gameObject.AddComponent<Image>();
            charPreviewHeroImage.preserveAspect = true;
            charPreviewHeroImage.raycastTarget = false;

            CreateMenuArrow(scRt, "Btn_Prev_Hero", "Menu_Arrow_Left", 30f, scH * 0.55f, () => CycleHero(-1));
            CreateMenuArrow(scRt, "Btn_Next_Hero", "Menu_Arrow_Right", scW - 30f - 96f, scH * 0.55f, () => CycleHero(1));

            // Equip / unlock action button
            charPreviewEquipBtnBg = NewMenuImage("Btn_EquipHero", root, "Menu_Btn_Green", false, true);
            RectTransform abRt = charPreviewEquipBtnBg.rectTransform;
            ShopPlaceTopLeft(abRt, 270f, scY + scH + 18f, 460f, 128f);
            abRt.pivot = new Vector2(0.5f, 0.5f);
            abRt.anchoredPosition = new Vector2(500f, -(scY + scH + 18f + 64f));
            charPreviewEquipBtn = charPreviewEquipBtnBg.gameObject.AddComponent<Button>();
            charPreviewEquipBtn.targetGraphic = charPreviewEquipBtnBg;
            charPreviewEquipBtn.transition = Selectable.Transition.None;
            charPreviewEquipBtn.onClick.AddListener(() => StartCoroutine(ShopPress(abRt, () => EquipSelectedCharacter())));
            BuildIconLabelRow(abRt, 52f, 50f, out charPreviewEquipBtnIcon, out charPreviewEquipBtnText, 20f);

            // Hero cards: 3 + 2 rows
            charCardPriceTexts.Clear();
            charCardPriceBgs.Clear();
            charCardSelectionBorders.Clear();
            charCardPriceIcons.Clear();

            const float cw = 280f, ch = 300f, gap = 30f;
            float row1Y = scY + scH + 166f;
            float row2Y = row1Y + ch + gap;
            float row1X = (ShopDesignSize.x - (cw * 3f + gap * 2f)) * 0.5f;
            float row2X = (ShopDesignSize.x - (cw * 2f + gap)) * 0.5f;
            string[] keys = { "Soldier", "Ranger", "Heavy", "Sniper", "Medic" };
            for (int i = 0; i < keys.Length; i++)
            {
                float x = i < 3 ? row1X + i * (cw + gap) : row2X + (i - 3) * (cw + gap);
                float y = i < 3 ? row1Y : row2Y;
                CreateHDHeroCard(root, keys[i], x, y, cw, ch);
            }

            UpdateCharactersScreenVisuals();
        }

        private void CreateMenuArrow(RectTransform parent, string name, string sprite, float x, float centerY, System.Action onClick)
        {
            Image img = NewMenuImage(name, parent, sprite, true, true);
            RectTransform rt = img.rectTransform;
            ShopPlaceTopLeft(rt, x, centerY - 64f, 96f, 128f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x + 48f, -centerY);
            Button b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => StartCoroutine(ShopPress(rt, onClick)));
        }

        // Centered [icon][label] row on a 3D button (the button face is the top ~80% of the art).
        private void BuildIconLabelRow(RectTransform btnRt, float iconSize, float fontSize, out Image icon, out TextMeshProUGUI label, float bottomInset)
        {
            RectTransform rowRt = NewShopRect("Row", btnRt);
            ShopStretch(rowRt, 12f, bottomInset, 12f, 2f);
            HorizontalLayoutGroup hlg = rowRt.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 10f;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            icon = NewShopRect("Icon", rowRt).gameObject.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            LayoutElement ile = icon.gameObject.AddComponent<LayoutElement>();
            ile.preferredWidth = iconSize;
            ile.preferredHeight = iconSize;

            label = CreateText("Label", rowRt, "", fontSize, Color.white, TextAlignmentOptions.Center, true);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            StyleShopTmp(label, new Color(0.03f, 0.18f, 0.06f, 1f), 0.26f, false);
            LayoutElement lle = label.gameObject.AddComponent<LayoutElement>();
            lle.preferredHeight = fontSize * 1.3f;
        }

        private void CreateHDHeroCard(RectTransform parent, string heroKey, float x, float y, float w, float h)
        {
            Image cardImg = NewMenuImage("Card_" + heroKey, parent, "Menu_Card_Frame", false, true);
            RectTransform cRt = cardImg.rectTransform;
            ShopPlaceTopLeft(cRt, x, y, w, h);
            cRt.pivot = new Vector2(0.5f, 0.5f);
            cRt.anchoredPosition = new Vector2(x + w * 0.5f, -(y + h * 0.5f));
            Button cardBtn = cardImg.gameObject.AddComponent<Button>();
            cardBtn.targetGraphic = cardImg;
            cardBtn.transition = Selectable.Transition.None;
            cardBtn.onClick.AddListener(() => StartCoroutine(ShopPress(cRt, () => SelectCharacter(heroKey))));

            // Selected state: green glowing frame drawn over the base frame
            Image sel = NewMenuImage("SelectionBorder", cRt, "Menu_Card_Frame_Selected", false);
            ShopStretch(sel.rectTransform, -4f, -4f, -4f, -4f);
            charCardSelectionBorders[heroKey] = sel.gameObject;
            sel.gameObject.SetActive(heroKey == selectedCharacterKey);

            Image port = NewMenuImage("Portrait", cRt, "Menu_Portrait_" + heroKey);
            ShopPlaceTopLeft(port.rectTransform, 22f, 20f, w - 44f, 196f);

            Image pill = NewMenuImage("PricePill", cRt, "Menu_Btn_Gold", false);
            ShopPlaceTopLeft(pill.rectTransform, 16f, h - 22f - 70f, w - 32f, 70f);
            charCardPriceBgs[heroKey] = pill;
            BuildIconLabelRow(pill.rectTransform, 34f, 30f, out Image icon, out TextMeshProUGUI txt, 12f);
            charCardPriceIcons[heroKey] = icon;
            charCardPriceTexts[heroKey] = txt;
        }

        public void CycleHero(int direction)
        {
            string[] heroKeys = new string[] { "Soldier", "Ranger", "Heavy", "Sniper", "Medic" };
            int idx = System.Array.IndexOf(heroKeys, selectedCharacterKey);
            if (idx < 0) idx = 0;
            idx = (idx + direction + heroKeys.Length) % heroKeys.Length;
            SelectCharacter(heroKeys[idx]);
        }

        public void SelectCharacter(string heroKey)
        {
            selectedCharacterKey = heroKey;
            Debug.Log($"[ZombieSquad] Selected Character: {heroKey}");
            if (charPreviewHeroImage != null)
            {
                StartCoroutine(PunchScale(charPreviewHeroImage.transform, 0.15f, null));
            }
            UpdateCharactersScreenVisuals();
        }

        public void EquipSelectedCharacter()
        {
            if (string.IsNullOrEmpty(selectedCharacterKey)) selectedCharacterKey = "Soldier";
            bool isUnlocked = selectedCharacterKey == "Soldier" || PlayerPrefs.GetInt("HeroUnlocked_" + selectedCharacterKey, 0) == 1;

            if (!isUnlocked && heroes.TryGetValue(selectedCharacterKey, out HeroData hData))
            {
                if (hData.priceCoins > 0)
                {
                    if (GetPlayerCoins() >= hData.priceCoins)
                    {
                        AddPlayerCoins(-hData.priceCoins);
                        PlayerPrefs.SetInt("HeroUnlocked_" + selectedCharacterKey, 1);
                        PlayerPrefs.Save();
                        Debug.Log($"[ZombieSquad] Unlocked {selectedCharacterKey} for {hData.priceCoins} coins!");
                    }
                    else
                    {
                        Debug.Log($"[ZombieSquad] Need {hData.priceCoins - GetPlayerCoins()} more coins!");
                        return;
                    }
                }
                else if (hData.priceDiamonds > 0)
                {
                    if (GetPlayerDiamonds() >= hData.priceDiamonds)
                    {
                        AddPlayerDiamonds(-hData.priceDiamonds);
                        PlayerPrefs.SetInt("HeroUnlocked_" + selectedCharacterKey, 1);
                        PlayerPrefs.Save();
                        Debug.Log($"[ZombieSquad] Unlocked {selectedCharacterKey} for {hData.priceDiamonds} diamonds!");
                    }
                    else
                    {
                        Debug.Log($"[ZombieSquad] Need {hData.priceDiamonds - GetPlayerDiamonds()} more diamonds!");
                        return;
                    }
                }
            }

            PlayerPrefs.SetString("ZombieSquad_SelectedHero", selectedCharacterKey);
            PlayerPrefs.Save();
            if (playerRunner != null) playerRunner.ApplyEquippedHeroStats();
            Debug.Log($"[ZombieSquad] Equipped Hero: {selectedCharacterKey}");
            UpdateCharactersScreenVisuals();
        }

        public void UpdateCharactersScreenVisuals()
        {
            if (string.IsNullOrEmpty(selectedCharacterKey)) selectedCharacterKey = "Soldier";
            string currentEquipped = PlayerPrefs.GetString("ZombieSquad_SelectedHero", "Soldier");

            // 1. Showcase hero + name/role
            if (charPreviewHeroImage != null)
            {
                Sprite charSpr = LoadSpriteFromUI(MenuHDPath + "Menu_Hero_" + selectedCharacterKey)
                              ?? LoadSpriteFromFile($"Resources/UI/Hero_{selectedCharacterKey}_Full.png")
                              ?? Resources.Load<Sprite>($"UI/Hero_{selectedCharacterKey}_Full")
                              ?? LoadSpriteFromFile($"Resources/UI/Hero_{selectedCharacterKey}_Icon.png")
                              ?? Resources.Load<Sprite>($"UI/Hero_{selectedCharacterKey}_Icon");
                if (charSpr != null) charPreviewHeroImage.sprite = charSpr;
            }
            if (heroes.TryGetValue(selectedCharacterKey, out HeroData selData))
            {
                if (charPreviewNameText != null) charPreviewNameText.text = selData.name.ToUpper();
                if (charPreviewRoleText != null) charPreviewRoleText.text = selData.role.ToUpper();
            }

            foreach (var kvp in charCardSelectionBorders)
            {
                if (kvp.Value != null) kvp.Value.SetActive(kvp.Key == selectedCharacterKey);
            }

            // 2. Main action button
            bool selUnlocked = selectedCharacterKey == "Soldier" || PlayerPrefs.GetInt("HeroUnlocked_" + selectedCharacterKey, 0) == 1;
            bool selEquipped = selectedCharacterKey == currentEquipped;
            ApplyHeroButtonState(charPreviewEquipBtnBg, charPreviewEquipBtnIcon, charPreviewEquipBtnText,
                selectedCharacterKey, selEquipped, selUnlocked, true);

            // 3. Card pills
            foreach (var kvp in heroes)
            {
                string hKey = kvp.Key;
                if (!charCardPriceTexts.TryGetValue(hKey, out TextMeshProUGUI txt) || txt == null) continue;
                charCardPriceBgs.TryGetValue(hKey, out Image bg);
                charCardPriceIcons.TryGetValue(hKey, out Image icon);
                bool unlocked = hKey == "Soldier" || PlayerPrefs.GetInt("HeroUnlocked_" + hKey, 0) == 1;
                ApplyHeroButtonState(bg, icon, txt, hKey, hKey == currentEquipped, unlocked, false);
            }
        }

        private void ApplyHeroButtonState(Image bg, Image icon, TextMeshProUGUI txt, string heroKey, bool equipped, bool unlocked, bool isMainButton)
        {
            if (txt == null) return;
            string btnSprite;
            string iconSprite = null;
            string label;
            Color outline;

            if (equipped)
            {
                btnSprite = "Menu_Btn_Green"; iconSprite = "Menu_Icon_Check"; label = "EQUIPPED";
                outline = new Color(0.03f, 0.25f, 0.07f, 1f);
            }
            else if (unlocked)
            {
                btnSprite = "Menu_Btn_Blue"; label = isMainButton ? "EQUIP HERO" : "EQUIP";
                outline = new Color(0.03f, 0.10f, 0.35f, 1f);
            }
            else if (heroes.TryGetValue(heroKey, out HeroData hd) && hd.priceCoins > 0)
            {
                btnSprite = "Menu_Btn_Gold"; iconSprite = "Shop_CoinIcon";
                label = isMainButton ? $"UNLOCK  {hd.priceCoins:N0}" : hd.priceCoins.ToString("N0");
                outline = new Color(0.40f, 0.18f, 0.02f, 1f);
            }
            else
            {
                int gems = heroes.TryGetValue(heroKey, out HeroData gd) ? gd.priceDiamonds : 0;
                btnSprite = "Menu_Btn_Cyan"; iconSprite = "Shop_DiamondIcon";
                label = isMainButton ? $"UNLOCK  {gems:N0}" : gems.ToString("N0");
                outline = new Color(0.02f, 0.18f, 0.40f, 1f);
            }

            SetMenuSprite(bg, btnSprite);
            if (bg != null) bg.color = Color.white;
            if (icon != null)
            {
                icon.gameObject.SetActive(iconSprite != null);
                if (iconSprite != null) SetMenuSprite(icon, iconSprite);
            }
            txt.text = label;
            txt.color = Color.white;
            txt.outlineColor = outline;
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

        // ===================== RESPONSIVE HD SQUAD UPGRADE SCREEN =====================
        private static readonly string[] s_upgKeys = { "Count", "Health", "Damage", "Speed" };
        private static readonly string[] s_upgLabels = { "SOLDIER COUNT", "SOLDIER HEALTH", "SOLDIER DAMAGE", "SOLDIER SPEED" };
        private static readonly string[] s_upgIcons = { "Menu_Upg_count", "Menu_Upg_health", "Menu_Upg_damage", "Menu_Upg_speed" };
        private static readonly int[] s_upgBaseCosts = { 200, 150, 180, 5 };
        private static readonly bool[] s_upgIsDiamonds = { false, false, false, true };
        private readonly Image[] upgradeStatBtnBgs = new Image[4];
        private readonly Image[] upgradeStatCostIcons = new Image[4];

        private void BuildZombieSquadUpgradeScreenPanel(Transform canvasRoot)
        {
            upgradePanel = CreateFullPanel("ZombieSquadUpgradePanel", canvasRoot, new Color(0.01f, 0.03f, 0.08f, 1f));
            RectTransform root = BuildHDScreenScaffold(upgradePanel, "SQUAD UPGRADE", () => ShowZombieSquadHomeScreen(), out RectTransform _);

            const float cw = 450f, ch = 640f, gx = 40f, gy = 40f;
            float x0 = (ShopDesignSize.x - (cw * 2f + gx)) * 0.5f;
            float y0 = (ShopDesignSize.y - (ch * 2f + gy)) * 0.5f - 40f;
            for (int i = 0; i < 4; i++)
            {
                float x = x0 + (i % 2) * (cw + gx);
                float y = y0 + (i / 2) * (ch + gy);
                CreateHDUpgradeCard(root, i, x, y, cw, ch);
            }

            UpdateUpgradeScreenVisuals();
        }

        private void CreateHDUpgradeCard(RectTransform parent, int idx, float x, float y, float w, float h)
        {
            string key = s_upgKeys[idx];
            Image frame = NewShopImage("UpgradeCard_" + key, parent, "Shop_Card_Frame", false);
            ShopPlaceTopLeft(frame.rectTransform, x, y, w, h);
            RectTransform cRt = frame.rectTransform;

            Image icon = NewMenuImage("Icon", cRt, s_upgIcons[idx]);
            ShopPlaceTopLeft(icon.rectTransform, 45f, 36f, w - 90f, 262f);

            TextMeshProUGUI title = CreateText("Title", cRt, s_upgLabels[idx], 44f, Color.white, TextAlignmentOptions.Center, true);
            ShopPlaceTopLeft(title.rectTransform, 20f, 310f, w - 40f, 64f);
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.enableAutoSizing = true;
            title.fontSizeMin = 28f;
            title.fontSizeMax = 44f;
            StyleShopTmp(title, new Color(0.02f, 0.08f, 0.32f, 1f), 0.24f, true);

            Image track = NewMenuImage("Upg_Bar_" + key, cRt, "Menu_Bar_Track", false);
            track.type = Image.Type.Sliced;
            ShopPlaceTopLeft(track.rectTransform, 40f, 392f, w - 80f, 56f);
            RectTransform fillRt = NewShopRect("Fill", track.rectTransform);
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(0.1f, 1f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            Image fill = fillRt.gameObject.AddComponent<Image>();
            Sprite fillSp = LoadSpriteFromUI(MenuHDPath + "Menu_Bar_Fill");
            if (fillSp != null) fill.sprite = fillSp;
            fill.type = Image.Type.Sliced;
            fill.raycastTarget = false;
            upgradeStatFills[idx] = fill;

            upgradeStatLevelTexts[idx] = CreateText("LevelTxt", track.rectTransform, "LEVEL 1 / 10", 28f, Color.white, TextAlignmentOptions.Center, true);
            upgradeStatLevelTexts[idx].textWrappingMode = TextWrappingModes.NoWrap;
            StyleShopTmp(upgradeStatLevelTexts[idx], new Color(0.01f, 0.05f, 0.18f, 1f), 0.3f, false);

            Image btn = NewMenuImage("Btn_Upg_" + key, cRt, "Menu_Btn_Green", false, true);
            RectTransform bRt = btn.rectTransform;
            ShopPlaceTopLeft(bRt, 34f, h - 40f - 124f, w - 68f, 124f);
            Button b = btn.gameObject.AddComponent<Button>();
            b.targetGraphic = btn;
            b.transition = Selectable.Transition.None;
            int capturedIdx = idx;
            b.onClick.AddListener(() => StartCoroutine(ShopPress(bRt, () =>
                UpgradeSquadStat(s_upgKeys[capturedIdx], capturedIdx, s_upgBaseCosts[capturedIdx], s_upgIsDiamonds[capturedIdx]))));
            upgradeStatBtnBgs[idx] = btn;

            BuildIconLabelRow(bRt, 54f, 48f, out Image costIcon, out TextMeshProUGUI costTxt, 22f);
            SetMenuSprite(costIcon, s_upgIsDiamonds[idx] ? "Shop_DiamondIcon" : "Shop_CoinIcon");
            upgradeStatCostIcons[idx] = costIcon;
            upgradeStatCostTexts[idx] = costTxt;
        }

        public void UpgradeSquadStat(string key, int idx, int baseCost, bool isDiamonds)
        {
            string prefKey = $"ZombieSquad_Upgrade_{key}";
            int currentLevel = PlayerPrefs.GetInt(prefKey, 1);
            if (currentLevel >= 10)
            {
                Debug.Log($"[ZombieSquad] {key} is already max level!");
                return;
            }

            int cost = baseCost * currentLevel;
            bool success = isDiamonds ? SpendPlayerDiamonds(cost) : SpendPlayerCoins(cost);
            if (success)
            {
                currentLevel++;
                PlayerPrefs.SetInt(prefKey, currentLevel);
                PlayerPrefs.Save();
                Debug.Log($"[ZombieSquad] Upgraded {key} to Level {currentLevel}!");
                UpdateAllCurrencyDisplays();
                UpdateUpgradeScreenVisuals();

                if (MobControl.Audio.ZombieSoundManager.Instance != null)
                {
                    if (isDiamonds) MobControl.Audio.ZombieSoundManager.Instance.PlayDiamondSpend();
                    else MobControl.Audio.ZombieSoundManager.Instance.PlayCoinSpend();
                    MobControl.Audio.ZombieSoundManager.Instance.PlayUpgradeSuccess();
                }
            }
            else
            {
                Debug.LogWarning($"[ZombieSquad] Not enough currency to upgrade {key}!");
            }
        }

        public void UpdateUpgradeScreenVisuals()
        {
            int coins = GetPlayerCoins();
            int gems = GetPlayerDiamonds();
            for (int i = 0; i < 4; i++)
            {
                int lvl = PlayerPrefs.GetInt($"ZombieSquad_Upgrade_{s_upgKeys[i]}", 1);
                bool max = lvl >= 10;
                int cost = s_upgBaseCosts[i] * lvl;
                bool affordable = s_upgIsDiamonds[i] ? gems >= cost : coins >= cost;

                if (upgradeStatFills[i] != null)
                    upgradeStatFills[i].rectTransform.anchorMax = new Vector2(Mathf.Max(0.1f, Mathf.Clamp01(lvl / 10f)), 1f);
                if (upgradeStatLevelTexts[i] != null)
                    upgradeStatLevelTexts[i].text = max ? "MAX LEVEL" : $"LEVEL {lvl} / 10";
                if (upgradeStatCostTexts[i] != null)
                    upgradeStatCostTexts[i].text = max ? "MAX" : cost.ToString("N0");
                if (upgradeStatCostIcons[i] != null)
                    upgradeStatCostIcons[i].gameObject.SetActive(!max);
                if (upgradeStatBtnBgs[i] != null)
                {
                    // Grey when maxed; slightly dimmed when the player can't afford it yet
                    SetMenuSprite(upgradeStatBtnBgs[i], max ? "Menu_Btn_Grey" : "Menu_Btn_Green");
                    upgradeStatBtnBgs[i].color = (!max && !affordable) ? new Color(0.78f, 0.78f, 0.78f, 1f) : Color.white;
                }
            }
        }
        #endregion

        #region Zombie Squad Weapons Screen

        private Sprite LoadZombieSquadWeaponsScreenSprite()
        {
            try
            {
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
                        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        if (tex.LoadImage(fileData))
                        {
                            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                        }
                    }
                }

                Sprite s = Resources.Load<Sprite>("UI/ZombieSquad_WeaponsScreen");
                if (s != null) return s;

                Texture2D resTex = Resources.Load<Texture2D>("UI/ZombieSquad_WeaponsScreen");
                if (resTex != null)
                {
                    return Sprite.Create(resTex, new Rect(0, 0, resTex.width, resTex.height), new Vector2(0.5f, 0.5f));
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[ZombieRunnerUIManager] WeaponsScreen sprite loading: " + ex.Message);
            }
            return null;
        }

        private static readonly string[] s_weaponKeys = new string[] { "AssaultRifle", "Shotgun", "Sniper", "Minigun" };

        private Sprite LoadWeaponSprite(string weaponKey)
        {
            try
            {
                string path = System.IO.Path.Combine(Application.dataPath, "Resources", "UI", $"Weapon_{weaponKey}.png");
                if (System.IO.File.Exists(path))
                {
                    byte[] bytes = System.IO.File.ReadAllBytes(path);
                    Texture2D directTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (directTex.LoadImage(bytes))
                    {
                        return Sprite.Create(directTex, new Rect(0, 0, directTex.width, directTex.height), new Vector2(0.5f, 0.5f));
                    }
                }

                Sprite s = Resources.Load<Sprite>($"UI/Weapon_{weaponKey}");
                if (s != null) return s;
                Texture2D tex = Resources.Load<Texture2D>($"UI/Weapon_{weaponKey}");
                if (tex != null)
                {
                    return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ZombieRunnerUIManager] LoadWeaponSprite({weaponKey}) error: " + ex.Message);
            }
            return null;
        }


        // ===================== RESPONSIVE HD WEAPONS SCREEN =====================
        private readonly Dictionary<string, GameObject> weaponCardSelectedOverlays = new Dictionary<string, GameObject>();
        private readonly Image[] weaponStatUpgradeBgs = new Image[4];
        private readonly Image[] weaponStatCostIcons = new Image[4];

        private static string WeaponDisplayName(string key)
        {
            switch (key)
            {
                case "AssaultRifle": return "ASSAULT RIFLE";
                case "Shotgun": return "SHOTGUN";
                case "Sniper": return "SNIPER";
                case "Minigun": return "MINIGUN";
            }
            return key.ToUpper();
        }

        private Sprite LoadWeaponSpriteHD(string key)
        {
            return LoadSpriteFromUI(MenuHDPath + "Menu_Weapon_" + key) ?? LoadWeaponSprite(key);
        }

        private void BuildZombieSquadWeaponsScreenPanel(Transform canvasRoot)
        {
            Transform oldWep = canvasRoot.Find("ZombieSquadWeaponsPanel");
            if (oldWep != null && (weaponsPanel == null || oldWep.gameObject != weaponsPanel)) SafeDestroy(oldWep.gameObject);
            if (weaponsPanel != null) SafeDestroy(weaponsPanel);

            weaponsPanel = CreateFullPanel("ZombieSquadWeaponsPanel", canvasRoot, new Color(0.01f, 0.03f, 0.08f, 1f));
            RectTransform root = BuildHDScreenScaffold(weaponsPanel, "WEAPONS", () => ShowZombieSquadHomeScreen(), out weaponsBackBtnRect);
            weaponCardSelectionHighlightRt = null;

            // 1. Showcase chamber with weapon on glowing pedestal
            const float scX = 40f, scY = 0f, scW = 920f, scH = 460f;
            Image scImg = NewMenuImage("WeaponShowcase", root, "Menu_Showcase_Frame", false);
            ShopPlaceTopLeft(scImg.rectTransform, scX, scY, scW, scH);
            RectTransform scRt = scImg.rectTransform;

            Image pedImg = NewShopRect("GlowPedestal", scRt).gameObject.AddComponent<Image>();
            Sprite pedSp = LoadSpriteFromUI("UI/CharactersDynamic/14_glow_platform_clean", "UI/CharactersDynamic/14_glow_platform");
            if (pedSp != null) pedImg.sprite = pedSp;
            pedImg.preserveAspect = true;
            pedImg.raycastTarget = false;
            ShopPlaceTopLeft(pedImg.rectTransform, (scW - 500f) * 0.5f, scH - 150f, 500f, 130f);

            RectTransform wRt = NewShopRect("ChamberWeaponDisplay", scRt);
            ShopPlaceTopLeft(wRt, (scW - 600f) * 0.5f, 40f, 600f, scH - 150f);
            weaponChamberImg = wRt.gameObject.AddComponent<Image>();
            weaponChamberImg.preserveAspect = true;
            weaponChamberImg.raycastTarget = false;

            CreateMenuArrow(scRt, "Btn_Prev_Weapon", "Menu_Arrow_Left", 30f, scH * 0.5f, () => CycleWeapon(-1));
            CreateMenuArrow(scRt, "Btn_Next_Weapon", "Menu_Arrow_Right", scW - 30f - 96f, scH * 0.5f, () => CycleWeapon(1));

            // 2. Title banner: name + level badge
            Image banner = NewMenuImage("TitleBanner", root, "Menu_Banner", false);
            banner.type = Image.Type.Sliced;
            ShopPlaceTopLeft(banner.rectTransform, scX, scY + scH + 20f, scW, 110f);
            weaponTitleText = CreateText("WepTitle", banner.rectTransform, "ASSAULT RIFLE", 50f, Color.white, TextAlignmentOptions.Left, true);
            ShopPlaceTopLeft(weaponTitleText.rectTransform, 40f, 12f, 560f, 86f);
            weaponTitleText.textWrappingMode = TextWrappingModes.NoWrap;
            weaponTitleText.enableAutoSizing = true;
            weaponTitleText.fontSizeMin = 30f;
            weaponTitleText.fontSizeMax = 50f;
            StyleShopTmp(weaponTitleText, new Color(0.02f, 0.08f, 0.32f, 1f), 0.24f, true);

            Image lvBadge = NewMenuImage("LevelBadge", banner.rectTransform, "Menu_Btn_Gold", false);
            ShopPlaceTopLeft(lvBadge.rectTransform, scW - 40f - 210f, 16f, 210f, 80f);
            weaponLevelText = CreateText("WepLevel", lvBadge.rectTransform, "LV. 1", 38f, Color.white, TextAlignmentOptions.Center, true);
            weaponLevelText.rectTransform.offsetMin = new Vector2(8f, 12f);
            weaponLevelText.rectTransform.offsetMax = new Vector2(-8f, -2f);
            weaponLevelText.textWrappingMode = TextWrappingModes.NoWrap;
            StyleShopTmp(weaponLevelText, new Color(0.40f, 0.18f, 0.02f, 1f), 0.26f, false);

            // 3. Stats panel (4 upgrade rows)
            const float stY = scY + scH + 150f, stH = 540f;
            Image statsPanel = NewShopImage("StatsPanel", root, "Shop_Panel_Frame", false);
            statsPanel.type = Image.Type.Sliced;
            ShopPlaceTopLeft(statsPanel.rectTransform, scX, stY, scW, stH);
            RectTransform spRt = statsPanel.rectTransform;

            string[] statNames = { "Damage", "FireRate", "Range", "Magazine" };
            string[] statLabels = { "DAMAGE", "FIRE RATE", "RANGE", "MAGAZINE" };
            string[] statIcons = { "Menu_Stat_damage", "Menu_Stat_firerate", "Menu_Stat_range", "Menu_Stat_magazine" };
            const float rowH = 118f, rowTop = 34f;
            for (int i = 0; i < 4; i++)
            {
                string sName = statNames[i];
                float ry = rowTop + i * rowH;

                Image ic = NewMenuImage("Icon_" + sName, spRt, statIcons[i]);
                ShopPlaceTopLeft(ic.rectTransform, 40f, ry + 22f, 74f, 74f);

                TextMeshProUGUI lbl = CreateText("Label_" + sName, spRt, statLabels[i], 34f, Color.white, TextAlignmentOptions.Left, true);
                ShopPlaceTopLeft(lbl.rectTransform, 134f, ry + 6f, 300f, 48f);
                lbl.textWrappingMode = TextWrappingModes.NoWrap;
                StyleShopTmp(lbl, new Color(0.02f, 0.08f, 0.32f, 1f), 0.22f, false);

                Image track = NewMenuImage("Wep_StatBar_" + sName, spRt, "Menu_Bar_Track", false);
                track.type = Image.Type.Sliced;
                ShopPlaceTopLeft(track.rectTransform, 134f, ry + 58f, 460f, 44f);

                RectTransform fillRt = NewShopRect("Fill", track.rectTransform);
                fillRt.anchorMin = Vector2.zero;
                fillRt.anchorMax = new Vector2(0.15f, 1f);
                fillRt.offsetMin = Vector2.zero;
                fillRt.offsetMax = Vector2.zero;
                Image fill = fillRt.gameObject.AddComponent<Image>();
                Sprite fillSp = LoadSpriteFromUI(MenuHDPath + "Menu_Bar_Fill");
                if (fillSp != null) fill.sprite = fillSp;
                fill.type = Image.Type.Sliced;
                fill.raycastTarget = false;
                weaponStatFills[i] = fill;

                weaponStatValTexts[i] = CreateText("ValTxt", track.rectTransform, "LV. 1 / 10", 26f, Color.white, TextAlignmentOptions.Center, true);
                weaponStatValTexts[i].textWrappingMode = TextWrappingModes.NoWrap;
                StyleShopTmp(weaponStatValTexts[i], new Color(0.01f, 0.05f, 0.18f, 1f), 0.3f, false);

                // Upgrade button: [up arrow] cost [coin]
                Image up = NewMenuImage("Btn_Upg_" + sName, spRt, "Menu_Btn_Green", false, true);
                RectTransform upRt = up.rectTransform;
                ShopPlaceTopLeft(upRt, 620f, ry + 14f, 260f, 92f);
                upRt.pivot = new Vector2(0.5f, 0.5f);
                upRt.anchoredPosition = new Vector2(620f + 130f, -(ry + 14f + 46f));
                Button upBtn = up.gameObject.AddComponent<Button>();
                upBtn.targetGraphic = up;
                upBtn.transition = Selectable.Transition.None;
                upBtn.onClick.AddListener(() => StartCoroutine(ShopPress(upRt, () => UpgradeWeaponStat(sName))));
                weaponStatUpgradeBgs[i] = up;

                RectTransform rowRt = NewShopRect("Row", upRt);
                ShopStretch(rowRt, 10f, 16f, 10f, 2f);
                HorizontalLayoutGroup hlg = rowRt.gameObject.AddComponent<HorizontalLayoutGroup>();
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.spacing = 8f;
                hlg.childControlWidth = true;
                hlg.childControlHeight = true;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = false;

                Image coin = NewShopImage("CoinIcon", rowRt, "Shop_CoinIcon");
                LayoutElement cle = coin.gameObject.AddComponent<LayoutElement>();
                cle.preferredWidth = 40f; cle.preferredHeight = 40f;
                weaponStatCostIcons[i] = coin;

                weaponStatCostTexts[i] = CreateText("CostTxt", rowRt, "100", 36f, Color.white, TextAlignmentOptions.Center, true);
                weaponStatCostTexts[i].textWrappingMode = TextWrappingModes.NoWrap;
                StyleShopTmp(weaponStatCostTexts[i], new Color(0.03f, 0.25f, 0.07f, 1f), 0.26f, false);
                LayoutElement tle = weaponStatCostTexts[i].gameObject.AddComponent<LayoutElement>();
                tle.preferredHeight = 46f;

                Image arrow = NewMenuImage("UpIcon", rowRt, "Menu_Icon_Up");
                LayoutElement ale = arrow.gameObject.AddComponent<LayoutElement>();
                ale.preferredWidth = 34f; ale.preferredHeight = 38f;
            }

            // 4. Weapon selection cards
            weaponCardSelectedOverlays.Clear();
            const float cw = 215f, chh = 260f, cgap = 20f;
            float cy = stY + stH + 24f;
            for (int i = 0; i < s_weaponKeys.Length; i++)
            {
                string key = s_weaponKeys[i];
                float cx = scX + i * (cw + cgap);
                Image card = NewMenuImage("Btn_Weapon_" + key, root, "Menu_Card_Frame", false, true);
                RectTransform cRt = card.rectTransform;
                ShopPlaceTopLeft(cRt, cx, cy, cw, chh);
                cRt.pivot = new Vector2(0.5f, 0.5f);
                cRt.anchoredPosition = new Vector2(cx + cw * 0.5f, -(cy + chh * 0.5f));
                Button cb = card.gameObject.AddComponent<Button>();
                cb.targetGraphic = card;
                cb.transition = Selectable.Transition.None;
                cb.onClick.AddListener(() => StartCoroutine(ShopPress(cRt, () => SelectWeapon(key))));

                Image sel = NewMenuImage("Selected", cRt, "Menu_Card_Frame_Gold", false);
                ShopStretch(sel.rectTransform, -4f, -4f, -4f, -4f);
                weaponCardSelectedOverlays[key] = sel.gameObject;

                RectTransform wr = NewShopRect("WeaponArt", cRt);
                ShopPlaceTopLeft(wr, 16f, 24f, cw - 32f, 150f);
                Image wimg = wr.gameObject.AddComponent<Image>();
                Sprite wsp = LoadWeaponSpriteHD(key);
                if (wsp != null) wimg.sprite = wsp;
                wimg.preserveAspect = true;
                wimg.raycastTarget = false;

                TextMeshProUGUI nm = CreateText("Name", cRt, WeaponDisplayName(key), 28f, Color.white, TextAlignmentOptions.Center, true);
                ShopPlaceTopLeft(nm.rectTransform, 10f, 182f, cw - 20f, 56f);
                nm.textWrappingMode = TextWrappingModes.NoWrap;
                nm.enableAutoSizing = true;
                nm.fontSizeMin = 18f;
                nm.fontSizeMax = 28f;
                StyleShopTmp(nm, new Color(0.02f, 0.08f, 0.32f, 1f), 0.24f, false);
            }

            UpdateWeaponsScreenVisuals();
        }

        private void CreateBorderLine(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            GameObject lineObj = new GameObject("BorderLine");
            lineObj.transform.SetParent(parent, false);
            RectTransform lRt = lineObj.AddComponent<RectTransform>();
            lRt.anchorMin = anchorMin;
            lRt.anchorMax = anchorMax;
            lRt.offsetMin = Vector2.zero;
            lRt.offsetMax = Vector2.zero;
            Image lImg = lineObj.AddComponent<Image>();
            lImg.color = color;
            lImg.raycastTarget = false;
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

            if (weaponChamberImg != null)
            {
                StartCoroutine(PunchScale(weaponChamberImg.transform, 0.14f, null));
            }
        }

        public void UpgradeWeaponStat(string statName)
        {
            string currentWeapon = PlayerPrefs.GetString("ZombieSquad_SelectedWeapon", "AssaultRifle");
            string prefKey = $"ZombieSquad_Wep_{currentWeapon}_{statName}";
            int currentLevel = PlayerPrefs.GetInt(prefKey, 1);
            if (currentLevel >= 10)
            {
                Debug.Log($"[ZombieSquad] {currentWeapon} {statName} is already at MAX level!");
                return;
            }

            int cost = 100 * currentLevel;

            if (SpendPlayerCoins(cost))
            {
                currentLevel++;
                PlayerPrefs.SetInt(prefKey, currentLevel);
                PlayerPrefs.Save();
                Debug.Log($"[ZombieSquad] Upgraded {currentWeapon} {statName} to Level {currentLevel}!");
                UpdateWeaponsScreenVisuals();

                if (MobControl.Audio.ZombieSoundManager.Instance != null)
                {
                    MobControl.Audio.ZombieSoundManager.Instance.PlayCoinSpend();
                    MobControl.Audio.ZombieSoundManager.Instance.PlayUpgradeSuccess();
                }
            }
            else
            {
                Debug.LogWarning($"[ZombieSquad] Not enough coins to upgrade {currentWeapon} {statName}! Cost: {cost}, Balance: {GetPlayerCoins()}");
            }
        }

        public void UpdateWeaponsScreenVisuals()
        {
            string currentWeapon = PlayerPrefs.GetString("ZombieSquad_SelectedWeapon", "AssaultRifle");

            // 1. Update Title
            if (weaponTitleText != null)
            {
                weaponTitleText.text = WeaponDisplayName(currentWeapon);
            }

            // 2. Update Chamber Weapon Display Sprite
            if (weaponChamberImg != null)
            {
                weaponChamberImg.sprite = LoadWeaponSpriteHD(currentWeapon);
            }

            // 3. Update Bottom Selection Highlight Border
            int wepIdx = System.Array.IndexOf(s_weaponKeys, currentWeapon);
            if (wepIdx < 0) wepIdx = 0;

            foreach (var kvp in weaponCardSelectedOverlays)
            {
                if (kvp.Value != null) kvp.Value.SetActive(kvp.Key == currentWeapon);
            }

            if (weaponCardSelectionHighlightRt != null)
            {
                Vector2[] cardMins = new Vector2[]
                {
                    new Vector2(0.018f, 0.022f),
                    new Vector2(0.263f, 0.022f),
                    new Vector2(0.508f, 0.022f),
                    new Vector2(0.753f, 0.022f)
                };
                Vector2[] cardMaxs = new Vector2[]
                {
                    new Vector2(0.262f, 0.162f),
                    new Vector2(0.507f, 0.162f),
                    new Vector2(0.752f, 0.162f),
                    new Vector2(0.982f, 0.162f)
                };
                weaponCardSelectionHighlightRt.anchorMin = cardMins[wepIdx];
                weaponCardSelectionHighlightRt.anchorMax = cardMaxs[wepIdx];
            }


            // 4. Update 4 Stats & Progress Bars
            int wepTotalLvl = 1;
            string[] statNames = new string[] { "Damage", "FireRate", "Range", "Magazine" };
            for (int i = 0; i < 4; i++)
            {
                int lvl = PlayerPrefs.GetInt($"ZombieSquad_Wep_{currentWeapon}_{statNames[i]}", 1);
                wepTotalLvl += (lvl - 1);
                float progress = Mathf.Clamp01(lvl / 10f);

                if (weaponStatFills[i] != null)
                {
                    weaponStatFills[i].rectTransform.anchorMax = new Vector2(Mathf.Max(0.08f, progress), 1f);
                }
                if (weaponStatValTexts[i] != null)
                {
                    weaponStatValTexts[i].text = lvl >= 10 ? "MAX" : $"LV. {lvl} / 10";
                }
                if (weaponStatCostTexts[i] != null)
                {
                    weaponStatCostTexts[i].text = lvl >= 10 ? "MAX" : $"{100 * lvl:N0}";
                }
                if (weaponStatCostIcons[i] != null) weaponStatCostIcons[i].gameObject.SetActive(lvl < 10);
                if (weaponStatCostIcons[i] != null)
                {
                    Transform upIcon = weaponStatCostIcons[i].transform.parent.Find("UpIcon");
                    if (upIcon != null) upIcon.gameObject.SetActive(lvl < 10);
                }
                if (weaponStatUpgradeBgs[i] != null) SetMenuSprite(weaponStatUpgradeBgs[i], lvl >= 10 ? "Menu_Btn_Grey" : "Menu_Btn_Green");
            }
            if (weaponLevelText != null) weaponLevelText.text = $"LV. {wepTotalLvl}";
        }
        #endregion

        #region Zombie Squad Shop Screen

        private Sprite LoadZombieSquadShopScreenSprite()
        {
            try
            {
                // Always prioritize high-resolution uncompressed clean dynamic shop modal
                string[] searchPaths = new string[]
                {
                    System.IO.Path.Combine(Application.dataPath, "Resources", "UI", "ShopDynamic", "00_master_clean_shop_modal.png"),
                    System.IO.Path.Combine(Application.dataPath, "Resources", "UI", "ShopDynamic", "Zombie_Squad_Dynamic_Shop_Assets", "00_master_clean_shop_modal.png"),
                    System.IO.Path.Combine(Application.dataPath, "Resources", "UI", "ZombieSquad_ShopScreen.png"),
                    System.IO.Path.Combine(Application.dataPath, "Resources", "UI", "ZombieSquad_ShopScreen.jpg")
                };

                foreach (string pngPath in searchPaths)
                {
                    if (System.IO.File.Exists(pngPath))
                    {
                        byte[] fileData = System.IO.File.ReadAllBytes(pngPath);
                        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        if (tex.LoadImage(fileData))
                        {
                            tex.filterMode = FilterMode.Bilinear;
                            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                        }
                    }
                }

                Sprite s = Resources.Load<Sprite>("UI/ShopDynamic/00_master_clean_shop_modal")
                         ?? Resources.Load<Sprite>("UI/ZombieSquad_ShopScreen");
                if (s != null) return s;

                Texture2D resTex = Resources.Load<Texture2D>("UI/ShopDynamic/00_master_clean_shop_modal")
                                ?? Resources.Load<Texture2D>("UI/ZombieSquad_ShopScreen");
                if (resTex != null)
                {
                    return Sprite.Create(resTex, new Rect(0, 0, resTex.width, resTex.height), new Vector2(0.5f, 0.5f));
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[ZombieRunnerUIManager] ShopScreen sprite loading: " + ex.Message);
            }
            return null;
        }

        private Sprite LoadZombieSquadShopExtendedSprite()
        {
            try
            {
                Sprite s = Resources.Load<Sprite>("UI/ShopDynamic/shop_extended_tall")
                         ?? Resources.Load<Sprite>("UI/ZombieSquad_ShopScreen_Extended");
                if (s != null) return s;

                Texture2D resTex = Resources.Load<Texture2D>("UI/ShopDynamic/shop_extended_tall")
                                ?? Resources.Load<Texture2D>("UI/ZombieSquad_ShopScreen_Extended");
                if (resTex != null)
                {
                    resTex.filterMode = FilterMode.Bilinear;
                    return Sprite.Create(resTex, new Rect(0, 0, resTex.width, resTex.height), new Vector2(0.5f, 0.5f));
                }

                string[] searchPaths = new string[]
                {
                    System.IO.Path.Combine(Application.dataPath, "Resources", "UI", "ShopDynamic", "shop_extended_tall.jpg"),
                    System.IO.Path.Combine(Application.dataPath, "Resources", "UI", "ZombieSquad_ShopScreen_Extended.jpg"),
                    System.IO.Path.Combine(Application.dataPath, "..", "assets", "shop", "shop_extended_tall.jpg")
                };

                foreach (string p in searchPaths)
                {
                    if (System.IO.File.Exists(p))
                    {
                        byte[] fileData = System.IO.File.ReadAllBytes(p);
                        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
                        if (tex.LoadImage(fileData))
                        {
                            tex.filterMode = FilterMode.Bilinear;
                            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[ZombieRunnerUIManager] ShopExtended sprite loading: " + ex.Message);
            }
            return null;
        }

        // ===================== RESPONSIVE HD SHOP SCREEN =====================
        // Fully modular layout (no baked full-screen artwork):
        //  - SafeArea aware header (back, title, live coin/diamond pills)
        //  - Fixed 1000x1490 design root, uniformly scaled to fit every aspect ratio
        //  - HD sprites from Resources/UI/ShopHD (uncompressed, clean alpha)
        private const string ShopHDPath = "UI/ShopHD/";
        private const string MenuHDPath = "UI/MenuHD/";
        private static readonly Vector2 ShopDesignSize = new Vector2(1000f, 1490f);

        private static RectTransform NewShopRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static void ShopStretch(RectTransform rt, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        // Places a rect inside its parent using top-left design coordinates.
        // Uses a centre pivot: Unity aligns preserveAspect images to the pivot, so this keeps art centred in its box.
        private static void ShopPlaceTopLeft(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x + w * 0.5f, -(y + h * 0.5f));
            rt.sizeDelta = new Vector2(w, h);
        }

        // Bottom-centre pivot: preserveAspect art (heroes) sits horizontally centred with its feet on the box bottom.
        private static void ShopPlaceBottomCenter(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(x + w * 0.5f, -(y + h));
            rt.sizeDelta = new Vector2(w, h);
        }

        private Image NewShopImage(string name, Transform parent, string spriteName, bool preserveAspect = true, bool raycast = false)
        {
            RectTransform rt = NewShopRect(name, parent);
            Image img = rt.gameObject.AddComponent<Image>();
            Sprite sp = LoadSpriteFromUI(ShopHDPath + spriteName);
            if (sp != null) img.sprite = sp;
            img.preserveAspect = preserveAspect;
            img.raycastTarget = raycast;
            return img;
        }

        // The project's "Fredoka-Bold.ttf" is actually a variable font that defaults to the Light (300) weight,
        // which is why text looked thin. The shop uses a true static Bold instance (Resources/Fonts/ShopHD).
        private static TMP_FontAsset s_shopBoldFont;
        private static bool s_shopBoldFontTried;

        private static TMP_FontAsset GetShopBoldFont()
        {
            if (s_shopBoldFont != null || s_shopBoldFontTried) return s_shopBoldFont;
            s_shopBoldFontTried = true;
            try
            {
                Font f = Resources.Load<Font>("Fonts/ShopHD/Fredoka-BoldStatic");
                if (f != null)
                {
                    s_shopBoldFont = TMP_FontAsset.CreateFontAsset(f, 72, 10,
                        UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024);
                    if (s_shopBoldFont != null) s_shopBoldFont.name = "Fredoka-BoldStatic SDF (Runtime)";
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[ZombieRunnerUIManager] Shop bold font: " + ex.Message);
            }
            return s_shopBoldFont;
        }

        private static void StyleShopTmp(TextMeshProUGUI t, Color outline, float outlineWidth, bool dropShadow)
        {
            if (t == null) return;
            TMP_FontAsset bold = GetShopBoldFont();
            if (bold != null)
            {
                t.font = bold;
                t.fontStyle = FontStyles.Normal; // real Bold glyphs, no faux-bold smearing
            }
            t.outlineColor = outline;
            t.outlineWidth = outlineWidth;
            if (dropShadow)
            {
                Material m = t.fontMaterial;
                m.EnableKeyword(ShaderUtilities.Keyword_Underlay);
                m.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0.02f, 0.10f, 0.65f));
                m.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.35f);
                m.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.65f);
                m.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.15f);
            }
        }

        // Press feedback that always returns to scale 1 (safe against rapid double taps).
        private IEnumerator ShopPress(Transform tr, System.Action onComplete)
        {
            const float duration = 0.14f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float s = 1f - 0.07f * Mathf.Sin(Mathf.Clamp01(t / duration) * Mathf.PI);
                if (tr != null) tr.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            if (tr != null) tr.localScale = Vector3.one;
            onComplete?.Invoke();
        }

        private void BuildZombieSquadShopScreenPanel(Transform canvasRoot)
        {
            if (shopPanel != null) SafeDestroy(shopPanel);
            Transform oldShop = canvasRoot.Find("ZombieSquadShopPanel");
            if (oldShop != null) SafeDestroy(oldShop.gameObject);
            shopCheckoutModalObj = null; // modal lives inside the panel -> always rebuild it with the panel

            shopPanel = CreateFullPanel("ZombieSquadShopPanel", canvasRoot, new Color(0.01f, 0.03f, 0.08f, 1f));

            // 1. Full-screen night battlefield cover (envelope, never stretched) + soft dim for focus
            Sprite nightScenerySprite = LoadSpriteFromUI(MenuHDPath + "Menu_Scenery_Night", "UI/Scenery_NightBattlefield_Clean");
            SetupAmbientSceneryCover(shopPanel, nightScenerySprite, 1080f / 1920f);

            RectTransform dimRt = NewShopRect("BackdropDim", shopPanel.transform);
            ShopStretch(dimRt);
            Image dimImg = dimRt.gameObject.AddComponent<Image>();
            dimImg.color = new Color(0.01f, 0.02f, 0.07f, 0.30f);
            dimImg.raycastTarget = false;
            shopScreenBgImage = dimImg;

            // 2. Safe area root (notch / punch-hole safe)
            RectTransform safeRt = NewShopRect("SafeArea", shopPanel.transform);
            ShopStretch(safeRt);
            safeRt.gameObject.AddComponent<ShopSafeArea>();

            // 3. Header
            BuildShopHeader(safeRt);

            // 4. Body: fixed design root scaled uniformly to fit the remaining space
            RectTransform bodyRt = NewShopRect("Body", safeRt);
            ShopStretch(bodyRt, 24f, 28f, 24f, 160f);

            RectTransform rootRt = NewShopRect("ShopContent", bodyRt);
            ShopResponsiveFitter fitter = rootRt.gameObject.AddComponent<ShopResponsiveFitter>();
            fitter.designSize = ShopDesignSize;
            fitter.maxScale = 1.25f;
            fitter.Apply(true);

            const float panelTop = 120f;
            const float panelHeight = 1370f;

            // Soldier mascot sits *behind* the panel so its cut-off base is hidden by the frame
            Image soldierImg = NewShopImage("HeaderSoldier", rootRt, "Shop_Soldier");
            ShopPlaceTopLeft(soldierImg.rectTransform, 160f, panelTop - 135f, 230f, 157f);

            // Main glowing panel (9-sliced, crisp corners at any size)
            Image panelImg = NewShopImage("ShopPanelFrame", rootRt, "Shop_Panel_Frame", false);
            panelImg.type = Image.Type.Sliced;
            panelImg.pixelsPerUnitMultiplier = 1f;
            ShopPlaceTopLeft(panelImg.rectTransform, 0f, panelTop, ShopDesignSize.x, panelHeight);

            // Logo overlapping panel top edge
            Image logoImg = NewShopImage("ZombieSquadLogo", rootRt, "Shop_Logo");
            ShopPlaceTopLeft(logoImg.rectTransform, 350f, 10f, 380f, 200f);

            // 5. 2x2 product grid
            RectTransform gridRt = NewShopRect("CardGrid", rootRt);
            ShopStretch(gridRt);
            shopCoinsContainer = gridRt.gameObject; // Cache for health check (Card_1000 lives directly under it)

            const string artDir = "UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/";
            const float cardW = 420f, cardH = 560f, gapX = 44f, gapY = 40f;
            float gridLeft = (ShopDesignSize.x - (cardW * 2f + gapX)) * 0.5f;
            float gridTop = panelTop + 140f;

            CreateShopCard(gridRt, 0, "1000", gridLeft, gridTop, cardW, cardH, 1000, 80, artDir + "10_coin_stack_product_art_clean");
            CreateShopCard(gridRt, 1, "5000", gridLeft + cardW + gapX, gridTop, cardW, cardH, 5000, 200, artDir + "10_coin_stack_product_art_clean");
            CreateShopCard(gridRt, 2, "20000", gridLeft, gridTop + cardH + gapY, cardW, cardH, 20000, 1000, artDir + "11_treasure_chest_product_art_clean");
            CreateShopCard(gridRt, 3, "50000", gridLeft + cardW + gapX, gridTop + cardH + gapY, cardW, cardH, 50000, 2000, artDir + "12_large_treasure_chest_product_art_clean");

            // 6. Checkout modal (scaled up so it reads well on 1080p canvases)
            BuildShopCheckoutModal(shopPanel.transform);
            if (shopCheckoutDialogRt != null) shopCheckoutDialogRt.localScale = new Vector3(2.2f, 2.2f, 1f);
        }

        private void BuildShopHeader(RectTransform parent)
        {
            shopBackBtnRect = BuildHDHeader(parent, "SHOP", () => ShowZombieSquadHomeScreen(),
                () => OpenShopCheckoutModal("1,000 COINS", 80, () => AddPlayerCoins(1000)),
                () => OpenShopCheckoutModal("5,000 COINS", 200, () => AddPlayerCoins(5000)));
        }

        // Shared HD header: back button, bold title, live coin + diamond pills. Returns the back button rect.
        private RectTransform BuildHDHeader(RectTransform parent, string titleStr, System.Action onBack, System.Action onCoinPlus, System.Action onDiamondPlus)
        {
            RectTransform headerRt = NewShopRect("HDHeader", parent);
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.anchoredPosition = new Vector2(0f, -12f);
            headerRt.sizeDelta = new Vector2(0f, 140f);

            Image backImg = NewShopImage("Btn_Back", headerRt, "Shop_BackButton", true, true);
            RectTransform bRt = backImg.rectTransform;
            bRt.anchorMin = bRt.anchorMax = new Vector2(0f, 0.5f);
            bRt.pivot = new Vector2(0f, 0.5f);
            bRt.anchoredPosition = new Vector2(30f, 0f);
            bRt.sizeDelta = new Vector2(108f, 110f);
            Button backBtn = backImg.gameObject.AddComponent<Button>();
            backBtn.targetGraphic = backImg;
            backBtn.transition = Selectable.Transition.None;
            backBtn.onClick.AddListener(() => StartCoroutine(ShopPress(bRt, onBack)));

            TextMeshProUGUI title = CreateText("Title_" + titleStr, headerRt, titleStr, 58f, Color.white, TextAlignmentOptions.Left, true);
            // Title stretches between the back button and the currency pills, auto-shrinking so it never overlaps them
            RectTransform tRt = title.rectTransform;
            tRt.anchorMin = new Vector2(0f, 0.5f);
            tRt.anchorMax = new Vector2(1f, 0.5f);
            tRt.pivot = new Vector2(0f, 0.5f);
            tRt.offsetMin = new Vector2(156f, -43f);
            tRt.offsetMax = new Vector2(-(26f + 250f + 22f + 250f + 34f), 47f);
            title.enableAutoSizing = true;
            title.fontSizeMin = 34f;
            title.fontSizeMax = 58f;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            StyleShopTmp(title, new Color(0.02f, 0.08f, 0.32f, 1f), 0.22f, true);

            const float pillW = 250f, pillH = 84f, pillGap = 22f;
            CreateShopCurrencyPill(headerRt, "CoinPill", "Shop_CoinIcon", 86f, -(26f + pillW + pillGap), pillW, pillH,
                GetPlayerCoins().ToString("N0"), new Color(1f, 0.92f, 0.38f, 1f), allCoinsDisplays, onCoinPlus);
            CreateShopCurrencyPill(headerRt, "DiamondPill", "Shop_DiamondIcon", 76f, -26f, pillW, pillH,
                GetPlayerDiamonds().ToString("N0"), new Color(0.62f, 0.92f, 1f, 1f), allDiamondsDisplays, onDiamondPlus);
            return bRt;
        }

        // Shared HD screen scaffold: night cover + dim + safe area + header + uniformly scaled design root.
        private RectTransform BuildHDScreenScaffold(GameObject panel, string titleStr, System.Action onBack, out RectTransform backRt)
        {
            SetupAmbientSceneryCover(panel, LoadSpriteFromUI(MenuHDPath + "Menu_Scenery_Night", "UI/Scenery_NightBattlefield_Clean"), 1080f / 1920f);

            RectTransform dimRt = NewShopRect("BackdropDim", panel.transform);
            ShopStretch(dimRt);
            Image dimImg = dimRt.gameObject.AddComponent<Image>();
            dimImg.color = new Color(0.01f, 0.02f, 0.07f, 0.34f);
            dimImg.raycastTarget = false;

            RectTransform safeRt = NewShopRect("SafeArea", panel.transform);
            ShopStretch(safeRt);
            safeRt.gameObject.AddComponent<ShopSafeArea>();

            backRt = BuildHDHeader(safeRt, titleStr, onBack,
                () => ShowZombieSquadShopScreen(),
                () => ShowZombieSquadShopScreen());

            RectTransform bodyRt = NewShopRect("Body", safeRt);
            ShopStretch(bodyRt, 24f, 28f, 24f, 160f);

            RectTransform rootRt = NewShopRect("Content", bodyRt);
            ShopResponsiveFitter fitter = rootRt.gameObject.AddComponent<ShopResponsiveFitter>();
            fitter.designSize = ShopDesignSize;
            fitter.maxScale = 1.25f;
            fitter.Apply(true);
            return rootRt;
        }

        private Image NewMenuImage(string name, Transform parent, string spriteName, bool preserveAspect = true, bool raycast = false)
        {
            RectTransform rt = NewShopRect(name, parent);
            Image img = rt.gameObject.AddComponent<Image>();
            Sprite sp = LoadSpriteFromUI(MenuHDPath + spriteName);
            if (sp != null) img.sprite = sp;
            img.preserveAspect = preserveAspect;
            img.raycastTarget = raycast;
            return img;
        }

        private void SetMenuSprite(Image img, string spriteName)
        {
            if (img == null) return;
            Sprite sp = LoadSpriteFromUI(MenuHDPath + spriteName, ShopHDPath + spriteName);
            if (sp != null) img.sprite = sp;
        }

        private void CreateShopCurrencyPill(
            RectTransform parent, string name, string iconSprite, float iconSize,
            float rightX, float w, float h, string value, Color valueColor,
            List<TextMeshProUGUI> registerList, System.Action onPlus)
        {
            Image pillImg = NewShopImage(name, parent, "Shop_Currency_Pill", false);
            RectTransform pRt = pillImg.rectTransform;
            pRt.anchorMin = pRt.anchorMax = new Vector2(1f, 0.5f);
            pRt.pivot = new Vector2(1f, 0.5f);
            pRt.anchoredPosition = new Vector2(rightX, 0f);
            pRt.sizeDelta = new Vector2(w, h);

            Image icon = NewShopImage("Icon", pRt, iconSprite);
            RectTransform iRt = icon.rectTransform;
            iRt.anchorMin = iRt.anchorMax = new Vector2(0f, 0.5f);
            iRt.pivot = new Vector2(0.5f, 0.5f);
            iRt.anchoredPosition = new Vector2(24f, 0f);
            iRt.sizeDelta = new Vector2(iconSize, iconSize);

            TextMeshProUGUI val = CreateText("ValueText", pRt, value, 38f, valueColor, TextAlignmentOptions.Center, true);
            val.enableAutoSizing = true;
            val.fontSizeMin = 22f;
            val.fontSizeMax = 40f;
            val.textWrappingMode = TextWrappingModes.NoWrap;
            val.rectTransform.offsetMin = new Vector2(72f, 6f);
            val.rectTransform.offsetMax = new Vector2(-72f, -6f);
            StyleShopTmp(val, new Color(0.01f, 0.04f, 0.16f, 1f), 0.2f, false);
            if (registerList != null && !registerList.Contains(val)) registerList.Add(val);

            Image plus = NewShopImage("Btn_Plus", pRt, "Shop_PlusIcon", true, true);
            RectTransform plRt = plus.rectTransform;
            plRt.anchorMin = plRt.anchorMax = new Vector2(1f, 0.5f);
            plRt.pivot = new Vector2(1f, 0.5f);
            plRt.anchoredPosition = new Vector2(-10f, 0f);
            plRt.sizeDelta = new Vector2(62f, 64f);
            Button plusBtn = plus.gameObject.AddComponent<Button>();
            plusBtn.targetGraphic = plus;
            plusBtn.transition = Selectable.Transition.None;
            plusBtn.onClick.AddListener(() => StartCoroutine(ShopPress(plRt, onPlus)));
        }

        private void CreateShopCard(
            RectTransform parent, int index, string key,
            float x, float y, float w, float h,
            int coinsAmount, int inrPrice, string artPath)
        {
            System.Action buy = () => OpenShopCheckoutModal($"{coinsAmount:N0} COINS", inrPrice, () =>
            {
                AddPlayerCoins(coinsAmount);
                ShowMultiplierPop($"🎉 +{coinsAmount:N0} COINS!");
            });

            // Card frame (2x HD art, drawn at exact aspect -> no stretching)
            Image cardImg = NewShopImage("Card_" + key, parent, "Shop_Card_Frame", false, true);
            RectTransform cRt = cardImg.rectTransform;
            ShopPlaceTopLeft(cRt, x, y, w, h);
            // Scale around the card centre for press feedback
            cRt.pivot = new Vector2(0.5f, 0.5f);
            cRt.anchoredPosition = new Vector2(x + w * 0.5f, -(y + h * 0.5f));

            Button cardBtn = cardImg.gameObject.AddComponent<Button>();
            cardBtn.targetGraphic = cardImg;
            cardBtn.transition = Selectable.Transition.None;
            cardBtn.onClick.AddListener(() => StartCoroutine(ShopPress(cRt, buy)));

            // Quantity label
            TextMeshProUGUI qty = CreateText("QuantityLabel", cRt, coinsAmount.ToString("N0"), 54f, Color.white, TextAlignmentOptions.Center, true);
            ShopPlaceTopLeft(qty.rectTransform, 20f, 28f, w - 40f, 80f);
            qty.textWrappingMode = TextWrappingModes.NoWrap;
            StyleShopTmp(qty, new Color(0.02f, 0.07f, 0.30f, 1f), 0.24f, true);

            // Product art
            RectTransform artRt = NewShopRect("ProductArt", cRt);
            ShopPlaceTopLeft(artRt, 25f, 108f, w - 50f, 290f);
            Image artImg = artRt.gameObject.AddComponent<Image>();
            Sprite artSp = LoadSpriteFromUI(artPath);
            if (artSp != null) artImg.sprite = artSp;
            artImg.preserveAspect = true;
            artImg.raycastTarget = false;

            // Green buy button
            Image btnImg = NewShopImage("Btn_Buy_" + key + "_Coins", cRt, "Shop_Buy_Button", false, true);
            RectTransform bRt = btnImg.rectTransform;
            ShopPlaceTopLeft(bRt, 30f, h - 34f - 100f, w - 60f, 100f);
            bRt.pivot = new Vector2(0.5f, 0.5f);
            bRt.anchoredPosition = new Vector2(w * 0.5f, -(h - 34f - 50f));
            Button buyBtn = btnImg.gameObject.AddComponent<Button>();
            buyBtn.targetGraphic = btnImg;
            buyBtn.transition = Selectable.Transition.None;
            buyBtn.onClick.AddListener(() => StartCoroutine(ShopPress(bRt, buy)));

            // Price row: [₹ icon][price] centred on the button face (face is the top ~80% of the art)
            RectTransform rowRt = NewShopRect("PriceRow", bRt);
            ShopStretch(rowRt, 10f, 16f, 10f, 0f);
            HorizontalLayoutGroup hlg = rowRt.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 10f;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            Image rupee = NewShopImage("RupeeIcon", rowRt, "Shop_Rupee");
            LayoutElement rle = rupee.gameObject.AddComponent<LayoutElement>();
            rle.preferredWidth = 34f;
            rle.preferredHeight = 43f;

            TextMeshProUGUI price = CreateText("PriceText", rowRt, inrPrice.ToString("N0"), 48f, Color.white, TextAlignmentOptions.Center, true);
            price.textWrappingMode = TextWrappingModes.NoWrap;
            StyleShopTmp(price, new Color(0.05f, 0.30f, 0.09f, 1f), 0.26f, false);
            LayoutElement ple = price.gameObject.AddComponent<LayoutElement>();
            ple.preferredHeight = 60f;
        }

        private GameObject CreateTransparentTouchButton(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            System.Action onClick)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image img = obj.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.001f);
            img.raycastTarget = true;

            Button btn = obj.AddComponent<Button>();
            btn.targetGraphic = img;
            if (onClick != null)
            {
                btn.onClick.AddListener(() =>
                {
                    StartCoroutine(PunchScale(obj.transform, 0.10f, () => onClick()));
                });
            }
            return obj;
        }

        private GameObject CreateShopProductCard(
            Transform parent,
            string cardName,
            string amountStr,
            string productArtPath,
            string priceStr,
            int inrPrice,
            int coinsAmount,
            string buttonGameObjectName)
        {
            GameObject cardObj = new GameObject(cardName);
            cardObj.transform.SetParent(parent, false);
            RectTransform cRt = cardObj.AddComponent<RectTransform>();
            cRt.sizeDelta = new Vector2(415f, 515f);

            // Card frame (9-sliced 09_empty_shop_card.png)
            Image cardBg = cardObj.AddComponent<Image>();
            Sprite cardSp = LoadSpriteFromUI("UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/09_empty_shop_card");
            if (cardSp != null) cardBg.sprite = cardSp;
            cardBg.type = Image.Type.Sliced;
            cardBg.color = Color.white;
            cardBg.raycastTarget = true;

            // Top Quantity Label (TMP)
            GameObject qtyObj = new GameObject("QuantityLabel");
            qtyObj.transform.SetParent(cardObj.transform, false);
            RectTransform qRt = qtyObj.AddComponent<RectTransform>();
            qRt.anchorMin = new Vector2(0.08f, 0.80f);
            qRt.anchorMax = new Vector2(0.92f, 0.95f);
            qRt.offsetMin = Vector2.zero; qRt.offsetMax = Vector2.zero;

            TextMeshProUGUI qTmp = qtyObj.AddComponent<TextMeshProUGUI>();
            qTmp.enabled = false;
            if (s_globalChildishFont != null) qTmp.font = s_globalChildishFont;
            qTmp.text = amountStr;
            qTmp.fontSize = 42f;
            qTmp.fontStyle = FontStyles.Bold;
            qTmp.alignment = TextAlignmentOptions.Center;
            qTmp.color = Color.white;
            qTmp.raycastTarget = false;
            qTmp.enabled = true;

            Outline qOutl = qtyObj.AddComponent<Outline>();
            qOutl.effectColor = new Color(0.01f, 0.08f, 0.28f, 1f);
            qOutl.effectDistance = new Vector2(2f, -2f);

            // Radiant Blue Glow behind art for juicy game pop
            Sprite glowSp = LoadSpriteFromUI("UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/24_blue_glow_effect");
            if (glowSp != null)
            {
                GameObject glowObj = new GameObject("GlowFx");
                glowObj.transform.SetParent(cardObj.transform, false);
                RectTransform glowRt = glowObj.AddComponent<RectTransform>();
                glowRt.anchorMin = new Vector2(0.12f, 0.22f);
                glowRt.anchorMax = new Vector2(0.88f, 0.78f);
                glowRt.offsetMin = Vector2.zero; glowRt.offsetMax = Vector2.zero;
                Image glowImg = glowObj.AddComponent<Image>();
                glowImg.sprite = glowSp;
                glowImg.color = new Color(1f, 1f, 1f, 0.80f);
                glowImg.preserveAspect = true;
                glowImg.raycastTarget = false;
            }

            // Product Art in center (preserveAspect = true, zero halos)
            Sprite artSp = LoadSpriteFromUI(productArtPath);
            GameObject artObj = new GameObject("ProductArt");
            artObj.transform.SetParent(cardObj.transform, false);
            RectTransform aRt = artObj.AddComponent<RectTransform>();
            aRt.anchorMin = new Vector2(0.12f, 0.24f);
            aRt.anchorMax = new Vector2(0.88f, 0.78f);
            aRt.offsetMin = Vector2.zero; aRt.offsetMax = Vector2.zero;

            Image aImg = artObj.AddComponent<Image>();
            if (artSp != null) aImg.sprite = artSp;
            aImg.preserveAspect = true;
            aImg.raycastTarget = false;

            // Buy Button at bottom (13_green_purchase_button_blank.png)
            Sprite btnSp = LoadSpriteFromUI("UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/13_green_purchase_button_blank", "UI/Btn_3D_Green");
            GameObject btnObj = new GameObject(buttonGameObjectName);
            btnObj.transform.SetParent(cardObj.transform, false);
            RectTransform bRt = btnObj.AddComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.08f, 0.05f);
            bRt.anchorMax = new Vector2(0.92f, 0.22f);
            bRt.offsetMin = Vector2.zero; bRt.offsetMax = Vector2.zero;

            Image bImg = btnObj.AddComponent<Image>();
            if (btnSp != null) bImg.sprite = btnSp;
            bImg.type = Image.Type.Sliced;
            bImg.raycastTarget = true;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bImg;
            btn.onClick.AddListener(() =>
            {
                StartCoroutine(PunchScale(btnObj.transform, 0.12f, () =>
                {
                    OpenShopCheckoutModal($"{coinsAmount:N0} COINS", inrPrice, () =>
                    {
                        AddPlayerCoins(coinsAmount);
                        ShowMultiplierPop($"🎉 +{coinsAmount:N0} COINS!");
                    });
                }));
            });

            // Price Container with icon_rupee and Price Number
            GameObject pBox = new GameObject("PriceContainer");
            pBox.transform.SetParent(btnObj.transform, false);
            RectTransform pbRt = pBox.AddComponent<RectTransform>();
            pbRt.anchorMin = Vector2.zero;
            pbRt.anchorMax = Vector2.one;
            pbRt.offsetMin = Vector2.zero;
            pbRt.offsetMax = Vector2.zero;

            HorizontalLayoutGroup phlg = pBox.AddComponent<HorizontalLayoutGroup>();
            phlg.spacing = 8f;
            phlg.childAlignment = TextAnchor.MiddleCenter;
            phlg.childControlWidth = false;
            phlg.childControlHeight = false;
            phlg.childForceExpandWidth = false;
            phlg.childForceExpandHeight = false;

            // Crisp Rupee Icon
            Sprite rupeeSp = LoadSpriteFromUI("UI/icon_rupee");
            if (rupeeSp != null)
            {
                GameObject rIconObj = new GameObject("RupeeIcon");
                rIconObj.transform.SetParent(pBox.transform, false);
                RectTransform riRt = rIconObj.AddComponent<RectTransform>();
                riRt.sizeDelta = new Vector2(24f, 30f);
                Image riImg = rIconObj.AddComponent<Image>();
                riImg.sprite = rupeeSp;
                riImg.preserveAspect = true;
                riImg.raycastTarget = false;
            }

            // Price Number TMP
            GameObject pTextObj = new GameObject("PriceText");
            pTextObj.transform.SetParent(pBox.transform, false);
            RectTransform ptRt = pTextObj.AddComponent<RectTransform>();
            ptRt.sizeDelta = new Vector2(130f, 45f);

            TextMeshProUGUI ptTmp = pTextObj.AddComponent<TextMeshProUGUI>();
            ptTmp.enabled = false;
            if (s_globalChildishFont != null) ptTmp.font = s_globalChildishFont;
            string cleanPriceNum = priceStr.Replace("₹", "").Replace("Rs.", "").Trim();
            ptTmp.text = cleanPriceNum;
            ptTmp.fontSize = 32f;
            ptTmp.fontStyle = FontStyles.Bold;
            ptTmp.alignment = TextAlignmentOptions.MidlineLeft;
            ptTmp.color = Color.white;
            ptTmp.raycastTarget = false;
            ptTmp.enabled = true;

            Outline ptOutl = pTextObj.AddComponent<Outline>();
            ptOutl.effectColor = new Color(0.04f, 0.22f, 0.08f, 1f);
            ptOutl.effectDistance = new Vector2(2f, -2f);

            return cardObj;
        }

        private GameObject CreateShopPriceButton(
            string buttonName,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            int inrPrice,
            int coinsAmount)
        {
            GameObject btnObj = new GameObject(buttonName);
            btnObj.transform.SetParent(parent, false);
            RectTransform bRt = btnObj.AddComponent<RectTransform>();
            bRt.anchorMin = anchorMin;
            bRt.anchorMax = anchorMax;
            bRt.offsetMin = Vector2.zero;
            bRt.offsetMax = Vector2.zero;

            Image bImg = btnObj.AddComponent<Image>();
            bImg.color = new Color(1f, 1f, 1f, 0.001f); // Transparent clickable overlay
            bImg.raycastTarget = true;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bImg;

            btn.onClick.AddListener(() =>
            {
                StartCoroutine(PunchScale(btnObj.transform, 0.12f, () =>
                {
                    OpenShopCheckoutModal($"{coinsAmount:N0} COINS", inrPrice, () =>
                    {
                        AddPlayerCoins(coinsAmount);
                        ShowMultiplierPop($"🎉 +{coinsAmount:N0} COINS!");
                    });
                }));
            });

            return btnObj;
        }

        private void BuildShopCheckoutModal(Transform parent)
        {
            if (shopCheckoutModalObj != null) return;

            // Fullscreen dark backdrop overlay
            shopCheckoutModalObj = new GameObject("ShopCheckoutModalOverlay");
            shopCheckoutModalObj.transform.SetParent(parent, false);
            RectTransform overlayRt = shopCheckoutModalObj.AddComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;

            Image overlayImg = shopCheckoutModalObj.AddComponent<Image>();
            overlayImg.color = new Color(0.01f, 0.04f, 0.12f, 0.88f); // Deep dark blur backdrop
            overlayImg.raycastTarget = true;
            Button overlayDismiss = shopCheckoutModalObj.AddComponent<Button>();
            overlayDismiss.onClick.AddListener(CloseShopCheckoutModal);

            // Dialog Box
            GameObject dialogObj = new GameObject("DialogBox");
            dialogObj.transform.SetParent(shopCheckoutModalObj.transform, false);
            shopCheckoutDialogRt = dialogObj.AddComponent<RectTransform>();
            shopCheckoutDialogRt.anchorMin = new Vector2(0.5f, 0.5f);
            shopCheckoutDialogRt.anchorMax = new Vector2(0.5f, 0.5f);
            shopCheckoutDialogRt.pivot = new Vector2(0.5f, 0.5f);
            shopCheckoutDialogRt.sizeDelta = new Vector2(380f, 350f);

            Image dialogBg = dialogObj.AddComponent<Image>();
            dialogBg.color = new Color(0.04f, 0.12f, 0.28f, 0.98f); // Deep cyber blue card
            dialogBg.raycastTarget = true;

            // Dialog Border Outline
            Outline dlgOutline = dialogObj.AddComponent<Outline>();
            dlgOutline.effectColor = new Color(0.0f, 0.82f, 1.0f, 0.90f); // Glowing neon cyan border
            dlgOutline.effectDistance = new Vector2(2.5f, -2.5f);

            // 1. Dialog Header: "SECURE CHECKOUT"
            TextMeshProUGUI titleTmp = CreateText("ModalTitle", dialogObj.transform, "SECURE CHECKOUT", 22f, new Color(0f, 0.92f, 1f, 1f), TextAlignmentOptions.Center, true);
            RectTransform titleRt = titleTmp.GetComponent<RectTransform>();
            titleRt.anchoredPosition = new Vector2(0f, 130f);

            // 2. Subtitle: "Instant In-Game Squad Credit"
            TextMeshProUGUI subTmp = CreateText("ModalSubtitle", dialogObj.transform, "Instant Real-Money Squad Credit", 13f, new Color(0.70f, 0.82f, 0.95f, 1f), TextAlignmentOptions.Center, false);
            RectTransform subRt = subTmp.GetComponent<RectTransform>();
            subRt.anchoredPosition = new Vector2(0f, 102f);

            // 3. Item & Price Summary Banner
            GameObject summaryObj = new GameObject("SummaryBanner");
            summaryObj.transform.SetParent(dialogObj.transform, false);
            RectTransform sumRt = summaryObj.AddComponent<RectTransform>();
            sumRt.sizeDelta = new Vector2(340f, 65f);
            sumRt.anchoredPosition = new Vector2(0f, 40f);
            Image sumBg = summaryObj.AddComponent<Image>();
            sumBg.color = new Color(0.02f, 0.08f, 0.20f, 0.95f);

            shopCheckoutItemText = CreateText("ItemText", summaryObj.transform, "1,000 COINS", 20f, new Color(1f, 0.90f, 0.25f, 1f), TextAlignmentOptions.Left, true);
            RectTransform itemRt = shopCheckoutItemText.GetComponent<RectTransform>();
            itemRt.anchorMin = new Vector2(0.06f, 0f);
            itemRt.anchorMax = new Vector2(0.62f, 1f);
            itemRt.offsetMin = Vector2.zero;
            itemRt.offsetMax = Vector2.zero;

            shopCheckoutPriceText = CreateText("PriceText", summaryObj.transform, "Rs. 80", 22f, new Color(0.20f, 1f, 0.45f, 1f), TextAlignmentOptions.Right, true);
            RectTransform priceRt = shopCheckoutPriceText.GetComponent<RectTransform>();
            priceRt.anchorMin = new Vector2(0.62f, 0f);
            priceRt.anchorMax = new Vector2(0.94f, 1f);
            priceRt.offsetMin = Vector2.zero;
            priceRt.offsetMax = Vector2.zero;

            // 4. Payment Methods Info
            TextMeshProUGUI payInfoTmp = CreateText("PayInfo", dialogObj.transform, "UPI (GPay, PhonePe, Paytm) | Cards | NetBanking", 12f, new Color(0.55f, 0.75f, 0.95f, 1f), TextAlignmentOptions.Center, false);
            RectTransform payInfoRt = payInfoTmp.GetComponent<RectTransform>();
            payInfoRt.anchoredPosition = new Vector2(0f, -20f);

            // 5. Big Green "CONFIRM & PAY" Button
            GameObject payBtnObj = new GameObject("Btn_Confirm_Pay");
            payBtnObj.transform.SetParent(dialogObj.transform, false);
            RectTransform pbRt = payBtnObj.AddComponent<RectTransform>();
            pbRt.sizeDelta = new Vector2(340f, 54f);
            pbRt.anchoredPosition = new Vector2(0f, -75f);

            Image payBtnImg = payBtnObj.AddComponent<Image>();
            payBtnImg.color = new Color(0.12f, 0.78f, 0.35f, 1f); // Vibrant glossy green
            payBtnImg.raycastTarget = true;

            Button payBtn = payBtnObj.AddComponent<Button>();
            payBtn.targetGraphic = payBtnImg;

            shopCheckoutConfirmBtnText = CreateText("PayBtnLabel", payBtnObj.transform, "CONFIRM & PAY", 22f, Color.white, TextAlignmentOptions.Center, true);
            Shadow sp = shopCheckoutConfirmBtnText.gameObject.AddComponent<Shadow>();
            sp.effectColor = new Color(0.02f, 0.25f, 0.08f, 1f);
            sp.effectDistance = new Vector2(1.5f, -1.5f);
            shopCheckoutConfirmBtnText.rectTransform.anchoredPosition = Vector2.zero;

            payBtn.onClick.AddListener(() =>
            {
                StartCoroutine(PunchScale(payBtnObj.transform, 0.12f, ConfirmShopCheckoutPayment));
            });

            // 6. Close [X] Button in Top-Right
            GameObject closeBtnObj = new GameObject("Btn_Close_Modal");
            closeBtnObj.transform.SetParent(dialogObj.transform, false);
            RectTransform cbRt = closeBtnObj.AddComponent<RectTransform>();
            cbRt.sizeDelta = new Vector2(32f, 32f);
            cbRt.anchoredPosition = new Vector2(165f, 145f);

            Image closeImg = closeBtnObj.AddComponent<Image>();
            closeImg.color = new Color(0.15f, 0.25f, 0.45f, 0.9f);

            Button closeBtn = closeBtnObj.AddComponent<Button>();
            closeBtn.targetGraphic = closeImg;

            TextMeshProUGUI closeTxt = CreateText("CloseLabel", closeBtnObj.transform, "X", 16f, Color.white, TextAlignmentOptions.Center, true);
            closeTxt.rectTransform.anchorMin = Vector2.zero;
            closeTxt.rectTransform.anchorMax = Vector2.one;
            closeTxt.rectTransform.offsetMin = Vector2.zero;
            closeTxt.rectTransform.offsetMax = Vector2.zero;

            closeBtn.onClick.AddListener(CloseShopCheckoutModal);

            shopCheckoutModalObj.SetActive(false);
        }

        private void OpenShopCheckoutModal(string itemTitle, int inrPrice, System.Action onConfirm)
        {
            if (shopCheckoutModalObj == null) return;

            onShopCheckoutConfirmAction = onConfirm;
            if (shopCheckoutItemText != null) shopCheckoutItemText.text = itemTitle;
            if (shopCheckoutPriceText != null) shopCheckoutPriceText.text = $"Rs. {inrPrice:N0}";
            if (shopCheckoutConfirmBtnText != null) shopCheckoutConfirmBtnText.text = $"CONFIRM & PAY (Rs. {inrPrice:N0})";

            shopCheckoutModalObj.SetActive(true);
            if (shopCheckoutDialogRt != null)
            {
                StartCoroutine(PunchScale(shopCheckoutDialogRt, 0.18f, null));
            }
        }

        private void ConfirmShopCheckoutPayment()
        {
            if (shopCheckoutModalObj != null) shopCheckoutModalObj.SetActive(false);
            onShopCheckoutConfirmAction?.Invoke();
            UpdateAllCurrencyDisplays();
        }

        private void CloseShopCheckoutModal()
        {
            if (shopCheckoutModalObj != null) shopCheckoutModalObj.SetActive(false);
        }

        public void SelectShopTab(string tabKey)
        {
            activeShopTab = tabKey;
            Debug.Log($"[ZombieSquad] Selected Shop Tab: {tabKey}");

            if (shopCoinsContainer != null) shopCoinsContainer.SetActive(true);
        }

        public void BuyCoins(int amount)
        {
            AddPlayerCoins(amount);
            Debug.Log($"[ZombieSquad] Purchased {amount} Coins! New Balance: {GetPlayerCoins()}");
            ShowMultiplierPop($"🎉 +{amount:N0} COINS!");
        }

        public void UpdateShopScreenVisuals()
        {
            UpdateAllCurrencyDisplays();
            SelectShopTab(activeShopTab);
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

            Sprite missionsSprite = LoadZombieSquadMissionsScreenSprite();
            float missAspect = (missionsSprite != null && missionsSprite.rect.height > 0)
                ? (missionsSprite.rect.width / missionsSprite.rect.height)
                : (576f / 1024f);

            // Ambient Scenery Cover (EnvelopeParent fills 100% of any tall/wide mobile display - zero black bars!)
            SetupAmbientSceneryCover(missionsPanel, missionsSprite, 576f / 1024f);

            // Master Responsive ArtContainer (FitInParent: ZERO horizontal crop, ZERO stretch)
            GameObject artContainer = new GameObject("ArtContainer");
            artContainer.transform.SetParent(missionsPanel.transform, false);
            RectTransform artRt = artContainer.AddComponent<RectTransform>();
            artRt.anchorMin = Vector2.zero; artRt.anchorMax = Vector2.one;
            artRt.offsetMin = Vector2.zero; artRt.offsetMax = Vector2.zero;

            AspectRatioFitter missArf = artContainer.AddComponent<AspectRatioFitter>();
            missArf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            missArf.aspectRatio = missAspect;

            GameObject missImgObj = new GameObject("ArtworkImage");
            missImgObj.transform.SetParent(artContainer.transform, false);
            RectTransform miRt = missImgObj.AddComponent<RectTransform>();
            miRt.anchorMin = Vector2.zero; miRt.anchorMax = Vector2.one;
            miRt.offsetMin = Vector2.zero; miRt.offsetMax = Vector2.zero;
            Image missionsImg = missImgObj.AddComponent<Image>();
            if (missionsSprite != null) missionsImg.sprite = missionsSprite;
            missionsImg.raycastTarget = true; missionsImg.preserveAspect = false;

            Shadow missShadowB = missImgObj.AddComponent<Shadow>();
            missShadowB.effectColor = new Color(0.005f, 0.015f, 0.05f, 0.85f);
            missShadowB.effectDistance = new Vector2(0f, -8f);

            Shadow missShadowT = missImgObj.AddComponent<Shadow>();
            missShadowT.effectColor = new Color(0.005f, 0.015f, 0.05f, 0.85f);
            missShadowT.effectDistance = new Vector2(0f, 8f);

            // Top Header: Back + Currency HUD
            CreateTopCurrencyHeader(artContainer.transform, "MISSIONS", () => ShowZombieSquadHomeScreen());

            // 3 Missions Progress Bars & Claim Buttons
            float[] rowY = new float[] { 0.485f, 0.270f, 0.055f };
            int[] rewards = new int[] { 100, 200, 500 };

            for (int i = 0; i < 3; i++)
            {
                int missionId = i + 1;
                int rewardCoins = rewards[i];
                float y = rowY[i];

                // Progress Bar
                GameObject barObj = new GameObject($"Mission_Bar_{missionId}");
                barObj.transform.SetParent(artContainer.transform, false);
                RectTransform bRt = barObj.AddComponent<RectTransform>();
                bRt.anchorMin = new Vector2(0.280f, y + 0.060f);
                bRt.anchorMax = new Vector2(0.640f, y + 0.095f);
                bRt.offsetMin = Vector2.zero;
                bRt.offsetMax = Vector2.zero;

                GameObject fillObj = new GameObject("Fill");
                fillObj.transform.SetParent(barObj.transform, false);
                RectTransform fRt = fillObj.AddComponent<RectTransform>();
                fRt.anchorMin = Vector2.zero;
                fRt.anchorMax = new Vector2(1f, 1f);
                fRt.offsetMin = Vector2.zero;
                fRt.offsetMax = Vector2.zero;
                missionProgressFills[i] = fillObj.AddComponent<Image>();
                missionProgressFills[i].color = new Color(0.20f, 0.85f, 0.40f, 1f);

                missionProgressTexts[i] = CreateText("ProgTxt", barObj.transform, "READY", 17f, Color.white, TextAlignmentOptions.Center, true);
                missionProgressTexts[i].GetComponent<RectTransform>().sizeDelta = new Vector2(200f, 30f);

                // Claim Button
                CreateTransparentButton($"Btn_Claim_Mission{missionId}", artContainer.transform, new Vector2(0.680f, y), new Vector2(0.940f, y + 0.065f), () => ClaimMissionReward(missionId, rewardCoins));

                // Claim Button Text
                GameObject btnLabelObj = new GameObject($"Claim_Label_{missionId}");
                btnLabelObj.transform.SetParent(artContainer.transform, false);
                RectTransform blRt = btnLabelObj.AddComponent<RectTransform>();
                blRt.anchorMin = new Vector2(0.680f, y);
                blRt.anchorMax = new Vector2(0.940f, y + 0.065f);
                blRt.offsetMin = Vector2.zero;
                blRt.offsetMax = Vector2.zero;
                missionClaimBtnTexts[i] = CreateText("ClaimTxt", btnLabelObj.transform, "CLAIM", 22f, Color.white, TextAlignmentOptions.Center, true);
                missionClaimBtnTexts[i].GetComponent<RectTransform>().sizeDelta = new Vector2(160f, 50f);
            }

            UpdateMissionsScreenVisuals();
        }

        public void ClaimMissionReward(int missionId, int coinsReward)
        {
            string prefKey = $"ZombieSquad_MissionClaimed_{missionId}";
            if (PlayerPrefs.GetInt(prefKey, 0) == 1)
            {
                Debug.Log($"[ZombieSquad] Mission {missionId} already claimed!");
                return;
            }

            PlayerPrefs.SetInt(prefKey, 1);
            PlayerPrefs.Save();
            AddPlayerCoins(coinsReward);
            if (MobControl.Audio.ZombieSoundManager.Instance != null)
            {
                MobControl.Audio.ZombieSoundManager.Instance.PlayCoinSpend();
            }
            Debug.Log($"[ZombieSquad] Claimed Mission {missionId} Reward: +{coinsReward} Coins!");
            UpdateMissionsScreenVisuals();
        }

        public void UpdateMissionsScreenVisuals()
        {
            for (int i = 0; i < 3; i++)
            {
                int missionId = i + 1;
                bool isClaimed = PlayerPrefs.GetInt($"ZombieSquad_MissionClaimed_{missionId}", 0) == 1;
                if (missionClaimBtnTexts[i] != null)
                {
                    missionClaimBtnTexts[i].text = isClaimed ? "✓ CLAIMED" : "CLAIM";
                    missionClaimBtnTexts[i].color = isClaimed ? new Color(0.7f, 0.9f, 0.7f, 0.8f) : Color.white;
                }
                if (missionProgressTexts[i] != null)
                {
                    missionProgressTexts[i].text = isClaimed ? "COMPLETED" : "100%";
                }
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

        // ===================== RESPONSIVE HD DAILY SCREEN =====================
        private static readonly int[] s_dailyAmounts = { 10, 20, 30, 40, 50, 60, 100 };
        private readonly Image[] dailyDayFrames = new Image[7];
        private readonly GameObject[] dailyDayTodayGlows = new GameObject[7];
        private readonly GameObject[] dailyDayClaimedBadges = new GameObject[7];
        private readonly Image[] dailyDayArts = new Image[7];
        private Image dailyClaimBtnIcon;

        private void BuildZombieSquadDailyScreenPanel(Transform canvasRoot)
        {
            Transform oldDaily = canvasRoot.Find("ZombieSquadDailyPanel");
            if (oldDaily != null && (dailyPanel == null || oldDaily.gameObject != dailyPanel)) SafeDestroy(oldDaily.gameObject);
            if (dailyPanel != null) SafeDestroy(dailyPanel);

            dailyPanel = CreateFullPanel("ZombieSquadDailyPanel", canvasRoot, new Color(0.01f, 0.03f, 0.08f, 1f));
            RectTransform root = BuildHDScreenScaffold(dailyPanel, "DAILY", () => ShowZombieSquadHomeScreen(), out RectTransform _);

            // 1. Hero on glowing pedestal
            Image ped = NewShopRect("GlowPedestal", root).gameObject.AddComponent<Image>();
            Sprite pedSp = LoadSpriteFromUI("UI/CharactersDynamic/14_glow_platform_clean", "UI/CharactersDynamic/14_glow_platform");
            if (pedSp != null) ped.sprite = pedSp;
            ped.preserveAspect = true;
            ped.raycastTarget = false;
            ShopPlaceTopLeft(ped.rectTransform, 260f, 380f, 480f, 135f);

            string heroKey = PlayerPrefs.GetString("ZombieSquad_SelectedHero", "Soldier");
            Image hero = NewShopRect("DailyHero", root).gameObject.AddComponent<Image>();
            Sprite heroSp = LoadSpriteFromUI(MenuHDPath + "Menu_Hero_" + heroKey, MenuHDPath + "Menu_Hero_Soldier", "UI/Hero_Soldier_Full");
            if (heroSp != null) hero.sprite = heroSp;
            hero.preserveAspect = true;
            hero.raycastTarget = false;
            ShopPlaceBottomCenter(hero.rectTransform, 280f, 10f, 440f, 438f);

            // 2. Rewards panel
            const float pY = 560f, pH = 650f;
            Image panel = NewShopImage("RewardsPanel", root, "Shop_Panel_Frame", false);
            panel.type = Image.Type.Sliced;
            ShopPlaceTopLeft(panel.rectTransform, 30f, pY, 940f, pH);

            Image banner = NewMenuImage("RewardsBanner", root, "Menu_Banner", false);
            banner.type = Image.Type.Sliced;
            ShopPlaceTopLeft(banner.rectTransform, 220f, pY - 46f, 560f, 108f);
            TextMeshProUGUI bTxt = CreateText("Title", banner.rectTransform, "DAILY REWARDS", 50f, Color.white, TextAlignmentOptions.Center, true);
            bTxt.rectTransform.offsetMin = new Vector2(20f, 8f);
            bTxt.rectTransform.offsetMax = new Vector2(-20f, -8f);
            bTxt.textWrappingMode = TextWrappingModes.NoWrap;
            StyleShopTmp(bTxt, new Color(0.02f, 0.08f, 0.32f, 1f), 0.24f, true);

            // 3. Day cards: 4 on top, then day 5, 6 and a wide day 7
            const float cw = 200f, ch = 250f, gap = 20f;
            float rowW = cw * 4f + gap * 3f;
            float x0 = (ShopDesignSize.x - rowW) * 0.5f;
            float r1 = pY + 88f, r2 = r1 + ch + gap;
            for (int i = 0; i < 7; i++)
            {
                float x, y, w = cw;
                if (i < 4) { x = x0 + i * (cw + gap); y = r1; }
                else if (i < 6) { x = x0 + (i - 4) * (cw + gap); y = r2; }
                else { x = x0 + 2f * (cw + gap); y = r2; w = cw * 2f + gap; }
                CreateDailyDayCard(root, i, x, y, w, ch);
            }

            // 4. Claim button
            dailyClaimBtnBg = NewMenuImage("Btn_ClaimDailyToday", root, "Menu_Btn_Green", false, true);
            RectTransform cbRt = dailyClaimBtnBg.rectTransform;
            ShopPlaceTopLeft(cbRt, 200f, pY + pH + 24f, 600f, 134f);
            cbRt.pivot = new Vector2(0.5f, 0.5f);
            cbRt.anchoredPosition = new Vector2(500f, -(pY + pH + 24f + 67f));
            dailyClaimBtn = dailyClaimBtnBg.gameObject.AddComponent<Button>();
            dailyClaimBtn.targetGraphic = dailyClaimBtnBg;
            dailyClaimBtn.transition = Selectable.Transition.None;
            dailyClaimBtn.onClick.AddListener(() => StartCoroutine(ShopPress(cbRt, ClaimTodayDailyReward)));
            BuildIconLabelRow(cbRt, 54f, 46f, out dailyClaimBtnIcon, out dailyClaimBtnText, 22f);

            UpdateDailyScreenVisuals();
        }

        private void CreateDailyDayCard(RectTransform parent, int i, float x, float y, float w, float h)
        {
            int day = i + 1;
            Image frame = NewMenuImage("Day_" + day, parent, "Menu_Card_Frame", false, true);
            RectTransform cRt = frame.rectTransform;
            ShopPlaceTopLeft(cRt, x, y, w, h);
            cRt.pivot = new Vector2(0.5f, 0.5f);
            cRt.anchoredPosition = new Vector2(x + w * 0.5f, -(y + h * 0.5f));
            dailyDayFrames[i] = frame;
            Button b = frame.gameObject.AddComponent<Button>();
            b.targetGraphic = frame;
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() =>
            {
                // Only today's reward can be collected from its card
                DailyState st = GetDailyState();
                if (st.canClaim && day == st.claimDay) StartCoroutine(ShopPress(cRt, ClaimTodayDailyReward));
            });

            Image glow = NewMenuImage("TodayGlow", cRt, "Menu_Card_Frame_Gold", false);
            ShopStretch(glow.rectTransform, -4f, -4f, -4f, -4f);
            dailyDayTodayGlows[i] = glow.gameObject;

            TextMeshProUGUI dayTxt = CreateText("DayLabel", cRt, "DAY " + day, 36f, Color.white, TextAlignmentOptions.Center, true);
            ShopPlaceTopLeft(dayTxt.rectTransform, 10f, 14f, w - 20f, 50f);
            dayTxt.textWrappingMode = TextWrappingModes.NoWrap;
            StyleShopTmp(dayTxt, new Color(0.02f, 0.08f, 0.32f, 1f), 0.24f, false);

            string art = day == 7 ? "Menu_Daily_Chest" : (day <= 2 ? "Menu_Daily_CoinPile" : "Menu_Daily_CoinStack");
            Image artImg = NewMenuImage("RewardArt", cRt, art);
            ShopPlaceTopLeft(artImg.rectTransform, 18f, 66f, w - 36f, 116f);
            dailyDayArts[i] = artImg;

            // Amount (+ optional diamond bonus on day 7)
            RectTransform amtRow = NewShopRect("AmountRow", cRt);
            ShopPlaceTopLeft(amtRow, 8f, 186f, w - 16f, 52f);
            HorizontalLayoutGroup hlg = amtRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 8f;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            Image coin = NewShopImage("Coin", amtRow, "Shop_CoinIcon");
            LayoutElement cle = coin.gameObject.AddComponent<LayoutElement>();
            cle.preferredWidth = 34f; cle.preferredHeight = 34f;
            dailyDayStatusTexts[i] = CreateText("Amount", amtRow, "+" + s_dailyAmounts[i], 38f, new Color(1f, 0.92f, 0.38f, 1f), TextAlignmentOptions.Center, true);
            dailyDayStatusTexts[i].textWrappingMode = TextWrappingModes.NoWrap;
            StyleShopTmp(dailyDayStatusTexts[i], new Color(0.25f, 0.10f, 0.0f, 1f), 0.28f, false);
            dailyDayStatusTexts[i].gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;
            if (day == 7)
            {
                Image gem = NewShopImage("Gem", amtRow, "Shop_DiamondIcon");
                LayoutElement gle = gem.gameObject.AddComponent<LayoutElement>();
                gle.preferredWidth = 32f; gle.preferredHeight = 32f;
                TextMeshProUGUI gTxt = CreateText("GemAmount", amtRow, "+10", 38f, new Color(0.62f, 0.92f, 1f, 1f), TextAlignmentOptions.Center, true);
                gTxt.textWrappingMode = TextWrappingModes.NoWrap;
                StyleShopTmp(gTxt, new Color(0.01f, 0.08f, 0.25f, 1f), 0.28f, false);
                gTxt.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;
            }

            Image badge = NewMenuImage("ClaimedBadge", cRt, "Menu_Badge_Check");
            ShopPlaceTopLeft(badge.rectTransform, (w - 96f) * 0.5f, 76f, 96f, 96f);
            dailyDayClaimedBadges[i] = badge.gameObject;
        }

        // ---------------- Daily reward logic (real calendar days) ----------------
        // Rules:
        //  * One claim per local calendar day.
        //  * Claims build a 7-day streak (Day 1..7). After Day 7 the cycle restarts at Day 1.
        //  * Missing a full day resets the streak to Day 1.
        //  * Day 7 also grants +10 diamonds.
        private const string DailyStreakKey = "ZombieSquad_DailyStreak";        // days claimed in current cycle (0..7)
        private const string DailyLastClaimKey = "ZombieSquad_DailyLastClaim";  // yyyyMMdd of last claim
        private float dailyCountdownTimer;

        private struct DailyState
        {
            public int claimedInCycle;  // how many cards show as claimed (0..7)
            public bool canClaim;
            public int claimDay;        // 1..7, the day that is (or will next be) claimable
        }

        private static System.DateTime? GetDailyLastClaimDate()
        {
            string s = PlayerPrefs.GetString(DailyLastClaimKey, "");
            if (System.DateTime.TryParseExact(s, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out System.DateTime d))
                return d.Date;
            return null;
        }

        private static DailyState GetDailyState()
        {
            int streak = Mathf.Clamp(PlayerPrefs.GetInt(DailyStreakKey, 0), 0, 7);
            System.DateTime today = System.DateTime.Now.Date;
            System.DateTime? last = GetDailyLastClaimDate();

            DailyState st = new DailyState();
            if (last == null)
            {
                st.claimedInCycle = 0; st.canClaim = true; st.claimDay = 1;
            }
            else if (last.Value >= today)
            {
                // Already claimed today (or device clock moved backwards): wait for tomorrow
                st.claimedInCycle = streak; st.canClaim = false;
                st.claimDay = streak >= 7 ? 1 : streak + 1;
            }
            else if ((today - last.Value).TotalDays <= 1.0)
            {
                // Claimed yesterday: continue the streak (restart cycle after day 7)
                int done = streak >= 7 ? 0 : streak;
                st.claimedInCycle = done; st.canClaim = true; st.claimDay = done + 1;
            }
            else
            {
                // Missed a day: streak resets
                st.claimedInCycle = 0; st.canClaim = true; st.claimDay = 1;
            }
            return st;
        }

        private static string GetDailyTimeUntilReset()
        {
            System.TimeSpan left = System.DateTime.Now.Date.AddDays(1) - System.DateTime.Now;
            if (left.TotalSeconds < 0) left = System.TimeSpan.Zero;
            return $"{(int)left.TotalHours:00}:{left.Minutes:00}:{left.Seconds:00}";
        }

        private void ClaimTodayDailyReward()
        {
            DailyState st = GetDailyState();
            if (!st.canClaim)
            {
                Debug.Log("[ZombieSquad] Daily reward already claimed today. Next in " + GetDailyTimeUntilReset());
                return;
            }

            int day = st.claimDay;
            int coins = s_dailyAmounts[day - 1];
            PlayerPrefs.SetInt(DailyStreakKey, day);
            PlayerPrefs.SetString(DailyLastClaimKey, System.DateTime.Now.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture));
            PlayerPrefs.Save();

            AddPlayerCoins(coins);
            if (day == 7) AddPlayerDiamonds(10);
            UpdateAllCurrencyDisplays();

            if (MobControl.Audio.ZombieSoundManager.Instance != null)
                MobControl.Audio.ZombieSoundManager.Instance.PlayCoinSpend();

            ShowMultiplierPop(day == 7 ? $"+{coins} COINS  +10 GEMS" : $"+{coins} COINS");
            Debug.Log($"[ZombieSquad] Claimed Daily Day {day}: +{coins} coins. Balance: {GetPlayerCoins()}");
            UpdateDailyScreenVisuals();
        }

        // Kept for compatibility with older callers: always claims today's reward (if available).
        public void ClaimDailyReward(int day, int coinsReward)
        {
            ClaimTodayDailyReward();
        }

        public bool IsDailyRewardAvailable()
        {
            return GetDailyState().canClaim;
        }

        // Called from Update(): refresh the "NEXT IN" countdown once per second and roll over at midnight.
        private void TickDailyCountdown()
        {
            if (dailyPanel == null || !dailyPanel.activeInHierarchy) return;
            dailyCountdownTimer -= Time.unscaledDeltaTime;
            if (dailyCountdownTimer > 0f) return;
            dailyCountdownTimer = 1f;
            UpdateDailyScreenVisuals();
        }

        public void UpdateDailyScreenVisuals()
        {
            DailyState st = GetDailyState();
            Color dim = new Color(0.55f, 0.60f, 0.72f, 1f);

            for (int i = 0; i < 7; i++)
            {
                int day = i + 1;
                bool claimed = day <= st.claimedInCycle;
                bool isToday = st.canClaim && day == st.claimDay;
                if (dailyDayFrames[i] != null) dailyDayFrames[i].color = claimed ? dim : Color.white;
                if (dailyDayArts[i] != null) dailyDayArts[i].color = claimed ? new Color(1f, 1f, 1f, 0.35f) : Color.white;
                if (dailyDayTodayGlows[i] != null) dailyDayTodayGlows[i].SetActive(isToday);
                if (dailyDayClaimedBadges[i] != null) dailyDayClaimedBadges[i].SetActive(claimed);
                if (dailyDayStatusTexts[i] != null) dailyDayStatusTexts[i].text = "+" + s_dailyAmounts[i];
            }

            if (dailyClaimBtnText == null || dailyClaimBtnBg == null) return;
            if (st.canClaim)
            {
                SetMenuSprite(dailyClaimBtnBg, "Menu_Btn_Green");
                if (dailyClaimBtnIcon != null) { dailyClaimBtnIcon.gameObject.SetActive(true); SetMenuSprite(dailyClaimBtnIcon, "Shop_CoinIcon"); }
                dailyClaimBtnText.text = $"COLLECT +{s_dailyAmounts[st.claimDay - 1]}";
                dailyClaimBtnText.outlineColor = new Color(0.03f, 0.25f, 0.07f, 1f);
                if (dailyClaimBtn != null) dailyClaimBtn.interactable = true;
            }
            else
            {
                SetMenuSprite(dailyClaimBtnBg, "Menu_Btn_Grey");
                if (dailyClaimBtnIcon != null) dailyClaimBtnIcon.gameObject.SetActive(false);
                dailyClaimBtnText.text = "NEXT IN " + GetDailyTimeUntilReset();
                dailyClaimBtnText.outlineColor = new Color(0.15f, 0.17f, 0.22f, 1f);
                if (dailyClaimBtn != null) dailyClaimBtn.interactable = false;
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
            TextMeshProUGUI titleText = CreateText("GameTitle", container.transform, "ZOMBIE RAGE", 54, new Color(0.2f, 0.95f, 0.42f, 1f), TextAlignmentOptions.Center, true);
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

        // ===================== HD GAMEPLAY EXTRAS: HEALTH + PAUSE =====================
        private bool isGamePaused;
        public bool IsGamePaused => isGamePaused;
        private GameObject pauseMenuPanel;
        private RectTransform pauseDialogRt;
        private Image pauseMusicBtnImg;
        private Image pauseSfxBtnImg;
        private TextMeshProUGUI pauseLevelText;
        private Image hudHealthFill;
        private RectTransform hudHeartRt;
        private int hudLastHealth = -1;

        private RectTransform hudProgressHDFillRt;

        // Clean, safe-area aware top HUD:
        //   Row 1: [Pause] [LEVEL n]                       [heart | HP bar]
        //   Row 2: [=========== level progress ===========[flag]] (boss skull)
        private void BuildHDGameplayExtras(Transform canvasRoot)
        {
            RectTransform safe = NewShopRect("HDSafeArea", gameplayHudPanel.transform);
            ShopStretch(safe);
            safe.gameObject.AddComponent<ShopSafeArea>();

            RectTransform top = NewShopRect("TopHud", safe);
            top.anchorMin = new Vector2(0f, 1f);
            top.anchorMax = new Vector2(1f, 1f);
            top.pivot = new Vector2(0.5f, 1f);
            top.anchoredPosition = new Vector2(0f, -18f);
            top.sizeDelta = new Vector2(-56f, 210f); // 28px side margins

            // ---- Row 1 ----
            Image pauseImg = NewMenuImage("Btn_Pause", top, "Menu_Btn_Pause", true, true);
            RectTransform pbRt = pauseImg.rectTransform;
            pbRt.anchorMin = pbRt.anchorMax = new Vector2(0f, 1f);
            pbRt.pivot = new Vector2(0.5f, 0.5f);
            pbRt.anchoredPosition = new Vector2(46f, -46f);
            pbRt.sizeDelta = new Vector2(92f, 92f);
            Button pb = pauseImg.gameObject.AddComponent<Button>();
            pb.targetGraphic = pauseImg;
            pb.transition = Selectable.Transition.None;
            pb.onClick.AddListener(() => StartCoroutine(ShopPress(pbRt, PauseGame)));

            Image lvlPill = NewMenuImage("LevelPill", top, "Menu_Hud_Pill", false, true);
            lvlPill.type = Image.Type.Sliced;
            RectTransform lpRt = lvlPill.rectTransform;
            lpRt.anchorMin = lpRt.anchorMax = new Vector2(0f, 1f);
            lpRt.pivot = new Vector2(0f, 0.5f);
            lpRt.anchoredPosition = new Vector2(108f, -46f);
            lpRt.sizeDelta = new Vector2(230f, 78f);
            Button lpBtn = lvlPill.gameObject.AddComponent<Button>();
            lpBtn.targetGraphic = lvlPill;
            lpBtn.transition = Selectable.Transition.None;
            lpBtn.onClick.AddListener(PauseGame); // tapping the level badge also opens the pause menu
            hudLevelText = CreateText("LevelText", lpRt, $"LEVEL {activeLevel}", 38f, Color.white, TextAlignmentOptions.Center, true);
            hudLevelText.rectTransform.offsetMin = new Vector2(12f, 4f);
            hudLevelText.rectTransform.offsetMax = new Vector2(-12f, -4f);
            hudLevelText.textWrappingMode = TextWrappingModes.NoWrap;
            StyleShopTmp(hudLevelText, new Color(0.02f, 0.08f, 0.32f, 1f), 0.22f, false);

            Image hpPill = NewMenuImage("HealthPill", top, "Menu_Hud_Pill", false);
            hpPill.type = Image.Type.Sliced;
            RectTransform hpRt = hpPill.rectTransform;
            hpRt.anchorMin = hpRt.anchorMax = new Vector2(1f, 1f);
            hpRt.pivot = new Vector2(1f, 0.5f);
            hpRt.anchoredPosition = new Vector2(0f, -46f);
            hpRt.sizeDelta = new Vector2(360f, 78f);

            Image hpTrack = NewMenuImage("HealthTrack", hpRt, "Menu_Bar_Track", false);
            hpTrack.type = Image.Type.Sliced;
            RectTransform htRt = hpTrack.rectTransform;
            htRt.anchorMin = new Vector2(0f, 0.5f);
            htRt.anchorMax = new Vector2(1f, 0.5f);
            htRt.pivot = new Vector2(0.5f, 0.5f);
            htRt.offsetMin = new Vector2(78f, -21f);
            htRt.offsetMax = new Vector2(-16f, 21f);
            RectTransform hfRt = NewShopRect("Fill", htRt);
            hfRt.anchorMin = Vector2.zero; hfRt.anchorMax = Vector2.one;
            hfRt.offsetMin = Vector2.zero; hfRt.offsetMax = Vector2.zero;
            hudHealthFill = hfRt.gameObject.AddComponent<Image>();
            Sprite redFill = LoadSpriteFromUI(MenuHDPath + "Menu_Bar_Fill_Red");
            if (redFill != null) hudHealthFill.sprite = redFill;
            hudHealthFill.type = Image.Type.Sliced;
            hudHealthFill.raycastTarget = false;
            hudHealthText = CreateText("HealthText", htRt, "HP 18 / 18", 26f, Color.white, TextAlignmentOptions.Center, true);
            hudHealthText.textWrappingMode = TextWrappingModes.NoWrap;
            StyleShopTmp(hudHealthText, new Color(0.25f, 0.02f, 0.05f, 1f), 0.3f, false);

            Image heart = NewMenuImage("HeartIcon", hpRt, "Menu_Icon_Heart");
            hudHeartRt = heart.rectTransform;
            hudHeartRt.anchorMin = hudHeartRt.anchorMax = new Vector2(0f, 0.5f);
            hudHeartRt.pivot = new Vector2(0.5f, 0.5f);
            hudHeartRt.anchoredPosition = new Vector2(42f, 2f);
            hudHeartRt.sizeDelta = new Vector2(82f, 76f);

            // ---- Row 2: level progress ----
            Image track = NewMenuImage("HordeProgressBarTrack", top, "Menu_Bar_Track", false);
            track.type = Image.Type.Sliced;
            RectTransform tRt = track.rectTransform;
            tRt.anchorMin = new Vector2(0f, 1f);
            tRt.anchorMax = new Vector2(1f, 1f);
            tRt.pivot = new Vector2(0.5f, 0.5f);
            tRt.anchoredPosition = new Vector2(-40f, -140f);
            tRt.sizeDelta = new Vector2(-100f, 40f); // leaves room on the right for the boss skull

            RectTransform pfRt = NewShopRect("Fill", tRt);
            pfRt.anchorMin = Vector2.zero; pfRt.anchorMax = new Vector2(0.05f, 1f);
            pfRt.offsetMin = Vector2.zero; pfRt.offsetMax = Vector2.zero;
            Image pf = pfRt.gameObject.AddComponent<Image>();
            Sprite greenFill = LoadSpriteFromUI(MenuHDPath + "Menu_Bar_Fill");
            if (greenFill != null) pf.sprite = greenFill;
            pf.type = Image.Type.Sliced;
            pf.raycastTarget = false;
            hudProgressHDFillRt = pfRt;

            // Invisible proxy keeps the existing progress code (fillAmount) working; mirrored into the HD bar each frame
            RectTransform proxyRt = NewShopRect("BarFill", tRt);
            hudProgressBarFillRt = proxyRt;
            hudProgressBarFill = proxyRt.gameObject.AddComponent<Image>();
            hudProgressBarFill.type = Image.Type.Filled;
            hudProgressBarFill.fillMethod = Image.FillMethod.Horizontal;
            hudProgressBarFill.fillAmount = 0f;
            hudProgressBarFill.color = new Color(1f, 1f, 1f, 0f);
            hudProgressBarFill.raycastTarget = false;
            hudProgressBarFill.enabled = false; // data-only: never render (it has no sprite, so any colour change showed a white box)
            hudProgressBarLabel = CreateText("BarLabel", tRt, "", 16, Color.clear, TextAlignmentOptions.Center, false);
            hudProgressBarLabel.gameObject.SetActive(false);

            Image flag = NewShopRect("FinishFlagIcon", tRt).gameObject.AddComponent<Image>();
            Sprite flagSprite = LoadSpriteFromFile("Resources/UI/Ref_CheckeredFlag.png") ?? LoadSpriteFromFile("Textures/tex_flag_finish.png");
            if (flagSprite != null) flag.sprite = flagSprite;
            flag.preserveAspect = true;
            flag.raycastTarget = false;
            RectTransform fRt = flag.rectTransform;
            fRt.anchorMin = fRt.anchorMax = new Vector2(1f, 0.5f);
            fRt.pivot = new Vector2(1f, 0.5f);
            fRt.anchoredPosition = new Vector2(-12f, 0f);
            fRt.sizeDelta = new Vector2(38f, 38f);

            // Boss skull at the end of the bar
            GameObject bossBadgeObj = new GameObject("BossSkullBadge", typeof(RectTransform));
            bossBadgeObj.transform.SetParent(top, false);
            hudBossBadgeRect = bossBadgeObj.GetComponent<RectTransform>();
            hudBossBadgeRect.anchorMin = hudBossBadgeRect.anchorMax = new Vector2(1f, 1f);
            hudBossBadgeRect.pivot = new Vector2(0.5f, 0.5f);
            hudBossBadgeRect.anchoredPosition = new Vector2(-46f, -140f);
            hudBossBadgeRect.sizeDelta = new Vector2(92f, 92f);

            GameObject glowHalo = new GameObject("GlowHalo", typeof(RectTransform));
            glowHalo.transform.SetParent(bossBadgeObj.transform, false);
            RectTransform haloRt = glowHalo.GetComponent<RectTransform>();
            haloRt.anchorMin = haloRt.anchorMax = new Vector2(0.5f, 0.5f);
            haloRt.sizeDelta = new Vector2(150f, 150f);
            Image haloImg = glowHalo.AddComponent<Image>();
            Sprite haloSprite = LoadSpriteFromFile("Resources/UI/Ref_BossSkull_Glow.png");
            if (haloSprite != null) haloImg.sprite = haloSprite; else haloImg.color = new Color(0.95f, 0.20f, 0.05f, 0.65f);
            haloImg.raycastTarget = false;

            hudBossBadgeImage = bossBadgeObj.AddComponent<Image>();
            Sprite bossSprite = LoadSpriteFromFile("Resources/UI/Ref_BossSkull_HD.png") ?? Resources.Load<Sprite>("UI/BossSkull_HD");
            if (bossSprite != null) hudBossBadgeImage.sprite = bossSprite; else hudBossBadgeImage.color = new Color(0.90f, 0.15f, 0.12f, 1f);
            hudBossBadgeImage.preserveAspect = true;
            hudBossBadgeImage.raycastTarget = false;
            if (Application.isPlaying) StartCoroutine(AnimateBossGlowRoutine(haloRt));

            BuildPauseMenu(canvasRoot);
        }

        private void BuildPauseMenu(Transform canvasRoot)
        {
            Transform old = canvasRoot.Find("PauseMenuPanel");
            if (old != null) SafeDestroy(old.gameObject);

            pauseMenuPanel = new GameObject("PauseMenuPanel", typeof(RectTransform));
            pauseMenuPanel.transform.SetParent(canvasRoot, false);
            RectTransform pr = pauseMenuPanel.GetComponent<RectTransform>();
            ShopStretch(pr);
            Image dim = pauseMenuPanel.AddComponent<Image>();
            dim.color = new Color(0.01f, 0.02f, 0.07f, 0.72f);
            dim.raycastTarget = true; // blocks taps from reaching the game

            RectTransform safe = NewShopRect("SafeArea", pr);
            ShopStretch(safe);
            safe.gameObject.AddComponent<ShopSafeArea>();
            RectTransform body = NewShopRect("Body", safe);
            ShopStretch(body, 30f, 30f, 30f, 30f);

            RectTransform root = NewShopRect("Dialog", body);
            ShopResponsiveFitter fit = root.gameObject.AddComponent<ShopResponsiveFitter>();
            fit.designSize = new Vector2(820f, 1180f);
            fit.maxScale = 1.2f;
            fit.Apply(true);
            pauseDialogRt = root;

            Image frame = NewShopImage("Frame", root, "Shop_Panel_Frame", false);
            frame.type = Image.Type.Sliced;
            ShopPlaceTopLeft(frame.rectTransform, 0f, 60f, 820f, 1120f);

            Image banner = NewMenuImage("Banner", root, "Menu_Banner", false);
            banner.type = Image.Type.Sliced;
            ShopPlaceTopLeft(banner.rectTransform, 160f, 0f, 500f, 120f);
            TextMeshProUGUI title = CreateText("Title", banner.rectTransform, "PAUSED", 62f, Color.white, TextAlignmentOptions.Center, true);
            title.rectTransform.offsetMin = new Vector2(10f, 8f);
            title.rectTransform.offsetMax = new Vector2(-10f, -8f);
            StyleShopTmp(title, new Color(0.02f, 0.08f, 0.32f, 1f), 0.24f, true);

            pauseLevelText = CreateText("LevelInfo", root, "LEVEL 1", 40f, new Color(1f, 0.92f, 0.38f, 1f), TextAlignmentOptions.Center, true);
            ShopPlaceTopLeft(pauseLevelText.rectTransform, 60f, 150f, 700f, 60f);
            StyleShopTmp(pauseLevelText, new Color(0.30f, 0.12f, 0.0f, 1f), 0.24f, false);

            float y = 240f;
            CreatePauseButton(root, "Btn_Resume", "Menu_Btn_Green", "RESUME", y, ResumeGame, new Color(0.03f, 0.25f, 0.07f, 1f)); y += 150f;
            CreatePauseButton(root, "Btn_Restart", "Menu_Btn_Blue", "RESTART", y, PauseRestart, new Color(0.03f, 0.10f, 0.35f, 1f)); y += 150f;
            CreatePauseButton(root, "Btn_LevelMap", "Menu_Btn_Blue", "LEVEL MAP", y, PauseToLevelMap, new Color(0.03f, 0.10f, 0.35f, 1f)); y += 150f;
            CreatePauseButton(root, "Btn_Home", "Menu_Btn_Gold", "HOME", y, PauseToHome, new Color(0.40f, 0.18f, 0.02f, 1f)); y += 160f;

            // Music / Sound toggles
            pauseMusicBtnImg = CreatePauseToggle(root, "Btn_Music", 410f - 150f, y, "MUSIC", () =>
            {
                MobControl.Audio.ZombieSoundManager.Instance?.ToggleMusic();
                RefreshPauseToggles();
            });
            pauseSfxBtnImg = CreatePauseToggle(root, "Btn_Sound", 410f + 150f, y, "SOUND", () =>
            {
                MobControl.Audio.ZombieSoundManager.Instance?.ToggleSFX();
                RefreshPauseToggles();
            });
            RefreshPauseToggles();

            pauseMenuPanel.SetActive(false);
        }

        private void CreatePauseButton(RectTransform root, string name, string sprite, string label, float y, System.Action onClick, Color outline)
        {
            Image img = NewMenuImage(name, root, sprite, false, true);
            RectTransform rt = img.rectTransform;
            ShopPlaceTopLeft(rt, 150f, y, 520f, 130f);
            Button b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => StartCoroutine(ShopPress(rt, onClick)));
            TextMeshProUGUI t = CreateText("Label", rt, label, 52f, Color.white, TextAlignmentOptions.Center, true);
            t.rectTransform.offsetMin = new Vector2(12f, 22f);
            t.rectTransform.offsetMax = new Vector2(-12f, -2f);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            StyleShopTmp(t, outline, 0.26f, false);
        }

        private Image CreatePauseToggle(RectTransform root, string name, float cx, float y, string label, System.Action onClick)
        {
            Image img = NewMenuImage(name, root, "Menu_Btn_Music_On", true, true);
            RectTransform rt = img.rectTransform;
            ShopPlaceTopLeft(rt, cx - 70f, y, 140f, 140f);
            Button b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => StartCoroutine(ShopPress(rt, onClick)));
            TextMeshProUGUI t = CreateText("Caption", root, label, 30f, Color.white, TextAlignmentOptions.Center, true);
            ShopPlaceTopLeft(t.rectTransform, cx - 110f, y + 146f, 220f, 44f);
            StyleShopTmp(t, new Color(0.02f, 0.08f, 0.32f, 1f), 0.24f, false);
            return img;
        }

        private void RefreshPauseToggles()
        {
            var snd = MobControl.Audio.ZombieSoundManager.Instance;
            bool music = snd == null || snd.IsMusicEnabled;
            bool sfx = snd == null || snd.IsSfxEnabled;
            SetMenuSprite(pauseMusicBtnImg, music ? "Menu_Btn_Music_On" : "Menu_Btn_Music_Off");
            SetMenuSprite(pauseSfxBtnImg, sfx ? "Menu_Btn_Sound_On" : "Menu_Btn_Sound_Off");
        }

        public void PauseGame()
        {
            if (isGamePaused || currentState != ScreenState.Gameplay || isFinishedHandled) return;
            if (pauseMenuPanel == null && mainCanvas != null) BuildPauseMenu(mainCanvas.transform);
            if (pauseMenuPanel == null) return;

            isGamePaused = true;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            if (pauseLevelText != null) pauseLevelText.text = $"LEVEL {activeLevel}";
            RefreshPauseToggles();
            pauseMenuPanel.transform.SetAsLastSibling();
            pauseMenuPanel.SetActive(true);
            if (pauseDialogRt != null) StartCoroutine(PunchScale(pauseDialogRt, 0.16f, null));
        }

        public void ResumeGame()
        {
            if (!isGamePaused) return;
            isGamePaused = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        }

        private void ClearPauseState()
        {
            isGamePaused = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        }

        private void PauseRestart()
        {
            ClearPauseState();
            OnReplayClicked();
        }

        private void PauseToLevelMap()
        {
            ClearPauseState();
            OnReturnToMenuClicked(); // reloads the scene and opens the Level Map
        }

        private void PauseToHome()
        {
            ClearPauseState();
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (defeatPanel != null) defeatPanel.SetActive(false);
            startAtHomeScreen = true;
            startAtLevelMap = false;
            startAtLevelSelect = false;
            autoStartMissionOnLoad = false;
            ReloadCurrentScene();
        }

        // Escape / Android back toggles pause during gameplay
        private void HandlePauseInput()
        {
            if (currentState != ScreenState.Gameplay) return;
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            {
                if (isGamePaused) ResumeGame(); else PauseGame();
            }
        }

        // Auto-pause when the app goes to the background
        private void OnApplicationPause(bool paused)
        {
            if (paused && Application.isPlaying) PauseGame();
        }

        private void UpdateHDHealthHud()
        {
            if (playerRunner == null) return;
            int hp = Mathf.Max(0, playerRunner.Health);
            int max = Mathf.Max(1, playerRunner.MaxHealth);
            if (hudHealthText != null) hudHealthText.text = $"HP {hp} / {max}";
            if (hudProgressHDFillRt != null && hudProgressBarFill != null)
                hudProgressHDFillRt.anchorMax = new Vector2(Mathf.Max(0.05f, Mathf.Clamp01(hudProgressBarFill.fillAmount)), 1f);
            if (hudHealthFill != null)
                hudHealthFill.rectTransform.anchorMax = new Vector2(Mathf.Max(0.06f, Mathf.Clamp01((float)hp / max)), 1f);
            if (hudHeartRt != null && hudLastHealth >= 0 && hp < hudLastHealth)
                StartCoroutine(PunchScale(hudHeartRt, 0.18f, null)); // heart flinches on damage
            hudLastHealth = hp;
        }

        private void BuildGameplayHudPanel(Transform canvasRoot)
        {
            gameplayHudPanel = CreateFullPanel("GameplayHud", canvasRoot, Color.clear);
            gameplayHudPanel.GetComponent<Image>().raycastTarget = false;

            // HD top HUD: pause, level, health, progress, boss
            BuildHDGameplayExtras(canvasRoot);

            // 5. Multiplier Blast Pop-up (Shows "×2" popping for 0.5 sec when container is destroyed)
            multiplierPopObj = new GameObject("MultiplierPopBadge");
            multiplierPopObj.transform.SetParent(gameplayHudPanel.transform, false);
            multiplierPopRect = multiplierPopObj.AddComponent<RectTransform>();
            multiplierPopRect.anchorMin = new Vector2(0.5f, 0.5f);
            multiplierPopRect.anchorMax = new Vector2(0.5f, 0.5f);
            multiplierPopRect.pivot = new Vector2(0.5f, 0.5f);
            multiplierPopRect.sizeDelta = new Vector2(320f, 140f);
            multiplierPopRect.anchoredPosition = new Vector2(0f, 60f);

            multiplierPopText = CreateText("PopText", multiplierPopObj.transform, "×2", 84, new Color(0.05f, 1f, 0.95f, 1f), TextAlignmentOptions.Center, true);
            multiplierPopText.rectTransform.anchoredPosition = Vector2.zero;
            multiplierPopObj.SetActive(false);

            // 6. On-Screen Left-Right Steering Joystick removed per user request
            Transform existingJoy = gameplayHudPanel.transform.Find("OnScreenJoystick_Steering");
            if (existingJoy != null)
            {
                if (Application.isPlaying) Destroy(existingJoy.gameObject);
                else DestroyImmediate(existingJoy.gameObject);
            }

            // 7. Soothing "Swipe Left / Right to Start" Prompt
            BuildSwipeToStartPrompt(gameplayHudPanel.transform);

            // 8. Gun Upgrade 15-Second Boost Timer Badge (Sleek pill badge directly below header bar)
            GameObject boostTimerObj = new GameObject("GunUpgradeTimerBadge");
            boostTimerObj.transform.SetParent(gameplayHudPanel.transform, false);
            hudGunUpgradeBadgeRt = boostTimerObj.AddComponent<RectTransform>();
            hudGunUpgradeBadgeRt.anchorMin = new Vector2(0.5f, 1f);
            hudGunUpgradeBadgeRt.anchorMax = new Vector2(0.5f, 1f);
            hudGunUpgradeBadgeRt.pivot = new Vector2(0.5f, 1f);
            hudGunUpgradeBadgeRt.sizeDelta = new Vector2(300f, 62f);
            hudGunUpgradeBadgeRt.anchoredPosition = new Vector2(0f, -232f); // centred under the progress bar

            Image boostBg = boostTimerObj.AddComponent<Image>();
            Sprite boostPill = LoadSpriteFromUI(MenuHDPath + "Menu_Hud_Pill");
            if (boostPill != null) { boostBg.sprite = boostPill; boostBg.type = Image.Type.Sliced; }
            else boostBg.color = new Color(0.06f, 0.12f, 0.22f, 0.88f);
            boostBg.raycastTarget = false;

            hudGunUpgradeText = CreateText("BoostTimerText", boostTimerObj.transform, "15s REMAINING", 28, new Color(0.45f, 0.95f, 1.0f, 1f), TextAlignmentOptions.Center, true);
            hudGunUpgradeText.rectTransform.offsetMin = new Vector2(14f, 4f);
            hudGunUpgradeText.rectTransform.offsetMax = new Vector2(-14f, -4f);
            hudGunUpgradeText.textWrappingMode = TextWrappingModes.NoWrap;
            StyleShopTmp(hudGunUpgradeText, new Color(0.01f, 0.06f, 0.22f, 1f), 0.22f, false);

            boostTimerObj.SetActive(false);
            hudGunUpgradeBadgeObj = boostTimerObj;
        }

        #region Soothing Swipe to Start Prompt (Hyper-Casual Ready State)

        private void BuildSwipeToStartPrompt(Transform parent)
        {
            if (parent == null) return;
            Transform existing = parent.Find("SwipeToStartPrompt");
            if (existing != null)
            {
                swipeToStartPromptObj = existing.gameObject;
                swipeToStartCanvasGroup = swipeToStartPromptObj.GetComponent<CanvasGroup>();
                swipePromptRect = swipeToStartPromptObj.GetComponent<RectTransform>();
                swipePromptTitleText = swipeToStartPromptObj.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
                swipePromptSubText = swipeToStartPromptObj.transform.Find("Subtitle")?.GetComponent<TextMeshProUGUI>();
                swipeLeftChevrons = swipeToStartPromptObj.transform.Find("LeftChevrons")?.GetComponent<TextMeshProUGUI>();
                swipeRightChevrons = swipeToStartPromptObj.transform.Find("RightChevrons")?.GetComponent<TextMeshProUGUI>();
                swipeIndicatorDotRect = swipeToStartPromptObj.transform.Find("GlidingDot")?.GetComponent<RectTransform>();
                return;
            }

            swipeToStartPromptObj = new GameObject("SwipeToStartPrompt");
            swipeToStartPromptObj.transform.SetParent(parent, false);

            swipePromptRect = swipeToStartPromptObj.AddComponent<RectTransform>();
            swipePromptRect.anchorMin = new Vector2(0.5f, 0f);
            swipePromptRect.anchorMax = new Vector2(0.5f, 0f);
            swipePromptRect.pivot = new Vector2(0.5f, 0.5f);
            swipePromptRect.sizeDelta = new Vector2(430f, 80f);
            swipePromptRect.anchoredPosition = new Vector2(0f, 230f);

            swipeToStartCanvasGroup = swipeToStartPromptObj.AddComponent<CanvasGroup>();
            swipeToStartCanvasGroup.alpha = 1f;
            swipeToStartCanvasGroup.interactable = false;
            swipeToStartCanvasGroup.blocksRaycasts = false;

            // 1. Soothing Glass Backdrop (Soft translucent dark tint with delicate white outline)
            Image bgImg = swipeToStartPromptObj.AddComponent<Image>();
            bgImg.color = new Color(0.02f, 0.05f, 0.12f, 0.55f);
            bgImg.raycastTarget = false;

            Outline outline = swipeToStartPromptObj.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.28f); // Translucent soothing white border
            outline.effectDistance = new Vector2(1.5f, 1.5f);

            // 2. Left Animated Chevrons
            swipeLeftChevrons = CreateText("LeftChevrons", swipeToStartPromptObj.transform, "« «", 28f, new Color(1f, 1f, 1f, 0.72f), TextAlignmentOptions.Center, true);
            RectTransform lcRt = swipeLeftChevrons.rectTransform;
            lcRt.sizeDelta = new Vector2(64f, 50f);
            lcRt.anchoredPosition = new Vector2(-160f, 0f);

            // 3. Right Animated Chevrons
            swipeRightChevrons = CreateText("RightChevrons", swipeToStartPromptObj.transform, "» »", 28f, new Color(1f, 1f, 1f, 0.72f), TextAlignmentOptions.Center, true);
            RectTransform rcRt = swipeRightChevrons.rectTransform;
            rcRt.sizeDelta = new Vector2(64f, 50f);
            rcRt.anchoredPosition = new Vector2(160f, 0f);

            // 4. Gliding Translucent Indicator Dot / Glow
            GameObject dotObj = new GameObject("GlidingDot");
            dotObj.transform.SetParent(swipeToStartPromptObj.transform, false);
            swipeIndicatorDotRect = dotObj.AddComponent<RectTransform>();
            swipeIndicatorDotRect.sizeDelta = new Vector2(32f, 6f);
            swipeIndicatorDotRect.anchoredPosition = new Vector2(0f, 26f);
            Image dotImg = dotObj.AddComponent<Image>();
            dotImg.color = new Color(1f, 1f, 1f, 0.55f);
            dotImg.raycastTarget = false;

            // 5. Main Title Text: "SWIPE LEFT / RIGHT"
            // Soothing white with lowered opacity
            swipePromptTitleText = CreateText("Title", swipeToStartPromptObj.transform, "SWIPE LEFT / RIGHT", 19f, new Color(1f, 1f, 1f, 0.88f), TextAlignmentOptions.Center, true);
            swipePromptTitleText.characterSpacing = 8f;
            RectTransform ttRt = swipePromptTitleText.rectTransform;
            ttRt.sizeDelta = new Vector2(300f, 28f);
            ttRt.anchoredPosition = new Vector2(0f, 4f);

            // 6. Subtitle Text: "TO START THE GAME"
            swipePromptSubText = CreateText("Subtitle", swipeToStartPromptObj.transform, "TO START THE GAME", 11.5f, new Color(1f, 1f, 1f, 0.52f), TextAlignmentOptions.Center, true);
            swipePromptSubText.characterSpacing = 14f;
            RectTransform stRt = swipePromptSubText.rectTransform;
            stRt.sizeDelta = new Vector2(300f, 22f);
            stRt.anchoredPosition = new Vector2(0f, -20f);

            swipeToStartPromptObj.SetActive(false);
        }

        public void ShowSwipeToStartPrompt()
        {
            if (gameplayHudPanel == null) return;

            if (swipeToStartPromptObj == null)
            {
                BuildSwipeToStartPrompt(gameplayHudPanel.transform);
            }

            if (swipeToStartPromptObj != null)
            {
                swipeToStartPromptObj.SetActive(true);
                if (swipeToStartCanvasGroup != null)
                {
                    swipeToStartCanvasGroup.alpha = 1f;
                }
                if (swipePromptRect != null)
                {
                    swipePromptRect.localScale = Vector3.one;
                }

                isWaitingForFirstSwipe = true;

                if (swipePromptAnimCoroutine != null) StopCoroutine(swipePromptAnimCoroutine);
                swipePromptAnimCoroutine = StartCoroutine(SwipeToStartAnimationRoutine());
            }
        }

        private IEnumerator SwipeToStartAnimationRoutine()
        {
            while (isWaitingForFirstSwipe && swipeToStartPromptObj != null && swipeToStartPromptObj.activeSelf)
            {
                float time = Time.unscaledTime;

                // 1. Soothing breathing opacity for title & subtitle (white with gentle soft pulse)
                float breathAlpha = 0.72f + 0.20f * Mathf.Sin(time * 3.0f);
                if (swipePromptTitleText != null)
                {
                    swipePromptTitleText.color = new Color(1f, 1f, 1f, breathAlpha);
                }
                if (swipePromptSubText != null)
                {
                    swipePromptSubText.color = new Color(1f, 1f, 1f, breathAlpha * 0.60f);
                }

                // 2. Gliding dot sweeps smoothly left and right
                if (swipeIndicatorDotRect != null)
                {
                    float sweepX = Mathf.Sin(time * 3.2f) * 55f;
                    swipeIndicatorDotRect.anchoredPosition = new Vector2(sweepX, 26f);
                }

                // 3. Chevrons nudge outward and pulse
                float nudge = Mathf.Sin(time * 3.5f) * 6f;
                if (swipeLeftChevrons != null)
                {
                    swipeLeftChevrons.rectTransform.anchoredPosition = new Vector2(-160f - nudge, 0f);
                    swipeLeftChevrons.color = new Color(1f, 1f, 1f, 0.48f + 0.35f * Mathf.Clamp01(-Mathf.Sin(time * 3.2f)));
                }
                if (swipeRightChevrons != null)
                {
                    swipeRightChevrons.rectTransform.anchoredPosition = new Vector2(160f + nudge, 0f);
                    swipeRightChevrons.color = new Color(1f, 1f, 1f, 0.48f + 0.35f * Mathf.Clamp01(Mathf.Sin(time * 3.2f)));
                }

                yield return null;
            }
        }

        // ---------------- Level difficulty curve ----------------
        // Level 1 is slow and relaxed; speed ramps up gradually and only reaches the original full speed at level 8,
        // so early levels are about learning + buying upgrades, not reflexes.
        private const float ROAD_SPEED_LEVEL1 = 1.8f;
        private const float ROAD_SPEED_MAX = 4.2f;
        private const float ZOMBIE_SPEED_SCALE_LEVEL1 = 0.32f; // was 0.45 — level 1 zombies ~30% slower
        private const int FULL_SPEED_LEVEL = 8;

        /// <summary>0 at level 1 .. 1 at FULL_SPEED_LEVEL+ (eased so the first few levels stay gentle).</summary>
        private float LevelDifficulty01
        {
            get
            {
                float t = Mathf.Clamp01((Mathf.Max(1, activeLevel) - 1) / (float)(FULL_SPEED_LEVEL - 1));
                return t * t * (3f - 2f * t) * 0.35f + t * 0.65f; // mostly linear, slightly softer at the start
            }
        }

        /// <summary>How fast the road (and everything on it) rushes towards the player for the current level.</summary>
        public float LevelRoadSpeed => Mathf.Lerp(ROAD_SPEED_LEVEL1, ROAD_SPEED_MAX, LevelDifficulty01);

        /// <summary>Multiplier on zombies' own walk speed for the current level (1 = original).</summary>
        public float LevelZombieSpeedScale => Mathf.Lerp(ZOMBIE_SPEED_SCALE_LEVEL1, 1f, LevelDifficulty01);

        public void StartGameplayFromWaiting()
        {
            if (!isWaitingForFirstSwipe) return;
            isWaitingForFirstSwipe = false;

            Debug.Log("[ZombieRunnerUIManager] First player swipe received -> Starting active gameplay & horde waves!");

            // Stop animation coroutine
            if (swipePromptAnimCoroutine != null)
            {
                StopCoroutine(swipePromptAnimCoroutine);
                swipePromptAnimCoroutine = null;
            }

            // Smooth fade out
            if (swipeFadeOutCoroutine != null) StopCoroutine(swipeFadeOutCoroutine);
            swipeFadeOutCoroutine = StartCoroutine(FadeOutSwipePromptRoutine());

            // 1. Unfreeze player controls & shooting
            if (playerRunner == null)
            {
                playerRunner = PlayerRunnerController.Instance ?? FindAnyObjectByType<PlayerRunnerController>();
            }
            if (playerRunner != null)
            {
                playerRunner.SetWaitingToStart(false);
                playerRunner.SetControllable(true);
            }

            if (!MobControl.Level.ArmyRunLevel.Enabled)
            {
                // 2. Start endless road
                if (MobControl.Level.EndlessRoadManager.Instance != null)
                {
                    MobControl.Level.EndlessRoadManager.Instance.SetSpeed(LevelRoadSpeed);
                    MobControl.Level.EndlessRoadManager.Instance.SetRunning(true);
                    Debug.Log($"[ZombieRunnerUIManager] Level {activeLevel} speed: road {LevelRoadSpeed:F2} m/s, zombie walk x{LevelZombieSpeedScale:F2}");
                }

                // 3. Start Zombie Spawner
                if (MobControl.Crowd.ZombieRoadWaveSpawner.Instance != null)
                {
                    MobControl.Crowd.ZombieRoadWaveSpawner.Instance.StartSpawning();
                }
            }

            // 4. Army Run level starts marching
            MobControl.Level.ArmyRunLevel.OnGameplayStarted(activeLevel);
        }

        private IEnumerator FadeOutSwipePromptRoutine()
        {
            if (swipeToStartPromptObj == null || swipeToStartCanvasGroup == null) yield break;

            float elapsed = 0f;
            float duration = 0.22f;
            Vector3 startScale = Vector3.one;
            Vector3 targetScale = new Vector3(1.06f, 1.06f, 1.06f);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                swipeToStartCanvasGroup.alpha = 1f - t;
                if (swipePromptRect != null)
                {
                    swipePromptRect.localScale = Vector3.Lerp(startScale, targetScale, t);
                }
                yield return null;
            }

            swipeToStartCanvasGroup.alpha = 0f;
            swipeToStartPromptObj.SetActive(false);
        }

        public bool HasAnySwipeStartInput()
        {
            // 1. On-Screen Joystick
            if (OnScreenJoystick.Instance != null && OnScreenJoystick.Instance.IsActive)
            {
                if (Mathf.Abs(OnScreenJoystick.Instance.HorizontalAxis) > 0.04f || OnScreenJoystick.Instance.IsDragging)
                {
                    return true;
                }
            }

            // 2. Touch Screen Swipe
            if (Input.touchCount > 0)
            {
                Touch t = Input.GetTouch(0);
                if (t.phase == TouchPhase.Began || t.phase == TouchPhase.Moved || Mathf.Abs(t.deltaPosition.x) > 2f)
                {
                    return true;
                }
            }

            // 3. Mouse Drag (Editor / PC)
            if (Input.GetMouseButtonDown(0) || (Input.GetMouseButton(0) && Mathf.Abs(Input.GetAxis("Mouse X")) > 0.04f))
            {
                return true;
            }

            // 4. Keyboard Controls
            if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.04f ||
                Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D) ||
                Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow) ||
                Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                return true;
            }

            return false;
        }

        #endregion

        public void UpdateWaveProgress(float progress)
        {
            if (isBossPhase) return;

            if (hudProgressBarFill != null)
            {
                hudProgressBarFill.fillAmount = Mathf.Clamp01(progress);
                hudProgressBarFill.color = Color.white; // Preserve glossy texture sprite
            }
            if (hudProgressHDFillRt != null)
            {
                Image hdFill = hudProgressHDFillRt.GetComponent<Image>();
                if (hdFill != null) hdFill.color = Color.white;
            }

            if (hudProgressBarLabel != null && hudProgressBarLabel.gameObject.activeSelf)
            {
                int remaining = Mathf.Max(0, Mathf.CeilToInt(30f * (1f - progress)));
                hudProgressBarLabel.text = remaining > 0 ? $"HORDE: {remaining}s" : "BOSS ARRIVING!";
            }
        }

        public void TriggerBossAlert()
        {
            if (bossPulseCoroutine != null) StopCoroutine(bossPulseCoroutine);
            bossPulseCoroutine = StartCoroutine(PulseBossSkullBadge());
        }

        private IEnumerator PulseBossSkullBadge()
        {
            if (hudBossBadgeRect == null) yield break;
            Vector3 baseScale = Vector3.one;
            for (int i = 0; i < 4; i++)
            {
                hudBossBadgeRect.localScale = baseScale * 1.35f;
                yield return new WaitForSeconds(0.12f);
                hudBossBadgeRect.localScale = baseScale;
                yield return new WaitForSeconds(0.12f);
            }
        }

        private IEnumerator AnimateBossGlowRoutine(RectTransform haloRt)
        {
            if (haloRt == null) yield break;
            Vector3 baseScale = Vector3.one;
            while (haloRt != null)
            {
                float t = Mathf.PingPong(Time.unscaledTime * 1.8f, 1f);
                float s = Mathf.Lerp(0.94f, 1.10f, t);
                haloRt.localScale = baseScale * s;
                yield return null;
            }
        }

        public void OnBossSpawned(MobControl.Crowd.MobUnit bossUnit, int maxHp = 60)
        {
            isBossPhase = true;
            MobControl.Audio.ZombieSoundManager.Instance?.PlayBossSpawn();

            if (hudProgressBarFill != null)
            {
                hudProgressBarFill.fillAmount = 1.0f;
                hudProgressBarFill.color = new Color(0.95f, 0.18f, 0.14f, 1f); // Fiery Red
                Image hdFill = hudProgressHDFillRt != null ? hudProgressHDFillRt.GetComponent<Image>() : null;
                if (hdFill != null) hdFill.color = new Color(1f, 0.35f, 0.3f, 1f); // tint the visible HD bar for the boss phase
            }

            if (hudProgressBarLabel != null)
            {
                hudProgressBarLabel.text = $"MONSTER POWER: {maxHp} / {maxHp}";
            }

            if (bossUnit != null)
            {
                bossUnit.OnMobHealthChanged += (current, max) =>
                {
                    if (hudProgressBarFill != null)
                    {
                        hudProgressBarFill.fillAmount = (float)current / Mathf.Max(1, max);
                    }
                    if (hudProgressBarLabel != null)
                    {
                        hudProgressBarLabel.text = $"MONSTER POWER: {current} / {max}";
                    }
                };

                bossUnit.OnMobDied += (unit) =>
                {
                    StartCoroutine(DelayedVictoryRoutine());
                };
            }
        }

        private IEnumerator DelayedVictoryRoutine()
        {
            yield return new WaitForSeconds(1.2f);
            TriggerVictory();
        }

        public void ShowMultiplierPop(string text = "×2")
        {
            if (multiplierPopObj != null)
            {
                StartCoroutine(MultiplierPopCoroutine(text));
            }
        }

        private IEnumerator MultiplierPopCoroutine(string text)
        {
            if (multiplierPopObj == null) yield break;
            multiplierPopText.text = text;
            multiplierPopObj.SetActive(true);

            float elapsed = 0f;
            float duration = 0.50f; // Exactly 0.5s pop duration as requested
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float scale = (t < 0.4f)
                    ? Mathf.Lerp(0.5f, 1.6f, t / 0.4f)
                    : Mathf.Lerp(1.6f, 1.0f, (t - 0.4f) / 0.6f);
                multiplierPopRect.localScale = Vector3.one * scale;
                yield return null;
            }

            multiplierPopObj.SetActive(false);
        }

        private void EnsureGunUpgradeTimerBadge()
        {
            if (hudGunUpgradeBadgeObj != null) return;
            if (gameplayHudPanel == null)
            {
                if (mainCanvas != null)
                {
                    Transform t = mainCanvas.transform.Find("GameplayHud");
                    if (t != null) gameplayHudPanel = t.gameObject;
                }
            }
            if (gameplayHudPanel == null) return;

            Transform existing = gameplayHudPanel.transform.Find("GunUpgradeTimerBadge");
            if (existing != null)
            {
                hudGunUpgradeBadgeObj = existing.gameObject;
                hudGunUpgradeText = existing.GetComponentInChildren<TextMeshProUGUI>();
                return;
            }

            GameObject boostTimerObj = new GameObject("GunUpgradeTimerBadge");
            boostTimerObj.transform.SetParent(gameplayHudPanel.transform, false);
            hudGunUpgradeBadgeRt = boostTimerObj.AddComponent<RectTransform>();
            hudGunUpgradeBadgeRt.anchorMin = new Vector2(0.5f, 1f);
            hudGunUpgradeBadgeRt.anchorMax = new Vector2(0.5f, 1f);
            hudGunUpgradeBadgeRt.pivot = new Vector2(0.5f, 1f);
            hudGunUpgradeBadgeRt.sizeDelta = new Vector2(240f, 84f);
            hudGunUpgradeBadgeRt.anchoredPosition = new Vector2(0f, -250f);

            // Featured PowerUp Badge Graphic
            GameObject badgeImgObj = new GameObject("BadgeGraphic");
            badgeImgObj.transform.SetParent(boostTimerObj.transform, false);
            RectTransform badgeImgRt = badgeImgObj.AddComponent<RectTransform>();
            badgeImgRt.anchorMin = new Vector2(0f, 0.36f);
            badgeImgRt.anchorMax = new Vector2(1f, 1f);
            badgeImgRt.offsetMin = Vector2.zero;
            badgeImgRt.offsetMax = Vector2.zero;
            Image badgeImg = badgeImgObj.AddComponent<Image>();
            Sprite pbSprite = Resources.Load<Sprite>("UI/PowerUps/purple_boost_15s")
                           ?? Resources.Load<Sprite>("UI/PowerUps/rapid_fire");
            if (pbSprite != null) badgeImg.sprite = pbSprite;
            badgeImg.preserveAspect = true;
            badgeImg.raycastTarget = false;

            // Timer Capsule Pill
            GameObject pillObj = new GameObject("TimerPill");
            pillObj.transform.SetParent(boostTimerObj.transform, false);
            RectTransform pillRt = pillObj.AddComponent<RectTransform>();
            pillRt.anchorMin = new Vector2(0.08f, 0f);
            pillRt.anchorMax = new Vector2(0.92f, 0.36f);
            pillRt.offsetMin = Vector2.zero;
            pillRt.offsetMax = Vector2.zero;
            Image pillImg = pillObj.AddComponent<Image>();
            pillImg.color = new Color(0.12f, 0.04f, 0.22f, 0.90f);
            pillImg.raycastTarget = false;

            hudGunUpgradeText = CreateText("BoostTimerText", pillObj.transform, "15s REMAINING", 20, new Color(1f, 0.95f, 0.35f, 1f), TextAlignmentOptions.Center, true);
            StyleShopTmp(hudGunUpgradeText, new Color(0.2f, 0.05f, 0.3f, 1f), 0.2f, false);
            hudGunUpgradeText.rectTransform.anchorMin = Vector2.zero;
            hudGunUpgradeText.rectTransform.anchorMax = Vector2.one;
            hudGunUpgradeText.rectTransform.offsetMin = Vector2.zero;
            hudGunUpgradeText.rectTransform.offsetMax = Vector2.zero;

            boostTimerObj.SetActive(false);
            hudGunUpgradeBadgeObj = boostTimerObj;
        }

        public void ShowGunUpgradeTimer(float duration = 15f)
        {
            if (hudGunUpgradeBadgeObj == null) EnsureGunUpgradeTimerBadge();
            if (hudGunUpgradeBadgeObj != null)
            {
                hudGunUpgradeBadgeObj.SetActive(true);
                UpdateGunUpgradeTimer(duration);
            }
        }

        public void UpdateGunUpgradeTimer(float remaining)
        {
            if (hudGunUpgradeBadgeObj == null && remaining > 0f) EnsureGunUpgradeTimerBadge();
            if (hudGunUpgradeBadgeObj != null && hudGunUpgradeText != null)
            {
                if (remaining > 0f)
                {
                    if (!hudGunUpgradeBadgeObj.activeSelf) hudGunUpgradeBadgeObj.SetActive(true);
                    int secs = Mathf.CeilToInt(remaining);
                    hudGunUpgradeText.text = $"{secs}s REMAINING";
                }
                else
                {
                    hudGunUpgradeBadgeObj.SetActive(false);
                }
            }
        }

        public void HideGunUpgradeTimer()
        {
            if (hudGunUpgradeBadgeObj != null)
            {
                hudGunUpgradeBadgeObj.SetActive(false);
            }
        }

        private static Sprite CreateRoundedRectSprite(int width, int height, int radius, Color fillColor, Color borderColor, int borderWidth = 3)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = 0;
                    if (x < radius) dx = radius - x;
                    else if (x > width - 1 - radius) dx = x - (width - 1 - radius);

                    float dy = 0;
                    if (y < radius) dy = radius - y;
                    else if (y > height - 1 - radius) dy = y - (height - 1 - radius);

                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist <= radius - borderWidth)
                    {
                        float t = (float)y / height;
                        Color col = Color.Lerp(fillColor * 0.85f, fillColor * 1.15f, t);
                        tex.SetPixel(x, y, col);
                    }
                    else if (dist <= radius)
                    {
                        tex.SetPixel(x, y, borderColor);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        }

        private static Sprite CreateCircleSprite(int size, Color fillColor, Color borderColor, int borderThickness = 3)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            float center = (size - 1) * 0.5f;
            float radius = center;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (dist <= radius - borderThickness)
                    {
                        tex.SetPixel(x, y, fillColor);
                    }
                    else if (dist <= radius)
                    {
                        tex.SetPixel(x, y, borderColor);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private static Sprite CreateDiamondSprite(int size, Color fillColor, Color borderColor)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            float center = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float manhattan = Mathf.Abs(x - center) + Mathf.Abs(y - center);
                    if (manhattan <= center - 3f)
                    {
                        tex.SetPixel(x, y, fillColor);
                    }
                    else if (manhattan <= center)
                    {
                        tex.SetPixel(x, y, borderColor);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private static Sprite LoadSpriteFromFile(string relPath)
        {
            try
            {
#if UNITY_EDITOR && !UNITY_ANDROID
                string fullPath = System.IO.Path.Combine(Application.dataPath, relPath);
                if (System.IO.File.Exists(fullPath))
                {
                    byte[] bytes = System.IO.File.ReadAllBytes(fullPath);
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (tex.LoadImage(bytes))
                    {
                        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    }
                }
#endif

                // 1. Try Resources.Load
                string resPath = relPath;
                if (resPath.StartsWith("Resources/")) resPath = resPath.Substring("Resources/".Length);
                if (resPath.EndsWith(".png") || resPath.EndsWith(".jpg"))
                    resPath = resPath.Substring(0, resPath.LastIndexOf('.'));

                Sprite resSprite = Resources.Load<Sprite>(resPath);
                if (resSprite != null) return resSprite;

                Texture2D resTex = Resources.Load<Texture2D>(resPath);
                if (resTex != null)
                {
                    return Sprite.Create(resTex, new Rect(0, 0, resTex.width, resTex.height), new Vector2(0.5f, 0.5f));
                }
            }
            catch { }
            return null;
        }

        public void RebuildVictoryPanel()
        {
            if (victoryPanel != null)
            {
                SafeDestroy(victoryPanel);
                victoryPanel = null;
            }
            if (mainCanvas != null)
            {
                BuildVictoryPanel(mainCanvas.transform);
            }
        }

        private static Sprite s_victoryFadeSprite;

        /// <summary>
        /// Letterbox for the 3:4 victory card on tall screens: a solid band outside the card edge plus a short
        /// gradient just inside it, both in one dark colour, so the card melts into the band with no hard seam.
        /// </summary>
        private static void AddVictoryEdgeFade(RectTransform card, bool top)
        {
            if (s_victoryFadeSprite == null)
            {
                var tex = new Texture2D(1, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                for (int y = 0; y < 64; y++)
                {
                    float t = y / 63f; // alpha 1 at the bottom row → 0 at the top row (smoothstep)
                    tex.SetPixel(0, y, new Color(1f, 1f, 1f, 1f - t * t * (3f - 2f * t)));
                }
                tex.Apply();
                s_victoryFadeSprite = Sprite.Create(tex, new Rect(0, 0, 1, 64), new Vector2(0.5f, 0.5f));
            }

            Color band = new Color(0.035f, 0.065f, 0.105f, 1f);
            float edge = top ? 1f : 0f;
            float inner = top ? 0.035f : 0.05f; // fraction of card height the fade reaches inside the card

            // Solid band from the card edge outward (generously past the screen edge)
            GameObject solid = new GameObject(top ? "EdgeBand_Top" : "EdgeBand_Bottom", typeof(RectTransform));
            solid.transform.SetParent(card, false);
            RectTransform srt = solid.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(-0.5f, edge);
            srt.anchorMax = new Vector2(1.5f, edge);
            srt.pivot = new Vector2(0.5f, top ? 0f : 1f);
            srt.sizeDelta = new Vector2(0f, 1200f);
            srt.anchoredPosition = Vector2.zero;
            Image simg = solid.AddComponent<Image>();
            simg.color = band;
            simg.raycastTarget = false;

            // Gradient inside the card: opaque at the edge, clear toward the content
            GameObject fade = new GameObject(top ? "EdgeFade_Top" : "EdgeFade_Bottom", typeof(RectTransform));
            fade.transform.SetParent(card, false);
            RectTransform frt = fade.GetComponent<RectTransform>();
            frt.anchorMin = new Vector2(-0.5f, top ? 1f - inner : 0f);
            frt.anchorMax = new Vector2(1.5f, top ? 1f : inner);
            frt.offsetMin = Vector2.zero;
            frt.offsetMax = Vector2.zero;
            if (top) frt.localScale = new Vector3(1f, -1f, 1f); // opaque end up against the top edge
            Image fimg = fade.AddComponent<Image>();
            fimg.sprite = s_victoryFadeSprite;
            fimg.color = band;
            fimg.raycastTarget = false;

            // Keep both under the card's text/stars/buttons
            fade.transform.SetAsFirstSibling();
            solid.transform.SetAsFirstSibling();
        }

        private void BuildVictoryPanel(Transform canvasRoot)
        {
            victoryPanel = CreateFullPanel("VictoryPanel", canvasRoot, Color.clear);

            // 1. Semi-Transparent Cinematic Focus Vignette over the real 3D game level
            GameObject vigObj = new GameObject("VictoryVignette");
            vigObj.transform.SetParent(victoryPanel.transform, false);
            RectTransform vigRt = vigObj.AddComponent<RectTransform>();
            vigRt.anchorMin = Vector2.zero;
            vigRt.anchorMax = Vector2.one;
            vigRt.offsetMin = Vector2.zero;
            vigRt.offsetMax = Vector2.zero;
            Image vigImg = vigObj.AddComponent<Image>();
            vigImg.color = new Color(0.02f, 0.04f, 0.08f, 0.86f);
            vigImg.raycastTarget = true;

            // 2. Master Responsive Card (AspectRatioFitter FitInParent with 1024x1365 aspect ~0.75018f)
            GameObject masterCard = new GameObject("VictoryMasterCard");
            masterCard.transform.SetParent(victoryPanel.transform, false);
            victoryCardRect = masterCard.AddComponent<RectTransform>();
            victoryCardRect.anchorMin = new Vector2(0.02f, 0.02f);
            victoryCardRect.anchorMax = new Vector2(0.98f, 0.98f);
            victoryCardRect.offsetMin = Vector2.zero;
            victoryCardRect.offsetMax = Vector2.zero;

            AspectRatioFitter arf = masterCard.AddComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            arf.aspectRatio = 1024f / 1365f;

            // The card art is an opaque 3:4 image; on tall phones it left hard-edged bands of undimmed level above
            // and below it. Soft fades bleed the card's dark edges out into the vignette so the screen reads as one piece.
            AddVictoryEdgeFade(victoryCardRect, true);
            AddVictoryEdgeFade(victoryCardRect, false);

            Image cardImg = masterCard.AddComponent<Image>();
            Sprite masterSp = LoadSpriteFromFile("Resources/UI/VictoryScreen/00_master_victory_modal.png")
                           ?? LoadSpriteFromFile("Resources/UI/VictoryScreen/00_full_victory_reference.png");
            if (masterSp != null)
            {
                cardImg.sprite = masterSp;
                cardImg.color = Color.white;
            }
            cardImg.preserveAspect = true;
            cardImg.raycastTarget = false;

            // 3. Dynamic Subtitle: "PERFECT RUN!"
            victorySubText = CreateText("VictorySub", masterCard.transform, "PERFECT RUN!", 24, new Color(0.85f, 0.92f, 1f, 0.95f), TextAlignmentOptions.Center, true);
            victorySubText.raycastTarget = false;
            try
            {
                if (victorySubText.fontSharedMaterial != null)
                {
                    victorySubText.outlineWidth = 0.2f;
                    victorySubText.outlineColor = new Color(0.02f, 0.05f, 0.12f, 1f);
                }
            }
            catch {}
            RectTransform vsRt = victorySubText.GetComponent<RectTransform>();
            vsRt.anchorMin = new Vector2(0.215f, 0.513f);
            vsRt.anchorMax = new Vector2(0.785f, 0.549f);
            vsRt.offsetMin = Vector2.zero;
            vsRt.offsetMax = Vector2.zero;
            victorySubText.gameObject.SetActive(true);

            // 4. Dynamic Coin Reward Count: "+100 COINS"
            victoryRewardText = CreateText("RewardText", masterCard.transform, "+100 COINS", 36, new Color(1f, 0.92f, 0.35f, 1f), TextAlignmentOptions.Center, true);
            victoryRewardText.raycastTarget = false;
            try
            {
                if (victoryRewardText.fontSharedMaterial != null)
                {
                    victoryRewardText.outlineWidth = 0.28f;
                    victoryRewardText.outlineColor = new Color(0.18f, 0.08f, 0.02f, 1f);
                }
            }
            catch {}
            RectTransform vrRt = victoryRewardText.GetComponent<RectTransform>();
            vrRt.anchorMin = new Vector2(0.410f, 0.320f);
            vrRt.anchorMax = new Vector2(0.745f, 0.372f);
            vrRt.offsetMin = Vector2.zero;
            vrRt.offsetMax = Vector2.zero;
            victoryRewardText.gameObject.SetActive(true);

            // 5. Dynamic Star Slots
            victoryStarSlots.Clear();
            Vector2[] starMin = new Vector2[]
            {
                new Vector2(0.283f, 0.396f),
                new Vector2(0.419f, 0.404f),
                new Vector2(0.581f, 0.396f)
            };
            Vector2[] starMax = new Vector2[]
            {
                new Vector2(0.444f, 0.509f),
                new Vector2(0.605f, 0.525f),
                new Vector2(0.742f, 0.509f)
            };

            Sprite goldStarSp = LoadSpriteFromFile("Resources/UI/VictoryScreen/victory_star_gold.png");
            Sprite dimStarSp = LoadSpriteFromFile("Resources/UI/VictoryScreen/victory_star_dim.png");

            for (int i = 0; i < 3; i++)
            {
                GameObject slot = new GameObject($"StarSlot_{i + 1}");
                slot.transform.SetParent(masterCard.transform, false);
                RectTransform slotRt = slot.AddComponent<RectTransform>();
                slotRt.anchorMin = starMin[i];
                slotRt.anchorMax = starMax[i];
                slotRt.offsetMin = Vector2.zero;
                slotRt.offsetMax = Vector2.zero;

                // Dim star socket (always present, visible when unearned)
                GameObject dimObj = new GameObject("DimStar");
                dimObj.transform.SetParent(slot.transform, false);
                RectTransform dimRt = dimObj.AddComponent<RectTransform>();
                dimRt.anchorMin = Vector2.zero;
                dimRt.anchorMax = Vector2.one;
                dimRt.offsetMin = Vector2.zero;
                dimRt.offsetMax = Vector2.zero;
                Image dimImg = dimObj.AddComponent<Image>();
                if (dimStarSp != null) dimImg.sprite = dimStarSp;
                dimImg.color = Color.white;
                dimImg.preserveAspect = true;
                dimImg.raycastTarget = false;
                dimObj.SetActive(true);

                // Gold star overlay (animated punch-in when earned)
                GameObject goldObj = new GameObject("GoldStar");
                goldObj.transform.SetParent(slot.transform, false);
                RectTransform goldRt = goldObj.AddComponent<RectTransform>();
                goldRt.anchorMin = Vector2.zero;
                goldRt.anchorMax = Vector2.one;
                goldRt.offsetMin = Vector2.zero;
                goldRt.offsetMax = Vector2.zero;
                Image goldImg = goldObj.AddComponent<Image>();
                if (goldStarSp != null) goldImg.sprite = goldStarSp;
                goldImg.color = Color.white;
                goldImg.preserveAspect = true;
                goldImg.raycastTarget = false;
                goldObj.SetActive(false);

                victoryStarSlots.Add(new StarSlotRef
                {
                    slot = slotRt,
                    dimImg = dimImg,
                    goldImg = goldImg,
                    goldRt = goldRt
                });
            }

            // 6. Interactive Button 1: NEXT LEVEL (Green 3D Button)
            GameObject nextObj = new GameObject("NextLevelButton");
            nextObj.transform.SetParent(masterCard.transform, false);
            victoryNextRect = nextObj.AddComponent<RectTransform>();
            victoryNextRect.anchorMin = new Vector2(0.19f, 0.216f);
            victoryNextRect.anchorMax = new Vector2(0.81f, 0.300f);
            victoryNextRect.offsetMin = Vector2.zero;
            victoryNextRect.offsetMax = Vector2.zero;

            Image nextImg = nextObj.AddComponent<Image>();
            nextImg.color = new Color(1f, 1f, 1f, 0.001f);
            nextImg.raycastTarget = true;

            victoryNextButton = nextObj.AddComponent<Button>();
            victoryNextButton.targetGraphic = nextImg;
            victoryNextButton.interactable = true;
            ColorBlock nextCb = victoryNextButton.colors;
            nextCb.normalColor = new Color(1f, 1f, 1f, 0.001f);
            nextCb.highlightedColor = new Color(1f, 1f, 1f, 0.15f);
            nextCb.pressedColor = new Color(0f, 0f, 0f, 0.25f);
            nextCb.fadeDuration = 0.08f;
            victoryNextButton.colors = nextCb;
            victoryNextButton.onClick.AddListener(() =>
            {
                ZombieSoundManager.Instance?.PlayUIClick();
                OnNextLevelClicked();
            });

            // 7. Interactive Button 2: REPLAY MISSION (Blue 3D Button)
            GameObject replayObj = new GameObject("ReplayMissionButton");
            replayObj.transform.SetParent(masterCard.transform, false);
            victoryReplayRect = replayObj.AddComponent<RectTransform>();
            victoryReplayRect.anchorMin = new Vector2(0.19f, 0.132f);
            victoryReplayRect.anchorMax = new Vector2(0.81f, 0.212f);
            victoryReplayRect.offsetMin = Vector2.zero;
            victoryReplayRect.offsetMax = Vector2.zero;

            Image replayImg = replayObj.AddComponent<Image>();
            replayImg.color = new Color(1f, 1f, 1f, 0.001f);
            replayImg.raycastTarget = true;

            Button replayBtn = replayObj.AddComponent<Button>();
            replayBtn.targetGraphic = replayImg;
            replayBtn.interactable = true;
            ColorBlock replayCb = replayBtn.colors;
            replayCb.normalColor = new Color(1f, 1f, 1f, 0.001f);
            replayCb.highlightedColor = new Color(1f, 1f, 1f, 0.15f);
            replayCb.pressedColor = new Color(0f, 0f, 0f, 0.25f);
            replayCb.fadeDuration = 0.08f;
            replayBtn.colors = replayCb;
            replayBtn.onClick.AddListener(() =>
            {
                ZombieSoundManager.Instance?.PlayUIClick();
                OnReplayClicked();
            });

            // 8. Interactive Button 3: MISSION MENU (Slate Blue Button)
            GameObject menuObj = new GameObject("MissionMenuButton");
            menuObj.transform.SetParent(masterCard.transform, false);
            victoryMenuRect = menuObj.AddComponent<RectTransform>();
            victoryMenuRect.anchorMin = new Vector2(0.19f, 0.044f);
            victoryMenuRect.anchorMax = new Vector2(0.81f, 0.128f);
            victoryMenuRect.offsetMin = Vector2.zero;
            victoryMenuRect.offsetMax = Vector2.zero;

            Image menuImg = menuObj.AddComponent<Image>();
            menuImg.color = new Color(1f, 1f, 1f, 0.001f);
            menuImg.raycastTarget = true;

            Button menuBtn = menuObj.AddComponent<Button>();
            menuBtn.targetGraphic = menuImg;
            menuBtn.interactable = true;
            ColorBlock menuCb = menuBtn.colors;
            menuCb.normalColor = new Color(1f, 1f, 1f, 0.001f);
            menuCb.highlightedColor = new Color(1f, 1f, 1f, 0.15f);
            menuCb.pressedColor = new Color(0f, 0f, 0f, 0.25f);
            menuCb.fadeDuration = 0.08f;
            menuBtn.colors = menuCb;
            menuBtn.onClick.AddListener(() =>
            {
                ZombieSoundManager.Instance?.PlayUIClick();
                OnReturnToMenuClicked();
            });
        }

        public void RebuildDefeatPanel()
        {
            if (defeatPanel != null)
            {
                SafeDestroy(defeatPanel);
                defeatPanel = null;
            }
            if (mainCanvas != null)
            {
                BuildDefeatPanel(mainCanvas.transform);
            }
        }

        private void BuildDefeatPanel(Transform canvasRoot)
        {
            defeatPanel = CreateFullPanel("DefeatPanel", canvasRoot, Color.clear);

            // 1. Semi-Transparent Cinematic Focus Vignette over the real 3D game level
            GameObject vigObj = new GameObject("DefeatVignette");
            vigObj.transform.SetParent(defeatPanel.transform, false);
            RectTransform vigRt = vigObj.AddComponent<RectTransform>();
            vigRt.anchorMin = Vector2.zero;
            vigRt.anchorMax = Vector2.one;
            vigRt.offsetMin = Vector2.zero;
            vigRt.offsetMax = Vector2.zero;
            Image vigImg = vigObj.AddComponent<Image>();
            vigImg.color = new Color(0.02f, 0.04f, 0.08f, 0.86f);
            vigImg.raycastTarget = true;

            // 2. Master Responsive Card (AspectRatioFitter FitInParent with 1086x1448 reference aspect)
            GameObject masterCard = new GameObject("DefeatMasterCard");
            masterCard.transform.SetParent(defeatPanel.transform, false);
            RectTransform cardRt = masterCard.AddComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.02f, 0.02f);
            cardRt.anchorMax = new Vector2(0.98f, 0.98f);
            cardRt.offsetMin = Vector2.zero;
            cardRt.offsetMax = Vector2.zero;

            AspectRatioFitter arf = masterCard.AddComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            arf.aspectRatio = 1086f / 1448f; // Exactly 0.75

            Image cardImg = masterCard.AddComponent<Image>();
            Sprite masterSp = LoadSpriteFromFile("Resources/UI/DefeatScreen/00_clean_defeat_modal.png")
                           ?? LoadSpriteFromFile("Resources/UI/DefeatScreen/00_full_reference_feathered.png");
            if (masterSp != null)
            {
                cardImg.sprite = masterSp;
                cardImg.color = Color.white;
            }
            cardImg.preserveAspect = true;
            cardImg.raycastTarget = false;

            // 3. Dynamic Title (rendered on the clean red stone banner)
            defeatTitleText = CreateText("DefeatTitle", masterCard.transform, "CRASHED!", 58, Color.white, TextAlignmentOptions.Center, true);
            defeatTitleText.raycastTarget = false;
            try
            {
                if (defeatTitleText.fontSharedMaterial != null)
                {
                    defeatTitleText.outlineWidth = 0.28f;
                    defeatTitleText.outlineColor = new Color(0.12f, 0.02f, 0.04f, 1f);
                }
            }
            catch {}
            RectTransform dtRt = defeatTitleText.GetComponent<RectTransform>();
            dtRt.anchorMin = new Vector2(0.18f, 0.472f);
            dtRt.anchorMax = new Vector2(0.82f, 0.565f);
            dtRt.offsetMin = Vector2.zero;
            dtRt.offsetMax = Vector2.zero;
            defeatTitleText.gameObject.SetActive(true);

            // 4. Dynamic Subtitle (rendered on the clean dark slate panel)
            defeatSubText = CreateText("DefeatSub", masterCard.transform, "You crashed into a wrecked burned car.", 30, new Color(0.95f, 0.96f, 0.98f, 1f), TextAlignmentOptions.Center, true);
            defeatSubText.raycastTarget = false;
            RectTransform dsRt = defeatSubText.GetComponent<RectTransform>();
            dsRt.anchorMin = new Vector2(0.18f, 0.360f);
            dsRt.anchorMax = new Vector2(0.82f, 0.455f);
            dsRt.offsetMin = Vector2.zero;
            dsRt.offsetMax = Vector2.zero;
            defeatSubText.gameObject.SetActive(true);

            // 5. Interactive Button 1: TRY AGAIN (Golden Yellow Button)
            GameObject retryObj = new GameObject("RetryButton");
            retryObj.transform.SetParent(masterCard.transform, false);
            defeatRetryRect = retryObj.AddComponent<RectTransform>();
            defeatRetryRect.anchorMin = new Vector2(0.161f, 0.265f);
            defeatRetryRect.anchorMax = new Vector2(0.838f, 0.365f);
            defeatRetryRect.offsetMin = Vector2.zero;
            defeatRetryRect.offsetMax = Vector2.zero;

            Image retryImg = retryObj.AddComponent<Image>();
            retryImg.color = new Color(1f, 1f, 1f, 0.001f);
            retryImg.raycastTarget = true;

            Button retryBtn = retryObj.AddComponent<Button>();
            retryBtn.targetGraphic = retryImg;
            retryBtn.interactable = true;
            ColorBlock retryCb = retryBtn.colors;
            retryCb.normalColor = new Color(1f, 1f, 1f, 0.001f);
            retryCb.highlightedColor = new Color(1f, 1f, 1f, 0.15f);
            retryCb.pressedColor = new Color(0f, 0f, 0f, 0.25f);
            retryCb.fadeDuration = 0.08f;
            retryBtn.colors = retryCb;
            retryBtn.onClick.AddListener(() =>
            {
                ZombieSoundManager.Instance?.PlayUIClick();
                OnReplayClicked();
            });

            // 6. Interactive Button 2: MISSION MENU (Slate Blue Button)
            GameObject menuObj = new GameObject("MenuButton");
            menuObj.transform.SetParent(masterCard.transform, false);
            defeatMenuRect = menuObj.AddComponent<RectTransform>();
            defeatMenuRect.anchorMin = new Vector2(0.198f, 0.123f);
            defeatMenuRect.anchorMax = new Vector2(0.801f, 0.220f);
            defeatMenuRect.offsetMin = Vector2.zero;
            defeatMenuRect.offsetMax = Vector2.zero;

            Image menuImg = menuObj.AddComponent<Image>();
            menuImg.color = new Color(1f, 1f, 1f, 0.001f);
            menuImg.raycastTarget = true;

            Button menuBtn = menuObj.AddComponent<Button>();
            menuBtn.targetGraphic = menuImg;
            menuBtn.interactable = true;
            ColorBlock menuCb = menuBtn.colors;
            menuCb.normalColor = new Color(1f, 1f, 1f, 0.001f);
            menuCb.highlightedColor = new Color(1f, 1f, 1f, 0.15f);
            menuCb.pressedColor = new Color(0f, 0f, 0f, 0.25f);
            menuCb.fadeDuration = 0.08f;
            menuBtn.colors = menuCb;
            menuBtn.onClick.AddListener(() =>
            {
                ZombieSoundManager.Instance?.PlayUIClick();
                OnReturnToMenuClicked();
            });
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
                    SafeDestroy(mainCanvas.gameObject);
                    mainCanvas = null;
                }
                for (int i = transform.childCount - 1; i >= 0; i--)
                {
                    Transform child = transform.GetChild(i);
                    if (child.name.Contains("Canvas") || child.GetComponent<Canvas>() != null)
                    {
                        SafeDestroy(child.gameObject);
                    }
                }
                BuildUIHierarchy();
            }
            else
            {
                EnsureLevelMapBound();
                if (gameplayHudPanel != null && swipeToStartPromptObj == null)
                {
                    BuildSwipeToStartPrompt(gameplayHudPanel.transform);
                }
            }

            if (mainCanvas != null)
            {
                HookAllButtonsWithSound(mainCanvas.gameObject);
            }
        }

        private void SwitchToPanel(GameObject targetPanel)
        {
            EnsureUIBuilt();

            // Re-resolve targetPanel based on currentState if targetPanel is null
            if (targetPanel == null)
            {
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
            }
            else
            {
                if (targetPanel == homeScreenPanel) currentState = ScreenState.HomeScreen;
                else if (targetPanel == charactersPanel) currentState = ScreenState.Characters;
                else if (targetPanel == levelMapPanel) currentState = ScreenState.LevelMap;
                else if (targetPanel == upgradePanel) currentState = ScreenState.Upgrade;
                else if (targetPanel == weaponsPanel) currentState = ScreenState.Weapons;
                else if (targetPanel == shopPanel) currentState = ScreenState.Shop;
                else if (targetPanel == missionsPanel) currentState = ScreenState.Missions;
                else if (targetPanel == dailyPanel) currentState = ScreenState.Daily;
                else if (targetPanel == gameplayHudPanel) currentState = ScreenState.Gameplay;
                else if (targetPanel == victoryPanel) currentState = ScreenState.Victory;
                else if (targetPanel == defeatPanel) currentState = ScreenState.Defeat;
            }

            if (MobControl.Audio.ZombieSoundManager.Instance != null)
            {
                bool inCombat = (currentState == ScreenState.Gameplay);
                MobControl.Audio.ZombieSoundManager.Instance.SetGameplayCombatMode(inCombat);
            }

            Debug.Log($"[ZombieRunnerUIManager] SwitchToPanel -> Target: {(targetPanel != null ? targetPanel.name : "NULL")}, CurrentState: {currentState}");

            // 1. Purge any duplicate canvases in scene/DontDestroyOnLoad
            Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            foreach (var c in allCanvases)
            {
                if (c != null && c != mainCanvas && c.name.Contains("ZombieRunnerCanvas"))
                {
                    Debug.Log($"[ZombieRunnerUIManager] Purging duplicate canvas: {c.gameObject.name}");
                    SafeDestroy(c.gameObject);
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
            }

            if (targetPanel != homeScreenPanel && MobControl.Level.HomeScreen3DManager.Instance != null)
            {
                MobControl.Level.HomeScreen3DManager.Instance.SetHomeMode(false);
            }

            if (targetPanel != gameplayHudPanel)
            {
                isWaitingForFirstSwipe = false;
                if (swipePromptAnimCoroutine != null)
                {
                    StopCoroutine(swipePromptAnimCoroutine);
                    swipePromptAnimCoroutine = null;
                }
                if (swipeToStartPromptObj != null)
                {
                    swipeToStartPromptObj.SetActive(false);
                }
            }

            if (targetPanel != null)
            {
                targetPanel.SetActive(true);
                targetPanel.transform.SetAsLastSibling();
                HookAllButtonsWithSound(targetPanel);
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

            if (MobControl.Level.HomeScreen3DManager.Instance != null)
            {
                MobControl.Level.HomeScreen3DManager.Instance.SetHomeMode(currentState == ScreenState.HomeScreen);
            }
        }

        public void ShowZombieSquadHomeScreen()
        {
            currentState = ScreenState.HomeScreen;
            isGameActive = false;
            LockPlayer(true);
            SwitchToPanel(homeScreenPanel);
            UpdateHomeScreenContainers();
            MobControl.Level.HomeScreen3DManager.EnsureInstance().SetHomeMode(true);
            if (MobControl.Audio.ZombieSoundManager.Instance != null)
            {
                MobControl.Audio.ZombieSoundManager.Instance.PlayBGM();
            }
        }

        /// <summary>
        /// Robust Fallback Launch: Guarantees safe and clean landing on the Home Screen on initial boot, scene load,
        /// or error recovery. Ensures UI hierarchy, main canvas, camera, and home 3D managers are initialized without black screen or lockups.
        /// </summary>
        public void LaunchFallbackHome()
        {
            try
            {
                Debug.Log("[ZombieRunnerUIManager] Executing Fallback Launch -> Ensuring UI Hierarchy and safe Home Screen landing...");
                EnsureUIBuilt();

                if (mainCanvas == null || homeScreenPanel == null)
                {
                    Debug.LogWarning("[ZombieRunnerUIManager] Fallback Launch: Missing Canvas or HomeScreenPanel, forcing full rebuild...");
                    BuildUIHierarchy();
                }

                currentState = ScreenState.HomeScreen;
                isGameActive = false;
                isFinishedHandled = false;
                Time.timeScale = 1f;
                LockPlayer(true);

                if (homeScreenPanel != null)
                {
                    SwitchToPanel(homeScreenPanel);
                    UpdateHomeScreenContainers();
                }

                if (MobControl.Level.HomeScreen3DManager.Instance != null)
                {
                    MobControl.Level.HomeScreen3DManager.Instance.SetHomeMode(true);
                }

                if (MobControl.Audio.ZombieSoundManager.Instance != null)
                {
                    MobControl.Audio.ZombieSoundManager.Instance.PlayBGM();
                }

                Debug.Log("[ZombieRunnerUIManager] Fallback Launch Completed Successfully!");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ZombieRunnerUIManager] Critical Exception during Fallback Launch: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public void ShowZombieSquadLevelMap()
        {
            currentState = ScreenState.LevelMap;
            isGameActive = false;
            LockPlayer(true);
            SwitchToPanel(levelMapPanel);
            EnsureLevelMapBound();
            UpdateLevelMapVisuals();
        }

        public void ShowZombieSquadCharactersScreen()
        {
            currentState = ScreenState.Characters;
            isGameActive = false;
            LockPlayer(true);
            if (charactersPanel == null || charPreviewHeroImage == null)
            {
                RebuildZombieSquadCharactersScreen();
            }
            else
            {
                SwitchToPanel(charactersPanel);
                selectedCharacterKey = PlayerPrefs.GetString("ZombieSquad_SelectedHero", "Soldier");
                UpdateCharactersScreenVisuals();
            }
        }

        public void RebuildZombieSquadCharactersScreen()
        {
            currentState = ScreenState.Characters;
            isGameActive = false;
            LockPlayer(true);
            if (mainCanvas != null)
            {
                BuildZombieSquadCharactersScreenPanel(mainCanvas.transform);
            }
            if (charactersPanel != null)
            {
                SwitchToPanel(charactersPanel);
                selectedCharacterKey = PlayerPrefs.GetString("ZombieSquad_SelectedHero", "Soldier");
                UpdateCharactersScreenVisuals();
            }
        }

        public void RebuildZombieSquadUpgradeScreen()
        {
            if (upgradePanel != null)
            {
                upgradePanel.name = "ZombieSquadUpgradePanel_Old";
                upgradePanel.SetActive(false);
                SafeDestroy(upgradePanel);
                upgradePanel = null;
            }
            if (mainCanvas != null)
            {
                Transform oldUpg = mainCanvas.transform.Find("ZombieSquadUpgradePanel");
                if (oldUpg != null)
                {
                    oldUpg.name = "ZombieSquadUpgradePanel_Old2";
                    oldUpg.gameObject.SetActive(false);
                    SafeDestroy(oldUpg.gameObject);
                }
                BuildZombieSquadUpgradeScreenPanel(mainCanvas.transform);
            }
        }

        public void ShowZombieSquadUpgradeScreen()
        {
            currentState = ScreenState.Upgrade;
            isGameActive = false;
            LockPlayer(true);
            RebuildZombieSquadUpgradeScreen();
            SwitchToPanel(upgradePanel);
            UpdateUpgradeScreenVisuals();
        }

        public void RebuildZombieSquadWeaponsScreen()
        {
            if (weaponsPanel != null)
            {
                SafeDestroy(weaponsPanel);
                weaponsPanel = null;
            }
            if (mainCanvas != null)
            {
                BuildZombieSquadWeaponsScreenPanel(mainCanvas.transform);
            }
        }

        public void ShowZombieSquadWeaponsScreen()
        {
            currentState = ScreenState.Weapons;
            isGameActive = false;
            LockPlayer(true);
            RebuildZombieSquadWeaponsScreen();
            SwitchToPanel(weaponsPanel);
            UpdateWeaponsScreenVisuals();
        }

        public void ShowZombieSquadShopScreen()
        {
            currentState = ScreenState.Shop;
            isGameActive = false;
            LockPlayer(true);
            RebuildZombieSquadShopScreen();
            SwitchToPanel(shopPanel);
            UpdateShopScreenVisuals();
        }

        public void RebuildZombieSquadShopScreen()
        {
            if (mainCanvas != null)
            {
                BuildZombieSquadShopScreenPanel(mainCanvas.transform);
            }
            if (shopPanel != null)
            {
                SwitchToPanel(shopPanel);
                UpdateShopScreenVisuals();
            }
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
            if (mainCanvas != null) BuildZombieSquadDailyScreenPanel(mainCanvas.transform); // refresh hero + state
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

            if (MobControl.Level.HomeScreen3DManager.Instance != null)
            {
                MobControl.Level.HomeScreen3DManager.Instance.SetHomeMode(false);
            }

            // 1. Ensure Reference Level 1 Road & Environment is built
            if (!MobControl.Level.ArmyRunLevel.Enabled)
            {
                MobControl.Level.NewRoadBuilder.EnsureNewRoad();

                // 2. Setup Camera and Atmosphere for Level Road
                MobControl.Level.NewRoadBuilder.SetupCameraAndAtmosphere(levelNumber);

                // 3. Configure Level for Endless Road and Enemy Wave Spawners
                if (MobControl.Level.EndlessRoadManager.Instance != null)
                {
                    MobControl.Level.EndlessRoadManager.Instance.CurrentLevel = levelNumber;
                }
                if (MobControl.Crowd.ZombieRoadWaveSpawner.Instance != null)
                {
                    MobControl.Crowd.ZombieRoadWaveSpawner.Instance.CurrentLevel = levelNumber;
                }
            }

            // 4. Update HUD level title
            if (hudLevelText != null)
            {
                hudLevelText.text = $"LEVEL {levelNumber}";
            }

            // 4. Switch to Gameplay HUD
            SwitchToPanel(gameplayHudPanel);
            if (gameplayHudPanel != null)
            {
                Transform existingJoy = gameplayHudPanel.transform.Find("OnScreenJoystick_Steering");
                if (existingJoy != null)
                {
                    if (Application.isPlaying) Destroy(existingJoy.gameObject);
                    else DestroyImmediate(existingJoy.gameObject);
                }
            }
            isBossPhase = false;
            UpdateWaveProgress(0f);

            // 5. Reset player controls into waiting state
            if (playerRunner == null)
            {
                playerRunner = PlayerRunnerController.Instance ?? FindAnyObjectByType<PlayerRunnerController>();
            }
            if (playerRunner != null)
            {
                playerRunner.ResetForNewRun(0f);
                playerRunner.SetWaitingToStart(true);
                playerRunner.SetControllable(true);
            }

            // 6. Stop road rushing until player initiates swipe
            if (MobControl.Level.EndlessRoadManager.Instance != null)
            {
                MobControl.Level.EndlessRoadManager.Instance.SetRunning(false);
            }

            // 7. Clear road wave spawner and prepare without spawning yet
            if (MobControl.Crowd.ZombieRoadWaveSpawner.Instance != null)
            {
                MobControl.Crowd.ZombieRoadWaveSpawner.Instance.PrepareForLevelStart();
            }

            // 8. Display soothing "Swipe Left / Right to Start" indicator
            ShowSwipeToStartPrompt();

            // 9. Level 1 = Army Run (Top War style crowd runner); other levels tear it down
            MobControl.Level.ArmyRunLevel.OnMissionStarted(levelNumber);

            Debug.Log($"[ZombieSquad] Level {levelNumber} ready! Waiting for user swipe to start.");
        }

        private void SetupLevelSpecificContent(int levelNumber)
        {
            // Clean up any previously spawned level creatures
            GameObject existingCreaturesRoot = GameObject.Find("Level_Creatures_Root");
            if (existingCreaturesRoot != null)
            {
                if (Application.isPlaying) Destroy(existingCreaturesRoot);
                else DestroyImmediate(existingCreaturesRoot);
            }
        }

        private void UpdateGameplayHud()
        {
            if (playerRunner == null) return;

            // In stationary defense mode, progress is driven by 30s wave timer & boss power!
            // Update Health Text
            UpdateHDHealthHud();
        }

        #endregion

        #region Victory & Animations

        public void ForceShowVictoryScreen(int stars = 3, int coins = 100, bool animate = false)
        {
            isFinishedHandled = false;
            TriggerVictory(stars, coins);
            if (!animate)
            {
                StopAllCoroutines();
                if (victoryCardRect != null) victoryCardRect.localScale = Vector3.one;
                if (victoryRewardText != null) victoryRewardText.text = $"+{coins} COINS";
                for (int i = 0; i < victoryStarSlots.Count; i++)
                {
                    var slot = victoryStarSlots[i];
                    if (slot == null) continue;
                    bool earned = (i < stars);
                    if (slot.dimImg != null) slot.dimImg.gameObject.SetActive(!earned);
                    if (slot.goldImg != null)
                    {
                        slot.goldImg.gameObject.SetActive(earned);
                        if (slot.goldRt != null) slot.goldRt.localScale = Vector3.one;
                    }
                }
            }
        }

        /// <summary>Overrides the line under the victory title (Army Run shows the final army count here).</summary>
        public void SetVictorySubtitle(string text)
        {
            if (victorySubText != null && !string.IsNullOrEmpty(text)) victorySubText.text = text;
        }

        public void TriggerVictory(int overrideStars, int overrideCoins)
        {
            if (isFinishedHandled) return;
            isFinishedHandled = true;
            isGameActive = false;
            currentState = ScreenState.Victory;
            MobControl.Audio.ZombieSoundManager.Instance?.PlayVictory();

            Debug.Log($"[ZombieRunnerUIManager] Victory triggered with override: stars={overrideStars}, coins={overrideCoins}");

            LockPlayer(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            EnsureEventSystem();
            SwitchToPanel(victoryPanel);

            int starsEarned = Mathf.Clamp(overrideStars, 1, 3);
            int coinsEarned = overrideCoins;

            int currentCoins = PlayerPrefs.GetInt(PREF_COINS, DEFAULT_COINS);
            currentCoins += coinsEarned;
            PlayerPrefs.SetInt(PREF_COINS, currentCoins);

            int unlockedLevel = PlayerPrefs.GetInt(PREF_UNLOCKED_LEVEL, 1);
            if (activeLevel >= unlockedLevel && activeLevel < totalLevels)
            {
                PlayerPrefs.SetInt(PREF_UNLOCKED_LEVEL, activeLevel + 1);
            }

            int prevStars = PlayerPrefs.GetInt(PREF_STARS_PREFIX + activeLevel, 0);
            if (starsEarned > prevStars)
            {
                PlayerPrefs.SetInt(PREF_STARS_PREFIX + activeLevel, starsEarned);
            }
            PlayerPrefs.Save();

            if (victorySubText != null)
            {
                if (starsEarned == 3) victorySubText.text = "PERFECT RUN!";
                else if (starsEarned == 2) victorySubText.text = "GREAT COMBAT!";
                else victorySubText.text = "SURVIVED!";
            }

            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);
                victoryPanel.transform.SetAsLastSibling();
            }
            StartCoroutine(AnimateVictoryCelebration(starsEarned, coinsEarned));
        }

        public void TriggerVictory()
        {
            if (isFinishedHandled) return;
            isFinishedHandled = true;
            isGameActive = false;
            currentState = ScreenState.Victory;
            MobControl.Audio.ZombieSoundManager.Instance?.PlayVictory();

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
            int currentCoins = PlayerPrefs.GetInt(PREF_COINS, DEFAULT_COINS);
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
                if (starsEarned == 3) victorySubText.text = "PERFECT RUN!";
                else if (starsEarned == 2) victorySubText.text = "GREAT COMBAT!";
                else victorySubText.text = "SURVIVED!";
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
                bool isUnearned = (i >= starsEarned);
                if (slot.dimImg != null) slot.dimImg.gameObject.SetActive(isUnearned);
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
                if (i >= starsEarned) break; // Unearned stars remain as dim silhouette sockets

                var slot = (i < victoryStarSlots.Count) ? victoryStarSlots[i] : null;
                if (slot != null && slot.goldImg != null)
                {
                    slot.goldImg.gameObject.SetActive(true);
                    if (slot.dimImg != null) slot.dimImg.gameObject.SetActive(false);
                }

                ZombieSoundManager.Instance?.PlayUIClick();

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
                    ZombieSoundManager.Instance?.PlayCoinSpend();
                    yield return new WaitForSeconds(0.035f);
                }
                victoryRewardText.text = $"+{coinsEarned} COINS";
            }
        }

        public void ForceShowDefeatScreen(string title = "CRASHED!", string subtitle = "You crashed into a wrecked burned car.")
        {
            isFinishedHandled = false;
            TriggerDefeatWithReason(title, subtitle);
        }

        public void TriggerDefeatWithReason(string title = "GAME OVER", string subtitle = "Mission Failed")
        {
            if (isFinishedHandled) return;
            isFinishedHandled = true;
            isGameActive = false;
            currentState = ScreenState.Defeat;
            MobControl.Audio.ZombieSoundManager.Instance?.PlayDefeat();

            Debug.Log($"[ZombieRunnerUIManager] Defeat triggered: title='{title}', subtitle='{subtitle}'");

            LockPlayer(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            EnsureEventSystem();
            SwitchToPanel(defeatPanel);

            if (defeatTitleText != null)
            {
                defeatTitleText.text = !string.IsNullOrEmpty(title) ? title : "DEFEATED!";
                defeatTitleText.gameObject.SetActive(true);
            }

            if (defeatSubText != null)
            {
                defeatSubText.text = !string.IsNullOrEmpty(subtitle) ? subtitle : "Your squad was eliminated.";
                defeatSubText.gameObject.SetActive(true);
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
            int coins = PlayerPrefs.GetInt(PREF_COINS, DEFAULT_COINS);
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
            PlayerPrefs.DeleteKey(PREF_DIAMONDS);
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
