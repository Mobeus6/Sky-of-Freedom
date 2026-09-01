using System;
using System.Collections.Generic;
using SkyOfFreedom.Contracts;
using SkyOfFreedom.Data;
using SkyOfFreedom.Production;
using UnityEngine;

namespace SkyOfFreedom.Managers
{
    public class FactoryStatisticsManager : BaseManager
    {
        private ProductionManager productionManager;
        private ContractManager contractManager;

        private readonly HashSet<ContractInstance> processedContracts =
            new HashSet<ContractInstance>();

        private long totalDroneProduced;
        private long componentsProduced;
        private long contractsCompleted;
        private long moneyEarned;
        private long reputationEarned;

        public event Action OnStatisticsChanged;

        public long TotalDroneProduced =>
            totalDroneProduced;

        public long ComponentsProduced =>
            componentsProduced;

        public long ContractsCompleted =>
            contractsCompleted;

        public long MoneyEarned =>
            moneyEarned;

        public long ReputationEarned =>
            reputationEarned;

        public override void Initialize()
        {
            if (IsInitialized)
                return;

            base.Initialize();

            if (GameManager.Instance == null)
            {
                Debug.LogError(
                    "FactoryStatisticsManager: GameManager is not available.",
                    this);

                return;
            }

            productionManager =
                GameManager.Instance.Production;

            contractManager =
                GameManager.Instance.Contracts;

            SubscribeToProduction();
            SubscribeToContracts();
        }

        public override void Shutdown()
        {
            if (!IsInitialized)
                return;

            UnsubscribeFromProduction();
            UnsubscribeFromContracts();

            processedContracts.Clear();

            base.Shutdown();
        }

        private void SubscribeToProduction()
        {
            if (productionManager == null)
            {
                Debug.LogError(
                    "FactoryStatisticsManager: ProductionManager is missing.",
                    this);

                return;
            }

            productionManager.OnItemProduced -=
                OnItemProduced;

            productionManager.OnItemProduced +=
                OnItemProduced;
        }

        private void UnsubscribeFromProduction()
        {
            if (productionManager == null)
                return;

            productionManager.OnItemProduced -=
                OnItemProduced;
        }

        private void SubscribeToContracts()
        {
            if (contractManager == null)
            {
                Debug.LogError(
                    "FactoryStatisticsManager: ContractManager is missing.",
                    this);

                return;
            }

            contractManager.OnContractsChanged -=
                OnContractsChanged;

            contractManager.OnContractsChanged +=
                OnContractsChanged;
        }

        private void UnsubscribeFromContracts()
        {
            if (contractManager == null)
                return;

            contractManager.OnContractsChanged -=
                OnContractsChanged;
        }

        private void OnItemProduced(
            IProducible item)
        {
            if (item == null)
                return;

            if (item is DroneModelSO)
            {
                totalDroneProduced++;

                OnStatisticsChanged?.Invoke();

                return;
            }

            if (item is ComponentSO)
            {
                componentsProduced++;

                OnStatisticsChanged?.Invoke();
            }
        }

        private void OnContractsChanged()
        {
            if (contractManager == null)
                return;

            IReadOnlyList<ContractInstance> completedContracts =
                contractManager.CompletedContracts;

            if (completedContracts == null)
                return;

            bool changed = false;

            for (int i = 0;
                 i < completedContracts.Count;
                 i++)
            {
                ContractInstance contract =
                    completedContracts[i];

                if (contract == null)
                    continue;

                if (processedContracts.Contains(contract))
                    continue;

                processedContracts.Add(contract);

                contractsCompleted++;

                moneyEarned +=
                    contract.Reward;

                if (contract.Template != null)
                {
                    reputationEarned +=
                        contract.Template.ReputationReward;
                }

                changed = true;
            }

            if (changed)
            {
                OnStatisticsChanged?.Invoke();
            }
        }
    }
}