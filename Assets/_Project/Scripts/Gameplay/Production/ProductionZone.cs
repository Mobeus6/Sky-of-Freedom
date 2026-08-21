using SkyOfFreedom.Data;
using SkyOfFreedom.Factory;
using SkyOfFreedom.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkyOfFreedom.Production
{
    /// <summary>
    /// Generic production zone capable of producing any IProducible.
    /// The zone registers itself with ProductionManager while it exists
    /// in the Gameplay scene. No external bootstrap object is required.
    /// </summary>
    public class ProductionZone : MonoBehaviour
    {
        [SerializeField] private FactoryZoneType zoneType;
        [SerializeField] private int queueCapacity = 5;

        private readonly List<ProductionTask> queue =
            new List<ProductionTask>();

        private ProductionTask currentTask;
        private float currentProgress;
        private bool registered;

        public int TaskCount =>
            queue.Count + (currentTask != null ? 1 : 0);

        public event Action<ProductionZone, IProducible> ItemProduced;
        public event Action<ProductionZone, ProductionTask> TaskCompleted;
        public event Action<ProductionZone> QueueChanged;

        public FactoryZoneType ZoneType => zoneType;
        public int QueueCapacity => queueCapacity;

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

        public ProductionTask CurrentTask => currentTask;
        public bool IsBusy => currentTask != null;

        public IReadOnlyCollection<ProductionTask> Queue => queue;

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

        private void Awake()
        {
            queueCapacity =
                Mathf.Max(
                    1,
                    queueCapacity);

            RegisterWithProductionManager();
        }

        private void OnEnable()
        {
            RegisterWithProductionManager();
        }

        private void OnDisable()
        {
            UnregisterFromProductionManager();
        }

        private void OnDestroy()
        {
            UnregisterFromProductionManager();
        }

        private void RegisterWithProductionManager()
        {
            if (registered)
                return;

            GameManager gameManager =
                GameManager.Instance;

            if (gameManager == null)
                return;

            ProductionManager productionManager =
                gameManager.Production;

            if (productionManager == null)
                return;

            productionManager.RegisterZone(this);
            registered = true;
        }

        private void UnregisterFromProductionManager()
        {
            if (!registered)
                return;

            GameManager gameManager =
                GameManager.Instance;

            if (gameManager != null &&
                gameManager.Production != null)
            {
                gameManager.Production.UnregisterZone(this);
            }

            registered = false;
        }

        public bool Enqueue(
            ProductionTask task)
        {
            if (task == null)
                return false;

            if (!isActiveAndEnabled)
                return false;

            if (zoneType == FactoryZoneType.Production &&
                task.Target is DroneModelSO)
            {
                Debug.LogError(
                    "Drone cannot be produced in Production Zone.",
                    this);

                return false;
            }

            if (zoneType == FactoryZoneType.Assembly &&
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

            if (task != currentTask)
                return false;

            currentProgress =
                task.Target.ProductionTime;

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

            float productionTime =
                Mathf.Max(
                    0.01f,
                    currentTask.Target.ProductionTime);

            float speedMultiplier =
                ProductionSpeedCalculator.GetMultiplier(this);

            currentProgress +=
                deltaTime * speedMultiplier;

            currentTask.CurrentItemProgress =
                Mathf.Clamp01(
                    currentProgress / productionTime);

            if (currentProgress < productionTime)
                return;

            currentProgress = 0f;

            currentTask.ProduceOne();

            ItemProduced?.Invoke(
                this,
                currentTask.Target);

            if (currentTask.RemainingQuantity > 0)
                return;

            ProductionTask completed =
                currentTask;

            TaskCompleted?.Invoke(
                this,
                completed);

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

            currentTask =
                queue[0];

            queue.RemoveAt(0);

            currentTask.Start();

            currentProgress = 0f;
        }
    }
}