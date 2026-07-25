using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuLoader : MonoBehaviour
{
    private void Start()
    {
        SceneManager.LoadScene("Game");
    }
}