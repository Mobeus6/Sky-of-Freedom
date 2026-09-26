using SkyOfFreedom.Production;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SkyOfFreedom.UI
{
    [DisallowMultipleComponent]
    public sealed class ProductionItemLinkUI : MonoBehaviour, IPointerClickHandler
    {
        private IProducible item;

        public static void Bind(Component owner, IProducible target)
        {
            if (owner == null) return;
            if (!owner.TryGetComponent<ProductionItemLinkUI>(out var link))
                link = owner.gameObject.AddComponent<ProductionItemLinkUI>();
            link.item = target;
            if (owner.TryGetComponent<Graphic>(out var graphic)) graphic.raycastTarget = true;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (item == null || eventData.button != PointerEventData.InputButton.Left ||
                eventData.dragging || !eventData.eligibleForClick) return;
            foreach (var panel in Resources.FindObjectsOfTypeAll<ProductionPanelUI>())
            {
                if (panel.gameObject.scene == gameObject.scene && panel.OpenItem(item)) return;
            }
            PlayerMessageUI.Show(this, "Production is unavailable.");
        }
    }
}
