using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MobControl.EditorTools
{
    public class IOSBuildHelper
    {
        public static void ConfigureIOSPlayerSettings()
        {
            PlayerSettings.companyName = "MyStudio";
            PlayerSettings.productName = "Zombie Rage";
            PlayerSettings.bundleVersion = "1.0.1";
            PlayerSettings.iOS.buildNumber = "1";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.mystudio.zombierage");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            // Scripting backend & Device
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
            PlayerSettings.iOS.targetOSVersionString = "14.0";

            // Performance & size
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.iOS, ManagedStrippingLevel.Minimal);

            // Hide splash
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;

            Debug.Log("[IOSBuildHelper] iOS PlayerSettings configured (com.mystudio.zombierage, IL2CPP, DeviceSDK, iOS 14.0+).");
        }

        [MenuItem("Build/Build iOS Xcode Project")]
        public static void BuildXcodeProject()
        {
            string projectBuildDir = Path.Combine(Directory.GetCurrentDirectory(), "Builds/iOS_Xcode");
            if (!Directory.Exists(projectBuildDir))
            {
                Directory.CreateDirectory(projectBuildDir);
            }

            ConfigureIOSPlayerSettings();

            string scenePath = "Assets/q.unity";
            BuildPlayerOptions buildOptions = new BuildPlayerOptions
            {
                scenes = new[] { scenePath },
                locationPathName = projectBuildDir,
                target = BuildTarget.iOS,
                targetGroup = BuildTargetGroup.iOS,
                options = BuildOptions.CompressWithLz4HC
            };

            Debug.Log($"[IOSBuildHelper] Starting iOS export to: {projectBuildDir}...");
            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"<color=#00FF66><b>[IOSBuildHelper] iOS Xcode Project successfully generated at: {projectBuildDir}</b></color>");
            }
            else
            {
                Debug.LogError($"[IOSBuildHelper] iOS Build failed: {summary.result}, Errors: {summary.totalErrors}");
            }
        }
    }
}

