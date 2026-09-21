using TMPro;
using UnityEngine;
using SkyOfFreedom.Managers;
using SkyOfFreedom.Utilities;

namespace SkyOfFreedom.UI
{
    public class HUDResourcesUI : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TMP_Text moneyText;
        [SerializeField] private TMP_Text reputationText;
        [SerializeField] private TMP_Text saveStatusText;
        [SerializeField] private TMP_Text playerIdText;

        private EconomyManager economyManager;

        private void Awake()
        {
            if (playerIdText == null)
            {
                foreach (GameObject root in gameObject.scene.GetRootGameObjects())
                {
                    if (root.name != "GameCanvas") continue;
                    Transform label = root.transform.Find("Top Bar/Resources Panel/Factory/UserName");
                    if (label != null) playerIdText = label.GetComponent<TMP_Text>();
                }
            }
            RefreshPlayerId();
            economyManager = FindAnyObjectByType<EconomyManager>();

            if (economyManager == null)
            {
                Debug.LogError("EconomyManager not found!");
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (economyManager == null)
                return;

            economyManager.OnMoneyChanged += UpdateMoney;
            economyManager.OnReputationChanged += UpdateReputation;

            UpdateMoney(economyManager.Money);
            UpdateReputation(economyManager.Reputation);
        }

        private void OnDisable()
        {
            if (economyManager == null)
                return;

            economyManager.OnMoneyChanged -= UpdateMoney;
            economyManager.OnReputationChanged -= UpdateReputation;
        }

        private void UpdateMoney(long value)
        {
            moneyText.text = NumberFormatter.Format(value);
        }

        private void Update()
        {
            RefreshPlayerId();
            if (saveStatusText == null)
                return;

            GameManager game = GameManager.Instance;
            string status = game != null && game.IsGameReady
                ? game.SaveStatusText
                : string.Empty;
            if (saveStatusText.text != status)
                saveStatusText.text = status;
            // This label must not intercept taps on the HUD.
            saveStatusText.raycastTarget = false;
        }

        private void RefreshPlayerId()
        {
            if (playerIdText == null) return;
            GameManager game = GameManager.Instance;
            string id = game != null && game.IsGameReady ? game.PublicId : null;
            string label = string.IsNullOrEmpty(id) ? "ID: —" : id;
            if (playerIdText.text != label) playerIdText.text = label;
        }

        private void UpdateReputation(int value)
        {
            reputationText.text = value.ToString();
        }
    }
}
