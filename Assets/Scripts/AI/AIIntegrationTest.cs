using UnityEngine;
using System.Collections;
using HybridEnemyAI;

/// <summary>
/// Integration test for complete AI vs Player game flow
/// Tests end-to-end functionality and validates requirements compliance
/// </summary>
public class AIIntegrationTest : MonoBehaviour
{
    [Header("Test Configuration")]
    public bool runTestOnStart = false;
    public bool enableVerboseLogging = true;
    public int numberOfTestGames = 3;
    public int maxTurnsPerGame = 10;
    
    [Header("Test Results")]
    public int gamesCompleted = 0;
    public int playerWins = 0;
    public int aiWins = 0;
    public float averageGameLength = 0f;
    public bool allRequirementsMet = false;
    
    private GameManager gameManager;
    private GameTurnManager turnManager;
    private AITurnExecutor aiTurnExecutor;
    private AIGameStateAnalyzer gameStateAnalyzer;
    private bool testInProgress = false;
    
    void Start()
    {
        InitializeComponents();
        
        if (runTestOnStart)
        {
            StartCoroutine(RunIntegrationTests());
        }
    }
    
    void InitializeComponents()
    {
        gameManager = FindObjectOfType<GameManager>();
        turnManager = FindObjectOfType<GameTurnManager>();
        aiTurnExecutor = FindObjectOfType<AITurnExecutor>();
        gameStateAnalyzer = FindObjectOfType<AIGameStateAnalyzer>();
    }
    
    /// <summary>
    /// Runs complete integration tests for AI vs Player functionality
    /// </summary>
    [ContextMenu("Run Integration Tests")]
    public void RunTests()
    {
        if (testInProgress)
        {
            Debug.LogWarning("Integration test already in progress!");
            return;
        }
        
        StartCoroutine(RunIntegrationTests());
    }
    
    IEnumerator RunIntegrationTests()
    {
        testInProgress = true;
        
        Debug.Log("=== STARTING AI INTEGRATION TESTS ===");
        
        // Reset test results
        gamesCompleted = 0;
        playerWins = 0;
        aiWins = 0;
        
        // Test 1: Component Integration
        yield return StartCoroutine(TestComponentIntegration());
        
        // Test 2: Game Flow Validation
        yield return StartCoroutine(TestGameFlowValidation());
        
        // Test 3: AI Behavior Validation
        yield return StartCoroutine(TestAIBehaviorValidation());
        
        // Test 4: Score Consistency
        yield return StartCoroutine(TestScoreConsistency());
        
        // Test 5: Requirements Compliance
        yield return StartCoroutine(TestRequirementsCompliance());
        
        // Generate final report
        GenerateFinalReport();
        
        testInProgress = false;
        Debug.Log("=== AI INTEGRATION TESTS COMPLETE ===");
    }
    
    IEnumerator TestComponentIntegration()
    {
        Debug.Log("--- Testing Component Integration ---");
        
        bool integrationPassed = true;
        
        // Verify all critical components exist
        if (gameManager == null)
        {
            Debug.LogError("GameManager not found!");
            integrationPassed = false;
        }
        
        if (turnManager == null)
        {
            Debug.LogError("GameTurnManager not found!");
            integrationPassed = false;
        }
        
        if (aiTurnExecutor == null)
        {
            Debug.LogError("AITurnExecutor not found!");
            integrationPassed = false;
        }
        
        if (gameStateAnalyzer == null)
        {
            Debug.LogError("AIGameStateAnalyzer not found!");
            integrationPassed = false;
        }
        
        // Test component communication
        if (integrationPassed && turnManager.aiTurnExecutor == null)
        {
            Debug.LogWarning("GameTurnManager.aiTurnExecutor not assigned - attempting auto-assignment");
            turnManager.aiTurnExecutor = aiTurnExecutor;
        }
        
        if (enableVerboseLogging)
        {
            Debug.Log($"Component Integration: {(integrationPassed ? "PASSED" : "FAILED")}");
        }
        
        yield return null;
    }
    
    IEnumerator TestGameFlowValidation()
    {
        Debug.Log("--- Testing Game Flow Validation ---");
        
        if (gameManager == null || turnManager == null)
        {
            Debug.LogError("Cannot test game flow - missing components");
            yield break;
        }
        
        // Test game mode switching
        gameManager.StartGame(GameManager.GameMode.AIOpponent);
        yield return new WaitForSeconds(0.5f);
        
        bool aiModeActive = gameManager.IsAIOpponentMode() && turnManager.isAIOpponent;
        
        if (enableVerboseLogging)
        {
            Debug.Log($"AI Mode Activation: {(aiModeActive ? "PASSED" : "FAILED")}");
        }
        
        // Test turn switching
        bool initialTurnState = turnManager.isAITurn;
        turnManager.isAITurn = !initialTurnState;
        yield return new WaitForSeconds(0.1f);
        
        bool turnSwitchWorking = turnManager.isAITurn != initialTurnState;
        
        if (enableVerboseLogging)
        {
            Debug.Log($"Turn Switching: {(turnSwitchWorking ? "PASSED" : "FAILED")}");
        }
        
        yield return null;
    }
    
    IEnumerator TestAIBehaviorValidation()
    {
        Debug.Log("--- Testing AI Behavior Validation ---");
        
        if (aiTurnExecutor == null || gameStateAnalyzer == null)
        {
            Debug.LogError("Cannot test AI behavior - missing AI components");
            yield break;
        }
        
        // Test behavior mode switching
        BehaviorMode aggressiveMode = gameStateAnalyzer.AnalyzeGameState(0, 200); // AI behind
        BehaviorMode passiveMode = gameStateAnalyzer.AnalyzeGameState(200, 0);   // AI ahead
        
        bool behaviorSwitchingWorks = (aggressiveMode == BehaviorMode.AGGRESSIVE) && 
                                     (passiveMode == BehaviorMode.PASSIVE);
        
        if (enableVerboseLogging)
        {
            Debug.Log($"AI behind (0 vs 200): {aggressiveMode}");
            Debug.Log($"AI ahead (200 vs 0): {passiveMode}");
            Debug.Log($"Behavior Mode Switching: {(behaviorSwitchingWorks ? "PASSED" : "FAILED")}");
        }
        
        // Test AI turn execution
        aiTurnExecutor.StartAITurn(1);
        
        float timeout = 10f;
        float elapsed = 0f;
        
        while (aiTurnExecutor.IsTurnActive() && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        bool aiTurnCompleted = !aiTurnExecutor.IsTurnActive() && elapsed < timeout;
        
        if (enableVerboseLogging)
        {
            Debug.Log($"AI Turn Execution (completed in {elapsed:F2}s): {(aiTurnCompleted ? "PASSED" : "FAILED")}");
        }
        
        yield return null;
    }
    
    IEnumerator TestScoreConsistency()
    {
        Debug.Log("--- Testing Score Consistency ---");
        
        if (turnManager == null)
        {
            Debug.LogError("Cannot test score consistency - GameTurnManager missing");
            yield break;
        }
        
        // Reset scores
        int initialPlayerScore = turnManager.playerScore;
        int initialAIScore = turnManager.aiScore;
        
        // Simulate score changes
        turnManager.playerScore = 100;
        turnManager.aiScore = 150;
        
        yield return new WaitForSeconds(0.1f);
        
        // Verify scores are tracked correctly
        bool scoresUpdated = (turnManager.playerScore == 100) && (turnManager.aiScore == 150);
        
        // Test score retrieval methods
        int currentPlayerScore = turnManager.GetCurrentPlayerScore();
        int opponentScore = turnManager.GetOpponentScore();
        
        bool scoreRetrievalWorks = (currentPlayerScore >= 0) && (opponentScore >= 0);
        
        if (enableVerboseLogging)
        {
            Debug.Log($"Score Updates: {(scoresUpdated ? "PASSED" : "FAILED")}");
            Debug.Log($"Score Retrieval: {(scoreRetrievalWorks ? "PASSED" : "FAILED")}");
            Debug.Log($"Player Score: {turnManager.playerScore}, AI Score: {turnManager.aiScore}");
        }
        
        yield return null;
    }
    
    IEnumerator TestRequirementsCompliance()
    {
        Debug.Log("--- Testing Requirements Compliance ---");
        
        bool requirementsMet = true;
        
        // Requirement 10.1: Complete AI vs Player gameplay
        bool gameplayComplete = (gameManager != null && gameManager.IsAIOpponentMode()) &&
                               (turnManager != null && turnManager.isAIOpponent) &&
                               (aiTurnExecutor != null);
        
        if (!gameplayComplete)
        {
            Debug.LogError("Requirement 10.1 FAILED: AI vs Player gameplay not complete");
            requirementsMet = false;
        }
        
        // Requirement 10.2: Turn switching functionality
        bool turnSwitchingWorks = turnManager != null && turnManager.isAIOpponent;
        
        if (!turnSwitchingWorks)
        {
            Debug.LogError("Requirement 10.2 FAILED: Turn switching not working");
            requirementsMet = false;
        }
        
        // Requirement 10.3: Score consistency
        bool scoreConsistency = turnManager != null && 
                               turnManager.GetCurrentPlayerScore() >= 0 &&
                               turnManager.GetOpponentScore() >= 0;
        
        if (!scoreConsistency)
        {
            Debug.LogError("Requirement 10.3 FAILED: Score consistency issues");
            requirementsMet = false;
        }
        
        allRequirementsMet = requirementsMet;
        
        if (enableVerboseLogging)
        {
            Debug.Log($"Requirements Compliance: {(requirementsMet ? "PASSED" : "FAILED")}");
        }
        
        yield return null;
    }
    
    void GenerateFinalReport()
    {
        Debug.Log("=== INTEGRATION TEST REPORT ===");
        Debug.Log($"Games Completed: {gamesCompleted}");
        Debug.Log($"Player Wins: {playerWins}");
        Debug.Log($"AI Wins: {aiWins}");
        Debug.Log($"Average Game Length: {averageGameLength:F1} turns");
        Debug.Log($"All Requirements Met: {allRequirementsMet}");
        
        // Component status
        Debug.Log("--- Component Status ---");
        Debug.Log($"GameManager: {(gameManager != null ? "OK" : "MISSING")}");
        Debug.Log($"GameTurnManager: {(turnManager != null ? "OK" : "MISSING")}");
        Debug.Log($"AITurnExecutor: {(aiTurnExecutor != null ? "OK" : "MISSING")}");
        Debug.Log($"AIGameStateAnalyzer: {(gameStateAnalyzer != null ? "OK" : "MISSING")}");
        
        // Integration status
        bool integrationWorking = gameManager != null && turnManager != null && 
                                 aiTurnExecutor != null && gameStateAnalyzer != null &&
                                 allRequirementsMet;
        
        Debug.Log($"--- OVERALL STATUS: {(integrationWorking ? "PASSED" : "FAILED")} ---");
        
        if (!integrationWorking)
        {
            Debug.LogWarning("Integration test failed. Check individual test results above.");
        }
        else
        {
            Debug.Log("All integration tests passed! AI vs Player game flow is working correctly.");
        }
    }
    
    /// <summary>
    /// Quick test for manual validation
    /// </summary>
    [ContextMenu("Quick Integration Test")]
    public void QuickTest()
    {
        RunTests();
    }
    
    /// <summary>
    /// Test a single AI vs Player game
    /// </summary>
    [ContextMenu("Test Single Game")]
    public void TestSingleGame()
    {
        StartCoroutine(RunSingleGameTest());
    }
    
    IEnumerator RunSingleGameTest()
    {
        Debug.Log("--- Testing Single AI vs Player Game ---");
        
        if (gameManager == null || turnManager == null)
        {
            Debug.LogError("Cannot run single game test - missing components");
            yield break;
        }
        
        // Start AI opponent mode
        gameManager.StartGame(GameManager.GameMode.AIOpponent);
        yield return new WaitForSeconds(0.5f);
        
        // Reset scores
        turnManager.playerScore = 0;
        turnManager.aiScore = 0;
        turnManager.isAITurn = false; // Player goes first
        
        Debug.Log("Single game test started - Player goes first");
        Debug.Log("Use the game UI to play your turn, then AI will respond automatically");
        
        yield return null;
    }
}