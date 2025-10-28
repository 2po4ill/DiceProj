using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    public bool gameActive = true;
    
    [Header("Game Mode")]
    public GameMode currentGameMode = GameMode.SinglePlayer;
    public GameTurnManager turnManager;
    
    [Header("UI References")]
    public GameObject mainMenuPanel;
    public GameObject gameplayPanel;
    public Button singlePlayerButton;
    public Button aiOpponentButton;
    public Button backToMenuButton;
    public TextMeshProUGUI gameModeText;
    
    [Header("Player vs AI UI")]
    public GameObject playerVsAIPanel;
    public TextMeshProUGUI playerScoreText;
    public TextMeshProUGUI aiScoreText;
    public TextMeshProUGUI currentPlayerText;
    public GameObject aiThinkingIndicator;
    
    [Header("Debug")]
    public bool enableDebugLogs = true;
    
    public enum GameMode
    {
        SinglePlayer,
        AIOpponent
    }
    
    void Start()
    {
        // Initialize game state
        SetupGame();
        SetupUI();
    }
    
    void SetupGame()
    {
        // Keep cursor visible and unlocked
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Get turn manager if not assigned
        if (turnManager == null)
            turnManager = FindObjectOfType<GameTurnManager>();
        
        if (enableDebugLogs)
            Debug.Log("Game initialized - cursor visible, no player movement");
    }
    
    void SetupUI()
    {
        // Setup button listeners
        if (singlePlayerButton != null)
            singlePlayerButton.onClick.AddListener(() => StartGame(GameMode.SinglePlayer));
        
        if (aiOpponentButton != null)
            aiOpponentButton.onClick.AddListener(() => StartGame(GameMode.AIOpponent));
        
        if (backToMenuButton != null)
            backToMenuButton.onClick.AddListener(ReturnToMainMenu);
        
        // Show main menu initially
        ShowMainMenu();
    }
    
    public void StartGame(GameMode mode)
    {
        currentGameMode = mode;
        
        if (enableDebugLogs)
            Debug.Log($"Starting game in {mode} mode");
        
        // Hide main menu, show gameplay
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (gameplayPanel != null) gameplayPanel.SetActive(true);
        
        // Update game mode text
        if (gameModeText != null)
            gameModeText.text = mode == GameMode.AIOpponent ? "vs AI" : "Solo";
        
        // Configure turn manager for game mode
        if (turnManager != null)
        {
            turnManager.SetAIOpponentMode(mode == GameMode.AIOpponent);
            
            // Reset scores for new game
            if (mode == GameMode.AIOpponent)
            {
                turnManager.playerScore = 0;
                turnManager.aiScore = 0;
                turnManager.isAITurn = false; // Player goes first
                
                // Show player vs AI UI
                if (playerVsAIPanel != null) playerVsAIPanel.SetActive(true);
                UpdatePlayerVsAIUI();
            }
            else
            {
                // Hide player vs AI UI for single player
                if (playerVsAIPanel != null) playerVsAIPanel.SetActive(false);
            }
        }
        
        gameActive = true;
    }
    
    public void ReturnToMainMenu()
    {
        if (enableDebugLogs)
            Debug.Log("Returning to main menu");
        
        // Stop any active AI turns
        if (turnManager != null && turnManager.isAITurn)
        {
            turnManager.ForceEndAITurn();
        }
        
        // Reset game state
        gameActive = false;
        
        // Show main menu, hide gameplay
        ShowMainMenu();
    }
    
    void ShowMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (gameplayPanel != null) gameplayPanel.SetActive(false);
        if (playerVsAIPanel != null) playerVsAIPanel.SetActive(false);
        if (aiThinkingIndicator != null) aiThinkingIndicator.SetActive(false);
    }
    
    void Update()
    {
        // Update Player vs AI UI if in AI opponent mode
        if (currentGameMode == GameMode.AIOpponent && gameActive)
        {
            UpdatePlayerVsAIUI();
        }
    }
    
    void UpdatePlayerVsAIUI()
    {
        if (turnManager == null) return;
        
        // Update scores
        if (playerScoreText != null)
            playerScoreText.text = $"Player: {turnManager.playerScore}";
        
        if (aiScoreText != null)
            aiScoreText.text = $"AI: {turnManager.aiScore}";
        
        // Update current player indicator
        if (currentPlayerText != null)
        {
            string currentPlayer = turnManager.isAITurn ? "AI Turn" : "Your Turn";
            currentPlayerText.text = currentPlayer;
            
            // Color coding
            currentPlayerText.color = turnManager.isAITurn ? Color.red : Color.green;
        }
        
        // Show/hide AI thinking indicator
        if (aiThinkingIndicator != null)
        {
            bool aiIsThinking = turnManager.isAITurn && turnManager.aiTurnExecutor != null && 
                               turnManager.aiTurnExecutor.IsTurnActive();
            aiThinkingIndicator.SetActive(aiIsThinking);
        }
    }
    
    public void OnButtonPressed(string buttonName)
    {
        if (!gameActive) return;
        
        if (enableDebugLogs)
            Debug.Log($"Button pressed: {buttonName}");
        
        // Handle specific button actions
        switch (buttonName.ToLower())
        {
            case "singleplayer":
                StartGame(GameMode.SinglePlayer);
                break;
            case "aiopponent":
                StartGame(GameMode.AIOpponent);
                break;
            case "mainmenu":
                ReturnToMainMenu();
                break;
        }
    }
    
    public void PauseGame()
    {
        gameActive = false;
        Time.timeScale = 0f;
        
        if (enableDebugLogs)
            Debug.Log("Game paused");
    }
    
    public void ResumeGame()
    {
        gameActive = true;
        Time.timeScale = 1f;
        
        if (enableDebugLogs)
            Debug.Log("Game resumed");
    }
    
    /// <summary>
    /// Gets current game mode
    /// </summary>
    public GameMode GetCurrentGameMode()
    {
        return currentGameMode;
    }
    
    /// <summary>
    /// Checks if game is in AI opponent mode
    /// </summary>
    public bool IsAIOpponentMode()
    {
        return currentGameMode == GameMode.AIOpponent;
    }
    
    /// <summary>
    /// Forces end of current game (for emergency stops)
    /// </summary>
    public void ForceEndGame()
    {
        if (turnManager != null && turnManager.isAITurn)
        {
            turnManager.ForceEndAITurn();
        }
        
        gameActive = false;
        
        if (enableDebugLogs)
            Debug.Log("Game force ended");
    }
    
    /// <summary>
    /// Restarts current game mode
    /// </summary>
    public void RestartGame()
    {
        if (enableDebugLogs)
            Debug.Log($"Restarting game in {currentGameMode} mode");
        
        StartGame(currentGameMode);
    }
}