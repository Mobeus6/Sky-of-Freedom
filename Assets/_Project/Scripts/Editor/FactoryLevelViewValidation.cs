#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SkyOfFreedom.Gameplay.Factory;
using SkyOfFreedom.Managers;
using SkyOfFreedom.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SkyOfFreedom.EditorTools
{
    public static class FactoryLevelViewValidation
    {
        private const string Request = "Temp/FactoryLevelViewValidation.request";
        private static readonly List<string> Results = new List<string>();
        private static readonly BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;
        private static void Check(bool ok, string label) { Results.Add((ok ? "PASS " : "FAIL ") + label); }
        [InitializeOnLoadMethod]
        private static void Schedule() { EditorApplication.delayCall += Requested; }
        private static void Requested()
        {
            if (!File.Exists(Request)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            { EditorApplication.delayCall += Requested; return; }
            File.Delete(Request);
            Run();
        }
        private static GameObject Child(string name, Transform parent)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }
        private static GameObject Building(int level, Transform parent)
        {
            var root = Child("Factory Lvl " + level, parent);
            var zones = Child("Factory Zones", root.transform);
            foreach (string name in new[] { "Production Zone", "Assembly Zone", "Research Zone", "Warehouse Zone" })
                Child(name, zones.transform).AddComponent<FactoryZoneInteraction>();
            return root;
        }
        [MenuItem("Tools/Sky of Freedom/Validate Factory Level Views")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Results.Clear();
            Scene scene = EditorSceneManager.NewPreviewScene();
            var root = new GameObject("Factory view tests");
            root.SetActive(false);
            SceneManager.MoveGameObjectToScene(root, scene);
            try
            {
                var parent = Child("Factory", root.transform);
                var first = Building(1, parent.transform);
                var second = Building(2, parent.transform);
                var third = Building(3, parent.transform);
                var fourth = Building(4, parent.transform);
                var fifth = Building(5, parent.transform);
                var production = Child("Production UI", root.transform).AddComponent<ProductionMiniPanelUI>();
                var assembly = Child("Assembly UI", root.transform).AddComponent<AssemblyMiniPanelUI>();
                var research = Child("Research UI", root.transform).AddComponent<ResearchMiniPanelUI>();
                var warehouse = Child("Warehouse UI", root.transform).AddComponent<WarehouseMiniPanelUI>();
                var view = parent.AddComponent<FactoryLevelView>();
                view.DiscoverLevels();
                var entries = (FactoryLevelView.LevelView[])typeof(FactoryLevelView).GetField("levels", Flags).GetValue(view);
                Check(entries.Length == 5 && entries[0].ReadyForUse && !entries[1].ReadyForUse, "Five levels; only first ready by default");
                Check(view.ApplyLevel(2) && view.VisibleLevel == 1 && first.activeSelf && !second.activeSelf, "Unprepared second level falls back to first");
                entries[1].ReadyForUse = true;
                Check(view.ApplyLevel(2) && view.VisibleLevel == 2 && !first.activeSelf && second.activeSelf, "Prepared second replaces first");
                Check(Bound(production, "production", second) && Bound(assembly, "assembly", second) && Bound(research, "research", second) && Bound(warehouse, "warehouse", second), "All four inactive panels rebound");
                Check(view.ApplyLevel(2) && second.activeSelf, "Repeated apply is stable");
                Check(view.ApplyLevel(5) && view.VisibleLevel == 2, "Highest prepared lower level used");
                entries[4].ReadyForUse = true;
                Check(view.ApplyLevel(99) && view.VisibleLevel == 5 && !second.activeSelf && fifth.activeSelf, "Upper bound is five");
                Check(view.ApplyLevel(0) && view.VisibleLevel == 1 && !fifth.activeSelf, "Lower bound is one; downgrade supported");
                UnityEngine.Object.DestroyImmediate(third.transform.Find("Factory Zones/Research Zone").gameObject);
                entries[2].ReadyForUse = true;
                Check(view.ApplyLevel(3) && view.VisibleLevel == 2, "Incomplete building rejected");
                fourth.AddComponent<FactoryManager>();
                entries[3].ReadyForUse = true;
                Check(view.ApplyLevel(4) && view.VisibleLevel == 2, "Gameplay manager inside scenery rejected");
                foreach (var entry in entries) entry.ReadyForUse = false;
                Check(!view.ApplyLevel(5) && second.activeSelf && view.VisibleLevel == 2, "No valid candidate preserves visible building");
                entries[1].ReadyForUse = true;
                view.DiscoverLevels();
                entries = (FactoryLevelView.LevelView[])typeof(FactoryLevelView).GetField("levels", Flags).GetValue(view);
                Check(entries[1].ReadyForUse && entries[1].Root == second, "Rediscovery preserves author configuration");
                foreach (MonoBehaviour panel in new MonoBehaviour[] { production, assembly, research, warehouse })
                {
                    string prefix = panel.GetType().Name.Replace("MiniPanelUI", "").ToLowerInvariant();
                    var oldZone = first.transform.Find("Factory Zones/" + char.ToUpperInvariant(prefix[0]) + prefix.Substring(1) + " Zone").GetComponent<FactoryZoneInteraction>();
                    var newZone = second.transform.Find("Factory Zones/" + char.ToUpperInvariant(prefix[0]) + prefix.Substring(1) + " Zone").GetComponent<FactoryZoneInteraction>();
                    var bind = panel.GetType().GetMethod("BindZone");
                    bind.Invoke(panel, new object[] { oldZone });
                    panel.GetType().GetMethod("OnEnable", Flags).Invoke(panel, null);
                    bind.Invoke(panel, new object[] { newZone });
                    oldZone.SelectZone();
                    Check(panel.GetComponent<CanvasGroup>().alpha == 0, prefix + " old zone unsubscribed");
                    oldZone.ClearSelection();
                    panel.GetType().GetMethod("OnDisable", Flags).Invoke(panel, null);
                    panel.GetType().GetMethod("OnEnable", Flags).Invoke(panel, null);
                    bind.Invoke(panel, new object[] { newZone });
                    newZone.SelectZone();
                    Check(panel.GetComponent<CanvasGroup>().alpha == 1, prefix + " new zone opens panel after enable");
                    newZone.ClearSelection();
                    Check(panel.GetComponent<CanvasGroup>().alpha == 0, prefix + " new zone deselection hides panel");
                    panel.GetType().GetMethod("OnDisable", Flags).Invoke(panel, null);
                }
            }
            catch (Exception e) { Results.Add("FAIL " + e); }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
            Directory.CreateDirectory("Temp/FactoryLevelViewValidation");
            File.WriteAllLines("Temp/FactoryLevelViewValidation/results.txt", Results);
        }
        private static bool Bound(MonoBehaviour panel, string prefix, GameObject root)
        {
            var zone = (FactoryZoneInteraction)panel.GetType().GetField(prefix + "ZoneInteraction", Flags).GetValue(panel);
            return zone != null && zone.transform.IsChildOf(root.transform);
        }
    }
}
#endif
