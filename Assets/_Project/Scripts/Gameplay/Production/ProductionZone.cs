using SkyOfFreedom.Data;
using SkyOfFreedom.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkyOfFreedom.Production
{
    /// <summary>
    /// Generic production zone capable of producing any IProducible.
    /// Designed as the replacement for ProductionZone/AssemblyZone hierarchy.
    /// </summary>
    public class ProductionZone : MonoBehaviour
    {
        [SerializeField] private ProductionZoneType zoneType;
        [SerializeField] private int queueCapacity = 5;
        [SerializeField] private int level = 1;

        private readonly List<ProductionTask> queue = new List<ProductionTask>();
        private ProductionTask currentTask;
        public IReadOnlyCollection<ProductionTask> Queue => queue;
        private float currentProgress;

        public event Action<ProductionZone, IProducible> ItemProduced;
        public event Action<ProductionZone, ProductionTask> TaskCompleted;
        public event Action<ProductionZone> QueueChanged;

        public ProductionZoneType ZoneType => zoneType;
        public int QueueCapacity => queueCapacity;
        public int Level => level;
        public ProductionTask CurrentTask => currentTask;
        public bool IsBusy => currentTask != null;
        public IEnumerable<ProductionTask> Tasks
        {
            get
            {
                if (currentTask != null)
                    yield return currentTask;

                foreach (ProductionTask task in queue)
                    yield return task;
            }
        }
        public bool Enqueue(ProductionTask task)
        {
            if (task == null)
                return false;

            if (zoneType == ProductionZoneType.Production &&
                task.Target is DroneModelSO)
            {
                Debug.LogError("Drone cannot be produced in Production Zone.");
                return false;
            }

            if (zoneType == ProductionZoneType.Assembly &&
                task.Target is ComponentSO)
            {
                Debug.LogError("Component cannot be assembled in Assembly Zone.");
                return false;
            }
            Debug.Log(
             $"CurrentTask={(currentTask != null ? 1 : 0)} Queue={queue.Count} Capacity={queueCapacity}");
            if ((currentTask != null ? 1 : 0) + queue.Count >= QueueCapacity)
            {
                Debug.Log("Queue Full");
                return false;
            }
            if (task == null)
                return false;

            if (queue.Count >= queueCapacity)
                return false;

            queue.Add(task);
            QueueChanged?.Invoke(this);

            if (currentTask == null)
                StartNextTask();

            return true;
        }
        public bool SpeedUpTask(ProductionTask task)
        {
            if (task == null)
                return false;

            if (task != currentTask)
                return false;

            currentProgress = task.Target.ProductionTime;

            return true;
        }
        public bool CancelTask(ProductionTask task)
        {
            if (task == null)
                return false;

            if (task == currentTask)
            {
                currentTask.Cancel();
                currentTask = null;

                StartNextTask();

                QueueChanged?.Invoke(this);
                return true;
            }

            if (queue.Remove(task))
            {
                task.Cancel();

                QueueChanged?.Invoke(this);
                return true;
            }

            return false;
        }

        private void AddProducedItem(IProducible item)
        {
            GameManager.Instance?.Warehouse?.AddItem(item.ID, 1);
        }

        public void ClearQueue()
        {
            currentTask?.Cancel();
            currentTask = null;
            currentProgress = 0f;

            foreach (ProductionTask task in queue)
            {
                task.Cancel();
            }

            queue.Clear();

            QueueChanged?.Invoke(this);
        }

        public void Tick(float deltaTime)
        {
            if (currentTask == null)
            {
                StartNextTask();

                if (currentTask == null)
                    return;
            }

            float speedMultiplier = ProductionSpeedCalculator.GetMultiplier(this);
            currentProgress += deltaTime * speedMultiplier;

            currentTask.CurrentItemProgress = currentProgress / currentTask.Target.ProductionTime;

            if (currentProgress < currentTask.Target.ProductionTime)
                return;

            currentProgress = 0f;

            currentTask.ProduceOne();
            ItemProduced?.Invoke(this, currentTask.Target);

            if (currentTask.RemainingQuantity > 0)
                return;

            ProductionTask completed = currentTask;

            TaskCompleted?.Invoke(this, completed);

            currentTask = null;

            StartNextTask();

            QueueChanged?.Invoke(this);

        }

        private void StartNextTask()
        {
            if (currentTask != null)
                return;

            if (queue.Count == 0)
                return;

            currentTask = queue[0];
            queue.RemoveAt(0);
            currentTask.Start();
            currentProgress = 0f;

            QueueChanged?.Invoke(this);

        }
    }
}
