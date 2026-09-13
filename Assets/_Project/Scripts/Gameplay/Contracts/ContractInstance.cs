using System;

namespace SkyOfFreedom.Contracts
{
    [Serializable]
    public class ContractInstance
    {
        public ContractSO Template { get; private set; }

        public int Quantity { get; private set; }

        public int Reward { get; private set; }

        public float DeadlineHours { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public DateTime ExpireAt { get; private set; }

        public ContractState State { get; private set; }

        public int DeliveredQuantity { get; private set; }

        public int RemainingQuantity =>
            Quantity - DeliveredQuantity;

        public ContractInstance(
            ContractSO template,
            int quantity,
            int reward,
            float deadlineHours)
        {
            Template = template;

            Quantity = quantity;

            Reward = reward;

            DeadlineHours = deadlineHours;

            CreatedAt = DateTime.UtcNow;

            ExpireAt = CreatedAt.AddHours(deadlineHours);

            State = ContractState.Available;
        }

        public void Accept()
        {
            if (State != ContractState.Available)
            {
                return;
            }

            State = ContractState.InProgress;
        }

        public static ContractInstance Restore(
            ContractSO template, int quantity, int reward, float deadlineHours,
            DateTime createdAt, DateTime expireAt, ContractState state,
            int deliveredQuantity)
        {
            if (template == null || quantity <= 0 || reward < 0 ||
                float.IsNaN(deadlineHours) || float.IsInfinity(deadlineHours) ||
                deadlineHours < 0 || expireAt < createdAt ||
                deliveredQuantity < 0 || deliveredQuantity > quantity ||
                (state != ContractState.InProgress && state != ContractState.Completed) ||
                (state == ContractState.Completed && deliveredQuantity != quantity) ||
                (state == ContractState.InProgress && deliveredQuantity == quantity))
                throw new ArgumentException("Invalid saved contract.");

            return new ContractInstance(template, quantity, reward, 0f)
            {
                DeadlineHours = deadlineHours,
                CreatedAt = createdAt,
                ExpireAt = expireAt,
                State = state,
                DeliveredQuantity = deliveredQuantity
            };
        }

        public void Deliver(int amount)
        {
            if (State != ContractState.InProgress)
            {
                return;
            }

            DeliveredQuantity += amount;

            if (DeliveredQuantity >= Quantity)
            {
                Complete();
            }
        }

        public void Complete()
        {
            if (State != ContractState.InProgress)
            {
                return;
            }

            State = ContractState.Completed;
        }

        public void Fail()
        {
            if (State != ContractState.InProgress)
            {
                return;
            }

            State = ContractState.Failed;
        }

        public bool IsExpired()
        {
            return DateTime.UtcNow >= ExpireAt;
        }
    }
}
