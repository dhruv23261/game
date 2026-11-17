using UnityEngine;

namespace MobControl.Level
{
    /// <summary>
    /// EDITOR ONLY: while Level 1 runs in Play mode, saves a Game-view screenshot every 3 s
    /// (Captures/level1_live_00.png …, overwritten each play) plus one stats line per shot in
    /// Captures/level1_live.txt — distance, army, swarm front, road speed. Lets the level be tuned from real frames.
    /// Turn off with ArmyRunLevel.EditorCapture = false. Does nothing in builds.
    /// </summary>
    public partial class ArmyRunLevel
    {
        public static bool EditorCapture = true;
        private const float CAPTURE_EVERY = 5f;
        private const int CAPTURE_MAX = 40;
        private float captureTimer;
        private int captureIndex;

        private void ResetEditorCapture()
        {
            captureTimer = 0.5f;
            captureIndex = 0;
#if UNITY_EDITOR
            if (!EditorCapture) return;
            try
            {
                string dir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "Captures"));
                System.IO.Directory.CreateDirectory(dir);
                foreach (var f in System.IO.Directory.GetFiles(dir, "level1_live_*.*")) if (!f.EndsWith(".txt")) System.IO.File.Delete(f);
                System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "level1_live.txt"),
                    $"# Level 1 capture {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}  screen {Screen.width}x{Screen.height}\n");
            }
            catch (System.Exception e) { Debug.LogWarning("[ArmyRun] capture setup failed: " + e.Message); }
#endif
        }

        private void EditorCaptureTick()
        {
#if UNITY_EDITOR
            if (!EditorCapture || !running || captureIndex >= CAPTURE_MAX) return;
            captureTimer -= Time.unscaledDeltaTime;
            if (captureTimer > 0f) return;
            captureTimer = CAPTURE_EVERY;
            try
            {
                string dir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "Captures"));
                string png = System.IO.Path.Combine(dir, $"level1_live_{captureIndex:00}.jpg");
                StartCoroutine(GrabFrame(png));      // the ScreenCapture module is disabled in this project
                int alive = 0;
                for (int i = 0; i < mobCount; i++) if (mobs[i].state < 3) alive++;
                System.IO.File.AppendAllText(System.IO.Path.Combine(dir, "level1_live.txt"),
                    $"{captureIndex:00}  t={Time.timeSinceLevelLoad:F1}s  run={travelled:F1}m  stage={stageIndex + 1}  army={soldierCount}  " +
                    $"leaderX={LeaderPos.x:F2}  swarmFront={(float.IsInfinity(swarmFront) ? -1f : swarmFront):F1}m  speed={scrollSpeedNow:F2}  " +
                    $"mobsAlive={alive}  killed={swarmKilledTotal}  plaques={plaquesCollected}  arena={arenaActive}\n");
                captureIndex++;
            }
            catch (System.Exception e) { Debug.LogWarning("[ArmyRun] capture failed: " + e.Message); captureIndex = CAPTURE_MAX; }
#endif
        }

#if UNITY_EDITOR
        /// <summary>Reads the finished frame (3D + UI overlay) and writes it as PNG.</summary>
        private System.Collections.IEnumerator GrabFrame(string path)
        {
            yield return new WaitForEndOfFrame();
            int w = Screen.width, h = Screen.height;
            if (w < 8 || h < 8) yield break;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply(false);
            byte[] bytes = tex.EncodeToJPG(80);           // ~5× faster than PNG: no visible hitch
            Destroy(tex);
            try { System.IO.File.WriteAllBytes(path, bytes); }
            catch (System.Exception e) { Debug.LogWarning("[ArmyRun] capture write failed: " + e.Message); }
        }
#endif
    }
}
