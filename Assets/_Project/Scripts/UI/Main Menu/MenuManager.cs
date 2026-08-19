using UnityEngine;

public class MenuManager : MonoBehaviour
{
    [Header("Main Menus")]
    [SerializeField]
    private MenuButton[] buttons;

    [Header("Additional Panels")]
    [SerializeField]
    private GameObject[] additionalPanels;

    private MenuButton currentButton;

    private void Awake()
    {
        foreach (MenuButton button in buttons)
        {
            if (button == null)
            {
                continue;
            }

            button.Initialize(this);

            Hide(button.Panel);

            if (button.ExtraPanel != null)
            {
                Hide(button.ExtraPanel);
            }

            if (button.Highlight != null)
            {
                button.Highlight.SetActive(false);
            }
        }

        foreach (GameObject panel in additionalPanels)
        {
            Hide(panel);
        }

        currentButton = null;
    }

    private void Show(GameObject panel)
    {
        if (panel == null)
        {
            return;
        }

        CanvasGroup group =
            panel.GetComponent<CanvasGroup>();

        if (group == null)
        {
            Debug.LogError(
                $"{panel.name} doesn't have CanvasGroup.",
                panel);

            return;
        }

        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;
    }

    private void Hide(GameObject panel)
    {
        if (panel == null)
        {
            return;
        }

        CanvasGroup group =
            panel.GetComponent<CanvasGroup>();

        if (group == null)
        {
            Debug.LogError(
                $"{panel.name} doesn't have CanvasGroup.",
                panel);

            return;
        }

        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    private void HideAllMainMenus()
    {
        foreach (MenuButton button in buttons)
        {
            if (button == null)
            {
                continue;
            }

            Hide(button.Panel);

            if (button.ExtraPanel != null)
            {
                Hide(button.ExtraPanel);
            }

            if (button.Highlight != null)
            {
                button.Highlight.SetActive(false);
            }
        }
    }

    private void HideAllAdditionalPanels()
    {
        foreach (GameObject panel in additionalPanels)
        {
            Hide(panel);
        }
    }

    public void Toggle(MenuButton selectedButton)
    {
        if (selectedButton == null)
        {
            return;
        }

        // Повторне натискання на ту саму кнопку
        // закриває абсолютно всі меню.
        if (currentButton == selectedButton)
        {
            CloseAll();
            return;
        }

        // При відкритті будь-якого нового меню
        // закриваємо всі основні та додаткові панелі.
        HideAllMainMenus();
        HideAllAdditionalPanels();

        Show(selectedButton.Panel);

        if (selectedButton.ExtraPanel != null)
        {
            Show(selectedButton.ExtraPanel);
        }

        if (selectedButton.Highlight != null)
        {
            selectedButton.Highlight.SetActive(true);
        }

        currentButton = selectedButton;
    }

    public void CloseAll()
    {
        HideAllMainMenus();
        HideAllAdditionalPanels();

        currentButton = null;
    }
}