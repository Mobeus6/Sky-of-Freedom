using UnityEngine;

namespace SkyOfFreedom.Data
{
    [System.Serializable]
    public class DroneComponent
    {
        [SerializeField]
        private ComponentSO component;

        [SerializeField]
        [Min(1)]
        private int amount = 1;

        public ComponentSO Component
        {
            get
            {
                return component;
            }
        }

        public int Amount
        {
            get
            {
                return amount;
            }
        }

        public int TotalCost
        {
            get
            {
                if (component == null)
                {
                    return 0;
                }

                return component.ProductionCost * amount;
            }
        }
    }
}