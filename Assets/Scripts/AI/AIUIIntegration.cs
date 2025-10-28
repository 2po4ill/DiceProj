using UnityEngine;
using System.Collections;
using HybridEnemyAI;

/// <summary>
/// AI UI Integration - coordinates all AI UI components and manages their interactions
/// Requirements: 10.4, 8.5
/// </summary>
public class AIUIIntegration : MonoBehaviour
{
    [Header("AI UI Components")]
    [SerializeField] private AIUIManager aiUIManager;
    [SerializeField] private AITurnFeedbackSystem turnFeedbackSystem;
    [SerializeField] private AIScoreDisplay scoreDisplay;
    [SerializeField] private AIDecisionStatusIndicator statusIndicator;
    [SerializeField] private AIDiceDisplaySystem diceDisplaySystem;
    
    [Header("Game Components")]
    [SerializeField] private AITurnExecutor aiTurnExecutor;
    [SerializeField] private AIGameStateAnalyzer gameStateAnalyzer;
    [SerializeField] private TurnScoreManager scoreManager;
    [SerializeField] private GameTurnManager turnManager;
    
    [Header("UI Panels")]
    [SerializeField] private GameObject aiUIPanel;
    [SerializeField] private GameObject aiScorePanel;
    [SerializeField] private GameObject aiFeedbackPanel;
    [SerializeField] private GameObject aiDecisionPanel;
    [SerializeField] private GameObject aiDicePanel;
    
    [Header("Integration Settings")]
    [SerializeField] private bool autoFindComponents = true;
    [SerializeField] private bool enableAllUIComponents = true;
    [SerializeField] private bool showUIOnAITurn = true;
    [SerializeField] private bool hideUIOnPlayerTurn = false;
    
    [Header("Score Tracking")]
    [SerializeField] private int aiTotalScore = 0;
    [SerializeField] private int playerTotalScore = 0;
    [SerializeField] private bool trackScoresAutomatically = true;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    [SerializeField] private bool showIntegrationStatus = true;
    
    // State tracking
    private bool isAITurnActive = false;
    private AITurnState currentTurnState;
    private BehaviorMode currentBehaviorMode = BehaviorMode.AGGRESSIVE;
    
    // Integration status
    private bool isInitialized = false;
    private int activeUIComponents = 0;
    
    void Start()
    {
        InitializeIntegration();
    }
    
    void InitializeIntegration()
    {
        if (autoFindComponents)
        {
            FindAllComponents();
        }
        
        ValidateComponents();
        SetupEventListeners();
        InitializeUIComponents();
        
        isInitialized = true;
        
        if (enableDebugLogs)
        {
            Debug.Log($"AIUIIntegration: Integration initialized - Active components: {activeUIComponents}");
        }
    }
    
    void FindAllComponents()
    {
        // Find AI UI components
        if (aiUIManager == null)
            aiUIManager = FindObjectOfType<AIUIManager>();
        
        if (turnFeedbackSystem == null)
            turnFeedbackSystem = FindObjectOfType<AITurnFeedbackSystem>();
        
        if (scoreDisplay == null)
            scoreDisplay = FindObjectOfType<AIScoreDisplay>();
        
        if (statusIndicator == null)
            statusIndicator = FindObjectOfType<AIDecisionStatusIndicator>();
        
        if (diceDisplaySystem == null)
            diceDisplaySystem = FindObjectOfType<AIDiceDisplaySystem>();
        
        // Find game components
        if (aiTurnExecutor == null)
            aiTurnExecutor = FindObjectOfType<AITurnExecutor>();
        
        if (gameStateAnalyzer == null)
            gameStateAnalyzer = FindObjectOfType<AIGameStateAnalyzer>();
        
        if (scoreManager == null)
            scoreManager = FindObjectOfType<TurnScoreManager>();
        
        if (turnManager == null)
            turnManager = FindObjectOfType<GameTurnManager>();
        
        if (enableDebugLogs)
        {
            Debug.Log($"AIUIIntegration: Components found - " +
                     $"UIManager: {aiUIManager != null}, " +
                     $"FeedbackSystem: {turnFeedbackSystem != null}, " +
                     $"ScoreDisplay: {scoreDisplay != null}, " +
                     $"StatusIndicator: {statusIndicator != null}, " +
                     $"DiceDisplay: {diceDisplaySystem != null}");
        }
    }
    
    void ValidateComponents()
    {
        activeUIComponents = 0;
        
        if (aiUIManager != null) activeUIComponents++;
        if (turnFeedbackSystem != null) activeUIComponents++;
        if (scoreDisplay != null) activeUIComponents++;
        if (statusIndicator != null) activeUIComponents++;
        if (diceDisplaySystem != null) activeUIComponents++;
        
        if (activeUIComponents == 0)
        {
            Debug.LogWarning("AIUIIntegration: No AI UI components found! UI integration will be limited.");
        }
        
        if (aiTurnExecutor == null)
        {
            Debug.LogError("AIUIIntegration: No AITurnExecutor found! AI UI integration requires AITurnExecutor.");
        }
    }
    
    void SetupEventListeners()
    {
        if (aiTurnExecutor != null)
        {
            aiTurnExecutor.OnTurnStarted += HandleAITurnStarted;
            aiTurnExecutor.OnTurnCompleted += HandleAITurnCompleted;
            
            if (enableDebugLogs)
                Debug.Log("AIUIIntegration: Event listeners set up");
        }
        
        // Set up turn manager events if available
        if (turnManager != null)
        {
            // Subscribe to turn change events if available
            // This would depend on the specific implementation of GameTurnManager
        }
    }
    
    void InitializeUIComponents()
    {
        // Initialize all UI components with consistent settings
        if (aiUIManager != null)
        {
            aiUIManager.SetEnableAnimations(enableAllUIComponents);
        }
        
        if (turnFeedbackSystem != null)
        {
            turnFeedbackSystem.SetEnableSmoothAnimations(enableAllUIComponents);
            turnFeedbackSystem.SetShowDetailedFeedback(showIntegrationStatus);
        }
        
        if (scoreDisplay != null)
        {
            scoreDisplay.SetEnableScoreAnimations(enableAllUIComponents);
            scoreDisplay.SetEnableColorTransitions(enableAllUIComponents);
        }
        
        if (statusIndicator != null)
        {
            statusIndicator.SetEnablePulseAnimation(enableAllUIComponents);
            statusIndicator.SetShowDetailedFactors(showIntegrationStatus);
        }
        
        // Set initial UI panel visibility
        SetUIVisibility(!hideUIOnPlayerTurn);
        
        if (enableDebugLogs)
            Debug.Log("AIUIIntegration: UI components initialized");
    }
    
    #region Event Handlers
    
    void HandleAITurnStarted(AITurnState turnState)
    {
        isAITurnActive = true;
        currentTurnState = turnState;
        currentBehaviorMode = turnState.CurrentMode;
        
        // Show AI UI if configured
        if (showUIOnAITurn)
        {
            SetUIVisibility(true);
        }
        
        // Update all UI components with current state
        UpdateAllUIComponents();
        
        if (enableDebugLogs)
        {
            Debug.Log($"AIUIIntegration: AI turn started - Mode: {turnState.CurrentMode}, " +
                     $"UI Visible: {showUIOnAITurn}");
        }
    }
    
    void HandleAITurnCompleted(AITurnState turnState)
    {
        isAITurnActive = false;
        currentTurnState = turnState;
        
        // Update AI total score
        if (trackScoresAutomatically)
        {
            aiTotalScore += turnState.CurrentTurnScore;
            UpdateScoreTracking();
        }
        
        // Hide AI UI if configured
        if (hideUIOnPlayerTurn)
        {
            StartCoroutine(HideUIDelayed(2f)); // Delay to show final results
        }
        
        // Final update of all UI components
        UpdateAllUIComponents();
        
        if (enableDebugLogs)
        {
            Debug.Log($"AIUIIntegration: AI turn completed - Final Score: {turnState.CurrentTurnScore}, " +
                     $"AI Total: {aiTotalScore}");
        }
    }
    
    #endregion
    
    #region UI Management
    
    void SetUIVisibility(bool visible)
    {
        if (aiUIPanel != null)
            aiUIPanel.SetActive(visible);
        
        if (aiScorePanel != null)
            aiScorePanel.SetActive(visible);
        
        if (aiFeedbackPanel != null)
            aiFeedbackPanel.SetActive(visible);
        
        if (aiDecisionPanel != null)
            aiDecisionPanel.SetActive(visible);
        
        if (aiDicePanel != null)
            aiDicePanel.SetActive(visible);
        
        if (enableDebugLogs)
            Debug.Log($"AIUIIntegration: UI visibility set to {visible}");
    }
    
    void UpdateAllUIComponents()
    {
        if (!isInitialized) return;
        
        // Update score display
        if (scoreDisplay != null && currentTurnState != null)
        {
            scoreDisplay.UpdateFromTurnState(currentTurnState, aiTotalScore, playerTotalScore);
        }
        
        // Update UI manager with current scores
        if (aiUIManager != null)
        {
            aiUIManager.SetAIScore(aiTotalScore, playerTotalScore);
        }
        
        // Update behavior mode across components
        UpdateBehaviorModeDisplay();
        
        // Update game state information
        UpdateGameStateDisplay();
    }
    
    void UpdateBehaviorModeDisplay()
    {
        if (scoreDisplay != null)
        {
            int pointsCap = gameStateAnalyzer?.GetPointsPerTurnCap(currentBehaviorMode) ?? 400;
            int bufferCap = gameStateAnalyzer?.GetCurrentBufferCap() ?? 200;
            
            scoreDisplay.UpdateBehaviorMode(currentBehaviorMode);
            scoreDisplay.UpdatePointsCap(pointsCap);
            scoreDisplay.UpdateBufferCap(bufferCap);
        }
    }
    
    void UpdateGameStateDisplay()
    {
        if (gameStateAnalyzer != null)
        {
            // Update current round and buffer information
            int currentRound = gameStateAnalyzer.GetCurrentRound();
            int bufferCap = gameStateAnalyzer.GetCurrentBufferCap();
            
            // This information could be displayed in a game state panel
            // For now, we'll log it if debug is enabled
            if (enableDebugLogs)
            {
                Debug.Log($"AIUIIntegration: Game State - Round: {currentRound}, Buffer: {bufferCap}");
            }
        }
    }
    
    void UpdateScoreTracking()
    {
        // Get player score from score manager
        if (scoreManager != null)
        {
            playerTotalScore = scoreManager.totalGameScore;
        }
        
        // Update all UI components with new scores
        UpdateAllUIComponents();
        
        if (enableDebugLogs)
        {
            Debug.Log($"AIUIIntegration: Scores updated - AI: {aiTotalScore}, Player: {playerTotalScore}");
        }
    }
    
    #endregion
    
    #region Animation and Effects
    
    IEnumerator HideUIDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        SetUIVisibility(false);
    }
    
    IEnumerator ShowUIDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        SetUIVisibility(true);
    }
    
    #endregion
    
    #region Public Interface
    
    /// <summary>
    /// Manually update AI score (for external integration)
    /// </summary>
    public void UpdateAIScore(int totalScore)
    {
        aiTotalScore = totalScore;
        UpdateScoreTracking();
        
        if (enableDebugLogs)
            Debug.Log($"AIUIIntegration: AI score manually updated to {totalScore}");
    }
    
    /// <summary>
    /// Manually update player score (for external integration)
    /// </summary>
    public void UpdatePlayerScore(int totalScore)
    {
        playerTotalScore = totalScore;
        UpdateScoreTracking();
        
        if (enableDebugLogs)
            Debug.Log($"AIUIIntegration: Player score manually updated to {totalScore}");
    }
    
    /// <summary>
    /// Set UI visibility manually
    /// </summary>
    public void SetUIVisible(bool visible)
    {
        SetUIVisibility(visible);
    }
    
    /// <summary>
    /// Toggle all UI animations
    /// </summary>
    public void SetEnableAnimations(bool enable)
    {
        enableAllUIComponents = enable;
        
        if (aiUIManager != null)
            aiUIManager.SetEnableAnimations(enable);
        
        if (turnFeedbackSystem != null)
            turnFeedbackSystem.SetEnableSmoothAnimations(enable);
        
        if (scoreDisplay != null)
        {
            scoreDisplay.SetEnableScoreAnimations(enable);
            scoreDisplay.SetEnableColorTransitions(enable);
        }
        
        if (statusIndicator != null)
            statusIndicator.SetEnablePulseAnimation(enable);
        
        if (enableDebugLogs)
            Debug.Log($"AIUIIntegration: Animations set to {enable}");
    }
    
    /// <summary>
    /// Toggle detailed information display
    /// </summary>
    public void SetShowDetailedInfo(bool show)
    {
        showIntegrationStatus = show;
        
        if (aiUIManager != null)
            aiUIManager.SetShowDetailedDecisions(show);
        
        if (turnFeedbackSystem != null)
            turnFeedbackSystem.SetShowDetailedFeedback(show);
        
        if (statusIndicator != null)
            statusIndicator.SetShowDetailedFactors(show);
        
        if (enableDebugLogs)
            Debug.Log($"AIUIIntegration: Detailed info display set to {show}");
    }
    
    /// <summary>
    /// Get current AI turn state
    /// </summary>
    public AITurnState GetCurrentTurnState()
    {
        return currentTurnState;
    }
    
    /// <summary>
    /// Check if AI turn is currently active
    /// </summary>
    public bool IsAITurnActive()
    {
        return isAITurnActive;
    }
    
    /// <summary>
    /// Get current scores
    /// </summary>
    public (int aiScore, int playerScore, int difference) GetCurrentScores()
    {
        return (aiTotalScore, playerTotalScore, aiTotalScore - playerTotalScore);
    }
    
    /// <summary>
    /// Get integration status
    /// </summary>
    public (bool initialized, int activeComponents) GetIntegrationStatus()
    {
        return (isInitialized, activeUIComponents);
    }
    
    /// <summary>
    /// Force refresh of all UI components
    /// </summary>
    public void RefreshAllUI()
    {
        UpdateAllUIComponents();
        
        if (enableDebugLogs)
            Debug.Log("AIUIIntegration: All UI components refreshed");
    }
    
    /// <summary>
    /// Reset all UI components to initial state
    /// </summary>
    public void ResetAllUI()
    {
        aiTotalScore = 0;
        playerTotalScore = 0;
        isAITurnActive = false;
        currentTurnState = null;
        currentBehaviorMode = BehaviorMode.AGGRESSIVE;
        
        if (scoreDisplay != null)
            scoreDisplay.ResetDisplay();
        
        // Reset other components as needed
        UpdateAllUIComponents();
        
        if (enableDebugLogs)
            Debug.Log("AIUIIntegration: All UI components reset");
    }
    
    #endregion
    
    #region Context Menu (for testing)
    
    [ContextMenu("Test AI Turn Start")]
    void TestAITurnStart()
    {
        var testTurnState = new AITurnState
        {
            CurrentMode = BehaviorMode.AGGRESSIVE,
            PointsPerTurnCap = 400,
            MaxIterations = 5,
            CurrentDice = new System.Collections.Generic.List<int> { 1, 2, 3, 4, 5, 6 }
        };
        
        HandleAITurnStarted(testTurnState);
        Debug.Log("AIUIIntegration: Test AI turn started");
    }
    
    [ContextMenu("Test Score Update")]
    void TestScoreUpdate()
    {
        UpdateAIScore(aiTotalScore + 300);
        UpdatePlayerScore(playerTotalScore + 200);
        Debug.Log($"AIUIIntegration: Test scores updated - AI: {aiTotalScore}, Player: {playerTotalScore}");
    }
    
    [ContextMenu("Toggle UI Visibility")]
    void TestToggleUI()
    {
        bool currentVisibility = aiUIPanel?.activeInHierarchy ?? false;
        SetUIVisibility(!currentVisibility);
        Debug.Log($"AIUIIntegration: UI visibility toggled to {!currentVisibility}");
    }
    
    #endregion
    
    void OnDestroy()
    {
        // Clean up event listeners
        if (aiTurnExecutor != null)
        {
            aiTurnExecutor.OnTurnStarted -= HandleAITurnStarted;
            aiTurnExecutor.OnTurnCompleted -= HandleAITurnCompleted;
        }
        
        // Stop any running coroutines
        StopAllCoroutines();
    }
}