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

        private Button purchaseButton;

        public void EnablePurchase(UnityEngine.Events.UnityAction openPurchase)
        {
            if (icon == null || openPurchase == null)
                return;

            purchaseButton = icon.GetComponent<Button>();
            if (purchaseButton == null)
                purchaseButton = icon.gameObject.AddComponent<Button>();
            purchaseButton.targetGraphic = icon;
            icon.raycastTarget = true;
            purchaseButton.onClick.RemoveAllListeners();
            purchaseButton.onClick.AddListener(openPurchase);
            purchaseButton.interactable = true;
            if (quantityText != null)
            {
                quantityText.text += "\nBuy";
                quantityText.raycastTarget = false;
            }
        }

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
