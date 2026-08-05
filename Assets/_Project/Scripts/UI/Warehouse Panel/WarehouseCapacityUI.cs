using SkyOfFreedom.Managers;
using SkyOfFreedom.Warehouse;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkyOfFreedom.UI
{
    public class WarehouseCapacityUI : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Slider capacitySlider;
        [SerializeField] private TMP_Text capacityText;
        [SerializeField] private TMP_Text percentText;

        private WarehouseManager warehouse;

        private void Start()
        {
            warehouse = GameManager.Instance.Warehouse;

            warehouse.OnItemAdded += OnWarehouseChanged;
            warehouse.OnItemRemoved += OnWarehouseChanged;
            warehouse.OnItemChanged += OnWarehouseItemChanged;

            Refresh();
        }

        private void OnDestroy()
        {
            if (warehouse == null)
                return;

            warehouse.OnItemAdded -= OnWarehouseChanged;
            warehouse.OnItemRemoved -= OnWarehouseChanged;
            warehouse.OnItemChanged -= OnWarehouseItemChanged;
        }

        private void OnWarehouseChanged(string id, int amount)
        {
            Refresh();
        }

        private void OnWarehouseItemChanged(string id, int quantity)
        {
            Refresh();
        }

        public void Refresh()
        {
            if (warehouse == null)
                return;

            int current = warehouse.CurrentCapacity;
            int max = warehouse.MaxCapacity;

            capacitySlider.value = warehouse.CapacityPercent;

            capacityText.text =
                $"{current:N0} / {max:N0}";

            percentText.text =
                $"{warehouse.CapacityPercent * 100f:0}%";
        }
    }
}