using System;

using SkyOfFreedom.Data;
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
                throw new ArgumentException("Player ID cannot be empty.", nameof(playerId));
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

            CurrentData = data;
        }

        public void Clear()
        {
            CurrentData = null;
        }
    }
}