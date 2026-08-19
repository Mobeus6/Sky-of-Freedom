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

        [Header("Open Research")]
        [SerializeField]
        private Button openResearchButton;

        [SerializeField]
        private MenuManager menuManager;

        [SerializeField]
        private MenuButton researchMenuButton;

        private ResearchManager researchManager;
        private TimeManager timeManager;

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

            if (FactoryZoneInteraction.SelectedZone ==
                researchZoneInteraction)
            {
                Open();
            }
        }

        private void OnDisable()
        {
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
                        state.RemainingTime);
            }

            if (progressFill != null)
            {
                progressFill.fillAmount =
                    Mathf.Clamp01(
                        state.Progress);
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

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        public void Hide()
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
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