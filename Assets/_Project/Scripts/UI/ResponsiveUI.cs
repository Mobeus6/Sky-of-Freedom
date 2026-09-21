using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkyOfFreedom.UI
{
    // Shared geometry helpers. All sizes are reference-canvas units.
    public static class ResponsiveUI
    {
        public static RectTransform Child(Transform parent, string name)
        {
            if (parent == null) return null;
            foreach (Transform child in parent)
                if (child.name.Trim() == name) return child as RectTransform;
            return null;
        }

        public static void Area(RectTransform rect, float left, float bottom, float right, float top,
            float insetLeft = 0, float insetBottom = 0, float insetRight = 0, float insetTop = 0)
        {
            if (rect == null) return;
            rect.anchorMin = new Vector2(left, bottom);
            rect.anchorMax = new Vector2(right, top);
            rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = new Vector2(insetLeft, insetBottom);
            rect.offsetMax = new Vector2(-insetRight, -insetTop);
            rect.localScale = Vector3.one;
        }

        public static void Top(RectTransform rect, float left, float top, float width, float height)
        {
            if (rect == null) return;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(left, -top);
            rect.sizeDelta = new Vector2(Mathf.Max(1, width), Mathf.Max(1, height));
            rect.localScale = Vector3.one;
        }

        public static void Label(TMP_Text text, float min, float max, int lines = 2)
        {
            if (text == null) return;
            text.enableAutoSizing = true;
            text.fontSizeMin = min;
            text.fontSizeMax = max;
            text.textWrappingMode = lines == 1 ? TextWrappingModes.NoWrap : TextWrappingModes.Normal;
            text.maxVisibleLines = lines;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
        }

        public static void Place(Component item, Transform parent, float x, float y, float width, float height)
        {
            if (item == null) return;
            if (item.transform.parent != parent) item.transform.SetParent(parent, false);
            Top(item.transform as RectTransform, x, y, width, height);
        }

        // Wrap the existing content, retaining object references and business scripts.
        public static ScrollRect Wrap(RectTransform content)
        {
            if (content == null) return null;
            ScrollRect existing = content.parent != null ? content.parent.GetComponent<ScrollRect>() : null;
            if (existing != null && existing.content == content) return existing;
            Transform parent = content.parent;
            int index = content.GetSiblingIndex();
            var go = new GameObject(content.name.Trim() + " Scroll", typeof(RectTransform), typeof(Image),
                typeof(RectMask2D), typeof(ScrollRect));
            go.transform.SetParent(parent, false);
            go.transform.SetSiblingIndex(index);
            var viewport = (RectTransform)go.transform;
            viewport.anchorMin = content.anchorMin;
            viewport.anchorMax = content.anchorMax;
            viewport.pivot = content.pivot;
            viewport.sizeDelta = content.sizeDelta;
            viewport.anchoredPosition = content.anchoredPosition;
            go.GetComponent<Image>().color = Color.clear;
            content.SetParent(viewport, false);
            var scroll = go.GetComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            ConfigureScroll(scroll);
            return scroll;
        }

        public static void ConfigureScroll(ScrollRect scroll)
        {
            if (scroll == null || scroll.content == null) return;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 35;
            if (scroll.horizontalScrollbar != null) scroll.horizontalScrollbar.gameObject.SetActive(false);
            scroll.horizontalScrollbar = null;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            if (scroll.viewport != null && scroll.viewport != scroll.transform)
                Area(scroll.viewport, 0, 0, 1, 1, 0, 0, 12, 0);
            RectTransform content = scroll.content;
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1);
            content.sizeDelta = new Vector2(0, content.sizeDelta.y);
            content.anchoredPosition = Vector2.zero;
        }

        public static void VerticalList(RectTransform content, float rowHeight)
        {
            var layout = content.GetComponent<VerticalLayoutGroup>();
            if (layout == null) layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 6;
            layout.padding = new RectOffset(6, 6, 6, 12);
            foreach (Transform child in content)
            {
                var element = child.GetComponent<LayoutElement>();
                if (element == null) element = child.gameObject.AddComponent<LayoutElement>();
                element.minHeight = element.preferredHeight = rowHeight;
                foreach (TMP_Text text in child.GetComponentsInChildren<TMP_Text>(true))
                {
                    Area(text.rectTransform, 0, 0, 1, 1, 36, 4, 6, 4);
                    Label(text, 16, 20);
                }
            }
            var fitter = content.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }
}
