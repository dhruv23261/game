using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MobControl.EditorTools
{
    public class AndroidBuildHelper
    {
        [MenuItem("Tools/Android/Build Android APK")]
        public static void BuildAndroidApk()
        {
            // 1. Ensure Scene is set
            string scenePath = "Assets/q.unity";
            if (!File.Exists(scenePath))
            {
                Debug.LogError($"[BuildHelper] Scene not found at {scenePath}");
                return;
            }

            // 2. Set Player Settings
            PlayerSettings.companyName = "MyStudio";
            PlayerSettings.productName = "Zombie Runner 3D";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.mystudio.zombierunner3d");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            // 3. Switch to Android Build Target if needed
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                Debug.Log("[BuildHelper] Switching active build target to Android...");
                bool switched = EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
                if (!switched)
                {
                    Debug.LogError("[BuildHelper] Could not switch to Android platform. Please ensure 'Android Build Support' module is installed in Unity Hub for this Unity version.");
                    EditorUtility.DisplayDialog("Android Module Missing", "Android Build Support is not installed for this Unity version in Unity Hub.\n\nPlease open Unity Hub > Installs > Add Modules > Android Build Support (with SDK & NDK).", "OK");
                    return;
                }
            }

            // 4. Select Destination Path
            string defaultFolder = Path.Combine(Directory.GetCurrentDirectory(), "Builds");
            if (!Directory.Exists(defaultFolder))
            {
                Directory.CreateDirectory(defaultFolder);
            }

            string savePath = EditorUtility.SaveFilePanel(
                "Save Android APK",
                defaultFolder,
                "ZombieRunner.apk",
                "apk"
            );

            if (string.IsNullOrEmpty(savePath))
            {
                Debug.Log("[BuildHelper] Build cancelled by user.");
                return;
            }

            // 5. Build Player
            BuildPlayerOptions buildOptions = new BuildPlayerOptions
            {
                scenes = new[] { scenePath },
                locationPathName = savePath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };

            Debug.Log($"[BuildHelper] Starting Android APK build to: {savePath}...");
            try
            {
                BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
                BuildSummary summary = report.summary;

                if (summary.result == BuildResult.Succeeded && File.Exists(savePath))
                {
                    Debug.Log($"<color=#00FF66><b>[BuildHelper] BUILD SUCCESS!</b></color> Output APK: {savePath} ({summary.totalSize / (1024 * 1024)} MB)");
                    EditorUtility.RevealInFinder(savePath);
                }
                else
                {
                    Debug.LogError($"[BuildHelper] BUILD FAILED or INCOMPLETE! Total errors: {summary.totalErrors}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[BuildHelper] Build failed: {ex.Message}");
            }
        }
    }
}
