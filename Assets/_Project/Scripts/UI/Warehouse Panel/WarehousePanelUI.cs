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

        [Header("Categories")]
        [SerializeField] private WarehouseCategoryButtonUI[] categoryButtons;

        private readonly List<WarehouseCardUI> cards = new();

        private WarehouseManager warehouse;
        private DatabaseManager database;

        private CatalogCategory currentCategory = CatalogCategory.All;

        private void Start()
        {
            warehouse = GameManager.Instance.Warehouse;
            database = GameManager.Instance.Database;

            warehouse.OnItemChanged += OnItemChanged;

            foreach (WarehouseCategoryButtonUI button in categoryButtons)
            {
                button.Initialize(this);
            }

            Refresh();
        }

        public void OpenCategory(CatalogCategory category)
        {
            Debug.Log($"OpenCategory: {category}");

            currentCategory = category;

            Refresh();
        }

        private void OnItemChanged(string id, int quantity)
        {
            Refresh();
        }

        public void Refresh()
        {
            Clear();

            foreach (WarehouseItem item in warehouse.GetAllItems())
            {
                DataSO data = FindData(item.ID);

                if (data == null)
                    continue;

                if (!MatchesCategory(data))
                    continue;

                WarehouseCardUI card =
                    Instantiate(cardPrefab, content);

                card.Setup(data, item.Quantity);

                cards.Add(card);
            }
        }

        private bool MatchesCategory(DataSO data)
        {
            if (currentCategory == CatalogCategory.All)
                return true;

            if (currentCategory == CatalogCategory.Components)
                return data is ComponentSO;

            return data.CatalogCategory == currentCategory;
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

        private void OnDestroy()
        {
            if (warehouse != null)
                warehouse.OnItemChanged -= OnItemChanged;
        }
    }
}