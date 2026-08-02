using UnityEngine;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private MenuButton[] buttons;

    private MenuButton currentButton;

    private void Awake()
    {
        foreach (MenuButton button in buttons)
        {
            button.Initialize(this);

            Hide(button.Panel);

            if (button.ExtraPanel != null)
                Hide(button.ExtraPanel);

            if (button.Highlight != null)
                button.Highlight.SetActive(false);
        }
    }

    private void Show(GameObject panel)
    {
        if (panel == null)
            return;

        CanvasGroup group = panel.GetComponent<CanvasGroup>();

        if (group == null)
        {
            Debug.LogError($"{panel.name} doesn't have CanvasGroup.");
            return;
        }

        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;
    }

    private void Hide(GameObject panel)
    {
        if (panel == null)
            return;

        CanvasGroup group = panel.GetComponent<CanvasGroup>();

        if (group == null)
        {
            Debug.LogError($"{panel.name} doesn't have CanvasGroup.");
            return;
        }

        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    public void Toggle(MenuButton selectedButton)
    {
        // Закриття поточної вкладки
        if (currentButton == selectedButton)
        {
            Hide(selectedButton.Panel);

            if (selectedButton.ExtraPanel != null)
                Hide(selectedButton.ExtraPanel);

            if (selectedButton.Highlight != null)
                selectedButton.Highlight.SetActive(false);

            currentButton = null;
            return;
        }

        // Ховаємо всі панелі
        foreach (MenuButton button in buttons)
        {
            Hide(button.Panel);

            if (button.ExtraPanel != null)
                Hide(button.ExtraPanel);

            if (button.Highlight != null)
                button.Highlight.SetActive(false);
        }

        // Показуємо потрібні
        Show(selectedButton.Panel);

        if (selectedButton.ExtraPanel != null)
            Show(selectedButton.ExtraPanel);

        if (selectedButton.Highlight != null)
            selectedButton.Highlight.SetActive(true);

        currentButton = selectedButton;
    }
}