using UnityEngine;

namespace MobControl.UI
{
    /// <summary>
    /// Uniformly scales a fixed design-size RectTransform so it always fits inside its parent
    /// (no stretching, no cropping) on every phone / tablet aspect ratio.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class ShopResponsiveFitter : MonoBehaviour
    {
        public Vector2 designSize = new Vector2(1000f, 1400f);
        public float maxScale = 1.25f;
        public float minScale = 0.35f;

        private RectTransform rt;
        private Vector2 lastParentSize = new Vector2(-1f, -1f);

        private void OnEnable()
        {
            rt = GetComponent<RectTransform>();
            Apply(true);
        }

        private void LateUpdate()
        {
            Apply(false);
        }

        private void OnRectTransformDimensionsChange()
        {
            Apply(true);
        }

        public void Apply(bool force)
        {
            if (rt == null) rt = GetComponent<RectTransform>();
            RectTransform parent = rt.parent as RectTransform;
            if (parent == null) return;

            Vector2 ps = parent.rect.size;
            if (!force && ps == lastParentSize) return;
            lastParentSize = ps;
            if (ps.x <= 1f || ps.y <= 1f) return;

            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = designSize;
            rt.anchoredPosition = Vector2.zero;

            float s = Mathf.Min(ps.x / designSize.x, ps.y / designSize.y);
            s = Mathf.Clamp(s, minScale, maxScale);
            rt.localScale = new Vector3(s, s, 1f);
        }
    }
}
