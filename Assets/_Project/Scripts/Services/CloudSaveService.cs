using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using SkyOfFreedom.Data;
using UnityEngine;

using UnityCloudSaveService =
    Unity.Services.CloudSave.CloudSaveService;

using UnityPlayerLoadOptions =
    Unity.Services.CloudSave.Models.Data.Player.LoadOptions;

using UnityPlayerSaveOptions =
    Unity.Services.CloudSave.Models.Data.Player.SaveOptions;

namespace SkyOfFreedom.Services
{
    public class CloudSaveService
    {
        private const string PlayerDataKey = "player_data";

        public async Task SavePlayerDataAsync(PlayerData playerData)
        {
            if (playerData == null)
            {
                throw new ArgumentNullException(nameof(playerData));
            }

            string json = JsonUtility.ToJson(playerData);

            Dictionary<string, object> data =
                new Dictionary<string, object>
                {
                    { PlayerDataKey, json }
                };

            await UnityCloudSaveService.Instance.Data.Player
                .SaveAsync(
                    data,
                    new UnityPlayerSaveOptions()
                );
        }

        public async Task<PlayerData> LoadPlayerDataAsync()
        {
            HashSet<string> keys = new HashSet<string>
            {
                PlayerDataKey
            };

            var result = await UnityCloudSaveService.Instance.Data.Player
                .LoadAsync(
                    keys,
                    new UnityPlayerLoadOptions()
                );

            if (!result.TryGetValue(
                    PlayerDataKey,
                    out var savedItem
                ))
            {
                return null;
            }

            string json = savedItem.Value.GetAs<string>();

            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            PlayerData playerData =
                JsonUtility.FromJson<PlayerData>(json);

            if (playerData == null)
            {
                throw new InvalidOperationException(
                    "Cloud Save returned invalid PlayerData."
                );
            }

            return playerData;
        }
    }
}