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
            PlayerSettings.productName = "Zombie Rage";
            PlayerSettings.bundleVersion = "1.0.1";
            PlayerSettings.Android.bundleVersionCode = 2;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.mystudio.zombierage");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            // Scripting & Architecture: 64-bit ARM only (Play Store requirement; dropping ARMv7 halves native code size)
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            // Strip unused native engine modules. Assets/link.xml preserves every assembly that
            // dynamically added components rely on (UI, TMP, Physics, Animation, Audio, Assembly-CSharp).
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Minimal);

            // Size: ASTC textures + LZ4HC compressed build data
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
            EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Disabled;

            // SDK compatibility (Android 8.0 Oreo to Android 16 / API Level 36 required by Google Play)
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel36;

            // Graphics & Windowing: Force OpenGLES3 first to prevent Vulkan driver abort on mobile
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3, UnityEngine.Rendering.GraphicsDeviceType.Vulkan });

            PlayerSettings.runInBackground = true;

            ApplyAppIcon();

            // No splash: skip the Unity logo screen
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;

            // Configure Release Keystore
            string keystoreRelative = "Builds/Signing/zombierage_release.keystore";
            string keystoreFull = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), keystoreRelative));
            if (File.Exists(keystoreFull))
            {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = keystoreFull;
                PlayerSettings.Android.keystorePass = "ZombieRage2026!SecureKey";
                PlayerSettings.Android.keyaliasName = "zombierage";
                PlayerSettings.Android.keyaliasPass = "ZombieRage2026!SecureKey";
                Debug.Log($"[BuildHelper] Release Keystore configured: {keystoreFull}");
            }
            else
            {
                Debug.LogWarning($"[BuildHelper] Keystore not found at {keystoreFull}");
            }

            Debug.Log("[BuildHelper] Android PlayerSettings successfully configured (OpenGLES3+Vulkan, ARM64, IL2CPP, EngineStrip=On, ASTC).");
        }

        private const string AppIconPath = "Assets/Icons/ZombieRage_Icon.png";
        private const string AppIconBgPath = "Assets/Icons/ZombieRage_Icon_Background.png";
        private const string AppIconFgPath = "Assets/Icons/ZombieRage_Icon_Foreground.png";

        private static Texture2D LoadIcon(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && (importer.textureCompression != TextureImporterCompression.Uncompressed || importer.mipmapEnabled))
            {
                importer.textureType = TextureImporterType.Default;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.maxTextureSize = 1024;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        [MenuItem("Tools/Android/Apply Zombie Rage App Icon")]
        public static void ApplyAppIcon()
        {
            Texture2D icon = LoadIcon(AppIconPath);
            Texture2D bg = LoadIcon(AppIconBgPath);
            Texture2D fg = LoadIcon(AppIconFgPath);
            if (icon == null || bg == null || fg == null) { Debug.LogWarning("[BuildHelper] App icon textures missing in Assets/Icons"); return; }

            // Default icon (fallback for every platform/size without an override)
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { icon });

            // Android adaptive icon: layer 0 = background, layer 1 = foreground (content kept in the 66% safe zone)
            PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, UnityEditor.Android.AndroidPlatformIconKind.Adaptive);
            foreach (var pi in icons) pi.SetTextures(bg, fg);
            PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, UnityEditor.Android.AndroidPlatformIconKind.Adaptive, icons);
            AssetDatabase.SaveAssets();
            Debug.Log("[BuildHelper] Zombie Rage app icon applied (default + Android adaptive).");
        }

        [MenuItem("Tools/Android/Build Android APK (Direct to Desktop)")]
        public static void BuildDirectDesktopApk()
        {
            string desktopApk = "/Users/savvy/Desktop/ZombieRage.apk";
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
                "ZombieRage.apk",
                "apk"
            );

            if (string.IsNullOrEmpty(savePath))
            {
                Debug.Log("[BuildHelper] Build cancelled by user.");
                return;
            }

            PerformBuild(savePath);
        }

        [MenuItem("Build/Build Android AAB (Google Play Store)")]
        public static void BuildReleaseAAB()
        {
            string projectBuildDir = Path.Combine(Directory.GetCurrentDirectory(), "Builds");
            if (!Directory.Exists(projectBuildDir)) Directory.CreateDirectory(projectBuildDir);
            string savePath = Path.Combine(projectBuildDir, "ZombieRage_v1.0.0_Release.aab");

            PerformBuild(savePath, buildAab: true);

            // Also copy to desktop for immediate convenience
            try
            {
                string desktopDir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
                string desktopAab = Path.Combine(desktopDir, "ZombieSquad_Release.aab");
                if (File.Exists(savePath))
                {
                    File.Copy(savePath, desktopAab, true);
                    Debug.Log($"[BuildHelper] Copied release AAB to Desktop: {desktopAab}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BuildHelper] Could not copy to desktop: {ex.Message}");
            }
        }

        public static void BuildApkBatch()
        {
            string desktopApk = "/Users/savvy/Desktop/ZombieRage.apk";
            PerformBuild(desktopApk, buildAab: false);
        }

        private static void PerformBuild(string savePath, bool buildAab = false)
        {
            // 0. Ensure Play mode is OFF
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                EditorApplication.delayCall += () => PerformBuild(savePath, buildAab);
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

            // 3. Configure build format: AAB (Google Play) vs APK (Testing)
            EditorPrefs.SetBool("SdkUseEmbedded", true);
            EditorPrefs.SetBool("NdkUseEmbedded", true);
            EditorPrefs.SetBool("JdkUseEmbedded", true);
            EditorPrefs.SetBool("GradleUseEmbedded", true);

            EditorUserBuildSettings.buildAppBundle = buildAab;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            if (buildAab)
            {
                PlayerSettings.Android.useCustomKeystore = true;
                string keystoreRelative = "Builds/Signing/zombierage_release.keystore";
                string keystoreFull = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), keystoreRelative));
                PlayerSettings.Android.keystoreName = keystoreFull;
                PlayerSettings.Android.keystorePass = "ZombieRage2026!SecureKey";
                PlayerSettings.Android.keyaliasName = "zombierage";
                PlayerSettings.Android.keyaliasPass = "ZombieRage2026!SecureKey";
            }
            else
            {
                PlayerSettings.Android.useCustomKeystore = false;
            }

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
                options = BuildOptions.CompressWithLz4HC
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

            if (File.Exists("Temp/trigger_build_aab.txt"))
            {
                s_isBuilding = true;
                try
                {
                    File.Delete("Temp/trigger_build_aab.txt");
                }
                catch { }

                Debug.Log("[AndroidBuildTriggerWatch] trigger_build_aab.txt detected! Starting Android Release AAB build...");
                try
                {
                    AndroidBuildHelper.BuildReleaseAAB();
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
