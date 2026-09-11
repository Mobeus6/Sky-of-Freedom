using System;
using System.Threading.Tasks;
using Unity.Services.Core;

using UnityAuthenticationService =
    Unity.Services.Authentication.AuthenticationService;

namespace SkyOfFreedom.Services
{
    public class AuthenticationService
    {
        public bool IsSignedIn =>
            UnityAuthenticationService.Instance.IsSignedIn;

        public string PlayerId =>
            UnityAuthenticationService.Instance.PlayerId;

        public bool IsGoogleLinked =>
            IsSignedIn &&
            !string.IsNullOrEmpty(
                UnityAuthenticationService.Instance
                    .PlayerInfo?.GetGoogleId()
            );

        public async Task InitializeAsync()
        {
            await UnityServices.InitializeAsync();
        }

        public async Task SignInAsGuestAsync()
        {
            if (!IsSignedIn)
            {
                await UnityAuthenticationService.Instance
                    .SignInAnonymouslyAsync();
            }

            await RefreshPlayerInfoAsync();
        }

        public async Task RefreshPlayerInfoAsync()
        {
            if (!IsSignedIn)
            {
                throw new InvalidOperationException(
                    "A signed-in player is required."
                );
            }

            string originalPlayerId = PlayerId;

            await UnityAuthenticationService.Instance
                .GetPlayerInfoAsync();

            if (!IsSignedIn || PlayerId != originalPlayerId)
            {
                throw new InvalidOperationException(
                    "The signed-in player changed."
                );
            }
        }

        public async Task LinkGoogleAccountAsync(string idToken)
        {
            if (!IsSignedIn)
            {
                throw new InvalidOperationException(
                    "A signed-in player is required."
                );
            }

            if (string.IsNullOrWhiteSpace(idToken))
            {
                throw new ArgumentException(
                    "Google ID token is missing.",
                    nameof(idToken)
                );
            }

            if (IsGoogleLinked)
            {
                throw new InvalidOperationException(
                    "Google is already linked."
                );
            }

            string originalPlayerId = PlayerId;

            // Never force-link or switch accounts on a conflict.
            await UnityAuthenticationService.Instance
                .LinkWithGoogleAsync(idToken);

            if (!IsSignedIn || PlayerId != originalPlayerId)
            {
                throw new InvalidOperationException(
                    "The signed-in player changed."
                );
            }
        }
    }
}