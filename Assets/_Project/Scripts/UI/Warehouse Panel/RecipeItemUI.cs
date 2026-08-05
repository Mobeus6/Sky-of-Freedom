using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkyOfFreedom.UI
{
    public class RecipeItemUI : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text quantityText;

        [SerializeField] private CardTierVisual tierVisual;

        public void Setup(Sprite sprite, int tier, int quantity)
        {
            if (icon != null)
                icon.sprite = sprite;

            if (quantityText != null)
                quantityText.text = quantity.ToString();

            if (tierVisual != null)
                tierVisual.SetTier(tier);
        }
    }
}