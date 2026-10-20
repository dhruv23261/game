#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using MobControl.Level;

namespace MobControl.EditorTools
{
    public static class RoadGeneratorMenuItem
    {
        [MenuItem("Zombie Squad/Generate Level 1 Road & Forest")]
        public static void GenerateRoad()
        {
            NewRoadBuilder.EnsureNewRoad();
            EditorUtility.SetDirty(GameObject.Find("NewRoad_Level1_Root") ?? new GameObject());
            Debug.Log("[Zombie Squad] Level 1 Road, Pine Trees, Rocks, Start Line & Finish Arch generated in Scene!");
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
    }
}
#endif
