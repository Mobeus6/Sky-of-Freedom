using System.Collections.Generic;
using SkyOfFreedom.Data;
using UnityEngine;

namespace SkyOfFreedom.Managers
{
    public class LicenseManager : MonoBehaviour
    {
        public static LicenseManager Instance { get; private set; }

        [Header("References")]
        [SerializeField]
        private GameDatabase database;

        private readonly HashSet<string> unlockedLicenses = new();

        private readonly Dictionary<ComponentSO, LicenseSO> componentLicenses = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            BuildLookup();
        }

        private void BuildLookup()
        {
            componentLicenses.Clear();

            if (database == null)
            {
                Debug.LogError("GameDatabase is not assigned.", this);
                return;
            }

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

                componentLicenses[license.UnlockedComponent] = license;
            }
        }

        public bool IsUnlocked(LicenseSO license)
        {
            if (license == null)
            {
                return true;
            }

            return unlockedLicenses.Contains(license.ID);
        }

        public bool CanProduce(ComponentSO component)
        {
            if (component == null)
            {
                return false;
            }

            if (!componentLicenses.TryGetValue(component, out LicenseSO license))
            {
                return true;
            }

            return IsUnlocked(license);
        }

        public bool CanPurchase(LicenseSO license)
        {
            if (license == null)
            {
                return false;
            }

            if (IsUnlocked(license))
            {
                return false;
            }

            // TODO:
            // Перевірити рівень фабрики.
            // Перевірити кількість грошей.

            return true;
        }

        public bool Purchase(LicenseSO license)
        {
            if (!CanPurchase(license))
            {
                return false;
            }

            // TODO:
            // EconomyManager.Instance.SpendMoney(license.PurchaseCost);

            Unlock(license);

            return true;
        }

        public void Unlock(LicenseSO license)
        {
            if (license == null)
            {
                return;
            }

            unlockedLicenses.Add(license.ID);
        }

        public void Lock(LicenseSO license)
        {
            if (license == null)
            {
                return;
            }

            unlockedLicenses.Remove(license.ID);
        }

        public void ResetLicenses()
        {
            unlockedLicenses.Clear();
        }

        public IReadOnlyCollection<string> GetUnlockedLicenses()
        {
            return unlockedLicenses;
        }

        public void LoadUnlockedLicenses(IEnumerable<string> ids)
        {
            unlockedLicenses.Clear();

            foreach (string id in ids)
            {
                unlockedLicenses.Add(id);
            }
        }
    }
}