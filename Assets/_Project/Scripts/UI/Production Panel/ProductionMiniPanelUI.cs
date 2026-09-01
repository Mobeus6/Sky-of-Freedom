using System.Collections.Generic;
using System.Linq;
using SkyOfFreedom.Factory;
using SkyOfFreedom.Gameplay.Factory;
using SkyOfFreedom.Managers;
using SkyOfFreedom.Production;
using UnityEngine;
using UnityEngine.UI;

namespace SkyOfFreedom.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class ProductionMiniPanelUI : MonoBehaviour
    {
        [Header("Zone References")]
        [SerializeField]
        private FactoryZoneInteraction productionZoneInteraction;

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

        private ProductionZone productionZone;

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
                    OpenProductionPanel);
            }
        }

        private void OnEnable()
        {
            ResolveProductionZone();

            if (productionZoneInteraction != null)
            {
                productionZoneInteraction.ZoneSelected +=
                    OnZoneSelected;

                productionZoneInteraction.ZoneDeselected +=
                    OnZoneDeselected;
            }

            if (productionZone != null)
            {
                productionZone.QueueChanged +=
                    RefreshQueue;
            }

            RefreshQueue(productionZone);

            if (FactoryZoneInteraction.SelectedZone ==
                productionZoneInteraction)
            {
                Open();
            }
        }

        private void OnDisable()
        {
            if (productionZoneInteraction != null)
            {
                productionZoneInteraction.ZoneSelected -=
                    OnZoneSelected;

                productionZoneInteraction.ZoneDeselected -=
                    OnZoneDeselected;
            }

            if (productionZone != null)
            {
                productionZone.QueueChanged -=
                    RefreshQueue;
            }

            productionZone = null;
        }

        private void ResolveProductionZone()
        {
            productionZone = null;

            if (GameManager.Instance == null)
            {
                return;
            }

            ProductionManager productionManager =
                GameManager.Instance.Production;

            if (productionManager == null)
            {
                return;
            }

            IReadOnlyList<ProductionZone> zones =
                productionManager.Zones;

            for (int i = 0; i < zones.Count; i++)
            {
                ProductionZone zone = zones[i];

                if (zone == null)
                {
                    continue;
                }

                if (!zone.isActiveAndEnabled)
                {
                    continue;
                }

                if (zone.ZoneType != FactoryZoneType.Production)
                {
                    continue;
                }

                productionZone = zone;
                return;
            }
        }

        private void OnZoneSelected(
            FactoryZoneInteraction zone)
        {
            if (zone != productionZoneInteraction)
            {
                return;
            }

            ResolveProductionZone();

            if (productionZone != null)
            {
                productionZone.QueueChanged -=
                    RefreshQueue;

                productionZone.QueueChanged +=
                    RefreshQueue;
            }

            RefreshQueue(productionZone);
            Open();
        }

        private void OnZoneDeselected(
            FactoryZoneInteraction zone)
        {
            if (zone != productionZoneInteraction)
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

            if (queueSlots == null)
            {
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
                        zone);
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

        public void OpenProductionPanel()
        {
            if (menuManager == null)
            {
                Debug.LogError(
                    "ProductionMiniPanelUI: MenuManager is not assigned.",
                    this);

                return;
            }

            if (productionMenuButton == null)
            {
                Debug.LogError(
                    "ProductionMiniPanelUI: Production MenuButton is not assigned.",
                    this);

                return;
            }

            if (productionSubPanel == null)
            {
                Debug.LogError(
                    "ProductionMiniPanelUI: Production Sub Panel is not assigned.",
                    this);

                return;
            }

            if (assemblySubPanel == null)
            {
                Debug.LogError(
                    "ProductionMiniPanelUI: Assembly Sub Panel is not assigned.",
                    this);

                return;
            }

            Hide();

            menuManager.Toggle(
                productionMenuButton);

            productionSubPanel.Show();
            assemblySubPanel.Hide();

            if (productionZoneHighlight != null)
            {
                productionZoneHighlight.gameObject.SetActive(true);
            }

            if (assemblyZoneHighlight != null)
            {
                assemblyZoneHighlight.gameObject.SetActive(false);
            }
        }
    }
}