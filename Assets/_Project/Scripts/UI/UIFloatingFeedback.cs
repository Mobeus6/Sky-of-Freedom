using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkyOfFreedom.UI
{
    public sealed class UIFloatingFeedback : MonoBehaviour
    {
        private TMP_Text label;
        private RectTransform rect;
        private Vector3 origin;
        private float age;
        private Color tint;
        private string account;
        private RectTransform sourceTarget;
        private const float Lifetime = 1.1f;

        public static void Show(RectTransform target, TMP_Text style, string text, Color color)
        {
            if (target == null || !UIUnlockFeedback.Visible(target)) return;
            Canvas canvas = target.GetComponentInParent<Canvas>();
            if (canvas == null) return;
            var canvasRect = (RectTransform)canvas.rootCanvas.transform;
            // Reuse the live label for this target, avoiding a stack of unreadable numbers.
            UIFloatingFeedback effect = null;
            foreach (var candidate in canvas.rootCanvas.GetComponentsInChildren<UIFloatingFeedback>())
                if (candidate.sourceTarget == target) { effect = candidate; break; }
            if (effect == null)
            {
                var go = new GameObject("Floating Feedback", typeof(RectTransform),
                    typeof(LayoutElement), typeof(TextMeshProUGUI));
                go.layer = canvas.gameObject.layer;
                go.GetComponent<LayoutElement>().ignoreLayout = true;
                go.transform.SetParent(canvasRect, false);
                effect = go.AddComponent<UIFloatingFeedback>();
                effect.sourceTarget = target;
                effect.rect = (RectTransform)go.transform;
                effect.label = go.GetComponent<TMP_Text>();
                effect.label.raycastTarget = false;
                effect.label.richText = false;
                effect.label.alignment = TextAlignmentOptions.Center;
                effect.label.font = style != null ? style.font : TMP_Settings.defaultFontAsset;
                effect.label.fontSize = style != null ? Mathf.Clamp(style.fontSize, 18f, 32f) : 24f;
                effect.rect.anchorMin = effect.rect.anchorMax = canvasRect.pivot;
                effect.rect.sizeDelta = new Vector2(240f, 48f);
            }
            Vector3 point = canvasRect.InverseTransformPoint(target.TransformPoint(target.rect.center));
            point.x = Mathf.Clamp(point.x, canvasRect.rect.xMin + 125f, canvasRect.rect.xMax - 125f);
            point.y = Mathf.Clamp(point.y - 30f, canvasRect.rect.yMin + 28f, canvasRect.rect.yMax - 52f);
            effect.origin = point;
            effect.rect.localPosition = point;
            effect.label.text = text;
            effect.label.color = color;
            effect.tint = color;
            effect.age = 0f;
            effect.account = SkyOfFreedom.Managers.GameManager.Instance?.PublicId;
            effect.transform.SetAsLastSibling();
        }

        private void Update()
        {
            var game = SkyOfFreedom.Managers.GameManager.Instance;
            if (game == null || !game.IsGameReady || game.IsAccountTransition || game.PublicId != account)
            {
                Destroy(gameObject);
                return;
            }
            age += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(age / Lifetime);
            rect.localPosition = origin + Vector3.up * (18f * t);
            Color color = tint;
            color.a *= 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.4f, 1f, t));
            label.color = color;
            if (t >= 1f) Destroy(gameObject);
        }
    }
}
