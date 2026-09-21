#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SkyOfFreedom.Data;
using SkyOfFreedom.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SkyOfFreedom.EditorTools
{
    public static class SeptemberUIValidation
    {
        private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;
        private static readonly List<string> results = new List<string>();
        [InitializeOnLoadMethod]
        private static void Schedule() { EditorApplication.delayCall += Requested; }
        private static void Requested()
        {
            if (!File.Exists("Temp/SeptemberUIValidation.request")) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            { EditorApplication.delayCall += Requested; return; }
            File.Delete("Temp/SeptemberUIValidation.request");
            Run();
        }
        private static void Check(bool value, string message) { results.Add((value ? "PASS " : "FAIL ") + message); }
        private static void Call(object obj, string method) { obj.GetType().GetMethod(method, Flags).Invoke(obj, null); }
        [MenuItem("Tools/Sky of Freedom/Validate Menu Background and Description")]
        public static void Run()
        {
            results.Clear();
            Scene scene = EditorSceneManager.NewPreviewScene();
            GameObject prefab = null;
            ComponentSO item = null;
            try
            {
                var canvasObject = new GameObject("MainMenuCanvas", typeof(RectTransform), typeof(Canvas));
                SceneManager.MoveGameObjectToScene(canvasObject, scene);
                canvasObject.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
                var canvas = (RectTransform)canvasObject.transform;
                canvas.sizeDelta = new Vector2(2400, 1080);
                var panelObject = new GameObject("MainMenu Panel", typeof(RectTransform), typeof(Image));
                panelObject.transform.SetParent(canvas, false);
                var panel = (RectTransform)panelObject.transform;
                ResponsiveUI.Area(panel, 0, 0, 1, 1);
                var safe = panelObject.AddComponent<ScreenSafeAreaUI>();
                Call(safe, "Awake");
                foreach (Vector4 inset in new[] { Vector4.zero, new Vector4(.04f, 0, 1, 1), new Vector4(0, 0, .96f, 1), new Vector4(.04f, .03f, .96f, .97f) })
                {
                    if (inset == Vector4.zero) ResponsiveUI.Area(panel, 0, 0, 1, 1);
                    else ResponsiveUI.Area(panel, inset.x, inset.y, inset.z, inset.w);
                    Call(safe, "FitBackground");
                    var background = (RectTransform)panel.Find("Full Screen Menu Background");
                    var a = new Vector3[4]; var b = new Vector3[4];
                    canvas.GetWorldCorners(a); background.GetWorldCorners(b);
                    Check(Enumerable.Range(0, 4).All(i => Vector3.Distance(a[i], b[i]) < .01f), "Background covers canvas: " + inset);
                    Check(!background.GetComponent<Image>().raycastTarget, "Background does not block controls");
                }
                prefab = PrefabUtility.LoadPrefabContents("Assets/_Project/Prefabs/UI/Production/Production Item Card 1.prefab");
                var card = prefab.GetComponentInChildren<ProductionCardUI>(true);
                ((RectTransform)card.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 480);
                Call(card, "LateUpdate");
                Button minus = (Button)typeof(ProductionCardUI).GetField("decreaseQuantityButton", Flags).GetValue(card);
                Button plus = (Button)typeof(ProductionCardUI).GetField("increaseQuantityButton", Flags).GetValue(card);
                Check(((RectTransform)minus.transform).anchoredPosition.x < ((RectTransform)plus.transform).anchoredPosition.x, "Minus is left of plus");
                item = ScriptableObject.CreateInstance<ComponentSO>();
                string description = string.Join(" ", Enumerable.Repeat("Full description must remain readable when the card is too small.", 30));
                typeof(ComponentSO).GetField("description", Flags).SetValue(item, description);
                foreach (Vector2 size in new[] { new Vector2(2400,1080), new Vector2(1920,1080), new Vector2(1280,720) })
                {
                    canvas.sizeDelta = size;
                    var popupObject = new GameObject("Popup Test", typeof(RectTransform));
                    popupObject.transform.SetParent(canvas, false);
                    ResponsiveUI.Area((RectTransform)popupObject.transform, 0, 0, 1, 1);
                    var popup = popupObject.AddComponent<ProductionRecipePopupUI>();
                    typeof(ProductionRecipePopupUI).GetField("fontSize", Flags).SetValue(popup, 24f);
                    typeof(ProductionRecipePopupUI).GetMethod("Build", Flags).Invoke(popup, new object[] { item, 1 });
                    TMP_Text text = popup.GetComponentsInChildren<TMP_Text>().Single(t => t.name == "Full Description");
                    ScrollRect scroll = popup.GetComponentInChildren<ScrollRect>();
                    Check(text.text == description && !text.enableAutoSizing && text.overflowMode == TextOverflowModes.Overflow, "Full description retained " + size);
                    Check(text.rectTransform.rect.height >= text.GetPreferredValues(description, text.rectTransform.rect.width, Mathf.Infinity).y, "Description fits its row " + size);
                    Check(scroll.vertical && scroll.content.rect.height > scroll.viewport.rect.height, "Long description scrolls " + size);
                    UnityEngine.Object.DestroyImmediate(popupObject);
                }
            }
            catch (Exception e) { results.Add("FAIL " + e); }
            finally
            {
                if (item != null) UnityEngine.Object.DestroyImmediate(item);
                if (prefab != null) PrefabUtility.UnloadPrefabContents(prefab);
                EditorSceneManager.ClosePreviewScene(scene);
            }
            Directory.CreateDirectory("Temp/SeptemberUIValidation");
            File.WriteAllLines("Temp/SeptemberUIValidation/results.txt", results);
        }
    }
}
#endif
