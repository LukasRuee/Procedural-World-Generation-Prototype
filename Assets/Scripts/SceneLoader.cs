using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    /// <summary>
    /// Load a scene by its build index
    /// </summary>
    /// <param name="id"></param>
    public void LoadSceneById(int id)
    {
        SceneManager.LoadScene(id);
    }
    /// <summary>
    /// Load a scene by its name
    /// </summary>
    /// <param name="sceneName"></param>
    public void LoadSceneByName(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
    /// <summary>
    /// Reloads current scene
    /// </summary>
    public void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    /// <summary>
    /// Load the next scene in the build order
    /// </summary>
    public void LoadNextScene()
    {
        int activeSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = activeSceneIndex + 1;

        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            nextSceneIndex = 0;
            SceneManager.LoadScene(nextSceneIndex);
        }
    }
    /// <summary>
    /// Exits the application
    /// </summary>
    public void ExitGame()
    {
        Application.Quit();
    }
}
