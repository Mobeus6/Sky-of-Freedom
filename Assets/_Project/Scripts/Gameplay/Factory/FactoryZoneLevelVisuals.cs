using System;
using System.Collections;
using System.Collections.Generic;
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
            [Tooltip("Optional smoke anchors outside the toggled decoration groups.")]
            public Transform[] EffectPoints = new Transform[0];
        }

        [SerializeField] private FactoryZoneType zoneType = FactoryZoneType.Production;
        [SerializeField] private VisualGroup[] groups = new VisualGroup[0];
        [Header("Upgrade presentation (all references optional)")]
        [SerializeField] private bool animateUpgrades = true;
        [SerializeField] private FactoryZoneInteraction zoneInteraction;
        [SerializeField] private ParticleSystem smokePrefab;
        [SerializeField, Min(0f)] private float revealDelay = 0.25f;
        [SerializeField, Min(0.1f)] private float effectDuration = 1.4f;
        [SerializeField, Min(0.1f)] private float smokeLifetime = 3f;
        [Header("Editor preview (does not change saved progression)")]
        [SerializeField, Range(1, 5)] private int previewLevel = 1;
        private FactoryManager factory;
        private bool applied;
        private int currentLevel;
        private Coroutine presentation;
        private readonly List<GameObject> spawnedEffects = new List<GameObject>();
        private bool deferUpgrade;
        private int deferredLevel;
        private bool previewActive;
        public FactoryZoneType ZoneType => zoneType;
        [SerializeField, HideInInspector] private int timingVersion;
        public bool IsPresenting => presentation != null;
        public bool IsUpgradeCameraMoving => zoneInteraction != null && zoneInteraction.IsUpgradeCameraMoving;
        public void FocusUpgradeCamera()
        {
            if (zoneInteraction == null) zoneInteraction = GetComponent<FactoryZoneInteraction>();
            if (zoneInteraction != null) zoneInteraction.FocusUpgradeCamera();
        }

        private void UpgradeTiming()
        {
            if (timingVersion >= 1) return;
            revealDelay = Mathf.Max(revealDelay, .8f);
            effectDuration = Mathf.Max(effectDuration, 3.5f);
            smokeLifetime = Mathf.Max(smokeLifetime, 5f);
            timingVersion = 1;
        }

        // Presentation only: never writes to the factory manager or saved progress.
        public void PreviewUpgrade(int previous, int level)
        {
            if (!Application.isPlaying || !isActiveAndEnabled) return;
            CancelPresentation();
            var game = GameManager.Instance;
            if (game == null || !game.IsGameReady || game.IsAccountTransition || game.Factory == null) return;
            currentLevel = game.Factory.GetLevel(zoneType);
            applied = true;
            previous = Mathf.Clamp(previous, 1, 4);
            level = Mathf.Clamp(level, previous + 1, 5);
            var additions = new List<VisualGroup>();
            if (groups != null)
                foreach (var group in groups)
                    if (ValidGroup(group) && Visible(group, level) && !Visible(group, previous))
                        additions.Add(group);
            previewActive = true;
            Apply(previous);
            presentation = StartCoroutine(AnimateUpgrade(level, additions));
        }

        public void StopPreview()
        {
            if (previewActive) CancelPresentation();
        }

        private void RestorePreview()
        {
            if (!previewActive) return;
            previewActive = false;
            Apply(currentLevel);
        }

        public void BeginSharedUpgrade()
        {
            CancelPresentation();
            if (applied) Apply(currentLevel);
            else if (GameManager.Instance != null && GameManager.Instance.Factory != null)
            {
                currentLevel = GameManager.Instance.Factory.GetLevel(zoneType);
                Apply(currentLevel);
                applied = true;
            }
            deferredLevel = 0;
            deferUpgrade = true;
        }

        public void FinishSharedUpgrade(bool animate)
        {
            deferUpgrade = false;
            int target = deferredLevel;
            deferredLevel = 0;
            if (target == 0) return;
            if (!animate || !isActiveAndEnabled)
            {
                currentLevel = target;
                Apply(target);
                return;
            }
            PresentLevel(target);
        }

        private void Awake()
        {
            UpgradeTiming();
            if (zoneInteraction == null) zoneInteraction = GetComponent<FactoryZoneInteraction>();
        }

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
                currentLevel = Mathf.Clamp(factory.GetLevel(zoneType), 1, 5);
                applied = true;
            }
        }

        private void OnZoneLevelChanged(FactoryZoneType changedZone, int level)
        {
            var game = GameManager.Instance;
            if (changedZone == zoneType && game != null && game.IsGameReady && !game.IsAccountTransition)
                PresentLevel(level);
        }

        private void PresentLevel(int level)
        {
            level = Mathf.Clamp(level, 1, 5);
            if (deferUpgrade) { deferredLevel = level; return; }
            bool upgrade = applied && level > currentLevel && animateUpgrades;
            CancelPresentation();
            // Finish a previous reveal before processing another level event.
            if (applied) Apply(currentLevel);
            var additions = new List<VisualGroup>();
            if (upgrade && groups != null)
                foreach (var group in groups)
                    if (ValidGroup(group) && Visible(group, level) && !Visible(group, currentLevel))
                        additions.Add(group);
            currentLevel = level;
            applied = true;
            if (!upgrade || !isActiveAndEnabled) { Apply(level); return; }
            presentation = StartCoroutine(AnimateUpgrade(level, additions));
        }

        private IEnumerator AnimateUpgrade(int level, List<VisualGroup> additions)
        {
            if (additions.Count == 0)
                Debug.LogWarning("Zone upgrade: no new decoration group at level " + level +
                    ". Check Groups / From Level; smoke is spawned only for newly unlocked groups.", this);
            if (zoneInteraction != null) zoneInteraction.PulseUpgrade(effectDuration);
            Debug.Log("[ZoneUpgrade] " + zoneType + ": animation started; new groups=" + additions.Count +
                ", smoke=" + (smokePrefab != null) + ", highlight=" + (zoneInteraction != null), this);
            foreach (var group in additions)
            {
                bool hasPoint = false;
                if (group.EffectPoints != null)
                    foreach (var point in group.EffectPoints)
                        if (point != null) { SpawnSmoke(point.position, point.rotation); hasPoint = true; }
                if (!hasPoint) SpawnSmoke(group.Root.transform.position, Quaternion.identity);
            }
            float duration = Mathf.Max(0.1f, effectDuration);
            // Without smoke there is no reason to hide the new equipment temporarily.
            float delay = smokePrefab != null ? Mathf.Min(revealDelay, duration * 0.5f) : 0f;
            bool revealed = false;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (!revealed && elapsed >= delay) { Apply(level); revealed = true; }
                float step = Mathf.Min(Time.unscaledDeltaTime, .05f);
                SimulateSmoke(step);
                elapsed += step;
                yield return null;
            }
            Apply(level);
            float remaining = Mathf.Max(0f, smokeLifetime - elapsed);
            while (remaining > 0f)
            {
                float step = Mathf.Min(Time.unscaledDeltaTime, .05f);
                SimulateSmoke(step);
                remaining -= step;
                yield return null;
            }
            ClearSmoke();
            RestorePreview();
            presentation = null;
        }

        private void SpawnSmoke(Vector3 position, Quaternion rotation)
        {
            if (smokePrefab == null) return;
            ParticleSystem effect = Instantiate(smokePrefab, position, rotation * smokePrefab.transform.localRotation, transform);
            spawnedEffects.Add(effect.gameObject);
            effect.gameObject.SetActive(true);
            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            foreach (var particle in effect.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = particle.main;
                main.loop = false;
                main.useUnscaledTime = true;
            }
            effect.Play(true);
            effect.Pause(true);
        }

        private void SimulateSmoke(float step)
        {
            foreach (var effect in spawnedEffects)
                if (effect != null)
                    effect.GetComponent<ParticleSystem>().Simulate(step, true, false, false);
        }

        private void ClearSmoke()
        {
            foreach (var effect in spawnedEffects)
                if (effect != null) { effect.SetActive(false); Destroy(effect); }
            spawnedEffects.Clear();
        }

        private void CancelPresentation()
        {
            if (zoneInteraction != null) zoneInteraction.StopUpgradePulse();
            if (presentation != null) StopCoroutine(presentation);
            presentation = null;
            ClearSmoke();
            RestorePreview();
        }

        private void OnDisable() { Unsubscribe(); }

        private void Unsubscribe()
        {
            CancelPresentation();
            if (deferredLevel > 0) currentLevel = deferredLevel;
            deferredLevel = 0;
            deferUpgrade = false;
            if (applied) Apply(currentLevel);
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
                if (!ValidGroup(group)) continue;
                bool visible = Visible(group, level);
                if (group.Root.activeSelf != visible) group.Root.SetActive(visible);
            }
        }

        private static bool Visible(VisualGroup group, int level)
        {
            return level >= group.FromLevel && level <= group.ThroughLevel;
        }

        private bool ValidGroup(VisualGroup group)
        {
            if (group == null || !SafeRoot(group.Root) || group.FromLevel > group.ThroughLevel) return false;
            foreach (var other in groups)
            {
                if (other == null || ReferenceEquals(other, group) || other.Root == null) continue;
                if (other.Root == group.Root || group.Root.transform.IsChildOf(other.Root.transform) ||
                    other.Root.transform.IsChildOf(group.Root.transform)) return false;
            }
            return true;
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
            UpgradeTiming();
            if (!Application.isPlaying) applied = false;
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
