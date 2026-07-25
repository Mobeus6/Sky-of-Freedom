using System;
using UnityEngine;

namespace SkyOfFreedom.Managers
{
    public class EconomyManager : BaseManager
    {
        [Header("Currencies")]
        [SerializeField] private long money;
        [SerializeField] private int reputation;

        public event Action<long> OnMoneyChanged;
        public event Action<int> OnReputationChanged;

        #region Properties

        public long Money => money;
        public int Reputation => reputation;

        #endregion

        #region Initialize

        public override void Initialize()
        {
            base.Initialize();
        }

        public override void Shutdown()
        {
            base.Shutdown();
        }

        #endregion

        #region Money

        public bool HasMoney(long amount)
        {
            return money >= amount;
        }

        public bool SpendMoney(long amount)
        {
            if (amount <= 0)
                return false;

            if (!HasMoney(amount))
                return false;

            money -= amount;

            OnMoneyChanged?.Invoke(money);

            return true;
        }

        public void AddMoney(long amount)
        {
            if (amount <= 0)
                return;

            money += amount;

            OnMoneyChanged?.Invoke(money);
        }

        public void SetMoney(long amount)
        {
            money = Mathf.Max(0, (int)amount);

            OnMoneyChanged?.Invoke(money);
        }

        #endregion

        #region Reputation

        public void AddReputation(int amount)
        {
            reputation += amount;

            OnReputationChanged?.Invoke(reputation);
        }

        public bool RemoveReputation(int amount)
        {
            if (amount <= 0)
                return false;

            reputation -= amount;

            if (reputation < 0)
                reputation = 0;

            OnReputationChanged?.Invoke(reputation);

            return true;
        }

        public void SetReputation(int amount)
        {
            reputation = Mathf.Max(0, amount);

            OnReputationChanged?.Invoke(reputation);
        }

        #endregion

        #region Reset

        public void ResetEconomy()
        {
            money = 0;
            reputation = 0;

            OnMoneyChanged?.Invoke(money);
            OnReputationChanged?.Invoke(reputation);
        }

        #endregion
    }
}