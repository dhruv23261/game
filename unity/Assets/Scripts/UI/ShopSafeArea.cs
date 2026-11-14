using UnityEngine;

namespace MobControl.UI
{
    /// <summary>
    /// Fits a RectTransform to Screen.safeArea so headers never hide under notches / punch-holes.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class ShopSafeArea : MonoBehaviour
    {
        private RectTransform rt;
        private Rect lastSafe = new Rect(0, 0, 0, 0);
        private Vector2Int lastScreen = Vector2Int.zero;

        private void OnEnable()
        {
            rt = GetComponent<RectTransform>();
            Apply(true);
        }

        private void Update()
        {
            Apply(false);
        }

        private void Apply(bool force)
        {
            Rect safe = Screen.safeArea;
            Vector2Int scr = new Vector2Int(Screen.width, Screen.height);
            if (!force && safe == lastSafe && scr == lastScreen) return;
            lastSafe = safe;
            lastScreen = scr;
            if (scr.x <= 0 || scr.y <= 0) return;

            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;
            min.x /= scr.x; min.y /= scr.y;
            max.x /= scr.x; max.y /= scr.y;

            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
