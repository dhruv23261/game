#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MobControl.Level;

namespace MobControl.EditorTools
{
    public static class RoadGeneratorMenuItem
    {
        [MenuItem("Zombie Squad/Clean Level 1 Scene (Remove All Clutter)")]
        public static void CleanLevel1Scene()
        {
            // 1. Remove procedural road root if any
            GameObject root = GameObject.Find("NewRoad_Level1_Root");
            if (root != null)
            {
                Undo.DestroyObjectImmediate(root);
            }

            GameObject mgr = GameObject.Find("NewRoadBuilder_Manager");
            if (mgr != null)
            {
                Undo.DestroyObjectImmediate(mgr);
            }

            // 2. Remove any Gun_M41D objects
            RemoveGunM41DFromScene();

            // 3. Setup clean Camera
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.farClipPlane = 300.0f;
                mainCam.backgroundColor = new Color(0.42f, 0.65f, 0.88f, 1f);
                mainCam.clearFlags = CameraClearFlags.SolidColor;
                EditorUtility.SetDirty(mainCam);
            }

            var activeScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("[Zombie Squad] Level 1 cleaned successfully! All procedural clutter removed and scene saved.");
        }

        [MenuItem("Zombie Squad/Build Reference Level 1 Road (Image Match)")]
        public static void BuildReferenceLevel1Road()
        {
            CleanLevel1Scene();

            GameObject builderObj = new GameObject("Level1_RoadBuilder_Temp");
            NewRoadBuilder builder = builderObj.AddComponent<NewRoadBuilder>();
            builder.BuildRoadScene();
            Object.DestroyImmediate(builderObj);

            var activeScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("[Zombie Squad] Level 1 reference road generated and scene saved!");
        }

        public static void RemoveGunM41DFromScene()
        {
            GameObject[] all = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int count = 0;
            foreach (var go in all)
            {
                if (go != null && go.name.Contains("Gun_M41D"))
                {
                    Undo.DestroyObjectImmediate(go);
                    count++;
                }
            }
            if (count > 0)
            {
                Debug.Log($"[Zombie Squad] Removed {count} Gun_M41D object(s).");
            }
        }
    }
}
#endif
