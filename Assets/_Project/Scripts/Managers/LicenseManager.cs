using System.Collections.Generic;
using SkyOfFreedom.Data;
using UnityEngine;
using SkyOfFreedom.Production;

namespace SkyOfFreedom.Managers
{
    public class LicenseManager : BaseManager
    {
        [Header("References")]
        [SerializeField]
        private GameDatabase database;

        private readonly HashSet<string> unlockedLicenses =
            new HashSet<string>();

        private readonly Dictionary<string, LicenseSO> componentLicenses =
            new Dictionary<string, LicenseSO>();

        public bool CanProduce(IProducible item)
        {
            if (item == null)
            {
                return false;
            }

            if (item is ComponentSO component)
            {
                return CanProduce(component.ID);
            }

            return true;
        }

        public override void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }

            base.Initialize();

            BuildLookup();
        }

        public override void Shutdown()
        {
            if (!IsInitialized)
            {
                return;
            }

            componentLicenses.Clear();

            base.Shutdown();
        }

        private void BuildLookup()
        {
            componentLicenses.Clear();

            if (database == null)
            {
                Debug.LogError(
                    "GameDatabase is not assigned.",
                    this);

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

                componentLicenses[
                    license.UnlockedComponent.ID] = license;
            }
        }

        public bool IsUnlocked(LicenseSO license)
        {
            if (license == null)
            {
                return false;
            }

            return unlockedLicenses.Contains(
                license.ID);
        }

        public bool IsFactoryLevelRequired(
            LicenseSO license)
        {
            if (license == null)
            {
                return false;
            }

            if (GameManager.Instance == null)
            {
                return false;
            }

            if (GameManager.Instance.Factory == null)
            {
                return false;
            }

            return GameManager.Instance.Factory.Level >=
                   license.RequiredFactoryLevel;
        }

        public bool IsLocked(
            LicenseSO license)
        {
            if (license == null)
            {
                return true;
            }

            if (IsUnlocked(license))
            {
                return false;
            }

            return !IsFactoryLevelRequired(
                license);
        }

        public bool IsAvailable(
            LicenseSO license)
        {
            if (license == null)
            {
                return false;
            }

            if (IsUnlocked(license))
            {
                return false;
            }

            return IsFactoryLevelRequired(
                license);
        }

        public bool CanProduce(
            string componentId)
        {
            if (string.IsNullOrEmpty(componentId))
            {
                return false;
            }

            if (!componentLicenses.TryGetValue(
                    componentId,
                    out LicenseSO license))
            {
                return true;
            }

            return IsUnlocked(
                license);
        }

        public bool CanPurchase(
            LicenseSO license)
        {
            if (license == null)
            {
                return false;
            }

            if (IsUnlocked(license))
            {
                return false;
            }

            /*
             * Factory level must be sufficient.
             */
            if (!IsFactoryLevelRequired(
                    license))
            {
                return false;
            }

            /*
             * EconomyManager must exist.
             */
            if (GameManager.Instance == null)
            {
                return false;
            }

            if (GameManager.Instance.Economy == null)
            {
                return false;
            }

            /*
             * Player must have enough money.
             */
            if (!GameManager.Instance.Economy.HasMoney(
                    license.PurchaseCost))
            {
                return false;
            }

            return true;
        }

        public bool Purchase(
            LicenseSO license)
        {
            if (!CanPurchase(
                    license))
            {
                return false;
            }

            EconomyManager economyManager =
                GameManager.Instance.Economy;

            /*
             * Spend money first.
             *
             * If the transaction fails,
             * the license must NOT be unlocked.
             */
            bool moneySpent =
                economyManager.SpendMoney(
                    license.PurchaseCost);

            if (!moneySpent)
            {
                return false;
            }

            /*
             * Only unlock after successful payment.
             */
            Unlock(license);

            return true;
        }

        public void Unlock(
            LicenseSO license)
        {
            if (license == null)
            {
                return;
            }

            unlockedLicenses.Add(
                license.ID);
        }

        public void Lock(
            LicenseSO license)
        {
            if (license == null)
            {
                return;
            }

            unlockedLicenses.Remove(
                license.ID);
        }

        public void ResetLicenses()
        {
            unlockedLicenses.Clear();
        }

        public IReadOnlyCollection<string>
            GetUnlockedLicenses()
        {
            return unlockedLicenses;
        }

        public void LoadUnlockedLicenses(
            IEnumerable<string> ids)
        {
            unlockedLicenses.Clear();

            if (ids == null)
            {
                return;
            }

            foreach (string id in ids)
            {
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                unlockedLicenses.Add(id);
            }
        }
    }
}