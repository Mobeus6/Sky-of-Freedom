using UnityEngine;
using UnityEngine.Serialization;

public class MenuManager : MonoBehaviour
{
    [Header("Main Menus")]
    [SerializeField]
    private MenuButton[] buttons;

    [Header("Additional Panels")]
    [SerializeField]
    private GameObject[] additionalPanels;

    [Header("Panel animation")]
    [FormerlySerializedAs("animateProduction")]
    [SerializeField] private bool animatePanels = true;
    [FormerlySerializedAs("productionShowDuration")]
    [SerializeField, Min(0f)] private float panelShowDuration = .18f;
    [FormerlySerializedAs("productionHideDuration")]
    [SerializeField, Min(0f)] private float panelHideDuration = .12f;
    private bool initialized;
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
        initialized = true;
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

        var fade = GetPanelFade(panel);
        if (fade != null)
        {
            fade.SetVisible(true, initialized && animatePanels);
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

        var fade = GetPanelFade(panel);
        if (fade != null)
        {
            fade.SetVisible(false, initialized && animatePanels);
            return;
        }
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    private SkyOfFreedom.UI.PanelFadeUI GetPanelFade(GameObject panel)
    {
        var fade = panel.GetComponent<SkyOfFreedom.UI.PanelFadeUI>();
        if (fade == null) fade = panel.AddComponent<SkyOfFreedom.UI.PanelFadeUI>();
        fade.Configure(animatePanels ? panelShowDuration : 0f,
            animatePanels ? panelHideDuration : 0f);
        return fade;
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

    // Unlike Toggle, a deep link must not close an already open destination.
    public bool OpenPanel(GameObject target)
    {
        if (target == null) return false;
        foreach (MenuButton button in buttons)
        {
            if (button == null || button.Panel == null) continue;
            if (target != button.Panel && !target.transform.IsChildOf(button.Panel.transform)) continue;
            if (currentButton != button) Toggle(button);
            else Show(button.Panel);
            return true;
        }
        return false;
    }
}
