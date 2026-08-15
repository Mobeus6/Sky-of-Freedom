using System;
using SkyOfFreedom.Data;
using SkyOfFreedom.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkyOfFreedom.UI
{
    public class LicenseCardItem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text licenseName;
        [SerializeField] private TMP_Text description;
        [SerializeField] private TMP_Text factoryLevelText;
        [SerializeField] private TMP_Text costText;

        [Header("Interaction")]
        [SerializeField] private Button button;

        [Header("Status")]
        [SerializeField] private GameObject locked;
        [SerializeField] private GameObject bought;

        private CardTierVisual tierVisual;
        private LicenseManager licenseManager;

        private LicenseSO license;

        public LicenseSO License
        {
            get
            {
                return license;
            }
        }

        public event Action<LicenseSO> Selected;
        public event Action<LicenseSO> Purchased;

        private void Awake()
        {
            tierVisual =
                GetComponent<CardTierVisual>();

            if (tierVisual == null)
            {
                Debug.LogWarning(
                    $"LicenseCardItem on '{gameObject.name}' has no CardTierVisual.",
                    this);
            }

            if (button == null)
            {
                Debug.LogError(
                    $"LicenseCardItem on '{gameObject.name}' has no Button assigned.",
                    this);

                return;
            }

            button.onClick.AddListener(
                OnClicked);
        }

        public void Setup(
            LicenseSO data)
        {
            license =
                data;

            if (data == null)
            {
                gameObject.SetActive(false);
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
                data.UnlockedComponent;

            if (component == null)
            {
                Debug.LogWarning(
                    $"License '{data.LicenseName}' has no UnlockedComponent.",
                    data);

                ClearVisuals();
                return;
            }

            SetupIcon(component);
            SetupNames(data);
            SetupDescription(data);
            SetupRequirements(data);
            SetupTier(component);
            SetupStatus(data);
        }

        private void SetupIcon(
            ComponentSO component)
        {
            if (icon == null)
            {
                return;
            }

            icon.sprite =
                component.Icon;

            icon.enabled =
                component.Icon != null;
        }

        private void SetupNames(
            LicenseSO data)
        {
            if (licenseName != null)
            {
                licenseName.text =
                    data.LicenseName;
            }
        }

        private void SetupDescription(
            LicenseSO data)
        {
            if (description != null)
            {
                description.text =
                    data.Description;
            }
        }

        private void SetupRequirements(
            LicenseSO data)
        {
            if (factoryLevelText != null)
            {
                factoryLevelText.text =
                    $"Factory Lv. {data.RequiredFactoryLevel}";
            }

            if (costText != null)
            {
                costText.text =
                    data.PurchaseCost.ToString("N0");
            }
        }

        private void SetupTier(
            ComponentSO component)
        {
            if (tierVisual != null)
            {
                tierVisual.SetTier(
                    component.Tier);
            }
        }

        private void SetupStatus(
            LicenseSO data)
        {
            HideAllStatuses();

            if (licenseManager == null)
            {
                SetAvailableStatus();
                return;
            }

            if (licenseManager.IsUnlocked(data))
            {
                SetBoughtStatus();
                return;
            }

            if (licenseManager.IsLocked(data))
            {
                SetLockedStatus();
                return;
            }

            SetAvailableStatus();
        }

        private void SetAvailableStatus()
        {
            if (button != null)
            {
                button.gameObject.SetActive(true);
                button.interactable = true;
            }

            if (locked != null)
            {
                locked.SetActive(false);
            }

            if (bought != null)
            {
                bought.SetActive(false);
            }
        }

        private void SetLockedStatus()
        {
            if (button != null)
            {
                button.gameObject.SetActive(false);
            }

            if (locked != null)
            {
                locked.SetActive(true);
            }

            if (bought != null)
            {
                bought.SetActive(false);
            }
        }

        private void SetBoughtStatus()
        {
            if (button != null)
            {
                button.gameObject.SetActive(false);
            }

            if (locked != null)
            {
                locked.SetActive(false);
            }

            if (bought != null)
            {
                bought.SetActive(true);
            }
        }

        private void HideAllStatuses()
        {
            if (locked != null)
            {
                locked.SetActive(false);
            }

            if (bought != null)
            {
                bought.SetActive(false);
            }

            if (button != null)
            {
                button.gameObject.SetActive(false);
            }
        }

        private void ClearVisuals()
        {
            if (icon != null)
            {
                icon.sprite = null;
                icon.enabled = false;
            }

            if (licenseName != null)
            {
                licenseName.text =
                    string.Empty;
            }

            if (description != null)
            {
                description.text =
                    string.Empty;
            }

            if (factoryLevelText != null)
            {
                factoryLevelText.text =
                    string.Empty;
            }

            if (costText != null)
            {
                costText.text =
                    string.Empty;
            }

            HideAllStatuses();
        }

        private void OnClicked()
        {
            if (license == null)
            {
                return;
            }

            if (licenseManager == null &&
                GameManager.Instance != null)
            {
                licenseManager =
                    GameManager.Instance.License;
            }

            /*
             * First select the license.
             * This keeps the Info Card synchronized
             * with the clicked card.
             */
            Selected?.Invoke(license);

            /*
             * If the license is already bought,
             * there is nothing else to do.
             */
            if (licenseManager == null)
            {
                return;
            }

            if (licenseManager.IsUnlocked(license))
            {
                return;
            }

            /*
             * Locked licenses cannot be purchased.
             */
            if (!licenseManager.CanPurchase(license))
            {
                return;
            }

            bool purchased =
                licenseManager.Purchase(
                    license);

            if (!purchased)
            {
                return;
            }

            /*
             * Update this card immediately.
             */
            SetupStatus(
                license);

            /*
             * Notify LicensesPanelUI.
             */
            Purchased?.Invoke(
                license);
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(
                    OnClicked);
            }
        }
    }
}