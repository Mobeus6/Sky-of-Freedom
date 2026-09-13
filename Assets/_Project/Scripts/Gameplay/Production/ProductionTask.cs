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

        public ProductionTask(
            Guid id, IProducible target, int quantity, int producedQuantity,
            float currentItemProgress, DateTime createdAt, ProductionState state)
        {
            if (id == Guid.Empty || target == null || quantity <= 0 ||
                producedQuantity < 0 || producedQuantity >= quantity ||
                float.IsNaN(currentItemProgress) || float.IsInfinity(currentItemProgress) ||
                currentItemProgress < 0f || currentItemProgress > 1f ||
                (state != ProductionState.Queued && state != ProductionState.Working &&
                 state != ProductionState.Paused && state != ProductionState.WaitingForStorage))
                throw new ArgumentException("Invalid saved production task.");

            if (state == ProductionState.WaitingForStorage && currentItemProgress != 1f)
                throw new ArgumentException("A waiting product must be complete.");
            if (state == ProductionState.Queued &&
                (producedQuantity != 0 || currentItemProgress != 0f))
                throw new ArgumentException("A queued task cannot have production progress.");

            Id = id;
            Target = target;
            Quantity = quantity;
            ProducedQuantity = producedQuantity;
            CurrentItemProgress = currentItemProgress;
            CreatedAt = createdAt;
            State = state;
        }

        public void WaitForStorage()
        {
            if (State != ProductionState.Working &&
                State != ProductionState.WaitingForStorage)
                return;
            CurrentItemProgress = 1f;
            State = ProductionState.WaitingForStorage;
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
            if (State != ProductionState.Working &&
                State != ProductionState.WaitingForStorage)
                return;

            State = ProductionState.Working;
            ProducedQuantity++;
            CurrentItemProgress = 0f;

            if (IsCompleted)
            {
                State = ProductionState.Completed;
            }
        }
    }
}
