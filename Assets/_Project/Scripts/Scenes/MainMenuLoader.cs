using System;
using SkyOfFreedom.Managers;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuLoader : MonoBehaviour
{
    private bool isOpeningGame;

    public void PlayGame()
    {
        if (isOpeningGame ||
            GameManager.Instance == null ||
            !GameManager.Instance.IsGameReady)
        {
            return;
        }

        try
        {
            isOpeningGame = true;
            AsyncOperation operation = SceneManager.LoadSceneAsync("Game");

            if (operation == null)
            {
                isOpeningGame = false;
                Debug.LogError("Could not start loading the Game scene.", this);
            }
            else
            {
                operation.completed += OnGameLoaded;
            }
        }
        catch (Exception exception)
        {
            isOpeningGame = false;
            Debug.LogException(exception, this);
        }
    }

    private void OnGameLoaded(AsyncOperation operation)
    {
        isOpeningGame = false;
    }
}
    