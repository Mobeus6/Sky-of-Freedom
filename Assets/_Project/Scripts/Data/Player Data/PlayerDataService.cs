using System;
using System.Collections.Generic;

using SkyOfFreedom.Data;
using SkyOfFreedom.Factory;
using SkyOfFreedom.Managers;
using SkyOfFreedom.Warehouse;
using UnityEngine;

namespace SkyOfFreedom.Services
{
    public class PlayerDataService
    {
        private readonly PlayerStartConfigSO startConfig;

        public PlayerData CurrentData { get; private set; }

        public bool HasData => CurrentData != null;

        public PlayerDataService(PlayerStartConfigSO startConfig)
        {
            this.startConfig = startConfig;
        }

        public PlayerData CreateNewPlayerData(string playerId)
        {
            if (string.IsNullOrEmpty(playerId))
            {
                throw new ArgumentException(
                    "Player ID cannot be empty.",
                    nameof(playerId)
                );
            }

            if (startConfig == null)
            {
                throw new InvalidOperationException(
                    "PlayerStartConfigSO is not assigned."
                );
            }

            string now = DateTime.UtcNow.ToString("O");

            PlayerData data = new PlayerData();

            data.Version = 1;

            data.Account.PlayerId = playerId;
            data.Account.CreatedAtUtc = now;
            data.Account.LastSaveAtUtc = now;

            data.Economy.Money = startConfig.StartingMoney;
            data.Economy.Reputation = startConfig.StartingReputation;

            data.Factory.FactoryLevel = 1;
            data.Factory.WarehouseLevel = 1;
            data.Factory.ProductionLevel = 1;
            data.Factory.AssemblyLevel = 1;
            data.Factory.ResearchLevel = 1;

            data.Production.LastProcessedAtUtc = now;
            data.Research.LastProcessedAtUtc = now;

            foreach (PlayerStartConfigSO.StartingMaterial material
                     in startConfig.Materials)
            {
                if (string.IsNullOrEmpty(material.MaterialId))
                {
                    Debug.LogWarning(
                        "PlayerStartConfig contains a material with an empty ID."
                    );

                    continue;
                }

                if (material.Quantity <= 0)
                {
                    continue;
                }

                data.Warehouse.Items.Add(
                    new PlayerWarehouseItemData
                    {
                        ItemId = material.MaterialId,
                        Quantity = material.Quantity
                    }
                );
            }

            CurrentData = data;

            return data;
        }

        public void SetData(PlayerData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            EnsureSupportedData(data);

            CurrentData = data;
        }

        public void ApplyToManagers(GameManager gameManager)
        {
            if (!HasData)
            {
                throw new InvalidOperationException(
                    "PlayerData has not been loaded."
                );
            }

            ValidateManagers(gameManager);

            PlayerData data = CurrentData;

            gameManager.Economy.SetMoney(
                data.Economy.Money
            );

            gameManager.Economy.SetReputation(
                data.Economy.Reputation
            );

            gameManager.Factory.LoadFactoryState(
                data.Factory.FactoryLevel,
                data.Factory.WarehouseLevel,
                data.Factory.ProductionLevel,
                data.Factory.AssemblyLevel,
                data.Factory.ResearchLevel
            );

            gameManager.Warehouse.Initialize();

            foreach (PlayerWarehouseItemData item
                     in data.Warehouse.Items)
            {
                if (item == null ||
                    string.IsNullOrWhiteSpace(item.ItemId) ||
                    item.Quantity <= 0)
                {
                    continue;
                }

                gameManager.Warehouse.SetQuantity(
                    item.ItemId,
                    item.Quantity
                );
            }

            gameManager.License.LoadUnlockedLicenses(
                data.Licenses.UnlockedLicenseIds
            );
        }

        public void CaptureFromManagers(GameManager gameManager)
        {
            if (!HasData)
            {
                throw new InvalidOperationException(
                    "PlayerData has not been loaded."
                );
            }

            ValidateManagers(gameManager);

            PlayerData data = CurrentData;

            data.Economy.Money =
                gameManager.Economy.Money;

            data.Economy.Reputation =
                gameManager.Economy.Reputation;

            data.Factory.FactoryLevel =
                gameManager.Factory.Level;

            data.Factory.WarehouseLevel =
                gameManager.Factory.GetLevel(
                    FactoryZoneType.Warehouse
                );

            data.Factory.ProductionLevel =
                gameManager.Factory.GetLevel(
                    FactoryZoneType.Production
                );

            data.Factory.AssemblyLevel =
                gameManager.Factory.GetLevel(
                    FactoryZoneType.Assembly
                );

            data.Factory.ResearchLevel =
                gameManager.Factory.GetLevel(
                    FactoryZoneType.Research
                );

            data.Warehouse.Items.Clear();

            foreach (KeyValuePair<string, WarehouseItem> item
                     in gameManager.Warehouse.GetItems())
            {
                if (string.IsNullOrWhiteSpace(item.Key) ||
                    item.Value == null ||
                    item.Value.Quantity <= 0)
                {
                    continue;
                }

                data.Warehouse.Items.Add(
                    new PlayerWarehouseItemData
                    {
                        ItemId = item.Key,
                        Quantity = item.Value.Quantity
                    }
                );
            }

            data.Licenses.UnlockedLicenseIds.Clear();

            foreach (string licenseId
                     in gameManager.License.GetUnlockedLicenses())
            {
                if (string.IsNullOrWhiteSpace(licenseId))
                {
                    continue;
                }

                data.Licenses.UnlockedLicenseIds.Add(
                    licenseId
                );
            }

            data.Account.LastSaveAtUtc =
                DateTime.UtcNow.ToString("O");
        }

        public void Clear()
        {
            CurrentData = null;
        }

        private static void ValidateManagers(
            GameManager gameManager)
        {
            if (gameManager == null)
            {
                throw new ArgumentNullException(
                    nameof(gameManager)
                );
            }

            if (gameManager.Economy == null ||
                gameManager.Factory == null ||
                gameManager.Warehouse == null ||
                gameManager.License == null)
            {
                throw new InvalidOperationException(
                    "Required managers are not initialized."
                );
            }
        }

        private static void EnsureSupportedData(
            PlayerData data)
        {
            if (data.Account == null)
            {
                data.Account = new PlayerAccountData();
            }

            if (data.Economy == null)
            {
                data.Economy = new PlayerEconomyData();
            }

            if (data.Factory == null)
            {
                data.Factory = new PlayerFactoryData();
            }

            if (data.Warehouse == null)
            {
                data.Warehouse = new PlayerWarehouseData();
            }

            if (data.Licenses == null)
            {
                data.Licenses = new PlayerLicensesData();
            }

            if (data.Warehouse.Items == null)
            {
                data.Warehouse.Items =
                    new List<PlayerWarehouseItemData>();
            }

            if (data.Licenses.UnlockedLicenseIds == null)
            {
                data.Licenses.UnlockedLicenseIds =
                    new List<string>();
            }
        }
    }
}