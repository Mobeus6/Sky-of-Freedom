using System;
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

        private void Awake()
        {
            if (googleSignInButton == null || playButton == null || statusText == null ||
                guestOptionsRoot == null || tapToPlayButton == null)
            {
                Debug.LogError("GoogleAccountLinkUI: assign all Inspector fields.", this);
                enabled = false;
                return;
            }

            if (transform.IsChildOf(guestOptionsRoot.transform) ||
                transform.IsChildOf(tapToPlayButton.transform))
            {
                Debug.LogError("GoogleAccountLinkUI must be outside the panels it hides.", this);
                enabled = false;
                return;
            }

            tapToPlayButton.gameObject.SetActive(false);

            // Attach this component to a dedicated empty object.
            gameObject.name = "GoogleAccountLinkReceiver_" + Guid.NewGuid().ToString("N");
            googleSignInButton.onClick.AddListener(BeginLink);
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
            bool linked = ready && authentication.IsGoogleLinked;

            googleSignInButton.interactable =
                ready && supported && !busy && !linked;

            bool showTapToPlay = ready && linked && !busy;
            if (guestOptionsRoot.activeSelf != !showTapToPlay)
                guestOptionsRoot.SetActive(!showTapToPlay);
            if (tapToPlayButton.gameObject.activeSelf != showTapToPlay)
                tapToPlayButton.gameObject.SetActive(showTapToPlay);
            tapToPlayButton.interactable = showTapToPlay;

            // Only override Play while a link operation is running.
            if (busy) playButton.interactable = false;

            if (!busy && linked)
                statusText.text = "Google account linked";
            else if (!busy && !supported)
                statusText.text = "Google sign-in is available in the Android build.";
        }

        private void BeginLink()
        {
            if (busy || GameManager.Instance == null ||
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
            if (destroyed || !busy) return;

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
                    Finish($"Google link failed. Auth code: {exception.ErrorCode}\n" +
                        (exception.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked
                            ? "This Google account is linked to another player. Your progress has not been switched."
                            : "Unable to confirm the link."));
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
