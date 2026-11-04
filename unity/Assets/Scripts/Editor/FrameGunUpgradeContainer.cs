using UnityEditor;
using UnityEngine;

namespace MobControl.EditorTools
{
    public static class FrameGunUpgradeContainer
    {
        [MenuItem("Zombie Squad/Frame Gun Upgrade Container In Scene")]
        public static void FrameContainer()
        {
            GameObject target = GameObject.Find("Obstacle_Container_GunUpgrade");
            if (target != null)
            {
                Selection.activeGameObject = target;
                if (SceneView.lastActiveSceneView != null)
                {
                    SceneView.lastActiveSceneView.FrameSelected();
                    SceneView.lastActiveSceneView.Repaint();
                }
                Debug.Log($"[FrameContainer] Selected and framed {target.name} at {target.transform.position}");
            }
            else
            {
                Debug.LogWarning("[FrameContainer] Could not find 'Obstacle_Container_GunUpgrade' in active scene.");
            }
        }

        [MenuItem("Zombie Squad/Test Trigger 15s Gun Upgrade")]
        public static void TestTriggerGunUpgrade()
        {
            if (UI.ZombieRunnerUIManager.Instance != null)
            {
                if (UI.ZombieRunnerUIManager.Instance.CurrentState != UI.ZombieRunnerUIManager.ScreenState.Gameplay)
                {
                    UI.ZombieRunnerUIManager.Instance.StartMission(1);
                }
                UI.ZombieRunnerUIManager.Instance.StartGameplayFromWaiting();
            }
            if (Combat.PlayerRunnerController.Instance != null)
            {
                Combat.PlayerRunnerController.Instance.SetWaitingToStart(false);
                Combat.PlayerRunnerController.Instance.UpgradeGunAbility(2.2f, true);
                Debug.Log("[TestTriggerGunUpgrade] Triggered 15-second Gun Upgrade on PlayerRunnerController!");
            }
            else
            {
                Debug.LogWarning("[TestTriggerGunUpgrade] PlayerRunnerController.Instance not found.");
            }
        }

        [MenuItem("Zombie Squad/Preview Widened Zone 9 Containers")]
        public static void PreviewWidenedZone9()
        {
            if (MobControl.Level.EndlessRoadManager.Instance != null)
            {
                var erm = MobControl.Level.EndlessRoadManager.Instance;
                var method = typeof(MobControl.Level.EndlessRoadManager).GetMethod("SpawnAngledContainerObstacle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (method != null)
                {
                    GameObject prev = GameObject.Find("Test_Zone9_Preview");
                    if (prev != null) Object.DestroyImmediate(prev);

                    GameObject root = new GameObject("Test_Zone9_Preview");
                    float zPos = 12f;
                    if (Combat.PlayerRunnerController.Instance != null)
                        zPos = Combat.PlayerRunnerController.Instance.transform.position.z + 12f;
                    else if (Camera.main != null)
                        zPos = Camera.main.transform.position.z + 12f;

                    root.transform.position = new Vector3(0f, 0f, zPos);

                    var redMat = erm.GetType().GetField("containerRedMat", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(erm) as Material;
                    var greenMat = erm.GetType().GetField("containerGreenMat", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(erm) as Material;

                    method.Invoke(erm, new object[] { root.transform, new Vector3(-3.25f, 1.10f, 0.0f), 8.0f, redMat });
                    method.Invoke(erm, new object[] { root.transform, new Vector3(3.25f, 1.10f, 0.0f), -8.0f, greenMat });
                    Debug.Log("[PreviewWidenedZone9] Spawned widened Zone 9 containers preview at Z=15m!");
                }
            }
        }

        [MenuItem("Zombie Squad/Clean And Purge All Badges From Scene")]
        public static void CleanAndPurgeAllBadges()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                EditorApplication.delayCall += () => CleanAndPurgeAllBadges();
                return;
            }

            string scenePath = "Assets/q.unity";
            var currentScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if (!currentScene.isLoaded || currentScene.path != scenePath)
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
            }

            GameObject[] allGos = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            int count = 0;
            foreach (var go in allGos)
            {
                if (go != null && (go.name.StartsWith("BadgePop_") || go.name.Contains("Celebratory") || go.name.Contains("score_2x") || go.name.Contains("purple_boost")))
                {
                    Object.DestroyImmediate(go);
                    count++;
                }
            }

            RoadGeneratorMenuItem.CleanLevel1Scene();
            RoadGeneratorMenuItem.EnsureAssaultRifleMaterialAsset();
            BulletPrefabBuilder.BuildPrefab();
            RoadGeneratorMenuItem.BuildReferenceLevel1Road();

            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            Debug.Log($"[CleanAndPurgeAllBadges] Purged {count} stray badge objects, rebuilt road and saved clean scene!");
        }

        [MenuItem("Zombie Squad/Capture Clean Scene Screenshot")]
        public static void CaptureCleanSceneScreenshot()
        {
            string path = "/Users/savvy/.gemini/antigravity-ide/brain/c4840675-c01c-4015-b0af-1b83673d3587/.tempmediaStorage/clean_scene_check.png";
            Camera cam = Camera.main;
            if (cam == null && Camera.allCameras.Length > 0) cam = Camera.allCameras[0];
            if (cam == null) cam = Object.FindAnyObjectByType<Camera>();
            if (cam != null)
            {
                RenderTexture rt = new RenderTexture(1080, 1920, 24);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                Texture2D screenShot = new Texture2D(1080, 1920, TextureFormat.RGB24, false);
                screenShot.ReadPixels(new Rect(0, 0, 1080, 1920), 0, 0);
                screenShot.Apply();
                cam.targetTexture = null;
                RenderTexture.active = null;
                byte[] bytes = screenShot.EncodeToPNG();
                System.IO.File.WriteAllBytes(path, bytes);
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(screenShot);
                Debug.Log($"[SCREENSHOT] Clean scene captured to: {path}");
            }
        }
    }
}

