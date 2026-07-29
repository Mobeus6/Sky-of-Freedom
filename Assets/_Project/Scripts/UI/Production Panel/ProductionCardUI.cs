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
        [Header("UI")]
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private Button produceButton;
        [SerializeField] private TMP_Text descriptionText;

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
            descriptionText.text = item.Description;
            nameText.text = item.Name;

            icon.sprite = item.Icon;
            icon.enabled = item.Icon != null;

            costText.text = $"{item.ProductionCost:N0} ₴";

            timeText.text = $"{item.ProductionTime:0.#} s";
        }

        private void OnProduceClicked()
        {
            if (producible == null)
                return;

            productionManager.QueueProduction(producible);
        }
    }
}