#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MobControl.Level;

namespace MobControl.EditorTools
{
    [InitializeOnLoad]
    public static class RoadGeneratorMenuItem
    {
        static RoadGeneratorMenuItem()
        {
            EditorApplication.delayCall += AutoFixRoadOnCompile;
        }

        private static void AutoFixRoadOnCompile()
        {
            if (Application.isPlaying) return;

            // Check if active scene has active old roads or is missing new road
            bool hasActiveOldRoad = false;
            GameObject[] allObjs = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var go in allObjs)
            {
                if (go == null) continue;
                if (go.name.StartsWith("NewRoad") || go.transform.root.name.StartsWith("NewRoad")) continue;

                string lower = go.name.ToLower().Trim();
                if (lower == "road" || lower.StartsWith("road ") || lower.StartsWith("road(") || lower.StartsWith("road_") || lower.Contains("road.obj"))
                {
                    if (go.activeSelf)
                    {
                        hasActiveOldRoad = true;
                        break;
                    }
                }
            }

            GameObject newRoadRoot = GameObject.Find("NewRoad_Level1_Root");
            if (hasActiveOldRoad || newRoadRoot == null)
            {
                GenerateRoad();
            }
        }

        [MenuItem("Zombie Squad/Replace Old Road With New Stylized Road")]
        [MenuItem("Zombie Squad/Generate Level 1 Road & Forest")]
        public static void GenerateRoad()
        {
            // 1. Purge & deactivate all legacy road objects
            NewRoadBuilder.DeactivateOldRoad();

            // 2. Build or recreate the complete Level 1 new road with trees, guardrails, banners
            GameObject builderObj = GameObject.Find("NewRoadBuilder_Manager");
            if (builderObj == null)
            {
                builderObj = new GameObject("NewRoadBuilder_Manager");
                Undo.RegisterCreatedObjectUndo(builderObj, "Create New Road Builder");
            }
            NewRoadBuilder builder = builderObj.GetComponent<NewRoadBuilder>() ?? builderObj.AddComponent<NewRoadBuilder>();
            builder.BuildRoad();

            GameObject newRoadRoot = GameObject.Find("NewRoad_Level1_Root");
            if (newRoadRoot != null)
            {
                EditorUtility.SetDirty(newRoadRoot);
            }

            var activeScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("[Zombie Squad] Level 1 New Stylized Road generated in Scene, old roads deactivated and scene saved!");
        }

        [MenuItem("Zombie Squad/Clear New Road")]
        public static void ClearRoad()
        {
            GameObject root = GameObject.Find("NewRoad_Level1_Root");
            if (root != null)
            {
                Undo.DestroyObjectImmediate(root);
                Debug.Log("[Zombie Squad] Cleared New Road.");
            }
        }

        [MenuItem("Zombie Squad/Deactivate All Old Roads")]
        public static void DeactivateOldRoadsMenu()
        {
            NewRoadBuilder.DeactivateOldRoad();
            var activeScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("[Zombie Squad] All legacy roads deactivated and scene saved.");
        }
    }
}
#endif
