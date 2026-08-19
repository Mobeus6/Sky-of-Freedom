using SkyOfFreedom.Data;
using SkyOfFreedom.Managers;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SkyOfFreedom.UI
{
    public class ResearchTreeUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform content;
        [SerializeField] private ResearchNodeUI nodePrefab;
        [SerializeField] private ResearchUIThemeSO theme;
        [SerializeField] private RectTransform linesParent;
        [SerializeField] private RectTransform horizontalLinePrefab;
        [SerializeField] private RectTransform verticalLinePrefab;

        [Header("Branch Labels")]
        [SerializeField] private RectTransform branchLabelsParent;
        [SerializeField] private ResearchBranchLabelUI branchLabelPrefab;
        [SerializeField] private ScrollRect scrollRect;
        public ResearchUIThemeSO Theme => theme;

        [Header("Layout")]
        [SerializeField] private ResearchItemCardUI itemCard;

        private readonly Dictionary<string, ResearchNodeUI> nodes =
            new Dictionary<string, ResearchNodeUI>();

        private readonly List<ResearchBranchLabelUI> branchLabels =
            new List<ResearchBranchLabelUI>();

        private ResearchNodeUI selectedNode;
        private ResearchManager researchManager;
        private GameDatabase database;

        [SerializeField]
        private List<ResearchLayout> layouts = new();

        private Dictionary<string, ResearchLayout> layoutLookup =
            new Dictionary<string, ResearchLayout>();

        private readonly ResearchCategory[] branchLabelCategories =
        {
            ResearchCategory.Assembly,
            ResearchCategory.Storage,
            ResearchCategory.Production,
            ResearchCategory.IndustrialAutomation,
            ResearchCategory.Finance,
            ResearchCategory.SupplyChain,
            ResearchCategory.GovernmentRelations,
            ResearchCategory.AI
        };

        private void Awake()
        {
            researchManager =
                GameManager.Instance.Research;

            database =
                GameManager.Instance.Database.Database;
        }

        private void Start()
        {
            BuildLookup();
            BuildTree();

            UpdateContentSize();

            BuildBranchLabels();

            SelectFirstAvailableResearch();

            RefreshTree(null);

            if (scrollRect != null)
                scrollRect.onValueChanged.AddListener(
                    OnScrollChanged);

            StartCoroutine(RefreshBranchLabelsNextFrame());
        }
        private IEnumerator RefreshBranchLabelsNextFrame()
        {
            yield return null;

            Canvas.ForceUpdateCanvases();

            RefreshBranchLabels();
        }
        private void OnEnable()
        {
            if (researchManager == null)
                return;

            researchManager.OnResearchStarted += RefreshTree;
            researchManager.OnResearchCompleted += RefreshTree;
            researchManager.OnResearchUnlocked += RefreshTree;
            researchManager.OnResearchCancelled += RefreshTree;
        }

        private void OnDisable()
        {
            if (researchManager != null)
            {
                researchManager.OnResearchStarted -= RefreshTree;
                researchManager.OnResearchCompleted -= RefreshTree;
                researchManager.OnResearchUnlocked -= RefreshTree;
                researchManager.OnResearchCancelled -= RefreshTree;
            }

            if (scrollRect != null)
                scrollRect.onValueChanged.RemoveListener(
                    OnScrollChanged);
        }

        private void Update()
        {
            foreach (ResearchNodeUI node in nodes.Values)
            {
                ResearchState state =
                    researchManager.GetState(
                        node.Research.ID);

                if (state == null)
                    continue;

                if (state.IsResearching)
                    node.Refresh();
            }

            if (selectedNode != null)
            {
                ResearchState state =
                    researchManager.GetState(
                        selectedNode.Research.ID);

                if (state != null &&
                    state.IsResearching)
                {
                    itemCard.Show(
                        selectedNode.Research);
                }
            }
        }

        private void SelectFirstAvailableResearch()
        {
            foreach (ResearchNodeUI node in nodes.Values)
            {
                ResearchState state =
                    researchManager.GetState(
                        node.Research.ID);

                if (state == null)
                    continue;

                if (state.IsUnlocked ||
                    state.IsResearching ||
                    state.IsCompleted)
                {
                    SelectNode(node);
                    return;
                }
            }

            foreach (ResearchNodeUI node in nodes.Values)
            {
                SelectNode(node);
                return;
            }
        }

        private void BuildTree()
        {
            nodes.Clear();

            foreach (ResearchSO research in database.Researches)
            {
                if (research == null)
                    continue; 

                if (string.IsNullOrEmpty(research.ID))
                {
                    Debug.LogError(
                        "ResearchTreeUI: ResearchSO has empty ID: " +
                        research.name);

                    continue;
                }

                if (!layoutLookup.TryGetValue(
                    research.ID,
                    out ResearchLayout layout))
                {
                    Debug.LogError(
                        "ResearchTreeUI: No ResearchLayout found for: " +
                        research.ID);

                    continue;
                }

                ResearchNodeUI node =
                    Instantiate(nodePrefab, content);

                RectTransform rect =
                    node.GetComponent<RectTransform>();

                rect.anchoredPosition = layout.Position;

                node.Initialize(
                    research,
                    researchManager,
                    this);

                nodes.Add(research.ID, node);
            }

            BuildConnections();
        }

        private void BuildConnections()
        {
            foreach (Transform child in linesParent)
            {
                Destroy(child.gameObject);
            }

            foreach (ResearchSO research in database.Researches)
            {
                if (research == null ||
                    string.IsNullOrEmpty(research.ID))
                {
                    continue;
                }

                if (research.Prerequisites == null)
                    continue;

                foreach (ResearchSO prerequisite
                    in research.Prerequisites)
                {
                    if (prerequisite == null ||
                        string.IsNullOrEmpty(prerequisite.ID))
                    {
                        Debug.LogError(
                            "ResearchTreeUI: Invalid prerequisite for research: " +
                            research.ID);

                        continue;
                    }

                    if (!nodes.TryGetValue(
                            prerequisite.ID,
                            out ResearchNodeUI parent))
                    {
                        continue;
                    }

                    if (!nodes.TryGetValue(
                            research.ID,
                            out ResearchNodeUI child))
                    {
                        continue;
                    }

                    Vector2 start =
                        parent.GetConnectionPoint();

                    Vector2 end =
                        child.GetConnectionPoint();

                    Color connectionColor =
                        GetConnectionColor(
                            research);

                    CreateHorizontalLine(
                        start,
                        end.x,
                        connectionColor);

                    CreateVerticalLine(
                        new Vector2(
                            end.x,
                            start.y),
                        end.y,
                        connectionColor);
                }
            }
        }

        private Color GetConnectionColor(
            ResearchSO research)
        {
            ResearchState state =
                researchManager.GetState(
                    research.ID);

            if (state == null)
            {
                return theme.GetConnectionColor(
                    true,
                    research.Tier);
            }

            bool locked =
                !state.IsUnlocked &&
                !state.IsResearching &&
                !state.IsCompleted;

            return theme.GetConnectionColor(
                locked,
                research.Tier);
        }

        private void CreateHorizontalLine(
            Vector2 start,
            float endX,
            Color color)
        {
            RectTransform line =
                Instantiate(
                    horizontalLinePrefab,
                    linesParent);

            float x =
                Mathf.Min(
                    start.x,
                    endX);

            float width =
                Mathf.Abs(
                    endX - start.x);

            line.anchoredPosition =
                new Vector2(
                    x,
                    start.y);

            line.sizeDelta =
                new Vector2(
                    width,
                    6f);

            Image image =
                line.GetComponent<Image>();

            if (image != null)
                image.color =
                    color;
        }

        private void CreateVerticalLine(
            Vector2 start,
            float endY,
            Color color)
        {
            RectTransform line =
                Instantiate(
                    verticalLinePrefab,
                    linesParent);

            float y =
                Mathf.Max(
                    start.y,
                    endY);

            float height =
                Mathf.Abs(
                    endY - start.y);

            line.anchoredPosition =
                new Vector2(
                    start.x,
                    y);

            line.sizeDelta =
                new Vector2(
                    6f,
                    height);

            Image image =
                line.GetComponent<Image>();

            if (image != null)
                image.color =
                    color;
        }

        private void BuildBranchLabels()
        {
            ClearBranchLabels();

            if (branchLabelsParent == null ||
                branchLabelPrefab == null)
            {
                return;
            }

            foreach (ResearchCategory category
                in branchLabelCategories)
            {
                ResearchBranchLabelUI label =
                    Instantiate(
                        branchLabelPrefab,
                        branchLabelsParent);

                label.Initialize(
                    GetCategoryLabel(category));

                branchLabels.Add(label);
            }

            RefreshBranchLabels();
        }

        private void ClearBranchLabels()
        {
            foreach (ResearchBranchLabelUI label
                in branchLabels)
            {
                if (label != null)
                    Destroy(label.gameObject);
            }

            branchLabels.Clear();
        }

        private void RefreshBranchLabels()
        {
            for (int i = 0;
                 i < branchLabelCategories.Length;
                 i++)
            {
                if (i >= branchLabels.Count)
                    continue;

                UpdateBranchLabel(
                    branchLabels[i],
                    branchLabelCategories[i]);
            }
        }
        private void UpdateBranchLabel(
     ResearchBranchLabelUI label,
     ResearchCategory category)
        {
            if (label == null)
                return;

            foreach (ResearchNodeUI node in nodes.Values)
            {
                if (node.Research.Category != category)
                    continue;

                Vector3 worldPosition =
                    node.RectTransform.position;

                Vector3 localPosition =
                    branchLabelsParent.InverseTransformPoint(
                        worldPosition);

                RectTransform labelRect =
                    label.RectTransform;

                float y =
                    localPosition.y -
                    node.RectTransform.rect.height * 0.5f;

                labelRect.anchoredPosition =
                    new Vector2(
                        labelRect.anchoredPosition.x,
                        y);

                label.gameObject.SetActive(true);

                return;
            }

            label.gameObject.SetActive(false);
        }
        private string GetCategoryLabel(
    ResearchCategory category)
        {
            switch (category)
            {
                case ResearchCategory.IndustrialAutomation:
                    return "INDUSTRIAL AUTOMATION";

                case ResearchCategory.GovernmentRelations:
                    return "GOVERNMENT RELATIONS";

                case ResearchCategory.SupplyChain:
                    return "SUPPLY CHAIN";

                default:
                    return category.ToString().ToUpperInvariant();
            }
        }

        private void OnScrollChanged(Vector2 _)
        {
            RefreshBranchLabels();
        }

        private void BuildLookup()
        {
            layoutLookup.Clear();

            foreach (ResearchLayout layout in layouts)
            {
                if (layout == null ||
                    layout.Research == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(layout.Research.ID))
                {
                    Debug.LogError(
                        "ResearchTreeUI: ResearchLayout has ResearchSO with empty ID.");

                    continue;
                }

                layoutLookup[
                    layout.Research.ID] =
                    layout;
            }
        }

        private void RefreshTree(
            ResearchSO _)
        {
            foreach (ResearchNodeUI node in nodes.Values)
                node.Refresh();

            BuildConnections();
            RefreshBranchLabels();

            if (selectedNode != null)
            {
                itemCard.Show(
                    selectedNode.Research);
            }
        }

        private void UpdateContentSize()
        {
            float maxX = 0;
            float maxY = 0;

            foreach (ResearchNodeUI node in nodes.Values)
            {
                RectTransform rect =
                    node.RectTransform;

                maxX = Mathf.Max(
                    maxX,
                    rect.anchoredPosition.x +
                    rect.rect.width);

                maxY = Mathf.Max(
                    maxY,
                    Mathf.Abs(
                        rect.anchoredPosition.y) +
                    rect.rect.height);
            }

            content.sizeDelta =
                new Vector2(
                    maxX + 300,
                    maxY + 300);
        }

        public ResearchNodeUI GetNode(
            string researchID)
        {
            nodes.TryGetValue(
                researchID,
                out ResearchNodeUI node);

            return node;
        }

        public void SelectNode(
            ResearchNodeUI node)
        {
            if (selectedNode != null)
                selectedNode.SetSelected(false);

            selectedNode = node;

            selectedNode.SetSelected(true);

            itemCard.Show(
                node.Research);
        }
    }
}