using SkyOfFreedom.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkyOfFreedom.UI.Contracts
{
    public class ContractComponentUI : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text quantityText;
        [SerializeField] private TMP_Text tierText;

        [Header("Tier Visual")]
        [SerializeField] private CardTierVisual tierVisual;

        public void Setup(
            ComponentSO component,
            int quantity)
        {
            if (component == null)
            {
                return;
            }

            if (icon != null)
            {
                icon.sprite = component.Icon;
                icon.enabled = component.Icon != null;
            }

            if (nameText != null)
            {
                nameText.text = component.Name;
            }

            if (quantityText != null)
            {
                quantityText.text =
                    $"Quantity {quantity}";
            }

            if (tierText != null)
            {
                tierText.text =
                    $"T{component.Tier}";
            }

            if (tierVisual != null)
            {
                tierVisual.SetTier(component.Tier);
            }
        }
    }
}