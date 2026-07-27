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

        private readonly Queue<ProductionTask> queue = new Queue<ProductionTask>();
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

        public bool Enqueue(ProductionTask task)
        {
            if (task == null)
                return false;

            if (queue.Count >= queueCapacity)
                return false;

            queue.Enqueue(task);
            QueueChanged?.Invoke(this);

            if (currentTask == null)
                StartNextTask();

            return true;
        }

        public bool CancelTask(ProductionTask task)
        {
            if (task == null)
                return false;

            if (currentTask == task)
            {
                currentTask.Cancel();
                currentTask = null;
                currentProgress = 0f;
                StartNextTask();
                return true;
            }

            Queue<ProductionTask> rebuilt = new Queue<ProductionTask>();
            bool removed = false;

            while (queue.Count > 0)
            {
                ProductionTask queued = queue.Dequeue();

                if (!removed && queued == task)
                {
                    queued.Cancel();
                    removed = true;
                    continue;
                }

                rebuilt.Enqueue(queued);
            }

            while (rebuilt.Count > 0)
                queue.Enqueue(rebuilt.Dequeue());
            QueueChanged?.Invoke(this);
            return removed;
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

            while (queue.Count > 0)
            {
                queue.Dequeue().Cancel();
            }
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
        }

        private void StartNextTask()
        {
            if (currentTask != null)
                return;

            if (queue.Count == 0)
                return;

            currentTask = queue.Dequeue();
            currentTask.Start();
            currentProgress = 0f;
        }
    }
}
