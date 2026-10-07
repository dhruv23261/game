using UnityEngine;
using UnityEditor;
using TMPro;

namespace MobControl.EditorTools
{
    public class FontAssetCreator : MonoBehaviour
    {
        [MenuItem("Tools/Generate Joyful Font Assets")]
        public static void GenerateFontAssets()
        {
            string[] fontGuids = AssetDatabase.FindAssets("t:Font", new[] { "Assets/Fonts" });

            if (fontGuids.Length == 0)
            {
                Debug.LogWarning("[FontAssetCreator] No fonts found in Assets/Fonts.");
                return;
            }

            foreach (string guid in fontGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(path);

                if (sourceFont != null)
                {
                    string assetPath = path.Replace(".ttf", " SDF.asset").Replace(".otf", " SDF.asset");
                    
                    // Check if already created
                    TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
                    if (existing != null)
                    {
                        Debug.Log($"[FontAssetCreator] Font Asset already exists at: {assetPath}");
                        continue;
                    }

                    TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont);
                    if (fontAsset != null)
                    {
                        AssetDatabase.CreateAsset(fontAsset, assetPath);
                        AssetDatabase.SaveAssets();
                        Debug.Log($"[FontAssetCreator] Successfully created SDF Font Asset: {assetPath}");
                    }
                }
            }

            AssetDatabase.Refresh();
            Debug.Log("[FontAssetCreator] All Joyful Font Assets generated successfully!");
        }
    }
}
