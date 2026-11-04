using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MobControl.EditorTools
{
    public class AndroidBuildHelper
    {
        public static void ConfigureAndroidPlayerSettings()
        {
            PlayerSettings.companyName = "MyStudio";
            PlayerSettings.productName = "Zombie Runner 3D";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.mystudio.zombierunner3d");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            // Scripting & Architecture: Target both 32-bit and 64-bit ARM
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;

            // Prevent code stripping of dynamically added components (UI, TMP, PhysX)
            PlayerSettings.stripEngineCode = false;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Minimal);

            // SDK compatibility (Android 8.0 Oreo to Android 14)
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel34;

            // Graphics & Windowing: Force OpenGLES3 first to prevent Vulkan driver abort on mobile
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3, UnityEngine.Rendering.GraphicsDeviceType.Vulkan });

            PlayerSettings.runInBackground = true;

            Debug.Log("[BuildHelper] Android PlayerSettings successfully configured (OpenGLES3+Vulkan, ARMv7+ARM64, IL2CPP, Stripping=Minimal, SilentExit).");
        }

        [MenuItem("Tools/Android/Build Android APK (Direct to Desktop)")]
        public static void BuildDirectDesktopApk()
        {
            string desktopApk = "/Users/savvy/Desktop/ZombieSquad_Runner3D.apk";
            PerformBuild(desktopApk);
        }

        [MenuItem("Tools/Android/Build Android APK (Choose Path...)")]
        public static void BuildAndroidApk()
        {
            string defaultFolder = Path.Combine(Directory.GetCurrentDirectory(), "Builds");
            if (!Directory.Exists(defaultFolder))
            {
                Directory.CreateDirectory(defaultFolder);
            }

            string savePath = EditorUtility.SaveFilePanel(
                "Save Android APK",
                defaultFolder,
                "ZombieSquad_Runner3D.apk",
                "apk"
            );

            if (string.IsNullOrEmpty(savePath))
            {
                Debug.Log("[BuildHelper] Build cancelled by user.");
                return;
            }

            PerformBuild(savePath);
        }

        public static void BuildApkBatch()
        {
            string desktopApk = "/Users/savvy/Desktop/ZombieSquad_Runner3D.apk";
            PerformBuild(desktopApk);
        }

        private static void PerformBuild(string savePath)
        {
            // 0. Ensure Play mode is OFF
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                EditorApplication.delayCall += () => PerformBuild(savePath);
                return;
            }

            // 1. Ensure Scene is loaded and saved cleanly
            string scenePath = "Assets/q.unity";
            if (!File.Exists(scenePath))
            {
                Debug.LogError($"[BuildHelper] Scene not found at {scenePath}");
                return;
            }

            var currentScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if (!currentScene.isLoaded || currentScene.path != scenePath)
            {
                Debug.Log($"[BuildHelper] Opening scene: {scenePath}");
                currentScene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
            }

            // Clean scene clutter and ensure assets
            RoadGeneratorMenuItem.CleanLevel1Scene();
            RoadGeneratorMenuItem.EnsureAssaultRifleMaterialAsset();
            BulletPrefabBuilder.BuildPrefab();
            RoadGeneratorMenuItem.BuildReferenceLevel1Road();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(currentScene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(currentScene);

            // 2. Configure PlayerSettings
            ConfigureAndroidPlayerSettings();

            // 3. Ensure embedded tools & APK format (NOT AAB)
            EditorPrefs.SetBool("SdkUseEmbedded", true);
            EditorPrefs.SetBool("NdkUseEmbedded", true);
            EditorPrefs.SetBool("JdkUseEmbedded", true);
            EditorPrefs.SetBool("GradleUseEmbedded", true);

            EditorUserBuildSettings.buildAppBundle = false; // MUST be false for APK!
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            PlayerSettings.Android.useCustomKeystore = false;

            // 4. Switch to Android Build Target if needed
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                Debug.Log("[BuildHelper] Switching active build target to Android...");
                bool switched = EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
                if (!switched)
                {
                    Debug.LogError("[BuildHelper] Could not switch to Android platform. Please ensure 'Android Build Support' is installed.");
                    return;
                }
            }

            // 5. Ensure destination folder exists
            string dir = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // 6. Build Player
            BuildPlayerOptions buildOptions = new BuildPlayerOptions
            {
                scenes = new[] { scenePath },
                locationPathName = savePath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };

            Debug.Log($"[BuildHelper] ========================================================");
            Debug.Log($"[BuildHelper] Starting Android APK build to: {savePath}...");
            Debug.Log($"[BuildHelper] Target Architectures: {PlayerSettings.Android.targetArchitectures}");
            Debug.Log($"[BuildHelper] Scripting Backend: {PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android)}");
            Debug.Log($"[BuildHelper] ========================================================");

            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
                BuildSummary summary = report.summary;
                sw.Stop();

                if (summary.result == BuildResult.Succeeded && File.Exists(savePath))
                {
                    long sizeBytes = new FileInfo(savePath).Length;
                    double sizeMb = sizeBytes / (1024.0 * 1024.0);

                    // Also make a copy with generic name ZombieRunner.apk on Desktop for convenience
                    string altPath = "/Users/savvy/Desktop/ZombieRunner.apk";
                    if (savePath != altPath)
                    {
                        try { File.Copy(savePath, altPath, true); } catch { }
                    }

                    Debug.Log($"<color=#00FF66><b>[BuildHelper] BUILD SUCCESSFUL in {sw.Elapsed.TotalSeconds:F1}s!</b></color>");
                    Debug.Log($"<color=#00FF66><b>[BuildHelper] Output APK: {savePath} ({sizeMb:F2} MB)</b></color>");
                    EditorUtility.RevealInFinder(savePath);
                }
                else
                {
                    Debug.LogError($"[BuildHelper] BUILD FAILED or INCOMPLETE in {sw.Elapsed.TotalSeconds:F1}s! Result: {summary.result}, Errors: {summary.totalErrors}");
                }
            }
            catch (System.Exception ex)
            {
                sw.Stop();
                Debug.LogError($"[BuildHelper] Build exception after {sw.Elapsed.TotalSeconds:F1}s: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }

    [InitializeOnLoad]
    public static class AndroidBuildTriggerWatch
    {
        private static bool s_isBuilding = false;

        static AndroidBuildTriggerWatch()
        {
            EditorSettings.unityRemoteDevice = "None";
            EditorApplication.update -= CheckTrigger;
            EditorApplication.update += CheckTrigger;
        }

        private static void CheckTrigger()
        {
            if (s_isBuilding) return;

            if (File.Exists("Temp/trigger_build.txt"))
            {
                s_isBuilding = true;
                try
                {
                    File.Delete("Temp/trigger_build.txt");
                }
                catch { }

                Debug.Log("[AndroidBuildTriggerWatch] trigger_build.txt detected! Starting Android APK build to Desktop...");
                try
                {
                    AndroidBuildHelper.BuildDirectDesktopApk();
                }
                finally
                {
                    s_isBuilding = false;
                }
            }

            if (File.Exists("Temp/trigger_test_shop.txt"))
            {
                try
                {
                    File.Delete("Temp/trigger_test_shop.txt");
                }
                catch { }

                Debug.Log("[AndroidBuildTriggerWatch] trigger_test_shop.txt detected! Refreshing AssetDatabase...");
                AssetDatabase.Refresh();
            }
        }
    }
}
