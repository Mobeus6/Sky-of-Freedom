using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using LocalAudioSettings = SkyOfFreedom.Services.AudioSettings;

namespace SkyOfFreedom.UI
{
    public class AudioSettingsPanelUI : MonoBehaviour
    {
        private GameObject audioPanel;
        private Transform content;
        private Button audioButton;
        private Button generalButton;
        private Button graphicsButton;
        private Slider music;
        private Slider effects;
        private TMP_Text musicValue;
        private TMP_Text effectsValue;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
                {
                    if (node.name == "Settings Panel" && node.GetComponent<AudioSettingsPanelUI>() == null)
                        node.gameObject.AddComponent<AudioSettingsPanelUI>();
                    if (node.name.Trim() == "Settings" && node.GetComponent<MenuButton>() != null)
                        node.gameObject.SetActive(true);
                }
            }
        }

        private void Start()
        {
            content = transform.Find("Scroll View/Viewport/Content");
            audioButton = FindButton("Category Buttons/Audio Button");
            generalButton = FindButton("Category Buttons/General Button");
            graphicsButton = FindButton("Category Buttons/Graphic Button ");
            if (content == null || audioButton == null)
            {
                Debug.LogWarning("Audio settings: expected Settings Panel hierarchy was not found.", this);
                return;
            }
            TMP_Text existing = GetComponentInChildren<TMP_Text>(true);
            TMP_FontAsset font = existing != null ? existing.font : TMP_Settings.defaultFontAsset;
            audioPanel = new GameObject("Audio Panel", typeof(RectTransform), typeof(LayoutElement));
            audioPanel.layer = content.gameObject.layer;
            audioPanel.transform.SetParent(content, false);
            RectTransform rect = (RectTransform)audioPanel.transform;
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, 1); rect.sizeDelta = new Vector2(0, 300);
            audioPanel.GetComponent<LayoutElement>().preferredHeight = 300;
            AddLabel(audioPanel.transform, "Audio", font, new Vector2(.04f, .78f), new Vector2(.96f, .98f));
            music = AddRow("Music", font, .48f, out musicValue);
            effects = AddRow("Sound Effects", font, .16f, out effectsValue);
            music.onValueChanged.AddListener(LocalAudioSettings.SetMusic);
            effects.onValueChanged.AddListener(LocalAudioSettings.SetEffects);
            LocalAudioSettings.Changed += Refresh;
            Refresh();
            audioPanel.SetActive(false);
            audioButton.onClick.AddListener(ShowAudio);
            if (generalButton != null) generalButton.onClick.AddListener(ShowGeneral);
            if (graphicsButton != null) graphicsButton.onClick.AddListener(ShowGraphics);
        }

        private Button FindButton(string path)
        {
            Transform node = transform.Find(path);
            return node != null ? node.GetComponent<Button>() : null;
        }

        private void ShowAudio() { ShowSection("Audio Panel"); }
        private void ShowGeneral() { ShowSection("General Panel"); }
        private void ShowGraphics() { ShowSection("Graphics Panel"); }
        private void ShowSection(string section)
        {
            foreach (Transform child in content)
                if (child.name == "Audio Panel" || child.name == "General Panel" || child.name == "Graphics Panel")
                    child.gameObject.SetActive(child.name == section);
            ScrollRect scroll = content.GetComponentInParent<ScrollRect>();
            if (scroll != null) { scroll.StopMovement(); scroll.verticalNormalizedPosition = 1f; }
        }

        private Slider AddRow(string title, TMP_FontAsset font, float y, out TMP_Text value)
        {
            AddLabel(audioPanel.transform, title, font, new Vector2(.04f, y), new Vector2(.38f, y + .2f));
            value = AddLabel(audioPanel.transform, "100%", font, new Vector2(.85f, y), new Vector2(.99f, y + .2f));
            RectTransform track = Rect(title + " Slider", audioPanel.transform, new Vector2(.40f, y), new Vector2(.82f, y + .2f));
            track.gameObject.AddComponent<Image>().color = Color.clear;
            Slider slider = track.gameObject.AddComponent<Slider>();
            slider.minValue = 0; slider.maxValue = 1; slider.wholeNumbers = false;
            RectTransform background = Rect("Track", track, new Vector2(0, .4f), new Vector2(1, .6f));
            background.gameObject.AddComponent<Image>().color = new Color(.08f, .11f, .13f);
            RectTransform fill = Rect("Fill", background, Vector2.zero, Vector2.one);
            fill.gameObject.AddComponent<Image>().color = new Color(.12f, .65f, .8f);
            RectTransform handle = Rect("Handle", track, new Vector2(0, .1f), new Vector2(0, .9f));
            handle.sizeDelta = new Vector2(24, 0);
            Image handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = Color.white;
            slider.fillRect = fill; slider.handleRect = handle; slider.targetGraphic = handleImage;
            return slider;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.layer = parent.gameObject.layer;
            var rect = (RectTransform)obj.transform;
            rect.SetParent(parent, false); rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static TMP_Text AddLabel(Transform parent, string text, TMP_FontAsset font, Vector2 min, Vector2 max)
        {
            TMP_Text label = Rect(text, parent, min, max).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font; label.text = text; label.color = new Color(.86f, .9f, .92f);
            label.enableAutoSizing = true; label.fontSizeMin = 12; label.fontSizeMax = 28;
            label.alignment = TextAlignmentOptions.MidlineLeft; label.raycastTarget = false;
            return label;
        }

        private void Refresh()
        {
            music.SetValueWithoutNotify(LocalAudioSettings.Music);
            effects.SetValueWithoutNotify(LocalAudioSettings.Effects);
            musicValue.text = $"{Mathf.RoundToInt(music.value * 100)}%";
            effectsValue.text = $"{Mathf.RoundToInt(effects.value * 100)}%";
        }

        private void OnDestroy()
        {
            LocalAudioSettings.Changed -= Refresh;
            if (audioButton != null) audioButton.onClick.RemoveListener(ShowAudio);
            if (generalButton != null) generalButton.onClick.RemoveListener(ShowGeneral);
            if (graphicsButton != null) graphicsButton.onClick.RemoveListener(ShowGraphics);
        }
    }
}
