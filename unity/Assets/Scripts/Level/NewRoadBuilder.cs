using UnityEngine;

namespace MobControl.Level
{
    /// <summary>
    /// Level 1 road and environment manager.
    /// Procedural generation has been cleanly removed to ensure zero Z-fighting,
    /// zero flickering, and zero duplicate objects in Level 1.
    /// </summary>
    public class NewRoadBuilder : MonoBehaviour
    {
        private static NewRoadBuilder instance;
        public static NewRoadBuilder Instance => instance;

        private void Awake()
        {
            instance = this;
            CleanUpProceduralRoots();
        }

        public static void SafeDestroy(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }

        public static void EnsureNewRoad()
        {
            CleanUpProceduralRoots();
        }

        public static void DeactivateOldRoad()
        {
            // Do not deactivate the native road segments.
        }

        public static void CleanUpProceduralRoots()
        {
            GameObject root = GameObject.Find("NewRoad_Level1_Root");
            if (root != null) SafeDestroy(root);

            GameObject mgr = GameObject.Find("NewRoadBuilder_Manager");
            if (mgr != null && mgr != instance?.gameObject) SafeDestroy(mgr);
        }

        [ContextMenu("Build New Road In Scene")]
        public void BuildRoad()
        {
            CleanUpProceduralRoots();
        }

        [ContextMenu("Clear New Road")]
        public void ClearNewRoad()
        {
            CleanUpProceduralRoots();
        }
    }
}
