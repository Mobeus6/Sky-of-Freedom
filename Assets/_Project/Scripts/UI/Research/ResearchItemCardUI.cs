using SkyOfFreedom.Data;
using SkyOfFreedom.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkyOfFreedom.UI
{
    public class ResearchItemCardUI : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text categoryText;
        [SerializeField] private TMP_Text tierText;

        [Header("Status Badge")]
        [SerializeField] private GameObject researching;
        [SerializeField] private GameObject researched;
        [SerializeField] private GameObject locked;
        [SerializeField] private GameObject available;

        [Header("Description")]
        [SerializeField] private TMP_Text descriptionText;

        [Header("Requirements")]
        [SerializeField] private TMP_Text factoryLevelText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text timeText;

        [Header("Effects")]
        [SerializeField] private TMP_Text effectsText;

        [Header("Unlocks")]
        [SerializeField] private TMP_Text unlocksText;

        [Header("Button")]
        [SerializeField] private Button researchButton;
        [SerializeField] private TMP_Text buttonText;

        [Header("Theme")]
        [SerializeField] private ResearchUIThemeSO theme;

        private ResearchManager researchManager;
        private ResearchSO currentResearch;

        private Color normalCostColor;
        private Color normalFactoryLevelColor;

        private void Awake()
        {
            researchManager =
                GameManager.Instance.Research;

            if (costText != null)
                normalCostColor =
                    costText.color;

            if (factoryLevelText != null)
                normalFactoryLevelColor =
                    factoryLevelText.color;

            researchButton.onClick.AddListener(
                StartResearch);
        }

        private void OnEnable()
        {
            if (researchManager == null)
                return;

            researchManager.OnResearchStarted += Refresh;
            researchManager.OnResearchCompleted += Refresh;
            researchManager.OnResearchUnlocked += Refresh;
            researchManager.OnResearchCancelled += Refresh;

            GameManager.Instance.Economy.OnMoneyChanged +=
                RefreshMoney;
        }

        private void OnDisable()
        {
            if (researchManager == null)
                return;

            researchManager.OnResearchStarted -= Refresh;
            researchManager.OnResearchCompleted -= Refresh;
            researchManager.OnResearchUnlocked -= Refresh;
            researchManager.OnResearchCancelled -= Refresh;

            GameManager.Instance.Economy.OnMoneyChanged -=
                RefreshMoney;
        }

        public void Show(ResearchSO research)
        {
            currentResearch = research;

            icon.sprite =
                research.Icon;

            titleText.text =
                research.ResearchName;

            categoryText.text =
                research.Category.ToString();

            tierText.text =
                $"Tier {research.Tier}";

            ApplyTierVisual();

            descriptionText.text =
                research.EffectDescription;

            factoryLevelText.text =
                research.RequiredFactoryLevel.ToString();

            costText.text =
                research.Cost.ToString();

            timeText.text =
                FormatTime(
                    research.ResearchTime);

            effectsText.text =
                research.EffectDescription;

            unlocksText.text =
                research.UnlockDescription;

            RefreshStatus();
            RefreshButton();
        }

        private void ApplyTierVisual()
        {
            if (currentResearch == null ||
                theme == null)
            {
                return;
            }

            Color tierColor =
                theme.GetTierColor(
                    currentResearch.Tier);

            icon.color =
                tierColor;

            titleText.color =
                tierColor;

            categoryText.color =
                tierColor;

            tierText.color =
                tierColor;
        }

        private void Refresh(
            ResearchSO _)
        {
            if (currentResearch == null)
                return;

            Show(currentResearch);
        }

        private void RefreshMoney(
            long _)
        {
            if (currentResearch == null)
                return;

            RefreshButton();
        }

        private void RefreshStatus()
        {
            if (currentResearch == null ||
                researchManager == null)
            {
                return;
            }

            ResearchState state =
                researchManager.GetState(
                    currentResearch.ID);

            if (state == null)
                return;

            bool isResearching =
                state.IsResearching;

            bool isResearched =
                state.IsCompleted;

            bool isAvailable =
                state.IsUnlocked &&
                !state.IsResearching &&
                !state.IsCompleted;

            bool isLocked =
                !state.IsUnlocked &&
                !state.IsResearching &&
                !state.IsCompleted;

            if (researching != null)
                researching.SetActive(
                    isResearching);

            if (researched != null)
                researched.SetActive(
                    isResearched);

            if (available != null)
                available.SetActive(
                    isAvailable);

            if (locked != null)
                locked.SetActive(
                    isLocked);
        }

        private void RefreshButton()
        {
            if (researchManager == null ||
                currentResearch == null ||
                researchButton == null ||
                buttonText == null)
            {
                return;
            }

            ResearchState state =
                researchManager.GetState(
                    currentResearch.ID);

            if (state == null)
                return;

            if (state.IsCompleted)
            {
                researchButton.gameObject.SetActive(
                    false);

                SetCostColor(false);

                return;
            }

            researchButton.gameObject.SetActive(
                true);

            if (state.IsResearching)
            {
                buttonText.text =
                    "Researching";

                researchButton.interactable =
                    false;

                SetCostColor(false);

                return;
            }

            if (!state.IsUnlocked)
            {
                buttonText.text =
                    "Locked";

                researchButton.interactable =
                    false;

                SetCostColor(false);

                return;
            }

            buttonText.text =
                "Research";

            bool canAfford =
                GameManager.Instance.Economy.HasMoney(
                    currentResearch.Cost);

            researchButton.interactable =
                canAfford;

            SetCostColor(
                !canAfford);

            RefreshFactoryLevel();
        }

        private void RefreshFactoryLevel()
        {
            if (factoryLevelText == null ||
                currentResearch == null ||
                GameManager.Instance.Factory == null)
            {
                return;
            }

            bool insufficientLevel =
                GameManager.Instance.Factory.Level <
                currentResearch.RequiredFactoryLevel;

            factoryLevelText.color =
                insufficientLevel
                    ? Color.red
                    : normalFactoryLevelColor;

            if (insufficientLevel)
            {
                researchButton.interactable =
                    false;
            }
        }

        private void SetCostColor(
            bool insufficientFunds)
        {
            if (costText == null)
                return;

            costText.color =
                insufficientFunds
                    ? Color.red
                    : normalCostColor;
        }

        private void StartResearch()
        {
            if (currentResearch == null)
                return;

            researchManager.StartResearch(
                currentResearch);
        }

        private string FormatTime(
            float seconds)
        {
            int minutes =
                Mathf.FloorToInt(
                    seconds / 60f);

            int sec =
                Mathf.FloorToInt(
                    seconds % 60f);

            return
                $"{minutes:00}:{sec:00}";
        }
    }
}