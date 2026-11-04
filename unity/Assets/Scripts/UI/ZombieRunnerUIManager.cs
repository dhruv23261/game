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
        public bool IsModalOpen => currentState != ScreenState.Gameplay;
        public ScreenState CurrentState => currentState;

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

        [Header("Home Screen Modular vs Fallback Mode")]
        [SerializeField] private GameObject homeScreenResponsiveContainer;
        [SerializeField] private GameObject homeScreenFallbackContainer;
        [SerializeField] private TextMeshProUGUI homeScreenModeToggleText;
        private RectTransform modularPlayBtnRect;
        private RectTransform modularCharactersBtnRect;
        private RectTransform modularUpgradeBtnRect;
        private RectTransform modularWeaponsBtnRect;
        private RectTransform modularShopBtnRect;
        private RectTransform modularMissionsBtnRect;
        private RectTransform modularDailyBtnRect;
        private RectTransform fallbackPlayBtnRect;
        private RectTransform fallbackCharactersBtnRect;
        private RectTransform fallbackUpgradeBtnRect;
        private RectTransform fallbackWeaponsBtnRect;
        private RectTransform fallbackShopBtnRect;
        private RectTransform fallbackMissionsBtnRect;
        private RectTransform fallbackDailyBtnRect;

        public static bool UseModularHomeScreen
        {
            get => PlayerPrefs.GetInt("ZombieSquad_HomeScreen_UseModular", 1) == 1;
            set
            {
                PlayerPrefs.SetInt("ZombieSquad_HomeScreen_UseModular", value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }
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
                GameObject uiRoot = new GameObject("[ZombieRunner_UI_Manager]");
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
            MobControl.Level.NewRoadBuilder.EnsureNewRoad();

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
                        int currentLevel = PlayerPrefs.GetInt(PREF_UNLOCKED_LEVEL, 1);
                        if (currentLevel < 1) currentLevel = 1;
                        Debug.Log($"[ZombieRunnerUIManager] HomeScreen PLAY clicked -> Launching Level {currentLevel} Road Battle directly!");
                        StartMission(currentLevel);
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

        public void ToggleHomeScreenMode()
        {
            SetHomeScreenModular(!UseModularHomeScreen);
        }

        public void SetHomeScreenModular(bool useModular)
        {
            UseModularHomeScreen = useModular;
            UpdateHomeScreenContainers();
        }

        public void UpdateHomeScreenContainers()
        {
            bool isModular = UseModularHomeScreen;
            if (homeScreenResponsiveContainer != null) homeScreenResponsiveContainer.SetActive(isModular);
            if (homeScreenFallbackContainer != null) homeScreenFallbackContainer.SetActive(!isModular);

            if (homeScreenModeToggleText != null)
            {
                homeScreenModeToggleText.text = isModular 
                    ? "MODE: RESPONSIVE  (Tap: Fallback)" 
                    : "MODE: FALLBACK  (Tap: Responsive)";
            }

            // Sync pointer rects for Update() fallback handling
            if (isModular)
            {
                if (modularPlayBtnRect != null) homeScreenPlayBtnRect = modularPlayBtnRect;
                if (modularShopBtnRect != null) homeScreenShopBtnRect = modularShopBtnRect;
                if (modularCharactersBtnRect != null) homeScreenCharactersBtnRect = modularCharactersBtnRect;
                if (modularWeaponsBtnRect != null) homeScreenWeaponsBtnRect = modularWeaponsBtnRect;
                if (modularMissionsBtnRect != null) homeScreenMissionsBtnRect = modularMissionsBtnRect;
                if (modularDailyBtnRect != null) homeScreenDailyBtnRect = modularDailyBtnRect;
                if (modularUpgradeBtnRect != null) homeScreenUpgradeBtnRect = modularUpgradeBtnRect;
            }
            else
            {
                if (fallbackPlayBtnRect != null) homeScreenPlayBtnRect = fallbackPlayBtnRect;
                if (fallbackShopBtnRect != null) homeScreenShopBtnRect = fallbackShopBtnRect;
                if (fallbackCharactersBtnRect != null) homeScreenCharactersBtnRect = fallbackCharactersBtnRect;
                if (fallbackWeaponsBtnRect != null) homeScreenWeaponsBtnRect = fallbackWeaponsBtnRect;
                if (fallbackMissionsBtnRect != null) homeScreenMissionsBtnRect = fallbackMissionsBtnRect;
                if (fallbackDailyBtnRect != null) homeScreenDailyBtnRect = fallbackDailyBtnRect;
                if (fallbackUpgradeBtnRect != null) homeScreenUpgradeBtnRect = fallbackUpgradeBtnRect;
            }

            Debug.Log($"[ZombieRunnerUIManager] HomeScreen switched to: {(isModular ? "Responsive Modular UI" : "Classic Fallback Static Image")}");
        }

        private void BuildZombieSquadHomeScreenPanel(Transform canvasRoot)
        {
            // Fullscreen solid background matching artwork color palette so widescreen/tablet letterboxing blends seamlessly
            homeScreenPanel = CreateFullPanel("ZombieSquadHomeScreenPanel", canvasRoot, new Color(0.06f, 0.08f, 0.12f, 1f));

            // ==========================================
            // 1. FALLBACK STATIC CONTAINER (CLASSIC SCREEN)
            // ==========================================
            homeScreenFallbackContainer = new GameObject("Fallback_BakedImage_Container");
            homeScreenFallbackContainer.transform.SetParent(homeScreenPanel.transform, false);
            RectTransform fallbackRootRt = homeScreenFallbackContainer.AddComponent<RectTransform>();
            fallbackRootRt.anchorMin = Vector2.zero;
            fallbackRootRt.anchorMax = Vector2.one;
            fallbackRootRt.offsetMin = Vector2.zero;
            fallbackRootRt.offsetMax = Vector2.zero;

            Sprite masterArtSprite = LoadZombieSquadHomeScreenSprite();

            // Ambient background filler for wider tablet/iPad displays (seamlessly fills tablet side wings with atmospheric scenery)
            GameObject ambientBg = new GameObject("AmbientTabletBackground");
            ambientBg.transform.SetParent(homeScreenFallbackContainer.transform, false);
            RectTransform ambRt = ambientBg.AddComponent<RectTransform>();
            ambRt.anchorMin = Vector2.zero;
            ambRt.anchorMax = Vector2.one;
            ambRt.offsetMin = Vector2.zero;
            ambRt.offsetMax = Vector2.zero;

            AspectRatioFitter ambFitter = ambientBg.AddComponent<AspectRatioFitter>();
            ambFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            ambFitter.aspectRatio = 941f / 1672f;

            Image ambImg = ambientBg.AddComponent<Image>();
            ambImg.sprite = masterArtSprite;
            ambImg.preserveAspect = true;
            ambImg.color = new Color(0.35f, 0.38f, 0.45f, 1f);
            ambImg.raycastTarget = false;

            // ArtContainer scaled with AspectRatioFitter to preserve authentic proportions on any phone, tablet or desktop
            // Master artwork ratio: 941 x 1672 (~0.5628)
            GameObject artContainer = new GameObject("ArtContainer");
            artContainer.transform.SetParent(homeScreenFallbackContainer.transform, false);
            RectTransform artRt = artContainer.AddComponent<RectTransform>();
            artRt.anchorMin = Vector2.zero;
            artRt.anchorMax = Vector2.one;
            artRt.offsetMin = Vector2.zero;
            artRt.offsetMax = Vector2.zero;

            Image artImg = artContainer.AddComponent<Image>();
            artImg.sprite = masterArtSprite;
            artImg.preserveAspect = true;
            artImg.raycastTarget = true;

            AspectRatioFitter fitter = artContainer.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 941f / 1672f;

            // Interactive Transparent Button Overlays exactly mapped to painted master buttons:
            // 1. PLAY Button (Bottom Center) - Directly launches active gameplay battle!
            Button playBtn = CreateTransparentButton("Btn_Play_Fallback", artContainer.transform, new Vector2(0.202f, 0.049f), new Vector2(0.797f, 0.133f), () =>
            {
                int currentLevel = PlayerPrefs.GetInt(PREF_UNLOCKED_LEVEL, 1);
                if (currentLevel < 1) currentLevel = 1;
                Debug.Log($"[ZombieSquad] PLAY Button Pressed -> Launching Level {currentLevel} Road Battle directly!");
                StartMission(currentLevel);
            });
            fallbackPlayBtnRect = playBtn.GetComponent<RectTransform>();

            // 2. Left Menu Column: Shop, Characters, Weapons
            Button shopBtn = CreateTransparentButton("Btn_Shop_Fallback", artContainer.transform, new Vector2(0.027f, 0.584f), new Vector2(0.213f, 0.677f), () =>
            {
                Debug.Log("[ZombieSquad] Shop Clicked -> Opening Shop Screen!");
                ShowZombieSquadShopScreen();
            });
            fallbackShopBtnRect = shopBtn.GetComponent<RectTransform>();

            Button charBtn = CreateTransparentButton("Btn_Characters_Fallback", artContainer.transform, new Vector2(0.027f, 0.468f), new Vector2(0.213f, 0.560f), () =>
            {
                Debug.Log("[ZombieSquad] Characters Clicked -> Opening Characters Screen!");
                ShowZombieSquadCharactersScreen();
            });
            fallbackCharactersBtnRect = charBtn.GetComponent<RectTransform>();

            Button wepBtn = CreateTransparentButton("Btn_Weapons_Fallback", artContainer.transform, new Vector2(0.027f, 0.351f), new Vector2(0.213f, 0.444f), () =>
            {
                Debug.Log("[ZombieSquad] Weapons Clicked -> Opening Weapons Screen!");
                ShowZombieSquadWeaponsScreen();
            });
            fallbackWeaponsBtnRect = wepBtn.GetComponent<RectTransform>();

            // 3. Right Menu Column: Missions, Daily, Upgrade
            Button missionsBtn = CreateTransparentButton("Btn_Missions_Fallback", artContainer.transform, new Vector2(0.786f, 0.584f), new Vector2(0.972f, 0.677f), () =>
            {
                Debug.Log("[ZombieSquad] Missions Clicked -> Opening Level Map Screen!");
                ShowZombieSquadLevelMap();
            });
            fallbackMissionsBtnRect = missionsBtn.GetComponent<RectTransform>();

            Button dailyBtn = CreateTransparentButton("Btn_Daily_Fallback", artContainer.transform, new Vector2(0.786f, 0.468f), new Vector2(0.972f, 0.560f), () =>
            {
                Debug.Log("[ZombieSquad] Daily Rewards Clicked -> Opening Daily Screen!");
                ShowZombieSquadDailyScreen();
            });
            fallbackDailyBtnRect = dailyBtn.GetComponent<RectTransform>();

            Button upgBtn = CreateTransparentButton("Btn_Upgrade_Fallback", artContainer.transform, new Vector2(0.786f, 0.351f), new Vector2(0.972f, 0.444f), () =>
            {
                Debug.Log("[ZombieSquad] Upgrade Clicked -> Opening Squad Upgrade Screen!");
                ShowZombieSquadUpgradeScreen();
            });
            fallbackUpgradeBtnRect = upgBtn.GetComponent<RectTransform>();

            // 4. Settings Gear (Top-Right)
            CreateTransparentButton("Btn_Settings_Fallback", artContainer.transform, new Vector2(0.861f, 0.904f), new Vector2(0.972f, 0.967f), () =>
            {
                Debug.Log("[ZombieSquad] Settings Clicked -> Opening Settings Popup!");
                ShowZombieSquadSettings();
            });

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

            // Clean Scenery Background (No baked buttons or text)
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

            // Title Logo (Zombie Squad)
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
                int currentLevel = PlayerPrefs.GetInt(PREF_UNLOCKED_LEVEL, 1);
                if (currentLevel < 1) currentLevel = 1;
                Debug.Log($"[ZombieSquad] PLAY Button Pressed -> Launching Level {currentLevel} Road Battle directly!");
                StartMission(currentLevel);
            });
            modularPlayBtnRect = respPlayBtn.GetComponent<RectTransform>();
            modularPlayBtnRect.anchorMin = new Vector2(0.5f, 0f);
            modularPlayBtnRect.anchorMax = new Vector2(0.5f, 0f);
            modularPlayBtnRect.pivot = new Vector2(0.5f, 0f);
            modularPlayBtnRect.anchoredPosition = new Vector2(0f, 110f);

            // ==========================================
            // 3. PERSISTENT FALLBACK / RESPONSIVE MODE TOGGLE PILL
            // ==========================================
            GameObject togglePillObj = new GameObject("HomeScreen_ModeToggle_Pill");
            togglePillObj.transform.SetParent(homeScreenPanel.transform, false);
            RectTransform pillRt = togglePillObj.AddComponent<RectTransform>();
            pillRt.anchorMin = new Vector2(0f, 1f);
            pillRt.anchorMax = new Vector2(0f, 1f);
            pillRt.pivot = new Vector2(0f, 1f);
            pillRt.anchoredPosition = new Vector2(35f, -50f);
            pillRt.sizeDelta = new Vector2(360f, 54f);

            Image pillBg = togglePillObj.AddComponent<Image>();
            pillBg.color = new Color(0.06f, 0.09f, 0.16f, 0.92f);
            pillBg.raycastTarget = true;

            Button pillBtn = togglePillObj.AddComponent<Button>();
            pillBtn.targetGraphic = pillBg;
            pillBtn.onClick.AddListener(ToggleHomeScreenMode);

            GameObject textObj = new GameObject("ToggleText");
            textObj.transform.SetParent(togglePillObj.transform, false);
            RectTransform textRt = textObj.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(10f, 0f);
            textRt.offsetMax = new Vector2(-10f, 0f);

            homeScreenModeToggleText = textObj.AddComponent<TextMeshProUGUI>();
            homeScreenModeToggleText.enabled = false; // Prevent GenerateTextMesh before material is ready
            homeScreenModeToggleText.fontSize = 17f;
            homeScreenModeToggleText.fontStyle = FontStyles.Bold;
            homeScreenModeToggleText.alignment = TextAlignmentOptions.Center;
            homeScreenModeToggleText.color = new Color(1.0f, 0.82f, 0.18f, 1f); // Golden yellow
            homeScreenModeToggleText.raycastTarget = false;
            homeScreenModeToggleText.enabled = true; // Now safe to generate mesh

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

        private void BuildZombieSquadLevelMapPanel(Transform canvasRoot)
        {
            levelMapPanel = CreateFullPanel("ZombieSquadLevelMapPanel", canvasRoot, new Color(0.04f, 0.06f, 0.10f, 1f));

            Sprite mapSprite = LoadZombieSquadLevelMapSprite();

            // Ambient Scenery Cover (EnvelopeParent fills 100% of any tall/wide mobile display - zero black bars!)
            SetupAmbientSceneryCover(levelMapPanel, mapSprite, 9f / 16f);

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

            Shadow mapShadowB = artContainer.AddComponent<Shadow>();
            mapShadowB.effectColor = new Color(0.005f, 0.015f, 0.05f, 0.85f);
            mapShadowB.effectDistance = new Vector2(0f, -8f);

            Shadow mapShadowT = artContainer.AddComponent<Shadow>();
            mapShadowT.effectColor = new Color(0.005f, 0.015f, 0.05f, 0.85f);
            mapShadowT.effectDistance = new Vector2(0f, 8f);

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

                RectTransform nodeRt = nodeRoot.AddComponent<RectTransform>();
                nodeRt.anchorMin = normPos;
                nodeRt.anchorMax = normPos;
                nodeRt.pivot = new Vector2(0.5f, 0.5f);
                nodeRt.anchoredPosition = Vector2.zero;
                nodeRt.sizeDelta = size;
                levelMapNodeRects[i] = nodeRt;

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
                innerDisc.raycastTarget = false;

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
                tmp.raycastTarget = false;
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
        private TextMeshProUGUI vibrationToggleText;
        private Image vibrationToggleBg;
        private TextMeshProUGUI volumeToggleText;
        private Image volumeToggleBg;

        private const string PREF_VIBRATION = "ZombieSquad_Vibration";
        private const string PREF_VOLUME = "ZombieSquad_Volume";
        private const string PREF_DIAMONDS = "ZombieRunner_Diamonds";

        public bool IsVibrationEnabled => PlayerPrefs.GetInt(PREF_VIBRATION, 1) == 1;
        public bool IsVolumeEnabled => PlayerPrefs.GetInt(PREF_VOLUME, 1) == 1;

        private List<TextMeshProUGUI> allCoinsDisplays = new List<TextMeshProUGUI>();
        private List<TextMeshProUGUI> allDiamondsDisplays = new List<TextMeshProUGUI>();

        public int GetPlayerCoins() => PlayerPrefs.GetInt(PREF_COINS, 500);
        public int GetPlayerDiamonds() => PlayerPrefs.GetInt(PREF_DIAMONDS, 50);

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
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
                UpdateSettingsVisuals();
            }
        }

        public void HideZombieSquadSettings()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
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
            Debug.Log($"[ZombieSquad] Settings -> Vibration toggled to: {next}");
            UpdateSettingsVisuals();
        }

        public void ToggleVolume()
        {
            bool next = !IsVolumeEnabled;
            PlayerPrefs.SetInt(PREF_VOLUME, next ? 1 : 0);
            PlayerPrefs.Save();
            AudioListener.volume = next ? 1f : 0f;
            Debug.Log($"[ZombieSquad] Settings -> Volume toggled to: {next}");
            UpdateSettingsVisuals();
        }

        private void UpdateSettingsVisuals()
        {
            if (vibrationToggleText != null && vibrationToggleBg != null)
            {
                bool vib = IsVibrationEnabled;
                vibrationToggleText.text = vib ? "ON" : "OFF";
                vibrationToggleBg.color = vib ? new Color(0.12f, 0.78f, 0.32f, 1f) : new Color(0.35f, 0.15f, 0.18f, 1f);
            }
            if (volumeToggleText != null && volumeToggleBg != null)
            {
                bool vol = IsVolumeEnabled;
                volumeToggleText.text = vol ? "ON" : "OFF";
                volumeToggleBg.color = vol ? new Color(0.12f, 0.78f, 0.32f, 1f) : new Color(0.35f, 0.15f, 0.18f, 1f);
            }
        }

        private void BuildSettingsPopupPanel(Transform canvasRoot)
        {
            settingsPanel = new GameObject("ZombieSquadSettingsPanel");
            settingsPanel.transform.SetParent(canvasRoot, false);
            RectTransform panelRt = settingsPanel.AddComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            Image bg = settingsPanel.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.04f, 0.08f, 0.85f);
            bg.raycastTarget = true;

            // Center Dialog Card
            GameObject dialog = new GameObject("SettingsDialog");
            dialog.transform.SetParent(settingsPanel.transform, false);
            RectTransform dlgRt = dialog.AddComponent<RectTransform>();
            dlgRt.sizeDelta = new Vector2(720f, 600f);
            dlgRt.anchorMin = new Vector2(0.5f, 0.5f);
            dlgRt.anchorMax = new Vector2(0.5f, 0.5f);
            dlgRt.pivot = new Vector2(0.5f, 0.5f);

            Image dlgBg = dialog.AddComponent<Image>();
            dlgBg.color = new Color(0.07f, 0.11f, 0.18f, 0.98f);

            Outline dlgOutline = dialog.AddComponent<Outline>();
            dlgOutline.effectColor = new Color(0.15f, 0.75f, 1f, 0.85f);
            dlgOutline.effectDistance = new Vector2(3f, -3f);

            // Header Banner
            GameObject header = new GameObject("HeaderBanner");
            header.transform.SetParent(dialog.transform, false);
            RectTransform hRt = header.AddComponent<RectTransform>();
            hRt.sizeDelta = new Vector2(680f, 85f);
            hRt.anchoredPosition = new Vector2(0f, 235f);
            Image hBg = header.AddComponent<Image>();
            hBg.color = new Color(0.10f, 0.18f, 0.32f, 1f);
            TextMeshProUGUI title = CreateText("Title", header.transform, "SETTINGS", 36f, new Color(1f, 0.88f, 0.25f, 1f), TextAlignmentOptions.Center, true);
            title.GetComponent<RectTransform>().sizeDelta = new Vector2(500f, 80f);

            // Close (X) button
            GameObject closeBtnObj = new GameObject("Btn_Close");
            closeBtnObj.transform.SetParent(dialog.transform, false);
            RectTransform cRt = closeBtnObj.AddComponent<RectTransform>();
            cRt.sizeDelta = new Vector2(64f, 64f);
            cRt.anchoredPosition = new Vector2(305f, 235f);
            Image cBg = closeBtnObj.AddComponent<Image>();
            cBg.color = new Color(0.85f, 0.20f, 0.25f, 1f);
            Button closeBtn = closeBtnObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(() => HideZombieSquadSettings());
            TextMeshProUGUI cTxt = CreateText("X", closeBtnObj.transform, "✕", 32f, Color.white, TextAlignmentOptions.Center, true);
            cTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(64f, 64f);

            // Option 1: Vibration
            GameObject vibRow = new GameObject("Row_Vibration");
            vibRow.transform.SetParent(dialog.transform, false);
            RectTransform vRt = vibRow.AddComponent<RectTransform>();
            vRt.sizeDelta = new Vector2(640f, 95f);
            vRt.anchoredPosition = new Vector2(0f, 85f);
            Image vBg = vibRow.AddComponent<Image>();
            vBg.color = new Color(0.05f, 0.08f, 0.14f, 0.9f);

            TextMeshProUGUI vLabel = CreateText("Label", vibRow.transform, "📳  VIBRATION", 26f, Color.white, TextAlignmentOptions.Left, true);
            RectTransform vlRt = vLabel.GetComponent<RectTransform>();
            vlRt.sizeDelta = new Vector2(340f, 80f);
            vlRt.anchoredPosition = new Vector2(-120f, 0f);

            GameObject vBtnObj = new GameObject("Toggle_Vibration");
            vBtnObj.transform.SetParent(vibRow.transform, false);
            RectTransform vbRt = vBtnObj.AddComponent<RectTransform>();
            vbRt.sizeDelta = new Vector2(170f, 64f);
            vbRt.anchoredPosition = new Vector2(210f, 0f);
            vibrationToggleBg = vBtnObj.AddComponent<Image>();
            vibrationToggleBg.color = new Color(0.12f, 0.78f, 0.32f, 1f);
            Button vBtn = vBtnObj.AddComponent<Button>();
            vBtn.onClick.AddListener(() => ToggleVibration());
            vibrationToggleText = CreateText("Txt", vBtnObj.transform, "ON", 26f, Color.white, TextAlignmentOptions.Center, true);
            vibrationToggleText.GetComponent<RectTransform>().sizeDelta = new Vector2(170f, 64f);

            // Option 2: Volume
            GameObject volRow = new GameObject("Row_Volume");
            volRow.transform.SetParent(dialog.transform, false);
            RectTransform volRt = volRow.AddComponent<RectTransform>();
            volRt.sizeDelta = new Vector2(640f, 95f);
            volRt.anchoredPosition = new Vector2(0f, -40f);
            Image volBg = volRow.AddComponent<Image>();
            volBg.color = new Color(0.05f, 0.08f, 0.14f, 0.9f);

            TextMeshProUGUI volLabel = CreateText("Label", volRow.transform, "🔊  SOUND / VOLUME", 26f, Color.white, TextAlignmentOptions.Left, true);
            RectTransform vollRt = volLabel.GetComponent<RectTransform>();
            vollRt.sizeDelta = new Vector2(340f, 80f);
            vollRt.anchoredPosition = new Vector2(-120f, 0f);

            GameObject volBtnObj = new GameObject("Toggle_Volume");
            volBtnObj.transform.SetParent(volRow.transform, false);
            RectTransform volbRt = volBtnObj.AddComponent<RectTransform>();
            volbRt.sizeDelta = new Vector2(170f, 64f);
            volbRt.anchoredPosition = new Vector2(210f, 0f);
            volumeToggleBg = volBtnObj.AddComponent<Image>();
            volumeToggleBg.color = new Color(0.12f, 0.78f, 0.32f, 1f);
            Button volBtn = volBtnObj.AddComponent<Button>();
            volBtn.onClick.AddListener(() => ToggleVolume());
            volumeToggleText = CreateText("Txt", volBtnObj.transform, "ON", 26f, Color.white, TextAlignmentOptions.Center, true);
            volumeToggleText.GetComponent<RectTransform>().sizeDelta = new Vector2(170f, 64f);

            // Bottom OK button
            GameObject okBtnObj = new GameObject("Btn_OK");
            okBtnObj.transform.SetParent(dialog.transform, false);
            RectTransform okRt = okBtnObj.AddComponent<RectTransform>();
            okRt.sizeDelta = new Vector2(260f, 70f);
            okRt.anchoredPosition = new Vector2(0f, -195f);
            Image okBg = okBtnObj.AddComponent<Image>();
            okBg.color = new Color(0.15f, 0.60f, 0.95f, 1f);
            Button okBtn = okBtnObj.AddComponent<Button>();
            okBtn.onClick.AddListener(() => HideZombieSquadSettings());
            TextMeshProUGUI okTxt = CreateText("OK", okBtnObj.transform, "CONFIRM", 28f, Color.white, TextAlignmentOptions.Center, true);
            okTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(260f, 70f);

            settingsPanel.SetActive(false);
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
        private void BuildZombieSquadCharactersScreenPanel(Transform canvasRoot)
        {
            if (charactersPanel != null) SafeDestroy(charactersPanel);
            Transform oldChars = canvasRoot.Find("ZombieSquadCharactersPanel");
            if (oldChars != null) SafeDestroy(oldChars.gameObject);

            charactersPanel = CreateFullPanel("ZombieSquadCharactersPanel", canvasRoot, new Color(0.01f, 0.03f, 0.08f, 1f));

            // 0. Clean Atmospheric Night Scenery Background (EnvelopeParent fills 100% of any phone/tablet display - zero black bars, zero baked UI!)
            Sprite nightBattlefield = LoadSpriteFromUI("UI/Scenery_NightBattlefield_Clean");
            SetupAmbientSceneryCover(charactersPanel, nightBattlefield, 1080f / 1920f);

            // Semi-transparent cinematic dark vignette over scenery for crisp UI contrast
            GameObject vig = new GameObject("CharactersVignette");
            vig.transform.SetParent(charactersPanel.transform, false);
            RectTransform vigRt = vig.AddComponent<RectTransform>();
            vigRt.anchorMin = Vector2.zero; vigRt.anchorMax = Vector2.one;
            vigRt.offsetMin = Vector2.zero; vigRt.offsetMax = Vector2.zero;
            Image vigImg = vig.AddComponent<Image>();
            vigImg.color = new Color(0.01f, 0.03f, 0.08f, 0.15f);
            vigImg.raycastTarget = false;

            // 1. Top HUD Header (Back, "CHARACTERS", Coins Pill, Diamonds Pill)
            CreateModularTopHeader(charactersPanel.transform, "CHARACTERS", () => ShowZombieSquadHomeScreen());

            // 2. Central Responsive Content Container
            GameObject contentContainer = new GameObject("CharactersContainer");
            contentContainer.transform.SetParent(charactersPanel.transform, false);
            RectTransform ccRt = contentContainer.AddComponent<RectTransform>();
            ccRt.anchorMin = new Vector2(0.5f, 0.5f);
            ccRt.anchorMax = new Vector2(0.5f, 0.5f);
            ccRt.pivot = new Vector2(0.5f, 0.5f);
            ccRt.anchoredPosition = new Vector2(0f, -40f);
            ccRt.sizeDelta = new Vector2(980f, 1420f);

            // 3. Top Mascot & Logo on Characters Modal (Clean transparent alpha cutout, zero black boxes)
            Sprite mascotCleanSp = LoadSpriteFromUI(
                "UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/00_header_mascot_clean",
                "UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/01_zombie_squad_logo"
            );

            GameObject logoHeader = new GameObject("LogoHeaderBanner");
            logoHeader.transform.SetParent(contentContainer.transform, false);
            RectTransform lhRt = logoHeader.AddComponent<RectTransform>();
            lhRt.anchorMin = new Vector2(0.5f, 1f);
            lhRt.anchorMax = new Vector2(0.5f, 1f);
            lhRt.pivot = new Vector2(0.5f, 1f);
            lhRt.anchoredPosition = new Vector2(0f, -8f);
            lhRt.sizeDelta = new Vector2(540f, 135f);

            if (mascotCleanSp != null)
            {
                Image mascotImg = logoHeader.AddComponent<Image>();
                mascotImg.sprite = mascotCleanSp;
                mascotImg.preserveAspect = true;
                mascotImg.raycastTarget = false;
            }

            // 4. Center Showcase Chamber (Pedestal + Active Hero + Arrows)
            GameObject showcaseChamber = new GameObject("ShowcaseChamber");
            showcaseChamber.transform.SetParent(contentContainer.transform, false);
            RectTransform scRt = showcaseChamber.AddComponent<RectTransform>();
            scRt.anchorMin = new Vector2(0.5f, 1f);
            scRt.anchorMax = new Vector2(0.5f, 1f);
            scRt.pivot = new Vector2(0.5f, 1f);
            scRt.anchoredPosition = new Vector2(0f, -140f);
            scRt.sizeDelta = new Vector2(920f, 490f);

            // Frame (12_main_showcase_panel.png)
            Image scImg = showcaseChamber.AddComponent<Image>();
            Sprite scSp = LoadSpriteFromUI("UI/CharactersDynamic/12_main_showcase_panel_clean", "UI/CharactersDynamic/12_main_showcase_panel");
            if (scSp != null) scImg.sprite = scSp;
            scImg.type = Image.Type.Sliced;
            scImg.color = Color.white;
            scImg.raycastTarget = false;

            // Hero Name & Role TextMeshPro
            charPreviewNameText = CreateText("HeroName", showcaseChamber.transform, "SOLDIER", 34f, Color.white, TextAlignmentOptions.Center, true);
            RectTransform nameRt = charPreviewNameText.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0.15f, 0.82f);
            nameRt.anchorMax = new Vector2(0.85f, 0.96f);
            nameRt.offsetMin = Vector2.zero; nameRt.offsetMax = Vector2.zero;
            Outline nameOutl = charPreviewNameText.gameObject.AddComponent<Outline>();
            nameOutl.effectColor = new Color(0.01f, 0.08f, 0.28f, 1f);
            nameOutl.effectDistance = new Vector2(2f, -2f);

            charPreviewRoleText = CreateText("HeroRole", showcaseChamber.transform, "ASSAULT SPECIALIST", 18f, new Color(0.35f, 0.90f, 1f, 1f), TextAlignmentOptions.Center, true);
            RectTransform roleRt = charPreviewRoleText.GetComponent<RectTransform>();
            roleRt.anchorMin = new Vector2(0.15f, 0.70f);
            roleRt.anchorMax = new Vector2(0.85f, 0.82f);
            roleRt.offsetMin = Vector2.zero; roleRt.offsetMax = Vector2.zero;

            // Glowing Pedestal (14_glow_platform.png) - Clean, ZERO baked characters
            GameObject pedObj = new GameObject("GlowPedestal");
            pedObj.transform.SetParent(showcaseChamber.transform, false);
            RectTransform pRt = pedObj.AddComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0.5f, 0f);
            pRt.anchorMax = new Vector2(0.5f, 0f);
            pRt.pivot = new Vector2(0.5f, 0f);
            pRt.anchoredPosition = new Vector2(0f, 20f);
            pRt.sizeDelta = new Vector2(520f, 110f);
            Image pedImg = pedObj.AddComponent<Image>();
            Sprite pedSp = LoadSpriteFromUI("UI/CharactersDynamic/14_glow_platform_clean", "UI/CharactersDynamic/14_glow_platform");
            if (pedSp != null) pedImg.sprite = pedSp;
            pedImg.preserveAspect = true;
            pedImg.raycastTarget = false;

            // Selected Hero Avatar (Standing directly on top of pedestal)
            // Only ONE hero avatar exists in hierarchy - NO baked soldier, NO double character!
            GameObject avatarObj = new GameObject("HeroAvatar");
            avatarObj.transform.SetParent(showcaseChamber.transform, false);
            RectTransform aRt = avatarObj.AddComponent<RectTransform>();
            aRt.anchorMin = new Vector2(0.5f, 0f);
            aRt.anchorMax = new Vector2(0.5f, 0f);
            aRt.pivot = new Vector2(0.5f, 0.12f);
            aRt.anchoredPosition = new Vector2(0f, 30f);
            aRt.sizeDelta = new Vector2(380f, 430f);

            charPreviewHeroImage = avatarObj.AddComponent<Image>();
            charPreviewHeroImage.preserveAspect = true;
            charPreviewHeroImage.raycastTarget = false;

            Shadow avShadow = avatarObj.AddComponent<Shadow>();
            avShadow.effectColor = new Color(0.01f, 0.05f, 0.15f, 0.95f);
            avShadow.effectDistance = new Vector2(3f, -4f);

            // Carousel Navigation Arrows (< and >)
            Sprite leftArrowSp = LoadSpriteFromUI("UI/CharactersDynamic/15_left_arrow");
            Sprite rightArrowSp = LoadSpriteFromUI("UI/CharactersDynamic/16_right_arrow");

            GameObject leftArrowObj = new GameObject("Btn_Prev_Hero");
            leftArrowObj.transform.SetParent(showcaseChamber.transform, false);
            RectTransform laRt = leftArrowObj.AddComponent<RectTransform>();
            laRt.anchorMin = new Vector2(0f, 0.5f);
            laRt.anchorMax = new Vector2(0f, 0.5f);
            laRt.pivot = new Vector2(0f, 0.5f);
            laRt.anchoredPosition = new Vector2(25f, -20f);
            laRt.sizeDelta = new Vector2(75f, 110f);
            Image laImg = leftArrowObj.AddComponent<Image>();
            if (leftArrowSp != null) laImg.sprite = leftArrowSp;
            laImg.preserveAspect = true;
            laImg.raycastTarget = true;
            Button laBtn = leftArrowObj.AddComponent<Button>();
            laBtn.targetGraphic = laImg;
            laBtn.onClick.AddListener(() =>
            {
                StartCoroutine(PunchScale(leftArrowObj.transform, 0.10f, () => CycleHero(-1)));
            });

            GameObject rightArrowObj = new GameObject("Btn_Next_Hero");
            rightArrowObj.transform.SetParent(showcaseChamber.transform, false);
            RectTransform raRt = rightArrowObj.AddComponent<RectTransform>();
            raRt.anchorMin = new Vector2(1f, 0.5f);
            raRt.anchorMax = new Vector2(1f, 0.5f);
            raRt.pivot = new Vector2(1f, 0.5f);
            raRt.anchoredPosition = new Vector2(-25f, -20f);
            raRt.sizeDelta = new Vector2(75f, 110f);
            Image raImg = rightArrowObj.AddComponent<Image>();
            if (rightArrowSp != null) raImg.sprite = rightArrowSp;
            raImg.preserveAspect = true;
            raImg.raycastTarget = true;
            Button raBtn = rightArrowObj.AddComponent<Button>();
            raBtn.targetGraphic = raImg;
            raBtn.onClick.AddListener(() =>
            {
                StartCoroutine(PunchScale(rightArrowObj.transform, 0.10f, () => CycleHero(1)));
            });

            // 5. Action / Equip Button below chamber
            GameObject actBtnObj = new GameObject("Btn_EquipHero");
            actBtnObj.transform.SetParent(contentContainer.transform, false);
            RectTransform abRt = actBtnObj.AddComponent<RectTransform>();
            abRt.anchorMin = new Vector2(0.5f, 1f);
            abRt.anchorMax = new Vector2(0.5f, 1f);
            abRt.pivot = new Vector2(0.5f, 1f);
            abRt.anchoredPosition = new Vector2(0f, -645f);
            abRt.sizeDelta = new Vector2(440f, 95f);

            charPreviewEquipBtnBg = actBtnObj.AddComponent<Image>();
            charPreviewEquipBtnBg.type = Image.Type.Sliced;
            charPreviewEquipBtnBg.raycastTarget = true;

            charPreviewEquipBtn = actBtnObj.AddComponent<Button>();
            charPreviewEquipBtn.targetGraphic = charPreviewEquipBtnBg;
            charPreviewEquipBtn.onClick.AddListener(() =>
            {
                StartCoroutine(PunchScale(actBtnObj.transform, 0.12f, () => EquipSelectedCharacter()));
            });

            charPreviewEquipBtnText = CreateText("Txt", actBtnObj.transform, "EQUIPPED", 28f, Color.white, TextAlignmentOptions.Center, true);
            RectTransform abtRt = charPreviewEquipBtnText.GetComponent<RectTransform>();
            abtRt.anchorMin = Vector2.zero; abtRt.anchorMax = Vector2.one;
            abtRt.offsetMin = Vector2.zero; abtRt.offsetMax = Vector2.zero;

            Outline abOutl = charPreviewEquipBtnText.gameObject.AddComponent<Outline>();
            abOutl.effectColor = new Color(0.04f, 0.20f, 0.08f, 1f);
            abOutl.effectDistance = new Vector2(2f, -2f);

            // 6. 5 Hero Cards (Row 1: Soldier, Ranger, Heavy; Row 2: Sniper, Medic)
            charCardPriceTexts.Clear();
            charCardPriceBgs.Clear();

            // Row 1 (3 cards)
            GameObject row1 = new GameObject("HeroCards_Row1");
            row1.transform.SetParent(contentContainer.transform, false);
            RectTransform r1Rt = row1.AddComponent<RectTransform>();
            r1Rt.anchorMin = new Vector2(0.5f, 1f);
            r1Rt.anchorMax = new Vector2(0.5f, 1f);
            r1Rt.pivot = new Vector2(0.5f, 1f);
            r1Rt.anchoredPosition = new Vector2(0f, -755f);
            r1Rt.sizeDelta = new Vector2(920f, 290f);

            HorizontalLayoutGroup hlg1 = row1.AddComponent<HorizontalLayoutGroup>();
            hlg1.spacing = 20f;
            hlg1.childAlignment = TextAnchor.MiddleCenter;
            hlg1.childControlWidth = false;
            hlg1.childControlHeight = false;
            hlg1.childForceExpandWidth = false;
            hlg1.childForceExpandHeight = false;

            CreateModularHeroCard(row1.transform, "Soldier",
                "UI/CharactersDynamic/17_equipped_character_card",
                "UI/CharactersDynamic/22_green_soldier_portrait");

            CreateModularHeroCard(row1.transform, "Ranger",
                "UI/CharactersDynamic/18_red_character_card",
                "UI/CharactersDynamic/23_red_soldier_portrait");

            CreateModularHeroCard(row1.transform, "Heavy",
                "UI/CharactersDynamic/19_yellow_character_card",
                "UI/CharactersDynamic/24_yellow_soldier_portrait");

            // Row 2 (2 cards)
            GameObject row2 = new GameObject("HeroCards_Row2");
            row2.transform.SetParent(contentContainer.transform, false);
            RectTransform r2Rt = row2.AddComponent<RectTransform>();
            r2Rt.anchorMin = new Vector2(0.5f, 1f);
            r2Rt.anchorMax = new Vector2(0.5f, 1f);
            r2Rt.pivot = new Vector2(0.5f, 1f);
            r2Rt.anchoredPosition = new Vector2(0f, -1060f);
            r2Rt.sizeDelta = new Vector2(920f, 290f);

            HorizontalLayoutGroup hlg2 = row2.AddComponent<HorizontalLayoutGroup>();
            hlg2.spacing = 30f;
            hlg2.childAlignment = TextAnchor.MiddleCenter;
            hlg2.childControlWidth = false;
            hlg2.childControlHeight = false;
            hlg2.childForceExpandWidth = false;
            hlg2.childForceExpandHeight = false;

            CreateModularHeroCard(row2.transform, "Sniper",
                "UI/CharactersDynamic/20_purple_character_card",
                "UI/CharactersDynamic/25_purple_soldier_portrait");

            CreateModularHeroCard(row2.transform, "Medic",
                "UI/CharactersDynamic/21_pink_medic_card",
                "UI/CharactersDynamic/26_pink_medic_portrait");

            // 7. Bottom Decorative Metal Trim & Rocks
            Sprite cTrimSp = LoadSpriteFromUI("UI/CharactersDynamic/30_bottom_metal_trim", "UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/26_bottom_metal_trim");
            Sprite cLeftRock = LoadSpriteFromUI("UI/CharactersDynamic/31_left_rock_grass", "UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/27_left_rock_grass_decoration");
            Sprite cRightRock = LoadSpriteFromUI("UI/CharactersDynamic/32_right_rock_grass", "UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/28_right_rock_grass_decoration");

            if (cTrimSp != null)
            {
                GameObject trimObj = new GameObject("BottomTrim");
                trimObj.transform.SetParent(contentContainer.transform, false);
                RectTransform tRt = trimObj.AddComponent<RectTransform>();
                tRt.anchorMin = new Vector2(0f, 0f);
                tRt.anchorMax = new Vector2(1f, 0f);
                tRt.pivot = new Vector2(0.5f, 0f);
                tRt.anchoredPosition = new Vector2(0f, 10f);
                tRt.sizeDelta = new Vector2(0f, 90f);
                Image trimImg = trimObj.AddComponent<Image>();
                trimImg.sprite = cTrimSp;
                trimImg.preserveAspect = false;
                trimImg.raycastTarget = false;
            }

            if (cLeftRock != null)
            {
                GameObject lrObj = new GameObject("LeftRock");
                lrObj.transform.SetParent(contentContainer.transform, false);
                RectTransform lrRt = lrObj.AddComponent<RectTransform>();
                lrRt.anchorMin = new Vector2(0f, 0f);
                lrRt.anchorMax = new Vector2(0f, 0f);
                lrRt.pivot = new Vector2(0f, 0f);
                lrRt.anchoredPosition = new Vector2(-20f, 5f);
                lrRt.sizeDelta = new Vector2(140f, 120f);
                Image lrImg = lrObj.AddComponent<Image>();
                lrImg.sprite = cLeftRock;
                lrImg.preserveAspect = true;
                lrImg.raycastTarget = false;
            }

            if (cRightRock != null)
            {
                GameObject rrObj = new GameObject("RightRock");
                rrObj.transform.SetParent(contentContainer.transform, false);
                RectTransform rrRt = rrObj.AddComponent<RectTransform>();
                rrRt.anchorMin = new Vector2(1f, 0f);
                rrRt.anchorMax = new Vector2(1f, 0f);
                rrRt.pivot = new Vector2(1f, 0f);
                rrRt.anchoredPosition = new Vector2(20f, 5f);
                rrRt.sizeDelta = new Vector2(140f, 120f);
                Image rrImg = rrObj.AddComponent<Image>();
                rrImg.sprite = cRightRock;
                rrImg.preserveAspect = true;
                rrImg.raycastTarget = false;
            }

            UpdateCharactersScreenVisuals();
        }

        private GameObject CreateModularHeroCard(Transform parent, string heroKey, string cardSpritePath, string portraitSpritePath)
        {
            GameObject cardObj = new GameObject($"Card_{heroKey}");
            cardObj.transform.SetParent(parent, false);
            RectTransform cRt = cardObj.AddComponent<RectTransform>();
            cRt.sizeDelta = new Vector2(280f, 280f);

            // Selection Glow Border (Enabled when this hero is selected)
            GameObject selBorder = new GameObject("SelectionBorder");
            selBorder.transform.SetParent(cardObj.transform, false);
            RectTransform sbRt = selBorder.AddComponent<RectTransform>();
            sbRt.anchorMin = Vector2.zero;
            sbRt.anchorMax = Vector2.one;
            sbRt.offsetMin = new Vector2(-6f, -6f);
            sbRt.offsetMax = new Vector2(6f, 6f);
            Image sbImg = selBorder.AddComponent<Image>();
            sbImg.color = new Color(0f, 0.95f, 1f, 0.85f);
            sbImg.raycastTarget = false;
            Outline sbOutl = selBorder.AddComponent<Outline>();
            sbOutl.effectColor = new Color(0.1f, 1f, 0.5f, 0.95f);
            sbOutl.effectDistance = new Vector2(3f, -3f);
            charCardSelectionBorders[heroKey] = selBorder;
            selBorder.SetActive(heroKey == selectedCharacterKey);

            // Card frame
            Image cardBg = cardObj.AddComponent<Image>();
            Sprite cardSp = LoadSpriteFromUI(cardSpritePath);
            if (cardSp != null) cardBg.sprite = cardSp;
            cardBg.type = Image.Type.Sliced;
            cardBg.color = Color.white;
            cardBg.raycastTarget = true;

            Button cardBtn = cardObj.AddComponent<Button>();
            cardBtn.targetGraphic = cardBg;
            cardBtn.onClick.AddListener(() =>
            {
                StartCoroutine(PunchScale(cardObj.transform, 0.12f, () => SelectCharacter(heroKey)));
            });

            // Hero Portrait
            Sprite portSp = LoadSpriteFromUI(portraitSpritePath);
            GameObject portObj = new GameObject("Portrait");
            portObj.transform.SetParent(cardObj.transform, false);
            RectTransform pRt = portObj.AddComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0.10f, 0.28f);
            pRt.anchorMax = new Vector2(0.90f, 0.94f);
            pRt.offsetMin = Vector2.zero; pRt.offsetMax = Vector2.zero;
            Image portImg = portObj.AddComponent<Image>();
            if (portSp != null) portImg.sprite = portSp;
            portImg.preserveAspect = true;
            portImg.raycastTarget = false;

            // Status / Price Button Pill at bottom of card
            GameObject pillObj = new GameObject("PricePill");
            pillObj.transform.SetParent(cardObj.transform, false);
            RectTransform pillRt = pillObj.AddComponent<RectTransform>();
            pillRt.anchorMin = new Vector2(0.08f, 0.06f);
            pillRt.anchorMax = new Vector2(0.92f, 0.28f);
            pillRt.offsetMin = Vector2.zero; pillRt.offsetMax = Vector2.zero;

            Image pillBg = pillObj.AddComponent<Image>();
            pillBg.type = Image.Type.Sliced;
            pillBg.raycastTarget = false;
            charCardPriceBgs[heroKey] = pillBg;

            TextMeshProUGUI pillTxt = CreateText("Txt", pillObj.transform, "", 20f, Color.white, TextAlignmentOptions.Center, true);
            RectTransform ptRt = pillTxt.GetComponent<RectTransform>();
            ptRt.anchorMin = Vector2.zero; ptRt.anchorMax = Vector2.one;
            ptRt.offsetMin = Vector2.zero; ptRt.offsetMax = Vector2.zero;

            Outline ptOutl = pillTxt.gameObject.AddComponent<Outline>();
            ptOutl.effectColor = new Color(0.01f, 0.05f, 0.15f, 1f);
            ptOutl.effectDistance = new Vector2(1.5f, -1.5f);

            charCardPriceTexts[heroKey] = pillTxt;

            return cardObj;
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

            bool isSoldier = (selectedCharacterKey == "Soldier");

            // 1. Showcase Hero Figure and Name/Role
            if (charPreviewHeroImage != null)
            {
                charPreviewHeroImage.gameObject.SetActive(true);
                if (charPreviewNameText != null) charPreviewNameText.gameObject.SetActive(true);
                if (charPreviewRoleText != null) charPreviewRoleText.gameObject.SetActive(true);

                Sprite charSpr = LoadSpriteFromFile($"Resources/UI/Hero_{selectedCharacterKey}_Full.png")
                              ?? Resources.Load<Sprite>($"UI/Hero_{selectedCharacterKey}_Full")
                              ?? LoadSpriteFromFile($"Resources/UI/Hero_{selectedCharacterKey}_Icon.png")
                              ?? Resources.Load<Sprite>($"UI/Hero_{selectedCharacterKey}_Icon");
                if (charSpr != null) charPreviewHeroImage.sprite = charSpr;

                if (heroes.TryGetValue(selectedCharacterKey, out HeroData hData))
                {
                    if (charPreviewNameText != null) charPreviewNameText.text = hData.name.ToUpper();
                    if (charPreviewRoleText != null) charPreviewRoleText.text = hData.role.ToUpper();
                }
            }

            // 2. Action Button State (Equipped / Equip / Unlock)
            bool isCurrentHeroUnlocked = isSoldier || PlayerPrefs.GetInt("HeroUnlocked_" + selectedCharacterKey, 0) == 1;
            bool isEquipped = (selectedCharacterKey == currentEquipped);

            foreach (var kvp in charCardSelectionBorders)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.SetActive(kvp.Key == selectedCharacterKey);
                }
            }

            if (charPreviewEquipBtnText != null && charPreviewEquipBtnBg != null)
            {
                Outline btnOutline = charPreviewEquipBtnText.GetComponent<Outline>();

                if (isEquipped)
                {
                    charPreviewEquipBtnBg.sprite = LoadSpriteFromUI("UI/CharactersDynamic/27_equipped_button", "UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/13_green_purchase_button_blank", "UI/Btn_3D_Green");
                    charPreviewEquipBtnBg.color = Color.white;
                    charPreviewEquipBtnText.text = "✔ EQUIPPED";
                    if (btnOutline != null) btnOutline.effectColor = new Color(0.04f, 0.22f, 0.08f, 1f);
                }
                else if (!isCurrentHeroUnlocked && heroes.TryGetValue(selectedCharacterKey, out HeroData lData))
                {
                    charPreviewEquipBtnBg.color = Color.white;
                    if (lData.priceCoins > 0)
                    {
                        charPreviewEquipBtnBg.sprite = LoadSpriteFromUI("UI/CharactersDynamic/28_coin_button_blank_base", "UI/Btn_3D_Gold");
                        charPreviewEquipBtnText.text = $"UNLOCK ({lData.priceCoins:N0} COINS)";
                        if (btnOutline != null) btnOutline.effectColor = new Color(0.24f, 0.09f, 0.02f, 1f);
                    }
                    else
                    {
                        charPreviewEquipBtnBg.sprite = LoadSpriteFromUI("UI/CharactersDynamic/29_gem_button_blank_base", "UI/Btn_3D_Cyan");
                        charPreviewEquipBtnText.text = $"UNLOCK ({lData.priceDiamonds} 💎)";
                        if (btnOutline != null) btnOutline.effectColor = new Color(0.02f, 0.12f, 0.25f, 1f);
                    }
                }
                else
                {
                    charPreviewEquipBtnBg.color = Color.white;
                    charPreviewEquipBtnBg.sprite = LoadSpriteFromUI("UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/14_blue_action_button_blank", "UI/Btn_3D_Blue");
                    charPreviewEquipBtnText.text = "EQUIP HERO";
                    if (btnOutline != null) btnOutline.effectColor = new Color(0.04f, 0.12f, 0.32f, 1f);
                }
            }

            // 3. Update All 5 Card Price Labels
            foreach (var kvp in heroes)
            {
                string hKey = kvp.Key;
                HeroData hd = kvp.Value;
                if (!charCardPriceTexts.TryGetValue(hKey, out TextMeshProUGUI pillTxt) || pillTxt == null) continue;
                charCardPriceBgs.TryGetValue(hKey, out Image pillBg);

                bool heroUnlocked = (hKey == "Soldier") || PlayerPrefs.GetInt("HeroUnlocked_" + hKey, 0) == 1;

                if (hKey == currentEquipped)
                {
                    if (pillBg != null)
                    {
                        pillBg.sprite = LoadSpriteFromUI("UI/CharactersDynamic/27_equipped_button", "UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/13_green_purchase_button_blank", "UI/Btn_3D_Green");
                        pillBg.color = Color.white;
                    }
                    pillTxt.text = "EQUIPPED";
                    pillTxt.color = Color.white;
                }
                else if (heroUnlocked)
                {
                    if (pillBg != null)
                    {
                        pillBg.sprite = LoadSpriteFromUI("UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets/14_blue_action_button_blank", "UI/Btn_3D_Blue");
                        pillBg.color = Color.white;
                    }
                    pillTxt.text = "READY";
                    pillTxt.color = Color.white;
                }
                else
                {
                    if (pillBg != null)
                    {
                        if (hd.priceCoins > 0)
                        {
                            pillBg.sprite = LoadSpriteFromUI("UI/CharactersDynamic/28_coin_button_blank_base", "UI/Btn_3D_Gold");
                            pillTxt.text = $"{hd.priceCoins:N0}";
                        }
                        else
                        {
                            pillBg.sprite = LoadSpriteFromUI("UI/CharactersDynamic/29_gem_button_blank_base", "UI/Btn_3D_Cyan");
                            pillTxt.text = $"{hd.priceDiamonds} 💎";
                        }
                        pillBg.color = Color.white;
                    }
                    pillTxt.color = Color.white;
                }
            }
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

            Sprite upgSprite = LoadZombieSquadUpgradeScreenSprite();
            float upgAspect = (upgSprite != null && upgSprite.rect.height > 0)
                ? (upgSprite.rect.width / upgSprite.rect.height)
                : (576f / 1024f);

            // Ambient Scenery Cover (EnvelopeParent fills 100% of any tall/wide mobile display - zero black bars!)
            SetupAmbientSceneryCover(upgradePanel, upgSprite, 576f / 1024f);

            // Master Responsive ArtContainer (FitInParent: ZERO horizontal crop, ZERO stretch)
            GameObject artContainer = new GameObject("ArtContainer");
            artContainer.transform.SetParent(upgradePanel.transform, false);
            RectTransform artRt = artContainer.AddComponent<RectTransform>();
            artRt.anchorMin = Vector2.zero; artRt.anchorMax = Vector2.one;
            artRt.offsetMin = Vector2.zero; artRt.offsetMax = Vector2.zero;

            AspectRatioFitter upgArf = artContainer.AddComponent<AspectRatioFitter>();
            upgArf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            upgArf.aspectRatio = upgAspect;

            GameObject upgImgObj = new GameObject("ArtworkImage");
            upgImgObj.transform.SetParent(artContainer.transform, false);
            RectTransform uiRt = upgImgObj.AddComponent<RectTransform>();
            uiRt.anchorMin = Vector2.zero; uiRt.anchorMax = Vector2.one;
            uiRt.offsetMin = Vector2.zero; uiRt.offsetMax = Vector2.zero;
            Image upgImg = upgImgObj.AddComponent<Image>();
            if (upgSprite != null) upgImg.sprite = upgSprite;
            upgImg.raycastTarget = true; upgImg.preserveAspect = false;

            Shadow upgShadowB = upgImgObj.AddComponent<Shadow>();
            upgShadowB.effectColor = new Color(0.005f, 0.015f, 0.05f, 0.85f);
            upgShadowB.effectDistance = new Vector2(0f, -8f);

            Shadow upgShadowT = upgImgObj.AddComponent<Shadow>();
            upgShadowT.effectColor = new Color(0.005f, 0.015f, 0.05f, 0.85f);
            upgShadowT.effectDistance = new Vector2(0f, 8f);

            // Top Header: Back + Currency HUD
            CreateTopCurrencyHeader(artContainer.transform, "SQUAD UPGRADE", () => ShowZombieSquadHomeScreen());

            // 4 Upgrade Cards Dynamic Overlays:
            // 1. Soldier Count (Top-Left)
            SetupSquadUpgradeCard(artContainer.transform, 0, "Count", "Soldier Count", 200, false,
                new Vector2(0.075f, 0.643f), new Vector2(0.435f, 0.681f),
                new Vector2(0.060f, 0.560f), new Vector2(0.445f, 0.624f));

            // 2. Soldier Health (Top-Right)
            SetupSquadUpgradeCard(artContainer.transform, 1, "Health", "Soldier Health", 150, false,
                new Vector2(0.569f, 0.643f), new Vector2(0.929f, 0.681f),
                new Vector2(0.557f, 0.560f), new Vector2(0.938f, 0.624f));

            // 3. Soldier Damage (Bottom-Left)
            SetupSquadUpgradeCard(artContainer.transform, 2, "Damage", "Soldier Damage", 180, false,
                new Vector2(0.075f, 0.241f), new Vector2(0.435f, 0.279f),
                new Vector2(0.060f, 0.158f), new Vector2(0.445f, 0.222f));

            // 4. Soldier Speed (Bottom-Right)
            SetupSquadUpgradeCard(artContainer.transform, 3, "Speed", "Soldier Speed", 5, true,
                new Vector2(0.569f, 0.241f), new Vector2(0.929f, 0.279f),
                new Vector2(0.557f, 0.158f), new Vector2(0.938f, 0.222f));

            UpdateUpgradeScreenVisuals();
        }

        private void SetupSquadUpgradeCard(Transform parent, int idx, string key, string label, int baseCost, bool isDiamonds, Vector2 barMin, Vector2 barMax, Vector2 btnMin, Vector2 btnMax)
        {
            // Bar container
            GameObject barObj = new GameObject($"Upg_Bar_{key}");
            barObj.transform.SetParent(parent, false);
            RectTransform bRt = barObj.AddComponent<RectTransform>();
            bRt.anchorMin = barMin;
            bRt.anchorMax = barMax;
            bRt.offsetMin = Vector2.zero;
            bRt.offsetMax = Vector2.zero;

            // Rounded Progress Fill inside capsule
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(barObj.transform, false);
            RectTransform fRt = fillObj.AddComponent<RectTransform>();
            fRt.anchorMin = Vector2.zero;
            fRt.anchorMax = new Vector2(0.1f, 1f);
            fRt.offsetMin = new Vector2(3f, 3f);
            fRt.offsetMax = new Vector2(-3f, -3f);
            Image fImg = fillObj.AddComponent<Image>();
            fImg.sprite = CreateRoundedRectSprite(180, 32, 10, isDiamonds ? new Color(0.10f, 0.75f, 0.95f, 0.95f) : new Color(0.18f, 0.85f, 0.35f, 0.95f), new Color(0.5f, 1f, 0.6f, 0.4f), 1);
            fImg.type = Image.Type.Simple;
            fImg.raycastTarget = false;
            upgradeStatFills[idx] = fImg;

            // Centered Level text inside capsule
            upgradeStatLevelTexts[idx] = CreateText("LevelTxt", barObj.transform, "LEVEL 1", 16f, Color.white, TextAlignmentOptions.Center, true);
            RectTransform ltRt = upgradeStatLevelTexts[idx].GetComponent<RectTransform>();
            ltRt.anchorMin = Vector2.zero;
            ltRt.anchorMax = Vector2.one;
            ltRt.offsetMin = Vector2.zero;
            ltRt.offsetMax = Vector2.zero;
            Outline lvlOutl = upgradeStatLevelTexts[idx].gameObject.AddComponent<Outline>();
            lvlOutl.effectColor = new Color(0.01f, 0.05f, 0.15f, 0.9f);
            lvlOutl.effectDistance = new Vector2(1.2f, -1.2f);

            // Button label (centered in the right portion of the green button next to emblem)
            GameObject btnLabelObj = new GameObject($"Upg_BtnLabel_{key}");
            btnLabelObj.transform.SetParent(parent, false);
            RectTransform blRt = btnLabelObj.AddComponent<RectTransform>();
            float btnWidth = btnMax.x - btnMin.x;
            blRt.anchorMin = new Vector2(btnMin.x + btnWidth * 0.35f, btnMin.y);
            blRt.anchorMax = new Vector2(btnMax.x - btnWidth * 0.05f, btnMax.y);
            blRt.offsetMin = Vector2.zero;
            blRt.offsetMax = Vector2.zero;
            upgradeStatCostTexts[idx] = CreateText("CostTxt", btnLabelObj.transform, $"{baseCost}", 24f, Color.white, TextAlignmentOptions.Center, true);
            RectTransform ctRt = upgradeStatCostTexts[idx].GetComponent<RectTransform>();
            ctRt.anchorMin = Vector2.zero;
            ctRt.anchorMax = Vector2.one;
            ctRt.offsetMin = Vector2.zero;
            ctRt.offsetMax = Vector2.zero;
            Outline costOutl = upgradeStatCostTexts[idx].gameObject.AddComponent<Outline>();
            costOutl.effectColor = new Color(0.08f, 0.35f, 0.15f, 1f);
            costOutl.effectDistance = new Vector2(1.5f, -1.5f);

            // Button click
            CreateTransparentButton($"Btn_Upg_{key}", parent, btnMin, btnMax, () => UpgradeSquadStat(key, idx, baseCost, isDiamonds));
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
            string[] keys = new string[] { "Count", "Health", "Damage", "Speed" };
            int[] baseCosts = new int[] { 200, 150, 180, 5 };
            bool[] isDias = new bool[] { false, false, false, true };

            for (int i = 0; i < 4; i++)
            {
                int lvl = PlayerPrefs.GetInt($"ZombieSquad_Upgrade_{keys[i]}", 1);
                float progress = Mathf.Clamp01(lvl / 10f);
                if (upgradeStatFills[i] != null)
                {
                    upgradeStatFills[i].rectTransform.anchorMax = new Vector2(Mathf.Max(0.1f, progress), 1f);
                }
                if (upgradeStatLevelTexts[i] != null)
                {
                    upgradeStatLevelTexts[i].text = (lvl >= 10) ? "MAX LEVEL" : $"LEVEL {lvl}";
                }
                if (upgradeStatCostTexts[i] != null)
                {
                    if (lvl >= 10)
                    {
                        upgradeStatCostTexts[i].text = "MAX";
                    }
                    else
                    {
                        int cost = baseCosts[i] * lvl;
                        upgradeStatCostTexts[i].text = $"{cost:N0}";
                    }
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


        private void BuildZombieSquadWeaponsScreenPanel(Transform canvasRoot)
        {
            weaponsPanel = CreateFullPanel("ZombieSquadWeaponsPanel", canvasRoot, new Color(0.04f, 0.06f, 0.10f, 1f));

            Sprite wepSprite = LoadZombieSquadWeaponsScreenSprite();
            float wepAspect = (wepSprite != null && wepSprite.rect.height > 0)
                ? (wepSprite.rect.width / wepSprite.rect.height)
                : (576f / 1024f);

            // Ambient Scenery Cover (EnvelopeParent fills 100% of any tall/wide mobile display - zero black bars!)
            SetupAmbientSceneryCover(weaponsPanel, wepSprite, 576f / 1024f);

            // Master Responsive ArtContainer (FitInParent: ZERO horizontal crop, ZERO stretch)
            GameObject artContainer = new GameObject("ArtContainer");
            artContainer.transform.SetParent(weaponsPanel.transform, false);
            RectTransform artRt = artContainer.AddComponent<RectTransform>();
            artRt.anchorMin = Vector2.zero; artRt.anchorMax = Vector2.one;
            artRt.offsetMin = Vector2.zero; artRt.offsetMax = Vector2.zero;

            AspectRatioFitter wepArf = artContainer.AddComponent<AspectRatioFitter>();
            wepArf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            wepArf.aspectRatio = wepAspect;

            GameObject wepImgObj = new GameObject("ArtworkImage");
            wepImgObj.transform.SetParent(artContainer.transform, false);
            RectTransform wiRt = wepImgObj.AddComponent<RectTransform>();
            wiRt.anchorMin = Vector2.zero; wiRt.anchorMax = Vector2.one;
            wiRt.offsetMin = Vector2.zero; wiRt.offsetMax = Vector2.zero;
            Image wepImg = wepImgObj.AddComponent<Image>();
            if (wepSprite != null) wepImg.sprite = wepSprite;
            wepImg.raycastTarget = true; wepImg.preserveAspect = false;

            Shadow wepShadowB = wepImgObj.AddComponent<Shadow>();
            wepShadowB.effectColor = new Color(0.005f, 0.015f, 0.05f, 0.85f);
            wepShadowB.effectDistance = new Vector2(0f, -8f);

            Shadow wepShadowT = wepImgObj.AddComponent<Shadow>();
            wepShadowT.effectColor = new Color(0.005f, 0.015f, 0.05f, 0.85f);
            wepShadowT.effectDistance = new Vector2(0f, 8f);

            // Top Header: Back + Currency HUD
            CreateTopCurrencyHeader(artContainer.transform, "WEAPONS", () => ShowZombieSquadHomeScreen());

            // Top Chamber Weapon Preview Display (Floats on Pedestal!)
            GameObject chamberWeaponObj = new GameObject("ChamberWeaponDisplay");
            chamberWeaponObj.transform.SetParent(artContainer.transform, false);
            RectTransform cwRt = chamberWeaponObj.AddComponent<RectTransform>();
            cwRt.anchorMin = new Vector2(0.18f, 0.58f);
            cwRt.anchorMax = new Vector2(0.82f, 0.82f);
            cwRt.offsetMin = Vector2.zero;
            cwRt.offsetMax = Vector2.zero;
            weaponChamberImg = chamberWeaponObj.AddComponent<Image>();
            weaponChamberImg.preserveAspect = true;
            weaponChamberImg.raycastTarget = false;

            // Carousel navigation arrows
            CreateTransparentButton("Btn_Prev_Weapon", artContainer.transform, new Vector2(0.010f, 0.700f), new Vector2(0.100f, 0.790f), () => CycleWeapon(-1));
            CreateTransparentButton("Btn_Next_Weapon", artContainer.transform, new Vector2(0.900f, 0.700f), new Vector2(0.990f, 0.790f), () => CycleWeapon(1));

            // Weapon Title (Directly inside clean banner frame on background)
            weaponTitleText = CreateText("WepTitle", artContainer.transform, "ASSAULT RIFLE", 34f, Color.white, TextAlignmentOptions.MidlineLeft, true);
            RectTransform wtRt = weaponTitleText.GetComponent<RectTransform>();
            wtRt.anchorMin = new Vector2(0.080f, 0.525f);
            wtRt.anchorMax = new Vector2(0.690f, 0.582f);
            wtRt.offsetMin = Vector2.zero;
            wtRt.offsetMax = Vector2.zero;
            weaponTitleText.raycastTarget = false;

            // Overall Weapon Level Badge (Right side of banner)
            weaponLevelText = CreateText("WepLevel", artContainer.transform, "LV. 1", 26f, new Color(1f, 0.88f, 0.25f, 1f), TextAlignmentOptions.Center, true);
            RectTransform wlRt = weaponLevelText.GetComponent<RectTransform>();
            wlRt.anchorMin = new Vector2(0.725f, 0.525f);
            wlRt.anchorMax = new Vector2(0.940f, 0.582f);
            wlRt.offsetMin = Vector2.zero;
            wlRt.offsetMax = Vector2.zero;
            weaponLevelText.raycastTarget = false;

            // 4 Stat Rows: Damage, Fire Rate, Range, Magazine
            // Exact Slot Bounds (does NOT overlap text "Damage", "Fire Rate", "Range", "Magazine"):
            float[] slotYMin = new float[] { 0.450f, 0.370f, 0.292f, 0.214f };
            float[] slotYMax = new float[] { 0.473f, 0.394f, 0.315f, 0.237f };
            float[] btnYMin  = new float[] { 0.445f, 0.367f, 0.288f, 0.210f };
            float[] btnYMax  = new float[] { 0.505f, 0.425f, 0.346f, 0.268f };
            string[] statNames = new string[] { "Damage", "FireRate", "Range", "Magazine" };

            for (int i = 0; i < 4; i++)
            {
                string sName = statNames[i];
                float sMin = slotYMin[i];
                float sMax = slotYMax[i];
                float bMin = btnYMin[i];
                float bMax = btnYMax[i];

                // Progress Bar Container (Fitted precisely inside the dark blue slot on background)
                GameObject barObj = new GameObject($"Wep_StatBar_{sName}");
                barObj.transform.SetParent(artContainer.transform, false);
                RectTransform bRt = barObj.AddComponent<RectTransform>();
                bRt.anchorMin = new Vector2(0.185f, sMin);
                bRt.anchorMax = new Vector2(0.625f, sMax);
                bRt.offsetMin = Vector2.zero;
                bRt.offsetMax = Vector2.zero;

                // Track Background (Covers static baked text inside slot)
                GameObject trackBgObj = new GameObject("TrackBg");
                trackBgObj.transform.SetParent(barObj.transform, false);
                RectTransform trRt = trackBgObj.AddComponent<RectTransform>();
                trRt.anchorMin = Vector2.zero;
                trRt.anchorMax = Vector2.one;
                trRt.offsetMin = Vector2.zero;
                trRt.offsetMax = Vector2.zero;
                Image trackBgImg = trackBgObj.AddComponent<Image>();
                trackBgImg.color = new Color(0.01f, 0.08f, 0.22f, 0.95f);
                trackBgImg.raycastTarget = false;

                // Dynamic Glowing Neon Green Fill
                GameObject fillObj = new GameObject("Fill");
                fillObj.transform.SetParent(barObj.transform, false);
                RectTransform fRt = fillObj.AddComponent<RectTransform>();
                fRt.anchorMin = Vector2.zero;
                fRt.anchorMax = new Vector2(0.15f, 1f);
                fRt.offsetMin = Vector2.zero;
                fRt.offsetMax = Vector2.zero;
                weaponStatFills[i] = fillObj.AddComponent<Image>();
                weaponStatFills[i].color = new Color(0.15f, 0.88f, 0.45f, 1f);
                weaponStatFills[i].raycastTarget = false;

                // Value / Level Label inside the slot
                weaponStatValTexts[i] = CreateText("ValTxt", barObj.transform, "LV. 1 / 10", 14f, Color.white, TextAlignmentOptions.Center, true);
                RectTransform vRt = weaponStatValTexts[i].GetComponent<RectTransform>();
                vRt.anchorMin = Vector2.zero;
                vRt.anchorMax = Vector2.one;
                vRt.offsetMin = Vector2.zero;
                vRt.offsetMax = Vector2.zero;
                weaponStatValTexts[i].raycastTarget = false;

                // Green Upgrade Arrow Button (Exact bounds, reliable click registration)
                Button upgBtn = CreateTransparentButton($"Btn_Upg_{sName}", artContainer.transform, new Vector2(0.765f, bMin), new Vector2(0.950f, bMax), () => UpgradeWeaponStat(sName));

                // Cost label directly as a child of the button with raycastTarget = false
                weaponStatCostTexts[i] = CreateText("CostTxt", upgBtn.transform, "100", 15f, Color.white, TextAlignmentOptions.Bottom, true);
                RectTransform cRt = weaponStatCostTexts[i].GetComponent<RectTransform>();
                cRt.anchorMin = Vector2.zero;
                cRt.anchorMax = Vector2.one;
                cRt.offsetMin = Vector2.zero;
                cRt.offsetMax = Vector2.zero;
                cRt.anchoredPosition = new Vector2(0f, 6f);
                weaponStatCostTexts[i].raycastTarget = false;
            }


            // Selection Highlight Border Frame that moves to the active card
            GameObject highlightObj = new GameObject("WeaponCardHighlight");
            highlightObj.transform.SetParent(artContainer.transform, false);
            weaponCardSelectionHighlightRt = highlightObj.AddComponent<RectTransform>();
            weaponCardSelectionHighlightRt.anchorMin = new Vector2(0.018f, 0.022f);
            weaponCardSelectionHighlightRt.anchorMax = new Vector2(0.262f, 0.162f);
            weaponCardSelectionHighlightRt.offsetMin = Vector2.zero;
            weaponCardSelectionHighlightRt.offsetMax = Vector2.zero;

            // 4 Yellow Border lines around the selected card
            CreateBorderLine(highlightObj.transform, new Vector2(0f, 0.94f), new Vector2(1f, 1f), new Color(1f, 0.85f, 0.10f, 1f));
            CreateBorderLine(highlightObj.transform, new Vector2(0f, 0f), new Vector2(1f, 0.06f), new Color(1f, 0.85f, 0.10f, 1f));
            CreateBorderLine(highlightObj.transform, new Vector2(0f, 0f), new Vector2(0.06f, 1f), new Color(1f, 0.85f, 0.10f, 1f));
            CreateBorderLine(highlightObj.transform, new Vector2(0.94f, 0f), new Vector2(1f, 1f), new Color(1f, 0.85f, 0.10f, 1f));

            // 4 Bottom Weapon Selection Cards Buttons
            CreateTransparentButton("Btn_Weapon_AssaultRifle", artContainer.transform, new Vector2(0.018f, 0.022f), new Vector2(0.262f, 0.162f), () => SelectWeapon("AssaultRifle"));
            CreateTransparentButton("Btn_Weapon_Shotgun",      artContainer.transform, new Vector2(0.263f, 0.022f), new Vector2(0.507f, 0.162f), () => SelectWeapon("Shotgun"));
            CreateTransparentButton("Btn_Weapon_Sniper",       artContainer.transform, new Vector2(0.508f, 0.022f), new Vector2(0.752f, 0.162f), () => SelectWeapon("Sniper"));
            CreateTransparentButton("Btn_Weapon_Minigun",      artContainer.transform, new Vector2(0.753f, 0.022f), new Vector2(0.982f, 0.162f), () => SelectWeapon("Minigun"));

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
                weaponTitleText.text = currentWeapon.Replace("AssaultRifle", "ASSAULT RIFLE").ToUpper();
            }

            // 2. Update Chamber Weapon Display Sprite
            if (weaponChamberImg != null)
            {
                weaponChamberImg.sprite = LoadWeaponSprite(currentWeapon);
            }

            // 3. Update Bottom Selection Highlight Border
            int wepIdx = System.Array.IndexOf(s_weaponKeys, currentWeapon);
            if (wepIdx < 0) wepIdx = 0;

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
                    weaponStatValTexts[i].text = lvl >= 10 ? "MAX (LV. 10)" : $"LV. {lvl} / 10";
                }
                if (weaponStatCostTexts[i] != null)
                {
                    weaponStatCostTexts[i].text = lvl >= 10 ? "MAX" : $"{100 * lvl}";
                }
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

        private void BuildZombieSquadShopScreenPanel(Transform canvasRoot)
        {
            if (shopPanel != null) SafeDestroy(shopPanel);
            Transform oldShop = canvasRoot.Find("ZombieSquadShopPanel");
            if (oldShop != null) SafeDestroy(oldShop.gameObject);

            shopPanel = CreateFullPanel("ZombieSquadShopPanel", canvasRoot, new Color(0.01f, 0.03f, 0.08f, 1f));

            // 1. Ambient Scenery Cover (100% full screen night battlefield cover, ZERO daytime sky bleed, ZERO gaps)
            Sprite nightScenerySprite = LoadSpriteFromUI("UI/Scenery_NightBattlefield_Clean");
            SetupAmbientSceneryCover(shopPanel, nightScenerySprite, 1080f / 1920f);

            // 2. Master Responsive ArtContainer (Bottom-anchored: 100% flush with bottom edge, ZERO floating gap)
            Sprite shopMasterSprite = LoadSpriteFromUI(
                "UI/ZombieSquad_ShopScreen_Master_HD",
                "UI/ZombieSquad_ShopScreen"
            );

            GameObject artContainer = new GameObject("ArtContainer");
            artContainer.transform.SetParent(shopPanel.transform, false);
            RectTransform artRt = artContainer.AddComponent<RectTransform>();
            artRt.anchorMin = new Vector2(0.5f, 0f);
            artRt.anchorMax = new Vector2(0.5f, 0f);
            artRt.pivot = new Vector2(0.5f, 0f);
            artRt.anchoredPosition = Vector2.zero;
            artRt.sizeDelta = new Vector2(1080f, 1920f);

            // Artwork Image (Pristine 1080x1920 HD Master Artwork with uncompressed art, complete glowing chests & round coins)
            GameObject artImgObj = new GameObject("ArtworkImage");
            artImgObj.transform.SetParent(artContainer.transform, false);
            RectTransform miRt = artImgObj.AddComponent<RectTransform>();
            miRt.anchorMin = Vector2.zero;
            miRt.anchorMax = Vector2.one;
            miRt.offsetMin = Vector2.zero;
            miRt.offsetMax = Vector2.zero;

            Image shopImg = artImgObj.AddComponent<Image>();
            if (shopMasterSprite != null) shopImg.sprite = shopMasterSprite;
            shopImg.raycastTarget = false;
            shopImg.preserveAspect = false;
            shopScreenBgImage = shopImg;

            shopCoinsContainer = artContainer; // Cache for health check

            // 3. Interactive Navigation
            // Top Back Button (Click target directly over the visible [<-] Back button)
            GameObject backBtnObj = new GameObject("Btn_Back");
            backBtnObj.transform.SetParent(artContainer.transform, false);
            RectTransform bRt = backBtnObj.AddComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.120f, 0.760f);
            bRt.anchorMax = new Vector2(0.250f, 0.830f);
            bRt.offsetMin = Vector2.zero;
            bRt.offsetMax = Vector2.zero;
            shopBackBtnRect = bRt;

            Image bImg = backBtnObj.AddComponent<Image>();
            bImg.color = new Color(1f, 1f, 1f, 0.001f);
            bImg.raycastTarget = true;
            Button bBtn = backBtnObj.AddComponent<Button>();
            bBtn.targetGraphic = bImg;
            bBtn.onClick.AddListener(() =>
            {
                StartCoroutine(PunchScale(backBtnObj.transform, 0.10f, () => ShowZombieSquadHomeScreen()));
            });

            // Currency Plus Buttons
            CreateTransparentTouchButton(artContainer.transform, "Btn_Plus_Coins",
                new Vector2(0.600f, 0.775f), new Vector2(0.665f, 0.815f), () => OpenShopCheckoutModal("1,000 COINS", 80, () => AddPlayerCoins(1000)));
            CreateTransparentTouchButton(artContainer.transform, "Btn_Plus_Diamonds",
                new Vector2(0.775f, 0.775f), new Vector2(0.840f, 0.815f), () => OpenShopCheckoutModal("5,000 COINS", 200, () => AddPlayerCoins(5000)));

            // Dynamic Live Currency Numbers inside header capsules
            TextMeshProUGUI liveCoinsTmp = CreateText(
                "LiveCoinsText",
                artContainer.transform,
                GetPlayerCoins().ToString("N0"),
                24f,
                new Color(1f, 0.95f, 0.40f, 1f),
                TextAlignmentOptions.Right,
                true
            );
            RectTransform lcRt = liveCoinsTmp.rectTransform;
            lcRt.anchorMin = new Vector2(0.520f, 0.780f);
            lcRt.anchorMax = new Vector2(0.605f, 0.815f);
            lcRt.offsetMin = Vector2.zero;
            lcRt.offsetMax = Vector2.zero;
            if (!allCoinsDisplays.Contains(liveCoinsTmp)) allCoinsDisplays.Add(liveCoinsTmp);

            TextMeshProUGUI liveDiamondsTmp = CreateText(
                "LiveDiamondsText",
                artContainer.transform,
                GetPlayerDiamonds().ToString("N0"),
                24f,
                new Color(0.60f, 0.92f, 1f, 1f),
                TextAlignmentOptions.Right,
                true
            );
            RectTransform ldRt = liveDiamondsTmp.rectTransform;
            ldRt.anchorMin = new Vector2(0.700f, 0.780f);
            ldRt.anchorMax = new Vector2(0.780f, 0.815f);
            ldRt.offsetMin = Vector2.zero;
            ldRt.offsetMax = Vector2.zero;
            if (!allDiamondsDisplays.Contains(liveDiamondsTmp)) allDiamondsDisplays.Add(liveDiamondsTmp);

            // 4. Four Dynamic Product Card Touch Targets & Buy CTAs
            // Card 1: 1,000 Coins (₹ 80)
            CreateShopProductTouchArea(artContainer.transform, "Card_1000", "Btn_Buy_1000_Coins",
                new Vector2(0.115f, 0.485f), new Vector2(0.470f, 0.760f),
                1000, 80);

            // Card 2: 5,000 Coins (₹ 200)
            CreateShopProductTouchArea(artContainer.transform, "Card_5000", "Btn_Buy_5000_Coins",
                new Vector2(0.530f, 0.485f), new Vector2(0.885f, 0.760f),
                5000, 200);

            // Card 3: 20,000 Coins (₹ 1,000)
            CreateShopProductTouchArea(artContainer.transform, "Card_20000", "Btn_Buy_20000_Coins",
                new Vector2(0.115f, 0.200f), new Vector2(0.470f, 0.480f),
                20000, 1000);

            // Card 4: 50,000 Coins (₹ 2,000)
            CreateShopProductTouchArea(artContainer.transform, "Card_50000", "Btn_Buy_50000_Coins",
                new Vector2(0.530f, 0.200f), new Vector2(0.885f, 0.480f),
                50000, 2000);

            // 5. Checkout Modal
            BuildShopCheckoutModal(shopPanel.transform);
        }

        private GameObject CreateShopProductTouchArea(
            Transform parent,
            string cardName,
            string buttonName,
            Vector2 cardAnchorMin,
            Vector2 cardAnchorMax,
            int coinsAmount,
            int inrPrice)
        {
            // Container representing Card (satisfies Find("Card_1000"))
            GameObject cardObj = new GameObject(cardName);
            cardObj.transform.SetParent(parent, false);
            RectTransform cRt = cardObj.AddComponent<RectTransform>();
            cRt.anchorMin = cardAnchorMin;
            cRt.anchorMax = cardAnchorMax;
            cRt.offsetMin = Vector2.zero;
            cRt.offsetMax = Vector2.zero;

            // Clickable Buy Button directly under parent (satisfies Find("Btn_Buy_1000_Coins"))
            GameObject btnObj = new GameObject(buttonName);
            btnObj.transform.SetParent(parent, false);
            RectTransform bRt = btnObj.AddComponent<RectTransform>();
            bRt.anchorMin = cardAnchorMin;
            bRt.anchorMax = cardAnchorMax;
            bRt.offsetMin = Vector2.zero;
            bRt.offsetMax = Vector2.zero;

            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(1f, 1f, 1f, 0.001f); // Invisible touch target over the pre-rendered 3D button
            btnImg.raycastTarget = true;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.onClick.AddListener(() =>
            {
                StartCoroutine(PunchScale(cardObj.transform, 0.12f, () =>
                {
                    OpenShopCheckoutModal($"{coinsAmount:N0} COINS", inrPrice, () =>
                    {
                        AddPlayerCoins(coinsAmount);
                        ShowMultiplierPop($"🎉 +{coinsAmount:N0} COINS!");
                    });
                }));
            });

            return cardObj;
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

        private void BuildZombieSquadDailyScreenPanel(Transform canvasRoot)
        {
            dailyPanel = CreateFullPanel("ZombieSquadDailyPanel", canvasRoot, new Color(0.04f, 0.06f, 0.10f, 1f));

            Sprite dailySprite = LoadZombieSquadDailyScreenSprite();
            float dailyAspect = (dailySprite != null && dailySprite.rect.height > 0)
                ? (dailySprite.rect.width / dailySprite.rect.height)
                : (576f / 1024f);

            // Ambient Scenery Cover (EnvelopeParent fills 100% of any tall/wide mobile display - zero black bars!)
            SetupAmbientSceneryCover(dailyPanel, dailySprite, 576f / 1024f);

            // Master Responsive ArtContainer (FitInParent: ZERO horizontal crop, ZERO stretch)
            GameObject artContainer = new GameObject("ArtContainer");
            artContainer.transform.SetParent(dailyPanel.transform, false);
            RectTransform artRt = artContainer.AddComponent<RectTransform>();
            artRt.anchorMin = Vector2.zero; artRt.anchorMax = Vector2.one;
            artRt.offsetMin = Vector2.zero; artRt.offsetMax = Vector2.zero;

            AspectRatioFitter dailyArf = artContainer.AddComponent<AspectRatioFitter>();
            dailyArf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            dailyArf.aspectRatio = dailyAspect;

            GameObject dailyImgObj = new GameObject("ArtworkImage");
            dailyImgObj.transform.SetParent(artContainer.transform, false);
            RectTransform diRt = dailyImgObj.AddComponent<RectTransform>();
            diRt.anchorMin = Vector2.zero; diRt.anchorMax = Vector2.one;
            diRt.offsetMin = Vector2.zero; diRt.offsetMax = Vector2.zero;
            Image dailyImg = dailyImgObj.AddComponent<Image>();
            if (dailySprite != null) dailyImg.sprite = dailySprite;
            dailyImg.raycastTarget = true; dailyImg.preserveAspect = false;

            Shadow dailyShadowB = dailyImgObj.AddComponent<Shadow>();
            dailyShadowB.effectColor = new Color(0.005f, 0.015f, 0.05f, 0.85f);
            dailyShadowB.effectDistance = new Vector2(0f, -8f);

            Shadow dailyShadowT = dailyImgObj.AddComponent<Shadow>();
            dailyShadowT.effectColor = new Color(0.005f, 0.015f, 0.05f, 0.85f);
            dailyShadowT.effectDistance = new Vector2(0f, 8f);

            // Top Header: Back + Currency HUD
            CreateTopCurrencyHeader(artContainer.transform, "DAILY", () => ShowZombieSquadHomeScreen());

            // 7 Days Columns & Status Overlays
            int[] dailyAmounts = new int[] { 10, 20, 30, 40, 50, 60, 100 };
            float[] colStarts = new float[] { 0.020f, 0.155f, 0.292f, 0.429f, 0.566f, 0.703f, 0.840f };
            float[] colEnds = new float[] { 0.155f, 0.292f, 0.429f, 0.566f, 0.703f, 0.840f, 0.980f };

            for (int i = 0; i < 7; i++)
            {
                int day = i + 1;
                int rewardCoins = dailyAmounts[i];
                Vector2 min = new Vector2(colStarts[i], 0.300f);
                Vector2 max = new Vector2(colEnds[i], 0.530f);

                // Click button for today
                CreateTransparentButton($"Btn_Day_{day}", artContainer.transform, min, max, () => ClaimDailyReward(day, rewardCoins));

                // Status text at bottom of day card
                GameObject statusObj = new GameObject($"DayStatus_{day}");
                statusObj.transform.SetParent(artContainer.transform, false);
                RectTransform sRt = statusObj.AddComponent<RectTransform>();
                sRt.anchorMin = new Vector2(colStarts[i], 0.305f);
                sRt.anchorMax = new Vector2(colEnds[i], 0.355f);
                sRt.offsetMin = Vector2.zero;
                sRt.offsetMax = Vector2.zero;
                dailyDayStatusTexts[i] = CreateText("StatusTxt", statusObj.transform, $"+{rewardCoins}", 17f, new Color(1f, 0.88f, 0.25f, 1f), TextAlignmentOptions.Center, true);
                dailyDayStatusTexts[i].GetComponent<RectTransform>().sizeDelta = new Vector2(120f, 35f);
            }

            // Big Central Claim Button for today (AAA rounded gradient styling)
            GameObject claimBtnObj = new GameObject("Btn_ClaimDailyToday");
            claimBtnObj.transform.SetParent(artContainer.transform, false);
            RectTransform cbRt = claimBtnObj.AddComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(0.180f, 0.215f);
            cbRt.anchorMax = new Vector2(0.820f, 0.280f);
            cbRt.offsetMin = Vector2.zero;
            cbRt.offsetMax = Vector2.zero;
            dailyClaimBtnBg = claimBtnObj.AddComponent<Image>();
            dailyClaimBtnBg.sprite = CreateRoundedRectSprite(360, 68, 18, new Color(0.12f, 0.78f, 0.35f, 1f), new Color(0.40f, 0.95f, 0.50f, 1f), 3);
            dailyClaimBtnBg.type = Image.Type.Simple;
            dailyClaimBtn = claimBtnObj.AddComponent<Button>();
            dailyClaimBtn.onClick.AddListener(() =>
            {
                int currentDay = PlayerPrefs.GetInt("ZombieSquad_DailyCurrentDay", 1);
                int reward = (currentDay <= 7) ? dailyAmounts[currentDay - 1] : 50;
                ClaimDailyReward(currentDay, reward);
            });
            dailyClaimBtnText = CreateText("ClaimTxt", claimBtnObj.transform, "COLLECT DAILY REWARD", 22f, Color.white, TextAlignmentOptions.Center, true);
            dailyClaimBtnText.GetComponent<RectTransform>().sizeDelta = new Vector2(400f, 60f);

            UpdateDailyScreenVisuals();
        }

        public void ClaimDailyReward(int day, int coinsReward)
        {
            string prefKey = $"ZombieSquad_DailyClaimed_Day_{day}";
            if (PlayerPrefs.GetInt(prefKey, 0) == 1)
            {
                Debug.Log($"[ZombieSquad] Day {day} reward already claimed!");
                return;
            }

            PlayerPrefs.SetInt(prefKey, 1);
            int nextDay = Mathf.Min(7, day + 1);
            PlayerPrefs.SetInt("ZombieSquad_DailyCurrentDay", nextDay);
            PlayerPrefs.Save();

            AddPlayerCoins(coinsReward);
            if (day == 7) AddPlayerDiamonds(10);

            if (MobControl.Audio.ZombieSoundManager.Instance != null)
            {
                MobControl.Audio.ZombieSoundManager.Instance.PlayCoinSpend();
            }

            Debug.Log($"[ZombieSquad] Claimed Day {day} Reward: +{coinsReward} Coins! New Balance: {GetPlayerCoins()}");
            UpdateDailyScreenVisuals();
        }

        public void UpdateDailyScreenVisuals()
        {
            int currentDay = PlayerPrefs.GetInt("ZombieSquad_DailyCurrentDay", 1);
            int[] dailyAmounts = new int[] { 10, 20, 30, 40, 50, 60, 100 };

            for (int i = 0; i < 7; i++)
            {
                int day = i + 1;
                bool isClaimed = PlayerPrefs.GetInt($"ZombieSquad_DailyClaimed_Day_{day}", 0) == 1;
                if (dailyDayStatusTexts[i] != null)
                {
                    if (isClaimed)
                    {
                        dailyDayStatusTexts[i].text = "✓ DONE";
                        dailyDayStatusTexts[i].color = new Color(0.35f, 1f, 0.5f, 0.95f);
                    }
                    else
                    {
                        // Clean: Keep blank so artwork's native crisp labels (+10, +20, etc.) are fully visible
                        dailyDayStatusTexts[i].text = "";
                    }
                }
            }

            bool todayClaimed = PlayerPrefs.GetInt($"ZombieSquad_DailyClaimed_Day_{currentDay}", 0) == 1;
            if (dailyClaimBtnText != null && dailyClaimBtnBg != null)
            {
                if (todayClaimed)
                {
                    dailyClaimBtnText.text = "✓ ALL REWARDS CLAIMED";
                    dailyClaimBtnBg.sprite = CreateRoundedRectSprite(360, 68, 18, new Color(0.28f, 0.32f, 0.40f, 1f), new Color(0.40f, 0.45f, 0.55f, 1f), 2);
                    dailyClaimBtn.interactable = false;
                }
                else
                {
                    dailyClaimBtnText.text = $"COLLECT DAY {currentDay} (+{dailyAmounts[currentDay - 1]} COINS)";
                    dailyClaimBtnBg.sprite = CreateRoundedRectSprite(360, 68, 18, new Color(0.12f, 0.78f, 0.35f, 1f), new Color(0.40f, 0.95f, 0.50f, 1f), 3);
                    dailyClaimBtn.interactable = true;
                }
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

            // Top Header Bar (Matches user reference screenshot exactly)
            GameObject headerBar = new GameObject("TopHeaderBar");
            headerBar.transform.SetParent(gameplayHudPanel.transform, false);
            RectTransform hbRt = headerBar.AddComponent<RectTransform>();
            hbRt.anchorMin = new Vector2(0.5f, 1f);
            hbRt.anchorMax = new Vector2(0.5f, 1f);
            hbRt.pivot = new Vector2(0.5f, 1f);
            hbRt.sizeDelta = new Vector2(810f, 90f);
            hbRt.anchoredPosition = new Vector2(0f, -60f);

            // 1. LEVEL Pill Badge (Deep navy with soft slate blue border, bold white text)
            GameObject levelPill = new GameObject("LevelPill");
            levelPill.transform.SetParent(headerBar.transform, false);
            RectTransform lpRt = levelPill.AddComponent<RectTransform>();
            lpRt.anchorMin = new Vector2(0f, 0.5f);
            lpRt.anchorMax = new Vector2(0f, 0.5f);
            lpRt.pivot = new Vector2(0f, 0.5f);
            lpRt.sizeDelta = new Vector2(185f, 58f);
            lpRt.anchoredPosition = new Vector2(0f, 0f);

            Image lpImg = levelPill.AddComponent<Image>();
            Sprite pillSprite = LoadSpriteFromFile("Resources/UI/Ref_LevelPill_Bg.png");
            if (pillSprite != null)
            {
                lpImg.sprite = pillSprite;
                lpImg.color = Color.white;
            }
            else
            {
                lpImg.color = new Color(0.08f, 0.11f, 0.22f, 1f);
            }
            lpImg.raycastTarget = true;

            // Clicking the level pill allows returning to the level map
            Button lpBtn = levelPill.AddComponent<Button>();
            lpBtn.targetGraphic = lpImg;
            lpBtn.onClick.AddListener(() =>
            {
                ShowZombieSquadLevelMap();
            });

            hudLevelText = CreateText("LevelText", levelPill.transform, "LEVEL 1", 28, Color.white, TextAlignmentOptions.Center, true);
            hudLevelText.fontStyle = FontStyles.Bold;
            hudLevelText.characterSpacing = 3f;
            hudLevelText.rectTransform.anchoredPosition = Vector2.zero;

            // 2. Central Level Completion Progress Bar Track
            // Thick black outline with dark navy interior
            GameObject barTrack = new GameObject("HordeProgressBarTrack");
            barTrack.transform.SetParent(headerBar.transform, false);
            RectTransform btRt = barTrack.AddComponent<RectTransform>();
            btRt.anchorMin = new Vector2(0f, 0.5f);
            btRt.anchorMax = new Vector2(0f, 0.5f);
            btRt.pivot = new Vector2(0f, 0.5f);
            btRt.sizeDelta = new Vector2(560f, 34f);
            btRt.anchoredPosition = new Vector2(201f, 0f);

            Image btImg = barTrack.AddComponent<Image>();
            Sprite trackSprite = LoadSpriteFromFile("Resources/UI/Ref_ProgressBar_Track.png");
            if (trackSprite != null)
            {
                btImg.sprite = trackSprite;
                btImg.color = Color.white;
            }
            else
            {
                btImg.color = new Color(0.07f, 0.09f, 0.15f, 1f);
            }
            btImg.raycastTarget = false;

            // Green Progress Fill (Glossy neon lime gradient fill)
            GameObject barFill = new GameObject("BarFill");
            barFill.transform.SetParent(barTrack.transform, false);
            hudProgressBarFillRt = barFill.AddComponent<RectTransform>();
            hudProgressBarFillRt.anchorMin = new Vector2(0f, 0f);
            hudProgressBarFillRt.anchorMax = new Vector2(1f, 1f);
            hudProgressBarFillRt.offsetMin = new Vector2(3f, 3f);
            hudProgressBarFillRt.offsetMax = new Vector2(-3f, -3f);

            hudProgressBarFill = barFill.AddComponent<Image>();
            Sprite fillSprite = LoadSpriteFromFile("Resources/UI/Ref_ProgressBar_Fill.png");
            if (fillSprite != null)
            {
                hudProgressBarFill.sprite = fillSprite;
                hudProgressBarFill.color = Color.white;
            }
            else
            {
                hudProgressBarFill.color = new Color(0.18f, 0.92f, 0.35f, 1f);
            }
            hudProgressBarFill.type = Image.Type.Filled;
            hudProgressBarFill.fillMethod = Image.FillMethod.Horizontal;
            hudProgressBarFill.fillOrigin = 0;
            hudProgressBarFill.fillAmount = 0f;
            hudProgressBarFill.raycastTarget = false;

            // Checkered Finish Flag Icon (3x3 grid slanted forward with thick black border)
            GameObject flagObj = new GameObject("FinishFlagIcon");
            flagObj.transform.SetParent(barTrack.transform, false);
            RectTransform flagRt = flagObj.AddComponent<RectTransform>();
            flagRt.anchorMin = new Vector2(1f, 0.5f);
            flagRt.anchorMax = new Vector2(1f, 0.5f);
            flagRt.pivot = new Vector2(1f, 0.5f);
            flagRt.sizeDelta = new Vector2(46f, 46f);
            flagRt.anchoredPosition = new Vector2(-30f, 0f);
            Image flagImg = flagObj.AddComponent<Image>();
            Sprite flagSprite = LoadSpriteFromFile("Resources/UI/Ref_CheckeredFlag.png") ?? LoadSpriteFromFile("Textures/tex_flag_finish.png");
            if (flagSprite != null)
            {
                flagImg.sprite = flagSprite;
                flagImg.color = Color.white;
            }
            else
            {
                flagImg.color = Color.white;
            }
            flagImg.raycastTarget = false;

            // Hidden label reference so existing code doesn't NullRef
            hudProgressBarLabel = CreateText("BarLabel", barTrack.transform, "", 16, Color.clear, TextAlignmentOptions.Center, false);
            hudProgressBarLabel.gameObject.SetActive(false);

            // 3. Glowing Boss Skull Badge (Fiery red halo bloom + gold ring + 3D devil skull)
            GameObject bossBadgeObj = new GameObject("BossSkullBadge");
            bossBadgeObj.transform.SetParent(headerBar.transform, false);
            hudBossBadgeRect = bossBadgeObj.AddComponent<RectTransform>();
            hudBossBadgeRect.anchorMin = new Vector2(0f, 0.5f);
            hudBossBadgeRect.anchorMax = new Vector2(0f, 0.5f);
            hudBossBadgeRect.pivot = new Vector2(0.5f, 0.5f);
            hudBossBadgeRect.sizeDelta = new Vector2(88f, 88f);
            hudBossBadgeRect.anchoredPosition = new Vector2(761f, 0f);

            // Radiant Outer Fiery Bloom Glow Halo
            GameObject glowHalo = new GameObject("GlowHalo");
            glowHalo.transform.SetParent(bossBadgeObj.transform, false);
            RectTransform haloRt = glowHalo.AddComponent<RectTransform>();
            haloRt.anchorMin = new Vector2(0.5f, 0.5f);
            haloRt.anchorMax = new Vector2(0.5f, 0.5f);
            haloRt.pivot = new Vector2(0.5f, 0.5f);
            haloRt.sizeDelta = new Vector2(144f, 144f);
            haloRt.anchoredPosition = Vector2.zero;
            Image haloImg = glowHalo.AddComponent<Image>();
            Sprite haloSprite = LoadSpriteFromFile("Resources/UI/Ref_BossSkull_Glow.png");
            if (haloSprite != null)
            {
                haloImg.sprite = haloSprite;
                haloImg.color = Color.white;
            }
            else
            {
                haloImg.color = new Color(0.95f, 0.20f, 0.05f, 0.65f);
            }
            haloImg.raycastTarget = false;

            // 3D Horned Devil Skull circular badge
            hudBossBadgeImage = bossBadgeObj.AddComponent<Image>();
            Sprite bossSprite = LoadSpriteFromFile("Resources/UI/Ref_BossSkull_HD.png") ?? Resources.Load<Sprite>("UI/BossSkull_HD");
            if (bossSprite != null)
            {
                hudBossBadgeImage.sprite = bossSprite;
                hudBossBadgeImage.color = Color.white;
            }
            else
            {
                hudBossBadgeImage.color = new Color(0.90f, 0.15f, 0.12f, 1f);
            }
            hudBossBadgeImage.raycastTarget = false;

            // Start subtle breathing pulse for the boss glow
            StartCoroutine(AnimateBossGlowRoutine(haloRt));

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
            hudGunUpgradeBadgeRt.sizeDelta = new Vector2(250f, 44f);
            hudGunUpgradeBadgeRt.anchoredPosition = new Vector2(0f, -160f);

            Image boostBg = boostTimerObj.AddComponent<Image>();
            boostBg.color = new Color(0.06f, 0.12f, 0.22f, 0.88f); // High-contrast translucent dark navy pill
            boostBg.raycastTarget = false;

            hudGunUpgradeText = CreateText("BoostTimerText", boostTimerObj.transform, "⚡ RAPID FIRE: 15s", 20, new Color(0.12f, 0.95f, 1.0f, 1f), TextAlignmentOptions.Center, true);
            hudGunUpgradeText.fontStyle = FontStyles.Bold;
            hudGunUpgradeText.rectTransform.anchoredPosition = Vector2.zero;

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

            // 2. Start endless road
            if (MobControl.Level.EndlessRoadManager.Instance != null)
            {
                MobControl.Level.EndlessRoadManager.Instance.SetRunning(true);
            }

            // 3. Start Zombie Spawner
            if (MobControl.Crowd.ZombieRoadWaveSpawner.Instance != null)
            {
                MobControl.Crowd.ZombieRoadWaveSpawner.Instance.StartSpawning();
            }
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

            if (hudProgressBarFill != null)
            {
                hudProgressBarFill.fillAmount = 1.0f;
                hudProgressBarFill.color = new Color(0.95f, 0.18f, 0.14f, 1f); // Fiery Red
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
            hudGunUpgradeBadgeRt.sizeDelta = new Vector2(190f, 64f);
            hudGunUpgradeBadgeRt.anchoredPosition = new Vector2(0f, -126f);

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

            hudGunUpgradeText = CreateText("BoostTimerText", pillObj.transform, "15s REMAINING", 14, new Color(1f, 0.95f, 0.35f, 1f), TextAlignmentOptions.Center, true);
            hudGunUpgradeText.fontStyle = FontStyles.Bold;
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
            vigImg.color = new Color(0.02f, 0.04f, 0.08f, 0.72f);
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

            // 3. Dynamic Subtitle: "PERFECT RUN! (3/3 HP)"
            victorySubText = CreateText("VictorySub", masterCard.transform, "PERFECT RUN! (3/3 HP)", 24, new Color(0.85f, 0.92f, 1f, 0.95f), TextAlignmentOptions.Center, true);
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
            vigImg.color = new Color(0.02f, 0.04f, 0.08f, 0.72f);
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

            Debug.Log($"[ZombieSquad] Level {levelNumber} ready! Waiting for user swipe to start.");
        }

        private void UpdateGameplayHud()
        {
            if (playerRunner == null) return;

            // In stationary defense mode, progress is driven by 30s wave timer & boss power!
            // Update Health Text
            if (hudHealthText != null)
            {
                int hp = Mathf.Max(0, playerRunner.Health);
                hudHealthText.text = $"HP: {hp} / {playerRunner.MaxHealth}";
            }
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

        public void TriggerVictory(int overrideStars, int overrideCoins)
        {
            if (isFinishedHandled) return;
            isFinishedHandled = true;
            isGameActive = false;
            currentState = ScreenState.Victory;

            Debug.Log($"[ZombieRunnerUIManager] Victory triggered with override: stars={overrideStars}, coins={overrideCoins}");

            LockPlayer(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            EnsureEventSystem();
            SwitchToPanel(victoryPanel);

            int starsEarned = Mathf.Clamp(overrideStars, 1, 3);
            int coinsEarned = overrideCoins;

            int currentCoins = PlayerPrefs.GetInt(PREF_COINS, 250);
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
                if (starsEarned == 3) victorySubText.text = "PERFECT RUN! (3/3 HP)";
                else if (starsEarned == 2) victorySubText.text = "GREAT COMBAT! (2/3 HP)";
                else victorySubText.text = "SURVIVED! (1/3 HP)";
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
