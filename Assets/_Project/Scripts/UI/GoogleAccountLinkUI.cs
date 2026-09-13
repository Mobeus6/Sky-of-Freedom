using System;
using System.Threading.Tasks;
using SkyOfFreedom.Managers;
using SkyOfFreedom.Services;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UI;

using GameAuthenticationService = SkyOfFreedom.Services.AuthenticationService;

namespace SkyOfFreedom.UI
{
    public class GoogleAccountLinkUI : MonoBehaviour
    {
        private const string BridgeClass =
            "com.iliadstudio.skyoffreedom.auth.GoogleSignInBridge";

        [SerializeField] private Button googleSignInButton;
        [SerializeField] private Button playButton;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private GameObject guestOptionsRoot;
        [SerializeField] private Button tapToPlayButton;
        [Header("Account confirmation")]
        [SerializeField] private Button signOutButton;
        [SerializeField] private GameObject signOutOverlay;
        [SerializeField] private TMP_Text confirmationTitle;
        [SerializeField] private TMP_Text confirmationStatusText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private TMP_Text confirmButtonText;
        [SerializeField] private Button cancelButton;

        [SerializeField]
        private string webClientId =
            "715019449943-dbu03rb254ppiocg4jiijfsuhi74b9al.apps.googleusercontent.com";

        private readonly GameAuthenticationService authentication =
            new GameAuthenticationService();

        private bool busy;
        private bool destroyed;
        private bool initialized;
        private string requestId;
        private string originalPlayerId;
        private bool confirmationOpen;

        private void Awake()
        {
            if (googleSignInButton == null || playButton == null || statusText == null ||
                guestOptionsRoot == null || tapToPlayButton == null ||
                signOutButton == null || signOutOverlay == null ||
                confirmationTitle == null ||
                confirmationStatusText == null || confirmButton == null || confirmButtonText == null ||
                cancelButton == null)
            {
                Debug.LogError("GoogleAccountLinkUI: assign all Inspector fields.", this);
                enabled = false;
                return;
            }

            if (transform.IsChildOf(guestOptionsRoot.transform) ||
                transform.IsChildOf(tapToPlayButton.transform) ||
                transform.IsChildOf(signOutOverlay.transform))
            {
                Debug.LogError("GoogleAccountLinkUI must be outside the panels it hides.", this);
                enabled = false;
                return;
            }

            tapToPlayButton.gameObject.SetActive(false);

            // Attach this component to a dedicated empty object.
            gameObject.name = "GoogleAccountLinkReceiver_" + Guid.NewGuid().ToString("N");
            googleSignInButton.onClick.AddListener(BeginLink);
            signOutButton.onClick.AddListener(OpenSignOut);
            confirmButton.onClick.AddListener(ConfirmAccountChange);
            cancelButton.onClick.AddListener(CancelAccountChange);
            signOutOverlay.SetActive(false);
            initialized = true;
            statusText.text = "";
            RefreshButtons();
        }

        private void Update()
        {
            if (initialized) RefreshButtons();
        }

        private void RefreshButtons()
        {
            bool ready = GameManager.Instance != null &&
                         GameManager.Instance.IsGameReady;
            bool supported = Application.platform == RuntimePlatform.Android;
            // Do not access Authentication.Instance before startup initializes UGS.
            bool linked = ready && authentication.IsGoogleLinked;
            bool canInteract = ready && !busy && !confirmationOpen;

            googleSignInButton.interactable = canInteract && supported && !linked;
            playButton.interactable = canInteract && !linked;

            bool showTapToPlay = ready && linked;
            guestOptionsRoot.SetActive(!showTapToPlay);
            tapToPlayButton.gameObject.SetActive(showTapToPlay);
            tapToPlayButton.interactable = canInteract && linked;
            signOutButton.gameObject.SetActive(linked);
            signOutButton.interactable = canInteract && linked;

            cancelButton.interactable = confirmationOpen && !busy;
            confirmButton.interactable = confirmationOpen && ready && !busy;

            if (!busy && !confirmationOpen && linked)
                statusText.text = "Google account linked";
            else if (!busy && !confirmationOpen && !supported)
                statusText.text = "Google sign-in is available in the Android build.";
        }

        private void OpenSignOut()
        {
            if (busy || confirmationOpen || GameManager.Instance == null ||
                !GameManager.Instance.IsGameReady || !authentication.IsGoogleLinked)
                return;

            originalPlayerId = authentication.PlayerId;
            ShowConfirmation(
                "Sign out?",
                "");
        }

        private void ShowConfirmation(string title, string message)
        {
            busy = false;
            confirmationOpen = true;
            confirmationTitle.text = title;
            confirmButtonText.text = "Sign Out";
            confirmationStatusText.text = message;
            signOutOverlay.SetActive(true);
            RefreshButtons();
        }

        private void CancelAccountChange()
        {
            if (busy) return;
            confirmationOpen = false;
            signOutOverlay.SetActive(false);
            RefreshButtons();
        }

        private async void ConfirmAccountChange()
        {
            GameManager manager = GameManager.Instance;
            if (!confirmationOpen || busy || manager == null ||
                !manager.IsGameReady)
                return;

            if (!authentication.IsSignedIn ||
                authentication.PlayerId != originalPlayerId)
            {
                confirmationStatusText.text =
                    "Account changed. Restart the game.";
                return;
            }

            busy = true;
            confirmationStatusText.text = "Saving…";
            RefreshButtons();

            try
            {
                await manager.SignOutAccountAsync();

                // The scene reload creates fresh UI and managers for the new player.
                if (!destroyed) confirmationStatusText.text = "Loading…";
            }
            catch (Exception exception)
            {
                if (destroyed) return;
                busy = false;
                bool recovered = GameManager.Instance != null &&
                                 GameManager.Instance.IsGameReady;
                string code = exception is RequestFailedException requestException
                    ? " Code: " + requestException.ErrorCode + "."
                    : "";
                confirmationStatusText.text = recovered
                    ? "Cancelled. Please try again." + code
                    : "Restart the game." + code;
                Debug.LogWarning("Account change failed." + code, this);
                RefreshButtons();
            }
        }

        private async Task SignInToExistingAccountAsync(string idToken)
        {
            // Choosing Google is the user's sign-in action; no second confirmation.
            confirmationOpen = false;
            signOutOverlay.SetActive(false);
            busy = true;
            statusText.text = "Signing in…";
            RefreshButtons();

            try
            {
                GameManager manager = GameManager.Instance;
                if (manager == null || !manager.IsGameReady ||
                    !authentication.IsSignedIn ||
                    authentication.PlayerId != originalPlayerId)
                    throw new InvalidOperationException("The active player changed.");

                // This existing path saves the guest first and reloads fresh managers.
                await manager.RestoreGoogleAccountAsync(idToken);
                if (!destroyed) statusText.text = "Loading…";
            }
            catch (Exception exception)
            {
                if (destroyed) return;
                bool recovered = GameManager.Instance != null &&
                                 GameManager.Instance.IsGameReady;
                string code = exception is RequestFailedException requestException
                    ? " Code: " + requestException.ErrorCode + "."
                    : "";
                Finish(recovered
                    ? "Sign-in failed. Please try again." + code
                    : "Unable to finish sign-in. Restart the game." + code);
                Debug.LogWarning("Google account sign-in failed." + code, this);
            }
        }

        private void BeginLink()
        {
            if (busy || confirmationOpen || GameManager.Instance == null ||
                !GameManager.Instance.IsGameReady ||
                !authentication.IsSignedIn || authentication.IsGoogleLinked)
                return;

#if UNITY_ANDROID && !UNITY_EDITOR
            if (string.IsNullOrWhiteSpace(webClientId))
            {
                statusText.text = "Google sign-in is not configured.";
                return;
            }

            originalPlayerId = authentication.PlayerId;
            requestId = Guid.NewGuid().ToString("N");
            busy = true;
            statusText.text = "Choose your Google account…";
            RefreshButtons();

            try
            {
                using (AndroidJavaClass bridge = new AndroidJavaClass(BridgeClass))
                {
                    bridge.CallStatic("begin", gameObject.name, requestId, webClientId);
                }
            }
            catch (Exception)
            {
                Finish("Unable to open Google sign-in. Check the Android plugin setup.");
                Debug.LogError("Google sign-in bridge could not be started.", this);
            }
#else
            statusText.text = "Test Google sign-in on an Android device.";
#endif
        }

        [Serializable]
        [Preserve]
        public class GoogleResult
        {
            public string requestId;
            public string status;
            public string token;
        }

        [Preserve]
        public async void OnGoogleCredential(string json)
        {
            if (destroyed || !busy || string.IsNullOrEmpty(requestId)) return;

            GoogleResult result;
            try
            {
                result = JsonUtility.FromJson<GoogleResult>(json);
            }
            catch (Exception)
            {
                Finish("Invalid Google sign-in response. Please try again.");
                return;
            }

            if (result == null || result.requestId != requestId) return;

            // Accept only one callback for this attempt.
            requestId = null;

            if (result.status != "success")
            {
                switch (result.status)
                {
                    case "cancelled":
                        Finish("Sign-in cancelled. Your guest progress is unchanged.");
                        break;
                    case "timeout":
                        Finish("Google sign-in timed out. Please try again.");
                        break;
                    case "no_credential":
                        Finish("No Google account is available. Check Google Play services and try again.");
                        break;
                    default:
                        Finish("Google sign-in failed. Check your connection and Google configuration.");
                        break;
                }
                return;
            }

            if (!authentication.IsSignedIn ||
                authentication.PlayerId != originalPlayerId)
            {
                result.token = null;
                Finish("The active player changed. Restart the game before linking.");
                return;
            }

            try
            {
                statusText.text = "Linking your account…";
                await authentication.LinkGoogleAccountAsync(result.token);
                if (!destroyed) Finish("Google account linked");
            }
            catch (AuthenticationException exception)
            {
                if (!destroyed)
                {
                    if (exception.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
                    {
                        await SignInToExistingAccountAsync(result.token);
                    }
                    else
                    {
                        Finish($"Google link failed. Auth code: {exception.ErrorCode}\nUnable to confirm the link.");
                    }
                    if (exception.ErrorCode != AuthenticationErrorCodes.AccountAlreadyLinked)
                        Debug.LogWarning("Google link authentication error code: " + exception.ErrorCode);
                }
            }
            catch (RequestFailedException exception)
            {
                if (!destroyed)
                {
                    Finish($"Google link failed. Request code: {exception.ErrorCode}");
                    Debug.LogWarning("Google link request error code: " + exception.ErrorCode);
                }
            }
            catch (Exception)
            {
                if (!destroyed)
                    Finish("Unable to link the account. Restart the game and check the account status.");
            }
            finally
            {
                result.token = null;
            }
        }

        private void Finish(string message)
        {
            busy = false;
            requestId = null;
            statusText.text = message;
            playButton.interactable = GameManager.Instance != null &&
                                      GameManager.Instance.IsGameReady;
            RefreshButtons();
        }

        private void OnDestroy()
        {
            destroyed = true;
            if (signOutButton != null) signOutButton.onClick.RemoveListener(OpenSignOut);
            if (confirmButton != null) confirmButton.onClick.RemoveListener(ConfirmAccountChange);
            if (cancelButton != null) cancelButton.onClick.RemoveListener(CancelAccountChange);
            if (googleSignInButton != null)
                googleSignInButton.onClick.RemoveListener(BeginLink);

#if UNITY_ANDROID && !UNITY_EDITOR
            if (!string.IsNullOrEmpty(requestId))
            {
                try
                {
                    using (AndroidJavaClass bridge = new AndroidJavaClass(BridgeClass))
                        bridge.CallStatic("cancel", requestId);
                }
                catch (Exception) { }
            }
#endif
            if (busy && playButton != null)
                playButton.interactable = GameManager.Instance != null &&
                                          GameManager.Instance.IsGameReady;
        }
    }
}
