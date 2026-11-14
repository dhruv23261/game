using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Batch-mode helper: renders the responsive shop screen offscreen at several resolutions.
// Usage: Unity -batchmode -projectPath . -executeMethod ShopBatchRender.Run -quit
[InitializeOnLoad]
public static class ShopBatchRender
{
    static ShopBatchRender()
    {
        EditorApplication.update -= Poll;
        EditorApplication.update += Poll;
    }

    private static void Poll()
    {
        const string trigger = "Temp/trigger_shop_batch_render.txt";
        if (!File.Exists(trigger)) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode) return; // never touch a running game session
        try { File.Delete(trigger); } catch { }
        try { Run(); } catch (System.Exception e) { Debug.LogError("[ShopBatchRender] " + e); }
    }

    public static void Run()
    {
        AssetDatabase.Refresh();
        string outDir = System.Environment.GetEnvironmentVariable("SHOP_RENDER_OUT");
        if (string.IsNullOrEmpty(outDir)) outDir = "Temp/ShopRenders";
        Directory.CreateDirectory(outDir);

        Vector2Int[] sizes = { new Vector2Int(1080, 1920), new Vector2Int(1080, 2400), new Vector2Int(1536, 2048), new Vector2Int(720, 1280) };
        string[,] screens = { { "BuildZombieSquadShopScreenPanel", "shopPanel", "shop" }, { "BuildZombieSquadCharactersScreenPanel", "charactersPanel", "characters" }, { "BuildZombieSquadWeaponsScreenPanel", "weaponsPanel", "weapons" }, { "BuildZombieSquadDailyScreenPanel", "dailyPanel", "daily" }, { "BuildZombieSquadCharactersScreenPanel", "charactersPanel", "characters_ranger" }, { "BuildZombieSquadUpgradeScreenPanel", "upgradePanel", "upgrade" }, { "BuildZombieSquadLevelMapPanel", "levelMapPanel", "levelmap" }, { "BuildGameplayHudPanel", "gameplayHudPanel", "hud_boost" }, { "BuildGameplayHudPanel", "gameplayHudPanel", "hud_paused" } };
        for (int k = 0; k < screens.GetLength(0); k++)
            foreach (var sz in sizes) RenderAt(sz.x, sz.y, Path.Combine(outDir, $"{screens[k, 2]}_{sz.x}x{sz.y}.png"), screens[k, 0], screens[k, 1]);
        Debug.Log("[ShopBatchRender] done -> " + outDir);
    }

    private static void RenderAt(int w, int h, string path, string buildMethod, string panelFieldName)
    {
        var camGo = new GameObject("ShopRenderCam");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.cullingMask = 1 << 5; // UI layer only (no scene objects leaking into the capture)
        cam.orthographic = true;
        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;

        var canvasGo = new GameObject("ShopRenderCanvas");
        canvasGo.layer = 5;
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 5f;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = ((float)w / h > 1080f / 1920f) ? 1f : 0f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var mgrGo = new GameObject("ShopRenderMgr");
        var mgr = mgrGo.AddComponent<MobControl.UI.ZombieRunnerUIManager>();
        var build = typeof(MobControl.UI.ZombieRunnerUIManager).GetMethod(buildMethod, BindingFlags.Instance | BindingFlags.NonPublic);
        if (path.Contains("ranger"))
            typeof(MobControl.UI.ZombieRunnerUIManager).GetField("selectedCharacterKey", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(mgr, "Ranger");
        build.Invoke(mgr, new object[] { canvasGo.transform });
        if (path.Contains("hud_"))
        {
            var bb = (GameObject)typeof(MobControl.UI.ZombieRunnerUIManager).GetField("hudGunUpgradeBadgeObj", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(mgr);
            if (bb != null) bb.SetActive(true);
        }
        if (path.Contains("hud_paused"))
        {
            var pm = (GameObject)typeof(MobControl.UI.ZombieRunnerUIManager).GetField("pauseMenuPanel", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(mgr);
            if (pm != null) { pm.SetActive(true); pm.transform.SetAsLastSibling(); }
        }
        if (path.Contains("levelmap"))
            typeof(MobControl.UI.ZombieRunnerUIManager).GetMethod("UpdateLevelMapVisuals", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(mgr, null);
        var panelField = typeof(MobControl.UI.ZombieRunnerUIManager).GetField(panelFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        var panel = (GameObject)panelField.GetValue(mgr);
        panel.SetActive(true);
        foreach (var tr in canvasGo.GetComponentsInChildren<Transform>(true)) tr.gameObject.layer = 5;

        for (int i = 0; i < 3; i++)
        {
            Canvas.ForceUpdateCanvases();
            foreach (var f in canvasGo.GetComponentsInChildren<MobControl.UI.ShopResponsiveFitter>(true)) f.Apply(true);
            foreach (var r in canvasGo.GetComponentsInChildren<RectTransform>(true)) LayoutRebuilder.MarkLayoutForRebuild(r);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)canvasGo.transform);
            foreach (var t in canvasGo.GetComponentsInChildren<TMPro.TMP_Text>(true)) t.ForceMeshUpdate();
        }
        cam.Render();

        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        File.WriteAllBytes(path, tex.EncodeToPNG());

        Object.DestroyImmediate(canvasGo);
        Object.DestroyImmediate(mgrGo);
        Object.DestroyImmediate(camGo);
        rt.Release();
    }
}
