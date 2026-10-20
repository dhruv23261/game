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
            GameObject root = GameObject.Find(NewRoadBuilder.ROOT_NAME);
            if (root != null)
            {
                Undo.DestroyObjectImmediate(root);
            }

            GameObject oldRoot = GameObject.Find("NewRoad_Level1_Root");
            if (oldRoot != null)
            {
                Undo.DestroyObjectImmediate(oldRoot);
            }

            // Remove any in-world 3D TextMeshPro floating labels
            foreach (var txt in Object.FindObjectsByType<TMPro.TextMeshPro>(FindObjectsInactive.Include))
            {
                if (txt != null && txt.GetComponentInParent<Canvas>() == null)
                {
                    Undo.DestroyObjectImmediate(txt.gameObject);
                }
            }

            // 2. Remove any Gun_M41D objects
            RemoveGunM41DFromScene();

            // 3. Setup clean Camera and Atmosphere matching reference image exactly
            NewRoadBuilder.SetupCameraAndAtmosphere();

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
