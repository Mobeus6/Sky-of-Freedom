using SkyOfFreedom.Data;
using SkyOfFreedom.Gameplay.Factory;
using SkyOfFreedom.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkyOfFreedom.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class ResearchMiniPanelUI : MonoBehaviour
    {
        [Header("Zone Level")]
        [SerializeField] private TMPro.TMP_Text zoneLevelText;

        private void LateUpdate()
        {
            if (zoneLevelText == null) return;
            var game = GameManager.Instance;
            string value = game != null && game.IsGameReady && game.Factory != null
                ? "Lv. " + game.Factory.GetLevel(SkyOfFreedom.Factory.FactoryZoneType.Research)
                : "Lv. —";
            if (zoneLevelText.text != value) zoneLevelText.text = value;
        }

        [Header("Zone References")]
        [SerializeField]
        private FactoryZoneInteraction researchZoneInteraction;

        [Header("UI States")]
        [SerializeField]
        private GameObject researchNode;

        [SerializeField]
        private GameObject noResearchNode;

        [Header("Research Theme")]
        [SerializeField]
        private ResearchUIThemeSO theme;

        [Header("Research UI")]
        [SerializeField]
        private Image researchIcon;

        [SerializeField]
        private TMP_Text titleText;

        [SerializeField]
        private TMP_Text timeText;

        [SerializeField]
        private Image progressFill;

        [Header("Panel")]
        [SerializeField]
        private CanvasGroup canvasGroup;

        private PanelFadeUI panelFade;
        private bool animationReady;

        private void SetPanelVisible(bool visible, bool animate)
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (panelFade == null)
            {
                panelFade = canvasGroup.GetComponent<PanelFadeUI>();
                if (panelFade == null)
                    panelFade = canvasGroup.gameObject.AddComponent<PanelFadeUI>();
            }
            panelFade.SetVisible(visible, animate && isActiveAndEnabled);
        }

        [Header("Open Research")]
        [SerializeField]
        private Button openResearchButton;

        [SerializeField]
        private MenuManager menuManager;

        [SerializeField]
        private MenuButton researchMenuButton;

        private ResearchManager researchManager;
        private TimeManager timeManager;

        /// <summary>Rebinds world interaction without replacing gameplay state.</summary>
        public void BindZone(FactoryZoneInteraction zone)
        {
            if (researchZoneInteraction == zone)
            {
                return;
            }

            if (researchZoneInteraction != null)
            {
                researchZoneInteraction.ZoneSelected -= OnZoneSelected;
                researchZoneInteraction.ZoneDeselected -= OnZoneDeselected;
            }

            researchZoneInteraction = zone;
            if (isActiveAndEnabled && researchZoneInteraction != null)
            {
                researchZoneInteraction.ZoneSelected += OnZoneSelected;
                researchZoneInteraction.ZoneDeselected += OnZoneDeselected;
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
            Hide();
        }

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup =
                    GetComponent<CanvasGroup>();
            }

            if (GameManager.Instance != null)
            {
                researchManager =
                    GameManager.Instance.Research;

                timeManager =
                    GameManager.Instance.Time;
            }

            Hide();

            if (openResearchButton != null)
            {
                openResearchButton.onClick.RemoveAllListeners();

                openResearchButton.onClick.AddListener(
                    OpenResearchPanel);
            }

            ShowNoResearch();
        }

        private void OnEnable()
        {
            // Start hidden without a flash; selection below may reopen the panel.
            SetPanelVisible(false, false);
            animationReady = true;
            if (researchManager == null &&
                GameManager.Instance != null)
            {
                researchManager =
                    GameManager.Instance.Research;
            }

            if (timeManager == null &&
                GameManager.Instance != null)
            {
                timeManager =
                    GameManager.Instance.Time;
            }

            if (researchZoneInteraction != null)
            {
                researchZoneInteraction.ZoneSelected +=
                    OnZoneSelected;

                researchZoneInteraction.ZoneDeselected +=
                    OnZoneDeselected;
            }

            if (researchManager != null)
            {
                researchManager.OnResearchStarted +=
                    OnResearchChanged;

                researchManager.OnResearchCancelled +=
                    OnResearchChanged;

                researchManager.OnResearchCompleted +=
                    OnResearchChanged;
            }

            if (timeManager != null)
            {
                timeManager.OnTick +=
                    OnTick;
            }

            Refresh();

            if (researchZoneInteraction != null && FactoryZoneInteraction.SelectedZone ==
                researchZoneInteraction)
            {
                Open();
            }
        }

        private void OnDisable()
        {
            animationReady = false;
            if (panelFade != null) panelFade.SetVisible(false, false);
            if (researchZoneInteraction != null)
            {
                researchZoneInteraction.ZoneSelected -=
                    OnZoneSelected;

                researchZoneInteraction.ZoneDeselected -=
                    OnZoneDeselected;
            }

            if (researchManager != null)
            {
                researchManager.OnResearchStarted -=
                    OnResearchChanged;

                researchManager.OnResearchCancelled -=
                    OnResearchChanged;

                researchManager.OnResearchCompleted -=
                    OnResearchChanged;
            }

            if (timeManager != null)
            {
                timeManager.OnTick -=
                    OnTick;
            }
        }

        private void OnZoneSelected(
            FactoryZoneInteraction zone)
        {
            if (zone != researchZoneInteraction)
            {
                return;
            }

            Refresh();
            Open();
        }

        private void OnZoneDeselected(
            FactoryZoneInteraction zone)
        {
            if (zone != researchZoneInteraction)
            {
                return;
            }

            Hide();
        }

        private void OnResearchChanged(
            ResearchSO research)
        {
            Refresh();
        }

        private void OnTick(float deltaTime)
        {
            if (researchManager == null)
            {
                return;
            }

            if (!researchManager.HasActiveResearch())
            {
                return;
            }

            RefreshActiveResearch();
        }

        public void Refresh()
        {
            if (researchManager == null)
            {
                ShowNoResearch();
                return;
            }

            if (!researchManager.HasActiveResearch())
            {
                ShowNoResearch();
                return;
            }

            RefreshActiveResearch();
        }

        private void RefreshActiveResearch()
        {
            ResearchState state =
                researchManager.ActiveResearch;

            if (state == null)
            {
                ShowNoResearch();
                return;
            }

            ResearchSO research =
                researchManager.GetResearch(
                    state.ResearchID);

            if (research == null)
            {
                ShowNoResearch();
                return;
            }

            ShowResearch(
                research,
                state);
        }

        private void ShowResearch(
            ResearchSO research,
            ResearchState state)
        {
            if (researchNode != null)
            {
                researchNode.SetActive(true);
            }

            if (noResearchNode != null)
            {
                noResearchNode.SetActive(false);
            }

            if (theme != null)
            {
                Color tierColor =
                    theme.GetTierColor(
                        research.Tier);

                if (researchIcon != null)
                {
                    researchIcon.color =
                        tierColor;
                }

                if (progressFill != null)
                {
                    progressFill.color =
                        tierColor;
                }
            }

            if (researchIcon != null)
            {
                researchIcon.sprite =
                    research.Icon;

                researchIcon.enabled =
                    research.Icon != null;
            }

            if (titleText != null)
            {
                titleText.text =
                    research.ResearchName;
            }

            if (timeText != null)
            {
                timeText.text =
                    FormatTime(
                        state.RemainingTime / researchManager.GetResearchSpeedMultiplier());
            }

            if (progressFill != null)
            {
                UIProgressMotion.Set(progressFill, state.Progress, state, state.IsResearching);
            }
        }

        private void ShowNoResearch()
        {
            if (researchNode != null)
            {
                researchNode.SetActive(false);
            }

            if (noResearchNode != null)
            {
                noResearchNode.SetActive(true);
            }
        }

        public void Open()
        {
            if (canvasGroup == null)
            {
                return;
            }

            SetPanelVisible(true, animationReady);
        }

        public void Hide()
        {
            if (canvasGroup == null)
            {
                return;
            }

            SetPanelVisible(false, animationReady);
        }

        private void OpenResearchPanel()
        {
            if (menuManager == null)
            {
                Debug.LogError(
                    "ResearchMiniPanelUI: MenuManager is not assigned.",
                    this);

                return;
            }

            if (researchMenuButton == null)
            {
                Debug.LogError(
                    "ResearchMiniPanelUI: Research MenuButton is not assigned.",
                    this);

                return;
            }

            Hide();

            menuManager.Toggle(
                researchMenuButton);
        }

        private string FormatTime(
            float seconds)
        {
            if (seconds < 0f)
            {
                seconds = 0f;
            }

            int minutes =
                Mathf.FloorToInt(
                    seconds / 60f);

            int secondsPart =
                Mathf.FloorToInt(
                    seconds % 60f);

            return
                $"{minutes:00}:{secondsPart:00}";
        }
    }
}
