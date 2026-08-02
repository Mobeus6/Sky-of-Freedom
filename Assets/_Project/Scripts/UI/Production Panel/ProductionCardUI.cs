using SkyOfFreedom.Data;
using SkyOfFreedom.Managers;
using SkyOfFreedom.Production;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkyOfFreedom.UI
{
    public class ProductionCardUI : MonoBehaviour
    {
        [Header("Tier Style")]
        [SerializeField] private CardTierVisual visual;

        [Header("UI")]
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text produceButtonText;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private TMP_Text tierText;
        [SerializeField] private Button produceButton;

        private IProducible producible;
        private ProductionManager productionManager;

        private void Awake()
        {
            productionManager = GameManager.Instance.Production;

            produceButton.onClick.RemoveAllListeners();
            produceButton.onClick.AddListener(OnProduceClicked);
        }

        public void Setup(IProducible item)
        {
            producible = item;

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
        }

        private void OnProduceClicked()
        {
            if (producible == null)
                return;

            if (producible is ComponentSO component)
            {
                productionManager.QueueComponent(component);
                return;
            }

            if (producible is DroneModelSO drone)
            {
                productionManager.QueueDrone(drone);
            }
        }
    }
}