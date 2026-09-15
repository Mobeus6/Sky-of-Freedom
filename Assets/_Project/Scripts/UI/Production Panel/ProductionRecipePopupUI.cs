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
        private readonly List<StockRow> rows = new List<StockRow>();

        private class StockRow
        {
            public string Id;
            public int Required;
            public TMP_Text Text;
        }

        public static void Show(ProductionCardUI source, IProducible item,
            int quantity, TMP_FontAsset labelFont)
        {
            Canvas canvas = source.GetComponentInParent<Canvas>();
            if (canvas == null) return;
            if (current != null) current.Close();
            var root = new GameObject("Production Recipe Popup", typeof(RectTransform));
            root.transform.SetParent(canvas.rootCanvas.transform, false);
            Stretch((RectTransform)root.transform);
            current = root.AddComponent<ProductionRecipePopupUI>();
            current.owner = source;
            current.font = labelFont;
            float width = ((RectTransform)canvas.rootCanvas.transform).rect.width;
            current.fontSize = Mathf.Clamp(width / 60f, 16f, 36f);
            current.Build(item, quantity);
        }

        public static void CloseFor(ProductionCardUI source)
        {
            if (current != null && current.owner == source) current.Close();
        }

        private void Close()
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
            var backdrop = Box("Backdrop", transform, Vector2.zero, Vector2.one,
                new Color(0, 0, 0, 0.7f));
            backdrop.gameObject.AddComponent<Button>().onClick.AddListener(Close);
            var panel = Box("Recipe", backdrop, new Vector2(0.14f, 0.1f),
                new Vector2(0.86f, 0.9f), new Color(0.10f, 0.14f, 0.17f));
            // Consume clicks on the panel so they do not close through the backdrop.
            panel.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;

            var heading = Box("Heading", panel, new Vector2(0.04f, 0.81f),
                new Vector2(0.84f, 0.97f), Color.clear);
            Text(item.Name + " — Recipe ×" + quantity, heading,
                fontSize * 1.15f, TextAlignmentOptions.MidlineLeft);
            var close = Box("Close", panel, new Vector2(0.87f, 0.84f),
                new Vector2(0.97f, 0.97f), new Color(0.35f, 0.18f, 0.2f));
            close.gameObject.AddComponent<Button>().onClick.AddListener(Close);
            Text("X", close, fontSize, TextAlignmentOptions.Center);

            var subtitle = Box("Requirements", panel, new Vector2(0.04f, 0.70f),
                new Vector2(0.96f, 0.81f), Color.clear);
            string requirement = ProductionManager.MeetsFactoryLevel(item)
                ? "In stock / Required"
                : "Requires Factory Lv. " + Mathf.Max(1, item.Tier) + "  |  In stock / Required";
            Text(requirement, subtitle, fontSize * 0.85f, TextAlignmentOptions.MidlineLeft);

            var viewport = Box("Viewport", panel, new Vector2(0.04f, 0.05f),
                new Vector2(0.96f, 0.68f), new Color(0, 0, 0, 0.08f));
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var contentObject = new GameObject("Rows", typeof(RectTransform));
            contentObject.transform.SetParent(viewport, false);
            var content = (RectTransform)contentObject.transform;
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero;
            scroll.content = content;

            if (!ProductionRecipeProcessor.TryGetRequirements(item, quantity, out var required))
            {
                content.sizeDelta = new Vector2(0, fontSize * 3);
                Text("Recipe unavailable", content, fontSize, TextAlignmentOptions.Center);
                return;
            }
            float rowHeight = fontSize * 3.5f;
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
                row.anchoredPosition = new Vector2(0, -index * rowHeight);
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
                index++;
            }
            content.sizeDelta = new Vector2(0, Mathf.Max(1, index) * rowHeight);
            if (index == 0)
                Text("No ingredients required", content, fontSize, TextAlignmentOptions.Center);
            RefreshStock();
        }

        private void Update()
        {
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
                row.Text.color = stock >= row.Required
                    ? new Color(0.5f, 0.8f, 0.55f) : new Color(1f, 0.4f, 0.4f);
            }
        }
    }
}
