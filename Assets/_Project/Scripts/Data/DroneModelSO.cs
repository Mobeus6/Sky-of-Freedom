using System.Collections.Generic;
using UnityEngine;

namespace SkyOfFreedom.Data
{
    [CreateAssetMenu(fileName = "NewDroneModel", menuName = "Sky of Freedom/Data/Drone Model")]
    public class DroneModelSO : DataSO
    {
        [Header("General")]

        [SerializeField]
        private string modelName;

        [SerializeField]
        private Sprite icon;

        [SerializeField]
        [TextArea]
        private string description;

        [Header("Classification")]

        [SerializeField]
        private DronePlatform platform;

        [SerializeField]
        private DroneType type;

        [SerializeField]
        private DroneTier tier;

        [Header("Requirements")]

        [SerializeField]
        [Min(1)]
        private int requiredFactoryLevel = 1;

        [Header("Production")]

        [SerializeField]
        [Min(0.1f)]
        private float assemblyTime = 1f;

        [SerializeField]
        private List<DroneComponent> components = new();

        [Header("Statistics")]

        [SerializeField]
        private int flightDistanceKm;

        [SerializeField]
        private int payloadCapacityKg;

        [SerializeField]
        [Range(1, 5)]
        private int durability = 1;

        [SerializeField]
        [Range(1, 5)]
        private int navigation = 1;

        [SerializeField]
        [Range(1, 5)]
        private int stealth = 1;

        public string ModelName
        {
            get
            {
                return modelName;
            }
        }

        public Sprite Icon
        {
            get
            {
                return icon;
            }
        }

        public string Description
        {
            get
            {
                return description;
            }
        }

        public DronePlatform Platform
        {
            get
            {
                return platform;
            }
        }

        public DroneType Type
        {
            get
            {
                return type;
            }
        }

        public DroneTier Tier
        {
            get
            {
                return tier;
            }
        }

        public int RequiredFactoryLevel
        {
            get
            {
                return requiredFactoryLevel;
            }
        }

        public float AssemblyTime
        {
            get
            {
                return assemblyTime;
            }
        }

        public IReadOnlyList<DroneComponent> Components
        {
            get
            {
                return components;
            }
        }

        public int FlightDistanceKm
        {
            get
            {
                return flightDistanceKm;
            }
        }

        public int PayloadCapacityKg
        {
            get
            {
                return payloadCapacityKg;
            }
        }

        public int Durability
        {
            get
            {
                return durability;
            }
        }

        public int Navigation
        {
            get
            {
                return navigation;
            }
        }

        public int Stealth
        {
            get
            {
                return stealth;
            }
        }

        public int ProductionCost
        {
            get
            {
                int total = 0;

                foreach (DroneComponent component in components)
                {
                    total += component.TotalCost;
                }

                return total;
            }
        }
    }
}