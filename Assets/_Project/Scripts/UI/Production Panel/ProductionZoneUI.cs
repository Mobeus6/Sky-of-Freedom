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
        [Tooltip("Factory means automatic detection from the existing ProductionZone/AssemblyZone panel name.")]
        [SerializeField] private FactoryZoneType zoneType = FactoryZoneType.Factory;
        private FactoryManager observedFactory;
        private float nextBindingRefresh;

        private FactoryZoneType EffectiveZoneType => zoneType != FactoryZoneType.Factory ? zoneType :
            (gameObject.name.Replace(" ", "").Equals("AssemblyZone", System.StringComparison.OrdinalIgnoreCase)
                ? FactoryZoneType.Assembly : FactoryZoneType.Production);

        private void OnEnable()
        {
            ResolveProductionZone();

            RefreshQueue(productionZone);
        }

        private void OnDisable()
        {
            if (productionZone != null)
            {
                productionZone.QueueChanged -=
                    RefreshQueue;
            }

            if (observedFactory != null)
            {
                observedFactory.OnFactoryLevelChanged -=
                    OnFactoryLevelChanged;
            }

            observedFactory = null;
            productionZone = null;
        }

        private void Start()
        {
            ResolveProductionZone();
            RefreshQueue(productionZone);
        }

        private void Update()
        {
            if (Time.unscaledTime < nextBindingRefresh) return;
            nextBindingRefresh = Time.unscaledTime + 0.25f;
            ResolveProductionZone();
        }

        private void ResolveProductionZone()
        {
            ProductionZone previous = productionZone;
            ProductionZone found = null;
            var game = GameManager.Instance;
            var factory = game != null && game.IsGameReady ? game.Factory : null;
            if (observedFactory != factory)
            {
                if (observedFactory != null) observedFactory.OnFactoryLevelChanged -= OnFactoryLevelChanged;
                observedFactory = factory;
                if (observedFactory != null) observedFactory.OnFactoryLevelChanged += OnFactoryLevelChanged;
            }

            if (game == null || !game.IsGameReady || game.IsAccountTransition)
            {
                if (previous != null) previous.QueueChanged -= RefreshQueue;
                productionZone = null;
                RefreshQueue(null);
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

                if (zone.ZoneType != EffectiveZoneType)
                {
                    continue;
                }

                found = zone;
                break;
            }
            if (previous == found) return;
            if (previous != null) previous.QueueChanged -= RefreshQueue;
            productionZone = found;
            if (productionZone != null) productionZone.QueueChanged += RefreshQueue;
            RefreshQueue(productionZone);
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
                if (queueSlots != null)
                    foreach (var slot in queueSlots)
                        if (slot != null) slot.ShowEmpty();
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
