using System;
using UnityEngine;

namespace SkyOfFreedom.Managers
{
    public class FactoryManager : BaseManager
    {
        [Header("Factory")]
        [SerializeField] private int level = 1;
        [SerializeField] private long experience;

        public event Action<int> OnFactoryLevelChanged;
        public event Action<long> OnExperienceChanged;

        #region Properties

        public int Level => level;
        public long Experience => experience;

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

        #region Level

        public void SetLevel(int value)
        {
            value = Mathf.Max(1, value);

            if (level == value)
                return;

            level = value;

            OnFactoryLevelChanged?.Invoke(level);
        }

        public void AddLevel(int amount)
        {
            if (amount <= 0)
                return;

            SetLevel(level + amount);
        }

        #endregion

        #region Experience

        public void AddExperience(long amount)
        {
            if (amount <= 0)
                return;

            experience += amount;

            OnExperienceChanged?.Invoke(experience);
        }

        public void SetExperience(long value)
        {
            experience = value < 0 ? 0 : value;

            OnExperienceChanged?.Invoke(experience);
        }

        #endregion

        #region Reset

        public void ResetFactory()
        {
            level = 1;
            experience = 0;

            OnFactoryLevelChanged?.Invoke(level);
            OnExperienceChanged?.Invoke(experience);
        }

        #endregion
    }
}