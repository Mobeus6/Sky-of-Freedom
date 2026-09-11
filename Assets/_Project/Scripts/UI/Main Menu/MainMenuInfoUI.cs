using SkyOfFreedom.Managers;
using TMPro;
using UnityEngine;

namespace SkyOfFreedom.UI
{
    public class MainMenuInfoUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text playerIdText;
        [SerializeField] private TMP_Text versionText;

        private string displayedPublicId;

        private void OnEnable()
        {
            displayedPublicId = null;

            if (versionText != null)
            {
                versionText.text = $"Version {Application.version}";
            }

            RefreshPlayerId();
        }

        private void Update()
        {
            RefreshPlayerId();
        }

        private void RefreshPlayerId()
        {
            if (playerIdText == null)
            {
                return;
            }

            GameManager manager = GameManager.Instance;

            string publicId =
                manager != null && manager.IsGameReady
                    ? manager.PublicId
                    : string.Empty;

            if (displayedPublicId == publicId)
            {
                return;
            }

            displayedPublicId = publicId;

            playerIdText.text = string.IsNullOrEmpty(publicId)
                ? "Player ID: —"
                : $"Player ID: {publicId}";
        }
    }
}