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

        [Header("Category Buttons")]
        [SerializeField] private CategoryButtonUI[] categoryButtons;

        private readonly List<ProductionCardUI> spawnedCards =
            new List<ProductionCardUI>();

        private ProductionManager productionManager;

        private ProductionView currentView =
            ProductionView.Components;

        private ComponentCategory currentCategory =
            ComponentCategory.All;

        private bool hasPendingView;

        private void Start()
        {
            productionManager =
                GameManager.Instance.Production;

            foreach (CategoryButtonUI button in categoryButtons)
            {
                if (button != null)
                    button.Initialize(this);
            }

            Refresh();
        }

        private void OnEnable()
        {
            if (!hasPendingView)
                return;

            hasPendingView = false;

            Refresh();
        }

        public void OpenProductionFromMiniPanel()
        {
            currentView =
                ProductionView.Components;

            currentCategory =
                ComponentCategory.All;

            hasPendingView = true;

            if (isActiveAndEnabled)
                Refresh();
        }

        public void OpenAssemblyFromMiniPanel()
        {
            currentView =
                ProductionView.Drones;

            currentCategory =
                ComponentCategory.All;

            hasPendingView = true;

            if (isActiveAndEnabled)
                Refresh();
        }

        public void ShowComponents()
        {
            currentView =
                ProductionView.Components;

            currentCategory =
                ComponentCategory.All;

            Refresh();
        }

        public void ShowDrones()
        {
            currentView =
                ProductionView.Drones;

            Refresh();
        }

        public void OpenCategory(
            ComponentCategory category)
        {
            currentView =
                ProductionView.Components;

            currentCategory =
                category;

            Refresh();
        }

        private void Refresh()
        {
            if (productionManager == null)
            {
                if (GameManager.Instance == null)
                    return;

                productionManager =
                    GameManager.Instance.Production;

                if (productionManager == null)
                    return;
            }

            ClearCards();

            List<IProducible> items =
                productionManager.GetAvailableItems(
                    currentView,
                    currentCategory);

            foreach (IProducible item in items)
            {
                ProductionCardUI card =
                    Instantiate(
                        cardPrefab,
                        content);

                card.Setup(item);

                spawnedCards.Add(card);
            }
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