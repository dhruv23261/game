#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using MobControl.Combat;

namespace MobControl.EditorTools
{
    public static class InspectSceneBullet
    {
        [MenuItem("Zombie Squad/Inspect Active Bullets In Scene")]
        public static void InspectActiveBullets()
        {
            var bullets = Object.FindObjectsByType<BulletProjectile>(FindObjectsInactive.Include);
            Debug.Log($"[InspectBullets] Found {bullets.Length} BulletProjectile instances in scene.");
            if (bullets.Length > 0)
            {
                var b = bullets[0];
                var v = b.transform.Find("Visual_9mmBullet");
                Debug.Log($"[InspectBullets] Sample Bullet: {b.gameObject.name}, active={b.gameObject.activeInHierarchy}, rootRot={b.transform.rotation.eulerAngles}, visualLocalRot={(v != null ? v.localRotation.eulerAngles.ToString() : "null")}, visualLocalPos={(v != null ? v.localPosition.ToString() : "null")}");
            }

            var player = Object.FindAnyObjectByType<PlayerRunnerController>();
            if (player != null)
            {
                var f = typeof(PlayerRunnerController).GetField("bulletPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var prefab = f != null ? f.GetValue(player) as GameObject : null;
                Debug.Log($"[PlayerRunnerController] current bulletPrefab field: {(prefab != null ? prefab.name : "null")}, path={AssetDatabase.GetAssetPath(prefab)}");
            }
        }

        [MenuItem("Zombie Squad/Inspect Active Mobs")]
        public static void InspectActiveMobs()
        {
            var mobs = Object.FindObjectsByType<MobControl.Crowd.MobUnit>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Debug.Log($"[InspectMobs] Total MobUnits in scene: {mobs.Length}");
            int activeCount = 0;
            foreach (var m in mobs)
            {
                if (m.gameObject.activeInHierarchy)
                {
                    activeCount++;
                    Debug.Log($"[InspectMob] {m.gameObject.name} | Pos={m.transform.position} | Rot={m.transform.rotation.eulerAngles} | Team={m.Team} | IsDead={m.IsDead}");
                }
            }
            Debug.Log($"[InspectMobs] Active count: {activeCount}");
        }

        [MenuItem("Zombie Squad/Spawn Gun Upgrade Container Preview")]
        public static void SpawnGunUpgradeContainerPreview()
        {
            if (MobControl.Level.EndlessRoadManager.Instance != null)
            {
                var method = typeof(MobControl.Level.EndlessRoadManager).GetMethod("SpawnGunUpgradeContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (method != null)
                {
                    method.Invoke(MobControl.Level.EndlessRoadManager.Instance, new object[] { MobControl.Level.EndlessRoadManager.Instance.transform, new Vector3(0f, 1.10f, 12.0f), 30 });
                    Debug.Log("[GunUpgradePreview] Spawned Gun Upgrade Container (30 HP) at Z=12m directly in front of camera!");
                }
            }
        }

        [MenuItem("Zombie Squad/Capture Leg Walk Screenshot")]
        public static void CaptureLegWalk()
        {
            string path = "/Users/savvy/.gemini/antigravity-ide/brain/3991ea85-e30c-48a6-b2bb-cd02ce52a055/.tempmediaStorage/leg_walk_live.png";
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"[SCREENSHOT] Captured leg walk screenshot: {path}");
        }

        [MenuItem("Zombie Squad/Apply Elevated Top Camera View")]
        public static void ApplyElevatedCamera()
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                var camFollow = mainCam.GetComponent<MobControl.Core.SmoothCameraFollow>();
                Vector3 newOffset = new Vector3(0f, 5.80f, -6.80f);
                Vector3 newRot = new Vector3(29.0f, 0f, 0f);
                if (camFollow != null)
                {
                    camFollow.SetCameraParameters(newOffset, newRot);
                }
                mainCam.transform.position = newOffset;
                mainCam.transform.rotation = Quaternion.Euler(newRot);
                string path = "/Users/savvy/.gemini/antigravity-ide/brain/3991ea85-e30c-48a6-b2bb-cd02ce52a055/.tempmediaStorage/elevated_camera_view.png";
                ScreenCapture.CaptureScreenshot(path);
                Debug.Log($"[CAMERA] Applied elevated camera offset {newOffset}, rot {newRot}. Screenshot: {path}");
            }
        }

        [MenuItem("Zombie Squad/Scan For Magenta Renderers")]
        public static void ScanForMagenta()
        {
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
            {
                Vector3 p = r.transform.position;
                if (p.z > 20f && p.z < 65f && p.y > 1.5f)
                {
                    Debug.Log($"[HIGH OBJ] Name: '{r.gameObject.name}', Root: '{r.transform.root.name}', Parent: '{r.transform.parent?.name}', Pos: {p}, Mat: {(r.sharedMaterial != null ? r.sharedMaterial.name : "null")}, Shader: {(r.sharedMaterial != null && r.sharedMaterial.shader != null ? r.sharedMaterial.shader.name : "null")}");
                }
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null || (m.shader != null && (m.shader.name.Contains("InternalErrorShader") || m.shader.name.Contains("Universal"))))
                    {
                        Debug.LogError($"[MAGENTA DETECTED] Obj: '{r.gameObject.name}', Parent: '{r.transform.parent?.name}', Pos: {r.transform.position}, Mat: {(m != null ? m.name : "null")}, Shader: {(m != null && m.shader != null ? m.shader.name : "null")}");
                    }
                }
            }

            foreach (var g in Object.FindObjectsByType<UnityEngine.UI.Graphic>(FindObjectsInactive.Include))
            {
                if (g.color.r > 0.8f && g.color.b > 0.8f && g.color.g < 0.2f)
                {
                    Debug.LogError($"[MAGENTA UI DETECTED] Graphic: '{g.gameObject.name}', Parent: '{g.transform.parent?.name}', Root: '{g.transform.root.name}', Pos: {g.rectTransform.anchoredPosition}, Size: {g.rectTransform.sizeDelta}, Color: {g.color}");
                }
            }
        }

        [MenuItem("Zombie Squad/Test Run And Capture Leg Animation")]
        public static void TestRunAndCapture()
        {
            var ui = MobControl.UI.ZombieRunnerUIManager.Instance ?? MobControl.UI.ZombieRunnerUIManager.EnsureInstance();
            if (ui != null)
            {
                ui.StartMission(1);
                ui.StartGameplayFromWaiting();
            }
            var player = Object.FindAnyObjectByType<PlayerRunnerController>();
            if (player != null)
            {
                player.SetInvulnerable(999f);
                player.SetObstacleInvulnerable(999f);
            }
        }

        [MenuItem("Zombie Squad/Inspect Player Bones")]
        public static void InspectPlayerBones()
        {
            var player = Object.FindAnyObjectByType<PlayerRunnerController>();
            if (player != null)
            {
                var ctrl = player.GetComponentInChildren<PlayerMobVisualController>();
                Debug.Log($"[InspectPlayerBones] PlayerMobVisualController on player? {(ctrl != null ? "YES (" + ctrl.gameObject.name + ")" : "NO")}");
                var transforms = player.GetComponentsInChildren<Transform>(true);
                foreach (var t in transforms)
                {
                    if (t.name.Contains("UpLeg") || t.name.Contains("LeftLeg") || t.name.Contains("RightLeg") || t.name.Contains("Foot"))
                    {
                        Vector3 charForward = player.transform.InverseTransformDirection(t.forward);
                        Vector3 charUp = player.transform.InverseTransformDirection(t.up);
                        Vector3 charRight = player.transform.InverseTransformDirection(t.right);
                        Debug.Log($"Bone '{t.name}': in Char Space -> forward={charForward}, up={charUp}, right={charRight}");
                    }
                }
            }
        }

        [MenuItem("Zombie Squad/Debug Inspect Bullet FBX Orientation")]
        public static void DebugBulletFBX()
        {
            GameObject bulletFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Bullet/source/9mm_bullet.fbx");
            if (bulletFbx != null)
            {
                Debug.Log($"[DebugBulletFBX] Found FBX: {bulletFbx.name}, children count = {bulletFbx.transform.childCount}");
                for (int i = 0; i < bulletFbx.transform.childCount; i++)
                {
                    var child = bulletFbx.transform.GetChild(i);
                    Debug.Log($"--- FBX Child[{i}]: '{child.name}' ---");
                    Debug.Log($"    localPos: {child.localPosition}, localRot: {child.localRotation.eulerAngles}, localScale: {child.localScale}");
                    var mf = child.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null)
                    {
                        var m = mf.sharedMesh;
                        Vector3 tipVert = Vector3.zero;
                        float maxCoord = -9999f;
                        foreach (var v in m.vertices)
                        {
                            // Check along each local axis
                            if (Mathf.Abs(v.x) > maxCoord) { maxCoord = Mathf.Abs(v.x); tipVert = v; }
                        }
                        Debug.Log($"    Mesh '{m.name}' vertCount={m.vertexCount}");
                        // Check along X, Y, Z
                        float minX = 999f, maxX = -999f, minY = 999f, maxY = -999f, minZ = 999f, maxZ = -999f;
                        foreach (var v in m.vertices)
                        {
                            minX = Mathf.Min(minX, v.x); maxX = Mathf.Max(maxX, v.x);
                            minY = Mathf.Min(minY, v.y); maxY = Mathf.Max(maxY, v.y);
                            minZ = Mathf.Min(minZ, v.z); maxZ = Mathf.Max(maxZ, v.z);
                        }
                        Debug.Log($"    Raw Mesh Local: X=[{minX:F5}, {maxX:F5}] (len={maxX-minX:F5}), Y=[{minY:F5}, {maxY:F5}] (len={maxY-minY:F5}), Z=[{minZ:F5}, {maxZ:F5}] (len={maxZ-minZ:F5})");

                        // Transformed by child.localRotation:
                        minX = 999f; maxX = -999f; minY = 999f; maxY = -999f; minZ = 999f; maxZ = -999f;
                        foreach (var v in m.vertices)
                        {
                            Vector3 tv = child.localRotation * v;
                            minX = Mathf.Min(minX, tv.x); maxX = Mathf.Max(maxX, tv.x);
                            minY = Mathf.Min(minY, tv.y); maxY = Mathf.Max(maxY, tv.y);
                            minZ = Mathf.Min(minZ, tv.z); maxZ = Mathf.Max(maxZ, tv.z);
                        }
                        Debug.Log($"    After child.localRotation: X=[{minX:F5}, {maxX:F5}], Y=[{minY:F5}, {maxY:F5}], Z=[{minZ:F5}, {maxZ:F5}]");
                    }
                }
            }
            else
            {
                Debug.LogError("[DebugBulletFBX] Assets/Models/Bullet/source/9mm_bullet.fbx not found!");
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Bullet_9mm.prefab");
            if (prefab != null)
            {
                Debug.Log($"[DebugBulletFBX] Inspecting Prefab: {prefab.name}");
                for (int i = 0; i < prefab.transform.childCount; i++)
                {
                    var child = prefab.transform.GetChild(i);
                    Debug.Log($"  Prefab Child[{i}]: '{child.name}', localPos={child.localPosition}, localRot={child.localRotation.eulerAngles}, localScale={child.localScale}");
                    for (int j = 0; j < child.childCount; j++)
                    {
                        var grandChild = child.GetChild(j);
                        Debug.Log($"    GrandChild[{j}]: '{grandChild.name}', localPos={grandChild.localPosition}, localRot={grandChild.localRotation.eulerAngles}, localScale={grandChild.localScale}");
                    }
                }
            }
        }

        [MenuItem("Zombie Squad/Inspect All Visible Meshes")]
        public static void InspectAllVisibleMeshes()
        {
            Debug.Log("====================== VISIBLE MESH INSPECTION ======================");
            var renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Debug.Log($"Total ACTIVE renderers in scene: {renderers.Length}");
            foreach (var r in renderers)
            {
                if (r is CanvasRenderer) continue;
                string path = r.name;
                Transform p = r.transform.parent;
                while (p != null)
                {
                    path = p.name + "/" + path;
                    p = p.parent;
                }
                Debug.Log($"[Renderer] {path} | Type={r.GetType().Name} | Enabled={r.enabled} | Pos={r.transform.position} | Scale={r.transform.lossyScale} | Mat={(r.sharedMaterial != null ? r.sharedMaterial.name : "null")} | Shader={(r.sharedMaterial != null && r.sharedMaterial.shader != null ? r.sharedMaterial.shader.name : "null")}");
            }
            Debug.Log("====================================================================");
        }

        [MenuItem("Zombie Squad/Clean And Prepare Android Scene")]
        public static void PrepareAndCleanSceneForAndroid()
        {
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
            var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if (!scene.isLoaded || scene.name != "q")
            {
                scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/q.unity");
            }

            Debug.Log("[AndroidPrepare] Cleaning scene of all duplicate pools and clutter...");
            RoadGeneratorMenuItem.CleanLevel1Scene();

            Debug.Log("[AndroidPrepare] Rebuilding Level 1 road and player setup cleanly...");
            RoadGeneratorMenuItem.EnsureAssaultRifleMaterialAsset();
            BulletPrefabBuilder.BuildPrefab();
            RoadGeneratorMenuItem.BuildReferenceLevel1Road();

            Debug.Log("[AndroidPrepare] Applying Android PlayerSettings (ARMv7+ARM64, no engine stripping)...");
            AndroidBuildHelper.ConfigureAndroidPlayerSettings();

            var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(activeScene);

            var rootObjs = activeScene.GetRootGameObjects();
            Debug.Log($"<color=#00FF88><b>[AndroidPrepare] SUCCESS! Scene cleaned and saved. Root GameObject count: {rootObjs.Length}</b></color>");
        }

        [MenuItem("Zombie Squad/Refresh Assets And Recompile")]
        public static void RefreshAssetsAndRecompile()
        {
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            Debug.Log("[ZombieSquad] AssetDatabase.Refresh(ForceUpdate) called successfully!");
        }

        [MenuItem("Zombie Squad/Test Open Shop And Capture")]
        public static void TestOpenShopAndCapture()
        {
            var ui = MobControl.UI.ZombieRunnerUIManager.Instance ?? MobControl.UI.ZombieRunnerUIManager.EnsureInstance();
            if (ui != null)
            {
                ui.ShowZombieSquadShopScreen();
                Debug.Log("[TestShop] ShowZombieSquadShopScreen called!");

                var allTmps = ui.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
                foreach (var t in allTmps)
                {
                    if (t.name.Contains("Price") || t.transform.parent?.name.Contains("Btn_Buy") == true || t.transform.parent?.name.Contains("PriceRow") == true)
                    {
                        Debug.Log($"[TestShop TMP] Obj: '{t.gameObject.name}', Parent: '{t.transform.parent?.name}', Text: '{t.text}', Font: {(t.font != null ? t.font.name : "null")}, Active: {t.gameObject.activeInHierarchy}, Enabled: {t.enabled}");
                    }
                }

                var allImgs = ui.GetComponentsInChildren<UnityEngine.UI.Image>(true);
                foreach (var im in allImgs)
                {
                    if (im.name.Contains("Rupee") || im.transform.parent?.name.Contains("PriceRow") == true)
                    {
                        Debug.Log($"[TestShop IMG] Obj: '{im.gameObject.name}', Parent: '{im.transform.parent?.name}', Sprite: {(im.sprite != null ? im.sprite.name : "null")}, Active: {im.gameObject.activeInHierarchy}, Enabled: {im.enabled}");
                    }
                }

                string ssPath = "/Users/savvy/.gemini/antigravity-ide/brain/c4840675-c01c-4015-b0af-1b83673d3587/.tempmediaStorage/shop_test_capture.png";
                ScreenCapture.CaptureScreenshot(ssPath);
                Debug.Log($"[TestShop] Screenshot saved: {ssPath}");
            }
            else
            {
                Debug.LogWarning("[TestShop] ZombieRunnerUIManager not found in scene!");
            }
        }
    }
}
#endif
