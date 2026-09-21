using System;
using System.Threading.Tasks;
using SkyOfFreedom.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkyOfFreedom.UI
{
    // Independent overlay: available during loading without changing scene/prefab references.
    public sealed class SaveConflictPopupUI : MonoBehaviour
    {
        private TaskCompletionSource<bool> completion;
        private bool? choice;
        private Button confirm;
        private TMP_Text confirmation;

        public static Task<bool> ChooseAsync(Transform owner, PlayerData local, PlayerData cloud)
        {
            var root = new GameObject("Save Conflict", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(owner, false);
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            SaveConflictPopupUI popup = root.AddComponent<SaveConflictPopupUI>();
            popup.completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            popup.Build(local, cloud);
            return popup.completion.Task;
        }

        private void Build(PlayerData local, PlayerData cloud)
        {
            Box(transform, "Dim", Vector2.zero, Vector2.one, new Color(0, 0, 0, .85f));
            RectTransform safe = Box(transform, "Safe Area", Vector2.zero, Vector2.one, Color.clear);
            safe.gameObject.AddComponent<ScreenSafeAreaUI>();
            RectTransform panel = Box(safe, "Panel", new Vector2(.08f, .08f), new Vector2(.92f, .92f),
                new Color(.10f, .14f, .17f));
            Label(panel, "Choose your progress", .06f, .83f, .94f, .95f, 40);
            Label(panel, "This device and the cloud have different progress. Select one, then confirm.\n" +
                "Progress will not be merged. A recovery copy of both saves will be kept on this device.",
                .06f, .67f, .94f, .83f, 25);
            Option(panel, "THIS DEVICE", local, .05f, .48f, true);
            Option(panel, "CLOUD", cloud, .52f, .95f, false);
            confirm = Button(panel, "Confirm", .27f, .05f, .73f, .17f);
            confirmation = confirm.GetComponentInChildren<TMP_Text>();
            confirm.interactable = false;
            confirm.onClick.AddListener(() =>
            {
                if (!choice.HasValue) return;
                completion.TrySetResult(choice.Value);
                gameObject.SetActive(false);
                Destroy(gameObject);
            });
        }

        private void Option(Transform parent, string title, PlayerData data, float left, float right, bool local)
        {
            RectTransform area = Box(parent, title, new Vector2(left, .22f), new Vector2(right, .66f),
                new Color(.14f, .20f, .24f));
            Label(area, title, .05f, .80f, .95f, .98f, 28);
            string summary = data == null ? "No cloud save\nStart a new game" :
                $"Saved: {DisplayDate(data.Account?.LastSaveAtUtc)}\n" +
                $"Factory level: {data.Factory?.FactoryLevel ?? 1}\n" +
                $"Money: {data.Economy?.Money ?? 0:N0}\n" +
                $"Active contracts: {data.Contracts?.Active?.Count ?? 0}\n" +
                $"Production tasks: {data.Production?.Tasks?.Count ?? 0}";
            Label(area, summary, .06f, .27f, .94f, .79f, 25);
            Button select = Button(area, local ? "Use this device" : "Use cloud", .08f, .04f, .92f, .24f);
            select.onClick.AddListener(() =>
            {
                choice = local;
                confirmation.text = local ? "Confirm: use this device" : "Confirm: use cloud";
                confirm.interactable = true;
            });
        }

        private static string DisplayDate(string value)
        {
            return DateTime.TryParse(value, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind, out DateTime time)
                ? time.ToUniversalTime().ToString("yyyy-MM-dd HH:mm 'UTC'") : "Unknown";
        }

        private static RectTransform Box(Transform parent, string name, Vector2 min, Vector2 max, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = color;
            return rect;
        }

        private static TMP_Text Label(Transform parent, string value, float x0, float y0, float x1, float y1, int size)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TMP_Text text = go.GetComponent<TMP_Text>();
            text.rectTransform.anchorMin = new Vector2(x0, y0);
            text.rectTransform.anchorMax = new Vector2(x1, y1);
            text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
            text.text = value; text.color = Color.white;
            text.fontSize = size; text.enableAutoSizing = true;
            text.fontSizeMin = size * .75f; text.fontSizeMax = size;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        private static Button Button(Transform parent, string text, float x0, float y0, float x1, float y1)
        {
            RectTransform rect = Box(parent, text, new Vector2(x0, y0), new Vector2(x1, y1),
                new Color(.16f, .34f, .55f));
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            Label(rect, text, .03f, .05f, .97f, .95f, 26);
            return button;
        }

        private void OnDestroy()
        {
            completion?.TrySetCanceled();
        }
    }
}
