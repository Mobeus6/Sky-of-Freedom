using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityCloudCodeService =
    Unity.Services.CloudCode.CloudCodeService;

using UnityAuthenticationService =
    Unity.Services.Authentication.AuthenticationService;

namespace SkyOfFreedom.Services
{
    public class PublicIdService
    {
        private const string EndpointName = "GetOrCreatePublicId";

        public async Task<string> GetOrCreatePublicIdAsync()
        {
            if (!UnityAuthenticationService.Instance.IsSignedIn)
            {
                throw new InvalidOperationException(
                    "Authentication is required before requesting PublicId."
                );
            }

            string playerId =
                UnityAuthenticationService.Instance.PlayerId;

            Dictionary<string, string> response =
                await UnityCloudCodeService.Instance
                    .CallEndpointAsync<Dictionary<string, string>>(
                        EndpointName,
                        new Dictionary<string, object>()
                    );

            if (!UnityAuthenticationService.Instance.IsSignedIn ||
                UnityAuthenticationService.Instance.PlayerId != playerId)
            {
                throw new InvalidOperationException(
                    "The player changed while requesting PublicId."
                );
            }

            if (response == null ||
                !response.TryGetValue("publicId", out string publicId) ||
                !IsValidPublicId(publicId))
            {
                throw new InvalidOperationException(
                    "Cloud Code returned an invalid PublicId."
                );
            }

            return publicId;
        }

        private static bool IsValidPublicId(string value)
        {
            if (string.IsNullOrEmpty(value) ||
                value.Length < 8 ||
                !value.StartsWith("ID", StringComparison.Ordinal))
            {
                return false;
            }

            for (int i = 2; i < value.Length; i++)
            {
                if (value[i] < '0' || value[i] > '9')
                {
                    return false;
                }
            }

            return true;
        }
    }
}