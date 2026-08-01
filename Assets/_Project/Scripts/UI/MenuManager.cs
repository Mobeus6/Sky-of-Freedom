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

            button.Panel.SetActive(false);

            if (button.Highlight != null)
                button.Highlight.SetActive(false);
        }
    }

    public void Toggle(MenuButton selectedButton)
    {
        if (currentButton == selectedButton)
        {
            selectedButton.Panel.SetActive(false);

            if (selectedButton.Highlight != null)
                selectedButton.Highlight.SetActive(false);

            currentButton = null;
            return;
        }

        foreach (MenuButton button in buttons)
        {
            button.Panel.SetActive(false);

            if (button.Highlight != null)
                button.Highlight.SetActive(false);
        }

        selectedButton.Panel.SetActive(true);

        if (selectedButton.Highlight != null)
            selectedButton.Highlight.SetActive(true);

        currentButton = selectedButton;
    }
}