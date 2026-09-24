using UnityEngine;
using UnityEngine.UI;

namespace SkyOfFreedom.UI
{
    // A temporary, non-interactive overlay. Only its own RectTransform is configured.
    public sealed class UIActionFlash : MonoBehaviour
    {
        private Image image;
        private Color tint;
        private float elapsed;
        private float duration;

        public static void Show(RectTransform target, Color color, float seconds, Image source,
            bool detached = false)
        {
            if (target == null || (!detached && !target.gameObject.activeInHierarchy)) return;
            Canvas canvas = detached ? target.GetComponentInParent<Canvas>(true) : null;
            if (detached && canvas == null) return;
            UIActionFlash flash = null;
            for (int i = 0; !detached && i < target.childCount; i++)
            {
                if (target.GetChild(i).TryGetComponent(out flash)) break;
            }
            if (flash == null)
            {
                var overlay = new GameObject("Action Highlight", typeof(RectTransform),
                    typeof(LayoutElement), typeof(Image));
                overlay.layer = target.gameObject.layer;
                overlay.GetComponent<LayoutElement>().ignoreLayout = true;
                overlay.transform.SetParent(target, false);
                var rect = (RectTransform)overlay.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                if (detached)
                {
                    // Keep the confirmation visible even if the action rebuilds/hides its card.
                    var canvasRect = (RectTransform)canvas.rootCanvas.transform;
                    var corners = new Vector3[4];
                    target.GetWorldCorners(corners);
                    Vector3 min = canvasRect.InverseTransformPoint(corners[0]);
                    Vector3 max = min;
                    for (int i = 1; i < corners.Length; i++)
                    {
                        Vector3 point = canvasRect.InverseTransformPoint(corners[i]);
                        min = Vector3.Min(min, point);
                        max = Vector3.Max(max, point);
                    }
                    rect.SetParent(canvasRect, false);
                    rect.anchorMin = rect.anchorMax = canvasRect.pivot;
                    rect.sizeDelta = new Vector2(max.x - min.x, max.y - min.y);
                    rect.localPosition = (min + max) * 0.5f;
                }
                flash = overlay.AddComponent<UIActionFlash>();
                flash.image = overlay.GetComponent<Image>();
                flash.image.raycastTarget = false;
                if (source != null)
                {
                    flash.image.sprite = source.overrideSprite;
                    flash.image.type = source.type;
                    flash.image.preserveAspect = source.preserveAspect;
                    flash.image.fillCenter = source.fillCenter;
                    flash.image.fillMethod = source.fillMethod;
                    flash.image.fillOrigin = source.fillOrigin;
                    flash.image.fillClockwise = source.fillClockwise;
                    flash.image.fillAmount = source.fillAmount;
                    flash.image.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
                }
            }
            // A late pointer/submit event must not replace an already confirmed success.
            if (flash.duration > seconds && flash.elapsed < flash.duration) return;
            flash.transform.SetAsLastSibling();
            flash.tint = color;
            flash.duration = Mathf.Max(0.01f, seconds);
            flash.elapsed = 0f;
            flash.image.color = color;
        }

        private void Update()
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Color color = tint;
            color.a *= 1f - t * t * (3f - 2f * t);
            image.color = color;
            if (t >= 1f) Destroy(gameObject);
        }

        private void OnDisable()
        {
            // Do not replay an old highlight when a pooled card is reused.
            if (image != null) image.color = Color.clear;
            Destroy(gameObject);
        }
    }
}
