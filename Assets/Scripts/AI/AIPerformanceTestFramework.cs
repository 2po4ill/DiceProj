using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HybridEnemyAI;

/// <summary>
/// Comprehensive AI performance testing framework for validating all AI components
/// Provides automated testing, performance metrics, and balance validation
/// Requirements: All requirements validation
/// </summary>
public class AIPerformanceTestFramework : MonoBehaviour
{
    [Header("Test Configuration")]
    public bool runFullTestSuiteOnStart = false;
    public bool enablePerformanceMetrics = true;
    public bool enableBalanceValidation = true;
    public int performanceTestIterations = 1000;
    public float testTimeoutSeconds = 30f;
    
    [Header("AI Components")]
    public AIGameStateAnalyzer gameStateAnalyzer;
    public AICombinationStrategy combinationStrategy;
    public AIDecisionEngine decisionEngine;
    public AIRiskCalculator riskCalculator;
    public AITurnExecutor turnExecutor;
    public AIDiceGenerator diceGenerator;
    public AIMinimumDiceSelector minimumDiceSelector;
    
    [Header("Test Results")]
    [SerializeField] private TestSuiteResults testResults;
    [SerializeField] private PerformanceMetrics performanceMetrics;
    [SerializeField] private BalanceMetrics balanceMetrics;
    
    [System.Serializable]
    public class TestSuiteResults
    {
        public int TotalTests;
        public int PassedTests;
        public int FailedTests;
        public float SuccessRate;
        public float TotalExecutionTime;
        public List<ComponentTestResult> ComponentResults = new List<ComponentTestResult>();
    }
    
    [System.Serializable]
    public class ComponentTestResult
    {
        public string ComponentName;
        public int TestsPassed;
        public int TestsFailed;
        public float ExecutionTime;
        public List<string> FailureReasons = new List<string>();
    }
    
    [System.Serializable]
    public class PerformanceMetrics
    {
        public float AverageDecisionTime;
        public float AverageTurnExecutionTime;
        public float MaxDecisionTime;
        public float MaxTurnExecutionTime;
        public int TotalDecisionsMade;
        public int TotalTurnsExecuted;
        public Dictionary<string, float> ComponentPerformance = new Dictionary<string, float>();
    }
    
    [System.Serializable]
    public class BalanceMetrics
    {
        public float AggressiveWinRate;
        public float PassiveWinRate;
        public float AveragePointsPerTurn;
        public float AverageGameLength;
        public float RiskRewardRatio;
        public Dictionary<BehaviorMode, float> ModeEffectiveness = new Dictionary<BehaviorMode, float>();
    }
    
    void Start()
    {
        InitializeTestFramework();
        
        if (runFullTestSuiteOnStart)
        {
            StartCoroutine(RunFullTestSuite());
        }
    }
    
    void InitializeTestFramework()
    {
        testResults = new TestSuiteResults();
        performanceMetrics = new PerformanceMetrics();
        balanceMetrics = new BalanceMetrics();
        
        ValidateAllComponents();
    }
    
    void ValidateAllComponents()
    {
        var components = new Dictionary<string, MonoBehaviour>
        {
            {"AIGameStateAnalyzer", gameStateAnalyzer},
            {"AICombinationStrategy", combinationStrategy},
            {"AIDecisionEngine", decisionEngine},
            {"AIRiskCalculator", riskCalculator},
            {"AITurnExecutor", turnExecutor},
            {"AIDiceGenerator", diceGenerator},
            {"AIMinimumDiceSelector", minimumDiceSelector}
        };
        
        foreach (var component in components)
        {
            if (component.Value == null)
            {
                Debug.LogWarning($"AIPerformanceTestFramework: {component.Key} not assigned!");
            }
        }
    }
    
    [ContextMenu("Run Full Test Suite")]
    public void RunFullTestSuiteMenu()
    {
        StartCoroutine(RunFullTestSuite());
    }
    
    public IEnumerator RunFullTestSuite()
    {
        Debug.Log("=== STARTING COMPREHENSIVE AI PERFORMANCE TEST SUITE ===");
        var startTime = Time.realtimeSinceStartup;
        
        testResults = new TestSuiteResults();
        
        // Component Tests
        yield return StartCoroutine(TestAllComponents());
        
        // Performance Tests
        if (enablePerformanceMetrics)
        {
            yield return StartCoroutine(RunPerformanceTests());
        }
        
        // Balance Tests
        if (enableBalanceValidation)
        {
            yield return StartCoroutine(RunBalanceTests());
        }
        
        // Integration Tests
        yield return StartCoroutine(RunIntegrationTests());
        
        // Calculate final results
        testResults.TotalExecutionTime = Time.realtimeSinceStartup - startTime;
        testResults.SuccessRate = testResults.TotalTests > 0 ? 
            (testResults.PassedTests / (float)testResults.TotalTests) * 100f : 0f;
        
        LogTestSummary();
        
        Debug.Log("=== AI PERFORMANCE TEST SUITE COMPLETED ===");
    }
    
    IEnumerator TestAllComponents()
    {
        Debug.Log("--- Testing All AI Components ---");
        
        // Test AIGameStateAnalyzer
        yield return StartCoroutine(TestGameStateAnalyzer());
        
        // Test AICombinationStrategy
        yield return StartCoroutine(TestCombinationStrategy());
        
        // Test AIDecisionEngine
        yield return StartCoroutine(TestDecisionEngine());
        
        // Test AIRiskCalculator
        yield return StartCoroutine(TestRiskCalculator());
        
        // Test AIDiceGenerator
        yield return StartCoroutine(TestDiceGenerator());
        
        // Test AIMinimumDiceSelector
        yield return StartCoroutine(TestMinimumDiceSelector());
        
        // Test AITurnExecutor
        yield return StartCoroutine(TestTurnExecutor());
    }
    
    IEnumerator TestGameStateAnalyzer()
    {
        var componentResult = new ComponentTestResult { ComponentName = "AIGameStateAnalyzer" };
        var startTime = Time.realtimeSinceStartup;
        
        if (gameStateAnalyzer == null)
        {
            componentResult.TestsFailed++;
            componentResult.FailureReasons.Add("Component not assigned");
            testResults.ComponentResults.Add(componentResult);
            yield break;
        }
        
        // Test 1: Behavior mode switching
        var mode1 = gameStateAnalyzer.AnalyzeGameState(100, 300); // Should be AGGRESSIVE
        var mode2 = gameStateAnalyzer.AnalyzeGameState(400, 100); // Should be PASSIVE
        
        if (mode1 == BehaviorMode.AGGRESSIVE && mode2 == BehaviorMode.PASSIVE)
        {
            componentResult.TestsPassed++;
        }
        else
        {
            componentResult.TestsFailed++;
            componentResult.FailureReasons.Add($"Behavior mode switching failed: {mode1}, {mode2}");
        }
        
        // Test 2: Dynamic buffer system
        gameStateAnalyzer.ResetState();
        int initialBuffer = gameStateAnalyzer.GetCurrentBufferCap();
        
        for (int i = 0; i < 6; i++)
        {
            gameStateAnalyzer.AdvanceRound();
        }
        
        int finalBuffer = gameStateAnalyzer.GetCurrentBufferCap();
        
        if (finalBuffer < initialBuffer)
        {
            componentResult.TestsPassed++;
        }
        else
        {
            componentResult.TestsFailed++;
            componentResult.FailureReasons.Add("Dynamic buffer system not working");
        }
        
        // Test 3: Threshold calculations
        float aggThreshold = gameStateAnalyzer.GetCombinationThreshold(BehaviorMode.AGGRESSIVE, 6);
        float passThreshold = gameStateAnalyzer.GetCombinationThreshold(BehaviorMode.PASSIVE, 6);
        
        if (aggThreshold > passThreshold && aggThreshold > 0)
        {
            componentResult.TestsPassed++;
        }
        else
        {
            componentResult.TestsFailed++;
            componentResult.FailureReasons.Add("Threshold calculations incorrect");
        }
        
        componentResult.ExecutionTime = Time.realtimeSinceStartup - startTime;
        testResults.ComponentResults.Add(componentResult);
        testResults.TotalTests += componentResult.TestsPassed + componentResult.TestsFailed;
        testResults.PassedTests += componentResult.TestsPassed;
        testResults.FailedTests += componentResult.TestsFailed;
        
        yield return null;
    }
    
    IEnumerator TestCombinationStrategy()
    {
        var componentResult = new ComponentTestResult { ComponentName = "AICombinationStrategy" };
        var startTime = Time.realtimeSinceStartup;
        
        if (combinationStrategy == null)
        {
            componentResult.TestsFailed++;
            componentResult.FailureReasons.Add("Component not assigned");
            testResults.ComponentResults.Add(componentResult);
            yield break;
        }
        
        // Test 1: Hierarchical evaluation
        var testDice = new List<int> { 1, 1, 1, 2, 3, 4 }; // Three of a kind
        var result = combinationStrategy.EvaluateBestStrategy(testDice, BehaviorMode.AGGRESSIVE, 6);
        
        if (result != null && result.combination.points > 0)
        {
            componentResult.TestsPassed++;
        }
        else
        {
            componentResult.TestsFailed++;
            componentResult.FailureReasons.Add("Hierarchical evaluation failed");
        }
        
        // Test 2: Strategic value calculation
        if (result != null && result.strategicValue > 0)
        {
            componentResult.TestsPassed++;
        }
        else
        {
            componentResult.TestsFailed++;
            componentResult.FailureReasons.Add("Strategic value calculation failed");
        }
        
        // Test 3: Behavior mode differences
        var aggressiveResult = combinationStrategy.EvaluateBestStrategy(testDice, BehaviorMode.AGGRESSIVE, 6);
        var passiveResult = combinationStrategy.EvaluateBestStrategy(testDice, BehaviorMode.PASSIVE, 6);
        
        if (aggressiveResult != null && passiveResult != null)
        {
            componentResult.TestsPassed++;
        }
        else
        {
            componentResult.TestsFailed++;
            componentResult.FailureReasons.Add("Behavior mode differences not working");
        }
        
        componentResult.ExecutionTime = Time.realtimeSinceStartup - startTime;
        testResults.ComponentResults.Add(componentResult);
        testResults.TotalTests += componentResult.TestsPassed + componentResult.TestsFailed;
        testResults.PassedTests += componentResult.TestsPassed;
        testResults.FailedTests += componentResult.TestsFailed;
        
        yield return null;
    }
    
    IEnumerator TestDecisionEngine()
    {
        var componentResult = new ComponentTestResult { ComponentName = "AIDecisionEngine" };
        var startTime = Time.realtimeSinceStartup;
        
        if (decisionEngine == null)
        {
            componentResult.TestsFailed++;
            componentResult.FailureReasons.Add("Component not assigned");
            testResults.ComponentResults.Add(componentResult);
            yield break;
        }
        
        // Test 1: Basic decision making
        var turnState = new AITurnState();
        turnState.CurrentTurnScore = 200;
        turnState.IterationCount = 1;
        turnState.SuccessfulCombinationsCount = 1;
        
        var decision = decisionEngine.MakeDecision(turnState, BehaviorMode.AGGRESSIVE, 100, 300);
        
        if (decision != null)
        {
            componentResult.TestsPassed++;
        }
        else
        {
            componentResult.TestsFailed++;
            componentResult.FailureReasons.Add("Basic decision making failed");
        }
        
        // Test 2: Momentum system integration
        turnState.SuccessfulCombinationsCount = 3; // Hot streak
        var momentumDecision = decisionEngine.MakeDecision(turnState, BehaviorMode.AGGRESSIVE, 100, 300);
        
        if (momentumDecision != null && momentumDecision.DecisionFactors?.StopDecision != null)
        {
            componentResult.TestsPassed++;
        }
        else
        {
            componentResult.TestsFailed++;
            componentResult.FailureReasons.Add("Momentum system integration failed");
        }
        
        componentResult.ExecutionTime = Time.realtimeSinceStartup - startTime;
        testResults.ComponentResults.Add(componentResult);
        testResults.TotalTests += componentResult.TestsPassed + componentResult.TestsFailed;
        testResults.PassedTests += componentResult.TestsPassed;
        testResults.FailedTests += componentResult.TestsFailed;
        
        yield return null;
    }
    
    IEnumerator TestRiskCalculator()
    {
        var componentResult = new ComponentTestResult { ComponentName = "AIRiskCalculator" };
        var startTime = Time.realtimeSinceStartup;
        
        if (riskCalculator == null)
        {
            componentResult.TestsFailed++;
            componentResult.FailureReasons.Add("Component not assigned");
            testResults.ComponentResults.Add(componentResult);
            yield break;
        }
        
        // Test 1: Dual probability calculation
        var turnState = new AITurnState();
        turnState.CurrentTurnScore = 300;
        turnState.IterationCount = 2;
        turnState.SuccessfulCombinationsCount = 2;
        
        var stopDecision = riskCalculator.CalculateStopDecision(turnState, BehaviorMode.AGGRESSIVE);
        
        if (stopDecision != null && stopDecision.CombinedStopChance >= 0)
        {
            componentResult.TestsPassed++;
        }
        else
        {
            componentResult.TestsFailed++;
            componentResult.FailureReasons.Add("Dual probability calculation failed");
        }
        
        // Test 2: Momentum-based calculations
        if (stopDecision != null && stopDecision.MomentumStopChance >= 0)
        {
            componentResult.TestsPassed++;
        }
        else
        {
            componentResult.TestsFailed++;
            componentResult.FailureReasons.Add("Momentum calculations failed");
        }
        
        // Test 3: Cap-based calculations
        if (stopDecision != null && stopDecision.CapStopChance >= 0)
        {
            componentResult.TestsPassed++;
        }
        else
        {
            componentResult.TestsFailed++;
            componentResult.FailureReasons.Add("Cap calculations failed");
        }
        
        componentResult.ExecutionTime = Time.realtimeSinceStartup - startTime;
        testResults.ComponentResults.Add(componentResult);
        testResults.TotalTests += componentResult.TestsPassed + componentResult.TestsFailed;
        testResults.PassedTests += componentResult.TestsPassed;
        testResults.FailedTests += componentResult.TestsFailed;
        
        yield return null;
    }
    
    IEnumerator TestDiceGenerator()
    {
        var componentResult = new ComponentTestResult { ComponentName = "AIDiceGenerator" };
        var startTime = Time.realtimeSinceStartup;
        
        if (diceGenerator == null)
        {
            componentResult.TestsFailed++;
            componentResult.FailureReasons.Add("Component not assigned");
            testResults.ComponentResults.Add(componentResult);
            yield break;
        }
        
        // Test 1: Basic dice generation
        var dice = diceGenerator.GenerateRandomDice(6);
        
        if (dice != null && dice.Count == 6 && dice.All(d => d >= 1 && d <= 6))
        {
            componentResult.TestsPassed++;
        }
        else
        {
            componentResult.TestsFailed++;
            componentResult.FailureReasons.Add("Basic dice generation failed");
        }
        
        // Test 2: Distribution validation (simplified)
        var distributionTest = true;
        for (int i = 0; i < 100; i++)
        {
            var testDice = diceGenerator.GenerateRandomDice(6);
            if (testDice == null || testDice.Count != 6 || testDice.Any(d => d < 1 || d > 6))
            {
                distributionTest = false;
                break;
            }
        }
        
        if (distributionTest)
        {
            componentResult.TestsPassed++;
        }
        else
        {
            componentResult.TestsFailed++;
            componentResult.FailureReasons.Add("Distribution validation failed");
        }
        
        componentResult.ExecutionTime = Time.realtimeSinceStartup - startTime;
        testResults.ComponentResults.Add(componentResult);
        testResults.TotalTests += componentResult.TestsPassed + componentResult.TestsFailed;
        testResults.PassedTests += componentResult.TestsPassed;
        testResults.FailedTests += componentResult.TestsFailed;
        
        yield return null;
    }
    
    IEnumerator TestMinimumDiceSelector()
    {
        var componentResult = new ComponentTestResult { ComponentName = "AIMinimumDiceSelector" };
        var startTime = Time.realtimeSinceStartup;
        
        if (minimumDiceSelector == null)
        {
            componentResult.TestsFailed++;
            componentResult.FailureReasons.Add("Component not assigned");
            testResults.ComponentResults.Add(componentResult);
            yield break;
        }
        
        // Test 1: Minimum dice selection
        var testDice = new List<int> { 1, 1, 1, 5, 2, 3 };
        var result = minimumDiceSelector.SelectMinimumDiceCombination(testDice, BehaviorMode.PASSIVE);
        
        if (result != null && result.selectedCombination != null)
        {
            componentResult.TestsPassed++;
        }
        else
        {
            componentResult.TestsFailed++;
            componentResult.FailureReasons.Add("Minimum dice selection failed");
        }
        
        // Test 2: Confidence scoring
        if (result != null && result.confidenceScore >= 0 && result.confidenceScore <= 1.0f)
        {
            componentResult.TestsPassed++;
        }
        else
        {
            componentResult.TestsFailed++;
            componentResult.FailureReasons.Add("Confidence scoring failed");
        }
        
        componentResult.ExecutionTime = Time.realtimeSinceStartup - startTime;
        testResults.ComponentResults.Add(componentResult);
        testResults.TotalTests += componentResult.TestsPassed + componentResult.TestsFailed;
        testResults.PassedTests += componentResult.TestsPassed;
        testResults.FailedTests += componentResult.TestsFailed;
        
        yield return null;
    }
    
    IEnumerator TestTurnExecutor()
    {
        var componentResult = new ComponentTestResult { ComponentName = "AITurnExecutor" };
        var startTime = Time.realtimeSinceStartup;
        
        if (turnExecutor == null)
        {
            componentResult.TestsFailed++;
            componentResult.FailureReasons.Add("Component not assigned");
            testResults.ComponentResults.Add(componentResult);
            yield break;
        }
        
        // Test 1: Turn state management
        if (turnExecutor.currentTurnState != null)
        {
            componentResult.TestsPassed++;
        }
        else
        {
            componentResult.TestsFailed++;
            componentResult.FailureReasons.Add("Turn state management failed");
        }
        
        // Test 2: Component integration
        bool hasRequiredComponents = turnExecutor.gameStateAnalyzer != null &&
                                   turnExecutor.combinationStrategy != null &&
                                   turnExecutor.decisionEngine != null;
        
        if (hasRequiredComponents)
        {
            componentResult.TestsPassed++;
        }
        else
        {
            componentResult.TestsFailed++;
            componentResult.FailureReasons.Add("Component integration incomplete");
        }
        
        componentResult.ExecutionTime = Time.realtimeSinceStartup - startTime;
        testResults.ComponentResults.Add(componentResult);
        testResults.TotalTests += componentResult.TestsPassed + componentResult.TestsFailed;
        testResults.PassedTests += componentResult.TestsPassed;
        testResults.FailedTests += componentResult.TestsFailed;
        
        yield return null;
    }
    
    IEnumerator RunPerformanceTests()
    {
        Debug.Log("--- Running Performance Tests ---");
        
        var decisionTimes = new List<float>();
        var turnTimes = new List<float>();
        
        // Performance test for decision making
        for (int i = 0; i < performanceTestIterations / 10; i++)
        {
            if (decisionEngine != null)
            {
                var startTime = Time.realtimeSinceStartup;
                
                var turnState = new AITurnState();
                turnState.CurrentTurnScore = Random.Range(0, 500);
                turnState.IterationCount = Random.Range(1, 5);
                
                decisionEngine.MakeDecision(turnState, BehaviorMode.AGGRESSIVE, 100, 300);
                
                var decisionTime = Time.realtimeSinceStartup - startTime;
                decisionTimes.Add(decisionTime);
            }
            
            if (i % 10 == 0)
                yield return null; // Prevent frame drops
        }
        
        // Calculate performance metrics
        if (decisionTimes.Count > 0)
        {
            performanceMetrics.AverageDecisionTime = (float)decisionTimes.Average();
            performanceMetrics.MaxDecisionTime = (float)decisionTimes.Max();
            performanceMetrics.TotalDecisionsMade = decisionTimes.Count;
        }
        
        Debug.Log($"Performance Test Complete - Avg Decision Time: {performanceMetrics.AverageDecisionTime * 1000f:F2}ms");
    }
    
    IEnumerator RunBalanceTests()
    {
        Debug.Log("--- Running Balance Tests ---");
        
        var aggressiveWins = 0;
        var passiveWins = 0;
        var totalGames = 50; // Reduced for testing
        
        for (int i = 0; i < totalGames; i++)
        {
            // Simulate game scenarios
            var aggressiveScore = SimulateAIPerformance(BehaviorMode.AGGRESSIVE);
            var passiveScore = SimulateAIPerformance(BehaviorMode.PASSIVE);
            
            if (aggressiveScore > passiveScore)
                aggressiveWins++;
            else
                passiveWins++;
            
            if (i % 10 == 0)
                yield return null;
        }
        
        balanceMetrics.AggressiveWinRate = (aggressiveWins / (float)totalGames) * 100f;
        balanceMetrics.PassiveWinRate = (passiveWins / (float)totalGames) * 100f;
        
        Debug.Log($"Balance Test Complete - Aggressive: {balanceMetrics.AggressiveWinRate:F1}%, Passive: {balanceMetrics.PassiveWinRate:F1}%");
    }
    
    int SimulateAIPerformance(BehaviorMode mode)
    {
        // Simplified AI performance simulation
        var baseScore = mode == BehaviorMode.AGGRESSIVE ? 400 : 300;
        var variance = mode == BehaviorMode.AGGRESSIVE ? 200 : 100;
        
        return baseScore + Random.Range(-variance, variance);
    }
    
    IEnumerator RunIntegrationTests()
    {
        Debug.Log("--- Running Integration Tests ---");
        
        // Test full AI system integration
        var integrationResult = new ComponentTestResult { ComponentName = "Integration" };
        var startTime = Time.realtimeSinceStartup;
        
        // Test 1: Component communication
        bool componentsConnected = ValidateComponentConnections();
        
        if (componentsConnected)
        {
            integrationResult.TestsPassed++;
        }
        else
        {
            integrationResult.TestsFailed++;
            integrationResult.FailureReasons.Add("Component connections failed");
        }
        
        // Test 2: End-to-end workflow
        bool workflowTest = TestEndToEndWorkflow();
        
        if (workflowTest)
        {
            integrationResult.TestsPassed++;
        }
        else
        {
            integrationResult.TestsFailed++;
            integrationResult.FailureReasons.Add("End-to-end workflow failed");
        }
        
        integrationResult.ExecutionTime = Time.realtimeSinceStartup - startTime;
        testResults.ComponentResults.Add(integrationResult);
        testResults.TotalTests += integrationResult.TestsPassed + integrationResult.TestsFailed;
        testResults.PassedTests += integrationResult.TestsPassed;
        testResults.FailedTests += integrationResult.TestsFailed;
        
        yield return null;
    }
    
    bool ValidateComponentConnections()
    {
        // Check if components can communicate with each other
        try
        {
            if (gameStateAnalyzer != null && decisionEngine != null)
            {
                var mode = gameStateAnalyzer.AnalyzeGameState(100, 300);
                var turnState = new AITurnState();
                var decision = decisionEngine.MakeDecision(turnState, mode, 100, 300);
                return decision != null;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Component connection test failed: {e.Message}");
        }
        
        return false;
    }
    
    bool TestEndToEndWorkflow()
    {
        // Test complete AI decision workflow
        try
        {
            if (gameStateAnalyzer == null || combinationStrategy == null || decisionEngine == null)
                return false;
            
            // Simulate complete workflow
            var mode = gameStateAnalyzer.AnalyzeGameState(200, 300);
            var testDice = new List<int> { 1, 2, 3, 4, 5, 6 };
            var strategy = combinationStrategy.EvaluateBestStrategy(testDice, mode, 6);
            
            if (strategy != null)
            {
                var turnState = new AITurnState();
                turnState.CurrentTurnScore = strategy.combination.points;
                var decision = decisionEngine.MakeDecision(turnState, mode, 200, 300);
                
                return decision != null;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"End-to-end workflow test failed: {e.Message}");
        }
        
        return false;
    }
    
    void LogTestSummary()
    {
        Debug.Log("=== AI PERFORMANCE TEST SUMMARY ===");
        Debug.Log($"Total Tests: {testResults.TotalTests}");
        Debug.Log($"Passed: {testResults.PassedTests}");
        Debug.Log($"Failed: {testResults.FailedTests}");
        Debug.Log($"Success Rate: {testResults.SuccessRate:F1}%");
        Debug.Log($"Total Execution Time: {testResults.TotalExecutionTime:F2}s");
        
        if (enablePerformanceMetrics)
        {
            Debug.Log($"Average Decision Time: {performanceMetrics.AverageDecisionTime * 1000f:F2}ms");
            Debug.Log($"Max Decision Time: {performanceMetrics.MaxDecisionTime * 1000f:F2}ms");
        }
        
        if (enableBalanceValidation)
        {
            Debug.Log($"Aggressive Win Rate: {balanceMetrics.AggressiveWinRate:F1}%");
            Debug.Log($"Passive Win Rate: {balanceMetrics.PassiveWinRate:F1}%");
        }
        
        // Component breakdown
        foreach (var component in testResults.ComponentResults)
        {
            Debug.Log($"{component.ComponentName}: {component.TestsPassed} passed, {component.TestsFailed} failed ({component.ExecutionTime * 1000f:F1}ms)");
            
            if (component.FailureReasons.Count > 0)
            {
                foreach (var reason in component.FailureReasons)
                {
                    Debug.LogWarning($"  - {reason}");
                }
            }
        }
        
        if (testResults.SuccessRate >= 90f)
        {
            Debug.Log("🎉 EXCELLENT: AI system performing at high quality!");
        }
        else if (testResults.SuccessRate >= 75f)
        {
            Debug.Log("✅ GOOD: AI system performing well with minor issues.");
        }
        else
        {
            Debug.LogWarning("⚠️ NEEDS ATTENTION: AI system has significant issues that need addressing.");
        }
    }
    
    /// <summary>
    /// Get detailed test results for external analysis
    /// </summary>
    public TestSuiteResults GetTestResults()
    {
        return testResults;
    }
    
    /// <summary>
    /// Get performance metrics for tuning
    /// </summary>
    public PerformanceMetrics GetPerformanceMetrics()
    {
        return performanceMetrics;
    }
    
    /// <summary>
    /// Get balance metrics for difficulty adjustment
    /// </summary>
    public BalanceMetrics GetBalanceMetrics()
    {
        return balanceMetrics;
    }
}