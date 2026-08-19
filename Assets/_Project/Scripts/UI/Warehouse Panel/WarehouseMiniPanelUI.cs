using System.Collections.Generic;
using SkyOfFreedom.Data;
using SkyOfFreedom.Managers;
using SkyOfFreedom.Warehouse;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkyOfFreedom.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class WarehouseMiniPanelUI : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField]
        private CanvasGroup canvasGroup;

        [SerializeField]
        private Slider capacitySlider;

        [SerializeField]
        private TMP_Text capacityText;

        [SerializeField]
        private TMP_Text capacityPercentText;

        [Header("Warehouse Info")]
        [SerializeField]
        private TMP_Text materialsText;

        [SerializeField]
        private TMP_Text componentsText;

        [SerializeField]
        private TMP_Text dronesText;

        [Header("Recently Added")]
        [SerializeField]
        private TMP_Text recentItem1Text;

        [SerializeField]
        private TMP_Text recentItem2Text;

        [Header("Open Warehouse")]
        [SerializeField]
        private Button openWarehouseButton;

        [SerializeField]
        private MenuManager menuManager;

        [SerializeField]
        private MenuButton warehouseMenuButton;

        private WarehouseManager warehouse;
        private DatabaseManager database;

        private readonly Queue<string> recentItems =
            new Queue<string>();

        private const int MaxRecentItems = 2;

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup =
                    GetComponent<CanvasGroup>();
            }

            if (GameManager.Instance != null)
            {
                warehouse =
                    GameManager.Instance.Warehouse;

                database =
                    GameManager.Instance.Database;
            }

            Hide();

            if (openWarehouseButton != null)
            {
                openWarehouseButton.onClick.RemoveAllListeners();

                openWarehouseButton.onClick.AddListener(
                    OpenWarehousePanel);
            }
        }

        private void OnEnable()
        {
            if (warehouse == null &&
                GameManager.Instance != null)
            {
                warehouse =
                    GameManager.Instance.Warehouse;
            }

            if (database == null &&
                GameManager.Instance != null)
            {
                database =
                    GameManager.Instance.Database;
            }

            if (warehouse != null)
            {
                warehouse.OnItemAdded +=
                    OnItemAdded;

                warehouse.OnItemRemoved +=
                    OnWarehouseChanged;

                warehouse.OnItemChanged +=
                    OnWarehouseChanged;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (warehouse == null)
            {
                return;
            }

            warehouse.OnItemAdded -=
                OnItemAdded;

            warehouse.OnItemRemoved -=
                OnWarehouseChanged;

            warehouse.OnItemChanged -=
                OnWarehouseChanged;
        }

        private void OnItemAdded(
            string id,
            int quantity)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return;
            }

            AddRecentItem(id);

            Refresh();
        }

        private void OnWarehouseChanged(
            string id,
            int quantity)
        {
            Refresh();
        }

        public void Refresh()
        {
            if (warehouse == null)
            {
                return;
            }

            RefreshCapacity();
            RefreshCategoryCounts();
            RefreshRecentItems();
        }

        private void RefreshCapacity()
        {
            int current =
                warehouse.CurrentCapacity;

            int max =
                warehouse.MaxCapacity;

            float percent =
                warehouse.CapacityPercent;

            if (capacitySlider != null)
            {
                capacitySlider.value =
                    Mathf.Clamp01(percent);
            }

            if (capacityText != null)
            {
                capacityText.text =
                    $"{current:N0} / {max:N0}";
            }

            if (capacityPercentText != null)
            {
                capacityPercentText.text =
                    $"{percent * 100f:0}%";
            }
        }

        private void RefreshCategoryCounts()
        {
            int materials = 0;
            int components = 0;
            int drones = 0;

            if (database == null ||
                database.Database == null)
            {
                SetCategoryText(
                    materialsText,
                    materials);

                SetCategoryText(
                    componentsText,
                    components);

                SetCategoryText(
                    dronesText,
                    drones);

                return;
            }

            foreach (WarehouseItem item
                     in warehouse.GetAllItems())
            {
                if (item == null)
                {
                    continue;
                }

                DataSO data =
                    database.Database.GetData(
                        item.ID);

                if (data is MaterialSO)
                {
                    materials +=
                        item.Quantity;

                    continue;
                }

                if (data is ComponentSO)
                {
                    components +=
                        item.Quantity;

                    continue;
                }

                if (data is DroneModelSO)
                {
                    drones +=
                        item.Quantity;
                }
            }

            SetCategoryText(
                materialsText,
                materials);

            SetCategoryText(
                componentsText,
                components);

            SetCategoryText(
                dronesText,
                drones);
        }

        private void SetCategoryText(
            TMP_Text text,
            int quantity)
        {
            if (text == null)
            {
                return;
            }

            text.text =
                quantity.ToString("N0");
        }

        private void AddRecentItem(
            string id)
        {
            List<string> items =
                new List<string>(
                    recentItems);

            items.Remove(id);
            items.Insert(0, id);

            if (items.Count > MaxRecentItems)
            {
                items.RemoveRange(
                    MaxRecentItems,
                    items.Count - MaxRecentItems);
            }

            recentItems.Clear();

            foreach (string item in items)
            {
                recentItems.Enqueue(item);
            }
        }

        private void RefreshRecentItems()
        {
            string[] items =
                recentItems.ToArray();

            SetRecentItemText(
                recentItem1Text,
                items.Length > 0
                    ? GetDisplayName(items[0])
                    : "-");

            SetRecentItemText(
                recentItem2Text,
                items.Length > 1
                    ? GetDisplayName(items[1])
                    : "-");
        }

        private void SetRecentItemText(
            TMP_Text text,
            string value)
        {
            if (text == null)
            {
                return;
            }

            text.text =
                value;
        }

        private string GetDisplayName(
            string id)
        {
            if (database == null ||
                database.Database == null)
            {
                return id;
            }

            DataSO data =
                database.Database.GetData(id);

            if (data == null)
            {
                return id;
            }

            if (data is MaterialSO material)
            {
                return material.MaterialName;
            }

            if (data is ComponentSO component)
            {
                return component.Name;
            }

            if (data is DroneModelSO drone)
            {
                return drone.Name;
            }

            return id;
        }

        public void Open()
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            Refresh();
        }

        public void Hide()
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        public void OpenWarehousePanel()
        {
            if (menuManager == null)
            {
                Debug.LogError(
                    "WarehouseMiniPanelUI: MenuManager is not assigned.",
                    this);

                return;
            }

            if (warehouseMenuButton == null)
            {
                Debug.LogError(
                    "WarehouseMiniPanelUI: Warehouse MenuButton is not assigned.",
                    this);

                return;
            }

            Hide();

            menuManager.Toggle(
                warehouseMenuButton);
        }
    }
}