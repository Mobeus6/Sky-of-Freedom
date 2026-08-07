using SkyOfFreedom.Data;
using SkyOfFreedom.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkyOfFreedom.UI
{
    public class ResearchNodeUI : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Image background;
        [SerializeField] private Image tierFrame;
        [SerializeField] private Image icon;

        [SerializeField] private RectTransform connectionPoint;
        [SerializeField] private GameObject progressGlow;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private GameObject locked;
        [SerializeField] private GameObject available;
        [SerializeField] private GameObject researched;
        [SerializeField] private Image selectionBorder;
        [SerializeField] private Image progressFill;
        [SerializeField] private GameObject progressOverlay;

        [SerializeField] private Button button;

        private ResearchTreeUI treeUI;

        private ResearchSO research;
        private ResearchManager researchManager;

        public ResearchSO Research => research;

        public RectTransform RectTransform =>
            (RectTransform)transform;

        public RectTransform ConnectionPoint =>
            connectionPoint;

        public void Initialize(
            ResearchSO research,
            ResearchManager manager,
            ResearchTreeUI tree)
        {
            this.research = research;
            researchManager = manager;
            treeUI = tree;

            titleText.text = research.ResearchName;
            icon.sprite = research.Icon;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClicked);

            Refresh();
        }

        public void Refresh()
        {
            ResearchState state =
                researchManager.GetState(research.ID);

            if (state == null)
                return;

            ApplyTierVisual(state);
            ApplyStateVisual(state);
        }

        private void ApplyTierVisual(
            ResearchState state)
        {
            ResearchUIThemeSO theme =
                treeUI.Theme;

            background.color =
                theme.Background;

            Color tierColor =
    theme.GetTierColor(research.Tier);

            tierFrame.color = tierColor;
            titleText.color = tierColor;

            progressFill.color = tierColor;

            progressGlow.GetComponent<Image>().color = tierColor;
        }

        private void ApplyStateVisual(
     ResearchState state)
        {
            bool isLocked =
                !state.IsUnlocked &&
                !state.IsCompleted;

            bool isAvailable =
                state.IsUnlocked &&
                !state.IsResearching &&
                !state.IsCompleted;

            bool isResearching =
                state.IsResearching;

            bool isCompleted =
                state.IsCompleted;

            locked.SetActive(isLocked);

            available.SetActive(isAvailable);

            researched.SetActive(isCompleted);
            progressFill.gameObject.SetActive(isResearching);

progressGlow.SetActive(isResearching);

timeText.gameObject.SetActive(isResearching);
            progressFill.fillAmount =
                state.Progress;

            timeText.text =
                FormatTime(state.RemainingTime);
        }

        public void SetSelected(bool value)
        {
            selectionBorder.gameObject.SetActive(value);

            if (value)
            {
                selectionBorder.color = treeUI.Theme.SelectedOutline;
            }
        }

        public Vector2 GetConnectionPoint()
        {
            RectTransform content =
                (RectTransform)transform.parent;

            return content.InverseTransformPoint(
                connectionPoint.position);
        }

        private string FormatTime(float seconds)
        {
            int minutes =
                Mathf.FloorToInt(seconds / 60f);

            int sec =
                Mathf.FloorToInt(seconds % 60f);

            return $"{minutes:00}:{sec:00}";
        }

        private void OnClicked()
        {
            treeUI.SelectNode(this);
        }
    }
}