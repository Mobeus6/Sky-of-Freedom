using SkyOfFreedom.Data;
using SkyOfFreedom.Managers;
using SkyOfFreedom.Warehouse;
using System.Collections.Generic;
using UnityEngine;

namespace SkyOfFreedom.Contracts
{
    public class ContractManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameDatabase database;
        [SerializeField] private WarehouseManager warehouseManager;
        [SerializeField] private EconomyManager economyManager;

        [Header("Settings")]
        [SerializeField, Min(1)]

        private int maxActiveContracts = 3;

        private readonly List<ContractInstance> activeContracts = new();

        public IReadOnlyList<ContractInstance> ActiveContracts => activeContracts;

        private void Start()
        {
            GenerateContracts();
        }

        public void GenerateContracts()
        {
            activeContracts.Clear();

            while (activeContracts.Count < maxActiveContracts)
            {
                GenerateSingleContract();
            }
        }

        private void GenerateSingleContract()
        {
            List<ContractSO> available = GetAvailableContracts();

            if (available.Count == 0)
            {
                return;
            }

            available.RemoveAll(c =>
                activeContracts.Exists(a => a.Template == c));

            if (available.Count == 0)
            {
                return;
            }

            ContractSO contract =
                available[Random.Range(0, available.Count)];

            activeContracts.Add(
                ContractGenerator.Generate(contract));
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
            // TODO:
            // Research
            // License
            // Reputation

            return true;
        }

        public void CompleteContract(ContractInstance contract)
        {
            if (!activeContracts.Contains(contract))
            {
                return;
            }

            contract.Complete();

            activeContracts.Remove(contract);

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