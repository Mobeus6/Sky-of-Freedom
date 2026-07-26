using System;
using UnityEngine;

namespace SkyOfFreedom.Contracts
{
    public static class ContractGenerator
    {
        private const float ComponentProfitMultiplier = 1.40f;
        private const float DroneProfitMultiplier = 1.60f;
        private const float DeadlineMultiplier = 1.50f;

        public static ContractInstance Generate(ContractSO contract)
        {
            int quantity = UnityEngine.Random.Range(
                contract.MinQuantity,
                contract.MaxQuantity + 1);

            int reward = CalculateReward(contract, quantity);
            float deadlineHours = CalculateDeadline(contract, quantity);

            DateTime createdAt = DateTime.UtcNow;
            DateTime expireAt = createdAt.AddHours(deadlineHours);

            return new ContractInstance(
         contract,
         quantity,
         reward,
         deadlineHours);
        }

        private static int CalculateReward(ContractSO contract, int quantity)
        {
            switch (contract.TargetType)
            {
                case ContractTargetType.Component:
                    {
                        int cost = contract.Component.ProductionCost;
                        return Mathf.RoundToInt(cost * quantity * ComponentProfitMultiplier);
                    }

                case ContractTargetType.Drone:
                    {
                        int cost = contract.DroneModel.ProductionCost;
                        return Mathf.RoundToInt(cost * quantity * DroneProfitMultiplier);
                    }

                default:
                    return 0;
            }
        }

        private static float CalculateDeadline(ContractSO contract, int quantity)
        {
            switch (contract.TargetType)
            {
                case ContractTargetType.Component:
                    {
                        return contract.Component.ProductionTime
                               * quantity
                               * DeadlineMultiplier;
                    }

                case ContractTargetType.Drone:
                    {
                        return contract.DroneModel.AssemblyTime
                               * quantity
                               * DeadlineMultiplier;
                    }

                default:
                    return 0f;
            }
        }
    }
}
