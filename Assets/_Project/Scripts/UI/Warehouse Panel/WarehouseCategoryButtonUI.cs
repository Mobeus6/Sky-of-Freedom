using SkyOfFreedom.Data;
using UnityEngine;
using UnityEngine.UI;

namespace SkyOfFreedom.UI
{
    [RequireComponent(typeof(Button))]
    public class WarehouseCategoryButtonUI : MonoBehaviour
    {
        [SerializeField] private CatalogCategory category;

        private Button button;
        private WarehousePanelUI panel;

        public void Initialize(WarehousePanelUI warehousePanel)
        {
            panel = warehousePanel;

            button = GetComponent<Button>();

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            Debug.Log($"Category: {category}");

            panel.OpenCategory(category);
        }
    }
}