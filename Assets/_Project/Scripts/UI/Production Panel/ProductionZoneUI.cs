using System.Collections.Generic;
using System.Linq;
using SkyOfFreedom.Factory;
using SkyOfFreedom.Managers;
using SkyOfFreedom.Production;
using UnityEngine;

namespace SkyOfFreedom.UI
{
    public class ProductionZoneUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private QueueItemUI[] queueSlots;

        private ProductionZone productionZone;

        private void OnEnable()
        {
            ResolveProductionZone();

            if (productionZone != null)
            {
                productionZone.QueueChanged +=
                    RefreshQueue;
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
            if (productionZone != null)
            {
                productionZone.QueueChanged -=
                    RefreshQueue;
            }

            if (GameManager.Instance != null &&
                GameManager.Instance.Factory != null)
            {
                GameManager.Instance.Factory.OnFactoryLevelChanged -=
                    OnFactoryLevelChanged;
            }

            productionZone = null;
        }

        private void Start()
        {
            ResolveProductionZone();
            RefreshQueue(productionZone);
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

        private void OnFactoryLevelChanged(int level)
        {
            RefreshQueue(productionZone);
        }

        private void RefreshQueue(
            ProductionZone zone)
        {
            if (zone == null)
            {
                return;
            }

            if (queueSlots == null ||
                queueSlots.Length == 0)
            {
                return;
            }

            List<ProductionTask> tasks =
                zone.Tasks.ToList();

            int availableSlots = Mathf.Min(
                zone.QueueCapacity,
                queueSlots.Length);

            for (int i = 0; i < queueSlots.Length; i++)
            {
                QueueItemUI slot = queueSlots[i];

                if (slot == null)
                {
                    continue;
                }

                if (i < availableSlots)
                {
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

                    continue;
                }

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