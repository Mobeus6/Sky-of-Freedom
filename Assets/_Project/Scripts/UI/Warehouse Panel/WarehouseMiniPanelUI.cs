using System.Collections.Generic;
using SkyOfFreedom.Data;
using SkyOfFreedom.Gameplay.Factory;
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
        [Header("Zone Level")]
        [SerializeField] private TMPro.TMP_Text zoneLevelText;

        private void LateUpdate()
        {
            if (zoneLevelText == null) return;
            var game = GameManager.Instance;
            string value = game != null && game.IsGameReady && game.Factory != null
                ? "Lv. " + game.Factory.GetLevel(SkyOfFreedom.Factory.FactoryZoneType.Warehouse)
                : "Lv. —";
            if (zoneLevelText.text != value) zoneLevelText.text = value;
        }

        [Header("Zone References")]
        [SerializeField]
        private FactoryZoneInteraction warehouseZoneInteraction;

        [Header("UI")]
        [SerializeField]
        private CanvasGroup canvasGroup;

        private PanelFadeUI panelFade;
        private bool animationReady;

        private void SetPanelVisible(bool visible, bool animate)
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (panelFade == null)
            {
                panelFade = canvasGroup.GetComponent<PanelFadeUI>();
                if (panelFade == null)
                    panelFade = canvasGroup.gameObject.AddComponent<PanelFadeUI>();
            }
            panelFade.SetVisible(visible, animate && isActiveAndEnabled);
        }

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

        /// <summary>Rebinds world interaction without replacing gameplay state.</summary>
        public void BindZone(FactoryZoneInteraction zone)
        {
            if (warehouseZoneInteraction == zone)
            {
                return;
            }

            if (warehouseZoneInteraction != null)
            {
                warehouseZoneInteraction.ZoneSelected -= OnZoneSelected;
                warehouseZoneInteraction.ZoneDeselected -= OnZoneDeselected;
            }

            warehouseZoneInteraction = zone;
            if (isActiveAndEnabled && warehouseZoneInteraction != null)
            {
                warehouseZoneInteraction.ZoneSelected += OnZoneSelected;
                warehouseZoneInteraction.ZoneDeselected += OnZoneDeselected;
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
            Hide();
        }

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
            // Start hidden without a flash; selection below may reopen the panel.
            SetPanelVisible(false, false);
            animationReady = true;
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

            if (warehouseZoneInteraction != null)
            {
                warehouseZoneInteraction.ZoneSelected +=
                    OnZoneSelected;

                warehouseZoneInteraction.ZoneDeselected +=
                    OnZoneDeselected;
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

            if (warehouseZoneInteraction != null && FactoryZoneInteraction.SelectedZone ==
                warehouseZoneInteraction)
            {
                Open();
            }
        }

        private void OnDisable()
        {
            animationReady = false;
            if (panelFade != null) panelFade.SetVisible(false, false);
            if (warehouseZoneInteraction != null)
            {
                warehouseZoneInteraction.ZoneSelected -=
                    OnZoneSelected;

                warehouseZoneInteraction.ZoneDeselected -=
                    OnZoneDeselected;
            }

            if (warehouse != null)
            {
                warehouse.OnItemAdded -=
                    OnItemAdded;

                warehouse.OnItemRemoved -=
                    OnWarehouseChanged;

                warehouse.OnItemChanged -=
                    OnWarehouseChanged;
            }
        }

        private void OnZoneSelected(
            FactoryZoneInteraction zone)
        {
            if (zone != warehouseZoneInteraction)
            {
                return;
            }

            Refresh();
            Open();
        }

        private void OnZoneDeselected(
            FactoryZoneInteraction zone)
        {
            if (zone != warehouseZoneInteraction)
            {
                return;
            }

            Hide();
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
                    ? items[0]
                    : "-");

            SetRecentItemText(
                recentItem2Text,
                items.Length > 1
                    ? items[1]
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

        public void Open()
        {
            if (canvasGroup == null)
            {
                return;
            }

            SetPanelVisible(true, animationReady);

            Refresh();
        }

        public void Hide()
        {
            if (canvasGroup == null)
            {
                return;
            }

            SetPanelVisible(false, animationReady);
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
