using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SkyOfFreedom.Factory;
using SkyOfFreedom.Managers;

namespace SkyOfFreedom.UI.Factory
{
    public class FactoryZoneUpgradeUI : MonoBehaviour
    {
        [Header("Zone")]
        [SerializeField]
        private FactoryZoneType zoneType;

        [Header("References")]
        [SerializeField]
        private TMP_Text currentLevelText;

        [SerializeField]
        private TMP_Text nextLevelText;

        [SerializeField]
        private TMP_Text upgradeCostText;

        [SerializeField]
        private TMP_Text factoryLevelRequirementText;

        [SerializeField]
        private Button upgradeButton;

        private FactoryManager factoryManager;

        private void OnEnable()
        {
            TryInitialize();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void TryInitialize()
        {
            if (GameManager.Instance == null)
                return;

            factoryManager =
                GameManager.Instance.Factory;

            if (factoryManager == null)
                return;

            Subscribe();
            Refresh();
        }

        private void Subscribe()
        {
            factoryManager.OnFactoryLevelChanged -=
                OnFactoryLevelChanged;

            factoryManager.OnFactoryLevelChanged +=
                OnFactoryLevelChanged;

            factoryManager.OnZoneLevelChanged -=
                OnZoneLevelChanged;

            factoryManager.OnZoneLevelChanged +=
                OnZoneLevelChanged;

            if (upgradeButton != null)
            {
                upgradeButton.onClick.RemoveListener(
                    OnUpgradeButtonClicked);

                upgradeButton.onClick.AddListener(
                    OnUpgradeButtonClicked);
            }
        }

        private void Unsubscribe()
        {
            if (factoryManager != null)
            {
                factoryManager.OnFactoryLevelChanged -=
                    OnFactoryLevelChanged;

                factoryManager.OnZoneLevelChanged -=
                    OnZoneLevelChanged;
            }

            if (upgradeButton != null)
            {
                upgradeButton.onClick.RemoveListener(
                    OnUpgradeButtonClicked);
            }
        }

        public void Refresh()
        {
            if (factoryManager == null)
                return;

            int currentLevel =
                factoryManager.GetLevel(zoneType);

            int nextLevel =
                factoryManager.GetNextZoneLevel(zoneType);

            bool isMaxLevel =
                factoryManager.IsMaxZoneLevel(zoneType);

            if (currentLevelText != null)
            {
                currentLevelText.text =
                    currentLevel.ToString();
            }

            if (nextLevelText != null)
            {
                nextLevelText.text =
                    isMaxLevel
                        ? "MAX"
                        : nextLevel.ToString();
            }

            if (isMaxLevel)
            {
                SetMaxLevelState();
                return;
            }

            if (!factoryManager.TryGetZoneUpgradeRequirement(
                    zoneType,
                    out FactoryProgressionConfig.ZoneLevelRequirement requirement))
            {
                SetUnavailableState();
                return;
            }

            if (upgradeCostText != null)
            {
                upgradeCostText.text =
                    requirement.MoneyRequired.ToString("N0");
            }

            if (factoryLevelRequirementText != null)
            {
                factoryLevelRequirementText.text =
                    $"Factory Lv. {nextLevel}";
            }

            if (upgradeButton != null)
            {
                upgradeButton.interactable =
                    factoryManager.CanUpgradeZone(
                        zoneType);
            }
        }

        private void SetMaxLevelState()
        {
            if (upgradeCostText != null)
            {
                upgradeCostText.text =
                    "MAX";
            }

            if (factoryLevelRequirementText != null)
            {
                factoryLevelRequirementText.text =
                    "MAX";
            }

            if (upgradeButton != null)
            {
                upgradeButton.interactable = false;
            }
        }

        private void SetUnavailableState()
        {
            if (upgradeCostText != null)
            {
                upgradeCostText.text =
                    "—";
            }

            if (factoryLevelRequirementText != null)
            {
                factoryLevelRequirementText.text =
                    "—";
            }

            if (upgradeButton != null)
            {
                upgradeButton.interactable = false;
            }
        }

        private void OnFactoryLevelChanged(
            int newFactoryLevel)
        {
            Refresh();
        }

        private void OnZoneLevelChanged(
            FactoryZoneType changedZone,
            int newLevel)
        {
            if (changedZone != zoneType)
                return;

            Refresh();
        }

        private void OnUpgradeButtonClicked()
        {
            if (factoryManager == null)
                return;

            if (!factoryManager.TryUpgradeZone(
                    zoneType))
            {
                Refresh();
                return;
            }

            Refresh();
        }
    }
}