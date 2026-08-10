using SkyOfFreedom.Data;
using SkyOfFreedom.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace SkyOfFreedom.Contracts
{
    public class ContractManager : BaseManager
    {
        [Header("References")]
        [SerializeField] private GameDatabase database;

        [SerializeField] private EconomyManager economyManager;

        [Header("Settings")]
        [SerializeField, Min(1)]
        private int maxActiveContracts = 10;

        private readonly List<ContractInstance> activeContracts = new();
        private readonly List<ContractInstance> availableContracts = new();
        private readonly List<ContractInstance> completedContracts = new();

        public IReadOnlyList<ContractInstance> ActiveContracts => activeContracts;

        public IReadOnlyList<ContractInstance> AvailableContracts =>
            availableContracts;

        public IReadOnlyList<ContractInstance> CompletedContracts =>
            completedContracts;

        private void Start()
        {
            GenerateContracts();
        }

        public void GenerateContracts()
        {
            availableContracts.Clear();

            while (availableContracts.Count < maxActiveContracts)
            {
                if (!GenerateSingleContract())
                {
                    break;
                }
            }
        }

        private bool GenerateSingleContract()
        {
            List<ContractSO> available = GetAvailableContracts();

            if (available.Count == 0)
            {
                return false;
            }

            available.RemoveAll(c =>
                activeContracts.Exists(a => a.Template == c) ||
                availableContracts.Exists(a => a.Template == c));

            if (available.Count == 0)
            {
                return false;
            }

            ContractSO contract =
                available[Random.Range(0, available.Count)];

            availableContracts.Add(
                ContractGenerator.Generate(contract));

            return true;
        }

        private List<ContractSO> GetAvailableContracts()
        {
            List<ContractSO> result = new();

            foreach (ContractSO contract in database.Contracts)
            {
                if (CanGenerate(contract))
                {
                    result.Add(contract);
                }
            }

            return result;
        }

        private bool CanGenerate(ContractSO contract)
        {
            if (contract == null)
            {
                return false;
            }

            FactoryManager factoryManager =
                GameManager.Instance != null
                    ? GameManager.Instance.Factory
                    : null;

            if (factoryManager == null)
            {
                Debug.LogError(
                    "ContractManager: FactoryManager is missing.",
                    this);

                return false;
            }

            int factoryLevel = factoryManager.Level;

            int requiredTier = GetContractTier(contract);

            if (requiredTier <= 0)
            {
                return false;
            }

            return factoryLevel >= requiredTier;
        }

        private int GetContractTier(ContractSO contract)
        {
            switch (contract.TargetType)
            {
                case ContractTargetType.Drone:
                    if (contract.DroneModel == null)
                    {
                        return 0;
                    }

                    return (int)contract.DroneModel.Tier;

                case ContractTargetType.Component:
                    if (contract.Component == null)
                    {
                        return 0;
                    }

                    return contract.Component.Tier;

                default:
                    return 0;
            }
        }

        public void AcceptContract(ContractInstance contract)
        {
            if (!availableContracts.Contains(contract))
            {
                return;
            }

            contract.Accept();

            availableContracts.Remove(contract);

            activeContracts.Add(contract);

            GenerateSingleContract();
        }

        public void CompleteContract(ContractInstance contract)
        {
            if (!activeContracts.Contains(contract))
            {
                return;
            }

            contract.Complete();

            activeContracts.Remove(contract);

            completedContracts.Add(contract);

            GenerateSingleContract();
        }

        public void FailContract(ContractInstance contract)
        {
            if (!activeContracts.Contains(contract))
            {
                return;
            }

            contract.Fail();

            activeContracts.Remove(contract);

            GenerateSingleContract();
        }

        private void Update()
        {
            for (int i = activeContracts.Count - 1; i >= 0; i--)
            {
                if (activeContracts[i].IsExpired())
                {
                    FailContract(activeContracts[i]);
                }
            }
        }
    }
}