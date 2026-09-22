using UnityEngine;

namespace SkyOfFreedom.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class UIPanel : MonoBehaviour
    {
        private CanvasGroup canvasGroup;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        public void Show()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            var fade = GetComponent<PanelFadeUI>();
            if (fade != null) { fade.SetVisible(true); return; }
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        public void Hide()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            var fade = GetComponent<PanelFadeUI>();
            if (fade != null) { fade.SetVisible(false); return; }
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
}
