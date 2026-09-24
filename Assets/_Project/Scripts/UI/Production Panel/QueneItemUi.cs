using SkyOfFreedom.Production;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkyOfFreedom.UI
{
    public class QueueItemUI : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private TMP_Text quantityText;
        [SerializeField] private Slider progress;
        [SerializeField] private CardTierVisual visual;
        [SerializeField] private TMP_Text tierText;
        [SerializeField] private Button removeButton;
        [SerializeField] private Button speedUpButton;

        [Header("States")]
        [SerializeField] private GameObject taskRoot;
        [SerializeField] private GameObject lockedRoot;
        [SerializeField] private TMP_Text lockedText;
        [SerializeField] private GameObject emptyRoot;
        private ProductionZone productionZone;
        private ProductionTask task;
        private ProductionTask lastCompletedEffect;

        private void NotifyCompletion()
        {
            if (task == null || !task.IsCompleted || lastCompletedEffect == task) return;
            lastCompletedEffect = task;
            var game = SkyOfFreedom.Managers.GameManager.Instance;
            if (game == null || !game.IsGameReady || game.IsAccountTransition ||
                !UIUnlockFeedback.Visible(transform as RectTransform)) return;
            ButtonFeedbackUI.Success(this);
            UIFloatingFeedback.Show(transform as RectTransform, nameText, "✓", new Color(0.4f, 1f, 0.55f));
        }

        public void Setup(ProductionTask productionTask, ProductionZone zone)
        {
            if (task != productionTask) NotifyCompletion();
            task = productionTask;
            productionZone = zone;
            SetState(true, false, false);
            gameObject.SetActive(true);

            taskRoot.SetActive(true);
            lockedRoot.SetActive(false);
            emptyRoot.SetActive(false);

            removeButton.onClick.RemoveAllListeners();
            removeButton.onClick.AddListener(OnRemoveClicked);

            speedUpButton.onClick.RemoveAllListeners();
            speedUpButton.onClick.AddListener(OnSpeedUpClicked);

            visual.SetTier(task.Target.Tier);

            if (tierText != null)
                tierText.text = $"T{task.Target.Tier}";

            icon.sprite = task.Target.Icon;
            nameText.text = task.Target.Name;
            descriptionText.text = task.Target.Description;

            UpdateUI();
        }
        private void SetState(bool task, bool empty, bool locked)
        {
            taskRoot.SetActive(task);
            emptyRoot.SetActive(empty);
            lockedRoot.SetActive(locked);
        }
        public void ShowEmpty()
        {
            NotifyCompletion();
            task = null;
            productionZone = null;

            SetState(false, true, false);

            gameObject.SetActive(true);
        }
        public void ShowLocked(int requiredFactoryLevel)
        {
            task = null;
            productionZone = null;

            SetState(false, false, true);

            lockedText.text = $"Upgrade Factory to Lv.{requiredFactoryLevel}";
            gameObject.SetActive(true);
        }
        private void Update()
        {
            NotifyCompletion();
            if (task == null)
                return;

            UpdateUI();
        }
        private void OnSpeedUpClicked()
        {
            if (task == null || productionZone == null)
                return;

            productionZone.SpeedUpTask(task);
        }
        private void OnRemoveClicked()
        {
            if (task == null || productionZone == null)
                return;

            productionZone.CancelTask(task);
        }
        private void UpdateUI()
        {
            UIProgressMotion.Set(progress, task.CurrentItemProgress, task,
                task.State == ProductionState.Working);
            bool waitingForStorage = task.State == ProductionState.WaitingForStorage;
            progress.gameObject.SetActive(task.State == ProductionState.Working || waitingForStorage);
            timeText.gameObject.SetActive(true);
            speedUpButton.interactable = task.State == ProductionState.Working;
            quantityText.text = $"x{task.RemainingQuantity}";
            if (waitingForStorage)
            {
                timeText.text = "Warehouse full";
                return;
            }
            if (task.State == ProductionState.Paused)
            {
                timeText.text = "Paused";
                return;
            }
            if (task.State == ProductionState.Queued)
            {
                UIProgressMotion.Set(progress, 0f, task, false);
                timeText.text = "Waiting...";
                return;
            }
            float seconds = task.Target.ProductionTime * (1f - task.CurrentItemProgress);

            if (seconds < 0f)
                seconds = 0f;
      
            timeText.text = FormatTime(seconds);
        }

        private string FormatTime(float seconds)
        {
            int totalSeconds = Mathf.CeilToInt(seconds);

            int minutes = totalSeconds / 60;
            int secs = totalSeconds % 60;

            return $"{minutes:00}:{secs:00}";
        }
    }
}
