using System.Collections.Generic;
using System.Linq;
using SkyOfFreedom.Gameplay.Factory;
using SkyOfFreedom.Managers;
using SkyOfFreedom.Production;
using UnityEngine;
using UnityEngine.UI;

namespace SkyOfFreedom.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class AssemblyMiniPanelUI : MonoBehaviour
    {
        [Header("Zone References")]
        [SerializeField]
        private FactoryZoneInteraction assemblyZoneInteraction;

        [SerializeField]
        private ProductionZone assemblyZone;

        [Header("UI")]
        [SerializeField]
        private CanvasGroup canvasGroup;

        [SerializeField]
        private QueueItemUI[] queueSlots;

        [Header("Open Production")]
        [SerializeField]
        private Button openProductionButton;

        [SerializeField]
        private MenuManager menuManager;

        [SerializeField]
        private MenuButton productionMenuButton;

        [Header("Production Sub Panels")]
        [SerializeField]
        private UIPanel productionSubPanel;

        [SerializeField]
        private UIPanel assemblySubPanel;

        [Header("Sub Panel Highlights")]
        [SerializeField]
        private Image productionZoneHighlight;

        [SerializeField]
        private Image assemblyZoneHighlight;

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            Hide();

            if (openProductionButton != null)
            {
                openProductionButton.onClick.RemoveAllListeners();
                openProductionButton.onClick.AddListener(
                    OpenAssemblyPanel);
            }
        }

        private void OnEnable()
        {
            if (assemblyZoneInteraction != null)
            {
                assemblyZoneInteraction.ZoneSelected +=
                    OnZoneSelected;

                assemblyZoneInteraction.ZoneDeselected +=
                    OnZoneDeselected;
            }

            if (assemblyZone != null)
            {
                assemblyZone.QueueChanged +=
                    RefreshQueue;
            }

            RefreshQueue(assemblyZone);

            if (FactoryZoneInteraction.SelectedZone ==
                assemblyZoneInteraction)
            {
                Open();
            }
        }

        private void OnDisable()
        {
            if (assemblyZoneInteraction != null)
            {
                assemblyZoneInteraction.ZoneSelected -=
                    OnZoneSelected;

                assemblyZoneInteraction.ZoneDeselected -=
                    OnZoneDeselected;
            }

            if (assemblyZone != null)
            {
                assemblyZone.QueueChanged -=
                    RefreshQueue;
            }
        }

        private void OnZoneSelected(
            FactoryZoneInteraction zone)
        {
            if (zone != assemblyZoneInteraction)
            {
                return;
            }

            RefreshQueue(assemblyZone);
            Open();
        }

        private void OnZoneDeselected(
            FactoryZoneInteraction zone)
        {
            if (zone != assemblyZoneInteraction)
            {
                return;
            }

            Hide();
        }

        private void RefreshQueue(
            ProductionZone zone)
        {
            if (zone == null)
            {
                HideAllSlots();
                return;
            }

            if (GameManager.Instance == null ||
                GameManager.Instance.Factory == null)
            {
                HideAllSlots();
                return;
            }

            List<ProductionTask> tasks =
                zone.Tasks.ToList();

            int factoryUnlockedSlots =
                GameManager.Instance.Factory
                    .GetUnlockedQueueSlots();

            int visibleSlots = Mathf.Min(
                factoryUnlockedSlots,
                zone.QueueCapacity,
                queueSlots.Length);

            for (int i = 0; i < queueSlots.Length; i++)
            {
                QueueItemUI slot = queueSlots[i];

                if (slot == null)
                {
                    continue;
                }

                if (i >= visibleSlots)
                {
                    slot.gameObject.SetActive(false);
                    continue;
                }

                slot.gameObject.SetActive(true);

                if (i < tasks.Count)
                {
                    slot.Setup(
                        tasks[i],
                        assemblyZone);
                }
                else
                {
                    slot.ShowEmpty();
                }
            }
        }

        private void HideAllSlots()
        {
            if (queueSlots == null)
            {
                return;
            }

            for (int i = 0; i < queueSlots.Length; i++)
            {
                if (queueSlots[i] != null)
                {
                    queueSlots[i].gameObject.SetActive(false);
                }
            }
        }

        public void Open()
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        public void Hide()
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        public void OpenAssemblyPanel()
        {
            if (menuManager == null)
            {
                Debug.LogError(
                    "AssemblyMiniPanelUI: MenuManager is not assigned.",
                    this);

                return;
            }

            if (productionMenuButton == null)
            {
                Debug.LogError(
                    "AssemblyMiniPanelUI: Production MenuButton is not assigned.",
                    this);

                return;
            }

            if (productionSubPanel == null)
            {
                Debug.LogError(
                    "AssemblyMiniPanelUI: Production Sub Panel is not assigned.",
                    this);

                return;
            }

            if (assemblySubPanel == null)
            {
                Debug.LogError(
                    "AssemblyMiniPanelUI: Assembly Sub Panel is not assigned.",
                    this);

                return;
            }

            Hide();

            menuManager.Toggle(
                productionMenuButton);

            productionSubPanel.Hide();
            assemblySubPanel.Show();

            if (productionZoneHighlight != null)
            {
                productionZoneHighlight.gameObject.SetActive(false);
            }

            if (assemblyZoneHighlight != null)
            {
                assemblyZoneHighlight.gameObject.SetActive(true);
            }
        }
    }
}