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
        private string loadedPlayerId;
        private string writeLock;
        public string LastLoadedJson { get; private set; }
        public string ConfirmedFingerprint { get; private set; } = string.Empty;

        public async Task SavePlayerDataAsync(PlayerData playerData)
        {
            if (playerData == null)
            {
                throw new ArgumentNullException(nameof(playerData));
            }

            if (!Unity.Services.Authentication.AuthenticationService.Instance.IsSignedIn ||
                playerData.Account == null ||
                playerData.Account.PlayerId !=
                    Unity.Services.Authentication.AuthenticationService.Instance.PlayerId)
            {
                throw new InvalidOperationException(
                    "Refusing to save data belonging to another player.");
            }

            string json = JsonUtility.ToJson(playerData);
            string playerId = playerData.Account.PlayerId;
            if (loadedPlayerId != playerId)
                throw new InvalidOperationException("Load this player's cloud save before writing it.");

            Dictionary<string, Unity.Services.CloudSave.Models.SaveItem> data =
                new Dictionary<string, Unity.Services.CloudSave.Models.SaveItem>
                {
                    { PlayerDataKey, new Unity.Services.CloudSave.Models.SaveItem(json, writeLock) }
                };

            var locks = await UnityCloudSaveService.Instance.Data.Player
                .SaveAsync(
                    data,
                    new UnityPlayerSaveOptions()
                );
            if (Unity.Services.Authentication.AuthenticationService.Instance.PlayerId != playerId)
                throw new InvalidOperationException("Player changed while saving.");
            writeLock = locks[PlayerDataKey];
            ConfirmedFingerprint = LocalSaveStore.Fingerprint(json);
        }

        public async Task<PlayerData> LoadPlayerDataAsync()
        {
            string playerId = Unity.Services.Authentication.AuthenticationService.Instance.PlayerId;
            HashSet<string> keys = new HashSet<string>
            {
                PlayerDataKey
            };

            var result = await UnityCloudSaveService.Instance.Data.Player
                .LoadAsync(
                    keys,
                    new UnityPlayerLoadOptions()
                );
            if (!Unity.Services.Authentication.AuthenticationService.Instance.IsSignedIn ||
                Unity.Services.Authentication.AuthenticationService.Instance.PlayerId != playerId)
                throw new InvalidOperationException("Player changed while loading.");

            if (!result.TryGetValue(
                    PlayerDataKey,
                    out var savedItem
                ))
            {
                loadedPlayerId = playerId;
                writeLock = null;
                LastLoadedJson = null;
                ConfirmedFingerprint = string.Empty;
                return null;
            }

            string json = savedItem.Value.GetAs<string>();

            if (string.IsNullOrEmpty(json))
            {
                throw new InvalidOperationException("Cloud Save contains empty player data.");
            }

            PlayerData playerData =
                JsonUtility.FromJson<PlayerData>(json);

            if (playerData == null)
            {
                throw new InvalidOperationException(
                    "Cloud Save returned invalid PlayerData."
                );
            }

            loadedPlayerId = playerId;
            writeLock = savedItem.WriteLock;
            if (string.IsNullOrEmpty(writeLock))
                throw new InvalidOperationException("Existing cloud save has no write lock.");
            LastLoadedJson = json;
            ConfirmedFingerprint = LocalSaveStore.Fingerprint(json);

            return playerData;
        }
    }
}
