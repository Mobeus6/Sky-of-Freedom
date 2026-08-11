using SkyOfFreedom.Data;
using SkyOfFreedom.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkyOfFreedom.UI
{
    public class LicenseInfoCardUI : MonoBehaviour
    {
        [Header("Top")]
        [SerializeField] private Image licenseIcon;
        [SerializeField] private CardTierVisual licenseTierVisual;
        [SerializeField] private TMP_Text licenseName;
        [SerializeField] private TMP_Text description;

        [Header("Status")]
        [SerializeField] private GameObject statusPanel;
        [SerializeField] private GameObject availableStatus;
        [SerializeField] private GameObject boughtStatus;
        [SerializeField] private GameObject lockedStatus;

        [Header("Requirements")]
        [SerializeField] private TMP_Text factoryLevelText;
        [SerializeField] private TMP_Text costText;

        [Header("Unlock")]
        [SerializeField] private Image unlockIcon;
        [SerializeField] private CardTierVisual unlockTierVisual;
        [SerializeField] private TMP_Text unlockName;

        private LicenseManager licenseManager;
        private LicenseSO currentLicense;

        public LicenseSO CurrentLicense
        {
            get
            {
                return currentLicense;
            }
        }

        private void Awake()
        {
            if (GameManager.Instance != null)
            {
                licenseManager =
                    GameManager.Instance.License;
            }

            HideAllStatuses();
        }

        public void Show(LicenseSO license)
        {
            currentLicense = license;

            if (license == null)
            {
                Clear();
                return;
            }

            gameObject.SetActive(true);

            if (licenseManager == null &&
                GameManager.Instance != null)
            {
                licenseManager =
                    GameManager.Instance.License;
            }

            ComponentSO component =
                license.UnlockedComponent;

            SetupLicense(
                license,
                component);

            SetupRequirements(
                license);

            SetupUnlock(
                component);

            SetupStatus(
                license);
        }

        private void SetupLicense(
            LicenseSO license,
            ComponentSO component)
        {
            if (licenseName != null)
            {
                licenseName.text =
                    license.LicenseName;
            }

            if (description != null)
            {
                description.text =
                    license.Description;
            }

            if (licenseIcon != null)
            {
                if (component != null &&
                    component.Icon != null)
                {
                    licenseIcon.sprite =
                        component.Icon;

                    licenseIcon.enabled = true;
                }
                else
                {
                    licenseIcon.sprite = null;
                    licenseIcon.enabled = false;
                }
            }

            if (licenseTierVisual != null &&
                component != null)
            {
                licenseTierVisual.SetTier(
                    component.Tier);
            }
        }

        private void SetupRequirements(
            LicenseSO license)
        {
            if (factoryLevelText != null)
            {
                factoryLevelText.text =
                    $"Factory Level {license.RequiredFactoryLevel}";
            }

            if (costText != null)
            {
                costText.text =
                    license.PurchaseCost.ToString("N0");
            }
        }

        private void SetupUnlock(
            ComponentSO component)
        {
            if (component == null)
            {
                ClearUnlock();
                return;
            }

            if (unlockIcon != null)
            {
                if (component.Icon != null)
                {
                    unlockIcon.sprite =
                        component.Icon;

                    unlockIcon.enabled = true;
                }
                else
                {
                    unlockIcon.sprite = null;
                    unlockIcon.enabled = false;
                }
            }

            if (unlockTierVisual != null)
            {
                unlockTierVisual.SetTier(
                    component.Tier);
            }

            if (unlockName != null)
            {
                unlockName.text =
                    component.Name;
            }
        }

        private void SetupStatus(
            LicenseSO license)
        {
            HideAllStatuses();

            if (licenseManager == null)
            {
                SetAvailableStatus();
                return;
            }

            if (licenseManager.IsUnlocked(
                    license))
            {
                SetBoughtStatus();
                return;
            }

            if (licenseManager.IsAvailable(
                    license))
            {
                SetAvailableStatus();
                return;
            }

            if (licenseManager.IsLocked(
                    license))
            {
                SetLockedStatus();
                return;
            }

            SetLockedStatus();
        }

        private void SetAvailableStatus()
        {
            if (statusPanel != null)
            {
                statusPanel.SetActive(true);
            }

            if (availableStatus != null)
            {
                availableStatus.SetActive(true);
            }
        }

        private void SetBoughtStatus()
        {
            if (statusPanel != null)
            {
                statusPanel.SetActive(true);
            }

            if (boughtStatus != null)
            {
                boughtStatus.SetActive(true);
            }
        }

        private void SetLockedStatus()
        {
            if (statusPanel != null)
            {
                statusPanel.SetActive(true);
            }

            if (lockedStatus != null)
            {
                lockedStatus.SetActive(true);
            }
        }

        private void HideAllStatuses()
        {
            if (availableStatus != null)
            {
                availableStatus.SetActive(false);
            }

            if (boughtStatus != null)
            {
                boughtStatus.SetActive(false);
            }

            if (lockedStatus != null)
            {
                lockedStatus.SetActive(false);
            }
        }

        private void Clear()
        {
            currentLicense = null;

            if (licenseIcon != null)
            {
                licenseIcon.sprite = null;
                licenseIcon.enabled = false;
            }

            if (unlockIcon != null)
            {
                unlockIcon.sprite = null;
                unlockIcon.enabled = false;
            }

            if (licenseName != null)
            {
                licenseName.text = string.Empty;
            }

            if (description != null)
            {
                description.text = string.Empty;
            }

            if (factoryLevelText != null)
            {
                factoryLevelText.text = string.Empty;
            }

            if (costText != null)
            {
                costText.text = string.Empty;
            }

            if (unlockName != null)
            {
                unlockName.text = string.Empty;
            }

            HideAllStatuses();

            if (statusPanel != null)
            {
                statusPanel.SetActive(false);
            }
        }

        private void ClearUnlock()
        {
            if (unlockIcon != null)
            {
                unlockIcon.sprite = null;
                unlockIcon.enabled = false;
            }

            if (unlockName != null)
            {
                unlockName.text = string.Empty;
            }
        }
    }
}