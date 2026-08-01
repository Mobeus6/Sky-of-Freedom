using SkyOfFreedom.Data;
using SkyOfFreedom.Managers;
using SkyOfFreedom.Production;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkyOfFreedom.UI
{
    public class CatalogueCardUI : MonoBehaviour
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
            if (item is DroneModelSO)
            {
                produceButtonText.text = "Assemble";
            }
            else
            {
                produceButtonText.text = "Produce";
            }
            nameText.text = item.Name;
            descriptionText.text = item.Description;

            icon.sprite = item.Icon;
            icon.enabled = item.Icon != null;

            costText.text = $"{item.ProductionCost:N0} ₴";
            timeText.text = $"{item.ProductionTime:0.#} s";
            Debug.Log($"{item.Name} -> Tier = {item.Tier}");
            visual.SetTier(item.Tier);

            if (tierText != null)
                tierText.text = $"T{item.Tier}";
        }

        public event Action<IProducible> Clicked;

        private void OnProduceClicked()
        {
            Clicked?.Invoke(producible);
        }
    }
}