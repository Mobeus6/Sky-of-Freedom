using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkyOfFreedom.UI
{
    // Reuses the existing Count background and TMP label; no prefab rewiring required.
    public static class QuantityInputUI
    {
        public static TMP_InputField Create(TMP_Text label)
        {
            if (label == null || !(label is TextMeshProUGUI text)) return null;
            RectTransform container = text.transform.parent as RectTransform;
            if (container == null) return null;
            TMP_InputField input = container.GetComponent<TMP_InputField>();
            if (input == null) input = container.gameObject.AddComponent<TMP_InputField>();
            Image background = container.GetComponent<Image>();
            if (background == null) background = container.gameObject.AddComponent<Image>();
            background.raycastTarget = true;
            Button oldButton = container.GetComponent<Button>();
            if (oldButton != null) oldButton.enabled = false;
            input.targetGraphic = background;
            input.textViewport = container;
            input.textComponent = text;
            input.contentType = TMP_InputField.ContentType.IntegerNumber;
            input.characterValidation = TMP_InputField.CharacterValidation.Digit;
            input.keyboardType = TouchScreenKeyboardType.NumberPad;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.characterLimit = 10;
            input.richText = false;
            text.richText = false;
            text.raycastTarget = true;
            return input;
        }

        public static int Parse(string text, int fallback, int minimum, int maximum)
        {
            maximum = Mathf.Max(minimum, maximum);
            return int.TryParse(text?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int value)
                ? Mathf.Clamp(value, minimum, maximum) : Mathf.Clamp(fallback, minimum, maximum);
        }

        public static void Show(TMP_InputField input, TMP_Text label, int value)
        {
            if (input != null)
            {
                if (!input.isFocused) input.SetTextWithoutNotify(value.ToString(CultureInfo.InvariantCulture));
            }
            else if (label != null) label.text = value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
