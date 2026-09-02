using SkyOfFreedom.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkyOfFreedom.UI
{
    public class LicenseOverviewUI : MonoBehaviour
    {
        [Header("License Overview")]
        [SerializeField]
        private TMP_Text totalLicenseText;

        [Header("Open Licenses")]
        [SerializeField]
        private Button openLicensesButton;

        [SerializeField]
        private MenuManager menuManager;

        [SerializeField]
        private MenuButton licenseMenuButton;

        private LicenseManager licenseManager;

        private void Awake()
        {
            if (GameManager.Instance != null)
            {
                licenseManager =
                    GameManager.Instance.License;
            }

            if (openLicensesButton != null)
            {
                openLicensesButton.onClick.RemoveAllListeners();

                openLicensesButton.onClick.AddListener(
                    OpenLicensesPanel);
            }
        }

        private void OnEnable()
        {
            if (licenseManager == null &&
                GameManager.Instance != null)
            {
                licenseManager =
                    GameManager.Instance.License;
            }

            Refresh();
        }

        public void Refresh()
        {
            if (licenseManager == null)
            {
                Clear();
                return;
            }

            if (GameManager.Instance == null ||
                GameManager.Instance.Database == null ||
                GameManager.Instance.Database.Database == null)
            {
                Clear();
                return;
            }

            int unlockedLicenses =
                licenseManager
                    .GetUnlockedLicenses()
                    .Count;

            int totalLicenses =
                GameManager.Instance
                    .Database
                    .Database
                    .Licenses
                    .Count;

            if (totalLicenseText != null)
            {
                totalLicenseText.text =
                    $"{unlockedLicenses}/{totalLicenses}";
            }
        }

        private void Clear()
        {
            if (totalLicenseText != null)
            {
                totalLicenseText.text =
                    "0/0";
            }
        }

        private void OpenLicensesPanel()
        {
            if (menuManager == null)
            {
                Debug.LogError(
                    "LicenseOverviewUI: MenuManager is not assigned.",
                    this);

                return;
            }

            if (licenseMenuButton == null)
            {
                Debug.LogError(
                    "LicenseOverviewUI: License MenuButton is not assigned.",
                    this);

                return;
            }

            menuManager.Toggle(
                licenseMenuButton);
        }
    }
}