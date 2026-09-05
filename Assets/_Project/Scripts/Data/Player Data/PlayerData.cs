using System;
using System.Collections.Generic;

namespace SkyOfFreedom.Data
{
    [Serializable]
    public class PlayerData
    {
        public int Version = 1;

        public PlayerAccountData Account = new PlayerAccountData();
        public PlayerEconomyData Economy = new PlayerEconomyData();
        public PlayerFactoryData Factory = new PlayerFactoryData();
        public PlayerWarehouseData Warehouse = new PlayerWarehouseData();
        public PlayerProductionData Production = new PlayerProductionData();
        public PlayerResearchData Research = new PlayerResearchData();
        public PlayerLicensesData Licenses = new PlayerLicensesData();
        public PlayerContractsData Contracts = new PlayerContractsData();
        public PlayerStatisticsData Statistics = new PlayerStatisticsData();
    }

    [Serializable]
    public class PlayerAccountData
    {
        public string PlayerId;
        public string CreatedAtUtc;
        public string LastSaveAtUtc;
    }

    [Serializable]
    public class PlayerEconomyData
    {
        public long Money;
        public int Reputation;
    }

    [Serializable]
    public class PlayerFactoryData
    {
        public int FactoryLevel = 1;

        public int WarehouseLevel = 1;
        public int ProductionLevel = 1;
        public int AssemblyLevel = 1;
        public int ResearchLevel = 1;
    }

    [Serializable]
    public class PlayerWarehouseData
    {
        public List<PlayerWarehouseItemData> Items =
            new List<PlayerWarehouseItemData>();
    }

    [Serializable]
    public class PlayerWarehouseItemData
    {
        public string ItemId;
        public int Quantity;
    }

    [Serializable]
    public class PlayerProductionData
    {
        public string LastProcessedAtUtc;

        public List<PlayerProductionTaskData> Tasks =
            new List<PlayerProductionTaskData>();
    }

    [Serializable]
    public class PlayerProductionTaskData
    {
        public string TaskId;
        public string ZoneType;
        public string TargetId;

        public int Quantity;
        public int ProducedQuantity;

        public float CurrentItemProgress;

        public string CreatedAtUtc;

        public string State;
    }

    [Serializable]
    public class PlayerResearchData
    {
        public string LastProcessedAtUtc;
        public string ActiveResearchId;

        public List<PlayerResearchStateData> ResearchStates =
            new List<PlayerResearchStateData>();
    }

    [Serializable]
    public class PlayerResearchStateData
    {
        public string ResearchId;

        public bool IsUnlocked;
        public bool IsCompleted;
        public bool IsResearching;

        public float Progress;
        public float RemainingTime;
        public float TotalResearchTime;

        public string StartedAtUtc;
    }

    [Serializable]
    public class PlayerLicensesData
    {
        public List<string> UnlockedLicenseIds =
            new List<string>();
    }

    [Serializable]
    public class PlayerContractsData
    {
        public List<PlayerContractData> Active =
            new List<PlayerContractData>();

        public List<PlayerContractData> History =
            new List<PlayerContractData>();
    }

    [Serializable]
    public class PlayerContractData
    {
        public string TemplateId;

        public int Quantity;
        public int Reward;
        public float DeadlineHours;

        public string CreatedAtUtc;
        public string ExpireAtUtc;

        public string State;

        public int DeliveredQuantity;
    }

    [Serializable]
    public class PlayerStatisticsData
    {
        public long TotalDroneProduced;
        public long ComponentsProduced;
        public long ContractsCompleted;

        public long MoneyEarned;
        public long ReputationEarned;
    }
}