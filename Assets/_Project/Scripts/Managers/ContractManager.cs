using SkyOfFreedom.Data;
using SkyOfFreedom.Managers;
using SkyOfFreedom.Production;
using System;
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

        private readonly List<ContractInstance> activeContracts =
            new List<ContractInstance>();

        private readonly List<ContractInstance> availableContracts =
            new List<ContractInstance>();

        private readonly List<ContractInstance> completedContracts =
            new List<ContractInstance>();

        private readonly HashSet<ContractInstance> readyContracts =
            new HashSet<ContractInstance>();

        private ProductionManager productionManager;

        public IReadOnlyList<ContractInstance> ActiveContracts =>
            activeContracts;

        public IReadOnlyList<ContractInstance> AvailableContracts =>
            availableContracts;

        public IReadOnlyList<ContractInstance> CompletedContracts =>
            completedContracts;

        public event Action<ContractInstance>
            OnContractReadyForSubmission;

        public event Action
            OnContractsChanged;

        public override void Initialize()
        {
            if (IsInitialized)
                return;

            base.Initialize();

            productionManager =
                GameManager.Instance != null
                    ? GameManager.Instance.Production
                    : null;

            economyManager =
                economyManager != null
                    ? economyManager
                    : GameManager.Instance?.Economy;

            if (productionManager != null)
            {
                productionManager.OnItemProduced -=
                    OnProductionItemProduced;

                productionManager.OnItemProduced +=
                    OnProductionItemProduced;
            }
        }

        public override void Shutdown()
        {
            if (!IsInitialized)
                return;

            if (productionManager != null)
            {
                productionManager.OnItemProduced -=
                    OnProductionItemProduced;
            }

            readyContracts.Clear();

            OnContractReadyForSubmission = null;
            OnContractsChanged = null;

            base.Shutdown();
        }

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

            OnContractsChanged?.Invoke();
        }

        private bool GenerateSingleContract()
        {
            List<ContractSO> available =
                GetAvailableContracts();

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
       available[UnityEngine.Random.Range(0, available.Count)];

            availableContracts.Add(
                ContractGenerator.Generate(contract));

            return true;
        }

        private List<ContractSO> GetAvailableContracts()
        {
            List<ContractSO> result =
                new List<ContractSO>();

            foreach (ContractSO contract in database.Contracts)
            {
                if (CanGenerate(contract))
                {
                    result.Add(contract);
                }
            }

            return result;
        }

        private bool CanGenerate(
            ContractSO contract)
        {
            if (contract == null)
                return false;

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

            int factoryLevel =
                factoryManager.Level;

            int requiredTier =
                GetContractTier(contract);

            if (requiredTier <= 0)
                return false;

            return factoryLevel >= requiredTier;
        }

        private int GetContractTier(
            ContractSO contract)
        {
            switch (contract.TargetType)
            {
                case ContractTargetType.Drone:

                    if (contract.DroneModel == null)
                        return 0;

                    return (int)contract.DroneModel.Tier;

                case ContractTargetType.Component:

                    if (contract.Component == null)
                        return 0;

                    return contract.Component.Tier;

                default:
                    return 0;
            }
        }

        public void AcceptContract(
            ContractInstance contract)
        {
            if (!availableContracts.Contains(contract))
                return;

            contract.Accept();

            availableContracts.Remove(contract);

            activeContracts.Add(contract);

            readyContracts.Remove(contract);

            GenerateSingleContract();

            OnContractsChanged?.Invoke();
        }

        public bool CanSubmitContract(
            ContractInstance contract)
        {
            if (contract == null)
                return false;

            if (!activeContracts.Contains(contract))
                return false;

            if (contract.State != ContractState.InProgress)
                return false;

            if (GameManager.Instance == null ||
                GameManager.Instance.Warehouse == null)
            {
                return false;
            }

            string itemId =
                GetContractItemId(contract);

            if (string.IsNullOrWhiteSpace(itemId))
                return false;

            return GameManager.Instance.Warehouse.HasItem(
                itemId,
                contract.Quantity);
        }

        public bool TrySubmitContract(
            ContractInstance contract)
        {
            if (!CanSubmitContract(contract))
                return false;

            if (economyManager == null)
            {
                economyManager =
                    GameManager.Instance?.Economy;
            }

            if (economyManager == null)
            {
                Debug.LogError(
                    "ContractManager: EconomyManager is missing.",
                    this);

                return false;
            }

            Warehouse.WarehouseManager warehouse =
                GameManager.Instance?.Warehouse;

            if (warehouse == null)
                return false;

            string itemId =
                GetContractItemId(contract);

            if (string.IsNullOrWhiteSpace(itemId))
                return false;

            if (!warehouse.RemoveItem(
                    itemId,
                    contract.Quantity))
            {
                return false;
            }

            economyManager.AddMoney(
                contract.Reward);

            economyManager.AddReputation(
                contract.Template.ReputationReward);

            contract.Deliver(
                contract.Quantity);

            readyContracts.Remove(contract);

            activeContracts.Remove(contract);

            completedContracts.Add(contract);

            GenerateSingleContract();

            OnContractsChanged?.Invoke();

            return true;
        }

        public void CompleteContract(
            ContractInstance contract)
        {
            TrySubmitContract(contract);
        }

        public void FailContract(
            ContractInstance contract)
        {
            if (!activeContracts.Contains(contract))
                return;

            contract.Fail();

            activeContracts.Remove(contract);

            readyContracts.Remove(contract);

            GenerateSingleContract();

            OnContractsChanged?.Invoke();
        }

        private string GetContractItemId(
            ContractInstance contract)
        {
            if (contract == null ||
                contract.Template == null)
            {
                return null;
            }

            switch (contract.Template.TargetType)
            {
                case ContractTargetType.Drone:

                    if (contract.Template.DroneModel == null)
                        return null;

                    return contract.Template.DroneModel.ID;

                case ContractTargetType.Component:

                    if (contract.Template.Component == null)
                        return null;

                    return contract.Template.Component.ID;

                default:
                    return null;
            }
        }

        private void OnProductionItemProduced(
            IProducible item)
        {
            if (item == null)
                return;

            for (int i = 0;
                 i < activeContracts.Count;
                 i++)
            {
                ContractInstance contract =
                    activeContracts[i];

                if (contract == null)
                    continue;

                if (contract.State !=
                    ContractState.InProgress)
                {
                    continue;
                }

                string requiredItemId =
                    GetContractItemId(contract);

                if (requiredItemId != item.ID)
                    continue;

                if (!CanSubmitContract(contract))
                    continue;

                if (readyContracts.Contains(contract))
                    continue;

                readyContracts.Add(contract);

                OnContractReadyForSubmission?.Invoke(
                    contract);
            }
        }

        private void Update()
        {
            for (int i = activeContracts.Count - 1;
                 i >= 0;
                 i--)
            {
                if (activeContracts[i].IsExpired())
                {
                    FailContract(
                        activeContracts[i]);
                }
            }
        }
    }
}