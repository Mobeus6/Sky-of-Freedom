using SkyOfFreedom.Data;
using SkyOfFreedom.Factory;
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
        public void QueueComponent(ComponentSO component)
        {
            QueueProduction(
                FactoryZoneType.Production,
                component,
                1);
        }
        public void QueueDrone(DroneModelSO drone)
        {
            QueueProduction(
                FactoryZoneType.Assembly,
                drone,
                1);
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
       
        public bool QueueProduction(FactoryZoneType zoneType, IProducible item, int quantity)
        {
            if (item == null || quantity <= 0)
            {

                return false;
            }

            ProductionZone zone = GetAvailableZone(zoneType);

            if (zone == null)
            {
                Debug.Log("2. Zone not found");
                return false;
            }
            if (!GameManager.Instance.License.CanProduce(item))
            {
                Debug.Log("3. License failed");
                return false;
            }

            if (!ProductionRecipeProcessor.CanProduce(item, quantity))
            {
                Debug.Log($"CanProduce failed: {item.ID}");
                return false;
            }

            if (!ProductionRecipeProcessor.Consume(item, quantity))
            {
                Debug.Log($"Consume failed: {item.ID}");
                return false;
            }

            return zone.Enqueue(new ProductionTask(item, quantity));
        }
        private ProductionZone GetAvailableZone(FactoryZoneType zoneType)
        {
            productionZones.RemoveAll(z => z == null);

            ProductionZone bestZone = null;

            foreach (ProductionZone zone in productionZones)
            {

                if (zone.ZoneType != zoneType)
                {
                    continue;
                }

                if (zone.TaskCount >= zone.QueueCapacity)
                {
                    continue;
                }

                if (bestZone == null)
                {
                    bestZone = zone;
                    continue;
                }

                if (zone.TaskCount < bestZone.TaskCount)
                {
                    bestZone = zone;
                }
            }

            return bestZone;
        }

        public void ClearAll()
        {
            foreach (ProductionZone zone in productionZones)
            {
                zone?.ClearQueue();
            }
        }
        public void RegisterZone(ProductionZone zone)
        {
            
            if (zone == null)
                return;

            if (productionZones.Contains(zone))
                return;

            productionZones.Add(zone);

            zone.ItemProduced -= OnItemProduced;
            zone.TaskCompleted -= OnTaskCompleted;

            zone.ItemProduced += OnItemProduced;
            zone.TaskCompleted += OnTaskCompleted;

        }
        public void UnregisterZone(ProductionZone zone)
        {
            if (zone == null)
                return;

            if (!productionZones.Remove(zone))
                return;

            zone.ItemProduced -= OnItemProduced;
            zone.TaskCompleted -= OnTaskCompleted;
        }
        public List<IProducible> GetAvailableItems(
     ProductionView view,
     ComponentCategory category)
        {
            List<IProducible> result = new();

            switch (view)
            {
                case ProductionView.Components:

                    foreach (ComponentSO component in databaseManager.Database.Components)
                    {
                        if (component == null)
                            continue;

                        if (category == ComponentCategory.All)
                        {
                            result.Add(component);
                            continue;
                        }

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
            
        }

        private void OnItemProduced(ProductionZone zone, IProducible item)
        {
            if (item == null)
                return;

            GameManager.Instance?.Warehouse?.AddItem(item.ID, 1);
        }
    }
}