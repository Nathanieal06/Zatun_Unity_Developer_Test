using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles the Retry button click on the Death Screen.
/// Restarts the level immediately.
/// </summary>
public class RetryButton : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("The panel that shows when the player dies.")]
    public GameObject deathPanel;
    
    [Tooltip("The panel for your Main Menu.")]
    public GameObject mainMenuPanel;

    /// <summary>
    /// Called when the Retry button is clicked.
    /// This restarts the entire level (Health, Zombies, etc).
    /// </summary>
    public void OnRetryClicked()
    {
        // 1. Reset Time and Cursor so the game isn't frozen when it reloads
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 2. RESTART THE GAME (Reload current scene)
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        
        Debug.Log("[RetryButton] Game Restarted.");
    }
}
