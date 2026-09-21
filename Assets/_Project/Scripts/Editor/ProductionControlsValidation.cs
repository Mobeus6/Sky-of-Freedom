#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SkyOfFreedom.UI;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace SkyOfFreedom.EditorTools
{
    public static class ProductionControlsValidation
    {
        private const string Request = "Temp/ProductionControlsValidation.request";
        private static readonly List<string> results = new List<string>();
        private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;
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
        private static void Check(bool value, string label) { results.Add((value ? "PASS " : "FAIL ") + label); }
        [MenuItem("Tools/Sky of Freedom/Validate Production Controls")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            results.Clear();
            GameObject root = null;
            try
            {
                Check(QuantityInputUI.Parse("25", 1, 1, 50) == 25, "Manual quantity");
                Check(QuantityInputUI.Parse("999", 1, 1, 50) == 50, "Clamp to available quantity");
                Check(QuantityInputUI.Parse("", 7, 1, 50) == 7, "Empty restores previous value");
                Check(QuantityInputUI.Parse("-3", 7, 1, 50) == 7, "Negative rejected");
                Check(QuantityInputUI.Parse("1.5", 7, 1, 50) == 7, "Decimal rejected");
                Check(QuantityInputUI.Parse("9999999999", 7, 1, 50) == 7, "Overflow rejected");
                Check(QuantityInputUI.Parse("0", 7, 1, 50) == 1, "Minimum quantity");
                Check(QuantityInputUI.Parse("25", 1, 0, 0) == 0, "No materials available");
                Check(QuantityInputUI.Parse("2257", 1, 1, 3000) == 2257, "Warehouse quantity over 999");
                root = PrefabUtility.LoadPrefabContents("Assets/_Project/Prefabs/UI/Production/Production Item Card 1.prefab");
                var card = root.GetComponentInChildren<ProductionCardUI>(true);
                TMP_Text quantity = (TMP_Text)typeof(ProductionCardUI).GetField("quantityText", Flags).GetValue(card);
                TMP_InputField input = QuantityInputUI.Create(quantity);
                Check(input != null && input.textComponent == quantity, "Existing quantity label wired");
                Check(QuantityInputUI.Create(quantity) == input, "No duplicate input on repeated setup");
                QuantityInputUI.Show(input, quantity, 2257);
                Check(input.text == "2257", "Input and label synchronized");
                foreach (float width in new[] { 390f, 480f, 600f })
                {
                    ((RectTransform)card.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                    typeof(ProductionCardUI).GetMethod("LateUpdate", Flags).Invoke(card, null);
                    foreach (string name in new[] { "costText", "timeText" })
                    {
                        var text = (TMP_Text)typeof(ProductionCardUI).GetField(name, Flags).GetValue(card);
                        text.text = "1200";
                        Check(text.margin == Vector4.zero && text.rectTransform.rect.width >= 100 && text.rectTransform.rect.height >= 30,
                            name + " has visible text area at width " + width);
                    }
                }
            }
            catch (Exception e) { results.Add("FAIL " + e); }
            finally { if (root != null) PrefabUtility.UnloadPrefabContents(root); }
            Directory.CreateDirectory("Temp/ProductionControlsValidation");
            File.WriteAllLines("Temp/ProductionControlsValidation/results.txt", results);
        }
    }
}
#endif
