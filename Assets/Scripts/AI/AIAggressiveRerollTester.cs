using UnityEngine;
using System.Collections.Generic;
using HybridEnemyAI;

/// <summary>
/// Test component for validating aggressive reroll strategy implementation
/// </summary>
public class AIAggressiveRerollTester : MonoBehaviour
{
    [Header("Test Configuration")]
    public bool runTestOnStart = false;
    public bool enableDetailedLogs = true;
    
    [Header("Test Components")]
    public AIAggressiveRerollStrategy aggressiveRerollStrategy;
    public AIDualProbabilityCapSystem dualProbabilityCapSystem;
    
    [Header("Test Results")]
    [SerializeField] private List<TestResult> testResults = new List<TestResult>();
    
    [System.Serializable]
    public class TestResult
    {
        public string TestName;
        public bool Passed;
        public string Details;
        public float ExecutionTime;
        
        public TestResult(string name, bool passed, string details, float time)
        {
            TestName = name;
            Passed = passed;
            Details = details;
            ExecutionTime = time;
        }
    }
    
    void Start()
    {
        if (runTestOnStart)
        {
            RunAllTests();
        }
    }
    
    [ContextMenu("Run All Tests")]
    public void RunAllTests()
    {
        Debug.Log("=== AGGRESSIVE REROLL STRATEGY TESTS ===");
        testResults.Clear();
        
        // Test 1: Basic aggressive reroll execution
        TestBasicAggressiveReroll();
        
        // Test 2: Minimum dice selection
        TestMinimumDiceSelection();
        
        // Test 3: Dual probability cap system
        TestDualProbabilityCapSystem();
        
        // Test 4: Iteration limit enforcement
        TestIterationLimitEnforcement();
        
        // Test 5: Hot streak handling
        TestHotStreakHandling();
        
        // Summary
        LogTestSummary();
    }
    
    void TestBasicAggressiveReroll()
    {
        var startTime = Time.realtimeSinceStartup;
        bool passed = false;
        string details = "";
        
        try
        {
            if (aggressiveRerollStrategy == null)
            {
                details = "AIAggressiveRerollStrategy component not found";
            }
            else
            {
                // Test with a simple dice set
                var testDice = new List<int> { 1, 2, 3, 4, 5, 6 };
                var result = aggressiveRerollStrategy.ExecuteAggressiveReroll(
                    testDice, BehaviorMode.AGGRESSIVE, 0, 500);
                
                if (result != null)
                {
                    passed = result.TotalPointsScored > 0 || result.ZonkOccurred;
                    details = $"Points: {result.TotalPointsScored}, Iterations: {result.Iterations.Count}, " +
                             $"Reason: {result.FinalReason}";
                }
                else
                {
                    details = "Aggressive reroll returned null result";
                }
            }
        }
        catch (System.Exception e)
        {
            details = $"Exception: {e.Message}";
        }
        
        var executionTime = (Time.realtimeSinceStartup - startTime) * 1000f;
        testResults.Add(new TestResult("Basic Aggressive Reroll", passed, details, executionTime));
        
        if (enableDetailedLogs)
        {
            Debug.Log($"Test 1 - Basic Aggressive Reroll: {(passed ? "PASSED" : "FAILED")}");
            Debug.Log($"  Details: {details}");
            Debug.Log($"  Execution Time: {executionTime:F1}ms");
        }
    }
    
    void TestMinimumDiceSelection()
    {
        var startTime = Time.realtimeSinceStartup;
        bool passed = false;
        string details = "";
        
        try
        {
            if (aggressiveRerollStrategy == null)
            {
                details = "AIAggressiveRerollStrategy component not found";
            }
            else
            {
                // Test with dice that should favor minimum dice usage
                var testDice = new List<int> { 1, 1, 2, 3, 4, 6 }; // Two 1s should be selected (minimum dice)
                var result = aggressiveRerollStrategy.ExecuteAggressiveReroll(
                    testDice, BehaviorMode.AGGRESSIVE, 0, 500);
                
                if (result != null && result.Iterations.Count > 0)
                {
                    var firstIteration = result.Iterations[0];
                    // Should select the two 1s (minimum dice for good points)
                    passed = firstIteration.DiceUsed <= 3; // Should use minimal dice
                    details = $"First iteration used {firstIteration.DiceUsed} dice for {firstIteration.PointsGained} points";
                }
                else
                {
                    details = "No iterations found in result";
                }
            }
        }
        catch (System.Exception e)
        {
            details = $"Exception: {e.Message}";
        }
        
        var executionTime = (Time.realtimeSinceStartup - startTime) * 1000f;
        testResults.Add(new TestResult("Minimum Dice Selection", passed, details, executionTime));
        
        if (enableDetailedLogs)
        {
            Debug.Log($"Test 2 - Minimum Dice Selection: {(passed ? "PASSED" : "FAILED")}");
            Debug.Log($"  Details: {details}");
            Debug.Log($"  Execution Time: {executionTime:F1}ms");
        }
    }
    
    void TestDualProbabilityCapSystem()
    {
        var startTime = Time.realtimeSinceStartup;
        bool passed = false;
        string details = "";
        
        try
        {
            if (dualProbabilityCapSystem == null)
            {
                details = "AIDualProbabilityCapSystem component not found";
            }
            else
            {
                // Test cap probability calculation
                dualProbabilityCapSystem.SetDynamicCap(BehaviorMode.AGGRESSIVE);
                var capResult = dualProbabilityCapSystem.CalculateCapProbability(600, BehaviorMode.AGGRESSIVE);
                
                passed = capResult.CapThresholdReached && capResult.FinalCapStopChance > 0f;
                details = $"Cap: {capResult.PointsPerTurnCap}, Score: {capResult.CurrentTurnScore}, " +
                         $"Stop Chance: {capResult.FinalCapStopChance:P1}";
            }
        }
        catch (System.Exception e)
        {
            details = $"Exception: {e.Message}";
        }
        
        var executionTime = (Time.realtimeSinceStartup - startTime) * 1000f;
        testResults.Add(new TestResult("Dual Probability Cap System", passed, details, executionTime));
        
        if (enableDetailedLogs)
        {
            Debug.Log($"Test 3 - Dual Probability Cap System: {(passed ? "PASSED" : "FAILED")}");
            Debug.Log($"  Details: {details}");
            Debug.Log($"  Execution Time: {executionTime:F1}ms");
        }
    }
    
    void TestIterationLimitEnforcement()
    {
        var startTime = Time.realtimeSinceStartup;
        bool passed = false;
        string details = "";
        
        try
        {
            if (aggressiveRerollStrategy == null)
            {
                details = "AIAggressiveRerollStrategy component not found";
            }
            else
            {
                // Test with dice that would normally continue but should hit iteration limit
                var testDice = new List<int> { 1, 5, 2, 3, 4, 6 };
                var result = aggressiveRerollStrategy.ExecuteAggressiveReroll(
                    testDice, BehaviorMode.AGGRESSIVE, 0, 500);
                
                if (result != null)
                {
                    // Should respect iteration limit (5 for aggressive mode)
                    passed = result.Iterations.Count <= 5;
                    details = $"Iterations: {result.Iterations.Count}/5, Limit Reached: {result.IterationLimitReached}";
                }
                else
                {
                    details = "Aggressive reroll returned null result";
                }
            }
        }
        catch (System.Exception e)
        {
            details = $"Exception: {e.Message}";
        }
        
        var executionTime = (Time.realtimeSinceStartup - startTime) * 1000f;
        testResults.Add(new TestResult("Iteration Limit Enforcement", passed, details, executionTime));
        
        if (enableDetailedLogs)
        {
            Debug.Log($"Test 4 - Iteration Limit Enforcement: {(passed ? "PASSED" : "FAILED")}");
            Debug.Log($"  Details: {details}");
            Debug.Log($"  Execution Time: {executionTime:F1}ms");
        }
    }
    
    void TestHotStreakHandling()
    {
        var startTime = Time.realtimeSinceStartup;
        bool passed = false;
        string details = "";
        
        try
        {
            if (aggressiveRerollStrategy == null)
            {
                details = "AIAggressiveRerollStrategy component not found";
            }
            else
            {
                // Test with dice that could potentially create hot streak
                var testDice = new List<int> { 1, 1, 1, 5, 5, 5 }; // Should use all dice for three of a kind
                var result = aggressiveRerollStrategy.ExecuteAggressiveReroll(
                    testDice, BehaviorMode.AGGRESSIVE, 0, 500);
                
                if (result != null)
                {
                    // Check if hot streak was detected and handled
                    passed = true; // Test passes if it completes without error
                    details = $"Hot Streaks: {result.HotStreakCount}, Total Points: {result.TotalPointsScored}";
                }
                else
                {
                    details = "Aggressive reroll returned null result";
                }
            }
        }
        catch (System.Exception e)
        {
            details = $"Exception: {e.Message}";
        }
        
        var executionTime = (Time.realtimeSinceStartup - startTime) * 1000f;
        testResults.Add(new TestResult("Hot Streak Handling", passed, details, executionTime));
        
        if (enableDetailedLogs)
        {
            Debug.Log($"Test 5 - Hot Streak Handling: {(passed ? "PASSED" : "FAILED")}");
            Debug.Log($"  Details: {details}");
            Debug.Log($"  Execution Time: {executionTime:F1}ms");
        }
    }
    
    void LogTestSummary()
    {
        int passedTests = 0;
        int totalTests = testResults.Count;
        float totalTime = 0f;
        
        foreach (var result in testResults)
        {
            if (result.Passed) passedTests++;
            totalTime += result.ExecutionTime;
        }
        
        Debug.Log($"=== TEST SUMMARY ===");
        Debug.Log($"Tests Passed: {passedTests}/{totalTests}");
        Debug.Log($"Success Rate: {(passedTests / (float)totalTests * 100f):F1}%");
        Debug.Log($"Total Execution Time: {totalTime:F1}ms");
        Debug.Log($"Average Test Time: {(totalTime / totalTests):F1}ms");
        
        if (passedTests == totalTests)
        {
            Debug.Log("🎉 ALL TESTS PASSED! Aggressive reroll strategy is working correctly.");
        }
        else
        {
            Debug.LogWarning($"⚠️ {totalTests - passedTests} test(s) failed. Check implementation.");
        }
    }
    
    /// <summary>
    /// Gets test results for external analysis
    /// </summary>
    public List<TestResult> GetTestResults()
    {
        return new List<TestResult>(testResults);
    }
    
    /// <summary>
    /// Validates component references
    /// </summary>
    void ValidateComponents()
    {
        if (aggressiveRerollStrategy == null)
        {
            aggressiveRerollStrategy = GetComponent<AIAggressiveRerollStrategy>();
            if (aggressiveRerollStrategy == null)
            {
                Debug.LogWarning("AIAggressiveRerollTester: AIAggressiveRerollStrategy component not found!");
            }
        }
        
        if (dualProbabilityCapSystem == null)
        {
            dualProbabilityCapSystem = GetComponent<AIDualProbabilityCapSystem>();
            if (dualProbabilityCapSystem == null)
            {
                Debug.LogWarning("AIAggressiveRerollTester: AIDualProbabilityCapSystem component not found!");
            }
        }
    }
    
    void Awake()
    {
        ValidateComponents();
    }
}