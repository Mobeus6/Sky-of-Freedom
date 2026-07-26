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

            State = ContractState.Active;
        }

        public void Deliver(int amount)
        {
            DeliveredQuantity += amount;

            if (DeliveredQuantity >= Quantity)
            {
                Complete();
            }
        }
        public void Complete()
        {
            State = ContractState.Completed;
        }

        public void Fail()
        {
            State = ContractState.Failed;
        }

        public bool IsExpired()
        {
            return DateTime.UtcNow >= ExpireAt;
        }
    }
}