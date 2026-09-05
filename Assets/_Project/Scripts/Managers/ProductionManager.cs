using SkyOfFreedom.Data;
using SkyOfFreedom.Factory;
using SkyOfFreedom.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkyOfFreedom.Production
{
    public class ProductionManager : BaseManager
    {
        [Header("Production Zones")]
        [SerializeField]
        private ProductionZone productionZone;

        [SerializeField]
        private ProductionZone assemblyZone;

        private DatabaseManager databaseManager;

        private readonly List<ProductionZone> productionZones =
            new List<ProductionZone>();

        public IReadOnlyList<ProductionZone> Zones =>
            productionZones;

        public event Action<IProducible> OnItemProduced;

        public override void Initialize()
        {
            if (IsInitialized)
                return;

            base.Initialize();

            databaseManager =
                GameManager.Instance.Database;

            RegisterZone(productionZone);
            RegisterZone(assemblyZone);
        }

        public override void Shutdown()
        {
            if (!IsInitialized)
                return;

            for (int i = 0;
                 i < productionZones.Count;
                 i++)
            {
                ProductionZone zone =
                    productionZones[i];

                if (zone == null)
                    continue;

                UnsubscribeFromZone(zone);
            }

            productionZones.Clear();

            OnItemProduced = null;

            base.Shutdown();
        }

        private void Update()
        {
            if (!IsInitialized)
                return;

            float deltaTime =
                Time.deltaTime;

            for (int i = productionZones.Count - 1;
                 i >= 0;
                 i--)
            {
                ProductionZone zone =
                    productionZones[i];

                if (zone == null)
                {
                    productionZones.RemoveAt(i);
                    continue;
                }

                zone.Tick(deltaTime);
            }
        }

        public ProductionZone GetZone(
            FactoryZoneType zoneType)
        {
            for (int i = 0;
                 i < productionZones.Count;
                 i++)
            {
                ProductionZone zone =
                    productionZones[i];

                if (zone == null)
                    continue;

                if (zone.ZoneType == zoneType)
                    return zone;
            }

            return null;
        }

        public void QueueComponent(
            ComponentSO component)
        {
            QueueProduction(
                FactoryZoneType.Production,
                component,
                1);
        }

        public void QueueDrone(
            DroneModelSO drone)
        {
            QueueProduction(
                FactoryZoneType.Assembly,
                drone,
                1);
        }

        public void QueueProduction(
            IProducible producible)
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
            }
        }

        public bool QueueProduction(
            FactoryZoneType zoneType,
            IProducible item,
            int quantity)
        {
            if (item == null ||
                quantity <= 0)
            {
                return false;
            }

            ProductionZone zone =
                GetAvailableZone(zoneType);

            if (zone == null)
            {
                Debug.LogWarning(
                    $"No available production zone for type: {zoneType}");

                return false;
            }

            if (GameManager.Instance == null ||
                GameManager.Instance.License == null)
            {
                Debug.LogError(
                    "ProductionManager: GameManager or LicenseManager is not available.");

                return false;
            }

            if (!GameManager.Instance.License.CanProduce(item))
            {
                Debug.Log(
                    "License failed");

                return false;
            }

            if (!ProductionRecipeProcessor.CanProduce(
                    item,
                    quantity))
            {
                Debug.Log(
                    $"CanProduce failed: {item.ID}");

                return false;
            }

            if (!ProductionRecipeProcessor.Consume(
                    item,
                    quantity))
            {
                Debug.Log(
                    $"Consume failed: {item.ID}");

                return false;
            }

            ProductionTask task =
                new ProductionTask(
                    item,
                    quantity);

            if (!zone.Enqueue(task))
                return false;

            return true;
        }

        private ProductionZone GetAvailableZone(
            FactoryZoneType zoneType)
        {
            ProductionZone bestZone = null;

            for (int i = 0;
                 i < productionZones.Count;
                 i++)
            {
                ProductionZone zone =
                    productionZones[i];

                if (zone == null)
                    continue;

                if (!zone.isActiveAndEnabled)
                    continue;

                if (zone.ZoneType != zoneType)
                    continue;

                if (zone.TaskCount >=
                    zone.QueueCapacity)
                {
                    continue;
                }

                if (bestZone == null ||
                    zone.TaskCount <
                    bestZone.TaskCount)
                {
                    bestZone = zone;
                }
            }

            return bestZone;
        }

        public void ClearAll()
        {
            for (int i = 0;
                 i < productionZones.Count;
                 i++)
            {
                ProductionZone zone =
                    productionZones[i];

                if (zone != null)
                    zone.ClearQueue();
            }
        }

        public void RegisterZone(
            ProductionZone zone)
        {
            if (zone == null)
                return;

            if (!IsInitialized)
            {

                return;
            }

            if (productionZones.Contains(zone))
                return;

            productionZones.Add(zone);

            SubscribeToZone(zone);
        }

        public void UnregisterZone(
            ProductionZone zone)
        {
            if (zone == null)
                return;

            if (!productionZones.Remove(zone))
                return;

            UnsubscribeFromZone(zone);

        }

        private void SubscribeToZone(
            ProductionZone zone)
        {
            zone.ItemProduced -=
                OnItemProducedInternal;

            zone.TaskCompleted -=
                OnTaskCompleted;

            zone.ItemProduced +=
                OnItemProducedInternal;

            zone.TaskCompleted +=
                OnTaskCompleted;
        }

        private void UnsubscribeFromZone(
            ProductionZone zone)
        {
            zone.ItemProduced -=
                OnItemProducedInternal;

            zone.TaskCompleted -=
                OnTaskCompleted;
        }

        public List<IProducible> GetAvailableItems(
            ProductionView view,
            ComponentCategory category)
        {
            List<IProducible> result =
                new List<IProducible>();

            switch (view)
            {
                case ProductionView.Components:

                    foreach (ComponentSO component
                             in databaseManager.Database.Components)
                    {
                        if (component == null)
                            continue;

                        if (category ==
                                ComponentCategory.All ||
                            component.Category ==
                                category)
                        {
                            result.Add(component);
                        }
                    }

                    break;

                case ProductionView.Drones:

                    foreach (DroneModelSO drone
                             in databaseManager.Database.DroneModels)
                    {
                        if (drone == null)
                            continue;

                        result.Add(drone);
                    }

                    break;
            }

            return result;
        }

        private void OnTaskCompleted(
            ProductionZone zone,
            ProductionTask task)
        {
        }

        private void OnItemProducedInternal(
            ProductionZone zone,
            IProducible item)
        {
            if (item == null)
                return;

            GameManager.Instance?.Warehouse?.AddItem(
                item.ID,
                1);

            OnItemProduced?.Invoke(item);
        }
    }
}