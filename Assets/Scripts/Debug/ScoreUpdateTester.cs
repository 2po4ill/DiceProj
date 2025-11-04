using UnityEngine;
using System.Collections;

/// <summary>
/// Score Update Tester - Tests the fixed score update flow
/// Verifies that total scores update correctly with multipliers
/// </summary>
public class ScoreUpdateTester : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private GameTurnManager turnManager;
    [SerializeField] private TurnScoreManager scoreManager;
    [SerializeField] private GameManager gameManager;
    
    [Header("Test Settings")]
    [SerializeField] private bool runTestsOnStart = false;
    [SerializeField] private bool enableDetailedLogs = true;
    
    void Start()
    {
        FindComponents();
        
        if (runTestsOnStart)
        {
            StartCoroutine(RunAllTests());
        }
    }
    
    void FindComponents()
    {
        if (turnManager == null)
            turnManager = FindObjectOfType<GameTurnManager>();
        
        if (scoreManager == null)
            scoreManager = FindObjectOfType<TurnScoreManager>();
        
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();
    }
    
    [ContextMenu("Run All Score Tests")]
    public void RunAllScoreTests()
    {
        StartCoroutine(RunAllTests());
    }
    
    IEnumerator RunAllTests()
    {
        Debug.Log("=== STARTING SCORE UPDATE TESTS ===");
        
        yield return StartCoroutine(TestPlayerTurnScoreUpdate());
        yield return new WaitForSeconds(1f);
        
        yield return StartCoroutine(TestAITurnScoreUpdate());
        yield return new WaitForSeconds(1f);
        
        yield return StartCoroutine(TestMultiplierApplication());
        yield return new WaitForSeconds(1f);
        
        Debug.Log("=== ALL SCORE TESTS COMPLETED ===");
    }
    
    IEnumerator TestPlayerTurnScoreUpdate()
    {
        Debug.Log("🧪 TEST 1: Player Turn Score Update");
        
        if (!ValidateComponents()) yield break;
        
        // Setup AI vs Player mode
        if (gameManager != null)
        {
            gameManager.StartGame(GameManager.GameMode.AIOpponent);
            yield return new WaitForSeconds(0.5f);
        }
        
        // Record initial state
        int initialPlayerScore = turnManager.playerScore;
        int initialAIScore = turnManager.aiScore;
        
        Debug.Log($"Initial scores - Player: {initialPlayerScore}, AI: {initialAIScore}");
        
        // Simulate player turn with score
        SimulatePlayerTurnWithScore(200, 1.5f); // 200 base points, 1.5x multiplier
        
        yield return new WaitForSeconds(0.5f);
        
        // Check results
        int expectedFinalScore = Mathf.RoundToInt(200 * 1.5f); // 300
        int actualPlayerScore = turnManager.playerScore;
        
        Debug.Log($"Expected player score increase: {expectedFinalScore}");
        Debug.Log($"Actual player score: {actualPlayerScore} (increase: {actualPlayerScore - initialPlayerScore})");
        
        if (actualPlayerScore - initialPlayerScore == expectedFinalScore)
        {
            Debug.Log("✅ TEST 1 PASSED: Player score updated correctly with multipliers");
        }
        else
        {
            Debug.LogError($"❌ TEST 1 FAILED: Expected increase {expectedFinalScore}, got {actualPlayerScore - initialPlayerScore}");
        }
    }
    
    IEnumerator TestAITurnScoreUpdate()
    {
        Debug.Log("🧪 TEST 2: AI Turn Score Update");
        
        if (!ValidateComponents()) yield break;
        
        // Record initial state
        int initialPlayerScore = turnManager.playerScore;
        int initialAIScore = turnManager.aiScore;
        
        Debug.Log($"Initial scores - Player: {initialPlayerScore}, AI: {initialAIScore}");
        
        // Simulate AI turn with score
        SimulateAITurnWithScore(300, 2.0f); // 300 base points, 2.0x multiplier
        
        yield return new WaitForSeconds(0.5f);
        
        // Check results
        int expectedFinalScore = Mathf.RoundToInt(300 * 2.0f); // 600
        int actualAIScore = turnManager.aiScore;
        
        Debug.Log($"Expected AI score increase: {expectedFinalScore}");
        Debug.Log($"Actual AI score: {actualAIScore} (increase: {actualAIScore - initialAIScore})");
        
        if (actualAIScore - initialAIScore == expectedFinalScore)
        {
            Debug.Log("✅ TEST 2 PASSED: AI score updated correctly with multipliers");
        }
        else
        {
            Debug.LogError($"❌ TEST 2 FAILED: Expected increase {expectedFinalScore}, got {actualAIScore - initialAIScore}");
        }
    }
    
    IEnumerator TestMultiplierApplication()
    {
        Debug.Log("🧪 TEST 3: Multiplier Application Test");
        
        if (!ValidateComponents()) yield break;
        
        // Test various multiplier scenarios
        var testCases = new[]
        {
            new { baseScore = 100, multiplier = 1.0f, expected = 100 },
            new { baseScore = 150, multiplier = 1.2f, expected = 180 },
            new { baseScore = 250, multiplier = 2.5f, expected = 625 },
            new { baseScore = 50, multiplier = 0.5f, expected = 25 }
        };
        
        foreach (var testCase in testCases)
        {
            Debug.Log($"Testing: {testCase.baseScore} × {testCase.multiplier} = {testCase.expected}");
            
            int initialScore = turnManager.playerScore;
            
            SimulatePlayerTurnWithScore(testCase.baseScore, testCase.multiplier);
            yield return new WaitForSeconds(0.2f);
            
            int actualIncrease = turnManager.playerScore - initialScore;
            
            if (actualIncrease == testCase.expected)
            {
                Debug.Log($"✅ Multiplier test passed: {testCase.baseScore} × {testCase.multiplier} = {actualIncrease}");
            }
            else
            {
                Debug.LogError($"❌ Multiplier test failed: Expected {testCase.expected}, got {actualIncrease}");
            }
        }
        
        Debug.Log("✅ TEST 3 COMPLETED: Multiplier application tests");
    }
    
    void SimulatePlayerTurnWithScore(int baseScore, float multiplier)
    {
        if (enableDetailedLogs)
            Debug.Log($"🎮 Simulating player turn: {baseScore} base points, {multiplier}x multiplier");
        
        // Ensure it's player's turn
        if (turnManager.isAITurn)
        {
            turnManager.isAITurn = false;
        }
        
        // Start new turn
        scoreManager.StartNewTurn(turnManager.currentTurn);
        
        // Set custom multiplier
        if (scoreManager.currentTurn != null)
        {
            scoreManager.currentTurn.turnMultiplier = multiplier;
        }
        
        // Add score
        var combination = new CombinationResult(Rule.ThreeOfKind, baseScore, $"Test combination: {baseScore} points", 0f);
        scoreManager.AddCombination(combination);
        
        // Complete turn (this should trigger the score update)
        scoreManager.CompleteTurn();
        
        // Switch turn (this should update player score)
        if (turnManager.isAIOpponent)
        {
            // Use reflection to call private SwitchTurn method
            var method = typeof(GameTurnManager).GetMethod("SwitchTurn", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(turnManager, null);
            }
        }
        
        if (enableDetailedLogs)
            Debug.Log($"🎮 Player turn simulation complete");
    }
    
    void SimulateAITurnWithScore(int baseScore, float multiplier)
    {
        if (enableDetailedLogs)
            Debug.Log($"🤖 Simulating AI turn: {baseScore} base points, {multiplier}x multiplier");
        
        // Ensure it's AI's turn
        if (!turnManager.isAITurn)
        {
            turnManager.isAITurn = true;
        }
        
        // Start new turn
        scoreManager.StartNewTurn(turnManager.currentTurn);
        
        // Set custom multiplier
        if (scoreManager.currentTurn != null)
        {
            scoreManager.currentTurn.turnMultiplier = multiplier;
        }
        
        // Add score
        var combination = new CombinationResult(Rule.Straight, baseScore, $"Test AI combination: {baseScore} points", 0f);
        scoreManager.AddCombination(combination);
        
        // Complete turn (this should trigger the score update)
        scoreManager.CompleteTurn();
        
        // Switch turn (this should update AI score)
        if (turnManager.isAIOpponent)
        {
            // Use reflection to call private SwitchTurn method
            var method = typeof(GameTurnManager).GetMethod("SwitchTurn", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(turnManager, null);
            }
        }
        
        if (enableDetailedLogs)
            Debug.Log($"🤖 AI turn simulation complete");
    }
    
    bool ValidateComponents()
    {
        if (turnManager == null)
        {
            Debug.LogError("❌ GameTurnManager not found!");
            return false;
        }
        
        if (scoreManager == null)
        {
            Debug.LogError("❌ TurnScoreManager not found!");
            return false;
        }
        
        return true;
    }
    
    [ContextMenu("Test Current Score State")]
    public void TestCurrentScoreState()
    {
        Debug.Log("=== CURRENT SCORE STATE ===");
        
        if (turnManager != null)
        {
            Debug.Log($"Game Mode: {(turnManager.isAIOpponent ? "AI vs Player" : "Single Player")}");
            Debug.Log($"Current Turn: {turnManager.currentTurn} ({(turnManager.isAITurn ? "AI" : "Player")})");
            Debug.Log($"Player Score: {turnManager.playerScore}");
            Debug.Log($"AI Score: {turnManager.aiScore}");
            Debug.Log($"Total Score: {turnManager.totalScore}");
        }
        
        if (scoreManager != null)
        {
            Debug.Log($"Current Turn Score: {scoreManager.GetCurrentTurnScore()}");
            Debug.Log($"Current Multiplier: {scoreManager.GetCurrentTurnMultiplier():F2}x");
            Debug.Log($"Projected Final: {scoreManager.GetProjectedFinalScore()}");
            Debug.Log($"Total Game Score: {scoreManager.totalGameScore}");
            Debug.Log($"Turn History Count: {scoreManager.turnHistory.Count}");
            
            if (scoreManager.turnHistory.Count > 0)
            {
                var lastTurn = scoreManager.turnHistory[scoreManager.turnHistory.Count - 1];
                Debug.Log($"Last Turn: Base={lastTurn.baseScore}, Multiplier={lastTurn.turnMultiplier:F2}x, Final={lastTurn.finalScore}");
            }
        }
    }
    
    [ContextMenu("Force Score Update")]
    public void ForceScoreUpdate()
    {
        Debug.Log("=== FORCING SCORE UPDATE ===");
        
        if (turnManager == null || scoreManager == null)
        {
            Debug.LogError("Missing components for score update test");
            return;
        }
        
        // Add a test combination
        var testCombination = new CombinationResult(Rule.Pair, 100, "Test combination", 0f);
        scoreManager.AddCombination(testCombination);
        
        Debug.Log($"Added test combination: {testCombination.points} points");
        Debug.Log($"Current turn score: {scoreManager.GetCurrentTurnScore()}");
        
        // Complete the turn
        scoreManager.CompleteTurn();
        Debug.Log($"Turn completed. Total game score: {scoreManager.totalGameScore}");
        
        // Force switch turn
        if (turnManager.isAIOpponent)
        {
            var method = typeof(GameTurnManager).GetMethod("SwitchTurn", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(turnManager, null);
                Debug.Log($"Turn switched. Player: {turnManager.playerScore}, AI: {turnManager.aiScore}");
            }
        }
    }
}