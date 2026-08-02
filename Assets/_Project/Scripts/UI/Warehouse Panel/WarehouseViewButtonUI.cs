using SkyOfFreedom.Warehouse;
using UnityEngine;
using UnityEngine.UI;

namespace SkyOfFreedom.UI
{
    [RequireComponent(typeof(Button))]
    public class WarehouseViewButtonUI : MonoBehaviour
    {
        [SerializeField] private WarehouseView view;

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
            switch (view)
            {
                case WarehouseView.Materials:
                    panel.ShowMaterials();
                    break;

                case WarehouseView.Components:
                    panel.ShowComponents();
                    break;

                case WarehouseView.Drones:
                    panel.ShowDrones();
                    break;
            }
        }
    }
}