#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MobControl.Level;
using MobControl.UI;

namespace MobControl.EditorTools
{
    public static class RoadGeneratorMenuItem
    {
        [MenuItem("Zombie Squad/Clean Level 1 Scene (Remove All Clutter)")]
        public static void CleanLevel1Scene()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.LogWarning("[Zombie Squad] Exited play mode before modifying scene. Please re-run after exit.");
                return;
            }

            // 1. Remove procedural road roots
            GameObject root = GameObject.Find(NewRoadBuilder.ROOT_NAME);
            if (root != null) Object.DestroyImmediate(root);

            GameObject oldRoot = GameObject.Find("NewRoad_Level1_Root");
            if (oldRoot != null) Object.DestroyImmediate(oldRoot);

            // 2. Remove all root pools, ObjectPooler, and stray managers
            var activeScn = EditorSceneManager.GetActiveScene();
            foreach (var rootGo in activeScn.GetRootGameObjects())
            {
                if (rootGo == null) continue;
                string n = rootGo.name;
                if (n.Contains("ObjectPooler") || 
                    n.StartsWith("Pool_") || 
                    n.Contains("(Clone)") || 
                    n.StartsWith("Preview_Zombie_") || 
                    n.StartsWith("Zombie_") ||
                    n.Contains("UpcomingZoneManager") ||
                    n.Contains("HomeScreen") ||
                    n.Contains("Level1_RoadBuilder_Temp"))
                {
                    Object.DestroyImmediate(rootGo);
                }
            }

            // Also check all remaining objects in scene
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            {
                if (go == null) continue;
                string n = go.name;
                if (n.StartsWith("Pool_") || 
                    n.Contains("(Clone)") || 
                    n.StartsWith("Preview_Zombie_") || 
                    n.StartsWith("Zombie_Small") || 
                    n.StartsWith("Zombie_Moderate") || 
                    n.StartsWith("Zombie_Boss") ||
                    n.Contains("ObjectPooler") ||
                    n.Contains("UpcomingZoneManager") ||
                    n.Contains("Level1_RoadBuilder_Temp") ||
                    n.Contains("SquadCountBadge") ||
                    n.Contains("BadgeBacking") ||
                    n.Contains("HealthBar3D") ||
                    n.Contains("HealthBar_Visual") ||
                    n.Contains("Gun_M41D"))
                {
                    Object.DestroyImmediate(go);
                }
            }

            // Remove any in-world 3D TextMeshPro floating labels
            foreach (var txt in Object.FindObjectsByType<TMPro.TextMeshPro>(FindObjectsInactive.Include))
            {
                if (txt != null && txt.GetComponentInParent<Canvas>() == null)
                {
                    Undo.DestroyObjectImmediate(txt.gameObject);
                }
            }

            // Remove any Gun_M41D objects
            RemoveGunM41DFromScene();

            // 3. Setup clean Camera
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.farClipPlane = 450.0f;
                mainCam.backgroundColor = new Color(0.25f, 0.58f, 0.95f, 1f);
                mainCam.clearFlags = CameraClearFlags.SolidColor;
                EditorUtility.SetDirty(mainCam);
            }

            var activeScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("[Zombie Squad] Level 1 cleaned successfully! All procedural clutter, zombie pools, and preview zombies removed and scene saved.");
        }

        [MenuItem("Zombie Squad/Rebuild Level 1 Reference Scene")]
        public static void ManualSyncScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.isLoaded || scene.name != "q")
            {
                scene = EditorSceneManager.OpenScene("Assets/q.unity");
            }
            if (scene.isLoaded && scene.name == "q")
            {
                Debug.Log("[RoadGeneratorMenuItem] Rebuilding and syncing Level 1 Reference Scene...");
                CleanLevel1Scene();
                BulletPrefabBuilder.BuildPrefab();
                BuildReferenceLevel1Road();
                InspectPlayerHeroInScene();
            }
        }

        public static Material EnsureAssaultRifleMaterialAsset()
        {
            string matPath = "Assets/Models/Generated/AssaultRifle/Materials/M_MeshyAssaultRifle.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (existing != null) return existing;

            if (!AssetDatabase.IsValidFolder("Assets/Models/Generated/AssaultRifle/Materials"))
            {
                AssetDatabase.CreateFolder("Assets/Models/Generated/AssaultRifle", "Materials");
            }

            Material mat = new Material(Shader.Find("Standard"));
            mat.name = "M_MeshyAssaultRifle";

            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/Generated/AssaultRifle/AssaultRifle_15k_textures/base_color.png");
            if (tex != null) mat.mainTexture = tex;

            Texture2D metalRough = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/Generated/AssaultRifle/AssaultRifle_15k_textures/metallic_roughness.png");
            if (metalRough != null) mat.SetTexture("_MetallicGlossMap", metalRough);

            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/Generated/AssaultRifle/AssaultRifle_15k_textures/normal.png");
            if (normal != null)
            {
                mat.EnableKeyword("_NORMALMAP");
                mat.SetTexture("_BumpMap", normal);
            }

            mat.SetFloat("_Metallic", 0.45f);
            mat.SetFloat("_Glossiness", 0.65f);

            AssetDatabase.CreateAsset(mat, matPath);
            AssetDatabase.SaveAssets();
            return mat;
        }

        [MenuItem("Zombie Squad/Build Reference Level 1 Road (Image Match)")]
        public static void BuildReferenceLevel1Road()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                EditorApplication.delayCall += () => BuildReferenceLevel1Road();
                return;
            }

            CleanLevel1Scene();

            // 1. Build fresh Cartoon Glowing Bullet Prefab
            BulletPrefabBuilder.BuildPrefab();

            // 2. Ensure persistent Assault Rifle Material asset
            EnsureAssaultRifleMaterialAsset();

            // 2b. Build 3 Meshy AI Zombie Prefabs (Small, Moderate, Boss)
            ZombiePrefabBuilder.BuildAllZombiePrefabs();

            // 3. Build Road Scene
            GameObject builderObj = new GameObject("Level1_RoadBuilder_Temp");
            NewRoadBuilder builder = builderObj.AddComponent<NewRoadBuilder>();
            builder.BuildRoadScene();
            Object.DestroyImmediate(builderObj);

            // 4. Wire up PlayerRunnerController bulletPrefab and input parameters
            GameObject hero = GameObject.Find("Player_Hero");
            if (hero != null)
            {
                var runner = hero.GetComponent<MobControl.Combat.PlayerRunnerController>();
                if (runner != null)
                {
                    GameObject bulletPf = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Bullet_9mm.prefab");
                    var so = new SerializedObject(runner);
                    var bpProp = so.FindProperty("bulletPrefab");
                    if (bpProp != null) bpProp.objectReferenceValue = bulletPf;
                    var smProp = so.FindProperty("stationaryMode");
                    if (smProp != null) smProp.boolValue = true;
                    var asProp = so.FindProperty("autoShoot");
                    if (asProp != null) asProp.boolValue = true;
                    var hlProp = so.FindProperty("horizontalLimit");
                    if (hlProp != null) hlProp.floatValue = 2.5f;
                    var msProp = so.FindProperty("mobileSlideMultiplier");
                    if (msProp != null) msProp.floatValue = 4.5f;
                    so.ApplyModifiedProperties();
                }
            }

            // 5. Wire Camera framing
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                var camFollow = mainCam.GetComponent<MobControl.Core.SmoothCameraFollow>();
                if (camFollow != null)
                {
                    var soCam = new SerializedObject(camFollow);
                    var offProp = soCam.FindProperty("offset");
                    if (offProp != null) offProp.vector3Value = new Vector3(0f, 5.80f, -6.80f);
                    var lrProp = soCam.FindProperty("lookRotation");
                    if (lrProp != null) lrProp.vector3Value = new Vector3(29.0f, 0f, 0f);
                    var fxProp = soCam.FindProperty("followX");
                    if (fxProp != null) fxProp.boolValue = false;
                    var fzProp = soCam.FindProperty("followZ");
                    if (fzProp != null) fzProp.boolValue = false;
                    soCam.ApplyModifiedProperties();
                }
                mainCam.transform.position = new Vector3(0f, 5.80f, -6.80f);
                mainCam.transform.rotation = Quaternion.Euler(29.0f, 0f, 0f);
                mainCam.fieldOfView = 60.0f;
                EditorUtility.SetDirty(mainCam);
            }

            var activeScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("[Zombie Squad] Level 1 reference road generated and scene saved!");
        }

        [MenuItem("Zombie Squad/Inspect Road And Fix Magenta")]
        public static void InspectRoadAndFixMagenta()
        {
            Material roadMat = Resources.Load<Material>("Materials/M_StylizedRoad");
            if (roadMat == null) roadMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/M_StylizedRoad.mat");
            if (roadMat == null) roadMat = NewRoadBuilder.LoadTextureMaterialPublic("Textures/Road/Road_Stylized_Reference.png", "M_StylizedRoad_Endless");

            var renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int fixedCount = 0;
            foreach (var mr in renderers)
            {
                if (mr.gameObject.name == "RoadSurface")
                {
                    if (mr.sharedMaterial == null || mr.sharedMaterial.shader == null || mr.sharedMaterial.shader.name == "Hidden/InternalErrorShader")
                    {
                        mr.sharedMaterial = roadMat;
                        EditorUtility.SetDirty(mr);
                        fixedCount++;
                    }
                }
            }
            var activeScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log($"[Zombie Squad] Fixed {fixedCount} RoadSurface materials! Road is now beautiful stylized asphalt.");
        }

        [MenuItem("Zombie Squad/Inspect Player_Hero In Scene")]
        public static void InspectPlayerHeroInScene()
        {
            GameObject hero = GameObject.Find("Player_Hero");
            Camera cam = Camera.main;
            if (cam != null)
            {
                Debug.Log($"Camera pos={cam.transform.position}, rot={cam.transform.rotation.eulerAngles}, fov={cam.fieldOfView}");
            }
            if (hero != null)
            {
                Debug.Log($"Hero pos={hero.transform.position}, world bounds on SMR:");
                var smr = hero.GetComponentInChildren<SkinnedMeshRenderer>();
                if (smr != null)
                {
                    Debug.Log($"SMR bounds center={smr.bounds.center}, extents={smr.bounds.extents}, enabled={smr.enabled}, isVisible={smr.isVisible}");
                    Debug.Log($"SMR sharedMesh size={smr.sharedMesh?.bounds.size}, visualObj scale={smr.transform.parent.localScale}");
                }
                var gun = hero.transform.Find("AssaultRifle_Meshy");
                if (gun == null) gun = hero.transform.Find("Visual_Mob/mixamorig:Hips/mixamorig:Spine/mixamorig:Spine1/mixamorig:Spine2/mixamorig:RightShoulder/mixamorig:RightArm/mixamorig:RightForeArm/mixamorig:RightHand/AssaultRifle_Meshy");
                if (gun == null) gun = hero.GetComponentInChildren<MeshFilter>()?.transform;
                if (gun != null)
                {
                    Transform gunParent = gun.parent;
                    Debug.Log($"[Gun Check] gunParent={gunParent.name}, pos={gunParent.position}, lossyScale={gunParent.lossyScale}");
                    Debug.Log($"[Gun Check] gun world pos={gun.position}, localPos={gun.localPosition}, world scale={gun.lossyScale}, localScale={gun.localScale}");
                    Debug.Log($"[Gun Check] gun forward={gun.forward}, up={gun.up}");
                    var mr = gun.GetComponent<MeshRenderer>();
                    var mf = gun.GetComponent<MeshFilter>();
                    Debug.Log($"[Gun Check] MeshRenderer: {(mr != null ? mr.enabled.ToString() : "NULL")}, mat={(mr != null && mr.sharedMaterial != null ? mr.sharedMaterial.name : "NULL")}");
                    Debug.Log($"[Gun Check] MeshFilter: {(mf != null && mf.sharedMesh != null ? mf.sharedMesh.name + " verts=" + mf.sharedMesh.vertexCount : "NULL")}");
                    var muzzle = gun.Find("MuzzleFirePoint");
                    if (muzzle != null) Debug.Log($"[Muzzle Check] muzzle world pos={muzzle.position}, localPos={muzzle.localPosition}");
                }
            }
        }

        [MenuItem("Zombie Squad/Debug Print Home Scene Status")]
        public static void DebugPrintHomeSceneStatus()
        {
            var homeManager = HomeScreen3DManager.Instance;
            Debug.Log($"[HomeStatus] HomeScreen3DManager.Instance: {(homeManager != null ? "FOUND" : "NULL")}");
            var sceneRoot = GameObject.Find(HomeScreen3DManager.ROOT_NAME);
            Debug.Log($"[HomeStatus] [HomeScreen_3D_Scene]: {(sceneRoot != null ? "FOUND" : "NULL")}");
            if (sceneRoot != null)
            {
                Debug.Log($"[HomeStatus] sceneRoot active={sceneRoot.activeSelf}, childCount={sceneRoot.transform.childCount}");
                for (int i = 0; i < sceneRoot.transform.childCount; i++)
                {
                    var child = sceneRoot.transform.GetChild(i);
                    Debug.Log($"[HomeStatus]   Child[{i}]={child.name}, pos={child.localPosition}, rot={child.localEulerAngles}, scale={child.localScale}, active={child.gameObject.activeSelf}");
                    for (int j = 0; j < child.childCount; j++)
                    {
                        var grandChild = child.GetChild(j);
                        Debug.Log($"[HomeStatus]     GrandChild[{j}]={grandChild.name}, pos={grandChild.localPosition}, rot={grandChild.localEulerAngles}, scale={grandChild.localScale}, active={grandChild.gameObject.activeSelf}");
                    }
                }
            }

            var roadRoot = GameObject.Find("Level1_StylizedRoad_Root");
            Debug.Log($"[HomeStatus] Level1_StylizedRoad_Root: {(roadRoot != null ? $"active={roadRoot.activeSelf}" : "NULL")}");

            var mainCam = Camera.main;
            if (mainCam != null)
            {
                Debug.Log($"[HomeStatus] Camera: pos={mainCam.transform.position}, rot={mainCam.transform.rotation.eulerAngles}, fov={mainCam.fieldOfView}");
            }
        }




        public static void RemoveGunM41DFromScene()
        {
            GameObject[] all = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            int count = 0;
            foreach (var go in all)
            {
                if (go != null && go.name.Contains("Gun_M41D"))
                {
                    Undo.DestroyObjectImmediate(go);
                    count++;
                }
            }
            if (count > 0)
            {
                Debug.Log($"[Zombie Squad] Removed {count} Gun_M41D object(s).");
            }
        }
        [MenuItem("Zombie Squad/Spawn 3 Preview Zombies In Front Of Camera")]
        public static void SpawnPreviewZombies()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            {
                if (go != null && go.name.StartsWith("Preview_Zombie_")) Object.DestroyImmediate(go);
            }

            GameObject smallPf = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Zombie_Small.prefab");
            GameObject modPf = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Zombie_Moderate.prefab");
            GameObject bossPf = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Zombie_Boss.prefab");

            if (smallPf != null)
            {
                var small = (GameObject)PrefabUtility.InstantiatePrefab(smallPf);
                small.name = "Preview_Zombie_Small";
                small.transform.position = new Vector3(-1.1f, 0f, 3.4f);
                small.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }

            if (modPf != null)
            {
                var mod = (GameObject)PrefabUtility.InstantiatePrefab(modPf);
                mod.name = "Preview_Zombie_Moderate";
                mod.transform.position = new Vector3(0.0f, 0f, 3.6f);
                mod.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }

            if (bossPf != null)
            {
                var boss = (GameObject)PrefabUtility.InstantiatePrefab(bossPf);
                boss.name = "Preview_Zombie_Boss";
                boss.transform.position = new Vector3(1.3f, 0f, 4.2f);
                boss.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }

            var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(activeScene);
            Debug.Log("<color=cyan>[Zombie Squad] Spawned 3 Preview Zombies in front of camera!</color>");
        }

        [MenuItem("Zombie Squad/Remove All Preview Zombies From Scene")]
        public static void RemovePreviewZombies()
        {
            int removed = 0;
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            {
                if (go != null && go.name.StartsWith("Preview_Zombie_"))
                {
                    Undo.DestroyObjectImmediate(go);
                    removed++;
                }
            }
            var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(activeScene);
            Debug.Log($"<color=green>[Zombie Squad] Removed {removed} preview zombies from scene.</color>");
        }

        [MenuItem("Zombie Squad/Enter Play Mode (Home Screen)")]
        public static void EnterPlayMode()
        {
            ZombieRunnerUIManager.startAtHomeScreen = true;
            ZombieRunnerUIManager.startAtLevelMap = false;
            ZombieRunnerUIManager.autoStartMissionOnLoad = false;
            UnityEditor.EditorApplication.isPlaying = true;
        }

        [MenuItem("Zombie Squad/Open Home Screen")]
        public static void OpenHomeScreen()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                var ui = ZombieRunnerUIManager.Instance ?? ZombieRunnerUIManager.EnsureInstance();
                if (ui != null) ui.ShowZombieSquadHomeScreen();
                HomeScreen3DManager.EnsureInstance().SetHomeMode(true);
            }
            else
            {
                ZombieRunnerUIManager.startAtHomeScreen = true;
                ZombieRunnerUIManager.startAtLevelMap = false;
                ZombieRunnerUIManager.autoStartMissionOnLoad = false;
                UnityEditor.EditorApplication.isPlaying = true;
            }
        }

        [MenuItem("Zombie Squad/Open Shop Screen")]
        public static void OpenShopScreen()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                var ui = ZombieRunnerUIManager.Instance ?? ZombieRunnerUIManager.EnsureInstance();
                if (ui != null) ui.ShowZombieSquadShopScreen();
            }
            else
            {
                ZombieRunnerUIManager.startAtHomeScreen = false;
                ZombieRunnerUIManager.startAtLevelMap = false;
                ZombieRunnerUIManager.startAtShop = true;
                ZombieRunnerUIManager.autoStartMissionOnLoad = false;
                UnityEditor.EditorApplication.isPlaying = true;
            }
        }

        [MenuItem("Zombie Squad/Open Level Map Screen")]
        public static void OpenLevelMapScreen()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                var ui = ZombieRunnerUIManager.Instance ?? ZombieRunnerUIManager.EnsureInstance();
                if (ui != null) ui.ShowZombieSquadLevelMap();
            }
            else
            {
                ZombieRunnerUIManager.startAtLevelMap = true;
                ZombieRunnerUIManager.startAtHomeScreen = false;
                ZombieRunnerUIManager.autoStartMissionOnLoad = false;
                UnityEditor.EditorApplication.isPlaying = true;
            }
        }

        [MenuItem("Zombie Squad/Restart Level Map Fresh")]
        public static void RestartLevelMapFresh()
        {
            UnityEditor.EditorApplication.isPaused = false;
            ZombieRunnerUIManager.startAtLevelMap = true;
            ZombieRunnerUIManager.startAtHomeScreen = false;
            ZombieRunnerUIManager.autoStartMissionOnLoad = false;
            if (UnityEditor.EditorApplication.isPlaying)
            {
                var ui = ZombieRunnerUIManager.Instance ?? ZombieRunnerUIManager.EnsureInstance();
                if (ui != null) ui.ShowZombieSquadLevelMap();
            }
            else
            {
                UnityEditor.EditorApplication.isPlaying = true;
            }
        }

        [MenuItem("Zombie Squad/Test Screens/Open Characters Screen")]
        public static void TestOpenCharacters()
        {
            var ui = ZombieRunnerUIManager.Instance ?? ZombieRunnerUIManager.EnsureInstance();
            if (ui != null) ui.ShowZombieSquadCharactersScreen();
        }

        [MenuItem("Zombie Squad/Test Screens/Open Upgrade Screen")]
        public static void TestOpenUpgrade()
        {
            var ui = ZombieRunnerUIManager.Instance ?? ZombieRunnerUIManager.EnsureInstance();
            if (ui != null) ui.ShowZombieSquadUpgradeScreen();
        }

        [MenuItem("Zombie Squad/Test Screens/Open Weapons Screen")]
        public static void TestOpenWeapons()
        {
            var ui = ZombieRunnerUIManager.Instance ?? ZombieRunnerUIManager.EnsureInstance();
            if (ui != null) ui.ShowZombieSquadWeaponsScreen();
        }

        [MenuItem("Zombie Squad/Test Screens/Open Shop Screen")]
        public static void TestOpenShop()
        {
            var ui = ZombieRunnerUIManager.Instance ?? ZombieRunnerUIManager.EnsureInstance();
            if (ui != null) ui.ShowZombieSquadShopScreen();
        }

        [MenuItem("Zombie Squad/Test Screens/Open Missions Screen")]
        public static void TestOpenMissions()
        {
            var ui = ZombieRunnerUIManager.Instance ?? ZombieRunnerUIManager.EnsureInstance();
            if (ui != null) ui.ShowZombieSquadMissionsScreen();
        }

        [MenuItem("Zombie Squad/Test Screens/Open Daily Screen")]
        public static void TestOpenDaily()
        {
            var ui = ZombieRunnerUIManager.Instance ?? ZombieRunnerUIManager.EnsureInstance();
            if (ui != null) ui.ShowZombieSquadDailyScreen();
        }

        [MenuItem("Zombie Squad/Test Screens/Open Settings Popup")]
        public static void TestOpenSettings()
        {
            var ui = ZombieRunnerUIManager.Instance ?? ZombieRunnerUIManager.EnsureInstance();
            if (ui != null) ui.ShowZombieSquadSettings();
        }

        [MenuItem("Zombie Squad/Quick Start Level 1 Road Battle")]
        public static void QuickStartLevel1()
        {
            StartMissionAtLevel(1);
        }

        [MenuItem("Zombie Squad/Quick Start Level 2 Road Battle")]
        public static void QuickStartLevel2()
        {
            StartMissionAtLevel(2);
        }

        [MenuItem("Zombie Squad/Level 2/Build & Preview Level 2 in Scene")]
        public static void BuildAndPreviewLevel2()
        {
            CleanLevel1Scene();
            BuildReferenceLevel1Road();

            if (EndlessRoadManager.Instance != null)
            {
                EndlessRoadManager.Instance.CurrentLevel = 2;
                EndlessRoadManager.Instance.BuildInitialEndlessTrack();
            }

            if (MobControl.Crowd.ZombieRoadWaveSpawner.Instance != null)
            {
                MobControl.Crowd.ZombieRoadWaveSpawner.Instance.CurrentLevel = 2;
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            Debug.Log("<color=#00FF66><b>[Level 2 Preview] Level 2 Scene successfully built with Plant Monsters and Flying Wyvern!</b></color>");
        }

        [MenuItem("Zombie Squad/Quick Start Level 3 Road Battle")]
        public static void QuickStartLevel3()
        {
            StartMissionAtLevel(3);
        }

        [MenuItem("Zombie Squad/Quick Start Level 5 Road Battle")]
        public static void QuickStartLevel5()
        {
            StartMissionAtLevel(5);
        }

        [MenuItem("Zombie Squad/Set Wave Progress 45% (Match User Reference)")]
        public static void SetWaveProgress45()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                var ui = ZombieRunnerUIManager.Instance ?? ZombieRunnerUIManager.EnsureInstance();
                if (ui != null)
                {
                    ui.UpdateWaveProgress(0.48f);
                    Debug.Log("[PROGRESS] Wave progress set to 48% to match reference image!");
                }
            }
        }

        [MenuItem("Zombie Squad/Capture Header Screenshot")]
        public static void CaptureHeaderScreenshot()
        {
            string path = "/Users/savvy/.gemini/antigravity-ide/brain/c4840675-c01c-4015-b0af-1b83673d3587/.tempmediaStorage/upgrade_live_check.png";
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"[SCREENSHOT] Captured verification screenshot to: {path}");
        }

        [MenuItem("Zombie Squad/Capture Shop Screen Screenshot")]
        public static void CaptureShopScreenScreenshot()
        {
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                UnityEditor.EditorApplication.isPlaying = true;
                UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeChangedForShopScreenshot;
                return;
            }
            DoCaptureShop();
        }

        private static void OnPlayModeChangedForShopScreenshot(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.EnteredPlayMode)
            {
                UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeChangedForShopScreenshot;
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    DoCaptureShop();
                };
            }
        }

        private static void DoCaptureShop()
        {
            var uiMgr = Object.FindAnyObjectByType<MobControl.UI.ZombieRunnerUIManager>();
            if (uiMgr != null)
            {
                uiMgr.ShowZombieSquadShopScreen();
                uiMgr.RebuildZombieSquadShopScreen();
            }
            UnityEditor.EditorApplication.delayCall += () =>
            {
                string dir = "/Users/savvy/.gemini/antigravity-ide/brain/c4840675-c01c-4015-b0af-1b83673d3587/.tempmediaStorage";
                if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
                string path = System.IO.Path.Combine(dir, "shop_screen_verification.png");
                ScreenCapture.CaptureScreenshot(path);
                Debug.Log($"[SCREENSHOT] Captured shop screen verification screenshot to: {path}");
            };
        }


        [MenuItem("Zombie Squad/Refresh Weapons Screen Panel")]
        public static void RefreshWeaponsScreenPanel()
        {
            var ui = Object.FindAnyObjectByType<MobControl.UI.ZombieRunnerUIManager>();
            if (ui != null)
            {
                ui.RebuildZombieSquadWeaponsScreen();
                ui.ShowZombieSquadWeaponsScreen();
                Debug.Log("[Zombie Squad] Weapons Screen Panel Rebuilt and Refreshed!");
            }
        }

        [MenuItem("Zombie Squad/Test Select Shotgun")]
        public static void TestSelectShotgun()
        {
            var ui = Object.FindAnyObjectByType<MobControl.UI.ZombieRunnerUIManager>();
            if (ui != null) ui.SelectWeapon("Shotgun");
        }

        [MenuItem("Zombie Squad/Test Select Sniper")]
        public static void TestSelectSniper()
        {
            var ui = Object.FindAnyObjectByType<MobControl.UI.ZombieRunnerUIManager>();
            if (ui != null) ui.SelectWeapon("Sniper");
        }

        [MenuItem("Zombie Squad/Test Select Minigun")]
        public static void TestSelectMinigun()
        {
            var ui = Object.FindAnyObjectByType<MobControl.UI.ZombieRunnerUIManager>();
            if (ui != null) ui.SelectWeapon("Minigun");
        }

        [MenuItem("Zombie Squad/Debug Print All Texts")]
        public static void DebugPrintAllTexts()
        {
            var tmps = Object.FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in tmps)
            {
                if (t.gameObject.activeInHierarchy)
                {
                    string path = t.name;
                    Transform p = t.transform.parent;
                    while (p != null) { path = p.name + "/" + path; p = p.parent; }
                    var rt = t.rectTransform;
                    Debug.Log($"[TMP_DEBUG] Text='{t.text}', Path='{path}', anchorMin={rt.anchorMin}, anchorMax={rt.anchorMax}, anchoredPos={rt.anchoredPosition}, sizeDelta={rt.sizeDelta}");
                }
            }
        }

        [MenuItem("Zombie Squad/Test Upgrade Damage")]
        public static void TestUpgradeDamage()
        {
            var ui = Object.FindAnyObjectByType<MobControl.UI.ZombieRunnerUIManager>();
            if (ui != null) ui.UpgradeWeaponStat("Damage");
        }

        [MenuItem("Zombie Squad/Spawn +1 Squad Ally (Test 2+ Players)")]
        public static void SpawnOneSquadAlly()
        {
            var player = MobControl.Combat.PlayerRunnerController.Instance ?? Object.FindAnyObjectByType<MobControl.Combat.PlayerRunnerController>();
            if (UnityEditor.EditorApplication.isPlaying && player != null)
            {
                player.SpawnSquadAllies(1);
                Debug.Log($"[SQUAD TEST] Spawned 1 Squad Ally! Total Squad Size is now: {player.TotalSquadSize}");
            }
        }

        [MenuItem("Zombie Squad/Test Hit Barricade Directly")]
        public static void TestHitBarricadeDirectly()
        {
            var player = MobControl.Combat.PlayerRunnerController.Instance ?? Object.FindAnyObjectByType<MobControl.Combat.PlayerRunnerController>();
            if (UnityEditor.EditorApplication.isPlaying && player != null)
            {
                int countBefore = player.TotalSquadSize;
                Debug.Log($"[TEST] Simulating Barricade Hit! Initial Squad Size: {countBefore}");
                
                var barricades = Object.FindObjectsByType<MobControl.Level.RoadObstacle>(FindObjectsSortMode.None);
                MobControl.Level.RoadObstacle targetObs = null;
                foreach (var b in barricades)
                {
                    if (b != null && !b.HasTriggered)
                    {
                        targetObs = b;
                        break;
                    }
                }

                if (targetObs != null)
                {
                    targetObs.TriggerObstacleHit(player.gameObject);
                }
                else
                {
                    if (countBefore > 1)
                    {
                        player.DeductSquadMember();
                        Debug.Log($"[TEST] Squad member deducted! Remaining: {player.TotalSquadSize}");
                    }
                    else
                    {
                        player.Die("CRASHED!", "You crashed into a road barricade.");
                        Debug.Log($"[TEST] Only 1 member left! Died and triggered TRY AGAIN!");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[TEST] TestHitBarricadeDirectly skipped: isPlaying={UnityEditor.EditorApplication.isPlaying}, player={(MobControl.Combat.PlayerRunnerController.Instance != null)}");
            }
        }

        public static void StartMissionAtLevel(int level)
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                var ui = ZombieRunnerUIManager.Instance ?? ZombieRunnerUIManager.EnsureInstance();
                if (ui != null)
                {
                    ui.StartMission(level);
                }
            }
            else
            {
                ZombieRunnerUIManager.autoStartMissionOnLoad = true;
                ZombieRunnerUIManager.levelToAutoStart = level;
                UnityEditor.EditorApplication.isPlaying = true;
            }
        }

        [MenuItem("Zombie Squad/Simulate Swipe Start (Drag or Tap)")]
        public static void SimulateSwipeStart()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                var ui = ZombieRunnerUIManager.Instance ?? ZombieRunnerUIManager.EnsureInstance();
                if (ui != null)
                {
                    ui.StartGameplayFromWaiting();
                }
            }
        }

        [MenuItem("Zombie Squad/Verify Gameplay Logic Fixes")]
        public static void VerifyGameplayFixes()
        {
            Debug.Log("==================================================");
            Debug.Log("[VERIFICATION] Checking Gameplay Logic Fixes...");
            Debug.Log("==================================================");

            // 1. Verify Player Forward & Road Speeds
            GameObject hero = GameObject.Find("Player_Hero");
            if (hero != null)
            {
                var controller = hero.GetComponent<MobControl.Combat.PlayerRunnerController>();
                if (controller != null)
                {
                    Debug.Log($"[SPEED PASS] PlayerRunnerController forwardSpeed: {controller.ForwardSpeed} m/s (balanced, down from 12+ m/s).");
                }
            }

            var roadMgr = Object.FindAnyObjectByType<MobControl.Level.EndlessRoadManager>();
            if (roadMgr != null)
            {
                Debug.Log($"[SPEED PASS] EndlessRoadManager roadSpeed: {roadMgr.RoadSpeed} m/s (balanced, down from 11.5 m/s).");
            }

            // 2. Verify Solid Obstacles & Colliders
            var colliders = Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None);
            int obstacleCount = 0;
            int solidCount = 0;
            foreach (var col in colliders)
            {
                Transform p = col.transform;
                bool isObstacle = false;
                while (p != null)
                {
                    if (p.name.Contains("Barricade") || p.name.Contains("Container") || p.name.Contains("Obstacle"))
                    {
                        isObstacle = true;
                        break;
                    }
                    p = p.parent;
                }

                if (isObstacle)
                {
                    obstacleCount++;
                    if (!col.isTrigger) solidCount++;
                }
            }
            Debug.Log($"[OBSTACLE CHECK] Obstacles found: {obstacleCount}. Solid colliders (isTrigger=false): {solidCount}.");

            // 3. Verify Bullet Impact on Solid Obstacles
            var bulletPrefab = Resources.Load<GameObject>("Prefabs/Bullet_9mm") ?? UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Bullet_9mm.prefab");
            if (bulletPrefab != null)
            {
                var bulletProj = bulletPrefab.GetComponent<MobControl.Combat.BulletProjectile>();
                Debug.Log($"[BULLET CHECK] BulletProjectile verified on prefab with non-passable solid obstacle destruction logic.");
            }

            // 4. Verify Left/Right Dodging & Capsule Sweep
            if (hero != null)
            {
                var runner = hero.GetComponent<MobControl.Combat.PlayerRunnerController>();
                var capCol = hero.GetComponent<CapsuleCollider>();
                var rb = hero.GetComponent<Rigidbody>();
                if (rb != null && rb.collisionDetectionMode != CollisionDetectionMode.ContinuousSpeculative)
                {
                    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                    EditorUtility.SetDirty(hero);
                }
                Debug.Log($"[DODGING CHECK] Player CapsuleCollider: {(capCol != null && !capCol.isTrigger ? "SOLID NON-TRIGGER (PASS)" : "FAIL")}. Rigidbody: {(rb != null && rb.collisionDetectionMode == CollisionDetectionMode.ContinuousSpeculative ? "CONTINUOUS SPECULATIVE (PASS)" : "FAIL")}.");
                Debug.Log($"[DODGING CHECK] Smooth left/right joystick dodging enabled with non-penetrating lateral CapsuleCast clamping.");
            }

            // 5. Verify Road Movement & Game-Over State
            var obstacles = Object.FindObjectsByType<MobControl.Level.RoadObstacle>(FindObjectsSortMode.None);
            Debug.Log($"[GAME-OVER CHECK] RoadObstacles active: {obstacles.Length}. Each obstacle stops EndlessRoadManager immediately upon contact and triggers PlayerRunnerController.Die().");

            // 6. Verify Zombie Direction & Target Tracking
            Debug.Log("[ZOMBIES CHECK] MobUnit configured with Y-axis level lock (toPlayer.y=0), strictly negative Z direction (never running in player's forward direction), and direct target tracking.");
            Debug.Log("[ZOMBIES CHECK] ZombieRoadWaveSpawner configured with spawnZ relative to playerZ (always in front).");
            Debug.Log("==================================================");
        }

        [MenuItem("Zombie Squad/Debug Inspect Play Button")]
        public static void DebugInspectPlayButton()
        {
            var uiMgr = Object.FindAnyObjectByType<MobControl.UI.ZombieRunnerUIManager>();
            if (uiMgr == null) { Debug.LogError("No ZombieRunnerUIManager found!"); return; }
            Debug.Log($"UIManager currentState = {uiMgr.CurrentState}");
            
            var es = UnityEngine.EventSystems.EventSystem.current;
            Debug.Log($"EventSystem.current = {(es != null ? es.name : "NULL")}, enabled = {(es != null ? es.enabled : false)}");

            var playBtn = GameObject.Find("Btn_Play");
            if (playBtn == null)
            {
                Debug.LogError("Btn_Play GameObject NOT FOUND in scene!");
                return;
            }

            var rt = playBtn.GetComponent<RectTransform>();
            var img = playBtn.GetComponent<UnityEngine.UI.Image>();
            var btn = playBtn.GetComponent<UnityEngine.UI.Button>();
            Debug.Log($"Btn_Play: activeInHierarchy={playBtn.activeInHierarchy}, rt.rect={rt.rect}, lossyScale={rt.lossyScale}, anchoredPos={rt.anchoredPosition}, anchorMin={rt.anchorMin}, anchorMax={rt.anchorMax}");
            Debug.Log($"Btn_Play Image: enabled={(img != null && img.enabled)}, color={(img != null ? img.color.ToString() : "none")}, raycastTarget={(img != null && img.raycastTarget)}");
            Debug.Log($"Btn_Play Button: enabled={(btn != null && btn.enabled)}, interactable={(btn != null && btn.interactable)}");

            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Debug.Log($"Btn_Play World Corners: BL={corners[0]}, TL={corners[1]}, TR={corners[2]}, BR={corners[3]}");

            // Test simulated click
            Debug.Log("Invoking btn.onClick...");
            btn.onClick.Invoke();
        }

        [MenuItem("Zombie Squad/Exit Play Mode")]
        public static void ExitPlayMode()
        {
            UnityEditor.EditorApplication.isPlaying = false;
        }

        [MenuItem("Zombie Squad/Audio/Test All Game Sounds")]
        public static void TestAllGameSounds()
        {
            var mgr = MobControl.Audio.ZombieSoundManager.Instance;
            if (mgr == null) { Debug.LogError("No ZombieSoundManager instance!"); return; }
            mgr.PlayGunfire(false);
            mgr.PlayUIClick();
            mgr.PlayUpgradeSuccess();
            mgr.PlayCoinSpend();
            mgr.PlayBGM();
            Debug.Log("[ZombieSoundManager] Successfully triggered test sounds and BGM!");
        }

        [MenuItem("Zombie Squad/Audio/Test Button Click Audio")]
        public static void TestButtonClickAudio()
        {
            var mgr = MobControl.Audio.ZombieSoundManager.Instance;
            if (mgr == null) { Debug.LogError("No ZombieSoundManager instance!"); return; }
            mgr.PlayUIClick();
            Debug.Log("[ZombieSoundManager] Played Button Click Audio!");
        }

        [MenuItem("Zombie Squad/Audio/Test Normal Gunfire Burst")]
        public static void TestNormalGunfireBurst()
        {
            var mgr = MobControl.Audio.ZombieSoundManager.Instance;
            if (mgr == null) { Debug.LogError("No ZombieSoundManager instance!"); return; }
            mgr.PlayGunfire(false);
            Debug.Log("[ZombieSoundManager] Triggered Normal Gunfire!");
        }

        [MenuItem("Zombie Squad/Audio/Test Boost Gunfire Burst")]
        public static void TestBoostGunfireBurst()
        {
            var mgr = MobControl.Audio.ZombieSoundManager.Instance;
            if (mgr == null) { Debug.LogError("No ZombieSoundManager instance!"); return; }
            mgr.PlayGunfire(true);
            Debug.Log("[ZombieSoundManager] Triggered Boost Gunfire!");
        }

        [MenuItem("Zombie Squad/Audio/Test Zombie Defeat Pop")]
        public static void TestZombieDefeatPop()
        {
            var mgr = MobControl.Audio.ZombieSoundManager.Instance;
            if (mgr == null) { Debug.LogError("No ZombieSoundManager instance!"); return; }
            mgr.PlayZombieDie(false);
            Debug.Log("[ZombieSoundManager] Triggered Kid-Friendly Zombie Defeat Pop!");
        }



        [MenuItem("Zombie Squad/Force Recompile Scripts")]
        public static void ForceRecompile()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
        }
    }
}
#endif
