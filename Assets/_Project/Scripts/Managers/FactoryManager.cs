using System;
using System.Collections.Generic;
using UnityEngine;
using SkyOfFreedom.Factory;

namespace SkyOfFreedom.Managers
{
    public class FactoryManager : BaseManager
    {
        private const int MinFactoryLevel = 1;
        private const int MaxFactoryLevel = 5;

        private const int MinZoneLevel = 1;
        private const int MaxZoneLevel = 5;

        private static readonly FactoryZoneType[] UpgradeableZones =
        {
            FactoryZoneType.Warehouse,
            FactoryZoneType.Production,
            FactoryZoneType.Assembly,
            FactoryZoneType.Research
        };

        [Header("Factory")]
        [SerializeField]
        [Min(MinFactoryLevel)]
        private int level = MinFactoryLevel;

        [Header("Progression")]
        [SerializeField]
        private FactoryProgressionConfig progressionConfig;

        private readonly Dictionary<FactoryZoneType, int> zoneLevels =
            new Dictionary<FactoryZoneType, int>();

        public event Action<int> OnFactoryLevelChanged;
        public event Action<FactoryZoneType, int> OnZoneLevelChanged;

        public int Level => level;

        public FactoryProgressionConfig ProgressionConfig =>
            progressionConfig;

        #region Initialize

        public override void Initialize()
        {
            base.Initialize();

            InitializeZoneLevels();
        }

        private void InitializeZoneLevels()
        {
            for (int i = 0; i < UpgradeableZones.Length; i++)
            {
                FactoryZoneType zone =
                    UpgradeableZones[i];

                if (!zoneLevels.ContainsKey(zone))
                {
                    zoneLevels.Add(
                        zone,
                        MinZoneLevel);
                }
            }
        }

        public override void Shutdown()
        {
            base.Shutdown();
        }

        #endregion

        #region Factory Level

        public int GetRequiredFactoryLevelForQueue(
            int slotIndex)
        {
            switch (slotIndex)
            {
                case 0:
                    return 1;

                case 1:
                    return 1;

                case 2:
                    return 2;

                case 3:
                    return 3;

                case 4:
                    return 4;

                default:
                    return 5;
            }
        }

        public int GetUnlockedQueueSlots()
        {
            return Mathf.Clamp(
                level + 1,
                2,
                5);
        }

        public void SetLevel(
            int value)
        {
            value = Mathf.Clamp(
                value,
                MinFactoryLevel,
                MaxFactoryLevel);

            if (level == value)
                return;

            level = value;

            OnFactoryLevelChanged?.Invoke(
                level);
        }

        public void AddLevel(
            int amount)
        {
            if (amount <= 0)
                return;

            SetLevel(
                level + amount);
        }

        #endregion

        #region Factory Progression

        public int GetNextFactoryLevel()
        {
            if (level >= MaxFactoryLevel)
                return MaxFactoryLevel;

            return level + 1;
        }

        public bool IsMaxFactoryLevel()
        {
            return level >= MaxFactoryLevel;
        }

        public bool CanUpgradeFactory()
        {
            return CanUpgradeFactory(
                out _);
        }

        public bool CanUpgradeFactory(
            out string reason)
        {
            reason = string.Empty;

            if (level >= MaxFactoryLevel)
            {
                reason =
                    "Factory is already at maximum level.";

                return false;
            }

            if (progressionConfig == null)
            {
                reason =
                    "Factory progression configuration is missing.";

                return false;
            }

            if (GameManager.Instance == null)
            {
                reason =
                    "GameManager is not available.";

                return false;
            }

            EconomyManager economy =
                GameManager.Instance.Economy;

            if (economy == null)
            {
                reason =
                    "EconomyManager is not available.";

                return false;
            }

            int targetLevel =
                level + 1;

            if (!progressionConfig.TryGetFactoryRequirement(
                    targetLevel,
                    out FactoryProgressionConfig.FactoryLevelRequirement requirement))
            {
                reason =
                    $"No progression requirements configured for Factory Level {targetLevel}.";

                return false;
            }

            if (!AreAllZonesReadyForFactoryLevel(
                    targetLevel))
            {
                reason =
                    $"All four factory zones must be Level {targetLevel}.";

                return false;
            }

            if (!economy.HasMoney(
                    requirement.MoneyRequired))
            {
                reason =
                    $"Not enough money. Required: {requirement.MoneyRequired}.";

                return false;
            }

            if (economy.Reputation <
                requirement.ReputationRequired)
            {
                reason =
                    $"Not enough reputation. Required: {requirement.ReputationRequired}.";

                return false;
            }

            return true;
        }

        public bool TryUpgradeFactory()
        {
            if (!CanUpgradeFactory(
                    out _))
            {
                return false;
            }

            if (!progressionConfig.TryGetFactoryRequirement(
                    level + 1,
                    out FactoryProgressionConfig.FactoryLevelRequirement requirement))
            {
                return false;
            }

            EconomyManager economy =
                GameManager.Instance.Economy;

            if (!economy.SpendMoney(
                    requirement.MoneyRequired))
            {
                return false;
            }

            return ApplyFactoryUpgrade();
        }

        public bool TryGetFactoryUpgradeRequirement(
            out FactoryProgressionConfig.FactoryLevelRequirement requirement)
        {
            requirement = null;

            if (level >= MaxFactoryLevel)
                return false;

            if (progressionConfig == null)
                return false;

            return progressionConfig.TryGetFactoryRequirement(
                level + 1,
                out requirement);
        }

        public bool AreAllZonesReadyForFactoryLevel(
            int targetLevel)
        {
            targetLevel =
                Mathf.Clamp(
                    targetLevel,
                    MinFactoryLevel,
                    MaxFactoryLevel);

            for (int i = 0;
                 i < UpgradeableZones.Length;
                 i++)
            {
                FactoryZoneType zone =
                    UpgradeableZones[i];

                if (GetLevel(zone) < targetLevel)
                    return false;
            }

            return true;
        }

        private bool ApplyFactoryUpgrade()
        {
            if (level >= MaxFactoryLevel)
                return false;

            int targetLevel =
                level + 1;

            if (!AreAllZonesReadyForFactoryLevel(
                    targetLevel))
            {
                return false;
            }

            level =
                targetLevel;

            OnFactoryLevelChanged?.Invoke(
                level);

            return true;
        }

        #endregion

        #region Zone Progression

        public int GetNextZoneLevel(
            FactoryZoneType zone)
        {
            int currentLevel =
                GetLevel(zone);

            if (currentLevel >= MaxZoneLevel)
                return MaxZoneLevel;

            return currentLevel + 1;
        }

        public bool IsMaxZoneLevel(
            FactoryZoneType zone)
        {
            return GetLevel(zone) >= MaxZoneLevel;
        }

        public bool CanUpgradeZone(
            FactoryZoneType zone)
        {
            return CanUpgradeZone(
                zone,
                out _);
        }

        public bool CanUpgradeZone(
            FactoryZoneType zone,
            out string reason)
        {
            reason = string.Empty;

            if (!IsUpgradeableZone(zone))
            {
                reason =
                    "This zone cannot be upgraded.";

                return false;
            }

            int currentLevel =
                GetLevel(zone);

            if (currentLevel >= MaxZoneLevel)
            {
                reason =
                    "Zone is already at maximum level.";

                return false;
            }

            if (progressionConfig == null)
            {
                reason =
                    "Factory progression configuration is missing.";

                return false;
            }

            if (GameManager.Instance == null)
            {
                reason =
                    "GameManager is not available.";

                return false;
            }

            EconomyManager economy =
                GameManager.Instance.Economy;

            if (economy == null)
            {
                reason =
                    "EconomyManager is not available.";

                return false;
            }

            int targetLevel =
                currentLevel + 1;

            if (targetLevel > level + 1)
            {
                reason =
                    $"Factory must be at least Level {targetLevel - 1}.";

                return false;
            }

            if (!progressionConfig.TryGetZoneRequirement(
                    targetLevel,
                    out FactoryProgressionConfig.ZoneLevelRequirement requirement))
            {
                reason =
                    $"No zone progression requirements configured for Level {targetLevel}.";

                return false;
            }

            if (!economy.HasMoney(
                    requirement.MoneyRequired))
            {
                reason =
                    $"Not enough money. Required: {requirement.MoneyRequired}.";

                return false;
            }

            if (economy.Reputation <
                requirement.ReputationRequired)
            {
                reason =
                    $"Not enough reputation. Required: {requirement.ReputationRequired}.";

                return false;
            }

            return true;
        }

        public bool TryUpgradeZone(
            FactoryZoneType zone)
        {
            if (!CanUpgradeZone(
                    zone,
                    out _))
            {
                return false;
            }

            if (!progressionConfig.TryGetZoneRequirement(
                    GetNextZoneLevel(zone),
                    out FactoryProgressionConfig.ZoneLevelRequirement requirement))
            {
                return false;
            }

            EconomyManager economy =
                GameManager.Instance.Economy;

            if (!economy.SpendMoney(
                    requirement.MoneyRequired))
            {
                return false;
            }

            return ApplyZoneUpgrade(
                zone);
        }

        public bool TryGetZoneUpgradeRequirement(
            FactoryZoneType zone,
            out FactoryProgressionConfig.ZoneLevelRequirement requirement)
        {
            requirement = null;

            if (!IsUpgradeableZone(zone))
                return false;

            int targetLevel =
                GetNextZoneLevel(zone);

            if (targetLevel > MaxZoneLevel)
                return false;

            if (progressionConfig == null)
                return false;

            return progressionConfig.TryGetZoneRequirement(
                targetLevel,
                out requirement);
        }

        private bool ApplyZoneUpgrade(
            FactoryZoneType zone)
        {
            if (!IsUpgradeableZone(zone))
                return false;

            int currentLevel =
                GetLevel(zone);

            if (currentLevel >= MaxZoneLevel)
                return false;

            int targetLevel =
                currentLevel + 1;

            if (targetLevel > level + 1)
                return false;

            SetLevel(
                zone,
                targetLevel);

            return true;
        }

        #endregion

        #region Zone Levels

        public int GetLevel(
            FactoryZoneType zone)
        {
            if (zoneLevels.TryGetValue(
                    zone,
                    out int zoneLevel))
            {
                return zoneLevel;
            }

            return MinZoneLevel;
        }

        public void SetLevel(
            FactoryZoneType zone,
            int zoneLevel)
        {
            if (!IsUpgradeableZone(zone))
                return;

            zoneLevel =
                Mathf.Clamp(
                    zoneLevel,
                    MinZoneLevel,
                    MaxZoneLevel);

            int currentLevel =
                GetLevel(zone);

            if (currentLevel == zoneLevel)
                return;

            zoneLevels[zone] =
                zoneLevel;

            OnZoneLevelChanged?.Invoke(
                zone,
                zoneLevel);
        }

        public void AddLevel(
            FactoryZoneType zone,
            int amount)
        {
            if (amount <= 0)
                return;

            SetLevel(
                zone,
                GetLevel(zone) + amount);
        }

        public bool IsUpgradeableZone(
            FactoryZoneType zone)
        {
            for (int i = 0;
                 i < UpgradeableZones.Length;
                 i++)
            {
                if (UpgradeableZones[i] == zone)
                    return true;
            }

            return false;
        }

        #endregion

        #region Save State

        public void LoadFactoryState(
            int factoryLevel,
            int warehouseLevel,
            int productionLevel,
            int assemblyLevel,
            int researchLevel)
        {
            level =
                Mathf.Clamp(
                    factoryLevel,
                    MinFactoryLevel,
                    MaxFactoryLevel);

            SetLoadedZoneLevel(
                FactoryZoneType.Warehouse,
                warehouseLevel);

            SetLoadedZoneLevel(
                FactoryZoneType.Production,
                productionLevel);

            SetLoadedZoneLevel(
                FactoryZoneType.Assembly,
                assemblyLevel);

            SetLoadedZoneLevel(
                FactoryZoneType.Research,
                researchLevel);
        }

        private void SetLoadedZoneLevel(
            FactoryZoneType zone,
            int zoneLevel)
        {
            zoneLevels[zone] =
                Mathf.Clamp(
                    zoneLevel,
                    MinZoneLevel,
                    MaxZoneLevel);
        }

        #endregion

        #region Reset

        public void ResetFactory()
        {
            level =
                MinFactoryLevel;

            for (int i = 0;
                 i < UpgradeableZones.Length;
                 i++)
            {
                FactoryZoneType zone =
                    UpgradeableZones[i];

                zoneLevels[zone] =
                    MinZoneLevel;

                OnZoneLevelChanged?.Invoke(
                    zone,
                    MinZoneLevel);
            }

            OnFactoryLevelChanged?.Invoke(
                level);
        }

        #endregion
    }
}