using UnityEngine;

namespace SkyOfFreedom.Production
{
    public static class ProductionSpeedCalculator
    {
        public static float GetMultiplier(ProductionZone zone)
        {
            if (zone == null)
                return 0f;

            float speed = 1f;

            // TODO
            // Zone Level
            // Employees
            // Research
            // Factory bonuses
            // Events
            // Boosters

            return Mathf.Max(0.01f, speed);
        }
    }
}