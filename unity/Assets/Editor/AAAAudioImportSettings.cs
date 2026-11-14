using UnityEditor;
using UnityEngine;

/// <summary>
/// Mobile-friendly import settings for Resources/Audio/AAA:
/// - music_*  : streamed from disk, Vorbis (low memory, no load hitch)
/// - jingles  : compressed in memory, Vorbis
/// - SFX      : decompressed on load (zero-latency playback for rapid gunfire), mono, ADPCM
/// </summary>
public class AAAAudioImportSettings : AssetPostprocessor
{
    private void OnPreprocessAudio()
    {
        if (!assetPath.Replace('\\', '/').Contains("Resources/Audio/AAA/")) return;

        var importer = (AudioImporter)assetImporter;
        string file = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        var s = importer.defaultSampleSettings;

        if (file.StartsWith("music_"))
        {
            importer.forceToMono = false;
            importer.loadInBackground = true;
            s.loadType = AudioClipLoadType.Streaming;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.6f;
        }
        else if (file.StartsWith("jingle_"))
        {
            importer.forceToMono = false;
            s.loadType = AudioClipLoadType.CompressedInMemory;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.7f;
        }
        else
        {
            importer.forceToMono = true;
            s.loadType = AudioClipLoadType.DecompressOnLoad;
            s.compressionFormat = AudioCompressionFormat.ADPCM;
            s.preloadAudioData = true;
        }

        importer.defaultSampleSettings = s;
    }
}
