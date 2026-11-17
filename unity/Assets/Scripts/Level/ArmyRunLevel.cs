using System.Collections.Generic;
using UnityEngine;
using TMPro;
using MobControl.Combat;
using MobControl.UI;
using MobControl.Core;

namespace MobControl.Level
{
    /// <summary>
    /// ═══════════════════════════════════════════════════════════════════════════════════════
    ///  LEVEL 1 — "ARMY RUN"  (Top War style crowd runner).  Rules: ARMY_RUN_LEVEL1.md
    /// ═══════════════════════════════════════════════════════════════════════════════════════
    ///  This class is split into one file per system (spec names in brackets):
    ///    ArmyRunLevel.cs          [GameManager / LevelManager] hooks, setup, frame loop, win/lose
    ///    ArmyRunLevel.Crowd.cs    [CrowdController + CrowdFormation] army, formation, pooled soldiers, templates
    ///    ArmyRunLevel.Combat.cs   [CombatSystem] army fire, bullet hits, damage, deaths, rewards, explosions
    ///    ArmyRunLevel.Enemies.cs  [EnemyController] red soldiers, group counters, gunners, parade
    ///    ArmyRunLevel.Gates.cs    [GateController + ObstacleController] gates, rollers, crashes
    ///    ArmyRunLevel.Track.cs    [Level layout] the 6 stages of Level 1 + builders for every object
    ///    ArmyRunLevel.Boss.cs     [Boss] the Brute and the Master Tyrant
    ///    ArmyRunLevel.UI.cs       [UIManager additions] HUD pills, coins, banners, boss bar
    ///    ArmyRunLevel.Build.cs    materials, primitives, labels, floating text
    ///  Helpers: ArmyRunEnvironment (world), ArmyRunFX (particles), ArmyRunModels (models),
    ///  existing SmoothCameraFollow [CameraController], existing PlayerRunnerController [PlayerController].
    /// </summary>
    public partial class ArmyRunLevel : MonoBehaviour
    {
        // ─────────────────────────────────────────── Switch ──
        public static bool Enabled = true;
        public static ArmyRunLevel Instance { get; private set; }
        public static bool HandlesLevel(int level) => Enabled && level == 1;

        // ─────────────────────────────────────────── Tuning ──
        // Speed
        private const float RUN_SPEED = 5f;                // road scroll speed (m/s) — reference pace, plaques readable
        // Road (main bridge is x -3.6 … +3.6)
        private const float LEADER_X_LIMIT = 3.0f;         // commander steering limit (road is ±3.6)
        private const float START_X = 2.1f;                // the squad starts beside the +1 strip
        private const float ZONE_HALF_WIDTH = 3.2f;        // soldiers stay inside this
        // Camera (Mob Control framing: close, steep ~47°, army in the lower third, road fills the screen)
        // Mob Control "Cave Spike Roller" framing (portrait): camera high behind the squad looking 48° down.
        // The squad sits in the bottom quarter, the road runs up the middle and fades into mist ~35 m ahead,
        // the road + strip fill ~75 % of the screen width at the squad.
        private const float CAM_HEIGHT = 16f;
        private const float CAM_BACK = 7.8f;               // metres behind the commander
        private const float CAM_PITCH = 48f;
        private const float CAM_FOV = 55f;
        private const float CAM_CENTER_X = 0.35f;          // road (-3.6…3.6) + strip a little right of centre
        private const float CAM_FOLLOW_X = 0.15f;          // barely follows sideways (reference camera is fixed)
        // Characters
        private const float UNIT_SCALE = 0.9f;
        private const float UNIT_SPACING = 0.38f;          // compact squad like the reference
        private const int MAX_VISIBLE_UNITS = 36;          // drawn soldiers (each is a skinned + animated mesh); the number shows the real count
        // Army fire
        private const float UNIT_FIRE_RATE = 2.2f;         // shots / soldier / s
        private const float FIRE_UP_MULT = 1.35f;
        private const float MAX_VISUAL_BULLETS = 90f;    // thick yellow bullet stream
        private const float BULLET_SPEED = 46f;
        // Bullets stop 12.5 m ahead of the commander = 2/3 of the screen height (≈4" on a 6" phone).
        // The top third is the horde's: you see it coming before you can shoot it.
        private const float BULLET_RANGE = 12.5f;
        // Enemies
        private const float ENEMY_CHARGE_SPEED = 2.4f;
        private const float ENEMY_ACTIVATE_Z = 48f;
        private const float GUNNER_RANGE = 34f;
        private const float GUNNER_INTERVAL = 1.5f;
        private const float ENEMY_SHOT_SPEED = 28f;
        private const float EXPLOSION_RADIUS = 3.6f;
        private const float EXPLOSION_DAMAGE = 30f;

        // ─────────────────────────────────────────── Types ──
        private enum Kind { Barrel, Explosive, Cage, Ice, Enemy, Gunner, Brute, Boss }
        private enum Reward { None, Soldiers, FireRate }

        private class Target
        {
            public Kind kind;
            public Transform tr, visual;
            public float halfWidth, depth = 0.45f;
            public float hp, maxHp;
            public TextMeshPro label;
            public bool alive = true;
            public Reward reward;
            public int rewardValue;
            public Vector3 baseScale = Vector3.one;
            public float punch;
            public Gate unlocksGate;
            // charging enemies
            public bool spawned;
            public Vector3 spawnLocal;
            public bool heavy;
            public int contactKills = 1;
            public float speedMul = 1f;       // stream enemies walk slower
            public bool freesGiant;           // rescue cage
            public float allyLife = -1f;      // seconds the freed giant stays (-1 = until he falls)
            public TextMeshPro helpLabel;
            // gunners
            public float fireTimer;
            public Transform muzzle;
            // looks
            public Renderer[] rends;          // for hit flash / damage darkening
            public float flashT;
            public int damageStage;           // 0..3 — containers crack in 4 steps
            public Horde horde;
            public int coins;                 // coins dropped when destroyed
        }

        /// <summary>A red squad: its counter floats above it and counts down as members die.</summary>
        private class Horde
        {
            public readonly List<Target> members = new List<Target>();
            public TextMeshPro label;
            public bool cleared;
        }

        private class Gate
        {
            public Transform tr;
            public float halfWidth;
            public char op;
            public int value;
            public bool used;
            public Gate pair, pair2;
            public Renderer panel;
            public TextMeshPro label;
            public float baseX, moveAmp, moveSpeed;
            public Color color;
            public bool plaque;               // small "+1" plaque from a strip (no partner, quiet feedback)
        }

        private class Roller { public Transform tr, log; public float halfLen, speed, phase, center, amp; public bool resolved, warned; }
        private class Unit { public Transform tr; public Animator anim; public float pop; }
        private class Fallen { public Transform tr; public float t; }
        private struct Bullet { public Transform tr; public float damage; public float startZ; public float vx; }
        private class EnemyShot { public Transform tr; public Vector3 from; public Transform aim; public Vector3 aimPos; public float t, dur; }
        private class Dying { public Transform tr; public Vector3 vel, spin; public float t; }
        private class FloatText { public TextMeshPro tmp; public float life; }
        private class ParadeRow { public Transform tr; public float phase; }

        // ─────────────────────────────────────────── State ──
        private Transform root, trackRoot, poolRoot;
        private ArmyRunEnvironment env;
        private ArmyRunFX fx;
        private GameObject unitTemplate, enemyTemplate, bossTemplate;
        private GameObject bruteGenTemplate, allyGenTemplate;   // Meshy BruteYellow / AllyGiant (null until generated)
        private float soldierHeight = 1.3f;
        private bool enemyIsGenerated;

        private readonly List<Target> targets = new List<Target>();
        private readonly List<Gate> gates = new List<Gate>();
        private readonly List<Roller> rollers = new List<Roller>();
        private readonly List<Unit> units = new List<Unit>();
        private readonly List<Bullet> bullets = new List<Bullet>();
        private readonly List<EnemyShot> enemyShots = new List<EnemyShot>();
        private readonly List<Dying> dying = new List<Dying>();
        private readonly List<FloatText> floatTexts = new List<FloatText>();
        private readonly List<ParadeRow> parade = new List<ParadeRow>();
        private readonly List<Horde> hordes = new List<Horde>();
        private readonly List<Fallen> fallen = new List<Fallen>();
        private readonly Stack<Transform> bulletPool = new Stack<Transform>();
        private readonly Stack<Transform> shotPool = new Stack<Transform>();
        private readonly Stack<GameObject> unitPool = new Stack<GameObject>();
        private readonly Stack<GameObject> enemyPool = new Stack<GameObject>();

        private TextMeshPro countLabel;
        private float countLabelPunch;

        private bool setupDone, running, finished, arenaActive, cameraOverridden;
        private int soldierCount = 1;
        private float fireMultiplier = 1f;
        private float travelled, levelLength = 545f;
        private float scrollSpeedNow;
        private int coinsEarned;
        private Vector3 clashPos;            // where the army is fighting hand-to-hand right now
        private float clashTimer, attackAnimTimer;
        private SmoothCameraFollow camFollow;
        private float camX;
        private float visualShotTimer, gunSoundTimer, finishTimer = -1f;

        private PlayerRunnerController runner;
        private Camera cam;
        private float prevFov = 60f;
        private MobControl.Audio.ZombieSoundManager Snd => MobControl.Audio.ZombieSoundManager.Instance;

        private static Material s_enemyBodyMat;
        private static Material s_wood, s_band, s_redBarrel, s_hazard, s_ice, s_iceRib, s_frame, s_steel, s_sand, s_log, s_spike;
        private static Material s_tracer, s_enemyTracer, s_gateBlue, s_gateRed, s_rune, s_warn;
        private static readonly Dictionary<int, Material> s_flatMats = new Dictionary<int, Material>();
        private static TMP_FontAsset s_font;

        // ═══════════════════════════════════════════ Hooks (from ZombieRunnerUIManager) ══

        public static void OnMissionStarted(int level)
        {
            if (!HandlesLevel(level))
            {
                if (Instance != null) Instance.Teardown();
                RestorePlayerDefaults();
                return;
            }
            Ensure().SetupLevel();
        }

        public static void OnGameplayStarted(int level)
        {
            if (HandlesLevel(level) && Instance != null) Instance.BeginRun();
        }

        private static ArmyRunLevel Ensure()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("ArmyRunLevel_Director");
            Instance = go.AddComponent<ArmyRunLevel>();
            return Instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private static void RestorePlayerDefaults()
        {
            var r = PlayerRunnerController.Instance;
            if (r != null) { r.ExternalShootingControl = false; r.RestoreHorizontalLimit(); }
            if (Instance != null && Instance.cameraOverridden)
            {
                var c = Camera.main;
                var follow = c != null ? c.GetComponent<SmoothCameraFollow>() : null;
                if (follow != null) follow.SetCameraParameters(new Vector3(0f, 5.80f, -6.80f), new Vector3(29.0f, 0f, 0f));
                if (c != null) c.fieldOfView = Instance.prevFov;
                Instance.cameraOverridden = false;
            }
            Time.timeScale = 1f;
        }

        // ═══════════════════════════════════════════ Setup ══

        private void SetupLevel()
        {
            Teardown();
            PurgeStaleRoots();
            runner = PlayerRunnerController.Instance ?? FindAnyObjectByType<PlayerRunnerController>();
            cam = Camera.main;
            EnsureMaterials();

            root = new GameObject("ArmyRun_Root").transform;
            root.SetParent(transform, false);
            trackRoot = new GameObject("ArmyRun_Track").transform;
            trackRoot.SetParent(root, false);
            poolRoot = new GameObject("ArmyRun_Pool").transform;
            poolRoot.SetParent(root, false);
            poolRoot.gameObject.SetActive(false);

            env = new ArmyRunEnvironment();
            env.Build(root, cam);
            fx = new ArmyRunFX();
            fx.Build(root);

            BuildTemplates();

            soldierCount = 1 + PlayerRunnerController.UpgradeBonusLevels("Count");
            if (runner != null)
            {
                runner.ClearSquadAlliesForArmyMode();
                runner.ExternalShootingControl = true;
                runner.SetHorizontalLimit(LEADER_X_LIMIT);
                if (runner.transform.Find("BlobShadow") == null) ArmyRunFX.AddBlobShadow(runner.transform, 1.1f);
                SetPlayerBarsHidden(true);
            }

            if (cam != null)
            {
                var follow = cam.GetComponent<SmoothCameraFollow>();
                camFollow = follow;
                if (!cameraOverridden) prevFov = cam.fieldOfView;
                if (follow != null)
                {
                    follow.SetCameraParameters(new Vector3(CAM_CENTER_X, CAM_HEIGHT, LeaderPos.z - CAM_BACK), new Vector3(CAM_PITCH, 0f, 0f));
                    cameraOverridden = true;
                }
                cam.fieldOfView = CAM_FOV;
            }

            ResetSwarm();
            BuildTrack();
            BuildUI();
            HideZombieHealthPill();
            countLabel = CreateLabel(root, soldierCount.ToString(), LeaderPos + Vector3.up * 3f, 9f, Color.white, new Color(0.03f, 0.2f, 0.6f));
            SyncVisibleUnits(Vector3.zero, true);

            fireMultiplier = 1f;
            travelled = 0f;
            coinsEarned = 0;
            plaquesCollected = 0;
            plaqueCombo = 0;
            missedPlus = 0;
            nextStripHint = 0f;
            weaponLevel = 1;
            camX = CAM_CENTER_X;
            clashTimer = 0f;
            running = finished = arenaActive = false;
            finishTimer = -1f;
            stageIndex = -1;
            setupDone = true;
        }

        private void BuildTemplates()
        {
            if (unitTemplate == null)
            {
                unitTemplate = BuildPlayerCloneTemplate(false);
                ApplyArmyAnimator(unitTemplate);
                ArmyRunFX.AddBlobShadow(unitTemplate.transform, 1.0f);
            }
            if (runner != null)
            {
                Bounds b = ArmyRunModels.MeasureBounds(runner.gameObject);
                if (b.size.y > 0.3f && b.size.y < 5f) soldierHeight = b.size.y * UNIT_SCALE;
            }
            if (enemyTemplate == null)
            {
                enemyTemplate = ArmyRunModels.TryBuildGeneratedTemplate(ArmyRunModels.ENEMY_NAME, soldierHeight, transform);
                enemyIsGenerated = enemyTemplate != null;
                if (enemyTemplate == null) { enemyTemplate = BuildPlayerCloneTemplate(true); ApplyArmyAnimator(enemyTemplate); }
                ArmyRunFX.AddBlobShadow(enemyTemplate.transform, enemyIsGenerated ? 0.95f : 1.0f);
            }
            // Big characters: Meshy model if generated, otherwise our hand-built Mob Control style characters
            // Big characters: Meshy model if generated → free CC0 Quaternius model (baked) → hand-built fallback
            if (bruteGenTemplate == null)
                bruteGenTemplate = ArmyRunModels.TryBuildGeneratedTemplate(ArmyRunModels.BRUTE_NAME, soldierHeight * BRUTE_HEIGHT_X, transform)
                                   ?? ArmyRunBakedModels.BuildCharacterTemplate("Orc", soldierHeight * BRUTE_HEIGHT_X, transform, new Color(1.3f, 1.3f, 1.25f))
                                   ?? ArmyRunCharacters.BuildTemplate(ArmyRunCharacters.Style.Brute, soldierHeight * BRUTE_HEIGHT_X, transform);
            if (allyGenTemplate == null)
                allyGenTemplate = ArmyRunModels.TryBuildGeneratedTemplate(ArmyRunModels.ALLY_NAME, soldierHeight * ALLY_SCALE, transform)
                                  ?? ArmyRunBakedModels.BuildCharacterTemplate("BlueDemon", soldierHeight * ALLY_SCALE, transform, new Color(1.45f, 1.45f, 1.45f))
                                  ?? ArmyRunCharacters.BuildTemplate(ArmyRunCharacters.Style.Giant, soldierHeight * ALLY_SCALE, transform);
            if (bossTemplate == null)
            {
                bossTemplate = ArmyRunModels.TryBuildGeneratedTemplate(ArmyRunModels.BOSS_NAME, soldierHeight * MASTER_HEIGHT_X, transform)
                               ?? ArmyRunBakedModels.BuildCharacterTemplate("Orc", soldierHeight * MASTER_HEIGHT_X, transform, new Color(1.6f, 0.55f, 0.45f))   // red Orc warlord
                               ?? ArmyRunCharacters.BuildTemplate(ArmyRunCharacters.Style.Tyrant, soldierHeight * MASTER_HEIGHT_X, transform);
                if (bossTemplate == null)
                {
                    bossTemplate = new GameObject("BossFallback_Template");
                    bossTemplate.SetActive(false);
                    bossTemplate.transform.SetParent(transform, false);
                    var body = Instantiate(enemyTemplate, bossTemplate.transform);
                    body.name = "Body";
                    body.transform.localScale = Vector3.one * (enemyIsGenerated ? MASTER_HEIGHT_X : UNIT_SCALE * MASTER_HEIGHT_X);
                    body.SetActive(true);
                }
            }
        }

        /// <summary>
        /// An old "preview in scene" tool saved a whole Level 1 (sea bridge, ships, its own UI) into q.unity under
        /// the director. Anything named ArmyRun_Root that is not ours is stale: remove it so only the live level shows.
        /// </summary>
        private void PurgeStaleRoots()
        {
            foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include))
                if (t != null && t != root && t.name == "ArmyRun_Root") Destroy(t.gameObject);
        }

        private readonly List<GameObject> hiddenPlayerBars = new List<GameObject>();

        /// <summary>The commander's zombie-mode HP bar (the red line beside the squad) means nothing here.</summary>
        private void SetPlayerBarsHidden(bool hide)
        {
            if (hide)
            {
                if (runner == null) return;
                foreach (var hb in runner.GetComponentsInChildren<ThomasDev.HealthSystem.HealthBarUI>(true))
                    if (hb.gameObject.activeSelf) { hb.gameObject.SetActive(false); hiddenPlayerBars.Add(hb.gameObject); }
            }
            else
            {
                foreach (var go in hiddenPlayerBars) if (go != null) go.SetActive(true);
                hiddenPlayerBars.Clear();
            }
        }

        private void BeginRun()
        {
            if (!setupDone) SetupLevel();
            running = true;
            if (runner != null) runner.TargetX = START_X;   // glide onto the +1 strip: the first plaques teach the strip
            ResetEditorCapture();
            SetArmyMoving(true);
            if (EndlessRoadManager.Instance != null)
            {
                EndlessRoadManager.Instance.SetSpeed(RUN_SPEED);
                EndlessRoadManager.Instance.SetRunning(true);
            }
        }

        private void Teardown()
        {
            setupDone = running = false;
            Time.timeScale = 1f;
            if (env != null) { env.Dispose(); env = null; }
            if (fx != null) { fx.Dispose(); fx = null; }
            targets.Clear(); gates.Clear(); rollers.Clear(); units.Clear(); bullets.Clear(); enemyShots.Clear();
            dying.Clear(); floatTexts.Clear(); parade.Clear(); bigs.Clear(); hordes.Clear(); fallen.Clear();
            ResetSwarm();
            bulletPool.Clear(); shotPool.Clear(); unitPool.Clear(); enemyPool.Clear(); floatPool.Clear();
            countLabel = null;
            master = null;
            ally = null;
            if (root != null) Destroy(root.gameObject);
            root = trackRoot = poolRoot = null;
            ResetUI();
            RestoreZombieHealthPill();
            SetPlayerBarsHidden(false);
        }

        // ═══════════════════════════════════════════ FRAME LOOP ══
        //  scroll → stages → shoot → bullets → enemies → gunners → gates → rollers → crashes → bosses → visuals

        private void Update()
        {
            if (!setupDone || root == null) return;

            var ui = ZombieRunnerUIManager.Instance;
            bool inLevel = ui == null ||
                           ui.CurrentState == ZombieRunnerUIManager.ScreenState.Gameplay ||
                           ui.CurrentState == ZombieRunnerUIManager.ScreenState.Victory ||
                           ui.CurrentState == ZombieRunnerUIManager.ScreenState.Defeat;
            if (root.gameObject.activeSelf != inLevel) root.gameObject.SetActive(inLevel);
            if (!inLevel)
            {
                if (deathBig != null) { Time.timeScale = 1f; deathBig = null; }   // left mid-finale: never stay in slow motion
                return;
            }
            if (runner == null) runner = PlayerRunnerController.Instance;
            if (cam == null) cam = Camera.main;

            float dt = Time.deltaTime;
            bool paused = ui != null && ui.IsModalOpen && ui.CurrentState == ZombieRunnerUIManager.ScreenState.Gameplay;

            if (!paused && running && !finished)
            {
                if (!arenaActive)
                {
                    float speed = RUN_SPEED * swarmSpeedFactor;   // push only as fast as you kill                 // push only as fast as you kill
                    scrollSpeedNow = speed;
                    float dz = speed * dt;
                    trackRoot.position += Vector3.back * dz;
                    env?.Scroll(dz);
                    travelled += dz;
                    if (ui != null) ui.UpdateWaveProgress(Mathf.Clamp01(travelled / levelLength));
                }

                else scrollSpeedNow = 0f;

                UpdateStages();
                UpdateSwarm(dt);
                UpdateShooting(dt);
                UpdateBullets(dt);
                UpdateAlly(dt);
                UpdateEnemies(dt);
                UpdateHordes();
                UpdateGunners(dt);
                UpdateEnemyShots(dt);
                UpdateGates();
                UpdateRollers(dt);
                UpdateObstacleContacts();
                UpdateBigEnemies(dt);
            }

            if (!paused)
            {
                env?.Tick(cam);
                UpdateParade(dt);
                UpdateFormation(dt);
                UpdateClashAnimations(dt);
                UpdateHelpBubbles();
                UpdateFallen(dt);
                UpdateCamera(dt);
                UpdateDying(dt);
                UpdateFloatTexts(dt);
                UpdatePunches(dt);
                UpdateBossDeath();
                UpdateUI();
                if (finishTimer > 0f)
                {
                    finishTimer -= Time.unscaledDeltaTime;
                    if (finishTimer <= 0f) DeclareVictory();
                }
            }
            DrawSwarm();
            EditorCaptureTick();
        }

        private void LateUpdate()
        {
            if (!setupDone || cam == null || root == null || !root.gameObject.activeInHierarchy) return;
            SyncLabelAtlas();
            Quaternion face = cam.transform.rotation;
            for (int i = 0; i < targets.Count; i++) if (targets[i].label != null) targets[i].label.transform.rotation = face;
            for (int i = 0; i < gates.Count; i++) if (gates[i].label != null && !gates[i].plaque) gates[i].label.transform.rotation = face;   // plaque text is printed on its sign
            for (int i = 0; i < floatTexts.Count; i++) if (floatTexts[i].tmp != null) floatTexts[i].tmp.transform.rotation = face;
            for (int i = 0; i < hordes.Count; i++) if (hordes[i].label != null) hordes[i].label.transform.rotation = face;
            UpdateBigBars(face);

            if (countLabel != null)
            {
                Vector3 lp = LeaderPos;
                countLabel.transform.position = new Vector3(lp.x, soldierHeight + 1.6f + CrowdRadius * 0.2f, lp.z + 0.2f);
                countLabel.transform.rotation = face;
                countLabel.transform.localScale = Vector3.one * (1f + countLabelPunch * 0.5f);
            }
        }

        private Vector3 LeaderPos => runner != null ? runner.transform.position : Vector3.zero;
        private float CrowdRadius => UNIT_SPACING * Mathf.Sqrt(units.Count + 1) + 0.35f;

        // ═══════════════════════════════════════════ WIN / LOSE ══

        private void Lose()
        {
            if (finished) return;
            finished = true;
            running = false;
            Time.timeScale = 1f;
            ClearBullets();
            HideBossBar();
            if (EndlessRoadManager.Instance != null) EndlessRoadManager.Instance.SetRunning(false);
            if (runner != null) runner.Die("ARMY DEFEATED!", "The Dark Legion overran your soldiers.");
        }

        private void DeclareVictory()
        {
            running = false;
            Time.timeScale = 1f;
            HideBossBar();
            int stars = soldierCount >= 60 ? 3 : (soldierCount >= 20 ? 2 : 1);
            int coins = 80 + Mathf.Min(soldierCount, 300) + coinsEarned;
            var ui = ZombieRunnerUIManager.Instance;
            if (ui != null)
            {
                ui.TriggerVictory(stars, coins);
                ui.SetVictorySubtitle($"FINAL ARMY: {soldierCount} SOLDIERS");
            }
        }

        private void Shake(float amount)
        {
            if (cam != null) cam.GetComponent<SmoothCameraFollow>()?.TriggerShake(amount);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Time.timeScale = 1f;
        }
    }
}
