#if UNITY_EDITOR || DEVELOPMENT_BUILD
using SkyOfFreedom.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SkyOfFreedom.UI
{
    // Entire feature is excluded from release players. No scene/prefab edits required.
    public sealed class DeveloperZoneResetUI : MonoBehaviour
    {
        private GameObject panel;
        private Button entry;
        private Button confirm;
        private Button close;
        private TMP_Text message;
        private bool busy;
        private GameManager confirmedGame;
        private string confirmedId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var root = new GameObject("Developer Zone Reset", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(root);
            root.AddComponent<DeveloperZoneResetUI>();
        }

        private void Awake()
        {
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            var scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            entry = MakeButton(transform, "DEV", new Vector2(.46f, .01f), new Vector2(.54f, .065f));
            panel = Box(transform, "Reset confirmation", Vector2.zero, Vector2.one, new Color(.04f, .07f, .10f, .98f)).gameObject;
            message = Label(panel.transform, new Vector2(.1f, .30f), new Vector2(.9f, .88f));
            confirm = MakeButton(panel.transform, "RESET ALL ZONES TO LEVEL 1", new Vector2(.12f, .12f), new Vector2(.62f, .23f));
            close = MakeButton(panel.transform, "CLOSE", new Vector2(.67f, .12f), new Vector2(.88f, .23f));
            entry.onClick.AddListener(Open);
            close.onClick.AddListener(() => { if (!busy) panel.SetActive(false); });
            confirm.onClick.AddListener(ResetZones);
            panel.SetActive(false);
        }

        private void Update()
        {
            var game = GameManager.Instance;
            bool available = game != null && game.IsGameReady && !game.IsAccountTransition &&
                SceneManager.GetSceneByName("Game").isLoaded;
            entry.gameObject.SetActive(available && !panel.activeSelf);
            if (!available && !busy) panel.SetActive(false);
        }

        private void Open()
        {
            confirmedGame = GameManager.Instance;
            if (confirmedGame == null) return;
            confirmedId = confirmedGame.PublicId;
            panel.SetActive(true);
            string reason = confirmedGame.DeveloperResetBlockReason();
            message.text = "DEVELOPMENT BUILD — " + confirmedId + "\n\n" +
                "Production, Assembly, Research and Warehouse will become LEVEL 1.\n" +
                "Factory level, money, items, licenses and completed research remain unchanged. No refunds.\n\n" +
                "A backup is created before reset. Cloud confirmation is required.\n" +
                "Do not run this account on another device during testing.\n\n" + reason;
            confirm.interactable = string.IsNullOrEmpty(reason);
        }

        private async void ResetZones()
        {
            if (busy || confirmedGame == null || confirmedGame != GameManager.Instance || confirmedGame.PublicId != confirmedId) return;
            busy = true;
            confirm.interactable = close.interactable = false;
            message.text = "Creating backup, resetting zones and saving...\nKeep the game open and connected.";
            try
            {
                string result = await confirmedGame.DeveloperResetZonesAsync();
                if (this != null) message.text = result;
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                if (this != null) message.text = "Operation failed. Check save status before continuing.";
            }
            finally
            {
                if (this != null) { busy = false; close.interactable = true; }
            }
        }

        private static RectTransform Box(Transform parent, string name, Vector2 min, Vector2 max, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            var rect = (RectTransform)obj.transform;
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            obj.GetComponent<Image>().color = color;
            return rect;
        }

        private static TMP_Text Label(Transform parent, Vector2 min, Vector2 max)
        {
            var obj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            var rect = (RectTransform)obj.transform;
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var text = obj.GetComponent<TextMeshProUGUI>();
            text.fontSize = 30; text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        private static Button MakeButton(Transform parent, string title, Vector2 min, Vector2 max)
        {
            var rect = Box(parent, title, min, max, new Color(.12f, .32f, .55f));
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            Label(rect, Vector2.zero, Vector2.one).text = title;
            return button;
        }
    }
}
#endif
