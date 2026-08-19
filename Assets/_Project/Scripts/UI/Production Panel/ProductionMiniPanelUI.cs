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
    public class ProductionMiniPanelUI : MonoBehaviour
    {
        [Header("Zone References")]
        [SerializeField]
        private FactoryZoneInteraction productionZoneInteraction;

        [SerializeField]
        private ProductionZone productionZone;

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
        }

        private void OnZoneSelected(
            FactoryZoneInteraction zone)
        {
            if (zone != productionZoneInteraction)
                return;

            RefreshQueue(productionZone);
            Open();
        }

        private void OnZoneDeselected(
            FactoryZoneInteraction zone)
        {
            if (zone != productionZoneInteraction)
                return;

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
                GameManager.Instance.Factory.GetUnlockedQueueSlots();

            int visibleSlots = Mathf.Min(
                factoryUnlockedSlots,
                zone.QueueCapacity,
                queueSlots.Length);

            for (int i = 0; i < queueSlots.Length; i++)
            {
                QueueItemUI slot = queueSlots[i];

                if (slot == null)
                    continue;

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
                        productionZone);
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
                return;

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
                return;

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        public void Hide()
        {
            if (canvasGroup == null)
                return;

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

            Hide();

            menuManager.Toggle(
                productionMenuButton);
        }
    }
}