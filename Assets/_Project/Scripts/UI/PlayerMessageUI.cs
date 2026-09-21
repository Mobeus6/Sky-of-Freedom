using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkyOfFreedom.UI
{
    public class PlayerMessageUI : MonoBehaviour
    {
        private TMP_Text label;
        private CanvasGroup group;
        private float expires;

        public static void Show(Component owner, string message)
        {
            Canvas canvas = owner.GetComponentInParent<Canvas>();
            if (canvas == null) return;
            canvas = canvas.rootCanvas;
            PlayerMessageUI toast = canvas.GetComponentInChildren<PlayerMessageUI>(true);
            if (toast == null)
            {
                var obj = new GameObject("Player Message", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
                obj.layer = canvas.gameObject.layer;
                obj.transform.SetParent(canvas.transform, false);
                RectTransform rect = (RectTransform)obj.transform;
                rect.anchorMin = new Vector2(.2f, .8f);
                rect.anchorMax = new Vector2(.8f, .9f);
                rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
                Image background = obj.GetComponent<Image>();
                background.color = new Color(.1f, .14f, .17f, .97f);
                background.raycastTarget = false;
                toast = obj.AddComponent<PlayerMessageUI>();
                toast.group = obj.GetComponent<CanvasGroup>();
                toast.group.blocksRaycasts = false; toast.group.interactable = false;
                var textObject = new GameObject("Message", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObject.layer = obj.layer;
                textObject.transform.SetParent(obj.transform, false);
                RectTransform textRect = (RectTransform)textObject.transform;
                textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(16, 6); textRect.offsetMax = new Vector2(-16, -6);
                toast.label = textObject.GetComponent<TMP_Text>();
                TMP_Text existing = owner.GetComponentInChildren<TMP_Text>(true);
                toast.label.font = existing != null ? existing.font : TMP_Settings.defaultFontAsset;
                toast.label.color = Color.white;
                toast.label.alignment = TextAlignmentOptions.Center;
                toast.label.enableAutoSizing = true;
                toast.label.fontSizeMin = 12; toast.label.fontSizeMax = 28;
                toast.label.raycastTarget = false;
            }
            toast.transform.SetAsLastSibling();
            toast.gameObject.SetActive(true);
            toast.label.text = message;
            toast.group.alpha = 1;
            toast.expires = Time.unscaledTime + 3f;
        }

        private void Update()
        {
            float remaining = expires - Time.unscaledTime;
            group.alpha = Mathf.Clamp01(remaining / .3f);
            if (remaining <= 0) gameObject.SetActive(false);
        }
    }
}
