using System;
using System.Collections.Generic;
using System.Linq;
using SkyOfFreedom.Data;
using SkyOfFreedom.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkyOfFreedom.UI
{
    public class LicensesPanelUI : MonoBehaviour
    {
        [Header("License List")]
        [SerializeField] private RectTransform content;
        [SerializeField] private LicenseCardItem licenseCardPrefab;

        [Header("License Info")]
        [SerializeField] private LicenseInfoCardUI licenseInfoCard;

        [Header("Category Buttons")]
        [SerializeField] private Transform categoryButtonsRoot;
        [Header("License Total")]
        [SerializeField] private TMP_Text totalLicenseText;
        private GameDatabase database;

        private readonly List<LicenseCardItem> cards =
            new List<LicenseCardItem>();

        private readonly Dictionary<Button, ComponentCategory> buttonCategories =
            new Dictionary<Button, ComponentCategory>();

        private ComponentCategory currentCategory =
            ComponentCategory.Hulls;

        private void Awake()
        {
            InitializeDatabase();
        }

        private void Start()
        {
            InitializeCategoryButtons();
            SelectFirstAvailableCategory();
            UpdateTotalLicenseText();
        }

        private void InitializeDatabase()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError(
                    "LicensesPanelUI: GameManager.Instance is missing.",
                    this);

                return;
            }

            if (GameManager.Instance.Database == null)
            {
                Debug.LogError(
                    "LicensesPanelUI: DatabaseManager is missing.",
                    this);

                return;
            }

            database =
                GameManager.Instance.Database.Database;

            if (database == null)
            {
                Debug.LogError(
                    "LicensesPanelUI: GameDatabase is missing.",
                    this);
            }
        }

        private void InitializeCategoryButtons()
        {
            if (categoryButtonsRoot == null)
            {
                Debug.LogError(
                    "LicensesPanelUI: Category Buttons Root is not assigned.",
                    this);

                return;
            }

            Button[] buttons =
                categoryButtonsRoot.GetComponentsInChildren<Button>(
                    true);

            foreach (Button button in buttons)
            {
                if (button == null)
                {
                    continue;
                }

                if (!TryGetCategoryFromButton(
                        button,
                        out ComponentCategory category))
                {
                    Debug.LogWarning(
                        $"LicensesPanelUI: Could not determine category for button '{button.name}'.",
                        button);

                    continue;
                }

                buttonCategories[button] = category;

                ComponentCategory capturedCategory =
                    category;

                button.onClick.AddListener(
                    () => SelectCategory(capturedCategory));

                SetCategoryButtonText(
                    button,
                    category);
            }
        }
        private void UpdateTotalLicenseText()
        {
            if (totalLicenseText == null)
            {
                return;
            }

            if (database == null ||
                database.Licenses == null)
            {
                totalLicenseText.text = "TOTAL LICENSE BOUGHT 0/0";
                return;
            }

            LicenseManager licenseManager = null;

            if (GameManager.Instance != null)
            {
                licenseManager =
                    GameManager.Instance.License;
            }

            int totalLicenses = 0;
            int purchasedLicenses = 0;

            foreach (LicenseSO license in database.Licenses)
            {
                if (license == null)
                {
                    continue;
                }

                totalLicenses++;

                if (licenseManager != null &&
                    licenseManager.IsUnlocked(license))
                {
                    purchasedLicenses++;
                }
            }

            totalLicenseText.text =
                $"TOTAL LICENSE BOUGHT {purchasedLicenses}/{totalLicenses}";
        }
        private bool TryGetCategoryFromButton(
            Button button,
            out ComponentCategory category)
        {
            category = ComponentCategory.All;

            TMP_Text text =
                button.GetComponentInChildren<TMP_Text>(
                    true);

            string source = string.Empty;

            if (text != null)
            {
                source = text.text;
            }

            if (string.IsNullOrWhiteSpace(source))
            {
                source = button.gameObject.name;
            }

            source = NormalizeCategoryName(source);

            foreach (ComponentCategory value
                     in Enum.GetValues(typeof(ComponentCategory)))
            {
                if (value == ComponentCategory.All)
                {
                    continue;
                }

                string enumName =
                    NormalizeCategoryName(value.ToString());

                if (string.Equals(
                        source,
                        enumName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    category = value;
                    return true;
                }
            }

            return false;
        }

        private string NormalizeCategoryName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            value = value.Trim();

            int buttonIndex =
                value.IndexOf(
                    "button",
                    StringComparison.OrdinalIgnoreCase);

            if (buttonIndex >= 0)
            {
                value =
                    value.Substring(
                        0,
                        buttonIndex);
            }

            value =
                value
                    .Replace(" ", string.Empty)
                    .Replace("_", string.Empty)
                    .Replace("-", string.Empty)
                    .Trim();

            value =
                value.ToLowerInvariant();

            if (value.EndsWith("ies"))
            {
                value =
                    value.Substring(
                        0,
                        value.Length - 3) +
                    "y";
            }
            else if (value.EndsWith("s"))
            {
                value =
                    value.Substring(
                        0,
                        value.Length - 1);
            }

            return value;
        }

        private void SetCategoryButtonText(
            Button button,
            ComponentCategory category)
        {
            TMP_Text text =
                button.GetComponentInChildren<TMP_Text>(
                    true);

            if (text == null)
            {
                return;
            }

            text.text =
                GetCategoryDisplayName(category);
        }

        private string GetCategoryDisplayName(
            ComponentCategory category)
        {
            switch (category)
            {
                case ComponentCategory.Hulls:
                    return "HULLS";

                case ComponentCategory.Batteries:
                    return "BATTERIES";

                case ComponentCategory.Controllers:
                    return "CONTROLLERS";

                case ComponentCategory.GPS:
                    return "GPS";

                case ComponentCategory.Cameras:
                    return "CAMERAS";

                case ComponentCategory.Antennas:
                    return "ANTENNAS";

                case ComponentCategory.Sensors:
                    return "SENSORS";

                case ComponentCategory.Propellers:
                    return "PROPELLERS";

                case ComponentCategory.Motors:
                    return "MOTORS";

                default:
                    return category.ToString().ToUpperInvariant();
            }
        }

        private void SelectFirstAvailableCategory()
        {
            foreach (KeyValuePair<Button, ComponentCategory> pair
                     in buttonCategories)
            {
                if (HasLicenses(pair.Value))
                {
                    SelectCategory(pair.Value);
                    return;
                }
            }

            foreach (KeyValuePair<Button, ComponentCategory> pair
                     in buttonCategories)
            {
                SelectCategory(pair.Value);
                return;
            }
        }

        public void SelectCategory(
            ComponentCategory category)
        {
            currentCategory = category;

            RefreshLicenseList();
        }

        private void RefreshLicenseList()
        {
            ClearContent();
            UpdateTotalLicenseText();

            if (database == null)
            {
                return;
            }

            if (content == null)
            {
                Debug.LogError(
                    "LicensesPanelUI: Content is not assigned.",
                    this);

                return;
            }

            if (licenseCardPrefab == null)
            {
                Debug.LogError(
                    "LicensesPanelUI: License Card Prefab is not assigned.",
                    this);

                return;
            }

            List<LicenseSO> filteredLicenses =
                new List<LicenseSO>();

            foreach (LicenseSO license in database.Licenses)
            {
                if (license == null)
                {
                    continue;
                }

                if (license.UnlockedComponent == null)
                {
                    continue;
                }

                if (license.UnlockedComponent.Category !=
                    currentCategory)
                {
                    continue;
                }

                filteredLicenses.Add(license);
            }

            filteredLicenses =
                filteredLicenses
                    .OrderBy(
                        license =>
                            license.UnlockedComponent.Tier)
                    .ThenBy(
                        license =>
                            license.RequiredFactoryLevel)
                    .ToList();

            foreach (LicenseSO license in filteredLicenses)
            {
                CreateCard(license);
            }

            Canvas.ForceUpdateCanvases();

            SelectFirstLicense();
        }

        private void CreateCard(LicenseSO license)
        {
            LicenseCardItem card =
                Instantiate(
                    licenseCardPrefab,
                    content);

            if (card == null)
            {
                return;
            }

            card.Setup(license);

            card.Selected += OnLicenseSelected;

            cards.Add(card);
        }

        private void OnLicenseSelected(
            LicenseSO license)
        {
            if (license == null)
            {
                return;
            }

            if (licenseInfoCard == null)
            {
                Debug.LogError(
                    "LicensesPanelUI: License Info Card is not assigned.",
                    this);

                return;
            }

            licenseInfoCard.Show(license);
        }

        private void SelectFirstLicense()
        {
            if (cards.Count == 0)
            {
                if (licenseInfoCard != null)
                {
                    licenseInfoCard.Show(null);
                }

                return;
            }

            LicenseCardItem firstCard = cards[0];

            if (firstCard == null ||
                firstCard.License == null)
            {
                return;
            }

            OnLicenseSelected(firstCard.License);
        }

        private bool HasLicenses(
            ComponentCategory category)
        {
            if (database == null ||
                database.Licenses == null)
            {
                return false;
            }

            foreach (LicenseSO license in database.Licenses)
            {
                if (license == null ||
                    license.UnlockedComponent == null)
                {
                    continue;
                }

                if (license.UnlockedComponent.Category ==
                    category)
                {
                    return true;
                }
            }

            return false;
        }

        private void ClearContent()
        {
            for (int i = cards.Count - 1; i >= 0; i--)
            {
                if (cards[i] != null)
                {
                    cards[i].Selected -= OnLicenseSelected;
                    Destroy(cards[i].gameObject);
                }
            }

            cards.Clear();

            if (content == null)
            {
                return;
            }

            for (int i = content.childCount - 1; i >= 0; i--)
            {
                Destroy(content.GetChild(i).gameObject);
            }
        }

        private void OnDestroy()
        {
            foreach (KeyValuePair<Button, ComponentCategory> pair
                     in buttonCategories)
            {
                if (pair.Key != null)
                {
                    pair.Key.onClick.RemoveAllListeners();
                }
            }

            foreach (LicenseCardItem card in cards)
            {
                if (card != null)
                {
                    card.Selected -= OnLicenseSelected;
                }
            }

            buttonCategories.Clear();
        }
    }
}