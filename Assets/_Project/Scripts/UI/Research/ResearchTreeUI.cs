using SkyOfFreedom.Data;
using SkyOfFreedom.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace SkyOfFreedom.UI
{
    public class ResearchTreeUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform content;
        [SerializeField] private ResearchNodeUI nodePrefab;
        [SerializeField]
        private ResearchUIThemeSO theme; 
        [SerializeField] private RectTransform linesParent;
        [SerializeField] private RectTransform horizontalLinePrefab;
        [SerializeField] private RectTransform verticalLinePrefab;
        public ResearchUIThemeSO Theme => theme;
        [Header("Layout")]
        [SerializeField] private ResearchItemCardUI itemCard;
        private readonly Dictionary<string, ResearchNodeUI> nodes =
            new Dictionary<string, ResearchNodeUI>();
        private ResearchNodeUI selectedNode;
        private ResearchManager researchManager;
        private GameDatabase database;
        [SerializeField]
        private List<ResearchLayout> layouts = new();
        private Dictionary<string, ResearchLayout> layoutLookup =
    new Dictionary<string, ResearchLayout>();

        private void Awake()
        {
            researchManager = GameManager.Instance.Research;
            database = GameManager.Instance.Database.Database;
        }

        private void Start()
        {
            BuildLookup();
            BuildTree();

            UpdateContentSize();
            SelectFirstAvailableResearch();

            RefreshTree(null);
        }
        private void OnEnable()
        {
            researchManager.OnResearchStarted += RefreshTree;
            researchManager.OnResearchCompleted += RefreshTree;
            researchManager.OnResearchUnlocked += RefreshTree;
            researchManager.OnResearchCancelled += RefreshTree;
        }

        private void OnDisable()
        {
            if (researchManager == null)
                return;

            researchManager.OnResearchStarted -= RefreshTree;
            researchManager.OnResearchCompleted -= RefreshTree;
            researchManager.OnResearchUnlocked -= RefreshTree;
            researchManager.OnResearchCancelled -= RefreshTree;
        }
        private void Update()
        {
            foreach (ResearchNodeUI node in nodes.Values)
            {
                ResearchState state =
                    researchManager.GetState(node.Research.ID);

                if (state == null)
                    continue;

                if (state.IsResearching)
                    node.Refresh();
            }

            if (selectedNode != null)
            {
                ResearchState state =
                    researchManager.GetState(selectedNode.Research.ID);

                if (state != null && state.IsResearching)
                    itemCard.Show(selectedNode.Research);
            }
        }

        private void SelectFirstAvailableResearch()
        {
            foreach (ResearchNodeUI node in nodes.Values)
            {
                ResearchState state =
                    researchManager.GetState(node.Research.ID);

                if (state == null)
                    continue;

                if (state.IsUnlocked || state.IsResearching || state.IsCompleted)
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
                if (!layoutLookup.TryGetValue(research.ID, out ResearchLayout layout))
                    continue;

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
            foreach (ResearchSO research in database.Researches)
            {
                if (research.Prerequisites == null)
                    continue;

                foreach (ResearchSO prerequisite in research.Prerequisites)
                {
                    if (!nodes.TryGetValue(prerequisite.ID, out ResearchNodeUI parent))
                        continue;

                    if (!nodes.TryGetValue(research.ID, out ResearchNodeUI child))
                        continue;

                    Vector2 start = parent.GetConnectionPoint();
                    Vector2 end = child.GetConnectionPoint();

                    CreateHorizontalLine(start, end.x);
                    CreateVerticalLine(
                        new Vector2(end.x, start.y),
                        end.y);
                }
            }
        }
        private void CreateHorizontalLine(
    Vector2 start,
    float endX)
        {
            RectTransform line =
                Instantiate(horizontalLinePrefab, linesParent);

            float x = Mathf.Min(start.x, endX);
            float width = Mathf.Abs(endX - start.x);

            line.anchoredPosition =
                new Vector2(x, start.y);

            line.sizeDelta =
                new Vector2(width, 4f);
        }
        private void CreateVerticalLine(
    Vector2 start,
    float endY)
        {
            RectTransform line =
                Instantiate(verticalLinePrefab, linesParent);

            float y = Mathf.Max(start.y, endY);
            float height = Mathf.Abs(endY - start.y);

            line.anchoredPosition =
                new Vector2(start.x, y);

            line.sizeDelta =
                new Vector2(4f, height);
        }

        private void BuildLookup()
        {
            layoutLookup.Clear();

            foreach (ResearchLayout layout in layouts)
            {
                if (layout == null || layout.Research == null)
                    continue;

                layoutLookup[layout.Research.ID] = layout;
            }
        }
        private void RefreshTree(ResearchSO _)
        {
            foreach (ResearchNodeUI node in nodes.Values)
                node.Refresh();


            if (selectedNode != null)
                itemCard.Show(selectedNode.Research);
        }
        private void UpdateContentSize()
        {
            float maxX = 0;
            float maxY = 0;

            foreach (ResearchNodeUI node in nodes.Values)
            {
                RectTransform rect = node.RectTransform;

                maxX = Mathf.Max(
                    maxX,
                    rect.anchoredPosition.x + rect.rect.width);

                maxY = Mathf.Max(
                    maxY,
                    Mathf.Abs(rect.anchoredPosition.y) + rect.rect.height);
            }

            content.sizeDelta =
                new Vector2(
                    maxX + 300,
                    maxY + 300);
        }
        public ResearchNodeUI GetNode(string researchID)
        {
            nodes.TryGetValue(researchID, out ResearchNodeUI node);
            return node;
        }
        public void SelectNode(ResearchNodeUI node)
        {

            if (selectedNode != null)
                selectedNode.SetSelected(false);

            selectedNode = node;

            selectedNode.SetSelected(true);

            itemCard.Show(node.Research);
        }
    }
}