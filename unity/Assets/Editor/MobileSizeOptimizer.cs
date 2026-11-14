using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-shot tool that shrinks the Android build without touching Editor/Standalone quality.
///  - Textures: Android override with ASTC compression.
///      UI / sprites           -> ASTC 4x4 (crisp, ~4x smaller than RGBA32), size kept (max 2048)
///      3D maps (normal, roughness, metallic, albedo of models) -> ASTC 6x6, max 1024
///  - Audio (outside Resources/Audio/AAA, which has its own rules):
///      long clips (music)  -> Streaming, Vorbis q0.45
///      short clips (SFX)   -> Compressed in memory, Vorbis q0.6, mono
/// Run from: Tools/Optimize/Apply Mobile Size Settings
/// or by creating the file Temp/trigger_optimize_size.txt while the editor is open.
/// </summary>
[InitializeOnLoad]
public static class MobileSizeOptimizer
{
    private const string Trigger = "Temp/trigger_optimize_size.txt";
    private const string Android = "Android";

    static MobileSizeOptimizer()
    {
        EditorApplication.update -= Watch;
        EditorApplication.update += Watch;
    }

    private static void Watch()
    {
        if (!File.Exists(Trigger)) return;
        try { File.Delete(Trigger); } catch { }
        Apply();
    }

    [MenuItem("Tools/Optimize/Apply Mobile Size Settings")]
    public static void Apply()
    {
        int tex = 0, aud = 0;
        try
        {
            AssetDatabase.StartAssetEditing();
            string[] texGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });
            for (int i = 0; i < texGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(texGuids[i]);
                if (Skip(path)) continue;
                EditorUtility.DisplayProgressBar("Mobile Size Optimizer", path, (float)i / texGuids.Length);
                if (OptimizeTexture(path)) tex++;
            }

            string[] audGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets" });
            foreach (string g in audGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                if (Skip(path) || path.Replace('\\', '/').Contains("Resources/Audio/AAA/")) continue;
                if (OptimizeAudio(path)) aud++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
        }
        AssetDatabase.Refresh();
        Debug.Log($"<color=#00FF66><b>[MobileSizeOptimizer] Done. Textures updated: {tex}, audio clips updated: {aud}.</b></color>");
    }

    private static bool Skip(string path)
    {
        return !path.StartsWith("Assets/")
            || path.StartsWith("Assets/_UnusedAssets/")
            || path.StartsWith("Assets/TextMesh Pro/")
            || path.Contains("/Editor/")
            || path.Contains("/Gizmos/");
    }

    private static bool IsUi(string path, TextureImporter ti)
    {
        string p = path.Replace('\\', '/');
        return ti.textureType == TextureImporterType.Sprite
            || ti.textureType == TextureImporterType.GUI
            || p.Contains("/UI/")
            || p.Contains("/Textures/") && Path.GetFileName(p).Contains("Badge");
    }

    private static bool OptimizeTexture(string path)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return false;

        bool ui = IsUi(path, ti);
        TextureImporterPlatformSettings def = ti.GetDefaultPlatformTextureSettings();
        TextureImporterPlatformSettings a = ti.GetPlatformTextureSettings(Android);

        int maxSize = ui ? Mathf.Min(def.maxTextureSize, 2048) : Mathf.Min(def.maxTextureSize, 1024);
        TextureImporterFormat fmt = ui ? TextureImporterFormat.ASTC_4x4 : TextureImporterFormat.ASTC_6x6;
        if (ti.textureType == TextureImporterType.Lightmap || ti.textureType == TextureImporterType.SingleChannel)
            fmt = TextureImporterFormat.ASTC_6x6;

        if (a.overridden && a.maxTextureSize == maxSize && a.format == fmt) return false;

        a.overridden = true;
        a.maxTextureSize = maxSize;
        a.format = fmt;
        a.compressionQuality = 50;
        a.resizeAlgorithm = TextureResizeAlgorithm.Mitchell;
        ti.SetPlatformTextureSettings(a);

        // Mipmaps waste ~33% on 2D UI that is never minified in 3D.
        if (ui && ti.mipmapEnabled && ti.textureType == TextureImporterType.Sprite) ti.mipmapEnabled = false;

        ti.SaveAndReimport();
        return true;
    }

    private static bool OptimizeAudio(string path)
    {
        var ai = AssetImporter.GetAtPath(path) as AudioImporter;
        if (ai == null) return false;
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        bool longClip = clip != null ? clip.length > 12f : new FileInfo(path).Length > 1024 * 1024;

        AudioImporterSampleSettings s = ai.defaultSampleSettings;
        s.compressionFormat = AudioCompressionFormat.Vorbis;
        if (longClip)
        {
            s.loadType = AudioClipLoadType.Streaming;
            s.quality = 0.45f;
            ai.loadInBackground = true;
        }
        else
        {
            s.loadType = AudioClipLoadType.CompressedInMemory;
            s.quality = 0.6f;
            ai.forceToMono = true;
        }
        s.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
        ai.defaultSampleSettings = s;
        ai.SaveAndReimport();
        return true;
    }
}
