using SkyOfFreedom.Data;
using SkyOfFreedom.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace SkyOfFreedom.Production
{
    public class ProductionManager : BaseManager
    {
        private DatabaseManager databaseManager;
        [SerializeField]
        private List<ProductionZone> productionZones = new List<ProductionZone>();

        public IReadOnlyList<ProductionZone> Zones => productionZones;

        public override void Initialize()
        {
            if (IsInitialized)
                return;

            base.Initialize();
            databaseManager = GameManager.Instance.Database;

            foreach (ProductionZone zone in productionZones)
            {
                if (zone == null)
                    continue;

                zone.ItemProduced += OnItemProduced;
                zone.TaskCompleted += OnTaskCompleted;
            }
        }

        public override void Shutdown()
        {
            if (!IsInitialized)
                return;

            foreach (ProductionZone zone in productionZones)
            {
                if (zone == null)
                    continue;

                zone.ItemProduced -= OnItemProduced;
                zone.TaskCompleted -= OnTaskCompleted;
                zone.ClearQueue();
            }

            base.Shutdown();
        }

        private void Update()
        {
            if (!IsInitialized)
                return;

            float deltaTime = Time.deltaTime;

            foreach (ProductionZone zone in productionZones)
            {
                if (zone != null)
                    zone.Tick(deltaTime);
            }
        }
        private void QueueComponent(ComponentSO component)
        {
            Debug.Log($"Queue component: {component.Name}");
        }

        private void QueueDrone(DroneModelSO drone)
        {
            Debug.Log($"Queue drone: {drone.Name}");
        }
        public void QueueProduction(IProducible producible)
        {
            if (producible == null)
                return;

            if (producible is ComponentSO component)
            {
                QueueComponent(component);
                return;
            }

            if (producible is DroneModelSO drone)
            {
                QueueDrone(drone);
                return;
            }
        }
        private ProductionZone GetAvailableZone(ProductionZoneType zoneType)
        {
            foreach (ProductionZone zone in productionZones)
            {
                if (zone == null)
                    continue;

                if (zone.ZoneType != zoneType)
                    continue;

                return zone;
            }

            return null;
        }
        public bool QueueProduction(ProductionZoneType zoneType, IProducible item, int quantity)
        {
            if (item == null || quantity <= 0)
                return false;

            ProductionZone zone = GetAvailableZone(zoneType);

            if (zone == null)
                return false;

            if (!GameManager.Instance.License.CanProduce(item))
                return false;

            if (!ProductionRecipeProcessor.CanProduce(item, quantity))
                return false;

            if (!ProductionRecipeProcessor.Consume(item, quantity))
                return false;

            return zone.Enqueue(new ProductionTask(item, quantity));
        }

        public void ClearAll()
        {
            foreach (ProductionZone zone in productionZones)
            {
                zone?.ClearQueue();
            }
        }

        public List<IProducible> GetAvailableItems(
            ProductionView view,
            ProductionCategory category)
        {
            List<IProducible> result = new();

            switch (view)
            {
                case ProductionView.Components:

                    foreach (ComponentSO component in databaseManager.Database.Components)
                    {
                        if (component == null)
                            continue;

                        // All Categories
                        if (category == ProductionCategory.All)
                        {
                            result.Add(component);
                            continue;
                        }

                        // Конкретна категорія
                        if (component.Category == category)
                        {
                            result.Add(component);
                        }
                    }

                    break;

                case ProductionView.Drones:

                    foreach (DroneModelSO drone in databaseManager.Database.DroneModels)
                    {
                        if (drone == null)
                            continue;

                        result.Add(drone);
                    }

                    break;
            }

            return result;
        }

        private void OnTaskCompleted(ProductionZone zone, ProductionTask task)
        {
            Debug.Log($"{task.Target.ID} production completed.");
        }

        private void OnItemProduced(ProductionZone zone, IProducible item)
        {
            if (item == null)
                return;

            GameManager.Instance?.Warehouse?.AddItem(item.ID, 1);
        }
    }
}