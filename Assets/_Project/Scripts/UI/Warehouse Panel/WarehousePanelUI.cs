using System.Collections.Generic;
using SkyOfFreedom.Data;
using SkyOfFreedom.Managers;
using SkyOfFreedom.Warehouse;
using UnityEngine;

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

        private void Start()
        {
            warehouse = GameManager.Instance.Warehouse;
            database = GameManager.Instance.Database;

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

            ShowMaterials();
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
            Clear();

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
                    Destroy(card.gameObject);
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