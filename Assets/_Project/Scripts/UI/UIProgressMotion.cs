using UnityEngine;
using UnityEngine.UI;

namespace SkyOfFreedom.UI
{
    [DisallowMultipleComponent]
    public sealed class UIProgressMotion : MonoBehaviour
    {
        private Slider slider;
        private Image image;
        private object identity;
        private float target;
        private float shown;
        private bool initialized;

        public static void Set(Slider view, float value, object key, bool animate = true)
        {
            if (view == null) return;
            var motion = Get(view.gameObject);
            motion.slider = view;
            motion.SetTarget(value, key, animate);
        }

        public static void Set(Image view, float value, object key, bool animate = true)
        {
            if (view == null) return;
            var motion = Get(view.gameObject);
            motion.image = view;
            motion.SetTarget(value, key, animate);
        }

        private static UIProgressMotion Get(GameObject owner)
        {
            if (!owner.TryGetComponent<UIProgressMotion>(out var motion))
                motion = owner.AddComponent<UIProgressMotion>();
            return motion;
        }

        private void SetTarget(float value, object key, bool animate)
        {
            value = Mathf.Clamp01(value);
            bool reset = !initialized || !Equals(identity, key) || value < target ||
                !animate || !isActiveAndEnabled;
            identity = key;
            target = value;
            initialized = true;
            if (reset || value >= 1f)
            {
                shown = value;
                Apply();
            }
        }

        private void Update()
        {
            if (!initialized) return;
            shown = Mathf.Lerp(shown, target, 1f - Mathf.Exp(-Time.unscaledDeltaTime / 0.12f));
            if (Mathf.Abs(shown - target) < 0.0001f) shown = target;
            Apply();
        }

        private void Apply()
        {
            if (slider != null) slider.SetValueWithoutNotify(Mathf.Lerp(slider.minValue, slider.maxValue, shown));
            if (image != null) image.fillAmount = shown;
        }

        private void OnDisable() { initialized = false; }
    }
}
