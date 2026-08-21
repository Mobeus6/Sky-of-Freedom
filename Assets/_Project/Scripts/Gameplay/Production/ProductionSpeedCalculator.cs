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
                        zone.ZoneType);

                switch (zone.ZoneType)
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

            // TODO
            // Employees
            // Research
            // Factory bonuses
            // Events
            // Boosters

            return Mathf.Max(
                0.01f,
                speed);
        }
    }
}