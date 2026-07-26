using System.Collections.Generic;
using UnityEngine;

namespace SkyOfFreedom.Data
{
    [CreateAssetMenu(fileName = "NewDroneModel", menuName = "Sky of Freedom/Data/Drone Model")]
    public class DroneModelSO : DataSO, IProducible
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

        [Header("Production")]

        [SerializeField]
        [Min(0.1f)]
        private float assemblyTime = 1f;

        [SerializeField]
        private List<DroneComponent> components = new();

        [Header("Statistics")]

        [SerializeField]
        [Min(1)]
        private int flightDistanceKm = 50;

        [SerializeField]
        [Min(1)]
        private int payloadCapacityKg = 5;

        [SerializeField]
        [Range(1, 5)]
        private int durability = 1;

        [SerializeField]
        [Range(1, 5)]
        private int navigation = 1;

        [SerializeField]
        [Range(1, 5)]
        private int stealth = 1;
        float IProducible.ProductionTime
        {
            get
            {
                return AssemblyTime;
            }
        }
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
                    if (component == null)
                    {
                        continue;
                    }

                    total += component.TotalCost;
                }

                return total;
            }
        }

#if UNITY_EDITOR
        public void SetData(
            string id,
            string modelName,
            string description,
            DronePlatform platform,
            DroneType type,
            DroneTier tier,
            float assemblyTime,
            List<DroneComponent> components,
            int flightDistanceKm,
            int payloadCapacityKg,
            int durability,
            int navigation,
            int stealth)
        {
            SetID(id);

            this.modelName = modelName;
            this.description = description;
            this.platform = platform;
            this.type = type;
            this.tier = tier;
            this.assemblyTime = assemblyTime;

            this.components.Clear();
            this.components.AddRange(components);

            this.flightDistanceKm = flightDistanceKm;
            this.payloadCapacityKg = payloadCapacityKg;
            this.durability = durability;
            this.navigation = navigation;
            this.stealth = stealth;
        }
#endif
    }

}