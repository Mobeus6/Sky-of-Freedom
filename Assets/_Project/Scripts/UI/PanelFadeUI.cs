using UnityEngine;

namespace SkyOfFreedom.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class PanelFadeUI : MonoBehaviour
    {
        private CanvasGroup group;
        private float showDuration = .18f;
        private float hideDuration = .12f;
        private float startAlpha;
        private float targetAlpha;
        private float elapsed;
        private float duration;
        private bool moving;

        public void Configure(float show, float hide)
        {
            showDuration = Mathf.Max(0f, show);
            hideDuration = Mathf.Max(0f, hide);
        }

        public void SetVisible(bool visible, bool animate = true)
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            float target = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
            if (moving && targetAlpha == target && animate) return;
            targetAlpha = target;
            startAlpha = group.alpha;
            elapsed = 0f;
            duration = (visible ? showDuration : hideDuration) * Mathf.Abs(target - startAlpha);
            moving = animate && isActiveAndEnabled && duration > 0f;
            if (!moving) group.alpha = target;
        }

        private void Update()
        {
            if (!moving) return;
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            group.alpha = Mathf.Lerp(startAlpha, targetAlpha, t * t * (3f - 2f * t));
            if (t >= 1f) moving = false;
        }

        private void OnDisable()
        {
            if (!moving || group == null) return;
            group.alpha = targetAlpha;
            moving = false;
        }
    }
}
