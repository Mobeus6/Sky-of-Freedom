using System.Collections.Generic;
using SkyOfFreedom.Data;
using SkyOfFreedom.Warehouse;
using UnityEngine;

namespace SkyOfFreedom.Managers
{
    public class ProductionManager : BaseManager
    {
        [Header("References")]
        [SerializeField] private FactoryManager factoryManager;
        [SerializeField] private WarehouseManager warehouseManager;

        [Header("Settings")]
        [SerializeField, Min(0)]
        private int initialProductionZoneCount = 1;

        [SerializeField, Min(0)]
        private int initialAssemblyZoneCount = 1;

        private readonly List<ProductionZone> productionZones = new();
        private readonly List<AssemblyZone> assemblyZones = new();

        public IReadOnlyList<ProductionZone> ProductionZones => productionZones;
        public IReadOnlyList<AssemblyZone> AssemblyZones => assemblyZones;

        public override void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }

            base.Initialize();

            factoryManager ??= GameManager.Instance?.Factory;
            warehouseManager ??= GameManager.Instance?.Inventory;

            CreateZones();

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnTick += HandleTick;
            }
        }

        public override void Shutdown()
        {
            if (!IsInitialized)
            {
                return;
            }

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnTick -= HandleTick;
            }

            ClearZones();

            base.Shutdown();
        }

        public bool StartComponentProduction(ComponentSO component, int quantity)
        {
            if (component == null || quantity <= 0)
            {
                return false;
            }

            ProductionZone zone = GetAvailableProductionZone();

            if (zone == null)
            {
                return false;
            }

            return zone.Enqueue(component, quantity);
        }

        public bool StartDroneAssembly(DroneModelSO droneModel, int quantity)
        {
            if (droneModel == null || quantity <= 0)
            {
                return false;
            }

            AssemblyZone zone = GetAvailableAssemblyZone();

            if (zone == null)
            {
                return false;
            }

            return zone.Enqueue(droneModel, quantity);
        }

        public bool CancelProduction(ProductionTask task)
        {
            if (task == null)
            {
                return false;
            }

            foreach (ProductionZone zone in productionZones)
            {
                if (zone.CancelTask(task))
                {
                    return true;
                }
            }

            foreach (AssemblyZone zone in assemblyZones)
            {
                if (zone.CancelTask(task))
                {
                    return true;
                }
            }

            return false;
        }

        public void ClearAllProduction()
        {
            foreach (ProductionZone zone in productionZones)
            {
                zone.ClearQueue();
            }

            foreach (AssemblyZone zone in assemblyZones)
            {
                zone.ClearQueue();
            }
        }

        private void HandleTick(float deltaTime)
        {
            for (int i = 0; i < productionZones.Count; i++)
            {
                productionZones[i].Tick(deltaTime);
                HandleProducedItems(productionZones[i]);
            }

            for (int i = 0; i < assemblyZones.Count; i++)
            {
                assemblyZones[i].Tick(deltaTime);
                HandleProducedItems(assemblyZones[i]);
            }
        }

        private void HandleProducedItems(ProductionZoneBase zone)
        {
            if (zone == null)
            {
                return;
            }

            zone.ItemProduced -= OnItemProduced;
            zone.ItemProduced += OnItemProduced;
        }

        private void OnItemProduced(IProducible producible)
        {
            if (producible == null || warehouseManager == null)
            {
                return;
            }

            DataSO data = producible as DataSO;

            if (data == null)
            {
                return;
            }

            warehouseManager.AddItem(data.ID, 1);
        }

        private void CreateZones()
        {
            ClearZones();

            for (int i = 0; i < initialProductionZoneCount; i++)
            {
                ProductionZone zone = new ProductionZone();
                zone.ItemProduced += OnItemProduced;
                productionZones.Add(zone);
            }

            for (int i = 0; i < initialAssemblyZoneCount; i++)
            {
                AssemblyZone zone = new AssemblyZone();
                zone.ItemProduced += OnItemProduced;
                assemblyZones.Add(zone);
            }
        }

        private void ClearZones()
        {
            for (int i = 0; i < productionZones.Count; i++)
            {
                productionZones[i].ItemProduced -= OnItemProduced;
                productionZones[i].ClearQueue();
            }

            for (int i = 0; i < assemblyZones.Count; i++)
            {
                assemblyZones[i].ItemProduced -= OnItemProduced;
                assemblyZones[i].ClearQueue();
            }

            productionZones.Clear();
            assemblyZones.Clear();
        }

        private ProductionZone GetAvailableProductionZone()
        {
            for (int i = 0; i < productionZones.Count; i++)
            {
                if (productionZones[i].Queue.Count < productionZones[i].QueueCapacity)
                {
                    return productionZones[i];
                }
            }

            return null;
        }

        private AssemblyZone GetAvailableAssemblyZone()
        {
            for (int i = 0; i < assemblyZones.Count; i++)
            {
                if (assemblyZones[i].Queue.Count < assemblyZones[i].QueueCapacity)
                {
                    return assemblyZones[i];
                }
            }

            return null;
        }
    }
}