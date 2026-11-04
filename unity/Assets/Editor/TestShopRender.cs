using UnityEngine;
using UnityEditor;
using System.IO;

[InitializeOnLoad]
public static class TestShopRender
{
    private static bool s_isTesting = false;

    static TestShopRender()
    {
        EditorApplication.update -= CheckTestTrigger;
        EditorApplication.update += CheckTestTrigger;
    }

    private static void CheckTestTrigger()
    {
        if (s_isTesting) return;
        if (File.Exists("Temp/trigger_test_shop.txt"))
        {
            s_isTesting = true;
            try
            {
                File.Delete("Temp/trigger_test_shop.txt");
            }
            catch { }

            try
            {
                Test();
            }
            finally
            {
                s_isTesting = false;
            }
        }
    }

    [MenuItem("Tools/Import Shop Sprites")]
    public static void ImportShopSprites()
    {
        string[] searchDirs = new string[]
        {
            "Assets/Resources/UI/ShopDynamic",
            "Assets/Resources/UI/ShopDynamic/Zombie_Squad_Dynamic_Shop_Assets",
            "Assets/Resources/UI/Shop",
            "Assets/Resources/UI"
        };

        int count = 0;
        foreach (string dirPath in searchDirs)
        {
            if (!Directory.Exists(dirPath)) continue;

            var allFiles = new System.Collections.Generic.List<string>();
            allFiles.AddRange(Directory.GetFiles(dirPath, "*.png"));
            allFiles.AddRange(Directory.GetFiles(dirPath, "*.jpg"));
            foreach (string file in allFiles)
            {
                TextureImporter importer = AssetImporter.GetAtPath(file) as TextureImporter;
                if (importer != null)
                {
                    bool changed = false;
                    if (importer.textureType != TextureImporterType.Sprite)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        changed = true;
                    }
                    if (importer.spriteImportMode != SpriteImportMode.Single)
                    {
                        importer.spriteImportMode = SpriteImportMode.Single;
                        changed = true;
                    }
                    if (importer.mipmapEnabled)
                    {
                        importer.mipmapEnabled = false;
                        changed = true;
                    }
                    if (!importer.alphaIsTransparency)
                    {
                        importer.alphaIsTransparency = true;
                        changed = true;
                    }

                    if (changed)
                    {
                        EditorUtility.SetDirty(importer);
                        importer.SaveAndReimport();
                        count++;
                    }
                }
            }
        }
        AssetDatabase.Refresh();
        Debug.Log($"[TestShopRender] Successfully imported/updated {count} shop sprites!");
    }

    [MenuItem("Tools/Test Shop Render")]
    public static void Test()
    {
        ImportShopSprites();

        string scenePath = "Assets/q.unity";
        var currentScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        if (!currentScene.isLoaded || currentScene.path != scenePath)
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
        }

        string dir = "/Users/savvy/.gemini/antigravity/brain/721e22ee-cbeb-4801-880d-1a4e43268b8d/.system_generated/media";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "shop_screen_verification.png");
        if (File.Exists(path)) File.Delete(path);

        if (!EditorApplication.isPlaying)
        {
            MobControl.UI.ZombieRunnerUIManager.startAtShop = true;
            EditorApplication.isPlaying = true;

            int frames = 0;
            void OnUpdate()
            {
                if (!EditorApplication.isPlaying) return;
                frames++;
                if (frames == 15)
                {
                    ScreenCapture.CaptureScreenshot(path);
                    Debug.Log($"[SCREENSHOT] Shop screen captured to: {path}");
                }
                else if (frames > 30)
                {
                    EditorApplication.update -= OnUpdate;
                }
            }
            EditorApplication.update += OnUpdate;
        }
        else
        {
            var ui = Object.FindAnyObjectByType<MobControl.UI.ZombieRunnerUIManager>();
            if (ui != null)
            {
                ui.ShowZombieSquadShopScreen();

                int frames = 0;
                void OnUpdate()
                {
                    frames++;
                    if (frames == 10)
                    {
                        ScreenCapture.CaptureScreenshot(path);
                        Debug.Log($"[SCREENSHOT] Shop screen captured to: {path}");
                    }
                    else if (frames > 25)
                    {
                        EditorApplication.update -= OnUpdate;
                    }
                }
                EditorApplication.update += OnUpdate;
            }
        }
    }
}

