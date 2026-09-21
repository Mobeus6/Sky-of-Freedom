#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SkyOfFreedom.Data;
using SkyOfFreedom.Gameplay;
using SkyOfFreedom.Managers;
using UnityEditor;
using UnityEngine;

namespace SkyOfFreedom.EditorTools
{
    public static class OfflineDayNightValidation
    {
        private const string Request = "Temp/OfflineDayNightValidation.request";
        private static readonly List<string> Results = new List<string>();
        private static void Check(bool ok, string label) { Results.Add((ok ? "PASS " : "FAIL ") + label); }
        private static void Set(object target, string field, object value)
        {
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
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
        [MenuItem("Tools/Sky of Freedom/Validate Offline and Day Night")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Results.Clear();
            GameObject root = null;
            GameDatabase db = null;
            ResearchSO research = null;
            try
            {
                DateTime start = new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc);
                float hour = FactoryDayNight.CalculateHour(start, 24, 0);
                Check(Math.Abs(FactoryDayNight.CalculateHour(start.AddMinutes(24),24,0)-hour)<0.001f, "Full day wraps");
                Check(Math.Abs(FactoryDayNight.CalculateHour(start.AddMinutes(6),24,0)-(hour+6)%24)<0.001f, "Six offline minutes advances six hours");
                Check(FactoryDayNight.CalculateHour(start,0,double.NaN)==hour, "Invalid cycle settings safe");
                root = new GameObject("Offline Validation (temporary)");
                root.hideFlags = HideFlags.HideAndDontSave;
                var manager = root.AddComponent<ResearchManager>();
                var factory = root.AddComponent<FactoryManager>();
                db = ScriptableObject.CreateInstance<GameDatabase>();
                research = ScriptableObject.CreateInstance<ResearchSO>();
                research.SetID("TEST-OFFLINE");
                Set(research, "productionSpeedBonusPercent", 25f);
                Set(db, "researches", new List<ResearchSO> { research });
                db.Initialize();
                Set(manager, "gameDatabase", db);
                Set(manager, "factoryManager", factory);
                typeof(ResearchManager).GetMethod("CreateResearchStates",
                    BindingFlags.Instance | BindingFlags.NonPublic).Invoke(manager, null);
                var state = new ResearchState { ResearchID=research.ID, IsResearching=true,
                    IsUnlocked=true, RemainingTime=100, TotalResearchTime=100 };
                manager.LoadSaveData(new List<ResearchState> { state });
                int completions = 0;
                manager.OnResearchCompleted += _ => completions++;
                manager.AdvanceTime(25);
                Check(state.RemainingTime==75 && state.Progress==0.25f, "Research partial offline progress");
                manager.AdvanceTime(double.NaN);
                manager.AdvanceTime(-1);
                Check(state.RemainingTime==75, "Research rejects invalid time");
                manager.AdvanceTime(1000);
                Check(state.IsCompleted && state.RemainingTime==0 && !manager.HasActiveResearch(), "Offline completion");
                manager.AdvanceTime(1000);
                Check(completions==1, "Completion issued once");
                Check(Math.Abs(manager.GetProductionSpeedMultiplier(SkyOfFreedom.Factory.FactoryZoneType.Production)-1.25f)<0.001f,
                    "Completed research bonus active");
                manager.LoadSaveData(manager.GetSaveData());
                manager.AdvanceTime(1000);
                Check(completions==1, "Reload completed research does not issue rewards again");
                Set(research, "researchSpeedBonusPercent", 25f);
                Check(manager.GetResearchSpeedMultiplier()==1.25f, "Research speed restored from completed states");
            }
            catch (Exception e) { Results.Add("FAIL " + e); }
            finally
            {
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
                if (db != null) UnityEngine.Object.DestroyImmediate(db);
                if (research != null) UnityEngine.Object.DestroyImmediate(research);
            }
            Directory.CreateDirectory("Temp/OfflineDayNightValidation");
            File.WriteAllLines("Temp/OfflineDayNightValidation/results.txt", Results);
        }
    }
}
#endif
