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

        public async Task InitializeAsync()
        {
            await UnityServices.InitializeAsync();
        }

        public async Task SignInAsGuestAsync()
        {
            if (IsSignedIn)
            {
                return;
            }

            await UnityAuthenticationService.Instance
                .SignInAnonymouslyAsync();
        }
    }
}