using System.Collections;
using SkyOfFreedom.Factory;
using SkyOfFreedom.Gameplay.Factory;
using SkyOfFreedom.Managers;
using TMPro;
using UnityEngine;

namespace SkyOfFreedom.UI.Factory
{
    // Keep this controller on an always-active object outside both panels.
    public sealed class ZoneUpgradePresentationUI : MonoBehaviour
    {
        [Header("Existing panels")]
        [SerializeField] private CanvasGroup factoryPanel;
        [SerializeField] private MenuManager menuManager;
        [SerializeField] private CanvasGroup upgradePanel;
        [Header("Existing text objects")]
        [SerializeField] private TMP_Text nameOfUpgradeText;
        [SerializeField] private TMP_Text levelText;
        [Header("Safe test mode (does not change progression)")]
        [SerializeField] private FactoryZoneType testZone = FactoryZoneType.Production;
        [SerializeField, Range(1, 5)] private int testPreviousLevel = 1;
        [SerializeField, Range(1, 5)] private int testNewLevel = 2;
        [Header("Presentation")]
        [SerializeField, Range(.1f, 1f)] private float panelOpacity = .8f;
        [SerializeField, Min(.1f)] private float fadeDuration = .2f;
        [SerializeField, Min(.5f)] private float holdDuration = 2.2f;
        private Coroutine routine;
        private FactoryZoneLevelVisuals pendingVisuals;
        private FactoryZoneLevelVisuals previewVisuals;
        private PanelFadeUI panelFade;
        private bool restoreFade;
        [SerializeField, HideInInspector] private int timingVersion;
        private void UpgradeTiming()
        {
            if (timingVersion >= 1) return;
            fadeDuration = Mathf.Max(fadeDuration, .45f);
            holdDuration = Mathf.Max(holdDuration, 5f);
            timingVersion = 1;
        }
        private void OnValidate() { UpgradeTiming(); }
        public bool IsBusy => routine != null;
        public bool IsConfigured => isActiveAndEnabled && factoryPanel != null && upgradePanel != null &&
            !transform.IsChildOf(factoryPanel.transform) && !transform.IsChildOf(upgradePanel.transform);

        [ContextMenu("Test Selected Zone Upgrade (No Save)")]
        public void TestSelectedZoneUpgrade()
        {
            if (!Application.isPlaying) { Debug.LogWarning("[ZoneUpgrade] Start Play Mode before testing.", this); return; }
            if (IsBusy) { Debug.LogWarning("[ZoneUpgrade] A presentation is already running.", this); return; }
            if (!IsConfigured) { Debug.LogWarning("[ZoneUpgrade] Assign both panels and keep this controller active outside them.", this); return; }
            if (GameManager.Instance == null || !GameManager.Instance.IsGameReady ||
                GameManager.Instance.IsAccountTransition || GameManager.Instance.Factory == null)
            { Debug.LogWarning("[ZoneUpgrade] Wait for account and factory initialization.", this); return; }
            Debug.Log("[ZoneUpgrade] Test requested: " + testZone, this);
            int previous = Mathf.Clamp(testPreviousLevel, 1, 4);
            int next = Mathf.Clamp(testNewLevel, previous + 1, 5);
            CancelPrepared();
            foreach (var visuals in Object.FindObjectsByType<FactoryZoneLevelVisuals>())
            {
                if (!visuals.isActiveAndEnabled || visuals.ZoneType != testZone) continue;
                previewVisuals = visuals;
                routine = StartCoroutine(Present(testZone, previous, next, true));
                return;
            }
            Debug.LogWarning("Upgrade preview: no active visuals found for " + testZone, this);
        }

        private void Awake()
        {
            UpgradeTiming();
            if (upgradePanel == null) return;
            upgradePanel.alpha = 0f;
            upgradePanel.blocksRaycasts = false;
            upgradePanel.interactable = false;
        }

        public FactoryZoneLevelVisuals Prepare(FactoryZoneType zone)
        {
            if (!IsConfigured || IsBusy) return null;
            foreach (var visuals in Object.FindObjectsByType<FactoryZoneLevelVisuals>())
            {
                if (!visuals.isActiveAndEnabled || visuals.ZoneType != zone) continue;
                pendingVisuals = visuals;
                visuals.BeginSharedUpgrade();
                return visuals;
            }
            return null;
        }

        public void CancelPrepared()
        {
            if (pendingVisuals != null) pendingVisuals.FinishSharedUpgrade(false);
            pendingVisuals = null;
        }

        public void Show(FactoryZoneType zone, int previous, int level, FactoryProgressionConfig config)
        {
            if (!IsConfigured || IsBusy) { CancelPrepared(); return; }
            routine = StartCoroutine(Present(zone, previous, level));
        }

        private IEnumerator Present(FactoryZoneType zone, int previous, int level, bool preview = false)
        {
            if (menuManager != null)
                menuManager.CloseAll();
            else
            {
                factoryPanel.interactable = false;
                factoryPanel.blocksRaycasts = false;
                var factoryFade = factoryPanel.GetComponent<PanelFadeUI>();
                if (factoryFade != null) factoryFade.SetVisible(false);
                else factoryPanel.alpha = 0f;
            }
            var closingFade = factoryPanel.GetComponent<PanelFadeUI>();
            float closeWait = 0f;
            while (closingFade != null && closingFade.isActiveAndEnabled && closingFade.IsAnimating)
            {
                closeWait += Mathf.Min(Time.unscaledDeltaTime, .05f);
                if (closeWait >= 2f)
                {
                    Debug.LogWarning("[ZoneUpgrade] Factory fade did not finish; completing closure.", this);
                    closingFade.SetVisible(false, false);
                    break;
                }
                yield return null;
            }
            Debug.Log("[ZoneUpgrade] Factory closed; presenting upgrade.", this);
            var focusVisuals = preview ? previewVisuals : pendingVisuals;
            if (focusVisuals != null)
            {
                focusVisuals.FocusUpgradeCamera();
                while (focusVisuals != null && focusVisuals.IsUpgradeCameraMoving)
                    yield return null;
            }

            if (nameOfUpgradeText != null) nameOfUpgradeText.text = zone + " Zone";
            if (levelText != null) levelText.text = "Lv. " + level;
            // This component owns opacity while presenting; no competing generic fade.
            panelFade = upgradePanel.GetComponent<PanelFadeUI>();
            restoreFade = panelFade != null && panelFade.enabled;
            if (panelFade != null) { panelFade.SetVisible(false, false); panelFade.enabled = false; }
            upgradePanel.gameObject.SetActive(true);
            upgradePanel.blocksRaycasts = false;
            upgradePanel.interactable = false;
            upgradePanel.alpha = 0f;
            var visuals = pendingVisuals;
            pendingVisuals = null;
            if (preview && previewVisuals != null) previewVisuals.PreviewUpgrade(previous, level);
            else if (visuals != null) visuals.FinishSharedUpgrade(true);
            yield return Fade(0f, panelOpacity);
            float elapsed = 0f;
            while (elapsed < holdDuration || (focusVisuals != null && focusVisuals.IsPresenting))
            {
                var game = GameManager.Instance;
                if (game == null || !game.IsGameReady || game.IsAccountTransition) break;
                elapsed += Mathf.Min(Time.unscaledDeltaTime, .05f);
                yield return null;
            }
            yield return Fade(upgradePanel.alpha, 0f);
            HidePresentation();
            routine = null;
        }

        private IEnumerator Fade(float from, float to)
        {
            float elapsed = 0f;
            float duration = Mathf.Max(.01f, fadeDuration);
            while (elapsed < duration)
            {
                upgradePanel.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / duration));
                elapsed += Mathf.Min(Time.unscaledDeltaTime, .05f);
                yield return null;
            }
            upgradePanel.alpha = to;
        }

        private void HidePresentation()
        {
            if (upgradePanel != null)
            {
                upgradePanel.alpha = 0f;
                upgradePanel.blocksRaycasts = false;
                upgradePanel.interactable = false;
            }
            if (panelFade != null && restoreFade) panelFade.enabled = true;
            panelFade = null;
            restoreFade = false;
        }

        private void OnDisable()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
            CancelPrepared();
            if (previewVisuals != null) previewVisuals.StopPreview();
            previewVisuals = null;
            HidePresentation();
        }
    }
}
