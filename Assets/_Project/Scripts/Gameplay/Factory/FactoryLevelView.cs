using System;
using SkyOfFreedom.Managers;
using SkyOfFreedom.Production;
using SkyOfFreedom.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SkyOfFreedom.Gameplay.Factory
{
    /// <summary>Changes scenery only. FactoryManager remains the authority for progression.</summary>
    [DisallowMultipleComponent]
    public sealed class FactoryLevelView : MonoBehaviour
    {
        [Serializable]
        public sealed class LevelView
        {
            [Range(1, 5)] public int Level = 1;
            public GameObject Root;
            [Tooltip("Enable only after preparing the building and all four interaction zones.")]
            public bool ReadyForUse;
        }

        [SerializeField] private LevelView[] levels = new LevelView[0];
        private FactoryManager factory;
        private GameObject currentRoot;
        private bool applied;
        public int VisibleLevel { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Game") return;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name != "Environment") continue;
                Transform parent = root.transform.Find("Factory");
                if (parent != null && parent.GetComponent<FactoryLevelView>() == null)
                    parent.gameObject.AddComponent<FactoryLevelView>();
            }
        }

        private void Reset() { DiscoverLevels(); }

        private void Awake()
        {
            if (levels == null || levels.Length == 0) DiscoverLevels();
        }

        [ContextMenu("Discover Missing Level Roots")]
        public void DiscoverLevels()
        {
            LevelView[] previous = levels;
            levels = new LevelView[5];
            for (int i = 0; i < levels.Length; i++)
            {
                LevelView entry = null;
                if (previous != null)
                    foreach (LevelView existing in previous)
                        if (existing != null && existing.Level == i + 1) { entry = existing; break; }
                if (entry == null) entry = new LevelView { Level = i + 1, ReadyForUse = i == 0 };
                if (entry.Root == null)
                {
                    Transform child = transform.Find("Factory Lvl " + (i + 1));
                    if (child != null) entry.Root = child.gameObject;
                }
                levels[i] = entry;
            }
        }

        private void Update()
        {
            GameManager game = GameManager.Instance;
            if (game == null || !game.IsGameReady)
            {
                applied = false;
                return;
            }
            if (factory != game.Factory)
            {
                Unsubscribe();
                factory = game.Factory;
                if (factory != null) factory.OnFactoryLevelChanged += OnLevelChanged;
                applied = false;
            }
            if (!applied && factory != null)
            {
                applied = true;
                ApplyLevel(factory.Level);
            }
        }

        private void OnDisable() { Unsubscribe(); applied = false; }
        private void Unsubscribe()
        {
            if (factory != null) factory.OnFactoryLevelChanged -= OnLevelChanged;
            factory = null;
        }
        private void OnLevelChanged(int level)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameReady) ApplyLevel(level);
        }

        /// <summary>False leaves the current scenery intact when no safe candidate exists.</summary>
        public bool ApplyLevel(int requestedLevel)
        {
            LevelView selected = null;
            int target = Mathf.Clamp(requestedLevel, 1, 5);
            if (levels == null) return false;
            foreach (LevelView entry in levels)
            {
                if (entry == null || !entry.ReadyForUse || entry.Level < 1 || entry.Level > target ||
                    (selected != null && entry.Level <= selected.Level)) continue;
                if (IsValidRoot(entry.Root)) selected = entry;
            }
            if (selected == null) return false;
            if (currentRoot == selected.Root && currentRoot.activeSelf && VisibleLevel == selected.Level) return true;

            FactoryZoneInteraction zone = FactoryZoneInteraction.SelectedZone;
            if (zone != null && zone.transform.IsChildOf(transform)) zone.ClearSelection();

            // Never disable managers or queues accidentally placed under a visual root.
            foreach (LevelView entry in levels)
                if (entry != null && entry.Root != selected.Root && IsSafeVisualRoot(entry.Root))
                    entry.Root.SetActive(false);

            selected.Root.SetActive(true);
            BindPanels(selected.Root);
            currentRoot = selected.Root;
            VisibleLevel = selected.Level;
            return true;
        }

        private bool IsSafeVisualRoot(GameObject root)
        {
            return root != null && root.transform.parent == transform &&
                root.GetComponentInChildren<BaseManager>(true) == null &&
                root.GetComponentInChildren<GameManager>(true) == null &&
                root.GetComponentInChildren<ProductionZone>(true) == null;
        }

        private bool IsValidRoot(GameObject root)
        {
            if (!IsSafeVisualRoot(root)) return false;
            return FindZone(root, "Production Zone") != null && FindZone(root, "Assembly Zone") != null &&
                FindZone(root, "Research Zone") != null && FindZone(root, "Warehouse Zone") != null;
        }

        private static FactoryZoneInteraction FindZone(GameObject root, string name)
        {
            Transform child = root.transform.Find("Factory Zones/" + name);
            return child != null ? child.GetComponent<FactoryZoneInteraction>() : null;
        }

        private void BindPanels(GameObject root)
        {
            // Include inactive UI; its next OnEnable will subscribe to the new zone.
            foreach (GameObject sceneRoot in gameObject.scene.GetRootGameObjects())
            {
                foreach (ProductionMiniPanelUI panel in sceneRoot.GetComponentsInChildren<ProductionMiniPanelUI>(true))
                    panel.BindZone(FindZone(root, "Production Zone"));
                foreach (AssemblyMiniPanelUI panel in sceneRoot.GetComponentsInChildren<AssemblyMiniPanelUI>(true))
                    panel.BindZone(FindZone(root, "Assembly Zone"));
                foreach (ResearchMiniPanelUI panel in sceneRoot.GetComponentsInChildren<ResearchMiniPanelUI>(true))
                    panel.BindZone(FindZone(root, "Research Zone"));
                foreach (WarehouseMiniPanelUI panel in sceneRoot.GetComponentsInChildren<WarehouseMiniPanelUI>(true))
                    panel.BindZone(FindZone(root, "Warehouse Zone"));
            }
        }
    }
}
