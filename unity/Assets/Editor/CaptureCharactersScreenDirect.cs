using UnityEngine;
using UnityEditor;
using System.IO;

public static class CaptureCharactersScreenDirect
{
    [MenuItem("Zombie Squad/Capture Ranger Characters Screen")]
    public static void CaptureRanger()
    {
        CaptureHero("Ranger");
    }

    [MenuItem("Zombie Squad/Capture Heavy Characters Screen")]
    public static void CaptureHeavy()
    {
        CaptureHero("Heavy");
    }

    [MenuItem("Zombie Squad/Capture Soldier Characters Screen")]
    public static void CaptureSoldier()
    {
        CaptureHero("Soldier");
    }

    [MenuItem("Zombie Squad/Capture Powerup HUD Screenshot")]
    public static void CapturePowerUpHud()
    {
        string dir = "/Users/savvy/.gemini/antigravity-ide/brain/c4840675-c01c-4015-b0af-1b83673d3587/.tempmediaStorage";
        string path = Path.Combine(dir, "gameplay_powerup_hud.png");
        if (File.Exists(path)) File.Delete(path);

        ScreenCapture.CaptureScreenshot(path);
        Debug.Log($"[CAPTURE_VERIFIED] ScreenCapture invoked for HUD: {path}");
    }

    private static void CaptureHero(string heroKey)
    {
        var ui = Object.FindAnyObjectByType<MobControl.UI.ZombieRunnerUIManager>();
        if (ui == null)
        {
            Debug.LogError("ZombieRunnerUIManager not found!");
            return;
        }

        ui.RebuildZombieSquadCharactersScreen();
        ui.ShowZombieSquadCharactersScreen();
        ui.SelectCharacter(heroKey);

        string dir = "/Users/savvy/.gemini/antigravity-ide/brain/c4840675-c01c-4015-b0af-1b83673d3587/.tempmediaStorage";
        string path = Path.Combine(dir, "characters_screen_verification.png");
        if (File.Exists(path)) File.Delete(path);

        int frames = 0;
        void OnUpdate()
        {
            frames++;
            if (frames == 5)
            {
                ScreenCapture.CaptureScreenshot(path);
            }
            else if (frames > 25)
            {
                EditorApplication.update -= OnUpdate;
                Debug.Log($"[CAPTURE_VERIFIED] Done capture for {heroKey}, file exists={File.Exists(path)}");
            }
        }
        EditorApplication.update += OnUpdate;
    }
}
