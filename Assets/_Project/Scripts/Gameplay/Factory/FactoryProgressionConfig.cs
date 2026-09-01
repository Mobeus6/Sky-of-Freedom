using System;
using UnityEngine;

namespace SkyOfFreedom.Factory
{
    [CreateAssetMenu(
        fileName = "FactoryProgressionConfig",
        menuName = "Sky of Freedom/Factory/Factory Progression Config")]
    public class FactoryProgressionConfig : ScriptableObject
    {
        [Serializable]
        public class FactoryLevelRequirement
        {
            [SerializeField]
            [Min(2)]
            private int targetLevel = 2;

            [SerializeField]
            [Min(0)]
            private long moneyRequired;

            [SerializeField]
            [Min(0)]
            private long reputationRequired;

            [SerializeField]
            [TextArea(2, 5)]
            private string rewardDescription;

            public int TargetLevel => targetLevel;

            public long MoneyRequired =>
                moneyRequired;

            public long ReputationRequired =>
                reputationRequired;

            public string RewardDescription =>
                rewardDescription;
        }

        [Serializable]
        public class ZoneLevelRequirement
        {
            [SerializeField]
            [Min(2)]
            private int targetLevel = 2;

            [SerializeField]
            [Min(0)]
            private long moneyRequired;

            [SerializeField]
            [Min(0)]
            private long reputationRequired;

            public int TargetLevel => targetLevel;

            public long MoneyRequired =>
                moneyRequired;

            public long ReputationRequired =>
                reputationRequired;
        }

        [Serializable]
        public class ProductionZoneLevelBonus
        {
            [SerializeField]
            [Range(1, 5)]
            private int level = 1;

            [SerializeField]
            [Min(0f)]
            private float speedMultiplier = 1f;

            public int Level => level;

            public float SpeedMultiplier =>
                speedMultiplier;
        }

        [Serializable]
        public class AssemblyZoneLevelBonus
        {
            [SerializeField]
            [Range(1, 5)]
            private int level = 1;

            [SerializeField]
            [Min(0f)]
            private float speedMultiplier = 1f;

            public int Level => level;

            public float SpeedMultiplier =>
                speedMultiplier;
        }

        [Serializable]
        public class WarehouseZoneLevelBonus
        {
            [SerializeField]
            [Range(1, 5)]
            private int level = 1;

            [SerializeField]
            [Min(0f)]
            private float capacityMultiplier = 1f;

            public int Level => level;

            public float CapacityMultiplier =>
                capacityMultiplier;
        }

        [Serializable]
        public class ResearchZoneLevelBonus
        {
            [SerializeField]
            [Range(1, 5)]
            private int level = 1;

            [SerializeField]
            [Min(0f)]
            private float speedMultiplier = 1f;

            public int Level => level;

            public float SpeedMultiplier =>
                speedMultiplier;
        }

        [Header("Factory Upgrade Requirements")]
        [SerializeField]
        private FactoryLevelRequirement[] factoryRequirements =
            new FactoryLevelRequirement[4];

        [Header("Zone Upgrade Requirements")]
        [SerializeField]
        private ZoneLevelRequirement[] zoneRequirements =
            new ZoneLevelRequirement[4];

        [Header("Production Zone Bonuses")]
        [SerializeField]
        private ProductionZoneLevelBonus[] productionZoneBonuses =
            new ProductionZoneLevelBonus[5];

        [Header("Assembly Zone Bonuses")]
        [SerializeField]
        private AssemblyZoneLevelBonus[] assemblyZoneBonuses =
            new AssemblyZoneLevelBonus[5];

        [Header("Warehouse Zone Bonuses")]
        [SerializeField]
        private WarehouseZoneLevelBonus[] warehouseZoneBonuses =
            new WarehouseZoneLevelBonus[5];

        [Header("Research Zone Bonuses")]
        [SerializeField]
        private ResearchZoneLevelBonus[] researchZoneBonuses =
            new ResearchZoneLevelBonus[5];

        public FactoryLevelRequirement[] FactoryRequirements =>
            factoryRequirements;

        public ZoneLevelRequirement[] ZoneRequirements =>
            zoneRequirements;

        public ProductionZoneLevelBonus[] ProductionZoneBonuses =>
            productionZoneBonuses;

        public AssemblyZoneLevelBonus[] AssemblyZoneBonuses =>
            assemblyZoneBonuses;

        public WarehouseZoneLevelBonus[] WarehouseZoneBonuses =>
            warehouseZoneBonuses;

        public ResearchZoneLevelBonus[] ResearchZoneBonuses =>
            researchZoneBonuses;

        public bool TryGetFactoryRequirement(
            int targetLevel,
            out FactoryLevelRequirement requirement)
        {
            if (factoryRequirements != null)
            {
                for (int i = 0;
                     i < factoryRequirements.Length;
                     i++)
                {
                    FactoryLevelRequirement current =
                        factoryRequirements[i];

                    if (current == null)
                        continue;

                    if (current.TargetLevel == targetLevel)
                    {
                        requirement = current;
                        return true;
                    }
                }
            }

            requirement = null;
            return false;
        }

        public bool TryGetZoneRequirement(
            int targetLevel,
            out ZoneLevelRequirement requirement)
        {
            if (zoneRequirements != null)
            {
                for (int i = 0;
                     i < zoneRequirements.Length;
                     i++)
                {
                    ZoneLevelRequirement current =
                        zoneRequirements[i];

                    if (current == null)
                        continue;

                    if (current.TargetLevel == targetLevel)
                    {
                        requirement = current;
                        return true;
                    }
                }
            }

            requirement = null;
            return false;
        }

        public bool TryGetProductionZoneBonus(
            int level,
            out ProductionZoneLevelBonus bonus)
        {
            if (productionZoneBonuses != null)
            {
                for (int i = 0;
                     i < productionZoneBonuses.Length;
                     i++)
                {
                    ProductionZoneLevelBonus current =
                        productionZoneBonuses[i];

                    if (current == null)
                        continue;

                    if (current.Level == level)
                    {
                        bonus = current;
                        return true;
                    }
                }
            }

            bonus = null;
            return false;
        }

        public bool TryGetAssemblyZoneBonus(
            int level,
            out AssemblyZoneLevelBonus bonus)
        {
            if (assemblyZoneBonuses != null)
            {
                for (int i = 0;
                     i < assemblyZoneBonuses.Length;
                     i++)
                {
                    AssemblyZoneLevelBonus current =
                        assemblyZoneBonuses[i];

                    if (current == null)
                        continue;

                    if (current.Level == level)
                    {
                        bonus = current;
                        return true;
                    }
                }
            }

            bonus = null;
            return false;
        }

        public bool TryGetWarehouseZoneBonus(
            int level,
            out WarehouseZoneLevelBonus bonus)
        {
            if (warehouseZoneBonuses != null)
            {
                for (int i = 0;
                     i < warehouseZoneBonuses.Length;
                     i++)
                {
                    WarehouseZoneLevelBonus current =
                        warehouseZoneBonuses[i];

                    if (current == null)
                        continue;

                    if (current.Level == level)
                    {
                        bonus = current;
                        return true;
                    }
                }
            }

            bonus = null;
            return false;
        }

        public bool TryGetResearchZoneBonus(
            int level,
            out ResearchZoneLevelBonus bonus)
        {
            if (researchZoneBonuses != null)
            {
                for (int i = 0;
                     i < researchZoneBonuses.Length;
                     i++)
                {
                    ResearchZoneLevelBonus current =
                        researchZoneBonuses[i];

                    if (current == null)
                        continue;

                    if (current.Level == level)
                    {
                        bonus = current;
                        return true;
                    }
                }
            }

            bonus = null;
            return false;
        }
    }
}