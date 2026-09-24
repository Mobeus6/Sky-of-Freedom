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
        private long previousMoney;
        private int previousReputation;
        private decimal moneyDelta;
        private long reputationDelta;
        private bool feedbackReady;
        private string feedbackAccount;
        private float nextFeedback;

        private bool CanAnimateResources()
        {
            var game = GameManager.Instance;
            return feedbackReady && game != null && game.IsGameReady &&
                !game.IsAccountTransition && game.PublicId == feedbackAccount;
        }

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
            feedbackReady = false;
            moneyDelta = 0m;
            reputationDelta = 0;
            if (economyManager == null)
                return;

            economyManager.OnMoneyChanged += UpdateMoney;
            economyManager.OnReputationChanged += UpdateReputation;

            UpdateMoney(economyManager.Money);
            UpdateReputation(economyManager.Reputation);
        }

        private void OnDisable()
        {
            feedbackReady = false;
            if (economyManager == null)
                return;

            economyManager.OnMoneyChanged -= UpdateMoney;
            economyManager.OnReputationChanged -= UpdateReputation;
        }

        private void UpdateMoney(long value)
        {
            if (CanAnimateResources()) moneyDelta += (decimal)value - previousMoney;
            previousMoney = value;
            moneyText.text = NumberFormatter.Format(value);
        }

        private void Update()
        {
            UpdateResourceFeedback();
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
            if (CanAnimateResources()) reputationDelta += (long)value - previousReputation;
            previousReputation = value;
            reputationText.text = value.ToString();
        }

        private void UpdateResourceFeedback()
        {
            var game = GameManager.Instance;
            if (game == null || !game.IsGameReady || game.IsAccountTransition || economyManager == null)
            {
                feedbackReady = false;
                moneyDelta = 0m;
                reputationDelta = 0;
                return;
            }
            if (!feedbackReady || feedbackAccount != game.PublicId)
            {
                feedbackAccount = game.PublicId;
                previousMoney = economyManager.Money;
                previousReputation = economyManager.Reputation;
                moneyDelta = 0m;
                reputationDelta = 0;
                feedbackReady = true;
                nextFeedback = Time.unscaledTime + 0.1f;
                return;
            }
            if (Time.unscaledTime < nextFeedback) return;
            nextFeedback = Time.unscaledTime + 0.1f;
            ShowDelta(moneyText, moneyDelta);
            ShowDelta(reputationText, reputationDelta);
            moneyDelta = 0m;
            reputationDelta = 0;
        }

        private static void ShowDelta(TMP_Text label, decimal delta)
        {
            if (label == null || delta == 0m) return;
            UIFloatingFeedback.Show(label.rectTransform, label,
                (delta > 0m ? "+" : "−") + System.Math.Abs(delta).ToString("N0"),
                delta > 0m ? new Color(0.4f, 1f, 0.55f) : new Color(1f, 0.4f, 0.4f));
        }
    }
}
