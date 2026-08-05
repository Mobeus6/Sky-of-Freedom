using SkyOfFreedom.Data;
using SkyOfFreedom.Managers;
using SkyOfFreedom.Production;
using SkyOfFreedom.Warehouse;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkyOfFreedom.UI
{
    public class WarehouseInfoPanelUI : MonoBehaviour
    {
        [Header("Common")]
        [SerializeField] private GameObject commonPanel;

        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text itemNameText;
        [SerializeField] private TMP_Text descriptionText;

        [SerializeField] private TMP_Text inStockText;
        [SerializeField] private TMP_Text storageSizeText;
        [SerializeField] private TMP_Text productionPriceText;
        [SerializeField] private TMP_Text badgeText;

        [Header("Material")]
        [SerializeField] private GameObject materialPanel;

        [SerializeField] private TMP_Text marketPriceText;
        [SerializeField] private TMP_Text differencePriceText;

        [SerializeField] private Button buyButton;
        [SerializeField] private Button sellButton;

        [Header("Component")]
        [SerializeField] private Transform componentRecipeContent;
        [SerializeField] private GameObject componentPanel;
        [SerializeField] private TMP_Text productionTimeText;
        [SerializeField] private Button produceComponentButton;

        [Header("Drone")]
        [SerializeField] private GameObject dronePanel;

        [SerializeField] private TMP_Text assemblyTimeText;
        [SerializeField] private Transform componentsContent;
        [SerializeField] private RecipeItemUI recipeItemPrefab;
        [SerializeField] private TMP_Text flightDistanceText;
        [SerializeField] private TMP_Text payloadText;
        [SerializeField] private TMP_Text durabilityText;
        [SerializeField] private TMP_Text navigationText;
        [SerializeField] private TMP_Text stealthText;
        [SerializeField] private CardTierVisual tierVisual;
        [SerializeField] private Button produceDroneButton;

        private WarehouseManager warehouse;
        private MarketManager market;
        private ProductionManager production;

        private DataSO currentData;

        private void Awake()
        {
            warehouse = GameManager.Instance.Warehouse;
            market = GameManager.Instance.Market;
            production = GameManager.Instance.Production;

            HideAll();
        }

        private void OnEnable()
        {
            if (market != null)
                market.OnPriceChanged += OnPriceChanged;
        }

        private void OnDisable()
        {
            if (market != null)
                market.OnPriceChanged -= OnPriceChanged;
        }

        public void Show(DataSO data, int quantity)
        {
            if (data == null)
            {
                HideAll();
                return;
            }

            currentData = data;

            HideAll();

            commonPanel.SetActive(true);

            switch (data)
            {
                case MaterialSO material:

                    materialPanel.SetActive(true);


                    itemNameText.text = material.MaterialName;
                    descriptionText.text = material.Description;
                    iconImage.sprite = material.Icon;
                    inStockText.text = quantity.ToString();
                    storageSizeText.text = material.StorageSize.ToString();
                    int tier = material.Tier;
                    tierVisual.SetTier(tier);
                    productionPriceText.text = "$ " + material.BasePrice;
                    badgeText.text = material.Tier.ToString();

                    ShowMaterial(material);

                    break;

                case ComponentSO component:

                    componentPanel.SetActive(true);

                    itemNameText.text = component.Name;
                    descriptionText.text = component.Description;

                    iconImage.sprite = component.Icon;

                    inStockText.text = quantity.ToString();
                    storageSizeText.text = component.StorageSize.ToString();
                    tierVisual.SetTier(component.Tier);

                    productionPriceText.text =
                        "$ " + component.ProductionCost;

                    badgeText.text =
                        $"T{component.Tier}";

                    ShowComponent(component);

                    break;

                case DroneModelSO drone:

                    dronePanel.SetActive(true);

                    itemNameText.text = drone.Name;
                    descriptionText.text = drone.Description;

                    iconImage.sprite = drone.Icon;

                    inStockText.text = quantity.ToString();
                    storageSizeText.text = drone.StorageSize.ToString();
                    tierVisual.SetTier(drone.Tier);
                    productionPriceText.text =
                        "$ " + drone.ProductionCost;

                    badgeText.text =
                        $"{drone.Platform}  T{drone.Tier}";

                    ShowDrone(drone);

                    break;
            }
        }
        private void ShowDroneRecipe(IReadOnlyList<DroneComponent> recipe)
        {
            ClearRecipe(componentsContent);

            foreach (DroneComponent component in recipe)
            {
                RecipeItemUI item =
                    Instantiate(recipeItemPrefab, componentsContent);

                item.Setup(
                    component.Component.Icon,
                    component.Component.Tier,
                    component.Amount);
            }
        }

        private void HideAll()
        {
            if (commonPanel != null)
                commonPanel.SetActive(false);

            if (materialPanel != null)
                materialPanel.SetActive(false);

            if (componentPanel != null)
                componentPanel.SetActive(false);

            if (dronePanel != null)
                dronePanel.SetActive(false);
        }

        private void OnPriceChanged(string id)
        {
            if (currentData == null)
                return;

            if (currentData.ID != id)
                return;

            int quantity =
                warehouse.GetQuantity(currentData.ID);

            Show(currentData, quantity);
        }
        private void ShowMaterial(MaterialSO material)
        {
            HideAll();

            commonPanel.SetActive(true);
            materialPanel.SetActive(true);

            marketPriceText.text =
                "$ " + market.GetCurrentPrice(material);

            float difference =
                market.GetPriceDifferencePercent(material);

            if (difference >= 0)
            {
                differencePriceText.color = Color.green;
                differencePriceText.text = $"+{difference:0.#}%";
            }
            else
            {
                differencePriceText.color = Color.red;
                differencePriceText.text = $"{difference:0.#}%";
            }

            buyButton.onClick.RemoveAllListeners();
            sellButton.onClick.RemoveAllListeners();

            buyButton.onClick.AddListener(() =>
            {
            });

            sellButton.onClick.AddListener(() =>
            {
            });
        }
        private void ShowComponentRecipe(IReadOnlyList<MaterialAmount> recipe)
        {
            ClearRecipe(componentRecipeContent);

            foreach (MaterialAmount material in recipe)
            {
                RecipeItemUI item =
                    Instantiate(recipeItemPrefab, componentRecipeContent);

                item.Setup(
                    material.Material.Icon,
                    material.Material.Tier,
                    material.Amount);
            }
        }
        private void ShowComponent(ComponentSO component)
        {
            HideAll();

            commonPanel.SetActive(true);
            componentPanel.SetActive(true);

            productionTimeText.text =
                $"{component.ProductionTime:0}s";

            ShowComponentRecipe(component.Recipe);

            produceComponentButton.onClick.RemoveAllListeners();

            produceComponentButton.onClick.AddListener(() =>
            {
                production.QueueComponent(component);
            });
        }

        private void ShowDrone(DroneModelSO drone)
        {
            HideAll();

            commonPanel.SetActive(true);
            dronePanel.SetActive(true);

            assemblyTimeText.text =
                $"{drone.ProductionTime:0}s";

            flightDistanceText.text =
                $"{drone.FlightDistanceKm} km";

            payloadText.text =
                $"{drone.PayloadCapacityKg} kg";

            durabilityText.text =
                drone.Durability.ToString();

            navigationText.text =
                drone.Navigation.ToString();

            stealthText.text =
                drone.Stealth.ToString();

            ClearRecipe(componentsContent);

            foreach (DroneComponent droneComponent in drone.Components)
            {
                RecipeItemUI item =
                    Instantiate(recipeItemPrefab, componentsContent);
                item.Setup(
    droneComponent.Component.Icon,
    droneComponent.Component.Tier,
    droneComponent.Amount);
            }
            produceDroneButton.onClick.RemoveAllListeners();

            produceDroneButton.onClick.AddListener(() =>
            {
                production.QueueDrone(drone);
            });
        }
        private void ClearRecipe(Transform parent)
        {
            foreach (Transform child in parent)
            {
                Destroy(child.gameObject);
            }
        }
    }
}
