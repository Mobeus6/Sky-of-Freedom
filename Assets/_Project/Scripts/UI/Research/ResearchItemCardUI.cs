using SkyOfFreedom.Data;
using SkyOfFreedom.Managers;
using TMPro;
using Unity.VisualScripting;
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

        private ResearchManager researchManager;
        private ResearchSO currentResearch;

        private void Awake()
        {
            researchManager = GameManager.Instance.Research;

            researchButton.onClick.AddListener(StartResearch);
        }
        private void OnEnable()
        {
            researchManager.OnResearchStarted += Refresh;
            researchManager.OnResearchCompleted += Refresh;
            researchManager.OnResearchUnlocked += Refresh;
            researchManager.OnResearchCancelled += Refresh;
        }

        private void OnDisable()
        {
            if (researchManager == null)
                return;

            researchManager.OnResearchStarted -= Refresh;
            researchManager.OnResearchCompleted -= Refresh;
            researchManager.OnResearchUnlocked -= Refresh;
            researchManager.OnResearchCancelled -= Refresh;
        }
        public void Show(ResearchSO research)
        {
            currentResearch = research;

            icon.sprite = research.Icon;

            titleText.text = research.ResearchName;
            categoryText.text = research.Category.ToString();
            tierText.text = $"Tier {research.Tier}";

            descriptionText.text = research.EffectDescription;

            factoryLevelText.text = research.RequiredFactoryLevel.ToString();
            costText.text = research.Cost.ToString();
            timeText.text = FormatTime(research.ResearchTime);

            effectsText.text = research.EffectDescription;
            unlocksText.text = research.UnlockDescription;

            RefreshButton();
        }
        private void Refresh(ResearchSO _)
        {
            if (currentResearch == null)
                return;

            Show(currentResearch);
        }
        private void RefreshButton()
        {
            if (researchManager == null)
                return;

            if (currentResearch == null)
                return;

            ResearchState state =
                researchManager.GetState(currentResearch.ID);

            if (state == null)
                return;

            if (buttonText == null || researchButton == null)
                return;

            if (state.IsCompleted)
            {
                buttonText.text = "Completed";
                researchButton.interactable = false;
                return;
            }

            if (state.IsResearching)
            {
                buttonText.text = "Researching";
                researchButton.interactable = false;
                return;
            }

            if (!researchManager.CanStartResearch(currentResearch))
            {
                buttonText.text = "Locked";
                researchButton.interactable = false;
                return;
            }

            buttonText.text = "Research";
            researchButton.interactable = true;
        }

        private void StartResearch()
        {
            if (currentResearch == null)
                return;

            if (researchManager.StartResearch(currentResearch))
            {
                RefreshButton();
            }
        }

        private string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            int sec = Mathf.FloorToInt(seconds % 60f);

            return $"{minutes:00}:{sec:00}";
        }
    }
}