using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Menu Panels")]
    public GameObject mainPanel;
    public GameObject optionsPanel;

    private void Start()
    {
        VolumeManager.Initialize();

        // Check if we were sent here specifically to open the Options panel
        if (PlayerPrefs.GetInt("OpenOptionsOnLoad", 0) == 1)
        {
            PlayerPrefs.SetInt("OpenOptionsOnLoad", 0); // Reset for next time
            ShowOptions();
        }
        else
        {
            ShowMainMenu();
        }
    }

    public void PlayGame()
    {
        // Hide the menu, lock the cursor, and unpause the game
        gameObject.SetActive(false);
        Time.timeScale = 1f; 
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // CRITICAL: Ensure the UIManager knows we are no longer paused
        // This fixes the issue where you couldn't pick up items or switch weapons after starting
        UIManager.SetPaused(false);
    }

    public void ShowOptions()
    {
        mainPanel.SetActive(false);
        optionsPanel.SetActive(true);
    }

    public void ShowMainMenu()
    {
        mainPanel.SetActive(true);
        optionsPanel.SetActive(false);
        
        // Pause the game and show the mouse cursor
        Time.timeScale = 0f; 
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game Activated!");
        Application.Quit();
    }
}