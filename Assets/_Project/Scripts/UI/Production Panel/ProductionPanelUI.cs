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
        [SerializeField] private ProductionCardUI cardPrefab;

        [Header("Buttons")]
        [SerializeField] private CategoryButtonUI[] categoryButtons;

        private readonly List<ProductionCardUI> spawnedCards = new();

        private ProductionManager productionManager;

        private ProductionView currentView = ProductionView.Components;
        private ProductionCategory currentCategory = ProductionCategory.All;

        private void Start()
        {
            productionManager = GameManager.Instance.Production;

            foreach (CategoryButtonUI button in categoryButtons)
            {
                button.Initialize(this);
            }

            Refresh();
        }

        public void OpenCategory(ProductionCategory category)
        {
            currentCategory = category;
            Refresh();
        }

        public void ShowComponents()
        {
            currentView = ProductionView.Components;
            currentCategory = ProductionCategory.All;

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
                ProductionCardUI card =
                    Instantiate(cardPrefab, content);

                card.Setup(item);

                spawnedCards.Add(card);
            }
            Debug.Log(content);
            Debug.Log(cardPrefab);
            Debug.Log(productionManager);
        }

        private void ClearCards()
        {
            foreach (ProductionCardUI card in spawnedCards)
            {
                if (card != null)
                    Destroy(card.gameObject);
            }

            spawnedCards.Clear();
        }
    }
}