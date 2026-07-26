using System;
using System.Collections.Generic;
using SkyOfFreedom.Data;

namespace SkyOfFreedom.Production
{
    public abstract class ProductionZoneBase
    {
        private readonly List<ProductionTask> queue = new();

        public IReadOnlyList<ProductionTask> Queue => queue;

        public ProductionTask CurrentTask
        {
            get
            {
                if (queue.Count == 0)
                {
                    return null;
                }

                return queue[0];
            }
        }

        public int QueueCapacity { get; protected set; } = 5;

        public event Action<IProducible> ItemProduced;

        public event Action<ProductionTask> TaskCompleted;

        private readonly ProductionSpeedCalculator speedCalculator = new();
        
        public bool Enqueue(IProducible target, int quantity)
        {
            if (target == null)
            {
                return false;
            }

            if (quantity <= 0)
            {
                return false;
            }

            if (!CanProduce(target))
            {
                return false;
            }

            ProductionTask lastTask = queue.Count > 0 ? queue[^1] : null;

            if (lastTask != null &&
                lastTask.State == ProductionState.Queued &&
                ReferenceEquals(lastTask.Target, target))
            {
                lastTask.AddQuantity(quantity);
                return true;
            }

            if (queue.Count >= QueueCapacity)
            {
                return false;
            }

            queue.Add(new ProductionTask(target, quantity));

            return true;
        }

        public bool CancelTask(ProductionTask task)
        {
            if (task == null)
            {
                return false;
            }

            if (!queue.Remove(task))
            {
                return false;
            }

            task.Cancel();

            return true;
        }

        public void ClearQueue()
        {
            foreach (ProductionTask task in queue)
            {
                task.Cancel();
            }

            queue.Clear();
        }

        public void Tick(float deltaTime)
        {
            ProductionTask task = CurrentTask;

            if (task == null)
            {
                return;
            }

            if (task.State == ProductionState.Queued)
            {
                task.Start();
            }

            if (task.State != ProductionState.Working)
            {
                return;
            }

            task.CurrentItemProgress +=
     deltaTime * speedCalculator.GetSpeed(this);

            while (task.CurrentItemProgress >= task.Target.ProductionTime)
            {
                task.CurrentItemProgress -= task.Target.ProductionTime;

                task.ProduceOne();

                ItemProduced?.Invoke(task.Target);

                if (!task.IsCompleted)
                {
                    continue;
                }

                TaskCompleted?.Invoke(task);

                queue.RemoveAt(0);

                break;
            }
        }

        protected abstract bool CanProduce(IProducible target);
    }
}