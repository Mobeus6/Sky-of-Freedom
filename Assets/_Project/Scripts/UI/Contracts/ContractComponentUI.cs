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

            SetCommonData(
                component.Icon,
                component.Name,
                component.Tier,
                quantity);
        }

        public void Setup(
            MaterialSO material,
            int quantity)
        {
            if (material == null)
            {
                return;
            }

            SetCommonData(
                material.Icon,
                material.MaterialName,
                material.Tier,
                quantity);
        }

        private void SetCommonData(
            Sprite sprite,
            string itemName,
            int tier,
            int quantity)
        {
            if (icon != null)
            {
                icon.sprite = sprite;
                icon.enabled = sprite != null;
            }

            if (nameText != null)
            {
                nameText.text = itemName;
            }

            if (quantityText != null)
            {
                quantityText.text =
                    $"Quantity {quantity}";
            }

            if (tierText != null)
            {
                tierText.text =
                    $"T{tier}";
            }

            if (tierVisual != null)
            {
                tierVisual.SetTier(tier);
            }
        }
    }
}