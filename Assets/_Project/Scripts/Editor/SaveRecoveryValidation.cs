#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SkyOfFreedom.Data;
using SkyOfFreedom.Managers;
using SkyOfFreedom.Services;
using SkyOfFreedom.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SkyOfFreedom.EditorTools
{
    public static class SaveRecoveryValidation
    {
        private const string Request = "Temp/SkySaveRecoveryValidation.request";
        private static readonly List<string> Results = new List<string>();
        [InitializeOnLoadMethod]
        private static void Schedule() { EditorApplication.delayCall += Requested; }
        private static void Requested()
        {
            if (!File.Exists(Request)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
            {
                EditorApplication.delayCall += Requested;
                return;
            }
            File.Delete(Request);
            Run();
        }

        private static void Check(bool value, string name)
        {
            Results.Add((value ? "PASS " : "FAIL ") + name);
        }

        [MenuItem("Tools/Sky of Freedom/Validate Save Recovery")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Results.Clear();
            string output = Path.Combine("Temp/SaveRecoveryValidation", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(output);
            Scene scene = default;
            try
            {
                var data = new PlayerData();
                data.Account.PlayerId = "synthetic-recovery-player";
                data.Account.LastSaveAtUtc = "2026-09-17T12:00:00.0000000Z";
                data.Economy.Money = 2147484000L;
                data.Economy.Reputation = 53;
                data.Factory.FactoryLevel = 3;
                data.Warehouse.Items.Add(new PlayerWarehouseItemData { ItemId = "test-material", Quantity = 17 });
                data.Production.LastProcessedAtUtc = data.Account.LastSaveAtUtc;
                data.Production.Tasks.Add(new PlayerProductionTaskData
                {
                    TaskId = Guid.NewGuid().ToString(), TargetId = "test-component", ZoneType = "Production",
                    Quantity = 5, ProducedQuantity = 2, CurrentItemProgress = 3.5f, State = "Running"
                });
                data.Contracts.Active.Add(new PlayerContractData { TemplateId = "active", Quantity = 5, DeliveredQuantity = 2 });
                data.Contracts.History.Add(new PlayerContractData { TemplateId = "done", State = "Completed", Reward = 200 });
                data.Research.ResearchStates.Add(new PlayerResearchStateData
                {
                    ResearchId = "research", IsResearching = true, RemainingTime = 23, TotalResearchTime = 60
                });
                data.Statistics.ContractsCompleted = 4;
                data.Licenses.UnlockedLicenseIds.Add("test-license");
                string json = JsonUtility.ToJson(data);
                var store = new LocalSaveStore(Path.Combine(output, "fixtures"));
                store.Write(data.Account.PlayerId, json, "base", true);
                var method = typeof(GameManager).GetMethod("ReadLocalPlayer", BindingFlags.NonPublic | BindingFlags.Static);
                var restored = (PlayerData)method.Invoke(null, new object[] { store.Read(data.Account.PlayerId), data.Account.PlayerId });
                Check(restored.Economy.Money == 2147484000L, "64-bit money round trip");
                Check(restored.Economy.Reputation == 53 && restored.Factory.FactoryLevel == 3, "reputation and factory round trip");
                Check(restored.Warehouse.Items.Single().Quantity == 17, "warehouse round trip");
                Check(restored.Production.Tasks.Single().ProducedQuantity == 2 &&
                    restored.Production.Tasks.Single().CurrentItemProgress == 3.5f, "production partial progress round trip");
                Check(restored.Production.LastProcessedAtUtc == data.Account.LastSaveAtUtc, "offline production timestamp preserved");
                Check(restored.Contracts.Active.Single().DeliveredQuantity == 2, "active contract round trip");
                Check(restored.Contracts.History.Single().State == "Completed", "completed contract retained without issuing rewards");
                Check(restored.Research.ResearchStates.Single().RemainingTime == 23, "active research round trip");
                Check(restored.Statistics.ContractsCompleted == 4, "statistics not incremented on decode");
                Check(restored.Licenses.UnlockedLicenseIds.Single() == "test-license", "licenses round trip");
                Check(JsonUtility.ToJson(restored) == json, "entire PlayerData snapshot unchanged on repeated decode");
                bool rejected = false;
                try { method.Invoke(null, new object[] { store.Read(data.Account.PlayerId), "other-account" }); }
                catch (TargetInvocationException e) { rejected = e.InnerException is InvalidDataException; }
                Check(rejected, "wrong account rejected before manager restoration");

                scene = EditorSceneManager.NewPreviewScene();
                var owner = new GameObject("Isolated Save Recovery UI Test");
                SceneManager.MoveGameObjectToScene(owner, scene);
                var task = SaveConflictPopupUI.ChooseAsync(owner.transform, restored, data);
                Button[] buttons = owner.GetComponentsInChildren<Button>(true);
                Button confirm = buttons.Single(b => b.name == "Confirm");
                Check(!confirm.interactable && !task.IsCompleted, "no default or automatic conflict choice");
                buttons.Single(b => b.name == "Use this device").onClick.Invoke();
                Check(confirm.interactable && !task.IsCompleted, "local selection still requires confirmation");
                buttons.Single(b => b.name == "Use cloud").onClick.Invoke();
                Check(confirm.GetComponentInChildren<TMPro.TMP_Text>().text.Contains("cloud"), "selection can switch to cloud before confirmation");
                // Non-ExecuteAlways behaviours in an Edit Mode preview do not receive
                // normal Play Mode lifecycle callbacks. Test the actual callback explicitly.
                var popup = owner.GetComponentInChildren<SaveConflictPopupUI>();
                typeof(SaveConflictPopupUI).GetMethod("OnDestroy", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(popup, null);
                Check(task.IsCanceled, "destruction callback cancels pending selection");
                UnityEngine.Object.DestroyImmediate(owner);
            }
            catch (Exception exception) { Results.Add("FAIL " + exception); }
            finally
            {
                if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
                File.WriteAllLines(Path.Combine(output, "report.txt"), Results);
                File.WriteAllText("Temp/SaveRecoveryValidation/latest.txt", output);
                Debug.Log("Save recovery validation: " + Results.Count(r => r.StartsWith("PASS")) + " PASS, " +
                    Results.Count(r => r.StartsWith("FAIL")) + " FAIL. " + output);
            }
        }
    }
}
#endif
