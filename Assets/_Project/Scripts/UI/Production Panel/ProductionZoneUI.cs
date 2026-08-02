using SkyOfFreedom.Managers;
using SkyOfFreedom.Production;
using System.Collections.Generic;
using System.Linq;
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
            Debug.Log($"{name} Enable");
            if (zone != null)
                zone.QueueChanged += RefreshQueue;

            GameManager.Instance.Factory.OnFactoryLevelChanged += OnFactoryLevelChanged;
        }

        private void OnDisable()
        {
            Debug.Log($"{name} Disable");

            if (zone != null)
                zone.QueueChanged -= RefreshQueue;

            if (GameManager.Instance != null)
                GameManager.Instance.Factory.OnFactoryLevelChanged -= OnFactoryLevelChanged;
        }

        private void Start()
        {
            if (zone != null)
                zone.QueueChanged += RefreshQueue;

            GameManager.Instance.Factory.OnFactoryLevelChanged += OnFactoryLevelChanged;

            RefreshQueue(zone);
        }
        private void OnDestroy()
        {
            if (zone != null)
                zone.QueueChanged -= RefreshQueue;

            if (GameManager.Instance != null)
                GameManager.Instance.Factory.OnFactoryLevelChanged -= OnFactoryLevelChanged;
        }
        private void OnFactoryLevelChanged(int level)
        {
            if (zone != null)
                RefreshQueue(zone);
        }
        private void RefreshQueue(ProductionZone productionZone)
        {
            Debug.Log($"RefreshQueue {productionZone.ZoneType} Tasks={productionZone.Tasks.Count()}");
            List<ProductionTask> tasks = productionZone.Tasks.ToList();

            int unlockedSlots = GameManager.Instance.Factory.GetUnlockedQueueSlots();

            for (int i = 0; i < queueSlots.Length; i++)
            {
                QueueItemUI slot = queueSlots[i];
                Debug.Log($"Slot {i}");
                if (i >= unlockedSlots)
                {
                    int requiredLevel =
                        GameManager.Instance.Factory.GetRequiredFactoryLevelForQueue(i);

                    slot.ShowLocked(requiredLevel);
                }
                else if (i < tasks.Count)
                {
                    slot.Setup(tasks[i], productionZone);
                }
                else
                {
                    slot.ShowEmpty();
                }
            }
        }
    }
}