using SkyOfFreedom.Data;
using SkyOfFreedom.Factory;
using SkyOfFreedom.Managers;
using System;
using System.Globalization;
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
        public event Action OnProductionChanged;

        public bool HasRunningTasks
        {
            get
            {
                foreach (ProductionZone zone in productionZones)
                    if (zone != null && zone.CurrentTask?.State == ProductionState.Working)
                        return true;
                return false;
            }
        }

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
            OnProductionChanged = null;

            base.Shutdown();
        }

        private void Update()
        {
            if (!IsInitialized || GameManager.Instance == null ||
                !GameManager.Instance.IsGameReady ||
                GameManager.Instance.ProductionPausedAtUtc.HasValue)
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
            if (GameManager.Instance == null || !GameManager.Instance.IsGameReady ||
                item == null || quantity <= 0)
            {
                return false;
            }

            if (!MeetsFactoryLevel(item))
                return false;

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

            if (!zone.CanAccept(item)) return false;

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
            {
                ProductionRecipeProcessor.Refund(item, quantity);
                return false;
            }

            return true;
        }

        public static bool MeetsFactoryLevel(IProducible item)
        {
            return item != null && GameManager.Instance != null &&
                GameManager.Instance.Factory != null &&
                GameManager.Instance.Factory.Level >= Mathf.Max(1, item.Tier);
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
            zone.QueueChanged -= OnQueueChanged;
            zone.QueueChanged += OnQueueChanged;
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
            zone.QueueChanged -= OnQueueChanged;
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

        private void OnQueueChanged(ProductionZone zone)
        {
            OnProductionChanged?.Invoke();
        }

        public PlayerProductionData GetSaveData()
        {
            return CaptureProduction();
        }

        public void AdvanceOffline(double seconds)
        {
            foreach (ProductionZone zone in productionZones)
                if (zone != null)
                    zone.AdvanceOffline(seconds);
        }

        private PlayerProductionData CaptureProduction()
        {
            PlayerProductionData data = new PlayerProductionData
            {
                LastProcessedAtUtc = (GameManager.Instance?.ProductionPausedAtUtc ?? DateTime.UtcNow).ToString("O")
            };
            foreach (ProductionZone zone in productionZones)
            {
                if (zone == null) continue;
                foreach (ProductionTask task in zone.Tasks)
                {
                    data.Tasks.Add(new PlayerProductionTaskData
                    {
                        TaskId = task.Id.ToString("D"),
                        ZoneType = zone.ZoneType.ToString(),
                        TargetId = task.Target.ID,
                        Quantity = task.Quantity,
                        ProducedQuantity = task.ProducedQuantity,
                        CurrentItemProgress = task.CurrentItemProgress,
                        CreatedAtUtc = task.CreatedAt.ToUniversalTime().ToString("O"),
                        State = task.State.ToString()
                    });
                }
            }
            return data;
        }

        public void LoadSaveData(PlayerProductionData data)
        {
            if (!IsInitialized || databaseManager?.Database == null)
                throw new InvalidOperationException("Production is not initialized.");

            Dictionary<FactoryZoneType, List<ProductionTask>> restored =
                new Dictionary<FactoryZoneType, List<ProductionTask>>();
            foreach (ProductionZone zone in productionZones)
            {
                if (zone == null) continue;
                if (restored.ContainsKey(zone.ZoneType))
                    throw new InvalidOperationException("Production zone types must be unique.");
                restored.Add(zone.ZoneType, new List<ProductionTask>());
            }

            HashSet<Guid> taskIds = new HashSet<Guid>();
            if (data?.Tasks != null)
            {
                foreach (PlayerProductionTaskData saved in data.Tasks)
                {
                    if (saved == null ||
                        !Enum.TryParse(saved.ZoneType, out FactoryZoneType type) ||
                        !restored.TryGetValue(type, out List<ProductionTask> tasks) ||
                        !Guid.TryParse(saved.TaskId, out Guid id) || !taskIds.Add(id) ||
                        !Enum.TryParse(saved.State, out ProductionState state) ||
                        !DateTime.TryParse(saved.CreatedAtUtc, CultureInfo.InvariantCulture,
                            DateTimeStyles.RoundtripKind, out DateTime createdAt) ||
                        string.IsNullOrWhiteSpace(saved.TargetId))
                        throw new InvalidOperationException("Invalid saved production data.");

                    IProducible target = databaseManager.Database.GetData(saved.TargetId) as IProducible;
                    ProductionZone zone = GetZone(type);
                    if (target == null || !zone.Supports(target) ||
                        (tasks.Count > 0 && state != ProductionState.Queued))
                        throw new InvalidOperationException("Saved production target or order is invalid.");

                    tasks.Add(new ProductionTask(id, target, saved.Quantity,
                        saved.ProducedQuantity, saved.CurrentItemProgress, createdAt, state));
                }
            }

            // Validate all tasks before changing any live queue. Never consume again.
            foreach (ProductionZone zone in productionZones)
                if (zone != null) zone.LoadTasks(restored[zone.ZoneType]);
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

            OnItemProduced?.Invoke(item);
        }
    }
}
