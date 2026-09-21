using System.Collections;
using System.Collections.Generic;
using SkyOfFreedom.Data;
using SkyOfFreedom.Managers;
using SkyOfFreedom.Warehouse;
using UnityEngine;
using UnityEngine.UI;

namespace SkyOfFreedom.UI
{
    public class WarehousePanelUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform content;
        [SerializeField] private WarehouseCardUI cardPrefab;
        [SerializeField] private WarehouseInfoPanelUI infoPanel;
        [Header("View Buttons")]
        [SerializeField] private WarehouseViewButtonUI[] viewButtons;

        [Header("Category Buttons")]
        [SerializeField] private WarehouseCategoryButtonUI[] categoryButtons;

        [Header("UI")]
        [SerializeField] private GameObject categoryPanel;

        private readonly List<WarehouseCardUI> cards = new();

        private WarehouseManager warehouse;
        private DatabaseManager database;

        private WarehouseView currentView = WarehouseView.Materials;
        private ComponentCategory currentCategory = ComponentCategory.All;
        private bool initialized;
        private Coroutine focusRoutine;

        private void Awake()
        {
        }

        private void Start()
        {
            if (initialized) return;
            if (!Initialize()) return;
            ShowMaterials();
        }

        private bool Initialize()
        {
            if (initialized) return true;
            if (GameManager.Instance == null) return false;
            warehouse = GameManager.Instance.Warehouse;
            database = GameManager.Instance.Database;
            if (warehouse == null || database == null) return false;

            warehouse.OnItemChanged += OnItemChanged;

            foreach (WarehouseViewButtonUI button in viewButtons)
            {
                if (button != null)
                    button.Initialize(this);
            }

            foreach (WarehouseCategoryButtonUI button in categoryButtons)
            {
                if (button != null)
                    button.Initialize(this);
            }

            initialized = true;
            return true;
        }

        public bool OpenMaterial(MaterialSO material)
        {
            if (material == null || !Initialize() || infoPanel == null) return false;
            MenuManager menu = FindAnyObjectByType<MenuManager>(FindObjectsInactive.Include);
            if (menu == null || !menu.OpenPanel(gameObject)) return false;
            ShowMaterials();
            OnCardSelected(material);
            if (focusRoutine != null) StopCoroutine(focusRoutine);
            focusRoutine = StartCoroutine(FocusMaterial(material.ID));
            return true;
        }

        public static bool TryOpenMaterial(MaterialSO material)
        {
            WarehousePanelUI panel = FindAnyObjectByType<WarehousePanelUI>(FindObjectsInactive.Include);
            return panel != null && panel.OpenMaterial(material);
        }

        private IEnumerator FocusMaterial(string id)
        {
            // Destroyed cards and layout groups settle at the end of the current frame.
            yield return null;
            Canvas.ForceUpdateCanvases();
            ScrollRect scroll = content.GetComponentInParent<ScrollRect>();
            WarehouseCardUI card = cards.Find(candidate => candidate != null && candidate.ItemId == id);
            if (scroll != null && scroll.content != null && card != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
                RectTransform viewport = scroll.viewport != null ? scroll.viewport : (RectTransform)scroll.transform;
                float overflow = scroll.content.rect.height - viewport.rect.height;
                float top = -((RectTransform)card.transform).anchoredPosition.y;
                scroll.StopMovement();
                scroll.verticalNormalizedPosition = overflow > 0 ? 1f - Mathf.Clamp01(top / overflow) : 1f;
            }
            focusRoutine = null;
        }

        public void ShowMaterials()
        {

            SetView(WarehouseView.Materials);
        }

        public void ShowComponents()
        {

            SetView(WarehouseView.Components);
        }

        public void ShowDrones()
        {

            SetView(WarehouseView.Drones);
        }
        private void SetView(WarehouseView view)
        {
            currentView = view;

            if (view == WarehouseView.Components)
                currentCategory = ComponentCategory.All;

            Refresh();
        }
        public void OpenCategory(ComponentCategory category)
        {

            currentView = WarehouseView.Components;
            currentCategory = category;

            Refresh();
        }

        private void OnItemChanged(string id, int quantity)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (!Initialize()) return;
            Clear();

            if (currentView == WarehouseView.Materials)
            {
                var seen = new HashSet<string>();
                foreach (MaterialSO material in database.Database.Materials)
                {
                    if (material == null || string.IsNullOrEmpty(material.ID) ||
                        !seen.Add(material.ID))
                        continue;

                    WarehouseCardUI materialCard = Instantiate(cardPrefab, content);
                    materialCard.Setup(material, warehouse.GetQuantity(material.ID));
                    materialCard.Selected += OnCardSelected;
                    cards.Add(materialCard);
                }
                return;
            }

            foreach (WarehouseItem item in warehouse.GetAllItems())
            {

                DataSO data = database.Database.GetData(item.ID);


                if (data == null)
                    continue;

                if (!Matches(data))
                {
                    continue;
                }


                WarehouseCardUI card = Instantiate(cardPrefab, content);

                card.Setup(data, item.Quantity);
                card.Selected += OnCardSelected;

                cards.Add(card);
            }
        }

        private bool Matches(DataSO data)
        {
            switch (currentView)
            {
                case WarehouseView.Materials:

                    return data is MaterialSO;

                case WarehouseView.Components:

                    if (data is not ComponentSO component)
                        return false;

                    if (currentCategory == ComponentCategory.All)
                        return true;

                    return component.Category == currentCategory;

                case WarehouseView.Drones:

                    return data is DroneModelSO;
            }

            return false;
        }

        private void Clear()
        {
            foreach (WarehouseCardUI card in cards)
            {
                if (card != null)
                {
                    card.gameObject.SetActive(false);
                    Destroy(card.gameObject);
                }
            }

            cards.Clear();
        }

        private void OnDestroy()
        {
            if (warehouse != null)
                warehouse.OnItemChanged -= OnItemChanged;
        }
        private void OnCardSelected(DataSO data)
{
    if (data == null)
        return;

    int quantity = warehouse.GetQuantity(data.ID);

    infoPanel.Show(data, quantity);
}
    }
}
