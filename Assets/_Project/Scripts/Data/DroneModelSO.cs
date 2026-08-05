using SkyOfFreedom.Production;
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
        [SerializeField] private int storageSize = 50;

        public int StorageSize => storageSize;
        [TextArea]
        [SerializeField]
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

        // ===== IProducible =====

        public string Name => modelName;
        public string Description => description;
        public Sprite Icon => icon;

        public int Tier => (int)tier;

        public float ProductionTime => assemblyTime;

        public int ProductionCost
        {
            get
            {
                int total = 0;

                foreach (DroneComponent component in components)
                {
                    if (component == null)
                        continue;

                    total += component.TotalCost;
                }

                return total;
            }
        }

        // ===== Additional Properties =====

        public DronePlatform Platform => platform;
        public DroneType Type => type;

        public DroneTier DroneTier => tier;

        public float AssemblyTime => assemblyTime;

        public IReadOnlyList<DroneComponent> Components => components;

        public int FlightDistanceKm => flightDistanceKm;

        public int PayloadCapacityKg => payloadCapacityKg;

        public int Durability => durability;

        public int Navigation => navigation;

        public int Stealth => stealth;

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