using UnityEngine;
using UnityEngine.UI;

namespace SkyOfFreedom.UI
{
    // Fit existing top-level UI anchors into the safe area without reparenting.
    // Background cameras and full-screen loading overlays remain unchanged.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class ScreenSafeAreaUI : MonoBehaviour
    {
        private RectTransform rect;
        private Vector2 originalMin;
        private Vector2 originalMax;
        private Vector2 originalPosition;
        private Vector2 originalSize;
        private Rect previousSafeArea;
        private int previousWidth;
        private int previousHeight;
        private bool applied;
        private RectTransform fullScreenBackground;
        private RectTransform backgroundCanvas;
        private Image originalBackground;
        private readonly Vector3[] canvasCorners = new Vector3[4];

        // Opt-in only: add this component explicitly in the editor when safe-area fitting is desired.

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            originalMin = rect.anchorMin;
            originalMax = rect.anchorMax;
            originalPosition = rect.anchoredPosition;
            originalSize = rect.sizeDelta;
            Canvas canvas = GetComponentInParent<Canvas>();
            Image background = GetComponent<Image>();
            if (name.Trim() == "MainMenu Panel" && canvas != null && canvas.isRootCanvas &&
                canvas.name == "MainMenuCanvas" && background != null && background.enabled)
            {
                originalBackground = background;
                backgroundCanvas = canvas.transform as RectTransform;
                var child = new GameObject("Full Screen Menu Background", typeof(RectTransform), typeof(Image));
                child.layer = gameObject.layer;
                child.transform.SetParent(transform, false);
                child.transform.SetAsFirstSibling();
                fullScreenBackground = (RectTransform)child.transform;
                Image image = child.GetComponent<Image>();
                image.sprite = background.sprite;
                image.type = background.type;
                image.color = background.color;
                image.material = background.material;
                image.raycastTarget = false;
                background.enabled = false;
            }
        }

        private void OnEnable() { applied = false; Apply(); }
        private void LateUpdate() { Apply(); FitBackground(); }

        private void Apply()
        {
            if (rect == null || Screen.width <= 0 || Screen.height <= 0) return;
            Rect safe = Screen.safeArea;
            if (safe.width <= 0 || safe.height <= 0) return;
            if (applied && previousSafeArea == safe && previousWidth == Screen.width && previousHeight == Screen.height)
                return;
            previousSafeArea = safe;
            previousWidth = Screen.width;
            previousHeight = Screen.height;
            applied = true;
            Vector2 min = new Vector2(Mathf.Clamp01(safe.xMin / Screen.width), Mathf.Clamp01(safe.yMin / Screen.height));
            Vector2 max = new Vector2(Mathf.Clamp01(safe.xMax / Screen.width), Mathf.Clamp01(safe.yMax / Screen.height));
            Vector2 size = max - min;
            rect.anchorMin = min + Vector2.Scale(originalMin, size);
            rect.anchorMax = min + Vector2.Scale(originalMax, size);
            rect.anchoredPosition = originalPosition;
            rect.sizeDelta = originalSize;
            FitBackground();
        }

        private void FitBackground()
        {
            if (fullScreenBackground == null || backgroundCanvas == null) return;
            backgroundCanvas.GetWorldCorners(canvasCorners);
            Vector3 min = rect.InverseTransformPoint(canvasCorners[0]);
            Vector3 max = rect.InverseTransformPoint(canvasCorners[2]);
            fullScreenBackground.anchorMin = fullScreenBackground.anchorMax = Vector2.zero;
            fullScreenBackground.pivot = Vector2.zero;
            fullScreenBackground.anchoredPosition = (Vector2)min - rect.rect.min;
            fullScreenBackground.sizeDelta = max - min;
        }

        private void OnDestroy()
        {
            if (originalBackground != null) originalBackground.enabled = true;
            if (fullScreenBackground != null)
            {
                if (Application.isPlaying) Destroy(fullScreenBackground.gameObject);
                else DestroyImmediate(fullScreenBackground.gameObject);
            }
        }

        private void OnDisable()
        {
            if (rect == null) return;
            rect.anchorMin = originalMin;
            rect.anchorMax = originalMax;
            rect.anchoredPosition = originalPosition;
            rect.sizeDelta = originalSize;
            applied = false;
            FitBackground();
        }
    }
}
