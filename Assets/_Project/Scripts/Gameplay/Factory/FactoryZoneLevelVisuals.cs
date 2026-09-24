using System;
using SkyOfFreedom.Factory;
using SkyOfFreedom.Managers;
using SkyOfFreedom.Production;
using UnityEngine;

namespace SkyOfFreedom.Gameplay.Factory
{
    [DisallowMultipleComponent]
    public sealed class FactoryZoneLevelVisuals : MonoBehaviour
    {
        [Serializable]
        public sealed class VisualGroup
        {
            [Range(1, 5)] public int FromLevel = 2;
            [Range(1, 5)] public int ThroughLevel = 5;
            public GameObject Root;
        }

        [SerializeField] private FactoryZoneType zoneType = FactoryZoneType.Production;
        [SerializeField] private VisualGroup[] groups = new VisualGroup[0];
        [Header("Editor preview (does not change saved progression)")]
        [SerializeField, Range(1, 5)] private int previewLevel = 1;
        private FactoryManager factory;
        private bool applied;

        private void Update()
        {
            var game = GameManager.Instance;
            if (game == null || !game.IsGameReady || game.IsAccountTransition)
            {
                Unsubscribe();
                return;
            }
            if (factory != game.Factory)
            {
                Unsubscribe();
                factory = game.Factory;
                if (factory != null) factory.OnZoneLevelChanged += OnZoneLevelChanged;
            }
            // Only initialization polls readiness. Object visibility changes on level events.
            if (!applied && factory != null)
            {
                Apply(factory.GetLevel(zoneType));
                applied = true;
            }
        }

        private void OnZoneLevelChanged(FactoryZoneType changedZone, int level)
        {
            var game = GameManager.Instance;
            if (changedZone == zoneType && game != null && game.IsGameReady && !game.IsAccountTransition)
                Apply(level);
        }

        private void OnDisable() { Unsubscribe(); }

        private void Unsubscribe()
        {
            if (factory != null) factory.OnZoneLevelChanged -= OnZoneLevelChanged;
            factory = null;
            applied = false;
        }

        private bool SafeRoot(GameObject root)
        {
            return root != null && root != gameObject && root.transform.IsChildOf(transform) &&
                root.GetComponentInChildren<BaseManager>(true) == null &&
                root.GetComponentInChildren<GameManager>(true) == null &&
                root.GetComponentInChildren<ProductionZone>(true) == null &&
                root.GetComponentInChildren<FactoryZoneInteraction>(true) == null &&
                root.GetComponentInChildren<FactoryZoneLevelVisuals>(true) == null;
        }

        private void Apply(int level)
        {
            if (zoneType == FactoryZoneType.Factory || groups == null) return;
            level = Mathf.Clamp(level, 1, 5);
            foreach (var group in groups)
            {
                if (group == null || !SafeRoot(group.Root)) continue;
                bool overlapping = false;
                foreach (var other in groups)
                {
                    if (other == null || ReferenceEquals(other, group) || other.Root == null) continue;
                    if (other.Root == group.Root || group.Root.transform.IsChildOf(other.Root.transform) ||
                        other.Root.transform.IsChildOf(group.Root.transform)) { overlapping = true; break; }
                }
                // Nested or duplicate groups would make activation order-dependent.
                if (overlapping || group.FromLevel > group.ThroughLevel) continue;
                bool visible = level >= group.FromLevel && level <= group.ThroughLevel;
                if (group.Root.activeSelf != visible) group.Root.SetActive(visible);
            }
        }

        [ContextMenu("Preview Selected Level")]
        private void PreviewSelectedLevel()
        {
            if (Application.isPlaying) return;
#if UNITY_EDITOR
            if (groups != null)
                foreach (var group in groups)
                    if (group != null && SafeRoot(group.Root))
                        UnityEditor.Undo.RecordObject(group.Root, "Preview zone level");
#endif
            Apply(previewLevel);
#if UNITY_EDITOR
            if (groups != null)
                foreach (var group in groups)
                    if (group != null && SafeRoot(group.Root))
                        UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(group.Root);
#endif
        }

        private void OnValidate()
        {
            applied = false;
            if (groups == null) return;
            foreach (var group in groups)
            {
                if (group == null || group.Root == null) continue;
                if (!SafeRoot(group.Root) || group.FromLevel > group.ThroughLevel)
                    Debug.LogWarning("Zone visuals: use child decoration groups only, with a valid level range.", this);
            }
        }
    }
}
