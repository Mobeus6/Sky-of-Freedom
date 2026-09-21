using SkyOfFreedom.Factory;
using SkyOfFreedom.Managers;
using UnityEngine;

namespace SkyOfFreedom.Production
{
    public static class ProductionSpeedCalculator
    {
        public static float GetMultiplier(
            ProductionZone zone)
        {
            if (zone == null)
                return 0f;

            return GetMultiplier(zone.ZoneType);
        }

        public static float GetMultiplier(FactoryZoneType zoneType)
        {

            float speed = 1f;

            GameManager gameManager =
                GameManager.Instance;

            if (gameManager != null &&
                gameManager.Factory != null &&
                gameManager.Factory.ProgressionConfig != null)
            {
                FactoryManager factory =
                    gameManager.Factory;

                FactoryProgressionConfig config =
                    factory.ProgressionConfig;

                int zoneLevel =
                    factory.GetLevel(
                        zoneType);

                switch (zoneType)
                {
                    case FactoryZoneType.Production:

                        if (config.TryGetProductionZoneBonus(
                                zoneLevel,
                                out FactoryProgressionConfig.ProductionZoneLevelBonus productionBonus))
                        {
                            speed *=
                                productionBonus.SpeedMultiplier;
                        }

                        break;

                    case FactoryZoneType.Assembly:

                        if (config.TryGetAssemblyZoneBonus(
                                zoneLevel,
                                out FactoryProgressionConfig.AssemblyZoneLevelBonus assemblyBonus))
                        {
                            speed *=
                                assemblyBonus.SpeedMultiplier;
                        }

                        break;
                }
            }

            if (gameManager != null && gameManager.Research != null)
                speed *= gameManager.Research.GetProductionSpeedMultiplier(zoneType);

            // TODO
            // Employees
            // Factory bonuses
            // Events
            // Boosters

            return Mathf.Max(
                0.01f,
                speed);
        }
    }
}
