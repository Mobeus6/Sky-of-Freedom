using UnityEngine;
using UnityEngine.UI;

public class MenuButton : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject highlight;

    private MenuManager menuManager;

    public GameObject Panel => panel;
    public GameObject Highlight => highlight;

    public void Initialize(MenuManager manager)
    {
        menuManager = manager;

        GetComponent<Button>().onClick.RemoveAllListeners();
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        menuManager.Toggle(this);
    }
}