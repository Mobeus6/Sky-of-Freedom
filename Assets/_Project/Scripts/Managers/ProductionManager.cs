using SkyOfFreedom.Data;
using SkyOfFreedom.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace SkyOfFreedom.Production
{
    public class ProductionManager : BaseManager
    {
        [SerializeField]
        private List<ProductionZone> productionZones = new List<ProductionZone>();

        public IReadOnlyList<ProductionZone> Zones => productionZones;

        public override void Initialize()
        {
            if (IsInitialized)
                return;

            base.Initialize();

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

        public ProductionZone GetAvailableZone(ProductionZoneType type)
        {
            ProductionZone bestZone = null;
            int smallestQueue = int.MaxValue;

            foreach (ProductionZone zone in productionZones)
            {
                if (zone == null)
                    continue;

                if (zone.ZoneType != type)
                    continue;

                if (zone.Queue.Count >= zone.QueueCapacity)
                    continue;

                int queueSize = zone.Queue.Count;

                if (zone.CurrentTask != null)
                    queueSize++;

                if (queueSize < smallestQueue)
                {
                    smallestQueue = queueSize;
                    bestZone = zone;
                }
            }

            return bestZone;
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