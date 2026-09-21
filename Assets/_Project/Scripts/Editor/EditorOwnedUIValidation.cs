#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SkyOfFreedom.UI;
using SkyOfFreedom.UI.Contracts;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SkyOfFreedom.EditorTools
{
    public static class EditorOwnedUIValidation
    {
        private const string Request = "Temp/EditorOwnedUIValidation.request";
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
        [MenuItem("Tools/Sky of Freedom/Validate Editor Owned UI")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var results = new List<string>();
            Action<bool, string> check = (ok, label) => results.Add((ok ? "PASS " : "FAIL ") + label);
            var root = new GameObject("Editor UI validation", typeof(RectTransform));
            root.SetActive(false);
            root.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                foreach (Type type in new[] { typeof(ProductionCardUI), typeof(WarehouseCardUI), typeof(WarehouseInfoPanelUI),
                    typeof(ContractCardUI), typeof(ContractComponentUI), typeof(ContractDetailUI), typeof(ResponsiveGamePanelUI), typeof(ResponsiveGridUI) })
                    check(type.GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic) == null, type.Name + " has no runtime layout update");
                var rect = (RectTransform)root.transform;
                rect.anchorMin = new Vector2(.2f, .3f);
                rect.anchorMax = new Vector2(.7f, .9f);
                rect.sizeDelta = new Vector2(321, 123);
                rect.anchoredPosition = new Vector2(17, 29);
                string before = JsonUtility.ToJson(rect);
                var grid = root.AddComponent<GridLayoutGroup>();
                grid.cellSize = new Vector2(81, 37);
                ResponsiveGamePanelUI.Attach(root, ResponsiveGamePanelUI.PanelKind.Warehouse);
                ResponsiveGridUI.Configure(rect, 600, 180);
                check(JsonUtility.ToJson(rect) == before, "Legacy entry points preserve RectTransform");
                check(grid.cellSize == new Vector2(81,37), "Authored grid cell size preserved");
                check(root.GetComponent<ResponsiveGamePanelUI>() == null && root.GetComponent<ResponsiveGridUI>() == null, "No automatic layout components added");
                var labelObject = new GameObject("Count Text", typeof(RectTransform));
                labelObject.transform.SetParent(root.transform, false);
                var text = labelObject.AddComponent<TextMeshProUGUI>();
                text.margin = new Vector4(9,8,7,6);
                text.alignment = TextAlignmentOptions.MidlineRight;
                string textRect = JsonUtility.ToJson(text.rectTransform);
                var input = QuantityInputUI.Create(text);
                check(input != null && input.textComponent == text, "Manual quantity entry retained");
                check(text.margin == new Vector4(9,8,7,6) && text.alignment == TextAlignmentOptions.MidlineRight, "Authored text styling preserved");
                check(JsonUtility.ToJson(text.rectTransform) == textRect, "Quantity RectTransform preserved");
                check(QuantityInputUI.Create(text) == input, "Repeated binding does not duplicate input");
                check(QuantityInputUI.Parse("999", 1, 1, 25) == 25, "Quantity bounds retained");
            }
            catch (Exception e) { results.Add("FAIL " + e); }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            Directory.CreateDirectory("Temp/EditorOwnedUIValidation");
            File.WriteAllLines("Temp/EditorOwnedUIValidation/results.txt", results);
        }
    }
}
#endif
