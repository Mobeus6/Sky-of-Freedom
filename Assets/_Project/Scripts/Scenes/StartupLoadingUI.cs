using SkyOfFreedom.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkyOfFreedom.UI
{
    public class StartupLoadingUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private GameObject mainMenuPanel;

        [Header("Loading")]
        [SerializeField] private Slider progressSlider;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button retryButton;

        [Header("Menu")]
        [SerializeField] private Button playButton;

        [Header("Presentation")]
        [SerializeField, Min(0f)] private float completedDisplaySeconds = 0.25f;

        private float completedAt = -1f;
        private bool menuShown;

        private void Awake()
        {
            if (loadingPanel == null || mainMenuPanel == null ||
                progressSlider == null || progressText == null ||
                statusText == null || retryButton == null ||
                playButton == null)
            {
                if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
                if (playButton != null) playButton.interactable = false;
                Debug.LogError("StartupLoadingUI: assign all Inspector fields.", this);
                enabled = false;
                return;
            }

            mainMenuPanel.SetActive(false);
            loadingPanel.SetActive(true);
            playButton.interactable = false;

            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
            progressSlider.wholeNumbers = false;
            progressSlider.interactable = false;
            progressSlider.SetValueWithoutNotify(0f);
            progressText.text = "0%";
            statusText.text = "Preparing…";

            retryButton.gameObject.SetActive(false);
            retryButton.onClick.AddListener(Retry);
        }

        private void Update()
        {
            if (menuShown) return;

            GameManager manager = GameManager.Instance;
            if (manager == null)
            {
                statusText.text = "Waiting for GameManager…";
                retryButton.gameObject.SetActive(false);
                return;
            }

            float progress = Mathf.Clamp01(manager.LoadingProgress);
            progressSlider.SetValueWithoutNotify(progress);
            progressText.text = $"{Mathf.RoundToInt(progress * 100f)}%";
            statusText.text = manager.LoadingStatus;

            bool canRetry = manager.HasLoadingError &&
                manager.CanRetryLoading && !manager.IsLoading;
            retryButton.gameObject.SetActive(canRetry);
            retryButton.interactable = canRetry;

            if (!manager.IsGameReady)
            {
                completedAt = -1f;
                return;
            }

            if (completedAt < 0f)
            {
                completedAt = UnityEngine.Time.unscaledTime;
            }

            if (UnityEngine.Time.unscaledTime - completedAt <
                completedDisplaySeconds)
            {
                return;
            }

            menuShown = true;
            playButton.interactable = true;
            mainMenuPanel.SetActive(true);
            loadingPanel.SetActive(false);
        }

        private void Retry()
        {
            GameManager manager = GameManager.Instance;
            if (manager == null || manager.IsLoading ||
                !manager.HasLoadingError || !manager.CanRetryLoading)
            {
                return;
            }

            retryButton.interactable = false;
            _ = manager.StartGameAsync();
        }

        private void OnDestroy()
        {
            if (retryButton != null)
            {
                retryButton.onClick.RemoveListener(Retry);
            }
        }
    }
}
