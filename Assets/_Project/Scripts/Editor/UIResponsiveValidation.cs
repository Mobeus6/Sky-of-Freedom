#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SkyOfFreedom.UI;
using SkyOfFreedom.UI.Contracts;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SkyOfFreedom.EditorTools
{
    public static class UIResponsiveValidation
    {
        private const string Request = "Temp/SkyUIValidation.request";
        private const string Output = "Temp/ResponsiveUIValidation";
        private static readonly List<string> Results = new List<string>();
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        [InitializeOnLoadMethod]
        private static void Schedule()
        {
            EditorApplication.delayCall += RunRequested;
        }

        private static void RunRequested()
        {
            if (!File.Exists(Request)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
            {
                EditorApplication.delayCall += RunRequested;
                return;
            }
            File.Delete(Request);
            Run();
        }

        private static T Field<T>(object obj, string name) where T : class
        {
            return obj.GetType().GetField(name, Flags)?.GetValue(obj) as T;
        }

        private static void Call(object obj, string name)
        {
            obj.GetType().GetMethod(name, Flags)?.Invoke(obj, null);
        }

        private static void Check(bool success, string message)
        {
            Results.Add((success ? "PASS " : "FAIL ") + message);
        }

        [MenuItem("Tools/Sky of Freedom/Validate Responsive UI")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Directory.CreateDirectory(Output);
            Results.Clear();
            Scene scene = default;
            try
            {
                scene = EditorSceneManager.OpenPreviewScene("Assets/_Project/Scenes/Game.unity");
                Canvas canvas = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Canvas>(true))
                    .First(c => c.name == "GameCanvas");
                GameObject production = canvas.GetComponentsInChildren<RectTransform>(true)
                    .First(t => t.name == "ProductionPanel").gameObject;
                WarehousePanelUI warehouse = canvas.GetComponentInChildren<WarehousePanelUI>(true);
                ContractsUI contracts = canvas.GetComponentInChildren<ContractsUI>(true);
                ContractDetailUI detail = canvas.GetComponentInChildren<ContractDetailUI>(true);
                WarehouseInfoPanelUI info = canvas.GetComponentInChildren<WarehouseInfoPanelUI>(true);
                ResponsiveGamePanelUI.Attach(production, ResponsiveGamePanelUI.PanelKind.Production);
                ResponsiveGamePanelUI.Attach(warehouse.gameObject, ResponsiveGamePanelUI.PanelKind.Warehouse);
                ResponsiveGamePanelUI.Attach(contracts.gameObject, ResponsiveGamePanelUI.PanelKind.Contracts);
                Call(detail, "ConfigureLayout");
                Call(info, "ConfigureLayout");
                ProductionPanelUI productionList = production.GetComponentInChildren<ProductionPanelUI>(true);
                Transform productionContent = Field<Transform>(productionList, "content");
                Transform warehouseContent = Field<Transform>(warehouse, "content");
                var productionCards = new List<ProductionCardUI>();
                var warehouseCards = new List<WarehouseCardUI>();
                var contractCards = new List<ContractCardUI>();
                for (int i = 0; i < 5; i++)
                {
                    var card = UnityEngine.Object.Instantiate(Field<ProductionCardUI>(productionList, "cardPrefab"), productionContent);
                    Field<TMP_Text>(card, "nameText").text = "Professional Flight Controller";
                    Field<TMP_Text>(card, "descriptionText").text = "Requires Factory Lv. 4\nHigh-performance flight controller for advanced drones.";
                    Field<TMP_Text>(card, "quantityText").text = "9999";
                    productionCards.Add(card);
                    warehouseCards.Add(UnityEngine.Object.Instantiate(Field<WarehouseCardUI>(warehouse, "cardPrefab"), warehouseContent));
                    contractCards.Add(UnityEngine.Object.Instantiate(Field<ContractCardUI>(contracts, "contractCardPrefab"),
                        Field<Transform>(contracts, "availableContent")));
                }
                foreach (ContractCardUI card in contractCards)
                    Field<TMP_Text>(card, "contractNameText").text = "MANUFACTURE PROFESSIONAL FLIGHT CONTROLLER T4";
                foreach (WarehouseCardUI card in warehouseCards)
                {
                    Field<TMP_Text>(card, "nameText").text = "Professional Flight Controller";
                    Field<TMP_Text>(card, "descriptionText").text = "High-performance component for drone assembly.";
                }
                Field<TMP_Text>(detail, "droneContractNameText").text = "WARDEN DELIVERY CONTRACT";
                Field<TMP_Text>(detail, "droneDescriptionText").text = "Manufacture and deliver Warden drones.";
                Field<TMP_Text>(info, "itemNameText").text = "Silicone Cable";
                Field<TMP_Text>(info, "descriptionText").text = "Flexible insulating material for electrical cables and electronic components.";
                Transform required = Field<Transform>(detail, "droneRequiredComponentsContent");
                var componentCards = new List<ContractComponentUI>();
                for (int i = 0; i < 7; i++)
                {
                    var card = UnityEngine.Object.Instantiate(Field<ContractComponentUI>(detail, "contractComponentPrefab"), required);
                    Field<TMP_Text>(card, "nameText").text = "Standard Brushless Motor";
                    Field<TMP_Text>(card, "quantityText").text = "Quantity 20";
                    componentCards.Add(card);
                }
                foreach (RectTransform content in new[] { productionContent, warehouseContent, required }.Cast<RectTransform>())
                    content.gameObject.SetActive(true);
                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler != null) scaler.enabled = false;
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.transform.position = Vector3.zero;
                canvas.transform.localScale = Vector3.one;
                var sizes = new[] { new Vector2(2400, 1080), new Vector2(1920, 1080), new Vector2(1280, 720), new Vector2(1024, 768) };
                foreach (Vector2 pixels in sizes)
                {
                    float scale = Mathf.Sqrt(pixels.x / 1920f * pixels.y / 1080f);
                    ((RectTransform)canvas.transform).sizeDelta = pixels / scale;
                    foreach (CanvasGroup group in canvas.GetComponentsInChildren<CanvasGroup>(true))
                        group.alpha = 1;
                    RectTransform miniPanels = ResponsiveUI.Child(canvas.transform, "Mini Panels");
                    if (miniPanels != null) miniPanels.gameObject.SetActive(false);
                    Field<GameObject>(contracts, "availableView").SetActive(true);
                    Field<GameObject>(contracts, "inProgressView").SetActive(false);
                    Field<GameObject>(contracts, "completedView").SetActive(false);
                    Field<GameObject>(detail, "droneContractInfoCard").SetActive(true);
                    Field<GameObject>(detail, "componentContractInfoCard").SetActive(false);
                    Field<GameObject>(info, "commonPanel").SetActive(true);
                    Field<GameObject>(info, "materialPanel").SetActive(true);
                    Field<GameObject>(info, "componentPanel").SetActive(false);
                    Field<GameObject>(info, "dronePanel").SetActive(false);
                    Settle(canvas, detail, info, productionCards, warehouseCards, contractCards, componentCards);
                    string tag = pixels.x + "x" + pixels.y;
                    RectTransform sidebar = ResponsiveUI.Child(contracts.transform, "ContractsMenu");
                    RectTransform detailRect = detail.transform as RectTransform;
                    Bounds leftBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(contracts.transform, sidebar);
                    Bounds rightBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(contracts.transform, detailRect);
                    // Bounds of descendants may include offscreen scroll content; compare panel corners only.
                    Check(sidebar.offsetMax.x <= detailRect.offsetMin.x, tag + " contract panes do not overlap");
                    foreach (ProductionCardUI card in productionCards)
                    {
                        RectTransform plus = Field<Button>(card, "increaseQuantityButton").transform as RectTransform;
                        RectTransform minus = Field<Button>(card, "decreaseQuantityButton").transform as RectTransform;
                        Check(plus.rect.width >= 64 && plus.rect.height >= 64 && minus.rect.width >= 64,
                            tag + " quantity touch targets >= 64 canvas units");
                        TMP_Text text = Field<TMP_Text>(card, "descriptionText");
                        var corners = new Vector3[4]; text.rectTransform.GetWorldCorners(corners);
                        float descriptionBottom = card.transform.InverseTransformPoint(corners[0]).y;
                        plus.GetWorldCorners(corners);
                        float buttonTop = card.transform.InverseTransformPoint(corners[1]).y;
                        Check(descriptionBottom >= buttonTop, tag + " description above quantity buttons");
                    }
                    ScrollRect scroll = required.parent.GetComponent<ScrollRect>();
                    foreach (int count in new[] { 1, 4, 7 })
                    {
                        for (int i = 0; i < componentCards.Count; i++) componentCards[i].gameObject.SetActive(i < count);
                        Canvas.ForceUpdateCanvases();
                        LayoutRebuilder.ForceRebuildLayoutImmediate(required as RectTransform);
                        scroll.verticalNormalizedPosition = 0;
                        Canvas.ForceUpdateCanvases();
                        var corners = new Vector3[4];
                        ((RectTransform)componentCards[count - 1].transform).GetWorldCorners(corners);
                        float bottom = scroll.viewport.InverseTransformPoint(corners[0]).y;
                        Check(bottom >= scroll.viewport.rect.yMin - 1, tag + " last component reachable: " + count);
                    }
                    scroll.verticalNormalizedPosition = 1;
                    if (pixels.x == 1920)
                    {
                        Render(canvas, scene, production, "production");
                        Render(canvas, scene, warehouse.gameObject, "warehouse");
                        Render(canvas, scene, contracts.gameObject, "contracts");
                    }
                }
                // Existing menu routing must open, rather than toggle off, its current destination.
                MenuManager menu = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MenuManager>(true)).FirstOrDefault();
                if (menu != null)
                {
                    Call(menu, "Awake");
                    Check(menu.OpenPanel(warehouse.gameObject), "Warehouse registered in MenuManager");
                    Check(menu.OpenPanel(warehouse.gameObject) && warehouse.GetComponent<CanvasGroup>().alpha == 1,
                        "Repeated Warehouse deep link stays open");
                    Check(production.GetComponent<CanvasGroup>().blocksRaycasts == false, "Production does not intercept Warehouse clicks");
                }
            }
            catch (Exception exception)
            {
                Results.Add("FAIL " + exception);
            }
            finally
            {
                if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
                File.WriteAllLines(Path.Combine(Output, "report.txt"), Results);
                Debug.Log("Responsive UI validation: " + Results.Count(r => r.StartsWith("PASS")) +
                    " passed, " + Results.Count(r => r.StartsWith("FAIL")) + " failed. " + Output);
            }
        }

        private static void Settle(Canvas canvas, ContractDetailUI detail, WarehouseInfoPanelUI info,
            List<ProductionCardUI> production, List<WarehouseCardUI> warehouse,
            List<ContractCardUI> contracts, List<ContractComponentUI> components)
        {
            for (int pass = 0; pass < 4; pass++)
            {
                foreach (ResponsiveGamePanelUI panel in canvas.GetComponentsInChildren<ResponsiveGamePanelUI>(true)) Call(panel, "LateUpdate");
                Call(detail, "LateUpdate"); Call(info, "LateUpdate");
                foreach (ResponsiveGridUI grid in canvas.GetComponentsInChildren<ResponsiveGridUI>(true)) grid.Refresh();
                Canvas.ForceUpdateCanvases();
                foreach (var card in production) Call(card, "LateUpdate");
                foreach (var card in warehouse) Call(card, "LateUpdate");
                foreach (var card in contracts) Call(card, "LateUpdate");
                foreach (var card in components) Call(card, "LateUpdate");
            }
        }

        private static void Render(Canvas canvas, Scene scene, GameObject panel, string name)
        {
            foreach (Transform child in panel.transform.parent)
            {
                CanvasGroup group = child.GetComponent<CanvasGroup>();
                if (group != null) group.alpha = child.gameObject == panel ? 1 : 0;
            }
            GameObject cameraObject = new GameObject("UI Validation Camera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.scene = scene;
            camera.orthographic = true;
            camera.orthographicSize = ((RectTransform)canvas.transform).rect.height / 2;
            camera.transform.position = new Vector3(0, 0, -100);
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 200;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.04f, .07f, .1f);
            camera.cullingMask = 1 << canvas.gameObject.layer;
            var texture = new RenderTexture(1920, 1080, 24);
            camera.targetTexture = texture;
            RenderTexture previous = RenderTexture.active;
            try
            {
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = texture;
                var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                image.Apply();
                File.WriteAllBytes(Path.Combine(Output, name + ".png"), image.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(image);
            }
            finally
            {
                RenderTexture.active = previous;
                camera.targetTexture = null;
                texture.Release();
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }
    }
}
#endif
