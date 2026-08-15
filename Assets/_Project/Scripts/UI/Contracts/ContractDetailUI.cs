using SkyOfFreedom.Contracts;
using SkyOfFreedom.Data;
using SkyOfFreedom.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkyOfFreedom.UI.Contracts
{
    public class ContractDetailUI : MonoBehaviour
    {
        [Header("Info Cards")]
        [SerializeField] private GameObject droneContractInfoCard;
        [SerializeField] private GameObject componentContractInfoCard;

        [Header("Accept Contract Buttons")]
        [SerializeField] private Button droneAcceptContractButton;
        [SerializeField] private Button componentAcceptContractButton;

        [Header("Drone Contract - Contract")]
        [SerializeField] private TMP_Text droneContractNameText;
        [SerializeField] private TMP_Text droneDescriptionText;
        [SerializeField] private Image droneContractImage;

        [Header("Drone Contract - Target")]
        [SerializeField] private Image droneTargetIcon;
        [SerializeField] private TMP_Text droneTargetNameText;
        [SerializeField] private TMP_Text droneTierText;
        [SerializeField] private TMP_Text droneQuantityText;
        [SerializeField] private CardTierVisual droneTargetTierVisual;

        [Header("Drone Contract - Specifications")]
        [SerializeField] private TMP_Text flightDistanceText;
        [SerializeField] private TMP_Text payloadText;
        [SerializeField] private TMP_Text navigationText;
        [SerializeField] private TMP_Text durabilityText;
        [SerializeField] private TMP_Text stealthText;

        [Header("Drone Contract - Required Components")]
        [SerializeField] private Transform droneRequiredComponentsContent;

        [Header("Drone Contract - Rewards")]
        [SerializeField] private TMP_Text droneFinanceRewardText;
        [SerializeField] private TMP_Text droneReputationRewardText;

        [Header("Component Contract - Contract")]
        [SerializeField] private TMP_Text componentContractNameText;
        [SerializeField] private TMP_Text componentDescriptionText;
        [SerializeField] private Image componentContractImage;

        [Header("Component Contract - Required Components")]
        [SerializeField] private Transform componentRequiredComponentsContent;

        [Header("Component Contract - Required Materials")]
        [SerializeField] private Transform componentRequiredMaterialsContent;

        [Header("Component Contract - Rewards")]
        [SerializeField] private TMP_Text componentFinanceRewardText;
        [SerializeField] private TMP_Text componentReputationRewardText;

        [Header("Contract Component Prefab")]
        [SerializeField] private ContractComponentUI contractComponentPrefab;

        [Header("Contract Icon Tier Visual")]
        [SerializeField] private CardTierVisual droneContractTierVisual;
        [SerializeField] private CardTierVisual componentContractTierVisual;

        private ContractInstance currentContract;
        private ContractManager contractManager;

        private System.Action<ContractInstance> acceptCallback;

        private void Awake()
        {
            if (droneAcceptContractButton != null)
            {
                droneAcceptContractButton.onClick.AddListener(
                    OnAcceptContractClicked);
            }

            if (componentAcceptContractButton != null)
            {
                componentAcceptContractButton.onClick.AddListener(
                    OnAcceptContractClicked);
            }

            HideCards();
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                contractManager =
                    GameManager.Instance.Contracts;
            }
        }

        private void OnDestroy()
        {
            if (droneAcceptContractButton != null)
            {
                droneAcceptContractButton.onClick.RemoveListener(
                    OnAcceptContractClicked);
            }

            if (componentAcceptContractButton != null)
            {
                componentAcceptContractButton.onClick.RemoveListener(
                    OnAcceptContractClicked);
            }
        }

        public void SetAcceptCallback(
            System.Action<ContractInstance> callback)
        {
            acceptCallback = callback;
        }

        public void Show(
            ContractInstance contract)
        {
            if (contract == null ||
                contract.Template == null)
            {
                Hide();
                return;
            }

            currentContract = contract;

            switch (contract.Template.TargetType)
            {
                case ContractTargetType.Drone:
                    ShowDroneContract(contract);
                    break;

                case ContractTargetType.Component:
                    ShowComponentContract(contract);
                    break;

                default:
                    Hide();
                    break;
            }
        }

        public void Hide()
        {
            currentContract = null;

            HideAcceptButtons();
            HideCards();
        }

        private void OnAcceptContractClicked()
        {
            if (currentContract == null)
            {
                return;
            }

            if (currentContract.State !=
                ContractState.Available)
            {
                return;
            }

            if (contractManager == null &&
                GameManager.Instance != null)
            {
                contractManager =
                    GameManager.Instance.Contracts;
            }

            if (contractManager == null)
            {
                Debug.LogError(
                    "ContractDetailUI: ContractManager was not found.",
                    this);

                return;
            }

            acceptCallback?.Invoke(
                currentContract);
        }

        private void UpdateAcceptButtons(
            ContractTargetType targetType)
        {
            bool isAvailable =
                currentContract != null &&
                currentContract.State ==
                ContractState.Available;

            if (droneAcceptContractButton != null)
            {
                droneAcceptContractButton.gameObject.SetActive(
                    isAvailable &&
                    targetType == ContractTargetType.Drone);

                droneAcceptContractButton.interactable =
                    isAvailable &&
                    targetType == ContractTargetType.Drone;
            }

            if (componentAcceptContractButton != null)
            {
                componentAcceptContractButton.gameObject.SetActive(
                    isAvailable &&
                    targetType == ContractTargetType.Component);

                componentAcceptContractButton.interactable =
                    isAvailable &&
                    targetType == ContractTargetType.Component;
            }
        }

        private void HideAcceptButtons()
        {
            if (droneAcceptContractButton != null)
            {
                droneAcceptContractButton.gameObject.SetActive(false);
            }

            if (componentAcceptContractButton != null)
            {
                componentAcceptContractButton.gameObject.SetActive(false);
            }
        }

        private void ShowDroneContract(
            ContractInstance contract)
        {
            ContractSO template =
                contract.Template;

            DroneModelSO drone =
                template.DroneModel;

            if (drone == null)
            {
                Hide();
                return;
            }

            droneContractInfoCard.SetActive(true);
            componentContractInfoCard.SetActive(false);

            UpdateAcceptButtons(
                ContractTargetType.Drone);

            ClearContent(
                droneRequiredComponentsContent);

            if (droneContractNameText != null)
            {
                droneContractNameText.text =
                    template.ContractName;
            }

            if (droneDescriptionText != null)
            {
                droneDescriptionText.text =
                    template.Description;
            }

            if (droneContractImage != null)
            {
                droneContractImage.sprite =
                    template.Icon;

                droneContractImage.enabled =
                    template.Icon != null;
            }

            if (droneContractTierVisual != null)
            {
                droneContractTierVisual.SetTier(
                    drone.Tier);
            }

            if (droneTargetIcon != null)
            {
                droneTargetIcon.sprite =
                    drone.Icon;

                droneTargetIcon.enabled =
                    drone.Icon != null;
            }

            if (droneTargetNameText != null)
            {
                droneTargetNameText.text =
                    drone.Name;
            }

            if (droneTierText != null)
            {
                droneTierText.text =
                    $"T{drone.Tier}";
            }

            if (droneQuantityText != null)
            {
                droneQuantityText.text =
                    contract.Quantity.ToString();
            }

            if (droneTargetTierVisual != null)
            {
                droneTargetTierVisual.SetTier(
                    drone.Tier);
            }

            UpdateDroneSpecifications(
                drone);

            CreateDroneRequiredComponents(
                drone,
                contract.Quantity);

            UpdateRewards(
                contract,
                droneFinanceRewardText,
                droneReputationRewardText);
        }

        private void ShowComponentContract(
            ContractInstance contract)
        {
            ContractSO template =
                contract.Template;

            ComponentSO component =
                template.Component;

            if (component == null)
            {
                Hide();
                return;
            }

            droneContractInfoCard.SetActive(false);
            componentContractInfoCard.SetActive(true);

            UpdateAcceptButtons(
                ContractTargetType.Component);

            ClearContent(
                componentRequiredComponentsContent);

            ClearContent(
                componentRequiredMaterialsContent);

            if (componentContractNameText != null)
            {
                componentContractNameText.text =
                    template.ContractName;
            }

            if (componentDescriptionText != null)
            {
                componentDescriptionText.text =
                    template.Description;
            }

            if (componentContractImage != null)
            {
                componentContractImage.sprite =
                    template.Icon;

                componentContractImage.enabled =
                    template.Icon != null;
            }

            if (componentContractTierVisual != null)
            {
                componentContractTierVisual.SetTier(
                    component.Tier);
            }

            CreateComponentRequiredComponent(
                component,
                contract.Quantity);

            CreateComponentRequiredMaterials(
                component,
                contract.Quantity);

            UpdateRewards(
                contract,
                componentFinanceRewardText,
                componentReputationRewardText);
        }

        private void UpdateDroneSpecifications(
            DroneModelSO drone)
        {
            if (flightDistanceText != null)
            {
                flightDistanceText.text =
                    $"{drone.FlightDistanceKm} km";
            }

            if (payloadText != null)
            {
                payloadText.text =
                    $"{drone.PayloadCapacityKg} kg";
            }

            if (navigationText != null)
            {
                navigationText.text =
                    drone.Navigation.ToString();
            }

            if (durabilityText != null)
            {
                durabilityText.text =
                    drone.Durability.ToString();
            }

            if (stealthText != null)
            {
                stealthText.text =
                    drone.Stealth.ToString();
            }
        }

        private void CreateDroneRequiredComponents(
            DroneModelSO drone,
            int contractQuantity)
        {
            if (droneRequiredComponentsContent == null)
            {
                Debug.LogError(
                    "ContractDetailUI: Drone Required Components Content is missing.",
                    this);

                return;
            }

            if (contractComponentPrefab == null)
            {
                Debug.LogError(
                    "ContractDetailUI: Contract Component Prefab is missing.",
                    this);

                return;
            }

            ClearContent(
                droneRequiredComponentsContent);

            if (drone.Components == null)
            {
                return;
            }

            foreach (
                DroneComponent droneComponent
                in drone.Components)
            {
                if (droneComponent == null ||
                    droneComponent.Component == null)
                {
                    continue;
                }

                int requiredAmount =
                    droneComponent.Amount *
                    contractQuantity;

                ContractComponentUI item =
                    Instantiate(
                        contractComponentPrefab,
                        droneRequiredComponentsContent);

                item.Setup(
                    droneComponent.Component,
                    requiredAmount);
            }
        }

        private void CreateComponentRequiredComponent(
            ComponentSO component,
            int contractQuantity)
        {
            if (componentRequiredComponentsContent == null)
            {
                Debug.LogError(
                    "ContractDetailUI: Component Required Components Content is missing.",
                    this);

                return;
            }

            if (contractComponentPrefab == null)
            {
                Debug.LogError(
                    "ContractDetailUI: Contract Component Prefab is missing.",
                    this);

                return;
            }

            ClearContent(
                componentRequiredComponentsContent);

            ContractComponentUI item =
                Instantiate(
                    contractComponentPrefab,
                    componentRequiredComponentsContent);

            item.Setup(
                component,
                contractQuantity);
        }

        private void CreateComponentRequiredMaterials(
            ComponentSO component,
            int contractQuantity)
        {
            if (componentRequiredMaterialsContent == null)
            {
                Debug.LogError(
                    "ContractDetailUI: Component Required Materials Content is missing.",
                    this);

                return;
            }

            if (contractComponentPrefab == null)
            {
                Debug.LogError(
                    "ContractDetailUI: Contract Component Prefab is missing.",
                    this);

                return;
            }

            ClearContent(
                componentRequiredMaterialsContent);

            if (component.Recipe == null)
            {
                return;
            }

            foreach (
                MaterialAmount materialAmount
                in component.Recipe)
            {
                if (materialAmount == null ||
                    materialAmount.Material == null)
                {
                    continue;
                }

                int requiredAmount =
                    materialAmount.Amount *
                    contractQuantity;

                ContractComponentUI item =
                    Instantiate(
                        contractComponentPrefab,
                        componentRequiredMaterialsContent);

                item.Setup(
                    materialAmount.Material,
                    requiredAmount);
            }
        }

        private void UpdateRewards(
            ContractInstance contract,
            TMP_Text financeText,
            TMP_Text reputationText)
        {
            if (financeText != null)
            {
                financeText.text =
                    contract.Reward.ToString("N0");
            }

            if (reputationText != null)
            {
                reputationText.text =
                    contract.Template.ReputationReward.ToString("N0");
            }
        }

        private void ClearContent(
            Transform content)
        {
            if (content == null)
            {
                return;
            }

            for (int i = content.childCount - 1;
                 i >= 0;
                 i--)
            {
                Destroy(
                    content.GetChild(i).gameObject);
            }
        }

        private void HideCards()
        {
            if (droneContractInfoCard != null)
            {
                droneContractInfoCard.SetActive(false);
            }

            if (componentContractInfoCard != null)
            {
                componentContractInfoCard.SetActive(false);
            }
        }
    }
}