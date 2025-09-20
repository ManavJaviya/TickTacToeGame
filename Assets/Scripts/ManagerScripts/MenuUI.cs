using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using Unity.Netcode;

public class MenuUI : MonoBehaviour
{
    [Header("UI Panel")]
    [Tooltip("The parent GameObject for the entire pause menu panel.")]
    [SerializeField] private GameObject pauseMenuPanel;

    [Header("Buttons")]
    [Tooltip("The button that will resume the game.")]
    [SerializeField] private Button resumeButton;

    [Tooltip("The button that will restart the current level.")]
    [SerializeField] private Button restartButton;

    [Tooltip("The button that will exit the application.")]
    [SerializeField] private Button exitButton;

    private void Awake()
    {
        // Wire up the button functions. When a button is clicked, it will call the specified method.
        resumeButton.onClick.AddListener(ResumeGame);
        //restartButton.onClick.AddListener(RestartGame);
        exitButton.onClick.AddListener(ExitGame);

        // Ensure the menu is hidden when the game first starts.
        pauseMenuPanel.SetActive(false);
    }

    private void Start()
    {
        // This is the essential line. It tells our script to listen for the "OnMenuBtnClicked"
        // signal from the GameManager. When that signal is fired, our "GameManager_OnMenuBtnClicked" method will run.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnMenuBtnClicked += GameManager_OnMenuBtnClicked;
        }
    }

    private void GameManager_OnMenuBtnClicked(object sender, EventArgs e)
    {
        ShowMenu();
    }

    private void ShowMenu()
    {
        pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f; // Pauses all time-based game physics and animations.
    }

    private void HideMenu()
    {
        pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f; // Resumes normal game time.
    }

    private void ResumeGame()
    {
        HideMenu();
    }

    // private void RestartGame()
    // {
    //     Time.timeScale = 1f;
    //     if (NetworkManager.Singleton.IsHost)
    //     {
    //         // Host tells all clients to quit
    //         GameManager.Instance.RequestRestart();

    //         // Host shuts down & reloads
    //         NetworkManager.Singleton.Shutdown();
    //         SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    //     }
    //     else if (NetworkManager.Singleton.IsClient)
    //     {
    //         // Clients can’t restart – only host controls
    //         Debug.Log("Only host can restart the game.");
    //     }
    // }
  
    private void ExitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnMenuBtnClicked -= GameManager_OnMenuBtnClicked;
        }
    }
}