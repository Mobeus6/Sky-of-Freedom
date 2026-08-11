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
        [SerializeField] private Button cardButton;
        [SerializeField] private Button buyButton;

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

            if (cardButton == null)
            {
                Debug.LogError(
                    $"LicenseCardItem on '{gameObject.name}' has no Card Button assigned.",
                    this);
            }
            else
            {
                cardButton.onClick.AddListener(
                    OnCardClicked);
            }

            if (buyButton == null)
            {
                Debug.LogWarning(
                    $"LicenseCardItem on '{gameObject.name}' has no Buy Button assigned.",
                    this);
            }
        }

        public void Setup(LicenseSO data)
        {
            license = data;

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
            if (cardButton != null)
            {
                cardButton.gameObject.SetActive(true);
                cardButton.interactable = true;
            }

            if (buyButton != null)
            {
                buyButton.gameObject.SetActive(true);
                buyButton.interactable = true;
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
            if (cardButton != null)
            {
                cardButton.gameObject.SetActive(true);
                cardButton.interactable = true;
            }

            if (buyButton != null)
            {
                buyButton.gameObject.SetActive(false);
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
            if (cardButton != null)
            {
                cardButton.gameObject.SetActive(true);
                cardButton.interactable = true;
            }

            if (buyButton != null)
            {
                buyButton.gameObject.SetActive(false);
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

            if (buyButton != null)
            {
                buyButton.gameObject.SetActive(false);
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

            HideAllStatuses();
        }

        private void OnCardClicked()
        {
            if (license == null)
            {
                return;
            }

            Selected?.Invoke(license);
        }

        private void OnDestroy()
        {
            if (cardButton != null)
            {
                cardButton.onClick.RemoveListener(
                    OnCardClicked);
            }
        }
    }
}