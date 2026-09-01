using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.VectorGraphics;
using SkyOfFreedom.Factory;
using SkyOfFreedom.Managers;

namespace SkyOfFreedom.UI.Factory
{
    public class FactoryDevelopmentUI : MonoBehaviour
    {
        [Serializable]
        private class ZoneRequirementUI
        {
            [SerializeField]
            private TMP_Text text;

            [SerializeField]
            private SVGImage icon;

            [SerializeField]
            private Sprite fulfilledIcon;

            [SerializeField]
            private Sprite failedIcon;

            public void SetState(
                string requirementText,
                bool fulfilled,
                Color fulfilledColor,
                Color failedColor)
            {
                Color stateColor =
                    fulfilled
                        ? fulfilledColor
                        : failedColor;

                if (text != null)
                {
                    text.text = requirementText;
                    text.color = stateColor;
                }

                if (icon != null)
                {
                    icon.sprite =
                        fulfilled
                            ? fulfilledIcon
                            : failedIcon;

                    icon.color = stateColor;
                }
            }
        }

        [Header("Factory Level")]
        [SerializeField]
        private TMP_Text currentFactoryLevelText;

        [Header("Zone Requirements")]
        [SerializeField]
        private ZoneRequirementUI assemblyRequirement;

        [SerializeField]
        private ZoneRequirementUI productionRequirement;

        [SerializeField]
        private ZoneRequirementUI warehouseRequirement;

        [SerializeField]
        private ZoneRequirementUI researchRequirement;

        [Header("Economy Requirements")]
        [SerializeField]
        private TMP_Text reputationRequirementText;

        [Header("Reward")]
        [SerializeField]
        private TMP_Text rewardText;

        [Header("Upgrade Cost")]
        [SerializeField]
        private TMP_Text moneyCostText;

        [Header("Upgrade Button")]
        [SerializeField]
        private Button upgradeButton;

        [Header("Statistics")]
        [SerializeField]
        private TMP_Text droneProducedText;

        [SerializeField]
        private TMP_Text componentsProducedText;

        [SerializeField]
        private TMP_Text contractsCompletedText;

        [SerializeField]
        private TMP_Text moneyEarnedText;

        [SerializeField]
        private TMP_Text reputationEarnedText;

        [Header("Colors")]
        [SerializeField]
        private Color requirementFulfilledColor =
            new Color(
                0.25f,
                0.75f,
                0.35f,
                1f);

        [SerializeField]
        private Color requirementFailedColor =
            new Color(
                0.85f,
                0.25f,
                0.25f,
                1f);

        private FactoryManager factoryManager;
        private EconomyManager economyManager;
        private FactoryStatisticsManager statisticsManager;

        private void OnEnable()
        {
            InitializeManagers();
            Subscribe();
            Refresh();
            RefreshStatistics();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void InitializeManagers()
        {
            if (GameManager.Instance == null)
                return;

            factoryManager =
                GameManager.Instance.Factory;

            economyManager =
                GameManager.Instance.Economy;

            statisticsManager =
                GameManager.Instance.Statistics;
        }

        private void Subscribe()
        {
            if (factoryManager != null)
            {
                factoryManager.OnFactoryLevelChanged -=
                    HandleFactoryLevelChanged;

                factoryManager.OnFactoryLevelChanged +=
                    HandleFactoryLevelChanged;

                factoryManager.OnZoneLevelChanged -=
                    HandleZoneLevelChanged;

                factoryManager.OnZoneLevelChanged +=
                    HandleZoneLevelChanged;
            }

            if (economyManager != null)
            {
                economyManager.OnMoneyChanged -=
                    HandleMoneyChanged;

                economyManager.OnMoneyChanged +=
                    HandleMoneyChanged;

                economyManager.OnReputationChanged -=
                    HandleReputationChanged;

                economyManager.OnReputationChanged +=
                    HandleReputationChanged;
            }

            if (statisticsManager != null)
            {
                statisticsManager.OnStatisticsChanged -=
                    HandleStatisticsChanged;

                statisticsManager.OnStatisticsChanged +=
                    HandleStatisticsChanged;
            }

            if (upgradeButton != null)
            {
                upgradeButton.onClick.RemoveListener(
                    HandleUpgradeClicked);

                upgradeButton.onClick.AddListener(
                    HandleUpgradeClicked);
            }
        }

        private void Unsubscribe()
        {
            if (factoryManager != null)
            {
                factoryManager.OnFactoryLevelChanged -=
                    HandleFactoryLevelChanged;

                factoryManager.OnZoneLevelChanged -=
                    HandleZoneLevelChanged;
            }

            if (economyManager != null)
            {
                economyManager.OnMoneyChanged -=
                    HandleMoneyChanged;

                economyManager.OnReputationChanged -=
                    HandleReputationChanged;
            }

            if (statisticsManager != null)
            {
                statisticsManager.OnStatisticsChanged -=
                    HandleStatisticsChanged;
            }

            if (upgradeButton != null)
            {
                upgradeButton.onClick.RemoveListener(
                    HandleUpgradeClicked);
            }
        }

        private void HandleFactoryLevelChanged(
            int newLevel)
        {
            Refresh();
        }

        private void HandleZoneLevelChanged(
            FactoryZoneType zone,
            int newLevel)
        {
            Refresh();
        }

        private void HandleMoneyChanged(
            long newMoney)
        {
            Refresh();
        }

        private void HandleReputationChanged(
            int newReputation)
        {
            Refresh();
        }

        private void HandleStatisticsChanged()
        {
            RefreshStatistics();
        }

        private void HandleUpgradeClicked()
        {
            if (factoryManager == null)
                return;

            factoryManager.TryUpgradeFactory();

            Refresh();
        }

        public void Refresh()
        {
            if (factoryManager == null ||
                economyManager == null)
            {
                InitializeManagers();
            }

            if (factoryManager == null ||
                economyManager == null)
            {
                return;
            }

            UpdateFactoryLevel();

            if (factoryManager.IsMaxFactoryLevel())
            {
                UpdateMaxLevel();
                return;
            }

            if (!factoryManager.TryGetFactoryUpgradeRequirement(
                    out FactoryProgressionConfig.FactoryLevelRequirement requirement))
            {
                ClearUnavailableState();
                return;
            }

            int targetLevel =
                requirement.TargetLevel;

            UpdateZoneRequirements(targetLevel);

            UpdateEconomyRequirements(requirement);

            UpdateReward(requirement);

            UpdateUpgradeCost(requirement);

            UpdateUpgradeButton();
        }

        private void RefreshStatistics()
        {
            if (statisticsManager == null)
            {
                InitializeManagers();
            }

            if (statisticsManager == null)
                return;

            if (droneProducedText != null)
            {
                droneProducedText.text =
                    statisticsManager.TotalDroneProduced.ToString("N0");
            }

            if (componentsProducedText != null)
            {
                componentsProducedText.text =
                    statisticsManager.ComponentsProduced.ToString("N0");
            }

            if (contractsCompletedText != null)
            {
                contractsCompletedText.text =
                    statisticsManager.ContractsCompleted.ToString("N0");
            }

            if (moneyEarnedText != null)
            {
                moneyEarnedText.text =
                    statisticsManager.MoneyEarned.ToString("N0");
            }

            if (reputationEarnedText != null)
            {
                reputationEarnedText.text =
                    statisticsManager.ReputationEarned.ToString("N0");
            }
        }

        private void UpdateFactoryLevel()
        {
            if (currentFactoryLevelText == null)
                return;

            currentFactoryLevelText.text =
                factoryManager.Level.ToString();
        }

        private void UpdateZoneRequirements(
            int targetLevel)
        {
            UpdateZoneRequirement(
                assemblyRequirement,
                FactoryZoneType.Assembly,
                targetLevel);

            UpdateZoneRequirement(
                productionRequirement,
                FactoryZoneType.Production,
                targetLevel);

            UpdateZoneRequirement(
                warehouseRequirement,
                FactoryZoneType.Warehouse,
                targetLevel);

            UpdateZoneRequirement(
                researchRequirement,
                FactoryZoneType.Research,
                targetLevel);
        }

        private void UpdateZoneRequirement(
            ZoneRequirementUI ui,
            FactoryZoneType zone,
            int targetLevel)
        {
            if (ui == null)
                return;

            int currentLevel =
                factoryManager.GetLevel(zone);

            bool fulfilled =
                currentLevel >= targetLevel;

            ui.SetState(
                $"{GetZoneName(zone)} Lvl. {targetLevel}",
                fulfilled,
                requirementFulfilledColor,
                requirementFailedColor);
        }

        private void UpdateEconomyRequirements(
            FactoryProgressionConfig.FactoryLevelRequirement requirement)
        {
            bool hasMoney =
                economyManager.HasMoney(
                    requirement.MoneyRequired);

            bool hasReputation =
                economyManager.Reputation >=
                requirement.ReputationRequired;

            if (moneyCostText != null)
            {
                moneyCostText.text =
                    requirement.MoneyRequired.ToString("N0");

                moneyCostText.color =
                    hasMoney
                        ? requirementFulfilledColor
                        : requirementFailedColor;
            }

            if (reputationRequirementText != null)
            {
                reputationRequirementText.text =
                    requirement.ReputationRequired.ToString("N0");

                reputationRequirementText.color =
                    hasReputation
                        ? requirementFulfilledColor
                        : requirementFailedColor;
            }
        }

        private void UpdateReward(
            FactoryProgressionConfig.FactoryLevelRequirement requirement)
        {
            if (rewardText == null)
                return;

            rewardText.text =
                requirement.RewardDescription;
        }

        private void UpdateUpgradeCost(
            FactoryProgressionConfig.FactoryLevelRequirement requirement)
        {
            if (moneyCostText == null)
                return;

            moneyCostText.text =
                requirement.MoneyRequired.ToString("N0");
        }

        private void UpdateUpgradeButton()
        {
            if (upgradeButton == null)
                return;

            upgradeButton.interactable =
                factoryManager.CanUpgradeFactory();
        }

        private void UpdateMaxLevel()
        {
            if (currentFactoryLevelText != null)
            {
                currentFactoryLevelText.text =
                    factoryManager.Level.ToString();
            }

            if (rewardText != null)
            {
                rewardText.text =
                    "Maximum Factory Level";
            }

            if (moneyCostText != null)
            {
                moneyCostText.text =
                    "—";

                moneyCostText.color =
                    requirementFailedColor;
            }

            SetMaxZoneRequirement(
                assemblyRequirement,
                "Assembly Zone");

            SetMaxZoneRequirement(
                productionRequirement,
                "Production Zone");

            SetMaxZoneRequirement(
                warehouseRequirement,
                "Warehouse Zone");

            SetMaxZoneRequirement(
                researchRequirement,
                "Research Zone");

            if (reputationRequirementText != null)
            {
                reputationRequirementText.text =
                    "—";

                reputationRequirementText.color =
                    requirementFailedColor;
            }

            if (upgradeButton != null)
            {
                upgradeButton.interactable =
                    false;
            }
        }

        private void SetMaxZoneRequirement(
            ZoneRequirementUI ui,
            string zoneName)
        {
            if (ui == null)
                return;

            ui.SetState(
                zoneName,
                true,
                requirementFulfilledColor,
                requirementFailedColor);
        }

        private void ClearUnavailableState()
        {
            if (rewardText != null)
                rewardText.text = string.Empty;

            if (moneyCostText != null)
            {
                moneyCostText.text = "—";
                moneyCostText.color =
                    requirementFailedColor;
            }

            if (reputationRequirementText != null)
            {
                reputationRequirementText.text = "—";
                reputationRequirementText.color =
                    requirementFailedColor;
            }

            if (upgradeButton != null)
                upgradeButton.interactable = false;
        }

        private string GetZoneName(
            FactoryZoneType zone)
        {
            switch (zone)
            {
                case FactoryZoneType.Assembly:
                    return "Assembly Zone";

                case FactoryZoneType.Production:
                    return "Production Zone";

                case FactoryZoneType.Warehouse:
                    return "Warehouse Zone";

                case FactoryZoneType.Research:
                    return "Research Zone";

                default:
                    return zone.ToString();
            }
        }
    }
}