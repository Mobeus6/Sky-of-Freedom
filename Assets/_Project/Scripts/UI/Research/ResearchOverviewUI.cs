using SkyOfFreedom.Data;
using SkyOfFreedom.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkyOfFreedom.UI
{
    public class ResearchOverviewUI : MonoBehaviour
    {
        [Header("Research Overview")]
        [SerializeField]
        private TMP_Text researchTotalText;

        [SerializeField]
        private TMP_Text currentResearchText;

        [Header("Open Research")]
        [SerializeField]
        private Button openResearchButton;

        [SerializeField]
        private MenuManager menuManager;

        [SerializeField]
        private MenuButton researchMenuButton;

        private ResearchManager researchManager;

        private void Awake()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError(
                    "ResearchOverviewUI: GameManager not found.",
                    this);

                return;
            }

            researchManager =
                GameManager.Instance.Research;

            if (openResearchButton != null)
            {
                openResearchButton.onClick.RemoveAllListeners();

                openResearchButton.onClick.AddListener(
                    OpenResearchPanel);
            }
        }

        private void OnEnable()
        {
            if (researchManager == null &&
                GameManager.Instance != null)
            {
                researchManager =
                    GameManager.Instance.Research;
            }

            if (researchManager != null)
            {
                researchManager.OnResearchStarted +=
                    OnResearchChanged;

                researchManager.OnResearchCancelled +=
                    OnResearchChanged;

                researchManager.OnResearchCompleted +=
                    OnResearchChanged;

                researchManager.OnResearchUnlocked +=
                    OnResearchChanged;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (researchManager == null)
                return;

            researchManager.OnResearchStarted -=
                OnResearchChanged;

            researchManager.OnResearchCancelled -=
                OnResearchChanged;

            researchManager.OnResearchCompleted -=
                OnResearchChanged;

            researchManager.OnResearchUnlocked -=
                OnResearchChanged;
        }

        private void OnResearchChanged(
            ResearchSO research)
        {
            Refresh();
        }

        public void Refresh()
        {
            if (researchManager == null)
            {
                ClearResearchOverview();
                return;
            }

            RefreshResearchTotal();
            RefreshCurrentResearch();
        }

        private void RefreshResearchTotal()
        {
            if (researchTotalText == null)
                return;

            int completedResearches = 0;
            int maximumResearches = 0;

            if (researchManager.Database != null &&
                researchManager.Database.Researches != null)
            {
                maximumResearches =
                    researchManager.Database.Researches.Count;
            }

            foreach (
                ResearchState state
                in researchManager.ResearchStates.Values)
            {
                if (state == null)
                    continue;

                if (state.IsCompleted)
                {
                    completedResearches++;
                }
            }

            researchTotalText.text =
                $"{completedResearches}/{maximumResearches}";
        }

        private void RefreshCurrentResearch()
        {
            if (currentResearchText == null)
                return;

            if (!researchManager.HasActiveResearch())
            {
                currentResearchText.text =
                    "No Research";

                return;
            }

            ResearchState activeState =
                researchManager.ActiveResearch;

            if (activeState == null)
            {
                currentResearchText.text =
                    "No Research";

                return;
            }

            ResearchSO research =
                researchManager.GetResearch(
                    activeState.ResearchID);

            if (research == null)
            {
                currentResearchText.text =
                    "No Research";

                return;
            }

            currentResearchText.text =
                research.ResearchName;
        }

        private void ClearResearchOverview()
        {
            if (researchTotalText != null)
            {
                researchTotalText.text =
                    "0/0";
            }

            if (currentResearchText != null)
            {
                currentResearchText.text =
                    "No Research";
            }
        }

        private void OpenResearchPanel()
        {
            if (menuManager == null)
            {
                Debug.LogError(
                    "ResearchOverviewUI: MenuManager is not assigned.",
                    this);

                return;
            }

            if (researchMenuButton == null)
            {
                Debug.LogError(
                    "ResearchOverviewUI: Research MenuButton is not assigned.",
                    this);

                return;
            }

            menuManager.Toggle(
                researchMenuButton);
        }
    }
}