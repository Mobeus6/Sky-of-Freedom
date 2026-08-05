using SkyOfFreedom.Factory;
using System;
using System.Collections.Generic;
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
        private readonly Dictionary<FactoryZoneType, int> zoneLevels =
    new();
        public event Action<FactoryZoneType, int> OnZoneLevelChanged;

        public int Level => level;
        public long Experience => experience;

        #region Initialize

        public override void Initialize()
        {
            base.Initialize();
            foreach (FactoryZoneType zone in Enum.GetValues(typeof(FactoryZoneType)))
            {
                zoneLevels[zone] = 1;
            }
        }

        public override void Shutdown()
        {
            base.Shutdown();
        }

        #endregion

        #region Level
        public int GetRequiredFactoryLevelForQueue(int slotIndex)
        {
            switch (slotIndex)
            {
                case 0: return 1;
                case 1: return 1;
                case 2: return 2;
                case 3: return 3;
                case 4: return 4;
                default: return 5;
            }
        }
        public int GetUnlockedQueueSlots()
        {
            return Mathf.Clamp(level + 1, 2, 5);
        }
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
        public int GetLevel(FactoryZoneType zone)
        {
            if (zoneLevels.TryGetValue(zone, out int level))
                return level;

            return 1;
        }

        public void SetLevel(FactoryZoneType zone, int level)
        {
            level = Mathf.Max(1, level);

            if (zoneLevels.TryGetValue(zone, out int currentLevel))
            {
                if (currentLevel == level)
                    return;

                zoneLevels[zone] = level;
            }
            else
            {
                zoneLevels.Add(zone, level);
            }

            OnZoneLevelChanged?.Invoke(zone, level);
        }

        public void AddLevel(FactoryZoneType zone, int amount)
        {
            if (amount <= 0)
                return;

            SetLevel(zone, GetLevel(zone) + amount);
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
            foreach (FactoryZoneType zone in Enum.GetValues(typeof(FactoryZoneType)))
            {
                zoneLevels[zone] = 1;

                OnZoneLevelChanged?.Invoke(zone, 1);
            }
            OnFactoryLevelChanged?.Invoke(level);
            OnExperienceChanged?.Invoke(experience);
        }

        #endregion
    }
}