using SkyOfFreedom.Data;
using SkyOfFreedom.Factory;
using SkyOfFreedom.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkyOfFreedom.Production
{
    public class ProductionZone : MonoBehaviour
    {
        [SerializeField]
        private FactoryZoneType zoneType;

        [SerializeField]
        private int queueCapacity = 5;

        private readonly List<ProductionTask> queue =
            new List<ProductionTask>();

        private ProductionTask currentTask;
        private float currentProgress;

        public int TaskCount =>
            queue.Count +
            (currentTask != null ? 1 : 0);

        public event Action<ProductionZone, IProducible>
            ItemProduced;

        public event Action<ProductionZone, ProductionTask>
            TaskCompleted;

        public event Action<ProductionZone>
            QueueChanged;

        public FactoryZoneType ZoneType =>
            zoneType;

        public int QueueCapacity =>
            queueCapacity;

        public int Level
        {
            get
            {
                if (GameManager.Instance == null ||
                    GameManager.Instance.Factory == null)
                {
                    return 1;
                }

                return GameManager.Instance.Factory.GetLevel(
                    zoneType);
            }
        }

        public ProductionTask CurrentTask =>
            currentTask;

        public bool IsBusy =>
            currentTask != null;

        public IReadOnlyCollection<ProductionTask> Queue =>
            queue;

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

        public bool CanAccept(IProducible item)
        {
            return isActiveAndEnabled && TaskCount < queueCapacity && Supports(item);
        }

        public bool Supports(IProducible item)
        {
            return (zoneType == FactoryZoneType.Production && item is ComponentSO) ||
                   (zoneType == FactoryZoneType.Assembly && item is DroneModelSO);
        }

        // Restore without consuming ingredients or issuing products/events.
        public void LoadTasks(IReadOnlyList<ProductionTask> tasks)
        {
            if (tasks == null) throw new ArgumentNullException(nameof(tasks));
            for (int i = 0; i < tasks.Count; i++)
            {
                if (tasks[i] == null || !Supports(tasks[i].Target) ||
                    (i > 0 && tasks[i].State != ProductionState.Queued))
                    throw new ArgumentException("Invalid production queue.");
            }

            queue.Clear();
            currentTask = null;
            currentProgress = 0f;
            for (int i = 0; i < tasks.Count; i++)
            {
                if (i == 0)
                {
                    currentTask = tasks[i];
                    if (currentTask.State == ProductionState.Queued) currentTask.Start();
                    currentProgress = currentTask.CurrentItemProgress *
                        Mathf.Max(0.01f, currentTask.Target.ProductionTime);
                }
                else queue.Add(tasks[i]);
            }
            // An old queue may exceed a reduced capacity: retain it, block new orders.
            QueueChanged?.Invoke(this);
        }

        private void Awake()
        {
            queueCapacity =
                Mathf.Max(
                    1,
                    queueCapacity);
        }

        public bool Enqueue(
            ProductionTask task)
        {
            if (task == null || !Supports(task.Target) ||
                task.State != ProductionState.Queued)
                return false;

            if (!isActiveAndEnabled)
                return false;

            if (zoneType ==
                    FactoryZoneType.Production &&
                task.Target is DroneModelSO)
            {
                Debug.LogError(
                    "Drone cannot be produced in Production Zone.",
                    this);

                return false;
            }

            if (zoneType ==
                    FactoryZoneType.Assembly &&
                task.Target is ComponentSO)
            {
                Debug.LogError(
                    "Component cannot be assembled in Assembly Zone.",
                    this);

                return false;
            }

            int taskCount =
                TaskCount;

            if (taskCount >= queueCapacity)
            {
                Debug.Log(
                    $"Production queue is full: {name}",
                    this);

                return false;
            }

            queue.Add(task);

            if (currentTask == null)
                StartNextTask();

            QueueChanged?.Invoke(this);

            return true;
        }

        public bool SpeedUpTask(
            ProductionTask task)
        {
            if (task == null)
                return false;

            if (task != currentTask || task.State != ProductionState.Working)
                return false;

            currentProgress =
                Mathf.Max(0.01f, task.Target.ProductionTime);
            task.CompleteCurrentItem();
            QueueChanged?.Invoke(this);

            return true;
        }

        public bool CancelTask(
            ProductionTask task)
        {
            if (task == null)
                return false;

            if (task == currentTask)
            {
                currentTask.Cancel();

                currentTask = null;

                currentProgress = 0f;

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

        public void ClearQueue()
        {
            if (currentTask != null)
            {
                currentTask.Cancel();
                currentTask = null;
            }

            currentProgress = 0f;

            foreach (ProductionTask task in queue)
                task.Cancel();

            queue.Clear();

            QueueChanged?.Invoke(this);
        }

        public void Tick(
            float deltaTime)
        {
            if (!isActiveAndEnabled)
                return;

            if (currentTask == null)
            {
                StartNextTask();

                if (currentTask == null)
                    return;

                QueueChanged?.Invoke(this);
            }

            if (currentTask.Target == null)
            {
                Debug.LogError(
                    $"Production task has no target: {name}",
                    this);

                currentTask.Cancel();

                currentTask = null;

                currentProgress = 0f;

                StartNextTask();

                QueueChanged?.Invoke(this);

                return;
            }

            if (currentTask.State == ProductionState.Paused)
                return;
            if (currentTask.State != ProductionState.Working &&
                currentTask.State != ProductionState.WaitingForStorage)
                return;

            float productionTime = Mathf.Max(0.01f, currentTask.Target.ProductionTime);
            if (currentTask.State == ProductionState.Working)
            {
                currentProgress = Mathf.Min(productionTime,
                    currentProgress + Mathf.Max(0f, deltaTime) *
                    ProductionSpeedCalculator.GetMultiplier(this));
                currentTask.CurrentItemProgress = Mathf.Clamp01(currentProgress / productionTime);
                if (currentProgress < productionTime) return;
            }

            Warehouse.WarehouseManager warehouse = GameManager.Instance?.Warehouse;
            if (warehouse == null || !warehouse.TryAddItem(currentTask.Target.ID, 1))
            {
                if (currentTask.State != ProductionState.WaitingForStorage)
                {
                    currentTask.WaitForStorage();
                    QueueChanged?.Invoke(this);
                }
                return;
            }

            // Count only products actually delivered to storage.
            ProductionTask deliveredTask = currentTask;
            currentProgress = 0f;
            deliveredTask.ProduceOne();
            if (deliveredTask.IsCompleted)
            {
                currentTask = null;
                StartNextTask();
            }

            // The queue and warehouse are consistent before notifying consumers.
            ItemProduced?.Invoke(this, deliveredTask.Target);
            if (deliveredTask.IsCompleted) TaskCompleted?.Invoke(this, deliveredTask);
            QueueChanged?.Invoke(this);
        }

        private void StartNextTask()
        {
            if (currentTask != null || queue.Count == 0) return;
            currentTask = queue[0];
            queue.RemoveAt(0);
            currentTask.Start();
            currentProgress = 0f;
        }

        public void AdvanceOffline(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds <= 0)
                return;
            while (seconds > 0 && currentTask != null && isActiveAndEnabled)
            {
                if (currentTask.State == ProductionState.Paused) break;
                double needed = Math.Max(0, Mathf.Max(0.01f, currentTask.Target.ProductionTime)
                    - currentProgress) / ProductionSpeedCalculator.GetMultiplier(this);
                double step = Math.Min(seconds, needed);
                ProductionTask before = currentTask;
                int produced = before.ProducedQuantity;
                Tick((float)step);
                seconds -= step;
                if (currentTask != null && currentTask.State == ProductionState.WaitingForStorage)
                    break;
                if (currentTask == before && before.ProducedQuantity == produced)
                {
                    if (seconds <= 0) break;
                    // Resolve floating point rounding at the completion boundary.
                    Tick(0.001f);
                    seconds = Math.Max(0, seconds - 0.001);
                    if (currentTask == before && before.ProducedQuantity == produced) break;
                }
            }
        }

    }
}
