using System.Collections.Generic;
using System.Linq;
using SkyOfFreedom.Managers;
using SkyOfFreedom.Production;
using UnityEngine;

namespace SkyOfFreedom.UI
{
    public class ProductionZoneUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ProductionZone zone;
        [SerializeField] private QueueItemUI[] queueSlots;

        private void OnEnable()
        {
            if (zone != null)
            {
                zone.QueueChanged += RefreshQueue;
            }

            if (GameManager.Instance != null &&
                GameManager.Instance.Factory != null)
            {
                GameManager.Instance.Factory.OnFactoryLevelChanged +=
                    OnFactoryLevelChanged;
            }
        }

        private void OnDisable()
        {
            if (zone != null)
            {
                zone.QueueChanged -= RefreshQueue;
            }

            if (GameManager.Instance != null &&
                GameManager.Instance.Factory != null)
            {
                GameManager.Instance.Factory.OnFactoryLevelChanged -=
                    OnFactoryLevelChanged;
            }
        }

        private void Start()
        {
            RefreshQueue(zone);
        }

        private void OnFactoryLevelChanged(int level)
        {
            RefreshQueue(zone);
        }

        private void RefreshQueue(ProductionZone productionZone)
        {
            if (productionZone == null)
            {
                return;
            }

            if (queueSlots == null || queueSlots.Length == 0)
            {
                return;
            }

            List<ProductionTask> tasks =
                productionZone.Tasks.ToList();

            /*
             * QueueCapacity belongs to the actual ProductionZone.
             * It determines how many queue slots are available
             * in this specific zone.
             *
             * Factory.GetUnlockedQueueSlots() is NOT used here.
             */
            int availableSlots = Mathf.Min(
                productionZone.QueueCapacity,
                queueSlots.Length);

            for (int i = 0; i < queueSlots.Length; i++)
            {
                QueueItemUI slot = queueSlots[i];

                if (slot == null)
                {
                    continue;
                }

                /*
                 * Slots inside the zone capacity are either:
                 * - occupied by a task
                 * - empty and available
                 */
                if (i < availableSlots)
                {
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

                    continue;
                }

                /*
                 * Slots beyond the zone capacity remain visible
                 * but are locked.
                 */
                int requiredLevel = 0;

                if (GameManager.Instance != null &&
                    GameManager.Instance.Factory != null)
                {
                    requiredLevel =
                        GameManager.Instance.Factory
                            .GetRequiredFactoryLevelForQueue(i);
                }

                slot.ShowLocked(requiredLevel);
            }
        }
    }
}