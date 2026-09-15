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
            ProductionRecipePopupUI.CloseFor(this);
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
            {
                SelectCard();
                if (producible != null)
                    ProductionRecipePopupUI.Show(this, producible,
                        Mathf.Max(1, selectedQuantity), nameText != null ? nameText.font : null);
            }
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
        private Graphic[] cardGraphics;
        private Color[] normalColors;
        private bool? shownLocked;

        private void RefreshLockedAppearance(bool locked)
        {
            if (shownLocked == locked) return;
            shownLocked = locked;
            if (cardGraphics == null)
            {
                cardGraphics = GetComponentsInChildren<Graphic>(true);
                normalColors = new Color[cardGraphics.Length];
                for (int i = 0; i < cardGraphics.Length; i++)
                    normalColors[i] = cardGraphics[i].color;
            }
            for (int i = 0; i < cardGraphics.Length; i++)
            {
                if (cardGraphics[i] == null || cardGraphics[i] == selectionBackground) continue;
                Color original = normalColors[i];
                float gray = Mathf.Clamp(original.grayscale, 0.18f, 0.65f);
                cardGraphics[i].color = locked
                    ? new Color(gray, gray, gray, original.a * 0.75f) : original;
            }
            if (producible != null && descriptionText != null)
                descriptionText.text = locked
                    ? $"Requires Factory Lv. {Mathf.Max(1, producible.Tier)}\n{producible.Description}"
                    : producible.Description;
        }
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
            if (shownLocked == true) RefreshLockedAppearance(false);
            cardGraphics = null;
            shownLocked = null;
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
            int available = ProductionRecipeProcessor.GetMaxQuantity(producible, maxQuantity);
            selectedQuantity = available == 0 ? 0 : Mathf.Clamp(selectedQuantity, 1, available);
            if (quantityText != null)
                quantityText.text = selectedQuantity.ToString();
            if (increaseQuantityButton != null)
                increaseQuantityButton.interactable = selectedQuantity < available;
            if (decreaseQuantityButton != null)
                decreaseQuantityButton.interactable = selectedQuantity > 1;
            if (producible == null)
                return;
            bool levelAllowed = ProductionManager.MeetsFactoryLevel(producible);
            RefreshLockedAppearance(!levelAllowed);
            if (increaseQuantityButton != null)
                increaseQuantityButton.interactable &= levelAllowed;
            if (decreaseQuantityButton != null)
                decreaseQuantityButton.interactable &= levelAllowed;
            if (produceButton != null)
                produceButton.interactable = available > 0 && levelAllowed &&
                    GameManager.Instance != null && GameManager.Instance.License != null &&
                    GameManager.Instance.License.CanProduce(producible);
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

        private float nextQuantityRefresh;
        private void Update()
        {
            if (Time.unscaledTime < nextQuantityRefresh) return;
            nextQuantityRefresh = Time.unscaledTime + 0.2f;
            RefreshQuantity();
        }


        private void OnProduceClicked()
        {
            SelectCard();
            RefreshQuantity();
            if (selectedQuantity <= 0) return;
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
