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

        public void Setup(ProductionTask productionTask, ProductionZone zone)
        {

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
            progress.value = task.CurrentItemProgress;
            progress.gameObject.SetActive(task.State == ProductionState.Working);
            timeText.gameObject.SetActive(task.State == ProductionState.Working);
            quantityText.text = $"x{task.RemainingQuantity}";
            if (task.State == ProductionState.Queued)
            {
                progress.value = 0f;
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