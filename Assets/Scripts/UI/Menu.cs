using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Menu : MonoBehaviour
{
    public enum GameState { Playing, Paused, Inventory, GameOver }

    [Header("Keybinds")]
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
    [SerializeField] private KeyCode inventoryKey = KeyCode.E;

    [Header("UI")]
    [SerializeField] private Canvas pauseCanvas;
    [SerializeField] private Canvas playCanvas;
    [SerializeField] private Canvas gameOverCanvas;
    [SerializeField] private Canvas inventoryCanvas;

    [SerializeField] private bool IsGameRunning = false;
    public static Menu Instance { get; private set; }

    public GameState CurrentGameState { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        IsGameRunning = true;
        CurrentGameState = GameState.Playing;
    }

    void Update()
    {
        if (!IsGameRunning) return;

        if (Input.GetKeyDown(pauseKey))
        {
            if (CurrentGameState == GameState.Paused)
            {
                UnpauseGame();
            }
            else if (CurrentGameState == GameState.Playing || CurrentGameState == GameState.Inventory)
            {
                PauseGame();
            }
        }

        if (Input.GetKeyDown(inventoryKey))
        {
            if (CurrentGameState == GameState.Inventory)
            {
                UnpauseGame();
            }
            else if (CurrentGameState == GameState.Playing || CurrentGameState == GameState.Paused)
            {
                OpenInventory();
            }
        }
    }
    /// <summary>
    /// Pauses the game
    /// </summary>
    private void PauseGame()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
        Time.timeScale = 0f;
        pauseCanvas.gameObject.SetActive(true);
        playCanvas.gameObject.SetActive(false);
        inventoryCanvas.gameObject.SetActive(false);
        CurrentGameState = GameState.Paused;
    }

    /// <summary>
    /// Unpauses the game
    /// </summary>
    private void UnpauseGame()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Time.timeScale = 1f;
        pauseCanvas.gameObject.SetActive(false);
        inventoryCanvas.gameObject.SetActive(false);
        playCanvas.gameObject.SetActive(true);
        CurrentGameState = GameState.Playing;
    }
    /// <summary>
    /// Stops the game (Game over)
    /// </summary>
    public void StopGame()
    {
        IsGameRunning = false;
        PauseGame();
        gameOverCanvas.gameObject.SetActive(true);
        playCanvas.gameObject.SetActive(false);
        pauseCanvas.gameObject.SetActive(false);
        inventoryCanvas.gameObject.SetActive(false);
    }

    /// <summary>
    /// Activates inventory UI
    /// </summary>
    private void OpenInventory()
    {
        InventoryUI.Instance.UpdateUI();
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
        Time.timeScale = 0f;
        inventoryCanvas.gameObject.SetActive(true);
        pauseCanvas.gameObject.SetActive(false);
        playCanvas.gameObject.SetActive(false);
        CurrentGameState = GameState.Inventory;

        // Refresh the inventory UI (if needed)
        //Inventory.Instance.RefreshInventoryUI();
    }
    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}
