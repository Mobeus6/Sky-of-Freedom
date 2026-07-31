using System;

namespace SkyOfFreedom.Production
{
    [Serializable]
    public class ProductionTask
    {
        public Guid Id { get; }

        public IProducible Target { get; }

        public int Quantity { get; private set; }

        public int ProducedQuantity { get; private set; }

        public float CurrentItemProgress { get; set; }

        public DateTime CreatedAt { get; }

        public ProductionState State { get; private set; }

        public int RemainingQuantity => Quantity - ProducedQuantity;

        public bool IsCompleted => ProducedQuantity >= Quantity;

        public ProductionTask(
            IProducible target,
            int quantity)
        {
            Id = Guid.NewGuid();
            Target = target;
            Quantity = quantity;

            ProducedQuantity = 0;
            CurrentItemProgress = 0f;

            CreatedAt = DateTime.UtcNow;
            State = ProductionState.Queued;
        }

        public void AddQuantity(int amount)
        {
            if (amount <= 0)
                return;

            Quantity += amount;
        }

        public void Start()
        {
            if (State == ProductionState.Queued ||
                State == ProductionState.Paused)
            {
                State = ProductionState.Working;
            }
        }

        public void Pause()
        {
            if (State == ProductionState.Working)
            {
                State = ProductionState.Paused;
            }
        }

        public void Cancel()
        {
            State = ProductionState.Cancelled;
        }
        public void CompleteCurrentItem()
        {
            CurrentItemProgress = 1f;
        }
        public void ProduceOne()
        {
            if (State != ProductionState.Working)
                return;

            ProducedQuantity++;
            CurrentItemProgress = 0f;

            if (IsCompleted)
            {
                State = ProductionState.Completed;
            }
        }
    }
}