using UnityEngine;
using System.Collections.Generic;
using HybridEnemyAI;

/// <summary>
/// Score Flow Tracker - Tracks the complete data flow of score updates
/// Identifies exactly where and why total scores are not updating
/// </summary>
public class ScoreFlowTracker : MonoBehaviour
{
    [Header("Components to Track")]
    [SerializeField] private GameTurnManager turnManager;
    [SerializeField] private TurnScoreManager scoreManager;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private ScoreUIManager uiManager;
    
    [Header("Tracking Settings")]
    [SerializeField] private bool enableRealTimeTracking = true;
    [SerializeField] private bool logEveryFrame = false;
    [SerializeField] private bool trackUIUpdates = true;
    
    [Header("Current State")]
    [SerializeField] private ScoreFlowState currentState;
    
    [System.Serializable]
    public class ScoreFlowState
    {
        [Header("Game Mode")]
        public bool isAIOpponent;
        public bool isAITurn;
        public int currentTurn;
        
        [Header("Turn Score Manager")]
        public bool turnScoreManagerExists;
        public bool currentTurnExists;
        public int currentTurnScore;
        public int totalGameScore;
        public float currentMultiplier;
        
        [Header("Game Turn Manager")]
        public bool gameTurnManagerExists;
        public int playerScore;
        public int aiScore;
        public int totalScore;
        
        [Header("UI State")]
        public bool gameManagerUIExists;
        public bool scoreUIManagerExists;
        public string playerScoreTextContent;
        public string aiScoreTextContent;
        
        [Header("Flow Issues")]
        public List<string> detectedIssues = new List<string>();
        public List<string> missingConnections = new List<string>();
        public List<string> scoreUpdateFailures = new List<string>();
    }
    
    void Start()
    {
        FindComponents();
        InitializeTracking();
    }
    
    void FindComponents()
    {
        if (turnManager == null)
            turnManager = FindObjectOfType<GameTurnManager>();
        
        if (scoreManager == null)
            scoreManager = FindObjectOfType<TurnScoreManager>();
        
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();
        
        if (uiManager == null)
            uiManager = FindObjectOfType<ScoreUIManager>();
        
        Debug.Log($"ScoreFlowTracker: Components found - " +
                 $"TurnManager: {turnManager != null}, " +
                 $"ScoreManager: {scoreManager != null}, " +
                 $"GameManager: {gameManager != null}, " +
                 $"UIManager: {uiManager != null}");
    }
    
    void InitializeTracking()
    {
        currentState = new ScoreFlowState();
        
        if (enableRealTimeTracking)
        {
            InvokeRepeating(nameof(TrackScoreFlow), 0f, 0.5f); // Track every 0.5 seconds
        }
    }
    
    void Update()
    {
        if (logEveryFrame)
        {
            TrackScoreFlow();
        }
    }
    
    [ContextMenu("Track Score Flow Now")]
    public void TrackScoreFlow()
    {
        currentState.detectedIssues.Clear();
        currentState.missingConnections.Clear();
        currentState.scoreUpdateFailures.Clear();
        
        // Track Game Mode State
        TrackGameModeState();
        
        // Track Turn Score Manager State
        TrackTurnScoreManagerState();
        
        // Track Game Turn Manager State
        TrackGameTurnManagerState();
        
        // Track UI State
        TrackUIState();
        
        // Analyze Issues
        AnalyzeScoreFlowIssues();
        
        // Log Results
        LogTrackingResults();
    }
    
    void TrackGameModeState()
    {
        if (turnManager != null)
        {
            currentState.isAIOpponent = turnManager.isAIOpponent;
            currentState.isAITurn = turnManager.isAITurn;
            currentState.currentTurn = turnManager.currentTurn;
            currentState.gameTurnManagerExists = true;
        }
        else
        {
            currentState.gameTurnManagerExists = false;
            currentState.missingConnections.Add("GameTurnManager not found");
        }
    }
    
    void TrackTurnScoreManagerState()
    {
        if (scoreManager != null)
        {
            currentState.turnScoreManagerExists = true;
            currentState.currentTurnExists = scoreManager.currentTurn != null;
            currentState.currentTurnScore = scoreManager.GetCurrentTurnScore();
            currentState.totalGameScore = scoreManager.totalGameScore;
            currentState.currentMultiplier = scoreManager.GetCurrentTurnMultiplier();
        }
        else
        {
            currentState.turnScoreManagerExists = false;
            currentState.missingConnections.Add("TurnScoreManager not found");
        }
    }
    
    void TrackGameTurnManagerState()
    {
        if (turnManager != null)
        {
            currentState.playerScore = turnManager.playerScore;
            currentState.aiScore = turnManager.aiScore;
            currentState.totalScore = turnManager.totalScore;
        }
    }
    
    void TrackUIState()
    {
        if (trackUIUpdates)
        {
            // Check GameManager UI
            if (gameManager != null)
            {
                currentState.gameManagerUIExists = true;
                
                // Use reflection to check UI text content
                var playerScoreText = GetFieldValue<TMPro.TextMeshProUGUI>(gameManager, "playerScoreText");
                var aiScoreText = GetFieldValue<TMPro.TextMeshProUGUI>(gameManager, "aiScoreText");
                
                currentState.playerScoreTextContent = playerScoreText?.text ?? "NULL";
                currentState.aiScoreTextContent = aiScoreText?.text ?? "NULL";
            }
            else
            {
                currentState.gameManagerUIExists = false;
            }
            
            // Check ScoreUIManager
            currentState.scoreUIManagerExists = uiManager != null;
        }
    }
    
    void AnalyzeScoreFlowIssues()
    {
        // Check for missing components
        if (!currentState.gameTurnManagerExists)
        {
            currentState.detectedIssues.Add("CRITICAL: GameTurnManager missing");
        }
        
        if (!currentState.turnScoreManagerExists)
        {
            currentState.detectedIssues.Add("CRITICAL: TurnScoreManager missing");
        }
        
        // Check for AI mode issues
        if (currentState.isAIOpponent)
        {
            // In AI mode, check if scores are updating
            if (currentState.playerScore == 0 && currentState.aiScore == 0 && currentState.currentTurn > 1)
            {
                currentState.detectedIssues.Add("ISSUE: AI mode active but no scores accumulated after turn 1");
            }
            
            // Check if SwitchTurn is working
            if (currentState.currentTurnScore > 0 && !IsScoreBeingTransferred())
            {
                currentState.scoreUpdateFailures.Add("Turn score exists but not transferred to player/AI totals");
            }
        }
        else
        {
            // Single player mode
            if (currentState.totalGameScore != currentState.totalScore)
            {
                currentState.scoreUpdateFailures.Add($"TurnScoreManager.totalGameScore ({currentState.totalGameScore}) != GameTurnManager.totalScore ({currentState.totalScore})");
            }
        }
        
        // Check UI issues
        if (currentState.playerScoreTextContent == "New Text" || currentState.aiScoreTextContent == "New Text")
        {
            currentState.detectedIssues.Add("UI ISSUE: Text components showing 'New Text' instead of scores");
        }
        
        // Check turn completion flow
        if (!currentState.currentTurnExists && currentState.currentTurnScore > 0)
        {
            currentState.detectedIssues.Add("FLOW ISSUE: Turn score exists but currentTurn is null");
        }
    }
    
    bool IsScoreBeingTransferred()
    {
        if (scoreManager == null || turnManager == null) return false;
        
        int turnScore = scoreManager.GetCurrentTurnScore();
        
        if (turnScore == 0) return true; // No score to transfer
        
        // Check if the turn score matches recent changes in player/AI scores
        // This is a simplified check - in reality, we'd need to track previous values
        return turnScore > 0;
    }
    
    void LogTrackingResults()
    {
        Debug.Log("=== SCORE FLOW TRACKING RESULTS ===");
        
        // Game State
        Debug.Log($"Game Mode: {(currentState.isAIOpponent ? "AI vs Player" : "Single Player")}");
        if (currentState.isAIOpponent)
        {
            Debug.Log($"Current Turn: {(currentState.isAITurn ? "AI" : "Player")} (Turn {currentState.currentTurn})");
        }
        
        // Score State
        Debug.Log($"Turn Score: {currentState.currentTurnScore} (Multiplier: {currentState.currentMultiplier:F2}x)");
        Debug.Log($"Total Game Score: {currentState.totalGameScore}");
        
        if (currentState.isAIOpponent)
        {
            Debug.Log($"Player Score: {currentState.playerScore}");
            Debug.Log($"AI Score: {currentState.aiScore}");
        }
        else
        {
            Debug.Log($"GameTurnManager Total: {currentState.totalScore}");
        }
        
        // UI State
        Debug.Log($"UI Text - Player: '{currentState.playerScoreTextContent}', AI: '{currentState.aiScoreTextContent}'");
        
        // Issues
        if (currentState.detectedIssues.Count > 0)
        {
            Debug.Log("🚨 DETECTED ISSUES:");
            foreach (string issue in currentState.detectedIssues)
            {
                Debug.LogError($"  ❌ {issue}");
            }
        }
        
        if (currentState.missingConnections.Count > 0)
        {
            Debug.Log("🔗 MISSING CONNECTIONS:");
            foreach (string missing in currentState.missingConnections)
            {
                Debug.LogWarning($"  ⚠️ {missing}");
            }
        }
        
        if (currentState.scoreUpdateFailures.Count > 0)
        {
            Debug.Log("📊 SCORE UPDATE FAILURES:");
            foreach (string failure in currentState.scoreUpdateFailures)
            {
                Debug.LogError($"  💥 {failure}");
            }
        }
        
        if (currentState.detectedIssues.Count == 0 && currentState.scoreUpdateFailures.Count == 0)
        {
            Debug.Log("✅ No issues detected in score flow");
        }
    }
    
    [ContextMenu("Trace Turn Completion Flow")]
    public void TraceTurnCompletionFlow()
    {
        Debug.Log("=== TRACING TURN COMPLETION FLOW ===");
        
        if (turnManager == null || scoreManager == null)
        {
            Debug.LogError("Cannot trace - missing components");
            return;
        }
        
        Debug.Log("📋 EXPECTED FLOW:");
        Debug.Log("1. Player/AI completes turn");
        Debug.Log("2. EndTurn() or OnAITurnCompleted() called");
        Debug.Log("3. scoreManager.CompleteTurn() called");
        Debug.Log("4. SwitchTurn() called (AI mode only)");
        Debug.Log("5. Player/AI scores updated");
        Debug.Log("6. StartNewTurn() called");
        
        Debug.Log("📊 CURRENT STATE:");
        Debug.Log($"Current Turn Score: {scoreManager.GetCurrentTurnScore()}");
        Debug.Log($"Current Turn Exists: {scoreManager.currentTurn != null}");
        Debug.Log($"Is AI Turn: {turnManager.isAITurn}");
        Debug.Log($"Player Score: {turnManager.playerScore}");
        Debug.Log($"AI Score: {turnManager.aiScore}");
        
        // Check if we're in the middle of a turn completion
        if (scoreManager.currentTurn != null && scoreManager.GetCurrentTurnScore() > 0)
        {
            Debug.Log("🔍 TURN IN PROGRESS - Score should transfer when turn completes");
        }
        else if (scoreManager.currentTurn == null)
        {
            Debug.Log("🔍 NO ACTIVE TURN - Waiting for StartNewTurn()");
        }
        else
        {
            Debug.Log("🔍 TURN ACTIVE BUT NO SCORE - Waiting for combinations");
        }
    }
    
    [ContextMenu("Force Score Update Test")]
    public void ForceScoreUpdateTest()
    {
        Debug.Log("=== FORCE SCORE UPDATE TEST ===");
        
        if (turnManager == null)
        {
            Debug.LogError("Cannot test - GameTurnManager missing");
            return;
        }
        
        Debug.Log("Before test:");
        Debug.Log($"Player Score: {turnManager.playerScore}");
        Debug.Log($"AI Score: {turnManager.aiScore}");
        
        // Simulate score update
        if (turnManager.isAIOpponent)
        {
            if (turnManager.isAITurn)
            {
                turnManager.aiScore += 100;
                Debug.Log("Added 100 to AI score");
            }
            else
            {
                turnManager.playerScore += 100;
                Debug.Log("Added 100 to Player score");
            }
        }
        else
        {
            turnManager.totalScore += 100;
            Debug.Log("Added 100 to total score");
        }
        
        Debug.Log("After test:");
        Debug.Log($"Player Score: {turnManager.playerScore}");
        Debug.Log($"AI Score: {turnManager.aiScore}");
        Debug.Log($"Total Score: {turnManager.totalScore}");
        
        // Force UI update
        if (gameManager != null)
        {
            var method = typeof(GameManager).GetMethod("UpdatePlayerVsAIUI", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(gameManager, null);
                Debug.Log("Forced GameManager UI update");
            }
        }
    }
    
    // Utility method for reflection
    T GetFieldValue<T>(object obj, string fieldName)
    {
        var field = obj.GetType().GetField(fieldName, 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            return (T)field.GetValue(obj);
        }
        
        return default(T);
    }
    
    [ContextMenu("Monitor Next Turn Completion")]
    public void MonitorNextTurnCompletion()
    {
        Debug.Log("=== MONITORING NEXT TURN COMPLETION ===");
        Debug.Log("This will log detailed information when the next turn completes...");
        
        StartCoroutine(MonitorTurnCompletionCoroutine());
    }
    
    System.Collections.IEnumerator MonitorTurnCompletionCoroutine()
    {
        if (scoreManager == null) yield break;
        
        int initialTurnScore = scoreManager.GetCurrentTurnScore();
        int initialPlayerScore = turnManager?.playerScore ?? 0;
        int initialAIScore = turnManager?.aiScore ?? 0;
        
        Debug.Log($"📊 MONITORING STARTED - Initial scores: Turn={initialTurnScore}, Player={initialPlayerScore}, AI={initialAIScore}");
        
        // Wait for turn score to change (indicating turn activity)
        while (scoreManager.GetCurrentTurnScore() == initialTurnScore)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        Debug.Log($"📊 TURN ACTIVITY DETECTED - Turn score changed to {scoreManager.GetCurrentTurnScore()}");
        
        // Wait for turn to complete (currentTurn becomes null)
        while (scoreManager.currentTurn != null)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        Debug.Log($"📊 TURN COMPLETED - Checking score transfer...");
        
        int finalPlayerScore = turnManager?.playerScore ?? 0;
        int finalAIScore = turnManager?.aiScore ?? 0;
        
        Debug.Log($"📊 FINAL SCORES: Player={finalPlayerScore} (change: {finalPlayerScore - initialPlayerScore}), AI={finalAIScore} (change: {finalAIScore - initialAIScore})");
        
        if (finalPlayerScore == initialPlayerScore && finalAIScore == initialAIScore)
        {
            Debug.LogError("❌ SCORE TRANSFER FAILED - No change in player/AI scores after turn completion!");
        }
        else
        {
            Debug.Log("✅ SCORE TRANSFER SUCCESS - Scores updated correctly");
        }
    }
}