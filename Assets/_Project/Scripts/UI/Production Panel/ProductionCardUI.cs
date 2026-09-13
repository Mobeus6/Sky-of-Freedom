using SkyOfFreedom.Data;
using SkyOfFreedom.Factory;
using SkyOfFreedom.Managers;
using SkyOfFreedom.Production;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace SkyOfFreedom.UI
{
    public class ProductionCardUI : MonoBehaviour, IPointerClickHandler
    {
        [Header("Tier Style")]
        [SerializeField] private CardTierVisual visual;

        [Header("Selection")]
        [SerializeField] private Image selectionBackground;
        private static ProductionCardUI selectedCard;

        private void OnEnable()
        {
            SetHighlight(false);
        }

        private void OnDisable()
        {
            if (selectedCard == this)
                selectedCard = null;
            SetHighlight(false);
        }

        private void SetHighlight(bool selected)
        {
            if (selectionBackground == null)
                return;
            selectionBackground.raycastTarget = false;
            selectionBackground.enabled = selected;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                SelectCard();
        }

        public void SelectCard()
        {
            if (!isActiveAndEnabled)
                return;
            if (selectedCard != null && selectedCard != this)
                selectedCard.SetHighlight(false);
            selectedCard = this;
            SetHighlight(true);
        }

        [Header("UI")]
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text produceButtonText;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private TMP_Text tierText;
        [SerializeField] private Button produceButton;

        [Header("Quantity")]
        [SerializeField] private Button increaseQuantityButton;
        [SerializeField] private Button decreaseQuantityButton;
        [SerializeField] private TMP_Text quantityText;
        [SerializeField, Min(1)] private int maxQuantity = 9999;

        private int selectedQuantity = 1;

        private IProducible producible;
        private ProductionManager productionManager;

        private void Awake()
        {
            productionManager = GameManager.Instance.Production;

            produceButton.onClick.RemoveAllListeners();
            produceButton.onClick.AddListener(OnProduceClicked);
            if (increaseQuantityButton != null)
                increaseQuantityButton.onClick.AddListener(IncreaseQuantity);
            if (decreaseQuantityButton != null)
                decreaseQuantityButton.onClick.AddListener(DecreaseQuantity);
        }

        public void Setup(IProducible item)
        {
            producible = item;
            selectedQuantity = 1;

            produceButtonText.text = item is DroneModelSO
                ? "Assemble"
                : "Produce";

            nameText.text = item.Name;
            descriptionText.text = item.Description;

            icon.sprite = item.Icon;
            icon.enabled = item.Icon != null;

            costText.text = $"{item.ProductionCost:N0} ₴";
            timeText.text = $"{item.ProductionTime:0.#} s";

            visual.SetTier(item.Tier);

            if (tierText != null)
                tierText.text = $"T{item.Tier}";

            RefreshQuantity();
        }

        private void IncreaseQuantity()
        {
            SelectCard();
            if (selectedQuantity < Mathf.Max(1, maxQuantity))
                selectedQuantity++;
            RefreshQuantity();
        }

        private void DecreaseQuantity()
        {
            SelectCard();
            if (selectedQuantity > 1)
                selectedQuantity--;
            RefreshQuantity();
        }

        private void RefreshQuantity()
        {
            selectedQuantity = Mathf.Clamp(selectedQuantity, 1, Mathf.Max(1, maxQuantity));
            if (quantityText != null)
                quantityText.text = selectedQuantity.ToString();
            if (increaseQuantityButton != null)
                increaseQuantityButton.interactable = selectedQuantity < Mathf.Max(1, maxQuantity);
            if (decreaseQuantityButton != null)
                decreaseQuantityButton.interactable = selectedQuantity > 1;
            if (producible == null)
                return;
            if (costText != null)
                costText.text = $"{(double)producible.ProductionCost * selectedQuantity:N0} ₴";
            if (timeText != null)
                timeText.text = $"{(double)producible.ProductionTime * selectedQuantity:0.#} s";
        }

        private void OnDestroy()
        {
            if (increaseQuantityButton != null)
                increaseQuantityButton.onClick.RemoveListener(IncreaseQuantity);
            if (decreaseQuantityButton != null)
                decreaseQuantityButton.onClick.RemoveListener(DecreaseQuantity);
            if (produceButton != null)
                produceButton.onClick.RemoveListener(OnProduceClicked);
        }

        private void OnProduceClicked()
        {
            SelectCard();
            if (producible == null || productionManager == null)
                return;

            if (producible is ComponentSO component)
            {
                productionManager.QueueProduction(
                    FactoryZoneType.Production, component, selectedQuantity);
                return;
            }

            if (producible is DroneModelSO drone)
            {
                productionManager.QueueProduction(
                    FactoryZoneType.Assembly, drone, selectedQuantity);
            }
        }
    }
}
