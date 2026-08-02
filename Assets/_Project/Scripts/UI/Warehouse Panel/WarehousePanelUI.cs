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
            currentView = WarehouseView.Materials;

            if (categoryPanel != null)
                categoryPanel.SetActive(false);

            Refresh();
        }

        public void ShowComponents()
        {
            currentView = WarehouseView.Components;
            currentCategory = ComponentCategory.All;

            if (categoryPanel != null)
                categoryPanel.SetActive(true);

            Refresh();
        }

        public void ShowDrones()
        {
            currentView = WarehouseView.Drones;

            if (categoryPanel != null)
                categoryPanel.SetActive(false);

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
                DataSO data = FindData(item.ID);

                if (data == null)
                    continue;

                if (!Matches(data))
                    continue;

                WarehouseCardUI card = Instantiate(cardPrefab, content);

                card.Setup(data, item.Quantity);

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

        private DataSO FindData(string id)
        {
            foreach (MaterialSO material in database.Database.Materials)
            {
                if (material.ID == id)
                    return material;
            }

            foreach (ComponentSO component in database.Database.Components)
            {
                if (component.ID == id)
                    return component;
            }

            foreach (DroneModelSO drone in database.Database.DroneModels)
            {
                if (drone.ID == id)
                    return drone;
            }

            return null;
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
    }
}