using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
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
        private Coroutine focusRoutine;
        private string focusId;

        public bool OpenItem(IProducible item)
        {
            if (item == null || content == null || cardPrefab == null ||
                GameManager.Instance == null || !GameManager.Instance.IsGameReady) return false;
            bool opened = false;
            foreach (var menu in Resources.FindObjectsOfTypeAll<MenuManager>())
            {
                if (menu.gameObject.scene == gameObject.scene && menu.OpenPanel(gameObject))
                {
                    opened = true;
                    break;
                }
            }
            if (!opened || !isActiveAndEnabled) return false;
            currentView = item is DroneModelSO ? ProductionView.Drones : ProductionView.Components;
            currentCategory = item is ComponentSO component ? component.Category : ComponentCategory.All;
            focusId = item.ID;
            hasPendingView = false;
            Refresh();
            if (focusRoutine != null) StopCoroutine(focusRoutine);
            focusRoutine = StartCoroutine(FocusItem());
            return true;
        }

        private IEnumerator FocusItem()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            var card = spawnedCards.Find(candidate => candidate != null && candidate.ItemId == focusId);
            if (card != null)
            {
                card.SelectCard();
                var scroll = content.GetComponentInParent<ScrollRect>();
                if (scroll != null && scroll.content != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
                    var viewport = scroll.viewport != null ? scroll.viewport : (RectTransform)scroll.transform;
                    Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, card.transform);
                    Bounds contentBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, scroll.content);
                    Vector3 delta = bounds.center - (Vector3)viewport.rect.center;
                    scroll.StopMovement();
                    float height = contentBounds.size.y - viewport.rect.height;
                    float width = contentBounds.size.x - viewport.rect.width;
                    if (scroll.vertical && height > 0)
                        scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition + delta.y / height);
                    if (scroll.horizontal && width > 0)
                        scroll.horizontalNormalizedPosition = Mathf.Clamp01(scroll.horizontalNormalizedPosition + delta.x / width);
                }
            }
            focusId = null;
            focusRoutine = null;
        }


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
