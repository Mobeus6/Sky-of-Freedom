using UnityEngine;

namespace SkyOfFreedom.Production
{
    public class ProductionSpeedCalculator
    {
        public float GetSpeed(ProductionZoneBase zone)
        {
            float speed = 1f;

            speed *= GetZoneLevelMultiplier(zone);
            speed *= GetEmployeeMultiplier(zone);
            speed *= GetResearchMultiplier(zone);
            speed *= GetFactoryMultiplier(zone);
            speed *= GetEventMultiplier(zone);
            speed *= GetBoosterMultiplier(zone);

            return Mathf.Max(0.01f, speed);
        }

        protected virtual float GetZoneLevelMultiplier(ProductionZoneBase zone)
        {
            return 1f;
        }

        protected virtual float GetEmployeeMultiplier(ProductionZoneBase zone)
        {
            return 1f;
        }

        protected virtual float GetResearchMultiplier(ProductionZoneBase zone)
        {
            return 1f;
        }

        protected virtual float GetFactoryMultiplier(ProductionZoneBase zone)
        {
            return 1f;
        }

        protected virtual float GetEventMultiplier(ProductionZoneBase zone)
        {
            return 1f;
        }

        protected virtual float GetBoosterMultiplier(ProductionZoneBase zone)
        {
            return 1f;
        }
    }
}