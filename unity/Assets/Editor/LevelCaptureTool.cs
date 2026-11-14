#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MobControl.EditorTools
{
    /// <summary>
    /// Plays a level in the editor (works in -batchmode too) and saves phone-sized screenshots
    /// of the gameplay camera + HUD at fixed times. Used to review level visuals without a device.
    ///   Unity -batchmode -projectPath unity -executeMethod MobControl.EditorTools.LevelCaptureTool.CaptureFromCommandLine -captureLevel 2 -captureDir /path
    /// </summary>
    [InitializeOnLoad]
    public static class LevelCaptureTool
    {
        private const string KEY_ACTIVE = "LevelCapture_Active";
        private const string KEY_LEVEL = "LevelCapture_Level";
        private const string KEY_DIR = "LevelCapture_Dir";
        private const string KEY_QUIT = "LevelCapture_Quit";

        private static readonly float[] ShotTimes = { 0.5f, 6f, 14f, 24f, 36f, 50f };
        private const float BootDelay = 2.5f;

        private static double startTime;
        private static int stage;
        private static int shotIndex;
        private static double missionStartTime;

        static LevelCaptureTool()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            if (SessionState.GetBool(KEY_ACTIVE, false) && EditorApplication.isPlaying) Begin();
        }

        [MenuItem("Zombie Squad/Debug/Capture Level 2 Screenshots")]
        public static void CaptureLevel2Menu() => Start(2, Path.GetFullPath(Path.Combine(Application.dataPath, "../Captures")), false);

        [MenuItem("Zombie Squad/Debug/Capture Level 1 Screenshots")]
        public static void CaptureLevel1Menu() => Start(1, Path.GetFullPath(Path.Combine(Application.dataPath, "../Captures")), false);

        public static void CaptureFromCommandLine()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            int level = 2;
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Captures"));
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-captureLevel") int.TryParse(args[i + 1], out level);
                if (args[i] == "-captureDir") dir = args[i + 1];
            }
            if (level == 2) Level2AssetSetup.SetupAllLevel2Assets();
            Start(level, dir, true);
        }

        private static void Start(int level, string dir, bool quit)
        {
            Directory.CreateDirectory(dir);
            SessionState.SetBool(KEY_ACTIVE, true);
            SessionState.SetInt(KEY_LEVEL, level);
            SessionState.SetString(KEY_DIR, dir);
            SessionState.SetBool(KEY_QUIT, quit);
            if (!EditorApplication.isPlaying)
            {
                EditorSceneManager.OpenScene("Assets/q.unity");
                EditorApplication.EnterPlaymode();
            }
            else Begin();
        }

        private static void OnPlayModeChanged(PlayModeStateChange s)
        {
            if (s == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(KEY_ACTIVE, false)) Begin();
        }

        private static void Begin()
        {
            startTime = EditorApplication.timeSinceStartup;
            stage = 0;
            shotIndex = 0;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) { EditorApplication.update -= Tick; return; }
            double t = EditorApplication.timeSinceStartup - startTime;
            int level = SessionState.GetInt(KEY_LEVEL, 2);
            var ui = MobControl.UI.ZombieRunnerUIManager.Instance;

            if (stage == 0 && t > BootDelay && ui != null)
            {
                ui.StartMission(level);
                stage = 1;
                Shot($"L{level}_00_ready");
            }
            else if (stage == 1 && t > BootDelay + 1.5f)
            {
                ui.StartGameplayFromWaiting();
                missionStartTime = EditorApplication.timeSinceStartup;
                stage = 2;
            }
            else if (stage == 2)
            {
                double m = EditorApplication.timeSinceStartup - missionStartTime;
                if (shotIndex < ShotTimes.Length && m >= ShotTimes[shotIndex])
                {
                    Shot($"L{level}_{shotIndex + 1:00}_t{ShotTimes[shotIndex]:0}");
                    shotIndex++;
                }
                if (shotIndex >= ShotTimes.Length) Finish();
            }

            if (t > 240) Finish();
        }

        private static void Finish()
        {
            EditorApplication.update -= Tick;
            SessionState.SetBool(KEY_ACTIVE, false);
            Debug.Log("[LevelCapture] Done -> " + SessionState.GetString(KEY_DIR, ""));
            bool quit = SessionState.GetBool(KEY_QUIT, false);
            EditorApplication.ExitPlaymode();
            if (quit) EditorApplication.delayCall += () => EditorApplication.Exit(0);
        }

        /// <summary>Renders the main camera plus every overlay canvas into a 1080x1920 PNG.</summary>
        public static void Shot(string name)
        {
            Camera cam = Camera.main;
            if (cam == null) { Debug.LogWarning("[LevelCapture] No main camera"); return; }
            const int W = 1080, H = 1920;
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };

            var changed = new List<(Canvas c, RenderMode mode, Camera cam, float dist)>();
            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!c.isRootCanvas || c.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                changed.Add((c, c.renderMode, c.worldCamera, c.planeDistance));
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = cam;
                c.planeDistance = cam.nearClipPlane + 0.05f;
            }
            Canvas.ForceUpdateCanvases();

            var prevTarget = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = prevTarget;

            foreach (var e in changed)
            {
                e.c.renderMode = e.mode;
                e.c.worldCamera = e.cam;
                e.c.planeDistance = e.dist;
            }

            var prevActive = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            RenderTexture.active = prevActive;

            string dir = SessionState.GetString(KEY_DIR, Path.GetFullPath(Path.Combine(Application.dataPath, "../Captures")));
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, name + ".png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
            Debug.Log("[LevelCapture] Saved " + path);
        }
    }
}
#endif
