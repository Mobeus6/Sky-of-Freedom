using System.Collections.Generic;
using UnityEngine;
using SkyOfFreedom.Data;
using SkyOfFreedom.Managers;
using SkyOfFreedom.Production;

namespace SkyOfFreedom.UI
{
    public class ProductionPanelUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform content;
        [SerializeField] private CatalogueCardUI cardPrefab;

        [Header("Buttons")]
        [SerializeField] private CategoryButtonUI[] categoryButtons;

        private readonly List<CatalogueCardUI> spawnedCards = new();

        private ProductionManager productionManager;

        private ProductionView currentView = ProductionView.Components;
        private CatalogCategory currentCategory = CatalogCategory.All;

        private void Start()
        {
            productionManager = GameManager.Instance.Production;

            foreach (CategoryButtonUI button in categoryButtons)
            {
                button.Initialize(this);
            }

            Refresh();
        }

        public void OpenCategory(CatalogCategory category)
        {
            currentView = ProductionView.Components;
            currentCategory = category;

            Refresh();
        }

        public void ShowComponents()
        {
            currentView = ProductionView.Components;
            currentCategory = CatalogCategory.All;

            Refresh();
        }

        public void ShowDrones()
        {
            currentView = ProductionView.Drones;

            Refresh();
        }

        private void Refresh()
        {
            ClearCards();

            List<IProducible> items =
                productionManager.GetAvailableItems(
                    currentView,
                    currentCategory);

            foreach (IProducible item in items)
            {
                CatalogueCardUI card =
                    Instantiate(cardPrefab, content);

                card.Setup(item);

                spawnedCards.Add(card);
            }
        }

        private void ClearCards()
        {
            foreach (CatalogueCardUI card in spawnedCards)
            {
                if (card != null)
                    Destroy(card.gameObject);
            }

            spawnedCards.Clear();
        }
    }
}