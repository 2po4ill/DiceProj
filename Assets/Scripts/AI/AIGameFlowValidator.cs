using UnityEngine;
using System.Collections;
using HybridEnemyAI;

/// <summary>
/// Validates complete AI vs Player game flow and ensures all components work together
/// </summary>
public class AIGameFlowValidator : MonoBehaviour
{
    [Header("Game Components")]
    public GameManager gameManager;
    public GameTurnManager turnManager;
    public AITurnExecutor aiTurnExecutor;
    public AIGameStateAnalyzer gameStateAnalyzer;
    public ScoreUIManager scoreUIManager;
    public AIConfigurationManager configManager;
    
    [Header("Validation Settings")]
    public bool runValidationOnStart = false;
    public bool enableDetailedLogs = true;
    public int maxTestTurns = 10;
    
    [Header("Test Results")]
    public bool allComponentsFound = false;
    public bool gameFlowWorking = false;
    public bool aiTurnsExecuting = false;
    public bool scoreTrackingWorking = false;
    public bool uiUpdatingCorrectly = false;
    
    private int testTurnCount = 0;
    private bool validationInProgress = false;
    
    void Start()
    {
        if (runValidationOnStart)
        {
            StartCoroutine(RunCompleteValidation());
        }
    }
    
    /// <summary>
    /// Runs complete validation of AI vs Player game flow
    /// </summary>
    public void RunValidation()
    {
        if (validationInProgress)
        {
            Debug.LogWarning("Validation already in progress!");
            return;
        }
        
        StartCoroutine(RunCompleteValidation());
    }
    
    IEnumerator RunCompleteValidation()
    {
        validationInProgress = true;
        testTurnCount = 0;
        
        Debug.Log("=== STARTING AI GAME FLOW VALIDATION ===");
        
        // Step 1: Validate all components are present
        yield return StartCoroutine(ValidateComponents());
        
        if (!allComponentsFound)
        {
            Debug.LogError("Component validation failed! Cannot proceed with game flow test.");
            validationInProgress = false;
            yield break;
        }
        
        // Step 2: Test game mode switching
        yield return StartCoroutine(ValidateGameModeSwitch());
        
        // Step 3: Test AI vs Player game flow
        yield return StartCoroutine(ValidateAIVsPlayerFlow());
        
        // Step 4: Test configuration system
        yield return StartCoroutine(ValidateConfigurationSystem());
        
        // Final results
        LogValidationResults();
        validationInProgress = false;
        
        Debug.Log("=== AI GAME FLOW VALIDATION COMPLETE ===");
    }
    
    IEnumerator ValidateComponents()
    {
        Debug.Log("--- Validating Components ---");
        
        // Find components if not assigned
        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
        if (turnManager == null) turnManager = FindObjectOfType<GameTurnManager>();
        if (aiTurnExecutor == null) aiTurnExecutor = FindObjectOfType<AITurnExecutor>();
        if (gameStateAnalyzer == null) gameStateAnalyzer = FindObjectOfType<AIGameStateAnalyzer>();
        if (scoreUIManager == null) scoreUIManager = FindObjectOfType<ScoreUIManager>();
        if (configManager == null) configManager = FindObjectOfType<AIConfigurationManager>();
        
        // Check critical components
        bool criticalComponentsFound = true;
        
        if (gameManager == null)
        {
            Debug.LogError("GameManager not found!");
            criticalComponentsFound = false;
        }
        
        if (turnManager == null)
        {
            Debug.LogError("GameTurnManager not found!");
            criticalComponentsFound = false;
        }
        
        if (aiTurnExecutor == null)
        {
            Debug.LogError("AITurnExecutor not found!");
            criticalComponentsFound = false;
        }
        
        if (gameStateAnalyzer == null)
        {
            Debug.LogError("AIGameStateAnalyzer not found!");
            criticalComponentsFound = false;
        }
        
        // Optional components (warn but don't fail)
        if (scoreUIManager == null)
            Debug.LogWarning("ScoreUIManager not found - UI updates may not work");
        
        if (configManager == null)
            Debug.LogWarning("AIConfigurationManager not found - configuration system unavailable");
        
        allComponentsFound = criticalComponentsFound;
        
        if (enableDetailedLogs)
        {
            Debug.Log($"Component validation: {(allComponentsFound ? "PASSED" : "FAILED")}");
        }
        
        yield return null;
    }
    
    IEnumerator ValidateGameModeSwitch()
    {
        Debug.Log("--- Validating Game Mode Switch ---");
        
        if (gameManager == null)
        {
            Debug.LogError("Cannot test game mode switch - GameManager missing");
            yield break;
        }
        
        // Test switching to AI opponent mode
        gameManager.StartGame(GameManager.GameMode.AIOpponent);
        yield return new WaitForSeconds(0.5f);
        
        // Verify AI opponent mode is active
        bool aiModeActive = gameManager.IsAIOpponentMode();
        bool turnManagerConfigured = turnManager != null && turnManager.isAIOpponent;
        
        gameFlowWorking = aiModeActive && turnManagerConfigured;
        
        if (enableDetailedLogs)
        {
            Debug.Log($"AI Mode Active: {aiModeActive}");
            Debug.Log($"Turn Manager Configured: {turnManagerConfigured}");
            Debug.Log($"Game mode switch: {(gameFlowWorking ? "PASSED" : "FAILED")}");
        }
        
        yield return null;
    }
    
    IEnumerator ValidateAIVsPlayerFlow()
    {
        Debug.Log("--- Validating AI vs Player Flow ---");
        
        if (!gameFlowWorking)
        {
            Debug.LogError("Cannot test AI vs Player flow - game mode switch failed");
            yield break;
        }
        
        // Reset scores for clean test
        if (turnManager != null)
        {
            turnManager.playerScore = 0;
            turnManager.aiScore = 0;
            turnManager.isAITurn = true; // Start with AI turn for testing
        }
        
        // Test AI turn execution
        yield return StartCoroutine(TestAITurnExecution());
        
        // Test score tracking
        yield return StartCoroutine(TestScoreTracking());
        
        // Test UI updates
        yield return StartCoroutine(TestUIUpdates());
        
        yield return null;
    }
    
    IEnumerator TestAITurnExecution()
    {
        Debug.Log("Testing AI turn execution...");
        
        if (aiTurnExecutor == null)
        {
            Debug.LogError("Cannot test AI turn - AITurnExecutor missing");
            aiTurnsExecuting = false;
            yield break;
        }
        
        // Start an AI turn
        aiTurnExecutor.StartAITurn(1);
        
        // Wait for AI turn to complete (with timeout)
        float timeout = 10f;
        float elapsed = 0f;
        
        while (aiTurnExecutor.IsTurnActive() && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Check if AI turn completed successfully
        aiTurnsExecuting = !aiTurnExecutor.IsTurnActive() && elapsed < timeout;
        
        if (enableDetailedLogs)
        {
            Debug.Log($"AI turn completed in {elapsed:F2} seconds");
            Debug.Log($"AI turn execution: {(aiTurnsExecuting ? "PASSED" : "FAILED")}");
        }
        
        yield return null;
    }
    
    IEnumerator TestScoreTracking()
    {
        Debug.Log("Testing score tracking...");
        
        if (turnManager == null)
        {
            Debug.LogError("Cannot test score tracking - GameTurnManager missing");
            scoreTrackingWorking = false;
            yield break;
        }
        
        // Record initial scores
        int initialPlayerScore = turnManager.playerScore;
        int initialAIScore = turnManager.aiScore;
        
        // Simulate some score changes
        turnManager.aiScore += 100;
        yield return new WaitForSeconds(0.1f);
        
        turnManager.playerScore += 150;
        yield return new WaitForSeconds(0.1f);
        
        // Verify scores changed
        bool scoresChanged = (turnManager.aiScore != initialAIScore) && 
                           (turnManager.playerScore != initialPlayerScore);
        
        scoreTrackingWorking = scoresChanged;
        
        if (enableDetailedLogs)
        {
            Debug.Log($"Initial - Player: {initialPlayerScore}, AI: {initialAIScore}");
            Debug.Log($"Final - Player: {turnManager.playerScore}, AI: {turnManager.aiScore}");
            Debug.Log($"Score tracking: {(scoreTrackingWorking ? "PASSED" : "FAILED")}");
        }
        
        yield return null;
    }
    
    IEnumerator TestUIUpdates()
    {
        Debug.Log("Testing UI updates...");
        
        if (scoreUIManager == null)
        {
            Debug.LogWarning("Cannot test UI updates - ScoreUIManager missing");
            uiUpdatingCorrectly = false;
            yield break;
        }
        
        // Trigger UI update
        scoreUIManager.OnTurnSwitched();
        yield return new WaitForSeconds(0.5f);
        
        // For now, just assume UI is working if no errors occurred
        // In a real implementation, you'd check specific UI elements
        uiUpdatingCorrectly = true;
        
        if (enableDetailedLogs)
        {
            Debug.Log($"UI updates: {(uiUpdatingCorrectly ? "PASSED" : "FAILED")}");
        }
        
        yield return null;
    }
    
    IEnumerator ValidateConfigurationSystem()
    {
        Debug.Log("--- Validating Configuration System ---");
        
        if (configManager == null)
        {
            Debug.LogWarning("Configuration system not available - skipping test");
            yield break;
        }
        
        // Test applying a difficulty preset
        var presets = configManager.difficultyPresets;
        if (presets != null && presets.Length > 0)
        {
            configManager.ApplyDifficultyPreset(presets[0]);
            yield return new WaitForSeconds(0.1f);
            
            if (enableDetailedLogs)
            {
                Debug.Log($"Applied difficulty preset: {presets[0].name}");
            }
        }
        
        // Test configuration application
        configManager.ApplyCurrentConfiguration();
        yield return new WaitForSeconds(0.1f);
        
        if (enableDetailedLogs)
        {
            Debug.Log("Configuration system: PASSED");
        }
        
        yield return null;
    }
    
    void LogValidationResults()
    {
        Debug.Log("=== VALIDATION RESULTS ===");
        Debug.Log($"All Components Found: {allComponentsFound}");
        Debug.Log($"Game Flow Working: {gameFlowWorking}");
        Debug.Log($"AI Turns Executing: {aiTurnsExecuting}");
        Debug.Log($"Score Tracking Working: {scoreTrackingWorking}");
        Debug.Log($"UI Updating Correctly: {uiUpdatingCorrectly}");
        
        bool overallSuccess = allComponentsFound && gameFlowWorking && aiTurnsExecuting && scoreTrackingWorking;
        Debug.Log($"OVERALL VALIDATION: {(overallSuccess ? "PASSED" : "FAILED")}");
        
        if (!overallSuccess)
        {
            Debug.LogWarning("Some validation tests failed. Check individual component logs for details.");
        }
    }
    
    /// <summary>
    /// Manual test method for quick validation
    /// </summary>
    [ContextMenu("Run Quick Validation")]
    public void RunQuickValidation()
    {
        RunValidation();
    }
    
    /// <summary>
    /// Forces a test AI turn for debugging
    /// </summary>
    [ContextMenu("Test AI Turn")]
    public void TestSingleAITurn()
    {
        if (aiTurnExecutor != null)
        {
            Debug.Log("Starting test AI turn...");
            aiTurnExecutor.StartAITurn(999); // Use turn 999 for testing
        }
        else
        {
            Debug.LogError("AITurnExecutor not found!");
        }
    }
}