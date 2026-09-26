using System.Collections.Generic;
using SkyOfFreedom.Data;
using SkyOfFreedom.Managers;
using SkyOfFreedom.Production;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkyOfFreedom.UI
{
    // Created under the existing Canvas; no scene or prefab references are required.
    public class ProductionRecipePopupUI : MonoBehaviour
    {
        private static ProductionRecipePopupUI current;
        private ProductionCardUI owner;
        private TMP_FontAsset font;
        private float fontSize;
        private float nextRefresh;
        private PanelFadeUI fade;
        private bool closing;
        private readonly List<StockRow> rows = new List<StockRow>();

        private class StockRow
        {
            public string Id;
            public int Required;
            public TMP_Text Text;
            public Button Purchase;
        }

        public static void Show(ProductionCardUI source, IProducible item,
            int quantity, TMP_FontAsset labelFont)
        {
            Canvas canvas = source.GetComponentInParent<Canvas>();
            if (canvas == null) return;
            // Replacing a recipe must not leave two modal backdrops visible.
            if (current != null) current.CloseImmediately();
            var root = new GameObject("Production Recipe Popup", typeof(RectTransform));
            root.transform.SetParent(canvas.rootCanvas.transform, false);
            Stretch((RectTransform)root.transform);
            current = root.AddComponent<ProductionRecipePopupUI>();
            current.fade = root.AddComponent<PanelFadeUI>();
            current.fade.SetVisible(false, false);
            current.owner = source;
            current.font = labelFont;
            Rect screen = ((RectTransform)canvas.rootCanvas.transform).rect;
            current.fontSize = Mathf.Clamp(Mathf.Min(screen.width / 65f, screen.height / 32f), 12f, 30f);
            current.Build(item, quantity);
            current.fade.SetVisible(true);
        }

        public static void CloseFor(ProductionCardUI source)
        {
            if (current != null && current.owner == source) current.Close();
        }

        private void Close()
        {
            if (closing) return;
            closing = true;
            if (fade == null || !isActiveAndEnabled)
            {
                CloseImmediately();
                return;
            }
            fade.SetVisible(false);
            if (!fade.IsAnimating) CloseImmediately();
        }

        private void CloseImmediately()
        {
            if (current == this) current = null;
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (current == this) current = null;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private RectTransform Box(string label, Transform parent, Vector2 min,
            Vector2 max, Color color)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = color;
            return rect;
        }

        private TMP_Text Text(string value, Transform parent, float size,
            TextAlignmentOptions alignment)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            Stretch((RectTransform)go.transform);
            var text = go.GetComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.text = value;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            text.enableAutoSizing = true;
            text.fontSizeMin = size * 0.7f;
            text.fontSizeMax = size;
            return text;
        }

        private void Build(IProducible item, int quantity)
        {
            bool validRecipe = ProductionRecipeProcessor.TryGetRequirements(item, quantity, out var required);
            int count = validRecipe ? required.Count : 1;
            Rect screen = ((RectTransform)transform).rect;
            float rowHeight = fontSize * 2.8f;
            float height = Mathf.Min(screen.height * 0.78f,
                fontSize * (string.IsNullOrWhiteSpace(item.Description) ? 7f : 12f) + Mathf.Max(1, count) * rowHeight);
            float width = Mathf.Min(screen.width * 0.9f,
                Mathf.Max(screen.width * 0.48f, fontSize * 30f));
            var backdrop = Box("Backdrop", transform, Vector2.zero, Vector2.one,
                new Color(0, 0, 0, 0.6f));
            backdrop.gameObject.AddComponent<Button>().onClick.AddListener(Close);
            var safeContent = Box("Safe Content", backdrop, Vector2.zero, Vector2.one, Color.clear);
            safeContent.GetComponent<Image>().raycastTarget = false;
            safeContent.gameObject.AddComponent<ScreenSafeAreaUI>();
            var panel = Box("Recipe", safeContent, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Color(0.105f, 0.14f, 0.16f));
            panel.sizeDelta = new Vector2(width, height);
            var outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.18f, 0.38f, 0.58f);
            outline.effectDistance = new Vector2(1, -1);
            // Consume clicks on the panel so they do not close through the backdrop.
            panel.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;

            var heading = Box("Heading", panel, new Vector2(0, 1),
                new Vector2(1, 1), Color.clear);
            heading.offsetMin = new Vector2(fontSize, -fontSize * 3);
            heading.offsetMax = new Vector2(-fontSize * 4, -fontSize * 0.5f);
            Text(item.Name + " ×" + quantity, heading,
                fontSize * 1.1f, TextAlignmentOptions.MidlineLeft);
            var close = Box("Close", panel, Vector2.one,
                Vector2.one, Color.white);
            close.pivot = Vector2.one;
            close.sizeDelta = Vector2.one * fontSize * 2.4f;
            close.anchoredPosition = new Vector2(-fontSize * 0.5f, -fontSize * 0.5f);
            Button closeButton = close.gameObject.AddComponent<Button>();
            closeButton.onClick.AddListener(Close);
            CloseButtonStyle.Apply(closeButton);
            var divider = Box("Blue Divider", panel, new Vector2(0, 1),
                Vector2.one, new Color(0.18f, 0.40f, 0.66f));
            divider.offsetMin = new Vector2(fontSize, -fontSize * 3.25f - 2);
            divider.offsetMax = new Vector2(-fontSize, -fontSize * 3.25f);

            string requirement = ProductionManager.MeetsFactoryLevel(item)
                ? "In stock / Required"
                : "Requires Factory Lv. " + Mathf.Max(1, item.Tier) + "  |  In stock / Required";

            var viewport = Box("Viewport", panel, Vector2.zero,
                Vector2.one, new Color(0, 0, 0, 0.08f));
            viewport.offsetMin = new Vector2(fontSize, fontSize);
            viewport.offsetMax = new Vector2(-fontSize, -fontSize * 3.5f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 35f;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var contentObject = new GameObject("Rows", typeof(RectTransform));
            contentObject.transform.SetParent(viewport, false);
            var content = (RectTransform)contentObject.transform;
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero;
            scroll.content = content;

            float offset = 0f;
            if (!string.IsNullOrWhiteSpace(item.Description))
            {
                TMP_Text description = Text(item.Description, content, fontSize * .9f, TextAlignmentOptions.TopLeft);
                description.name = "Full Description";
                description.enableAutoSizing = false;
                description.richText = false;
                description.textWrappingMode = TextWrappingModes.Normal;
                description.overflowMode = TextOverflowModes.Overflow;
                description.maxVisibleLines = int.MaxValue;
                float textHeight = description.GetPreferredValues(item.Description, width - 2f * fontSize, Mathf.Infinity).y;
                ResponsiveUI.Top(description.rectTransform, 0, 0, width - 2f * fontSize, textHeight + 4);
                offset = textHeight + fontSize;
            }
            var subtitle = Box("Requirements", content, new Vector2(0, 1), Vector2.one, Color.clear);
            subtitle.pivot = new Vector2(.5f, 1);
            subtitle.anchoredPosition = new Vector2(0, -offset);
            subtitle.sizeDelta = new Vector2(0, fontSize * 1.75f);
            Text(requirement, subtitle, fontSize * .85f, TextAlignmentOptions.MidlineLeft);
            offset += fontSize * 2f;

            if (!validRecipe)
            {
                content.sizeDelta = new Vector2(0, offset + rowHeight);
                TMP_Text unavailable = Text("Recipe unavailable", content, fontSize, TextAlignmentOptions.Center);
                ResponsiveUI.Top(unavailable.rectTransform, 0, offset, width - 2f * fontSize, rowHeight);
                return;
            }
            int index = 0;
            foreach (var entry in required)
            {
                DataSO data = GameManager.Instance.Database.Database.GetData(entry.Key);
                string name = entry.Key;
                Sprite sprite = null;
                if (data is MaterialSO material)
                {
                    name = material.MaterialName;
                    sprite = material.Icon;
                }
                else if (data is IProducible product)
                {
                    name = product.Name;
                    sprite = product.Icon;
                }
                var row = Box("Resource", content, new Vector2(0, 1), Vector2.one,
                    new Color(0.15f, 0.20f, 0.23f));
                row.pivot = new Vector2(0.5f, 1);
                row.sizeDelta = new Vector2(0, rowHeight - 6);
                row.anchoredPosition = new Vector2(0, -offset - index * rowHeight);
                var icon = Box("Icon", row, new Vector2(0.01f, 0.1f),
                    new Vector2(0.13f, 0.9f), Color.white).GetComponent<Image>();
                icon.sprite = sprite;
                icon.preserveAspect = true;
                icon.enabled = sprite != null;
                var nameRect = Box("Name", row, new Vector2(0.16f, 0.08f),
                    new Vector2(0.68f, 0.92f), Color.clear);
                Text(name, nameRect, fontSize, TextAlignmentOptions.MidlineLeft);
                var countRect = Box("Stock", row, new Vector2(0.70f, 0.08f),
                    new Vector2(0.98f, 0.92f), Color.clear);
                rows.Add(new StockRow { Id = entry.Key, Required = entry.Value,
                    Text = Text("", countRect, fontSize, TextAlignmentOptions.MidlineRight) });
                if (data is MaterialSO purchaseMaterial)
                {
                    Button purchase = row.gameObject.AddComponent<Button>();
                    purchase.targetGraphic = row.GetComponent<Image>();
                    purchase.onClick.AddListener(() =>
                    {
                        if (WarehousePanelUI.TryOpenMaterial(purchaseMaterial)) Close();
                        else PlayerMessageUI.Show(this, "Warehouse is unavailable.");
                    });
                    rows[rows.Count - 1].Purchase = purchase;
                    // The row owns the hit area, including its icon and labels.
                    foreach (Graphic graphic in row.GetComponentsInChildren<Graphic>())
                        graphic.raycastTarget = graphic.gameObject == row.gameObject;
                }
                index++;
            }
            content.sizeDelta = new Vector2(0, offset + Mathf.Max(1, index) * rowHeight);
            if (index == 0)
            {
                TMP_Text empty = Text("No ingredients required", content, fontSize, TextAlignmentOptions.Center);
                ResponsiveUI.Top(empty.rectTransform, 0, offset, width - 2f * fontSize, rowHeight);
            }
            RefreshStock();
        }

        private void Update()
        {
            if (closing)
            {
                if (fade == null || !fade.IsAnimating) CloseImmediately();
                return;
            }
            if (owner == null || !owner.isActiveAndEnabled)
            {
                Close();
                return;
            }
            if (Time.unscaledTime >= nextRefresh)
            {
                nextRefresh = Time.unscaledTime + 0.2f;
                RefreshStock();
            }
        }

        private void RefreshStock()
        {
            var warehouse = GameManager.Instance != null ? GameManager.Instance.Warehouse : null;
            if (warehouse == null) return;
            foreach (StockRow row in rows)
            {
                int stock = warehouse.GetQuantity(row.Id);
                row.Text.text = stock + " / " + row.Required;
                if (row.Purchase != null)
                {
                    row.Purchase.interactable = true;
                }
                row.Text.color = stock >= row.Required
                    ? new Color(0.5f, 0.8f, 0.55f) : new Color(1f, 0.4f, 0.4f);
            }
        }
    }
}
