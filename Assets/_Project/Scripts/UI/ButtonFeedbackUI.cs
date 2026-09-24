using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SkyOfFreedom.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class ButtonFeedbackUI : MonoBehaviour, IPointerDownHandler, ISubmitHandler
    {
        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) Press();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            Press();
        }

        private void Press()
        {
            if (button == null || !button.IsActive() || !button.IsInteractable()) return;
            Graphic graphic = button.targetGraphic;
            UIActionFlash.Show(graphic != null ? graphic.rectTransform : transform as RectTransform,
                new Color(0.75f, 0.88f, 1f, 0.22f), 0.18f, graphic as Image);
        }

        public static void Success(Component owner)
        {
            if (owner == null) return;
            RectTransform target = owner.transform as RectTransform;
            if (target == null) return;
            UIActionFlash.Show(target, new Color(0.25f, 1f, 0.5f, 0.16f), 0.45f,
                target.GetComponent<Image>(), true);
        }

        public static void Error(Component owner)
        {
            if (owner == null) return;
            RectTransform target = owner.transform as RectTransform;
            GameObject selected = EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject : null;
            if (selected != null && (selected.transform == owner.transform ||
                selected.transform.IsChildOf(owner.transform)) &&
                selected.TryGetComponent<Button>(out var selectedButton))
            {
                target = selectedButton.targetGraphic != null
                    ? selectedButton.targetGraphic.rectTransform : selected.transform as RectTransform;
            }
            if (target != null)
                UIActionFlash.Show(target, new Color(1f, 0.2f, 0.2f, 0.25f), 0.35f,
                    target.GetComponent<Image>(), true);
        }
    }
}
