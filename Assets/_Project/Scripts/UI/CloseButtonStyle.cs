using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SkyOfFreedom.UI
{
    public static class CloseButtonStyle
    {
        private const string GlyphName = "Unified Close Glyph";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Button button in root.GetComponentsInChildren<Button>(true))
                    if (button.name == "Exit Button" || button.name == "Close Button" ||
                        button.name == "Cancel Button")
                        Apply(button);
        }

        public static void Apply(Button button)
        {
            if (button == null || button.transform.Find(GlyphName) != null) return;
            Image background = button.GetComponent<Image>();
            if (background == null) background = button.gameObject.AddComponent<Image>();
            background.enabled = true;
            background.sprite = null;
            background.color = Color.white;
            background.raycastTarget = true;
            button.targetGraphic = background;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.35f, 0.18f, 0.20f);
            colors.highlightedColor = new Color(0.48f, 0.24f, 0.27f);
            colors.selectedColor = colors.normalColor;
            colors.pressedColor = new Color(0.25f, 0.12f, 0.14f);
            colors.disabledColor = new Color(0.24f, 0.23f, 0.24f, 0.6f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            button.colors = colors;

            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
            foreach (Graphic graphic in button.GetComponentsInChildren<Graphic>(true))
            {
                if (graphic == background) continue;
                // Keep explanatory captions such as Cancel below the button.
                if (graphic is TMP_Text label && label.text.Trim().ToLowerInvariant() == "cancel")
                    continue;
                if (graphic is TMP_Text text && text.font != null) font = text.font;
                graphic.enabled = false;
            }
            var glyph = new GameObject(GlyphName, typeof(RectTransform), typeof(TextMeshProUGUI));
            glyph.layer = button.gameObject.layer;
            glyph.transform.SetParent(button.transform, false);
            var rect = (RectTransform)glyph.transform;
            rect.anchorMin = new Vector2(0.18f, 0.12f);
            rect.anchorMax = new Vector2(0.82f, 0.88f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var caption = glyph.GetComponent<TextMeshProUGUI>();
            caption.font = font;
            caption.text = "X";
            caption.color = new Color(0.95f, 0.97f, 0.98f);
            caption.alignment = TextAlignmentOptions.Center;
            caption.enableAutoSizing = true;
            caption.fontSizeMin = 8;
            caption.fontSizeMax = 128;
            caption.raycastTarget = false;
        }
    }
}
